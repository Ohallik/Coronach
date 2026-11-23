using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;
using UnityEngine.Rendering;
namespace Lattice.Combat
{
    /// <summary>
    /// The fixed camera's first rule: a hero out in the field is never lost behind anything. Each body is drawn a second time with <c>Lattice/HeroThrough</c>, which shows
    /// only where something stands in front of it by more than the body is thick: a bright rim round a tinted fill, the lead brighter than the partner. It needs nothing
    /// of what is in the way, so it holds for a boss, its arms and its wreck, a wall, a lying arm or a crate alike. In the first whole-game capture (nov20) neither hero
    /// could be seen behind the Foundry Saint's lying arms (s59 022, 024, 041), which belong to the room and opened no door for anyone, and Taren inside the Slag
    /// Colossus's wreck was a patch of dots that read as a fault (s49 067).
    /// The copies are kept on an object of their own, outside the hero, so nothing that walks a hero's renderers (a hit's flash, the framing's bounds, a defeat's pose)
    /// ever meets them. A skinned copy shares the body's bones, so it moves as the body does at no cost but its own skinning.
    /// </summary>
    [DefaultExecutionOrder(960)]
    public sealed class HeroThrough:MonoBehaviour
    {
        /// <summary>How far in front of a point of the body something must stand before it counts as hiding it: more than an arm across a chest, or a foot in the floor.
        /// A ship's own wing over its hull is a hull's width.</summary>
        public const float Thickness=.55f,HullThickness=2.4f;
        /// <summary>The lead warm gold, so he reads apart from white plates and the partner; the partner pale blue. The first render's warm white was lost on the Saint's white panels.</summary>
        static readonly Color LeadFill=new(1,.8f,.42f,1),PartnerFill=new(.6f,.88f,1,1);
        static Material[] looks;
        CombatActor actor;GameObject holder;
        readonly List<(Renderer from,Renderer drawn)> pairs=new();
        int measured=-1;float nextMeasure;
        /// <summary>Whether a copy stands for this renderer and draws this frame: what the shared camera tests read.</summary>
        public int Drawn{get{int n=0;foreach(var (from,drawn) in pairs)if(drawn!=null&&drawn.enabled)n++;return n;}}
        public IEnumerable<Renderer> Copies{get{foreach(var pair in pairs)if(pair.drawn!=null)yield return pair.drawn;}}
        /// <summary>The hero's child the copies hang under.</summary>
        public Transform Holder=>holder!=null?holder.transform:null;
        void Awake()=>actor=GetComponent<CombatActor>();
        void OnEnable(){if(holder!=null)holder.SetActive(true);}
        void OnDisable(){if(holder!=null)holder.SetActive(false);}
        void OnDestroy(){if(holder!=null)Destroy(holder);}
        static Material Look(bool lead,bool flight)
        {
            if(looks==null||looks[0]==null)
            {
                var shader=Resources.Load<Shader>("Effects/HeroThrough");if(shader==null)return null;
                looks=new Material[4];
                for(int i=0;i<4;i++)
                {
                    bool isLead=(i&1)==0,isFlight=(i&2)!=0;
                    var material=new Material(shader){name=(isLead?"Hero through (lead":"Hero through (partner")+(isFlight?", flight)":")")};
                    material.SetColor("_BaseColor",isLead?LeadFill:PartnerFill);material.SetColor("_RimColor",isLead?Color.white:new Color(.8f,.95f,1,1));
                    // An outline first and a light fill: a fill that covered him flat lost his pose (E2-sorrel-03, the old ZTest-greater ghost), a rim keeps it.
                    material.SetFloat("_Fill",isLead?.4f:.3f);material.SetFloat("_Intensity",isLead?1.6f:1.15f);material.SetFloat("_Sharpness",1.5f);material.SetFloat("_Thickness",isFlight?HullThickness:Thickness);
                    looks[i]=material;
                }
            }
            return looks[(lead?0:1)+(flight?2:0)];
        }
        /// <summary>What of the body is drawn solid: its meshes, not a trail, a marking or a glow.</summary>
        static bool Solid(Renderer body)
        {
            if(body is not SkinnedMeshRenderer&&body is not MeshRenderer)return false;
            if(body.name=="Telegraph"||body.shadowCastingMode==ShadowCastingMode.ShadowsOnly)return false;
            if(body is MeshRenderer&&(!body.TryGetComponent<MeshFilter>(out var filter)||filter.sharedMesh==null))return false;
            if(body is SkinnedMeshRenderer skinned&&skinned.sharedMesh==null)return false;
            foreach(var material in body.sharedMaterials)if(material!=null&&material.renderQueue<2500)return true;
            return false;
        }
        void Measure()
        {
            var bodies=actor!=null&&actor.TryGetComponent<FormController>(out var forms)?Gather(forms):System.Array.FindAll(GetComponentsInChildren<Renderer>(true),r=>holder==null||!r.transform.IsChildOf(holder.transform));
            if(bodies.Length==measured&&holder!=null)return;
            measured=bodies.Length;
            if(holder!=null)Destroy(holder);pairs.Clear();
            // Under the hero, not at the scene's root: what the world's own checks walk (the props that float or sink, a zone's drawn things) passes over anything a hero carries,
            // and the copies at the root were counted as floating props in every zone (QaPlanWorldTests.NothingFloatsOrSinksIn, November 21). Beside his forms, never inside one, so
            // whatever walks a form's renderers (the flight framing, a form's fold) never meets them.
            holder=new GameObject("Hero through");holder.transform.SetParent(transform,false);
            foreach(var body in bodies)
            {
                if(body==null||!Solid(body))continue;
                var copy=new GameObject(body.name+" (through)");copy.transform.SetParent(holder.transform,false);
                Renderer drawn;int parts;
                if(body is SkinnedMeshRenderer skinned)
                {
                    var made=copy.AddComponent<SkinnedMeshRenderer>();made.sharedMesh=skinned.sharedMesh;made.bones=skinned.bones;made.rootBone=skinned.rootBone!=null?skinned.rootBone:skinned.transform;
                    made.localBounds=skinned.localBounds;made.updateWhenOffscreen=skinned.updateWhenOffscreen;made.quality=skinned.quality;made.skinnedMotionVectors=false;
                    drawn=made;parts=skinned.sharedMesh.subMeshCount;
                }
                else
                {
                    var mesh=body.GetComponent<MeshFilter>().sharedMesh;copy.AddComponent<MeshFilter>().sharedMesh=mesh;drawn=copy.AddComponent<MeshRenderer>();parts=mesh.subMeshCount;
                }
                drawn.shadowCastingMode=ShadowCastingMode.Off;drawn.receiveShadows=false;drawn.lightProbeUsage=LightProbeUsage.Off;drawn.reflectionProbeUsage=ReflectionProbeUsage.Off;
                drawn.motionVectorGenerationMode=MotionVectorGenerationMode.ForceNoMotion;drawn.allowOcclusionWhenDynamic=false;
                drawn.sharedMaterials=new Material[Mathf.Max(1,parts)];drawn.enabled=false;
                pairs.Add((body,drawn));
            }
        }
        static Renderer[] Gather(FormController forms)
        {
            var all=new List<Renderer>();
            foreach(var visual in new[]{forms.natural,forms.shaped,forms.flight})if(visual!=null)all.AddRange(visual.GetComponentsInChildren<Renderer>(true));
            return all.ToArray();
        }
        void LateUpdate()
        {
            if(actor==null)return;
            if(holder==null||Time.unscaledTime>=nextMeasure){nextMeasure=Time.unscaledTime+1;Measure();}
            var party=PartyController.Current;bool lead=party!=null&&party.members!=null&&party.members.Length>0&&party.Active==actor;
            var look=Look(lead,actor.flight);
            foreach(var (from,drawn) in pairs)
            {
                if(drawn==null)continue;
                bool on=look!=null&&from!=null&&from.enabled&&from.gameObject.activeInHierarchy&&from.forceRenderingOff==false;
                if(drawn.enabled!=on)drawn.enabled=on;
                if(!on)continue;
                if(drawn.sharedMaterial!=look){var materials=drawn.sharedMaterials;for(int i=0;i<materials.Length;i++)materials[i]=look;drawn.sharedMaterials=materials;}
                if(drawn is MeshRenderer)
                {
                    var at=from.transform;var copy=drawn.transform;copy.SetPositionAndRotation(at.position,at.rotation);
                    var under=holder.transform.lossyScale;var scale=at.lossyScale;copy.localScale=new Vector3(scale.x/Mathf.Max(1e-4f,under.x),scale.y/Mathf.Max(1e-4f,under.y),scale.z/Mathf.Max(1e-4f,under.z));
                }
            }
        }
    }
}
