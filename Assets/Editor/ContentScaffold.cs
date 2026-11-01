using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FMODUnity;
using TentacleVania.Bosses;
using TentacleVania.Enemies;
using TentacleVania.Enemies.Data;
using TentacleVania.Weapons.Data;
using UnityEditor;
using UnityEngine;

public static class ContentScaffold
{
    public const string EnemyFolder = "Assets/Game/Enemies";
    public const string BossFolder = "Assets/Game/Bosses";
    public const string CatalogPath = "Assets/Game/Core/Data/ContentCatalog.asset";
    public const string EnemySpawnListPath = "Assets/Game/Enemies/Debug/Resources/DebugEnemySpawnList.asset";
    public const string BossSpawnListPath = "Assets/Game/Bosses/Debug/Resources/DebugBossSpawnList.asset";
    public const string WeaponFolder = "Assets/Game/Weapons/Data/Weapons";
    public const string MutationFolder = "Assets/Game/Abilities/Mutations/Data";
    public const string AbilityFolder = "Assets/Game/Abilities";

    public static readonly string[] VoiceMoments = { "Alert", "Pain", "Death", "Grabbed", "Attack" };

    public static string Create(string kind, string name, string from, string title)
    {
        var report = new StringBuilder();
        try
        {
            switch (kind)
            {
                case "enemy":
                    CreateEnemy(name, string.IsNullOrEmpty(from) ? "Handler" : from, false, report);
                    break;
                case "boss":
                    CreateEnemy(name, string.IsNullOrEmpty(from) ? "WardenDrone" : from, true, report);
                    break;
                case "weapon":
                    CreateWeapon(name, string.IsNullOrEmpty(from) ? "CrusherMass" : from, title, report);
                    break;
                case "ability":
                    CreateAbility(name, title, report);
                    break;
                case "mutation":
                    CreateMutation(name, report);
                    break;
                default:
                    return $"ERROR unknown kind {kind} (enemy, boss, weapon, ability, mutation)";
            }
            AssetDatabase.SaveAssets();
        }
        catch (Exception e)
        {
            report.AppendLine("ERROR " + e.Message);
        }
        return report.ToString().TrimEnd();
    }

    public static string LowerFirst(string name) => char.ToLowerInvariant(name[0]) + name.Substring(1);

    private static void CreateEnemy(string name, string from, bool boss, StringBuilder report)
    {
        var sourcePath = FindPrefab<Enemy>(from) ?? throw new ArgumentException($"no enemy prefab named {from}");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath).GetComponent<Enemy>();
        if (boss && source is not BossEnemy)
            throw new ArgumentException($"{from} is not a boss (BossEnemy) prefab");
        if (!boss && source is BossEnemy)
            throw new ArgumentException($"{from} is a boss prefab; use the boss kind");

        var folder = boss ? EnsureFolder($"{BossFolder}/{name}") : $"{EnemyFolder}/Prefabs";
        var prefabPath = $"{folder}/{name}.prefab";
        var definitionPath = boss ? $"{folder}/{name}.asset" : $"{EnemyFolder}/Data/Enemies/{name}.asset";
        var materialFolder = boss ? folder : $"{EnemyFolder}/Materials";
        foreach (var path in new[] { prefabPath, definitionPath })
            if (File.Exists(path))
                throw new ArgumentException($"{path} already exists");

        var sourceDefinition = (EnemyDefinition)Serialized(source).FindProperty("_definition").objectReferenceValue;
        if (sourceDefinition == null)
            throw new ArgumentException($"{from} has no EnemyDefinition");
        Copy(AssetDatabase.GetAssetPath(sourceDefinition), definitionPath, report);
        var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(definitionPath);

        var voicePath = $"{EnemyFolder}/Data/Voices/{name}Voice.asset";
        var voice = ScriptableObject.CreateInstance<EnemyVoice>();
        AssetDatabase.CreateAsset(voice, voicePath);
        var voiceObject = new SerializedObject(voice);
        var missing = new List<string>();
        foreach (var moment in boss ? VoiceMoments.Append("Stagger") : VoiceMoments)
            if (!SetEvent(voiceObject, "_" + moment.ToLowerInvariant(), $"event:/Enemies/Voice/{name}/{moment}"))
                missing.Add(moment);
        voiceObject.FindProperty("_painCooldown").floatValue = 1.2f;
        voiceObject.ApplyModifiedPropertiesWithoutUndo();
        report.AppendLine($"created {voicePath}" + (missing.Count > 0 ? $" (no FMOD events yet for {string.Join(", ", missing)})" : ""));
        SetReference(definition, "_voice", voice);

