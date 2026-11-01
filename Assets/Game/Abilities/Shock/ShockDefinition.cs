using FMODUnity;
using TentacleVania.Combat.Damage;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.Abilities.Shock
{
    [CreateAssetMenu(menuName = "TentacleVania/Abilities/Shock")]
    public sealed class ShockDefinition : AbilityDefinition
    {
        [Tooltip("Discharge radius per tier (index 0 = tier 1).")]
        [SerializeField] private float[] _radius = { 4f, 5f, 6f };
        [SerializeField] private float[] _damage = { 25f, 35f, 45f };
        [Tooltip("Static added per enemy hit, per tier (a full bar chains lightning).")]
        [SerializeField] private float[] _static = { 35f, 50f, 70f };
        [SerializeField, Min(0f)] private float _stagger = 40f;
        [SerializeField] private LayerMask _hitLayers;
        [SerializeField, ColorUsage(false, true)] private Color _flashColor = new(0.4f, 1.6f, 3f, 1f);
        [SerializeField] private VisualEffect _burst;
        [SerializeField] private EventReference _activateSound;

        public LayerMask HitLayers => _hitLayers;
        public float Stagger => _stagger;
        public Color FlashColor => _flashColor;
        public VisualEffect Burst => _burst;
        public EventReference ActivateSound => _activateSound;

        public float RadiusAt(int tier) => At(_radius, tier);
        public float DamageAt(int tier) => At(_damage, tier);
        public float StaticAt(int tier) => At(_static, tier);

        public static float At(float[] values, int tier) =>
            values == null || values.Length == 0 ? 0f : values[Mathf.Clamp(tier - 1, 0, values.Length - 1)];

        public override AbilityRuntime CreateRuntime(AbilityContext context) => new ShockRuntime(this, context);
    }
}
