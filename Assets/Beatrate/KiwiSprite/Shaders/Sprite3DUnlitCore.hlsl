#include "Sprite3DCore.hlsl"

float4 Sprite3DFragmentUnlit(Sprite3DVaryings input) : SV_Target
{
    float4 color = SampleSprite3DTexture(input.uv);
    Sprite3DAlpha(color);
    return float4(color.rgb, 1);
}

float4 Sprite3DFragmentDepthOnly(Sprite3DVaryings input) : SV_Target
{
    float4 color = SampleSprite3DTexture(input.uv);
    Sprite3DAlpha(color);
    return 0;
}