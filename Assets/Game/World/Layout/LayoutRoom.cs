using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.World.Layout
{
    public sealed class LayoutRoom : LayoutSpace
    {
        [Tooltip("Cell rectangles; overlapping or touching ones merge into one space.")]
        [SerializeField] private List<RectInt> _rects = new() { new RectInt(0, 0, 4, 4) };

        public List<RectInt> Rects => _rects;
        public override bool IsHallway => false;

        public override void CollectCells(Dictionary<Vector2Int, AxisMask> cells)
        {
            foreach (var rect in _rects)
                LayoutShapes.AddRect(cells, rect);
        }
    }
}
