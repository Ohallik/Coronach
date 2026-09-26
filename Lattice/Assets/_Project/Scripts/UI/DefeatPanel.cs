using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Lattice.UI
{
    public sealed class DefeatPanel:MonoBehaviour
    {
        bool shown;
        float downAt=-1;
        void Update()
        {
            var party=PartyController.Current;if(shown||party==null||party.members==null)return;
            if(party.members.Any(a=>a.Health.Alive))
            {if(downAt>=0){downAt=-1;GameServices.Current.Input.Blocked=false;}return;}
            if(downAt<0){downAt=GameTime.Now;GameServices.Current.Input.Blocked=true;}
            if(GameTime.Now-downAt<CombatActor.DownDuration+.15f)return;
            shown=true;GameServices.Current.Input.Blocked=true;GameTime.Paused=true;
            var canvas=UiKit.CreateCanvas("Defeat",60,transform);var panel=UiKit.Dim(canvas.transform,.9f);
            var title=UiKit.Heading(panel.transform,"Heading","LINK LOST",54,UiKit.TextColor);UiKit.Rect(title.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,150),new(900,90));
            var retry=UiKit.Button(panel.transform,"Retry","Return to last autosave",()=>
            {
                var state=GameServices.Current.Saves.Load("autosave");
                if(state==null){state=GameServices.Current.State;foreach(var m in state.party)m.integrity=Lattice.Rpg.Levels.MaxIntegrity(m.level);}
                GameServices.Current.State=state;GameTime.Reset();SceneFlow.Current.LoadZone(state.zone,state.spawn);
            });UiKit.Rect(retry.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,-10),new(650,70));
            var quit=UiKit.Button(panel.transform,"Title","Return to title",()=>{GameTime.Reset();GameServices.Current.Input.Blocked=false;SceneFlow.Current.LoadZone("Title");});UiKit.Rect(quit.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,-110),new(650,70));
            UiKit.LinkVertical(retry,quit);EventSystem.current.SetSelectedGameObject(retry.gameObject);
        }
    }
}
