using UnityEngine;

namespace TentacleVania.Core.Rendering
{
    public static class LowHealthRules
    {
        public const float LightBelow = 0.4f;
        public const float HeavyBelow = 0.15f;
        public const float LightAmount = 0.5f;
        public const float HeavyAmount = 1f;

        public static float Target(float normalizedHealth, bool alive) =>
            !alive ? 0f : normalizedHealth <= HeavyBelow ? HeavyAmount : normalizedHealth <= LightBelow ? LightAmount : 0f;

        public static float Heartbeat(float time, float beatsPerMinute)
        {
            var phase = Mathf.Repeat(time * beatsPerMinute / 60f, 1f);
            var lub = Mathf.Exp(-Sq((phase - 0.05f) / 0.05f));
            var dub = 0.6f * Mathf.Exp(-Sq((phase - 0.25f) / 0.05f));
            return Mathf.Clamp01(lub + dub);
        }

        public static float Lurch(float sinceConsume, float duration) =>
            sinceConsume < 0f || sinceConsume >= duration ? 0f : 1f - Mathf.SmoothStep(0f, 1f, sinceConsume / duration);

        private static float Sq(float x) => x * x;
    }
}
