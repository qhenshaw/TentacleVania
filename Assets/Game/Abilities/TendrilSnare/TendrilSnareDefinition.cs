using FMODUnity;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.Abilities.TendrilSnare
{
    [CreateAssetMenu(menuName = "TentacleVania/Abilities/Tendril Snare")]
    public sealed class TendrilSnareDefinition : AbilityDefinition
    {
        [Tooltip("Snare radius per tier (index 0 = tier 1).")]
        [SerializeField] private float[] _radius = { 3f, 3.5f, 4f };
        [Tooltip("Root duration per tier.")]
        [SerializeField] private float[] _rootTime = { 2.5f, 3f, 3.5f };
        [SerializeField, Min(0f)] private float _damage = 8f;
        [Tooltip("Where the snare erupts: on the nearest enemy within this range ahead, else this far in front.")]
        [SerializeField, Min(0f)] private float _reach = 7f;
        [SerializeField, Min(0f)] private float _fallbackDistance = 4f;
        [SerializeField, Range(0f, 180f)] private float _aimCone = 90f;
        [SerializeField, Min(0f)] private float _delay = 0.3f;
        [SerializeField] private LayerMask _hitLayers;
        [SerializeField] private VisualEffect _burst;
        [SerializeField] private EventReference _activateSound;
        [SerializeField] private EventReference _eruptSound;

        public float RadiusAt(int tier) => At(_radius, tier);
        public float RootTimeAt(int tier) => At(_rootTime, tier);
        public float Damage => _damage;
        public float Reach => _reach;
        public float FallbackDistance => _fallbackDistance;
        public float AimCone => _aimCone;
        public float Delay => _delay;
        public LayerMask HitLayers => _hitLayers;
        public VisualEffect Burst => _burst;
        public EventReference ActivateSound => _activateSound;
        public EventReference EruptSound => _eruptSound;

        private static float At(float[] values, int tier) =>
            values == null || values.Length == 0 ? 0f : values[Mathf.Clamp(tier - 1, 0, values.Length - 1)];

        public override AbilityRuntime CreateRuntime(AbilityContext context) => new TendrilSnareRuntime(this, context);
    }
}
