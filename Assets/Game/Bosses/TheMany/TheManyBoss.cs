using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies;
using TentacleVania.Enemies.Traps;
using UnityEngine;

namespace TentacleVania.Bosses.TheMany
{
    public sealed class TheManyBoss : BossEnemy
    {
        private const int MaxMotes = 48;
        private const int MaxSplinters = 4;

        [Header("Swarm (design: the queen hides inside the swarm; hitting her hurts The Many)")]
        [SerializeField] private Transform _motePrefab;
        [SerializeField, Range(1, MaxMotes)] private int _motes = 40;
        [SerializeField, Min(0.2f)] private float _cloudRadius = 1.8f;
        [SerializeField, Min(0.1f)] private float _orbitSpeed = 2.2f;

        [Header("Swarm Wave")]
        [SerializeField] private AcidBurst _bite;
        [SerializeField, Min(1f)] private float _waveInterval = 10f;
        [SerializeField, Min(1)] private int _waveBites = 6;
        [SerializeField, Min(0.1f)] private float _waveRadius = 1.6f;
        [SerializeField, Min(0.3f)] private float _waveFuse = 0.9f;
        [SerializeField, Min(1f)] private float _waveLength = 16f;
        [SerializeField, Min(1f)] private float _waveSpeed = 14f;
        [SerializeField, Min(0f)] private float _waveDamage = 14f;

        [Header("Engulf (red: dash out before it closes)")]
        [SerializeField] private AcidBurst _engulf;
        [SerializeField, Min(1f)] private float _engulfInterval = 12f;
        [SerializeField, Min(1f)] private float _engulfRange = 9f;
        [SerializeField, Min(0.5f)] private float _engulfRadius = 2.6f;
        [SerializeField, Min(0.4f)] private float _engulfFuse = 1.3f;
        [SerializeField, Min(0f)] private float _engulfDamage = 26f;

        [Header("Splinter Swarms (phase 2)")]
        [SerializeField] private Enemy _splinterPrefab;
        [SerializeField, Min(0)] private int _splinterPhase = 1;
        [SerializeField, Min(1f)] private float _splinterInterval = 15f;
        [SerializeField, Range(1, MaxSplinters)] private int _splinters = 3;
        [Tooltip("Share of the swarm cloud that leaves with the splinters; the queen keeps the rest.")]
        [SerializeField, Range(0f, 1f)] private float _splinterShare = 0.6f;

        private readonly Transform[] _cloud = new Transform[MaxMotes];
        private readonly Vector3[] _seeds = new Vector3[MaxMotes];
        private readonly Enemy[] _splinterBodies = new Enemy[MaxSplinters];
        private Vector3 _waveDirection;
        private float _waveUntil = -1f;
        private float _nextWave;
        private float _nextEngulf;
        private float _nextSplinter;

        public int Waves { get; private set; }
        public int Engulfs { get; private set; }
        public int Splinters { get; private set; }
        public bool Charging => _waveUntil >= Time.time;

