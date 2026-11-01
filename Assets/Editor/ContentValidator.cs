using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using FMODUnity;
using TentacleVania.Enemies;
using TentacleVania.Enemies.Data;
using TentacleVania.World.Arenas;
using TentacleVania.World.Map;
using TentacleVania.World.Streaming;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Localization;
using UnityEngine;

public static class ContentValidator
{
    public const string GameFolder = "Assets/Game";

    private static readonly (Regex Pattern, string Rule)[] BannedApis =
    {
        (new(@"\bParticleSystem\b"), "uses ParticleSystem (use VFX Graph)"),
        (new(@"\bAudioSource\b"), "uses AudioSource (use FMOD events)"),
        (new(@"\bMaterialPropertyBlock\b"), "uses MaterialPropertyBlock (use CharacterMaterials)"),
        (new(@"\bShader\.Find\s*\("), "uses Shader.Find (reference shaders through serialized fields)"),
    };

    private sealed class Report
    {
        private readonly StringBuilder _text = new();
        public int Errors { get; private set; }
        public int Warnings { get; private set; }

        public void Error(string message)
        {
            Errors++;
            _text.Append("ERROR ").AppendLine(message);
        }

        public void Warn(string message)
        {
            Warnings++;
            _text.Append("WARN ").AppendLine(message);
        }

        public override string ToString() =>
            _text + $"Content validation: {Errors} error(s), {Warnings} warning(s).";
    }

    [MenuItem("TentacleVania/Validate Content")]
    private static void RunFromMenu()
    {
        var report = Run();
        if (report.Contains("ERROR "))
            Debug.LogError(report);
        else
            Debug.Log(report);
    }

    public static string Run()
    {
        var report = new Report();
        CheckRooms(report);
        CheckWaves(report);
        CheckLocalizationKeys(report);
        CheckAssets(report);
        CheckCode(report);
        return report.ToString();
    }

    private static IEnumerable<T> Assets<T>(string filter) where T : UnityEngine.Object =>
        AssetDatabase.FindAssets(filter, new[] { GameFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>)
            .Where(asset => asset != null);

    private static void CheckRooms(Report report)
    {
        var atlas = Resources.Load<MapAtlas>("MapAtlas");
        if (atlas == null)
            report.Error("MapAtlas resource not found");
        var buildScenes = new HashSet<string>(EditorBuildSettings.scenes.Where(s => s.enabled)
            .Select(s => Path.GetFileNameWithoutExtension(s.path)));
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var navMeshes = new HashSet<string>(AssetDatabase.FindAssets("t:NavMeshData", new[] { GameFolder })
            .Select(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g))));

