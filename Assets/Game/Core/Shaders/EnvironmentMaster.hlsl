#ifndef TENTACLEVANIA_ENVIRONMENT_MASTER
#define TENTACLEVANIA_ENVIRONMENT_MASTER

float4 _EnvironmentFadeFocus;

float EnvironmentBayer(float2 pixel)
{
    uint2 p = uint2(pixel) & 3u;
    const float bayer[16] = { 0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0 };
    return (bayer[p.y * 4u + p.x] + 0.5) / 16.0;
}

void EnvironmentMaster_float(float4 UV0, float3 PositionWS, float3 NormalWS,
    UnityTexture2D BaseMap, float4 BaseColor, UnityTexture2D NormalMap, float Smoothness, float4 EmissionColor, float Fade,
    out float3 OutBaseColor, out float OutAlpha, out float OutAlphaClipThreshold, out float3 OutEmission, out float OutSmoothness, out float3 OutNormalTS)
{
    float2 uv = BaseMap.GetTransformedUV(UV0.xy);
    float4 albedo = SAMPLE_TEXTURE2D(BaseMap.tex, BaseMap.samplerstate, uv) * BaseColor;
    OutBaseColor = albedo.rgb;
    OutEmission = EmissionColor.rgb;
    OutSmoothness = Smoothness;
    OutNormalTS = UnpackNormal(SAMPLE_TEXTURE2D(NormalMap.tex, NormalMap.samplerstate, uv));
    OutAlphaClipThreshold = 0.5;
    OutAlpha = 1.0;
#if !defined(SHADERPASS) || (SHADERPASS != SHADERPASS_SHADOWCASTER)
    if (Fade > 0.001)
    {
        float4 clipPosition = TransformWorldToHClip(PositionWS);
        float2 screen = clipPosition.xy / clipPosition.w * 0.5 + 0.5;
        float2 pixel = screen * _ScreenParams.xy;
        float2 offset = (screen - _EnvironmentFadeFocus.xy) * float2(_EnvironmentFadeFocus.w, 1.0);
        float radius = max(_EnvironmentFadeFocus.z, 1e-4);
        float hole = saturate(1.0 - (length(offset) - radius * 0.6) / (radius * 0.4));
        float strength = Fade * hole;
        OutAlpha = EnvironmentBayer(pixel) >= strength ? 1.0 : 0.0;
    }
#endif
}

#endif
