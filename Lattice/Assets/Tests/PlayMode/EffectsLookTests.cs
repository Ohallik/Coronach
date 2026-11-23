using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The shared effects as a player sees them (the whole-game capture of November 20, s06 to s63): a struck body keeps its form and is gone again in the blow's own
    /// time, whatever stop the blow brought; a guardian's plate is a see-through shield with a lit rim, not a slab of lime; a swarm's motes are something to see. Drawn through
    /// the game's own shader and pipeline, measured from the pixels, and saved as stills under Builds/quality/focused/effects-look for the lead to open.</summary>
    public sealed class EffectsLookTests
    {
        static CombatActor Sela=>PartyController.Current.members.First(m=>m.character=="Sela");
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();ImpactSettings.Reset();ImpactSettings.Volatile=true;
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");yield return Zone("Arena_Ground");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();ImpactSettings.Reset();
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+15;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.7f);
        }
        static void Arena()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Effects fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            if(actor.motor is GroundMotor ground)ground.KeepFacing(Vector3.forward);
            Physics.SyncTransforms();
        }
        static CombatActor Lead(string hero)
        {
            var party=PartyController.Current;if(party.Active.character!=hero)Assert.IsTrue(party.Swap(),"fixture: the swap");
            foreach(var member in party.members){member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;}
            return party.Active;
        }
        static EnemyBrain Enemy(string id,Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;return enemy;}
        /// <summary>A hound that never dies or breaks, to which every type is plain.</summary>
        static EnemyBrain Dummy(Vector3 at)
        {
            var enemy=Enemy("Ridgehound",at);var body=enemy.Health;body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;
            body.weakness=body.resistance=DamageType.Kinetic;return enemy;
        }
        static DamagePacket Blow(CombatActor from,HitWeight weight,float amount=10)=>new DamagePacket{source=from.Health,amount=amount,type=DamageType.Plasma,weight=weight};
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Settled(){for(float end=Time.unscaledTime+2;GameTime.Stopped&&Time.unscaledTime<end;)yield return null;}

        // ------------------------------------------------------------------ drawing and reading back
        const int Layer=31;
        /// <summary>A camera of the game's own ground framing (pitch 40, yaw 20, 30 degrees, 19.5 m) or nearer, which draws only the fixture's layer over a flat colour.</summary>
        static Camera Eye(Vector3 look,float distance,Color ground)
        {
            var camera=new GameObject("Effects fixture camera").AddComponent<Camera>();camera.enabled=false;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=ground;camera.cullingMask=1<<Layer;camera.fieldOfView=30;camera.nearClipPlane=.3f;camera.farClipPlane=200;
            var turn=Quaternion.Euler(40,20,0);camera.transform.SetPositionAndRotation(look-turn*Vector3.forward*distance,turn);
            return camera;
        }
        static void OnLayer(GameObject body,int layer=Layer){foreach(var t in body.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;}
        static Color[] Shot(Camera camera,int w,int h,string still=null)
        {
            var rt=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32);Assert.IsTrue(rt.Create());var active=RenderTexture.active;
            try
            {
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(w,h,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();var pixels=image.GetPixels();
                if(still!=null)
                {
                    string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/focused/effects-look"));System.IO.Directory.CreateDirectory(folder);
                    string file=System.IO.Path.Combine(folder,still+".png");System.IO.File.WriteAllBytes(file,image.EncodeToPNG());TestContext.WriteLine("EFFECTS_LOOK_STILL "+file);
                }
                Object.Destroy(image);return pixels;
            }
            finally{camera.targetTexture=null;RenderTexture.active=active;Object.Destroy(rt);}
        }
        static float Luma(Color c)=>.299f*c.r+.587f*c.g+.114f*c.b;
        static float Gap(Color a,Color b)=>Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Max(Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b)));
        static float Spread(IEnumerable<float> values){var v=values.ToArray();if(v.Length==0)return 0;float mean=v.Average();return Mathf.Sqrt(v.Select(x=>(x-mean)*(x-mean)).Average());}

        // ------------------------------------------------------------------ the hit, the hurt and the armour flash
        /// <summary>A struck body keeps its form. The flash used to lerp every pixel of the model toward its colour by up to nine tenths, before the fog and over the lighting, so a
        /// boss in it was one flat colour with no shading: the Burrower and the Bellows white blobs, Sela and Taren solid red, guardians flat pale blue (s06/031, s08/082, s06/037,
        /// s08/056, s46/135), read by four reviewers as a missing material. A lit ball of the world's own shader is drawn at rest and at each flash's full strength (a finisher's
        /// hit, a heavy hurt, a heavy blow on armour): flashed, its light still varies over it as much as six tenths of what it did at rest, under a third of it is the flash's
        /// own colour flat, it is plainly brighter, and it brightens most at its silhouette (the blow burns along its edge).</summary>
        [UnityTest] public IEnumerator AStruckBodyKeepsItsShadingAndBurnsAtItsEdgeNeverAFlatFill()
        {
            var at=new Vector3(600,60,600);
            var ball=GameObject.CreatePrimitive(PrimitiveType.Sphere);Object.Destroy(ball.GetComponent<Collider>());ball.name="Flash fixture body";ball.transform.position=at;ball.transform.localScale=Vector3.one*2.2f;
            var shader=Shader.Find("Lattice/Toon");Assert.IsNotNull(shader,"fixture: the world's shader");
            var material=new Material(shader){name="flash fixture"};material.SetColor("_BaseColor",new Color(.55f,.5f,.46f,1));
            var view=ball.GetComponent<Renderer>();view.sharedMaterial=material;OnLayer(ball);
            var camera=Eye(at,8,Color.black);yield return Frames(2);
            const int W=192,H=192;
            var failures=new List<string>();
            foreach(var (name,colour,amount) in new[]{("hit",Impact.Tune.HitFlash,Impact.Tune.Flash[(int)HitWeight.Finisher]),("hurt",Impact.Tune.HurtFlash,Impact.Tune.Flash[(int)HitWeight.Heavy]),
                ("armour",Impact.Tune.ArmourFlash,Impact.Tune.Flash[(int)HitWeight.Heavy])})
            {
                var block=new MaterialPropertyBlock();
                block.SetColor("_HitFlash",new Color(colour.r,colour.g,colour.b,0));view.SetPropertyBlock(block);var rest=Shot(camera,W,H,"flash-"+name+"-rest");
                block.SetColor("_HitFlash",new Color(colour.r,colour.g,colour.b,amount));view.SetPropertyBlock(block);var struck=Shot(camera,W,H,"flash-"+name+"-struck");
                view.SetPropertyBlock(null);
                var body=Enumerable.Range(0,W*H).Where(i=>Luma(rest[i])>.02f||Luma(struck[i])>.02f).ToArray();
                Assert.Greater(body.Length,W*H/8,"fixture: the ball fills the frame");
                // Its middle and its rim, by distance from where it stands in the frame.
                float cx=(float)body.Average(i=>i%W),cy=(float)body.Average(i=>i/W),reach=body.Max(i=>Mathf.Sqrt((i%W-cx)*(i%W-cx)+(i/W-cy)*(i/W-cy)));
                float Out(int i)=>Mathf.Sqrt((i%W-cx)*(i%W-cx)+(i/W-cy)*(i/W-cy))/reach;
                float form=Spread(body.Select(i=>Luma(rest[i]))),kept=Spread(body.Select(i=>Luma(struck[i])));
                // A fill is one value over the whole body: the share of it within a fiftieth of the struck body's middle value.
                var lumas=body.Select(i=>Luma(struck[i])).OrderBy(v=>v).ToArray();float median=lumas[lumas.Length/2];
                float filled=body.Count(i=>Mathf.Abs(Luma(struck[i])-median)<.02f)/(float)body.Length;
                float lift=body.Average(i=>Luma(struck[i])-Luma(rest[i]));
                float rim=body.Where(i=>Out(i)>.78f).Average(i=>Luma(struck[i])-Luma(rest[i])),middle=body.Where(i=>Out(i)<.45f).Average(i=>Luma(struck[i])-Luma(rest[i]));
                string line=$"EFFECTS_FLASH {name} at={amount:0.00} shading={form:0.000}->{kept:0.000} flat={filled:0.00} lift={lift:0.000} rim={rim:0.000} middle={middle:0.000}";
                TestContext.WriteLine(line);Debug.Log(line);
                if(kept<form*.6f)failures.Add(line+": the flash takes the body's shading away (a flat fill)");
                if(filled>.6f)failures.Add(line+": six tenths of the body or more is one flat value");
                if(lift<.04f)failures.Add(line+": the flash can't be seen");
                if(rim<=middle)failures.Add(line+": the flash doesn't burn at the body's edge");
            }
            Object.Destroy(camera.gameObject);Object.Destroy(ball);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        /// <summary>A blow's flash lasts the blow's own time, whatever stop it came with, and a fight is rarely caught in one. It used to hold at full through every stop: a heavy
        /// blow's 70 ms, a break's 120, a boss going down's 250 (the Bellows and Mother of Doors white over a third of the screen for ten frames, s08/086, s63/149), and stops
        /// take up to 15% of a fight, so a still caught a boss flashed about one time in five. Here one heavy blow from the held hero lands with a boss-down beat after it, and
        /// the flash is gone within its own .09 s and a frame or two; then a hound is struck at the replay's own pace (s08's Bellows fight: about 72 flashes a minute) for eight
        /// seconds, and a quarter strength or more is on it under 6% of the time.</summary>
        [UnityTest] public IEnumerator AFlashLastsItsBlowsOwnTimeWhateverStopItCameWithAndAFightIsRarelyCaughtInOne()
        {
            Arena();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(204,0,0));
            var dummy=Dummy(new Vector3(200,0,3));yield return Frames(3);
            var flash=dummy.GetComponent<HitFlash>();Assert.IsNotNull(flash,"fixture: the body has a HitFlash");
            dummy.Health.Receive(Blow(taren,HitWeight.Heavy));Impact.Current.Hold(Impact.Tune.BossKill.stop,true);
            Assert.IsTrue(GameTime.Stopped,"fixture: the blow and the beat after it hold the fight");
            float shown=0;
            for(float end=Time.unscaledTime+1;Time.unscaledTime<end;){float before=Time.unscaledTime;yield return null;if(flash.Shown>.05f)shown+=Time.unscaledTime-before;}
            string first=$"EFFECTS_FLASH_HELD seconds={shown:0.000} stop={Impact.Tune.BossKill.stop:0.000} own={Impact.Tune.FlashSeconds:0.000}";TestContext.WriteLine(first);Debug.Log(first);
            Assert.LessOrEqual(shown,Impact.Tune.FlashSeconds+.05f,first+": the flash is held through the stop the blow came with");
            yield return Settled();yield return new WaitForSecondsRealtime(.3f);
            float caught=0,total=0,next=Time.unscaledTime;int hits=0;
            for(float end=Time.unscaledTime+8;Time.unscaledTime<end;)
            {
                if(Time.unscaledTime>=next){dummy.Health.Receive(Blow(taren,HitWeight.Heavy));hits++;next+=60f/72;}
                float before=Time.unscaledTime;yield return null;float dt=Time.unscaledTime-before;total+=dt;if(flash.Shown>=.25f)caught+=dt;
            }
            string pace=$"EFFECTS_FLASH_SHARE hits={hits} seconds={total:0.00} flashed={caught:0.000} share={caught/total:0.000}";TestContext.WriteLine(pace);Debug.Log(pace);
            Assert.GreaterOrEqual(hits,9,"fixture: the hound was struck at the replay's pace");
            Assert.LessOrEqual(caught/total,.06f,pace+": a still would catch the body flashed too often");
        }

        /// <summary>A guardian's plate reads as a shield: see-through in its middle, lit along its rim, coloured and never blown out. It drew as a flat, unlit, half-opaque slab of
        /// lime over the party panel's corner (s46/047, s47/077, s48/216): the strip's two faces shared their vertices, so its normals summed to nothing and the shader read the
        /// whole of it as rim. A Muster Pikeguard's plate is drawn alone at the game's framing over the Scoria's dark ground: where its middle stands the ground still shows (it
        /// changes the pixel by under .3), its top edge is drawn at least twice as strongly as its middle, and under a tenth of it is at full value in any channel.</summary>
        [UnityTest] public IEnumerator AGuardiansPlateIsASeeThroughShieldWithALitRim()
        {
            Arena();var guard=Enemy("MusterPikeguard",new Vector3(230,0,10));guard.transform.rotation=Quaternion.Euler(0,200,0);yield return Frames(4);
            var plate=guard.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r=>r.name=="Front plate");Assert.IsNotNull(plate,"fixture: the Pikeguard stands its plate");
            // Drawn here and now, before its own Present can lower it again.
            plate.gameObject.SetActive(true);
            var parts=guard.GetComponentsInChildren<Transform>(true).ToDictionary(t=>t,t=>t.gameObject.layer);plate.gameObject.layer=Layer;
            var ground=new Color(.24f,.2f,.18f);var bounds=plate.bounds;var camera=Eye(bounds.center,9,ground);
            const int W=320,H=240;var with=Shot(camera,W,H,"plate-pikeguard");
            // What the camera draws with nothing in it (the clear colour as this pipeline writes it), against which the plate is read.
            plate.gameObject.layer=0;var bare=Shot(camera,W,H);plate.gameObject.layer=Layer;
            foreach(var (t,layer) in parts)if(t!=null)t.gameObject.layer=layer;Object.Destroy(camera.gameObject);
            // Where the plate is in the frame, and its top edge and middle by height within it.
            var drawn=Enumerable.Range(0,W*H).Where(i=>Gap(with[i],bare[i])>.03f).ToArray();
            Assert.Greater(drawn.Length,300,"fixture: the plate is in the frame");
            int top=drawn.Max(i=>i/W),bottom=drawn.Min(i=>i/W);float span=Mathf.Max(1,top-bottom);
            float Up(int i)=>(i/W-bottom)/span;
            var middle=drawn.Where(i=>Up(i)>.35f&&Up(i)<.65f).ToArray();var rim=drawn.Where(i=>Up(i)>.9f).ToArray();
            float through=middle.Length>0?middle.Average(i=>Gap(with[i],bare[i])):1,edge=rim.Length>0?rim.Average(i=>Gap(with[i],bare[i])):0;
            float blown=drawn.Count(i=>with[i].r>.98f||with[i].g>.98f||with[i].b>.98f)/(float)drawn.Length;
            string line=$"EFFECTS_PLATE pixels={drawn.Length} middle={through:0.000} rim={edge:0.000} blown={blown:0.00}";TestContext.WriteLine(line);Debug.Log(line);
            var failures=new List<string>();
            if(through>=.3f)failures.Add(line+": its middle hides the ground (a slab, not a see-through shield)");
            if(edge<through*2)failures.Add(line+": its rim isn't drawn more strongly than its middle");
            if(blown>.1f)failures.Add(line+": it is blown out to full value");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        /// <summary>Stills for the lead, from the game's own camera: a hound at rest and at a finisher's full flash in the arena, and the Frozen Host's first vent pocket where the
        /// whole-game capture stood (s35/097: Taren at (-25.4, 103.9), the vent at (-30, 104)) with the whiteout closed over it. Under Builds/quality/focused/effects-look. What is
        /// measured of them is measured above and in EffectsBuildTests; this asks only that the vent's steam is there to see.</summary>
        [UnityTest] public IEnumerator StillsOfAStruckHoundAndTheVentPocketsSteamFromTheGamesCamera()
        {
            Arena();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(202.5f,0,-1));
            var hound=Dummy(new Vector3(201,0,3));yield return new WaitForSecondsRealtime(.6f);
            Game("hound-rest");
            hound.GetComponent<HitFlash>().Struck(HitWeight.Finisher,Vector3.forward,Impact.Tune.HitFlash,false,1,0);yield return null;
            GameTime.Paused=true;yield return null;Game("hound-struck");GameTime.Paused=false;
            yield return Settled();
            yield return Zone("FrozenHost");
            var lead=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=lead);
            Place(lead,Ground(-25.4f,103.9f));Place(partner,Ground(-23.5f,101.5f));
            var steam=Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Where(p=>p.name.StartsWith("Vent")&&Vector3.Distance(p.transform.position,new Vector3(-30,0,104))<9).ToArray();
            Assert.IsNotEmpty(steam,"fixture: the first vent pocket's steam");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(steam.Any(p=>p.particleCount>0),"the first vent pocket gives off no steam");
            Game("vent-pocket-calm");
            // What the zone's next rebuild gives it: ZoneKit.Drift thins a drift that stacks over ParticleSprites.MostStack (.6) on a line of sight, and each vent column of
            // ninety puffs at .36 stacked 1.46 (DRIFT_THINNED: .36 -> .15). The same here by hand, for the still.
            foreach(var column in steam.Where(p=>p.name.StartsWith("Vent steam")))
            {var main=column.main;var tone=main.startColor.color;tone.a=.36f*.6f/1.46f;main.startColor=tone;column.Clear();column.Simulate(5,true,true);column.Play();}
            yield return new WaitForSecondsRealtime(.5f);Game("vent-pocket-thinned");
            var weather=Lattice.World.Whiteout.Current;
            if(weather!=null){weather.Begin();for(float end=Time.unscaledTime+9;weather.Now!=Lattice.World.Whiteout.Phase.Closed&&Time.unscaledTime<end;)yield return null;yield return new WaitForSecondsRealtime(.5f);Game("vent-pocket-whiteout");}
        }
        static Vector3 Ground(float x,float z)
        {
            var hits=Physics.RaycastAll(new Vector3(x,60,z),Vector3.down,140,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).ToArray();
            Assert.IsNotEmpty(hits,$"fixture: there is ground under ({x}, {z})");return hits[0].point+Vector3.up*.05f;
        }
        /// <summary>The game's own camera, drawn at 1920 by 1080 into a still (the world, its grade and bloom; the HUD is an overlay and isn't in it).</summary>
        static void Game(string still)
        {
            var camera=Camera.main;Assert.IsNotNull(camera,"fixture: the game has its camera");var target=camera.targetTexture;
            var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);Assert.IsTrue(rt.Create());var active=RenderTexture.active;
            try
            {
                camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/focused/effects-look"));System.IO.Directory.CreateDirectory(folder);
                string file=System.IO.Path.Combine(folder,still+".jpg");System.IO.File.WriteAllBytes(file,image.EncodeToJPG(90));TestContext.WriteLine("EFFECTS_LOOK_STILL "+file);Object.Destroy(image);
            }
            finally{camera.targetTexture=target;RenderTexture.active=active;Object.Destroy(rt);}
        }

        // ------------------------------------------------------------------ how a beaten boss's body ends
        /// <summary>Puts a body at nought now, whatever holds its bar up (a plate, a held line, a shield).</summary>
        static void Beat(EnemyBrain body,CombatActor by)
        {
            var health=body.Health;health.Damageable=true;health.InvulnerableUntil=0;health.ShieldUntil=0;health.Plate=null;health.Holds=null;
            health.Receive(new DamagePacket{source=by.Health,amount=1e7f,type=DamageType.Kinetic});
        }
        /// <summary>Bosses (and the Compact Warden) who survive their fight on a humanoid rig: driven off on their feet (BossDeparture) or giving up where they stand (Ending.Yields).
        /// Dessik Varn yields too, but in a hired suit on a rig of its own, which kneels: it has no pelvis and head to read.</summary>
        static IEnumerable<string> Survivors()=>new[]{"VessaKorl","CompactWarden","HessaKorl","DraskVaal","LastWarden","MeretWell"};
        /// <summary>A boss beaten and not killed stays on its feet from the moment its bar empties until it is gone: its pelvis at six tenths of its standing height or more and its back
        /// leaning under sixty degrees (bowed or buckled is beaten; under that it is lying), its lower ankle no more than .3 m over where it stood and never .2 m under it. Vessa Korl lay sprawled with one leg in the air while the story said she was driven out (s48/232): her rig played its Down, and
        /// BossDeparture slid her out lying. Only what dies falls. Shown to fail first on the Compact Warden with its leaving taken away (it then dies, and falls).</summary>
        [UnityTest] public IEnumerator ABossThatSurvivesItsFightStaysOnItsFeetUntilItIsGone([ValueSource(nameof(Survivors))] string id)
        {
            LogAssert.ignoreFailingMessages=true;// a boss away from its own zone may miss its room's fixtures; this watches its body only
            Arena();var taren=Lead("Taren");Place(taren,new Vector3(200,0,-14));Place(Sela,new Vector3(196,0,-16));
            float floor=0;
            IEnumerator Watch(EnemyBrain body,List<string> faults,string label)
            {
                var rig=body.GetComponentsInChildren<Animator>().FirstOrDefault(a=>a.isHuman);Assert.IsNotNull(rig,"fixture: "+label+" stands on a humanoid rig");
                Transform B(HumanBodyBones bone)=>rig.GetBoneTransform(bone);var head=B(HumanBodyBones.Head);var hips=B(HumanBodyBones.Hips);
                var feet=new[]{B(HumanBodyBones.LeftFoot),B(HumanBodyBones.RightFoot)}.Where(f=>f!=null).ToArray();
                // At rest first: how far its head stands over its pelvis, and its lower ankle over the floor (a foot's bone is a hand above the sole).
                float standing=head.position.y-hips.position.y,ankle=feet.Min(f=>f.position.y)-floor,scale=body.transform.lossyScale.y,pelvis=Mathf.Max(.1f,hips.position.y-floor);
                Beat(body,taren);Assert.IsFalse(body.Health.Alive,"fixture: "+label+" is at nought");
                float worstUp=float.MaxValue,worstFoot=0,worstUnder=0,worstPelvis=float.MaxValue,worstLean=0;int frames=0;
                // Until it is gone, or while it is still half its size: one that walks out of sight shrinks there, and is read until then.
                for(float end=Time.unscaledTime+6;Time.unscaledTime<end&&body!=null&&rig!=null&&head!=null;)
                {
                    yield return null;if(body==null||rig==null||head==null||!rig.gameObject.activeInHierarchy||body.transform.lossyScale.y<scale*.5f)break;frames++;
                    // Read at the size it is drawn now: one walking out of sight shrinks as it goes.
                    float now=Mathf.Max(.05f,body.transform.lossyScale.y/scale);
                    worstUp=Mathf.Min(worstUp,(head.position.y-hips.position.y)/Mathf.Max(.05f,standing*now));
                    // Lying is a pelvis down near the floor and a back laid over; bowed, buckled or walking it stays most of its standing height and its back leans under sixty degrees.
                    worstPelvis=Mathf.Min(worstPelvis,(hips.position.y-floor)/(pelvis*now));worstLean=Mathf.Max(worstLean,Vector3.Angle(head.position-hips.position,Vector3.up));
                    float lowest=feet.Min(f=>f.position.y)-floor;worstFoot=Mathf.Max(worstFoot,lowest-ankle*now);worstUnder=Mathf.Max(worstUnder,ankle*now-lowest);
                }
                string line=$"EFFECTS_SURVIVOR {label} frames={frames} head-over-pelvis={worstUp:0.00} pelvis-height={worstPelvis:0.00} (of standing) back-lean={worstLean:0}deg lower-ankle-raised={worstFoot:0.00} m sunk={worstUnder:0.00} m";
                TestContext.WriteLine(line);Debug.Log(line);
                if(worstPelvis<.6f||worstLean>60)faults.Add(line+": it went down (its pelvis to the floor, or its back laid over)");
                if(worstFoot>.3f)faults.Add(line+": both its feet left the floor");
                if(worstUnder>.2f)faults.Add(line+": it sank through the floor");
            }
            // The fault first, once: an ordinary soldier on a rig that has a Down of its own, killed, falls, and the check says so.
            if(id=="CompactWarden")
            {
                EnemyBrain dying=null;
                foreach(var soldier in new[]{"MusterScav","MusterSergeant","MusterPikeguard","ThawedGuard"})
                {
                    if(GameCatalog.Find<EnemyDef>(soldier)==null)continue;var tried=Enemy(soldier,new Vector3(214,0,8));yield return new WaitForSecondsRealtime(.6f);
                    var rig=tried.GetComponentsInChildren<Animator>().FirstOrDefault(a=>a.isHuman);
                    if(rig!=null&&rig.HasState(0,Animator.StringToHash("Down"))&&!tried.TryGetComponent<BossDeparture>(out _)){dying=tried;break;}
                    Object.Destroy(tried.gameObject);yield return null;
                }
                Assert.IsNotNull(dying,"fixture: an ordinary soldier whose rig lies down");
                var seen=new List<string>();yield return Watch(dying,seen,dying.definition.id+" (dying)");
                Assert.IsNotEmpty(seen,"the check doesn't see a body that falls down");
                if(dying!=null)Object.Destroy(dying.gameObject);yield return new WaitForSecondsRealtime(.3f);
            }
            var boss=Enemy(id,new Vector3(230,0,8));yield return new WaitForSecondsRealtime(1.2f);
            var faults=new List<string>();yield return Watch(boss,faults,id);
            LogAssert.ignoreFailingMessages=false;
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
        /// <summary>Huge bosses that die, and machines that shut down: a body as big as a room.</summary>
        static IEnumerable<string> HugeEnds()=>new[]{"Rotbloom","SlagColossus","FoundrySaint","HostMarshal","Tollkeeper"};
        /// <summary>A huge boss's body ends where it stands. Tipped over by the shared 42 degrees like an ordinary body, its top swung metres across the floor the party stood on, and
        /// the Rotbloom stood on whole with the heroes inside it (s26/457). From its death to its clearing up, the middle of what is drawn moves no further across the floor than a fifth
        /// of its own reach (and a metre at least), and what is destroyed rather than shut down comes down to under seven tenths of its height. Shown to fail first on the same body
        /// tipped by the old roll by hand.</summary>
        [UnityTest] public IEnumerator AHugeBossEndsWhereItStands([ValueSource(nameof(HugeEnds))] string id)
        {
            LogAssert.ignoreFailingMessages=true;// a boss away from its own zone may miss its room's fixtures; this watches its body only
            Arena();var taren=Lead("Taren");Place(taren,new Vector3(200,0,-20));Place(Sela,new Vector3(196,0,-22));
            var boss=Enemy(id,new Vector3(232,0,10));yield return new WaitForSecondsRealtime(1.2f);
            Bounds Drawn(){var b=new Bounds(boss.transform.position,Vector3.zero);bool any=false;foreach(var r in boss.GetComponentsInChildren<Renderer>())if(r.enabled&&r.gameObject.activeInHierarchy&&!(r is ParticleSystemRenderer)){if(!any){b=r.bounds;any=true;}else b.Encapsulate(r.bounds);}return b;}
            var rest=Drawn();float reach=Mathf.Max(rest.extents.x,rest.extents.z),allowed=Mathf.Max(1,reach*.2f);
            var presentation=boss.GetComponent<DefeatPresentation>();var visual=presentation!=null?presentation.visual:null;Assert.IsNotNull(visual,"fixture: "+id+" has a body to end");
            // The fault first: the old roll, laid on this body by hand, carries its middle further than the check allows.
            var before=visual.localRotation;visual.localRotation=before*Quaternion.Euler(8,0,42);
            float rolled=Vector2.Distance(new Vector2(Drawn().center.x,Drawn().center.z),new Vector2(rest.center.x,rest.center.z));visual.localRotation=before;
            Assert.Greater(rolled,allowed,$"the check doesn't see the old roll carry {id} across the floor (it moved {rolled:0.00} m)");
            var ending=BossController.EndingOf(id);
            Beat(boss,taren);Assert.IsFalse(boss.Health.Alive,"fixture: "+id+" is at nought");
            float drift=0,lowest=rest.max.y;
            // Until it begins to be cleared away (its body shrinks to nothing in the last .45 s): the 1.5 s of its ending and the hold after.
            for(float end=Time.unscaledTime+2.9f;Time.unscaledTime<end&&boss!=null;)
            {
                yield return null;if(boss==null)break;var now=Drawn();
                drift=Mathf.Max(drift,Vector2.Distance(new Vector2(now.center.x,now.center.z),new Vector2(rest.center.x,rest.center.z)));lowest=Mathf.Min(lowest,now.max.y);
            }
            float height=Mathf.Max(.1f,rest.max.y-rest.min.y),down=(rest.max.y-lowest)/height;
            string line=$"EFFECTS_HUGE_END {id} ending={ending} reach={reach:0.0} drift={drift:0.00} allowed={allowed:0.00} old-roll={rolled:0.00} came-down={down:0.00}";TestContext.WriteLine(line);Debug.Log(line);
            LogAssert.ignoreFailingMessages=false;
            var faults=new List<string>();
            if(drift>allowed)faults.Add(line+": its body was carried across the floor as it ended");
            if(ending==BossController.Ending.Destroyed&&down<.3f)faults.Add(line+": destroyed, it still stands near its full height");
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        // ------------------------------------------------------------------ a swarm, from the game's camera, on the floor it lives on
        /// <summary>Each enemy drawn as motes, on each floor the whole-game capture fought it on (the lead's place at the kill: s20, s21, s23, s26, s35), as "id@zone@x,y,z".</summary>
        static IEnumerable<string> SwarmFloors()=>new[]{"MireflySwarm@BraidedRiver@53.2,0.7,122.3","MireflySwarm@WalkingCanopy@2.0,-2.72,11.5","SporeSwarm@SporeVeil@153.6,1.0,866.3",
            "MireLeeches@DrownedWaystation@15.1,-0.17,87.8","MireLeeches@DrownedWaystation@-5.5,-3.92,239.7","FrostMotes@FrozenHost@4.9,0.07,113.6","FrostMotes@FrozenHost@38.2,-3.92,184.6"};
        /// <summary>A swarm is plainly seen from the game's camera on every floor it lives on: the Mirefly Swarm on the Braided River's banks and the Walking Canopy's decks, the Spore
        /// Swarm in the Spore Veil's lanes, the Mire Leeches on the Drowned Waystation's water and its lower gallery, the Frost Motes on the White Reach's snow and the dig's floor.
        /// Its motes were the shared flare, a spark whose bright core is under a hundredth of its square: two or three pixels each from the play camera, in the editor as in the
        /// player (the Mirefly Swarm in s20/022, the Frost Motes in s35/101). The lead stands where the capture's lead stood at the kill and the swarm five metres ahead of
        /// them; drawn as the game draws it (its pipeline, grade and bloom) the swarm changes at least 2,400 pixels of a 1920 by 1080 frame, 900 of them strongly, as the Oriel
        /// engineer's Fibre Wisp check asks of a body. Its motes drawn with the old flare and no shadow are shown to fall short of that first, on the same floor.</summary>
        [UnityTest] public IEnumerator ASwarmIsPlainlySeenFromTheGamesCameraOnEachFloorItLivesOn([ValueSource(nameof(SwarmFloors))] string floor)
        {
            var part=floor.Split('@');string id=part[0],zone=part[1];var xyz=part[2].Split(',').Select(v=>float.Parse(v,System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            // No fight of the zone's own wakes as the party is set down inside its box (its spawner would reach for the bodies taken away above).
            foreach(var volume in Object.FindObjectsByType<Lattice.World.EncounterVolume>(FindObjectsSortMode.None))foreach(var box in volume.GetComponents<Collider>())box.enabled=false;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.5f);
            bool flight=ZoneController.Current!=null&&ZoneController.Current.Flight;
            Vector3 On(float x,float z)
            {
                if(flight)return new Vector3(x,xyz[1],z);
                var hits=Physics.RaycastAll(new Vector3(x,xyz[1]+2.5f,z),Vector3.down,8,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<CombatActor>()==null).OrderBy(h=>h.distance).ToArray();
                return hits.Length>0?hits[0].point+Vector3.up*.05f:new Vector3(x,xyz[1],z);
            }
            var lead=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=lead);
            Place(lead,On(xyz[0],xyz[2]));Place(partner,On(xyz[0]+2.2f,xyz[2]-1.2f));
            var def=GameCatalog.Find<EnemyDef>(id);Assert.IsNotNull(def,"fixture: "+id);
            var swarm=ActorFactory.Enemy(def,On(xyz[0],xyz[2]+5)+(flight?Vector3.zero:Vector3.up*.05f));swarm.Passive=true;
            for(float end=Time.unscaledTime+1.6f;Time.unscaledTime<end;){lead.motor.Move(Vector2.zero,false,false);yield return null;}
            var motes=swarm.GetComponentInChildren<SwarmMotes>();Assert.IsNotNull(motes,"fixture: "+id+" is drawn as motes");
            var view=motes.GetComponentInChildren<ParticleSystemRenderer>();Assert.IsNotNull(view,"fixture: its motes are drawn");
            var shadow=motes.transform.Find("Swarm shadow");
            // The fault first: the same cloud with the old flare for its motes and no shadow under it.
            var drawn=view.sharedMaterial;bool laid=shadow!=null&&shadow.gameObject.activeSelf;
            view.sharedMaterial=Resources.Load<Material>("Effects/flare_01");if(shadow!=null)shadow.gameObject.SetActive(false);
            var (oldSeen,oldStrong)=Seen(swarm.gameObject,null);
            view.sharedMaterial=drawn;if(shadow!=null)shadow.gameObject.SetActive(laid);
            var (seen,strong)=Seen(swarm.gameObject,$"swarm-{id}-{zone}-{Mathf.RoundToInt(xyz[2])}");
            string line=$"EFFECTS_SWARM_SEEN {id} {zone} ({xyz[0]}, {xyz[2]}) pixels={seen} strong={strong} shadow={laid} old flare: pixels={oldSeen} strong={oldStrong}";TestContext.WriteLine(line);Debug.Log(line);
            Assert.IsTrue(oldSeen<2400||oldStrong<900,line+": the check doesn't see the old flare's motes as too small to find");
            Assert.IsTrue(seen>=2400&&strong>=900,line+": a swarm a player can find asks 2,400 and 900");
        }
        /// <summary>What a body adds to the game camera's frame at 1920 by 1080, as the game draws it: the frame with it, then twice without it (what changes by itself between two
        /// draws isn't counted), then with it again. How many pixels it changes at all (over 18 of 255) and strongly (over 60): the Oriel engineer's Fibre Wisp measure.</summary>
        static (int seen,int strong) Seen(GameObject body,string still)
        {
            const int W=1920,H=1080;var camera=Camera.main;Assert.IsNotNull(camera,"fixture: the game has its camera");
            var drawn=body.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
            var target=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);Assert.IsTrue(rt.Create());
            Color32[] Shot(string name)
            {
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(W,H,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();var p=image.GetPixels32();
                if(name!=null){string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/focused/effects-look"));System.IO.Directory.CreateDirectory(folder);
                    string file=System.IO.Path.Combine(folder,name+".jpg");System.IO.File.WriteAllBytes(file,image.EncodeToJPG(88));TestContext.WriteLine("EFFECTS_LOOK_STILL "+file);}
                Object.Destroy(image);return p;
            }
            void Hide(bool hidden){foreach(var r in drawn)if(r!=null)r.enabled=!hidden;}
            Color32[] with,without,again,withAgain;
            try{Shot(null);Shot(null);with=Shot(still);Hide(true);without=Shot(null);again=Shot(null);Hide(false);withAgain=Shot(null);}
            finally{Hide(false);camera.targetTexture=target;RenderTexture.active=active;Object.Destroy(rt);}
            static int Gap32(Color32 a,Color32 b)=>Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Max(Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b)));
            int seen=0,strong=0;
            for(int i=0;i<with.Length;i++)
            {
                if(Gap32(without[i],again[i])>4)continue;int gap=Mathf.Min(Gap32(with[i],without[i]),Gap32(withAgain[i],again[i]));
                if(gap>18)seen++;if(gap>60)strong++;
            }
            return (seen,strong);
        }

        /// <summary>A swarm drawn as motes is something to see on bright ground and dark. Its motes were the shared spark texture, a fifth of a metre each, whose bright core is under
        /// a hundredth of its square: two or three pixels each at the game's framing, in the editor as in the player (the Fibre Wisp not drawn at all on the Skyfields' stone,
        /// s39/153, s40/096; the Frost Motes a few flecks on snow, s35/101). The Frost Motes' cloud is drawn alone at the game's own distance over pale stone and
        /// over a dark deck, at 960 by 540: they change at least 600 pixels (2,400 at 1080p) on each.</summary>
        [UnityTest] public IEnumerator ASwarmsMotesAreSeenOnPaleGroundAndDark()
        {
            var at=new Vector3(600,60,600);var wisp=GameCatalog.Find<EnemyDef>("FrostMotes");Assert.IsNotNull(wisp,"fixture: the Frost Motes' definition");
            var go=new GameObject("Swarm fixture");go.transform.position=at;var cloud=go.AddComponent<SwarmMotes>();cloud.count=wisp.tune.count>0?wisp.tune.count:10;cloud.radius=wisp.capsuleRadius*1.6f;
            if(wisp.tune.tint.a>0)cloud.tone=wisp.tune.tint;
            yield return Frames(3);OnLayer(go);yield return Frames(2);
            var failures=new List<string>();
            foreach(var (name,ground) in new[]{("pale",new Color(.78f,.8f,.78f)),("dark",new Color(.13f,.14f,.16f))})
            {
                var camera=Eye(at,19.5f,ground);var seen=Shot(camera,960,540,"swarm-"+name);
                // Against the same camera's frame with the motes off its layer: the clear colour as this pipeline writes it.
                OnLayer(go,0);var bare=Shot(camera,960,540);OnLayer(go);Object.Destroy(camera.gameObject);
                int changed=Enumerable.Range(0,seen.Length).Count(i=>Gap(seen[i],bare[i])>18/255f);
                string line=$"EFFECTS_SWARM {name} changed={changed}";TestContext.WriteLine(line);Debug.Log(line);
                if(changed<600)failures.Add(line+": a swarm a player can find asks 600 at 960 by 540");
            }
            Object.Destroy(go);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
