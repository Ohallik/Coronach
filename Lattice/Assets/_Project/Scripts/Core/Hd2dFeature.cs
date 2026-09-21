using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
namespace Lattice.Core
{
    public sealed class Hd2dFeature:ScriptableRendererFeature
    {
        public Material material;
        Pass pass;
        public override void Create(){pass=new Pass();pass.renderPassEvent=RenderPassEvent.AfterRenderingPostProcessing;}
        public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data)
        {
            if(material==null||data.cameraData.cameraType!=CameraType.Game)return;
            pass.material=material;renderer.EnqueuePass(pass);
        }
        sealed class Pass:ScriptableRenderPass
        {
            public Pass(){requiresIntermediateTexture=true;}
            public Material material;
            public override void RecordRenderGraph(RenderGraph graph,ContextContainer frameData)
            {
                var resources=frameData.Get<UniversalResourceData>();
                if(resources.isActiveTargetBackBuffer)return;
                var camera=frameData.Get<UniversalCameraData>();
                var profile=ZoneController.Current!=null?ZoneController.Current.definition.hd2dProfile:null;
                if(profile==null)return;
                material.SetVector("_PixelGrid",new Vector4(profile.internalWidth,profile.internalHeight,profile.tiltStart,profile.tiltStrength));
                material.SetColor("_GradeTint",profile.tint);
                var descriptor=graph.GetTextureDesc(resources.activeColorTexture);
                descriptor.name="Lattice HD2D";descriptor.clearBuffer=false;
                descriptor.width=profile.internalWidth;descriptor.height=profile.internalHeight;descriptor.filterMode=FilterMode.Point;
                var target=graph.CreateTexture(descriptor);
                graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(resources.activeColorTexture,target,material,0),passName:"Lattice pixel / tilt bands");
                resources.cameraColor=target;
            }
        }
    }
}
