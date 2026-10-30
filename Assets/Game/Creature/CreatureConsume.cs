using System;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Characters;
using TentacleVania.Core.Input;
using TentacleVania.Creature.Movement;
using TentacleVania.Creature.Rig;
using UnityEngine;
using UnityEngine.Rendering;

namespace TentacleVania.Creature
{
    [RequireComponent(typeof(Health), typeof(CreatureMotor))]
    public sealed class CreatureConsume : MonoBehaviour
    {
        private const int Joints = 6;

        private static readonly Collider[] Candidates = new Collider[16];

        [SerializeField, Min(0f)] private float _reach = 2.2f;
        [SerializeField, Range(0f, 1f)] private float _grabThreshold = Consumable.DefaultGrabThreshold;
        [SerializeField, Min(0f)] private float _rootDuration = 0.4f;
        [SerializeField, Min(0.01f)] private float _healDuration = 1.5f;
        [SerializeField, Range(0f, 1f)] private float _damageTakenWhileConsuming = 0.5f;
        [SerializeField] private Vector3 _mawOffset = new(0f, 0.45f, 0.35f);
        [SerializeField] private LayerMask _grabLayers;
        [SerializeField] private Material _tentacleMaterial;

        private readonly LineRenderer[] _lines = new LineRenderer[2];
        private readonly Vector3[] _joints = new Vector3[Joints];

        private Health _health;
        private CreatureMotor _motor;
        private CreatureCharge _charge;
        private ScaleModifier _armored;

        private Consumable _victim;
        private Vector3 _victimStart;
        private Vector3 _victimScale;
        private float _grabbedAt = float.NegativeInfinity;
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

        private void Awake()
        {
            _health = GetComponent<Health>();
            _motor = GetComponent<CreatureMotor>();
            _charge = GetComponent<CreatureCharge>();
            _armored = new ScaleModifier(100) { HealthScale = _damageTakenWhileConsuming };
            var width = new AnimationCurve(new Keyframe(0f, 0.12f), new Keyframe(1f, 0.04f));
            for (var i = 0; i < _lines.Length; i++)
            {
                var line = new GameObject($"ConsumeTentacle {i + 1}").AddComponent<LineRenderer>();
                line.transform.SetParent(transform, false);
                line.useWorldSpace = true;
                line.positionCount = Joints;
                line.widthCurve = width;
                line.numCornerVertices = 3;
                line.numCapVertices = 3;
                line.sharedMaterial = _tentacleMaterial;
                line.shadowCastingMode = ShadowCastingMode.On;
                line.generateLightingData = true;
                line.enabled = false;
                _lines[i] = line;
                if (TryGetComponent<CharacterMaterials>(out var materials))
                    materials.Register(line);
            }
        }

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
                TickGulp(now);
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
            var target = FindTarget();
            if (target == null)
                return false;
            _victim = target;
            _grabbedAt = Time.time;
            _victimStart = target.transform.position;
            _victimScale = target.transform.localScale;
            target.BeginConsume();
            _motor.InputEnabled = false;
            _motor.FaceImmediately(target.transform.position - transform.position);
            _healRate = _health.Max * target.HealFraction / _healDuration;
            _healRemaining += _health.Max * target.HealFraction;
            _armoredUntil = Time.time + _healDuration;
            Biomass += target.Biomass;
            if (_charge != null)
                _charge.AddConsume();
            Consuming?.Invoke(this, target);
            return true;
        }

        private Consumable FindTarget()
        {
            var origin = transform.position;
            var count = Physics.OverlapSphereNonAlloc(origin, _reach, Candidates, _grabLayers, QueryTriggerInteraction.Collide);
            Consumable best = null;
            var bestScore = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Candidates[i], out var hurtbox) || !hurtbox.TryGetComponent<Consumable>(out var consumable))
                    continue;
                if (!consumable.CanBeGrabbed(_grabThreshold))
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

        private void TickGulp(float now)
        {
            var t = Mathf.Clamp01((now - _grabbedAt) / _rootDuration);
            var maw = transform.TransformPoint(_mawOffset);
            var eased = t * t;
            _victim.transform.position = Vector3.Lerp(_victimStart, maw - Vector3.up * 0.4f, eased);
            _victim.transform.localScale = _victimScale * Mathf.Lerp(1f, 0.15f, eased);
            DrawTentacles(maw, _victim.transform.position + Vector3.up * 0.9f * (1f - eased));
            if (t >= 1f)
                ReleaseVictim(true);
        }

        private void DrawTentacles(Vector3 maw, Vector3 victim)
        {
            for (var i = 0; i < _lines.Length; i++)
            {
                var side = i == 0 ? -1f : 1f;
                var root = maw + transform.right * (0.15f * side);
                var tip = victim + transform.right * (0.25f * side);
                ChainSolver.SeedArch(_joints, root, tip, Vector3.up, 0.35f);
                _lines[i].SetPositions(_joints);
                _lines[i].enabled = true;
            }
        }

        private void ReleaseVictim(bool consumed)
        {
            foreach (var line in _lines)
                line.enabled = false;
            if (_victim == null)
                return;
            var victim = _victim;
            _victim = null;
            _motor.InputEnabled = _health.IsAlive;
            if (consumed)
            {
                victim.transform.localScale = Vector3.zero;
                victim.FinishConsume();
            }
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
