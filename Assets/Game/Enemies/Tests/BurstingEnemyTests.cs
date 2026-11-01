using NUnit.Framework;
using TentacleVania.Enemies.Types;
using UnityEngine;

namespace TentacleVania.Enemies.Tests
{
    public class BurstingEnemyTests
    {
        [Test]
        public void ZeroRangeNeverTriggers() =>
            Assert.IsFalse(BurstingEnemy.InTrigger(Vector3.zero, Vector3.zero, 0f));

        [Test]
        public void TriggersInsideRangeOnTheGroundPlane() =>
            Assert.IsTrue(BurstingEnemy.InTrigger(Vector3.zero, new Vector3(1.5f, 3f, 1.5f), 2.2f));

        [Test]
        public void DoesNotTriggerOutsideRange() =>
            Assert.IsFalse(BurstingEnemy.InTrigger(Vector3.zero, new Vector3(2f, 0f, 1.5f), 2.2f));
    }
}
