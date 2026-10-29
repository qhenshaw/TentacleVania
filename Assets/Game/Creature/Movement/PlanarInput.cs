using UnityEngine;

namespace TentacleVania.Creature.Movement
{
    public static class PlanarInput
    {
        public static Vector3 ToWorld(Vector2 input, Vector3 cameraForward)
        {
            var forward = new Vector3(cameraForward.x, 0f, cameraForward.z);
            if (forward.sqrMagnitude < 1e-6f)
                forward = Vector3.forward;
            forward.Normalize();
            var right = new Vector3(forward.z, 0f, -forward.x);
            return forward * input.y + right * input.x;
        }

        public static bool TryPointerDirection(Ray ray, Vector3 origin, out Vector3 direction)
        {
            direction = default;
            if (Mathf.Abs(ray.direction.y) < 1e-5f)
                return false;
            var distance = (origin.y - ray.origin.y) / ray.direction.y;
            if (distance <= 0f)
                return false;
            var toPoint = ray.GetPoint(distance) - origin;
            toPoint.y = 0f;
            if (toPoint.sqrMagnitude < 0.01f)
                return false;
            direction = toPoint.normalized;
            return true;
        }
    }
}
