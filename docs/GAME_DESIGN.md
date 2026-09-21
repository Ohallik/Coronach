# LATTICE (working title) — game design, playable-slice scope

Status 2026-09-20. Every proper noun below is a placeholder Nathan can rename; the mechanics are
the decisions. Companion documents: `ARCHITECTURE.md` (how it is built) and `SLICE_PROMPT.md`
(the hand-off that builds it).

## 1. Pillars

1. **Sync.** The Vael are a bipedal species who bond with living-metal machinery called the
   Lattice. Every Vael can *link* (fly a ship, run a drill). A rare few, the **Shapers**, can
   *shape*: the Lattice flows over their bodies and becomes armour, wings, blades and guns. The
   party are Shapers.
2. **One body, three forms.** The hero is never "in a vehicle". The same person walks the Decks
   in a natural body, fights on a moon in a Shaped body, and crosses space in Flight. Form is
   decided by the zone, never by a menu.
3. **Ys-fast action on both surfaces.** Ground combat is Ys VIII: fast combos, perfect dodge
   (Flash Move) and perfect guard (Flash Guard), break meter, two-member party with instant swap.
   Space combat keeps the same buttons and the same reads, on a flight plane with inertia.
4. **Warp tunnels are the dungeons.** Big, hand-authored, owned by other species, full of
   space-faring life. A tunnel is a route between places and a gauntlet in one.
5. **Tech parts are the loot, fabrication is the second progression.** Combat levels and crafting
   levels are separate ladders.
6. **HD-2D presentation.** Low-poly 3D rendered as a diorama: low internal resolution, tilt-shift
   depth of field, bloom on the sync-lines, fixed three-quarter camera.
7. **Every named face has sixteen expressions**, in the natural body and (for Shapers) in the
   Shaped body.

## 2. The Vael — species design

Designed to be unmistakably alien and immediately likeable at portrait scale and at 180 px tall in
the diorama. The point of the design is the **sync-lines**.

**Shared anatomy**

- Plantigrade bipeds, 1.80–2.00 m, long-limbed, sturdy through the shoulders. Four fingers and a
  thumb. Forearms a little longer than human (elbow to wrist ≈ 1.1× upper arm). No tail.
- Skin: smooth and matte, a dusk range from slate-blue through warm grey; lighter on the face,
  throat, palms. No scales, no fur, no gloss.
- **Sync-lines**: thin bioluminescent filaments under the skin in a branching pattern — temple to
  jaw hinge, collarbone to sternum, elbow down the forearm to the back of the hand, knee to
  shin. Barely visible when relaxed; they glow when the Vael links, and blaze when a Shaper shapes.
  Each individual has a personal hue.
- Head: cranium slightly elongated and swept back. **No hair.** From the crown down the back of the
  skull grows a crest of soft keratin **plumes** — flat, overlapping, feather-like fronds. Plumes
  lie down when calm, rise when alarmed, fan when delighted. They carry personal colour the way
  hair does. This is the expression instrument for portraits.
- Eyes: large, almond, bright ring-iris on a pale sclera (readable whites are deliberate). A thin
  inner lid flicks sideways on a blink.
- No external ears; a low sensory ridge behind the jaw. A small soft nose with a low bridge.
  A normal mouth with slightly darker lips. A faint keel-ridge down the sternum where the core
  node sits.

**Sex differentiation (visible at any size)**

- Male: broader shoulders, squarer jaw, heavier brow ridge. Plumes shorter and thicker, swept
  back like a fin or a crest. Sync-lines bolder and fewer.
- Female: narrower face, higher cheek ridge. Plumes longer and finer, falling past the shoulders
  like a plume-mane. Sync-lines finer and more branched. Same height range.

**Dress — Natural form.** Layered asymmetric wraps and sashes over a fitted underlayer, muted
tones with one accent per character. Minimal tech shows: a metal **collar node** at the base of
the neck, **wrist bands**, and a **spine plate** visible above the collar. Those are docking
points; everything Shaped grows from them. Wearing so little Lattice in town is a courtesy — it
makes the Linked comfortable.

