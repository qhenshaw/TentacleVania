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
