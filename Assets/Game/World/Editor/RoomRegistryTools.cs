using System.Collections.Generic;
using System.Linq;
using TentacleVania.World.BioNests;
using TentacleVania.World.Hoarder;
using TentacleVania.World.Map;
using TentacleVania.World.Security;
using TentacleVania.World.Streaming;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TentacleVania.World.Editor
{
    public static class RoomRegistryTools
    {
        private const float TouchDistance = 1f;

        [MenuItem("TentacleVania/World/Update Selected Room Registry")]
        private static void UpdateSelected()
        {
            if (Selection.activeObject is RoomRegistry registry)
                Debug.Log(Update(registry, "Rooms", true));
            else
                Debug.LogWarning("Select a Room Registry asset first.");
        }

        public static string Update(RoomRegistry registry, string group, bool includeInBuild)
        {
            var rooms = registry.Rooms;
            var report = new System.Text.StringBuilder();
            registry.Markers.Clear();
            foreach (var room in rooms)
            {
                var path = ScenePath(room.Scene);
                if (path == null)
                {
                    report.AppendLine($"{room.Scene}: scene not found");
                    continue;
                }
                var scene = SceneManager.GetSceneByPath(path);
                var opened = !scene.isLoaded;
                if (opened)
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                if (TryMeasure(scene, out var bounds))
                {
                    room.Center = bounds.center;
                    room.Size = bounds.size;
                }
                CollectMarkers(scene, room.Scene, registry.Markers);
                if (opened)
                    EditorSceneManager.CloseScene(scene, true);
                Register(path, room.Scene, group, includeInBuild);
                report.AppendLine($"{room.Scene}: center {room.Center}, size {room.Size}");
            }
            foreach (var room in rooms)
            {
                room.Neighbours.Clear();
                foreach (var other in rooms)
                    if (other != room && Touches(room, other))
                        room.Neighbours.Add(other.Scene);
                report.AppendLine($"{room.Scene} neighbours: {string.Join(", ", room.Neighbours)}");
            }
            report.AppendLine($"markers: {registry.Markers.Count}");
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        private static void CollectMarkers(Scene scene, string room, List<MapMarker> markers)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var nest in root.GetComponentsInChildren<BioNest>(true))
                {
                    var respawn = nest.RespawnPoint;
                    markers.Add(new MapMarker { Kind = MapMarkerKind.Nest, Id = nest.SaveId, Room = room, Position = respawn.position, Yaw = respawn.eulerAngles.y });
                }
                foreach (var relay in root.GetComponentsInChildren<SecurityRelay>(true))
                    markers.Add(new MapMarker { Kind = MapMarkerKind.Relay, Id = relay.System + "/" + relay.Id, Room = room, Position = relay.transform.position });
                foreach (var door in root.GetComponentsInChildren<SecurityDoor>(true))
                    markers.Add(new MapMarker { Kind = MapMarkerKind.SecurityDoor, Id = door.System, Room = room, Position = door.transform.position });
                foreach (var gate in root.GetComponentsInChildren<MapGate>(true))
                    markers.Add(new MapMarker { Kind = MapMarkerKind.Gate, Id = gate.Id, Room = room, Position = gate.transform.position });
                foreach (var hoarder in root.GetComponentsInChildren<HoarderMerchant>(true))
                    markers.Add(new MapMarker { Kind = MapMarkerKind.Hoarder, Id = "Hoarder", Room = room, Position = hoarder.transform.position });
            }
        }

        private static bool TryMeasure(Scene scene, out Bounds bounds)
        {
            bounds = default;
            var any = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                {
                    if (renderer is ParticleSystemRenderer || renderer is LineRenderer)
                        continue;
                    if (!any)
                        bounds = renderer.bounds;
                    else
                        bounds.Encapsulate(renderer.bounds);
                    any = true;
                }
            }
            return any;
        }

        private static bool Touches(RoomEntry a, RoomEntry b)
        {
            var gapX = Mathf.Abs(a.Center.x - b.Center.x) - (a.Size.x + b.Size.x) * 0.5f;
            var gapZ = Mathf.Abs(a.Center.z - b.Center.z) - (a.Size.z + b.Size.z) * 0.5f;
            return gapX <= TouchDistance && gapZ <= TouchDistance;
        }

        private static string ScenePath(string scene) =>
            AssetDatabase.FindAssets($"t:Scene {scene}").Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == scene);

        private static void Register(string path, string address, string groupName, bool includeInBuild)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = settings.FindGroup(groupName);
            if (group == null)
                group = settings.CreateGroup(groupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema != null && schema.IncludeInBuild != includeInBuild)
            {
                schema.IncludeInBuild = includeInBuild;
                EditorUtility.SetDirty(schema);
            }
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group, false, false);
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToArray();
            if (scenes.Length != EditorBuildSettings.scenes.Length)
                EditorBuildSettings.scenes = scenes;
        }
    }
}
