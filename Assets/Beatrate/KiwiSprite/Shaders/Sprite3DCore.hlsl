#ifndef SPRITE3D_CORE_INCLUDED
#define SPRITE3D_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Sprite3DAttributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Sprite3DVaryings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

TEXTURE2D(_AlphaTex);
SAMPLER(sampler_AlphaTex);

CBUFFER_START(PerSprite3D)
float _EnableExternalAlpha;
float _AlphaThreshold;
float2 _Flip;
float _BillboardMode;
CBUFFER_END

float3 FlipSprite3D(float3 position, float2 flip)
{
    // Need to flip horizontal x when billboarding is turned off.
    // Otherwise the mesh will be horizontally flipped when looking from the front.
    flip.x *= (_BillboardMode == 0) ? -1 : 1;
    return float3(position.x * flip.x, position.y * flip.y, position.z);
}

float4 SampleSprite3DTexture(float2 uv)
{
    float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

    float4 alpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, uv);
    color.a = lerp(color.a, alpha.r, _EnableExternalAlpha);

    return color;
}

void Sprite3DAlpha(float4 color)
{
    clip(color.a < _AlphaThreshold ? -1 : 1);
}

Sprite3DVaryings Sprite3DVertex(Sprite3DAttributes input)
{
    Sprite3DVaryings output;

    float4x4 modelMatrix = UNITY_MATRIX_M;
    float3 scale = float3(
        length(float3(modelMatrix[0][0], modelMatrix[1][0], modelMatrix[2][0])),
        length(float3(modelMatrix[0][1], modelMatrix[1][1], modelMatrix[2][1])),
        length(float3(modelMatrix[0][2], modelMatrix[1][2], modelMatrix[2][2]))
    );

    modelMatrix = lerp(modelMatrix, float4x4(
        1, 0, 0, modelMatrix[0][3],
        0, 1, 0, modelMatrix[1][3],
        0, 0, 1, modelMatrix[2][3],
        0, 0, 0, 1
    ), _BillboardMode != 0);

    float4x4 viewMatrix = UNITY_MATRIX_V;

    float4x4 rotationMatrixY = float4x4(
        viewMatrix._m00_m01_m02, 0,
        float3(0, 1, 0), 0,
        viewMatrix._m20_m21_m22, 0,
        0, 0, 0, 1
    );

    float4x4 rotationMatrixXYZ = float4x4(
        viewMatrix._m00_m01_m02, 0,
        viewMatrix._m10_m11_m12, 0,
        viewMatrix._m20_m21_m22, 0,
        0, 0, 0, 1
    );

    float4x4 rotationMatrix = float4x4(
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1
    );
    rotationMatrix = lerp(rotationMatrix, rotationMatrixY, _BillboardMode == 1);
    rotationMatrix = lerp(rotationMatrix, rotationMatrixXYZ, _BillboardMode == 2);
    rotationMatrix = transpose(rotationMatrix); // Inverse of an orthogonal matrix is the same matrix transposed.
    float4x4 viewProjectionMatrix = mul(UNITY_MATRIX_P, viewMatrix);

    float4x4 scaleMatrix = float4x4(
        scale.x, 0, 0, 0,
        0, scale.y, 0, 0,
        0, 0, scale.z, 0,
        0, 0, 0, 1
    );

    float3 positionOS = FlipSprite3D(input.positionOS.xyz, _Flip);
    float4 position = mul(scaleMatrix, float4(positionOS, 1.0));
    position = mul(rotationMatrix, position);
    position = mul(modelMatrix, position);
    position = mul(viewProjectionMatrix, position);
    output.positionHCS = position;

    output.uv = input.uv;

    return output;
}

#endif