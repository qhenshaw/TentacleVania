using UnityEngine;

namespace TentacleVania.Core.Interaction
{
    public enum HoldResult
    {
        Pending,
        Tap,
        Hold,
    }

    public static class HoldRules
    {
        public static float Progress(float elapsed, float duration) => duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

        public static HoldResult Resolve(bool held, float elapsed, float duration)
        {
            if (elapsed >= duration)
                return HoldResult.Hold;
            return held ? HoldResult.Pending : HoldResult.Tap;
        }
    }
}