        protected override void Awake()
        {
            base.Awake();
            if (_bite != null)
                EnemyManager.PrewarmBursts(_bite, _waveBites);
            if (_engulf != null)
                EnemyManager.PrewarmBursts(_engulf, 1);
            for (var i = 0; i < _motes; i++)
            {
                if (_motePrefab == null)
                    break;
                _cloud[i] = Instantiate(_motePrefab);
                _cloud[i].gameObject.SetActive(false);
                _seeds[i] = new(Random.Range(0f, Mathf.PI * 2f), Random.Range(0.4f, 1f), Random.Range(0.6f, 1.4f));
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            var now = Time.time;
            Waves = Engulfs = Splinters = 0;
            _waveUntil = -1f;
            _nextWave = now + _waveInterval * 0.5f;
            _nextEngulf = now + _engulfInterval * 0.7f;
            _nextSplinter = float.MaxValue;
            for (var i = 0; i < MaxSplinters; i++)
                _splinterBodies[i] = null;
            ShowCloud(true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ShowCloud(false);
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            ShowCloud(false);
        }

        private void OnDestroy()
        {
            foreach (var mote in _cloud)
                if (mote != null)
                    Destroy(mote.gameObject);
        }

        private void ShowCloud(bool shown)
        {
            for (var i = 0; i < _motes; i++)
                if (_cloud[i] != null)
                    _cloud[i].gameObject.SetActive(shown);
        }

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            StepCloud(now);
            if (!Health.IsAlive || State == EnemyState.Dead || State == EnemyState.Consumed || State == EnemyState.Spawning || IsFinishing)
                return;
            if (_waveUntil >= now && Agent.enabled && Agent.isOnNavMesh)
                Agent.Move(_waveDirection * (_waveSpeed * dt));
            var creature = CombatTargets.Creature;
            if (creature == null || !creature.IsAlive || State == EnemyState.Stunned)
                return;
            var target = creature.transform.position;
            if (now >= _nextWave)
            {
                _nextWave = now + _waveInterval;
                Wave(now, target);
            }
            if (now >= _nextEngulf && (target - Position).sqrMagnitude <= _engulfRange * _engulfRange)
            {
                _nextEngulf = now + _engulfInterval;
                Engulf(target);
            }
            if (Phase >= _splinterPhase && _nextSplinter == float.MaxValue)
                _nextSplinter = now;
            if (now >= _nextSplinter)
            {
                _nextSplinter = now + _splinterInterval;
                Splinter();
            }
        }

        private void Wave(float now, Vector3 target)
        {
            if (_bite == null)
                return;
            var direction = target - Position;
            direction.y = 0f;
            _waveDirection = direction.sqrMagnitude > 1e-4f ? direction.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(_waveDirection);
            var info = DamageInfo.Create(_waveDamage, Team.Site, DamageType.Physical, _waveDamage * 0.5f);
            info.Attacker = Health;
            var step = _waveLength / _waveBites;
            for (var i = 1; i <= _waveBites; i++)
                EnemyManager.PlaceBurst(_bite, Position + _waveDirection * (step * i), _waveRadius, _waveFuse + i * 0.06f, info);
            _waveUntil = now + _waveFuse + _waveLength / _waveSpeed;
            Waves++;
        }

        private void Engulf(Vector3 target)
        {
            if (_engulf == null)
                return;
            var info = DamageInfo.Create(_engulfDamage, Team.Site, DamageType.Physical, _engulfDamage);
            info.Attacker = Health;
            info.Status = StatusKind.Bleed;
            info.Flags = HitFlags.Undodgeable;
            EnemyManager.PlaceBurst(_engulf, target, _engulfRadius, _engulfFuse, info);
            Engulfs++;
        }

        private void Splinter()
        {
            if (_splinterPrefab == null)
                return;
            for (var i = 0; i < _splinters; i++)
            {
                if (_splinterBodies[i] != null && _splinterBodies[i].Health.IsAlive)
                    continue;
                var spot = Position + Quaternion.Euler(0f, 360f / _splinters * i, 0f) * Vector3.forward * 2.5f;
                _splinterBodies[i] = SpawnAdd(_splinterPrefab, spot);
            }
            Splinters++;
        }

        private void StepCloud(float now)
        {
            var queen = Position + Vector3.up * 0.8f;
            var split = Mathf.RoundToInt(_motes * _splinterShare);
            for (var i = 0; i < _motes; i++)
            {
                var mote = _cloud[i];
                if (mote == null)
                    continue;
                var center = queen;
                var radius = _cloudRadius;
                if (i < split)
                {
                    var body = _splinterBodies[i % MaxSplinters];
                    if (body != null && body.Health.IsAlive && body.State != EnemyState.Consumed)
                    {
                        center = body.transform.position + Vector3.up * 0.6f;
                        radius = _cloudRadius * 0.6f;
                    }
                }
                var seed = _seeds[i];
                var angle = seed.x + now * _orbitSpeed * seed.z;
                var height = Mathf.Sin(now * 1.7f * seed.z + seed.x) * 0.6f * seed.y;
                mote.position = center + new Vector3(Mathf.Cos(angle) * radius * seed.y, height, Mathf.Sin(angle) * radius * seed.y);
            }
        }
    }
}
