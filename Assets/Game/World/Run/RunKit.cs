using System;
using System.Collections.Generic;
using TentacleVania.Core.Runs;
using TentacleVania.Enemies;
using UnityEngine;

namespace TentacleVania.World.Run
{
    [CreateAssetMenu(menuName = "TentacleVania/World/Run Kit")]
    public sealed class RunKit : ScriptableObject
    {
        public const string RootScene = "Run";

        [Serializable]
        public sealed class Room
        {
            [Tooltip("Room scene (its Addressables address).")]
            public string Scene;
            public RoomRole Role;
            [Tooltip("Floor 0 (L0) to 3 (L3); -1 = any floor.")]
            public int Floor = -1;
            [Tooltip("Keep the scene's own waves instead of composing them (rare elite rooms with optional bosses).")]
            public bool KeepWaves;
        }

        [Serializable]
        public sealed class FloorPool
        {
            [Range(0, 3)] public int Floor;
            [Tooltip("DifficultyCurve row used on this floor.")]
            public string Sector = "SH";
            public List<Enemy> Enemies = new();
            [Tooltip("At least one of these per wave: grabbable at any HP (consume healing).")]
            public List<Enemy> Prey = new();
        }

        [SerializeField] private List<Room> _rooms = new();
        [SerializeField] private List<FloorPool> _floors = new();
        [SerializeField, Range(1, 4)] private int _floorCount = 4;

        public int FloorCount => _floorCount;

        public int Count(int floor, RoomRole role)
        {
            var own = HasOwn(floor, role);
            var count = 0;
            foreach (var room in _rooms)
                if (Matches(room, floor, role, own))
                    count++;
            return count;
        }

        public string Scene(int floor, RoomRole role, int index) => Find(floor, role, index)?.Scene;

        public bool KeepsWaves(int floor, RoomRole role, int index) => Find(floor, role, index)?.KeepWaves ?? false;

        private Room Find(int floor, RoomRole role, int index)
        {
            var own = HasOwn(floor, role);
            var count = 0;
            foreach (var room in _rooms)
            {
                if (!Matches(room, floor, role, own))
                    continue;
                if (count++ == index)
                    return room;
            }
            return null;
        }

        private bool HasOwn(int floor, RoomRole role)
        {
            foreach (var room in _rooms)
                if (room.Role == role && room.Floor == floor)
                    return true;
            return false;
        }

        private static bool Matches(Room room, int floor, RoomRole role, bool own) => room.Role == role && (own ? room.Floor == floor : room.Floor < 0);

        public FloorPool Pool(int floor)
        {
            FloorPool fallback = null;
            foreach (var pool in _floors)
            {
                if (pool.Floor == floor)
                    return pool;
                fallback ??= pool;
            }
            return fallback;
        }
    }
}
