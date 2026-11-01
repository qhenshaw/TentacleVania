using UnityEngine;

namespace TentacleVania.Combat.Status
{
    public sealed class StaticMeter
    {
        public StaticMeter(float capacity) => Capacity = Mathf.Max(0.01f, capacity);

        public float Capacity { get; }
        public float Value { get; private set; }
        public float Normalized => Value / Capacity;
        public bool IsCharged => Value > 0f;

        public bool Add(float amount)
        {
            if (amount <= 0f)
                return false;
            Value += amount;
            if (Value < Capacity)
                return false;
            Value = 0f;
            return true;
        }

        public float Release()
        {
            var fraction = Mathf.Clamp01(Normalized);
            Value = 0f;
            return fraction;
        }

        public void Clear() => Value = 0f;
    }
}
