using FMODUnity;
using PrimeTween;
using TentacleVania.Core;
using TentacleVania.Core.Interaction;
using TentacleVania.Core.Progression;
using UnityEngine;

namespace TentacleVania.World.Shortcuts
{
    public sealed class Shortcut : MonoBehaviour, IInteractable
    {
        public const string OpenPromptKey = "prompt.shortcut.open";

        [Tooltip("Unique save id; when empty, the scene name and object name are used.")]
        [SerializeField] private string _id;
        [Tooltip("Opens when this nest is grown (e.g. the pre-boss nest), as well as from the far side.")]
        [SerializeField] private string _openOnNestGrown;
        [Tooltip("Placed on the far side; Grab within range of it opens the shortcut. Its forward points away from the blocker.")]
        [SerializeField] private Transform _lever;
        [SerializeField] private Transform _blocker;
        [SerializeField] private Collider _blockerCollider;
        [SerializeField] private Vector3 _openOffset = new(0f, -2.4f, 0f);
        [SerializeField, Min(0f)] private float _openDuration = 0.8f;
        [SerializeField, Min(0.1f)] private float _radius = 1.6f;
        [SerializeField] private EventReference _openSound;

        private Vector3 _closedPosition;
        private Tween _tween;
        private string _saveId;

        public string SaveId => _saveId ??= string.IsNullOrEmpty(_id) ? gameObject.scene.name + "/" + name : _id;
        public bool IsOpen { get; private set; }
        public Vector3 Position => _lever != null ? _lever.position : transform.position;
        public float Radius => _radius;
        public string PromptKey => OpenPromptKey;
        public bool CanInteract => isActiveAndEnabled && !IsOpen && CreatureOnFarSide();

        private void Awake() => _closedPosition = _blocker != null ? _blocker.localPosition : Vector3.zero;

        private void OnEnable()
        {
            Interactions.Register(this);
            Nests.Grown += OnNestGrown;
            ShortcutProgress.Opened += OnOpened;
        }

        private void OnDisable()
        {
            Interactions.Unregister(this);
            Nests.Grown -= OnNestGrown;
            ShortcutProgress.Opened -= OnOpened;
        }

        private void Start() => Show(ShortcutProgress.IsOpen(SaveId) || (!string.IsNullOrEmpty(_openOnNestGrown) && Nests.IsGrown(_openOnNestGrown)), false);

        public void Interact()
        {
            if (CanInteract)
                Open();
        }

        public void Open()
        {
            if (!ShortcutProgress.Open(SaveId))
                Show(true, true);
        }

        public void ResetClosed() => Show(false, false);

        private bool CreatureOnFarSide()
        {
            var target = CameraFocus.Target;
            var lever = _lever != null ? _lever : transform;
            return target != null && ShortcutProgress.IsFarSide(transform.position, lever.forward, target.position);
        }

        private void OnNestGrown(string nest)
        {
            if (nest == _openOnNestGrown)
                Open();
        }

        private void OnOpened(string id)
        {
            if (id == SaveId)
                Show(true, true);
        }

        private void Show(bool open, bool animate)
        {
            if (open == IsOpen && animate)
                return;
            IsOpen = open;
            if (_blockerCollider != null)
                _blockerCollider.enabled = !open;
            if (_blocker == null)
                return;
            _tween.Stop();
            var target = open ? _closedPosition + _openOffset : _closedPosition;
            if (animate)
            {
                _tween = Tween.LocalPosition(_blocker, target, _openDuration, Ease.InOutSine);
                if (!_openSound.IsNull)
                    RuntimeManager.PlayOneShot(_openSound, transform.position);
            }
            else
            {
                _blocker.localPosition = target;
            }
        }
    }
}
