#ifndef TENTACLEVANIA_TELEGRAPH_DECAL
#define TENTACLEVANIA_TELEGRAPH_DECAL

float TelegraphBand(float distance, float width)
{
    float aa = max(fwidth(distance), 1e-4);
    return smoothstep(-aa, aa, distance) * (1.0 - smoothstep(width - aa, width + aa, distance));
}

float TelegraphInside(float distance)
{
    float aa = max(fwidth(distance), 1e-4);
    return smoothstep(-aa, aa, distance);
}

void TelegraphDecal_float(float4 UV0, float3 PositionWS, float3 NormalWS,
    float4 Color, float Shape, float Arc, float Progress, float4 Size, float OutlineWidth,
    float OutlineAlpha, float FillAlpha, float Glow, float Lock,
    out float3 OutBaseColor, out float OutAlpha, out float3 OutEmission)
{
    float2 uv = UV0.xy;
    float edge;
    float reach;
    float along;
    if (Shape < 0.5)
    {
        float2 p = (uv - 0.5) * Size.xy;
        float radius = Size.x * 0.5;
        along = length(p);
        reach = radius;
        edge = radius - along;
    }
    else if (Shape < 1.5)
    {
        float2 p = float2((uv.x - 0.5) * Size.x, uv.y * Size.y);
        along = p.y;
        reach = Size.y;
        edge = min(Size.x * 0.5 - abs(p.x), min(p.y, Size.y - p.y));
    }
    else
    {
        float2 p = (uv - 0.5) * Size.xy;
        float radius = Size.x * 0.5;
        along = length(p);
        reach = radius;
        float halfArc = radians(Arc * 0.5);
        float angle = atan2(abs(p.x), p.y);
        float delta = halfArc - angle;
        float side = Arc >= 359.0 ? radius : (delta > 1.5707963 ? along : along * sin(delta));
        edge = min(radius - along, side);
    }

    float inside = TelegraphInside(edge);
    float outline = TelegraphBand(edge, OutlineWidth);
    float fillEdge = reach * saturate(Progress) - along;
    float fill = inside * TelegraphInside(fillEdge);
    float lockBoost = 1.0 + 2.0 * saturate(Lock);
    float outlineAlpha = outline * saturate(OutlineAlpha * lockBoost);
    float alpha = max(outlineAlpha, fill * FillAlpha);

    OutBaseColor = Color.rgb;
    OutAlpha = saturate(alpha);
    OutEmission = Color.rgb * Glow * (fill * FillAlpha + outline * lockBoost);
}

#endif