        var materials = new Dictionary<Material, Material>();
        foreach (var renderer in source.GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null || materials.ContainsKey(material) || !material.shader.name.Contains("CharacterMaster"))
                    continue;
                var path = materials.Count == 0 ? $"{materialFolder}/{name}.mat" : $"{materialFolder}/{name}_{material.name}.mat";
                Copy(AssetDatabase.GetAssetPath(material), path, report);
                materials[material] = AssetDatabase.LoadAssetAtPath<Material>(path);
            }

        BossDefinition bossDefinition = null;
        if (boss)
        {
            var sourceBoss = (BossDefinition)Serialized(source).FindProperty("_boss").objectReferenceValue;
            var bossPath = $"{folder}/{name}Boss.asset";
            if (sourceBoss != null)
                Copy(AssetDatabase.GetAssetPath(sourceBoss), bossPath, report);
            else
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<BossDefinition>(), bossPath);
                report.AppendLine($"created {bossPath}");
            }
            bossDefinition = AssetDatabase.LoadAssetAtPath<BossDefinition>(bossPath);
            var bossObject = new SerializedObject(bossDefinition);
            bossObject.FindProperty("_id").stringValue = name;
            bossObject.FindProperty("_nameKey").stringValue = "boss." + LowerFirst(name);
            bossObject.FindProperty("_rewardAbility").objectReferenceValue = null;
            bossObject.ApplyModifiedPropertiesWithoutUndo();
        }

        if (!AssetDatabase.CopyAsset(sourcePath, prefabPath))
            throw new InvalidOperationException($"copying {sourcePath} failed");
        AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
        var mappedDefinition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(definitionPath);
        var mappedBoss = boss ? AssetDatabase.LoadAssetAtPath<BossDefinition>($"{folder}/{name}Boss.asset") : null;
        var mappedMaterials = materials.ToDictionary(pair => AssetDatabase.GetAssetPath(pair.Key), pair => AssetDatabase.GetAssetPath(pair.Value));
        var prefab = PrefabEdit.Edit(prefabPath, root =>
        {
            var enemy = root.GetComponent<Enemy>();
            SetReference(enemy, "_definition", mappedDefinition);
            if (mappedBoss != null)
                SetReference(enemy, "_boss", mappedBoss);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var shared = renderer.sharedMaterials;
                for (var i = 0; i < shared.Length; i++)
                    if (shared[i] != null && mappedMaterials.TryGetValue(AssetDatabase.GetAssetPath(shared[i]), out var replacement))
                        shared[i] = AssetDatabase.LoadAssetAtPath<Material>(replacement);
                renderer.sharedMaterials = shared;
            }
        });
        report.AppendLine($"created {prefabPath} (copy of {sourcePath}, {source.GetType().Name})");

        var listPath = boss ? BossSpawnListPath : EnemySpawnListPath;
        AppendReference(AssetDatabase.LoadAssetAtPath<EnemySpawnList>(listPath), "_prefabs", prefab.GetComponent<Enemy>());
        report.AppendLine($"added to {Path.GetFileNameWithoutExtension(listPath)}");
        if (bossDefinition != null)
            report.AppendLine($"boss name key boss.{LowerFirst(name)}");
        report.AppendLine($"attacks, sounds and numbers are copied from {from}: tune {definitionPath}");
    }

    private static void CreateWeapon(string name, string from, string title, StringBuilder report)
    {
        var sourcePath = FindAsset<WeaponDefinition>(from) ?? throw new ArgumentException($"no weapon named {from}");
        var path = $"{WeaponFolder}/{name}.asset";
        if (File.Exists(path))
            throw new ArgumentException($"{path} already exists");
        Copy(sourcePath, path, report);
        var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
        var serialized = new SerializedObject(weapon);
        serialized.FindProperty("_displayName").stringValue = title;
        serialized.FindProperty("_modifiers").arraySize = 0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AddToCatalog(weapon, report);
        report.AppendLine($"name key weapon.{name}.name; moves, VFX and sounds are copied from {from}");
    }

    private static void CreateAbility(string name, string title, StringBuilder report)
    {
        var type = FindType($"TentacleVania.Abilities.{name}.{name}Definition");
        var folder = EnsureFolder($"{AbilityFolder}/{name}");
        var path = $"{folder}/{name}.asset";
        if (File.Exists(path))
            throw new ArgumentException($"{path} already exists");
        var ability = ScriptableObject.CreateInstance(type);
        AssetDatabase.CreateAsset(ability, path);
        var serialized = new SerializedObject(ability);
        serialized.FindProperty("_displayName").stringValue = title;
        if (!SetEvent(serialized, "_activateSound", $"event:/Abilities/{name}/Activate"))
            report.AppendLine($"no FMOD event event:/Abilities/{name}/Activate yet");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        report.AppendLine($"created {path}");
        AddToCatalog(ability, report);
    }

    private static void CreateMutation(string name, StringBuilder report)
    {
        var type = FindType($"TentacleVania.Abilities.Mutations.{name}Mutation");
        var path = $"{MutationFolder}/{name}.asset";
        if (File.Exists(path))
            throw new ArgumentException($"{path} already exists");
        var mutation = ScriptableObject.CreateInstance(type);
        AssetDatabase.CreateAsset(mutation, path);
        var serialized = new SerializedObject(mutation);
        serialized.FindProperty("_textKey").stringValue = "mutation." + LowerFirst(name);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        report.AppendLine($"created {path} (keys mutation.{LowerFirst(name)}.name / .effect)");
        AddToCatalog(mutation, report);
    }

    private static Type FindType(string fullName) =>
        TypeCache.GetTypesDerivedFrom<ScriptableObject>().FirstOrDefault(t => t.FullName == fullName)
        ?? throw new ArgumentException($"type {fullName} not compiled yet");

    private static void AddToCatalog(ScriptableObject asset, StringBuilder report)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
        var entries = new SerializedObject(catalog).FindProperty("_entries");
        for (var i = 0; i < entries.arraySize; i++)
            if (entries.GetArrayElementAtIndex(i).objectReferenceValue is { } other && other != asset && other.name == asset.name)
                throw new ArgumentException($"ContentCatalog already has an entry named {asset.name}");
        AppendReference(catalog, "_entries", asset);
        report.AppendLine("added to ContentCatalog");
    }

    private static string FindPrefab<T>(string name) where T : Component =>
        AssetDatabase.FindAssets($"t:Prefab {name}", new[] { "Assets/Game" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == name && AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponent<T>() != null);

    private static string FindAsset<T>(string name) where T : UnityEngine.Object =>
        AssetDatabase.FindAssets($"t:{typeof(T).Name} {name}", new[] { "Assets/Game" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == name);

    private static string EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path)!.Replace('\\', '/'), Path.GetFileName(path));
        return path;
    }

    private static void Copy(string from, string to, StringBuilder report)
    {
        if (File.Exists(to))
            throw new ArgumentException($"{to} already exists");
        if (!AssetDatabase.CopyAsset(from, to))
            throw new InvalidOperationException($"copying {from} to {to} failed");
        report.AppendLine($"created {to} (copy of {from})");
    }

    private static SerializedObject Serialized(UnityEngine.Object target) => new(target);

    private static void SetReference(UnityEngine.Object target, string property, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void AppendReference(UnityEngine.Object target, string property, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        var array = serialized.FindProperty(property);
        for (var i = 0; i < array.arraySize; i++)
            if (array.GetArrayElementAtIndex(i).objectReferenceValue == value)
                return;
        array.arraySize++;
        array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static bool SetEvent(SerializedObject serialized, string property, string path)
    {
        var editorEvent = EventManager.EventFromPath(path);
        if (editorEvent == null)
            return false;
        var field = serialized.FindProperty(property);
        field.FindPropertyRelative("Guid.Data1").intValue = editorEvent.Guid.Data1;
        field.FindPropertyRelative("Guid.Data2").intValue = editorEvent.Guid.Data2;
        field.FindPropertyRelative("Guid.Data3").intValue = editorEvent.Guid.Data3;
        field.FindPropertyRelative("Guid.Data4").intValue = editorEvent.Guid.Data4;
        var pathField = field.FindPropertyRelative("Path");
        if (pathField != null)
            pathField.stringValue = path;
        return true;
    }
}
