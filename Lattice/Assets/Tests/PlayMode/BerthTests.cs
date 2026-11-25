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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>
    /// Chapter 10's berth (ch10-wiring 6.1 and 6.1.1, D285) on a bare shelf in the flight yard, and a stretch of space beside the ground yard: docking and launching by the
    /// prompt with no load, both heroes moved and everything that runs the fight still running; forms apart (a flier never hits a hero on a shelf); a save on a shelf and a fall on
    /// one mid-fight, each read back from the disk after the scene is loaded again; a conversation holding the berth and its clocks (D275); and nothing it makes solid walling a
    /// hero in (D289). Every check is first run against the broken state, inside the test, and required to fail there.
    /// </summary>
    public sealed class BerthTests
    {
        // ------------------------------------------------------------------ the yard's shelf, well east of the proving berth's rails: 14 x 10 m, its floor 1.2 m under the plane
        const float Plane=1,FloorTop=-.2f;
        static Bounds Box=>new(new Vector3(150,FloorTop+.5f,150),new Vector3(14,1,10));
        static readonly Vector3 Pad=new(150,Plane,140),Launch=new(145,FloorTop,152),Foot=new(150,FloorTop,147),FlightAt=new(150,Plane,131),ReturnAt=new(142,Plane,133),DoorAt=new(150,Plane,108);
        const string Id="TestShelf",Return="TestShelfPad",Door="TestShelfDoor",Cradle="TestShelfCradle",Part="end.testClamp1";

        /// <summary>The yard, built into every load of the flight yard before its party is set down (a scene's own objects, as a builder's would be).</summary>
        static void Built(Scene scene,LoadSceneMode mode)
        {
            if(scene.name!="Arena_Flight")return;
            var yard=new GameObject("Berth yard");SceneManager.MoveGameObjectToScene(yard,scene);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Shelf floor";floor.transform.SetParent(yard.transform,false);
            floor.transform.position=new Vector3(150,FloorTop-.5f,150);floor.transform.localScale=new Vector3(14,1,10);
            Point(yard,Door,DoorAt);Point(yard,Cradle,Foot+Vector3.right*4);
            Berth.Declare(Id,"The test shelf",ZoneKind.GroundCombat,Box,Pad,Launch,Foot,FlightAt,Return,ReturnAt,true,true,yard.transform);
        }
        static void Point(GameObject yard,string id,Vector3 at){var go=new GameObject(id,typeof(SpawnPoint));go.transform.SetParent(yard.transform,false);go.transform.position=at;go.GetComponent<SpawnPoint>().id=id;}

        readonly List<string> heard=new();
        void Heard(string line,string stack,LogType type)=>heard.Add(line);
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.sceneLoaded+=Built;Application.logMessageReceived+=Heard;
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded-=Built;Application.logMessageReceived-=Heard;
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static SaveSystem Saves=>GameServices.Current.Saves;
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static bool OnDisk(string key)=>new FlagService(Saves.Load("autosave").flags).GetBool(key);
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=PartyController.Current.Active);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Until(System.Func<bool> condition,float seconds){float end=Time.unscaledTime+seconds;while(!condition()&&Time.unscaledTime<end)yield return null;}
        static IEnumerator Load(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+15;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            yield return Ready();
        }
        /// <summary>The yard's own opponents asleep, the heroes the test's to move and out of harm's way.</summary>
        static IEnumerator Ready()
        {
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            Hold();yield return new WaitForSecondsRealtime(.5f);
        }
        static void Hold(){foreach(var hero in PartyController.Current.members){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;hero.Health.InvulnerableUntil=GameTime.Now+900;}}
        static void Put(Component body,Vector3 at)
        {
            var controller=body.GetComponent<CharacterController>();bool on=controller.enabled;controller.enabled=false;
            if(body.TryGetComponent<FlightMotor>(out var ship))ship.Halt();if(body.TryGetComponent<GroundMotor>(out var feet))feet.Halt();
            body.transform.position=at;controller.enabled=on;Physics.SyncTransforms();
        }
        /// <summary>As PlayerBrain does: the prompt nearest the lead, then A. Past the hand-back hold a change leaves (GameInput.HandBackHold).</summary>
        static IEnumerator Press(InteractionPrompt expected,string what)
        {
            yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.1f);
            PromptService.FindInteraction(Lead.transform.position);
            Assert.AreSame(expected,PromptService.Interaction,$"{what}: the lead at {Lead.transform.position} isn't offered \"{expected.prompt}\" (offered \"{PromptService.Interaction?.prompt??"nothing"}\")");
            Assert.IsTrue(PromptService.TryInteract(),what+": A didn't take the prompt");
            Assert.IsTrue(Berth.Changing,what+": the press started no change");
            float end=Time.unscaledTime+3;while(Berth.Changing&&Time.unscaledTime<end)yield return null;
            Assert.IsFalse(Berth.Changing,what+": the change never finished");
            Assert.IsFalse(GameServices.Current.Input.Blocked,what+": the controls weren't given back");
        }
        static bool Overlaps(CombatActor hero)
        {
            var body=hero.GetComponent<CharacterController>();float r=body.radius,half=Mathf.Max(r,body.height*.5f)-r;var centre=hero.transform.position+body.center;
            foreach(var other in Physics.OverlapCapsule(centre-Vector3.up*half+Vector3.up*.1f,centre+Vector3.up*half,r-.05f,~0,QueryTriggerInteraction.Ignore))
                if(other.GetComponentInParent<CombatActor>()==null)return true;
            return false;
        }
        static bool Cell(Vector3 at,float radius,float height){var low=at+Vector3.up*(radius+.1f);return Physics.CheckCapsule(low,low+Vector3.up*Mathf.Max(0,height-radius*2),radius-.05f,~0,QueryTriggerInteraction.Ignore);}

        // ------------------------------------------------------------------ docking and launching
        [UnityTest] public IEnumerator AShipDocksAtAShelfAndBothLaunchBesideItsPadInOneScene()
        {
            yield return Load("Arena_Flight");
            var berth=Berth.Find(Id);Assert.IsNotNull(berth,"fixture: the yard has no berth");
            var zone=ZoneController.Current;var partner=Partner;var hud=Object.FindFirstObjectByType<GameHud>();var rig=Object.FindFirstObjectByType<CameraRig>();
            string track=MusicDirector.Current!=null?MusicDirector.Current.Track:null;
            bool OnFoot(CombatActor hero)=>!hero.flight&&zone.Kind==ZoneKind.GroundCombat&&berth.Contains(hero.transform.position+Vector3.up*.5f)&&Mathf.Abs(hero.transform.position.y-FloorTop)<.2f&&
                hero.GetComponent<GroundMotor>().enabled&&!hero.GetComponent<FlightMotor>().enabled;
            bool Afloat(CombatActor hero)=>hero.flight&&zone.Kind==ZoneKind.SpaceCombat&&Mathf.Abs(hero.transform.position.y-Plane)<.05f&&Flat(hero.transform.position,Pad)<14&&
                hero.GetComponent<FlightMotor>().enabled&&!hero.GetComponent<GroundMotor>().enabled;
            // The broken state first: the world without a berth. A hero put onto the shelf by hand is a ship over a hole (the form is the zone's), and his partner is back at the arrival.
            Put(Lead,Foot);yield return null;
            Assert.IsFalse(OnFoot(Lead),"fixture: a hero put on the shelf by hand already stands there on foot");Assert.IsFalse(OnFoot(partner),"fixture: the partner is on the shelf before anyone docked");
            Put(Lead,Pad+Vector3.back*3);yield return null;
            Assert.AreEqual("Flight",rig.profile.name,"fixture: the yard isn't seen with the flight camera before anyone docks");
            Assert.AreEqual("Dock — The test shelf",berth.pad.prompt);
            var seen=new Dictionary<CombatActor,Vector3>();System.Action<Berth,bool> changed=(b,on)=>{foreach(var m in PartyController.Current.members)seen[m]=m.transform.position;};
            Berth.Changed+=changed;
            try
            {
                heard.Clear();bool loading=false;
                var press=Press(berth.pad,"docking");while(press.MoveNext()){loading|=SceneFlow.Current.Loading;yield return press.Current;}
                Assert.IsTrue(Berth.Docked&&Berth.Current==berth,"the pad didn't put the party on the shelf");
                Assert.IsFalse(loading||heard.Any(l=>l.StartsWith("ZONE_ENTER")),"docking loaded a scene");Assert.AreSame(zone,ZoneController.Current,"docking replaced the zone");
                Assert.IsTrue(heard.Contains("BERTH_DOCK "+Id),"no BERTH_DOCK in the log");
                Assert.IsTrue(OnFoot(Lead),$"the lead isn't on his feet on the shelf: at {Lead.transform.position}, flight {Lead.flight}, the zone {zone.Kind}");
                Assert.IsTrue(OnFoot(partner),$"the partner didn't come down onto the shelf with him: at {partner.transform.position}");
                Assert.Less(Flat(seen[Lead],seen[partner]),3,"the two were set down apart, not side by side");Assert.Greater(Flat(seen[Lead],seen[partner]),1,"the two were set down on top of each other");
                Assert.IsTrue(berth.edges.activeSelf,"the shelf's edges aren't solid while the party stands on it");
                Assert.AreEqual("Ground",zone.CameraProfile.name,"the shelf isn't seen with the ground camera");Assert.AreEqual("Ground",rig.profile.name,"the camera rig still holds the flight camera");
                Assert.AreSame(hud,Object.FindFirstObjectByType<GameHud>(),"the HUD was replaced");Assert.IsTrue(hud!=null&&hud.isActiveAndEnabled,"the HUD went out");
                Assert.AreEqual(track,MusicDirector.Current!=null?MusicDirector.Current.Track:null,"the music changed with the form");
                yield return new WaitForSecondsRealtime(1);
                Assert.AreEqual(BodyForm.Shaped,Lead.GetComponent<FormController>().Current,"the lead isn't in his Shaped body on the shelf");
                Assert.AreEqual(BodyForm.Shaped,partner.GetComponent<FormController>().Current,"the partner isn't in her Shaped body on the shelf");
                float pitch=Camera.main.transform.eulerAngles.x;Assert.AreEqual(zone.CameraProfile.pitch,pitch,4,"the camera doesn't look down at the ground camera's angle");
                // Back out: the launch, at its own place on the shelf.
                Hold();Put(Lead,Launch+Vector3.right*1.2f);Put(partner,Foot);yield return null;
                Assert.IsFalse(Afloat(Lead)||Afloat(partner),"fixture: a hero on the shelf already reads as a ship beside the pad");
                StringAssert.StartsWith("Launch — ",berth.launch.prompt);
                heard.Clear();loading=false;press=Press(berth.launch,"launching");while(press.MoveNext()){loading|=SceneFlow.Current.Loading;yield return press.Current;}
                Assert.IsFalse(Berth.Docked,"the launch left the party on the shelf");Assert.AreEqual(Id,Berth.LastLaunched);
                Assert.IsFalse(loading||heard.Any(l=>l.StartsWith("ZONE_ENTER")),"launching loaded a scene");Assert.AreSame(zone,ZoneController.Current,"launching replaced the zone");
                Assert.IsTrue(Afloat(Lead),$"the lead isn't back in his ship beside the pad: at {Lead.transform.position}, flight {Lead.flight}, the zone {zone.Kind}");
                Assert.IsTrue(Afloat(partner),$"the partner isn't back in her ship beside the pad: at {partner.transform.position}");
                Assert.Less(Flat(seen[Lead],FlightAt),1.5f,"the lead wasn't set down at the berth's flight place");
                float apart=Flat(seen[Lead],seen[partner]);Assert.GreaterOrEqual(apart,HeroCollision.Apart(Lead.character,partner.character)-.1f,"the two hulls were set down overlapping");Assert.Less(apart,8,"the two ships were set down far apart");
                Assert.IsFalse(berth.edges.activeSelf,"the shelf's edges stayed solid at the plane after the launch");
                Assert.AreEqual("Flight",zone.CameraProfile.name);Assert.AreEqual("Flight",rig.profile.name,"the camera rig kept the ground camera in flight");
                Assert.AreEqual(track,MusicDirector.Current!=null?MusicDirector.Current.Track:null,"the music changed with the form");
                yield return new WaitForSecondsRealtime(1);
                Assert.AreEqual(BodyForm.Flight,Lead.GetComponent<FormController>().Current,"the lead isn't in his ship");
                Assert.AreEqual(Plane,Lead.GetComponent<FlightMotor>().plane,.01f,"the ship flies at the wrong plane after the launch");
            }
            finally{Berth.Changed-=changed;}
        }

        [UnityTest] public IEnumerator AFlierNeitherMarksNorHitsAHeroOnAShelfAndTheShelfsBodiesLeaveTheShipsAlone()
        {
            yield return Load("Arena_Flight");
            var berth=Berth.Find(Id);var zone=ZoneController.Current;
            // A hound on the shelf, awake and hard to kill, and a shooter at the plane east of the pad, asleep for now.
            var hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),new Vector3(151,FloorTop+.1f,148));
            var flier=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDrifter"),new Vector3(170,Plane,140));flier.Passive=true;
            foreach(var body in new[]{hound,flier})body.Health.maximum=body.Health.integrity=50000;
            yield return Frames(3);
            Assert.IsFalse(hound.Flier,"fixture: the hound on the shelf reads as a flier");Assert.IsTrue(flier.Flier,"fixture: the shooter at the plane reads as a body on foot");
            // A ship hovering over the shelf, the hound under it: the broken state (no berth in the world) is a hound that tells a lunge at a ship.
            Put(Lead,new Vector3(150,Plane,146.5f));var berths=ZoneController.Berths.ToArray();
            bool told=false;
            ZoneController.Berths.Clear();
            try{for(float end=Time.unscaledTime+3;Time.unscaledTime<end&&!told;){told|=hound.Telegraphing;yield return null;}}
            finally{ZoneController.Berths.AddRange(berths);}
            Assert.IsTrue(told,"fixture: with no berth the hound never tells an attack at a ship right over it");
            Put(hound,new Vector3(151,FloorTop+.1f,148));yield return Frames(2);
            told=false;for(float end=Time.unscaledTime+2.5f;Time.unscaledTime<end;){told|=hound.Telegraphing;yield return null;}
            Assert.IsFalse(told,"a hound on the shelf told an attack at a ship flying over it");
            Assert.IsTrue(berth.Contains(hound.transform.position+Vector3.up*.5f),$"the hound left its shelf (at {hound.transform.position})");
            // Docked: the hound is the fight, and its tells are painted on foot.
            Put(Lead,Pad+Vector3.back*3);yield return Press(berth.pad,"docking with the shelf's hound awake");Hold();
            told=false;for(float end=Time.unscaledTime+5;Time.unscaledTime<end&&!told;){told|=hound.Telegraphing&&hound.AttackMark!=null;yield return null;}
            Assert.IsTrue(told,"on the shelf the hound never told an attack at the hero on foot");
            hound.Passive=true;Put(hound,new Vector3(220,FloorTop,220));yield return Frames(2);
            // The shooter, woken with the party on the shelf. Broken state first: no berth in the world, and it falls off the plane, locks and lands its shots.
            var lead=Lead;var at=lead.transform.position;
            ZoneController.Berths.Clear();
            try
            {
                Assert.AreSame(flier.Health,lead.GetComponent<PlayerBrain>().FindTarget(),"fixture: with no berth the lock-on doesn't find the shooter over the shelf");
                lead.Health.InvulnerableUntil=0;float before=lead.Health.integrity;
                lead.Health.Receive(new DamagePacket{source=flier.Health,amount=40,type=DamageType.Beam});
                Assert.Less(lead.Health.integrity,before,"fixture: with no berth the shooter's shot doesn't land on the hero on the shelf");
                lead.Health.Heal(lead.Health.maximum);lead.Health.InvulnerableUntil=GameTime.Now+900;
                flier.Passive=false;float top=flier.transform.position.y;yield return new WaitForSecondsRealtime(1);
                Assert.Less(flier.transform.position.y,top-1,"fixture: with no berth the shooter doesn't sink off the plane while the zone is on foot");
            }
            finally{ZoneController.Berths.AddRange(berths);flier.Passive=true;lead.Health.InvulnerableUntil=GameTime.Now+900;}
            Put(flier,new Vector3(170,Plane,140));Put(lead,at);yield return Frames(2);
            Assert.IsNull(lead.GetComponent<PlayerBrain>().FindTarget(),"the lock-on found a flier from the shelf");
            lead.Health.InvulnerableUntil=0;float whole=lead.Health.integrity;
            lead.Health.Receive(new DamagePacket{source=flier.Health,amount=40,type=DamageType.Beam});
            Assert.AreEqual(whole,lead.Health.integrity,.01f,"a flier's shot landed on a hero standing on the shelf");
            lead.Health.Receive(new DamagePacket{source=flier.Health,amount=40,type=DamageType.Beam,tag=Health.AnyForm});
            Assert.Less(lead.Health.integrity,whole,"a move marked to reach the shelf (Health.AnyForm) passed the hero by");
            lead.Health.Heal(lead.Health.maximum);lead.Health.InvulnerableUntil=GameTime.Now+900;
            flier.Passive=false;told=false;
            for(float end=Time.unscaledTime+3;Time.unscaledTime<end;){told|=flier.Telegraphing;yield return null;}
            Assert.IsFalse(told,"a flier told an attack at a hero on the shelf");
            Assert.AreEqual(Plane,flier.transform.position.y,.3f,"a flier fell off the plane while the party stood on the shelf");
            // Launched, the same shooter fights the ships again: its targeting and its tells came through the change.
            Put(Lead,Launch+Vector3.right*1.2f);yield return Press(berth.launch,"launching");Hold();
            told=false;for(float end=Time.unscaledTime+8;Time.unscaledTime<end&&!told;){told|=flier.Telegraphing&&flier.AttackMark!=null;yield return null;}
            Assert.IsTrue(told,"after the launch the shooter never told an attack at the ships");
            Assert.AreSame(flier.Health,Lead.GetComponent<PlayerBrain>().FindTarget(),"after the launch the lock-on doesn't find the shooter");
        }

        // ------------------------------------------------------------------ saves (rules 2 to 4)
        [UnityTest] public IEnumerator ASaveOnAShelfContinuesInFlightBesideItsPadWithWhatWasDoneThereKept()
        {
            yield return Load("Arena_Flight");
            var berth=Berth.Find(Id);
            Put(Lead,Pad+Vector3.back*3);yield return Press(berth.pad,"docking");
            // A cradle on the shelf saves where it stands. The broken state first: with nobody minding the berth that save names the shelf, and a Continue sets a ship on it.
            var guard=SaveSystem.SafeSpawn;State.spawn=Cradle;
            SaveSystem.SafeSpawn=null;try{Saves.Save("autosave",State);}finally{SaveSystem.SafeSpawn=guard;}
            Assert.AreEqual(Cradle,Saves.Load("autosave").spawn,"fixture: a save with no berth's rule doesn't name the shelf's spawn");
            State.spawn=Cradle;heard.Clear();Saves.Save("autosave",State);
            Assert.AreEqual(Return,Saves.Load("autosave").spawn,"a save made on the shelf names a spawn on the shelf");Assert.IsTrue(heard.Contains("BERTH_SAVE "+Return),"no BERTH_SAVE in the log");
            // What the shelf's work did is kept the moment it is done.
            GameServices.Current.Flags.SetBool(Part,true);
            Assert.IsFalse(OnDisk(Part),"fixture: the part is on the disk before anything kept it");
            SaveSystem.KeepCheckpoint();
            Assert.IsTrue(OnDisk(Part),"KeepCheckpoint didn't write the part done");Assert.AreEqual(Return,Saves.Load("autosave").spawn,"keeping the part moved the save onto the shelf");
            // Quit to the title and Continue.
            SceneFlow.Current.LoadZone("Title");float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            yield return Frames(2);var title=Object.FindFirstObjectByType<TitleScreen>();Assert.IsNotNull(title,"fixture: the title never came up");
            title.StartGame(true);yield return null;
            deadline=Time.unscaledTime+15;while((SceneFlow.Current.Loading||SceneFlow.Current.Zone!="Arena_Flight")&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual("Arena_Flight",SceneFlow.Current.Zone,"the Continue didn't come back to the yard");yield return Ready();
            var zone=ZoneController.Current;
            Assert.IsFalse(Berth.Docked||zone.Berthed,"the Continue arrived on the shelf");Assert.AreEqual(ZoneKind.SpaceCombat,zone.Kind,"the Continue arrived in the shelf's form");
            foreach(var hero in PartyController.Current.members)
            {
                Assert.IsTrue(hero.flight,hero.character+" came back on foot");
                Assert.AreEqual(Plane,hero.transform.position.y,.05f,hero.character+" came back off the plane");
                Assert.Less(Flat(hero.transform.position,ReturnAt),5,$"{hero.character} came back at {hero.transform.position}, not beside the shelf's pad");
                Assert.IsFalse(Box.Contains(hero.transform.position),hero.character+" came back inside the shelf");
            }
            Assert.IsTrue(Has(Part),"the Continue undid the part done on the shelf");Assert.AreEqual(Return,State.spawn);
        }

        [UnityTest] public IEnumerator AFallOnAShelfMidFightRetriesAtTheDoorInFlightWithItsPartsKept()
        {
            yield return Load("Arena_Flight");
            var berth=Berth.Find(Id);
            // The fight's door: an encounter whose checkpoint is the door spawn, its boss far up the yard and held still.
            var door=new GameObject("Test door").AddComponent<EncounterVolume>();door.encounterId="TestShelfFight";door.checkpointSpawn=Door;door.membranes=new Membrane[0];
            var spawner=new GameObject("Boss spawner").AddComponent<Spawner>();spawner.transform.position=new Vector3(150,Plane,198);
            spawner.definition=GameCatalog.Find<EnemyDef>("Burrower");spawner.count=1;spawner.radius=0;door.spawners=new[]{spawner};
            yield return null;door.Begin();yield return Frames(2);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            Assert.IsTrue(door.Started&&!door.Cleared,"fixture: the fight didn't begin");Assert.AreEqual(Door,Saves.Load("autosave").spawn,"fixture: beginning the fight didn't save at its door");
            Put(Lead,Pad+Vector3.back*3);yield return Press(berth.pad,"docking mid-fight");
            // A part done on the shelf. The broken state first (D204's "the boss at full integrity"): the door's save doesn't hold it, so a defeat would undo it.
            GameServices.Current.Flags.SetBool(Part,true);
            Assert.IsFalse(OnDisk(Part),"fixture: the door's save already holds the part");
            SaveSystem.KeepCheckpoint();
            Assert.IsTrue(OnDisk(Part),"the part done on the shelf wasn't kept");Assert.AreEqual(Door,Saves.Load("autosave").spawn,"keeping a part mid-fight moved the retry off the door");
            // Any other save on the shelf while the fight is begun (a gun encounter's clear) keeps the door too, never the pad beside the shelf.
            Saves.Save("autosave",State);Assert.AreEqual(Door,Saves.Load("autosave").spawn,"a save on the shelf mid-fight named somewhere other than the door");
            // The two out go down on the shelf.
            foreach(var hero in PartyController.Current.members){hero.Health.InvulnerableUntil=0;hero.Health.Receive(new DamagePacket{amount=hero.Health.maximum*10,type=DamageType.Kinetic});}
            Button retry=null;float deadline=Time.unscaledTime+6;
            while(retry==null&&Time.unscaledTime<deadline){retry=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name=="Retry"&&b.isActiveAndEnabled);yield return null;}
            Assert.IsNotNull(retry,"no defeat screen after both heroes fell on the shelf");
            retry.onClick.Invoke();yield return null;
            deadline=Time.unscaledTime+15;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"the retry never finished loading");yield return Ready();
            var zone=ZoneController.Current;
            Assert.IsFalse(Berth.Docked||zone.Berthed,"the retry put the party back on the shelf");Assert.AreEqual(ZoneKind.SpaceCombat,zone.Kind,"the retry arrived in the shelf's form");
            foreach(var hero in PartyController.Current.members)
            {
                Assert.IsTrue(hero.flight,hero.character+" retried on foot");Assert.AreEqual(Plane,hero.transform.position.y,.05f,hero.character+" retried off the plane");
                Assert.Less(Flat(hero.transform.position,DoorAt),5,$"{hero.character} retried at {hero.transform.position}, not at the fight's door");
                Assert.IsTrue(hero.Health.Alive&&hero.Health.integrity>=hero.Health.maximum-.01f,hero.character+" retried short of whole");
            }
            Assert.AreEqual(Door,State.spawn);Assert.IsTrue(Has(Part),"the retry undid the part done on the shelf");Assert.IsFalse(Has("clear.TestShelfFight"),"the retry counted the fight as won");
        }

        // ------------------------------------------------------------------ a conversation holds the berth (D275)
        [UnityTest] public IEnumerator AConversationHoldsTheBerthItsPromptsAndItsClocksAndAMovingBerthNeverMovesUnderTheParty()
        {
            yield return Load("Arena_Flight");
            var berth=Berth.Find(Id);var start=berth.transform.position;
            // A berth on a slow swing, as Mother's fourth pad: its driver asks every frame.
            Vector3 Swing(float t)=>start+new Vector3(Mathf.Sin(t*.8f)*2.5f,0,0);
            IEnumerator Drive(float seconds){for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){berth.Drive(Swing(berth.Travel),Quaternion.identity);yield return null;}}
            float travel=berth.Travel;yield return Drive(1);
            Assert.Greater(Vector3.Distance(berth.transform.position,start),.3f,"fixture: the free berth doesn't swing");Assert.Greater(berth.Travel,travel+.5f,"fixture: the free berth's clock doesn't run");
            yield return Until(()=>DialogueSystem.Current!=null&&DialogueSystem.Current.Prepared,10);
            void Talk(){if(DialogueSystem.Current==null||!DialogueSystem.Current.StartNode("ArenaGuide","Hal"))GameServices.Current.Input.Blocked=true;}
            void Hush(){if(DialogueSystem.Current!=null&&DialogueSystem.Current.Running)DialogueSystem.Current.Cancel();GameServices.Current.Input.Blocked=false;}
            Put(Lead,Pad+Vector3.back*3);yield return null;
            try
            {
                // In flight, talking: the broken state first, a driver that moves it minding nothing swings it through the talk.
                Talk();yield return Frames(2);Assert.IsTrue(GameServices.Current.Input.Blocked,"fixture: the conversation doesn't hold the controls");
                var held=berth.transform.position;
                for(float end=Time.unscaledTime+1;Time.unscaledTime<end;){berth.transform.position=Swing(Time.time);yield return null;}
                Assert.Greater(Vector3.Distance(berth.transform.position,held),.1f,"fixture: a driver minding nothing doesn't move it");
                berth.transform.position=held;travel=berth.Travel;
                yield return Drive(1.5f);
                Assert.AreEqual(held,berth.transform.position,"the berth swung through a conversation");Assert.AreEqual(travel,berth.Travel,.001f,"the berth's clock ran through a conversation");
                PromptService.FindInteraction(Lead.transform.position);Assert.IsFalse(berth.pad.Available,"the pad is offered in the middle of a conversation");
                Hush();yield return Drive(.5f);Assert.Greater(berth.Travel,travel+.2f,"the berth's clock didn't start again after the talk");
                // Docked: it never moves, its own clock runs, and a talk stops that clock and the launch.
                Put(Lead,berth.pad.transform.position+Vector3.back*3);yield return Press(berth.pad,"docking");Hold();
                var docked=berth.transform.position;var standing=Lead.transform.position;float since=berth.DockedSeconds;
                yield return Drive(1.5f);
                Assert.AreEqual(docked,berth.transform.position,"the berth moved with the party on it");Assert.Greater(berth.DockedSeconds,since+1,"the docked clock doesn't run");
                Assert.Less(Flat(Lead.transform.position,standing),.05f,"the floor carried the hero");
                Talk();yield return Frames(2);since=berth.DockedSeconds;
                yield return Drive(1.5f);
                Assert.AreEqual(since,berth.DockedSeconds,.001f,"the docked clock ran through a conversation");Assert.AreEqual(docked,berth.transform.position);
                Assert.IsFalse(berth.launch.Available,"the launch is offered in the middle of a conversation");
                // An arm drawn in mid-talk waits for the talk.
                heard.Clear();berth.Launch("arm");yield return new WaitForSecondsRealtime(1);
                Assert.IsTrue(Berth.Docked,"the berth put the party out in the middle of a conversation");
                Hush();yield return Until(()=>!Berth.Docked&&!Berth.Changing,3);
                Assert.IsFalse(Berth.Docked,"the arm drawn in never put the party out once the talk was over");Assert.IsTrue(heard.Contains("BERTH_LAUNCH "+Id+" arm"),"no BERTH_LAUNCH for the arm in the log");
                Assert.IsTrue(Lead.flight&&Mathf.Abs(Lead.transform.position.y-Plane)<.05f,"put out, the party isn't flying");
                yield return Drive(1);Assert.Greater(Vector3.Distance(berth.transform.position,docked),.1f,"the berth never swung again after the party left it");
            }
            finally{Hush();}
        }

        // ------------------------------------------------------------------ nothing it makes solid walls a hero in (D289)
        [UnityTest] public IEnumerator NothingTheBerthMakesSolidWallsAHeroIn()
        {
            yield return Load("Arena_Flight");
            var berth=Berth.Find(Id);
            // A crate on the foot place, and a rock where the ships come out.
            var crate=GameObject.CreatePrimitive(PrimitiveType.Cube);crate.transform.position=Foot+Vector3.up*.6f;crate.transform.localScale=new Vector3(1.6f,1.2f,1.6f);
            var rock=GameObject.CreatePrimitive(PrimitiveType.Cube);rock.transform.position=FlightAt;rock.transform.localScale=Vector3.one*4;Physics.SyncTransforms();
            Assert.IsTrue(Cell(Foot,.35f,1.8f),"fixture: a body set down on the foot place doesn't stand in the crate");
            Assert.IsTrue(Cell(FlightAt+Vector3.down*.9f,1.7f,3.4f),"fixture: a hull set down at the flight place doesn't stand in the rock");
            Put(Lead,Pad+Vector3.back*3);yield return Press(berth.pad,"docking onto the crate");Hold();
            foreach(var hero in PartyController.Current.members)
            {
                Assert.IsFalse(Overlaps(hero),$"{hero.character} was set down inside something solid at {hero.transform.position}");
                Assert.IsTrue(berth.Contains(hero.transform.position+Vector3.up*.5f),$"{hero.character} was set down off the shelf at {hero.transform.position}");
            }
            // Walled by its edges and the crate, the lead still walks to the launch.
            var feet=Lead.GetComponent<GroundMotor>();
            for(float end=Time.unscaledTime+4;Time.unscaledTime<end&&Flat(Lead.transform.position,Launch)>2;)
            {var way=Launch-Lead.transform.position;feet.Move(new Vector2(way.x,way.z).normalized,false,false);yield return null;}
            Assert.LessOrEqual(Flat(Lead.transform.position,Launch),2,$"the lead couldn't walk from where he was set down to the launch (stopped at {Lead.transform.position})");
            yield return Press(berth.launch,"launching into the rock");Hold();
            foreach(var hero in PartyController.Current.members)
            {
                Assert.IsFalse(Overlaps(hero),$"{hero.character}'s hull was set down inside something solid at {hero.transform.position}");
                Assert.AreEqual(Plane,hero.transform.position.y,.05f);
            }
            Assert.IsFalse(berth.edges.activeSelf,"the shelf's edges stayed solid after the launch");
            // Each ship flies clear, once its hull has opened.
            yield return new WaitForSecondsRealtime(1);var from=PartyController.Current.members.ToDictionary(m=>m,m=>m.transform.position);
            for(float end=Time.unscaledTime+1.5f;Time.unscaledTime<end;){foreach(var hero in PartyController.Current.members)hero.GetComponent<FlightMotor>().Move(new Vector2(0,-1),false,false);yield return null;}
            foreach(var hero in PartyController.Current.members)Assert.Greater(Flat(hero.transform.position,from[hero]),3,$"{hero.character}'s ship couldn't fly out of where it was set down");
            Object.Destroy(crate);Object.Destroy(rock);
        }

        // ------------------------------------------------------------------ the reverse: space beside a ground zone (the Continuance's flight berth)
        [UnityTest] public IEnumerator AHeroOnFootLaunchesIntoTheSpaceBesideAGroundZoneAndDocksBackOnFoot()
        {
            yield return Load("Arena_Ground");
            var zone=ZoneController.Current;
            Vector3 pad=new(0,0,-10),launch=new(150,3,160),foot=new(2,0,-13),flightAt=new(150,3,140);
            var berth=Berth.Declare("TestSpace","The test lane",ZoneKind.SpaceCombat,new Bounds(new Vector3(150,3,150),new Vector3(30,3,30)),pad,launch,foot,flightAt,"TestSpaceDoor",new Vector3(-3,0,-10));
            yield return Frames(2);
            Assert.AreEqual("Launch — The test lane",berth.pad.prompt);StringAssert.StartsWith("Dock — ",berth.launch.prompt);
            bool Flying(CombatActor hero)=>hero.flight&&zone.Kind==ZoneKind.SpaceCombat&&Mathf.Abs(hero.transform.position.y-3)<.05f&&berth.Contains(hero.transform.position);
            Assert.IsFalse(Flying(Lead),"fixture: the lead on the yard's floor already reads as flying the lane");
            Put(Lead,pad+Vector3.back*1.5f);yield return Press(berth.pad,"launching from the ground");Hold();
            Assert.IsTrue(Berth.Docked,"the pad didn't put the party in the lane");
            foreach(var hero in PartyController.Current.members)Assert.IsTrue(Flying(hero),$"{hero.character} isn't flying the lane at its plane (at {hero.transform.position}, flight {hero.flight})");
            Assert.AreEqual("Flight",zone.CameraProfile.name,"the lane isn't seen with the flight camera");Assert.AreEqual(3,zone.FlightPlane,.01f,"the lane's plane isn't its own");
            yield return new WaitForSecondsRealtime(1);var was=Lead.transform.position;
            // North, along the lane: east is the partner's hull, set down beside him.
            for(float end=Time.unscaledTime+1;Time.unscaledTime<end;){Lead.GetComponent<FlightMotor>().Move(new Vector2(0,1),false,false);yield return null;}
            Assert.Greater(Flat(Lead.transform.position,was),1,"the ship in the lane doesn't fly");Assert.AreEqual(3,Lead.transform.position.y,.05f,"a ship flying in the lane left its plane");
            var guard=SaveSystem.SafeSpawn;State.spawn="Arrival";SaveSystem.SafeSpawn=null;try{Saves.Save("autosave",State);}finally{SaveSystem.SafeSpawn=guard;}
            Assert.AreEqual("Arrival",Saves.Load("autosave").spawn,"fixture: a save with no berth's rule doesn't keep the state's spawn");
            State.spawn="Arrival";Saves.Save("autosave",State);Assert.AreEqual("TestSpaceDoor",Saves.Load("autosave").spawn,"a save in the lane names a spawn of the ground zone's other form");
            Put(Lead,launch+Vector3.back*3);yield return Press(berth.launch,"docking back on the ground");Hold();
            Assert.IsFalse(Berth.Docked);Assert.AreEqual(ZoneKind.GroundCombat,zone.Kind);Assert.AreEqual("Ground",zone.CameraProfile.name);
            foreach(var hero in PartyController.Current.members)
            {
                Assert.IsFalse(hero.flight,hero.character+" is still a ship on the ground");Assert.Less(Flat(hero.transform.position,foot),4,$"{hero.character} came down at {hero.transform.position}, not beside the pad");
                Assert.IsFalse(Overlaps(hero),hero.character+" came down inside something solid");
                Assert.IsTrue(Physics.Raycast(hero.transform.position+Vector3.up*.5f,Vector3.down,1,~0,QueryTriggerInteraction.Ignore),hero.character+" came down with no floor under him");
            }
        }

        // ------------------------------------------------------------------ D407: the screen clears on people, not on two hulls through each other
        static bool Ship(CombatActor hero){var form=hero.GetComponent<FormController>();return form.flight!=null&&form.flight.activeInHierarchy;}
        /// <summary>What a hero draws round his root on the plan: a ship's hull, or a person's capsule.</summary>
        static float Reach(CombatActor hero)=>Ship(hero)?HeroCollision.HullRadius(hero.character):hero.GetComponent<CharacterController>().radius;
        /// <summary>Presses <paramref name="prompt"/> as the lead and reads every frame for a second and a half that the screen isn't black: the least room left between the two
        /// heroes' drawn bodies (below nought, they lie through each other), and what was drawn then.</summary>
        static IEnumerator Watched(InteractionPrompt prompt,string what,System.Action<float,string> seen)
        {
            float fade=0;System.Action<float> faded=a=>fade=a;SceneFlow.Current.FadeChanged+=faded;
            float least=float.MaxValue;string worst="nothing seen";var partner=Partner;
            try
            {
                yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.1f);
                PromptService.FindInteraction(Lead.transform.position);
                Assert.AreSame(prompt,PromptService.Interaction,$"{what}: the lead at {Lead.transform.position} isn't offered \"{prompt.prompt}\"");
                Assert.IsTrue(PromptService.TryInteract(),what+": A didn't take the prompt");
                for(float end=Time.unscaledTime+1.5f;Time.unscaledTime<end;)
                {
                    yield return null;
                    if(fade>.99f)continue;
                    float apart=Flat(Lead.transform.position,partner.transform.position),room=apart-Reach(Lead)-Reach(partner);
                    if(room<least){least=room;worst=$"{Lead.character} a {(Ship(Lead)?"ship":"person")} and {partner.character} a {(Ship(partner)?"ship":"person")} {apart:0.00} m apart at a fade of {fade:0.00}, {(Berth.Docked?"docked":"not docked")}";}
                }
                Assert.IsFalse(Berth.Changing,what+": the change never finished");
            }
            finally{SceneFlow.Current.FadeChanged-=faded;}
            TestContext.WriteLine($"D407 {what}: least room {least:0.00} m ({worst})");seen(least,worst);
        }
        /// <summary>D407: at every dock onto a fight's deck the screen cleared on the two ships lying through each other (nov24-recon-h-63 s63/111, -h-65 s65/028). The berth sets
        /// the partner down 2 m beside the lead, right for two people, and the form change it begins under the black closed each ship for .23 s and opened its person .27 s
        /// after, so the deck showed two hulls at a person's spacing. Read on every frame not under a black screen: onto the shelf, off it again, and (the other kind of berth,
        /// the Continuance's) down from the space beside a ground zone onto its floor.</summary>
        [UnityTest] public IEnumerator DockedOnADeckTheScreenClearsOnPeopleBesideEachOtherNotOnTwoHullsThroughEachOther()
        {
            yield return Load("Arena_Flight");
            var berth=Berth.Find(Id);Assert.IsNotNull(berth,"fixture: the yard has no berth");var partner=Partner;
            Put(Lead,Pad+Vector3.back*3);Put(partner,Pad+Vector3.back*3+Vector3.right*HeroCollision.Apart(Lead.character,partner.character));yield return null;
            var faults=new List<string>();
            yield return Watched(berth.pad,"docking onto the shelf",(least,worst)=>{if(least<-.05f)faults.Add($"docking onto the shelf: {worst}, drawn {-least:0.00} m through each other");});
            Assert.IsTrue(Berth.Docked,"the pad didn't put the party on the shelf");Hold();
            yield return new WaitForSecondsRealtime(1);
            Assert.AreEqual(BodyForm.Shaped,Lead.GetComponent<FormController>().Current,"the lead isn't on his feet on the shelf");
            Assert.Less(Flat(Lead.transform.position,partner.transform.position),3,"D406: she wasn't set down beside him on the shelf");
            Put(Lead,Launch+Vector3.right*1.2f);Put(partner,Foot);yield return null;
            yield return Watched(berth.launch,"launching off the shelf",(least,worst)=>{if(least<-.05f)faults.Add($"launching off the shelf: {worst}, drawn {-least:0.00} m through each other");});
            Assert.IsFalse(Berth.Docked,"the launch left the party on the shelf");
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
        [UnityTest] public IEnumerator DownFromTheSpaceBesideAGroundZoneTheScreenClearsOnPeopleNotOnTwoHullsThroughEachOther()
        {
            yield return Load("Arena_Ground");
            Vector3 pad=new(0,0,-10),launch=new(150,3,160),foot=new(2,0,-13),flightAt=new(150,3,140);
            var berth=Berth.Declare("TestSpace","The test lane",ZoneKind.SpaceCombat,new Bounds(new Vector3(150,3,150),new Vector3(30,3,30)),pad,launch,foot,flightAt,"TestSpaceDoor",new Vector3(-3,0,-10));
            yield return Frames(2);
            Put(Lead,pad+Vector3.back*1.5f);yield return Press(berth.pad,"launching from the ground");Hold();
            Assert.IsTrue(Berth.Docked,"the pad didn't put the party in the lane");
            yield return new WaitForSecondsRealtime(1);
            var partner=Partner;Put(Lead,launch+Vector3.back*3);Put(partner,launch+Vector3.back*3+Vector3.right*HeroCollision.Apart(Lead.character,partner.character));yield return null;
            float seen=0;string what="";
            yield return Watched(berth.launch,"docking back on the ground",(least,worst)=>{seen=least;what=worst;});
            Assert.IsFalse(Berth.Docked,"the party wasn't put back on the ground");
            Assert.GreaterOrEqual(seen,-.05f,$"docking back on the ground: {what}, drawn {-seen:0.00} m through each other");
        }
    }
}
