using NUnit.Framework;
using UnityEngine;

namespace TentacleVania.Camera.Tests
{
    public class FloorFramingTests
    {
        private const float Distance = 16f;
        private const float Pitch = 60f;
        private const float Fov = 35f;
        private const float Aspect = 16f / 9f;

        private static readonly Quaternion Rotation = Quaternion.Euler(Pitch, 0f, 0f);

        private static Vector3 PositionFor(Vector3 focus) => focus - Rotation * Vector3.forward * Distance;

        [Test]
        public void FocusIsWhereTheCameraLooks()
        {
            Assert.IsTrue(FloorFraming.TryVisibleExtents(PositionFor(new Vector3(3f, 0f, 4f)), Rotation, Fov, Aspect, 0f, out var focus, out _, out _));
            Assert.Less(Vector2.Distance(new Vector2(3f, 4f), focus), 1e-3f);
        }

        [Test]
        public void ViewIsAbout18MetresWideAtTheFocus()
        {
            FloorFraming.TryVisibleExtents(PositionFor(Vector3.zero), Rotation, Fov, Aspect, 0f, out _, out var min, out var max);
            var widthAtFocus = 2f * Distance * Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad) * Aspect;
            Assert.AreEqual(18f, widthAtFocus, 0.5f);
            Assert.Greater(max.y, -min.y, "a tilted camera sees further beyond the focus than in front of it");
        }

        [Test]
        public void FocusStaysPutInTheMiddleOfABigRoom()
        {
            var clamped = FloorFraming.ClampFocus(Vector2.zero, new Vector2(-10f, -6f), new Vector2(10f, 9f), new Vector2(-30f, -30f), new Vector2(30f, 30f));
            Assert.AreEqual(Vector2.zero, clamped);
        }

        [Test]
        public void FocusIsPushedInFromTheRoomEdge()
        {
            var clamped = FloorFraming.ClampFocus(new Vector2(25f, 0f), new Vector2(-10f, -6f), new Vector2(10f, 9f), new Vector2(-30f, -30f), new Vector2(30f, 30f));
            Assert.AreEqual(20f, clamped.x, 1e-4f);
        }

        [Test]
        public void RoomSmallerThanTheViewIsCentred()
        {
            var clamped = FloorFraming.ClampFocus(new Vector2(4f, 4f), new Vector2(-10f, -6f), new Vector2(10f, 9f), new Vector2(-6f, -6f), new Vector2(6f, 6f));
            Assert.AreEqual(0f, clamped.x, 1e-4f);
            Assert.AreEqual(-1.5f, clamped.y, 1e-4f);
        }

        [Test]
        public void CameraLookingAboveTheHorizonHasNoFraming() =>
            Assert.IsFalse(FloorFraming.TryVisibleExtents(Vector3.up * 5f, Quaternion.Euler(-10f, 0f, 0f), Fov, Aspect, 0f, out _, out _, out _));
    }
}
