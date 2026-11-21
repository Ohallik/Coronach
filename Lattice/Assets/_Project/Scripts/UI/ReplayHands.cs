using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>What the replay driver decides for itself, as a player who knows the game would: which item the pouch should give up now, who needs raising, whether
    /// the EXTRA is worth releasing. Decisions only, made from what a player reads off the screen (the bars, the quick item's count and its wait, who is down, what
    /// stands in reach). The driver still does every one of them with the pad (ContinuousReview), and the game still refuses whatever it would refuse a person.</summary>
    public static class ReplayHands
    {
        /// <summary>In a fight the lead mends under this share of their Integrity. Between fights they top up under the second, and only with an item none of which
        /// is wasted. The routes pressed a gel on a clock, once a round, hurt or not: one went at nine tenths, and there was none left at a fifth.</summary>
        public const float FightBelow=.5f,CalmBelow=.75f;
        /// <summary>How far Taren's Anvil Chorus carries him to his target (ExtraMove closes 9 m on foot and 11 in flight, to a stride short of it), and how near it
        /// bodies must stand to be under the hammers.</summary>
        public const float ChorusReach=10,ChorusFlightReach=14,ChorusCrowd=5;
        /// <summary>Sela's Lattice Aria falls on everything on the screen out to 45 m: inside this the fixed camera has it in frame.</summary>
        public const float AriaReach=30;
        /// <summary>An EXTRA is for a boss, or for this many at once: on two scavengers it's a minute's gauge spent on ten seconds' work.</summary>
        public const int Crowd=3;

        /// <summary>The partner who is down, if one is.</summary>
        public static CombatActor Fallen(PartyController party)
        {
            if(party==null||party.members==null)return null;
            foreach(var member in party.members)if(member!=null&&member!=party.Active&&member.gameObject.activeInHierarchy&&!member.Health.Alive)return member;
            return null;
        }
        /// <summary>A Relink Spike, or whatever else in the pouch raises a partner: its id, or null with none held.</summary>
        public static string HeldRevive(GameState state)
        {
            foreach(var held in state.consumables)
            {
                if(held.Value<=0)continue;var def=GameCatalog.Find<ConsumableDef>(held.Key);
                if(def!=null&&def.revive)return def.id;
            }
            return null;
        }
        /// <summary>The item the lead should use right now, or null: nothing while the game's wait between items runs (the HUD counts it down), a mend when they are hurt
        /// enough for where they are, a Spike when the partner is down. Their own Integrity comes first: a raised partner is no use to a lead who falls beside them.</summary>
        public static string ItemFor(PartyController party,CombatActor lead,GameState state)
        {
            if(party==null||lead==null||state==null||!lead.Health.Alive||ItemUse.Wait(lead)>0)return null;
            float most=lead.Health.maximum,has=lead.Health.integrity;bool fight=GearEffects.InFight(lead.transform.position);
            if(has<most*(fight?FightBelow:CalmBelow))
            {
                // Between fights nothing is wasted, unless they'd walk into the next one under half.
                string mend=Mend(party,lead,state,fight||has<most*FightBelow);
                if(mend!=null)return mend;
            }
            return Fallen(party)!=null?HeldRevive(state):null;
        }
        /// <summary>The mend that fits what's missing best: the least Integrity left unmended and the least wasted, counted together, so a Salve goes at half and a Core
        /// only near the floor. One that restores both heroes is credited with what it gives the partner. Of two that fit as well, the cheaper.</summary>
        public static string Mend(PartyController party,CombatActor lead,GameState state,bool mayWaste)
        {
            float missing=lead.Health.maximum-lead.Health.integrity,bestMiss=float.PositiveInfinity;ConsumableDef best=null;
            foreach(var held in state.consumables)
            {
                if(held.Value<=0)continue;var def=GameCatalog.Find<ConsumableDef>(held.Key);
                if(def==null||def.revive||def.seconds>0||def.heal<=0&&def.healPercent<=0)continue;
                float heal=ItemUse.Heal(def,lead),over=Mathf.Max(0,heal-missing);
                if(!mayWaste&&over>.5f)continue;
                float miss=over+Mathf.Max(0,missing-heal);
                if(def.party)foreach(var other in party.members)
                    if(other!=null&&other!=lead&&other.Health.Alive)miss-=Mathf.Min(ItemUse.Heal(def,other),other.Health.maximum-other.Health.integrity);
                if(best==null||miss<bestMiss-.01f||miss<=bestMiss+.01f&&def.value<best.value){best=def;bestMiss=miss;}
            }
            return best!=null?best.id:null;
        }
        /// <summary>Whether the lead's full gauge should go now: a boss that can be hurt is in the move's reach, or three or more enemies are. Never into a boss's shift
        /// or its cover, where every blow of it would be turned away, and never by Taren at the air: his goes where his target is.</summary>
        public static bool ExtraDue(CombatActor lead)
        {
            if(lead==null||!lead.Health.Alive||lead.Recovering||lead.State==ActorState.Stagger||lead.State==ActorState.Down||!ExtraGauge.Ready(lead.character))return false;
            var hero=GameCatalog.Find<CharacterDef>(lead.character);if(hero==null||hero.extra==null)return false;
            bool aria=hero.extra.kind==SkillKind.LatticeAria;
            var centre=lead.transform.position;float reach=AriaReach;
            if(!aria)
            {
                if(lead.target==null||!lead.target.Targetable)return false;
                var gap=lead.target.transform.position-centre;gap.y=0;if(gap.magnitude>(lead.flight?ChorusFlightReach:ChorusReach))return false;
                centre=lead.target.transform.position;reach=ChorusCrowd;
            }
            int bodies=0;
            foreach(var body in Health.All)
            {
                if(body==null||body.friendly||!body.Targetable)continue;
                var gap=body.transform.position-centre;gap.y=0;if(gap.sqrMagnitude>reach*reach)continue;
                var carrier=body.reactionOwner!=null?body.reactionOwner:body;
                if(carrier.TryGetComponent<BossController>(out var boss))
                {
                    if(!boss.Shifting&&carrier.Targetable&&GameTime.Now>=carrier.InvulnerableUntil)return true;
                    continue;
                }
                if(++bodies>=Crowd)return true;
            }
            return false;
        }
    }
}
