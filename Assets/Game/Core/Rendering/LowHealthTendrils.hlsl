#ifndef TENTACLEVANIA_LOW_HEALTH_TENDRILS
#define TENTACLEVANIA_LOW_HEALTH_TENDRILS

float LowHealthHash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float LowHealthNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = LowHealthHash(i);
    float b = LowHealthHash(i + float2(1.0, 0.0));
    float c = LowHealthHash(i + float2(0.0, 1.0));
    float d = LowHealthHash(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float LowHealthFbm(float2 p)
{
    float sum = 0.0;
    float amplitude = 0.5;
    for (int i = 0; i < 4; i++)
    {
        sum += LowHealthNoise(p) * amplitude;
        p = p * 2.03 + float2(17.1, 9.3);
        amplitude *= 0.5;
    }
    return sum;
}

void LowHealthTendrils_float(float2 UV, UnityTexture2D Source, float Amount, float Pulse, float Reach, out float3 Color, out float Alpha)
{
    float3 source = SAMPLE_TEXTURE2D(Source.tex, Source.samplerstate, UV).rgb;
    float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
    float2 centred = UV - 0.5;
    float edge = min((0.5 - abs(centred.x)) * aspect, 0.5 - abs(centred.y));
    float angle = atan2(centred.y, centred.x * aspect);
    float time = _TimeParameters.x;
    float2 polar = float2(angle * 3.2, edge * 7.0 - time * 0.08);
    float warp = LowHealthFbm(polar * 1.7 + time * 0.05);
    float ridge = 1.0 - abs(LowHealthFbm(polar + warp * 1.4) * 2.0 - 1.0);
    ridge = pow(saturate(ridge), 3.0);
    float reach = Reach * Amount * (1.0 + 0.12 * Pulse);
    float body = saturate(1.0 - edge / max(reach * 0.45, 1e-4));
    float tendril = saturate(1.0 - (edge - ridge * reach) / max(reach * 0.25, 1e-4)) * step(edge, reach);
    float mask = saturate(max(body, tendril * ridge * 1.4)) * saturate(Amount * 4.0);
    float rim = saturate(mask * (1.0 - mask) * 4.0);
    float vein = rim * (1.0 - body) * (0.25 + 0.75 * Pulse);
    float3 flesh = float3(0.012, 0.0, 0.004) + float3(0.16, 0.0, 0.025) * vein;
    Color = lerp(source, flesh, mask * 0.94);
    Alpha = 1.0;
}

#endif
