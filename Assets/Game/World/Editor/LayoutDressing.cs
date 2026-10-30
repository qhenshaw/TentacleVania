using System.Collections.Generic;
using TentacleVania.World.Layout;
using UnityEditor;
using UnityEngine;

namespace TentacleVania.World.Editor
{
    public static class LayoutDressing
    {
        public const string PropsLayerName = "Props";

        private struct Spot
        {
            public DressingZone Zone;
            public Vector3 Position;
            public Vector3 Facing;
            public Vector3 Push;
            public uint Order;
        }

        private struct Footprint
        {
            public Vector3 Position;
            public float Radius;
        }

        public static void Place(LevelLayout layout, LayoutGrid grid, List<LayoutSpace> spaces, List<LayoutOpening> openings, Transform parent)
        {
            var set = layout.Dressing;
            var inset = layout.Tileset != null ? layout.Tileset.WallInset : 1f;
            var size = layout.CellSize;
            var placed = new List<Footprint>();
            Blockers(layout, grid, set, placed);
            var propsLayer = LayerMask.NameToLayer(PropsLayerName);

            for (var s = 0; s < spaces.Count; s++)
            {
                var space = spaces[s];
                var spaceParent = LayoutGenerator.Child(parent, space.name);
                foreach (var pin in space.PinnedProps)
                {
                    if (pin.Prefab == null)
                        continue;
                    var prop = Find(set, pin.Prefab);
                    var world = layout.transform.TransformPoint(pin.Position);
                    var instance = LayoutGenerator.Spawn(pin.Prefab, spaceParent, world, layout.transform.rotation * pin.Rotation, Vector3.one);
                    Categorize(instance, prop != null ? prop.Category : PropCategory.Cover, propsLayer);
                    placed.Add(new Footprint { Position = world, Radius = prop != null ? prop.Radius : 0.5f });
                }
                if (!space.Dress)
                    continue;

                var spots = Spots(layout, grid, s, space.Seed, inset, size);
                var cover = 0;
                var index = 0;
                foreach (var spot in spots)
                {
                    index++;
                    var rule = set.RuleFor(spot.Zone);
                    if (rule == null || LayoutRandom.Value(LayoutRandom.Hash(space.Seed, index, 7)) > rule.Density)
                        continue;
                    var prop = Choose(set, spot.Zone, LayoutRandom.Hash(space.Seed, index, 11), cover);
                    if (prop == null)
                        continue;
                    var position = spot.Position + spot.Push * prop.Radius;
                    if (Blocked(placed, position, prop.Radius, rule.Spacing))
                        continue;
                    var facing = Quaternion.LookRotation(spot.Facing, Vector3.up);
                    var yaw = prop.RandomYaw ? LayoutRandom.Value(LayoutRandom.Hash(space.Seed, index, 13)) * 360f : 0f;
                    var rotation = facing * Quaternion.Euler(0f, prop.Rotation + yaw, 0f);
                    var instance = LayoutGenerator.Spawn(prop.Prefab, spaceParent, position + facing * prop.Offset, rotation, Vector3.one);
                    Categorize(instance, prop.Category, propsLayer);
                    placed.Add(new Footprint { Position = position, Radius = prop.Radius });
                    if (prop.Category == PropCategory.Cover)
                        cover++;
                }
            }
        }

        public static DressingSet.Prop Find(DressingSet set, GameObject prefab)
        {
            foreach (var prop in set.Props)
                if (prop.Prefab == prefab)
                    return prop;
            return null;
        }

        private static List<Spot> Spots(LevelLayout layout, LayoutGrid grid, int space, int seed, float inset, float size)
        {
            var spots = new List<Spot>();
            var rotation = layout.transform.rotation;
            foreach (var cell in grid.Cells)
            {
                if (grid.OwnerOf(cell) != space || Narrow(grid, cell))
                    continue;
                var center = layout.CellCenter(cell);
                var touchesEdge = false;
                foreach (var side in Sides.All)
                {
                    var normal = rotation * Direction(side);
                    var state = grid.Edge(cell, side);
                    if (state != EdgeState.Interior)
                        touchesEdge = true;
                    if (state == EdgeState.Wall)
                    {
                        var zone = NextToOpening(grid, cell, side) ? DressingZone.NearDoor : DressingZone.Wall;
                        Add(spots, zone, center + normal * (size * 0.5f - inset), -normal, -normal, seed, cell, (int)side);
                    }
                    if (grid.IsConvexCorner(cell, side))
                    {
                        var other = rotation * Direction(Sides.Clockwise(side));
                        var push = -(normal + other).normalized;
                        Add(spots, DressingZone.Corner, center + (normal + other) * (size * 0.5f - inset), push, push, seed, cell, 10 + (int)side);
                    }
                }
                if (!touchesEdge && Surrounded(grid, cell, space))
                    Add(spots, DressingZone.Center, center, rotation * Vector3.forward, Vector3.zero, seed, cell, 20);
            }
            spots.Sort((a, b) => a.Order.CompareTo(b.Order));
            return spots;
        }

