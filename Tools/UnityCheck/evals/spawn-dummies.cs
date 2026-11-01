var prefabName = UnityEditor.SessionState.GetString("dummyPrefab", "SecurityGuard");
var count = UnityEditor.SessionState.GetInt("dummyCount", 3);
var ai = UnityEditor.SessionState.GetBool("dummyAI", false);
var guids = UnityEditor.AssetDatabase.FindAssets(prefabName + " t:Prefab", new[] { "Assets/Game" });
UnityEngine.GameObject prefab = null;
foreach (var guid in guids)
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    if (System.IO.Path.GetFileNameWithoutExtension(path) == prefabName)
        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
}
if (prefab == null)
    return "no prefab named " + prefabName;
if (!ai)
    TentacleVania.Enemies.EnemyManager.Instance.Paused = true;
var creature = TentacleVania.Combat.CombatTargets.Creature;
var centre = creature.transform.position + UnityEngine.Vector3.forward * UnityEditor.SessionState.GetFloat("dummyDistance", 2.5f);
var log = "";
for (var i = 0; i < count; i++)
{
    var go = UnityEngine.Object.Instantiate(prefab, centre + new UnityEngine.Vector3((i - (count - 1) * 0.5f) * 1.2f, 0f, 0f), UnityEngine.Quaternion.identity);
    go.name = "Dummy" + i;
    log += go.name + " ";
}
return "spawned " + log + "from " + prefabName + (ai ? " (AI on)" : " (AI off)");
