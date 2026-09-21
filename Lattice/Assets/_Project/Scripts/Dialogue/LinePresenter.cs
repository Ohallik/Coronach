using Lattice.Core;
using Lattice.Data;
using UnityEngine;
using Yarn.Unity;
namespace Lattice.Dialogue
{
    public sealed class LinePresenter:DialoguePresenterBase
    {
        public DialogueSystem system;
        public override YarnTask OnDialogueStartedAsync()=>YarnTask.CompletedTask;
        public override async YarnTask RunLineAsync(LocalizedLine line,LineCancellationToken token)
        {
            var view=system.View;if(view==null)return;
            var emotion=EmotionTags.Parse(line.Metadata);var form=ZoneController.Current!=null?ZoneController.Current.Form:BodyForm.Natural;
            string plain=line.TextWithoutCharacterName.Text;
            view.Show(line.CharacterName,emotion,PortraitLookup.Get(line.CharacterName,form,emotion),plain);system.LinesPresented++;
            await YarnTask.Yield();
            for(int i=0;i<=plain.Length&&!token.IsNextContentRequested;i++)
            {
                view.SetVisibleCharacters(i);
                if(system.AutoAdvance||view.AdvanceRequested)break;
                float until=Time.unscaledTime+TypewriterPacing.DelayAfter(plain,i);
                while(Time.unscaledTime<until&&!token.IsNextContentRequested)await YarnTask.Yield();
            }
            view.SetVisibleCharacters(int.MaxValue);await YarnTask.Yield();
            if(system.AutoAdvance){float until=Time.unscaledTime+.12f;while(Time.unscaledTime<until)await YarnTask.Yield();return;}
            while(!system.AutoAdvance&&!token.IsNextContentRequested&&!view.AdvanceRequested)await YarnTask.Yield();
        }
        public override YarnTask OnDialogueCompleteAsync(){system.View?.Hide();return YarnTask.CompletedTask;}
    }
}
