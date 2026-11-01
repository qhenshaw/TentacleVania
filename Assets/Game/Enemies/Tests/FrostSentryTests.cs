using NUnit.Framework;
using TentacleVania.Enemies.Types;
using UnityEngine;

namespace TentacleVania.Enemies.Tests
{
    public class FrostSentryTests
    {
        [Test]
        public void WallRisesBetweenCreatureAndSentryAcrossTheLine()
        {
            Assert.IsTrue(FrostSentryEnemy.WallSpot(new Vector3(10f, 0f, 0f), Vector3.zero, 2.5f, 14f, out var position, out var along));
            Assert.AreEqual(2.5f, position.x, 1e-4f);
            Assert.AreEqual(0f, Vector3.Dot(along, Vector3.right), 1e-4f);
        }

        [Test]
        public void NoWallWhenTheCreatureIsOutOfRange() =>
            Assert.IsFalse(FrostSentryEnemy.WallSpot(new Vector3(20f, 0f, 0f), Vector3.zero, 2.5f, 14f, out _, out _));

        [Test]
        public void NoWallWhenTheCreatureIsTooClose() =>
            Assert.IsFalse(FrostSentryEnemy.WallSpot(new Vector3(3f, 0f, 0f), Vector3.zero, 2.5f, 14f, out _, out _));
    }
}
