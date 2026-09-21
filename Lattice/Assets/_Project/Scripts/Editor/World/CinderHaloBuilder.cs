using Lattice.World;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
namespace Lattice.EditorTools
{
    public static class CinderHaloBuilder
    {
        public static void Build()=>BatchTools.Run(()=>{WorldBuilder.Prepare();BuildZone();Debug.Log("CINDER_HALO_OK");});
        public static void BuildZone()
        {
            var scene=WorldBuilder.Begin("Hub_CinderHalo");
            WorldArt.Planet("Vorun",new(45,-110,105),180);WorldArt.Planet("Sorrel",new(-85,-40,85),35);
            WorldBuilder.Spawn("Arrival",new(-20,1,3));WorldBuilder.Spawn("Office",new(-20,1,5));WorldBuilder.Spawn("Shop",new(0,1,9));WorldBuilder.Spawn("Repair",new(20,1,5));WorldBuilder.Spawn("Outer",new(0,1,61));WorldBuilder.Spawn("Moon",new(54,1,8));
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12;var go=WorldBuilder.Piece(i%2==0?"RingSegmentA":"RingSegmentB",new Vector3(Mathf.Sin(a)*45,-4,Mathf.Cos(a)*45),new(11,3,8));go.transform.rotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
            }
            string[] pods={"Dock Office","Outfitter","Repair Bay"};string[] spawns={"Office","Shop","Repair"};
            for(int i=0;i<3;i++)
            {
                var pos=new Vector3((i-1)*20,1,i==1?18:12);WorldBuilder.Piece("DockingPod"+spawns[i],pos-Vector3.up*2,new(9,3,10));WorldBuilder.Dock("Dock — "+pods[i],pos,"Hub_Decks",spawns[i]);WorldBuilder.Label(pods[i],pos+Vector3.up*4);
            }
            WorldBuilder.Warp("Sorrel approach",new(65,1,8),"Sorrel_Ridges","met.Orrin");
            WorldBuilder.Warp("Outer warp — The Gullet",new(0,1,74),"Gullet_Tunnel");
            var neve=WorldBuilder.Npc("Neve",new(-7,1,-8));foreach(var renderer in neve.GetComponentsInChildren<Renderer>())renderer.enabled=false;neve.GetComponent<Collider>().enabled=false;
            WorldBuilder.Piece("Skiff",neve.transform.position-Vector3.up,new(4,1.5f,6),"Sela",false);
            var hail=neve.gameObject.AddComponent<HailPoint>();hail.contact=neve;hail.prompt="Hail Neve";hail.range=7;neve.range=0;
            for(int laneIndex=0;laneIndex<4;laneIndex++)
            {
                var root=new GameObject("TrafficLane_"+laneIndex,typeof(SplineContainer),typeof(TrafficLane));var container=root.GetComponent<SplineContainer>();var spline=new Spline();
                for(int i=0;i<12;i++){float a=i*Mathf.PI/6;float radius=61+laneIndex*7;spline.Add(new BezierKnot(new float3(Mathf.Sin(a)*radius,1+laneIndex*.6f,Mathf.Cos(a)*radius)));}
                spline.Closed=true;spline.SetTangentMode(TangentMode.AutoSmooth);container.Spline=spline;var lane=root.GetComponent<TrafficLane>();lane.speed=4+laneIndex*.5f;
                for(int j=0;j<2;j++){var ship=WorldBuilder.Piece(j==0?"Hauler":"PatrolCutter",Vector3.zero,new Vector3(j==0?4:2.5f,1.5f,j==0?8:5),"Rock",false);ship.isStatic=false;var mover=ship.AddComponent<TrafficShip>();mover.lane=lane;mover.phase=j*.5f+laneIndex*.12f;}
            }
            WorldBuilder.Save(scene,"Hub_CinderHalo");
        }
    }
}
