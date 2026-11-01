using System.Collections.Generic;
using NUnit.Framework;
using TentacleVania.Bosses.Seeper;
using UnityEngine;

namespace TentacleVania.Bosses.Tests
{
    public class SeeperRulesTests
    {
        private static List<Vector3> Vents()
        {
            var vents = new List<Vector3>();
            SeeperRules.Ring(Vector3.zero, 5f, 4, vents);
            return vents;
        }

        [Test]
        public void RingPlacesEveryVentOnTheRadius()
        {
            foreach (var vent in Vents())
                Assert.AreEqual(5f, vent.magnitude, 1e-4f);
        }

        [Test]
        public void NearestVentIsUnderTheTarget() =>
            Assert.AreEqual(1, SeeperRules.NearestVent(Vents(), new Vector3(4f, 0f, 0f), -1));

        [Test]
        public void NearestVentSkipsTheCurrentOne() =>
            Assert.AreNotEqual(1, SeeperRules.NearestVent(Vents(), new Vector3(4f, 0f, 0f), 1));

        [Test]
        public void SingleVentIsReusedEvenWhenCurrent() =>
            Assert.AreEqual(0, SeeperRules.NearestVent(new List<Vector3> { Vector3.zero }, Vector3.one, 0));

        [Test]
        public void FarthestVentIsOpposite() =>
            Assert.AreEqual(2, SeeperRules.FarthestVent(Vents(), Vents()[0], 0));
    }
}
