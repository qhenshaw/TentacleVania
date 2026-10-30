using System;
using System.Collections.Generic;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies.AI;
using TentacleVania.Enemies.Data;
using TentacleVania.Enemies.Telegraphs;
using UnityEngine;
using UnityEngine.AI;

namespace TentacleVania.Enemies
{
    public enum EnemyState
    {
        Spawning,
        Chase,
        Circle,
        Attacking,
        Stunned,
        Dead,
    }

    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        private const float CircleStepDegrees = 35f;

        private static readonly RaycastHit[] SweepHits = new RaycastHit[16];
        private static readonly Collider[] OverlapHits = new Collider[16];

        [SerializeField] private EnemyDefinition _definition;
        [SerializeField] private TelegraphGlow _glow;
        [SerializeField] private GroundIndicator _indicator;
        [SerializeField] private Transform _muzzle;

        private readonly List<Health> _hitThisAttack = new(4);

        private NavMeshAgent _agent;
        private Health _health;
        private float[] _readyAt;
        private float _stateStarted;
        private float _stunUntil;
        private float _circleAngle;
        private int _attackIndex = -1;
        private float _attackStarted;
        private Vector3 _attackForward;
        private Vector3 _areaCenter;
        private float _lastSweepT;
        private bool _activeStarted;

        public event Action<Enemy> StateChanged;

        public EnemyDefinition Definition => _definition;
        public Health Health => _health;
        public EnemyState State { get; private set; }
        public EnemyAttack CurrentAttack => _attackIndex >= 0 ? _definition.Attacks[_attackIndex] : null;
        public AttackPhase CurrentPhase => CurrentAttack == null ? AttackPhase.Done : AttackMath.Phase(CurrentAttack, Time.time - _attackStarted);
        protected NavMeshAgent Agent => _agent;
        protected float StateTime => Time.time - _stateStarted;

