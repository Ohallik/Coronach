using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>What a part or an item does, in the words Gear, the bench and the shop all use. The numbers are what the hero
    /// actually gets: tier, upgrades and affixes counted the way Gear.Calculate counts them.</summary>
    public static class PartText
    {
        static IEnumerable<(string name,float value)> Lines(Stats stats)
        {
            yield return ("Output",stats.output);yield return ("Plating",stats.plating);yield return ("Response",stats.response);yield return ("Resonance",stats.resonance);yield return ("Fortune",stats.fortune);
        }
        static bool Counts((string name,float value) line)=>Mathf.Abs(line.value)>=.5f;
        static Stats Total(TechPartDef def,PartInstance item)=>item==null?def.baseStats:Gear.PartTotal(def,item,GameCatalog.Find<AffixDef>);
        public static string Says(Stats stats)=>string.Join(", ",Lines(stats).Where(Counts).Select(l=>$"{l.name} +{l.value:0}"));
        public static string Effect(TechPartDef def,PartInstance item=null)=>Says(Total(def,item));
        static string One(float value)=>value.ToString("0.#",CultureInfo.InvariantCulture);
        /// <summary>A share as a percentage, to two places where it needs them: a meal's quarter of a percent a second mustn't round to nothing or to more than it is.</summary>
        static string Percent(float share)=>(Mathf.Abs(share)*100).ToString("0.##",CultureInfo.InvariantCulture)+"%";
        static string Signed(float value)=>(value<0?"-":"+")+One(Mathf.Abs(value));
        static string SignedPercent(float share)=>(share<0?"-":"+")+Percent(share);
        /// <summary>What a set of modifiers does, a phrase for each, in plain words (D200): an affix's, a module's passive's, a draught's. One place says it, from the numbers the fight
        /// reads, so a row can't promise what the part doesn't do.</summary>
        public static IEnumerable<string> Phrases(Mods m)
        {
            if(m.damageDealt!=0)yield return $"damage dealt {SignedPercent(m.damageDealt)}";
            if(m.damageTaken!=0)yield return $"damage taken {SignedPercent(m.damageTaken)}";
            if(m.ownType!=0)yield return $"{SignedPercent(m.ownType)} damage of the weapon's own type";
            if(m.beam!=0)yield return $"Beam damage {SignedPercent(m.beam)}";
            if(m.plasma!=0)yield return $"Plasma damage {SignedPercent(m.plasma)}";
            if(m.kinetic!=0)yield return $"Kinetic damage {SignedPercent(m.kinetic)}";
            if(m.pulse!=0)yield return $"Pulse damage {SignedPercent(m.pulse)}";
            if(m.critChance!=0)yield return $"crit chance {SignedPercent(m.critChance)}";
            if(m.critDamage!=0)yield return $"crits hit {Percent(m.critDamage)} {(m.critDamage<0?"softer":"harder")}";
            if(m.breakPower!=0)yield return $"Break power {SignedPercent(m.breakPower)}";
            if(m.brokenTime!=0)yield return $"a Break lasts {One(Mathf.Abs(m.brokenTime))} s {(m.brokenTime<0?"less":"longer")}";
            if(m.brokenDamage!=0)yield return $"{SignedPercent(m.brokenDamage)} damage to Broken enemies";
            if(m.executeDamage!=0)yield return $"{SignedPercent(m.executeDamage)} damage to enemies under {Percent(Lattice.Combat.GearEffects.ExecuteBelow)} Integrity";
            if(m.chargeGain!=0)yield return $"Charge gain {SignedPercent(m.chargeGain)}";
            if(m.chargeHold>0)yield return "Charge doesn't fade";
            if(m.chargeFloor>0)yield return $"out of a fight, Charge refills to {One(m.chargeFloor)}";
            if(m.skillCost!=0)yield return $"skills cost {One(Mathf.Abs(m.skillCost))} {(m.skillCost<0?"less":"more")} Charge";
            if(m.cooldown!=0)yield return $"skill cooldowns {Percent(m.cooldown)} {(m.cooldown<0?"shorter":"longer")}";
            if(m.mastery>0)yield return $"skills act {One(m.mastery)} mastery level{(m.mastery>1?"s":"")} higher";
            if(m.guardCost!=0)yield return $"guarding costs {Percent(m.guardCost)} {(m.guardCost<0?"less":"more")} Thrust";
            if(m.mashCost!=0)yield return $"a rushed roll costs {Percent(m.mashCost)} {(m.mashCost<0?"less":"more")} Thrust";
            if(m.thrustRegen!=0)yield return $"Thrust returns {Percent(m.thrustRegen)} {(m.thrustRegen<0?"slower":"faster")}";
            if(m.flashWindow!=0)yield return $"the Flash window is {Mathf.Abs(m.flashWindow).ToString("0.##",CultureInfo.InvariantCulture)} s {(m.flashWindow<0?"tighter":"wider")}";
            if(m.flashCharge!=0)yield return $"a Flash pays {One(m.flashCharge)} more Charge";
            if(m.flashDamage!=0)yield return $"{SignedPercent(m.flashDamage)} damage for {One(Lattice.Combat.GearEffects.FlashCoilSeconds)} s after a Flash";
            if(m.lifeOnKill!=0)yield return $"a kill restores {Percent(m.lifeOnKill)} Integrity";
            if(m.regen!=0)yield return $"restores {Percent(m.regen)} Integrity a second";
            if(m.sharedHeal!=0)yield return $"a healing item also gives your partner {Percent(m.sharedHeal)} as much";
            if(m.partnerDamage!=0)yield return $"your partner deals {Percent(Lattice.Combat.PartyController.PartnerDamage+m.partnerDamage)} damage, not {Percent(Lattice.Combat.PartyController.PartnerDamage)}";
            if(m.dropLuck!=0)yield return $"drop chance {SignedPercent(m.dropLuck)}";
            if(m.luckCap!=0)yield return $"Fortune's drop luck can reach {Percent(.15f+m.luckCap)}";
            if(m.secondWind>0)yield return "once a fight, a killing blow leaves 1 Integrity";
        }
        public static string Says(Mods mods)=>string.Join(", ",Phrases(mods));
        /// <summary>What the part itself always does beyond its stats: a module's passive.</summary>
        public static string Passive(TechPartDef def)=>Says(def.mods.On(def.damageType));
        /// <summary>What one affix does on this part: its stats at the part's tier, and its effect. "Keen: crit chance +5%".</summary>
        public static string AffixSays(AffixDef affix,TechPartDef def)
        {
            string stats=Says(Gear.AffixStats(affix,def)),effect=Says(affix.mods.On(def.damageType));
            return affix.displayName+": "+(stats!=""&&effect!=""?stats+", "+effect:stats+effect);
        }
        /// <summary>Everything a part does that its stat line can't say: its passive, then each affix. Empty for a plain part.</summary>
        public static string Effects(TechPartDef def,PartInstance item=null)
        {
            var said=new List<string>();string passive=Passive(def);if(passive!="")said.Add(char.ToUpperInvariant(passive[0])+passive.Substring(1));
            if(item!=null)foreach(string name in item.affixes.Take(2)){var affix=GameCatalog.Find<AffixDef>(name);if(affix!=null)said.Add(AffixSays(affix,def));}
            return string.Join("  ·  ",said);
        }
        /// <summary>A part against what the hero wears in its slot, stat by stat, all five: "Against Taren's Refined Edge: Output +20, Response -2". The row's "now +8" has room for one.</summary>
        public static string Compare(GameState state,MemberState member,TechPartDef def,PartInstance item=null,bool secondModule=false)
        {
            string who=DisplayNames.Speaker(member.id);
            string wornId=secondModule?member.module2:member.equipped.TryGetValue(def.slot,out string id)?id:null;
            var worn=string.IsNullOrEmpty(wornId)?null:state.parts.Find(p=>p.instanceId==wornId);var wornDef=worn!=null?GameCatalog.Find<TechPartDef>(worn.definitionId):null;
            string slot=secondModule?"second module":def.slot.ToString();
            if(wornDef==null)return $"{who}'s {slot} slot is empty.";
            if(item!=null&&worn==item)return $"{who} has this on.";
            var mine=Lines(Total(def,item)).ToArray();var theirs=Lines(Total(wornDef,worn)).ToArray();
            var changes=mine.Select((line,i)=>(line.name,delta:line.value-theirs[i].value)).Where(c=>Mathf.Abs(c.delta)>=.05f).Select(c=>$"{c.name} {Signed(c.delta)}").ToArray();
            string effects=Effects(wornDef,worn)!=""?", which has effects of its own":"";
            return $"Against {who}'s {wornDef.displayName}{effects}: {(changes.Length>0?string.Join(", ",changes):"the same stats")}.";
        }
        /// <summary>A price in materials and scrip, each against what's held, the short ones marked as the bench marks them: "Scrap Alloy 40/12, Rootfibre 0/2, 150 scrip".</summary>
        public static string Price(GameState state,Ingredient[] materials,int scrip)
        {
            var inventory=new Inventory(state);
            string Short(string words,bool lacking)=>lacking?$"<color=#FFB08A>{words}</color>":words;
            var parts=(materials??System.Array.Empty<Ingredient>()).Select(m=>Short($"{DisplayNames.Item(m.id)} {inventory.Count(m.id)}/{m.count}",inventory.Count(m.id)<m.count)).ToList();
            if(scrip>0)parts.Add(Short($"{scrip} scrip",state.scrip<scrip));
            return string.Join(", ",parts);
        }
        /// <summary>What the next upgrade costs, or that there isn't one.</summary>
        public static string UpgradePrice(GameState state,TechPartDef def,PartInstance item)
        {
            if(item.upgrade>=Gear.MaxUpgrade)return "Fully upgraded.";
            var (materials,scrip)=Gear.UpgradeCost(def,item.upgrade+1);
            return $"+{item.upgrade+1} costs {Price(state,materials,scrip)}.";
        }
        /// <summary>A row says as much as fits on its one line: its leaner wordings follow in order, and what's dropped is in the note under the list.</summary>
        public static void Narrow(UnityEngine.UI.Button row,params string[] wordings)
        {
            var label=row.GetComponentInChildren<TMPro.TMP_Text>();if(label==null||wordings==null||wordings.Length==0)return;
            float room=label.rectTransform.rect.width-label.margin.x-label.margin.z;
            // A row that can't say how wide it is yet keeps its full wording: better long than silently short.
            if(room<=1){label.text=wordings[0];return;}
            foreach(string words in wordings){label.text=words;if(label.GetPreferredValues(words).x<=room)return;}
        }
        /// <summary>Puts lines in a note that holds only so many. If they run over, the first line gives way from its end, a piece at a time ("  ·  "), and then goes altogether;
        /// the lines under it never do: a price or a comparison is what the press is decided on.</summary>
        public static void Fit(TMPro.TMP_Text note,params string[] lines)
        {
            if(note==null)return;
            var kept=(lines??System.Array.Empty<string>()).Where(l=>!string.IsNullOrEmpty(l)).ToList();
            note.text=string.Join("\n",kept);if(kept.Count==0)return;
            float room=note.rectTransform.rect.height+1,wide=note.rectTransform.rect.width;if(room<=1||wide<=1)return;
            while(note.GetPreferredValues(note.text,wide,0).y>room)
            {
                int cut=kept[0].LastIndexOf("  ·  ",System.StringComparison.Ordinal);
                if(cut<=0){if(kept.Count<2)return;kept.RemoveAt(0);note.text=string.Join("\n",kept);continue;}
                kept[0]=kept[0].Substring(0,cut);note.text=kept[0]+"  ·  and more"+(kept.Count>1?"\n"+string.Join("\n",kept.Skip(1)):"");
            }
        }
        /// <summary>How many of a consumable are held against the most that can be: "3 of 9".</summary>
        public static string Carried(GameState state,ConsumableDef def)
        {
            int held=new Inventory(state).Held(def.id),cap=Inventory.Cap(def);
            return cap==int.MaxValue?held.ToString():$"{held} of {cap}";
        }
        /// <summary>What the next upgrade buys, by the sums the upgrade itself will use: "Output +30 to +32.4". To one place, in this mode alone: a Scrap Module's Fortune gains about a quarter, and
        /// rounded it read "+3 to +3", paying for nothing. What an upgrade can't move, an affix's own stat, stands alone.</summary>
        public static string NextUpgrade(TechPartDef def,PartInstance item)
        {
            if(item.upgrade>=5)return Upgraded(def,item)+"  ·  fully upgraded";
            var next=new PartInstance{definitionId=item.definitionId,upgrade=item.upgrade+1,affixes=item.affixes};
            return string.Join(", ",Lines(Total(def,item)).Zip(Lines(Total(def,next)),(now,then)=>(now,then)).Where(line=>Counts(line.then))
                .Select(line=>$"{line.now.name} +{One(line.now.value)}"+(Mathf.Abs(line.then.value-line.now.value)<.01f?"":$" to +{One(line.then.value)}")));
        }
        /// <summary>The same numbers once it's paid: with the row promising +32.4, the line under it said +32.</summary>
        public static string Upgraded(TechPartDef def,PartInstance item)=>string.Join(", ",Lines(Total(def,item)).Where(Counts).Select(line=>$"{line.name} +{One(line.value)}"));
        /// <summary>"  ·  now +8": what the hero has in that slot, in the stat this part leads with, or that the slot is free.</summary>
        public static string Against(GameState state,MemberState member,TechPartDef def,PartInstance item=null)
        {
            var lead=Lines(Total(def,item)).FirstOrDefault(Counts);if(lead.name==null)return "";
            if(!member.equipped.TryGetValue(def.slot,out string id))return "  ·  slot empty";
            var worn=state.parts.Find(p=>p.instanceId==id);var wornDef=worn!=null?GameCatalog.Find<TechPartDef>(worn.definitionId):null;
            if(wornDef==null)return "  ·  slot empty";
            if(worn==item)return "";
            return $"  ·  now +{Lines(Total(wornDef,worn)).First(l=>l.name==lead.name).value:0}";
        }
        /// <summary>Who has it on, in words: the bracketed E read as a key prompt, and a partner's parts looked free.</summary>
        public static string Mark(MemberState wearer,MemberState viewer)=>wearer==null?"":wearer==viewer?"Equipped · ":DisplayNames.Speaker(wearer.id)+" · ";
        public static string Affixes(PartInstance item)=>string.Concat(item.affixes.Take(2).Select(name=>"  ·  "+(GameCatalog.Find<AffixDef>(name)?.displayName??name)));
        /// <summary>What breaking it down gives: the part's own yield, and with the part itself, half of what its upgrades took.</summary>
        public static string Yield(TechPartDef def,PartInstance item=null)=>string.Join(", ",Inventory.SalvageYield(def,item).Select(i=>$"{i.count} {DisplayNames.Item(i.id)}"));
        /// <summary>What a consumable does, in a row's words. A heal is a share of the hero's own bar, with its floor where it has one: a gel is worth using at Sync 30 and still mends a new hero.</summary>
        public static string Restores(ConsumableDef def)
        {
            if(def==null)return "";
            if(def.revive)return $"raises a downed partner at {Percent(def.healPercent)} Integrity";
            if(def.seconds>0)return $"both heroes: {Says(def.buff)} for {def.seconds:0} s";
            string heal=def.healPercent>0?$"{Percent(def.healPercent)} Integrity"+(def.heal>0?$" (min {def.heal:0})":""):def.heal>0?$"{def.heal:0} Integrity":"";
            string charge=def.charge>0?$"{def.charge:0} Charge":"";
            return "restores "+(heal!=""&&charge!=""?heal+" and "+charge:heal+charge)+(def.party?" to both heroes":"");
        }
        /// <summary>A hero's slots at a glance, the empty ones named.</summary>
        public static string Wearing(GameState state,MemberState member)
        {
            TechPartDef Def(string id){var item=state.parts.Find(p=>p.instanceId==id);return item!=null?GameCatalog.Find<TechPartDef>(item.definitionId):null;}
            var worn=new List<string>();var empty=new List<string>();
            foreach(GearSlot slot in System.Enum.GetValues(typeof(GearSlot)))
            {
                var def=member.equipped.TryGetValue(slot,out string id)?Def(id):null;
                if(def!=null)worn.Add(def.displayName);else empty.Add(slot.ToString());
            }
            if(!string.IsNullOrEmpty(member.module2)){var second=Def(member.module2);if(second!=null)worn.Add(second.displayName);}
            string who=DisplayNames.Speaker(member.id);
            return (worn.Count>0?$"{who} wears {string.Join(", ",worn)}.":$"{who} wears no parts yet.")+(empty.Count>0?$" Empty: {string.Join(", ",empty)}.":"");
        }
    }
}
