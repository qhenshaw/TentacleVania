using TentacleVania.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace TentacleVania.Camera
{
    public sealed class GameplayCameraRig : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _camera;
        [SerializeField] private CinemachineImpulseListener _impulses;

        private void Update()
        {
            var target = CameraFocus.Target;
            if (_camera.Follow != target)
            {
                _camera.Follow = target;
                _camera.PreviousStateIsValid = false;
            }
            _impulses.Gain = GameSettings.ScreenShake ? 1f : 0f;
        }
    }
}
