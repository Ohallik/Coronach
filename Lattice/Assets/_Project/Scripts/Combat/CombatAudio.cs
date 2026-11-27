using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    // Combat cue choice and level in one place. Families are staged palette
    // variations under Resources/Audio/Palette; single keys are original SFX.
    public static class CombatAudio
    {
        // A wide skill can strike a crowd in one frame. Keep the first few
        // impacts readable instead of stacking every body into one peak.
        public const int ImpactBurst=3;
        const float BurstWindow=.05f;
        static float burstUntil;static int burstCount;

        public static void Swing(CombatActor actor,Vector3 point)=>
            AudioManager.Family("swing",3,actor.transform,point,.2f,110,.12f);

        public static void Impact(Health victim,bool armored)
        {
            if(Time.unscaledTime>=burstUntil){burstUntil=Time.unscaledTime+BurstWindow;burstCount=0;}
            if(burstCount>=ImpactBurst)return;
            burstCount++;
            AudioManager.Family(armored?"hit_armor":"hit_soft",3,victim.transform,victim.transform.position+Vector3.up*.9f,armored?.24f:.22f,90,.05f);
        }

        // Each hero keeps a recognisable shot; hostile fire is lower and heavier
        // so an incoming shot never reads as the player's own needle.
        public static void Shot(CombatActor actor,Vector3 point)=>
            AudioManager.Family(actor.character=="Sela"?"needle_sela":"needle_taren",3,actor.transform,point,.18f,100,.05f);

        public static void HostileShot(Transform shooter,Vector3 point)=>
            AudioManager.PlayAt("laserSmall_000",shooter,point,.16f,AudioBus.SFX,120,.08f,.78f);

        // The body owns its death: machines fail with a crunch, creatures fall
        // heavily. Corpses outlive the longest clip, so the voice can follow them.
        public static void Death(Transform body,bool mechanical)=>
            AudioManager.Family(mechanical?"death_mech":"death_creature",3,body,body.position+Vector3.up*.6f,mechanical?.26f:.24f,70,.1f);

        // A downed pilot's hull fails like a machine; a downed body hits the deck.
        public static void HeroDown(CombatActor hero)
        {
            if(hero.flight)AudioManager.Family("death_mech",3,hero.transform,hero.transform.position,.2f,60,.3f);
            else AudioManager.PlayAt("hero_down",hero.transform,hero.transform.position+Vector3.up*.4f,.3f,AudioBus.SFX,60,.3f);
        }

        public static void HeroRevive(CombatActor hero)=>
            AudioManager.PlayAt("hero_revive",hero.transform,hero.transform.position+Vector3.up,.26f,AudioBus.SFX,80,.3f);

        // Ground dodges are a body moving; flight rolls keep the thruster burst.
        public static void Dodge(CombatActor actor)
        {
            if(actor.flight)AudioManager.PlayAt("thrusterFire_000",actor.transform,actor.transform.position,.16f);
            else AudioManager.Family("dodge",3,actor.transform,actor.transform.position+Vector3.up*.9f,.18f,110,.1f);
        }

        // A flight dash leaves on the roll's thruster, so one that misses isn't silent.
        public static void Launch(CombatActor actor)=>
            AudioManager.PlayAt("thrusterFire_000",actor.transform,actor.transform.position,.18f,AudioBus.SFX,110,.1f,1.2f);

        // The tag-in strike: a short push of Pulse from the hero arriving, with a body behind it.
        public static void SwapStrike(CombatActor actor)
        {
            AudioManager.PlayAt("forceField_000",actor.transform,actor.transform.position+Vector3.up,.2f,AudioBus.SFX,100,.1f,1.25f);
            // The class, not this one's Impact(victim, armored).
            global::Lattice.Combat.Impact.SwapStrike(actor);
        }

        static readonly System.Collections.Generic.Dictionary<string,int> turns=new();
        // What a hit did, over the impact that says what it struck: a heavy blow's thump, a crit's tick, a break's crack. Impact asks for these, and only for the
        // hero the player holds, so a crowd can't stack them. A family's variations go round in turn with the pitch the caller gives: no random numbers are drawn
        // here, because the fight's own rolls come off that stream.
        public static void Accent(string family,int count,Transform emitter,Vector3 point,float volume,float pitch=1,int priority=110,float interval=.05f)
        {
            turns.TryGetValue(family,out int turn);turns[family]=turn+1;
            AudioManager.PlayAt(count>1?family+"_"+turn%count:family,emitter,point,volume,AudioBus.SFX,priority,interval,pitch);
        }

        // Round ten: things a player did that played nothing. Anything taken into the bag (a drop, a cache's spoils, a key, a quest's
        // reward) plucks once, not while a menu holds the game (a purchase already confirms), and a run of them is a patter; a save by
        // the player's hand chimes, an autosave doesn't (it comes with every key and every door).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Listen()
        {
            Lattice.Rpg.Inventory.Gained-=Taken;Lattice.Rpg.Inventory.Gained+=Taken;
            SaveSystem.Written-=Saved;SaveSystem.Written+=Saved;
        }
        static void Taken(string id,int amount)=>AudioManager.PlayAt("pickup",null,Vector3.zero,.3f,AudioBus.SFX,150,.12f);
        static void Saved(string slot){if(slot!="autosave")AudioManager.PlayUi("saved",.3f);}

        public static void Deflect(CombatActor actor)=>
            AudioManager.Family("deflect",2,actor.transform,actor.transform.position+Vector3.up*1.1f,.26f,60,.1f);
    }
}
