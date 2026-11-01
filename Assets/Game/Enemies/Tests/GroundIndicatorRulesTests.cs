using NUnit.Framework;
using TentacleVania.Enemies.AI;
using TentacleVania.Enemies.Data;
using UnityEngine;

namespace TentacleVania.Enemies.Tests
{
    public class GroundIndicatorRulesTests
    {
        [TestCase(AttackKind.Area, Telegraph.Dodgeable, true)]
        [TestCase(AttackKind.Projectile, Telegraph.Dodgeable, true)]
        [TestCase(AttackKind.Melee, Telegraph.Undodgeable, true)]
        [TestCase(AttackKind.Melee, Telegraph.Dodgeable, false)]
        public void AutoShowsAreaProjectileAndRedAttacks(AttackKind kind, Telegraph telegraph, bool expected) =>
            Assert.AreEqual(expected, new EnemyAttack { Kind = kind, Telegraph = telegraph }.ShowsGroundIndicator(false));

        [Test]
        public void OverridesAndForceAllWin()
        {
            var yellowMelee = new EnemyAttack { Kind = AttackKind.Melee, GroundIndicator = IndicatorMode.On };
            var hiddenShot = new EnemyAttack { Kind = AttackKind.Projectile, GroundIndicator = IndicatorMode.Off };
            Assert.IsTrue(yellowMelee.ShowsGroundIndicator(false));
            Assert.IsFalse(hiddenShot.ShowsGroundIndicator(false));
            Assert.IsTrue(hiddenShot.ShowsGroundIndicator(true));
            Assert.IsTrue(new EnemyAttack { Kind = AttackKind.Melee }.ShowsGroundIndicator(true));
        }

        [TestCase(11f, 20f, 5f, 6f)]
        [TestCase(11f, 3f, 5f, 3f)]
        [TestCase(11f, 20f, 15f, 11f)]
        public void LaneEndsAtTheNearestOfRangeWallAndTarget(float range, float wall, float target, float expected) =>
            Assert.AreEqual(expected, AttackMath.LaneLength(range, wall, target, 1f), 1e-4f);

        [Test]
        public void LockStartsWhenTrackingEnds()
        {
            var attack = new EnemyAttack { TrackDuringWindUp = 0.6f };
            Assert.IsFalse(AttackMath.IsLocked(attack, 0.6f));
            Assert.IsTrue(AttackMath.IsLocked(attack, 0.61f));
        }

        [Test]
        public void TrapCirclesAreLockedFromTheStart()
        {
            var trap = new EnemyAttack { Kind = AttackKind.Trap, TrackDuringWindUp = 0.75f };
            Assert.IsTrue(AttackMath.PlacedAtTarget(trap));
            Assert.IsTrue(AttackMath.IsLocked(trap, 0f));
            Assert.IsFalse(AttackMath.PlacedAtTarget(new EnemyAttack { Kind = AttackKind.Area }));
        }

        [Test]
        public void LockedAimKeepsTheLockedHeadingAndPitchesToTheAimHeight()
        {
            var aim = AttackMath.LockedAim(Vector3.forward, new Vector3(0f, 1f, 0f), new Vector3(3f, 0.5f, 4f));
            var flat = new Vector3(aim.x, 0f, aim.z);
            Assert.AreEqual(0f, Vector3.Angle(Vector3.forward, flat), 1e-3f);
            Assert.AreEqual(-0.5f / 5f, aim.y / flat.magnitude, 1e-4f);
        }
    }
}
