using Lattice.World;
using UnityEngine;
namespace Lattice.EditorTools
{
    public static class SorrelBuilder
    {
        public static void Build()=>BatchTools.Run(()=>{WorldBuilder.Prepare();BuildZone();Debug.Log("SORREL_OK");});
        public static void BuildZone()
        {
            var scene=WorldBuilder.Begin("Sorrel_Ridges");
            WorldArt.Planet("Vorun",new(160,35,350),150);
            // Generated tile texture on continuous walkable terrain; generated slabs dress its margins.
            var terrain=ArenaBuilder.Block("Sorrel terrain",new(0,-.6f,90),new(140,1,230),"Ground");WorldArt.Dress(terrain,"MoonGroundA",new(140,1,230));
            for(int i=0;i<20;i++)
            {
                float x=(i%2==0?-1:1)*(i%3==0?52:19),z=8+(i/2)*21;
                var slab=WorldBuilder.Piece(i%2==0?"MoonGroundA":"MoonGroundB",new(x,0,z),new(12,.6f,12),"Ground",false);
                var bounds=ModelGeometry.BoundsOf(slab);slab.transform.position+=Vector3.up*(-.08f-bounds.max.y);
            }
            WorldBuilder.Spawn("Arrival",new(0,0,-16));WorldBuilder.Spawn("Outpost",new(0,0,14));
            WorldBuilder.Dock("Launch — Cinder Halo",new(0,0,-18),"Hub_CinderHalo","Moon");
            WorldBuilder.Piece("Skiff",new(-8,1,-18),new(4,2,7),"Rock",false);
            for(int i=0;i<3;i++)WorldBuilder.Piece("DeckCrate",new(5+i*1.3f,.45f,-15),Vector3.one*.9f);
            // Rock spines divide the three paths while keeping their encounter centres open.
            for(int side=-1;side<=1;side+=2)
                for(int i=0;i<8;i++)
                {
                    float z=29+i*16, x=side*(16+Mathf.Sin(i*1.7f)*2);
                    var ridge=WorldBuilder.Piece("RidgeRock"+(i%3+1),new(x,1.6f,z),new(5,3.2f,9));ridge.transform.rotation=Quaternion.Euler(0,i*47,0);
                    for(int chip=0;chip<3;chip++)
                    {
                        var rock=WorldBuilder.Piece("RidgeRock"+(chip+1),new(x+side*(2+chip),.25f,z-4+chip*3),new(1,.5f+chip*.2f,1),"Rock",false);
                        rock.transform.rotation=Quaternion.Euler(0,chip*77+i*31,0);
                    }
                }
            WorldBuilder.Piece("OutpostHab",new(-7,2,18),new(5,4,8));WorldBuilder.Piece("OutpostHab",new(7,2,18),new(5,4,8));
            var safe=new GameObject("Outpost safe pocket",typeof(BoxCollider),typeof(SafePocket));safe.transform.position=new Vector3(0,1,16);safe.GetComponent<BoxCollider>().size=new Vector3(10,5,18);safe.GetComponent<BoxCollider>().isTrigger=true;
            WorldBuilder.Npc("Survivor",new(1,0,18));
            var repair=WorldBuilder.Piece("OutpostConsole",new(-3,.7f,16),new(1.3f,1.4f,1.3f),"Sela").AddComponent<RepairBay>();repair.free=true;repair.saveSpawn="Outpost";repair.prompt="Outpost cradle — repair & save";
            WorldBuilder.Label("SORREL OUTPOST",new(0,.1f,7));
            int index=0;
            for(int path=0;path<3;path++)
            {
                float x=(path-1)*32;
                for(int step=0;step<4;step++)
                {
                    float z=45+step*28;string enemy=index<6?"Ridgehound":index<10?"Scrapmite":"SentinelHusk";int count=enemy=="Ridgehound"?3:enemy=="Scrapmite"?6:1;
                    WorldBuilder.Encounter("Sorrel_"+index,new(x,1,z),new(22,5,18),enemy,count);index++;
                    WorldBuilder.Piece("RidgeRock"+(step%3+1),new(x+(path==1?12:-11),2,z+4),new(8,4,12));
                }
                for(int n=0;n<2;n++)
                {
                    var pos=new Vector3(x+6,1,35+n*68);var node=WorldBuilder.Piece(n==0?"CrystalClusterA":"CrystalClusterB",pos,new(2,2.5f,2),"Emission").AddComponent<MineNode>();
                    node.nodeId="ridge_"+path+"_"+n;node.prompt="Mine Ridge Crystal";node.amount=1;
                }
            }
            for(int i=0;i<8;i++){WorldBuilder.Piece("RidgeRock1",new(-66,3,i*30),new(8,8,32));WorldBuilder.Piece("RidgeRock2",new(66,3,i*30),new(8,8,32));}
            var anvil=WorldBuilder.Piece("LatticeAnvil",new(5,1,140),new(3,2,3),"Emission").AddComponent<Bench>();anvil.prompt="Link to the Lattice Anvil";
            var entry=WorldBuilder.Membrane("Drill entry seal",new(0,2,153),125,true);var exit=WorldBuilder.Membrane("Drill cradle seal",new(0,2,191),125,false);
            var boss=WorldBuilder.Encounter("Sorrel_Burrower",new(0,1,164),new(110,5,12),"Burrower",1,new[]{entry,exit});
            WorldBuilder.Piece("OutpostDrill",new(-12,4,181),new(7,8,7));WorldBuilder.Label("DRILL SITE",new(0,.1f,147));
            var key=WorldBuilder.Piece("WarpKeyCradle",new(0,1,199),new(1.5f,2,1.5f),"Emission").AddComponent<KeyPickup>();key.prompt="Recover the warp key";
            WorldBuilder.Dock("Launch — outer Halo beacon",new(8,0,201),"Hub_CinderHalo","Outer");
            WorldBuilder.Boundary("West ridge limit",new(-70,10,90),new(1,30,234));WorldBuilder.Boundary("East ridge limit",new(70,10,90),new(1,30,234));
            WorldBuilder.Boundary("Landing edge",new(0,10,-25),new(140,30,1));WorldBuilder.Boundary("Drill edge",new(0,10,205),new(140,30,1));
            WorldBuilder.Save(scene,"Sorrel_Ridges");
        }
    }
}
