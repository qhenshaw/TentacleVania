using TentacleVania.Core.Characters;
using TentacleVania.Enemies.Data;
using UnityEngine;

namespace TentacleVania.Enemies.Telegraphs
{
    [RequireComponent(typeof(CharacterMaterials))]
    public sealed class TelegraphGlow : MonoBehaviour
    {
        private const float MinimumGlow = 0.25f;

        [SerializeField, ColorUsage(false, true)] private Color _dodgeable = new(4f, 2.6f, 0.2f);
        [SerializeField, ColorUsage(false, true)] private Color _undodgeable = new(4.5f, 0.25f, 0.15f);
        [SerializeField, ColorUsage(false, true)] private Color _hitFlash = new(2.5f, 2.5f, 2.5f);
        [SerializeField, ColorUsage(false, true)] private Color _stunned = new(0.45f, 0.8f, 1.3f);
        [SerializeField, Min(0f)] private float _stunnedPulse = 6f;
        [SerializeField, ColorUsage(false, true)] private Color _feedable = new(0.9f, 2.2f, 0.25f);
        [SerializeField, Min(0f)] private float _feedableAmount = 0.8f;
        [SerializeField, Min(0f)] private float _feedablePulse = 4f;

        private CharacterMaterials _materials;
        private bool _isStunned;
        private bool _isFeedable;
        private float _current = -1f;
        private Telegraph _telegraph;

        public float Intensity => Mathf.Max(0f, _current);
        public bool IsFlashing => _materials.IsFlashing;

        private void Awake()
        {
            _materials = GetComponent<CharacterMaterials>();
            Clear();
        }

        public void Show(Telegraph telegraph, float progress)
        {
            var intensity = Mathf.Lerp(MinimumGlow, 1f, Mathf.Clamp01(progress));
            if (telegraph == _telegraph && Mathf.Approximately(intensity, _current))
                return;
            _telegraph = telegraph;
            _current = intensity;
            _materials.SetGlow(telegraph == Telegraph.Undodgeable ? _undodgeable : _dodgeable, intensity);
        }

        public void Clear()
        {
            if (_current == 0f)
                return;
            _current = 0f;
            _materials.ClearGlow();
        }

        public void Flash(float duration) => _materials.Flash(_hitFlash, duration);

        public void ShowStunned()
        {
            _isStunned = true;
            ApplyStatus();
        }

        public void ClearStunned()
        {
            _isStunned = false;
            ApplyStatus();
        }

        public void SetFeedable(bool feedable)
        {
            if (_isFeedable == feedable)
                return;
            _isFeedable = feedable;
            ApplyStatus();
        }

        private void ApplyStatus()
        {
            if (_isStunned)
                _materials.SetStatus(_stunned, 1f, _stunnedPulse);
            else if (_isFeedable)
                _materials.SetStatus(_feedable, _feedableAmount, _feedablePulse);
            else
                _materials.ClearStatus();
        }
    }
}
