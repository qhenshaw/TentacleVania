using UnityEngine;

namespace TentacleVania.Combat
{
    public sealed class ChargeMeter
    {
        public ChargeMeter(float max) => Max = Mathf.Max(1f, max);

        public float Max { get; private set; }
        public float Current { get; private set; }
        public float GainScale { get; set; } = 1f;
        public bool IsFull => Current >= Max;
        public float Normalized => Current / Max;

        public float Add(float amount)
        {
            if (amount <= 0f)
                return 0f;
            var before = Current;
            Current = Mathf.Min(Max, Current + amount * GainScale);
            return Current - before;
        }

        public bool TrySpend(float cost)
        {
            if (cost < 0f || Current < cost)
                return false;
            Current -= cost;
            return true;
        }

        public void Set(float value) => Current = Mathf.Clamp(value, 0f, Max);

        public void Resize(float max)
        {
            Max = Mathf.Max(1f, max);
            Current = Mathf.Min(Current, Max);
        }
    }
}
