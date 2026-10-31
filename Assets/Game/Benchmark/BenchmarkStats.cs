using System;

namespace TentacleVania.Benchmark
{
    public readonly struct BenchmarkStats
    {
        public const float FrameBudgetMs = 1000f / 60f;

        public readonly int Frames;
        public readonly float AverageMs;
        public readonly float P50Ms;
        public readonly float P95Ms;
        public readonly float P99Ms;
        public readonly float MaxMs;
        public readonly int OverBudget;

        private BenchmarkStats(int frames, float average, float p50, float p95, float p99, float max, int overBudget)
        {
            Frames = frames;
            AverageMs = average;
            P50Ms = p50;
            P95Ms = p95;
            P99Ms = p99;
            MaxMs = max;
            OverBudget = overBudget;
        }

        public float AverageFps => AverageMs > 0f ? 1000f / AverageMs : 0f;
        public float OverBudgetPercent => Frames > 0 ? 100f * OverBudget / Frames : 0f;

        public static BenchmarkStats Compute(float[] samples, int count)
        {
            if (samples == null || count <= 0)
                return default;
            var sorted = new float[count];
            Array.Copy(samples, sorted, count);
            Array.Sort(sorted);
            var sum = 0d;
            var over = 0;
            for (var i = 0; i < count; i++)
            {
                sum += sorted[i];
                if (sorted[i] > FrameBudgetMs)
                    over++;
            }
            return new BenchmarkStats(count, (float)(sum / count), Percentile(sorted, 0.5f), Percentile(sorted, 0.95f), Percentile(sorted, 0.99f), sorted[count - 1], over);
        }

        private static float Percentile(float[] sorted, float fraction)
        {
            var index = (int)Math.Ceiling(fraction * sorted.Length - 1e-3) - 1;
            return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
        }
    }
}
