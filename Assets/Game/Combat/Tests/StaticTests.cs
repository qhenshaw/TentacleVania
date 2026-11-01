using System.Collections.Generic;
using NUnit.Framework;
using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Combat.Tests
{
    public sealed class StaticTests
    {
        [Test]
        public void FiveHitsFillTheMeterAndReset()
        {
            var meter = new StaticMeter(100f);
            for (var i = 0; i < 4; i++)
                Assert.That(meter.Add(20f), Is.False);
            Assert.That(meter.Normalized, Is.EqualTo(0.8f).Within(1e-4f));
            Assert.That(meter.Add(20f), Is.True);
            Assert.That(meter.IsCharged, Is.False);
            Assert.That(meter.Add(0f), Is.False);
        }

        [Test]
        public void ReleaseReturnsTheFillAndEmpties()
        {
            var meter = new StaticMeter(100f);
            meter.Add(60f);
            Assert.That(meter.Release(), Is.EqualTo(0.6f).Within(1e-4f));
            Assert.That(meter.Value, Is.EqualTo(0f));
            Assert.That(meter.Release(), Is.EqualTo(0f));
        }

        [Test]
        public void ChainPicksTheNearestInRange()
        {
            var candidates = new List<Vector3>
            {
                new(5f, 0f, 0f),
                new(1f, 0f, 0f),
                new(10f, 0f, 0f),
                new(3f, 0f, 0f),
                new(2f, 0f, 0f),
            };
            var result = new int[16];
            var count = ChainRules.PickNearest(Vector3.zero, candidates, 6f, 3, result);
            Assert.That(count, Is.EqualTo(3));
            Assert.That(result[0], Is.EqualTo(1));
            Assert.That(result[1], Is.EqualTo(4));
            Assert.That(result[2], Is.EqualTo(3));
        }

        [Test]
        public void ChainSkipsOutOfRangeAndHandlesFewTargets()
        {
            var candidates = new List<Vector3> { new(7f, 0f, 0f), new(4f, 0f, 0f) };
            var result = new int[16];
            Assert.That(ChainRules.PickNearest(Vector3.zero, candidates, 6f, 3, result), Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(1));
            Assert.That(ChainRules.PickNearest(Vector3.zero, candidates, 6f, 0, result), Is.EqualTo(0));
        }
    }
}
