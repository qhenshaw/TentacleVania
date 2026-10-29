using UnityEngine;

namespace TentacleVania.Creature.Movement
{
    [CreateAssetMenu(menuName = "TentacleVania/Creature/Motor Settings")]
    public sealed class CreatureMotorSettings : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float _moveSpeed = 7f;
        [SerializeField, Min(0f)] private float _acceleration = 120f;
        [SerializeField, Min(0f)] private float _turnSpeed = 1440f;
        [SerializeField, Range(0f, 1f)] private float _aimDeadzone = 0.25f;

        [Header("Dash")]
        [SerializeField, Min(0f)] private float _dashDistance = 4f;
        [SerializeField, Min(0.01f)] private float _dashDuration = 0.15f;
        [SerializeField, Min(0f)] private float _dashCooldown = 0.3f;
        [SerializeField, Min(1)] private int _dashChain = 2;
        [SerializeField, Min(0f)] private float _dashLockout = 0.8f;
        [SerializeField, Min(0f)] private float _dashInvulnerability = 0.2f;
        [SerializeField, Min(0f)] private float _dashBuffer = 0.1f;

        public float MoveSpeed => _moveSpeed;
        public float Acceleration => _acceleration;
        public float TurnSpeed => _turnSpeed;
        public float AimDeadzone => _aimDeadzone;
        public float DashDistance => _dashDistance;
        public float DashDuration => _dashDuration;
        public float DashSpeed => _dashDistance / _dashDuration;
        public float DashCooldown => _dashCooldown;
        public int DashChain => _dashChain;
        public float DashLockout => _dashLockout;
        public float DashInvulnerability => _dashInvulnerability;
        public float DashBuffer => _dashBuffer;
    }
}
