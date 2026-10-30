using TentacleVania.Combat;
using TentacleVania.Core.Debugging;
using TentacleVania.Enemies.Data;
using TentacleVania.Enemies.Telegraphs;
using UnityEngine;
using UnityEngine.AI;

namespace TentacleVania.Enemies
{
    public static class EnemyDebugCommands
    {
        private const float SpawnDistance = 6f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            const string tab = DebugMenu.Enemies;
            var list = Resources.Load<EnemySpawnList>(EnemySpawnList.ResourcePath);
            if (list != null && list.Prefabs.Length > 0)
            {
                var names = new string[list.Prefabs.Length];
                for (var i = 0; i < names.Length; i++)
                    names[i] = list.Prefabs[i].name;
                DebugMenu.Choice(tab, "Spawn", names, index => Spawn(list.Prefabs[index]));
            }
            DebugMenu.Value(tab, "Alive", () => CountAlive().ToString());
            DebugMenu.Value(tab, "Tokens (melee / ranged)", () =>
            {
                var tokens = EnemyManager.Instance.Tokens;
                return $"{tokens.MeleeHeld}/{tokens.MeleeCapacity}  {tokens.RangedHeld}/{tokens.RangedCapacity}";
            });
            DebugMenu.Value(tab, "HP / stagger / animation", DescribeEnemies);
            DebugMenu.Toggle(tab, "Freeze AI", () => EnemyManager.Instance.Paused, v => EnemyManager.Instance.Paused = v);
            DebugMenu.Toggle(tab, "Force ground indicators", () => GroundIndicator.ForceAll, v => GroundIndicator.ForceAll = v);
            DebugMenu.Button(tab, "Stun all (3 s)", () =>
            {
                foreach (var enemy in EnemyManager.Instance.Enemies)
                    enemy.Stun(3f);
            });
            DebugMenu.Button(tab, "Bring all to 20% HP", () =>
            {
                foreach (var enemy in EnemyManager.Instance.Enemies)
                {
                    var health = enemy.Health;
                    var excess = health.Current - health.Max * 0.2f;
                    if (excess > 0f)
                        health.TakeDamage(Combat.Damage.DamageInfo.Create(excess, Combat.Damage.Team.Environment));
                }
            });
            DebugMenu.Button(tab, "Kill all", () =>
            {
                var enemies = EnemyManager.Instance.Enemies;
                for (var i = enemies.Count - 1; i >= 0; i--)
                    enemies[i].Health.Kill();
            });
        }

        private static void Spawn(Enemy prefab)
        {
            var creature = CombatTargets.Creature;
            var origin = creature != null ? creature.transform.position : Vector3.zero;
            var direction = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
            var position = origin + direction * SpawnDistance;
            if (NavMesh.SamplePosition(position, out var hit, SpawnDistance, NavMesh.AllAreas))
                position = hit.position;
            Object.Instantiate(prefab, position, Quaternion.LookRotation(-direction));
        }

        private static string DescribeEnemies()
        {
            var text = new System.Text.StringBuilder();
            var shown = 0;
            foreach (var enemy in EnemyManager.Instance.Enemies)
            {
                if (enemy.State == EnemyState.Dead || shown++ >= 4)
                    continue;
                if (text.Length > 0)
                    text.Append('\n');
                text.Append(enemy.Definition.DisplayName).Append(": ")
                    .Append(enemy.Health.Current.ToString("F0")).Append(" HP, stagger ")
                    .Append(enemy.Stagger.Current.ToString("F0")).Append('/').Append(enemy.Stagger.Max.ToString("F0"))
                    .Append(enemy.IsStaggerBroken ? " BROKEN" : string.Empty);
                if (enemy.TryGetComponent<EnemyAnimator>(out var animator))
                    text.Append(", anim ").Append(animator.CurrentStateName);
            }
            return text.Length > 0 ? text.ToString() : "-";
        }

        private static int CountAlive()
        {
            var count = 0;
            foreach (var enemy in EnemyManager.Instance.Enemies)
                if (enemy.State != EnemyState.Dead)
                    count++;
            return count;
        }
    }
}
