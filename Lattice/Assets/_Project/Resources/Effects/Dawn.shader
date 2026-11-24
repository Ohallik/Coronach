Shader "Lattice/Dawn"
{
    // Chapter 9's dawn line (World's DawnLine sets _DawnAxis every frame, and for an editor still as it renders): xy is the way the line comes on, on the plan; z is where the line is
    // along that way. A point's "ahead" is how far it stands in front of the edge, in metres: positive in the dusk, negative in the light.
    //   Kind 0, the light (drawn again over the floor and the walls; the Terminator adds it, Blend One One): the ground behind the edge lit hot, a heat shimmer through it, a
    //   crisp brighter band at the edge itself. Pale gold-white, never a marking's orange; nothing ahead of the edge.
    //   Kind 1, frost (Blend SrcAlpha OneMinusSrcAlpha): rime lying ahead of the edge, as its vertex alpha lays it; it flashes white just behind the edge and is gone.
    //   Kind 2, the veil (Blend One One, on a strip standing at the edge): a low wall of light fading upward (uv.y) and toward its ends (uv.x).
    // Drawn after the ground's flat details and before every danger marking, which keeps its rim over it.
    Properties
    {
        _Kind ("Kind: 0 the light, 1 frost, 2 the veil", Float) = 0
        [HDR] _Light ("Light behind the edge", Color) = (2.3,2.0,1.6,1)
        [HDR] _Edge ("The edge's band", Color) = (3.2,2.8,2.1,1)
        _EdgeWidth ("Edge band (m)", Float) = 0.45
        _Shimmer ("Heat shimmer", Range(0,1)) = 0.3
        [HDR] _Frost ("Frost", Color) = (0.74,0.84,0.98,0.62)
        [HDR] _Flash ("Frost burning off", Color) = (2.2,2.1,1.9,1)
        _FlashWidth ("Flash band (m)", Float) = 1.1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src blend", Float) = 2
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst blend", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-15" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "Dawn"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Kind;
                half4 _Light;
                half4 _Edge;
                float _EdgeWidth;
                float _Shimmer;
                half4 _Frost;
                half4 _Flash;
                float _FlashWidth;
                float _SrcBlend;
                float _DstBlend;
            CBUFFER_END
            float4 _DawnAxis;
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float2 uv:TEXCOORD1; half4 color:COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(output.positionWS);
                output.uv=input.uv;
                output.color=input.color;
                return output;
            }
            // A cell's number, folded small first: the band runs to z 760, and a cell index of thousands times 456 is past where a float keeps its fraction (3 million: quarters),
            // so the old hash fell into a few values in rows and columns, and the rime's glints were a regular grid of white squares (the November 22 capture, s55/152).
            float Hash(float2 p){p=p-289.0*floor(p/289.0);float3 q=frac(float3(p.xyx)*0.1031);q+=dot(q,q.yzx+33.33);return frac((q.x+q.y)*q.z);}
            float Noise(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+float2(1,1)),f.x),f.y);
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 axis=_DawnAxis.xy;
                float ahead=dot(input.positionWS.xz,axis)-_DawnAxis.z;
                float px=max(fwidth(ahead),0.002);
                // 1 behind the edge (in the light), 0 ahead of it: a hard edge, a pixel soft.
                float lit=1-smoothstep(-px,px,ahead);
                if(_Kind<0.5)
                {
                    float t=_Time.y;
                    float2 p=input.positionWS.xz;
                    float shimmer=Noise(p*0.42+float2(t*0.9,t*0.35))*0.6+Noise(p*1.3-float2(t*0.4,t*1.1))*0.4;
                    float band=lit*(1-smoothstep(0,_EdgeWidth,-ahead));
                    half3 boost=_Light.rgb*(1-_Shimmer+_Shimmer*shimmer)*lit+_Edge.rgb*band;
                    return half4(boost,1);
                }
                if(_Kind<1.5)
                {
                    float keep=smoothstep(-px,px,ahead);
                    float flash=(1-keep)*(1-smoothstep(0,_FlashWidth,-ahead));
                    float2 p=input.positionWS.xz;
                    // Rime lies in grains and drifts on the plates, the ground showing between: laid evenly at its alpha over a few metres it was a pale see-through sheet over
                    // the ground, and its glints (11 cm cells lit whole) were squares. Here it is patchy at three sizes, thickest where it has drifted, with no hard-edged cell anywhere.
                    float grain=Noise(p*0.9)*0.4+Noise(p*2.7+17.0)*0.32+Noise(p*7.3+41.0)*0.28;
                    float lie=smoothstep(0.38,0.7,grain);
                    half3 colour=lerp(_Frost.rgb*(0.74+0.3*lie),_Flash.rgb,flash);
                    half alpha=input.color.a*(_Frost.a*keep*lie*0.9+0.85*flash*(0.35+0.65*lie));
                    return half4(colour,saturate(alpha));
                }
                float up=saturate(input.uv.y);float along=saturate(input.uv.x);
                float ends=smoothstep(0,0.18,along)*smoothstep(1,0.82,along);
                float fall=(1-up)*(1-up);
                float flicker=0.85+0.15*Noise(float2(input.positionWS.x*0.3+_Time.y*0.7,_Time.y*0.5));
                return half4(_Light.rgb*0.16*fall*ends*flicker,1);
            }
            ENDHLSL
        }
    }
}
