using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using TentacleVania.Core.Saves;
using TentacleVania.Weapons.Data;
using UnityEngine;

namespace TentacleVania.Weapons.Behaviours
{
    public sealed class ArcTendrils : MonoBehaviour
    {
        private const int MaxTrail = 10;
        private static readonly Collider[] Hits = new Collider[24];

        private struct Node
        {
            public Vector3 Position;
            public float Until;
        }

        [SerializeField] private WeaponController _weapons;
        [SerializeField] private string _weaponId = "ArcTendrils";
        [SerializeField] private LayerMask _hitLayers;
        [SerializeField] private Transform _fieldPrefab;
        [SerializeField, Min(0.1f)] private float _fieldDuration = 4f;
        [SerializeField, Min(0.1f)] private float _fieldRadius = 3f;
        [SerializeField, Min(0.05f)] private float _tick = 0.35f;
        [SerializeField, Min(0f)] private float _tickDamage = 2f;
        [SerializeField, Min(0f)] private float _machineRoot = 1f;
        [SerializeField, Min(0.1f)] private float _trailLife = 3f;
        [SerializeField, Min(0.05f)] private float _trailSpacing = 0.1f;
        [SerializeField, Min(0.1f)] private float _trailRadius = 1f;
        [Tooltip("Static Overload Chain makes chain lightning add to each enemy it hits.")]
        [SerializeField, Min(0f)] private float _chainStatic = 25f;

        private readonly Node[] _trail = new Node[MaxTrail];
        private Health _health;
        private Transform _field;
        private Vector3 _fieldCenter;
        private float _fieldUntil;
        private float _nextFieldTick;
        private float _trailUntil;
        private float _nextNode;
        private float _nextTrailTick;
        private int _trailCursor;
        private bool _chainSet;

        private void Awake()
        {
            _health = GetComponent<Health>();
            if (_fieldPrefab != null)
            {
                _field = Instantiate(_fieldPrefab);
                _field.gameObject.SetActive(false);
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
            if (_chainSet)
                StatusEffects.ChainAddsStatic = 0f;
            _chainSet = false;
        }

        private void OnDestroy()
        {
            if (_field != null)
                Destroy(_field.gameObject);
        }

        private bool Equipped(out WeaponModifier modifier)
        {
            modifier = null;
            for (var slot = 0; slot < 2; slot++)
                if (_weapons.Weapon(slot) != null && ContentCatalog.IdOf(_weapons.Weapon(slot)) == _weaponId)
                {
                    modifier = _weapons.Modifier(slot);
                    return true;
                }
            return false;
        }

        private void OnAttackStarted(WeaponController controller)
        {
            if (controller.ActiveSlot < 0 || ContentCatalog.IdOf(controller.Weapon(controller.ActiveSlot)) != _weaponId)
                return;
            var now = Time.time;
            if (controller.ActiveKind == AttackKind.Heavy)
            {
                _fieldCenter = transform.position;
                _fieldUntil = now + _fieldDuration;
                _nextFieldTick = now;
                if (_field != null)
                {
                    _field.position = _fieldCenter;
                    _field.localScale = new Vector3(_fieldRadius * 2f, 1f, _fieldRadius * 2f);
                    _field.gameObject.SetActive(true);
                }
            }
            else if (controller.ActiveKind == AttackKind.DashAttack)
            {
                _trailUntil = now + 0.35f;
                _nextNode = now;
            }
        }

        private void Update()
        {
            if (_weapons == null)
                return;
            var now = Time.time;
            var equipped = Equipped(out var modifier);
            var chain = equipped && modifier != null && modifier.Kind == ModifierKind.OverloadChain;
            if (chain != _chainSet)
            {
                _chainSet = chain;
                StatusEffects.ChainAddsStatic = chain ? _chainStatic : 0f;
            }
            if (now < _fieldUntil && now >= _nextFieldTick)
            {
                _nextFieldTick = now + _tick;
                Zap(_fieldCenter, _fieldRadius, modifier != null && modifier.Kind == ModifierKind.ConductiveField);
            }
            if (_field != null && _field.gameObject.activeSelf && now >= _fieldUntil)
                _field.gameObject.SetActive(false);
            if (now < _trailUntil && now >= _nextNode)
            {
                _nextNode = now + _trailSpacing;
                _trail[_trailCursor] = new Node { Position = transform.position, Until = now + _trailLife };
                _trailCursor = (_trailCursor + 1) % MaxTrail;
            }
            if (now < _nextTrailTick)
                return;
            _nextTrailTick = now + _tick;
            for (var i = 0; i < MaxTrail; i++)
                if (_trail[i].Until > now)
                    Zap(_trail[i].Position, _trailRadius, false);
        }

        private void Zap(Vector3 center, float radius, bool rootMachines)
        {
            var count = Physics.OverlapSphereNonAlloc(center + Vector3.up * 0.5f, radius, Hits, _hitLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Hits[i], out var hurtbox) || hurtbox.Health == _health)
                    continue;
                var target = hurtbox.Health;
                var info = DamageInfo.Create(_tickDamage, Team.Creature, DamageType.Electric);
                info.Attacker = _health;
                info.Point = target.transform.position;
                info.Direction = Vector3.up;
                if (rootMachines && !target.Bleeds)
                {
                    info.Status = StatusKind.Root;
                    info.StatusDuration = _machineRoot;
                }
                target.TakeDamage(info);
            }
        }
    }
}
