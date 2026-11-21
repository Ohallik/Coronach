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
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Sela's chart (RouteChart, the pause menu's Chart page): it isn't in the menu until chapter 3 draws it, a place is on it once the party has stood there, travel is
    /// refused with its reason wherever a way out would be, the d-pad walks the stops, and saying yes arrives at the stop's own spawn with the party whole and the game saved.
    /// Every fixture is a party arriving at chapter 3, holding only what chapters 1 and 2 always set: so the same checks say an older save has its four places.</summary>
    public sealed class RouteChartTests
    {
        static readonly string[] First={"Decks","SorrelOutpost","TallowDrift","Lanternwick"};
        static readonly string[] Seven={"Party","Skills","Gear","Fabricate","Items","Quests","Settings"};
        Gamepad pad;
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            // Headless has no focused Game view. Route virtual devices into play.
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();PromptService.ForceDevice(null);SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter3");
            foreach(string id in First)Assert.IsNotNull(RouteChart.Find(id),$"fixture: the stop {id} isn't defined (lead: run Lattice.EditorTools.ContentScaffold.Apply, which makes chapters 1 and 2's four)");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var dialogue=DialogueSystem.Current;
            if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Menu;if(menu!=null&&menu.IsOpen)menu.Close();
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);
            pad=null;
            PromptService.ForceDevice(PromptDevice.Keyboard);PromptService.ForceDevice(null);InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the world
        static GameState State=>GameServices.Current.State;
        static PauseMenu Menu=>Object.FindFirstObjectByType<PauseMenu>();
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static ChartStop Place(string id)=>RouteChart.Find(id);
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone(zone,spawn);deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            yield return new WaitForSecondsRealtime(.5f);
        }
        static IEnumerator Until(System.Func<bool> done,float seconds){for(float end=Time.unscaledTime+seconds;!done()&&Time.unscaledTime<end;)yield return null;}
        /// <summary>Open floor far from the yard's walls, and a hero stood on it.</summary>
        static void Arena()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Chart fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static void Put(CombatActor hero,Vector3 at)
        {var body=hero.GetComponent<CharacterController>();body.enabled=false;hero.transform.position=at;body.enabled=true;Physics.SyncTransforms();}
        static Vector3 Flat(Vector3 v){v.y=0;return v;}

        // ------------------------------------------------------------------ what's on screen
        static Button[] Buttons()=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        static Button Row(string name)=>Buttons().FirstOrDefault(b=>b.name==name);
        static Button Stop(string id)
        {
            var found=Row(RouteChart.StopPrefix+id);
            Assert.IsNotNull(found,$"the chart shows no stop for {id}. On screen: {string.Join(" | ",Buttons().Select(b=>b.name))}");return found;
        }
        static TMP_Text Label(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
        static string Words(string name){var t=Label(name);return t!=null?t.text:"";}
        static string Words(Component control){var t=control.GetComponentInChildren<TMP_Text>();return t!=null?t.text:"";}
        static string Heading(){var t=Label("Heading");Assert.IsNotNull(t,"the page has no heading");return t.text;}
        /// <summary>What the chart last said no with.</summary>
        static string Said()=>Words("ChartSaid");
        static GameObject Selected()
        {var s=EventSystem.current.currentSelectedGameObject;Assert.IsNotNull(s,"nothing has focus: a pad player is stuck");return s;}
        static Vector2 At(Component control)=>((RectTransform)control.transform).anchoredPosition;
        static Rect Box(Component c){var corners=new Vector3[4];((RectTransform)c.transform).GetWorldCorners(corners);return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);}
        static string[] TabsTopDown()=>Buttons().Where(b=>b.name.StartsWith("Tab_")).OrderByDescending(b=>At(b).y).Select(b=>b.name.Substring(4)).ToArray();
        static IEnumerator Focus(Component control){EventSystem.current.SetSelectedGameObject(control.gameObject);yield return null;}
        void AddPad(){if(pad==null||!pad.added)pad=InputSystem.AddDevice<Gamepad>();pad.MakeCurrent();}
        IEnumerator Hold(GamepadState state){InputSystem.QueueStateEvent(pad,state);yield return null;yield return null;}
        IEnumerator Tap(GamepadButton button){yield return Hold(new GamepadState().WithButton(button));yield return Hold(new GamepadState());yield return null;}

        // ------------------------------------------------------------------ the page exists when the story says so
        [UnityTest] public IEnumerator TheMenuHasNoChartUntilTheStoryDrawsItAndThenItsTabStandsAfterQuests()
        {
            AddPad();yield return Zone("TallowDrift");
            Assert.IsFalse(RouteChart.Available,"fixture: arriving at chapter 3, nobody has drawn the chart yet");
            var menu=Menu;menu.Open();yield return null;
            CollectionAssert.AreEqual(Seven,TabsTopDown(),"before the chart exists the menu isn't the seven tabs it always was");
            Assert.AreEqual(7,menu.PageCount,"before the chart exists the menu counts a page nobody can turn to");
            yield return Tap(GamepadButton.LeftShoulder);
            StringAssert.Contains("SETTINGS",Heading(),"LB from the first page doesn't wrap to Settings, as it always did");
            menu.Close();yield return null;
            menu.Open(PauseMenu.ChartPage);yield return null;
            StringAssert.Contains("PARTY",Heading(),"asked for before it exists, the chart opens as something other than the first page");
            Assert.IsNull(Row("Tab_Chart"));
            menu.Close();yield return null;

            Flag(RouteChart.ChartedFlag);
            menu.Open();yield return null;
            CollectionAssert.AreEqual(new[]{"Party","Skills","Gear","Fabricate","Items","Quests","Chart","Settings"},TabsTopDown(),"the Chart tab doesn't stand after Quests");
            Assert.AreEqual(8,menu.PageCount);
            // Every page there was keeps its number: the chart's comes after them all.
            Assert.AreEqual(5,PauseMenu.QuestsPage);Assert.AreEqual(6,PauseMenu.SettingsPage);Assert.AreEqual(7,PauseMenu.ChartPage);
            menu.Close();yield return null;menu.Open(PauseMenu.QuestsPage);yield return null;
            yield return Tap(GamepadButton.RightShoulder);StringAssert.Contains("CHART",Heading(),"RB from Quests doesn't turn to the chart");
            Assert.IsNotNull(Row("Tab_Chart").transform.Find("ActiveTab"),"nothing marks Chart as the open tab");
            yield return Tap(GamepadButton.RightShoulder);StringAssert.Contains("SETTINGS",Heading(),"RB from the chart doesn't turn to Settings");
            yield return Tap(GamepadButton.RightShoulder);StringAssert.Contains("PARTY",Heading(),"RB past the last tab doesn't wrap to the first");
            yield return Tap(GamepadButton.LeftShoulder);yield return Tap(GamepadButton.LeftShoulder);
            StringAssert.Contains("CHART",Heading(),"LB from Settings doesn't turn back to the chart");
            menu.Close();
        }

        // ------------------------------------------------------------------ charted, uncharted, closed
        [UnityTest] public IEnumerator APlaceIsOnTheChartOnceThePartyHasStoodThereAndClosedWhileTheStoryShutsIt()
        {
            // Nothing a save from before the chart existed doesn't hold: no visited flag anywhere, and all four places are the party's.
            Assert.IsFalse(State.flags.Keys.Any(k=>k.StartsWith(ChartStop.VisitedPrefix)),"fixture: a party arriving at chapter 3 holds no visited flag");
            foreach(string id in First)Assert.AreNotEqual(RouteChart.Standing.Uncharted,RouteChart.Of(Place(id),State),$"a save that finished chapters 1 and 2 before the chart existed has never been to {Place(id).displayName}");
            Flag(RouteChart.ChartedFlag);Flag("met.Aumi",false);   // as if the road had never reached Lanternwick
            yield return Zone("TallowDrift");
            Assert.AreEqual(RouteChart.Standing.Here,RouteChart.Of(Place("TallowDrift"),State));
            Assert.AreEqual(RouteChart.Standing.Open,RouteChart.Of(Place("Decks"),State));
            Assert.AreEqual(RouteChart.Standing.Open,RouteChart.Of(Place("SorrelOutpost"),State));
            Assert.AreEqual(RouteChart.Standing.Uncharted,RouteChart.Of(Place("Lanternwick"),State));
            var menu=Menu;menu.Open(PauseMenu.ChartPage);yield return null;
            StringAssert.Contains("CHART",Heading());
            Assert.AreEqual("The Decks",Words(Stop("Decks")));Assert.AreEqual("Uncharted",Words(Stop("Lanternwick")),"a place nobody has stood in shows its name");
            var every=RouteChart.Stops;
            StringAssert.Contains($"{every.Count(s=>RouteChart.Of(s,State)!=RouteChart.Standing.Uncharted)} of {every.Length} places charted",Words("Note"),"the note doesn't count what's charted");
            // An uncharted place takes the cursor, says only that, and can't be chosen.
            yield return Focus(Stop("Lanternwick"));
            Assert.AreEqual("Uncharted",Words("ChartStopName"));
            StringAssert.DoesNotContain("Lantern",Words("ChartStopWhere")+Words("ChartStopBlurb"),"an uncharted place gives itself away under the cursor");
            Stop("Lanternwick").onClick.Invoke();yield return null;
            StringAssert.Contains("Uncharted",Said(),"a press on an uncharted place doesn't say why nothing happened");
            Assert.IsNull(Row("Travel"),"an uncharted place can be chosen");Assert.IsFalse(SceneFlow.Current.Loading);
            Assert.AreEqual("Stop_Lanternwick",Selected().name,"a refusal takes the cursor off the stop it was about");
            // The place the party is in says so, and isn't somewhere to travel to.
            yield return Focus(Stop("TallowDrift"));
            Assert.AreEqual("Tallow Drift",Words("ChartStopName"));StringAssert.Contains("YOU ARE HERE",Words("ChartStopStatus"));
            Stop("TallowDrift").onClick.Invoke();yield return null;
            StringAssert.Contains("already",Said());Assert.IsNull(Row("Travel"),"the chart offers travel to where the party already is");
            // A charted place says what it is and what's there to go back for.
            yield return Focus(Stop("Decks"));
            Assert.AreEqual("The Decks",Words("ChartStopName"));StringAssert.Contains("Cinder Halo",Words("ChartStopWhere"));
            foreach(string service in new[]{"Shop","Bench","Repair"})StringAssert.Contains(service,Words("ChartStopWhere"),"the Decks' "+service.ToLowerInvariant()+" isn't listed");
            Assert.AreEqual(Place("Decks").blurb,Words("ChartStopBlurb"));
            menu.Close();yield return null;

            // A story beat closes a place: it keeps its name, says why, and takes nobody until the beat has passed.
            var decks=Place("Decks");string wasFlag=decks.lockedFlag,wasReason=decks.lockedReason;
            try
            {
                decks.lockedFlag="test.decksSealed";decks.lockedReason="The Decks are sealed while the ring is cut loose.";Flag("test.decksSealed");
                Assert.AreEqual(RouteChart.Standing.Closed,RouteChart.Of(decks,State));
                menu.Open(PauseMenu.ChartPage);yield return null;
                Assert.AreEqual("The Decks",Words(Stop("Decks")),"a closed place loses its name");
                yield return Focus(Stop("Decks"));
                StringAssert.Contains("CLOSED",Words("ChartStopStatus"));StringAssert.Contains(decks.lockedReason,Words("ChartStopBlurb"),"a closed place doesn't say why under the cursor");
                Stop("Decks").onClick.Invoke();yield return null;
                Assert.AreEqual(decks.lockedReason,Said(),"a press on a closed place doesn't say why");Assert.IsNull(Row("Travel"),"a closed place can be chosen");
                menu.Close();yield return null;
                Flag("test.decksSealed",false);
                Assert.AreEqual(RouteChart.Standing.Open,RouteChart.Of(decks,State),"a place stays closed after its beat has passed");
                Assert.IsNull(RouteChart.Refusal(decks));
            }
            finally{decks.lockedFlag=wasFlag;decks.lockedReason=wasReason;}
        }
        [UnityTest] public IEnumerator StandingInALaterPlaceChartsItAndTheFirstChaptersZonesStillWriteNothing()
        {
            yield return Zone("Hub_Decks");
            Assert.IsFalse(State.flags.Keys.Any(k=>k.StartsWith(ChartStop.VisitedPrefix)),"entering a zone of chapters 1 and 2 writes a visited flag: their saves are no longer what they were");
            var later=ContentRegistry.ZoneDefs.FirstOrDefault(z=>z.chapter>=RouteChart.FirstVisitedChapter);
            Assert.IsNotNull(later,"fixture: a zone of chapter 3 or later is defined");
            // A stop with no flag of its own is charted by its zone's visited flag, which arriving sets.
            var stop=ScriptableObject.CreateInstance<ChartStop>();stop.id="Fixture";stop.displayName="Fixture";stop.zone=later.name;
            Assert.AreEqual(ChartStop.VisitedPrefix+later.name,stop.UnlockFlag);
            Assert.AreEqual(RouteChart.Standing.Uncharted,RouteChart.Of(stop,State),"a place nobody has stood in is charted");
            StringAssert.Contains("Uncharted",RouteChart.Refusal(stop));
            RouteChart.Arrive(later.name);
            Assert.IsTrue(GameServices.Current.Flags.GetBool(ChartStop.VisitedPrefix+later.name),"arriving in "+later.name+" doesn't mark it visited");
            Assert.AreEqual(RouteChart.Standing.Open,RouteChart.Of(stop,State),"having stood there doesn't chart the place");
            RouteChart.Arrive("Hub_Decks");RouteChart.Arrive("Arena_Ground");
            Assert.AreEqual(1,State.flags.Keys.Count(k=>k.StartsWith(ChartStop.VisitedPrefix)),"a zone of the first two chapters, or a yard, was marked visited");
            Object.Destroy(stop);
            // A party that has only finished chapter 1 has stood in its three places and not in Lanternwick.
            GameServices.Current.NewGame();DevLoadout.Apply("chapter2");
            foreach(string id in new[]{"Decks","SorrelOutpost","TallowDrift"})Assert.AreEqual(RouteChart.Standing.Open,RouteChart.Of(Place(id),State),"arriving at chapter 2 without having stood in "+Place(id).displayName);
            Assert.AreEqual(RouteChart.Standing.Uncharted,RouteChart.Of(Place("Lanternwick"),State),"Lanternwick is charted before the road reaches it");
        }

        // ------------------------------------------------------------------ refusals
        [UnityTest] public IEnumerator TravelWaitsForAnEnemyNearbyABossFightAndAConversationAndSaysWhich()
        {
            Flag(RouteChart.ChartedFlag);yield return Zone("Arena_Ground");Arena();
            var party=PartyController.Current;var lead=party.Active;var decks=Place("Decks");
            for(int i=0;i<party.members.Length;i++){Put(party.members[i],new Vector3(200+i*3,.1f,0));party.members[i].Health.InvulnerableUntil=GameTime.Now+120;}
            yield return null;
            Assert.IsNull(RouteChart.Refusal(decks),"fixture: with nothing awake the way to the Decks is clear");
            // An ordinary enemy awake within a quiet interaction's reach.
            var hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),new Vector3(200,.1f,9));yield return null;yield return null;
            Assert.IsTrue(EnemyBrain.AwakeNear(lead.transform.position,EnemyBrain.InteractionCalm),"fixture: a hound is awake beside the party");
            StringAssert.Contains("enemy",RouteChart.Refusal(decks),"with an enemy awake nearby the chart doesn't say that's why");
            var menu=Menu;menu.Open(PauseMenu.ChartPage);yield return null;
            Stop("Decks").onClick.Invoke();yield return null;
            StringAssert.Contains("enemy",Said(),"the page doesn't show why travel is refused");
            Assert.IsNull(Row("Travel"),"the chart asks about travelling out of a fight");Assert.IsFalse(SceneFlow.Current.Loading,"the chart left a fight");
            menu.Close();yield return null;
            hound.Passive=true;
            Assert.IsNull(RouteChart.Refusal(decks),"an enemy gone quiet still holds the chart");
            // A boss fight under way, with the boss itself well outside that reach.
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Burrower"),new Vector3(200,.1f,32));yield return null;yield return null;yield return null;
            boss.Health.Receive(new DamagePacket{source=lead.Health,amount=1,type=DamageType.Pulse});yield return null;yield return null;
            Assert.IsTrue(BossController.AnyFightUnderway,"fixture: the Burrower's fight is under way");
            StringAssert.Contains("boss",RouteChart.Refusal(decks),"in a boss fight the chart doesn't say that's why");
            Assert.IsFalse(RouteChart.Travel(decks));Assert.IsFalse(SceneFlow.Current.Loading,"the chart left a boss fight");
            boss.Passive=true;yield return null;
            Assert.IsNull(RouteChart.Refusal(decks),"a boss gone quiet still holds the chart");
            // A conversation.
            yield return Until(()=>DialogueSystem.Current.Prepared,10);
            Assert.IsTrue(DialogueSystem.Current.StartNode("MiraFirst","Mira"),"fixture: Mira's first words start");yield return new WaitForSecondsRealtime(.2f);
            Assert.IsTrue(DialogueSystem.Current.Running,"fixture: a conversation is running");
            StringAssert.Contains("conversation",RouteChart.Refusal(decks),"in a conversation the chart doesn't say that's why");
            Assert.IsFalse(RouteChart.Travel(decks));Assert.IsFalse(SceneFlow.Current.Loading,"the chart left a conversation");
            DialogueSystem.Current.Cancel();yield return Until(()=>!DialogueSystem.Current.Running,5);
            Assert.IsNull(RouteChart.Refusal(decks),"the chart stays shut after the conversation is over");
        }
        [UnityTest] public IEnumerator TravelWaitsForAFightInFlightToBeCleared()
        {
            Flag(RouteChart.ChartedFlag);Flag("clear.Wick_0",false);
            yield return Zone("WickReach");
            Assert.AreEqual(ZoneKind.SpaceCombat,ZoneController.Current.definition.kind,"fixture: the Wick Reach is a flight lane");
            var decks=Place("Decks");
            Assert.IsNull(RouteChart.Refusal(decks),"with no fight begun the chart doesn't answer in a flight lane");
            var fight=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).FirstOrDefault(e=>e.encounterId=="Wick_0");
            Assert.IsNotNull(fight,"fixture: the lane's first fight");
            fight.Begin();yield return null;
            Assert.Greater(fight.Remaining,0,"fixture: the lane's first fight has begun");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            // Nothing is awake beside the party now: it's the fight itself, begun and not cleared, that holds the chart.
            Assert.IsFalse(EnemyBrain.AwakeNear(PartyController.Current.Active.transform.position,EnemyBrain.InteractionCalm),"fixture: nothing awake beside the party");
            StringAssert.Contains("flight",RouteChart.Refusal(decks),"mid-fight in a flight lane the chart doesn't say that's why");
            Assert.IsFalse(RouteChart.Travel(decks));Assert.IsFalse(SceneFlow.Current.Loading,"the chart left a fight in flight");
        }

        // ------------------------------------------------------------------ the d-pad
        [UnityTest] public IEnumerator TheDPadMovesTheCursorToTheNearestPlaceInThePressedDirection()
        {
            AddPad();Flag(RouteChart.ChartedFlag);yield return Zone("TallowDrift");
            var stops=RouteChart.Stops;
            Assert.IsEmpty(RouteChart.LayoutFaults(stops),"as placed, the stops can't all be read or reached");
            var menu=Menu;menu.Open(PauseMenu.ChartPage);yield return null;
            Assert.AreEqual("Stop_TallowDrift",Selected().name,"the chart doesn't open with the cursor on the place the party is at");
            var at=stops.Select(s=>At(Stop(s.id))).ToList();var reached=new HashSet<string>();
            var ways=new[]{(GamepadButton.DpadUp,Vector2.up),(GamepadButton.DpadRight,Vector2.right),(GamepadButton.DpadDown,Vector2.down),(GamepadButton.DpadLeft,Vector2.left)};
            for(int from=0;from<stops.Length;from++)foreach(var (button,way) in ways)
            {
                yield return Focus(Stop(stops[from].id));yield return Tap(button);
                string now=Selected().name,start=RouteChart.StopPrefix+stops[from].id;int next=RouteChart.Neighbour(at,from,way);
                if(next>=0)
                {
                    Assert.AreEqual(RouteChart.StopPrefix+stops[next].id,now,$"{button} from {stops[from].displayName} doesn't go to the nearest place that way");
                    Assert.Greater(Vector2.Dot(At(Selected().transform)-at[from],way),0,$"{button} from {stops[from].displayName} moved the cursor the other way");
                    reached.Add(stops[next].id);
                }
                // With nothing that way: left is the page's tab and down is Back, as on every page; up and right leave the cursor where it is.
                else Assert.AreEqual(button==GamepadButton.DpadLeft?"Tab_Chart":button==GamepadButton.DpadDown?"Close":start,now,$"{button} from {stops[from].displayName}, with no place that way");
            }
            CollectionAssert.AreEquivalent(stops.Select(s=>s.id),reached,"a place no single press brings the cursor to from any other");
            // On and off the chart: right from the tabs and up from Back come back to it.
            yield return Focus(Row("Tab_Chart"));yield return Tap(GamepadButton.DpadRight);
            StringAssert.StartsWith(RouteChart.StopPrefix,Selected().name,"right from the tab column doesn't come back to the chart");
            yield return Focus(Row("Close"));yield return Tap(GamepadButton.DpadUp);
            StringAssert.StartsWith(RouteChart.StopPrefix,Selected().name,"up from Back doesn't come back to the chart");
            yield return Tap(GamepadButton.RightShoulder);StringAssert.Contains("SETTINGS",Heading(),"RB doesn't change page from the chart");
            yield return Tap(GamepadButton.LeftShoulder);StringAssert.Contains("CHART",Heading(),"LB doesn't change page back to the chart");
            menu.Close();
        }
        [UnityTest] public IEnumerator TheReplayDriverIsToldWhichWayToPressForANamedStop()
        {
            AddPad();Flag(RouteChart.ChartedFlag);yield return Zone("TallowDrift");
            var menu=Menu;menu.Open(PauseMenu.ChartPage);yield return null;
            Assert.IsNull(RouteChart.Toward("Row_3"),"a row that isn't a stop is steered by the chart");
            foreach(var stop in RouteChart.Stops.Reverse())
            {
                string want=RouteChart.StopPrefix+stop.id;
                for(int presses=0;presses<12&&Selected().name!=want;presses++)
                {
                    var way=RouteChart.Toward(want);
                    Assert.IsNotNull(way,$"from {Selected().name} the chart can't say which way {want} is");
                    yield return Tap(way.Value);
                }
                Assert.AreEqual(want,Selected().name,"following the chart's own directions never reaches "+stop.displayName);
            }
            yield return Focus(Row("Tab_Chart"));
            Assert.AreEqual(GamepadButton.DpadRight,RouteChart.Toward(RouteChart.StopPrefix+"Decks"),"from the tab column the way onto the chart isn't right");
            yield return Focus(Row("Close"));
            Assert.AreEqual(GamepadButton.DpadUp,RouteChart.Toward(RouteChart.StopPrefix+"Decks"),"from Back the way onto the chart isn't up");
            menu.Close();yield return null;
            Assert.IsNull(RouteChart.Toward(RouteChart.StopPrefix+"Decks"),"with the menu shut the chart still steers a press");
        }

        // ------------------------------------------------------------------ travelling
        [UnityTest] public IEnumerator TravellingAsksFirstThenArrivesAtThePlacesSpawnWithThePartyWholeAndTheGameSaved()
        {
            Flag(RouteChart.ChartedFlag);yield return Zone("TallowDrift");
            var who=State.party.Select(m=>m.id+" "+m.level).ToArray();int scrip=State.scrip;
            var menu=Menu;menu.Open(PauseMenu.ChartPage);yield return null;
            Stop("Lanternwick").onClick.Invoke();yield return null;
            var go=Row("Travel");Assert.IsNotNull(go,"choosing a charted place goes without asking");
            Assert.AreEqual("Travel — Lanternwick",Words(go));Assert.AreEqual("Travel",Selected().name,"the question doesn't open on its yes");
            // The press that asked is often pressed twice: the second doesn't answer.
            go.onClick.Invoke();yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"a double press on the place went straight through the question");Assert.IsTrue(menu.IsOpen);
            // B steps back to the chart, on the place asked about, and the menu stays.
            menu.StepBack();yield return null;
            Assert.IsNull(Row("Travel"),"B doesn't take the question down");Assert.IsTrue(menu.IsOpen,"B on the question closed the whole menu");
            Assert.AreEqual("Stop_Lanternwick",Selected().name,"B on the question loses the place it was about");
            Stop("Lanternwick").onClick.Invoke();yield return null;
            Row("Stay").onClick.Invoke();yield return null;
            Assert.IsNull(Row("Travel"),"Stay doesn't take the question down");Assert.AreEqual("TallowDrift",SceneFlow.Current.Zone);Assert.IsFalse(SceneFlow.Current.Loading);
            // And yes goes.
            var heard=new List<string>();void Listen(string text,string stack,LogType type){if(text.StartsWith("CHART_TRAVEL")||text.StartsWith("ZONE_ENTER"))heard.Add(text);}
            Stop("Lanternwick").onClick.Invoke();yield return new WaitForSecondsRealtime(FirstPress.Hold+.1f);
            Application.logMessageReceived+=Listen;
            try
            {
                Row("Travel").onClick.Invoke();
                Assert.IsTrue(SceneFlow.Current.Loading,"saying yes went nowhere");
                yield return Until(()=>!SceneFlow.Current.Loading,30);
            }
            finally{Application.logMessageReceived-=Listen;}
            Assert.IsFalse(SceneFlow.Current.Loading,"the way to Lanternwick never finished loading");
            CollectionAssert.AreEqual(new[]{"CHART_TRAVEL Lanternwick Lanternwick Arrival","ZONE_ENTER Lanternwick"},heard);
            yield return new WaitForSecondsRealtime(.4f);
            Assert.AreEqual("Lanternwick",SceneFlow.Current.Zone);Assert.AreEqual("Lanternwick",State.zone);Assert.AreEqual("Arrival",State.spawn);
            var spawn=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).FirstOrDefault(s=>s.id=="Arrival");Assert.IsNotNull(spawn,"fixture: Lanternwick's arrival");
            var heroes=PartyController.Current.members;
            Assert.AreEqual(who.Length,heroes.Length,"somebody was left behind");
            foreach(var hero in heroes)
            {
                Assert.IsTrue(hero.Health.Alive,hero.character+" arrived down");
                Assert.Less(Flat(hero.transform.position-spawn.transform.position).magnitude,4,hero.character+" didn't arrive at the stop's spawn");
            }
            CollectionAssert.AreEqual(who,State.party.Select(m=>m.id+" "+m.level).ToArray(),"the party changed on the way");Assert.AreEqual(scrip,State.scrip,"travelling cost scrip");
            // The zone's own rules: a town is walked in a natural body, and the menu and the clock are let go.
            Assert.AreEqual(BodyForm.Natural,ZoneController.Current.Form);
            yield return Until(()=>PartyController.Current.Active.GetComponent<FormController>().Current==BodyForm.Natural,5);
            Assert.AreEqual(BodyForm.Natural,PartyController.Current.Active.GetComponent<FormController>().Current,"the party didn't take the town's form");
            Assert.IsFalse(Menu.IsOpen);Assert.IsFalse(GameTime.Paused,"the clock is still stopped after arriving");
            SaveSystem.Drain();var saved=GameServices.Current.Saves.Load("autosave");
            Assert.IsNotNull(saved,"arriving didn't save");
            Assert.AreEqual("Lanternwick",saved.zone,"the autosave isn't of the place arrived at");Assert.AreEqual("Arrival",saved.spawn);Assert.AreEqual(who.Length,saved.party.Count);
        }
        [UnityTest] public IEnumerator EveryStopArrivesAtASpawnThatStandsInItsZoneBesideTheServicesItLists()
        {
            foreach(var stop in RouteChart.Stops)
            {
                Assert.IsNotNull(GameCatalog.Find<ZoneDef>(stop.zone),$"{stop.name} arrives in {stop.zone}, which is no zone");
                Assert.IsTrue(Application.CanStreamedLevelBeLoaded(stop.zone),$"{stop.displayName} arrives in {stop.zone}, which isn't in the build: the chart refuses it until its zone is built");
                yield return Zone(stop.zone,stop.spawn);
                var spawn=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).FirstOrDefault(s=>s.id==stop.spawn);
                Assert.IsNotNull(spawn,$"{stop.displayName} arrives at the spawn '{stop.spawn}', and {stop.zone} has none by that id: the party would land at the zone's fallback point");
                Assert.Less(Flat(PartyController.Current.Active.transform.position-spawn.transform.position).magnitude,4,$"{stop.displayName}: the party didn't arrive at '{stop.spawn}'");
                if(stop.bench)Assert.IsNotEmpty(Object.FindObjectsByType<Bench>(FindObjectsSortMode.None),stop.displayName+" lists a bench and has none");
                if(stop.repair)Assert.IsNotEmpty(Object.FindObjectsByType<RepairBay>(FindObjectsSortMode.None),stop.displayName+" lists a repair and has none");
                foreach(string reached in stop.reaches??System.Array.Empty<string>())Assert.IsNotNull(GameCatalog.Find<ZoneDef>(reached),$"{stop.name} reaches {reached}, which is no zone");
            }
        }

        // ------------------------------------------------------------------ what the chart marks, and that it reads
        [UnityTest] public IEnumerator TheChartMarksWhereThePartyIsAndWhereTheStorysNextStepLies()
        {
            Flag(RouteChart.ChartedFlag);
            // A zone belongs to the stop that arrives there, or the one it's reached from.
            Assert.AreEqual("Lanternwick",RouteChart.At("Lanternwick")?.id);Assert.AreEqual("Lanternwick",RouteChart.At("LanternGorge")?.id);
            Assert.AreEqual("SorrelOutpost",RouteChart.At("Hushwell")?.id);Assert.AreEqual("Decks",RouteChart.At("Hub_CinderHalo")?.id);Assert.IsNull(RouteChart.At("Arena_Ground"));
            // The Lantern Road, open again at its first step: fly the Wick Reach, which is reached from Lanternwick.
            foreach(string id in StoryQuests.Ordered)if(System.Array.IndexOf(StoryQuests.First,id)<0)State.questSteps.Remove(id);
            State.questSteps["LanternRoad"]=0;
            Assert.AreEqual("Lanternwick",RouteChart.Objective(State)?.id,"a step that reaches the Wick Reach isn't marked at Lanternwick, where the Reach is flown from");
            State.questSteps["LanternRoad"]=2;
            Assert.IsNull(RouteChart.Objective(State),"a step that names no place is marked somewhere");
            State.questSteps["LanternRoad"]=1;
            Assert.AreEqual("Lanternwick",RouteChart.Objective(State)?.id,"a step that reaches Lanternwick isn't marked there");
            yield return Zone("TallowDrift");
            var menu=Menu;menu.Open(PauseMenu.ChartPage);yield return null;
            var arrow=Object.FindObjectsByType<Image>(FindObjectsSortMode.None).FirstOrDefault(i=>i.name=="ChartParty");
            var diamond=Object.FindObjectsByType<Image>(FindObjectsSortMode.None).FirstOrDefault(i=>i.name=="ChartObjective");
            Assert.IsNotNull(arrow,"nothing marks where the party is");Assert.IsNotNull(diamond,"nothing marks where the story goes next");
            Assert.AreEqual(At(Stop("TallowDrift")).x,At(arrow).x,.5f,"the party's mark isn't over Tallow Drift");Assert.Greater(At(arrow).y,At(Stop("TallowDrift")).y);
            Assert.Less(Vector2.Distance(At(diamond),At(Stop("Lanternwick"))),20,"the story's mark isn't on Lanternwick");
            yield return Focus(Stop("Lanternwick"));StringAssert.Contains("NEXT STEP",Words("ChartStopStatus"));
            yield return Focus(Stop("TallowDrift"));StringAssert.Contains("YOU ARE HERE",Words("ChartStopStatus"));StringAssert.DoesNotContain("NEXT STEP",Words("ChartStopStatus"));
            StringAssert.Contains("Tallow Drift",Words("Note"),"the note doesn't say where the party is");
            menu.Close();
        }
        [UnityTest] public IEnumerator EveryWordOnTheChartIsAtTheTypeFloorAndEveryNameFitsUnderItsOwnLantern()
        {
            Flag(RouteChart.ChartedFlag);yield return Zone("TallowDrift");
            var menu=Menu;menu.Open(PauseMenu.ChartPage);yield return null;yield return null;
            var failures=new List<string>();
            foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Where(t=>{var c=t.GetComponentInParent<Canvas>();return c!=null&&c.rootCanvas.name=="Menus";}))
                if(!string.IsNullOrEmpty(text.text)&&text.fontSize<24)failures.Add($"\"{text.text}\" is set at {text.fontSize:0}, under the floor of 24");
            var field=Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None).FirstOrDefault(g=>g.name=="ChartField");
            Assert.IsNotNull(field,"the chart has no field behind it");
            var names=new List<(string id,Rect ink)>();
            foreach(var stop in RouteChart.Stops)
            {
                var label=Stop(stop.id).GetComponentInChildren<TMP_Text>();var box=Box(label);float wide=label.GetPreferredValues(RouteChart.Name(stop)).x*label.canvas.scaleFactor;
                if(wide>box.width+1)failures.Add($"{stop.displayName} needs {wide:0} under its lantern and has {box.width:0}");
                // The letters as drawn, centred in the label: what another name mustn't stand in, and what has to stay on the field.
                var ink=Rect.MinMaxRect(box.center.x-wide*.5f,box.yMin,box.center.x+wide*.5f,box.yMax);
                if(ink.xMin<Box(field).xMin||ink.xMax>Box(field).xMax||ink.yMin<Box(field).yMin||ink.yMax>Box(field).yMax)failures.Add($"{stop.displayName}'s name runs off the chart");
                foreach(var other in names)if(other.ink.Overlaps(ink))failures.Add($"{stop.displayName}'s name stands in {other.id}'s");
                names.Add((stop.displayName,ink));
                // What the page says about it fits the lines it has.
                yield return Focus(Stop(stop.id));
                foreach(string line in new[]{"ChartStopName","ChartStopWhere","ChartStopBlurb"})
                {
                    var words=Label(line);var room=words.rectTransform.rect;var need=words.GetPreferredValues(words.text,room.width,0);
                    if(need.y>room.height+1)failures.Add($"{stop.displayName}: \"{words.text}\" needs {need.y:0} of height in {line} and has {room.height:0}");
                }
            }
            // The question fits its rows too.
            Stop("Decks").onClick.Invoke();yield return null;
            foreach(string row in new[]{"Travel","Stay"})
            {
                var words=Row(row).GetComponentInChildren<TMP_Text>();var room=words.rectTransform.rect;var need=words.GetPreferredValues(words.text,room.width-words.margin.x-words.margin.z,0);
                if(need.y>room.height+1)failures.Add($"\"{words.text}\" needs {need.y:0} in a {room.height:0} tall row");
            }
            menu.Close();
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
