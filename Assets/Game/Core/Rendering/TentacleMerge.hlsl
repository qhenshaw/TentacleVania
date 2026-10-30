#ifndef TENTACLEVANIA_TENTACLE_MERGE
#define TENTACLEVANIA_TENTACLE_MERGE

#define TENTACLE_MERGE_TAPS 6

Texture2D<uint> _TentacleMergeLayers;
float _TentacleMergeLayerMask;

bool TentacleMergeMarked(int2 pixel)
{
    return (_TentacleMergeLayers.Load(int3(pixel, 0)) & (uint)_TentacleMergeLayerMask) != 0;
}

float3 TentacleMergeDecode(float4 packed)
{
#if defined(_GBUFFER_NORMALS_OCT)
    return UnpackNormalOctQuadEncode(Unpack888ToFloat2(packed.rgb) * 2.0 - 1.0);
#else
    return normalize(packed.rgb);
#endif
}

float3 TentacleMergeEncode(float3 normal)
{
#if defined(_GBUFFER_NORMALS_OCT)
    return PackFloat2To888(saturate(PackNormalOctQuadEncode(normal) * 0.5 + 0.5));
#else
    return normal;
#endif
}

int2 TentacleMergePixel(float2 uv, float4 texelSize)
{
    return clamp(int2(uv * texelSize.zw), int2(0, 0), int2(texelSize.zw) - 1);
}

float TentacleMergeDepthWeight(float a, float b, float falloff)
{
    float d = (a - b) / falloff;
    return exp(-d * d);
}

void TentacleMerge_float(float4 UV0, UnityTexture2D Source, UnityTexture2D Depth, UnityTexture2D Blurred,
    float MergePass, float4 Params, out float3 OutBaseColor, out float OutAlpha)
{
    float pixelScale = Params.x;
    float radius = Params.y;
    float falloff = Params.z;
    float strength = Params.w;
    OutBaseColor = 0;
    OutAlpha = 0;

    if (MergePass < 0.5)
    {
        int2 p = TentacleMergePixel(UV0.xy, Source.texelSize);
        if (!TentacleMergeMarked(p))
            return;
        OutBaseColor = TentacleMergeDecode(LOAD_TEXTURE2D(Source.tex, p));
        OutAlpha = LinearEyeDepth(LOAD_TEXTURE2D(Depth.tex, p).r, _ZBufferParams);
        return;
    }

    if (MergePass < 2.5)
    {
        int2 size = int2(Blurred.texelSize.zw);
        int2 p = TentacleMergePixel(UV0.xy, Blurred.texelSize);
        float4 center = LOAD_TEXTURE2D(Blurred.tex, p);
        OutBaseColor = center.rgb;
        OutAlpha = center.a;
        if (center.a <= 0.0)
            return;
        int2 direction = MergePass < 1.5 ? int2(1, 0) : int2(0, 1);
        float step = min(radius * pixelScale / center.a, 32.0) / TENTACLE_MERGE_TAPS;
        float3 sum = center.rgb;
        for (int i = 1; i <= TENTACLE_MERGE_TAPS; i++)
        {
            float t = (float)i / TENTACLE_MERGE_TAPS;
            float gauss = exp(-2.0 * t * t);
            int2 offset = direction * (int)round(i * step);
            float4 a = LOAD_TEXTURE2D(Blurred.tex, clamp(p + offset, int2(0, 0), size - 1));
            float4 b = LOAD_TEXTURE2D(Blurred.tex, clamp(p - offset, int2(0, 0), size - 1));
            sum += a.rgb * (a.a > 0.0 ? gauss * TentacleMergeDepthWeight(a.a, center.a, falloff) : 0.0);
            sum += b.rgb * (b.a > 0.0 ? gauss * TentacleMergeDepthWeight(b.a, center.a, falloff) : 0.0);
        }
        OutBaseColor = normalize(sum);
        return;
    }

    int2 pixel = TentacleMergePixel(UV0.xy, Source.texelSize);
    float4 gbuffer = LOAD_TEXTURE2D(Source.tex, pixel);
    OutBaseColor = gbuffer.rgb;
    OutAlpha = gbuffer.a;
    if (!TentacleMergeMarked(pixel))
        return;

    float3 normal = TentacleMergeDecode(gbuffer);
    float depth = LinearEyeDepth(LOAD_TEXTURE2D(Depth.tex, pixel).r, _ZBufferParams);
    int2 halfSize = int2(Blurred.texelSize.zw);
    float2 halfCoord = (pixel + 0.5) * 0.5 - 0.5;
    int2 base = int2(floor(halfCoord));
    float2 f = halfCoord - base;
    float3 sum = 0;
    float weight = 0;
    for (int y = 0; y <= 1; y++)
    {
        for (int x = 0; x <= 1; x++)
        {
            float4 s = LOAD_TEXTURE2D(Blurred.tex, clamp(base + int2(x, y), int2(0, 0), halfSize - 1));
            float w = (x == 0 ? 1.0 - f.x : f.x) * (y == 0 ? 1.0 - f.y : f.y);
            w *= s.a > 0.0 ? TentacleMergeDepthWeight(s.a, depth, falloff) : 0.0;
            sum += s.rgb * w;
            weight += w;
        }
    }
    if (weight > 1e-4)
        normal = normalize(lerp(normal, normalize(sum), strength));
    OutBaseColor = TentacleMergeEncode(normal);
}

#endif
