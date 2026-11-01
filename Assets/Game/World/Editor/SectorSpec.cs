using System.Collections.Generic;

namespace TentacleVania.World.Editor
{
    public sealed class SectorSpec
    {
        public string Sector;
        public string RootScene;
        public string Folder;
        public string AddressablesGroup;
        public float[] Color;
        public float[] Spawn;
        public float SpawnYaw;
        public List<RoomSpec> Rooms = new();
    }

    public sealed class RoomSpec
    {
        public string Scene;
        public string Role = "Plain";
        public int[] Rect;
        public bool WallMounts;
        public bool OneTime;
        public List<int[][]> Halls = new();
        public List<DoorSpec> Doors = new();
        public List<List<WaveEntrySpec>> Waves = new();
        public List<PlacementSpec> Place = new();
    }

    public sealed class DoorSpec
    {
        public int X;
        public int Y;
        public string Side;
        public bool Arena;
    }

    public sealed class WaveEntrySpec
    {
        public string Enemy;
        public int Count = 1;
    }

    public sealed class PlacementSpec
    {
        public string Type;
        public float[] At;
        public float[] Cell;
        public float[] World;
        public float Yaw;
        public string Id;
        public string Name;
        public string Prefab;
        public string Log;
        public string System;
        public string ItemId;
        public bool OpensHere;
        public string Nest;
        public float[] OtherAt;
        public float OtherYaw;
        public string Sequence;
        public float[] Size;
        public float Delay;
        public string Destination;
        public float[] ArrivalAt;
        public float[] ArrivalOffset;
        public float ArrivalYaw;
        public string RequiredBoss;
        public string Hook;
        public string Kind;
        public string RequiredAbility;
    }
}
