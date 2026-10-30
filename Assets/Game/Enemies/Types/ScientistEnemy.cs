using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class ScientistEnemy : Enemy
    {
        [SerializeField, Min(0f)] private float _panicRange = 7f;
        [SerializeField, Min(0f)] private float _fleeDistance = 6f;
        [SerializeField, Min(0f)] private float _corneredDistance = 1.2f;

        private bool _cowering;

        public override bool IsCowering => _cowering;

        protected override Vector3 ChaseDestination(Vector3 targetPosition)
        {
            var away = Position - targetPosition;
            away.y = 0f;
            var distance = away.magnitude;
            if (distance > _panicRange)
            {
                _cowering = false;
                return Position;
            }

            var direction = distance > 1e-3f ? away / distance : transform.forward;
            for (var turn = 0; turn < 5; turn++)
            {
                var angle = (turn + 1) / 2 * 35f * (turn % 2 == 0 ? 1f : -1f);
                var candidate = Position + Quaternion.Euler(0f, angle, 0f) * direction * _fleeDistance;
                if (TrySampleNavMesh(candidate, 1.5f, out var flee) && (flee - Position).sqrMagnitude > _corneredDistance * _corneredDistance
                    && (flee - targetPosition).sqrMagnitude > (Position - targetPosition).sqrMagnitude)
                {
                    _cowering = false;
                    return flee;
                }
            }
            _cowering = true;
            return Position;
        }

        protected override Vector3 CircleDestination(Vector3 targetPosition, float dt) => ChaseDestination(targetPosition);

        protected override bool FaceTarget(float distance) => IsCowering || distance < 2f;
    }
}
