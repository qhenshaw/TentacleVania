using System.Collections.Generic;
using NUnit.Framework;
using TentacleVania.World.Layout;
using UnityEngine;

namespace TentacleVania.World.Tests
{
    public class LayoutGridTests
    {
        private static Dictionary<Vector2Int, AxisMask> Rects(params RectInt[] rects)
        {
            var cells = new Dictionary<Vector2Int, AxisMask>();
            foreach (var rect in rects)
                LayoutShapes.AddRect(cells, rect);
            return cells;
        }

        private static Dictionary<Vector2Int, AxisMask> Path(int width, params Vector2Int[] points)
        {
            var cells = new Dictionary<Vector2Int, AxisMask>();
            LayoutShapes.AddPath(cells, points, width);
            return cells;
        }

        [Test]
        public void OverlappingRectsOfOneRoomMergeWithoutInnerWalls()
        {
            var grid = new LayoutGrid();
            grid.AddSpace(false, Rects(new RectInt(0, 0, 3, 2), new RectInt(2, 0, 3, 2)));
            grid.Finish();
            Assert.AreEqual(10, grid.Cells.Count);
            Assert.AreEqual(EdgeState.Interior, grid.Edge(new Vector2Int(2, 0), Side.East));
            Assert.AreEqual(EdgeState.Wall, grid.Edge(new Vector2Int(4, 0), Side.East));
            Assert.AreEqual(EdgeState.Wall, grid.Edge(new Vector2Int(0, 1), Side.North));
        }

        [Test]
        public void OpeningOnAnOuterEdgeStaysOpenForTheNextScene()
        {
            var grid = new LayoutGrid();
            grid.AddSpace(false, Rects(new RectInt(0, 0, 2, 2)));
            grid.AddOpening(new OpeningSpec(new Vector2Int(1, 0), Side.East, 1), 0);
            grid.Finish();
            Assert.AreEqual(EdgeState.Opening, grid.Edge(new Vector2Int(1, 0), Side.East));
            Assert.AreEqual(EdgeState.Wall, grid.Edge(new Vector2Int(1, 1), Side.East));
            Assert.IsFalse(grid.IsConvexCorner(new Vector2Int(1, 0), Side.East));
        }

        [Test]
        public void TwoRoomsTouchingGetWallsUnlessAnOpeningJoinsThem()
        {
            var grid = new LayoutGrid();
            grid.AddSpace(false, Rects(new RectInt(0, 0, 2, 2)));
            grid.AddSpace(false, Rects(new RectInt(2, 0, 2, 2)));
            grid.AddOpening(new OpeningSpec(new Vector2Int(1, 0), Side.East, 1), 0);
            grid.Finish();
            Assert.AreEqual(EdgeState.Wall, grid.Edge(new Vector2Int(1, 1), Side.East));
            Assert.AreEqual(EdgeState.Wall, grid.Edge(new Vector2Int(2, 1), Side.West));
            Assert.AreEqual(EdgeState.Opening, grid.Edge(new Vector2Int(1, 0), Side.East));
            Assert.AreEqual(EdgeState.Opening, grid.Edge(new Vector2Int(2, 0), Side.West));
        }

        [Test]
        public void HallwayEndOpensIntoARoomButARunAlongsideItDoesNot()
        {
            var grid = new LayoutGrid();
            grid.AddSpace(false, Rects(new RectInt(0, 0, 4, 4)));
            grid.AddSpace(true, Path(1, new Vector2Int(4, 1), new Vector2Int(8, 1)));
            grid.AddSpace(true, Path(1, new Vector2Int(0, 4), new Vector2Int(3, 4)));
            grid.Finish();
            Assert.AreEqual(EdgeState.Open, grid.Edge(new Vector2Int(3, 1), Side.East));
            Assert.AreEqual(EdgeState.Open, grid.Edge(new Vector2Int(4, 1), Side.West));
            Assert.AreEqual(EdgeState.Wall, grid.Edge(new Vector2Int(1, 3), Side.North));
            Assert.AreEqual(EdgeState.Wall, grid.Edge(new Vector2Int(1, 4), Side.South));
        }

