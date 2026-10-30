using UnityEngine;

namespace TentacleVania.World.Layout
{
    public static class LayoutRandom
    {
        public static uint Hash(int seed, int a, int b = 0, int c = 0, int d = 0)
        {
            unchecked
            {
                var h = 2166136261u ^ (uint)seed;
                h = Mix(h, a);
                h = Mix(h, b);
                h = Mix(h, c);
                h = Mix(h, d);
                h ^= h >> 15;
                h *= 0x2c1b3c6du;
                h ^= h >> 12;
                return h;
            }
        }

        public static float Value(uint hash) => (hash & 0xFFFFFF) / (float)0x1000000;

        public static int Pick(TileSlot slot, uint hash)
        {
            var total = 0f;
            foreach (var entry in slot.Entries)
                if (entry.Prefab != null)
                    total += entry.Weight;
            if (total <= 0f)
                return -1;
            var roll = Value(hash) * total;
            for (var i = 0; i < slot.Entries.Count; i++)
            {
                var entry = slot.Entries[i];
                if (entry.Prefab == null || entry.Weight <= 0f)
                    continue;
                roll -= entry.Weight;
                if (roll < 0f)
                    return i;
            }
            for (var i = slot.Entries.Count - 1; i >= 0; i--)
                if (slot.Entries[i].Prefab != null && slot.Entries[i].Weight > 0f)
                    return i;
            return -1;
        }

        private static uint Mix(uint h, int value)
        {
            unchecked
            {
                h ^= (uint)value;
                h *= 16777619u;
                return h;
            }
        }
    }
}
