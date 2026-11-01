using System;
using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Core.Interaction
{
    public static class Traversals
    {
        private static readonly List<ITraversal> Registered = new(16);
        private static int _claimedFrame = -1;

        public static bool ClaimedThisFrame => _claimedFrame == Time.frameCount;
        public static IReadOnlyList<ITraversal> All => Registered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Registered.Clear();
            _claimedFrame = -1;
        }

        public static void Claim() => _claimedFrame = Time.frameCount;

        public static void Register(ITraversal traversal)
        {
            if (traversal != null && !Registered.Contains(traversal))
                Registered.Add(traversal);
        }

        public static void Unregister(ITraversal traversal) => Registered.Remove(traversal);

        public static ITraversal Nearest(Vector3 position, Func<string, bool> unlocked)
        {
            ITraversal best = null;
            var bestDistance = float.MaxValue;
            foreach (var traversal in Registered)
            {
                if (!traversal.CanTraverse || (unlocked != null && !unlocked(traversal.AbilityId)))
                    continue;
                var offset = traversal.Position - position;
                offset.y = 0f;
                var distance = offset.sqrMagnitude;
                if (distance > traversal.Radius * traversal.Radius || distance >= bestDistance)
                    continue;
                bestDistance = distance;
                best = traversal;
            }
            return best;
        }
    }
}
