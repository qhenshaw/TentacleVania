using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.World.Streaming
{
    public static class RoomStreamingRules
    {
        public static float Distance(Vector3 point, Vector3 center, Vector3 size)
        {
            var dx = Mathf.Max(0f, Mathf.Abs(point.x - center.x) - size.x * 0.5f);
            var dz = Mathf.Max(0f, Mathf.Abs(point.z - center.z) - size.z * 0.5f);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static int CurrentRoom(Vector3 point, IReadOnlyList<RoomEntry> rooms, int previous)
        {
            if (previous >= 0 && previous < rooms.Count && Distance(point, rooms[previous].Center, rooms[previous].Size) <= 0f)
                return previous;
            var best = -1;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < rooms.Count; i++)
            {
                var distance = Distance(point, rooms[i].Center, rooms[i].Size);
                if (distance < bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }
            return best;
        }

        public static bool IsNeighbour(IReadOnlyList<RoomEntry> rooms, int current, int index) =>
            current >= 0 && rooms[current].Neighbours.Contains(rooms[index].Scene);

        public static bool Wanted(Vector3 point, IReadOnlyList<RoomEntry> rooms, int index, int current, float loadRadius) =>
            index == current || IsNeighbour(rooms, current, index) || Distance(point, rooms[index].Center, rooms[index].Size) <= loadRadius;

        public static bool Unwanted(Vector3 point, IReadOnlyList<RoomEntry> rooms, int index, int current, float unloadRadius) =>
            index != current && !IsNeighbour(rooms, current, index) && Distance(point, rooms[index].Center, rooms[index].Size) > unloadRadius;
    }
}
