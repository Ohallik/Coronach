using System.Globalization;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using UnityEngine;

namespace Lattice.UI
{
    // "wearBest": the driver wears the best it holds, as a player does. It wore only what the game put into an empty slot, so from chapter 3 on every route and every
    // boss time was a party in its chapter 1 and 2 parts with the chapter's floor in the bag (after route 62: a Refined Edge on Taren, two Choirwrought Edges spare).
    // What to wear is ReplayGear's (the Gear page's own numbers); this puts it on with the page's own call, out of any fight and any talk, and says so in the log.
    public sealed partial class ContinuousReview
    {
        /// <summary>An awake enemy this near still makes it a fight for changing parts: the lock's reach. The game's own calm (GearEffects.InFight) is 15 m, which a
        /// shooter stands outside.</summary>
        const float WearCalm = 30;
        /// <summary>How often the driver looks whether there is anything to put on: a thumb doesn't open the Gear page every frame, and the looking costs.</summary>
        const double WearLook = .25;
        /// <summary>The most changes in one look: five slots and module slot 2 for each hero out, and room for a part that moves down a slot.</summary>
        const int WearMost = 16;
        /// <summary>For a case's broken state only (WearBestTests), off otherwise: the driver as it might have been written, putting parts on whenever it looks (in a fight,
        /// in a talk), or taking them off whoever wears them.</summary>
        static bool wearAnyTime, wearFromAnyone;
        double nextWearLook = -1;
        bool wearSafe;
        int wearSeen = int.MinValue;

        /// <summary>Out of any fight and any talk: the world has the pad, no conversation runs, no boss's fight is on about the lead and nothing awake is within the lock's reach.</summary>
        bool SafeToWear(PartyController party)
        {
            if (ObserveUi() != "world" || GameTime.Paused || GameInput.Current == null || GameInput.Current.Blocked) return false;
            if (DialogueSystem.Current != null && DialogueSystem.Current.Running) return false;
            var at = party.Active.transform.position;
            return !GearEffects.InFight(at) && !EnemyBrain.AwakeNear(at, WearCalm);
        }
        /// <summary>What has changed that a player would look at the Gear page for: the bag's size and who is out.</summary>
        static int WearSignature(GameState state, System.Collections.Generic.List<MemberState> heroes)
        {
            int seen = state.parts.Count;
            foreach (var hero in heroes) if (hero != null) seen = seen * 31 + hero.id.GetHashCode();
            return seen;
        }
        /// <summary>Each frame of a route that asks for it: when the party is first safe in the route, when it is safe again after a fight or a talk, and when the bag or the
        /// pair out has changed since the last look, every hero out is put into the best spare for each slot, the biggest step first.</summary>
        void WearWhenSafe()
        {
            if (route == null || !route.wearBest || retrying) return;
            double now = clock.Elapsed.TotalSeconds;
            if (now < nextWearLook) return;
            nextWearLook = now + WearLook;
            var party = PartyController.Current; var state = GameServices.Current != null ? GameServices.Current.State : null;
            if (party == null || party.members == null || party.Active == null || state == null || state.parts == null || SceneFlow.Current == null || SceneFlow.Current.Loading) { wearSafe = false; return; }
            bool safe = wearAnyTime || SafeToWear(party), began = safe && !wearSafe;
            wearSafe = safe;
            if (!safe) return;
            var heroes = Deployment.Of(state);
            if (!began && WearSignature(state, heroes) == wearSeen) return;
            var inventory = RpgServices.Inventory; MemberState lead = null;
            foreach (var hero in heroes) if (hero != null && hero.id == party.Active.character) lead = hero;
            bool changed = false;
            for (int i = 0; i < WearMost; i++)
            {
                var wear = ReplayGear.Next(state, heroes, lead, wearFromAnyone);
                if (wear == null) break;
                var from = wearFromAnyone ? inventory.Wearer(wear.part) : null;
                // The Gear page's own call: it holds every rule the page does (a module alone in module slot 2, one part on one hero).
                if (!inventory.Equip(wear.hero, wear.part, wear.def, wear.second)) { Debug.Log("QUALITY_WEARS_REFUSED " + wear.hero.id + " " + wear.SlotName + " " + wear.def.id); break; }
                changed = true;
                Debug.Log("QUALITY_WEARS " + wear.hero.id + " " + wear.SlotName + " " + wear.def.id + " (was " + (wear.wasDef != null ? wear.wasDef.id : "nothing") + ")" + (from != null && from != wear.hero ? " off " + from.id : ""));
            }
            if (changed) party.RefreshStats();
            wearSeen = WearSignature(state, heroes);
        }

        // ---------- a part bought at a stall: a stock step on a part's row ----------
        // The same step that fills the pouch with gels (ContinuousReview.BeginStock), on a part's row with a count: the step's button is pressed on the row until the
        // party holds that many of the part, as the pair out can wear them (spare, or on a hero out: what a resting hero keeps is never taken, ReplayGear), or until a
        // press buys nothing. Short of its count, unlike a gel stock, the step fails and says what it had to spend: a route buys a part because a boss is sized on it.
        TechPartDef stockPart;
        int stockScrip;
        /// <summary>How many of the stocked thing the party holds: a consumable in the pouch, or a part the pair out can wear.</summary>
        int StockHeld() => stockPart != null ? PartsHeld(GameServices.Current.State, stockPart) : stockItem != null ? Carried(stockItem) : 0;
        public static int PartsHeld(GameState state, TechPartDef def)
        {
            var resting = Deployment.Resting(state); var inventory = new Inventory(state); int held = 0;
            foreach (var part in state.parts)
            {
                if (part == null || part.definitionId != def.id) continue;
                var wearer = inventory.Wearer(part);
                if (wearer == null || !resting.Contains(wearer)) held++;
            }
            return held;
        }
        /// <summary>A stock step whose row is a part's (its displayName or id): stocked to a count, never "cap". True when the row is a part's, whatever else is wrong.</summary>
        /// <summary>For a case's broken state only (PartPurchaseTests), off otherwise: the driver as it was, which stocked a consumable's row and no other.</summary>
        static bool partStockOff;
        bool BeginPartStock()
        {
            if (partStockOff) return false;
            foreach (var def in GameCatalog.All<TechPartDef>()) if (def != null && (def.displayName == step.pickRow || def.id == step.pickRow)) stockPart = def;
            if (stockPart == null) return false;
            if (!int.TryParse(step.stock, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) || count <= 0)
            {
                failures.Add(step.name + ": a part is stocked to a count of it, not \"" + step.stock + "\"");
                stockPart = null; return true;
            }
            var state = GameServices.Current.State;
            stockTarget = count; stockBefore = stockAtPress = PartsHeld(state, stockPart); stockScrip = state.scrip;
            return true;
        }
        /// <summary>A part's stock that ended short of its count fails the step, with what the party had to spend.</summary>
        void PartStockShort(int after)
        {
            if (stockPart == null || after >= stockTarget) return;
            failures.Add(step.name + ": bought " + (after - stockBefore) + " " + stockPart.displayName + ", and the pair needed " + (stockTarget - stockBefore) +
                " (holding " + after + " of " + stockTarget + "): the stall said no with " + GameServices.Current.State.scrip + " scrip left of " + stockScrip);
        }
    }
}
