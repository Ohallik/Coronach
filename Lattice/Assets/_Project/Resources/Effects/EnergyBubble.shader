Shader "Lattice/EnergyBubble"
{
    // A body of pure energy, drawn where a blockout ball used to be: the ball's edge burns and its middle shows what's behind it.
    Properties
    {
        _BaseColor ("Rim colour", Color) = (1,0.5,0.2,1)
        _Core ("Middle opacity", Range(0,1)) = 0.14
        _Sharpness ("Rim sharpness", Float) = 2.6
        _Intensity ("Intensity", Float) = 1.8
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Core;
                float _Sharpness;
                float _Intensity;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 viewWS:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(world);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.viewWS=GetWorldSpaceViewDir(world);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float facing=saturate(dot(normalize(input.normalWS),normalize(input.viewWS)));
                float rim=pow(1-facing,_Sharpness);
                return half4(_BaseColor.rgb*_Intensity*(0.45+rim),saturate(_Core+rim)*_BaseColor.a);
            }
            ENDHLSL
        }
    }
}