**Shaped form (ground combat).** From the nodes, Lattice flows out into segmented plating over
forearms, shins, shoulders and chest. A light **visor** forms across the eyes. The plumes are
sheathed in thin metal ribs. Sync-lines blaze in the character's hue. The **emitter** (gun) forms
on the off-hand forearm; the **edge** (blade) extends from the main-hand wrist as a light blade.
Four **back-vanes** sit folded along the spine. Silhouette: angular, swept collar, still clearly
the same person (face, plumes, build).

**Flight form (space).** Shaped form with the back-vanes unfolded into four thin thruster wings,
legs together, body horizontal. Reads as an angelic mech-diver. In **safe** space the vanes sit
half-folded, weapons retract, lines dim: "civil flight".

## 3. Cast for the slice

| Name | Role | Sex | Plumes / sync hue | Voice |
|---|---|---|---|---|
| **Taren Vosk** | Shaper, starting hero, edge-first | M | dark slate plumes, copper tips / amber-gold | dry, steady, curious |
| **Sela Irun** | Shaper, joins in the hub, emitter-first | F | pale violet-white plumes / cyan | quick, sharp, teasing |
| **Orrin Malk** | Dockmaster of Cinder Halo, quest giver | M | grey plumes, cropped / dim green | gruff, fond |
| **Mira Tessen** | Outfitter (item shop) | F | rust-red plumes / orange | brisk, salesy |
| **Hal Quen** | Repair bay (the inn) and fabrication bench | M | pale plumes, one missing / blue | slow, kind |
| **Neve** | Pilot you hail in the lanes; tunnel rumours | F | black plumes / white | restless |

Portraits: Taren and Sela get **two** 16-cell sheets each (Natural, Shaped). NPCs get one (Natural).

## 4. Forms and zones

| ZoneKind | Form | Combat | Interact |
|---|---|---|---|
| GroundSafe (the Decks, outposts) | Natural | none, attack disabled | A talks / examines |
| GroundCombat (Sorrel Ridges) | Shaped | Ys ground rules | A when a prompt shows |
| SpaceSafe (Cinder Halo lanes) | Civil Flight | none | A hails ships / docks |
| SpaceCombat (the Gullet) | Flight | flight rules | A when a prompt shows |

Transitions are a 0.6 s shaping animation (Lattice blooms from the nodes, VFX + mesh swap) and
happen on zone entry, never mid-zone. Entering a safe pocket inside a combat zone (the outpost
interior) reverts to Natural.

## 5. Combat

### Shared core
- **Integrity** (HP), **Charge** (skill resource, builds on hits, decays slowly), **Thrust** (boost
  meter, flight only; also fuels ground sprint).
- Four damage types: **Beam**, **Plasma**, **Kinetic**, **Pulse**. Every enemy has one weakness
  (×1.5 damage, double break) and one resistance (×0.5). Weapons have a type; skills have a type.
- **Break**: enemies carry a break meter that fills on hits (weakness hits fill it twice as fast).
  Broken enemies stagger for 3 s and take ×1.5 from everything.
- **Flash Move**: dodge in the 0.2 s before a hit lands → 1.5 s of world slow-mo, the dodger at
  full speed, invulnerable.
- **Flash Guard**: guard in the 0.2 s before a hit lands → no damage, Charge +25%, next attack
  crits.
- **Two-member party (Ys VIII).** One controlled, one AI partner at 60% damage, targets what the
  player targets, revives when the player stands over them for 2 s. **Swap** is instant and the
  incoming member enters with a swap strike.
- Lock-on soft-targets the nearest enemy in the facing cone; d-pad left/right cycles.