        [Test]
        public void RoomCellsWinOverAHallwayDrawnIntoTheRoom()
        {
            var grid = new LayoutGrid();
            grid.AddSpace(true, Path(1, new Vector2Int(1, 1), new Vector2Int(6, 1)));
            grid.AddSpace(false, Rects(new RectInt(0, 0, 3, 3)));
            grid.Finish();
            Assert.AreEqual(1, grid.OwnerOf(new Vector2Int(1, 1)));
            Assert.AreEqual(0, grid.OwnerOf(new Vector2Int(3, 1)));
            Assert.AreEqual(EdgeState.Open, grid.Edge(new Vector2Int(3, 1), Side.West));
        }

        [Test]
        public void WideBendFillsTheCornerBlock()
        {
            var cells = Path(3, new Vector2Int(0, 0), new Vector2Int(5, 0), new Vector2Int(5, 5));
            for (var x = 4; x <= 6; x++)
            for (var y = -1; y <= 1; y++)
                Assert.IsTrue(cells.ContainsKey(new Vector2Int(x, y)), $"missing {x},{y}");
            Assert.IsFalse(cells.ContainsKey(new Vector2Int(7, 0)));
            Assert.IsFalse(cells.ContainsKey(new Vector2Int(5, -2)));
        }

        [Test]
        public void HallwaysMeetingAtAJunctionAreOpen()
        {
            var grid = new LayoutGrid();
            grid.AddSpace(true, Path(1, new Vector2Int(0, 0), new Vector2Int(6, 0)));
            grid.AddSpace(true, Path(1, new Vector2Int(3, 5), new Vector2Int(3, 1)));
            grid.Finish();
            Assert.AreEqual(EdgeState.Open, grid.Edge(new Vector2Int(3, 1), Side.South));
            Assert.AreEqual(EdgeState.Wall, grid.Edge(new Vector2Int(2, 0), Side.North));
        }

        [Test]
        public void LShapedRoomHasConvexAndConcaveCorners()
        {
            var grid = new LayoutGrid();
            grid.AddSpace(false, Rects(new RectInt(0, 0, 2, 1), new RectInt(0, 1, 1, 1)));
            grid.Finish();
            Assert.IsTrue(grid.IsConvexCorner(new Vector2Int(0, 1), Side.West));
            Assert.IsTrue(grid.IsConvexCorner(new Vector2Int(1, 0), Side.North));
            Assert.IsTrue(grid.IsConcaveCorner(new Vector2Int(0, 0), Side.North));
            Assert.IsFalse(grid.IsConcaveCorner(new Vector2Int(0, 0), Side.South));
        }

        [Test]
        public void WeightedPickIsStableAndRespectsWeights()
        {
            var slot = new TileSlot();
            var a = new GameObject("a");
            var b = new GameObject("b");
            try
            {
                slot.Entries.Add(new TileEntry { Prefab = a, Weight = 3f });
                slot.Entries.Add(new TileEntry { Prefab = b, Weight = 1f });
                slot.Entries.Add(new TileEntry { Prefab = null, Weight = 5f });
                var counts = new int[3];
                for (var i = 0; i < 4000; i++)
                    counts[LayoutRandom.Pick(slot, LayoutRandom.Hash(7, i))]++;
                Assert.AreEqual(0, counts[2]);
                Assert.That(counts[0] / (float)counts[1], Is.InRange(2.5f, 3.5f));
                Assert.AreEqual(LayoutRandom.Pick(slot, LayoutRandom.Hash(7, 12, 3)), LayoutRandom.Pick(slot, LayoutRandom.Hash(7, 12, 3)));
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }
    }
}
