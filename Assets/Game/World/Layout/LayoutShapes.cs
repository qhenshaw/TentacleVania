using System;
using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.World.Layout
{
    [Flags]
    public enum AxisMask
    {
        None = 0,
        EastWest = 1,
        NorthSouth = 2,
    }

    public readonly struct OpeningSpec
    {
        public readonly Vector2Int Cell;
        public readonly Side Side;
        public readonly int Width;

        public OpeningSpec(Vector2Int cell, Side side, int width)
        {
            Cell = cell;
            Side = side;
            Width = Mathf.Max(1, width);
        }

        public Vector2Int CellAt(int index) => Cell + Sides.Along(Side) * index;
    }

    public static class Sides
    {
        public static readonly Side[] All = { Side.North, Side.East, Side.South, Side.West };

        public static Vector2Int Offset(Side side) => side switch
        {
            Side.North => Vector2Int.up,
            Side.East => Vector2Int.right,
            Side.South => Vector2Int.down,
            _ => Vector2Int.left,
        };

        public static Side Opposite(Side side) => (Side)(((int)side + 2) % 4);
        public static Side Clockwise(Side side) => (Side)(((int)side + 1) % 4);
        public static float Yaw(Side side) => (int)side * 90f;
        public static AxisMask Axis(Side side) => side is Side.North or Side.South ? AxisMask.NorthSouth : AxisMask.EastWest;
        public static Vector2Int Along(Side side) => side is Side.North or Side.South ? Vector2Int.right : Vector2Int.up;
    }

    public static class LayoutShapes
    {
        public static void AddRect(Dictionary<Vector2Int, AxisMask> cells, RectInt rect)
        {
            for (var x = rect.xMin; x < rect.xMax; x++)
            for (var y = rect.yMin; y < rect.yMax; y++)
            {
                var cell = new Vector2Int(x, y);
                if (!cells.ContainsKey(cell))
                    cells[cell] = AxisMask.None;
            }
        }

        public static void AddPath(Dictionary<Vector2Int, AxisMask> cells, IReadOnlyList<Vector2Int> points, int width)
        {
            width = Mathf.Max(1, width);
            var low = -(width - 1) / 2;
            if (points.Count == 1)
            {
                for (var a = 0; a < width; a++)
                for (var b = 0; b < width; b++)
                    Mark(cells, points[0] + new Vector2Int(low + a, low + b), AxisMask.EastWest | AxisMask.NorthSouth);
                return;
            }
            for (var i = 1; i < points.Count; i++)
            {
                var from = points[i - 1];
                var to = points[i];
                var delta = to - from;
                var horizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
                if (horizontal)
                    to.y = from.y;
                else
                    to.x = from.x;
                var step = horizontal ? new Vector2Int(Math.Sign(to.x - from.x), 0) : new Vector2Int(0, Math.Sign(to.y - from.y));
                var across = horizontal ? Vector2Int.up : Vector2Int.right;
                var axis = horizontal ? AxisMask.EastWest : AxisMask.NorthSouth;
                var length = horizontal ? Mathf.Abs(to.x - from.x) : Mathf.Abs(to.y - from.y);
                var start = i > 1 ? low : 0;
                var end = i < points.Count - 1 ? length + low + width - 1 : length;
                for (var s = Mathf.Min(start, 0); s <= Mathf.Max(end, length); s++)
                for (var a = 0; a < width; a++)
                    Mark(cells, from + step * s + across * (low + a), axis);
            }
        }

        private static void Mark(Dictionary<Vector2Int, AxisMask> cells, Vector2Int cell, AxisMask axis) =>
            cells[cell] = cells.TryGetValue(cell, out var existing) ? existing | axis : axis;
    }
}
