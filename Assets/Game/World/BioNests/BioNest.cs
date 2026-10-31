using FMODUnity;
using TentacleVania.Core.Interaction;
using TentacleVania.Core.Progression;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.World.BioNests
{
    public sealed class BioNest : MonoBehaviour, IHoldInteractable
    {
        public const string GrowPromptKey = "prompt.nest.grow";
        public const string RestPromptKey = "prompt.nest.rest";
        public const float RestHoldDuration = 0.6f;

        [Tooltip("Unique save id; when empty, the scene name and object name are used.")]
        [SerializeField] private string _id;
        [SerializeField] private Transform _respawnPoint;
        [SerializeField, Min(0.5f)] private float _radius = 2.5f;
        [SerializeField] private GameObject _siteVisual;
        [SerializeField] private GameObject _grownVisual;
        [SerializeField] private VisualEffect _growBurst;
        [SerializeField] private VisualEffect _restBurst;
        [SerializeField] private EventReference _growSound;
        [SerializeField] private EventReference _restSound;

        private string _saveId;

        public string SaveId => _saveId ??= string.IsNullOrEmpty(_id) ? gameObject.scene.name + "/" + name : _id;
        public bool IsGrown => Nests.IsGrown(SaveId);
        public Vector3 Position => transform.position;
        public float Radius => _radius;
        public string PromptKey => IsGrown ? RestPromptKey : GrowPromptKey;
        public float HoldDuration => RestHoldDuration;
        public bool CanInteract => isActiveAndEnabled;
        public Transform RespawnPoint => _respawnPoint != null ? _respawnPoint : transform;

        private void OnEnable() => Interactions.Register(this);

        private void OnDisable() => Interactions.Unregister(this);

        private void Start() => RefreshVisuals();

        public void Interact()
        {
            if (!IsGrown)
                Grow();
            SetCheckpoint();
        }

        public void InteractHold()
        {
            if (!IsGrown)
                Grow();
            Rest();
        }

        public void SetCheckpoint()
        {
            if (!IsGrown)
                return;
            var respawn = RespawnPoint;
            if (!Nests.SetRespawn(SaveId, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, respawn.position, respawn.eulerAngles.y))
                return;
            Core.Saves.Autosave.Request(Core.Saves.AutosaveReason.NestRest);
            Play(_restBurst, default);
        }

        public void Grow()
        {
            if (!Nests.Grow(SaveId))
                return;
            RefreshVisuals();
            Play(_growBurst, _growSound);
        }

        public void Rest()
        {
            if (!IsGrown)
                return;
            var respawn = RespawnPoint;
            Nests.Rest(SaveId, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, respawn.position, respawn.eulerAngles.y);
            Play(_restBurst, _restSound);
        }

        private void RefreshVisuals()
        {
            var grown = IsGrown;
            if (_siteVisual != null)
                _siteVisual.SetActive(!grown);
            if (_grownVisual != null)
                _grownVisual.SetActive(grown);
        }

        private void Play(VisualEffect effect, EventReference sound)
        {
            if (effect != null)
            {
                effect.Reinit();
                effect.Play();
            }
            if (!sound.IsNull)
                RuntimeManager.PlayOneShot(sound, transform.position);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.6f, 0.2f, 0.9f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
