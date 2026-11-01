using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using TentacleVania.Core;
using TentacleVania.Core.Debugging;
using UnityEngine;

namespace TentacleVania.Combat
{
    public static class CombatDebugCommands
    {
        private static Health _lastTarget;
        private static DamageInfo _lastInfo;
        private static DamageResult _lastResult;
        private static int _hits;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            _hits = 0;
            _lastTarget = null;
            CombatEvents.HitLanded += OnHit;
            DebugMenu.Slider(DebugMenu.Combat, "Damage dealt (assist)", 0.1f, 3f, () => GameSettings.DamageDealtScale, v => GameSettings.DamageDealtScale = v);
            DebugMenu.Slider(DebugMenu.Combat, "Damage taken (assist)", 0f, 3f, () => GameSettings.DamageTakenScale, v => GameSettings.DamageTakenScale = v);
            DebugMenu.Button(DebugMenu.Combat, "Reset assist sliders", () =>
            {
                GameSettings.DamageDealtScale = 1f;
                GameSettings.DamageTakenScale = 1f;
            });
            DebugMenu.Value(DebugMenu.Combat, "Hits landed", () => _hits.ToString());
            DebugMenu.Value(DebugMenu.Combat, "Last hit", FormatLastHit);
            DebugMenu.Value(DebugMenu.Combat, "Last hit feedback", () => Feedback.HitFeedback.LastKind.ToString());
            DebugMenu.Toggle(DebugMenu.Combat, "Hit-stop", () => Core.Feedback.HitStop.Enabled, v => Core.Feedback.HitStop.Enabled = v);
            DebugMenu.Toggle(DebugMenu.Combat, "Rumble", () => GameSettings.Rumble, v => GameSettings.Rumble = v);
            DebugMenu.Toggle(DebugMenu.Combat, "Gore", () => GameSettings.Gore, v => GameSettings.Gore = v);
            DebugMenu.Toggle(DebugMenu.Combat, "Hit effects", () => Feedback.HitEffects.Enabled, v => Feedback.HitEffects.Enabled = v);
            DebugMenu.Value(DebugMenu.Combat, "Blood decals", () =>
                Feedback.HitEffects.Instance != null ? $"{Feedback.HitEffects.Instance.ActiveDecals} / {Feedback.HitEffects.Instance.DecalCapacity}" : "-");
            DebugMenu.Button(DebugMenu.Combat, "Clear blood decals", () =>
            {
                if (Feedback.HitEffects.Instance != null)
                    Feedback.HitEffects.Instance.ClearDecals();
            });
            DebugMenu.Value(DebugMenu.Combat, "Gibs active", () =>
                Feedback.HitEffects.Instance != null ? Feedback.HitEffects.Instance.ActiveGibs.ToString() : "-");
            DebugMenu.Button(DebugMenu.Combat, "Clear gibs", () =>
            {
                if (Feedback.HitEffects.Instance != null)
                    Feedback.HitEffects.Instance.ClearGibs();
            });
            DebugMenu.Button(DebugMenu.Combat, "Bleed nearest enemy (+1 stack)", () => ApplyToNearest(StatusKind.Bleed));
            DebugMenu.Button(DebugMenu.Combat, "Burn nearest enemy", () => ApplyToNearest(StatusKind.Burn));
            DebugMenu.Button(DebugMenu.Combat, "Burst nearest enemy's bleed", () =>
            {
                var target = NearestEnemyStatus();
                if (target != null)
                    target.BurstBleed();
            });
            DebugMenu.Button(DebugMenu.Combat, "Static hit nearest enemy", () => ApplyToNearest(StatusKind.Static));
            DebugMenu.Button(DebugMenu.Combat, "Fill nearest enemy's static", () =>
            {
                var target = NearestEnemyStatus();
                if (target != null)
                    target.FillStatic(Team.Creature);
            });
            DebugMenu.Button(DebugMenu.Combat, "Release nearest enemy's static", () =>
            {
                var target = NearestEnemyStatus();
                if (target != null)
                    target.ReleaseStatic(Team.Creature);
            });
            DebugMenu.Button(DebugMenu.Combat, "Corrode nearest enemy", () => ApplyToNearest(StatusKind.Corrode));
            DebugMenu.Button(DebugMenu.Combat, "Acid pool under nearest enemy", () =>
            {
                var target = NearestEnemyStatus();
                if (target != null)
                    AcidPools.Spawn(target.transform.position, Team.Creature);
            });
            DebugMenu.Button(DebugMenu.Combat, "Hostile acid pool under creature", () =>
            {
                if (CombatTargets.Creature != null)
                    AcidPools.Spawn(CombatTargets.Creature.transform.position, Team.Site);
            });
            DebugMenu.Value(DebugMenu.Combat, "Acid pools active", () => AcidPools.Active.ToString());
            DebugMenu.Button(DebugMenu.Combat, "Corrode creature", () => ApplyToCreature(StatusKind.Corrode));
            DebugMenu.Value(DebugMenu.Combat, "Chain lightning discharges", () => StatusEffects.Discharges.ToString());
            DebugMenu.Value(DebugMenu.Combat, "Nearest enemy status", FormatNearestStatus);
            DebugMenu.Button(DebugMenu.Combat, "Bleed creature (+1 stack)", () => ApplyToCreature(StatusKind.Bleed));
            DebugMenu.Button(DebugMenu.Combat, "Burn creature", () => ApplyToCreature(StatusKind.Burn));
            DebugMenu.Button(DebugMenu.Combat, "Clear creature statuses", () =>
            {
                var status = CreatureStatus();
                if (status != null)
                    status.ClearAll();
            });
        }

