#ifndef TENTACLEVANIA_CHARACTER_MASTER
#define TENTACLEVANIA_CHARACTER_MASTER

void CharacterMaster_float(float4 UV0, float3 PositionWS, float3 NormalWS,
    UnityTexture2D BaseMap, float4 BaseColor, float Smoothness, float4 EmissionColor,
    float4 GlowColor, float GlowAmount, float GlowFresnel, float GlowFill, float4 FlashColor, float FlashAmount,
    float4 StatusColor, float StatusAmount, float StatusPulse, float StatusFresnel, float StatusFill,
    out float3 OutBaseColor, out float3 OutEmission, out float OutSmoothness)
{
    float4 albedo = SAMPLE_TEXTURE2D(BaseMap.tex, BaseMap.samplerstate, BaseMap.GetTransformedUV(UV0.xy)) * BaseColor;
    float3 toCamera = normalize(_WorldSpaceCameraPos - PositionWS);
    float rim = 1.0 - saturate(dot(normalize(NormalWS), toCamera));
    float pulse = StatusPulse > 0.0 ? 0.65 + 0.35 * sin(_TimeParameters.x * StatusPulse * 6.2831853) : 1.0;
    float fresnel = pow(rim, StatusFresnel > 0.0 ? StatusFresnel : 3.0);
    float flash = saturate(FlashAmount);

    float3 glow = GlowColor.rgb * GlowAmount * (pow(rim, GlowFresnel > 0.0 ? GlowFresnel : 3.0) + GlowFill);
    float3 status = StatusColor.rgb * StatusAmount * (fresnel * pulse + StatusFill);

    OutBaseColor = lerp(albedo.rgb, FlashColor.rgb, flash * 0.5);
    OutEmission = EmissionColor.rgb + glow + status + FlashColor.rgb * flash;
    OutSmoothness = Smoothness;
}

#endif
