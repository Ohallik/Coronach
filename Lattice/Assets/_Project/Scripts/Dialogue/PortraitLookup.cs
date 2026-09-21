using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Dialogue
{
    public static class PortraitLookup
    {
        public static Sprite Get(string speaker,BodyForm form,PortraitEmotion emotion)
        {
            var character=GameCatalog.Find<CharacterDef>(speaker);
            var portraits=character!=null?(form==BodyForm.Natural||form==BodyForm.CivilFlight?character.portraitNatural:character.portraitShaped):null;
            if(portraits==null)portraits=Resources.Load<PortraitSet>("Portraits/"+speaker+"_Natural");
            return portraits!=null?portraits.Get(emotion):null;
        }
    }
}
