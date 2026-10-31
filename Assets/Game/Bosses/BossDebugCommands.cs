using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Debugging;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using UnityEngine;
using UnityEngine.AI;

namespace TentacleVania.Bosses
{
    public static class BossDebugCommands
    {
        public const string Tab = "Bosses";
        public const string TestBossResource = "DebugTestBoss";
        private const float SpawnDistance = 7f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Value(Tab, "Boss", Describe);
            DebugMenu.Button(Tab, "Spawn test boss", SpawnTestBoss);
            DebugMenu.Button(Tab, "HP to next phase", ToNextPhase);
            DebugMenu.Button(Tab, "Spawn add", () =>
            {
                var boss = BossEnemy.Active;
                if (boss == null)
                    return;
                var prefab = boss.Boss.Phases[Mathf.Max(0, boss.Phase)].AddPrefab;
                if (prefab != null)
                    boss.SpawnAdd(prefab);
            });
            DebugMenu.Button(Tab, "Kill boss", () =>
            {
                if (BossEnemy.Active != null)
                    BossEnemy.Active.Health.Kill();
            });
            DebugMenu.Button(Tab, "Reset defeated bosses", ResetDefeated);
        }

        public static void SpawnTestBoss()
        {
            var prefab = Resources.Load<BossEnemy>(TestBossResource);
            if (prefab == null)
                return;
            BossProgress.Revive(prefab.Boss.Id);
            var creature = CombatTargets.Creature;
            var origin = creature != null ? creature.transform.position : Vector3.zero;
            var forward = creature != null ? creature.transform.forward : Vector3.forward;
            var position = origin + forward * SpawnDistance;
            if (NavMesh.SamplePosition(position, out var hit, SpawnDistance, NavMesh.AllAreas))
                position = hit.position;
            Object.Instantiate(prefab, position, Quaternion.LookRotation(-forward));
        }

        public static void ToNextPhase()
        {
            var boss = BossEnemy.Active;
            if (boss == null)
                return;
            var thresholds = boss.Boss.Thresholds;
            var next = boss.Phase + 1;
            var target = next < thresholds.Count ? thresholds[next] : 0.05f;
            var excess = boss.Health.Current - boss.Health.Max * target;
            if (excess > 0f)
                boss.Health.TakeDamage(DamageInfo.Create(excess + 0.01f, Team.Environment));
        }

        private static void ResetDefeated()
        {
            var defeated = SaveSystem.Data.World.DefeatedBosses;
            for (var i = defeated.Count - 1; i >= 0; i--)
                BossProgress.Revive(defeated[i]);
        }

        private static string Describe()
        {
            var boss = BossEnemy.Active;
            if (boss == null)
                return "none";
            var stagger = boss.Stagger;
            return $"{boss.Definition.DisplayName}: phase {boss.Phase + 1}/{boss.Boss.Phases.Count}, {boss.Health.Current:F0}/{boss.Health.Max:F0} HP, stagger {stagger.Current:F0}/{stagger.Max:F0}, adds {boss.Adds.Count}, {boss.State}";
        }
    }
}
