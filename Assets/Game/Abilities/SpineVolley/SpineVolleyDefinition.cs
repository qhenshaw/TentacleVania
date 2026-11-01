using FMODUnity;
using UnityEngine;

namespace TentacleVania.Abilities.SpineVolley
{
    [CreateAssetMenu(menuName = "TentacleVania/Abilities/Spine Volley")]
    public sealed class SpineVolleyDefinition : AbilityDefinition
    {
        [Tooltip("Spines per volley, per tier (index 0 = tier 1).")]
        [SerializeField] private int[] _spines = { 5, 7, 9 };
        [SerializeField] private float[] _damage = { 10f, 13f, 16f };
        [SerializeField, Range(0f, 120f)] private float _spread = 30f;
        [SerializeField, Min(1f)] private float _speed = 24f;
        [SerializeField, Min(1f)] private float _range = 12f;
        [SerializeField, Min(0.05f)] private float _radius = 0.2f;
        [SerializeField, Min(0f)] private float _stagger = 6f;
        [Tooltip("Auto-aim: the nearest enemy within the range and this cone of the facing becomes the volley's centre.")]
        [SerializeField, Range(0f, 180f)] private float _aimCone = 70f;
        [SerializeField] private LayerMask _hitLayers;
        [SerializeField] private LayerMask _wallLayers;
        [SerializeField] private Transform _spinePrefab;
        [Tooltip("Mutation hooks (design): ids checked with MutationLoadout.IsEquipped.")]
        [SerializeField] private string _piercingMutation = "PiercingSpines";
        [SerializeField] private string _cascadeMutation = "SpineCascade";
        [SerializeField, Min(0)] private int _cascadeExtra = 2;
        [SerializeField, Min(1f)] private float _cascadeSpread = 1.5f;
        [SerializeField] private string _bleedMutation = "EmbeddedBarbs";
        [SerializeField] private EventReference _activateSound;
        [SerializeField] private EventReference _hitSound;

        public int SpinesAt(int tier) => _spines == null || _spines.Length == 0 ? 1 : _spines[Mathf.Clamp(tier - 1, 0, _spines.Length - 1)];
        public float DamageAt(int tier) => _damage == null || _damage.Length == 0 ? 0f : _damage[Mathf.Clamp(tier - 1, 0, _damage.Length - 1)];
        public int MaxSpines
        {
            get
            {
                var max = 1;
                if (_spines != null)
                    foreach (var count in _spines)
                        max = Mathf.Max(max, count);
                return max;
            }
        }

        public float Spread => _spread;
        public float Speed => _speed;
        public float Range => _range;
        public float Radius => _radius;
        public float Stagger => _stagger;
        public float AimCone => _aimCone;
        public LayerMask HitLayers => _hitLayers;
        public LayerMask WallLayers => _wallLayers;
        public Transform SpinePrefab => _spinePrefab;
        public string PiercingMutation => _piercingMutation;
        public string CascadeMutation => _cascadeMutation;
        public int CascadeExtra => _cascadeExtra;
        public float CascadeSpread => _cascadeSpread;
        public string BleedMutation => _bleedMutation;
        public EventReference ActivateSound => _activateSound;
        public EventReference HitSound => _hitSound;

        public static Vector3 FanDirection(Vector3 forward, float spread, int index, int count)
        {
            if (count <= 1)
                return forward;
            var angle = Mathf.Lerp(-spread * 0.5f, spread * 0.5f, index / (float)(count - 1));
            return Quaternion.Euler(0f, angle, 0f) * forward;
        }

        public override AbilityRuntime CreateRuntime(AbilityContext context) => new SpineVolleyRuntime(this, context);
    }
}
