using UnityEngine;
using UnityEngine.VFX;

namespace TentacleVania.Combat.Feedback
{
    internal sealed class VfxPool
    {
        private static readonly int IntensityId = Shader.PropertyToID("Intensity");

        private readonly VisualEffect[] _effects;
        private readonly bool _hasIntensity;
        private int _next;

        public VfxPool(VisualEffect prefab, int size, Transform parent)
        {
            if (prefab == null)
            {
                _effects = System.Array.Empty<VisualEffect>();
                return;
            }
            _effects = new VisualEffect[size];
            for (var i = 0; i < size; i++)
            {
                _effects[i] = Object.Instantiate(prefab, parent);
                _effects[i].name = $"{prefab.name} {i + 1}";
            }
            _hasIntensity = _effects[0].HasFloat(IntensityId);
        }

        public void Play(Vector3 point, Vector3 direction, float intensity)
        {
            if (_effects.Length == 0)
                return;
            var effect = _effects[_next];
            _next = (_next + 1) % _effects.Length;
            effect.transform.SetPositionAndRotation(point, Quaternion.LookRotation(direction));
            if (_hasIntensity)
                effect.SetFloat(IntensityId, intensity);
            effect.Reinit();
            effect.Play();
        }
    }
}
