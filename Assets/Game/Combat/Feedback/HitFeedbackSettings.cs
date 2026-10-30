using System;
using UnityEngine;

namespace TentacleVania.Combat.Feedback
{
    public enum HitFeedbackKind
    {
        None,
        Light,
        Heavy,
        Finisher,
        DashAttack,
        StaggerBreak,
        CreatureHurt,
    }

    [Serializable]
    public struct HitFeedbackLevel
    {
        [Tooltip("Seconds of hit-stop (unscaled).")]
        [Min(0f)] public float HitStop;
        [Tooltip("Camera impulse strength.")]
        [Min(0f)] public float Shake;
        [Range(0f, 1f)] public float RumbleLow;
        [Range(0f, 1f)] public float RumbleHigh;
        [Min(0f)] public float RumbleDuration;
        [Tooltip("Metres an enemy is pushed along the hit direction.")]
        [Min(0f)] public float Knockback;
        [Tooltip("Degrees the enemy's body leans back on the flinch.")]
        [Min(0f)] public float Flinch;

        public HitFeedbackLevel(float hitStop, float shake, float rumbleLow, float rumbleHigh, float rumbleDuration, float knockback, float flinch)
        {
            HitStop = hitStop;
            Shake = shake;
            RumbleLow = rumbleLow;
            RumbleHigh = rumbleHigh;
            RumbleDuration = rumbleDuration;
            Knockback = knockback;
            Flinch = flinch;
        }
    }

    [CreateAssetMenu(menuName = "TentacleVania/Combat/Hit Feedback Settings")]
    public sealed class HitFeedbackSettings : ScriptableObject
    {
        [SerializeField] private HitFeedbackLevel _light = new(0.035f, 0.08f, 0.05f, 0.2f, 0.06f, 0.25f, 10f);
        [SerializeField] private HitFeedbackLevel _heavy = new(0.09f, 0.35f, 0.5f, 0.6f, 0.16f, 0.9f, 18f);
        [SerializeField] private HitFeedbackLevel _finisher = new(0.07f, 0.25f, 0.35f, 0.5f, 0.12f, 0.7f, 16f);
        [SerializeField] private HitFeedbackLevel _dashAttack = new(0.05f, 0.18f, 0.25f, 0.4f, 0.1f, 0.5f, 14f);
        [SerializeField] private HitFeedbackLevel _staggerBreak = new(0.14f, 0.5f, 0.7f, 0.8f, 0.22f, 0f, 0f);
        [SerializeField] private HitFeedbackLevel _creatureHurt = new(0.06f, 0.4f, 0.6f, 0.35f, 0.18f, 0f, 0f);
        [Tooltip("Seconds the knockback push takes.")]
        [SerializeField, Min(0.01f)] private float _knockbackDuration = 0.15f;
        [Tooltip("Seconds the flinch lean takes to settle.")]
        [SerializeField, Min(0.01f)] private float _flinchDuration = 0.22f;

        public float KnockbackDuration => _knockbackDuration;
        public float FlinchDuration => _flinchDuration;

        public HitFeedbackLevel Level(HitFeedbackKind kind) => kind switch
        {
            HitFeedbackKind.Light => _light,
            HitFeedbackKind.Heavy => _heavy,
            HitFeedbackKind.Finisher => _finisher,
            HitFeedbackKind.DashAttack => _dashAttack,
            HitFeedbackKind.StaggerBreak => _staggerBreak,
            HitFeedbackKind.CreatureHurt => _creatureHurt,
            _ => default,
        };
    }
}
