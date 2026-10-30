using UnityEngine;

namespace TentacleVania.Camera
{
    public static class FloorFraming
    {
        public static bool TryVisibleExtents(Vector3 position, Quaternion rotation, float verticalFov, float aspect, float floorHeight,
            out Vector2 focus, out Vector2 min, out Vector2 max)
        {
            focus = min = max = default;
            if (!TryHitFloor(position, rotation * Vector3.forward, floorHeight, out var center))
                return false;
            focus = center;

            var tanY = Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad);
            var tanX = tanY * aspect;
            min = new Vector2(float.MaxValue, float.MaxValue);
            max = new Vector2(float.MinValue, float.MinValue);
            for (var corner = 0; corner < 4; corner++)
            {
                var local = new Vector3((corner & 1) == 0 ? -tanX : tanX, (corner & 2) == 0 ? -tanY : tanY, 1f);
                if (!TryHitFloor(position, rotation * local, floorHeight, out var hit))
                    return false;
                var offset = hit - center;
                min = Vector2.Min(min, offset);
                max = Vector2.Max(max, offset);
            }
            return true;
        }

        public static Vector2 ClampFocus(Vector2 focus, Vector2 viewMin, Vector2 viewMax, Vector2 roomMin, Vector2 roomMax)
        {
            return new(ClampAxis(focus.x, viewMin.x, viewMax.x, roomMin.x, roomMax.x), ClampAxis(focus.y, viewMin.y, viewMax.y, roomMin.y, roomMax.y));
        }

        private static float ClampAxis(float focus, float viewMin, float viewMax, float roomMin, float roomMax)
        {
            var low = roomMin - viewMin;
            var high = roomMax - viewMax;
            return low > high ? (low + high) * 0.5f : Mathf.Clamp(focus, low, high);
        }

        private static bool TryHitFloor(Vector3 origin, Vector3 direction, float floorHeight, out Vector2 hit)
        {
            hit = default;
            if (direction.y >= -1e-4f)
                return false;
            var distance = (floorHeight - origin.y) / direction.y;
            var point = origin + direction * distance;
            hit = new Vector2(point.x, point.z);
            return true;
        }
    }
}
