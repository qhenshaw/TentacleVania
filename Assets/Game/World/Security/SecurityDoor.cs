using System;
using FMODUnity;
using PrimeTween;
using TentacleVania.Core.Progression;
using UnityEngine;

namespace TentacleVania.World.Security
{
    public sealed class SecurityDoor : MonoBehaviour
    {
        [Serializable]
        public sealed class Cable
        {
            public string Relay;
            public LineRenderer Line;
            public Renderer Light;
        }

        [SerializeField] private string _system = "SH";
        [SerializeField, Min(1)] private int _required = SecurityRelays.PerSystem;
        [SerializeField] private Transform _slab;
        [SerializeField] private Collider _blocker;
        [SerializeField] private float _openHeight = -3.2f;
        [SerializeField, Min(0f)] private float _openDuration = 1.6f;
        [SerializeField] private Renderer[] _lights = Array.Empty<Renderer>();
        [SerializeField] private Cable[] _cables = Array.Empty<Cable>();
        [SerializeField] private Material _armedLight;
        [SerializeField] private Material _offLight;
        [SerializeField, ColorUsage(false, true)] private Color _liveCable = new(2.2f, 0.25f, 0.15f, 1f);
        [SerializeField] private Color _deadCable = new(0.08f, 0.08f, 0.08f, 1f);
        [SerializeField, Min(0.1f)] private float _flickerRate = 3f;
        [SerializeField] private EventReference _powerDownSound;
        [SerializeField] private EventReference _openSound;

        private Tween _tween;
        private bool _flickerOn;

        public string System => _system;
        public int Required => _required;
        public DoorPower Power { get; private set; }

        private void OnEnable() => SecurityRelays.RelayDestroyed += OnRelayDestroyed;

        private void OnDisable() => SecurityRelays.RelayDestroyed -= OnRelayDestroyed;

        private void Start() => Refresh(false);

        private void Update()
        {
            if (Power != DoorPower.PoweringDown)
                return;
            var on = Mathf.PerlinNoise(Time.time * _flickerRate, 0.37f) > 0.45f;
            if (on != _flickerOn)
                SetLights(on);
        }

        public void Refresh(bool animate)
        {
            var previous = Power;
            Power = SecurityRelays.Power(SecurityRelays.Count(_system), _required);
            foreach (var cable in _cables)
            {
                var dead = SecurityRelays.IsDestroyed(_system, cable.Relay);
                if (cable.Line != null)
                {
                    var color = dead ? _deadCable : _liveCable;
                    cable.Line.startColor = color;
                    cable.Line.endColor = color;
                }
                if (cable.Light != null)
                    cable.Light.sharedMaterial = dead ? _offLight : _armedLight;
            }
            SetLights(Power == DoorPower.Armed || Power == DoorPower.PoweringDown);
            var open = Power == DoorPower.Open;
            if (_blocker != null)
                _blocker.enabled = !open;
            _tween.Stop();
            var height = open ? _openHeight : 0f;
            if (animate && _slab != null)
                _tween = Tween.LocalPositionY(_slab, height, _openDuration, Ease.InOutSine);
            else if (_slab != null)
                _slab.localPosition = new(_slab.localPosition.x, height, _slab.localPosition.z);
            if (!animate || previous == Power)
                return;
            var sound = open ? _openSound : Power == DoorPower.PoweringDown ? _powerDownSound : default;
            if (!sound.IsNull)
                RuntimeManager.PlayOneShot(sound, transform.position);
        }

        private void OnRelayDestroyed(string system, int count)
        {
            if (system == _system)
                Refresh(true);
        }

        private void SetLights(bool on)
        {
            _flickerOn = on;
            foreach (var light in _lights)
                if (light != null)
                    light.sharedMaterial = on ? _armedLight : _offLight;
        }
    }
}
