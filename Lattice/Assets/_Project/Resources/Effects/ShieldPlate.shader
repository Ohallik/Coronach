Shader "Lattice/ShieldPlate"
{
    // A guardian's plate, drawn as a shield of light standing before it: a see-through pane in the plate's own colour, lit along its rim and down the seams between its panels,
    // with a faint shimmer climbing it. It was a strip of the energy-bubble shader on a mesh whose two faces shared their vertices: the normals summed to nothing, the rim term
    // read as full everywhere, and the strip drew as one flat, unlit, half-opaque slab of lime (whole-game capture s46/047, November 20). This reads the strip's own uv instead
    // (u along the arc, v up it) and has no normals to lose. No keywords, so the one variant there is goes into the build with the shader (it sits in Resources).
    Properties
    {
        _BaseColor ("Plate colour", Color) = (0.62, 0.9, 0.5, 0.55)
        _Fill ("Pane opacity", Range(0, 1)) = 0.14
        _Rim ("Rim opacity", Range(0, 1)) = 0.85
        _Panels ("Panels along the arc", Float) = 7
        _Intensity ("Rim brightness", Float) = 1.15
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Fill;
                half _Rim;
                float _Panels;
                half _Intensity;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=input.uv;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float u=input.uv.x,v=input.uv.y;
                // The rim: a bright band along the top, a softer one at the foot, and the two ends of the arc.
                half top=smoothstep(0.86h,0.97h,v),foot=smoothstep(0.1h,0.0h,v)*0.6h;
                half ends=smoothstep(0.035h,0.0h,u)+smoothstep(0.965h,1.0h,u);
                // Seams between the panels, thin, so the pane reads as plates and not as a sheet of glass.
                float seam=abs(frac(u*_Panels)-0.5)*2;
                half seams=smoothstep(0.9h,1.0h,(half)seam)*0.55h;
                // A faint shimmer climbing it, so it is seen to be held up and not painted on.
                half shimmer=smoothstep(0.92h,1.0h,(half)frac(v*1.5-_Time.y*0.35))*0.12h;
                half edge=saturate(max(max(top,foot),max(ends,seams)));
                half alpha=saturate(_Fill+shimmer+edge*(_Rim-_Fill))*saturate(_BaseColor.a*1.8h);
                // The pane in the plate's colour at its own value, the rim brighter but under white: a shield is coloured light, never a blown-out slab.
                half3 colour=_BaseColor.rgb*lerp(0.75h,_Intensity,edge);
                return half4(min(colour,half3(1.05h,1.05h,1.05h)),alpha);
            }
            ENDHLSL
        }
    }
}
