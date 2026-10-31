using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies;
using TentacleVania.Enemies.AI;
using UnityEngine;

namespace TentacleVania.Bosses.Warden
{
    public sealed class WardenDrone : BossEnemy
    {
        private const int MaxBeams = 3;

        private sealed class StaggerShield : IDamageModifier
        {
            public Enemy Owner;
            public int Order => 950;

            public void Modify(ref DamageContext context)
            {
                if (!Owner.IsStaggerBroken)
                    context.HealthScale = 0f;
            }
        }

        [Header("Movement (Warden Drone page)")]
        [SerializeField] private Vector2 _keepAway = new(6f, 8f);
        [SerializeField] private Transform _model;
        [SerializeField, Min(0f)] private float _hoverHeight = 1.8f;

        [Header("Shock Prod lunge")]
        [SerializeField] private string _lungeAttack = "Shock Prod";
        [SerializeField, Min(0f)] private float _lungeSpeed = 22f;

        [Header("Adds: Security Guards per stagger break")]
        [SerializeField] private Enemy _guardPrefab;
        [SerializeField, Min(0)] private int _guardsPerBreak = 2;
        [SerializeField, Min(0)] private int _maxGuards = 4;

        [Header("Wall Beams (red)")]
        [SerializeField, Min(1f)] private float _beamInterval = 15f;
        [SerializeField, Min(0.1f)] private float _beamWarning = 1.2f;
        [SerializeField, Min(0.05f)] private float _beamActive = 0.3f;
        [SerializeField, Min(0.1f)] private float _beamWidth = 1.6f;
        [SerializeField, Min(0f)] private float _beamDamage = 25f;
        [SerializeField, Min(1)] private int _beams = 2;
        [SerializeField, Min(1)] private int _lowHealthBeams = 3;
        [SerializeField, Range(0f, 1f)] private float _lowHealthBelow = 0.4f;
        [Tooltip("Hall the beams cross; when empty the hall is centred on the spawn point.")]
        [SerializeField] private Vector2 _hallSize = new(24f, 18f);
        [SerializeField] private LineRenderer[] _beamLines = new LineRenderer[MaxBeams];
        [SerializeField] private Color _warningColor = new(2.5f, 0.2f, 0.1f, 0.35f);
        [SerializeField] private Color _firingColor = new(4f, 1.2f, 3.5f, 1f);
        [SerializeField] private EventReference _beamWarnSound;
        [SerializeField] private EventReference _beamFireSound;

        private readonly StaggerShield _shield = new();
        private readonly float[] _beamOffsets = new float[MaxBeams];
        private Vector3 _hallCenter;
        private Quaternion _hallRotation;
        private float _nextBeams;
        private float _beamsStarted = -1f;
        private int _beamCount;
        private bool _beamsFired;
        private float _bobPhase;

        protected override void Awake()
        {
            base.Awake();
            _shield.Owner = this;
            _bobPhase = Random.value * 6.28f;
            Health.Bleeds = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _hallCenter = transform.position;
            _hallRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            var arena = FindAnyObjectByType<WardenHall>();
            if (arena != null)
            {
                _hallCenter = arena.transform.position;
                _hallRotation = arena.transform.rotation;
                _hallSize = arena.Size;
            }
            Health.Incoming.Add(_shield);
            Staggered += OnStaggered;
            _nextBeams = Time.time + Definition.SpawnDelay + _beamInterval * 0.6f;
            HideBeams();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Health.Incoming.Remove(_shield);
            Staggered -= OnStaggered;
            HideBeams();
        }

        protected override Vector3 ChaseDestination(Vector3 targetPosition) => KeepAway(targetPosition);

        protected override Vector3 CircleDestination(Vector3 targetPosition, float dt) => KeepAway(targetPosition);

        protected override bool FaceTarget(float distance) => true;

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            Bob(now);
            if (!Health.IsAlive || IsFinishing || State == EnemyState.Dead || State == EnemyState.Consumed)
            {
                HideBeams();
                return;
            }
            if (State == EnemyState.Attacking && CurrentAttack != null && CurrentAttack.Name == _lungeAttack && CurrentPhase == AttackPhase.Active && Agent.enabled && Agent.isOnNavMesh)
                Agent.Move(transform.forward * (_lungeSpeed * dt));
            TickBeams(now);
        }

