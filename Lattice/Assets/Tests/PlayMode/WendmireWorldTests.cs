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
    /// <summary>Chapter 4's first two places as a player meets them, in the built scenes: the Spore Veil (Maru's hail, the beacons keeping Driftreed's count and the dead one, the off-lane
    /// beacons, the swarms a lit ship draws and a dark one slips past, the tide-gate that waits for the dead beacon) and Driftreed (the stern line, Ona's terms, the tide that swings the rope
    /// walks and opens the mire gate, the two counters, the town's people, Driftreed's decision and Abri's call), the story's steps through both, and a save and a Continue in each. What every
    /// zone gets from the shared checks is theirs and not repeated here. Coordinates are the map briefs' (docs/maps/SporeVeil.md, Driftreed.md), which are the builder's own.</summary>
    public sealed class WendmireWorldTests
    {
        readonly List<string> heard=new(),log=new();
        void Talked(string node,string speaker,System.Action<bool> done)=>heard.Add(node);
        void Looked(DiscoveryPoint point){if(!string.IsNullOrEmpty(point.dialogueNode))heard.Add(point.dialogueNode);}
        static readonly string[] Heeded={"ROPE_HAULED","TIDE_TURNED","SWARM_DRAWN","DISCOVERY","CACHE_OK"};
        void Logged(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        Picker picker;IDialogueView panelView;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter4");heard.Clear();log.Clear();picker=null;panelView=null;
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
        [ContentRegistry.Expectations] static void Wendmire(ContentRegistry.Book book)
        {
            // The ways out of the Veil, Driftreed and Tallow's approach that wait on a story flag, and what a refused press must name.
            book.Lock("wend.charted","Pell","lantern");      // Tallow Approach's warp to the Veil: "No lane to Wendmire yet. Pell's lantern has to learn it first."
            book.Lock("wend.beaconRelit","relight");         // the tide-gate: "The tide-gate stays shut while a beacon in the lane is dark: relight it"
            book.Lock("wend.tideTurned","Maru");             // Driftreed's mire gate: "... until the tide turns: ask Maru Wend at the pilots' stage"
            // Where each choice of the chapter's nodes with choices leads (docs/writing/Wendmire.yarn): Ona's three questions are talk; Dov's first is his counter or leave, his repeat
            // gossip, counter or leave; Quill's three are the press, the frame and his parts at every tier.
            book.Branch("OnaRepeat","world","world","world");book.Branch("DovFirst","shop","world");book.Branch("DovRepeat","world","shop","world");
            foreach(string node in new[]{"QuillFirst","QuillRepeat","QuillPumpOffer","QuillPumpReturn","QuillPost"})book.Branch(node,"bench","repair","shop");
        }

        // ------------------------------------------------------------------ the Veil's lane, as the builder lays it (WendmireBuilder.VeilLane, VeilBeside: editor code a test can't call)
        static readonly Vector2[] Lane={new(0,0),new(-40,260),new(30,560),new(150,820),new(140,1080),new(60,1300),new(40,1560),new(36,1620)};
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
        const float Beacon1=40,Beacon2=520,Beacon3=1000,DeadBeacon=1380,GateS=1560;
        static readonly (float s,float side)[] Pockets={(300,-56),(1150,58)};
        /// <summary>The swarm field: three rows of three across the lane (WendmireBuilder.Field).</summary>
        static readonly (float s,float side)[] FieldAt={(615,-46),(611,2),(619,50),(760,-52),(756,-4),(764,44),(905,-44),(901,4),(909,52)};
        /// <summary>Half the lane's width (WendmireBuilder.VeilHalf).</summary>
        const float LaneHalf=72;
        // Driftreed's places, on the plan (docs/maps/Driftreed.md; WendmireBuilder.Driftreed's ReedArrival, MaruAtStage and the rest).
        static readonly Vector2 TownArrival=new(0,8.5f),MaruStage=new(9.5f,13.5f),LineAt=new(13.4f,10.6f),Notice=new(-6,39.5f),DovSmokehouse=new(19.5f,45),QuillRaft=new(-34.5f,46),
            OnaHall=new(-.5f,82.2f),PimBench=new(10.5f,82),School=new(36.5f,55.5f),MireGate=new(48.2f,99.4f),MireSpawn=new(45.5f,92.2f),AbriCall=new(0,26),LineMoored=new(22.6f,9.5f);

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
        /// <summary>The story at the Veil's door: Wendmire charted and the warp the next step, so arriving reports it.</summary>
        static void ToTheVeil(){Flag("wend.charted");State.questSteps["ReedRoad"]=1;}
        /// <summary>The story at Driftreed's door: the dead beacon relit and the landing the next step.</summary>
        static void Landed(){Flag("wend.charted");foreach(string f in new[]{"wend.maruHailed","wend.beacon1","wend.beacon2","wend.beacon3","wend.beaconRelit"})Flag(f);State.questSteps["ReedRoad"]=3;}
        /// <summary>Driftreed with the line held and Ona heard: the next step is Maru and the tide.</summary>
        static void Heard(){Landed();foreach(string f in new[]{"wend.arrived","wend.arrivalSeen","wend.lineHeld","met.Maru","met.Ona","wend.onaHeard"})Flag(f);State.questSteps["ReedRoad"]=5;}
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
            Assert.IsTrue(NavMesh.SamplePosition(new Vector3(at.x,1,at.y),out var hit,2.5f,NavMesh.AllAreas),$"fixture: no floor near ({at.x}, {at.y})");
            return hit.position;
        }
        static IEnumerator Settle()
        {
            float deadline=Time.unscaledTime+30;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
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
        static EncounterVolume Fight(string id)=>All<EncounterVolume>().SingleOrDefault(e=>e.encounterId==id);
        static LootCache Cache(string id)=>All<LootCache>().SingleOrDefault(c=>c.cacheId==id);
        static Vector3? Goal()=>Object.FindFirstObjectByType<ObjectiveHud>().Goal;
        static string Heading(ShopUi shop)=>shop.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name=="Heading").Select(t=>t.text).FirstOrDefault()??"";
        /// <summary>Flies the lead ship across a crossing line as a player would: from nine metres short of it, along the lane, until a conversation starts or it is nine metres past.</summary>
        static IEnumerator FlyAcross(DiscoveryPoint line)
        {
            DialogueSystem.Current.AutoAdvance=true;
            var ahead=line.transform.forward;ahead.y=0;ahead.Normalize();var at=line.transform.position;at.y=1;
            Put(Lead,at-ahead*9);yield return null;
            float deadline=Time.unscaledTime+5;
            while(!DialogueSystem.Current.Running&&Vector3.Dot(Lead.transform.position-at,ahead)<9&&Time.unscaledTime<deadline){Lead.motor.Move(new Vector2(ahead.x,ahead.z),false,false);yield return null;}
            // Stopped where it is, so it doesn't coast on into the next line along the lane.
            if(Lead.motor is FlightMotor ship)ship.Halt();
            yield return Settle();
        }
        /// <summary>Is there a whole path over the town's floor from the arrival to here, with the tide's seals as they stand.</summary>
        static bool Reached(Vector2 to)
        {
            var path=new NavMeshPath();
            return NavMesh.SamplePosition(Floor(TownArrival),out var from,1,NavMesh.AllAreas)&&NavMesh.SamplePosition(new Vector3(to.x,0,to.y),out var goal,2.5f,NavMesh.AllAreas)
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

        // ================================================================== the Spore Veil
        [UnityTest] public IEnumerator EveryBeaconKeepsDriftreedsCountAndTheGateWaitsForTheDeadOne()
        {
            ToTheVeil();yield return Zone("SporeVeil");
            Assert.AreEqual(2,Step("ReedRoad"),"arriving in the Veil didn't move the story on to the beacons");
            var lamps=All<LampRhythm>();
            LampRhythm At(Vector3 point)=>lamps.OrderBy(l=>Flat(l.transform.position,point)).First();
            foreach(float s in new[]{Beacon1,Beacon2,Beacon3})
            {
                var beacon=At(Beside(s,-12));Assert.Less(Flat(beacon.transform.position,Beside(s,-12)),2,$"there is no beacon {s} m along the lane, where the map has one");
                Assert.AreEqual(LampRhythm.Count.TwoShortOneLong,beacon.Keeping,$"the beacon {s} m along doesn't keep Driftreed's count");Assert.IsFalse(beacon.Dark,$"the beacon {s} m along is dark");
            }
            var dead=At(Beside(DeadBeacon,-12));Assert.IsTrue(dead.Dark,"the dead beacon is lit before anyone has relit it");
            for(int n=0;n<2;n++)Assert.AreEqual(LampRhythm.Count.ShortLongShort,At(Beside(Pockets[n].s,Pockets[n].side)).Keeping,$"off-lane beacon {n+1} doesn't blink the wrong count it has slipped to");
            // The count itself: two short and a long, a lure never holds a long.
            float Lit(LampRhythm.Count count,float from,float to){float on=0;for(float t=from;t<to;t+=.01f)on+=LampRhythm.At(count,t);return on*.01f;}
            Assert.AreEqual(.22f+.22f+.95f,Lit(LampRhythm.Count.TwoShortOneLong,0,3.1f),.05f,"two short and one long isn't two short flashes and a long one");
            Assert.AreEqual(.95f+.95f+.22f,Lit(LampRhythm.Count.LongLongShort,0,3.1f),.05f,"long, long, short isn't two long flashes and a short one");
            float longest=0,run=0;for(float t=0;t<20;t+=.01f){run=LampRhythm.At(LampRhythm.Count.Flicker,t,7)>0?run+.01f:0;longest=Mathf.Max(longest,run);}
            Assert.Less(longest,.6f,"a lure's flicker held a light as long as a beacon's long flash");
            // The lane's three beacons are passed, each once, as the ship flies by.
            for(int n=1;n<=3;n++)
            {
                var line=Point("wend.beacon"+n);Assert.IsTrue(line.startsOnCrossing,$"beacon {n} isn't passed by flying across its line");
                yield return FlyAcross(line);Assert.IsTrue(Has("wend.beacon"+n),$"flying past beacon {n} didn't count it");
            }
            // The tide-gate: shut until the dead beacon is relit, and says so.
            var gate=All<WarpBeacon>().Single(w=>w.scene=="Driftreed");Assert.Less(Flat(gate.transform.position,Beside(GateS,0)),2,"the tide-gate isn't at the lane's end");
            StringAssert.Contains("relight",gate.lockedPrompt,"the shut tide-gate doesn't say what opens it");
            gate.Interact();yield return Frames(3);Assert.IsFalse(SceneFlow.Current.Loading,"the tide-gate opened with the dead beacon still dark");
        }

        [UnityTest] public IEnumerator MarusHailPlaysOnceJustInsideTheFirstBeaconAndHerLegCallsComeOverTheFlying()
        {
            ToTheVeil();yield return Zone("SporeVeil");
            var hail=Point("wend.maruHailed");Assert.AreEqual("MaruVeilHail",hail.dialogueNode);Assert.IsTrue(hail.startsOnCrossing,"Maru's hail can be flown round");
            Assert.Less(Flat(hail.transform.position,Beside(52,0)),2,"Maru's hail isn't just inside the first beacon");
            yield return FlyAcross(hail);Assert.AreEqual(1,heard.Count(h=>h=="MaruVeilHail"),"Maru's hail didn't play as the ship crossed its line: heard "+string.Join(", ",heard));
            yield return FlyAcross(hail);Assert.AreEqual(1,heard.Count(h=>h=="MaruVeilHail"),"Maru's hail played twice");
            var calls=All<RadioHail>().Where(r=>r.speaker=="Maru").ToArray();
            foreach(string situation in new[]{"hail.pods","hail.swarm","hail.ray","hail.relit","hail.gate"})Assert.IsTrue(calls.Any(c=>c.situations.Contains(situation)),$"nothing on the lane has Maru say {situation}");
            Assert.IsTrue(calls.Single(c=>c.situations.Contains("hail.relit")).needs.Contains("wend.beaconRelit"),"Maru's word on the relit beacon doesn't wait for it to be relit");
        }

        [UnityTest] public IEnumerator RelightingTheDeadBeaconLightsItOpensTheTideGateAndMovesTheStoryToDriftreed()
        {
            ToTheVeil();yield return Zone("SporeVeil");
            var point=Point("wend.beaconRelit");Assert.AreEqual("Relight the dark beacon",point.prompt);Assert.AreEqual("MaruDeadBeacon",point.dialogueNode);
            Assert.Less(Flat(point.transform.position,Beside(DeadBeacon,-12)),2,"the dead beacon's lamp isn't where the map has it");
            // The story's arrow on this step is the dead beacon itself.
            yield return Frames(3);var goal=Goal();Assert.IsTrue(goal.HasValue,"the objective points at nothing on the beacons' step");
            Assert.Less(Flat(goal.Value,Beside(DeadBeacon,-12)),3,$"the objective points at {goal}, not at the dead beacon");
            var dead=All<LampRhythm>().OrderBy(l=>Flat(l.transform.position,Beside(DeadBeacon,-12))).First();Assert.IsTrue(dead.Dark,"fixture: the dead beacon is lit already");
            yield return Discover(point);
            Assert.AreEqual("MaruDeadBeacon",heard.LastOrDefault());Assert.IsTrue(Has("wend.beaconRelit"),"setting the dead beacon didn't relight it");
            Assert.AreEqual(3,Step("ReedRoad"),"relighting the dead beacon didn't move the story on to Driftreed");
            yield return Frames(2);Assert.IsFalse(dead.Dark,"the dead beacon stays dark once relit");Assert.AreEqual(LampRhythm.Count.TwoShortOneLong,dead.Keeping,"the relit beacon doesn't keep Driftreed's count");
            float deadline=Time.unscaledTime+8;while(!Has("wend.hailRelit")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(Has("wend.hailRelit"),"Maru says nothing on the radio once the beacon is relit");
            var gate=All<WarpBeacon>().Single(w=>w.scene=="Driftreed");Put(Lead,gate.transform.position+Vector3.back*2);yield return Frames(3);
            gate.Interact();yield return Arrive("Driftreed");
            Assert.AreEqual(4,Step("ReedRoad"),"landing in Driftreed didn't move the story on to the stern line and Ona");
            Assert.Less(Flat(Lead.transform.position,TownArrival),4,"the tide-gate didn't land on the pilots' stage");
        }

        [UnityTest] public IEnumerator TheRaysFarPassLeavesTheDeadBeaconOpenLongEnoughToRelightIt()
        {
            ToTheVeil();yield return Zone("SporeVeil");
            var point=Point("wend.beaconRelit");var lamp=point.transform.position;var hunt=Fight("Veil_Ray");Assert.IsNotNull(hunt,"the Veil has no ray round the dead beacon");
            // Watched from out of its reach: the lead well to port of its circuit, the ray awake on its loop.
            Put(Lead,Beside(DeadBeacon,-62));hunt.Begin();yield return new WaitForSecondsRealtime(.6f);
            var ray=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).SingleOrDefault(b=>b.definition!=null&&b.definition.id=="VeilRay");Assert.IsNotNull(ray,"the ray never came out");
            // A ship at the lamp (within the prompt's reach of it) is safe while the ray is further from the lamp than its run-in plus that reach.
            float runIn=ray.definition.attacks[0].range,safe=runIn+point.range,clear=0,longest=0,nearest=float.MaxValue,furthest=0;
            float until=Time.time+18;
            while(Time.time<until)
            {
                float d=Flat(ray.transform.position,lamp);nearest=Mathf.Min(nearest,d);furthest=Mathf.Max(furthest,d);
                clear=d>safe?clear+Time.deltaTime:0;longest=Mathf.Max(longest,clear);yield return null;
            }
            TestContext.WriteLine($"the ray's loop comes {nearest:0} m from the lamp and goes {furthest:0} m off; clear of a ship at the lamp {longest:0.0} s at a stretch");
            Assert.Less(nearest,8,"the ray's near pass doesn't come by the lamp: there is nothing to time");
            Assert.GreaterOrEqual(longest,5,$"the ray is clear of a ship at the lamp for {longest:0.0} s at most: no far pass long enough to fly in and relight it");
            // On the far pass: in to the lamp and relight it, the ray still awake on its loop.
            float wait=Time.time+18;while(Flat(ray.transform.position,lamp)<safe+8&&Time.time<wait)yield return null;
            var by=lamp+(Beside(DeadBeacon,-18)-Beside(DeadBeacon,-12)).normalized*4;by.y=1;Put(Lead,by);yield return Frames(2);
            yield return Discover(point);Assert.IsTrue(Has("wend.beaconRelit"),"the dead beacon couldn't be relit with the ray awake on its far pass");
        }

        [UnityTest] public IEnumerator ALitShipDrawsTheSleepingSwarmsNearItAndADarkOneSlipsBetweenThem()
        {
            ToTheVeil();Flag("wend.maruHailed");yield return Zone("SporeVeil");
            var field=All<GlowField>().SingleOrDefault();Assert.IsNotNull(field,"the Veil has no rule that light draws the swarms");
            var clouds=FieldAt.Select((f,k)=>Fight("Field_"+k)).ToArray();Assert.IsTrue(clouds.All(c=>c!=null),"the swarm field isn't nine hanging swarms in three rows");
            for(int k=0;k<clouds.Length;k++){Assert.Less(Flat(clouds[k].transform.position,Beside(FieldAt[k].s,FieldAt[k].side)),2,$"swarm {k} isn't where the map hangs it");Assert.IsNotNull(clouds[k].previewPoint,$"swarm {k} isn't on show before it wakes");}
            CollectionAssert.IsSubsetOf(clouds,field.drawn,"the rule doesn't know the field's swarms");
            // Eighteen metres from the first cloud, toward the lane's middle, dark: clear of its cloud, inside a lit ship's reach, and nothing wakes.
            var between=Beside(FieldAt[0].s,FieldAt[0].side+18);
            Assert.Greater(Flat(between,clouds[0].transform.position),12,"fixture: the gap isn't clear of the first cloud");Assert.Less(Flat(between,clouds[0].transform.position),field.reach,"fixture: the first cloud is out of a lit ship's reach");
            Put(Lead,between);var glow=ShipGlow.Of(Lead);glow.Set(0);
            yield return new WaitForSecondsRealtime(2.4f);
            Assert.IsTrue(clouds.All(c=>!c.Started),"a ship flying dark between the clouds woke a swarm: "+string.Join(", ",clouds.Where(c=>c.Started).Select(c=>c.encounterId)));
            var gauge=Object.FindFirstObjectByType<GlowGauge>();Assert.IsNotNull(gauge,"the HUD has no glow gauge");
            StringAssert.Contains("DARK",GlowGauge.Words(glow),"a ship two seconds unlit doesn't read as dark on the gauge");
            // Lit, the same ship draws the two clouds within its light's reach and nothing further off.
            glow.Set(1);yield return Frames(3);
            Assert.IsTrue(glow.IsLit,"fixture: the ship isn't lit");StringAssert.Contains("LIT",GlowGauge.Words(glow),"a lit ship doesn't say so on the gauge");
            for(int k=0;k<clouds.Length;k++)
            {
                bool near=Flat(Lead.transform.position,clouds[k].transform.position)<=field.reach;
                Assert.AreEqual(near,clouds[k].Started,near?$"swarm {k} is {Flat(Lead.transform.position,clouds[k].transform.position):0} m from a lit ship and didn't wake":$"swarm {k} woke {Flat(Lead.transform.position,clouds[k].transform.position):0} m from the ship");
            }
            Assert.GreaterOrEqual(log.Count(l=>l.StartsWith("SWARM_DRAWN Field_")),1,"no swarm was drawn by the light");
            // Firing lights the ship by itself: the ship's own shots, not a fixture's number.
            glow.Set(0);for(int k=0;k<8;k++){Lead.Attack();yield return new WaitForSecondsRealtime(.2f);}
            Assert.Greater(glow.Level,.3f,"eight shots left the ship's glow at "+glow.Level);
        }

        [UnityTest] public IEnumerator AShipFlownDarkThreadsTheFieldWithItsPartnerAndNoLineAcrossARowIsOutOfALitShipsReach()
        {
            ToTheVeil();Flag("wend.maruHailed");yield return Zone("SporeVeil");
            var field=All<GlowField>().Single();var clouds=FieldAt.Select((f,k)=>Fight("Field_"+k)).ToArray();
            // On the map: across each row no point of the lane is further than a lit ship's light carries from a swarm, so flown lit there is no going round; and between the
            // swarms there is a way wide enough to fly dark, well clear of either cloud.
            foreach(var row in FieldAt.GroupBy(f=>Mathf.RoundToInt(f.s/50)))
            {
                var sides=row.Select(f=>f.side).OrderBy(x=>x).ToArray();float furthest=Mathf.Max(sides[0]+LaneHalf,LaneHalf-sides[sides.Length-1]);
                for(int i=1;i<sides.Length;i++){furthest=Mathf.Max(furthest,(sides[i]-sides[i-1])*.5f);Assert.GreaterOrEqual(sides[i]-sides[i-1]-12,24,$"the row at {row.First().s} leaves no dark way between {sides[i-1]} and {sides[i]}");}
                Assert.LessOrEqual(furthest,field.reach-2,$"a lit ship can cross the row at {row.First().s} {furthest:0} m from every swarm, out of reach of them all");
            }
            // In flight: the lead flown at the stick's own pace, never boosted and never firing, through the gaps, its partner following under her own brain, wakes nothing,
            // and is never lit.
            var partner=PartyController.Current.members.First(m=>m!=Lead);partner.GetComponent<PartnerBrain>().enabled=true;
            var glow=ShipGlow.Of(Lead);glow.Set(0);float brightest=0;
            var route=new[]{(575f,-22f),(615f,-22f),(690f,-25f),(760f,-28f),(830f,-24f),(905f,-20f),(960f,-20f)}.Select(w=>Beside(w.Item1,w.Item2)).ToArray();
            Put(Lead,route[0]);Put(partner,route[0]+Vector3.back*3);yield return Frames(3);
            foreach(var goal in route.Skip(1))
            {
                float deadline=Time.unscaledTime+40;
                while(Flat(Lead.transform.position,goal)>3&&Time.unscaledTime<deadline)
                {
                    var d=goal-Lead.transform.position;d.y=0;d.Normalize();Lead.motor.Move(new Vector2(d.x,d.z),false,false);brightest=Mathf.Max(brightest,glow.Level);yield return null;
                }
                Assert.Less(Flat(Lead.transform.position,goal),3,$"the lead didn't get to {goal} through the field");
            }
            Lead.motor.Move(Vector2.zero,false,false);
            Assert.Less(brightest,ShipGlow.Lit,"flying at the stick's own pace lit the lead's hull");
            Assert.IsTrue(clouds.All(c=>!c.Started),"a ship flown dark through the gaps, its partner with it, woke "+string.Join(", ",clouds.Where(c=>c.Started).Select(c=>c.encounterId)));
        }

        [UnityTest] public IEnumerator MarusStrayCallStartsTwoShortOneLongAndRetuningBothOffLaneBeaconsSendsThePartyBackToHer()
        {
            ToTheVeil();yield return Zone("SporeVeil");
            var stray=Point("wend.strayed");Assert.AreEqual("MaruVeilStray",stray.dialogueNode);Assert.IsTrue(stray.startsOnCrossing);
            var tunes=new[]{Point("wend.offBeacon1"),Point("wend.offBeacon2")};
            for(int n=0;n<2;n++){Assert.Less(Flat(tunes[n].transform.position,Beside(Pockets[n].s,Pockets[n].side)),2,$"off-lane beacon {n+1} isn't in its pocket");Assert.IsFalse(tunes[n].Available,$"off-lane beacon {n+1} can be retuned before Maru has asked");}
            yield return FlyAcross(stray);
            Assert.Contains("MaruVeilStray",heard,"straying toward pocket A didn't bring Maru's call");Assert.AreEqual(0,Step("BeaconKeeper"),"Maru's stray call didn't start her side quest");
            yield return FlyAcross(stray);Assert.AreEqual(1,heard.Count(h=>h=="MaruVeilStray"),"Maru's stray call played twice");
            for(int n=0;n<2;n++)
            {
                Put(Lead,tunes[n].transform.position);yield return Frames(2);
                Assert.IsTrue(tunes[n].Available,$"off-lane beacon {n+1} isn't offered with the quest asking for it");
                tunes[n].Interact();yield return Frames(3);Assert.IsTrue(Has("wend.offBeacon"+(n+1)),$"retuning off-lane beacon {n+1} didn't take");
                var lamp=All<LampRhythm>().OrderBy(l=>Flat(l.transform.position,tunes[n].transform.position)).First();
                Assert.AreEqual(LampRhythm.Count.TwoShortOneLong,lamp.Keeping,$"off-lane beacon {n+1} still blinks the wrong count once retuned");
            }
            Assert.AreEqual(1,Step("BeaconKeeper"),"both beacons retuned and the quest isn't waiting on Maru");
            // Once she has asked in town, the stray call is gone for good.
            Flag("maru.comesHeard");yield return Frames(3);Assert.IsFalse(stray.gameObject.activeInHierarchy,"Maru's stray call still waits in the lane after she has asked in town");
        }

        [UnityTest] public IEnumerator SavingInTheVeilAndContinuingKeepsTheRelitBeaconAndTheRetunedOne()
        {
            ToTheVeil();State.questSteps["BeaconKeeper"]=0;yield return Zone("SporeVeil");
            yield return Discover(Point("wend.beaconRelit"));var tune=Point("wend.offBeacon1");Put(Lead,tune.transform.position);yield return Frames(2);tune.Interact();yield return Frames(3);
            Put(Lead,Beside(1200,0));yield return Frames(2);
            yield return SaveQuitAndContinue();
            Assert.AreEqual("SporeVeil",State.zone);Assert.IsTrue(Has("wend.beaconRelit")&&Has("wend.offBeacon1"),"the relit beacon or the retuned one was lost to a save");
            var lamps=All<LampRhythm>();
            Assert.IsFalse(lamps.OrderBy(l=>Flat(l.transform.position,Beside(DeadBeacon,-12))).First().Dark,"the relit beacon came back dark");
            Assert.AreEqual(LampRhythm.Count.TwoShortOneLong,lamps.OrderBy(l=>Flat(l.transform.position,Beside(Pockets[0].s,Pockets[0].side))).First().Keeping,"the retuned beacon came back blinking the wrong count");
            Assert.IsFalse(Point("wend.beaconRelit").Available,"the dead beacon is offered again after a Continue");
        }

        // ================================================================== Driftreed
        [UnityTest] public IEnumerator MaruPutsTheLineInSelasHandsThreeHoldsSettleTheRaftAndOnlyThenOnaTalksTerms()
        {
            Landed();State.questSteps["ReedRoad"]=4;yield return Zone("Driftreed");
            var line=All<MooringHaul>().Single();var maru=Here("Maru");var ona=Here("Ona");
            Assert.AreEqual("Hold the stern line",line.prompt);Assert.Less(Flat(line.transform.position,LineAt),1,"the stern line isn't on the stage's east end");
            Assert.Less(Flat(maru.transform.position,MaruStage),1,"Maru isn't on the pilots' stage");Assert.Less(Flat(ona.transform.position,OnaHall),1,"Ona isn't at the weaving hall's door");
            Assert.IsTrue(Has("wend.arrived"),"landing in Driftreed didn't say so");
            Assert.IsFalse(line.Available,"the line is offered before Maru has put it in Sela's hands");
            yield return Talk(maru);Assert.AreEqual("MaruLineHold",heard.LastOrDefault(),"Maru says something other than her line about the line while the raft surges");
            yield return Talk(ona);Assert.AreEqual("OnaLineHold",heard.LastOrDefault(),"Ona talks terms before the line is held");
            Assert.IsFalse(Has("met.Ona"),"Ona's line about the line counted as meeting her");
            var crossing=Point("wend.arrivalSeen");Assert.AreEqual("MaruArrival",crossing.dialogueNode);Assert.IsTrue(crossing.startsOnCrossing,"Maru's call can be walked round");
            DialogueSystem.Current.AutoAdvance=true;Put(Lead,Floor(new Vector2(3,12)));yield return Frames(2);Put(Lead,Floor(new Vector2(6,12)));
            float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            yield return Settle();Assert.AreEqual(1,heard.Count(h=>h=="MaruArrival"),"walking from the pad toward the line doesn't start Maru's call: heard "+string.Join(", ",heard));
            Assert.IsTrue(line.Available,"Maru's call ended with the line in Sela's hands and the line isn't offered");
            for(int hold=1;hold<=3;hold++)
            {
                Assert.IsTrue(line.Available,$"hold {hold} isn't offered");line.Interact();
                deadline=Time.unscaledTime+6;while(line.Progress<hold/3f-.01f&&Time.unscaledTime<deadline)yield return null;yield return new WaitForSecondsRealtime(.3f);
                Assert.AreEqual(hold==3,Has("wend.lineHeld"),hold<3?$"the raft lay easy after {hold} holds":"three holds didn't settle the raft");
            }
            Assert.Less(Flat(line.island.position,LineMoored),.5f,"the raft settled somewhere other than off the stage's east end");
            Assert.IsNull(maru.holdNode,"Maru still answers with her line about the line");Assert.IsNull(ona.holdNode,"Ona still answers with her line about the line");
            yield return Talk(ona);Assert.AreEqual("OnaTerms",heard.LastOrDefault(),"Ona's first conversation once the line is held isn't her terms");
            Assert.IsTrue(Has("wend.onaHeard"));Assert.AreEqual(5,Step("ReedRoad"),"Ona's terms didn't move the story on to Maru and the tide");
            yield return Talk(ona);Assert.AreEqual("OnaRepeat",heard.LastOrDefault(),"Ona says her terms twice");
            // The story's arrow now finds Maru on the stage.
            yield return Frames(3);var goal=Goal();Assert.IsTrue(goal.HasValue,"the objective points at nothing once Ona has spoken");
            Assert.Less(Flat(goal.Value,MaruStage),3,$"the objective points at {goal}, not at Maru");
        }

        [UnityTest] public IEnumerator TheTideSwingsTwoWalksAwayAndTwoAcrossAndOnlyThenIsTheMireGateReachable()
        {
            Heard();yield return Zone("Driftreed");
            var swings=All<TideSwing>();Assert.AreEqual(4,swings.Length,"the tide doesn't move four rope walks");
            Assert.AreEqual(2,swings.Count(s=>s.arrives),"the tide doesn't bring two walks across");
            var gate=All<WarpBeacon>().Single(w=>w.scene=="DrownedWaystation");Assert.Less(Flat(gate.transform.position,MireGate),1.5f,"the mire gate isn't on the gate raft");
            StringAssert.Contains("Maru",gate.lockedPrompt,"the shut mire gate doesn't say who to ask");
            yield return new WaitForSecondsRealtime(.5f);
            foreach(var swing in swings)foreach(var end in swing.ends)Assert.AreEqual(!swing.arrives,end.Open,$"{swing.name}: an end is {(end.Open?"open":"shut")} before the tide");
            Assert.IsFalse(Reached(MireSpawn),"the gate raft can be walked to before the tide turns");
            foreach(var (place,at) in new[]{("the school raft",School),("Quill",QuillRaft),("Dov",DovSmokehouse),("Ona",OnaHall)})Assert.IsTrue(Reached(at),place+" can't be walked to before the tide turns");
            // Maru's tide talk turns it.
            yield return Talk(Here("Maru"));Assert.AreEqual("MaruTide",heard.LastOrDefault(),"Maru's talk once Ona has spoken isn't the tide");
            Assert.IsTrue(Has("wend.tideTurned"));Assert.AreEqual(6,Step("ReedRoad"),"the tide turned and the story isn't at the mire");
            Assert.AreEqual(4,log.Count(l=>l=="TIDE_TURNED wend.tideTurned"),"not every walk the tide moves answered it");
            yield return new WaitForSecondsRealtime(8.5f);
            foreach(var swing in swings)
            {
                foreach(var end in swing.ends)Assert.AreEqual(swing.arrives,end.Open,$"{swing.name}: an end is {(end.Open?"open":"shut")} after the tide");
                Assert.AreEqual(0,Mathf.DeltaAngle(swing.walk.localEulerAngles.y,swing.toYaw),1,$"{swing.name} didn't finish its swing");
            }
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsTrue(Reached(MireSpawn),"the tide has turned and the gate raft still can't be walked to");
            foreach(var (place,at) in new[]{("the school raft",School),("Quill",QuillRaft),("Dov",DovSmokehouse),("Ona",OnaHall)})Assert.IsTrue(Reached(at),place+" can't be walked to after the tide turns");
            // Across a walk the tide brought, there is a deck to stand on: the ship's body stands on it.
            var gateWalk=swings.Single(s=>s.name.StartsWith("Gate walk"));var mid=gateWalk.transform.position+gateWalk.walk.forward*11.7f+Vector3.up*3;
            Assert.IsTrue(Physics.Raycast(mid,Vector3.down,out var hit,6,~0,QueryTriggerInteraction.Ignore)&&hit.collider.transform.IsChildOf(gateWalk.transform),"the gate walk has come across and there is no deck under its middle");
            // Maru goes ahead into the mire: on a fresh load she isn't on the stage.
            yield return Zone("Driftreed");Assert.IsNull(Here("Maru"),"Maru is still on the stage after she has gone ahead into the mire");
            swings=All<TideSwing>();Assert.AreEqual(4,swings.Length,"a fresh load lost a rope walk");
            foreach(var swing in swings)Assert.AreEqual(0,Mathf.DeltaAngle(swing.walk.localEulerAngles.y,swing.toYaw),1,$"{swing.name} isn't where the tide left it on a fresh load");
            gate=All<WarpBeacon>().Single(w=>w.scene=="DrownedWaystation");Assert.IsTrue(Has(gate.requiredFlag),"the tide has turned and the mire gate is still shut");
            if(Application.CanStreamedLevelBeLoaded("DrownedWaystation")){Put(Lead,Floor(MireGate+new Vector2(-1,-3)));yield return Frames(2);gate.Interact();yield return Arrive("DrownedWaystation");}
        }

        /// <summary>Pim waits behind a grille they tied shut, and the talk that finds them opens it. The bars stood 3.9 m in front of Pim and a talk reached 3.2: from the floor
        /// in front of the grille nothing was offered, and pump three and the great hall's door wait on that talk (the routes' press check, November 13).</summary>
        [UnityTest] public IEnumerator PimCanBeSpokenToThroughTheGrilleTheyTiedShut()
        {
            Heard();foreach(string f in new[]{"wend.tideTurned","wend.lureLearned","wend.pump1","wend.pump2"})Flag(f);
            yield return Zone("DrownedWaystation");
            var pim=Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(n=>n.firstNode=="PimFound");Assert.IsNotNull(pim,"fixture: Pim in their pocket");
            Assert.IsTrue(pim.isActiveAndEnabled,"fixture: Pim hasn't been found, and waits behind the grille");
            var grille=GameObject.Find("Pim's grille");Assert.IsNotNull(grille,"fixture: the grille");
            var bars=grille.GetComponentInChildren<BoxCollider>();Assert.IsNotNull(bars,"fixture: the grille is solid");Assert.IsTrue(bars.enabled&&bars.gameObject.activeInHierarchy,"fixture: the grille is shut");
            // As near as a body gets: against the bars on the side away from Pim, at Pim's own place along them.
            var away=bars.bounds.center-pim.transform.position;away.y=0;away.Normalize();
            var hero=PartyController.Current.Active;float radius=hero.GetComponent<CharacterController>().radius;
            var face=bars.ClosestPoint(pim.transform.position+away*20);var stand=new Vector3(face.x,hero.transform.position.y,face.z)+away*(radius+.15f);
            var floor=stand+Vector3.up*3;Assert.IsTrue(Physics.Raycast(floor,Vector3.down,out var ground,8,~0,QueryTriggerInteraction.Ignore),"fixture: floor in front of the grille");
            Put(hero,ground.point+Vector3.up*.05f);yield return new WaitForSecondsRealtime(.4f);
            var gap=pim.transform.position-hero.transform.position;gap.y=0;
            Assert.AreSame(pim,PromptService.Interaction,$"standing against the grille, {gap.magnitude:0.0} m from Pim, nothing offers the talk that finds them (their reach is {pim.range:0.0} m)");
            // And from a step back, where a person stops before they touch the bars.
            Put(hero,ground.point+away*.6f+Vector3.up*.05f);yield return new WaitForSecondsRealtime(.4f);
            Assert.AreSame(pim,PromptService.Interaction,"a step back from the grille, Pim is out of reach");
        }
        [UnityTest] public IEnumerator DovAndQuillEachOpenTheirOwnCounterAndQuillsPressAndFrameWork()
        {
            Heard();yield return Zone("Driftreed");var shop=Object.FindFirstObjectByType<ShopUi>();
            var dov=Here("Dov");Assert.Less(Flat(dov.transform.position,DovSmokehouse),1,"Dov isn't at his smokehouse");
            yield return Choose(dov,"sell");Assert.AreEqual("DovFirst",heard.FirstOrDefault(),"Dov's first conversation isn't his first meeting");
            Assert.IsTrue(shop.IsOpen,"Dov's 'Show us what you sell' didn't open his counter");StringAssert.Contains("SMOKEHOUSE",Heading(shop),"Dov opened someone else's counter");
            var smoked=GameCatalog.Find<ShopInventory>("Driftreed_Dov");Assert.IsNotNull(smoked,"Dov has no counter of his own");
            var offered=ShopUi.Stock(smoked,State).Select(o=>o.id).ToArray();Assert.Greater(offered.Length,0,"Dov's counter is empty");
            CollectionAssert.DoesNotContain(offered,"SurgeDraught","Dov's smoked provisions are on his counter before his order is filled");
            shop.Close();yield return Frames(2);
            Flag("dov.orderFilled");CollectionAssert.Contains(ShopUi.Stock(smoked,State).Select(o=>o.id).ToArray(),"SurgeDraught","the order is filled and Dov's smoked provisions aren't on the counter");
            var quill=Here("Quill");Assert.Less(Flat(quill.transform.position,QuillRaft),1,"Quill isn't on his pump raft");
            yield return Choose(quill,"parts");
            Assert.IsTrue(shop.IsOpen,"Quill's 'See the parts' didn't open his counter");StringAssert.Contains("QUILL",Heading(shop),"Quill opened someone else's counter");
            var parts=GameCatalog.Find<ShopInventory>("Driftreed_Quill");Assert.IsNotNull(parts,"Quill has no counter of his own");Assert.Greater(ShopUi.Parts(parts).Count(),0,"Quill sells no parts");
            shop.Close();yield return Frames(2);
            yield return Choose(quill,"press");
            var menu=Object.FindFirstObjectByType<PauseMenu>();Assert.IsTrue(menu.IsOpen,"Quill's 'Use the pump-and-press' didn't open the press");menu.Close();yield return Frames(2);
            var bench=All<Bench>().SingleOrDefault();Assert.IsNotNull(bench,"Driftreed has no press to fabricate at");Assert.IsTrue(bench.Available,"the pump-and-press can't be used");
            var frame=All<RepairBay>().SingleOrDefault();Assert.IsNotNull(frame,"Driftreed has no mending frame");StringAssert.Contains("repair and save",frame.prompt);
            var spawn=All<SpawnPoint>().SingleOrDefault(s=>s.id==frame.saveSpawn);Assert.IsNotNull(spawn,$"the frame saves at '{frame.saveSpawn}', which Driftreed hasn't got");
            Assert.Less(Flat(spawn.transform.position,frame.transform.position),10,"the frame's save puts the party far from it");
            frame.Interact();yield return Frames(2);Assert.AreEqual(frame.saveSpawn,State.spawn,"the mending frame didn't save the party's place");
        }

        [UnityTest] public IEnumerator DriftreedIsLivedInByIluInSeveralCoatsWhoStandOffTheWaysAndNeverTwoAlikeSideBySide()
        {
            Heard();yield return Zone("Driftreed");
            var standing=All<PropTint>().Where(t=>t.name.StartsWith("Townsperson")||t.name.StartsWith("A child")).ToArray();
            var walking=All<WalkLoop>();
            Assert.GreaterOrEqual(standing.Length+walking.Length,12,"Driftreed has fewer than twelve residents about");
            Assert.GreaterOrEqual(walking.Length,6,"most of Driftreed stands about: fewer than six residents walk an errand");
            string Coat(Component who){var model=who.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Generated civilian");var r=model!=null?model.GetComponentInChildren<SkinnedMeshRenderer>():null;
                return r!=null&&r.sharedMaterial!=null?r.sharedMaterial.name:"?";}
            var coats=standing.Select(s=>(at:s.transform.position,coat:Coat(s))).ToArray();
            Assert.GreaterOrEqual(coats.Select(c=>c.coat).Distinct().Count(),3,"Driftreed's people wear fewer than three coats: "+string.Join(", ",coats.Select(c=>c.coat).Distinct()));
            for(int a=0;a<coats.Length;a++)for(int b=a+1;b<coats.Length;b++)
                if(Flat(coats[a].at,coats[b].at)<3.2f)Assert.AreNotEqual(coats[a].coat,coats[b].coat,$"two residents side by side at {coats[a].at} and {coats[b].at} wear the same coat");
            foreach(var named in new[]{"Maru","Ona","Dov","Quill"})
            {
                var npc=Here(named);if(npc==null)continue;
                foreach(var (at,_) in coats)Assert.Greater(Flat(at,npc.transform.position),1.4f,$"a resident stands on top of {named}");
            }
        }

        [UnityTest] public IEnumerator OnaDecidesOncePimIsHomeAndAbriNamesTheConvoyOnTheWalkBackToTheStage()
        {
            Heard();foreach(string f in new[]{"wend.tideTurned","wend.lureLearned","wend.pimFound","bossdown.Rotbloom","wend.rotbloomDead"})Flag(f);State.questSteps["ReedRoad"]=11;
            yield return Zone("Driftreed","Mire");
            Assert.IsNotNull(Here("Maru"),"Maru isn't back on the stage once the bloom is dead");Assert.Less(Flat(Here("Maru").transform.position,MaruStage),1);
            var pim=Here("Pim");Assert.IsNotNull(pim,"Pim isn't home at the bench once the bloom is dead");Assert.Less(Flat(pim.transform.position,PimBench),1,"Pim isn't at the bench in the hall");
            yield return Talk(pim);Assert.AreEqual("PimHome",heard.LastOrDefault(),"Pim says something other than PimHome at home");
            yield return Frames(3);var goal=Goal();Assert.IsTrue(goal.HasValue&&Flat(goal.Value,OnaHall)<3,$"the objective points at {goal}, not at Ona, on the last step");
            var locked=All<FlagGate>().Single(g=>g.name=="The reed store is locked").targets[0];var opened=All<FlagGate>().Single(g=>g.name=="The reed store is open").targets[0];
            Assert.IsTrue(locked.activeInHierarchy&&!opened.activeInHierarchy,"the reed store's poles are out before Ona decides");
            yield return Talk(Here("Ona"));Assert.AreEqual("OnaDecides",heard.LastOrDefault(),"Ona's talk once the bloom is dead isn't her decision");
            Assert.IsTrue(Has("wend.decided"));yield return Frames(3);
            Assert.IsTrue(!locked.activeInHierarchy&&opened.activeInHierarchy,"Ona has decided and the reed store's poles are still locked away");
            Assert.IsTrue(Has("quest.ReedRoad.complete"),"Driftreed has decided and the chapter's quest isn't done");
            yield return Talk(Here("Ona"));Assert.AreEqual("OnaAfter",heard.LastOrDefault(),"Ona's word after deciding isn't her last tier");
            // Abri's call on the walk back to the stage, once.
            var call=Point("chapter4.complete");Assert.AreEqual("AbriManyHandsCall",call.dialogueNode);Assert.Less(Flat(call.transform.position,AbriCall),1.5f,"Abri's call isn't on the walk to the stage");
            DialogueSystem.Current.AutoAdvance=true;Put(Lead,Floor(new Vector2(0,31)));yield return Frames(2);Put(Lead,Floor(new Vector2(0,26)));
            float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;yield return Settle();
            Assert.AreEqual(1,heard.Count(h=>h=="AbriManyHandsCall"),"walking back to the stage didn't bring Abri's call");Assert.IsTrue(Has("chapter4.complete"));
            yield return Talk(Here("Maru"));Assert.AreEqual("MaruComes",heard.LastOrDefault(),"Maru's first word once Driftreed has decided isn't that she's coming");
        }

        [UnityTest] public IEnumerator SavingInDriftreedAndContinuingKeepsTheHeldLineTheTurnedTideAndThePlaceSavedAt()
        {
            Heard();Flag("wend.tideTurned");State.questSteps["ReedRoad"]=6;yield return Zone("Driftreed");
            var frame=All<RepairBay>().Single();Put(Lead,frame.transform.position+Vector3.up*.2f);yield return Frames(2);frame.Interact();yield return Frames(2);
            yield return SaveQuitAndContinue();
            Assert.AreEqual("Driftreed",State.zone);Assert.Less(Flat(Lead.transform.position,All<SpawnPoint>().Single(s=>s.id==frame.saveSpawn).transform.position),4,"Continue didn't come back where the frame saved");
            var line=All<MooringHaul>().Single();Assert.IsFalse(line.Available,"the stern line is to hold again after a Continue");Assert.Less(Flat(line.island.position,LineMoored),.5f,"the settled raft came back adrift");
            yield return new WaitForSecondsRealtime(.5f);
            foreach(var swing in All<TideSwing>())Assert.AreEqual(0,Mathf.DeltaAngle(swing.walk.localEulerAngles.y,swing.toYaw),1,$"{swing.name} came back where it lay before the tide");
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsTrue(Reached(MireSpawn),"after a Continue the gate raft can't be walked to");
            Assert.AreEqual(6,Step("ReedRoad"),"the story's place was lost to a save");
        }
    }
}
