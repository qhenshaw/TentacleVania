using System.Collections.Generic;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies.AI;
using TentacleVania.Enemies.Traps;
using UnityEngine;

namespace TentacleVania.Enemies
{
    [DefaultExecutionOrder(-50)]
    public sealed class EnemyManager : MonoBehaviour
    {
        public const int MeleeTokens = 2;
        public const int RangedTokens = 2;

        private static EnemyManager _instance;

        private readonly List<Enemy> _enemies = new(64);
        private readonly Dictionary<EnemyProjectile, Stack<EnemyProjectile>> _projectilePools = new();
        private readonly Dictionary<ContainmentTrap, Stack<ContainmentTrap>> _trapPools = new();
        private readonly Dictionary<AcidBurst, Stack<AcidBurst>> _burstPools = new();
        private readonly Dictionary<IceWall, Stack<IceWall>> _wallPools = new();
        private readonly Dictionary<FireZone, Stack<FireZone>> _firePools = new();

        public static EnemyManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GameObject(nameof(EnemyManager)).AddComponent<EnemyManager>();
                return _instance;
            }
        }

        private bool _paused;

        public static EnemyManager Current => _instance;

        public AttackTokens Tokens { get; } = new(MeleeTokens, RangedTokens);

        public bool Paused
        {
            get => _paused;
            set
            {
                _paused = value;
                foreach (var enemy in _enemies)
                    enemy.SetFrozen(value);
            }
        }
        public IReadOnlyList<Enemy> Enemies => _enemies;

        public static void Register(Enemy enemy) => Instance._enemies.Add(enemy);

        public static void Unregister(Enemy enemy)
        {
            if (_instance == null)
                return;
            _instance._enemies.Remove(enemy);
            _instance.Tokens.Release(enemy);
        }

        public static void ReleaseTokens(Enemy enemy)
        {
            if (_instance != null)
                _instance.Tokens.Release(enemy);
        }

        private void Update()
        {
            for (var i = 0; i < _enemies.Count; i++)
                _enemies[i].TickFeedback();
            if (_paused)
                return;
            var now = Time.time;
            var dt = Time.deltaTime;
            for (var i = _enemies.Count - 1; i >= 0; i--)
            {
                if (i < _enemies.Count)
                    _enemies[i].Tick(now, dt);
            }
        }

        public static void FireProjectile(EnemyProjectile prefab, Vector3 position, Vector3 velocity, in DamageInfo damage, float lifetime)
        {
            var manager = Instance;
            if (!manager._projectilePools.TryGetValue(prefab, out var pool))
                manager._projectilePools[prefab] = pool = new Stack<EnemyProjectile>(8);
            var projectile = pool.Count > 0 ? pool.Pop() : null;
            if (projectile == null)
            {
                projectile = Instantiate(prefab, manager.transform);
                projectile.Prefab = prefab;
            }
            projectile.Launch(position, velocity, damage, lifetime);
        }

        public static void ReturnProjectile(EnemyProjectile projectile)
        {
            projectile.gameObject.SetActive(false);
            if (_instance == null || projectile.Prefab == null)
            {
                Destroy(projectile.gameObject);
                return;
            }
            if (!_instance._projectilePools.TryGetValue(projectile.Prefab, out var pool))
                _instance._projectilePools[projectile.Prefab] = pool = new Stack<EnemyProjectile>(8);
            pool.Push(projectile);
        }

        public static void PlaceTrap(ContainmentTrap prefab, Vector3 position, float radius, StatusKind status, float magnitude, float duration, float lifetime)
        {
            var manager = Instance;
            if (!manager._trapPools.TryGetValue(prefab, out var pool))
                manager._trapPools[prefab] = pool = new Stack<ContainmentTrap>(4);
            var trap = pool.Count > 0 ? pool.Pop() : null;
            if (trap == null)
            {
                trap = Instantiate(prefab, manager.transform);
                trap.Prefab = prefab;
            }
            trap.Arm(position, radius, status, magnitude, duration, lifetime);
        }

        public static void ReturnTrap(ContainmentTrap trap)
        {
            trap.gameObject.SetActive(false);
            if (_instance == null || trap.Prefab == null)
            {
                Destroy(trap.gameObject);
                return;
            }
            if (!_instance._trapPools.TryGetValue(trap.Prefab, out var pool))
                _instance._trapPools[trap.Prefab] = pool = new Stack<ContainmentTrap>(4);
            pool.Push(trap);
        }

        public static void PlaceBurst(AcidBurst prefab, Vector3 position, float radius, float fuse, in DamageInfo info)
        {
            var manager = Instance;
            if (!manager._burstPools.TryGetValue(prefab, out var pool))
                manager._burstPools[prefab] = pool = new Stack<AcidBurst>(4);
            var burst = pool.Count > 0 ? pool.Pop() : null;
            if (burst == null)
            {
                burst = Instantiate(prefab, manager.transform);
                burst.Prefab = prefab;
            }
            burst.Arm(position, radius, fuse, info);
        }

        public static void PrewarmBursts(AcidBurst prefab, int count)
        {
            var manager = Instance;
            if (!manager._burstPools.TryGetValue(prefab, out var pool))
                manager._burstPools[prefab] = pool = new Stack<AcidBurst>(count);
            while (pool.Count < count)
            {
                var burst = Instantiate(prefab, manager.transform);
                burst.Prefab = prefab;
                burst.gameObject.SetActive(false);
                pool.Push(burst);
            }
        }

        public static IceWall RaiseWall(IceWall prefab, Vector3 position, Vector3 along, float width, float lifetime)
        {
            var manager = Instance;
            if (!manager._wallPools.TryGetValue(prefab, out var pool))
                manager._wallPools[prefab] = pool = new Stack<IceWall>(4);
            var wall = pool.Count > 0 ? pool.Pop() : null;
            if (wall == null)
            {
                wall = Instantiate(prefab, manager.transform);
                wall.Prefab = prefab;
            }
            wall.Raise(position, along, width, lifetime);
            return wall;
        }

        public static FireZone PlaceFire(FireZone prefab, Vector3 center, Vector3 forward, float radius, float arc, float warn, float duration, float damagePerSecond, Health source)
        {
            var manager = Instance;
            if (!manager._firePools.TryGetValue(prefab, out var pool))
                manager._firePools[prefab] = pool = new Stack<FireZone>(8);
            var zone = pool.Count > 0 ? pool.Pop() : null;
            if (zone == null)
            {
                zone = Instantiate(prefab, manager.transform);
                zone.Prefab = prefab;
            }
            zone.Arm(center, forward, radius, arc, warn, duration, damagePerSecond, source);
            return zone;
        }

        public static void ReturnFire(FireZone zone)
        {
            zone.gameObject.SetActive(false);
            if (_instance == null || zone.Prefab == null)
            {
                Destroy(zone.gameObject);
                return;
            }
            if (!_instance._firePools.TryGetValue(zone.Prefab, out var pool))
                _instance._firePools[zone.Prefab] = pool = new Stack<FireZone>(8);
            pool.Push(zone);
        }

        public static void ReturnWall(IceWall wall)
        {
            wall.gameObject.SetActive(false);
            if (_instance == null || wall.Prefab == null)
            {
                Destroy(wall.gameObject);
                return;
            }
            if (!_instance._wallPools.TryGetValue(wall.Prefab, out var pool))
                _instance._wallPools[wall.Prefab] = pool = new Stack<IceWall>(4);
            pool.Push(wall);
        }

        public static void ReturnBurst(AcidBurst burst)
        {
            burst.gameObject.SetActive(false);
            if (_instance == null || burst.Prefab == null)
            {
                Destroy(burst.gameObject);
                return;
            }
            if (!_instance._burstPools.TryGetValue(burst.Prefab, out var pool))
                _instance._burstPools[burst.Prefab] = pool = new Stack<AcidBurst>(4);
            pool.Push(burst);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
