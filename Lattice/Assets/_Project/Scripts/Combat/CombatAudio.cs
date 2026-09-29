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
    }
}
