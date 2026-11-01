using TentacleVania.Combat.Damage;
using TentacleVania.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;

namespace TentacleVania.Combat.Feedback
{
    public sealed class HitEffects : MonoBehaviour
    {
        private const float GroundProbeHeight = 1f;
        private const float GroundProbeDepth = 4f;
        private static readonly Vector3 Parked = new(0f, -1000f, 0f);

        [SerializeField] private HitEffectSettings _settings;

        private readonly RaycastHit[] _hits = new RaycastHit[8];

        private Transform _root;
        private VfxPool _creatureImpact;
        private VfxPool _creatureSpray;
        private VfxPool _enemyImpact;
        private VfxPool _enemySpray;
        private VfxPool _machineImpact;
        private VfxPool _machineSpray;
        private VfxPool _smoke;
        private GibPool _flesh;
        private GibPool _bone;
        private GibPool _metal;
        private DecalProjector[] _decals;
        private float[] _decalBaseFade;
        private int _nextDecal;
        private int _placedDecals;
        private int _fading = -1;
        private float _fadeStarted;

        public static bool Enabled { get; set; } = true;
        public static HitEffects Instance { get; private set; }

        public int ActiveDecals => Mathf.Min(_placedDecals, _settings != null ? _settings.DecalCap : 0);
        public int DecalCapacity => _settings != null ? _settings.DecalCap : 0;
        public int ActiveGibs => _flesh == null ? 0 : _flesh.Active + _bone.Active + _metal.Active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Enabled = true;
            Instance = null;
        }

        private void Awake()
        {
            if (_settings == null)
                return;
            _root = new GameObject("HitEffects").transform;
            _creatureImpact = new(_settings.Creature.Impact, _settings.Creature.PoolSize, _root);
            _creatureSpray = new(_settings.Creature.BloodSpray, _settings.Creature.PoolSize, _root);
            _enemyImpact = new(_settings.Enemy.Impact, _settings.Enemy.PoolSize, _root);
            _enemySpray = new(_settings.Enemy.BloodSpray, _settings.Enemy.PoolSize, _root);
            _machineImpact = new(_settings.Machine.Impact, _settings.Machine.PoolSize, _root);
            _machineSpray = new(_settings.Machine.BloodSpray, _settings.Machine.PoolSize, _root);
            _smoke = new(_settings.IchorSmoke, _settings.Enemy.PoolSize, _root);
            _flesh = new(_settings.FleshGib, _settings.GibPoolSize, _root);
            _bone = new(_settings.BoneGib, _settings.GibPoolSize / 2, _root);
            _metal = new(_settings.MetalGib, _settings.GibPoolSize, _root);

            var count = _settings.DecalCap + 1;
            _decals = new DecalProjector[count];
            _decalBaseFade = new float[count];
            var decalPrefab = _settings.Enemy.BloodDecal != null ? _settings.Enemy.BloodDecal : _settings.Creature.BloodDecal;
            if (decalPrefab == null)
            {
                _decals = System.Array.Empty<DecalProjector>();
                return;
            }
            for (var i = 0; i < count; i++)
            {
                _decals[i] = Instantiate(decalPrefab, _root);
                _decals[i].name = $"BloodDecal {i + 1}";
                _decals[i].enabled = true;
                Hide(_decals[i]);
            }
        }

        private void OnEnable()
        {
            Instance = this;
            CombatEvents.HitLanded += OnHit;
            CombatEvents.ConsumeBitten += OnBitten;
            CombatEvents.Died += OnDied;
        }

        private void OnDisable()
        {
            CombatEvents.HitLanded -= OnHit;
            CombatEvents.ConsumeBitten -= OnBitten;
            CombatEvents.Died -= OnDied;
            if (Instance == this)
                Instance = null;
        }

        private void OnDestroy()
        {
            if (_root != null)
                Destroy(_root.gameObject);
        }

        private void Update()
        {
            if (_flesh != null)
            {
                var now = Time.time;
                _flesh.Tick(now);
                _bone.Tick(now);
                _metal.Tick(now);
            }
            if (_fading < 0)
                return;
            var decal = _decals[_fading];
            var fade = _settings.RecycleFade;
            var t = fade > 0f ? (Time.time - _fadeStarted) / fade : 1f;
            if (t >= 1f)
            {
                Hide(decal);
                _fading = -1;
                return;
            }
            decal.fadeFactor = _decalBaseFade[_fading] * (1f - t);
        }

        public void ClearDecals()
        {
            foreach (var decal in _decals)
                Hide(decal);
            _placedDecals = 0;
            _nextDecal = 0;
            _fading = -1;
        }

        public void ClearGibs()
        {
            _flesh?.Clear();
            _bone?.Clear();
            _metal?.Clear();
        }

