using UnityEngine;

namespace TentacleVania.Enemies.AI
{
    public sealed class StaggerBar
    {
        public StaggerBar(float max) => Max = Mathf.Max(1f, max);

        public float Max { get; }
        public float Current { get; private set; }
        public float Normalized => Current / Max;

        public bool Add(float amount)
        {
            if (amount <= 0f)
                return false;
            Current = Mathf.Min(Max, Current + amount);
            if (Current < Max)
                return false;
            Current = 0f;
            return true;
        }

        public void Clear() => Current = 0f;
    }
}
