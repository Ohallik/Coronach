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
    /// <summary>Chapter 5's ship as a player meets it, in the built scene: the welcome on the dock, the three jobs in any order, everyone's first talk aboard (and never an earlier chapter's
    /// line), Ilo and the claim, the freight hall built to its contract and a combat place only for its fight, the breach and the dinner's seven talks in their order, the chart that closes
    /// the chapter, the four peoples who live here, the counters and the bench, and a save and a Continue. What every zone gets from the shared checks is theirs and not repeated here.
    /// Coordinates are the map brief's (docs/maps/ManyHands.md), which are the builder's own.</summary>
    public sealed class ManyHandsWorldTests
    {
        readonly List<string> heard=new();
        void Talked(string node,string speaker,System.Action<bool> done)=>heard.Add(node);
        void Looked(DiscoveryPoint point){if(!string.IsNullOrEmpty(point.dialogueNode))heard.Add(point.dialogueNode);}
        Picker picker;IDialogueView panelView;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter5");heard.Clear();picker=null;panelView=null;
            Npc.TalkRequested+=Talked;DiscoveryPoint.DialogueRequested+=Looked;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Npc.TalkRequested-=Talked;DiscoveryPoint.DialogueRequested-=Looked;
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
        [ContentRegistry.Expectations] static void City(ContentRegistry.Book book)
        {
            // Where each choice of the chapter's nodes with choices leads (docs/writing/ManyHands.yarn, manifest 8.2): the mooring's two names and Jask's three places are talk; Hadda's
            // three tiers are the press, the frame or a leave line; Corra's and Dov's first talks are their counter or a leave line.
            book.Branch("SelaMooringRail","world","world");book.Branch("TeoStowawayDecide","world","world","world");
            foreach(string node in new[]{"HaddaHands","HaddaHandsRepeat","HaddaHandsPost"})book.Branch(node,"bench","repair","world");
            book.Branch("CorraHands","shop","world");book.Branch("DovHands","shop","world");
        }

        // ------------------------------------------------------------------ the plan, as the builder lays it (ManyHandsBuilder.Hands: editor code a test can't call)
        const string Zone="ManyHands";
        static readonly Vector2 ArrivalAt=new(-4,-60),AbriDock=new(4,-56.4f),AbriDesk=new(-11,43.6f),Pell=new(8,47),Binna=new(66,11.6f),Ruva=new(42.6f,31),Maru=new(-25,-21.4f),RingsAt=new(-19,-9.6f),
            ChartTalk=new(0,45.2f),IloDock=new(-10,-57.6f),F=new(32,-34.5f),HallDoor=new(32,-52.5f),HallTalk=new(32,-50.6f),FreightHallSpawn=new(32,-59),Patrol=new(-60,50),GalleyDoor=new(54.8f,6.6f),
            Corra=new(-24,7.6f),Dov=new(-12,7.6f),Hadda=new(18,7.6f),MeretSeat=new(61.8f,.9f);
        static readonly Vector2[] Scopes={new(-2.9f,47.2f),new(0,49.5f),new(2.9f,47.2f)};
        /// <summary>The ways feet go (ManyHandsBuilder.Hands.Ways): nobody with no name stands within two metres of one.</summary>
        static readonly Vector2[][] Ways=
        {
            new[]{ArrivalAt,new Vector2(0,-55),new Vector2(0,-40),new Vector2(0,-20),new Vector2(0,3),new Vector2(0,20),new Vector2(0,23),new Vector2(0,38.6f),ChartTalk},
            new[]{ArrivalAt,new Vector2(-12,-59.6f),new Vector2(-22,-58.6f)},new[]{new Vector2(0,-57),new Vector2(12,-59.4f),FreightHallSpawn,new Vector2(32,-50),new Vector2(31.4f,-47.6f)},
            new[]{new Vector2(0,-45),new Vector2(-8.6f,-45)},
            new[]{new Vector2(0,-24),new Vector2(-14.5f,-24),new Vector2(-24,-24),new Vector2(-31,-23),new Vector2(-32.5f,-22.5f),new Vector2(-49,-21),new Vector2(-56,-18.6f)},new[]{new Vector2(-24,-24),new Vector2(-29,-29),new Vector2(-42.5f,-32),new Vector2(-44.75f,-32.5f),new Vector2(-44.5f,-33.6f),new Vector2(-48,-34.3f)},
            new[]{new Vector2(-24,3),new Vector2(-40,2),new Vector2(-58,-4),new Vector2(-58,-11.5f),new Vector2(-58,-15.5f)},new[]{new Vector2(-24,-2),new Vector2(-24,-11.5f),new Vector2(-24,-19)},
            new[]{new Vector2(-34,3),new Vector2(0,3),new Vector2(50,3),new Vector2(56,2.4f),new Vector2(66,3.6f)},new[]{new Vector2(56,2.4f),new Vector2(55.6f,4.6f),new Vector2(55.6f,10.4f),new Vector2(64.8f,10.4f)},
            new[]{new Vector2(0,20),new Vector2(16,20.4f),new Vector2(27,24.2f),new Vector2(44,24.2f),new Vector2(60,24.2f),new Vector2(73.4f,24.2f)},new[]{new Vector2(44,24.2f),new Vector2(44,52)},new[]{new Vector2(60,24.2f),new Vector2(60,52)},
            new[]{new Vector2(0,19.6f),new Vector2(-16,20.4f),new Vector2(-30,20),new Vector2(-52,20),new Vector2(-52,30.6f),new Vector2(-52,40.6f),new Vector2(-52,50.2f),new Vector2(-58,50.2f)},
        };
        /// <summary>The four peoples' bodies (ManyHandsBuilder.Hands.Vael, Okhari, Ilu, WickFolk).</summary>
        static readonly (string people,string[] bodies)[] Peoples={("Vael",new[]{"Neve","Mira","Hal","Orrin","SurvivorNatural"}),("Okhari",new[]{"OkhariBody","OkhariGrower","SennBody"}),
            ("Ilu",new[]{"QuillBody","MaruBody","OnaBody","DovBody","PimBody"}),("Wick-folk",new[]{"AumiBody","IshenBody","BrekkBody"})};

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static int Step(string quest)=>State.questSteps.TryGetValue(quest,out var step)?step:-1;
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static float Flat(Vector3 a,Vector2 b)=>Vector2.Distance(new Vector2(a.x,a.z),b);
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>Abri's welcome as it plays: the quest started, then the flag told to it (her node's two lines).</summary>
        static void Aboard(){RpgStart("ManyHands");Flag("hands.aboard");Flag("hands.welcomeHeard");Lattice.Rpg.RpgServices.Quests.Report(ObjectiveKind.Flag,"hands.aboard");}
        static void RpgStart(string quest){if(Step(quest)<0)Lattice.Rpg.RpgServices.Quests.Start(quest);}
        /// <summary>The story with the three jobs done and Ilo heard.</summary>
        static void JobsDone(){Aboard();foreach(string f in new[]{"hands.ruvaHeard","hands.maruHeard","hands.bothHeard","hands.mooringSettled","hands.mooringForRoots","hands.ventFixed","hands.pellAboard",
            "hands.lamp1","hands.lamp2","hands.lamp3","hands.lampsSighted","hands.chartOpen","hands.jobsDone"})Flag(f);}
        /// <summary>The story at the end of the fight: Dessik has yielded.</summary>
        static void Yielded(){JobsDone();foreach(string f in new[]{"hands.iloAboard","hands.claimRead","bossdown.Hulljack","hands.feet1","hands.feet2","hands.feet3","hands.feet4","hands.hallEntered",
            "bossdown.DessikVarn","hands.dessikYielded"})Flag(f);State.questSteps["ManyHands"]=7;}
        static IEnumerator Load(string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(Zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"Many Hands never finished loading");
            yield return Ready();
        }
        static IEnumerator Ready()
        {
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            float deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Prepared,"fixture: Many Hands' conversations never finished preparing");
            yield return new WaitForSecondsRealtime(.7f);
        }
        static IEnumerator SaveQuitAndContinue()
        {
            GameServices.Current.Saves.Save("autosave",State);
            SceneFlow.Current.LoadZone("Title");float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            yield return null;yield return null;
            var title=Object.FindFirstObjectByType<TitleScreen>();Assert.IsNotNull(title,"fixture: the title never came up");
            title.StartGame(true);yield return null;
            deadline=Time.unscaledTime+25;while((State.zone!=Zone||SceneFlow.Current.Loading)&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual(Zone,SceneFlow.Current.Zone,"Continue didn't bring the party back aboard");yield return Ready();
        }
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static Vector3 Floor(Vector2 at,float y=1)
        {
            Assert.IsTrue(NavMesh.SamplePosition(new Vector3(at.x,y,at.y),out var hit,2.5f,NavMesh.AllAreas),$"fixture: no floor near ({at.x}, {at.y})");
            return hit.position;
        }
        static IEnumerator Settle()
        {
            float deadline=Time.unscaledTime+40;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(DialogueSystem.Current.Running,"a conversation never ended");
            yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Talk(Npc npc){Assert.IsNotNull(npc,"fixture: nobody to talk to");DialogueSystem.Current.AutoAdvance=true;Assert.IsTrue(npc.Available,npc.speaker+" can't be talked to");npc.Interact();yield return null;yield return Settle();}
        static IEnumerator Discover(DiscoveryPoint point){DialogueSystem.Current.AutoAdvance=true;Assert.IsTrue(point.Available,$"\"{point.prompt}\" isn't offered");point.Interact();yield return null;yield return Settle();}
        static Npc Here(string speaker)
        {
            var here=All<Npc>().Where(n=>n.speaker==speaker&&n.gameObject.activeInHierarchy&&n.enabled).ToArray();
            Assert.LessOrEqual(here.Length,1,speaker+" stands in two places at once: "+string.Join(", ",here.Select(n=>n.transform.position)));return here.FirstOrDefault();
        }
        static DiscoveryPoint Point(string flag)=>All<DiscoveryPoint>().Single(p=>p.flag==flag);
        static bool Reached(Vector2 to,float y=1)
        {
            var path=new NavMeshPath();
            return NavMesh.SamplePosition(Floor(ArrivalAt),out var from,1,NavMesh.AllAreas)&&NavMesh.SamplePosition(new Vector3(to.x,y,to.y),out var goal,2.5f,NavMesh.AllAreas)
                &&NavMesh.CalculatePath(from.position,goal.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        }
        static string Heading(ShopUi shop)=>shop.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name=="Heading").Select(t=>t.text).FirstOrDefault()??"";
        sealed class Picker:IDialogueView
        {
            public string want;public string[] also;public readonly List<string> offered=new();
            public bool AdvanceRequested=>true;
            public void Show(string speaker,PortraitEmotion emotion,Sprite portrait,string body){}
            public void SetVisibleCharacters(int count){}
            public void Hide(){}
            public void Options(IReadOnlyList<string> labels,System.Action<int> selected)
            {
                offered.Clear();offered.AddRange(labels);int row=offered.FindIndex(l=>l.IndexOf(want,System.StringComparison.OrdinalIgnoreCase)>=0);
                if(row<0&&also!=null){foreach(var w in also){row=offered.FindIndex(l=>l.IndexOf(w,System.StringComparison.OrdinalIgnoreCase)>=0);if(row>=0)break;}if(row<0)row=offered.Count-1;}
                selected(row<0?0:row);
            }
            public void ClearOptions(){}
        }
        /// <summary>A talk through to its end, taking the choice that says one of these if it offers any (else its last, the leave line), and saying nothing about what was offered.</summary>
        IEnumerator Answer(Npc npc,params string[] wants)
        {
            var dialogue=DialogueSystem.Current;panelView=dialogue.View;picker=new Picker{want=wants[0],also=wants};dialogue.View=picker;dialogue.AutoAdvance=false;
            npc.Interact();yield return null;
            float deadline=Time.unscaledTime+30;while(dialogue.Running&&Time.unscaledTime<deadline)yield return null;
            dialogue.View=panelView;Assert.IsFalse(dialogue.Running,npc.speaker+"'s conversation never ended");yield return Frames(3);
        }
        IEnumerator Choose(System.Action begin,string who,string want)
        {
            var dialogue=DialogueSystem.Current;panelView=dialogue.View;picker=new Picker{want=want};dialogue.View=picker;dialogue.AutoAdvance=false;
            begin();yield return null;
            float deadline=Time.unscaledTime+30;while(dialogue.Running&&Time.unscaledTime<deadline)yield return null;
            dialogue.View=panelView;
            Assert.IsFalse(dialogue.Running,who+"'s conversation never ended");
            Assert.IsTrue(picker.offered.Any(l=>l.IndexOf(want,System.StringComparison.OrdinalIgnoreCase)>=0),$"{who} offers no choice that says '{want}': {string.Join(" / ",picker.offered)}");
            yield return Frames(3);
        }

        // ================================================================== the ship
        [UnityTest] public IEnumerator ManyHandsIsASafeTownWithItsWaysInAndOutAndEveryServiceItsChartStopLists()
        {
            yield return Load();
            Assert.AreEqual(ZoneKind.GroundSafe,ZoneController.Current.Kind,"Many Hands isn't a town");
            var stop=GameCatalog.Find<ChartStop>(Zone);Assert.IsNotNull(stop,"Many Hands isn't on Sela's chart");
            Assert.IsTrue(stop.shop&&stop.bench&&stop.repair,"the chart doesn't list the ship's counters, bench and frame");
            Assert.IsNotNull(All<Bench>().SingleOrDefault(),"Many Hands has no bench");Assert.GreaterOrEqual(All<RepairBay>().Length,2,"Many Hands hasn't Hadda's frame and the riggers' cradle");
            foreach(string spawn in new[]{"Arrival","FreightHall","Arcade"})Assert.IsTrue(All<SpawnPoint>().Any(s=>s.id==spawn),"Many Hands has no spawn "+spawn);
            var pads=All<DockingPad>();
            var tallow=pads.SingleOrDefault(p=>p.scene=="TallowApproach");Assert.IsNotNull(tallow,"there's no launch back to Tallow's approach");Assert.IsTrue(tallow.gameObject.activeInHierarchy,"the launch to Tallow is hidden");
            var hull=pads.SingleOrDefault(p=>p.scene=="ManyHandsHull");Assert.IsNotNull(hull,"there's no launch to the hull");
            Assert.IsFalse(hull.gameObject.activeInHierarchy,"the launch to the hull is offered before Dessik has read his claim");
            Flag("hands.claimRead");yield return Frames(3);Assert.IsTrue(hull.gameObject.activeInHierarchy,"the claim is read and there's no launch to the hull");
            Assert.Less(Flat(hull.transform.position,new Vector2(8,-62)),3,"the hull pad isn't on the dock");
            // Every district a player is sent to can be walked to from the dock.
            foreach(var (place,at,y) in new[]{("Abri's desk",AbriDesk,3.6f),("Pell",Pell,3.6f),("the chart table",ChartTalk,3.6f),("Binna's hatch",Binna,0f),("Ruva",Ruva,0f),("Maru",Maru,0f),
                ("the hold wall",RingsAt,0f),("the freight yard",FreightHallSpawn,0f),("Corra",Corra,0f),("Dov",Dov,0f),("Hadda",Hadda,0f),("the cabins",Patrol,0f)})
                Assert.IsTrue(Reached(at,y+1),place+" can't be walked to from the dock");
        }

        [UnityTest] public IEnumerator AbrisWelcomeOnTheDockStartsTheChapterAndSheWalksUpToHerDesk()
        {
            yield return Load();
            Assert.AreEqual(-1,Step("ManyHands"),"fixture: the chapter's quest has begun before the party is aboard");
            var abri=Here("Keeper");Assert.IsNotNull(abri,"Keeper Abri isn't on the dock");Assert.Less(Flat(abri.transform.position,AbriDock),1.5f,"Keeper Abri isn't on the dock to meet the party");
            var welcome=Point("hands.welcomeHeard");Assert.IsTrue(welcome.startsOnCrossing,"the welcome can be walked round");
            DialogueSystem.Current.AutoAdvance=true;Put(Lead,Floor(ArrivalAt));yield return Frames(2);Put(Lead,Floor(new Vector2(-2,-57.4f)));
            float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            yield return Settle();
            Assert.Contains("AbriHandsArrival",heard,"stepping off the pad doesn't bring Abri's welcome: heard "+string.Join(", ",heard));
            Assert.IsTrue(Has("hands.aboard"));Assert.AreEqual(1,Step("ManyHands"),"the welcome started the quest and left it short of the three jobs");
            // She leaves the dock for her desk: off the dock in sight, at her desk on the gallery.
            deadline=Time.unscaledTime+20;while(abri.gameObject.activeInHierarchy&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(abri.gameObject.activeInHierarchy,"Abri never left the dock");
            var desk=Here("Keeper");Assert.IsNotNull(desk,"Abri isn't at her desk once she has welcomed the party");Assert.Less(Flat(desk.transform.position,AbriDesk),1.5f,"Abri's other place isn't her desk");
            yield return Talk(desk);Assert.AreEqual("AbriHandsRepeat",heard.Last(),"Abri at her desk says something other than her repeat line (an earlier chapter's?)");
        }

        [UnityTest] public IEnumerator TheThreeJobsGoInAnyOrderAndOnlyThenDoesIloComeOffTheCourier()
        {
            Aboard();yield return Load();
            Assert.AreEqual(1,Step("ManyHands"),"fixture: the story isn't at the three jobs");
            Assert.IsNull(Here("Ilo"),"Ilo is aboard before the three jobs are done");
            // The vent first.
            yield return Talk(Here("Binna"));Assert.AreEqual("BinnaVent",heard.Last(),"Binna's first talk aboard isn't the vent");Assert.IsTrue(Has("hands.ventFixed"));
            // The chart: the lamps can't be sighted until Pell has spoken, then the three, then the table.
            foreach(var scope in Scopes)Assert.IsFalse(All<DiscoveryPoint>().Single(p=>p.prompt=="Sight the gallery lamp"&&Flat(p.transform.position,scope)<.6f).Available,"a gallery lamp can be sighted before Pell has asked");
            Assert.IsFalse(Point("hands.chartSet").Available,"the chart table opens before its lamps are sighted");
            yield return Talk(Here("Pell"));Assert.AreEqual("PellChartLamps",heard.Last(),"Pell's first talk aboard isn't the lamps");Assert.IsTrue(Has("hands.pellAboard"),"hearing Pell out wasn't kept");
            yield return Talk(Here("Pell"));Assert.AreEqual("PellHandsRepeat",heard.Last(),"Pell tells the lamps twice");
            foreach(string f in new[]{"hands.lamp1","hands.lamp2","hands.lamp3"}){Assert.IsFalse(Point("hands.chartSet").Available,"the chart opens with a lamp unsighted");yield return Discover(Point(f));}
            Assert.IsTrue(Has("hands.lampsSighted"),"three lamps sighted and the chart table doesn't know");
            yield return Discover(Point("hands.chartSet"));Assert.AreEqual("SelaChartOpen",heard.Last());Assert.IsTrue(Has("hands.chartOpen"));
            // The mooring last: the rings can't be fitted until both sides are heard.
            Assert.IsFalse(Point("hands.ringsFitted").Available,"the rings can be fitted before anyone is heard");
            yield return Talk(Here("Maru"));Assert.AreEqual("MaruMooring",heard.Last(),"Maru's first talk aboard isn't the mooring (an earlier chapter's beat?)");
            Assert.IsFalse(Point("hands.ringsFitted").Available,"the rings can be fitted with only Maru heard");
            yield return Talk(Here("Ruva"));Assert.AreEqual("RuvaMooring",heard.Last(),"Ruva's first talk aboard isn't the mooring");
            yield return Frames(3);
            var rings=Point("hands.ringsFitted");Assert.IsTrue(rings.Available,"both sides are heard and the rings can't be fitted");Assert.Less(Flat(rings.transform.position,RingsAt),1);
            yield return Choose(()=>rings.Interact(),"Sela","tide line");
            Assert.IsTrue(Has("hands.mooringSettled")&&Has("hands.mooringForTides"),"fitting the rings as a tide line didn't settle it Maru's way");
            yield return Frames(3);Assert.AreEqual(4,Step("ManyHands"),"the three jobs done in that order didn't bring the story to the courier");
            Assert.IsTrue(Has("hands.jobsDone"));
            var ilo=Here("Ilo");Assert.IsNotNull(ilo,"the three jobs are done and Ilo hasn't come aboard");Assert.Less(Flat(ilo.transform.position,IloDock),1.5f,"Ilo isn't on the dock");
            yield return Talk(Here("Keeper"));Assert.AreEqual("AbriCourier",heard.Last(),"Abri doesn't send the party to the courier");
            yield return Talk(ilo);Assert.AreEqual("IloArrives",heard.Last());Assert.AreEqual(5,Step("ManyHands"),"meeting Ilo didn't move the story to the hull");
            // Dessik's claim comes over the radio straight after, and the launch to the hull is there.
            float deadline=Time.unscaledTime+8;while(!heard.Contains("DessikClaim")&&Time.unscaledTime<deadline)yield return null;
            yield return Settle();Assert.Contains("DessikClaim",heard,"Dessik's claim never came over the radio after Ilo");Assert.IsTrue(Has("hands.claimRead"));
            Assert.IsTrue(All<DockingPad>().Single(p=>p.scene=="ManyHandsHull").gameObject.activeInHierarchy,"the claim is read and there's no launch to the hull");
        }

        [UnityTest] public IEnumerator EveryonesFirstTalkAboardPlaysOnceAndNoEarlierChaptersLinePlaysAboard()
        {
            // Chapter 4 left Dov's order and Maru's beacons untaken in this fixture (DevLoadout tells only the main story): their beats are still true, and must not play aboard.
            Aboard();Flag("met.Dov");yield return Load();
            Assert.AreEqual(-1,Step("SmokehouseOrder"),"fixture: the smokehouse order was taken");
            foreach(var (who,first,repeat) in new[]{("Dov","DovHands","DovHandsRepeat"),("Hadda","HaddaHands","HaddaHandsRepeat"),("Corra","CorraHands","CorraHandsRepeat"),("Ona","OnaHands","OnaHandsRepeat"),
                ("Pim","PimHands","PimHandsRepeat"),("Garo","GaroHands","GaroHandsRepeat"),("Dabb","DabbHands","DabbHandsRepeat"),("Teo","TeoHands","TeoHandsRepeat")})
            {
                var npc=Here(who);Assert.IsNotNull(npc,who+" isn't aboard");
                heard.Clear();yield return Answer(npc,"later","not today","not now","nothing");Assert.AreEqual(first,heard.FirstOrDefault(),who+"'s first talk aboard isn't theirs");
                var shop=Object.FindFirstObjectByType<ShopUi>();if(shop!=null&&shop.IsOpen)shop.Close();var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();yield return Frames(2);
                heard.Clear();yield return Answer(npc,"nothing","later","not today","not now");Assert.AreEqual(repeat,heard.FirstOrDefault(),who+" says their first talk aboard twice, or something else the second time");
                shop=Object.FindFirstObjectByType<ShopUi>();if(shop!=null&&shop.IsOpen)shop.Close();menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();yield return Frames(2);
            }
            // The control: in Driftreed, chapter 4's beat for Dov is live in this very state (his untaken order), so aboard it is chapter 5's beats alone that keep it off the ship.
            State.zone="Driftreed";Assert.AreEqual("DovOrderOffer",SideStories.NodeFor("Dov",State),"fixture: chapter 4's beat for Dov isn't live in this state, so this test proves nothing");
            Assert.IsNull(SideStories.NodeFor("Hadda",State),"a line meant for the ship plays in Driftreed");State.zone=Zone;
        }

        // ================================================================== the freight hall
        [UnityTest] public IEnumerator TheFreightHallIsBuiltToItsContractAndIsACombatPlaceOnlyForItsFight()
        {
            JobsDone();yield return Load();
            var door=All<Membrane>().Single(m=>m.name=="Freight hall cargo door");Assert.IsTrue(door.gameObject.activeInHierarchy&&!door.Open,"the freight hall's cargo door isn't shut before the frame comes through the roof");
            var pocket=All<FightPocket>().Single();Assert.AreEqual(0,pocket.State,"the hall is staged for its fight before the frame is in");
            var duel=All<EncounterVolume>().Single(e=>e.encounterId=="DessikVarn");
            Assert.AreEqual("hands.hallEntered",duel.requiredFlag);Assert.AreEqual("FreightHall",duel.checkpointSpawn);
            var post=duel.spawners.Single();Assert.Less(Flat(post.transform.position,F),.5f,"Dessik's frame isn't at F");Assert.AreEqual(180,post.transform.eulerAngles.y,1,"the frame doesn't face the hall door");
            Assert.IsTrue(post.faceAsPlaced);Assert.AreEqual(0,post.radius);
            // The floor: level and clear round F, walls north and east 16 m off, four fixed stacks and six loose crates where the contract puts them.
            var at=Floor(F);
            Assert.IsEmpty(Physics.OverlapCapsule(at+Vector3.up*.6f,at+Vector3.up*2,6,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.isTrigger&&c.GetComponentInParent<CombatActor>()==null&&c.name!="Many Hands deck").Select(c=>c.name).ToArray(),"something solid stands within 6 m of F");
            foreach(var (dir,name) in new[]{(Vector3.forward,"north"),(Vector3.right,"east")})
                Assert.IsFalse(Physics.Raycast(at+Vector3.up*1.2f,dir,out var wall,16,~0,QueryTriggerInteraction.Ignore)&&wall.collider.name=="Many Hands deck","the hall's "+name+" wall is nearer F than 16 m");
            var stacks=All<BoxCollider>().Where(b=>b.name=="Cargo stack").ToArray();Assert.AreEqual(4,stacks.Length,"the hall hasn't its four fixed stacks");
            foreach(var s in stacks){float d=Flat(s.bounds.center,F);Assert.That(d,Is.InRange(9f,13f),"a stack stands "+d+" m from F");Assert.IsTrue(s.gameObject.isStatic,"a stack isn't in the bake");}
            var crates=All<LooseCargo>();Assert.AreEqual(6,crates.Length,"the hall hasn't six loose crates");
            foreach(var c in crates){float d=Flat(c.transform.position,F);Assert.That(d,Is.InRange(7f,14f),"a crate stands "+d+" m from F");Assert.IsFalse(c.gameObject.isStatic,"a loose crate is in the bake");}
            // The checkpoint: outside the seal, nearer the door than the arrival, on the floor.
            var spawn=All<SpawnPoint>().Single(s=>s.id=="FreightHall");Assert.Less(Flat(spawn.transform.position,HallDoor),Flat(All<SpawnPoint>().Single(s=>s.id=="Arrival").transform.position,HallDoor),"the checkpoint is no nearer the hall than the dock");
            Assert.Less(Flat(spawn.transform.position,HallDoor),60);
            // The frame through the roof: the door opens, the talk waits inside it, and the room is a combat place while the fight is on, and only inside it.
            Flag("hands.iloAboard");Flag("hands.claimRead");Flag("bossdown.Hulljack");State.questSteps["ManyHands"]=6;
            yield return Load("FreightHall");
            Assert.AreEqual(ZoneKind.GroundSafe,ZoneController.Current.Kind,"the freight yard is a combat place");
            Assert.IsFalse(All<Membrane>().Single(m=>m.name=="Freight hall cargo door").gameObject.activeInHierarchy,"the cargo door is still shut after the frame came through the roof");
            Assert.IsTrue(Reached(F),"the hall's floor can't be walked to once its door is open");
            pocket=All<FightPocket>().Single();yield return Frames(2);Assert.AreEqual(1,pocket.State);
            DialogueSystem.Current.AutoAdvance=true;Put(Lead,Floor(HallDoor+new Vector2(0,-2)));yield return Frames(2);Put(Lead,Floor(HallTalk));
            float deadline=Time.unscaledTime+4;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            yield return Settle();Assert.Contains("DessikHall",heard,"walking into the hall doesn't bring Dessik's talk");Assert.IsTrue(Has("hands.hallEntered"));
            yield return Frames(3);Assert.AreEqual(ZoneKind.GroundCombat,ZoneController.Current.Kind,"the hall isn't a combat place with the frame in it");
            Assert.AreEqual(BodyForm.Shaped,ZoneController.Current.Form,"the party doesn't take its fighting form in the hall");
            Put(Lead,Floor(FreightHallSpawn));yield return Frames(4);Assert.AreEqual(ZoneKind.GroundSafe,ZoneController.Current.Kind,"the yard outside the hall became a combat place");
        }

        /// <summary>The builder's four fixed stacks in the hall (ManyHandsBuilder.Hands.HallStacks), copied.</summary>
        static readonly Vector2[] HallStacks={new(42.5f,-29.5f),new(22,-28.5f),new(22.5f,-41),new(42,-41.5f)};
        /// <summary>A stack as tall as a body that comes between the fixed camera and the party fades as the rest of the town's tall scenery does, from the freight hall's arrival
        /// and from the fight's floor behind each of the hall's four stacks: drawn as one piece with no fade, a container stack two high stood across the arrival and filled
        /// half the frame (s29/070). Measured on the camera's own line to the lead's chest, and shown first on a stack with its fade taken off.</summary>
        [UnityTest] public IEnumerator TheFreightYardsAndHallsTallStacksFadeWhenTheyComeBetweenTheCameraAndTheParty()
        {
            Flag("hands.iloAboard");Flag("hands.claimRead");Flag("bossdown.Hulljack");State.questSteps["ManyHands"]=6;
            yield return Load("FreightHall");
            // D414: scenery opens its hole only for a lead nothing draws through (HeroThrough draws the rest as a clean line); whether it can is what this reads.
            Lead.GetComponent<HeroThrough>().enabled=false;
            // What is drawn on the camera's line to the lead's chest and as tall as the fade's rule (ZoneKit.TallEnoughToBlock, 2.5 m): scenery, never a body.
            List<MeshRenderer> InTheWay()
            {
                var eye=Camera.main.transform.position;var chest=Lead.transform.position+Vector3.up*1.1f;var ray=new Ray(eye,chest-eye);float far=Vector3.Distance(eye,chest)-.4f;
                return Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.bounds.size.y>=2.5f&&r.bounds.size.x<40&&r.bounds.size.z<40
                    &&r.GetComponentInParent<CombatActor>()==null&&r.GetComponentInParent<Npc>()==null&&r.bounds.IntersectRay(ray,out float at)&&at<far).ToList();
            }
            static float Faded(Renderer r){var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);return block.GetFloat("_OccFade");}
            // The freight hall's arrival no longer has a stack on the camera's line at all (D359: moved off it, s29/070): ChapterFourToSixLookTests walks it and holds it clear.
            var places=new List<(string where,Vector3 at,string stack)>();
            var behind=new Vector2(Mathf.Sin(20*Mathf.Deg2Rad),Mathf.Cos(20*Mathf.Deg2Rad));
            for(int i=0;i<HallStacks.Length;i++)places.Add(($"the fight's floor behind the hall's stack {i+1}",Floor(HallStacks[i]+behind*2.7f),"Hall cargo"));
            bool shownFailing=false;var faults=new List<string>();
            foreach(var (where,at,stack) in places)
            {
                Put(Lead,at);yield return new WaitForSecondsRealtime(1.4f);
                var way=InTheWay();var stacks=way.Where(r=>r.name.StartsWith(stack)).ToList();
                if(stacks.Count==0){faults.Add($"from {where} no {stack} stands on the camera's line (in the way: {string.Join(", ",way.Select(r=>r.name))})");continue;}
                if(!shownFailing)
                {
                    // The broken state, built here: the stack with its fade taken off is what the check has to find.
                    var fade=stacks[0].GetComponentInParent<SceneryOcclusion>();Assert.IsNotNull(fade,$"{stacks[0].name}, on the camera's line from {where}, has no scenery fade");
                    fade.enabled=false;yield return new WaitForSecondsRealtime(.8f);
                    Assert.IsTrue(InTheWay().Any(r=>r==stacks[0]&&Faded(r)<=0),"the check doesn't see a stack whose fade is off standing solid in the way");
                    fade.enabled=true;shownFailing=true;yield return new WaitForSecondsRealtime(1.4f);way=InTheWay();
                }
                // Only the stacks: a renderer's box is all this measures, and the hall's dressed walls and its floor detail span the room, so their boxes cross every line in it.
                foreach(var r in way.Where(r=>r.name.StartsWith("Container stack")||r.name.StartsWith("Hall cargo")))
                {
                    if(r.GetComponentInParent<SceneryOcclusion>()==null)faults.Add($"{r.name} ({r.bounds.size.y:0.0} m tall) stands between the camera and the lead at {where} and doesn't fade");
                    else if(Faded(r)<=0)faults.Add($"{r.name} is marked to fade but stood solid between the camera and the lead at {where}");
                }
            }
            // Out of the fight's room before the scene goes: a pocket switched off with the lead inside hands the zone back as the services go, and that threw in teardown (D359).
            Put(Lead,Floor(FreightHallSpawn));yield return Frames(4);
            Assert.IsTrue(shownFailing,"fixture: no stack was ever on the camera's line");
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        // ================================================================== the breach and the dinner
        [UnityTest] public IEnumerator TheBreachIsSealedAndTheDinnersSevenTalksComeOnlyInTheirOrder()
        {
            Yielded();yield return Load();
            Assert.IsTrue(All<GameObject>().Any(g=>g.name=="The breach, sealed"&&g.activeInHierarchy),"the breached cabins show no seal after the fight");
            var captain=Here("Patrol");Assert.IsNotNull(captain,"the Steady Hand's captain isn't at the cabins");Assert.Less(Flat(captain.transform.position,Patrol),1.5f);
            Assert.IsNull(Here("Meret"),"Meret can be talked to before the captain has reported to her");
            yield return Talk(captain);Assert.AreEqual("PatrolBreachSealed",heard.Last());Assert.IsTrue(Has("hands.breachSealed"));
            yield return Frames(3);var meret=Here("Meret");Assert.IsNotNull(meret,"the captain has reported and Meret isn't behind him");
            yield return Talk(meret);Assert.AreEqual("MeretArrives",heard.Last());Assert.IsTrue(Has("hands.meretMet"));
            // Out of turn: Ruva at the table before Meret has explained anything says her ordinary line, and nobody's talk moves the story on but the one it waits for.
            yield return new WaitForSecondsRealtime(1);
            var step=Step("ManyHands");Assert.AreEqual(9,step,"meeting Meret didn't bring the story to the meal");
            Assert.IsTrue(All<Npc>().Where(n=>n.speaker=="Meret").All(n=>!n.gameObject.activeInHierarchy||Flat(n.transform.position,MeretSeat)>2),"Meret can be talked to at the table before Binna has fed her");
            var ruvaAtTable=All<Npc>().Single(n=>n.speaker=="Ruva"&&Flat(n.transform.position,new Vector2(59.4f,.9f))<1);Assert.IsTrue(ruvaAtTable.gameObject.activeInHierarchy,"Ruva isn't at the table");
            yield return Talk(ruvaAtTable);Assert.AreNotEqual("RuvaDinnerNumber",heard.Last(),"Ruva asks for the number before Meret has explained Stillwater");Assert.AreEqual(9,Step("ManyHands"));
            // In turn.
            yield return Talk(Here("Binna"));Assert.AreEqual("BinnaDinner",heard.Last());Assert.AreEqual(10,Step("ManyHands"));
            yield return Frames(3);var seated=Here("Meret");Assert.IsNotNull(seated,"Meret isn't at the table to explain Stillwater");Assert.Less(Flat(seated.transform.position,MeretSeat),1.5f);
            yield return Talk(seated);Assert.AreEqual("MeretStillwater",heard.Last());
            yield return Frames(3);Assert.IsNull(Here("Meret"),"Meret can be talked to while it's Ruva's turn");
            yield return Talk(ruvaAtTable);Assert.AreEqual("RuvaDinnerNumber",heard.Last());
            yield return Talk(Here("Ilo"));Assert.AreEqual("IloFootnotes",heard.Last());
            yield return Frames(3);yield return Talk(Here("Meret"));Assert.AreEqual("MeretSorrel",heard.Last());Assert.IsTrue(Has("hands.sorrelNamed"));
            // She walks to the galley door, and is there once she would have got there: never two of her.
            float deadline=Time.unscaledTime+12;Npc atDoor=null;
            while(Time.unscaledTime<deadline){yield return null;atDoor=Here("Meret");if(atDoor!=null&&Flat(atDoor.transform.position,GalleyDoor)<1.5f&&atDoor.Available)break;}
            Assert.IsNotNull(atDoor,"Meret never got to the galley door");Assert.Less(Flat(atDoor.transform.position,GalleyDoor),1.5f,"Meret's last talk isn't at the galley door");
            yield return Talk(atDoor);Assert.AreEqual("MeretDrawings",heard.Last());Assert.IsTrue(Has("hands.dinnerDone"));Assert.AreEqual(15,Step("ManyHands"),"the dinner's end didn't bring the story to the chart");
            // The chapter's last talk at the chart table.
            var read=Point("hands.chartLooked");Assert.IsTrue(read.Available,"the chart can't be read after the dinner");
            yield return Discover(read);Assert.AreEqual("SelaLampGallery",heard.Last());Assert.IsTrue(Has("chapter5.complete"));Assert.IsTrue(Has("quest.ManyHands.complete"),"the chapter's quest didn't complete");
        }

        [UnityTest] public IEnumerator ASaveMidDinnerContinuesWithEveryoneWhereTheDinnerLeftThem()
        {
            Yielded();foreach(string f in new[]{"hands.breachSealed","met.Meret","hands.meretMet","hands.dinnerSat"})Flag(f);State.questSteps["ManyHands"]=10;
            yield return Load("Arcade");yield return SaveQuitAndContinue();
            var meret=Here("Meret");Assert.IsNotNull(meret,"Meret isn't at the table after a Continue mid-dinner");Assert.Less(Flat(meret.transform.position,MeretSeat),1.5f);
            Assert.IsNull(All<Npc>().FirstOrDefault(n=>n.speaker=="Ruva"&&n.gameObject.activeInHierarchy&&Flat(n.transform.position,Ruva)<2),"Ruva is in the garden and at the table at once");
            yield return Talk(meret);Assert.AreEqual("MeretStillwater",heard.Last(),"the dinner didn't go on where it was saved");
        }

        // ================================================================== the people who live here
        [UnityTest] public IEnumerator ManyHandsIsLivedInByFourPeoplesInTheirOwnBodiesAboutTheirOwnBusiness()
        {
            Aboard();yield return Load();
            string Coat(Component who){var body=who.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Generated civilian");var skin=body!=null?body.GetComponentInChildren<SkinnedMeshRenderer>(true):null;return skin!=null&&skin.sharedMaterial!=null?skin.sharedMaterial.name:"";}
            // Standing is standing: a walker's loop is checked by the builder against what's solid, and the children's run of the spine is the brief's.
            var standing=All<PropTint>().Where(p=>p.name.StartsWith("Townsperson")||p.name.StartsWith("A child")).Where(p=>p.gameObject.activeInHierarchy&&p.GetComponent<WalkLoop>()==null).ToArray();
            var walking=All<WalkLoop>();
            Assert.GreaterOrEqual(standing.Length+walking.Length,20,"fewer than twenty people live aboard");Assert.GreaterOrEqual(walking.Length,6,"hardly anyone is about their business");
            var everyone=standing.Select(p=>(Component)p).Concat(walking).ToArray();
            foreach(var (people,bodies) in Peoples)
            {
                var looks=everyone.Select(Coat).Where(c=>bodies.Any(b=>c.StartsWith(b))).Distinct().ToArray();
                Assert.GreaterOrEqual(looks.Length,3,$"the {people} aboard wear fewer than three looks: {string.Join(", ",looks)}");
            }
            for(int i=0;i<standing.Length;i++)for(int j=i+1;j<standing.Length;j++)
                if(Vector3.Distance(standing[i].transform.position,standing[j].transform.position)<3.2f)Assert.AreNotEqual(Coat(standing[i]),Coat(standing[j]),$"two neighbours wear one body at {standing[i].transform.position}");
            foreach(var p in standing)
            {
                var at=new Vector2(p.transform.position.x,p.transform.position.z);float d=Ways.Min(w=>Enumerable.Range(1,w.Length-1).Min(k=>Segment(at,w[k-1],w[k])));
                Assert.Greater(d,1.6f,$"{p.name} stands on a way people walk, at {at}");
                foreach(var npc in All<Npc>().Where(n=>n.gameObject.activeInHierarchy))Assert.Greater(Vector3.Distance(npc.transform.position,p.transform.position),1.4f,$"{p.name} stands on {npc.speaker}");
            }
            // And they walk: give the walkers a few seconds and most of them have moved.
            var before=walking.Select(w=>w.transform.position).ToArray();yield return new WaitForSecondsRealtime(4);
            Assert.GreaterOrEqual(walking.Where((w,i)=>Vector3.Distance(w.transform.position,before[i])>1).Count(),walking.Length/2,"the people on errands stand still");
        }
        static float Segment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));return Vector2.Distance(p,a+d*t);}

        [UnityTest] public IEnumerator CorraAndDovOpenTheirOwnCountersAndHaddasPressAndFrameWork()
        {
            Aboard();Flag("hands.corraAboard");Flag("hands.dovAboard");Flag("hands.haddaAboard");yield return Load("Arcade");var shop=Object.FindFirstObjectByType<ShopUi>();
            yield return Talk(Here("Corra"));Assert.AreEqual("CorraHandsRepeat",heard.Last());Assert.IsTrue(shop.IsOpen,"Corra's repeat doesn't open her stall");StringAssert.Contains("CORRA",Heading(shop),"Corra opened someone else's counter");
            Assert.Greater(ShopUi.Parts(GameCatalog.Find<ShopInventory>("ManyHands_Corra")).Count(),0,"Corra sells no parts");shop.Close();yield return Frames(2);
            yield return Talk(Here("Dov"));Assert.IsTrue(shop.IsOpen,"Dov's repeat doesn't open his counter");StringAssert.Contains("DOV",Heading(shop),"Dov opened someone else's counter");shop.Close();yield return Frames(2);
            yield return Choose(()=>Here("Hadda").Interact(),"Hadda","pump-and-press");
            var menu=Object.FindFirstObjectByType<PauseMenu>();Assert.IsTrue(menu.IsOpen,"Hadda's 'Use the pump-and-press' didn't open the bench");menu.Close();yield return Frames(2);
            var frame=All<RepairBay>().Single(r=>!r.free);StringAssert.Contains("repair and save",frame.prompt);
            Assert.Less(Flat(All<SpawnPoint>().Single(s=>s.id==frame.saveSpawn).transform.position,new Vector2(frame.transform.position.x,frame.transform.position.z)),10,"Hadda's frame saves the party far from it");
        }

        [UnityTest] public IEnumerator TheSideQuestsAreOfferedAboardAndPaidOnce()
        {
            JobsDone();Flag("hands.corraAboard");yield return Load("Arcade");
            yield return Talk(Here("Corra"));Assert.AreEqual("CorraKettleOffer",heard.Last(),"Corra doesn't offer the kettles once the jobs are done");Assert.AreEqual(0,Step("KettleTrade"));
            var shop=Object.FindFirstObjectByType<ShopUi>();if(shop.IsOpen)shop.Close();yield return Frames(2);
            yield return Talk(Here("Keeper"));Assert.AreEqual("AbriKettle",heard.Last());yield return Talk(Here("Ona"));Assert.AreEqual("OnaKettle",heard.Last());
            yield return Talk(Here("Garo"));Assert.AreEqual("GaroKettle",heard.Last());Assert.AreEqual(3,Step("KettleTrade"),"three kettles delivered in any order and the quest isn't at Corra");
            // Corra's stall price: full before the kettles are reported, hers after, and the stall says why.
            var stall=GameCatalog.Find<ShopInventory>("ManyHands_Corra");var prime=stall.offers[0];
            Assert.AreEqual(prime.price,ShopUi.Price(stall,prime,State),"Corra's own price holds before the kettles are delivered");
            int scrip=State.scrip;yield return Talk(Here("Corra"));Assert.AreEqual("CorraKettleReturn",heard.Last());Assert.IsTrue(Has("quest.KettleTrade.complete"));
            Assert.Less(ShopUi.Price(stall,prime,State),prime.price,"the kettles are delivered and Corra still charges the marked price");
            Assert.IsTrue(shop.IsOpen,"Corra's thanks doesn't open her stall");yield return Frames(2);
            var why=shop.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.name=="Discount");Assert.IsNotNull(why,"the stall's prices changed and nothing on it says why");StringAssert.Contains("CORRA",why.text);
            StringAssert.Contains(ShopUi.Price(stall,prime,State)+" scrip",string.Join("|",shop.GetComponentsInChildren<TMP_Text>(true).Select(t=>t.text)),"the stall's rows don't show Corra's price");
            if(shop.IsOpen)shop.Close();yield return Frames(2);
            yield return Talk(Here("Corra"));Assert.AreNotEqual("CorraKettleReturn",heard.Last(),"Corra pays for the kettles twice");
            // The stowaway: found behind the racks, then Teo's offer, Ruva, the decision, the thanks.
            yield return Discover(Point("hands.racksLooked"));Assert.AreEqual("JaskFound",heard.Last());Assert.IsTrue(Has("hands.jaskFound"));
            yield return Talk(Here("Teo"));Assert.AreEqual("TeoStowawayOffer",heard.Last());
            yield return Talk(Here("Ruva"));Assert.AreEqual("RuvaStowaway",heard.Last());
            yield return Choose(()=>Here("Teo").Interact(),"Teo","ferry");Assert.IsTrue(Has("jask.worksFerry")&&Has("hands.jaskPlaced"));
            yield return Load();var jask=Here("Jask");Assert.IsNotNull(jask,"Jask isn't at the ferry");Assert.Less(Flat(jask.transform.position,new Vector2(-22.4f,-26.4f)),1.5f,"Jask isn't where Teo put him");
            yield return Talk(jask);Assert.AreEqual("JaskThanksFerry",heard.Last());Assert.IsTrue(Has("quest.StowawayFare.complete"));
            yield return Talk(jask);Assert.AreEqual("JaskFerryHand",heard.Last(),"Jask thanks the party twice");
        }
    }
}
