using NUnit.Framework;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Feedback;

namespace TentacleVania.Combat.Tests
{
    public class HitFeedbackRulesTests
    {
        [Test]
        public void PlayerHitsAreClassifiedByTheirStrongestFlag()
        {
            Assert.AreEqual(HitFeedbackKind.Light, HitFeedbackRules.Classify(HitFlags.None, true, false));
            Assert.AreEqual(HitFeedbackKind.Heavy, HitFeedbackRules.Classify(HitFlags.Heavy | HitFlags.Finisher, true, false));
            Assert.AreEqual(HitFeedbackKind.Finisher, HitFeedbackRules.Classify(HitFlags.Finisher, true, false));
            Assert.AreEqual(HitFeedbackKind.DashAttack, HitFeedbackRules.Classify(HitFlags.DashAttack, true, false));
        }

        [Test]
        public void HitsOnThePlayerAreCreatureHurt()
        {
            Assert.AreEqual(HitFeedbackKind.CreatureHurt, HitFeedbackRules.Classify(HitFlags.None, false, true));
            Assert.AreEqual(HitFeedbackKind.CreatureHurt, HitFeedbackRules.Classify(HitFlags.Hazard, false, true));
        }

        [Test]
        public void HitsThatDoNotInvolveThePlayerGetNoFeedback()
        {
            Assert.AreEqual(HitFeedbackKind.None, HitFeedbackRules.Classify(HitFlags.Heavy, false, false));
            Assert.AreEqual(HitFeedbackKind.None, HitFeedbackRules.Classify(HitFlags.Hazard, true, false));
        }

        [Test]
        public void StaggerBreakOutranksEveryHit()
        {
            foreach (var kind in new[] { HitFeedbackKind.Light, HitFeedbackKind.Heavy, HitFeedbackKind.Finisher, HitFeedbackKind.DashAttack, HitFeedbackKind.CreatureHurt })
                Assert.IsTrue(HitFeedbackRules.Outranks(HitFeedbackKind.StaggerBreak, kind), kind.ToString());
            Assert.IsFalse(HitFeedbackRules.Outranks(HitFeedbackKind.Light, HitFeedbackKind.Heavy));
            Assert.IsFalse(HitFeedbackRules.Outranks(HitFeedbackKind.None, HitFeedbackKind.None));
        }
    }
}
