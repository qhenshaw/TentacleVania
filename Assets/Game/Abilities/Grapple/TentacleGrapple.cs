using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Input;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using TentacleVania.Creature.Movement;
using UnityEngine;

namespace TentacleVania.Abilities.Grapple
{
    [RequireComponent(typeof(CreatureMotor))]
    public sealed class TentacleGrapple : MonoBehaviour
    {
        private enum Phase
        {
            Idle,
            Shooting,
            Zipping,
        }

        [SerializeField] private GrappleDefinition _definition;
        [SerializeField] private LayerMask _enemyLayers;
        [SerializeField] private LayerMask _blockerLayers = 1;
        [SerializeField] private LineRenderer _line;
        [SerializeField] private Transform _origin;

        private readonly Collider[] _overlaps = new Collider[24];
        private readonly RaycastHit[] _hits = new RaycastHit[8];
        private CreatureMotor _motor;
        private CharacterController _controller;
        private Health _health;
        private Phase _phase;
        private float _phaseStarted;
        private float _readyAt;
        private float _zipDuration;
        private Vector3 _start;
        private Vector3 _end;
        private GrappleAnchor _anchor;
        private Health _enemy;

        public static bool ForceUnlocked { get; set; }
        public bool GrabOnArrival { get; set; }
        public bool IsUnlocked => ForceUnlocked || (_definition != null && BossProgress.IsAbilityUnlocked(ContentCatalog.IdOf(_definition)));
        public bool IsBusy => _phase != Phase.Idle;
        public float CooldownRemaining => Mathf.Max(0f, _readyAt - Time.time);
        public GrappleDefinition Definition => _definition;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ForceUnlocked = false;

        private void Awake()
        {
            _motor = GetComponent<CreatureMotor>();
            _controller = GetComponent<CharacterController>();
            _health = GetComponent<Health>();
            if (_line != null)
                _line.enabled = false;
        }

        private void OnDisable()
        {
            if (_phase != Phase.Idle)
                End(false);
        }

        private void Update()
        {
            if (_definition == null)
                return;
            switch (_phase)
            {
                case Phase.Idle:
                    if (GameplayInput.Default.Traverse.Pressed && _motor.InputEnabled && !Core.Interaction.Traversals.ClaimedThisFrame)
                        TryGrapple();
                    break;
                case Phase.Shooting:
                    TickShoot();
                    break;
                case Phase.Zipping:
                    TickZip();
                    break;
            }
        }

        public bool TryGrapple()
        {
            if (_phase != Phase.Idle || !IsUnlocked || Time.time < _readyAt || (_health != null && !_health.IsAlive))
                return false;
            var aim = _motor.TryGetAimDirection(out var direction) ? direction : transform.forward;
            if (!PickTarget(aim))
                return false;
            _phase = Phase.Shooting;
            _phaseStarted = Time.time;
            _motor.LockControl();
            _motor.FaceImmediately(TargetPoint() - transform.position);
            Play(_definition.ShootSound);
            if (_line != null)
                _line.enabled = true;
            return true;
        }

        private bool PickTarget(Vector3 aim)
        {
            _anchor = null;
            _enemy = null;
            var origin = transform.position;
            var best = float.MaxValue;
            foreach (var anchor in GrappleAnchor.All)
            {
                if (GrappleRules.TryScore(origin, aim, anchor.Point, _definition.Range, _definition.MaxAimAngle, out var angle) && angle < best)
                {
                    best = angle;
                    _anchor = anchor;
                }
            }
            var count = Physics.OverlapSphereNonAlloc(origin, _definition.Range, _overlaps, _enemyLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(_overlaps[i], out var hurtbox) || hurtbox.Health == null || hurtbox.Health == _health)
                    continue;
                var target = hurtbox.Health;
                if (!target.IsAlive || target.Team == Team.Creature)
                    continue;
                if (!GrappleRules.TryScore(origin, aim, target.transform.position, _definition.Range, _definition.MaxAimAngle, out var angle) || angle >= best)
                    continue;
                if (Blocked(origin, target.transform.position))
                    continue;
                best = angle;
                _enemy = target;
                _anchor = null;
            }
            return _anchor != null || _enemy != null;
        }

        private bool Blocked(Vector3 from, Vector3 to)
        {
            var offset = to - from;
            offset.y = 0f;
            var distance = offset.magnitude;
            if (distance < 1e-3f)
                return false;
            var origin = from + Vector3.up * 0.5f;
            var count = Physics.RaycastNonAlloc(origin, offset / distance, _hits, distance, _blockerLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
                if (_hits[i].collider.GetComponent<ChasmEdge>() != null)
                    return true;
            return false;
        }

        private Vector3 TargetPoint() => _anchor != null ? _anchor.Point : _enemy != null ? _enemy.transform.position + Vector3.up : transform.position;

        private Vector3 Destination()
        {
            if (_anchor != null)
                return _anchor.Landing;
            if (_enemy == null)
                return transform.position;
            var away = transform.position - _enemy.transform.position;
            away.y = 0f;
            var direction = away.sqrMagnitude > 1e-4f ? away.normalized : -transform.forward;
            return _enemy.transform.position + direction * _definition.EnemyStopDistance;
        }

        private void TickShoot()
        {
            var t = Mathf.Clamp01((Time.time - _phaseStarted) / _definition.ShootTime);
            DrawLine(t);
            if (_enemy != null && !_enemy.IsAlive)
            {
                End(true);
                return;
            }
            if (t < 1f)
                return;
            _phase = Phase.Zipping;
            _phaseStarted = Time.time;
            _start = transform.position;
            _end = Destination();
            _zipDuration = GrappleRules.ZipDuration(Vector3.Distance(_start, _end), _definition.ZipSpeed, _definition.ZipDuration);
            _motor.Suspended = true;
            _motor.GrantInvulnerability(_zipDuration + 0.05f);
            if (_controller != null)
                _controller.enabled = false;
            Play(_definition.ZipSound);
        }

        private void TickZip()
        {
            var t = Mathf.Clamp01((Time.time - _phaseStarted) / _zipDuration);
            if (_enemy != null)
                _end = Destination();
            var height = _anchor != null ? _definition.AnchorArc : _definition.EnemyArc;
            transform.position = GrappleRules.Arc(_start, _end, t, height);
            DrawLine(1f);
            if (t >= 1f)
                End(true);
        }

        private void DrawLine(float extend)
        {
            if (_line == null)
                return;
            var from = _origin != null ? _origin.position : transform.position + Vector3.up * 0.6f;
            _line.SetPosition(0, from);
            _line.SetPosition(1, Vector3.Lerp(from, TargetPoint(), extend));
        }

        private void End(bool latched)
        {
            var arrivedAt = latched && _phase == Phase.Zipping ? _enemy : null;
            if (_phase == Phase.Zipping)
            {
                if (_controller != null)
                    _controller.enabled = true;
                _motor.Suspended = false;
            }
            if (_phase != Phase.Idle)
                _motor.UnlockControl();
            if (latched)
                Play(_definition.LatchSound);
            if (_line != null)
                _line.enabled = false;
            _phase = Phase.Idle;
            _readyAt = Time.time + _definition.Cooldown;
            _anchor = null;
            _enemy = null;
            if (GrabOnArrival && arrivedAt != null && arrivedAt.TryGetComponent<Consumable>(out var victim) && TryGetComponent<Creature.CreatureConsume>(out var consume))
                consume.GrabTarget(victim);
        }

        private void Play(EventReference sound)
        {
            if (!sound.IsNull)
                RuntimeManager.PlayOneShot(sound, transform.position);
        }
    }
}
