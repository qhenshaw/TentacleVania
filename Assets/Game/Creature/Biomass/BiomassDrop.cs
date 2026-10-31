using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Core.Progression;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.Creature.Biomass
{
    public sealed class BiomassDrop : MonoBehaviour
    {
        [SerializeField] private GameObject _visual;
        [SerializeField] private VisualEffect _reclaimBurst;
        [SerializeField, Min(0.1f)] private float _reclaimRadius = 1.2f;
        [SerializeField] private EventReference _dropSound;
        [SerializeField] private EventReference _reclaimSound;

        public bool Showing { get; private set; }

        public void Show(Vector3 position, bool announce)
        {
            transform.position = position;
            Showing = true;
            _visual.SetActive(true);
            if (announce && !_dropSound.IsNull)
                RuntimeManager.PlayOneShot(_dropSound, position);
        }

        public void Hide()
        {
            Showing = false;
            _visual.SetActive(false);
        }

        private void Update()
        {
            if (!Showing)
                return;
            var creature = CombatTargets.Creature;
            if (creature == null || !creature.IsAlive)
                return;
            var offset = creature.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > _reclaimRadius * _reclaimRadius)
                return;
            Core.Progression.Biomass.Reclaim();
            Hide();
            if (_reclaimBurst != null)
            {
                _reclaimBurst.Reinit();
                _reclaimBurst.Play();
            }
            if (!_reclaimSound.IsNull)
                RuntimeManager.PlayOneShot(_reclaimSound, transform.position);
        }
    }
}
