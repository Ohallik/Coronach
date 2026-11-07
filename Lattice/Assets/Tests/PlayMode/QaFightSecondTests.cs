using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Batch 10, the second eye pass over the fights: a ghost on the hero, a camera that loses what the bar sends you at, markings that break on a wall's foot and read as planks,
    /// a friendly ring in the danger's colour, black bodies in the dark, people held like mannequins, a view that whips back. Each failure message says what the player sees.
    /// The checks that read real pixels need the -Graphics runner the full suite already uses. Their bars are estimates from captured colours: read the printed value before moving one.</summary>
    public sealed class QaFightSecondTests
    {
        const int ShotWidth=1920,ShotHeight=1080;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static void Flag(string key)=>GameServices.Current.Flags.SetBool(key,true);
        static IEnumerator Zone(string zone,bool clear=true)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+20;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            Camera.main.aspect=16f/9;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)){if(clear)Object.Destroy(enemy.gameObject);else enemy.Passive=true;}
            if(clear)foreach(var volume in Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None))volume.enabled=false;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.5f);
        }
        static CombatActor Hero=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=PartyController.Current.Active);
        static void Put(CombatActor hero,Vector3 at)
        {
            var body=hero.GetComponent<CharacterController>();body.enabled=false;
            if(hero.TryGetComponent<FlightMotor>(out var ship))ship.Halt();
            hero.transform.position=at;body.enabled=true;hero.target=null;hero.TargetLocked=false;Physics.SyncTransforms();
        }
        /// <summary>Open floor far from the arena's walls.</summary>
        static void Arena()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Fight fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static EnemyBrain Enemy(string id,Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;return enemy;}
        static void NoSpecials(EnemyBrain enemy)
        {if(enemy.TryGetComponent<BossController>(out var boss))typeof(BossController).GetField("nextSpecial",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(boss,Time.time+900);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        static Vector3 Floor(float x,float z,float level=0)=>CombatCover.Ground(new Vector3(x,level+4,z));
        static bool Inside(Vector3 view,float x0,float y0,float x1,float y1)=>view.z>0&&view.x>=x0&&view.x<=x1&&view.y>=y0&&view.y<=y1;
        static void Hide(Component who,bool hidden)
        {foreach(var body in who.GetComponentsInChildren<Renderer>())if(body is MeshRenderer||body is SkinnedMeshRenderer)body.forceRenderingOff=hidden;}
        static void NoShadows(Component who)
        {foreach(var body in who.GetComponentsInChildren<Renderer>())body.shadowCastingMode=ShadowCastingMode.Off;}
        static void NeedsGraphics()=>Assert.AreNotEqual(GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType,"this reads real pixels: run it with the -Graphics runner");
        static Color32[] Shot(Camera camera)
        {
            var image=new Texture2D(ShotWidth,ShotHeight,TextureFormat.RGB24,false);var rt=new RenderTexture(ShotWidth,ShotHeight,24,RenderTextureFormat.ARGB32);
            var active=RenderTexture.active;var target=camera.targetTexture;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,ShotWidth,ShotHeight),0,0);image.Apply();return image.GetPixels32();
            }
            finally{camera.targetTexture=target;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);}
        }
        static float Change(Color32 a,Color32 b)=>(Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b))/(3*255f);
        static float ChangeAt(Color32[] a,Color32[] b,Camera camera,Vector3 world)
        {
            var view=camera.WorldToViewportPoint(world);if(view.z<=0||view.x<0||view.x>=1||view.y<0||view.y>=1)return 0;
            int index=(int)(view.y*ShotHeight)*ShotWidth+(int)(view.x*ShotWidth);return Change(a[index],b[index]);
        }
        static float Percentile(List<float> values,float share){values.Sort();return values[Mathf.Clamp((int)(values.Count*share),0,values.Count-1)];}
        static float Light(Color32 c)=>(.3f*c.r+.59f*c.g+.11f*c.b)/255f;
        struct Patch{public float warmth,light,spread;public int pixels;}
        /// <summary>The pixels a body changes, read from the shot with it, or for the background from the shot without it.</summary>
        static Patch Read(Color32[] with,Color32[] without,bool background)
        {
            float warm=0,light=0;int count=0;var lights=new List<float>();
            for(int i=0;i<with.Length;i++)
            {
                if(Change(with[i],without[i])<.04f)continue;
                var c=background?without[i]:with[i];
                warm+=(c.r-c.b)/255f;light+=Light(c);count++;lights.Add(Light(c));
            }
            return new Patch{warmth=warm/Mathf.Max(1,count),light=light/Mathf.Max(1,count),pixels=count,spread=count>0?Percentile(lights,.9f)-Percentile(lights,.1f):0};
        }
        static float Fade(Renderer body){var block=new MaterialPropertyBlock();body.GetPropertyBlock(block);return block.GetFloat("_OccFade");}
        static Renderer BodyMesh(Component who)=>who.GetComponentsInChildren<Renderer>().First(r=>r is MeshRenderer or SkinnedMeshRenderer);
        static List<Vector3> Vertices(Component who)=>who.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToList();
        /// <summary>The spot nearest the body where the line from the lens to a hero's chest crosses its bounding box and misses its real surface by a body's breadth: what the old box test opened for.</summary>
        static bool SpotBeside(Bounds box,List<Vector3> points,Vector3 back,float distance,float chestAbove,float floorY,out Vector3 spot)
        {
            spot=default;float best=float.PositiveInfinity;bool found=false;
            for(float x=box.min.x-1;x<=box.max.x+1;x+=.5f)for(float z=box.min.z-1;z<=box.max.z+1;z+=.5f)
            {
                var chest=new Vector3(x,floorY+chestAbove,z);
                if(!box.IntersectRay(new Ray(chest+back*distance,-back),out float entry)||entry>=distance-.15f)continue;
                float clearance=float.PositiveInfinity;
                foreach(var point in points)
                {
                    var off=point-chest;float along=Mathf.Clamp(Vector3.Dot(off,back),.15f,distance);
                    clearance=Mathf.Min(clearance,(off-back*along).sqrMagnitude);
                }
                clearance=Mathf.Sqrt(clearance);
                if(clearance>=.6f&&clearance<best){best=clearance;spot=new Vector3(x,floorY,z);found=true;}
            }
            return found;
        }

        // ------------------------------------------------------------------ 1. no ghost, and only what really hides him
        [UnityTest] public IEnumerator TheHeroWearsNoGhostBehindABoss()
        {
            yield return Zone("Arena_Ground");
            var hero=Hero;var camera=Camera.main;
            var usual=hero.GetComponentsInChildren<Renderer>(true).ToDictionary(r=>r,r=>r.sharedMaterials.Length);
            var toward=Flat(camera.transform.position-hero.transform.position).normalized;
            var boss=Enemy("Burrower",hero.transform.position+toward*2.3f);yield return new WaitForSecondsRealtime(.5f);
            Assert.Greater(Fade(BodyMesh(boss.GetComponent<DefeatPresentation>().visual)),.9f,"fixture: the boss is opening for the hero");
            foreach(var (body,count) in usual)Assert.AreEqual(count,body.sharedMaterials.Length,"the hero behind a boss wears an extra material: a flat cyan ghost on his upper body, breaking at the boss's edge");
            Assert.IsNull(Resources.Load<Shader>("Effects/HeroSilhouette"),"the ghost's shader is still there: its ZTest Greater passes where his own limbs overlap, so he tints himself");
        }
        [UnityTest] public IEnumerator TheDoorOpensOnlyForAHeroTheDrillReallyHides()
        {
            yield return Zone("Arena_Ground");Arena();
            var hero=Hero;Put(hero,new Vector3(200,.1f,0));Put(Partner,new Vector3(215,.1f,0));
            var boss=Enemy("Burrower",new Vector3(200,.1f,0));boss.transform.rotation=Quaternion.Euler(0,45,0);boss.GetComponent<CharacterController>().enabled=false;
            yield return new WaitForSecondsRealtime(1.5f);
            var camera=Camera.main;var back=-camera.transform.forward;var visual=boss.GetComponent<DefeatPresentation>().visual;var body=BodyMesh(visual);
            float distance=Vector3.Distance(camera.transform.position,hero.transform.position+Vector3.up*.95f);float floor=hero.transform.position.y;
            var points=Vertices(visual);
            Assert.IsTrue(SpotBeside(body.bounds,points,back,distance,.95f,floor,out var beside),"fixture: a corner of the box round a drill lying diagonally, with nothing in it");
            Put(hero,beside);yield return new WaitForSecondsRealtime(1.5f);
            Assert.Less(Fade(body),.01f,$"a hero standing beside the drill at {beside} opens a patch of dots in it: the box round a diagonal body is far bigger than the body");
            // Behind its real middle for the camera.
            var middle=new Vector3(points.Average(p=>p.x),floor+(points.Max(p=>p.y)-floor)*.6f,points.Average(p=>p.z));
            var behind=middle-back*((middle.y-(floor+.95f))/back.y);behind.y=floor;
            Put(hero,behind);yield return new WaitForSecondsRealtime(1.5f);
            Assert.Greater(Fade(body),.9f,$"a hero the drill really hides at {behind} gets nothing opened for him");
        }
        [UnityTest] public IEnumerator TheWreckLeavesAHeroBesideItAlone()
        {
            foreach(var flag in new[]{"met.Survivor","bossdown.Burrower","clear.Sorrel_Burrower"})Flag(flag);
            yield return Zone("Sorrel_Ridges");
            var site=Object.FindFirstObjectByType<BossWreck>();Assert.IsTrue(site.wreck.activeInHierarchy,"fixture: a later visit finds the wreck");
            var hero=Hero;var camera=Camera.main;var body=BodyMesh(site.rig);var box=body.bounds;float floor=Floor(box.center.x,box.center.z,box.min.y).y;
            Put(hero,Floor(box.center.x,box.min.z-3,floor));Put(Partner,Floor(box.center.x+3,box.min.z-3,floor));yield return new WaitForSecondsRealtime(1.5f);
            float distance=Vector3.Distance(camera.transform.position,hero.transform.position+Vector3.up*.95f);
            Assert.IsTrue(SpotBeside(box,Vertices(site.rig),-camera.transform.forward,distance,.95f,floor,out var beside),"fixture: beside the wreck, inside its box");
            Put(hero,Floor(beside.x,beside.z,floor));yield return new WaitForSecondsRealtime(1.5f);
            Assert.Less(Fade(body),.01f,$"dots open in the dead wreck for a hero standing clear of it at {beside}");
        }
        [UnityTest] public IEnumerator TheHeroShowsThroughTheDoor()
        {
            NeedsGraphics();yield return Zone("Arena_Ground");Arena();
            var hero=Hero;Put(hero,new Vector3(200,.1f,0));Put(Partner,new Vector3(215,.1f,0));NoShadows(hero);yield return new WaitForSecondsRealtime(1.5f);
            var camera=Camera.main;var toward=Flat(camera.transform.position-hero.transform.position).normalized;
            var bare=Shot(camera);Hide(hero,true);var none=Shot(camera);Hide(hero,false);
            var boss=Enemy("Burrower",hero.transform.position+toward*2.3f);boss.GetComponent<CharacterController>().enabled=false;yield return new WaitForSecondsRealtime(.5f);NoShadows(boss);yield return null;
            var through=Shot(camera);Hide(hero,true);var hidden=Shot(camera);Hide(hero,false);
            int whole=0,seen=0;
            for(int i=0;i<bare.Length;i++){if(Change(bare[i],none[i])<.04f)continue;whole++;if(Change(through[i],hidden[i])>=.02f)seen++;}
            float share=seen/(float)Mathf.Max(1,whole);
            TestContext.WriteLine($"SEE_THROUGH share={share:0.00} whole={whole}");
            Assert.Greater(whole,500,"fixture: the hero is on screen");
            Assert.GreaterOrEqual(share,.4f,$"behind the boss only {share:P0} of the hero shows: with no ghost on him the door itself has to show him");
        }
        [UnityTest] public IEnumerator HessasHurtboxStandsForHerRiggedBody()
        {
            yield return Zone("Arena_Ground");Arena();
            var hero=Hero;Put(hero,new Vector3(200,.1f,0));Put(Partner,new Vector3(215,.1f,0));yield return new WaitForSecondsRealtime(1.5f);
            var camera=Camera.main;var toward=Flat(camera.transform.position-hero.transform.position).normalized;var aside=Flat(camera.transform.right).normalized;
            var boss=Enemy("HessaKorl",hero.transform.position+toward*1.5f);var body=BodyMesh(boss.GetComponent<DefeatPresentation>().visual);
            boss.GetComponent<CharacterController>().enabled=false;
            yield return new WaitForSecondsRealtime(.5f);
            Assert.Greater(Fade(body),.9f,"a hero behind a captain half as tall again gets nothing opened");
            boss.transform.position=hero.transform.position+toward*1.5f+aside*2.6f;Physics.SyncTransforms();yield return new WaitForSecondsRealtime(.5f);
            Assert.Less(Fade(body),.01f,"a hero standing a body's width clear of her still gets a door in her");
        }

        // ------------------------------------------------------------------ 2. the camera: hero and target, then the boss; a rim moves it only if it must
        static PressureOrgan Organ(string name)=>Object.FindObjectsByType<PressureOrgan>(FindObjectsSortMode.None).Single(o=>o.name.Contains(name));
        [UnityTest] public IEnumerator TheOrganTheBarSendsYouAtStaysInFrame()
        {
            yield return Zone("Hushwell",false);
            var encounter=Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(e=>e.encounterId=="Hushwell_Bellows");
            encounter.Begin();yield return null;
            var boss=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Single(e=>e.definition.id=="BellowsBelow");
            boss.Passive=false;yield return Frames(2);NoSpecials(boss);boss.GetComponent<CharacterController>().enabled=false;
            var hero=Hero;var partner=Partner;float level=HushwellLayout.Middle;var west=Organ("west");
            // Numbers from the real chamber: at the old framing the organ is off the left edge in both (-.04 and -.64 of the screen, with the boss held in view).
            foreach(var (heroAt,bossAt) in new[]{(new Vector2(-9,262),new Vector2(18,256)),(new Vector2(10,258),new Vector2(0,268))})
            {
                Put(hero,Floor(heroAt.x,heroAt.y,level));Put(partner,Floor(heroAt.x+1.5f,heroAt.y,level));
                boss.transform.position=Floor(bossAt.x,bossAt.y,level);
                hero.target=west.Health;hero.TargetLocked=true;
                yield return new WaitForSecondsRealtime(4);
                Assert.IsFalse(DialogueSystem.Current.Running,"fixture: nothing else is on the screen");
                Assert.IsNotNull(BossController.Focus(hero.transform.position),"fixture: the boss bar is up");
                var camera=Camera.main;var organ=camera.WorldToViewportPoint(west.transform.position+Vector3.up*1.5f);
                Assert.IsTrue(Inside(organ,.01f,.01f,.99f,.99f),$"the plate says LOCKED, PRESSURE ORGAN and its health is falling, and the organ is off screen ({organ}) with the hero at {heroAt}");
                foreach(var point in new[]{hero.transform.position,hero.transform.position+Vector3.up*1.9f})
                {var view=camera.WorldToViewportPoint(point);Assert.IsTrue(Inside(view,.08f,.18f,.92f,.92f),$"keeping the organ in frame, the hero went under the HUD or out of frame ({view})");}
            }
        }
        [UnityTest] public IEnumerator AWallBesideTheHeroDoesNotDiveTheCameraInAFight()
        {
            Flag("met.Survivor");
            yield return Zone("Sorrel_Ridges",false);
            Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(e=>e.encounterId=="Sorrel_Burrower").Begin();yield return null;
            var boss=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Single(e=>e.definition.id=="Burrower");
            boss.Passive=false;yield return Frames(2);NoSpecials(boss);boss.GetComponent<CharacterController>().enabled=false;
            var hero=Hero;var rig=Object.FindFirstObjectByType<CameraRig>();
            Put(hero,Floor(-8,168));Put(Partner,Floor(-6.5f,168));boss.transform.position=Floor(8,168);
            yield return new WaitForSecondsRealtime(4);
            float before=rig.GroundCameraDistance;Assert.GreaterOrEqual(before,rig.profile.distance-.5f,"fixture: the framed distance");
            // A wall on the camera's side with its top a hand under the line to his chest: a wide sphere down the view grazes it, and it hides nobody.
            var camera=Camera.main;var chest=hero.transform.position+Vector3.up*1.2f;var line=(camera.transform.position-chest).normalized;var flat=Flat(line);float run=flat.magnitude;flat/=run;
            float floor=hero.transform.position.y,top=chest.y+1.35f/run*line.y-.2f;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Fixture wall";wall.transform.rotation=Quaternion.LookRotation(flat);
            var near=chest+flat*1.6f;wall.transform.position=new Vector3(near.x,(top+floor)*.5f,near.z);wall.transform.localScale=new Vector3(5,top-floor,.5f);Physics.SyncTransforms();
            Assert.IsTrue(Physics.SphereCastAll(chest,.4f,-camera.transform.forward,before,~0,QueryTriggerInteraction.Ignore).Any(h=>h.collider.gameObject==wall),"fixture: the sphere the rim test uses touches the wall");
            Assert.IsFalse(Physics.Linecast(camera.transform.position,chest,out var seen,~0,QueryTriggerInteraction.Ignore)&&seen.collider.gameObject==wall,"fixture: the wall hides nobody");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.GreaterOrEqual(rig.GroundCameraDistance,before-1,$"beside a wall that hides nobody the camera drops in to {rig.GroundCameraDistance:0.0} m in the middle of a boss fight, with the hero huge at the edge of the screen and the boss out of it");
            // Tall enough to hide all of him from anywhere behind it, 12 m off along the line: it comes in as far as that takes, and no further. A wall that leaves his head in sight doesn't count as hiding him.
            var far=chest+line*12;wall.transform.position=new Vector3(far.x,(far.y+4+floor)*.5f,far.z);wall.transform.localScale=new Vector3(9,far.y+4-floor,.5f);Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(1.5f);
            var eye=camera.transform.position;
            Assert.IsFalse(Physics.RaycastAll(eye,(chest-eye).normalized,Vector3.Distance(eye,chest)-.3f,~0,QueryTriggerInteraction.Ignore).Any(h=>h.collider.gameObject==wall),"a wall that really hides the hero stays between him and the lens");
            Assert.Greater(rig.GroundCameraDistance,CameraRig.NearestGroundDistance+1,$"a wall 12 m off along the line pulls the lens all the way in to {rig.GroundCameraDistance:0.0} m");
        }

        // ------------------------------------------------------------------ 3. markings on real ground
        static float RimJump(Telegraph mark)
        {
            var mesh=mark.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var uv=mesh.uv;
            var rim=Enumerable.Range(0,vertices.Length).Where(i=>uv[i].x>.999f).OrderBy(i=>uv[i].y).Select(i=>vertices[i]).ToList();
            float jump=0;for(int i=1;i<rim.Count;i++)jump=Mathf.Max(jump,Mathf.Abs(rim[i].y-rim[i-1].y));
            return jump;
        }
        /// <summary>A whole circle's first and last columns are one line on the ground: how far apart they stand, row by row.</summary>
        static float SeamGap(Telegraph mark)
        {
            var mesh=mark.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var uv=mesh.uv;float gap=0;
            for(int i=0;i<vertices.Length;i++)if(uv[i].y<.001f)for(int j=0;j<vertices.Length;j++)
                if(uv[j].y>.999f&&Mathf.Abs(uv[j].x-uv[i].x)<.001f)gap=Mathf.Max(gap,Mathf.Abs(vertices[j].y-vertices[i].y));
            return gap;
        }
        [UnityTest] public IEnumerator AMarkingBesideAWallHasNoSpikesOrNotches()
        {
            yield return Zone("Sorrel_Ridges");
            // Sorrel's arena: flat floor to a wall that climbs 10 m in two, with a low foot. Spikes rose 2 m wherever a sample caught that foot beside one that missed it.
            var fan=Telegraph.Cone(Floor(10,172),new Vector3(1,0,.35f),10,230,5,Telegraph.Heavy);
            var lane=Telegraph.Line(Floor(4,165),new Vector3(1,0,.2f),20,3.4f,5,Telegraph.Heavy);
            float jump=RimJump(fan);
            float top=fan.GetComponent<MeshFilter>().sharedMesh.vertices.Max(v=>v.y),laneTop=lane.GetComponent<MeshFilter>().sharedMesh.vertices.Max(v=>v.y);
            float floor=Floor(10,172).y;
            TestContext.WriteLine($"MARKING_WALL jump={jump:0.00} top={top-floor:0.00} laneTop={laneTop-floor:0.00}");
            Assert.LessOrEqual(jump,.9f,$"the fan's rim jumps {jump:0.0} m between two neighbours at the wall's foot: long pale spikes with bare ground between them, and a notch in the rim");
            Assert.LessOrEqual(laneTop-floor,.8f,$"a lane climbs {laneTop-floor:0.0} m up the wall's foot and folds into a bent ribbon");
            // Past the wall it goes on flat under the hill, and its rim doesn't show through from there.
            var mesh=lane.GetComponent<MeshFilter>().sharedMesh;var flags=new List<Vector3>();mesh.GetUVs(1,flags);var vertices=mesh.vertices;
            var under=Enumerable.Range(0,vertices.Length).Where(i=>vertices[i].x>19).ToList();
            Assert.Greater(under.Count,4,"fixture: the lane runs past the wall");
            Assert.IsTrue(under.All(i=>flags[i].z<.01f),"a marking's rim still floats on the rock where it has gone on under the hill: a thin line beyond the foot of the slope");
            Assert.IsTrue(Enumerable.Range(0,vertices.Length).Where(i=>vertices[i].x<12).All(i=>flags[i].z>.99f),"the part of the lane on the floor has lost its outline through props");
            // A whole ring's seam lies due south of it, the camera's side. By the wall's foot it stayed whole only if its two ends settle together.
            float seam=SeamGap(Telegraph.Circle(Floor(16,168),7,5,Telegraph.Heavy));
            TestContext.WriteLine($"MARKING_SEAM gap={seam:0.00}");
            Assert.LessOrEqual(seam,.02f,$"a ring by the wall's foot tears open {seam:0.00} m at its seam: two rims standing apart where the circle should close");
        }
        [UnityTest] public IEnumerator AMarkingFollowsAWalkableSlopeAllTheWayButNotACrate()
        {
            yield return Zone("Arena_Ground");Arena();yield return null;
            var ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);ramp.transform.SetPositionAndRotation(new Vector3(200,12,0),Quaternion.Euler(-25,0,0));ramp.transform.localScale=new Vector3(40,1,60);Physics.SyncTransforms();
            Assert.IsTrue(Physics.Raycast(new Vector3(200,40,0),Vector3.down,out var ground,60),"fixture: the ramp");
            float Slope(float x,float z)=>Physics.Raycast(new Vector3(x,60,z),Vector3.down,out var hit,90)?hit.point.y:float.NaN;
            var mark=Telegraph.Circle(ground.point,8,5,Telegraph.Heavy);var vertices=mark.GetComponent<MeshFilter>().sharedMesh.vertices;
            float span=vertices.Max(v=>v.y)-vertices.Min(v=>v.y);
            Assert.Greater(span,6,$"a ring on a 25 degree slope spans {span:0.0} m: past a few metres above its middle it misses the surface and lies flat under the hill");
            foreach(var v in vertices)Assert.AreEqual(Slope(v.x,v.z)+.06f,v.y,.2f,$"a vertex of a ring on a slope is off the ground at ({v.x:0.0},{v.z:0.0})");
            Object.Destroy(mark.gameObject);yield return null;
            // A crate on the same slope: what it stands on counts, not its top.
            var crate=GameObject.CreatePrimitive(PrimitiveType.Cube);var spot=ground.point+new Vector3(3,0,0);crate.transform.position=spot+Vector3.up*.6f;crate.transform.localScale=new Vector3(2,2,2);Physics.SyncTransforms();
            mark=Telegraph.Circle(ground.point,8,5,Telegraph.Heavy);vertices=mark.GetComponent<MeshFilter>().sharedMesh.vertices;
            float climbed=0;crate.SetActive(false);Physics.SyncTransforms();
            foreach(var v in vertices)climbed=Mathf.Max(climbed,v.y-(Slope(v.x,v.z)+.06f));
            Assert.Less(climbed,.3f,$"a marking on a slope climbs {climbed:0.0} m onto a crate standing on it: the terrain counts, a crate doesn't");
        }

        // ------------------------------------------------------------------ 4. the hatch, a spent marking, and what the outline crosses
        /// <summary>A marking hands its renderer the block in its first Update, so read this a frame after making it.</summary>
        static Vector4 HatchOf(Telegraph mark)
        {
            var block=new MaterialPropertyBlock();var view=mark.GetComponent<MeshRenderer>();view.GetPropertyBlock(block);
            Assert.IsTrue(block.HasVector("_Hatch"),"a marking's hatch is the same on every shape and size: planks across a huge ring, rungs across a lane");
            return block.GetVector("_Hatch");
        }
        [UnityTest] public IEnumerator AHatchSlantsAcrossALaneAndOnTheScreenAndThinsWithTheArea()
        {
            yield return Zone("Arena_Ground");Arena();
            Put(Hero,new Vector3(200,.1f,0));Put(Partner,new Vector3(215,.1f,0));yield return new WaitForSecondsRealtime(1.5f);
            var at=new Vector3(200,0,0);var camera=Camera.main;
            foreach(var along in new[]{Vector3.forward,Vector3.right,new Vector3(1,0,1).normalized,new Vector3(-1,0,2).normalized})
            {
                var lane=Telegraph.Line(at,along,14,2.2f,5,Telegraph.Threat);yield return null;var hatch=HatchOf(lane);
                float slant=Vector3.Angle(along,new Vector3(hatch.y,0,-hatch.x));slant=Mathf.Min(slant,180-slant);
                Assert.That(slant,Is.InRange(30f,60f),$"on a lane running {along} the hatch lines lie {slant:0} degrees off it: square across, they're the rungs of a ladder");
                Object.Destroy(lane.gameObject);yield return null;
            }
            var small=Telegraph.Circle(at,4,5,Telegraph.Threat);var huge=Telegraph.Circle(at,13,5,Telegraph.Threat);yield return null;
            Vector4 a=HatchOf(small),b=HatchOf(huge);
            Assert.AreEqual(.62f,a.z,.01f,"a small marking keeps its fine hatch: it's what says marked rather than painted");
            Assert.Greater(b.z,a.z*2,"a ring the size of the chamber has its lines as close as a small one's: a whole floor of even stripes");
            Assert.Less(b.w,a.w*.6f,"a ring the size of the chamber has its lines as strong as a small one's: plank flooring");
            // Corner to corner on the fixed camera's screen: lines at 45 degrees in the world lay almost flat there.
            var d=new Vector3(b.y,0,-b.x);var p=camera.WorldToViewportPoint(at);var q=camera.WorldToViewportPoint(at+d);
            float onScreen=Mathf.Abs(Mathf.Atan2(q.y-p.y,(q.x-p.x)*camera.aspect)*Mathf.Rad2Deg);onScreen=Mathf.Min(onScreen,180-onScreen);
            Assert.That(onScreen,Is.InRange(30f,60f),$"a ring's hatch lies {onScreen:0} degrees off level on screen: it reads as planks");
        }
        [UnityTest] public IEnumerator AHugeRingsEdgeOutweighsItsHatch()
        {
            NeedsGraphics();yield return Zone("Hushwell");
            var spot=new Vector2(-4,255);float level=HushwellLayout.Middle;
            Put(Hero,Floor(spot.x,spot.y,level));Put(Partner,Floor(spot.x-3,spot.y,level));yield return new WaitForSecondsRealtime(1.5f);
            var camera=Camera.main;var center=Floor(spot.x,spot.y,level);const float Radius=13;
            var mark=Telegraph.Circle(center,Radius,.01f,Telegraph.Threat);yield return new WaitForSecondsRealtime(.25f);
            var view=mark.GetComponent<MeshRenderer>();var marked=Shot(camera);view.enabled=false;var bare=Shot(camera);view.enabled=true;
            var fill=new List<float>();
            for(float x=-5;x<=5;x+=.09f)for(float z=-3;z<=3;z+=.09f)fill.Add(ChangeAt(marked,bare,camera,Floor(center.x+x,center.z+z,level)));
            float hatch=Percentile(fill,.92f),wash=Percentile(fill,.5f);
            Object.Destroy(mark.gameObject);
            // The rim's own strength on this floor, from a small marking of the same colour: it's off screen on a ring this size.
            var small=Telegraph.Circle(center,3,.01f,Telegraph.Threat);yield return new WaitForSecondsRealtime(.25f);
            var ringed=Shot(camera);var thin=small.GetComponent<MeshRenderer>();thin.enabled=false;var clean=Shot(camera);thin.enabled=true;
            var rim=new List<float>();
            for(float a=0;a<360;a+=5){var o=Quaternion.Euler(0,a,0)*Vector3.forward;float best=0;foreach(float r in new[]{.03f,.06f,.09f,.12f})best=Mathf.Max(best,ChangeAt(ringed,clean,camera,Floor(center.x+o.x*(3-r),center.z+o.z*(3-r),level)));rim.Add(best);}
            float edge=Percentile(rim,.5f);
            TestContext.WriteLine($"HUGE_RING wash={wash:0.000} hatch={hatch:0.000} rim={edge:0.000}");
            Assert.GreaterOrEqual(edge,hatch*2.5f,$"inside a ring the size of the chamber the hatch shifts the floor by {hatch:0.00} against the rim's {edge:0.00}: a floor of stripes with a scrap of rim at its edge");
        }
        [UnityTest] public IEnumerator ASpentMarkingClearsInRealTimeThroughAFlash()
        {
            yield return Zone("Arena_Ground");Arena();yield return null;
            var mark=Telegraph.Circle(new Vector3(200,0,0),13,.01f,Telegraph.Threat);yield return new WaitForSecondsRealtime(.1f);
            GameTime.BeginFlash();mark.Release();
            yield return new WaitForSecondsRealtime(.5f);
            bool left=mark!=null;GameTime.EndFlash();
            Assert.IsFalse(left,"through a Flash a spent ring stays on the floor over a second, filled, with the bar already saying OPENING  ·  STRIKE NOW");
        }
        [UnityTest] public IEnumerator AStrikesFlashIsNotAWhiteDisc()
        {
            NeedsGraphics();yield return Zone("Hushwell");
            var spot=new Vector2(-4,255);float level=HushwellLayout.Middle;
            Put(Hero,Floor(spot.x,spot.y,level));Put(Partner,Floor(spot.x-3,spot.y,level));yield return new WaitForSecondsRealtime(1.5f);
            var camera=Camera.main;var right=Flat(camera.transform.right).normalized;var center=Floor(spot.x+right.x*5.2f,spot.y+right.z*5.2f,level);
            var mark=Telegraph.Circle(center,2.5f,.01f,Telegraph.Threat,true);yield return new WaitForSecondsRealtime(.2f);
            var view=mark.GetComponent<MeshRenderer>();
            // At the height of its flash, however this frame's clock fell.
            mark.enabled=false;var block=new MaterialPropertyBlock();view.GetPropertyBlock(block);block.SetFloat("_Flash",1);block.SetFloat("_Fade",1);view.SetPropertyBlock(block);
            var flashing=Shot(camera);view.enabled=false;var bare=Shot(camera);view.enabled=true;
            var fill=new List<float>();
            for(float x=-1.2f;x<=1.2f;x+=.09f)for(float z=-1.2f;z<=1.2f;z+=.09f)fill.Add(ChangeAt(flashing,bare,camera,Floor(center.x+x,center.z+z,level)));
            float wash=Percentile(fill,.5f);
            TestContext.WriteLine($"FLASH wash={wash:0.000}");
            Assert.LessOrEqual(wash,.42f,$"at a strike the whole marking goes {wash:0.00} toward white: a full pale disc, the brightest thing on screen, with the Scrapmites standing black on it");
            Object.Destroy(mark.gameObject);
        }
        [UnityTest] public IEnumerator AMarkingsOutlineStaysOffABossBody()
        {
            NeedsGraphics();yield return Zone("Arena_Ground");Arena();
            var hero=Hero;Put(hero,new Vector3(190,.1f,-8));Put(Partner,new Vector3(188,.1f,-8));yield return new WaitForSecondsRealtime(1.5f);
            var boss=Enemy("BellowsBelow",new Vector3(192,.1f,-2));boss.GetComponent<CharacterController>().enabled=false;yield return new WaitForSecondsRealtime(.5f);NoShadows(boss);yield return null;
            var camera=Camera.main;var center=Flat(boss.transform.position);var right=Flat(camera.transform.right).normalized;var far=Flat(camera.transform.forward).normalized;
            // A beam across the dome's middle, its far rim behind the body from where the camera stands.
            var lane=Telegraph.Line(center-right*8,right,16,2.4f,.01f,Telegraph.Threat);yield return new WaitForSecondsRealtime(.25f);
            var view=lane.GetComponent<MeshRenderer>();var marked=Shot(camera);view.enabled=false;var clean=Shot(camera);view.enabled=true;
            Hide(boss,true);var noBoss=Shot(camera);Hide(boss,false);
            float strongest=0;int covered=0;
            for(float s=-1.6f;s<=1.6f;s+=.1f)for(float off=1.08f;off<=1.2f;off+=.02f)
            {
                var point=center+right*s+far*off+Vector3.up*.06f;
                if(ChangeAt(clean,noBoss,camera,point)<.04f)continue;covered++;
                strongest=Mathf.Max(strongest,ChangeAt(marked,clean,camera,point));
            }
            TestContext.WriteLine($"OUTLINE_ON_BODY strongest={strongest:0.000} covered={covered}");
            Assert.Greater(covered,10,"fixture: the body stands over the marking's far rim");
            Assert.Less(strongest,.03f,$"a marking's rim draws across the boss in front of it ({strongest:0.00}): a bright stick stuck in its side");
        }

        // ------------------------------------------------------------------ 5. orange, red and purple on the floor mean danger
        static bool ReadsAsDanger(Color c)
        {
            Color.RGBToHSV(c,out float hue,out float saturation,out _);hue*=360;
            return saturation>=.4f&&(hue<=50||hue>=250&&hue<=345);
        }
        [UnityTest] public IEnumerator TheHeroesRingsAreNotTheColourOfDanger()
        {
            yield return Zone("Arena_Ground");Arena();
            var taren=PartyController.Current.members.First(m=>m.character=="Taren");var sela=PartyController.Current.members.First(m=>m.character=="Sela");
            Put(taren,new Vector3(200,.1f,0));Put(sela,new Vector3(212,.1f,0));yield return new WaitForSecondsRealtime(.5f);
            var seen=new Dictionary<string,Color>();
            void Look(string what)
            {
                var block=new MaterialPropertyBlock();
                foreach(var ring in Object.FindObjectsByType<GroundRing>(FindObjectsSortMode.None))
                {
                    ring.GetComponent<MeshRenderer>().GetPropertyBlock(block);var c=block.GetColor("_BaseColor");
                    if(c.a>.01f)seen[what]=c;
                }
            }
            IEnumerator Watch(string what,float seconds){for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){yield return null;Look(what);}}
            PulseWave.Release(taren.transform.position,taren.Packet(1,DamageType.Pulse,0));yield return Watch("Pulse Burst",.3f);
            taren.GetComponent<HeroSkills>().Ground(SkillKind.AnchorSlam,1,Vector3.forward);yield return Watch("Anchor Slam",1);
            taren.GetComponent<HeroSkills>().Ground(SkillKind.ForgeSpiral,1,Vector3.forward);yield return Watch("Forge Spiral",1);
            taren.GetComponent<HeroSkills>().Ground(SkillKind.Faultline,1,Vector3.forward);yield return Watch("Faultline",1);
            sela.GetComponent<HeroSkills>().Ground(SkillKind.GravityFold,1,Vector3.forward);yield return Watch("Gravity Fold",3.5f);
            sela.GetComponent<HeroSkills>().Ground(SkillKind.WaypointBlink,1,Vector3.forward);yield return Watch("Waypoint Blink",1.2f);
            foreach(var what in new[]{"Pulse Burst","Anchor Slam","Forge Spiral","Faultline","Gravity Fold","Waypoint Blink"})
            {
                Assert.IsTrue(seen.ContainsKey(what),"fixture: "+what+" drew a ring");
                Assert.IsFalse(ReadsAsDanger(seen[what]),$"{what} draws its ring in {seen[what]}, in with the danger's own colours: now that a marking is mostly its rim, the hero's own skill looks like a warning");
            }
            foreach(var danger in new[]{Telegraph.Threat,Telegraph.Heavy,Telegraph.Stagger})Assert.IsTrue(ReadsAsDanger(danger),"fixture: the test knows the danger's colours");
        }

        // ------------------------------------------------------------------ 6. bodies in the dark
        /// <summary>The lower vault between its lamps, where the Sergeant stood black at the top of the frame.</summary>
        static IEnumerator ReadInTheRelay(string id,float light,float warmth)
        {
            yield return null;
            var hero=Hero;Put(hero,Floor(-5,98));Put(Partner,Floor(-7,98));NoShadows(hero);
            yield return new WaitForSecondsRealtime(1.5f);
            var camera=Camera.main;var enemy=Enemy(id,Floor(1,98));yield return new WaitForSecondsRealtime(.6f);NoShadows(enemy);yield return null;
            var with=Shot(camera);Hide(enemy,true);var without=Shot(camera);Hide(enemy,false);
            var body=Read(with,without,false);var floor=Read(with,without,true);
            TestContext.WriteLine($"RELAY_READ {id} light={body.light:0.00} floor={floor.light:0.00} warm={body.warmth:0.00} floorwarm={floor.warmth:0.00} spread={body.spread:0.00} px={body.pixels}");
            Assert.Greater(body.pixels,300,$"fixture: the {id} is on screen");
            Assert.GreaterOrEqual(body.light,floor.light+light,$"a {id} in the relay is {body.light:0.00} against the floor's {floor.light:0.00}: a near-black cut-out with no wind-up or pose on its body");
            Assert.GreaterOrEqual(body.warmth,floor.warmth+warmth,$"a {id} in the relay has no warmth on it ({body.warmth:0.00} against {floor.warmth:0.00}): nothing on the body says hostile");
            Object.Destroy(enemy.gameObject);yield return Frames(2);
        }
        [UnityTest] public IEnumerator TheMusterReadInTheDarkOfTheRelay()
        {
            NeedsGraphics();Flag("relay.hessaConfronted");yield return Zone("SunkenRelay");
            foreach(var id in new[]{"MusterScav","MusterSergeant","HessaKorl"})yield return ReadInTheRelay(id,0,.08f);
        }
        [UnityTest] public IEnumerator EveryBodyInTheDarkKeepsARimInShade()
        {
            yield return Zone("Arena_Ground");
            foreach(var id in new[]{"MusterScav","MusterSergeant","HessaKorl"})
            {
                var enemy=Enemy(id,new Vector3(0,-200,0));yield return null;var material=BodyMesh(enemy).sharedMaterial;
                var rim=material.GetColor("_RimColor");
                Assert.Greater(rim.r,rim.b*2,$"{id}'s rim is the same cold grey as everything else's: nothing on its body says hostile from above");
                Assert.GreaterOrEqual(material.GetFloat("_RimUnlit"),.5f,$"{id}'s rim only shows on its lit side, and in the relay almost nothing is");
                Assert.Greater(material.GetFloat("_LightFloor"),.3f,$"{id} takes no more light than the relay gives: .3 at most");
                Object.Destroy(enemy.gameObject);yield return null;
            }
        }
        [UnityTest] public IEnumerator TheBurrowerHoldsItsShapeInShade()
        {
            NeedsGraphics();Flag("met.Survivor");yield return Zone("Sorrel_Ridges");
            var hero=Hero;Put(hero,Floor(-10,168));Put(Partner,Floor(-13,168));NoShadows(hero);yield return new WaitForSecondsRealtime(1.5f);
            var camera=Camera.main;var enemy=Enemy("Burrower",Floor(-3,168));yield return new WaitForSecondsRealtime(.5f);NoShadows(enemy);yield return null;
            // A cast shadow over it all, as the wall throws one: a sheet between the sun and the body, seen by nobody.
            var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);
            var sheet=GameObject.CreatePrimitive(PrimitiveType.Quad);Object.Destroy(sheet.GetComponent<Collider>());sheet.name="Fixture shade";
            sheet.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.ShadowsOnly;sheet.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            sheet.transform.position=enemy.transform.position-sun.transform.forward*14;sheet.transform.rotation=Quaternion.LookRotation(sun.transform.forward);sheet.transform.localScale=Vector3.one*16;
            yield return Frames(2);
            var with=Shot(camera);Hide(enemy,true);var without=Shot(camera);Hide(enemy,false);
            var body=Read(with,without,false);var floor=Read(with,without,true);
            TestContext.WriteLine($"BURROWER_SHADE light={body.light:0.00} spread={body.spread:0.00} floor={floor.light:0.00} px={body.pixels}");
            Assert.Greater(body.pixels,2000,"fixture: the rig is on screen");
            Assert.GreaterOrEqual(body.light,.31f,$"in a cast shadow the Burrower goes flat dark slate ({body.light:0.00}, as it was at .27): more a hole than a machine");
            Assert.GreaterOrEqual(body.spread,.12f,$"in a cast shadow its segments and maw are gone: the whole body is within {body.spread:0.00} of one value");
        }
        [UnityTest] public IEnumerator AMusterSkiffKeepsItsHullLines()
        {
            NeedsGraphics();yield return Zone("WickReach");
            var hero=Hero;Put(Partner,hero.transform.position+Vector3.back*9+Vector3.right*5);NoShadows(hero);yield return new WaitForSecondsRealtime(1);
            var camera=Camera.main;var enemy=Enemy("MusterSkiff",hero.transform.position+Vector3.forward*6);yield return new WaitForSecondsRealtime(.6f);NoShadows(enemy);yield return null;
            var with=Shot(camera);Hide(enemy,true);var without=Shot(camera);Hide(enemy,false);
            var body=Read(with,without,false);
            TestContext.WriteLine($"SKIFF_READ light={body.light:0.00} spread={body.spread:0.00} px={body.pixels}");
            Assert.Greater(body.pixels,150,"fixture: the skiff is on screen");
            Assert.AreEqual(1,BodyMesh(enemy).sharedMaterial.GetFloat("_BaseMapStrength"),.01f,"the skiff's dark, even map is lifted to almost nothing: flat tan clay with no hull lines or trim");
            Assert.GreaterOrEqual(body.spread,.12f,$"a Muster Skiff is within {body.spread:0.00} of one value: soft clay claws, beside Darts and Shellmines with crisp detail");
        }

        // ------------------------------------------------------------------ 7. people at rest
        static float ArmOffDown(Animator rig,HumanBodyBones upper,HumanBodyBones lower)=>Vector3.Angle(rig.GetBoneTransform(lower).position-rig.GetBoneTransform(upper).position,Vector3.down);
        static void ArmsHang(Animator rig,string who)
        {
            foreach(var (upper,lower,side) in new[]{(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,"left"),(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,"right")})
            {
                float off=ArmOffDown(rig,upper,lower);
                Assert.LessOrEqual(off,30,$"{who} stands with the {side} upper arm {off:0} degrees off straight down: held out like a mannequin's");
            }
        }
        [UnityTest] public IEnumerator HeroesAndTownsfolkStandWithTheirArmsDown()
        {
            Flag("met.Survivor");yield return Zone("Sorrel_Ridges");
            var taren=PartyController.Current.members.First(m=>m.character=="Taren");var sela=PartyController.Current.members.First(m=>m.character=="Sela");
            var npc=Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).First(n=>n.GetComponentInChildren<Animator>()!=null&&n.GetComponentInChildren<Animator>().isHuman);
            // Read after a yield, the bones are last frame's finished pose, what LateUpdate left them. These checks read no pixels, so nothing here waits on the end of a rendered frame.
            yield return new WaitForSecondsRealtime(1.5f);
            foreach(var hero in new[]{taren,sela})
            {
                var rig=hero.GetComponent<FormController>().shaped.GetComponentInChildren<Animator>();
                Assert.IsTrue(rig.GetCurrentAnimatorStateInfo(0).IsName("CalmIdle"),$"{hero.character} stands with nothing near in a guard stance played empty-handed: Sword_Idle or Pistol_Idle_Loop, one arm above her head");
                ArmsHang(rig,hero.character);
            }
            ArmsHang(npc.GetComponentInChildren<Animator>(),npc.speaker);
            // In the fight's presence the stance comes back, and the arms are the stance's own.
            var rigged=taren.GetComponent<FormController>().shaped.GetComponentInChildren<Animator>();
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),taren.transform.position+Vector3.right*6);
            yield return new WaitForSecondsRealtime(1);
            Assert.IsFalse(rigged.GetCurrentAnimatorStateInfo(0).IsName("CalmIdle"),"with an enemy awake beside him the hero stands relaxed");
            Object.Destroy(enemy.gameObject);
        }
        [UnityTest] public IEnumerator TheArmsHangOnlyForStandingStill()
        {
            yield return Zone("Sorrel_Ridges");yield return new WaitForSecondsRealtime(1.5f);
            var hero=PartyController.Current.members.First(m=>m.character=="Taren");
            var driver=hero.GetComponent<FormController>().shaped.GetComponent<GeneratedAnimator>();var rig=driver.GetComponentInChildren<Animator>();
            Assert.AreEqual("Idle",driver.CurrentAnimation,"the feet and everything else that reads the state still see Idle, whichever stance is showing");
            Assert.IsTrue(hero.Attack(),"fixture: the swing");
            float widest=0;
            for(float end=Time.unscaledTime+.4f;Time.unscaledTime<end;)
            {
                yield return null;
                widest=Mathf.Max(widest,Mathf.Max(ArmOffDown(rig,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm),ArmOffDown(rig,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm)));
            }
            Assert.Greater(widest,35,$"through a whole swing no arm gets more than {widest:0} degrees off hanging: the hang pins the arms when it should only be for standing still");
        }
        [UnityTest] public IEnumerator HessaReadsBeatenAfterSheYields()
        {
            Flag("relay.hessaConfronted");
            yield return Zone("SunkenRelay",false);
            var duel=Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(e=>e.encounterId=="Relay_Hessa");
            var person=Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(n=>n.speaker=="Hessa");
            duel.Begin();yield return null;
            var boss=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Single(e=>e.definition.id=="HessaKorl");boss.Passive=true;
            var rigged=boss.GetComponentInChildren<Animator>();yield return new WaitForSecondsRealtime(.5f);
            float standing=Vector3.Angle(rigged.GetBoneTransform(HumanBodyBones.Head).position-rigged.GetBoneTransform(HumanBodyBones.Hips).position,Vector3.up);
            DialogueSystem.Current.AutoAdvance=true;
            boss.Health.Damageable=true;boss.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=boss.Health.maximum*50,type=DamageType.Plasma});
            for(float end=Time.unscaledTime+6;!person.gameObject.activeInHierarchy&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(person.gameObject.activeInHierarchy,"fixture: she takes over where she fell");
            yield return new WaitForSecondsRealtime(.3f);
            var rig=person.GetComponentInChildren<Animator>();
            Assert.AreEqual(0,rig.speed,.001f,"her beaten pose doesn't hold");
            // Either the clip has her low, or what's laid over it has to say she's done: which clip it is isn't this test's business.
            Vector3 Bone(HumanBodyBones bone)=>rig.GetBoneTransform(bone).position;
            float legs=Vector3.Distance(Bone(HumanBodyBones.LeftUpperLeg),Bone(HumanBodyBones.LeftLowerLeg))+Vector3.Distance(Bone(HumanBodyBones.LeftLowerLeg),Bone(HumanBodyBones.LeftFoot));
            bool low=Bone(HumanBodyBones.Hips).y-Bone(HumanBodyBones.LeftFoot).y<legs*.7f;
            float bowed=Vector3.Angle(Bone(HumanBodyBones.Head)-Bone(HumanBodyBones.Hips),Vector3.up);
            TestContext.WriteLine($"HESSA_BOW standing={standing:0} bowed={bowed:0} low={low}");
            if(low)yield break;
            ArmsHang(rig,"Hessa after she yields");
            Assert.GreaterOrEqual(bowed,standing+8,$"she stands mid-stride with her back straight ({bowed:0} degrees against {standing:0} in the fight): nothing shows she has given up");
            Assert.Less(Vector3.Dot(Bone(HumanBodyBones.Hips)-Bone(HumanBodyBones.Head),person.GetComponentInChildren<GeneratedAnimator>().transform.forward),0,"she bows backward");
        }

        // ------------------------------------------------------------------ 8. the views that pump
        [UnityTest] public IEnumerator TheFlightViewEasesBackAsATargetAppears()
        {
            yield return Zone("Arena_Flight");
            var party=PartyController.Current;party.enabled=false;var hero=Hero;hero.GetComponent<FlightMotor>().Halt();
            var rig=Object.FindFirstObjectByType<CameraRig>();yield return new WaitForSecondsRealtime(1);
            var camera=Camera.main;float Back()=>Vector3.Dot(hero.transform.position-camera.transform.position,camera.transform.forward);
            float start=Back();
            rig.FrameEncounter(new Bounds(hero.transform.position,new Vector3(40,8,40)));float began=Time.unscaledTime;
            while(Time.unscaledTime-began<.15f){rig.FrameEncounter(new Bounds(hero.transform.position,new Vector3(40,8,40)));yield return null;}
            float early=Back();
            while(Time.unscaledTime-began<1.6f)yield return null;
            float late=Back();while(Time.unscaledTime-began<3)yield return null;float settled=Back();
            float share=(early-start)/(settled-start);
            TestContext.WriteLine($"FLIGHT_PULL start={start:0.0} early={early:0.0} late={late:0.0} settled={settled:0.0}");
            Assert.Greater(settled,start+10,"fixture: the encounter needs the view further back");
            Assert.LessOrEqual(share,.25f,$"the view whips {share:P0} of the way back in the first .15 s as the first target appears: a pump in a fixed-camera game");
            Assert.GreaterOrEqual((late-start)/(settled-start),.95f,"the view is still on its way back 1.6 s on: enemies are off screen while it comes");
        }
        [UnityTest] public IEnumerator TheShipStaysInFrameWhileTheViewEasesBack()
        {
            yield return Zone("Arena_Flight");
            var party=PartyController.Current;party.enabled=false;var hero=Hero;hero.GetComponent<FlightMotor>().Halt();
            var rig=Object.FindFirstObjectByType<CameraRig>();yield return new WaitForSecondsRealtime(1);
            // A target 30 m up the screen, the far end of what an encounter frames, and the commonest way one arrives in the Gullet. The view's middle goes half that way: if it outruns the pull, the ship is left below the frame.
            var camera=Camera.main;var ship=hero.transform.position;
            rig.FrameEncounter(new Bounds(ship+Vector3.forward*15,new Vector3(2.4f,2.4f,32.4f)));
            float lowest=1;
            for(float end=Time.unscaledTime+1.6f;Time.unscaledTime<end;){yield return null;lowest=Mathf.Min(lowest,camera.WorldToViewportPoint(ship).y);}
            TestContext.WriteLine($"FLIGHT_SHIP lowest={lowest:0.00}");
            Assert.GreaterOrEqual(lowest,.05f,$"as a target comes into reach the player's ship drops to {lowest:0.00} of the screen: off the bottom while the view catches up");
        }
        [UnityTest] public IEnumerator AShooterFiringAtTheHeroIsKeptInFrame()
        {
            yield return Zone("Arena_Ground");Arena();
            var hero=Hero;Put(hero,new Vector3(200,.1f,0));Put(Partner,new Vector3(185,.1f,0));yield return new WaitForSecondsRealtime(1);
            var camera=Camera.main;var up=Flat(camera.transform.forward).normalized;var rig=Object.FindFirstObjectByType<CameraRig>();
            var enemy=Enemy("MusterScav",hero.transform.position+up*14);enemy.Passive=false;enemy.GetComponent<CharacterController>().enabled=false;
            yield return new WaitForSecondsRealtime(5);
            var feet=camera.WorldToViewportPoint(enemy.transform.position);var head=camera.WorldToViewportPoint(enemy.transform.position+Vector3.up*2.2f);
            Assert.IsTrue(Inside(feet,0,0,1,1)&&Inside(head,0,0,1,1),$"long danger lines come in from a figure off the top of the screen, only its legs in frame (feet {feet}, head {head})");
            Assert.LessOrEqual(rig.GroundCameraDistance,28.5f,"an ordinary fight draws back as far as a boss does");
            Object.Destroy(enemy.gameObject);yield return new WaitForSecondsRealtime(7);
            Assert.AreEqual(rig.profile.distance,rig.GroundCameraDistance,.75f,"the camera stays drawn back after the shooter has gone");
        }
        [UnityTest] public IEnumerator AHeroWalkingPastIdleEnemiesIsNotFramedForThem()
        {
            yield return Zone("Arena_Ground");Arena();
            var hero=Hero;Put(hero,new Vector3(200,.1f,0));Put(Partner,new Vector3(185,.1f,0));yield return new WaitForSecondsRealtime(1);
            var up=Flat(Camera.main.transform.forward).normalized;var rig=Object.FindFirstObjectByType<CameraRig>();
            var enemy=Enemy("MusterScav",hero.transform.position+up*14);
            yield return new WaitForSecondsRealtime(3);
            Assert.AreEqual(rig.profile.distance,rig.GroundCameraDistance,.1f,"the camera draws back for a body that isn't firing at anyone");
            Object.Destroy(enemy.gameObject);
        }

        // ------------------------------------------------------------------ 9. what bursts, and the Cantor's payoff
        [UnityTest] public IEnumerator ABurstIsNotARingOfThinOutlines()
        {
            yield return Zone("Arena_Ground");Arena();yield return null;
            CombatVfx.Burst(new Vector3(200,1,0),new Color(.85f,.72f,.55f),"shape");yield return null;
            var alive=Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Where(p=>p.name=="Lattice particles"&&p.IsAlive()).ToList();
            Assert.Greater(alive.Count,0,"fixture: something burst");
            foreach(var burst in alive)Assert.AreNotEqual("circle_02",burst.GetComponent<ParticleSystemRenderer>().sharedMaterial.name,"the Bellows' bursts are forty thin tan outline rings: they look like debug gizmos, and they hang round it as it dies");
        }
        [UnityTest] public IEnumerator TheCantorsReleaseHasAMoment()
        {
            yield return Zone("Arena_Flight");
            var hero=Hero;Put(hero,new Vector3(30,1,-17));Put(Partner,new Vector3(40,1,-17));
            var enemy=Enemy("Cantor",new Vector3(30,1,0));yield return Frames(3);
            var packet=new DamagePacket{source=hero.Health,amount=enemy.Health.maximum*100,type=DamageType.Pulse};
            foreach(var link in enemy.GetComponent<CantorCollar>().Links)link.GetComponent<Hurtbox>().Hit(packet);
            enemy.Health.Receive(packet);yield return new WaitForSecondsRealtime(.2f);
            Assert.IsFalse(enemy.Health.Alive,"fixture: the lock is open");
            var ring=Object.FindObjectsByType<SkillRing>(FindObjectsSortMode.None);
            Assert.Greater(ring.Length,0,"the main fight's release has no visible payoff: no burst, and the Cantor simply swims off");
            yield return new WaitForSecondsRealtime(.4f);
            Assert.Greater(Object.FindObjectsByType<SkillRing>(FindObjectsSortMode.None).Length,0,"the release's ring is gone before anyone sees it: the fight's end cleared it with the rest of the field");
        }
    }
}
