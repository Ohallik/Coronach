using Lattice.Core;
using Lattice.Data;
using UnityEngine;
using Yarn.Unity;
using Unity.Profiling;
namespace Lattice.Dialogue
{
    public sealed class LinePresenter:DialoguePresenterBase
    {
        public DialogueSystem system;
        int pendingLines;
        static readonly ProfilerMarker ShowMarker=new("Coronach.Dialogue.PresentLine");
        public override YarnTask OnDialogueStartedAsync()=>YarnTask.CompletedTask;
        public override async YarnTask RunLineAsync(LocalizedLine line,LineCancellationToken token)
        {
            pendingLines++;
            try{await PresentLine(line,token);}
            finally{if(system!=null&&token.IsNextContentRequested)system.Interrupted=true;pendingLines--;}
        }
        async YarnTask PresentLine(LocalizedLine line,LineCancellationToken token)
        {
            var view=system.PresenterView;if(view==null)return;
            var emotion=EmotionTags.Parse(line.Metadata);var form=ZoneController.Current!=null?ZoneController.Current.Form:BodyForm.Natural;
            string plain=line.TextWithoutCharacterName.Text;
            using(ShowMarker.Auto())view.Show(line.CharacterName,emotion,PortraitLookup.Get(line.CharacterName,form,emotion),plain);
            if(!system.Preparing)system.LinesPresented++;
            await YarnTask.Yield();
            if(system==null||system.PresenterView!=view)return;
            for(int i=0;i<=plain.Length&&!token.IsNextContentRequested&&system!=null&&system.PresenterView==view;i++)
            {
                view.SetVisibleCharacters(i);
                if(system.AutoAdvance||view.AdvanceRequested)break;
                float until=Time.unscaledTime+TypewriterPacing.DelayAfter(plain,i);
                while(Time.unscaledTime<until&&!token.IsNextContentRequested)await YarnTask.Yield();
            }
            if(system==null||system.PresenterView!=view)return;
            view.SetVisibleCharacters(int.MaxValue);await YarnTask.Yield();
            if(system==null||system.PresenterView!=view)return;
            if(system.AutoAdvance){float until=Time.unscaledTime+.12f;while(Time.unscaledTime<until)await YarnTask.Yield();return;}
            while(system!=null&&system.PresenterView==view&&!system.AutoAdvance&&!token.IsNextContentRequested&&!view.AdvanceRequested)await YarnTask.Yield();
        }
        public override async YarnTask OnDialogueCompleteAsync()
        {
            while(pendingLines>0)await YarnTask.Yield();
            if(system!=null)system.PresenterView?.Hide();
        }
    }
}
