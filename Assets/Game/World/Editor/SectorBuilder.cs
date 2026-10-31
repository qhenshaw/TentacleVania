using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TentacleVania.Enemies;
using TentacleVania.World.Arenas;
using TentacleVania.World.Layout;
using TentacleVania.World.Map;
using TentacleVania.World.Streaming;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TentacleVania.World.Editor
{
    public sealed class RoomContext
    {
        public RoomContext(Scene scene, Transform root, Transform content, RoomPlan plan)
        {
            Scene = scene;
            Root = root;
            Content = content;
            Plan = plan;
        }

        public Scene Scene { get; }
        public Transform Root { get; }
        public Transform Content { get; }
        public RoomPlan Plan { get; }

        public static Vector3 Cell(float x, float y) => new(x * SectorBuilder.CellSize, 0f, y * SectorBuilder.CellSize);

        public static Vector3 CellCenter(float x, float y) => Cell(x + 0.5f, y + 0.5f);

        public GameObject Spawn(string prefabPath, Vector3 position, float yaw, string name = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Missing prefab " + prefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, Scene);
            instance.transform.SetParent(Content, false);
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            if (name != null)
                instance.name = name;
            return instance;
        }

        public GameObject Empty(string name, Vector3 position, float yaw)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Content, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            return go;
        }

        public Component AddByName(GameObject target, string typeName)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(typeName)).FirstOrDefault(t => t != null);
            if (type == null)
                throw new InvalidOperationException("Missing type " + typeName);
            return target.AddComponent(type);
        }

        public static void Set(UnityEngine.Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field) ?? throw new InvalidOperationException($"{target.GetType().Name} has no {field}");
            switch (value)
            {
                case string text:
                    property.stringValue = text;
                    break;
                case bool flag:
                    property.boolValue = flag;
                    break;
                case int number:
                    property.intValue = number;
                    break;
                case float number:
                    property.floatValue = number;
                    break;
                case Vector2 vector:
                    property.vector2Value = vector;
                    break;
                case Vector3 vector:
                    property.vector3Value = vector;
                    break;
                case UnityEngine.Object reference:
                    property.objectReferenceValue = reference;
                    break;
                case null:
                    property.objectReferenceValue = null;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported value for " + field);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    public static class SectorBuilder
    {
        public const float CellSize = 3f;
        private const string TilesetPath = "Assets/Game/World/Data/Layout/SciFiTopDown_Tileset.asset";
        private const string DressingPath = "Assets/Game/World/Data/Layout/SciFiTopDown_Dressing.asset";
        public const string ArenaDoorPath = "Assets/Game/World/Arenas/ArenaDoorCell.prefab";
        public const string BeaconPath = "Assets/Game/World/Arenas/ArenaBeacon.prefab";
        private const string TemplateRoot = "Assets/Game/Scenes/StreamingTest/StreamingTest.unity";
        private const string CameraBoundsType = "TentacleVania.Camera.CameraBounds";

        private static readonly Dictionary<string, string> EnemyPaths = new()
        {
            { "Scientist", "Assets/Game/Enemies/Prefabs/Scientist.prefab" },
            { "SecurityGuard", "Assets/Game/Enemies/Prefabs/SecurityGuard.prefab" },
            { "Turret", "Assets/Game/Enemies/Prefabs/Turret.prefab" },
            { "Handler", "Assets/Game/Enemies/Prefabs/Handler.prefab" },
            { "Surgeon", "Assets/Game/Enemies/Prefabs/Surgeon.prefab" },
            { "LabSecurity", "Assets/Game/Enemies/Prefabs/LabSecurity.prefab" },
            { "MedDrone", "Assets/Game/Enemies/Prefabs/MedDrone.prefab" },
            { "Hazmat", "Assets/Game/Enemies/Prefabs/Hazmat.prefab" },
            { "WardenDrone", "Assets/Game/Bosses/Warden/WardenDrone.prefab" },
            { "SurgicalAutomaton", "Assets/Game/Bosses/Automaton/SurgicalAutomaton.prefab" },
        };

        public static string Build(SectorPlan plan, string onlyScene = null)
        {
            var report = new System.Text.StringBuilder();
            EnsureFolder(plan.Folder);
            var dataFolder = "Assets/Game/World/Data/Sectors/" + plan.Sector;
            EnsureFolder(dataFolder);
            foreach (var room in plan.Rooms)
            {
                if (onlyScene != null && room.Scene != onlyScene)
                    continue;
                report.AppendLine(BuildRoom(plan, room, dataFolder));
            }
            var registry = BuildRegistry(plan);
            report.Append(RoomRegistryTools.Update(registry, plan.AddressablesGroup, true));
            BuildRoot(plan, registry);
            AddToAtlas(registry);
            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        private static string BuildRoom(SectorPlan plan, RoomPlan room, string dataFolder)
        {
            var path = $"{plan.Folder}/{room.Scene}.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var root = new GameObject(room.Scene);
            SceneManager.MoveGameObjectToScene(root, scene);
            var roomComponent = root.AddComponent<Room>();
            RoomContext.Set(roomComponent, "_sector", plan.Sector);

            var layoutObject = new GameObject("Layout");
            layoutObject.transform.SetParent(root.transform, false);
            var layout = layoutObject.AddComponent<LevelLayout>();
            layout.Tileset = AssetDatabase.LoadAssetAtPath<Tileset>(TilesetPath);
            layout.Dressing = AssetDatabase.LoadAssetAtPath<DressingSet>(DressingPath);
            var surface = layoutObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;

            var bounds = new List<RectInt>();
            if (room.Rect.HasValue)
            {
                var shape = new GameObject("Room");
                shape.transform.SetParent(layoutObject.transform, false);
                var layoutRoom = shape.AddComponent<LayoutRoom>();
                layoutRoom.Rects.Clear();
                layoutRoom.Rects.Add(room.Rect.Value);
                bounds.Add(room.Rect.Value);
            }
            var hallIndex = 0;
            foreach (var points in room.Halls)
            {
                var shape = new GameObject("Hall" + hallIndex++);
                shape.transform.SetParent(layoutObject.transform, false);
                var hallway = shape.AddComponent<LayoutHallway>();
                hallway.Points.Clear();
                hallway.Points.AddRange(points);
                hallway.Width = 1;
                foreach (var point in points)
                    bounds.Add(new RectInt(point.x, point.y, 1, 1));
            }
            var doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArenaDoorPath);
            var doorIndex = 0;
            foreach (var door in room.Doors)
            {
                var shape = new GameObject("Opening" + doorIndex++);
                shape.transform.SetParent(layoutObject.transform, false);
                var opening = shape.AddComponent<LayoutOpening>();
                opening.Cell = door.Cell;
                opening.Side = door.Side;
                RoomContext.Set(opening, "_kind", door.ArenaDoor ? (int)OpeningKind.Prefab : (int)OpeningKind.Doorway);
                if (door.ArenaDoor)
                    RoomContext.Set(opening, "_prefab", doorPrefab);
            }
            LayoutGenerator.Generate(layout, false, false);

            var min = new Vector2(bounds.Min(r => r.xMin), bounds.Min(r => r.yMin)) * CellSize;
            var max = new Vector2(bounds.Max(r => r.xMax), bounds.Max(r => r.yMax)) * CellSize;
            var cameraBounds = new GameObject("CameraBounds");
            cameraBounds.transform.SetParent(root.transform, false);
            cameraBounds.transform.position = new Vector3((min.x + max.x) * 0.5f, 0f, (min.y + max.y) * 0.5f);
            var boundsComponent = new RoomContext(scene, root.transform, root.transform, room).AddByName(cameraBounds, CameraBoundsType);
            RoomContext.Set(boundsComponent, "_size", max - min + Vector2.one * 6f);
            RoomContext.Set(roomComponent, "_cameraBounds", boundsComponent);

            var content = new GameObject("Content");
            content.transform.SetParent(root.transform, false);
            var context = new RoomContext(scene, root.transform, content.transform, room);
            if (room.Role != PlanRole.Plain && room.Rect.HasValue)
                BuildArena(plan, room, root, dataFolder);
            room.Place?.Invoke(context);

            surface.BuildNavMesh();
            var navMeshPath = $"{plan.Folder}/NavMesh-{room.Scene}.asset";
            AssetDatabase.DeleteAsset(navMeshPath);
            AssetDatabase.CreateAsset(surface.navMeshData, navMeshPath);
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.CloseScene(scene, true);
            return $"{room.Scene}: built at {path}";
        }

        private static void BuildArena(SectorPlan plan, RoomPlan room, GameObject root, string dataFolder)
        {
            var rect = room.Rect.Value;
            var x0 = rect.xMin * CellSize;
            var x1 = rect.xMax * CellSize;
            var z0 = rect.yMin * CellSize;
            var z1 = rect.yMax * CellSize;
            var center = new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f);
            const float inset = 1.5f;
            var spawnRoot = new GameObject("SpawnPoints").transform;
            spawnRoot.SetParent(root.transform, false);
            var spots = new[]
            {
                new Vector3(Mathf.Lerp(x0, x1, 0.25f), 0f, z1 - inset),
                new Vector3(Mathf.Lerp(x0, x1, 0.75f), 0f, z1 - inset),
                new Vector3(x1 - inset, 0f, Mathf.Lerp(z0, z1, 0.7f)),
                new Vector3(x1 - inset, 0f, Mathf.Lerp(z0, z1, 0.3f)),
                new Vector3(x0 + inset, 0f, Mathf.Lerp(z0, z1, 0.7f)),
                new Vector3(x0 + inset, 0f, Mathf.Lerp(z0, z1, 0.3f)),
                new Vector3(Mathf.Lerp(x0, x1, 0.25f), 0f, z0 + inset),
                new Vector3(Mathf.Lerp(x0, x1, 0.75f), 0f, z0 + inset),
            };
            var points = new List<Transform>();
            for (var i = 0; i < spots.Length; i++)
            {
                var point = new GameObject("Spawn " + (i + 1)).transform;
                point.SetParent(spawnRoot, false);
                point.position = spots[i];
                point.rotation = Quaternion.LookRotation(center - spots[i]);
                points.Add(point);
            }
            var mounts = new List<Transform>();
            if (room.WallMounts)
            {
                var mountRoot = new GameObject("WallMounts").transform;
                mountRoot.SetParent(root.transform, false);
                foreach (var (spot, yaw) in new[] { (new Vector3(x0 + 1.4f, 0f, Mathf.Lerp(z0, z1, 0.8f)), 90f), (new Vector3(x1 - 1.4f, 0f, Mathf.Lerp(z0, z1, 0.8f)), 270f) })
                {
                    var mount = new GameObject("Mount").transform;
                    mount.SetParent(mountRoot, false);
                    mount.SetPositionAndRotation(spot, Quaternion.Euler(0f, yaw, 0f));
                    mounts.Add(mount);
                }
            }

            var wavesPath = $"{dataFolder}/{room.Scene}_Waves.asset";
            var waves = AssetDatabase.LoadAssetAtPath<ArenaWaveSet>(wavesPath);
            if (waves == null)
            {
                waves = ScriptableObject.CreateInstance<ArenaWaveSet>();
                AssetDatabase.CreateAsset(waves, wavesPath);
            }
            var waveSo = new SerializedObject(waves);
            var list = waveSo.FindProperty("_waves");
            list.arraySize = room.Waves.Count;
            for (var w = 0; w < room.Waves.Count; w++)
            {
                var enemies = list.GetArrayElementAtIndex(w).FindPropertyRelative("Enemies");
                enemies.arraySize = room.Waves[w].Length;
                for (var e = 0; e < room.Waves[w].Length; e++)
                {
                    var entry = enemies.GetArrayElementAtIndex(e);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPaths[room.Waves[w][e].Prefab]);
                    entry.FindPropertyRelative("Prefab").objectReferenceValue = prefab.GetComponent<Enemy>();
                    entry.FindPropertyRelative("Count").intValue = room.Waves[w][e].Count;
                }
            }
            waveSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(waves);
            AssetDatabase.SaveAssetIfDirty(waves);

            var encounter = root.AddComponent<ArenaEncounter>();
            var so = new SerializedObject(encounter);
            so.FindProperty("_waves").objectReferenceValue = waves;
            so.FindProperty("_doors").arraySize = 0;
            var spawnProperty = so.FindProperty("_spawnPoints");
            spawnProperty.arraySize = points.Count;
            for (var i = 0; i < points.Count; i++)
                spawnProperty.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            var mountProperty = so.FindProperty("_wallMounts");
            mountProperty.arraySize = mounts.Count;
            for (var i = 0; i < mounts.Count; i++)
                mountProperty.GetArrayElementAtIndex(i).objectReferenceValue = mounts[i];
            so.FindProperty("_triggerCenter").vector3Value = center + Vector3.up;
            so.FindProperty("_triggerSize").vector3Value = new Vector3(x1 - x0 - 3f, 4f, z1 - z0 - 3f);
            so.FindProperty("_bossArena").boolValue = room.Role == PlanRole.Boss;
            so.FindProperty("_oneTime").boolValue = room.OneTime;
            so.FindProperty("_id").stringValue = plan.Sector + "/" + room.Scene;
            so.ApplyModifiedPropertiesWithoutUndo();
            AddLockdown(room, root);
        }

        private static void AddLockdown(RoomPlan room, GameObject root)
        {
            var lockdown = root.AddComponent<ArenaLockdown>();
            SetEvent(lockdown, "_klaxon", "event:/World/Arena/Klaxon");
            SetEvent(lockdown, "_allClear", "event:/World/Arena/AllClear");
            var beaconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BeaconPath);
            if (beaconPrefab == null)
                return;
            var parent = new GameObject("Beacons").transform;
            parent.SetParent(root.transform, false);
            foreach (var door in room.Doors)
            {
                if (!door.ArenaDoor)
                    continue;
                var inward = -Offset(door.Side);
                var lateral = new Vector3(inward.z, 0f, -inward.x);
                var position = RoomContext.CellCenter(door.Cell.x, door.Cell.y) - inward * 1.2f + lateral * 1.7f + Vector3.up * 2.4f;
                var beacon = (GameObject)PrefabUtility.InstantiatePrefab(beaconPrefab, root.scene);
                beacon.transform.SetParent(parent, false);
                beacon.transform.SetPositionAndRotation(position, Quaternion.LookRotation(inward));
            }
        }

        private static Vector3 Offset(Side side) => side switch
        {
            Side.North => Vector3.forward,
            Side.South => Vector3.back,
            Side.East => Vector3.right,
            _ => Vector3.left,
        };

        private static void SetEvent(UnityEngine.Object target, string field, string path)
        {
            var reference = FMODUnity.EventManager.EventFromPath(path);
            if (reference == null)
                return;
            var so = new SerializedObject(target);
            so.FindProperty(field + ".Guid.Data1").intValue = reference.Guid.Data1;
            so.FindProperty(field + ".Guid.Data2").intValue = reference.Guid.Data2;
            so.FindProperty(field + ".Guid.Data3").intValue = reference.Guid.Data3;
            so.FindProperty(field + ".Guid.Data4").intValue = reference.Guid.Data4;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RoomRegistry BuildRegistry(SectorPlan plan)
        {
            var path = $"{plan.Folder}/{plan.RootScene}Rooms.asset";
            var registry = AssetDatabase.LoadAssetAtPath<RoomRegistry>(path);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<RoomRegistry>();
                AssetDatabase.CreateAsset(registry, path);
            }
            registry.Rooms.RemoveAll(r => plan.Rooms.All(p => p.Scene != r.Scene));
            foreach (var room in plan.Rooms)
                if (registry.IndexOf(room.Scene) < 0)
                    registry.Rooms.Add(new RoomEntry { Scene = room.Scene });
            RoomContext.Set(registry, "_sector", plan.Sector);
            RoomContext.Set(registry, "_rootScene", plan.RootScene);
            var so = new SerializedObject(registry);
            so.FindProperty("_color").colorValue = plan.Color;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
            return registry;
        }

        private static void BuildRoot(SectorPlan plan, RoomRegistry registry)
        {
            var path = $"{plan.Folder}/{plan.RootScene}.unity";
            if (!File.Exists(path))
                AssetDatabase.CopyAsset(TemplateRoot, path);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            foreach (var root in scene.GetRootGameObjects())
            {
                var streamer = root.GetComponent<RoomStreamer>();
                if (streamer != null)
                    RoomContext.Set(streamer, "_registry", registry);
                if (root.name == "CreatureSpawn")
                    root.transform.SetPositionAndRotation(plan.Spawn, Quaternion.Euler(0f, plan.SpawnYaw, 0f));
            }
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);
        }

        private static void AddToAtlas(RoomRegistry registry)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<MapAtlas>("Assets/Game/World/Map/Resources/MapAtlas.asset");
            if (atlas == null || atlas.Sectors.Contains(registry))
                return;
            var so = new SerializedObject(atlas);
            var sectors = so.FindProperty("_sectors");
            sectors.arraySize++;
            sectors.GetArrayElementAtIndex(sectors.arraySize - 1).objectReferenceValue = registry;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string folder)
        {
            var parts = folder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
