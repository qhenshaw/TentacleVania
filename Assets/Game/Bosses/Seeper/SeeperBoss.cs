using System.Collections.Generic;
using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies;
using TentacleVania.Enemies.Traps;
using UnityEngine;
using UnityEngine.AI;

namespace TentacleVania.Bosses.Seeper
{
    public sealed class SeeperBoss : BossEnemy
    {
        public enum VentMode
        {
            Exposed,
            Sinking,
            Submerged,
            Rising,
        }

        [Header("Vent ambush (design: only hittable while exposed)")]
        [SerializeField] private Transform _model;
        [SerializeField] private AcidBurst _eruption;
        [SerializeField, Min(0.1f)] private float _eruptionRadius = 2f;
        [SerializeField, Min(0.4f)] private float _eruptionFuse = 1.2f;
        [SerializeField, Min(0f)] private float _eruptionDamage = 22f;
        [SerializeField, Min(0f)] private float _eruptionStagger;
        [Tooltip("Seconds exposed before sinking again, per boss phase (the last entry repeats).")]
        [SerializeField] private float[] _exposedTime = { 7f, 5f };
        [SerializeField, Min(0.05f)] private float _sinkTime = 0.6f;
        [SerializeField, Min(0f)] private float _sinkDepth = 2.5f;
        [Tooltip("From this boss phase on, a second vent erupts with the body's split-off adds (design phase 2: multi-vent).")]
        [SerializeField, Min(0)] private int _multiVentPhase = 1;
        [SerializeField] private Enemy _splitAdd;
        [SerializeField, Min(0)] private int _splitAddCount = 2;
        [SerializeField, Min(0)] private int _maxAdds = 4;
        [Tooltip("SeeperVent markers this close are its vents; with fewer than 2 it uses a ring around where it spawned.")]
        [SerializeField, Min(1f)] private float _ventRange = 30f;
        [SerializeField, Min(1f)] private float _fallbackRadius = 7f;
        [SerializeField, Min(2)] private int _fallbackCount = 6;
        [SerializeField] private EventReference _sinkSound;

        private readonly List<Vector3> _vents = new(16);
        private Collider[] _colliders;
        private Vector3 _modelHome;
        private VentMode _mode;
        private float _modeStarted;
        private float _modeUntil;
        private int _vent = -1;
        private int _splitVent = -1;
        private float _exposedSpeed;

        public VentMode Mode => _mode;
        public IReadOnlyList<Vector3> Vents => _vents;
        public bool Exposed => _mode == VentMode.Exposed;

        protected override void Awake()
        {
            base.Awake();
            _colliders = GetComponentsInChildren<Collider>(true);
            if (_model != null)
                _modelHome = _model.localPosition;
            if (_eruption != null)
                EnemyManager.PrewarmBursts(_eruption, 2);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (this == null || !isActiveAndEnabled)
                return;
            FindVents();
            SetMode(VentMode.Exposed, Time.time, ExposedTime());
            SetHittable(true);
            if (_model != null)
                _model.localPosition = _modelHome;
        }

        private void FindVents()
        {
            _vents.Clear();
            var range2 = _ventRange * _ventRange;
            foreach (var vent in SeeperVent.All)
                if (vent != null && (vent.transform.position - Position).sqrMagnitude <= range2 && _vents.Count < _vents.Capacity)
                    _vents.Add(vent.transform.position);
            if (_vents.Count >= 2)
                return;
            SeeperRules.Ring(Position, _fallbackRadius, _fallbackCount, _vents);
            for (var i = 0; i < _vents.Count; i++)
                if (NavMesh.SamplePosition(_vents[i], out var hit, 2f, NavMesh.AllAreas))
                    _vents[i] = hit.position;
        }

        private float ExposedTime() =>
            _exposedTime.Length == 0 ? 6f : _exposedTime[Mathf.Clamp(Phase, 0, _exposedTime.Length - 1)];

        private void SetMode(VentMode mode, float now, float duration)
        {
            _mode = mode;
            _modeStarted = now;
            _modeUntil = now + duration;
        }

