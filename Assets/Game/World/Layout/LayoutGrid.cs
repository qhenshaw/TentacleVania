using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.World.Layout
{
    public enum EdgeState
    {
        Interior,
        Wall,
        Open,
        Opening,
    }

    public sealed class LayoutGrid
    {
        private readonly Dictionary<Vector2Int, int> _owner = new();
        private readonly Dictionary<Vector2Int, AxisMask> _axes = new();
        private readonly List<bool> _hallway = new();
        private readonly Dictionary<(Vector2Int, Side), int> _openings = new();
        private readonly List<Vector2Int> _cells = new();

        public IReadOnlyList<Vector2Int> Cells => _cells;
        public int SpaceCount => _hallway.Count;

        public int AddSpace(bool hallway, Dictionary<Vector2Int, AxisMask> cells)
        {
            var id = _hallway.Count;
            _hallway.Add(hallway);
            foreach (var pair in cells)
            {
                if (_owner.TryGetValue(pair.Key, out var owner))
                {
                    if (hallway || !_hallway[owner])
                        continue;
                    _owner[pair.Key] = id;
                    _axes[pair.Key] = pair.Value;
                    continue;
                }
                _owner[pair.Key] = id;
                _axes[pair.Key] = pair.Value;
                _cells.Add(pair.Key);
            }
            return id;
        }

        public void AddOpening(OpeningSpec opening, int index)
        {
            for (var i = 0; i < opening.Width; i++)
            {
                var cell = opening.CellAt(i);
                _openings[(cell, opening.Side)] = index;
                _openings[(cell + Sides.Offset(opening.Side), Sides.Opposite(opening.Side))] = index;
            }
        }

        public void Finish() => _cells.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

        public bool Contains(Vector2Int cell) => _owner.ContainsKey(cell);
        public int OwnerOf(Vector2Int cell) => _owner.TryGetValue(cell, out var owner) ? owner : -1;
        public bool IsHallwaySpace(int space) => space >= 0 && space < _hallway.Count && _hallway[space];

        public bool TryGetOpening(Vector2Int cell, Side side, out int index) => _openings.TryGetValue((cell, side), out index);

        public EdgeState Edge(Vector2Int cell, Side side)
        {
            if (!_owner.TryGetValue(cell, out var owner))
                return EdgeState.Interior;
            var neighbour = cell + Sides.Offset(side);
            var hasNeighbour = _owner.TryGetValue(neighbour, out var other);
            if (_openings.ContainsKey((cell, side)))
                return EdgeState.Opening;
            if (!hasNeighbour)
                return EdgeState.Wall;
            if (other == owner)
                return EdgeState.Interior;
            var axis = Sides.Axis(side);
            var mine = _hallway[owner] && (_axes[cell] & axis) != 0;
            var theirs = _hallway[other] && (_axes[neighbour] & axis) != 0;
            return mine || theirs ? EdgeState.Open : EdgeState.Wall;
        }

        public bool IsWall(Vector2Int cell, Side side) => Edge(cell, side) == EdgeState.Wall;

        public bool IsConvexCorner(Vector2Int cell, Side first) =>
            IsWall(cell, first) && IsWall(cell, Sides.Clockwise(first));

        public bool IsConcaveCorner(Vector2Int cell, Side first)
        {
            var second = Sides.Clockwise(first);
            if (!Contains(cell) || IsWall(cell, first) || IsWall(cell, second))
                return false;
            var a = cell + Sides.Offset(first);
            var b = cell + Sides.Offset(second);
            return IsWall(a, second) && IsWall(b, first);
        }
    }
}
