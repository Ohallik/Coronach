using Lattice.World;
using UnityEditor;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>The occupied deck fits inside the existing generated beacon hull.
    /// Interior origin is fifteen metres aft of the approach origin.</summary>
    public static class TallowRedesign
    {
        public static void Blockout()=>BatchTools.Run(()=>{Build(true);AssetDatabase.SaveAssets();Debug.Log("TALLOW_BLOCKOUT_OK");});
        public static void Final()=>BatchTools.Run(()=>{Build();AssetDatabase.SaveAssets();Debug.Log("TALLOW_REDESIGN_OK");});
        public static void Exterior()=>BatchTools.Run(()=>{BuildExterior();AssetDatabase.SaveAssets();Debug.Log("TALLOW_EXTERIOR_OK");});
        static bool grey;
        // Interior z=-12..-7 is the five-metre pass-through lift cabin.
        // With the shared +15 m origin it occupies exterior z=3..8.
        public const float InteriorOriginZ=15,LiftFrontZ=3,LiftBackZ=8,DockZ=.5f;
        public static void Build(bool blockout=false)
        {
            grey=blockout;
            BuildExterior();BuildInterior();
        }
        static void BuildExterior()
        {
            var approach=WorldBuilder.Begin("TallowApproach");
            WorldBuilder.Spawn("Arrival",new Vector3(0,1,-25));WorldBuilder.Spawn("Dock",new Vector3(0,1,DockZ-9));
            // The antenna/arms skew the mesh bounds. The occupied circular body
            // was centred near (-3.8,10.8), not the wrapper origin (0,15).
            // Recenter and enlarge that body so the deck's corners fit its rim.
            WorldBuilder.Piece("TallowStationHull",new Vector3(4.75f,-7,20.25f),new Vector3(57.5f,12.5f,52.5f),"Rock",false);
            // A fitted service cover closes a small malformed roof patch in
            // the generated shell. Its base intersects the roof rather than
            // floating above it; the visible cover reuses generated geometry.
            var roofCover=WorldBuilder.Piece("DeckFloor",new Vector3(-6.25f,-2.7f,3),new Vector3(4.5f,.7f,4),"Rock",false);
            roofCover.name="Roof inspection cover";
            // A short pressure-transfer tower joins the flight plane to the
            // lower refuge deck. Its dock apron is physically mounted to it.
            WorldBuilder.Piece("DeckFloor",new Vector3(0,-.25f,DockZ),new Vector3(6,.5f,5));
            foreach(int side in new[]{-1,1})
            {
                WorldBuilder.Piece("DeckWall",new Vector3(side*2.5f,-3,(LiftFrontZ+LiftBackZ)*.5f),new Vector3(.45f,13,5));
                WorldBuilder.Piece("DeckWall",new Vector3(side*2,-3.25f,DockZ),new Vector3(.4f,5.5f,5));
                WorldBuilder.Piece("DeckWall",new Vector3(side*1.8f,1.75f,LiftFrontZ),new Vector3(1.4f,3.5f,.5f));
            }
            WorldBuilder.Piece("DeckWall",new Vector3(0,-3,LiftBackZ),new Vector3(5,13,.5f));
            WorldBuilder.Piece("DeckWall",new Vector3(0,-4.75f,LiftFrontZ),new Vector3(5,9.5f,.5f));
            WorldBuilder.Piece("DeckFloor",new Vector3(0,3.65f,(LiftFrontZ+LiftBackZ)*.5f),new Vector3(5,.3f,5));
            var hatch=WorldBuilder.Piece("DeckDoorway",new Vector3(0,1.6f,LiftFrontZ),new Vector3(3,3.2f,.5f),"Rock",false);
            StationSurfaces.ClosedHatch(hatch,0);
            WorldBuilder.Dock("Dock — Tallow Drift",new Vector3(0,1,DockZ),"TallowDrift","Arrival",WorldBuilder.FlightDockRange);
            WorldBuilder.Label("TALLOW DRIFT",new Vector3(0,5,12));TallowMooringStaging.ConfigureApproach();WorldBuilder.Save(approach,"TallowApproach");
        }
        static void BuildInterior()
        {
            var scene=WorldBuilder.Begin("TallowDrift");
            WorldBuilder.Spawn("Arrival",new Vector3(0,0,-8));
            var floor=Piece("DeckFloor",new Vector3(0,-.08f,0),new Vector3(25,.16f,24));
            if(!grey)StationSurfaces.FloorFinish(floor,"quiet");
            // Consistent cutaway convention: actual colliding hull walls, roof omitted.
            Wall(-12.5f,0,.6f,24);Wall(12.5f,0,.6f,24);
            if(grey)Wall(0,-12,25,.6f);
            else {Wall(-7,-12,11,.6f);Wall(7,-12,11,.6f);}
            Piece("DeckWall",new Vector3(0,1.75f,12),new Vector3(25,3.5f,.6f));
            WorldBuilder.Dock("Launch — Tallow approach",new Vector3(0,0,-10),"TallowApproach","Dock");
            foreach(int side in new[]{-1,1})
            {
                Wall(side*2.5f,-9.5f,.5f,5);
                Wall(side*10.65f,-7,3.7f,.5f);Wall(side*4.2f,-7,4,.5f);
                Door(side*7.5f,-7,2.6f);
                if(!grey)StationSurfaces.ClosedHatch(GameObject.Find("Life support hatch "+side),0);
                else Piece("DeckWall",new Vector3(side*7.5f,1.5f,-7),new Vector3(2.2f,2.7f,.16f));
                Prop("DeckConsole",side*7.5f,-10,1.5f);
            }
            Door(0,-7,4.4f);
            var keeper=WorldBuilder.Npc("Keeper",new Vector3(0,0,4));keeper.postFlag="sliceComplete";TallowMooringStaging.ConfigureKeeper(keeper);
            Prop("DeckConsole",-7,5,1.5f);
            Prop("DeckBench",-10,-1,1);Prop("DeckBench",-10,4,1);
            var repair=Prop("DeckConsole",6,4,1.5f).AddComponent<RepairBay>();
            repair.free=true;repair.CompleteSlice=true;repair.prompt="Repair, save and rest";
            Prop("DeckCrate",10,4,1);Prop("DeckCrate",10,7,1);
            // Rear service cross-aisle at z=7.5 supplies repair and life support.
            // Two private rest rooms have closed doors and genuine interior volume.
            Wall(0,10.5f,.5f,3);
            Wall(-9.9f,9,5.2f,.5f);Wall(0,9,9.4f,.5f);Wall(9.9f,9,5.2f,.5f);
            foreach(int side in new[]{-1,1})
            {
                Door(side*6,9,2.6f);
                if(!grey)
                {
                    var frame=GameObject.Find("Private rest hatch "+side);
                    StationSurfaces.ClosedHatch(frame,0);
                }
                else Piece("DeckWall",new Vector3(side*6,1.5f,9),new Vector3(2.2f,2.7f,.16f));
                Prop("DeckBench",side*6,10.7f,.75f);
                Prop("DeckCrate",side*10,10.6f,.65f);
            }
            WorldBuilder.Label("ARRIVALS / PRESSURE HATCH",new Vector3(0,.12f,-8),.9f);
            WorldBuilder.Label("REFUGE / KEEPER",new Vector3(-6,.12f,1),.9f);
            WorldBuilder.Label("REPAIR / STORES",new Vector3(7,.12f,1),.9f);
            WorldBuilder.Label("SERVICE / REST CABINS",new Vector3(0,.12f,7.5f),.9f);
            if(!grey)StationPressureEntries.Tallow();
            StationNavigationBake.Bake("TallowDrift");
            WorldBuilder.Save(scene,"TallowDrift");
        }
        static GameObject Piece(string key,Vector3 position,Vector3 size,bool solid=true)
        {
            if(!grey)return WorldBuilder.Piece(key,position,size,"Rock",solid);
            var result=ArenaBuilder.Block("BLOCKOUT "+key,position,size,key=="DeckFloor"?"Ground":key=="DeckBench"?"Taren":key=="DeckConsole"?"Sela":key=="DeckCrate"?"Enemy":"Rock");
            if(!solid)foreach(var collider in result.GetComponentsInChildren<Collider>())Object.DestroyImmediate(collider);
            return result;
        }
        static void Wall(float x,float z,float width,float depth)=>Piece("DeckWall",new Vector3(x,.55f,z),new Vector3(width,1.1f,depth));
        static void Door(float x,float z,float width)
        {
            if(!grey){var frame=Piece("DeckDoorway",new Vector3(x,1.6f,z),new Vector3(width,3.2f,.5f),false);StationSurfaces.Fit(frame,new Vector3(width,3.2f,.5f));if(z==9)frame.name="Private rest hatch "+(x<0?-1:1);else if(z==-7&&x!=0)frame.name="Life support hatch "+(x<0?-1:1);else StationSurfaces.DoorwayCollision(frame);return;}
            foreach(int side in new[]{-1,1})Piece("DeckWall",new Vector3(x+side*width*.5f,1.6f,z),new Vector3(.25f,3.2f,.5f));
            Piece("DeckWall",new Vector3(x,3.1f,z),new Vector3(width,.25f,.5f));
        }
        static GameObject Prop(string key,float x,float z,float height)=>Piece(key,new Vector3(x,height*.5f,z),new Vector3(2,height,2));
    }
}
