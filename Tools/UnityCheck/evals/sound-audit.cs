var gaps = new System.Text.StringBuilder();
var attacksMissing = 0;
var voiceMissing = 0;
var abilityMissing = 0;
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:TentacleVania.Enemies.Data.EnemyDefinition", new[] { "Assets/Game" }))
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var def = UnityEditor.AssetDatabase.LoadAssetAtPath<TentacleVania.Enemies.Data.EnemyDefinition>(path);
    if (def == null || path.Contains("/Debug/"))
        continue;
    if (def.Voice == null)
    {
        voiceMissing++;
        gaps.Append("voice ").Append(def.name).Append('\n');
    }
    foreach (var attack in def.Attacks)
    {
        if (!attack.WindUpSound.IsNull && !attack.HitSound.IsNull)
            continue;
        attacksMissing++;
        gaps.Append("attack ").Append(def.name).Append('|').Append(attack.Name).Append('|').Append(attack.WindUpSound.IsNull ? "windup " : "").Append(attack.HitSound.IsNull ? "hit" : "").Append('\n');
    }
}
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:TentacleVania.Abilities.AbilityDefinition", new[] { "Assets/Game/Abilities" }))
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var ability = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.ScriptableObject>(path);
    var so = new UnityEditor.SerializedObject(ability);
    var sound = so.FindProperty("_activateSound");
    if (sound == null)
        continue;
    var reference = (FMODUnity.EventReference)sound.boxedValue;
    if (!reference.IsNull)
        continue;
    abilityMissing++;
    gaps.Append("ability ").Append(ability.name).Append('\n');
}
UnityEditor.SessionState.SetString("soundAuditGaps", gaps.ToString());
System.IO.File.WriteAllText("Temp/sound-audit.txt", gaps.ToString());
return "attacks " + attacksMissing + ", voices " + voiceMissing + ", abilities " + abilityMissing + " (details in Temp/sound-audit.txt)";
