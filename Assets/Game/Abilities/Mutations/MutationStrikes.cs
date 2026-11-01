using System.Collections.Generic;
using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    public static class MutationStrikes
    {
        public const float MeleeRange = 3.5f;
        private static readonly Collider[] Hits = new Collider[32];
        private static readonly List<Health> Struck = new(32);
        private static int _mask = -1;

        public static float ThornsStaggerScale { get; set; }
        public static int ThornsHits { get; private set; }

        public static bool IsMelee(Health target, in DamageInfo info) =>
            info.Attacker != null && info.Attacker != target
            && (info.Flags & (HitFlags.Hazard | HitFlags.DamageOverTime | HitFlags.Reflected | HitFlags.ChainLightning)) == 0
            && (info.Attacker.transform.position - target.transform.position).sqrMagnitude <= MeleeRange * MeleeRange;

        public static void Thorns(Health creature, Health attacker, float damage)
        {
            if (creature == null || attacker == null || attacker == creature || !attacker.IsAlive || damage <= 0f)
                return;
            var info = DamageInfo.Create(damage, creature.Team, DamageType.Physical, damage * ThornsStaggerScale);
            info.Flags = HitFlags.Reflected;
            info.Attacker = creature;
            info.Point = attacker.transform.position + Vector3.up;
            info.Direction = Away(creature.transform.position, attacker.transform.position);
            if (attacker.TakeDamage(info).Applied)
                ThornsHits++;
        }

        public static int Ring(Health source, Vector3 center, float radius, float damage, float stagger, DamageType type, HitFlags flags, StatusKind status = StatusKind.None, Health skip = null)
        {
            if (_mask < 0)
                _mask = 1 << LayerMask.NameToLayer("Enemy");
            var count = Physics.OverlapSphereNonAlloc(center + Vector3.up * 0.5f, radius, Hits, _mask, QueryTriggerInteraction.Collide);
            Struck.Clear();
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Hits[i], out var hurtbox))
                    continue;
                var target = hurtbox.Health;
                if (target == null || target == source || target == skip || !target.IsAlive || Struck.Contains(target))
                    continue;
                Struck.Add(target);
            }
            var struck = Struck.Count;
            for (var i = 0; i < struck; i++)
            {
                var target = Struck[i];
                var info = DamageInfo.Create(damage, source != null ? source.Team : Team.Creature, type, stagger);
                info.Flags = flags;
                info.Status = status;
                info.Attacker = source;
                info.Point = target.transform.position + Vector3.up;
                info.Direction = Away(center, target.transform.position);
                target.TakeDamage(info);
            }
            return struck;
        }

        public static void Play(EventReference sound, Vector3 position)
        {
            if (!sound.IsNull)
                RuntimeManager.PlayOneShot(sound, position);
        }

        private static Vector3 Away(Vector3 from, Vector3 to)
        {
            var away = to - from;
            away.y = 0f;
            return away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.forward;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ThornsStaggerScale = 0f;
            ThornsHits = 0;
        }
    }
}
