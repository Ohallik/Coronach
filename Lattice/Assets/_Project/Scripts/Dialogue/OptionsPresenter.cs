using System.Linq;
using Yarn.Unity;
namespace Lattice.Dialogue
{
    public sealed class OptionsPresenter:DialoguePresenterBase
    {
        public DialogueSystem system;
        public override YarnTask OnDialogueStartedAsync()=>YarnTask.CompletedTask;
        public override YarnTask OnDialogueCompleteAsync()=>YarnTask.CompletedTask;
        public override YarnTask RunLineAsync(LocalizedLine line,LineCancellationToken token)=>YarnTask.CompletedTask;
        public override async YarnTask<DialogueOption> RunOptionsAsync(DialogueOption[] options,LineCancellationToken token)
        {
            var available=options.Where(o=>o.IsAvailable).ToArray();if(available.Length==0)return null;
            int choice=-1;system.PresenterView.Options(available.Select(o=>o.Line.TextWithoutCharacterName.Text).ToArray(),i=>choice=i);
            await YarnTask.Yield();if(system.AutoAdvance)choice=0;
            while(choice<0&&!token.IsNextContentRequested)await YarnTask.Yield();
            system.PresenterView.ClearOptions();return available[choice<0?0:choice];
        }
    }
}
