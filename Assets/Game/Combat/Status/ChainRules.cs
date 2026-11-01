using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Combat.Status
{
    public static class ChainRules
    {
        public static int PickNearest(Vector3 origin, IReadOnlyList<Vector3> candidates, float radius, int max, int[] result)
        {
            var count = 0;
            var limit = Mathf.Min(max, result.Length);
            var radiusSq = radius * radius;
            for (var i = 0; i < candidates.Count; i++)
            {
                var distance = (candidates[i] - origin).sqrMagnitude;
                if (distance > radiusSq)
                    continue;
                var slot = count;
                while (slot > 0 && (candidates[result[slot - 1]] - origin).sqrMagnitude > distance)
                {
                    if (slot < limit)
                        result[slot] = result[slot - 1];
                    slot--;
                }
                if (slot >= limit)
                    continue;
                result[slot] = i;
                if (count < limit)
                    count++;
            }
            return count;
        }
    }
}
