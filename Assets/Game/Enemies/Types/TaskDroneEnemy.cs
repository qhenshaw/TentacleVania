using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class TaskDroneEnemy : Enemy
    {
        public Transform Goal { get; set; }
        public float SpeedScale { get; set; } = 1f;

        public bool AtGoal(float range)
        {
            if (Goal == null)
                return false;
            var offset = Goal.position - Position;
            offset.y = 0f;
            return offset.sqrMagnitude <= range * range;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Goal = null;
            SpeedScale = 1f;
        }

        protected override Vector3 ChaseDestination(Vector3 targetPosition) => Goal != null ? Goal.position : base.ChaseDestination(targetPosition);

        protected override Vector3 CircleDestination(Vector3 targetPosition, float dt) => Goal != null ? Goal.position : base.CircleDestination(targetPosition, dt);

        protected override bool AttackAllowed(int index) => Goal == null && base.AttackAllowed(index);

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            BaseSpeed = Definition.MoveSpeed * SpeedScale;
        }
    }
}
