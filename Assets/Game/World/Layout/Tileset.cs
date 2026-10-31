using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.World.Layout
{
    [CreateAssetMenu(menuName = "TentacleVania/World/Layout Tileset")]
    public sealed class Tileset : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float _cellSize = 3f;
        [SerializeField] private TileSlot _floor = new();
        [SerializeField] private List<WallLayer> _wallLayers = new() { new WallLayer() };
        [Tooltip("Distance from a cell edge to the inside face of the wall, used to keep props off walls.")]
        [SerializeField, Min(0f)] private float _wallInset = 1f;

        public float CellSize => _cellSize;
        public TileSlot Floor => _floor;
        public IReadOnlyList<WallLayer> WallLayers => _wallLayers;
        public float WallInset => _wallInset;
    }
}
