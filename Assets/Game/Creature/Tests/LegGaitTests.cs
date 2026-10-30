using NUnit.Framework;
using TentacleVania.Creature.Rig;

namespace TentacleVania.Creature.Tests
{
    public class LegGaitTests
    {
        private const float Threshold = 0.28f;
        private const float Emergency = 0.6f;

        private static bool[] Select(float[] error, bool[] stepping)
        {
            var start = new bool[error.Length];
            LegGait.SelectSteps(error, stepping, start, Threshold, Emergency);
            return start;
        }

        [Test]
        public void LegsUnderTheThresholdStayPlanted()
        {
            var start = Select(new[] { 0.1f, 0.2f, 0.27f, 0f, 0.05f, 0.1f }, new bool[6]);
            CollectionAssert.DoesNotContain(start, true);
        }

        [Test]
        public void NeighborOfASteppingLegWaits()
        {
            var stepping = new bool[6];
            stepping[1] = true;
            var start = Select(new[] { 0.4f, 0f, 0.4f, 0f, 0f, 0f }, stepping);
            Assert.IsFalse(start[0]);
            Assert.IsFalse(start[2]);
        }

        [Test]
        public void NeighborRuleWrapsAroundTheRing()
        {
            var stepping = new bool[6];
            stepping[5] = true;
            Assert.IsFalse(Select(new[] { 0.4f, 0f, 0f, 0f, 0f, 0f }, stepping)[0]);
        }

        [Test]
        public void AllLegsNeedingStepsGiveAlternatingSet()
        {
            var start = Select(new[] { 0.5f, 0.45f, 0.5f, 0.45f, 0.5f, 0.45f }, new bool[6]);
            CollectionAssert.AreEqual(new[] { true, false, true, false, true, false }, start);
        }

        [Test]
        public void LargestErrorStepsFirst()
        {
            var start = Select(new[] { 0.3f, 0.5f, 0.3f, 0f, 0f, 0f }, new bool[6]);
            Assert.IsTrue(start[1]);
            Assert.IsFalse(start[0]);
            Assert.IsFalse(start[2]);
        }

        [Test]
        public void AtMostHalfTheLegsAreLifted()
        {
            var stepping = new[] { true, false, true, false, false, false };
            var start = Select(new[] { 0f, 0f, 0f, 0f, 0.4f, 0f }, stepping);
            Assert.IsTrue(start[4]);
            stepping = new[] { true, false, true, false, true, false };
            start = Select(new[] { 0f, 0.4f, 0f, 0.4f, 0f, 0.4f }, stepping);
            CollectionAssert.DoesNotContain(start, true);
        }

        [Test]
        public void OverstretchedLegStepsDespiteTheRules()
        {
            var stepping = new[] { true, false, true, false, true, false };
            var start = Select(new[] { 0f, 0.7f, 0f, 0f, 0f, 0f }, stepping);
            Assert.IsTrue(start[1]);
        }
    }
}
