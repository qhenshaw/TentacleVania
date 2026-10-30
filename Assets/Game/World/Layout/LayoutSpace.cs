using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.World.Layout
{
    public abstract class LayoutSpace : MonoBehaviour
    {
        [SerializeField] private int _seed = 1;
        [SerializeField] private bool _dress = true;
        [SerializeField] private List<CellPin> _floorPins = new();
        [SerializeField] private List<EdgePin> _wallPins = new();
        [SerializeField] private List<PinnedProp> _pinnedProps = new();

        public int Seed
        {
            get => _seed;
            set => _seed = value;
        }

        public bool Dress => _dress;
        public List<CellPin> FloorPins => _floorPins;
        public List<EdgePin> WallPins => _wallPins;
        public List<PinnedProp> PinnedProps => _pinnedProps;

        public abstract bool IsHallway { get; }

        public abstract void CollectCells(Dictionary<Vector2Int, AxisMask> cells);
    }
}
