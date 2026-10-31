using System.Collections.Generic;
using NUnit.Framework;
using TentacleVania.World.Streaming;
using UnityEngine;

namespace TentacleVania.World.Tests
{
    public sealed class RoomStreamingRulesTests
    {
        private static List<RoomEntry> Chain()
        {
            var rooms = new List<RoomEntry>();
            for (var i = 0; i < 5; i++)
                rooms.Add(new RoomEntry { Scene = "R" + i, Center = new Vector3(0f, 0f, i * 20f), Size = new Vector3(20f, 4f, 20f) });
            for (var i = 0; i < 5; i++)
            {
                if (i > 0)
                    rooms[i].Neighbours.Add("R" + (i - 1));
                if (i < 4)
                    rooms[i].Neighbours.Add("R" + (i + 1));
            }
            return rooms;
        }

        [Test]
        public void DistanceIsZeroInsideAndFlatOnTheFloor()
        {
            Assert.That(RoomStreamingRules.Distance(new Vector3(3f, 50f, 4f), Vector3.zero, new Vector3(20f, 4f, 20f)), Is.EqualTo(0f));
            Assert.That(RoomStreamingRules.Distance(new Vector3(13f, 0f, 0f), Vector3.zero, new Vector3(20f, 4f, 20f)), Is.EqualTo(3f).Within(1e-4f));
            Assert.That(RoomStreamingRules.Distance(new Vector3(13f, 0f, 14f), Vector3.zero, new Vector3(20f, 4f, 20f)), Is.EqualTo(5f).Within(1e-4f));
        }

        [Test]
        public void CurrentRoomIsTheOneContainingThePoint()
        {
            var rooms = Chain();
            Assert.That(RoomStreamingRules.CurrentRoom(new Vector3(0f, 0f, 41f), rooms, -1), Is.EqualTo(2));
            Assert.That(RoomStreamingRules.CurrentRoom(new Vector3(0f, 0f, 200f), rooms, -1), Is.EqualTo(4));
        }

        [Test]
        public void CurrentRoomSticksOnTheSharedEdge()
        {
            var rooms = Chain();
            Assert.That(RoomStreamingRules.CurrentRoom(new Vector3(0f, 0f, 10f), rooms, 0), Is.EqualTo(0));
            Assert.That(RoomStreamingRules.CurrentRoom(new Vector3(0f, 0f, 10f), rooms, 1), Is.EqualTo(1));
        }

        [Test]
        public void NeighboursLoadEvenOutsideTheRadius()
        {
            var rooms = Chain();
            var point = new Vector3(0f, 0f, 0f);
            Assert.That(RoomStreamingRules.Wanted(point, rooms, 1, 0, 0f), Is.True);
            Assert.That(RoomStreamingRules.Wanted(point, rooms, 2, 0, 0f), Is.False);
            Assert.That(RoomStreamingRules.Wanted(point, rooms, 2, 0, 30f), Is.True);
        }

        [Test]
        public void RoomsUnloadOnlyPastTheUnloadRadius()
        {
            var rooms = Chain();
            var point = new Vector3(0f, 0f, 0f);
            Assert.That(RoomStreamingRules.Unwanted(point, rooms, 2, 0, 35f), Is.False);
            Assert.That(RoomStreamingRules.Unwanted(point, rooms, 3, 0, 35f), Is.True);
            Assert.That(RoomStreamingRules.Unwanted(point, rooms, 1, 0, 0f), Is.False);
            Assert.That(RoomStreamingRules.Unwanted(point, rooms, 0, 0, 0f), Is.False);
        }
    }
}
