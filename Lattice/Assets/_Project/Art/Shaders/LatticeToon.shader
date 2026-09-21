// Lattice unified toon/ramp shader (URP).
// EVERY model in the game renders through this shader — that is the unification rule.
// Banded diffuse with soft edges, shadow tint, lit-side rim, emission for Hunter/Creator-tech glow.
//
// POLISH-01 extensions (all OFF by default — a material with no keywords renders
// exactly as before):
//   _DETAIL_ON   — two-scale world-space noise breakup for large ground planes
//                  (macro patch variation + micro grain), the "no flat fills" law.
//   _EDGEFADE_ON — soft organic edges for paths/patches/water instead of razor
//                  polygon seams. Coverage comes from UV2 baked by Polish01Tools
//                  (paths/discs) or from object-local slab distance (cubes).
//   _WATER_ON    — depth-tinted water (shore->deep), scrolling ripple/sparkle,
//                  and a noise-warped foam line at the shore.
//   _GLOW_ON     — soft unlit glow (aurora ribbons, god-ray shafts): no lighting,
//                  boosted color for bloom pickup, partial fog participation.
//   _VCOLOR_ON   — multiply vertex color into albedo (the scatter pass bakes its
//                  per-instance tints there). Only for meshes that HAVE colors.
// Pass state (Cull/Blend/ZWrite) is material-driven with opaque defaults so the
// same shader serves opaque terrain and transparent water/glow without breaking
// the SRP batcher (all properties live in one stable CBUFFER).
Shader "Lattice/Toon"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        // P76: authored ground can soften grain without changing shared source art.
        // Default one is the existing texture read for every other material.
        _BaseMapStrength("Base Map Strength", Range(0, 1)) = 1
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _RampSteps("Ramp Steps", Range(2, 6)) = 3
        _ShadowTint("Shadow Tint", Color) = (0.55, 0.55, 0.68, 1)
        _RimColor("Rim Color", Color) = (0.25, 0.25, 0.3, 1)
        _RimPower("Rim Power", Range(0.5, 12)) = 4
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionMap("Emission Map", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        [Toggle(_ALPHATEST_ON)] _AlphaTest("Alpha Clip", Float) = 0

        // ---- POLISH-01: terrain detail ----
        [Toggle(_DETAIL_ON)] _Detail("Terrain Detail", Float) = 0
        _DetailTintA("Detail Tint A (macro)", Color) = (0.86, 0.9, 0.82, 1)
        _DetailTintB("Detail Tint B (micro)", Color) = (1.08, 1.06, 1.02, 1)
        // x = macro scale (1/world-units), y = micro scale, z = macro strength, w = micro strength
        _DetailParams("Detail Params", Vector) = (0.035, 0.55, 0.4, 0.2)

        // ---- POLISH-20: wall strata (the #65 blockout-slab recipe: world-space
        //      horizontal strata banding + crack seams + top-light, scale-agnostic
        //      so stretched kit blocks and giant prim walls read as dressed rock) ----
        [Toggle(_STRATA_ON)] _Strata("Wall Strata", Float) = 0
        _StrataTintA("Strata Band Tint", Color) = (0.8, 0.75, 0.7, 1)
        _StrataTintB("Strata Weather Tint", Color) = (1.12, 1.08, 1.02, 1)
        // x = band frequency (1/world-u), y = band strength, z = crack scale (1/world-u), w = crack strength
        _StrataParams("Strata Params", Vector) = (0.55, 0.5, 0.35, 0.6)

        // ---- POLISH-01: soft edges ----
        [Toggle(_EDGEFADE_ON)] _EdgeFade("Edge Fade", Float) = 0
        // x = mode (0 = path uv2, 1 = disc uv2, 2 = object-local slab), y = fade width (world u),
        // z = edge noise amplitude (world u), w = edge noise scale
        _EdgeParams("Edge Params", Vector) = (0, 0.9, 0.8, 0.5)

        // ---- POLISH-01: water ----
        [Toggle(_WATER_ON)] _Water("Water", Float) = 0
        _DeepColor("Deep Color", Color) = (0.1, 0.3, 0.5, 0.92)
        _FoamColor("Foam Color", Color) = (0.95, 0.99, 1.0, 1)
        // x = wave noise scale, y = scroll speed, z = shore->deep range (world u), w = sparkle strength
        _WaterParams("Water Params", Vector) = (0.35, 0.6, 6.0, 0.5)
        // x = foam width (world u), y = foam noise scale, z = foam noise amplitude (world u), w = lap-line strength
        _FoamParams("Foam Params", Vector) = (0.7, 0.8, 0.5, 0.4)

        // ---- POLISH-01: unlit glow (aurora / shafts) ----
        [Toggle(_GLOW_ON)] _Glow("Glow", Float) = 0
        // x = color boost (HDR, for bloom), y = fog participation 0..1
        _GlowParams("Glow Params", Vector) = (1.3, 0.3, 0, 0)

        // ---- POLISH-01: vertex color (scatter) ----
        [Toggle(_VCOLOR_ON)] _VColor("Vertex Color", Float) = 0
        // POLISH-63 #308: the combined ground-scatter mesh contains a repeated
        // five-vertex pebble whose lit top facet read as the same pale paper chip
        // in every biome.  This is deliberately a material property rather than a
        // global shader change: only Polish_Scatter opts in.  Mid-value,
        // low-chroma, upward-facing facets settle into the ground palette; bright
        // snow lumps, saturated leaves/grass, and every authored paper/sign
        // material remain untouched.
        _ScatterPebbleRerole("Ground Scatter Pebble Re-role", Range(0, 1)) = 0

        // ---- POLISH-61C: vertex wind (foliage/banners/laundry). Height-masked sine
        //      sway along the REGION wind globals (_FbWindG, set by RegionAmbience /
        //      the render tools), keyed off FbTime() so gallery renders stay
        //      deterministic. Applied in ALL depth-writing passes so shadows and
        //      SSAO agree with the color pass. ----
        [Toggle(_WIND_ON)] _Wind("Wind", Float) = 0
        // x = amplitude (world u at full mask), y = frequency (rad/s),
        // z = mask height (object u mapping base->full sway), w = flutter amount 0..1
        _WindParams("Wind Params", Vector) = (0.12, 1.7, 3.0, 0.35)

        // ---- POLISH-06: gel (slime translucency: dense fresnel rim, internal flow
        //      veins, an emissive core that reads strongest through the body) ----
        [Toggle(_GEL_ON)] _Gel("Gel", Float) = 0
        // x = fresnel power, y = core alpha, z = rim alpha, w = flow noise scale (1/obj units)
        _GelParams("Gel Params", Vector) = (2.2, 0.62, 0.94, 1.6)
        // rgb = vein tint multiplier, a = vein strength
        _GelDeepColor("Gel Deep Color", Color) = (0.55, 0.7, 0.9, 0.55)
        [HDR] _GelCoreColor("Gel Core Color", Color) = (0.4, 0.9, 1.2, 1)

        // ---- POLISH-21: ice/crystal translucency read (#31) — fragment-only and
        //      opaque-safe: interior depth pooling, a fresnel rim sheen (emissive,
        //      so glacial edges read under aurora light), sparse facet glints ----
        [Toggle(_ICE_ON)] _Ice("Ice", Float) = 0
        // x = rim power, y = rim strength, z = depth strength, w = glint strength
        _IceParams("Ice Params", Vector) = (2.6, 0.4, 0.5, 0.35)
        _IceDeepColor("Ice Deep Color", Color) = (0.16, 0.34, 0.52, 1)
        [HDR] _IceRimColor("Ice Rim Color", Color) = (0.7, 0.92, 1.05, 1)

        // ---- POLISH-18: family surface program (enemy roster detail without UVs:
        //      counter-shading, segment banding, crack seams, panel lines — all masks
        //      are object-space + noise, normalized by the mesh's baked bounds) ----
        [Toggle(_SURF_ON)] _Surf("Surface Program", Float) = 0
        // 1 = fur (counter-shade + breakup + extremity lightening)
        // 2 = chitin (segment banding + plate scallop + wet sheen)
        // 3 = stone (crack seams + emissive crack cores + weathering)
        // 4 = mech (panel lines + running-light dashes + hazard chevron band)
        _SurfMode("Surf Mode", Float) = 1
        _SurfColor("Surf Secondary", Color) = (1, 1, 1, 1)
        [HDR] _SurfGlow("Surf Glow", Color) = (0, 0, 0, 1)
        // x = detail noise scale (1/obj units), y = strength 0..1, z = band/panel count,
        // w = family extra (fur: saddle, chitin: sheen, stone: crack glow, mech: chevron)
        _SurfParams("Surf Params", Vector) = (1.5, 0.5, 6, 0.5)
        // Object-space mesh bounds for normalized masks: x=minY, y=sizeY, z=minZ, w=sizeZ
        _SurfBounds("Surf Bounds", Vector) = (0, 1, 0, 1)
        // Some authored animal packs use Z-up mesh coordinates under a rotated
        // renderer. Default-off remapping keeps the UV-free anatomy masks aligned
        // without changing any existing Y-up material.
        [Toggle] _SurfZUp("Surf Source Is Z-Up", Float) = 0

        // ---- POLISH-06: KO dissolve (StageActor swaps to a keyworded clone at death
        //      and animates the amount; the ref material bakes the variant into builds) ----
        [Toggle(_DISSOLVE_ON)] _Dissolve("Dissolve", Float) = 0
        // x = dissolve amount 0..1, y = noise scale (1/obj units)
        _DissolveParams("Dissolve Params", Vector) = (0, 3.0, 0, 0)
        [HDR] _DissolveEdgeColor("Dissolve Edge Color", Color) = (2.0, 1.2, 0.4, 1)

        // ---- POLISH-22: camera-occluder screen-door fade (#12). OccluderFade swaps a
        //      blocking renderer to a keyworded clone and drives the amount per-renderer
        //      (MPB). Bayer dither clip, mirrored into the depth passes so the SSAO/depth
        //      prepass agrees with the color pass; the ShadowCaster stays UNCLIPPED on
        //      purpose — a faded roof still throws its shadow, which keeps the world
        //      grounded while you see through it. ----
        [Toggle(_OCCFADE_ON)] _OccFadeToggle("Occluder Fade", Float) = 0
        _OccFade("Occluder Fade Amount", Range(0, 1)) = 0
        // xy = avatar centre in viewport space; zw = compact ellipse half extents.
        // OccluderFade drives this per renderer so only pixels over the avatar ghost.
        _OccFadeEllipse("Occluder Avatar Ellipse", Vector) = (0.5, 0.5, 0.08, 0.14)

        // ---- POLISH-01: material-driven pass state (defaults == the old hardcoded state) ----
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half _BaseMapStrength;
            half4 _BaseColor;
            half _RampSteps;
            half4 _ShadowTint;
            half4 _RimColor;
            half _RimPower;
            half4 _EmissionColor;
            float4 _EmissionMap_ST;
            half _Cutoff;
            half4 _DetailTintA;
            half4 _DetailTintB;
            float4 _DetailParams;
            half4 _StrataTintA;
            half4 _StrataTintB;
            float4 _StrataParams;
            float4 _EdgeParams;
            half4 _DeepColor;
            half4 _FoamColor;
            float4 _WaterParams;
            float4 _FoamParams;
            float4 _GlowParams;
            float4 _GelParams;
            half4 _GelDeepColor;
            half4 _GelCoreColor;
            float4 _IceParams;
            half4 _IceDeepColor;
            half4 _IceRimColor;
            half _SurfMode;
            half4 _SurfColor;
            half4 _SurfGlow;
            float4 _SurfParams;
            float4 _SurfBounds;
            half _SurfZUp;
            float4 _DissolveParams;
            half4 _DissolveEdgeColor;
            half _OccFade;
            float4 _OccFadeEllipse;
            float4 _WindParams;
            half _ScatterPebbleRerole;
        CBUFFER_END

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

        // Global (NOT a material property): the gallery pins this for deterministic
        // renders; 0 in the live game -> real time drives the motion.
        float _FbTimeOverride;

        float FbTime()
        {
            return _FbTimeOverride > 0.0 ? _FbTimeOverride : _Time.y;
        }

        // ---- POLISH-61C atmosphere globals (set per region by RegionAmbience at
        // runtime and by the render tools in editor batch; all-zero = system off,
        // which is the pre-61C look, so scenes without an authority are safe). ----
        float4 _FbAerialFar;     // rgb = far haze color (== region fog color == sky horizon)
        float4 _FbAerialNear;    // rgb = mid-distance atmosphere tint
        float4 _FbAerialDist;    // x = start dist, y = 1/(end-start), z = strength, w = high-altitude floor
        float4 _FbAerialHeightG; // x = haze ceiling (world Y), y = 1/fade band
        float4 _FbWindG;         // x,y = wind dir XZ (normalized), z = strength 0..1, w = unused
        float _FbCloudShadowStrength;

        // POLISH-61C: the whole scene agrees on the wind — smoke/cloth/cloud drift
        // scripts read the same _FbWindG the shader sways by. Height mask squares
        // toward the base so trunks hold still while crowns ride the gusts.
        void FbApplyWind(inout float3 positionOS)
        {
            float amp = _WindParams.x * _FbWindG.z;
            float mask = saturate(positionOS.y / max(_WindParams.z, 0.001));
            mask *= mask;
            if (amp * mask <= 0.0001)
                return;
            float3 pivotWS = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
            float t = FbTime();
            float phase = dot(pivotWS.xz, float2(0.437, 0.293));
            float gust = 0.72 + 0.28 * sin(t * 0.53 + phase * 0.31);
            float sway = sin(t * _WindParams.y + phase) * (1.0 - _WindParams.w * 0.4)
                       + sin(t * _WindParams.y * 2.33 + phase * 1.7 + positionOS.x * 2.1)
                         * _WindParams.w * 0.6;
            float2 dir2 = _FbWindG.xy;
            float3 offsetWS = float3(dir2.x, 0.0, dir2.y) * (sway * gust * amp * mask);
            // A slight settle on the swing keeps canopies from pure lateral shearing.
            offsetWS.y = -abs(sway) * gust * amp * mask * 0.18;
            positionOS += mul((float3x3)GetWorldToObjectMatrix(), offsetWS);
        }

        // ---- cheap deterministic 2D value noise (no textures) ----
        float FbHash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 34.345);
            return frac(p.x * p.y);
        }

        float FbNoise2(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            float2 u = f * f * (3.0 - 2.0 * f);
            float a = FbHash21(i);
            float b = FbHash21(i + float2(1, 0));
            float c = FbHash21(i + float2(0, 1));
            float d = FbHash21(i + float2(1, 1));
            return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
        }

        float FbFbm2(float2 p)
        {
            return FbNoise2(p) * 0.667 + FbNoise2(p * 2.13 + 17.7) * 0.333;
        }

        // 4x4 Bayer screen-door threshold (POLISH-22 occluder fade). At full fade the
        // 0.82 cap in the callers keeps ~1/5 of the pixel lattice alive, so the blocker
        // still reads as a ghost silhouette instead of popping out of existence.
        half FbBayer4(float2 pixel)
        {
            uint2 p = uint2(pixel) & 3u;
            uint idx = p.y * 4u + p.x;
            static const half m[16] = { 0.03125h, 0.53125h, 0.15625h, 0.65625h,
                                        0.78125h, 0.28125h, 0.90625h, 0.40625h,
                                        0.21875h, 0.71875h, 0.09375h, 0.59375h,
                                        0.96875h, 0.46875h, 0.84375h, 0.34375h };
            return m[idx];
        }

        // bug-20260820-144100 / -144131: the old renderer-wide clip could ghost a
        // whole cottage or erase a whole tree just because one AABB sightline touched
        // the player. Preserve the blocker everywhere except this compact avatar-sized
        // screen ellipse. The soft outer fifth keeps the hole from reading as a decal;
        // every pass which writes camera color/depth calls this same mask.
        half FbOccFadeMask(float4 positionCS)
        {
            float2 screenUv = GetNormalizedScreenSpaceUV(positionCS);
            float2 radius = max(_OccFadeEllipse.zw, float2(0.0001, 0.0001));
            float2 ellipse = (screenUv - _OccFadeEllipse.xy) / radius;
            half distanceSq = dot(ellipse, ellipse);
            return 1.0h - smoothstep(0.64h, 1.0h, distanceSq);
        }

        // Distance (world units) from this fragment to the mesh's soft edge.
        //   mode 0 (paths): uv2.x = ribbon width, uv2.y = world dist to the nearer END;
        //                   the side tent comes from uv0.x (0..1 across the ribbon).
        //   mode 1 (discs): uv2.y = world dist to the rim (baked by Polish01Tools).
        //   mode 2 (cubes): object-local distance to the cube's XZ faces (extent 0.5).
        //   mode 3 (built-in Plane prims): same, extent 5.
        float FbEdgeWorld(float2 uv, float2 uv2, float3 positionWS)
        {
            int mode = (int)(_EdgeParams.x + 0.5);
            if (mode == 0)
            {
                float side01 = 1.0 - abs(uv.x * 2.0 - 1.0);
                return min(side01 * uv2.x * 0.5, uv2.y);
            }
            if (mode == 1)
                return uv2.y;
            float extent = mode == 3 ? 5.0 : 0.5;
            float3 posOS = mul(GetWorldToObjectMatrix(), float4(positionWS, 1.0)).xyz;
            float sx = length(GetObjectToWorldMatrix()._m00_m10_m20);
            float sz = length(GetObjectToWorldMatrix()._m02_m12_m22);
            float ex = (extent - abs(posOS.x)) * sx;
            float ez = (extent - abs(posOS.z)) * sz;
            return max(min(ex, ez), 0.0);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _DETAIL_ON
            #pragma shader_feature_local_fragment _STRATA_ON
            #pragma shader_feature_local_fragment _EDGEFADE_ON
            #pragma shader_feature_local_fragment _WATER_ON
            #pragma shader_feature_local_fragment _GLOW_ON
            #pragma shader_feature_local _VCOLOR_ON
            #pragma shader_feature_local _GEL_ON
            #pragma shader_feature_local_fragment _ICE_ON
            #pragma shader_feature_local_fragment _SURF_ON
            #pragma shader_feature_local_fragment _DISSOLVE_ON
            #pragma shader_feature_local_fragment _OCCFADE_ON
            #pragma shader_feature_local _WIND_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                #if defined(_VCOLOR_ON)
                half4 color : COLOR;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                float2 uv2 : TEXCOORD4;
                #if defined(_VCOLOR_ON)
                half4 color : TEXCOORD5;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                #if defined(_GEL_ON)
                {
                    // POLISH-18 jelly wobble: a slow volume-ish shimmy (relative, so it
                    // survives any body scale). Color pass only — the shadow blob and
                    // depth passes skip it; at 2% amplitude the mismatch is invisible.
                    float t = FbTime();
                    float w = sin(t * 2.6 + input.positionOS.y * 9.0 + input.positionOS.x * 5.0) * 0.02;
                    input.positionOS.xz *= 1.0 + w;
                    input.positionOS.y *= 1.0 - w * 0.6;
                }
                #endif

                #if defined(_WIND_ON)
                FbApplyWind(input.positionOS.xyz);
                #endif

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.uv2 = input.uv2;
                #if defined(_VCOLOR_ON)
                output.color = input.color;
                #endif
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            // Banded ramp with screen-space anti-aliased band edges.
            half ToonRamp(half lambert)
            {
                half steps = max(_RampSteps, 2.0h);
                half x = saturate(lambert) * steps;
                half band = floor(x);
                half f = x - band;
                half aa = max(fwidth(x), 1e-3h);
                half soft = smoothstep(0.5h - aa, 0.5h + aa, f);
                return saturate((band + soft) / steps);
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                baseTex.rgb = lerp(half3(1, 1, 1), baseTex.rgb, _BaseMapStrength);
                half4 albedo = baseTex * _BaseColor;
                #if defined(_VCOLOR_ON)
                albedo *= input.color;
                // #308 is a rendered-read defect, not a support defect.  The
                // generator already places the chips on the floor; darkening the
                // low-chroma upward facets makes them read as embedded pebbles
                // instead of identical loose paper without flattening foliage or
                // turning the deliberately near-white snow lumps grey.
                half scatterHi = max(input.color.r, max(input.color.g, input.color.b));
                half scatterLo = min(input.color.r, min(input.color.g, input.color.b));
                half scatterSat = (scatterHi - scatterLo) / max(scatterHi, 0.001h);
                half scatterMid = smoothstep(0.20h, 0.32h, scatterHi)
                                * (1.0h - smoothstep(0.72h, 0.84h, scatterHi));
                half scatterStone = 1.0h - smoothstep(0.38h, 0.55h, scatterSat);
                half scatterUp = smoothstep(0.25h, 0.72h,
                    abs(normalize(input.normalWS).y));
                half scatterMask = saturate(_ScatterPebbleRerole
                    * scatterMid * scatterStone * scatterUp);
                albedo.rgb *= lerp(1.0h, 0.55h, scatterMask);
                #endif
                #ifdef _ALPHATEST_ON
                clip(albedo.a - _Cutoff);
                #endif

                #if defined(_OCCFADE_ON)
                clip(FbBayer4(input.positionCS.xy)
                     - _OccFade * 0.82h * FbOccFadeMask(input.positionCS));
                #endif

                half alphaOut = albedo.a;
                half3 sparkle = 0;
                half3 gelCore = 0;
                half3 dissolveEdge = 0;
                half3 surfEmiss = 0;

                #if defined(_DISSOLVE_ON)
                {
                    float3 posOS = mul(GetWorldToObjectMatrix(), float4(input.positionWS, 1.0)).xyz;
                    float n = FbFbm2(posOS.xy * _DissolveParams.y + posOS.zx * (_DissolveParams.y * 0.73) + 7.31);
                    float d = n - _DissolveParams.x * 1.08;
                    clip(d);
                    // A burning rim right at the dissolve frontier.
                    dissolveEdge = _DissolveEdgeColor.rgb
                                   * (1.0 - smoothstep(0.0, 0.14, d))
                                   * step(0.001, _DissolveParams.x);
                }
                #endif

                #if defined(_GEL_ON)
                {
                    float t = FbTime();
                    float3 viewDirGel = normalize(GetWorldSpaceViewDir(input.positionWS));
                    float fres = pow(1.0 - saturate(abs(dot(viewDirGel, normalize(input.normalWS)))),
                        max(_GelParams.x, 0.5));
                    // Internal flow: slow veins drifting upward through the body.
                    float3 posOS = mul(GetWorldToObjectMatrix(), float4(input.positionWS, 1.0)).xyz;
                    float flow = FbFbm2(posOS.xz * _GelParams.w + float2(0.0, -t * 0.22))
                               * 0.6
                               + FbNoise2(posOS.xy * (_GelParams.w * 1.9) + float2(t * 0.07, -t * 0.31))
                               * 0.4;
                    albedo.rgb = lerp(albedo.rgb, albedo.rgb * _GelDeepColor.rgb,
                        smoothstep(0.38, 0.78, flow) * _GelDeepColor.a);
                    // Jelly optics: dense silhouette rim, clearer heart.
                    alphaOut = lerp(_GelParams.y, _GelParams.z, fres);
                    // The emissive core reads strongest looking through the middle.
                    float corePulse = 0.78 + 0.22 * sin(t * 2.3);
                    gelCore = _GelCoreColor.rgb * (1.0 - fres) * corePulse;
                    // POLISH-18 wet glint: a tight fresnel band sells the jelly surface
                    // at battle distance (rides the existing lit-gated sparkle add).
                    sparkle += smoothstep(0.55, 0.72, fres) * smoothstep(0.92, 0.78, fres) * 0.55;
                }
                #endif

                #if defined(_SURF_ON)
                {
                    // POLISH-18 family surface program. All masks are object-space
                    // (normalized by the mesh bounds the generator bakes into
                    // _SurfBounds), so they need no UVs and hold up under animation.
                    float3 posOS = mul(GetWorldToObjectMatrix(), float4(input.positionWS, 1.0)).xyz;
                    posOS = lerp(posOS, posOS.xzy, step(0.5, _SurfZUp));
                    float nY = saturate((posOS.y - _SurfBounds.x) / max(_SurfBounds.y, 1e-4));
                    float nZ = saturate((posOS.z - _SurfBounds.z) / max(_SurfBounds.w, 1e-4));
                    int surfMode = (int)(_SurfMode + 0.5);
                    float str = _SurfParams.y;
                    float breakup = FbFbm2(posOS.xz * _SurfParams.x
                                           + posOS.yy * (_SurfParams.x * 0.71) + 3.7);

                    if (surfMode == 1)
                    {
                        // FUR: counter-shading (light belly/paws, darker saddle),
                        // muzzle/tail-tip lightening, organic tonal breakup.
                        float belly = smoothstep(0.34, 0.04, nY);
                        float paws = smoothstep(0.14, 0.02, nY);
                        float snoutTail = smoothstep(0.78, 0.97, abs(nZ * 2.0 - 1.0))
                                          * step(0.3, nY) * 0.55;
                        float lightMask = saturate(belly + paws + snoutTail);
                        albedo.rgb = lerp(albedo.rgb, _SurfColor.rgb, lightMask * str);
                        float saddle = smoothstep(0.6, 0.95, nY) * _SurfParams.w * (1.0 - lightMask);
                        albedo.rgb *= 1.0 - saddle * 0.22;
                        albedo.rgb *= 1.0 + (breakup - 0.5) * 0.26 * str;
                    }
                    else if (surfMode == 2)
                    {
                        // CHITIN: segment banding along the body with dark seams,
                        // per-plate scallop shading, and a wet carapace sheen band.
                        float seg = frac(nZ * _SurfParams.z + (breakup - 0.5) * 0.22);
                        float edgeD = min(seg, 1.0 - seg);
                        float seam = 1.0 - smoothstep(0.03, 0.1, edgeD);
                        albedo.rgb *= 1.0 - seam * 0.38 * str;
                        albedo.rgb *= lerp(1.05, 0.9, seg * str);
                        albedo.rgb = lerp(albedo.rgb, albedo.rgb * _SurfColor.rgb,
                                          smoothstep(0.55, 0.8, breakup) * 0.5 * str);
                        float3 vDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                        float f = pow(1.0 - saturate(abs(dot(vDir, normalize(input.normalWS)))), 2.0);
                        sparkle += smoothstep(0.32, 0.5, f) * smoothstep(0.85, 0.6, f)
                                   * _SurfParams.w * 0.6;
                    }
                    else if (surfMode == 3)
                    {
                        // STONE: crack seams darkening, an emissive core inside the
                        // deepest cracks (ember/moss per _SurfGlow), weathering patches.
                        float vein = abs(FbFbm2(posOS.xz * (_SurfParams.x * 1.7)
                                                + posOS.yy * _SurfParams.x + 11.3) - 0.5);
                        float crack = 1.0 - smoothstep(0.02, 0.075, vein);
                        float crackCore = 1.0 - smoothstep(0.0, 0.028, vein);
                        albedo.rgb *= 1.0 - crack * 0.5 * str;
                        surfEmiss = _SurfGlow.rgb * crackCore * _SurfParams.w;
                        // Weathering stays IN and NEAR the cracks — a body-wide wash
                        // turns stone golems into green skeletons (mountain sheet, r3).
                        albedo.rgb = lerp(albedo.rgb, albedo.rgb * _SurfColor.rgb,
                                          smoothstep(0.62, 0.85, breakup) * crack * 0.8);
                        albedo.rgb *= 1.0 + (breakup - 0.5) * 0.14 * str;
                    }
                    else
                    {
                        // MECH: panel-line grid over (length, height), running-light
                        // dashes on the upper seams, optional hazard chevron band.
                        float2 cellUV = float2(nZ * _SurfParams.z,
                                               nY * _SurfParams.z * 0.62)
                                        + (breakup - 0.5) * 0.16;
                        float2 g = abs(frac(cellUV) - 0.5);
                        float line01 = smoothstep(0.415, 0.48, max(g.x, g.y));
                        albedo.rgb *= 1.0 - line01 * 0.34 * str;
                        // Panel tonal steps so adjacent plates read as separate parts.
                        float plate = FbHash21(floor(cellUV) + 5.7);
                        albedo.rgb *= lerp(0.94, 1.06, plate);
                        // Running lights: dashes riding the horizontal seams, upper body.
                        float dash = step(0.62, frac(cellUV.x * 3.0 + plate))
                                     * smoothstep(0.4, 0.46, g.y) * step(0.45, nY);
                        float pulse = 0.7 + 0.3 * sin(FbTime() * 2.0 + plate * 6.28);
                        surfEmiss += _SurfGlow.rgb * dash * pulse;
                        // Hazard chevrons: one belt of diagonal stripes, mid body.
                        float belt = smoothstep(0.26, 0.3, nY) * smoothstep(0.44, 0.4, nY)
                                     * _SurfParams.w;
                        float stripe = step(0.5, frac((posOS.x + posOS.y)
                                                      * (_SurfParams.z * 1.4)));
                        albedo.rgb = lerp(albedo.rgb,
                                          lerp(half3(0.08, 0.07, 0.06), _SurfColor.rgb, stripe),
                                          belt * str);
                    }
                }
                #endif

                #if defined(_DETAIL_ON)
                {
                    float macro = FbFbm2(input.positionWS.xz * _DetailParams.x);
                    float micro = FbNoise2(input.positionWS.xz * _DetailParams.y);
                    // Painter's-pass value islands: the old one-sided multiply only
                    // darkened the field and then collapsed back into a single toon
                    // band in foggy/dark scenes. Cross the material's two palette
                    // tints instead, so broad world-space patches contain both a
                    // shadow family and a lifted family without introducing a
                    // texture or an off-palette color.
                    half3 macroTint = lerp(_DetailTintA.rgb, _DetailTintB.rgb,
                        smoothstep(0.2, 0.8, macro));
                    albedo.rgb = lerp(albedo.rgb, albedo.rgb * macroTint,
                        saturate(_DetailParams.z));
                    albedo.rgb = lerp(albedo.rgb, albedo.rgb * _DetailTintB.rgb,
                        smoothstep(0.45, 0.8, micro) * _DetailParams.w * 0.45);
                }
                #endif

                #if defined(_STRATA_ON)
                {
                    // Horizontal strata bands keyed on world height (a slow XZ drift
                    // keeps band edges organic), crack seams in a 3D-ish noise, and a
                    // faint top-light so upward faces read as dusted ledges.
                    float band = FbNoise2(float2(input.positionWS.y * _StrataParams.x,
                                                 dot(input.positionWS.xz, float2(0.11, 0.07))));
                    albedo.rgb = lerp(albedo.rgb, albedo.rgb * _StrataTintA.rgb,
                        smoothstep(0.42, 0.60, band) * _StrataParams.y);
                    float vein = abs(FbFbm2(input.positionWS.xz * _StrataParams.z
                                            + input.positionWS.yy * (_StrataParams.z * 0.85)
                                            + 7.7) - 0.5);
                    float seam = 1.0 - smoothstep(0.012, 0.05, vein);
                    albedo.rgb *= 1.0 - seam * 0.5 * _StrataParams.w;
                    float weather = FbNoise2(input.positionWS.xz * (_StrataParams.z * 0.45)
                                             + input.positionWS.yy * (_StrataParams.z * 0.3) + 3.1);
                    albedo.rgb = lerp(albedo.rgb, albedo.rgb * _StrataTintB.rgb,
                        smoothstep(0.55, 0.85, weather) * 0.5 * _StrataParams.w);
                    albedo.rgb *= 1.0 + saturate(input.normalWS.y) * 0.06;
                }
                #endif

                #if defined(_ICE_ON)
                {
                    // POLISH-21 #31: interior depth pools in a slow 3D-ish noise
                    // (deep saturated color where the mass reads thick), a fresnel
                    // rim sheen carried as emission (glacier edges catch the aurora
                    // even in dark scenes), and sparse lit facet glints.
                    float3 vIce = normalize(GetWorldSpaceViewDir(input.positionWS));
                    float fresIce = pow(1.0 - saturate(abs(dot(vIce, normalize(input.normalWS)))),
                        max(_IceParams.x, 0.5));
                    float depthIce = FbFbm2(input.positionWS.xz * 0.5
                                            + input.positionWS.yy * 0.37 + 13.7);
                    albedo.rgb = lerp(albedo.rgb, _IceDeepColor.rgb,
                        smoothstep(0.36, 0.85, depthIce) * _IceParams.z * (1.0 - fresIce * 0.55));
                    surfEmiss += _IceRimColor.rgb * fresIce * _IceParams.y;
                    float glint = FbNoise2(input.positionWS.xz * 7.0 + input.positionWS.yy * 5.0);
                    sparkle += smoothstep(0.86, 0.965, glint) * _IceParams.w;
                }
                #endif

                #if defined(_WATER_ON)
                {
                    float t = FbTime();
                    float edge = FbEdgeWorld(input.uv, input.uv2, input.positionWS);
                    float edgeN = edge + (FbFbm2(input.positionWS.xz * _FoamParams.y + t * 0.13)
                                          - 0.5) * _FoamParams.z * 2.0;
                    float depthT = smoothstep(0.0, max(_WaterParams.z, 0.01), edgeN);
                    albedo.rgb = lerp(albedo.rgb, _DeepColor.rgb, depthT);
                    alphaOut = lerp(_BaseColor.a, _DeepColor.a, depthT);

                    float2 p = input.positionWS.xz * _WaterParams.x;
                    float n1 = FbNoise2(p + t * _WaterParams.y * float2(1.0, 0.6));
                    float n2 = FbNoise2(p * 1.9 + 31.7 - t * _WaterParams.y * float2(0.7, 1.0));
                    float ripple = (n1 + n2) * 0.5;
                    albedo.rgb *= 0.93 + ripple * 0.14;
                    sparkle = smoothstep(0.62, 0.8, n1 * n2 * 2.0) * _WaterParams.w;

                    float foam = 1.0 - smoothstep(_FoamParams.x * 0.35, _FoamParams.x, edgeN);
                    float lap = saturate(1.0 - abs(edgeN - _FoamParams.x * 2.2)
                                               / max(_FoamParams.x * 0.45, 0.01));
                    lap *= _FoamParams.w * smoothstep(0.35, 0.65,
                        FbNoise2(input.positionWS.xz * _FoamParams.y * 1.7 + t * 0.21));
                    half foamMix = saturate(foam + lap);
                    albedo.rgb = lerp(albedo.rgb, _FoamColor.rgb, foamMix);
                    alphaOut = max(alphaOut, foamMix * 0.92h);
                }
                #endif

                #if defined(_EDGEFADE_ON)
                {
                    float edge = FbEdgeWorld(input.uv, input.uv2, input.positionWS)
                                 + (FbFbm2(input.positionWS.xz * _EdgeParams.w) - 0.5) * _EdgeParams.z * 2.0;
                    alphaOut *= smoothstep(0.0, max(_EdgeParams.y, 0.01), edge);
                }
                #endif

                float3 normalWS = normalize(input.normalWS) * IS_FRONT_VFACE(frontFace, 1.0, -1.0);

                #if defined(_GLOW_ON)
                {
                    half3 glow = albedo.rgb * _GlowParams.x
                               + SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb
                                 * _EmissionColor.rgb;
                    // POLISH-61C: glow participates in the aerial haze by the same
                    // knob as fog (aurora stays sky-bright, distant shafts melt).
                    if (_FbAerialDist.z > 0.0001)
                    {
                        float viewDistG = distance(_WorldSpaceCameraPos, input.positionWS);
                        float dTg = saturate((viewDistG - _FbAerialDist.x) * _FbAerialDist.y);
                        dTg = dTg * dTg * (3.0 - 2.0 * dTg);
                        half3 aColG = lerp(_FbAerialNear.rgb, _FbAerialFar.rgb, dTg);
                        glow = lerp(glow, aColG,
                            dTg * _FbAerialDist.z * saturate(_GlowParams.y));
                    }
                    half3 fogged = MixFog(glow, input.fogFactor);
                    return half4(lerp(glow, fogged, saturate(_GlowParams.y)), alphaOut);
                }
                #endif

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                #if defined(_LIGHT_COOKIES)
                // POLISH-63 i6 (#385): the cookie is a cloud SHADOW, and a shadow has a floor.
                // A near-black cookie texel at cloudShadowStrength 1 multiplied the sun to ~2%
                // — BramblewoodOutpost's ground crushed to (25,21,17) while the Staggart grass
                // standing on it (a different shader, no cookie multiply) stayed lit. Real
                // shadowed ground keeps 55% via _ShadowTint, so the darkest cloud keeps 35%:
                // visibly a passing cloud, never a hole in the world. ~40 region profiles ship
                // strength 1, so the floor belongs here, not in the data.
                mainLight.color *= lerp(
                    half3(1, 1, 1),
                    max(SampleMainLightCookie(input.positionWS), half3(0.35, 0.35, 0.35)),
                    saturate(_FbCloudShadowStrength));
                #endif

                half ndotl = dot(normalWS, mainLight.direction);
                half lit = ToonRamp(ndotl * mainLight.shadowAttenuation * mainLight.distanceAttenuation);
                half3 lighting = lerp(_ShadowTint.rgb, half3(1, 1, 1), lit) * mainLight.color;

                // Additional lights: smooth, scaled down so the sun stays the author of the look.
                #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                for (uint li = 0u; li < count; li++)
                {
                    Light light = GetAdditionalLight(li, input.positionWS);
                    half contribution = saturate(dot(normalWS, light.direction))
                        * light.distanceAttenuation * light.shadowAttenuation;
                    lighting += light.color * ToonRamp(contribution) * 0.6h;
                }
                #endif

                // Ambient from spherical harmonics keeps shadow sides alive.
                half3 ambient = SampleSH(normalWS) * 0.55h;

                // POLISH-61C SSAO: the renderer feature ran since P35 but no pass
                // here ever consumed its term — wire it into ambient (full) and
                // direct (by the feature's DirectLightingStrength). Opaque only:
                // water/gel/glow sample opaque depth behind them and would smear.
                #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_WATER_ON) && !defined(_GEL_ON)
                {
                    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(
                        GetNormalizedScreenSpaceUV(input.positionCS));
                    lighting *= aoFactor.directAmbientOcclusion;
                    ambient *= aoFactor.indirectAmbientOcclusion;
                }
                #endif

                // Lit-side rim for silhouette pop.
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                half rim = pow(1.0h - saturate(dot(viewDir, normalWS)), _RimPower);
                half3 rimColor = _RimColor.rgb * rim * lit;

                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;

                half3 color = albedo.rgb * (lighting + ambient) + rimColor + emission
                              + gelCore + dissolveEdge + surfEmiss + sparkle * lit;

                // POLISH-61C aerial perspective: distance+height COLOR haze under the
                // classic linear fog. Far color == region fog color == sky horizon
                // (Polish01 BuildSkies), so ridgelines melt into the sky seamlessly;
                // the near tint gives mid-distance masses a value step the flat fog
                // never had. All-zero globals (no authority) = pre-61C pixels.
                if (_FbAerialDist.z > 0.0001)
                {
                    float viewDist = distance(_WorldSpaceCameraPos, input.positionWS);
                    float dT = saturate((viewDist - _FbAerialDist.x) * _FbAerialDist.y);
                    dT = dT * dT * (3.0 - 2.0 * dT);
                    float above = smoothstep(0.0, 1.0,
                        (input.positionWS.y - _FbAerialHeightG.x) * _FbAerialHeightG.y);
                    float hT = lerp(1.0, _FbAerialDist.w, above);
                    half3 aCol = lerp(_FbAerialNear.rgb, _FbAerialFar.rgb, dT);
                    color = lerp(color, aCol, dT * hT * _FbAerialDist.z);
                }

                #if defined(_OCCFADE_ON)
                // POLISH-63 (ledger #316). The Bayer clip above removes pixels but the
                // ones it leaves kept the blocker's full albedo, so a half-faded red
                // trunk stippled against green foliage read as compression artefacting
                // rather than as a fade — the four-judge walkthrough panel called it a
                // "salmon screen-door wash" and found it on every forested and rocky
                // walk in the game. Contrast, not lattice size, is what made it legible.
                // Ghost pixels now walk toward the region's own near-haze colour (the
                // POLISH-61C aerial authority, which is the scene's horizon by
                // construction) as the fade rises, so the survivors lose their contrast
                // before they lose their pixels. Scenes with no aerial authority fall
                // back to desaturating in place, which needs no global. The silhouette
                // is deliberately not erased: OccluderFade's whole premise is that the
                // blocker stays readable as a ghost.
                {
                    half occGhost = saturate(_OccFade) * 0.78h
                                    * FbOccFadeMask(input.positionCS);
                    half3 ghostHaze = _FbAerialDist.z > 0.0001
                        ? _FbAerialNear.rgb
                        : lerp(color, dot(color, half3(0.299h, 0.587h, 0.114h)).xxx, 0.85h);
                    color = lerp(color, ghostHaze, occGhost);
                }
                #endif

                color = MixFog(color, input.fogFactor);
                return half4(color, alphaOut);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _WIND_ON
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                #if defined(_WIND_ON)
                FbApplyWind(input.positionOS.xyz);
                #endif

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                output.positionCS = positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _OCCFADE_ON
            #pragma shader_feature_local _WIND_ON
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                #if defined(_WIND_ON)
                FbApplyWind(input.positionOS.xyz);
                #endif
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                #endif
                #if defined(_OCCFADE_ON)
                clip(FbBayer4(input.positionCS.xy)
                     - _OccFade * 0.82h * FbOccFadeMask(input.positionCS));
                #endif
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _OCCFADE_ON
            #pragma shader_feature_local _WIND_ON
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD0;
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                #if defined(_WIND_ON)
                FbApplyWind(input.positionOS.xyz);
                #endif
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 DepthNormalsFrag(Varyings input) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                #endif
                #if defined(_OCCFADE_ON)
                clip(FbBayer4(input.positionCS.xy)
                     - _OccFade * 0.82h * FbOccFadeMask(input.positionCS));
                #endif
                return half4(normalize(input.normalWS), 0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
