// A word drawn in the world: a place's name on the floor, a gate's, a person's over their head. TextMeshPro's distance-field text (TMP_SDF.shader), with two things of its own.
// It is drawn a few metres nearer the camera than it stands, along its own lines of sight, so it covers exactly the pixels it would and nothing within that reach in front of it
// cuts it: a lamp post across THE PRESS, a well across FARMERS' QUARTER, an awning over Ishen's name (the November 20 capture). And near the camera it holds its size: a word
// nearer than the camera's own distance grows no more than _Band times its made size (PRESSURE LIFT at the foot of Tallow's screen stood twice the size of the words above it).
// On pale ground SignStyle prints a word in dark ink; its edge then turns to a thin pale halo (by the ink's own colour, so every sign keeps one material).
// It sorts with the world's other transparent things by distance, as TextMeshPro's own shader does (drawn earlier, water and glass came down over it). A place's name steps
// aside in a fight (SignFade), so danger markings and floor words don't meet; its plate (SignPlate, Transparent-15) still draws under the markings.
// Bevel, glow and the UI clip are left out: no sign uses them. The underlay (a name's soft shadow) is multi_compile, so a material made at run time keeps it in a player.
Shader "Lattice/SignText" {

Properties {
	_FaceTex			("Face Texture", 2D) = "white" {}
	_FaceUVSpeedX		("Face UV Speed X", Range(-5, 5)) = 0.0
	_FaceUVSpeedY		("Face UV Speed Y", Range(-5, 5)) = 0.0
	_FaceColor		    ("Face Color", Color) = (1,1,1,1)
	_FaceDilate			("Face Dilate", Range(-1,1)) = 0

	_OutlineColor	    ("Outline Color", Color) = (0,0,0,1)
	_OutlineTex			("Outline Texture", 2D) = "white" {}
	_OutlineUVSpeedX	("Outline UV Speed X", Range(-5, 5)) = 0.0
	_OutlineUVSpeedY	("Outline UV Speed Y", Range(-5, 5)) = 0.0
	_OutlineWidth		("Outline Thickness", Range(0, 1)) = 0
	_OutlineSoftness	("Outline Softness", Range(0,1)) = 0

	_Bevel				("Bevel", Range(0,1)) = 0.5
	_BevelOffset		("Bevel Offset", Range(-0.5,0.5)) = 0
	_BevelWidth			("Bevel Width", Range(-.5,0.5)) = 0
	_BevelClamp			("Bevel Clamp", Range(0,1)) = 0
	_BevelRoundness		("Bevel Roundness", Range(0,1)) = 0

	_LightAngle			("Light Angle", Range(0.0, 6.2831853)) = 3.1416
	_SpecularColor	    ("Specular", Color) = (1,1,1,1)
	_SpecularPower		("Specular", Range(0,4)) = 2.0
	_Reflectivity		("Reflectivity", Range(5.0,15.0)) = 10
	_Diffuse			("Diffuse", Range(0,1)) = 0.5
	_Ambient			("Ambient", Range(1,0)) = 0.5

	_BumpMap 			("Normal map", 2D) = "bump" {}
	_BumpOutline		("Bump Outline", Range(0,1)) = 0
	_BumpFace			("Bump Face", Range(0,1)) = 0

	_ReflectFaceColor	("Reflection Color", Color) = (0,0,0,1)
	_ReflectOutlineColor("Reflection Color", Color) = (0,0,0,1)
	_Cube 				("Reflection Cubemap", Cube) = "black" { /* TexGen CubeReflect */ }
	_EnvMatrixRotation	("Texture Rotation", vector) = (0, 0, 0, 0)

	_UnderlayColor	    ("Border Color", Color) = (0,0,0, 0.5)
	_UnderlayOffsetX	("Border OffsetX", Range(-1,1)) = 0
	_UnderlayOffsetY	("Border OffsetY", Range(-1,1)) = 0
	_UnderlayDilate		("Border Dilate", Range(-1,1)) = 0
	_UnderlaySoftness	("Border Softness", Range(0,1)) = 0

	_GlowColor		    ("Color", Color) = (0, 1, 0, 0.5)
	_GlowOffset			("Offset", Range(-1,1)) = 0
	_GlowInner			("Inner", Range(0,1)) = 0.05
	_GlowOuter			("Outer", Range(0,1)) = 0.05
	_GlowPower			("Falloff", Range(1, 0)) = 0.75

	_WeightNormal		("Weight Normal", float) = 0
	_WeightBold			("Weight Bold", float) = 0.5

	_ShaderFlags		("Flags", float) = 0
	_ScaleRatioA		("Scale RatioA", float) = 1
	_ScaleRatioB		("Scale RatioB", float) = 1
	_ScaleRatioC		("Scale RatioC", float) = 1

	_MainTex			("Font Atlas", 2D) = "white" {}
	_TextureWidth		("Texture Width", float) = 512
	_TextureHeight		("Texture Height", float) = 512
	_GradientScale		("Gradient Scale", float) = 5.0
	_ScaleX				("Scale X", float) = 1.0
	_ScaleY				("Scale Y", float) = 1.0
	_PerspectiveFilter	("Perspective Correction", Range(0, 1)) = 0.875
	_Sharpness			("Sharpness", Range(-1,1)) = 0

	_VertexOffsetX		("Vertex OffsetX", float) = 0
	_VertexOffsetY		("Vertex OffsetY", float) = 0

	_MaskCoord			("Mask Coordinates", vector) = (0, 0, 32767, 32767)
	_ClipRect			("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
	_MaskSoftnessX		("Mask SoftnessX", float) = 0
	_MaskSoftnessY		("Mask SoftnessY", float) = 0

	_StencilComp		("Stencil Comparison", Float) = 8
	_Stencil			("Stencil ID", Float) = 0
	_StencilOp			("Stencil Operation", Float) = 0
	_StencilWriteMask	("Stencil Write Mask", Float) = 255
	_StencilReadMask	("Stencil Read Mask", Float) = 255

	_CullMode			("Cull Mode", Float) = 0
	_ColorMask			("Color Mask", Float) = 15

	_Reach				("Drawn this many metres nearer the camera", Float) = 0
	_Band				("Grows no more than this near the camera (0: as perspective has it)", Float) = 0
	_HaloPale			("The halo round dark ink", Color) = (0.93,0.95,1,0.95)
	// Two fifths: at three fifths the pale edge filled a far word's thin strokes, and BOARDING SPINE's slate read at .04 from across the Continuance's deck (WorldLabelTests).
	_HaloThin			("How much of the outline's width it keeps", Float) = 0.4
}

SubShader {

	Tags
	{
		"Queue"="Transparent"
		"IgnoreProjector"="True"
		"RenderType"="Transparent"
	}

	Stencil
	{
		Ref [_Stencil]
		Comp [_StencilComp]
		Pass [_StencilOp]
		ReadMask [_StencilReadMask]
		WriteMask [_StencilWriteMask]
	}

	Cull [_CullMode]
	ZWrite Off
	Lighting Off
	Fog { Mode Off }
	ZTest LEqual
	Blend One OneMinusSrcAlpha
	ColorMask [_ColorMask]

	CGINCLUDE
		#include "UnityCG.cginc"
		#include "UnityUI.cginc"
		#include "Assets/TextMesh Pro/Shaders/TMPro_Properties.cginc"
		#include "Assets/TextMesh Pro/Shaders/TMPro.cginc"

		struct vertex_t
		{
			UNITY_VERTEX_INPUT_INSTANCE_ID
			float4	position		: POSITION;
			float3	normal			: NORMAL;
			fixed4	color			: COLOR;
			float4	texcoord0		: TEXCOORD0;
			float2	texcoord1		: TEXCOORD1;
		};

		struct pixel_t
		{
			UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO
			float4	position		: SV_POSITION;
			fixed4	color			: COLOR;
			float2	atlas			: TEXCOORD0;
			float4	param			: TEXCOORD1;		// alphaClip, scale, bias, weight
			float3	viewDir			: TEXCOORD3;
		    #if UNDERLAY_ON
			float4	texcoord2		: TEXCOORD4;		// u,v, scale, bias
			fixed4	underlayColor	: COLOR1;
		    #endif
		    float4 textures			: TEXCOORD5;
		};

		float4 _FaceTex_ST;
		float4 _OutlineTex_ST;
		int _UIVertexColorAlwaysGammaSpace;
		float _Reach;
		float _Band;
		// The zone camera's own distance from what it follows (CameraProfile.distance), set as a zone loads (SignStyle.Apply): what "made size" is measured at.
		float _SignMadeDistance;
		fixed4 _HaloPale;
		float _HaloThin;

		pixel_t VertShader(vertex_t input)
		{
			pixel_t output;

			UNITY_INITIALIZE_OUTPUT(pixel_t, output);
			UNITY_SETUP_INSTANCE_ID(input);
			UNITY_TRANSFER_INSTANCE_ID(input,output);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

			float bold = step(input.texcoord0.w, 0);

			float4 vert = input.position;
			vert.x += _VertexOffsetX;
			vert.y += _VertexOffsetY;

			// Where it stands, and how far that is from the eye.
			float4 standing = UnityObjectToClipPos(vert);
			float3 world = mul(unity_ObjectToWorld, vert).xyz;
			float3 origin = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;
			float3 eye = _WorldSpaceCameraPos.xyz;
			float far = max(distance(eye, origin), 0.001);
			// Held to its band near the camera: shrunk about its own middle.
			float shrink = (_Band > 0 && _SignMadeDistance > 0) ? min(1.0, _Band * far / _SignMadeDistance) : 1.0;
			world = origin + (world - origin) * shrink;
			// Brought toward the eye along its lines of sight, scaled about the eye: the same pixels, drawn in front of what stands within reach.
			float pull = min(_Reach, far * 0.6);
			world = eye + (world - eye) * ((far - pull) / far);
			float4 vPosition = mul(UNITY_MATRIX_VP, float4(world, 1.0));

			// Its edge's sharpness by the size it is drawn at: where it stands, less what the band took off.
			float2 pixelSize = standing.w / shrink;
			pixelSize /= float2(_ScaleX, _ScaleY) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
			float scale = rsqrt(dot(pixelSize, pixelSize));
			scale *= abs(input.texcoord0.w) * _GradientScale * (_Sharpness + 1);
			if (UNITY_MATRIX_P[3][3] == 0) scale = lerp(abs(scale) * (1 - _PerspectiveFilter), scale, abs(dot(UnityObjectToWorldNormal(input.normal.xyz), normalize(WorldSpaceViewDir(vert)))));

			float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
			weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

			float bias =(.5 - weight) + (.5 / scale);

			float alphaClip = (1.0 - _OutlineWidth * _ScaleRatioA - _OutlineSoftness * _ScaleRatioA);
			alphaClip = alphaClip / 2.0 - ( .5 / scale) - weight;

		    #if UNDERLAY_ON
			float4 underlayColor = _UnderlayColor;
			underlayColor.rgb *= underlayColor.a;

			float bScale = scale;
			bScale /= 1 + ((_UnderlaySoftness*_ScaleRatioC) * bScale);
			float bBias = (0.5 - weight) * bScale - 0.5 - ((_UnderlayDilate * _ScaleRatioC) * 0.5 * bScale);

			float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _TextureWidth;
			float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _TextureHeight;
			float2 bOffset = float2(x, y);
		    #endif

			float2 textureUV = input.texcoord1;
			float2 faceUV = TRANSFORM_TEX(textureUV, _FaceTex);
			float2 outlineUV = TRANSFORM_TEX(textureUV, _OutlineTex);

            if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
            {
                input.color.rgb = UIGammaToLinear(input.color.rgb);
            }
			output.position = vPosition;
			output.color = input.color;
			output.atlas =	input.texcoord0;
			output.param =	float4(alphaClip, scale, bias, weight);
			output.viewDir =	mul((float3x3)_EnvMatrix, _WorldSpaceCameraPos.xyz - mul(unity_ObjectToWorld, vert).xyz);
			#if UNDERLAY_ON
			output.texcoord2 = float4(input.texcoord0 + bOffset, bScale, bBias);
			output.underlayColor =	underlayColor;
			#endif
			output.textures = float4(faceUV, outlineUV);

			return output;
		}

		fixed4 PixShader(pixel_t input) : SV_Target
		{
			UNITY_SETUP_INSTANCE_ID(input);

			float c = tex2D(_MainTex, input.atlas).a;

		    #ifndef UNDERLAY_ON
			clip(c - input.param.x);
		    #endif

			float	scale	= input.param.y;
			float	bias	= input.param.z;
			float	weight	= input.param.w;
			float	sd = (bias - c) * scale;

			// A word on pale ground is printed in dark ink (its vertex colour, SignStyle) and takes a thin pale halo in place of the dark edge white ink wears.
			half ink = step(0.5, 1 - max(input.color.r, max(input.color.g, input.color.b)));
			float outline = (_OutlineWidth * _ScaleRatioA) * scale * lerp(1, _HaloThin, ink);
			float softness = (_OutlineSoftness * _ScaleRatioA) * scale;

			half4 faceColor = _FaceColor;
			half4 outlineColor = lerp(_OutlineColor, _HaloPale, ink);

			faceColor.rgb *= input.color.rgb;

			faceColor *= tex2D(_FaceTex, input.textures.xy + float2(_FaceUVSpeedX, _FaceUVSpeedY) * _Time.y);
			outlineColor *= tex2D(_OutlineTex, input.textures.zw + float2(_OutlineUVSpeedX, _OutlineUVSpeedY) * _Time.y);

			faceColor = GetColor(sd, faceColor, outlineColor, outline, softness);

		    #if UNDERLAY_ON
			float d = tex2D(_MainTex, input.texcoord2.xy).a * input.texcoord2.z;
			faceColor += input.underlayColor * saturate(d - input.texcoord2.w) * (1 - faceColor.a);
		    #endif

  		    return faceColor * input.color.a;
		}

		// A name's dark tag is TextMeshPro's mark (SignStyle.Backed), and TextMeshPro writes every mark after every letter, so in one draw the tag came down over the name it
		// backs: ORRIN's gold at a third of its brightness (WorldLabelTests, November 21). The marks are drawn first, in a pass of their own, and the letters after them. A mark is
		// told by its scale: every letter carries its size in the fourth texture coordinate, a mark none.
		// Outside the view on every side, so the quad is clipped whole.
		static const float4 Nowhere = float4(2, 2, 2, 1);
		pixel_t MarkShader(vertex_t input)
		{
			pixel_t output = VertShader(input);
			if (input.texcoord0.w != 0) output.position = Nowhere;
			return output;
		}
		pixel_t LetterShader(vertex_t input)
		{
			pixel_t output = VertShader(input);
			if (input.texcoord0.w == 0) output.position = Nowhere;
			return output;
		}
	ENDCG

	// URP draws an object's SRPDefaultUnlit pass before its UniversalForward one (DrawObjectsPass).
	Pass {
		Name "Marks"
		Tags { "LightMode"="SRPDefaultUnlit" }
		CGPROGRAM
		#pragma target 3.0
		#pragma vertex MarkShader
		#pragma fragment PixShader
		#pragma multi_compile_local __ UNDERLAY_ON
		ENDCG
	}
	Pass {
		Name "Letters"
		Tags { "LightMode"="UniversalForward" }
		CGPROGRAM
		#pragma target 3.0
		#pragma vertex LetterShader
		#pragma fragment PixShader
		#pragma multi_compile_local __ UNDERLAY_ON
		ENDCG
	}
}

Fallback "TextMeshPro/Distance Field"
CustomEditor "TMPro.EditorUtilities.TMP_SDFShaderGUI"
}
