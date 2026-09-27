# Sorrel working-basin blockout checkpoint

September 26, Codex review. **Blockout and final MAP_EYE_TEST remain OPEN.** This is a verified implementation checkpoint, not workshop acceptance. [Layout and annotated plan](../maps/Sorrel_Ridges.md).

The isolated scene now has a continuous basin, curved haul/seam/service paths, an arrival/receiving/stores sequence, usable repair pad, hab approaches and a cliff-enclosed drill recess. Scene GUID, named spawns, encounter IDs, mine IDs and completion flags remain stable. No unrelated zone or save was regenerated.

## Rejections and corrections

| Evidence under `Builds/quality/workshop/` | Actual finding |
|---|---|
| `sorrel-blockout-nav-red` | Initial check requested standing inside a solid crystal. Changed the fixture to a reachable approach within the real interaction range. |
| `sorrel-blockout-01` | Opened arrival, overhead, structure, hab, junction, seam and excavation views. High foreground hid arrival; terrain looked like a rectangular tray; matching flat materials hid prop silhouettes. Rejected. |
| `sorrel-blockout-02` | Opened arrival, hab, overhead, repair, excavation and structure. Lower foreground helped, but the outpost still had an artificial rectangular cut. Rejected. |
| `sorrel-blockout-03` | Opened overhead, receiving, hab, junction, seam, service branch, drill approach, north return and section. Rounded hollow, hab entrance markers and varied strata improve the functional plan. Closure regression still rejected this terrain. |
| `sorrel-seal-red` | First destination was inside the solid key prop. Preserve as a fixture failure; it does not establish the opening defect. |
| `sorrel-seal-opening-red` | Corrected destination reproduces the physical-open/navigation-closed bug: 0/1 PlayMode, 3.8087532 s. |
| `sorrel-seal-opening-green` | `Membrane.SetOpen` toggles the carved obstacle with collision: 1/1, zero skips, 4.0522956 s. |
| `sorrel-cliff-bypass-red`, `sorrel-cliff-bypass-trace` | New hill contours merge into the rim and permit a long route around the closed gate. Exact corner trace retained; no assertion weakened. |
| `sorrel-cliff-bypass-green` | Continuous steep rim is added above local strata. Closed/open/both-direction/reclosed checks pass: 1/1, zero skips, 4.0329668 s. |
| `sorrel-blockout-04` | Opened arrival, overhead and excavation again. Landing remains visible; the closed seal terminates in enclosing cliffs. Other local adjacencies are unchanged from 03. |
| `sorrel-dissolve-red` | Reclosing during the real opening animation leaves the surface one-third erased: 0/1 PlayMode, 3.1799791 s. |
| `sorrel-dissolve-green` | Cancel the old coroutine and reset its material property without discarding other overrides. Both closure/navigation and interrupted-animation checks pass: 2/2, zero skips, 7.6999114 s. |

## Ordinary outpost route

`sorrel-blockout-walk-01` runs [the committed route](../quality/routes/sorrel-outpost-map.json) through ordinary Input System virtual-gamepad controls, with companion/camera/HUD active and isolated saves. **36/36 checkpoints pass**, 153.6821851 seconds, 9,134/9,134 samples focused, no failures. Both heroes traverse hab approaches, stores and receiving. Taren mines `ridge_1_0` and `ridge_0_0`, uses repair/save, returns, swaps to Sela, and reaches the landing again. The saved fixture records `Outpost` and the mined flags. No enemies or damage settings were suppressed.

Codex opened player frames **000, 005, 009, 013, 019, 029, 035**: arriving beside receiving, repair operator clearance, both heroes at hab access, ore interaction/removal and the final launch approach. These are actual still-image observations. The captured moving video and mix exist but normal/slow viewing and sound audition remain **UNVERIFIED**. Physical Logitech feel is also **UNVERIFIED**.

`sorrel-west-blockout-walk-01` is **rejected**: 240.178249 seconds, 14,279 samples, only 11,369 focused. All four western encounter flags clear, but Taren goes down at 151.038510 s and the route leaves him outside revive range. Six return checkpoints and the living-party check fail; later focus loss interrupts input. Opened 003/012/023 show combat and cleared ground; 044 shows the downed Taren behind Sela. A revised ordinary route regroups at each cleared area, waits for nearby revival, and uses carried gel before the next clearing. No combat stat, count or damage has changed. Rerun pending.

The revised `sorrel-west-blockout-walk-02` clears all four packs without either hero going down: Taren finishes at 127.4/138, Sela at 138/138. It remains **rejected**, with four missed return checkpoints after focus is lost at 252.545082 s. Duration 281.0832736 s; 15,030/16,733 samples focused. Codex opened 062 and 073, showing both living heroes after the fourth clearing and stopped along the return. No completion is inferred from those stills. The outpost rerun `sorrel-blockout-walk-02` is also rejected: focus loss begins at 70.384593 s, interrupts western ore access and causes later missed navigation. It has 6,902/9,125 focused samples over 153.6217706 s. The earlier fully focused pass remains separately identified.

The outpost result does not establish remote service traversal, boss access, all boundaries or final art.

## Generated-prop audit and remaining work

`sorrel-prop-audit-01` measures actual existing mesh vertices and renders four angles. Codex opened all four hab angles, drill 0/180, anvil 180 and doorway 180. Hab is 5.6731 × 4 × 5.6731 m with its main door on +Z; drill is 6.9531 × 8 × 6.8281 m; anvil is 3.6968 × 2 × 2.0650 m. Final code prepares inward-facing hab doors aligned to their paths. Conversion is not yet run. A second audit measures the generated landing pad at 5 ? 0.213 ? 5 m: the inherited centre offset buries it under this terrain. Blockout 05 grounds a visible pad at each launch; its arrival and north-return views were opened. The northern pad still intersected the rising hollow at one corner; iteration 06 grades its entire apron. Opened north-return, arrival and excavation-section views show the whole pad clear of the hillside and the same enclosed drill recess. The two seal checks pass again on 06: 2/2, zero skips, 7.6696535 s. The Development player is rebuilt from 06; an ordinary route with this final blockout revision remains pending. Review the drill's feet/bit against its work footing and the final ore/bench collision approaches before accepting art.

Original save hashes remain `65b2ca12c171d0167a62b443988b7b66b0ac737a0ce11537a2197ab4278de47a` and backup `a9b131c2c9bd9ca03f1d69adfaf551a28f82be812f96f4146f26499d64e20745`. Development was rebuilt with this blockout; Release remains the preceding C3 integrated package. Full suites, final player pair and final in-player map acceptance are pending. C1 clean timing remains unavailable with foreign Unity/player activity recorded in `Builds/quality/C1/competitors-after-sorrel-blockout.json`.

The checkpoint manifest records exact source/evidence hashes. No independent viewer or Nathan approval is claimed.
