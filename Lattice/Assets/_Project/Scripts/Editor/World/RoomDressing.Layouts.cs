using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Lattice.EditorTools
{
    public static partial class RoomDressing
    {
        // Yaw convention: 0 = front faces south (toward the camera), 180 = north,
        // -90 = east (piece stands against a west wall), 90 = west (against an east wall).

        /// <summary>The Decks: three converted hulls (office x=-28, market x=0, repair x=28).
        /// Tall pieces stand only where the floor behind them is wall, closed cabin or other furniture.</summary>
        static void Decks()
        {
            // Construction fix: the 16 m cross walls left 3.7 m side gaps, so the four "private"
            // cabins behind closed hatches were open to the office and service gallery.
            foreach(float x in new[]{-37.85f,-18.15f}){Wall("Cabin end wall",x,10,3.7f,.4f);Wall("Cabin end wall",x,19,3.7f,.4f);}

            // Walkway runners on the real circulation: each hull's dock-to-gallery spine, the promenade, the service gallery.
            foreach(float x in new[]{-28f,0f,28f})Decal("runner",x,1.5f,1.6f,34.6f,0);
            Decal("runner",0,-3,1.6f,79.4f,90,.016f);Decal("runner",0,21.5f,1.3f,79.4f,90,.016f);
            Decal("rug",-36.4f,5f,3.4f,2.4f,90);Decal("rug",-22f,5f,3.6f,2.2f,90);
            Decal("rug",-36.4f,12.2f,2.4f,1.7f,0);Decal("rug",-36f,17.2f,2.6f,1.9f,0);Decal("rug",-20.2f,11.6f,2.3f,1.6f,0);Decal("rug",-19.9f,17f,2.4f,1.7f,0);
            Decal("rug",-5.2f,15.15f,4.6f,2.4f,90);Decal("rug",5.2f,15.15f,4.6f,2.4f,90);
            // ---- Office hull: arrivals vestibule ----
            Put("DeckBench",-36.8f,-15.1f,180);Put("DressPlanter",-33f,-15.2f,180);
            Lamp(-30.9f,-15.2f,0);Lamp(-25.1f,-15.2f,0);
            Put("DressCargoPallet",-22.4f,-14.8f,0);Put("DeckCrate",-20.4f,-15.1f,15,.8f);Put("DressCargoPallet",-18.2f,-14.8f,0);
            Put("DressShelfRack",-37.4f,-7.65f,0);Put("DressShelfRack",-34.6f,-7.65f,0);
            Put("DeckBench",-21.6f,-8f,0);Put("DressPlanter",-18.4f,-7.7f,0);
            Put("DressPlanter",-37f,-6.2f,0);Put("DressPlanter",-20.6f,-6.2f,0);
            // ---- Orrin's office and the visitors' side ----
            Put("DressOfficeDesk",-37.8f,5f,90);Put("DressShelfRack",-39.3f,8.6f,-90);Put("DressShelfRack",-36.9f,9.38f,0);
            Put("DressPlanter",-39.2f,1.6f,-90);Lamp(-35.6f,6.4f,0);
            Put("DressShelfRack",-24.4f,9.38f,0);Put("DressPlanter",-16.9f,5f,90);Lamp(-17f,1.2f,0);
            // ---- Four cabins, now closed: homes, each a little different ----
            Retire("DeckBench",-36.5f,12.25f);Retire("DeckBench",-36.5f,16.75f);Retire("DeckBench",-19.5f,12.25f);Retire("DeckBench",-19.5f,16.75f);
            Retire("DeckCrate",-38f,13.5f);Retire("DeckCrate",-38f,18f);Retire("DeckCrate",-18f,13.5f);Retire("DeckCrate",-18f,18f);
            Put("DressCabinBunk",-38.7f,12.25f,0,.8f);Put("DressShelfRack",-36f,13.9f,0);Lamp(-35f,10.75f,0);
            Put("DressCabinBunk",-38.7f,16.75f,0,.8f);Put("DressGalleyTable",-36f,17.4f,0);Put("DressPlanter",-36.2f,15.2f,0);
            Put("DressCabinBunk",-17.3f,12.25f,0,.8f);Put("DressWorkbench",-20f,13.75f,0);Lamp(-18.2f,13.9f,0);
            Put("DressCabinBunk",-17.3f,16.75f,0,.8f);Put("DressShelfRack",-19.8f,15.15f,180);Put("DressPlanter",-19.6f,18.25f,0);

            // ---- Market hull: vestibule, Mira's market, galley ----
            Put("DeckBench",-8f,-15.1f,180);Put("DeckBench",8f,-15.1f,180);Lamp(-2.9f,-15.2f,0);Lamp(2.9f,-15.2f,0);
            Put("DressPlanter",-10.9f,-11.5f,-90);Put("DressPlanter",10.9f,-11.5f,90);
            Put("DressCargoPallet",-9.5f,-8.1f,0);Put("DressCargoPallet",9.5f,-8.1f,0);
            Put("DressPlanter",-7.5f,-6.2f,0);Put("DressPlanter",7.5f,-6.2f,0);
            foreach(float x in new[]{-7.8f,-6.4f,-5f,5f,6.4f,7.8f})Retire("DeckCrate",x,8);
            Put("DressMarketStall",-4.6f,9.05f,0);Put("DressMarketStall",4.6f,9.05f,0);
            Put("DressCargoPallet",-7.5f,8.9f,0);Put("DressCargoPallet",7.5f,8.9f,0);
            Lamp(-2.6f,5.3f,0);Lamp(2.6f,5.3f,0);
            // Her own counter, at her shoulder: on the runner, with the stalls five metres behind her, she didn't read as a shopkeeper. Under the camera's 1.4 m rule, like her mobile cart.
            Put("DressMarketStall",1.5f,5f,0,.643f);
            Put("HushwellCrates",-9.9f,6.6f,20,.9f);Put("DressCargoPallet",10.2f,6.4f,90);
            // Galley: herb planters back onto the market stalls through the wall.
            Put("DressPlanter",-4.6f,10.7f,180);Put("DressPlanter",4.6f,10.7f,180);
            foreach(float x in new[]{-5f,5f}){Retire("DeckBench",x,12);Retire("DeckBench",x,17);Retire("DeckCrate",x,14.5f);}
            foreach(float x in new[]{-5.2f,5.2f})foreach(float z in new[]{13.4f,16.9f})Put("DressGalleyTable",x,z,0);
            Put("DressPlanter",11.2f,13.5f,90);Put("DressPlanter",-11.2f,13.5f,-90);
            Lamp(-7.4f,15.1f,0);Lamp(7.4f,15.1f,0);Put("DressWaterBarrels",-10.6f,17.7f,0,.8f);

            // ---- Repair hull: receiving, Hal's workshop, stores ----
            Put("DressCargoPallet",37.9f,-14.6f,0);Put("DressCargoPallet",37.9f,-12.3f,0);
            Put("DressShelfRack",21f,-7.65f,0);Put("DressShelfRack",23.8f,-7.65f,0);
            Put("DressFuelTank",20.6f,-14.9f,0);Lamp(25.1f,-15.2f,0);Lamp(30.9f,-15.2f,0);
            Put("DressWorkbench",38.9f,6f,90);Put("DressShelfRack",33f,9.38f,0);Put("DressShelfRack",23f,9.38f,0);
            Put("DressCargoPallet",19f,2f,0);Put("HushwellOreCart",19.4f,6.6f,90);
            Lamp(30.6f,7.6f,0);Lamp(25.6f,7.4f,0);
            Put("DressShelfRack",16.7f,13.7f,-90);Put("DressShelfRack",16.7f,16.05f,-90);Put("DressCargoPallet",17.25f,18.15f,0);
            Put("DressWaterBarrels",37.8f,14f,0,.8f);Put("DressFuelTank",38.4f,17.5f,90);Put("DressCargoPallet",23.8f,17.6f,0);

            // ---- Service gallery: stores against the full-height back wall ----
            foreach(float x in new[]{-36f,-21f,-7f,7f,21f,36f})Put("DressShelfRack",x,23.3f,0);
            foreach(float x in new[]{-31f,-3f,3f,31f})Lamp(x,23.35f,0);

            DecksServices();DecksDetail();
        }

        /// <summary>Tallow's refuge: waiting and mess space to port, repair to starboard,
        /// growing beds, life support behind closed hatches, bunks in the rest cabins.</summary>
        static void Tallow()
        {
            foreach(int side in new[]{-1,1}){Retire("DeckBench",side*6,10.7f);Retire("DeckCrate",side*10,10.6f);}
            Decal("runner",0,-2.1f,1.5f,9.4f,0);
            Decal("rug",-10.4f,1.5f,3.2f,2f,90);Decal("rug",-4.3f,-2.6f,2.8f,2f,0);Decal("rug",-10.6f,6.4f,2.6f,1.9f,90);
            Decal("rug",-6.4f,10.5f,2.2f,1.5f,0);Decal("rug",6.4f,10.5f,2.2f,1.5f,0);
            Put("DressCabinBunk",-10.4f,10.45f,90,.8f);Put("DressCabinBunk",-2.6f,10.45f,-90,.8f);Put("DressCabinBunk",2.6f,10.45f,90,.8f);Put("DressCabinBunk",10.4f,10.45f,-90,.8f);
            Put("DressFuelTank",-10.6f,-10.4f,0);Put("DressWaterBarrels",-4.3f,-10.5f,0,.8f);
            Put("DressShelfRack",10.4f,-11.25f,0);Put("DressCargoPallet",4.5f,-10.5f,0);
            Put("DressPlanter",-11.7f,1.5f,-90);Lamp(-11.8f,-2.6f,0);
            Put("DressGalleyTable",-4.3f,-2.6f,0);
            Put("DressShelfRack",-11f,8.4f,0);Put("DressShelfRack",-8.75f,8.4f,0);Lamp(-8.7f,5.6f,0);
            Put("DressGalleyTable",-10.7f,6.4f,90);
            Lamp(1.8f,5.2f,0);Put("DressWorkbench",3.3f,5.4f,0);
            Put("DressShelfRack",10.85f,8.4f,0);Put("DressCargoPallet",11.3f,-5.8f,90);
            TallowDetail();
        }

        /// <summary>Sorrel: outpost life beside the habs and pads, slope strata and spoil
        /// generated across the basin, and ground colour that follows use and formation.</summary>
        static void Sorrel()
        {
            // The basin's north-east shoulder: a hero in this corner stood behind the rock from every distance the lens can take, with nothing on the screen but rock. A boulder fills the corner.
            Put("DressBoulders",14.8f,200.6f,35,1.6f,true,false,.15f);
            SorrelGround();
            // Habs: equipment in front of each hab, where the hab itself is what lies behind it.
            Put("DressFuelTank",16f,12.2f,0);Put("DressCommsMast",12.9f,11.9f,0);Put("DressSolarArray",21.6f,16f,90);Put("DressWaterBarrels",19f,20.6f,0,.8f);
            Put("DressRover",-15.3f,11.6f,0);Put("DressSolarArray",-21.6f,16f,90);Put("DressWaterBarrels",-19f,20.6f,0,.8f);
            // Receiving, stores and the repair pad, beside (never across) the chapter tour path.
            Put("DressCargoPallet",5f,-8.6f,0);Put("DressCargoPallet",8.6f,-8.4f,90);Put("DressWaterBarrels",2.6f,-9f,0,.8f);
            Put("HushwellOreCart",12f,-7.6f,0);Put("DressCargoPallet",19f,-4f,0);
            // Sorrel's terrain is one renderer: extra point lights would fight Forward's per-object
            // limit across the whole basin, so outpost bollards glow without casting a pool.
            Put("DressWorkbench",-6f,3f,0);Put("DressCargoPallet",-3.8f,4.6f,0);Lamp(-11.2f,8.6f,0,false);
            Put("DressFuelTank",-13f,-9f,90);
            Put("HushwellCrates",-9.5f,22f,30);Lamp(3.4f,20.4f,0,false);
            // Ore handling: the hopper backs onto the excavation face it is fed from.
            Put("DressOreHopper",15.5f,146.4f,0);
            Scatter();
            Verges();
            SorrelDetail();
        }

        /// <summary>Low rock, strata and spoil along the edges of the graded paths, and bollard
        /// lamps on the outpost stretch of the haul road. Everything here stays below the
        /// sightline ceiling, and clearance/sightline pruning still applies.</summary>
        static void Verges()
        {
            var random=new System.Random(2207);int count=0;
            foreach(var (path,half) in new[]{(SorrelRedesign.Haul,6.5f),(SorrelRedesign.Seam,5.5f),(SorrelRedesign.Service,5.5f)})
            {
                float along=4;int n=0;
                for(int i=1;i<path.Length;i++)
                {
                    Vector2 a=path[i-1],d=path[i]-a;float length=d.magnitude;var normal=new Vector2(-d.y,d.x)/length;
                    for(;along<length;along+=7.5f)
                    {
                        n++;float side=n%2==0?1:-1;
                        var p=a+d*(along/length)+normal*side*(half+.5f+(float)random.NextDouble()*2f);
                        if(p.y<28||p.y>150)continue; // outpost and excavation have their own dressing
                        int pick=random.Next(5);float yaw=(float)random.NextDouble()*360;
                        // Boulders, half sunk, both ways. A strata ledge laid along the road was a curved row of bright blocks, somebody's garden kerb on a wild ridge, and an outcrop beside it a stack of mortared bricks. The draws stay, so what follows stands where it stood.
                        if(pick==0)Put("DressBoulders",p.x,p.y,yaw,.42f+(float)random.NextDouble()*.12f,true,true,.12f);
                        else if(pick==1)Put("DressBoulders",p.x,p.y,yaw,.5f+(float)random.NextDouble()*.3f,true,true,.12f);
                        else if(pick==2)Put("HushwellRubble",p.x,p.y,yaw,.9f+(float)random.NextDouble()*.4f,true,true,.1f);
                        else if(pick==3)Put("DressSpoilHeap",p.x,p.y,yaw,.5f+(float)random.NextDouble()*.3f,true,true,.1f);
                        else Put("HushwellLowRidge",p.x,p.y,Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg+90,.9f,true,true,.1f);
                        count++;
                    }
                    along-=length;
                }
            }
            for(float z=-6;z<=40;z+=11)foreach(int side in new[]{-1,1})
            {
                var p=new Vector2(z<8?0:z<24?0:9*(z-24)/15,z)+new Vector2(side*3.6f,0);
                Put("DressLightBollard",p.x,p.y,0,1,true,true);count++;
            }
            Debug.Log("DRESSING_VERGES_PLACED "+count);
        }

        /// <summary>Nearer a road than this a strata piece isn't a far slope's contour: a ledge is a curved row of bright blocks on a low shoulder at the party's feet (the reader's kerb, at (-1.1, 112.2)), and an outcrop a stack of orange bricks in the open (above the key's cradle, at (2.6, 207.2)).</summary>
        const float LedgeFromRoad=18;
        static void Scatter()
        {
            var away=new Vector2(Mathf.Sin(20*Mathf.Deg2Rad),Mathf.Cos(20*Mathf.Deg2Rad));
            var random=new System.Random(1103);var kept=new List<Vector2>();int ledges=0,heaps=0;
            for(float z=-40;z<=240;z+=4.5f)for(float x=-92;x<=92;x+=4.5f)
            {
                var p=new Vector2(x+(float)random.NextDouble()*4-2,z+(float)random.NextDouble()*4-2);
                float h=SorrelRedesign.Height(p);
                var g=new Vector2(SorrelRedesign.Height(p+Vector2.right)-SorrelRedesign.Height(p-Vector2.right),SorrelRedesign.Height(p+Vector2.up)-SorrelRedesign.Height(p-Vector2.up))*.5f;
                float slope=g.magnitude;bool clear=true;
                foreach(var k in kept)if(Vector2.Distance(k,p)<6){clear=false;break;}
                if(!clear)continue;
                float rise=SorrelRedesign.Height(p+away*3)-h;
                if(slope>.2f&&slope<1.6f&&rise>.6f&&Mathf.Abs(p.x)<88&&p.y>-35&&p.y<235)
                {
                    // A far slope that faces the camera: strata ledges follow its contour, half buried.
                    float yaw=Mathf.Atan2(g.x,g.y)*Mathf.Rad2Deg;int pick=random.Next(6);
                    string key=pick<3?"DressStrataLedge":pick<5?"DressStrataOutcrop":"RidgeRock"+(1+random.Next(3));
                    float scale=key=="DressStrataLedge"?.8f+(float)random.NextDouble()*.5f:.7f+(float)random.NextDouble()*.4f;
                    // Boulders there, half sunk and low enough for any sightline. Its draws are all behind it, so what follows stands where it stood.
                    if((key=="DressStrataLedge"||key=="DressStrataOutcrop")&&RoadDistance(p)<LedgeFromRoad)Put("DressBoulders",p.x,p.y,yaw,scale*.6f,true,true,.12f);
                    else Put(key,p.x,p.y,yaw,scale,true,true,Nominal[key].y*scale*.3f);
                    kept.Add(p);ledges++;
                    continue;
                }
                if(slope<.08f)
                {
                    bool nearOre=false;foreach(var m in SorrelRedesign.Mines){float d=Vector2.Distance(m,p);if(d>5&&d<13)nearOre=true;}
                    bool excavation=p.y>140&&p.y<150&&Mathf.Abs(p.x)>8&&Mathf.Abs(p.x)<26;
                    if((nearOre||excavation)&&random.Next(2)==0)
                    {Put("DressSpoilHeap",p.x,p.y,(float)random.NextDouble()*360,.6f+(float)random.NextDouble()*.4f,true,true,.15f);kept.Add(p);heaps++;}
                }
            }
            Debug.Log($"DRESSING_SCATTER_PLACED ledges={ledges} heaps={heaps}");
        }

        /// <summary>Terrain vertex colour (packed haul road, clay and dust patches, darker
        /// slopes) and slope-masked strata on the shared ground material. Vertices are
        /// unchanged, so collision and navigation are unaffected.</summary>
        static void SorrelGround()
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Project/Scenes/Sorrel_Ridges-Terrain.asset");
            if(mesh==null)throw new System.InvalidOperationException("Sorrel terrain mesh missing");
            var v=mesh.vertices;var n=mesh.normals;var colors=new Color[v.Length];
            for(int i=0;i<v.Length;i++)
            {
                var p=new Vector2(v[i].x,v[i].z);float slope=1-n[i].y;
                float haul=Distance(p,SorrelRedesign.Haul),side=Mathf.Min(Distance(p,SorrelRedesign.Seam),Distance(p,SorrelRedesign.Service));
                float packed=Mathf.Max(1-Mathf.Clamp01((haul-3f)/.9f),.7f*(1-Mathf.Clamp01((side-1.8f)/.9f)));
                var c=new Color(.9f,.9f,.9f);
                // Wheel-packed ground: paler and less red.
                c=Color.Lerp(c,new Color(1f,.97f,.9f),packed);
                // Broad clay and wind-dust patches on the open floor.
                float clay=Mathf.PerlinNoise(p.x*.05f+31.7f,p.y*.05f+7.3f),dust=Mathf.PerlinNoise(p.x*.09f+3.1f,p.y*.09f+90.2f);
                c*=Color.Lerp(Color.white,new Color(.9f,.7f,.6f),Mathf.SmoothStep(0,1,(clay-.56f)/.035f)*.7f*(1-packed));
                c=Color.Lerp(c,new Color(1f,.98f,.92f),Mathf.SmoothStep(0,1,(dust-.6f)/.035f)*.55f*(1-packed));
                // Slopes: rustier, shadowed rock.
                c*=Color.Lerp(Color.white,new Color(.84f,.67f,.57f),Mathf.SmoothStep(.1f,.45f,slope));
                colors[i]=new Color(Mathf.Clamp01(c.r),Mathf.Clamp01(c.g),Mathf.Clamp01(c.b),1);
            }
            mesh.colors=colors;EditorUtility.SetDirty(mesh);
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/sorrel-ground.mat");
            material.EnableKeyword("_VCOLOR_ON");material.SetFloat("_VColor",1);
            material.EnableKeyword("_STRATA_ON");material.SetFloat("_Strata",1);
            material.SetColor("_StrataTintA",new Color(.7f,.5f,.42f));material.SetColor("_StrataTintB",new Color(1.12f,1.07f,.98f));
            material.SetVector("_StrataParams",new Vector4(1f,.85f,.3f,.55f));material.SetVector("_StrataSlope",new Vector4(.22f,.4f,1,0));
            EditorUtility.SetDirty(material);
            Debug.Log("DRESSING_SORREL_GROUND_OK vertices="+v.Length);
        }
        static float Distance(Vector2 p,Vector2[] path)
        {
            float d=float.PositiveInfinity;
            for(int i=1;i<path.Length;i++){var a=path[i-1];var s=path[i]-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,s)/s.sqrMagnitude);d=Mathf.Min(d,Vector2.Distance(p,a+s*t));}
            return d;
        }
    }
}
