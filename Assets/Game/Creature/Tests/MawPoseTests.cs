using NUnit.Framework;
using TentacleVania.Creature.Rig;
using UnityEngine;

namespace TentacleVania.Creature.Tests
{
    public class MawPoseTests
    {
        private const int Count = 8;
        private const float Tolerance = 1e-3f;

        private static readonly Vector3 Mouth = new(0f, 0.45f, 0.35f);
        private static readonly Vector3 Target = new(0f, 0.9f, 1.6f);

        [Test]
        public void RootsLieOnTheRootCircleFacingForward()
        {
            var facing = Quaternion.identity;
            for (var i = 0; i < Count; i++)
            {
                var root = MawPose.Root(MawPose.Angle(i, Count) + 13f, Mouth, facing, 0.25f);
                Assert.That(Vector3.Distance(root, Mouth), Is.EqualTo(0.25f).Within(Tolerance));
                Assert.That(root.z, Is.EqualTo(Mouth.z).Within(Tolerance));
            }
        }

        [Test]
        public void OpenTipsRingTheAxisAtTheOpenRadius()
        {
            var axis = (Target - Mouth).normalized;
            var centre = Mouth + (Target - Mouth) * 0.5f;
            for (var i = 0; i < Count; i++)
            {
                var tip = MawPose.OpenTip(MawPose.Angle(i, Count), Mouth, Target, 1.2f, 0.5f);
                var offset = tip - centre;
                Assert.That(Vector3.Dot(offset, axis), Is.EqualTo(0f).Within(Tolerance));
                Assert.That(offset.magnitude, Is.EqualTo(1.2f).Within(Tolerance));
            }
        }

        [Test]
        public void CloseTipsConvergeBeyondTheTarget()
        {
            var axis = (Target - Mouth).normalized;
            var closePoint = Target + axis * 0.4f;
            for (var i = 0; i < Count; i++)
            {
                var tip = MawPose.CloseTip(MawPose.Angle(i, Count), Mouth, Target, 0.4f, 0.05f);
                Assert.That(Vector3.Distance(tip, closePoint), Is.EqualTo(0.05f).Within(Tolerance));
            }
        }

        [Test]
        public void PinTipsHugTheTargetSide()
        {
            var axis = (Target - Mouth).normalized;
            for (var i = 0; i < Count; i++)
            {
                var offset = MawPose.PinTip(MawPose.Angle(i, Count), Mouth, Target, 0.4f, 0.9f) - Target;
                Assert.That(offset.magnitude, Is.EqualTo(0.36f).Within(Tolerance));
                Assert.That(Vector3.Dot(offset, axis), Is.EqualTo(0f).Within(Tolerance));
            }
        }

        [Test]
        public void EaseOutBackStartsAndEndsOnTargetAndOvershoots()
        {
            Assert.That(MawPose.EaseOutBack(0f, 1.7f), Is.EqualTo(0f).Within(Tolerance));
            Assert.That(MawPose.EaseOutBack(1f, 1.7f), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(MawPose.EaseOutBack(0.7f, 1.7f), Is.GreaterThan(1f));
            Assert.That(MawPose.EaseOutBack(0.7f, 0f), Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void StaggerWaitsForItsDelayThenStillFinishesOnTime()
        {
            Assert.That(MawPose.Stagger(0.2f, 0.4f), Is.EqualTo(0f));
            Assert.That(MawPose.Stagger(0.7f, 0.4f), Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(MawPose.Stagger(1f, 0.4f), Is.EqualTo(1f));
            Assert.That(MawPose.Stagger(0.5f, 0f), Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void TimelineRunsThePhasesInOrderThenHides()
        {
            var timeline = new MawTimeline(0.3f, 0.2f, 0.4f, 0.2f);
            Assert.That(timeline.Evaluate(0.1f, out var t), Is.EqualTo(MawPhase.Open));
            Assert.That(t, Is.EqualTo(1f / 3f).Within(Tolerance));
            Assert.That(timeline.Evaluate(0.4f, out t), Is.EqualTo(MawPhase.Close));
            Assert.That(t, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(timeline.Evaluate(0.6f, out _), Is.EqualTo(MawPhase.Swallow));
            Assert.That(timeline.Evaluate(1f, out _), Is.EqualTo(MawPhase.Retract));
            Assert.That(timeline.Evaluate(1.2f, out _), Is.EqualTo(MawPhase.Hidden));
            Assert.That(timeline.Total, Is.EqualTo(1.1f).Within(Tolerance));
        }

        [Test]
        public void ArcLengthCoversTheArch()
        {
            Assert.That(MawPose.ArcLength(2f, 0f), Is.EqualTo(2f).Within(Tolerance));
            Assert.That(MawPose.ArcLength(2f, 1f), Is.GreaterThan(2f));
        }
    }
}
