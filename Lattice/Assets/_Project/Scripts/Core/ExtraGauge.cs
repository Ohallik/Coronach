using UnityEngine;
namespace Lattice.Core
{
    /// <summary>The EXTRA gauge (S3): each hero's second meter, 0 to 100, which a full one spends on that hero's EXTRA move. It fills from damage dealt and taken and from
    /// every Flash Move and Flash Guard, never runs down, and lives in the saved <see cref="MemberState"/>, so a save, a load and a zone change all keep it. Until the
    /// story sets <see cref="Flag"/> there is no gauge: nothing fills and nothing shows.</summary>
    public static class ExtraGauge
    {
        public const string Flag="extra.unlocked",TaughtFlag="taught.extra";
        public const float Full=100;
        /// <summary>What a landed hit of the hero's own Output pays (a skill's bigger hit pays in proportion, up to <see cref="MostHit"/> hits' worth at once), what a whole
        /// Integrity bar taken pays, and what a Flash pays. A lead who keeps swinging fills it in a little over a minute of fighting, sooner for being hit or for timing
        /// a Flash: one EXTRA in a long scrap, two or three in a boss fight.</summary>
        public const float PerHit=.75f,MostHit=4,PerBarTaken=50,PerFlash=10;
        /// <summary>The partner's gauge fills at half the lead's rate, from every source.</summary>
        public const float PartnerShare=.5f;
        /// <summary>A gauge has just come full: (character).</summary>
        public static event System.Action<string> Filled;
        static GameState State=>GameServices.Current!=null?GameServices.Current.State:null;
        static MemberState Member(string character)=>State?.party?.Find(m=>m.id==character);
        public static bool Unlocked{get{var state=State;return state!=null&&state.flags!=null&&state.flags.TryGetValue(Flag,out bool on)&&on;}}
        public static float Of(string character){var member=Member(character);return member!=null&&Unlocked?Mathf.Clamp(member.extra,0,Full):0;}
        public static bool Ready(string character)=>Unlocked&&Of(character)>=Full;
        /// <param name="lead">Whether this hero is the one the player is playing: anyone else's fills at <see cref="PartnerShare"/>.</param>
        public static void Add(string character,float amount,bool lead)
        {
            if(amount<=0||!Unlocked)return;var member=Member(character);if(member==null)return;
            float before=Mathf.Clamp(member.extra,0,Full);member.extra=Mathf.Min(Full,before+amount*(lead?1:PartnerShare));
            if(before<Full&&member.extra>=Full){Debug.Log("EXTRA_FULL "+character);Filled?.Invoke(character);}
        }
        /// <summary>Empties a full gauge; a gauge short of full spends nothing.</summary>
        public static bool Spend(string character)
        {
            if(!Ready(character))return false;
            Member(character).extra=0;return true;
        }
        /// <summary>What to press, in the glyphs of the device in the player's hands: RB and LT together on a pad (LT alone stays the quick item, or the brake in flight), the key on a keyboard.</summary>
        public static string Binding(bool flight)=>PromptService.Device==PromptDevice.Gamepad?
            PromptService.Tag("SkillMod")+"+"+PromptService.Tag(flight?"Brake":"QuickItem"):PromptService.Tag("Extra");
    }
}
