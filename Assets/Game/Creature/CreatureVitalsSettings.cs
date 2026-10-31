using TentacleVania.Core.Characters;
using UnityEngine;

namespace TentacleVania.Creature
{
    [CreateAssetMenu(menuName = "TentacleVania/Creature/Vitals Settings")]
    public sealed class CreatureVitalsSettings : ScriptableObject
    {
        [Header("Health")]
        [SerializeField, Min(1f)] private float _maxHealth = 100f;
        [SerializeField, Min(0f)] private float _maxArmor;

        [Header("Death")]
        [SerializeField, Min(0f)] private float _restartDelay = 1.5f;

        [Header("Stun")]
        [SerializeField, ColorUsage(false, true)] private Color _stunGlow = new(0.4f, 0.7f, 2.2f);
        [SerializeField, Min(0f)] private float _stunGlowPulse = 2.23f;
        [SerializeField, Min(0.01f)] private float _stunGlowFresnel = CharacterMaterials.DefaultStatusFresnel;
        [SerializeField, Min(0f)] private float _stunGlowFill = CharacterMaterials.DefaultStatusFill;
        [SerializeField, Min(0f)] private float _stunGrace = 3f;

        public float MaxHealth => _maxHealth;
        public float MaxArmor => _maxArmor;
        public float RestartDelay => _restartDelay;
        public Color StunGlow => _stunGlow;
        public float StunGlowPulse => _stunGlowPulse;
        public float StunGlowFresnel => _stunGlowFresnel;
        public float StunGlowFill => _stunGlowFill;
        public float StunGrace => _stunGrace;
    }
}