### Ground (Ys)
- Move 8-way (digital pad fully supported), sprint on RT (drains Thrust slowly).
- A: attack combo (3 hits, edge or emitter by character, cancelable into dodge).
- B: dodge (i-frames 0.25 s). X: guard (hold). Y: swap.
- RB + A/B/X/Y: skills 1–4. Charge cost, short cooldown.
- LT: quick item. LB: lock-on. D-pad up/down: cycle quick item.

### Flight ("skate")
- Plane-locked flight with inertia: left stick/d-pad = thrust direction; releasing coasts.
- RT: boost (Thrust drains, regenerates at rest). LT: brake/drift (hard decel, tight turn).
- A: emitter fire (hold; auto-aims at the locked target within a 30° cone).
- X: **blade lunge** — dash 8 m at the target and slash (the melee-while-flying pillar).
- B: barrel roll (i-frames 0.3 s, Flash Move on perfect). Y: swap (the partner flies alongside).
- RB + face: the same four skills in flight variants.
- Tunnel walls deal Kinetic contact damage and shove; membranes gate encounter chambers.
- Guard has no flight equivalent; Flash Guard becomes a **Flash Roll** counter.

### Skills for the slice (each has a ground and a flight variant, same name)
- Taren: **Cleave Arc** (Kinetic sweep), **Ember Lunge** (Plasma dash), **Pulse Break** (Pulse
  burst, big break), **Overdrive** (buff: Output +20% for 10 s).
- Sela: **Lance** (Beam pierce), **Scatter** (Plasma spread), **Static Net** (Pulse field, slows),
  **Refract** (guard-counter beam).

## 6. RPG layer

**Sync Level** (combat level). XP from kills and quests. Stats: **Output** (attack), **Plating**
(defence), **Response** (speed, dodge window, crit rate), **Resonance** (skill power, Charge gain),
**Fortune** (crit damage, drop luck). Growth per character.

**Gear = tech parts.** Slots per character: **Emitter** (ranged), **Edge** (melee), **Frame**
(armour), **Drive** (thruster: flight speed, roll distance, ground dash), **Module ×2** (passives).
Parts have a damage type (weapons), tier (T1 Scrap / T2 Refined / T3 Prime), upgrade level +0..+5,
0–2 affixes. Salvage a part → materials.

**Fabrication** (crafting) has five disciplines with their own levels: Emitters, Edges, Frames,
Drives, Modules. Crafting anything grants discipline XP (recipe tier × 10). Levels unlock recipes
and tiers, and raise the chance of an **overclock** (a free affix). Recipes need materials:
Scrap Alloy, Lattice Filament, Ridge Crystal, Husk Core, Choir Resin, Cantor Pearl. Benches: Hal's
bay in the Decks and any Lattice Anvil in the world; consumables can be fabricated anywhere from
the menu.

**Economy.** Currency: **scrip**. Outfitter sells consumables, T1 parts, materials; buys parts.
Repair bay restores Integrity/Charge for a fee scaled to Sync Level and is a save point.

## 7. World of the slice

1. **Cinder Halo** (SpaceSafe) — a ring-city orbiting the gas giant **Vorun**. Flyable exterior
   with lanes of NPC ships, three docking pods (Dock Office, Outfitter, Repair Bay), a warp beacon
   on the outer edge (locked until the moon quest is done), a few hailable ships (Neve).
2. **The Decks** (GroundSafe) — one interior scene with three rooms joined by a corridor; you
   arrive in the room of the pod you docked at. Orrin, Mira, Hal live here. Fabrication bench.
3. **Sorrel** (GroundCombat) — a dim ochre moon. Landing pad → **Sorrel Outpost** (safe pocket,
   one survivor NPC line) → **the Ridges**: crystal ridgelines, three enemy types, mineable Ridge
   Crystal nodes, a Lattice Anvil, the mini-boss **Burrower** at the drill site. Reward: the warp
   key.
4. **The Gullet** (SpaceCombat) — a warp tunnel owned by **the Choir**, singing bioluminescent
   fauna. Four chambers gated by membranes, hazards (pulsing walls), a mid-tunnel salvage field,
   the boss **Cantor** (segmented serpent, weak-point segments). ~8–10 minutes.
