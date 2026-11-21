using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 3's world mechanics, each on a bare floor in the proving yard with everything it needs made here: water a body wades, deep water that drains on a flag and stays drained,
    /// the wheel that sets that flag, water that climbs a hall and sets a caught party back on its last ledge, an anchor whose cutting swaps the doors, and a basin that drains as its gates break.
    /// None of them knows the river or the Canopy: later chapters' pumps, valves and couplings are the same parts.</summary>
    public sealed class XylosMechanicsTests
    {
        /// <summary>What the shared zone checks have to be told about the river and the Canopy.</summary>
        [ContentRegistry.Expectations] static void XylosWild(ContentRegistry.Book book)
        {
            // The Canopy's mooring roots stay shut until Teo has been heard at Marrow-3 (the anchor roots are counted from that talk on): a refused press names him.
            book.Lock("xylos.seedthaneGone","Teo");
        }
        const float X=230;
        readonly List<string> log=new(),cues=new();NavMeshDataInstance paths;
        void Heard(string text,string stack,LogType type){if(text.StartsWith("WATER_")||text.StartsWith("SLUICE_")||text.StartsWith("ANCHOR_")||text.StartsWith("FLOOD_")||text.StartsWith("GATE_")||text.StartsWith("BASIN_")||text.StartsWith("AFTER_")||text.StartsWith("POD_"))log.Add(text);}
        void Cue(string key,Transform emitter)=>cues.Add(key);
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");log.Clear();cues.Clear();Application.logMessageReceived+=Heard;AudioManager.CueStarted+=Cue;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;AudioManager.CueStarted-=Cue;if(paths.valid)paths.Remove();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static void Flag(string key){GameServices.Current.Flags.SetBool(key,true);}
        static bool Set(string key)=>GameServices.Current.Flags.GetBool(key);
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+15;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.4f);
        }
        static Vector3 At(float x,float y,float z)=>new(X+x,y,z);
        /// <summary>Open, level floor far from the yard's walls, its top at height zero, and paths over it to carve.</summary>
        void Floor()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Xylos fixture floor";floor.transform.position=new Vector3(X,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();
            var sources=new List<NavMeshBuildSource>{new(){shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.Translate(new Vector3(X,-.5f,0)),size=new Vector3(140,1,140),area=0}};
            var built=NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),sources,new Bounds(new Vector3(X,0,0),new Vector3(150,10,150)),Vector3.zero,Quaternion.identity);
            Assert.IsNotNull(built,"fixture: no paths over the fixture floor");paths=NavMesh.AddNavMeshData(built);
        }
        static bool Walkable(Vector3 at)=>NavMesh.SamplePosition(at,out _,.25f,NavMesh.AllAreas);
        static void Put(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;Physics.SyncTransforms();
        }
        static IEnumerator Walk(CombatActor actor,Vector2 direction,float seconds)
        {float until=Time.time+seconds;while(Time.time<until){actor.motor.Move(direction,false,false);yield return null;}}
        static IEnumerator Wait(float seconds){float until=Time.time+seconds;while(Time.time<until)yield return null;}
        static (CombatActor lead,CombatActor partner) Party(){var lead=PartyController.Current.Active;return (lead,PartyController.Current.members.First(m=>m!=lead));}
        /// <summary>A sheet of water over a rectangle of the floor. Made asleep and woken once it is whole, as a scene's would be.</summary>
        static WaterVolume Water(Vector3 centre,Vector2 size,float level,System.Action<WaterVolume,GameObject> more=null)
        {
            var go=new GameObject("Fixture water");go.SetActive(false);go.transform.position=centre;
            var reach=new GameObject("Reach").transform;reach.SetParent(go.transform,false);reach.localScale=new Vector3(size.x,1,size.y);
            var surface=GameObject.CreatePrimitive(PrimitiveType.Quad);surface.name="Surface";Object.DestroyImmediate(surface.GetComponent<Collider>());
            surface.transform.SetParent(go.transform,false);surface.transform.SetPositionAndRotation(new Vector3(centre.x,level,centre.z),Quaternion.Euler(90,0,0));surface.transform.localScale=new Vector3(size.x,size.y,1);
            var water=go.AddComponent<WaterVolume>();water.level=level;water.surface=surface.transform;water.reaches=new[]{reach};
            more?.Invoke(water,surface);
            go.SetActive(true);return water;
        }
        /// <summary>Something solid that also cuts the paths under it, as a deep channel's wall or a root door does.</summary>
        static GameObject Block(string name,Vector3 centre,Vector3 size,Transform parent=null)
        {
            var block=new GameObject(name,typeof(BoxCollider),typeof(NavMeshObstacle));if(parent!=null)block.transform.SetParent(parent,true);block.transform.position=centre;
            block.GetComponent<BoxCollider>().size=size;var carve=block.GetComponent<NavMeshObstacle>();carve.shape=NavMeshObstacleShape.Box;carve.size=size;carve.carving=true;carve.carveOnlyStationary=false;
            Physics.SyncTransforms();return block;
        }

        [UnityTest] public IEnumerator ShallowsSlowAHeroAndAnEnemyAndSplashOnTheWayIn()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-40,.05f,-40));
            Put(hero,At(0,.05f,-30));yield return Walk(hero,Vector2.up,.5f);
            float from=hero.transform.position.z;yield return Walk(hero,Vector2.up,1);float dry=hero.transform.position.z-from;
            Assert.Greater(dry,3,"fixture: the hero doesn't walk the fixture floor");
            Assert.AreEqual(1,hero.Health.Pace,.001f,"a hero on dry ground is already slowed");
            var water=Water(At(0,0,20),new Vector2(40,40),.5f);yield return null;
            Put(hero,At(0,.05f,4));yield return Walk(hero,Vector2.up,.5f);
            Assert.AreEqual(water.wadePace,hero.Health.Pace,.001f,"knee-deep water doesn't slow a hero");
            CollectionAssert.Contains(cues,water.splash,"stepping into the water made no splash");
            from=hero.transform.position.z;yield return Walk(hero,Vector2.up,1);float wet=hero.transform.position.z-from;
            Assert.Less(wet,dry*(water.wadePace+.12f),$"wading covered {wet:0.00} m against {dry:0.00} m on dry ground: that isn't slower");
            Assert.Greater(wet,dry*(water.wadePace-.2f),$"wading covered {wet:0.00} m against {dry:0.00} m on dry ground: the water is a wall");
            // A dodge is the way out, and it keeps its distance.
            var before=hero.transform.position;hero.motor.Dash(Vector3.forward,4);yield return Walk(hero,Vector2.up,.2f);
            Assert.Greater(hero.transform.position.z-before.z,3.2f,"a dash through shallows is cut short: there has to be a way out of anything laid in them");
            // The same water slows what chases through it. (A body comes after a hero inside 35 m: the hero stands 24 m off each time, on dry ground and then in the water.)
            Put(hero,At(20,.05f,-10));
            var hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("SentinelHusk"),At(20,.05f,-34));hound.Passive=false;
            yield return Wait(.8f);var start=hound.transform.position;yield return Wait(1.5f);float chase=Vector3.Distance(start,hound.transform.position);
            Assert.Greater(chase,1,$"fixture: the husk doesn't come after the hero (brain {(hound.enabled?"on":"off")}, passive {hound.Passive}, telling {hound.Telegraphing}, input blocked {GameServices.Current.Input.Blocked}, "+
                $"paused {GameTime.Paused}, safe pocket {ZoneController.Current!=null&&ZoneController.Current.InSafePocket}, capsule {(hound.GetComponent<CharacterController>().enabled?"on":"off")}, at {hound.transform.position}, hero at {hero.transform.position}, lead {PartyController.Current.Active.name})");
            Assert.AreEqual(1,hound.Health.Pace,.001f);
            hound.Passive=true;Put(hero,At(10,.05f,30));var body=hound.GetComponent<CharacterController>();body.enabled=false;hound.transform.position=At(10,.05f,6);body.enabled=true;Physics.SyncTransforms();hound.Passive=false;
            yield return Wait(.8f);start=hound.transform.position;yield return Wait(1.5f);float waded=Vector3.Distance(start,hound.transform.position);
            Assert.AreEqual(water.wadePace,hound.Health.Pace,.001f,"the water slows the hero and not what hunts him");
            Assert.Less(waded,chase*(water.wadePace+.15f),$"the husk covered {waded:0.00} m wading against {chase:0.00} m dry");
            hound.Passive=true;
            // Out of it, the claim lapses by itself; and a film of water is a splash, not a slog.
            Put(hero,At(0,.05f,-30));yield return Wait(.4f);Assert.AreEqual(1,hero.Health.Pace,.001f,"the water still holds a hero who has left it");
            water.MoveTo(.1f);Put(hero,At(0,.05f,20));yield return Wait(.4f);Assert.AreEqual(1,hero.Health.Pace,.001f,"ankle-deep water slows a hero");
            Assert.Less(WaterVolume.DepthAt(At(0,0,-30)),0,"dry ground reads as under water");Assert.Greater(WaterVolume.DepthAt(At(0,0,20)),0);
        }

        [UnityTest] public IEnumerator AShipOverWaterIsNotWading()
        {
            yield return Zone("Arena_Flight");var hero=PartyController.Current.Active;
            var water=Water(hero.transform.position,new Vector2(60,60),hero.transform.position.y+2);
            yield return Wait(.5f);
            Assert.Greater(water.Depth(hero.transform.position),1,"fixture: the ship isn't under the surface");
            foreach(var member in PartyController.Current.members)Assert.AreEqual(1,member.Health.Pace,.001f,member.character+"'s ship is wading");
        }

        [UnityTest] public IEnumerator DeepWaterStopsAWalkerUntilItsFlagThenDrainsAndStaysDrained()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-40,.05f,-40));
            GameObject wall=null,bed=null;
            var water=Water(At(0,0,10),new Vector2(30,8),1.1f,(w,surface)=>
            {
                wall=Block("Deep water",At(0,1,10),new Vector3(30,4,6),surface.transform);bed=new GameObject("Wet bed");bed.SetActive(false);
                w.drainFlag="test.channel";w.drainedLevel=.3f;w.drainSeconds=1.5f;w.deep=new[]{wall};w.revealed=new[]{bed};
            });
            yield return Wait(.6f);
            Assert.IsFalse(Walkable(At(0,0,10)),"the deep channel is still on the paths: a partner or a pack would walk into it");
            Put(hero,At(0,.05f,0));yield return Walk(hero,Vector2.up,2.5f);
            Assert.Less(hero.transform.position.z,7.5f,"the hero walked into deep water");
            Assert.IsFalse(water.Drained);Assert.AreEqual(1.1f,water.Level,.001f);Assert.IsFalse(bed.activeSelf,"the bed shows under standing water");
            Flag("test.channel");yield return null;yield return null;
            CollectionAssert.Contains(log,"WATER_DRAINING test.channel");
            Assert.IsTrue(wall.activeSelf,"the channel opened before the water had gone");
            yield return Wait(.7f);Assert.Less(water.Level,1.05f,"the water isn't going down");Assert.Greater(water.Level,.3f,"the water went in one step: nobody saw it drain");
            Assert.AreEqual(water.Level,water.surface.position.y,.001f,"the surface drawn isn't where the water is");
            yield return Wait(1.4f);
            CollectionAssert.Contains(log,"WATER_DRAINED test.channel");
            Assert.IsTrue(water.Drained);Assert.AreEqual(.3f,water.Level,.001f);Assert.IsFalse(wall.activeSelf,"the drained channel still stops a walker");Assert.IsTrue(bed.activeSelf,"nothing shows where the water was");
            yield return Wait(.5f);Assert.IsTrue(Walkable(At(0,0,10)),"the drained bed isn't on the paths");
            Put(hero,At(0,.05f,0));yield return Walk(hero,Vector2.up,2.2f);
            Assert.Greater(hero.transform.position.z,9,"the hero can't wade the drained channel");
            Put(hero,At(0,.05f,10));yield return Wait(.3f);Assert.AreEqual(water.wadePace,hero.Health.Pace,.001f,"what's left in the bed isn't waded");
            // The flag is the save: water that wakes with it set is already down, with nothing played.
            log.Clear();GameObject later=null;
            var loaded=Water(At(40,0,10),new Vector2(30,8),1.1f,(w,surface)=>{later=Block("Deep water",At(40,1,10),new Vector3(30,4,6),surface.transform);w.drainFlag="test.channel";w.drainedLevel=.3f;w.deep=new[]{later};});
            yield return null;yield return null;
            Assert.IsTrue(loaded.Drained,"a drained channel came back full on a load");Assert.AreEqual(.3f,loaded.Level,.001f);Assert.IsFalse(later.activeSelf);
            Assert.IsEmpty(log,"a loaded save drained its channel all over again: "+string.Join(", ",log));
        }

        [UnityTest] public IEnumerator ASluiceWheelDrainsItsChannelOnceAndALoadedSaveFindsItTurned()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-40,.05f,-40));Put(hero,At(0,.05f,0));
            var water=Water(At(0,0,10),new Vector2(30,8),1.1f,(w,surface)=>{w.drainFlag="test.sluice";w.drainedLevel=.3f;w.drainSeconds=1.2f;});
            FlowSwitch Sluice(Vector3 at)
            {
                var go=new GameObject("Fixture sluice");go.SetActive(false);go.transform.position=at;var wheel=new GameObject("Wheel").transform;wheel.SetParent(go.transform,false);
                var sluice=go.AddComponent<FlowSwitch>();sluice.flag="test.sluice";sluice.wheel=wheel;sluice.seconds=1;sluice.turns=1.25f;sluice.point=Work("Open the middle sluice",at+Vector3.back,"test.sluice");
                go.SetActive(true);return sluice;
            }
            var wheel=Sluice(At(2,0,2));yield return null;yield return null;
            Assert.IsTrue(wheel.point.Available,"fixture: the wheel isn't offered");Assert.IsFalse(wheel.Worked);
            Assert.AreEqual(0,Quaternion.Angle(Quaternion.identity,wheel.wheel.localRotation),.01f,"the wheel turned before anyone touched it");
            wheel.point.Interact();yield return null;yield return null;
            Assert.IsTrue(Set("test.sluice"),"working the wheel set nothing");
            CollectionAssert.Contains(log,"SLUICE_OPEN test.sluice");CollectionAssert.Contains(log,"WATER_DRAINING test.sluice");
            CollectionAssert.Contains(cues,wheel.sound,"the sluice opened without a sound");
            Assert.IsFalse(wheel.point.Available,"a spent wheel is still offered");
            yield return Wait(.5f);float angle=Quaternion.Angle(Quaternion.identity,wheel.wheel.localRotation);Assert.Greater(angle,10,"the wheel doesn't turn");
            yield return Wait(1.2f);
            Assert.AreEqual(Mathf.DeltaAngle(0,1.25f*360),Mathf.DeltaAngle(0,wheel.wheel.localEulerAngles.z),1,"the wheel didn't finish its turn");
            Assert.IsTrue(water.Drained,"the wheel turned and the water stayed");
            int opened=log.Count(l=>l.StartsWith("SLUICE_OPEN"));
            // A save that has the flag: the wheel is found turned, and says nothing.
            var found=Sluice(At(-4,0,2));yield return null;yield return null;
            Assert.AreEqual(Mathf.DeltaAngle(0,1.25f*360),Mathf.DeltaAngle(0,found.wheel.localEulerAngles.z),1,"a loaded save's wheel is back where it started");
            Assert.IsFalse(found.point.Available);Assert.AreEqual(opened,log.Count(l=>l.StartsWith("SLUICE_OPEN")),"a loaded save turned its wheel again");
        }

        [UnityTest] public IEnumerator AnAfterTalkWaitsForItsEventAndForTheFightAndIsHeardOnce()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-40,.05f,-40));Put(hero,At(0,.05f,0));
            int asked=0;void Count(string node,string speaker,System.Action<bool> done){if(node=="SelaRelayLit")asked++;}
            Npc.TalkRequested+=Count;
            var talk=new GameObject("Fixture after talk").AddComponent<AfterTalk>();talk.flag="test.event";talk.node="SelaRelayLit";talk.speaker="Sela";talk.heardFlag="test.heard";talk.delay=.6f;
            DialogueSystem.Current.AutoAdvance=true;
            yield return Wait(1);Assert.AreEqual(0,asked,"the party talks about something that hasn't happened");
            var husk=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("SentinelHusk"),At(6,.05f,6));husk.Passive=false;
            Flag("test.event");yield return Wait(.3f);Assert.AreEqual(0,asked,"the talk came on the instant of the event, over it");
            yield return Wait(1.2f);Assert.AreEqual(0,asked,"the party stopped to talk with something awake beside it");
            husk.Passive=true;float deadline=Time.unscaledTime+25;
            while(!Set("test.heard")&&Time.unscaledTime<deadline)yield return null;
            Npc.TalkRequested-=Count;
            Assert.AreEqual(1,asked,"the talk was asked for more than once");Assert.IsTrue(Set("test.heard"),"a talk heard through isn't remembered");CollectionAssert.Contains(log,"AFTER_TALK SelaRelayLit");
            deadline=Time.unscaledTime+10;while((DialogueSystem.Current.Running||GameInput.Current.Blocked)&&Time.unscaledTime<deadline)yield return null;
            yield return Wait(3);Assert.IsFalse(DialogueSystem.Current.Running,"a talk that was heard plays again");Assert.IsFalse(talk.enabled);
            // A zone that wakes with the talk already heard owes nothing.
            var owed=new GameObject("Fixture after talk").AddComponent<AfterTalk>();owed.flag="test.event";owed.node="SelaRelayLit";owed.heardFlag="test.heard";
            yield return Wait(1.5f);Assert.IsFalse(DialogueSystem.Current.Running,"a loaded save replays a talk it has heard");
        }
        [UnityTest] public IEnumerator AHungPodIsABodyUntilItFallsAndCoverOnceItHas()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-40,.05f,-40));Put(hero,At(0,.05f,-6));
            CoverPod Pod(string name,Vector3 at,string settledBy=null)
            {
                var go=new GameObject(name);go.SetActive(false);go.transform.position=at;
                var hung=new GameObject("Hanging");hung.transform.SetParent(go.transform,false);hung.transform.localPosition=Vector3.up*1.4f;
                var health=hung.AddComponent<Health>();health.id="SeedPod";health.maximum=health.integrity=200;health.breakThreshold=100000;hung.AddComponent<SphereCollider>().radius=.9f;hung.AddComponent<Hurtbox>().owner=health;
                var down=new GameObject("Fallen",typeof(BoxCollider),typeof(NavMeshObstacle));down.transform.SetParent(go.transform,false);down.transform.localPosition=Vector3.up*.7f;
                down.GetComponent<BoxCollider>().size=new Vector3(2.2f,1.4f,2.2f);var carve=down.GetComponent<NavMeshObstacle>();carve.shape=NavMeshObstacleShape.Box;carve.size=new Vector3(2.2f,1.4f,2.2f);carve.carving=true;carve.carveOnlyStationary=false;
                var pod=go.AddComponent<CoverPod>();pod.hanging=hung;pod.fallen=down;pod.settledBy=settledBy;
                go.SetActive(true);return pod;
            }
            var pod=Pod("Fixture pod",At(0,0,0),"TestThane");var other=Pod("Other pod",At(12,0,0),"TestThane");yield return null;yield return null;
            Vector3 from=At(0,1,-6),to=At(0,1,6);
            Assert.IsTrue(pod.Hanging);Assert.IsFalse(pod.Down);Assert.IsTrue(CombatCover.Clear(from,to),"a pod still on its stalk stops a shot: it should be a body, not cover");
            Assert.IsTrue(Walkable(At(0,0,0)),"a hanging pod blocks the floor under it");Assert.AreEqual(pod,CoverPod.NearestHanging(hero.transform.position));
            CoverPod felt=null;void Feel(CoverPod p)=>felt=p;CoverPod.Fell+=Feel;
            pod.hanging.GetComponent<Health>().Receive(new DamagePacket{source=hero.Health,amount=9999,type=DamageType.Kinetic});yield return null;
            CoverPod.Fell-=Feel;
            Assert.AreEqual(pod,felt);Assert.IsTrue(log.Contains("POD_DOWN Fixture pod"),"breaking a pod's stalk brought nothing down");Assert.IsFalse(pod.Hanging);Assert.IsTrue(pod.Down);
            Physics.SyncTransforms();Assert.IsFalse(CombatCover.Clear(from,to),"a fallen pod stops nothing: it isn't cover");
            Assert.AreEqual(other,CoverPod.NearestHanging(hero.transform.position),"a fallen pod is still offered as one to knock down");
            yield return Wait(.6f);Assert.IsFalse(Walkable(At(0,0,0)),"the fallen pod is still on the paths: a pack would walk through it");
            // Shaken loose by whatever it hangs from, and gone when that thing is.
            other.Drop();Assert.IsTrue(other.Down);Assert.IsNull(CoverPod.NearestHanging(hero.transform.position));
            Flag("bossdown.TestThane");yield return null;yield return null;
            Assert.IsFalse(pod.Down||pod.Hanging||other.Down,"the pods outlast the thing that hung them");Assert.IsTrue(CombatCover.Clear(from,to));
        }
        /// <summary>The hand of a switch: a point a hero works, with every field set (its defaults are chapter 1's).</summary>
        static DiscoveryPoint Work(string prompt,Vector3 at,string flag,string report="")
        {
            var go=new GameObject(prompt);go.SetActive(false);go.transform.position=at;var point=go.AddComponent<DiscoveryPoint>();
            point.prompt=prompt;point.flag=flag;point.report=report;point.situation="";point.dialogueNode="";point.dialogueSpeaker="Sela";point.requiredQuest="";point.requiredFlag="";point.range=3.2f;
            go.SetActive(true);return point;
        }

        [UnityTest] public IEnumerator RisingWaterMarksTheFloorItTakesSetsACaughtPartyBackOnItsLastLedgeAndDrainsForGood()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();
            // A hall floor that rises 4 m over 30: a slab tipped up toward the north, its low end at the fixture floor.
            var ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);ramp.name="Fixture hall floor";float pitch=Mathf.Atan2(4,30)*Mathf.Rad2Deg,run=Mathf.Sqrt(30*30+4*4);
            ramp.transform.SetPositionAndRotation(At(0,2,15)+Quaternion.Euler(-pitch,0,0)*Vector3.down*.25f,Quaternion.Euler(-pitch,0,0));ramp.transform.localScale=new Vector3(12,.5f,run);Physics.SyncTransforms();
            float Ground(float z)=>z*4f/30;
            Transform Mark(string name,float z){var t=new GameObject(name).transform;t.position=At(0,Ground(z),z);return t;}
            var water=Water(At(0,0,15),new Vector2(14,34),-.5f);
            var go=new GameObject("Fixture flood");go.SetActive(false);var flood=go.AddComponent<RisingWater>();
            flood.water=water;flood.restLevel=-.5f;flood.topLevel=3.6f;flood.rate=.6f;flood.caughtDepth=.6f;flood.damageShare=.12f;flood.fallBack=1;flood.grace=1.2f;flood.drainSeconds=1.2f;
            flood.low=Mark("Low end",.5f);flood.high=Mark("High end",29.5f);flood.width=10;flood.lead=3;flood.stopFlag="test.anchor2";flood.startFlag="test.anchor1";
            var first=Mark("First ledge",10);var second=Mark("Second ledge",20);flood.ledges=new[]{first,second};
            go.SetActive(true);yield return null;
            Put(hero,At(0,Ground(29)+.1f,29));Put(partner,At(2,Ground(29)+.1f,29));
            flood.Begin();Assert.IsFalse(flood.Rising,"the hall flooded before its door had opened");
            Flag("test.anchor1");flood.Begin();Assert.IsTrue(flood.Rising,"the water doesn't start");CollectionAssert.Contains(log,"FLOOD_RISING Fixture flood");
            yield return Wait(1.6f);
            Assert.Greater(water.Level,.2f,"the water isn't climbing");
            // What it is about to take is marked, in the colour that means danger, from the water that can catch someone to past the waterline.
            var band=Telegraph.Live.FirstOrDefault(t=>t!=null&&t.owner==flood&&!t.Released);
            Assert.IsNotNull(band,"the rising water marks nothing on the floor");Assert.AreEqual(Telegraph.Kind.Line,band.Shape);
            float line=water.Level*30/4;
            Assert.IsTrue(band.Contains(At(0,0,line+1.5f)),$"the floor a stride above the waterline (z {line:0.0}) isn't marked");
            Assert.IsFalse(band.Contains(At(0,0,line+8)),"the whole hall is marked: the marking says nothing about where the water is");
            // A hero stands on the first ledge, then goes back down into the deep.
            Put(hero,first.position+Vector3.up*.1f);yield return null;yield return null;Assert.AreEqual(first,flood.LastLedge,"standing on a ledge doesn't count as reaching it");
            float whole=hero.Health.integrity;Put(hero,At(0,Ground(1)+.05f,1));
            float deadline=Time.time+4;while(!log.Any(l=>l.StartsWith("FLOOD_CAUGHT"))&&Time.time<deadline)yield return null;
            CollectionAssert.Contains(log,"FLOOD_CAUGHT First ledge","a hero under the water was left there");
            Assert.Less(Vector3.Distance(hero.transform.position,first.position),.6f,"the caught hero isn't back on the last ledge reached");
            Assert.Less(Vector3.Distance(partner.transform.position,first.position),2.2f,"the partner was left behind in the water");
            Assert.Less(hero.Health.integrity,whole,"being caught cost nothing");Assert.GreaterOrEqual(hero.Health.integrity,whole-hero.Health.maximum*.125f,"being caught cost more than a modest share");
            yield return Wait(.9f);
            Assert.LessOrEqual(water.Level,first.position.y-.95f,"the water didn't fall back below the ledge: the party is stood in what caught it");
            Assert.Less(water.Depth(hero.transform.position),0,"the ledge the party was set back on is under water");
            // It never takes the last of a hero.
            hero.Health.integrity=1;hero.Health.InvulnerableUntil=0;log.Clear();Put(hero,At(0,Ground(1)+.05f,1));
            deadline=Time.time+6;while(!log.Any(l=>l.StartsWith("FLOOD_CAUGHT"))&&Time.time<deadline)yield return null;
            CollectionAssert.Contains(log,"FLOOD_CAUGHT First ledge");Assert.IsTrue(hero.Health.Alive,"the water killed a hero outright");
            // Cutting the far anchor ends it, and it doesn't come back.
            Flag("test.anchor2");yield return Wait(1.6f);
            Assert.IsFalse(flood.Rising);Assert.IsTrue(flood.Stopped);CollectionAssert.Contains(log,"FLOOD_DRAINED Fixture flood");
            Assert.AreEqual(-.5f,water.Level,.01f,"the hall didn't drain");
            Assert.IsFalse(Telegraph.Live.Any(t=>t!=null&&t.owner==flood),"a drained hall is still marked as danger");
            flood.Begin();yield return Wait(.5f);Assert.IsFalse(flood.Rising,"a hall whose anchor is cut flooded again");Assert.AreEqual(-.5f,water.Level,.01f);
        }

        [UnityTest] public IEnumerator CuttingAnAnchorLurchesThePlaceAndSwapsItsDoorsWithoutABake()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-40,.05f,-40));Put(hero,At(0,.05f,0));
            var gallery=Block("Gallery door",At(-10,1.5f,12),new Vector3(6,3,1.2f));var stair=Block("Vault stair door",At(10,1.5f,12),new Vector3(6,3,1.2f));stair.SetActive(false);
            var gates=new GameObject("Door sets");
            var opens=gates.AddComponent<FlagGate>();opens.flag="test.anchor";opens.showWhenSet=false;opens.targets=new[]{gallery};
            var shuts=gates.AddComponent<FlagGate>();shuts.flag="test.anchor";shuts.showWhenSet=true;shuts.targets=new[]{stair};
            var go=new GameObject("Fixture anchor");go.SetActive(false);go.transform.position=At(0,0,3);
            var whole=new GameObject("Root taut");whole.transform.SetParent(go.transform,false);var cut=new GameObject("Root cut");cut.transform.SetParent(go.transform,false);cut.SetActive(false);
            var anchor=go.AddComponent<ShiftAnchor>();anchor.flag="test.anchor";anchor.whole=whole;anchor.severed=cut;anchor.answers=new[]{gallery.transform,stair.transform};
            anchor.point=Work("Cut the anchor root",At(0,0,2),"test.anchor","AnchorRoot");
            go.SetActive(true);ShiftAnchor felt=null;void Feel(ShiftAnchor a)=>felt=a;ShiftAnchor.Lurched+=Feel;
            yield return Wait(.6f);
            Assert.IsFalse(Walkable(At(-10,0,12)),"fixture: the shut gallery door is on the paths");Assert.IsTrue(Walkable(At(10,0,12)),"fixture: the open stair isn't on the paths");
            Put(hero,At(-10,.05f,6));yield return Walk(hero,Vector2.up,1.6f);Assert.Less(hero.transform.position.z,11.5f,"the hero walked through a shut root door");
            var rig=Object.FindFirstObjectByType<CameraRig>();Assert.IsFalse(rig.Trembling,"fixture: the view is already shaking");
            Assert.IsTrue(whole.activeSelf);Assert.IsFalse(cut.activeSelf,"the root looks cut before anyone has cut it");
            Assert.IsTrue(anchor.point.Available);anchor.point.Interact();yield return null;yield return null;
            ShiftAnchor.Lurched-=Feel;
            Assert.IsTrue(Set("test.anchor"),"cutting the root set nothing");CollectionAssert.Contains(log,"ANCHOR_CUT test.anchor");
            Assert.AreEqual(anchor,felt);Assert.IsTrue(rig.Trembling,"the island doesn't lurch");CollectionAssert.Contains(cues,anchor.sound,"the island lurched without a sound");
            Assert.IsFalse(whole.activeSelf);Assert.IsTrue(cut.activeSelf,"the cut root still looks taut");Assert.IsFalse(anchor.point.Available,"a cut root can be cut again");
            Assert.IsFalse(gallery.activeSelf,"the gallery door didn't open");Assert.IsTrue(stair.activeSelf,"the vault stair didn't shut");
            yield return Wait(.6f);
            Assert.IsTrue(Walkable(At(-10,0,12)),"the opened door is still off the paths");Assert.IsFalse(Walkable(At(10,0,12)),"the shut stair is still on the paths");
            Put(hero,At(-10,.05f,6));yield return Walk(hero,Vector2.up,1.6f);Assert.Greater(hero.transform.position.z,13,"the opened door still stops the hero");
            Put(hero,At(10,.05f,6));yield return Walk(hero,Vector2.up,1.6f);Assert.Less(hero.transform.position.z,11.5f,"the hero walked through the stair that shut");
        }

        [UnityTest] public IEnumerator ABasinDrainsByThirdsAsItsGatesBreakAndAWonArenaLoadsDry()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-40,.05f,-40));Put(hero,At(0,.05f,-20));
            FloodGate Gate(string name,Vector3 at,string settledBy=null)
            {
                var go=new GameObject(name);go.SetActive(false);go.transform.position=at;
                var health=go.AddComponent<Health>();health.id="FloodGate";health.maximum=health.integrity=400;health.breakThreshold=100000;
                go.AddComponent<SphereCollider>().radius=1.2f;go.AddComponent<Hurtbox>().owner=health;
                var whole=new GameObject("Standing");whole.transform.SetParent(go.transform,false);var wreck=new GameObject("Wreck");wreck.transform.SetParent(go.transform,false);
                var gate=go.AddComponent<FloodGate>();gate.whole=whole;gate.wrecked=wreck;gate.defeatedFlag="test.gate."+name;gate.settledBy=settledBy;
                go.SetActive(true);return gate;
            }
            var gates=new[]{Gate("north",At(0,0,17)),Gate("east",At(15,0,-8)),Gate("west",At(-15,0,-8))};
            var water=Water(At(0,0,0),new Vector2(34,34),1.2f);
            var basin=new GameObject("Fixture basin").AddComponent<FloodBasin>();basin.water=water;basin.fullLevel=1.2f;basin.emptyLevel=-.3f;basin.seconds=1;
            yield return null;yield return null;
            Assert.AreEqual(1,FloodGate.Held,.001f);Assert.AreEqual(1.2f,water.Level,.01f,"three gates standing and the basin isn't full");
            // It is the arena's water as a boss reads it: a share of full, and whether a spot of floor is under it.
            Assert.AreSame(basin,ArenaWater.Current,"the basin doesn't stand in as the arena's water");Assert.AreEqual(1,ArenaWater.Level,.001f);
            Assert.IsTrue(ArenaWater.Submerged(At(0,0,0)),"the flooded floor reads as dry");Assert.IsFalse(ArenaWater.Submerged(At(0,0,40)),"floor outside the basin reads as under water");
            Assert.IsTrue(gates[0].whole.activeSelf);Assert.IsFalse(gates[0].wrecked.activeSelf);
            // Shut to blows outside the boss's openings.
            FloodGate.Expose(false);gates[0].Health.Receive(new DamagePacket{source=hero.Health,amount=9999,type=DamageType.Kinetic});
            Assert.IsTrue(gates[0].Holding,"a gate broke while it was shut to blows");
            FloodGate.Expose(true);
            float[] thirds={.7f,.2f,-.3f};
            for(int i=0;i<3;i++)
            {
                Assert.AreEqual(gates[i],FloodGate.Nearest(gates[i].transform.position));
                gates[i].Health.Receive(new DamagePacket{source=hero.Health,amount=9999,type=DamageType.Kinetic});yield return null;
                CollectionAssert.Contains(log,"GATE_DOWN "+gates[i].name);Assert.AreEqual(2-i,FloodGate.Standing);
                Assert.IsTrue(Set("test.gate."+gates[i].name),"a broken gate isn't in the save");Assert.IsFalse(gates[i].whole.activeSelf);Assert.IsTrue(gates[i].wrecked.activeSelf,"a broken gate looks whole");
                yield return Wait(.4f);Assert.Greater(water.Level,thirds[i]+.02f,"the basin dropped a third in one step");
                yield return Wait(.9f);Assert.AreEqual(thirds[i],water.Level,.02f,$"with {2-i} gates standing the water is at the wrong height");
            }
            Assert.IsNull(FloodGate.Nearest(Vector3.zero));Assert.AreEqual(0,ArenaWater.Level,.001f);Assert.IsFalse(ArenaWater.Submerged(At(0,0,0)),"the drained floor reads as under water");
            Put(hero,At(0,.05f,0));yield return Wait(.3f);Assert.AreEqual(1,hero.Health.Pace,.001f,"the drained basin's floor is still waded");
            // A boss can send the water where a move wants it, and gets what it asked for.
            ArenaWater.SetLevel(.5f,.6f);yield return Wait(.9f);Assert.AreEqual(.45f,water.Level,.02f,"the basin didn't go where it was sent");Assert.AreEqual(.5f,basin.Level,.001f);
            ArenaWater.SetLevel(0,0);Assert.AreEqual(-.3f,water.Level,.01f);
            // A basin whose boss has fallen loads with its gates down and its floor dry.
            foreach(var gate in gates)Object.Destroy(gate.gameObject);Object.Destroy(basin.gameObject);yield return null;
            Flag("bossdown.TestCrown");log.Clear();
            var later=new[]{Gate("north again",At(0,0,17),"TestCrown"),Gate("east again",At(15,0,-8),"TestCrown")};
            var dry=Water(At(0,0,0),new Vector2(34,34),1.2f);var won=new GameObject("Fixture basin").AddComponent<FloodBasin>();won.water=dry;won.fullLevel=1.2f;won.emptyLevel=-.3f;
            yield return null;yield return null;
            Assert.AreEqual(0,FloodGate.Standing,"a won arena's gates stand again");Assert.AreEqual(-.3f,dry.Level,.01f,"a won arena loads flooded");
            Assert.IsEmpty(log,"a loaded save broke its gates all over again: "+string.Join(", ",log));
        }
    }
}
