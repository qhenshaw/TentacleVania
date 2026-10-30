using UnityEngine;

namespace TentacleVania.UI.Hud
{
    public sealed class BarTrail
    {
        private readonly float _drainDelay;
        private readonly float _drainSpeed;
        private readonly float _fillSpeed;
        private float _sinceDrop;

        public BarTrail(float drainDelay, float drainSpeed, float fillSpeed)
        {
            _drainDelay = drainDelay;
            _drainSpeed = drainSpeed;
            _fillSpeed = fillSpeed;
        }

        public float Value { get; private set; }
        public float Shown { get; private set; }
        public float Trail { get; private set; }

        public void Snap(float value)
        {
            Value = Shown = Trail = Mathf.Clamp01(value);
            _sinceDrop = 0f;
        }

        public void Update(float value, float dt)
        {
            value = Mathf.Clamp01(value);
            if (value < Value)
                _sinceDrop = 0f;
            else
                _sinceDrop += dt;
            Value = value;

            Shown = value < Shown ? value : Mathf.MoveTowards(Shown, value, _fillSpeed * dt);

            if (value >= Trail)
                Trail = value;
            else if (_sinceDrop >= _drainDelay)
                Trail = Mathf.MoveTowards(Trail, value, _drainSpeed * dt);
        }
    }
}
