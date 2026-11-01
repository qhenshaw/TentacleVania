using NUnit.Framework;
using TentacleVania.Enemies.Types;
using UnityEngine;

namespace TentacleVania.Enemies.Tests
{
    public class MirrorEnemyTests
    {
        [Test]
        public void MirrorsTheTargetThroughTheAnchor()
        {
            var mirrored = MirrorEnemy.Mirror(new Vector3(2f, 0f, 2f), new Vector3(5f, 0.5f, -1f));
            Assert.AreEqual(-1f, mirrored.x, 1e-4f);
            Assert.AreEqual(5f, mirrored.z, 1e-4f);
            Assert.AreEqual(0.5f, mirrored.y, 1e-4f);
        }
    }
}
