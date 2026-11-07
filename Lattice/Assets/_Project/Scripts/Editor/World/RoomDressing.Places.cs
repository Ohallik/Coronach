using System.Linq;
using Lattice.Core;
using Lattice.World;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Dressing pass 2 (D192): the walkway centres, lamps and basin floors that pass 1 left bare.
    /// Stations get amber guide studs on every runner, stencilled pictograms at each room's
    /// threshold, hazard bands at dock doors, hull joints and work bays, inset grates/hatches/
    /// drains/tread plates on the plate grid, wall-side pipe runs, kiosks and cable drums.
    /// Sorrel gets tyre tracks on the haul road, ground patches that follow the terrain colour's
    /// clay and dust zones, plant clusters, low boulder groups and survey beacons by the ore.
    /// </summary>
    public static partial class RoomDressing
    {
        static readonly float[] Hulls={-28f,0f,28f};
        static bool terrainGround;

        static void DecksDetail()
        {
            // Guide studs on both edges of every runner (doorways skipped).
            foreach(float x in Hulls)Studs(new Vector2(x,-15.8f),new Vector2(x,18.8f),1.12f,2.4f,-16f,-7f,10f,19f);
            Studs(new Vector2(-39.7f,-3),new Vector2(39.7f,-3),1.12f,2.4f,-28f,0f,28f);
            Studs(new Vector2(-39.7f,21.5f),new Vector2(39.7f,21.5f),.95f,2.4f,-28f,0f,28f);

            // Stencilled pictograms just inside each room, beside its runner.
            foreach(float x in Hulls)Detail("PictoDock",x+2.5f,-12.4f,1.3f,1.3f,0);
            Detail("PictoOffice",-30.5f,-5.3f,1.3f,1.3f,0);Detail("PictoCabins",-30.5f,11.8f,1.2f,1.2f,0);
            Detail("PictoMarket",-2.5f,-5.3f,1.3f,1.3f,0);Detail("PictoGalley",3.7f,11.9f,1.2f,1.2f,0);
            Detail("PictoRepair",25.5f,-5.3f,1.3f,1.3f,0);Detail("PictoMarket",31.7f,11.9f,1.2f,1.2f,0);

            // Hazard paint: dock thresholds, the separation collars where hulls join, work and cargo bays.
            foreach(float x in Hulls)Detail("DecalHazard",x,-15.35f,.42f,3.8f,90,.02f,true,.9f);
            foreach(float x in new[]{-15.45f,-12.55f,12.55f,15.45f})
            {Detail("DecalHazard",x,-3,.34f,5.6f,0,.02f,true,.9f);Detail("DecalHazard",x,21.5f,.34f,4.6f,0,.02f,true,.9f);}
            HazardEdges(36.5f,-15.7f,39.7f,-10.9f,true,false,false,true);      // receiving bay
            HazardEdges(16.3f,-15.7f,22.7f,-13.6f,false,false,true,true);      // fuel store
            HazardEdges(29.8f,4.3f,34.2f,7.15f,true,true,true,true,.24f);      // the anvil's work zone
            HazardEdges(22.6f,16.5f,25.0f,18.7f,true,true,true,false);         // stores bay

            // Inset deck furniture on the plate grid: public grates and drains, work-floor tread plates.
            foreach(float x in Hulls)
            {
                bool work=x>20,market=Mathf.Abs(x)<1,office=x<-20;
                foreach(int s in new[]{-1,1})
                {
                    Detail("DecalGrate",x+s*6,-11.5f,1.5f,1.5f,0);
                    Detail("DecalDrain",x+s*9.6f,-8.7f,.95f,.95f,0);
                    if(work){Detail("DecalTread",x+s*6,-5.2f,1.7f,1.7f,0);Detail("DecalTread",x+s*6,1.7f,1.7f,1.7f,0);Detail("DecalHatch",x+s*10,1.7f,1.15f,1.15f,0);}
                    else{Detail("DecalGrate",x+s*6,1.7f,1.5f,1.5f,0);Detail("DecalHatch",x+s*9.8f,-5.2f,1.1f,1.1f,0);}
                    Detail("DecalTrench",x+s*4.4f,23.05f,.7f,1.5f,90);
                    Detail("DecalHatch",x+s*10.2f,23f,1f,1f,0);
                }
                if(market)foreach(int s in new[]{-1,1})Detail("DecalDrain",x+s*2.4f,15.2f,.9f,.9f,0);
                if(work){Detail("DecalTread",x-6,13.4f,1.7f,1.7f,0);Detail("DecalHatch",x+5.4f,12.6f,1.1f,1.1f,0);}
                if(office)Detail("DecalDrain",x+6,4.9f,.9f,.9f,0);
            }
        }

        static void DecksServices()
        {
            // A public information kiosk at each hull's promenade junction.
            foreach(float x in Hulls)Put("DressKiosk",x+3.3f,-5.4f,0);
            // Service pipes along the gallery's south walls, the market's side walls and the workshop/stores hull side.
            // Gallery services run along the full-height back wall, where the camera can see them.
            foreach(float x in new[]{-26f,0f,26f})Put("DressPipeRun",x,23.45f,0);
            Put("DressPipeRun",-11.43f,2.4f,-90);Put("DressPipeRun",11.43f,2.4f,90);
            Put("DressPipeRun",39.43f,.5f,90);Put("DressPipeRun",39.43f,12.4f,90);
            // Cable stock at receiving and in the workshop corner.
            Put("DressCableDrum",34.6f,-15f,0);Put("DressCableDrum",35.2f,8.75f,0);
        }

        static void TallowDetail()
        {
            Studs(new Vector2(0,-6.8f),new Vector2(0,2.6f),1f,2f);
            // The lift lobby is also the launch square: painted edges, threshold band, launch pictogram.
            HazardEdges(-1.95f,-11.65f,1.95f,-7.35f,true,true,true,false,.28f);
            Detail("DecalHazard",0,-7f,.4f,4f,90,.02f,true,.9f);
            Detail("PictoDock",0,-9.5f,1.5f,1.5f,0);
            // The bed's picture lay under its sign. The galley's stood at the foot of the screen from Abri's stop, sliced by the button bar and poking out under her dialogue box, and over the hatch's top from the lobby.
            // Here it's whole in the open from her stop, and whole under the box while she talks.
            Detail("PictoGalley",-7,.2f,1.1f,1.1f,0);Detail("PictoRepair",4.6f,1.4f,1.1f,1.1f,0);Detail("PictoCabins",2.5f,7.7f,1.1f,1.1f,0);
            foreach(int s in new[]{-1,1}){Detail("DecalGrate",s*8.4f,-4.6f,1.3f,1.3f,0);Detail("DecalDrain",s*4.4f,7f,.9f,.9f,0);}
            Detail("DecalHatch",9.8f,-1.4f,1.1f,1.1f,0);Detail("DecalTrench",8.6f,2.6f,.7f,1.4f,90);
            // No kiosk here: every open spot near the lobby is somebody's route, and the Keeper is the refuge's information point.
            // Life support behind the closed hatches gets its plumbing.
            Put("DressPipeRun",-7.4f,-11.4f,180);Put("DressPipeRun",7.4f,-11.4f,180);
            Put("DressCableDrum",10.6f,5.6f,0);
        }

        static void SorrelDetail()
        {
            // Haulers' tyre tracks: pad to outpost to excavation, a second lane on the busy outpost stretch, the rover's spur.
            // Vehicles turn on curves, not at the road's polyline vertices.
            var haul=Smooth(SorrelRedesign.Haul);
            Strip("GroundTracks",haul,1.6f,2.8f,.05f,-17,146);
            Strip("GroundTracks",haul,-1.6f,2.8f,.05f,-17,36);
            Strip("GroundTracks",Smooth(new[]{new Vector2(0,2),new Vector2(-6,8.5f),new Vector2(-12.6f,11.2f)}),0,2.8f,.05f);
            terrainGround=true;
            try
            {
                // Sample cores at the outpost bench and beside the hopper they were drilled for.
                Put("DressCoreRack",-8.8f,1.6f,0);Put("DressCoreRack",11.2f,144.2f,0);
                // Solid pieces first, so plants and patches can keep off them.
                Boulders();SurveyBeacons();ExcavationFloor();Flora();GroundPatches();
            }
            finally{terrainGround=false;}
        }

        /// <summary>The Burrower's arena was more flat dirt. The spoil line now runs from the bore to the hopper with its buffer, and what the drill threw up lies along the walls.
        /// All drawn only and under a metre: the fight's ground and sightlines don't change.</summary>
        static void ExcavationFloor()
        {
            // Ground patches draw after the bore's dark, so none may lie on it, nor a plant's gravel.
            signRects.Add(Rect.MinMaxRect(4.8f,174.2f,11.2f,179.8f));
            Strip("DecalRails",Smooth(new[]{new Vector2(12.4f,149.6f),new Vector2(11.4f,158),new Vector2(11.6f,168),new Vector2(10.7f,173.4f)}),0,1.25f,.035f);
            Put("DressRailBuffer",10.5f,174.4f,100,.8f,false);
            foreach(var (x,z,yaw,scale) in new[]{(13.6f,181.5f,40f,.72f),(-11.6f,171.5f,200f,.66f),(-12f,189f,110f,.7f),(13f,187f,300f,.62f)})
                Put("DressSpoilHeap",x,z,yaw,scale,false,false,.12f).name="Excavation spoil";
            foreach(var (x,z,size) in new[]{(9.4f,170f,4.4f),(5.5f,183f,5.2f),(-6f,189f,4.6f)})Detail("GroundStain",x,z,size,size,x*41,.045f,true);
        }
        /// <summary>Chaikin corner cutting (three passes), endpoints kept.</summary>
        static Vector2[] Smooth(Vector2[] path)
        {
            var points=path.ToList();
            for(int pass=0;pass<3;pass++)
            {
                var next=new System.Collections.Generic.List<Vector2>{points[0]};
                for(int i=0;i<points.Count-1;i++){next.Add(Vector2.Lerp(points[i],points[i+1],.25f));next.Add(Vector2.Lerp(points[i],points[i+1],.75f));}
                next.Add(points[^1]);points=next;
            }
            return points.ToArray();
        }

        static float Slope(Vector2 p)
        {
            var g=new Vector2(SorrelRedesign.Height(p+Vector2.right)-SorrelRedesign.Height(p-Vector2.right),SorrelRedesign.Height(p+Vector2.up)-SorrelRedesign.Height(p-Vector2.up))*.5f;
            return g.magnitude;
        }
        /// <summary>Distance beyond the packed surface of the nearest graded road.</summary>
        static float RoadDistance(Vector2 p)=>Mathf.Min(Distance(p,SorrelRedesign.Haul)-3.6f,Mathf.Min(Distance(p,SorrelRedesign.Seam),Distance(p,SorrelRedesign.Service))-2.4f);
        /// <summary>Clear of encounter arenas (D131: no vegetation in combat lanes), prompts and spawns, and
        /// optionally of replay corridors. Collider-free plants cannot block a route, so they skip corridors.</summary>
        static bool Open(Vector2 p,float radius,bool corridorsToo=true)
        {
            if(corridorsToo)foreach(var c in corridors)if(PointSegment(p,c.a,c.b)<c.half+radius)return false;
            foreach(var e in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))
            {var b=e.GetComponent<Collider>().bounds;if(p.x>b.min.x-radius&&p.x<b.max.x+radius&&p.y>b.min.z-radius&&p.y<b.max.z+radius)return false;}
            foreach(var pr in Object.FindObjectsByType<InteractionPrompt>(FindObjectsSortMode.None))
                if(Vector2.Distance(p,new Vector2(pr.transform.position.x,pr.transform.position.z))<radius+1.5f)return false;
            foreach(var s in Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
                if(Vector2.Distance(p,new Vector2(s.transform.position.x,s.transform.position.z))<radius+2f)return false;
            return true;
        }

        /// <summary>Ground patches whose kind follows the terrain colour: clay where the ground is
        /// tinted clay, wind ripples in the dust zones, mineral stains near ore, gravel and pebbles elsewhere.</summary>
        static void GroundPatches()
        {
            var random=new System.Random(5101);string[] plain={"GroundGravel","GroundGravel","GroundPebbles","GroundPebbles","GroundScuff"};
            for(float z=-34;z<=236;z+=7f)for(float x=-86;x<=86;x+=7f)
            {
                var p=new Vector2(x+(float)random.NextDouble()*6-3,z+(float)random.NextDouble()*6-3);
                float size=2.4f+(float)random.NextDouble()*2.4f,yaw=(float)random.NextDouble()*360,roll=(float)random.NextDouble();string pick=plain[random.Next(plain.Length)];
                if(roll>.38f||Slope(p)>.32f||RoadDistance(p)<.6f+size*.5f)continue;
                float clay=Mathf.PerlinNoise(p.x*.05f+31.7f,p.y*.05f+7.3f),dust=Mathf.PerlinNoise(p.x*.09f+3.1f,p.y*.09f+90.2f);
                bool ore=SorrelRedesign.Mines.Any(m=>Vector2.Distance(m,p)<11);
                string kind=ore?"GroundStain":clay>.55f?"GroundClay":dust>.6f?"GroundRipples":pick;
                Detail(kind,p.x,p.y,size,size,yaw,.045f);
            }
        }

        /// <summary>Low alien plants in clusters, thicker where a broad noise field says the basin holds moisture.</summary>
        static void Flora()
        {
            // The succulent's own red, faceted and white-tipped, is what a crystal looks like, and it stood beside the road while the objective pointed at the real, cyan ones.
            // Most of its colour now comes from a dusty sage, so it reads as dry scrub.
            var succulent=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Generated/Models/DressSucculent/DressSucculent_Toon.mat");
            succulent.SetFloat("_BaseMapStrength",.45f);succulent.SetColor("_BaseColor",new Color(.56f,.62f,.42f));UnityEditor.EditorUtility.SetDirty(succulent);
            // Everything built or standing, thin steel pads and the collider-free skiff included. A clump's middle kept clear of solid pieces, but its plants scatter 2 m round it.
            var built=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.GetComponent<TMPro.TMP_Text>()==null&&r.GetComponent<BiomePatch>()==null&&Mathf.Max(r.bounds.size.x,r.bounds.size.z)<60)
                .Select(r=>Rect.MinMaxRect(r.bounds.min.x,r.bounds.min.z,r.bounds.max.x,r.bounds.max.z)).ToList();
            var random=new System.Random(7717);int clusters=0,plants=0;
            // Only the band a player walks through (within 30 m of a graded road) is planted;
            // the far slopes are seen at a distance where the grass already reads.
            for(float z=-28;z<=200;z+=7f)for(float x=-72;x<=72;x+=7f)
            {
                var p=new Vector2(x+(float)random.NextDouble()*6-3,z+(float)random.NextDouble()*6-3);
                // Plants up to ~0.95 m: anything under 1 m can never cross the 40° sightline to a 1 m chest,
                // and at 0.5 m they read as specks from the gameplay camera.
                bool fan=random.Next(3)==0;int count=3+random.Next(3);double roll=random.NextDouble();
                var offsets=Enumerable.Range(0,count).Select(_=>(new Vector2((float)random.NextDouble()*3.2f-1.6f,(float)random.NextDouble()*3.2f-1.6f),(float)random.NextDouble()*360,1.3f+(float)random.NextDouble()*.45f)).ToArray();
                float wet=Mathf.SmoothStep(0,1,(Mathf.PerlinNoise(p.x*.035f+12.4f,p.y*.035f+55.1f)-.15f)/.35f);
                if(RoadDistance(p)>30)continue;
                if(roll>.85f*wet||Slope(p)>.4f||RoadDistance(p)<2.5f||!Open(p,2f,false))continue;
                if(Covered(p,1.6f)||signRects.Any(r=>PointRect(p,r)<2.4f)||placed.Any(q=>q.solid&&!q.structural&&PointRect(p,Footprint2D(q.go))<1.8f))continue;
                int rooted=plants;
                foreach(var (o,yaw,scale) in offsets)
                {
                    // On a slope's edge a plant sinks to its lowest footing and leaves a few shards poking out of the dirt. A clump mixes its kinds so it doesn't repeat.
                    if(Slope(p+o)>.16f||RoadDistance(p+o)<.6f||built.Any(r=>PointRect(p+o,r)<.8f))continue;
                    bool other=Mathf.Repeat(o.x*7.3f+o.y*3.1f,1)<.25f;string plant=fan!=other?"DressFanShrub":"DressSucculent";
                    // The kit's plants stand on a jagged disc of their own, up to 15% of their height. This puts it under the ground. A slope keeps its downhill side out, so they keep to gentler ones.
                    Put(plant,p.x+o.x,p.y+o.y,yaw,scale,false,true,.16f*Nominal[plant].y*scale);plants++;
                }
                if(plants==rooted)continue;
                // Their gravel skirts melt into one soft patch instead of each sitting on its own jagged disc.
                Detail("GroundGravel",p.x,p.y,5.2f,5.2f,p.x*37,.04f,true);
                clusters++;
            }
            Debug.Log($"DRESSING_FLORA clusters={clusters} plants={plants}");
        }

        /// <summary>Low boulder groups on the open basin floor; clearance and sightline pruning apply.</summary>
        static void Boulders()
        {
            var random=new System.Random(9029);int count=0;
            for(float z=-26;z<=230;z+=15f)for(float x=-80;x<=80;x+=15f)
            {
                var p=new Vector2(x+(float)random.NextDouble()*10-5,z+(float)random.NextDouble()*10-5);
                float yaw=(float)random.NextDouble()*360,scale=.75f+(float)random.NextDouble()*.4f;double roll=random.NextDouble();
                if(roll>.4f||Slope(p)>.22f||RoadDistance(p)<3f||Covered(p,1.8f)||signRects.Any(r=>PointRect(p,r)<2.2f))continue;
                Put("DressBoulders",p.x,p.y,yaw,scale,true,true,.12f);count++;
            }
            Debug.Log("DRESSING_BOULDERS placed="+count);
        }

        /// <summary>Survey beacons staking out each ore body and the seam road.</summary>
        static void SurveyBeacons()
        {
            var random=new System.Random(3301);int count=0;
            foreach(var m in SorrelRedesign.Mines)
                for(int i=0;i<2;i++)
                {
                    float a=(float)(random.NextDouble()*Mathf.PI*2);var p=m+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*5.5f;
                    float yaw=(float)random.NextDouble()*360;
                    if(!Covered(p,.8f)){Put("DressSurveyBeacon",p.x,p.y,yaw,1,true,true);count++;}
                }
            float along=10;int n=0;var path=SorrelRedesign.Seam;
            for(int i=1;i<path.Length;i++)
            {
                Vector2 a=path[i-1],d=path[i]-a;float length=d.magnitude;var normal=new Vector2(-d.y,d.x)/length;
                for(;along<length;along+=22f){var p=a+d*(along/length)+normal*((n++%2==0)?4.2f:-4.2f);float yaw=(float)random.NextDouble()*360;if(!Covered(p,.8f)){Put("DressSurveyBeacon",p.x,p.y,yaw,1,true,true);count++;}}
                along-=length;
            }
            Debug.Log("DRESSING_SURVEY placed="+count);
        }
    }
}
