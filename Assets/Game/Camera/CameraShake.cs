using TentacleVania.Core.Feedback;
using Unity.Cinemachine;
using UnityEngine;

namespace TentacleVania.Camera
{
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public sealed class CameraShake : MonoBehaviour
    {
        private CinemachineImpulseSource _source;
        private int _lastFrame = -1;
        private float _lastStrength;

        private void Awake()
        {
            _source = GetComponent<CinemachineImpulseSource>();
            CinemachineImpulseManager.Instance.IgnoreTimeScale = true;
        }

        private void OnEnable() => ScreenShake.Requested += OnShake;

        private void OnDisable() => ScreenShake.Requested -= OnShake;

        private void OnShake(Vector3 origin, float strength)
        {
            if (Time.frameCount == _lastFrame && strength <= _lastStrength)
                return;
            _lastFrame = Time.frameCount;
            _lastStrength = strength;
            var direction = Random.insideUnitSphere;
            direction.y = Mathf.Abs(direction.y) + 0.5f;
            _source.GenerateImpulseAtPositionWithVelocity(origin, direction.normalized * strength);
        }
    }
}
