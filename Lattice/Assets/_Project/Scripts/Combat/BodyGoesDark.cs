using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>A killed boss goes dark at once (D355): its lights go out and its colour drains, to ash if it is destroyed, cold if it is a machine shut down. The Rotbloom stood whole in
    /// its own colours for three seconds after its integrity was gone and the Bellows kept its seams lit (nov22-whole s26/454 to 469, s08/092 to 098): a body seen from above that only
    /// sinks in height looks the same as one alive. Added by DefeatPresentation as the body is beaten; on its own component, so it runs through a boss whose own ending stops that
    /// presentation (the Foundry Saint and the Tollkeeper settle where they stand). Runs after the flash and the see-through door, and writes only its own values into the block they share.</summary>
    /// <para>D363 (nov22b-whole s49/047 to 057): the ash it went to was paler than the body. Every generated body carries its colour in its texture under a white base colour, and the
    /// ash took that white's lightness (.45) and washed the texture's grain to under half, toward white: the Slag Colossus's dark iron came out a flat pale ochre the colour of the
    /// Foundry's floor (mean lightness 53 to 80 of 255 in BossDeathTests' own still, its contrast halved), and on the kill's frame the killing blow's flash burned along every
    /// curved plate of it, a glass shell. Now the paint is darkened over the texture as it is (destroyed: two fifths, shut down: half, driven off: three fifths), the killing blow's
    /// light on a body going dark is a glow, never the rim of a white shell, and it all runs through the kill's stop rather than waiting for it. A hero standing in a dead body sees
    /// out through a clean opening, not through two thirds of its pixels (BossOcclusion's living door).</para>
    [DefaultExecutionOrder(960)]
    public sealed class BodyGoesDark:MonoBehaviour
    {
        /// <summary>How long it takes, its lights first: out by the time its paint is half drained.</summary>
        public const float Seconds=.7f;
        /// <summary>The most the killing blow lights a body going dark, falling as it darkens.</summary>
        const float KillLight=.15f;
        static readonly int BaseColor=Shader.PropertyToID("_BaseColor"),OldColor=Shader.PropertyToID("_Color"),Emission=Shader.PropertyToID("_EmissionColor"),
            Flash=Shader.PropertyToID("_HitFlash"),DoorDepth=Shader.PropertyToID("_OccFadeDepth");
        Renderer[] painted;Color[] paintRest;Vector4[] lightRest;bool[] lit;int[] paintId;
        MaterialPropertyBlock block;float age,share,paintKeep,lightKeep;bool ashes,done,restoring;
        /// <summary>How dark it is now, 0 its colours to 1 dark, for a check to read.</summary>
        public float Share=>share;
        /// <summary>How many of its renderers it darkens, for a check to read.</summary>
        public int Painted=>painted!=null?painted.Length:0;
        /// <param name="body">The whole body: everything drawn under its root, not only its presentation's model (a body staged with art of its own hangs that beside it).</param>
        public void Begin(Transform body,bool destroyed){ashes=destroyed;paintKeep=destroyed?.4f:.5f;lightKeep=0;Gather(body);}
        /// <summary>A boss driven off (BossDeparture): it goes as one beaten, its lights down to a fifth and its paint to three fifths, in the second it turns away.</summary>
        public void BeginBeaten(Transform body){ashes=false;paintKeep=.6f;lightKeep=.2f;Gather(body);}
        void Gather(Transform body)
        {
            share=0;age=0;done=false;block??=new MaterialPropertyBlock();
            var list=new List<Renderer>();var ids=new List<int>();
            foreach(var r in body.GetComponentsInChildren<Renderer>(true))
            {
                if(!(r is MeshRenderer||r is SkinnedMeshRenderer)||r.sharedMaterial==null||r.name=="Telegraph"||r.name=="Knot")continue;
                var m=r.sharedMaterial;int id=m.HasProperty(BaseColor)?BaseColor:m.HasProperty(OldColor)?OldColor:0;if(id==0)continue;
                list.Add(r);ids.Add(id);
            }
            painted=list.ToArray();paintId=ids.ToArray();paintRest=new Color[painted.Length];lightRest=new Vector4[painted.Length];lit=new bool[painted.Length];
            for(int i=0;i<painted.Length;i++)
            {
                var m=painted[i].sharedMaterial;paintRest[i]=m.GetColor(paintId[i]);
                lit[i]=m.HasProperty(Emission);lightRest[i]=lit[i]?m.GetVector(Emission):Vector4.zero;
            }
        }
        /// <summary>Its colours given back, as it stood (a body revived), and this gone.</summary>
        public void Restore(){restoring=true;Paint(0);Destroy(this);}
        void LateUpdate()
        {
            if(painted==null)return;
            if(!done)
            {
                // Through the kill's stop, which holds the game's clock and not a pause: waiting for it, the body stood in its colours under the killing blow's light for the
                // whole stop (s49/047). Fastest at first, and no frame moves it by more than a small step however long it ran (a hitch is not a pop).
                if(!GameTime.Paused)age+=Time.unscaledDeltaTime;
                float left=1-Mathf.Clamp01(age/Seconds);
                share=Mathf.MoveTowards(share,1-left*left,Impact.Tune.MostStepAFrame);
                if(share>=1)done=true;
            }
            // Written every frame, dark or not yet: the see-through door writes its own share over a hero into the same block each frame before this (BossOcclusion).
            Paint(share);
        }
        /// <summary>Its own paint darkened as it is, texture and all, so it is still its surface: destroyed, to two fifths (a little cooler in red, so the Rotbloom's petals go
        /// to a dead brown and not a darker pink); shut down, to half; driven off, to three fifths. In the colour space the block is given, so these are shares of how light it looks.</summary>
        Color Dark(Color rest)
        {
            if(ashes)return new Color(rest.r*.37f,rest.g*.4f,rest.b*.38f,rest.a);
            return new Color(rest.r*paintKeep,rest.g*paintKeep,rest.b*paintKeep,rest.a);
        }
        void Paint(float k)
        {
            if(painted==null)return;
            for(int i=0;i<painted.Length;i++)
            {
                var r=painted[i];if(r==null)continue;
                r.GetPropertyBlock(block);
                block.SetColor(paintId[i],Color.Lerp(paintRest[i],Dark(paintRest[i]),k));
                // HDR emission is a shader-space vector, so it is scaled as one (BurrowerShutdown), and it goes down twice as fast as the paint drains. Never brighter than the body
                // already is: a boss whose own ending put its lights out at once (the Debris Cutter, dead in space) keeps them out.
                if(lit[i])
                {
                    float on=1-Mathf.Clamp01(k*2);var light=lightRest[i]*(lightKeep+(1-lightKeep)*on*on);light.w=lightRest[i].w;
                    if(!restoring&&block.HasVector(Emission)){var now=block.GetVector(Emission);light=new Vector4(Mathf.Min(light.x,now.x),Mathf.Min(light.y,now.y),Mathf.Min(light.z,now.z),light.w);}
                    block.SetVector(Emission,light);
                }
                if(!restoring)
                {
                    // The killing blow's light (HitFlash, written just before this) burns along a body's silhouette: on a boss of curved plates at full strength that was a white
                    // glass shell. On a body going dark it is a short glow that goes as the dark comes.
                    if(block.HasVector(Flash)){var flash=block.GetVector(Flash);float most=KillLight*(1-k);if(flash.w>most){flash.w=most;block.SetVector(Flash,flash);}}
                    // Over a hero standing in it the door opens whole: through the living door's two pixels in three he was a speckled figure inside a dead body (s49/049).
                    block.SetFloat(DoorDepth,1);
                }
                r.SetPropertyBlock(block);
            }
        }
    }
}
