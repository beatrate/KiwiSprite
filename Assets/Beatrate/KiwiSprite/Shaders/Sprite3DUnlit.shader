Shader "Sprite3D/Unlit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _AlphaTex ("Alpha Texture", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable external alpha", Float) = 0
        [PerRendererData] _Flip ("Flip", Vector) = (1, 1, 0, 0)
        [PerRendererData] [Enum(Beatrate.KiwiSprite.Sprite3DBillboardMode)] _BillboardMode ("Billboard Mode", Float) = 0
        _AlphaThreshold ("Alpha Threshold", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Unlit"

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Sprite3DUnlitCore.hlsl"           

            #pragma vertex Sprite3DVertex
            #pragma fragment Sprite3DFragmentUnlit
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            Cull Off
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Sprite3DUnlitCore.hlsl"           

            #pragma vertex Sprite3DVertex
            #pragma fragment Sprite3DFragmentDepthOnly
            ENDHLSL
        }
    }
}