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
    /// <summary>The Returning Passage as a player meets it, in the built scene (chapter 10's last flight; rule: keep to the lanterns). Places are the map brief's
    /// (docs/maps/ReturningPassage.md), which are the builder's own (LastMooringBuilder.Passage): the road a hull flies and its solid lantern-lined edges, nothing over the plane and the
    /// convoy three metres and more under it, the convoy flowing north and closing to the lanterns on Orrin's line, everything that moves held through a conversation (D275), the tug's
    /// fight round her with her line far from its drones, the tow, every line where the script puts it, Sela's turn (D286), and the cut to SEVEN WEEKS ON and the neighbourhood.</summary>
    public sealed class ReturningPassageTests
    {
        readonly List<string> log=new();
        static readonly string[] Heeded={"CUT_AWAY","TIME_PLATE","TUG_","CONVOY_","ALONGSIDE_","PASSAGE_CALL"};
        void Logged(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();Time.timeScale=1;SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter10");log.Clear();Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;Application.logMessageReceived-=Logged;
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            TimePlate.Clear();GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the zone's places (LastMooringBuilder.Passage; docs/maps/ReturningPassage.md)
        const string Passage="ReturningPassage";const float Plane=1,Wide=30,Idra=2.17f;
        static readonly Vector3[] Line={new(0,1,-40),new(0,1,170),new(-36,1,450),new(20,1,760),new(0,1,1050),new(0,1,1170),new(0,1,1240),new(0,1,1300),new(0,1,1345)};
        static readonly float[] Halves={30,30,30,30,30,30,14,44,44};
        const float IloS=170,TugS=760,TugFightS=752,LineS=769,TowS=785,TurnS=1060,FarEndS=1080,MouthS=1288,EndS=1393;
        static Vector3 On(float s,float side=0)=>ConvoyFlow.At(Line,s,side);
        static float Half(float s){float run=0;for(int i=1;i<Line.Length;i++){var d=Line[i]-Line[i-1];d.y=0;float leg=d.magnitude;if(s<=run+leg||i==Line.Length-1)return Mathf.Lerp(Halves[i-1],Halves[i],Mathf.Clamp01((s-run)/leg));run+=leg;}return Wide;}

        // ------------------------------------------------------------------ fixtures
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Zone(string spawn="Arrival",bool lift=true)
        {
            SceneFlow.Current.LoadZone(Passage,spawn);float deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,Passage+" never finished loading");
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            // Out of the way of every crossing and every check of the air: two hulls parked over the bore, far above it.
            if(lift)foreach(var actor in PartyController.Current.members)actor.transform.position+=Vector3.up*300;
            yield return null;yield return null;Physics.SyncTransforms();
        }
        /// <summary>The state after the turn, without the turn itself (its cut has been made): the tug and her fight wait up the road.</summary>
        static void Turned(){Flag("end.namesBegun");Flag("end.farEnd");Flag("end.passageRun");Flag("end.turnedBack");}
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship)ship.Halt();
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        /// <summary>Whether a hull of Idra's reach, at the plane, meets anything solid here (her sphere's middle is .8 over the ship's root). Bodies (a drone, a hero) aren't the air's.</summary>
        static bool Solid(Vector3 at)=>Physics.OverlapSphere(new Vector3(at.x,Plane+.8f,at.z),Idra,~0,QueryTriggerInteraction.Ignore).Any(c=>c.GetComponentInParent<Health>()==null);
        static IEnumerator Seconds(float seconds){for(float until=Time.time+seconds;Time.time<until;)yield return null;}
        static IEnumerator Settled(float most=20){float deadline=Time.unscaledTime+most;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;yield return null;}

        // ------------------------------------------------------------------ the ways in, and the road
        [UnityTest] public IEnumerator ThePassageOpensInFlightOnTheRoadWithEveryCheckpointItsSectionsRestartAt()
        {
            yield return Zone();
            Assert.AreEqual(ZoneKind.SpaceCombat,ZoneController.Current.definition.kind,"the Returning Passage is a flight zone");
            var flow=All<ConvoyFlow>().SingleOrDefault();Assert.IsNotNull(flow,"the passage has no convoy running it");
            Assert.AreEqual(Line.Length,flow.path.Length,"the convoy doesn't run the map's line");for(int i=0;i<Line.Length;i++)Assert.Less(Vector3.Distance(Line[i],flow.path[i]),.1f,"the convoy's line isn't the map's at point "+i);
            foreach(var (id,s,north) in new[]{("Arrival",40f,true),("Turn",TurnS,false),("TugReach",800f,false),("Tow",TowS,true)})
            {
                var spawn=All<SpawnPoint>().FirstOrDefault(p=>p.id==id);Assert.IsNotNull(spawn,"the passage has no spawn "+id);
                Assert.Less(Flat(spawn.transform.position,On(s)),1,id+" isn't on the road's middle where the map says");Assert.AreEqual(Plane,spawn.transform.position.y,.05f,id+" isn't at the plane");
                ConvoyFlow.At(Line,s,0,out var ahead);Assert.Greater(Vector3.Dot(spawn.transform.forward,north?ahead:-ahead),.9f,id+" doesn't face "+(north?"up the road":"back down it"));
                Assert.IsFalse(Solid(spawn.transform.position),id+" is inside something solid");
            }
            var reach=All<SpawnPoint>().First(p=>p.id=="TugReach");var fight=All<EncounterVolume>().First(e=>e.encounterId=="Passage_Tug");
            Assert.IsFalse(fight.GetComponent<BoxCollider>().bounds.Contains(reach.transform.position),"a fall at the tug's fight retries inside it");
            Assert.AreEqual("TugReach",fight.checkpointSpawn,"entering the tug's fight doesn't make its door the checkpoint");
            var line=All<DiscoveryPoint>().Single(p=>p.flag=="end.lineOn");Assert.AreEqual("Tow",line.saveSpawn,"taking the line doesn't make the tow's start the checkpoint");
        }

        [UnityTest] public IEnumerator TheRoadIsOpenAirForIdrasHullAndItsLanternLinedEdgesAreSolid()
        {
            Turned();yield return Zone("Turn");
            // The check first, on a box stood in the road: it must be found.
            var probe=GameObject.CreatePrimitive(PrimitiveType.Cube);probe.transform.position=On(300,4);probe.transform.localScale=new Vector3(2,4,2);Physics.SyncTransforms();
            Assert.IsTrue(Solid(On(300,4)),"fixture: a box in the road isn't found by the check");Object.Destroy(probe);yield return null;Physics.SyncTransforms();
            var tug=All<TugTow>().Single();var tugAt=tug.transform.position;
            var blocked=new List<string>();
            for(float s=4;s<EndS-4;s+=5)
            {
                // Where the road widens or narrows its wall is laid in 10 m chords, which stand up to a metre inside the line between them.
                float h=Half(s)-1-Idra-.6f-(Mathf.Abs(Half(s+5)-Half(s-5))>.5f?1.2f:0);
                for(float side=-h;side<=h+.01f;side+=Mathf.Max(1,h/4))
                {
                    var p=On(s,side);if(Flat(p,tugAt)<8+Idra)continue;
                    if(Solid(p))blocked.Add($"s {s:0} side {side:0}");
                }
            }
            CollectionAssert.IsEmpty(blocked.Take(12),"a hull meets something solid in the road's open air ("+blocked.Count+" points)");
            var open=new List<string>();
            for(float s=6;s<EndS-6;s+=8)foreach(int side in new[]{-1,1})if(!Physics.CheckSphere(On(s,side*(Half(s)+1))+Vector3.up*.8f,1,~0,QueryTriggerInteraction.Ignore))open.Add($"s {s:0} {(side<0?"port":"starboard")}");
            CollectionAssert.IsEmpty(open.Take(12),"the road's edge lets a ship out ("+open.Count+" points)");
            foreach(var (where,at) in new[]{("behind the arrival",On(0)),("past the rendezvous",On(EndS))})Assert.IsTrue(Physics.CheckSphere(at+Vector3.up*.8f,1.2f,~0,QueryTriggerInteraction.Ignore),"nothing stops a ship "+where);
            // Every stretch of edge has the lantern chain drawn on it: something lit within 24 m of every point of it, at the plane or under it.
            var lamps=All<Light>().Where(l=>l.type==LightType.Point&&l.GetComponentInParent<ConvoyFlow>()==null&&l.GetComponentInParent<Alongside>()==null&&l.transform.position.y<Plane).Select(l=>l.transform.position).ToList();
            var dark=new List<string>();for(float s=10;s<EndS-10;s+=30)foreach(int side in new[]{-1,1}){var p=On(s,side*Half(s));if(!lamps.Any(l=>Flat(l,p)<44))dark.Add($"s {s:0} {(side<0?"port":"starboard")}");}
            CollectionAssert.IsEmpty(dark,"a stretch of the road's edge has no lantern near it");
        }

        // ------------------------------------------------------------------ the camera's side
        [UnityTest] public IEnumerator NothingIsDrawnOverTheRoadAndTheConvoyHangsThreeMetresUnderThePlane()
        {
            Turned();yield return Zone("Turn");
            var tug=All<TugTow>().Single();
            List<string> Over()
            {
                var found=new List<string>();
                foreach(var r in All<Renderer>())
                {
                    if(!r.gameObject.activeInHierarchy||!r.enabled||r is ParticleSystemRenderer||r is LineRenderer)continue;
                    if(r.GetComponentInParent<CombatActor>(true)!=null||r.GetComponentInParent<TMPro.TMP_Text>()!=null||r.transform.IsChildOf(tug.transform))continue;
                    var b=r.bounds;if(b.max.y<=Plane+1.5f)continue;
                    float s=ConvoyFlow.Along(Line,b.center,out float side);if(s<-20||s>EndS+20||Mathf.Abs(side)>Half(s)+24)continue;
                    found.Add(r.name+" at "+b.center+" to y "+b.max.y.ToString("0.0"));
                }
                return found;
            }
            var post=GameObject.CreatePrimitive(PrimitiveType.Cylinder);post.name="A post on the camera's side";post.transform.position=On(700,-4)+Vector3.up*2;post.transform.localScale=new Vector3(2,3,2);
            Assert.IsTrue(Over().Any(f=>f.StartsWith("A post")),"fixture: a post over the road isn't found");Object.Destroy(post);yield return null;
            CollectionAssert.IsEmpty(Over().Take(10),"something is drawn over the plane in the road or by it");
            Assert.LessOrEqual(ZoneKit_Top(tug.gameObject),Plane+3.6f,"the tug stands taller than cover at the plane");
            // The convoy hangs under the plane by three metres and more: drawn only, nothing a ship could seem to meet or be hidden by.
            var flow=All<ConvoyFlow>().Single();
            var high=flow.groups.SelectMany(g=>g.GetComponentsInChildren<Renderer>()).Where(r=>!(r is ParticleSystemRenderer)&&r.bounds.max.y>Plane-2.5f).Select(r=>r.name+" to y "+r.bounds.max.y.ToString("0.0")).ToList();
            CollectionAssert.IsEmpty(high.Take(10),"a hull of the convoy comes within 2.5 m of the plane");
            Assert.IsFalse(flow.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled),"something of the convoy is solid: it would push a hero");
            foreach(var a in All<Alongside>())
            {
                Assert.IsFalse(a.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled),a.name+" is solid");
                Assert.IsFalse(a.GetComponentsInChildren<Renderer>().Any(r=>!(r is ParticleSystemRenderer)&&r.bounds.max.y>Plane-1),a.name+" comes up to the plane");
            }
        }
        static float ZoneKit_Top(GameObject go){float top=float.NegativeInfinity;foreach(var r in go.GetComponentsInChildren<Renderer>())if(!(r is LineRenderer))top=Mathf.Max(top,r.bounds.max.y);return top;}

        // ------------------------------------------------------------------ the convoy's flow
        [UnityTest] public IEnumerator TheConvoyFlowsNorthAndClosesToTheLanternsOnOrrinsLineLeavingTheMiddleEmpty()
        {
            Flag("end.namesBegun");yield return Zone();
            var flow=All<ConvoyFlow>().Single();Assert.Greater(flow.groups.Length,60,"the convoy is a few hulls, not a river");
            var kinds=new[]{"District: the market","District: the repair quarter","District: the residential crescent","District: the tug dock","Convoy: TallowStationHull","Convoy: SeedBarge","Convoy: ReedRaft",
                "Convoy: CrawlTread","Convoy: LanternTower","Convoy: KeptLonghouse","Convoy: RibArch","Convoy: PatrolCutter","Convoy: Hauler","Convoy: CompactCutter","Convoy: OutpostHab","Convoy: SeedHouse"};
            var missing=kinds.Where(k=>!flow.GetComponentsInChildren<Transform>(true).Any(t=>t.name==k&&t.GetComponentInChildren<Renderer>()!=null)).ToList();
            Assert.IsTrue(flow.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("Convoy: RingSegment")),"the Halo's ring isn't in the convoy");
            CollectionAssert.IsEmpty(missing,"a place the player knows isn't in the convoy");
            Assert.IsNotNull(All<Transform>().FirstOrDefault(t=>t.name=="Many Hands"),"Many Hands isn't waiting at the rendezvous");
            var was=flow.along.ToArray();var at=flow.groups.Select(g=>g.position).ToArray();
            yield return Seconds(2);
            for(int i=0;i<flow.groups.Length;i++)
            {
                float moved=flow.along[i]-was[i];if(moved<0)moved+=flow.to-flow.from;
                Assert.Greater(moved,flow.pace[i]*1.5f,flow.groups[i].name+" isn't flowing north");
            }
            Assert.Greater(Vector3.Distance(flow.groups[0].position,at[0]),5,"the groups' places move but the groups don't");
            // Orrin's line: every group under the road closes to the lanterns at its own edge, and the middle is left empty.
            List<string> InTheMiddle(float keep)
            {
                var found=new List<string>();
                for(int i=0;i<flow.groups.Length;i++)
                {
                    float s=flow.along[i];if(s<20||s>1180)continue;
                    foreach(var r in flow.groups[i].GetComponentsInChildren<Renderer>()){if(r is ParticleSystemRenderer)continue;float side;ConvoyFlow.Along(Line,r.bounds.center,out side);
                        // A hull under the middle: its drawn box reaches within keep of the line.
                        float reach=Mathf.Max(r.bounds.extents.x,r.bounds.extents.z)*.7f;if(Mathf.Abs(side)-reach<keep){found.Add(flow.groups[i].name+" at s "+s.ToString("0"));break;}}
                }
                return found;
            }
            Assert.IsNotEmpty(InTheMiddle(8),"fixture: nothing of the convoy runs under the road's middle before Orrin's line");
            Flag("end.passageRun");Flag("end.turnedBack");Time.timeScale=4;
            yield return Seconds(9.5f);Time.timeScale=1;
            Assert.AreEqual(1,flow.Closed,.01f,"the convoy hasn't closed to the lanterns eight seconds after Orrin's line");
            CollectionAssert.IsEmpty(InTheMiddle(8).Take(8),"the convoy leaves something under the middle of the road");
            Assert.IsTrue(log.Any(l=>l.StartsWith("CONVOY_CLOSES")),"the convoy didn't say it closed");
            yield return Zone("Turn");flow=All<ConvoyFlow>().Single();
            Assert.AreEqual(1,flow.Closed,.01f,"a zone opened after Orrin's line has the convoy back in the middle of the road");
        }

        // ------------------------------------------------------------------ D275: a conversation holds everything that moves
        [UnityTest] public IEnumerator AConversationHoldsTheConvoyTheYoungMotherOfDoorsIdraAndTheTow()
        {
            Turned();Flag("end.lineOn");Flag("end.tugFetched");yield return Zone("Tow",false);
            var flow=All<ConvoyFlow>().Single();var tug=All<TugTow>().Single();var idra=All<Alongside>().Single(a=>a.pace>0);
            Assert.IsTrue(tug.OnTheLine,"fixture: loaded with the line on, the tug isn't on it");
            var input=GameServices.Current.Input;bool before=input.Blocked;
            // Everything that moves by itself in the passage, and how far it went in a second and a half of blocked controls. The lead is moved by hand while the controls are
            // held, as a ship could still drift, so the tow has somewhere to go.
            IEnumerator Talk(bool held,Dictionary<string,float> moved)
            {
                flow.heldByTalk=held;tug.heldByTalk=held;foreach(var a in All<Alongside>())a.heldByTalk=held;
                var groups=flow.groups.Select(g=>g.position).ToArray();var tugAt=tug.transform.position;var idraAt=idra.transform.position;
                var others=All<Alongside>().Where(a=>a.pace<=0).ToDictionary(a=>a,a=>a.transform.position);
                input.Blocked=true;Put(Lead,Lead.transform.position+On(TowS+30)-On(TowS));
                for(float until=Time.time+1.5f;Time.time<until;){input.Blocked=true;yield return null;}
                moved["the convoy"]=flow.groups.Select((g,i)=>Vector3.Distance(g.position,groups[i])).Max();
                moved["the tug on her line"]=Vector3.Distance(tug.transform.position,tugAt);
                moved["Idra on the flank"]=Vector3.Distance(idra.transform.position,idraAt);
                foreach(var (a,from) in others.Select(p=>(p.Key,p.Value)))moved[a.name]=Vector3.Distance(a.transform.position,from);
                input.Blocked=before;
            }
            try
            {
                // The old way first (nothing heeds the talk): everything goes on moving through it, and the check must say so.
                var loose=new Dictionary<string,float>();yield return Talk(false,loose);
                foreach(var (what,far) in loose.Select(p=>(p.Key,p.Value)))if(what!="Mother of Doors, far off"&&what!="The young Choir, alongside")Assert.Greater(far,.5f,"fixture: with nothing held, "+what+" didn't move in a talk");
                var held=new Dictionary<string,float>();yield return Talk(true,held);
                foreach(var (what,far) in held.Select(p=>(p.Key,p.Value)))Assert.Less(far,.05f,what+" moved "+far.ToString("0.00")+" m while a conversation held the controls");
                // The controls back, they go on.
                var tugAt=tug.transform.position;var first=flow.groups[0].position;
                for(float until=Time.time+1.2f;Time.time<until;)yield return null;
                Assert.Greater(Vector3.Distance(flow.groups[0].position,first),2,"the convoy doesn't go on once the talk is over");
                Assert.Greater(Vector3.Distance(tug.transform.position,tugAt),1,"the tug doesn't follow the lead on once the talk is over");
            }
            finally{input.Blocked=before;flow.heldByTalk=true;tug.heldByTalk=true;foreach(var a in All<Alongside>())a.heldByTalk=true;}
            // The young and Mother of Doors keep station by the lead (gone on ahead after the turn): before it they are held as well.
            yield return Cleanup();yield return Boot();Flag("end.namesBegun");yield return Zone("Arrival",false);
            var swimmers=All<Alongside>().Where(a=>a.pace<=0).ToList();Assert.AreEqual(2,swimmers.Count,"fixture: the young and Mother of Doors aren't both keeping station");
            input=GameServices.Current.Input;before=input.Blocked;
            try
            {
                foreach(bool held in new[]{false,true})
                {
                    foreach(var a in swimmers)a.heldByTalk=held;var from=swimmers.ToDictionary(a=>a,a=>a.transform.position);
                    input.Blocked=true;Put(Lead,On(260));
                    for(float until=Time.time+1.5f;Time.time<until;){input.Blocked=true;yield return null;}
                    foreach(var a in swimmers){float far=Vector3.Distance(a.transform.position,from[a]);if(held)Assert.Less(far,.05f,a.name+" moved "+far.ToString("0.00")+" m while a conversation held the controls");else Assert.Greater(far,.5f,"fixture: with nothing held, "+a.name+" didn't move");}
                    input.Blocked=before;Put(Lead,On(40));yield return null;
                }
            }
            finally{input.Blocked=before;foreach(var a in swimmers)a.heldByTalk=true;}
        }

        // ------------------------------------------------------------------ the tug
        [UnityTest] public IEnumerator TheTugsFightIsRoundHerWithItsMouthOpenToTheWayBackAndHerLineFarFromItsDrones()
        {
            yield return Zone();
            var tug=All<TugTow>().Single();var fight=All<EncounterVolume>().Single(e=>e.encounterId=="Passage_Tug");
            Assert.IsFalse(tug.gameObject.activeInHierarchy,"the tug is on the road before Bryn has called (D283: what isn't there yet isn't drawn)");
            Assert.AreEqual("end.passageRun",fight.requiredFlag,"the tug's fight can wake before the turn");
            Turned();yield return Zone("Turn");
            tug=All<TugTow>().Single();fight=All<EncounterVolume>().Single(e=>e.encounterId=="Passage_Tug");
            Assert.IsTrue(tug.gameObject.activeInHierarchy,"the tug isn't waiting up the road after the turn");Assert.Less(Flat(tug.transform.position,On(TugS)),1.5f,"the tug isn't where the map says");
            Assert.IsTrue(tug.solid!=null&&tug.solid.enabled&&Solid(tug.transform.position),"the tug isn't solid at the plane while she waits: she is cover, and a harpoon line breaks on her");
            var box=fight.GetComponent<BoxCollider>();var b=box.bounds;
            Assert.IsTrue(box.bounds.Contains(new Vector3(tug.transform.position.x,Plane,tug.transform.position.z)),"the fight isn't round the tug");
            Assert.GreaterOrEqual(box.size.x,40,"the tug's fight is narrower than 40 m");Assert.GreaterOrEqual(box.size.z,30,"the tug's fight is shallower than 30 m");
            Assert.IsTrue(b.min.y<Plane&&b.max.y>Plane,"the tug's fight doesn't span the plane");
            Assert.AreEqual(3,fight.spawners.Sum(s=>s.count),"the tug isn't held by three drones");
            ConvoyFlow.At(Line,TugFightS,0,out var ahead);var across=Vector3.Cross(Vector3.up,ahead);
            foreach(var s in fight.spawners)
            {
                Assert.IsTrue(s.faceAsPlaced,s.name+" doesn't wake as placed");Assert.Greater(Vector3.Dot(s.transform.forward,-ahead),.9f,s.name+" doesn't face away from her, down the road");
                Assert.IsTrue(s.definition!=null&&(s.definition.id=="StillwaterTowDrone"||s.definition.id=="TowDrone"),s.name+" isn't a tow-drone");
                Assert.Less(Vector3.Dot(s.transform.position-tug.transform.position,ahead),-8,s.name+" isn't off her stern, south of her");
            }
            // Its mouth faces the way the party comes back (north): the longest run of its north face a hull can fly in through. The check first, on a face walled but for 4 m.
            float Mouth(){float run=0,best=0;var face=box.transform.TransformPoint(box.center+Vector3.forward*(box.size.z*.5f-Idra-.5f));
                for(float x=-box.size.x*.5f+Idra;x<=box.size.x*.5f-Idra;x+=1){var p=face+across*x;if(Solid(p)){run=0;continue;}run++;best=Mathf.Max(best,run);}return best;}
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetPositionAndRotation(box.transform.TransformPoint(box.center+Vector3.forward*(box.size.z*.5f-Idra-.5f))+across*4,Quaternion.LookRotation(ahead));wall.transform.localScale=new Vector3(56,4,2);Physics.SyncTransforms();
            Assert.Less(Mouth(),9,"fixture: a face walled but for a gap a hull can't take passes for open");Object.Destroy(wall);yield return null;Physics.SyncTransforms();
            Assert.GreaterOrEqual(Mouth(),9,"the tug's fight can't be flown into from the way back");
            var line=All<DiscoveryPoint>().Single(p=>p.flag=="end.lineOn");
            Assert.AreEqual("TugLine",line.report);Assert.AreEqual("clear.Passage_Tug",line.requiredFlag,"her line is offered before the drones are off her");
            Assert.IsFalse(line.Wanted,"her line is offered with the drones still on her");
            foreach(var s in fight.spawners)Assert.Greater(Flat(line.transform.position,s.transform.position),15,"her line is within 15 m of where a drone wakes");
            Assert.Less(Flat(line.transform.position,tug.transform.position),9,"her line isn't at the tug");Assert.IsFalse(Solid(line.transform.position),"her line's prompt has no room for a hull");
            Flag("clear.Passage_Tug");yield return null;Assert.IsTrue(line.Wanted,"her line isn't offered once the drones are off her");
        }

        [UnityTest] public IEnumerator TheTugFollowsTheLeadOnHerLineAndIsNoLongerSolid()
        {
            Turned();Flag("clear.Passage_Tug");Flag("end.lineOn");Flag("end.tugFetched");yield return Zone("Tow",false);
            var tug=All<TugTow>().Single();Assert.IsTrue(tug.OnTheLine,"loaded with the line on, she isn't on it");
            Assert.IsFalse(tug.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger),"on the line she is still solid: she would stop the party she follows");
            Assert.Less(Flat(tug.transform.position,Lead.transform.position),14,"loaded with the line on, she isn't behind the lead");Assert.IsTrue(tug.line.enabled,"her line isn't drawn");
            Put(Lead,On(TowS+60));yield return Seconds(3);
            float gap=Flat(tug.transform.position,Lead.transform.position);Assert.Less(gap,15,"she doesn't follow the lead up the road");Assert.Greater(gap,7,"she rides on top of the lead");
            Assert.Less(Vector3.Distance(tug.line.GetPosition(1),tug.transform.TransformPoint(tug.bow)),.5f,"her line isn't made fast at her bow");
        }

        // ------------------------------------------------------------------ what is said, and where
        [UnityTest] public IEnumerator EveryLineOfTheRunIsWhereTheScriptPutsIt()
        {
            yield return Zone();
            DiscoveryPoint Crossing(string node){var p=All<DiscoveryPoint>().FirstOrDefault(d=>d.dialogueNode==node);Assert.IsNotNull(p,"nothing plays "+node);Assert.IsTrue(p.startsOnCrossing,node+" isn't played by crossing");return p;}
            void Across(DiscoveryPoint p,float s,string what)
            {
                var b=p.GetComponent<BoxCollider>();Assert.Less(Flat(p.transform.position,On(s)),2,what+" isn't where the map says");Assert.GreaterOrEqual(b.size.x,2*Half(s)-.5f,what+" can be flown round");
                Assert.IsTrue(b.bounds.min.y<=Plane&&b.bounds.max.y>=Plane,what+" isn't at the plane");
            }
            var ilo=Crossing("IloFirstName");Assert.AreEqual("end.namesBegun",ilo.flag);Assert.AreEqual("Ilo",ilo.dialogueSpeaker);Across(ilo,IloS,"Ilo's first name");
            var far=Crossing("BrynFallsBehind");Assert.AreEqual("FarEnd",far.report,"the far end doesn't report step 10's arrow");Assert.AreEqual("Sela",far.dialogueSpeaker);Across(far,FarEndS,"the far end");
            var mouth=Crossing("OrrinRendezvous");Assert.AreEqual("FarMouth",mouth.report);Assert.AreEqual("end.tugFetched",mouth.requiredFlag,"the rendezvous can be made without the tug");
            Assert.AreEqual("Orrin",mouth.dialogueSpeaker);Across(mouth,MouthS,"the far mouth");
            var line=All<AfterTalk>().FirstOrDefault(t=>t.node=="BrynOnTheLine");Assert.IsNotNull(line,"nothing plays BrynOnTheLine");
            Assert.AreEqual("end.lineOn",line.flag);Assert.AreEqual("end.tugFetched",line.heardFlag);Assert.AreEqual("Sela",line.speaker);
            var turn=All<CutAway>().SingleOrDefault(c=>c.flag=="end.passageRun");Assert.IsNotNull(turn,"nothing turns Sela back on Bryn's call");
            CollectionAssert.AreEquivalent(new[]{"Taren","Sela"},turn.pair,"the pair out after the turn isn't Sela and Taren (D286)");Assert.AreEqual(Passage,turn.scene);Assert.AreEqual("Turn",turn.spawn);
            var cut=All<CutAway>().SingleOrDefault(c=>c.flag=="end.rendezvous");Assert.IsNotNull(cut,"nothing cuts to the epilogue on the rendezvous");
            Assert.AreEqual("SEVEN WEEKS ON",cut.plate);Assert.AreEqual("NeighbourhoodAfloat",cut.scene);Assert.AreEqual("Arrival",cut.spawn);
            // The radio and the barks: every key of the manifest's for the passage, each pool's lines by their own places, and each said by somebody who has it.
            var calls=All<PassageCalls>().Single().calls;
            var wanted=new (string who,string key,int count)[]{("","passage",1),("","youngchoir",1),("Pell","hail.count",3),("Ilo","hail.names",6),("Orrin","hail.board",3),("Mira","hail.manifest",2),
                ("Dorrow","hail.step",2),("Hal","hail.cradle",1),("Neve","hail.rear",2),("Bryn","hail.lead",1),("","wake",1)};
            foreach(var (who,key,count) in wanted)
            {
                var these=calls.Where(c=>c.key==key).ToList();Assert.AreEqual(count,these.Count,key+" isn't said "+count+" times");
                CollectionAssert.AreEquivalent(Enumerable.Range(0,count),these.Select(c=>c.index),key+"'s lines aren't each said by their own place in it");
                foreach(var c in these){Assert.AreEqual(who,c.speaker,key+" is said by the wrong voice");Assert.IsNotNull(BarkService.Line(who==""?"Taren":who,key),(who==""?"Taren":who)+" has no "+key);}
            }
            Assert.IsTrue(calls.Single(c=>c.key=="wake").back,"the wake isn't said flying back up the road");
            // In the order the run flies them, Bryn's before her tug fails.
            Assert.Less(calls.Single(c=>c.key=="hail.lead").along,FarEndS,"Bryn's lead is said after her call");
        }

        [UnityTest] public IEnumerator ACountSaidInTurnStaysInStepWhateverWasSaidBefore()
        {
            Flag("end.namesBegun");yield return Zone("Arrival",false);
            string heard=null;void Hear(string who,string line){if(who=="Pell")heard=line;}
            BarkService.Spoken+=Hear;
            try
            {
                // The old way first: said by turn, Pell's count comes round to whatever follows what was said last this session (here the second), not the bend's own.
                const string First="Two short, one long. Here. This way.";
                BarkService.PlayAt("Pell","hail.count",0,true);heard=null;
                BarkService.Play("Pell","hail.count",true);var byTurn=heard;
                Assert.IsNotNull(byTurn,"fixture: Pell's count isn't heard at all");Assert.AreNotEqual(First,byTurn,"fixture: said by turn after the first, the count isn't a different line");
                // The lead past bend one: the run's calls before it come first (the passage, the young), each 4.2 s apart, then Pell's.
                heard=null;Put(Lead,On(200));
                float deadline=Time.time+18;while(heard==null&&Time.time<deadline)yield return null;
                Assert.IsNotNull(heard,"Pell says nothing at bend one");Assert.AreEqual(First,heard,"Pell's first count isn't the first at bend one");
            }
            finally{BarkService.Spoken-=Hear;}
        }

        // ------------------------------------------------------------------ the turn (D286) and the cut
        [UnityTest] public IEnumerator SelaTurnsBackWithTarenWhoeverWasOutAndTheZoneStartsAgainAtTheTurn()
        {
            var state=GameServices.Current.State;
            CollectionAssert.AreEquivalent(new[]{"Taren","Idra"},Deployment.Of(state).Select(m=>m.id),"fixture: chapter 10 doesn't arrive with Taren and Idra out");
            Flag("end.namesBegun");Flag("end.farEnd");Flag("end.passageRun");yield return Zone("Arrival",false);
            // The cut: held while the convoy closes, then the reload at Turn.
            float deadline=Time.unscaledTime+3;while(!log.Any(l=>l.StartsWith("CUT_AWAY_HOLD"))&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(log.Any(l=>l.StartsWith("CUT_AWAY_HOLD")),"the turn doesn't hold the party once Bryn's call has closed");
            Assert.IsTrue(GameServices.Current.Input.Blocked,"the party isn't held for the turn");
            deadline=Time.unscaledTime+20;while(!log.Any(l=>l.StartsWith("CUT_AWAY ")&&l.Contains("ReturningPassage/Turn"))&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(log.Any(l=>l.StartsWith("CUT_AWAY ")),"the turn never made its cut");
            yield return null;yield return Settled(30);
            state=GameServices.Current.State;
            Assert.AreEqual(Passage,state.zone);Assert.AreEqual("Turn",state.spawn,"the turn doesn't start the zone again at Turn");Assert.IsTrue(Has("end.turnedBack"));
            CollectionAssert.AreEquivalent(new[]{"Taren","Sela"},Deployment.Of(state).Select(m=>m.id),"after the turn the pair out isn't Sela and Taren");
            CollectionAssert.AreEquivalent(new[]{"Taren","Sela"},PartyController.Current.members.Select(m=>m.character),"the field doesn't fly Sela and Taren after the turn");
            Assert.AreEqual("Taren",Lead.character,"a lead who is one of the pair doesn't keep the lead");
            Assert.Less(Flat(Lead.transform.position,On(TurnS)),4,"the party isn't at the turn");
            log.Clear();yield return Seconds(1.5f);Assert.IsFalse(log.Any(l=>l.StartsWith("CUT_AWAY")),"the turn is made again in the zone it started");
        }

        [UnityTest] public IEnumerator TheRendezvousHoldsThePartyPutsUpSevenWeeksOnAndLoadsTheNeighbourhood()
        {
            Turned();foreach(var f in new[]{"clear.Passage_Tug","end.lineOn","end.tugFetched","end.mouthReached","end.rendezvous"})Flag(f);
            yield return Zone("Tow",false);
            float deadline=Time.unscaledTime+3;while(!log.Any(l=>l.StartsWith("CUT_AWAY_HOLD"))&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(GameServices.Current.Input.Blocked,"the party isn't held after Orrin's last word");
            deadline=Time.unscaledTime+8;while(!log.Any(l=>l.StartsWith("TIME_PLATE"))&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(log.Contains("TIME_PLATE SEVEN WEEKS ON"),"no SEVEN WEEKS ON");
            yield return null;Assert.Greater(TimePlate.Alpha(true),0,"the plate isn't up");
            yield return Settled(40);
            Assert.AreEqual("NeighbourhoodAfloat",GameServices.Current.State.zone,"the cut doesn't load the neighbourhood");Assert.AreEqual("Arrival",GameServices.Current.State.spawn);
            Assert.AreEqual("NeighbourhoodAfloat",SceneFlow.Current.Zone);
            // The plate holds over the load and then goes, uncovering the place.
            Assert.Greater(TimePlate.Alpha(false),0,"the plate is gone before the neighbourhood is in");
            deadline=Time.unscaledTime+8;while(TimePlate.Alpha(false)>0&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual(0,TimePlate.Alpha(false),"the plate never goes");
        }

        // ------------------------------------------------------------------ the look (D390)
        /// <summary>nov23-whole s68/105: a flat unshaded pink band crossed the whole frame (a rib of the bore drawn in the unlit glow) and the road's markers were brown discs under flat
        /// cream bars. A rib is the bore's lit tissue now, with a current of its own light carried round it by the convoy's flow; a marker is a float with height and a lantern whose glass
        /// is amber under the bloom (glass times the glow's gain: a second channel at .75 or over is drawn near white). Each predicate is shown to hold of the capture's state first.</summary>
        [UnityTest] public IEnumerator TheBoresRibsHaveFormAndTheRoadsLanternsAreThingsThatFloat()
        {
            yield return Zone();
            bool Unlit(Material m)=>m!=null&&m.IsKeywordEnabled("_GLOW_ON");
            const float GlowGain=1.15f;   // passage-glow's _GlowParams.x (LastMooringBuilder.Passage.GlowGain)
            Assert.AreEqual(GlowGain,All<Renderer>().First(r=>r.name.StartsWith("The road's lantern glass")).sharedMaterial.GetVector("_GlowParams").x,.001f,"fixture: the glow's gain");
            bool Pale(Color c){var v=new[]{c.r,c.g,c.b}.Select(x=>x*GlowGain).OrderByDescending(x=>x).ToArray();return v[1]>=.75f;}
            bool Flat(Renderer r)=>r.bounds.size.y<.3f;
            Renderer Named(string name)=>All<Renderer>().FirstOrDefault(r=>r.name==name);
            // The ribs: lit, shaded.
            var lanternGlass=All<Renderer>().Where(r=>r.name.StartsWith("The road's lantern glass")).ToList();Assert.IsNotEmpty(lanternGlass,"fixture: the road's lantern glass");
            Assert.IsTrue(Unlit(lanternGlass[0].sharedMaterial),"fixture: the glow the capture's ribs were drawn in reads as unlit");
            var ribs=Named("The bore's ribs");Assert.IsNotNull(ribs,"the bore has no ribs");
            Assert.IsFalse(Unlit(ribs.sharedMaterial),"the bore's ribs are drawn in the unlit glow: one flat band across the frame");
            // Their current: moved by the flow.
            var flow=All<ConvoyFlow>().Single();var current=Named("The ribs' currents");
            Assert.IsNotNull(current,"the ribs carry no current of light");Assert.IsTrue(flow.streams.Contains(current),"the ribs' current isn't carried by the convoy's flow");
            var block=new MaterialPropertyBlock();float Offset(){current.GetPropertyBlock(block);return block.GetVector("_BaseMap_ST").z;}
            float then=Offset();yield return Seconds(1.2f);float now=Offset();
            TestContext.WriteLine($"PASSAGE_RIB_CURRENT offset then={then:0.000} now={now:0.000}");
            Assert.Greater(Mathf.Abs(now-then),.02f,"the ribs' current stands still");
            // The markers: floats with height, glass that isn't drawn white.
            var disc=GameObject.CreatePrimitive(PrimitiveType.Quad);disc.transform.rotation=Quaternion.Euler(90,0,0);
            Assert.IsTrue(Flat(disc.GetComponent<Renderer>()),"fixture: a disc laid flat reads as flat");Object.Destroy(disc);
            Assert.IsTrue(Pale(new Color(1f,.7f,.32f)),"fixture: the capture's lantern glass reads as near white");
            var floats=All<Renderer>().Where(r=>r.name.StartsWith("The road's lantern floats")).ToList();Assert.IsNotEmpty(floats,"the road has no lantern floats");
            CollectionAssert.IsEmpty(floats.Where(Flat).Select(r=>r.name),"a stretch of the road's lantern floats is flat discs");
            var pale=new List<string>();
            foreach(var r in lanternGlass)
            {
                var mesh=r.GetComponent<MeshFilter>().sharedMesh;Assert.IsTrue(mesh.isReadable,"fixture: "+r.name+"'s mesh can be read");
                pale.AddRange(mesh.colors.Where(Pale).Distinct().Select(c=>r.name+" "+c));
            }
            CollectionAssert.IsEmpty(pale.Take(6),"a lantern's glass is drawn near white under the bloom");
        }
    }
}
