using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class ScientistEnemy : Enemy
    {

        private bool _cowering;

        public override bool IsCowering => _cowering;

        protected override Vector3 ChaseDestination(Vector3 targetPosition)
        {
            var away = Position - targetPosition;
            away.y = 0f;
            var distance = away.magnitude;
            if (distance > Definition.PanicRange)
            {
                _cowering = false;
                return Position;
            }

            var direction = distance > 1e-3f ? away / distance : transform.forward;
            for (var turn = 0; turn < 5; turn++)
            {
                var angle = (turn + 1) / 2 * 35f * (turn % 2 == 0 ? 1f : -1f);
                var candidate = Position + Quaternion.Euler(0f, angle, 0f) * direction * Definition.FleeDistance;
                if (TrySampleNavMesh(candidate, 1.5f, out var flee) && (flee - Position).sqrMagnitude > Definition.CorneredDistance * Definition.CorneredDistance
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