5. **Tallow Drift** (SpaceSafe → GroundSafe) — a tiny waystation with one NPC and a save point.
   Reaching it ends the slice.

**Quest spine.** Orrin: the Sorrel outpost went quiet → land, clear the Ridges, find the survivor
(and the key) → return or use the key at the beacon → cross the Gullet → Tallow Drift. Side: Mira
wants six Ridge Crystal; Hal teaches fabrication with a free T1 recipe; Neve's hail unlocks a
salvage cache in the Gullet.

## 8. Enemies for the slice

| Enemy | Zone | Shape | Weak / resist | Behaviour |
|---|---|---|---|---|
| Ridgehound | Sorrel | quadruped, rigged (donor graft) | Plasma / Kinetic | pack of 3, lunge, circle |
| Scrapmite | Sorrel | small skitterer, static + procedural | Pulse / Beam | swarm of 6, weak, explode on death |
| Sentinel Husk | Sorrel | armoured biped, rigged | Kinetic / Plasma | slow, telegraphed slam, shield up when hit by resisted type |
| Burrower (mini-boss) | Sorrel | large, static + procedural | Beam / Pulse | surfaces, spits, burrow-charge, 2 phases |
| Chorister Dart | Gullet | small flier, static | Beam / Pulse | swarm, dive-bomb |
| Chorister Drifter | Gullet | spitter, static | Pulse / Beam | keeps range, volleys |
| Shellmine | Gullet | anchored, static | Kinetic / Plasma | detonates on proximity, blade-lunge safe |
| Cantor (boss) | Gullet | segmented serpent, procedural | Plasma / Kinetic | weak-point segments, sweep, song stun, 3 phases |

## 9. Presentation

- Camera: fixed three-quarter, pitch ≈ 40°, yaw fixed per zone, FOV 30°, hero ≈ 180 px tall at
  1080p. Follow with soft dead-zone on ground; velocity look-ahead and mild zoom-out with speed in
  flight. No player rotation.
- HD-2D stack (URP): internal 960×540 target with nearest upscale, tilt-shift DoF (top and
  bottom bands), bloom keyed to emissives, LUT grade per zone, light vignette, toon shading with
  thin outlines.
- Skybox: painted nebula equirect per space zone. Tunnel: scrolling membrane shader on a spline
  tube.
- UI: sci-fi panel art (generated), pad glyphs, typewriter dialogue with portrait slide-in.
- Audio: Kenney sci-fi SFX now; Nathan's music later (silence + ambience loops in the slice).

## 10. Controls

Gamepad first. The desk pad is a Logitech Precision (DirectInput, digital d-pad, ten buttons, no
sticks); everything below is reachable on it. XInput pads add analog movement.

| Button | Ground | Flight | Safe zones |
|---|---|---|---|
| Stick / d-pad | move | thrust direction | move / fly |
| A | attack | fire emitter | interact / advance |
| B | dodge | roll | cancel |
| X | guard | blade lunge | — |
| Y | swap | swap | swap |
| RB (hold) + A/B/X/Y | skills 1–4 | skills 1–4 | — |
| LB | lock-on | lock-on | — |
| RT | sprint | boost | boost |
| LT | quick item | brake / drift | brake |
| D-pad ↑↓ | cycle quick item | cycle quick item | — |
| D-pad ←→ | cycle target | cycle target | — |
| Start | pause menu | pause menu | pause menu |
| Select | quest log / map | quest log / map | quest log / map |

Keyboard: WASD move, J attack, Space dodge, K guard, Tab swap, 1–4 skills, Q lock-on, Shift
sprint/boost, F item, E interact, Esc menu, M log.

## 11. Out of scope for the slice

Story beyond the quest spine, characters three and four, more than one tunnel, console input
glyphs, localisation, music, cutscene direction beyond portrait dialogue, difficulty modes.
