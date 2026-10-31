using TentacleVania.Combat;
using UnityEngine;

namespace TentacleVania.Creature.Biomass
{
    [RequireComponent(typeof(Health))]
    public sealed class BiomassOrbs : MonoBehaviour
    {
        [SerializeField] private GameObject _orbPrefab;
        [SerializeField, Min(1)] private int _poolSize = 12;
        [Tooltip("Seconds the orb pops up out of the body before it homes in.")]
        [SerializeField, Min(0f)] private float _popTime = 0.25f;
        [SerializeField, Min(0.05f)] private float _flyTime = 0.45f;
        [SerializeField, Min(0f)] private float _popSpeed = 4f;

        private Health _health;
        private Transform _root;
        private Transform[] _orbs;
        private Vector3[] _velocity;
        private Vector3[] _popEnd;
        private float[] _started;
        private int[] _amount;
        private bool[] _live;
        private Vector3 _scale;
        private int _next;

        public static bool Enabled { get; set; } = true;
        public int Flying { get; private set; }
        public int Credited { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Enabled = true;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _orbs = new Transform[_poolSize];
            _velocity = new Vector3[_poolSize];
            _popEnd = new Vector3[_poolSize];
            _started = new float[_poolSize];
            _amount = new int[_poolSize];
            _live = new bool[_poolSize];
            if (_orbPrefab == null)
                return;
            _root = new GameObject("BiomassOrbs").transform;
            _scale = _orbPrefab.transform.localScale;
            for (var i = 0; i < _poolSize; i++)
            {
                _orbs[i] = Instantiate(_orbPrefab, _root).transform;
                _orbs[i].name = $"BiomassOrb {i + 1}";
                _orbs[i].gameObject.SetActive(false);
            }
        }

        private void OnEnable() => CombatEvents.Died += OnDied;

        private void OnDisable()
        {
            CombatEvents.Died -= OnDied;
            for (var i = 0; i < _poolSize; i++)
                if (_live[i])
                    Land(i);
        }

        private void OnDestroy()
        {
            if (_root != null)
                Destroy(_root.gameObject);
        }

        private void OnDied(Health victim)
        {
            if (victim == _health || !victim.TryGetComponent(out Consumable consumable) || consumable.IsBeingConsumed || consumable.Biomass <= 0)
                return;
            var centre = victim.TryGetComponent(out Collider body) ? body.bounds.center : victim.transform.position + Vector3.up;
            Launch(centre, consumable.Biomass);
        }

        public void Launch(Vector3 from, int amount)
        {
            if (!Enabled || _orbPrefab == null || _live[_next])
            {
                Core.Progression.Biomass.Add(amount);
                Credited++;
                return;
            }
            var i = _next;
            _next = (_next + 1) % _poolSize;
            _live[i] = true;
            _amount[i] = amount;
            _started[i] = Time.time;
            var sideways = Random.insideUnitCircle * 0.6f;
            _velocity[i] = new Vector3(sideways.x, 1f, sideways.y).normalized * _popSpeed;
            _orbs[i].position = from;
            _orbs[i].localScale = _scale;
            _orbs[i].gameObject.SetActive(true);
            Flying++;
        }

        private void Update()
        {
            if (Flying == 0)
                return;
            var now = Time.time;
            var target = transform.position + Vector3.up * 0.8f;
            for (var i = 0; i < _poolSize; i++)
            {
                if (!_live[i])
                    continue;
                var elapsed = now - _started[i];
                var orb = _orbs[i];
                if (elapsed < _popTime)
                {
                    orb.position += _velocity[i] * Time.deltaTime;
                    _popEnd[i] = orb.position;
                    continue;
                }
                var t = Mathf.Clamp01((elapsed - _popTime) / _flyTime);
                var eased = t * t;
                orb.position = Vector3.Lerp(_popEnd[i], target, eased) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.6f;
                orb.localScale = _scale * Mathf.Lerp(1f, 0.5f, eased);
                if (t >= 1f)
                    Land(i);
            }
        }

        private void Land(int i)
        {
            _live[i] = false;
            _orbs[i].gameObject.SetActive(false);
            Flying--;
            Credited++;
            Core.Progression.Biomass.Add(_amount[i]);
        }
    }
}
