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
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 9's first engineer's work as a player meets it, in the built scenes: the chapter's definitions (its zones, people, quests and chart), its way in (Abri turning for
    /// Nacre at Harrowgate, the warps off the Picket and Many Hands' stern), Many Hands' middle and last beats, and Nacre Grave (a wreck field with a spiral: the lane winding in, the
    /// marked tracks quicker inward, the three lamps). The other zones are their own engineers' and tested in their classes. Places are the map brief's (docs/maps/NacreGrave.md), which
    /// are the builder's own (HomeBuilder.Nacre).</summary>
    public sealed class HomeWorldTests
    {
        readonly List<string> heard=new(),log=new();
        void Talked(string node,string speaker,System.Action<bool> done)=>heard.Add(node);
        void Looked(DiscoveryPoint point){if(!string.IsNullOrEmpty(point.dialogueNode))heard.Add(point.dialogueNode);}
        static readonly string[] Heeded={"WRECK_","DISCOVERY","AFTER_TALK","FIRST_SIGHT","FLAG_ALL"};
        void Logged(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter9");heard.Clear();log.Clear();
            Npc.TalkRequested+=Talked;DiscoveryPoint.DialogueRequested+=Looked;Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Npc.TalkRequested-=Talked;DiscoveryPoint.DialogueRequested-=Looked;Application.logMessageReceived-=Logged;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Home(ContentRegistry.Book book)
        {
            // The two warps to Nacre are drawn, and open, once Abri has turned the convoy (HomeBuilder.Approach.WarpToNacre): no lock to name.
            // The script's one node with choices (manifest 8.2). Mira's three end at her counter with no choice, and are read so from their own lines (DialogueInputTests.EndingOf):
            // branched, the test waits for a choice that never comes (chapter 8 found it so).
            book.Branch("HalCrated","bench","repair","world");
        }

        // ------------------------------------------------------------------ Nacre Grave's places (HomeBuilder.Nacre; docs/maps/NacreGrave.md)
        /// <summary>A wall box is 2 m thick about its line, so open water ends a metre short of a wall's line; and a little more for the chord of a curve.</summary>
        const float Skin=1.2f;
        const float R0=158,Pitch=52,Half=16,StartTurn=-18,EndTurn=452,BasinRim=70,BerthSolid=30,MouthTurn=400,BreachTurn=200,BreachHalf=11,Idra=2.17f;
        static float R(float turn)=>R0-Pitch*turn/360f;
        static Vector2 OnPlan(float turn,float radius){float a=turn*Mathf.Deg2Rad;return new Vector2(Mathf.Sin(a),Mathf.Cos(a))*radius;}
        static Vector3 At(float turn,float offset=0){var p=OnPlan(turn,R(turn)+offset);return new Vector3(p.x,1,p.y);}
        static Vector3 In(float turn,float radius){var p=OnPlan(turn,radius);return new Vector3(p.x,1,p.y);}
        static readonly Vector3 Arrival=new(-14.01f,1,160.11f),HaulerHail=new(58.27f,1,30.99f),TugHail=new(10.14f,1,-145.02f),PodHail=new(-26.43f,1,29.35f),NeveHail=new(101.1f,1,120.4f);

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static int Step(string quest)=>State.questSteps.TryGetValue(quest,out var step)?step:-1;
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
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
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship)ship.Halt();else if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static IEnumerator Settle()
        {
            float deadline=Time.unscaledTime+40;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(DialogueSystem.Current.Running,"a conversation never ended");
            yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Discover(DiscoveryPoint point){DialogueSystem.Current.AutoAdvance=true;Assert.IsTrue(point.Available,$"\"{point.prompt}\" isn't offered");point.Interact();yield return null;yield return Settle();}
        static DiscoveryPoint Point(string flag)=>All<DiscoveryPoint>().Single(p=>p.flag==flag);
        static WreckTrack Track(string name)=>All<WreckTrack>().Single(t=>t.trackName==name);
        static Vector3? Goal()=>Object.FindFirstObjectByType<ObjectiveHud>().Goal;
        /// <summary>The colliders the field's walls are made of (HomeBuilder.Nacre.Walls: every box under "Field limits").</summary>
        static bool IsWall(Collider c)=>c!=null&&c.transform.parent!=null&&c.transform.parent.name=="Field limits"&&c.name!="Berth body";
        static IEnumerable<Collider> WallsAt(Vector3 p,float radius)=>Physics.OverlapSphere(p,radius,~0,QueryTriggerInteraction.Ignore).Where(IsWall);
        /// <summary>The story at Nacre's door: Abri's word heard and the main quest begun.</summary>
        static void ToNacre(){Flag("home.turnedForNacre");State.questSteps["HouseLeaves"]=0;}

        // ================================================================== the chapter's definitions
        [UnityTest] public IEnumerator TheChapterIsDefinedAsItsScriptAsks()
        {
            yield return null;
            var zones=new Dictionary<string,(ZoneKind kind,string music)>{["NacreGrave"]=(ZoneKind.SpaceCombat,"Endless Flight"),["MeridianCrawl"]=(ZoneKind.GroundSafe,"Dventure at Dusk"),
                ["Terminator"]=(ZoneKind.GroundCombat,"Dventure at Dusk"),["DecksLeaving"]=(ZoneKind.GroundSafe,"Hub Town Groove"),["SeparationWorks"]=(ZoneKind.GroundCombat,"Moon Caverns"),["BrokenRing"]=(ZoneKind.SpaceCombat,"Starfight")};
            foreach(var (id,(kind,music)) in zones)
            {
                var zone=GameCatalog.Find<ZoneDef>(id);Assert.IsNotNull(zone,id+" has no ZoneDef");Assert.AreEqual(id,zone.id);Assert.AreEqual(id,zone.scene);Assert.AreEqual(9,zone.chapter,id);
                Assert.AreEqual(kind,zone.kind,id);Assert.AreEqual(music,zone.musicId,id);Assert.AreEqual(1,zone.enemyThreat,id+"'s threat isn't 1: chapter 9's hits are authored at their weight");
                Assert.IsNotNull(zone.hd2dProfile,id+" has no profile");Assert.IsFalse(string.IsNullOrEmpty(zone.displayName),id+" has no name a sign can say");
            }
            foreach(string speaker in new[]{"Dorrow","Tam","Jun","Bryn","Oda"})
            {
                var person=GameCatalog.Find<CharacterDef>(speaker);Assert.IsNotNull(person,speaker+" has no definition");Assert.IsNotNull(person.portraitNatural,speaker+" has no portrait set");
                Assert.IsNotNull(person.natural,speaker+" has no body");
                Assert.AreEqual(16,person.portraitNatural.entries.Select(e=>e.emotion).Distinct().Count(),speaker+"'s sheet hasn't all sixteen expressions");
            }
            Assert.AreEqual("Chief Engineer Dorrow",GameCatalog.Find<CharacterDef>("Dorrow").displayName);
            var main=GameCatalog.Find<QuestDef>("HouseLeaves");Assert.IsNotNull(main,"the main quest isn't defined");
            Assert.IsTrue(main.main);Assert.AreEqual(9,main.chapter);Assert.AreEqual("The House That Can Leave",main.title);Assert.AreEqual(15,main.steps.Length,"the main quest hasn't the manifest's fifteen steps");
            Assert.AreEqual(main.steps.Length,main.stepTargets.Length,"the main quest's steps and targets differ");Assert.AreEqual("chapter9.complete",main.cardFlag);
            Assert.LessOrEqual(main.cardBody.Length,200,"the chapter's closing card says more than two lines of the plate");
            foreach(string id in new[]{"HouseLeaves","SecondInventory","PortableBenches","ReturnAddress","StillGround"})
            {
                var quest=GameCatalog.Find<QuestDef>(id);Assert.IsNotNull(quest,id+" isn't defined");Assert.AreEqual(9,quest.chapter,id);
                Assert.AreEqual(quest.steps.Length,quest.stepText.Length,id+"'s steps and words differ");Assert.IsTrue(quest.stepText.All(t=>!string.IsNullOrWhiteSpace(t)),id+" has a blank step");
                foreach(var reward in quest.rewards)
                {
                    Assert.IsNull(GameCatalog.Find<ConsumableDef>(reward.id),$"{id} pays {reward.id}, a capped consumable");
                    Assert.IsTrue(GameCatalog.Find<MaterialDef>(reward.id)!=null||GameCatalog.Find<TechPartDef>(reward.id)!=null,$"{id} pays {reward.id}, which is neither a material nor a part");
                }
            }
            var crawl=GameCatalog.Find<ChartStop>("MeridianCrawl");Assert.IsNotNull(crawl,"the Crawl isn't on the chart");Assert.AreEqual("home.crawlNamed",crawl.unlockFlag,"the Crawl isn't charted by Idra's word");
            var home=GameCatalog.Find<ChartStop>("DecksLeaving");Assert.IsNotNull(home,"the Halo on the day isn't on the chart");Assert.AreEqual("home.launched",home.lockedFlag,"the Halo's stop stays open after it has flown");
            StringAssert.Contains("came apart",home.lockedReason);
        }

        /// <summary>Steps 1 to 3 are the three lamps, in any order (manifest section 4): a lamp found out of turn is caught up as the story reaches it.</summary>
        [UnityTest] public IEnumerator TheThreeLampsMoveTheStoryInWhateverOrderTheyAreFound()
        {
            yield return null;
            RpgServices.Quests.Start("HouseLeaves");Assert.AreEqual(0,Step("HouseLeaves"));
            RpgServices.Quests.Report(ObjectiveKind.Reach,"NacreGrave");Assert.AreEqual(1,Step("HouseLeaves"),"arriving at Nacre didn't move the story to the lamps");
            // The pod first, then the tug: neither is the step it's on, so neither moves it yet.
            Flag("home.navigatorsFound");RpgServices.Quests.Report(ObjectiveKind.Flag,"home.navigatorsFound");Flag("home.tugFound");RpgServices.Quests.Report(ObjectiveKind.Flag,"home.tugFound");
            Assert.AreEqual(1,Step("HouseLeaves"),"a lamp found out of turn moved the story past Oda");
            Flag("home.odaFound");RpgServices.Quests.Report(ObjectiveKind.Flag,"home.odaFound");
            Assert.AreEqual(4,Step("HouseLeaves"),"Oda found last didn't catch the story up past the tug and the pod");
        }

        // ================================================================== Nacre Grave
        [UnityTest] public IEnumerator NacreGraveHasItsWaysItsLampsAndItsThreeTracks()
        {
            ToNacre();yield return Zone("NacreGrave");
            var zone=ZoneController.Current.definition;Assert.AreEqual(ZoneKind.SpaceCombat,zone.kind);Assert.AreEqual(1,zone.flightPlane);
            Assert.Less(Flat(Lead.transform.position,Arrival),4,"the party isn't at the arrival");
            var home=All<WarpBeacon>().Single(w=>w.scene=="ManyHandsHull");Assert.IsTrue(string.IsNullOrEmpty(home.requiredFlag),"the way home is shut");Assert.AreEqual("Nacre",home.spawn);
            foreach(var (flag,report,node,at) in new[]{("home.odaFound","OdaHauler","OdaFound",HaulerHail),("home.tugFound","WelcomeBack","BrynWelcomeBack",TugHail),("home.navigatorsFound","NavigatorsPod","BrynNavigators",PodHail)})
            {
                var p=Point(flag);Assert.AreEqual(report,p.report);Assert.AreEqual(node,p.dialogueNode);Assert.Less(Flat(p.transform.position,at),1,$"{node}'s point isn't where the map has it");
                Assert.AreEqual("home.nacreSpoken",p.requiredFlag,$"{node} is offered before Bryn has named the lamps");
                var lamp=All<SignalBeacon>().OrderBy(b=>Flat(b.transform.position,at)).First();Assert.AreEqual(flag,lamp.flag,$"{node}'s hull has no lamp of its own calling");Assert.Less(Flat(lamp.transform.position,at),12);
                Assert.IsFalse(lamp.Quiet,$"{node}'s lamp is dark before it's found");
            }
            Assert.AreEqual("NeveReturnOffer",Point("neve.offered").dialogueNode);
            var crossing=Point("home.nacreReached");Assert.AreEqual("BrynNacreApproach",crossing.dialogueNode);Assert.IsTrue(crossing.startsOnCrossing,"Bryn's hail can be flown round");
            var all=All<FlagAll>().Single(f=>f.result=="home.nacreDone");CollectionAssert.AreEquivalent(new[]{"home.odaFound","home.tugFound","home.navigatorsFound"},all.flags);
            var tracks=All<WreckTrack>();Assert.AreEqual(3,tracks.Length,"Nacre hasn't its three tracks");
            foreach(var t in tracks){Assert.Greater(t.pieces.Length,0,t.trackName+" carries no wrecks");Assert.IsNotNull(t.lampStrip,t.trackName+" has no lamps");Assert.Greater(t.lampCount,10,t.trackName);}
            // The story's arrow: arriving, the step is Oda's lamp, and it points at her hauler once Bryn has named it.
            Assert.AreEqual(1,Step("HouseLeaves"),"arriving at Nacre didn't move the story on to the lamps");
            // The panel looks again on its own beat, not every frame: give it a second to see the flag.
            Flag("home.nacreSpoken");float deadline=Time.unscaledTime+2;while((!Goal().HasValue||Flat(Goal().Value,HaulerHail)>=3)&&Time.unscaledTime<deadline)yield return null;
            var goal=Goal();Assert.IsTrue(goal.HasValue,"the objective points at nothing on Oda's step");
            Assert.Less(Flat(goal.Value,HaulerHail),3,$"the objective points at {goal}, not at Oda's hauler");
        }

        /// <summary>The rule, read off the tracks as built: inward is quicker. The tow line runs two and a half times as fast at the mouth as on the outer pass, the race and the ring
        /// quicker still; and a quicker wreck throws a longer amber ahead of it, so the inner tracks stand mostly amber where the outer one is mostly cold.</summary>
        [UnityTest] public IEnumerator InwardIsQuickerAndItsAmberLongerOnEveryTrack()
        {
            ToNacre();yield return Zone("NacreGrave");
            var tow=Track("The tow line");var race=Track("The breach race");var ring=Track("The berth ring");
            float outer=tow.SpeedAt(tow.Total*.08f),inner=tow.SpeedAt(tow.Total*.95f);
            Assert.Greater(tow.RadiusAt(tow.Total*.08f)-tow.RadiusAt(tow.Total*.95f),40,"fixture: the tow line doesn't run inward");
            Assert.GreaterOrEqual(inner,outer*2.5f,$"the tow line runs {outer:0.0} m/s on the outer pass and only {inner:0.0} at the mouth");
            Assert.Greater(race.SpeedAt(race.Total*.5f),inner,"the breach race isn't quicker than the tow line");Assert.Greater(ring.SpeedAt(10),race.SpeedAt(race.Total*.5f),"the berth ring isn't the quickest");
            Assert.Greater(tow.ReachAhead(tow.Total*.95f),tow.ReachAhead(tow.Total*.08f)*1.8f,"a tow's amber is no longer at the mouth than on the outer pass");
            float Lit(WreckTrack t){int amber=0;for(int k=0;k<t.lampCount;k++){var c=t.Lamp(k,t.Clock);if(c.r>200&&c.g<160)amber++;}return amber/(float)t.lampCount;}
            float towLit=Lit(tow),ringLit=Lit(ring);TestContext.WriteLine($"NACRE_AMBER tow={towLit:0.00} race={Lit(race):0.00} ring={ringLit:0.00}");
            Assert.Greater(ringLit,.35f,"the berth ring isn't mostly amber");Assert.Less(towLit,.25f,"the tow line is mostly amber: the outer pass should read quiet");
            // Each piece's own lamp is amber, and the lamp a little behind it is not.
            float now=tow.Clock;
            for(int i=0;i<tow.pieces.Length;i++){float a=tow.ArcOf(i,now);if(!tow.AtPlane(a))continue;Assert.IsTrue(tow.AmberAt(a+1,now),$"tow {i} has no amber ahead of it");}
        }

        /// <summary>A wreck on the berth ring meets a ship sitting on its track: its stretch is marked before it comes, it strikes inside the marking for its share of the hull's Integrity,
        /// never the last of it, and throws the ship clear off the track. The same ship sitting a hull clear of the track is never touched.</summary>
        [UnityTest] public IEnumerator AWreckOnItsTrackIsMarkedBeforeItComesAndThrowsAShipClear()
        {
            ToNacre();yield return Zone("NacreGrave");
            var ring=Track("The berth ring");var lead=Lead;lead.Health.InvulnerableUntil=0;float full=lead.Health.maximum;lead.Health.integrity=full;
            // A point on the ring two and a half seconds ahead of the next wreck to come round to it.
            float now=ring.Clock,arc=ring.ArcAfter(Mathf.Repeat(ring.TimeTo(Mathf.Repeat(ring.ArcOf(0,now),ring.Total))+2.6f,ring.Traverse));
            var spot=ring.PointAt(arc);Put(lead,spot);
            float deadline=Time.time+1.2f;while(ring.Marked==0&&Time.time<deadline){Put(lead,spot);yield return null;}
            Assert.Greater(ring.Marked,0,"nothing on the plane marks the ring ahead of a wreck coming round to a ship on it");
            bool marked=Telegraph.Live.Any(t=>t!=null&&t.Contains(spot));Assert.IsTrue(marked,"the marking doesn't cover the stretch the ship sits on");
            deadline=Time.time+5;var start=lead.transform.position;while(!log.Any(l=>l.StartsWith("WRECK_STRUCK"))&&Time.time<deadline){if(lead.motor is FlightMotor s)s.Move(Vector2.zero,false,false);yield return null;}
            Assert.IsTrue(log.Any(l=>l.StartsWith("WRECK_STRUCK")),"the wreck came round and never struck the ship sitting on its track");
            yield return new WaitForSeconds(.4f);
            float lost=(full-lead.Health.integrity)/full;Assert.Greater(lost,.04f,"the strike took nothing");Assert.Less(lost,.2f,$"the strike took {lost:P0}");
            Assert.Greater(Flat(lead.transform.position,spot),4,"the ship wasn't thrown clear of the track");
            Assert.Greater(Mathf.Abs(Vector2.Distance(new Vector2(lead.transform.position.x,lead.transform.position.z),Vector2.zero)-48),4.5f,"the ship was thrown along the ring, not off it");
            // A hull clear of the ring's marking, the next time a wreck goes by, is never touched.
            log.Clear();lead.Health.integrity=full;var clear=In(300,58);Put(lead,clear);
            deadline=Time.time+ring.Traverse/ring.pieces.Length+1;while(Time.time<deadline){Put(lead,clear);yield return null;}
            Assert.IsFalse(log.Any(l=>l.StartsWith("WRECK_STRUCK")),"a wreck struck a ship ten metres off its track");
        }

        /// <summary>Every wall of the field is where the reef is, never in open water: a hull's sphere flown down the lane's middle and both its sides, down the breach and round the basin
        /// meets no wall anywhere (the lane bends the whole way; chapter 6's corridor ran a wall 72 m past a bend into a current). And the field is shut everywhere but its ways: out from
        /// every point of the lane's edges there is a wall within two metres of where the water ends.</summary>
        [UnityTest] public IEnumerator NoWallStandsInOpenWaterAndEveryEdgeIsWalled()
        {
            ToNacre();yield return Zone("NacreGrave");
            var problems=new List<string>();float hull=Idra;
            for(float t=StartTurn+3;t<=EndTurn-3;t+=1)foreach(float off in new[]{-Half+Skin+hull,-8,0,8,Half-Skin-hull})
            {
                var p=At(t,off);var hit=WallsAt(p,hull).FirstOrDefault();if(hit!=null)problems.Add($"lane turn {t:0} offset {off:0.0}: {hit.name}");
            }
            var ray=OnPlan(BreachTurn,1);
            for(float r=BasinRim-1;r<=R(BreachTurn)-Half+1;r+=1)foreach(float side in new[]{-BreachHalf+Skin+hull,0,BreachHalf-Skin-hull})
            {var p2=OnPlan(BreachTurn,r)+new Vector2(ray.y,-ray.x)*side;var p=new Vector3(p2.x,1,p2.y);var hit=WallsAt(p,hull).FirstOrDefault();if(hit!=null)problems.Add($"breach r {r:0} side {side:0.0}: {hit.name}");}
            for(float t=0;t<360;t+=2)foreach(float r in new[]{BerthSolid+hull+.5f,48f,60,BasinRim-Skin-hull})
            {var p=In(t,r);var hit=WallsAt(p,hull).FirstOrDefault();if(hit!=null)problems.Add($"basin turn {t:0} r {r:0}: {hit.name}");}
            Assert.IsEmpty(problems,"a wall stands in open water:\n"+string.Join("\n",problems.Take(30)));
            // Shut: out past the outer edge everywhere, and past the inner edge wherever there is no breach and no mouth.
            for(float t=StartTurn+2;t<=EndTurn-2;t+=3)
            {
                var outward=OnPlan(t,1);
                if(!Physics.SphereCastAll(At(t,Half-hull-1),hull,new Vector3(outward.x,0,outward.y),6,~0,QueryTriggerInteraction.Ignore).Any(h=>IsWall(h.collider)))problems.Add($"nothing shuts the lane's outer edge at turn {t:0}");
                bool open=Mathf.Abs(Mathf.DeltaAngle(t,BreachTurn))<8||t>MouthTurn-1;
                if(!open&&!Physics.SphereCastAll(At(t,-Half+hull+1),hull,new Vector3(-outward.x,0,-outward.y),6,~0,QueryTriggerInteraction.Ignore).Any(h=>IsWall(h.collider)))problems.Add($"nothing shuts the lane's inner edge at turn {t:0}");
            }
            for(float t=94;t<MouthTurn-1;t+=3)
            {
                if(Mathf.Abs(Mathf.DeltaAngle(t,BreachTurn))<12)continue;var outward=OnPlan(t,1);
                if(!Physics.SphereCastAll(In(t,BasinRim-hull-1),hull,new Vector3(outward.x,0,outward.y),6,~0,QueryTriggerInteraction.Ignore).Any(h=>IsWall(h.collider)))problems.Add($"nothing shuts the basin's rim at turn {t:0}");
            }
            Assert.IsEmpty(problems,"the field has an open edge:\n"+string.Join("\n",problems.Take(30)));
        }

        /// <summary>The lane a player is sent down is open by more than two hulls' width (9 m) all the way round, with nothing solid across it and off the tow line's marked
        /// stretch: across the lane at every degree, the widest run of water no solid stands in and no track marks. The tug and her raft once lay 2 to 10 m out across the outer
        /// pass's quiet side, and a ship got past only at the outer edge (chapter 9's routes, November 14): the same raft is stood in the field first, and must be found.</summary>
        [UnityTest] public IEnumerator TheLaneIsOpenByMoreThanTwoHullsAllTheWayRound()
        {
            ToNacre();yield return Zone("NacreGrave");
            const float TowOffset=-11,TowFrom=16,TowTo=446,TowMarkHalf=4,Needed=9,Step=.1f;
            List<string> Narrow()
            {
                Physics.SyncTransforms();var narrow=new List<string>();
                for(float t=StartTurn+3;t<=EndTurn-3;t+=1)
                {
                    // The open run's extent, sampled every tenth of a metre with a point's worth of probe: the raft fits the lane with half a metre to spare, so a coarse
                    // grid would read a 9.3 m channel as 9.
                    float best=0,bestFrom=0;float? from=null,last=null;
                    for(int k=0;k<=Mathf.RoundToInt((Half-Skin)*2/Step);k++)
                    {
                        float off=-Half+Skin+k*Step;
                        bool marked=t>=TowFrom&&t<=TowTo&&Mathf.Abs(off-TowOffset)<=TowMarkHalf;
                        bool solid=Physics.OverlapSphere(At(t,off),.02f,~0,QueryTriggerInteraction.Ignore).Any(c=>c.GetComponentInParent<CombatActor>()==null);
                        if(marked||solid){from=last=null;continue;}
                        from??=off;last=off;float run=last.Value-from.Value+Step;if(run>best){best=run;bestFrom=from.Value;}
                    }
                    if(best<=Needed)narrow.Add($"turn {t:0}: widest open {best:0.0} m (from {bestFrom:0.0} out)");
                }
                return narrow;
            }
            // The raft as it lay: the tug 4 m out (3.8 m solid) and four hulls 2 to 7.5 m out either side of her.
            var old=new GameObject("The raft as it lay");
            foreach(var (turn,off,radius) in new[]{(176f,4f,3.8f),(169f,6f,2.16f),(183f,7.5f,2.52f),(163f,2f,1.8f),(189f,3f,2.16f)})
            {var hull=new GameObject("Old raft hull",typeof(SphereCollider));hull.transform.SetParent(old.transform,false);hull.transform.position=At(turn,off);hull.GetComponent<SphereCollider>().radius=radius;}
            var before=Narrow();TestContext.WriteLine("NACRE_LANE_WITH_THE_OLD_RAFT "+string.Join(" | ",before.Take(8)));
            Assert.IsTrue(before.Any(n=>n.StartsWith("turn 176:")),"the check doesn't find the raft that shut the outer pass's quiet side: "+string.Join(" | ",before.Take(8)));
            Object.DestroyImmediate(old);yield return null;
            var narrow=Narrow();
            Assert.IsEmpty(narrow,"the lane is narrower than two hulls with nothing across it:\n"+string.Join("\n",narrow.Take(30)));
        }

        /// <summary>The three lamps as a player finds them: offered once Bryn has named them at the outer marker, each a conversation that sets its own flag, each lamp going dark once
        /// found; all three in, the field's FlagAll says so, and the story turns back to Many Hands.</summary>
        [UnityTest] public IEnumerator BrynNamesTheLampsAndEachIsFoundInItsOwnTalk()
        {
            ToNacre();yield return Zone("NacreGrave");
            var oda=Point("home.odaFound");Assert.IsFalse(oda.Available,"Oda's hauler is offered before Bryn has spoken");
            // Across the outer marker's line: Bryn's hail.
            var line=Point("home.nacreReached");DialogueSystem.Current.AutoAdvance=true;
            Put(Lead,line.transform.position-line.transform.forward*8);
            float deadline=Time.unscaledTime+6;var way=line.transform.forward;
            while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline){Lead.motor.Move(new Vector2(way.x,way.z),false,false);yield return null;}
            yield return Settle();
            Assert.Contains("BrynNacreApproach",heard,"crossing the outer marker didn't hear Bryn");Assert.IsTrue(Has("home.nacreSpoken"),"Bryn's hail didn't name the lamps");Assert.IsTrue(Has("home.nacreReached"));
            foreach(var (flag,node,at) in new[]{("home.odaFound","OdaFound",HaulerHail),("home.tugFound","BrynWelcomeBack",TugHail),("home.navigatorsFound","BrynNavigators",PodHail)})
            {
                Put(Lead,at);yield return Frames(3);var p=Point(flag);yield return Discover(p);
                Assert.AreEqual(node,heard.LastOrDefault());Assert.IsTrue(Has(flag),node+" didn't set "+flag);
                var lamp=All<SignalBeacon>().OrderBy(b=>Flat(b.transform.position,at)).First();Assert.IsTrue(lamp.Quiet,node+"'s lamp still calls once found");
            }
            yield return Frames(3);Assert.IsTrue(Has("home.nacreDone"),"all three lamps found and the field's count didn't say so");
            Assert.AreEqual(4,Step("HouseLeaves"),"the three lamps didn't turn the story back to Many Hands");
            // Neve's offer, once.
            Put(Lead,NeveHail);yield return Frames(3);yield return Discover(Point("neve.offered"));Assert.AreEqual(0,Step("ReturnAddress"),"Neve's hail didn't start her parcels");
            Assert.IsFalse(Point("neve.offered").Available,"Neve's offer is offered twice");
        }

        // ================================================================== the way in, and Many Hands
        static string Objective()=>Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Objective").text.Split('\n')[0].Trim();
        /// <summary>Chapter 8 hands on by itself (D273): Abri turns the convoy for Nacre ten seconds after chapter 8's card, at Harrowgate's cells, or aboard Many Hands for a save that
        /// left first. From a chapter 9 party standing in either place her word plays with nothing pressed and begins the chapter, and the first step's arrow is then followed as a
        /// player follows it, one way out at a time, to a warp to Nacre that is drawn and open: from Harrowgate by the quay's launch to the Picket's north end, from Many Hands by
        /// the launch to the hull's stern. ContentFrameworkTests.HandOffs can't hold this hand-off: its form stands where the next chapter can't begin by itself and asks for a prompt
        /// that begins it, and here nothing is pressed.</summary>
        [UnityTest] public IEnumerator ChapterEightHandsOnByItselfAndTheFirstArrowLeadsToAnOpenWarpToNacre([Values("Harrowgate","ManyHands")] string place)
        {
            Assert.IsTrue(Has("chapter8.complete"),"fixture: chapter 9's loadout hasn't finished chapter 8");Assert.Less(Step("HouseLeaves"),0,"fixture: the chapter is begun already");
            Assert.IsFalse(Has("home.turnedForNacre"),"fixture: Abri has turned the convoy already");
            yield return Zone(place);
            var turn=All<AfterTalk>().Single(t=>t.node=="AbriTurnsForNacre");Assert.AreEqual("chapter8.complete",turn.flag);Assert.AreEqual("home.turnedForNacre",turn.heardFlag);
            Assert.AreEqual("Keeper",turn.speaker);Assert.AreEqual(0,turn.range,"Abri's word waits for the party to stand somewhere in particular");
            DialogueSystem.Current.AutoAdvance=true;float deadline=Time.unscaledTime+20;while(!heard.Contains("AbriTurnsForNacre")&&Time.unscaledTime<deadline)yield return null;
            yield return Settle();
            Assert.Contains("AbriTurnsForNacre",heard,$"Abri never turned the convoy for Nacre in {place}");
            Assert.AreEqual(0,Step("HouseLeaves"),"Abri's word didn't begin the chapter");Assert.IsTrue(Has("home.turnedForNacre"),"Abri's word didn't open the lane");
            var path=new List<string>{place};
            for(int hop=0;hop<3&&SceneFlow.Current.Zone!="NacreGrave";hop++)
            {
                string here=SceneFlow.Current.Zone;InteractionPrompt way=null;float until=Time.unscaledTime+4;
                while(way==null&&Time.unscaledTime<until)
                {
                    yield return null;var goal=Goal();if(!goal.HasValue)continue;
                    way=InteractionPrompt.Active.Where(p=>(p is WarpBeacon||p is DockingPad)&&Flat(p.transform.position,goal.Value)<3).OrderBy(p=>Flat(p.transform.position,goal.Value)).FirstOrDefault();
                }
                Assert.AreEqual("Take the lane to Nacre",Objective(),$"in {here} the panel doesn't say the chapter's first step");
                Assert.IsNotNull(way,$"in {here} the first step's arrow points at {Goal()}, and no way out stands there");
                string to=way is WarpBeacon warp?warp.scene:((DockingPad)way).scene;
                if(way is WarpBeacon lane&&to=="NacreGrave")
                {
                    Assert.IsTrue(string.IsNullOrEmpty(lane.requiredFlag)||Has(lane.requiredFlag),$"the warp to Nacre the arrow finds in {here} is shut on {lane.requiredFlag}");
                    Assert.AreEqual("Warp — Nacre Grave",lane.prompt,$"the warp to Nacre in {here} doesn't say where it goes");
                }
                Put(Lead,way.transform.position+Vector3.back*1.5f);yield return Frames(3);
                Assert.IsTrue(way.Available,$"\"{way.prompt}\" in {here} isn't offered: something is awake by it");
                way.Interact();yield return Arrive(to);path.Add(to);
            }
            var expected=place=="Harrowgate"?new[]{"Harrowgate","PicketOfTeeth","NacreGrave"}:new[]{"ManyHands","ManyHandsHull","NacreGrave"};
            CollectionAssert.AreEqual(expected,path,"the first step's arrow didn't lead the way a player goes to Nacre");
            Assert.AreEqual(1,Step("HouseLeaves"),"arriving at Nacre didn't move the story on");
        }

        /// <summary>What the warp to Nacre shows and says in every state a player can be in, in both scenes it stands in: before chapter 8 ends, and after it until Abri has spoken,
        /// nothing is drawn or offered; from her word it stands open and says where it goes. The shared lock test found the warp first staged here (drawn from chapter 8's end, shut
        /// until her word) still refusing once it had opened, because a press while it was hidden became the words it woke with (November 13). The same states are walked first on
        /// a warp built that way in the test, and must find it wrong.</summary>
        [UnityTest] public IEnumerator TheWarpToNacreIsDrawnAndOpenOnlyOnceAbriHasTurnedTheConvoy([Values("PicketOfTeeth","ManyHandsHull")] string from)
        {
            Flag("home.turnedForNacre",false);Flag("chapter8.complete",false);
            yield return Zone(from,from=="PicketOfTeeth"?"Harrowgate":"Arrival");
            var old=new GameObject("The warp to Nacre as first staged");old.SetActive(false);old.transform.position=Lead.transform.position+Vector3.right*300;
            var shut=old.AddComponent<WarpBeacon>();shut.prompt="Warp — Nacre Grave";shut.scene="NacreGrave";shut.spawn="Arrival";shut.range=5;
            shut.requiredFlag="home.turnedForNacre";shut.lockedPrompt="No lane to Nacre yet: Keeper Abri turns the convoy for it first";
            var oldGate=new GameObject("Its gate").AddComponent<FlagGate>();oldGate.flag="chapter8.complete";oldGate.showWhenSet=true;oldGate.targets=new[]{old};
            var wrong=new List<string>();yield return States(shut,wrong);
            TestContext.WriteLine("NACRE_WARP_AS_FIRST_STAGED "+string.Join(" | ",wrong));
            Assert.IsTrue(wrong.Any(w=>w.Contains("still says")),"walking the states doesn't find the first staging still refusing once open: "+string.Join(" | ",wrong));
            Object.Destroy(old);Object.Destroy(oldGate.gameObject);yield return Frames(2);
            var warp=All<WarpBeacon>().Single(w=>w.scene=="NacreGrave");Assert.AreEqual("Arrival",warp.spawn);
            var problems=new List<string>();yield return States(warp,problems);
            Assert.IsEmpty(problems,$"the warp to Nacre in {from}:\n"+string.Join("\n",problems));
            Put(Lead,warp.transform.position+Vector3.back*2);yield return Frames(3);
            Assert.IsTrue(warp.Available,$"the warp to Nacre in {from} isn't offered once open: something is awake by it");
        }
        /// <summary>The warp in each state, as a player meets it and as the shared lock test presses it (every warp with a flag, drawn or not).</summary>
        IEnumerator States(WarpBeacon warp,List<string> problems)
        {
            foreach(bool told in new[]{false,true})
            {
                Flag("chapter8.complete",told);Flag("home.turnedForNacre",false);yield return Frames(3);
                string when=told?"after chapter 8, before Abri's word":"before chapter 8 ends";
                if(warp.gameObject.activeInHierarchy)problems.Add($"{when} it is drawn, {(string.IsNullOrEmpty(warp.requiredFlag)?"open":"shut")}, saying \"{warp.prompt}\"");
                if(!told&&!string.IsNullOrEmpty(warp.requiredFlag)){warp.Interact();yield return Frames(2);if(SceneFlow.Current.Loading)problems.Add($"{when} a press goes through");}
            }
            Flag("home.turnedForNacre",true);yield return Frames(3);
            if(!warp.gameObject.activeInHierarchy||!InteractionPrompt.Active.Contains(warp))problems.Add("once Abri has spoken it isn't drawn and offered");
            if(!string.IsNullOrEmpty(warp.requiredFlag)&&!Has(warp.requiredFlag))problems.Add($"once Abri has spoken it is still shut on {warp.requiredFlag}");
            if(warp.prompt!="Warp — Nacre Grave")problems.Add($"once Abri has spoken it still says \"{warp.prompt}\"");
        }

        [UnityTest] public IEnumerator ManyHandsHearsMeretThenTheLessonThenMaruAndEndsTheChapterAtTheChart()
        {
            ToNacre();foreach(string f in new[]{"home.nacreReached","home.nacreSpoken","home.odaFound","home.tugFound","home.navigatorsFound"})Flag(f);State.questSteps["HouseLeaves"]=4;
            yield return Zone("ManyHands");
            yield return Frames(4);Assert.IsTrue(Has("home.nacreDone"),"Many Hands' count of the three lamps didn't say so");
            var notice=Point("home.noticeHeard");Assert.AreEqual("MeretBerthNotice",notice.dialogueNode);Assert.IsTrue(notice.startsOnCrossing,"Meret's notice can be walked round");
            Assert.IsFalse(Point("home.chartLent").Available,"the pencil is offered before Meret's notice");Assert.IsFalse(Point("home.crawlNamed").Available,"Maru is asked before the lesson");
            yield return Discover(notice);Assert.AreEqual("MeretBerthNotice",heard.LastOrDefault());Assert.IsTrue(Has("home.noticeHeard"));
            yield return Discover(Point("home.chartLent"));Assert.AreEqual("BrynChartLesson",heard.LastOrDefault());
            // The story's arrow, on step 4, goes to Maru in the raft bay.
            yield return Frames(4);var goal=Goal();var maru=Point("home.crawlNamed");Assert.IsTrue(goal.HasValue,"the objective points at nothing on Maru's step");
            Assert.Less(Flat(goal.Value,maru.transform.position),3,$"the objective points at {goal}, not at Maru");
            yield return Discover(maru);Assert.AreEqual("IdraTownMovers",heard.LastOrDefault());Assert.AreEqual(5,Step("HouseLeaves"),"Maru's answer didn't send the story to the Crawl");
            Assert.IsTrue(Has("home.crawlNamed"));
            // Oda aboard, her words on the body Many Hands stands by her cabin.
            var oda=All<Npc>().Single(n=>n.speaker=="Oda");Assert.IsTrue(oda.gameObject.activeInHierarchy,"Oda isn't aboard once found");Assert.AreEqual("OdaAboard",oda.repeatNode);
            // The chapter's end at the same table, only once the Cutter is towed off.
            Assert.IsFalse(Point("home.chartRead").Available,"the fleet's chart is offered before the Ring");
            Assert.AreEqual("OrrinChalk",Point("home.chartRead").dialogueNode);Assert.AreEqual("FleetChart",Point("home.chartRead").report);
        }

        [UnityTest] public IEnumerator ChapterNinesBeatsWaitForChapterNine()
        {
            yield return null;
            var chapter9=new[]{"AbriNamesOnTheWall","DraskCount"};
            GameServices.Current.NewGame();DevLoadout.Apply("chapter8");State.zone="ManyHands";Flag("met.Drask");
            foreach(string speaker in new[]{"Keeper","Drask"}){var node=SideStories.NodeFor(speaker,State);Assert.IsFalse(chapter9.Contains(node),$"{node} wins in chapter 8");}
            Flag("chapter8.complete");Assert.AreEqual("AbriSmiths",SideStories.NodeFor("Keeper",State),"chapter 8's last aboard line was lost to chapter 9's");
            Flag("home.odaFound");Assert.AreEqual("DraskCount",SideStories.NodeFor("Drask",State));Flag("drask.asked");Assert.AreNotEqual("DraskCount",SideStories.NodeFor("Drask",State),"Drask asks his count twice");
            Flag("home.noticeHeard");Assert.AreEqual("AbriNamesOnTheWall",SideStories.NodeFor("Keeper",State));
            State.zone="Harrowgate";Assert.AreNotEqual("AbriNamesOnTheWall",SideStories.NodeFor("Keeper",State),"Abri's wall of names plays off the ship");
        }
    }
}
