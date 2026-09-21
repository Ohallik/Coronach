using Lattice.World;
using UnityEngine;
namespace Lattice.EditorTools
{
    public static class DecksBuilder
    {
        public static void Build()=>BatchTools.Run(()=>{WorldBuilder.Prepare();BuildZone();Debug.Log("DECKS_OK");});
        public static void BuildZone()
        {
            var scene=WorldBuilder.Begin("Hub_Decks");
            WorldBuilder.Piece("DeckFloor",new(0,-.6f,-2),new(64,1,28),"Ground");
            WorldBuilder.Piece("DeckWall",new(0,2,11),new(64,4,1));
            WorldBuilder.Piece("DeckWall",new(-32,2,-2),new(1,4,28));WorldBuilder.Piece("DeckWall",new(32,2,-2),new(1,4,28));
            for(int i=0;i<2;i++)WorldBuilder.Piece("DeckWall",new(-10+i*20,1.5f,5),new(1,3,12));
            string[] ids={"Office","Shop","Repair"};
            for(int i=0;i<3;i++)
            {
                float x=(i-1)*20;WorldBuilder.Spawn(ids[i],new(x,0,-3));WorldBuilder.Dock("Launch — Cinder Halo",new(x,0,-10),"Hub_CinderHalo",ids[i]);
                WorldBuilder.Piece("DeckConsole",new(x+5,.8f,5),new(2,1.6f,1),"Sela");WorldBuilder.Piece("DeckCrate",new(x-5,.7f,5),Vector3.one*1.4f);
                WorldBuilder.Piece("DeckDoorway",new(x,1.8f,-7),new(5,3.6f,1),"Rock",false);
                WorldBuilder.Piece("DeckBench",new(x-6,.55f,8),new(3,1.1f,1.5f));
            }
            WorldBuilder.Spawn("Arrival",new(-20,0,-3));
            WorldBuilder.Npc("Orrin",new(-20,0,3));var mira=WorldBuilder.Npc("Mira",new(0,0,3));mira.postFlag="quest.MiraCrystals.complete";
            WorldBuilder.Npc("Hal",new(20,0,3));var sela=WorldBuilder.Npc("Sela",new(-10,0,-6),"SelaWaiting");sela.hideWhenJoined="Sela";sela.repeatNode="SelaWaiting";sela.postNode="";
            var bench=WorldBuilder.Piece("LatticeAnvil",new(26,.8f,1),new(2,1.6f,2),"Emission").AddComponent<Bench>();bench.prompt="Fabricate at Hal's bench";
            var repair=WorldBuilder.Piece("RepairConsole",new(15,.8f,1),new(1.5f,1.6f,1.5f),"Sela").AddComponent<RepairBay>();repair.prompt="Repair & save — 8 scrip per Sync level";
            WorldBuilder.Label("DOCK OFFICE",new(-20,.1f,-1));WorldBuilder.Label("OUTFITTER",new(0,.1f,-1));WorldBuilder.Label("REPAIR BAY",new(20,.1f,-1));
            WorldBuilder.Boundary("Deck front edge",new(0,5,-16),new(64,12,1));
            WorldBuilder.Save(scene,"Hub_Decks");
        }
    }
}
