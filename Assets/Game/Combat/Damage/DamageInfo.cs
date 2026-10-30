using UnityEngine;

namespace TentacleVania.Combat.Damage
{
    public struct DamageInfo
    {
        public float Amount;
        public float Stagger;
        public float ArmorDamageScale;
        public DamageType Type;
        public Team SourceTeam;
        public HitFlags Flags;
        public float StunDuration;
        public Health Attacker;
        public Vector3 Point;
        public Vector3 Direction;

        public static DamageInfo Create(float amount, Team sourceTeam, DamageType type = DamageType.Physical, float stagger = 0f) => new()
        {
            Amount = amount,
            Stagger = stagger,
            ArmorDamageScale = 1f,
            Type = type,
            SourceTeam = sourceTeam,
        };
    }

    public enum DamageOutcome
    {
        Applied,
        Ignored,
        Blocked,
    }

    public readonly struct DamageResult
    {
        public readonly DamageOutcome Outcome;
        public readonly float HealthDamage;
        public readonly float ArmorDamage;
        public readonly float StaggerDamage;
        public readonly bool Killed;

        public DamageResult(DamageOutcome outcome, float health = 0f, float armor = 0f, float stagger = 0f, bool killed = false)
        {
            Outcome = outcome;
            HealthDamage = health;
            ArmorDamage = armor;
            StaggerDamage = stagger;
            Killed = killed;
        }

        public bool Applied => Outcome == DamageOutcome.Applied;
    }

    public struct DamageContext
    {
        public DamageInfo Info;
        public Team TargetTeam;
        public float HealthScale;
        public float ArmorScale;
        public float StaggerScale;
        public bool Cancelled;

        public DamageContext(in DamageInfo info, Team targetTeam)
        {
            Info = info;
            TargetTeam = targetTeam;
            HealthScale = 1f;
            ArmorScale = 1f;
            StaggerScale = 1f;
            Cancelled = false;
        }
    }
}
