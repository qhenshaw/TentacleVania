using TentacleVania.Core.Characters;
using UnityEngine;

namespace TentacleVania.Creature
{
    [CreateAssetMenu(menuName = "TentacleVania/Creature/Charge Settings")]
    public sealed class CreatureChargeSettings : ScriptableObject
    {
        [SerializeField, Min(1f)] private float _max = 100f;
        [SerializeField, Min(0f)] private float _perWeaponHit = 2f;
        [SerializeField, Min(0f)] private float _perConsume = 10f;

        [Header("Full glow")]
        [SerializeField, ColorUsage(false, true)] private Color _fullGlow = new(0.3f, 1.4f, 1.1f);
        [SerializeField, Min(0f)] private float _fullGlowAmount = 0.8f;
        [SerializeField, Min(0f)] private float _fullGlowPulse = 0.8f;
        [SerializeField, Min(0.01f)] private float _fullGlowFresnel = CharacterMaterials.DefaultStatusFresnel;
        [SerializeField, Min(0f)] private float _fullGlowFill = CharacterMaterials.DefaultStatusFill;

        public float Max => _max;
        public float PerWeaponHit => _perWeaponHit;
        public float PerConsume => _perConsume;
        public Color FullGlow => _fullGlow;
        public float FullGlowAmount => _fullGlowAmount;
        public float FullGlowPulse => _fullGlowPulse;
        public float FullGlowFresnel => _fullGlowFresnel;
        public float FullGlowFill => _fullGlowFill;
    }
}
