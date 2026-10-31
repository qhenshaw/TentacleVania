using System.Collections.Generic;
using TentacleVania.World.Streaming;
using UnityEngine;

namespace TentacleVania.World.Map
{
    public static class TunnelRoute
    {
        private static readonly Queue<int> Open = new();
        private static readonly List<int> Previous = new();

        public static void Build(IReadOnlyList<RoomEntry> rooms, Vector3 from, Vector3 to, List<Vector3> path)
        {
            path.Clear();
            path.Add(from);
            var start = RoomStreamingRules.CurrentRoom(from, rooms, -1);
            var goal = RoomStreamingRules.CurrentRoom(to, rooms, -1);
            if (start < 0 || goal < 0 || start == goal)
                return;
            Previous.Clear();
            for (var i = 0; i < rooms.Count; i++)
                Previous.Add(-2);
            Previous[start] = -1;
            Open.Clear();
            Open.Enqueue(start);
            while (Open.Count > 0)
            {
                var current = Open.Dequeue();
                if (current == goal)
                    break;
                foreach (var name in rooms[current].Neighbours)
                {
                    var next = IndexOf(rooms, name);
                    if (next < 0 || Previous[next] != -2)
                        continue;
                    Previous[next] = current;
                    Open.Enqueue(next);
                }
            }
            if (Previous[goal] == -2)
                return;
            var insert = path.Count;
            for (var room = Previous[goal]; room >= 0 && room != start; room = Previous[room])
                path.Insert(insert, rooms[room].Center);
        }

        private static int IndexOf(IReadOnlyList<RoomEntry> rooms, string scene)
        {
            for (var i = 0; i < rooms.Count; i++)
                if (rooms[i].Scene == scene)
                    return i;
            return -1;
        }
    }
}
