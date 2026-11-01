using System.Collections.Generic;
using System.Linq;
using TentacleVania.World.Layout;
using TentacleVania.World.Shortcuts;
using TentacleVania.World.Streaming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TentacleVania.World.Editor
{
    public static class AutoTunnels
    {
        public const int MinRoomsFromNest = 3;
        public const int MaxPerSector = 3;
        private const string Prefix = "AutoTunnel_";
        private const string CrawlspacePath = "Assets/Game/World/Shortcuts/Crawlspace.prefab";
        private const float Clearance = 3.5f;

        private sealed class Spot
        {
            public Vector3 Position;
            public float Yaw;
        }

        public static string Place(SectorPlan plan, RoomRegistry registry)
        {
            var report = new System.Text.StringBuilder();
            var nests = registry.Markers.Where(m => m.Kind == MapMarkerKind.Nest && !string.IsNullOrEmpty(m.Room)).Select(m => m.Room).Distinct().ToList();
            var plans = plan.Rooms.ToDictionary(r => r.Scene);
            var pairs = AutoTunnelRules.Pairs(registry.Rooms, nests, MinRoomsFromNest,
                scene => plans.TryGetValue(scene, out var room) && room.Rect.HasValue && room.Role != PlanRole.Boss, MaxPerSector);

            var scenes = new Dictionary<string, Scene>();
            var used = new Dictionary<string, List<Vector3>>();
            foreach (var room in plan.Rooms)
            {
                var scene = EditorSceneManager.OpenScene($"{plan.Folder}/{room.Scene}.unity", OpenSceneMode.Additive);
                scenes[room.Scene] = scene;
                var positions = new List<Vector3>();
                var content = Content(scene);
                if (content != null)
                {
                    for (var i = content.childCount - 1; i >= 0; i--)
                    {
                        var child = content.GetChild(i);
                        if (child.name.StartsWith(Prefix))
                            Object.DestroyImmediate(child.gameObject);
                        else
                            positions.Add(child.position);
                    }
                }
                used[room.Scene] = positions;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrawlspacePath);
            foreach (var pair in pairs)
            {
                var far = FindSpot(plan, plans[pair.Far], used[pair.Far]);
                var near = plans.TryGetValue(pair.Nest, out var nestPlan) && nestPlan.Rect.HasValue ? FindSpot(plan, nestPlan, used[pair.Nest]) : null;
                if (far == null || near == null)
                {
                    report.AppendLine($"auto tunnel {pair.Far} -> {pair.Nest}: no free wall spot");
                    continue;
                }
                used[pair.Far].Add(far.Position);
                used[pair.Nest].Add(near.Position);
                var id = $"{plan.Sector}/Auto-{pair.Far}";
                Spawn(prefab, scenes[pair.Far], id, true, far, near);
                Spawn(prefab, scenes[pair.Nest], id, false, near, far);
                report.AppendLine($"auto tunnel {id}: {pair.Far} ({pair.Distance} rooms from a nest) -> {pair.Nest}");
            }

            foreach (var scene in scenes.Values)
            {
                EditorSceneManager.SaveScene(scene);
                EditorSceneManager.CloseScene(scene, true);
            }
            return report.ToString();
        }

        private static Transform Content(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var content = root.transform.Find("Content");
                if (content != null)
                    return content;
            }
            return null;
        }

        private static Spot FindSpot(SectorPlan plan, RoomPlan room, List<Vector3> used)
        {
            var rect = room.Rect.Value;
            var center = new Vector2(rect.center.x, rect.center.y);
            Spot best = null;
            var bestScore = float.MinValue;
            for (var x = rect.xMin; x < rect.xMax; x++)
            {
                for (var y = rect.yMin; y < rect.yMax; y++)
                {
                    var cell = new Vector2Int(x, y);
                    foreach (var side in Sides.All)
                    {
                        var outside = cell + Sides.Offset(side);
                        if (rect.Contains(outside) || Occupied(plan, outside) || NearDoor(plan, cell))
                            continue;
                        var position = RoomContext.CellCenter(x, y);
                        if (used.Any(p => Flat(p - position) < Clearance))
                            continue;
                        var score = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                        if (score <= bestScore)
                            continue;
                        bestScore = score;
                        best = new Spot { Position = position, Yaw = Sides.Yaw(side) + 180f };
                    }
                }
            }
            return best;
        }

        private static bool Occupied(SectorPlan plan, Vector2Int cell)
        {
            foreach (var room in plan.Rooms)
            {
                if (room.Rect.HasValue && room.Rect.Value.Contains(cell))
                    return true;
                foreach (var hall in room.Halls)
                    for (var i = 0; i < hall.Length; i++)
                        if (OnSegment(hall[i], i + 1 < hall.Length ? hall[i + 1] : hall[i], cell))
                            return true;
            }
            return false;
        }

        private static bool OnSegment(Vector2Int a, Vector2Int b, Vector2Int cell) =>
            cell.x >= Mathf.Min(a.x, b.x) && cell.x <= Mathf.Max(a.x, b.x) && cell.y >= Mathf.Min(a.y, b.y) && cell.y <= Mathf.Max(a.y, b.y) && (a.x == b.x || a.y == b.y);

        private static bool NearDoor(SectorPlan plan, Vector2Int cell)
        {
            foreach (var room in plan.Rooms)
                foreach (var door in room.Doors)
                    if (Mathf.Abs(door.Cell.x - cell.x) <= 1 && Mathf.Abs(door.Cell.y - cell.y) <= 1)
                        return true;
            return false;
        }

        private static float Flat(Vector3 offset) => new Vector2(offset.x, offset.z).magnitude;

        private static void Spawn(GameObject prefab, Scene scene, string id, bool opensHere, Spot here, Spot other)
        {
            var content = Content(scene);
            var end = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            end.transform.SetParent(content, false);
            end.transform.SetPositionAndRotation(here.Position, Quaternion.Euler(0f, here.Yaw, 0f));
            end.name = Prefix + id.Replace('/', '_') + (opensHere ? "_Far" : "_Near");
            var crawl = end.GetComponent<Crawlspace>();
            RoomContext.Set(crawl, "_id", id);
            RoomContext.Set(crawl, "_opensHere", opensHere);
            RoomContext.Set(crawl, "_openOnNestGrown", "");
            RoomContext.Set(crawl, "_exitPosition", other.Position + Quaternion.Euler(0f, other.Yaw, 0f) * Vector3.forward * 1.2f);
            RoomContext.Set(crawl, "_exitYaw", other.Yaw);
        }
    }
}
