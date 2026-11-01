using FMODUnity;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies;
using TentacleVania.Enemies.Traps;
using TentacleVania.Enemies.Types;
using UnityEngine;

namespace TentacleVania.Bosses.ReactorAI
{
    public sealed class ReactorBoss : BossEnemy
    {
        private sealed class ReactorShield : IDamageModifier
        {
            public ReactorBoss Owner;
            public int Order => 950;

            public void Modify(ref DamageContext context)
            {
                if (Owner.Exposed)
                    return;
                context.HealthScale = 0f;
                context.ArmorScale = 0f;
                context.StaggerScale = 0f;
            }
        }

        public const int RodCount = 4;

        [Header("Control rods (design: each rod pulled exposes the core and escalates the room)")]
        [SerializeField] private BossPart _rodPrefab;
        [SerializeField, Min(1f)] private float _rodRing = 4.5f;
        [SerializeField, Min(0.5f)] private float _exposeTime = 6f;
        [SerializeField] private EventReference _pulledSound;
        [SerializeField] private EventReference _restoredSound;

        [Header("Repair drones (they reinsert pulled rods)")]
        [SerializeField] private TaskDroneEnemy _repairDrone;
        [SerializeField, Min(0.5f)] private float _repairRange = 1.6f;
        [SerializeField, Min(0.5f)] private float _repairTime = 3f;
        [SerializeField, Min(1f)] private float _fastDrones = 1.6f;

        [Header("Radiation")]
        [SerializeField] private FireZone _radiation;
        [SerializeField, Min(1f)] private float _burstInterval = 5f;
        [SerializeField, Min(1)] private int _burstCount = 3;
        [SerializeField, Min(0.1f)] private float _burstRadius = 2.5f;
        [SerializeField, Min(1f)] private float _burstSpread = 9f;
        [SerializeField, Min(0f)] private float _burstDamagePerSecond = 18f;
        [SerializeField, Min(1f)] private float _floorPulseInterval = 6f;
        [SerializeField, Min(1f)] private float _floorPulseRadius = 16f;
        [SerializeField, Min(0f)] private float _floorPulseDamagePerSecond = 10f;

        [Header("Steam Jets")]
        [SerializeField] private string _steamAttack = "Steam Jets";
        [SerializeField, Min(0)] private int _steamRods = 2;

        private readonly ReactorShield _shield = new();
        private readonly BossPart[] _rods = new BossPart[RodCount];
        private readonly TaskDroneEnemy[] _drones = new TaskDroneEnemy[RodCount];
        private readonly float[] _repairProgress = new float[RodCount];
        private float _exposedUntil;
        private float _nextBurst;
        private float _nextPulse;

        public int RodsPulled
        {
            get
            {
                var pulled = 0;
                foreach (var rod in _rods)
                    if (rod != null && !rod.Inserted)
                        pulled++;
                return pulled;
            }
        }

        public bool Exposed => RodsPulled >= RodCount || Time.time < _exposedUntil || IsFinishing;
        public BossPart Rod(int index) => _rods[index];

        protected override void Awake()
        {
            base.Awake();
            _shield.Owner = this;
            BaseSpeed = 0f;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Health.Incoming.Add(_shield);
            _exposedUntil = 0f;
            _nextBurst = float.MaxValue;
            _nextPulse = float.MaxValue;
            if (_rodPrefab == null)
                return;
            for (var i = 0; i < RodCount; i++)
            {
                var spot = Position + Quaternion.Euler(0f, 45f + i * 90f, 0f) * Vector3.forward * _rodRing;
                _rods[i] = Instantiate(_rodPrefab, spot, Quaternion.identity);
                _rods[i].Pulled += OnPulled;
                _repairProgress[i] = 0f;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Health.Incoming.Remove(_shield);
            ClearRods();
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            ClearRods();
        }

        private void ClearRods()
        {
            for (var i = 0; i < RodCount; i++)
            {
                if (_rods[i] != null)
                {
                    _rods[i].Pulled -= OnPulled;
                    Destroy(_rods[i].gameObject);
                }
                _rods[i] = null;
                _drones[i] = null;
            }
        }

        protected override Vector3 ChaseDestination(Vector3 targetPosition) => Position;

        protected override Vector3 CircleDestination(Vector3 targetPosition, float dt) => Position;

        protected override bool AttackAllowed(int index) =>
            (Definition.Attacks[index].Name != _steamAttack || RodsPulled >= _steamRods) && base.AttackAllowed(index);

        private void OnPulled(BossPart rod)
        {
            _exposedUntil = Time.time + _exposeTime;
            if (!_pulledSound.IsNull)
                RuntimeManager.PlayOneShot(_pulledSound, Position);
            var index = System.Array.IndexOf(_rods, rod);
            if (index < 0 || _repairDrone == null || IsFinishing)
                return;
            var drone = (TaskDroneEnemy)SpawnAdd(_repairDrone, Position + (rod.transform.position - Position) * 0.4f);
            drone.Goal = rod.transform;
            _drones[index] = drone;
            _repairProgress[index] = 0f;
        }

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            BaseSpeed = 0f;
            if (!Health.IsAlive || State == EnemyState.Dead || State == EnemyState.Consumed || State == EnemyState.Spawning || IsFinishing)
                return;
            var pulled = RodsPulled;
            TickRepairs(dt, pulled);
            if (pulled >= 1 && _radiation != null)
            {
                if (_nextBurst == float.MaxValue)
                    _nextBurst = now + 1f;
                if (now >= _nextBurst)
                {
                    _nextBurst = now + _burstInterval;
                    for (var i = 0; i < _burstCount; i++)
                    {
                        var spot = Position + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(_rodRing + 1f, _burstSpread);
                        EnemyManager.PlaceFire(_radiation, spot, Vector3.forward, _burstRadius, 360f, 1f, 1.5f, _burstDamagePerSecond, Health);
                    }
                }
            }
            if (pulled >= RodCount && _radiation != null)
            {
                if (_nextPulse == float.MaxValue)
                    _nextPulse = now + 2f;
                if (now >= _nextPulse)
                {
                    _nextPulse = now + _floorPulseInterval;
                    EnemyManager.PlaceFire(_radiation, Position, Vector3.forward, _floorPulseRadius, 360f, 1.5f, 1f, _floorPulseDamagePerSecond, Health);
                }
            }
        }

        private void TickRepairs(float dt, int pulled)
        {
            for (var i = 0; i < RodCount; i++)
            {
                var drone = _drones[i];
                var rod = _rods[i];
                if (drone == null || rod == null || !drone.Health.IsAlive || rod.Inserted)
                {
                    _drones[i] = null;
                    _repairProgress[i] = 0f;
                    continue;
                }
                drone.SpeedScale = pulled >= 3 ? _fastDrones : 1f;
                _repairProgress[i] = drone.AtGoal(_repairRange) ? _repairProgress[i] + dt : 0f;
                if (_repairProgress[i] < _repairTime)
                    continue;
                rod.Restore();
                drone.Goal = null;
                _drones[i] = null;
                _repairProgress[i] = 0f;
                if (!_restoredSound.IsNull)
                    RuntimeManager.PlayOneShot(_restoredSound, rod.transform.position);
            }
        }
    }
}
