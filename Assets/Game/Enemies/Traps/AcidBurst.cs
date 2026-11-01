using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using TentacleVania.Enemies.AI;
using TentacleVania.Enemies.Data;
using TentacleVania.Enemies.Telegraphs;
using UnityEngine;

namespace TentacleVania.Enemies.Traps
{
    public sealed class AcidBurst : MonoBehaviour
    {
        private static readonly Collider[] Overlaps = new Collider[16];
        private static readonly Health[] Struck = new Health[16];

        [SerializeField] private GroundIndicator _indicator;
        [SerializeField] private EventReference _burstSound;
        [SerializeField, Min(0f)] private float _poolDuration = 5f;
        [SerializeField, Min(0f)] private float _poolDamagePerSecond = 6f;

        private float _radius;
        private float _armedAt;
        private float _fuse;
        private DamageInfo _info;

        public AcidBurst Prefab { get; set; }
        public float Radius => _radius;
        public float Progress => _fuse > 0f ? Mathf.Clamp01((Time.time - _armedAt) / _fuse) : 1f;

        public void Arm(Vector3 position, float radius, float fuse, in DamageInfo info)
        {
            transform.position = position;
            _radius = radius;
            _fuse = fuse;
            _armedAt = Time.time;
            _info = info;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            var progress = Progress;
            if (progress < 1f)
            {
                if (_indicator != null)
                    _indicator.ShowCircle(transform.position, _radius, Telegraph.Dodgeable, progress, true);
                return;
            }
            Detonate();
        }

        private void Detonate()
        {
            if (_indicator != null)
                _indicator.Hide();
            var center = transform.position;
            var count = Physics.OverlapCapsuleNonAlloc(center, center + Vector3.up * 2f, _radius, Overlaps, EnemyLayers.HurtboxMask, QueryTriggerInteraction.Collide);
            var struck = 0;
            for (var i = 0; i < count; i++)
            {
                var collider = Overlaps[i];
                if (!Hurtbox.TryGet(collider, out var hurtbox) || !AttackMath.InsideArea(center, _radius, collider.ClosestPoint(center + Vector3.up)))
                    continue;
                var health = hurtbox.Health;
                var seen = false;
                for (var s = 0; s < struck; s++)
                    seen |= Struck[s] == health;
                if (seen || struck >= Struck.Length)
                    continue;
                Struck[struck++] = health;
                var info = _info;
                info.Point = health.transform.position;
                var away = health.transform.position - center;
                away.y = 0f;
                info.Direction = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.forward;
                health.TakeDamage(info);
            }
            for (var s = 0; s < struck; s++)
                Struck[s] = null;
            if (_poolDuration > 0f)
                AcidPools.Spawn(center, Team.Site, _radius * 0.8f, _poolDuration, _poolDamagePerSecond);
            if (!_burstSound.IsNull)
                RuntimeManager.PlayOneShot(_burstSound, center);
            EnemyManager.ReturnBurst(this);
        }
    }
}
