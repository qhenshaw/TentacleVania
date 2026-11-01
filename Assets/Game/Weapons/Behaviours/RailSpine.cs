using System.Collections.Generic;
using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Saves;
using TentacleVania.Creature.Movement;
using TentacleVania.Weapons.Data;
using UnityEngine;

namespace TentacleVania.Weapons.Behaviours
{
    public sealed class RailSpine : MonoBehaviour
    {
        private const int Lines = 6;
        private const float Height = 1.1f;
        private static readonly RaycastHit[] Hits = new RaycastHit[16];
        private static readonly Collider[] Nearby = new Collider[24];

        [SerializeField] private WeaponController _weapons;
        [SerializeField] private CreatureMotor _motor;
        [SerializeField] private string _weaponId = "RailSpine";
        [SerializeField] private LayerMask _hitLayers;
        [SerializeField] private LayerMask _wallLayers;
        [SerializeField] private Material _lineMaterial;
        [SerializeField, Min(1f)] private float _range = 14f;
        [SerializeField, Min(1f)] private float _sniperRange = 22f;
        [SerializeField, Min(1f)] private float _sniperScale = 3f;
        [Tooltip("Sniper crits (this multiplier) on staggered enemies: the placeholder for weak points.")]
        [SerializeField, Min(1f)] private float _critScale = 1.5f;
        [SerializeField, Min(0)] private int _bounces = 3;
        [SerializeField, Min(1f)] private float _bounceRange = 6f;
        [SerializeField, Min(0.01f)] private float _doubleDelay = 0.12f;
        [SerializeField, Min(0f)] private float _retreatDistance = 3f;
        [SerializeField, Min(0.05f)] private float _retreatTime = 0.18f;
        [SerializeField, Min(0f)] private float _pinTime = 1.5f;
        [SerializeField, Min(0.01f)] private float _lineTime = 0.08f;
        [SerializeField] private EventReference _shotSound;

        private readonly LineRenderer[] _lines = new LineRenderer[Lines];
        private readonly float[] _lineUntil = new float[Lines];
        private readonly List<Health> _visited = new(8);
        private Health _health;
        private float _fireAt = -1f;
        private float _secondAt = -1f;
        private AttackKind _kind;
        private bool _finisher;
        private int _lineCursor;

        public int LastHits { get; private set; }

        private void Awake()
        {
            _health = GetComponent<Health>();
            for (var i = 0; i < Lines; i++)
            {
                var line = new GameObject("RailLine").AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.widthMultiplier = 0.07f;
                line.sharedMaterial = _lineMaterial;
                line.startColor = line.endColor = new Color(2.5f, 2.2f, 1.6f, 1f);
                line.enabled = false;
                _lines[i] = line;
            }
        }

        private void OnEnable()
        {
            if (_weapons != null)
                _weapons.AttackStarted += OnAttackStarted;
        }

        private void OnDisable()
        {
            if (_weapons != null)
                _weapons.AttackStarted -= OnAttackStarted;
        }

        private void OnDestroy()
        {
            foreach (var line in _lines)
                if (line != null)
                    Destroy(line.gameObject);
        }

        private void OnAttackStarted(WeaponController controller)
        {
            if (controller.ActiveSlot < 0 || ContentCatalog.IdOf(controller.Weapon(controller.ActiveSlot)) != _weaponId)
                return;
            _kind = controller.ActiveKind;
            _finisher = controller.IsFinisher;
            _fireAt = Time.time + controller.MoveWindUp;
            _secondAt = _kind == AttackKind.Combo && !_finisher && controller.ActiveStep == 1 ? _fireAt + _doubleDelay : -1f;
            if (_kind == AttackKind.DashAttack && _motor != null)
                _motor.Lunge(-controller.ActiveForward, _retreatDistance, _retreatTime);
        }

        private void Update()
        {
            var now = Time.time;
            for (var i = 0; i < Lines; i++)
                if (_lines[i].enabled && now >= _lineUntil[i])
                    _lines[i].enabled = false;
            if (_fireAt >= 0f && now >= _fireAt)
            {
                _fireAt = -1f;
                Fire();
            }
            if (_secondAt >= 0f && now >= _secondAt)
            {
                _secondAt = -1f;
                Fire();
            }
        }

        private void Fire()
        {
            if (_weapons.ActiveSlot < 0)
                return;
            var weapon = _weapons.Weapon(_weapons.ActiveSlot);
            var modifier = _weapons.Modifier(_weapons.ActiveSlot);
            var origin = transform.position + Vector3.up * Height;
            var forward = _weapons.ActiveForward;
            if (!_shotSound.IsNull)
                RuntimeManager.PlayOneShot(_shotSound, origin);
            LastHits = 0;
            if (_kind == AttackKind.Heavy)
            {
                Sniper(origin, forward, weapon.Damage * _sniperScale, weapon.Stagger * _sniperScale, modifier);
                return;
            }
            var first = Shoot(origin, forward, _range, weapon.Damage, weapon.Stagger, out var end);
            Line(origin, end);
            if (!_finisher || first == null)
                return;
            Ricochet(first, weapon, modifier);
        }

