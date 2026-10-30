using System;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Weapons.Data
{
    public enum MoveShape
    {
        Sweep,
        Area,
        Body,
    }

    [Serializable]
    public sealed class WeaponMove
    {
        public string Name = "Move";
        public MoveShape Shape;

        [Header("Timing (s)")]
        [Min(0f)] public float WindUp = 0.08f;
        [Min(0.01f)] public float Strike = 0.1f;
        [Min(0f)] public float Recovery = 0.2f;

        [Header("Multipliers on the weapon's base values")]
        [Min(0f)] public float DamageScale = 1f;
        [Min(0f)] public float StaggerScale = 1f;
        [Min(0f)] public float ArmorDamageScale = 1f;
        public HitFlags Flags;

        [Header("Shape")]
        [Tooltip("Sweep: distance of the swept arc. Area: unused. Body: unused.")]
        [Min(0.1f)] public float Reach = 2f;
        [Tooltip("Sweep: the arc runs from StartAngle to EndAngle (degrees, 0 = forward, positive = right).")]
        public float StartAngle = -60f;
        public float EndAngle = 60f;
        [Tooltip("Sweep: cast radius. Area: circle radius. Body: radius around the creature.")]
        [Min(0.05f)] public float Radius = 0.35f;
        [Tooltip("Area: centre distance in front of the creature.")]
        public float AreaOffset;
        [Tooltip("Area: sector width in degrees (360 = full circle).")]
        [Range(1f, 360f)] public float SectorAngle = 360f;
        [Min(0f)] public float Height = 0.5f;

        [Header("Dash attacks")]
        [Min(0f)] public float LungeDistance;

        public float Duration => WindUp + Strike + Recovery;
    }
}
