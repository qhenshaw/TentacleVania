using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Creature.Biomass
{
    public sealed class BiomassCache : MonoBehaviour
    {
        [Tooltip("Unique save id; when empty, the scene name and object name are used.")]
        [SerializeField] private string _id;
        [SerializeField, Min(1)] private int _amount = 50;
        [SerializeField, Min(0.1f)] private float _pickupRadius = 1.2f;
        [SerializeField] private GameObject _visual;
        [SerializeField] private EventReference _pickupSound;

        private string _saveId;
        private bool _collected;

        public string SaveId => _saveId;

        private void Start()
        {
            _saveId = string.IsNullOrEmpty(_id) ? gameObject.scene.name + "/" + name : _id;
            _collected = SaveSystem.Data.World.Has(WorldFlag.CollectedPickup, _saveId);
            _visual.SetActive(!_collected);
        }

        private void Update()
        {
            if (_collected)
                return;
            var creature = CombatTargets.Creature;
            if (creature == null || !creature.IsAlive)
                return;
            var offset = creature.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > _pickupRadius * _pickupRadius)
                return;
            _collected = true;
            SaveSystem.Data.World.Set(WorldFlag.CollectedPickup, _saveId);
            Core.Progression.Biomass.Add(_amount);
            Autosave.Request(AutosaveReason.Pickup);
            _visual.SetActive(false);
            if (!_pickupSound.IsNull)
                RuntimeManager.PlayOneShot(_pickupSound, transform.position);
        }
    }
}
