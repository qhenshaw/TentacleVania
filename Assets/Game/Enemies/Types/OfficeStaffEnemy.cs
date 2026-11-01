using TentacleVania.Enemies.Alarms;
using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class OfficeStaffEnemy : ScientistEnemy
    {
        [Tooltip("Alarm panels this close are run to and triggered (design: Office Staff hit alarm panels).")]
        [SerializeField, Min(0f)] private float _alarmRange = 20f;
        [SerializeField, Min(0.1f)] private float _useDistance = 1.3f;
        [SerializeField, Min(0f)] private float _useTime = 1.5f;

        private AlarmPanel _panel;
        private float _usingSince = -1f;

        public bool RaisingAlarm => _panel != null;

        protected override Vector3 ChaseDestination(Vector3 targetPosition)
        {
            if (_panel == null || _panel.Triggered)
            {
                _panel = AlarmPanel.Nearest(Position, _alarmRange);
                _usingSince = -1f;
            }
            if (_panel == null)
                return base.ChaseDestination(targetPosition);
            var offset = _panel.Position - Position;
            offset.y = 0f;
            if (offset.magnitude > _useDistance)
            {
                _usingSince = -1f;
                return _panel.Position;
            }
            if (_usingSince < 0f)
                _usingSince = Time.time;
            if (Time.time - _usingSince >= _useTime)
            {
                _panel.Trigger();
                _panel = null;
            }
            return Position;
        }

        protected override bool AttackAllowed(int index) => _panel == null && base.AttackAllowed(index);
    }
}
