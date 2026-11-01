using System.Collections.Generic;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Abilities.Feats
{
    public static class FeatTracker
    {
        private const float AbilityGroupTime = 0.25f;
        private const int MaxTracked = 8;

        private static readonly List<FeatDefinition> Defs = new();
        private static readonly List<FeatCounter> Counters = new();
        private static readonly List<Health> Attackers = new(MaxTracked);
        private static readonly List<Health> AbilityTargets = new(MaxTracked);
        private static float _abilityGroupStart = -1f;
        private static bool _abilityGroupCounted;
        private static bool _consuming;
        private static Health _pendingStagger;
        private static bool _hooked;

        public static bool Enabled { get; set; } = true;
        public static IReadOnlyList<FeatDefinition> Definitions => Defs;

        public static int Progress(int index) => index >= 0 && index < Counters.Count ? Counters[index].Count : 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Defs.Clear();
            Counters.Clear();
            Attackers.Clear();
            AbilityTargets.Clear();
            _abilityGroupStart = -1f;
            _consuming = false;
            _pendingStagger = null;
            _hooked = false;
            Enabled = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Application.isPlaying || _hooked)
                return;
            _hooked = true;
            var catalog = Resources.Load<FeatCatalog>(FeatCatalog.ResourcePath);
            if (catalog != null)
                foreach (var def in catalog.Feats)
                    if (def != null)
                    {
                        Defs.Add(def);
                        Counters.Add(new FeatCounter(def.Count, def.Window));
                    }
            LoadSaved();
            SaveSystem.Loaded += OnLoaded;
            CombatEvents.HitLanded += OnHit;
            CombatEvents.StaggerBroken += OnStaggerBroken;
            CombatEvents.ConsumeStarted += OnConsumeStarted;
            CombatEvents.Consumed += OnConsumed;
        }

        private static void OnLoaded(SaveData data) => LoadSaved();

        private static void LoadSaved()
        {
            for (var i = 0; i < Defs.Count; i++)
                Counters[i].Set(Defs[i].Cumulative ? FeatProgress.Count(Defs[i].Id) : 0);
        }

        public static void ResetProgress()
        {
            FeatProgress.ResetAll();
            foreach (var counter in Counters)
                counter.Reset();
        }

        private static bool IsCreature(Health health) => health != null && health == CombatTargets.Creature;

        private static float CreatureFraction()
        {
            var creature = CombatTargets.Creature;
            return creature != null ? creature.Normalized : 1f;
        }

        private static bool HasStatus(Health target, StatusKind kind) =>
            kind != StatusKind.None && target.TryGetComponent<StatusEffects>(out var status) && status.IsActive(kind);

        private static void OnHit(Health target, in DamageInfo info, in DamageResult result)
        {
            if (!Enabled || target == null)
                return;
            if (IsCreature(target))
            {
                if (_consuming && info.Attacker != null && !Attackers.Contains(info.Attacker) && Attackers.Count < MaxTracked)
                    Attackers.Add(info.Attacker);
                return;
            }
            if (info.SourceTeam != Team.Creature)
                return;
            var now = Time.time;
            var machine = !target.Bleeds;
            if (target == _pendingStagger)
            {
                _pendingStagger = null;
                Check(FeatCondition.StaggerBreak, info, machine, target, now);
            }
            if ((info.Flags & HitFlags.Ability) != 0)
                TrackAbilityGroup(target, info, now);
            if (result.Killed)
                Check(FeatCondition.Kill, info, machine, target, now);
        }

        private static void TrackAbilityGroup(Health target, in DamageInfo info, float now)
        {
            if (now - _abilityGroupStart > AbilityGroupTime)
            {
                _abilityGroupStart = now;
                _abilityGroupCounted = false;
                AbilityTargets.Clear();
            }
            if (!AbilityTargets.Contains(target) && AbilityTargets.Count < MaxTracked)
                AbilityTargets.Add(target);
            for (var i = 0; i < Defs.Count; i++)
                if (Defs[i].Condition == FeatCondition.AbilityMultiHit && !_abilityGroupCounted && AbilityTargets.Count >= Defs[i].MinTargets)
                {
                    _abilityGroupCounted = true;
                    Advance(i, now);
                }
        }

        private static void Check(FeatCondition condition, in DamageInfo info, bool machine, Health target, float now)
        {
            var fraction = CreatureFraction();
            for (var i = 0; i < Defs.Count; i++)
            {
                var def = Defs[i];
                if (def.Condition != condition)
                    continue;
                if (def.MatchesHit(info, machine, HasStatus(target, def.TargetStatus), fraction))
                    Advance(i, now);
                else if (def.Streak && !Counters[i].Done)
                    Counters[i].Reset();
            }
        }

        private static void OnStaggerBroken(Health target)
        {
            if (Enabled && !IsCreature(target))
                _pendingStagger = target;
        }

        private static void OnConsumeStarted(Consumable consumable)
        {
            _consuming = true;
            Attackers.Clear();
        }

        private static void OnConsumed(Consumable consumable)
        {
            if (!Enabled)
                return;
            var now = Time.time;
            var target = consumable != null ? consumable.GetComponent<Health>() : null;
            for (var i = 0; i < Defs.Count; i++)
            {
                var def = Defs[i];
                if (def.Condition == FeatCondition.ConsumeUnderPressure && Attackers.Count >= def.MinTargets)
                    Advance(i, now);
                else if (def.Condition == FeatCondition.Consume && target != null && (def.TargetStatus == StatusKind.None || HasStatus(target, def.TargetStatus)))
                    Advance(i, now);
            }
            _consuming = false;
            Attackers.Clear();
        }

        private static void Advance(int index, float now)
        {
            var def = Defs[index];
            var counter = Counters[index];
            if (counter.Done || MutationLoadout.Owns(def.MutationId))
                return;
            var count = counter.Record(now);
            if (def.Cumulative)
                FeatProgress.SetCount(def.Id, count);
            FeatProgress.Report(def.TitleKey, count, counter.Target);
            if (!counter.Done)
                return;
            Collectibles.Grant(PickupKind.Mutation, def.MutationId, 1);
            Autosave.Request(AutosaveReason.Pickup);
        }
    }
}
