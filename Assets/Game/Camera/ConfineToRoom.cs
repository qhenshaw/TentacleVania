using Unity.Cinemachine;
using UnityEngine;

namespace TentacleVania.Camera
{
    public sealed class ConfineToRoom : CinemachineExtension
    {
        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Body)
                return;
            var bounds = CameraBounds.Active;
            if (bounds == null)
                return;

            var position = state.GetCorrectedPosition();
            var rotation = vcam.transform.rotation;
            if (!FloorFraming.TryVisibleExtents(position, rotation, state.Lens.FieldOfView, state.Lens.Aspect, bounds.FloorHeight,
                    out var focus, out var viewMin, out var viewMax))
                return;

            var clamped = FloorFraming.ClampFocus(focus, viewMin, viewMax, bounds.Min, bounds.Max);
            state.PositionCorrection += new Vector3(clamped.x - focus.x, 0f, clamped.y - focus.y);
        }
    }
}
