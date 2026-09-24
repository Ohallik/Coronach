Shader "Lattice/HD2D"
{
    Properties { _PixelGrid("Grid and tilt",Vector)=(960,540,.28,1.4) _GradeTint("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float4 _PixelGrid;
            float4 _GradeTint;
            half4 Frag(Varyings input):SV_Target
            {
                float2 uv=(floor(input.texcoord*_PixelGrid.xy)+.5)/_PixelGrid.xy;
                // Gameplay keeps the full frame sharp: no screen-position-dependent blur.
                half3 color=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_PointClamp,uv).rgb;
                return half4(color*_GradeTint.rgb,1);
            }
            ENDHLSL
        }
    }
}
