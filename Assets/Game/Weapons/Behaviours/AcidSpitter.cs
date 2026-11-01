using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using TentacleVania.Core.Saves;
using TentacleVania.Weapons.Data;
using UnityEngine;

namespace TentacleVania.Weapons.Behaviours
{
    public sealed class AcidSpitter : MonoBehaviour
    {
        private const int MaxGlobs = 6;
        private const int MaxPools = 6;
        private static readonly Collider[] Hits = new Collider[24];

        private struct Glob
        {
            public bool Live;
            public bool Big;
            public Vector3 From;
            public Vector3 To;
            public float Started;
            public float Damage;
            public float ArmorScale;
        }

        private struct OwnPool
        {
            public Vector3 Position;
            public float Until;
        }

        [SerializeField] private WeaponController _weapons;
        [SerializeField] private string _weaponId = "AcidSpitter";
        [SerializeField] private LayerMask _hitLayers;
        [SerializeField] private Transform _globPrefab;
        [SerializeField] private Transform _cloudPrefab;
        [SerializeField, Min(0.1f)] private float _flightTime = 0.45f;
        [SerializeField, Min(0f)] private float _arcHeight = 2f;
        [SerializeField, Min(1f)] private float _range = 8f;
        [SerializeField, Range(0f, 180f)] private float _aimCone = 40f;
        [SerializeField, Min(0.1f)] private float _globRadius = 1.2f;
        [SerializeField, Min(0.1f)] private float _bigRadius = 2.2f;
        [SerializeField, Min(0.1f)] private float _poolDuration = 5f;
        [SerializeField, Min(0f)] private float _poolDamagePerSecond = 8f;
        [SerializeField, Min(0.1f)] private float _cloudDuration = 4f;
        [SerializeField, Min(0.1f)] private float _cloudRadius = 2.5f;
        [SerializeField, Min(0.05f)] private float _cloudTick = 0.4f;
        [SerializeField, Min(0f)] private float _cloudDamagePerTick = 4f;
        [SerializeField, Min(0.05f)] private float _trailSpacing = 0.12f;
        [SerializeField, Min(0.1f)] private float _trailRadius = 1f;
        [SerializeField, Min(1f)] private float _meltingArmorScale = 3f;
        [SerializeField] private EventReference _spitSound;
        [SerializeField] private EventReference _splashSound;

        private readonly Glob[] _globs = new Glob[MaxGlobs];
        private readonly Transform[] _globBodies = new Transform[MaxGlobs];
        private readonly OwnPool[] _pools = new OwnPool[MaxPools];
        private Health _health;
        private Transform _cloud;
        private float _launchAt = -1f;
        private bool _launchBig;
        private float _cloudUntil;
        private float _nextCloudTick;
        private float _trailUntil;
        private float _nextTrail;

