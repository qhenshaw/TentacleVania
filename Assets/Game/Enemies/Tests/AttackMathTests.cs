using NUnit.Framework;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies.AI;
using TentacleVania.Enemies.Data;
using UnityEngine;

namespace TentacleVania.Enemies.Tests
{
    public class AttackMathTests
    {
        private static EnemyAttack Attack() => new() { WindUp = 0.6f, Active = 0.2f, Recovery = 0.4f, Arc = 120f };

        [TestCase(0f, AttackPhase.WindUp)]
        [TestCase(0.59f, AttackPhase.WindUp)]
        [TestCase(0.6f, AttackPhase.Active)]
        [TestCase(0.79f, AttackPhase.Active)]
        [TestCase(0.81f, AttackPhase.Recovery)]
        [TestCase(1.2f, AttackPhase.Done)]
        public void PhasesFollowTheTimings(float elapsed, AttackPhase expected) =>
            Assert.AreEqual(expected, AttackMath.Phase(Attack(), elapsed));

        [Test]
        public void ArcSweepsSymmetricallyAroundForward()
        {
            var start = AttackMath.ArcDirection(Vector3.forward, 120f, 0f);
            var middle = AttackMath.ArcDirection(Vector3.forward, 120f, 0.5f);
            var end = AttackMath.ArcDirection(Vector3.forward, 120f, 1f);
            Assert.AreEqual(60f, Vector3.Angle(Vector3.forward, start), 1e-3f);
            Assert.AreEqual(0f, Vector3.Angle(Vector3.forward, middle), 1e-3f);
            Assert.AreEqual(60f, Vector3.Angle(Vector3.forward, end), 1e-3f);
            Assert.Less(start.x, 0f);
            Assert.Greater(end.x, 0f);
        }

        [Test]
        public void SweepIsSubdividedSoFastSwingsDontSkipTargets()
        {
            Assert.AreEqual(12, AttackMath.SweepSteps(120f, 0f, 1f));
            Assert.AreEqual(1, AttackMath.SweepSteps(120f, 0.5f, 0.51f));
        }

        [Test]
        public void AreaCheckMatchesTheIndicatorCircle()
        {
            Assert.IsTrue(AttackMath.InsideArea(Vector3.zero, 2.5f, new Vector3(2.4f, 1.5f, 0f)));
            Assert.IsFalse(AttackMath.InsideArea(Vector3.zero, 2.5f, new Vector3(2.6f, 0f, 0f)));
        }

        [Test]
        public void UndodgeableAttacksCarryTheFlag()
        {
            Assert.AreEqual(HitFlags.Undodgeable, new EnemyAttack { Telegraph = Telegraph.Undodgeable }.Flags);
            Assert.AreEqual(HitFlags.None, new EnemyAttack { Telegraph = Telegraph.Dodgeable }.Flags);
        }

        [Test]
        public void ProjectilesUseRangedTokens() =>
            Assert.AreEqual(AttackTokenKind.Ranged, new EnemyAttack { Kind = AttackKind.Projectile }.TokenKind);
    }
}
