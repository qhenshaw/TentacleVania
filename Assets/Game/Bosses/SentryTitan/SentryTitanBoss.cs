using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies;
using TentacleVania.Enemies.Traps;
using UnityEngine;

namespace TentacleVania.Bosses.SentryTitan
{
    public sealed class SentryTitanBoss : BossEnemy
    {
        private const int MaxMissiles = 4;

        private sealed class SelfFire : IDamageModifier
        {
            private readonly SentryTitanBoss _owner;

            public SelfFire(SentryTitanBoss owner) => _owner = owner;

            public int Order => 50;

            public void Modify(ref DamageContext context)
            {
                if (context.Info.Attacker != _owner.Health)
                    return;
                context.ArmorScale *= _owner._selfArmorScale;
                context.HealthScale *= _owner._selfHealthScale;
                _owner.SelfHits++;
            }
        }

        [Header("Friendly fire (design: its own shells break its armor)")]
        [SerializeField, Min(0f)] private float _selfArmorScale = 5f;
        [SerializeField, Min(0f)] private float _selfHealthScale = 0.5f;

        [Header("Shells")]
        [SerializeField] private AcidBurst _shell;
        [SerializeField, Min(0f)] private float _shellDamage = 18f;

        [Header("Artillery Barrage")]
        [SerializeField, Min(1f)] private float _barrageInterval = 9f;
        [SerializeField, Min(1)] private int _barrageShells = 7;
        [SerializeField, Min(0.1f)] private float _barrageRadius = 2f;
        [SerializeField, Min(0.4f)] private float _barrageFuse = 1.8f;
        [SerializeField, Min(0f)] private float _barrageSpread = 9f;
        [Tooltip("Shells that land around the Titan itself, so it can be baited into its own barrage.")]
        [SerializeField, Min(0)] private int _barrageNearSelf = 2;

        [Header("Arcing rockets")]
        [SerializeField, Min(1f)] private float _rocketInterval = 6f;
        [SerializeField, Min(1)] private int _rockets = 4;
        [SerializeField, Min(0.1f)] private float _rocketRadius = 1.6f;
        [SerializeField, Min(0.4f)] private float _rocketFuse = 1.3f;

        [Header("Grenade launcher")]
        [SerializeField, Min(1f)] private float _grenadeInterval = 5f;
        [SerializeField, Min(1)] private int _grenades = 3;
        [SerializeField, Min(0.1f)] private float _grenadeRadius = 1.4f;
        [SerializeField, Min(0.2f)] private float _grenadeFuse = 0.9f;

        [Header("Seeking missiles (phase 2)")]
        [SerializeField] private Transform _missilePrefab;
        [SerializeField, Min(0)] private int _missilePhase = 1;
        [SerializeField, Min(1f)] private float _missileInterval = 8f;
        [SerializeField, Min(1)] private int _missilesPerVolley = 2;
        [SerializeField, Min(0.5f)] private float _missileSpeed = 6f;
        [SerializeField, Min(10f)] private float _missileTurn = 120f;
        [SerializeField, Min(0.5f)] private float _missileLife = 5f;
        [SerializeField, Min(0.1f)] private float _missileRadius = 1.5f;

        private readonly Transform[] _missiles = new Transform[MaxMissiles];
        private readonly float[] _missileUntil = new float[MaxMissiles];
        private SelfFire _selfFire;
        private float _nextBarrage;
        private float _nextRocket;
        private float _nextGrenade;
        private float _nextMissile;

