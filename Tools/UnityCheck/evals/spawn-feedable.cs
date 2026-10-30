TentacleVania.Enemies.EnemyManager.Instance.Paused = true;
var prefabPath = UnityEditor.SessionState.GetString("feedablePrefab", "Assets/Game/Enemies/Prefabs/Scientist.prefab");
var creature = TentacleVania.Creature.CreatureSpawner.Spawned.transform;
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath);
var position = creature.position + creature.forward * 1.6f;
var enemy = UnityEngine.Object.Instantiate(prefab, position, UnityEngine.Quaternion.LookRotation(-creature.forward));
enemy.name = "Feedable";
var health = enemy.GetComponent<TentacleVania.Combat.Health>();
for (var i = 0; i < 40 && health.Normalized >= TentacleVania.Combat.Consumable.GrabThreshold * 0.8f; i++)
    health.TakeDamage(TentacleVania.Combat.Damage.DamageInfo.Create(health.Max * 0.05f, TentacleVania.Combat.Damage.Team.Environment));
return "Feedable at " + position.ToString("F1") + " hp " + health.Current + " / " + health.Max;
