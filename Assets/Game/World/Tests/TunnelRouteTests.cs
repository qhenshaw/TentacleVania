using System.Collections.Generic;
using NUnit.Framework;
using TentacleVania.World.Map;
using TentacleVania.World.Streaming;
using UnityEngine;

namespace TentacleVania.World.Tests
{
    public sealed class TunnelRouteTests
    {
        private static RoomEntry Room(string scene, float x, params string[] neighbours)
        {
            var room = new RoomEntry { Scene = scene, Center = new Vector3(x, 0f, 0f), Size = new Vector3(10f, 3f, 10f) };
            room.Neighbours.AddRange(neighbours);
            return room;
        }

        [Test]
        public void RouteRunsThroughTheRoomsBetweenTheEnds()
        {
            var rooms = new List<RoomEntry> { Room("A", 0f, "B"), Room("B", 20f, "A", "C"), Room("C", 40f, "B", "D"), Room("D", 60f, "C") };
            var path = new List<Vector3>();
            TunnelRoute.Build(rooms, new Vector3(1f, 0f, 0f), new Vector3(59f, 0f, 0f), path);
            Assert.That(path.Count, Is.EqualTo(3));
            Assert.That(path[0].x, Is.EqualTo(1f));
            Assert.That(path[1].x, Is.EqualTo(20f));
            Assert.That(path[2].x, Is.EqualTo(40f));
        }

        [Test]
        public void SameRoomIsJustTheStart()
        {
            var rooms = new List<RoomEntry> { Room("A", 0f) };
            var path = new List<Vector3>();
            TunnelRoute.Build(rooms, Vector3.zero, new Vector3(2f, 0f, 2f), path);
            Assert.That(path.Count, Is.EqualTo(1));
        }
    }
}
