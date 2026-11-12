using System.Collections;
using Lattice.Core;
using Lattice.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>Pays once per run, at the title, what each menu's first opening paid on its own frame (QA-034): the save picker was the first thing to use the
    /// display face and the first to put a date on the player's clock, and the bench on a Continue was the first to load the item sheet, so each first opening
    /// cost a 38 to 46 ms frame. One thing a frame, so nothing here is a hitch of its own, and what has to be drawn once is drawn off the screen, under the title.</summary>
    public static class UiWarmup
    {
        public static bool Done{get;private set;}
        /// <summary>How long what's drawn stays up: long enough to be drawn and to clear the frames in flight.</summary>
        const int DrawnFor=4;
        public static IEnumerator Run(Transform under)
        {
            if(Done)yield break;
            var canvas=UiKit.CreateCanvas("UiWarmup",-100,under);var root=canvas.transform;
            // The faces, plain and legible, laid out once: a font asset's tables are built on its first use.
            var display=UiKit.Heading(root,"WarmDisplay","WARM 0123",45,UiKit.TextColor);
            var body=UiKit.Text(root,"WarmBody","Warm 0123 · × —",26,UiKit.TextColor);
            var legible=UiKit.Legible(UiKit.Text(root,"WarmLegible","Warm",24,UiKit.LooseTextColor));
            var legibleDisplay=UiKit.Legible(UiKit.Heading(root,"WarmLegibleDisplay","WARM",30,UiKit.LooseTextColor));
            foreach(var text in new TMP_Text[]{display,body,legible,legibleDisplay}){Away(text.gameObject);text.ForceMeshUpdate();}
            yield return null;
            // The sheets every menu frames itself with, and the drawn ones.
            Away(UiKit.SolidFrame(root,"WarmFrame",new(.5f,.5f),Vector2.zero,new(200,120)).gameObject);
            // Its button off: a selectable nobody can see would be somewhere for a lost focus to land.
            var row=UiKit.Row(root,"WarmRow","Warm",null);row.interactable=false;row.enabled=false;Away(row.gameObject);
            yield return null;
            UiSkin.Kit("bar_frame");UiSkin.Kit("keycap");UiSkin.Kit("reticle");UiSkin.SoftBand();UiSkin.BustFade();
            yield return null;
            foreach(string item in UiSkin.Items)UiSkin.Icon(item);
            Away(UiKit.Icon(root,"WarmIcon",UiSkin.Items[0],Color.white).gameObject);
            yield return null;
            GameCatalog.Warm();
            yield return null;
            foreach(var skill in GameCatalog.All<SkillDef>())UiSkin.Skill(skill);
            yield return null;
            // A save row's date: the first local time and the first culture format of a run are slow to set up.
            DisplayNames.SavedAt(System.DateTime.UtcNow.ToString("o"));
            for(int i=0;i<DrawnFor;i++)yield return null;
            Object.Destroy(canvas.gameObject);
            Done=true;
        }
        /// <summary>Off the screen to the left: still drawn, so its shaders and textures are ready, and never seen.</summary>
        static void Away(GameObject element)
        {
            var rect=(RectTransform)element.transform;rect.anchorMin=rect.anchorMax=new(0,.5f);rect.anchoredPosition=new(-4000,0);
            foreach(var graphic in element.GetComponentsInChildren<Graphic>())graphic.raycastTarget=false;
        }
        /// <summary>For tests that need a run's first opening again.</summary>
        public static void Forget()=>Done=false;
    }
}