        foreach (var registry in Assets<RoomRegistry>("t:RoomRegistry"))
        {
            var label = AssetDatabase.GetAssetPath(registry);
            var shipped = buildScenes.Contains(registry.RootScene);
            if (shipped && atlas != null && !atlas.Sectors.Contains(registry))
                report.Error($"{label}: root scene {registry.RootScene} is in the build but the registry is missing from MapAtlas");
            if (string.IsNullOrEmpty(registry.RootScene) || ScenePath(registry.RootScene) == null)
                report.Error($"{label}: root scene '{registry.RootScene}' not found");

            foreach (var room in registry.Rooms)
            {
                var path = ScenePath(room.Scene);
                if (path == null)
                {
                    report.Error($"{label}: room scene {room.Scene} not found");
                    continue;
                }
                var entry = settings != null ? settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)) : null;
                if (entry == null)
                    report.Error($"{room.Scene}: not in Addressables");
                else if (entry.address != room.Scene)
                    report.Error($"{room.Scene}: Addressables address is '{entry.address}'");
                if (!navMeshes.Contains("NavMesh-" + room.Scene))
                    report.Error($"{room.Scene}: no NavMesh-{room.Scene} asset (rebuild the sector)");
                foreach (var neighbour in room.Neighbours)
                    if (registry.IndexOf(neighbour) < 0)
                        report.Error($"{room.Scene}: neighbour {neighbour} is not a room of {registry.name}");
            }

            foreach (var marker in registry.Markers)
                if (registry.IndexOf(marker.Room) < 0)
                    report.Error($"{label}: {marker.Kind} marker {marker.Id} is in unknown room {marker.Room}");
        }
    }

    private static string ScenePath(string scene) =>
        string.IsNullOrEmpty(scene)
            ? null
            : AssetDatabase.FindAssets($"t:Scene {scene}").Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == scene);

    private static void CheckWaves(Report report)
    {
        var spawnList = Resources.Load<EnemySpawnList>(EnemySpawnList.ResourcePath);
        var spawnable = new HashSet<Enemy>(spawnList != null && spawnList.Prefabs != null ? spawnList.Prefabs.Where(p => p != null) : Array.Empty<Enemy>());
        if (spawnList == null)
            report.Error("DebugEnemySpawnList resource not found");
        else if (spawnList.Prefabs.Any(p => p == null))
            report.Error("DebugEnemySpawnList has an empty entry");

        foreach (var set in Assets<ArenaWaveSet>("t:ArenaWaveSet"))
        {
            var label = AssetDatabase.GetAssetPath(set);
            if (set.Waves.Count == 0)
                report.Error($"{label}: no waves");
            for (var w = 0; w < set.Waves.Count; w++)
            {
                if (set.Waves[w].Enemies.Count == 0)
                    report.Error($"{label}: wave {w + 1} is empty");
                foreach (var entry in set.Waves[w].Enemies)
                    if (entry.Prefab == null)
                        report.Error($"{label}: wave {w + 1} has an entry with no prefab");
                    else if (spawnList != null && !spawnable.Contains(entry.Prefab) && !IsBoss(entry.Prefab))
                        report.Warn($"{label}: {entry.Prefab.name} is not in DebugEnemySpawnList");
            }
        }

        foreach (var go in Assets<GameObject>("t:Prefab"))
        {
            var enemy = go.GetComponent<Enemy>();
            if (enemy != null && spawnList != null && !spawnable.Contains(enemy) && !IsBoss(enemy))
                report.Warn($"{AssetDatabase.GetAssetPath(go)}: enemy prefab is not in DebugEnemySpawnList");
        }
    }

    private static bool IsBoss(Enemy enemy) =>
        enemy.GetType().Namespace?.StartsWith("TentacleVania.Bosses", StringComparison.Ordinal) == true;

    private static void CheckLocalizationKeys(Report report)
    {
        var keys = new HashSet<string>();
        foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
            foreach (var entry in collection.SharedData.Entries)
                keys.Add(entry.Key);

        foreach (var asset in Assets<ScriptableObject>("t:ScriptableObject"))
        {
            var type = asset.GetType();
            if (type.Namespace == null || !type.Namespace.StartsWith("TentacleVania", StringComparison.Ordinal))
                continue;
            var label = AssetDatabase.GetAssetPath(asset);
            if (Key(type, asset, "NameKey") is { Length: > 0 } nameKey && !keys.Contains(nameKey))
                report.Error($"{label}: string key {nameKey} not in any string table");
            if (Key(type, asset, "TextKey") is { Length: > 0 } textKey)
                foreach (var suffix in new[] { ".name", ".effect" })
                    if (!keys.Contains(textKey + suffix))
                        report.Error($"{label}: string key {textKey + suffix} not in any string table");
        }
    }

    private static string Key(Type type, object asset, string property) =>
        type.GetProperty(property, BindingFlags.Public | BindingFlags.Instance) is { PropertyType: var t } info && t == typeof(string)
            ? info.GetValue(asset) as string
            : null;

    private static void CheckAssets(Report report)
    {
        foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { GameFolder }).Select(AssetDatabase.GUIDToAssetPath))
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
                continue;
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                foreach (var component in transform.GetComponents<Component>())
                    if (component == null)
                        report.Error($"{path}: missing script on {transform.name}");
                    else
                        CheckEvents(report, $"{path} ({transform.name}/{component.GetType().Name})", component);
        }
        foreach (var path in AssetDatabase.FindAssets("t:ScriptableObject", new[] { GameFolder }).Select(AssetDatabase.GUIDToAssetPath).Distinct())
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset is ScriptableObject && asset.GetType().Namespace?.StartsWith("TentacleVania", StringComparison.Ordinal) == true)
                CheckEvents(report, path, asset);
        }
    }

    private static void CheckEvents(Report report, string label, UnityEngine.Object target)
    {
        var property = new SerializedObject(target).GetIterator();
        while (property.Next(true))
        {
            if (property.type != nameof(EventReference))
                continue;
            var guid = new FMOD.GUID
            {
                Data1 = property.FindPropertyRelative("Guid.Data1").intValue,
                Data2 = property.FindPropertyRelative("Guid.Data2").intValue,
                Data3 = property.FindPropertyRelative("Guid.Data3").intValue,
                Data4 = property.FindPropertyRelative("Guid.Data4").intValue,
            };
            if (guid.IsNull)
                continue;
            if (EventManager.EventFromGUID(guid) == null)
                report.Error($"{label}: {property.propertyPath} points at an FMOD event that no longer exists ({property.FindPropertyRelative("Path").stringValue})");
        }
    }

    private static void CheckCode(Report report)
    {
        foreach (var script in AssetDatabase.FindAssets("t:MonoScript", new[] { GameFolder }).Select(AssetDatabase.GUIDToAssetPath))
        {
            if (!script.EndsWith(".cs", StringComparison.Ordinal) || script.Contains("/Editor/") || script.Contains("/Tests/"))
                continue;
            var text = File.ReadAllText(script);
            foreach (var (pattern, rule) in BannedApis)
                if (pattern.IsMatch(text))
                    report.Error($"{script}: {rule}");
        }

        var scripted = new HashSet<Type>(AssetDatabase.FindAssets("t:MonoScript", new[] { GameFolder })
            .Select(g => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(s => s != null).Select(s => s.GetClass()).Where(t => t != null));
        foreach (var type in TypeCache.GetTypesDerivedFrom<MonoBehaviour>().Concat(TypeCache.GetTypesDerivedFrom<ScriptableObject>()))
        {
            if (type.IsAbstract || type.IsGenericTypeDefinition || type.IsNested || typeof(Editor).IsAssignableFrom(type) || type.Namespace == null
                || !type.Namespace.StartsWith("TentacleVania", StringComparison.Ordinal) || scripted.Contains(type))
                continue;
            report.Error($"{type.FullName}: no script file named {type.Name}.cs holds it (one MonoBehaviour or ScriptableObject per file)");
        }
    }
}
