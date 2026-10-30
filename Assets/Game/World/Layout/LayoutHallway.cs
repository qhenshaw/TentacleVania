using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.World.Layout
{
    public sealed class LayoutHallway : LayoutSpace
    {
        [Tooltip("Cell path with 90 degree bends; consecutive points must share a row or column.")]
        [SerializeField] private List<Vector2Int> _points = new() { new Vector2Int(0, 0), new Vector2Int(4, 0) };
        [SerializeField, Min(1)] private int _width = 1;

        public List<Vector2Int> Points => _points;

        public int Width
        {
            get => _width;
            set => _width = Mathf.Max(1, value);
        }

        public override bool IsHallway => true;

        public override void CollectCells(Dictionary<Vector2Int, AxisMask> cells) => LayoutShapes.AddPath(cells, _points, _width);
    }
}
