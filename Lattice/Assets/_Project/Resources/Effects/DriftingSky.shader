// The Skybox/Panoramic look (same mapping, tint and exposure) with one thing added: the sky can take a turn in two axes, from SkyDrift's _SkyTurn.
// x tips it about the world's x axis, which moves it up and down the screen as the camera travels north; y turns it about the world's y axis, across the screen.
// Panoramic turns its vertices by _Rotation and looks up with the unturned ones: looked up from a fixed vertex, as here, that same picture is minus _Rotation.
// LOD 0 throughout: the texture is only ever magnified at this field of view, and a lat-long's wrap would otherwise draw a seam at its mips.
Shader "Lattice/DriftingSky"
{
    Properties
    {
        _MainTex ("Panorama", 2D) = "black" {}
        _Tint ("Tint", Color) = (0.5,0.5,0.5,0.5)
        [Gamma] _Exposure ("Exposure", Range(0,8)) = 1
        _Rotation ("Rotation", Range(0,360)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            half4 _MainTex_HDR;
            half4 _Tint;
            half _Exposure;
            float _Rotation;
            float4 _SkyTurn;
            struct Attributes { float4 vertex:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 position:SV_POSITION; float3 direction:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            float3 AroundY(float3 v,float degrees)
            {
                float s,c;sincos(radians(degrees),s,c);
                return float3(c*v.x-s*v.z,v.y,s*v.x+c*v.z);
            }
            float3 AroundX(float3 v,float degrees)
            {
                float s,c;sincos(radians(degrees),s,c);
                return float3(v.x,c*v.y+s*v.z,c*v.z-s*v.y);
            }
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.position=UnityObjectToClipPos(input.vertex);
                output.direction=AroundY(AroundX(input.vertex.xyz,_SkyTurn.x),-_Rotation-_SkyTurn.y);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float3 d=normalize(input.direction);
                float2 uv=float2(.5,1)-float2(atan2(d.z,d.x),acos(d.y))*float2(.5/UNITY_PI,1/UNITY_PI);
                half3 c=DecodeHDR(tex2Dlod(_MainTex,float4(uv,0,0)),_MainTex_HDR);
                return half4(c*_Tint.rgb*unity_ColorSpaceDouble.rgb*_Exposure,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
