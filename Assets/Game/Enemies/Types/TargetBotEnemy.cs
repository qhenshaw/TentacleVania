using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class TargetBotEnemy : Enemy
    {
        [Header("Rail (design: sweeps along rails and fires on a pattern)")]
        [Tooltip("Length of its rail, centred where it spawned, along its right axis.")]
        [SerializeField, Min(0.5f)] private float _railLength = 8f;

        private Vector3 _railCenter;
        private Vector3 _railAxis;
        private int _heading = 1;

        public static Vector3 RailTarget(Vector3 center, Vector3 axis, float length, float tolerance, Vector3 position, ref int heading)
        {
            var half = length * 0.5f;
            var along = Vector3.Dot(position - center, axis);
            if (heading > 0 && along >= half - tolerance)
                heading = -1;
            else if (heading < 0 && along <= -half + tolerance)
                heading = 1;
            return center + axis * (half * heading);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _railCenter = Position;
            _railAxis = transform.right;
            _railAxis.y = 0f;
            _railAxis = _railAxis.sqrMagnitude > 1e-4f ? _railAxis.normalized : Vector3.right;
            _heading = 1;
        }

        protected override Vector3 ChaseDestination(Vector3 targetPosition) => RailTarget(_railCenter, _railAxis, _railLength, Agent.stoppingDistance + 0.3f, Position, ref _heading);

        protected override Vector3 CircleDestination(Vector3 targetPosition, float dt) => ChaseDestination(targetPosition);

        protected override bool FaceTarget(float distance) => false;
    }
}
