using System.Collections.Generic;
using Lattice.Core;
using Lattice.World;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>Someone with a new conversation waiting shows it: their name label turns gold and grows, to be seen across a square, and
    /// the prompt says so up close. Five quests are only ever offered on a second talk, and Corra's offer stands where a player expects her
    /// shop, with nothing to suggest either. World can't see TextMeshPro, so the mark lives here and World doesn't change.</summary>
    public sealed class PeopleMarks:MonoBehaviour
    {
        /// <summary>Beats that stay true after they've been heard: the person is only repeating themselves, and a mark on them would never clear.</summary>
        static readonly HashSet<string> Repeats=new(){"AbriLanternRepeat","AbriLanternWaiting","AbriLanternAfter","PellLampAfter","AumiRelaysLit","MiraRepeat"};
        /// <summary>How tall a name stands on a 1080p screen, and how tall when it's marked; and how far over the head it hangs, in metres.</summary>
        const float NamePixels=26,MarkPixels=32,OverHead=.32f;
        /// <summary>A heavy mark, not a tick: thin Cinzel's '!' read as a stray stroke before the name.</summary>
        const string Mark="<size=150%><b>!</b></size>  ";
        sealed class Label{public TextMeshPro text;public string name;public Color color;public float size,markSize;public bool marked;}
        readonly Dictionary<Npc,Label> labels=new();
        readonly List<Label> shown=new();
        float next;
        public static bool HasNews(Npc npc)
        {
            var service=GameServices.Current;
            if(npc==null||service==null||!string.IsNullOrEmpty(npc.holdNode))return false;
            string node=SideStories.NodeFor(npc.speaker,service.State);
            return node!=null&&!Repeats.Contains(node);
        }
        /// <summary>What the prompt adds up close. A hail reaches the same person as a talk.</summary>
        public static string PromptNote(InteractionPrompt prompt)
        {
            var npc=prompt as Npc;if(prompt is HailPoint hail)npc=hail.contact;
            return HasNews(npc)?"  ·  has something to say":"";
        }
        void Update()
        {
            if(Time.unscaledTime<next||GameServices.Current==null)return;next=Time.unscaledTime+.25f;
            foreach(var prompt in InteractionPrompt.Active)
            {
                if(prompt is not Npc npc)continue;
                var label=LabelOf(npc);if(label==null)continue;
                bool news=HasNews(npc);if(news==label.marked)continue;
                label.marked=news;label.text.text=news?Mark+label.name:label.name;label.text.color=news?UiKit.GoldColor:label.color;
                label.text.fontSize=news?label.markSize:label.size;
            }
        }
        /// <summary>A name reads level whichever way its person faces: as a child it turned with them, so CORRA stood on end.</summary>
        void LateUpdate()
        {
            var view=Camera.main;if(view==null)return;var facing=view.transform.rotation;
            for(int i=shown.Count-1;i>=0;i--)
            {
                var text=shown[i].text;
                if(text==null)shown.RemoveAt(i);else text.transform.rotation=facing;
            }
        }
        /// <summary>The label the camera can see: a hailed person's own is switched off with their body, and a sign named for them hangs beside their ship.</summary>
        Label LabelOf(Npc npc)
        {
            if(labels.TryGetValue(npc,out var known))return known;
            TextMeshPro found=null;
            foreach(var own in npc.GetComponentsInChildren<TextMeshPro>(true))if(Shown(own)){found=own;break;}
            if(found==null)
            {
                string sign="Sign_"+DisplayNames.Speaker(npc.speaker);
                foreach(var other in FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(other.name==sign&&Shown(other)){found=other;break;}
            }
            Label label=null;
            if(found!=null)
            {
                found.fontSharedMaterial=UiKit.LegibleMaterial(found.font);
                // A name goes with the signs: '! Keeper Abri' printed through the box, and 'ORRIN' was cut in half by the vitals.
                if(!found.TryGetComponent<SignFade>(out _))found.gameObject.AddComponent<SignFade>().bossOnly=true;
                float scale=Mathf.Max(.01f,found.transform.lossyScale.y),size=Mathf.Max(found.fontSize,UiKit.WorldFontSize(NamePixels)/scale);found.fontSize=size;
                if(found.transform.IsChildOf(npc.transform))OverTheHead(found,npc);
                label=new Label{text=found,name=found.text,color=found.color,size=size,markSize=Mathf.Max(size,UiKit.WorldFontSize(MarkPixels)/scale)};shown.Add(label);
            }
            labels[npc]=label;return label;
        }
        /// <summary>Over this body's own head. A fixed 2.8 m left a name floating against the wall behind a short person, and in the shoulder of a tall one.</summary>
        static void OverTheHead(TextMeshPro label,Npc npc)
        {
            float top=float.MinValue;
            foreach(var body in npc.GetComponentsInChildren<Renderer>())
                if(!body.transform.IsChildOf(label.transform)&&body is MeshRenderer or SkinnedMeshRenderer)top=Mathf.Max(top,body.bounds.max.y);
            if(top>float.MinValue){var at=npc.transform.position;label.transform.position=new Vector3(at.x,top+OverHead,at.z);}
        }
        static bool Shown(TextMeshPro text){var view=text.GetComponent<Renderer>();return view!=null&&view.enabled;}
    }
}
