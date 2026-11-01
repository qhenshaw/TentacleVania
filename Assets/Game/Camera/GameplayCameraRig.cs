using TentacleVania.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace TentacleVania.Camera
{
    public sealed class GameplayCameraRig : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _camera;
        [SerializeField] private CinemachineImpulseListener _impulses;
        [SerializeField] private CinemachinePositionComposer _composer;
        [Tooltip("How fast the camera distance follows a size stage change (units of distance per second, relative).")]
        [SerializeField, Min(0.01f)] private float _sizeZoomSpeed = 0.5f;

        private float _baseDistance;
        private float _distanceScale = 1f;

        private void Awake()
        {
            if (_composer != null)
                _baseDistance = _composer.CameraDistance;
        }

        private void Update()
        {
            var target = CameraFocus.Target;
            if (_camera.Follow != target)
            {
                _camera.Follow = target;
                _camera.PreviousStateIsValid = false;
            }
            _impulses.Gain = GameSettings.ScreenShake ? 1f : 0f;
            if (_composer == null)
                return;
            _distanceScale = Mathf.MoveTowards(_distanceScale, Core.Characters.CreatureSize.CameraDistanceScale, _sizeZoomSpeed * Time.unscaledDeltaTime);
            _composer.CameraDistance = _baseDistance * _distanceScale;
        }
    }
}
