using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Enemies
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        private Rigidbody _body;
        private DamageInfo _damage;
        private float _expiresAt;

        public EnemyProjectile Prefab { get; set; }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.useGravity = false;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        public void Launch(Vector3 position, Vector3 velocity, in DamageInfo damage, float lifetime)
        {
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(velocity));
            _body.position = position;
            _damage = damage;
            _expiresAt = Time.time + lifetime;
            gameObject.SetActive(true);
            _body.linearVelocity = velocity;
        }

        private void Update()
        {
            if (Time.time >= _expiresAt)
                Despawn();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Hurtbox.TryGet(other, out var hurtbox))
            {
                var info = _damage;
                info.Point = transform.position;
                info.Direction = _body.linearVelocity.normalized;
                if (hurtbox.Health.TakeDamage(info).Applied)
                    Despawn();
                return;
            }
            if (!other.isTrigger && other.attachedRigidbody == null && other.gameObject.layer != EnemyLayers.Enemy)
                Despawn();
        }

        private void Despawn()
        {
            _body.linearVelocity = Vector3.zero;
            EnemyManager.ReturnProjectile(this);
        }
    }
}
