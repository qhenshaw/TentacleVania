using System.Collections.Generic;
using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Core.Feedback;
using TentacleVania.Core.Input;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using TentacleVania.Creature.Movement;
using UnityEngine;

namespace TentacleVania.Abilities.Breach
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(CreatureMotor))]
    public sealed class CreatureBreach : MonoBehaviour
    {
        private enum Phase
        {
            Idle,
            Lunging,
            Recovering,
        }

        [SerializeField] private BreachDefinition _definition;

        private readonly List<Vector3> _points = new();
        private CreatureMotor _motor;
        private CharacterController _controller;
        private Health _health;
        private Phase _phase;
        private float _phaseStarted;
        private Vector3 _start;
        private Vector3 _end;
        private WeakWall _target;

        public static bool ForceUnlocked { get; set; }
        public static int Smashed { get; private set; }
        public bool IsUnlocked => ForceUnlocked || (_definition != null && BossProgress.IsAbilityUnlocked(ContentCatalog.IdOf(_definition)));
        public bool IsBusy => _phase != Phase.Idle;
        public BreachDefinition Definition => _definition;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ForceUnlocked = false;
            Smashed = 0;
        }

        private void Awake()
        {
            _motor = GetComponent<CreatureMotor>();
            _controller = GetComponent<CharacterController>();
            _health = GetComponent<Health>();
        }

        private void OnDisable()
        {
            if (_phase != Phase.Idle)
                End();
        }

        private void Update()
        {
            if (_definition == null)
                return;
            switch (_phase)
            {
                case Phase.Idle:
                    if (GameplayInput.Default.Traverse.Pressed && _motor.InputEnabled && TryBreach())
                        TraversalInput.Claim();
                    break;
                case Phase.Lunging:
                    TickLunge();
                    break;
                case Phase.Recovering:
                    if (Time.time - _phaseStarted >= _definition.RecoverTime)
                        End();
                    break;
            }
        }

        public WeakWall FindTarget()
        {
            var walls = WeakWall.All;
            _points.Clear();
            for (var i = 0; i < walls.Count; i++)
                _points.Add(walls[i].IsBroken ? new Vector3(float.MaxValue, 0f, float.MaxValue) : walls[i].ClosestPoint(transform.position));
            var index = BreachRules.Pick(transform.position, transform.forward, _points, _definition.Range, _definition.MaxAngle);
            return index >= 0 ? walls[index] : null;
        }

        public bool TryBreach()
        {
            if (_definition == null || _phase != Phase.Idle || !IsUnlocked || (_health != null && !_health.IsAlive))
                return false;
            var wall = FindTarget();
            if (wall == null)
                return false;
            _target = wall;
            _phase = Phase.Lunging;
            _phaseStarted = Time.time;
            _start = transform.position;
            var point = wall.ClosestPoint(_start);
            point.y = _start.y;
            var toWall = point - _start;
            var distance = toWall.magnitude;
            var direction = distance > 1e-4f ? toWall / distance : transform.forward;
            _end = _start + direction * Mathf.Max(0f, distance - _definition.StopDistance);
            _motor.LockControl();
            _motor.FaceImmediately(direction);
            if (!_definition.LungeSound.IsNull)
                RuntimeManager.PlayOneShot(_definition.LungeSound, transform.position);
            return true;
        }

        private void TickLunge()
        {
            var t = Mathf.Clamp01((Time.time - _phaseStarted) / _definition.LungeTime);
            var target = Vector3.Lerp(_start, _end, t * t);
            var step = target - transform.position;
            step.y = 0f;
            if (_controller != null && _controller.enabled)
                _controller.Move(step);
            else
                transform.position += step;
            if (t < 1f)
                return;
            if (_target != null)
                _target.Smash();
            Smashed++;
            HitStop.Request(_definition.HitStop);
            ScreenShake.Request(transform.position, _definition.Shake);
            Rumble.Play(_definition.Rumble.x, _definition.Rumble.y, _definition.RumbleTime);
            _phase = Phase.Recovering;
            _phaseStarted = Time.time;
        }

        private void End()
        {
            if (_phase != Phase.Idle)
                _motor.UnlockControl();
            _phase = Phase.Idle;
            _target = null;
        }
    }
}
