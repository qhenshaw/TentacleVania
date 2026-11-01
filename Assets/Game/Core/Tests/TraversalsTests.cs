using NUnit.Framework;
using TentacleVania.Core.Interaction;
using UnityEngine;

namespace TentacleVania.Core.Tests
{
    public sealed class TraversalsTests
    {
        private sealed class Spot : ITraversal
        {
            public string AbilityId { get; set; } = "Burrow";
            public Vector3 Position { get; set; }
            public float Radius { get; set; } = 1.4f;
            public bool CanTraverse { get; set; } = true;
            public int Used { get; private set; }
            public void Traverse() => Used++;
        }

        [Test]
        public void NearestSpotInReachWithAnUnlockedAbilityWins()
        {
            var near = new Spot { Position = new Vector3(1f, 0f, 0f) };
            var nearer = new Spot { Position = new Vector3(0.5f, 0f, 0f), AbilityId = "VentSqueeze" };
            var far = new Spot { Position = new Vector3(3f, 0f, 0f) };
            Traversals.Register(near);
            Traversals.Register(nearer);
            Traversals.Register(far);
            try
            {
                Assert.That(Traversals.Nearest(Vector3.zero, id => true), Is.SameAs(nearer));
                Assert.That(Traversals.Nearest(Vector3.zero, id => id == "Burrow"), Is.SameAs(near));
                nearer.CanTraverse = false;
                Assert.That(Traversals.Nearest(Vector3.zero, null), Is.SameAs(near));
                Assert.That(Traversals.Nearest(new Vector3(10f, 0f, 0f), null), Is.Null);
            }
            finally
            {
                Traversals.Unregister(near);
                Traversals.Unregister(nearer);
                Traversals.Unregister(far);
            }
        }
    }
}
