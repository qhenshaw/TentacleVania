using System;
using System.Collections.Generic;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Input;
using TentacleVania.Creature.Movement;
using TentacleVania.Weapons.Combo;
using TentacleVania.Weapons.Data;
using UnityEngine;

namespace TentacleVania.Weapons
{
    public enum AttackKind
    {
        Combo,
        Heavy,
        DashAttack,
    }

    [RequireComponent(typeof(CreatureMotor), typeof(Health))]
    public sealed class WeaponController : MonoBehaviour
    {
        public const int Slots = 2;

        private static readonly RaycastHit[] SweepHits = new RaycastHit[16];
        private static readonly Collider[] OverlapHits = new Collider[16];

        [SerializeField] private WeaponDefinition _weapon1;
        [SerializeField] private WeaponDefinition _weapon2;
        [SerializeField] private LayerMask _hitLayers;

        [Header("Feel")]
        [SerializeField, Min(0f)] private float _heavyThreshold = 0.35f;
        [SerializeField, Min(0f)] private float _maxHold = 1.5f;
        [SerializeField, Min(0f)] private float _inputBuffer = 0.15f;
        [SerializeField, Min(0f)] private float _comboReset = 0.6f;
        [SerializeField, Min(0f)] private float _dashAttackWindow = 0.12f;
        [SerializeField, Range(0f, 1f)] private float _attackMoveSpeed = 0.4f;

        [Header("Soft-lock")]
        [SerializeField, Min(0f)] private float _softLockRange = 6f;
        [SerializeField, Range(0f, 360f)] private float _softLockCone = 90f;
        [SerializeField, Range(0f, 360f)] private float _aimLockCone = 30f;

        private readonly List<Health> _hitThisMove = new(8);
        private readonly ChargeButton[] _buttons = new ChargeButton[Slots];
        private readonly WeaponDefinition[] _weapons = new WeaponDefinition[Slots];

        private CreatureMotor _motor;
        private Health _health;
        private ComboCounter _combo;

        private WeaponMove _move;
        private float _moveStarted;
        private Vector3 _moveForward;
        private bool _finisher;
        private float _lastSweepT;
        private bool _areaDone;

        private bool _hasRequest;
        private int _requestSlot;
        private AttackKind _requestKind;
        private float _requestTime;

        public event Action<WeaponController> AttackStarted;

        public int ActiveSlot { get; private set; } = -1;
        public AttackKind ActiveKind { get; private set; }
        public int ActiveStep { get; private set; } = -1;
        public WeaponMove ActiveMove => _move;
        public Vector3 ActiveForward => _moveForward;
        public float ActiveElapsed => Time.time - _moveStarted;
        public bool IsAttacking => _move != null;
        public MovePhase Phase => _move != null ? MoveShapes.Phase(_move, ActiveElapsed) : MovePhase.Done;
        public int NextComboStep => _combo.Peek(Time.time);

        public WeaponDefinition Weapon(int slot) => _weapons[slot];

        public bool IsCharging(int slot) => _buttons[slot].IsCharging(Time.time);

        public float Charge(int slot) => _buttons[slot].Charge(Time.time);

        private void Awake()
        {
            _motor = GetComponent<CreatureMotor>();
            _health = GetComponent<Health>();
            _combo = new(WeaponDefinition.ComboSteps, _comboReset);
            for (var i = 0; i < Slots; i++)
                _buttons[i] = new(_heavyThreshold, _maxHold);
            Equip(0, _weapon1);
            Equip(1, _weapon2);
        }

        private void OnEnable() => _motor.Dashed += OnDashed;

        private void OnDisable()
        {
            _motor.Dashed -= OnDashed;
            EndMove(Time.time);
        }

        public void Equip(int slot, WeaponDefinition weapon)
        {
            _weapons[slot] = weapon;
            _buttons[slot].Cancel();
        }

        private void Update()
        {
            var now = Time.time;
            if (!_health.IsAlive || !_motor.InputEnabled)
            {
                EndMove(now);
                ApplyMotorState(now);
                return;
            }

            var input = GameplayInput.Default;
            ReadButton(0, input.Weapon1, now);
            ReadButton(1, input.Weapon2, now);

            if (_move != null)
                TickMove(now);

            if (_hasRequest && now - _requestTime > _inputBuffer && _move != null)
                _hasRequest = false;
            if (_hasRequest && _move == null)
            {
                _hasRequest = false;
                StartMove(_requestSlot, _requestKind, now);
            }

            ApplyMotorState(now);
        }

        private void ReadButton(int slot, ButtonInput button, float now)
        {
            if (_weapons[slot] == null)
                return;
            if (button.Pressed && (_motor.IsDashing || now - _motor.LastDashEndTime <= _dashAttackWindow))
            {
                _buttons[slot].Cancel();
                Request(slot, AttackKind.DashAttack, now);
                return;
            }
            switch (_buttons[slot].Update(button.Pressed, button.Held, now))
            {
                case ChargeEvent.Tap:
                    Request(slot, AttackKind.Combo, now);
                    break;
                case ChargeEvent.Heavy:
                    Request(slot, AttackKind.Heavy, now);
                    break;
            }
        }

        private void Request(int slot, AttackKind kind, float now)
        {
            _hasRequest = true;
            _requestSlot = slot;
            _requestKind = kind;
            _requestTime = now;
        }

