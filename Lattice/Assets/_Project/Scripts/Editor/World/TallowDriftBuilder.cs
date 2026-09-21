using Lattice.World;
using UnityEngine;
namespace Lattice.EditorTools
{
    public static class TallowDriftBuilder
    {
        public static void Build()=>BatchTools.Run(()=>{WorldBuilder.Prepare();BuildZone();Debug.Log("TALLOW_DRIFT_OK");});
        public static void BuildZone()
        {
            var scene=WorldBuilder.Begin("TallowDrift");WorldBuilder.Spawn("Arrival",new(0,0,-8));
            WorldBuilder.Piece("TallowStationHull",new(0,-2,0),new(36,3,32));WorldBuilder.Piece("DeckFloor",new(0,-.5f,0),new(25,1,24),"Ground");
            WorldBuilder.Piece("DeckWall",new(0,2,12),new(28,4,1));WorldBuilder.Piece("DeckConsole",new(-7,1,5),new(3,2,2),"Sela");
            var keeper=WorldBuilder.Npc("Keeper",new(0,0,4));keeper.postFlag="sliceComplete";
            var repair=WorldBuilder.Piece("RepairConsole",new(6,1,4),new(2,2,2),"Emission").AddComponent<RepairBay>();repair.free=true;repair.CompleteSlice=true;repair.prompt="Repair, save and rest";
            WorldBuilder.Label("TALLOW DRIFT",new(0,.1f,-4));WorldBuilder.Save(scene,"TallowDrift");
        }
    }
}
