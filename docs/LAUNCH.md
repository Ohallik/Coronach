# Launcher message for the build session

Open Claude Code in `C:\Users\natem\Projects\SpaceRPG` (a session with image-generation
capability) and paste the block below. It hands the session `SLICE_PROMPT.md`, which carries
everything else.

```
Build the LATTICE playable slice.

Read docs/SLICE_PROMPT.md and follow it end to end. It tells you which
documents to read first (docs/GAME_DESIGN.md, docs/ARCHITECTURE.md,
docs/ASSET_SOURCES.md, the generated-3d-characters skill, and Frostbound's
CONTROLLER_SUPPORT.md), then runs six gated phases from an empty Unity
project to a build I can play on my Logitech gamepad, which is plugged in now.

You have an image-generation tool in this session. Use it for every 2D image
the prompt asks for (species sheets, character boards, portrait grids,
textures, skyboxes, UI). Use the Meshy bridge at
C:\Users\natem\Projects\FrostboundUnity\tools\meshy for 3D models. All visible
art is generated; the downloaded packs are stand-ins, donors, particles,
glyphs, fonts and sound only.

Work autonomously. The only time you stop for me is the P1 species gate:
generate the species sheet and the Taren/Sela boards, write the image paths
into docs/PROGRESS.md under NATHAN GATE, tell me, and keep building P2 while
you wait. Every other decision is yours; record it in docs/DECISIONS.md.
Meshy budget is 1,200 credits. Commit at every gate.

Start with P0 now.
```

If the session is resumed later, paste the same block with the last line replaced by
"Read docs/PROGRESS.md and continue from the first open gate."
