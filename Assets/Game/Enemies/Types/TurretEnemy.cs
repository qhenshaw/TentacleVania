using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public class TurretEnemy : Enemy
    {
        private const float LaserRange = 16f;

        [SerializeField] private LineRenderer _laser;
        [SerializeField] private Transform _laserOrigin;
        [SerializeField, ColorUsage(false, true)] private Color _trackingColor = new(2.5f, 0.15f, 0.1f, 0.8f);
        [SerializeField, ColorUsage(false, true)] private Color _chargingColor = new(3f, 2.2f, 0.2f, 1f);
        [SerializeField, Min(0f)] private float _sweepAngle = 40f;
        [SerializeField, Min(0.01f)] private float _sweepSpeed = 0.35f;

        private readonly RaycastHit[] _hits = new RaycastHit[4];
        private float _baseYaw;

        protected override void OnEnable()
        {
            base.OnEnable();
            _baseYaw = transform.eulerAngles.y;
        }

        protected override Vector3 ChaseDestination(Vector3 targetPosition) => Position;

        protected override Vector3 CircleDestination(Vector3 targetPosition, float dt) => Position;

        protected override bool FaceTarget(float distance) => distance <= LaserRange;

        private void LateUpdate()
        {
            if (_laser == null)
                return;
            var alive = Health != null && Health.IsAlive;
            _laser.enabled = alive;
            if (!alive)
                return;
            if (State == EnemyState.Chase || State == EnemyState.Circle)
                Sweep();
            var origin = _laserOrigin != null ? _laserOrigin.position : transform.position + Vector3.up;
            var direction = transform.forward;
            var length = LaserRange;
            var count = Physics.RaycastNonAlloc(origin, direction, _hits, LaserRange, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
                if (_hits[i].collider.transform.root != transform.root && _hits[i].distance < length)
                    length = _hits[i].distance;
            _laser.SetPosition(0, origin);
            _laser.SetPosition(1, origin + direction * length);
            var color = State == EnemyState.Attacking ? _chargingColor : _trackingColor;
            _laser.startColor = color;
            _laser.endColor = color;
        }

        private void Sweep()
        {
            var creature = Combat.CombatTargets.Creature;
            if (creature != null && (creature.transform.position - Position).sqrMagnitude <= LaserRange * LaserRange)
                return;
            var yaw = _baseYaw + Mathf.Sin(Time.time * _sweepSpeed * Mathf.PI * 2f) * _sweepAngle;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
