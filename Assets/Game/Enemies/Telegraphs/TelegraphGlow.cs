using TentacleVania.Core;
using TentacleVania.Enemies.Data;
using UnityEngine;

namespace TentacleVania.Enemies.Telegraphs
{
    public sealed class TelegraphGlow : MonoBehaviour
    {
        private const float MinimumGlow = 0.25f;
        private const float StunnedMarker = -2f;
        private const string EmissionKeyword = "_EMISSION";

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer[] _renderers;
        [SerializeField, ColorUsage(false, true)] private Color _dodgeable = new(4f, 2.6f, 0.2f);
        [SerializeField, ColorUsage(false, true)] private Color _undodgeable = new(4.5f, 0.25f, 0.15f);
        [SerializeField, ColorUsage(false, true)] private Color _hitFlash = new(2.5f, 2.5f, 2.5f);
        [SerializeField, ColorUsage(false, true)] private Color _stunned = new(0.45f, 0.8f, 1.3f);

        private Material[] _materials;
        private Color[] _baseEmission;
        private float _current = -1f;
        private Telegraph _telegraph;
        private Color _glow;
        private float _flashUntil;
        private bool _flashing;

        public float Intensity => Mathf.Max(0f, _current);
        public bool IsFlashing => _flashing;

        private void Awake()
        {
            _materials = new Material[_renderers.Length];
            _baseEmission = new Color[_renderers.Length];
            for (var i = 0; i < _renderers.Length; i++)
            {
                _materials[i] = _renderers[i].material;
                _materials[i].EnableKeyword(EmissionKeyword);
                _baseEmission[i] = _materials[i].GetColor(EmissionColor);
            }
            Clear();
        }

        private void OnDestroy()
        {
            if (_materials == null)
                return;
            foreach (var material in _materials)
                Destroy(material);
        }

        private void LateUpdate()
        {
            if (_flashing && Time.time >= _flashUntil)
            {
                _flashing = false;
                Apply(_glow);
            }
        }

        public void Show(Telegraph telegraph, float progress)
        {
            var intensity = Mathf.Lerp(MinimumGlow, 1f, Mathf.Clamp01(progress));
            if (telegraph == _telegraph && Mathf.Approximately(intensity, _current))
                return;
            _telegraph = telegraph;
            _current = intensity;
            SetGlow((telegraph == Telegraph.Undodgeable ? _undodgeable : _dodgeable) * intensity);
        }

        public void ShowStunned()
        {
            _current = StunnedMarker;
            SetGlow(_stunned);
        }

        public void Clear()
        {
            if (_current == 0f)
                return;
            _current = 0f;
            SetGlow(Color.black);
        }

        public void Flash(float duration)
        {
            _flashing = true;
            _flashUntil = Time.time + duration;
            Apply(GameSettings.Flashes ? _hitFlash : _hitFlash * 0.4f);
        }

        private void SetGlow(Color color)
        {
            _glow = color;
            if (!_flashing)
                Apply(color);
        }

        private void Apply(Color color)
        {
            for (var i = 0; i < _materials.Length; i++)
                _materials[i].SetColor(EmissionColor, _baseEmission[i] + color);
        }
    }
}
