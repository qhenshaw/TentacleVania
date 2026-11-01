using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies;
using TentacleVania.Enemies.Traps;
using UnityEngine;
using UnityEngine.AI;

namespace TentacleVania.Bosses.ExcavatorRig
{
    public sealed class ExcavatorBoss : BossEnemy
    {
        public enum DigMode
        {
            Surfaced,
            Sinking,
            Tunnelling,
            Rising,
        }

        private sealed class EngineWeakPoint : IDamageModifier
        {
            public ExcavatorBoss Owner;
            public int Order => 940;

            public void Modify(ref DamageContext context)
            {
                if (!Owner.Surfaced)
                {
                    context.HealthScale = 0f;
                    context.StaggerScale = 0f;
                    return;
                }
                if (Owner._dropped && Owner.FromBehind(context.Info))
                    context.HealthScale *= Owner._engineScale;
            }
        }

        [Header("Burrow & surface (design: hittable while surfaced)")]
        [SerializeField] private Transform _model;
        [SerializeField] private AcidBurst _burst;
        [SerializeField, Min(0.1f)] private float _burstRadius = 2.5f;
        [SerializeField, Min(0.4f)] private float _burstFuse = 1.3f;
        [SerializeField, Min(0f)] private float _burstDamage = 26f;
        [SerializeField, Min(1f)] private float _surfacedTime = 7f;
        [SerializeField, Min(0.05f)] private float _sinkTime = 0.7f;
        [SerializeField, Min(0f)] private float _sinkDepth = 3f;
        [SerializeField] private EventReference _rumbleSound;

        [Header("Collapse pits")]
        [SerializeField] private FireZone _pit;
        [SerializeField, Min(0.1f)] private float _pitRadius = 1.8f;
        [SerializeField, Min(0f)] private float _pitDamagePerSecond = 30f;
        [SerializeField, Min(0)] private int _maxPits = 6;

        [Header("Drop down (phase 2)")]
        [SerializeField, Min(1)] private int _dropPhase = 1;
        [SerializeField, Min(0f)] private float _dropStun = 2.5f;
        [Tooltip("Health damage multiplier on hits from behind once dropped (the exposed engine).")]
        [SerializeField, Min(1f)] private float _engineScale = 1.75f;
        [SerializeField, Range(0f, 180f)] private float _engineArc = 120f;
        [SerializeField] private GameObject _engine;
        [SerializeField] private EventReference _collapseSound;

        private readonly EngineWeakPoint _weakPoint = new();
        private readonly FireZone[] _pits = new FireZone[8];
        private Vector3 _modelHome;
        private DigMode _mode;
        private float _modeStarted;
        private float _modeUntil;
        private float _surfacedSpeed;
        private Vector3 _target;
        private int _pitCount;
        private bool _dropped;

        public DigMode Mode => _mode;
        public bool Surfaced => _mode == DigMode.Surfaced || _mode == DigMode.Rising;
        public bool Dropped => _dropped;
        public int Pits => _pitCount;

        protected override void Awake()
        {
            base.Awake();
            _weakPoint.Owner = this;
            if (_model != null)
                _modelHome = _model.localPosition;
            if (_burst != null)
                EnemyManager.PrewarmBursts(_burst, 1);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Health.Incoming.Add(_weakPoint);
            _dropped = false;
            _pitCount = 0;
            SetMode(DigMode.Surfaced, Time.time, _surfacedTime);
            PlaceModel(1f);
            if (_engine != null)
                _engine.SetActive(false);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Health.Incoming.Remove(_weakPoint);
            ClearPits();
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            ClearPits();
        }

        public bool FromBehind(in DamageInfo info)
        {
            var from = info.Attacker != null ? info.Attacker.transform.position - Position : -info.Direction;
            from.y = 0f;
            return from.sqrMagnitude > 1e-4f && Vector3.Angle(-transform.forward, from) <= _engineArc * 0.5f;
        }

        protected override bool AttackAllowed(int index) => Surfaced && base.AttackAllowed(index);

        protected override Vector3 ChaseDestination(Vector3 targetPosition) => _mode == DigMode.Surfaced ? base.ChaseDestination(targetPosition) : Position;

