using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.World.Hazards
{
    public sealed class HazardZone : MonoBehaviour
    {
        [SerializeField] private HazardKind _kind;
        [Tooltip("Box over the floor: width (x), height (y, from the floor up) and depth (z), in local space.")]
        [SerializeField] private Vector3 _size = new(6f, 2f, 6f);
        [Tooltip("Without the resistance (developer rule: heavy damage over time; short stretches can be braved, long ones can't).")]
        [SerializeField, Min(0f)] private float _damagePerSecond = 25f;
        [SerializeField, Min(0.05f)] private float _tick = 0.25f;
        [Tooltip("Cryo vents: move speed multiplier while inside.")]
        [SerializeField, Range(0.1f, 1f)] private float _slow = 0.5f;
        [Tooltip("Placeholder surface scaled to the zone footprint.")]
        [SerializeField] private Transform _surface;

        private float _nextTick;

        public HazardKind Kind => _kind;
        public Vector3 Size => _size;

        public void Configure(HazardKind kind, Vector3 size, Transform surface = null)
        {
            _kind = kind;
            _size = size;
            if (surface != null)
                _surface = surface;
            FitSurface();
        }

        private void OnValidate() => FitSurface();

        private void FitSurface()
        {
            if (_surface != null)
                _surface.localScale = new Vector3(_size.x, _surface.localScale.y, _size.z);
        }

        private void Update()
        {
            var creature = CombatTargets.Creature;
            if (creature == null || !creature.IsAlive)
                return;
            if (!HazardRules.Inside(transform.InverseTransformPoint(creature.transform.position), _size))
                return;
            var now = Time.time;
            if (now < _nextTick)
                return;
            _nextTick = now + _tick;
            if (!HazardRules.Damages(_kind))
            {
                if (creature.TryGetComponent<StatusEffects>(out var status))
                    status.Apply(StatusKind.Slow, 1f - _slow, _tick * 2f);
                return;
            }
            if (HazardRules.IsResisted(_kind))
                return;
            var info = DamageInfo.Create(_damagePerSecond * _tick, Team.Environment);
            info.Type = HazardRules.DamageTypeFor(_kind);
            info.Flags = HitFlags.Hazard | HitFlags.DamageOverTime;
            info.Point = creature.transform.position;
            creature.TakeDamage(info);
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.8f);
            Gizmos.DrawWireCube(new Vector3(0f, _size.y * 0.5f, 0f), _size);
        }
    }
}
