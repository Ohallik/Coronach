using Lattice.Core;
using Lattice.Data;
namespace Lattice.Rpg
{
    public static class RpgServices
    {
        static GameState bound;
        static QuestService quests;
        public static QuestService Quests
        {
            get{var state=GameServices.Current.State;if(bound!=state){bound=state;quests=new QuestService(state,GameCatalog.All<QuestDef>());}return quests;}
        }
        public static Inventory Inventory=>new(GameServices.Current.State);
    }
}
