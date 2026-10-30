using System;
using TentacleVania.Core.Input;
using UnityEngine;

namespace TentacleVania.Creature.Movement
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CreatureMotor : MonoBehaviour
    {
        private const float GroundStick = -2f;

        [SerializeField] private CreatureMotorSettings _settings;
        [SerializeField] private LayerMask _dashPassLayers;

        private CharacterController _controller;
        private DashTimer _dash;
        private Camera _camera;
        private Vector3 _velocity;
        private float _fallSpeed;
        private Vector3 _dashDirection;
        private float _dashRemaining;
        private float _dashBufferedUntil = float.NegativeInfinity;
        private float _invulnerableUntil = float.NegativeInfinity;

        private Vector3 _lungeDirection;
        private float _lungeRemaining;
        private float _lungeSpeed;

        public event Action Dashed;

        public CreatureMotorSettings Settings => _settings;
        private int _controlLocks;

        public bool InputEnabled => _controlLocks == 0;

        public void LockControl() => _controlLocks++;

        public void UnlockControl() => _controlLocks = Mathf.Max(0, _controlLocks - 1);
        public float SpeedMultiplier { get; set; } = 1f;
        public bool DashLocked { get; set; }
        public bool FacingLocked { get; set; }
        public float LastDashEndTime { get; private set; } = float.NegativeInfinity;
        public bool IsDashing => _dashRemaining > 0f;
        public bool IsLunging => _lungeRemaining > 0f;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;
        public int DashChainCount => _dash.ChainCount;
        public Vector3 Velocity => _velocity;
        public Vector3 DashDirection => _dashDirection;
        public Vector3 MoveInput { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _dash = new(_settings);
        }

        private void Update()
        {
            var input = GameplayInput.Default;
            var now = Time.time;
            var dt = Time.deltaTime;
            _camera = _camera != null ? _camera : Camera.main;

            MoveInput = InputEnabled ? PlanarInput.ToWorld(input.Move, CameraForward) : Vector3.zero;

            if (InputEnabled && input.Dash.Pressed)
                _dashBufferedUntil = now + _settings.DashBuffer;
            if (now <= _dashBufferedUntil && TryDash(MoveInput))
                _dashBufferedUntil = float.NegativeInfinity;

            var walkSpeed = _settings.MoveSpeed * SpeedMultiplier;
            Vector3 step;
            if (IsLunging)
            {
                var distance = Mathf.Min(_lungeSpeed * dt, _lungeRemaining);
                _lungeRemaining -= distance;
                _velocity = IsLunging ? _lungeDirection * _lungeSpeed : MoveInput * walkSpeed;
                step = _lungeDirection * distance;
            }
            else if (IsDashing)
            {
                var distance = Mathf.Min(_settings.DashSpeed * dt, _dashRemaining);
                _dashRemaining -= distance;
                _velocity = IsDashing ? _dashDirection * _settings.DashSpeed : MoveInput * walkSpeed;
                step = _dashDirection * distance;
                if (!IsDashing)
                    LastDashEndTime = now;
            }
            else
            {
                _velocity = Vector3.MoveTowards(_velocity, MoveInput * walkSpeed, _settings.Acceleration * dt);
                step = _velocity * dt;
            }

            _controller.excludeLayers = IsDashing || IsLunging ? _dashPassLayers : 0;
            _fallSpeed = _controller.isGrounded ? GroundStick : _fallSpeed + Physics.gravity.y * dt;
            step.y = _fallSpeed * dt;
            _controller.Move(step);

            if (InputEnabled && !FacingLocked && TryGetFacing(input, out var facing))
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(facing), _settings.TurnSpeed * dt);
        }

        public bool TryDash(Vector3 direction)
        {
            var now = Time.time;
            if (DashLocked || !_dash.TryStart(now))
                return false;
            _lungeRemaining = 0f;
            direction.y = 0f;
            _dashDirection = direction.sqrMagnitude > 1e-4f ? direction.normalized : transform.forward;
            _dashRemaining = _settings.DashDistance;
            _invulnerableUntil = now + _settings.DashInvulnerability;
            Dashed?.Invoke();
            return true;
        }

        public void Lunge(Vector3 direction, float distance, float duration)
        {
            direction.y = 0f;
            if (distance <= 0f || duration <= 0f || direction.sqrMagnitude < 1e-4f)
                return;
            if (IsDashing)
            {
                _dashRemaining = 0f;
                LastDashEndTime = Time.time;
            }
            _lungeDirection = direction.normalized;
            _lungeRemaining = distance;
            _lungeSpeed = distance / duration;
        }

        public void FaceImmediately(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.LookRotation(direction);
        }

        public bool TryGetAimDirection(out Vector3 direction)
        {
            var input = GameplayInput.Default;
            if (input.LastDevice == InputDeviceKind.KeyboardMouse && _camera != null)
                return PlanarInput.TryPointerDirection(_camera.ScreenPointToRay(input.AimPointer), transform.position, out direction);
            var aim = input.AimStick;
            if (aim.sqrMagnitude >= _settings.AimDeadzone * _settings.AimDeadzone)
            {
                direction = PlanarInput.ToWorld(aim, CameraForward).normalized;
                return true;
            }
            direction = default;
            return false;
        }

        private Vector3 CameraForward => _camera != null ? _camera.transform.forward : Vector3.forward;

        private bool TryGetFacing(GameplayInput input, out Vector3 facing)
        {
            if (TryGetAimDirection(out facing))
                return true;
            facing = MoveInput.normalized;
            return MoveInput.sqrMagnitude > 1e-4f;
        }
    }
}
