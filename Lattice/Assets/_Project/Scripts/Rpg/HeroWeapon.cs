using Lattice.Core;
using Lattice.Data;
namespace Lattice.Rpg
{
    /// <summary>The weapon slot a hero fights with on foot, read off their definition's own starting gear: Taren's is an Edge, Sela's an Emitter, Idra's the Edge her
    /// hammer strikes with. Read by what wears a new part first (Inventory.EquipIfEmpty), a chapter's arrival gear and the Party page's Output (DevLoadout.Weapon).
    /// It was asked by name, an Edge for Taren and an Emitter for anybody else, so with three heroes a hammer read as an emitter and a found Edge went past her.
    /// NAMED FALLBACK, for a hero with no definition or one that starts with no weapon: that old answer, so the first two read as they always did.</summary>
    public static class HeroWeapon
    {
        public static GearSlot Of(string hero)
        {
            var definition=GameCatalog.Find<CharacterDef>(hero);
            if(definition!=null&&definition.startingGear!=null)foreach(var part in definition.startingGear)if(part!=null&&(part.slot==GearSlot.Edge||part.slot==GearSlot.Emitter))return part.slot;
            return hero=="Taren"?GearSlot.Edge:GearSlot.Emitter;
        }
        /// <summary>Whether a part in this slot is a hero's own weapon (an Edge for an Edge hero), which a part found in the world goes to first.</summary>
        public static bool Wields(string hero,GearSlot slot)=>(slot==GearSlot.Edge||slot==GearSlot.Emitter)&&Of(hero)==slot;
    }
}