        private static void OnHit(Health target, in DamageInfo info, in DamageResult result)
        {
            _hits++;
            _lastTarget = target;
            _lastInfo = info;
            _lastResult = result;
        }

        private static StatusEffects CreatureStatus() =>
            CombatTargets.Creature != null ? CombatTargets.Creature.GetComponent<StatusEffects>() : null;

        private static void ApplyToCreature(StatusKind kind)
        {
            var status = CreatureStatus();
            if (status != null)
                status.Apply(kind, 0f, 0f, Team.Site, 1f);
        }

        private static void ApplyToNearest(StatusKind kind)
        {
            var target = NearestEnemyStatus();
            if (target != null)
                target.Apply(kind, 0f, 0f, Team.Creature, Core.Progression.Stats.StatusPotency);
        }

        private static StatusEffects NearestEnemyStatus()
        {
            var creature = CombatTargets.Creature;
            var origin = creature != null ? creature.transform.position : Vector3.zero;
            StatusEffects best = null;
            var bestDistance = float.MaxValue;
            foreach (var status in Object.FindObjectsByType<StatusEffects>())
            {
                if (status.Health == creature || status.Health == null || !status.Health.IsAlive)
                    continue;
                var distance = (status.transform.position - origin).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = status;
                }
            }
            return best;
        }

        private static string FormatNearestStatus()
        {
            var target = NearestEnemyStatus();
            if (target == null)
                return "-";
            return $"{target.name}: bleed {target.BleedStacks} ({target.BleedDamagePerSecond:F1}/s, {target.Remaining(StatusKind.Bleed):F1} s) burn {target.BurnDamagePerSecond:F1}/s ({target.Remaining(StatusKind.Burn):F1} s) static {target.StaticNormalized:P0} corrode {target.Remaining(StatusKind.Corrode):F1} s";
        }

        private static string FormatLastHit()
        {
            if (_lastTarget == null)
                return "-";
            var armor = _lastResult.ArmorDamage > 0f ? $" +{_lastResult.ArmorDamage:F0} armor" : string.Empty;
            var stagger = _lastResult.StaggerDamage > 0f ? $" {_lastResult.StaggerDamage:F0} stagger" : string.Empty;
            return $"{_lastTarget.name}: {_lastResult.HealthDamage:F0} {_lastInfo.Type}{armor}{stagger}";
        }
    }
}
