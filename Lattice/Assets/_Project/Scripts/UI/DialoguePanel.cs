using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Lattice.UI
{
    public sealed class DialoguePanel:MonoBehaviour,IDialogueView
    {
        /// <summary>Choices stand in a column to the right: across the middle of the screen they lay on the people talking. The longest choice in the script is 617 wide at the button's 34.</summary>
        const float ChoiceX=560,ChoiceWidth=720;
        Canvas canvas;
        GameObject panel,backing,options;
        TMP_Text body,speaker,hint;
        Image portrait;
        bool clicked;
        int shownFrame,lastQueuedFrame=-1,optionsFrame;
        FirstPress first;
        public bool IsVisible=>panel!=null&&panel.activeInHierarchy;
        public bool OptionsVisible=>options!=null&&options.activeInHierarchy;
        public bool AdvanceRequested
        {
            get{if(Time.frameCount<=shownFrame||!clicked)return false;clicked=false;return true;}
        }
        void Start()
        {
            canvas=UiKit.CreateCanvas("Dialogue",25,transform);
            // The sheet's opening is 254 of 255 solid and the canvas blends in linear, so on a plate this dark a bright floor sign read through it as ghost letters. Solid under the opening, and short of the frame's cut corners.
            var under=UiKit.Panel(canvas.transform,"DialogueBacking",UiKit.PlateColor);under.raycastTarget=false;backing=under.gameObject;UiKit.Rect(backing,new(.5f,0),new(.5f,0),new(0,205),new(1712,282));
            panel=UiKit.DarkFrame(canvas.transform,"DialoguePanel").gameObject;
            UiKit.Rect(panel,new(.5f,0),new(.5f,0),new(0,205),new(1760,330));
            var click=panel.AddComponent<Button>();click.onClick.AddListener(QueueAdvance);
            // Its sides fade rather than stop: a straight cut down a shoulder read as a pasted rectangle on every line.
            portrait=UiKit.Bust(panel.transform,"Portrait",Vector2.zero,new(34,30),360);
            speaker=UiKit.Heading(panel.transform,"Speaker","",32,UiKit.GoldColor,TextAlignmentOptions.Left);UiKit.Rect(speaker.gameObject,new(0,1),new(0,1),new(980,-59),new(1280,55));
            body=UiKit.Text(panel.transform,"Body","",30,UiKit.TextColor,TextAlignmentOptions.TopLeft);UiKit.Rect(body.gameObject,new(0,.5f),new(0,.5f),new(980,-25),new(1280,185));
            hint=UiKit.Text(panel.transform,"Continue",DeviceHints.Continue(),25,UiKit.DimTextColor,TextAlignmentOptions.Right);UiKit.Rect(hint.gameObject,new(1,0),new(1,0),new(-300,68),new(470,40));
            DialogueSystem.Current.View=this;Up(false);PromptService.Changed+=RefreshHint;
        }
        void OnDestroy(){PromptService.Changed-=RefreshHint;first.End();}
        void RefreshHint(){if(hint!=null)hint.text=OptionsVisible?DeviceHints.Choose():DeviceHints.Continue();}
        void Update()
        {
            first.Step();
            if(panel==null||!panel.activeSelf||Time.frameCount<=shownFrame)return;
            bool submit=UiActions.Submit.WasPressedThisFrame(),interact=GameServices.Current.Input.Find("Interact").WasPressedThisFrame();
            if(!OptionsVisible){if(submit||interact)QueueAdvance();return;}
            // A choice isn't a line to advance. Submit reaches the selected button through the EventSystem; the world's Interact key has to be handed to it.
            if(first.Held||!interact||submit||Time.frameCount<=optionsFrame)return;
            var chosen=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
            if(chosen!=null&&chosen.transform.IsChildOf(options.transform)&&chosen.TryGetComponent<Button>(out var button))button.onClick.Invoke();
        }
        void QueueAdvance(){if(OptionsVisible||Time.frameCount<=shownFrame||lastQueuedFrame==Time.frameCount)return;lastQueuedFrame=Time.frameCount;clicked=true;UiSounds.Advance();}
        public void Show(string name,PortraitEmotion emotion,Sprite sprite,string text)
        {shownFrame=Time.frameCount;clicked=false;Up(true);RefreshHint();speaker.text=name;body.text=text;body.maxVisibleCharacters=0;portrait.sprite=sprite;portrait.gameObject.SetActive(sprite!=null);}
        public void SetVisibleCharacters(int count){body.maxVisibleCharacters=count;}
        public void Hide(){Up(false);ClearOptions();}
        void Up(bool on){panel.SetActive(on);backing.SetActive(on);}
        public void Options(IReadOnlyList<string> labels,System.Action<int> selected)
        {
            ClearOptions();options=new GameObject("Options",typeof(RectTransform));options.transform.SetParent(canvas.transform,false);var buttons=new List<Selectable>();
            for(int i=0;i<labels.Count;i++){int n=i;var b=UiKit.Button(options.transform,"Option_"+i,labels[i],()=>selected(n));UiKit.Rect(b.gameObject,new(.5f,.5f),new(.5f,.5f),new(ChoiceX,100-i*85),new(ChoiceWidth,65));buttons.Add(b);}
            UiKit.LinkVertical(buttons.ToArray());EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);optionsFrame=Time.frameCount;
            // A thumb tapping through the lines answered the top row five frames after it appeared: Hal's bench unasked, or a paid repair.
            first.Begin();RefreshHint();
        }
        public void ClearOptions(){if(options!=null)Destroy(options);options=null;first.End();RefreshHint();}
    }
}
