using System.Collections.Generic;
using TentacleVania.World.Layout;
using UnityEditor;
using UnityEngine;

namespace TentacleVania.World.Editor
{
    public static class LayoutEditing
    {
        private static readonly HashSet<LevelLayout> Pending = new();

        public static LevelLayout LayoutOf(Component component) =>
            component == null ? null : component.GetComponentInParent<LevelLayout>();

        public static LevelLayout Selected()
        {
            var active = Selection.activeGameObject;
            return active == null ? null : active.GetComponentInParent<LevelLayout>();
        }

        public static void RequestRebuild(LevelLayout layout)
        {
            if (layout == null || !layout.AutoRegenerate)
                return;
            if (Pending.Count == 0)
                EditorApplication.delayCall += Flush;
            Pending.Add(layout);
        }

        public static void Rebuild(LevelLayout layout, bool bake)
        {
            if (layout == null)
                return;
            Pending.Remove(layout);
            LayoutGenerator.Generate(layout, bake && layout.BakeNavMesh);
        }

        private static void Flush()
        {
            var layouts = new List<LevelLayout>(Pending);
            Pending.Clear();
            foreach (var layout in layouts)
                if (layout != null)
                    LayoutGenerator.Generate(layout, false);
        }

        public static T AddShape<T>(LevelLayout layout, string name) where T : Component
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Add {name}");
            go.transform.SetParent(layout.transform, false);
            go.name = GameObjectUtility.GetUniqueNameForSibling(layout.transform, name);
            var component = go.AddComponent<T>();
            if (component is LayoutSpace space)
                space.Seed = Random.Range(1, 100000);
            var view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                var cell = layout.WorldToCell(view.pivot);
                switch (component)
                {
                    case LayoutRoom room:
                        room.Rects[0] = new RectInt(cell.x - 2, cell.y - 2, 4, 4);
                        break;
                    case LayoutHallway hallway:
                        hallway.Points[0] = cell;
                        hallway.Points[1] = cell + new Vector2Int(4, 0);
                        break;
                    case LayoutOpening opening:
                        opening.Cell = cell;
                        break;
                }
            }
            Selection.activeGameObject = go;
            RequestRebuild(layout);
            return component;
        }

        public static void Reroll(LevelLayout layout, IEnumerable<LayoutSpace> spaces)
        {
            foreach (var space in spaces)
            {
                Undo.RecordObject(space, "Reroll layout");
                space.Seed = Random.Range(1, 100000);
            }
            Rebuild(layout, false);
        }

        public static int LockSelectedProps(LevelLayout layout)
        {
            var locked = 0;
            foreach (var go in Selection.gameObjects)
            {
                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                if (root == null || !IsGeneratedProp(layout, root.transform, out var space))
                    continue;
                var prefab = PrefabUtility.GetCorrespondingObjectFromSource(root);
                if (prefab == null)
                    continue;
                Undo.RecordObject(space, "Lock prop");
                space.PinnedProps.Add(new PinnedProp
                {
                    Prefab = prefab,
                    Position = layout.transform.InverseTransformPoint(root.transform.position),
                    Rotation = Quaternion.Inverse(layout.transform.rotation) * root.transform.rotation,
                });
                locked++;
            }
            return locked;
        }

        public static int UnlockSelectedProps(LevelLayout layout)
        {
            var unlocked = 0;
            foreach (var go in Selection.gameObjects)
            {
                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                if (root == null || !IsGeneratedProp(layout, root.transform, out var space))
                    continue;
                var local = layout.transform.InverseTransformPoint(root.transform.position);
                Undo.RecordObject(space, "Unlock prop");
                unlocked += space.PinnedProps.RemoveAll(pin => (pin.Position - local).sqrMagnitude < 0.01f);
            }
            return unlocked;
        }

        private static bool IsGeneratedProp(LevelLayout layout, Transform prop, out LayoutSpace space)
        {
            space = null;
            var group = prop.parent;
            if (group == null || group.parent == null || group.parent.name != "Props")
                return false;
            var generated = group.parent.parent;
            if (generated == null || generated.parent != layout.transform)
                return false;
            var spaces = new List<LayoutSpace>();
            layout.GetSpaces(spaces);
            foreach (var candidate in spaces)
                if (candidate.name == group.name)
                {
                    space = candidate;
                    return true;
                }
            return false;
        }

        public static Color SpaceColor(bool hallway) => hallway ? new Color(0.3f, 1f, 0.5f, 1f) : new Color(0.3f, 0.65f, 1f, 1f);
    }
}
