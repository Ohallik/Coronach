using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>What of the look pass on chapters 1 and 2 after the whole-game capture of November 20 can be measured (the rest is judged on stills: nov20-whole s02 to s16 beside
    /// Builds/quality/look/ch12-after). Each case builds the state the capture showed first and requires the fault there, so a check that can't fail can't pass.</summary>
    public sealed class ChapterOneTwoLookTests
    {
        CombatActor Taren=>PartyController.Current.members.First(m=>m.character=="Taren");
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+15;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"Arena_Ground never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.7f);
        }
        [UnityTearDown] public IEnumerator Cleanup(){GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static void Floor(){var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Look fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();if(body!=null)body.enabled=false;actor.transform.position=position;if(body!=null)body.enabled=true;actor.target=null;actor.TargetLocked=false;Physics.SyncTransforms();
        }
        static Bounds Drawn(Transform root){var parts=root.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)).ToArray();var all=parts[0].bounds;foreach(var p in parts)all.Encapsulate(p.bounds);return all;}
        /// <summary>Walks a hero's own body straight at a point, a step a frame, until it stops or gets there: where it stood.</summary>
        static IEnumerator Walk(CombatActor hero,Vector3 toward,float[] reached)
        {
            var body=hero.GetComponent<CharacterController>();
            for(int i=0;i<160;i++)
            {
                var step=toward-hero.transform.position;step.y=0;if(step.magnitude<.05f)break;
                body.Move(step.normalized*Mathf.Min(.08f,step.magnitude)+Vector3.down*.02f);yield return null;
            }
            reached[0]=hero.transform.position.z;
        }

        /// <summary>s06/025, 030: Taren stood inside the Burrower's drill. The rig is drawn five and a half metres long and its body was the upright capsule of any rig, 2.8 m across,
        /// so a hero walked a metre into its mouth, up to that capsule. Its length is solid as drawn now, its flanks as they were, and a blade swung from where its drill stops a hero lands.</summary>
        [UnityTest] public IEnumerator TheBurrowersDrillAndTailAreSolidWhereTheyAreDrawn()
        {
            Floor();
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Burrower"),new Vector3(230,.05f,0));enemy.Passive=true;enemy.transform.rotation=Quaternion.identity;
            yield return Frames(4);
            var drawn=Drawn(enemy.GetComponent<DefeatPresentation>().visual);float front=drawn.max.z,back=drawn.min.z,flank=drawn.max.x-230;
            var own=enemy.GetComponent<CharacterController>();
            Assert.Greater(front,own.radius+.6f,"fixture: the rig is drawn longer than its upright capsule");
            var length=enemy.transform.Find("Burrower length");Assert.IsNotNull(length,"the Burrower has no body along its drawn length");
            // The build the capture played: no length, only the upright capsule.
            var solid=length.GetComponent<CapsuleCollider>();var hurt=enemy.transform.Find("Burrower length hurtbox");
            solid.gameObject.SetActive(false);hurt.gameObject.SetActive(false);
            var reached=new float[1];Place(Taren,new Vector3(230,.1f,front+3));yield return Walk(Taren,new Vector3(230,.1f,0),reached);
            Assert.Less(reached[0],front-.6f,$"fixture: as the capture had it, a hero walks {front-reached[0]:0.0} m into the drawn drill");
            solid.gameObject.SetActive(true);hurt.gameObject.SetActive(true);Physics.SyncTransforms();yield return Frames(2);
            // From in front, and from behind.
            Place(Taren,new Vector3(230,.1f,front+3));yield return Walk(Taren,new Vector3(230,.1f,0),reached);
            Assert.GreaterOrEqual(reached[0],front-.3f,$"a hero walks {front-reached[0]:0.0} m into the Burrower's drawn drill");
            var stopped=Taren.transform.position;
            var reach=Physics.OverlapSphere(stopped+Vector3.back*1.1f+Vector3.up*1,.6f,~0,QueryTriggerInteraction.Collide).Select(c=>c.GetComponent<Hurtbox>()).Where(b=>b!=null&&b.owner==enemy.GetComponent<Health>());
            Assert.IsTrue(reach.Any(),"stopped at the drill, a blade swung at it meets nothing: the rig can't be struck from in front");
            Place(Taren,new Vector3(230,.1f,back-3));yield return Walk(Taren,new Vector3(230,.1f,0),reached);
            Assert.LessOrEqual(reached[0],back+.3f,$"a hero walks {reached[0]-back:0.0} m into the Burrower's drawn tail");
            // Its flank is where it was: the length is no wider than the upright capsule.
            Place(Taren,new Vector3(230+flank+3,.1f,0));var across=new float[1];
            var body=Taren.GetComponent<CharacterController>();for(int i=0;i<160;i++){body.Move(Vector3.left*.08f);yield return null;}
            float side=Taren.transform.position.x-230;
            Assert.LessOrEqual(side,own.radius+body.radius+.15f,$"at its flank a hero stops {side:0.00} m out, wider than its capsule ({own.radius+body.radius:0.00}): the weak spots there are out of reach");
        }

        // ---------------------------------------------------------------- the second round (nov22-whole, D360)
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static void NeedsGraphics()=>Assert.AreNotEqual(UnityEngine.Rendering.GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType,"this reads real pixels: run it with the -Graphics runner");
        /// <summary>A zone with nothing in it fighting and nobody steering: the camera is put where the game's would be, by hand.</summary>
        static IEnumerator Zone(string zone,params string[] flags)
        {
            foreach(var flag in flags)GameServices.Current.Flags.SetBool(flag,true);
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var flag in flags)GameServices.Current.Flags.SetBool(flag,true);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var volume in All<EncounterVolume>())volume.enabled=false;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            // The rig and the brain that carries its virtual cameras onto the lens: with the brain left on, the lens stayed where the rig last left it, at the arrival.
            var rig=Object.FindFirstObjectByType<CameraRig>();if(rig!=null)rig.enabled=false;
            foreach(var part in Camera.main.GetComponents<Behaviour>())if(part.GetType().Name=="CinemachineBrain")part.enabled=false;
            Camera.main.aspect=16f/9;
            yield return new WaitForSecondsRealtime(.8f);
        }
        /// <summary>The game's camera where it stands for a body at <paramref name="at"/>: the profile's distance back along its pitch and yaw from the body's feet (nov22-whole frames.csv).</summary>
        static void Frame(Vector3 at,string profile,float zoom=1)
        {
            var view=Resources.Load<CameraProfile>("Definitions/CameraProfile/"+profile);var turn=Quaternion.Euler(view.pitch,view.yaw,0);var camera=Camera.main;
            camera.fieldOfView=view.fov;camera.transform.SetPositionAndRotation(at-turn*Vector3.forward*view.distance*zoom,turn);
        }
        static float GroundAt(Vector2 p){return Physics.Raycast(new Vector3(p.x,40,p.y),Vector3.down,out var hit,80,~0,QueryTriggerInteraction.Ignore)?hit.point.y:0;}
        /// <summary>What the game's camera draws, without the HUD, at the capture's size. Row 0 is the bottom of the screen.</summary>
        static Color[] Grab(int W=960,int H=540)
        {
            var camera=Camera.main;var image=new Texture2D(W,H,TextureFormat.RGB24,false);var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var active=RenderTexture.active;var target=camera.targetTexture;
            try{Assert.IsTrue(rt.Create());camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();return image.GetPixels();}
            finally{camera.targetTexture=target;RenderTexture.active=active;rt.Release();Object.Destroy(rt);Object.Destroy(image);}
        }
        /// <summary>The HUD's panels over a 1920 by 1080 frame, top-left origin: the status panel, the zone's name, the objective, the control strip, the skills.</summary>
        static readonly RectInt[] Panels={new(20,20,515,285),new(1660,20,245,50),new(1340,95,560,165),new(130,1005,990,47),new(1270,925,590,150)};
        static bool UnderAPanel(int x,int y,int W,int H){float sx=x*1920f/W,sy=(H-1-y)*1080f/H;foreach(var p in Panels)if(sx>=p.xMin&&sx<p.xMax&&sy>=p.yMin&&sy<p.yMax)return true;return false;}
        /// <summary>Of the frame a player sees round the HUD's panels, the share these renderers are drawn over: what changes when they are hidden.</summary>
        static float Share(Renderer[] parts)
        {
            const int W=960,H=540;var with=Grab(W,H);foreach(var p in parts)p.enabled=false;var without=Grab(W,H);foreach(var p in parts)p.enabled=true;
            int seen=0,drawn=0;
            for(int y=0;y<H;y++)for(int x=0;x<W;x++)
            {
                if(UnderAPanel(x,y,W,H))continue;seen++;var a=with[y*W+x];var b=without[y*W+x];
                if(Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)>.06f)drawn++;
            }
            return drawn/(float)seen;
        }

        /// <summary>The haul road from the outpost to the drill, and the fights on it (SorrelRedesign.Haul, Encounters).</summary>
        static readonly Vector2[] Haul={new(0,24),new(9,39),new(4,58),new(-8,75),new(-5,94),new(9,111),new(16,129),new(7,145)};
        static readonly Vector2[] HaulFights={new(9,45),new(-7,75),new(-4,101),new(15,129)};
        /// <summary>Where the drill site's apron begins: from there on the drill, its bench and its seals are in the frame, which is where the objective's ridges lead.</summary>
        const float DrillApron=135;
        static IEnumerable<Vector2> AlongTheHaul(float step)
        {
            for(int i=1;i<Haul.Length;i++){var a=Haul[i-1];var d=Haul[i]-a;float length=d.magnitude;for(float t=0;t<length;t+=step)yield return a+d*(t/length);}
        }
        /// <summary>s04/019, 054 (and s02/019, s04/020 before them): "Follow the ridges", and from the haul road the frame was road, scrub and one rock clump. Wherever the road isn't
        /// crossing a fight's clearing, from the outpost's gate to the drill site's apron, a ridge is in the frame round the HUD's panels now, as the game's camera sees it from the road's middle.
        /// On the capture's state (only the spine by the outpost, which is where the first round's corner spines were) almost none of the road had one.</summary>
        [UnityTest] public IEnumerator TheHaulRoadRunsBetweenRidges()
        {
            NeedsGraphics();yield return Zone("Sorrel_Ridges","met.Survivor");
            var spines=GameObject.Find("Sorrel ridge spines");Assert.IsNotNull(spines,"Sorrel has no ridge spines");
            var ridges=spines.GetComponentsInChildren<MeshRenderer>();
            Assert.GreaterOrEqual(ridges.Length,14,"a ridge was refused where it stood (CH1_LOOK_RIDGE_REFUSED in the builder's log): "+string.Join(", ",ridges.Select(r=>r.name)));
            var outpost=ridges.Where(r=>r.name=="Outpost ridge").ToArray();var lead=PartyController.Current.Active;
            var partner=PartyController.Current.members.First(m=>m!=lead);
            var now=new List<(Vector2 at,float share)>();var then=new List<(Vector2 at,float share)>();
            foreach(var p in AlongTheHaul(3))
            {
                if(p.y<30||p.y>DrillApron||HaulFights.Any(f=>Vector2.Distance(f,p)<10))continue;
                var feet=new Vector3(p.x,GroundAt(p),p.y);Place(lead,feet+Vector3.up*.1f);Place(partner,feet+new Vector3(1.8f,.1f,-1.6f));
                Frame(feet,"Ground");yield return null;yield return null;
                now.Add((p,Share(ridges)));
                // The capture's state: only the outpost's spine.
                foreach(var r in ridges.Except(outpost))r.enabled=false;then.Add((p,Share(outpost)));foreach(var r in ridges)r.enabled=true;
            }
            string Say(List<(Vector2 at,float share)> list)=>string.Join(" ",list.Select(s=>$"({s.at.x:0},{s.at.y:0}) {s.share:0.000}"));
            TestContext.WriteLine("HAUL_RIDGES now: "+Say(now)+"\nHAUL_RIDGES then: "+Say(then));
            Assert.Greater(now.Count,10,"fixture: the haul road's stretches between its fights");
            const float Seen=.03f;
            Assert.Greater(then.Count(s=>s.share<Seen),then.Count*2/3,"fixture: put back as the capture had it, most of the haul road has no ridge in frame: "+Say(then));
            var bare=now.Where(s=>s.share<Seen).ToList();
            Assert.IsEmpty(bare,$"from the haul road, where the objective says to follow the ridges, no ridge fills {Seen:P0} of the frame round the panels at: "+Say(bare));
        }
        /// <summary>s04/019, 054: the one rock clump either frame had was the same stacked rubble, at the bend below the strata and again past the upper ore. The verge's clumps (a
        /// rock three quarters of a metre tall or more within six and a half metres of the haul road's middle) are each a different piece.</summary>
        [UnityTest] public IEnumerator TheHaulRoadsVergeIsNotOneRockRepeated()
        {
            yield return Zone("Sorrel_Ridges","met.Survivor");
            string[] rocks={"HushwellRubble","DressBoulders","DressStrataLedge","DressSpoilHeap","DressStrataOutcrop","HushwellLowRidge","RidgeRock1","RidgeRock2","RidgeRock3"};
            List<string> Repeated()
            {
                var clumps=new List<(string kit,Vector3 at)>();
                foreach(var t in All<Transform>())
                {
                    if(!t.gameObject.activeInHierarchy||!rocks.Contains(t.name)||!(t.parent==null||t.parent.name=="Room dressing"||t.parent.name=="Haul verge rocks"))continue;
                    var parts=t.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();if(parts.Length==0)continue;var box=parts[0].bounds;foreach(var r in parts)box.Encapsulate(r.bounds);
                    var at=new Vector2(box.center.x,box.center.z);if(box.size.y<.75f||at.y<30||at.y>140)continue;
                    float road=float.PositiveInfinity;for(int i=1;i<Haul.Length;i++){var a=Haul[i-1];var d=Haul[i]-a;road=Mathf.Min(road,Vector2.Distance(at,a+d*Mathf.Clamp01(Vector2.Dot(at-a,d)/d.sqrMagnitude)));}
                    if(road<=6.5f)clumps.Add((t.name,box.center));
                }
                return clumps.GroupBy(c=>c.kit).Where(g=>g.Count()>1).Select(g=>g.Key+" at "+string.Join(", ",g.Select(c=>$"({c.at.x:0},{c.at.z:0})"))).ToList();
            }
            // The capture's state: the cave's stacked rubble where the verge's rocks stand, as the basin's dressing put it there.
            var verge=GameObject.Find("Haul verge rocks");Assert.IsNotNull(verge,"the haul road's verge rocks are missing");
            float Tall(Transform t){var parts=t.GetComponentsInChildren<Renderer>();if(parts.Length==0)return 0;var box=parts[0].bounds;foreach(var r in parts)box.Encapsulate(r.bounds);return box.size.y;}
            var rubble=All<Transform>().Where(t=>t.name=="HushwellRubble"&&t.parent==null).OrderByDescending(Tall).FirstOrDefault();Assert.IsNotNull(rubble,"fixture: a piece of the cave's rubble to copy");
            var copies=new List<GameObject>();
            foreach(Transform rock in verge.transform){var copy=Object.Instantiate(rubble.gameObject,rock.position,Quaternion.identity);copy.name="HushwellRubble";copies.Add(copy);}
            verge.SetActive(false);yield return null;
            var then=Repeated();
            verge.SetActive(true);foreach(var c in copies)Object.Destroy(c);yield return null;
            var now=Repeated();
            TestContext.WriteLine("VERGE then: "+string.Join("; ",then)+"\nVERGE now: "+string.Join("; ",now));
            Assert.IsNotEmpty(then,"fixture: with the rubble back on the verge, one piece is there twice");
            Assert.IsEmpty(now,"along the haul road the verge's rock clumps repeat one piece: "+string.Join("; ",now));
        }

        /// <summary>The line the way home over the Reach flies (the replay's own steps), and the sides of the flight frame between its panels (LanternRoadBuilder.ReachHome, ReachLeft, ReachRight).</summary>
        static readonly Vector2[] ReachHome={new(0,274),new(0,152),new(-5.8f,32.8f),new(-10.7f,8.6f),new(-12.8f,.9f)};
        static float ReachLaneX(float z)
        {
            for(int i=1;i<ReachHome.Length;i++){var a=ReachHome[i-1];var b=ReachHome[i];if(z<=a.y&&z>=b.y)return Mathf.Lerp(a.x,b.x,(z-a.y)/(b.y-a.y));}
            return 0;
        }
        static readonly Rect ReachLeft=new(.03f,.14f,.39f,.56f),ReachRight=new(.58f,.14f,.39f,.56f);
        /// <summary>s16/134, 136 and the video between: on the way home one rock island was in frame, cut by an edge, and the rest was starfield. Every four metres of the way home,
        /// at the flight camera's distance and drawn back as far as a boost draws it, each side of the frame has an island of the old road between the HUD's panels. With the islands
        /// this round added hidden (the capture's state), most of the way home has a bare side.</summary>
        [UnityTest] public IEnumerator TheWayHomeOverTheReachHasIslandsOnBothSides()
        {
            yield return Zone("WickReach");
            var added=GameObject.Find("The lantern road below, both sides");Assert.IsNotNull(added,"the Reach has no islands laid for both sides of the lane");
            var view=Resources.Load<CameraProfile>("Definitions/CameraProfile/Flight");
            var eye=new GameObject("Reach eye",typeof(Camera)).GetComponent<Camera>();eye.enabled=false;eye.fieldOfView=view.fov;eye.aspect=16f/9;eye.nearClipPlane=.3f;eye.farClipPlane=1500;
            List<string> Bare(float zoom)
            {
                var islands=All<Transform>().Where(t=>t.name=="Lantern road island"&&t.gameObject.activeInHierarchy).Select(t=>t.position).ToList();var bare=new List<string>();
                var turn=Quaternion.Euler(view.pitch,view.yaw,0);
                for(float z=12;z<=268;z+=4)
                {
                    eye.transform.SetPositionAndRotation(new Vector3(ReachLaneX(z),1,z)-turn*Vector3.forward*view.distance*zoom,turn);int left=0,right=0;
                    foreach(var p in islands){var v=eye.WorldToViewportPoint(p+Vector3.up*1.5f);if(v.z<=0)continue;if(ReachLeft.Contains(v))left++;if(ReachRight.Contains(v))right++;}
                    if(left==0||right==0)bare.Add($"{z:0}({left}/{right})");
                }
                return bare;
            }
            try
            {
                added.SetActive(false);var then=Bare(1);added.SetActive(true);
                var now=Bare(1);var back=Bare(1.12f);
                TestContext.WriteLine($"REACH_SIDES then {then.Count}: {string.Join(" ",then)}\nREACH_SIDES now {now.Count}: {string.Join(" ",now)}\nREACH_SIDES drawn back {back.Count}: {string.Join(" ",back)}");
                Assert.Greater(then.Count,30,"fixture: as the capture had it, most of the way home has a side of the frame with no island: "+string.Join(" ",then));
                Assert.IsEmpty(now,"on the way home a side of the flight frame has no island at: "+string.Join(" ",now));
                Assert.LessOrEqual(back.Count,2,"drawn back as a boost draws it, a side of the frame has no island at: "+string.Join(" ",back));
            }
            finally{Object.Destroy(eye.gameObject);}
        }

        /// <summary>What a body is drawn as: its skinned meshes, not what it carries.</summary>
        static Bounds Skinned(Transform root){var parts=root.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).ToArray();Assert.IsNotEmpty(parts,"fixture: "+root.name+" has a skinned body");var all=parts[0].bounds;foreach(var p in parts)all.Encapsulate(p.bounds);return all;}
        /// <summary>s15/135, 149: on screen Hessa read as one more of the party, a little taller, in orange. On screen, because she stands at the top of the frame when her fight
        /// opens: measured, drawn 1.3 times over she was already about 1.8 heroes tall (the review's 1.2 was her height in pixels, 26 m from the lens against heroes 19 m from it).
        /// What she lacked was a shape no hero has. Her outline now carries a standard well over her head and a stolen lantern past each shoulder; with her gear hidden (the
        /// capture's state) it had neither. She is drawn larger as well (1.55), and her capsule, what she hits and is hit by, is as it was.</summary>
        [UnityTest] public IEnumerator HessasOutlineIsNotAHeros()
        {
            Floor();
            var definition=GameCatalog.Find<EnemyDef>("HessaKorl");var hessa=ActorFactory.Enemy(definition,new Vector3(230,.05f,0));hessa.Passive=true;hessa.transform.rotation=Quaternion.identity;
            Place(Taren,new Vector3(236,.1f,0));yield return Frames(6);
            var visual=hessa.GetComponent<DefeatPresentation>().visual;var body=Skinned(visual);float hero=Skinned(Taren.transform).size.y;
            var gear=visual.GetComponentInChildren<HessaRegalia>(true);Assert.IsNotNull(gear,"Hessa's boss body carries no captain's gear");
            (float over,float wider) Outline()
            {
                var drawn=visual.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&(r is MeshRenderer or SkinnedMeshRenderer)).ToArray();
                var all=drawn[0].bounds;foreach(var r in drawn)all.Encapsulate(r.bounds);
                return (all.max.y-body.max.y,all.size.x-body.size.x);
            }
            gear.gameObject.SetActive(false);yield return null;var then=Outline();gear.gameObject.SetActive(true);yield return null;var now=Outline();
            TestContext.WriteLine($"HESSA_OUTLINE hero={hero:0.00} hessa={body.size.y:0.00} ({body.size.y/hero:0.00}x, drawn {definition.drawnScale}) over her head then={then.over:0.00} now={now.over:0.00} wider than her body then={then.wider:0.00} now={now.wider:0.00}");
            Assert.Less(then.over,.2f,"fixture: with her gear hidden, nothing stands over her head");
            Assert.GreaterOrEqual(now.over,.9f,$"what she carries stands {now.over:0.00} m over her head: from the play camera's height her outline is a hero's");
            var glass=gear.GetComponentsInChildren<Transform>().Where(t=>t.name=="Stolen relay glass").ToArray();Assert.AreEqual(2,glass.Length,"a lantern hung past each shoulder");
            var chest=visual.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.LeftUpperArm);float shoulder=Mathf.Abs(chest.position.x-hessa.transform.position.x);
            foreach(var g in glass)Assert.Greater(Mathf.Abs(g.position.x-hessa.transform.position.x),shoulder*1.5f,"a lantern hangs at her shoulder, not past it");
            Assert.GreaterOrEqual(body.size.y/hero,1.4f,$"Hessa is drawn {body.size.y/hero:0.00} times a hero's height");
            var cc=hessa.GetComponent<CharacterController>();
            Assert.AreEqual(3,cc.height,.01f,"her capsule's height changed: what she hits and is hit by is not as it was");Assert.AreEqual(.8f,cc.radius,.01f,"her capsule's width changed");
            Assert.IsEmpty(gear.GetComponentsInChildren<Collider>(true),"her gear has a collider: it is drawn only");
            Assert.AreSame(visual.GetComponentsInChildren<Renderer>().First(r=>r is MeshRenderer or SkinnedMeshRenderer).GetType(),typeof(SkinnedMeshRenderer),"her gear is found before her body's own mesh");
        }
        /// <summary>As she yields the fighter's body goes and the woman under guard takes its place in the same frame, in her plain body (QaStoryWorldTests.HessaUnderGuardWearsHerOwnBody).
        /// Her gear doesn't vanish with the fighter, which would pop it off her with the camera on her: it stays that frame and is down on the floor before she speaks (.4 s later),
        /// the standard lying behind her and the lanterns still burning where they were set down. With its handler off (the gear as it would be without one) it went with the body.</summary>
        [UnityTest] public IEnumerator HessasGearComesOffAsSheYields()
        {
            Floor();Place(Taren,new Vector3(236,.1f,0));
            IEnumerator Yield(bool handled,List<HessaRegalia> found)
            {
                var hessa=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),new Vector3(230,.05f,0));hessa.Passive=true;hessa.transform.rotation=Quaternion.identity;yield return Frames(4);
                var gear=hessa.GetComponentInChildren<HessaRegalia>(true);Assert.IsNotNull(gear,"fixture: her boss body carries her gear");gear.enabled=handled;
                var health=hessa.Health;health.Damageable=true;health.InvulnerableUntil=0;health.ShieldUntil=0;health.Plate=null;health.Holds=null;
                health.Receive(new DamagePacket{source=Taren.Health,amount=1e7f,type=DamageType.Kinetic});
                for(float end=Time.unscaledTime+8;hessa!=null&&Time.unscaledTime<end;)yield return null;
                Assert.IsTrue(hessa==null,"fixture: she yielded and the fighter's body went");
                found.Add(gear);
            }
            var off=new List<HessaRegalia>();yield return Yield(false,off);
            Assert.IsTrue(off[0]==null,"fixture: with no handler her gear goes with the fighter's body, in the frame she yields");
            var on=new List<HessaRegalia>();yield return Yield(true,on);var lying=on[0];
            Assert.IsTrue(lying!=null,"her gear vanished with the fighter's body as she yielded: it pops off her with the camera on her");
            Assert.IsTrue(lying.Down&&lying.transform.parent==null,"her gear didn't come off the fighter as she yielded");
            yield return new WaitForSecondsRealtime(HessaRegalia.Falls+.15f);
            var standard=lying.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Standard")).ToArray();var glass=lying.GetComponentsInChildren<Renderer>().Where(r=>r.transform.GetComponentsInParent<Transform>().Any(t=>t.name=="Stolen relay glass")).ToArray();
            float top=standard.Max(r=>r.bounds.max.y);
            TestContext.WriteLine($"HESSA_YIELD standard top {top:0.00} m over the floor after {HessaRegalia.Falls+.15f:0.00} s, lanterns {glass.Length} pieces lit={glass.All(r=>r.enabled)}");
            Assert.Less(top,1.2f,$"{HessaRegalia.Falls+.15f:0.00} s after she yields her standard still stands {top:0.00} m up: it isn't down before her first word");
            Assert.IsNotEmpty(glass,"her lanterns went");Assert.IsTrue(glass.All(r=>r.enabled&&r.bounds.max.y<1.5f),"her lanterns aren't set down on the floor, alight");
            Object.Destroy(lying.gameObject);
        }
        /// <summary>s15/135, 149: her warm orange rim was the colour of the lamps and the lantern that light her hall, so on its floor it didn't show. Its hue is well away from every
        /// coloured light within thirty metres of her post now; the capture's rim was within a few degrees of them.</summary>
        [UnityTest] public IEnumerator HessasRimIsAColourHerHallIsNot()
        {
            yield return Zone("SunkenRelay","relay.hessaConfronted");
            var post=new Vector3(3,0,129);
            var colours=All<Light>().Where(l=>l.enabled&&l.gameObject.activeInHierarchy&&l.gameObject.scene.name=="SunkenRelay"&&(l.type==LightType.Directional||Vector3.Distance(l.transform.position,post)<30))
                .Select(l=>l.color).Where(c=>{Color.RGBToHSV(c,out _,out float s,out _);return s>=.2f;}).ToList();
            Assert.Greater(colours.Count,2,"fixture: the hall's coloured lights");
            float Hue(Color c){Color.RGBToHSV(c,out float h,out _,out _);return h*360;}
            float Apart(Color rim)=>colours.Min(c=>{float d=Mathf.Abs(Hue(c)-Hue(rim))%360;return Mathf.Min(d,360-d);});
            var hessa=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),new Vector3(0,-200,0));hessa.Passive=true;yield return null;
            var rim=hessa.GetComponentsInChildren<Renderer>().First(r=>r is SkinnedMeshRenderer).sharedMaterial.GetColor("_RimColor");
            float then=Apart(new Color(1,.55f,.28f)),now=Apart(rim);
            TestContext.WriteLine($"HESSA_RIM rim={rim} hue={Hue(rim):0} then={then:0} now={now:0} lights={string.Join(" ",colours.Select(c=>Hue(c).ToString("0")))}");
            Assert.Less(then,20,"fixture: the capture's rim was the hue of a light in her hall");
            Assert.GreaterOrEqual(now,40,$"Hessa's rim is {now:0} degrees from a light in her hall: on its floor it is the colour around it");
            Object.Destroy(hessa.gameObject);
        }
        /// <summary>s10/159 to 176: after the Cantor the camera comes back in over the coil and for twenty seconds the frame was its bed's blue cells and two ships. Where the replay
        /// waits there (s10/165, 172, 176), what lies on the coil's bed now is in frame round the HUD's panels, and none of it can be flown into: no collider, nothing up to the
        /// flight plane. (The capture's state had none of it; the stills are the before and after.)</summary>
        [UnityTest] public IEnumerator AfterTheCantorTheCoilsBedHasSomethingOnIt()
        {
            NeedsGraphics();yield return Zone("Gullet_Tunnel","bossdown.Cantor");
            var bed=GameObject.Find("The coil's bed");Assert.IsNotNull(bed,"nothing is laid on the coil's bed");
            var parts=bed.GetComponentsInChildren<Renderer>();
            Assert.IsEmpty(bed.GetComponentsInChildren<Collider>(true),"something on the coil's bed has a collider: it is drawn only");
            Assert.Less(parts.Max(r=>r.bounds.max.y),-1,"something on the coil's bed comes up to the flight plane");
            var lead=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=lead);
            var shares=new List<string>();
            foreach(var (where,at) in new[]{("s10/165",new Vector3(17.8f,1,764.9f)),("s10/172",new Vector3(20.4f,1,764.2f)),("s10/176",new Vector3(15.1f,1,764.2f))})
            {
                Place(lead,at);Place(partner,at+new Vector3(-6,0,2));Frame(at,"Flight");yield return null;yield return null;
                float share=Share(parts);
                // Read as tools/look_density.py reads a still, for the record (the cells' veins are edges to it, so a bare bed need not read as plain).
                float plain=SunkenRelayLookTests.LargestPlain(Grab(1920,1080),1920,1080);bed.SetActive(false);yield return null;float plainThen=SunkenRelayLookTests.LargestPlain(Grab(1920,1080),1920,1080);bed.SetActive(true);yield return null;
                shares.Add($"{where} {share:0.000} plain {plainThen:0.00}->{plain:0.00}");
                Assert.GreaterOrEqual(share,.04f,$"after the Cantor, at {where}, what lies on the coil's bed is {share:P1} of the frame: the same blue cells and two ships");
            }
            TestContext.WriteLine("COIL_BED "+string.Join(", ",shares));
        }
    }
}