        private Vector3 KeepAway(Vector3 target)
        {
            var away = Position - target;
            away.y = 0f;
            var distance = away.magnitude;
            var direction = distance > 1e-3f ? away / distance : -transform.forward;
            if (distance >= _keepAway.x && distance <= _keepAway.y)
                return Position;
            var wanted = target + direction * ((_keepAway.x + _keepAway.y) * 0.5f);
            for (var turn = 0; turn < 5; turn++)
            {
                var angle = (turn + 1) / 2 * 40f * (turn % 2 == 0 ? 1f : -1f);
                var candidate = target + Quaternion.Euler(0f, angle, 0f) * direction * ((_keepAway.x + _keepAway.y) * 0.5f);
                if (TrySampleNavMesh(candidate, 1.5f, out var spot))
                    return spot;
            }
            return TrySampleNavMesh(wanted, 3f, out var fallback) ? fallback : Position;
        }

        private void OnStaggered(Enemy enemy)
        {
            if (_guardPrefab == null)
                return;
            for (var i = 0; i < _guardsPerBreak && Adds.Count < _maxGuards; i++)
                SpawnAdd(_guardPrefab);
        }

        private void TickBeams(float now)
        {
            if (_beamsStarted < 0f)
            {
                if (now < _nextBeams || State == EnemyState.Spawning)
                    return;
                StartBeams(now);
                return;
            }
            var elapsed = now - _beamsStarted;
            if (!_beamsFired && elapsed >= _beamWarning)
            {
                _beamsFired = true;
                Play(_beamFireSound);
                for (var i = 0; i < _beamCount; i++)
                    ShowBeam(i, true);
                HitCreature();
            }
            if (elapsed >= _beamWarning + _beamActive)
            {
                HideBeams();
                _beamsStarted = -1f;
                _nextBeams = now + _beamInterval * (Health.Normalized <= _lowHealthBelow ? 0.75f : 1f);
            }
        }

        private void StartBeams(float now)
        {
            _beamsStarted = now;
            _beamsFired = false;
            _beamCount = Mathf.Min(MaxBeams, Health.Normalized <= _lowHealthBelow ? _lowHealthBeams : _beams);
            var creature = CombatTargets.Creature;
            var local = creature != null ? Quaternion.Inverse(_hallRotation) * (creature.transform.position - _hallCenter) : Vector3.zero;
            WardenRules.PickBeams(_hallSize.x, _beamWidth, _beamCount, local.x, Random.value, _beamOffsets);
            for (var i = 0; i < _beamCount; i++)
                ShowBeam(i, false);
            Play(_beamWarnSound);
        }

        private void HitCreature()
        {
            var creature = CombatTargets.Creature;
            if (creature == null || !creature.IsAlive)
                return;
            var local = Quaternion.Inverse(_hallRotation) * (creature.transform.position - _hallCenter);
            if (!WardenRules.InBeam(local.x, _beamOffsets, _beamCount, _beamWidth))
                return;
            var info = DamageInfo.Create(_beamDamage, Team.Site, DamageType.Electric);
            info.Flags = HitFlags.Undodgeable;
            info.Attacker = Health;
            info.Point = creature.transform.position + Vector3.up;
            creature.TakeDamage(info);
        }

        private void ShowBeam(int index, bool firing)
        {
            var line = index < _beamLines.Length ? _beamLines[index] : null;
            if (line == null)
                return;
            var half = _hallSize.y * 0.5f;
            var x = _beamOffsets[index];
            var height = firing ? 1f : 0.05f;
            line.SetPosition(0, _hallCenter + _hallRotation * new Vector3(x, height, -half));
            line.SetPosition(1, _hallCenter + _hallRotation * new Vector3(x, height, half));
            line.widthMultiplier = firing ? _beamWidth * 0.6f : _beamWidth;
            line.startColor = line.endColor = firing ? _firingColor : _warningColor;
            line.enabled = true;
        }

        private void HideBeams()
        {
            foreach (var line in _beamLines)
                if (line != null)
                    line.enabled = false;
        }

        private void Bob(float now)
        {
            if (_model == null)
                return;
            var y = IsFinishing ? 0.4f : _hoverHeight + Mathf.Sin(now * 1.3f + _bobPhase) * 0.15f;
            var local = _model.localPosition;
            _model.localPosition = new(local.x, Mathf.MoveTowards(local.y, y, Time.deltaTime * 4f), local.z);
        }

        private void Play(EventReference sound)
        {
            if (!sound.IsNull)
                RuntimeManager.PlayOneShot(sound, transform.position);
        }
    }
}
