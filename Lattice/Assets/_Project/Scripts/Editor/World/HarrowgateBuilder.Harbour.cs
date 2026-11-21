using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>The War Harbour (space combat: the pursuit, part two of the Warmaster's fight), built to the duel engineer's contract (docs/maps/Harrowgate.md, "Pursuit contract"): the
    /// skiff's run in four legs, "under Auntie, round the flak mole, up the slip"; cranes whose legs are cover; four batteries that answer his calls; the door that is the checkpoint; the
    /// way he goes down; the way back to the field. Its places are <see cref="Harbour"/>'s constants.</summary>
    public static partial class HarrowgateBuilder
    {
        internal const string HarbourZone="HarrowgateHarbour";
        internal static class Harbour
        {
            /// <summary>Where the party lifts in from the forecourt, and the four legs of his run (about 50 m apart, open water 30 m round each but for the cranes' legs).</summary>
            public static readonly Vector3 Launch=new(0,1,-4);
            public static readonly Vector3[] Legs={new(0,1,30),new(20,1,80),new(-6,1,130),new(12,1,180)};
            /// <summary>The batteries his calls name, 1 to 4: two on the flak mole, two on the shore.</summary>
            public static readonly Vector3[] Batteries={new(-44,1,122),new(-26,1,136),new(58,1,62),new(58,1,150)};
            /// <summary>Each crane by its legs' corners (x, z of the middle, half its span across and along) and how tall it stands over the plane. Auntie straddles leg 1.</summary>
            public static readonly (string name,Vector2 at,float halfX,float halfZ,float tall)[] Cranes={("Auntie",new(20,80),9,6,16),("East crane",new(34,124),5,4,11),("Slip crane",new(-14,172),5,4,11)};
            public static readonly Vector3 HomeWay=new(-14,1,-8),Exit=new(-52,1,22);
            public const float XMin=-50,XMax=72,ZMin=-30,ZMax=212,Water=-7;
            public static readonly LookDev.View[] Views=
            {
                new("launch",Launch,true),new("leg-0",Legs[0],true),new("auntie",Legs[1],true),new("mole",Legs[2],true),new("slip",Legs[3],true),new("home-way",HomeWay,true),
                new("battery-shore",new Vector3(46,1,62),true),new("wide",new Vector3(6,1,90),true,4),
                new("east-shore",new Vector3(56,1,100),true),new("wall",new Vector3(-46,1,64),true),new("carrier",new Vector3(14,1,204),true),new("quay-wide",new Vector3(-4,1,4),true,2.5f),
            };
        }

        static void BuildHarbour()
        {
            var kit=ZoneKit.Flight(HarbourZone);
            var scene=WorldBuilder.Begin(kit.zone);
            // The sun goes down behind the town, in the west-south-west (HarbourLooks has its height and colour).
            var key=GameObject.Find("KeyLight");if(key!=null)key.transform.rotation=Quaternion.Euler(HarbourSunPitch,HarbourSunYaw,0);
            WorldBuilder.Spawn("Launch",Harbour.Launch);WorldBuilder.Spawn("Arrival",Harbour.Launch+new Vector3(4,0,-3));
            kit.DriftingSky();
            RockMaterials();HarbourMaterials(kit);
            var root=new GameObject("The war harbour");

            // ---- the place, all of it drawn only and under the plane but the cranes (HarrowgateBuilder.HarbourLook): the town's stone (the wall below the field, the quay under the
            // launch, the flak mole, the piers, the gun pits, the slip, the cranes' rail piers), the east shore, the breakwater, the town over the wall, what floats, the carrier and
            // the lamps; then the water, which foams at every one of them.
            var dry=new Dry();
            HarbourStone(kit,root.transform,dry);EastShore(kit,root.transform,dry);Breakwater(kit,root.transform,dry);HarbourTown(kit,root.transform);
            Moored(kit,root.transform,dry);Carrier(kit,root.transform,dry);HarbourLamps(kit,root.transform);
            HarbourSurface(kit,root.transform,dry);
            // Ash in the air: the Reach's, fine and grey, so the air over the water is a volcanic world's.
            var ash=kit.Drift("Ash on the air",new Vector3(11,0,92),new Vector3(150,8,270),kit.Glow("Ash",false,new Color(.7f,.68f,.68f,1)),new Color(.6f,.58f,.58f,.3f),900,.05f,.15f,.25f,20);ash.transform.SetParent(root.transform,true);

            // ---- the run, the cranes (their legs the cover), the batteries
            for(int i=0;i<Harbour.Legs.Length;i++){var leg=new GameObject("Skiff's run "+i,typeof(PursuitLeg));leg.transform.position=Harbour.Legs[i];leg.GetComponent<PursuitLeg>().index=i;leg.transform.SetParent(root.transform,true);}
            foreach(var (name,at,hx,hz,tall) in Harbour.Cranes)Crane(kit,root.transform,name,at,hx,hz,tall);
            for(int i=0;i<Harbour.Batteries.Length;i++)
            {
                var at=Harbour.Batteries[i];var battery=new GameObject("Battery "+(i+1),typeof(HarbourBattery));battery.transform.position=at;battery.GetComponent<HarbourBattery>().index=i+1;battery.transform.SetParent(root.transform,true);
                var gun=DrawnEnemy("MusterMortar",at+Vector3.down*3.2f,3.2f,200);if(gun!=null){gun.transform.SetParent(battery.transform,true);gun.name="Battery "+(i+1)+" guns";}
            }

            // ---- the door: the skiff at leg 0, facing the party's way in; the checkpoint; where he goes down
            var fight=kit.FlightFight("DraskSkiff",Harbour.Legs[0],new Vector3(60,8,60),(Foe("DraskSkiff","ReachSkiff"),1));
            var post=fight.spawners[0];post.transform.position=Harbour.Legs[0];post.transform.rotation=Quaternion.Euler(0,180,0);post.faceAsPlaced=true;post.radius=0;
            fight.checkpointSpawn="Launch";
            var exit=new GameObject("The skiff goes down on the field",typeof(BossExit)).GetComponent<BossExit>();exit.transform.position=Harbour.Exit;exit.boss="DraskSkiff";exit.sinks=true;

            // ---- the way back to the field, and the harbour's line
            var home=WorldBuilder.Warp("Land — Harrowgate",Harbour.HomeWay,TownZone,"bossdown.DraskSkiff","Verdict");home.lockedPrompt="Stop the Warmaster's skiff first";home.range=WorldBuilder.FlightDockRange;
            WorldBuilder.Label("HARROWGATE",Harbour.HomeWay+new Vector3(0,-1,-7),4);
            HarbourLine(kit,root.transform);
            // Every lamp's light on the water, now that every lamp stands (the cranes' arc lamps and the harbour line's among them).
            HarbourGlints(kit,root.transform,dry);
            kit.FlightLimits(Harbour.XMin,Harbour.XMax,Harbour.ZMin,Harbour.ZMax);
            WorldBuilder.Save(scene,kit.zone);
            Debug.Log("HARROWGATE_COORDS HarrowgateHarbour launch="+Harbour.Launch+" legs="+string.Join(" ",Harbour.Legs.Select(l=>l.ToString()))+" batteries="+string.Join(" ",Harbour.Batteries.Select(b=>b.ToString()))
                +" home="+Harbour.HomeWay+" exit="+Harbour.Exit);
            Debug.Log("HARROWGATE_HARBOUR_OK");
        }
        const float HarbourSunPitch=19,HarbourSunYaw=74;
        [ZoneLook.Source] static void HarbourLooks()
        {
            // Dusk, committed: the pursuit is flown as the sun goes down behind the town. It comes in low and amber from the west-south-west, so the sea wall and the houses on its
            // walk lay long shadows out over the western water, every face turned west (the cranes' legs, the hulls' flanks, the mole's head) holds the last of it, and the tops of
            // things go to the evening's violet fill; the town's lamps and the Muster's arc lamps are lit, and their light on the water is the brightest thing on it. (A late afternoon
            // from the south-east, the first look, was grey-green and flat from end to end: harrow-look2-04.)
            // A sun redder than this turned every white thing on the carrier's deck orange (a danger's colour on a floor, D217), and a rose fill and haze turned the water mauve
            // (harbour-dusk-01): the sun is gold, the fill and the haze the blue of the sky behind the town.
            ZoneLook.Register(HarbourZone,new ZoneLook.Spec{sun=C(1f,.8f,.6f),sunIntensity=1.5f,sunPitch=HarbourSunPitch,shadowStrength=.86f,
                sky=C(.28f,.35f,.52f),equator=C(.42f,.4f,.4f),ground=C(.12f,.12f,.14f),
                fog=true,fogColor=C(.3f,.35f,.46f),fogStart=100,fogEnd=360,
                contrast=14,saturation=4,splitShadows=C(.36f,.44f,.6f),splitHighlights=C(.6f,.54f,.44f),
                bloomThreshold=.85f,bloomIntensity=.8f,vignette=.24f});
        }
        /// <summary>A gantry crane: legs from its rail piers to well over the plane, each solid at the plane (a lance run stops at it: the cover), lattice girders across their tops, and the
        /// jib north, so nothing drawn above the plane stands between the camera and a leg's water. What it is drawn as is <see cref="CraneDrawn"/>: each leg a lattice column 1.4 m
        /// square inside its 1.6 m box, so the box is the shape a lance run meets (D272).</summary>
        static void Crane(ZoneKit kit,Transform parent,string name,Vector2 at,float hx,float hz,float tall)
        {
            var holder=new GameObject(name);holder.transform.SetParent(parent,true);
            float top=ZoneKit.FlightPlane+tall;
            foreach(var (sx,sz) in new[]{(-1,-1),(1,-1),(-1,1),(1,1)})
            {
                var foot=new Vector3(at.x+sx*hx,Harbour.Water,at.y+sz*hz);
                var leg=new GameObject(name+" leg",typeof(BoxCollider));leg.transform.SetParent(holder.transform,true);leg.isStatic=true;
                leg.transform.position=new Vector3(foot.x,(Harbour.Water+ZoneKit.FlightPlane+4)*.5f,foot.z);leg.GetComponent<BoxCollider>().size=new Vector3(1.6f,ZoneKit.FlightPlane+4-Harbour.Water,1.6f);
            }
            CraneDrawn(kit,holder.transform,name,at,hx,hz,top);
        }
        /// <summary>The harbour's line: lamp buoys round the run's water with a lit line between them at the ships' height, so the edge is seen where it stops a hull.</summary>
        static void HarbourLine(ZoneKit kit,Transform parent)
        {
            var glow=kit.Glow("Harbour line",false,new Color(1.4f,1.1f,.7f,1));var line=new GameObject("Harbour line");line.transform.SetParent(parent,true);
            var corners=new[]{new Vector3(Harbour.XMin,1,Harbour.ZMin),new Vector3(Harbour.XMax,1,Harbour.ZMin),new Vector3(Harbour.XMax,1,Harbour.ZMax),new Vector3(Harbour.XMin,1,Harbour.ZMax)};
            var floats=new ZoneKit.Shape();var masts=new ZoneKit.Shape();var heads=new ZoneKit.Shape();var halos=new GameObject("Harbour line lamps");halos.transform.SetParent(line.transform,true);
            for(int c=0;c<4;c++)
            {
                var a=corners[c];var b=corners[(c+1)%4];
                ZoneKit.Strand(line.transform,a,b,glow,.35f,new Color(1,.8f,.5f,.6f),new Color(1,.8f,.5f,.6f));
                int n=Mathf.Max(1,Mathf.RoundToInt(Vector3.Distance(a,b)/16));
                for(int k=0;k<n;k++)
                {
                    // A lamp buoy: its float banded bone and iron on the water, a slim mast to the ships' height, its lamp where the lit line runs (the first were pale posts a metre thick).
                    // Where the line crosses the carrier's deck its lamps stand on the deck, on short posts.
                    var p=Vector3.Lerp(a,b,k/(float)n);bool deck=OnTheCarrier(new Vector2(p.x,p.z));var water=new Vector3(p.x,deck?CarrierDeck-.8f:Harbour.Water,p.z);
                    if(!deck)floats.Lump(water+Vector3.up*.25f,new Vector3(.95f,.7f,.95f),k+c*40,.05f,new Color(.82f,.78f,.68f),new Color(.24f,.24f,.26f),10,6);
                    masts.Post(water+Vector3.up*.8f,ZoneKit.FlightPlane-.25f-(water.y+.8f),.13f,.09f,6,new Color(.22f,.22f,.24f),new Color(.3f,.3f,.32f),.5f);
                    heads.Lump(new Vector3(p.x,ZoneKit.FlightPlane-.05f,p.z),new Vector3(.24f,.3f,.24f),k+c*40,.05f,Color.white,Color.white,8,5);
                    var halo=GameObject.CreatePrimitive(PrimitiveType.Quad);halo.name="Harbour line lamp";Object.DestroyImmediate(halo.GetComponent<Collider>());halo.transform.SetParent(halos.transform,false);
                    halo.transform.position=new Vector3(p.x,ZoneKit.FlightPlane,p.z);halo.transform.rotation=Quaternion.Euler(ZoneKit.FlightPitch,0,0);halo.transform.localScale=Vector3.one*1.8f;
                    var view=halo.GetComponent<Renderer>();view.sharedMaterial=HGlow;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
                    if(!deck)Reflections.Add((new Vector3(p.x,ZoneKit.FlightPlane-.05f,p.z),new Color(1,.84f,.56f),.8f,.6f,0));
                }
            }
            kit.Built("Harbour line buoys",floats,HHull).transform.SetParent(parent,true);kit.Built("Harbour line masts",masts,HIron).transform.SetParent(parent,true);
            kit.Built("Harbour line lamps",heads,HLamp).transform.SetParent(parent,true);
        }
    }
}
