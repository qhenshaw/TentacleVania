using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Core.Progression;
using UnityEngine;

namespace TentacleVania.World.Pickups
{
    public sealed class Pickup : MonoBehaviour
    {
        [Tooltip("Save id; defaults to <scene>/<object name>.")]
        [SerializeField] private string _id;
        [SerializeField] private PickupKind _kind;
        [Tooltip("Weapon, mutation or trinket id (asset name).")]
        [SerializeField] private string _itemId;
        private const int DefaultCacheAmount = 250;

        [SerializeField, Min(0)] private int _amount = 1;
        [SerializeField, Min(0.1f)] private float _radius = 1.2f;
        [SerializeField] private Transform _visual;
        [SerializeField] private float _spinSpeed = 90f;
        [SerializeField] private float _bobHeight = 0.15f;
        [SerializeField] private EventReference _collectSound;

        private Vector3 _visualBase;

        public string SaveId => string.IsNullOrEmpty(_id) ? gameObject.scene.name + "/" + name : _id;
        public PickupKind Kind => _kind;

        private void OnEnable()
        {
            if (Collectibles.IsCollected(SaveId))
            {
                gameObject.SetActive(false);
                return;
            }
            if (_visual != null)
                _visualBase = _visual.localPosition;
        }

        private void Update()
        {
            if (_visual != null)
            {
                _visual.Rotate(0f, _spinSpeed * Time.deltaTime, 0f, Space.World);
                _visual.localPosition = _visualBase + Vector3.up * (Mathf.Sin(Time.time * 2.5f) * _bobHeight);
            }
            var creature = CombatTargets.Creature;
            if (creature == null || !creature.IsAlive)
                return;
            var offset = creature.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= _radius * _radius)
                Collect();
        }

        public void Collect()
        {
            var amount = _kind == PickupKind.Biomass && _amount == DefaultCacheAmount ? Core.Progression.DifficultyCurve.Current.Cache : _amount;
            if (!Collectibles.Collect(SaveId, _kind, _itemId, amount))
                return;
            if (!_collectSound.IsNull)
                RuntimeManager.PlayOneShot(_collectSound, transform.position);
            gameObject.SetActive(false);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