        protected override Vector3 CircleDestination(Vector3 targetPosition, float dt) => _mode == DigMode.Surfaced ? base.CircleDestination(targetPosition, dt) : Position;

        protected override bool FaceTarget(float distance) => _mode == DigMode.Surfaced && base.FaceTarget(distance);

        private void SetMode(DigMode mode, float now, float duration)
        {
            _mode = mode;
            _modeStarted = now;
            _modeUntil = now + duration;
        }

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            if (!Health.IsAlive || State == EnemyState.Dead || State == EnemyState.Consumed || State == EnemyState.Spawning)
                return;
            if (IsFinishing)
            {
                if (!Surfaced)
                {
                    PlaceModel(1f);
                    SetMode(DigMode.Surfaced, now, float.MaxValue);
                }
                return;
            }
            if (!_dropped && Phase >= _dropPhase && Surfaced)
            {
                Drop(now);
                return;
            }
            switch (_mode)
            {
                case DigMode.Surfaced:
                    if (!_dropped && now >= _modeUntil && (State == EnemyState.Chase || State == EnemyState.Circle))
                        Sink(now);
                    break;
                case DigMode.Sinking:
                    PlaceModel(1f - Mathf.Clamp01((now - _modeStarted) / _sinkTime));
                    if (now >= _modeUntil)
                        Tunnel(now);
                    break;
                case DigMode.Tunnelling:
                    if (now >= _modeUntil)
                        Surface(now);
                    break;
                case DigMode.Rising:
                    PlaceModel(Mathf.Clamp01((now - _modeStarted) / _sinkTime));
                    if (now >= _modeUntil)
                    {
                        PlaceModel(1f);
                        BaseSpeed = _surfacedSpeed;
                        SetMode(DigMode.Surfaced, now, _surfacedTime);
                    }
                    break;
            }
            if (_mode == DigMode.Sinking || _mode == DigMode.Tunnelling)
                BaseSpeed = 0f;
        }

        private void Sink(float now)
        {
            _surfacedSpeed = BaseSpeed;
            if (!_rumbleSound.IsNull)
                RuntimeManager.PlayOneShot(_rumbleSound, Position);
            if (_pit != null && _pitCount < Mathf.Min(_maxPits, _pits.Length))
                _pits[_pitCount++] = EnemyManager.PlaceFire(_pit, Position, Vector3.forward, _pitRadius, 360f, _sinkTime + 0.4f, 600f, _pitDamagePerSecond, Health);
            SetMode(DigMode.Sinking, now, _sinkTime);
        }

        private void Tunnel(float now)
        {
            PlaceModel(0f);
            var creature = CombatTargets.Creature;
            _target = creature != null ? creature.transform.position : Position;
            if (NavMesh.SamplePosition(_target, out var hit, 2f, NavMesh.AllAreas))
                _target = hit.position;
            if (_burst != null)
            {
                var info = DamageInfo.Create(_burstDamage, Team.Site, DamageType.Physical);
                info.Attacker = Health;
                EnemyManager.PlaceBurst(_burst, _target, _burstRadius, _burstFuse, info);
            }
            SetMode(DigMode.Tunnelling, now, _burstFuse);
        }

        private void Surface(float now)
        {
            if (Agent.enabled)
                Agent.Warp(_target);
            SetMode(DigMode.Rising, now, _sinkTime);
        }

        private void Drop(float now)
        {
            _dropped = true;
            ClearPits();
            if (_engine != null)
                _engine.SetActive(true);
            if (!_collapseSound.IsNull)
                RuntimeManager.PlayOneShot(_collapseSound, Position);
            SetMode(DigMode.Surfaced, now, float.MaxValue);
            Stun(_dropStun);
        }

        private void ClearPits()
        {
            for (var i = 0; i < _pits.Length; i++)
            {
                if (_pits[i] != null && _pits[i].isActiveAndEnabled)
                    EnemyManager.ReturnFire(_pits[i]);
                _pits[i] = null;
            }
            _pitCount = 0;
        }

        private void PlaceModel(float exposed)
        {
            if (_model != null)
                _model.localPosition = _modelHome + Vector3.down * (_sinkDepth * (1f - exposed));
        }
    }
}
