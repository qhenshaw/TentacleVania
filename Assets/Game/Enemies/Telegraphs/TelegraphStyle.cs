using System;
using TentacleVania.Core;
using TentacleVania.Core.Characters;
using TentacleVania.Core.Settings;
using UnityEngine;

namespace TentacleVania.Enemies.Telegraphs
{
    [Serializable]
    public sealed class TelegraphPalette
    {
        public ColorblindMode Mode;
        [ColorUsage(false, true)] public Color Dodgeable;
        [ColorUsage(false, true)] public Color Undodgeable;
        [ColorUsage(false, true)] public Color Feedable;
        [ColorUsage(false, true)] public Color IndicatorDodgeable;
        [ColorUsage(false, true)] public Color IndicatorUndodgeable;
    }

    [CreateAssetMenu(menuName = "TentacleVania/Enemies/Telegraph Style")]
    public sealed class TelegraphStyle : ScriptableObject
    {
        [Header("Body glow: dodgeable (yellow)")]
        [SerializeField, ColorUsage(false, true)] private Color _dodgeable = new(4f, 2.6f, 0.2f);
        [SerializeField, Min(0.01f)] private float _dodgeableFresnel = 5f;
        [SerializeField, Min(0f)] private float _dodgeableFill = 0.05f;

        [Header("Body glow: undodgeable (red)")]
        [SerializeField, ColorUsage(false, true)] private Color _undodgeable = new(4.5f, 0.25f, 0.15f);
        [SerializeField, Min(0.01f)] private float _undodgeableFresnel = 5f;
        [SerializeField, Min(0f)] private float _undodgeableFill = 0.05f;

        [Header("Body glow: hit flash and states")]
        [SerializeField, ColorUsage(false, true)] private Color _hitFlash = new(2.5f, 2.5f, 2.5f);
        [SerializeField, ColorUsage(false, true)] private Color _stunned = new(0.45f, 0.8f, 1.3f);
        [SerializeField, Min(0f)] private float _stunnedPulse = 0.95f;
        [SerializeField, Min(0.01f)] private float _stunnedFresnel = CharacterMaterials.DefaultStatusFresnel;
        [SerializeField, Min(0f)] private float _stunnedFill = CharacterMaterials.DefaultStatusFill;
        [SerializeField, ColorUsage(false, true)] private Color _feedable = new(0.9f, 2.2f, 0.25f);
        [SerializeField, Min(0f)] private float _feedableAmount = 0.8f;
        [SerializeField, Min(0f)] private float _feedablePulse = 0.64f;
        [SerializeField, Min(0.01f)] private float _feedableFresnel = CharacterMaterials.DefaultStatusFresnel;
        [SerializeField, Min(0f)] private float _feedableFill = CharacterMaterials.DefaultStatusFill;

        [Header("Ground indicator")]
        [SerializeField, ColorUsage(false, true)] private Color _indicatorDodgeable = new(2f, 1.3f, 0.1f);
        [SerializeField, ColorUsage(false, true)] private Color _indicatorUndodgeable = new(2.4f, 0.2f, 0.1f);
        [SerializeField, Min(0.01f)] private float _lockFlashTime = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _lockedLevel = 0.35f;

        [Header("Colour-blind palettes (GameSettings.Colorblind)")]
        [SerializeField] private TelegraphPalette[] _palettes =
        {
            new() { Mode = ColorblindMode.Protanopia, Dodgeable = new(4f, 3.3f, 0.3f), Undodgeable = new(1.4f, 0.6f, 5f), Feedable = new(0.3f, 2.2f, 2.6f), IndicatorDodgeable = new(2f, 1.7f, 0.15f), IndicatorUndodgeable = new(0.7f, 0.3f, 2.6f) },
            new() { Mode = ColorblindMode.Deuteranopia, Dodgeable = new(4f, 3.3f, 0.3f), Undodgeable = new(1.4f, 0.6f, 5f), Feedable = new(0.3f, 2.2f, 2.6f), IndicatorDodgeable = new(2f, 1.7f, 0.15f), IndicatorUndodgeable = new(0.7f, 0.3f, 2.6f) },
            new() { Mode = ColorblindMode.Tritanopia, Dodgeable = new(4f, 1.2f, 2.4f), Undodgeable = new(4.5f, 0.25f, 0.15f), Feedable = new(0.6f, 2.6f, 0.4f), IndicatorDodgeable = new(2f, 0.6f, 1.2f), IndicatorUndodgeable = new(2.4f, 0.2f, 0.1f) },
        };

        private TelegraphPalette Palette
        {
            get
            {
                var mode = GameSettings.Colorblind;
                if (mode == ColorblindMode.Off)
                    return null;
                foreach (var palette in _palettes)
                    if (palette.Mode == mode)
                        return palette;
                return null;
            }
        }

        public Color Dodgeable => Palette?.Dodgeable ?? _dodgeable;
        public float DodgeableFresnel => _dodgeableFresnel;
        public float DodgeableFill => _dodgeableFill;
        public Color Undodgeable => Palette?.Undodgeable ?? _undodgeable;
        public float UndodgeableFresnel => _undodgeableFresnel;
        public float UndodgeableFill => _undodgeableFill;
        public Color HitFlash => _hitFlash;
        public Color Stunned => _stunned;
        public float StunnedPulse => _stunnedPulse;
        public float StunnedFresnel => _stunnedFresnel;
        public float StunnedFill => _stunnedFill;
        public Color Feedable => Palette?.Feedable ?? _feedable;
        public float FeedableAmount => _feedableAmount;
        public float FeedablePulse => _feedablePulse;
        public float FeedableFresnel => _feedableFresnel;
        public float FeedableFill => _feedableFill;
        public Color IndicatorDodgeable => Palette?.IndicatorDodgeable ?? _indicatorDodgeable;
        public Color IndicatorUndodgeable => Palette?.IndicatorUndodgeable ?? _indicatorUndodgeable;
        public float LockFlashTime => _lockFlashTime;
        public float LockedLevel => _lockedLevel;
    }
}
