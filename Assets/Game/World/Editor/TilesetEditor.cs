using TentacleVania.World.Layout;
using UnityEditor;
using UnityEngine;

namespace TentacleVania.World.Editor
{
    [CustomEditor(typeof(Tileset))]
    public sealed class TilesetEditor : UnityEditor.Editor
    {
        public const string PreviewName = "__TilesetPreview";
        public static readonly Vector3 PreviewOrigin = new(200f, 0f, 200f);

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            var changed = EditorGUI.EndChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Canonical poses: floor fills the cell; Straight walls the cell's north edge; Corner the north and east edges; posts the north-east vertex; doors the north edge of their span. Offsets are in metres from the cell centre, before the turn.", MessageType.Info);
            var preview = GameObject.Find(PreviewName);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(preview == null ? "Preview in scene" : "Refresh preview"))
                    ShowPreview((Tileset)target, true);
                if (preview != null && GUILayout.Button("Remove preview"))
                    DestroyImmediate(preview);
            }
            if (changed && preview != null)
                ShowPreview((Tileset)target, false);
        }

        public static LevelLayout ShowPreview(Tileset tileset, bool frame)
        {
            var existing = GameObject.Find(PreviewName);
            if (existing != null)
                DestroyImmediate(existing);
            var root = Create(PreviewName, null);
            root.transform.position = PreviewOrigin;
            var layout = root.AddComponent<LevelLayout>();
            layout.Tileset = tileset;

            var room = Create("Room A", root.transform).AddComponent<LayoutRoom>();
            room.Rects.Clear();
            room.Rects.Add(new RectInt(0, 0, 4, 3));
            room.Rects.Add(new RectInt(0, 3, 2, 2));
            var hallway = Create("Hallway", root.transform).AddComponent<LayoutHallway>();
            hallway.Points.Clear();
            hallway.Points.Add(new Vector2Int(4, 1));
            hallway.Points.Add(new Vector2Int(7, 1));
            hallway.Points.Add(new Vector2Int(7, 4));
            var roomB = Create("Room B", root.transform).AddComponent<LayoutRoom>();
            roomB.Rects.Clear();
            roomB.Rects.Add(new RectInt(6, 5, 3, 3));
            var door = Create("Doorway", root.transform).AddComponent<LayoutOpening>();
            door.Cell = new Vector2Int(0, 4);
            door.Side = Side.West;

            LayoutGenerator.Generate(layout, false, false);
            if (frame && SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.Frame(new Bounds(PreviewOrigin + new Vector3(4.5f, 0f, 4f) * tileset.CellSize, Vector3.one * tileset.CellSize * 9f), false);
            return layout;
        }

        private static GameObject Create(string name, Transform parent)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go;
        }
    }
}
