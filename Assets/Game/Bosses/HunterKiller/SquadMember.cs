using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using TentacleVania.Enemies;
using TentacleVania.Enemies.Traps;
using UnityEngine;

namespace TentacleVania.Bosses.HunterKiller
{
    public sealed class SquadMember : BossEnemy
    {
        public enum SquadRole
        {
            Sniper,
            Heavy,
            Medic,
            Tracker,
        }

        [Header("Role (design: Hunter-Killer Squad, four specialists fought together)")]
        [SerializeField] private SquadRole _role;

        [Header("Sniper (red: break line of sight)")]
        [SerializeField] private LineRenderer _sight;
        [SerializeField, Min(1f)] private float _shotInterval = 6f;
        [SerializeField, Min(0.3f)] private float _aimTime = 1.6f;
        [SerializeField, Min(0f)] private float _shotDamage = 34f;
        [SerializeField] private LayerMask _cover;
        [SerializeField] private EventReference _shotSound;

        [Header("Medic")]
        [SerializeField, Min(1f)] private float _healInterval = 5f;
        [SerializeField, Range(0f, 1f)] private float _healFraction = 0.08f;
        [SerializeField, Range(0f, 1f)] private float _healBelow = 0.9f;
        [SerializeField, Min(1f)] private float _healRange = 14f;

        [Header("Tracker (nets: ground indicators)")]
        [SerializeField] private AcidBurst _net;
        [SerializeField, Min(1f)] private float _netInterval = 7f;
        [SerializeField, Min(0.1f)] private float _netRadius = 2f;
        [SerializeField, Min(0.3f)] private float _netFuse = 1f;
        [SerializeField, Min(0f)] private float _netDamage = 6f;
        [SerializeField, Min(0.1f)] private float _netRoot = 1.6f;

        private float _next;
        private float _aimUntil = -1f;
        private float _lineUntil = -1f;

        public SquadRole Role => _role;
        public int Shots { get; private set; }
        public int ShotsBlocked { get; private set; }
        public int Heals { get; private set; }
        public int Nets { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (_net != null)
                EnemyManager.PrewarmBursts(_net, 2);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Shots = ShotsBlocked = Heals = Nets = 0;
            _aimUntil = _lineUntil = -1f;
            _next = Time.time + Interval * 0.6f;
            if (_sight != null)
                _sight.enabled = false;
        }

        private float Interval => _role switch
        {
            SquadRole.Sniper => _shotInterval,
            SquadRole.Medic => _healInterval,
            SquadRole.Tracker => _netInterval,
            _ => float.MaxValue,
        };

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            if (_sight != null && _lineUntil >= 0f && now >= _lineUntil)
            {
                _lineUntil = -1f;
                _sight.enabled = false;
            }
            if (!Health.IsAlive || State == EnemyState.Dead || State == EnemyState.Consumed || State == EnemyState.Spawning || IsFinishing || State == EnemyState.Stunned)
            {
                CancelAim();
                return;
            }
            var creature = CombatTargets.Creature;
            if (creature == null || !creature.IsAlive)
                return;
            if (_aimUntil >= 0f)
            {
                Aim(now, creature);
                return;
            }
            if (now < _next)
                return;
            _next = now + Interval;
            switch (_role)
            {
                case SquadRole.Sniper:
                    _aimUntil = now + _aimTime;
                    Halt();
                    break;
                case SquadRole.Medic:
                    HealMostHurt();
                    break;
                case SquadRole.Tracker:
                    ThrowNet(creature.transform.position);
                    break;
            }
        }

        private Vector3 Muzzle => Position + Vector3.up * 1.4f;

        private void Aim(float now, Health creature)
        {
            var target = creature.transform.position + Vector3.up;
            var blocked = Physics.Linecast(Muzzle, target, out var hit, _cover, QueryTriggerInteraction.Ignore);
            var end = blocked ? hit.point : target;
            var look = target - Position;
            look.y = 0f;
            if (look.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.LookRotation(look);
            if (_sight != null)
            {
                _sight.enabled = true;
                _sight.SetPosition(0, Muzzle);
                _sight.SetPosition(1, end);
                _sight.widthMultiplier = now >= _aimUntil - 0.25f ? 0.08f : 0.03f;
            }
            if (now < _aimUntil)
                return;
            _aimUntil = -1f;
            _lineUntil = now + 0.12f;
            Shots++;
            if (!_shotSound.IsNull)
                RuntimeManager.PlayOneShot(_shotSound, Muzzle);
            if (blocked)
            {
                ShotsBlocked++;
                return;
            }
            var info = DamageInfo.Create(_shotDamage, Team.Site, DamageType.Physical, _shotDamage);
            info.Flags = HitFlags.Undodgeable;
            info.Attacker = Health;
            info.Point = target;
            info.Direction = (target - Muzzle).normalized;
            creature.TakeDamage(info);
        }

        private void CancelAim()
        {
            _aimUntil = -1f;
            if (_sight != null && _lineUntil < 0f)
                _sight.enabled = false;
        }

        private void HealMostHurt()
        {
            BossEnemy best = null;
            var lowest = _healBelow;
            foreach (var member in Members)
            {
                if (member == null || member == this || member.Boss != Boss || !member.Health.IsAlive || member.Health.Normalized >= lowest)
                    continue;
                if ((member.transform.position - Position).sqrMagnitude > _healRange * _healRange)
                    continue;
                lowest = member.Health.Normalized;
                best = member;
            }
            if (best == null)
                return;
            best.Health.Heal(best.Health.Max * _healFraction);
            Heals++;
            ChainLightningLines.Draw(Muzzle, best.transform.position + Vector3.up);
        }

        private void ThrowNet(Vector3 target)
        {
            if (_net == null)
                return;
            var info = DamageInfo.Create(_netDamage, Team.Site, DamageType.Physical, 0f);
            info.Attacker = Health;
            info.Status = StatusKind.Root;
            info.StatusDuration = _netRoot;
            EnemyManager.PlaceBurst(_net, target, _netRadius, _netFuse, info);
            Nets++;
        }
    }
}
