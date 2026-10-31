using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TentacleVania.Core.Rendering
{
    [AddComponentMenu("")]
    public sealed class DynamicResolution : MonoBehaviour
    {
        private const float Window = 0.5f;
        private const float RaiseAfter = 2f;

        private static DynamicResolution _instance;

        private readonly FrameTiming[] _timing = new FrameTiming[1];

        private UniversalRenderPipelineAsset _asset;
        private float _windowStarted;
        private double _gpuSum;
        private int _gpuFrames;
        private float _calmSince;

        public static bool Enabled { get; set; } = true;
        public static float Scale { get; private set; } = 1f;
        public static float LastGpuMs { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            Enabled = true;
            Scale = 1f;
            LastGpuMs = 0f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (_instance != null || Application.isEditor || !Application.isPlaying)
                return;
            var host = new GameObject("DynamicResolution");
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<DynamicResolution>();
        }

        private void Awake()
        {
            _asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (_asset == null)
            {
                enabled = false;
                return;
            }
            _asset.upscalingFilter = UpscalingFilterSelection.FSR;
            Apply(1f);
            _windowStarted = Time.unscaledTime;
            _calmSince = Time.unscaledTime;
        }

        private void Update()
        {
            if (!Enabled)
            {
                if (Scale < 1f)
                    Apply(1f);
                return;
            }
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _timing) > 0 && _timing[0].gpuFrameTime > 0d)
            {
                _gpuSum += _timing[0].gpuFrameTime;
                _gpuFrames++;
            }
            var now = Time.unscaledTime;
            if (now - _windowStarted < Window || _gpuFrames == 0)
                return;
            LastGpuMs = (float)(_gpuSum / _gpuFrames);
            _gpuSum = 0d;
            _gpuFrames = 0;
            _windowStarted = now;
            if (LastGpuMs >= DynamicResolutionRules.RaiseBelowMs)
                _calmSince = now;
            var next = DynamicResolutionRules.Next(Scale, LastGpuMs, now - _calmSince, RaiseAfter);
            if (!Mathf.Approximately(next, Scale))
            {
                Apply(next);
                _calmSince = now;
            }
        }

        private void Apply(float scale)
        {
            Scale = scale;
            _asset.renderScale = scale;
        }
    }
}
