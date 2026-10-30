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
    public sealed class CreatureConsume : MonoBehaviour, IChainSource
    {
        private const int Joints = Chains.Joints;
        private const float RootWidth = 0.12f;
        private const float TipWidth = 0.04f;
        private const int TentacleCount = 3;
        private const float ReachShare = 0.3f;
        private const float WrapRadius = 0.32f;
        private const float WrapTurns = 0.75f;
        private const float WrapHeight = 0.3f;
        private const float MawArch = 0.35f;

        private static readonly Collider[] Candidates = new Collider[16];

        [SerializeField, Min(0f)] private float _reach = 2.2f;
        [SerializeField, Range(0f, 1f)] private float _grabThreshold = Consumable.DefaultGrabThreshold;
        [SerializeField, Min(0f)] private float _rootDuration = 0.4f;
        [SerializeField, Min(0.01f)] private float _healDuration = 1.5f;
        [SerializeField, Range(0f, 1f)] private float _damageTakenWhileConsuming = 0.5f;
        [SerializeField] private Vector3 _mawOffset = new(0f, 0.45f, 0.35f);
        [SerializeField] private LayerMask _grabLayers;
        [SerializeField] private Material _tentacleMaterial;

        [SerializeField] private SpringChainSettings _spring = new()
        {
            PoseStiffness = 24f,
            TipLooseness = 0.6f,
            Damping = 0.15f,
            Gravity = 2f,
            Iterations = 3,
        };

        private readonly LineRenderer[] _lines = new LineRenderer[TentacleCount];
        private readonly SpringChain[] _chains = new SpringChain[TentacleCount];
        private readonly Vector3[] _pose = new Vector3[Joints];

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
            var width = new AnimationCurve(new Keyframe(0f, RootWidth), new Keyframe(1f, TipWidth));
            for (var i = 0; i < _lines.Length; i++)
            {
                _chains[i] = new SpringChain(Joints);
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

        public int ChainOrder => 20;
        public int ChainCount => _chains.Length;

        public void WriteChain(int chain, Span<Vector4> joints)
        {
            if (IsRooted)
                Chains.Write(joints, _chains[chain].Positions, RootWidth, TipWidth);
            else
                Chains.Collapse(joints, transform.TransformPoint(_mawOffset));
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
            var maw = transform.TransformPoint(_mawOffset);
            foreach (var chain in _chains)
                chain.Collapse(maw);
            target.BeginConsume();
            _motor.LockControl();
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
            DrawTentacles(maw, _victim.transform.position + Vector3.up * 0.9f * (1f - eased), t, 1f - eased);
            if (t >= 1f)
                ReleaseVictim(true);
        }

        private void DrawTentacles(Vector3 maw, Vector3 victim, float t, float size)
        {
            var reach = Mathf.Clamp01(t / ReachShare);
            reach = reach * reach * (3f - 2f * reach);
            var spin = t * WrapTurns * 360f;
            for (var i = 0; i < _chains.Length; i++)
            {
                var side = (i - (_chains.Length - 1) * 0.5f) * 0.15f;
                var root = maw + transform.right * side;
                var angle = 360f / _chains.Length * i + spin;
                var around = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward * (WrapRadius * Mathf.Max(size, 0.2f));
                var height = (i - (_chains.Length - 1) * 0.5f) * WrapHeight * size;
                var wrap = victim + around + Vector3.up * height;
                var tip = Vector3.Lerp(root, wrap, reach);
                var bone = Mathf.Max(Vector3.Distance(root, tip) * 1.1f / (Joints - 1), 0.02f);
                ChainSolver.SeedArch(_pose, root, tip, Vector3.up, MawArch);
                _chains[i].Step(_spring, Time.deltaTime, _pose, bone, true);
                _lines[i].SetPositions(_chains[i].Positions);
                _lines[i].enabled = Chains.ShowPlaceholderLines;
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
            _motor.UnlockControl();
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
