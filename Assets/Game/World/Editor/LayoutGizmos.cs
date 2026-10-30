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

        [DrawGizmo(GizmoType.Selected | GizmoType.InSelectionHierarchy | GizmoType.NonSelected)]
        private static void Draw(LevelLayout layout, GizmoType type)
        {
            var active = (type & (GizmoType.Selected | GizmoType.InSelectionHierarchy)) != 0;
            Spaces.Clear();
            Openings.Clear();
            layout.GetSpaces(Spaces);
            layout.GetOpenings(Openings);
            var grid = layout.BuildGrid(Spaces, Openings);
            var size = layout.CellSize;
            var up = layout.transform.up * Lift;
            foreach (var cell in grid.Cells)
            {
                var hallway = grid.IsHallwaySpace(grid.OwnerOf(cell));
                var color = LayoutEditing.SpaceColor(hallway);
                if (active)
                {
                    color.a = 0.08f;
                    Gizmos.color = color;
                    var center = layout.CellCenter(cell) + up;
                    Gizmos.matrix = Matrix4x4.TRS(center, layout.transform.rotation, Vector3.one);
                    Gizmos.DrawCube(Vector3.zero, new Vector3(size * 0.96f, 0.01f, size * 0.96f));
                    Gizmos.matrix = Matrix4x4.identity;
                }
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
