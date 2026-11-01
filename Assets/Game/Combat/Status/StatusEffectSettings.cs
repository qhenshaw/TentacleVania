using System;
using FMODUnity;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.Combat.Status
{
    [CreateAssetMenu(menuName = "TentacleVania/Combat/Status Effect Settings")]
    public sealed class StatusEffectSettings : ScriptableObject
    {
        public const string ResourcePath = "StatusEffectSettings";

        [Serializable]
        public sealed class DamageStatus
        {
            [Min(0f)] public float DamagePerSecond = 3f;
            [Min(0.01f)] public float Duration = 5f;
            [Min(1)] public int MaxStacks = 1;
            public VisualEffect Effect;
            public EventReference ApplySound;
        }

        [Header("Balance Sheet: Stagger & status effects")]
        [SerializeField] private DamageStatus _bleed = new() { DamagePerSecond = 3f, Duration = 5f, MaxStacks = 5 };
        [SerializeField] private DamageStatus _burn = new() { DamagePerSecond = 5f, Duration = 4f, MaxStacks = 1 };
        [SerializeField, Min(0.05f)] private float _tickInterval = 0.5f;

        [Header("Static: fills from electric hits (~5 hits on a soldier), then chain lightning")]
        [SerializeField, Min(1f)] private float _staticCapacity = 100f;
        [SerializeField, Min(0f)] private float _staticPerHit = 20f;
        [SerializeField, Min(0f)] private float _chainDamage = 30f;
        [SerializeField, Min(0)] private int _chainTargets = 3;
        [SerializeField, Min(0f)] private float _chainRadius = 6f;
        [SerializeField] private bool _chainHitsSource;
        [SerializeField] private Material _chainMaterial;
        [SerializeField, ColorUsage(false, true)] private Color _chainColor = new(0.6f, 1.6f, 2.4f);
        [SerializeField, Min(0.01f)] private float _chainWidth = 0.08f;
        [SerializeField, Min(0.01f)] private float _chainDuration = 0.25f;
        [SerializeField, Min(0f)] private float _chainJitter = 0.35f;
        [SerializeField] private EventReference _chainSound;

        [Header("Corrode: +20% health damage taken, bonus armor damage (TBD), 4 s, refreshes")]
        [SerializeField, Min(1f)] private float _corrodeDamageTaken = 1.2f;
        [SerializeField, Min(1f)] private float _corrodeArmorDamage = 1.5f;
        [SerializeField, Min(0.01f)] private float _corrodeDuration = 4f;
        [SerializeField] private VisualEffect _corrodeEffect;
        [SerializeField] private EventReference _corrodeSound;

        [Header("Acid pools: last 4 s (damage per second and radius are placeholders)")]
        [SerializeField, Min(1)] private int _acidPoolCount = 12;
        [SerializeField, Min(0.1f)] private float _acidPoolRadius = 1.5f;
        [SerializeField, Min(0.1f)] private float _acidPoolDuration = 4f;
        [SerializeField, Min(0f)] private float _acidPoolDamagePerSecond = 6f;
        [SerializeField, Min(0f)] private float _acidPoolFade = 0.4f;
        [SerializeField] private Material _acidPoolMaterial;
        [SerializeField] private EventReference _acidPoolSound;

        private static StatusEffectSettings _loaded;
        private static bool _tried;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _loaded = null;
            _tried = false;
        }

        public DamageStatus Bleed => _bleed;
        public DamageStatus Burn => _burn;
        public float TickInterval => _tickInterval;
        public float StaticCapacity => _staticCapacity;
        public float StaticPerHit => _staticPerHit;
        public float ChainDamage => _chainDamage;
        public int ChainTargets => _chainTargets;
        public float ChainRadius => _chainRadius;
        public bool ChainHitsSource => _chainHitsSource;
        public Material ChainMaterial => _chainMaterial;
        public Color ChainColor => _chainColor;
        public float ChainWidth => _chainWidth;
        public float ChainDuration => _chainDuration;
        public float ChainJitter => _chainJitter;
        public EventReference ChainSound => _chainSound;
        public float CorrodeDamageTaken => _corrodeDamageTaken;
        public float CorrodeArmorDamage => _corrodeArmorDamage;
        public float CorrodeDuration => _corrodeDuration;
        public VisualEffect CorrodeEffect => _corrodeEffect;
        public EventReference CorrodeSound => _corrodeSound;
        public int AcidPoolCount => _acidPoolCount;
        public float AcidPoolRadius => _acidPoolRadius;
        public float AcidPoolDuration => _acidPoolDuration;
        public float AcidPoolDamagePerSecond => _acidPoolDamagePerSecond;
        public float AcidPoolFade => _acidPoolFade;
        public Material AcidPoolMaterial => _acidPoolMaterial;
        public EventReference AcidPoolSound => _acidPoolSound;

        public static StatusEffectSettings Instance
        {
            get
            {
                if (!_tried)
                {
                    _tried = true;
                    _loaded = Resources.Load<StatusEffectSettings>(ResourcePath);
                    if (_loaded == null)
                        _loaded = CreateInstance<StatusEffectSettings>();
                }
                return _loaded;
            }
        }
    }
}
