using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Enemies.Traps
{
    public sealed class ContainmentTrap : MonoBehaviour
    {
        private const float DiscHeight = 0.02f;

        [SerializeField] private Transform _disc;
        [SerializeField] private EventReference _snapSound;
        [Tooltip("Proximity mine: stepping in arms this pooled burst (its circle fills over the fuse, then it blasts) instead of only applying the status.")]
        [SerializeField] private AcidBurst _burst;
        [SerializeField, Min(0.1f)] private float _burstRadius = 2f;
        [SerializeField, Min(0.4f)] private float _burstFuse = 0.6f;
        [SerializeField, Min(0f)] private float _burstDamage = 20f;

        private float _radius;
        private StatusKind _status;
        private float _magnitude;
        private float _duration;
        private float _expiresAt;
        private Health _cachedCreature;
        private StatusEffects _cachedStatus;

        public ContainmentTrap Prefab { get; set; }
        public float Radius => _radius;

        public static bool Contains(Vector3 center, float radius, Vector3 position)
        {
            var offset = position - center;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius;
        }

        public void Arm(Vector3 position, float radius, StatusKind status, float magnitude, float duration, float lifetime)
        {
            transform.position = position;
            _radius = radius;
            _status = status;
            _magnitude = magnitude;
            _duration = duration;
            _expiresAt = Time.time + lifetime;
            if (_disc != null)
                _disc.localScale = new Vector3(radius * 2f, DiscHeight, radius * 2f);
            gameObject.SetActive(true);
            TrySnap();
        }

        private void Update()
        {
            if (Time.time >= _expiresAt)
            {
                EnemyManager.ReturnTrap(this);
                return;
            }
            TrySnap();
        }

        private void TrySnap()
        {
            var creature = CombatTargets.Creature;
            if (creature == null || !creature.IsAlive || !Contains(transform.position, _radius, creature.transform.position))
                return;
            if (creature != _cachedCreature)
            {
                _cachedCreature = creature;
                _cachedStatus = creature.GetComponent<StatusEffects>();
            }
            if (_cachedStatus != null && _status != StatusKind.None)
                _cachedStatus.Apply(_status, _magnitude, _duration);
            if (_burst != null)
            {
                var info = DamageInfo.Create(_burstDamage, Team.Site, DamageType.Physical);
                EnemyManager.PlaceBurst(_burst, transform.position, _burstRadius, _burstFuse, info);
            }
            if (!_snapSound.IsNull)
                RuntimeManager.PlayOneShot(_snapSound, transform.position);
            EnemyManager.ReturnTrap(this);
        }
    }
}
