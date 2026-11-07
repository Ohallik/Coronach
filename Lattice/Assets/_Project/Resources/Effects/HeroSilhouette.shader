Shader "Lattice/HeroSilhouette"
{
    // The hero's shape through a big body in front of them (HeroSilhouette, BossOcclusion). It draws only where something nearer covers the hero:
    // faint at the heart so the boss's surface still reads, bright at the outline so the hero's pose does.
    Properties
    {
        _Color ("Colour", Color) = (0.6,0.92,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "Silhouette"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Greater
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 positionWS:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(output.positionWS);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float3 view=normalize(_WorldSpaceCameraPos-input.positionWS);
                half edge=1-saturate(dot(normalize(input.normalWS),view));
                return half4(_Color.rgb,0.22+0.6*edge*edge);
            }
            ENDHLSL
        }
    }
}