        private void StartMove(int slot, AttackKind kind, float now)
        {
            var weapon = _weapons[slot];
            _finisher = false;
            ActiveStep = -1;
            switch (kind)
            {
                case AttackKind.Heavy:
                    _move = weapon.Heavy;
                    _combo.Reset();
                    break;
                case AttackKind.DashAttack:
                    _move = weapon.DashAttack;
                    break;
                default:
                    ActiveStep = _combo.Advance(now);
                    _finisher = _combo.IsFinisher(ActiveStep);
                    _move = weapon.Step(ActiveStep);
                    break;
            }

            ActiveSlot = slot;
            ActiveKind = kind;
            _moveStarted = now;
            _moveForward = ChooseDirection();
            _motor.FaceImmediately(_moveForward);
            _hitThisMove.Clear();
            _lastSweepT = 0f;
            _areaDone = false;
            if (kind == AttackKind.DashAttack)
                _motor.Lunge(_moveForward, _move.LungeDistance, _move.WindUp + _move.Strike);
            AttackStarted?.Invoke(this);
        }

        private Vector3 ChooseDirection()
        {
            var origin = transform.position;
            var aiming = _motor.TryGetAimDirection(out var aim);
            var forward = aiming ? aim : (_motor.MoveInput.sqrMagnitude > 1e-4f ? _motor.MoveInput.normalized : transform.forward);
            var cone = aiming ? _aimLockCone : _softLockCone;
            if (SoftLock.TryPick(origin, forward, _softLockRange, cone, _hitLayers, _health, out var locked))
                return locked;
            forward.y = 0f;
            return forward.sqrMagnitude > 1e-4f ? forward.normalized : transform.forward;
        }

        private void TickMove(float now)
        {
            var elapsed = now - _moveStarted;
            switch (MoveShapes.Phase(_move, elapsed))
            {
                case MovePhase.Strike:
                    ResolveHits(MoveShapes.StrikeProgress(_move, elapsed));
                    break;
                case MovePhase.Recovery:
                    if (_lastSweepT < 1f)
                        ResolveHits(1f);
                    break;
                case MovePhase.Done:
                    EndMove(now);
                    break;
            }
        }

        private void ResolveHits(float t)
        {
            var origin = transform.position + Vector3.up * _move.Height;
            switch (_move.Shape)
            {
                case MoveShape.Sweep:
                    var steps = MoveShapes.SweepSteps(_move, _lastSweepT, t);
                    for (var step = 1; step <= steps; step++)
                    {
                        var direction = MoveShapes.SweepDirection(_moveForward, _move, Mathf.Lerp(_lastSweepT, t, (float)step / steps));
                        var count = Physics.SphereCastNonAlloc(origin, _move.Radius, direction, SweepHits, _move.Reach, _hitLayers, QueryTriggerInteraction.Collide);
                        for (var i = 0; i < count; i++)
                            Deliver(SweepHits[i].collider, direction);
                    }
                    break;
                case MoveShape.Area:
                    if (!_areaDone)
                    {
                        _areaDone = true;
                        var center = MoveShapes.AreaCenter(transform.position, _moveForward, _move);
                        var count = Physics.OverlapCapsuleNonAlloc(center, center + Vector3.up * 2f, _move.Radius, OverlapHits, _hitLayers, QueryTriggerInteraction.Collide);
                        for (var i = 0; i < count; i++)
                        {
                            var collider = OverlapHits[i];
                            if (MoveShapes.InArea(center, _moveForward, _move, collider.ClosestPoint(center + Vector3.up)))
                                Deliver(collider, collider.transform.position - center);
                        }
                    }
                    break;
                case MoveShape.Body:
                    var hits = Physics.OverlapSphereNonAlloc(origin, _move.Radius, OverlapHits, _hitLayers, QueryTriggerInteraction.Collide);
                    for (var i = 0; i < hits; i++)
                        Deliver(OverlapHits[i], _moveForward);
                    break;
            }
            _lastSweepT = t;
        }

        private void Deliver(Collider collider, Vector3 direction)
        {
            if (!Hurtbox.TryGet(collider, out var hurtbox))
                return;
            var target = hurtbox.Health;
            if (target == _health || _hitThisMove.Contains(target))
                return;
            var weapon = _weapons[ActiveSlot];
            var info = DamageInfo.Create(weapon.Damage * _move.DamageScale, Team.Creature, weapon.DamageType, weapon.Stagger * _move.StaggerScale);
            info.ArmorDamageScale = weapon.ArmorDamageScale * _move.ArmorDamageScale;
            info.Flags = _move.Flags | KindFlags();
            info.Attacker = _health;
            info.Point = collider.ClosestPoint(transform.position + Vector3.up * _move.Height);
            direction.y = 0f;
            info.Direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : _moveForward;
            if (target.TakeDamage(info).Outcome != DamageOutcome.Ignored)
                _hitThisMove.Add(target);
        }

        private HitFlags KindFlags() => ActiveKind switch
        {
            AttackKind.Heavy => HitFlags.Heavy,
            AttackKind.DashAttack => HitFlags.DashAttack,
            _ => _finisher ? HitFlags.Finisher : HitFlags.None,
        };

        private void EndMove(float now)
        {
            if (_move == null)
                return;
            if (ActiveKind != AttackKind.DashAttack)
                _combo.AttackEnded(now);
            if (_finisher)
                _combo.Reset();
            _move = null;
            ActiveSlot = -1;
            ActiveStep = -1;
        }

        private void OnDashed()
        {
            if (_move != null && Phase == MovePhase.Recovery)
                EndMove(Time.time);
        }

        private void ApplyMotorState(float now)
        {
            var phase = Phase;
            var charging = _buttons[0].IsCharging(now) || _buttons[1].IsCharging(now);
            _motor.DashLocked = phase is MovePhase.WindUp or MovePhase.Strike;
            _motor.FacingLocked = _move != null;
            _motor.SpeedMultiplier = _move != null || charging ? _attackMoveSpeed : 1f;
        }
    }
}
