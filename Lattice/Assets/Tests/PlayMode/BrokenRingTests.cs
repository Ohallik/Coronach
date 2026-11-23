using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The Broken Ring as a player meets it, in the built scene (chapter 9's last zone; rule: keep beside a district, its lit water is calm and its bulk is cover, and the danger runs
    /// down the marked channel between them). Places are the map brief's (docs/maps/BrokenRing.md), which are the builder's own (HomeBuilder.Ring): the arrival in the market's lee, the
    /// districts lifting out of the ring and keeping with their escort, the channel's debris marked before it comes, the formation the Cutter's door waits on, the walls, the pairs and
    /// their cover, and the close: the tug, Bryn, the districts leaving for the convoy and the way home opening.</summary>
    public sealed class BrokenRingTests
    {
        readonly List<string> heard=new(),log=new(),spoken=new();
        void Talked(string node,string speaker,System.Action<bool> done)=>heard.Add(node);
        static readonly string[] Heeded={"SECTION_","RING_FORMED","WRECK_STRUCK","AFTER_TALK","FIRST_SIGHT","TOW_"};
        void Logged(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        void Said(string who,string line)=>spoken.Add(who+": "+line);
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();Time.timeScale=1;SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter9");heard.Clear();log.Clear();spoken.Clear();
            Npc.TalkRequested+=Talked;Application.logMessageReceived+=Logged;BarkService.Spoken+=Said;
            // Bryn's two lines at the launch are said once a save, in turn (RadioHail, BarkService's pools). In play nothing else says them, so a session reaching the Ring starts her
            // pool at its first line; the tests before this one in the same run had moved it on (its first case heard "Market district, you're drifting" first).
            var turn=typeof(BarkService).GetField("turn",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)?.GetValue(null) as Dictionary<string,int>;
            Assert.IsNotNull(turn,"fixture: the bark pools' turns");turn.Remove("Bryn/hail.ring");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;
            Npc.TalkRequested-=Talked;Application.logMessageReceived-=Logged;BarkService.Spoken-=Said;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Ring(ContentRegistry.Book book)
        {
            // The way home off the Ring waits on Bryn's tow ("Hold the Ring first: the Debris Cutter is still hunting the districts").
            book.Lock("home.cutterDisabled","Debris Cutter");
        }

        // ------------------------------------------------------------------ the Ring's places (HomeBuilder.Ring; docs/maps/BrokenRing.md)
        const float Plane=1,Wide=12,Long=17,WallX=46,SouthZ=-18,Bound=60,ExitHalf=14,ExitEnd=382,Idra=2.17f,LaneRadius=30;
        static readonly Vector3 Arrival=new(-15,1,2),Checkpoint=new(0,1,240),C=new(0,1,280),HomeWarp=new(0,1,368),Tug=new(-64,1,280),Pair1=new(-16,1,118),Pair2=new(16,1,188),Cover1=new(-20,1,134),Cover2=new(20,1,172);
        static readonly Dictionary<string,Vector3> Stations=new(){["market"]=new(-42,1,296),["repair"]=new(42,1,296),["crescent"]=new(-42,1,264),["dock"]=new(42,1,264)};
        static readonly Dictionary<string,string> Names=new(){["market"]="the market",["repair"]="the repair quarter",["crescent"]="the residential crescent",["dock"]="the tug dock"};
        static Vector3 Lane(int k){float a=k*45*Mathf.Deg2Rad;return new Vector3(Mathf.Sin(a)*LaneRadius,1,280+Mathf.Cos(a)*LaneRadius);}

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static int Step(string quest)=>State.questSteps.TryGetValue(quest,out var step)?step:-1;
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>The story at the Ring's door: the launch heard and the main quest at its Cutter step.</summary>
        static void ToTheRing(){Flag("home.launched");Flag("home.coronachWalked");RpgServices.Quests.Start("HouseLeaves");State.questSteps["HouseLeaves"]=13;}
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Prepared,"fixture: "+zone+"'s conversations never finished preparing");
            yield return new WaitForSecondsRealtime(.7f);
        }
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship)ship.Halt();
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        /// <summary>Both ships held at a point: the lead there, the partner a hull's width to starboard.</summary>
        static void Hold(Vector3 at){Put(Lead,at);var partner=PartyController.Current.Partner;if(partner!=null)Put(partner,at+Vector3.right*4.4f);}
        static SectionFlight Section(string id)=>All<SectionFlight>().Single(s=>s.districtId==id);
        static WreckTrack Channel=>All<WreckTrack>().Single(t=>t.trackName=="The channel");
        static Vector3? Goal()=>Object.FindFirstObjectByType<ObjectiveHud>().Goal;
        static IEnumerator Settle(){float deadline=Time.unscaledTime+40;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;Assert.IsFalse(DialogueSystem.Current.Running,"a conversation never ended");yield return new WaitForSecondsRealtime(.3f);}
        /// <summary>A collider's solid in its own space (the district's root's, which never turns): its mesh's box, or its box. Read off the shape, so a cover switched off under the
        /// plane still says what it is.</summary>
        static Bounds Local(Collider c)=>c is MeshCollider m&&m.sharedMesh!=null?m.sharedMesh.bounds:c is BoxCollider b?new Bounds(b.center,b.size):default;
        /// <summary>The colliders the Ring's limits are made of (HomeBuilder.Ring.Walls: every box under "Ring limits").</summary>
        static bool IsWall(Collider c)=>c!=null&&c.transform.parent!=null&&c.transform.parent.name=="Ring limits";
        static IEnumerable<Collider> WallsAt(Vector3 p,float radius)=>Physics.OverlapSphere(p,radius,~0,QueryTriggerInteraction.Ignore).Where(IsWall);

        // ================================================================== the zone as it opens
        [UnityTest] public IEnumerator TheRingOpensInTheMarketsLeeWithItsWaysItsDistrictsItsChannelAndTheCuttersDoor()
        {
            ToTheRing();yield return Zone("BrokenRing");
            var zone=ZoneController.Current.definition;Assert.AreEqual(ZoneKind.SpaceCombat,zone.kind);Assert.AreEqual(1,zone.flightPlane);Assert.AreEqual(9,zone.chapter);Assert.AreEqual("Starfight",zone.musicId);
            Assert.IsTrue(zone.quietArrival,"a hero's arrival line plays over Bryn's radio at the launch");
            Assert.Less(Flat(Lead.transform.position,Arrival),4,"the party isn't at the arrival");
            // The way home waits on Bryn's tow, and says what it waits for.
            var home=All<WarpBeacon>().Single(w=>w.scene=="ManyHandsHull");Assert.AreEqual("home.cutterDisabled",home.requiredFlag);Assert.AreEqual("Ring",home.spawn);
            StringAssert.Contains("Debris Cutter",home.lockedPrompt);Assert.Less(Flat(home.transform.position,HomeWarp),1);
            // Four districts, each its own cover and contract name, the market up beside the arrival and the other three still in the ring far below.
            var sections=All<SectionFlight>();CollectionAssert.AreEquivalent(new[]{"market","repair","crescent","dock"},sections.Select(s=>s.districtId).ToArray());
            foreach(var s in sections)
            {
                var district=s.GetComponent<RingDistrict>();Assert.IsNotNull(district,s.districtId+" has no RingDistrict");Assert.AreEqual(s.districtId,district.id);Assert.AreEqual(Names[s.districtId],district.displayName);
                var cover=s.body;Assert.IsNotNull(cover,s.districtId+" has no cover");Assert.IsFalse(cover.isTrigger,s.districtId+"'s cover is a trigger");Assert.IsNull(cover.GetComponentInParent<Health>(),s.districtId+"'s cover has a Health over it");
                // Solid 2 m under the plane to 4 over (the contract); on the plan it is the shape the district is drawn (EveryPieceOfCoverIsTheShapeItIsDrawnAtThePlane).
                var solid=Local(cover);Assert.AreEqual(-2,solid.min.y,.05f,s.districtId+"'s cover doesn't reach 2 m under the plane");Assert.AreEqual(4,solid.max.y,.05f,s.districtId+"'s cover doesn't reach 4 m over the plane");
                Assert.That(s.Footprint.width,Is.InRange(Wide-.5f,Wide+3),s.districtId+"'s footprint isn't a district's width");Assert.That(s.Footprint.height,Is.InRange(Long-.5f,Long+2.5f),s.districtId+"'s footprint isn't a district's length");
                var rb=s.GetComponent<Rigidbody>();Assert.IsTrue(rb!=null&&rb.isKinematic,s.districtId+"'s moving root isn't kinematic");
                Assert.IsFalse(s.GetComponentsInChildren<Transform>(true).Any(t=>t.gameObject.isStatic),s.districtId+" has a static part: it would not move in a player");
                Assert.AreEqual(Stations[s.districtId],s.Station,s.districtId+"'s station isn't the map's");Assert.AreEqual("ring.formed",s.stationFlag);Assert.AreEqual("home.cutterDisabled",s.releaseFlag);
            }
            var market=Section("market");Assert.IsTrue(market.AtPlane,"the market isn't up at the plane");Assert.IsTrue(market.body.enabled);
            Assert.Less(SectionFlight.From(market.Footprint,new Vector2(Arrival.x,Arrival.z)),12,"the arrival isn't beside the market");Assert.Greater(SectionFlight.From(market.Footprint,new Vector2(Arrival.x,Arrival.z)),Idra+1,"the arrival is inside the market");
            foreach(string id in new[]{"repair","crescent","dock"}){var s=Section(id);Assert.AreEqual(SectionFlight.Stage.Docked,s.State,id+" isn't waiting in the ring");Assert.Less(s.transform.position.y,-50,id+" isn't in the ring far below");Assert.IsFalse(s.body.enabled,id+"'s cover is on in the ring");}
            // The channel: five pieces of the ring riding it, its lamps, and its marking 6 m either side of x 0.
            var channel=Channel;Assert.AreEqual(5,channel.pieces.Length);Assert.IsNotNull(channel.lampStrip);Assert.Greater(channel.lampCount,50);
            Assert.AreEqual(9,channel.SpeedAt(channel.Total*.5f),.01f);Assert.LessOrEqual(channel.markWidth*.5f,6,"the channel's marking runs past its banks");
            // The Cutter's door: its lanes, its encounter, its checkpoint, and Bryn's tug waiting with her line.
            var lanes=All<CutterLane>().OrderBy(l=>l.index).ToArray();Assert.AreEqual(8,lanes.Length);
            for(int k=0;k<8;k++){Assert.AreEqual(k,lanes[k].index);Assert.Less(Flat(lanes[k].transform.position,Lane(k)),.1f,$"lane {k} isn't at r 30, {k*45} degrees");Assert.AreEqual(1,lanes[k].transform.position.y,.01f);}
            var door=All<EncounterVolume>().Single(e=>e.encounterId=="DebrisCutter");Assert.Less(Flat(door.transform.position,C),.1f);Assert.AreEqual(new Vector3(100,8,100),door.GetComponent<BoxCollider>().size);
            Assert.AreEqual("ring.formed",door.requiredFlag);Assert.AreEqual("Ring",door.checkpointSpawn);
            var post=door.spawners[0];Assert.Less(Flat(post.transform.position,Lane(0)),.1f,"the Cutter doesn't start at lane 0");Assert.IsTrue(post.faceAsPlaced);Assert.Less(Vector3.Angle(post.transform.forward,C-post.transform.position),1,"the Cutter doesn't face C");
            var ring=All<SpawnPoint>().Single(p=>p.id=="Ring");Assert.Less(Flat(ring.transform.position,Checkpoint),.1f);Assert.AreEqual(40,Flat(ring.transform.position,C),.1f);
            var tug=All<TowedOff>().Single();Assert.AreEqual("DebrisCutter",tug.boss);Assert.IsNotNull(tug.away);Assert.AreEqual(130,Flat(tug.away.position,C),.5f,"the tug's away isn't 130 m out");
            Assert.Less(Vector3.Angle(tug.transform.forward,C-tug.transform.position),1,"the tug doesn't face C");
            var tow=All<AfterTalk>().Single(t=>t.node=="BrynTowsCutter");Assert.AreEqual("bossdown.DebrisCutter",tow.flag);Assert.AreEqual("Bryn",tow.speaker);Assert.AreEqual("home.cutterDisabled",tow.heardFlag);
            var formation=All<RingFormation>().Single();Assert.AreEqual(2,formation.need);Assert.AreEqual(45,formation.reach);Assert.Less(Flat(formation.centre,C),.1f);
            // Bryn on the radio at the launch, and the story's arrow on the Cutter's door.
            var hail=All<RadioHail>().Single();Assert.AreEqual("Bryn",hail.speaker);CollectionAssert.AreEqual(new[]{"hail.ring","hail.ring"},hail.situations);
            float deadline=Time.unscaledTime+4;while(!spoken.Any(s=>s.StartsWith("Bryn: "))&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(spoken.Any(s=>s.StartsWith("Bryn: Section traffic")),"Bryn says nothing at the launch: "+string.Join(" | ",spoken));
            yield return Frames(4);var goal=Goal();Assert.IsTrue(goal.HasValue,"the objective points at nothing on the Cutter's step");
            Assert.Less(Flat(goal.Value,C),4,$"the objective points at {goal}, not at the Cutter's door");
        }

        // ================================================================== the districts
        /// <summary>The market keeps with its escort: cruising while the lead is level with it or ahead, easing once it is thirty metres ahead of the lead, never over 4 m/s. A ship
        /// sitting in its way holds it where it is, a hull clear of it; taken away, the market goes on.</summary>
        [UnityTest] public IEnumerator ADistrictKeepsWithItsEscortAndHoldsForAShipInItsWay()
        {
            ToTheRing();yield return Zone("BrokenRing");
            var market=Section("market");
            // At the arrival it waits for its escort: the party hasn't moved, and the market is still beside it.
            float waiting=market.transform.position.z;Hold(Arrival);yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(market.SetOff,"the market set off before its escort came level with it");Assert.AreEqual(waiting,market.transform.position.z,.05f,"the market left the arrival before the party had moved");
            // The lead level with it, then well behind it.
            Hold(new Vector3(-12,1,market.transform.position.z+6));yield return new WaitForSeconds(.6f);
            Assert.AreEqual(3.8f,market.Speed,.3f,"the market doesn't cruise with the lead beside it");
            Hold(new Vector3(-12,1,market.transform.position.z-40));yield return new WaitForSeconds(.6f);
            Assert.AreEqual(2.2f,market.Speed,.3f,"the market doesn't ease once it's thirty metres ahead of its escort");
            // A ship a hull's width ahead of its bow: it holds, and never comes into it.
            var ahead=new Vector3(market.transform.position.x,1,market.Footprint.yMax+Idra+.4f);Hold(ahead+Vector3.left*20);var lead=Lead;Put(lead,ahead);
            float z0=market.transform.position.z;float deadline=Time.time+2;
            while(Time.time<deadline){Put(lead,ahead);yield return null;Assert.Greater(SectionFlight.From(market.Footprint,new Vector2(lead.transform.position.x,lead.transform.position.z)),Idra-.6f,"the market pushed into a ship in its way");}
            Assert.Less(market.transform.position.z-z0,.6f,"the market didn't hold for a ship in its way");Assert.IsNotNull(market.Held,"the market says nothing held it");
            Assert.IsTrue(log.Any(l=>l.StartsWith("SECTION_HOLDS market")),"no SECTION_HOLDS logged");
            Put(lead,new Vector3(-12,1,ahead.z));yield return new WaitForSeconds(.8f);
            Assert.Greater(market.transform.position.z-z0,1.5f,"the market didn't go on once its way was clear");Assert.LessOrEqual(market.Speed,4.01f);
        }

        /// <summary>Each district lifts out of the ring as the lead's z passes its line, with Orrin's word for it, climbs to the plane with its cover off until it is there, and the repair
        /// quarter clearing the ring brings Hal on the radio. Played at five times speed.</summary>
        [UnityTest] public IEnumerator EachDistrictLiftsOutOfTheRingAsTheLeadPassesItsLineAndComesUpToThePlane()
        {
            ToTheRing();yield return Zone("BrokenRing");
            var repair=Section("repair");var crescent=Section("crescent");
            Hold(new Vector3(15,1,12));yield return Frames(3);
            Assert.AreEqual(SectionFlight.Stage.Flying,repair.State,"the repair quarter didn't lift as the lead passed z 10");Assert.AreEqual(SectionFlight.Stage.Docked,crescent.State,"the crescent lifted before its line");
            Assert.IsTrue(log.Contains("SECTION_LIFTS repair"));
            float deadline=Time.unscaledTime+3;while(!spoken.Any(s=>s.StartsWith("Orrin: Repair quarter, lifting."))&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(spoken.Any(s=>s.StartsWith("Orrin: Repair quarter, lifting.")),"Orrin doesn't call the repair quarter's lift: "+string.Join(" | ",spoken));
            // Up out of the ring: under the plane its cover is off; at the plane, on, and Hal says so. Five times speed; the lead held where nothing it climbs through is.
            Time.timeScale=5;deadline=Time.unscaledTime+20;
            while(!repair.AtPlane&&Time.unscaledTime<deadline){Hold(new Vector3(15,1,12));if(repair.transform.position.y<-2)Assert.IsFalse(repair.body.enabled,"the repair quarter's cover is on under the plane");yield return null;}
            Time.timeScale=1;
            Assert.IsTrue(repair.AtPlane,"the repair quarter never reached the plane");Assert.IsTrue(repair.body.enabled,"its cover didn't come on at the plane");
            Assert.AreEqual(30,repair.transform.position.x,.5f,"the repair quarter didn't come up on the east column");
            deadline=Time.unscaledTime+5;while(!spoken.Any(s=>s.StartsWith("Hal: "))&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(spoken.Any(s=>s.StartsWith("Hal: Repair quarter.")),"Hal says nothing as the repair quarter clears the ring: "+string.Join(" | ",spoken));
            // The crescent's line, and then the tug dock's, with Orrin's later words.
            Hold(new Vector3(-12,1,72));yield return Frames(3);Assert.AreEqual(SectionFlight.Stage.Flying,crescent.State,"the crescent didn't lift as the lead passed z 70");
            Hold(new Vector3(12,1,132));yield return Frames(3);Assert.AreEqual(SectionFlight.Stage.Flying,Section("dock").State,"the tug dock didn't lift as the lead passed z 130");
        }

        // ================================================================== the channel and the lee
        /// <summary>A piece of the ring coming down the channel meets a ship sitting in it: its stretch is marked before it comes, it strikes inside the marking for its share of the hull,
        /// never the last of it, and throws the ship clear. A ship in a district's lee, the whole time a piece takes to pass it, is never touched, and nothing marks the lee.</summary>
        [UnityTest] public IEnumerator TheChannelIsMarkedAheadOfItsDebrisAndBesideADistrictIsCalm()
        {
            ToTheRing();yield return Zone("BrokenRing");
            var channel=Channel;var lead=Lead;lead.Health.InvulnerableUntil=0;float full=lead.Health.maximum;lead.Health.integrity=full;
            float now=channel.Clock;int i=Enumerable.Range(0,channel.pieces.Length).OrderBy(k=>Mathf.Repeat(channel.ArcOf(k,now)-30,channel.Total)).First();
            float arc=channel.ArcAfter(Mathf.Min(channel.Traverse-1,channel.TimeTo(channel.ArcOf(i,now))+2.8f));if(!channel.AtPlane(arc))arc=channel.Total*.5f;
            var spot=channel.PointAt(arc);Put(lead,spot);
            // Until the track has looked at the ship where it now sits: stretches marked round the arrival count before then, and it was asked in the frame the ship was put there.
            float deadline=Time.time+1.5f;while(!Telegraph.Live.Any(t=>t!=null&&t.Contains(spot))&&Time.time<deadline){Put(lead,spot);yield return null;}
            Assert.Greater(channel.Marked,0,"nothing on the plane marks the channel ahead of a piece coming down it to a ship in it");
            Assert.IsTrue(Telegraph.Live.Any(t=>t!=null&&t.Contains(spot)),"the marking doesn't cover the stretch the ship sits in");
            deadline=Time.time+Channel.Traverse/Channel.pieces.Length+2;while(!log.Any(l=>l.StartsWith("WRECK_STRUCK"))&&Time.time<deadline){if(lead.motor is FlightMotor s)s.Move(Vector2.zero,false,false);yield return null;}
            Assert.IsTrue(log.Any(l=>l.StartsWith("WRECK_STRUCK")&&l.EndsWith("The channel")),"a piece came down the channel and never struck the ship in it");
            yield return new WaitForSeconds(.4f);
            float lost=(full-lead.Health.integrity)/full;Assert.Greater(lost,.05f,"the strike took nothing");Assert.Less(lost,.16f,$"the strike took {lost:P0}");
            Assert.Greater(Mathf.Abs(lead.transform.position.x),4,"the ship wasn't thrown out of the channel");
            // In the market's lee, a full traverse: never struck, and no marking ever lies there.
            log.Clear();lead.Health.integrity=full;var market=Section("market");
            deadline=Time.time+Channel.Traverse/Channel.pieces.Length+1;
            while(Time.time<deadline)
            {
                var lee=new Vector3(-15,1,market.transform.position.z);Put(lead,lee);yield return null;
                Assert.IsFalse(Telegraph.Live.Any(t=>t!=null&&t.Contains(lee)),"a danger marking lies in the market's lee");
            }
            Assert.IsFalse(log.Any(l=>l.StartsWith("WRECK_STRUCK")),"a piece struck a ship in the market's lee");
            // The lee and the channel, side by side: the channel's marking keeps off the strip between a column and its banks.
            Assert.LessOrEqual(Channel.markWidth*.5f+Idra,30-Wide*.5f-6,"the channel's marking leaves less than a hull of calm water beside the districts");
        }

        // ================================================================== the formation and the door
        /// <summary>Two districts holding their stations within 45 m of C set the door's flag; the Cutter's door doesn't open on a ship inside it until then. Played at twenty times speed,
        /// the party held up the exit lane. A save that has the flag loads every district on station.</summary>
        [UnityTest] public IEnumerator TheFormationTakesTheDoorAndASaveWithItLoadsEveryDistrictOnStation()
        {
            ToTheRing();yield return Zone("BrokenRing");
            var exit=new Vector3(0,1,356);Hold(exit);
            Assert.IsFalse(Has("ring.formed"),"fixture: the formation is formed at the arrival");
            Time.timeScale=20;float deadline=Time.unscaledTime+30;
            while(!Has("ring.formed")&&Time.unscaledTime<deadline){Hold(exit);yield return null;}
            Time.timeScale=1;
            Assert.IsTrue(Has("ring.formed"),"two districts never came to their stations: "+string.Join(" | ",log.Where(l=>l.StartsWith("SECTION_"))));
            Assert.IsTrue(log.Any(l=>l.StartsWith("RING_FORMED")));
            var formation=All<RingFormation>().Single();Assert.GreaterOrEqual(formation.Holding(),2);
            foreach(var s in All<SectionFlight>().Where(s=>s.OnStation))
            {
                Assert.LessOrEqual(SectionFlight.From(s.Footprint,new Vector2(C.x,C.z)),45,s.districtId+" holds its station beyond 45 m of C");
                for(int k=0;k<8;k++){var l=Lane(k);Assert.GreaterOrEqual(SectionFlight.From(s.Footprint,new Vector2(l.x,l.z)),8,$"{s.districtId} stands within 8 m of Cutter lane {k}");}
                foreach(var o in All<SectionFlight>())if(o!=s&&o.AtPlane)Assert.GreaterOrEqual(SectionFlight.Gap(s.Footprint,o.Footprint),14,$"{s.districtId} and {o.districtId} are closer than a channel");
            }
            // A save with the flag (the door's checkpoint): every district on station at load.
            yield return Zone("BrokenRing","Ring");
            Assert.Less(Flat(Lead.transform.position,Checkpoint),4,"the party isn't at the checkpoint");
            foreach(var s in All<SectionFlight>()){Assert.IsTrue(s.OnStation,s.districtId+" isn't on station in a save with the formation");Assert.Less(Flat(s.transform.position,Stations[s.districtId]),1.5f,s.districtId+" isn't at its station");}
            Assert.AreEqual(4,All<RingFormation>().Single().Holding(),"not every district holds its station within reach of C");
        }

        // ================================================================== the walls
        /// <summary>Every limit is where the buoys are, never in open water: a hull's sphere flown over the whole approach, round the fight's circle and up the exit lane meets no wall,
        /// at the corners where the approach meets the circle and the circle the exit lane too (no box runs on past a bend: chapter 6). And the Ring is shut everywhere: out from every
        /// edge there is a wall within reach.</summary>
        [UnityTest] public IEnumerator NoWallStandsInOpenWaterAndEveryEdgeIsWalled()
        {
            ToTheRing();yield return Zone("BrokenRing");
            var problems=new List<string>();float hull=Idra,skin=1.2f;
            bool Open(Vector2 p)
            {
                float r=Vector2.Distance(p,new Vector2(0,280));
                bool approach=Mathf.Abs(p.x)<=WallX-skin-hull&&p.y>=SouthZ+skin+hull&&p.y<=280;
                bool circle=r<=Bound-skin-hull;bool lane=Mathf.Abs(p.x)<=ExitHalf-skin-hull&&p.y>=280&&p.y<=ExitEnd-skin-hull;
                return approach||circle||lane;
            }
            for(float x=-WallX;x<=WallX;x+=2)for(float z=SouthZ;z<=ExitEnd;z+=2)
            {
                var p=new Vector2(x,z);if(!Open(p))continue;var hit=WallsAt(new Vector3(x,1,z),hull).FirstOrDefault();if(hit!=null)problems.Add($"({x:0},{z:0}): {hit.name}");
            }
            Assert.IsEmpty(problems,"a wall stands in open water:\n"+string.Join("\n",problems.Take(30)));
            // Shut: out from the approach's sides and its south end, round the circle wherever it isn't open to the approach or the exit, and up the exit lane's sides and end.
            void Shut(Vector3 from,Vector3 way,string what){if(!Physics.SphereCastAll(from,hull,way,6,~0,QueryTriggerInteraction.Ignore).Any(h=>IsWall(h.collider)))problems.Add("nothing shuts "+what);}
            for(float z=SouthZ+4;z<240;z+=4){Shut(new Vector3(-WallX+hull+1,1,z),Vector3.left,$"the west side at z {z:0}");Shut(new Vector3(WallX-hull-1,1,z),Vector3.right,$"the east side at z {z:0}");}
            for(float x=-WallX+4;x<WallX;x+=4)Shut(new Vector3(x,1,SouthZ+hull+1),Vector3.back,$"the south end at x {x:0}");
            for(float b=-128;b<=128;b+=4)
            {
                if(Mathf.Abs(b)<16)continue;float a=b*Mathf.Deg2Rad;var dir=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));Shut(C+dir*(Bound-hull-1),dir,$"the fight's bound at bearing {b:0}");
            }
            for(float z=342;z<ExitEnd-3;z+=4){Shut(new Vector3(-ExitHalf+hull+1,1,z),Vector3.left,$"the exit lane's west side at z {z:0}");Shut(new Vector3(ExitHalf-hull-1,1,z),Vector3.right,$"the exit lane's east side at z {z:0}");}
            Shut(new Vector3(0,1,ExitEnd-hull-1),Vector3.forward,"the exit lane's end");
            Assert.IsEmpty(problems,"the Ring has an open edge:\n"+string.Join("\n",problems.Take(30)));
        }

        // ================================================================== the pairs
        /// <summary>The flight fights' contract as built: each pair an encounter of two Interceptors 8 m apart, outside the Cutter's bound, 75 m from C and 70 from the other, with solid
        /// cover within 20 m of its centre and nothing solid within 8, and the channel's marking 12 m off it; the first pair's wake is the lead's word.</summary>
        [UnityTest] public IEnumerator ThePairsAndTheirCoverStandWhereTheContractAsks()
        {
            ToTheRing();yield return Zone("BrokenRing");
            foreach(var (id,at,cover) in new[]{("Ring_Pair1",Pair1,Cover1),("Ring_Pair2",Pair2,Cover2)})
            {
                var fight=All<EncounterVolume>().Single(e=>e.encounterId==id);Assert.Less(Flat(fight.transform.position,at),.1f,id+" isn't where the map has it");
                Assert.AreEqual(new Vector3(40,8,40),fight.GetComponent<BoxCollider>().size);Assert.AreEqual(1,fight.spawners.Length);Assert.AreEqual(2,fight.spawners[0].count);Assert.AreEqual(4,fight.spawners[0].radius);
                Assert.GreaterOrEqual(Flat(at,C),75,id+" is inside 75 m of C");Assert.AreEqual(1,fight.spawners[0].transform.position.y,.01f);
                Assert.GreaterOrEqual(Mathf.Abs(at.x)-Channel.markWidth*.5f,12,id+"'s centre is within 12 m of the channel's marking");
                var solid=Physics.OverlapSphere(at,8,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.isTrigger&&c.GetComponentInParent<CombatActor>()==null&&!(c is CharacterController)).ToArray();
                Assert.IsEmpty(solid,id+" has something solid within 8 m of its centre: "+string.Join(", ",solid.Select(c=>c.name)));
                var near=Physics.OverlapSphere(at,20,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.isTrigger&&c.name.StartsWith("Cover wreck")).ToArray();Assert.IsNotEmpty(near,id+" has no cover within 20 m");
            }
            Assert.GreaterOrEqual(Flat(Pair1,Pair2),70,"the pairs are less than 70 m apart");
            var sight=All<FirstSight>().Single(f=>f.situation=="interceptor");Assert.AreEqual("Ring_Pair1",sight.encounter.encounterId);Assert.AreEqual("ring.interceptorSeen",sight.onceFlag);
        }

        /// <summary>Cover in flight is the shape it looks (D272): a box over a seed barge's pointed bow stopped shots in clear space at Xylos. Round every district on its station and
        /// both wrecks, the outline of what is drawn at the plane (from a metre under it to a metre and a half over, what a hull flying there meets; light, flames and names left
        /// out) is taken the furthest each way round, every 10 degrees, and the cover's outline reaches as far each way, within 0.6 m. The districts' and
        /// wrecks' first covers were boxes, and the crescent's cabins stood a metre and more out of its box.</summary>
        [UnityTest] public IEnumerator EveryPieceOfCoverIsTheShapeItIsDrawnAtThePlane()
        {
            ToTheRing();Flag("ring.formed");yield return Zone("BrokenRing","Ring");
            var pieces=All<SectionFlight>().Select(s=>(s.transform,s.body,s.districtId)).ToList();
            foreach(var name in new[]{"Cover wreck 1","Cover wreck 2"})
            {
                var root=All<Transform>().Single(t=>t.parent==null&&t.name==name);var body=root.GetComponentsInChildren<Collider>().Single(c=>!c.isTrigger);pieces.Add((root,body,name));
            }
            Assert.AreEqual(6,pieces.Count,"fixture: four districts and two wrecks");
            bool NotIron(Transform t,Transform root){for(;t!=null&&t!=root;t=t.parent){string n=t.name;if(n=="Its lee"||n=="Engine flare"||n=="Engine flame"||n.StartsWith("Flame ")||n.StartsWith("Sign_")||t.GetComponent<TMPro.TMP_Text>()!=null)return true;}return false;}
            // What is drawn is read as it is drawn: a probe collider on each of the piece's meshes, on a layer of its own (a kit mesh's vertices can't be read in play).
            const int Probe=31;var probes=new List<GameObject>();
            List<string> Measure(Transform root,Collider cover,string id)
            {
                var faults=new List<string>();
                Assert.IsNotNull(cover,id+" has no cover");Assert.IsTrue(cover.enabled,id+"'s cover is off at the plane");
                foreach(var p in probes)Object.DestroyImmediate(p);probes.Clear();
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                {
                    var view=filter.GetComponent<MeshRenderer>();if(filter.sharedMesh==null||view==null||!view.enabled||NotIron(filter.transform,root))continue;
                    var probe=new GameObject("Cover probe"){layer=Probe};probe.transform.SetParent(filter.transform,false);probe.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;probes.Add(probe);
                }
                Physics.SyncTransforms();
                float plane=root.position.y;var middle=new Vector2(root.position.x,root.position.z);
                // The furthest the drawn piece reaches each way round: parallel rays from far out, across its whole width, at six heights from a metre under the plane to a metre
                // and a half over, each meeting only this piece's probes.
                string by="";
                float Reach(Vector2 u)
                {
                    var across=new Vector2(-u.y,u.x);float most=float.NegativeInfinity;
                    foreach(float h in new[]{-.9f,-.5f,0f,.5f,1f,1.4f})for(float s=-14;s<=14;s+=.25f)
                    {
                        var from=middle+u*20+across*s;
                        if(Physics.Raycast(new Vector3(from.x,plane+h,from.y),new Vector3(-u.x,0,-u.y),out var hit,40,1<<Probe,QueryTriggerInteraction.Collide)){float d=Vector2.Dot(new Vector2(hit.point.x,hit.point.z)-middle,u);if(d>most){most=d;by=hit.collider.transform.parent!=null?hit.collider.transform.parent.name+(hit.collider.transform.parent.parent!=null?" of "+hit.collider.transform.parent.parent.name:""):"";}}
                    }
                    return most;
                }
                // The cover's own reach each way round, from its outline (a box's corners, or a fitted shape's corners): reach against reach. A point straight out along the
                // way at the drawn reach is no measure: a rectangle's corner reaches further on a slant than its side does along it.
                var outline=new List<Vector2>();
                if(cover is MeshCollider fitted&&fitted.sharedMesh!=null)foreach(var v in fitted.sharedMesh.vertices){var w=cover.transform.TransformPoint(v);outline.Add(new Vector2(w.x,w.z));}
                else if(cover is BoxCollider box)for(int k=0;k<4;k++){var w=box.transform.TransformPoint(box.center+Vector3.Scale(box.size*.5f,new Vector3(k%2==0?-1:1,0,k<2?-1:1)));outline.Add(new Vector2(w.x,w.z));}
                Assert.GreaterOrEqual(outline.Count,3,"fixture: "+id+"'s cover has an outline to read");
                int read=0;
                for(int deg=0;deg<360;deg+=10)
                {
                    var u=new Vector2(Mathf.Sin(deg*Mathf.Deg2Rad),Mathf.Cos(deg*Mathf.Deg2Rad));float reach=Reach(u);if(float.IsNegativeInfinity(reach))continue;read++;
                    float solid=outline.Max(p=>Vector2.Dot(p-middle,u));
                    if(solid>reach+.6f)faults.Add($"{id} at {deg} degrees: solid {solid-reach:0.0} m out past what is drawn");
                    if(reach>solid+.6f)faults.Add($"{id} at {deg} degrees: drawn {reach-solid:0.0} m out past its cover ({by})");
                }
                Assert.AreEqual(36,read,"fixture: "+id+" is drawn all the way round at the plane");
                return faults;
            }
            var faults=new List<string>();
            try
            {
                // The measure has to see the first build's cover for what it was: the crescent under a 12 by 17 m box, its cabins' ends out past it.
                var crescent=pieces.Single(p=>p.Item3=="crescent");var first=new GameObject("First build's cover (the fixture)",typeof(BoxCollider));first.transform.SetParent(crescent.Item1,false);
                var box=first.GetComponent<BoxCollider>();box.center=new Vector3(0,1,0);box.size=new Vector3(Wide,6,Long);Physics.SyncTransforms();
                var boxed=Measure(crescent.Item1,box,"crescent (boxed)");Object.DestroyImmediate(first);
                Assert.IsTrue(boxed.Any(f=>f.Contains("out past its cover")),"fixture: the measure can't see the crescent's cabins standing out of a box");
                foreach(var (root,cover,id) in pieces)faults.AddRange(Measure(root,cover,id));
            }
            finally{foreach(var p in probes)Object.DestroyImmediate(p);}
            Assert.IsEmpty(faults,"cover that isn't the shape it looks: "+string.Join("; ",faults.Take(20)));
        }

        // ================================================================== what each district is known by
        /// <summary>Every district is the station's kit (its floor plate and wall modules) with what a player names it by, a cut stern with the Crawl's three engines under it and their light,
        /// its lee's light on the plane beside its inner side, and nothing but a mast or a crane's beam more than 3 m over the plane, so it never hides a ship beside it.</summary>
        [UnityTest] public IEnumerator EveryDistrictIsTheStationsKitAndCarriesWhatItIsKnownBy()
        {
            ToTheRing();Flag("ring.formed");yield return Zone("BrokenRing","Ring");
            bool HasPiece(SectionFlight s,string key)=>s.GetComponentsInChildren<Transform>(true).Any(t=>t.name==key);
            foreach(var s in All<SectionFlight>())
            {
                Assert.IsTrue(HasPiece(s,"DeckFloor")&&HasPiece(s,"DeckWall"),s.districtId+" isn't built of the station's floor and wall modules");
                Assert.AreEqual(3,s.flames.Length,s.districtId+" hasn't three engines");Assert.IsNotNull(s.engineLight,s.districtId+"'s engines cast no light");
                var lee=s.transform.Find("Its lee");Assert.IsNotNull(lee,s.districtId+" has no lee drawn");float side=Mathf.Sign(lee.position.x-s.transform.position.x);
                Assert.AreEqual(-Mathf.Sign(s.transform.position.x),side,s.districtId+"'s lee isn't on its inner side");
                foreach(var r in s.GetComponentsInChildren<Renderer>())
                {
                    if(r is ParticleSystemRenderer||r.name.Contains("CommsMast")||r.transform.parent!=null&&r.transform.parent.name.Contains("CommsMast"))continue;
                    if(r.bounds.max.y>Plane+3.3f&&!(s.districtId=="repair"&&r.name=="District repair"))Assert.Fail($"{s.districtId}: {r.name} stands {r.bounds.max.y-Plane:0.0} m over the plane");
                }
            }
            Assert.IsTrue(HasPiece(Section("market"),"MarketAwning"),"the market has no awnings");Assert.IsTrue(HasPiece(Section("repair"),"DeckDoorway")&&HasPiece(Section("repair"),"LatticeAnvil"),"the repair quarter has no shop front or anvil");
            Assert.GreaterOrEqual(Section("crescent").GetComponentsInChildren<Transform>().Count(t=>t.name=="DeckWall"),10,"the crescent hasn't its rows of cabins");
            Assert.IsTrue(HasPiece(Section("dock"),"Skiff"),"the tug dock's cradle holds no tug");
        }

        /// <summary>A district's engines burn as light from the game's camera, not as solid white cones. Drawn solid (unlit glow written over what was behind it) the three cones under
        /// each stern were flat white funnels as the districts lifted (whole-game capture, November 20, s61/058, 071, 083, 102, 112). The camera as the flight camera holds it frames the
        /// market's stern on station and draws it without its flames, with them, and with the same cones drawn solid warm white: the flames add at most a fifth of the flat white the
        /// solid cones add, and still add a tenth of their light. The flares at the nozzles are left out of all three.</summary>
        [UnityTest] public IEnumerator ADistrictsEnginesBurnAsLightAndNotAsSolidWhiteCones()
        {
            ToTheRing();Flag("ring.formed");yield return Zone("BrokenRing","Ring");
            foreach(var ship in PartyController.Current.members)Put(ship,C+Vector3.back*40);
            yield return Frames(10);
            var district=Section("market");var holders=district.flames.Where(f=>f!=null).ToArray();
            Assert.AreEqual(3,holders.Length,"fixture: the market's three engines");Assert.IsTrue(holders.All(f=>f.gameObject.activeInHierarchy),"fixture: the market's engines aren't burning on station");
            var cones=holders.SelectMany(f=>f.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.name.StartsWith("Flame ")).ToArray();
            var flares=holders.SelectMany(f=>f.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.name=="Engine flare").ToArray();
            Assert.AreEqual(3,cones.Length,"fixture: a cone at each engine");
            var view=Camera.main;Assert.IsNotNull(view,"fixture: the game's camera");
            var built=cones.Select(r=>r.sharedMaterial).ToArray();
            var solid=new Material(Shader.Find("Universal Render Pipeline/Unlit"));solid.SetColor("_BaseColor",new Color(1.6f,1.54f,1.38f));// the old cones' glow (1.6) on their colour
            foreach(var f in flares)f.enabled=false;
            const int W=960,H=540;var target=new RenderTexture(W,H,24);var read=new Texture2D(W,H,TextureFormat.RGB24,false);
            var keptAt=view.transform.position;var keptTurn=view.transform.rotation;
            Color[] Draw(bool shown,Material with)
            {
                foreach(var r in cones){r.enabled=shown;}if(with!=null)foreach(var r in cones)r.sharedMaterial=with;
                var middle=cones.Aggregate(cones[0].bounds,(b,r)=>{b.Encapsulate(r.bounds);return b;}).center;
                view.transform.position=middle-view.transform.forward*26;
                view.targetTexture=target;view.Render();view.targetTexture=null;
                RenderTexture.active=target;read.ReadPixels(new Rect(0,0,W,H),0,0);read.Apply();RenderTexture.active=null;
                for(int i=0;i<cones.Length;i++)cones[i].sharedMaterial=built[i];
                return read.GetPixels();
            }
            var off=Draw(false,null);var lit=Draw(true,null);
            byte[] litPng=read.EncodeToPNG();var white=Draw(true,solid);byte[] whitePng=read.EncodeToPNG();
            view.transform.SetPositionAndRotation(keptAt,keptTurn);foreach(var f in flares)f.enabled=true;foreach(var r in cones)r.enabled=true;
            Object.Destroy(target);Object.Destroy(solid);
            static bool FlatWhite(Color c)=>Mathf.Min(c.r,Mathf.Min(c.g,c.b))>.85f;
            static float Bright(Color c)=>(c.r+c.g+c.b)/3;
            int FlatAdded(Color[] with){int n=0;for(int i=0;i<off.Length;i++)if(FlatWhite(with[i])&&!FlatWhite(off[i]))n++;return n;}
            float LightAdded(Color[] with){float sum=0;for(int i=0;i<off.Length;i++)sum+=Mathf.Max(0,Bright(with[i])-Bright(off[i]));return sum;}
            int flatLit=FlatAdded(lit),flatWhite=FlatAdded(white);float lightLit=LightAdded(lit),lightWhite=LightAdded(white);
            try{var folder=System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/look/ch9-flames");System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,"market-stern-flames.png"),litPng);System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,"market-stern-solid.png"),whitePng);}
            catch(System.Exception e){TestContext.WriteLine("couldn't keep the stills: "+e.Message);}
            TestContext.WriteLine($"RING_FLAMES flat white added: solid {flatWhite} px, flames {flatLit} px; light added: solid {lightWhite:0}, flames {lightLit:0}");
            Assert.Greater(flatWhite,400,$"fixture: the cones drawn solid white add only {flatWhite} flat white pixels, so the measure can't see what s61/102 showed");
            Assert.LessOrEqual(flatLit,flatWhite/5,$"the engines' flames add {flatLit} flat white pixels to the solid cones' {flatWhite}: they read as white cones, not light");
            Assert.Greater(lightLit,lightWhite*.1f,$"the engines' flames add {lightLit:0} of light to the solid cones' {lightWhite:0}: they barely show");
        }

        // ================================================================== the close
        /// <summary>The Cutter down: the tug comes and goes, Bryn speaks (<c>BrynTowsCutter</c>, which sets home.cutterDisabled and moves the story to the chart), the districts leave their
        /// stations for the convoy, and the way home opens. A save with the flag has them with the convoy and the tug gone.</summary>
        [UnityTest] public IEnumerator BrynTowsTheCutterOffThenTheDistrictsLeaveForTheConvoyAndTheWayHomeOpens()
        {
            ToTheRing();Flag("ring.formed");yield return Zone("BrokenRing","Ring");
            DialogueSystem.Current.AutoAdvance=true;Flag("bossdown.DebrisCutter");
            float deadline=Time.unscaledTime+16;while(!heard.Contains("BrynTowsCutter")&&Time.unscaledTime<deadline)yield return null;
            yield return Settle();
            Assert.Contains("BrynTowsCutter",heard,"Bryn never spoke after the Cutter's fall");Assert.IsTrue(Has("home.cutterDisabled"),"BrynTowsCutter didn't set home.cutterDisabled");
            Assert.AreEqual(14,Step("HouseLeaves"),"the story didn't move on to the chart");
            yield return Frames(3);Assert.IsTrue(log.Any(l=>l.StartsWith("SECTION_LEAVES")),"no district left its station");
            var home=All<WarpBeacon>().Single(w=>w.scene=="ManyHandsHull");Put(Lead,home.transform.position+Vector3.back*3);yield return Frames(4);
            var goal=Goal();Assert.IsTrue(goal.HasValue,"the objective points at nothing on the chart's step");Assert.Less(Flat(goal.Value,home.transform.position),3,$"the objective points at {goal}, not the way home");
            // Loaded again with the flag: every district with the convoy, far down, and the tug gone.
            yield return Zone("BrokenRing","Ring");
            foreach(var s in All<SectionFlight>()){Assert.AreEqual(SectionFlight.Stage.Gone,s.State,s.districtId+" isn't with the convoy");Assert.Less(s.transform.position.y,-50);Assert.IsFalse(s.body.enabled);}
            Assert.IsFalse(All<TowedOff>().Single().gameObject.activeInHierarchy,"the tug is still waiting after the tow");
        }

        // ================================================================== what a ship meets of the Cutter and the tug
        /// <summary>The Debris Cutter, standing still at C with its nose north and its fight's door cleared so no other wakes (its body only, as the fight has it).</summary>
        static IEnumerator Cutter(Vector3 at)
        {
            var definition=GameCatalog.Find<EnemyDef>("DebrisCutter");Assert.IsNotNull(definition,"fixture: the Debris Cutter is defined (HomeBuilder.Bosses)");
            var enemy=ActorFactory.Enemy(definition,at);enemy.transform.rotation=Quaternion.identity;enemy.Passive=true;yield return Frames(5);
            cutter=enemy;
        }
        static EnemyBrain cutter;
        /// <summary>What is drawn of a body as solid shapes on a layer nothing else uses, read only by the checks here: a ship's hull sphere that meets one is inside what the player sees.</summary>
        static List<GameObject> Probes(Transform body)
        {
            var made=new List<GameObject>();
            foreach(var filter in body.GetComponentsInChildren<MeshFilter>())
            {
                var view=filter.GetComponent<MeshRenderer>();if(filter.sharedMesh==null||view==null||!view.enabled||filter.GetComponentInParent<TMPro.TMP_Text>()!=null)continue;
                var probe=new GameObject("Drawn probe "+filter.name){layer=31};probe.transform.SetParent(filter.transform,false);probe.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;made.Add(probe);
            }
            Physics.SyncTransforms();return made;
        }
        static bool Inside(CombatActor ship,float into=.15f)=>Physics.CheckSphere(ship.transform.position+Vector3.up*.8f,Mathf.Max(.1f,HeroCollision.HullRadius(ship.character)-into),1<<31,QueryTriggerInteraction.Collide);

        /// <summary>No ship sinks into the Cutter where it is drawn. Its solid was a 2.6 m capsule in the middle of a hull drawn about twice as long, so Idra's tug flew into its underside
        /// mid-fight and stayed there (whole-game capture, November 20, s61/065, and the fight's stills from 052 to 069). Each ship is flown straight at it from round the compass, as a
        /// pilot or the partner's steering would, and must end outside what is drawn.</summary>
        [UnityTest] public IEnumerator NoShipSinksIntoTheCuttersHullWhereItIsDrawn()
        {
            ToTheRing();Flag("ring.formed");Flag("clear.DebrisCutter");yield return Zone("BrokenRing","Ring");
            yield return Cutter(C);var failures=new List<string>();
            var probes=Probes(cutter.GetComponent<DefeatPresentation>().visual);Assert.IsNotEmpty(probes,"fixture: nothing of the Cutter is drawn");
            // The check first: a ship stood on its middle is inside it.
            Put(Lead,C);Assert.IsTrue(Inside(Lead),"fixture: a ship stood in the Cutter's middle isn't found inside it");
            foreach(var p in probes)Object.Destroy(p);yield return null;
            // And the hull as it was (what keeps a ship off what is drawn switched off): nothing a ship meets stops it short of the Cutter's middle (its capsule takes blows, it isn't
            // in a ship's way), so flown in toward where it is drawn furthest and held two metres from its middle, as Idra's tug sat in s61/065, a ship is inside it.
            var keeps=cutter.GetComponent<HullKeepsOff>();Assert.IsNotNull(keeps,"the Cutter keeps nothing off its drawn hull");Assert.GreaterOrEqual(keeps.outline.Length,3,"the Cutter's hull has no outline");
            var furthest=keeps.outline.OrderByDescending(p=>p.magnitude).First();var toward=cutter.transform.TransformDirection(new Vector3(furthest.x,0,furthest.y)).normalized;
            keeps.enabled=false;foreach(var other in PartyController.Current.members)if(other!=Lead)Put(other,C+Vector3.back*40);
            Put(Lead,C+toward*16);
            for(int frame=0;frame<75;frame++){var lb=Lead.GetComponent<CharacterController>();float left=Flat(Lead.transform.position,C)-2;if(lb.enabled&&left>0)lb.Move(-toward*Mathf.Min(.25f,left));yield return null;}
            TestContext.WriteLine($"CUTTER_HULL_FURTHEST {furthest.magnitude:0.00} m toward {toward}; with nothing keeping it off a ship flown in got to {Flat(Lead.transform.position,C):0.00} m from its middle");
            probes=Probes(cutter.GetComponent<DefeatPresentation>().visual);bool sank=Inside(Lead);foreach(var p in probes)Object.Destroy(p);keeps.enabled=true;yield return null;
            Assert.IsTrue(sank,$"fixture: with nothing keeping it off, a ship flown at the Cutter ends {Flat(Lead.transform.position,C):0.00} m from its middle and isn't found inside it, so the check can't see what s61/065 showed");
            foreach(var ship in PartyController.Current.members)
            {
                var others=PartyController.Current.members.Where(m=>m!=ship).ToList();
                for(float bearing=0;bearing<360;bearing+=30)
                {
                    foreach(var other in others)Put(other,C+Vector3.back*40);
                    var way=Quaternion.Euler(0,bearing,0)*Vector3.forward;Put(ship,C+way*16);var body=ship.GetComponent<CharacterController>();
                    for(int frame=0;frame<75;frame++){if(body.enabled)body.Move(-way*.25f);yield return null;}
                    yield return null;probes=Probes(cutter.GetComponent<DefeatPresentation>().visual);
                    if(Inside(ship))failures.Add($"{ship.character}'s hull, flown in from bearing {bearing:0}, ends inside the Cutter's drawn hull at {Flat(ship.transform.position,C):0.0} m from its middle");
                    foreach(var p in probes)Object.Destroy(p);yield return null;
                }
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        /// <summary>Bryn's tug takes the dead Cutter from its side away from the ships and tows it away from them and down, so neither hull comes through a ship that Bryn's talk is holding.
        /// The tug came in to the body's side toward where it tows to, which was where the party stood, laid its boom through Idra's tug and dragged the Cutter through Taren's fighter
        /// (whole-game capture, November 20, s61/086 to 098). The party stands as the replay's did when the Cutter went dead, between it and the tug's side of the fight.</summary>
        [UnityTest] public IEnumerator TheTugTakesTheCutterFromTheSideAwayFromTheShipsAndTowsItClearOfThem()
        {
            ToTheRing();Flag("ring.formed");Flag("clear.DebrisCutter");yield return Zone("BrokenRing","Ring");
            DialogueSystem.Current.AutoAdvance=true;
            yield return Cutter(new Vector3(-16,Plane,286));
            Put(Lead,new Vector3(-29.8f,Plane,286.4f));var partner=PartyController.Current.Partner;if(partner!=null)Put(partner,new Vector3(-34,Plane,282));
            var tug=All<TowedOff>().Single();var failures=new List<string>();
            cutter.Health.InvulnerableUntil=0;cutter.Health.Receive(new DamagePacket{source=Lead.Health,amount=cutter.Health.maximum*100,type=DamageType.Beam,weight=HitWeight.Heavy});yield return Frames(2);
            Assert.IsFalse(cutter.Health.Alive,"fixture: the Cutter didn't go dead");if(!Has("bossdown.DebrisCutter"))Flag("bossdown.DebrisCutter");
            var probes=Probes(cutter.GetComponent<DefeatPresentation>().visual);Assert.IsNotEmpty(probes,"fixture: nothing of the Cutter is drawn");
            var tugViews=tug.GetComponentsInChildren<MeshRenderer>().Where(r=>r.GetComponentInParent<TMPro.TMP_Text>()==null).ToArray();Assert.IsNotEmpty(tugViews,"fixture: nothing of the tug is drawn");
            bool TugIn(CombatActor ship)
            {
                var at=ship.transform.position+Vector3.up*.8f;float r=HeroCollision.HullRadius(ship.character)-.15f;
                foreach(var view in tugViews){if(view==null||!view.enabled||!view.gameObject.activeInHierarchy)continue;if(view.bounds.SqrDistance(at)<r*r)return true;}
                return false;
            }
            // The checks first: a ship stood in the dead Cutter's middle is inside it, and one stood on the tug is inside the tug.
            var heldAt=Lead.transform.position;
            Put(Lead,cutter.transform.position);Assert.IsTrue(Inside(Lead),"fixture: a ship in the Cutter's middle isn't found inside it");
            Put(Lead,new Vector3(tug.transform.position.x,Plane,tug.transform.position.z));Assert.IsTrue(TugIn(Lead),"fixture: a ship on the tug isn't found inside it");
            Put(Lead,heldAt);
            var ships=PartyController.Current.members.ToList();var held=ships.ToDictionary(s=>s,s=>s.transform.position);
            for(float end=Time.unscaledTime+45;Time.unscaledTime<end&&!log.Any(l=>l.StartsWith("TOW_GONE"));)
            {
                // The ships stay where Bryn's talk holds them.
                foreach(var ship in ships)if(Flat(ship.transform.position,held[ship])>.3f)Put(ship,held[ship]);
                Physics.SyncTransforms();
                foreach(var ship in ships)
                {
                    if(cutter!=null&&Inside(ship)&&failures.Count<6)failures.Add($"the towed Cutter came through {ship.character}'s hull at {ship.transform.position} (it was at {cutter.transform.position})");
                    if(TugIn(ship)&&failures.Count<6)failures.Add($"the tug came through {ship.character}'s hull at {ship.transform.position} (it was at {tug.transform.position})");
                }
                yield return null;
            }
            Assert.IsTrue(log.Any(l=>l.StartsWith("TOW_TAKES")),"the tug never took the Cutter");Assert.IsTrue(log.Any(l=>l.StartsWith("TOW_GONE")),"the tug never left with it");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
