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
        /// <summary>D432: the share hidden from which a hero is drawn through scenery, and from which all of the drawing is: nearly the whole of them, and never while the middle of
        /// the head is in sight (the shader's _Head). Ordinary hiding is the depth cue: a person half behind a wall with nothing drawn reads as behind it at once, and the line round
        /// the hidden part, lying on the wall's lit top, read as a ghost standing ON it (nov25-whole s30/196 Sela on the Many Hands wall, s22/061 on the Rootmarket kiln with her
        /// head over it, s25/012 Taren on a Driftreed roof). So the lead is drawn through scenery only where the player would not otherwise know where he is (hidden whole by one
        /// broad piece he stands in its window instead: SceneryOcclusion), and the partner, whom nobody steers, never: behind a wall she is simply hidden, as anyone is. A boss is
        /// different: behind a boss, its arms or its wreck (<see cref="BossOcclusion.Between"/>), or anywhere in a live boss fight, the lead keeps <see cref="ShareLow"/>, since the
        /// player must read his pose while he fights from behind it; the partner behind a boss is drawn from this share. A ship is drawn through as before.
        /// D438: behind scenery nothing of the body is drawn now, and no window opens: from this share, his head hidden too, the lead is marked (<see cref="Mark"/>).</summary>
        public const float WholeLow=.9f,WholeHigh=.95f;
        /// <summary>The mark's own share (the lead's ruling, November 26): at the Separation Works' gallery wall a gap between two blocks shows a twentieth of him, the measure reads
        /// .8 to .9, and by <see cref="WholeLow"/> he stood unmarked with nothing of him to find (nov26-r7-chain s58/151). A lead four fifths hidden with his head hidden is lost to
        /// the player; where his head shows (s16/114, s25/012) the head rule keeps the mark off whatever the share.</summary>
        public const float MarkLow=.8f,MarkHigh=.85f;
        /// <summary>For a test's before: every hero on foot drawn through whatever hides them from <see cref="ShareLow"/>, and the partner given windows of her own
        /// (SceneryOcclusion), as until D432.</summary>
        public static bool AsItWas;
        /// <summary>D438: for a test's before, the rule of D432 (the morning of November 26): behind scenery the lead drawn through from <see cref="WholeLow"/> and stood in the
        /// window of a piece that hides him whole (SceneryOcclusion), and no mark.</summary>
        public static bool AsThisMorning;
        /// <summary>D438: the places the mark reads (the shader's _Trail), how far apart in time, and how far a hero may move in one frame before what he stood on before is
        /// forgotten (set down from afar). A downed partner lies low: her share is read over this height.</summary>
        const int TrailLength=8;const float TrailStep=.045f,Jump=3,DownHeight=.7f;
        static readonly int HeroId=Shader.PropertyToID("_Hero"),LowId=Shader.PropertyToID("_ShareLow"),HighId=Shader.PropertyToID("_ShareHigh"),HeadId=Shader.PropertyToID("_Head"),
            TrailId=Shader.PropertyToID("_Trail");
        static MaterialPropertyBlock block;
        static Material[] looks;
        static Mesh markMesh;
        CombatActor actor;GameObject holder;Renderer mark;
        readonly List<(Renderer from,Renderer body,Renderer hull)> pairs=new();
        readonly List<Vector4> path=new();readonly Vector4[] trail=new Vector4[TrailLength];
        int measured=-1;float nextMeasure;
        /// <summary>How many of the hero's bodies are drawn through this frame: what the shared camera tests read.</summary>
        public int Drawn{get{int n=0;foreach(var (from,body,hull) in pairs)if(body!=null&&body.enabled)n++;return n;}}
        /// <summary>D438: the mark over this hero (a small quad under <see cref="Holder"/>, not one of the <see cref="Copies"/>), or none yet.</summary>
        public Renderer Mark=>mark;
        /// <summary>D438: the mark is out for this hero: the shader shows it only while they are hidden almost whole, and has been a fifth of a second.</summary>
        public bool Marking=>isActiveAndEnabled&&mark!=null&&mark.enabled;
        /// <summary>D438: by today's rule this hero on foot is found by HeroThrough alone (the line by a boss, the mark elsewhere), so no scenery opens a window for them.</summary>
        public static bool Finds(CombatActor hero)=>!AsItWas&&!AsThisMorning&&hero!=null&&!hero.flight&&BossOcclusion.Outlined(hero);
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
        static Material Look(bool lead,bool flight,bool hull)=>Made()?looks[(lead?0:1)+(flight?2:0)+(hull?4:0)]:null;
        /// <summary>D438: the mark's material, the lead's gold or the partner's pale blue: the same shader with _Mark, drawn after every copy and hull.</summary>
        static Material MarkLook(bool lead)=>Made()?looks[lead?8:9]:null;
        static bool Made()
        {
            if(looks==null||looks[0]==null)
            {
                var shader=Resources.Load<Shader>("Effects/HeroThrough");if(shader==null)return false;
                looks=new Material[10];
                for(int i=8;i<10;i++)
                {
                    var material=new Material(shader){name=$"Hero mark ({(i==8?"lead":"partner")})"};
                    material.SetColor("_BaseColor",i==8?LeadLine:PartnerLine);material.SetFloat("_Line",.95f);material.SetFloat("_Mark",1);material.SetFloat("_Push",0);
                    material.SetFloat("_Thickness",Thickness);material.SetFloat("_Cull",(float)CullMode.Off);material.SetFloat("_HeroWidth",HalfWidth);
                    material.SetFloat("_ShareLow",MarkLow);material.SetFloat("_ShareHigh",MarkHigh);material.SetFloat("_Head",1);
                    material.renderQueue=(int)RenderQueue.Transparent+42;
                    looks[i]=material;
                }
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
            return true;
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
            // D432: on foot, the lead is drawn from ShareLow only behind a boss or in its fight, and otherwise only when nearly whole and his head are hidden; the partner only
            // behind a boss, and then only so. Per hero through the property block: the materials stay as they are, with no keyword.
            // D438: and behind scenery not at all. Anything of his body shown there, a line, a tint or the window SceneryOcclusion opened round him, lay on the lit top of the
            // piece that hid him (from 48 degrees up nothing of the floor he stands on shows beside him) and read as him walking on the roof (nov26-r7-chain s51/218, s50/058,
            // s58/151). Hidden almost whole, the lead is marked instead (the mark, drawn by the same share and head rule); the partner only while she is down to be raised.
            // In a boss's fight or behind a boss, its arms or its wreck, the line as D432 left it and no mark.
            bool ground=!actor.flight&&!AsItWas,boss=ground&&(lead&&BossFight()||BossOcclusion.Between(Camera.main,actor));
            bool shown=!ground||boss||lead&&AsThisMorning,near=!ground||lead&&boss;
            bool marked=ground&&!boss&&!AsThisMorning&&(lead||Down());
            var at=Body();Remember(at);
            block??=new MaterialPropertyBlock();block.SetVector(HeroId,at);
            block.SetFloat(LowId,near?ShareLow:WholeLow);block.SetFloat(HighId,near?ShareHigh:WholeHigh);block.SetFloat(HeadId,near?0:1);
            if(marked)block.SetVectorArray(TrailId,Trail());
            foreach(var (from,fill,hull) in pairs)
            {
                bool on=shown&&body!=null&&line!=null&&from!=null&&from.enabled&&from.gameObject.activeInHierarchy&&from.forceRenderingOff==false;
                Show(from,fill,on,body);Show(from,hull,on,line);
            }
            ShowMark(marked,lead);
        }
        /// <summary>D438: the partner down and waiting to be raised (ReplayHands.Fallen's test): the one time she is marked, so the player can find her to raise her
        /// (nov26-r7-chain s58/173, 178, 181: "Idra Sorn - Down." and Idra nowhere).</summary>
        bool Down()=>actor.Health!=null&&!actor.Health.Alive&&actor.gameObject.activeInHierarchy;
        /// <summary>D438: where the hero's feet were, kept a little longer than the mark reads. Set down from afar (a zone's door, a test's fixture), what came before is
        /// forgotten, so the mark waits its fifth of a second at the new place.</summary>
        void Remember(Vector4 body)
        {
            float now=Time.unscaledTime;var feet=(Vector3)body;
            if(path.Count>0&&((Vector3)path[path.Count-1]-feet).sqrMagnitude>Jump*Jump)path.Clear();
            path.Add(new Vector4(feet.x,feet.y,feet.z,now));
            int stale=0;while(stale<path.Count-1&&path[stale+1].w<=now-TrailStep*TrailLength)stale++;
            if(stale>0)path.RemoveRange(0,stale);
        }
        /// <summary>D438: where the feet stood at each of the mark's moments, newest first, between the frames that bracket it; w 0 before anything known.</summary>
        Vector4[] Trail()
        {
            float now=Time.unscaledTime;
            for(int k=0;k<TrailLength;k++)
            {
                float when=now-k*TrailStep;trail[k]=Vector4.zero;
                for(int i=path.Count-1;i>=0;i--)
                {
                    if(path[i].w>when)continue;
                    var p=i==path.Count-1?(Vector3)path[i]:Vector3.Lerp(path[i],path[i+1],Mathf.InverseLerp(path[i].w,path[i+1].w,when));
                    trail[k]=new Vector4(p.x,p.y,p.z,1);break;
                }
            }
            return trail;
        }
        /// <summary>D438: the mark, a quad whose corners are the glyph's pixels of 1080 (the shader stands it over the head and sizes it on the screen). Under the holder
        /// beside the copies, so nothing that walks the hero's renderers for his own takes it.</summary>
        void ShowMark(bool on,bool lead)
        {
            var look=on?MarkLook(lead):null;on&=look!=null&&holder!=null;
            if(on&&mark==null)
            {
                var made=new GameObject("Hero mark");made.transform.SetParent(holder.transform,false);
                made.AddComponent<MeshFilter>().sharedMesh=MarkMesh();var drawn=made.AddComponent<MeshRenderer>();
                drawn.shadowCastingMode=ShadowCastingMode.Off;drawn.receiveShadows=false;drawn.lightProbeUsage=LightProbeUsage.Off;drawn.reflectionProbeUsage=ReflectionProbeUsage.Off;
                drawn.motionVectorGenerationMode=MotionVectorGenerationMode.ForceNoMotion;drawn.allowOcclusionWhenDynamic=false;drawn.enabled=false;
                mark=drawn;
            }
            if(mark==null)return;
            if(mark.enabled!=on)mark.enabled=on;
            if(!on)return;
            if(mark.sharedMaterial!=look)mark.sharedMaterial=look;
            // The mark's own share and head rule, over the body copies' (the block is theirs first).
            block.SetFloat(LowId,MarkLow);block.SetFloat(HighId,MarkHigh);block.SetFloat(HeadId,1);
            mark.SetPropertyBlock(block);
        }
        static Mesh MarkMesh()
        {
            if(markMesh!=null)return markMesh;
            markMesh=new Mesh{name="Hero mark"};
            // The glyph's pixels of 1080 about its point, with room for its edge (HeroThrough.shader: MarkHalf 21, MarkTall 27, MarkEdge 3).
            markMesh.vertices=new[]{new Vector3(-25,-5,0),new Vector3(25,-5,0),new Vector3(-25,31,0),new Vector3(25,31,0)};
            markMesh.normals=new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.back};
            markMesh.triangles=new[]{0,2,1,1,2,3};
            // What the lens culls by: the hero's own height and the mark over it, not the glyph's pixels.
            markMesh.bounds=new Bounds(Vector3.up*1.4f,new Vector3(1.2f,2.8f,1.2f));
            return markMesh;
        }
        /// <summary>A boss awake and in its fight (D432: the lead keeps today's drawing through all of it).</summary>
        static bool BossFight(){foreach(var boss in BossController.All)if(boss!=null&&boss.Fighting)return true;return false;}
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
            // D438: a downed hero lies on the floor; her share (and the mark over her) is read that low.
            if(Down())tall=DownHeight;
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
