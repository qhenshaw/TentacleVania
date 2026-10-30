using System.Collections.Generic;
using TentacleVania.World.Layout;
using Unity.AI.Navigation;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TentacleVania.World.Editor
{
    public static class LayoutGenerator
    {
        private const int FloorLayer = -1;
        private const int DoorLayer = -2;
        private const StaticEditorFlags StaticFlags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic;

        public static void Generate(LevelLayout layout, bool bakeNavMesh, bool undo = true)
        {
            var tileset = layout.Tileset;
            var spaces = new List<LayoutSpace>();
            var openings = new List<LayoutOpening>();
            layout.GetSpaces(spaces);
            layout.GetOpenings(openings);
            var grid = layout.BuildGrid(spaces, openings);

            var old = layout.transform.Find(LevelLayout.GeneratedName);
            if (old != null)
            {
                if (undo)
                    Undo.DestroyObjectImmediate(old.gameObject);
                else
                    Object.DestroyImmediate(old.gameObject);
            }
            var root = new GameObject(LevelLayout.GeneratedName).transform;
            root.SetParent(layout.transform, false);
            root.gameObject.hideFlags = layout.gameObject.hideFlags;
            if (undo)
                Undo.RegisterCreatedObjectUndo(root.gameObject, "Generate layout");

            if (tileset != null)
            {
                var context = new Context(layout, tileset, grid, spaces, root);
                context.Floors();
                for (var layer = 0; layer < tileset.WallLayers.Count; layer++)
                    context.Walls(layer);
                context.Doors(openings);
            }
            if (layout.Dressing != null)
                LayoutDressing.Place(layout, grid, spaces, openings, Child(root, "Props"));
            foreach (Transform group in root)
                if (group.name != "Props")
                    SceneVisibilityManager.instance.DisablePicking(group.gameObject, true);

            if (!undo)
                return;
            EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);
            if (bakeNavMesh)
                BakeNavMesh(layout);
        }

        public static void BakeNavMesh(LevelLayout layout)
        {
            var surfaces = new List<Object>();
            foreach (var root in layout.gameObject.scene.GetRootGameObjects())
                foreach (var surface in root.GetComponentsInChildren<NavMeshSurface>())
                    surfaces.Add(surface);
            if (surfaces.Count > 0)
                NavMeshAssetManager.instance.StartBakingSurfaces(surfaces.ToArray());
        }

        public static Transform Child(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
                return child;
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.gameObject.hideFlags = parent.gameObject.hideFlags;
            return child;
        }

        public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene) as GameObject;
            if (instance == null)
                instance = Object.Instantiate(prefab);
            instance.transform.SetParent(parent, false);
            var root = prefab.transform;
            instance.transform.SetPositionAndRotation(position + rotation * root.localPosition, rotation * root.localRotation);
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, scale);
            instance.hideFlags = parent.gameObject.hideFlags;
            return instance;
        }

        private sealed class Context
        {
            private readonly LevelLayout _layout;
            private readonly Tileset _tileset;
            private readonly LayoutGrid _grid;
            private readonly List<LayoutSpace> _spaces;
            private readonly Transform _root;
            private readonly HashSet<Side> _used = new();

            public Context(LevelLayout layout, Tileset tileset, LayoutGrid grid, List<LayoutSpace> spaces, Transform root)
            {
                _layout = layout;
                _tileset = tileset;
                _grid = grid;
                _spaces = spaces;
                _root = root;
            }

            public void Floors()
            {
                if (_tileset.Floor.IsEmpty)
                    return;
                var parent = Child(_root, "Floors");
                foreach (var cell in _grid.Cells)
                {
                    var space = _spaces[_grid.OwnerOf(cell)];
                    var hash = LayoutRandom.Hash(space.Seed, cell.x, cell.y, FloorLayer);
                    var index = FloorPin(space, cell, out var pinned) ? pinned : LayoutRandom.Pick(_tileset.Floor, hash);
                    if (index < 0 || index >= _tileset.Floor.Entries.Count)
                        continue;
                    var entry = _tileset.Floor.Entries[index];
                    var turns = entry.RandomQuarterTurns ? (int)(LayoutRandom.Hash(space.Seed, cell.x, cell.y, FloorLayer, 1) % 4) : 0;
                    Place(entry, parent, _layout.CellCenter(cell), turns * 90f, true);
                }
            }

            public void Walls(int layerIndex)
            {
                var layer = _tileset.WallLayers[layerIndex];
                var parent = Child(_root, string.IsNullOrEmpty(layer.Name) ? $"Layer {layerIndex}" : layer.Name);
                foreach (var cell in _grid.Cells)
                {
                    var space = _spaces[_grid.OwnerOf(cell)];
                    _used.Clear();
                    var center = _layout.CellCenter(cell);
                    foreach (var first in Sides.All)
                    {
                        if (!_grid.IsConvexCorner(cell, first))
                            continue;
                        if (!layer.ConvexPost.IsEmpty)
                            PlaceFrom(layer.ConvexPost, space, cell, first, layerIndex, 10, parent, center, layer.Static);
                        var second = Sides.Clockwise(first);
                        if (layer.Corner.IsEmpty || _used.Contains(first) || _used.Contains(second))
                            continue;
                        if (PlaceFrom(layer.Corner, space, cell, first, layerIndex, 20, parent, center, layer.Static))
                        {
                            _used.Add(first);
                            _used.Add(second);
                        }
                    }
                    foreach (var side in Sides.All)
                    {
                        if (!layer.ConcavePost.IsEmpty && _grid.IsConcaveCorner(cell, side))
                            PlaceFrom(layer.ConcavePost, space, cell, side, layerIndex, 30, parent, center, layer.Static);
                        if (_used.Contains(side) || !_grid.IsWall(cell, side) || layer.Straight.IsEmpty)
                            continue;
                        var index = WallPin(space, cell, side, layerIndex, out var pinned)
                            ? pinned
                            : LayoutRandom.Pick(layer.Straight, LayoutRandom.Hash(space.Seed, cell.x, cell.y, layerIndex, (int)side));
                        if (index >= 0 && index < layer.Straight.Entries.Count)
                            Place(layer.Straight.Entries[index], parent, center, Sides.Yaw(side), layer.Static);
                    }
                }
            }

            public void Doors(List<LayoutOpening> openings)
            {
                Transform parent = null;
                for (var i = 0; i < openings.Count; i++)
                {
                    var opening = openings[i];
                    if (opening.Kind == OpeningKind.Doorway)
                        continue;
                    var spec = opening.Spec;
                    var center = Vector3.zero;
                    for (var c = 0; c < spec.Width; c++)
                        center += _layout.CellCenter(spec.CellAt(c));
                    center /= spec.Width;
                    if (parent == null)
                        parent = Child(_root, "Doors");
                    var yaw = Sides.Yaw(spec.Side);
                    if (opening.Kind == OpeningKind.Prefab)
                    {
                        if (opening.Prefab == null)
                            continue;
                        var rotation = _layout.transform.rotation * Quaternion.Euler(0f, yaw, 0f);
                        var edge = center + rotation * Vector3.forward * (_layout.CellSize * 0.5f);
                        Spawn(opening.Prefab, parent, edge, rotation, Vector3.one);
                        continue;
                    }
                    var index = LayoutRandom.Pick(_tileset.Door, LayoutRandom.Hash(i, spec.Cell.x, spec.Cell.y, DoorLayer));
                    if (index >= 0)
                        Place(_tileset.Door.Entries[index], parent, center, yaw, false);
                }
            }

            private bool PlaceFrom(TileSlot slot, LayoutSpace space, Vector2Int cell, Side side, int layer, int salt, Transform parent, Vector3 center, bool isStatic)
            {
                var index = LayoutRandom.Pick(slot, LayoutRandom.Hash(space.Seed, cell.x, cell.y, layer * 100 + salt, (int)side));
                if (index < 0)
                    return false;
                Place(slot.Entries[index], parent, center, Sides.Yaw(side), isStatic);
                return true;
            }

            private void Place(TileEntry entry, Transform parent, Vector3 center, float yaw, bool isStatic)
            {
                var rotation = _layout.transform.rotation * Quaternion.Euler(0f, yaw, 0f);
                var instance = Spawn(entry.Prefab, parent, center + rotation * entry.Offset, rotation * Quaternion.Euler(0f, entry.Rotation, 0f), entry.Scale);
                if (isStatic)
                    foreach (var child in instance.GetComponentsInChildren<Transform>(true))
                        GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticFlags);
            }

            private static bool FloorPin(LayoutSpace space, Vector2Int cell, out int variant)
            {
                foreach (var pin in space.FloorPins)
                    if (pin.Cell == cell)
                    {
                        variant = pin.Variant;
                        return true;
                    }
                variant = -1;
                return false;
            }

            private static bool WallPin(LayoutSpace space, Vector2Int cell, Side side, int layer, out int variant)
            {
                foreach (var pin in space.WallPins)
                    if (pin.Cell == cell && pin.Side == side && pin.Layer == layer)
                    {
                        variant = pin.Variant;
                        return true;
                    }
                variant = -1;
                return false;
            }
        }
    }
}
