using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

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
        private static readonly int EffectStatusColor = Shader.PropertyToID("StatusColor");
        private static readonly int EffectStatusAmount = Shader.PropertyToID("StatusAmount");
        private static readonly int EffectStatusPulse = Shader.PropertyToID("StatusPulse");

        [SerializeField] private Renderer[] _renderers;

        private readonly List<Material> _materials = new(8);
        private readonly List<VisualEffect> _effects = new(2);
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

        public void Register(VisualEffect effect)
        {
            if (effect == null || _effects.Contains(effect) || !effect.HasFloat(EffectStatusAmount))
                return;
            _effects.Add(effect);
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
            foreach (var effect in _effects)
            {
                effect.SetVector4(EffectStatusColor, color);
                effect.SetFloat(EffectStatusAmount, amount);
                effect.SetFloat(EffectStatusPulse, pulseSpeed);
            }
        }

        public void ClearStatus()
        {
            SetFloat(StatusAmount, 0f);
            foreach (var effect in _effects)
                effect.SetFloat(EffectStatusAmount, 0f);
        }

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
