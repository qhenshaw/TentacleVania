using NUnit.Framework;
using TentacleVania.Enemies.Traps;
using UnityEngine;

namespace TentacleVania.Enemies.Tests
{
    public class FireZoneTests
    {
        [Test]
        public void CircleCoversEverythingWithinItsRadius()
        {
            Assert.IsTrue(FireZone.Inside(Vector3.zero, Vector3.forward, 2f, 360f, new Vector3(-1.5f, 0f, -1f)));
            Assert.IsFalse(FireZone.Inside(Vector3.zero, Vector3.forward, 2f, 360f, new Vector3(2f, 0f, 1f)));
        }

        [Test]
        public void WedgeCoversOnlyItsArc()
        {
            Assert.IsTrue(FireZone.Inside(Vector3.zero, Vector3.forward, 14f, 45f, new Vector3(1f, 0f, 8f)));
            Assert.IsFalse(FireZone.Inside(Vector3.zero, Vector3.forward, 14f, 45f, new Vector3(6f, 0f, 6f)));
        }
    }
}
