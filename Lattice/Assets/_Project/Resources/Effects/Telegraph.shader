Shader "Lattice/Telegraph"
{
    // Ground warning for a marked attack (D198). The mesh carries its own coordinates: uv.x runs from the attack's origin
    // (0) to its far edge (1) and uv.y across it (0..1). The floor has to stay readable under it, so the area itself is a
    // faint wash and a hatch of constant width in the world; what the player reads is the edge (a bright rim with a dark
    // keyline inside it, which holds on an orange floor and on a purple one) and the bright charge front.
    Properties
    {
        _BaseColor ("Warning colour", Color) = (1,0.38,0.1,1)
        _Fill ("Charged fraction", Range(0,1)) = 0
        _Flash ("Release flash", Range(0,1)) = 0
        _Fade ("Visibility", Range(0,1)) = 1
        _SideEdges ("Outline the sides", Float) = 1
        _InnerEdge ("Outline the inner edge", Float) = 0
        _Through ("Outline shows through what stands in it", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Fill;
            half _Flash;
            half _Fade;
            half _SideEdges;
            half _InnerEdge;
            half _Through;
        CBUFFER_END
        struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float2 scale:TEXCOORD1; };
        struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 scale:TEXCOORD1; float3 positionWS:TEXCOORD2; };
        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS=TransformWorldToHClip(output.positionWS);
            output.uv=input.uv;
            output.scale=input.scale;
            return output;
        }
        // scale = metres spanned by uv.x and uv.y, so the outline keeps a constant width in the world.
        float EdgeMetres(Varyings input)
        {
            float2 metres=max(input.scale,0.01);
            float edge=(1-input.uv.x)*metres.x;
            edge=_InnerEdge>0.5?min(edge,input.uv.x*metres.x):edge;
            edge=_SideEdges>0.5?min(edge,min(input.uv.y,1-input.uv.y)*metres.y):edge;
            return edge;
        }
        half Rim(float edge){return 1-smoothstep(0.07,0.13,edge);}
        ENDHLSL
        // Grass or a low prop standing in the marking doesn't chew its edge: the outline alone, fainter, through whatever is nearer the camera.
        // Only the rim, so a hero standing in the marking stays in front of it at full strength.
        Pass
        {
            Name "MarkingThrough"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Greater
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings input):SV_Target
            {
                // A marking tinted faint on purpose (a lit relay's ward) is a boundary, not a danger: it stays out of the walls.
                half alpha=Rim(EdgeMetres(input))*0.42*_Through*_Fade*_BaseColor.a*step(0.99,_BaseColor.a);
                clip(alpha-0.004);
                return half4(_BaseColor.rgb*1.3,alpha);
            }
            ENDHLSL
        }
        Pass
        {
            Name "Marking"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -2, -2
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings input):SV_Target
            {
                float2 metres=max(input.scale,0.01);
                float edge=EdgeMetres(input);
                half rim=Rim(edge);
                // Not on a faint boundary either: a narrow band would be a dark line down its own middle.
                half keyline=(1-smoothstep(0.17,0.23,edge))*(1-rim)*step(0.99,_BaseColor.a);
                half charged=step(input.uv.x,_Fill);
                // The charge front is a bright band so the timing reads at a glance.
                half front=(1-smoothstep(0.0,0.35,abs(input.uv.x-_Fill)*metres.x))*step(0.001,_Fill)*step(_Fill,0.999);
                // Diagonal lines 8 cm wide every 62 cm in the world, whatever the shape: marked ground, not a flat sheet of colour.
                float along=dot(input.positionWS.xz,float2(0.7071,0.7071));
                float line_=abs(frac(along/0.62)-0.5)*0.62;
                half hatch=1-smoothstep(0.04,0.04+max(fwidth(along),0.01),line_);
                half alpha=0.03+0.035*charged+hatch*(0.10+0.16*charged)+0.6*front;
                half3 colour=_BaseColor.rgb*(1+0.6*front);
                colour=lerp(colour,_BaseColor.rgb*0.16,keyline);
                alpha=max(alpha,keyline*0.6);
                colour=lerp(colour,_BaseColor.rgb*1.5,rim);
                alpha=max(alpha,rim);
                colour=lerp(colour,half3(1,1,1),_Flash*0.7);
                alpha=saturate(alpha+_Flash*0.5)*_Fade*_BaseColor.a;
                return half4(colour,alpha);
            }
            ENDHLSL
        }
    }
}
