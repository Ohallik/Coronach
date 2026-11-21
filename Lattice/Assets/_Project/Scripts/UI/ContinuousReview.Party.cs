using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Lattice.UI
{
    // The driver's dealings with a party of three (chapter 6 on, when Idra joins and one hero rests): who the partner is,
    // by role and never by a place in an array; a lead it is asked for and can't have; a resting hero brought out as a
    // player would bring them out; and the pair a route names. Ordinary pad presses and what a player reads off the
    // screen, as everywhere in the driver: the game still refuses whatever it would refuse a person.
    public sealed partial class ContinuousReview
    {
        /// <summary>How many presses of Swap an ensureCharacter step spends, half a second apart, before it gives up and says why. A step
        /// used to press until its seconds ran out, behind a hero who was down or who isn't out at all; every step the routes have ever
        /// written is 1.2 s, three presses at the most, and plays exactly as it did.</summary>
        public const int EnsureSwaps = 6;
        /// <summary>The most the driver presses in a menu to bring one resting hero out: open, a page or three, a row or three, the press, close.</summary>
        const int EnsureMenuPresses = 30;
        const double EnsureBeat = .3, EnsureHold = .12;
        QualityStep ensureFor;
        QualityRoute pairCheckedFor;
        int ensureSwaps, ensureMenu;
        bool ensureGaveUp, ensureOpened;
        double ensureTapAt = -1;
        GamepadButton ensureButton;

        // ---------- the partner, by role ----------
        /// <summary>The hero out beside the lead (PartyController.Partner: with more than one, whoever is down, since that is who the lead
        /// must reach). It was read as members[1-index], which is members[-1] for a lead in a third place, and an exception in the
        /// log rejects a run.</summary>
        static CombatActor Partner(PartyController party) => party != null && party.members != null && party.members.Length > 1 ? party.Partner : null;
        static bool PartnerDown(PartyController party)
        {
            var partner = Partner(party);
            return partner != null && !partner.Health.Alive && party.Active.Health.Alive;
        }
        static bool PartyAlive(PartyController party)
        {
            if (party == null || party.members == null || party.members.Length < 2) return false;
            foreach (var member in party.members) if (member == null || !member.Health.Alive) return false;
            return true;
        }
        /// <summary>Any hero out beyond the lead and the partner, for the captures. Null with two on the field, which is every run there
        /// has been, so nothing is made per frame.</summary>
        static CombatActor[] Others(PartyController party, CombatActor lead, CombatActor partner)
        {
            if (party == null || party.members == null || party.members.Length <= 2) return null;
            var others = new System.Collections.Generic.List<CombatActor>();
            foreach (var member in party.members) if (member != null && member != lead && member != partner) others.Add(member);
            return others.ToArray();
        }
        /// <summary>A lead who shoots on foot holds a chase at range (rangedTolerance) where the others close to arm's length. The game
        /// itself knows its one shooter by her name (CombatActor's attack, PartnerBrain's reach), so that is what is asked, here and
        /// nowhere else in the driver: Idra's hammer closes, as Taren's edge does. If a hero's style ever says it, read it here.</summary>
        static bool ShootsOnFoot(CombatActor actor) => actor != null && actor.character == "Sela";
        /// <summary>"until":"lead:Idra": that hero leads, and the world has the pad again.</summary>
        static bool Leads(string hero)
        {
            var party = PartyController.Current;
            return party != null && party.Active != null && party.Active.character == hero && !GameInput.Current.Blocked && !GameTime.Paused;
        }

        // ---------- ensureCharacter: the lead a step asks for ----------
        /// <summary>The step asked for a lead it can't have: it ends here, failed, instead of running out its seconds.</summary>
        bool EnsureGaveUp => ensureGaveUp && ReferenceEquals(ensureFor, step);
        GamepadState GiveUp(GamepadState state, string why)
        {
            ensureGaveUp = true;
            failures.Add(step.name + ": ensureCharacter " + step.ensureCharacter + ": " + why);
            Debug.Log("QUALITY_ENSURE_FAILED " + step.name + ": " + why);
            return state;
        }
        /// <summary>What ensureCharacter adds to the pad this frame. A hero out on the field: Swap, half a second apart, as it always
        /// pressed, and no more than <see cref="EnsureSwaps"/> times. A hero resting: out through the Party page where the game lets a
        /// player change the pair (BringOut), and a failure at once where it doesn't. A name the party hasn't got: a failure at once.</summary>
        GamepadState Ensure(GamepadState state, PartyController party)
        {
            if (step == null || string.IsNullOrEmpty(step.ensureCharacter) || party == null || party.Active == null) return state;
            if (!ReferenceEquals(ensureFor, step)) { ensureFor = step; ensureSwaps = ensureMenu = 0; ensureGaveUp = ensureOpened = false; ensureTapAt = -1; }
            if (ensureGaveUp) return state;
            string want = step.ensureCharacter; double now = clock.Elapsed.TotalSeconds;
            CombatActor body = null;
            foreach (var member in party.members) if (member != null && member.character == want) body = member;
            if (body == null)
            {
                if (Deployment.PartyIndex(GameServices.Current.State, want) < 0) return GiveUp(state, "nobody called " + want + " is in the party");
                return BringOut(state, want, now);
            }
            if (ensureOpened)
            {
                // They are out. The menu that brought them out is closed with the button that opened it, and Swap answers in the world.
                if (now < ensureTapAt + EnsureHold) return state.WithButton(ensureButton);
                if (now < ensureTapAt + EnsureBeat) return state;
                string ui = ObserveUi();
                if (ui == "pause" || ui == "bench")
                {
                    if (++ensureMenu > EnsureMenuPresses) return GiveUp(state, "the menu that brought " + want + " out never closed");
                    ensureButton = GamepadButton.Start; ensureTapAt = now;
                    return state.WithButton(ensureButton);
                }
                ensureOpened = false;
            }
            if (party.Active.character == want || GameInput.Current.Blocked || now - lastSwapPress <= .5) return state;
            if (ensureSwaps >= EnsureSwaps)
                return GiveUp(state, body.Health.Alive ? "Swap was pressed " + EnsureSwaps + " times and " + party.Active.character + " still leads" : want + " is down");
            ensureSwaps++; lastSwapPress = now;
            return state.WithButton(GamepadButton.North);
        }
        /// <summary>A hero who is resting comes out as a player brings them out: Start opens the pause menu on its Party page (at a bench,
        /// LB turns to it), the d-pad finds the hero's row by its name, and a press on it puts them out in the partner's place. Only
        /// where the game allows it, and only on a step that says the menu may hold the game ("pauseUi"), since a pause nobody declared
        /// rejects the run. One press at a time, held and let go as a thumb does.</summary>
        GamepadState BringOut(GamepadState state, string want, double now)
        {
            if (now < ensureTapAt + EnsureHold) return state.WithButton(ensureButton);
            if (now < ensureTapAt + EnsureBeat) return state;
            string ui = ObserveUi(); bool menu = ui == "pause" || ui == "bench";
            if (!menu && ui != "world") return state;
            if (!PartyField.MayChange(ui == "bench"))
                return GiveUp(state, want + " is resting, and the party can only be changed in a safe zone or at a bench");
            string needs = menu ? ui : "pause";
            if (step.pauseUi != needs)
                return GiveUp(state, want + " is resting: bringing them out opens the Party page, so the step needs \"pauseUi\":\"" + needs + "\"");
            if (++ensureMenu > EnsureMenuPresses) return GiveUp(state, "the Party page never brought " + want + " out");
            string row = PartyPairUi.RowName(want);
            ensureButton = !menu ? GamepadButton.Start : OnRow(row) ? GamepadButton.South : GameObject.Find(row) != null ? GamepadButton.DpadDown : GamepadButton.LeftShoulder;
            ensureOpened = true; ensureTapAt = now;
            return state.WithButton(ensureButton);
        }

        // ---------- the pair a route names ----------
        /// <summary>A route may name the two heroes it is played with ("deployed"). Starting from a loadout they are put out with it, as
        /// its Sync and its gear are: a fixture's doing, before the zone loads, not an input.</summary>
        public static void ApplyPair(QualityRoute route)
        {
            if (route == null || route.deployed == null || route.deployed.Length == 0 || route.scene == "Title") return;
            if (!Deployment.Set(GameServices.Current.State, route.deployed))
                Debug.Log("QUALITY_PAIR_UNSET " + string.Join(",", route.deployed) + ": not two different heroes of this party");
        }
        /// <summary>Continuing a save, the pair is whatever the save has out. If that isn't the pair the route names, the run fails at the
        /// first step that has a party, saying both, and not twenty steps later behind the wrong partner.</summary>
        void CheckPair()
        {
            if (route == null || ReferenceEquals(pairCheckedFor, route) || route.deployed == null || route.deployed.Length == 0) return;
            var party = PartyController.Current;
            if (party == null || party.members == null || SceneFlow.Current == null || SceneFlow.Current.Loading) return;
            pairCheckedFor = route;
            bool same = party.members.Length == route.deployed.Length;
            string named = "", found = "";
            foreach (string hero in route.deployed)
            {
                bool there = false;
                foreach (var member in party.members) if (member != null && member.character == hero) there = true;
                same &= there; named += (named == "" ? "" : " and ") + DisplayNames.Speaker(hero);
            }
            foreach (var member in party.members) if (member != null) found += (found == "" ? "" : " and ") + DisplayNames.Speaker(member.character);
            if (!same) failures.Add("route: deployed names " + named + ", and the save has " + found + " out");
        }
    }
}
