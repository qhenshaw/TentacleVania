using NUnit.Framework;
using TentacleVania.Weapons.Combo;
using TentacleVania.Weapons.Data;
using UnityEngine;

namespace TentacleVania.Weapons.Tests
{
    public class MoveShapesTests
    {
        [Test]
        public void SweepRunsFromStartToEndAngle()
        {
            var move = new WeaponMove { StartAngle = -60f, EndAngle = 60f };
            var start = MoveShapes.SweepDirection(Vector3.forward, move, 0f);
            var end = MoveShapes.SweepDirection(Vector3.forward, move, 1f);
            Assert.Less(start.x, 0f, "starts on the left");
            Assert.Greater(end.x, 0f, "ends on the right");
            Assert.AreEqual(120f, Vector3.Angle(start, end), 1e-3f);
        }

        [Test]
        public void SpinIsSubdividedFinely() =>
            Assert.AreEqual(36, MoveShapes.SweepSteps(new WeaponMove { StartAngle = -180f, EndAngle = 180f }, 0f, 1f));

        [Test]
        public void AreaRespectsRadiusAndSector()
        {
            var cone = new WeaponMove { Shape = MoveShape.Area, Radius = 3f, SectorAngle = 90f };
            Assert.IsTrue(MoveShapes.InArea(Vector3.zero, Vector3.forward, cone, new Vector3(0.5f, 0f, 2f)));
            Assert.IsFalse(MoveShapes.InArea(Vector3.zero, Vector3.forward, cone, new Vector3(2f, 0f, 0.3f)), "outside the 90 degree sector");
            Assert.IsFalse(MoveShapes.InArea(Vector3.zero, Vector3.forward, cone, new Vector3(0f, 0f, 3.2f)), "beyond the radius");
            var circle = new WeaponMove { Shape = MoveShape.Area, Radius = 3f, SectorAngle = 360f };
            Assert.IsTrue(MoveShapes.InArea(Vector3.zero, Vector3.forward, circle, new Vector3(0f, 0f, -2f)));
        }

        [TestCase(0f, MovePhase.WindUp)]
        [TestCase(0.1f, MovePhase.Strike)]
        [TestCase(0.25f, MovePhase.Recovery)]
        [TestCase(0.5f, MovePhase.Done)]
        public void PhasesFollowTheTimings(float elapsed, MovePhase expected) =>
            Assert.AreEqual(expected, MoveShapes.Phase(new WeaponMove { WindUp = 0.08f, Strike = 0.1f, Recovery = 0.2f }, elapsed));

        [Test]
        public void TipStartsAndEndsNearTheBody()
        {
            var move = new WeaponMove { WindUp = 0.1f, Strike = 0.1f, Recovery = 0.2f, Reach = 2f };
            var start = MoveShapes.Tip(Vector3.zero, Vector3.forward, move, 0f);
            var strike = MoveShapes.Tip(Vector3.zero, Vector3.forward, move, 0.15f);
            var end = MoveShapes.Tip(Vector3.zero, Vector3.forward, move, 0.4f);
            Assert.Less(start.magnitude, 0.5f);
            Assert.AreEqual(2f, strike.magnitude, 1e-3f);
            Assert.Less(end.magnitude, 0.5f);
        }
    }
}
