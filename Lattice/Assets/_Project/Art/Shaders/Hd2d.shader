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
                float band=smoothstep(_PixelGrid.z,.5,abs(uv.y-.5));
                float2 step=float2(1,1)/_PixelGrid.xy*band*_PixelGrid.w;
                half3 color=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_PointClamp,uv).rgb*.4;
                color+=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_PointClamp,uv+float2(step.x,0)).rgb*.15;
                color+=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_PointClamp,uv-float2(step.x,0)).rgb*.15;
                color+=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_PointClamp,uv+float2(0,step.y)).rgb*.15;
                color+=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_PointClamp,uv-float2(0,step.y)).rgb*.15;
                return half4(color*_GradeTint.rgb,1);
            }
            ENDHLSL
        }
    }
}
