using PrimeTween;
using UnityEngine;

namespace TentacleVania.World.Arenas
{
    public sealed class ArenaDoor : MonoBehaviour
    {
        [SerializeField] private Transform _slab;
        [SerializeField] private Collider _blocker;
        [SerializeField] private float _openHeight = -1.2f;
        [SerializeField] private float _closedHeight;
        [SerializeField, Min(0f)] private float _duration = 0.35f;
        [SerializeField] private bool _startOpen;

        private Tween _tween;

        public bool IsSealed { get; private set; }

        private void Awake() => Snap(!_startOpen);

        public void Seal() => Move(true);

        public void Open() => Move(false);

        private void Move(bool seal)
        {
            if (IsSealed == seal)
                return;
            IsSealed = seal;
            _blocker.enabled = seal;
            _tween.Stop();
            _tween = Tween.LocalPositionY(_slab, seal ? _closedHeight : _openHeight, _duration, seal ? Ease.OutBack : Ease.InQuad);
        }

        private void Snap(bool seal)
        {
            IsSealed = seal;
            _blocker.enabled = seal;
            var position = _slab.localPosition;
            position.y = seal ? _closedHeight : _openHeight;
            _slab.localPosition = position;
        }
    }
}
