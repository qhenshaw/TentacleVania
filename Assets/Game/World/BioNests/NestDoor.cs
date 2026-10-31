using FMODUnity;
using PrimeTween;
using TentacleVania.Core.Progression;
using UnityEngine;

namespace TentacleVania.World.BioNests
{
    public sealed class NestDoor : MonoBehaviour
    {
        [Tooltip("Nest save id that opens this door once grown (the pod chamber's SH/PodNest).")]
        [SerializeField] private string _nestId = "SH/PodNest";
        [SerializeField] private Transform _slab;
        [SerializeField] private Collider _blocker;
        [SerializeField] private float _openHeight = -3.2f;
        [SerializeField, Min(0f)] private float _openDuration = 0.8f;
        [SerializeField] private Renderer _lamp;
        [SerializeField] private Material _lockedLight;
        [SerializeField] private Material _openLight;
        [SerializeField] private EventReference _openSound;

        private float _closedHeight;

        public bool IsOpen { get; private set; }
        public string NestId => _nestId;

        private void Awake()
        {
            if (_slab != null)
                _closedHeight = _slab.localPosition.y;
        }

        private void OnEnable()
        {
            Nests.Grown += OnGrown;
            Apply(Nests.IsGrown(_nestId), false);
        }

        private void OnDisable() => Nests.Grown -= OnGrown;

        private void OnGrown(string id)
        {
            if (id == _nestId && !IsOpen)
                Apply(true, true);
        }

        private void Apply(bool open, bool animate)
        {
            IsOpen = open;
            SetLamp();
            if (_blocker != null)
                _blocker.enabled = !open;
            if (_slab == null)
                return;
            var target = open ? _openHeight : _closedHeight;
            if (!animate)
            {
                var position = _slab.localPosition;
                position.y = target;
                _slab.localPosition = position;
                return;
            }
            Tween.LocalPositionY(_slab, target, _openDuration, Ease.InQuad);
            if (!_openSound.IsNull)
                RuntimeManager.PlayOneShot(_openSound, transform.position);
        }

        private void SetLamp()
        {
            var material = IsOpen ? _openLight : _lockedLight;
            if (_lamp != null && material != null)
                _lamp.sharedMaterial = material;
        }
    }
}
