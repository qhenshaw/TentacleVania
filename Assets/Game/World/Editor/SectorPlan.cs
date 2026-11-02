using System;
using System.Collections.Generic;
using TentacleVania.World.Layout;
using UnityEngine;

namespace TentacleVania.World.Editor
{
    public enum PlanRole
    {
        Plain,
        Arena,
        Boss,
    }

    public sealed class DoorPlan
    {
        public Vector2Int Cell;
        public Side Side;
        public bool ArenaDoor;

        public DoorPlan(int x, int y, Side side, bool arenaDoor = false)
        {
            Cell = new Vector2Int(x, y);
            Side = side;
            ArenaDoor = arenaDoor;
        }
    }

    public sealed class WaveEntryPlan
    {
        public string Prefab;
        public int Count;
        public bool Elite;

        public WaveEntryPlan(string prefab, int count, bool elite = false)
        {
            Prefab = prefab;
            Count = count;
            Elite = elite;
        }
    }

    public sealed class RoomPlan
    {
        public string Scene;
        public PlanRole Role;
        public RectInt? Rect;
        public readonly List<Vector2Int[]> Halls = new();
        public readonly List<DoorPlan> Doors = new();
        public readonly List<WaveEntryPlan[]> Waves = new();
        public bool WallMounts;
        public bool OneTime;
        public Action<RoomContext> Place;

        public RoomPlan(string scene, PlanRole role, RectInt? rect = null)
        {
            Scene = scene;
            Role = role;
            Rect = rect;
        }

        public RoomPlan Hall(params Vector2Int[] points)
        {
            Halls.Add(points);
            return this;
        }

        public RoomPlan Door(int x, int y, Side side, bool arenaDoor = false)
        {
            Doors.Add(new DoorPlan(x, y, side, arenaDoor));
            return this;
        }

        public RoomPlan Wave(params WaveEntryPlan[] entries)
        {
            Waves.Add(entries);
            return this;
        }
    }

    public sealed class SectorPlan
    {
        public string Sector;
        public string RootScene;
        public string Folder;
        public string AddressablesGroup;
        public Color Color;
        public Vector3 Spawn;
        public float SpawnYaw;
        public bool RunKit;
        public readonly List<RoomPlan> Rooms = new();
    }
}
