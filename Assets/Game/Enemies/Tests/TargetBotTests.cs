using NUnit.Framework;
using TentacleVania.Enemies.Types;
using UnityEngine;

namespace TentacleVania.Enemies.Tests
{
    public class TargetBotTests
    {
        [Test]
        public void HeadsToTheFarEndUntilWithinTolerance()
        {
            var heading = 1;
            var target = TargetBotEnemy.RailTarget(Vector3.zero, Vector3.right, 8f, 1f, new Vector3(2f, 0f, 0f), ref heading);
            Assert.AreEqual(1, heading);
            Assert.AreEqual(4f, target.x, 1e-4f);
        }

        [Test]
        public void TurnsBackAtTheEnd()
        {
            var heading = 1;
            var target = TargetBotEnemy.RailTarget(Vector3.zero, Vector3.right, 8f, 1f, new Vector3(3.2f, 0f, 0f), ref heading);
            Assert.AreEqual(-1, heading);
            Assert.AreEqual(-4f, target.x, 1e-4f);
        }
    }
}
