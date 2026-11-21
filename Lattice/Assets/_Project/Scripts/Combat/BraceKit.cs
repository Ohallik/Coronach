using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>The guard hero's kit (S6a: Idra Sorn's Brace), on a hero whose <see cref="HeroStyle"/> asks for it. The guard's own numbers are data (what it lets
    /// through, what it costs, whether a ship can raise it); this is what a guard does beyond stopping a hit:
    /// <list type="bullet">
    /// <item>a Flash Guard answers with a shockwave round her (the style's counter), marked as a ring for the moment it lands;</item>
    /// <item>Counterweight arms her next three guarded hits to answer the same way, smaller;</item>
    /// <item>Hold the Line turns ordinary enemies near her onto her for a few seconds and raises her Plating. Never a boss: a boss's specials are aimed at whoever
    /// the player is playing, and a boss half-turned between two heroes would mark one and face the other;</item>
    /// <item>in flight the guard is a prow, drawn as an arc ahead of the ship while it's up.</item>
    /// </list>
    /// A shockwave is owed, not thrown, from inside the hit that earned it: it leaves once that hit has finished landing, the same frame.</summary>
    public sealed class BraceKit:MonoBehaviour
    {
        /// <summary>Counterweight: how long it stays armed, how many guarded hits it answers, and each answer's worth in Output, its Break and its reach.</summary>
        public const float CounterweightSeconds=12,CounterweightHit=1.2f,CounterweightBreak=45,CounterweightReach=3.5f;
        public const int CounterweightHits=3;
        /// <summary>Hold the Line: how long, how far it reaches, and the Plating it lends.</summary>
        public const float HoldSeconds=6,HoldReach=14,HoldPlating=40;
        CombatActor actor;HeroSkills skills;float counterweightUntil,holdUntil;bool prowShown;FlightEnergyArc prow;
        readonly List<(float hit,float breakPower,float reach,HitWeight weight)> owed=new();
        readonly List<Material> washed=new();
        static BraceKit holder;
        /// <summary>Guarded hits Counterweight will still answer.</summary>
        public int Counterweights{get;private set;}
        /// <summary>Shockwaves released so far, a Flash Guard's and Counterweight's together.</summary>
        public int Shockwaves{get;private set;}
        public bool Holding=>holder==this&&GameTime.Now<holdUntil&&actor.Health.Alive;

        // HeroSkills asks for this component, so it's here before they are: they're found at Start.
        void Awake(){actor=GetComponent<CombatActor>();CombatActor.FlashGuarded+=OnFlashGuard;CombatActor.GuardHeld+=OnGuardHeld;}
        void Start(){skills=GetComponent<HeroSkills>();Wash();}
        void OnDestroy()
        {
            CombatActor.FlashGuarded-=OnFlashGuard;CombatActor.GuardHeld-=OnGuardHeld;if(holder==this)holder=null;
            foreach(var material in washed)if(material!=null)Destroy(material);
        }
        void OnFlashGuard(CombatActor who,DamagePacket packet)
        {
            var style=actor.Style;if(who!=actor||style.counter<=0)return;
            owed.Add((style.counter,style.counterBreak,style.counterReach,HitWeight.Heavy));
        }
        void OnGuardHeld(CombatActor who)
        {
            if(who!=actor||Counterweights<=0||GameTime.Now>=counterweightUntil)return;
            Counterweights--;owed.Add((CounterweightHit,CounterweightBreak,CounterweightReach,HitWeight.Medium));
        }
        void LateUpdate()
        {
            if(owed.Count>0&&!GameTime.CombatHeld)
            {
                // By count, not by walking the list: whatever a shockwave sets off may owe another, and that one leaves next frame.
                int due=owed.Count;
                if(actor.Health.Alive&&skills!=null)for(int i=0;i<due;i++){var shock=owed[i];skills.Shock(shock.hit,shock.breakPower,shock.reach,shock.weight);Shockwaves++;}
                owed.RemoveRange(0,due);
            }
            if(Counterweights>0&&GameTime.Now>=counterweightUntil)Counterweights=0;
            Prow();
        }
        /// <summary>Counterweight is cast: the next three hits the guard stops are answered.</summary>
        public void Arm(){Counterweights=CounterweightHits;counterweightUntil=GameTime.Now+CounterweightSeconds;}
        /// <summary>Hold the Line is cast.</summary>
        public void Hold()
        {
            // One clock for both halves of it, so the Plating it lends goes in the frame the enemies turn back.
            holder=this;holdUntil=GameTime.Now+HoldSeconds;
            actor.platingBoost=HoldPlating;actor.platingBoostUntil=holdUntil;
        }
        /// <summary>Who an ordinary enemy turns to while someone holds the line near it, or nobody: EnemyBrain asks before it takes the player's hero as usual.</summary>
        public static CombatActor Holder(EnemyBrain brain)
        {
            if(holder==null||!holder.Holding||brain==null||brain.definition==null||brain.definition.boss)return null;
            var gap=brain.transform.position-holder.transform.position;gap.y=0;
            return gap.sqrMagnitude<=HoldReach*HoldReach?holder.actor:null;
        }
        /// <summary>The prow guard, while it's up: an arc ahead of the ship as wide as what it covers.</summary>
        void Prow()
        {
            bool up=actor.flight&&actor.Guarding&&actor.Health.Alive;
            if(!up){if(prowShown){prow.Draw(0,1,Color.clear,false);prowShown=false;}return;}
            if(prow==null){prow=FlightEnergyArc.Create("Prow guard");prow.transform.SetParent(transform,false);prow.transform.localPosition=Vector3.up*.8f;}
            prow.transform.rotation=Quaternion.LookRotation(actor.motor.Facing,Vector3.up);
            prow.Draw(HeroCollision.HullRadius(actor.character)+.7f,actor.Style.prowArc*.5f,new Color(1.5f,.9f,.35f,.85f),false);prowShown=true;
        }
        /// <summary>A hero standing in borrowed bodies (hers aren't made yet) wears them in her own cast, so two of one model can be told apart.</summary>
        void Wash()
        {
            var style=actor.Style;if(style.bodyTint==Color.white||string.IsNullOrEmpty(style.bodiesFrom)||!TryGetComponent<FormController>(out var form))return;
            var mine=GameCatalog.Find<CharacterDef>(actor.character);var lender=GameCatalog.Find<CharacterDef>(style.bodiesFrom);if(mine==null||lender==null)return;
            Tint(form.natural,mine.natural==lender.natural,style.bodyTint);Tint(form.shaped,mine.shaped==lender.shaped,style.bodyTint);Tint(form.flight,mine.flight==lender.flight,style.bodyTint);
        }
        void Tint(GameObject body,bool borrowed,Color tint)
        {
            if(body==null||!borrowed)return;
            foreach(var view in body.GetComponentsInChildren<Renderer>(true))
            {
                if(view is not (MeshRenderer or SkinnedMeshRenderer))continue;
                foreach(var material in view.materials)
                {washed.Add(material);if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",material.GetColor("_BaseColor")*tint);}
            }
        }
        /// <summary>Whether any of her bodies is wearing the wash: a check's way to see that a borrowed body is told apart.</summary>
        public bool Washed=>washed.Count>0;
    }
}
