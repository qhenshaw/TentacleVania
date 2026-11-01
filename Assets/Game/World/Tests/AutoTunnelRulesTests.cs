using System.Collections.Generic;
using NUnit.Framework;
using TentacleVania.World.Shortcuts;
using TentacleVania.World.Streaming;

namespace TentacleVania.World.Tests
{
    public sealed class AutoTunnelRulesTests
    {
        private static List<RoomEntry> Chain(int count)
        {
            var rooms = new List<RoomEntry>();
            for (var i = 0; i < count; i++)
                rooms.Add(new RoomEntry { Scene = "R" + i });
            for (var i = 0; i < count; i++)
            {
                if (i > 0)
                    rooms[i].Neighbours.Add("R" + (i - 1));
                if (i < count - 1)
                    rooms[i].Neighbours.Add("R" + (i + 1));
            }
            return rooms;
        }

        [Test]
        public void RoomsFarFromANestGetATunnelToTheNearestNest()
        {
            var pairs = AutoTunnelRules.Pairs(Chain(7), new[] { "R0" }, 4);
            Assert.That(pairs.Count, Is.EqualTo(3));
            Assert.That(pairs[0].Far, Is.EqualTo("R4"));
            Assert.That(pairs[0].Nest, Is.EqualTo("R0"));
            Assert.That(pairs[2].Far, Is.EqualTo("R6"));
        }

        [Test]
        public void TheNearestNestWins()
        {
            var pairs = AutoTunnelRules.Pairs(Chain(10), new[] { "R0", "R9" }, 4);
            Assert.That(pairs.Count, Is.EqualTo(2));
            Assert.That(pairs[0].Far, Is.EqualTo("R4"));
            Assert.That(pairs[0].Nest, Is.EqualTo("R0"));
            Assert.That(pairs[1].Far, Is.EqualTo("R5"));
            Assert.That(pairs[1].Nest, Is.EqualTo("R9"));
        }

        [Test]
        public void CloseRoomsAndUnreachableRoomsGetNone()
        {
            var rooms = Chain(3);
            rooms.Add(new RoomEntry { Scene = "Island" });
            Assert.That(AutoTunnelRules.Pairs(rooms, new[] { "R0" }, 4), Is.Empty);
        }

        [Test]
        public void EligibleFilterSkipsRooms()
        {
            var pairs = AutoTunnelRules.Pairs(Chain(7), new[] { "R0" }, 4, scene => scene != "R5");
            Assert.That(pairs.Count, Is.EqualTo(2));
            Assert.That(pairs[1].Far, Is.EqualTo("R6"));
        }

        [Test]
        public void KeepsOnlyTheFarthestRoomsWhenCapped()
        {
            var pairs = AutoTunnelRules.Pairs(Chain(8), new[] { "R0" }, 3, null, 2);
            Assert.That(pairs.Count, Is.EqualTo(2));
            Assert.That(pairs[0].Far, Is.EqualTo("R6"));
            Assert.That(pairs[1].Far, Is.EqualTo("R7"));
        }
    }
}
