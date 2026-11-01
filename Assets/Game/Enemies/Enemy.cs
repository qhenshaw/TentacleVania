using System;
using System.Collections.Generic;
using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Feedback;
using TentacleVania.Combat.Status;
using TentacleVania.Core;
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
        Consumed,
        Dead,
    }

    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public class Enemy : MonoBehaviour, IStatsSource
    {
        private const float CircleStepDegrees = 35f;
        private const float HitFlashDuration = 0.08f;

        private static readonly RaycastHit[] SweepHits = new RaycastHit[16];
        private static readonly Collider[] OverlapHits = new Collider[16];
        private const float LaneOvershoot = 1f;
        private const float DissolveTime = 0.8f;
        private const float DissolveSinkSpeed = 2.5f;
        private static readonly List<Renderer> BodyRenderers = new();

        [SerializeField] private EnemyDefinition _definition;
        [SerializeField] private TelegraphGlow _glow;
        [SerializeField] private GroundIndicator _indicator;
        [SerializeField] private Transform _muzzle;

        private readonly List<Health> _hitThisAttack = new(4);

        private NavMeshAgent _agent;
        private Health _health;
        private float _nextPain;
        private float[] _readyAt;
        private float _stateStarted;
        private float _stunUntil;
        private float _circleAngle;
        private int _attackIndex = -1;
        private float _attackStarted;
        private Vector3 _attackForward;
        private Vector3 _areaCenter;
        private float _lastSweepT;
        private int _hitIndex;
        private bool _activeStarted;
        private StaggerBar _stagger;
        private ScaleModifier _staggerVulnerability;
        private bool _staggerBroken;
        private Consumable _consumable;
        private StatusEffects _status;
        private EnemyAnimator _animator;
        private Vector3 _knockbackVelocity;
        private float _knockbackRemaining;
        private float _dissolveAt = -1f;

        public event Action<Enemy> StateChanged;
        public event Action<Enemy> Staggered;

        public StaggerBar Stagger => _stagger;
        public bool IsStaggerBroken => _staggerBroken;
        public bool IsFeedable => _consumable != null && _consumable.IsGrabbable;

        public EnemyDefinition Definition => _definition;
        public UnityEngine.Object StatsAsset => _definition;
        public Health Health => _health;
        public StatusEffects Status => _status;
        public EnemyState State { get; private set; }
        public EnemyAttack CurrentAttack => _attackIndex >= 0 ? _definition.Attacks[_attackIndex] : null;
        public float AttackStartedAt => _attackStarted;
        public Vector3 Velocity => _agent.enabled ? _agent.velocity : Vector3.zero;
        public virtual bool IsCowering => false;
        public virtual bool IsBoss => false;
        protected virtual bool FeedProtection => true;
        protected Consumable ConsumeTarget => _consumable;
        public AttackPhase CurrentPhase => CurrentAttack == null ? AttackPhase.Done : AttackMath.Phase(CurrentAttack, Time.time - _attackStarted);
        protected NavMeshAgent Agent => _agent;
        protected float BaseSpeed { get; set; }
        protected float StateTime => Time.time - _stateStarted;

        protected virtual void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();
            _agent.updateRotation = false;
            _agent.autoBraking = true;
            _readyAt = new float[_definition.Attacks.Count];
            _health.Configure(Team.Site, _definition.MaxHealth, _definition.MaxArmor);
            _health.Bleeds = !_definition.Machine;
            _health.BurstsOnDeath = !IsBoss;
            BaseSpeed = _definition.MoveSpeed;
            _agent.speed = BaseSpeed * GameSettings.EnemySpeedScale;
            _circleAngle = UnityEngine.Random.Range(0f, 360f);
            _stagger = new(_definition.MaxStagger);
            _staggerVulnerability = new() { HealthScale = _definition.StaggerDamageTaken };
            if (TryGetComponent(out _consumable))
                _consumable.Configure(_definition.ConsumeHeal, _definition.Biomass);
            TryGetComponent(out _animator);
            if (!TryGetComponent(out _status))
                _status = gameObject.AddComponent<StatusEffects>();
            _status.Glow = false;
            _status.Affinity.Configure(_definition.WeakTo, _definition.Resists);
            UpdateProtection();
        }

        protected virtual void OnEnable()
        {
            _health.Died += OnDied;
            _health.Damaged += OnDamaged;
            if (_consumable != null)
                _consumable.Grabbed += OnGrabbed;
            EnemyManager.Register(this);
            _knockbackRemaining = 0f;
            SetState(EnemyState.Spawning);
        }

        protected virtual void OnDisable()
        {
            _health.Died -= OnDied;
            _health.Damaged -= OnDamaged;
            if (_consumable != null)
                _consumable.Grabbed -= OnGrabbed;
            EnemyManager.Unregister(this);
            EndAttack(false);
            EndStaggerBreak();
        }

        public void Tick(float now, float dt)
        {
            _agent.speed = BaseSpeed * GameSettings.EnemySpeedScale;
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
                    {
                        EndStaggerBreak();
                        SetState(EnemyState.Chase);
                    }
                    break;
            }
            OnTick(now, dt);
        }

        protected virtual void OnTick(float now, float dt) { }

        public void TickFeedback()
        {
            UpdateProtection();
            if (_glow != null)
                _glow.SetFeedable(IsFeedable);
            if (_animator != null)
                _animator.Tick(Time.deltaTime);
            TickKnockback(Time.deltaTime);
            TickDissolve(Time.deltaTime);
        }

        private void TickDissolve(float dt)
        {
            if (_dissolveAt < 0f || Time.time < _dissolveAt)
                return;
            transform.position += Vector3.down * (DissolveSinkSpeed * dt);
        }

        private void TickKnockback(float dt)
        {
            if (_knockbackRemaining <= 0f)
                return;
            var step = Mathf.Min(dt, _knockbackRemaining);
            _knockbackRemaining -= step;
            if (_agent.enabled && _agent.isOnNavMesh)
                _agent.Move(_knockbackVelocity * step);
        }

        private void React(in DamageInfo info)
        {
            var settings = HitFeedback.Settings;
            if (settings == null || State == EnemyState.Dead || State == EnemyState.Consumed)
                return;
            var kind = HitFeedback.ClassifyForPlayer(info, _health);
            if (kind == HitFeedbackKind.None || kind == HitFeedbackKind.CreatureHurt)
                return;
            var level = settings.Level(kind);
            var direction = Flat(info.Direction);
            if (direction.sqrMagnitude < 1e-4f && info.Attacker != null)
                direction = Flat(transform.position - info.Attacker.transform.position);
            if (direction.sqrMagnitude > 1e-4f && level.Knockback > 0f)
            {
                _knockbackRemaining = settings.KnockbackDuration;
                _knockbackVelocity = direction.normalized * (level.Knockback * _definition.KnockbackScale / settings.KnockbackDuration);
            }
            if (_animator != null)
                _animator.Flinch(direction, level.Flinch, settings.FlinchDuration);
        }

        private void UpdateProtection() =>
            _health.SetProtection(_consumable != null && FeedProtection ? Consumable.GrabThreshold : 0f, _definition.FeedableDamageTaken);

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
                if (now >= _readyAt[i] && attacks[i].InRange(distance) && AttackAllowed(i))
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        protected virtual bool AttackAllowed(int index) => true;

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
            var destination = State == EnemyState.Circle ? CircleDestination(targetPosition, dt) : ChaseDestination(targetPosition);
            if ((Flat(destination) - Flat(transform.position)).sqrMagnitude < 0.04f)
            {
                _agent.isStopped = true;
            }
            else
            {
                _agent.isStopped = false;
                _agent.SetDestination(destination);
            }
            var look = FaceTarget(distance) ? targetPosition - transform.position : _agent.velocity;
            Face(look, dt);
        }

        protected virtual bool FaceTarget(float distance) => distance < _definition.CircleRange.y + 1f;

        protected Vector3 Position => transform.position;

        protected static bool TrySampleNavMesh(Vector3 position, float range, out Vector3 result)
        {
            if (NavMesh.SamplePosition(position, out var hit, range, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
            result = position;
            return false;
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
            PlaySound(_definition.Attacks[index].WindUpSound);
            if (_definition.Voice != null)
                EnemyVoices.Play(_definition.Voice.Attack, transform.position);
        }

        private void PlaySound(EventReference sound)
        {
            if (!sound.IsNull)
                RuntimeManager.PlayOneShot(sound, transform.position);
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
                        if (!AttackMath.PlacedAtTarget(attack))
                            UpdateAreaCenter();
                    }
                    Face(_attackForward, dt);
                    if (_glow != null)
                        _glow.Show(attack.Telegraph, progress);
                    ShowIndicator(attack, progress, target);
                    break;
                case AttackPhase.Active:
                    if (!_activeStarted)
                    {
                        _activeStarted = true;
                        _lastSweepT = 0f;
                        _hitIndex = 0;
                        PlaySound(attack.StrikeSound);
                        OnActiveStart(attack);
                    }
                    var activeProgress = AttackMath.ActiveProgress(attack, elapsed);
                    var hits = Mathf.Max(1, attack.Hits);
                    var hitIndex = Mathf.Min(hits - 1, (int)(activeProgress * hits));
                    if (hitIndex > _hitIndex)
                    {
                        _hitIndex = hitIndex;
                        _hitThisAttack.Clear();
                        _lastSweepT = 0f;
                        if (attack.Kind == AttackKind.Area)
                            AreaHit(attack);
                    }
                    if (attack.Kind == AttackKind.Melee)
                        Sweep(attack, hits == 1 ? activeProgress : Mathf.Clamp01(activeProgress * hits - hitIndex));
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
                case AttackKind.Trap:
                    if (attack.TrapPrefab != null)
                        EnemyManager.PlaceTrap(attack.TrapPrefab, _areaCenter, attack.AreaRadius, attack.Status, attack.StatusMagnitude, attack.StatusDuration, attack.TrapLifetime);
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

        private void ShowIndicator(EnemyAttack attack, float progress, Health target)
        {
            if (_indicator == null || !attack.ShowsGroundIndicator(GroundIndicator.ForceAll))
                return;
            var locked = AttackMath.IsLocked(attack, progress);
            switch (attack.Kind)
            {
                case AttackKind.Area:
                case AttackKind.Trap:
                    _indicator.ShowCircle(_areaCenter, attack.AreaRadius, attack.Telegraph, progress, locked);
                    break;
                case AttackKind.Melee:
                    _indicator.ShowSector(transform.position, _attackForward, attack.Reach + attack.SweepRadius, attack.Arc, attack.Telegraph, progress, locked);
                    break;
                case AttackKind.Projectile:
                {
                    var origin = MuzzleOrigin(attack);
                    var ground = new Vector3(origin.x, transform.position.y, origin.z);
                    var wall = Physics.Raycast(origin, _attackForward, out var hit, attack.MaxRange, EnemyLayers.WallMask, QueryTriggerInteraction.Ignore) ? hit.distance : attack.MaxRange;
                    var toTarget = target != null ? Flat(target.transform.position - ground).magnitude : attack.MaxRange;
                    var length = AttackMath.LaneLength(attack.MaxRange, wall, toTarget, LaneOvershoot);
                    _indicator.ShowLane(ground, _attackForward, attack.LaneWidth, length, attack.Telegraph, progress, locked);
                    break;
                }
            }
        }

        private Vector3 MuzzleOrigin(EnemyAttack attack) =>
            _muzzle != null ? _muzzle.position : transform.position + Vector3.up * attack.Height;

        private void Fire(EnemyAttack attack)
        {
            if (attack.Projectile == null)
                return;
            var origin = MuzzleOrigin(attack);
            var aim = CombatTargets.Creature != null ? AttackMath.LockedAim(_attackForward, origin, CombatTargets.CreatureAimPoint) : _attackForward;
            var info = Damage(attack, origin, aim);
            EnemyManager.FireProjectile(attack.Projectile, origin, aim * attack.ProjectileSpeed, info, attack.ProjectileLifetime);
        }

        private void Deliver(EnemyAttack attack, Collider collider, Vector3 point, Vector3 direction)
        {
            if (!Hurtbox.TryGet(collider, out var hurtbox) || hurtbox.Health == _health || _hitThisAttack.Contains(hurtbox.Health))
                return;
            var result = hurtbox.Health.TakeDamage(Damage(attack, point, direction));
            if (result.Outcome == DamageOutcome.Ignored)
                return;
            _hitThisAttack.Add(hurtbox.Health);
            PlaySound(attack.HitSound);
        }

        private DamageInfo Damage(EnemyAttack attack, Vector3 point, Vector3 direction)
        {
            var info = DamageInfo.Create(attack.Damage, Team.Site, attack.Type, attack.Stagger);
            info.Flags = attack.Flags;
            info.StunDuration = attack.StunDuration;
            info.Status = attack.Status;
            info.StatusMagnitude = attack.StatusMagnitude;
            info.StatusDuration = attack.StatusDuration;
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

        private void UpdateAreaCenter()
        {
            var attack = CurrentAttack;
            var creature = CombatTargets.Creature;
            if (attack != null && attack.Kind == AttackKind.Trap && creature != null)
            {
                var offset = Flat(creature.transform.position - transform.position);
                _areaCenter = transform.position + Vector3.ClampMagnitude(offset, attack.MaxRange);
                return;
            }
            _areaCenter = transform.position + _attackForward * (attack?.AreaForwardOffset ?? 0f);
        }

        private void OnDied(Health health)
        {
            if (_definition.Voice != null && State != EnemyState.Consumed)
                EnemyVoices.Play(_definition.Voice.Death, transform.position, true);
            EndStaggerBreak();
            EndAttack(false);
            if (_agent.enabled)
                Halt();
            _agent.enabled = false;
            var consumed = State == EnemyState.Consumed;
            SetState(EnemyState.Dead);
            OnDeath();
            if (!consumed && _health.BurstsOnDeath)
            {
                if (GameSettings.Gore)
                    HideBody();
                else
                    _dissolveAt = Time.time + Mathf.Max(0f, _definition.DespawnDelay - DissolveTime);
            }
            Destroy(gameObject, _definition.DespawnDelay);
        }

        protected virtual void OnDeath() { }

        private void HideBody()
        {
            GetComponentsInChildren(true, BodyRenderers);
            foreach (var body in BodyRenderers)
                body.enabled = false;
            BodyRenderers.Clear();
        }

        private void OnDamaged(Health health, in DamageInfo info, in DamageResult result)
        {
            if (_glow != null)
                _glow.Flash(HitFlashDuration);
            if (health.IsAlive)
                React(info);
            var voice = _definition.Voice;
            if (voice != null && health.IsAlive && result.HealthDamage > 0f && Time.time >= _nextPain)
            {
                _nextPain = Time.time + voice.PainCooldown;
                EnemyVoices.Play(voice.Pain, transform.position);
            }
            if (health.IsAlive && State != EnemyState.Stunned && State != EnemyState.Dead && _stagger.Add(result.StaggerDamage))
                BreakStagger();
            TickFeedback();
        }

        private void OnGrabbed(Consumable consumable)
        {
            if (_definition.Voice != null)
                EnemyVoices.Play(_definition.Voice.Grabbed, transform.position, true);
            EndAttack(false);
            EndStaggerBreak();
            Halt();
            _agent.enabled = false;
            foreach (var collider in GetComponentsInChildren<Collider>())
                collider.enabled = false;
            SetState(EnemyState.Consumed);
        }

        private void BreakStagger()
        {
            if (_definition.Voice != null)
                EnemyVoices.Play(_definition.Voice.Stagger, transform.position, true);
            Stun(_definition.StaggerStun);
            _staggerBroken = true;
            _health.IsStaggered = true;
            _health.Incoming.Add(_staggerVulnerability);
            if (_glow != null)
                _glow.ShowStunned();
            Staggered?.Invoke(this);
            CombatEvents.RaiseStaggerBroken(_health);
        }

        private void EndStaggerBreak()
        {
            if (!_staggerBroken)
                return;
            _staggerBroken = false;
            _health.IsStaggered = false;
            _health.Incoming.Remove(_staggerVulnerability);
            if (_glow != null)
                _glow.ClearStunned();
        }

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
            if (State == EnemyState.Spawning && state is EnemyState.Chase or EnemyState.Circle or EnemyState.Attacking && _definition.Voice != null)
                EnemyVoices.Play(_definition.Voice.Alert, transform.position);
            State = state;
            _stateStarted = Time.time;
            StateChanged?.Invoke(this);
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
