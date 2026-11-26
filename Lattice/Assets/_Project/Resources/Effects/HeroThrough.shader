Shader "Lattice/HeroThrough"
{
    // A hero seen through whatever stands between the fixed camera and them (HeroThrough.cs): one clean line round the part of the body that is hidden, of even width
    // on the screen, with a faint flat tint inside it so the surface in front stays readable through the figure (that is what says the hero is behind it, not on it).
    // D414: the old drawing was a Fresnel rim over a fill; seen from above almost all of a body is at a grazing angle, so it was a bright, nearly solid glass figure that
    // read as standing ON the arch, the lintel or the roof that hid it (nov24-whole s13/007, s11/007, s50/071).
    // D421: drawn only when about half the body or more is hidden. Feet behind a rail or a collar left a loose scrap of line below a hero in plain view (nov25-r5-chain
    // s55/139, s59/053): the share hidden is read from the scene's depth at points over the hero's body (_Hero, set per copy by HeroThrough.cs), and the drawing fades
    // in between _ShareLow and _ShareHigh. A hero wholly behind one broad piece of scenery is shown in its window instead (SceneryOcclusion), which clears the depth there.
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
        _Hero ("The hero's feet (xyz) and height (w); no height draws wherever hidden", Vector) = (0,0,0,0)
        _HeroWidth ("Half the hero's width (m)", Float) = 0.3
        _ShareLow ("Share hidden below which nothing is drawn", Range(0,1)) = 0.3
        _ShareHigh ("Share hidden from which all is drawn", Range(0,1)) = 0.5
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
                float4 _Hero;
                float _HeroWidth;
                float _ShareLow;
                float _ShareHigh;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float4 clipPos:TEXCOORD0; };
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
                output.clipPos = position;
                return output;
            }
            // The scene's depth at a point of the screen, read with no derivatives (it is read inside loops and after an early return).
            float SceneDepthAt(float2 uv)
            {
                float2 at = ClampAndScaleUVForBilinear(UnityStereoTransformScreenSpaceTex(saturate(uv)), _CameraDepthTexture_TexelSize.xy);
                return LinearEyeDepth(SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_PointClamp, at, 0).r, _ZBufferParams);
            }
            // Does what the camera drew at this point stand in front of a body this far away by more than the body is thick: an arm across the chest, a foot in the floor
            // or a tuft at the ankle never counts.
            float Hidden(float2 uv, float self)
            {
                return self - SceneDepthAt(uv) > _Thickness ? 1.0 : 0.0;
            }
            // A clip-space point on the screen as a projection gives it, before the target's own flip (which Frag measures).
            float2 Projected(float4 clip) { return clip.xy / clip.w * 0.5 + 0.5; }
            // Of points over the hero's body (nine heights, the middle and either side, each at its own depth), the share something stands in front of. Each point is placed
            // on the screen by its offset from this fragment's own projection, so the target's flip (a render texture in the editor, the back buffer in the player) can't
            // mislead it: flip is -1 where the screen's rows run against the projection's.
            float ShareHidden(float2 uv, float4 own, float flip)
            {
                if (_Hero.w <= 0.0) return 1.0;
                float2 mine = Projected(own);
                float3 side = UNITY_MATRIX_V[0].xyz * _HeroWidth;
                float hidden = 0, counted = 0;
                [unroll] for (int i = 0; i < 9; i++)
                {
                    [unroll] for (int j = -1; j <= 1; j++)
                    {
                        float4 there = TransformWorldToHClip(_Hero.xyz + float3(0, (i + 0.5) / 9.0 * _Hero.w, 0) + side * j);
                        float2 offset = Projected(there) - mine;
                        float2 at = uv + float2(offset.x, offset.y * flip);
                        float inside = (there.w > 0 && at.x > 0 && at.y > 0 && at.x < 1 && at.y < 1) ? 1.0 : 0.0;
                        counted += inside;
                        hidden += inside * (there.w - SceneDepthAt(at) > _Thickness ? 1.0 : 0.0);
                    }
                }
                return counted > 0 ? hidden / counted : 1.0;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                // Taken here, before any branch: whether the screen's rows run with the projection's or against it.
                float flip = ddy(uv.y) * ddy(Projected(input.clipPos).y) < 0 ? -1.0 : 1.0;
                float self = input.positionCS.w;
                float hidden = Hidden(uv, self);
                if (_Push > 0.5)
                {
                    // The hull: only off the bodies (the stencil) and only where something stands in front of it.
                    clip(hidden - 0.5);
                    return half4(_BaseColor.rgb, _Line * smoothstep(_ShareLow, _ShareHigh, ShareHidden(uv, input.clipPos, flip)));
                }
                // The body where nothing hides it draws nothing, but still marks its pixels, so no hull draws a line across a hero in plain view.
                if (hidden < 0.5) return half4(0, 0, 0, 0);
                half shown = smoothstep(_ShareLow, _ShareHigh, ShareHidden(uv, input.clipPos, flip));
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
                return half4(_BaseColor.rgb, (open > 0.5 ? _Line : _Fill) * shown);
            }
            ENDHLSL
        }
    }
}
