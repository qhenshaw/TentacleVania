using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Core.Characters
{
    public sealed class CharacterMaterials : MonoBehaviour
    {
        public static readonly int GlowColor = Shader.PropertyToID("_GlowColor");
        public static readonly int GlowAmount = Shader.PropertyToID("_GlowAmount");
        public static readonly int FlashColor = Shader.PropertyToID("_FlashColor");
        public static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");
        public static readonly int StatusColor = Shader.PropertyToID("_StatusColor");
        public static readonly int StatusAmount = Shader.PropertyToID("_StatusAmount");
        public static readonly int StatusPulse = Shader.PropertyToID("_StatusPulse");

        [SerializeField] private Renderer[] _renderers;

        private readonly List<Material> _materials = new(8);
        private float _flashStarted;
        private float _flashDuration;
        private float _flashStrength;

        public bool IsFlashing => _flashDuration > 0f;

        private void Awake()
        {
            foreach (var renderer in _renderers)
                Register(renderer);
        }

        private void OnDestroy()
        {
            foreach (var material in _materials)
                Destroy(material);
            _materials.Clear();
        }

        private void LateUpdate()
        {
            if (_flashDuration <= 0f)
                return;
            var t = (Time.time - _flashStarted) / _flashDuration;
            if (t >= 1f)
            {
                _flashDuration = 0f;
                SetFloat(FlashAmount, 0f);
                return;
            }
            SetFloat(FlashAmount, _flashStrength * (1f - t));
        }

        public void Register(Renderer renderer)
        {
            if (renderer == null)
                return;
            var material = renderer.material;
            if (!_materials.Contains(material))
                _materials.Add(material);
        }

        public void SetGlow(Color color, float amount)
        {
            SetColor(GlowColor, color);
            SetFloat(GlowAmount, amount);
        }

        public void ClearGlow() => SetFloat(GlowAmount, 0f);

        public void Flash(Color color, float duration, float strength = 1f)
        {
            if (duration <= 0f)
                return;
            _flashStarted = Time.time;
            _flashDuration = duration;
            _flashStrength = (GameSettings.Flashes ? 1f : 0.4f) * strength;
            SetColor(FlashColor, color);
            SetFloat(FlashAmount, _flashStrength);
        }

        public void SetStatus(Color color, float amount, float pulseSpeed = 0f)
        {
            SetColor(StatusColor, color);
            SetFloat(StatusAmount, amount);
            SetFloat(StatusPulse, pulseSpeed);
        }

        public void ClearStatus() => SetFloat(StatusAmount, 0f);

        private void SetColor(int id, Color color)
        {
            foreach (var material in _materials)
                material.SetColor(id, color);
        }

        private void SetFloat(int id, float value)
        {
            foreach (var material in _materials)
                material.SetFloat(id, value);
        }
    }
}
