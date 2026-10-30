using NUnit.Framework;
using TentacleVania.Enemies.AI;
using TentacleVania.Enemies.Data;

namespace TentacleVania.Enemies.Tests
{
    public class AttackTokensTests
    {
        [Test]
        public void MeleeTokensRunOutAtCapacity()
        {
            var tokens = new AttackTokens(2, 2);
            Assert.IsTrue(tokens.TryAcquire("a", AttackTokenKind.Melee));
            Assert.IsTrue(tokens.TryAcquire("b", AttackTokenKind.Melee));
            Assert.IsFalse(tokens.TryAcquire("c", AttackTokenKind.Melee));
        }

        [Test]
        public void MeleeAndRangedPoolsAreSeparate()
        {
            var tokens = new AttackTokens(1, 1);
            Assert.IsTrue(tokens.TryAcquire("a", AttackTokenKind.Melee));
            Assert.IsTrue(tokens.TryAcquire("b", AttackTokenKind.Ranged));
            Assert.IsFalse(tokens.TryAcquire("c", AttackTokenKind.Ranged));
        }

        [Test]
        public void HolderReacquiringDoesNotTakeASecondToken()
        {
            var tokens = new AttackTokens(2, 2);
            tokens.TryAcquire("a", AttackTokenKind.Melee);
            Assert.IsTrue(tokens.TryAcquire("a", AttackTokenKind.Melee));
            Assert.AreEqual(1, tokens.MeleeHeld);
        }

        [Test]
        public void ReleasingFreesTheToken()
        {
            var tokens = new AttackTokens(1, 1);
            tokens.TryAcquire("a", AttackTokenKind.Melee);
            tokens.Release("a");
            Assert.IsFalse(tokens.Holds("a"));
            Assert.IsTrue(tokens.TryAcquire("b", AttackTokenKind.Melee));
        }
    }
}
