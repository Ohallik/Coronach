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
        public bool IsOpen=>shown;
        float downAt=-1;
        void Update()
        {
            var party=PartyController.Current;if(shown||party==null||party.members==null)return;
            if(party.members.Any(a=>a.Health.Alive))
            {if(downAt>=0){downAt=-1;GameServices.Current.Input.Blocked=false;}return;}
            // A conversation must not outlive the party: it could finish under this panel and autosave a dead party.
            if(downAt<0){downAt=GameTime.Now;Lattice.Dialogue.DialogueSystem.Current?.Cancel();}
            GameServices.Current.Input.Blocked=true;
            if(GameTime.Now-downAt<CombatActor.DownDuration+.15f)return;
            shown=true;GameServices.Current.Input.Blocked=true;GameTime.Paused=true;
            var canvas=UiKit.CreateCanvas("Defeat",60,transform);var panel=UiKit.Dim(canvas.transform,.9f);
            var title=UiKit.Heading(panel.transform,"Heading","LINK LOST",54,UiKit.TextColor);UiKit.Rect(title.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,150),new(900,90));
            var retry=UiKit.Button(panel.transform,"Retry","Return to last autosave",()=>
            {
                // A second press in the fade would read the save and replace the game under the load.
                if(SceneFlow.Current.Loading)return;
                var saves=GameServices.Current.Saves;var state=saves.Load(saves.RetrySlot)??GameServices.Current.State;
                // A retry starts the party whole: an autosave can hold wounds taken mid-fight, or a partner still down.
                foreach(var m in state.party)m.integrity=Lattice.Rpg.Levels.MaxIntegrity(m.level);
                // A slot picked from the title is only armed in memory until the next autosave writes it: read back, it needs it again.
                GameServices.ArmAnEmptyParty(state);GameServices.Current.State=state;GameTime.Reset();
                if(saves.RetrySlot=="autosave")SceneFlow.Current.LoadZone(state.zone,state.spawn);else SceneFlow.Current.LoadSavedZone(state.zone,state.spawn);
            });UiKit.Rect(retry.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,-10),new(650,70));
            var quit=UiKit.Button(panel.transform,"Title","Return to title",()=>{if(SceneFlow.Current.Loading)return;GameTime.Reset();GameServices.Current.Input.Blocked=false;SceneFlow.Current.LoadZone("Title");});UiKit.Rect(quit.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,-110),new(650,70));
            UiKit.LinkVertical(retry,quit);EventSystem.current.SetSelectedGameObject(retry.gameObject);
        }
    }
}