        private static void Add(List<Spot> spots, DressingZone zone, Vector3 position, Vector3 facing, Vector3 push, int seed, Vector2Int cell, int salt) =>
            spots.Add(new Spot { Zone = zone, Position = position, Facing = facing, Push = push, Order = LayoutRandom.Hash(seed, cell.x, cell.y, salt, 3) });

        private static bool NextToOpening(LayoutGrid grid, Vector2Int cell, Side side)
        {
            var along = Sides.Along(side);
            return Passable(grid.Edge(cell + along, side)) || Passable(grid.Edge(cell - along, side));
        }

        private static bool Narrow(LayoutGrid grid, Vector2Int cell) =>
            (grid.IsWall(cell, Side.North) && grid.IsWall(cell, Side.South)) || (grid.IsWall(cell, Side.East) && grid.IsWall(cell, Side.West));

        private static bool Passable(EdgeState state) => state is EdgeState.Open or EdgeState.Opening;

        private static bool Surrounded(LayoutGrid grid, Vector2Int cell, int space)
        {
            for (var x = -1; x <= 1; x++)
            for (var y = -1; y <= 1; y++)
                if (grid.OwnerOf(cell + new Vector2Int(x, y)) != space)
                    return false;
            return true;
        }

        private static void Blockers(LevelLayout layout, LayoutGrid grid, DressingSet set, List<Footprint> placed)
        {
            var size = layout.CellSize;
            var rotation = layout.transform.rotation;
            foreach (var cell in grid.Cells)
            foreach (var side in Sides.All)
            {
                if (!Passable(grid.Edge(cell, side)))
                    continue;
                var normal = rotation * Direction(side);
                var edge = layout.CellCenter(cell) + normal * (size * 0.5f);
                placed.Add(new Footprint { Position = edge - normal * (set.DoorClearance * 0.5f), Radius = Mathf.Max(size * 0.5f, set.DoorClearance) });
            }
            var manual = layout.transform.Find(LevelLayout.ManualName);
            if (manual == null)
                return;
            foreach (var renderer in manual.GetComponentsInChildren<Renderer>())
            {
                var bounds = renderer.bounds;
                placed.Add(new Footprint { Position = bounds.center, Radius = Mathf.Max(bounds.extents.x, bounds.extents.z) });
            }
        }

        private static bool Blocked(List<Footprint> placed, Vector3 position, float radius, float spacing)
        {
            foreach (var other in placed)
            {
                var offset = other.Position - position;
                offset.y = 0f;
                var needed = Mathf.Max(spacing, radius + other.Radius);
                if (offset.sqrMagnitude < needed * needed)
                    return true;
            }
            return false;
        }

        private static DressingSet.Prop Choose(DressingSet set, DressingZone zone, uint hash, int cover)
        {
            var total = 0f;
            foreach (var prop in set.Props)
                if (Fits(set, prop, zone, cover))
                    total += prop.Weight;
            if (total <= 0f)
                return null;
            var roll = LayoutRandom.Value(hash) * total;
            DressingSet.Prop last = null;
            foreach (var prop in set.Props)
            {
                if (!Fits(set, prop, zone, cover))
                    continue;
                last = prop;
                roll -= prop.Weight;
                if (roll < 0f)
                    return prop;
            }
            return last;
        }

        private static bool Fits(DressingSet set, DressingSet.Prop prop, DressingZone zone, int cover) =>
            prop.Prefab != null && prop.Weight > 0f && (prop.Zones & zone) != 0 &&
            (prop.Category != PropCategory.Cover || cover < set.MaxCoverPerSpace);

        private static void Categorize(GameObject instance, PropCategory category, int propsLayer)
        {
            switch (category)
            {
                case PropCategory.WalkThrough:
                    foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                        collider.enabled = false;
                    break;
                case PropCategory.Dynamic:
                    if (propsLayer >= 0)
                        foreach (var child in instance.GetComponentsInChildren<Transform>(true))
                            child.gameObject.layer = propsLayer;
                    break;
            }
        }

        private static Vector3 Direction(Side side)
        {
            var offset = Sides.Offset(side);
            return new Vector3(offset.x, 0f, offset.y);
        }
    }
}
