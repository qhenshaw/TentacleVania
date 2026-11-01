using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class MirrorEnemy : Enemy
    {
        [Header("Mirror (design: mirrors the player's moves)")]
        [Tooltip("Within this distance it stops mirroring and closes in to attack.")]
        [SerializeField, Min(0f)] private float _engageRange = 3f;

        private Vector3 _anchor;

        public Vector3 Anchor => _anchor;

        public static Vector3 Mirror(Vector3 anchor, Vector3 target)
        {
            var mirrored = anchor * 2f - target;
            mirrored.y = target.y;
            return mirrored;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _anchor = Position;
        }

        protected override Vector3 ChaseDestination(Vector3 targetPosition)
        {
            var offset = targetPosition - Position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= _engageRange * _engageRange)
                return targetPosition;
            var mirrored = Mirror(_anchor, targetPosition);
            return TrySampleNavMesh(mirrored, 2f, out var point) ? point : targetPosition;
        }
    }
}
