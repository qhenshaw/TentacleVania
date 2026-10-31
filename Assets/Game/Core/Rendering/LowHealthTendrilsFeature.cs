using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace TentacleVania.Core.Rendering
{
    public sealed class LowHealthTendrilsFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader _shader;
        [Tooltip("Furthest the tendrils reach in from the screen edge, as a fraction of screen height.")]
        [SerializeField, Range(0.05f, 0.35f)] private float _reach = 0.22f;

        private LowHealthTendrilsPass _pass;

        public static bool Enabled { get; set; } = true;
        public static float Amount { get; set; }
        public static float Pulse { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Enabled = true;
            Amount = 0f;
            Pulse = 0f;
        }

        public override void Create()
        {
            _pass?.Dispose();
            _pass = _shader == null ? null : new(_shader) { renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null || !Enabled || Amount <= 0.001f || renderingData.cameraData.cameraType != CameraType.Game)
                return;
            _pass.Setup(Amount, Pulse, _reach);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
            _pass = null;
        }
    }

    public sealed class LowHealthTendrilsPass : ScriptableRenderPass
    {
        private static readonly int SourceId = Shader.PropertyToID("_Source");
        private static readonly int AmountId = Shader.PropertyToID("_Amount");
        private static readonly int PulseId = Shader.PropertyToID("_Pulse");
        private static readonly int ReachId = Shader.PropertyToID("_Reach");
        private static readonly Vector4 ScaleBias = new(1f, 1f, 0f, 0f);

        private readonly Material _material;
        private readonly ProfilingSampler _sampler = new("Low Health Tendrils");
        private float _amount;
        private float _pulse;
        private float _reach;

        private sealed class PassData
        {
            public Material Material;
            public TextureHandle Source;
            public float Amount;
            public float Pulse;
            public float Reach;
        }

        public LowHealthTendrilsPass(Shader shader)
        {
            _material = CoreUtils.CreateEngineMaterial(shader);
            requiresIntermediateTexture = true;
        }

        public void Setup(float amount, float pulse, float reach)
        {
            _amount = amount;
            _pulse = pulse;
            _reach = reach;
        }

        public void Dispose() => CoreUtils.Destroy(_material);

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer)
                return;
            var color = resources.activeColorTexture;
            var desc = renderGraph.GetTextureDesc(color);
            desc.name = "_LowHealthTendrilsSource";
            desc.clearBuffer = false;
            var copy = renderGraph.CreateTexture(desc);
            renderGraph.AddBlitPass(color, copy, Vector2.one, Vector2.zero, passName: "Low Health Tendrils Copy");

            using var builder = renderGraph.AddRasterRenderPass<PassData>("Low Health Tendrils", out var data, _sampler);
            data.Material = _material;
            data.Source = copy;
            data.Amount = _amount;
            data.Pulse = _pulse;
            data.Reach = _reach;
            builder.UseTexture(copy);
            builder.SetRenderAttachment(color, 0);
            builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
            {
                d.Material.SetTexture(SourceId, (RTHandle)d.Source);
                d.Material.SetFloat(AmountId, d.Amount);
                d.Material.SetFloat(PulseId, d.Pulse);
                d.Material.SetFloat(ReachId, d.Reach);
                Blitter.BlitTexture(context.cmd, d.Source, ScaleBias, d.Material, 0);
            });
        }
    }
}
