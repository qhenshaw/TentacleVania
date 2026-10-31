using System;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Enemies.Data
{
    public enum AttackKind
    {
        Melee,
        Area,
        Projectile,
    }

    public enum Telegraph
    {
        Dodgeable,
        Undodgeable,
    }

    public enum AttackAnimation
    {
        Swing,
        Punch,
        Kick,
        Shoot,
    }

    public enum IndicatorMode
    {
        Auto,
        On,
        Off,
    }

    public enum AttackTokenKind
    {
        Melee,
        Ranged,
    }

    [Serializable]
    public sealed class EnemyAttack
    {
        public string Name = "Attack";
        public AttackKind Kind;
        public Telegraph Telegraph;
        [Tooltip("Body animation; its hit frame is timed to land when the attack goes active.")]
        public AttackAnimation Animation;
        [Tooltip("Seconds into the body animation where its hit lands; the clip is sped up so that moment meets the end of the wind-up.")]
        [Min(0.01f)] public float AnimationHitTime = 0.48f;
        [Tooltip("Ground telegraph: Auto shows one for area, projectile and red (undodgeable) attacks.")]
        public IndicatorMode GroundIndicator;

        [Header("Timing (s)")]
        [Min(0f)] public float WindUp = 0.6f;
        [Min(0.01f)] public float Active = 0.15f;
        [Min(0f)] public float Recovery = 0.5f;
        [Min(0f)] public float Cooldown = 1.5f;
        [Range(0f, 1f)] public float TrackDuringWindUp = 0.6f;

        [Header("Use when the target is within (m)")]
        [Min(0f)] public float MinRange;
        [Min(0f)] public float MaxRange = 2f;

        [Header("Damage")]
        [Min(0f)] public float Damage = 8f;
        [Min(0f)] public float Stagger;
        public DamageType Type;
        [Tooltip("Seconds the creature loses control when this lands (Handler's heavy stun).")]
        [Min(0f)] public float StunDuration;

        [Header("Melee sweep")]
        [Min(0.1f)] public float Reach = 1.8f;
        [Range(0f, 360f)] public float Arc = 120f;
        [Min(0.05f)] public float SweepRadius = 0.25f;
        [Min(0f)] public float Height = 1f;

        [Header("Area (cylinder matching the ground indicator)")]
        [Min(0.1f)] public float AreaRadius = 2.5f;
        public float AreaForwardOffset;

        [Header("Projectile")]
        public EnemyProjectile Projectile;
        [Min(0.1f)] public float ProjectileSpeed = 14f;
        [Min(0.1f)] public float ProjectileLifetime = 3f;
        [Tooltip("Width of the ground lane telegraphing the shot.")]
        [Min(0.05f)] public float LaneWidth = 0.4f;

        public AttackTokenKind TokenKind => Kind == AttackKind.Projectile ? AttackTokenKind.Ranged : AttackTokenKind.Melee;
        public float Duration => WindUp + Active + Recovery;
        public HitFlags Flags => Telegraph == Telegraph.Undodgeable ? HitFlags.Undodgeable : HitFlags.None;
        public bool InRange(float distance) => distance >= MinRange && distance <= MaxRange;

        public bool ShowsGroundIndicator(bool forceAll) => GroundIndicator switch
        {
            IndicatorMode.On => true,
            IndicatorMode.Off => forceAll,
            _ => forceAll || Kind != AttackKind.Melee || Telegraph == Telegraph.Undodgeable,
        };
    }
}
