using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using TentacleVania.Core.Saves;
using TentacleVania.Weapons.Data;
using UnityEngine;

namespace TentacleVania.Weapons.Behaviours
{
    public sealed class FlameGland : MonoBehaviour
    {
        private const int MaxGround = 6;
        private static readonly Collider[] Hits = new Collider[24];

        private struct Ground
        {
            public Vector3 Position;
            public float Until;
            public Transform Body;
        }

        [SerializeField] private WeaponController _weapons;
        [SerializeField] private string _weaponId = "FlameGland";
        [SerializeField] private LayerMask _hitLayers;
        [Tooltip("Ignite: a burning target hit by the finisher explodes for this much Fire damage, splashing others in the radius.")]
        [SerializeField, Min(0f)] private float _igniteDamage = 30f;
        [SerializeField, Min(0.1f)] private float _igniteRadius = 2f;
        [SerializeField, Min(0f)] private float _dashBurstDamage = 14f;
        [SerializeField, Min(0.1f)] private float _dashBurstRadius = 2.5f;
        [SerializeField] private Transform _groundPrefab;
        [SerializeField, Min(0.1f)] private float _groundDuration = 4f;
        [SerializeField, Min(0.1f)] private float _groundRadius = 1.5f;
        [SerializeField, Min(0.05f)] private float _groundTick = 0.4f;
        [SerializeField, Min(0f)] private float _groundDamage = 3f;
        [SerializeField] private EventReference _igniteSound;

        private readonly Ground[] _ground = new Ground[MaxGround];
        private Health _health;
        private float _dashBurstAt = -1f;
        private float _nextGroundTick;
        private int _groundCursor;
        private bool _exploding;

        private void Awake()
        {
            _health = GetComponent<Health>();
            for (var i = 0; i < MaxGround; i++)
                if (_groundPrefab != null)
                {
                    _ground[i].Body = Instantiate(_groundPrefab);
                    _ground[i].Body.gameObject.SetActive(false);
                }
        }

        private void OnEnable()
        {
            CombatEvents.HitLanded += OnHit;
            if (_weapons != null)
                _weapons.AttackStarted += OnAttackStarted;
        }

        private void OnDisable()
        {
            CombatEvents.HitLanded -= OnHit;
            if (_weapons != null)
                _weapons.AttackStarted -= OnAttackStarted;
        }

        private void OnDestroy()
        {
            foreach (var ground in _ground)
                if (ground.Body != null)
                    Destroy(ground.Body.gameObject);
        }

        private bool Active => _weapons != null && _weapons.ActiveSlot >= 0 && ContentCatalog.IdOf(_weapons.Weapon(_weapons.ActiveSlot)) == _weaponId;

        private void OnAttackStarted(WeaponController controller)
        {
            if (!Active)
                return;
            if (controller.ActiveKind == AttackKind.DashAttack)
                _dashBurstAt = Time.time + 0.25f;
            else if (controller.ActiveKind == AttackKind.Combo && !controller.IsFinisher)
            {
                var modifier = controller.Modifier(controller.ActiveSlot);
                if (modifier != null && modifier.Kind == ModifierKind.ScorchedEarth)
                    LayGround(transform.position + controller.ActiveForward * 2f);
            }
        }

        private void OnHit(Health target, in DamageInfo info, in DamageResult result)
        {
            if (_exploding || info.Attacker != _health || target == _health || !Active || (info.Flags & HitFlags.Finisher) == 0)
                return;
            if (!target.TryGetComponent<StatusEffects>(out var status) || !status.IsActive(StatusKind.Burn))
                return;
            if (!_igniteSound.IsNull)
                RuntimeManager.PlayOneShot(_igniteSound, target.transform.position);
            _exploding = true;
            Blast(target.transform.position, _igniteRadius, _igniteDamage, HitFlags.Heavy);
            _exploding = false;
        }

        private void Update()
        {
            var now = Time.time;
            if (_dashBurstAt >= 0f && now >= _dashBurstAt)
            {
                _dashBurstAt = -1f;
                Blast(transform.position, _dashBurstRadius, _dashBurstDamage, HitFlags.DashAttack);
            }
            if (now < _nextGroundTick)
                return;
            _nextGroundTick = now + _groundTick;
            for (var i = 0; i < MaxGround; i++)
            {
                if (_ground[i].Until <= 0f)
                    continue;
                if (_ground[i].Until <= now)
                {
                    _ground[i].Until = 0f;
                    if (_ground[i].Body != null)
                        _ground[i].Body.gameObject.SetActive(false);
                    continue;
                }
                Blast(_ground[i].Position, _groundRadius, _groundDamage, HitFlags.None);
            }
        }

        private void LayGround(Vector3 position)
        {
            ref var ground = ref _ground[_groundCursor];
            _groundCursor = (_groundCursor + 1) % MaxGround;
            ground.Position = position;
            ground.Until = Time.time + _groundDuration;
            if (ground.Body == null)
                return;
            ground.Body.position = position;
            ground.Body.localScale = new Vector3(_groundRadius * 2f, 1f, _groundRadius * 2f);
            ground.Body.gameObject.SetActive(true);
        }

        private void Blast(Vector3 center, float radius, float damage, HitFlags flags)
        {
            var count = Physics.OverlapSphereNonAlloc(center + Vector3.up * 0.5f, radius, Hits, _hitLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Hits[i], out var hurtbox) || hurtbox.Health == _health)
                    continue;
                var target = hurtbox.Health;
                var info = DamageInfo.Create(damage, Team.Creature, DamageType.Fire, damage * 0.5f);
                info.Flags = flags;
                info.Status = StatusKind.Burn;
                info.Attacker = _health;
                info.Point = target.transform.position;
                var away = target.transform.position - center;
                away.y = 0f;
                info.Direction = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.forward;
                target.TakeDamage(info);
            }
        }
    }
}
