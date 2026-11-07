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
    /// <summary>Batch 12, the third eye pass over the fights: a glowing ball on the Bellows with the hero inside it, a door that reads as a sticker, a boss that changes colour with nothing to cause it,
    /// a floor of planks with no safe side, a bright line from the world's origin, orange on the hero, a lane ending in teeth, a corpse that swallows the party, a partner under the HUD and a view that whips.
    /// Each failure message says what the player sees. The checks that read real pixels need the -Graphics runner the full suite already uses. Their bars are estimates: read the printed value before moving one.</summary>
    public sealed class QaFightThirdTests
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
        static CombatActor Named(string who)=>PartyController.Current.members.First(m=>m.character==who);
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
        static Renderer BodyMesh(Component who)=>who.GetComponentsInChildren<Renderer>(true).First(r=>r is MeshRenderer or SkinnedMeshRenderer);
        static bool ReadsAsDanger(Color c)
        {
            Color.RGBToHSV(c,out float hue,out float saturation,out _);hue*=360;
            return saturation>=.4f&&(hue<=50||hue>=250&&hue<=345);
        }
        static bool ReadsAsFire(Color c)
        {
            Color.RGBToHSV(c,out float hue,out float saturation,out _);hue*=360;
            return saturation>=.4f&&hue<=50;
        }
        static Transform[] Bubbles()=>Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Pressure bubble").ToArray();

        // ------------------------------------------------------------------ 1. the ball on the Bellows, and the door
        /// <summary>A hero 4 m from the dome with its volley on him, nearly at the end of its fuse. Hands back the boss with the bubbles gathered.</summary>
        static IEnumerator BubblesOn(CombatActor hero,System.Action<BossController> ready)
        {
            Put(hero,new Vector3(200,.1f,-4));Put(Partner,new Vector3(260,.1f,0));
            var enemy=Enemy("BellowsBelow",new Vector3(200,.1f,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();
            enemy.Health.integrity=enemy.Health.maximum*.45f;yield return Frames(2);Assert.AreEqual(2,boss.HealthPhase,"fixture: phase 2, where the bubbles begin");
            yield return new WaitForSecondsRealtime(1);
            Assert.IsTrue(boss.Perform("Bubbles"),"fixture: the bubbles");
            yield return new WaitForSeconds(2.85f);
            ready(boss);
        }
        [UnityTest] public IEnumerator TheBubblesGatherOverTheHeroAndNotOnHim()
        {
            yield return Zone("Arena_Ground");Arena();
            var hero=Hero;BossController boss=null;yield return BubblesOn(hero,b=>boss=b);
            var views=Bubbles();Assert.AreEqual(5,views.Length,"fixture: phase 2's five bubbles are still up");
            var chest=hero.transform.position+Vector3.up*.95f;float nearest=float.PositiveInfinity,closest=float.PositiveInfinity;
            for(int a=0;a<views.Length;a++)
            {
                nearest=Mathf.Min(nearest,Vector3.Distance(views[a].position,chest));
                for(int b=a+1;b<views.Length;b++)closest=Mathf.Min(closest,Vector3.Distance(views[a].position,views[b].position));
            }
            TestContext.WriteLine($"BUBBLES nearest={nearest:0.00} closest={closest:0.00}");
            Assert.GreaterOrEqual(nearest,1.2f,$"a bubble sits {nearest:0.00} m from the hero's chest: a glowing ball with him a thin shape inside it");
            Assert.GreaterOrEqual(closest,.8f,$"two bubbles stand {closest:0.00} m apart: stacked on one spot they're one ball of fire, not five bubbles");
            foreach(var mark in Telegraph.Live.Where(t=>t!=null&&t.owner==boss))Assert.IsTrue(mark.Contains(hero.transform.position),"the marking has to be what's on him: a hero who stands still is still struck");
            var drawn=Telegraph.Live.Where(t=>t!=null&&t.owner==boss&&t.GetComponent<MeshRenderer>().enabled).ToList();Assert.Greater(drawn.Count,0,"the bubbles' markings aren't drawn at all");
            for(int a=0;a<drawn.Count;a++)for(int b=a+1;b<drawn.Count;b++)Assert.GreaterOrEqual(Flat(drawn[a].Origin-drawn[b].Origin).magnitude,.3f,"rings drawn one on another on the same spot: five of them were a glaring disc");
            foreach(var view in views)
            {
                var colour=view.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor");
                Assert.IsFalse(ReadsAsFire(colour),$"a pressure bubble is {colour}: hot orange on the ribs reads as a fireball or a weak spot on the boss, and its own marking is purple");
            }
        }
        [UnityTest] public IEnumerator AHeroAmongTheBubblesStaysClear()
        {
            NeedsGraphics();yield return Zone("Arena_Ground");Arena();
            var hero=Hero;BossController boss=null;yield return BubblesOn(hero,b=>boss=b);
            var camera=Camera.main;var views=Bubbles().Select(t=>t.GetComponent<Renderer>()).ToArray();Assert.Greater(views.Length,0,"fixture: the bubbles are up");
            var with=Shot(camera);foreach(var view in views)view.enabled=false;var without=Shot(camera);foreach(var view in views)view.enabled=true;
            float worst=0;for(float up=.3f;up<=1.8f;up+=.15f)worst=Mathf.Max(worst,ChangeAt(with,without,camera,hero.transform.position+Vector3.up*up));
            TestContext.WriteLine($"BUBBLE_ON_HERO {worst:0.000}");
            Assert.LessOrEqual(worst,.12f,$"the bubbles shift the hero's own pixels by {worst:0.00}: he's an orange-tinted shape inside them, and the hero is the one thing that has to stay clear");
        }
        [UnityTest] public IEnumerator TheHerosOverdriveIsNotHotOrange()
        {
            yield return Zone("Arena_Ground");Arena();
            var taren=Named("Taren");Put(taren,new Vector3(200,.1f,0));Put(Named("Sela"),new Vector3(260,.1f,0));taren.charge=100;yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(taren.Skill(3),"fixture: Overdrive");yield return new WaitForSecondsRealtime(.65f);
            Assert.IsTrue(taren.Overdriving,"fixture: it's running");yield return null;
            var effect=taren.GetComponentsInChildren<ParticleSystem>(true).First(p=>p.name=="Overdrive charge");
            var particles=new ParticleSystem.Particle[2];Assert.AreEqual(2,effect.GetParticles(particles),"fixture: both hands");
            Color colour=particles[0].startColor;
            Assert.IsFalse(ReadsAsDanger(colour),$"Overdrive rings his hands in {colour}: the party's effects are cream and cyan, and orange on the hero is what's about to hit him");
        }
        [UnityTest] public IEnumerator TheDoorDissolvesAndTheRestOfTheBodyStaysSolid()
        {
            NeedsGraphics();yield return Zone("Arena_Ground");Arena();
            var hero=Hero;Put(hero,new Vector3(200,.1f,0));Put(Partner,new Vector3(215,.1f,0));yield return new WaitForSecondsRealtime(1.5f);
            var camera=Camera.main;var toward=Flat(camera.transform.position-hero.transform.position).normalized;
            var boss=Enemy("BellowsBelow",hero.transform.position+toward*2.3f);boss.GetComponent<CharacterController>().enabled=false;yield return new WaitForSecondsRealtime(.5f);NoShadows(boss);Hide(hero,true);yield return null;
            var body=BodyMesh(boss.GetComponent<DefeatPresentation>().visual);var block=new MaterialPropertyBlock();body.GetPropertyBlock(block);
            Assert.Greater(block.GetFloat("_OccFade"),.9f,"fixture: the boss is opening for the hero");
            Assert.GreaterOrEqual(block.GetFloat("_OccFadeSoft"),.99f,"what the door leaves is washed toward the zone's haze: a pale sticker on the boss, its outline the first thing anyone sees");
            var ellipse=block.GetVector("_OccFadeEllipse");
            var open=Shot(camera);var door=boss.GetComponent<BossOcclusion>();door.enabled=false;var shut=Shot(camera);door.enabled=true;Hide(hero,false);
            // Open is where the floor shows through the body: well over what the glow off the floor does to the pixels beside it.
            int inner=0,innerOpen=0,edge=0,edgeOpen=0,outside=0,outsideChanged=0;
            for(int y=0;y<ShotHeight;y++)for(int x=0;x<ShotWidth;x++)
            {
                float dx=((x+.5f)/ShotWidth-ellipse.x)/ellipse.z,dy=((y+.5f)/ShotHeight-ellipse.y)/ellipse.w,r=dx*dx+dy*dy;
                if(r>4)continue;
                bool changed=Change(open[y*ShotWidth+x],shut[y*ShotWidth+x])>.08f;
                if(r<.2f){inner++;if(changed)innerOpen++;}
                else if(r>=.65f&&r<=.85f){edge++;if(changed)edgeOpen++;}
                else if(r>=1.5f){outside++;if(changed)outsideChanged++;}
            }
            float atMiddle=innerOpen/(float)Mathf.Max(1,inner),atEdge=edgeOpen/(float)Mathf.Max(1,edge),beyond=outsideChanged/(float)Mathf.Max(1,outside);
            TestContext.WriteLine($"DOOR middle={atMiddle:0.00} edge={atEdge:0.00} beyond={beyond:0.000}");
            Assert.Greater(atMiddle,.35f,"fixture: the door is open over him");
            Assert.LessOrEqual(atEdge,atMiddle*.6f,$"the door opens {atEdge:P0} of the body near its edge against {atMiddle:P0} at its middle: a hard round patch with a screen-door rim, hiding what the player's hitting");
            Assert.LessOrEqual(beyond,.01f,$"{beyond:P1} of the body away from the door changes when it opens: the whole boss going pale and see-through");
        }

        // ------------------------------------------------------------------ 2. one thing at all times
        static Vector4 Paint(Renderer body){var block=new MaterialPropertyBlock();body.GetPropertyBlock(block);return block.GetVector("_BaseColor");}
        [UnityTest] public IEnumerator TheWreckTakesOverInTheBodysOwnColour()
        {
            Flag("met.Survivor");yield return Zone("Sorrel_Ridges",false);
            Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(e=>e.encounterId=="Sorrel_Burrower").Begin();yield return null;
            var boss=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Single(e=>e.definition.id=="Burrower");
            boss.Passive=false;yield return Frames(2);NoSpecials(boss);
            var site=Object.FindFirstObjectByType<BossWreck>();Assert.IsNotNull(site,"fixture: the wreck's site");Assert.IsFalse(site.wreck.activeInHierarchy,"fixture: the wreck isn't there yet");
            var body=BodyMesh(boss.GetComponent<DefeatPresentation>().visual);var hero=Hero;
            boss.Health.Damageable=true;boss.Health.Receive(new DamagePacket{source=hero.Health,amount=boss.Health.maximum*50,type=DamageType.Plasma});
            Vector4 last=Vector4.zero;float steepest=0;bool any=false;
            for(float end=Time.unscaledTime+8;body!=null&&!site.wreck.activeInHierarchy&&Time.unscaledTime<end;)
            {
                // Nothing in the block until something writes the colour: on the frame it dies it still has only its material's.
                var now=Paint(body);if(now!=Vector4.zero){if(any)steepest=Mathf.Max(steepest,(now-last).magnitude);last=now;any=true;}
                yield return null;
            }
            Assert.IsTrue(site.wreck.activeInHierarchy,"fixture: the wreck takes over");yield return null;
            var wreck=Paint(BodyMesh(site.rig));
            TestContext.WriteLine($"WRECK_PAINT body={last} wreck={wreck} steepest={steepest:0.000}");
            Assert.LessOrEqual((wreck-last).magnitude,.03f,$"the body wears {last} as the wreck in {wreck} takes over from it: seconds after the fall, with nothing on screen to cause it, a dusty machine goes to glazed caramel");
            Assert.LessOrEqual(steepest,.08f,$"the body's colour jumps by {steepest:0.00} in a frame as it falls: a pop, not burning out");
        }

        // ------------------------------------------------------------------ 3. a mark bigger than the screen, and a safe side
        static Vector4 HatchOf(Telegraph mark){var block=new MaterialPropertyBlock();mark.GetComponent<MeshRenderer>().GetPropertyBlock(block);return block.GetVector("_Hatch");}
        [UnityTest] public IEnumerator AMarkBiggerThanTheScreenIsARimAndAFaintWash()
        {
            NeedsGraphics();yield return Zone("Hushwell");
            var spot=new Vector2(-4,255);float level=HushwellLayout.Middle;
            Put(Hero,Floor(spot.x,spot.y,level));Put(Partner,Floor(spot.x-3,spot.y,level));yield return new WaitForSecondsRealtime(1.5f);
            var camera=Camera.main;var center=Floor(spot.x,spot.y,level);
            var breath=Telegraph.Cone(center,Vector3.right,24,180,5,Telegraph.Stagger);var ring=Telegraph.Circle(center,13,.01f,Telegraph.Threat);var shot=Telegraph.Line(center,Vector3.forward,13,1.1f,5,Telegraph.Threat,true);yield return new WaitForSecondsRealtime(.25f);
            Assert.LessOrEqual(HatchOf(breath).w,.15f,"half the chamber is marked in lines as strong as they were: a floor of purple planks");
            Assert.LessOrEqual(HatchOf(ring).w,.15f,"a ring wider than the screen is marked in lines as strong as they were: a floor of orange planks");
            Assert.LessOrEqual(HatchOf(shot).w,.5f,"a shot's narrow lane has a full hatch: two rails and the rungs between them, a ladder");
            Object.Destroy(breath.gameObject);Object.Destroy(shot.gameObject);yield return null;
            var view=ring.GetComponent<MeshRenderer>();var marked=Shot(camera);view.enabled=false;var bare=Shot(camera);view.enabled=true;
            var fill=new List<float>();
            for(float x=-5;x<=5;x+=.09f)for(float z=-3;z<=3;z+=.09f)fill.Add(ChangeAt(marked,bare,camera,Floor(center.x+x,center.z+z,level)));
            float hatch=Percentile(fill,.92f),wash=Percentile(fill,.5f);
            TestContext.WriteLine($"HUGE_WASH wash={wash:0.000} hatch={hatch:0.000}");
            Assert.LessOrEqual(hatch-wash,.02f,$"inside a ring wider than the screen the lines stand {hatch-wash:0.000} over the wash: they still read as planks");
        }
        [UnityTest] public IEnumerator ABreathWithAnOrganBlowingMarksOneThingAndLeavesASafeSide()
        {
            yield return Zone("Hushwell");
            var lead=Hero;var partner=Partner;float floor=HushwellLayout.OnFloor(HushwellLayout.Bellows).y;
            var enemy=Enemy("BellowsBelow",HushwellLayout.OnFloor(HushwellLayout.Bellows));yield return Frames(3);var boss=enemy.GetComponent<BossController>();
            Assert.IsTrue(PressureOrgan.AnyPumping,"fixture: the organs pump");
            Put(lead,PressureOrgan.Middle+Vector3.forward*2);Put(partner,PressureOrgan.Middle+Vector3.back*40);
            Assert.IsTrue(boss.Perform("Breath"),"fixture: the breath");yield return null;
            var marks=Telegraph.Live.Where(t=>t!=null&&t.owner==boss).ToList();
            Assert.AreEqual(1,marks.Count,$"a breath with an organ pumping marks {marks.Count} things at once, its half and the ring round the dome: all the floor on screen is marked and there's no safe side to run to");
            var half=marks[0];Assert.AreEqual(Telegraph.Kind.Cone,half.Shape,"while an organ pumps the breath goes out through it, across its half");
            // On the side it doesn't mark nothing is coming, and nobody says otherwise.
            var safe=PressureOrgan.Middle-half.Forward*9;safe.y=floor;Put(lead,safe);yield return Frames(2);
            Assert.IsFalse(half.Contains(lead.transform.position),"fixture: he's on the safe side");
            Assert.IsFalse(boss.Telegraphing,"a hero on the safe side is told a hit is coming at him");
            for(float end=Time.unscaledTime+6;boss.Busy&&Time.unscaledTime<end;)yield return null;
            // With no organ left to blast, the dome breathes for itself.
            foreach(var organ in PressureOrgan.All.ToArray())organ.Health.Receive(new DamagePacket{source=lead.Health,amount=organ.Health.maximum*10,type=DamageType.Kinetic});
            for(float end=Time.unscaledTime+6;(boss.Busy||boss.Health.Broken)&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(boss.Perform("Breath"),"fixture: the breath with the organs broken");yield return null;
            marks=Telegraph.Live.Where(t=>t!=null&&t.owner==boss&&!t.Released).ToList();
            Assert.AreEqual(1,marks.Count,"with the organs broken a breath marks one thing");Assert.AreEqual(Telegraph.Kind.Circle,marks[0].Shape,"with the organs broken the breath is the ring round the dome");
        }
        [UnityTest] public IEnumerator TheWayOutOfABreathIsOnScreenWhileItIsTold()
        {
            yield return Zone("Hushwell",false);
            Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(e=>e.encounterId=="Hushwell_Bellows").Begin();yield return null;
            var boss=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Single(e=>e.definition.id=="BellowsBelow");
            boss.Passive=false;yield return Frames(2);NoSpecials(boss);boss.GetComponent<CharacterController>().enabled=false;
            // At the organ that blasts first, locked on it, in the half it marks: the bar sends him there, and it's as far as anyone stands from the safe side.
            var organ=PressureOrgan.All.First(o=>o.Pumping);var mid=PressureOrgan.Middle;var side=Flat(organ.transform.position-mid).normalized;var hero=Hero;
            var at=mid+side*13+Vector3.back*3;Put(hero,Floor(at.x,at.z,HushwellLayout.Middle));Put(Partner,Floor(at.x+1.5f,at.z,HushwellLayout.Middle));
            hero.target=organ.Health;hero.TargetLocked=true;yield return new WaitForSecondsRealtime(3);
            Assert.IsTrue(boss.GetComponent<BossController>().Perform("Breath"),"fixture: the breath");yield return null;
            var half=Telegraph.Live.First(t=>t!=null&&t.owner==boss.GetComponent<BossController>());Assert.IsTrue(half.Contains(hero.transform.position),"fixture: he stands in what it marks");
            yield return new WaitForSeconds(.85f);
            var camera=Camera.main;var wayOut=half.EdgeFrom(hero.transform.position);var edge=camera.WorldToViewportPoint(wayOut);var aimed=camera.WorldToViewportPoint(organ.transform.position+Vector3.up*1.5f);var feet=camera.WorldToViewportPoint(hero.transform.position);
            TestContext.WriteLine($"BREATH_WAY_OUT edge={edge} organ={aimed} hero={feet}");
            Assert.Less(Mathf.Abs(Vector3.Dot(Flat(wayOut-mid),half.Forward)),.6f,$"the way out from beside the organ is {wayOut}, past the chamber's wall, where nobody can stand: the safe side is back across the line");
            Assert.IsTrue(Inside(edge,.01f,.01f,.99f,.99f),$"the nearest safe floor, across the marking's edge, is off screen ({edge}) with the breath about to land");
            Assert.IsTrue(Inside(aimed,.01f,.01f,.99f,.99f),$"the organ he's locked on has left the frame ({aimed})");
            Assert.IsTrue(Inside(feet,.08f,.18f,.92f,.92f),$"the hero has gone under the HUD or out of frame ({feet})");
        }

        // ------------------------------------------------------------------ 4. the bright line
        /// <summary>Pixels the shot changes further off the line it has really flown than any trail can stand: however long this frame took, muzzle to head is all it may draw.</summary>
        static IEnumerator FarPixels(Camera camera,Vector3 from,DamagePacket packet,System.Action<int> report)
        {
            var known=new HashSet<Projectile>(Projectile.Live);
            Projectile.Fire(from,Vector3.forward,packet);var shot=Projectile.Live.First(s=>!known.Contains(s));yield return null;
            var with=Shot(camera);var trail=shot.GetComponent<TrailRenderer>();var ball=shot.GetComponent<MeshRenderer>();
            trail.enabled=false;ball.enabled=false;var without=Shot(camera);trail.enabled=true;ball.enabled=true;
            var head=camera.WorldToViewportPoint(shot.transform.position);var muzzle=camera.WorldToViewportPoint(from);
            var end=new Vector2(head.x*ShotWidth,head.y*ShotHeight);var start=new Vector2(muzzle.x*ShotWidth,muzzle.y*ShotHeight);var flown=end-start;int far=0;
            for(int y=0;y<ShotHeight;y+=2)for(int x=0;x<ShotWidth;x+=2)
            {
                var pixel=new Vector2(x,y);float along=Mathf.Clamp01(Vector2.Dot(pixel-start,flown)/Mathf.Max(1,flown.sqrMagnitude));
                if((pixel-start-flown*along).sqrMagnitude<120*120)continue;
                if(Change(with[y*ShotWidth+x],without[y*ShotWidth+x])>.06f)far++;
            }
            report(far);
        }
        [UnityTest] public IEnumerator AShotsTrailStartsAtItsMuzzle()
        {
            NeedsGraphics();yield return Zone("Arena_Ground");Arena();
            var sela=Named("Sela");Put(sela,new Vector3(200,.1f,0));Put(Named("Taren"),new Vector3(260,.1f,0));yield return new WaitForSecondsRealtime(1);
            var camera=Camera.main;var muzzle=sela.transform.position+Vector3.up+Vector3.forward*1.5f;var packet=sela.Packet(1,DamageType.Beam,0);
            // The first shot of a scene: its object is born at the world's origin, far off the bottom of the screen here, as Sorrel's and the Gullet's are.
            int far=-1;yield return FarPixels(camera,muzzle,packet,n=>far=n);
            TestContext.WriteLine($"TRAIL new={far}");
            Assert.LessOrEqual(far,30,$"a new shot draws {far} pixels away from itself: a long bright line from where its object was born to the muzzle, the brightest thing on screen");
            // One from the pool, that last died somewhere else: a spot on the far side of the screen.
            yield return new WaitForSecondsRealtime(.3f);
            Projectile.Fire(sela.transform.position+new Vector3(9,1,14),Vector3.forward,packet,24,.01f);
            for(float end=Time.unscaledTime+.5f;Projectile.Live.Count>0&&Time.unscaledTime<end;)yield return null;
            yield return new WaitForSecondsRealtime(.2f);
            yield return FarPixels(camera,muzzle,packet,n=>far=n);
            TestContext.WriteLine($"TRAIL reused={far}");
            Assert.LessOrEqual(far,30,$"a reused shot draws {far} pixels away from itself: a line from where the last one died to the muzzle");
        }

        // ------------------------------------------------------------------ 5. the party's colours in a lunge, and a strike on a dark floor
        [UnityTest] public IEnumerator ALungeIsNotTheColourOfDanger()
        {
            yield return Zone("Arena_Flight");
            var taren=Named("Taren");var sela=Named("Sela");Put(taren,new Vector3(200,1,0));Put(sela,new Vector3(212,1,0));taren.charge=100;
            var victim=Enemy("ChoristerDart",new Vector3(200,1,7));victim.Health.maximum=victim.Health.integrity=1000;taren.target=victim.Health;Physics.SyncTransforms();yield return Frames(2);
            Assert.IsTrue(taren.Lunge(),"fixture: the lunge");
            var feathers=new List<Color>();
            for(float end=Time.unscaledTime+.5f;Time.unscaledTime<end;)
            {
                taren.motor.Move(Vector2.zero,false,false);
                foreach(var burst in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                    if(burst.name=="Lattice particles"&&burst.IsAlive()&&burst.GetComponent<ParticleSystemRenderer>().sharedMaterial.name.Contains("slash"))feathers.Add(burst.main.startColor.color);
                yield return null;
            }
            var edge=taren.transform.Find("Flight lunge edge").GetComponent<LineRenderer>();
            Assert.IsFalse(ReadsAsDanger(edge.startColor),$"a thin ring in {edge.startColor} round the hero's ship in his lunge: the colour of the enemy's attack outlines, so it reads as an attack marked on him");
            Assert.Greater(feathers.Count,0,"fixture: the lunge laid its feathers");
            foreach(var feather in feathers)Assert.IsFalse(ReadsAsDanger(feather),$"the lunge's feathers trail behind him in {feather}");
            var flames=taren.GetComponent<FormController>().flight.GetComponentsInChildren<LineRenderer>(true).Where(l=>l.name=="Flight exhaust").ToList();Assert.Greater(flames.Count,0,"fixture: his exhaust");
            foreach(var flame in flames){var colour=flame.endColor;colour.a=1;Assert.IsFalse(ReadsAsDanger(colour),$"his exhaust trails off in {colour}");}
        }
        [UnityTest] public IEnumerator AStrikeOnADarkFloorIsNotASalmonSlab()
        {
            NeedsGraphics();yield return Zone("Gullet_Tunnel");yield return new WaitForSecondsRealtime(1.5f);
            var hero=Hero;var camera=Camera.main;var origin=hero.transform.position+new Vector3(4,0,-1);
            var lane=Telegraph.Line(origin,Vector3.forward,6.15f,2.3f,.01f,Telegraph.Threat,true);yield return new WaitForSecondsRealtime(.2f);
            // The same lane, full and steady, then at the height of its flash, however this frame's clock fell.
            var view=lane.GetComponent<MeshRenderer>();lane.enabled=false;var block=new MaterialPropertyBlock();view.GetPropertyBlock(block);block.SetFloat("_Fill",1);block.SetFloat("_Fade",1);block.SetFloat("_Flash",0);view.SetPropertyBlock(block);
            var steady=Shot(camera);block.SetFloat("_Flash",1);view.SetPropertyBlock(block);var flashing=Shot(camera);
            // A marking lies flat under the ships in flight, at this height.
            var fill=new List<float>();
            for(float along=.8f;along<=5.4f;along+=.07f)for(float across=-.8f;across<=.8f;across+=.07f)fill.Add(ChangeAt(flashing,steady,camera,new Vector3(origin.x+across,.15f,origin.z+along)));
            float lift=Percentile(fill,.5f);
            TestContext.WriteLine($"SALMON lift={lift:0.000}");
            Assert.LessOrEqual(lift,.09f,$"at a strike the whole lane lifts by {lift:0.00} on the dark floor: a flat pale salmon slab with hard edges, like a debug shape");
            Object.Destroy(lane.gameObject);
        }

        // ------------------------------------------------------------------ 6. a lane that meets a wall
        /// <summary>The ground's real surface over a point: a hill, not a prop standing on it.</summary>
        static bool Ground(Vector3 point,out float height)
        {
            height=float.NegativeInfinity;bool any=false;
            foreach(var hit in Physics.RaycastAll(new Vector3(point.x,80,point.z),Vector3.down,120,~0,QueryTriggerInteraction.Ignore))
                if(hit.collider is MeshCollider&&hit.point.y>height){height=hit.point.y;any=true;}
            return any;
        }
        static void EndsOnTheFloor(Telegraph lane,string what)
        {
            var mesh=lane.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var uv=mesh.uv;
            var rim=Enumerable.Range(0,vertices.Length).Where(i=>Mathf.Abs(uv[i].x-1)<.001f).ToList();
            Assert.GreaterOrEqual(rim.Count,2,$"fixture: {what} has a far edge");
            Assert.IsTrue(vertices.Any(v=>v.x>19),$"fixture: {what} runs at the wall");
            float reach=0;
            foreach(int i in rim)
            {
                Assert.IsTrue(Ground(vertices[i],out float top),$"fixture: there's ground under {what}");
                float buried=top-vertices[i].y;reach=Mathf.Max(reach,buried);
                Assert.LessOrEqual(buried,.45f,$"{what}'s far rim lies {buried:0.0} m under the hill it ran into, so nothing closes it: its hatch stops in a saw-tooth at the wall's foot with a thin rim running on beyond");
            }
            TestContext.WriteLine($"LANE_END {what} buried={reach:0.00} rim={rim.Count}");
            float furthest=rim.Max(i=>vertices[i].x);
            Assert.Less(furthest,19,$"{what} is painted up to x {furthest:0.0}, past the wall's foot");
        }
        [UnityTest] public IEnumerator ALaneEndsInARimAtTheWallsFoot()
        {
            yield return Zone("Sorrel_Ridges");
            // Sorrel's arena, the wall to the east: a charge, and one of the boss's shots with its coarser samples. Numbers from the real terrain: both reach the foot at about x 16.
            var charge=Telegraph.Line(Floor(4,165),new Vector3(1,0,.2f),20,3.4f,5,Telegraph.Heavy);
            var spit=Telegraph.Line(Floor(8,172),new Vector3(1,0,.05f),15,1.1f,5,Telegraph.Threat,true);
            EndsOnTheFloor(charge,"a charge's lane");EndsOnTheFloor(spit,"a shot's lane");
            Object.Destroy(charge.gameObject);Object.Destroy(spit.gameObject);yield return null;
            // On open floor it's the whole lane, as it always was.
            Arena();yield return null;
            var open=Telegraph.Line(new Vector3(200,0,0),Vector3.forward,10,2.4f,5,Telegraph.Heavy);var mesh=open.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var uv=mesh.uv;
            var flags=new List<Vector3>();mesh.GetUVs(1,flags);
            Assert.IsTrue(uv.All(u=>u.x<=1.0001f),"a lane on open floor paints past its own end");
            Assert.AreEqual(10,Enumerable.Range(0,vertices.Length).Where(i=>uv[i].x>.999f).Max(i=>vertices[i].z),.01f,"a lane on open floor stops short of its length");
            Assert.IsTrue(flags.All(f=>f.z>.99f),"a lane on open floor has lost its outline through props");
            // One side of it over a hole, or caught on a wall's foot, ends nothing else: the rest of it is still floor, a charge still runs down it, and past the paint it would strike with nothing marked.
            void Slab(GameObject slab,float x0,float x1,float z0,float z1){slab.name="Lane fixture floor";slab.transform.position=new Vector3((x0+x1)*.5f,-.5f,(z0+z1)*.5f);slab.transform.localScale=new Vector3(x1-x0,1,z1-z0);}
            Slab(GameObject.CreatePrimitive(PrimitiveType.Cube),399,430,-5,30);Slab(GameObject.CreatePrimitive(PrimitiveType.Cube),380,399,-5,.5f);
            var beyond=GameObject.CreatePrimitive(PrimitiveType.Cube);Slab(beyond,380,399,2.5f,30);Physics.SyncTransforms();yield return null;
            float PaintedTo(Telegraph lane)
            {
                var laid=lane.GetComponent<MeshFilter>().sharedMesh;var points=laid.vertices;var along=laid.uv;
                return Enumerable.Range(0,points.Length).Where(i=>points[i].x>399&&Mathf.Abs(along[i].x-1)<.001f).Min(i=>points[i].z);
            }
            var overHole=Telegraph.Line(new Vector3(400,0,0),Vector3.forward,20,3.4f,5,Telegraph.Heavy);
            float painted=PaintedTo(overHole);Object.Destroy(overHole.gameObject);Object.Destroy(beyond);yield return null;Physics.SyncTransforms();
            TestContext.WriteLine($"LANE_PAINTED over a hole to z {painted:0.0}");
            Assert.AreEqual(20,painted,.01f,$"a hole 2 m long under one side of a 20 m lane ends its paint at {painted:0.0} m: the charge runs on past it over floor with nothing marked on it");
            var alongWall=Telegraph.Line(new Vector3(400,0,0),Vector3.forward,20,3.4f,5,Telegraph.Heavy);
            painted=PaintedTo(alongWall);Object.Destroy(alongWall.gameObject);
            TestContext.WriteLine($"LANE_PAINTED along a wall to z {painted:0.0}");
            Assert.AreEqual(20,painted,.01f,$"one side of a 20 m lane off the floor from its start ends its paint at {painted:0.0} m: the charge runs on along the wall over floor with nothing marked on it");
        }

        // ------------------------------------------------------------------ 7. a corpse that keeps to itself
        [UnityTest] public IEnumerator TheBellowsDeflatesWithinItsLivingWidth()
        {
            yield return Zone("Arena_Ground");Arena();
            var hero=Hero;Put(hero,new Vector3(200,.1f,12));Put(Partner,new Vector3(214,.1f,12));
            var enemy=Enemy("BellowsBelow",new Vector3(200,.1f,0));yield return Frames(3);
            var body=BodyMesh(enemy.GetComponent<DefeatPresentation>().visual);float living=Mathf.Max(body.bounds.extents.x,body.bounds.extents.z);
            enemy.Health.Damageable=true;enemy.Health.Receive(new DamagePacket{source=hero.Health,amount=enemy.Health.maximum*50,type=DamageType.Plasma});
            float widest=0,last=living;
            for(float end=Time.unscaledTime+1.6f;Time.unscaledTime<end;){last=Mathf.Max(body.bounds.extents.x,body.bounds.extents.z);widest=Mathf.Max(widest,last);yield return null;}
            TestContext.WriteLine($"DEFLATE living={living:0.00} widest={widest:0.00} last={last:0.00}");
            // Over its breathing, which is ±3.5% and caught wherever it was.
            Assert.LessOrEqual(widest,living*1.05f,$"as the Bellows dies it spreads to {widest:0.00} m from {living:0.00} m: it flattens out under the party, with Sela standing in its ribs");
            Assert.LessOrEqual(last,2.05f,$"it lies {last:0.00} m wide where its capsule stopped them at 1.75 m: anyone who fought beside it is standing in it");
        }

        // ------------------------------------------------------------------ 8. the partner, last of all
        [UnityTest] public IEnumerator ThePartnerGoesInIfSheFits()
        {
            yield return Zone("Arena_Ground");Arena();
            var hero=Hero;var partner=Partner;var camera=Camera.main;var up=Flat(camera.transform.forward).normalized;var right=Flat(camera.transform.right).normalized;var rig=Object.FindFirstObjectByType<CameraRig>();
            Put(hero,new Vector3(200,.1f,0));Put(partner,new Vector3(201,.1f,0));
            var boss=Enemy("Burrower",hero.transform.position+up*12);yield return Frames(3);NoSpecials(boss);boss.GetComponent<CharacterController>().enabled=false;boss.Passive=false;
            yield return new WaitForSecondsRealtime(3);
            float alone=rig.GroundCameraDistance;
            // South of him, where framing the hero and the boss first left her legs under the hint strip and the rest of her behind the skill icons.
            Put(partner,hero.transform.position-up*4+right*7);yield return new WaitForSecondsRealtime(4);
            var feet=camera.WorldToViewportPoint(partner.transform.position);var head=camera.WorldToViewportPoint(partner.transform.position+Vector3.up*1.9f);
            TestContext.WriteLine($"PARTNER_FRAMED alone={alone:0.0} now={rig.GroundCameraDistance:0.0} feet={feet} head={head}");
            Assert.IsTrue(Inside(feet,.04f,.18f,.96f,.92f)&&Inside(head,.04f,.18f,.96f,.92f),$"in a boss fight with room to spare the partner stands under the HUD or off the screen (feet {feet}, head {head}), her bolts coming in from nowhere");
            foreach(var point in new[]{hero.transform.position,hero.transform.position+Vector3.up*1.9f})
            {var view=camera.WorldToViewportPoint(point);Assert.IsTrue(Inside(view,.14f,.22f,.86f,.86f),$"the hero is pushed about for her ({view})");}
            foreach(var point in new[]{boss.transform.position,boss.transform.position+Vector3.up*2})
            {var view=camera.WorldToViewportPoint(point);Assert.IsTrue(Inside(view,.01f,.01f,.99f,.99f),$"the boss goes out of frame for her ({view})");}
            // Too far off to share a view: she costs nothing, and the view doesn't pop as she goes.
            Put(partner,hero.transform.position-up*60);yield return new WaitForSecondsRealtime(5);
            Assert.AreEqual(alone,rig.GroundCameraDistance,1.5f,"a partner 60 m off draws the view back for someone it can't show");
        }
        [UnityTest] public IEnumerator AnOrdinaryFightKeepsThePartnerOnScreenAndLetsGoAfter()
        {
            yield return Zone("Arena_Ground");Arena();
            var hero=Hero;var partner=Partner;var camera=Camera.main;var up=Flat(camera.transform.forward).normalized;var right=Flat(camera.transform.right).normalized;var rig=Object.FindFirstObjectByType<CameraRig>();
            Put(hero,new Vector3(200,.1f,0));Put(partner,hero.transform.position-up*5-right*9);yield return new WaitForSecondsRealtime(3);
            Assert.AreEqual(rig.profile.distance,rig.GroundCameraDistance,.1f,"with nothing awake the view doesn't draw back for her");
            var enemy=Enemy("Scrapmite",hero.transform.position+right*3);enemy.Passive=false;enemy.GetComponent<CharacterController>().enabled=false;
            yield return new WaitForSecondsRealtime(4);
            var feet=camera.WorldToViewportPoint(partner.transform.position);var head=camera.WorldToViewportPoint(partner.transform.position+Vector3.up*1.9f);
            TestContext.WriteLine($"PARTNER_ORDINARY now={rig.GroundCameraDistance:0.0} feet={feet} head={head}");
            Assert.IsTrue(Inside(feet,.04f,.16f,.96f,.92f)&&Inside(head,.04f,.16f,.96f,.92f),$"in an ordinary fight the partner stands under the bars or off the screen (feet {feet}, head {head})");
            Assert.LessOrEqual(rig.GroundCameraDistance,28.5f,"an ordinary fight draws back as far as a boss does");
            Object.Destroy(enemy.gameObject);yield return new WaitForSecondsRealtime(7);
            Assert.AreEqual(rig.profile.distance,rig.GroundCameraDistance,.75f,"the camera stays drawn back after the fight");
        }
        [UnityTest] public IEnumerator ThePartnerNeverCostsTheHeroHisTarget()
        {
            yield return Zone("Hushwell",false);
            Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(e=>e.encounterId=="Hushwell_Bellows").Begin();yield return null;
            var boss=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Single(e=>e.definition.id=="BellowsBelow");
            boss.Passive=false;yield return Frames(2);NoSpecials(boss);boss.GetComponent<CharacterController>().enabled=false;
            var hero=Hero;var partner=Partner;float level=HushwellLayout.Middle;
            var west=Object.FindObjectsByType<PressureOrgan>(FindObjectsSortMode.None).Single(o=>o.name.Contains("west"));
            // Both framing tests' spots, with her across the chamber from the organ, where she can't all fit.
            foreach(var (heroAt,bossAt,herAt) in new[]{(new Vector2(-9,262),new Vector2(18,256),new Vector2(12,246)),(new Vector2(10,258),new Vector2(0,268),new Vector2(24,244))})
            {
                Put(hero,Floor(heroAt.x,heroAt.y,level));Put(partner,Floor(herAt.x,herAt.y,level));
                boss.transform.position=Floor(bossAt.x,bossAt.y,level);
                hero.target=west.Health;hero.TargetLocked=true;
                yield return new WaitForSecondsRealtime(4);
                var camera=Camera.main;var organ=camera.WorldToViewportPoint(west.transform.position+Vector3.up*1.5f);
                Assert.IsTrue(Inside(organ,.01f,.01f,.99f,.99f),$"for the partner's sake the organ he's locked on is off screen ({organ}) with him at {heroAt}");
                foreach(var point in new[]{hero.transform.position,hero.transform.position+Vector3.up*1.9f})
                {var view=camera.WorldToViewportPoint(point);Assert.IsTrue(Inside(view,.08f,.18f,.92f,.92f),$"for the partner's sake the hero went under the HUD or out of frame ({view})");}
            }
        }

        // ------------------------------------------------------------------ 9. a view that whips mid-fight
        [UnityTest] public IEnumerator AStepMidFightDoesNotWhipTheView()
        {
            yield return Zone("Arena_Flight");
            var party=PartyController.Current;party.enabled=false;var hero=Hero;hero.GetComponent<FlightMotor>().Halt();
            var rig=Object.FindFirstObjectByType<CameraRig>();yield return new WaitForSecondsRealtime(1);
            var camera=Camera.main;var ship=hero.transform.position;
            // In flight the view stands straight back along its own axis from what it holds, at the ship's height here, so its height is its distance.
            float pitch=Mathf.Sin(rig.profile.pitch*Mathf.Deg2Rad);float Back()=>(camera.transform.position.y-ship.y)/pitch;
            var near=new Bounds(ship+Vector3.forward*8,new Vector3(6,4,20));var far=new Bounds(ship,new Vector3(8,4,58));
            rig.FrameEncounter(near);yield return new WaitForSecondsRealtime(3);
            float before=Back();
            // A new target and the partner coming inside the range that frames her, in the one frame, mid-fight.
            var seen=new List<(float t,float d)>();rig.FrameEncounter(far);float began=Time.unscaledTime;
            while(Time.unscaledTime-began<3){seen.Add((Time.unscaledTime-began,Back()));yield return null;}
            float settled=Back();float early=seen.Where(s=>s.t<=.15f).Select(s=>s.d).DefaultIfEmpty(before).Last();
            float Fastest(List<(float t,float d)> list)
            {
                float top=0;
                // Over a quarter second: a frame's jitter is a read's error, not the camera's speed.
                for(int a=0;a<list.Count;a++)for(int b=a+1;b<list.Count;b++){float dt=list[b].t-list[a].t;if(dt<.25f)continue;top=Mathf.Max(top,Mathf.Abs(list[b].d-list[a].d)/dt);break;}
                return top;
            }
            float outward=Fastest(seen);
            TestContext.WriteLine($"STEP_OUT before={before:0.0} early={early:0.0} settled={settled:0.0} top={outward:0.0}");
            Assert.Greater(settled,before+25,"fixture: the step asks for a much wider view");
            Assert.LessOrEqual((early-before)/(settled-before),.25f,$"the view is {(early-before)/(settled-before):P0} of the way back within .15 s of a new target mid-fight: it jumps zoom, as it used to at the first target");
            Assert.LessOrEqual(outward,56,$"the view draws back at {outward:0} m/s: at that speed a player can't follow what's on screen");
            seen.Clear();rig.FrameEncounter(near);began=Time.unscaledTime;
            while(Time.unscaledTime-began<2){seen.Add((Time.unscaledTime-began,Back()));yield return null;}
            float inward=Fastest(seen);
            TestContext.WriteLine($"STEP_IN top={inward:0.0}");
            Assert.LessOrEqual(inward,31,$"with the fight still on the view snaps in at {inward:0} m/s, straight after whipping out");
            party.enabled=true;
        }
    }
}
