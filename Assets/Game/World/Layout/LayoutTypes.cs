using System;
using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.World.Layout
{
    public enum Side
    {
        North,
        East,
        South,
        West,
    }

    public enum OpeningKind
    {
        Doorway,
        Door,
        Prefab,
    }

    [Flags]
    public enum DressingZone
    {
        None = 0,
        Wall = 1,
        Corner = 2,
        Center = 4,
        NearDoor = 8,
    }

    public enum PropCategory
    {
        WalkThrough,
        Dynamic,
        Cover,
    }

    [Serializable]
    public sealed class TileEntry
    {
        public GameObject Prefab;
        [Min(0f)] public float Weight = 1f;
        public Vector3 Offset;
        [Range(-180f, 180f)] public float Rotation;
        public Vector3 Scale = Vector3.one;
        [Tooltip("Floors only: turn by a random multiple of 90 degrees.")]
        public bool RandomQuarterTurns;
    }

    [Serializable]
    public sealed class TileSlot
    {
        public List<TileEntry> Entries = new();

        public bool IsEmpty
        {
            get
            {
                foreach (var entry in Entries)
                    if (entry.Prefab != null && entry.Weight > 0f)
                        return false;
                return true;
            }
        }
    }

    [Serializable]
    public sealed class WallLayer
    {
        public string Name = "Walls";
        [Tooltip("Canonical: a wall on the cell's north edge, the room to the south.")]
        public TileSlot Straight = new();
        [Tooltip("Canonical: walls on the cell's north and east edges; replaces both straight pieces.")]
        public TileSlot Corner = new();
        [Tooltip("Canonical: a post at the cell's north-east vertex where its north and east walls meet.")]
        public TileSlot ConvexPost = new();
        [Tooltip("Canonical: a post at the cell's north-east vertex of an inside corner (north and east neighbours open, the diagonal walled off).")]
        public TileSlot ConcavePost = new();
        public bool Static = true;
    }

    [Serializable]
    public struct CellPin
    {
        public Vector2Int Cell;
        public int Variant;
    }

    [Serializable]
    public struct EdgePin
    {
        public Vector2Int Cell;
        public Side Side;
        public int Layer;
        public int Variant;
    }

    [Serializable]
    public struct PinnedProp
    {
        public GameObject Prefab;
        public Vector3 Position;
        public Quaternion Rotation;
    }
}
