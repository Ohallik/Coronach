using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Three in the party, two out (chapter 6 on). The save says which two (GameState.deployed, read by Deployment); the one resting has no body, so a fight, a
    /// swap, the HUD's partner line and the replay driver are only ever about the pair; the Party page chooses the pair somewhere safe or at a bench and refuses in
    /// the open; the driver finds the partner by role, gives up on a lead it can't have and says why, and brings a rester out with the pad where the game lets a
    /// player; and the chapter loadouts from 7 arrive with the third hero.
    /// <para>The third hero is Idra once her CharacterDef exists (IdraBuilder.Build writes it). Until then she is stood in for by Steady Hand ("Patrol"): a real
    /// CharacterDef with no body and no skills, the least the game will stand up as a hero (a capsule). Every check but the two-hero guard failed on the tree before
    /// the deployed pair was wired in.</para></summary>
    public sealed class PartyOfThreeTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject fixture;ContinuousReview replay;bool finished;Gamepad menuPad;
        readonly List<string> folders=new(),log=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        static readonly string[] Slots={"autosave","slot1","slot2","slot3"};
        void Logged(string text,string stack,LogType type){if(text.StartsWith("DEV_LOADOUT")||text.StartsWith("PARTY_")||text.StartsWith("QUALITY_")||text.StartsWith("SAVE_"))log.Add(text);}
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            // Headless has no focused Game view. Route virtual devices into play.
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            ClearSaves();DevLoadout.Apply("starter");
            // Never active: the driver's Start wants a player's launch arguments. Its steps, its pad and its decisions are driven from here, a frame at a time.
            fixture=new GameObject("Party of three replay fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();
            log.Clear();finished=false;Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            // Unity does not call OnDestroy on a never-active fixture: its pad is taken away here, with whatever it held.
            if(replay!=null){var pad=Get<Gamepad>("pad");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);Set("pad",null);}
            if(menuPad!=null&&menuPad.added)InputSystem.RemoveDevice(menuPad);menuPad=null;PromptService.ForceDevice(null);
            if(fixture!=null)Object.Destroy(fixture);
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null){GameServices.Current.Input.Blocked=false;GameServices.Current.Saves.ResumeFrom("autosave");ClearSaves();}
            foreach(string folder in folders)if(Directory.Exists(folder))Directory.Delete(folder,true);
            folders.Clear();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static void ClearSaves()
        {
            // The batch runner's save shield holds the player's real saves elsewhere for the whole run.
            if(!Application.isBatchMode)return;
            foreach(var slot in Slots)foreach(var suffix in new[]{"",".bak",".tmp"})
            {var path=GameServices.Current.Saves.PathForSlot(slot)+suffix;if(File.Exists(path))File.Delete(path);}
            GameServices.Current.Saves.ResumeFrom("autosave");
        }

        // ------------------------------------------------------------------ the driver, by its private parts
        void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
        T Get<T>(string name)=>(T)typeof(ContinuousReview).GetField(name,Private).GetValue(replay);
        object Call(string name,params object[] arguments)=>typeof(ContinuousReview).GetMethod(name,Private).Invoke(replay,arguments);
        bool Condition(string name)=>(bool)Call("Condition",name);
        List<string> Failures=>Get<List<string>>("failures");
        string Failed=>string.Join(" | ",Failures);
        /// <summary>Gives the driver its pad, its route and a running clock, as its Start does once the settle has let the scene's own input go by.</summary>
        IEnumerator Hands(QualityRoute route)
        {
            Set("pad",InputSystem.AddDevice<Gamepad>("PartyOfThreePad"));
            Call("InputUpdate");yield return null;yield return null;
            Set("route",route);Set("recording",true);Get<System.Diagnostics.Stopwatch>("clock").Start();
        }
        /// <summary>Holds the driver on its current step, feeding the pad each frame, until the time is up or the thing looked for has happened.</summary>
        IEnumerator Drive(float seconds,System.Func<bool> until=null,System.Action each=null)
        {
            for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end&&(until==null||!until());){each?.Invoke();Call("InputUpdate");yield return null;}
        }
        /// <summary>Plays a whole route's steps as the player does (RunSteps), a frame at a time.</summary>
        IEnumerator Play(QualityRoute route,float most,System.Action each=null)
        {
            yield return Hands(route);var run=(IEnumerator)Call("RunSteps");finished=false;
            for(float end=Time.unscaledTime+most;Time.unscaledTime<end;)
            {
                if(!run.MoveNext()){finished=true;break;}
                each?.Invoke();Call("InputUpdate");yield return null;
            }
        }
        /// <summary>Counts the driver's presses of Swap: the pad's own button, as the game reads it.</summary>
        System.Action SwapPresses(int[] count)
        {
            bool down=false;
            return ()=>{var pad=Get<Gamepad>("pad");bool now=pad!=null&&pad.buttonNorth.isPressed;if(now&&!down)count[0]++;down=now;};
        }

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static PartyController Party=>PartyController.Current;
        static PauseMenu Menu=>Object.FindFirstObjectByType<PauseMenu>();
        /// <summary>Idra, or until her definition lands the least the game will stand up in a hero's place: a CharacterDef with no body and no skills.</summary>
        static string Third=>GameCatalog.Find<CharacterDef>(DevLoadout.ThirdHero)!=null?DevLoadout.ThirdHero:"Patrol";
        static string Named(string id)=>DisplayNames.Speaker(id);
        static MemberState Member(string id)=>State.party.Find(m=>m.id==id);
        /// <summary>The third hero in the save as a conversation's join leaves them: at the party's Sync, whole, wearing what their definition gives.</summary>
        static MemberState Recruit()
        {
            Assert.AreEqual(2,State.party.Count,"fixture: the starter party is Taren and Sela");
            int level=State.party[0].level;var joined=new MemberState{id=Third,level=level,integrity=Levels.MaxIntegrity(level)};
            State.party.Add(joined);GameServices.GiveStartingGear(State,joined);return joined;
        }
        static string[] Field()=>Party.members.Select(m=>m.character).ToArray();
        static string[] Out()=>Deployment.Of(State).Select(m=>m.id).ToArray();
        /// <summary>Every hero's body in the scene, shown or not.</summary>
        static string[] Bodies()=>Object.FindObjectsByType<CombatActor>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(a=>a.character).OrderBy(c=>c).ToArray();
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Zone(string zone)
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone(zone);deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var hero in Party.members){hero.GetComponent<PartnerBrain>().enabled=false;hero.Health.InvulnerableUntil=0;}
            yield return new WaitForSecondsRealtime(.7f);
        }
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=position;body.enabled=true;Physics.SyncTransforms();
        }
        static void Fell(CombatActor hero){hero.Health.InvulnerableUntil=0;hero.Health.Receive(new DamagePacket{amount=hero.Health.maximum*100,type=DamageType.Pulse});}
        /// <summary>A third body on the field, put there by hand as a join used to put one: what the driver and the captures must survive, though the game no longer does it.</summary>
        static CombatActor ThreeOnTheField()
        {
            var party=Party;var extra=ActorFactory.Hero(Third,party.Active.transform.position+Vector3.right*4);extra.transform.SetParent(party.transform);
            party.members=party.members.Append(extra).ToArray();party.Apply();
            foreach(var hero in party.members)hero.GetComponent<PartnerBrain>().enabled=false;
            return extra;
        }
        static Button[] Buttons()=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        static Button Row(string name)=>Buttons().FirstOrDefault(b=>b.name==name);
        static Button HeroRow(string id){var row=Row(PartyPairUi.RowName(id));Assert.IsNotNull(row,$"the Party page has no row to choose {Named(id)} by. On screen: {string.Join(" | ",Buttons().Select(b=>b.name+"="+Words(b)))}");return row;}
        static string Words(Component c){var t=c!=null?c.GetComponentInChildren<TMP_Text>():null;return t!=null?t.text:"";}
        static string Label(string name){var t=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(l=>l.name==name);return t!=null?t.text:"";}
        static string PartnerLine()
        {
            var canvas=Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(c=>c.name=="HUD");
            var line=canvas.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.name=="Partner");Assert.IsNotNull(line,"the HUD has no partner line");return line.text;
        }
        /// <summary>The driver as a second route finds it: its clock at nought, no pad, nothing remembered of the last one's presses.</summary>
        void Again()
        {
            Failures.Clear();Set("recording",false);Set("lastSwapPress",-1.0);Get<System.Diagnostics.Stopwatch>("clock").Reset();
            var pad=Get<Gamepad>("pad");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);Set("pad",null);
        }
        SaveSystem Folder()
        {
            string folder=Path.Combine(Application.temporaryCachePath,"party-of-three-"+System.Guid.NewGuid().ToString("N"));folders.Add(folder);
            Directory.CreateDirectory(folder);return new SaveSystem(folder);
        }
        /// <summary>A save as November wrote it, two heroes and no word of a pair, with Sela leading. Its quotes are swapped so it reads here.</summary>
        const string TwoHeroSave="{'version':2,'savedAtUtc':'2026-11-04T22:41:09.5120000Z','zone':'Arena_Ground','spawn':'Arrival','activeMember':1,'quickItem':'RepairGel',"+
            "'party':[{'id':'Taren','level':6,'xp':0,'integrity':210.0,'charge':0.0,'gear':[],'equipped':{},'module2':null},{'id':'Sela','level':6,'xp':0,'integrity':180.0,'charge':0.0,'gear':[],'equipped':{},'module2':null}],"+
            "'flags':{},'materials':{},'collectedMaterials':{},'consumables':{'RepairGel':2},'craftingXp':{},'questSteps':{},'questCounts':{},'parts':[],'scrip':640,'playtime':3120.5}";

        // ------------------------------------------------------------------ the field
        [UnityTest] public IEnumerator OnlyTwoStandInAPlaceWithFightsAndTheOneRestingHasNoBody()
        {
            var third=Recruit();third.integrity=77;
            yield return Zone("Arena_Ground");
            Assert.IsTrue(ZoneController.Current.Combat,"fixture: the yard is a place with fights");
            Assert.AreEqual(3,State.party.Count,"fixture: three in the party");
            CollectionAssert.AreEqual(new[]{"Taren","Sela"},Field(),"with nobody chosen, the two who were there are the two out");
            CollectionAssert.AreEqual(new[]{"Sela","Taren"},Bodies(),Named(Third)+" is resting and still has a body in a place with fights");
            StringAssert.Contains(Named("Sela").ToUpperInvariant(),PartnerLine(),"the HUD's partner line isn't the hero out beside the lead");
            yield return Frames(5);
            Assert.AreEqual(77,third.integrity,.01f,"a hero resting had their Integrity written over by somebody on the field");
            // Nobody standing is the two out, not the three in the save: a rester can't hold the defeat screen back.
            foreach(var hero in Party.members)Fell(hero);
            float deadline=Time.unscaledTime+8;var defeat=Object.FindFirstObjectByType<DefeatPanel>();
            while(!defeat.IsOpen&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(defeat.IsOpen,"both heroes out are down and the defeat screen never came: the one resting was counted as standing");
            // The one resting doesn't come in: a retry starts the same pair over, everyone whole, as a retry of two does.
            CollectionAssert.AreEqual(new[]{"Taren","Sela"},Field(),"a hero came out of rest when the two out fell");
            var retry=Row("Retry");Assert.IsNotNull(retry,"fixture: the defeat screen offers a retry");retry.onClick.Invoke();
            deadline=Time.unscaledTime+25;yield return null;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            yield return Frames(5);
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: the retry never finished loading");
            CollectionAssert.AreEqual(new[]{"Taren","Sela"},Field(),"the retry didn't start the pair that fell over");
            foreach(var member in State.party)Assert.AreEqual(Levels.MaxIntegrity(member.level),member.integrity,.5f,Named(member.id)+" isn't whole after a retry");
        }
        [UnityTest] public IEnumerator SwapGoesBetweenTheTwoOutAndNeverLandsOnTheOneResting()
        {
            Recruit();Member("Sela").integrity=55;
            Assert.IsTrue(Deployment.Set(State,new[]{"Taren",Third}),"fixture: the pair is named");
            yield return Zone("Arena_Ground");
            CollectionAssert.AreEqual(new[]{"Taren",Third},Field(),"the pair the save names isn't the pair on the field");
            Assert.AreEqual("Taren",Party.Active.character,"fixture: Taren leads");
            for(int press=0;press<5;press++)
            {
                string was=Party.Active.character,next=was=="Taren"?Third:"Taren";
                Assert.IsTrue(Party.Swap(),"a swap between the two out was refused");yield return null;yield return null;
                Assert.AreEqual(next,Party.Active.character,$"swap {press+1} went from {was} to somebody who isn't the other hero out");
                Assert.AreEqual(next,State.party[State.activeMember].id,"the save's lead isn't the hero leading: a reload would put somebody else in front");
                StringAssert.Contains(Named(was).ToUpperInvariant(),PartnerLine(),"the HUD's partner line isn't the other hero out");
                StringAssert.DoesNotContain(Named("Sela").ToUpperInvariant(),PartnerLine(),"the HUD's partner line names the hero resting");
            }
            Assert.AreEqual(55,Member("Sela").integrity,.01f,"the hero resting had their Integrity written over by the hero who took their place in the party's order");
            // With the partner down a swap is refused, as it always was: it doesn't go looking for the rester.
            Fell(Party.Partner);yield return null;
            string lead=Party.Active.character;Assert.IsFalse(Party.Swap(),"a swap went through with the only other hero out down");Assert.AreEqual(lead,Party.Active.character);
        }
        [UnityTest] public IEnumerator AHeroWhoJoinsAFullFieldComesOutInThePartnersPlace()
        {
            yield return Zone("Arena_Ground");
            Party.members.First(m=>m.character=="Sela").Health.integrity=61;yield return null;
            Recruit();
            var bridge=Object.FindFirstObjectByType<WorldUiBridge>();Assert.IsNotNull(bridge,"fixture: the zone has its bridge to the conversation");
            typeof(WorldUiBridge).GetMethod("Join",Private).Invoke(bridge,new object[]{Third});
            yield return Frames(3);
            CollectionAssert.AreEqual(new[]{"Taren",Third},Field(),"a third hero who joins isn't out beside the lead");
            CollectionAssert.AreEqual(new[]{Third,"Taren"}.OrderBy(c=>c).ToArray(),Bodies(),"the hero who made room still has a body");
            Assert.AreEqual("Taren",Party.Active.character,"joining changed who leads");
            Assert.AreEqual(61,Member("Sela").integrity,.5f,"the hero who went to rest didn't keep what she had");
            // She shapes in as anyone arriving does: given the seconds that takes, she is in the zone's form.
            var form=Party.members[1].GetComponent<FormController>();float settled=Time.unscaledTime+8;
            while(form.Current!=ZoneController.Current.Form&&Time.unscaledTime<settled)yield return null;
            Assert.AreEqual(ZoneController.Current.Form,form.Current,"the hero who came out never took the zone's form");
            Assert.IsTrue(Party.Swap(),"the hero who joined can't be swapped to");Assert.AreEqual(Third,Party.Active.character);
        }

        // ------------------------------------------------------------------ the Party page
        [UnityTest] public IEnumerator ThePairIsChosenOnThePartyPageSomewhereSafeAndAtABenchAndRefusedInTheOpenWithItsReason()
        {
            Recruit();yield return Zone("Hub_Decks");
            Assert.IsFalse(ZoneController.Current.Combat,"fixture: the Decks are safe");
            var menu=Menu;menu.Open(PauseMenu.PartyPage);yield return null;
            foreach(string hero in new[]{"Taren","Sela",Third})HeroRow(hero);
            // The rows under the heroes keep the names every route and test finds them by.
            StringAssert.StartsWith("Swap active member",Words(Row("Row_2")),"Row_2 isn't the swap with three in the party");
            for(int slot=1;slot<=3;slot++)StringAssert.StartsWith("Save to slot "+slot,Words(Row("Row_"+(slot+2))),"the save rows moved with three in the party");
            StringAssert.StartsWith(PartyPairUi.RestingMark,Words(HeroRow(Third)),"the hero resting isn't marked");
            StringAssert.DoesNotContain(PartyPairUi.RestingMark,Words(HeroRow("Taren"))+Words(HeroRow("Sela")),"a hero out is marked as resting");
            // Somewhere safe: the one resting comes out in the partner's place, and the lead stays the lead.
            HeroRow(Third).onClick.Invoke();yield return Frames(3);
            CollectionAssert.AreEqual(new[]{"Taren",Third},Out(),"a press on the rester's row in a safe zone didn't bring them out");
            CollectionAssert.AreEqual(new[]{"Taren",Third},Field(),"the field isn't the pair just chosen");
            CollectionAssert.AreEqual(new[]{Third,"Taren"}.OrderBy(c=>c).ToArray(),Bodies(),"the hero sent to rest still has a body");
            Assert.AreEqual("Taren",Party.Active.character,"bringing a hero out changed who leads");
            StringAssert.Contains(Named(Third)+" is out",Label("Feedback"),"the page didn't say what the press did");
            StringAssert.StartsWith(PartyPairUi.RestingMark,Words(HeroRow("Sela")),"the hero who made room isn't marked as resting");
            Assert.AreEqual(ZoneController.Current.Form,Party.members[1].GetComponent<FormController>().Current,"the hero who came out isn't in the zone's form");
            // Resting the lead: whoever comes out leads, so the pad is never steering nobody.
            HeroRow("Taren").onClick.Invoke();yield return Frames(3);
            CollectionAssert.AreEqual(new[]{"Sela",Third},Field(),"resting the lead didn't bring the rester out");
            Assert.AreEqual("Sela",Party.Active.character,"the hero who came out in the lead's place doesn't lead");
            Assert.AreEqual("Sela",State.party[State.activeMember].id,"the save's lead isn't the hero leading");
            menu.Close();yield return null;
            // In the open the choice holds, and can't be changed from the pause menu: a fresh hero for a spent one mid-dungeon would be a third life.
            yield return Zone("Arena_Ground");
            CollectionAssert.AreEqual(new[]{"Sela",Third},Field(),"the pair chosen didn't come through a change of zone");
            menu=Menu;menu.Open(PauseMenu.PartyPage);yield return null;
            HeroRow("Taren").onClick.Invoke();yield return Frames(3);
            Assert.AreEqual(PartyField.Refusal,Label("Feedback"),"a press in the open wasn't refused with its reason");
            CollectionAssert.AreEqual(new[]{"Sela",Third},Field(),"a refused press changed the field");CollectionAssert.AreEqual(new[]{"Sela",Third},Out(),"a refused press changed the save");
            menu.Close();yield return null;
            // At a bench, in the same place, it can.
            menu.Open(PauseMenu.FabricatePage,true);yield return null;Row("Tab_Party").onClick.Invoke();yield return null;
            HeroRow("Taren").onClick.Invoke();yield return Frames(3);
            CollectionAssert.AreEqual(new[]{"Taren","Sela"},Field(),"a press at a bench didn't bring the rester out in the partner's place");
            Assert.AreEqual("Sela",Party.Active.character,"bringing a hero out at a bench changed who leads");
            StringAssert.StartsWith(PartyPairUi.RestingMark,Words(HeroRow(Third)),"the hero who made room at the bench isn't marked as resting");
        }
        [UnityTest] public IEnumerator TheChosenPairComesBackFromASave()
        {
            Recruit();Assert.IsTrue(Deployment.Set(State,new[]{"Sela",Third}),"fixture: the pair is named");State.activeMember=Deployment.PartyIndex(State,Third);
            // The hero resting is in the file whole: what they had, what they wear, how far on they are.
            var taren=Member("Taren");taren.integrity=93;taren.xp=41;var worn=new Inventory(State).GivePart(GameCatalog.Find<TechPartDef>("EdgesT2"));taren.equipped[GearSlot.Edge]=worn.instanceId;
            var saves=Folder();saves.Save("slot1",State);var loaded=saves.Load("slot1");
            Assert.IsNotNull(loaded,"fixture: the save reads back");
            CollectionAssert.AreEquivalent(new[]{"Sela",Third},loaded.deployed??new List<string>(),"the save lost the pair");
            var rested=loaded.party.Find(m=>m.id=="Taren");Assert.IsNotNull(rested,"the hero resting isn't in the save");
            Assert.AreEqual(93,rested.integrity,.01f,"the hero resting came back from the save without what they had");Assert.AreEqual(41,rested.xp,"the hero resting came back from the save without their XP");
            Assert.IsTrue(rested.equipped.TryGetValue(GearSlot.Edge,out string edge)&&edge==worn.instanceId,"the hero resting came back from the save without what they wear");
            GameServices.Current.State=loaded;yield return Zone("Arena_Ground");
            CollectionAssert.AreEqual(new[]{"Sela",Third},Field(),"the pair on the field after a load isn't the pair that was saved");
            Assert.AreEqual(Third,Party.Active.character,"the lead after a load isn't the lead that was saved");
        }
        /// <summary>Guard: it passes before the pair existed too. A save of two has no pair, plays as two, and its Party page is the one it always was.</summary>
        [UnityTest] public IEnumerator ASaveOfTwoPlaysAsItAlwaysDid()
        {
            var saves=Folder();File.WriteAllText(saves.PathForSlot("slot1"),TwoHeroSave.Replace('\'','"'));
            var loaded=saves.Load("slot1");Assert.IsNotNull(loaded,"fixture: the old save reads");
            Assert.IsNull(loaded.deployed,"a save from before the third hero came back with a pair");
            GameServices.Current.State=loaded;GameServices.ArmAnEmptyParty(loaded);yield return Zone("Arena_Ground");
            CollectionAssert.AreEqual(new[]{"Taren","Sela"},Field());Assert.AreEqual(1,Party.index,"the save's lead (Sela) doesn't lead");Assert.AreEqual(1,State.activeMember);
            Assert.AreEqual(180,Party.Active.Health.integrity,.5f,"Sela didn't come back with what the save gave her");
            Assert.IsTrue(Party.Swap());yield return null;Assert.AreEqual("Taren",Party.Active.character);Assert.AreEqual(0,State.activeMember,"the save's lead didn't follow the swap");
            var menu=Menu;menu.Open(PauseMenu.PartyPage);yield return null;
            Assert.IsFalse(Buttons().Any(b=>b.name.StartsWith(PartyPairUi.RowPrefix)),"a party of two is offered a choice of who rests");
            StringAssert.StartsWith(Named("Taren"),Words(Row("Row_0")));StringAssert.StartsWith(Named("Sela"),Words(Row("Row_1")));
            StringAssert.StartsWith("Swap active member",Words(Row("Row_2")));StringAssert.StartsWith("Save to slot 1",Words(Row("Row_3")));
            Row("Row_1").onClick.Invoke();yield return Frames(2);
            Assert.AreEqual("",Label("Feedback"),"a hero's row answers a press in a party of two");CollectionAssert.AreEqual(new[]{"Taren","Sela"},Field());
            menu.Close();yield return null;
            saves.Save("slot2",State);Assert.IsNull(saves.Load("slot2").deployed,"a party of two was written with a pair");
        }

        // ------------------------------------------------------------------ the replay driver
        [UnityTest] public IEnumerator TheDriverFindsThePartnerByRoleWhereverTheLeadStands()
        {
            yield return Zone("Arena_Ground");
            var extra=ThreeOnTheField();var party=Party;yield return null;
            yield return Hands(new QualityRoute{name="party of three",scene="Arena_Ground"});
            for(int place=0;place<3;place++)
            {
                party.index=place;party.Apply();foreach(var hero in party.members)hero.GetComponent<PartnerBrain>().enabled=false;yield return null;
                Assert.AreEqual(place,party.index,"fixture: the lead stands in place "+place);
                bool alive=false,down=true;
                Assert.DoesNotThrow(()=>{alive=Condition("partyAlive");down=Condition("partnerDown");},"the driver's conditions threw with the lead in place "+place);
                Assert.IsTrue(alive,"partyAlive is false with all three standing, lead in place "+place);Assert.IsFalse(down,"partnerDown is true with all three standing, lead in place "+place);
                Assert.DoesNotThrow(()=>Call("LateUpdate"),"the driver's frame sample threw with the lead in place "+place);
            }
            // The lead in the third place, and the hero in the first down: the one the lead has to reach.
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Party of three fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(60,1,60);Physics.SyncTransforms();
            var fallen=party.members[0];Place(party.Active,new Vector3(230,0,0));Place(party.members[1],new Vector3(236,0,-6));Place(fallen,new Vector3(230,0,9));yield return Frames(3);
            Fell(fallen);yield return null;
            Assert.IsTrue(Condition("partnerDown"),"partnerDown is false with a hero out down and the lead standing in the third place");
            Assert.IsFalse(Condition("partyAlive"),"partyAlive is true with a hero out down");
            Assert.AreSame(fallen,party.Partner,"the partner by role isn't the hero who is down");
            float before=Flat(party.Active.transform.position,fallen.transform.position);Assert.Greater(before,7,"fixture: the fallen hero is a walk away");
            Call("BeginStep",new QualityStep{name="reach the fallen hero",seconds=20,navigate=true,approachPartner=true,tolerance=1.2f,stopWhen="partyAlive"});
            yield return Drive(9,()=>Flat(party.Active.transform.position,fallen.transform.position)<1.6f||fallen.Health.Alive);
            Assert.Less(Flat(party.Active.transform.position,fallen.transform.position),2.2f,$"approachPartner didn't walk the lead to the fallen hero from the third place (was {before:0.0} m away)");
            Object.Destroy(floor);
        }
        [UnityTest] public IEnumerator EnsureCharacterGivesUpBehindADownedHeroAndSaysSoInsteadOfPressingSwapForever()
        {
            yield return Zone("Arena_Ground");
            var sela=Party.members.First(m=>m.character=="Sela");Fell(sela);yield return null;
            Assert.IsFalse(sela.Health.Alive,"fixture: Sela is down");Party.Active.Health.InvulnerableUntil=float.PositiveInfinity;
            var presses=new int[1];
            yield return Play(new QualityRoute{name="lead",scene="Arena_Ground",steps=new[]{new QualityStep{name="Sela leads",seconds=30,ensureCharacter="Sela"}}},14,SwapPresses(presses));
            Assert.IsTrue(finished,$"a thirty-second step ran on behind a hero who can't lead ({presses[0]} presses of Swap so far)");
            Assert.LessOrEqual(presses[0],8,"the driver went on pressing Swap at a downed hero");
            Assert.IsTrue(Failures.Any(f=>f.StartsWith("Sela leads: ")&&f.Contains("Sela")&&f.Contains("down")),"the step didn't fail saying who couldn't lead and why: "+Failed);
        }
        [UnityTest] public IEnumerator EnsureCharacterFailsAtOnceForAHeroRestingWhereThePairCantBeChanged()
        {
            Recruit();yield return Zone("Arena_Ground");
            CollectionAssert.AreEqual(new[]{"Taren","Sela"},Field(),"fixture: the third hero is resting");
            var presses=new int[1];
            yield return Play(new QualityRoute{name="lead",scene="Arena_Ground",steps=new[]
            {
                new QualityStep{name=Third+" leads",seconds=30,ensureCharacter=Third,pauseUi="pause"},
                new QualityStep{name="nobody leads",seconds=30,ensureCharacter="Nobody"},
            }},10,SwapPresses(presses));
            Assert.IsTrue(finished,"two thirty-second steps ran on behind heroes who can't lead here");
            Assert.AreEqual(0,presses[0],"the driver pressed Swap for a hero who isn't out");
            Assert.IsTrue(Failures.Any(f=>f.StartsWith(Third+" leads: ")&&f.Contains("resting")),"the step didn't say the hero is resting where the party can't be changed: "+Failed);
            Assert.IsTrue(Failures.Any(f=>f.StartsWith("nobody leads: ")&&f.Contains("Nobody")),"the step didn't say there is nobody of that name: "+Failed);
            CollectionAssert.AreEqual(new[]{"Taren","Sela"},Field(),"a refused ensureCharacter changed the field");
            Assert.IsFalse(Menu.IsOpen,"the driver opened the menu in the open");
        }
        [UnityTest] public IEnumerator EnsureCharacterBringsARestingHeroOutThroughThePartyPageSomewhereSafe()
        {
            Recruit();yield return Zone("Hub_Decks");
            CollectionAssert.AreEqual(new[]{"Taren","Sela"},Field(),"fixture: the third hero is resting");
            yield return Play(new QualityRoute{name="deploy",scene="Hub_Decks",steps=new[]
            {
                new QualityStep{name=Third+" leads",seconds=25,ensureCharacter=Third,pauseUi="pause",until="lead:"+Third},
                new QualityStep{name=Third+" in the lead",seconds=.3f,expectedCharacter=Third,expectedUi="world"},
            }},20);
            Assert.IsTrue(finished,"the route never ended: the step that brings a hero out didn't end when they led");
            Assert.AreEqual("",Failed,"bringing a resting hero out failed the route");
            Assert.AreEqual(Third,Party.Active.character,"the hero named doesn't lead");
            CollectionAssert.AreEqual(new[]{"Taren",Third},Field(),"the hero came out in somebody's place other than the partner's");
            Assert.IsFalse(Menu.IsOpen,"the driver left the menu open");
            // Without the step's leave to pause, the driver doesn't open a menu the run would be rejected for.
            Assert.IsTrue(PartyField.Choose(Party,Third,false,out _),"fixture: the third hero rests again");yield return Frames(3);
            Assert.IsFalse(Deployment.Out(State,Third),"fixture: the third hero is resting again");
            Again();
            yield return Play(new QualityRoute{name="deploy",scene="Hub_Decks",steps=new[]{new QualityStep{name=Third+" leads",seconds=30,ensureCharacter=Third}}},8);
            Assert.IsTrue(finished,"a thirty-second step ran on with no leave to open the Party page");
            Assert.IsTrue(Failures.Any(f=>f.Contains("pauseUi")),"the step didn't say it needs pauseUi to open the Party page: "+Failed);
            Assert.IsFalse(Menu.IsOpen,"the driver opened the menu without the step's leave");
        }
        /// <summary>At a bench in a place with fights the pair can be changed too, and the driver does it as a player at the bench would: LB round to the Party page, the
        /// d-pad to the hero's row, the press, Start to leave, and Swap until they lead.</summary>
        [UnityTest] public IEnumerator EnsureCharacterBringsARestingHeroOutAtABenchInAPlaceWithFights()
        {
            Recruit();yield return Zone("Arena_Ground");
            Assert.IsTrue(ZoneController.Current.Combat,"fixture: the yard is a place with fights");
            CollectionAssert.AreEqual(new[]{"Taren","Sela"},Field(),"fixture: the third hero is resting");
            Menu.Open(PauseMenu.FabricatePage,true);yield return Frames(2);
            Assert.IsTrue(Menu.IsOpen&&Menu.AtBench,"fixture: the bench is open");
            yield return Play(new QualityRoute{name="bench",scene="Arena_Ground",steps=new[]
            {
                new QualityStep{name=Third+" leads",seconds=25,ensureCharacter=Third,pauseUi="bench",until="lead:"+Third},
                new QualityStep{name=Third+" in the lead",seconds=.3f,expectedCharacter=Third,expectedUi="world"},
            }},20);
            Assert.IsTrue(finished,"the route never ended: the step that brings a hero out at a bench didn't end when they led");
            Assert.AreEqual("",Failed,"bringing a resting hero out at a bench failed the route");
            Assert.AreEqual(Third,Party.Active.character,"the hero named doesn't lead");
            CollectionAssert.AreEqual(new[]{"Taren",Third},Field(),"the hero came out in somebody's place other than the partner's");
            Assert.IsFalse(Menu.IsOpen,"the driver left the bench open");
        }
        [UnityTest] public IEnumerator ARouteNamesItsPairAndTheDriverSaysSoWhenTheSaveDisagrees()
        {
            Recruit();yield return Zone("Arena_Ground");
            string json="{\"name\":\"pair\",\"scene\":\"Arena_Ground\",\"deployed\":[\"Taren\",\""+Third+"\"],\"steps\":[{\"name\":\"stand\",\"seconds\":0.3},{\"name\":\"stand on\",\"seconds\":0.3}]}";
            yield return Play(JsonUtility.FromJson<QualityRoute>(json),6);
            Assert.IsTrue(finished,"the route never ended");
            Assert.AreEqual(1,Failures.Count(f=>f.Contains("deployed")),"a route that names a pair the save doesn't have out must say so, once: "+Failed);
            StringAssert.Contains(Named("Sela"),Failures.First(f=>f.Contains("deployed")),"the failure doesn't say who is out");
            // A route that starts from a loadout has its pair put out with the loadout, as its Sync and gear are.
            var apply=typeof(ContinuousReview).GetMethod("ApplyPair",BindingFlags.Static|BindingFlags.Public);
            Assert.IsNotNull(apply,"the driver has no way to put a loadout route's pair out");
            apply.Invoke(null,new object[]{JsonUtility.FromJson<QualityRoute>(json)});
            CollectionAssert.AreEqual(new[]{"Taren",Third},Out(),"a loadout route's pair wasn't put out");
            PartyField.Reform(Party);yield return Frames(3);
            Again();
            yield return Play(JsonUtility.FromJson<QualityRoute>(json),6);
            Assert.AreEqual("",Failed,"a route whose pair is the pair out failed");
        }

        // ------------------------------------------------------------------ the captures
        [UnityTest] public IEnumerator TheCapturesWriteAThirdBodyAfterTheTwoAndTheTwoAsTheyAlwaysWere()
        {
            yield return Zone("Arena_Ground");
            var extra=ThreeOnTheField();yield return Frames(3);
            var active=Party.Active;var partner=Party.members[1];
            string two=Path.Combine(Application.temporaryCachePath,"party-of-three-pose-two-"+System.Guid.NewGuid().ToString("N")),three=two.Replace("-two-","-three-");
            folders.Add(two);folders.Add(three);Directory.CreateDirectory(two);Directory.CreateDirectory(three);
            using(var capture=new GroundPoseCapture(two)){capture.Record(0,Time.frameCount,.25,7,active,partner);capture.Finish();}
            using(var capture=new GroundPoseCapture(three)){capture.Record(0,Time.frameCount,.25,7,active,partner,new[]{extra});capture.Finish();}
            var pair=File.ReadAllLines(Path.Combine(two,"ground-pose.csv"));var all=File.ReadAllLines(Path.Combine(three,"ground-pose.csv"));
            Assert.AreEqual(3,pair.Length,"a capture of two isn't its header and two rows");Assert.AreEqual(4,all.Length,"a third body didn't add one row");
            CollectionAssert.AreEqual(pair,all.Take(3).ToArray(),"a third body changed the rows of the two, or their order");
            var headers=all[0].Split(',');var last=all[3].Split(',');Assert.AreEqual(headers.Length,last.Length,"the third body's row has other columns");
            Assert.AreEqual(GroundPoseCapture.OtherRole,last[System.Array.IndexOf(headers,"role")]);Assert.AreEqual(Third,last[System.Array.IndexOf(headers,"hero")]);
        }

        // ------------------------------------------------------------------ the loadouts
        [UnityTest] public IEnumerator ChapterSevenArrivesWithTheThirdHeroAndChapterSixWithout()
        {
            yield return null;
            bool defined=GameCatalog.Find<CharacterDef>(DevLoadout.ThirdHero)!=null;
            GameServices.Current.NewGame();log.Clear();DevLoadout.Apply("chapter6");
            Assert.AreEqual(2,State.party.Count,"she joins during chapter 6: its arrival is still the two");Assert.IsFalse(Deployment.Choosing(State));
            Assert.IsFalse(log.Any(l=>l.StartsWith(DevLoadout.NoHeroMarker)),"chapter 6's arrival speaks of a hero it was never going to have");
            for(int chapter=DevLoadout.ThirdHeroFrom;chapter<=DevLoadout.LastChapter;chapter++)
            {
                GameServices.Current.NewGame();log.Clear();Assert.DoesNotThrow(()=>DevLoadout.Apply("chapter"+chapter),"chapter "+chapter+"'s loadout broke");
                Assert.IsTrue(log.Any(l=>l.StartsWith("DEV_LOADOUT_OK chapter"+chapter)),"chapter "+chapter+"'s loadout didn't finish");
                if(!defined)
                {
                    // No definition yet: the loadout arrives as the two it can make, and says who is missing.
                    Assert.AreEqual(2,State.party.Count,"a loadout added a hero the game has no definition for");
                    Assert.IsTrue(log.Any(l=>l.StartsWith(DevLoadout.NoHeroMarker+" "+DevLoadout.ThirdHero)),"chapter "+chapter+" arrived without the third hero and didn't say so");
                    continue;
                }
                Assert.AreEqual(3,State.party.Count,"chapter "+chapter+" arrives without the third hero");
                var her=Member(DevLoadout.ThirdHero);int sync=DevLoadout.SyncOnArrival(chapter);
                Assert.AreEqual(sync,her.level,"she doesn't arrive at the plan's Sync");Assert.AreEqual(Levels.MaxIntegrity(sync),her.integrity,.01f,"she doesn't arrive whole");
                // Her hammer strikes with an Edge (her definition's own starting gear says so): she must not arrive holding Sela's emitter as her weapon.
                Assert.IsTrue(her.equipped.TryGetValue(GearSlot.Edge,out string edge),"she arrives with no Edge");
                var worn=GameCatalog.Find<TechPartDef>(State.parts.First(p=>p.instanceId==edge).definitionId);
                if(her.equipped.TryGetValue(GearSlot.Emitter,out string emitter))
                    Assert.Greater(worn.tier,GameCatalog.Find<TechPartDef>(State.parts.First(p=>p.instanceId==emitter).definitionId).tier,"her Edge isn't the better of her two weapons");
                CollectionAssert.AreEquivalent(DevLoadout.ArrivalPair,Out(),"the pair out on arriving isn't the loadout's");
                Assert.IsTrue(Deployment.Out(State,State.party[State.activeMember].id),"the loadout's lead is resting");
            }
        }
        /// <summary>Chapter 6's arrival is before she is met; a chapter 6 test of the dig, after the join, has a loadout of its own.</summary>
        [UnityTest] public IEnumerator ChapterSixOnceIdraHasJoinedArrivesAsThreeWithHerOutBesideTaren()
        {
            yield return null;
            Assert.IsNotNull(GameCatalog.Find<CharacterDef>("Idra"),"fixture: Idra has a definition");
            GameServices.Current.NewGame();log.Clear();
            Assert.DoesNotThrow(()=>DevLoadout.Apply("chapter6joined"),"there is no loadout for chapter 6 once Idra has joined");
            Assert.IsTrue(log.Any(l=>l.StartsWith("DEV_LOADOUT_OK chapter6joined")),"the loadout for chapter 6 after the join didn't finish: "+string.Join(" | ",log));
            CollectionAssert.AreEquivalent(new[]{"Taren","Sela","Idra"},State.party.Select(m=>m.id).ToArray(),"chapter 6 after the join isn't the three");
            int sync=DevLoadout.SyncOnArrival(6);
            foreach(var member in State.party){Assert.AreEqual(sync,member.level,Named(member.id)+" isn't at chapter 6's Sync");Assert.AreEqual(Levels.MaxIntegrity(sync),member.integrity,.01f,Named(member.id)+" isn't whole");}
            CollectionAssert.AreEquivalent(new[]{"Taren","Idra"},Out(),"she isn't out beside Taren, as the join leaves her");
            Assert.IsTrue(State.flags.TryGetValue("hoar.idraJoined",out bool joined)&&joined,"the loadout after the join doesn't say she has joined");
            Assert.IsTrue(State.flags.TryGetValue("chapter5.complete",out bool told)&&told,"the loadout after the join isn't chapter 6: chapter 5 isn't told");
            // Chapter 6's own arrival is unchanged: two, and nothing joined.
            GameServices.Current.NewGame();DevLoadout.Apply("chapter6");
            Assert.AreEqual(2,State.party.Count,"chapter 6's arrival now holds Idra, who is still a voice and a person in Lastlight there");
            Assert.IsFalse(State.flags.ContainsKey("hoar.idraJoined"),"chapter 6's arrival says she has joined");
        }

        // ------------------------------------------------------------------ the HUD: who is waiting
        /// <summary>The HUD's line for the hero resting, or null while there is none on the screen.</summary>
        static TMP_Text RestingOnHud()
        {
            var canvas=Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(c=>c.name=="HUD");
            return canvas.GetComponentsInChildren<TMP_Text>(false).FirstOrDefault(t=>t.name=="Resting");
        }
        static Rect OnScreen(RectTransform rect){var corners=new Vector3[4];rect.GetWorldCorners(corners);return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);}
        /// <summary>The partner line names the hero beside the lead; with three, a line under it in the same panel names the one who is resting and follows a change of pair.
        /// A party of two has no such line.</summary>
        [UnityTest] public IEnumerator TheHudSaysWhoIsRestingAndAPartyOfTwoHasNoSuchLine()
        {
            yield return Zone("Arena_Ground");yield return Frames(3);
            Assert.IsNull(RestingOnHud(),"a party of two has a line on the HUD for somebody resting");
            Recruit();yield return Frames(3);
            var line=RestingOnHud();Assert.IsNotNull(line,"with three in the party nothing on the HUD says who is resting");
            StringAssert.Contains(Named(Third).ToUpperInvariant(),line.text,"the HUD's resting line doesn't name the hero resting");
            StringAssert.DoesNotContain(Named("Sela").ToUpperInvariant(),line.text,"the HUD's resting line names the partner");
            StringAssert.Contains(Named("Sela").ToUpperInvariant(),PartnerLine(),"fixture: Sela is the partner");
            // It is part of the vitals: inside their frame, under the partner's line, not over it.
            var canvas=Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(c=>c.name=="HUD");
            var vitals=canvas.GetComponentsInChildren<Image>(true).First(i=>i.name=="VitalsFrame");var partner=canvas.GetComponentsInChildren<TMP_Text>(true).First(t=>t.name=="Partner");
            Rect frame=OnScreen(vitals.rectTransform),own=OnScreen(line.rectTransform),above=OnScreen(partner.rectTransform);
            Assert.IsTrue(frame.Contains(own.min)&&frame.Contains(own.max),$"the resting line {own} isn't inside the vitals' frame {frame}");
            Assert.LessOrEqual(own.yMax,above.yMin+.5f,$"the resting line {own} runs into the partner's line {above}");
            // Another pair: the line follows who is resting.
            Assert.IsTrue(Deployment.Set(State,new[]{"Taren",Third}),"fixture: the pair is named");PartyField.Reform(Party);yield return Frames(3);
            line=RestingOnHud();Assert.IsNotNull(line,"the resting line went with a change of pair");
            StringAssert.Contains(Named("Sela").ToUpperInvariant(),line.text,"the resting line didn't follow a change of pair");
            StringAssert.DoesNotContain(Named(Third).ToUpperInvariant(),line.text,"the resting line still names the hero who came out");
        }

        // ------------------------------------------------------------------ two who wield an Edge
        /// <summary>A part found in the world goes on whoever has its slot free, and a weapon first to a hero who fights with it. By name an Edge was Taren's: with his
        /// worn, a found Edge went past Idra, whose hammer is one, and onto Sela, who fights with an Emitter. A found Emitter is still Sela's.</summary>
        [UnityTest] public IEnumerator AFoundWeaponGoesToTheHeroWhoWieldsItOfThreeWhoseTwoShareAnEdge()
        {
            yield return null;
            var third=Recruit();var inventory=new Inventory(State);
            var edge=GameCatalog.Find<TechPartDef>("EdgesT2");var emitter=GameCatalog.Find<TechPartDef>("EmittersT2");Assert.IsNotNull(edge);Assert.IsNotNull(emitter);
            foreach(var member in State.party)member.equipped.Clear();
            inventory.Equip(Member("Taren"),inventory.GivePart(edge),edge);
            Assert.AreEqual(GearSlot.Edge,DevLoadout.Weapon(Third),"fixture: her definition starts her with an Edge");
            var found=inventory.ReceivePart(edge);
            Assert.AreEqual(Third,inventory.Wearer(found)?.id,"a found Edge didn't go to the hero resting with her hammer's slot empty: it went on "+(inventory.Wearer(found)?.id??"nobody"));
            var shot=inventory.ReceivePart(emitter);
            Assert.AreEqual("Sela",inventory.Wearer(shot)?.id,"a found Emitter didn't go to the hero who fights with one");
        }
        /// <summary>D255: she joins at the party's Sync on the same ladder, so at Sync 16 she knows nine. She brings four into her slots, the rescue officer's mix, and the
        /// join says so on one card: not a card, or a slot, for each of the nine.</summary>
        [UnityTest] public IEnumerator AHeroWhoJoinsAtSyncSixteenBringsFourIntoHerSlotsAndOneCard()
        {
            foreach(var member in State.party){member.level=16;member.integrity=Levels.MaxIntegrity(16);}
            yield return Zone("Arena_Ground");
            var cards=new List<(string heading,string body)>();void Card(string heading,string body)=>cards.Add((heading,body));
            int levels=0;void Raised(MemberState member,int from,int to)=>levels++;
            Notices.Posted+=Card;Levels.LevelRaised+=Raised;
            try
            {
                var joined=Recruit();Assert.AreEqual(16,joined.level,"fixture: she joins at the party's Sync");
                Assert.AreEqual(9,SkillBook.Learned(Third).Length,"fixture: at Sync 16 she knows nine (D255)");
                var bridge=Object.FindFirstObjectByType<WorldUiBridge>();typeof(WorldUiBridge).GetMethod("Join",Private).Invoke(bridge,new object[]{Third});yield return Frames(3);
                var slots=Enumerable.Range(0,SkillBook.Slots).Select(i=>SkillBook.Equipped(Third,i)?.kind).ToArray();
                CollectionAssert.AreEqual(new SkillKind?[]{SkillKind.Breach,SkillKind.RescueFlare,SkillKind.Shelter,SkillKind.SlagHammer},slots,"she didn't bring the four a join gives her: "+string.Join(", ",slots));
                var said=cards.Where(c=>c.heading.Contains(Named(Third).ToUpperInvariant())).ToArray();
                Assert.AreEqual(1,said.Length,"the join isn't said on one card: "+string.Join(" | ",cards.Select(c=>c.heading+": "+c.body)));
                StringAssert.Contains("Rescue Flare",said[0].body,"the join's card doesn't say what is in her slots");StringAssert.Contains("5 more",said[0].body,"the join's card doesn't say how many more wait on the Skills page");
                Assert.AreEqual(0,levels,"the join raised levels, and with them a card for each skill she knows");
                // The Skills page shows her four slots, as it shows anyone's, with the rest in the chooser behind them.
                var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return Frames(2);
                for(int i=0;i<4&&!Header().StartsWith(Named(Third));i++)yield return Tap(GamepadButton.North);
                StringAssert.StartsWith(Named(Third)+"'s skills",Header(),"Swap on the Skills page never reaches her");
                var rows=Buttons().Where(b=>b.name.StartsWith("Row_")).Select(Words).ToArray();
                Assert.AreEqual(4,rows.Count(r=>new[]{"Breach","Rescue Flare","Shelter","Slag Hammer"}.Any(r.Contains)),"her Skills page doesn't show her four slots: "+string.Join(" | ",rows));
                Assert.IsFalse(rows.Any(r=>r.Contains("Tow Line")||r.Contains("Avalanche")),"the Skills page lists skills she hasn't slotted beside her slots: "+string.Join(" | ",rows));
                menu.Close();yield return null;
            }
            finally{Notices.Posted-=Card;Levels.LevelRaised-=Raised;}
            // A fixture's Idra is a player's: the chapter loadouts give her the same four.
            GameServices.Current.NewGame();DevLoadout.Apply("chapter7");
            CollectionAssert.AreEqual(new SkillKind?[]{SkillKind.Breach,SkillKind.RescueFlare,SkillKind.Shelter,SkillKind.SlagHammer},Enumerable.Range(0,SkillBook.Slots).Select(i=>SkillBook.Equipped(Third,i)?.kind).ToArray(),"chapter 7's Idra hasn't the four a join gives her");
        }

        // ------------------------------------------------------------------ the Gear page with three
        IEnumerator Tap(GamepadButton button)
        {
            if(menuPad==null||!menuPad.added)menuPad=InputSystem.AddDevice<Gamepad>("PartyOfThreeMenuPad");menuPad.MakeCurrent();
            InputSystem.QueueStateEvent(menuPad,new GamepadState().WithButton(button));yield return null;yield return null;
            InputSystem.QueueStateEvent(menuPad,new GamepadState());yield return null;yield return null;yield return null;
        }
        static string Header(){var t=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(l=>l.name=="Header");Assert.IsNotNull(t,"the page has no header");return t.text;}
        /// <summary>A real swap only ever reaches the two out, so with three the Gear page's Swap looks round all three instead, the one resting too, and swaps nobody;
        /// a part put on there goes on the hero shown. (With two it is still the real swap: QaMenusLastTests.ThePageOpensOnWhoeverLeadsAndGearStillReallySwaps.)</summary>
        [UnityTest] public IEnumerator TheGearPageReachesTheHeroRestingAndItsSwapThereSwapsNobody()
        {
            var third=Recruit();yield return Zone("Hub_Decks");
            var part=GameCatalog.Find<TechPartDef>("DrivesT2");Assert.IsNotNull(part,"fixture: a spare part to put on");
            var spare=new Inventory(State).GivePart(part);
            int swaps=Party.SwapCount;
            var menu=Menu;menu.Open(PauseMenu.GearPage);yield return Frames(2);
            StringAssert.StartsWith("Equipping "+Named("Taren"),Header(),"the Gear page doesn't open on the lead");
            yield return Tap(GamepadButton.North);
            Assert.AreEqual("Taren",Party.Active.character,"with three in the party Swap on the Gear page swapped the field: it can only ever reach the two out");
            Assert.AreEqual(swaps,Party.SwapCount,"Swap on the Gear page swapped the party");
            StringAssert.StartsWith("Equipping "+Named("Sela"),Header(),"Swap on the Gear page didn't come round to the partner");
            yield return Tap(GamepadButton.North);
            StringAssert.StartsWith("Equipping "+Named(Third),Header(),"Swap on the Gear page never reaches the hero resting, whose parts can't be changed");
            StringAssert.Contains("resting",Header(),"the Gear page doesn't say the hero it shows is resting");
            var row=Buttons().FirstOrDefault(b=>Words(b).StartsWith(part.displayName));Assert.IsNotNull(row,$"fixture: the spare {part.displayName} has a row. On screen: {string.Join(" | ",Buttons().Select(Words))}");
            row.onClick.Invoke();yield return Frames(2);
            Assert.IsTrue(third.equipped.TryGetValue(GearSlot.Drive,out string worn)&&worn==spare.instanceId,"a part put on from the Gear page didn't go on the hero it shows, who is resting");
            Assert.IsFalse(State.party.Any(m=>m!=third&&m.equipped.ContainsValue(spare.instanceId)),"the part went on a hero out");
            yield return Tap(GamepadButton.North);
            StringAssert.StartsWith("Equipping "+Named("Taren"),Header(),"Swap on the Gear page doesn't come back round to the lead");
            Assert.AreEqual("Taren",Party.Active.character);CollectionAssert.AreEqual(new[]{"Taren","Sela"},Field(),"looking round the party's gear changed who is out");
            menu.Close();yield return null;
        }

        // ------------------------------------------------------------------ a fall with a hero waiting
        /// <summary>With the two out down and the one resting whole, nobody is standing on the field: the autosave refuses it, as it refuses a wipe of two, and a file
        /// that holds one loads with the two out on their feet. Before, the rester's Integrity let the wipe through, and Continue loaded into a defeat.</summary>
        [UnityTest] public IEnumerator AWipeOfTheTwoOutIsNeverAutosavedAndLoadsStandingWhileTheOneRestingIsWhole()
        {
            yield return null;var saves=Folder();
            foreach(var member in State.party)member.integrity=0;
            saves.Save("autosave",State);Assert.IsFalse(File.Exists(saves.PathForSlot("autosave")),"fixture: a wipe of two is never autosaved");
            var third=Recruit();third.integrity=77;
            foreach(var member in State.party)if(member!=third)member.integrity=0;
            log.Clear();saves.Save("autosave",State);
            Assert.IsFalse(File.Exists(saves.PathForSlot("autosave")),"the two out are down and the autosave took it because the hero resting had Integrity: a retry or a Continue would load into a defeat");
            Assert.IsTrue(log.Any(l=>l.StartsWith("SAVE_REFUSED")),"the refused autosave didn't say so: "+string.Join(" | ",log));
            // A manual slot isn't guarded: what it holds of such a wipe loads with the two out standing and the one resting as they were.
            saves.Save("slot1",State);var loaded=saves.Load("slot1");Assert.IsNotNull(loaded,"fixture: the slot reads back");
            foreach(var member in Deployment.Of(loaded))Assert.AreEqual(Levels.MaxIntegrity(member.level),member.integrity,.01f,$"{Named(member.id)} is out and loaded at nothing: the party loads into a defeat");
            Assert.AreEqual(77,loaded.party.Find(m=>m.id==Third).integrity,.01f,"the hero resting lost what they had");
        }

        // ------------------------------------------------------------------ lines
        /// <summary>A hero resting has no body: a line of theirs from the field (Sela's at a gate a chapter 2 world object gives her) is the lead's own, or nobody's.
        /// A hero out keeps theirs, a voice on the radio is heard from wherever it is, and with two every line is spoken as asked.</summary>
        [UnityTest] public IEnumerator ALineFromTheFieldForTheHeroRestingIsTheLeadsOwnOrNobodys()
        {
            yield return null;
            var heard=new List<(string who,string line)>();void Heard(string who,string line)=>heard.Add((who,line));
            BarkService.Spoken+=Heard;
            try
            {
                BarkService.Play("Sela","gate.open",true);
                Assert.AreEqual("Sela",heard.Single().who,"fixture: with two in the party Sela says her own line at the gate");
                Recruit();Assert.IsTrue(Deployment.Set(State,new[]{"Taren",Third}),"fixture: Sela rests");State.activeMember=0;heard.Clear();
                BarkService.Play("Sela","gate.open",true);
                Assert.IsEmpty(heard,"Sela is resting, with no body anywhere, and still spoke from the field: "+string.Join(" | ",heard.Select(h=>h.who+": "+h.line)));
                BarkService.Play("Sela","kill",true);
                Assert.AreEqual(1,heard.Count,"a line the lead has words for wasn't said at all");
                Assert.AreEqual("Taren",heard[0].who,"a line given to the hero resting wasn't said by the lead");Assert.AreEqual(BarkService.Line("Taren","kill"),heard[0].line,"the lead said someone else's words");
                heard.Clear();BarkService.Play(Third,"kill",true);
                Assert.AreEqual(Third,heard.Single().who,"a hero out lost their own line");
                if(Third=="Idra")
                {
                    // Idra resting, on the radio: a hail is a voice from somewhere else.
                    Assert.IsTrue(Deployment.Set(State,new[]{"Taren","Sela"}));heard.Clear();
                    BarkService.Play("Idra","hail.tender",true);
                    Assert.AreEqual("Idra",heard.Single().who,"a hail on the radio from the hero resting went unheard or went to someone else");
                }
            }
            finally{BarkService.Spoken-=Heard;}
        }

        // ------------------------------------------------------------------ what the hero resting earns
        /// <summary>Guard: a kill pays every hero in the party, the one resting too, so a benched hero is never behind on Sync (and so on Integrity and on the skills Sync
        /// teaches). Mastery is by use, as Ys's is: a hero resting keeps what they had.</summary>
        [UnityTest] public IEnumerator TheHeroRestingGainsTheSameSyncFromAKillAndKeepsTheirMastery()
        {
            var third=Recruit();
            var skill=SkillBook.All(Third).FirstOrDefault();if(skill!=null){third.skillUses??=new Dictionary<string,int>();third.skillUses[skill.id]=20;}
            yield return Zone("Arena_Ground");
            CollectionAssert.DoesNotContain(Field(),Third,"fixture: the third hero is resting");
            var start=State.party.ToDictionary(m=>m.id,m=>(m.level,m.xp));
            var hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),Party.Active.transform.position+Vector3.forward*6);yield return Frames(2);
            var health=hound.GetComponent<Health>();health.InvulnerableUntil=0;health.Receive(new DamagePacket{amount=health.maximum*10,type=DamageType.Kinetic,source=Party.Active.Health});
            yield return Frames(3);
            Assert.IsFalse(health.Alive,"fixture: the hound is down");
            var after=State.party.ToDictionary(m=>m.id,m=>(m.level,m.xp));
            foreach(var member in State.party)Assert.AreNotEqual(start[member.id],after[member.id],Named(member.id)+" earned nothing from the kill");
            Assert.AreEqual(after["Taren"],after[Third],"the hero resting didn't earn what the lead did from the kill");
            Assert.AreEqual(after["Sela"],after[Third],"the hero resting didn't earn what the partner did from the kill");
            if(skill!=null)Assert.AreEqual(20,third.skillUses[skill.id],"the hero resting had their mastery changed while they rested");
        }

        // ------------------------------------------------------------------ the companion
        /// <summary>Guard: whoever stands beside the lead is the companion, with the companion's brain and share, and whoever leads has the pad; the hero resting is
        /// neither. Taren and Idra out, Sela resting: Idra is the companion, and after a swap Taren is.</summary>
        [UnityTest] public IEnumerator WhoeverStandsBesideTheLeadIsTheCompanionAndTheHeroRestingIsNeither()
        {
            Recruit();Assert.IsTrue(Deployment.Set(State,new[]{"Taren",Third}),"fixture: the pair is named");
            yield return Zone("Arena_Ground");Party.Apply();yield return null;
            for(int turn=0;turn<2;turn++)
            {
                var lead=Party.Active;var beside=Party.Partner;
                Assert.IsNotNull(beside,"nobody stands beside the lead");Assert.AreNotEqual("Sela",beside.character,"the hero resting is the companion");
                Assert.IsTrue(lead.GetComponent<PlayerBrain>().enabled&&!lead.GetComponent<PartnerBrain>().enabled,Named(lead.character)+" leads without the pad");
                Assert.IsTrue(beside.GetComponent<PartnerBrain>().enabled&&!beside.GetComponent<PlayerBrain>().enabled,Named(beside.character)+" stands beside the lead without the companion's brain");
                Assert.AreEqual(Party.PartnerShare,beside.damageScale,.001f,Named(beside.character)+" strikes with the lead's share, not the companion's");Assert.AreEqual(1,lead.damageScale,.001f);
                Assert.IsTrue(Party.Swap(),"fixture: the swap");yield return Frames(2);
            }
        }
    }
}
