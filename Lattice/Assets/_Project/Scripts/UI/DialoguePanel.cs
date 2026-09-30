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
        Canvas canvas;
        GameObject panel,options;
        TMP_Text body,speaker;
        Image portrait;
        bool clicked;
        int shownFrame,lastQueuedFrame=-1;
        public bool IsVisible=>panel!=null&&panel.activeInHierarchy;
        public bool AdvanceRequested
        {
            get{if(Time.frameCount<=shownFrame||!clicked)return false;clicked=false;return true;}
        }
        void Start()
        {
            canvas=UiKit.CreateCanvas("Dialogue",25,transform);
            panel=UiKit.DarkFrame(canvas.transform,"DialoguePanel").gameObject;
            UiKit.Rect(panel,new(.5f,0),new(.5f,0),new(0,205),new(1760,330));
            var click=panel.AddComponent<Button>();click.onClick.AddListener(QueueAdvance);
            portrait=UiKit.Panel(panel.transform,"Portrait",Color.white);UiKit.Rect(portrait.gameObject,new(0,.5f),new(0,.5f),new(170,60),new(300,390));portrait.preserveAspect=true;portrait.raycastTarget=false;
            speaker=UiKit.Heading(panel.transform,"Speaker","",32,UiKit.GoldColor,TextAlignmentOptions.Left);UiKit.Rect(speaker.gameObject,new(0,1),new(0,1),new(980,-55),new(1280,55));
            body=UiKit.Text(panel.transform,"Body","",30,UiKit.TextColor,TextAlignmentOptions.TopLeft);UiKit.Rect(body.gameObject,new(0,.5f),new(0,.5f),new(980,-25),new(1280,185));
            var hint=UiKit.Text(panel.transform,"Continue","A / E / ENTER   CONTINUE",25,UiKit.DimTextColor,TextAlignmentOptions.Right);UiKit.Rect(hint.gameObject,new(1,0),new(1,0),new(-300,68),new(470,40));
            DialogueSystem.Current.View=this;panel.SetActive(false);
        }
        void Update()
        {
            if(panel!=null&&panel.activeSelf&&Time.frameCount>shownFrame&&(UiActions.Submit.WasPressedThisFrame()||GameServices.Current.Input.Find("Interact").WasPressedThisFrame()))QueueAdvance();
        }
        void QueueAdvance(){if(Time.frameCount<=shownFrame||lastQueuedFrame==Time.frameCount)return;lastQueuedFrame=Time.frameCount;clicked=true;UiSounds.Advance();}
        public void Show(string name,PortraitEmotion emotion,Sprite sprite,string text)
        {shownFrame=Time.frameCount;clicked=false;panel.SetActive(true);speaker.text=name;body.text=text;body.maxVisibleCharacters=0;portrait.sprite=sprite;portrait.gameObject.SetActive(sprite!=null);}
        public void SetVisibleCharacters(int count){body.maxVisibleCharacters=count;}
        public void Hide(){panel.SetActive(false);ClearOptions();}
        public void Options(IReadOnlyList<string> labels,System.Action<int> selected)
        {
            ClearOptions();options=new GameObject("Options",typeof(RectTransform));options.transform.SetParent(canvas.transform,false);var buttons=new List<Selectable>();
            for(int i=0;i<labels.Count;i++){int n=i;var b=UiKit.Button(options.transform,"Option_"+i,labels[i],()=>selected(n));UiKit.Rect(b.gameObject,new(.5f,.5f),new(.5f,.5f),new(200,100-i*85),new(920,65));buttons.Add(b);}
            UiKit.LinkVertical(buttons.ToArray());EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
        }
        public void ClearOptions(){if(options!=null)Destroy(options);options=null;}
    }
}
