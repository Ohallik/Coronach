using Lattice.Core;
namespace Lattice.Combat
{
    /// <summary>Flash Move, swap and Flash Guard, taught in the fight they belong to (TEST_PLAN 4.1): the first marked attack that starts near
    /// the player's hero gets a word from the partner about the dodge, a few marks later a word about swapping, and some marks after that a word about the guard. Once per save. The
    /// proving grounds have Hal's own lesson, and flight has no guard, so both are left alone.</summary>
    public static class FlashLessons
    {
        public const string DodgeFlag="taught.flash",SwapFlag="taught.swap",GuardFlag="taught.guard";
        const float Near=8;
        /// <summary>Marks between the lessons: long enough to have tried the first one.</summary>
        const int MarksBetween=8;
        /// <summary>Marks before the swap lesson: in the first scrap, and clear of both others.</summary>
        const int MarksToSwap=4;
        /// <summary>How long a lesson's line keeps the screen (BarkPanel holds one 3.5 s). Three hounds mark four times in under three seconds, and the next lesson would cut off the dodge lesson half read.</summary>
        const float LineStays=3.6f;
        static float saidAt=-99;
        static GameState counted;static int marks;
        public static void MarkedAttack(CombatActor hero,float distance)
        {
            var service=GameServices.Current;var zone=ZoneController.Current;
            if(service==null||zone==null||!zone.Combat||hero.flight||distance>Near||zone.definition.id.StartsWith("Arena_"))return;
            var flags=service.Flags;if(flags.GetBool(GuardFlag))return;
            if(!object.ReferenceEquals(counted,service.State)){counted=service.State;marks=0;}
            if(!flags.GetBool(DodgeFlag)){flags.SetBool(DodgeFlag,true);Say(hero,"teach.flash");return;}
            marks++;
            if(UnityEngine.Time.unscaledTime-saidAt<LineStays)return;
            // Someone has to be up to offer it. One mark says one thing.
            if(marks>=MarksToSwap&&!flags.GetBool(SwapFlag)&&PartnerUp(hero)){flags.SetBool(SwapFlag,true);Say(hero,"teach.swap");return;}
            if(marks<MarksBetween)return;
            flags.SetBool(GuardFlag,true);Say(hero,"teach.guard");
        }
        static bool PartnerUp(CombatActor hero)
        {
            var party=PartyController.Current;if(party==null)return false;
            foreach(var member in party.members)if(member!=hero&&member.Health.Alive)return true;
            return false;
        }
        static void Say(CombatActor hero,string situation)
        {
            saidAt=UnityEngine.Time.unscaledTime;
            var voice=hero;var party=PartyController.Current;
            if(party!=null)foreach(var member in party.members)if(member!=hero&&member.Health.Alive)voice=member;
            BarkService.Play(voice.character,situation,true);
        }
    }
}