        private void Awake()
        {
            _health = GetComponent<Health>();
            for (var i = 0; i < MaxGlobs; i++)
                if (_globPrefab != null)
                {
                    _globBodies[i] = Instantiate(_globPrefab);
                    _globBodies[i].gameObject.SetActive(false);
                }
            if (_cloudPrefab != null)
            {
                _cloud = Instantiate(_cloudPrefab);
                _cloud.gameObject.SetActive(false);
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
            {
                _weapons.AttackStarted -= OnAttackStarted;
                _weapons.SpeedBonus = 1f;
            }
        }

        private void OnDestroy()
        {
            foreach (var body in _globBodies)
                if (body != null)
                    Destroy(body.gameObject);
            if (_cloud != null)
                Destroy(_cloud.gameObject);
        }

        private bool IsSpitter(WeaponController controller) =>
            controller.ActiveSlot >= 0 && ContentCatalog.IdOf(controller.Weapon(controller.ActiveSlot)) == _weaponId;

        private WeaponModifier Modifier => _weapons.ActiveSlot >= 0 ? _weapons.Modifier(_weapons.ActiveSlot) : null;

        private void OnAttackStarted(WeaponController controller)
        {
            if (!IsSpitter(controller))
                return;
            var now = Time.time;
            switch (controller.ActiveKind)
            {
                case AttackKind.Heavy:
                    _cloudUntil = now + _cloudDuration;
                    _nextCloudTick = now;
                    if (_cloud != null)
                        _cloud.gameObject.SetActive(true);
                    break;
                case AttackKind.DashAttack:
                    _trailUntil = now + 0.35f;
                    _nextTrail = now;
                    break;
                default:
                    _launchAt = now + controller.MoveWindUp;
                    _launchBig = controller.IsFinisher;
                    break;
            }
        }

        private void Update()
        {
            if (_weapons == null)
                return;
            var now = Time.time;
            if (_launchAt >= 0f && now >= _launchAt)
            {
                _launchAt = -1f;
                Launch(_launchBig);
            }
            TickGlobs(now);
            TickCloud(now);
            TickTrail(now);
            TickBath(now);
        }

        private void Launch(bool big)
        {
            var weapon = _weapons.ActiveSlot >= 0 ? _weapons.Weapon(_weapons.ActiveSlot) : null;
            if (weapon == null)
                return;
            var forward = _weapons.ActiveForward;
            var from = transform.position + Vector3.up * 1.2f;
            var to = Target(forward);
            for (var i = 0; i < MaxGlobs; i++)
            {
                if (_globs[i].Live)
                    continue;
                var modifier = Modifier;
                _globs[i] = new Glob
                {
                    Live = true,
                    Big = big,
                    From = from,
                    To = to,
                    Started = Time.time,
                    Damage = weapon.Damage * (big ? 1.6f : 1f),
                    ArmorScale = modifier != null && modifier.Kind == ModifierKind.MeltingArmor ? _meltingArmorScale : 1f,
                };
                if (_globBodies[i] != null)
                {
                    _globBodies[i].localScale = Vector3.one * (big ? 0.7f : 0.4f);
                    _globBodies[i].position = from;
                    _globBodies[i].gameObject.SetActive(true);
                }
                if (!_spitSound.IsNull)
                    RuntimeManager.PlayOneShot(_spitSound, from);
                return;
            }
        }

        private Vector3 Target(Vector3 forward)
        {
            var origin = transform.position;
            var count = Physics.OverlapSphereNonAlloc(origin + Vector3.up, _range, Hits, _hitLayers, QueryTriggerInteraction.Collide);
            var best = origin + forward * (_range * 0.75f);
            var bestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Hits[i], out var hurtbox) || hurtbox.Health == _health || !hurtbox.Health.IsAlive)
                    continue;
                var to = hurtbox.Health.transform.position - origin;
                to.y = 0f;
                var distance = to.sqrMagnitude;
                if (distance >= bestDistance || Vector3.Angle(forward, to) > _aimCone * 0.5f)
                    continue;
                bestDistance = distance;
                best = hurtbox.Health.transform.position;
            }
            return best;
        }

        public static Vector3 Arc(Vector3 from, Vector3 to, float height, float t)
        {
            var point = Vector3.Lerp(from, to, t);
            point.y += height * 4f * t * (1f - t);
            return point;
        }

        private void TickGlobs(float now)
        {
            for (var i = 0; i < MaxGlobs; i++)
            {
                ref var glob = ref _globs[i];
                if (!glob.Live)
                    continue;
                var t = Mathf.Clamp01((now - glob.Started) / _flightTime);
                if (_globBodies[i] != null)
                    _globBodies[i].position = Arc(glob.From, glob.To, _arcHeight, t);
                if (t < 1f)
                    continue;
                glob.Live = false;
                if (_globBodies[i] != null)
                    _globBodies[i].gameObject.SetActive(false);
                Splash(glob);
            }
        }

        private void Splash(in Glob glob)
        {
            var radius = glob.Big ? _bigRadius : _globRadius;
            if (!_splashSound.IsNull)
                RuntimeManager.PlayOneShot(_splashSound, glob.To);
            Strike(glob.To, radius, glob.Damage, glob.ArmorScale, HitFlags.None);
            if (!glob.Big)
                return;
            AcidPools.Spawn(glob.To, Team.Creature, radius, _poolDuration, _poolDamagePerSecond);
            Remember(glob.To, _poolDuration);
        }

        private void Strike(Vector3 center, float radius, float damage, float armorScale, HitFlags flags)
        {
            var count = Physics.OverlapSphereNonAlloc(center + Vector3.up * 0.5f, radius, Hits, _hitLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Hits[i], out var hurtbox) || hurtbox.Health == _health)
                    continue;
                var target = hurtbox.Health;
                var info = DamageInfo.Create(damage, Team.Creature, DamageType.Acid, damage * 0.5f);
                info.Flags = flags;
                info.ArmorDamageScale = armorScale;
                info.Status = StatusKind.Corrode;
                info.Attacker = _health;
                info.Point = target.transform.position;
                var away = target.transform.position - center;
                away.y = 0f;
                info.Direction = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.forward;
                target.TakeDamage(info);
            }
        }

        private void TickCloud(float now)
        {
            if (_cloud == null && _cloudUntil <= 0f)
                return;
            var active = now < _cloudUntil;
            if (_cloud != null)
            {
                if (_cloud.gameObject.activeSelf != active)
                    _cloud.gameObject.SetActive(active);
                if (active)
                {
                    _cloud.position = transform.position + Vector3.up;
                    _cloud.localScale = Vector3.one * (_cloudRadius * 2f);
                }
            }
            if (!active || now < _nextCloudTick)
                return;
            _nextCloudTick = now + _cloudTick;
            var modifier = Modifier;
            Strike(transform.position, _cloudRadius, _cloudDamagePerTick, modifier != null && modifier.Kind == ModifierKind.MeltingArmor ? _meltingArmorScale : 1f, HitFlags.DamageOverTime);
        }

        private void TickTrail(float now)
        {
            if (now >= _trailUntil || now < _nextTrail)
                return;
            _nextTrail = now + _trailSpacing;
            AcidPools.Spawn(transform.position, Team.Creature, _trailRadius, _poolDuration * 0.8f, _poolDamagePerSecond * 0.75f);
            Remember(transform.position, _poolDuration * 0.8f);
        }

        private void Remember(Vector3 position, float duration)
        {
            var oldest = 0;
            for (var i = 0; i < MaxPools; i++)
            {
                if (_pools[i].Until < _pools[oldest].Until)
                    oldest = i;
            }
            _pools[oldest] = new OwnPool { Position = position, Until = Time.time + duration };
        }

        private void TickBath(float now)
        {
            var modifier = _weapons.ActiveSlot >= 0 ? Modifier : null;
            var bonus = 1f;
            if (modifier != null && modifier.Kind == ModifierKind.AcidBath && IsSpitter(_weapons))
                for (var i = 0; i < MaxPools; i++)
                {
                    if (_pools[i].Until <= now)
                        continue;
                    var offset = transform.position - _pools[i].Position;
                    offset.y = 0f;
                    if (offset.sqrMagnitude <= _globRadius * _globRadius)
                    {
                        bonus = modifier.Value;
                        break;
                    }
                }
            _weapons.SpeedBonus = bonus;
        }
    }
}
