using System;
using System.Collections.Generic;

namespace TentacleVania.Core.Runs
{
    public sealed class FloorPlan
    {
        public FloorPlan(int floor, IReadOnlyList<RoomRole> steps)
        {
            Floor = floor;
            Steps = steps ?? Array.Empty<RoomRole>();
        }

        public int Floor { get; }
        public IReadOnlyList<RoomRole> Steps { get; }
        public int Count => Steps.Count;

        public int CountOf(RoomRole role)
        {
            var count = 0;
            foreach (var step in Steps)
                if (step == role)
                    count++;
            return count;
        }
    }
}
