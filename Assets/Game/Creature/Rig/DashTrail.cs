using UnityEngine;

namespace TentacleVania.Creature.Rig
{
    public sealed class DashTrail
    {
        private const float MaxStep = 1f / 120f;
        private const float MinLengthScale = 0.5f;

        public float Amount { get; private set; }
        public float Velocity { get; private set; }
        public float Weight => Mathf.Clamp01(Amount);

        public void Reset()
        {
            Amount = 0f;
            Velocity = 0f;
        }

        public void Update(bool dashing, float dt, float rise, float frequency, float damping)
        {
            if (dt <= 0f)
                return;
            if (dashing)
            {
                Amount += (1f - Amount) * (1f - Mathf.Exp(-dt / Mathf.Max(0.001f, rise)));
                Velocity = 0f;
                return;
            }
            var omega = 2f * Mathf.PI * frequency;
            var friction = 2f * damping * omega;
            var steps = Mathf.CeilToInt(dt / MaxStep);
            var step = dt / steps;
            for (var i = 0; i < steps; i++)
            {
                Velocity += (-omega * omega * Amount - friction * Velocity) * step;
                Amount += Velocity * step;
            }
        }

        public float LengthScale(float stretch) => Mathf.Max(MinLengthScale, 1f + (stretch - 1f) * Amount);

        public float WidthScale(float thinning) => Mathf.Lerp(1f, thinning, Weight);
    }
}
