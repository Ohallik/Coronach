using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>A killed boss goes dark at once (D355): its lights go out and its colour drains, to ash if it is destroyed, cold if it is a machine shut down. The Rotbloom stood whole in
    /// its own colours for three seconds after its integrity was gone and the Bellows kept its seams lit (nov22-whole s26/454 to 469, s08/092 to 098): a body seen from above that only
    /// sinks in height looks the same as one alive. Added by DefeatPresentation as the body is beaten; on its own component, so it runs through a boss whose own ending stops that
    /// presentation (the Foundry Saint and the Tollkeeper settle where they stand). Runs after the flash and the see-through door, and writes only its own values into the block they share.</summary>
    [DefaultExecutionOrder(960)]
    public sealed class BodyGoesDark:MonoBehaviour
    {
        /// <summary>How long it takes, its lights first: out by the time its paint is half drained.</summary>
        public const float Seconds=.7f;
        static readonly int BaseColor=Shader.PropertyToID("_BaseColor"),OldColor=Shader.PropertyToID("_Color"),Emission=Shader.PropertyToID("_EmissionColor"),Grain=Shader.PropertyToID("_BaseMapStrength");
        Renderer[] painted;Color[] paintRest;Vector4[] lightRest;float[] grainRest;bool[] lit;int[] paintId;
        MaterialPropertyBlock block;float began,share;bool ashes,done,restoring;
        /// <summary>How dark it is now, 0 its colours to 1 dark, for a check to read.</summary>
        public float Share=>share;
        /// <summary>How many of its renderers it darkens, for a check to read.</summary>
        public int Painted=>painted!=null?painted.Length:0;
        /// <param name="body">The whole body: everything drawn under its root, not only its presentation's model (a body staged with art of its own hangs that beside it).</param>
        public void Begin(Transform body,bool destroyed)
        {
            ashes=destroyed;share=0;done=false;began=GameTime.Now;block??=new MaterialPropertyBlock();
            var list=new List<Renderer>();var ids=new List<int>();
            foreach(var r in body.GetComponentsInChildren<Renderer>(true))
            {
                if(!(r is MeshRenderer||r is SkinnedMeshRenderer)||r.sharedMaterial==null||r.name=="Telegraph"||r.name=="Knot")continue;
                var m=r.sharedMaterial;int id=m.HasProperty(BaseColor)?BaseColor:m.HasProperty(OldColor)?OldColor:0;if(id==0)continue;
                list.Add(r);ids.Add(id);
            }
            painted=list.ToArray();paintId=ids.ToArray();paintRest=new Color[painted.Length];lightRest=new Vector4[painted.Length];grainRest=new float[painted.Length];lit=new bool[painted.Length];
            for(int i=0;i<painted.Length;i++)
            {
                var m=painted[i].sharedMaterial;paintRest[i]=m.GetColor(paintId[i]);grainRest[i]=m.HasProperty(Grain)?m.GetFloat(Grain):1;
                lit[i]=m.HasProperty(Emission);lightRest[i]=lit[i]?m.GetVector(Emission):Vector4.zero;
            }
        }
        /// <summary>Its colours given back, as it stood (a body revived), and this gone.</summary>
        public void Restore(){restoring=true;Paint(0);Destroy(this);}
        void LateUpdate()
        {
            if(painted==null||done)return;
            // On the game's clock, fastest at first, and no frame moves it by more than a small step however long it ran (a hitch is not a pop).
            float left=1-Mathf.Clamp01((GameTime.Now-began)/Seconds);
            share=Mathf.MoveTowards(share,1-left*left,Impact.Tune.MostStepAFrame);Paint(share);
            // Once dark it stays as written: nothing empties a beaten body's block (HitFlash keeps it), and the see-through door writes round it.
            if(share>=1)done=true;
        }
        /// <summary>Ash: grey at the paint's own lightness and under half of it, a little warm, with the texture's grain at under half so it is still the body's surface and not a flat fill
        /// (at six tenths the Rotbloom's petals still read pink under the hall's warm light).
        /// A machine shut down only goes cold: its own colours at half.</summary>
        Color Dark(Color rest)
        {
            if(!ashes)return new Color(rest.r*.5f,rest.g*.5f,rest.b*.5f,rest.a);
            float grey=(.299f*rest.r+.587f*rest.g+.114f*rest.b)*.45f;
            return new Color(grey*1.04f,grey,grey*.94f,rest.a);
        }
        void Paint(float k)
        {
            if(painted==null)return;
            for(int i=0;i<painted.Length;i++)
            {
                var r=painted[i];if(r==null)continue;
                r.GetPropertyBlock(block);
                block.SetColor(paintId[i],Color.Lerp(paintRest[i],Dark(paintRest[i]),k));
                if(ashes)block.SetFloat(Grain,Mathf.Lerp(grainRest[i],Mathf.Min(grainRest[i],.45f),k));
                // HDR emission is a shader-space vector, so it is scaled as one (BurrowerShutdown), and it goes out twice as fast as the paint drains. Never brighter than the body already
                // is: a boss whose own ending put its lights out at once (the Debris Cutter, dead in space) keeps them out.
                if(lit[i])
                {
                    float on=1-Mathf.Clamp01(k*2);var light=lightRest[i]*(on*on);light.w=lightRest[i].w;
                    if(!restoring&&block.HasVector(Emission)){var now=block.GetVector(Emission);light=new Vector4(Mathf.Min(light.x,now.x),Mathf.Min(light.y,now.y),Mathf.Min(light.z,now.z),light.w);}
                    block.SetVector(Emission,light);
                }
                r.SetPropertyBlock(block);
            }
        }
    }
}
