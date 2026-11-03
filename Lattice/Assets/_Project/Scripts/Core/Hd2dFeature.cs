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
        // The pass writes per-frame grade/grid values. Write them to a private
        // instance so play sessions never dirty the shared Hd2d.mat asset.
        Material source,runtime;
        public override void Create(){pass=new Pass();pass.renderPassEvent=RenderPassEvent.AfterRenderingPostProcessing;}
        public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data)
        {
            if(material==null||data.cameraData.cameraType!=CameraType.Game)return;
            if(runtime==null||source!=material)
            {
                CoreUtils.Destroy(runtime);source=material;
                runtime=new Material(material){name=material.name+" (runtime)",hideFlags=HideFlags.HideAndDontSave};
            }
            pass.material=runtime;renderer.EnqueuePass(pass);
        }
        protected override void Dispose(bool disposing){CoreUtils.Destroy(runtime);runtime=null;source=null;}
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
                material.SetColor("_GradeTint",profile.tint);
                var descriptor=graph.GetTextureDesc(resources.activeColorTexture);
                descriptor.name="Lattice HD2D";descriptor.clearBuffer=false;
                if(!profile.nativeResolution){descriptor.width=profile.internalWidth;descriptor.height=profile.internalHeight;}
                descriptor.filterMode=FilterMode.Point;
                material.SetVector("_PixelGrid",new Vector4(descriptor.width,descriptor.height,0,0));
                var target=graph.CreateTexture(descriptor);
                graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(resources.activeColorTexture,target,material,0),passName:"Coronach sharp colour grade");
                resources.cameraColor=target;
            }
        }
    }
}
