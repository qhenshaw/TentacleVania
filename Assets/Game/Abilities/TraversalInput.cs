using UnityEngine;

namespace TentacleVania.Abilities
{
    public static class TraversalInput
    {
        private static int _claimedFrame = -1;

        public static bool ClaimedThisFrame => _claimedFrame == Time.frameCount;

        public static void Claim() => _claimedFrame = Time.frameCount;
    }
}
