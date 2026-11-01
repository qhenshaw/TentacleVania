using FMODUnity;
using PrimeTween;
using TentacleVania.Core.Interaction;
using TentacleVania.Core.Progression;
using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.World.Overload
{
    public sealed class OverloadDoor : MonoBehaviour, ITraversal
    {
        public const string Ability = "Overload";

        [Tooltip("Save id; defaults to the scene and object name.")]
        [SerializeField] private string _id;
        [Tooltip("The junction box the creature surges; LT within the radius of it.")]
        [SerializeField] private Transform _junction;
        [SerializeField, Min(0.1f)] private float _radius = 1.6f;
        [SerializeField] private Transform _slab;
        [SerializeField] private Collider _blocker;
        [SerializeField] private float _openHeight = -3.2f;
        [SerializeField, Min(0f)] private float _openDuration = 0.8f;
        [SerializeField, Min(0f)] private float _openDelay = 0.35f;
        [SerializeField] private Renderer _lamp;
        [SerializeField] private Material _lockedLight;
        [SerializeField] private Material _openLight;
        [SerializeField] private VisualEffect _surge;
        [SerializeField] private EventReference _surgeSound;
        [SerializeField] private EventReference _openSound;

        private float _closedHeight;

        public string Id => string.IsNullOrEmpty(_id) ? $"{gameObject.scene.name}/{name}" : _id;
        public bool IsOpen { get; private set; }
        public string AbilityId => Ability;
        public Vector3 Position => _junction != null ? _junction.position : transform.position;
        public float Radius => _radius;
        public bool CanTraverse => isActiveAndEnabled && !IsOpen;

        private void Awake()
        {
            if (_slab != null)
                _closedHeight = _slab.localPosition.y;
        }

        private void OnEnable()
        {
            Traversals.Register(this);
            Apply(OverloadedNodes.IsOverloaded(Id), false);
        }

        private void OnDisable() => Traversals.Unregister(this);

        public void Traverse()
        {
            if (!CanTraverse)
                return;
            OverloadedNodes.Overload(Id);
            if (_surge != null)
            {
                _surge.Reinit();
                _surge.Play();
            }
            if (!_surgeSound.IsNull)
                RuntimeManager.PlayOneShot(_surgeSound, Position);
            Apply(true, true);
        }

        public void ResetClosed() => Apply(false, false);

        private void Apply(bool open, bool animate)
        {
            IsOpen = open;
            var material = open ? _openLight : _lockedLight;
            if (_lamp != null && material != null)
                _lamp.sharedMaterial = material;
            if (_blocker != null)
                _blocker.enabled = !open;
            if (_slab == null)
                return;
            var target = open ? _openHeight : _closedHeight;
            if (!animate)
            {
                Tween.StopAll(_slab);
                var position = _slab.localPosition;
                position.y = target;
                _slab.localPosition = position;
                return;
            }
            Tween.LocalPositionY(_slab, target, _openDuration, Ease.InQuad, startDelay: _openDelay);
            if (!_openSound.IsNull)
                RuntimeManager.PlayOneShot(_openSound, transform.position);
        }
    }
}
