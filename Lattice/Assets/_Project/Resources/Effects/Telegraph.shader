Shader "Lattice/Telegraph"
{
    // Ground warning for a marked attack (D198). The mesh carries its own coordinates: uv.x runs from the attack's origin
    // (0) to its far edge (1) and uv.y across it (0..1). The marked area reads faintly, the part already "charged" fills
    // in from the origin as the tell runs, and the outline stays crisp so the edge of the danger is never in doubt.
    Properties
    {
        _BaseColor ("Warning colour", Color) = (1,0.38,0.1,1)
        _Fill ("Charged fraction", Range(0,1)) = 0
        _Flash ("Release flash", Range(0,1)) = 0
        _Fade ("Visibility", Range(0,1)) = 1
        _SideEdges ("Outline the sides", Float) = 1
        _InnerEdge ("Outline the inner edge", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -2, -2
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Fill;
                half _Flash;
                half _Fade;
                half _SideEdges;
                half _InnerEdge;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float2 scale:TEXCOORD1; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 scale:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=input.uv;
                output.scale=input.scale;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                // scale = metres spanned by uv.x and uv.y, so the outline keeps a constant width in the world.
                float2 metres=max(input.scale,0.01);
                float outer=(1-input.uv.x)*metres.x;
                float inner=input.uv.x*metres.x;
                float side=min(input.uv.y,1-input.uv.y)*metres.y;
                float edge=outer;
                edge=_InnerEdge>0.5?min(edge,inner):edge;
                edge=_SideEdges>0.5?min(edge,side):edge;
                half outline=1-smoothstep(0.06,0.14,edge);
                half charged=step(input.uv.x,_Fill);
                // The charge front is a bright band so the timing reads at a glance.
                half front=(1-smoothstep(0.0,0.35,abs(input.uv.x-_Fill)*metres.x))*step(0.001,_Fill)*step(_Fill,0.999);
                half alpha=0.16+0.30*charged+0.55*front;
                alpha=max(alpha,outline*0.95);
                half3 colour=_BaseColor.rgb*(1+0.6*front+0.4*outline);
                colour=lerp(colour,half3(1,1,1),_Flash*0.7);
                alpha=saturate(alpha+_Flash*0.5)*_Fade*_BaseColor.a;
                return half4(colour,alpha);
            }
            ENDHLSL
        }
    }
}
