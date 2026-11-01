using NUnit.Framework;
using TentacleVania.Enemies.Types;
using UnityEngine;

namespace TentacleVania.Enemies.Tests
{
    public class OfficerTests
    {
        [Test]
        public void RalliesAlliesWithinRangeOnTheGroundPlane()
        {
            Assert.IsTrue(OfficerEnemy.Rallies(Vector3.zero, new Vector3(6f, 3f, 7f), 10f));
            Assert.IsFalse(OfficerEnemy.Rallies(Vector3.zero, new Vector3(8f, 0f, 7f), 10f));
        }

        [Test]
        public void RallyBoostsOutgoingDamage() => Assert.Greater(OfficerEnemy.RallyModifier.HealthScale, 1f);
    }
}
