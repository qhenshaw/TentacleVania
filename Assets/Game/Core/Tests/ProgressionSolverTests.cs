using NUnit.Framework;
using TentacleVania.Core.Progression;

namespace TentacleVania.Core.Tests
{
    public sealed class ProgressionSolverTests
    {
        private static readonly ProgressionSolver.Gate[] Gates =
        {
            new("A", "B", new[] { "boss:A/Boss" }),
            new("B", "C", new[] { "Key" }),
        };

        [Test]
        public void BossRewardsOpenTheWayInOrder()
        {
            var bosses = new[] { new ProgressionSolver.BossNode("C/Boss", null), new ProgressionSolver.BossNode("B/Boss", new[] { "Key" }), new ProgressionSolver.BossNode("A/Boss", null) };
            var result = ProgressionSolver.Solve("A", new[] { "A", "B", "C" }, Gates, bosses);
            Assert.That(result.Complete, Is.True);
            Assert.That(result.Order, Is.EqualTo(new[] { "A/Boss", "B/Boss", "C/Boss" }));
        }

        [Test]
        public void AMissingKeyLeavesSectorsAndBossesUnreached()
        {
            var bosses = new[] { new ProgressionSolver.BossNode("A/Boss", null), new ProgressionSolver.BossNode("C/Boss", null) };
            var result = ProgressionSolver.Solve("A", new[] { "A", "B", "C" }, Gates, bosses);
            Assert.That(result.Complete, Is.False);
            Assert.That(result.UnreachedSectors, Is.EqualTo(new[] { "C" }));
            Assert.That(result.UndefeatedBosses, Is.EqualTo(new[] { "C/Boss" }));
        }

        [Test]
        public void ArenaRequirementsGateOptionalBosses()
        {
            var bosses = new[] { new ProgressionSolver.BossNode("A/Optional", null, new[] { "Key" }), new ProgressionSolver.BossNode("A/Boss", null), new ProgressionSolver.BossNode("B/Boss", new[] { "Key" }) };
            var result = ProgressionSolver.Solve("A", new[] { "A", "B", "C" }, Gates, bosses);
            Assert.That(result.Order.IndexOf("A/Optional"), Is.GreaterThan(result.Order.IndexOf("B/Boss")));
        }
    }
}
