using TentacleVania.Combat;
using TentacleVania.Core.Saves;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TentacleVania.Creature.Biomass
{
    [RequireComponent(typeof(Health))]
    public sealed class CreatureBiomass : MonoBehaviour
    {
        private const float GroundProbe = 3f;

        [SerializeField] private BiomassDrop _dropPrefab;
        [SerializeField] private LayerMask _groundLayers = 1;

        private readonly RaycastHit[] _hits = new RaycastHit[4];

        private Health _health;
        private BiomassDrop _drop;

        public BiomassDrop Drop => _drop;

        private void Awake()
        {
            _health = GetComponent<Health>();
            if (_dropPrefab == null)
                return;
            _drop = Instantiate(_dropPrefab);
            SceneManager.MoveGameObjectToScene(_drop.gameObject, gameObject.scene);
            _drop.Hide();
        }

        private void OnEnable()
        {
            CombatEvents.Consumed += OnConsumed;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            CombatEvents.Consumed -= OnConsumed;
            _health.Died -= OnDied;
        }

        private void OnDestroy()
        {
            if (_drop != null)
                Destroy(_drop.gameObject);
        }

        private void Start()
        {
            if (_drop != null && Core.Progression.Biomass.HasDrop && Core.Progression.Biomass.DropScene == gameObject.scene.name)
                _drop.Show(Core.Progression.Biomass.DropPosition, false);
        }

        private void OnConsumed(Consumable consumable) => Core.Progression.Biomass.Add(consumable.Biomass);

        private void OnDied(Health health)
        {
            var position = Ground(transform.position);
            Core.Progression.Biomass.Die(gameObject.scene.name, position);
            Autosave.Request(AutosaveReason.Death);
            if (_drop == null)
                return;
            if (Core.Progression.Biomass.HasDrop)
                _drop.Show(position, true);
            else
                _drop.Hide();
        }

        private Vector3 Ground(Vector3 point)
        {
            var count = Physics.RaycastNonAlloc(point + Vector3.up, Vector3.down, _hits, GroundProbe, _groundLayers, QueryTriggerInteraction.Ignore);
            var best = -1;
            for (var i = 0; i < count; i++)
                if (best < 0 || _hits[i].distance < _hits[best].distance)
                    best = i;
            return best >= 0 ? _hits[best].point : point;
        }
    }
}
