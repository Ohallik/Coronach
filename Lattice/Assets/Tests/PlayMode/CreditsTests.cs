using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
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
    /// <summary>The game's end (Credits, CreditsRoll; ch10-wiring section 8, D288): in Neighbourhood Afloat the conversation that puts game.complete up hands over to the credits, which hold
    /// the party and the menus, play the title's theme over a camera of their own above the town, and carry the two lines, every credit owed at couch sizes and a reading pace, the last
    /// line alone and the title; holding A hurries them and Start skips them only once they have been seen; at their end the party stands in the town with the closing line and the save
    /// says so, and the title knows the game has been finished. No CHAPTER 10 COMPLETE card shows, and every earlier chapter's still does. The flag is set by hand or by the script's own
    /// node (JunLastLine), never through the neighbourhood's own staging. Each check is first shown to fail on the state it guards against, inside the test. Stills of the first screen, a
    /// middle heading, the last line and the title land in the mirror's Builds/quality/focused/credits.</summary>
    public sealed class CreditsTests
    {
        const string Town="NeighbourhoodAfloat",ClosingLine="The convoy is through, and everyone who came is here. There is no hurry.";
        Gamepad pad;Keyboard keyboard;bool seenBefore;
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        readonly List<string> log=new();
        void Heard(string line,string stack,LogType type)=>log.Add(line);
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            seenBefore=Credits.Seen;Credits.Seen=false;
            GameTime.Reset();PromptService.ForceDevice(null);SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            pad=InputSystem.AddDevice<Gamepad>();keyboard=InputSystem.AddDevice<Keyboard>();
            log.Clear();Application.logMessageReceived+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(savesSwapped!=null){Saves=savesSwapped;savesSwapped=null;}
            foreach(var (quest,handle) in made){handle.Dispose();Object.Destroy(quest);}made.Clear();
            Credits.Seen=seenBefore;
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            PromptService.ForceDevice(null);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Seconds(float s){float until=Time.unscaledTime+s;while(Time.unscaledTime<until)yield return null;}
        static IEnumerator Until(System.Func<bool> met,float seconds){float deadline=Time.unscaledTime+seconds;while(!met()&&Time.unscaledTime<deadline)yield return null;}
        static Credits Roll=>Credits.Current;
        static QuestToast Toast=>Object.FindFirstObjectByType<QuestToast>();
        /// <summary>The story told to the last scene's door: every earlier chapter (DevLoadout), the operation done, Orrin's offer heard, the handrail mended and the line painted, TurnBack at
        /// its last step. With <paramref name="over"/>, the last scene heard too: the main quest done and game.complete up, as a save made after it (or during the roll) holds it.</summary>
        static void Told(bool over)
        {
            GameServices.Current.NewGame();DevLoadout.Apply("chapter10");
            foreach(var f in new[]{"end.planHeard","end.planSaid","end.motherFreed","end.gateOpen","end.boarded","end.meretShip","end.meretGantry","end.crewsOrdered","end.anchorReleased",
                "end.passageRun","end.tugFetched","end.rendezvous","end.epilogue","end.handrail","end.railMended","end.linePainted","visited.NeighbourhoodAfloat"})Flag(f);
            foreach(var s in new[]{"Taren","Mira","Jun","Pip","Orrin","Sela","Idra"})Flag("met."+s);
            var main=GameCatalog.Find<QuestDef>("TurnBack");Assert.IsNotNull(main,"fixture: the main quest");
            State.questSteps["TurnBack"]=over?main.steps.Length:main.steps.Length-1;
            if(over){Flag("quest.TurnBack.complete");Flag(Credits.Flag);}
            State.zone=Town;State.spawn="Arrival";
        }
        static IEnumerator Load(string zone=Town,string spawn="Arrival")
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone(zone,spawn);deadline=Time.unscaledTime+45;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            DialogueSystem.Current.AutoAdvance=false;
        }
        static CombatActor Lead=>PartyController.Current.Active;
        static string Objective(){var hud=Object.FindFirstObjectByType<ObjectiveHud>();return hud.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Objective").text.Split('\n')[0].Trim();}
        static Canvas ObjectiveCanvas=>Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<Canvas>(true).First(c=>c.name=="Objective");
        static Transform GroundCamera=>Object.FindFirstObjectByType<CameraRig>().transform.Find("GroundFollow");
        static TMP_Text Word(string name)=>Roll.Words.First(t=>t.name==name);
        /// <summary>The roll put at a moment and held there for a few frames (a frame of the town can take a third of a second in the runner), so a still shows that moment.</summary>
        static IEnumerator At(float clock){for(int i=0;i<3;i++){Roll.Clock=clock;yield return null;}}
        /// <summary>A virtual pad's button pressed for two frames and let go for two.</summary>
        IEnumerator Press(GamepadButton button){InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return Frames(2);InputSystem.QueueStateEvent(pad,new GamepadState());yield return Frames(2);}
        IEnumerator Trigger(bool left){InputSystem.QueueStateEvent(pad,left?new GamepadState{leftTrigger=1}:new GamepadState{rightTrigger=1});yield return Frames(2);InputSystem.QueueStateEvent(pad,new GamepadState());yield return Frames(2);}
        IEnumerator Tap(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return Frames(2);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Frames(2);}
        /// <summary>A conversation of somebody's in the town, with the flag put up by hand while it is on screen: the hand-over as the wiring has it, without the neighbourhood's staging.</summary>
        static IEnumerator TalkThatPutsTheFlagUp()
        {
            var dialogue=DialogueSystem.Current;dialogue.AutoAdvance=true;
            Assert.IsTrue(dialogue.StartNode("PipRota","Pip"),"fixture: Pip's line wouldn't start");yield return Frames(4);
            Assert.IsTrue(dialogue.Running,"fixture: the talk ended before the flag went up");Flag(Credits.Flag);
            yield return Until(()=>!dialogue.Running,20);Assert.IsFalse(dialogue.Running,"fixture: the talk never ended");
        }
        /// <summary>What the game's camera draws this frame with every canvas over it (drawn by that camera for the moment), as a 1920 by 1080 still, in the mirror's Builds/quality/focused/credits.</summary>
        static void Still(string name)
        {
            const int W=1920,H=1080;var camera=Camera.main;Assert.IsNotNull(camera,"fixture: no game camera");
            var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var image=new Texture2D(W,H,TextureFormat.RGB24,false);var active=RenderTexture.active;var target=camera.targetTexture;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;
                foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=camera.nearClipPlane+.1f;}
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/focused/credits"));Directory.CreateDirectory(folder);
                string file=Path.Combine(folder,name+".png");File.WriteAllBytes(file,image.EncodeToPNG());TestContext.WriteLine("CREDITS_STILL "+file);
            }
            finally
            {
                foreach(var c in canvases)if(c!=null){c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}
                camera.targetTexture=target;RenderTexture.active=active;Object.Destroy(rt);Object.Destroy(image);
            }
        }
        /// <summary>Where a piece of type's letters really stand, and its box, in the credits canvas's own units (1920 by 1080 at 16:9, whatever the test's screen).</summary>
        static (Rect ink,Rect box) Measure(TMP_Text text,RectTransform canvas)
        {
            var local=UiKit.Ink(text);var c=new Vector3[4];text.rectTransform.GetWorldCorners(c);
            Vector3 lo=canvas.InverseTransformPoint(text.transform.TransformPoint(new Vector3(local.xMin,local.yMin))),hi=canvas.InverseTransformPoint(text.transform.TransformPoint(new Vector3(local.xMax,local.yMax)));
            Vector3 a=canvas.InverseTransformPoint(c[0]),b=canvas.InverseTransformPoint(c[2]);
            return (Rect.MinMaxRect(lo.x,lo.y,hi.x,hi.y),Rect.MinMaxRect(a.x,a.y,b.x,b.y));
        }
        /// <summary>What is wrong with a piece of type: out of its box, wider than the 1920 the canvas is made for, or under the type floor.</summary>
        static string Fault(TMP_Text text,RectTransform canvas)
        {
            var (ink,box)=Measure(text,canvas);const float slack=1;
            if(text.fontSize<24)return $"'{text.text}' is {text.fontSize}, under the type floor";
            if(ink.xMin<box.xMin-slack||ink.xMax>box.xMax+slack||ink.yMin<box.yMin-slack||ink.yMax>box.yMax+slack)return $"'{text.text}' runs out of its box: ink {ink}, box {box}";
            if(ink.xMin<-960||ink.xMax>960)return $"'{text.text}' runs off a 1920-wide screen: {ink}";
            return null;
        }

        // ------------------------------------------------------------------ the hand-over, the town under the roll, the stills
        /// <summary>Only a conversation that puts game.complete up hands over (the script's own JunLastLine here): a flag put up by hand outside a talk rolls nothing, so nobody's fixture
        /// starts the credits. From the frame the talk closes the roll has the screen: the party held, the HUD gone, nothing barking, the title's theme, no card; the camera draws back from
        /// the party to the town's wide frames until the convoy's hulls and the new sky's giant are in it. Its four moments are kept as stills.</summary>
        [UnityTest] public IEnumerator TheLastSceneHandsOverToTheCreditsOverTheTownAndNothingElseDoes()
        {
            Told(false);yield return Load();yield return Seconds(1.5f);
            var roll=Roll;Assert.IsNotNull(roll,"there are no credits in the town the game ends in");Assert.AreEqual(Credits.Stage.Waiting,roll.Now);
            // A flag put up by a fixture, not by a talk: nothing.
            Flag(Credits.Flag);yield return Seconds(1.5f);
            Assert.AreEqual(Credits.Stage.Waiting,roll.Now,"game.complete put up outside any conversation rolled the credits: every fixture that sets it would");
            Flag(Credits.Flag,false);yield return Frames(2);
            // A control the convoy check needs: from the party's own camera no hull of the convoy is in frame.
            Assert.IsEmpty(ConvoyInFrame(),"fixture: the convoy is in the party's own frame, so seeing it later proves nothing");
            var dialogue=DialogueSystem.Current;dialogue.AutoAdvance=true;
            Assert.IsTrue(dialogue.StartNode("JunLastLine","Taren"),"the last scene's node isn't in the script");
            yield return Until(()=>!dialogue.Running,40);Assert.IsFalse(dialogue.Running,"the last scene never ended");
            Assert.IsTrue(Has(Credits.Flag),"the last scene's own line doesn't put game.complete up");
            Assert.IsTrue(roll.Running,"the last scene closed with game.complete up and the credits didn't take the screen in that frame");
            Assert.IsTrue(GameInput.Current.Blocked,"the party isn't held under the roll");
            yield return Frames(3);
            Assert.AreEqual(Credits.Music,MusicDirector.Current.Track,"the credits don't play their own music");
            Assert.IsTrue(BarkService.Hushed,"something can bark over the roll");Assert.IsFalse(ObjectiveCanvas.enabled,"the objective panel stays up over the roll");
            // The last line's setFlag runs as the talk ends, so the card it earns is queued at the toast's next look (every 0.2 s), under the roll.
            var pending=(System.Collections.IList)typeof(QuestToast).GetField("pending",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            yield return Until(()=>pending.Count>0,1.5f);
            Assert.Greater(pending.Count,0,"fixture: the last quest's card isn't waiting under the roll, so its not coming up afterwards proves nothing");
            for(float until=Time.unscaledTime+2;Time.unscaledTime<until;){Assert.IsFalse(Toast.Showing,"a card comes up over the roll: "+Toast.Heading);Assert.IsFalse(QuestToast.CardUp);yield return null;}
            // The world goes on being drawn behind the type: the camera has left the party for the town's wide frame, and nothing over it is opaque.
            yield return At(9.6f);
            Assert.Greater(Vector3.Distance(Camera.main.transform.position,GroundCamera.position),30,"the camera never left the party: the roll stands over the stall, not the town");
            Assert.IsTrue(Camera.main.enabled&&Camera.main.cullingMask!=0,"the town isn't drawn under the roll");
            foreach(var image in Object.FindObjectsByType<Image>(FindObjectsSortMode.None).Where(i=>i.name=="CreditsBand"||i.name=="CreditsDim"))Assert.Less(image.color.a,.7f,image.name+" hides the town");
            Still("credits-1-first");
            Assert.AreEqual(1,Word("CreditsFirstLine").alpha,.01f,"the first line isn't up in the first still");Assert.AreEqual(1,Word("CreditsSecondLine").alpha,.01f,"the second line isn't up with it");
            // A heading in the middle of the roll, at the middle of the screen.
            const string borrowed="WHAT WE BORROWED, WITH THANKS";float half=Mathf.Max(540,((RectTransform)roll.Roll.GetComponentInParent<Canvas>().transform).rect.height*.5f);
            yield return At(Credits.RollAt+(roll.Headings[borrowed]+half)/Credits.Speed);Still("credits-2-heading");
            // The convoy under its new sky, as the drift ends: its hulls at their lines and the giant.
            yield return At(roll.TitleAt-.5f);
            Assert.IsNotEmpty(ConvoyInFrame(),"none of the convoy's hulls is in the roll's frame: the credits aren't over the convoy");
            var giant=GameObject.Find("The giant");Assert.IsNotNull(giant,"fixture: the new sky's giant");
            var g=Camera.main.WorldToViewportPoint(giant.GetComponent<Renderer>().bounds.center);Assert.IsTrue(g.z>0&&g.x>0&&g.x<1&&g.y>0&&g.y<1,"the new sky's giant isn't in the roll's frame: "+g);
            yield return At(roll.LastLineAt+1.5f);Still("credits-3-last");
            yield return At(roll.TitleAt+2);Still("credits-4-title");
            // To its end: the camera home, the hold let go, and the card the last scene earned never comes up.
            yield return At(roll.TitleEnd);yield return Until(()=>roll.Now==Credits.Stage.Done,8);
            Assert.AreEqual(Credits.Stage.Done,roll.Now,"the roll never ended");
            Assert.IsFalse(GameInput.Current.Blocked,"the party is still held after the roll");
            for(float until=Time.unscaledTime+3;Time.unscaledTime<until;){Assert.IsFalse(Toast.Showing,"after the roll a card comes up over the town: "+Toast.Heading);yield return null;}
        }
        static List<string> ConvoyInFrame()
        {
            var view=Camera.main;var seen=new List<string>();
            // A hull's renderers are the pieces of its model, under the object the town names for it.
            foreach(var hull in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.GetComponentsInParent<Transform>(true).Any(t=>t.name.StartsWith("The convoy, moored"))))
            {var p=view.WorldToViewportPoint(hull.bounds.center);if(p.z>0&&p.x>.02f&&p.x<.98f&&p.y>.02f&&p.y<.98f)seen.Add(hull.name);}
            return seen;
        }

        // ------------------------------------------------------------------ presses
        /// <summary>Every button on the pad and every key the game binds, pressed in turn under the roll, does nothing: no pause, log, map, bag, swap, item or step, no talk, no skip the
        /// first time through. First shown to reach the game at all (Start opens the pause menu before the roll). Holding A runs it five times over; once the roll has been seen to its end,
        /// Start skips it, the press doesn't fall through to the pause menu, and the party is let go.</summary>
        [UnityTest] public IEnumerator NoPressBreaksTheRollHoldingAHurriesItAndOnlyASeenRollSkips()
        {
            Told(false);yield return Load();yield return Seconds(1);
            var menu=Object.FindFirstObjectByType<PauseMenu>();
            yield return Press(GamepadButton.Start);
            Assert.IsTrue(menu.IsOpen,"fixture: the pad's Start never reached the game, so the presses below prove nothing");
            menu.Close();yield return Seconds(.6f);
            yield return TalkThatPutsTheFlagUp();yield return Frames(2);
            var roll=Roll;Assert.IsTrue(roll.Running,"a talk that put game.complete up closed and nothing rolled");
            var from=Lead.transform.position;string zone=State.zone;int scrip=State.scrip;
            var faults=new List<string>();
            void Held(string what)
            {
                if(menu.IsOpen){faults.Add(what+" opened the pause menu");menu.Close();}
                if(Object.FindFirstObjectByType<ShopUi>().IsOpen)faults.Add(what+" opened a shop");
                if(DialogueSystem.Current.Running)faults.Add(what+" started a conversation");
                if(!roll.Running||roll.Now==Credits.Stage.Returning)faults.Add(what+" ended or skipped the roll the first time through");
                if(!GameInput.Current.Blocked)faults.Add(what+" let go of the party");
                if(GameTime.Paused)faults.Add(what+" paused the game under the roll");
            }
            foreach(var button in new[]{GamepadButton.South,GamepadButton.East,GamepadButton.West,GamepadButton.North,GamepadButton.Start,GamepadButton.Select,GamepadButton.LeftShoulder,
                GamepadButton.RightShoulder,GamepadButton.DpadUp,GamepadButton.DpadDown,GamepadButton.DpadLeft,GamepadButton.DpadRight,GamepadButton.LeftStick,GamepadButton.RightStick})
            {yield return Press(button);Held("the pad's "+button);}
            yield return Trigger(true);Held("the left trigger");yield return Trigger(false);Held("the right trigger");
            foreach(var key in new[]{UnityEngine.InputSystem.Key.Escape,UnityEngine.InputSystem.Key.M,UnityEngine.InputSystem.Key.E,UnityEngine.InputSystem.Key.Tab,UnityEngine.InputSystem.Key.Space,
                UnityEngine.InputSystem.Key.J,UnityEngine.InputSystem.Key.K,UnityEngine.InputSystem.Key.Q,UnityEngine.InputSystem.Key.F,UnityEngine.InputSystem.Key.R,UnityEngine.InputSystem.Key.Enter,
                UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.A,UnityEngine.InputSystem.Key.S,UnityEngine.InputSystem.Key.D,UnityEngine.InputSystem.Key.Digit1,UnityEngine.InputSystem.Key.Digit4,
                UnityEngine.InputSystem.Key.LeftShift})
            {yield return Tap(key);Held("the "+key+" key");}
            Assert.IsEmpty(faults,string.Join("\n",faults));
            Assert.Less(Vector3.Distance(Lead.transform.position,from),.15f,"a press moved the party under the roll");Assert.AreEqual(zone,State.zone);Assert.AreEqual(scrip,State.scrip,"a press spent scrip under the roll");
            // A press shows how to hurry, and no skip the first time through.
            yield return Press(GamepadButton.East);var hint=Word("CreditsHint");
            StringAssert.Contains("HOLD TO HURRY",hint.text,"a press under the roll doesn't say how to hurry it");StringAssert.DoesNotContain("SKIP",hint.text,"a roll never seen offers a skip");
            // Hurried: A held runs it five times over; let go, at its own pace.
            float before=roll.Clock,started=Time.unscaledTime;yield return Seconds(1);float plain=(roll.Clock-before)/(Time.unscaledTime-started);
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return Frames(2);
            before=roll.Clock;started=Time.unscaledTime;yield return Seconds(1);float hurried=(roll.Clock-before)/(Time.unscaledTime-started);
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return Frames(2);
            Assert.AreEqual(1,plain,.2f,"the roll doesn't run at its own pace");Assert.AreEqual(Credits.HurryRate,hurried,.8f,"holding A doesn't hurry the roll");
            Assert.IsTrue(roll.Running&&roll.Now!=Credits.Stage.Returning,"hurrying ended the roll");
            // Seen once on this machine: Start skips, and its press goes no further.
            Credits.Seen=true;yield return Press(GamepadButton.East);StringAssert.Contains("SKIP",Word("CreditsHint").text,"a roll that has been seen doesn't say it can be skipped");
            yield return Press(GamepadButton.Start);
            Assert.AreEqual(Credits.Stage.Returning,roll.Now,"Start doesn't skip a roll that has been seen");
            yield return Until(()=>roll.Now==Credits.Stage.Done,8);Assert.AreEqual(Credits.Stage.Done,roll.Now,"a skipped roll never let go");
            Assert.IsFalse(menu.IsOpen,"the skip's press fell through to the pause menu");Assert.IsFalse(GameInput.Current.Blocked,"a skipped roll still holds the party");
            Assert.IsTrue(Has(Credits.Rolled),"a skipped roll isn't marked rolled in the save");
        }

        // ------------------------------------------------------------------ the end
        /// <summary>A save made between the last scene and the roll's end (a quit in the middle of it) rolls it again on entering the town. At its end: game.complete and the roll's own mark
        /// are in the autosave, the roll is seen on this machine, the camera is the party's again, the town's music is back, the objective panel says the main quest's closing line, and nothing
        /// is held. Continue from the title loads that, in the town, with the same line and no roll; and the title says the game has been played to its end.</summary>
        [UnityTest] public IEnumerator AfterTheRollThePartyStandsInTheTownWithTheClosingLineAndContinueLoadsJustThat()
        {
            Told(true);GameServices.Current.Saves.Save("autosave",State);
            Assert.IsFalse(Credits.Finished(new GameState()),"fixture: a fresh game reads as finished");
            yield return Load();var roll=Roll;
            yield return Until(()=>roll.Running,6);Assert.IsTrue(roll.Running,"entering the town with the game over and the roll never seen to its end rolls nothing");
            // The state the end has to change (the panel goes in its own LateUpdate).
            yield return Frames(2);
            Assert.IsTrue(GameInput.Current.Blocked&&!ObjectiveCanvas.enabled,"fixture: under the roll the panel is up or the party free");
            Assert.IsFalse(GameServices.Current.Saves.Load("autosave").flags.ContainsKey(Credits.Rolled),"fixture: the save says rolled before the roll has ended");
            Assert.IsFalse(Credits.Seen,"fixture: the roll is marked seen before it has been");
            yield return At(roll.TitleEnd-.2f);yield return Until(()=>roll.Now==Credits.Stage.Done,8);
            Assert.AreEqual(Credits.Stage.Done,roll.Now,"the roll never ended");Assert.IsTrue(log.Contains("CREDITS_END"));
            yield return Seconds(1.6f);
            Assert.IsFalse(GameInput.Current.Blocked,"the party is still held after the roll");Assert.IsTrue(ObjectiveCanvas.enabled,"the objective panel doesn't come back");
            Assert.AreEqual(ClosingLine,Objective(),"after the roll the panel doesn't say the main quest's closing line");
            Assert.IsNull(GameObject.Find("CreditsView"),"the roll's camera is still there");
            Assert.Less(Vector3.Distance(Camera.main.transform.position,GroundCamera.position),1.5f,"the camera didn't come home to the party");
            Assert.AreEqual(GameCatalog.Find<ZoneDef>(Town).musicId,MusicDirector.Current.Track,"the town's own music doesn't come back after the roll");
            Assert.IsFalse(BarkService.Hushed&&!Object.FindObjectsByType<Lattice.World.QuietAfter>(FindObjectsSortMode.None).Any(q=>q.enabled),"the roll still hushes the town");
            Assert.IsTrue(Credits.Seen,"the roll seen to its end isn't marked seen on this machine");
            var saved=GameServices.Current.Saves.Load("autosave");
            Assert.IsTrue(Credits.Finished(saved)&&saved.flags.TryGetValue(Credits.Rolled,out bool rolled)&&rolled,"the autosave after the roll doesn't hold the game's end and the roll's");
            Assert.AreEqual(Town,saved.zone,"the autosave after the roll isn't in the town");Assert.IsTrue(saved.flags.ContainsKey("quest.TurnBack.complete"),"the autosave doesn't hold the main quest done");
            // Quit to the title, which says so, and Continue.
            SceneFlow.Current.LoadZone("Title");float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            yield return Frames(2);var title=Object.FindFirstObjectByType<TitleScreen>();Assert.IsNotNull(title,"fixture: the title never came up");
            Assert.IsTrue(title.Finished,"the title doesn't know the game has been played to its end");
            title.StartGame(true);yield return null;
            deadline=Time.unscaledTime+45;while((SceneFlow.Current.Loading||SceneFlow.Current.Zone!=Town)&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual(Town,SceneFlow.Current.Zone,"Continue doesn't come back to the town");
            yield return Seconds(3);
            Assert.IsNotNull(Roll,"fixture: the town's credits");Assert.AreEqual(Credits.Stage.Waiting,Roll.Now,"Continue after the roll rolls it again");
            Assert.IsFalse(GameInput.Current.Blocked,"Continue arrives held");Assert.AreEqual(ClosingLine,Objective(),"Continue doesn't say the closing line");
        }

        // ------------------------------------------------------------------ the cards
        readonly List<(QuestDef quest,System.IDisposable handle)> made=new();
        static readonly FieldInfo Timer=typeof(QuestToast).GetField("showing",BindingFlags.NonPublic|BindingFlags.Instance);
        /// <summary>The headings that come up within the time, each sent on after a frame so a queue of cards turns over quickly.</summary>
        static IEnumerator Read(List<string> seen,float seconds)
        {
            for(float until=Time.unscaledTime+seconds;Time.unscaledTime<until;)
            {
                yield return null;var toast=Toast;if(toast==null)continue;
                if(toast.Showing&&(seen.Count==0||seen[seen.Count-1]!=toast.Heading))seen.Add(toast.Heading);
                Timer.SetValue(toast,0f);
            }
        }
        /// <summary>No CHAPTER 10 COMPLETE card for the main quest that ends on the credits, and every earlier chapter's card still comes up: chapter 2's on its homecoming, chapters 3 to 9
        /// on their own flags. The watch is first shown to see a chapter-10 card when one is made (a chapter in memory with a flag of its own). Chapter 1 never ended on a card.</summary>
        [UnityTest] public IEnumerator ChapterTenEndsOnTheCreditsAndEveryEarlierChapterStillOnItsCard()
        {
            GameServices.Current.NewGame();DevLoadout.Apply("starter");
            var later=ChapterCards.Quests.Where(q=>GameCatalog.Find<QuestDef>(q.id)==q).ToArray();
            foreach(var quest in later){State.questSteps.Remove(quest.id);if(!string.IsNullOrWhiteSpace(quest.cardFlag))State.flags.Remove(quest.cardFlag);}
            var last=GameCatalog.Find<QuestDef>("TurnBack");Assert.IsNotNull(last,"fixture: the main quest");
            Assert.IsTrue(ChapterCards.EndsOnCredits(last),"the last chapter's quest doesn't end on the credits");
            var others=GameCatalog.All<QuestDef>().Where(q=>q.main&&q!=last&&ChapterCards.EndsOnCredits(q)).Select(q=>q.id).ToArray();
            Assert.IsEmpty(others,"another chapter ends on the credits: "+string.Join(", ",others));
            SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+25;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            yield return Seconds(.6f);Assert.IsNotNull(Timer,"fixture: the card's own timer has moved");
            var seen=new List<string>();var faults=new List<string>();
            Flag("chapter2.homecoming");yield return Read(seen,1.2f);
            if(!seen.Contains("CHAPTER 2 COMPLETE"))faults.Add("chapter 2 no longer ends on its card: "+string.Join(", ",seen));
            foreach(var quest in later.Where(q=>!ChapterCards.EndsOnCredits(q)))
            {
                State.questSteps[quest.id]=0;seen.Clear();yield return Read(seen,.6f);
                State.questSteps[quest.id]=quest.steps.Length;if(!string.IsNullOrWhiteSpace(quest.cardFlag))Flag(quest.cardFlag);
                seen.Clear();yield return Read(seen,1.2f);
                if(!seen.Contains(ChapterCards.Closing(quest)))faults.Add($"chapter {quest.chapter} ({quest.id}) no longer ends on its card: "+string.Join(", ",seen));
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
            // The watch sees a chapter-10 card when there is one: a chapter 10 in memory, ending on a flag of its own.
            var berth=ScriptableObject.CreateInstance<QuestDef>();berth.name=berth.id="TestLastBerth";berth.title="The Last Berth";berth.description="A chapter built in memory for CreditsTests.";
            berth.main=true;berth.chapter=10;berth.cardFlag="test.lastBerth";berth.steps=new[]{new Objective{kind=ObjectiveKind.Flag,target="test.lastBerth",count=1}};berth.stepText=new[]{"Do the last thing"};
            made.Add((berth,ChapterCards.Follow(berth)));
            State.questSteps[berth.id]=0;seen.Clear();yield return Read(seen,.6f);
            State.questSteps[berth.id]=1;Flag("test.lastBerth");seen.Clear();yield return Read(seen,1.2f);
            Assert.Contains("CHAPTER 10 COMPLETE",seen,"fixture: a chapter-10 card made on purpose isn't seen, so seeing none below proves nothing");
            // The game's own chapter 10: begun on its card, ended on the credits and no card.
            State.questSteps[last.id]=0;seen.Clear();yield return Read(seen,.8f);
            Assert.Contains(ChapterCards.Opening(last),seen,"chapter 10 no longer opens on its card");
            State.questSteps[last.id]=last.steps.Length;Flag(Credits.Flag);seen.Clear();yield return Read(seen,3);
            Assert.IsFalse(seen.Any(h=>h.StartsWith("CHAPTER")),"the last chapter ends on a card: "+string.Join(", ",seen));
        }

        // ------------------------------------------------------------------ what the roll says, and how it reads
        /// <summary>Every piece of type in the roll stands inside its box and inside a 1920-wide screen, at couch sizes, measured from the letters (as GameHud.StandState's test measures the
        /// boss bar's state). The measure is first shown to catch a line too long for its box. It reads at a pace: a line crosses the screen in fifteen seconds or more, each opening line holds
        /// for five, the last line stands alone. It says the two lines, the manifest's headings in order, the player's line and the last line, carries every name and licence docs/CREDITS.md
        /// owes and every track docs/music/tracks.json lists (each staged), and prints no id.</summary>
        [UnityTest] public IEnumerator EveryLineOfTheRollReadsInsideItsBoxAtCouchSizesAndCarriesEveryCredit()
        {
            Told(true);yield return Load();var roll=Roll;
            yield return Until(()=>roll.Running,6);Assert.IsTrue(roll.Running,"fixture: the roll");
            var canvas=(RectTransform)roll.Roll.GetComponentInParent<Canvas>().transform;
            // The measure catches a line too long for its box.
            var probe=Word("CreditsName");string kept=probe.text;probe.text=new string('W',90);
            Assert.IsNotNull(Fault(probe,canvas),"fixture: the measure lets a line of ninety capitals through a 1200-wide box");probe.text=kept;probe.ForceMeshUpdate();
            var faults=roll.Words.Where(t=>t.name!="CreditsHint").Select(t=>Fault(t,canvas)).Where(f=>f!=null).ToList();
            Assert.IsEmpty(faults,string.Join("\n",faults));
            foreach(var t in roll.Words.Where(t=>t.name=="CreditsHeading"))Assert.GreaterOrEqual(t.fontSize,40,"a heading is too small to read from a couch");
            foreach(var t in roll.Words.Where(t=>t.name=="CreditsName"||t.name=="CreditsFirstLine"||t.name=="CreditsLastLine"))Assert.GreaterOrEqual(t.fontSize,36,t.text+" is too small to read from a couch");
            // A reading pace.
            Assert.GreaterOrEqual(1080/Credits.Speed,15,"a line crosses the screen in under fifteen seconds");
            Assert.GreaterOrEqual(Credits.LinesGo-Credits.LineTwoAt,5,"the second line doesn't hold long enough to read");Assert.GreaterOrEqual(Credits.LastLineHold,4);
            Assert.Less(roll.TitleEnd,240,"the credits run past four minutes at their own pace");
            // The last line alone on the screen.
            yield return At(roll.LastLineAt+2);
            Assert.AreEqual(1,Word("CreditsLastLine").alpha,.01f,"the last line isn't up");
            Assert.IsTrue(Word("CreditsFirstLine").alpha<.01f&&Word("CreditsSecondLine").alpha<.01f&&roll.Roll.GetComponent<CanvasGroup>().alpha<.01f&&GameObject.Find("CreditsWordmark").GetComponent<RawImage>().color.a<.01f,
                "the last line shares the screen with something");
            // What it says, in order.
            var text=string.Join("\n",roll.Words.Select(t=>t.text));
            var headings=roll.Words.Where(t=>t.name=="CreditsHeading").Select(t=>t.text).ToArray();
            string[] manifest={"WHO DREW THE CHART","WHO SAID IT AT THE DOORS","WHO BUILT THE STREETS","WHO PAINTED THE LINE","WHO HUMMED","WHO KEPT THE LAMPS","WHO COUNTED TWICE","WHAT WE BORROWED, WITH THANKS","WHO HELD THE LADDER","AND WHOEVER WALKED IT THIS FAR"};
            CollectionAssert.IsSubsetOf(headings,manifest,"a heading isn't one of the manifest's");
            CollectionAssert.AreEqual(manifest.Where(headings.Contains).ToArray(),headings,"the headings aren't in the manifest's order");
            Assert.AreEqual(CreditsRoll.PlayerHeading,headings.Last(),"the roll doesn't end on the player's own heading");
            foreach(var line in new[]{CreditsRoll.FirstLine,CreditsRoll.SecondLine,CreditsRoll.PlayerLine,CreditsRoll.LastLine})StringAssert.Contains(line,text);
            // Every name and licence docs/CREDITS.md owes, each looked for in it first so this list says nothing it doesn't.
            string credits=File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../docs/CREDITS.md")));
            foreach(var owed in new[]{"Nathan","Meshy","Unity","Liberation Sans","Yarn Spinner","MIT","Kenney","Sci-Fi Sounds","Impact Sounds","Interface Sounds","RPG Audio","Particle Pack","CC0",
                "Real-ESRGAN","Xintao Wang","BSD-3","Quaternius","Ultimate Animated Animals","Zig","MinGW","Frostbound","SIL Open Font License"})
            {
                Assert.IsTrue(credits.IndexOf(owed,System.StringComparison.OrdinalIgnoreCase)>=0,$"fixture: '{owed}' isn't in docs/CREDITS.md");
                string said=owed=="BSD-3"?"BSD 3-Clause":owed;
                Assert.IsTrue(text.IndexOf(said,System.StringComparison.OrdinalIgnoreCase)>=0,$"the roll doesn't carry '{owed}', which docs/CREDITS.md owes");
            }
            // Every track by its maker's title, each one the game can play.
            var tracks=System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../docs/music/tracks.json"))),"\"title\":\\s*\"([^\"]+)\"").Select(m=>m.Groups[1].Value).ToArray();
            Assert.Greater(tracks.Length,20,"fixture: docs/music/tracks.json");
            var listed=roll.Words.Where(t=>t.name=="CreditsTrack").Select(t=>t.text).ToArray();
            // Each as the roll shows it (CreditsRoll.Shown: two files came with a slip in their names).
            CollectionAssert.AreEquivalent(tracks.Select(CreditsRoll.Shown).ToArray(),listed,"the roll's tracks aren't docs/music/tracks.json's");
            // The game plays each by its file's name, which for two tracks is not the title the roll shows.
            foreach(var title in listed)
            {
                string file=CreditsRoll.Tracks.FirstOrDefault(t=>CreditsRoll.Shown(t)==title);Assert.IsNotNull(file,title+" is on the roll and is no track of the game's");
                Assert.IsNotNull(Resources.Load<AudioClip>("Audio/Music/"+file),title+" is on the roll and the game can't play it");
            }
            Resources.UnloadUnusedAssets();
            // No id: no flag, scene, quest or speaker key anywhere in it.
            var ids=new[]{Credits.Flag,Credits.Rolled,Town,"TurnBack","LastSurvey","Homecomings","Survivor","Keeper"}.Concat(GameCatalog.All<ZoneDef>().Select(z=>z.name)).Where(i=>i.Length>5&&i!="Nathan").ToArray();
            foreach(var id in ids)Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(text,"\\b"+System.Text.RegularExpressions.Regex.Escape(id)+"\\b"),"the roll prints an id: "+id);
        }

        /// <summary>Every screen of the roll, kept as a still (chapter 10's look pass, November 21: the whole-game capture took none while the credits ran, its route holding them in one
        /// step whose still came after they had ended), from the two lines through every section to the last line and the title, a screen's height of the roll apart with a fifth over. And
        /// every line stands centred in its column: a heading, its role, a name and its detail on the column's middle, each track title on the middle of its half, measured from the letters.
        /// The measure is first shown to catch a line set off its middle.</summary>
        [UnityTest] public IEnumerator EveryScreenOfTheRollIsKeptAsAStillAndEveryLineStandsCentredInItsColumn()
        {
            Told(true);yield return Load();var roll=Roll;
            yield return Until(()=>roll.Running,6);Assert.IsTrue(roll.Running,"fixture: the roll");
            var canvas=(RectTransform)roll.Roll.GetComponentInParent<Canvas>().transform;
            float Off(TMP_Text t){var (ink,_)=Measure(t,canvas);float middle=t.name=="CreditsTrack"?Mathf.Sign(t.rectTransform.anchoredPosition.x)*290:0;return ink.center.x-middle;}
            var lines=roll.Words.Where(t=>t.name!="CreditsHint"&&!string.IsNullOrEmpty(t.text)).ToList();
            // The measure catches a line set forty units off its middle.
            var probe=Word("CreditsHeading");var home=probe.rectTransform.anchoredPosition;probe.rectTransform.anchoredPosition=home+new Vector2(40,0);Canvas.ForceUpdateCanvases();
            Assert.Greater(Mathf.Abs(Off(probe)),20,"fixture: the measure lets a heading forty units off its middle through");probe.rectTransform.anchoredPosition=home;Canvas.ForceUpdateCanvases();
            var off=lines.Select(t=>(t,d:Off(t))).Where(x=>Mathf.Abs(x.d)>4).Select(x=>$"'{x.t.text}' ({x.t.name}) stands {x.d:0.0} off its middle").ToList();
            Assert.IsEmpty(off,string.Join("\n",off));
            // The tracks read down each column in the roll's own order, the left column first (set row by row, each column skipped every other title). Shown failing first on the
            // same titles laid row by row.
            static string Order(IEnumerable<(string title,float x,float y)> laid,string[] own)
            {
                var read=laid.OrderBy(l=>l.x<0?0:1).ThenBy(l=>-l.y).Select(l=>l.title).ToArray();
                return read.SequenceEqual(own)?null:"the tracks read down their columns as "+string.Join(", ",read.Take(6))+"…, not in order";
            }
            // The roll lists each track by the title that was meant: two files came with a slip in their names, which the game still asks for them by.
            var own=CreditsRoll.Titles;
            Assert.AreEqual(CreditsRoll.Tracks.Length,own.Length,"a track is missing from the roll's titles");
            CollectionAssert.Contains(CreditsRoll.Tracks,"Dventure at Dusk","fixture: the file's own name is the track's id");CollectionAssert.Contains(CreditsRoll.Tracks,"Glass Ruans","fixture: the file's own name is the track's id");
            CollectionAssert.Contains(own,"Adventure at Dusk");CollectionAssert.Contains(own,"Glass Ruins");
            foreach(string slip in new[]{"Dventure at Dusk","Glass Ruans"})CollectionAssert.DoesNotContain(own,slip,"the roll shows a file's slip as a track's title");
            Assert.IsNotNull(Order(own.Select((title,i)=>(title,(i%2==0?-1f:1f)*290,-(float)(i/2)*40)),own),"fixture: the order check lets titles laid row by row through");
            Assert.IsNull(Order(roll.Words.Where(t=>t.name=="CreditsTrack").Select(t=>(t.text,t.rectTransform.anchoredPosition.x,t.rectTransform.anchoredPosition.y)),own));
            // A detail too wide for one line breaks at one of its dots, never wherever its width runs out: shown failing first on a long line with no break of its own.
            static string Wrapped(TMP_Text t){t.ForceMeshUpdate();int lines=t.textInfo.lineCount,own=t.text.Count(c=>c=='\n')+1;return lines>own?$"'{t.text}' wraps where its width runs out ({lines} lines, {own} of its own)":null;}
            var detail=Word("CreditsDetail");string kept=detail.text;detail.text=string.Join(" and ",Enumerable.Repeat("a licence with a long name",8));
            Assert.IsNotNull(Wrapped(detail),"fixture: the wrap check lets a line wider than its box through");detail.text=kept;
            var wrapped=roll.Words.Where(t=>t.name=="CreditsDetail").Select(Wrapped).Where(f=>f!=null).ToList();
            Assert.IsEmpty(wrapped,string.Join("\n",wrapped));
            // The stills: the opening, a screen at a time through the roll, the last section held, the last line, the title.
            float half=Mathf.Max(540,canvas.rect.height*.5f);float screen=canvas.rect.height/Credits.Speed*.8f;int n=0;
            yield return At(Credits.LinesGo-.5f);Still("credits-roll-"+(n++).ToString("D2")+"-lines");
            for(float t=Credits.RollAt+half/Credits.Speed;t<roll.RollEnd+Credits.LastHold;t+=screen){yield return At(t);Still("credits-roll-"+(n++).ToString("D2"));}
            yield return At(roll.RollEnd+Credits.LastHold*.8f);Still("credits-roll-"+(n++).ToString("D2")+"-held");
            yield return At(roll.LastLineAt+2);Still("credits-roll-"+(n++).ToString("D2")+"-last");
            yield return At(roll.TitleAt+3);Still("credits-roll-"+(n++).ToString("D2")+"-title");
            TestContext.WriteLine($"CREDITS_SCREENS {n} roll {Credits.RollAt:0.0}..{roll.RollEnd:0.0} s, title at {roll.TitleAt:0.0}, end {roll.TitleEnd:0.0}");
        }

        // ------------------------------------------------------------------ the title
        static readonly FieldInfo SavesField=typeof(GameServices).GetField("<Saves>k__BackingField",BindingFlags.NonPublic|BindingFlags.Instance);
        SaveSystem savesSwapped;
        SaveSystem Saves{get=>GameServices.Current.Saves;set=>SavesField.SetValue(GameServices.Current,value);}
        /// <summary>A title of its own, made after the saves were written, from a zone: _Boot puts one up on the runner's own saves before the test starts, outside SceneFlow, and a
        /// second loaded beside it is never the first one found.</summary>
        static IEnumerator Title()
        {
            var before=Object.FindFirstObjectByType<TitleScreen>();
            SceneFlow.Current.LoadZone("Title");float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null||Object.FindFirstObjectByType<TitleScreen>()==before)&&Time.unscaledTime<deadline)yield return null;
            yield return Frames(2);var title=Object.FindFirstObjectByType<TitleScreen>();Assert.IsTrue(title!=null&&title!=before,"fixture: the title never came up afresh");
            Assert.AreEqual(1,Object.FindObjectsByType<TitleScreen>(FindObjectsSortMode.None).Length,"fixture: two titles are up");
        }
        static TMP_Text Subtitle=>Object.FindFirstObjectByType<TitleScreen>().GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Subtitle");
        /// <summary>The title says the game has been played to its end when a save there has (any of the four), in the closing line's words, and a fresh game's title says what it always did;
        /// either fits its box.</summary>
        [UnityTest] public IEnumerator TheTitleKnowsAFinishedGameAndAFreshOneDoesNot()
        {
            Assert.IsNotNull(SavesField,"fixture: GameServices keeps its saves somewhere else now");
            savesSwapped=Saves;var saves=new SaveSystem(Path.Combine(Application.temporaryCachePath,"Credits-"+System.Guid.NewGuid().ToString("N")));Saves=saves;
            Told(false);saves.Save("autosave",State);
            yield return Load("Arena_Ground");yield return Title();var title=Object.FindFirstObjectByType<TitleScreen>();
            Assert.IsFalse(title.Finished,"a game not yet finished reads as finished");Assert.AreEqual("ONE BODY. THREE FORMS.",Subtitle.text,"a fresh game's title lost its own line");
            var canvas=(RectTransform)Subtitle.canvas.transform;Assert.IsNull(Fault(Subtitle,canvas));
            // Finished in a slot only, the autosave a new game since.
            var over=new GameState();foreach(var kv in State.flags)over.flags[kv.Key]=kv.Value;foreach(var kv in State.questSteps)over.questSteps[kv.Key]=kv.Value;over.party=State.party;over.zone=Town;over.spawn="Arrival";
            over.flags[Credits.Flag]=true;over.flags[Credits.Rolled]=true;saves.Save("slot2",over);
            yield return Title();title=Object.FindFirstObjectByType<TitleScreen>();
            Assert.IsTrue(title.Finished,"a finished game in a slot doesn't show on the title");Assert.AreEqual(Credits.TitleLine,Subtitle.text,"the title doesn't say the game has been played to its end");
            Assert.AreEqual(UiKit.GoldColor,Subtitle.color);canvas=(RectTransform)Subtitle.canvas.transform;Assert.IsNull(Fault(Subtitle,canvas));
        }
    }
}
