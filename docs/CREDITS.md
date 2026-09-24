# CORONACH credits

Original game: Nathan. Original generated art: LATTICE image generation and Meshy pipeline; provenance in `docs/art` and `art-src/Generated` prompt archives.

Original music supplied by Nathan: **Title Theme**, **Hub Town Groove**, **Moonbase Market**, **Adventure Awaits**, **Riverbed Stomp**, **Seed Shakers of Xylos**, and **Shop Theme**. The first four are integrated in the current slice; the remaining tracks are reserved in the full-game atlas. Original OGG files remain in `Music/`.

Reusable code and tooling adapted from Nathan's FrostboundUnity project. No Frostbound character, portrait, environment or UI image art is included.

- Unity Engine and Unity packages: Unity Technologies, applicable Unity package and engine licences.
- TextMesh Pro essentials and Liberation Sans font: copied from Frostbound's installed TMP essentials. Font licence included at `Lattice/Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` (SIL Open Font License).
- Yarn Spinner 3.2.4: Yarn Spinner contributors, MIT licence (package licence included by Package Manager).

- Kenney Sci-Fi Sounds, CC0: eight OGG effects/ambiences staged from the owned SpaceRPG archive through `PackStaging`. Source: https://kenney.nl/assets/sci-fi-sounds . Licence: `Lattice/Assets/_Project/Audio/Licenses/Kenney-SciFiSounds.txt`.
- Kenney Particle Pack, CC0: `flare_01`, `circle_02`, `slash_01` only, used for shaping, hits, trails, boost and bursts. Licence: `Lattice/Assets/_Project/Art/Particles/License.txt`. Source: https://kenney.nl/assets/particle-pack . This is the particle-texture exception explicitly allowed by the slice prompt.
- Real-ESRGAN NCNN Vulkan (portrait, sky and planet-map production tool only): Xintao Wang and contributors, BSD-3-Clause; uses the existing read-only tool/model installation in Frostbound's `art-src/realesrgan`. No demonstration images are imported into LATTICE.
- Quaternius Universal Animation Library, CC0: skeleton and Idle, Walk, Sword Attack, Roll and Hit Chest animation clips only. Source archive: Frostbound's owned `Quaternius_UniversalAnimationLibrary`; staged licence `Lattice/Assets/_Project/Art/Animation/UAL-License.txt`. The donor export contains zero mesh renderers.
- Quaternius Universal Animation Libraries 1 and 2, CC0: the Coronach presentation pass adds curated jog, sprint, sword combo, guard, dash, ranged and cast clips. `HeroLocomotion.fbx` and `HeroCombat.fbx` contain skeletons and animation only; no donor meshes. Runtime characters remain generated art. The second library licence is staged as `Lattice/Assets/_Project/Art/Animation/UAL2-License.txt`.

- Quaternius Ultimate Animated Animals, CC0: Fox skeleton and Idle, Walk and Attack clips only, grafted onto the generated Ridgehound. Zero donor meshes are staged. Licence: `Lattice/Assets/_Project/Art/Animation/Fox-License.txt`.

No downloaded model, portrait, environment image, panel or inventory icon is final LATTICE art. All 50 generated production models and the paid task costs are indexed in `docs/art/gen-manifest.json`; image sources and processing are indexed in `docs/art/image-manifest.json`, `docs/art/portrait-catalog.json` and `docs/art/world-upscale.json`.
