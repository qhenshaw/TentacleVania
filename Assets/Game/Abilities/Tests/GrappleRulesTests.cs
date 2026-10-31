using NUnit.Framework;
using TentacleVania.Abilities.Grapple;
using UnityEngine;

namespace TentacleVania.Abilities.Tests
{
    public sealed class GrappleRulesTests
    {
        [Test]
        public void TargetsMustBeInRangeAndNearTheAim()
        {
            Assert.That(GrappleRules.TryScore(Vector3.zero, Vector3.forward, new Vector3(0f, 2f, 8f), 10f, 70f, out var angle), Is.True);
            Assert.That(angle, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(GrappleRules.TryScore(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 11f), 10f, 70f, out _), Is.False);
            Assert.That(GrappleRules.TryScore(Vector3.zero, Vector3.forward, new Vector3(5f, 0f, -1f), 10f, 70f, out _), Is.False);
        }

        [Test]
        public void ZipDurationStaysInTheIFrameWindow()
        {
            Assert.That(GrappleRules.ZipDuration(2f, 28f, new Vector2(0.2f, 0.4f)), Is.EqualTo(0.2f));
            Assert.That(GrappleRules.ZipDuration(28f * 0.3f, 28f, new Vector2(0.2f, 0.4f)), Is.EqualTo(0.3f).Within(1e-4f));
            Assert.That(GrappleRules.ZipDuration(50f, 28f, new Vector2(0.2f, 0.4f)), Is.EqualTo(0.4f));
        }

        [Test]
        public void ArcStartsAndEndsOnThePoints()
        {
            Assert.That(GrappleRules.Arc(Vector3.zero, Vector3.forward * 6f, 0f, 2f), Is.EqualTo(Vector3.zero));
            Assert.That(Vector3.Distance(GrappleRules.Arc(Vector3.zero, Vector3.forward * 6f, 1f, 2f), Vector3.forward * 6f), Is.LessThan(1e-4f));
            Assert.That(GrappleRules.Arc(Vector3.zero, Vector3.forward * 6f, 0.5f, 2f).y, Is.EqualTo(2f).Within(1e-4f));
        }
    }
}
