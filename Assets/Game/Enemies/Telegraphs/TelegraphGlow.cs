using TentacleVania.Core.Characters;
using TentacleVania.Enemies.Data;
using UnityEngine;

namespace TentacleVania.Enemies.Telegraphs
{
    [RequireComponent(typeof(CharacterMaterials))]
    public sealed class TelegraphGlow : MonoBehaviour
    {
        private const float MinimumGlow = 0.25f;

        [SerializeField] private TelegraphStyle _style;

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
            if (telegraph == Telegraph.Undodgeable)
                _materials.SetGlow(_style.Undodgeable, intensity, _style.UndodgeableFresnel, _style.UndodgeableFill);
            else
                _materials.SetGlow(_style.Dodgeable, intensity, _style.DodgeableFresnel, _style.DodgeableFill);
        }

        public void Clear()
        {
            if (_current == 0f)
                return;
            _current = 0f;
            _materials.ClearGlow();
        }

        public void Flash(float duration) => _materials.Flash(_style.HitFlash, duration);

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
                _materials.SetStatus(_style.Stunned, 1f, _style.StunnedPulse, _style.StunnedFresnel, _style.StunnedFill);
            else if (_isFeedable)
                _materials.SetStatus(_style.Feedable, _style.FeedableAmount, _style.FeedablePulse, _style.FeedableFresnel, _style.FeedableFill);
            else
                _materials.ClearStatus();
        }
    }
}