        private void OnHit(Health target, in DamageInfo info, in DamageResult result)
        {
            if (!Enabled || _settings == null || result.Outcome != DamageOutcome.Applied || (info.Flags & HitFlags.DamageOverTime) != 0)
                return;
            var isCreature = CombatTargets.Creature != null && target == CombatTargets.Creature;
            var kind = HitFeedback.ClassifyForPlayer(info, target);
            var effects = HitEffectRules.Select(isCreature, kind, result.HealthDamage, target.Max, result.Killed, GameSettings.Gore && target.Bleeds, _settings.CreatureBigHit);
            if (result.Killed && target.BurstsOnDeath)
                effects &= HitEffect.Impact;
            var big = HitEffectRules.IsBigHit(isCreature, kind, result.HealthDamage, target.Max, _settings.CreatureBigHit);
            var intensity = result.Killed ? _settings.KillIntensity : big ? _settings.HeavyIntensity : _settings.LightIntensity;

            var centre = target.transform.position;
            var point = info.Point.sqrMagnitude > 0f ? info.Point : centre;
            var direction = info.Direction;
            if (direction.sqrMagnitude < 1e-6f && info.Attacker != null)
                direction = centre - info.Attacker.transform.position;
            direction.y = Mathf.Max(direction.y, 0f);
            if (direction.sqrMagnitude < 1e-6f)
                direction = target.transform.forward;
            direction.Normalize();

            if ((effects & HitEffect.Impact) != 0)
                (isCreature ? _creatureImpact : target.Bleeds ? _enemyImpact : _machineImpact).Play(point, direction, intensity);
            if ((effects & HitEffect.Spray) != 0)
                (isCreature ? _creatureSpray : _enemySpray).Play(point, direction, intensity);
            if ((effects & HitEffect.Decal) != 0)
                PlaceDecal(isCreature ? _settings.Creature : _settings.Enemy, point, direction, intensity);
        }

        private void OnBitten(Consumable consumable)
        {
            if (!Enabled || _settings == null)
                return;
            var effects = DeathGoreRules.OnBite(GameSettings.Gore, consumable.Health.Bleeds, consumable.CoreBleeds);
            Burst(consumable.transform, effects, _settings.BiteGibs);
        }

        private void OnDied(Health health)
        {
            if (!Enabled || _settings == null || !health.BurstsOnDeath)
                return;
            if (health.TryGetComponent<Consumable>(out var consumable) && consumable.IsBeingConsumed)
                return;
            Burst(health.transform, DeathGoreRules.OnKill(GameSettings.Gore, health.Bleeds), _settings.KillGibs);
        }

        private void Burst(Transform body, DeathEffect effects, int gibs)
        {
            if (effects == DeathEffect.None)
                return;
            var point = body.position;
            var centre = body.TryGetComponent<Collider>(out var collider) ? collider.bounds.center : point + Vector3.up;
            var direction = CombatTargets.Creature != null ? point - CombatTargets.Creature.transform.position : Vector3.zero;
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-6f)
                direction = body.forward;
            direction.Normalize();
            var machine = (effects & DeathEffect.Machine) != 0;
            var side = machine ? _settings.Machine : _settings.Enemy;
            if ((effects & DeathEffect.Smoke) != 0)
                _smoke.Play(centre, Vector3.up, _settings.KillIntensity);
            if ((effects & DeathEffect.Spray) != 0)
                (machine ? _machineSpray : _enemySpray).Play(centre, direction, _settings.KillIntensity);
            if ((effects & DeathEffect.Decal) != 0)
                PlaceDecal(side, point, Vector3.zero, _settings.KillIntensity);
            if ((effects & DeathEffect.Gibs) != 0)
                SpawnGibs(centre, direction, gibs, machine);
        }

        private void SpawnGibs(Vector3 centre, Vector3 direction, int count, bool machine)
        {
            var speed = _settings.GibSpeed;
            for (var i = 0; i < count; i++)
            {
                var outward = Random.insideUnitSphere;
                outward.y = 0f;
                outward = (outward + direction * 0.6f).normalized;
                var velocity = outward * Random.Range(speed.x, speed.y) + Vector3.up * Random.Range(speed.x, speed.y);
                var point = centre + Random.insideUnitSphere * 0.3f;
                var pool = machine ? _metal : (i + 1) % _settings.BoneEvery == 0 ? _bone : _flesh;
                pool.Spawn(point, velocity, _settings.GibLifetime * Random.Range(0.8f, 1.2f));
            }
        }

        private void PlaceDecal(HitEffectSettings.Side side, Vector3 point, Vector3 direction, float intensity)
        {
            if (_decals.Length == 0 || side.BloodDecal == null)
                return;
            var flat = new Vector3(direction.x, 0f, direction.z);
            var origin = point + flat.normalized * _settings.DecalOffset;
            if (!TryGround(origin, out var ground))
                return;

            var index = NextDecalSlot();
            var decal = _decals[index];
            var width = Mathf.Lerp(side.DecalSize.x, side.DecalSize.y, intensity);
            var size = side.BloodDecal.size;
            decal.material = side.BloodDecal.material;
            decal.size = new Vector3(width, width, size.z);
            decal.fadeFactor = side.BloodDecal.fadeFactor;
            _decalBaseFade[index] = decal.fadeFactor;
            decal.transform.SetPositionAndRotation(ground, Quaternion.Euler(90f, Random.Range(0f, 360f), 0f));
            _placedDecals++;

            if (_placedDecals >= _settings.DecalCap)
            {
                _fading = _nextDecal;
                _fadeStarted = Time.time;
            }
        }

        private static void Hide(DecalProjector decal)
        {
            decal.fadeFactor = 0f;
            decal.transform.position = Parked;
        }

        private int NextDecalSlot()
        {
            var index = _nextDecal;
            _nextDecal = (_nextDecal + 1) % _decals.Length;
            if (_fading == index)
                _fading = -1;
            return index;
        }

        private bool TryGround(Vector3 point, out Vector3 ground)
        {
            var origin = point + Vector3.up * GroundProbeHeight;
            var count = Physics.RaycastNonAlloc(origin, Vector3.down, _hits, GroundProbeHeight + GroundProbeDepth, _settings.GroundLayers, QueryTriggerInteraction.Ignore);
            var best = -1;
            for (var i = 0; i < count; i++)
            {
                if (best < 0 || _hits[i].distance < _hits[best].distance)
                    best = i;
            }
            ground = best >= 0 ? _hits[best].point : point;
            return best >= 0;
        }
    }
}
