using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The November QA pass, TEST_PLAN 4.1 to 4.3 taken from the player's side: what the title and the first morning offer, what
    /// each press on a keyboard, an analog pad and a Precision answers, how a hull behaves against the world, and what the progression
    /// says it will do. Written from the plan, not from the code, so some of these are expected to fail: each failure is a finding.</summary>
    public sealed class QaPlanEarlyTests
    {
        Gamepad pad;Keyboard keyboard;InputDevice joystick;bool digital;
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        string heard;
        void Listen(string node,string speaker,System.Action<bool> done)=>heard=node;

        // D197's paces, mirrored from the decision rather than read from the motor.
        const float CivilWalk=1.5f,CivilJog=4.5f,CivilSprint=6.6f,CombatRun=6.7f,CombatSprint=CombatRun*1.55f;
        /// <summary>Open ground no zone's own geometry reaches.</summary>
        static readonly Vector3 Open=new Vector3(300,0,300);

        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            // Headless has no focused Game view. Route virtual devices into play.
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();PromptService.ForceDevice(null);SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");heard=null;digital=false;Npc.TalkRequested+=Listen;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Npc.TalkRequested-=Listen;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var shop=Object.FindFirstObjectByType<ShopUi>();if(shop!=null&&shop.IsOpen)shop.Close();
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            if(joystick!=null&&joystick.added)InputSystem.RemoveDevice(joystick);joystick=null;
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);pad=null;
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);keyboard=null;
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            // Back to the default device, which also drops any tag cached off another device's words.
            PromptService.ForceDevice(PromptDevice.Keyboard);PromptService.ForceDevice(null);
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the world
        static GameState State=>GameServices.Current.State;
        static SaveSystem Saves=>GameServices.Current.Saves;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            Quiet();
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            DialogueSystem.Current.AutoAdvance=false;
            deadline=Time.unscaledTime+3;while(PartyController.Current.members.Any(m=>m.ChangingForm)&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.35f);
        }
        /// <summary>Enemies stand, encounters stay asleep however the fixture walks in, and nothing moves a hero but the test.</summary>
        static void Quiet()
        {
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var volume in All<EncounterVolume>())volume.enabled=false;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
        }
        static CombatActor Lead(){var hero=PartyController.Current.Active;foreach(var member in PartyController.Current.members)member.GetComponent<PlayerBrain>().AutoPilot=false;return hero;}
        static void Put(CombatActor hero,Vector3 at)
        {
            var body=hero.GetComponent<CharacterController>();body.enabled=false;
            if(hero.TryGetComponent<FlightMotor>(out var ship))ship.Halt();
            if(hero.TryGetComponent<GroundMotor>(out var feet))feet.Halt();
            hero.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        /// <summary>Everyone but the lead, out of the way of what the lead is about to do.</summary>
        static void Park(CombatActor hero,Vector3 at){foreach(var member in PartyController.Current.members)if(member!=hero)Put(member,at);}
        static GameObject Floor(Vector3 centre,float size)
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Plan fixture floor";
            floor.transform.position=centre+Vector3.down*.5f;floor.transform.localScale=new Vector3(size,1,size);Physics.SyncTransforms();return floor;
        }
        static EnemyBrain Enemy(string id,Vector3 at,bool passive=true){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=passive;return enemy;}
        static float Planar(CombatActor actor){var v=actor.motor.Velocity;v.y=0;return v.magnitude;}
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator UntilFree(CombatActor hero,float seconds=3){for(float end=Time.unscaledTime+seconds;!hero.CanAct&&Time.unscaledTime<end;)yield return null;}
        static IEnumerator UntilTime(float gameTime){while(GameTime.Now<gameTime)yield return null;}
        static IEnumerator Finish(float seconds=25)
        {
            float deadline=Time.unscaledTime+seconds;
            while((DialogueSystem.Current.Running||GameInput.Current.Blocked)&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.2f);
        }
        static DamagePacket Incoming()=>new DamagePacket{amount=20,type=DamageType.Pulse};
        /// <summary>The world direction, as a stick held in the camera's frame.</summary>
        static Vector2 Stick(Vector3 world)
        {
            var local=Quaternion.Euler(0,-ZoneController.Current.definition.cameraProfile.yaw,0)*world;
            return new Vector2(local.x,local.z).normalized;
        }
        static Vector3 Heading(Vector2 stick)=>Quaternion.Euler(0,ZoneController.Current.definition.cameraProfile.yaw,0)*new Vector3(stick.x,0,stick.y);
        /// <summary>How fast the body really covers the ground: the distance it moved over a window, once it has had time to settle.</summary>
        static IEnumerator Pace(CombatActor hero,float settle,float window,System.Action<float> said)
        {
            yield return new WaitForSecondsRealtime(settle);
            var from=hero.transform.position;float began=Time.unscaledTime;
            yield return new WaitForSecondsRealtime(window);
            said(Flat(from,hero.transform.position)/Mathf.Max(.001f,Time.unscaledTime-began));
        }

        // ------------------------------------------------------------------ what's on screen
        static TMP_Text Text(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
        static Button[] Buttons()=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        static Button Row(string name)=>Buttons().FirstOrDefault(b=>b.name==name);
        static string Words(Component c){var t=c.GetComponentInChildren<TMP_Text>();return t!=null?t.text:"";}
        static Button With(string words)
        {
            var found=Buttons().FirstOrDefault(b=>Words(b).Contains(words));
            Assert.IsNotNull(found,$"no row says \"{words}\". On screen: {string.Join(" | ",Buttons().Select(b=>Words(b)))}");return found;
        }
        static string Objective()=>Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Objective").text;
        static string Feedback(){var t=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(n=>n.name=="Feedback");return t!=null?t.text:"";}
        static IEnumerator ToTitle()
        {
            SceneFlow.Current.LoadZone("Title");float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<TitleScreen>(),"fixture: the title never came up");
            yield return null;yield return null;
        }
        static void ClearSaves()
        {
            // The batch runner's save shield holds the player's real saves elsewhere for the whole run.
            if(!Application.isBatchMode)return;
            foreach(string slot in new[]{"autosave","slot1","slot2","slot3"})foreach(string suffix in new[]{"",".bak",".tmp"})
            {var path=Saves.PathForSlot(slot)+suffix;if(File.Exists(path))File.Delete(path);}
            Saves.ResumeFrom("autosave");
        }

        // ------------------------------------------------------------------ the devices
        [StructLayout(LayoutKind.Sequential)]
        struct JoyState:IInputStateTypeInfo
        {
            public int buttons;public Vector2 stick;
            public FourCC format=>new FourCC('J','O','Y');
        }
        static int HidButton(int n)=>n==1?1<<4:1<<(n+3);
        static bool precisionRegistered;
        static string PrecisionLayout()
        {
            var controls=string.Join(",",Enumerable.Range(2,11).Select(n=>$"{{ \"name\": \"button{n}\", \"layout\": \"Button\", \"format\": \"BIT\", \"offset\": 0, \"bit\": {n+3}, \"sizeInBits\": 1 }}"));
            return "{ \"name\": \"QaPlanEarlyPrecision\", \"extend\": \"Joystick\", \"device\": { \"interface\": \"QaPlanEarly\", \"product\": \"Logitech.*Precision.*\" }, \"controls\": [ "+controls+" ] }";
        }
        /// <summary>The real thing, bridged as Nathan's is: a joystick with ten buttons whose only directions are its d-pad.</summary>
        IEnumerator AddPrecision()
        {
            PadSupport.Install();if(!PadBridge.Installed)PadBridge.Install();
            if(!precisionRegistered){InputSystem.RegisterLayout(PrecisionLayout());precisionRegistered=true;}
            joystick=InputSystem.AddDevice(new InputDeviceDescription{interfaceName="QaPlanEarly",product="Logitech(R) Precision(TM) Gamepad"});yield return null;
            pad=PadBridge.PadFor(joystick);Assert.IsNotNull(pad,"fixture: the Precision isn't bridged: "+PadBridge.Describe());
            Assert.IsTrue(PadBridge.StickIsDpad(pad),"fixture: this pad's directions are its d-pad");
            pad.MakeCurrent();digital=true;yield return null;
        }
        IEnumerator Use(string device)
        {
            if(device=="keyboard"){keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();yield return null;}
            else if(device=="precision")yield return AddPrecision();
            else{pad=InputSystem.AddDevice<Gamepad>("QaPlanEarlyPad");pad.MakeCurrent();yield return null;}
        }
        /// <summary>A state on the pad, then the two frames the plan's responsiveness bar allows.</summary>
        IEnumerator Send(GamepadState state){InputSystem.QueueStateEvent(pad,state);yield return null;yield return null;}
        IEnumerator Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));yield return null;yield return null;}
        IEnumerator Tap(GamepadButton button){yield return Send(new GamepadState().WithButton(button));yield return Send(new GamepadState());yield return null;}
        /// <summary>Directions in the camera's frame, and what a Precision also says for them: its stick and its d-pad are one control.</summary>
        GamepadState Moving(Vector2 stick,bool boost=false,bool brake=false)
        {
            var state=new GamepadState{leftStick=stick,rightTrigger=boost?1:0,leftTrigger=brake?1:0};
            if(!digital)return state;
            if(stick.y>.5f)state=state.WithButton(GamepadButton.DpadUp);
            if(stick.y<-.5f)state=state.WithButton(GamepadButton.DpadDown);
            if(stick.x<-.5f)state=state.WithButton(GamepadButton.DpadLeft);
            if(stick.x>.5f)state=state.WithButton(GamepadButton.DpadRight);
            return state;
        }
        static Key KeyFor(string action)=>action switch
        {
            "Attack"=>Key.J,"Dodge"=>Key.Space,"Guard"=>Key.K,"Swap"=>Key.Tab,"LockOn"=>Key.Q,"QuickItem"=>Key.F,_=>Key.None,
        };
        static GamepadState ButtonFor(string action)=>action switch
        {
            "Attack"=>new GamepadState().WithButton(GamepadButton.South),
            "Dodge"=>new GamepadState().WithButton(GamepadButton.East),
            "Guard"=>new GamepadState().WithButton(GamepadButton.West),
            "Swap"=>new GamepadState().WithButton(GamepadButton.North),
            "LockOn"=>new GamepadState().WithButton(GamepadButton.LeftShoulder),
            "QuickItem"=>new GamepadState{leftTrigger=1},
            _=>new GamepadState(),
        };
        IEnumerator Hold(string action){if(pad==null)yield return Keys(KeyFor(action));else yield return Send(ButtonFor(action));}
        IEnumerator LetGo(){if(pad==null)yield return Keys();else yield return Send(new GamepadState());}

        // ================================================================== 4.1 First minutes
        [UnityTest] public IEnumerator TheTitleShowsItsLogoAndItsFourRows()
        {
            // With nothing saved, the way a first-time player meets it: Continue is there, and greyed.
            ClearSaves();SceneManager.LoadScene("_Boot");yield return null;
            float deadline=Time.unscaledTime+20;
            while(Object.FindFirstObjectByType<TitleScreen>()==null&&Time.unscaledTime<deadline)yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<TitleScreen>(),"the game doesn't open on its title");
            yield return new WaitForSecondsRealtime(.3f);
            var wordmark=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).SingleOrDefault(t=>t.name=="Title"&&t.transform.parent.name=="MainMenu");
            Assert.IsNotNull(wordmark,"the title has no wordmark");
            Assert.AreEqual("CORONACH",wordmark.text,"the logo on the title isn't the game's name");
            var art=Object.FindObjectsByType<RawImage>(FindObjectsSortMode.None).FirstOrDefault(r=>r.name=="KeyArt");
            Assert.IsTrue(art!=null&&art.texture!=null,"the title's key art is missing: the menu sits on a blank screen");
            float above=float.MaxValue;
            foreach(string name in new[]{"New Game","Continue","Settings","Quit"})
            {
                var row=Row(name);Assert.IsNotNull(row,$"the title has no {name} row");
                Assert.AreEqual(name,Words(row),$"the {name} row says something else");
                Assert.IsTrue(row.gameObject.activeInHierarchy,$"the {name} row isn't showing");
                float y=((RectTransform)row.transform).anchoredPosition.y;Assert.Less(y,above,$"{name} is out of order in the column");above=y;
                if(name!="Continue")Assert.IsTrue(row.interactable,$"{name} can't be pressed on a first run");
            }
            if(Application.isBatchMode)Assert.IsFalse(Row("Continue").interactable,"with no save on disk Continue can still be pressed");
            Assert.IsNotNull(EventSystem.current.currentSelectedGameObject,"the title opens with nothing selected: a pad player can't start");
        }
        [UnityTest] public IEnumerator SaveRowsReadInWordsForEveryZoneOnTheTitleAndInThePauseMenu()
        {
            yield return Zone("Arena_Ground");
            var zones=GameCatalog.All<ZoneDef>().Select(z=>z.id).ToArray();Assert.GreaterOrEqual(zones.Length,13,"fixture: every zone is in the catalog");
            var rawId=new Regex("[a-z][A-Z]|_");
            GameState Saved(string zone){var state=new GameState{zone=zone};state.party[0].level=3;return state;}
            Saves.Save("autosave",Saved("Hub_Decks"));
            yield return ToTitle();
            foreach(string zone in zones)
            {
                Saves.Save("autosave",Saved(zone));Saves.Save("slot1",Saved(zone));
                Row("Continue").onClick.Invoke();yield return null;yield return null;
                var row=Row("autosave");Assert.IsNotNull(row,"fixture: the picker lists the autosave");
                string words=Words(row),name=DisplayNames.Zone(zone);
                StringAssert.StartsWith($"Autosave — {name} — Sync 3",words,$"the autosave in {zone} doesn't read as the plan's row");
                Assert.IsFalse(rawId.IsMatch(words),$"the autosave row prints a raw id: {words}");
                StringAssert.StartsWith($"Slot 1 — {name} — Sync 3",Words(Row("slot1")),$"slot 1 in {zone} doesn't read in words");
                Row("Back").onClick.Invoke();yield return null;yield return null;
            }
            yield return Zone("Arena_Ground");
            Saves.Save("slot1",Saved("Hub_Decks"));
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open(PauseMenu.PartyPage);yield return null;
            string pause=Words(With("Save to slot 1"));
            StringAssert.Contains("The Decks, Sync 3",pause,"the pause menu's save row doesn't say what is in the slot");
            Assert.IsFalse(rawId.IsMatch(pause),"the pause menu's save row prints a raw id: "+pause);
            menu.Close();ClearSaves();
        }
        /// <summary>D197: "no NPC would talk to him". The existing checks call Interact() on each speaker, which no player can do: this walks up
        /// to them and presses A, from the only side of the amber gates a player following the objective can be on.</summary>
        [UnityTest] public IEnumerator OrrinMiraAndSelaEachAnswerAPressFromWhereAPlayerCanStand()
        {
            GameServices.Current.NewGame();Flag(OpeningMarketSequence.Requested);
            yield return Use("analog");
            yield return Zone("Hub_Decks","Office");
            var hero=Lead();var failures=new List<string>();
            Npc Speaker(string name)=>All<Npc>().Single(n=>n.speaker==name);
            IEnumerator Talk(string name,string node)
            {
                yield return null;yield return null;
                if(!(PromptService.Interaction is Npc npc)||npc.speaker!=name)
                {failures.Add($"standing at {hero.transform.position} the prompt is \"{PromptService.Interaction?.prompt}\", not {name}");yield break;}
                heard=null;DialogueSystem.Current.AutoAdvance=true;
                yield return Tap(GamepadButton.South);
                if(heard!=node)failures.Add($"A beside {name} played {heard??"nothing"}, not the holding line {node}");
                yield return Finish();DialogueSystem.Current.AutoAdvance=false;
                yield return new WaitForSecondsRealtime(.45f);
            }
            // Orrin, in his office.
            Put(hero,new Vector3(-33,0,3.8f));
            yield return Talk("Orrin","OrrinOpening");
            // Mira, in plain view at her stall across the gate: walk the gate from end to end, pressing into it.
            var mira=Speaker("Mira");float closest=float.MaxValue;bool promptedMira=false;
            foreach(float z in new[]{-5.2f,-4.4f,-1.7f,-.8f})
            {
                Put(hero,new Vector3(-14.8f,0,z));
                yield return Send(new GamepadState{leftStick=Stick(Vector3.right)});
                yield return new WaitForSecondsRealtime(.8f);
                yield return Send(new GamepadState());yield return null;
                closest=Mathf.Min(closest,Flat(hero.transform.position,mira.transform.position));
                if(PromptService.Interaction is Npc near&&near.speaker=="Mira"){promptedMira=true;break;}
            }
            Assert.Less(hero.transform.position.x,-12.5f,"fixture: the gate holds the hero on the office side");
            Assert.Greater(hero.transform.position.x,-13.6f,"fixture: the hero walked up to the gate");
            if(promptedMira)yield return Talk("Mira","MiraOpening");
            else failures.Add($"Mira stands in plain view at her stall, and the nearest a player can get is {closest:0.00} m against her {mira.range} m prompt: A does nothing and she never answers");
            // Sela, once she has crossed.
            var walk=Speaker("Sela").GetComponent<ScriptedWalk>();
            for(float end=Time.unscaledTime+15;!walk.Arrived&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(walk.Arrived,"fixture: Sela crosses the market");
            Put(hero,new Vector3(-13.3f,0,-4.8f));
            yield return Talk("Sela","SelaOpening");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator ANewGameWalksToTheWalkwayJobWithNoWallInTheWay()
        {
            ClearSaves();yield return Use("analog");
            for(float end=Time.unscaledTime+15;Row("New Game")==null&&Time.unscaledTime<end;)yield return null;
            Row("New Game").onClick.Invoke();yield return null;
            if(Row("ConfirmNewGame")!=null)Row("ConfirmNewGame").onClick.Invoke();
            for(float end=Time.unscaledTime+25;(SceneFlow.Current.Loading||State.zone!="Hub_CinderHalo"||PartyController.Current==null)&&Time.unscaledTime<end;)yield return null;
            Assert.AreEqual("Hub_CinderHalo",State.zone,"fixture: a new game starts on the Halo");
            Assert.AreEqual(1,PartyController.Current.members.Length,"fixture: a new game starts solo");
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsFalse(DialogueSystem.Current.Running,"a new game opens on a wall of text before the player has moved");
            Assert.IsFalse(GameInput.Current.Blocked,"a new game holds the controls");
            StringAssert.Contains("Orrin",Objective(),"nothing says where a new game goes first");
            var dock=All<DockingPad>().First(d=>d.spawn=="Office");var ship=PartyController.Current.Active;
            Put(ship,new Vector3(dock.transform.position.x,1,dock.transform.position.z-5));yield return null;yield return null;
            Assert.AreSame(dock,PromptService.Interaction,"fixture: the ship is at the office dock");
            yield return Tap(GamepadButton.South);
            for(float end=Time.unscaledTime+15;(SceneFlow.Current.Loading||State.zone!="Hub_Decks")&&Time.unscaledTime<end;)yield return null;
            Assert.AreEqual("Hub_Decks",State.zone,"A at the office dock doesn't dock");
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsFalse(DialogueSystem.Current.Running,"docking opens on a wall of text");
            Assert.IsFalse(GameInput.Current.Blocked,"the controls stay held after docking");
            StringAssert.Contains("walkway",Objective(),"nothing on the Decks says what the job is");
            var hero=PartyController.Current.Active;var repair=All<DiscoveryPoint>().Single(d=>d.flag==OpeningMarketSequence.Repaired);
            // On foot, along the replay's own proven line, with the stick.
            foreach(var goal in new[]{new Vector3(-28,0,-3),new Vector3(-16.2f,0,-3)})
                for(float end=Time.unscaledTime+10;Flat(hero.transform.position,goal)>.5f&&Time.unscaledTime<end;)
                {
                    InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=Stick(goal-hero.transform.position)});yield return null;
                    Assert.IsFalse(DialogueSystem.Current.Running,"a conversation stops the walk to the first job");
                }
            yield return Send(new GamepadState());
            Assert.Less(Flat(hero.transform.position,repair.transform.position),repair.range,"the walk from the dock doesn't reach the job");
            for(float end=Time.unscaledTime+3;PromptService.Interaction!=repair&&Time.unscaledTime<end;)yield return null;
            Assert.AreSame(repair,PromptService.Interaction,"the player stands at the job and nothing offers it");
            yield return null;yield return null;
            StringAssert.Contains("Finish the walkway repair",Text("Interaction").text,"the HUD doesn't show the job's prompt");
            DialogueSystem.Current.AutoAdvance=true;yield return Tap(GamepadButton.South);
            Assert.IsTrue(DialogueSystem.Current.Running,"A at the job plays nothing");
            yield return Finish();
            Assert.IsTrue(Has(OpeningMarketSequence.Repaired),"the job isn't done when its conversation ends");
        }
        [UnityTest] public IEnumerator TheFirstFightNamesAttackDodgeAndSwapAndTeachesByPlaying()
        {
            yield return Zone("Sorrel_Ridges");
            var party=PartyController.Current;var hero=party.Active;
            Put(hero,new Vector3(0,0,32));Park(hero,new Vector3(3,0,32));
            foreach(var member in party.members)member.Health.InvulnerableUntil=GameTime.Now+60;
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsFalse(ZoneController.Current.InSafePocket,"fixture: open ground");
            var barks=new List<string>();void Barked(string who,string line)=>barks.Add(line);BarkService.Spoken+=Barked;
            try
            {
                int lines=DialogueSystem.Current.LinesPresented;
                for(int i=0;i<3;i++){var hound=Enemy("Ridgehound",hero.transform.position+Quaternion.Euler(0,i*120,0)*Vector3.forward*4,false);hound.Health.Damageable=false;}
                for(float end=Time.unscaledTime+6;!Has("taught.flash")&&Time.unscaledTime<end;)yield return null;
                Assert.IsTrue(Has("taught.flash"),"the first fight's marked attacks start and nothing is said about dodging them");
                yield return new WaitForSecondsRealtime(1);
                Assert.IsFalse(DialogueSystem.Current.Running,"a conversation stops the first fight to explain it");
                Assert.AreEqual(lines,DialogueSystem.Current.LinesPresented,"the first fight is taught in dialogue boxes");
                foreach(var device in new[]{PromptDevice.Keyboard,PromptDevice.Gamepad})
                {
                    PromptService.ForceDevice(device);yield return null;yield return null;
                    string strip=Text("Skills").text,partner=Text("Partner").text;
                    StringAssert.Contains(PromptService.Tag("Attack")+"  ATTACK",strip,$"on {device} the first fight's screen doesn't say which button attacks");
                    StringAssert.Contains(PromptService.Tag("Dodge")+"  DODGE",strip,$"on {device} the first fight's screen doesn't say which button dodges");
                    StringAssert.Contains(PromptService.Tag("Swap"),partner,$"on {device} nothing beside the partner's name gives the swap button");
                    bool told=(strip+partner).Contains("SWAP")||barks.Any(l=>Regex.IsMatch(l,"swap|switch|tag in|tag me",RegexOptions.IgnoreCase));
                    Assert.IsTrue(told,$"on {device} nothing in the first fight says swapping exists: the strip has no SWAP, the only hint is an unlabelled button beside the partner's name (\"{partner}\"), and no one mentions it. Heard: {string.Join(" | ",barks)}");
                }
            }
            finally{BarkService.Spoken-=Barked;}
        }

        // ================================================================== 4.2 Controls: on foot
        [UnityTest] public IEnumerator APrecisionJogsAndSprintsAtTheDecidedPacesInTownAndInAFight()
        {
            yield return AddPrecision();
            IEnumerator Run(float jog,float sprint,string where)
            {
                var hero=Lead();Floor(Open,280);Park(hero,Open+new Vector3(0,.1f,80));
                Put(hero,Open+new Vector3(-110,.1f,0));yield return new WaitForSecondsRealtime(.2f);
                float pace=0;
                InputSystem.QueueStateEvent(joystick,new JoyState{stick=Vector2.right});
                yield return Pace(hero,.6f,1,v=>pace=v);
                Assert.AreEqual(jog,pace,.35f,$"{where} a Precision held over runs at {pace:0.00} m/s, not D197's {jog}");
                InputSystem.QueueStateEvent(joystick,new JoyState{stick=Vector2.right,buttons=HidButton(8)});
                yield return Pace(hero,.6f,1,v=>pace=v);
                Assert.AreEqual(sprint,pace,.5f,$"{where} a Precision with RT down sprints at {pace:0.00} m/s, not {sprint}");
                InputSystem.QueueStateEvent(joystick,new JoyState());yield return new WaitForSecondsRealtime(.3f);
                Assert.Less(Planar(hero),.1f,$"{where} the hero keeps running after the pad is let go");
            }
            yield return Zone("Hub_Decks","Office");yield return Run(CivilJog,CivilSprint,"in town");
            yield return Zone("Arena_Ground");yield return Run(CombatRun,CombatSprint,"in a fight");
        }
        [UnityTest] public IEnumerator AnAnalogPadWalksUnderHalfTiltJogsPastItAndSprints()
        {
            yield return Use("analog");
            IEnumerator Run(float raw,bool sprint,System.Action<float> said)
            {
                var runner=PartyController.Current.Active;
                Put(runner,Open+new Vector3(-110,.1f,0));yield return new WaitForSecondsRealtime(.1f);
                yield return Send(new GamepadState{leftStick=Stick(Vector3.right)*raw,rightTrigger=sprint?1:0});
                yield return Pace(runner,.5f,.8f,said);
                yield return Send(new GamepadState());
            }
            yield return Zone("Hub_Decks","Office");
            var hero=Lead();Floor(Open,280);Park(hero,Open+new Vector3(0,.1f,80));
            float pace=0;
            yield return Run(.45f,false,v=>pace=v);
            Assert.Greater(pace,.4f,"under half tilt the hero doesn't move");
            Assert.LessOrEqual(pace,CivilWalk+.2f,$"under half tilt the hero runs at {pace:0.00} m/s: D197 walks at {CivilWalk}");
            yield return Run(.75f,false,v=>pace=v);
            Assert.AreEqual(CivilJog,pace,.35f,$"past half tilt the hero moves at {pace:0.00} m/s: D197 jogs at {CivilJog}");
            yield return Run(1,false,v=>pace=v);
            Assert.AreEqual(CivilJog,pace,.35f,$"at full tilt the hero moves at {pace:0.00} m/s: D197 jogs at {CivilJog}");
            yield return Run(1,true,v=>pace=v);
            Assert.AreEqual(CivilSprint,pace,.35f,$"sprinting in town the hero moves at {pace:0.00} m/s: D197 sprints at {CivilSprint}");
            yield return Zone("Arena_Ground");
            hero=Lead();Floor(Open,280);Park(hero,Open+new Vector3(0,.1f,80));
            yield return Send(new GamepadState{leftStick=new Vector2(.55f,0)});yield return null;
            float tilt=pad.leftStick.ReadValue().magnitude;yield return Send(new GamepadState());
            yield return Run(.55f,false,v=>pace=v);
            Assert.AreEqual(CombatRun*tilt,pace,.6f,"in a fight an analog stick stopped being proportional");
            yield return Run(1,false,v=>pace=v);
            Assert.AreEqual(CombatRun,pace,.4f,"at full tilt in a fight the hero isn't at combat pace");
            yield return Run(1,true,v=>pace=v);
            Assert.AreEqual(CombatSprint,pace,.6f,"sprinting in a fight the hero isn't at sprint pace");
        }
        [UnityTest] public IEnumerator ATurnAtSpeedKeepsThePaceThroughTheRealBrain([Values(false,true)] bool precision)
        {
            yield return Use(precision?"precision":"analog");
            IEnumerator Turn(string where)
            {
                var hero=Lead();Floor(Open,280);Park(hero,Open+new Vector3(0,.1f,80));
                Put(hero,Open+new Vector3(-60,.1f,0));yield return new WaitForSecondsRealtime(.1f);
                yield return Send(Moving(Vector2.right,true));
                yield return new WaitForSecondsRealtime(.8f);
                float top=Planar(hero);Assert.Greater(top,5,$"fixture: {where} the sprint got going");
                InputSystem.QueueStateEvent(pad,Moving(Vector2.up,true));
                float slowest=float.MaxValue;
                for(float end=Time.unscaledTime+.6f;Time.unscaledTime<end;){yield return null;slowest=Mathf.Min(slowest,Planar(hero));}
                var heading=hero.motor.Velocity;heading.y=0;
                Assert.GreaterOrEqual(slowest,top*.85f,$"{where} a sprint turning a corner falls from {top:0.0} to {slowest:0.0} m/s with the button still held");
                Assert.Less(Vector3.Angle(heading,Heading(Vector2.up)),12,$"{where} the turn hasn't settled on its new heading after .6 s");
                yield return Send(new GamepadState());
            }
            yield return Zone("Hub_Decks","Office");yield return Turn("in town");
            yield return Zone("Arena_Ground");yield return Turn("in a fight");
        }
        [UnityTest] public IEnumerator ACombatSprintRunsDownFromFullAndRestsOnAPrecision()
        {
            yield return AddPrecision();
            yield return Zone("Arena_Ground");
            var hero=Lead();var brain=hero.GetComponent<PlayerBrain>();
            Floor(Open,320);Park(hero,Open+new Vector3(0,.1f,110));Put(hero,Open+new Vector3(-150,.1f,0));
            yield return new WaitForSecondsRealtime(.2f);hero.thrust=100;
            yield return Send(Moving(Vector2.right,true));
            float started=Time.unscaledTime,fastest=0;
            while(!brain.Winded&&Time.unscaledTime-started<10){fastest=Mathf.Max(fastest,Planar(hero));yield return null;}
            Assert.IsTrue(brain.Winded,"ten seconds of sprint and the Thrust bar never ran out");
            float lasted=Time.unscaledTime-started;
            Assert.Greater(fastest,CombatRun*1.3f,"the sprint never reached sprint pace");
            Assert.Greater(lasted,3,$"a full Thrust bar lasts {lasted:0.0} s of sprint");
            yield return new WaitForSecondsRealtime(.2f);
            float rest=0;
            for(float end=Time.unscaledTime+.8f;Time.unscaledTime<end&&hero.thrust<28;){rest=Mathf.Max(rest,Planar(hero));yield return null;}
            Assert.LessOrEqual(rest,CombatRun*1.08f,$"a spent sprint still runs at {rest:0.0} m/s: it flickers instead of resting");
            Assert.Greater(rest,CombatRun*.8f,$"a spent sprint drops to {rest:0.0} m/s instead of running on at combat pace");
            yield return Send(new GamepadState());
        }

        /// <summary>No route and no check drives a body from the keyboard: the replay driver is a pad, and every key press in the suite is in a menu.</summary>
        [UnityTest] public IEnumerator AKeyboardWalksSprintsAndFliesOnItsOwnKeys()
        {
            yield return Use("keyboard");
            IEnumerator Run(float jog,float sprint,string where)
            {
                var hero=Lead();Floor(Open,280);Park(hero,Open+new Vector3(0,.1f,80));
                Put(hero,Open+new Vector3(-110,.1f,0));yield return new WaitForSecondsRealtime(.2f);
                float pace=0;
                yield return Keys(Key.D);yield return Pace(hero,.5f,1,v=>pace=v);
                Assert.AreEqual(jog,pace,.35f,$"{where} D runs at {pace:0.00} m/s, not D197's {jog}");
                yield return Keys(Key.D,Key.LeftShift);yield return Pace(hero,.5f,1,v=>pace=v);
                Assert.AreEqual(sprint,pace,.5f,$"{where} D and Shift sprint at {pace:0.00} m/s, not {sprint}");
                yield return Keys();
            }
            yield return Zone("Hub_Decks","Office");yield return Run(CivilJog,CivilSprint,"in town");
            yield return Zone("Arena_Ground");yield return Run(CombatRun,CombatSprint,"in a fight");
            yield return Zone("Arena_Flight");
            var pilot=Lead();var ship=pilot.GetComponent<FlightMotor>();Park(pilot,new Vector3(260,1,-200));Put(pilot,new Vector3(200,1,-200));yield return Frames(3);
            yield return Keys(Key.W);yield return new WaitForSecondsRealtime(1.1f);
            Assert.AreEqual(12,Planar(pilot),1.5f,"W doesn't thrust a ship up to cruise");
            yield return Keys();float released=Planar(pilot);yield return new WaitForSecondsRealtime(.4f);
            Assert.Greater(Planar(pilot),released*.5f,"with the keys let go the ship stops instead of coasting");
            yield return Keys(Key.F);yield return new WaitForSecondsRealtime(.6f);
            Assert.Less(Planar(pilot),1,"F doesn't brake in flight");
            yield return Keys(Key.W,Key.LeftShift);yield return new WaitForSecondsRealtime(1.2f);
            Assert.Greater(Planar(pilot),18,"Shift doesn't boost in flight");
            yield return Keys(Key.F);yield return new WaitForSecondsRealtime(.7f);yield return Keys();
            int dashes=ship.DashSequence;
            yield return Keys(Key.Space);Assert.AreEqual(dashes+1,ship.DashSequence,"Space doesn't roll in flight");yield return Keys();
            yield return UntilFree(pilot);yield return new WaitForSecondsRealtime(.3f);
            yield return Keys(Key.K);Assert.AreEqual("Dash",pilot.VisualAction,"K doesn't lunge in flight");yield return Keys();
            yield return UntilFree(pilot);yield return new WaitForSecondsRealtime(.8f);
            int shots=pilot.AttackSequence;
            yield return Keys(Key.J);Assert.Greater(pilot.AttackSequence,shots,"J doesn't fire in flight");yield return Keys();
        }

        // ------------------------------------------------------------------ the first press
        [UnityTest] public IEnumerator AttackDodgeGuardLockQuickItemAndSwapAnswerTheFirstPress([Values("analog","precision","keyboard")] string device)
        {
            yield return Use(device);
            yield return Zone("Arena_Ground");
            var party=PartyController.Current;var hero=Lead();
            Floor(Open,160);Put(hero,Open+Vector3.up*.1f);Park(hero,Open+new Vector3(-12,.1f,0));
            var hound=Enemy("Ridgehound",hero.transform.position+new Vector3(5,0,5));hound.Health.maximum=hound.Health.integrity=1000000;hound.Health.breakThreshold=1e9f;
            yield return Frames(3);
            Assert.IsFalse(PromptService.Interaction!=null,"fixture: no prompt shares the A button here");
            string pressing=$"on a {device}";
            int swings=hero.AttackSequence;
            yield return Hold("Attack");Assert.Greater(hero.AttackSequence,swings,$"{pressing} the first press of attack is eaten");
            yield return LetGo();yield return UntilFree(hero);
            yield return Hold("Dodge");Assert.AreEqual(ActorState.Dodge,hero.State,$"{pressing} the first press of dodge is eaten");
            yield return LetGo();yield return UntilFree(hero);
            yield return Hold("Guard");Assert.AreEqual(ActorState.Guard,hero.State,$"{pressing} the first press of guard is eaten");
            yield return LetGo();Assert.AreNotEqual(ActorState.Guard,hero.State,$"{pressing} the hero stays in guard after it's let go");
            Assert.IsFalse(hero.TargetLocked,"fixture: nothing is locked yet");
            yield return Hold("LockOn");
            if(device=="precision")Assert.IsFalse(hero.TargetLocked,"on a Precision LB is also the cycle modifier: the press alone mustn't lock");
            else Assert.IsTrue(hero.TargetLocked,$"{pressing} the first press of lock-on is eaten");
            yield return LetGo();Assert.IsTrue(hero.TargetLocked,$"{pressing} lock-on doesn't hold after the press");
            yield return Hold("LockOn");yield return LetGo();Assert.IsFalse(hero.TargetLocked,$"{pressing} a second press doesn't let the lock go");
            State.quickItem="RepairGel";State.consumables["RepairGel"]=3;hero.Health.integrity=hero.Health.maximum*.5f;
            yield return Hold("QuickItem");Assert.AreEqual(2,State.consumables["RepairGel"],$"{pressing} the first press of the quick item is eaten");
            yield return LetGo();
            int swaps=party.SwapCount;var was=party.Active;
            yield return Hold("Swap");Assert.Greater(party.SwapCount,swaps,$"{pressing} the first press of swap is eaten");Assert.AreNotSame(was,party.Active,$"{pressing} swap doesn't change who leads");
            yield return LetGo();
            foreach(var member in party.members)member.GetComponent<PartnerBrain>().enabled=false;
        }
        [UnityTest] public IEnumerator EachSkillButtonFiresItsOwnSlotAndNothingElse([Values("analog","precision","keyboard")] string device,[Values(false,true)] bool flight)
        {
            yield return Use(device);
            yield return Zone(flight?"Arena_Flight":"Arena_Ground");
            var party=PartyController.Current;var hero=Lead();
            var home=flight?new Vector3(200,1,-200):Open+Vector3.up*.1f;
            if(!flight)Floor(Open,160);
            Park(hero,home+new Vector3(-30,0,0));
            var faces=new[]{GamepadButton.South,GamepadButton.East,GamepadButton.West,GamepadButton.North};
            var digits=new[]{Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4};
            string pressing=$"on a {device} {(flight?"in flight":"on foot")}";
            for(int slot=0;slot<4;slot++)
            {
                yield return UntilFree(hero);Put(hero,home);yield return Frames(2);
                hero.charge=100;for(int i=0;i<4;i++)hero.cooldowns[i]=0;int swaps=party.SwapCount;
                if(pad==null)yield return Keys(digits[slot]);
                else
                {
                    yield return Send(new GamepadState().WithButton(GamepadButton.RightShoulder));
                    yield return Send(new GamepadState().WithButton(GamepadButton.RightShoulder).WithButton(faces[slot]));
                }
                for(int i=0;i<4;i++)
                    if(i==slot)Assert.Greater(hero.cooldowns[i],0,$"{pressing} skill button {slot+1} fires nothing");
                    else Assert.AreEqual(0,hero.cooldowns[i],.001f,$"{pressing} skill button {slot+1} fires slot {i+1}");
                Assert.AreEqual(swaps,party.SwapCount,$"{pressing} skill button {slot+1} also swapped hero");
                Assert.AreNotEqual(ActorState.Dodge,hero.State,$"{pressing} skill button {slot+1} also rolled");
                Assert.AreNotEqual(ActorState.Guard,hero.State,$"{pressing} skill button {slot+1} also guarded");
                if(pad==null)yield return Keys();else yield return Send(new GamepadState());
            }
        }
        /// <summary>The plan's exception to "A is an attack": a story prompt and a relay stay open, with the real ones in the real zone.</summary>
        [UnityTest] public IEnumerator ARelayAndAStoryPromptStillAnswerAWithAnEnemyOnTheHero()
        {
            State.questSteps["LanternRoad"]=3;
            yield return Use("analog");
            yield return Zone("LanternGorge");
            var hero=Lead();Park(hero,new Vector3(0,.2f,20));
            hero.Health.InvulnerableUntil=GameTime.Now+60;
            DiscoveryPoint Relay(string flag)=>All<DiscoveryPoint>().Single(d=>d.flag==flag);
            // The nearest walkable floor inside the prompt's reach, whatever stands on the spot itself.
            Vector3 Beside(DiscoveryPoint point)
            {
                Assert.IsTrue(NavMesh.SamplePosition(point.transform.position,out var floor,point.range-.2f,NavMesh.AllAreas),"fixture: "+point.prompt+" has floor within reach");
                return floor.position+Vector3.up*.1f;
            }
            var plain=Relay("gorge.relay2");Put(hero,Beside(plain));
            var hound=Enemy("Ridgehound",hero.transform.position+Vector3.right*3,false);hound.Health.InvulnerableUntil=GameTime.Now+60;
            yield return Frames(3);
            Assert.AreSame(plain,PromptService.Interaction,"with a hound on the hero a relay stops answering: relighting it mid-fight is what its pulse is for");
            int swings=hero.AttackSequence;
            yield return Tap(GamepadButton.South);
            Assert.IsTrue(Has("gorge.relay2"),"A beside a relay with a hound on the hero doesn't relight it");
            Assert.AreEqual(swings,hero.AttackSequence,"the press that relit the relay also swung");
            yield return Frames(3);
            yield return Tap(GamepadButton.South);
            Assert.Greater(hero.AttackSequence,swings,"with the relay lit, A with a hound on the hero still doesn't swing");
            yield return UntilFree(hero);
            var told=Relay("gorge.relay1");Put(hero,Beside(told));
            var body=hound.GetComponent<CharacterController>();body.enabled=false;hound.transform.position=hero.transform.position+Vector3.right*3;body.enabled=true;Physics.SyncTransforms();
            yield return Frames(3);
            Assert.AreSame(told,PromptService.Interaction,"with a hound on the hero a story prompt stops answering");
            DialogueSystem.Current.AutoAdvance=true;
            yield return Tap(GamepadButton.South);
            Assert.IsTrue(DialogueSystem.Current.Running,"A at a relay with something to say plays nothing mid-fight");
            yield return Finish();
            Assert.IsTrue(Has("gorge.relay1"),"the story prompt's flag isn't set when its conversation ends");
        }

        // ------------------------------------------------------------------ rolls and guards
        IEnumerator Fight(string device="analog")
        {
            yield return Use(device);
            yield return Zone("Arena_Ground");
            var hero=Lead();Floor(Open,160);Put(hero,Open+Vector3.up*.1f);Park(hero,Open+new Vector3(-30,.1f,0));
            yield return Frames(3);
        }
        /// <summary>Rolls with B and says when, on the same clock as the roll's own.</summary>
        IEnumerator Roll(CombatActor hero,System.Action<float> at)
        {
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East));
            for(int i=0;i<4&&hero.State!=ActorState.Dodge;i++)yield return null;
            Assert.AreEqual(ActorState.Dodge,hero.State,"fixture: B rolled");
            at(GameTime.Now);
            InputSystem.QueueStateEvent(pad,new GamepadState());
        }
        [UnityTest] public IEnumerator ARollPressedWhileGuardingStillEvades()
        {
            yield return Fight();var hero=PartyController.Current.Active;
            yield return Send(new GamepadState().WithButton(GamepadButton.West));
            yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(ActorState.Guard,hero.State,"fixture: X holds the guard");
            yield return Send(new GamepadState().WithButton(GamepadButton.West).WithButton(GamepadButton.East));
            Assert.AreEqual("Dodge",hero.VisualAction,"B with the guard held does nothing");
            float before=hero.Health.integrity;
            hero.Health.Receive(Incoming());
            Assert.AreEqual(before,hero.Health.integrity,.01f,$"the hero visibly rolls with the guard held, and the hit lands anyway for {before-hero.Health.integrity:0.0}: with X down the roll has no i-frames");
            yield return Send(new GamepadState());
        }
        [UnityTest] public IEnumerator AFlashMoveLandsOnlyInItsWindowSlowsTheWorldAndEnds()
        {
            yield return Fight();var hero=PartyController.Current.Active;float rolled=0;
            // Early in the roll: a Flash, from a real press.
            yield return Roll(hero,v=>rolled=v);
            float integrity=hero.Health.integrity;hero.Health.Receive(Incoming());
            Assert.AreEqual(1,hero.FlashMoves,"a hit at the very start of a roll earned no Flash Move");
            Assert.AreEqual(integrity,hero.Health.integrity,.01f,"the hit that earned a Flash Move still hurt");
            Assert.Less(Time.timeScale,.5f,"a Flash Move doesn't slow the world");
            yield return new WaitForSecondsRealtime(.3f);
            // The dodger keeps full speed through the slow, and can't be touched.
            float pace=0;
            yield return Send(new GamepadState{leftStick=Stick(Vector3.right)});
            yield return Pace(hero,.1f,.45f,v=>pace=v);
            Assert.Greater(pace,CombatRun*.8f,$"in their own Flash the hero crawls at {pace:0.0} m/s with the world");
            yield return Send(new GamepadState());
            Assert.Less(Time.timeScale,.5f,"the Flash is over before its 1.5 s");
            hero.Health.Receive(Incoming());
            Assert.AreEqual(integrity,hero.Health.integrity,.01f,"the hero can be hit during their own Flash");
            yield return UntilTime(rolled+1.8f);
            Assert.AreEqual(1,Time.timeScale,.001f,"the world stays slow after the Flash's 1.5 s");
            // Late in the roll: past the window, inside the i-frames. It evades and pays nothing.
            bool sampled=false;
            for(int attempt=0;attempt<4&&!sampled;attempt++)
            {
                yield return new WaitForSecondsRealtime(.6f);
                yield return Roll(hero,v=>rolled=v);
                while(GameTime.Now-rolled<.215f)yield return null;
                if(GameTime.Now-rolled>=.245f)continue;
                sampled=true;integrity=hero.Health.integrity;
                hero.Health.Receive(Incoming());
                Assert.AreEqual(1,hero.FlashMoves,$"a hit {GameTime.Now-rolled:0.000} s into a roll, past the .2 s window, still earned a Flash Move");
                Assert.AreEqual(integrity,hero.Health.integrity,.01f,"a hit late in a roll goes through its i-frames");
                Assert.AreEqual(1,Time.timeScale,.001f,"a roll that earned no Flash still slowed the world");
            }
            Assert.IsTrue(sampled,"fixture: a frame landed late in the roll");
        }
        [UnityTest] public IEnumerator AMashedRollCostsThirtyThrustAndARefusalBuzzesAndRestartsTheClock()
        {
            yield return Fight();var hero=PartyController.Current.Active;float rolled=0;
            var bar=Object.FindFirstObjectByType<GameHud>().GetComponentsInChildren<Image>(true).First(i=>i.name=="Thrust");
            using var log=new CueLog();
            hero.thrust=100;
            yield return Roll(hero,v=>rolled=v);
            yield return UntilFree(hero);
            Assert.AreEqual(100,hero.thrust,1,"a roll in its own time cost Thrust");
            // The second roll, inside .45 s of the first.
            yield return UntilTime(rolled+.36f);
            float before=hero.thrust;float first=rolled;
            yield return Roll(hero,v=>rolled=v);
            Assert.Less(rolled-first,.45f,"fixture: the second roll is inside the rhythm");
            Assert.AreEqual(before-30,hero.thrust,1.5f,"a mashed roll doesn't cost the plan's 30 Thrust");
            hero.Health.Receive(Incoming());
            Assert.AreEqual(0,hero.FlashMoves,"a mashed roll earned a Flash");
            yield return UntilFree(hero);
            // Short of Thrust: nothing goes, the bar and the speaker say why.
            log.Clear();
            yield return UntilTime(rolled+.37f);
            hero.thrust=0;float pressed=GameTime.Now;
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            Assert.AreNotEqual(ActorState.Dodge,hero.State,"a mashed roll with an empty bar still goes");
            Assert.Less(bar.color.g,.5f,"a roll refused for Thrust doesn't flash the bar");
            Assert.Greater(log.Count("ui_error"),0,"a roll refused for Thrust makes no error sound: "+log.All);
            yield return Send(new GamepadState());
            // The refused press restarted the .45 s: still short, still refused, though the last roll is long past.
            yield return UntilTime(pressed+.4f);
            hero.thrust=0;pressed=GameTime.Now;
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            Assert.AreNotEqual(ActorState.Dodge,hero.State,"a refused press doesn't restart the .45 s: an empty bar spaces a mash out into free rolls");
            yield return Send(new GamepadState());
            // Its own time again, and free.
            yield return UntilTime(pressed+.5f);
            hero.thrust=0;
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            Assert.AreEqual(ActorState.Dodge,hero.State,"a roll in its own time is refused on an empty bar");
            yield return Send(new GamepadState());
        }
        [UnityTest] public IEnumerator AFlashGuardPaysChargeAndTheNextHitCritsAndALateOneOnlyBlocks()
        {
            yield return Fight();var hero=PartyController.Current.Active;
            // Inside the first cut's reach. Its drawn edge (Builds/quality/C3/attack-wrist-poses-01) passes a hound's body up to about 1.55 m dead ahead and misses one at 1.6 by 2 cm;
            // a whiff here says nothing about the Flash Guard.
            var dummy=Enemy("Ridgehound",hero.transform.position+Vector3.forward*1.2f);dummy.Health.maximum=dummy.Health.integrity=1000000;dummy.Health.breakThreshold=1e9f;
            hero.target=dummy.Health;
            var hits=new List<bool>();void Hit(Health _,DamagePacket packet,float __)=>hits.Add(packet.isCrit);dummy.Health.Damaged+=Hit;
            hero.GetComponent<GroundMotor>().KeepFacing(Vector3.forward);hero.transform.rotation=Quaternion.identity;
            yield return Frames(2);
            // In the window: nothing lands, Charge comes in, and the next hit crits.
            hero.charge=10;float integrity=hero.Health.integrity;
            yield return Send(new GamepadState().WithButton(GamepadButton.West));
            hero.Health.Receive(Incoming());
            Assert.AreEqual(1,hero.FlashGuards,"a hit straight after X isn't a Flash Guard");
            Assert.AreEqual(integrity,hero.Health.integrity,.01f,"a Flash Guard still took damage");
            Assert.AreEqual(35,hero.charge,1,"a Flash Guard doesn't pay its 25 Charge");
            yield return Send(new GamepadState());
            yield return Send(new GamepadState().WithButton(GamepadButton.South));yield return Send(new GamepadState());
            for(float end=Time.unscaledTime+1.2f;hits.Count==0&&Time.unscaledTime<end;)yield return null;
            Assert.IsNotEmpty(hits,"fixture: the swing after the guard landed");
            Assert.IsTrue(hits[0],"the swing after a Flash Guard isn't a crit");
            dummy.Health.Damaged-=Hit;
            yield return UntilFree(hero);
            // Past the window: the guard still blocks, and pays nothing.
            yield return new WaitForSecondsRealtime(.6f);
            hero.charge=10;integrity=hero.Health.integrity;
            yield return Send(new GamepadState().WithButton(GamepadButton.West));
            yield return new WaitForSecondsRealtime(.35f);
            hero.Health.Receive(Incoming());
            float taken=integrity-hero.Health.integrity;
            Assert.AreEqual(1,hero.FlashGuards,"a guard held .35 s still counts as a Flash Guard");
            Assert.Greater(taken,0,"a late guard stopped the whole hit");
            Assert.Less(taken,10,"a late guard stopped blocking");
            Assert.Less(hero.charge,12,"a late guard paid Charge");
            yield return Send(new GamepadState());
        }

        // ------------------------------------------------------------------ the camera
        /// <summary>The walkable corners where the fixed camera's line to a hero's chest meets visible scenery, tightest first: if the camera
        /// hides anyone anywhere, it is here. Wherever scenery is still between the lens and the chest after the camera has settled, the hero is
        /// out of sight unless that scenery has faded for them.</summary>
        static string Hider(CombatActor hero)
        {
            var eye=Camera.main.transform.position;var chest=hero.transform.position+Vector3.up*1.2f;
            float distance=Vector3.Distance(eye,chest);
            foreach(var hit in Physics.RaycastAll(eye,(chest-eye).normalized,Mathf.Max(0,distance-.3f),~0,QueryTriggerInteraction.Ignore))
            {
                if(!CameraRig.Scenery(hit.collider))continue;
                if(hit.collider.GetComponentInParent<SceneryOcclusion>()!=null)
                {
                    var block=new MaterialPropertyBlock();hit.collider.GetComponent<Renderer>().GetPropertyBlock(block);
                    if(block.GetFloat("_OccFade")>.5f)continue;
                }
                return hit.collider.name;
            }
            return null;
        }
        [UnityTest] public IEnumerator TheCameraNeverHidesTheHeroAtTheFootOfAWall([Values("Sorrel_Ridges","LanternGorge","SunkenRelay","Lanternwick")] string zone)
        {
            foreach(string flag in new[]{"met.Survivor","bossdown.Burrower","clear.Sorrel_Burrower","relay.hessaConfronted"})Flag(flag);
            yield return Zone(zone);
            foreach(var seal in All<Membrane>())seal.SetOpen(true);
            yield return new WaitForSecondsRealtime(.5f);
            var hero=PartyController.Current.Active;
            var view=ZoneController.Current.definition.cameraProfile;var back=-(Quaternion.Euler(view.pitch,view.yaw,0)*Vector3.forward);
            Assert.IsTrue(NavMesh.SamplePosition(hero.transform.position,out var from,6,NavMesh.AllAreas),"fixture: the arrival is on the navigation");
            var corners=new List<(Vector3 at,float gap)>();
            foreach(var vertex in NavMesh.CalculateTriangulation().vertices)
            {
                // Every hit, not the first: Lanternwick's invisible town limit stands in front of its west wall, and a first-hit search counted the limit and threw the wall away.
                float gap=float.MaxValue;
                foreach(var hit in Physics.RaycastAll(vertex+Vector3.up*1.2f,back,view.distance,~0,QueryTriggerInteraction.Ignore))
                    if(CameraRig.Scenery(hit.collider))gap=Mathf.Min(gap,hit.distance);
                if(gap<float.MaxValue)corners.Add((vertex,gap));
            }
            var picks=new List<Vector3>();var path=new NavMeshPath();
            foreach(var corner in corners.OrderBy(c=>c.gap))
            {
                if(picks.Count>=24)break;
                if(picks.Any(p=>(p-corner.at).sqrMagnitude<16))continue;
                if(NavMesh.CalculatePath(from.position,corner.at,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)picks.Add(corner.at);
            }
            // The search must see each zone's walls at all, reachable or not. Lanternwick is exempt from the count: chapter 2's lowered west wall leaves next to no foot of any wall
            // on the camera's side, and few picks there is that fix working, not a broken search. CAMERA_SWEEP below says how many it checked.
            if(zone=="Lanternwick"&&corners.Count==0){TestContext.WriteLine("CAMERA_SWEEP Lanternwick checked=0 hidden=0: no scenery stands on the camera's line anywhere");yield break;}
            Assert.IsNotEmpty(corners,$"fixture: the search finds no scenery on the camera's line anywhere in {zone}");
            if(zone!="Lanternwick")Assert.GreaterOrEqual(picks.Count,5,$"fixture: {zone} has wall feet a hero can reach ({corners.Count} corners)");
            var hidden=new List<string>();
            foreach(var at in picks)
            {
                Put(hero,at+Vector3.up*.05f);
                // Until the camera has settled, as CameraTerrainTests waits and the summary above says. A pick can be 230 m from the last one: at .7 s the lens is still .2 m
                // past its dead zone, which a hero who walked there never sees, and it swung the rig's cast into or off the rock at three of Sorrel's picks.
                yield return new WaitForSecondsRealtime(2);
                string who=Hider(hero);
                if(who!=null)hidden.Add($"({at.x:0.0},{at.y:0.0},{at.z:0.0}) behind {who}");
            }
            TestContext.WriteLine($"CAMERA_SWEEP {zone} checked={picks.Count} hidden={hidden.Count}");
            Assert.IsEmpty(hidden,$"in {zone} the camera has the hero behind the scenery at {hidden.Count} of {picks.Count} wall feet:\n"+string.Join("\n",hidden));
        }

        // ================================================================== 4.2 Controls: flight
        IEnumerator Flight(string device)
        {
            yield return Use(device);
            yield return Zone("Arena_Flight");
            var hero=Lead();Park(hero,new Vector3(260,1,-200));Put(hero,new Vector3(200,1,-200));
            yield return Frames(3);
        }
        [UnityTest] public IEnumerator FlightAnswersThrustCoastBrakeBoostRollLungeAndFire([Values(false,true)] bool precision)
        {
            yield return Flight(precision?"precision":"analog");
            var hero=PartyController.Current.Active;var ship=hero.GetComponent<FlightMotor>();
            yield return Send(Moving(Vector2.up));
            Assert.Greater(Planar(hero),.1f,"thrust doesn't answer in two frames");
            yield return new WaitForSecondsRealtime(1.1f);
            float cruise=Planar(hero);Assert.AreEqual(12,cruise,1.5f,$"held thrust settles at {cruise:0.0} m/s, not the 12 of the flight contract");
            // Inertia: letting go coasts.
            yield return Send(Moving(Vector2.zero));
            float released=Planar(hero);var coasted=hero.transform.position;
            yield return new WaitForSecondsRealtime(.5f);
            Assert.Greater(Planar(hero),released*.5f,"with the stick let go the ship stops instead of coasting");
            Assert.Less(Planar(hero),released,"with the stick let go the ship keeps its speed: no drag");
            Assert.Greater(Flat(coasted,hero.transform.position),2,"with the stick let go the ship doesn't travel");
            // Brake.
            yield return Send(Moving(Vector2.zero,false,true));
            yield return new WaitForSecondsRealtime(.6f);
            Assert.Less(Planar(hero),1,"LT doesn't brake a coasting ship");
            yield return Send(Moving(Vector2.zero));
            // Boost.
            hero.thrust=100;
            yield return Send(Moving(Vector2.up,true));
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.Greater(Planar(hero),18,"RT doesn't boost past cruise");
            Assert.Less(hero.thrust,95,"a boost spends no Thrust");
            yield return Send(Moving(Vector2.zero,false,true));
            yield return new WaitForSecondsRealtime(.7f);
            yield return Send(Moving(Vector2.zero));
            Assert.Less(Planar(hero),1,"fixture: the ship braked to a stop");
            // Roll.
            int dashes=ship.DashSequence;var at=hero.transform.position;
            yield return Tap(GamepadButton.East);
            Assert.AreEqual(dashes+1,ship.DashSequence,"B doesn't roll in flight");
            yield return new WaitForSecondsRealtime(.25f);
            Assert.GreaterOrEqual(Flat(at,hero.transform.position),3,"the roll doesn't carry the ship its 4 m");
            yield return UntilFree(hero);yield return new WaitForSecondsRealtime(.3f);
            // Lunge.
            at=hero.transform.position;
            yield return Tap(GamepadButton.West);
            Assert.AreEqual("Dash",hero.VisualAction,"X doesn't lunge");
            yield return new WaitForSecondsRealtime(.3f);
            Assert.GreaterOrEqual(Flat(at,hero.transform.position),6.5f,"the lunge doesn't carry the ship its 8 m");
            yield return UntilFree(hero);yield return new WaitForSecondsRealtime(.8f);
            // Fire.
            int shots=hero.AttackSequence;
            yield return Send(new GamepadState().WithButton(GamepadButton.South));
            Assert.Greater(hero.AttackSequence,shots,"A doesn't fire on its first press");
            yield return Send(new GamepadState());
        }
        [UnityTest] public IEnumerator LettingGoOfBoostCoastsDownInsteadOfDroppingToCruiseInAFrame()
        {
            yield return Flight("analog");var hero=PartyController.Current.Active;
            yield return Send(Moving(Vector2.up,true));
            yield return new WaitForSecondsRealtime(1.2f);
            float top=Planar(hero);Assert.Greater(top,18,"fixture: the boost got going");
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;yield return null;
            float next=Planar(hero);
            Assert.Greater(next,top*.8f,$"let go of RT at {top:0.0} m/s and the ship is at {next:0.0} two frames later: a hard stop to cruise, where the flight contract says releasing coasts");
        }
        [UnityTest] public IEnumerator ASpentBoostRestsInsteadOfStuttering()
        {
            yield return Flight("analog");var hero=PartyController.Current.Active;var ship=hero.GetComponent<FlightMotor>();
            hero.thrust=3;
            yield return Send(Moving(Vector2.up,true));
            yield return new WaitForSecondsRealtime(.5f);
            int flips=0;bool burning=ship.EngineDrive>1.5f;
            for(float end=Time.unscaledTime+1.5f;Time.unscaledTime<end;)
            {yield return null;bool now=ship.EngineDrive>1.5f;if(now!=burning)flips++;burning=now;}
            yield return Send(new GamepadState());
            Assert.LessOrEqual(flips,4,$"RT held on an empty Thrust bar flips the boost on and off {flips} times in 1.5 s: the speed only wobbles, but the flame stays at boost length on an empty bar and the bar never refills while RT is down (D197 gave the sprint on foot a rest for this)");
        }
        [UnityTest] public IEnumerator AWallAndACornerShoveAHullAndLetItLeave()
        {
            yield return Flight("analog");var hero=PartyController.Current.Active;
            hero.Health.maximum=hero.Health.integrity=100000;
            var home=new Vector3(200,1,-200);var apex=home+Vector3.forward*9;
            GameObject Wall(Vector3 from,Vector3 along,float length)
            {
                var wall=new GameObject("Plan wall",typeof(BoxCollider));wall.transform.SetPositionAndRotation(from+along*length*.5f+Vector3.up,Quaternion.FromToRotation(Vector3.right,along));
                wall.GetComponent<BoxCollider>().size=new Vector3(length,8,.6f);return wall;
            }
            IEnumerator Drive(string what,params GameObject[] walls)
            {
                Physics.SyncTransforms();Put(hero,home);yield return Frames(2);
                float lowest=0;
                yield return Send(Moving(Vector2.up,true));
                for(float end=Time.unscaledTime+1.5f;Time.unscaledTime<end;){lowest=Mathf.Min(lowest,hero.motor.Velocity.z);yield return null;}
                var pressed=hero.transform.position;
                Assert.Less(pressed.z,apex.z,$"a hull boosted into {what} goes through it");
                Assert.Less(lowest,0,$"a hull boosted into {what} never gets shoved back");
                Assert.AreEqual(1,pressed.y,.05f,$"a hull pressed into {what} leaves the flight plane");
                yield return Send(Moving(Vector2.down));
                yield return new WaitForSecondsRealtime(1.5f);
                yield return Send(new GamepadState());
                float gone=Flat(pressed,hero.transform.position);
                Assert.Greater(gone,4,$"a hull pressed into {what} moved only {gone:0.0} m in the 1.5 s after the stick reversed: it's trapped");
                foreach(var wall in walls)Object.Destroy(wall);yield return null;
            }
            yield return Drive("a wall",Wall(apex+Vector3.left*25,Vector3.right,50));
            var left=(Vector3.left+Vector3.back).normalized;var right=(Vector3.right+Vector3.back).normalized;
            yield return Drive("a corner",Wall(apex,left,24),Wall(apex,right,24));
        }
        /// <summary>The chain never touches a wall (WALL_CONTACT appears in none of its logs), so this is the only place a real one gets hit.</summary>
        [UnityTest] public IEnumerator AHullDrivenIntoARealWallGetsBackOff([Values("Hub_CinderHalo","TallowApproach","WickReach","Gullet_Tunnel")] string zone)
        {
            yield return Zone(zone);
            var hero=PartyController.Current.Active;float hull=HeroCollision.HullRadius(hero.character);var home=hero.transform.position;
            Park(hero,home+Vector3.up*200);Put(hero,home+Vector3.up*200);
            hero.Health.InvulnerableUntil=GameTime.Now+300;
            var walls=new List<(Vector3 start,Vector3 way,string name)>();
            // Every 15 degrees, taken in an order that spreads the first six round the compass: Tallow's two warps stand between the rays of a 30-degree fan from its arrival.
            for(int i=0;i<24&&walls.Count<6;i++)
            {
                var way=Quaternion.Euler(0,i*75,0)*Vector3.forward;
                foreach(var hit in Physics.SphereCastAll(home+Vector3.up*.8f,hull,way,70,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                {
                    if(hit.collider.GetComponentInParent<Health>()!=null)continue;
                    // Head on: a hull that slid along the wall on the way in has nothing to prove on the way back along another line. Measured level, and only what's nearly flat isn't a wall:
                    // a hull held to the flight plane climbs nothing it meets, and the Gullet's bowl of a shell or the lip of Tallow's dock meets its sphere 35 to 45 degrees off plumb.
                    var facing=hit.normal;facing.y=0;
                    if(hit.distance>0&&Mathf.Abs(hit.normal.y)<.85f&&Vector3.Dot(facing.normalized,-way)>.7f)walls.Add((home+way*Mathf.Max(0,hit.distance-6),way,hit.collider.name));
                    break;
                }
            }
            Assert.IsNotEmpty(walls,$"fixture: {zone} has walls within 70 m of its arrival");
            var failures=new List<string>();
            foreach(var (start,way,name) in walls)
            {
                Put(hero,start);var stick=new Vector2(way.x,way.z);
                for(float end=Time.unscaledTime+1.2f;Time.unscaledTime<end;){hero.motor.Move(stick,false,false);yield return null;}
                var pressed=hero.transform.position;
                for(float end=Time.unscaledTime+1.1f;Time.unscaledTime<end;){hero.motor.Move(-stick,false,false);yield return null;}
                float gone=Flat(pressed,hero.transform.position);
                if(gone<4)failures.Add($"pressed into {name} at {pressed}, the hull moved only {gone:0.0} m in the second after the stick reversed");
            }
            TestContext.WriteLine($"REAL_WALLS {zone} {walls.Count}: {string.Join(", ",walls.Select(w=>w.name))}");
            Assert.IsEmpty(failures,$"{zone}: a wall traps a hull:\n"+string.Join("\n",failures));
        }
        /// <summary>DockReachTests sweeps a sphere at the Halo's and Tallow's docks and stops there. This flies a hull in on its own motor, from
        /// every side a hull can come from, and includes the Wick Reach's dock, which nothing else checks.</summary>
        [UnityTest] public IEnumerator EveryDockAnswersAHullFlownInFromEveryOpenSide([Values("Hub_CinderHalo","TallowApproach","WickReach")] string zone)
        {
            foreach(string flag in new[]{"lantern.routeKnown","wick.relay1","wick.relay2","wick.gateOpen"})Flag(flag);
            yield return Use("analog");
            yield return Zone(zone);
            var hero=PartyController.Current.Active;float hull=HeroCollision.HullRadius(hero.character);
            var docks=Object.FindObjectsByType<DockingPad>(FindObjectsSortMode.None);Assert.IsNotEmpty(docks,$"fixture: {zone} has a dock");
            Park(hero,hero.transform.position+Vector3.up*200);
            var failures=new List<string>();
            foreach(var dock in docks)
            {
                var spot=dock.transform.position;int open=0;
                for(int i=0;i<8;i++)
                {
                    var side=Quaternion.Euler(0,i*45,0)*Vector3.back;
                    // The hull itself must be out of the way of the look it takes first.
                    Put(hero,new Vector3(spot.x,201,spot.z));
                    var start=new Vector3(spot.x,1,spot.z)+side*14;var centre=start+Vector3.up*.8f;
                    if(Physics.CheckSphere(centre,hull,~0,QueryTriggerInteraction.Ignore))continue;
                    float stop=14;
                    foreach(var hit in Physics.SphereCastAll(centre,hull,-side,14,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                    {if(hit.collider.GetComponentInParent<CombatActor>()!=null)continue;stop=hit.distance;break;}
                    if(stop<14-dock.range-2)continue; // blocked well before the dock: not an approach side.
                    open++;Put(hero,start);
                    var stick=new Vector2(-side.x,-side.z);bool answered=false;float closest=14;
                    for(float end=Time.unscaledTime+2.2f;Time.unscaledTime<end&&!answered;)
                    {
                        hero.motor.Move(stick,false,false);yield return null;
                        closest=Mathf.Min(closest,Flat(hero.transform.position,spot));answered=PromptService.Interaction==dock;
                    }
                    if(!answered)failures.Add($"{dock.name} flown at from {side}: the hull gets to {closest:0.0} m of it, and the prompt says \"{PromptService.Interaction?.prompt}\", against its {dock.range} m range");
                }
                if(open==0)failures.Add($"{dock.name} has no side a hull can come in from");
            }
            Assert.IsEmpty(failures,$"{zone}:\n"+string.Join("\n",failures));
            if(zone!="WickReach")yield break;
            // And A docks, in the one zone no sweep ever looked at.
            var last=docks[0];Put(hero,last.transform.position+new Vector3(0,.3f,-6));yield return Frames(2);
            Assert.AreSame(last,PromptService.Interaction,"fixture: the hull is at the Wick's dock");
            Lead();yield return Tap(GamepadButton.South);
            for(float end=Time.unscaledTime+15;(SceneFlow.Current.Loading||State.zone!="Lanternwick")&&Time.unscaledTime<end;)yield return null;
            Assert.AreEqual("Lanternwick",State.zone,"A at the Wick's dock doesn't reach Lanternwick");
        }

        // ================================================================== 4.2 Controls: what the prompts say
        static readonly Regex PadSprite=new Regex("<sprite name=\"pad_"),KeySprite=new Regex("<sprite name=\"key_");
        /// <summary>The words a pad's buttons go by when no glyph is drawn, where a keyboard player can't press them. "A" only as a hint writes it, ahead of two spaces: it's also a word.</summary>
        static readonly Regex PadWords=new Regex(@"\b(RB|LB|RT|LT|LS|RS|D-pad|d-pad)\b|\b[BXY]\b|\bA {2}"),KeyWords=new Regex(@"\b(Esc|Left Shift|Tab|Arrow)\b");
        static void Scan(string surface,PromptDevice device,List<string> failures)
        {
            var sprite=device==PromptDevice.Keyboard?PadSprite:KeySprite;var words=device==PromptDevice.Keyboard?PadWords:KeyWords;int read=0;
            foreach(var text in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                if(!text.isActiveAndEnabled||text.name=="InputProbe"||string.IsNullOrEmpty(text.text))continue;
                read++;string shown=text.text;var found=sprite.Match(shown);
                if(!found.Success)found=words.Match(Regex.Replace(shown,"<[^>]+>"," "));
                if(found.Success)failures.Add($"{surface}: {text.name} names \"{found.Value}\" on {device}: \"{shown}\"");
            }
            Assert.Greater(read,0,"fixture: "+surface+" has words on it");
        }
        [UnityTest] public IEnumerator NoSurfaceNamesTheOtherDevicesButtons([Values("Keyboard","Gamepad")] string name)
        {
            var device=name=="Keyboard"?PromptDevice.Keyboard:PromptDevice.Gamepad;
            PromptService.ForceDevice(device);var failures=new List<string>();
            for(float end=Time.unscaledTime+15;Object.FindFirstObjectByType<TitleScreen>()==null&&Time.unscaledTime<end;)yield return null;
            yield return Frames(3);Scan("the title",device,failures);
            // In town.
            yield return Zone("Hub_Decks","Office");yield return Frames(3);Scan("the town HUD",device,failures);
            var panel=Object.FindFirstObjectByType<DialoguePanel>();panel.Show("Orrin",PortraitEmotion.Neutral,null,"The dock office is this way.");yield return Frames(2);
            Scan("a line of dialogue",device,failures);panel.Hide();
            var shop=Object.FindFirstObjectByType<ShopUi>();shop.Open(null);yield return Frames(2);Scan("the shop",device,failures);shop.Close();yield return Frames(2);
            // In a fight, with a target to show the switching hint.
            yield return Zone("Arena_Ground");var hero=PartyController.Current.Active;
            var hound=Enemy("Ridgehound",hero.transform.position+Vector3.forward*6);hero.target=hound.Health;yield return Frames(3);
            Scan("the fight HUD",device,failures);
            // Every pause tab, and the skill chooser.
            var menu=Object.FindFirstObjectByType<PauseMenu>();
            for(int page=0;page<menu.PageCount;page++)
            {
                menu.Open(page);yield return Frames(2);Scan($"pause tab {page}",device,failures);
                if(page==PauseMenu.SkillsPage){Row("Row_1").onClick.Invoke();yield return Frames(2);Scan("the skill chooser",device,failures);}
                menu.Close();yield return Frames(2);
            }
            // In flight.
            yield return Zone("Arena_Flight");yield return Frames(3);Scan("the flight HUD",device,failures);
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }
        [UnityTest] public IEnumerator TheHudsWordsFollowARealDeviceSwapBothWays()
        {
            pad=InputSystem.AddDevice<Gamepad>("QaPlanEarlyPad");keyboard=InputSystem.AddDevice<Keyboard>();
            yield return Zone("Arena_Ground");
            PromptService.ForceDevice(null);
            foreach(var (device,key) in new[]{(PromptDevice.Keyboard,true),(PromptDevice.Gamepad,false),(PromptDevice.Keyboard,true)})
            {
                if(key)yield return Keys(Key.J);else yield return Send(new GamepadState().WithButton(GamepadButton.South));
                yield return Frames(2);
                Assert.AreEqual(device,PromptService.Device,$"a real press on the {device} doesn't hand it the prompts");
                string other=device==PromptDevice.Keyboard?"pad":"keyboard";var unused=device==PromptDevice.Keyboard?PromptDevice.Gamepad:PromptDevice.Keyboard;
                string strip=Text("Skills").text,label=Text("SkillLabel0").text,partner=Text("Partner").text;
                StringAssert.Contains(PromptService.TagFor("Attack",device)+"  ATTACK",strip,$"the strip doesn't name {device}'s attack button");
                StringAssert.DoesNotContain(PromptService.TagFor("Attack",unused)+"  ATTACK",strip,$"on {device} the strip still names the {other}'s attack button");
                StringAssert.Contains(PromptService.TagFor("Swap",device),partner,$"the partner row doesn't name {device}'s swap button");
                StringAssert.Contains(PromptService.TagFor(device==PromptDevice.Keyboard?"Skill1":"SkillMod",device),label,$"the first skill's label doesn't name {device}'s button");
                if(key)yield return Keys();else yield return Send(new GamepadState());
                yield return Frames(3);
                Assert.AreEqual(device,PromptService.Device,$"letting go of the {device} hands the prompts back");
            }
            // Open on the way past: the page follows too.
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open(PauseMenu.ItemsPage);yield return Frames(2);
            yield return Send(new GamepadState{leftTrigger=1});yield return Frames(2);yield return Send(new GamepadState());yield return Frames(2);
            Assert.AreEqual(PromptDevice.Gamepad,PromptService.Device,"fixture: the pad spoke");
            var note=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).First(t=>t.name=="Note");
            StringAssert.Contains("Press "+PromptService.TagFor("Swap",PromptDevice.Gamepad),note.text,"an open page keeps the keyboard's words after the pad has taken over");
            menu.Close();
        }
        /// <summary>A real analog stick rests a hair off centre. The dead zone hides that from movement, and the device watch reads the raw state.</summary>
        [UnityTest] public IEnumerator AStickRestingOffCentreDoesNotHoldThePromptsOnThePad()
        {
            pad=InputSystem.AddDevice<Gamepad>("QaPlanEarlyPad");keyboard=InputSystem.AddDevice<Keyboard>();
            yield return Zone("Arena_Ground");
            PromptService.ForceDevice(null);
            InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=new Vector2(.04f,-.03f)});yield return Frames(2);
            Assert.Less(pad.leftStick.ReadValue().sqrMagnitude,.0001f,"fixture: the dead zone hides this much drift");
            yield return Keys(Key.J);yield return Frames(2);
            Assert.AreEqual(PromptDevice.Keyboard,PromptService.Device,"a keyboard player pressing keys with an analog pad plugged in, its stick untouched, is shown pad buttons");
            yield return Keys();yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(PromptDevice.Keyboard,PromptService.Device,"let go of the keys and the prompts jump to a pad nobody has touched");
            StringAssert.Contains(PromptService.TagFor("Attack",PromptDevice.Keyboard),Text("Skills").text,"the strip doesn't name the keyboard's attack key");
        }

        // ================================================================== 4.3 Progression and pacing
        static int SyncAt(int xp){var hero=new MemberState{id="Taren",level=1};Levels.Grant(hero,xp);return hero.level;}
        [UnityTest] public IEnumerator BySyncSevenEachHeroHasTheTwoSkillsThePlanNames()
        {
            yield return null;
            foreach(var member in State.party)member.level=7;
            foreach(var (hero,knows,later) in new[]{("Taren",new[]{"Anchor Slam","Tether Hook"},new[]{"Forge Spiral","Faultline"}),("Sela",new[]{"Survey Drone","Waypoint Blink"},new[]{"Gravity Fold","Starfall"})})
            {
                var learned=SkillBook.Learned(hero).Select(s=>s.displayName).ToArray();
                foreach(string skill in knows)CollectionAssert.Contains(learned,skill,$"at the end of chapter 1 {hero} should know {skill}");
                foreach(string skill in later)CollectionAssert.DoesNotContain(learned,skill,$"at the end of chapter 1 {hero} already knows {skill}");
            }
        }
        /// <summary>The chain did chapter 2's three side quests and none of chapter 1's, and still ended chapter 2 at Sync 11 (Builds/quality/QA/v2c/s16/final-state.json).</summary>
        [UnityTest] public IEnumerator ThePlansSyncBandHoldsWithEverySideQuestDone()
        {
            yield return null;
            // The main path's XP at chapter 2's end: the chain's measured 7095 less the 620 of the three side quests it did (Builds/quality/QA/v2c/s16). TheMainPathLandsOnThePlansSyncLevels' ledger runs 320 low.
            const int mainPath=6475;
            int sides=GameCatalog.All<QuestDef>().Where(q=>q.id!="Sorrel"&&q.id!="Hushwell"&&q.id!="LanternRoad").Sum(q=>q.rewardXp);
            Assert.Greater(sides,0,"fixture: the side quests pay XP");
            int sync=SyncAt(mainPath+sides);
            TestContext.WriteLine($"SYNC_BAND main={mainPath} sides={sides} sync={sync}");
            // The lead's call: keep the curve. The plan's band is the main path at 8 to 10, and 11 with every side quest, with Sync 12's skills waiting for chapter 3.
            Assert.LessOrEqual(sync,11,$"a player who does the {sides} XP of side quests ends chapter 2 at Sync {sync}: the plan says 11 with every side quest, and Sync 12 belongs to chapter 3");
            Assert.GreaterOrEqual(sync,SyncAt(mainPath),"side quests leave a player behind the main path");
            Assert.GreaterOrEqual(SyncAt(mainPath),8,"a player who skips them all ends chapter 2 under the plan's band");
            Assert.LessOrEqual(SyncAt(mainPath),10,"a player who skips every side quest still ends chapter 2 over the plan's band");
        }
        [UnityTest] public IEnumerator ALevelUpCardNamesTheSkillLearnedAndSaysWhereToEquipIt()
        {
            yield return Zone("Arena_Ground");
            var toast=Object.FindFirstObjectByType<QuestToast>();yield return new WaitForSecondsRealtime(.3f);
            IEnumerator CardFor(string heading,string what)
            {
                for(float end=Time.unscaledTime+10;!(toast.Showing&&toast.Heading.StartsWith(heading))&&Time.unscaledTime<end;)yield return null;
                Assert.IsTrue(toast.Showing&&toast.Heading.StartsWith(heading),$"{what}: no {heading} card. On screen: {toast.Heading} / {toast.Body}");
            }
            foreach(int level in new[]{3,4,5,8,12})
            {
                foreach(var member in State.party){member.level=level-1;member.xp=0;}
                foreach(var member in State.party)Levels.Grant(member,Levels.RequiredXp(level-1));
                yield return CardFor("SYNC "+level,"a level-up");
                if(level==4)
                {
                    StringAssert.DoesNotContain("NEW SKILL",toast.Heading,"a level that teaches nothing says it does");
                    StringAssert.Contains("Sync 4",toast.Body,"a level that teaches nothing doesn't say what happened");
                    continue;
                }
                StringAssert.Contains("NEW SKILL",toast.Heading,$"Sync {level} teaches a skill and its card doesn't lead with that");
                foreach(string hero in new[]{"Taren","Sela"})
                {
                    string skill=SkillBook.All(hero).First(s=>s.unlockLevel==level).displayName;
                    StringAssert.Contains(skill,toast.Body,$"the Sync {level} card doesn't name {hero}'s {skill}");
                }
                StringAssert.Contains("Skills",toast.Body,$"the Sync {level} card doesn't say where to equip what was learned");
            }
        }
        [UnityTest] public IEnumerator MasteryCardsComeAtSixEighteenThirtySixAndSixtyUses()
        {
            yield return Zone("Arena_Ground");
            var toast=Object.FindFirstObjectByType<QuestToast>();yield return new WaitForSecondsRealtime(.3f);
            var skill=SkillBook.All("Taren").First(s=>s.kind==SkillKind.ArcCleave);int used=0,level=2;
            foreach(int at in new[]{6,18,36,60})
            {
                string heading="MASTERY "+level;
                while(used<at-1){SkillBook.RecordUse("Taren",skill);used++;}
                yield return Frames(2);
                Assert.AreNotEqual(heading,toast.Heading,$"the {heading} card came at {used} uses, before its {at}");
                SkillBook.RecordUse("Taren",skill);used++;
                for(float end=Time.unscaledTime+8;!(toast.Showing&&toast.Heading==heading)&&Time.unscaledTime<end;)yield return null;
                Assert.IsTrue(toast.Showing&&toast.Heading==heading,$"{at} uses and no {heading} card. On screen: {toast.Heading}");
                StringAssert.Contains(skill.displayName,toast.Body,$"the {heading} card doesn't name the skill");
                StringAssert.Contains(DisplayNames.Speaker("Taren"),toast.Body,$"the {heading} card doesn't name whose it is");
                level++;
            }
        }
        [UnityTest] public IEnumerator CorrasStockIsRefinedAndOutclassesWhatTheHeroesStartedWith()
        {
            // A party as the story makes it: both heroes wearing what they arrive in.
            GameServices.Current.NewGame();var joined=new MemberState{id="Sela"};State.party.Add(joined);GameServices.GiveStartingGear(State,joined);
            State.scrip=1000;
            yield return Zone("Lanternwick");
            var stock=GameCatalog.Find<ShopInventory>("Lanternwick");Assert.IsNotNull(stock,"Lanternwick has no shop of its own");
            Assert.IsTrue(stock.parts.Any(p=>p.slot==GearSlot.Edge)&&stock.parts.Any(p=>p.slot==GearSlot.Emitter),"Corra sells nothing either hero fights with");
            var shop=Object.FindFirstObjectByType<ShopUi>();shop.Open(stock);yield return Frames(2);
            foreach(var part in stock.parts)
            {
                Assert.AreEqual(2,part.tier,$"Corra's {part.displayName} isn't a T2 part");
                StringAssert.StartsWith("Refined",part.displayName,$"{part.id} doesn't read as Refined");
                string row=Words(With(part.displayName));
                StringAssert.Contains(" scrip",row,$"the {part.displayName} row has no price");
                StringAssert.DoesNotContain(part.id,row,$"the {part.displayName} row prints a raw id");
                // Everything counted the way the part is: what T2 gives against the T1 of the same slot.
                var starter=GameCatalog.Find<TechPartDef>(part.id.Replace("T2","T1"));Assert.IsNotNull(starter,"fixture: the starter part of the same slot: "+part.id);
                float Sum(Stats s)=>s.output+s.plating+s.response+s.resonance+s.fortune;
                Assert.GreaterOrEqual(Sum(part.baseStats),Sum(starter.baseStats)*2,$"{part.displayName} doesn't clearly outclass {starter.displayName}");
            }
            // What the player reads at the counter: the offer beside what the hero wears now.
            var edge=stock.parts.First(p=>p.slot==GearSlot.Edge);string words=Words(With(edge.displayName));
            var numbers=Regex.Match(words,@"Output \+(\d+).*now \+(\d+)");
            Assert.IsTrue(numbers.Success,$"Taren wears a Scrap Edge, and the Refined Edge row doesn't set one against the other: \"{words}\"");
            Assert.GreaterOrEqual(int.Parse(numbers.Groups[1].Value),int.Parse(numbers.Groups[2].Value)*2,$"the Refined Edge doesn't read as clearly better than what's worn: \"{words}\"");
            int price=int.Parse(Regex.Match(words,@"(\d+) scrip").Groups[1].Value);
            With(edge.displayName).onClick.Invoke();yield return Frames(2);
            Assert.AreEqual(1000-price,State.scrip,"buying a Refined Edge doesn't take its price");
            Assert.IsTrue(State.parts.Any(p=>p.definitionId==edge.id),"buying a Refined Edge doesn't put one in Gear");
            StringAssert.Contains("Purchased "+edge.displayName,Feedback(),"nothing says the purchase went through");
            shop.Close();
        }
        [UnityTest] public IEnumerator TheMainQuestsAlonePayForARefinedPart()
        {
            yield return null;
            var stock=GameCatalog.Find<ShopInventory>("Lanternwick");
            // Not a kill, not a boss's purse, not a side quest: a new game's scrip and what the two main quests before Lanternwick pay.
            int purse=new GameState().scrip+GameCatalog.Find<QuestDef>("Sorrel").rewardScrip+GameCatalog.Find<QuestDef>("Hushwell").rewardScrip;
            int cheapest=stock.prices.Take(stock.parts.Length).Min();
            Assert.GreaterOrEqual(purse,cheapest,$"the main quests pay {purse} scrip by Lanternwick and Corra's cheapest part is {cheapest}: a Refined part takes grinding");
        }
    }
}
