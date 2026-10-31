using FMODUnity;
using TentacleVania.Core.Interaction;
using TentacleVania.Core.Progression;
using UnityEngine;

namespace TentacleVania.World.Lore
{
    public sealed class LoreTerminal : MonoBehaviour, IInteractable
    {
        public const string PromptReadKey = "prompt.lore.read";

        [SerializeField] private LoreLog _log;
        [SerializeField, Min(0.1f)] private float _radius = 1.6f;
        [Tooltip("Shown until the log is collected (placeholder screen glow).")]
        [SerializeField] private GameObject _unreadIndicator;
        [SerializeField] private EventReference _openSound;

        public LoreLog Log => _log;
        public Vector3 Position => transform.position;
        public float Radius => _radius;
        public string PromptKey => PromptReadKey;
        public bool CanInteract => isActiveAndEnabled && _log != null;

        private void OnEnable()
        {
            Interactions.Register(this);
            RefreshIndicator();
        }

        private void OnDisable() => Interactions.Unregister(this);

        public void SetLog(LoreLog log)
        {
            _log = log;
            RefreshIndicator();
        }

        public void Interact()
        {
            if (_log == null)
                return;
            LoreLogs.Collect(_log.Id);
            if (!_openSound.IsNull)
                RuntimeManager.PlayOneShot(_openSound, transform.position);
            RefreshIndicator();
            LoreLogs.Read(_log.Id);
        }

        private void RefreshIndicator()
        {
            if (_unreadIndicator != null)
                _unreadIndicator.SetActive(_log != null && !LoreLogs.Has(_log.Id));
        }
    }
}
