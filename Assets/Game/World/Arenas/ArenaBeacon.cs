using UnityEngine;

namespace TentacleVania.World.Arenas
{
    public enum BeaconState
    {
        Off,
        Alarm,
        Clear,
    }

    public sealed class ArenaBeacon : MonoBehaviour
    {
        [SerializeField] private Light _light;
        [SerializeField] private Transform _spinner;
        [SerializeField] private Renderer _lamp;
        [SerializeField] private Material _offMaterial;
        [SerializeField] private Material _alarmMaterial;
        [SerializeField] private Material _clearMaterial;
        [SerializeField] private Color _alarmColor = new(1f, 0.08f, 0.05f);
        [SerializeField] private Color _clearColor = new(1f, 0.62f, 0.12f);
        [SerializeField, Min(0f)] private float _spinSpeed = 300f;

        public BeaconState State { get; private set; }

        private void Awake() => SetState(BeaconState.Off);

        public void SetState(BeaconState state)
        {
            State = state;
            if (_light != null)
            {
                _light.enabled = state != BeaconState.Off;
                _light.color = state == BeaconState.Clear ? _clearColor : _alarmColor;
                _light.spotAngle = state == BeaconState.Clear ? 120f : 55f;
            }
            if (_lamp != null)
            {
                var material = state switch
                {
                    BeaconState.Alarm => _alarmMaterial,
                    BeaconState.Clear => _clearMaterial,
                    _ => _offMaterial,
                };
                if (material != null)
                    _lamp.sharedMaterial = material;
            }
        }

        private void Update()
        {
            if (State == BeaconState.Alarm && _spinner != null)
                _spinner.Rotate(0f, _spinSpeed * Time.deltaTime, 0f, Space.World);
        }
    }
}
