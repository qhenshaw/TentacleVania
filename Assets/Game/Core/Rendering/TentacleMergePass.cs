using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace TentacleVania.Core.Rendering
{
    public sealed class TentacleMergePass : ScriptableRenderPass
    {
        private const int NormalsIndex = 2;
        private const int Downsample = 0;
        private const int BlurHorizontal = 1;
        private const int BlurVertical = 2;
        private const int Composite = 3;

        private static readonly int SourceId = Shader.PropertyToID("_Source");
        private static readonly int DepthId = Shader.PropertyToID("_Depth");
        private static readonly int BlurredId = Shader.PropertyToID("_Blurred");
        private static readonly int PassId = Shader.PropertyToID("_MergePass");
        private static readonly int ParamsId = Shader.PropertyToID("_Params");
        private static readonly int LayersId = Shader.PropertyToID("_TentacleMergeLayers");
        private static readonly int LayerMaskId = Shader.PropertyToID("_TentacleMergeLayerMask");
        private static readonly Vector4 ScaleBias = new(1f, 1f, 0f, 0f);

        private readonly Material[] _materials = new Material[4];
        private readonly ProfilingSampler _sampler = new("Tentacle Merge");
        private RenderingLayerMask _layers;
        private float _radius;
        private float _depthFalloff;
        private float _strength;

        private sealed class FullscreenData
        {
            public Material Material;
            public TextureHandle Source;
            public TextureHandle Layers;
            public TextureHandle Depth;
            public TextureHandle Blurred;
            public Vector4 Params;
            public float LayerMask;
        }

        public TentacleMergePass(Shader shader)
        {
            for (var i = 0; i < _materials.Length; i++)
            {
                _materials[i] = CoreUtils.CreateEngineMaterial(shader);
                _materials[i].SetFloat(PassId, i);
            }
        }

        public void Setup(RenderingLayerMask layers, float radius, float depthFalloff, float strength)
        {
            _layers = layers;
            _radius = radius;
            _depthFalloff = depthFalloff;
            _strength = strength;
        }

        public void Dispose()
        {
            foreach (var material in _materials)
                CoreUtils.Destroy(material);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var gBuffer = frameData.Get<UniversalResourceData>().gBuffer;
            if (gBuffer == null || gBuffer.Length <= NormalsIndex || !gBuffer[NormalsIndex].IsValid())
                return;
            var layers = FindRenderingLayers(renderGraph, gBuffer);
            if (!layers.IsValid())
                return;
            var camera = frameData.Get<UniversalCameraData>();
            var normals = gBuffer[NormalsIndex];
            var depth = frameData.Get<UniversalResourceData>().activeDepthTexture;

            var fullDesc = renderGraph.GetTextureDesc(normals);
            var copyDesc = fullDesc;
            copyDesc.name = "_TentacleMergeNormals";
            copyDesc.clearBuffer = false;
            copyDesc.filterMode = FilterMode.Point;
            var copy = renderGraph.CreateTexture(copyDesc);
            renderGraph.AddBlitPass(normals, copy, Vector2.one, Vector2.zero, filterMode: RenderGraphUtils.BlitFilterMode.ClampNearest, passName: "Tentacle Merge Copy");

            var halfDesc = fullDesc;
            halfDesc.width = Mathf.Max(1, fullDesc.width / 2);
            halfDesc.height = Mathf.Max(1, fullDesc.height / 2);
            halfDesc.format = GraphicsFormat.R16G16B16A16_SFloat;
            halfDesc.clearBuffer = false;
            halfDesc.filterMode = FilterMode.Point;
            halfDesc.name = "_TentacleMergeHalfA";
            var halfA = renderGraph.CreateTexture(halfDesc);
            halfDesc.name = "_TentacleMergeHalfB";
            var halfB = renderGraph.CreateTexture(halfDesc);

            var pixelScale = halfDesc.height * 0.5f * camera.GetProjectionMatrix().m11;
            var parameters = new Vector4(pixelScale, _radius, _depthFalloff, _strength);

            AddFullscreen(renderGraph, "Tentacle Merge Downsample", Downsample, halfA, copy, layers, depth, TextureHandle.nullHandle, parameters);
            AddFullscreen(renderGraph, "Tentacle Merge Blur", BlurHorizontal, halfB, copy, layers, depth, halfA, parameters);
            AddFullscreen(renderGraph, "Tentacle Merge Blur", BlurVertical, halfA, copy, layers, depth, halfB, parameters);
            AddFullscreen(renderGraph, "Tentacle Merge Composite", Composite, normals, copy, layers, depth, halfA, parameters);
        }

        private static TextureHandle FindRenderingLayers(RenderGraph renderGraph, TextureHandle[] gBuffer)
        {
            for (var i = gBuffer.Length - 1; i > NormalsIndex; i--)
                if (gBuffer[i].IsValid() && GraphicsFormatUtility.IsUIntFormat(renderGraph.GetTextureDesc(gBuffer[i]).format))
                    return gBuffer[i];
            return TextureHandle.nullHandle;
        }

        private void AddFullscreen(RenderGraph renderGraph, string name, int pass, TextureHandle target,
            TextureHandle source, TextureHandle layers, TextureHandle depth, TextureHandle blurred, Vector4 parameters)
        {
            using var builder = renderGraph.AddRasterRenderPass<FullscreenData>(name, out var data, _sampler);
            data.Material = _materials[pass];
            data.Source = source;
            data.Layers = layers;
            data.Depth = depth;
            data.Blurred = blurred;
            data.Params = parameters;
            data.LayerMask = _layers.value;
            builder.UseTexture(source);
            builder.UseTexture(layers);
            builder.UseTexture(depth);
            if (blurred.IsValid())
                builder.UseTexture(blurred);
            builder.SetRenderAttachment(target, 0);
            builder.SetRenderFunc(static (FullscreenData d, RasterGraphContext context) =>
            {
                d.Material.SetTexture(SourceId, (RTHandle)d.Source);
                d.Material.SetTexture(LayersId, (RTHandle)d.Layers);
                d.Material.SetTexture(DepthId, (RTHandle)d.Depth);
                if (d.Blurred.IsValid())
                    d.Material.SetTexture(BlurredId, (RTHandle)d.Blurred);
                d.Material.SetVector(ParamsId, d.Params);
                d.Material.SetFloat(LayerMaskId, d.LayerMask);
                Blitter.BlitTexture(context.cmd, d.Source, ScaleBias, d.Material, 0);
            });
        }
    }
}
