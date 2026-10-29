using NUnit.Framework;
using TentacleVania.Creature.Movement;
using UnityEngine;

namespace TentacleVania.Creature.Tests
{
    public class PlanarInputTests
    {
        private static void AssertClose(Vector3 expected, Vector3 actual) =>
            Assert.Less(Vector3.Distance(expected, actual), 1e-4f, $"expected {expected}, got {actual}");

        [Test]
        public void StickUpFollowsCameraYaw()
        {
            var tilted = Quaternion.Euler(60f, 90f, 0f) * Vector3.forward;
            AssertClose(Vector3.right, PlanarInput.ToWorld(Vector2.up, tilted));
        }

        [Test]
        public void StickRightIsCameraRight() =>
            AssertClose(Vector3.right, PlanarInput.ToWorld(Vector2.right, Quaternion.Euler(60f, 0f, 0f) * Vector3.forward));

        [Test]
        public void StraightDownCameraFallsBackToWorldForward() =>
            AssertClose(Vector3.forward, PlanarInput.ToWorld(Vector2.up, Vector3.down));

        [Test]
        public void PointerRayHitsTheCreaturePlane()
        {
            var ray = new Ray(new Vector3(5f, 10f, 0f), Vector3.down);
            Assert.IsTrue(PlanarInput.TryPointerDirection(ray, Vector3.zero, out var direction));
            AssertClose(Vector3.right, direction);
        }

        [Test]
        public void PointerOnTheCreatureGivesNoDirection()
        {
            var ray = new Ray(new Vector3(0f, 10f, 0f), Vector3.down);
            Assert.IsFalse(PlanarInput.TryPointerDirection(ray, Vector3.zero, out _));
        }
    }
}
