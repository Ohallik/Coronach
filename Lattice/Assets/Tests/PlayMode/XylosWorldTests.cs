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
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 3's first two places as a player meets them, in the built scenes: Xylos Nearspace (the lane's three voices, three barges held by knots and let go, the swarms, the
    /// two hoards, the delta beacon that stays dark until the third barge, the lane's limits and the warp home) and Rootmarket (the arrival's rope, Ruva's terms, the notice and the seed
    /// house, who stands where, the two stalls, the caches, the chart), the story's steps from Tallow's warp to the river gate, and a save and a Continue in each. What every zone gets
    /// from the shared checks (prompts reachable on foot, exits named, locks worded, props on the ground, light, music) is theirs and not repeated here: this chapter's words for those
    /// checks are in XylosCombatTests' expectations. Coordinates are the map briefs' (docs/maps/XylosNearspace.md, Rootmarket.md), which are the builder's own logged ones.</summary>
    public sealed class XylosWorldTests
    {
        readonly List<string> heard=new(),log=new();
        void Talked(string node,string speaker,System.Action<bool> done)=>heard.Add(node);
        void Looked(DiscoveryPoint point){if(!string.IsNullOrEmpty(point.dialogueNode))heard.Add(point.dialogueNode);}
        static readonly string[] Heeded={"BARGE_FREED","FLAG_ALL","ROPE_HAULED","CACHE_OK","ENCOUNTER_CLEAR","WALL_CONTACT"};
        void Logged(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        Picker picker;IDialogueView panelView;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter3");heard.Clear();log.Clear();picker=null;panelView=null;
            Npc.TalkRequested+=Talked;DiscoveryPoint.DialogueRequested+=Looked;Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Npc.TalkRequested-=Talked;DiscoveryPoint.DialogueRequested-=Looked;Application.logMessageReceived-=Logged;
            var dialogue=DialogueSystem.Current;
            if(dialogue!=null)
            {
                if(picker!=null&&ReferenceEquals(dialogue.View,picker))dialogue.View=panelView;
                if(dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            }
            var shop=Object.FindFirstObjectByType<ShopUi>();if(shop!=null&&shop.IsOpen)shop.Close();
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the lane, as the builder lays it (XylosBuilder.Lane, LaneHalf, Along, Beside: editor code a test can't call)
        static readonly Vector2[] Lane={new(0,0),new(30,240),new(130,520),new(300,800),new(520,1000),new(760,1120),new(980,1180)};
        const float LaneHalf=62,LaneLength=1660.4f;
        static Vector2 Along(float s,out Vector2 heading)
        {
            heading=Vector2.up;
            for(int i=1;i<Lane.Length;i++)
            {
                float leg=Vector2.Distance(Lane[i-1],Lane[i]);heading=(Lane[i]-Lane[i-1])/leg;
                if(s<=leg||i==Lane.Length-1)return Lane[i-1]+heading*s;
                s-=leg;
            }
            return Lane[0];
        }
        /// <summary>A point so far along the lane and so far to starboard (port negative), at the flight plane.</summary>
        static Vector3 Beside(float s,float starboard){var p=Along(s,out var h)+new Vector2(h.y,-h.x)*starboard;return new Vector3(p.x,1,p.y);}
        /// <summary>How far a point is from the lane's middle line, on the plan.</summary>
        static float FromLane(Vector3 at)
        {
            var p=new Vector2(at.x,at.z);float best=float.MaxValue;
            for(int i=1;i<Lane.Length;i++){var d=Lane[i]-Lane[i-1];float t=Mathf.Clamp01(Vector2.Dot(p-Lane[i-1],d)/d.sqrMagnitude);best=Mathf.Min(best,Vector2.Distance(p,Lane[i-1]+d*t));}
            return best;
        }
        /// <summary>The map's places for each barge (docs/maps/XylosNearspace.md, Evidence): where it lies snagged and where it comes to rest once free.</summary>
        static readonly Vector3[] Snagged={new(56.9f,1,241.1f),new(344.2f,1,878.1f),new(757.2f,1,1151.4f)},Rested={new(54.8f,1,288.6f),new(395.2f,1,878.5f),new(802.2f,1,1123.2f)};
        static readonly Vector3 LaneArrival=new(0,1,6),HomeWarp=new(-16,1,-2),DeltaBeacon=new(969.9f,1,1177.3f),FromTown=new(945.7f,1,1174.8f);
        // Rootmarket's places, on the plan (docs/maps/Rootmarket.md; XylosBuilder.Rootmarket's RootArrival, RuvaAtMooring and the rest).
        static readonly Vector2 TownArrival=new(0,17),RuvaMooring=new(28.5f,21),RuvaSeedHouse=new(5.5f,68.5f),TeoPool=new(-8.5f,24.6f),JaskFerry=new(-25,47.6f),BinnaKitchen=new(24.6f,68),
            HaddaPress=new(35.4f,46.6f),SennIsland=new(-46.4f,13.6f),Notice=new(-2.7f,72.3f),Door=new(2.5f,72.3f),RiverGate=new(60.2f,43),Crossing=new(23.5f,24),IslandMoored=new(38.6f,13.2f),PressSpawn=new(34,41.2f);

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static int Step(string quest)=>State.questSteps.TryGetValue(quest,out var step)?step:-1;
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static float Flat(Vector3 a,Vector2 b)=>Vector2.Distance(new Vector2(a.x,a.z),b);
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>The story at Nearspace's door: the lane charted and the warp off Tallow's approach the next step, so arriving reports it.</summary>
        static void ToTheLane(){Flag("xylos.charted");State.questSteps["SeedRoad"]=1;}
        /// <summary>The story at Rootmarket's door: three barges free and the next step the landing.</summary>
        static void Landed(){Flag("xylos.charted");foreach(string f in new[]{"xylos.barge1","xylos.barge2","xylos.barge3","xylos.bargesFree"})Flag(f);State.questSteps["SeedRoad"]=3;}
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
        /// <summary>The player's way back in: the autosave as it stands, the title, and Continue.</summary>
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
        /// <summary>On the floor at a plan point, as near as the town's paths have it.</summary>
        static Vector3 Floor(Vector2 at,float level=1)
        {
            Assert.IsTrue(NavMesh.SamplePosition(new Vector3(at.x,level,at.y),out var hit,2.5f,NavMesh.AllAreas),$"fixture: no floor near ({at.x}, {at.y})");
            return hit.position;
        }
        static IEnumerator Settle()
        {
            float deadline=Time.unscaledTime+25;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(DialogueSystem.Current.Running,"a conversation never ended");
            yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Talk(Npc npc){DialogueSystem.Current.AutoAdvance=true;Assert.IsTrue(npc.Available,npc.speaker+" can't be talked to");npc.Interact();yield return null;yield return Settle();}
        static IEnumerator Discover(DiscoveryPoint point){DialogueSystem.Current.AutoAdvance=true;Assert.IsTrue(point.Available,$"\"{point.prompt}\" isn't offered");point.Interact();yield return null;yield return Settle();}
        /// <summary>The one person of that name standing here now. Two at once would be one person in two places.</summary>
        static Npc Here(string speaker)
        {
            var here=All<Npc>().Where(n=>n.speaker==speaker&&n.gameObject.activeInHierarchy).ToArray();
            Assert.LessOrEqual(here.Length,1,speaker+" stands in two places at once");return here.FirstOrDefault();
        }
        static DiscoveryPoint Point(string flag)=>All<DiscoveryPoint>().Single(p=>p.flag==flag);
        static LootCache Cache(string id)=>All<LootCache>().SingleOrDefault(c=>c.cacheId==id);
        static EncounterVolume Fight(string id)=>All<EncounterVolume>().SingleOrDefault(e=>e.encounterId==id);
        static SnaggedBarge Barge(int n)=>All<SnaggedBarge>().Single(b=>b.freedFlag=="xylos.barge"+n);
        static DamageType Neutral(Health body)=>System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=body.weakness&&t!=body.resistance);
        static void Kill(Health body)=>body.Receive(new DamagePacket{source=Lead.Health,amount=body.maximum*20,type=Neutral(body)});
        /// <summary>Living bodies of this kind within a reach of a point, on the plan.</summary>
        static Health[] Bodies(string id,Vector3 near,float reach)=>Health.All.Where(h=>h!=null&&!h.friendly&&h.Alive&&h.id==id&&Flat(h.transform.position,near)<reach).ToArray();
        static int Held(string id){State.consumables.TryGetValue(id,out int item);State.materials.TryGetValue(id,out int material);return item+material;}
        static string Objective()=>ObjectiveHud.CurrentLabel;
        static Vector3? Goal()=>Object.FindFirstObjectByType<ObjectiveHud>().Goal;
        /// <summary>Cuts all three of a barge's knots, the way a fight there ends, and waits for the barge to say it is free.</summary>
        static IEnumerator Free(int n)
        {
            var knots=Fight("Barge"+n+"_Knots");Assert.IsNotNull(knots,"fixture: barge "+n+" has no knots");
            knots.Begin();yield return Frames(2);
            foreach(var knot in Bodies("SnagvineNode",Barge(n).transform.position,18))Kill(knot);
            float deadline=Time.unscaledTime+3;while(!Has("xylos.barge"+n)&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(Has("xylos.barge"+n),"fixture: barge "+n+" never came free");
        }
        /// <summary>Flies the lead ship across a crossing line as a player would: from nine metres short of it, along the lane, until a conversation starts or it is nine metres past.</summary>
        static IEnumerator FlyAcross(DiscoveryPoint line)
        {
            var ahead=line.transform.forward;ahead.y=0;ahead.Normalize();var at=line.transform.position;at.y=1;
            DialogueSystem.Current.AutoAdvance=true;Put(Lead,at-ahead*9);yield return null;
            float deadline=Time.unscaledTime+5;
            while(!DialogueSystem.Current.Running&&Vector3.Dot(Lead.transform.position-at,ahead)<9&&Time.unscaledTime<deadline){Lead.motor.Move(new Vector2(ahead.x,ahead.z),false,false);yield return null;}
            yield return Settle();
        }
        static IEnumerator Haul(MooringHaul rope)
        {
            for(int pull=1;pull<=rope.pulls;pull++)
            {
                Assert.IsTrue(rope.Available,$"pull {pull} of {rope.pulls} isn't offered");rope.Interact();
                float deadline=Time.unscaledTime+5;while(rope.Progress<pull/(float)rope.pulls-.01f&&Time.unscaledTime<deadline)yield return null;
                yield return Frames(2);
            }
            float until=Time.unscaledTime+3;while(!Has("xylos.ropeHauled")&&Time.unscaledTime<until)yield return null;
        }
        static string Heading(ShopUi shop)=>shop.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name=="Heading").Select(t=>t.text).FirstOrDefault()??"";
        /// <summary>A reader who takes the choice that says this word: the conversation runs as it would on the pad, with the line pressed on each time and that row picked.</summary>
        sealed class Picker:IDialogueView
        {
            public string want;public readonly List<string> offered=new();
            public bool AdvanceRequested=>true;
            public void Show(string speaker,PortraitEmotion emotion,Sprite portrait,string body){}
            public void SetVisibleCharacters(int count){}
            public void Hide(){}
            public void Options(IReadOnlyList<string> labels,System.Action<int> selected)
            {offered.Clear();offered.AddRange(labels);int row=offered.FindIndex(l=>l.IndexOf(want,System.StringComparison.OrdinalIgnoreCase)>=0);selected(row<0?0:row);}
            public void ClearOptions(){}
        }
        IEnumerator Choose(Npc npc,string want)
        {
            var dialogue=DialogueSystem.Current;panelView=dialogue.View;picker=new Picker{want=want};dialogue.View=picker;dialogue.AutoAdvance=false;
            npc.Interact();yield return null;
            float deadline=Time.unscaledTime+20;while(dialogue.Running&&Time.unscaledTime<deadline)yield return null;
            dialogue.View=panelView;
            Assert.IsFalse(dialogue.Running,npc.speaker+"'s conversation never ended");
            Assert.IsTrue(picker.offered.Any(l=>l.IndexOf(want,System.StringComparison.OrdinalIgnoreCase)>=0),$"{npc.speaker} offers no choice that says '{want}': {string.Join(" / ",picker.offered)}");
            yield return Frames(3);
        }

        // ================================================================== Xylos Nearspace
        [UnityTest] public IEnumerator EachVoiceOnTheLaneSpeaksOnceAsTheShipCrossesItsLine()
        {
            ToTheLane();yield return Zone("XylosNearspace");
            var expected=new[]{("xylos.teoHailed","TeoBargeHail",46f),("xylos.patrolHeard","PatrolHail",470f),("xylos.claimSeen","TeoClaimDrones",1386f)};
            var lines=All<DiscoveryPoint>().Where(p=>p.startsOnCrossing).ToArray();
            CollectionAssert.AreEquivalent(expected.Select(e=>e.Item2),lines.Select(l=>l.dialogueNode),"the lane's voices aren't Teo's hail, the patrol's and Teo's sight of the claim drones");
            foreach(var (flag,node,along) in expected)
            {
                var line=lines.Single(l=>l.dialogueNode==node);
                Assert.AreEqual(flag,line.flag,node+" sets the wrong flag");
                Assert.Less(Flat(line.transform.position,Beside(along,0)),2,$"{node}'s line isn't {along} m along the lane, where the map has it");
                Assert.IsFalse(Has(flag),"fixture: "+flag+" is set before anyone has flown across its line");
                yield return FlyAcross(line);
                Assert.AreEqual(1,heard.Count(h=>h==node),$"{node} didn't play as the ship crossed its line {along} m along: heard {string.Join(", ",heard)}");
                Assert.IsTrue(Has(flag),node+" played and "+flag+" wasn't set");
                yield return FlyAcross(line);
                Assert.AreEqual(1,heard.Count(h=>h==node),node+" played again when the ship crossed its line a second time");
            }
            Assert.AreEqual(0,Step("ClaimTags"),"Teo's sight of the claim drones didn't start his side quest");
            // Heard is heard for good: the zone loaded again plays none of them.
            yield return Zone("XylosNearspace");
            foreach(var line in All<DiscoveryPoint>().Where(p=>p.startsOnCrossing).ToArray())yield return FlyAcross(line);
            foreach(var (_,node,_) in expected)Assert.AreEqual(1,heard.Count(h=>h==node),node+" played again after the zone was loaded again");
        }

        [UnityTest] public IEnumerator EachBargeComesFreeWhenItsLastKnotIsCutAndComesToRestInTheLane()
        {
            ToTheLane();yield return Zone("XylosNearspace");
            Assert.AreEqual(2,Step("SeedRoad"),"fixture: arriving in Nearspace puts the story at the barges");
            Assert.AreEqual(3,All<SnaggedBarge>().Length,"Nearspace doesn't hold three snagged barges");
            var lit=All<FlagGate>().Single(g=>g.name=="The delta beacon lights").targets[0];var dark=All<FlagGate>().Single(g=>g.name=="The delta beacon is dark").targets[0];
            for(int n=1;n<=3;n++)
            {
                var barge=Barge(n);var knots=Fight("Barge"+n+"_Knots");
                Assert.IsNotNull(knots,$"barge {n} has no knots to cut");Assert.AreEqual(knots.encounterId,barge.knotsEncounter,$"barge {n} listens for the wrong fight");
                Assert.Less(Flat(barge.transform.position,Snagged[n-1]),1.5f,$"barge {n} isn't held where the map puts it");
                Assert.IsFalse(barge.Free,$"barge {n} is free before anything is cut");
                Assert.AreEqual(3,barge.cables.Count(c=>c.activeSelf),$"barge {n} is held by three knots and doesn't show three cables");
                var shown=Bodies("SnagvineNode",barge.transform.position,18);
                Assert.AreEqual(3,shown.Length,$"barge {n}'s three knots aren't on show before its fight: a held barge has to be seen to be held");
                knots.Begin();yield return Frames(2);
                Assert.AreEqual(3,knots.Remaining,$"barge {n}'s fight isn't its three knots");
                var live=Bodies("SnagvineNode",barge.transform.position,18);CollectionAssert.AreEquivalent(shown,live,$"barge {n}: the knots that woke aren't the ones on show");
                for(int k=0;k<3;k++)
                {
                    Kill(live[k]);yield return null;yield return new WaitForSecondsRealtime(.8f);
                    if(k==2)break;
                    Assert.IsFalse(barge.Free,$"barge {n} came free with {2-k} of its knots still holding it");Assert.IsFalse(Has("xylos.barge"+n));
                    Assert.AreEqual(2-k,barge.cables.Count(c=>c.activeSelf),$"barge {n}: {k+1} knots cut and the cables drawn don't match the {2-k} left");
                }
                Assert.IsTrue(Has("clear.Barge"+n+"_Knots"),$"barge {n}'s last knot is cut and its fight isn't cleared");
                Assert.IsTrue(barge.Free,$"barge {n} didn't come free when its last knot was cut");Assert.IsTrue(Has("xylos.barge"+n));
                Assert.AreEqual(1,log.Count(l=>l=="BARGE_FREED xylos.barge"+n),$"barge {n} was freed other than once");
                Assert.AreEqual(n,State.questCounts.TryGetValue("SeedRoad/2",out int counted)?counted:0,$"barge {n} free and the story counts {counted}");
                // Teo's line about this barge, once, on the radio.
                float deadline=Time.unscaledTime+6;while(!Has("xylos.teoBarge"+n)&&Time.unscaledTime<deadline)yield return null;
                Assert.IsTrue(Has("xylos.teoBarge"+n),$"Teo says nothing on the radio as barge {n} comes free");
                yield return Frames(2);
                Assert.AreEqual(n==3,Has("xylos.bargesFree"),n<3?$"the delta beacon is told the barges are free with {n} of three":"three barges free and xylos.bargesFree isn't set");
                Assert.AreEqual(n==3,lit.activeSelf,n<3?"the delta beacon is lit before the third barge":"the delta beacon stays dark with all three barges free");
                Assert.AreEqual(n<3,dark.activeSelf,"the delta beacon's dark lanterns don't follow the barges");
            }
            Assert.AreEqual(3,Step("SeedRoad"),"three barges free and the story hasn't moved on to the landing");
            Assert.AreEqual(1,log.Count(l=>l=="FLAG_ALL xylos.bargesFree"),"xylos.bargesFree wasn't set once, by the barges");
            float landing=Time.unscaledTime+8;while(!Has("xylos.teoLanding")&&Time.unscaledTime<landing)yield return null;
            Assert.IsTrue(Has("xylos.teoLanding"),"Teo never calls the landing once all three are free");
            // Each drifts back into the lane and stays there, as cover.
            float settle=Time.unscaledTime+16;
            while(Enumerable.Range(1,3).Any(n=>Flat(Barge(n).transform.position,Rested[n-1])>.3f)&&Time.unscaledTime<settle)yield return null;
            for(int n=1;n<=3;n++)
            {
                var barge=Barge(n);
                Assert.Less(Flat(barge.transform.position,Rested[n-1]),1,$"barge {n} came to rest at {barge.transform.position}, not where the map has it ({Rested[n-1]})");
                Assert.Less(FromLane(barge.transform.position),LaneHalf-40,$"barge {n} rests {FromLane(barge.transform.position):0} m off the lane's middle: it isn't back in the lane");
                Assert.AreEqual(1,barge.transform.position.y,.5f,$"barge {n} rests off the flight plane: it can't be cover");
                Assert.IsFalse(barge.cables.Any(c=>c.activeSelf),$"barge {n} is free and still drawn on its cables");
                Assert.IsFalse(barge.whileSnagged.activeSelf,$"barge {n} is free and still sits in its weed");
                Assert.IsNotNull(barge.GetComponentInChildren<Collider>(),$"barge {n} has nothing solid: it is no cover");
            }
        }

        /// <summary>Clear water a few metres off a barge's hull, on the side a point lies to: where a ship that has just cut the knot there would be.</summary>
        static Vector3 OffTheHull(SnaggedBarge barge,Vector3 toward)
        {
            var centre=barge.transform.position;centre.y=1;var side=toward-centre;side.y=0;side.Normalize();
            var met=Physics.RaycastAll(centre+side*80,-side,80,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<SnaggedBarge>()==barge).OrderBy(h=>h.distance).ToArray();
            Assert.IsNotEmpty(met,"fixture: "+barge.name+" has nothing solid on that side");
            for(float clear=2.4f;clear<=12;clear+=.6f){var at=met[0].point+side*clear;at.y=1;if(InTheWay(null,at,1.5f).Length==0)return at;}
            Assert.Fail("fixture: no clear water within twelve metres of "+barge.name+"'s hull");return default;
        }

        [UnityTest] public IEnumerator AFreedBargeDriftingOntoTheShipThatCutItShovesItAndChargesNothingUntilItRests()
        {
            // The last knot is cut from the side the barge then drifts to, so it comes through where the ship lies. Until November 20 each touch of that cost the pilot 12, as flying
            // into a wall does (three plays of barge three through the pad, and part of one defeat). A barge under way is on FlightMotor.UnderWay now. The first pass takes it off
            // again as it comes free, the game as it was, to show that this flight meets the hull and was charged for it; the second leaves the game alone.
            foreach(bool listed in new[]{false,true})
            {
                Flag("xylos.barge3",false);Flag("clear.Barge3_Knots",false);
                ToTheLane();yield return Zone("XylosNearspace");
                DialogueSystem.Current.AutoAdvance=true;
                var barge=Barge(3);Assert.IsFalse(barge.Free,"fixture: barge 3 is free before its knots are cut");
                CollectionAssert.DoesNotContain(FlightMotor.UnderWay,barge.transform,"a barge still held is counted as under way");
                var hull=barge.GetComponentsInChildren<Collider>(true).Select(c=>c.name).Distinct().ToArray();
                var ship=Lead;Put(ship,OffTheHull(barge,barge.drift[0]));yield return Frames(2);
                float cost=0;void Charged(Health who,DamagePacket packet,float damage){if(packet.source==null&&packet.type==DamageType.Kinetic)cost+=damage;}
                string[] Struck()=>log.Where(l=>l.StartsWith($"WALL_CONTACT {ship.name} ")).ToArray();
                bool OfTheHull(string line)=>hull.Any(n=>line.StartsWith($"WALL_CONTACT {ship.name} {n} at "));
                int before=Struck().Length;
                yield return Free(3);
                // Nothing else in the fight may be what costs the ship anything.
                foreach(var body in Health.All.Where(h=>h!=null&&!h.friendly&&h.Alive).ToArray())Kill(body);
                if(listed)CollectionAssert.Contains(FlightMotor.UnderWay,barge.transform,"barge 3 came free and isn't counted as under way");
                else FlightMotor.UnderWay.Remove(barge.transform);
                ship.Health.Damaged+=Charged;ship.Health.InvulnerableUntil=0;
                float deadline=Time.unscaledTime+barge.driftSeconds+4;
                while(Flat(barge.transform.position,Rested[2])>.3f&&Time.unscaledTime<deadline&&(listed||cost<=0))
                {
                    var to=barge.transform.position-ship.transform.position;to.y=0;to.Normalize();ship.motor.Move(new Vector2(to.x,to.z),false,false);yield return null;
                }
                ship.Health.InvulnerableUntil=GameTime.Now+900;
                var struck=Struck().Skip(before).ToArray();int touches=struck.Count(OfTheHull);
                Assert.IsEmpty(struck.Where(l=>!OfTheHull(l)).ToArray(),"fixture: pressed against the drifting barge the ship struck something else as well, and what it was charged can't be told apart");
                Assert.Greater(touches,0,"fixture: the barge drifted through where the ship lay and the ship never met its hull");
                if(!listed)
                {
                    // What the hull takes of a scrape's 12 is its own plating's business (9 of it, at this Sync): anything at all is the charge.
                    Assert.Greater(cost,1,$"fixture: a barge that isn't counted as under way charged the ship {cost:0.0} for {touches} touches: this flight shows nothing");
                    ship.Health.Damaged-=Charged;continue;
                }
                Assert.AreEqual(0,cost,.01f,$"barge 3 drifted onto the ship that cut its last knot and charged it {cost:0} over {touches} touches");
                // Come to rest it is a hull like any other: off the list, and flying into it costs what a wall costs.
                float rest=Time.unscaledTime+6;while(Flat(barge.transform.position,Rested[2])>.3f&&Time.unscaledTime<rest)yield return null;
                Assert.Less(Flat(barge.transform.position,Rested[2]),.3f,"fixture: barge 3 never came to rest");yield return new WaitForSecondsRealtime(.8f);
                CollectionAssert.DoesNotContain(FlightMotor.UnderWay,barge.transform,"barge 3 has come to rest and is still counted as under way");
                Put(ship,OffTheHull(barge,barge.transform.position+(Rested[2]-Snagged[2])));yield return Frames(2);
                cost=0;before=Struck().Length;ship.Health.InvulnerableUntil=0;
                float into=Time.unscaledTime+6;
                while(cost<=0&&Time.unscaledTime<into)
                {
                    var to=barge.transform.position-ship.transform.position;to.y=0;to.Normalize();ship.motor.Move(new Vector2(to.x,to.z),false,false);yield return null;
                }
                ship.Health.InvulnerableUntil=GameTime.Now+900;ship.Health.Damaged-=Charged;
                Assert.Greater(Struck().Skip(before).Count(OfTheHull),0,"fixture: flown at the barge where it rests the ship never met its hull");
                Assert.Greater(cost,1,"a barge at rest in the lane charges nothing for being flown into: it should be a wall again");
            }
            // A barge left in mid-drift (the zone left, a save loaded) leaves nothing on the list.
            yield return Free(2);CollectionAssert.Contains(FlightMotor.UnderWay,Barge(2).transform,"fixture: barge 2 came free and isn't counted as under way");
            yield return Zone("XylosNearspace");
            Assert.IsTrue(Barge(2).Free,"fixture: barge 2 isn't free after the zone loaded again");
            Assert.IsEmpty(FlightMotor.UnderWay,"a barge left in mid-drift is still counted as under way in the zone loaded after it");
        }

        [UnityTest] public IEnumerator FireFromBesideABargeReachesEveryKnotInSightAndTheHullIsStillCover()
        {
            // A barge is solid where it is drawn. One box over the whole model stood out past the pointed bow and stern, and fire at the bow knot from beside the hull's
            // middle stopped in clear space: ninety seconds of it and the knot untouched (first player run, November 13).
            ToTheLane();yield return Zone("XylosNearspace");
            for(int n=1;n<=3;n++)
            {
                var barge=Barge(n);var knots=Bodies("SnagvineNode",barge.transform.position,18);
                Assert.AreEqual(3,knots.Length,$"fixture: barge {n} shows three knots");
                Assert.IsFalse(barge.GetComponentsInChildren<Collider>().Any(c=>c is BoxCollider&&!c.isTrigger),$"barge {n} is a box again: it stands out past its pointed ends");
                bool Hull(RaycastHit hit)=>hit.collider.GetComponentInParent<SnaggedBarge>()==barge;
                // Where a ship fights the flank knot from: beside the hull's middle on the lane side, a hull's width outside the knot.
                var flank=knots.OrderBy(k=>Flat(k.transform.position,barge.transform.position)).First();
                var away=flank.transform.position-barge.transform.position;away.y=0;away.Normalize();
                var gun=flank.transform.position+away*2.5f;gun.y=1;
                foreach(var knot in knots)
                {
                    var aim=knot.transform.position;aim.y=1;var way=aim-gun;
                    var met=Physics.SphereCastAll(gun,.15f,way.normalized,way.magnitude,~0,QueryTriggerInteraction.Ignore).Where(Hull).ToArray();
                    Assert.IsEmpty(met,$"barge {n}: a shot from beside its middle at the knot at {aim} meets the hull {(met.Length>0?met[0].distance:0):0.0} m out of {way.magnitude:0.0}, in clear space");
                }
                // And it is still cover: a shot across the hull's middle is stopped.
                var across=barge.transform.position-away*6;across.y=1;var through=across-gun;
                Assert.IsTrue(Physics.SphereCastAll(gun,.15f,through.normalized,through.magnitude,~0,QueryTriggerInteraction.Ignore).Any(Hull),$"barge {n} doesn't stop a shot across its middle: it is no cover");
            }
        }

        static readonly (string id,(string enemy,int count)[] wakes)[] Swarms=
        {
            ("Lane_0",new[]{("Podskimmer",3)}),("Lane_1",new[]{("Podskimmer",5)}),("Lane_2",new[]{("Podskimmer",4),("ClaimDrone",1)}),("Nest_1",new[]{("Podskimmer",5)}),("Nest_2",new[]{("Podskimmer",5)}),
            ("Barge1_Skimmers",new[]{("Podskimmer",5)}),("Barge2_Skimmers",new[]{("Podskimmer",5)}),("Barge3_Skimmers",new[]{("Podskimmer",4)}),("Barge3_Claim",new[]{("ClaimDrone",3)}),
        };
        [UnityTest] public IEnumerator EveryFightOnTheLaneWakesAsTheShipFliesInAndClearsWhenItsLastBodyFalls()
        {
            ToTheLane();yield return Zone("XylosNearspace");
            var fights=All<EncounterVolume>().Where(e=>!e.encounterId.EndsWith("_Knots")).Select(e=>e.encounterId).ToArray();
            CollectionAssert.AreEquivalent(Swarms.Select(s=>s.id),fights,"the lane's fights aren't the map's three lane swarms, two nests, three barges' skimmers and the claim drones");
            foreach(var (id,wakes) in Swarms)
            {
                var fight=Fight(id);Assert.IsFalse(fight.Started,id+" is awake before anyone flies in");
                var before=new HashSet<EnemyBrain>(Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None));
                var centre=fight.transform.position;centre.y=1;Put(Lead,centre);
                float deadline=Time.unscaledTime+3;while(!fight.Started&&Time.unscaledTime<deadline)yield return null;
                Assert.IsTrue(fight.Started,$"the ship flew into {id} at {centre} and nothing woke");
                yield return Frames(2);
                var woke=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>!before.Contains(e)&&e.Health.Alive).ToArray();
                foreach(var (enemy,count) in wakes)Assert.AreEqual(count,woke.Count(e=>e.definition.id==enemy),$"{id} woke the wrong number of {enemy}: {string.Join(", ",woke.Select(e=>e.definition.id))}");
                Assert.AreEqual(wakes.Sum(w=>w.count),fight.Remaining,id+" counts a different number of bodies from what woke");
                foreach(var body in woke)Assert.AreEqual(1,body.transform.position.y,.75f,$"{id}: a {body.definition.id} woke off the flight plane, at {body.transform.position}");
                foreach(var body in woke){body.Passive=true;Kill(body.Health);}
                deadline=Time.unscaledTime+3;while(!Has("clear."+id)&&Time.unscaledTime<deadline)yield return null;
                Assert.IsTrue(Has("clear."+id),id+" was emptied and isn't cleared");Assert.AreEqual(0,fight.Remaining,id+" still counts bodies");
                yield return new WaitForSecondsRealtime(.3f);
            }
        }

        [UnityTest] public IEnumerator EveryCacheOnTheLaneAndInTownPaysOnceAndStaysOpened()
        {
            ToTheLane();State.consumables["RepairGel"]=0;
            yield return Zone("XylosNearspace");
            Assert.Less(Flat(Cache("xylos.nest1").transform.position,Beside(612,-46)),2,"nest A's hoard isn't at nest A");Assert.Less(Flat(Cache("xylos.nest2").transform.position,Beside(1236,45)),2,"nest B's hoard isn't at nest B");
            yield return PaysOnce("xylos.nest1",120,0,("AmberSap",3),("Rootfibre",2));
            yield return PaysOnce("xylos.nest2",90,1,("AmberSap",2));
            yield return Zone("XylosNearspace");
            foreach(string id in new[]{"xylos.nest1","xylos.nest2"}){Assert.IsFalse(Cache(id).gameObject.activeInHierarchy,id+" is back after the zone loads again");Assert.IsFalse(Cache(id).Available,id+" can be opened again");}
            Flag("xylos.arrivalSeen");Flag("xylos.ropeHauled");yield return Zone("Rootmarket");
            Assert.Less(Flat(Cache("xylos.walkway").transform.position,new Vector2(0,102.3f)),1.5f,"the growers' cache isn't at the top of the upper walk");
            Assert.Less(Flat(Cache("xylos.sennIsland").transform.position,new Vector2(-51.2f,11)),1.5f,"the old mooring box isn't on Senn's island");
            yield return PaysOnce("xylos.walkway",140,1,("AmberSap",3),("Rootfibre",3));
            yield return PaysOnce("xylos.sennIsland",80,0,("Rootfibre",2),("RepairGel",1));
            yield return Zone("Rootmarket");
            foreach(string id in new[]{"xylos.walkway","xylos.sennIsland"}){Assert.IsFalse(Cache(id).gameObject.activeInHierarchy,id+" is back after the zone loads again");Assert.IsFalse(Cache(id).Available,id+" can be opened again");}
        }
        static IEnumerator PaysOnce(string id,int scrip,int parts,params (string item,int count)[] contents)
        {
            var cache=Cache(id);Assert.IsNotNull(cache,"there is no cache "+id);Assert.IsTrue(cache.Available,id+" can't be opened");
            int money=State.scrip,owned=State.parts.Count;var held=contents.Select(c=>Held(c.item)).ToArray();
            cache.Interact();yield return Frames(2);
            Assert.IsTrue(Has("cache."+id),id+" opened and isn't marked opened");
            Assert.AreEqual(money+scrip,State.scrip,id+" doesn't pay its scrip");Assert.AreEqual(owned+parts,State.parts.Count,id+" doesn't hold the parts it promises");
            for(int i=0;i<contents.Length;i++)Assert.AreEqual(held[i]+contents[i].count,Held(contents[i].item),$"{id} doesn't give {contents[i].count} {contents[i].item}");
            money=State.scrip;owned=State.parts.Count;held=contents.Select(c=>Held(c.item)).ToArray();
            cache.Interact();yield return Frames(2);
            Assert.AreEqual(money,State.scrip,id+" paid twice");Assert.AreEqual(owned,State.parts.Count,id+" gave its parts twice");
            for(int i=0;i<contents.Length;i++)Assert.AreEqual(held[i],Held(contents[i].item),id+" gave "+contents[i].item+" twice");
        }

        [UnityTest] public IEnumerator TheDeltaBeaconStaysShutUntilTheThirdBargeAndThenLandsAtRootmarketAndThePadComesBack()
        {
            ToTheLane();yield return Zone("XylosNearspace");
            var land=All<WarpBeacon>().SingleOrDefault(w=>w.scene=="Rootmarket");Assert.IsNotNull(land,"Nearspace has no way down to Rootmarket");
            Assert.Less(Flat(land.transform.position,DeltaBeacon),2,"the way down isn't at the delta beacon, at the lane's east end");
            Assert.AreEqual("Arrival",land.spawn,"the delta beacon lands somewhere other than Rootmarket's arrival");
            string open=land.prompt;
            foreach(var freed in new[]{new string[0],new[]{"xylos.barge1"},new[]{"xylos.barge2"}})
            {
                foreach(string f in freed)Flag(f);yield return Frames(3);
                Assert.IsFalse(Has("xylos.bargesFree"),"the delta beacon is open with fewer than three barges free");
                land.Interact();yield return Frames(2);
                Assert.IsFalse(SceneFlow.Current.Loading,$"the delta beacon landed the party with {freed.Length} more barges free");
                StringAssert.Contains("barges",land.prompt,"a refused landing doesn't say the barges are what it waits on");
            }
            Flag("xylos.barge3");RpgServices.Quests.Report(ObjectiveKind.Interact,"SeedBarge",3);yield return Frames(4);
            Assert.IsTrue(Has("xylos.bargesFree"),"three barges free and the delta beacon isn't told");
            Assert.AreEqual(open,land.prompt,"the delta beacon opened and still says why it's shut");
            Assert.IsTrue(land.Available,"the delta beacon is open and can't be used");
            land.Interact();yield return Arrive("Rootmarket");
            var arrival=All<SpawnPoint>().Single(s=>s.id=="Arrival");
            Assert.Less(Flat(Lead.transform.position,arrival.transform.position),4,"the landing didn't put the party at Rootmarket's arrival");
            Assert.Less(Flat(arrival.transform.position,TownArrival),1.5f,"Rootmarket's arrival isn't on the landing, where the map has it");
            Assert.IsFalse(Lead.flight,"the party landed in Rootmarket still flying");
            Assert.AreEqual(4,Step("SeedRoad"),"landing at Rootmarket didn't move the story on to the rope and Ruva's terms");
            Assert.IsTrue(Has("xylos.landed"),"Rootmarket doesn't know the party has landed");
            // And back up: the landing pad lifts to the lane's far end, short of the beacon.
            var pad=All<DockingPad>().SingleOrDefault(d=>d.scene=="XylosNearspace");Assert.IsNotNull(pad,"Rootmarket has no pad back up to the lane");
            Assert.AreEqual("Rootmarket",pad.spawn);Assert.IsTrue(pad.Available,"the landing pad can't be used");
            pad.Interact();yield return Arrive("XylosNearspace",40);
            var from=All<SpawnPoint>().Single(s=>s.id=="Rootmarket");
            Assert.Less(Flat(from.transform.position,FromTown),1.5f,"Nearspace's spawn from Rootmarket isn't where the map has it");
            Assert.Less(Flat(Lead.transform.position,from.transform.position),4,"the pad didn't bring the party up at the spawn from Rootmarket");
            Assert.Less(Flat(Lead.transform.position,DeltaBeacon),30,"lifting off from Rootmarket arrives far from the delta beacon");
            Assert.IsTrue(Lead.flight,"the party left Rootmarket and isn't flying");
        }

        [UnityTest] public IEnumerator TheLaneLimitsHoldTheShipInsideTheLaneAllTheWayAlong()
        {
            ToTheLane();yield return Zone("XylosNearspace");var ship=Lead;var escapes=new List<string>();
            // Pushed hard at the side, boosting, then rolled into it: down every leg and at each bend, both sides, where no fight's trigger, barge or pane stands between the ship and the limit.
            foreach(var (s,side) in new[]{(60f,1),(60f,-1),(242f,-1),(400f,1),(400f,-1),(539f,1),(539f,-1),(800f,1),(800f,-1),(867f,1),(1100f,1),(1100f,-1),(1156f,1),(1156f,-1),(1320f,1),(1320f,-1),(1432f,1),(1560f,1),(1560f,-1)})
            {
                Along(s,out var h);var outward=new Vector3(h.y,0,-h.x)*side;string where=$"{s:0} m along, to {(side>0?"starboard":"port")}";
                Put(ship,Beside(s,side*(LaneHalf-14)));yield return null;
                for(float end=Time.unscaledTime+3;Time.unscaledTime<end;){ship.motor.Move(new Vector2(outward.x,outward.z),true,false);yield return null;}
                float off=FromLane(ship.transform.position);
                if(off>LaneHalf+1)escapes.Add($"{where}: boosting outward took the ship {off:0.0} m from the lane's middle, past its {LaneHalf} m limit");
                if(off<LaneHalf-14+2)escapes.Add($"{where}: the ship stopped {off:0.0} m out, short of the lane's limit: something else holds it");
                // A roll is carried out only by a motor that is being driven.
                ship.motor.Dash(outward,8);for(int i=0;i<20;i++){ship.motor.Move(Vector2.zero,false,false);yield return null;}
                off=FromLane(ship.transform.position);if(off>LaneHalf+1)escapes.Add($"{where}: a roll into the limit took the ship {off:0.0} m out, through it");
            }
            // The ends: astern of the arrival beacon and on past the delta beacon.
            var first=(Lane[1]-Lane[0]).normalized;var last=(Lane[6]-Lane[5]).normalized;
            Put(ship,Beside(20,0));yield return null;
            for(float end=Time.unscaledTime+4;Time.unscaledTime<end;){ship.motor.Move(-first,true,false);yield return null;}
            float astern=-Vector2.Dot(new Vector2(ship.transform.position.x,ship.transform.position.z)-Lane[0],first);
            if(astern>LaneHalf+1)escapes.Add($"astern of the arrival: the ship got {astern:0.0} m behind the lane's start");
            Put(ship,Beside(LaneLength-30,14));yield return null;
            for(float end=Time.unscaledTime+4;Time.unscaledTime<end;){ship.motor.Move(last,true,false);yield return null;}
            float beyond=Vector2.Dot(new Vector2(ship.transform.position.x,ship.transform.position.z)-Lane[6],last);
            if(beyond>LaneHalf+1)escapes.Add($"past the delta beacon: the ship got {beyond:0.0} m beyond the lane's end");
            Assert.IsEmpty(escapes,"the ship leaves the lane:\n"+string.Join("\n",escapes));
        }

        [UnityTest] public IEnumerator TheWarpHomeLandsAboveTallowAndTallowsWarpBringsThePartyBack()
        {
            ToTheLane();yield return Zone("XylosNearspace");
            Assert.Less(Flat(All<SpawnPoint>().Single(s=>s.id=="Arrival").transform.position,LaneArrival),1.5f,"Nearspace's arrival isn't at the lane's west end");
            var home=All<WarpBeacon>().SingleOrDefault(w=>w.scene=="TallowApproach");Assert.IsNotNull(home,"Nearspace has no warp home");
            Assert.Less(Flat(home.transform.position,HomeWarp),2,"the warp home isn't beside the arrival, where the map has it");
            Assert.IsTrue(string.IsNullOrEmpty(home.requiredFlag),"the warp home is locked: nobody stands here without the lane");
            Assert.IsTrue(home.Available,"the warp home can't be used at the arrival");
            home.Interact();yield return Arrive("TallowApproach");
            var xylos=All<SpawnPoint>().SingleOrDefault(s=>s.id=="Xylos");Assert.IsNotNull(xylos,"Tallow's approach has no spawn for a party coming home from Xylos");
            Assert.Less(Flat(Lead.transform.position,xylos.transform.position),4,"the warp home didn't land at Tallow's spawn for Xylos");
            Assert.IsTrue(Lead.flight,"home from Xylos and not flying");
            var away=All<WarpBeacon>().SingleOrDefault(w=>w.scene=="XylosNearspace");Assert.IsNotNull(away,"Tallow's approach has no warp to Xylos");
            Assert.AreEqual("xylos.charted",away.requiredFlag,"Tallow's warp to Xylos doesn't wait for Pell's lantern to learn the lane");
            Assert.Less(Flat(away.transform.position,xylos.transform.position),12,"the warp to Xylos is far from where a party coming home from Xylos arrives");
            away.Interact();yield return Arrive("XylosNearspace");
            Assert.Less(Flat(Lead.transform.position,LaneArrival),4,"Tallow's warp didn't land at the lane's arrival");
        }

        [UnityTest] public IEnumerator SavingOnTheLaneAndContinuingKeepsTheFreedBargeTheClearedSwarmAndTheEmptyHoard()
        {
            ToTheLane();yield return Zone("XylosNearspace");
            yield return Free(1);
            var swarm=Fight("Lane_0");var centre=swarm.transform.position;centre.y=1;Put(Lead,centre);
            float deadline=Time.unscaledTime+3;while(!swarm.Started&&Time.unscaledTime<deadline)yield return null;
            foreach(var body in Health.All.Where(h=>h!=null&&!h.friendly&&h.Alive&&h.id=="Podskimmer").ToArray())Kill(body);
            deadline=Time.unscaledTime+3;while(!Has("clear.Lane_0")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(Has("clear.Lane_0"),"fixture: the first swarm cleared");
            Cache("xylos.nest1").Interact();yield return Frames(2);Assert.IsTrue(Has("cache.xylos.nest1"),"fixture: nest A's hoard opened");
            int scrip=State.scrip;
            yield return SaveQuitAndContinue();
            Assert.Less(Flat(Lead.transform.position,LaneArrival),4,"Continue didn't bring the party back to the lane where it was saved");
            Assert.IsTrue(Lead.flight,"Continue on the lane lands the party on foot");
            foreach(string f in new[]{"xylos.barge1","clear.Barge1_Knots","clear.Lane_0","cache.xylos.nest1","xylos.charted"})Assert.IsTrue(Has(f),f+" was lost through a save and Continue");
            Assert.IsFalse(Has("xylos.barge2"),"a barge nobody touched came back free");
            Assert.AreEqual(scrip,State.scrip,"scrip changed through a save and Continue");
            Assert.AreEqual(2,Step("SeedRoad"),"the story lost its place through a save");
            Assert.AreEqual(1,State.questCounts.TryGetValue("SeedRoad/2",out int counted)?counted:0,"the story lost its count of freed barges through a save");
            var barge=Barge(1);yield return Frames(2);
            Assert.IsTrue(barge.Free,"barge one came back snagged");
            Assert.Less(Flat(barge.transform.position,Rested[0]),.5f,"barge one isn't already at rest in the lane after Continue: it drifts again, or lies where it was snagged");
            Assert.IsFalse(barge.cables.Any(c=>c.activeSelf)||barge.whileSnagged.activeSelf,"barge one came back drawn in its weed and cables");
            Assert.AreEqual(0,Bodies("SnagvineNode",Snagged[0],18).Length,"barge one's knots are back");
            Assert.AreEqual(3,Bodies("SnagvineNode",Snagged[1],18).Length,"barge two's knots aren't on show after Continue");
            var before=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Length;Put(Lead,centre);yield return new WaitForSecondsRealtime(1.2f);
            Assert.AreEqual(before,Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Length,"the cleared swarm woke again after Continue");
            Assert.IsFalse(Cache("xylos.nest1").gameObject.activeInHierarchy,"nest A's hoard is back after Continue");
        }

        // ================================================================== Rootmarket
        [UnityTest] public IEnumerator TheArrivalTalkHandsTarenTheRopeThreePullsSeatTheIslandAndOnlyThenRuvaTalksTerms()
        {
            Landed();yield return Zone("Rootmarket");
            Assert.AreEqual(4,Step("SeedRoad"),"fixture: landing puts the story at the rope");
            var rope=All<MooringHaul>().Single();var ruva=Here("Ruva");var binna=Here("Binna");
            Assert.Less(Flat(rope.transform.position,new Vector2(32.4f,18.8f)),1,"the rope isn't at the mooring post");
            Assert.Less(Flat(ruva.transform.position,RuvaMooring),1,"Ruva isn't at the mooring when the party lands");
            // Before the arrival talk the rope isn't anyone's to take, and Ruva and Binna have only the rope to talk about.
            Assert.IsFalse(rope.Available,"the rope is offered before Ruva has handed it over");
            rope.Interact();yield return new WaitForSecondsRealtime(.5f);Assert.AreEqual(0,rope.Progress,.001f,"the rope pulled before the arrival talk");
            yield return Talk(ruva);Assert.AreEqual("RuvaRopeHold",heard.LastOrDefault(),"Ruva says something other than her line about the rope while the island is out");
            Assert.IsFalse(Has("met.Ruva"),"a line about the rope counted as meeting Ruva, so her terms would never be heard as a first meeting");
            yield return Talk(binna);Assert.AreEqual("BinnaRopeHold",heard.LastOrDefault(),"Binna says something other than her line about the rope while her garden is out");
            Assert.IsFalse(Has("met.Binna"),"Binna's rope line counted as meeting her");
            // The river gate is shut until the terms.
            var gate=All<WarpBeacon>().Single(w=>w.scene=="BraidedRiver");Assert.Less(Flat(gate.transform.position,RiverGate),1.5f,"the river gate isn't east past the press");
            gate.Interact();yield return Frames(2);Assert.IsFalse(SceneFlow.Current.Loading,"the river gate opened before Ruva's terms");
            StringAssert.Contains("Ruva",gate.prompt,"the shut river gate doesn't say Ruva is what it waits on");
            // The crossing between the pad and the mooring: the arrival talk, once.
            var crossing=Point("xylos.arrivalSeen");Assert.AreEqual("RuvaArrival",crossing.dialogueNode);Assert.IsTrue(crossing.startsOnCrossing,"the arrival talk can be walked round");
            Assert.Less(Flat(crossing.transform.position,Crossing),1.5f,"the arrival talk's line isn't between the pad and the mooring");
            // Close enough to Ruva that the talk's frame holds her and the party in its middle: where the way from the pad first meets the line, about seven metres from her.
            // The line at x 14 opened the talk 16.6 m from her, and she spoke from the right edge of the frame (s18 010, November 22).
            var box=crossing.GetComponent<BoxCollider>().bounds;float meets=box.min.x,along=Mathf.Lerp(26.5f,21,(meets-20)/8.5f);
            Assert.Greater(Vector2.Distance(new Vector2(12.5f,25.5f),RuvaMooring),9,"fixture: the check can't see the line where it was until November 22");
            Assert.LessOrEqual(Vector2.Distance(new Vector2(meets,along),RuvaMooring),8,"the arrival talk opens "+Vector2.Distance(new Vector2(meets,along),RuvaMooring).ToString("0.0")+" m from Ruva");
            DialogueSystem.Current.AutoAdvance=true;Put(Lead,Floor(new Vector2(23.4f,25)));float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            yield return Settle();
            Assert.AreEqual(1,heard.Count(h=>h=="RuvaArrival"),"walking from the pad to the mooring doesn't start the arrival talk: heard "+string.Join(", ",heard));
            Assert.IsTrue(Has("xylos.arrivalSeen"));
            Put(Lead,Floor(new Vector2(17,26)));yield return new WaitForSecondsRealtime(.4f);Put(Lead,Floor(new Vector2(23.4f,25)));yield return new WaitForSecondsRealtime(1);
            Assert.AreEqual(1,heard.Count(h=>h=="RuvaArrival"),"the arrival talk played again on the way back across");
            // Three pulls. Two don't seat her; the third does, once.
            Assert.IsTrue(rope.Available,"the arrival talk ended with the rope in Taren's hands and the rope isn't offered");Assert.AreEqual(3,rope.pulls,"the rope takes other than three pulls");
            for(int pull=1;pull<=2;pull++)
            {
                rope.Interact();deadline=Time.unscaledTime+5;while(rope.Progress<pull/3f-.01f&&Time.unscaledTime<deadline)yield return null;
                yield return new WaitForSecondsRealtime(.4f);
                Assert.IsFalse(Has("xylos.ropeHauled"),$"the island seated after {pull} pulls");
                Assert.AreEqual(pull/3f,rope.Progress,.02f,$"pull {pull} didn't bring the island a third of the way in");
                Assert.IsTrue(rope.Available,$"pull {pull + 1} isn't offered once the island has come in");
                Assert.IsNotNull(ruva.holdNode,"Ruva lost her rope line before the island seated");
            }
            rope.Interact();deadline=Time.unscaledTime+5;while(!Has("xylos.ropeHauled")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(Has("xylos.ropeHauled"),"three pulls didn't seat the island");Assert.AreEqual(1,log.Count(l=>l=="ROPE_HAULED xylos.ropeHauled"));
            yield return Frames(2);
            Assert.IsFalse(rope.Available,"the rope is still offered with the island seated");
            rope.Interact();yield return new WaitForSecondsRealtime(.5f);Assert.AreEqual(1,log.Count(l=>l.StartsWith("ROPE_HAULED")),"a fourth pull hauled again");
            Assert.Less(Flat(rope.island.position,IslandMoored),.4f,$"the island seated at {rope.island.position}, not at its mooring");
            var saved=GameServices.Current.Saves.Load("autosave");Assert.IsTrue(saved!=null&&saved.flags.TryGetValue("xylos.ropeHauled",out bool kept)&&kept,"seating the island wasn't saved");
            Assert.IsNull(ruva.holdNode,"Ruva still answers with her rope line once the island is in");Assert.IsNull(binna.holdNode,"Binna still answers with her rope line once the island is in");
            // Now the terms, as Ruva's first conversation, and they open the river.
            yield return Talk(ruva);Assert.AreEqual("RuvaTerms",heard.LastOrDefault(),"Ruva's first conversation once the island is in isn't her terms");
            Assert.IsTrue(Has("xylos.termsHeard"));Assert.AreEqual(5,Step("SeedRoad"),"Ruva's terms didn't move the story on to the river");
            yield return Talk(ruva);Assert.AreEqual("RuvaRepeat",heard.LastOrDefault(),"Ruva says her terms twice");
            gate.Interact();yield return Arrive("BraidedRiver");
            Assert.Less(Flat(Lead.transform.position,All<SpawnPoint>().Single(s=>s.id=="Arrival").transform.position),4,"the river gate didn't land at the Braided River's arrival");
        }

        [UnityTest] public IEnumerator TheNoticeReadsAnyTimeAndTheSeedHouseOpensOnlyOnceRuvaHasSaidWhatSheWontSell()
        {
            Landed();Flag("xylos.arrivalSeen");Flag("xylos.ropeHauled");yield return Zone("Rootmarket");
            var notice=Point("xylos.noticeRead");var door=Point("xylos.seedHouseSeen");
            Assert.AreEqual("Read the Compact notice",notice.prompt);Assert.AreEqual("Look into the seed house",door.prompt);
            Assert.Less(Flat(notice.transform.position,Notice),1,"the Compact notice isn't on the seed house's door");Assert.Less(Flat(door.transform.position,Door),1,"the seed house's doorway isn't at its door");
            Assert.Greater(Flat(notice.transform.position,door.transform.position),notice.range,"the notice and the doorway share a spot, so one press can't choose between them");
            Assert.IsFalse(door.Available,"the seed house can be looked into before Ruva has said what she won't sell");
            door.Interact();yield return Frames(3);Assert.IsFalse(heard.Contains("SelaSeedHouse"),"the seed house's look played before the terms");
            yield return Discover(notice);
            Assert.AreEqual("SelaCompactNotice",heard.LastOrDefault());Assert.IsTrue(Has("xylos.noticeRead"));Assert.IsFalse(notice.Available,"the notice is offered again once read");
            Flag("xylos.termsHeard");yield return Frames(2);
            yield return Discover(door);
            Assert.AreEqual("SelaSeedHouse",heard.LastOrDefault());Assert.IsTrue(Has("xylos.seedHouseSeen"));Assert.IsFalse(door.Available,"the seed house is offered again once seen");
        }

        [UnityTest] public IEnumerator RuvaJaskAndTeoStandWhereTheStoryPutsThemAndNobodyIsInTwoPlaces()
        {
            Landed();Flag("xylos.arrivalSeen");Flag("xylos.ropeHauled");
            // Each row adds its flags to the ones before, as the chapter does, and loads the town again.
            var story=new (string[] adds,Vector2? ruva,Vector2? jask,Vector2? teo)[]
            {
                (new string[0],RuvaMooring,JaskFerry,null),
                (new[]{"xylos.termsHeard"},RuvaMooring,JaskFerry,TeoPool),
                (new[]{"xylos.archSeen"},RuvaSeedHouse,null,TeoPool),
                (new[]{"bossdown.Seedthane"},RuvaSeedHouse,null,null),
                (new[]{"xylos.seedthaneGone","xylos.floodcrownGone","xylos.voteHeard"},RuvaSeedHouse,null,TeoPool),
            };
            foreach(var (adds,ruva,jask,teo) in story)
            {
                foreach(string f in adds)Flag(f);string when=adds.Length==0?"on landing":"once "+string.Join(", ",adds)+" is set";
                yield return Zone("Rootmarket");
                foreach(var (who,at) in new[]{("Ruva",ruva),("Jask",jask),("Teo",teo)})
                {
                    var npc=Here(who);
                    if(at==null){Assert.IsNull(npc,$"{who} stands in Rootmarket {when}, at {npc?.transform.position}");continue;}
                    Assert.IsNotNull(npc,$"{who} isn't in Rootmarket {when}");
                    Assert.Less(Flat(npc.transform.position,at.Value),1,$"{who} stands at {npc.transform.position} {when}, not at ({at.Value.x}, {at.Value.y})");
                }
                foreach(var (who,at) in new[]{("Binna",BinnaKitchen),("Hadda",HaddaPress),("Senn",SennIsland)})
                {var npc=Here(who);Assert.IsNotNull(npc,$"{who} isn't in Rootmarket {when}");Assert.Less(Flat(npc.transform.position,at),1,$"{who} isn't at home {when}");}
            }
            // Teo comes down the moment the terms are heard, out of sight of the mooring; Ruva doesn't walk off mid-scene.
            foreach(string f in new[]{"xylos.termsHeard","xylos.archSeen","bossdown.Seedthane","xylos.seedthaneGone","xylos.floodcrownGone","xylos.voteHeard"})Flag(f,false);
            yield return Zone("Rootmarket");
            Assert.IsNull(Here("Teo"),"fixture: Teo isn't in town before the terms");
            Flag("xylos.termsHeard");yield return Frames(3);
            Assert.IsNotNull(Here("Teo"),"the terms heard in town and Teo doesn't come down to the pool");
            Flag("xylos.archSeen");yield return Frames(3);
            Assert.Less(Flat(Here("Ruva").transform.position,RuvaMooring),1,"Ruva vanished from the mooring in front of the party: her place is decided as the town loads");
        }

        [UnityTest] public IEnumerator BinnaAndHaddaEachOpenTheirOwnStallAndHaddasPressAndCradleWork()
        {
            Landed();Flag("xylos.arrivalSeen");Flag("xylos.ropeHauled");yield return Zone("Rootmarket");
            var shop=Object.FindFirstObjectByType<ShopUi>();
            // Binna's first meeting: her first choice is her stall.
            yield return Talk(Here("Binna"));Assert.AreEqual("BinnaFirst",heard.FirstOrDefault(),"Binna's first conversation isn't her first meeting");
            yield return Frames(2);
            Assert.IsTrue(shop.IsOpen,"Binna's 'What are you selling?' didn't open her stall");
            StringAssert.Contains("BINNA'S KITCHEN",Heading(shop),"Binna opened someone else's stall");
            var kitchen=GameCatalog.Find<ShopInventory>("Rootmarket_Binna");Assert.IsNotNull(kitchen,"Binna has no stall of her own");
            var offered=ShopUi.Stock(kitchen,State).Select(o=>o.id).ToArray();Assert.Greater(offered.Length,0,"Binna's counter is empty");
            Assert.IsTrue(offered.All(id=>GameCatalog.Find<ConsumableDef>(id)!=null),"Binna sells something other than what can be eaten or drunk: "+string.Join(", ",offered));
            CollectionAssert.DoesNotContain(offered,"PodStew","the food Binna's baskets make is on her counter before the baskets are back");
            shop.Close();yield return Frames(2);
            Flag("quest.ThreeBaskets.complete");CollectionAssert.Contains(ShopUi.Stock(kitchen,State).Select(o=>o.id).ToArray(),"PodStew","the baskets are back and Binna's stew isn't on the counter");
            // Hadda's three: her parts, the press, the cradle.
            var hadda=Here("Hadda");
            yield return Choose(hadda,"parts");
            Assert.IsTrue(shop.IsOpen,"Hadda's 'See the parts' didn't open her stall");StringAssert.Contains("MENDER'S PRESS",Heading(shop),"Hadda opened someone else's stall");
            var press=GameCatalog.Find<ShopInventory>("Rootmarket_Hadda");Assert.IsNotNull(press,"Hadda has no stall of her own");
            Assert.Greater(ShopUi.Parts(press).Count(),0,"Hadda's stall has no parts");
            shop.Close();yield return Frames(2);
            yield return Choose(hadda,"press");
            var menu=Object.FindFirstObjectByType<PauseMenu>();Assert.IsTrue(menu.IsOpen,"Hadda's 'Use the press' didn't open the press");menu.Close();yield return Frames(2);
            var bench=All<Bench>().SingleOrDefault();Assert.IsNotNull(bench,"Rootmarket has no press to fabricate at");Assert.AreEqual("Fabricate — The Press",bench.prompt);
            Assert.IsTrue(bench.Available,"the press can't be used");
            var cradle=All<RepairBay>().SingleOrDefault();Assert.IsNotNull(cradle,"Rootmarket has no mender's cradle");
            StringAssert.StartsWith("Mender's cradle — repair and save",cradle.prompt);
            var spawn=All<SpawnPoint>().SingleOrDefault(s=>s.id==cradle.saveSpawn);Assert.IsNotNull(spawn,$"the cradle saves at the spawn '{cradle.saveSpawn}', which Rootmarket hasn't got");
            Assert.Less(Flat(spawn.transform.position,cradle.transform.position),10,"the cradle's save puts the party far from the cradle");
            cradle.Interact();yield return Frames(2);
            Assert.AreEqual(cradle.saveSpawn,State.spawn,"saving at the cradle didn't move the party's place to it");
            var saved=GameServices.Current.Saves.Load("autosave");Assert.IsNotNull(saved);Assert.AreEqual("Rootmarket",saved.zone);Assert.AreEqual(cradle.saveSpawn,saved.spawn,"the cradle didn't save");
        }

        [UnityTest] public IEnumerator ArrivingChartsRootmarketAndTheLaneIsMarkedAtItsStop()
        {
            Landed();Assert.IsFalse(Has("visited.Rootmarket"),"fixture: Rootmarket is charted before anyone stands there");
            var stop=RouteChart.Find("Rootmarket");Assert.IsNotNull(stop,"Rootmarket has no stop on Sela's chart");
            Assert.AreEqual(RouteChart.Standing.Uncharted,RouteChart.Of(stop,State),"Rootmarket is on the chart before the party has stood there");
            yield return Zone("Rootmarket");
            Assert.IsTrue(Has(stop.UnlockFlag),"standing in Rootmarket didn't chart it");Assert.AreEqual(RouteChart.Standing.Here,RouteChart.Of(stop,State));
            Assert.IsTrue(RouteChart.Available,"the chart isn't in the menu with the lane to Xylos charted");
            Assert.IsNotNull(All<SpawnPoint>().SingleOrDefault(s=>s.id==stop.spawn),"the chart arrives at a spawn Rootmarket hasn't got");
            yield return Zone("XylosNearspace","Rootmarket");
            Assert.AreEqual(RouteChart.Standing.Open,RouteChart.Of(stop,State),"Rootmarket isn't open on the chart from the lane");
            Assert.AreEqual("Rootmarket",RouteChart.At("XylosNearspace")?.id,"the party's mark on the lane isn't at Rootmarket");
            State.questSteps["SeedRoad"]=2;Assert.AreEqual("Rootmarket",RouteChart.Objective(State)?.id,"the barges' step isn't marked at Rootmarket, the stop the lane is reached from");
            State.questSteps["SeedRoad"]=4;Assert.AreEqual("Rootmarket",RouteChart.Objective(State)?.id,"the terms' step isn't marked at Rootmarket");
            Assert.IsNull(RouteChart.Refusal(stop),"travel to Rootmarket from a quiet lane is refused: "+RouteChart.Refusal(stop));
            Assert.IsTrue(RouteChart.Travel(stop));yield return Arrive("Rootmarket");
            Assert.Less(Flat(Lead.transform.position,All<SpawnPoint>().Single(s=>s.id==stop.spawn).transform.position),4,"travel didn't arrive at the stop's spawn");
        }

        [UnityTest] public IEnumerator SavingInRootmarketAndContinuingKeepsTheSeatedIslandTheTermsAndThePlaceSavedAt()
        {
            Landed();Flag("xylos.arrivalSeen");yield return Zone("Rootmarket");
            yield return Haul(All<MooringHaul>().Single());Assert.IsTrue(Has("xylos.ropeHauled"),"fixture: three pulls seat the island");
            yield return Talk(Here("Ruva"));Assert.IsTrue(Has("xylos.termsHeard"),"fixture: Ruva's terms");
            yield return Discover(Point("xylos.noticeRead"));
            Cache("xylos.walkway").Interact();yield return Frames(2);
            All<RepairBay>().Single().Interact();yield return Frames(2);
            int scrip=State.scrip;
            yield return SaveQuitAndContinue();
            var press=All<SpawnPoint>().Single(s=>s.id=="Press");
            Assert.Less(Flat(press.transform.position,PressSpawn),1.5f,"the cradle's spawn isn't beside the press");
            Assert.Less(Flat(Lead.transform.position,press.transform.position),4,"Continue didn't bring the party back to the cradle it saved at");
            Assert.IsFalse(Lead.flight,"Continue in Rootmarket lands the party flying");
            foreach(string f in new[]{"xylos.arrivalSeen","xylos.ropeHauled","xylos.termsHeard","xylos.noticeRead","cache.xylos.walkway","visited.Rootmarket","met.Ruva"})Assert.IsTrue(Has(f),f+" was lost through a save and Continue");
            Assert.AreEqual(5,Step("SeedRoad"),"the story lost its place through a save");Assert.AreEqual(scrip,State.scrip);
            yield return Frames(2);
            var rope=All<MooringHaul>().Single();Assert.AreEqual(1,rope.Progress,.001f,"the island came back adrift");Assert.IsFalse(rope.Available,"the rope is offered again after Continue");
            Assert.Less(Flat(rope.island.position,IslandMoored),.4f,"the island isn't at its mooring after Continue");
            Assert.IsNull(Here("Ruva").holdNode,"Ruva answers with her rope line after Continue");
            Assert.IsNotNull(Here("Teo"),"Teo isn't at the pool after Continue with the terms heard");
            Assert.IsFalse(Point("xylos.noticeRead").Available,"the read notice is offered again after Continue");
            Assert.IsFalse(Cache("xylos.walkway").gameObject.activeInHierarchy,"the opened cache is back after Continue");
            Put(Lead,Floor(new Vector2(Crossing.x-.1f,25)));yield return new WaitForSecondsRealtime(1);Assert.IsFalse(DialogueSystem.Current.Running,"the arrival talk played again after Continue");
        }

        // ================================================================== the Braided River, the Walking Canopy and the vote (wiring only: the water, the anchors' doors and
        // the fights themselves are XylosMechanicsTests' and XylosCombatTests')
        static readonly Vector2 RiverWheelA=new(172.4f,114.5f),RiverWheelB=new(323.5f,102),GaroAt=new(35,91.6f),DabbAt=new(212,100.8f),TeoSeedfall=new(355,101),JaskStage=new(103.2f,94.4f);
        /// <summary>The story at this step, its objective line its step's words, and the arrow on a real place within reach of what the step names.</summary>
        static IEnumerator Points(int step,Vector3 near,string what,float within=2,List<string> misses=null)
        {
            yield return new WaitForSecondsRealtime(.6f);var quest=GameCatalog.Find<QuestDef>("SeedRoad");
            Assert.AreEqual(step,Step("SeedRoad"),$"the story isn't at step {step} ({quest.stepText[step]})");
            StringAssert.StartsWith(quest.stepText[step],Objective(),$"at step {step} the objective doesn't say its step's words");
            var goal=Goal();string miss=!goal.HasValue?$"at step {step} ({quest.stepText[step]}) the objective points nowhere: it should point at {what}"
                :Flat(goal.Value,near)>=within?$"at step {step} the objective points at {goal.Value}, not at {what} ({near})":null;
            // Gathered where a test asks, so one step's missing arrow doesn't hide whether everything after it is wired; the test still fails on it at its end.
            if(miss!=null){if(misses!=null)misses.Add(miss);else Assert.Fail(miss);}
        }
        /// <summary>Opens a cache whose contents this doesn't list: it must pay something the first time and nothing the second.</summary>
        static IEnumerator PaysSomethingOnce(string id)
        {
            var cache=Cache(id);Assert.IsNotNull(cache,"there is no cache "+id);Assert.IsTrue(cache.Available,id+" can't be opened");
            int Worth()=>State.scrip+State.parts.Count+State.materials.Values.Sum()+State.consumables.Values.Sum();
            int before=Worth();cache.Interact();yield return Frames(2);
            Assert.IsTrue(Has("cache."+id),id+" opened and isn't marked opened");Assert.Greater(Worth(),before,id+" gave nothing");
            before=Worth();cache.Interact();yield return Frames(2);Assert.AreEqual(before,Worth(),id+" paid twice");
        }
        static void RiverStory(){Landed();foreach(string f in new[]{"xylos.arrivalSeen","xylos.ropeHauled","xylos.termsHeard"})Flag(f);State.questSteps["SeedRoad"]=5;}

        [UnityTest] public IEnumerator TheRiverRunsArchSluicesCrewsAndSeedfallInTheOrderItsStoryNeeds()
        {
            RiverStory();yield return Zone("BraidedRiver");var misses=new List<string>();
            Assert.IsNull(Here("Jask"),"Jask is at the river's ferry before the party has looked down the river (he's still at the town's)");
            var arch=Point("xylos.archSeen");Assert.AreEqual("SelaRiverArch",arch.dialogueNode);Assert.IsTrue(arch.startsOnCrossing,"the first arch's look can be walked round");
            DialogueSystem.Current.AutoAdvance=true;Put(Lead,Floor(new Vector2(39,122.5f)));float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            yield return Settle();Assert.AreEqual(1,heard.Count(h=>h=="SelaRiverArch"),"walking east under the first arch doesn't start its talk");Assert.IsTrue(Has("xylos.archSeen"));
            yield return Frames(3);var jask=Here("Jask");Assert.IsNotNull(jask,"the party has looked down the river and Jask doesn't come to the ferry stage");
            Assert.Less(Flat(jask.transform.position,JaskStage),1,"Jask isn't on the ferry stage");
            var wheelA=Point("xylos.sluiceA");Assert.Less(Flat(wheelA.transform.position,RiverWheelA),1,"the middle sluice's wheel isn't on the north bank's east tip");
            yield return Points(5,wheelA.transform.position,"the middle sluice's wheel",2,misses);
            yield return Discover(wheelA);Assert.AreEqual("SelaSluiceA",heard.LastOrDefault());Assert.IsTrue(Has("xylos.sluiceA"),"the middle sluice's talk didn't open it");
            // Marrow-1: its crew comes out once its fight is won, and the relief is theirs to say.
            Assert.IsNull(Here("Garo"),"Garo is out of Marrow-1 before the fight round her is won");
            yield return Points(6,Fight("River_Marrow1").transform.position,"Marrow-1's fight, where Garo is shut in",16,misses);
            Flag("clear.River_Marrow1");yield return Frames(3);var garo=Here("Garo");Assert.IsNotNull(garo,"Marrow-1's fight won and Garo doesn't come out");Assert.Less(Flat(garo.transform.position,GaroAt),1,$"Garo came out at {garo.transform.position}, not at Marrow-1's hatch ({GaroAt})");
            yield return Talk(garo);Assert.AreEqual("GaroRelief",heard.LastOrDefault());Assert.IsTrue(Has("xylos.marrow1"));
            Assert.IsNull(Here("Dabb"),"Dabb is out of Marrow-2 before the fight round her is won");
            yield return Points(7,Fight("River_Marrow2").transform.position,"Marrow-2's fight, where Dabb is shut in",16,misses);
            Flag("clear.River_Marrow2");yield return Frames(3);var dabb=Here("Dabb");Assert.IsNotNull(dabb,"Marrow-2's fight won and Dabb doesn't come out");Assert.Less(Flat(dabb.transform.position,DabbAt),1,$"Dabb came out at {dabb.transform.position}, not at Marrow-2's hatch ({DabbAt})");
            yield return Talk(dabb);Assert.AreEqual("DabbRelief",heard.LastOrDefault());Assert.IsTrue(Has("xylos.marrow2"));
            var wheelB=Point("xylos.sluiceB");Assert.Less(Flat(wheelB.transform.position,RiverWheelB),1,"the south sluice's wheel isn't on island two's east tip");
            yield return Points(8,wheelB.transform.position,"the south sluice's wheel",2,misses);
            yield return Discover(wheelB);Assert.AreEqual("SelaSluiceB",heard.LastOrDefault());Assert.IsTrue(Has("xylos.sluiceB"));
            yield return Points(9,Fight("Seedthane").transform.position,"the Seedthane's ground",20,misses);
            // The fan's edge, from the islands: the talk that shows what is sitting on Marrow-3.
            Put(Lead,Floor(new Vector2(339.6f,95)));deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            yield return Settle();Assert.AreEqual(1,heard.Count(h=>h=="SelaSeedfall"),"walking onto Seedfall's fan doesn't show what's on Marrow-3");Assert.IsTrue(Has("xylos.seedfallSeen"));
            // The mooring roots stay shut until Teo has said what holds them; he comes to Marrow-3 once the Seedthane is down.
            var roots=All<WarpBeacon>().Single(w=>w.scene=="WalkingCanopy");roots.Interact();yield return Frames(2);
            Assert.IsFalse(SceneFlow.Current.Loading,"the mooring roots opened before Teo was heard");StringAssert.Contains("Teo",roots.prompt);
            Assert.IsNull(Here("Teo"),"Teo is at Marrow-3 before the Seedthane is down");
            Flag("bossdown.Seedthane");Flag("clear.Seedthane");RpgServices.Quests.Report(ObjectiveKind.Kill,"Seedthane");yield return Frames(3);
            var teo=Here("Teo");Assert.IsNotNull(teo,"the Seedthane is down and Teo doesn't come to Marrow-3");
            // He walks up from the root bridge to the carrier (a ScriptedWalk at 2.4 m/s, about 37 m).
            deadline=Time.unscaledTime+25;while(Flat(teo.transform.position,TeoSeedfall)>1&&Time.unscaledTime<deadline)yield return null;if(Flat(teo.transform.position,TeoSeedfall)>=1)misses.Add($"Teo's walk up from the bridge stops at {teo.transform.position}, not beside Marrow-3 ({TeoSeedfall})");
            yield return Points(10,new Vector3(TeoSeedfall.x,0,TeoSeedfall.y),"Teo at Marrow-3",2,misses);   // a Vector2 here became (x, z, 0) and was measured a hundred metres off
            yield return Talk(teo);Assert.AreEqual("TeoSeedfall",heard.LastOrDefault());Assert.IsTrue(Has("xylos.seedthaneGone"));
            yield return Points(11,roots.transform.position,"the mooring roots to the Walking Canopy",3,misses);
            roots.Interact();yield return Arrive("WalkingCanopy");
            Assert.Less(Flat(Lead.transform.position,All<SpawnPoint>().Single(s=>s.id=="Arrival").transform.position),4,"the mooring roots didn't land at the Canopy's arrival");
            Assert.IsEmpty(misses,"along the river, the objective's arrow and who stands where:\n"+string.Join("\n",misses));
        }

        [UnityTest] public IEnumerator TheRiversPicksPostCachesAndCampCradleAnswerTheirQuestsOnce()
        {
            RiverStory();Flag("xylos.archSeen");Flag("xylos.sluiceA");Flag("xylos.sluiceB");State.consumables["RepairGel"]=0;yield return Zone("BraidedRiver");
            var pods=new[]{"red","blue","yellow"}.Select(c=>Point("xylos.pods."+c)).ToArray();var fork=Point("senn.postSet");
            foreach(var pick in pods.Append(fork))Assert.IsFalse(pick.Available,$"\"{pick.prompt}\" is offered before its quest has asked for it");
            RpgServices.Quests.Start("ThreeBaskets");RpgServices.Quests.Start("MooringPost");yield return Frames(2);
            for(int i=0;i<3;i++){yield return Discover(pods[i]);Assert.IsTrue(Has(pods[i].flag),pods[i].prompt+" didn't pick");Assert.IsFalse(pods[i].Available,pods[i].prompt+" is offered again");}
            Assert.AreEqual(1,Step("ThreeBaskets"),"three plants picked and Binna's quest isn't waiting on the baskets");
            yield return Discover(fork);Assert.AreEqual("SelaMooringPost",heard.LastOrDefault());Assert.AreEqual(1,Step("MooringPost"),"Senn's post set and his quest doesn't move on");
            foreach(string id in new[]{"xylos.river.arch","xylos.river.camp","xylos.river.marrow2","xylos.river.ulma","xylos.river.bar"})yield return PaysSomethingOnce(id);
            var cradle=All<RepairBay>().Single(b=>b.saveSpawn=="Camp");Assert.IsTrue(cradle.free,"the ferry cradle charges");
            var camp=All<SpawnPoint>().SingleOrDefault(s=>s.id=="Camp");Assert.IsNotNull(camp,"the river has no Camp spawn for its cradle");Assert.Less(Flat(camp.transform.position,cradle.transform.position),10);
            cradle.Interact();yield return Frames(2);Assert.AreEqual("Camp",State.spawn);
            yield return SaveQuitAndContinue();
            Assert.Less(Flat(Lead.transform.position,All<SpawnPoint>().Single(s=>s.id=="Camp").transform.position),4,"Continue didn't bring the party back to the camp it saved at");
            foreach(string f in new[]{"xylos.pods.red","senn.postSet","cache.xylos.river.bar","xylos.sluiceA","xylos.sluiceB"})Assert.IsTrue(Has(f),f+" was lost through a save");
            Assert.IsFalse(Point("xylos.pods.red").Available||Cache("xylos.river.camp").gameObject.activeInHierarchy,"a picked plant or an opened cache came back after Continue");
        }

        [UnityTest] public IEnumerator TheCanopysAnchorsVaultAndBasinDoorAnswerTheStoryAndItsCradleSavesAtTheDoor()
        {
            RiverStory();foreach(string f in new[]{"xylos.archSeen","xylos.sluiceA","xylos.marrow1","xylos.marrow2","xylos.sluiceB","xylos.seedfallSeen","bossdown.Seedthane","xylos.seedthaneGone"})Flag(f);
            State.questSteps["SeedRoad"]=11;yield return Zone("WalkingCanopy");
            var hall=Point("xylos.canopyEntered");Assert.AreEqual("SelaCanopyMooring",hall.dialogueNode);
            DialogueSystem.Current.AutoAdvance=true;Put(Lead,Floor(new Vector2(2,8),-2.8f));float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            yield return Settle();Assert.AreEqual(1,heard.Count(h=>h=="SelaCanopyMooring"),"walking into the root hall doesn't start its talk");
            var anchors=Enumerable.Range(1,3).Select(n=>Point("xylos.anchor"+n)).ToArray();
            foreach(var (anchor,at) in anchors.Zip(new[]{new Vector2(8.6f,13),new Vector2(-36.4f,57.6f),new Vector2(-10.6f,76.4f)},(a,p)=>(a,p)))
            {Assert.AreEqual("AnchorRoot",anchor.report);Assert.Less(Flat(anchor.transform.position,at),1,anchor.name+" isn't where the map has it");}
            yield return Points(11,anchors[0].transform.position,"the root hall's anchor root");
            for(int n=0;n<3;n++)
            {
                yield return Discover(anchors[n]);Assert.IsTrue(Has("xylos.anchor"+(n+1)),$"anchor root {n+1} didn't take");
                if(n<2){Assert.AreEqual(n+1,State.questCounts.TryGetValue("SeedRoad/11",out int c)?c:0,$"anchor root {n+1} cut and the story counts otherwise");yield return Points(11,anchors[n+1].transform.position,$"anchor root {n+2}");}
            }
            yield return Points(12,Fight("Floodcrown").transform.position,"the basin's door",20);
            var slot=Point("xylos.vaultSeen");yield return Discover(slot);Assert.AreEqual("SelaSeedVault",heard.LastOrDefault());
            foreach(string id in new[]{"xylos.canopy.porch","xylos.canopy.gallery","xylos.canopy.vault"})yield return PaysSomethingOnce(id);
            var cradle=All<RepairBay>().Single();Assert.AreEqual("Basin",cradle.saveSpawn,"the root cradle saves somewhere other than the basin door");Assert.IsTrue(cradle.free);
            var door=Point("xylos.basinSeen");Assert.AreEqual("SelaFloodcrownBasin",door.dialogueNode);Assert.AreEqual("Basin",door.saveSpawn,"the basin's door talk doesn't make the door a checkpoint");
            Assert.AreEqual("xylos.basinSeen",Fight("Floodcrown").requiredFlag,"the Floodcrown wakes without the door's talk");
            cradle.Interact();yield return Frames(2);yield return SaveQuitAndContinue();
            Assert.Less(Flat(Lead.transform.position,All<SpawnPoint>().Single(s=>s.id=="Basin").transform.position),4,"Continue didn't bring the party back to the basin's door");
            foreach(string f in new[]{"xylos.anchor1","xylos.anchor2","xylos.anchor3","xylos.vaultSeen","cache.xylos.canopy.vault"})Assert.IsTrue(Has(f),f+" was lost through a save");
            Assert.AreEqual(12,Step("SeedRoad"),"the story lost its place through a save");
            // Won, the creature leaves with the water, and the party hears it: then the story turns for home.
            Flag("bossdown.Floodcrown");Flag("clear.Floodcrown");RpgServices.Quests.Report(ObjectiveKind.Kill,"Floodcrown");
            // The first anchor's after-talk is still owed here (the anchors were cut one after another with no calm between, and the cradle saved before it could play), so the load
            // plays it first and the Floodcrown's comes two seconds after it: wait for that one by name, not for the first conversation to start (which read as the after-talk failing).
            DialogueSystem.Current.AutoAdvance=true;deadline=Time.unscaledTime+20;while(!heard.Contains("SelaFloodcrownGone")&&Time.unscaledTime<deadline)yield return null;yield return Settle();
            var after=All<AfterTalk>().SingleOrDefault(a=>a.node=="SelaFloodcrownGone");
            Assert.Contains("SelaFloodcrownGone",heard,$"the Floodcrown down and nobody says it's gone (heard {string.Join(", ",heard)}; its after-talk {(after==null?"isn't in the scene":after.isActiveAndEnabled?"is waiting":"is off")}; input blocked {GameServices.Current.Input.Blocked}, a boss fight under way {BossController.AnyFightUnderway}, an enemy awake near {EnemyBrain.AwakeNear(Lead.transform.position,EnemyBrain.InteractionCalm)})");Assert.IsTrue(Has("xylos.floodcrownGone"));
            yield return Points(13,All<WarpBeacon>().Single(w=>w.scene=="BraidedRiver").transform.position,"the mooring roots home",3);
        }

        [UnityTest] public IEnumerator TheVoteIsHeldAtTheSeedHouseAndAbrisCallOnTheWayToThePadEndsTheChapter()
        {
            RiverStory();foreach(string f in new[]{"xylos.archSeen","xylos.sluiceA","xylos.marrow1","xylos.marrow2","xylos.sluiceB","xylos.seedfallSeen","bossdown.Seedthane","xylos.seedthaneGone",
                "xylos.anchor1","xylos.anchor2","xylos.anchor3","bossdown.Floodcrown","xylos.floodcrownGone","met.Jask"})Flag(f);
            State.questSteps["SeedRoad"]=13;RpgServices.Quests.Start("Questions");State.questSteps["Questions"]=1;yield return Zone("Rootmarket","River");
            var ruva=Here("Ruva");Assert.Less(Flat(ruva.transform.position,RuvaSeedHouse),1,"Ruva isn't at the seed house for the vote");
            yield return Points(13,ruva.transform.position,"Ruva at the seed house");
            var abri=Point("chapter3.complete");Assert.AreEqual("AbriDinnerCall",abri.dialogueNode);Assert.IsFalse(abri.Available,"Abri's call can come before the vote");
            yield return Talk(ruva);Assert.AreEqual("RuvaVote",heard.LastOrDefault(),"the crossing open and Ruva doesn't hold the vote");
            Assert.IsTrue(Has("xylos.voteHeard"));Assert.IsTrue(Has("quest.SeedRoad.complete"),"the vote heard and the chapter's quest isn't complete");
            yield return Frames(3);Assert.IsNull(Here("Jask"),"Jask is still in town after the vote: he has stowed away");Assert.IsNotNull(Here("Teo"),"Teo isn't back in town after the vote");
            Assert.IsTrue(Point("jask.poleFound").Available,"Jask's pole isn't at the ferry stage after the vote");
            // On the way back to the pad: Abri on the radio, and the chapter closes.
            Assert.IsTrue(abri.Available,"Abri's call isn't waiting on the way to the pad after the vote");
            DialogueSystem.Current.AutoAdvance=true;Put(Lead,Floor(new Vector2(4,22)));float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            yield return Settle();Assert.AreEqual(1,heard.Count(h=>h=="AbriDinnerCall"),"walking back to the pad after the vote doesn't bring Abri's call");
            Assert.IsTrue(Has("chapter3.complete"),"Abri's call didn't close the chapter");
            var saved=GameServices.Current.Saves.Load("autosave");Assert.IsTrue(saved!=null&&saved.flags.TryGetValue("chapter3.complete",out bool kept)&&kept,"the chapter's end wasn't saved");
        }

        /// <summary>The ways feet go between the town's named places, as the builder lists them (XylosBuilder.Rootmarket's RootWays, with its named spots written out).</summary>
        static readonly Vector2[][] RootWays=
        {
            new[]{TownArrival,new Vector2(13,25.5f),new Vector2(20,26.5f),RuvaMooring,new Vector2(32.4f,18.8f)},
            new[]{TownArrival,TeoPool,new Vector2(-19,27),new Vector2(-27,38),JaskFerry,new Vector2(-26,52),new Vector2(-17,62),new Vector2(-6,68),RuvaSeedHouse},
            new[]{RuvaSeedHouse,new Vector2(11,63.5f),new Vector2(17.5f,62.5f),BinnaKitchen,new Vector2(23.5f,58),new Vector2(27.5f,50),new Vector2(28.5f,45),HaddaPress,new Vector2(45,43),RiverGate},
            new[]{HaddaPress,new Vector2(29,40),new Vector2(25,32),RuvaMooring},new[]{new Vector2(-20,44),new Vector2(20,44)},
            new[]{new Vector2(-19,27),new Vector2(-24,28),new Vector2(-39.2f,17.4f),SennIsland},
        };
        static float ToSegment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));return Vector2.Distance(p,a+d*t);}
        /// <summary>Lanternwick's rule (QaWorldSecondTests: TownRoutes), which is pinned to chapter 2's town: someone with no name stands 1.6 m or more off every way between the named, and
        /// never between the fixed camera and anyone with a name.</summary>
        [UnityTest] public IEnumerator RootmarketsTownsfolkStandOffItsWaysAndOutOfTheCamerasLineToAnyoneNamed()
        {
            Landed();Flag("xylos.arrivalSeen");Flag("xylos.ropeHauled");yield return Zone("Rootmarket");
            var folk=All<Transform>().Where(t=>t.name=="Townsperson"&&t.parent==null).ToArray();
            Assert.GreaterOrEqual(folk.Length,11,"fixture: Rootmarket's people with no names, standing");
            var faults=new List<string>();
            foreach(var person in folk)
            {
                var at=new Vector2(person.position.x,person.position.z);
                foreach(var way in RootWays)for(int i=1;i<way.Length;i++)
                {float gap=ToSegment(at,way[i-1],way[i]);if(gap<=1.6f)faults.Add($"a townsperson at {at} stands {gap:0.00} m from the way from {way[i-1]} to {way[i]}");}
                foreach(var npc in All<Npc>())foreach(float height in new[]{.4f,.9f,1.5f})
                {
                    var target=npc.transform.position+Vector3.up*height;var eye=target+SightlineAudit.ToCamera*SightlineAudit.Distance;
                    var solid=new Bounds(person.position+Vector3.up*.95f,new Vector3(.9f,1.9f,.9f));
                    if(solid.IntersectRay(new Ray(eye,target-eye),out float along)&&along<SightlineAudit.Distance-SightlineAudit.NearIgnore){faults.Add($"a townsperson at {at} stands between the camera and {npc.speaker}");break;}
                }
            }
            Assert.IsEmpty(faults,"Rootmarket's townsfolk stand in the way:\n"+string.Join("\n",faults.Distinct()));
        }

        /// <summary>The town's laid floors, as the builder lays them (XylosBuilder.Rootmarket's FloorMats): middle, half size, turn.</summary>
        static readonly (Vector2 at,Vector2 half,float yaw)[] FloorMats={(new(25.2f,65.6f),new(6.2f,4),-8),(new(1.4f,68.6f),new(6,3.4f),0),(new(-2.6f,21.6f),new(4.6f,4),0),(new(27.4f,22.2f),new(4.4f,3.4f),12),
            (new(14.8f,24.6f),new(6,1.4f),-11.6f),(new(26,51.8f),new(6.1f,1.6f),67.4f)};
        /// <summary>Points of a laid floor no triangle of the floors' mesh lies over, on the plan, sampled every half metre a hand in from its border.</summary>
        static List<string> Bare(Vector3[] v,int[] t)
        {
            var tris=new List<(Vector2 a,Vector2 b,Vector2 c)>();for(int i=0;i<t.Length;i+=3)tris.Add((new Vector2(v[t[i]].x,v[t[i]].z),new Vector2(v[t[i+1]].x,v[t[i+1]].z),new Vector2(v[t[i+2]].x,v[t[i+2]].z)));
            static float Side(Vector2 p,Vector2 a,Vector2 b)=>(b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);
            bool Over(Vector2 p){foreach(var (a,b,c) in tris){float s1=Side(p,a,b),s2=Side(p,b,c),s3=Side(p,c,a);if((s1>=0&&s2>=0&&s3>=0)||(s1<=0&&s2<=0&&s3<=0))return true;}return false;}
            var bare=new List<string>();
            foreach(var (at,half,yaw) in FloorMats)
            {
                var turn=Quaternion.Euler(0,yaw,0);
                for(float x=-half.x+.3f;x<=half.x-.3f+.01f;x+=.5f)for(float z=-half.y+.3f;z<=half.y-.3f+.01f;z+=.5f)
                {var q=turn*new Vector3(x,0,z);var p=new Vector2(at.x+q.x,at.y+q.z);if(!Over(p))bare.Add($"({p.x:0.0}, {p.y:0.0}) on the floor at {at}");}
            }
            return bare;
        }
        /// <summary>Every laid floor in Rootmarket is laid whole. A mat was left out wherever a floor sign's ground came near it, and a sign goes for every conversation: through the
        /// whole arrival talk the mooring's and the seed house's floors showed a bare pale square in a corner (s18 010 and 051, s19 061, November 22).</summary>
        [UnityTest] public IEnumerator EveryLaidFloorInRootmarketIsLaidWhole()
        {
            Landed();Flag("xylos.arrivalSeen");Flag("xylos.ropeHauled");yield return Zone("Rootmarket");
            var laid=All<MeshFilter>().Where(f=>f.name=="Laid floors").ToArray();Assert.AreEqual(1,laid.Length,"fixture: Rootmarket's laid floors");
            var mesh=laid[0].sharedMesh;Assert.IsTrue(mesh.isReadable,"fixture: the laid floors' mesh can be read");
            var v=mesh.vertices.Select(p=>laid[0].transform.TransformPoint(p)).ToArray();var t=mesh.triangles;
            // The check sees a mat left out: the same floors with the mooring's north-west corner mat taken away, as it was laid until November 22.
            var off=Quaternion.Euler(0,12,0)*new Vector3(-3.7f,0,2.8f);var corner=new Vector2(27.4f+off.x,22.2f+off.z);
            var without=new List<int>();for(int i=0;i<t.Length;i+=3){var mid=(v[t[i]]+v[t[i+1]]+v[t[i+2]])/3;if(Vector2.Distance(new Vector2(mid.x,mid.z),corner)>.9f)without.AddRange(new[]{t[i],t[i+1],t[i+2]});}
            Assert.IsNotEmpty(Bare(v,without.ToArray()),"fixture: the check can't see a mat left out of the mooring's floor");
            var bare=Bare(v,t);
            Assert.IsEmpty(bare,bare.Count+" points of Rootmarket's laid floors have no mat over them: "+string.Join("; ",bare.Take(12)));
        }
        /// <summary>From wherever BINNA'S KITCHEN is read on the way past it (on the town's ways within six metres of it, the sign itself in the open middle of the screen), at least two of
        /// the kitchen's oven, its pot on legs and its drying rack are on screen and clear of the corner panels: a kitchen that reads as one. Laid out across its north-east they stood
        /// at the top of the frame and under the objective card (s22 014 and 017, November 22): the camera looks north-north-east, and the sign is at the kitchen's south-west corner.</summary>
        [UnityTest] public IEnumerator BinnasKitchenIsOnScreenWhereverItsSignIsRead()
        {
            Landed();Flag("xylos.arrivalSeen");Flag("xylos.ropeHauled");yield return Zone("Rootmarket");
            var sign=All<TextMeshPro>().Where(s=>s.name.StartsWith("Sign_")&&s.text=="BINNA'S KITCHEN").ToArray();Assert.AreEqual(1,sign.Length,"fixture: the kitchen's floor sign");
            // The kitchen's own (the press has a drying rack of its own).
            var works=new[]{"Clay oven","Cook pot","Drying rack"}.Select(n=>All<BoxCollider>().Where(b=>b.name==n&&b.transform.root.name=="Kitchen").ToArray()).ToArray();
            foreach(var found in works)Assert.AreEqual(1,found.Length,"fixture: the kitchen's works, each once");
            var at=works.Select(w=>{var b=w[0].bounds;return new Vector3(b.center.x,b.max.y,b.center.z);}).ToArray();
            // Where they stood until November 22, on the same floor: what the check has to refuse.
            float y=at[0].y;var before=new[]{new Vector3(32.2f,y,68.8f),new Vector3(28.8f,y,65.6f),new Vector3(32.8f,y,61.2f)};
            var view=new GameObject("Kitchen check camera",typeof(Camera)).GetComponent<Camera>();view.fieldOfView=30;view.aspect=16f/9;view.enabled=false;
            var rotation=Quaternion.Euler(SightlineAudit.Pitch,SightlineAudit.Yaw,0);
            bool Open(Vector3 p){var s=view.WorldToViewportPoint(p);return s.z>0&&s.x>.03f&&s.x<.97f&&s.y>.03f&&s.y<.97f&&!(s.y>CameraRig.PanelFoot&&(s.x<CameraRig.PanelSide||s.x>1-CameraRig.PanelSide));}
            var signAt=sign[0].transform.position;var faults=new List<string>();int stands=0,refused=0;
            foreach(var way in RootWays)for(int i=1;i<way.Length;i++)
            {
                float length=Vector2.Distance(way[i-1],way[i]);
                for(float s=0;s<=length;s+=1.5f)
                {
                    var p=Vector2.Lerp(way[i-1],way[i],s/length);if(Vector2.Distance(p,new Vector2(signAt.x,signAt.z))>6)continue;
                    var hero=new Vector3(p.x,signAt.y-.1f,p.y);view.transform.SetPositionAndRotation(hero+Vector3.up*SightlineAudit.Chest-rotation*Vector3.forward*SightlineAudit.Distance,rotation);
                    if(!Open(signAt))continue;stands++;
                    int seen=at.Count(Open);if(seen<2)faults.Add($"from {p} only {seen} of the oven, the pot and the rack are on screen with the sign");
                    if(before.Count(Open)<2)refused++;
                }
            }
            Object.Destroy(view.gameObject);
            Assert.Greater(stands,4,"fixture: places on the ways where the kitchen's sign is read");
            Assert.Greater(refused,0,"fixture: the check can't see the kitchen as it was laid until November 22");
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        // ================================================================== the replay routes, checked against the built zones
        static readonly string[] ChapterThreeRoutes={"chapter-17-xylos-nearspace","chapter-18-rootmarket","chapter-19-rootmarket-errands","chapter-24-braided-river","chapter-25-walking-canopy","chapter-26-rootmarket-vote"};
        /// <summary>One straight leg the driver walks or flies: from where (unknown after a chase, and at a zone's first leg in flight), to where, and which barges are free by then.</summary>
        sealed class Leg{public string route,name,scene;public Vector3? from;public Vector3 to;public float tolerance;public HashSet<string> freed;}
        static List<Leg> Legs()
        {
            var legs=new List<Leg>();string root=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
            foreach(string name in ChapterThreeRoutes)
            {
                string file=System.IO.Path.Combine(root,name+".json");Assert.IsTrue(System.IO.File.Exists(file),"fixture: "+file+" (python tools/xylos_routes.py writes it)");
                var route=JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(file));string scene=null;Vector3? at=null;
                // Only chapter-17 frees the barges; every route after it meets all three where they rest.
                var freed=name=="chapter-17-xylos-nearspace"?new HashSet<string>():new HashSet<string>{"xylos.barge1","xylos.barge2","xylos.barge3"};
                foreach(var step in route.steps)
                {
                    if(step.expectedScene!=scene){scene=step.expectedScene;at=null;}
                    if(!string.IsNullOrEmpty(step.expectedFlag)&&step.expectedFlag.StartsWith("xylos.barge"))freed.Add(step.expectedFlag);
                    if(!step.navigate)continue;
                    if(step.approachTarget||step.approachPartner){at=null;continue;}
                    legs.Add(new Leg{route=name,name=step.name,scene=scene,from=at,to=step.point,tolerance=step.tolerance,freed=new HashSet<string>(freed)});at=step.point;
                }
            }
            return legs;
        }
        /// <summary>What a hull flying this leg at the plane would meet, bodies left out.</summary>
        static Collider[] InTheWay(Vector3? from,Vector3 to,float hull)
        {
            to.y=1;var met=Physics.OverlapSphere(to+Vector3.up*.8f,hull,~0,QueryTriggerInteraction.Ignore).AsEnumerable();
            if(from is Vector3 start)
            {
                start.y=1;var way=to-start;
                if(way.magnitude>.01f)met=met.Concat(Physics.SphereCastAll(start+Vector3.up*.8f,hull,way.normalized,way.magnitude,~0,QueryTriggerInteraction.Ignore).Select(h=>h.collider));
            }
            return met.Where(c=>c!=null&&!(c is CharacterController)&&c.GetComponentInParent<Health>()==null).Distinct().ToArray();
        }
        /// <summary>Every zone the chapter 3 routes walk or fly, one case each, so a zone's own run (focused.ps1 -Zones) reaches its legs.</summary>
        static readonly string[] ChapterThreeLegZones={"TallowDrift","TallowApproach","XylosNearspace","Rootmarket","BraidedRiver","WalkingCanopy"};
        [Test] public void TheChapterThreeLegCheckHasACaseForEveryZoneItsRoutesWalk()
            =>CollectionAssert.AreEquivalent(Legs().Select(l=>l.scene).Distinct().ToArray(),ChapterThreeLegZones,"a chapter 3 route walks a zone the leg check has no case for, or the other way round");
        [UnityTest] public IEnumerator EveryWalkOnTheChapterThreeRoutesFollowsFloorABodyCanCross([ValueSource(nameof(ChapterThreeLegZones))] string only)
        {
            var legs=Legs();Assert.IsNotEmpty(legs.Where(l=>l.scene==only).ToArray(),"fixture: the chapter 3 routes walk "+only);
            var faults=new List<string>();float hull=Mathf.Max(HeroCollision.HullRadius("Taren"),HeroCollision.HullRadius("Sela"));
            foreach(string scene in new[]{only})
            {
                var mine=legs.Where(l=>l.scene==scene).ToArray();var zone=GameCatalog.Find<ZoneDef>(scene);Assert.IsNotNull(zone,"fixture: a route walks in "+scene+", which is no zone");
                bool flying=zone.kind==ZoneKind.SpaceCombat||zone.kind==ZoneKind.SpaceSafe;
                if(!flying)
                {
                    // As the routes walk them: the river with both sluices open (its drained beds are walked), the Canopy with its three anchors cut (no anchor shuts a door an earlier one opened).
                    foreach(string f in new[]{"xylos.charted","xylos.sluiceA","xylos.sluiceB","xylos.anchor1","xylos.anchor2","xylos.anchor3"})Flag(f);yield return Zone(scene);
                    var spawns=All<SpawnPoint>();
                    foreach(var leg in mine)
                    {
                        string where=$"{leg.route} '{leg.name}' ({scene})";var start=leg.from;
                        // A route's first walk in a zone starts at the spawn it arrived at: the nearest, if there's one near enough to be it.
                        if(start==null){var spawn=spawns.OrderBy(s=>Flat(s.transform.position,leg.to)).FirstOrDefault();if(spawn!=null&&Flat(spawn.transform.position,leg.to)<25)start=spawn.transform.position;}
                        if(!NavMesh.SamplePosition(leg.to+Vector3.up*.5f,out var end,2.5f,NavMesh.AllAreas)||Flat(end.position,leg.to)>Mathf.Max(.6f,leg.tolerance)){faults.Add($"{where} ends at {leg.to}, off the floor a body walks"+(NavMesh.SamplePosition(leg.to,out var nearest,6,NavMesh.AllAreas)?$" (the nearest floor is at {nearest.position})":""));continue;}
                        if(start==null)continue;
                        if(!NavMesh.SamplePosition(start.Value+Vector3.up*.5f,out var begin,2.5f,NavMesh.AllAreas)){faults.Add($"{where} starts at {start.Value}, off the floor");continue;}
                        if(NavMesh.Raycast(begin.position,end.position,out var cut,NavMesh.AllAreas))
                        {
                            // What stands there, and whether the floor joins the two ends at all: the evidence a finding needs.
                            var near=Physics.OverlapSphere(cut.position+Vector3.up*.6f,1.1f,~0,QueryTriggerInteraction.Ignore).Where(c=>!(c is CharacterController)&&c.GetComponentInParent<CombatActor>()==null&&Mathf.Max(c.bounds.size.x,c.bounds.size.z)<30)
                                .Select(c=>c.transform.root==c.transform?c.name:c.transform.root.name+"/"+c.name).Distinct().ToArray();
                            var path=new NavMeshPath();bool joined=NavMesh.CalculatePath(begin.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                            // The ray stops at a seam where a walk's deck meets a raft as readily as at a wall. The floor's own shortest way is the judge: if it is the straight line itself
                            // (no corner between the ends), the straight walk is on floor; anything standing on the line, or a rim it clips, bends that way.
                            if(joined&&path.corners.Length<=2)continue;
                            float walked=0;for(int i=1;i<path.corners.Length;i++)walked+=Vector3.Distance(path.corners[i-1],path.corners[i]);
                            faults.Add($"{where}: the straight walk from {start.Value} to {leg.to} is cut at {cut.position}; within a metre there: {(near.Length>0?string.Join(", ",near):"nothing solid (an edge of the floor)")}; "
                                +(joined?$"the floor joins the ends by {walked:0.0} m against {Vector3.Distance(begin.position,end.position):0.0} straight, by {string.Join(" ",path.corners.Select(c=>$"({c.x:0.0},{c.z:0.0})"))}":"the floor doesn't join the ends at all"));
                        }
                    }
                    continue;
                }
                // The walks on foot are judged before any flight zone loads, so a fault on the lane can't hide what the town's legs did.
                Assert.IsEmpty(faults,"the chapter 3 routes walk where a body can't:\n"+string.Join("\n",faults.Distinct().Take(40)));
                if(scene!="XylosNearspace")
                {
                    Flag("xylos.charted");yield return Zone(scene);
                    foreach(var leg in mine)foreach(var c in InTheWay(leg.from,leg.to,hull))faults.Add($"{leg.route} '{leg.name}' ({scene}) flies into {c.name} on the way to {leg.to}");
                    continue;
                }
                // The lane twice: with every barge where it lies snagged, then with every barge at rest. A leg meets a barge where it is at that point of the route only.
                foreach(bool free in new[]{false,true})
                {
                    foreach(string f in new[]{"xylos.barge1","xylos.barge2","xylos.barge3"})Flag(f,free);
                    ToTheLane();yield return Zone(scene);
                    foreach(var leg in mine)
                    {
                        if(!free&&FromLane(leg.to)>LaneHalf-1-hull)faults.Add($"{leg.route} '{leg.name}' ends {FromLane(leg.to):0.0} m off the lane's middle, in its limit");
                        foreach(var c in InTheWay(leg.from,leg.to,hull))
                        {
                            var barge=c.GetComponentInParent<SnaggedBarge>();
                            if(barge!=null&&leg.freed.Contains(barge.freedFlag)!=free)continue;
                            faults.Add($"{leg.route} '{leg.name}' flies into {(barge!=null?barge.name+(free?" at rest":" where it is snagged"):c.name)} on the way to {leg.to}");
                        }
                    }
                }
            }
            Assert.IsEmpty(faults,"the chapter 3 routes walk or fly where a body can't:\n"+string.Join("\n",faults.Distinct().Take(40)));
        }

        // ================================================================== the story, step by step
        [UnityTest] public IEnumerator TheSeedRoadMovesStepByStepFromTallowsWarpToTheRiverGateAndAlwaysPointsAtARealPlace()
        {
            Flag("xylos.charted");RpgServices.Quests.Start("SeedRoad");RpgServices.Quests.Report(ObjectiveKind.Flag,"xylos.charted");
            Assert.AreEqual(1,Step("SeedRoad"),"fixture: the lane is charted and the warp is next");
            var quest=GameCatalog.Find<QuestDef>("SeedRoad");Assert.IsTrue(quest.main,"the Seed Road isn't the chapter's main quest");
            IEnumerator Says(int step,Vector3 near,string what)
            {
                yield return new WaitForSecondsRealtime(.6f);
                Assert.AreEqual(step,Step("SeedRoad"),$"the story isn't at step {step} ({quest.stepText[step]})");
                StringAssert.StartsWith(quest.stepText[step],Objective(),$"at step {step} the objective doesn't say its step's words");
                var goal=Goal();Assert.IsTrue(goal.HasValue,$"at step {step} the objective points nowhere: it should point at {what}");
                Assert.Less(Flat(goal.Value,near),1.5f,$"at step {step} the objective points at {goal.Value}, not at {what} ({near})");
            }
            yield return Zone("TallowApproach");
            var warp=All<WarpBeacon>().Single(w=>w.scene=="XylosNearspace");
            yield return Says(1,warp.transform.position,"the warp to Xylos");
            warp.Interact();yield return Arrive("XylosNearspace");
            var held=All<DiscoveryPoint>().Where(p=>p.report=="SeedBarge").ToArray();Assert.AreEqual(3,held.Length,"the lane doesn't hold three barges for the story to count");
            yield return Says(2,held.OrderBy(p=>Flat(p.transform.position,Lead.transform.position)).First().transform.position,"the nearest snagged barge");
            StringAssert.EndsWith("(0/3)",Objective().Split('\n')[0],"the barges' step doesn't count them");
            for(int n=1;n<=3;n++)
            {
                yield return Free(n);yield return new WaitForSecondsRealtime(.6f);
                if(n<3)
                {
                    StringAssert.EndsWith($"({n}/3)",Objective().Split('\n')[0],$"barge {n} free and the objective doesn't count it");
                    var still=held.Where(p=>p.Wanted).ToArray();Assert.AreEqual(3-n,still.Length,"a freed barge is still wanted, or a held one isn't");
                    yield return Says(2,still.OrderBy(p=>Flat(p.transform.position,Lead.transform.position)).First().transform.position,"the nearest barge still held");
                }
            }
            var land=All<WarpBeacon>().Single(w=>w.scene=="Rootmarket");
            yield return Says(3,land.transform.position,"the delta beacon");
            land.Interact();yield return Arrive("Rootmarket");
            yield return Says(4,Here("Ruva").transform.position,"Ruva at the mooring");
            DialogueSystem.Current.AutoAdvance=true;Put(Lead,Floor(new Vector2(Crossing.x-.1f,25)));float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            yield return Settle();
            yield return Haul(All<MooringHaul>().Single());
            yield return Says(4,Here("Ruva").transform.position,"Ruva, for her terms");
            yield return Talk(Here("Ruva"));
            var gate=All<WarpBeacon>().Single(w=>w.scene=="BraidedRiver");
            yield return Says(5,gate.transform.position,"the river gate to the Braided River");
        }

        /// <summary>The roots drawn across the basin's passage for the fight stand where the Floodcrown can't put its body. It is eight metres long on a capsule two round, so its head
        /// or tail reaches four from wherever it stands in its basin: at 31.8 the seal stood 3.3 m off the rim, and a charge run aground there had the roots straight through its body
        /// (nov20-whole s21 262 and 266).</summary>
        [UnityTest] public IEnumerator TheBasinSealStandsBeyondTheFloodcrownsReachFromItsBasin()
        {
            yield return Zone("WalkingCanopy","Basin");
            var seal=All<Membrane>().SingleOrDefault(m=>m.name=="Basin seal");Assert.IsNotNull(seal,"fixture: the basin seal");
            var definition=GameCatalog.Find<EnemyDef>("Floodcrown");Assert.IsNotNull(definition,"fixture: the Floodcrown is defined");
            var body=ActorFactory.Enemy(definition,new Vector3(200,0,200));body.Passive=true;yield return null;yield return null;
            var drawn=new Bounds(body.transform.position,Vector3.zero);
            foreach(var view in body.GetComponentsInChildren<Renderer>())if(view.enabled&&!(view is ParticleSystemRenderer))drawn.Encapsulate(view.bounds);
            float reach=Mathf.Max(drawn.extents.x,drawn.extents.z);Object.Destroy(body.gameObject);
            Assert.Greater(reach,2.5f,"fixture: the Floodcrown's drawn body was measured");
            // The basin, as XylosBuilder.Canopy has it (copied: the tests can't see the editor assembly): 17 m round about (24, 52).
            float fromBasin=Vector2.Distance(new Vector2(seal.transform.position.x,seal.transform.position.z),new Vector2(24,52))-17;
            Assert.Greater(fromBasin,reach+.4f,$"the basin seal stands {fromBasin:0.0} m off the basin's rim, and the Floodcrown's body reaches {reach:0.0} m from where it stands");
        }
    }
}
