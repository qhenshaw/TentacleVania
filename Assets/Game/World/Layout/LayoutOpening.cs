using UnityEngine;

namespace TentacleVania.World.Layout
{
    public sealed class LayoutOpening : MonoBehaviour
    {
        [Tooltip("The cell on one side of the opening; the opening is on this cell's Side edge.")]
        [SerializeField] private Vector2Int _cell;
        [SerializeField] private Side _side;
        [Tooltip("Width in cells, running east (north/south edges) or north (east/west edges) from Cell.")]
        [SerializeField, Min(1)] private int _width = 1;
        [SerializeField] private OpeningKind _kind;
        [Tooltip("Placed at the opening when Kind is Prefab, e.g. an arena blast door.")]
        [SerializeField] private GameObject _prefab;

        public Vector2Int Cell
        {
            get => _cell;
            set => _cell = value;
        }

        public Side Side
        {
            get => _side;
            set => _side = value;
        }

        public int Width => _width;
        public OpeningKind Kind => _kind;
        public GameObject Prefab => _prefab;
        public OpeningSpec Spec => new(_cell, _side, _width);
    }
}
