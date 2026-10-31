using UnityEngine;

namespace TentacleVania.Weapons.Data
{
    [CreateAssetMenu(menuName = "TentacleVania/Weapons/Weapon Handling Settings")]
    public sealed class WeaponHandlingSettings : ScriptableObject
    {
        [Header("Feel")]
        [SerializeField, Min(0f)] private float _heavyThreshold = 0.35f;
        [Tooltip("Seconds the weapon takes to pull back into the heavy pose once charging starts; a release before then fires when it completes. Holding never fires by itself.")]
        [SerializeField, Min(0f)] private float _heavyWindUp = 0.5f;
        [SerializeField, Min(0f)] private float _inputBuffer = 0.15f;
        [SerializeField, Min(0f)] private float _comboReset = 0.6f;
        [SerializeField, Min(0f)] private float _dashAttackWindow = 0.12f;
        [SerializeField, Range(0f, 1f)] private float _attackMoveSpeed = 0.4f;

        [Header("Soft-lock")]
        [SerializeField, Min(0f)] private float _softLockRange = 6f;
        [SerializeField, Range(0f, 360f)] private float _softLockCone = 90f;
        [SerializeField, Range(0f, 360f)] private float _aimLockCone = 30f;

        public float HeavyThreshold => _heavyThreshold;
        public float HeavyWindUp => _heavyWindUp;
        public float InputBuffer => _inputBuffer;
        public float ComboReset => _comboReset;
        public float DashAttackWindow => _dashAttackWindow;
        public float AttackMoveSpeed => _attackMoveSpeed;
        public float SoftLockRange => _softLockRange;
        public float SoftLockCone => _softLockCone;
        public float AimLockCone => _aimLockCone;
    }
}