        protected virtual void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();
            _agent.updateRotation = false;
            _agent.autoBraking = true;
            _readyAt = new float[_definition.Attacks.Count];
            _health.Configure(Team.Site, _definition.MaxHealth, _definition.MaxArmor);
            _agent.speed = _definition.MoveSpeed;
            _circleAngle = UnityEngine.Random.Range(0f, 360f);
        }

        protected virtual void OnEnable()
        {
            _health.Died += OnDied;
            EnemyManager.Register(this);
            SetState(EnemyState.Spawning);
        }

        protected virtual void OnDisable()
        {
            _health.Died -= OnDied;
            EnemyManager.Unregister(this);
            EndAttack(false);
        }

        public void Tick(float now, float dt)
        {
            var target = CombatTargets.Creature;
            switch (State)
            {
                case EnemyState.Spawning:
                    if (StateTime >= _definition.SpawnDelay)
                        SetState(EnemyState.Chase);
                    break;
                case EnemyState.Chase:
                case EnemyState.Circle:
                    if (target == null || !target.IsAlive)
                    {
                        Halt();
                        break;
                    }
                    Pursue(target, now, dt);
                    break;
                case EnemyState.Attacking:
                    TickAttack(target, now, dt);
                    break;
                case EnemyState.Stunned:
                    if (now >= _stunUntil)
                        SetState(EnemyState.Chase);
                    break;
            }
        }

        public void Stun(float duration)
        {
            if (State == EnemyState.Dead)
                return;
            EndAttack(false);
            _stunUntil = Mathf.Max(_stunUntil, Time.time + duration);
            Halt();
            SetState(EnemyState.Stunned);
        }

        public void SetFrozen(bool frozen)
        {
            if (frozen)
            {
                EndAttack(false);
                Halt();
                if (State == EnemyState.Attacking)
                    SetState(EnemyState.Chase);
            }
        }

        protected virtual bool TrySelectAttack(float distance, float now, out int index)
        {
            var attacks = _definition.Attacks;
            for (var i = 0; i < attacks.Count; i++)
            {
                if (now >= _readyAt[i] && attacks[i].InRange(distance))
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        protected virtual Vector3 ChaseDestination(Vector3 targetPosition) => targetPosition;

        protected virtual Vector3 CircleDestination(Vector3 targetPosition, float dt)
        {
            var range = _definition.CircleRange;
            var radius = Mathf.Clamp(Vector3.Distance(transform.position, targetPosition), range.x, range.y);
            var bearing = Quaternion.LookRotation(Flat(transform.position - targetPosition).normalized + Vector3.forward * 1e-4f).eulerAngles.y;
            _circleAngle = Mathf.MoveTowardsAngle(_circleAngle, bearing + CircleStepDegrees, 90f * dt);
            return targetPosition + Quaternion.Euler(0f, _circleAngle, 0f) * Vector3.forward * radius;
        }

        private void Pursue(Health target, float now, float dt)
        {
            var targetPosition = target.transform.position;
            var distance = Vector3.Distance(Flat(transform.position), Flat(targetPosition));

            if (TrySelectAttack(distance, now, out var index))
            {
                if (EnemyManager.Instance.Tokens.TryAcquire(this, _definition.Attacks[index].TokenKind))
                {
                    BeginAttack(index, targetPosition);
                    return;
                }
                if (State != EnemyState.Circle)
                    SetState(EnemyState.Circle);
            }
            else if (State != EnemyState.Chase)
            {
                SetState(EnemyState.Chase);
            }

            if (!_agent.isOnNavMesh)
                return;
            _agent.isStopped = false;
            _agent.SetDestination(State == EnemyState.Circle ? CircleDestination(targetPosition, dt) : ChaseDestination(targetPosition));
            var look = distance < _definition.CircleRange.y + 1f ? targetPosition - transform.position : _agent.velocity;
            Face(look, dt);
        }

        private void BeginAttack(int index, Vector3 targetPosition)
        {
            _attackIndex = index;
            _attackStarted = Time.time;
            _activeStarted = false;
            _hitThisAttack.Clear();
            Halt();
            _attackForward = Flat(targetPosition - transform.position).normalized;
            if (_attackForward.sqrMagnitude < 1e-4f)
                _attackForward = transform.forward;
            UpdateAreaCenter();
            SetState(EnemyState.Attacking);
        }

        private void TickAttack(Health target, float now, float dt)
        {
            var attack = CurrentAttack;
            var elapsed = now - _attackStarted;
            switch (AttackMath.Phase(attack, elapsed))
            {
                case AttackPhase.WindUp:
                    var progress = AttackMath.WindUpProgress(attack, elapsed);
                    if (progress <= attack.TrackDuringWindUp && target != null)
                    {
                        _attackForward = Flat(target.transform.position - transform.position).normalized;
                        UpdateAreaCenter();
                    }
                    Face(_attackForward, dt);
                    if (_glow != null)
                        _glow.Show(attack.Telegraph, progress);
                    if (attack.Kind == AttackKind.Area && _indicator != null)
                        _indicator.Show(_areaCenter, attack.AreaRadius, attack.Telegraph, progress);
                    break;
                case AttackPhase.Active:
                    if (!_activeStarted)
                    {
                        _activeStarted = true;
                        _lastSweepT = 0f;
                        OnActiveStart(attack);
                    }
                    if (attack.Kind == AttackKind.Melee)
                        Sweep(attack, AttackMath.ActiveProgress(attack, elapsed));
                    break;
                case AttackPhase.Recovery:
                    if (_glow != null)
                        _glow.Clear();
                    if (_indicator != null)
                        _indicator.Hide();
                    break;
                case AttackPhase.Done:
                    EndAttack(true);
                    SetState(EnemyState.Chase);
                    break;
            }
        }

        protected virtual void OnActiveStart(EnemyAttack attack)
        {
            switch (attack.Kind)
            {
                case AttackKind.Melee:
                    Sweep(attack, 0f);
                    break;
                case AttackKind.Area:
                    AreaHit(attack);
                    break;
                case AttackKind.Projectile:
                    Fire(attack);
                    break;
            }
        }

        private void Sweep(EnemyAttack attack, float t)
        {
            var origin = transform.position + Vector3.up * attack.Height;
            var steps = AttackMath.SweepSteps(attack.Arc, _lastSweepT, t);
            for (var step = 1; step <= steps; step++)
            {
                var stepT = Mathf.Lerp(_lastSweepT, t, (float)step / steps);
                var direction = AttackMath.ArcDirection(_attackForward, attack.Arc, stepT);
                var count = Physics.SphereCastNonAlloc(origin, attack.SweepRadius, direction, SweepHits, attack.Reach, EnemyLayers.HurtboxMask, QueryTriggerInteraction.Collide);
                for (var i = 0; i < count; i++)
                    Deliver(attack, SweepHits[i].collider, SweepHits[i].point.sqrMagnitude > 0f ? SweepHits[i].point : origin, direction);
            }
            _lastSweepT = t;
        }

        private void AreaHit(EnemyAttack attack)
        {
            var bottom = _areaCenter;
            var top = _areaCenter + Vector3.up * 2f;
            var count = Physics.OverlapCapsuleNonAlloc(bottom, top, attack.AreaRadius, OverlapHits, EnemyLayers.HurtboxMask, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                var collider = OverlapHits[i];
                if (AttackMath.InsideArea(_areaCenter, attack.AreaRadius, collider.ClosestPoint(_areaCenter + Vector3.up)))
                    Deliver(attack, collider, collider.transform.position, Flat(collider.transform.position - _areaCenter).normalized);
            }
        }

        private void Fire(EnemyAttack attack)
        {
            if (attack.Projectile == null)
                return;
            var origin = _muzzle != null ? _muzzle.position : transform.position + Vector3.up * attack.Height;
            var aim = CombatTargets.Creature != null ? (CombatTargets.CreatureAimPoint - origin).normalized : _attackForward;
            if (aim.sqrMagnitude < 1e-4f)
                aim = _attackForward;
            var info = Damage(attack, origin, aim);
            EnemyManager.FireProjectile(attack.Projectile, origin, aim * attack.ProjectileSpeed, info, attack.ProjectileLifetime);
        }

        private void Deliver(EnemyAttack attack, Collider collider, Vector3 point, Vector3 direction)
        {
            if (!Hurtbox.TryGet(collider, out var hurtbox) || hurtbox.Health == _health || _hitThisAttack.Contains(hurtbox.Health))
                return;
            var result = hurtbox.Health.TakeDamage(Damage(attack, point, direction));
            if (result.Outcome != DamageOutcome.Ignored)
                _hitThisAttack.Add(hurtbox.Health);
        }

        private DamageInfo Damage(EnemyAttack attack, Vector3 point, Vector3 direction)
        {
            var info = DamageInfo.Create(attack.Damage, Team.Site, attack.Type, attack.Stagger);
            info.Flags = attack.Flags;
            info.Attacker = _health;
            info.Point = point;
            info.Direction = direction;
            return info;
        }

        private void EndAttack(bool startCooldown)
        {
            if (_attackIndex >= 0 && startCooldown)
                _readyAt[_attackIndex] = Time.time + _definition.Attacks[_attackIndex].Cooldown;
            _attackIndex = -1;
            if (_glow != null)
                _glow.Clear();
            if (_indicator != null)
                _indicator.Hide();
            EnemyManager.ReleaseTokens(this);
        }

        private void UpdateAreaCenter() =>
            _areaCenter = transform.position + _attackForward * (CurrentAttack?.AreaForwardOffset ?? 0f);

        private void OnDied(Health health)
        {
            EndAttack(false);
            Halt();
            _agent.enabled = false;
            SetState(EnemyState.Dead);
            OnDeath();
            Destroy(gameObject, _definition.DespawnDelay);
        }

        protected virtual void OnDeath() { }

        protected void Halt()
        {
            if (_agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        protected void Face(Vector3 direction, float dt)
        {
            direction = Flat(direction);
            if (direction.sqrMagnitude < 1e-4f)
                return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), _definition.TurnSpeed * dt);
        }

        private void SetState(EnemyState state)
        {
            if (State == state && state != EnemyState.Spawning)
                return;
            State = state;
            _stateStarted = Time.time;
            StateChanged?.Invoke(this);
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
