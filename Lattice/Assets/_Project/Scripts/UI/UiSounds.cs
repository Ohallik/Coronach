using System;
using Lattice.Core;

namespace Lattice.UI
{
    // Menu feedback on the UI bus. A button plays confirm after its action unless
    // that action reported an error or a cancel, so a refused purchase never
    // sounds accepted and Back never sounds like confirming.
    public static class UiSounds
    {
        static int depth;static bool handled;
        public static void Move()=>AudioManager.PlayUi("ui_move",.28f);
        public static void Error(){handled=true;AudioManager.PlayUi("ui_error",.4f);}
        public static void Cancel(){handled=true;AudioManager.PlayUi("ui_cancel",.38f);}
        public static void Advance()=>AudioManager.PlayUi("dialogue_tick",.3f);
        public static void Run(Action action)
        {
            bool outer=depth==0;if(outer)handled=false;depth++;
            try{action();}
            finally{depth--;if(outer&&!handled)AudioManager.PlayUi("ui_confirm",.38f);}
        }
    }
}
