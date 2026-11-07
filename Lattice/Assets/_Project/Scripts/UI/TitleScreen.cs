using System.Collections;
using System.Collections.Generic;
using System.IO;
using Lattice.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lattice.UI
{
    public sealed class TitleScreen:MonoBehaviour
    {
        Canvas canvas;
        GameObject settings,savePicker,mainMenu,newGameQuestion;
        Button settingsButton,continueButton,playButton;
        public bool SettingsOpen=>settings!=null;
        public bool SavesOpen=>savePicker!=null;
        public bool NewGameQuestionOpen=>newGameQuestion!=null;
        TMPro.TMP_Text hint;
        void RefreshHint(){if(hint!=null)hint.text=DeviceHints.TitleMenu();}
        void OnDestroy(){PromptService.Changed-=RefreshHint;}
        void Start()
        {
            PromptService.Changed+=RefreshHint;PromptService.ReadConnectedPad();
            MusicDirector.SetLocation("Title",false);
            canvas=UiKit.CreateCanvas("TitleCanvas",10,transform);
            var background=new GameObject("KeyArt",typeof(RawImage)); background.transform.SetParent(canvas.transform,false);
            background.GetComponent<RawImage>().texture=Resources.Load<Texture2D>("UI/Title/key-art");
            UiKit.Rect(background,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).sizeDelta=Vector2.zero;
            mainMenu=new GameObject("MainMenu",typeof(RectTransform));mainMenu.transform.SetParent(canvas.transform,false);
            UiKit.Rect(mainMenu,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).sizeDelta=Vector2.zero;
            // The designed wordmark: a display face's capitals alone read as a heading, not a logo. Its width is its art's, at the art's own proportions.
            var title=new GameObject("Title",typeof(RawImage));title.transform.SetParent(mainMenu.transform,false);
            var wordmark=title.GetComponent<RawImage>();wordmark.texture=Resources.Load<Texture2D>("UI/Title/wordmark");wordmark.raycastTarget=false;
            float wordmarkHeight=wordmark.texture!=null?800f*wordmark.texture.height/wordmark.texture.width:156;
            UiKit.Rect(title,new Vector2(0,1),new Vector2(0,1),new Vector2(470,-215),new Vector2(800,wordmarkHeight));
            var subtitle=UiKit.Text(mainMenu.transform,"Subtitle","ONE BODY. THREE FORMS.",24,new Color(.58f,.85f,.9f),TextAlignmentOptions.Left);
            UiKit.Rect(subtitle.gameObject,new Vector2(0,1),new Vector2(0,1),new Vector2(370,-345),new Vector2(540,55));
            var buttons=new List<Selectable>();
            playButton=MenuButton("New Game",0,AskNewGame); buttons.Add(playButton);
            var cont=MenuButton("Continue",1,OpenSaves);continueButton=cont;
            cont.interactable=System.Array.Exists(new[]{"autosave","slot1","slot2","slot3"},GameServices.Current.Saves.Exists); if(cont.interactable)buttons.Add(cont);
            settingsButton=MenuButton("Settings",2,OpenSettings);buttons.Add(settingsButton);
            buttons.Add(MenuButton("Quit",3,Application.Quit)); UiKit.LinkVertical(buttons.ToArray());
            hint=UiKit.Text(mainMenu.transform,"Hint",DeviceHints.TitleMenu(),24,UiKit.DimTextColor,TextAlignmentOptions.Left);
            UiKit.Rect(hint.gameObject,new Vector2(0,0),new Vector2(0,0),new Vector2(510,85),new Vector2(840,50));
            // A returning player's habitual press must continue, not start over.
            EventSystem.current.SetSelectedGameObject((cont.interactable?cont:playButton).gameObject);
            Debug.Log("TITLE_BOOT_OK");
            if(DevArgs.Has("-smoketest")&&!DevArgs.Has("-route")) StartCoroutine(Capture());
        }
        Button MenuButton(string name,int row,System.Action action)
        {
            var b=UiKit.Button(mainMenu.transform,name,name,action);
            UiKit.Rect(b.gameObject,new Vector2(0,1),new Vector2(0,1),new Vector2(330,-475-row*92),new Vector2(470,70));return b;
        }
        public void OpenSettings()
        {
            if(settings!=null)return;
            mainMenu.SetActive(false);
            settings=UiKit.Dim(canvas.transform,.97f).gameObject;
            var title=UiKit.Heading(settings.transform,"SettingsTitle","SETTINGS",48,UiKit.TextColor);
            UiKit.Rect(title.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,320),new(900,85));
            var controls=new List<Selectable>();
            AudioSettingsUi.Add(settings.transform,new Vector2(0,200),850,80,controls);
            var back=UiKit.Button(settings.transform,"Back","Back",()=>{UiSounds.Cancel();CloseSettings();});
            UiKit.Rect(back.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,-265),new(380,65));controls.Add(back);
            UiKit.LinkVertical(controls.ToArray());EventSystem.current.SetSelectedGameObject(controls[0].gameObject);
        }
        public void CloseSettings(){if(settings==null)return;AudioMix.Current.Flush();Destroy(settings);settings=null;mainMenu.SetActive(true);EventSystem.current.SetSelectedGameObject(settingsButton.gameObject);}
        void Update(){if(UiActions.Cancel.WasPressedThisFrame()){if(settings!=null||savePicker!=null)UiSounds.Cancel();if(settings!=null)CloseSettings();if(savePicker!=null)CloseSaves();if(newGameQuestion!=null)CloseNewGameQuestion();}}
        /// <summary>New Game replaces the autosave a moment after it starts, so with one on disk it asks first and defaults to no.</summary>
        void AskNewGame()
        {
            if(SceneFlow.Current.Loading)return;
            var autosave=GameServices.Current.Saves.Load("autosave");
            if(autosave==null){StartGame(false);return;}
            mainMenu.SetActive(false);newGameQuestion=UiKit.Dim(canvas.transform,.96f).gameObject;
            var heading=UiKit.Heading(newGameQuestion.transform,"NewGameTitle","START A NEW GAME?",45,UiKit.TextColor);UiKit.Rect(heading.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,190),new(1300,80));
            int level=autosave.party.Count>0?autosave.party[0].level:1;
            var body=UiKit.Text(newGameQuestion.transform,"NewGameBody",$"This replaces your autosave ({DisplayNames.Zone(autosave.zone)}, Sync {level}). Saved slots are kept.",30,UiKit.TextColor);UiKit.Rect(body.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,90),new(1300,90));
            var keep=UiKit.Button(newGameQuestion.transform,"KeepSave","Back",()=>{UiSounds.Cancel();CloseNewGameQuestion();});UiKit.Rect(keep.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,-30),new(520,70));
            var start=UiKit.Button(newGameQuestion.transform,"ConfirmNewGame","Start new game",()=>StartGame(false));UiKit.Rect(start.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,-120),new(520,70));
            UiKit.LinkVertical(keep,start);EventSystem.current.SetSelectedGameObject(keep.gameObject);
        }
        void CloseNewGameQuestion(){if(newGameQuestion==null)return;Destroy(newGameQuestion);newGameQuestion=null;mainMenu.SetActive(true);EventSystem.current.SetSelectedGameObject(playButton.gameObject);}
        void CloseSaves(){if(savePicker==null)return;Destroy(savePicker);savePicker=null;mainMenu.SetActive(true);EventSystem.current.SetSelectedGameObject(continueButton.gameObject);}
        public void StartGame(bool resume)
        {
            // The menu is live during its own fade-in, when a load is refused: replacing the game and hiding the menu here
            // left a black screen with no way out.
            if(SceneFlow.Current.Loading)return;
            GameServices.Current.Saves.ResumeFrom("autosave");
            if(resume) GameServices.Current.State=GameServices.Current.Saves.Load("autosave")??new GameState();
            else
            {
                GameServices.Current.NewGame();
                // Only a genuine New Game from the title plays the first town's morning (D195); Continue, older saves and
                // controlled fixtures never see it.
                GameServices.Current.Flags.SetBool(Lattice.World.OpeningMarketSequence.Requested,true);
            }
            // The menu stays up behind the fade; the Title scene's unload removes it.
            SceneFlow.Current.LoadZone(GameServices.Current.State.zone,GameServices.Current.State.spawn);
        }
        void OpenSaves()
        {
            mainMenu.SetActive(false);savePicker=UiKit.Dim(canvas.transform,.96f).gameObject;var buttons=new List<Selectable>();int row=0;
            // Where the player last was: a slot saved after the autosave is the newer one, and two presses of A loaded the older. Ties and dateless old saves keep the first row.
            Selectable newest=null;string newestAt=null;
            var heading=UiKit.Heading(savePicker.transform,"LoadTitle","CONTINUE YOUR ROUTE",45,UiKit.TextColor);UiKit.Rect(heading.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,330),new(1200,80));
            foreach(string slot in new[]{"autosave","slot1","slot2","slot3"})
            {
                var state=GameServices.Current.Saves.Load(slot);string id=slot;
                string empty=GameServices.Current.Saves.Damaged(slot)?" — Damaged":" — Empty";
                var button=UiKit.Button(savePicker.transform,slot,state==null?SlotName(slot)+empty:SlotName(slot)+" — "+DisplayNames.Zone(state.zone)+" — Sync "+state.party[0].level+" — "+Played(state.playtime)+SavedWhen(state),()=>
                {
                    if(SceneFlow.Current.Loading)return;
                    var loaded=GameServices.Current.Saves.Load(id);if(loaded==null)return;GameServices.Current.State=loaded;GameServices.ArmAnEmptyParty(loaded);
                    GameServices.Current.Saves.ResumeFrom(id);
                    if(id=="autosave")SceneFlow.Current.LoadZone(loaded.zone,loaded.spawn);else SceneFlow.Current.LoadSavedZone(loaded.zone,loaded.spawn);
                });
                // With its date a row runs past its 1200 at the button's 34.
                button.GetComponentInChildren<TMP_Text>().fontSize=30;
                button.interactable=state!=null;UiKit.Rect(button.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,180-row++*100),new(1200,72));if(button.interactable)buttons.Add(button);
                if(state!=null&&(newest==null||string.CompareOrdinal(state.savedAtUtc,newestAt)>0)){newest=button;newestAt=state.savedAtUtc;}
            }
            var back=UiKit.Button(savePicker.transform,"Back","Back",()=>{UiSounds.Cancel();CloseSaves();});UiKit.Rect(back.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,-280),new(400,65));buttons.Add(back);UiKit.LinkVertical(buttons.ToArray());EventSystem.current.SetSelectedGameObject((newest!=null?newest:buttons[0]).gameObject);
        }
        IEnumerator Capture()
        {
            yield return new WaitForSecondsRealtime(3);
            string path=DevArgs.Value("-screenshot");
            if(!string.IsNullOrEmpty(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                ScreenCapture.CaptureScreenshot(path);
                yield return new WaitForSecondsRealtime(1);
                ScreenCapture.CaptureScreenshot(path);
                yield return new WaitForSecondsRealtime(1);
                Debug.Log("SCREENSHOT_OK "+path);
            }
            yield return new WaitForSecondsRealtime(6);
            Debug.Log("TITLE_SMOKE_OK"); Application.Quit();
        }
        static string SavedWhen(GameState state){string at=DisplayNames.SavedAt(state.savedAtUtc);return at==""?"":" — "+at;}
        static string SlotName(string slot)=>slot=="autosave"?"Autosave":"Slot "+slot.Substring(4);
        static string Played(float seconds){int minutes=Mathf.FloorToInt(seconds/60);return minutes<60?$"{minutes} min":$"{minutes/60} h {minutes%60} min";}
    }
}
