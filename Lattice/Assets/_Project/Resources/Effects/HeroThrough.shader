Shader "Lattice/HeroThrough"
{
    // A hero seen through whatever stands between the fixed camera and them (HeroThrough.cs): a second drawing of the body that shows only where the scene's
    // depth is in front of it by more than the body's own thickness. A bright rim round a tinted fill, so a hero behind a boss, a wreck or a wall reads as
    // an outline standing where they really are. The boss's and the scenery's screen door left a dither with the hero somewhere inside it (nov20 s49, s59).
    Properties
    {
        _BaseColor ("Fill colour", Color) = (1,0.9,0.6,1)
        _RimColor ("Rim colour", Color) = (1,1,1,1)
        _Fill ("Fill opacity", Range(0,1)) = 0.42
        _Sharpness ("Rim sharpness", Float) = 1.8
        _Intensity ("Rim intensity", Float) = 1.35
        _Thickness ("Own thickness (m)", Float) = 0.55
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+40" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "HeroThrough"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RimColor;
                float _Fill;
                float _Sharpness;
                float _Intensity;
                float _Thickness;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 viewWS:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(world);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(world);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                // What the camera really drew here, against this point of the body: only something standing in front of it by more than the body is thick counts,
                // so an arm across the chest, a foot in the floor or a tuft of grass at the ankle never lights it.
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                float scene = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float self = input.positionCS.w;
                float hidden = saturate((self - scene - _Thickness) * 4.0);
                clip(hidden - 0.01);
                float facing = saturate(dot(normalize(input.normalWS), normalize(input.viewWS)));
                float rim = pow(1.0 - facing, _Sharpness);
                half3 colour = lerp(_BaseColor.rgb, _RimColor.rgb * _Intensity, saturate(rim * 1.4));
                return half4(colour, saturate(_Fill + rim) * hidden);
            }
            ENDHLSL
        }
    }
}
