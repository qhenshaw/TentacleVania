using UnityEngine;

namespace TentacleVania.Bosses.Automaton
{
    public static class AutomatonRules
    {
        public static Vector3 AnchorAlong(Vector3 center, float radius, int anchors, Vector3 from, Vector3 direction)
        {
            var best = center + Vector3.forward * radius;
            var bestScore = float.MinValue;
            for (var i = 0; i < anchors; i++)
            {
                var angle = i * 360f / anchors;
                var anchor = center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
                var to = anchor - from;
                to.y = 0f;
                if (to.sqrMagnitude < 1e-4f)
                    continue;
                var score = Vector3.Dot(to.normalized, direction);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = anchor;
                }
            }
            return best;
        }

        public static bool InBlade(Vector3 offset, float angle, float arc, float tableRadius, float radius, int sectors)
        {
            offset.y = 0f;
            var distance = offset.magnitude;
            if (distance <= tableRadius || distance > radius)
                return false;
            var bearing = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            for (var i = 0; i < sectors; i++)
                if (Mathf.Abs(Mathf.DeltaAngle(bearing, angle + i * 360f / sectors)) <= arc * 0.5f)
                    return true;
            return false;
        }
    }
}
