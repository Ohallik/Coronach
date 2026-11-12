Shader "Lattice/Telegraph"
{
    // The outline of a marked attack (D198): a bright rim with a dark keyline inside it, which holds on an orange floor and on a purple one. The mesh carries its own coordinates: uv.x runs
    // from the attack's origin (0) to its far edge (1) and uv.y across it (0..1). The area under the outline (wash, hatch, the charge's glow) is TelegraphFill, drawn from the same mesh a step
    // earlier, and it leaves a mark in the stencil (bit 64) wherever it is well inside a danger: an outline isn't drawn there, so markings that overlap read as one shape with one edge.
    Properties
    {
        _BaseColor ("Warning colour", Color) = (1,0.38,0.1,1)
        _Fill ("Charged fraction", Range(0,1)) = 0
        _Flash ("Release flash", Range(0,1)) = 0
        _Fade ("Visibility", Range(0,1)) = 1
        _SideEdges ("Outline the sides", Float) = 1
        _InnerEdge ("Outline the inner edge", Float) = 0
        _Through ("Outline shows through what stands in it", Float) = 1
        _Hatch ("Hatch: across the lines (x,z), spacing, strength", Vector) = (-0.607,0.795,0.62,1)
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
            float4 _Hatch;
        CBUFFER_END
        struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float3 scale:TEXCOORD1; };
        struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 scale:TEXCOORD1; float3 positionWS:TEXCOORD2; };
        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS=TransformWorldToHClip(output.positionWS);
            output.uv=input.uv;
            output.scale=input.scale;
            return output;
        }
        // scale.xy = metres spanned by uv.x and uv.y, so the outline keeps a constant width in the world. scale.z = metres from where the ground cuts the marking (D217): positive on the
        // floor, negative past it, 99 where nothing cuts it. The cut is an edge like any other, so the paint stops there and the rim closes the shape.
        float EdgeMetres(Varyings input)
        {
            float2 metres=max(input.scale.xy,0.01);
            float edge=(1-input.uv.x)*metres.x;
            edge=_InnerEdge>0.5?min(edge,input.uv.x*metres.x):edge;
            edge=_SideEdges>0.5?min(edge,min(input.uv.y,1-input.uv.y)*metres.y):edge;
            return min(edge,input.scale.z);
        }
        // Thinner than it was (0.07 to 0.13 m): with its keyline it took a third of a shot's narrow lane, two heavy rails, and it was the strongest thing on the screen. Never under about
        // three pixels, though, wherever the camera stands: in metres alone it's under two at the flight camera's farthest pull. px is metres a pixel across the edge (fwidth of edge).
        half Rim(float edge,float px){return 1-smoothstep(max(0.05,1.5*px),max(0.1,3*px),edge);}
        half Keyline(float edge,float px,half rim){return (1-smoothstep(max(0.13,3.5*px),max(0.19,5*px),edge))*(1-rim);}
        ENDHLSL
        // Grass or a low prop standing in the marking doesn't chew its edge: the outline alone, fainter, through whatever is nearer the camera.
        // Only the rim, so a hero standing in the marking stays in front of it at full strength. Bodies say so in the stencil (LatticeToon's _BodyStencil, bit 128): across a boss it was
        // bright sticks stuck in its side, stopping dead partway over. Nor inside another marking (bit 64).
        Pass
        {
            Name "MarkingThrough"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Greater
            Cull Off
            Stencil { Ref 0 ReadMask 192 Comp Equal }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings input):SV_Target
            {
                clip(1.0005-input.uv.x);
                // Past where the ground cut it, the marking has gone on under a hill: no use to anyone there, floating on the rock.
                clip(input.scale.z);
                // A marking tinted faint on purpose (a lit relay's ward) is a boundary, not a danger: it stays out of the walls.
                float edge=EdgeMetres(input);
                half alpha=Rim(edge,fwidth(edge))*0.42*_Through*_Fade*_BaseColor.a*step(0.99,_BaseColor.a);
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
            Stencil { Ref 0 ReadMask 64 Comp Equal }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings input):SV_Target
            {
                // uv.x runs on past 1 where a lane has gone on under a hill: its paint ended at the rim before that.
                clip(1.0005-input.uv.x);
                clip(input.scale.z);
                float edge=EdgeMetres(input),px=fwidth(edge);
                half rim=Rim(edge,px);
                // Only on a danger: a faint boundary has no outline, and a narrow band would be a dark line down its own middle.
                half danger=step(0.99,_BaseColor.a);
                half keyline=Keyline(edge,px,rim);
                half3 colour=lerp(_BaseColor.rgb*0.16,_BaseColor.rgb*1.4,rim);
                half alpha=max(rim*0.92,keyline*0.6)*danger;
                // The strike's flash lights the outline with the lines under it.
                colour=lerp(colour,half3(1,1,1),_Flash*0.22);
                alpha*=_Fade*_BaseColor.a;
                clip(alpha-0.004);
                return half4(colour,alpha);
            }
            ENDHLSL
        }
    }
}
