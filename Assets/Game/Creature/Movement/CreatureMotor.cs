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

        private CharacterController _controller;
        private DashTimer _dash;
        private Camera _camera;
        private Vector3 _velocity;
        private float _fallSpeed;
        private Vector3 _dashDirection;
        private float _dashRemaining;
        private float _dashBufferedUntil = float.NegativeInfinity;
        private float _invulnerableUntil = float.NegativeInfinity;

        public event Action Dashed;

        public CreatureMotorSettings Settings => _settings;
        public bool InputEnabled { get; set; } = true;
        public bool IsDashing => _dashRemaining > 0f;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;
        public int DashChainCount => _dash.ChainCount;
        public Vector3 Velocity => _velocity;
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

            Vector3 step;
            if (IsDashing)
            {
                var distance = Mathf.Min(_settings.DashSpeed * dt, _dashRemaining);
                _dashRemaining -= distance;
                _velocity = IsDashing ? _dashDirection * _settings.DashSpeed : MoveInput * _settings.MoveSpeed;
                step = _dashDirection * distance;
            }
            else
            {
                _velocity = Vector3.MoveTowards(_velocity, MoveInput * _settings.MoveSpeed, _settings.Acceleration * dt);
                step = _velocity * dt;
            }

            _fallSpeed = _controller.isGrounded ? GroundStick : _fallSpeed + Physics.gravity.y * dt;
            step.y = _fallSpeed * dt;
            _controller.Move(step);

            if (InputEnabled && TryGetFacing(input, out var facing))
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(facing), _settings.TurnSpeed * dt);
        }

        public bool TryDash(Vector3 direction)
        {
            var now = Time.time;
            if (!_dash.TryStart(now))
                return false;
            direction.y = 0f;
            _dashDirection = direction.sqrMagnitude > 1e-4f ? direction.normalized : transform.forward;
            _dashRemaining = _settings.DashDistance;
            _invulnerableUntil = now + _settings.DashInvulnerability;
            Dashed?.Invoke();
            return true;
        }

        private Vector3 CameraForward => _camera != null ? _camera.transform.forward : Vector3.forward;

        private bool TryGetFacing(GameplayInput input, out Vector3 facing)
        {
            if (input.LastDevice == InputDeviceKind.KeyboardMouse && _camera != null)
            {
                if (PlanarInput.TryPointerDirection(_camera.ScreenPointToRay(input.AimPointer), transform.position, out facing))
                    return true;
            }
            else
            {
                var aim = input.AimStick;
                if (aim.sqrMagnitude >= _settings.AimDeadzone * _settings.AimDeadzone)
                {
                    facing = PlanarInput.ToWorld(aim, CameraForward).normalized;
                    return true;
                }
            }
            facing = MoveInput.normalized;
            return MoveInput.sqrMagnitude > 1e-4f;
        }
    }
}
