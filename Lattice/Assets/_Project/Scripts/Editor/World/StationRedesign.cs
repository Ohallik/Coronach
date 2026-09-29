using System;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace Lattice.EditorTools
{
    /// <summary>One dimensional plan for the occupied vessels, their interior,
    /// docks, separation bridges and ring. Rebuilds only the two Cinder scenes.</summary>
    public static class StationRedesign
    {
        public static readonly float[] HullX = { -28, 0, 28 };
        public static readonly string[] DockIds = { "Office", "Shop", "Repair" };
        public const float Front = -16, Back = 24, HalfWidth = 12;
        public const float PublicZ = -3, ServiceZ = 21.5f, DockZ = -22;
        static bool blockout;

        public static void Blockout() => BatchTools.Run(() => Build(true));
        public static void Final() => BatchTools.Run(() => Build(false));
        /// <summary>Rebuild only the flight exterior; the walkable Decks and its navigation stay untouched.</summary>
        public static void Exterior() => BatchTools.Run(() =>
        {
            foreach(string key in new[]{"DeckFloor","DeckWall","DeckDoorway","LandingPad"})
                if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/"+key+".prefab")==null)throw new InvalidOperationException("Missing generated station piece "+key);
            BuildExterior(false);AssetDatabase.SaveAssets();Debug.Log("STATION_EXTERIOR_OK");
        });
        static void Build(bool grey)
        {
            blockout=grey;
            if(!grey) foreach(string key in new[]{"DeckFloor","DeckWall","DeckDoorway","DeckConsole","DeckBench","DeckCrate","LandingPad","LatticeAnvil"})
                if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/"+key+".prefab")==null)throw new InvalidOperationException("Missing generated station piece "+key);
            BuildDecks(grey);BuildExterior(grey);AssetDatabase.SaveAssets();
            Debug.Log(grey?"STATION_BLOCKOUT_OK":"STATION_REDESIGN_OK");
        }
        public static void BuildDecks(bool grey=false)
        {
            blockout=grey;
            var scene=WorldBuilder.Begin("Hub_Decks");
            foreach(float x in HullX) Hull(x,false);
            Bridges(false);
            // Public and service cross-passages stay clear of every furnishing.
            foreach(float x in HullX)
            {
                SplitWall(x, -7, 24, 4, 1.1f);
                SplitWall(x, 10, 16, 4, 1.1f);
                SplitWall(x, 19, 16, 4, 1.1f);
                Door(x,-7); Door(x,10); Door(x,19);
            }
            for(int i=0;i<3;i++)
            {
                float x=HullX[i];string id=DockIds[i];
                WorldBuilder.Spawn(id,new Vector3(x,0,-11));
                WorldBuilder.Dock("Launch — Cinder Halo",new Vector3(x,0,-14),"Hub_CinderHalo",id);
            }
            WorldBuilder.Spawn("Arrival",new Vector3(-28,0,-11));
            WorldBuilder.Npc("Orrin",new Vector3(-34,0,5));
            var mira=WorldBuilder.Npc("Mira",new Vector3(0,0,4));mira.postFlag="quest.MiraCrystals.complete";
            WorldBuilder.Npc("Hal",new Vector3(34,0,4));
            var sela=WorldBuilder.Npc("Sela",new Vector3(-23,0,-1),"SelaWaiting");sela.hideWhenJoined="Sela";sela.repeatNode="SelaWaiting";sela.postNode="";
            // Orrin's desk faces arrivals. Waiting benches are in alcoves, not across the promenade.
            Prop("DeckConsole",-34,7,1.4f);Prop("DeckBench",-22,3,1.1f);Prop("DeckBench",-22,7,1.1f);
            // Mira's front sales counters and stock entered from the service side.
            Prop("DeckConsole",-6,5,1.4f);Prop("DeckConsole",6,5,1.4f);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++)Prop("DeckCrate",side*(5+i*1.4f),8,.85f);
            // Workshop has an open central repair stand and separate parts stores.
            var bench=Prop("LatticeAnvil",32,6,1.6f).AddComponent<Bench>();bench.prompt="Fabricate at Hal's bench";
            var repair=Prop("DeckConsole",24,6,1.6f).AddComponent<RepairBay>();repair.prompt="Repair & save — 8 scrip per Sync level";
            for(int i=0;i<3;i++)Prop("DeckCrate",33+i*1.3f,-10,1.1f);
            foreach(float x in new[]{21f,35f})foreach(float z in new[]{12f,16f})Prop("DeckCrate",x,z,1.3f);
            // Quiet cabin doors imply rooms inside the pressure envelope. The center aisle
            // connects housing to the galley and service gallery without crossing a cabin.
            for(int side=-1;side<=1;side+=2)
            {
                float x=-28+side*5;
                // Split the cabin frontage at actual hatch openings; a wall
                // continuing across the panel looked like a barricaded door.
                Wall(x,10.475f,1,.95f,1.1f);Wall(x,14.5f,1,1.9f,1.1f);Wall(x,18.525f,1,.95f,1.1f);
                Wall(-28+side*8.5f,14.5f,7,.4f,1.1f);
                foreach(float z in new[]{12.25f,16.75f})
                {
                    var door=Piece("DeckDoorway",new Vector3(x,1.6f,z),new Vector3(3.2f,3.2f,.5f),false);
                    door.transform.rotation=Quaternion.Euler(0,90,0);
                    if(!grey)StationSurfaces.ClosedHatch(door,90);
                    else {var panel=Piece("DeckWall",new Vector3(x,1.5f,z),new Vector3(.16f,2.7f,2.2f));panel.name="Closed cabin hatch blockout";}
                    // Closed private rooms have actual volumes behind their frontages.
                    Prop("DeckBench",-28+side*8.5f,z,1);Prop("DeckCrate",-28+side*10,z+1.25f,.8f);
                }
            }
            // Shared galley: paired seats, central tables and a provision counter.
            for(int side=-1;side<=1;side+=2)
            {
                Prop("DeckBench",side*5,12,1).transform.rotation=Quaternion.Euler(0,180,0);
                Prop("DeckBench",side*5,17,1);
                var table=Prop("DeckCrate",side*5,14.5f,.75f);
                if(!grey)StationSurfaces.Fit(table,new Vector3(2.4f,.75f,1.4f));
            }
            Prop("DeckConsole",8,17,1.5f);
            WorldBuilder.Label("ARRIVALS / DOCK OFFICE",new Vector3(-28,.12f,-2),1.2f);
            WorldBuilder.Label("MARKET / GALLEY",new Vector3(0,.12f,-2),1.2f);
            WorldBuilder.Label("RECEIVING / REPAIR",new Vector3(28,.12f,-2),1.2f);
            WorldBuilder.Label("CABINS 01–04",new Vector3(-28,.12f,16),1);
            WorldBuilder.Label("SERVICE / STORES",new Vector3(16,.12f,22),1);
            if(!grey)StationPressureEntries.Cinder();
            StationNavigationBake.Bake("Hub_Decks");
            WorldBuilder.Save(scene,"Hub_Decks");
        }
        public static void BuildExterior(bool grey=false)
        {
            blockout=grey;
            var scene=WorldBuilder.Begin("Hub_CinderHalo");
            WorldArt.Planet("Vorun",new Vector3(150,-410,460),155);WorldArt.Planet("Sorrel",new Vector3(-85,-120,160),35);
            foreach(float x in HullX) Hull(x,true);
            Bridges(true);
            Ring();
            for(int i=0;i<3;i++)
            {
                float x=HullX[i];string id=DockIds[i];
                WorldBuilder.Dock("Dock — "+(i==0?"Dock Office":i==1?"Outfitter":"Repair Bay"),new Vector3(x,1,DockZ),"Hub_Decks",id);
                WorldBuilder.Spawn(id,new Vector3(x,1,DockZ-9));
                WorldBuilder.Label(i==0?"ARRIVALS":i==1?"MARKET":"RECEIVING",new Vector3(x,5,-14),1.3f);
            }
            WorldBuilder.Spawn("Arrival",new Vector3(-28,1,-34));WorldBuilder.Spawn("Outer",new Vector3(0,1,140));WorldBuilder.Spawn("Moon",new Vector3(53,1,8));
            WorldBuilder.Warp("Sorrel approach",new Vector3(65,1,8),"Sorrel_Ridges","met.Orrin");
            WorldBuilder.Warp("Outer warp — The Gullet",new Vector3(0,1,148),"Gullet_Tunnel");
            var neve=WorldBuilder.Npc("Neve",new Vector3(-5,1,-35));foreach(var renderer in neve.GetComponentsInChildren<Renderer>())renderer.enabled=false;neve.GetComponent<Collider>().enabled=false;
            WorldBuilder.Piece("Skiff",neve.transform.position-Vector3.up,new Vector3(4,1.5f,6),"Sela",false);
            WorldBuilder.Label("Neve",neve.transform.position+Vector3.up*3,1.5f);
            var hail=neve.gameObject.AddComponent<HailPoint>();hail.contact=neve;hail.prompt="Hail Neve";hail.range=7;neve.range=0;
            // Two more crews hail from their own ships, so the wider response is
            // visible before Meret appears: the Compact is charting which routes
            // will hold, and a household is packed for the Nacre transfer.
            Resident("Oda","Hauler",new Vector3(17,1,-45),new Vector3(4,1.5f,8),200);
            Resident("Ilo","PatrolCutter",new Vector3(50,1,-9),new Vector3(4,1.5f,5),250);
            for(int laneIndex=0;laneIndex<4;laneIndex++)
            {
                var root=new GameObject("Outer freight lane "+laneIndex,typeof(SplineContainer),typeof(TrafficLane));var container=root.GetComponent<SplineContainer>();var spline=new Spline();
                // Hull corners extend almost 99 m from the ring center. Traffic
                // must clear those vessels, not cross their occupied roofs.
                for(int i=0;i<24;i++){float a=i*Mathf.PI/12;float r=112+laneIndex*10;spline.Add(new BezierKnot(new float3(Mathf.Sin(a)*r,1+laneIndex*2,70+Mathf.Cos(a)*r)));}
                spline.Closed=true;spline.SetTangentMode(TangentMode.AutoSmooth);container.Spline=spline;
                var lane=root.GetComponent<TrafficLane>();lane.speed=4+laneIndex;
                for(int j=0;j<2;j++){var ship=WorldBuilder.Piece(j==0?"Hauler":"PatrolCutter",Vector3.zero,new Vector3(4,1.5f,j==0?8:5),"Rock",false);ship.isStatic=false;var mover=ship.AddComponent<TrafficShip>();mover.lane=lane;mover.phase=j*.5f+laneIndex*.2f;ship.transform.SetPositionAndRotation(lane.Position(mover.phase),Quaternion.LookRotation(lane.Tangent(mover.phase)));}
            }
            WorldBuilder.Save(scene,"Hub_CinderHalo");
        }
        static void Resident(string id,string ship,Vector3 at,Vector3 size,float yaw)
        {
            // They speak from their ships: no standing body, and nothing that collides
            // (a placeholder capsule would be an invisible obstacle in the lane).
            var npc=WorldBuilder.Npc(id,at);var placeholder=npc.transform.Find("ART_PENDING_Civilian");if(placeholder!=null)UnityEngine.Object.DestroyImmediate(placeholder.gameObject);
            foreach(var renderer in npc.GetComponentsInChildren<Renderer>())renderer.enabled=false;npc.GetComponent<Collider>().enabled=false;
            var hull=WorldBuilder.Piece(ship,at-Vector3.up,size,"Sela",false);hull.transform.rotation=Quaternion.Euler(0,yaw,0);hull.name=id+"'s ship";
            WorldBuilder.Label(id,at+Vector3.up*3,1.5f);
            var hail=npc.gameObject.AddComponent<HailPoint>();hail.contact=npc;hail.prompt="Hail "+id;hail.range=7;npc.range=0;
        }
        // A continuous load-bearing box ring built from the hulls' own deck and wall
        // modules at near-native proportions: a deck plate over inner and outer 3.5 m
        // fascias. The former single wall panel was stretched tenfold through the 5 m
        // section, smearing its texture and exposing its ragged edge all the way round.
        static void Ring()
        {
            const int segments=96;const float radius=62,width=5,top=-3.5f,plate=.5f,fascia=3.5f,skin=.55f;
            var centre=new Vector3(0,0,70);float half=Mathf.Tan(Mathf.PI/segments);
            for(int i=0;i<segments;i++)
            {
                float angle=i*2*Mathf.PI/segments;var rotation=Quaternion.Euler(0,angle*Mathf.Rad2Deg,0);
                var radial=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));var mid=centre+radial*radius;
                // Plates meet at the outer polygon vertex. Alternate plates sit 2 cm
                // proud so the unavoidable inner overlap never shares a plane.
                var deck=Piece("DeckFloor",mid+Vector3.up*(top-plate*.5f+(i%2)*.02f),new Vector3(2*(radius+width*.5f)*half+.08f,plate,width));
                deck.name="Ring deck plate "+i;deck.transform.rotation=rotation;
                foreach(int side in new[]{-1,1})
                {
                    // Each fascia's exposed face meets its neighbour at the vertex.
                    float face=radius+side*width*.5f;
                    var wall=Piece("DeckWall",mid+radial*side*(width*.5f-skin*.5f)+Vector3.up*(top-plate-fascia*.5f),new Vector3(2*face*half+.06f,fascia,skin));
                    wall.name=(side<0?"Ring inner fascia ":"Ring outer fascia ")+i;wall.transform.rotation=rotation;
                }
            }
        }
        static void Hull(float x,bool exterior)
        {
            // Different floor finishes follow actual rooms and circulation.
            Floor(x,-11.5f,24,9,"arrival");Floor(x,-3.5f,24,7,"public");
            Floor(x,5,24,10,x==28?"work":"rooms");
            Floor(x,14.5f,24,9,x==28?"work":"quiet");Floor(x,21.5f,24,5,"service");
            // Side bulkheads have openings only at the two enclosed bridge levels.
            foreach(int side in new[]{-1,1})
            {
                float edge=x+side*12;
                Wall(edge,-11, .55f,10,exterior?3.5f:1.1f);
                Wall(edge,9.5f,.55f,19,exterior?3.5f:1.1f);
                // Exterior end hulls need closed cross-passage edges.
                if(edge==-40||edge==40){Wall(edge,-3,.55f,6,exterior?3.5f:1.1f);Wall(edge,21.5f,.55f,5,exterior?3.5f:1.1f);}
            }
            Wall(x,Back,24,.6f,3.5f);SplitWall(x,Front,24,4,exterior?3.5f:1.1f);Door(x,Front);
            // The keel and transverse beams visibly carry the deck, including its corners.
            for(int i=0;i<5;i++) Piece("DeckWall",new Vector3(x,-2,Front+4+i*8),new Vector3(24,2,.8f));
            foreach(float side in new[]{-8f,8f}) Piece("DeckWall",new Vector3(x+side,-2.5f,4),new Vector3(1,3,40));
            // Close the utility keel. Bare hanging beams read as legs; the lower
            // pressure casing also makes the contact with the ring continuous.
            Piece("DeckFloor",new Vector3(x,-3.5f,4),new Vector3(24,.8f,40));
            // The casing reaches the underside of the walking deck. The old
            // top at -0.5 left a visible open seam below the thin floor.
            foreach(int side in new[]{-1,1})Piece("DeckWall",new Vector3(x+side*12,-1.75f,4),new Vector3(.55f,3.5f,40));
            foreach(float end in new[]{Front,Back})Piece("DeckWall",new Vector3(x,-1.75f,end),new Vector3(24,3.5f,.55f));
            if(exterior) Piece("DeckFloor",new Vector3(x,3.65f,4),new Vector3(24,.5f,40));
            // The short dock neck is enclosed to the vestibule, not a floating launch disc.
            Floor(x,-18,4,4);Wall(x-2,-18,.5f,4,exterior?3.5f:1.1f);Wall(x+2,-18,.5f,4,exterior?3.5f:1.1f);
            // Closed outer pressure hatch; launch is an interaction at the dock.
            // It must not be an unbounded walk off the end of the floor.
            if(exterior||blockout)Wall(x,-20,4,.5f,exterior?3.5f:1.1f);
            if(exterior)
            {
                Piece("DeckFloor",new Vector3(x,3.65f,-18),new Vector3(4,.5f,4));
                // The exposed docking apron meets a visible closed pressure hatch.
                // It sits outside the neck, so its pad is not buried in the hull.
                Floor(x,-22,7,4,"arrival");
                if(!blockout)
                {
                    var hatch=Piece("DeckDoorway",new Vector3(x,1.6f,-20.3f),new Vector3(4,3.2f,.6f),false);
                    StationSurfaces.ClosedHatch(hatch,0);
                    // Enclosed roof plant volume, connected to the utility keel;
                    // varied crowns make the converted vessels legible outside.
                    float width=x==-28?10:x==0?12:7,depth=x==28?16:10;
                    foreach(int side in new[]{-1,1})
                    {
                        Piece("DeckWall",new Vector3(x+side*width*.5f,4.65f,14),new Vector3(.3f,1.5f,depth));
                        Piece("DeckWall",new Vector3(x,4.65f,14+side*depth*.5f),new Vector3(width,1.5f,.3f));
                    }
                    Piece("DeckFloor",new Vector3(x,5.5f,14),new Vector3(width,.2f,depth));
                }
            }
        }
        static void Bridges(bool exterior)
        {
            foreach(float x in new[]{-14f,14f})foreach(var passage in new[]{(z:-3f,width:6f),(z:21.5f,width:5f)})
            {
                Floor(x,passage.z,4,passage.width);
                foreach(int side in new[]{-1,1})Wall(x,passage.z+side*passage.width*.5f,4,.5f,exterior?3.5f:1.1f);
                Piece("DeckWall",new Vector3(x,-1.5f,passage.z),new Vector3(4,3,passage.width));
                if(exterior)Piece("DeckFloor",new Vector3(x,3.65f,passage.z),new Vector3(4,.5f,passage.width));
            }
        }
        static GameObject Piece(string key,Vector3 position,Vector3 size,bool solid=true)
        {
            if(!blockout)return WorldBuilder.Piece(key,position,size,"Rock",solid);
            string material=key=="DeckFloor"?"Ground":key=="DeckWall"?"Rock":key=="DeckCrate"?"Enemy":key=="DeckBench"?"Taren":"Sela";
            var piece=ArenaBuilder.Block("BLOCKOUT "+key,position,size,material);
            if(!solid)foreach(var collider in piece.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
            return piece;
        }
        static void Floor(float x,float z,float width,float depth,string area="public")
        {
            var floor=Piece("DeckFloor",new Vector3(x,-.08f,z),new Vector3(width,.16f,depth));
            if(!blockout)StationSurfaces.FloorFinish(floor,area);
        }
        static void Wall(float x,float z,float width,float depth,float height)=>Piece("DeckWall",new Vector3(x,height*.5f,z),new Vector3(width,height,depth));
        static void SplitWall(float x,float z,float width,float opening,float height)
        {float span=(width-opening)*.5f;foreach(int side in new[]{-1,1})Wall(x+side*(opening*.5f+span*.5f),z,span,.5f,height);}
        static void Door(float x,float z)
        {
            if(blockout){foreach(int side in new[]{-1,1})Wall(x+side*2,z,.3f,.6f,3.2f);Piece("DeckWall",new Vector3(x,3.1f,z),new Vector3(4,.3f,.6f));}
            else
            {
                var frame=Piece("DeckDoorway",new Vector3(x,1.6f,z),new Vector3(4,3.2f,.6f),false);
                StationSurfaces.Fit(frame,new Vector3(4,3.2f,.6f));
                StationSurfaces.DoorwayCollision(frame);
            }
        }
        static GameObject Prop(string key,float x,float z,float height)=>Piece(key,new Vector3(x,height*.5f,z),new Vector3(2,height,2));
    }
}
