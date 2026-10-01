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
        GameObject settings,savePicker,mainMenu;
        Button settingsButton;
        public bool SettingsOpen=>settings!=null;
        TMPro.TMP_Text hint;
        void RefreshHint(){if(hint!=null)hint.text=DeviceHints.TitleMenu();}
        void OnDestroy(){PromptService.Changed-=RefreshHint;}
        void Start()
        {
            PromptService.Changed+=RefreshHint;
            MusicDirector.SetLocation("Title",false);
            canvas=UiKit.CreateCanvas("TitleCanvas",10,transform);
            var background=new GameObject("KeyArt",typeof(RawImage)); background.transform.SetParent(canvas.transform,false);
            background.GetComponent<RawImage>().texture=Resources.Load<Texture2D>("UI/Title/key-art");
            UiKit.Rect(background,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).sizeDelta=Vector2.zero;
            mainMenu=new GameObject("MainMenu",typeof(RectTransform));mainMenu.transform.SetParent(canvas.transform,false);
            UiKit.Rect(mainMenu,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).sizeDelta=Vector2.zero;
            var title=UiKit.Heading(mainMenu.transform,"Title",Application.productName.ToUpperInvariant(),105,UiKit.TextColor,TextAlignmentOptions.Left);
            UiKit.Rect(title.gameObject,new Vector2(0,1),new Vector2(0,1),new Vector2(470,-235),new Vector2(760,150));
            var subtitle=UiKit.Text(mainMenu.transform,"Subtitle","ONE BODY. THREE FORMS.",24,new Color(.58f,.85f,.9f),TextAlignmentOptions.Left);
            UiKit.Rect(subtitle.gameObject,new Vector2(0,1),new Vector2(0,1),new Vector2(370,-345),new Vector2(540,55));
            var buttons=new List<Selectable>();
            var play=MenuButton("New Game",0,()=>StartGame(false)); buttons.Add(play);
            var cont=MenuButton("Continue",1,OpenSaves);
            cont.interactable=System.Array.Exists(new[]{"autosave","slot1","slot2","slot3"},GameServices.Current.Saves.Exists); if(cont.interactable)buttons.Add(cont);
            settingsButton=MenuButton("Settings",2,OpenSettings);buttons.Add(settingsButton);
            buttons.Add(MenuButton("Quit",3,Application.Quit)); UiKit.LinkVertical(buttons.ToArray());
            hint=UiKit.Text(mainMenu.transform,"Hint",DeviceHints.TitleMenu(),24,UiKit.DimTextColor,TextAlignmentOptions.Left);
            UiKit.Rect(hint.gameObject,new Vector2(0,0),new Vector2(0,0),new Vector2(510,85),new Vector2(840,50));
            EventSystem.current.SetSelectedGameObject(play.gameObject);
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
        void Update(){if(UiActions.Cancel.WasPressedThisFrame()){if(settings!=null||savePicker!=null)UiSounds.Cancel();if(settings!=null)CloseSettings();if(savePicker!=null){Destroy(savePicker);savePicker=null;}}}
        public void StartGame(bool resume)
        {
            if(resume) GameServices.Current.State=GameServices.Current.Saves.Load("autosave")??new GameState();
            else GameServices.Current.NewGame();
            SceneFlow.Current.LoadZone(GameServices.Current.State.zone,GameServices.Current.State.spawn);
            canvas.gameObject.SetActive(false);
        }
        void OpenSaves()
        {
            savePicker=UiKit.Dim(canvas.transform,.96f).gameObject;var buttons=new List<Selectable>();int row=0;
            var heading=UiKit.Heading(savePicker.transform,"LoadTitle","CONTINUE YOUR ROUTE",45,UiKit.TextColor);UiKit.Rect(heading.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,330),new(1200,80));
            foreach(string slot in new[]{"autosave","slot1","slot2","slot3"})
            {
                var state=GameServices.Current.Saves.Load(slot);string id=slot;
                var button=UiKit.Button(savePicker.transform,slot,state==null?slot+" — Empty":slot+" — "+state.zone.Replace('_',' ')+" — Sync "+state.party[0].level,()=>
                {var loaded=GameServices.Current.Saves.Load(id);if(loaded==null)return;GameServices.Current.State=loaded;SceneFlow.Current.LoadZone(loaded.zone,loaded.spawn);canvas.gameObject.SetActive(false);});
                button.interactable=state!=null;UiKit.Rect(button.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,180-row++*100),new(1200,72));if(button.interactable)buttons.Add(button);
            }
            var back=UiKit.Button(savePicker.transform,"Back","B  Back",()=>{UiSounds.Cancel();Destroy(savePicker);savePicker=null;});UiKit.Rect(back.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,-280),new(400,65));buttons.Add(back);UiKit.LinkVertical(buttons.ToArray());EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
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
    }
}
