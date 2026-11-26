using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>What a drawing of a hidden body is judged by (D414): of the edge of what is hidden, the share with the drawing's line on it or within a line's reach; and inside it,
    /// how far the drawing changes what is in front, which must stay readable. Shared by the camera's and the fights' checks that read the drawing.</summary>
    internal static class OutlineMeasure
    {
        public static float Change(Color32 a,Color32 b)=>(Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b))/(3*255f);
        static bool Edge(bool[] mask,int w,int h,int i)
        {
            int x=i%w,y=i/w;
            return x==0||y==0||x==w-1||y==h-1||!mask[i-1]||!mask[i+1]||!mask[i-w]||!mask[i+w];
        }
        /// <summary>Of the pixels on the edge of <paramref name="mask"/>, the share with a pixel the drawing marks (<paramref name="marked"/>) within <paramref name="reach"/>
        /// pixels: a closed line round it is all of them, a drawing that is not there none.</summary>
        public static float Covered(bool[] mask,int w,int h,System.Func<int,bool> marked,int reach,out int edge)
        {
            edge=0;int covered=0;
            for(int i=0;i<mask.Length;i++)
            {
                if(!mask[i]||!Edge(mask,w,h,i))continue;
                edge++;int x=i%w,y=i/w;bool found=false;
                for(int dy=-reach;dy<=reach&&!found;dy++)for(int dx=-reach;dx<=reach&&!found;dx++)
                {int px=x+dx,py=y+dy;if(px>=0&&py>=0&&px<w&&py<h&&marked(py*w+px))found=true;}
                if(found)covered++;
            }
            return covered/(float)Mathf.Max(1,edge);
        }
        /// <summary>The mask without its outer <paramref name="depth"/> pixels: what lies inside the line.</summary>
        public static bool[] Inside(bool[] mask,int w,int h,int depth)
        {
            var now=(bool[])mask.Clone();
            for(int pass=0;pass<depth;pass++){var next=(bool[])now.Clone();for(int i=0;i<now.Length;i++)if(now[i]&&Edge(now,w,h,i))next[i]=false;now=next;}
            return now;
        }
    }

    /// <summary>
    /// D414: a hero hidden by scenery, a wreck or a boss is one clean line round the hidden part, gold for the lead and pale blue for the partner, with at most a faint flat tint
    /// inside it, so what is in front stays readable through the figure and the hero reads as behind it, not on it. The old drawing (a Fresnel rim over a fill) was a bright,
    /// nearly solid glass figure from the fixed high camera, and two reviewers of the nov24-whole capture read Sela as standing on the Arch Market's arch (s13/007), sitting
    /// on Tallow's lift door (s11/007) and on the Burrower (s06/048, 050), and the party as walking on Harrowgate's gate roof (s50/071). At those places, from the game's
    /// camera at 1920 by 1080: the old drawing (a fixture shader, Tests/PlayMode/Fixtures/HeroThroughBefore.shader, with the old values) fails first, then the game's own.
    /// Stills of both go to Builds/quality/look/d414.
    /// </summary>
    public sealed class HeroOutlineTests
    {
        const int W=1920,H=1080;
        /// <summary>Of the hidden part's edge, the least share the line closes round (within <see cref="Reach"/> pixels of 1080). Not all of it: where a body is pressed against
        /// what hides it, closer than its own thickness (Sela against Tallow's door frame), nothing is drawn, as an arm across the chest isn't.</summary>
        const float LeastClosed=.8f;
        /// <summary>Inside the line: the most the drawing may change what is in front on average (a faint tint, in the frame's sRGB, where a dark surface shows a tint most), the
        /// most of it it may cover nearly whole, and how closely what is in front must still read through it (the correlation of its light with and without the drawing: the old
        /// glass figure -.23 to .51 at these places, the line and its tint .90 to 1.00 at the first renders, .90 being Sela under Harrowgate's gate roof in its shadow band).</summary>
        const float MostInsideChange=.15f,MostInsideCovered=.05f,LeastReadable=.8f;
        const int Reach=4;

        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            if(Camera.main!=null)Camera.main.ResetAspect();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        static void Flag(string key)=>GameServices.Current.Flags.SetBool(key,true);
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.FirstOrDefault(m=>m!=Lead&&m.gameObject.activeInHierarchy);
        static bool Graphics=>SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Null;
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            yield return new WaitForSecondsRealtime(.8f);
        }
        static void Put(CombatActor hero,Vector3 at)
        {
            Assert.IsTrue(NavMesh.SamplePosition(at+Vector3.up*.5f,out var floor,3,NavMesh.AllAreas),$"fixture: no floor near {at}");
            var body=hero.GetComponent<CharacterController>();body.enabled=false;
            if(hero.TryGetComponent<GroundMotor>(out var feet))feet.Halt();
            hero.transform.position=floor.position;body.enabled=true;Physics.SyncTransforms();
        }
        static IEnumerator Still()
        {
            var last=Camera.main.transform.position;float since=Time.unscaledTime,deadline=Time.unscaledTime+8;
            while(Time.unscaledTime-since<.6f&&Time.unscaledTime<deadline)
            {yield return null;var now=Camera.main.transform.position;if(Vector3.Distance(last,now)>.01f){last=now;since=Time.unscaledTime;}}
        }
        static int ProbeLayer{get{for(int l=31;l>=8;l--)if(string.IsNullOrEmpty(LayerMask.LayerToName(l)))return l;return 31;}}
        static Color32[] Shot(Camera camera,int only=-1,string keep=null)
        {
            var image=new Texture2D(W,H,TextureFormat.RGB24,false);var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);
            var active=RenderTexture.active;var target=camera.targetTexture;int mask=camera.cullingMask;var clear=camera.clearFlags;var back=camera.backgroundColor;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;
                // Not only>=0: the probe layer is often 31, and 1<<31 is negative.
                if(only!=-1){camera.cullingMask=only;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;}
                camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();
                if(keep!=null)
                {
                    // Under Builds/quality/look: d414/ the line's first renders, d421/ the window's and the sliver's.
                    var file=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/look",keep.Contains('/')?keep:"d414/"+keep)+".png");
                    Directory.CreateDirectory(Path.GetDirectoryName(file));File.WriteAllBytes(file,image.EncodeToPNG());
                }
                return image.GetPixels32();
            }
            finally{camera.cullingMask=mask;camera.clearFlags=clear;camera.backgroundColor=back;camera.targetTexture=target;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);}
        }

        /// <summary>The capture's places (frames.csv at the still's step: the lead's and the partner's feet). Sorrel's is the Burrower's wreck on a later visit, the partner put
        /// behind its middle for the camera, as the capture had her behind the beaten body.
        /// D421: the Arch Market's place (nov24 s13/007) is gone from here: Sela is hidden whole by the arch there, and stands in its window instead
        /// (<see cref="AHeroWhollyBehindOneBroadPieceStandsInItsWindow"/>).</summary>
        static readonly (string name,string zone,string[] flags,Vector3 lead,Vector3 partner)[] Places=
        {
            ("s11-007-lift-door","TallowDrift",new string[0],new Vector3(0,.08f,-8),new Vector3(1.631f,.08f,-10.078f)),
            ("s06-050-burrower-wreck","Sorrel_Ridges",new[]{"met.Survivor","bossdown.Burrower","clear.Sorrel_Burrower"},new Vector3(-12.74f,0,167.73f),Vector3.zero),
            ("s50-071-gatehouse","Harrowgate",new string[0],new Vector3(7.996f,2.48f,28.197f),new Vector3(9.719f,2.48f,27.628f)),
        };
        public static IEnumerable<string> PlaceNames=>Places.Select(p=>p.name);

        static Material Before(Shader shader,bool lead)
        {
            // HeroThrough's values before D414.
            var material=new Material(shader){name="Hero through before D414"};
            material.SetColor("_BaseColor",lead?new Color(1,.8f,.42f,1):new Color(.6f,.88f,1,1));material.SetColor("_RimColor",lead?Color.white:new Color(.8f,.95f,1,1));
            material.SetFloat("_Fill",lead?.4f:.3f);material.SetFloat("_Intensity",lead?1.6f:1.15f);material.SetFloat("_Sharpness",1.5f);material.SetFloat("_Thickness",HeroThrough.Thickness);
            material.renderQueue=(int)RenderQueue.Transparent+40;
            return material;
        }

        [UnityTest,Timeout(600000)] public IEnumerator AHiddenHeroIsOneCleanLineWithWhatHidesThemReadableInside([ValueSource(nameof(PlaceNames))] string place)
        {
            if(!Graphics)Assert.Ignore("reads real pixels: run with the -Graphics runner");
            var (name,zone,flags,leadAt,partnerAt)=Places.Single(p=>p.name==place);
            foreach(var flag in flags)Flag(flag);
            yield return Zone(zone);
            var partner=Partner;Assert.IsNotNull(partner,"fixture: a partner out");
            Put(Lead,leadAt);
            if(partnerAt==Vector3.zero)
            {
                // Behind the wreck's middle for the camera, at chest height, and the lead a few metres to its side, where the capture had him.
                var site=Object.FindFirstObjectByType<BossWreck>();Assert.IsNotNull(site,"fixture: the wreck's site");Assert.IsTrue(site.wreck.activeInHierarchy,"fixture: a later visit finds the wreck");
                var points=site.rig.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToList();
                float floor=points.Min(p=>p.y);var middle=new Vector3(points.Average(p=>p.x),floor+(points.Max(p=>p.y)-floor)*.6f,points.Average(p=>p.z));
                var look=Camera.main.transform.forward;Put(Lead,middle-Vector3.Cross(Vector3.up,new Vector3(look.x,0,look.z).normalized)*4.5f-new Vector3(look.x,0,look.z).normalized*1.5f);
                yield return new WaitForSecondsRealtime(1.5f);yield return Still();
                look=Camera.main.transform.forward;partnerAt=middle+look*((middle.y-(floor+.95f))/-look.y);
            }
            Put(partner,partnerAt);
            yield return new WaitForSecondsRealtime(1.5f);yield return Still();yield return null;
            var camera=Camera.main;var old=Shader.Find("Hidden/Lattice/HeroThroughBefore");Assert.IsNotNull(old,"fixture: the old drawing's shader (Tests/PlayMode/Fixtures)");
            var heroes=new[]{Lead,partner};
            var through=heroes.ToDictionary(h=>h,h=>h.GetComponent<HeroThrough>());
            foreach(var h in heroes)Assert.Greater(through[h].Drawn,0,$"fixture: {h.character} is drawn through");
            Renderer[] Own(CombatActor h)=>h.GetComponentsInChildren<Renderer>().Where(r=>(r is MeshRenderer||r is SkinnedMeshRenderer)&&r.enabled&&!r.transform.IsChildOf(through[h].Holder)).ToArray();
            void Show(IEnumerable<Renderer> set,bool shown){foreach(var r in set)if(r!=null)r.forceRenderingOff=!shown;}
            var copies=heroes.SelectMany(h=>through[h].Copies).ToArray();
            var faults=new List<string>();int measured=0;
            foreach(var h in heroes)foreach(var r in Own(h))r.shadowCastingMode=ShadowCastingMode.Off;
            // All shots in one frame, nothing running between them.
            // D421: kept beside round five's own renders of these places (d414/<place>-after), which stay as they were.
            var now=Shot(camera,-1,"d421/"+name+"-now");
            Show(copies,false);var bare=Shot(camera);
            var swapped=new Dictionary<Renderer,Material[]>();
            foreach(var h in heroes)foreach(var copy in through[h].Copies)
            {
                if(copy.name.EndsWith("(through line)"))continue;
                swapped[copy]=copy.sharedMaterials;var before=Before(old,h==Lead);copy.sharedMaterials=Enumerable.Repeat(before,copy.sharedMaterials.Length).ToArray();copy.forceRenderingOff=false;
            }
            var then=Shot(camera);
            foreach(var (copy,materials) in swapped)copy.sharedMaterials=materials;
            Show(copies,true);
            int probe=ProbeLayer;var empty=Shot(camera,1<<probe);
            foreach(var h in heroes)
            {
                var own=Own(h);var layers=own.Select(r=>r.gameObject.layer).ToArray();
                foreach(var r in own)r.gameObject.layer=probe;var alone=Shot(camera,1<<probe);for(int i=0;i<own.Length;i++)own[i].gameObject.layer=layers[i];
                Show(copies,false);Show(own,false);var gone=Shot(camera);Show(own,true);Show(copies,true);
                // His silhouette, and of it what the frame doesn't show of him (taking him out changes nothing there).
                var hidden=new bool[W*H];int silhouette=0,count=0;
                for(int i=0;i<hidden.Length;i++){if(OutlineMeasure.Change(alone[i],empty[i])<=.03f)continue;silhouette++;if(OutlineMeasure.Change(bare[i],gone[i])<=.04f){hidden[i]=true;count++;}}
                // A few hundred pixels of a hero in plain view whose colour happens to match the ground behind him are not hidden: a fifth of him at least. D421: and the line
                // is drawn whole only once about half of him is hidden (HeroThrough.ShareLow to ShareHigh), so it is read where .45 of him is; a sliver is drawn by nothing
                // (AHeroHiddenOnlyAtTheFeetIsNotDrawnThrough). Sela behind the Burrower's wreck is .52 hidden, her head and shoulders over it.
                if(count<300||count<silhouette*.45f){TestContext.WriteLine($"HERO_OUTLINE {name} {h.character}: hidden {count} of {silhouette} px, not measured");continue;}
                measured++;var inside=OutlineMeasure.Inside(hidden,W,H,Reach+3);int insideCount=inside.Count(b=>b);
                // What is inside is judged only where there is enough of it: of thin limbs (Sela's legs on the wreck, s06) a few hundred pixels are left, each beside the line,
                // on a near-black surface with next to no light of its own to read through.
                bool wide=insideCount>=500;
                static float Light(Color32 c)=>(.2126f*c.r+.7152f*c.g+.0722f*c.b)/255f;
                (float closed,float change,float covered,float readable) Read(Color32[] frame)
                {
                    float closed=OutlineMeasure.Covered(hidden,W,H,i=>OutlineMeasure.Change(frame[i],bare[i])>.1f,Reach,out _);
                    float sum=0;int strong=0,tinted=0;double sx=0,sy=0,sxx=0,syy=0,sxy=0;
                    for(int i=0;i<inside.Length;i++)if(inside[i])
                    {
                        float c=OutlineMeasure.Change(frame[i],bare[i]);sum+=c;if(c>.25f){strong++;continue;}
                        // D421: read through the tint, where the drawing doesn't cover; what it covers nearly whole is bounded on its own (covered). Two in a hundred pixels of a
                        // hair strand's stroke inside Sela under Harrowgate's gate roof took the read of a flat roof from .86 to .64 by her pose alone.
                        double x=Light(bare[i]),y=Light(frame[i]);sx+=x;sy+=y;sxx+=x*x;syy+=y*y;sxy+=x*y;tinted++;
                    }
                    int n=Mathf.Max(1,tinted);double vx=sxx/n-sx*sx/n/n,vy=syy/n-sy*sy/n/n,cov=sxy/n-sx*sy/n/n;
                    float readable=vx>1e-6&&vy>1e-6?(float)(cov/System.Math.Sqrt(vx*vy)):1;
                    int all=Mathf.Max(1,insideCount);
                    return (closed,sum/all,strong/(float)all,readable);
                }
                var was=Read(then);var is_=Read(now);
                string line=$"HERO_OUTLINE {name} {h.character} ({(h==Lead?"lead":"partner")}): hidden {count} of {silhouette} px, {insideCount} inside the line; " +
                    $"before: closed {was.closed:0.00} inside change {was.change:0.000} covered {was.covered:0.00} readable {was.readable:0.00}; now: closed {is_.closed:0.00} inside change {is_.change:0.000} covered {is_.covered:0.00} readable {is_.readable:0.00}";
                TestContext.WriteLine(line);Debug.Log(line);
                bool Clean((float closed,float change,float covered,float readable) r)=>r.closed>=LeastClosed&&(!wide||r.change<=MostInsideChange&&r.covered<=MostInsideCovered&&r.readable>=LeastReadable);
                if(wide&&Clean(was))faults.Add("fixture: the old glass figure passes as a clean line: "+line);
                if(!Clean(is_))faults.Add($"not one clean line (closed at least {LeastClosed:0.00}; inside at most {MostInsideChange:0.00} on average, {MostInsideCovered:0.00} covered, read through at least {LeastReadable:0.00}): "+line);
            }
            foreach(var h in heroes)foreach(var r in Own(h))r.shadowCastingMode=ShadowCastingMode.On;
            Assert.Greater(measured,0,$"fixture: at {name} neither hero is hidden by 300 px");
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        static IEnumerator Stage(string zone,Vector3 leadAt,Vector3 partnerAt)
        {
            yield return Zone(zone);
            // The shots are 1920 by 1080; the runner's screen is not. A window's ellipse is placed in LateUpdate by the camera as it stands, so it stands at the shots' shape (in the
            // player the screen and the frame are one): at the runner's 4 by 3 Sela's window at the Arch Market sat a quarter wider of the middle than she did.
            Camera.main.aspect=W/(float)H;
            Assert.IsNotNull(Partner,"fixture: a partner out");
            Put(Lead,leadAt);Put(Partner,partnerAt);
            yield return new WaitForSecondsRealtime(1.5f);yield return Still();yield return null;
        }
        static Renderer[] CopiesOf(CombatActor hero)=>hero.GetComponent<HeroThrough>().Copies.ToArray();
        static Renderer[] AllCopies=>PartyController.Current.members.Where(m=>m!=null&&m.gameObject.activeInHierarchy&&m.GetComponent<HeroThrough>()!=null).SelectMany(CopiesOf).ToArray();
        static void Off(IEnumerable<Renderer> set,bool off){foreach(var r in set)if(r!=null)r.forceRenderingOff=off;}
        /// <summary>Of the hero's silhouette (his body alone on its own layer), the share where his own body shows in the frame (taking him out changes it, nothing drawn through
        /// counted), and how many pixels his own drawing-through changes. All shots in one frame, his shadow kept out.</summary>
        static (float shows,int drawn) Showing(CombatActor hero,string keep)
        {
            var camera=Camera.main;var holder=hero.GetComponent<HeroThrough>().Holder;
            var own=hero.GetComponentsInChildren<Renderer>().Where(r=>(r is MeshRenderer||r is SkinnedMeshRenderer)&&r.enabled&&!r.forceRenderingOff&&!r.transform.IsChildOf(holder)).ToArray();
            var casting=own.Select(r=>r.shadowCastingMode).ToArray();var layers=own.Select(r=>r.gameObject.layer).ToArray();
            var mine=CopiesOf(hero);var all=AllCopies;
            try
            {
                foreach(var r in own)r.shadowCastingMode=ShadowCastingMode.Off;
                var now=Shot(camera,-1,keep);
                Off(mine,true);var without=Shot(camera);
                Off(all,true);var bare=Shot(camera);Off(own,true);var gone=Shot(camera);Off(own,false);
                int probe=ProbeLayer;var empty=Shot(camera,1<<probe);
                foreach(var r in own)r.gameObject.layer=probe;var alone=Shot(camera,1<<probe);
                int silhouette=0,shows=0,drawn=0;
                for(int i=0;i<now.Length;i++)
                {
                    if(OutlineMeasure.Change(now[i],without[i])>.1f)drawn++;
                    if(OutlineMeasure.Change(alone[i],empty[i])<=.03f)continue;
                    silhouette++;if(OutlineMeasure.Change(bare[i],gone[i])>.04f)shows++;
                }
                Assert.Greater(silhouette,300,$"fixture: {hero.character} is on the screen");
                return (shows/(float)silhouette,drawn);
            }
            finally
            {
                Off(all,false);
                for(int i=0;i<own.Length;i++)if(own[i]!=null){own[i].shadowCastingMode=casting[i];own[i].gameObject.layer=layers[i];}
            }
        }

        /// <summary>D421: where one broad piece of scenery hid a hero whole in the nov25-r5-chain capture (frames.csv at the still's step), and whom. Round five drew only their
        /// outline there, lying wholly on the lit face in front, and three reviewers read it as the hero standing ON the arch, walking along the gate roof ("a ghost"), or on a
        /// pillar's lamp ledge: nothing of them showed past an edge to say they were behind it.</summary>
        static readonly (string name,string zone,Vector3 lead,Vector3 partner,bool partnerHidden)[] Windows=
        {
            ("s13-007-arch-top","Lanternwick",new Vector3(0,.192f,1.24f),new Vector3(2.003f,.192f,-2.532f),true),
            ("s51-218-gate-roof","Harrowgate",new Vector3(7.994f,2.402f,26.513f),new Vector3(9.941f,.08f,22.723f),false),
            ("s26-195-arch-pillar","DrownedWaystation",new Vector3(2.217f,.38f,171.974f),new Vector3(3.731f,.38f,171.535f),true),
        };
        public static IEnumerable<string> WindowNames=>Windows.Select(p=>p.name);

        /// <summary>D421: a hero hidden whole by one broad piece stands in its window, in their own colours on the floor under it, and is not also drawn through it. Round five's
        /// state (no piece opens a window for a hero drawn through it: every SceneryOcclusion off) fails first. Stills of both go to Builds/quality/look/d421.</summary>
        [UnityTest,Timeout(600000)] public IEnumerator AHeroWhollyBehindOneBroadPieceStandsInItsWindow([ValueSource(nameof(WindowNames))] string place)
        {
            if(!Graphics)Assert.Ignore("reads real pixels: run with the -Graphics runner");
            var (name,zone,leadAt,partnerAt,partnerHidden)=Windows.Single(p=>p.name==place);
            yield return Stage(zone,leadAt,partnerAt);
            var hero=partnerHidden?Partner:Lead;
            var pieces=Object.FindObjectsByType<SceneryOcclusion>(FindObjectsSortMode.None);
            foreach(var piece in pieces)piece.enabled=false;
            yield return null;yield return null;
            var (before,drawnBefore)=Showing(hero,"d421/"+name+"-before");
            foreach(var piece in pieces)piece.enabled=true;
            yield return new WaitForSecondsRealtime(1);yield return null;
            var (after,drawn)=Showing(hero,"d421/"+name+"-after");
            string line=$"HERO_WINDOW {name} {hero.character}: own colours show over {before:0.00} of the silhouette before (drawn through {drawnBefore} px), {after:0.00} now (drawn through {drawn} px)";
            TestContext.WriteLine(line);Debug.Log(line);
            Assert.Less(before,.3f,"fixture: the hero isn't hidden whole here: "+line);
            Assert.Greater(drawnBefore,300,"fixture: round five drew no outline here: "+line);
            Assert.GreaterOrEqual(after,.75f,"a hero hidden whole by one broad piece doesn't stand in its window: "+line);
            Assert.Less(drawn,80,"a hero in a window is drawn through as well: "+line);
        }

        /// <summary>D427: on the Walking Canopy (nov25-r6-chain s21/295) one root arch hid both heroes whole: Taren stood in its window, and Sela, behind the same root, was only her
        /// line, lying on the root's upper face, and read as standing on top of it (D421 opened one window a piece, the lead's first). She stands in a window of her own in it now,
        /// hero-sized, beside his. Round six's state (the piece's second window shut just before the shots) fails first. Stills go to Builds/quality/look/d427.</summary>
        [UnityTest,Timeout(600000)] public IEnumerator APartnerHiddenWholeByThePieceHoldingTheLeadsWindowStandsInHerOwn()
        {
            if(!Graphics)Assert.Ignore("reads real pixels: run with the -Graphics runner");
            yield return Stage("WalkingCanopy",new Vector3(22.713f,-4.328f,30.072f),new Vector3(25.111f,-4.328f,28.126f));
            yield return new WaitForSecondsRealtime(1);yield return null;
            var partner=Partner;var block=new MaterialPropertyBlock();
            // Between the pieces' LateUpdate and the shots (all in this frame): one window a piece, as D421 had it.
            foreach(var piece in Object.FindObjectsByType<SceneryOcclusion>(FindObjectsSortMode.None))
                foreach(var body in piece.GetComponentsInChildren<Renderer>(true)){body.GetPropertyBlock(block);block.SetVector("_OccFadeEllipse2",Vector4.zero);body.SetPropertyBlock(block);}
            var (before,drawnBefore)=Showing(partner,"d427/s21-295-canopy-root-before");
            yield return null;yield return null;
            var (after,drawn)=Showing(partner,"d427/s21-295-canopy-root-after");
            var (lead,leadDrawn)=Showing(Lead,null);
            string line=$"HERO_SECOND_WINDOW s21-295 {partner.character}: own colours over {before:0.00} of her with one window a piece (drawn through {drawnBefore} px), {after:0.00} now (drawn through {drawn} px); {Lead.character} {lead:0.00} (drawn through {leadDrawn} px)";
            TestContext.WriteLine(line);Debug.Log(line);
            // The root is a lattice of strands: between them a third of her shows in her colours with one window a piece, and the line over the rest is what read wrong.
            Assert.Less(before,.5f,"fixture: with one window a piece most of her isn't hidden here: "+line);
            Assert.Greater(drawnBefore,300,"fixture: with one window a piece she isn't drawn through here: "+line);
            Assert.GreaterOrEqual(lead,.75f,"fixture: the lead doesn't stand in the root's window here: "+line);
            Assert.GreaterOrEqual(after,.75f,"the partner hidden whole by the piece that holds the lead's window doesn't stand in her own: "+line);
            Assert.Less(drawn,80,"the partner in her window is drawn through as well: "+line);
        }

        /// <summary>D421: where only a hero's feet were hidden, behind a thin rail on the Meridian Crawl (nov25-r5-chain s55/139, 146): round five's line round that sliver read
        /// as a loose cyan scrap below Idra, who stood in plain view.</summary>
        static readonly (string name,string zone,Vector3 lead,Vector3 partner)[] Slivers=
        {
            ("s55-139-rail","MeridianCrawl",new Vector3(-66.459f,.233f,.001f),new Vector3(-69.958f,.233f,-1.35f)),
            ("s55-146-rail","MeridianCrawl",new Vector3(23.561f,.08f,-3),new Vector3(19.807f,.08f,-4.948f)),
        };
        public static IEnumerable<string> SliverNames=>Slivers.Select(p=>p.name);

        /// <summary>D421: a hero hidden only in a small part is not drawn through at all (the share hidden under HeroThrough.ShareLow). Round five's drawing (wherever hidden,
        /// however little: the materials' share bounds set below zero) fails first. Stills of both go to Builds/quality/look/d421.</summary>
        [UnityTest,Timeout(600000)] public IEnumerator AHeroHiddenOnlyAtTheFeetIsNotDrawnThrough([ValueSource(nameof(SliverNames))] string place)
        {
            if(!Graphics)Assert.Ignore("reads real pixels: run with the -Graphics runner");
            var (name,zone,leadAt,partnerAt)=Slivers.Single(p=>p.name==place);
            yield return Stage(zone,leadAt,partnerAt);
            var camera=Camera.main;var copies=AllCopies;
            var looks=copies.SelectMany(c=>c.sharedMaterials).Where(m=>m!=null).Distinct().ToArray();
            Assert.IsNotEmpty(looks,"fixture: the heroes' drawing-through");
            int Changed(Color32[] a,Color32[] b){int n=0;for(int i=0;i<a.Length;i++)if(OutlineMeasure.Change(a[i],b[i])>.1f)n++;return n;}
            Color32[] then,now,bare;
            try
            {
                foreach(var look in looks){look.SetFloat("_ShareLow",-1);look.SetFloat("_ShareHigh",-.5f);}
                then=Shot(camera,-1,"d421/"+name+"-before");
            }
            finally{foreach(var look in looks){look.SetFloat("_ShareLow",HeroThrough.ShareLow);look.SetFloat("_ShareHigh",HeroThrough.ShareHigh);}}
            now=Shot(camera,-1,"d421/"+name+"-after");
            Off(copies,true);bare=Shot(camera);Off(copies,false);
            int was=Changed(then,bare),is_=Changed(now,bare);
            string line=$"HERO_SLIVER {name}: drawn through {was} px before, {is_} px now";
            TestContext.WriteLine(line);Debug.Log(line);
            Assert.Greater(was,40,"fixture: nothing is hidden here to draw: "+line);
            Assert.Less(is_,15,"a sliver of a hero is still drawn through: "+line);
        }
    }
}
