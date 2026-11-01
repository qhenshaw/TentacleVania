using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using TentacleVania.Enemies.Data;
using TentacleVania.Enemies.Telegraphs;
using UnityEngine;

namespace TentacleVania.Enemies.Traps
{
    public sealed class FireZone : MonoBehaviour
    {
        [SerializeField] private GroundIndicator _indicator;
        [SerializeField, Min(0.05f)] private float _tick = 0.25f;
        [SerializeField] private EventReference _igniteSound;
        [SerializeField] private DamageType _type = DamageType.Fire;
        [SerializeField] private StatusKind _status = StatusKind.Burn;
        [Tooltip("Status magnitude and duration (0 uses the status defaults; Slow and Root need a duration).")]
        [SerializeField, Min(0f)] private float _statusMagnitude;
        [SerializeField, Min(0f)] private float _statusDuration;
        [Tooltip("Colour of the warning before it goes live (Undodgeable for red grabs).")]
        [SerializeField] private Telegraph _warnTelegraph = Telegraph.Dodgeable;

        private Vector3 _forward;
        private float _radius;
        private float _arc;
        private float _armedAt;
        private float _warn;
        private float _duration;
        private float _damagePerSecond;
        private float _nextTick;
        private Health _source;
        private bool _lit;
        private Health _cachedCreature;
        private StatusEffects _cachedStatus;

        public FireZone Prefab { get; set; }
        public bool Lit => _lit;

        public static bool Inside(Vector3 center, Vector3 forward, float radius, float arc, Vector3 point)
        {
            var offset = point - center;
            offset.y = 0f;
            if (offset.sqrMagnitude > radius * radius)
                return false;
            if (arc >= 360f || offset.sqrMagnitude < 1e-4f)
                return true;
            forward.y = 0f;
            return Vector3.Angle(forward, offset) <= arc * 0.5f;
        }

        public void Arm(Vector3 center, Vector3 forward, float radius, float arc, float warn, float duration, float damagePerSecond, Health source)
        {
            transform.position = center;
            forward.y = 0f;
            _forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            _radius = radius;
            _arc = arc;
            _warn = warn;
            _duration = duration;
            _damagePerSecond = damagePerSecond;
            _source = source;
            _armedAt = Time.time;
            _lit = false;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            var elapsed = Time.time - _armedAt;
            if (elapsed >= _warn + _duration)
            {
                if (_indicator != null)
                    _indicator.Hide();
                EnemyManager.ReturnFire(this);
                return;
            }
            var lit = elapsed >= _warn;
            if (lit && !_lit)
            {
                _lit = true;
                _nextTick = Time.time;
                if (!_igniteSound.IsNull)
                    RuntimeManager.PlayOneShot(_igniteSound, transform.position);
            }
            Draw(lit ? 1f : elapsed / _warn, lit);
            if (lit && Time.time >= _nextTick)
            {
                _nextTick = Time.time + _tick;
                Burn();
            }
        }

        private void Draw(float progress, bool lit)
        {
            if (_indicator == null)
                return;
            var telegraph = lit ? Telegraph.Undodgeable : _warnTelegraph;
            if (_arc >= 360f)
                _indicator.ShowCircle(transform.position, _radius, telegraph, progress, true);
            else
                _indicator.ShowSector(transform.position, _forward, _radius, _arc, telegraph, progress, true);
        }

        private void Burn()
        {
            var creature = CombatTargets.Creature;
            if (creature == null || !creature.IsAlive || !Inside(transform.position, _forward, _radius, _arc, creature.transform.position))
                return;
            var info = DamageInfo.Create(_damagePerSecond * _tick, Team.Site, _type);
            info.Flags = HitFlags.Hazard | HitFlags.DamageOverTime;
            info.Attacker = _source;
            info.Point = creature.transform.position;
            creature.TakeDamage(info);
            if (_status == StatusKind.None)
                return;
            if (creature != _cachedCreature)
            {
                _cachedCreature = creature;
                _cachedStatus = creature.GetComponent<StatusEffects>();
            }
            if (_cachedStatus != null)
                _cachedStatus.Apply(_status, _statusMagnitude, _statusDuration);
        }
    }
}
