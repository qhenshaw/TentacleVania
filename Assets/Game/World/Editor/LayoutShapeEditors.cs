using TentacleVania.World.Layout;
using UnityEditor;
using UnityEngine;

namespace TentacleVania.World.Editor
{
    public abstract class LayoutShapeEditor : UnityEditor.Editor
    {
        private bool _changed;

        protected LevelLayout Layout => LayoutEditing.LayoutOf((Component)target);

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
                LayoutEditing.RequestRebuild(Layout);
            ExtraInspector();
            if (Layout == null)
                EditorGUILayout.HelpBox("Put this under a LevelLayout.", MessageType.Warning);
        }

        protected virtual void ExtraInspector()
        {
        }

        protected void Changed() => _changed = true;

        protected void EndDrag()
        {
            if (!_changed || GUIUtility.hotControl != 0)
                return;
            _changed = false;
            LayoutEditing.RequestRebuild(Layout);
        }

        protected static Vector3 Drag(Vector3 position, float size, Color color)
        {
            Handles.color = color;
            return Handles.FreeMoveHandle(position, size, Vector3.zero, Handles.SphereHandleCap);
        }

        protected Vector3 OnPlane(LevelLayout layout, Vector3 world)
        {
            var plane = new Plane(layout.transform.up, layout.transform.position);
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            return plane.Raycast(ray, out var distance) ? ray.GetPoint(distance) : world;
        }
    }

    [CustomEditor(typeof(LayoutRoom))]
    public sealed class LayoutRoomEditor : LayoutShapeEditor
    {
        private static readonly Side[] EdgeOrder = Sides.All;

        protected override void ExtraInspector()
        {
            var room = (LayoutRoom)target;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add rect"))
                {
                    Undo.RecordObject(room, "Add rect");
                    var last = room.Rects.Count > 0 ? room.Rects[^1] : new RectInt(0, 0, 3, 3);
                    room.Rects.Add(new RectInt(last.xMax, last.yMin, 3, last.height));
                    LayoutEditing.RequestRebuild(Layout);
                }
                if (GUILayout.Button("Reroll"))
                    LayoutEditing.Reroll(Layout, new[] { room });
            }
        }

        private void OnSceneGUI()
        {
            var room = (LayoutRoom)target;
            var layout = Layout;
            if (layout == null)
                return;
            for (var i = 0; i < room.Rects.Count; i++)
            {
                var rect = room.Rects[i];
                var a = layout.CellToWorld(new Vector2(rect.xMin, rect.yMin));
                var b = layout.CellToWorld(new Vector2(rect.xMax, rect.yMin));
                var c = layout.CellToWorld(new Vector2(rect.xMax, rect.yMax));
                var d = layout.CellToWorld(new Vector2(rect.xMin, rect.yMax));
                var fill = LayoutEditing.SpaceColor(false);
                Handles.DrawSolidRectangleWithOutline(new[] { a, b, c, d }, new Color(fill.r, fill.g, fill.b, 0.1f), fill);

                foreach (var side in EdgeOrder)
                {
                    var mid = side switch
                    {
                        Side.North => (c + d) * 0.5f,
                        Side.East => (b + c) * 0.5f,
                        Side.South => (a + b) * 0.5f,
                        _ => (a + d) * 0.5f,
                    };
                    var direction = layout.transform.rotation * new Vector3(Sides.Offset(side).x, 0f, Sides.Offset(side).y);
                    Handles.color = fill;
                    EditorGUI.BeginChangeCheck();
                    var moved = Handles.Slider(mid, direction, HandleUtility.GetHandleSize(mid) * 0.12f, Handles.CubeHandleCap, 0f);
                    if (!EditorGUI.EndChangeCheck())
                        continue;
                    var vertex = layout.WorldToVertex(moved);
                    var next = rect;
                    switch (side)
                    {
                        case Side.North:
                            next.yMax = Mathf.Max(vertex.y, rect.yMin + 1);
                            break;
                        case Side.South:
                            next.yMin = Mathf.Min(vertex.y, rect.yMax - 1);
                            break;
                        case Side.East:
                            next.xMax = Mathf.Max(vertex.x, rect.xMin + 1);
                            break;
                        default:
                            next.xMin = Mathf.Min(vertex.x, rect.xMax - 1);
                            break;
                    }
                    if (next.Equals(rect))
                        continue;
                    Undo.RecordObject(room, "Resize room");
                    room.Rects[i] = rect = next;
                    Changed();
                }

                var center = (a + c) * 0.5f;
                EditorGUI.BeginChangeCheck();
                Drag(center, HandleUtility.GetHandleSize(center) * 0.08f, fill);
                if (EditorGUI.EndChangeCheck())
                {
                    var cell = layout.WorldToCell(OnPlane(layout, center));
                    var position = new Vector2Int(cell.x - rect.width / 2, cell.y - rect.height / 2);
                    if (position != rect.position)
                    {
                        Undo.RecordObject(room, "Move room");
                        rect.position = position;
                        room.Rects[i] = rect;
                        Changed();
                    }
                }
            }
            EndDrag();
        }
    }

    [CustomEditor(typeof(LayoutHallway))]
    public sealed class LayoutHallwayEditor : LayoutShapeEditor
    {
        protected override void ExtraInspector()
        {
            var hallway = (LayoutHallway)target;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add point"))
                {
                    Undo.RecordObject(hallway, "Add point");
                    var points = hallway.Points;
                    var last = points.Count > 0 ? points[^1] : Vector2Int.zero;
                    var step = points.Count > 1 ? last - points[^2] : new Vector2Int(3, 0);
                    var direction = Mathf.Abs(step.x) >= Mathf.Abs(step.y) ? new Vector2Int(0, 3) : new Vector2Int(3, 0);
                    points.Add(last + direction);
                    LayoutEditing.RequestRebuild(Layout);
                }
                if (GUILayout.Button("Remove point") && hallway.Points.Count > 2)
                {
                    Undo.RecordObject(hallway, "Remove point");
                    hallway.Points.RemoveAt(hallway.Points.Count - 1);
                    LayoutEditing.RequestRebuild(Layout);
                }
                if (GUILayout.Button("Reroll"))
                    LayoutEditing.Reroll(Layout, new[] { hallway });
            }
        }

        private void OnSceneGUI()
        {
            var hallway = (LayoutHallway)target;
            var layout = Layout;
            if (layout == null)
                return;
            var color = LayoutEditing.SpaceColor(true);
            var points = hallway.Points;
            for (var i = 0; i < points.Count; i++)
            {
                var world = layout.CellCenter(points[i]);
                if (i > 0)
                {
                    Handles.color = color;
                    Handles.DrawLine(layout.CellCenter(points[i - 1]), world, 3f);
                }
                EditorGUI.BeginChangeCheck();
                Drag(world, HandleUtility.GetHandleSize(world) * 0.1f, color);
                if (!EditorGUI.EndChangeCheck())
                    continue;
                var cell = layout.WorldToCell(OnPlane(layout, world));
                if (i > 0)
                    cell = Align(points[i - 1], cell);
                if (cell == points[i])
                    continue;
                Undo.RecordObject(hallway, "Move hallway point");
                points[i] = cell;
                if (i + 1 < points.Count)
                    points[i + 1] = Align(cell, points[i + 1]);
                Changed();
            }
            EndDrag();
        }

        private static Vector2Int Align(Vector2Int from, Vector2Int to) =>
            Mathf.Abs(to.x - from.x) >= Mathf.Abs(to.y - from.y) ? new Vector2Int(to.x, from.y) : new Vector2Int(from.x, to.y);
    }

    [CustomEditor(typeof(LayoutOpening))]
    public sealed class LayoutOpeningEditor : LayoutShapeEditor
    {
        private void OnSceneGUI()
        {
            var opening = (LayoutOpening)target;
            var layout = Layout;
            if (layout == null)
                return;
            var spec = opening.Spec;
            LayoutGizmos.EdgePoints(layout, spec.CellAt(0), spec.Side, out var start, out _);
            LayoutGizmos.EdgePoints(layout, spec.CellAt(spec.Width - 1), spec.Side, out _, out var end);
            var color = new Color(1f, 0.8f, 0.2f, 1f);
            Handles.color = color;
            Handles.DrawLine(start, end, 6f);
            var mid = (start + end) * 0.5f;
            EditorGUI.BeginChangeCheck();
            Drag(mid, HandleUtility.GetHandleSize(mid) * 0.1f, color);
            if (!EditorGUI.EndChangeCheck())
            {
                EndDrag();
                return;
            }
            var point = OnPlane(layout, mid);
            var cell = layout.WorldToCell(point);
            var local = layout.transform.InverseTransformPoint(point) / layout.CellSize - new Vector3(cell.x + 0.5f, 0f, cell.y + 0.5f);
            var side = Mathf.Abs(local.x) > Mathf.Abs(local.z) ? (local.x > 0f ? Side.East : Side.West) : (local.z > 0f ? Side.North : Side.South);
            var along = Sides.Along(side);
            cell -= along * ((opening.Width - 1) / 2);
            if (cell != opening.Cell || side != opening.Side)
            {
                Undo.RecordObject(opening, "Move opening");
                opening.Cell = cell;
                opening.Side = side;
                Changed();
            }
            EndDrag();
        }
    }
}
