using NUnit.Framework;
using TentacleVania.Abilities.SpineVolley;
using UnityEngine;

namespace TentacleVania.Abilities.Tests
{
    public class SpineVolleyTests
    {
        [Test]
        public void FanSpansTheSpreadSymmetrically()
        {
            var left = SpineVolleyDefinition.FanDirection(Vector3.forward, 30f, 0, 5);
            var middle = SpineVolleyDefinition.FanDirection(Vector3.forward, 30f, 2, 5);
            var right = SpineVolleyDefinition.FanDirection(Vector3.forward, 30f, 4, 5);
            Assert.AreEqual(15f, Vector3.Angle(Vector3.forward, left), 1e-3f);
            Assert.AreEqual(0f, Vector3.Angle(Vector3.forward, middle), 1e-3f);
            Assert.AreEqual(15f, Vector3.Angle(Vector3.forward, right), 1e-3f);
            Assert.Less(left.x, 0f);
        }

        [Test]
        public void SingleSpineFliesStraight() =>
            Assert.AreEqual(Vector3.forward, SpineVolleyDefinition.FanDirection(Vector3.forward, 30f, 0, 1));
    }
}
