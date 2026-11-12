# Coronach - chapter guide

How a chapter is put together in this codebase, derived from how chapter 2 (The Lantern Road, D202: `WickReach`, `Lanternwick`, `LanternGorge`, `SunkenRelay`, boss `HessaKorl`) was built. Plan: [SHIPPING_PLAN.md](SHIPPING_PLAN.md). Each chapter's content brief: `docs/chapters/chNN-*.md`. Commands and session safety: [NEXT_SESSION_PROMPT.md](NEXT_SESSION_PROMPT.md). Checked against the scripts and tests committed at `ff99a7a` (November 11; unchanged at `70264d7`). Line numbers drift; names don't. What could not be confirmed is listed in [section 14](#14-unverified).

**In flight while this was written:** the systems streams (S1 impact, S2 loot, S3 skills) had uncommitted edits across `S/Combat`, `S/Rpg` and `S/Data`. Where this guide and the code disagree about shops, loot, items, consumables or hit weight, the code is newer; the places known to be moving are marked "S2".

Short paths: `S/` = `Lattice/Assets/_Project/Scripts/`, `A/` = `Lattice/Assets/_Project/`, `T/` = `Lattice/Assets/Tests/`. The worked example throughout is `S/Editor/World/LanternRoadBuilder.cs` (LRB).

## 0. Ground rules

- **Only the lead starts Unity** (one Unity may own `Lattice/`). Streams write files, run `python tools/typecheck.py` and their Python generators, and never run `scripts/headless.ps1`, `exec.ps1`, `full-suite.ps1`, `quality-*.ps1`, `look.ps1` or a player. Section 12 marks each command **[stream]** or **[lead]**.
- **Scenes, prefabs, meshes, navigation and most definitions are build output.** Nobody edits a `.unity`, `.prefab` or generated `.asset` by hand: change the builder and have it rebuilt. A rebuild starts each scene from an empty one, so anything not in code is lost.
- **Isolated entry points only.** Never run `WorldBuilder.BuildAll/BuildFinal`, `ArenaBuilder.Build`, `WorldBuilder.Prepare` or `GenIntake.Build` for chapter work: they regenerate chapter 1's definitions and scenes and strip their staging.
- **Evidence rules:** prove a new check fails on the broken state first, never loosen an assertion, never claim unobserved quality. Record consequential decisions in `docs/DECISIONS.md` (next free on November 11: D232; NEXT_SESSION_PROMPT keeps the number).
- **A screen never prints a raw id** (D200). Every id a player could read goes through `S/Core/DisplayNames.cs`.

## 1. What a chapter is made of

| Piece | Source you write | What Unity then produces |
|---|---|---|
| Zones | `S/Editor/World/<Region>Builder.cs` | `A/Scenes/<Zone>.unity`, `<Zone>-Terrain/-Ground/-Cliffs/-Navigation.asset`, `<Zone>-Curtain.mat`, `A/Settings/<Zone>Volume.asset` |
| Definitions (zone, enemy, loot, quest, character, shop) | `Define()` in that builder, through `ArenaBuilder.Asset<T>` | `A/Resources/Definitions/<Type>/<id>.asset` |
| Light and grade | one `Spec` in `S/Editor/World/ZoneLook.cs` | sun, ambient, fog and the volume profile, written into the scene |
| Kit art | `tools/meshy/<batch>.py`, `tools/<batch>_intake.py`, rows in `docs/art/intake.json` | `A/Art/Generated/Models/<id>/...`, `A/Prefabs/<folder>/<id>.prefab` |
| Portraits | a 4x4 sheet through `tools/portrait_pipeline.py` | `A/Art/Portraits/<Name>_Natural.png`, `A/Resources/Portraits/<Name>_Natural.asset` |
| Dialogue and barks | `docs/writing/<Region>.yarn`, `docs/writing/barks.json` | copies in `A/Resources/Dialogue/`, `A/Resources/Audio/barks.json`, the compiled `Lattice.yarnproject` |
| Who says what when | `S/World/SideStories.cs` (`Beats`), `S/UI/PeopleMarks.cs` (`Repeats`) | |
| Objective line | the `switch(state.zone)` in `S/UI/ObjectiveHud.cs`; `QuestText.Briefs` in `S/UI/QuestToast.cs`; `PauseMenu.MainQuests` | |
| Enemy behaviour | `S/Combat/EnemyBrain.cs`, `S/Combat/ActorFactory.cs` (only if the six archetypes don't cover it) | |
| Boss | `S/Combat/BossController.<Name>.cs` plus the switches in `S/Combat/BossController.cs` | |
| Names, music | `DisplayNames.zones/targets`, `MusicDirector.SetLocation`, `MusicIntake.SliceTracks`, `docs/music/tracks.json` | `A/Resources/Audio/Music/<Title>.ogg` |
| Build registration | `BatchTools.RegisterScenes()` in `S/Editor/BatchTools.cs` | `EditorBuildSettings.scenes` |
| Replay routes | `tools/<region>_routes.py` | `docs/quality/routes/chapter-NN-<name>.json` |
| Tests | `T/PlayMode/<Region>Tests.cs`, plus the hard-coded lists in section 11 | |
| Map brief | `docs/maps/<Zone>.md` before construction (MAP_EYE_TEST, `docs/PRODUCTION_PLAN.md`) | |

Chapter 2's ids, as a naming model: zones `LanternGorge`; enemies `MusterScav`; quests `PellsLamp`; speakers `Aumi`; encounters `Gorge_0`, `Relay_Hessa`; spawns `Arrival`, `Vault`; flags `gorge.relay1`, `relay.hessaConfronted`, `lanternwick.chartGiven`, `chapter2.complete`; yarn nodes `<Speaker><Purpose>` (`AumiFirst`, `PellLampOffer`).

Flags the game sets itself: `met.<Speaker>` (any ordinary talk), `bossdown.<EnemyId>`, `clear.<encounterId>`, `quest.<QuestId>.complete`.

## 2. House style

- **Assemblies** (one `.asmdef` per folder under `S/`): `Lattice.Data` <- `Core` <- `Rpg` <- `Combat` <- `World`; `Dialogue` sits beside Combat (Data, Core, Rpg); `UI` sees all of them and `Editor` sees everything. World sees Combat; Combat never sees World, Dialogue or UI, which is why `ObjectiveHud` and `PeopleMarks` live in UI. Namespaces match (`Lattice.World`), except editor code, which is `Lattice.EditorTools`. Tests are `Lattice.Tests.PlayMode` / `.EditMode`.
- **Dense code.** No spaces around operators or after commas, several statements on a line, expression-bodied members, `var`, target-typed `new()`, tuples and `switch` expressions, local functions for one zone's helpers (`void Fight(...)`). Four-space indent, Allman braces for multi-line blocks, one-line `{...}` bodies. Lines of 150 to 300 characters are normal. A few files are conventionally spaced instead (`MusicDirector.cs`, `ContinuousReview.cs`, parts of `StationRedesign.cs`): match the file you are in.
- **Comments say why, in what a player saw.** Most record the fault that led to the line ("a post stood across Corra's legs for her whole conversation"). `/// <summary>` on types and on anything non-obvious. No commented-out code.
- **Naming.** Public serialized fields are lower camel (`requiredFlag`); constants and `static readonly` are Pascal (`TallOverAFight`); private statics lower camel. Prompts read "Verb — Place" with an em dash (`"Launch — The Wick Reach"`). Floor signs are upper case (`"ARCH MARKET"`).
- **One scene MonoBehaviour per file**, the file named after the class. Unity resolves a serialized component by file name.
- **Serialized enums are append-only** (`SkillKind`, `PortraitEmotion`, and in practice `ObjectiveKind`, `EnemyArchetype`, `ZoneKind`: assets store the ordinal).
- **Editor entry points** are `public static void X()=>BatchTools.Run(()=>{...;Debug.Log("X_OK");});`. The marker is the method's last log line and `exec.ps1 -Marker` waits for it. Arguments come through `DevArgs.Value("-flag")` / `DevArgs.Has("-flag")`.
- **Gameplay logs** are upper-case markers tests listen for: `ZONE_ENTER`, `DISCOVERY`, `FLAG_ALL`, `RELAY_LIT`, `ENCOUNTER_CLEAR`, `BOSS_MOVE`, `BOSS_SHIFT`, `BOSS_DOWN`, `BOSS_DURATION`.
- **Randomness in builders is seeded** (`new System.Random(41)`), and a scatter *drops* what it must keep off rather than moving it, so later draws stay in step and every replay route still walks the same ground (D213). Replacing a kit key in a scatter list goes in the old key's slot for the same reason.
- **`.meta` files are committed**, one per file and folder. Unity writes them. A new `.cs` may be committed with a two-line meta (`fileFormatVersion: 2` / `guid: <32 hex, new>`) or left for the lead's next Unity run to create; never reuse or edit an existing GUID. See section 14.
- **Python tools:** stdlib plus Pillow, a usage docstring at the top, a final `*_OK` line.
- **Commits:** message from a file (`git commit -F`). Revert `Lattice/Packages/packages-lock.json` and `A/Resources/WorldMaterials/staged-light-line.mat` first. Never commit `Builds/`, `Library/`, `art-src/`, `tools/meshy/.env`.

## 3. Zones

### 3.1 The builder file

One static class per region in `S/Editor/World/`, with these entry points (LRB lines 33-72):

```csharp
public static class XylosBuilder
{
    public static readonly string[] Zones={"XylosNearspace","Rootmarket","BraidedRiver","WalkingCanopy"};
    public static void Definitions()=>BatchTools.Run(()=>{Define();AssetDatabase.SaveAssets();Debug.Log("XYLOS_DEFS_OK");});
    public static void BuildZones()=>BatchTools.Run(()=>
    {
        string arg=DevArgs.Value("-xylos-zones");var zones=string.IsNullOrEmpty(arg)?Zones:arg.Split(',',StringSplitOptions.RemoveEmptyEntries);
        Define();foreach(var zone in zones)BuildZone(zone.Trim());
        BatchTools.RegisterScenes();AssetDatabase.SaveAssets();Debug.Log("XYLOS_ZONES_OK "+string.Join(",",zones));
    });
    public static void Dialogue()=>BatchTools.Run(()=>{RefreshDialogue();Debug.Log("XYLOS_DIALOGUE_OK");});
}
```

`Define()` always runs before a zone is built, because `WorldBuilder.Begin` reads the `ZoneDef`. A way into the region from an existing scene is staged by opening that scene and replacing only its own objects (LRB `StageTallowApproach`, entry `StageApproach`): destroy the old warp and spawn by query, add the new ones, save.

**LRB's helpers are private.** `Terrain`, `Prop`, `Kit`, `Scatter`, `CliffFoot`, `SeatInWall`, `HidesFloor`, `Gate`, `Person`, `Discovery`, `Relay`, `Lantern`, `Lamp`, `Arch`, `CurtainMaterial`, `Strand`, `BakeNavigation`, `OnASign`, `InSightOf` and the state they share (`height`, `CliffTop`, `fadeTall`) cannot be called from another class today, and `Terrain` hard-wires the canyon cliff material. Until the lead lifts them into a shared class, a region builder carries its own copies. Don't fork them four ways without saying so.

### 3.2 The ZoneDef

`S/Data/ZoneDef.cs`. The asset's file name, `id` and `scene` are the same string (`GameCatalog.Find<T>(id)` loads `Resources/Definitions/<Type>/<id>`).

```csharp
var ground=Find<CameraProfile>("Ground");var flight=Find<CameraProfile>("Flight");
var dusk=ArenaBuilder.Asset<Hd2dProfile>("Dusk",p=>{p.tint=new Color(1,.9f,.82f);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.5f;});
ArenaBuilder.Asset<ZoneDef>("LanternGorge",z=>{z.id=z.scene="LanternGorge";z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=dusk;z.skybox=sky;z.enemyThreat=1.6f;z.cameraClearsScenery=true;});
```

`ArenaBuilder.Asset<T>(string id,Action<T> configure)` loads or creates the asset and runs the lambda. Fields the lambda doesn't name survive a rebuild (which is how `ambientAudio` persists); fields it names are overwritten every run, so **tune in the builder, never in the asset**.

| Field | Use |
|---|---|
| `kind` | `GroundSafe` (Natural bodies, no combat), `GroundCombat` (Shaped), `SpaceSafe` (CivilFlight), `SpaceCombat` (Flight). `ZoneController` derives form, input mode and combat from it |
| `cameraProfile` | `Ground` (pitch 40, yaw 20, distance 19.5) or `Flight` (pitch 48, yaw 0, distance 26). Both exist; don't make more |
| `hd2dProfile` | must not be null (`WorldBuilder.Begin` reads `.bloom`). Existing: `Warm`, `Cold`, `DeckSoft`, `Halo`, `Tallow`, `Cave`, `Dusk` |
| `skybox` | a material, or null for underground (camera clears to solid colour) |
| `enemyThreat` | multiplies ordinary enemies' damage here; bosses stay at 1 (D194). Chapter 2: 1.5, 1.6, 1.7 |
| `cameraClearsScenery` | true on open landforms: the camera slides in rather than hide the party. Stations and flight zones leave it off |
| `flightPlane` | 1 in every flight zone |
| `ambientAudio`, `ambientVolume` | the zone's bed; no builder sets it (section 14) |
| `musicId` | **not read by anything.** Music is section 3.8 |

### 3.3 The scene, by kind

Every zone starts `var scene=WorldBuilder.Begin("<Zone>");` (an empty scene with `ZoneRoot` carrying `ZoneController` and `ArenaRuntime`, one directional `KeyLight`, one global `ZoneLook` volume) and ends `WorldBuilder.Save(scene,"<Zone>");Debug.Log("<ZONE>_OK");`. `ArenaRuntime` spawns the party at the `SpawnPoint` named in the save, adds the HUD, dialogue and shop, and reports `Reach <scene name>` to the quests.

| | Town (GroundSafe) | Wild (GroundCombat) | Dungeon (GroundCombat) | SpaceCombat | SpaceSafe |
|---|---|---|---|---|---|
| Model | LRB `Lanternwick()` | LRB `Gorge()` | LRB `Relay()`; `HushwellBuilder` | LRB `WickReach()`; `GulletBuilder` (tunnel) | `StationRedesign.BuildExterior`, `TallowRedesign` |
| Before `Begin` | `height=TownHeight;CliffTop=3.6f;fadeTall=true;` | `height=GorgeHeight;CliffTop=2.5f;fadeTall=true;` | `height=RelayFloor;CliffTop=1.2f;fadeTall=true;` | `height=_=>0;fadeTall=false;` | nothing (those builders call `WorldBuilder` directly) |
| Floor | `Terrain(...)` with a tone function | same | `Terrain(...,wallTop,wallFoot)` to keep buried wall tops dark | none: everything at y 1 | none |
| Profile, sky | `Dusk`, sky material | `Dusk`, sky | `Cave`, `skybox=null` | `Cold`, a `Lattice/DriftingSky` material plus a `SkyDrift` object | `Halo` / `Tallow` |
| Ways out | `DockingPad` on a `LandingPad` prop; `Gate(...)` | `Gate(...)` | `Gate(...)` | `WorldBuilder.Dock(name,pos,scene,spawn,WorldBuilder.FlightDockRange)`, `WorldBuilder.Warp(...)` | same |
| Limits | four `WorldBuilder.Boundary` boxes, with terrain rising at each so the stop is visible | same | the halls' own rock | `Boundary` boxes at the flight plane | same |
| Services | paid `RepairBay` with `saveSpawn`, `Bench`, a stall (section 9) | free `RepairBay` at the arrival, a `Bench` before the dungeon door | free cradle plus its own `Spawn` under a minute from the boss | none | docks, `HailPoint` on ship NPCs |
| Fights | none | `WorldBuilder.Encounter` per clearing | encounters, then the boss door | encounters along the lane | none |
| Navigation | `BakeNavigation` | `BakeNavigation` | `BakeNavigation` | none | none |
| Backdrop | homes, cliff feet, scatter | fallen towers seated in walls, scatter | wall-side "trade" props, drawn only | everything below the flight plane, `solid:false,grounded:false` | hulls, planets (`WorldArt.Planet`) |

A wild zone in outline (LRB `Gorge()`):

```csharp
static void River()
{
    height=RiverHeight;CliffTop=2.5f;fadeTall=true;
    var scene=WorldBuilder.Begin("BraidedRiver");
    Terrain("BraidedRiver","Braided river",-60,60,-24,250,.5f,Ground(false),RiverTone);
    WorldBuilder.Spawn("Arrival",new Vector3(0,0,0));WorldBuilder.Spawn("Canopy",new Vector3(0,0,220));
    Gate("Climb to Rootmarket",new Vector3(-4,0,-8),"Rootmarket","River",null,null,"HushwellScaffold",3,3.6f,new Vector2(.4f,-9));
    var rest=Prop("DeckConsole",new Vector3(6,0,8),1.4f,-60).AddComponent<RepairBay>();rest.free=true;rest.saveSpawn="Arrival";rest.prompt="Waystone — repair and save";
    void Fight(string id,Vector2 at,string enemy,int count){var e=WorldBuilder.Encounter(id,new Vector3(at.x,1,at.y),new Vector3(22,5,16),enemy,count);e.GetComponentInChildren<Spawner>().transform.position=new Vector3(at.x,height(at),at.y+3);}
    Fight("River_0",new Vector2(2,26),"Gloomgrazer",3);
    WorldBuilder.Boundary("South river limit",new Vector3(0,8,-16),new Vector3(120,20,2));
    BakeNavigation("BraidedRiver",new Bounds(new Vector3(0,8,110),new Vector3(110,40,260)),new Vector3(0,0,0),RiverPath.Select(p=>new Vector3(p.x,0,p.y)));
    WorldBuilder.Save(scene,"BraidedRiver");Debug.Log("BRAIDED_RIVER_OK");
}
```

### 3.4 Helper signatures

`WorldBuilder` (public, `S/Editor/World/WorldBuilder.cs`):

```csharp
Scene Begin(string name);  void Save(Scene scene,string name);
GameObject Piece(string key,Vector3 pos,Vector3 size,string material="Rock",bool solid=true);   // centre-positioned, fitted to size
SpawnPoint Spawn(string id,Vector3 position);
void Boundary(string name,Vector3 position,Vector3 size);                                       // invisible static box
GameObject Label(string text,Vector3 position,float size=2);                                    // object "Sign_<text>"
Npc Npc(string id,Vector3 position,string first=null);
DockingPad Dock(string name,Vector3 position,string scene,string spawn,float range=4);          // FlightDockRange=6.5f in flight
WarpBeacon Warp(string name,Vector3 position,string scene,string flag="warpkey",string spawn="Arrival");
EncounterVolume Encounter(string id,Vector3 center,Vector3 triggerSize,string enemy,int count,Membrane[] membranes=null);
Membrane Membrane(string id,Vector3 position,float width,bool open,float height=6);
```

LRB (private today; positions are x,z on the plan with y as an offset above `height`):

```csharp
GameObject Terrain(string zone,string label,float xMin,float xMax,float zMin,float zMax,float cell,Material material,Func<Vector2,Color> floorTone=null,Color? wallTop=null,Color? wallFoot=null);
string Kit(string key,string fallback);                                                         // the key if its prefab exists, else the fallback
GameObject Prop(string key,Vector3 at,float size,float yaw=0,bool solid=true,bool grounded=true);// size is height; stands on the ground
void Scatter(System.Random rng,int count,Func<Vector2> pick,Func<Vector2,bool> allowed,Func<Vector2,bool> keptOff,params (string key,float size,bool solid)[] kinds);
void CliffFoot(IEnumerable<Vector3> points,int seed,Func<GameObject,bool> stays=null,Func<Vector2,bool> leftOut=null);
bool SeatInWall(GameObject rock,Func<Vector2,float> fromFloor,float clear,bool capped=true);
bool HidesFloor(Bounds box,List<Vector3> floor);
WarpBeacon Gate(string label,Vector3 at,string scene,string spawn,string flag,string locked,string key="WarpBeacon",float size=3,float range=3.6f,Vector2? signAt=null);
Npc Person(string id,Vector3 at,float yaw,string postFlag,bool post=true,string finalFlag=null,string finalNode=null);
DiscoveryPoint Discovery(string name,Vector3 at,string prompt,string flag,string report,string node=null,string speaker="Sela",string quest=null,int step=0,float range=3.2f);
RelayLantern Relay(string name,Vector3 at,string flag,float size,float yaw=0,bool grounded=true,bool solid=true);
void Lamp(Vector3 at,float range=9,float intensity=1.6f);                                        // a point light, no shadows
Material CurtainMaterial(string zone,bool fromFloor=true,string asset="Curtain");  void Strand(Transform parent,Vector3 from,Vector3 to,Material beam,float width,Color bottom,Color top);
void BakeNavigation(string zone,Bounds bounds,Vector3 origin,IEnumerable<Vector3> mustReach);
bool OnASign(Vector2 p,(Vector2 at,float half)[] signs);  bool InSightOf(Vector2 p,Vector2 person,float reach,float length,float half);
```

World components a builder wires by hand (`S/World/`): `FlagGate{flag,showWhenSet,targets}` (shows or hides objects by a flag; lives on an always-active object), `FlagAll{flags,result}` (sets `result` once all are set), `RelayLantern`, `StaysDark{glass}`, `PropTint{tint}`, `SceneryOcclusion{opening}`, `LightCurtain{flag,sweep}`, `SafePocket` (a trigger box that makes a combat zone safe inside it), `QuestStarter{questId}`, `ScriptedWalk` (an authored walk for a civilian; D195), `YieldedCaptain`.

### 3.5 Terrain and the fixed camera

`Terrain` turns one height function into three saved meshes: an invisible full-resolution collider (`<Zone>-Terrain.asset`, also the single welded shadow caster), the walkable floor (`-Ground`, flatter than normal.y .78 and below `CliffTop`) and the walls (`-Cliffs`, strata mapped along each wall). The floor's vertex colour is the tone function (a paler worn path, a market square), darkened toward wall feet. Use .5 m cells: at 1 m every wall foot is a row of teeth. Rules the chapter 2 comments paid for:

- The ground camera sits south-west of the hero and never turns (D119, D212). **West and south walls are its foreground: keep them low** (`walls*=.42f` for x<0 in town; `cameraSide?.36f:1` in the relay) or the camera closes in on the hero's knees.
- It sees about 7 to 9 m north of the hero. A wall meant to stand behind a speaker has to come that close; a talk trigger goes just inside the door.
- An arrival opens on the room, not on a wall behind the party (camera distance over 14 at every spawn).
- Nothing 7 m or more across and more than 3 m over the floor stands on the camera's line to a fight or the walked path (`HidesFloor`, `TallOverAFight=3`). Kit rocks are capped at 8 m wide and seated into the wall (`SeatInWall`).
- In a ground zone any prop 2.5 m or taller gets `SceneryOcclusion` and its material's `_OCCFADE_ON` (`fadeTall`). An arch you walk through declares its `opening`.
- Nothing stands on a floor sign's ground or between the camera and a named person (`OnASign`, `InSightOf`). Each zone lists its own signs and people for its scatter.
- In flight the camera looks down at 48 degrees, so a thing below the lane shows south of where it is: clear backdrop by where it shows (LRB `ClearOnScreen`).
- `WorldBuilder.Label` signs are restyled at load by `S/UI/SignStyle.cs`. Put a floor sign at y about .1 beside what it names. Sunlit pale floors go in `SignStyle.BrightFloors`.
- Underground, light comes from fixtures you can see: every `Lamp` has a prop under it, hung out over the room rather than against a wall (`OffTheWall`). One floor renderer takes about six lights; past that, paint a pool (`ChapterOneStaging.Soft`, `SoftPatch`) instead of adding a light.

### 3.6 Spawns, exits and locks

- Every zone needs `WorldBuilder.Spawn("Arrival",...)`. Each way in names a spawn id that exists in the target scene. A spawn needs floor, room for two bodies (the companion stands 2 m to +X) and, on foot, navigation within 1.5 m.
- `Gate(label,at,scene,spawn,flag,locked,key,size,range,signAt)`: `flag` null means open; `locked` is what the prompt says when refused and must name what opens it. A tall gate's prompt measures to the middle of its body, so its `range` grows with its size.
- `WorldBuilder.Warp`'s default `flag` is `"warpkey"`: pass `""` and clear `lockedPrompt` for an open warp. `WarpBeacon`, `DockingPad`, `RepairBay` and `Bench` refuse while a boss fight is under way or an enemy is awake nearby.
- Entering a zone autosaves (`SceneFlow`), except `Title` and `Arena_*`.

### 3.7 Navigation

`BakeNavigation` reads only **static, non-trigger `BoxCollider` and `MeshCollider`** (not children of a `Membrane`), marks each `Npc`'s collider not walkable, bakes with radius .55, height 2, climb .3, slope 35, voxel .15, and **throws** if the arrival has no floor or any `mustReach` point has no complete path (log `LANTERN_ROUTE_BAKE_OK <zone> points=N` on success). It saves `<Zone>-Navigation.asset` and adds a `GroundNavigation` object. List every place a player must stand: each named person, each pick-up, each door, the boss floor. A capsule or sphere collider is invisible to the bake, which is why a townsperson stands in a static box. A seal that closes mid-zone carries a carving `NavMeshObstacle`, disabled while open (`Membrane.SetOpen` toggles it).

### 3.8 Light, music, ambience

- **Light.** Add `["<Zone>"]=new Spec{...}` to `ZoneLook.Specs` (sun colour, intensity and pitch, trilight ambient, fog, exposure, contrast, saturation, split toning, bloom, vignette). `ZoneLook.Apply` only visits zones in `Specs`, needs exactly one directional light and one global volume in the scene, and must be rerun after every rebuild because `Begin` resets the light to flat. Lessons: a warm sun wants a cooler fill and grade or stone goes one flat orange (Lanternwick); a cave is a faint cold fill too weak to cast a shadow, lit by its lamps (SunkenRelay, Hushwell). No blur, ever (D060).
- **Music.** Three edits: a branch in `MusicDirector.SetLocation` (`S/Core/MusicDirector.cs`; a zone with none plays silence, without error), the title in `MusicIntake.SliceTracks` (`S/Editor/MusicIntake.cs`), and `"status":"slice"` on its row in `docs/music/tracks.json` (sources are `Music/<Title>.ogg`; `MusicIntake.Stage` checks the sha256). A shop overrides with "Moonbase Market"; every boss plays "Alien Boss Battle", hard-coded in `BossController.Start`.
- **Ambience.** `ZoneDef.ambientAudio` plays as the zone's bed (clips in `A/Audio/Ambience/`); with none, a ground zone hums `computerNoise_000`. Natural places need a bed of their own.

### 3.9 Registration checklist for a new zone

1. `ArenaBuilder.Asset<ZoneDef>("<Zone>",...)` with `id`, `scene` and the file name identical.
2. `BatchTools.RegisterScenes()` names array. Without it the scene isn't in the player and `LoadZone` fails.
3. `ZoneLook.Specs`.
4. `MusicDirector.SetLocation` (and the track staged).
5. `DisplayNames.zones`, when `Readable(id)` isn't the name ("The Wick Reach").
6. A `case "<Zone>":` in `ObjectiveHud.LateUpdate`. Without one the panel reads "Practice attacks, dodges and swaps". The chapter's closing line also lives there (`chapter2.homecoming` currently prints "Chapter 2 complete...").
7. `SignStyle.BrightFloors` for sunlit ground.
8. An ambience bed on the `ZoneDef`.
9. The test lists in section 11, and `docs/maps/<Zone>.md`.
10. For in-player stills, `scripts/look.ps1` and `S/UI/ZoneLookProbe.cs` (both hard-code chapter 1's six zones).

## 4. Kit art

### 4.1 What exists

A prop key is a prefab name in `A/Prefabs/Environment/` (or `Ships/`). `Prop`/`Piece` fit it by height (a few keys by footprint or length) and add a box collider when solid.

- **Chapter 2 kit:** `AncelaneArch`, `AncelaneColumn`, `CanyonHouse`, `GreatLantern`, `LampPost`, `LanternShard`, `LanternTower`, `MusterCrate`, `MusterCutterRig`, `MusterLeanTo`, `OrchardTree`, `RelayMirror`.
- **Dressing (`Dress*`):** `Boulders`, `CabinBunk`, `CableDrum`, `CargoPallet`, `CommsMast`, `CoreRack`, `DeckLamp`, `FanShrub`, `FuelTank`, `GalleyTable`, `Kiosk`, `LightBollard`, `MarketStall`, `OfficeDesk`, `OreHopper`, `PipeRun`, `Planter`, `PowerCell`, `RailBuffer`, `Rover`, `ShelfRack`, `SolarArray`, `SpoilHeap`, `StrataLedge`, `StrataOutcrop`, `Succulent`, `SurveyBeacon`, `WaterBarrels`, `WorkLamp`, `Workbench`.
- **Station:** `DeckBench`, `DeckConsole`, `DeckCrate`, `DeckDoorway`, `DeckFloor`, `DeckWall`, `DockingPodOffice/Outfitter/Repair`, `LandingPad`, `LatticeAnvil`, `WarpBeacon`, `RingSegmentA/B`, `TallowStationHull`, `TallowMooringCoupler`.
- **Cave (`Hushwell*`):** `ChoirGrowth`, `Column`, `CradleHatched`, `CradleSingle`, `Crates`, `CrystalBed`, `DrillBit`, `EggCradle`, `FallenLamp`, `Lift`, `LowRidge`, `Marker`, `OreCart`, `Rubble`, `Scaffold`, `Shells`, `Slope`, `Stalagmites`, `Vent`, `Wall`, `WallCrystal`.
- **Rock and other:** `RidgeRock1-3`, `CrystalClusterA/B`, `MoonGroundA/B`, `OutpostDrill`, `OutpostHab`, `GulletWallA/B`, `ChoirPod`, `CoilMooring`, `NurseryGroove`, `SurveyDrone`. Ships: `Hauler`, `PatrolCutter`, `Skiff`.
- **Bodies:** `A/Prefabs/Characters/` (`AumiBody`, `BrekkBody`, `CorraBody`, `IshenBody`, `PellBody`, `Hal`, `KeeperNatural`, `Mira`, `Neve`, `Orrin`, `SurvivorNatural`, the heroes' three forms) and `A/Prefabs/Enemies/` (one per enemy id, plus `CantorBody/Head/Tail`).

Write `Prop(Kit("NewPiece","ExistingStandIn"),...)` so the zone is walkable before its art lands; add a `StandIn` scale when proportions differ. A key with no prefab and no fallback becomes an `ART_PENDING_<key>` cube, which a test rejects.

### 4.2 What code can make without art

The heightfield and its vertex-colour paths; a mesh built in the builder (LRB `LanternStair`: treads, risers, a box collider); strands and curtains of light on `LineRenderer` (`CurtainMaterial`, `Strand`, `ArchiveWall`); a particle layer (`DriftingDust`); point lights; soft painted floor patches (`ChapterOneStaging.Soft(name,paint)`, `SoftPatch(...)`); a cradle bay (`ChapterOneStaging.CradleBay`); planets (`WorldArt.Planet(name,position,diameter)`); a tint or an unlit variant of an existing piece (`PropTint`, `StaysDark`, `DeadGlass(kit)`); a world material cloned from a template with a generated texture (LRB `CanyonMaterials`, into `A/Resources/WorldMaterials/`).

### 4.3 Shader

Everything in the world draws with `Lattice/Toon` (`A/Art/Shaders/LatticeToon.shader`). Always on: `_BaseMap`, `_BaseColor`, `_RampSteps` (2-6), `_ShadowTint`, rim, `_LightFloor`, `_EmissionMap` x `_EmissionColor`. Toggles, each a `shader_feature_local` keyword:

| Keyword | Does |
|---|---|
| `_VCOLOR_ON` | multiplies vertex colour into albedo (floor tones, dark wall tops) |
| `_TRIPLANAR_ON` | steep faces sample the base map from the side |
| `_STRATA_ON` | horizontal rock bands by world height, with a slope mask |
| `_DETAIL_ON` | world-space macro and micro noise on ground |
| `_OCCFADE_ON` | screen-door fade of whatever hides the hero |
| `_EDGEFADE_ON` | soft alpha edges on patches and paths |
| `_PANELS_ON` | world-projected hull seams |
| `_WATER_ON`, `_ICE_ON`, `_GEL_ON` | depth-tinted water with foam; ice; jelly |
| `_WIND_ON` | vertex sway |
| `_GLOW_ON`, `_SURF_ON`, `_DISSOLVE_ON`, `_ALPHATEST_ON` | unlit glow; UV-free fur/chitin/stone/mech detail; KO dissolve; clip |

**A variant no material in the build enables is stripped from the player.** A keyword first switched on at runtime works in the editor and does nothing in the build. Other shaders: `Lattice/DriftingSky`, `Lattice/Telegraph`, `Lattice/TelegraphFill`, `Lattice/SignPlate`, `Lattice/EnergyBubble`, `Lattice/FlightEnergy` (`A/Resources/Effects/`). A property block isn't saved with a scene: anything set that way is applied by a component at load (`PropTint`, `StaysDark`).

### 4.4 Ordering and intake of generated art

Visible art is generated, not sourced (`docs/ASSET_SOURCES.md`); packs are only stand-ins, rig donors, particles, fonts and sound. Procedure (`docs/art/PIPELINE.md`; chapter 2's scripts are the model):

1. **Order** [art runner]. Copy `tools/meshy/c9_lantern_batch.py`: stages `refs` (design boards, portrait atlases, tiling textures), `isolate` (cut boards into single objects with `tools/isolate_board.py`), `models` (`meshy.py img3d --band prop --target-polycount 6000`), `reskin` (`meshy.py retexture` of an existing rigged body, original UVs). Chapter 3's `tools/meshy/c10_xylos_batch.py` already follows it. Parallel tasks used to race on the ledger `docs/art/gen-manifest.json` and drop rows, hence NEXT_SESSION_PROMPT's "one at a time"; since `19638a0` `meshy.py` takes a `manifest_lock` around every write and says tasks may run side by side. `meshy.py` refuses unless `docs/art/species-approval.json` is approved and stops at a credit cap. Review every isolated reference before spending on 3D. Output lands in git-ignored `art-src/Generated/<batch>/`.
2. **Prepare rows** [art runner]. Copy `tools/c9_intake.py`: for a static prop it strips embedded textures in Blender (`_clean.fbx`), keeps an albedo of at most 1024 px, derives an emission mask (`tools/emission_mask.py`) and upserts a row in **`docs/art/intake.json`** (the file `GenIntake` reads; `gen-manifest.json` is only the ledger). For a reskin it copies the source id's row (model, clips) and swaps the textures.

```json
{"id":"LanternTower","model":"art-src/Generated/C9-Lantern/LanternTower_clean.fbx","albedo":"art-src/Generated/C9-Lantern/LanternTower_albedo.png","emission":"art-src/Generated/C9-Lantern/LanternTower_emission.png","emissionColor":"#FFB62E","kind":"static","folder":"Environment","size":12,"fit":"height","yaw":0,"clips":[]}
```

   Row fields: `kind` `static|biped|generic`; `folder` `Environment|Enemies|Characters|Ships`; `fit` `height|length|depth` with `size` in metres; `enemy` (binds `EnemyDef.prefab`, so **the EnemyDef must exist first**) or `character`+`form` (binds a `CharacterDef` body); `clips` (`state`, `path`, `name`; a rigged row needs `Idle` and `Walk`; humanoid donors are in `A/Art/Animation/UAL-Skeleton.fbx`, quadruped in `Fox-*.anim`).
3. **Intake** [lead]. `GenIntake.BuildPrefix` with `-intake-prefix <id prefixes>`, marker `GEN_INTAKE_OK`. It stages to `A/Art/Generated/Models/<id>/`, makes `<id>_Toon.mat`, normalises scale and pivot (feet at the origin), and writes `A/Prefabs/<folder>/<id>.prefab`. It adds no colliders. The prefix is `StartsWith`: `Muster` matches every `Muster*` row.
4. **Use.** The key is now live; rerun the zone build.

Traps: a bad `emissionColor` gives black emission silently; an unknown `kind` or `fit` silently means static or height; a pitch on an already-upright model lays it flat and scales it by the wrong axis (D213); a retexture cannot fix a shape (D214: a new body for a named person, not a new coat); a flat-colour texture reads as an untextured slab (`CrystalClusterA`'s glass).

## 5. A new enemy

1. **Define it** in the region's `Define()` (LRB 130-141). The asset name is the id. The body falls back to an existing prefab until its own is taken in:

```csharp
ArenaBuilder.Asset<EnemyDef>("MusterScav",e=>{e.id="MusterScav";e.prefab=Body("Enemies","MusterScav","Enemies","SentinelHusk");e.integrity=210;e.breakThreshold=90;e.speed=2.7f;e.attacks=new[]{volley};
    e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Spitter;e.xp=45;e.boss=false;e.lootTable=Loot("MusterScav",new LootEntry{id="ScrapAlloy",count=2,chance=.5f});});
```

2. **Pick an archetype.** `EnemyBrain` uses only `attacks[0]` (`AttackDef`: `telegraph`, `range`, `damage`, `cooldown`, `type`, `projectile`) and behaves by archetype:

| Archetype | Behaviour | Marking |
|---|---|---|
| `PackHunter` | weaves in and lunges along a lane (reach 6) | a lane |
| `Swarm` | closes and bites | a disc ahead of it |
| `Sentinel` | slow and heavy; a hook pulls the hero to it; gets `EnemyArmorPresentation` | a disc ahead of it |
| `Spitter` | keeps range and fires `attacks[0]` as a shot | a line to the end of the shot |
| `Mine` | doesn't move; blasts and dies | a circle |
| `Serpent` | the Cantor's segmented body | (boss) |

   Existing attacks: `Bite`, `Nip`, `Volley`, `BellowsSpit`. Weakness multiplies damage by 1.5, resistance by .5. Damage is `attack.damage` x the zone's `enemyThreat`; chapter 2 sits near 10% of a hero's integrity a hit (D206).
3. **Anything beyond that is code keyed on the id**, and there is a lot of it to know about: `ActorFactory.Enemy` (capsule size; `ProceduralMotion` hover for `Chorister*` and `MusterSkiff`, skitter for `Scrapmite`; drawn scale), `EnemyBrain` (the Husk's resistance shield, the Drifter's three-shot cone, the Dart's dive, the Scrapmite's death blast, death sounds), `RelayLantern` (the ward acts on `Gloomgrazer` by id). A new behaviour means a new branch there, and a test that fails without it.
4. **Body.** An intake row with `"enemy":"<id>"` (section 4.4). A biped reskin of the Husk rig reuses its five clips.
5. **Name.** `DisplayNames.Target(id)` splits camel case; add to `DisplayNames.targets` when that isn't what anyone would say (`MusterScav` -> "Muster Scavenger").
6. **Place it.** `WorldBuilder.Encounter(id,center,triggerSize,enemy,count)` makes a trigger volume and one `Spawner` (ring of `count` at radius 3). Add a second kind with another `Spawner` child and reassign `encounter.spawners` (LRB `Fight`). Put spawners on the ground (`height(at)`) or at y 1 in flight. Clearing sets `clear.<encounterId>` and autosaves. A `Kill` quest step's target is the enemy id.
7. **Tests** (section 11): the automatic one spawns every non-boss `EnemyDef` 2 m from a hero and requires a tell, a marking and a sound within 4 s. Then add the id to `AllFourteen` and to the ground or flying tell table in `T/PlayMode/QaPlanCombatTests.cs`.

## 6. A new boss

A boss is an `EnemyDef` with `boss=true`: `ActorFactory` adds `BossController`, which runs marked specials between the brain's ordinary attack.

1. **Definition.** Integrity is sized so the replay party takes 120 to 300 s (D206; chapter values 35000 to 55000), `breakThreshold` 600 to 900, `yieldsOnDefeat=true` for a person who surrenders. Retune in the builder: chapter 2's tests read the builder's source and require the literal `e.integrity=<n>;` to equal the asset (`QaFairTests.cs`, `QaFightLookTests.cs`), and a new boss should get the same pin.
2. **Moves**: `S/Combat/BossController.<Name>.cs`, a `partial class BossController` (model: `BossController.Hessa.cs`). A chooser and one coroutine per move:

```csharp
float XRest=>Phase>=3?1.9f:Phase>=2?2.3f:2.8f;
(string,IEnumerator) ChooseX(CombatActor hero)
{
    float distance=Flat(hero.transform.position-transform.position).magnitude;
    string move=Pick(("Thrust",distance<8?2.2f:0),("Chain",distance>5?1.8f:FullOffer),("Flash",Phase>=3?1.4f:0));
    return move switch{"Chain"=>(move,Chain()),"Flash"=>(move,Flash()),_=>(move,Thrust())};
}
IEnumerator Thrust()
{
    var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);float tell=.6f;
    var cone=Mark(Telegraph.Cone(transform.position+direction*1.6f,direction,4.4f,80,tell,Telegraph.Threat));
    strikeAt=Time.time+tell;
    yield return Windup(tell,-7);
    if(Interrupted)yield break;
    Strike(cone,22,DamageType.Kinetic);
    AudioManager.PlayAt("laserSmall_000",transform,transform.position+Vector3.up,.2f,AudioBus.SFX,50);
    Pose(0);Finish(XRest);
}
```

   Shared pieces (`BossController.cs`): `Mark(telegraph)`, `Strike(marking,amount,type,stagger=0,shove=0,once=null,landing=false)`, `Contact(hit,centre,radius,amount,stagger,lane)` for a moving body, `Windup(seconds,pitch,lift=0,tremble=0)`, `Pose(pitch,lift,tremble)`, `Finish(cooldown)`, `Opening(seconds,rest)` / `health.Expose(seconds)` for a punish window, `Pick((move,weight)...)` (nothing at weight `FullOffer`=.5 or more waits beyond three picks), `Packet(amount,type)`, `Toward(point)`, `Flat(v)`, `Interrupted`, `Collide(false/true)` for a charge through the party, `Tremor(seconds,amplitude)`, `SummonPoint(offset)`, the `temporary` and `summoned` lists (cleaned up for you), `Situation("bossopening")` for a bark. Set `strikeAt` to when the next hit threatens the active hero: the companion and the replay driver dodge by `TelegraphRemaining`.
3. **Markings** (`S/Combat/Telegraph.cs`): `Telegraph.Circle(center,radius,duration,color,light=false)`, `Ring(center,inner,outer,duration,color)`, `Cone(origin,forward,radius,angle,duration,color,light=false)`, `Line(origin,direction,length,width,duration,color,light=false)`; colours `Telegraph.Threat`, `Heavy`, `Stagger`. The shape drawn is the shape tested (`Contains`): a hit lands only inside its marking, and a shot's line travels with it (`line.Carry(shots)`). `Release()` when it lands, `Cancel()` when interrupted. A marking under alpha .99 is a boundary, not a danger (D222). Every hit is marked (D207); orange, red and purple on the floor mean danger and nothing else uses them (D217).
4. **Phases.** `Thresholds` gives the integrity fractions. Crossing one runs the shared `PhaseShift()`: untouchable 1.4 s, a shove ring, the boss's summons, log `BOSS_SHIFT <id> <phase>`, then `SignatureMove(phase)` next. Each phase needs a deliberate opening.
5. **Register the boss in `BossController.cs`**: `DisplayName`, `Epithet`, `Thresholds`, the chooser `switch` in `Update` (its default is the Burrower's `DrillCharge`), `Perform` (every move by name; tests drive moves through it), `SignatureMove`, summons in `PhaseShift`, and `ShiftText`/`StatusText`/`GuardedText` if it has states. Elsewhere: `ActorFactory.Enemy` (capsule radius is .8 only for `HessaKorl`, else 1.4; `BossOcclusion` is added by id), `EnemyBrain.OnDeath` (a yield or a release instead of the boss explosion), `ObjectiveHud.FightGoal`, `GenIntake` (keeps the `_OCCFADE_ON` variant for three boss ids), `tools/boss_durations.py` `BOSSES`.
6. **Barks.** `bossphase.<Id>` and `bossopening.<Id>` for both `Taren` and `Sela` in `docs/writing/barks.json` (a pool is `"line|line|line"`); without them the generic lines play.
7. **The arena and its door** (LRB `Relay()` 1247-1293). Columns that survive every phase as cover; a free cradle and a spawn under a minute away; the talk as a walk-through trigger; a seal that shuts behind the party; the boss standing in the room before the fight:

```csharp
var cradle=Prop("DeckConsole",new Vector3(8,0,110),1.4f,-90).AddComponent<RepairBay>();cradle.free=true;cradle.saveSpawn="Vault";cradle.prompt="Relay cradle — repair and save";
WorldBuilder.Spawn("Vault",new Vector3(4,0,108));
var confront=Discovery("Confront the scavenger captain",new Vector3(0,0,124),"Confront the scavenger captain","relay.hessaConfronted","","HessaConfront","Hessa",null,0,2);
confront.saveSpawn="Vault";confront.startsOnCrossing=true;
var mouth=confront.gameObject.AddComponent<BoxCollider>();mouth.isTrigger=true;mouth.size=new Vector3(40,5,3);mouth.center=Vector3.up*1.5f;
var entry=WorldBuilder.Membrane("Great Lantern seal",new Vector3(0,1.5f,116.5f),23,true,6);
var duel=WorldBuilder.Encounter("Relay_Hessa",new Vector3(0,1,138),new Vector3(38,5,28),"HessaKorl",1,new[]{entry});
var post=duel.GetComponentInChildren<Spawner>();post.transform.position=new Vector3(3,0,129);post.transform.rotation=Quaternion.Euler(0,180,0);post.faceAsPlaced=true;
duel.requiredFlag="relay.hessaConfronted";duel.checkpointSpawn="Vault";
var viewpoint=new GameObject("Hessa preview");viewpoint.transform.position=new Vector3(0,0,124);duel.previewPoint=viewpoint.transform;duel.previewRadius=6;confront.previewEncounter=duel;
```

   **The door is a checkpoint (D204):** `EncounterVolume.Begin` sets `state.spawn=checkpointSpawn` and autosaves, closes its membranes, wakes the boss; the last death opens them, sets `clear.<encounterId>` and saves. A defeat returns to that spawn with the party whole and the boss at full integrity. `previewPoint` makes the real boss visible and inert (`DormantEnemy`) until the fight. The yarn confrontation sets the encounter's `requiredFlag` with `<<setFlag>>`. Chapter 2 then removes the membrane's drawn cube, strings strands of light across it (hidden until it shuts) and gives it a disabled carving `NavMeshObstacle` (LRB 1264-1276).
8. **After the fight.** `bossdown.<id>` is set and the zone's music returns. A boss who yields hands over to a hidden `Npc` through `YieldedCaptain{npc,bossId,downFlag,heardFlag,guards,guardedFlag,guardedNode}` (LRB 1285-1287). The way out is a `Gate` locked on the "heard" flag.
9. **Tests** (section 11): the boss lists in `QaPlanCombatTests.cs` (title card, every special marked, an opening per phase, each shift, duration, retry at the door), the checkpoint tuple in `QaStoryWorldTests.EveryBossDoorIsACheckpoint`, a `NoWayOut` wrapper and the boss-track test in `QaPlanSystemsTests.cs`, and the fight's own tests in the chapter class (`HessasDodgedChainLeavesHerOpenAndHerFinisherWaitsABeat`, `HessasPhaseShiftsCallHerCrew` in `LanternRoadTests.cs`).

Not available without new work: boss animation beyond `Pose` on the existing body, and a boss track other than "Alien Boss Battle".

## 7. People

### 7.1 A named NPC

1. **CharacterDef** in `Define()`: `Person("Aumi","Keeper Aumi",true,"AumiBody","Characters","Neve")` sets `id=speakerId`, `displayName`, `female` and the `natural` body (with a fallback body until the reskin lands). The id is the Yarn speaker and the save key; `displayName` is what a player reads.
2. **Portrait.** One 4x4 sheet of sixteen expressions in `PortraitEmotion` order (`Neutral, Happy, Sad, Angry, Shocked, Worried, Intense, Synced, Amused, Smug, Curious, Flustered, Tender, Determined, Playful, Special`; append-only). `python tools/portrait_pipeline.py --sheet <png> --name <Id>_Natural --order 0,1,...,15` slices, upscales and assembles `art-src/Generated/P3/portraits/<name>/<name>.png` (2304x2304); `--order` is the reviewed source cell for each expression, never guessed. Then `PortraitIntake.ImportNamed` with `-portrait-names <Id>_Natural,...` writes the sliced sheet and `A/Resources/Portraits/<Id>_Natural.asset` and binds `CharacterDef.portraitNatural`. A missing portrait hides the picture silently; a missing expression falls back to Neutral silently; the tests don't.
3. **Place** with `Person(id,at,yaw,postFlag,post,finalFlag,finalNode)`, which wraps `WorldBuilder.Npc`: nodes `<Id>First`, `<Id>Repeat`, `<Id>Post`, prompt "Talk to <display name>", range 3.2, a capsule, a name label. `Npc`'s own default `postFlag` is `"warpkey"`, so always set it.
4. **Which node plays** (`Npc.Interact`): a `holdNode`; else a side-story beat (`SideStories.NodeFor`); else the first meeting while `met.<Id>` is unset; else `finalNode` once `finalFlag` is set; else `postNode` once `postFlag` is set; else `repeatNode`. A finished conversation reports `TalkTo <speaker>` to the quests.
5. **Post-states.** Different words: the tiers above, or a beat. Gone or arrived: a `FlagGate` (Pell leaves when `pell.aboard` is set; her lamp lights on `pell.lampReturned`). Decided once at load: a small component (`YieldedCaptain`).

### 7.2 Dialogue

Source is `docs/writing/<Region>.yarn`; the builder's `Dialogue` entry copies every `.yarn` and `barks.json` into `Resources` and reimports `Lattice.yarnproject` (which globs `**/*.yarn`), then checks a hard-coded list of node names. **A new `.yarn` file is not in the compiled program until that runs, and a missing node fails silently at runtime.** Give your builder its own `RefreshDialogue` list.

```
title: CorraFirst
---
Corra: Fresh faces, fresh scrip. Corra Vey, arch market. I sell gel, parts and advice, and the advice is free. #emotion:Happy
-> Show us what you have.
    <<shop>>
-> Maybe later.
    Corra: Later's fine. Later still pays. #emotion:Smug
===
```

- Every line is `Speaker: text #emotion:Name`. The speaker is a `CharacterDef` id (`Keeper:` shows as "Keeper Abri"). An unknown emotion logs and plays Neutral.
- Commands (`S/Dialogue/DialogueSystem.cs`): `<<setFlag key>>` (also reports a `Flag` objective; a key starting `chapter` autosaves when the talk closes), `<<startQuest Id>>`, `<<giveItem Id n>>`, `<<shop>>`, `<<repair>>`, `<<bench>>`, `<<joinParty Id>>`, `<<warpUnlock>>`, `<<stationShift>>`. Flags, quests and items take effect the moment the command is reached; a shop, repair, bench or party join waits for the conversation to finish, and an interrupted one drops them.
- A line said by nobody present: `DiscoveryPoint` with a `dialogueNode` (Sela at a relay). On arrival or in a fight: a bark (`barks.json`, `{"Speaker":{"situation":"line|line"}}`; `zone` and `zone.flight` on arrival).
- Words: no ids, no camel case, no `TODO`. The player-text check rejects any word shaped like `aB...C` or `word.word`, and a list of retired phrases, including the bare substring `bridge` (section 13).
- Compulsory conversations stay under 90 s at reading pace. Replay routes press through a conversation line by line, so changing a node's length changes its route (section 10).

### 7.3 Side-story beats

`SideStories.Beats` (`S/World/SideStories.cs`) is an ordered list of `new("Speaker","Node",state=>condition)`; the first that matches plays instead of the speaker's tier, so order is priority. Write each so it can only be true once its speaker has been met (`Met(s,"Pell")`, or a quest step that implies it): a beat must never preempt a first meeting. Offers, returns and afterwords all go here:

```csharp
new("Pell","PellLampReturn",s=>Step(s,"PellsLamp")==1),
new("Pell","PellLampOffer",s=>Step(s,"PellsLamp")<0&&Met(s,"Pell")&&Step(s,"LanternRoad")>=3),
new("Pell","PellLampAfter",s=>Flag(s,"pell.lampReturned")&&!Flag(s,"lanternwick.chartGiven")),
```

A beat that stays true after it has been heard (a repeat line) must be added to `PeopleMarks.Repeats`, or its speaker wears a "!" forever.

### 7.4 Townsfolk

Unnamed residents are props with a body (LRB `Townsfolk`, `Townsperson`): an existing character prefab as `"Generated civilian"`, a `PropTint` so nobody takes one for the person whose body it borrows, a static `BoxCollider` (.7 x 1.9 x .7) so the bake walks round them, animators set to `CullCompletely`. Keep them off every route, off signs, and out of the camera's line to anyone with a name; the frame can hold about four animated bodies. A walking civilian is `ScriptedWalk` (chapter 1's market morning; staged by `MarketOpeningStaging`).

## 8. Quests

`QuestDef` (`S/Data/QuestDef.cs`): `id`, `title`, `description`, `steps[]` (`kind`, `target`, `count`), `stepText[]` (one per step, never blank), `rewardXp`, `rewardScrip`, `rewards[]` (`Ingredient{id,count}`), `flagsOnComplete[]`.

| Kind | `target` | What reports it |
|---|---|---|
| `Reach` | scene name | `ArenaRuntime` on every zone load |
| `TalkTo` | speaker id | a conversation that finishes |
| `Kill` | enemy id | `LootDrop` on death |
| `Collect` | material id | `Inventory.Grant` |
| `Interact` | a `DiscoveryPoint.report` string; `count` points share it | `DiscoveryPoint` when taken |
| `Flag` | a flag key | **only** `<<setFlag>>` in Yarn (a flag set by `FlagAll` or code doesn't advance a step until the next load) |

```csharp
ArenaBuilder.Asset<QuestDef>("PellsLamp",q=>{q.id="PellsLamp";q.title="Light for a Lamp-Keeper";
    q.description="Pell lost her mother's lamp to a grazer in Lantern Gorge and hasn't told Aumi.";
    q.steps=new[]{Step(ObjectiveKind.Interact,"PellsLamp"),Step(ObjectiveKind.Flag,"pell.lampReturned")};
    q.stepText=new[]{"Search the grazer den under the fallen tower, west of the path before the second relay","Bring the lamp back to Pell"};
    q.rewardXp=180;q.rewardScrip=120;q.rewards=new[]{new Ingredient{id="ChargeCell",count=2}};q.flagsOnComplete=Array.Empty<string>();});
```

- **Main quest.** Starts from a conversation (`<<startQuest LanternRoad>>` in `AbriLanternTale`), walks `Reach`/`TalkTo`/`Interact`/`Kill`, ends on a `Flag` set by the closing talk, and sets `chapter<N>.complete`. Add its id to `PauseMenu.MainQuests` and write the objective line for each state in `ObjectiveHud`'s switch (label plus a target prompt or point).
- **Side quest (D199).** An offer node with `<<startQuest>>`, a thing in the world, a return node with `<<setFlag>>`. Steps advance on flags the side conversations set, never on `TalkTo`, so the main story's talk with the same person can't tick them. Exactly one node in the whole script starts each quest.
- **A thing to take** is `Discovery(name,at,prompt,flag,report,node,speaker,quest,step)` with `quest`/`step` set: offered only while that quest is at that step. Give it a `visual` (shown only while it can be taken) and switch the visual off in the builder. Make it look unlike its surroundings (the ration crate is the one pale crate, under a light).
- **Briefs.** A side step's line under the objective is `QuestText.Briefs["<Quest>/<step>"]=(about 40 characters, speaker it waits on)`; without an entry the full `stepText` is used and must still fit one line at size 24.
- **Completion** pays once: scrip, XP to each member, rewards, `quest.<id>.complete`, `flagsOnComplete`.
- **Balance (D208).** 125 XP a level; a test sums every side quest's `rewardXp` into the Sync band. New quests move that sum.

Hand-written definition assets also exist (`QuestDef/NeveParcel.asset` is YAML written by hand with its own `.meta`). Prefer `Define()`: it needs no GUIDs and survives review as code.

## 9. Shops, loot, recipes

- **Shop.** `<<shop>>` opens `GameCatalog.Find<ShopInventory>(<current zone id>)`: one stall per zone, its asset named for the zone (`ShopInventory/Lanternwick.asset`, LRB 174-181). A `Shop` component with its own `inventory` is the other way in. A shop opened from a conversation lands on its page toggle so a tapping thumb buys nothing (D222).
  - At `ff99a7a` the stock is three arrays and `prices`, read by position in the order the rows are shown: parts **sorted by `GearSlot`** (Emitter, Edge, Frame, Drive, Module), then consumables, then materials (sold three at a time); a missing price falls back to 60, 20 or 12, and a zone with no inventory asset sells chapter 1's starter stock without complaint.
  - **S2** replaces those arrays with `ShopOffer[] offers` (`{id,price,lot,requires}`: each row its own price, shown in order, `requires` a flag that puts it on the counter) and adds `buysMaterials`. Write a new region's stall in that form once it lands; LRB's lambda changes with it.
- **Loot.** `LootTable` named for the enemy; entries `{id,count,chance}` (fortune adds up to .15). Every kill also pays XP, 8 scrip (150 for a boss) and `ScrapAlloy`. **S2** adds `partTier`/`partSlot`/`anySlot` to `LootEntry` (any part of a tier, with rolled affixes), `EnemyDef.scrip`/`scrap` (below zero means the old amounts) and `QuestDef.rewardsRolled`.
- **Items.** `MaterialDef`, `ConsumableDef`, `TechPartDef`, `RecipeDef`, `AffixDef`, all through `ArenaBuilder.Asset<T>` (models in `ArenaBuilder.Definitions`: tiers, the trophy parts). A part's `displayName` must equal `DisplayNames.Item(id)`; a recipe's must equal its output's; every word capitalised. A consumable recipe names no discipline (D222).
- **Services.** `Bench` (prompt "Fabricate — Lattice Anvil") on a `LatticeAnvil`; `RepairBay` on a `DeckConsole` (`free`, `saveSpawn`, a prompt ending "repair and save"; a paid bay prints its fee itself).
- `tools/create_data_defs.py` wrote the first definition classes in September and is stale. Don't run it.

## 10. Replay routes

The driver (`S/UI/ContinuousReview.cs`, schema `S/UI/QualityRoute.cs`) plays a route JSON with a virtual pad: real inputs, no invulnerability, no pathfinding. A route is a list of flat steps, each: `name`, `seconds` (at most 180), held `buttons` / stick `x`,`y` / triggers, optional steering (`navigate` + `point` + `tolerance`, `approachTarget`, `approachPartner`), reactions (`evadeTelegraphs`, `evadeProjectiles`), `pickRow` for menus, an end condition (`until`, required; `stopWhen`, not required: `arrived`, `targetDown`, `partyAlive`, `flag:<key>`, `ui:<state>`, `scene:<zone>`, `row:<name>`) and post-conditions (`expectedScene`, `expectedFlag`, `expectedUi`, `expectedForm`, `expectedCharacter`). It parses with `JsonUtility`, so **a misspelt key is ignored without a word**.

Chapter routes are generated: **edit the generator, never the JSON.** Copy `tools/lantern_routes.py` (import its `Route` class; generation is under `__main__`):

```python
def gorge():                                                                  # abridged from the real one
    r = Route("chapter-14-lantern-gorge", "Chapter 2: three relays, the den, the camp, the door")
    r.resume("LanternGorge", "met.Aumi", speed=COMBAT_SPEED, spawn=(0, 0))    # Title, Continue, pick the autosave, assert flag and scene
    r.lead("Taren")
    r.walk("into the gorge", 2, 0, 24)                                        # a straight line to x, y, z
    r.fight_ground("Gorge_0")                                                 # lock, skills, chase, dodge, gel; ends on flag:clear.Gorge_0
    r.walk("to the first relay", 8.2, 0.2, 50.5, tolerance=0.5)
    r.talk("first gorge relay", 3, flag="gorge.relay1")                       # presses through 3 lines, waits for ui:world, asserts the flag
    r.walk("to Pell's lamp", -24.5, 0, 99.5, tolerance=1)
    r.add("pick up the chewed lamp", 0.3, buttons=["South"], expectedFlag="pell.lampFound")
    r.add("release lamp", 0.5)
    r.walk("to the buried door", 0, 0.2, 226.8, tolerance=0.5)
    r.scene = "SunkenRelay"                                                   # set before the step that changes zone
    r.add("enter the Sunken Relay", 4, buttons=["South"], expectedUi="world", expectedForm="Shaped")
    r.write()
```

- Other helpers: `path(name, points)`, `press`, `read`, `talk_to_shop`, `wait`, `gel`, `recover`, `fight_flight(id, rounds, seconds)`. A boss is written inline as rounds that `stopWhen "flag:bossdown.<Id>"` (lines 311-321).
- Coordinates are literals copied from the builder. Walk points must be joined by straight walkable lines; `Lattice.EditorTools.QaRoutePaths.Print` [lead] asks a zone's baked navigation for the corners between points.
- Each route starts from the save the previous one left: Title, Continue, the autosave, then `expectedFlag` and `expectedScene`. So a route ends somewhere that autosaves (a zone load, a cradle, a cleared fight, a boss door).
- Name routes `chapter-NN-<name>`, numbered on from `chapter-16-lantern-home`. **The only registration is the `-Routes` argument** of `scripts/quality-chain.ps1`; its default list stops at chapter 11. Add the new names to the command in `NEXT_SESSION_PROMPT.md`.
- A wipe pauses the game: unless the step carries `pauseUi:"defeat"` the segment is rejected and the chain retries it once (`s15b`). Budget a fight's rounds for a party at the Sync the previous route leaves.
- Town routes matter to the town: `QaWorldSecondTests.cs` hard-codes Lanternwick's walked waypoints as `TownRoutes` and keeps townspeople 1.6 m off them.
- After a chain [lead]: `python tools/boss_durations.py Builds/quality/<run>` (120 to 300 s), `python tools/quality_analyze.py <segment>`, `python tools/eye_sheet.py <segment>` (needs `-Capture`). The performance bar is a median of 16.7 ms and a p95 at or under 17.1 ms.

## 11. Tests

House pattern (`T/PlayMode/LanternRoadTests.cs`): `public sealed class <Topic>Tests`, one per file, no base class; each class carries its own `Boot` (`GameTime.Reset();SceneManager.LoadScene("_Boot");... DevLoadout.Apply("starter");`), `Cleanup`, `Zone(zone,spawn)`, `Flag`, `Step`, `Talk(npc)`, `Discover(point)`. Method names are a sentence about what a player sees; messages say it too; a precondition's message starts `"fixture:"`. No `[Category]`, no `[Timeout]`: waits are deadlines on `Time.unscaledTime`. Bosses and enemies are fought in `Arena_Ground`/`Arena_Flight` with `ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),pos)`, `boss.Perform("Move")` and `health.Receive(new DamagePacket{...})`. `DevLoadout` knows only `starter`, `moon`, `hushwell`, `gullet`: a chapter test sets its own flags and quest steps. `tools/split_suite.py` picks up a class only if its name ends in `Tests`. Throwaway render tests are named `QaDebug*` and deleted.

```csharp
[UnityTest] public IEnumerator EveryChapterTwoZoneLoadsWithItsWayInAndOut()
{
    var expected=new Dictionary<string,(ZoneKind kind,string[] exits)>{["LanternGorge"]=(ZoneKind.GroundCombat,new[]{"Lanternwick","SunkenRelay"})};
    foreach(var (zone,(kind,exits)) in expected)
    {
        yield return Zone(zone);
        Assert.AreEqual(kind,ZoneController.Current.definition.kind,zone);
        var ways=Object.FindObjectsByType<DockingPad>(FindObjectsSortMode.None).Select(d=>d.scene).Concat(Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).Select(w=>w.scene)).ToArray();
        foreach(var exit in exits)CollectionAssert.Contains(ways,exit,zone+" has no way to "+exit);
        StringAssert.DoesNotContain("Practice",Objective(),zone+" has no objective of its own");
        if(kind!=ZoneKind.SpaceCombat)Assert.IsNotNull(Object.FindFirstObjectByType<GroundNavigation>(),zone+" has no navigation");
    }
}
```

### 11.1 Tests that fail by themselves when new content is incomplete

| Test (file) | Fails when |
|---|---|
| `EveryZoneVolumeIsSharpAndGraded` (`T/EditMode/ZoneLookTests.cs`) | a `Settings/*Volume.asset` has no colour grade (a zone built but never given `ZoneLook.Apply`) or has blur |
| `TheCatalogKeepsEveryKindWhereFindLooksAndHandsOutItsOwnArray` (`QaNov10MenuTests.cs`) | a definition is outside `Resources/Definitions/<TypeName>/` |
| `SaveRowsReadInWordsForEveryZone...` (`QaPlanEarlyTests.cs`) | a zone's save row shows camel case or an underscore |
| `NoRawIdOrWrongWordInTheScriptOrTheData` (`QaPlanWorldTests.cs`) | any yarn line, bark, quest text, item, skill, shop title or enemy name contains an id-shaped word, `TODO`, `placeholder`, or a retired word |
| `ItemNamesAreSpelledOneWayEverywhere` (`QaPlanSystemsTests.cs`) | an item's `displayName` isn't `DisplayNames.Item(id)`, or the script spells an item in different casing |
| `EveryOrdinaryEnemysWarningSoundHasAMarkOnTheGround` (`QaPlanSystemsTests.cs`) | a non-boss enemy doesn't tell, mark and sound within 4 s |
| `EveryQuestsCardsFollowItsStepsAndItsRewardIsPaidOnce`, `EveryQuestKeepsItsPlaceThroughASaveAtEveryStep`, `NoQuestMovesOnOutOfTurn` (`QaPlanWorldTests.cs`) | a quest has blank text, `stepText` and `steps` differ in length, a reward pays twice. **The first also asserts exactly ten quests exist** |
| `TheQuestLogLeadsWithTheMainQuest...` (`QaMenusTests.cs`) | also pins ten quests, and "The Lantern Road" as the first row |
| `EverySideQuestStepFitsOneLineOfThePanelAtTheTypeFloor` (`QaUiFlowTests.cs`) | a step's brief is too long. A new main quest counts as a side quest until its id joins the exclusion |
| `ThePlansSyncBandHoldsWithEverySideQuestDone` (`QaPlanEarlyTests.cs`) | total quest XP moves the party out of Sync 8 to 10, or past 11 |
| `EveryLineAndChoiceFitsItsBox`, `EverySpeakerShowsTheirOwnFaceInTheEmotionAsked` (`DialogueLayoutTests.cs`) | a line has no speaker or overflows; a speaker has no portrait set of their own, or lacks an expression a line asks for |
| `EveryCompiledStoryNodeAndChoiceAcceptsRapidInput...` (`DialogueInputTests.cs`) | a node's name doesn't begin with a speaker in its hard-coded list (it throws); a node with choices isn't in `branches` |
| `EveryBustIsSeatedOnTheFramesInnerEdge` (`QaHudLookTests.cs`) | a portrait sprite stops short of the frame |
| `TheScriptStopsPromisingWhatIsntThere` (`QaStoryWorldTests.cs`) | the script contains a banned phrase, among them the substring `bridge` |
| `TheFirstMarkedAttacksTeach...` (`QaHudFlowTests.cs`) | `docs/writing/barks.json` and the `Resources` copy differ |
| `EveryRouteThroughThePickerNamesItsSave`, `EveryRouteBuyingAtAStallNamesTheRow` (`QaFlowTests.cs`, `QaLastPlayTests.cs`) | a route picks a save or buys without `pickRow` |
| `EveryExitNamesWhereItGoes...`, `EveryLockedExitStaysShut...`, `EveryConversationLockAndQuestStepIsWired...` (`QaPlanWorldTests.cs`) | **an existing zone gains an exit** to a scene outside the list, a lock on a flag not in `WhatOpensIt`, a label with no `LabelNames` arm; or any quest step nothing in the listed zones can answer |

### 11.2 Lists a new chapter must be added to

No test discovers zones. A zone not added here is simply not checked.

| Where | What to add |
|---|---|
| `QaPlanWorldTests.cs` `Zones` (29) and `QaChapterTwoLookTests.cs` `Everywhere` (23) | every new zone: exits, spawns, locks, placeholders, stretched pieces, words on prompts and signs, wiring |
| `QaPlanWorldTests.cs` `LabelNames` (338), `WhatOpensIt` (425), the quest-starter table (1355), compulsory conversations (1243) | an arm per scene, a key per lock flag, `(quest, starting node)`, each must-hear node |
| `QaPlanWorldTests.cs` `[Values]` on `NothingFloatsOrSinksIn`, `EveryPromptCanBeReachedOnFoot`, `NoWalkableEdgeDropsIntoNothing`, `NoInvisibleWallStandsInTheOpen`, `TheBodyNeverSticksAtAWallFootOrAProp`, `NoGroundArrivalOpensOnACloseUp`, `TheCompanionKeepsUpOnFoot`, `NobodyStandsInsideAProp`, `CliffBandsRunLevel...` | each ground zone |
| `QaPlanWorldTests.cs` `EveryFlightPromptHasRoomForAHull...`; `QaPlanEarlyTests.cs` `AHullDrivenIntoARealWallGetsBackOff`, `EveryDockAnswersAHullFlownIn...` | each flight zone |
| `QaPlanEarlyTests.cs` `TheCameraNeverHidesTheHeroAtTheFootOfAWall`; `QaWorldSecondTests.cs` `NothingWideStandsBetweenTheCameraAndAFight`; `QaChapterTwoLookTests.cs` `NobodyStandsBehindTheScenery`, `FloorAndWallMeetInShade` | zones with walls, fights, people |
| `QaChapterOneLookTests.cs` sign tests; `QaPlanSystemsTests.cs` `WorldLabelsRead...`; `QaUiFlowTests.cs` `WarpSignsRead...`; `QaStoryWorldTests.cs` `EveryLockedWarpNamesWhatOpensIt`, `EveryChapterTwoExitIsSigned` | zones with signs and locked exits |
| `QaPlanSystemsTests.cs` `EachZonePlaysThePlansTrack` (582); `QaLookAndSoundTests.cs` `EachNaturalPlaceHasItsOwnAir`; `QaWorldBeatsTests.cs` `TheChapterTwoZonesKeepTheirAuthoredLight` | `(zone, track)`; ambience; authored light |
| `QaPlanCombatTests.cs` `AllFourteen` (587) and the tell tables (528, 533) | each ordinary enemy |
| `QaPlanCombatTests.cs` boss `[Values]`, `Specials`, the phase-move, promised-move, sync and door switches; `QaPlanSystemsTests.cs` `NoWayOut(...)`, boss cue lists, `EachBossFightPlaysTheBossTrack...`; `QaStoryWorldTests.cs` `EveryBossDoorIsACheckpoint`; `QaCombatTests.cs` and `QaFightLookTests.cs` duration tuples | each boss. **The switches' default arms are Hessa's: a new id added to `[Values]` without its own arms is silently tested against her moves** |
| `QaCombatTests.cs` XP ledger (449); `QaPlanEarlyTests.cs` (1153) and `QaUiFlowTests.cs` (474) main-quest exclusions; both "ten quests" pins | progression |
| `DialogueInputTests.cs` speaker prefixes (97) and `branches` | each new speaker; each node with choices |
| `LanternRoadTests.cs`-style class for the chapter | zones and exits, the story from first node to last flag, the chapter's mechanism, the boss, the people |

Rules worth knowing before building (all in those tests): props float if more than .25 m over ground and sink if half buried; a textured piece may not be scaled 5x more on one broad side than the other; cliff strata stay level; every prompt has a walkable point within its range, reachable from Arrival; the companion ends a 140 m walk within 8 m; no walkable rim drops into nothing; no invisible wall stands in the open; floor signs are at least 36 px tall at 1080p and clear of props; a boss door's checkpoint spawn is within 60 m and on navigation.

## 12. Rebuild and verify

**[stream]**, any time, no Unity:

```powershell
python tools/typecheck.py                      # every Lattice assembly with Unity's own Roslyn; or: python tools/typecheck.py Combat World Editor
python tools/<region>_routes.py                # rewrites the chapter's route JSON
python tools/quality_route.py docs/quality/routes/<route>.json    # the preflight the replay runs
```

`typecheck.py` reuses the response files of Unity's last compile (`Lattice/Library/Bee`), so it needs one earlier Unity compile on the machine and doesn't see a new assembly or package reference. Yarn isn't compiled outside Unity.

**[lead]**, in this order (each `& .\scripts\exec.ps1 -Method Lattice.EditorTools.<Method> -Marker <MARKER> [-Graphics] [-ExtraArgs @(...)]`; session-start safety in NEXT_SESSION_PROMPT first):

| # | Step | Method | Marker | Arguments |
|---|---|---|---|---|
| 1 | compile | `.\scripts\headless.ps1 -Mode verify` | `VERIFY_OK` | |
| 2 | definitions (before any enemy intake) | `<Region>Builder.Definitions` | `<REGION>_DEFS_OK` | |
| 3 | generated art | `GenIntake.BuildPrefix` | `GEN_INTAKE_OK` | `-ExtraArgs @('-intake-prefix','<ids>')` |
| 4 | portraits | `PortraitIntake.ImportNamed` | `PORTRAIT_INTAKE_OK` | `-ExtraArgs @('-portrait-names','A_Natural,B_Natural')` |
| 5 | music | `MusicIntake.Stage` | `MUSIC_STAGED_OK` | |
| 6 | dialogue (after any `.yarn` or bark change) | `<Region>Builder.Dialogue` | `<REGION>_DIALOGUE_OK` | |
| 7 | zones | `<Region>Builder.BuildZones` | `<REGION>_ZONES_OK` | `-Graphics -ExtraArgs @('-<region>-zones','A,B')` |
| 8 | their light (after every zone rebuild) | `ZoneLook.Apply` | `ZONE_LOOK_OK` | `-Graphics -ExtraArgs @('-look-zones','A,B')` |
| 9 | the way in from an older scene | the builder's staging entry (LRB `StageApproach`) | its marker | re-run after that scene's own rebuild or staging |
| 10 | focused tests | `.\scripts\headless.ps1 -Mode playtests -Graphics -TestFilter "Lattice.Tests.PlayMode.<A>\|Lattice.Tests.PlayMode.<B>" -TimeoutSec 2400`; `-Mode tests` for EditMode | | |
| 11 | players | `.\scripts\headless.ps1 -Mode build -TimeoutSec 3600 ; .\scripts\headless.ps1 -Mode builddev -TimeoutSec 3600` | `BUILD_OK` | move the previous `Builds/Windows*` aside first |
| 12 | the chain | `.\scripts\quality-chain.ps1 -Output Builds/quality/<run> -Routes <all routes in order> -Build Release` | `CHAIN_OK` in `chain.txt` | about an hour; alone, displays awake, nothing else issued |
| 13 | the full suite (chapters 5, 8 and the end) | `.\scripts\full-suite.ps1 -Label <name>` then `python tools/suite_results.py <name>` | | about 2.5 hours, in the background |

Afterwards: delete any `A/Resources/WorldMaterials/gullet-membrane-<w>-<d>.mat*` a new seal created and nothing uses (each `WorldBuilder.Membrane` size makes one), revert `packages-lock.json` and `staged-light-line.mat`, and check staged paths before committing. Rebuilding chapter 1 or 2 scenes has its own order (the table in NEXT_SESSION_PROMPT, plus `QaNov10Staging.Apply`, marker `QA_NOV10_STAGED_OK`, after `ChapterOneStaging`).

A chapter is done when it plays first scene to last on the replay driver, its captures have been looked at frame by frame, its focused tests pass, the earlier chapters' chain still passes and a build exists (SHIPPING_PLAN section 6). Each new map also owes MAP_EYE_TEST twice: on the blockout, and in the player.

## 13. Pitfalls

Things that fail without saying so:

1. **A missing yarn node** does nothing when talked to. A new `.yarn` file isn't compiled until the dialogue step runs.
2. **A zone missing from `MusicDirector.SetLocation`** is silent; from `ObjectiveHud`, it shows the arena's practice line; from `BatchTools.RegisterScenes`, it can't be loaded at all, because every zone build and player build rewrites the build list from that array.
3. **`ZoneDef.musicId` does nothing.** Hushwell's builder sets it; nothing reads it.
4. **Component defaults are chapter 1's.** `WarpBeacon.requiredFlag="warpkey"`, `scene="Gullet_Tunnel"`; `Npc.postFlag="warpkey"`; `DiscoveryPoint.flag="hushwell.nursery"`, `report="HushwellNursery"`, `dialogueNode="SelaNursery"`; `LightCurtain.flag="wick.gateOpen"`; `YieldedCaptain` is all Hessa. Use the helpers, which set every field, or set every field.
5. **A `Flag` quest step set from code** (not `<<setFlag>>`) waits for the next load.
6. **One shop per zone**, found by the zone's id; at `ff99a7a`, with no asset by that name `<<shop>>` sells the starter stock at fallback prices, and prices follow parts sorted by slot, not the array's order (S2 is replacing both).
7. **`ArenaBuilder.Asset` overwrites what its lambda names**: a value tuned in the asset is undone by the next `Define()`, and several tests compare builder source with asset.
8. **Shader variants** not enabled on a material in the build are stripped. **Property blocks** aren't saved with a scene.
9. **Only static box and mesh colliders reach the navigation bake.**
10. **A scatter that moves instead of drops**, or a kit key inserted mid-list, shifts every later prop and breaks every route through the zone.
11. **`JsonUtility` ignores unknown route keys**; a route's `sourceRevision` and step names are free text nobody checks.
12. **`GenIntake` binds an enemy row only if the EnemyDef exists**; on "Unknown enemy" it has already saved the prefab and aborts the remaining rows.
13. **Boss test switches default to Hessa**, and `BossController.Update`'s chooser defaults to the Burrower's charge: an unregistered boss fights with someone else's moves.
14. **The word `bridge`** in any `.yarn` fails `TheScriptStopsPromisingWhatIsntThere` (chapter 1 retired a bridge). Chapter 3 is a river town: that ban has to be narrowed by the lead, with the reason recorded, before a writer needs the word.
15. **Two pins on "ten quests"** fail on the first new `QuestDef`. They are fixtures to update deliberately, not assertions to loosen.
16. **`ZoneLook.Apply` after every zone rebuild**, or the zone ships with the builder's flat light.
17. **A `Membrane` is a blockout cube** with a generated material variant; chapter 2 replaced its renderers with strands. Don't leave the cube drawn.
18. **`tools/create_data_defs.py`, `WorldBuilder.BuildAll`, `GenIntake.Build`** are old whole-project entry points. `meshy.py` still prints "run PackStaging.StagePacks", which doesn't exist. `docs/ARCHITECTURE.md` section 10 and `docs/art/PIPELINE.md` line 13 describe names and methods the code no longer has.

Structural limits a new chapter will run into: LRB's helpers are private and canyon-specific; `RoomDressing` knows chapter 1's seven zones only; `ObjectiveHud`, `MusicDirector`, `BossController`, `ActorFactory` and `EnemyBrain` are keyed on literal ids, so parallel streams will all edit the same switches (merge by the lead); boss music is one hard-coded track; `tools/portrait_pipeline.py` needs a Real-ESRGAN executable whose default path is in the FrostboundUnity repo; `DevLoadout` has no chapter 2 or later loadout; there are no `docs/maps/` briefs for chapter 2's zones to copy; the driver's party logic assumes two members; and no test or tool checks that a route parses, uses real keys or names a real scene.

## 14. Unverified

- **Who sets `ZoneDef.ambientAudio`.** Five zones have a bed and no script writes the field; presumably the asset was edited directly. Treat as a manual step for the lead.
- **The rule for `.meta` files.** Most are Unity-written; about 70 script metas and some definition metas were written by hand or by a tool (two lines, CRLF, a fresh GUID). No document states a rule. Both forms import.
- **The exact `exec.ps1` lines for `PortraitIntake.ImportNamed`, `MusicIntake.Stage` and `QaRoutePaths.Print`** (`-Marker QA_PATHS_OK -ExtraArgs @('-qa-paths','<in>','-qa-paths-out','<out>')`, one request a line: `Scene x y z x y z ...`) follow the documented pattern but appear in no document.
- **`headless.ps1 -Mode lint`** is accepted as a mode and described as a writing gate in the header; no implementation was found.
- **Whether routes `chapter-01` to `chapter-11` came from a generator.** No tool names them.
- **SpaceSafe hubs and tunnel-shaped SpaceCombat.** `StationRedesign`, `TallowRedesign` and `GulletBuilder` were read for their entry points and placement calls only; the Wick Reach is the flight zone this guide describes in full. Chapter 1's zones also pass through `ChapterOneStaging`, `RoomDressing`, `MarketOpeningStaging`, `SideStoryStaging` and `QaNov10Staging`, a layering chapter 2 does not use and new chapters should not copy.
- **`EnemyDef.stats`, `ZoneDef.safePockets`, `ZoneDef.mapIcon`** exist on the classes; no use was found or looked for beyond a grep.
- **Whether `typecheck.py` sees a brand-new `.cs` in every case.** It globs each assembly folder, so it should; a new assembly or package reference it cannot see.
- **What S1 to S3 leave behind.** Sections 5, 6, 8 and 9 describe `EnemyDef`, `LootEntry`, `ShopInventory`, `QuestDef`, the consumables and `BossController.Packet` as committed at `ff99a7a`, with the S2 changes seen in the working tree noted. Reread those classes once the streams commit.
- **Nothing here was run.** A Unity test run held the project while this was written; every command is quoted from the scripts and NEXT_SESSION_PROMPT, not from a run.
