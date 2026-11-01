using NUnit.Framework;
using TentacleVania.Enemies.Traps;
using UnityEngine;

namespace TentacleVania.Enemies.Tests
{
    public class IceWallTests
    {
        [Test]
        public void DistanceIsZeroOnTheWall() =>
            Assert.AreEqual(0f, IceWall.Distance(Vector3.zero, Vector3.forward, 6f, 1f, new Vector3(0.3f, 0f, 2f)), 1e-4f);

        [Test]
        public void DistanceIsMeasuredFromTheFaceAcrossTheWall() =>
            Assert.AreEqual(1.5f, IceWall.Distance(Vector3.zero, Vector3.forward, 6f, 1f, new Vector3(2f, 0f, 0f)), 1e-4f);

        [Test]
        public void DistanceIsMeasuredFromTheEndPastTheWall() =>
            Assert.AreEqual(1.5f, IceWall.Distance(Vector3.zero, Vector3.forward, 6f, 1f, new Vector3(0f, 0f, 5f)), 1e-4f);
    }
}
