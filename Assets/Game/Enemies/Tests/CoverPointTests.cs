using NUnit.Framework;
using TentacleVania.Enemies.AI;
using UnityEngine;

namespace TentacleVania.Enemies.Tests
{
    public class CoverPointTests
    {
        private static readonly Vector2 Range = new(3f, 8f);

        [Test]
        public void CoversATargetInFrontWithinRange() =>
            Assert.IsTrue(CoverPoint.Covers(Vector3.zero, Vector3.forward, new Vector3(1f, 0f, 5f), Range, 0.3f));

        [Test]
        public void DoesNotCoverATargetBehind() =>
            Assert.IsFalse(CoverPoint.Covers(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -5f), Range, 0.3f));

        [Test]
        public void DoesNotCoverATargetTooCloseOrTooFar()
        {
            Assert.IsFalse(CoverPoint.Covers(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 2f), Range, 0.3f));
            Assert.IsFalse(CoverPoint.Covers(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 9f), Range, 0.3f));
        }
    }
}
