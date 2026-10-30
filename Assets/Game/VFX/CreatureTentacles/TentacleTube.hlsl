#ifndef TENTACLEVANIA_TENTACLE_TUBE
#define TENTACLEVANIA_TENTACLE_TUBE

void TentacleTube_float(float4 UV0, float3 PositionWS, float3 NormalWS,
    float4 BaseColor, float Smoothness, float4 VeinColor, float VeinAmount, float VeinScale, float VeinSpeed,
    float4 StatusColor, float StatusAmount, float StatusPulse, float StatusFresnel, float StatusFill,
    out float3 OutBaseColor, out float3 OutEmission, out float OutSmoothness, out float3 OutNormalTS)
{
    float across = saturate(UV0.y) * 2.0 - 1.0;
    float bulge = sqrt(saturate(1.0 - across * across));
    OutNormalTS = normalize(float3(0.0, across, max(bulge, 0.05)));

    float edge = 1.0 - bulge;
    float flow = UV0.x * VeinScale - _Time.y * VeinSpeed;
    float vein = pow(saturate(sin(flow * 6.2831853) * 0.5 + 0.5), 8.0) * saturate(1.0 - abs(across) * 1.6);

    float pulse = StatusPulse > 0.0 ? 0.65 + 0.35 * sin(_Time.y * StatusPulse * 6.2831853) : 1.0;
    float fresnel = pow(edge, StatusFresnel > 0.0 ? StatusFresnel : 3.0);
    float3 status = StatusColor.rgb * StatusAmount * (fresnel * pulse + StatusFill);

    OutBaseColor = BaseColor.rgb;
    OutSmoothness = Smoothness;
    OutEmission = VeinColor.rgb * vein * VeinAmount + status;
}

#endif
