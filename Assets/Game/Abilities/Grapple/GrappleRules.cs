using UnityEngine;

namespace TentacleVania.Abilities.Grapple
{
    public static class GrappleRules
    {
        public static bool TryScore(Vector3 origin, Vector3 aim, Vector3 target, float range, float maxAngle, out float angle)
        {
            var offset = target - origin;
            offset.y = 0f;
            aim.y = 0f;
            angle = 180f;
            var distance = offset.magnitude;
            if (distance > range || distance < 0.5f || aim.sqrMagnitude < 1e-6f)
                return false;
            angle = Vector3.Angle(aim, offset);
            return angle <= maxAngle;
        }

        public static float ZipDuration(float distance, float speed, Vector2 clamp) => Mathf.Clamp(distance / Mathf.Max(0.01f, speed), clamp.x, clamp.y);

        public static Vector3 Arc(Vector3 start, Vector3 end, float t, float height)
        {
            var eased = t * t * (3f - 2f * t);
            return Vector3.Lerp(start, end, eased) + Vector3.up * (Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * height);
        }
    }
}
