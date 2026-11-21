using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The replay buys the floor parts it is short of at the chapter's own stall, through the real shop screen, with the step the routes buy gels with: a stock step on a
    /// part's row, to a count of that part the pair out holds (ContinuousReview.BeginPartStock). The replay's party reached chapter 3 with Scrap Frames and no Drive, chapter 4
    /// with one Prime Module for two and chapter 7 with Prime modules (the chain's saves replayed with wearBest, November 15): routes 16, 21 and 39 now buy the Refined set at
    /// Corra's, a Prime Module from Quill (found among his answers) and two Tempered Modules from Lume, each with the scrip its route has there, and the driver puts them on as the
    /// stall closes. Each route's own steps are read from the file its generator writes. Shown failing first as the driver was (a stock step for consumables only,
    /// ContinuousReview.partStockOff); and with too little scrip the step fails and says what it had.</summary>
    public sealed class PartPurchaseTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject fixture;ContinuousReview replay;readonly List<string> log=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        void Logged(string text,string stack,LogType type){if(text.StartsWith("QUALITY_STOCK")||text.StartsWith("QUALITY_WEARS"))log.Add(text);}
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            log.Clear();Application.logMessageReceived+=Logged;AsItWas(false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;AsItWas(false);
            var shop=Object.FindFirstObjectByType<ShopUi>();if(shop!=null&&shop.IsOpen)shop.Close();
            if(DialogueSystem.Current!=null&&DialogueSystem.Current.Running)DialogueSystem.Current.Cancel();
            DropDriver();
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the driver, by its private parts
        void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
        T Get<T>(string name)=>(T)typeof(ContinuousReview).GetField(name,Private).GetValue(replay);
        object Call(string name,params object[] arguments)=>typeof(ContinuousReview).GetMethod(name,Private).Invoke(replay,arguments);
        List<string> Failures=>Get<List<string>>("failures");
        static void AsItWas(bool off)=>typeof(ContinuousReview).GetField("partStockOff",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,off);
        void DropDriver()
        {
            if(replay!=null){var pad=Get<Gamepad>("pad");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);}
            if(fixture!=null)Object.Destroy(fixture);fixture=null;replay=null;
        }
        IEnumerator Play(QualityRoute route,float most)
        {
            DropDriver();fixture=new GameObject("Part purchase fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();
            Set("pad",InputSystem.AddDevice<Gamepad>("PartPurchasePad"));Call("InputUpdate");yield return null;yield return null;
            Set("route",route);Set("recording",true);Get<System.Diagnostics.Stopwatch>("clock").Start();
            var run=(IEnumerator)Call("RunSteps");
            for(float end=Time.unscaledTime+most;Time.unscaledTime<end;){if(!run.MoveNext())break;Call("InputUpdate");yield return null;}
            Set("recording",false);Call("InputUpdate");
        }

        // ------------------------------------------------------------------ the routes, read
        static string Folder=>System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
        static QualityRoute Read(string name)=>JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(System.IO.Path.Combine(Folder,name+".json")));
        /// <summary>The route's own steps from one named step to another, with its hands, and a moment standing in the world after (where the driver puts on what was bought).</summary>
        static QualityRoute Slice(string route,string from,string to)
        {
            var whole=Read(route);int a=System.Array.FindIndex(whole.steps,s=>s.name==from);Assert.GreaterOrEqual(a,0,$"fixture: {route} has no step '{from}'");
            int b=System.Array.FindIndex(whole.steps,a,s=>s.name==to);Assert.GreaterOrEqual(b,a,$"fixture: {route} has no step '{to}' after '{from}'");
            var steps=whole.steps.Skip(a).Take(b-a+1).Concat(new[]{new QualityStep{name="a moment after the stall",seconds=2}}).ToArray();
            return new QualityRoute{name=whole.name+" at the stall",scene=whole.steps[a].expectedScene,autoHeal=whole.autoHeal,useExtra=whole.useExtra,wearBest=whole.wearBest,deployed=whole.deployed,steps=steps};
        }
        static string[] FlagsBefore(int first,string route,string step)
        {
            var flags=new HashSet<string>();
            foreach(string name in System.IO.Directory.GetFiles(Folder,"chapter-*.json").Select(System.IO.Path.GetFileNameWithoutExtension)
                .Where(n=>int.TryParse(n.Split('-')[1],out int k)&&k>=first&&string.CompareOrdinal(n,route)<=0).OrderBy(n=>n,System.StringComparer.Ordinal))
                foreach(var s in Read(name).steps)
                {
                    if(name==route&&s.name==step)return flags.ToArray();
                    if(!string.IsNullOrEmpty(s.expectedFlag))flags.Add(s.expectedFlag);
                    if(!string.IsNullOrEmpty(s.until)&&s.until.StartsWith("flag:"))flags.Add(s.until.Substring(5));
                }
            Assert.Fail($"fixture: {route} has no step '{step}'");return null;
        }

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static MemberState Member(string id)=>State.party.Find(m=>m.id==id);
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static int Held(string part)=>ContinuousReview.PartsHeld(State,GameCatalog.Find<TechPartDef>(part));
        static int Price(ShopInventory stall,string part)=>ShopUi.Price(stall,stall.offers.First(o=>o.id==part),State);
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+40;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            yield return new WaitForSecondsRealtime(1);
            if(PartyController.Current.Active.character!="Taren")PartyController.Current.Swap();yield return Frames(3);
        }
        static void Put(CombatActor actor,Vector3 at)
        {
            if(UnityEngine.AI.NavMesh.SamplePosition(at,out var hit,3,UnityEngine.AI.NavMesh.AllAreas))at=hit.position;
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        /// <summary>Takes every one of a part off whoever wears it and out of the bag: the fixture's party made to hold what the chain's did.</summary>
        static void Without(string part)
        {
            var inventory=new Inventory(State);
            foreach(var p in State.parts.Where(p=>p.definitionId==part).ToArray()){inventory.Unequip(p);State.parts.Remove(p);}
            PartyController.Current.RefreshStats();
        }
        /// <summary>A stall opened as a conversation opens it (on its page toggle), the stall a route's talk ends at.</summary>
        static ShopInventory Open(string zone,string seller)
        {
            var stall=ShopUi.For(zone,seller);Assert.IsNotNull(stall,"fixture: "+zone+" has "+seller+"'s stall");
            Object.FindFirstObjectByType<ShopUi>().Open(stall,seller);return stall;
        }
        static bool Wears(string hero,string part)=>Member(hero).equipped.Values.Concat(new[]{Member(hero).module2}).Any(id=>State.parts.Any(p=>p.instanceId==id&&p.definitionId==part));

        // ================================================================== route 16: the Refined set at Corra's
        const string Corra="chapter-16-lantern-home";
        /// <summary>At route 16's stall the chain's party had 1,734 scrip after the relay and the three errands' 370 (Ishen's 150, Pell's 120, Brekk's 100), Refined weapons and
        /// nothing else of the tier. Chapter 2's loadout stands for it (the Refined weapon alone).</summary>
        IEnumerator AtCorras(int scrip)
        {
            DevLoadout.Apply("chapter2");yield return Zone("Lanternwick");
            State.scrip=scrip;foreach(string part in new[]{"FramesT2","DrivesT2","ModulesT2"})Assert.AreEqual(0,Held(part),"fixture: the pair holds no "+part);
            Open("Lanternwick","Corra");yield return Frames(3);
            Assert.IsTrue(Object.FindFirstObjectByType<ShopUi>().IsOpen,"fixture: Corra's stall is open");
        }
        [UnityTest] public IEnumerator RouteSixteenBuysTheRefinedSetAtCorrasWithTheScripItHasAndWearsItAsTheStallCloses()
        {
            const int Scrip=1734+370;
            // As the driver was: a stock step on a part's row is refused, and the step's button is one held press, as route 13 bought its one Refined Emitter: one Frame, not two.
            AsItWas(true);
            yield return AtCorras(Scrip);
            yield return Play(Slice(Corra,"look over her stock","release her stall"),90);
            Assert.Less(Held("FramesT2"),2,"fixture: the driver as it was bought both Refined Frames: the check below proves nothing");
            Assert.IsTrue(Failures.Any(f=>f.Contains("Refined Frame")),"fixture: the driver as it was said nothing of the Refined Frame's row");
            AsItWas(false);yield return Cleanup();yield return Boot();
            yield return AtCorras(Scrip);var stall=ShopUi.For("Lanternwick","Corra");
            int cost=2*Price(stall,"FramesT2")+2*Price(stall,"DrivesT2")+Price(stall,"ModulesT2");
            yield return Play(Slice(Corra,"look over her stock","release her stall"),60);
            string said=string.Join(" | ",log)+" || "+string.Join(" | ",Failures);
            CollectionAssert.IsEmpty(Failures,"route 16's stall steps failed with "+Scrip+" scrip: "+said);
            Assert.AreEqual(2,Held("FramesT2"),"route 16 didn't buy two Refined Frames: "+said);
            Assert.AreEqual(2,Held("DrivesT2"),"route 16 didn't buy two Refined Drives: "+said);
            Assert.AreEqual(1,Held("ModulesT2"),"route 16 didn't buy one Refined Module: "+said);
            Assert.AreEqual(Scrip-cost,State.scrip,"route 16 spent other than the Refined set's price: "+said);
            Assert.IsFalse(Object.FindFirstObjectByType<ShopUi>().IsOpen,"the stall was left open");
            foreach(string hero in new[]{"Taren","Sela"})
            {
                Assert.IsTrue(Wears(hero,"FramesT2"),hero+" doesn't wear a Refined Frame once the stall has closed: "+said);
                Assert.IsTrue(Wears(hero,"DrivesT2"),hero+" doesn't wear a Refined Drive once the stall has closed: "+said);
            }
            Assert.IsTrue(Wears("Taren","ModulesT2")||Wears("Sela","ModulesT2"),"nobody wears the Refined Module once the stall has closed: "+said);
        }
        /// <summary>With 700 scrip the two Frames are bought (520) and the first Drive is refused: the step fails, saying what it bought, what the pair needed and what was in hand.</summary>
        [UnityTest] public IEnumerator WithTooLittleScripTheStepThatBuysAPartFailsAndSaysWhatItHad()
        {
            yield return AtCorras(700);
            yield return Play(Slice(Corra,"look over her stock","release her stall"),60);
            Assert.AreEqual(2,Held("FramesT2"),"fixture: 700 scrip didn't buy the two Refined Frames: "+string.Join(" | ",log));
            Assert.AreEqual(0,Held("DrivesT2"),"fixture: a Refined Drive was bought with what was left");
            var short_=Failures.FirstOrDefault(f=>f.Contains("Refined Drive"));
            Assert.IsNotNull(short_,"a part the route set out to buy wasn't bought, and the step didn't fail: "+string.Join(" | ",Failures));
            StringAssert.Contains("said no with 180 scrip left of 180",short_,"the failure doesn't say what the party had to spend");
        }

        // ================================================================== route 21: a Prime Module among Quill's answers
        const string Quill="chapter-21-driftreed";
        [UnityTest] public IEnumerator RouteTwentyOneFindsQuillsPartsAmongHisAnswersAndBuysThePrimeModuleThePairLacks()
        {
            DevLoadout.Apply("chapter4");
            foreach(string flag in FlagsBefore(20,Quill,"greet Quill"))GameServices.Current.Flags.SetBool(flag,true);
            yield return Zone("Driftreed");
            // As the chain had it: one Prime Module between the two (Sela's taken), and the scrip after chapter 2's Refined set.
            var sela=State.parts.First(p=>p.instanceId==Member("Sela").equipped[GearSlot.Module]);new Inventory(State).Unequip(sela);State.parts.Remove(sela);
            State.scrip=4549-1240;Assert.AreEqual(1,Held("ModulesT3"),"fixture: the pair holds one Prime Module");
            var whole=Read(Quill);var walk=whole.steps.Last(s=>s.navigate&&System.Array.IndexOf(whole.steps,s)<System.Array.FindIndex(whole.steps,t=>t.name=="greet Quill"));
            Put(Lead,walk.point);Put(PartyController.Current.members.First(m=>m!=Lead),walk.point+new Vector3(1.5f,0,-1.5f));yield return new WaitForSecondsRealtime(.8f);
            int price=Price(ShopUi.For("Driftreed","Quill"),"ModulesT3"),before=State.scrip;
            yield return Play(Slice(Quill,"greet Quill","release Quill's counter"),90);
            string said=string.Join(" | ",log)+" || "+string.Join(" | ",Failures);
            CollectionAssert.IsEmpty(Failures,"route 21's steps at Quill failed: "+said);
            Assert.AreEqual(2,Held("ModulesT3"),"route 21 didn't buy the Prime Module the pair lacked at Quill's counter: "+said);
            Assert.AreEqual(before-price,State.scrip,"route 21 spent other than one Prime Module at Quill's: "+said);
            Assert.IsTrue(Wears("Sela","ModulesT3"),"Sela doesn't wear the Prime Module once Quill's counter has closed: "+said);
        }

        // ================================================================== the Choirwrought set before chapter 10's bosses (D326)
        /// <summary>Chapter 10 is sized on Choirwrought in every slot ("from chapter 9's stalls", DevLoadout), and until November 15 nothing sold any before Neighbourhood Afloat,
        /// after the last boss. Mira's stall on the Decks opens her road crates once the Decks have voted to go, and Corra's stall at Many Hands stocks the convoy's once chapter 9
        /// closes at the fleet's chart (route 62 passes it): the plain part of every slot, on the ladder (Neighbourhood Afloat's three prices, the Frame and Drive by the same step),
        /// each stall saying why on its page, and none of it before.</summary>
        [UnityTest] public IEnumerator TheChoirwroughtSetIsOnSaleOnTheDecksOnceTheyVoteToGoAndAtCorrasOnceTheConvoyIsIn()
        {
            yield return null;
            var ladder=new Dictionary<string,int>{["EmittersT5"]=1480,["EdgesT5"]=1560,["FramesT5"]=1620,["DrivesT5"]=1420,["ModulesT5"]=1600};
            var afloat=GameCatalog.Find<ShopInventory>("NeighbourhoodAfloat");Assert.IsNotNull(afloat,"fixture: Neighbourhood Afloat's stall");
            foreach(var o in afloat.offers.Where(o=>ladder.ContainsKey(o.id)))Assert.AreEqual(ladder[o.id],o.price,$"fixture: Neighbourhood Afloat's {o.id} isn't the ladder's price");
            foreach(var (stall,flag) in new[]{("DecksLeaving_Mira","home.meetingHeld"),("ManyHands_Corra","chapter9.complete")})
            {
                var inventory=GameCatalog.Find<ShopInventory>(stall);Assert.IsNotNull(inventory,"fixture: "+stall);
                var before=new GameState();var after=new GameState();after.flags[flag]=true;
                var early=ShopUi.Stock(inventory,before).Where(o=>ladder.ContainsKey(o.id)).Select(o=>o.id).ToArray();
                CollectionAssert.IsEmpty(early,$"{stall} sells Choirwrought before {flag}");
                var sold=ShopUi.Stock(inventory,after).Where(o=>ladder.ContainsKey(o.id)).ToDictionary(o=>o.id,o=>o.price);
                CollectionAssert.AreEquivalent(ladder.Keys,sold.Keys,$"{stall} doesn't sell the plain Choirwrought part of every slot once {flag} is set (it sells {string.Join(", ",sold.Keys)})");
                foreach(var p in ladder)Assert.AreEqual(p.Value,sold[p.Key],$"{stall} asks {sold[p.Key]} for {p.Key}, off the ladder's {p.Value}");
                // A step up the ladder: dearer than the same slot's Tempered part wherever that is sold.
                foreach(var p in ladder){var t4=GameCatalog.Find<ShopInventory>("DecksLeaving_Mira").offers.Where(o=>o.id==p.Key.Replace("T5","T4")).Select(o=>(int?)o.price).FirstOrDefault();if(t4.HasValue)Assert.Greater(p.Value,t4.Value*1.4f,$"{p.Key} is barely dearer than its Tempered part's {t4.Value}");}
                Assert.IsFalse(ShopUi.Noted(inventory,before),$"{stall} says what came to its counter before it came");
                Assert.IsTrue(ShopUi.Noted(inventory,after)&&inventory.note.Contains("CHOIRWROUGHT"),$"{stall} doesn't say on its page why Choirwrought is on the counter");
            }
        }

        // ================================================================== route 62: the Choirwrought Edge and Module at Corra's
        const string OnePlan="chapter-62-one-plan";
        /// <summary>Route 62 passes Corra's stall on its way down to the hull pad, the convoy's Choirwrought on her counter. The chain's pair reached chapter 10 with a Choirwrought Pulse Edge
        /// on Taren (87 against the plain 95) and Tempered modules on Idra: the route buys the plain Edge (the weapon first) and the Module the pair lacks, with the scrip the chain had
        /// (18,325 after the fleet's chart, less chapters 2 to 7's purchases).</summary>
        [UnityTest] public IEnumerator RouteSixtyTwoBuysTheChoirwroughtEdgeAndModuleThePairLacksAtCorras()
        {
            DevLoadout.Apply("chapter10");
            foreach(string flag in FlagsBefore(62,OnePlan,"start Corra's stall"))GameServices.Current.Flags.SetBool(flag,true);
            yield return Zone("ManyHands");
            var inventory=new Inventory(State);
            void Swap(string hero,GearSlot slot,string to){var m=Member(hero);var worn=State.parts.First(p=>p.instanceId==m.equipped[slot]);inventory.Unequip(worn);State.parts.Remove(worn);var def=GameCatalog.Find<TechPartDef>(to);inventory.Equip(m,inventory.GivePart(def),def);}
            Swap("Taren",GearSlot.Edge,"EdgesT5Pulse");Swap("Idra",GearSlot.Module,"ModulesT4");PartyController.Current.RefreshStats();
            Assert.AreEqual(1,Held("EdgesT5"),"fixture: the pair holds one plain Choirwrought Edge (Idra's)");Assert.AreEqual(1,Held("ModulesT5"),"fixture: the pair holds one Choirwrought Module (Taren's)");
            State.scrip=18325-1240-640-2080;
            var whole=Read(OnePlan);int at=System.Array.FindLastIndex(whole.steps,s=>s.name.StartsWith("to Corra's stall "));Assert.GreaterOrEqual(at,0,"fixture: route 62 walks to Corra's stall");
            Put(Lead,whole.steps[at].point);Put(PartyController.Current.members.First(m=>m!=Lead),whole.steps[at].point+new Vector3(1.5f,0,-1.5f));yield return new WaitForSecondsRealtime(.8f);
            var stall=ShopUi.For("ManyHands","Corra");int cost=Price(stall,"EdgesT5")+Price(stall,"ModulesT5"),before=State.scrip;
            yield return Play(Slice(OnePlan,"start Corra's stall","release the stall after Corra's stall"),90);
            string said=string.Join(" | ",log)+" || "+string.Join(" | ",Failures);
            CollectionAssert.IsEmpty(Failures,"route 62's steps at Corra's failed: "+said);
            Assert.AreEqual(2,Held("EdgesT5"),"route 62 didn't buy the plain Choirwrought Edge the pair lacked: "+said);
            Assert.AreEqual(2,Held("ModulesT5"),"route 62 didn't buy the Choirwrought Module the pair lacked: "+said);
            Assert.AreEqual(before-cost,State.scrip,"route 62 spent other than an Edge and a Module at Corra's: "+said);
            Assert.IsTrue(Wears("Taren","EdgesT5"),"Taren doesn't wear the plain Choirwrought Edge once the stall has closed: "+said);
            Assert.IsTrue(Wears("Idra","ModulesT5"),"Idra doesn't wear the Choirwrought Module once the stall has closed: "+said);
        }

        // ================================================================== route 39: two Tempered Modules at Lume's, after the gels
        const string Lume="chapter-39-skyfields";
        [UnityTest] public IEnumerator RouteThirtyNineBuysTwoTemperedModulesAtLumesAfterItsGelsAndNotForSelaResting()
        {
            DevLoadout.Apply("chapter7");yield return Zone("OrielSkyfields");
            // As the chain had it: Taren and Idra out with Prime modules, Sela resting in the Tempered one she wore (which the pair can't count on).
            var inventory=new Inventory(State);
            foreach(string hero in new[]{"Taren","Idra"})
            {
                var m=Member(hero);var worn=State.parts.First(p=>p.instanceId==m.equipped[GearSlot.Module]);inventory.Unequip(worn);State.parts.Remove(worn);
                var prime=inventory.GivePart(GameCatalog.Find<TechPartDef>("ModulesT3"));inventory.Equip(m,prime,GameCatalog.Find<TechPartDef>("ModulesT3"));
            }
            PartyController.Current.RefreshStats();
            Assert.IsTrue(Wears("Sela","ModulesT4"),"fixture: Sela rests in a Tempered Module");Assert.AreEqual(0,Held("ModulesT4"),"fixture: the pair out holds no Tempered Module");
            State.scrip=9819-1240-640;State.consumables["RepairGel"]=4;
            var stall=Open("OrielSkyfields","Lume");yield return Frames(3);
            int gels=(Inventory.Cap(GameCatalog.Find<ConsumableDef>("RepairGel"))-4)*Price(stall,"RepairGel"),modules=2*Price(stall,"ModulesT4"),before=State.scrip;
            yield return Play(Slice(Lume,"find the repair gel","release Lume's stall"),60);
            string said=string.Join(" | ",log)+" || "+string.Join(" | ",Failures);
            CollectionAssert.IsEmpty(Failures,"route 39's stall steps failed: "+said);
            Assert.AreEqual(Inventory.Cap(GameCatalog.Find<ConsumableDef>("RepairGel")),State.consumables["RepairGel"],"the gels weren't bought first, to the cap: "+said);
            Assert.AreEqual(2,Held("ModulesT4"),"route 39 didn't buy two Tempered Modules for the pair out: "+said);
            Assert.AreEqual(before-gels-modules,State.scrip,"route 39 spent other than the gels and two Tempered Modules: "+said);
            Assert.IsTrue(Wears("Taren","ModulesT4")&&Wears("Idra","ModulesT4"),"Taren and Idra don't each wear a Tempered Module once Lume's stall has closed: "+said);
        }
    }
}
