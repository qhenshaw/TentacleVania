using System;

namespace TentacleVania.Creature.Rig
{
    public static class LegGait
    {
        public static int MaxLifted(int legCount) => Math.Max(1, legCount / 2);

        public static int SelectSteps(ReadOnlySpan<float> error, ReadOnlySpan<bool> stepping, Span<bool> start, float threshold, float emergency)
        {
            var count = error.Length;
            Span<bool> considered = stackalloc bool[count];
            var lifted = 0;
            for (var i = 0; i < count; i++)
            {
                start[i] = false;
                considered[i] = stepping[i];
                if (stepping[i])
                    lifted++;
            }

            var maxLifted = MaxLifted(count);
            var started = 0;
            while (true)
            {
                var best = -1;
                for (var i = 0; i < count; i++)
                {
                    if (!considered[i] && error[i] > threshold && (best < 0 || error[i] > error[best]))
                        best = i;
                }
                if (best < 0)
                    return started;

                considered[best] = true;
                var urgent = error[best] > emergency;
                var previous = (best + count - 1) % count;
                var next = (best + 1) % count;
                var neighborBusy = stepping[previous] || start[previous] || stepping[next] || start[next];
                if (!urgent && (neighborBusy || lifted >= maxLifted))
                    continue;

                start[best] = true;
                lifted++;
                started++;
            }
        }
    }
}