        public int SelfHits { get; private set; }
        public int Barrages { get; private set; }
        public int LiveMissiles
        {
            get
            {
                var count = 0;
                foreach (var missile in _missiles)
                    if (missile != null && missile.gameObject.activeSelf)
                        count++;
                return count;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _selfFire = new(this);
            if (_shell != null)
                EnemyManager.PrewarmBursts(_shell, _barrageShells + _rockets + _grenades + MaxMissiles);
            for (var i = 0; i < MaxMissiles; i++)
            {
                if (_missilePrefab == null)
                    break;
                _missiles[i] = Instantiate(_missilePrefab);
                _missiles[i].gameObject.SetActive(false);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Health.Incoming.Add(_selfFire);
            SelfHits = 0;
            Barrages = 0;
            var now = Time.time;
            _nextBarrage = now + _barrageInterval * 0.6f;
            _nextRocket = now + _rocketInterval;
            _nextGrenade = now + _grenadeInterval * 0.5f;
            _nextMissile = now + _missileInterval;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Health.Incoming.Remove(_selfFire);
            HideMissiles();
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            HideMissiles();
        }

        private void OnDestroy()
        {
            foreach (var missile in _missiles)
                if (missile != null)
                    Destroy(missile.gameObject);
        }

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            StepMissiles(now, dt);
            if (!Health.IsAlive || State == EnemyState.Dead || State == EnemyState.Consumed || State == EnemyState.Spawning || IsFinishing)
                return;
            var creature = CombatTargets.Creature;
            if (creature == null || !creature.IsAlive)
                return;
            var target = creature.transform.position;
            var pace = Phase >= 1 ? 0.75f : 1f;
            if (now >= _nextBarrage)
            {
                _nextBarrage = now + _barrageInterval * pace;
                Barrage(target);
            }
            if (now >= _nextRocket)
            {
                _nextRocket = now + _rocketInterval * pace;
                for (var i = 0; i < _rockets; i++)
                    Shell(i == 0 ? target : target + Around(_rocketRadius * 2f, _rocketRadius * 4f), _rocketRadius, _rocketFuse);
            }
            if (now >= _nextGrenade)
            {
                _nextGrenade = now + _grenadeInterval * pace;
                var toward = target - Position;
                toward.y = 0f;
                var step = toward.sqrMagnitude > 1e-4f ? toward / _grenades : Vector3.forward;
                for (var i = 1; i <= _grenades; i++)
                    Shell(Position + step * i, _grenadeRadius, _grenadeFuse + i * 0.15f);
            }
            if (Phase >= _missilePhase && now >= _nextMissile)
            {
                _nextMissile = now + _missileInterval;
                for (var i = 0; i < _missilesPerVolley; i++)
                    Launch(now, i);
            }
        }

        private void Barrage(Vector3 target)
        {
            Barrages++;
            for (var i = 0; i < _barrageShells; i++)
            {
                var spot = i < _barrageNearSelf ? Position + Around(1f, 2.5f) : target + Around(0f, _barrageSpread);
                Shell(spot, _barrageRadius, _barrageFuse + i * 0.08f);
            }
        }

        private static Vector3 Around(float min, float max) => Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(min, max);

        private void Shell(Vector3 spot, float radius, float fuse)
        {
            if (_shell == null)
                return;
            spot.y = Position.y;
            var info = DamageInfo.Create(_shellDamage, Team.Environment, DamageType.Physical, _shellDamage);
            info.Attacker = Health;
            EnemyManager.PlaceBurst(_shell, spot, radius, fuse, info);
        }

        private void Launch(float now, int index)
        {
            for (var i = 0; i < MaxMissiles; i++)
            {
                var missile = _missiles[i];
                if (missile == null || missile.gameObject.activeSelf)
                    continue;
                var side = index % 2 == 0 ? transform.right : -transform.right;
                missile.position = Position + Vector3.up * 2.5f + side;
                missile.rotation = Quaternion.LookRotation(transform.forward + Vector3.up * 0.5f);
                missile.gameObject.SetActive(true);
                _missileUntil[i] = now + _missileLife;
                return;
            }
        }

        private void StepMissiles(float now, float dt)
        {
            var creature = CombatTargets.Creature;
            for (var i = 0; i < MaxMissiles; i++)
            {
                var missile = _missiles[i];
                if (missile == null || !missile.gameObject.activeSelf)
                    continue;
                var goal = creature != null ? creature.transform.position + Vector3.up * 0.5f : missile.position + missile.forward;
                var to = goal - missile.position;
                if (to.sqrMagnitude > 1e-4f)
                    missile.rotation = Quaternion.RotateTowards(missile.rotation, Quaternion.LookRotation(to), _missileTurn * dt);
                missile.position += missile.forward * (_missileSpeed * dt);
                if (now < _missileUntil[i] && to.sqrMagnitude > 0.8f * 0.8f && missile.position.y > Position.y + 0.1f)
                    continue;
                missile.gameObject.SetActive(false);
                Shell(missile.position, _missileRadius, 0.05f);
            }
        }

        private void HideMissiles()
        {
            foreach (var missile in _missiles)
                if (missile != null)
                    missile.gameObject.SetActive(false);
        }
    }
}
