using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TentacleVania.Core.Rendering
{
    public sealed class TentacleMergeFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader _shader;
        [SerializeField] private RenderingLayerMask _layers;
        [SerializeField, Min(0f)] private float _radius = 0.08f;
        [SerializeField, Min(0.001f)] private float _depthFalloff = 0.15f;
        [SerializeField, Range(0f, 1f)] private float _strength = 0.85f;

        private TentacleMergePass _pass;

        public override void Create()
        {
            _pass?.Dispose();
            _pass = _shader == null ? null : new(_shader) { renderPassEvent = RenderPassEvent.AfterRenderingGbuffer };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null || !GameSettings.TentacleMerge)
                return;
            var cameraType = renderingData.cameraData.cameraType;
            if (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
                return;
            _pass.Setup(_layers, _radius, _depthFalloff, _strength);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
            _pass = null;
        }
    }
}
