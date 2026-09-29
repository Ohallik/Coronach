using System;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The two practice scenes rebuilt as places (docs/maps/Arena_Ground.md,
    /// Arena_Flight.md): an outpost maintenance proving yard and a dockside ship
    /// proving berth. Isolated: no definition regeneration and no other scene.
    /// Contracts kept: the Arrival spawn, Hal's ArenaGuide dialogue, the three
    /// z=5 opponent spawns, and a clear core around the arrival and opponents
    /// (|x| &lt; 10, -18 &lt; z &lt; 10, where the old arena's rocks began) that the arena tests fly and fight across.
    /// </summary>
    public static class ArenaRedesign
    {
        static bool blockout;
        public const float CoreHalfWidth = 10, YardHalf = 20, RailX = 24, RailNorthZ = 28, RailSouthZ = -26, LaneHalf = 6, LaneEndZ = -40;
        // The graded yard's top surface (its 1 m slab is centred at y -0.7).
        const float Yard = -.2f;

        public static void GroundBlockout()=>BatchTools.Run(()=>{Ground(true);AssetDatabase.SaveAssets();Debug.Log("ARENA_GROUND_BLOCKOUT_OK");});
        public static void GroundFinal()=>BatchTools.Run(()=>{Ground(false);AssetDatabase.SaveAssets();Debug.Log("ARENA_GROUND_FINAL_OK");});
        public static void FlightBlockout()=>BatchTools.Run(()=>{Flight(true);AssetDatabase.SaveAssets();Debug.Log("ARENA_FLIGHT_BLOCKOUT_OK");});
        public static void FlightFinal()=>BatchTools.Run(()=>{Flight(false);AssetDatabase.SaveAssets();Debug.Log("ARENA_FLIGHT_FINAL_OK");});

        // ---- Outpost maintenance proving yard ------------------------------

        static void Ground(bool grey)
        {
            blockout=grey;var scene=Begin("Arena_Ground",false);
            // The graded test surface keeps the old square's size and height; the
            // outpost's worn apron continues around it so it is not a floating tray.
            var yard=ArenaBuilder.Block("Training terrain",new Vector3(0,-.7f,0),new Vector3(40,1,40),"Ground");if(!grey)WorldArt.Dress(yard,"MoonGroundA",new(40,1,40));
            var apron=ArenaBuilder.Block("Outpost apron",new Vector3(0,-.72f,-8),new Vector3(110,1,110),"Ground");if(!grey)WorldArt.Dress(apron,"MoonGroundB",new(110,1,110));
            WorldArt.Planet("Vorun",new Vector3(150,40,320),150);
            // West: the natural rock outcrop the yard was graded against.
            for(int i=0;i<9;i++)Piece("RidgeRock"+(i%3+1),new Vector3(-23-(i%2)*1.5f,Yard,-18+i*4.6f),3.2f+(i*7%3),i*37);
            // North: a rock backstop, and the obstacle/target area in front of it.
            for(int i=0;i<9;i++)Piece("RidgeRock"+((i+1)%3+1),new Vector3(-18+i*4.5f,Yard,24+(i%2)),4+(i*5%3),i*53);
            Piece("HushwellCrates",new Vector3(-10,Yard,15),1.6f,15);Piece("HushwellCrates",new Vector3(9,Yard,16),1.6f,-20);
            Piece("HushwellLowRidge",new Vector3(-1,Yard,17),1.1f,95,4.2f);Piece("HushwellRubble",new Vector3(15,Yard,12),1,30,2.6f);
            Piece("HushwellDrillBit",new Vector3(-16,Yard,11),1.1f,70,2.8f);
            // East: an installed barrier of generated deck panels on their own footings.
            for(int i=0;i<7;i++)Piece("DeckWall",new Vector3(21.2f,Yard,-11.5f+i*5),new Vector3(.6f,3.5f,5),0);
            // South, on the camera side: a low crate barrier either side of the
            // service entrance, which leads out to the landing pad.
            foreach(int side in new[]{-1,1})for(int i=0;i<6;i++)Piece("HushwellCrates",new Vector3(side*(6.5f+i*2.6f),Yard,-20.6f),1.3f,side*8+i*11);
            Piece("LandingPad",new Vector3(0,Yard-.01f,-31),new Vector3(9,.3f,9),0,0,false);
            Piece("Skiff",new Vector3(-9,Yard,-31),new Vector3(4,2,7),90,0,false);
            // South-east, beside the entrance: the operator's sheltered repair
            // space, fenced from the firing area. Hal stands at its open corner.
            Pad("Operator pad",new Vector3(15.5f,Yard+.02f,-14.5f),new Vector3(8,.16f,9));
            Piece("OutpostHab",new Vector3(16.5f,Yard,-16),4,-90);
            Piece("DeckConsole",new Vector3(12.6f,Yard,-11.4f),1.4f,-135);Piece("DeckBench",new Vector3(12.5f,Yard,-17.4f),1,90);
            for(int i=0;i<3;i++)Piece("DeckCrate",new Vector3(11,Yard,-18.6f+i*2.2f),1,i*23);
            for(int i=0;i<3;i++)Piece("DeckCrate",new Vector3(14.2f+i*2.2f,Yard,-9.8f),1,i*31);
            var guide=WorldBuilder.Npc("Hal",new Vector3(11.6f,Yard,-9.2f),"ArenaGuide");guide.repeatNode="ArenaGuide";guide.postNode="ArenaGuide";
            guide.transform.rotation=Quaternion.Euler(0,-45,0);
            Opponents(false);
            WorldBuilder.Label("PROVING YARD",new Vector3(0,.1f,-17.5f));
            // Collision follows the visible edges: rocks, panels, crates, the lane.
            WorldBuilder.Boundary("West outcrop limit",new Vector3(-21.8f,2,0),new Vector3(.8f,6,44));
            WorldBuilder.Boundary("North backstop limit",new Vector3(0,2,22.4f),new Vector3(44,6,.8f));
            WorldBuilder.Boundary("East barrier limit",new Vector3(21.8f,2,0),new Vector3(.8f,6,44));
            foreach(int side in new[]{-1,1})WorldBuilder.Boundary("South crate line",new Vector3(side*13.5f,1,-20.8f),new Vector3(15,3,.8f));
            foreach(int side in new[]{-1,1})WorldBuilder.Boundary("Service lane edge",new Vector3(side*6,1,-29),new Vector3(.8f,3,17));
            WorldBuilder.Boundary("Landing apron end",new Vector3(0,1,-37.6f),new Vector3(13,3,.8f));
            EditorSceneManager.SaveScene(scene,"Assets/_Project/Scenes/Arena_Ground.unity");
        }

        // ---- Dockside ship proving berth -----------------------------------

        static void Flight(bool grey)
        {
            blockout=grey;var scene=Begin("Arena_Flight",true);
            // Below the flight plane: the dock's lower deck, and Vorun far beneath.
            var deck=Piece("DeckFloor",new Vector3(0,-12,1),new Vector3(56,.4f,60),0,0,false);deck.name="Berth lower deck";
            WorldArt.Planet("Vorun",new Vector3(60,-420,300),210);
            // Perimeter: posts rise from the lower deck; rails at the flight plane
            // are the collision boundary. The south end leaves the entry lane open.
            for(int i=0;i<=7;i++)foreach(int side in new[]{-1,1})Post(new Vector3(side*RailX,0,RailSouthZ+i*(RailNorthZ-RailSouthZ)/7));
            for(int i=0;i<=6;i++)Post(new Vector3(-RailX+i*8,0,RailNorthZ));
            foreach(int side in new[]{-1,1})
            {
                Rail(new Vector3(side*RailX,1,(RailSouthZ+RailNorthZ)/2),new Vector3(.5f,1.2f,RailNorthZ-RailSouthZ));
                Rail(new Vector3(side*(RailX+LaneHalf)/2,1,RailSouthZ),new Vector3(RailX-LaneHalf,1.2f,.5f));
                Post(new Vector3(side*LaneHalf,0,RailSouthZ));
            }
            Rail(new Vector3(0,1,RailNorthZ),new Vector3(RailX*2,1.2f,.5f));
            // The entry lane: a railed approach to the berth mouth, marked by a lit
            // pad and closed at its outer end.
            foreach(int side in new[]{-1,1}){Rail(new Vector3(side*LaneHalf,1,(RailSouthZ+LaneEndZ)/2),new Vector3(.5f,1.2f,RailSouthZ-LaneEndZ));Post(new Vector3(side*LaneHalf,0,LaneEndZ));}
            Rail(new Vector3(0,1,LaneEndZ),new Vector3(LaneHalf*2,1.2f,.5f));
            Piece("LandingPad",new Vector3(0,-.6f,(RailSouthZ+LaneEndZ)/2),new Vector3(5,.3f,5),0,0,false).name="Lane marker pad";
            // West: the supported observation and control cabin, outside the rail.
            var cabin=Piece("DockingPodOffice",new Vector3(-RailX-9,-1,-6),new Vector3(12,6,12),90);cabin.name="Control cabin";
            for(int i=-1;i<=1;i+=2)Post(new Vector3(-RailX-9+i*3.5f,0,-6),-1);
            // Hal speaks from the cabin; the call point is a lit pad at the berth edge.
            Piece("LandingPad",new Vector3(-RailX+4,-.6f,-6),new Vector3(4,.3f,4),0,0,false).name="Control call pad";
            var hal=WorldBuilder.Npc("Hal",new Vector3(-RailX+4,1,-6),"ArenaGuide");hal.repeatNode="ArenaGuide";hal.postNode="ArenaGuide";hal.prompt="Call control — Hal";
            foreach(var renderer in hal.GetComponentsInChildren<Renderer>())renderer.enabled=false;hal.GetComponent<Collider>().enabled=false;
            Opponents(true);
            WorldBuilder.Label("PROVING BERTH",new Vector3(0,1.2f,RailSouthZ+3));WorldBuilder.Label("CONTROL",new Vector3(-RailX-9,6,-6),1.5f);
            EditorSceneManager.SaveScene(scene,"Assets/_Project/Scenes/Arena_Flight.unity");
        }
        static void Post(Vector3 foot,float top=4)
        {
            // A structural post from the lower deck to just above the rail.
            float bottom=-12;var go=Piece("DeckWall",new Vector3(foot.x,bottom,foot.z),new Vector3(1,top-bottom,1),0);go.name="Berth post";
        }
        static void Rail(Vector3 center,Vector3 size)
        {
            var go=blockout?ArenaBuilder.Block("Berth rail",center,size,"Sela"):WorldBuilder.Piece("DeckWall",center,size,"Rock");go.name="Berth rail";go.isStatic=true;
        }

        // ---- Shared ----------------------------------------------------------

        static UnityEngine.SceneManagement.Scene Begin(string name,bool flight)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("ZoneRoot",typeof(ZoneController),typeof(ArenaRuntime));
            root.GetComponent<ZoneController>().definition=GameCatalog.Find<ZoneDef>(name)??throw new InvalidOperationException("Zone missing: "+name);
            new GameObject("Arrival",typeof(SpawnPoint)).transform.position=new Vector3(0,flight?1:0,-4);
            var light=new GameObject("Sun",typeof(Light));light.transform.rotation=Quaternion.Euler(48,-30,0);light.GetComponent<Light>().type=LightType.Directional;light.GetComponent<Light>().intensity=1.5f;light.GetComponent<Light>().shadows=LightShadows.Soft;
            RenderSettings.ambientLight=new Color(.35f,.39f,.5f);RenderSettings.ambientMode=AmbientMode.Flat;
            var volume=new GameObject("Look",typeof(Volume)).GetComponent<Volume>();volume.isGlobal=true;
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/"+name+"Volume.asset")??throw new InvalidOperationException("Volume missing: "+name);
            volume.sharedProfile=profile;return scene;
        }
        static void Opponents(bool flight)
        {
            for(int i=0;i<3;i++)
            {
                var spawn=new GameObject("EnemySpawn_"+i,typeof(Spawner));spawn.transform.position=new Vector3((i-1)*5,flight?1:0,5);
                var s=spawn.GetComponent<Spawner>();s.definition=GameCatalog.Find<EnemyDef>(flight?"ChoristerDart":"Ridgehound");s.count=1;s.radius=0;
            }
        }
        static void Pad(string label,Vector3 center,Vector3 size)
        {
            var go=blockout?ArenaBuilder.Block(label,center-Vector3.up*.08f,size,"Taren"):WorldBuilder.Piece("DeckFloor",center-Vector3.up*.08f,size,"Ground");
            go.name=label;if(!blockout)StationSurfaces.FloorFinish(go,"work");
        }
        static GameObject Piece(string key,Vector3 foot,float height,float yaw,float footprint=0,bool solid=true)=>
            Piece(key,foot,footprint>0?new Vector3(footprint,height,footprint):Vector3.one*height,yaw,footprint,solid);
        /// <summary>Place a piece standing on <paramref name="foot"/> (its base, not its centre).</summary>
        static GameObject Piece(string key,Vector3 foot,Vector3 size,float yaw,float footprint=0,bool solid=true)
        {
            GameObject go;
            if(blockout)go=ArenaBuilder.Block(key+" blockout",Vector3.zero,size,key.StartsWith("Deck")||key.Contains("Pad")||key.Contains("Pod")?"Sela":key.StartsWith("Hushwell")?"Taren":"Rock");
            else
            {
                var fit=size;
                if(footprint>0)
                {
                    var probe=WorldBuilder.Piece(key,Vector3.zero,Vector3.one,"Rock",false);var b=ModelGeometry.BoundsOf(probe);UnityEngine.Object.DestroyImmediate(probe);
                    fit=Vector3.one*(footprint*b.size.y/Mathf.Max(b.size.x,b.size.z));
                }
                go=WorldBuilder.Piece(key,Vector3.zero,fit,"Rock",solid);
            }
            go.transform.rotation=Quaternion.Euler(0,yaw,0);var bounds=ModelGeometry.BoundsOf(go);
            go.transform.position+=foot-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            if(!solid)foreach(var c in go.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
            go.isStatic=solid;return go;
        }
    }
}
