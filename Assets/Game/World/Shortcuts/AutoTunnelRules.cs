using System;
using System.Collections.Generic;
using TentacleVania.World.Streaming;

namespace TentacleVania.World.Shortcuts
{
    public readonly struct AutoTunnelPair
    {
        public readonly string Far;
        public readonly string Nest;
        public readonly int Distance;

        public AutoTunnelPair(string far, string nest, int distance)
        {
            Far = far;
            Nest = nest;
            Distance = distance;
        }
    }

    public static class AutoTunnelRules
    {
        public static List<AutoTunnelPair> Pairs(IReadOnlyList<RoomEntry> rooms, IEnumerable<string> nestRooms, int minDistance, Func<string, bool> eligible = null, int maxPairs = int.MaxValue)
        {
            var distance = new Dictionary<string, int>();
            var source = new Dictionary<string, string>();
            var open = new Queue<string>();
            foreach (var nest in nestRooms)
            {
                if (distance.ContainsKey(nest))
                    continue;
                distance[nest] = 0;
                source[nest] = nest;
                open.Enqueue(nest);
            }
            while (open.Count > 0)
            {
                var current = open.Dequeue();
                var room = Find(rooms, current);
                if (room == null)
                    continue;
                foreach (var next in room.Neighbours)
                {
                    if (distance.ContainsKey(next))
                        continue;
                    distance[next] = distance[current] + 1;
                    source[next] = source[current];
                    open.Enqueue(next);
                }
            }
            var pairs = new List<AutoTunnelPair>();
            foreach (var room in rooms)
            {
                if (!distance.TryGetValue(room.Scene, out var steps) || steps < minDistance)
                    continue;
                if (eligible != null && !eligible(room.Scene))
                    continue;
                pairs.Add(new AutoTunnelPair(room.Scene, source[room.Scene], steps));
            }
            if (pairs.Count <= maxPairs)
                return pairs;
            var order = new List<AutoTunnelPair>(pairs);
            order.Sort((a, b) => b.Distance.CompareTo(a.Distance));
            var kept = new HashSet<string>();
            for (var i = 0; i < maxPairs; i++)
                kept.Add(order[i].Far);
            pairs.RemoveAll(pair => !kept.Contains(pair.Far));
            return pairs;
        }

        private static RoomEntry Find(IReadOnlyList<RoomEntry> rooms, string scene)
        {
            foreach (var room in rooms)
                if (room.Scene == scene)
                    return room;
            return null;
        }
    }
}
