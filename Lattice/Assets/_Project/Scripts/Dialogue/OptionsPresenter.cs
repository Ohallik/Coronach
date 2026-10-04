using System.Linq;
using Yarn.Unity;
namespace Lattice.Dialogue
{
    public sealed class OptionsPresenter:DialoguePresenterBase
    {
        public DialogueSystem system;
        int pendingOptions;
        public override YarnTask OnDialogueStartedAsync()=>YarnTask.CompletedTask;
        public override async YarnTask OnDialogueCompleteAsync()
        {
            // Yarn must retain its cancelled dialogue token until the options
            // continuation observes it. Otherwise Stop can fall through into
            // SetSelectedOption after the VM has already stopped.
            while(pendingOptions>0)await YarnTask.Yield();
            await YarnTask.Yield();
        }
        public override YarnTask RunLineAsync(LocalizedLine line,LineCancellationToken token)=>YarnTask.CompletedTask;
        public override async YarnTask<DialogueOption> RunOptionsAsync(DialogueOption[] options,LineCancellationToken token)
        {
            pendingOptions++;
            try
            {
                var available=options.Where(o=>o.IsAvailable).ToArray();if(available.Length==0)return null;
                var view=system.PresenterView;if(view==null)return null;
                int choice=-1;view.Options(available.Select(o=>o.Line.TextWithoutCharacterName.Text).ToArray(),i=>choice=i);
                await YarnTask.Yield();if(system.AutoAdvance)choice=0;
                while(choice<0&&!token.IsNextContentRequested&&system!=null&&system.PresenterView==view)await YarnTask.Yield();
                if(system==null||system.PresenterView!=view)return null;
                view.ClearOptions();
                // Stopping/unloading a conversation is not consent to its first branch.
                return token.IsNextContentRequested||choice<0?null:available[choice];
            }
            finally{if(system!=null&&token.IsNextContentRequested)system.Interrupted=true;pendingOptions--;}
        }
    }
}
