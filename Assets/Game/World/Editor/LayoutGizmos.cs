using System.Collections.Generic;
using TentacleVania.World.Layout;
using UnityEditor;
using UnityEngine;

namespace TentacleVania.World.Editor
{
    public static class LayoutGizmos
    {
        private const float Lift = 0.05f;

        private static readonly List<LayoutSpace> Spaces = new();
        private static readonly List<LayoutOpening> Openings = new();
        private static readonly Dictionary<Vector2Int, AxisMask> Cells = new();

        [DrawGizmo(GizmoType.Selected | GizmoType.InSelectionHierarchy | GizmoType.NonSelected)]
        private static void Draw(LevelLayout layout, GizmoType type)
        {
            var active = (type & (GizmoType.Selected | GizmoType.InSelectionHierarchy)) != 0;
            Spaces.Clear();
            Openings.Clear();
            layout.GetSpaces(Spaces);
            layout.GetOpenings(Openings);
            var grid = layout.BuildGrid(Spaces, Openings);
            var up = layout.transform.up * Lift;
            foreach (var cell in grid.Cells)
            {
                foreach (var side in Sides.All)
                {
                    var state = grid.Edge(cell, side);
                    if (state is EdgeState.Interior)
                        continue;
                    Gizmos.color = state switch
                    {
                        EdgeState.Wall => active ? Color.white : new Color(1f, 1f, 1f, 0.35f),
                        EdgeState.Opening => new Color(1f, 0.8f, 0.2f, 1f),
                        _ => new Color(0.3f, 1f, 0.5f, 0.6f),
                    };
                    EdgePoints(layout, cell, side, out var a, out var b);
                    Gizmos.DrawLine(a + up, b + up);
                }
            }
        }

        [DrawGizmo(GizmoType.Pickable | GizmoType.Selected | GizmoType.InSelectionHierarchy | GizmoType.NonSelected)]
        private static void DrawRoom(LayoutRoom room, GizmoType type)
        {
            var layout = room.GetComponentInParent<LevelLayout>();
            if (layout == null)
                return;
            BeginShape(layout, false, type);
            foreach (var rect in room.Rects)
                Pad(layout, rect.center, rect.size);
            Gizmos.matrix = Matrix4x4.identity;
        }

        [DrawGizmo(GizmoType.Pickable | GizmoType.Selected | GizmoType.InSelectionHierarchy | GizmoType.NonSelected)]
        private static void DrawHallway(LayoutHallway hallway, GizmoType type)
        {
            var layout = hallway.GetComponentInParent<LevelLayout>();
            if (layout == null)
                return;
            BeginShape(layout, true, type);
            Cells.Clear();
            hallway.CollectCells(Cells);
            foreach (var cell in Cells.Keys)
                Pad(layout, cell + new Vector2(0.5f, 0.5f), Vector2.one);
            Gizmos.matrix = Matrix4x4.identity;
        }

        [DrawGizmo(GizmoType.Pickable | GizmoType.Selected | GizmoType.InSelectionHierarchy | GizmoType.NonSelected)]
        private static void DrawOpening(LayoutOpening opening, GizmoType type)
        {
            var layout = opening.GetComponentInParent<LevelLayout>();
            if (layout == null)
                return;
            var spec = opening.Spec;
            EdgePoints(layout, spec.CellAt(0), spec.Side, out var start, out _);
            EdgePoints(layout, spec.CellAt(spec.Width - 1), spec.Side, out _, out var end);
            var selected = (type & GizmoType.Selected) != 0;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, selected ? 0.9f : 0.5f);
            var along = end - start;
            var size = layout.CellSize;
            Gizmos.matrix = Matrix4x4.TRS((start + end) * 0.5f + layout.transform.up * size * 0.25f, Quaternion.LookRotation(along.normalized, layout.transform.up), Vector3.one);
            Gizmos.DrawCube(Vector3.zero, new Vector3(size * 0.2f, size * 0.5f, along.magnitude * 0.9f));
            Gizmos.matrix = Matrix4x4.identity;
        }

        private static void BeginShape(LevelLayout layout, bool hallway, GizmoType type)
        {
            var color = LayoutEditing.SpaceColor(hallway);
            color.a = (type & GizmoType.Selected) != 0 ? 0.22f : (type & GizmoType.InSelectionHierarchy) != 0 ? 0.1f : 0.05f;
            Gizmos.color = color;
            Gizmos.matrix = Matrix4x4.TRS(layout.transform.position + layout.transform.up * Lift, layout.transform.rotation, Vector3.one);
        }

        private static void Pad(LevelLayout layout, Vector2 center, Vector2 cells)
        {
            var size = layout.CellSize;
            Gizmos.DrawCube(new Vector3(center.x * size, 0f, center.y * size), new Vector3(cells.x * size - 0.1f, 0.02f, cells.y * size - 0.1f));
        }

        public static void EdgePoints(LevelLayout layout, Vector2Int cell, Side side, out Vector3 a, out Vector3 b)
        {
            var corners = side switch
            {
                Side.North => (new Vector2(cell.x, cell.y + 1), new Vector2(cell.x + 1, cell.y + 1)),
                Side.East => (new Vector2(cell.x + 1, cell.y), new Vector2(cell.x + 1, cell.y + 1)),
                Side.South => (new Vector2(cell.x, cell.y), new Vector2(cell.x + 1, cell.y)),
                _ => (new Vector2(cell.x, cell.y), new Vector2(cell.x, cell.y + 1)),
            };
            a = layout.CellToWorld(corners.Item1);
            b = layout.CellToWorld(corners.Item2);
        }
    }
}
