using UnityEngine;

namespace TentacleVania.Core.Rendering
{
    public static class DynamicResolutionRules
    {
        public const float MinScale = 0.7f;
        public const float MaxScale = 1f;
        public const float Step = 0.05f;
        public const float BudgetMs = 1000f / 60f;
        public const float DropAboveMs = BudgetMs * 0.93f;
        public const float RaiseBelowMs = BudgetMs * 0.75f;

        public static float Next(float scale, float gpuMs, float calmSeconds, float raiseAfter)
        {
            if (gpuMs <= 0f)
                return scale;
            if (gpuMs > DropAboveMs)
                return Mathf.Max(MinScale, Round(scale - Step));
            if (gpuMs < RaiseBelowMs && calmSeconds >= raiseAfter)
                return Mathf.Min(MaxScale, Round(scale + Step));
            return scale;
        }

        private static float Round(float scale) => Mathf.Round(scale / Step) * Step;
    }
}
