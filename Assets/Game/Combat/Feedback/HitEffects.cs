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

        private static readonly int IntensityId = Shader.PropertyToID("Intensity");

        [SerializeField] private HitEffectSettings _settings;

        private readonly RaycastHit[] _hits = new RaycastHit[8];

        private Transform _root;
        private VfxPool _creatureImpact;
        private VfxPool _creatureSpray;
        private VfxPool _enemyImpact;
        private VfxPool _enemySpray;
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Enabled = true;
            Instance = null;
        }

        private sealed class VfxPool
        {
            private readonly VisualEffect[] _effects;
            private readonly bool _hasIntensity;
            private int _next;

            public VfxPool(VisualEffect prefab, int size, Transform parent)
            {
                if (prefab == null)
                {
                    _effects = System.Array.Empty<VisualEffect>();
                    return;
                }
                _effects = new VisualEffect[size];
                for (var i = 0; i < size; i++)
                {
                    _effects[i] = Instantiate(prefab, parent);
                    _effects[i].name = $"{prefab.name} {i + 1}";
                }
                _hasIntensity = _effects[0].HasFloat(IntensityId);
            }

            public void Play(Vector3 point, Vector3 direction, float intensity)
            {
                if (_effects.Length == 0)
                    return;
                var effect = _effects[_next];
                _next = (_next + 1) % _effects.Length;
                effect.transform.SetPositionAndRotation(point, Quaternion.LookRotation(direction));
                if (_hasIntensity)
                    effect.SetFloat(IntensityId, intensity);
                effect.Reinit();
                effect.Play();
            }
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
                _decals[i].enabled = false;
            }
        }

        private void OnEnable()
        {
            Instance = this;
            CombatEvents.HitLanded += OnHit;
            CombatEvents.Consumed += OnConsumed;
        }

        private void OnDisable()
        {
            CombatEvents.HitLanded -= OnHit;
            CombatEvents.Consumed -= OnConsumed;
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
            if (_fading < 0)
                return;
            var decal = _decals[_fading];
            var fade = _settings.RecycleFade;
            var t = fade > 0f ? (Time.time - _fadeStarted) / fade : 1f;
            if (t >= 1f)
            {
                decal.enabled = false;
                _fading = -1;
                return;
            }
            decal.fadeFactor = _decalBaseFade[_fading] * (1f - t);
        }

        public void ClearDecals()
        {
            foreach (var decal in _decals)
                decal.enabled = false;
            _placedDecals = 0;
            _nextDecal = 0;
            _fading = -1;
        }

        private void OnHit(Health target, in DamageInfo info, in DamageResult result)
        {
            if (!Enabled || _settings == null || result.Outcome != DamageOutcome.Applied)
                return;
            var isCreature = CombatTargets.Creature != null && target == CombatTargets.Creature;
            var kind = HitFeedback.ClassifyForPlayer(info, target);
            var effects = HitEffectRules.Select(isCreature, kind, result.HealthDamage, target.Max, result.Killed, GameSettings.Gore, _settings.CreatureBigHit);
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
                (isCreature ? _creatureImpact : _enemyImpact).Play(point, direction, intensity);
            if ((effects & HitEffect.Spray) != 0)
                (isCreature ? _creatureSpray : _enemySpray).Play(point, direction, intensity);
            if ((effects & HitEffect.Decal) != 0)
                PlaceDecal(isCreature ? _settings.Creature : _settings.Enemy, point, direction, intensity);
        }

        private void OnConsumed(Consumable consumable)
        {
            if (!Enabled || _settings == null || !GameSettings.Gore)
                return;
            PlaceDecal(_settings.Enemy, consumable.transform.position, Vector3.zero, _settings.KillIntensity);
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
            decal.enabled = true;
            _placedDecals++;

            if (_placedDecals >= _settings.DecalCap)
            {
                _fading = _nextDecal;
                _fadeStarted = Time.time;
            }
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
