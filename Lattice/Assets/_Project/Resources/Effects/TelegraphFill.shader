Shader "Lattice/TelegraphFill"
{
    // The area of a marked attack (D198), under the outline Lattice/Telegraph draws from the same mesh. The floor has to stay readable under it, so it's a faint wash and a hatch of constant
    // width in the world, with the charge's glow running out from the origin. Drawn a step earlier in the queue than every outline, and once a pixel (stencil bit 32): where markings overlap
    // the area is laid once, not stacked into a brighter lattice of crossing hatch. Well inside a danger it marks bit 64, and no outline is drawn there.
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
        // A faint boundary (a ward) sets these to 0 and Always: it neither waits for nor keeps out another marking's area.
        [HideInInspector] _FillMask ("Area stencil mask", Float) = 32
        [HideInInspector] _FillComp ("Area stencil compare", Float) = 6
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-11" "RenderType"="Transparent" "IgnoreProjector"="True" }
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
        // As Lattice/Telegraph: metres to the nearest edge, the ground's cut (scale.z) among them.
        float EdgeMetres(Varyings input)
        {
            float2 metres=max(input.scale.xy,0.01);
            float edge=(1-input.uv.x)*metres.x;
            edge=_InnerEdge>0.5?min(edge,input.uv.x*metres.x):edge;
            edge=_SideEdges>0.5?min(edge,min(input.uv.y,1-input.uv.y)*metres.y):edge;
            return min(edge,input.scale.z);
        }
        ENDHLSL
        // Well inside a danger, past its own rim and keyline: no other marking's outline is drawn here. Nothing is drawn by this pass itself.
        Pass
        {
            Name "MarkingInside"
            Tags { "LightMode"="SRPDefaultUnlit" }
            ColorMask 0
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -2, -2
            Stencil { Ref 64 WriteMask 64 Comp Always Pass Replace }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings input):SV_Target
            {
                clip(1.0005-input.uv.x);
                clip(input.scale.z);
                // Past the outline's rim and keyline, which never go under a few pixels (Lattice/Telegraph): no other marking's outline crosses it there.
                float edge=EdgeMetres(input);
                clip(edge-max(0.21,5.5*fwidth(edge)));
                clip(_BaseColor.a-0.99);
                // One fading out keeps nobody's outline away.
                clip(_Fade-0.5);
                return 0;
            }
            ENDHLSL
        }
        Pass
        {
            Name "MarkingArea"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -2, -2
            Stencil { Ref 32 ReadMask [_FillMask] WriteMask [_FillMask] Comp [_FillComp] Pass Replace }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings input):SV_Target
            {
                clip(1.0005-input.uv.x);
                clip(input.scale.z);
                float2 metres=max(input.scale.xy,0.01);
                half charged=step(input.uv.x,_Fill);
                // The charge glows behind its leading edge and fades back toward the origin over a metre: the marking filling up. A bright band either side of the front read as a rung across
                // a shot's narrow lane, and at the start of a charge as a red column standing under the Burrower.
                float behind=(_Fill-input.uv.x)*metres.x;
                half front=(1-smoothstep(0.0,1.0,behind))*step(0.0,behind)*step(0.001,_Fill)*step(_Fill,0.999);
                // Soft slanting lines, constant in the world: marked ground, not a flat sheet of colour. Telegraph.Hatch says which way and how far apart.
                float along=dot(input.positionWS.xz,_Hatch.xy);
                float line_=abs(frac(along/_Hatch.z)-0.5)*_Hatch.z;
                half hatch=(1-smoothstep(0.015,0.07+max(fwidth(along),0.01),line_))*_Hatch.w;
                half alpha=(0.03+0.035*charged)*(0.55+0.45*_Hatch.w)+hatch*(0.07+0.11*charged)+0.3*front;
                half3 colour=_BaseColor.rgb*(1+0.4*front);
                // The strike's flash lights the marking's own lines. Across the whole of it, a small burst was a white disc under whatever stood in it, a wide one a pale sheet,
                // and a lane on flight's dark floor a flat salmon slab.
                colour=lerp(colour,half3(1,1,1),_Flash*0.22);
                alpha=saturate(alpha+_Flash*(0.05+hatch*0.35))*_Fade*_BaseColor.a;
                // A faint boundary isn't a marking: light lying on the ground, a soft line near its outer edge that falls away inward to nothing, with no flat band for a rim.
                half faint=1-step(0.99,_BaseColor.a);
                half falloff=smoothstep(0.0,0.75,input.uv.x)*(1-smoothstep(0.85,1.0,input.uv.x));
                colour=lerp(colour,_BaseColor.rgb,faint);
                alpha=lerp(alpha,falloff*falloff*_Fade*_BaseColor.a,faint);
                clip(alpha-0.002);
                return half4(colour,alpha);
            }
            ENDHLSL
        }
    }
}
