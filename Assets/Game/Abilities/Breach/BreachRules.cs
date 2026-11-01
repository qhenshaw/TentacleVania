using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Abilities.Breach
{
    public static class BreachRules
    {
        public const float CloseEnough = 0.9f;

        public static int Pick(Vector3 position, Vector3 facing, IReadOnlyList<Vector3> closestPoints, float range, float maxAngle)
        {
            facing.y = 0f;
            var best = -1;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < closestPoints.Count; i++)
            {
                var offset = closestPoints[i] - position;
                offset.y = 0f;
                var distance = offset.magnitude;
                if (distance > range)
                    continue;
                if (distance > CloseEnough && facing.sqrMagnitude > 1e-6f && Vector3.Angle(facing, offset) > maxAngle)
                    continue;
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                best = i;
            }
            return best;
        }
    }
}
