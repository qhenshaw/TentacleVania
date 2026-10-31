var prefabPath = UnityEditor.SessionState.GetString("fightPrefab", "Assets/Game/Enemies/Prefabs/SecurityGuard.prefab");
var seconds = UnityEditor.SessionState.GetFloat("fightSeconds", 8f);
var creature = TentacleVania.Combat.CombatTargets.Creature;
foreach (var arena in UnityEngine.Object.FindObjectsByType<TentacleVania.World.Arenas.ArenaEncounter>())
    arena.enabled = false;
var controller = creature.GetComponent<UnityEngine.CharacterController>();
controller.enabled = false;
creature.transform.SetPositionAndRotation(new UnityEngine.Vector3(0f, 0f, -2f), UnityEngine.Quaternion.identity);
controller.enabled = true;
var spawn = new UnityEngine.Vector3(0f, 0f, 4f);
if (UnityEngine.AI.NavMesh.SamplePosition(spawn, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas)) spawn = hit.position;
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath);
var enemy = UnityEngine.Object.Instantiate(prefab, spawn, UnityEngine.Quaternion.LookRotation(UnityEngine.Vector3.back));
enemy.name = "Fighter";
var status = creature.GetComponent<TentacleVania.Combat.Status.StatusEffects>();
var log = new System.Collections.Generic.List<string>();
var start = UnityEngine.Time.time;
var startHealth = creature.Current;
TentacleVania.Combat.DamageHandler onHit = (TentacleVania.Combat.Health target, in TentacleVania.Combat.Damage.DamageInfo info, in TentacleVania.Combat.Damage.DamageResult result) =>
{
    if (target == creature)
        log.Add($"{UnityEngine.Time.time - start:F1}s {result.HealthDamage:F1}{(info.Status != TentacleVania.Combat.Damage.StatusKind.None ? " " + info.Status : "")}");
};
TentacleVania.Combat.CombatEvents.HitLanded += onHit;
UnityEditor.SessionState.SetString("fightLog", "running");
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (!UnityEditor.EditorApplication.isPlaying) { TentacleVania.Combat.CombatEvents.HitLanded -= onHit; UnityEditor.EditorApplication.update -= tick; return; }
    if (UnityEngine.Time.time - start < seconds) return;
    TentacleVania.Combat.CombatEvents.HitLanded -= onHit;
    UnityEditor.EditorApplication.update -= tick;
    var state = enemy != null ? enemy.GetComponent<TentacleVania.Enemies.Enemy>().State.ToString() : "destroyed";
    UnityEditor.SessionState.SetString("fightLog", $"{prefab.name}: {log.Count} hits on the creature [{string.Join(", ", log)}], creature hp {startHealth} -> {creature.Current}, enemy state {state}" + (status != null ? $", creature move scale {status.MoveScale:F2}" : ""));
};
UnityEditor.EditorApplication.update += tick;
return "fighting " + prefab.name + " for " + seconds + " s";
