using Lattice.Core;
namespace Lattice.Combat
{
    /// <summary>Flash Move and Flash Guard, taught in the fight they belong to (TEST_PLAN 4.1): the first marked attack that starts near
    /// the player's hero gets a word from the partner about the dodge, and some marks later a word about the guard. Once per save. The
    /// proving grounds have Hal's own lesson, and flight has no guard, so both are left alone.</summary>
    public static class FlashLessons
    {
        public const string DodgeFlag="taught.flash",GuardFlag="taught.guard";
        const float Near=8;
        /// <summary>Marks between the lessons: long enough to have tried the first one.</summary>
        const int MarksBetween=8;
        static GameState counted;static int marks;
        public static void MarkedAttack(CombatActor hero,float distance)
        {
            var service=GameServices.Current;var zone=ZoneController.Current;
            if(service==null||zone==null||!zone.Combat||hero.flight||distance>Near||zone.definition.id.StartsWith("Arena_"))return;
            var flags=service.Flags;if(flags.GetBool(GuardFlag))return;
            if(!object.ReferenceEquals(counted,service.State)){counted=service.State;marks=0;}
            if(!flags.GetBool(DodgeFlag)){flags.SetBool(DodgeFlag,true);Say(hero,"teach.flash");return;}
            if(++marks<MarksBetween)return;
            flags.SetBool(GuardFlag,true);Say(hero,"teach.guard");
        }
        static void Say(CombatActor hero,string situation)
        {
            var voice=hero;var party=PartyController.Current;
            if(party!=null)foreach(var member in party.members)if(member!=hero&&member.Health.Alive)voice=member;
            BarkService.Play(voice.character,situation,true);
        }
    }
}
