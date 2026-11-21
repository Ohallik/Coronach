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
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 6's way in, its flight zone and its town as a player meets them, in the built scenes: the chart's reading for Hoarfell and the hull's warp (staged in Many
    /// Hands), the Aurora Shear (the currents that carry a ship with them and drain it against them, Idra's hail and tow as the first current turns, her downed cutter, the way down
    /// that waits for her), Lastlight (Old Tern seated under the fourth rib, Idra and Hask at the pens, the plan at the carving yard and the Reach gate that waits for it, Hask's
    /// counter, the bath-house cradle and the bench, the climb to the high lamps, the Kept at their work, Iru speaking up), and Idra joining as the third hero. What every zone gets
    /// from the shared checks is theirs and not repeated here. Coordinates are the map briefs' (docs/maps/AuroraShear.md, Lastlight.md), which are the builder's own.</summary>
    public sealed class HoarfellWorldTests
    {
        readonly List<string> heard=new(),log=new();
        void Talked(string node,string speaker,System.Action<bool> done)=>heard.Add(node);
        void Looked(DiscoveryPoint point){if(!string.IsNullOrEmpty(point.dialogueNode))heard.Add(point.dialogueNode);}
        static readonly string[] Heeded={"CURRENT_","TOW_","CUTTER_LEADS","DISCOVERY","PARTY_JOIN","LEAVES","GONE","WALL_CONTACT"};
        void Logged(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        Picker picker;IDialogueView panelView;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter6");heard.Clear();log.Clear();picker=null;panelView=null;
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

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Hoarfell(ContentRegistry.Book book)
        {
            // The ways out that wait on a story flag, and what a refused press must name.
            book.Lock("hoar.charted","chart");          // Many Hands' hull, the warp to the Shear: "No lane to Hoarfell yet: Sela has to read it off the chart on the lamp gallery"
            book.Lock("hoar.idraHailed","cutter");      // the Shear's way down: "The way down through the ring is the striped cutter's: follow it"
            book.Lock("hoar.planTaken","Idra");         // Lastlight's Reach gate: "... until there's a plan: hear Idra Sorn's at the carving yard"
            // Hask's first talk: his counter, or later (docs/writing/Hoarfell.yarn).
            book.Branch("HaskFirst","shop","world");
        }

        // ------------------------------------------------------------------ the Shear's lane, as the builder lays it (HoarfellBuilder.ShearZone: editor code a test can't call)
        static readonly Vector2[] Lane={new(0,0),new(20,260),new(-30,560),new(30,860),new(0,1160),new(-20,1420),new(-20,1500)};
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
        static Vector3 Beside(float s,float starboard){var p=Along(s,out var h)+new Vector2(h.y,-h.x)*starboard;return new Vector3(p.x,1,p.y);}
        const float HailS=200,PodS=1020,PodSide=60,LandS=1470,LandSide=-20;
        // Lastlight's places, on the plan (docs/maps/Lastlight.md; HoarfellBuilder.TownZone's constants).
        static readonly Vector2 TownArrival=new(0,4),Tern=new(8,60.5f),Hask=new(-25,26.5f),IdraYard=new(-12,48),IruShrine=new(44,44),IruHall=new(5.2f,59.6f),ReachGate=new(-63.5f,42),
            PensCrossing=new(-20,25);
        static readonly Vector2[] HighLamps={new(47.5f,53.8f),new(52,63),new(60,68)};

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
        /// <summary>The story at the Shear's door: Hoarfell charted from Many Hands' chart and the lane the next step, so arriving reports it.</summary>
        static void ToTheShear(){Flag("hoar.charted");State.questSteps["LongSleep"]=0;}
        /// <summary>The story at Lastlight's door: Idra has hailed and towed, and the town is the next step.</summary>
        static void Landed(){ToTheShear();foreach(string f in new[]{"hoar.idraHailed","hoar.towed"})Flag(f);State.questSteps["LongSleep"]=2;}
        /// <summary>Lastlight with Tern heard: the next step is Idra at the carving yard, through the pens.</summary>
        static void Heard(){Landed();foreach(string f in new[]{"visited.Lastlight","hoar.arrived","hoar.ribsSeen","met.Tern","hoar.ternHeard"})Flag(f);State.questSteps["LongSleep"]=4;}
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
        static Vector3 Floor(Vector2 at)
        {
            Assert.IsTrue(NavMesh.SamplePosition(new Vector3(at.x,8,at.y),out var hit,9,NavMesh.AllAreas),$"fixture: no floor near ({at.x}, {at.y})");
            return hit.position;
        }
        static IEnumerator Settle()
        {
            float deadline=Time.unscaledTime+40;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(DialogueSystem.Current.Running,"a conversation never ended");
            yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Talk(Npc npc){DialogueSystem.Current.AutoAdvance=true;Assert.IsTrue(npc.Available,npc.speaker+" can't be talked to");npc.Interact();yield return null;yield return Settle();}
        static IEnumerator Discover(DiscoveryPoint point){DialogueSystem.Current.AutoAdvance=true;Assert.IsTrue(point.Available,$"\"{point.prompt}\" isn't offered");point.Interact();yield return null;yield return Settle();}
        static Npc Here(string speaker)
        {
            var here=All<Npc>().Where(n=>n.speaker==speaker&&n.gameObject.activeInHierarchy).ToArray();
            Assert.LessOrEqual(here.Length,1,speaker+" stands in two places at once");return here.FirstOrDefault();
        }
        static DiscoveryPoint Point(string flag)=>All<DiscoveryPoint>().Single(p=>p.flag==flag);
        static AuroraCurrent Current(string name)=>All<AuroraCurrent>().SingleOrDefault(c=>c.name==name);
        static string Heading(ShopUi shop)=>shop.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name=="Heading").Select(t=>t.text).FirstOrDefault()??"";
        /// <summary>Flies the lead ship across a crossing line as a player would: from nine metres short of it, along the lane, until a conversation starts or it is nine metres past.</summary>
        static IEnumerator FlyAcross(DiscoveryPoint line)
        {
            DialogueSystem.Current.AutoAdvance=true;
            var ahead=line.transform.forward;ahead.y=0;ahead.Normalize();var at=line.transform.position;at.y=1;
            Put(Lead,at-ahead*9);yield return null;
            float deadline=Time.unscaledTime+5;
            while(!DialogueSystem.Current.Running&&Vector3.Dot(Lead.transform.position-at,ahead)<9&&Time.unscaledTime<deadline){Lead.motor.Move(new Vector2(ahead.x,ahead.z),false,false);yield return null;}
            if(Lead.motor is FlightMotor ship)ship.Halt();
            yield return Settle();
        }
        /// <summary>A whole path over the town's floor from the arrival to here.</summary>
        static bool Reached(Vector2 to)
        {
            var path=new NavMeshPath();
            return NavMesh.SamplePosition(Floor(TownArrival),out var from,1,NavMesh.AllAreas)&&NavMesh.SamplePosition(Floor(to),out var goal,2.5f,NavMesh.AllAreas)
                &&NavMesh.CalculatePath(from.position,goal.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        }
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
        /// <summary>Flies the lead along a direction for a while at the stick's own pace, and says how far it got and what its Thrust did.</summary>
        static IEnumerator Fly(Vector3 from,Vector3 direction,float seconds,System.Action<float,float> done)
        {
            Put(Lead,from);Lead.thrust=50;yield return null;direction.y=0;direction.Normalize();
            float until=Time.time+seconds;var start=Lead.transform.position;
            while(Time.time<until){Lead.motor.Move(new Vector2(direction.x,direction.z),false,false);yield return null;}
            if(Lead.motor is FlightMotor ship)ship.Halt();
            done(Vector3.Dot(Lead.transform.position-start,direction),Lead.thrust);
        }

        // ================================================================== Many Hands: the way in
        [UnityTest] public IEnumerator TheChartReadsHoarfellAndTheHullsWarpTakesThePartyIntoTheShear()
        {
            Assert.IsTrue(Has("chapter5.complete"),"fixture: chapter 6's loadout hasn't finished chapter 5");Assert.AreEqual(-1,Step("LongSleep"),"fixture: the chapter's quest has begun already");
            yield return Zone("ManyHands");
            var chart=All<DiscoveryPoint>().SingleOrDefault(p=>p.flag=="hoar.charted");
            Assert.IsNotNull(chart,"Many Hands' chart table has nothing to read for Hoarfell (HoarfellBuilder.StageApproach not run since the city's last build?)");
            Assert.AreEqual("SelaHoarfellChart",chart.dialogueNode);
            Put(Lead,chart.transform.position+Vector3.back*.8f);yield return Frames(2);
            yield return Discover(chart);
            Assert.AreEqual("SelaHoarfellChart",heard.LastOrDefault());Assert.IsTrue(Has("hoar.charted"),"reading the chart didn't chart Hoarfell");
            Assert.AreEqual(0,Step("LongSleep"),"reading the chart didn't start the chapter at the lane to the Shear");
            Assert.IsFalse(chart.Available,"the chart offers Hoarfell again once read");
            yield return Zone("ManyHandsHull");
            var warp=All<WarpBeacon>().SingleOrDefault(w=>w.scene=="AuroraShear");Assert.IsNotNull(warp,"the hull has no warp to the Aurora Shear");
            StringAssert.Contains("Aurora Shear",warp.prompt);Assert.AreEqual("hoar.charted",warp.requiredFlag);
            Put(Lead,warp.transform.position+Vector3.back*3);yield return Frames(3);
            warp.Interact();yield return Arrive("AuroraShear");
            Assert.AreEqual(1,Step("LongSleep"),"arriving in the Shear didn't move the story on to the currents");
            Assert.Less(Flat(Lead.transform.position,new Vector2(0,6)),5,"the warp didn't come out at the Shear's arrival");
        }

        // ================================================================== the Aurora Shear
        [UnityTest] public IEnumerator ACurrentCarriesAShipWithItAndFillsItsThrustAndHoldsItBackAndDrainsItAgainstIt()
        {
            Landed();yield return Zone("AuroraShear");
            var fast=Current("The fast lane");Assert.IsNotNull(fast,"the Shear has no fast lane");
            Assert.GreaterOrEqual(All<AuroraCurrent>().Length,5,"the Shear's lane isn't a sequence of currents");
            // The middle of the fast lane, between two of its points, and the way it flows there.
            var at=(fast.path[1]+fast.path[2])*.5f;at.y=1;var flow=fast.Flow(at,out float off,out float strength);
            Assert.Less(off,1,"fixture: not in the middle of the fast lane");Assert.AreEqual(1,strength,.01f,"fixture: the fast lane doesn't carry at its middle");
            // The same flight in the same place with the current switched off: what the ship does by itself.
            float still=0,with=0,against=0,thrustWith=0,thrustAgainst=0,thrustStill=0;
            fast.enabled=false;yield return Fly(at,flow,1.5f,(d,t)=>{still=d;thrustStill=t;});fast.enabled=true;yield return null;
            yield return Fly(at,flow,1.5f,(d,t)=>{with=d;thrustWith=t;});
            Assert.AreEqual(AuroraCurrent.Riding.With,AuroraCurrent.Of(Lead,out var current),"flying the fast lane's way doesn't count as riding it");Assert.AreSame(fast,current);
            StringAssert.Contains("WITH YOU",CurrentGauge.Words(AuroraCurrent.Riding.With));
            Assert.IsNotNull(Object.FindFirstObjectByType<CurrentGauge>(),"the HUD has no current gauge");
            yield return Fly(at,-flow,1.5f,(d,t)=>{against=d;thrustAgainst=t;});
            Assert.AreEqual(AuroraCurrent.Riding.Against,AuroraCurrent.Of(Lead,out _),"flying against the fast lane doesn't count as against it");
            TestContext.WriteLine($"1.5 s at the stick's pace: by itself {still:0.0} m (Thrust {thrustStill:0}), with the fast lane {with:0.0} m (Thrust {thrustWith:0}), against it {against:0.0} m (Thrust {thrustAgainst:0})");
            Assert.Greater(with,still*1.4f,"riding with the fast lane is no faster than the ship flies by itself");
            Assert.Less(against,still*.6f,"flying against the fast lane is barely slower than the ship flies by itself");
            Assert.Greater(thrustWith,thrustStill+10,"riding with a current doesn't fill the ship's Thrust faster");
            Assert.Less(thrustAgainst,50,"flying against a current doesn't drain the ship's Thrust");
            Assert.IsTrue(log.Any(l=>l.StartsWith("CURRENT_AGAINST The fast lane")),"the ship's turn against the current wasn't said");
        }

        [UnityTest] public IEnumerator ACurrentHoldsAShipWhereItIsWhileAConversationHoldsTheControls()
        {
            // Idra's call about the tender is five lines. In the first player run the short lane carried the party 87 m in the 12.4 s they took (7 m a second, its push),
            // through the tender's fight and out the far side, with the controls held the whole way (Builds/quality/nov13b/s32, frames 108 to 124).
            Landed();yield return Zone("AuroraShear");
            var lane=Current("The short lane");Assert.IsNotNull(lane,"the Shear has no short lane");
            var at=(lane.path[1]+lane.path[2])*.5f;at.y=1;var flow=lane.Flow(at,out _,out float strength);
            Assert.AreEqual(1,strength,.01f,"fixture: the short lane doesn't carry at its middle");
            var input=GameServices.Current.Input;bool was=input.Blocked;
            try
            {
                Put(Lead,at);yield return null;input.Blocked=true;var start=Lead.transform.position;
                for(float until=Time.time+1.5f;Time.time<until;){input.Blocked=true;yield return null;}
                float talking=Vector3.Distance(Lead.transform.position,start);
                Assert.Less(talking,.5f,$"a ship in the short lane was carried {talking:0.0} m in a second and a half of conversation");
                Assert.AreNotEqual(AuroraCurrent.Riding.None,AuroraCurrent.Of(Lead,out _),"held, the ship no longer counts as in the current: the gauge would go out for the talk");
                // The controls back, the same current carries it on.
                input.Blocked=false;start=Lead.transform.position;
                for(float until=Time.time+1.5f;Time.time<until;)yield return null;
                float carried=Vector3.Dot(Lead.transform.position-start,flow.normalized);
                Assert.Greater(carried,lane.push*1.5f*.7f,$"with the controls back the short lane carried the ship {carried:0.0} m in a second and a half");
            }
            finally{input.Blocked=was;}
        }

        [UnityTest] public IEnumerator IdrasHailTurnsTheFirstCurrentAndHerCutterTowsThePartyIntoHerLane()
        {
            ToTheShear();yield return Zone("AuroraShear");
            Assert.AreEqual(1,Step("LongSleep"),"arriving in the Shear didn't move the story on to the currents");
            var first=Current("The first current");Assert.IsNotNull(first,"the Shear has no first current");Assert.AreEqual(1,first.Sense,"the first current runs the wrong way before Idra hails");
            Assert.AreEqual(AuroraCurrent.Riding.With,AuroraCurrent.Judge(Vector3.forward,first.Flow(new Vector3(first.path[0].x,1,first.path[0].z+20),out _,out _)),"the arrival's current doesn't run the party's way");
            var hail=Point("hoar.idraHailed");Assert.AreEqual("IdraShearHail",hail.dialogueNode);Assert.IsTrue(hail.startsOnCrossing,"Idra's hail can be flown round");
            Assert.Less(Flat(hail.transform.position,Beside(HailS,0)),2,"Idra's hail isn't where the map has it");
            // The story's arrow on this step is the line where the current turns.
            yield return Frames(3);var goal=Object.FindFirstObjectByType<ObjectiveHud>().Goal;Assert.IsTrue(goal.HasValue,"the objective points at nothing on the currents' step");
            Assert.Less(Flat(goal.Value,Beside(HailS,0)),4,$"the objective points at {goal}, not at Idra's hail");
            yield return FlyAcross(hail);
            Assert.AreEqual(1,heard.Count(h=>h=="IdraShearHail"),"Idra's hail didn't play as the ship crossed its line: heard "+string.Join(", ",heard));
            Assert.IsTrue(Has("hoar.idraHailed"));Assert.AreEqual(2,Step("LongSleep"),"Idra's hail didn't move the story on to following her down");
            float deadline=Time.unscaledTime+6;while(first.Sense>-.99f&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual(-1,first.Sense,.01f,"the first current didn't turn round once Idra had hailed");
            deadline=Time.unscaledTime+30;while(!Has("hoar.towed")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(log.Contains("TOW_START"),"Idra's cutter never put a line on the party");Assert.IsTrue(Has("hoar.towed"),"the tow never let the party go");
            yield return Frames(3);
            AuroraCurrent.Of(Lead,out var into);Assert.IsNotNull(into,"the tow let the party go outside every current");
            Assert.AreEqual("Idra's lane",into.name,"the tow let the party go somewhere other than Idra's lane");
            // A save after the tow loads with the first current turned and the tow over.
            yield return SaveQuitAndContinue();
            Assert.AreEqual(-1,Current("The first current").Sense,.01f,"the first current came back running north after a Continue");
            var tow=All<CutterTow>().Single();Assert.IsFalse(tow.Towing,"the tow started again after a Continue");
        }

        /// <summary>Where the two ships are held on Idra's line, on the plan: from the first current's width across her hail (where a ship riding it is held through her call) to the
        /// first place behind the cutter, then along her tow, the lead <see cref="CutterTow.lineLength"/> astern and the partner three more and four to starboard (CutterTow's own
        /// spacing), every half metre.</summary>
        static List<(Vector3 lead,Vector3 partner)> TowWay(CutterTow tow)
        {
            var way=new List<(Vector3,Vector3)>();
            (Vector3,Vector3) Behind(Vector3 cutter,Vector3 heading){heading.y=0;heading.Normalize();return (cutter-heading*tow.lineLength,cutter-heading*(tow.lineLength+3)+Vector3.Cross(Vector3.up,heading)*4);}
            var (lead0,partner0)=Behind(tow.tow[0],tow.tow[1]-tow.tow[0]);
            for(float side=-20;side<=0;side+=2)
            {
                var held=Beside(HailS,side);
                for(float t=0;t<=1;t+=.02f)way.Add((Vector3.Lerp(held,lead0,t),Vector3.Lerp(held+Vector3.back*4,partner0,t)));
            }
            for(int i=1;i<tow.tow.Length;i++)
            {
                var heading=tow.tow[i]-tow.tow[i-1];float leg=heading.magnitude;
                for(float s=0;s<=leg;s+=.5f)way.Add(Behind(tow.tow[i-1]+heading*(s/leg),heading));
            }
            return way;
        }
        /// <summary>Every shard a ship on Idra's line comes within its hull and a margin of, with where.</summary>
        static List<string> Grazed(List<(Vector3 lead,Vector3 partner)> way,IEnumerable<BoxCollider> ice,float leadHull,float partnerHull,float margin)
        {
            var found=new List<string>();
            foreach(var shard in ice)
                foreach(var (lead,partner) in way)
                {
                    float toLead=Flat(shard.ClosestPoint(new Vector3(lead.x,shard.bounds.center.y,lead.z)),lead),toPartner=Flat(shard.ClosestPoint(new Vector3(partner.x,shard.bounds.center.y,partner.z)),partner);
                    if(toLead<leadHull+margin){found.Add($"the lead passes {toLead:0.0} m from the shard at {shard.bounds.center} at {lead}");break;}
                    if(toPartner<partnerHull+margin){found.Add($"the partner passes {toPartner:0.0} m from the shard at {shard.bounds.center} at {partner}");break;}
                }
            return found;
        }
        /// <summary>The first player run's tow (November 14, Builds/quality/nov13b/s32-rejected-current-a and -b): the currents had carried the party 160 m north through Idra's call, and
        /// the tow dragged both hulls back south through an ice shard to reach her line (WALL_CONTACT at (19.0, 261.7) and (15.3, 261.5) between TOW_START and TOW_DONE). With the ship
        /// held through a conversation (D275), the way the line takes both ships is from where the first current crosses her hail to her lane: no shard may stand within a hull and a
        /// margin of it, and riding the first current into her hail as a player does, the call holds the ship and the tow takes both hulls to her lane touching nothing.</summary>
        [UnityTest] public IEnumerator RidingTheFirstCurrentIntoIdrasHailTheShipWaitsOutHerCallAndHerTowClearsEveryShard()
        {
            ToTheShear();yield return Zone("AuroraShear");
            var tow=All<CutterTow>().Single();var way=TowWay(tow);var ice=All<BoxCollider>().Where(b=>b.name=="Ice shard").ToArray();
            Assert.Greater(ice.Length,40,"fixture: the Shear's ring has its shards");
            var party=PartyController.Current;var partner=party.members.First(m=>m!=Lead);
            float leadHull=HeroCollision.HullRadius(Lead.character),partnerHull=HeroCollision.HullRadius(partner.character);const float Margin=1.5f;
            // The check built wrong first: a shard set on the line is found.
            var planted=new GameObject("Ice shard").AddComponent<BoxCollider>();planted.transform.position=Vector3.Lerp(tow.tow[1],tow.tow[2],.5f);planted.size=new Vector3(3,6,3);Physics.SyncTransforms();
            Assert.IsNotEmpty(Grazed(way,new[]{planted},leadHull,partnerHull,Margin),"fixture: the check doesn't see a shard standing on Idra's line");
            Object.Destroy(planted.gameObject);yield return null;
            var grazed=Grazed(way,ice,leadHull,partnerHull,Margin);
            Assert.IsEmpty(grazed,"a shard stands within a hull and "+Margin+" m of the way Idra's line takes the ships:\n"+string.Join("\n",grazed));
            // As a player meets it: up the first current at its own middle, the partner behind, both at the stick's pace, until her call begins.
            DialogueSystem.Current.AutoAdvance=true;
            var first=Current("The first current");var line=first.Flow(Beside(HailS,-11),out _,out _);Assert.AreEqual(1,first.Sense,"fixture: the first current runs north before she hails");
            Put(Lead,Beside(HailS-26,-10.5f));Put(partner,Beside(HailS-30,-12));yield return null;
            var ahead=line.normalized;float deadline=Time.unscaledTime+8;
            while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline){foreach(var ship in party.members)ship.motor.Move(new Vector2(ahead.x,ahead.z),false,false);yield return null;}
            Assert.IsTrue(DialogueSystem.Current.Running,"riding the first current up to her line, Idra never hailed");
            var heldAt=Lead.transform.position;float drifted=0;
            for(deadline=Time.unscaledTime+40;DialogueSystem.Current.Running&&Time.unscaledTime<deadline;){drifted=Mathf.Max(drifted,Flat(Lead.transform.position,heldAt));yield return null;}
            Assert.IsFalse(DialogueSystem.Current.Running,"her call never ended");
            Assert.Less(drifted,1,$"her call carried the ship {drifted:0.0} m while it held the controls");
            deadline=Time.unscaledTime+30;while(!Has("hoar.towed")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(Has("hoar.towed"),"the tow never let the party go");
            int from=log.IndexOf("TOW_START"),to=log.IndexOf("TOW_DONE");Assert.IsTrue(from>=0&&to>from,"fixture: the tow said when it began and ended: "+string.Join(", ",log));
            var scraped=log.Skip(from).Take(to-from).Where(l=>l.StartsWith("WALL_CONTACT")).ToArray();
            Assert.IsEmpty(scraped,"Idra's tow dragged a hull into something: "+string.Join("; ",scraped));
            yield return Frames(3);AuroraCurrent.Of(Lead,out var into);Assert.IsNotNull(into,"the tow let the party go outside every current");Assert.AreEqual("Idra's lane",into.name);
        }

        /// <summary>A RectTransform's box on the screen, in pixels.</summary>
        static Rect OnScreen(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);var canvas=rect.GetComponentInParent<Canvas>().rootCanvas;
            var eye=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            Vector2 a=RectTransformUtility.WorldToScreenPoint(eye,corners[0]),b=RectTransformUtility.WorldToScreenPoint(eye,corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y));
        }
        /// <summary>The first player run (November 14): the current gauge stood under the vitals, where the pickup feed writes every kill's loot and "Saved", and its longest words
        /// wrapped onto its own bar. The lead moved it low on the left and cut its words to one line (UI/CurrentGauge.cs). Here it is in the Shear with the feed full: it is up, it
        /// is clear of the feed's lines and band, the vitals, the skill row and the prompt's plate, and every thing it can say is one line that fits inside its frame and above its bar.</summary>
        [UnityTest] public IEnumerator TheCurrentGaugeSitsLowOnTheLeftClearOfTheFeedAndSaysEachThingOnOneLine()
        {
            Landed();yield return Zone("AuroraShear");
            var fast=Current("The fast lane");var at=(fast.path[1]+fast.path[2])*.5f;at.y=1;Put(Lead,at);
            foreach(string id in new[]{"ScrapAlloy","RimeGlass","ChargeCell","scrip"})Lattice.Rpg.Inventory.Announce(id,2);
            GameServices.Current.Saves.Save("autosave",State);yield return Frames(4);
            var gauge=All<RectTransform>().FirstOrDefault(r=>r.name=="CurrentGauge");Assert.IsNotNull(gauge,"the HUD has no current gauge");
            Assert.IsTrue(gauge.gameObject.activeInHierarchy,"in the fast lane the current gauge isn't up");
            var frame=OnScreen(gauge);var faults=new List<string>();
            var others=All<RectTransform>().Where(r=>r.gameObject.activeInHierarchy&&(r.name=="PickupLine"||r.name=="PickupBand"||r.name=="VitalsFrame"||r.name=="SkillRow"||r.name=="InteractionPlate")).ToArray();
            Assert.GreaterOrEqual(others.Count(r=>r.name=="PickupLine"),4,"fixture: the pickup feed isn't writing");
            foreach(var other in others){var box=OnScreen(other);if(box.Overlaps(frame))faults.Add($"the gauge {frame} lies over {other.name} {box}");}
            Assert.Less(frame.center.x,Screen.width*.5f,"the gauge isn't on the left");Assert.Less(frame.center.y,Screen.height*.5f,"the gauge isn't low on the screen");
            var words=gauge.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="CurrentLabel");var bar=OnScreen(gauge.Find("CurrentBack") as RectTransform);
            foreach(AuroraCurrent.Riding riding in System.Enum.GetValues(typeof(AuroraCurrent.Riding)))
            {
                string said=CurrentGauge.Words(riding);words.text=said;words.ForceMeshUpdate();
                var ink=words.textBounds;var lo=words.rectTransform.TransformPoint(ink.min);var hi=words.rectTransform.TransformPoint(ink.max);
                var canvas=words.canvas.rootCanvas;var eye=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                Vector2 a=RectTransformUtility.WorldToScreenPoint(eye,lo),b=RectTransformUtility.WorldToScreenPoint(eye,hi);var inked=Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y));
                if(words.textInfo.lineCount!=1)faults.Add($"\"{said}\" runs to {words.textInfo.lineCount} lines");
                if(inked.xMin<frame.xMin||inked.xMax>frame.xMax||inked.yMax>frame.yMax)faults.Add($"\"{said}\" ({inked}) runs out of the gauge's frame {frame}");
                if(inked.Overlaps(bar))faults.Add($"\"{said}\" ({inked}) is written over the gauge's bar {bar}");
                TestContext.WriteLine($"HOARFELL_GAUGE \"{said}\" at {words.fontSize:0} pt, {inked.width:0} px wide in a {frame.width:0} px frame");
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        /// <summary>What the game's camera draws, saved for the eye (in the test mirror's Builds/quality/focused/hoarfell), and handed back to be measured.</summary>
        static Texture2D Still(string name)
        {
            const int W=1280,H=720;var camera=Camera.main;Assert.IsNotNull(camera,"fixture: the game has a camera");
            string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/focused/hoarfell"));System.IO.Directory.CreateDirectory(folder);
            var image=new Texture2D(W,H,TextureFormat.RGB24,false);var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var active=RenderTexture.active;var target=camera.targetTexture;
            try{rt.Create();camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,name+".png"),image.EncodeToPNG());}
            finally{camera.targetTexture=target;RenderTexture.active=active;rt.Release();Object.Destroy(rt);}
            TestContext.WriteLine("HOARFELL_STILL "+System.IO.Path.Combine(folder,name+".png"));return image;
        }
        /// <summary>How much of a box round a body on the screen is burnt out to white: what a glare has done to whoever stands there.</summary>
        static float BurntOut(Texture2D image,Vector3 body)
        {
            var camera=Camera.main;var at=camera.WorldToViewportPoint(body+Vector3.up*1);int cx=Mathf.RoundToInt(at.x*image.width),cy=Mathf.RoundToInt(at.y*image.height);
            int white=0,all=0;
            for(int x=cx-50;x<=cx+50;x++)for(int y=cy-70;y<=cy+70;y++)
            {
                if(x<0||y<0||x>=image.width||y>=image.height)continue;var c=image.GetPixel(x,y);all++;
                if(c.r>.96f&&c.g>.96f&&c.b>.96f)white++;
            }
            return all>0?white/(float)all:0;
        }
        /// <summary>Nothing that fights on foot may come to rest where a hero on the floor can't strike it. On the 17:17 build both of the third rank's Bridle-hounds ended in the gap
        /// between Rig three's north-east face and the third level's north wall (DriverBoundsTests.TheFrozenHostsThirdRankIsFoughtToItsEnd found them at (148.2, 235.4), one on the
        /// other's back), while the lead swung at them round the rig's corner for two minutes and the fight never cleared (nov14-recon24 s35). A gap a hound fits into (1.2 m) and a hero
        /// can't stand beside it in (under 2.2 m: a hero's width, a hound's and a swing's room) is a pocket. Here no face of the dig's three rigs or the hall's three couplings has one:
        /// each stands against what is beside it or clear of it.</summary>
        [UnityTest] public IEnumerator NoThawRigLeavesAGapAHoundFitsInAndAHeroCantFightIn()
        {
            Flag("hoar.reachEntered");yield return Zone("FrozenHost");
            const float HoundFits=1.2f,HeroFights=2.2f;
            var props=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Rig one"||t.name=="Rig two"||t.name=="Rig three"||t.name=="Coupling body").ToArray();
            Assert.AreEqual(6,props.Length,"fixture: the dig's three rigs and the hall's three couplings are built: "+string.Join(", ",props.Select(p=>p.name)));
            var pockets=new List<string>();int looked=0;
            foreach(var prop in props)
            {
                var box=prop.GetComponentsInChildren<BoxCollider>(true).FirstOrDefault(b=>!b.isTrigger);Assert.IsNotNull(box,"fixture: "+prop.name+" is solid");
                var frame=box.transform;var size=Vector3.Scale(box.size,frame.lossyScale);var centre=frame.TransformPoint(box.center);float foot=centre.y-size.y*.5f;
                foreach(var (normal,across,half,span) in new[]{(frame.forward,frame.right,size.z*.5f,size.x),(-frame.forward,frame.right,size.z*.5f,size.x),(frame.right,frame.forward,size.x*.5f,size.z),(-frame.right,frame.forward,size.x*.5f,size.z)})
                    for(float k=-.45f;k<=.451f;k+=.075f)
                    {
                        // Out from the face at a hound's middle, along the floor: what stands beside it, and how far off.
                        var from=new Vector3(centre.x,foot+.65f,centre.z)+normal*(half+.02f)+across*(span*k);looked++;
                        // A stretch of face something solid already stands against has no gap (and a ray started inside a solid doesn't see it).
                        if(Physics.OverlapSphere(from+normal*.1f,.05f,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(prop)&&!(c is CharacterController)))continue;
                        // Not ice that melts (a Membrane: a plug, a rank's block): the rig's own thaw takes it before the rig's fight is fought.
                        var hits=Physics.RaycastAll(from,normal,HeroFights+.5f,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.collider.transform.IsChildOf(prop)&&!(h.collider is CharacterController)&&h.collider.GetComponentInParent<Membrane>()==null)
                            .OrderBy(h=>h.distance).ToArray();
                        if(hits.Length==0)continue;float gap=hits[0].distance;
                        if(gap>=HoundFits&&gap<HeroFights)pockets.Add($"{prop.name}: {gap:0.00} m to '{hits[0].collider.name}' from ({from.x:0.0}, {from.z:0.0})");
                    }
            }
            Assert.Greater(looked,100,"fixture: the rigs' faces were looked along");
            Assert.IsEmpty(pockets,"a gap a hound fits into and a hero can't fight in, between a rig and what stands beside it:\n"+string.Join("\n",pockets.Take(20)));
        }
        /// <summary>The first player run's Host Marshal (Builds/quality/nov14-recon5/r36, stills 056 and 061): behind each thaw rig a white glare covered whoever stood there, which is where
        /// a coupling is broken from, once that rig's third of the wall had gone back to sleep (and so behind all three in the cold). The light a refrozen stretch brings stood inside
        /// its own ice. Here a hero stands at a rig, on its wall side and in front of it, with the wall thawing and then asleep: nobody is burnt out. And the hall's camera-side rim
        /// (D272, the Walking Canopy's fault) is looked at with a hero at its foot.</summary>
        [UnityTest] public IEnumerator AHeroAtAThawRigCanBeSeenWithTheWallThawingOrAsleep()
        {
            Landed();foreach(string f in new[]{"visited.Lastlight","hoar.arrived","hoar.ribsSeen","hoar.ternHeard","hoar.pensSeen","hoar.planTaken","hoar.reachEntered","hoar.rig1","hoar.rig2","hoar.rig3","hoar.slateRead","hoar.idraJoined"})Flag(f);
            yield return Zone("FrozenHost","Door");
            var party=PartyController.Current;var partner=party.members.First(m=>m!=Lead);
            var centre=All<EncounterVolume>().Single(v=>v.encounterId=="HostMarshal").transform.position;
            var faults=new List<string>();
            IEnumerator Stand(string name,Vector3 lead,Vector3 beside,bool measure)
            {
                Vector3 On(Vector3 at){Assert.IsTrue(NavMesh.SamplePosition(new Vector3(at.x,centre.y,at.z),out var hit,5,NavMesh.AllAreas),$"fixture: no floor near {at}");return hit.position;}
                Put(Lead,On(lead));Put(partner,On(beside));
                for(float until=Time.unscaledTime+1.5f;Time.unscaledTime<until;)yield return null;
                var image=Still(name);float burnt=BurntOut(image,Lead.transform.position);Object.Destroy(image);
                TestContext.WriteLine($"HOARFELL_GLARE {name}: {burnt:P0} of the box round the lead burnt out to white");
                if(measure&&burnt>.15f)faults.Add($"{name}: {burnt:P0} of the lead is burnt out to white");
            }
            foreach(bool asleep in new[]{false,true})
            {
                if(asleep){ThawCoupling.SettleAll();yield return Frames(5);}
                foreach(var coupling in ThawCoupling.All.OrderBy(c=>c.index))
                {
                    var rig=coupling.transform.position;var outward=rig-centre;outward.y=0;outward.Normalize();var across=Vector3.Cross(Vector3.up,outward);
                    yield return Stand($"rig-{coupling.index}-{(asleep?"asleep":"thawing")}-wall-side",rig+outward*2.2f,rig-outward*2.6f+across*1.5f,true);
                    yield return Stand($"rig-{coupling.index}-{(asleep?"asleep":"thawing")}-front",rig-outward*2.2f,rig-outward*4.6f+across*1.5f,true);
                }
            }
            // The camera's side of the hall: its south rim, the foot of the ice at bearings 150 to 210, seen with the lead at it. Looked at, not measured.
            foreach(float bearing in new[]{150f,180f,210f})
            {
                var rim=centre+new Vector3(Mathf.Sin(bearing*Mathf.Deg2Rad),0,Mathf.Cos(bearing*Mathf.Deg2Rad))*16.5f;
                yield return Stand($"south-rim-{bearing:0}",rim,centre+(rim-centre)*.8f+Vector3.right*2,false);
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        [UnityTest] public IEnumerator TheWayDownWaitsForIdraAndLandsAtLastlight()
        {
            ToTheShear();yield return Zone("AuroraShear");
            var land=All<WarpBeacon>().Single(w=>w.scene=="Lastlight");Assert.Less(Flat(land.transform.position,Beside(LandS,LandSide)),2,"the way down isn't at the ring's gap");
            StringAssert.Contains("cutter",land.lockedPrompt,"the shut way down doesn't say what opens it");
            Put(Lead,land.transform.position+Vector3.back*3);yield return Frames(3);
            land.Interact();yield return Frames(3);Assert.IsFalse(SceneFlow.Current.Loading,"the way down opened before Idra had hailed");
            Flag("hoar.idraHailed");Flag("hoar.towed");State.questSteps["LongSleep"]=2;
            land.Interact();yield return Arrive("Lastlight");
            Assert.AreEqual(3,Step("LongSleep"),"landing at Lastlight didn't move the story on to Old Tern");
            Assert.Less(Flat(Lead.transform.position,TownArrival),4,"the way down didn't land at Lastlight's landing");
            Assert.IsTrue(Has("visited.Lastlight"),"arriving didn't chart Lastlight");
            float deadline=Time.unscaledTime+3;while(!Has("hoar.arrived")&&Time.unscaledTime<deadline)yield return null;Assert.IsTrue(Has("hoar.arrived"),"landing at Lastlight didn't say so");
        }

        [UnityTest] public IEnumerator IdrasDownedCutterIsOfferedOnlyForHerQuestAndItsPodIsTowedIn()
        {
            Landed();yield return Zone("AuroraShear");
            var pod=Point("hoar.podFound");Assert.AreEqual("Tow the crew pod",pod.prompt);Assert.AreEqual("IdraCutterFound",pod.dialogueNode);Assert.AreEqual("CrewPod",pod.report);
            Assert.Less(Flat(pod.transform.position,Beside(PodS,PodSide)),2,"the downed cutter isn't under the ice shelf where the map has it");
            Put(Lead,pod.transform.position+Vector3.back*2);yield return Frames(3);
            Assert.IsFalse(pod.Available,"the crew pod can be towed before Idra has asked anyone to");
            State.questSteps["CutterDown"]=0;yield return Frames(2);Assert.IsTrue(pod.Available,"the crew pod isn't offered with Idra's quest asking for it");
            yield return Discover(pod);
            Assert.AreEqual("IdraCutterFound",heard.LastOrDefault());Assert.IsTrue(Has("quest.CutterDown.complete"),"towing the pod in didn't finish Cutter Down");
            var lamp=All<LampRhythm>().OrderBy(l=>Flat(l.transform.position,pod.transform.position)).First();yield return Frames(2);Assert.IsTrue(lamp.Dark,"the pod's lamp still blinks once it is towed");
        }

        // ================================================================== Lastlight
        [UnityTest] public IEnumerator OldTernSitsUnderTheFourthRibAndSendsThePartyToThePens()
        {
            Landed();Flag("visited.Lastlight");State.questSteps["LongSleep"]=3;yield return Zone("Lastlight");
            var tern=Here("Tern");Assert.IsNotNull(tern,"Old Tern isn't in Lastlight");Assert.Less(Flat(tern.transform.position,Tern),1,"Old Tern isn't at her hall under the fourth rib");
            var body=tern.transform.Find("Generated civilian");Assert.IsNotNull(body,"Old Tern has no body");Assert.Less(body.localPosition.y,-.2f,"Old Tern is standing: she is seated in every scene (lowered into her hides)");
            yield return Frames(3);var goal=Object.FindFirstObjectByType<ObjectiveHud>().Goal;Assert.IsTrue(goal.HasValue&&Flat(goal.Value,Tern)<3,$"the objective points at {goal}, not at Old Tern");
            var pens=Point("hoar.pensSeen");Assert.IsFalse(pens.Available,"Idra and Hask's argument starts before Tern has sent the party to it");
            Assert.IsNull(Here("Idra"),"Idra is at the carving yard before the party has met her at the pens");
            yield return Talk(tern);Assert.AreEqual("TernFirst",heard.LastOrDefault(),"Tern's first talk isn't TernFirst");
            Assert.IsTrue(Has("hoar.ternHeard"));Assert.AreEqual(4,Step("LongSleep"),"Tern's talk didn't move the story on to Idra's plan");
            yield return Talk(tern);Assert.AreEqual("TernRepeat",heard.LastOrDefault(),"Tern tells it twice");
            var tally=Point("hoar.tallySeen");Assert.AreEqual("Look at the cuts in the rib",tally.prompt);Put(Lead,tally.transform.position+Vector3.back*.6f);yield return Frames(2);
            yield return Discover(tally);Assert.AreEqual("SelaRibTally",heard.LastOrDefault());
        }

        [UnityTest] public IEnumerator IdraArguesAtThePensThenPlansAtTheCarvingYardAndTheReachGateWaitsForThePlan()
        {
            Heard();yield return Zone("Lastlight");
            var gate=All<WarpBeacon>().Single(w=>w.scene=="FrozenHost");Assert.Less(Flat(gate.transform.position,ReachGate),2,"the Reach gate isn't on the west side");
            StringAssert.Contains("Idra",gate.lockedPrompt,"the shut Reach gate doesn't say who has the plan");
            Put(Lead,gate.transform.position+Vector3.right*2.4f);yield return Frames(3);gate.Interact();yield return Frames(3);
            Assert.IsFalse(SceneFlow.Current.Loading,"the Reach gate opened before Idra's plan");
            var arguing=All<LeavesAfter>().SingleOrDefault(l=>l.flag=="hoar.pensSeen");Assert.IsNotNull(arguing,"Idra isn't at the pens arguing with Hask");Assert.IsTrue(arguing.gameObject.activeInHierarchy);
            Assert.Less(Flat(Here("Hask").transform.position,Hask),1,"Hask isn't at the pens");
            // Walking up to the pens from the commons starts the argument.
            var pens=Point("hoar.pensSeen");Assert.AreEqual("IdraPens",pens.dialogueNode);Assert.IsTrue(pens.startsOnCrossing&&pens.Available,"the pens' argument isn't waiting to be walked into");
            DialogueSystem.Current.AutoAdvance=true;Put(Lead,Floor(PensCrossing+new Vector2(6,0)));yield return Frames(2);Put(Lead,Floor(PensCrossing));
            float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;yield return Settle();
            Assert.AreEqual(1,heard.Count(h=>h=="IdraPens"),"walking to the pens didn't bring Idra and Hask's argument: heard "+string.Join(", ",heard));
            Assert.IsTrue(Has("hoar.pensSeen"));Assert.AreEqual(0,Step("CutterDown"),"Taren's offer at the pens didn't start Cutter Down");
            deadline=Time.unscaledTime+30;while(arguing.gameObject.activeInHierarchy&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(arguing.gameObject.activeInHierarchy,"Idra never left the pens for the carving yard");
            var idra=Here("Idra");Assert.IsNotNull(idra,"Idra isn't at the carving yard after the pens");Assert.Less(Flat(idra.transform.position,IdraYard),1);
            Put(Lead,Floor(IdraYard+new Vector2(0,-1.6f)));yield return Frames(2);
            yield return Talk(idra);Assert.AreEqual("IdraPlan",heard.LastOrDefault(),"Idra's first talk at the yard isn't her plan");
            Assert.IsTrue(Has("hoar.planTaken"));Assert.AreEqual(5,Step("LongSleep"),"the plan didn't move the story on to the Reach and the dig");
            yield return Talk(idra);Assert.AreEqual("IdraRepeat",heard.LastOrDefault(),"Idra says the plan twice");
            Assert.IsTrue(Has(gate.requiredFlag),"the plan is taken and the Reach gate is still shut");
            // Once she has gone ahead onto the Reach, the yard is empty.
            Flag("hoar.reachEntered");yield return Frames(3);Assert.IsNull(Here("Idra"),"Idra is still at the carving yard once she has gone onto the Reach");
        }

        [UnityTest] public IEnumerator HaskOpensHisCounterAndTheBathHouseCradleAndTheCarvingBenchServeTheParty()
        {
            Heard();yield return Zone("Lastlight");var shop=Object.FindFirstObjectByType<ShopUi>();
            var hask=Here("Hask");yield return Choose(hask,"trade");Assert.AreEqual("HaskFirst",heard.FirstOrDefault(),"Hask's first conversation isn't his first meeting");
            Assert.IsTrue(shop.IsOpen,"Hask's 'See what you trade' didn't open his counter");StringAssert.Contains("HASK",Heading(shop),"Hask opened someone else's counter");
            var stock=GameCatalog.Find<ShopInventory>("Lastlight");Assert.IsNotNull(stock,"Lastlight has no counter of its own");Assert.Greater(ShopUi.Parts(stock).Count(),0,"Hask sells no parts");
            CollectionAssert.DoesNotContain(ShopUi.Stock(stock,State).Select(o=>o.id).ToArray(),"HostAlloy","the dig's alloy is on Hask's counter before the Marshal sleeps");
            shop.Close();yield return Frames(2);
            var bench=All<Bench>().SingleOrDefault();Assert.IsNotNull(bench,"Lastlight has no bench");Assert.IsTrue(bench.Available,"the carving yard's bench can't be used");
            var cradle=All<RepairBay>().SingleOrDefault();Assert.IsNotNull(cradle,"Lastlight has no repair cradle");StringAssert.Contains("repair and save",cradle.prompt);
            var spawn=All<SpawnPoint>().SingleOrDefault(s=>s.id==cradle.saveSpawn);Assert.IsNotNull(spawn,$"the cradle saves at '{cradle.saveSpawn}', which Lastlight hasn't got");
            Assert.Less(Flat(spawn.transform.position,cradle.transform.position),10,"the cradle's save puts the party far from it");
            Put(Lead,cradle.transform.position+Vector3.up*.2f);yield return Frames(2);cradle.Interact();yield return Frames(2);Assert.AreEqual(cradle.saveSpawn,State.spawn,"the cradle didn't save the party's place");
            var savedAt=spawn.transform.position;
            yield return SaveQuitAndContinue();
            Assert.AreEqual("Lastlight",State.zone);Assert.Less(Flat(Lead.transform.position,savedAt),4,"Continue didn't come back where the cradle saved");
            Assert.AreEqual(4,Step("LongSleep"),"the story's place was lost to a save");
        }

        [UnityTest] public IEnumerator TheThreeHighLampsAreAClimbUpTheRibHallsAndEachLightsWhenLit()
        {
            Heard();foreach(string f in new[]{"met.Iru"})Flag(f);State.questSteps["ShrineLamps"]=0;yield return Zone("Lastlight");
            var lamps=All<DiscoveryPoint>().Where(p=>p.report=="SkullLamp").OrderBy(p=>p.flag).ToArray();Assert.AreEqual(3,lamps.Length,"the shrine hasn't three high lamps to light");
            float floor=Floor(TownArrival).y;
            for(int n=0;n<3;n++)
            {
                Assert.Less(Flat(lamps[n].transform.position,HighLamps[n]),1.5f,$"high lamp {n+1} isn't where the map has it");
                Assert.Greater(lamps[n].transform.position.y-floor,2,$"high lamp {n+1} isn't high: it is a climb away");
                Assert.IsTrue(Reached(HighLamps[n]),$"high lamp {n+1} can't be climbed to from the landing");
                Assert.AreEqual("Light the high lamp",lamps[n].prompt);
            }
            for(int n=0;n<3;n++)
            {
                var lit=All<FlagGate>().Single(g=>g.name=="High lamp "+(n+1)+" lights").targets[0];Assert.IsFalse(lit.activeInHierarchy,$"high lamp {n+1} is lit before anyone has climbed to it");
                Put(Lead,Floor(HighLamps[n]));yield return Frames(2);Assert.IsTrue(lamps[n].Available,$"high lamp {n+1} isn't offered with Iru's quest asking for it");
                lamps[n].Interact();yield return Frames(3);Assert.IsTrue(Has(lamps[n].flag),$"lighting high lamp {n+1} didn't take");Assert.IsTrue(lit.activeInHierarchy,$"high lamp {n+1} didn't light");
            }
            Assert.AreEqual(1,Step("ShrineLamps"),"three lamps lit and the quest isn't waiting on Iru");
            var iru=Here("Iru");Assert.Less(Flat(iru.transform.position,IruShrine),1,"Iru isn't at the shrine");
            yield return Talk(iru);Assert.AreEqual("IruLampsReturn",heard.LastOrDefault(),"Iru's talk once the lamps are lit isn't her thanks");Assert.IsTrue(Has("quest.ShrineLamps.complete"));
        }

        [UnityTest] public IEnumerator IruSpeaksUpInTernsHallOnceTheLampsAreLitAndTheLongSleepIsTold()
        {
            Heard();foreach(string f in new[]{"met.Iru","met.Hask","quest.ShrineLamps.complete","hoar.idraJoined","bossdown.HostMarshal","hoar.marshalAsleep","hoar.sleepLearned"})Flag(f);
            State.questSteps["LongSleep"]=8;yield return Zone("Lastlight");
            var iru=Here("Iru");Assert.IsNotNull(iru,"Iru is nowhere once the lamps are lit and the long sleep told");Assert.Less(Flat(iru.transform.position,IruHall),1,"Iru isn't standing in Tern's hall to speak up");
            yield return Talk(iru);Assert.AreEqual("IruSpeaksUp",heard.LastOrDefault(),"Iru's talk in Tern's hall isn't her speaking up");Assert.IsTrue(Has("iru.spokeUp"));
            yield return Frames(3);iru=Here("Iru");Assert.IsNotNull(iru);Assert.Less(Flat(iru.transform.position,IruShrine),1,"Iru didn't go back to the shrine once she had spoken");
            yield return Talk(iru);Assert.AreEqual("IruRepeat",heard.LastOrDefault(),"Iru's word after speaking up isn't her last tier");
            yield return Talk(Here("Tern"));Assert.AreEqual("TernAfter",heard.LastOrDefault(),"Tern's word once she has told the long sleep isn't her last");
            yield return Talk(Here("Hask"));Assert.AreEqual("HaskOwes",heard.LastOrDefault(),"Hask's first word once the Marshal sleeps isn't what he owes");
        }

        [UnityTest] public IEnumerator LastlightIsLivedInByTheKeptInSeveralCoatsWhoStandOffTheWaysAndNeverTwoAlikeSideBySide()
        {
            Heard();yield return Zone("Lastlight");
            var standing=All<PropTint>().Where(t=>t.name.StartsWith("Townsperson")||t.name.StartsWith("A child")).ToArray();
            var walking=All<WalkLoop>();
            Assert.GreaterOrEqual(standing.Length+walking.Length,15,"Lastlight has fewer than fifteen residents about");
            Assert.GreaterOrEqual(walking.Length,6,"most of Lastlight stands about: fewer than six residents walk an errand");
            string Coat(Component who){var model=who.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Generated civilian");var r=model!=null?model.GetComponentInChildren<SkinnedMeshRenderer>():null;
                return r!=null&&r.sharedMaterial!=null?r.sharedMaterial.name:"?";}
            var coats=standing.Select(s=>(at:s.transform.position,coat:Coat(s))).ToArray();
            Assert.GreaterOrEqual(coats.Select(c=>c.coat).Distinct().Count(),3,"Lastlight's people wear fewer than three coats: "+string.Join(", ",coats.Select(c=>c.coat).Distinct()));
            for(int a=0;a<coats.Length;a++)for(int b=a+1;b<coats.Length;b++)
                if(Flat(coats[a].at,coats[b].at)<3.2f)Assert.AreNotEqual(coats[a].coat,coats[b].coat,$"two residents side by side at {coats[a].at} and {coats[b].at} wear the same coat");
            foreach(var named in new[]{"Tern","Hask","Iru"})
            {
                var npc=Here(named);Assert.IsNotNull(npc,named+" isn't in Lastlight");
                foreach(var (at,_) in coats)Assert.Greater(Flat(at,npc.transform.position),1.4f,$"a resident stands on top of {named}");
            }
            Assert.GreaterOrEqual(All<Transform>().Count(t=>t.name=="Rime-beast"),7,"the pens hold fewer than seven beasts");
        }

        // ================================================================== the join
        [UnityTest] public IEnumerator IdraJoinsAsTheThirdHeroAndTheGameStillPlays()
        {
            // The story at the bottom gallery: the plan taken, the Reach crossed, the dig walked down; her join is the step the story waits on.
            Heard();foreach(string f in new[]{"met.Idra","hoar.planTaken","hoar.reachEntered","hoar.herdMet","hoar.whiteoutMet","hoar.vessaHeard","hoar.ranksSeen","hoar.offerHeard","hoar.slateRead"})Flag(f);
            State.questSteps["LongSleep"]=5;
            Assert.AreEqual(2,State.party.Count,"fixture: chapter 6 doesn't arrive as two");
            yield return Zone("Arena_Ground");
            var lead=Lead.character;
            DialogueSystem.Current.AutoAdvance=true;Assert.IsTrue(DialogueSystem.Current.StartNode("IdraJoins","Idra"),"the join's conversation wouldn't start");
            yield return null;yield return Settle();yield return Frames(5);
            Assert.AreEqual(3,State.party.Count,"after IdraJoins the party isn't three: "+string.Join(", ",State.party.Select(m=>m.id)));
            Assert.IsTrue(State.party.Exists(m=>m.id=="Idra"),"Idra isn't in the party after she joins");
            Assert.IsTrue(log.Contains("PARTY_JOIN Idra"),"the join wasn't said");
            Assert.IsTrue(Has("hoar.idraJoined"));Assert.AreEqual(6,Step("LongSleep"),"her joining didn't move the story on to the Host Marshal");
            // She comes out beside the lead, who still leads; whoever made room rests with a body nowhere.
            var party=PartyController.Current;Assert.AreEqual(2,party.members.Length,"more than two heroes stand on the field");
            Assert.AreEqual(lead,Lead.character,"her joining changed who leads");
            Assert.IsTrue(party.members.Any(m=>m.character=="Idra"),"Idra didn't come out where she joined");
            Assert.AreEqual(1,Deployment.Resting(State).Count,"nobody is resting with three in the party");
            // And the game still plays: she can be taken to the lead and swung with, and a blow lands.
            var settle=Time.unscaledTime+8;var idra=party.members.First(m=>m.character=="Idra");var form=idra.GetComponent<FormController>();
            while(form.Current!=ZoneController.Current.Form&&Time.unscaledTime<settle)yield return null;
            Assert.IsTrue(party.Swap(),"the party can't swap to Idra");Assert.AreEqual("Idra",Lead.character,"swapping didn't put Idra in the lead");
            foreach(var actor in party.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            var target=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Gloomgrazer"),Lead.transform.position+Lead.transform.forward*1.6f);Assert.IsNotNull(target,"fixture: no enemy to strike");
            var health=target.GetComponent<Health>();float before=health.integrity;
            for(int k=0;k<10&&health.integrity>=before;k++){Lead.Attack();yield return new WaitForSecondsRealtime(.35f);}
            Assert.Less(health.integrity,before,"Idra's blows don't land: the game doesn't play with her in the lead");
            Object.Destroy(target.gameObject);yield return Frames(2);
            // Three come back from a save in the town, with the pair as it was left.
            var pair=Deployment.Of(State).Select(m=>m.id).OrderBy(id=>id).ToArray();
            yield return Zone("Lastlight");Assert.AreEqual(2,PartyController.Current.members.Length,"three bodies walk the town");
            yield return SaveQuitAndContinue();
            Assert.AreEqual(3,State.party.Count,"a Continue after the join lost a hero");
            CollectionAssert.AreEqual(pair,Deployment.Of(State).Select(m=>m.id).OrderBy(id=>id).ToArray(),"a Continue changed who is out");
        }
    }
}
