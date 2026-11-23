Shader "Lattice/SignPlate"
{
    // Soft dark paint on the world: the plate behind a sign, a shaft's dark, a wreck's scar. The mesh's vertex alpha feathers its rim.
    // It draws under marked attacks, so a danger's edge is never painted over, and over the opaque floor.
    // Behind a word it keeps to the word (SignText): drawn as many metres nearer the camera along its lines of sight (_Reach) and held to the same band near the camera (_Band),
    // so the plate stays under its letters wherever they are drawn. Both are 0 on any other paint, which stays where it lies.
    Properties
    {
        _BaseColor ("Paint", Color) = (0.03,0.05,0.08,0.62)
        _Fade ("Visibility", Range(0,1)) = 1
        _Reach ("Drawn this many metres nearer the camera", Float) = 0
        _Band ("Grows no more than this near the camera (0: as perspective has it)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-15" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Fade;
                float _Reach;
                float _Band;
            CBUFFER_END
            // The zone camera's own distance (SignStyle.Apply), what a word's made size is measured at.
            float _SignMadeDistance;
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world=TransformObjectToWorld(input.positionOS.xyz);
                if(_Reach>0||_Band>0)
                {
                    float3 origin=TransformObjectToWorld(float3(0,0,0));float3 eye=_WorldSpaceCameraPos.xyz;float far=max(distance(eye,origin),0.001);
                    float shrink=(_Band>0&&_SignMadeDistance>0)?min(1.0,_Band*far/_SignMadeDistance):1.0;
                    world=origin+(world-origin)*shrink;
                    float pull=min(_Reach,far*0.6);
                    world=eye+(world-eye)*((far-pull)/far);
                }
                output.positionCS=TransformWorldToHClip(world);
                output.color=input.color;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                return half4(_BaseColor.rgb,_BaseColor.a*input.color.a*_Fade);
            }
            ENDHLSL
        }
    }
}
