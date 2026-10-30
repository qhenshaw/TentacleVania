using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.World.Layout
{
    public sealed class LevelLayout : MonoBehaviour
    {
        public const string GeneratedName = "Generated";
        public const string ManualName = "Manual";

        [SerializeField] private Tileset _tileset;
        [SerializeField] private DressingSet _dressing;
        [Tooltip("Rebuild whenever a shape handle is released or a value changes.")]
        [SerializeField] private bool _autoRegenerate = true;
        [Tooltip("Rebake the scene's NavMeshSurfaces after each rebuild from the overlay or inspector.")]
        [SerializeField] private bool _bakeNavMesh = true;

        public Tileset Tileset
        {
            get => _tileset;
            set => _tileset = value;
        }

        public DressingSet Dressing
        {
            get => _dressing;
            set => _dressing = value;
        }

        public bool AutoRegenerate => _autoRegenerate;
        public bool BakeNavMesh => _bakeNavMesh;
        public float CellSize => _tileset != null ? _tileset.CellSize : 3f;

        public Vector3 CellToWorld(Vector2 cell) =>
            transform.TransformPoint(new Vector3(cell.x * CellSize, 0f, cell.y * CellSize));

        public Vector3 CellCenter(Vector2Int cell) => CellToWorld(new Vector2(cell.x + 0.5f, cell.y + 0.5f));

        public Vector2Int WorldToCell(Vector3 world)
        {
            var local = transform.InverseTransformPoint(world) / CellSize;
            return new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.z));
        }

        public Vector2Int WorldToVertex(Vector3 world)
        {
            var local = transform.InverseTransformPoint(world) / CellSize;
            return new Vector2Int(Mathf.RoundToInt(local.x), Mathf.RoundToInt(local.z));
        }

        public void GetSpaces(List<LayoutSpace> spaces) => GetComponentsInChildren(false, spaces);

        public void GetOpenings(List<LayoutOpening> openings) => GetComponentsInChildren(false, openings);

        public LayoutGrid BuildGrid(List<LayoutSpace> spaces, List<LayoutOpening> openings)
        {
            var grid = new LayoutGrid();
            var cells = new Dictionary<Vector2Int, AxisMask>();
            foreach (var space in spaces)
            {
                cells.Clear();
                space.CollectCells(cells);
                grid.AddSpace(space.IsHallway, cells);
            }
            for (var i = 0; i < openings.Count; i++)
                grid.AddOpening(openings[i].Spec, i);
            grid.Finish();
            return grid;
        }
    }
}
