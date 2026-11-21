using System.IO;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Read-only look-development stills. Opens each zone, places static hero
    /// bodies at authored points and renders through the zone's own gameplay
    /// camera profile and post volume. Never saves a scene or asset.
    /// </summary>
    public static class LookDev
    {
        public struct View{public string name;public Vector3 point;public bool flight;public float zoom;public View(string n,Vector3 p,bool f=false,float z=1){name=n;point=p;flight=f;zoom=z;}}

        public static readonly (string zone,View[] views)[] Views=
        {
            ("Hub_Decks",new[]{new View("arrival",new Vector3(-20,0,0)),new View("promenade",new Vector3(0,0,0)),new View("market",new Vector3(20,0,0)),
                new View("vestibule",new Vector3(-28,0,-11)),new View("office",new Vector3(-31,0,4)),new View("cabins",new Vector3(-28,0,14)),new View("stalls",new Vector3(0,0,4)),
                new View("galley",new Vector3(0,0,14)),new View("workshop",new Vector3(29,0,5)),new View("stores",new Vector3(28,0,14)),new View("receiving",new Vector3(28,0,-11)),new View("service",new Vector3(0,0,21.5f)),
                // Paired overview with the Halo exterior (MAP_EYE_TEST exterior/interior agreement).
                new View("overview",new Vector3(0,0,3),false,3.6f),
                // The opening scene's walkway job (D195), staged in its closed morning state.
                new View("walkway",new Vector3(-14,0,-3.5f)),new View("walkway-wide",new Vector3(-14,0,-3),false,1.7f)}),
            ("Hub_CinderHalo",new[]{new View("dock",new Vector3(-20,1,0),true),new View("ring",new Vector3(25,1,25),true),
                new View("hulls-overview",new Vector3(0,1,0),true,4f),new View("station-overview",new Vector3(0,1,0),true,7f),
                // The old dock/ring points lie inside the redesigned hulls; these sit where ships actually fly.
                new View("office-dock",new Vector3(-28,1,-27),true),new View("market-roof",new Vector3(0,1,-25),true,2.2f),new View("workshop-roof",new Vector3(28,1,-25),true,2.2f)}),
            ("Sorrel_Ridges",new[]{new View("outpost",new Vector3(0,0,18)),new View("seam",new Vector3(-35,0,58)),new View("service",new Vector3(35,0,82)),new View("bore",new Vector3(8,0,164)),
                new View("landing",new Vector3(2,0,-11)),new View("habs",new Vector3(12,0,10)),new View("haul",new Vector3(5,0,47)),new View("basin",new Vector3(-5,0,96)),new View("drill",new Vector3(4,0,141)),
                // The Burrower's arena and the slope beside it, and the wrecked rig past the bore (E2-sorrel-07, E2-gullet-06).
                new View("arena-slope",new Vector3(10,0,168)),new View("rig",new Vector3(-2,0,180))}),
            ("Arena_Ground",new[]{new View("yard",new Vector3(-6,0,4))}),
            ("Hushwell",new[]{new View("descent",new Vector3(0,0,20)),new View("nursery",new Vector3(14,-15,334)),
                new View("breach",new Vector3(0,0,6)),new View("gallery-a",new Vector3(-12,0,48)),new View("gallery-b",new Vector3(8,0,84)),
                new View("survey-wall",new Vector3(5,0,112)),new View("ramp",new Vector3(1,-4,135)),new View("chamber-1",new Vector3(0,-8,164)),
                new View("chamber-2",new Vector3(-4,-8,196)),new View("chamber-3",new Vector3(1,-8,226)),new View("bellows",new Vector3(0,-8,262)),
                new View("east-pocket",new Vector3(40,-8,164)),new View("west-pocket",new Vector3(-36,-8,212)),new View("lower-ramp",new Vector3(9,-12,306)),
                new View("nursery-lift",new Vector3(24,-16,342)),
                // The Bellows' two doors, from the chamber side (E4-hushwell-04).
                new View("bellows-exit",new Vector3(0,-8,276)),new View("bellows-entry",new Vector3(0,-8,250))}),
            ("Gullet_Tunnel",new[]{new View("mouth",new Vector3(0,1,8),true),new View("throat",new Vector3(Mathf.Sin(320f/900*Mathf.PI*4)*12,1,320),true),
                new View("canal",new Vector3(GulletProfile.RouteX(70),1,70),true),new View("feeding",new Vector3(GulletProfile.RouteX(165),1,165),true),
                new View("feeding-far",new Vector3(GulletProfile.RouteX(225),1,225),true),new View("valve-0",new Vector3(GulletProfile.RouteX(252),1,252),true),
                new View("slalom",new Vector3(GulletProfile.RouteX(362),1,362),true),new View("eddy",new Vector3(GulletProfile.RouteX(410)+8,1,410),true),
                new View("gate",new Vector3(GulletProfile.RouteX(560),1,560),true),new View("gate-far",new Vector3(GulletProfile.RouteX(660),1,660),true),
                new View("coil-entry",new Vector3(GulletProfile.RouteX(745),1,745),true),new View("coil",new Vector3(GulletProfile.RouteX(805),1,805),true),
                new View("exit",new Vector3(GulletProfile.RouteX(880),1,880),true),
                // The way in as the party arrives, and drawn back as the first fight draws the view back (E2-gullet-15).
                new View("arrival",new Vector3(0,1,8),true,1.6f),new View("arrival-far",new Vector3(0,1,14),true,2.6f)}),
            ("TallowApproach",new[]{new View("refuge",new Vector3(0,1,-8),true),new View("deck",new Vector3(4.75f,1,18),true,1.8f),new View("mooring",new Vector3(-25,1,3),true),
                // The dock at the play camera's own distance, where the hull was soft beside the ships (E2-tallow-11).
                new View("dock-close",new Vector3(4.75f,1,12),true)}),
            ("TallowDrift",new[]{new View("refuge",new Vector3(0,0,-3)),new View("garden",new Vector3(6,0,3)),
                new View("waiting",new Vector3(-7,0,1)),new View("repair",new Vector3(7,0,5)),new View("arrival",new Vector3(0,0,-9)),new View("rest",new Vector3(0,0,8))}),
            // Chapter 2, the Lantern Road (D202).
            ("WickReach",new[]{new View("arrival",new Vector3(0,1,8),true),new View("relay",new Vector3(-22,1,150),true),new View("gate",new Vector3(0,1,250),true),
                new View("dock",new Vector3(0,1,282),true),new View("overview",new Vector3(0,1,150),true,4f)}),
            ("Lanternwick",new[]{new View("dock",new Vector3(0,0,-14)),new View("arch-market",new Vector3(0,0,14)),new View("wick-house",new Vector3(-12,0,28)),
                new View("lantern-stair",new Vector3(24,3,52)),new View("orchard",new Vector3(-28,1.4f,62)),new View("archive",new Vector3(0,0,92)),new View("overview",new Vector3(0,0,40),false,3.4f)}),
            ("LanternGorge",new[]{new View("entry",new Vector3(0,0,6)),new View("first-relay",new Vector3(6,0,52)),new View("den",new Vector3(-20,0,98)),
                new View("camp",new Vector3(-8,0,116)),new View("ridge",new Vector3(8,0,186)),new View("relay-door",new Vector3(0,0,222)),new View("overview",new Vector3(0,0,110),false,3.6f)}),
            ("SunkenRelay",new[]{new View("entry",new Vector3(0,0,8)),new View("corridor",new Vector3(0,0,32)),new View("gallery",new Vector3(0,0,66)),
                new View("vault",new Vector3(0,0,104)),new View("great-lantern",new Vector3(0,0,140)),new View("overview",new Vector3(0,0,90),false,3.6f)}),
            // Chapter 3, Carrying a Place. Nearspace is placed along its lane (XylosBuilder.Beside: metres along, metres to starboard): the named places, then the lane itself every
            // hundred and fifty metres or so, which is what "never an empty screen" is judged on.
            ("XylosNearspace",new[]{new View("arrival",new Vector3(0,1,8),true),new View("first-fight",XylosBuilder.Beside(130,0),true),new View("barge-1",XylosBuilder.Beside(252,17),true),
                new View("limb",XylosBuilder.Beside(380,0),true),new View("patrol",XylosBuilder.Beside(470,0),true),new View("nest",XylosBuilder.Beside(612,-34),true),
                new View("mirror",XylosBuilder.Beside(905,-10),true),new View("barge-2",XylosBuilder.Beside(952,-20),true),new View("barge-3",XylosBuilder.Beside(1438,-22),true),
                new View("claim",XylosBuilder.ClaimSide,true),new View("beacon",XylosBuilder.Beside(1640,0),true),
                new View("lane-0060",XylosBuilder.Beside(60,0),true),new View("lane-0560",XylosBuilder.Beside(560,0),true),new View("lane-0720",XylosBuilder.Beside(720,0),true),
                new View("lane-0820",XylosBuilder.Beside(820,0),true),new View("lane-1060",XylosBuilder.Beside(1060,0),true),new View("lane-1180",XylosBuilder.Beside(1180,0),true),
                new View("lane-1300",XylosBuilder.Beside(1300,0),true),new View("lane-1560",XylosBuilder.Beside(1560,0),true),
                new View("barge-1-wide",XylosBuilder.Beside(250,8),true,3f),new View("mirror-wide",XylosBuilder.Beside(930,-24),true,4.5f),new View("beacon-wide",XylosBuilder.Beside(1630,0),true,3f)}),
            ("Rootmarket",new[]{new View("arrival",new Vector3(0,0,17)),new View("mooring",new Vector3(29,0,19)),new View("landing-west",new Vector3(-8,0,24)),new View("ferry-stage",new Vector3(-25,0,46)),
                new View("raft-crossing",new Vector3(0,0,44)),new View("ring-northwest",new Vector3(-18,0,60)),new View("seed-house",new Vector3(2,0,68)),new View("seed-house-door",new Vector3(0,0,72)),
                new View("kitchen",new Vector3(25,0,65)),new View("press",new Vector3(37,0,45)),new View("river-gate",new Vector3(55,0,43)),new View("ring-southeast",new Vector3(22,0,31)),
                new View("rope-walk",new Vector3(-31,0,23)),new View("senn",new Vector3(-45,0,13.5f)),new View("upper-walk-west",new Vector3(-28,2,74)),new View("upper-walk-top",new Vector3(0,2.4f,101)),
                new View("upper-walk-east",new Vector3(28,2,85)),new View("overview",new Vector3(0,0,46),false,3.6f),new View("overview-north",new Vector3(0,0,82),false,2.4f)}),
            // The Braided River, west to east as it is walked: the north bank, the first island and its camp, the second island, Seedfall and the mooring. Three wide views tile its length.
            ("BraidedRiver",new[]{new View("arrival",new Vector3(0,0,122.6f)),new View("first-arch",new Vector3(36,0,124)),new View("thornback",new Vector3(76,0,123.5f)),new View("bank-east",new Vector3(132,0,122.5f)),
                new View("sluice-a",new Vector3(171.5f,0,114.5f)),new View("ferry-stage",new Vector3(103,0,96)),new View("camp",new Vector3(104,0,87)),new View("marrow-1",new Vector3(40,0,90)),
                new View("island-shore",new Vector3(70,0,92.5f)),new View("island-east",new Vector3(146,0,88)),new View("weir",new Vector3(160,0,89)),new View("marrow-2",new Vector3(214,0,99.5f)),
                new View("north-shore",new Vector3(260,0,102)),new View("ford",new Vector3(250.5f,0,83)),new View("ulma",new Vector3(250,0,71)),new View("island-two-east",new Vector3(296,0,101)),
                new View("sluice-b",new Vector3(323.5f,0,101.8f)),new View("seedfall-edge",new Vector3(343,0,90)),new View("seedfall",new Vector3(362,0,88)),new View("marrow-3",new Vector3(357,0,99)),
                new View("seedfall-south",new Vector3(362,0,79)),new View("root-bridge",new Vector3(350,0,67)),new View("mooring",new Vector3(386,0,90)),new View("west-bank",new Vector3(-12,0,119)),
                new View("overview-west",new Vector3(90,0,104),false,3.6f),new View("overview-mid",new Vector3(240,0,98),false,3.6f),new View("overview-east",new Vector3(350,0,94),false,2.8f)}),
            // The Walking Canopy in the order it is walked: the hall, down the gallery to the second anchor, up into the vault and the chamber behind it, back by the ladder, the basin.
            // Each point's height is its floor's (the hall is 2.8 m under the vault, the gallery and the basin lower still).
            ("WalkingCanopy",new[]{new View("arrival",new Vector3(2,-2.8f,2)),new View("hall",new Vector3(2,-2.8f,12)),new View("anchor-a",new Vector3(8.6f,-2.8f,13)),new View("gallery-door",new Vector3(-16.5f,-2.8f,10)),
                new View("gallery-low",new Vector3(-36.5f,-7,8)),new View("gallery-mid",new Vector3(-37.5f,-5.5f,30)),new View("ledge",new Vector3(-32,-4.2f,30.7f)),new View("anchor-b",new Vector3(-36.4f,-3.6f,57.6f)),
                new View("back-ramp",new Vector3(-26,-1.8f,56)),new View("vault",new Vector3(-10,0,50)),new View("vault-window",new Vector3(-1.4f,0,52)),new View("anchor-c",new Vector3(-10.6f,0,76.4f)),
                new View("ladder",new Vector3(-12,-1.4f,29)),new View("porch",new Vector3(-12,0,37)),new View("basin-door",new Vector3(22,-3.4f,27)),new View("basin-south",new Vector3(24,-5.3f,40)),
                new View("basin",new Vector3(24,-6,52)),new View("basin-north",new Vector3(24,-5.4f,63)),new View("basin-west-gate",new Vector3(12.5f,-5.6f,47.8f)),new View("basin-east-gate",new Vector3(35.3f,-5.6f,48.4f)),
                new View("overview-hall",new Vector3(2,-2.8f,12),false,2.6f),new View("overview-basin",new Vector3(24,-6,52),false,2.4f),new View("overview",new Vector3(-6,-2,40),false,5)}),
            // Chapter 4, Breathing Room. The Veil is placed along its lane (WendmireBuilder.VeilBeside: metres along, metres to starboard): its named places, then the lane every hundred
            // and fifty metres or so. Driftreed: each raft and walk as it is first walked, then two wide views.
            ("SporeVeil",new[]{new View("arrival",new Vector3(0,1,8),true),new View("beacon-1",WendmireBuilder.VeilBeside(40,-6),true),new View("pods-0",WendmireBuilder.VeilBeside(150,0),true),
                new View("pods-1",WendmireBuilder.VeilBeside(260,10),true),new View("pocket-a",WendmireBuilder.VeilBeside(300,-46),true),new View("pods-2",WendmireBuilder.VeilBeside(400,0),true),
                new View("lane-0460",WendmireBuilder.VeilBeside(460,0),true),new View("beacon-2",WendmireBuilder.VeilBeside(520,-6),true),new View("field-0",WendmireBuilder.VeilBeside(600,-14),true),
                new View("field-2",WendmireBuilder.VeilBeside(730,0),true),new View("field-4",WendmireBuilder.VeilBeside(870,-18),true),new View("beacon-3",WendmireBuilder.VeilBeside(1000,-6),true),
                new View("lane-1080",WendmireBuilder.VeilBeside(1080,0),true),new View("pocket-b",WendmireBuilder.VeilBeside(1150,48),true),new View("lane-1260",WendmireBuilder.VeilBeside(1260,0),true),
                new View("dead-beacon",WendmireBuilder.VeilBeside(1380,-8),true),new View("lane-1460",WendmireBuilder.VeilBeside(1460,0),true),new View("gate",WendmireBuilder.VeilBeside(1552,0),true),
                new View("lane-0100",WendmireBuilder.VeilBeside(100,0),true),new View("lane-0340",WendmireBuilder.VeilBeside(340,0),true),new View("lane-0660",WendmireBuilder.VeilBeside(660,20),true),
                new View("field-wide",WendmireBuilder.VeilBeside(760,0),true,3.5f),new View("gate-wide",WendmireBuilder.VeilBeside(1540,0),true,3f)}),
            ("Driftreed",new[]{new View("arrival",new Vector3(0,0,8.5f)),new View("stern-line",new Vector3(11,0,11)),new View("pad",new Vector3(-8,0,11)),new View("stage-walk",new Vector3(0,0,26)),
                new View("market",new Vector3(0,0,46)),new View("market-west",new Vector3(-10,0,44)),new View("notice",new Vector3(-6,0,40)),new View("smokehouse",new Vector3(19,0,43)),
                new View("pump-walk",new Vector3(-23,0,46)),new View("pump-raft",new Vector3(-37,0,45)),new View("press",new Vector3(-42,0,47)),new View("hall-walk",new Vector3(0,0,66)),
                new View("hall-door",new Vector3(2,0,79)),new View("hall-racks",new Vector3(10,0,87)),new View("reed-store",new Vector3(-13,0,82)),new View("school-walk",new Vector3(22,0,54.5f)),
                new View("school",new Vector3(39,0,57)),new View("gate",new Vector3(46,0,93)),new View("overview",new Vector3(5,0,52),false,3.6f),new View("overview-north",new Vector3(10,0,82),false,2.4f)}),
            // The Drowned Waystation as it is walked: the three islands, the ford, the lure posts, the bell pocket, the leech channel and the spit round it, the causeway and the portico; then
            // the station room by room (each point at its floor's height: the hall's walk .3, the terrace -2.4, the low floor -4, the great hall -1.6), and two wide views.
            ("DrownedWaystation",new[]{new View("arrival",new Vector3(-4,.55f,2.5f)),new View("first-island",new Vector3(3,.6f,19)),new View("strider",new Vector3(2,.6f,32)),new View("ford",new Vector3(16,-.3f,44)),
                new View("lure-lesson",new Vector3(28,.6f,61)),new View("lure-posts",new Vector3(26,.6f,72)),new View("bell",new Vector3(53.5f,.6f,68)),new View("leech-channel",new Vector3(10,-.3f,92)),
                new View("dry-spit",new Vector3(-14,.6f,88)),new View("third-island",new Vector3(-4,.6f,119)),new View("causeway",new Vector3(0,-.24f,147)),new View("portico",new Vector3(0,.55f,161)),
                new View("hall",new Vector3(10,.3f,178)),new View("pump-one",new Vector3(11,.3f,183)),new View("hall-floor",new Vector3(-4,-1.2f,186)),new View("stair",new Vector3(12,-1,199)),
                new View("terrace",new Vector3(14,-2.4f,222)),new View("terrace-north",new Vector3(14,-2.4f,241)),new View("low-floor",new Vector3(-8,-4,226)),new View("low-north",new Vector3(-8,-4,244)),
                new View("pim",new Vector3(-29,-2,221.5f)),new View("vault-passage",new Vector3(25,-2.4f,217.5f)),new View("vault",new Vector3(43,-2.4f,218)),new View("landing",new Vector3(0,-.6f,261.5f)),
                new View("great-hall",new Vector3(0,-1.6f,279)),new View("great-hall-north",new Vector3(0,-1.6f,295)),new View("plinth",new Vector3(-10.6f,-.85f,278.4f)),
                new View("overview-mire",new Vector3(12,0,78),false,4.2f),new View("overview-station",new Vector3(4,-2,236),false,4.2f)}),
            // Chapter 5, the Hull of Many Hands (ManyHandsBuilder.Outside): the launch and the approach over the raft bay, the four joins Hulljack is fought at (each at the play view and
            // drawn back as a fight draws it), the roof door, then each district from outside, the tug, the queue and the harbour line at its edges, and the whole ship.
            ("ManyHandsHull",new[]{new View("arrival",new Vector3(-30,1,-124),true),new View("dock",new Vector3(-40,1,-130),true),new View("drones",new Vector3(-58,1,-80),true),
                new View("approach",new Vector3(-44,1,-54),true),new View("join-1",new Vector3(-44,1,-29),true),new View("join-2",new Vector3(-44,1,29),true),new View("join-3",new Vector3(44,1,29),true),
                new View("join-4",new Vector3(44,1,-30),true),new View("door",new Vector3(44,1,-64),true),new View("crossing",new Vector3(0,1,0),true),new View("spine-north",new Vector3(0,1,90),true),
                new View("garden",new Vector3(60,1,72),true),new View("cabins",new Vector3(-62,1,80),true),new View("galley",new Vector3(86,1,-2),true),new View("bow",new Vector3(0,1,146),true),
                new View("raft-bay",new Vector3(-62,1,-64),true),new View("apron",new Vector3(48,1,-128),true),new View("stern",new Vector3(0,1,-140),true),new View("tug",new Vector3(50,1,-192),true),new View("queue",new Vector3(-128,1,20),true),
                new View("edge-east",new Vector3(138,1,10),true),new View("edge-south",new Vector3(-40,1,-203),true),new View("edge-west",new Vector3(-138,1,-40),true),new View("edge-north",new Vector3(30,1,194),true),
                new View("join-1-fight",new Vector3(-44,1,-29),true,3),new View("join-3-fight",new Vector3(44,1,29),true,3),new View("fight-wide",new Vector3(0,1,-6),true,4.2f),new View("overview",new Vector3(0,1,-10),true,8)}),
            // Chapter 5, Many Hands (ManyHandsBuilder.Hands.Views): each district as it is first walked, the talks and the freight hall, two wide views.
            ("ManyHands",ManyHandsBuilder.Hands.Views),
            // Chapter 6, the Frozen Host (HoarfellBuilder.FrozenHost.Views): the White Reach as it is walked, the dig level by level, the bottom gallery, two wide views.
            ("FrozenHost",HoarfellBuilder.FrozenHost.Views),
            // Chapter 6, the Aurora Shear and Lastlight (HoarfellBuilder.ShearZone.Views, TownZone.Views): the lane's named places and the town as it is first walked, each with wide views.
            ("AuroraShear",HoarfellBuilder.ShearZone.Views),("Lastlight",HoarfellBuilder.TownZone.Views),
            // Chapter 7, the Glass Choir (OrielBuilder.Choir.Views): the nave, the first clamp, the stays room by room, the landing and the engine hall, three wide views.
            ("GlassChoir",OrielBuilder.Choir.Views),
            // Chapter 7, the Skyfields (OrielBuilder.Skyfields.Views): each island as it is walked, the camp's lesson, a lift, the junctions, the gardens, the door, two wide views.
            ("OrielSkyfields",OrielBuilder.Skyfields.Views),
            // Chapter 7, the Orrery (OrielBuilder.Orrery.Views): the arrival and Ishen's talk, each rail's gates as a ship waits at them, the double rail, the far gate, the landing, wide views of the machine.
            ("Orrery",OrielBuilder.Orrery.Views),
            // Chapter 8, the Scoria Flows (HarrowgateBuilder.Scoria.Views): each crossing as it is read from its south bank, the delta from its islands, the battery, the camp, the gate, wide views.
            ("ScoriaFlows",HarrowgateBuilder.Scoria.Views),
            // Chapter 8, the Bridle Foundry (HarrowgateBuilder.Foundry.Views): the rooms as walked, floor by floor, the pours and the press, the two contracted rooms and the dais, wide views.
            ("BridleFoundry",HarrowgateBuilder.Foundry.Views),
            // Chapter 8, the Picket of Teeth (HarrowgateBuilder.Picket.Views): each buoy's stretch from a shadow and from the open, the carrier, the lock, the ways down, wide views.
            ("PicketOfTeeth",HarrowgateBuilder.Picket.Views),
            // Chapter 8, the War Harbour (HarrowgateBuilder.Harbour.Views): the launch, each leg of the skiff's run, the way home, a battery, a wide view.
            ("HarrowgateHarbour",HarrowgateBuilder.Harbour.Views),
            // Chapter 8, Harrowgate (HarrowgateBuilder.Town.Views): each terrace as it is first walked, the kitchen and the quarter, the muster field as the duel uses it, the cells, wide views.
            ("Harrowgate",HarrowgateBuilder.Town.Views),
            // Chapter 9, the Decks on separation day (HomeBuilder.Decks.Views): the promenade end to end, the stall, the docks, the way down, Hal's bench, the dock office and the meeting.
            ("DecksLeaving",HomeBuilder.Decks.Views),
            // Chapter 9, the Separation Works (HomeBuilder.Works.Views): each hull as it is walked, each joint from its jack, the rigs and what they drive, the control room, the pockets, the press hall, wide views.
            ("SeparationWorks",HomeBuilder.Works.Views),
            // Chapter 9, Nacre Grave (HomeBuilder.Nacre.Views): where a player arrives and stops, each pass as flown, the three tracks where they are crossed, wide views of the spiral.
            ("NacreGrave",HomeBuilder.Nacre.Views),
            // Chapter 9, the Broken Ring (HomeBuilder.Ring.Views): the arrival in the market's lee, the channel, each district as it is flown beside (build with -ring-pose stills),
            // both pairs and their cover, the formation from inside the fight, the tug, the way home, wide views.
            ("BrokenRing",HomeBuilder.Ring.Views),
            // Chapter 9, the Meridian Crawl (HomeBuilder.Crawl.Views): where a player arrives and stops, each coupling as it is crossed, the helm, the sky-garden, the tail, wide views.
            ("MeridianCrawl",HomeBuilder.Crawl.Views),
            // Chapter 9, the Terminator (HomeBuilder.Terminator.Views): the arrival, the lesson, each coupling with the dawn line in frame behind it (an editor still poses the line by each
            // view's gap), the stretches between, the gallery, the ride's end, wide views.
            ("Terminator",HomeBuilder.Terminator.Views),
            // Chapter 10, Sorrel's second sky (LastMooringBuilder.Sorrel.Views): the arrival and the canyon as flown, both fights, the apertures, each shelf's pad and two on foot, the
            // crater and the clamp, the way on, the young's road, wide views.
            ("SorrelSecondSky",LastMooringBuilder.Sorrel.Views),
            // Chapter 10, the Continuance (LastMooringBuilder.Continuance.Views): the spine foot to top, each deck as walked, the captain's line, the works' machines, the chart, the bay
            // and its mouth, the flight berth over the hull from the launch to the gantry, wide views.
            ("Continuance",LastMooringBuilder.Continuance.Views),
            // Chapter 10, Last Mooring (LastMooringBuilder.Mooring.Views): the arrival over the Halo's districts, each chain's gap, both pairs' arenas, the section and Many Hands, each
            // island from the plane and on foot, the guns' gap, the queue, the Tollkeeper's lanes and its arm, the cage, the Continuance's dock, wide views.
            ("LastMooring",LastMooringBuilder.Mooring.Views),
            // Chapter 10, Neighbourhood Afloat (LastMooringBuilder.Neighbourhood.Views): the arrival, each district as first walked, each place a player stops, the last scene, wide views.
            ("NeighbourhoodAfloat",LastMooringBuilder.Neighbourhood.Views),
            // Chapter 10, the Returning Passage (LastMooringBuilder.Passage.Views): the run as flown, each bend, the tug's place, the far end, the narrows, the mouth, wide views (build with
            // -passage-pose turned for the zone after the turn).
            ("ReturningPassage",LastMooringBuilder.Passage.Views),
            // Chapter 10, the anchor chamber (LastMooringBuilder.Chamber.Views): the dock and the gantry as the fight uses it, the run down, the well and its door, the collar, the anchor
            // drawn back as its release frames it, wide views of both rooms. Build with -chamber-released for the anchor let go.
            ("StillwaterChamber",LastMooringBuilder.Chamber.Views),
        };

        public static void Render()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-look-run")??throw new System.ArgumentException("-look-run <name> is required");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/look",run));
            if(Directory.Exists(folder))throw new System.InvalidOperationException("Preserve earlier look run: "+folder);
            Directory.CreateDirectory(folder);
            string only=DevArgs.Value("-look-zones");
            bool local=DevArgs.Has("-local-biomes");
            var before=GraphicsSettings.defaultRenderPipeline;var beforeQuality=QualitySettings.renderPipeline;
            var ground=AssetDatabase.LoadAssetAtPath<CameraProfile>("Assets/_Project/Resources/Definitions/CameraProfile/Ground.asset");
            var flight=AssetDatabase.LoadAssetAtPath<CameraProfile>("Assets/_Project/Resources/Definitions/CameraProfile/Flight.asset");
            try
            {
                if(local)
                {
                    var pipeline=Resources.Load<RenderPipelineAsset>("LocalBiomes/LocalPipeline");
                    if(pipeline==null)throw new System.InvalidOperationException("Local biome pipeline is missing");
                    GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                }
                int count=0;
                foreach(var (zone,views) in Views)
                {
                    if(!string.IsNullOrEmpty(only)&&!only.Split(',').Contains(zone))continue;
                    EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");
                    foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
                    if(local)foreach(var patch in Object.FindObjectsByType<Lattice.World.BiomePatch>(FindObjectsSortMode.None))patch.ApplyLocalArt();
                    if(DevArgs.Has("-look-preview"))ZoneLook.Preview(zone);
                    // Whoever stands in the scene stands as they will in play, not in the pose their rig was bound in: a town of people with their arms out is not what a still is for.
                    foreach(var animator in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
                    {
                        // A body held in a pose of its own (sitting on a trunk at gate six) keeps it: in play its Animator is off and Posed writes the pose.
                        if(animator.GetComponentInParent<Lattice.World.Posed>()!=null)continue;
                        var clips=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:new AnimationClip[0];
                        var idle=clips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle"));
                        if(idle!=null)idle.SampleAnimation(animator.gameObject,.3f);
                    }
                    foreach(var posed in Object.FindObjectsByType<Lattice.World.Posed>(FindObjectsSortMode.None))posed.Hold();
                    // ZoneController assigns the skybox at runtime; mirror it so editor stills match play.
                    var def=AssetDatabase.FindAssets("t:ZoneDef").Select(g=>AssetDatabase.LoadAssetAtPath<ZoneDef>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(d=>d.scene==zone||d.id==zone);
                    if(def!=null)RenderSettings.skybox=def.skybox;
                    var camera=new GameObject("Look camera",typeof(Camera),typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
                    camera.clearFlags=RenderSettings.skybox!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.03f,.06f);
                    camera.nearClipPlane=.3f;camera.farClipPlane=DevArgs.Has("-look-far")?float.Parse(DevArgs.Value("-look-far"),System.Globalization.CultureInfo.InvariantCulture):1500;
                    var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.None;
                    foreach(var view in views)
                    {
                        var heroes=Heroes(view);
                        var profile=view.flight?flight:ground;var rotation=Quaternion.Euler(profile.pitch,profile.yaw,0);
                        camera.fieldOfView=profile.fov;
                        camera.transform.SetPositionAndRotation(view.point+Vector3.up*(view.flight?0:1)-rotation*Vector3.forward*profile.distance*Mathf.Max(1,view.zoom),rotation);
                        Shot(camera,Path.Combine(folder,zone+"-"+view.name+".png"));count++;
                        foreach(var h in heroes)Object.DestroyImmediate(h);
                    }
                    Object.DestroyImmediate(camera.gameObject);
                }
                Debug.Log($"LOOKDEV_OK count={count} folder={folder}");
            }
            finally{GraphicsSettings.defaultRenderPipeline=before;QualitySettings.renderPipeline=beforeQuality;}
        });

        static GameObject[] Heroes(View view)
        {
            string[] names=view.flight?new[]{"TarenFlight","SelaFlight"}:new[]{"TarenNatural","SelaNatural"};
            var result=new GameObject[names.Length];
            for(int i=0;i<names.Length;i++)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+names[i]+".prefab");
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var at=view.point+new Vector3(i*2.2f-1.1f,0,0);
                if(!view.flight&&Physics.Raycast(at+Vector3.up*6,Vector3.down,out var hit,20,~0,QueryTriggerInteraction.Ignore))at=hit.point;
                body.transform.SetPositionAndRotation(at,Quaternion.Euler(0,160,0));
                foreach(var b in body.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;
                foreach(var animator in body.GetComponentsInChildren<Animator>())
                {
                    var clips=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:new AnimationClip[0];
                    var idle=clips.FirstOrDefault(c=>c.name.ToLowerInvariant().Contains("idle"));
                    if(idle!=null)idle.SampleAnimation(animator.gameObject,.3f);
                }
                result[i]=body;
            }
            return result;
        }

        static void Shot(Camera camera,string path)
        {
            var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;
            camera.Render();camera.Render();
            var prior=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());
            RenderTexture.active=prior;camera.targetTexture=null;
            Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
        }
    }
}
