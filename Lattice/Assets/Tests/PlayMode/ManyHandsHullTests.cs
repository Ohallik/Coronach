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

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 5's flight zone as a player meets it, in the built scene: the launch out at the stern dock and the dock home, the harbour line holding a ship on every side, the
    /// four joins Hulljack is fought at built to the combat engineer's contract (docs/maps/ManyHandsHull.md, "Boss arena contract": the coupling drums, the room, the cover, the anchors),
    /// the frame on show at the first join and its door waiting for the claim, the claim haulers over the raft bay, the roof door the frame goes through and the way after it into the
    /// freight hall, the hull after the second part, a save and a Continue, nothing above the plane between the camera and a join; and the freight hall's staging (FightPocket), on a bare
    /// safe floor. What every zone gets from the shared checks is theirs and not repeated here. Coordinates are the map brief's, which are the builder's own (ManyHandsBuilder.Outside).</summary>
    public sealed class ManyHandsHullTests
    {
        readonly List<string> log=new();
        static readonly string[] Heeded={"ENCOUNTER_CLEAR","FIGHT_POCKET"};
        void Logged(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter5");log.Clear();Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Hull(ContentRegistry.Book book)
        {
            // The way after the frame, into the freight hall: shown only once the frame is through the roof door, and what its refused press would name.
            book.Lock("bossdown.Hulljack","salvage frame");
        }

        // ------------------------------------------------------------------ the hull, as the builder lays it (ManyHandsBuilder.Outside: editor code a test can't call)
        const string HullZone="ManyHandsHull",City="ManyHands";
        static readonly Vector2[] Joins={new(-44,-29),new(-44,29),new(44,29),new(44,-30)};
        static readonly Vector2 DoorAt=new(44,-67),DockAt=new(-40,-134),ArrivalAt=new(-30,-126),CheckpointAt=new(-28,-66),DronesAt=new(-58,-84),TugAt=new(62,-198);
        const float XMin=-150,XMax=150,ZMin=-215,ZMax=205,DrumRadius=9.4f;
        static Vector3 P(Vector2 at,float y=1)=>new(at.x,y,at.y);

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static float Flat(Vector3 a,Vector2 b)=>Vector2.Distance(new Vector2(a.x,a.z),b);
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>The story at the launch: the three jobs done, Ilo aboard, the claim read on the radio, and the main quest on its fifth step (cut the frame's feet).</summary>
        static void ToTheHull()
        {
            foreach(string f in new[]{"hands.aboard","hands.ruvaHeard","hands.maruHeard","hands.mooringSettled","hands.ventFixed","hands.chartOpen","hands.iloAboard","hands.claimRead","met.Ilo"})Flag(f);
            State.questSteps["ManyHands"]=5;
        }
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            Assert.IsTrue(Application.CanStreamedLevelBeLoaded(zone),"fixture: "+zone+" isn't built into the game yet");
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            yield return Ready(zone);
        }
        static IEnumerator Ready(string zone)
        {
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            float deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Prepared,"fixture: "+zone+"'s conversations never finished preparing");
            yield return new WaitForSecondsRealtime(.7f);
        }
        static IEnumerator Arrive(string zone,float most=25)
        {
            float deadline=Time.unscaledTime+most;
            while((State.zone!=zone||SceneFlow.Current.Loading)&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual(zone,SceneFlow.Current.Zone,"the party never arrived in "+zone);Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            yield return Ready(zone);
        }
        static IEnumerator SaveQuitAndContinue()
        {
            string zone=State.zone;GameServices.Current.Saves.Save("autosave",State);
            SceneFlow.Current.LoadZone("Title");float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            yield return null;yield return null;
            var title=Object.FindFirstObjectByType<TitleScreen>();Assert.IsNotNull(title,"fixture: the title never came up");
            title.StartGame(true);yield return null;
            yield return Arrive(zone);
        }
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship)ship.Halt();else if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static EncounterVolume Fight(string id)=>All<EncounterVolume>().SingleOrDefault(e=>e.encounterId==id);
        static GameObject Named(string name)=>All<Transform>().Where(t=>t.name==name).Select(t=>t.gameObject).SingleOrDefault();
        static bool IsBody(Collider c)=>c.GetComponentInParent<CombatActor>()!=null||c.GetComponentInParent<Health>()!=null||c is CharacterController;
        /// <summary>Everything solid at the ships' height (y .2 to 2.2) whose nearest point is within <paramref name="reach"/> of a point on the plan: what a frame's sweep or a ship's roll would meet.</summary>
        static List<Collider> SolidWithin(Vector2 at,float reach)
        {
            var centre=P(at,1.2f);
            return Physics.OverlapBox(centre,new Vector3(reach,1,reach),Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Where(c=>!IsBody(c)&&Flat(c.ClosestPoint(centre),at)<reach).ToList();
        }

        // ================================================================== the way in and out
        [UnityTest] public IEnumerator TheLaunchComesOutAtTheSternDockAndTheDockTakesThePartyHome()
        {
            ToTheHull();yield return Zone(HullZone);
            Assert.AreEqual(ZoneKind.SpaceCombat,ZoneController.Current.definition.kind,"the hull isn't flown");
            var arrival=All<SpawnPoint>().Single(s=>s.id=="Arrival");Assert.Less(Flat(arrival.transform.position,ArrivalAt),1.5f,"the hull's arrival isn't beside the stern dock, where the map has it");
            Assert.Less(Flat(Lead.transform.position,ArrivalAt),5,"the launch didn't come out at the stern dock");
            Assert.AreEqual(1f,Lead.transform.position.y,.6f,"the launch didn't come out on the flight plane");
            var dock=All<DockingPad>().Where(d=>d.scene==City).ToArray();Assert.AreEqual(1,dock.Length,"the hull has no dock home to Many Hands, or more than one");
            Assert.AreEqual("Arrival",dock[0].spawn,"the dock doesn't land at the city's dock");StringAssert.Contains("Many Hands",dock[0].prompt);
            Assert.Less(Flat(dock[0].transform.position,DockAt),2,"the dock isn't at the stern, where the map has it");
            Put(Lead,dock[0].transform.position+new Vector3(0,0,-4));yield return Frames(3);
            Assert.IsTrue(dock[0].Available,"the dock home refuses with nothing awake and no fight on");
            dock[0].Interact();yield return Arrive(City);
            var home=All<SpawnPoint>().SingleOrDefault(s=>s.id=="Arrival");Assert.IsNotNull(home,"Many Hands has no Arrival for the dock to land at");
            Assert.Less(Flat(Lead.transform.position,home.transform.position),4,"docking didn't land the party at the city's dock");
        }

        /// <summary>The hull is the hub's outside for the rest of the game, so the third hero flies here too: every arrival (the lead and a partner beside her at the widest pair's
        /// spacing) and every way out has room for the widest hull there is (Idra's cutter, 2.17 m; the other two are under 1.8).</summary>
        [UnityTest] public IEnumerator EveryArrivalAndWayOutOfTheHullHasRoomForTheWidestHull()
        {
            ToTheHull();Flag("bossdown.Hulljack");yield return Zone(HullZone);yield return Frames(3);
            string[] heroes={"Taren","Sela","Idra"};float widest=heroes.Max(HeroCollision.HullRadius);float apart=heroes.SelectMany(a=>heroes.Where(b=>b!=a).Select(b=>HeroCollision.Apart(a,b))).Max();
            Assert.GreaterOrEqual(widest,2.1f,"fixture: the third hero's hull isn't the widest the game knows");
            bool Fits(Vector3 at)=>!Physics.OverlapSphere(new Vector3(at.x,1.8f,at.z),widest,~0,QueryTriggerInteraction.Ignore).Any(c=>!IsBody(c));
            var faults=new List<string>();
            foreach(var spawn in All<SpawnPoint>())
            {
                var at=spawn.transform.position;if(!Fits(at))faults.Add($"{spawn.id}: a {widest:0.00} m hull arriving at {at} is inside something solid");
                if(!Fits(at+Vector3.right*apart))faults.Add($"{spawn.id}: a partner arriving {apart:0.00} m beside the lead at {at} is inside something solid");
            }
            void Reachable(string what,Vector3 at,float range)
            {
                for(float r=0;r<=range;r+=.5f)for(int a=0;a<16;a++)if(Fits(at+Quaternion.Euler(0,a*22.5f,0)*Vector3.forward*r))return;
                faults.Add($"{what}: nowhere within its {range:0.0} m is there room for a {widest:0.00} m hull");
            }
            foreach(var dock in All<DockingPad>())Reachable(dock.prompt,dock.transform.position,dock.range);
            foreach(var warp in All<WarpBeacon>().Where(w=>w.gameObject.activeInHierarchy))Reachable(warp.prompt,warp.transform.position,warp.range);
            // The measure sees a hull that doesn't fit: one set down on the dock's pad.
            Assert.IsFalse(Fits(All<DockingPad>().First().transform.position),"fixture: the room check finds room for a hull inside the dock's pad");
            Assert.IsEmpty(faults,"the widest hull has no room:\n"+string.Join("\n",faults));
        }

        // ================================================================== the harbour line
        [UnityTest] public IEnumerator TheHarbourLineHoldsAShipUnderBoostAndRollOnEverySideAndAtEveryCorner()
        {
            ToTheHull();yield return Zone(HullZone);var ship=Lead;var escapes=new List<string>();
            var line=All<MeshFilter>().SingleOrDefault(f=>f.name=="Harbour line");Assert.IsNotNull(line,"the limits are drawn by nothing: there is no harbour line");
            var lamps=line.sharedMesh.vertices.Select(v=>line.transform.TransformPoint(v)).Where(v=>v.y>.4f&&v.y<1.6f).ToArray();
            Assert.Greater(lamps.Length,100,"fixture: the harbour line's strung lights");
            // Out along each side (a third and two thirds of the way, and the middle), then each corner diagonally, from fourteen metres inside: boosting, then a roll into it.
            var starts=new List<(Vector3 at,Vector3 outward,string where)>();
            foreach(float t in new[]{.2f,.5f,.8f})
            {
                float x=Mathf.Lerp(XMin,XMax,t),z=Mathf.Lerp(ZMin,ZMax,t);
                starts.Add((new Vector3(x,1,ZMin+14),Vector3.back,$"the south side at x {x:0}"));starts.Add((new Vector3(x,1,ZMax-14),Vector3.forward,$"the north side at x {x:0}"));
                starts.Add((new Vector3(XMin+14,1,z),Vector3.left,$"the west side at z {z:0}"));starts.Add((new Vector3(XMax-14,1,z),Vector3.right,$"the east side at z {z:0}"));
            }
            foreach(var (cx,cz) in new[]{(XMin,ZMin),(XMax,ZMin),(XMax,ZMax),(XMin,ZMax)})starts.Add((new Vector3(cx-Mathf.Sign(cx)*14,1,cz-Mathf.Sign(cz)*14),new Vector3(Mathf.Sign(cx),0,Mathf.Sign(cz)).normalized,$"the corner at ({cx:0}, {cz:0})"));
            static float Out(Vector3 p)=>Mathf.Max(Mathf.Max(XMin-p.x,p.x-XMax),Mathf.Max(ZMin-p.z,p.z-ZMax));
            foreach(var (at,outward,where) in starts)
            {
                Put(ship,at);yield return null;
                for(float end=Time.unscaledTime+3;Time.unscaledTime<end;){ship.motor.Move(new Vector2(outward.x,outward.z),true,false);yield return null;}
                var stop=ship.transform.position;float past=Out(stop);
                if(past>1)escapes.Add($"{where}: boosting outward took the ship {past:0.0} m past the harbour line");
                if(past<-6)escapes.Add($"{where}: the ship stopped {-past:0.0} m short of the harbour line: something else holds it");
                if(!lamps.Any(l=>Flat(l,stop)<4.5f))escapes.Add($"{where}: the ship stopped at {stop} with no lit line of the harbour within 4.5 m of it: stopped by nothing visible");
                ship.motor.Dash(outward,8);for(int i=0;i<20;i++){ship.motor.Move(Vector2.zero,false,false);yield return null;}
                past=Out(ship.transform.position);if(past>1)escapes.Add($"{where}: a roll into the line took the ship {past:0.0} m past it");
            }
            Assert.IsEmpty(escapes,"the ship leaves the harbour, or stops short of it:\n"+string.Join("\n",escapes));
            // The measure sees an escape: with the east limit's box switched off the same push carries the ship out past the line.
            var east=All<BoxCollider>().Single(b=>b.name=="East limit");east.enabled=false;Physics.SyncTransforms();
            Put(ship,new Vector3(XMax-14,1,0));yield return null;
            for(float end=Time.unscaledTime+3;Time.unscaledTime<end;){ship.motor.Move(Vector2.right,true,false);yield return null;}
            Assert.Greater(Out(ship.transform.position),1,"fixture: with the east limit off, the push didn't carry the ship past the line, so the check above proves nothing");
            east.enabled=true;
        }

        // ================================================================== the arena
        [UnityTest] public IEnumerator TheFourJoinsAreBuiltToTheArenaContract()
        {
            ToTheHull();yield return Zone(HullZone);var faults=new List<string>();
            var joins=HullJoin.All.Where(j=>j!=null).OrderBy(j=>j.index).ToArray();
            Assert.AreEqual(new[]{1,2,3,4},joins.Select(j=>j.index).ToArray(),"the hull doesn't have the four joins the frame takes, numbered 1 to 4");
            for(int n=0;n<4;n++)
            {
                var join=joins[n];string at=$"join {n+1}";
                if(Flat(join.transform.position,Joins[n])>1)faults.Add($"{at} stands at {join.transform.position}, not where the map has it ({Joins[n]})");
                if(Mathf.Abs(join.transform.position.y-1)>.3f)faults.Add($"{at}'s anchor is at y {join.transform.position.y:0.0}, not on the plane");
                // The coupling drum: drawn, its top just under the plane over r 9, and nothing of it solid.
                var drum=All<MeshRenderer>().SingleOrDefault(r=>r.name=="Coupling drum "+(n+1));
                if(drum==null){faults.Add($"{at} has no coupling drum for the frame to stand on");continue;}
                var b=drum.bounds;
                if(b.max.y<.2f||b.max.y>.62f)faults.Add($"{at}'s drum tops out at y {b.max.y:0.00}: the contract wants .2 to .6, just under the plane");
                if(b.extents.x<9||b.extents.z<9||Flat(b.center,Joins[n])>.6f)faults.Add($"{at}'s drum covers {b.size.x:0.0} x {b.size.z:0.0} m about {b.center}: the contract wants r 9 round the join");
                if(drum.GetComponentsInChildren<Collider>().Length>0)faults.Add($"{at}'s drum is solid: it's drawn only");
                // The room: nothing solid at the plane within r 14.
                foreach(var c in SolidWithin(Joins[n],14))faults.Add($"{at}: {c.name} is solid at the ships' height {Flat(c.ClosestPoint(P(Joins[n])),Joins[n]):0.0} m from the join's middle, inside the frame's room");
                // Its cover: two or three pieces, solid through the ships' height, thick enough, between r 16 and r 26, none on the camera's side.
                var cover=All<BoxCollider>().Where(c=>!c.isTrigger&&c.transform.parent!=null&&c.GetComponentsInParent<Transform>().Any(t=>t.name.StartsWith("Cover "+(n+1)+" "))).GroupBy(c=>c.GetComponentsInParent<Transform>().First(t=>t.name.StartsWith("Cover "+(n+1)+" ")).name).ToArray();
                if(cover.Length<2||cover.Length>3)faults.Add($"{at} has {cover.Length} pieces of cover: the contract wants two or three");
                foreach(var piece in cover)
                {
                    var at2=piece.OrderBy(c=>Flat(c.ClosestPoint(P(Joins[n])),Joins[n])).First();var near=Flat(at2.ClosestPoint(P(Joins[n])),Joins[n]);
                    var reach=piece.Where(c=>c.bounds.min.y<=.2f&&c.bounds.max.y>=2.2f).ToArray();
                    if(reach.Length==0)faults.Add($"{at}: {piece.Key} isn't solid from y .2 up to 2.2");
                    if(!piece.Any(c=>Mathf.Min(c.size.x*c.transform.lossyScale.x,c.size.z*c.transform.lossyScale.z)>=1.2f))faults.Add($"{at}: {piece.Key} is thinner than 1.2 m: a line slips past it");
                    if(near<16||near>26)faults.Add($"{at}: {piece.Key} comes to {near:0.0} m of the join: the contract wants r 16 to 26");
                    var centre=piece.Select(c=>c.bounds.center).Aggregate(Vector3.zero,(a,c)=>a+c)/piece.Count();var off=new Vector2(centre.x,centre.z)-Joins[n];
                    if(off.y<0&&Mathf.Abs(off.x)<Mathf.Abs(off.y)*.36f)faults.Add($"{at}: {piece.Key} stands due south of the join, on the camera's side");
                }
            }
            // The tug's side, and the way the frame goes.
            var tug=ClaimTug.All.Where(t=>t!=null).ToArray();Assert.AreEqual(1,tug.Length,"the hull has no ClaimTug for the frame's drones to come from, or more than one");
            if(Flat(tug[0].transform.position,TugAt)>25||Mathf.Abs(tug[0].transform.position.y-1)>.3f)faults.Add($"the tug's anchor is at {tug[0].transform.position}, not at its drone rack on the plane");
            var exit=BossExit.For("Hulljack");
            if(exit==null)faults.Add("there is no BossExit for Hulljack: it would leave straight away from the party");
            else{if(Flat(exit.transform.position,DoorAt)>1||Mathf.Abs(exit.transform.position.y-1)>.3f)faults.Add($"Hulljack's exit is at {exit.transform.position}, not over the roof door on the plane");if(!exit.sinks)faults.Add("Hulljack doesn't go down through the roof door");}
            // The room check finds what it is for: a solid box set down at the plane six metres from the first join.
            var planted=new GameObject("Planted box",typeof(BoxCollider));planted.transform.position=P(Joins[0]+Vector2.right*6);planted.GetComponent<BoxCollider>().size=Vector3.one*2;Physics.SyncTransforms();
            Assert.IsTrue(SolidWithin(Joins[0],14).Any(c=>c.gameObject==planted),"fixture: the room check doesn't see a box planted inside the room");Object.Destroy(planted);
            Assert.IsEmpty(faults,"the arena isn't built to its contract:\n"+string.Join("\n",faults));
        }

        [UnityTest] public IEnumerator TheFrameWaitsOnShowAtTheFirstJoinAndItsDoorWaitsForTheClaim()
        {
            ToTheHull();Flag("hands.claimRead",false);yield return Zone(HullZone);
            var door=Fight("Hulljack");Assert.IsNotNull(door,"the hull has no fight called Hulljack, which the main quest's step names");
            Assert.AreEqual("hands.claimRead",door.requiredFlag,"the frame's door doesn't wait for the claim to be read");
            Assert.AreEqual("Hulljack",door.checkpointSpawn,"the frame's door is no checkpoint");
            var checkpoint=All<SpawnPoint>().SingleOrDefault(s=>s.id=="Hulljack");Assert.IsNotNull(checkpoint,"the hull has no spawn for the frame's checkpoint");
            Assert.Less(Flat(checkpoint.transform.position,CheckpointAt),1.5f,"the checkpoint isn't where the map has it");
            Assert.Less(Vector3.Distance(checkpoint.transform.position,door.transform.position),60,"the checkpoint is a long way from its door");
            var box=door.GetComponent<BoxCollider>();Assert.IsFalse(box.bounds.Contains(checkpoint.transform.position),"the checkpoint is inside the door: a retry would start the fight before the party has its bearings");
            foreach(var join in Joins)Assert.IsTrue(box.bounds.Contains(P(join)),$"the door's box doesn't cover the join at {join}");
            var post=door.spawners[0];Assert.Less(Flat(post.transform.position,Joins[0]),1,"the frame isn't set down at the first join");Assert.AreEqual(1,post.transform.position.y,.3f,"the frame isn't set on the plane");
            Assert.AreEqual(180,post.transform.eulerAngles.y,1,"the frame doesn't face the approach");Assert.IsTrue(post.faceAsPlaced);
            Assert.IsNotNull(door.previewPoint,"the frame isn't on show before its fight");Assert.LessOrEqual(Flat(door.previewPoint.position,Joins[0]),door.previewRadius,"the frame's preview doesn't take in the join it is on");
            // On show and inert: the frame stands at the first join from the moment the hull loads.
            yield return Frames(5);var shown=All<EnemyBrain>().Where(e=>e.definition!=null&&e.definition.id==door.spawners[0].definition.id&&e.gameObject.activeInHierarchy).ToArray();
            Assert.AreEqual(1,shown.Length,"the frame isn't on show at the first join");Assert.IsNotNull(shown[0].GetComponent<DormantEnemy>(),"the frame on show isn't inert");
            // Unread, the claim keeps the door shut; read, a ship over the joins starts the fight and saves at the checkpoint.
            Put(Lead,P(Joins[0]+new Vector2(0,-22)));yield return new WaitForSecondsRealtime(.6f);
            Assert.IsFalse(door.Started,"the frame's fight started before the claim was read");
            Flag("hands.claimRead");Put(Lead,P(Joins[0]+new Vector2(0,-21)));yield return new WaitForSecondsRealtime(.6f);
            Assert.IsTrue(door.Started,"a ship over the joins with the claim read didn't start the frame's fight");
            Assert.AreEqual("Hulljack",State.spawn,"starting the frame's fight didn't move the respawn to its checkpoint");
        }

        [UnityTest] public IEnumerator TheClaimHaulersOverTheRaftBayWakeAsAShipComesOverTheHoldAndStayClearedThroughAContinue()
        {
            ToTheHull();yield return Zone(HullZone);
            var drones=Fight("Hull_Drones");Assert.IsNotNull(drones,"nothing waits over the raft bay");
            Assert.Less(Flat(drones.transform.position,DronesAt),2,"the claim haulers aren't over the raft bay, where the map has them");
            Assert.AreEqual("hands.claimRead",drones.requiredFlag);Assert.IsNotNull(drones.previewPoint,"the haulers aren't on show before they wake");
            Assert.AreEqual(3,drones.spawners.Sum(s=>s.count),"there aren't three haulers over the hold");
            Assert.IsTrue(drones.spawners.All(s=>s.definition!=null&&s.definition.id=="ClaimHauler"),"the haulers over the hold aren't the chapter's ClaimHauler: "+string.Join(", ",drones.spawners.Select(s=>s.definition!=null?s.definition.id:"nothing")));
            Assert.IsFalse(drones.Started,"fixture: the haulers are awake already");
            Put(Lead,P(DronesAt+new Vector2(0,-12)));yield return new WaitForSecondsRealtime(.6f);
            Assert.IsTrue(drones.Started,"a ship over the hold didn't wake the haulers");
            var awake=All<EnemyBrain>().Where(e=>e.Health.Alive&&e.definition!=null&&e.definition.id=="ClaimHauler").ToArray();Assert.AreEqual(3,awake.Length,"three haulers didn't wake");
            foreach(var hauler in awake)hauler.Health.Receive(new DamagePacket{amount=999999,type=DamageType.Kinetic,source=Lead.Health});
            float deadline=Time.unscaledTime+6;while(!Has("clear.Hull_Drones")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(Has("clear.Hull_Drones"),"clearing the haulers didn't count");Assert.IsTrue(log.Any(l=>l.StartsWith("ENCOUNTER_CLEAR Hull_Drones")),"the haulers' clear wasn't logged");
            yield return new WaitForSecondsRealtime(.5f);
            yield return SaveQuitAndContinue();
            Assert.AreEqual(HullZone,State.zone);drones=Fight("Hull_Drones");yield return Frames(3);
            Assert.IsTrue(drones.Cleared||drones.Started&&drones.Remaining==0,"the cleared haulers came back after a Continue");
            Assert.IsFalse(All<EnemyBrain>().Any(e=>e.Health.Alive&&e.definition!=null&&e.definition.id=="ClaimHauler"),"a cleared hauler is flying again after a Continue");
        }

        // ================================================================== after the frame
        [UnityTest] public IEnumerator OnceTheFrameIsThroughTheRoofDoorTheWayAfterItTakesThePartyIntoTheFreightHall()
        {
            ToTheHull();yield return Zone(HullZone);
            var follow=All<WarpBeacon>().SingleOrDefault(w=>w.scene==City);Assert.IsNotNull(follow,"there is no way after the frame into Many Hands");
            Assert.IsTrue(Named("Roof door shut").activeInHierarchy,"the freight hall's roof door isn't shut before the fight");
            Assert.IsFalse(Has("hands.hullCleared"),"fixture: the hull is cleared already");
            // Shut, the door is only a door: it doesn't name where it would lead, a press is refused and says what opens it, and nothing loads.
            Assert.AreEqual("bossdown.Hulljack",follow.requiredFlag,"the way after the frame doesn't wait for the frame to go through");
            StringAssert.DoesNotContain("Follow",follow.prompt,"the shut roof door offers to follow a frame that hasn't gone through it");
            Put(Lead,follow.transform.position+new Vector3(0,0,-3.5f));yield return Frames(3);follow.Interact();yield return Frames(2);
            Assert.IsFalse(SceneFlow.Current.Loading,"the shut roof door let the party through");StringAssert.Contains("salvage frame",follow.prompt,"a press on the shut roof door doesn't say what opens it");
            Flag("bossdown.Hulljack");yield return Frames(3);
            Assert.IsTrue(follow.gameObject.activeInHierarchy,"the way after the frame isn't there once it's through the door");
            StringAssert.StartsWith("Follow Varn in",follow.prompt,"once the frame is through, the door still says it is shut");
            Assert.IsFalse(Named("Roof door shut").activeInHierarchy,"the roof door still shows shut once the frame has torn through it");Assert.IsTrue(Named("Roof door torn").activeInHierarchy,"the torn roof door doesn't show");
            Assert.IsTrue(Has("hands.hullCleared"),"the hull isn't cleared once the frame is gone");
            Assert.Less(Flat(follow.transform.position,DoorAt),1.5f,"the way after the frame isn't at the roof door");Assert.AreEqual("FreightHall",follow.spawn,"the way after the frame doesn't land at the freight hall's checkpoint");
            StringAssert.Contains("Many Hands",follow.prompt);StringAssert.Contains("freight hall",follow.prompt);
            Put(Lead,follow.transform.position+new Vector3(0,0,-3.5f));yield return Frames(3);
            Assert.IsTrue(follow.Available,"the way after the frame refuses with the hull clear");
            follow.Interact();yield return Arrive(City);
            var hall=All<SpawnPoint>().SingleOrDefault(s=>s.id=="FreightHall");Assert.IsNotNull(hall,"Many Hands has no FreightHall spawn for the way after the frame (docs/maps/ManyHands.md, Freight hall contract)");
            Assert.Less(Flat(Lead.transform.position,hall.transform.position),4,"following the frame in didn't land the party at the freight hall's checkpoint");
            Assert.AreEqual("FreightHall",State.spawn,"the load into the freight hall didn't save at its checkpoint");
        }

        [UnityTest] public IEnumerator AfterTheSecondPartTheRoofDoorIsPatchedAndTheBreachSealedWithTheCuttersLatched()
        {
            ToTheHull();foreach(string f in new[]{"bossdown.Hulljack","bossdown.DessikVarn","hands.dessikYielded","hands.breachSealed"})Flag(f);yield return Zone(HullZone);yield return Frames(3);
            Assert.IsTrue(Named("Roof door patched").activeInHierarchy,"the roof door isn't patched after the fight");
            Assert.IsFalse(Named("Roof door torn").activeInHierarchy,"the roof door still gapes after the fight");Assert.IsFalse(Named("Roof door shut").activeInHierarchy,"the roof door came back whole");
            Assert.IsFalse(All<WarpBeacon>().Any(w=>w.scene==City&&w.gameObject.activeInHierarchy),"the way after the frame is still offered once the second part is won");
            var sealedBreach=Named("The breach sealed");Assert.IsNotNull(sealedBreach);Assert.IsTrue(sealedBreach.activeInHierarchy,"the breach isn't shown sealed after the fight");
            Assert.GreaterOrEqual(sealedBreach.GetComponentsInChildren<Transform>().Count(t=>t.name=="Steady Hand cutter"),2,"the Steady Hand's two cutters aren't latched at the breach");
            Assert.IsFalse(Named("The cut at join 1").activeInHierarchy,"the frame's cut still glows once it's beaten");
            // And before it: no seal, no cutters.
            Flag("bossdown.DessikVarn",false);yield return Frames(3);Assert.IsFalse(sealedBreach.activeInHierarchy,"the breach shows sealed before it has happened");
        }

        [UnityTest] public IEnumerator SavingOnTheHullAndContinuingComesBackOnTheHullAsItStood()
        {
            ToTheHull();yield return Zone(HullZone);
            Put(Lead,P(new Vector2(-40,-100)));yield return Frames(2);
            yield return SaveQuitAndContinue();
            Assert.AreEqual(HullZone,State.zone,"a Continue from the hull came back somewhere else");Assert.IsTrue(Has("hands.claimRead"),"the claim was lost to a save");
            Assert.AreEqual(1f,Lead.transform.position.y,.6f,"the party came back off the flight plane");
            Assert.IsTrue(Named("Roof door shut").activeInHierarchy,"the roof door came back open before the frame has been fought");
            var door=Fight("Hulljack");Assert.IsFalse(door.Started,"the frame's fight started by itself after a Continue");
            yield return Frames(3);Assert.IsTrue(All<EnemyBrain>().Any(e=>e.definition!=null&&e.definition.id==door.spawners[0].definition.id&&e.GetComponent<DormantEnemy>()!=null),"the frame isn't on show at its join after a Continue");
        }

        // ================================================================== the camera
        [UnityTest] public IEnumerator NothingAboveThePlaneHidesAFightAtAJoinOrTheRoofDoorFromTheFlightCamera()
        {
            ToTheHull();yield return Zone(HullZone);
            var profile=GameCatalog.Find<CameraProfile>("Flight");var toCamera=-(Quaternion.Euler(profile.pitch,profile.yaw,0)*Vector3.forward);
            var places=Joins.Select((j,i)=>(j,"join "+(i+1))).Append((DoorAt,"the roof door")).ToArray();
            List<string> Hidden()
            {
                var hidden=new List<string>();var views=All<MeshRenderer>();
                foreach(var (centre,what) in places)foreach(float r in new[]{0f,4f,8f,12f})for(int a=0;a<(r==0?1:8);a++)
                {
                    var p=P(centre+(Vector2)(Quaternion.Euler(0,0,a*45)*Vector2.up)*r,1.6f);
                    // Toward the camera from the play view out to the farthest a fight draws it back (about 3.3 times).
                    foreach(var hit in Physics.RaycastAll(p,toCamera,profile.distance*3.3f,~0,QueryTriggerInteraction.Ignore))
                        if(!IsBody(hit.collider)&&hit.point.y>1.6f)hidden.Add($"{what}: {hit.collider.name} stands on the camera's line to {p} at {hit.point}");
                    foreach(var view in views)
                    {
                        // Scenery only: the frame itself stands on its join and hides what is behind it by being there (the contract keeps the cover out of the 15 m north of each join).
                        if(view==null||!view.enabled||!view.gameObject.activeInHierarchy||view.bounds.max.y<2.2f||view.GetComponentInParent<CombatActor>()!=null||view.GetComponentInParent<Health>()!=null
                            ||view.GetComponentInParent<EnemyBrain>()!=null||view.name=="Harbour line")continue;
                        if(view.bounds.IntersectRay(new Ray(p,toCamera),out float along)&&along<profile.distance*3.3f&&along>.5f&&view.bounds.size.x<40&&view.bounds.size.z<40)
                        {
                            var entry=p+toCamera*along;if(entry.y>2.2f)hidden.Add($"{what}: {view.name} (drawn up to y {view.bounds.max.y:0.0}) stands on the camera's line to {p}");
                        }
                    }
                }
                return hidden.Distinct().ToList();
            }
            var found=Hidden();
            Assert.IsEmpty(found,"something above the plane hides a fight from the flight camera:\n"+string.Join("\n",found.Take(30)));
            // The measure sees a hiding piece: a mast eight metres over the plane, planted five metres south of the first join, stands on the camera's line to it.
            var planted=GameObject.CreatePrimitive(PrimitiveType.Cube);planted.name="Planted mast";planted.transform.position=P(Joins[0]+Vector2.down*5,5);planted.transform.localScale=new Vector3(1.4f,8,1.4f);Physics.SyncTransforms();
            Assert.IsTrue(Hidden().Any(h=>h.Contains("Planted mast")),"fixture: the camera check doesn't see a mast planted between the camera and the first join");
            Object.Destroy(planted);
        }

        // ================================================================== the freight hall's staging, on a bare safe floor
        [UnityTest] public IEnumerator AStagedHallShowsItsWorkFightAndAfterAndIsACombatPlaceOnlyWhileItsFightIsOn()
        {
            // A safe zone of chapter 1's, standing in for the town: the staging and the pocket are components, and the room they are set round is the test's.
            yield return Zone("TallowDrift");
            Assert.AreEqual(ZoneKind.GroundSafe,ZoneController.Current.Kind,"fixture: Tallow Drift isn't a safe zone");
            var at=Lead.transform.position;
            GameObject Set(string name){var go=new GameObject(name);go.transform.position=at;return go;}
            var before=Set("Working hall");var during=Set("Hall staged for the fight");var after=Set("Hall at work again");var alarm=Set("Cabin alarm");
            var room=new GameObject("Test freight hall",typeof(BoxCollider));room.transform.position=at;var box=room.GetComponent<BoxCollider>();box.size=new Vector3(16,6,16);box.isTrigger=true;
            var stage=room.AddComponent<FightPocket>();stage.from="test.frameIn";stage.until="test.frameBeaten";stage.before=new[]{before};stage.during=new[]{during};stage.after=new[]{after};
            stage.alarm=new[]{alarm};stage.alarmBoss="SentinelHusk";stage.alarmPhase=1;
            yield return Frames(3);
            Assert.AreEqual(0,stage.State);Assert.IsTrue(before.activeSelf&&!during.activeSelf&&!after.activeSelf,"before its fight the room doesn't show the working hall alone");
            Assert.IsFalse(alarm.activeSelf,"the alarm is on with no fight");
            Assert.AreEqual(ZoneKind.GroundSafe,ZoneController.Current.Kind,"the room is a combat place before its fight");
            Flag("test.frameIn");yield return Frames(3);
            Assert.AreEqual(1,stage.State);Assert.IsTrue(!before.activeSelf&&during.activeSelf&&!after.activeSelf,"in its fight's window the room doesn't show the fight's staging alone");
            Assert.IsFalse(alarm.activeSelf,"the alarm sounds before its boss is in the phase that sounds it");
            Assert.AreEqual(ZoneKind.GroundCombat,ZoneController.Current.Kind,"standing in the room in its fight's window isn't a combat place");
            Assert.AreEqual(BodyForm.Shaped,ZoneController.Current.Form,"the party doesn't take its fighting form in the room");Assert.IsFalse(ZoneController.Current.InSafePocket,"a fight pocket reads as a safe pocket");
            // An enemy in the room fights: under the old reading of a safe pocket it went home and waited, and the hall's fight would never have begun.
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("SentinelHusk"),at+new Vector3(0,0,4));yield return null;
            float deadline=Time.unscaledTime+6;while(!enemy.Telegraphing&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(enemy.Telegraphing,"an enemy in the fight pocket never moved to strike the party");
            Object.Destroy(enemy.gameObject);yield return null;
            // The alarm: a boss of the name it listens for, in the phase it waits on, sounds it.
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),at+new Vector3(0,0,30));stage.alarmBoss=boss.GetComponent<Health>().id;boss.Passive=true;yield return Frames(3);
            Assert.IsNotNull(boss.GetComponent<BossController>(),"fixture: Hessa has no boss controller");Assert.IsTrue(alarm.activeSelf,"the alarm doesn't sound with its boss in the phase that sounds it");
            Object.Destroy(boss.gameObject);yield return Frames(3);Assert.IsFalse(alarm.activeSelf,"the alarm rings on with its boss gone");
            // Out of the room the town is the town; back in, a combat place again; the fight won, safe even inside.
            Put(Lead,at+new Vector3(14,0,0));yield return Frames(3);Assert.AreEqual(ZoneKind.GroundSafe,ZoneController.Current.Kind,"leaving the room didn't give the town back");
            Put(Lead,at);yield return Frames(3);Assert.AreEqual(ZoneKind.GroundCombat,ZoneController.Current.Kind,"coming back into the room isn't a combat place again");
            Flag("test.frameBeaten");yield return Frames(3);
            Assert.AreEqual(2,stage.State);Assert.IsTrue(!before.activeSelf&&!during.activeSelf&&after.activeSelf,"after its fight the room doesn't show the hall at work again");
            Assert.AreEqual(ZoneKind.GroundSafe,ZoneController.Current.Kind,"the room stays a combat place after its fight is won");
            Assert.IsTrue(log.Any(l=>l=="FIGHT_POCKET_STAGE Test freight hall 1")&&log.Any(l=>l=="FIGHT_POCKET_STAGE Test freight hall 2"),"the staging's states weren't logged: "+string.Join(", ",log));
            Assert.IsTrue(log.Any(l=>l=="FIGHT_POCKET Test freight hall in")&&log.Any(l=>l=="FIGHT_POCKET Test freight hall out"),"the pocket's comings and goings weren't logged: "+string.Join(", ",log));
            Flag("test.frameIn",false);Flag("test.frameBeaten",false);
        }
    }
}
