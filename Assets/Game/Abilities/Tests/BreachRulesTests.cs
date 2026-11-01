using System.Collections.Generic;
using NUnit.Framework;
using TentacleVania.Abilities.Breach;
using UnityEngine;

namespace TentacleVania.Abilities.Tests
{
    public sealed class BreachRulesTests
    {
        [Test]
        public void PicksTheNearestWallInFrontWithinReach()
        {
            var points = new List<Vector3> { new(0f, 0f, 2.2f), new(0f, 0f, 1.5f), new(0f, 0f, 3f) };
            Assert.That(BreachRules.Pick(Vector3.zero, Vector3.forward, points, 2.5f, 75f), Is.EqualTo(1));
        }

        [Test]
        public void IgnoresWallsBehindUnlessHuggingThem()
        {
            var behind = new List<Vector3> { new(0f, 0f, -2f) };
            Assert.That(BreachRules.Pick(Vector3.zero, Vector3.forward, behind, 2.5f, 75f), Is.EqualTo(-1));
            var hugging = new List<Vector3> { new(0f, 0f, -0.6f) };
            Assert.That(BreachRules.Pick(Vector3.zero, Vector3.forward, hugging, 2.5f, 75f), Is.EqualTo(0));
        }

        [Test]
        public void IgnoresWallsOutOfReach()
        {
            var far = new List<Vector3> { new(0f, 0f, 2.6f) };
            Assert.That(BreachRules.Pick(Vector3.zero, Vector3.forward, far, 2.5f, 75f), Is.EqualTo(-1));
        }
    }
}