        private Health Shoot(Vector3 origin, Vector3 direction, float range, float damage, float stagger, out Vector3 end)
        {
            var wall = Physics.Raycast(origin, direction, out var wallHit, range, _wallLayers, QueryTriggerInteraction.Ignore) ? wallHit.distance : range;
            end = origin + direction * wall;
            var count = Physics.SphereCastNonAlloc(origin, 0.25f, direction, Hits, wall, _hitLayers, QueryTriggerInteraction.Collide);
            Health best = null;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Hits[i].collider, out var hurtbox) || hurtbox.Health == _health || !hurtbox.Health.IsAlive || Hits[i].distance >= bestDistance)
                    continue;
                best = hurtbox.Health;
                bestDistance = Hits[i].distance;
            }
            if (best == null)
                return null;
            end = origin + direction * bestDistance;
            Strike(best, damage, stagger, direction, HitFlags.None, StatusKind.None);
            return best;
        }

        private void Sniper(Vector3 origin, Vector3 direction, float damage, float stagger, WeaponModifier modifier)
        {
            var wall = Physics.Raycast(origin, direction, out var wallHit, _sniperRange, _wallLayers, QueryTriggerInteraction.Ignore) ? wallHit.distance : _sniperRange;
            Line(origin, origin + direction * wall);
            var count = Physics.SphereCastNonAlloc(origin, 0.3f, direction, Hits, wall, _hitLayers, QueryTriggerInteraction.Collide);
            _visited.Clear();
            var pin = modifier != null && modifier.Kind == ModifierKind.PinningSpines;
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Hits[i].collider, out var hurtbox) || hurtbox.Health == _health || _visited.Contains(hurtbox.Health))
                    continue;
                var target = hurtbox.Health;
                _visited.Add(target);
                var crit = target.IsStaggered ? _critScale : 1f;
                Strike(target, damage * crit, stagger, direction, HitFlags.Heavy, pin ? StatusKind.Root : StatusKind.None);
            }
        }

        private void Ricochet(Health first, WeaponDefinition weapon, WeaponModifier modifier)
        {
            _visited.Clear();
            _visited.Add(first);
            var current = first;
            var bounces = _bounces;
            var endless = modifier != null && modifier.Kind == ModifierKind.EndlessRicochet;
            if (endless && !first.IsAlive)
                bounces++;
            for (var b = 0; b < bounces; b++)
            {
                var next = Nearest(current.transform.position);
                if (next == null)
                    return;
                _visited.Add(next);
                var from = current.transform.position + Vector3.up * Height;
                var to = next.transform.position + Vector3.up * Height;
                Line(from, to);
                var direction = to - from;
                direction.y = 0f;
                Strike(next, weapon.Damage, weapon.Stagger, direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.forward, HitFlags.Finisher, StatusKind.None);
                if (endless && !next.IsAlive)
                    bounces++;
                current = next;
            }
        }

        private Health Nearest(Vector3 from)
        {
            var count = Physics.OverlapSphereNonAlloc(from + Vector3.up, _bounceRange, Nearby, _hitLayers, QueryTriggerInteraction.Collide);
            Health best = null;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Nearby[i], out var hurtbox) || hurtbox.Health == _health || !hurtbox.Health.IsAlive || _visited.Contains(hurtbox.Health))
                    continue;
                var distance = (hurtbox.Health.transform.position - from).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;
                best = hurtbox.Health;
                bestDistance = distance;
            }
            return best;
        }

        private void Strike(Health target, float damage, float stagger, Vector3 direction, HitFlags flags, StatusKind status)
        {
            var info = DamageInfo.Create(damage, Team.Creature, DamageType.Physical, stagger);
            info.Flags = flags;
            info.Attacker = _health;
            info.Point = target.transform.position + Vector3.up * Height;
            info.Direction = direction;
            if (status != StatusKind.None)
            {
                info.Status = status;
                info.StatusDuration = _pinTime;
            }
            if (target.TakeDamage(info).Outcome != DamageOutcome.Ignored)
                LastHits++;
        }

        private void Line(Vector3 from, Vector3 to)
        {
            var line = _lines[_lineCursor];
            _lineUntil[_lineCursor] = Time.time + _lineTime;
            _lineCursor = (_lineCursor + 1) % Lines;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.enabled = true;
        }
    }
}
