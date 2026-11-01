using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Bosses.Seeper
{
    public static class SeeperRules
    {
        public static int NearestVent(IReadOnlyList<Vector3> vents, Vector3 target, int exclude)
        {
            var best = -1;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < vents.Count; i++)
            {
                if (i == exclude && vents.Count > 1)
                    continue;
                var offset = vents[i] - target;
                offset.y = 0f;
                var distance = offset.sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }

        public static int FarthestVent(IReadOnlyList<Vector3> vents, Vector3 from, int exclude)
        {
            var best = -1;
            var bestDistance = -1f;
            for (var i = 0; i < vents.Count; i++)
            {
                if (i == exclude)
                    continue;
                var offset = vents[i] - from;
                offset.y = 0f;
                var distance = offset.sqrMagnitude;
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }

        public static void Ring(Vector3 center, float radius, int count, List<Vector3> into)
        {
            into.Clear();
            for (var i = 0; i < count; i++)
            {
                var angle = i * Mathf.PI * 2f / count;
                into.Add(center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius);
            }
        }
    }
}
