using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;
using UnityEngine.Rendering;
namespace Lattice.Combat
{
    /// <summary>
    /// The fixed camera's first rule: a hero out in the field is never lost behind anything. Each body is drawn twice more with <c>Lattice/HeroThrough</c>, shown only where
    /// something stands in front of it by more than the body is thick: one clean line round the hidden part, of even width on the screen, gold for the lead and pale blue
    /// for the partner, with a faint flat tint inside so the surface in front stays readable through the figure. It needs nothing of what is in the way, so it holds for a
    /// boss, its arms and its wreck, a wall, a roof, a lying arm or a crate alike. In the first whole-game capture (nov20) neither hero could be seen behind the Foundry
    /// Saint's lying arms (s59 022, 024, 041), and Taren inside the Slag Colossus's wreck was a patch of dots that read as a fault (s49 067). D414: the drawing was a Fresnel
    /// rim over a fill, and seen from above almost all of a body is at a grazing angle, so it was a bright, nearly solid glass figure that read as standing ON what hid it
    /// (nov24-whole s13/007 on the Arch Market's arch, s11/007 sitting on Tallow's lift door, s50/071 on Harrowgate's gate roof).
    /// The two copies of a body: the body itself (it marks the body's pixels, tints where it is hidden and draws the line where the scenery's edge crosses it) and a hull
    /// pushed out by the line's width (it draws the silhouette's line, off every body's pixels). The body's material draws before any hull's (the queue), so a line never
    /// crosses a hero, his own parts or the other hero's.
    /// The copies are kept on an object of their own, outside the hero, so nothing that walks a hero's renderers (a hit's flash, the framing's bounds, a defeat's pose)
    /// ever meets them. A skinned copy shares the body's bones, so it moves as the body does at no cost but its own skinning.
    /// </summary>
    [DefaultExecutionOrder(960)]
    public sealed class HeroThrough:MonoBehaviour
    {
        /// <summary>How far in front of a point of the body something must stand before it counts as hiding it: more than an arm across a chest, or a foot in the floor.
        /// A ship's own wing over its hull is a hull's width.</summary>
        public const float Thickness=.55f,HullThickness=2.4f;
        /// <summary>The line's width in pixels of 1080 (the party is about 90 px tall), and the flat tint inside it: faint enough that the scenery's own surface reads through.</summary>
        public const float LineWidth=3,Tint=.09f;
        /// <summary>The lead warm gold, so he reads apart from white plates and the partner; the partner pale blue. No more than 1, so nothing blooms into a glow.</summary>
        static readonly Color LeadLine=new(1,.8f,.36f,1),PartnerLine=new(.62f,.9f,1,1);
        /// <summary>D421: of the hero's body, the share hidden below which nothing is drawn and from which all of the drawing is. A hero in plain view but for his feet behind a
        /// rail or a collar needs no finding, and the line round that sliver read as a loose scrap under him (nov25-r5-chain s55/139, s59/053). The shader reads the share
        /// from the scene's depth at points over the body (the capsule's height by twice <see cref="HalfWidth"/>). A hero wholly behind one broad piece of scenery is shown
        /// in a window the piece opens round them instead (SceneryOcclusion).</summary>
        public const float ShareLow=.3f,ShareHigh=.5f,HalfWidth=.3f;
        static readonly int HeroId=Shader.PropertyToID("_Hero");
        static MaterialPropertyBlock block;
        static Material[] looks;
        CombatActor actor;GameObject holder;
        readonly List<(Renderer from,Renderer body,Renderer hull)> pairs=new();
        int measured=-1;float nextMeasure;
        /// <summary>How many of the hero's bodies are drawn through this frame: what the shared camera tests read.</summary>
        public int Drawn{get{int n=0;foreach(var (from,body,hull) in pairs)if(body!=null&&body.enabled)n++;return n;}}
        /// <summary>Every copy, the bodies' and the hulls'.</summary>
        public IEnumerable<Renderer> Copies{get{foreach(var (from,body,hull) in pairs){if(body!=null)yield return body;if(hull!=null)yield return hull;}}}
        /// <summary>The hero's child the copies hang under.</summary>
        public Transform Holder=>holder!=null?holder.transform:null;
        void Awake()=>actor=GetComponent<CombatActor>();
        void OnEnable(){if(holder!=null)holder.SetActive(true);}
        void OnDisable(){if(holder!=null)holder.SetActive(false);}
        void OnDestroy(){if(holder!=null)Destroy(holder);}
        /// <summary>The drawing's materials, made once at run time from the shader in Resources. They differ by properties alone (colour, push, cull, queue): no keyword,
        /// so no variant a player build could strip.</summary>
        static Material Look(bool lead,bool flight,bool hull)
        {
            if(looks==null||looks[0]==null)
            {
                var shader=Resources.Load<Shader>("Effects/HeroThrough");if(shader==null)return null;
                looks=new Material[8];
                for(int i=0;i<8;i++)
                {
                    bool isLead=(i&1)==0,isFlight=(i&2)!=0,isHull=(i&4)!=0;
                    var material=new Material(shader){name=$"Hero through ({(isLead?"lead":"partner")}{(isFlight?", flight":"")}, {(isHull?"line":"body")})"};
                    material.SetColor("_BaseColor",isLead?LeadLine:PartnerLine);material.SetFloat("_Fill",Tint);material.SetFloat("_Line",.95f);material.SetFloat("_Width",LineWidth);
                    material.SetFloat("_Thickness",isFlight?HullThickness:Thickness);material.SetFloat("_Push",isHull?1:0);
                    // D421: the body culls nothing either. Culled, a hair card turned away left its pixels unmarked, and the hull drew its line there, short strokes inside the
                    // figure (Sela's hair under Harrowgate's gate roof); a hero in plain view, whose own parts might read as hiding one another, draws nothing at all (the share).
                    material.SetFloat("_Cull",(float)CullMode.Off);
                    material.SetFloat("_HeroWidth",HalfWidth);material.SetFloat("_ShareLow",ShareLow);material.SetFloat("_ShareHigh",ShareHigh);                    material.renderQueue=(int)RenderQueue.Transparent+(isHull?41:40);
                    looks[i]=material;
                }
            }
            return looks[(lead?0:1)+(flight?2:0)+(hull?4:0)];
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
            foreach(var body in bodies)if(body!=null&&Solid(body))pairs.Add((body,Copy(body," (through)"),Copy(body," (through line)")));
        }
        Renderer Copy(Renderer body,string suffix)
        {
            var copy=new GameObject(body.name+suffix);copy.transform.SetParent(holder.transform,false);
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
            return drawn;
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
            Material body=Look(lead,actor.flight,false),line=Look(lead,actor.flight,true);
            block??=new MaterialPropertyBlock();block.SetVector(HeroId,Body());
            foreach(var (from,fill,hull) in pairs)
            {
                bool on=body!=null&&line!=null&&from!=null&&from.enabled&&from.gameObject.activeInHierarchy&&from.forceRenderingOff==false;
                Show(from,fill,on,body);Show(from,hull,on,line);
            }
        }
        /// <summary>What the shader reads the share hidden over: the hero's feet and his capsule's height. None for a ship, which is drawn wherever it is hidden, as before.</summary>
        Vector4 Body()
        {
            if(actor.flight)return Vector4.zero;
            var feet=transform.position;float tall=1.8f;
            if(TryGetComponent<CharacterController>(out var capsule))
            {
                float scale=Mathf.Abs(transform.lossyScale.y);tall=Mathf.Clamp(capsule.height*scale,1.4f,2.4f);
                feet=transform.TransformPoint(capsule.center)-Vector3.up*(capsule.height*scale*.5f);
            }
            return new Vector4(feet.x,feet.y,feet.z,tall);
        }
        void Show(Renderer from,Renderer drawn,bool on,Material look)
        {
            if(drawn==null)return;
            if(drawn.enabled!=on)drawn.enabled=on;
            if(!on)return;
            if(drawn.sharedMaterial!=look){var materials=drawn.sharedMaterials;for(int i=0;i<materials.Length;i++)materials[i]=look;drawn.sharedMaterials=materials;}
            drawn.SetPropertyBlock(block);
            if(drawn is MeshRenderer)
            {
                var at=from.transform;var copy=drawn.transform;copy.SetPositionAndRotation(at.position,at.rotation);
                var under=holder.transform.lossyScale;var scale=at.lossyScale;copy.localScale=new Vector3(scale.x/Mathf.Max(1e-4f,under.x),scale.y/Mathf.Max(1e-4f,under.y),scale.z/Mathf.Max(1e-4f,under.z));
            }
        }
    }
}
