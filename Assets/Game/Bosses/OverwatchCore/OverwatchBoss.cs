using FMODUnity;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies;
using TentacleVania.Enemies.Traps;
using UnityEngine;
using UnityEngine.AI;

namespace TentacleVania.Bosses.OverwatchCore
{
    public sealed class OverwatchBoss : BossEnemy
    {
        private sealed class CoreShell : IDamageModifier
        {
            public OverwatchBoss Owner;
            public int Order => 950;

            public void Modify(ref DamageContext context)
            {
                if (Owner.Open)
                    return;
                context.HealthScale = 0f;
                context.ArmorScale = 0f;
                context.StaggerScale = 0f;
            }
        }

        [Header("Room hacking (design: the core never attacks; the room fights for it)")]
        [SerializeField, Min(1f)] private float _reconfigureTime = 7f;
        [Tooltip("Seconds the core stays open between reconfigurations (the damage window).")]
        [SerializeField, Min(0.5f)] private float _openTime = 4f;
        [SerializeField] private GameObject _shell;
        [SerializeField] private EventReference _reconfigureSound;
        [SerializeField] private EventReference _openSound;

        [Header("Walls")]
        [SerializeField] private IceWall _wall;
        [SerializeField, Min(0)] private int _walls = 3;
        [SerializeField, Min(1f)] private float _wallRing = 5f;
        [SerializeField, Min(1f)] private float _wallWidth = 4f;

        [Header("Turrets (they swap positions each reconfiguration)")]
        [SerializeField] private Enemy _turret;
        [SerializeField] private int[] _turretsPerPhase = { 2, 3, 3 };
        [SerializeField, Min(1f)] private float _turretRing = 8f;

        [Header("Laser hazards (from phase 2)")]
        [SerializeField] private FireZone _laser;
        [SerializeField, Min(0)] private int _laserPhase = 1;
        [SerializeField, Min(1)] private int _lasers = 3;
        [SerializeField, Min(1f)] private float _laserLength = 13f;
        [SerializeField, Range(2f, 45f)] private float _laserArc = 8f;
        [SerializeField, Min(0f)] private float _laserDamagePerSecond = 20f;

        [Header("Assault Mech (phase 3)")]
        [SerializeField] private Enemy _mech;
        [SerializeField, Min(0)] private int _mechPhase = 2;

        private readonly CoreShell _coreShell = new();
        private readonly Enemy[] _turrets = new Enemy[4];
        private Vector3 _home;
        private float _cycleStarted;
        private bool _open;
        private bool _mechSent;
        private int _reconfigurations;

        public bool Open => _open;
        public int Reconfigurations => _reconfigurations;

        protected override void Awake()
        {
            base.Awake();
            _coreShell.Owner = this;
            BaseSpeed = 0f;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Health.Incoming.Add(_coreShell);
            _home = Position;
            _open = false;
            _mechSent = false;
            _reconfigurations = 0;
            _cycleStarted = float.MaxValue;
            SetShell();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Health.Incoming.Remove(_coreShell);
            ClearTurrets();
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            ClearTurrets();
        }

        protected override Vector3 ChaseDestination(Vector3 targetPosition) => Position;

        protected override Vector3 CircleDestination(Vector3 targetPosition, float dt) => Position;

        protected override bool FaceTarget(float distance) => false;

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            BaseSpeed = 0f;
            if (!Health.IsAlive || State == EnemyState.Dead || State == EnemyState.Consumed)
                return;
            if (IsFinishing)
            {
                ClearTurrets();
                if (!_open)
                {
                    _open = true;
                    SetShell();
                }
                return;
            }
            if (State == EnemyState.Spawning)
                return;
            if (_cycleStarted == float.MaxValue)
                Reconfigure(now);
            var elapsed = now - _cycleStarted;
            if (!_open && elapsed >= _reconfigureTime)
            {
                _open = true;
                SetShell();
                if (!_openSound.IsNull)
                    RuntimeManager.PlayOneShot(_openSound, Position);
            }
            else if (_open && elapsed >= _reconfigureTime + _openTime)
                Reconfigure(now);
            if (!_mechSent && Phase >= _mechPhase && _mech != null)
            {
                _mechSent = true;
                SpawnAdd(_mech, Ring(_turretRing, Random.Range(0f, 360f)));
            }
        }

        private void SetShell()
        {
            if (_shell != null)
                _shell.SetActive(!_open);
        }

        private Vector3 Ring(float radius, float angle) => _home + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;

        private void Reconfigure(float now)
        {
            _cycleStarted = now;
            _open = false;
            _reconfigurations++;
            SetShell();
            if (!_reconfigureSound.IsNull)
                RuntimeManager.PlayOneShot(_reconfigureSound, Position);
            var offset = Random.Range(0f, 360f);
            if (_wall != null)
                for (var i = 0; i < _walls; i++)
                {
                    var angle = offset + i * 360f / Mathf.Max(1, _walls);
                    var center = Ring(_wallRing, angle);
                    var along = Quaternion.Euler(0f, angle + 90f, 0f) * Vector3.forward;
                    var creature = Combat.CombatTargets.Creature;
                    if (creature != null && IceWall.Distance(center, along, _wallWidth, 1f, creature.transform.position) < 2f)
                        continue;
                    EnemyManager.RaiseWall(_wall, center, along, _wallWidth, _reconfigureTime + _openTime - 1f);
                }
            ClearTurrets();
            var count = _turretsPerPhase.Length == 0 ? 0 : Mathf.Min(_turrets.Length, _turretsPerPhase[Mathf.Clamp(Phase, 0, _turretsPerPhase.Length - 1)]);
            if (_turret != null)
                for (var i = 0; i < count; i++)
                {
                    var spot = Ring(_turretRing, offset + 45f + i * 360f / count);
                    if (NavMesh.SamplePosition(spot, out var hit, 2f, NavMesh.AllAreas))
                        spot = hit.position;
                    var away = spot - _home;
                    away.y = 0f;
                    _turrets[i] = Instantiate(_turret, spot, Quaternion.LookRotation(-away));
                }
            if (_laser != null && Phase >= _laserPhase)
                for (var i = 0; i < _lasers; i++)
                {
                    var direction = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
                    EnemyManager.PlaceFire(_laser, _home, direction, _laserLength, _laserArc, 1.2f, 3f, _laserDamagePerSecond, Health);
                }
        }

        private void ClearTurrets()
        {
            for (var i = 0; i < _turrets.Length; i++)
            {
                if (_turrets[i] != null)
                    Destroy(_turrets[i].gameObject);
                _turrets[i] = null;
            }
        }
    }
}