        protected override bool AttackAllowed(int index) => Exposed && base.AttackAllowed(index);

        protected override Vector3 ChaseDestination(Vector3 targetPosition) => Exposed ? base.ChaseDestination(targetPosition) : Position;

        protected override Vector3 CircleDestination(Vector3 targetPosition, float dt) => Exposed ? base.CircleDestination(targetPosition, dt) : Position;

        protected override bool FaceTarget(float distance) => Exposed && base.FaceTarget(distance);

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            if (!Health.IsAlive || State == EnemyState.Dead || State == EnemyState.Consumed || State == EnemyState.Spawning)
                return;
            if (IsFinishing)
            {
                if (!Exposed)
                {
                    SetHittable(true);
                    PlaceModel(1f);
                    SetMode(VentMode.Exposed, now, float.MaxValue);
                }
                return;
            }
            switch (_mode)
            {
                case VentMode.Exposed:
                    if (now >= _modeUntil && (State == EnemyState.Chase || State == EnemyState.Circle))
                        Sink(now);
                    break;
                case VentMode.Sinking:
                    PlaceModel(1f - Mathf.Clamp01((now - _modeStarted) / _sinkTime));
                    if (now >= _modeUntil)
                        Ambush(now);
                    break;
                case VentMode.Submerged:
                    if (now >= _modeUntil)
                        Erupt(now);
                    break;
                case VentMode.Rising:
                    PlaceModel(Mathf.Clamp01((now - _modeStarted) / _sinkTime));
                    if (now >= _modeUntil)
                    {
                        PlaceModel(1f);
                        BaseSpeed = _exposedSpeed;
                        SetMode(VentMode.Exposed, now, ExposedTime());
                    }
                    break;
            }
            if (!Exposed)
                BaseSpeed = 0f;
        }

        private void Sink(float now)
        {
            _exposedSpeed = BaseSpeed;
            SetHittable(false);
            if (!_sinkSound.IsNull)
                RuntimeManager.PlayOneShot(_sinkSound, Position);
            SetMode(VentMode.Sinking, now, _sinkTime);
        }

        private void Ambush(float now)
        {
            PlaceModel(0f);
            var creature = CombatTargets.Creature;
            var target = creature != null ? creature.transform.position : Position;
            _vent = SeeperRules.NearestVent(_vents, target, _vent);
            _splitVent = Phase >= _multiVentPhase ? SeeperRules.FarthestVent(_vents, _vents[_vent], _vent) : -1;
            Telegraph(_vent);
            Telegraph(_splitVent);
            SetMode(VentMode.Submerged, now, _eruptionFuse);
        }

        private void Telegraph(int vent)
        {
            if (vent < 0 || _eruption == null)
                return;
            var info = DamageInfo.Create(_eruptionDamage, Team.Site, DamageType.Acid, _eruptionStagger);
            info.Status = StatusKind.Corrode;
            info.Attacker = Health;
            EnemyManager.PlaceBurst(_eruption, _vents[vent], _eruptionRadius, _eruptionFuse, info);
        }

        private void Erupt(float now)
        {
            if (_vent >= 0 && Agent.enabled)
                Agent.Warp(_vents[_vent]);
            if (_splitVent >= 0 && _splitAdd != null)
                for (var i = 0; i < _splitAddCount && Adds.Count < _maxAdds; i++)
                    SpawnAdd(_splitAdd, _vents[_splitVent] + Random.insideUnitSphere * 0.8f);
            Surface(now);
        }

        private void Surface(float now)
        {
            SetHittable(true);
            SetMode(VentMode.Rising, now, _sinkTime);
        }

        private void PlaceModel(float exposed)
        {
            if (_model != null)
                _model.localPosition = _modelHome + Vector3.down * (_sinkDepth * (1f - exposed));
        }

        private void SetHittable(bool hittable)
        {
            foreach (var collider in _colliders)
                if (collider != null)
                    collider.enabled = hittable;
        }
    }
}
