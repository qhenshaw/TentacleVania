using UnityEngine;

namespace TentacleVania.Bosses.Warden
{
    public static class WardenRules
    {
        public static void PickBeams(float hallLength, float width, int count, float creatureX, float random, float[] offsets)
        {
            var half = hallLength * 0.5f - width;
            var spacing = hallLength / (count + 1);
            var shift = (random - 0.5f) * spacing * 0.5f;
            for (var i = 0; i < count; i++)
                offsets[i] = Mathf.Clamp(-hallLength * 0.5f + spacing * (i + 1) + shift, -half, half);
            var nearest = 0;
            for (var i = 1; i < count; i++)
                if (Mathf.Abs(offsets[i] - creatureX) < Mathf.Abs(offsets[nearest] - creatureX))
                    nearest = i;
            offsets[nearest] = Mathf.Clamp(creatureX, -half, half);
        }

        public static bool InBeam(float x, float[] offsets, int count, float width)
        {
            for (var i = 0; i < count; i++)
                if (Mathf.Abs(x - offsets[i]) <= width * 0.5f)
                    return true;
            return false;
        }
    }
}
