using FMODUnity;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Characters;
using UnityEngine;

namespace TentacleVania.Abilities.BioShield
{
    [CreateAssetMenu(menuName = "TentacleVania/Abilities/Bio-Shield")]
    public sealed class BioShieldDefinition : AbilityDefinition
    {
        [Header("Carapace")]
        [SerializeField, Min(0f)] private float _absorb = 40f;
        [SerializeField, Min(0.1f)] private float _duration = 3f;

        [Header("Burst")]
        [SerializeField, Min(0f)] private float _burstDamage = 25f;
        [SerializeField, Min(0.1f)] private float _burstRadius = 4f;
        [Tooltip("Heavy stagger (no number in the design): enough to break most prototype enemies' bars.")]
        [SerializeField, Min(0f)] private float _burstStagger = 45f;
        [SerializeField] private DamageType _burstType = DamageType.Physical;
        [SerializeField] private LayerMask _hitLayers;

        [Header("Placeholder presentation")]
        [SerializeField] private GameObject _shellPrefab;
        [SerializeField, Min(0.1f)] private float _shellRadius = 0.75f;
        [SerializeField, Min(0.01f)] private float _burstVisualTime = 0.3f;
        [SerializeField] private Color _statusColor = new(1f, 0.62f, 0.15f, 1f);
        [SerializeField, Range(0f, 1f)] private float _statusAmount = 0.6f;
        [SerializeField, Min(0.01f)] private float _statusFresnel = CharacterMaterials.DefaultStatusFresnel;
        [SerializeField, Min(0f)] private float _statusFill = CharacterMaterials.DefaultStatusFill;
        [SerializeField] private EventReference _activateSound;
        [SerializeField] private EventReference _absorbSound;
        [SerializeField] private EventReference _burstSound;

        public float Absorb => _absorb;
        public float Duration => _duration;
        public float BurstDamage => _burstDamage;
        public float BurstRadius => _burstRadius;
        public float BurstStagger => _burstStagger;
        public DamageType BurstType => _burstType;
        public LayerMask HitLayers => _hitLayers;
        public GameObject ShellPrefab => _shellPrefab;
        public float ShellRadius => _shellRadius;
        public float BurstVisualTime => _burstVisualTime;
        public Color StatusColor => _statusColor;
        public float StatusAmount => _statusAmount;
        public float StatusFresnel => _statusFresnel;
        public float StatusFill => _statusFill;
        public EventReference ActivateSound => _activateSound;
        public EventReference AbsorbSound => _absorbSound;
        public EventReference BurstSound => _burstSound;

        public override AbilityRuntime CreateRuntime(AbilityContext context) => new BioShieldRuntime(this, context);
    }
}
