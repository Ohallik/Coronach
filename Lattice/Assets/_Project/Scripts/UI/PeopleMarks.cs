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
        static readonly HashSet<string> Repeats=new(){"AbriLanternRepeat","AbriLanternWaiting","AbriLanternAfter","PellLampAfter"};
        const float MarkPixels=30;
        sealed class Label{public TextMeshPro text;public string name;public Color color;public float size;public bool marked;}
        readonly Dictionary<Npc,Label> labels=new();
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
                label.marked=news;label.text.text=news?"! "+label.name:label.name;label.text.color=news?UiKit.GoldColor:label.color;
                label.text.fontSize=news?Mathf.Max(label.size,UiKit.WorldFontSize(MarkPixels)):label.size;
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
            var label=found!=null?new Label{text=found,name=found.text,color=found.color,size=found.fontSize}:null;
            labels[npc]=label;return label;
        }
        static bool Shown(TextMeshPro text){var view=text.GetComponent<Renderer>();return view!=null&&view.enabled;}
    }
}
