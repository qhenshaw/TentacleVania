using System;
using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Input;
using TentacleVania.Creature.Movement;
using TentacleVania.Creature.Rig;
using UnityEngine;

namespace TentacleVania.Creature
{
    [RequireComponent(typeof(Health), typeof(CreatureMotor), typeof(CreatureMaw))]
    public sealed class CreatureConsume : MonoBehaviour
    {
        private const float SwallowDepth = 0.4f;
        private const float SwallowScale = 0.15f;
        private const float FallbackHeight = 1f;

        private static readonly Collider[] Candidates = new Collider[16];

        [SerializeField, Min(0f)] private float _reach = 2.2f;
        [SerializeField, Range(0f, 1f)] private float _grabThreshold = Consumable.DefaultGrabThreshold;
        [SerializeField, Min(0.01f)] private float _healDuration = 1.5f;
        [SerializeField, Range(0f, 1f)] private float _damageTakenWhileConsuming = 0.5f;
        [SerializeField] private LayerMask _grabLayers;
        [SerializeField] private EventReference _openSound;
        [SerializeField] private EventReference _chompSound;
        [SerializeField] private EventReference _swallowSound;

        private Health _health;
        private CreatureMotor _motor;
        private CreatureCharge _charge;
        private CreatureMaw _maw;
        private ScaleModifier _armored;

        private Consumable _victim;
        private Vector3 _victimStart;
        private Vector3 _victimScale;
        private Vector3 _victimCentre;
        private float _victimRadius;
        private bool _bitten;
        private float _healRemaining;
        private float _healRate;
        private float _armoredUntil = float.NegativeInfinity;
        private bool _armorApplied;

        public event Action<CreatureConsume, Consumable> Consuming;

        public int Biomass { get; private set; }
        public bool IsRooted => _victim != null;
        public bool IsHealing => _healRemaining > 0f;
        public bool IsArmored => _armorApplied;
        public float GrabThreshold => _grabThreshold;
        public float Reach => _reach;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _motor = GetComponent<CreatureMotor>();
            _charge = GetComponent<CreatureCharge>();
            _maw = GetComponent<CreatureMaw>();
            _armored = new ScaleModifier(100) { HealthScale = _damageTakenWhileConsuming };
        }

        private void OnEnable() => Consumable.GrabThreshold = _grabThreshold;

        private void OnDisable()
        {
            ReleaseVictim(false);
            SetArmored(false);
            _healRemaining = 0f;
        }

        private void Update()
        {
            var now = Time.time;
            if (_victim != null)
                TickGulp();
            else if (_health.IsAlive && _motor.InputEnabled && !_motor.IsDashing && GameplayInput.Default.Grab.Pressed)
                TryGrab();

            if (_healRemaining > 0f && _health.IsAlive)
            {
                var amount = Mathf.Min(_healRemaining, _healRate * Time.deltaTime);
                _healRemaining -= amount;
                _health.Heal(amount);
            }
            SetArmored(now < _armoredUntil && _health.IsAlive);
        }

        public bool TryGrab()
        {
            var target = FindTarget(_grabThreshold);
            return target != null && Grab(target);
        }

        public bool ForceGrab()
        {
            var target = FindTarget(1.01f);
            return target != null && Grab(target);
        }

        private bool Grab(Consumable target)
        {
            if (_victim != null)
                return false;
            _victim = target;
            _bitten = false;
            var victim = target.transform;
            _victimStart = victim.position;
            _victimScale = victim.localScale;
            if (target.TryGetComponent<Collider>(out var body))
            {
                var bounds = body.bounds;
                _victimCentre = bounds.center;
                _victimRadius = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
            }
            else
            {
                _victimCentre = _victimStart + Vector3.up * FallbackHeight;
                _victimRadius = FallbackHeight * 0.5f;
            }
            target.BeginConsume();
            _motor.LockControl();
            var toVictim = _victimStart - transform.position;
            toVictim.y = 0f;
            _motor.FaceImmediately(toVictim);
            var stop = _maw.Settings.MawLungeStop + Mathf.Min(_victimRadius, 0.5f);
            _motor.Lunge(toVictim, Mathf.Max(0f, toVictim.magnitude - stop), _maw.OpenTime / Mathf.Max(0.01f, CreatureMaw.TimeScale));
            _maw.Engulf(_victimCentre, _victimRadius);
            Play(_openSound);

            _healRate = _health.Max * target.HealFraction / _healDuration;
            _healRemaining += _health.Max * target.HealFraction;
            _armoredUntil = Time.time + _healDuration;
            Biomass += target.Biomass;
            if (_charge != null)
                _charge.AddConsume();
            Consuming?.Invoke(this, target);
            return true;
        }

        private Consumable FindTarget(float threshold)
        {
            var origin = transform.position;
            var count = Physics.OverlapSphereNonAlloc(origin, _reach, Candidates, _grabLayers, QueryTriggerInteraction.Collide);
            Consumable best = null;
            var bestScore = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Candidates[i], out var hurtbox) || !hurtbox.TryGetComponent<Consumable>(out var consumable))
                    continue;
                if (!consumable.CanBeGrabbed(threshold))
                    continue;
                var offset = consumable.transform.position - origin;
                offset.y = 0f;
                var score = offset.magnitude * (1f + Vector3.Angle(transform.forward, offset) / 180f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = consumable;
                }
            }
            return best;
        }

        private void TickGulp()
        {
            switch (_maw.Phase)
            {
                case MawPhase.Open:
                case MawPhase.Close:
                    _maw.SetTarget(_victimCentre, _victimRadius);
                    break;
                case MawPhase.Swallow:
                {
                    if (!_bitten)
                    {
                        _bitten = true;
                        Play(_chompSound);
                        _victim.Bite();
                    }
                    var eased = _maw.PhaseT * _maw.PhaseT;
                    var scale = Mathf.Lerp(1f, SwallowScale, eased);
                    var end = _maw.Mouth - Vector3.up * SwallowDepth;
                    var position = Vector3.Lerp(_victimStart, end, eased);
                    var victim = _victim.transform;
                    victim.position = position;
                    victim.localScale = _victimScale * scale;
                    _maw.SetTarget(position + (_victimCentre - _victimStart) * scale, _victimRadius * scale);
                    break;
                }
                default:
                    Play(_swallowSound);
                    ReleaseVictim(true);
                    break;
            }
        }

        private void ReleaseVictim(bool consumed)
        {
            if (_victim == null)
                return;
            var victim = _victim;
            _victim = null;
            _motor.UnlockControl();
            if (consumed)
            {
                victim.transform.localScale = Vector3.zero;
                victim.FinishConsume();
            }
            else
            {
                _maw.Cancel();
            }
        }

        private void Play(EventReference sound)
        {
            if (!sound.IsNull)
                RuntimeManager.PlayOneShot(sound, transform.position);
        }

        private void SetArmored(bool armored)
        {
            if (armored == _armorApplied)
                return;
            _armorApplied = armored;
            if (armored)
                _health.Incoming.Add(_armored);
            else
                _health.Incoming.Remove(_armored);
        }
    }
}
