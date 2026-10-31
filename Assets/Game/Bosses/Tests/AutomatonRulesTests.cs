using NUnit.Framework;
using TentacleVania.Bosses.Automaton;
using UnityEngine;

namespace TentacleVania.Bosses.Tests
{
    public sealed class AutomatonRulesTests
    {
        [Test]
        public void ClampPicksTheAnchorAheadOfTheLunge()
        {
            var anchor = AutomatonRules.AnchorAlong(Vector3.zero, 11f, 8, new Vector3(0f, 0f, -5f), Vector3.right);
            Assert.That(anchor.x, Is.GreaterThan(7f));
        }

        [Test]
        public void BladesCoverTwoOppositeSectorsOutsideTheTable()
        {
            Assert.That(AutomatonRules.InBlade(new Vector3(0f, 0f, 6f), 0f, 45f, 2.5f, 11f, 2), Is.True);
            Assert.That(AutomatonRules.InBlade(new Vector3(0f, 0f, -6f), 0f, 45f, 2.5f, 11f, 2), Is.True);
            Assert.That(AutomatonRules.InBlade(new Vector3(6f, 0f, 0f), 0f, 45f, 2.5f, 11f, 2), Is.False);
            Assert.That(AutomatonRules.InBlade(new Vector3(0f, 0f, 2f), 0f, 45f, 2.5f, 11f, 2), Is.False);
            Assert.That(AutomatonRules.InBlade(new Vector3(0f, 0f, 12f), 0f, 45f, 2.5f, 11f, 2), Is.False);
        }
    }
}
