Shader "Lattice/HeroThrough"
{
    // A hero seen through whatever stands between the fixed camera and them (HeroThrough.cs): one clean line round the part of the body that is hidden, of even width
    // on the screen, with a faint flat tint inside it so the surface in front stays readable through the figure (that is what says the hero is behind it, not on it).
    // D414: the old drawing was a Fresnel rim over a fill; seen from above almost all of a body is at a grazing angle, so it was a bright, nearly solid glass figure that
    // read as standing ON the arch, the lintel or the roof that hid it (nov24-whole s13/007, s11/007, s50/071).
    // Two materials of this one shader, by property only (no keyword, no variant), each on a copy of the body of its own:
    //  - the body (_Push 0, queue Transparent+40): marks every pixel of the body in stencil bit 16, once; where it is hidden draws the tint, and along the edge where
    //    the scenery stops hiding it (the hiding edge, found in the scene's depth a line's width away) draws the line, so a half-hidden body's outline closes;
    //  - the hull (_Push 1, queue Transparent+41, after every body): the body pushed out along its normals by the line's width in screen pixels, drawn only off the
    //    bodies' stencil and only where it is hidden: the silhouette's line, outside the body.
    Properties
    {
        _BaseColor ("Line colour", Color) = (1,0.8,0.36,1)
        _Fill ("Tint inside the line", Range(0,1)) = 0.12
        _Line ("Line opacity", Range(0,1)) = 0.95
        _Width ("Line width (pixels of 1080)", Float) = 3
        _Push ("Hull (1) or body (0)", Float) = 0
        _Thickness ("Own thickness (m)", Float) = 0.55
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
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
            Cull [_Cull]
            // Bit 16 is free: bodies write 128 (LatticeToon's _BodyStencil, a whole-byte replace in the opaque pass, which clears this bit under anything opaque), the
            // telegraphs 64 and 32. Each pixel is drawn once, by the first fragment that reaches it: overlapping parts don't stack their tint, and the hull's line never
            // draws inside a body.
            Stencil { Ref 16 ReadMask 16 WriteMask 16 Comp NotEqual Pass Replace }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Fill;
                half _Line;
                float _Width;
                float _Push;
                float _Thickness;
                float _Cull;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; };
            // The line's width in this target's pixels: the same share of the screen's height at any resolution.
            float Pixels() { return _Width * _ScaledScreenParams.y / 1080.0; }
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                float4 position = TransformWorldToHClip(world);
                // Pushed out across the screen, by pixels, not metres: an even line near or far. The normal's direction is taken in pixels so the screen's aspect doesn't
                // stretch it.
                float2 across = mul((float3x3)UNITY_MATRIX_VP, TransformObjectToWorldNormal(input.normalOS)).xy * _ScaledScreenParams.xy;
                float length2 = dot(across, across);
                float2 direction = length2 > 1e-10 ? across * rsqrt(length2) : float2(0, 0);
                position.xy += direction * (Pixels() * 2.0 / _ScaledScreenParams.xy) * position.w * _Push;
                output.positionCS = position;
                return output;
            }
            // Does what the camera drew at this point stand in front of a body this far away by more than the body is thick: an arm across the chest, a foot in the floor
            // or a tuft at the ankle never counts.
            float Hidden(float2 uv, float self)
            {
                float scene = LinearEyeDepth(SampleSceneDepth(saturate(uv)), _ZBufferParams);
                return self - scene > _Thickness ? 1.0 : 0.0;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                float self = input.positionCS.w;
                float hidden = Hidden(uv, self);
                if (_Push > 0.5)
                {
                    // The hull: only off the bodies (the stencil) and only where something stands in front of it.
                    clip(hidden - 0.5);
                    return half4(_BaseColor.rgb, _Line);
                }
                // The body where nothing hides it draws nothing, but still marks its pixels, so no hull draws a line across a hero in plain view.
                if (hidden < 0.5) return half4(0, 0, 0, 0);
                // Within a line's width of a point where the scenery stops hiding this body: the hiding edge, where the line closes.
                float2 reach = Pixels() / _ScaledScreenParams.xy;
                float open = 0;
                [unroll] for (int i = 0; i < 8; i++)
                {
                    float angle = i * 0.785398;
                    float2 toward = float2(cos(angle), sin(angle)) * reach;
                    open = max(open, 1.0 - Hidden(uv + toward, self));
                    open = max(open, 1.0 - Hidden(uv + toward * 0.5, self));
                }
                return half4(_BaseColor.rgb, open > 0.5 ? _Line : _Fill);
            }
            ENDHLSL
        }
    }
}
