using System.Collections.Generic;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies.AI;
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

        public static EnemyManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GameObject(nameof(EnemyManager)).AddComponent<EnemyManager>();
                return _instance;
            }
        }

        public AttackTokens Tokens { get; } = new(MeleeTokens, RangedTokens);
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

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
