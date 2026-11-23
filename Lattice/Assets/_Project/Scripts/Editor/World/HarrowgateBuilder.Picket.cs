using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>The Picket of Teeth (space combat; rule: staged detection). Map brief and evidence: docs/maps/PicketOfTeeth.md. Its places are the named constants of
    /// <see cref="Picket"/>: the map brief, the tests and a route writer read the same numbers.</summary>
    public static partial class HarrowgateBuilder
    {
        /// <summary>The spawn in the Picket a ship coming back up from the Scoria Flows arrives at.</summary>
        internal const string FromScoriaSpawn="Scoria";
        /// <summary>The spawn in the Picket a ship launched from Harrowgate's quay arrives at.</summary>
        internal const string FromTownSpawn="Harrowgate";

        /// <summary>The Picket's places. x east, z north, all on the flight plane.</summary>
        internal static class Picket
        {
            /// <summary>The lane: straight north, walled 52 m either side.</summary>
            public static readonly Vector2[] Lane={new(0,-10),new(0,800)};
            public const float Half=52;
            public static readonly Vector3 Arrival=new(0,1,6),HomeWarp=new(-14,1,-4),LockSpawn=new(0,1,594),FromScoria=new(-14,1,762),FromTown=new(18,1,762),
                ScoriaWay=new(-18,1,790),TownWay=new(22,1,790),WingLaunch=new(44,1,520);
            /// <summary>Lines across the lane: Idra's first words, and the lock in sight.</summary>
            public const float EdgeZ=78,LockSeenZ=604,WallZ=640;
            /// <summary>The four buoys (the quiet route's test reads these): where, the yaws a beam sweeps between, its half angle, reach, period and phase.</summary>
            public static readonly (string name,Vector2 at,float from,float to,float half,float reach,float period,float phase)[] Buoys=
            {
                ("B1",new(50,200),222,318,11,108,10,0),("B2",new(-50,360),50,130,11,108,10,.35f),("B3",new(50,470),230,310,11,108,10,.85f),("B4",new(0,570),140,220,13,62,8,.15f),
            };
            /// <summary>The rocks that shade the quiet route (centre, radius at the plane).</summary>
            public static readonly (Vector2 at,float r)[] Shades=
            {
                (new(10,172),6),(new(-14,196),5.5f),(new(8,226),6),(new(-14.1f,348.6f),5),(new(5.6f,370.4f),5),(new(-10.6f,392.6f),5),(new(-12.9f,428.7f),5),(new(7.8f,451.4f),5),
                (new(28.4f,472.9f),5),(new(9.2f,495.5f),5),(new(-18.2f,533.6f),5),
            };
            /// <summary>The quiet route: pockets (hidden from every buoy at every turn of its sweep) and the points a leg bends round a rock at.</summary>
            public static readonly (Vector2 at,bool pocket)[] Quiet=
            {
                (new(0,110),false),(new(2,160),true),(new(-26,190),true),(new(-21.7f,200),false),(new(-6,228),true),(new(0,300),false),(new(-22,322),true),(new(-6,346),true),
                (new(12.1f,365.4f),false),(new(13.5f,368.1f),false),(new(14,372),true),(new(-4,398),true),(new(-20,424),true),(new(-21,429.4f),false),(new(-19.2f,433.9f),false),
                (new(0,448),true),(new(-.3f,452.6f),false),(new(1.3f,456.4f),false),(new(20,474),true),(new(2.5f,490.8f),false),(new(1,495.1f),false),(new(2,500),true),
                (new(-22,526),true),(new(-24.7f,528.7f),false),(new(-26,531.3f),false),(new(-36,574),true),(new(0,606),false),
            };
            /// <summary>The lock's pieces in Idra's order: where each hand is, its prompt, its flag.</summary>
            public static readonly (string name,Vector3 at,string prompt,string flag)[] Pieces=
            {
                ("Port collar",new(-5.5f,1,633),"Take the collar off","vask.lock1"),("Starboard collar",new(5.5f,1,633),"Take the collar off","vask.lock2"),
                ("Keel collar",new(0,1,631),"Take the collar off","vask.lock3"),("Keeper bar",new(0,1,634),"Take the keeper bar out","vask.lock4"),
            };
            /// <summary>The flak gates in the wall, either side of the lock.</summary>
            public static readonly Vector3[] Flak={new(-24,1,636),new(24,1,636)};
            /// <summary>The clean lane through the wall, and the lock's length across it.</summary>
            public const float Gap=6.5f,LockLength=13.4f;
            /// <summary>The carrier, far under the field's east side, and the yaw it swings to.</summary>
            public static readonly Vector3 Carrier=new(34,-38,546);
            /// <summary>LookDev's stills: the arrival and Idra's line, each buoy's stretch as it is flown (from a pocket of the quiet route and from the open), the carrier from above the field,
            /// the lock as it is reached and close to, the ways down, and wide views of the field and the wall.</summary>
            public static readonly LookDev.View[] Views=
            {
                new("arrival",Arrival,true),new("edge",new Vector3(0,1,EdgeZ),true),new("first-tooth",new Vector3(2,1,160),true),new("b1-open",new Vector3(20,1,196),true),
                new("b1-shadows",new Vector3(-14,1,206),true),new("teeth-1",new Vector3(-6,1,346),true),new("teeth-2",new Vector3(14,1,372),true),new("teeth-3",new Vector3(-20,1,424),true),
                new("carrier",new Vector3(20,1,474),true),new("b3",new Vector3(36,1,470),true),new("b4",new Vector3(-22,1,526),true),new("b4-flank",new Vector3(-36,1,574),true),
                new("lock",new Vector3(0,1,606),true),new("lock-close",new Vector3(0,1,626),true),new("flak",new Vector3(-22,1,626),true),new("beyond",new Vector3(0,1,700),true),
                new("ways-down",new Vector3(2,1,782),true),new("field-wide",new Vector3(0,1,380),true,3.5f),new("first-wide",new Vector3(10,1,196),true,3f),new("wall-wide",new Vector3(0,1,630),true,3f),
                // Where the whole-game capture's stills stood (November 20, segment 45), at the flight camera's zoom, for the stills beside them.
                new("s45-066",new Vector3(8.7f,1,379.7f),true),new("s45-095",new Vector3(2.2f,1,500.5f),true),new("s45-177",new Vector3(1.2f,1,636.5f),true),
            };
            /// <summary>The nearest a rock may come to the quiet route's line: Idra's hull and a margin.</summary>
            public const float RouteClear=2.8f;
            /// <summary>How far a leg of the quiet route passes from a point.</summary>
            public static float FromQuiet(Vector2 p){float best=float.PositiveInfinity;for(int i=1;i<Quiet.Length;i++)best=Mathf.Min(best,ZoneKit.Segment(p,Quiet[i-1].at,Quiet[i].at));return best;}
        }
        /// <summary>An enemy of the chapter's roster if the combat engineer has defined it, else a stand-in that fights the same way (null: draw the body and fight nothing).</summary>
        static string Foe(string id,string standIn)=>Find<EnemyDef>(id)!=null?id:standIn;

        // ---------------------------------------------------------------- what the Picket is made of
        static Material BeamGlow,HaloGlow,ScoriaFace;
        static void PicketMaterials(ZoneKit kit)
        {
            // A beam's light: brighter along its two edges, so it reads as a sweep of light and not a grey sheet (the first stills); the fan's own vertex alpha fades it along its length. Additive, so where two beams cross it is brighter, and nothing under it is hidden.
            var soft=ZoneKit.Texture("Vaskar/picket-beam",64,(u,v)=>{float edge=ZoneKit.Smooth(.78f,.97f,Mathf.Abs(2*u-1));return new Color(1,1,1,.4f+.6f*edge);},false,true);
            BeamGlow=kit.Glow("Sensor beam",false,new Color(1,1,1,1));BeamGlow.SetTexture("_BaseMap",soft);BeamGlow.SetFloat("_Cull",0);
            HaloGlow=kit.Glow("Buoy halo",false,new Color(1.4f,1.5f,1.6f,1));
            RockMaterials();
            // Scoria from orbit (ScoriaFrom), painted at 1024 so its texels are as dense as the screen at its depth; its glow is its emission, so the night side still reads. The calderas are
            // fixed sites, so a rebuild paints the same bytes. The glow is painted from the same function: a texture once imported can't be read back.
            var sites=new System.Random(17);var calderas=new (float u,float v,float r)[7];
            for(int k=0;k<calderas.Length;k++)calderas[k]=((float)sites.NextDouble(),.2f+.6f*(float)sites.NextDouble(),.012f+.02f*(float)sites.NextDouble());
            // Kept as painted (uncompressed): block compression softened the rivers' and craters' edges into the blur the whole-game capture saw.
            var face=ZoneKit.Exact(ZoneKit.Texture("Vaskar/scoria-face",1024,(u,v)=>{var c=ScoriaFrom(u,v,calderas,out _);return new Color(c.r,c.g,c.b,1);}));
            // Its glow: the face itself, faintly (so the night side is still a world and not a hole), and the melt, a dull ember.
            var glow=ZoneKit.Exact(ZoneKit.Texture("Vaskar/scoria-glow",1024,(u,v)=>{var c=ScoriaFrom(u,v,calderas,out float h);return new Color(c.r*.28f+h*.55f,c.g*.28f+h*.12f,c.b*.28f+h*.05f,1);}));
            // A moon's way (the Spore Veil's): lit a little from its own face so the toon ramp doesn't cut the globe into two flat bands, and a detail layer keeps it crisp nearer in.
            ScoriaFace=ZoneKit.Toon("vaskar-scoria-face",face,m=>{m.SetColor("_BaseColor",new Color(.95f,.92f,.9f));m.SetTexture("_EmissionMap",glow);m.SetColor("_EmissionColor",new Color(.85f,.8f,.8f));
                m.SetFloat("_ReceiveShadows",0);m.SetFloat("_RampSteps",6);m.SetColor("_RimColor",new Color(.42f,.28f,.24f));ZoneKit.With(m,"_DETAIL_ON","_Detail");
                m.SetColor("_DetailTintA",new Color(.84f,.84f,.86f));m.SetColor("_DetailTintB",new Color(1.12f,1.1f,1.08f));m.SetVector("_DetailParams",new Vector4(.05f,.9f,.2f,.35f));});
        }

        static void BuildPicket()
        {
            var kit=ZoneKit.Flight(PicketZone);
            var scene=WorldBuilder.Begin(kit.zone);
            PicketMaterials(kit);
            WorldBuilder.Spawn("Arrival",Picket.Arrival);WorldBuilder.Spawn(FromScoriaSpawn,Picket.FromScoria);WorldBuilder.Spawn(FromTownSpawn,Picket.FromTown);WorldBuilder.Spawn("Lock",Picket.LockSpawn);
            kit.DriftingSky();
            var keep=PicketKeep();

            // ---- the backdrop: Scoria large under the field, ash far below, debris under the lane, the teeth along both edges
            ScoriaBelow(kit);ScoriaMoon(kit);
            AshBands(kit);
            Debris(kit,keep);ApproachWrecks(kit,keep);WaysDown(kit);
            Teeth(kit);

            // ---- the rocks in the lane: every one a shadow, solid to a hull, tumbling
            var rocks=LaneRocks(kit);

            // ---- the buoys and their beams
            var sweeps=new List<SensorSweep>();
            for(int n=0;n<Picket.Buoys.Length;n++)sweeps.Add(Buoy(kit,n));
            // The beams as they stand at the zone's first moment, and the fans they can sweep, drawn into the scene for the editor's stills: in play each buoy draws its own every frame.
            var shades=rocks.Select(r=>(r.at,r.r)).ToList();
            for(int n=0;n<sweeps.Count;n++)
            {
                var s=sweeps[n];var b=Picket.Buoys[n];var beamMesh=new Mesh{name="Buoy "+(n+1)+" beam"};var arcMesh=new Mesh{name="Buoy "+(n+1)+" reach"};
                SensorSweep.Still(beamMesh,arcMesh,b.at,Vector3.up*(SensorSweep.BeamHeight-1),b.from,b.to,b.half,b.reach,SensorSweep.YawAt(b.from,b.to,b.period,b.phase,0),s.cold,shades);
                s.beam.sharedMesh=ZoneKit.SaveMesh(beamMesh,"Assets/_Project/Scenes/"+kit.zone+"-Buoy"+(n+1)+"-Beam.asset");s.arc.sharedMesh=ZoneKit.SaveMesh(arcMesh,"Assets/_Project/Scenes/"+kit.zone+"-Buoy"+(n+1)+"-Reach.asset");
            }

            // ---- the carrier, the hunter wings it launches, and the alarm
            var (carrier,lights)=CarrierBelow(kit);
            string lancer=Foe("MusterLancer","MusterSkiff");var wings=new List<EncounterVolume>();
            for(int w=0;w<3;w++)
            {
                // Launched by the alarm, never flown into: the trigger is a box far under the plane.
                var wing=kit.FlightFight("Hunters_"+(w+1),Picket.WingLaunch,Vector3.one,(lancer,w<2?2:3));
                var spawner=wing.spawners[0];wing.transform.position=new Vector3(Picket.WingLaunch.x,-80,Picket.WingLaunch.z);
                spawner.transform.position=Picket.WingLaunch+new Vector3(0,0,w*4);spawner.radius=3;wings.Add(wing);
            }

            // ---- the wall, the flak gates in it and the lock in its gap
            var flak=Wall(kit);
            var routeLock=RouteLockAcross(kit);

            var alarm=new GameObject("The picket's alarm",typeof(PicketAlarm)).GetComponent<PicketAlarm>();
            alarm.wings=wings.ToArray();alarm.flak=flak;alarm.carrier=carrier;alarm.carrierLights=lights;

            // ---- Idra's words: the edge, the lock in sight (the main quest's step, a checkpoint), and the lock off once nothing is awake near the ship
            DiscoveryPoint Line(string name,float z,string flag,string node,string speaker,string report=null)
            {
                var line=kit.Crossing(name,new Vector3(0,0,z),new Vector3(Picket.Half*2+4,8,5),flag,node,speaker);line.report=report??"";return line;
            }
            Line("Idra at the picket's edge",Picket.EdgeZ,"vask.picketRead","IdraPicketEdge","Idra");
            var seen=Line("The lock in sight",Picket.LockSeenZ,"vask.picketCrossed","IdraLockReached","Idra","PicketCrossed");seen.saveSpawn="Lock";
            var off=new GameObject("Idra: the lock is off",typeof(AfterTalk)).GetComponent<AfterTalk>();off.transform.position=new Vector3(0,1,Picket.WallZ);
            off.flag="vask.lock4";off.node="IdraLockOff";off.speaker="Idra";off.heardFlag="vask.lockRemoved";off.delay=2.5f;off.range=0;

            // ---- ways in and out
            var home=WorldBuilder.Warp("Warp — The Hull of Many Hands",Picket.HomeWarp,"ManyHandsHull","",HomeSpawn);home.lockedPrompt="";
            var down=WorldBuilder.Warp("Land — The Scoria Flows",Picket.ScoriaWay,ScoriaZone,"vask.lockRemoved","Arrival");
            down.lockedPrompt="The lane to Scoria runs through the route lock: take it apart first";down.range=WorldBuilder.FlightDockRange;
            var town=WorldBuilder.Warp("Land — Harrowgate",Picket.TownWay,TownZone,"vask.parley","Arrival");
            town.lockedPrompt="Harrowgate's harbour guns cover the quay: walk in under a parley flag first, by the road from the Unsworn camp";town.range=WorldBuilder.FlightDockRange;
            WorldBuilder.Label("SCORIA",Picket.ScoriaWay+new Vector3(0,-1,-7),4);WorldBuilder.Label("HARROWGATE",Picket.TownWay+new Vector3(0,-1,-7),4);

            kit.FlightCorridor(Picket.Lane,Picket.Half);
            WorldBuilder.Save(scene,kit.zone);
            Debug.Log("HARROWGATE_COORDS PicketOfTeeth arrival="+Picket.Arrival+" home="+Picket.HomeWarp+" lock-spawn="+Picket.LockSpawn+" from-scoria="+Picket.FromScoria+" from-town="+Picket.FromTown
                +" buoys="+string.Join(" ",Picket.Buoys.Select(b=>b.name+b.at))+" rocks="+rocks.Count+" lock-pieces="+string.Join(" ",Picket.Pieces.Select(p=>p.at.ToString()))
                +" flak="+string.Join(" ",Picket.Flak.Select(f=>f.ToString()))+" wing-launch="+Picket.WingLaunch+" to-scoria="+Picket.ScoriaWay+" to-town="+Picket.TownWay);
            Debug.Log("PICKET_OF_TEETH_OK");
            _=routeLock;
        }

        /// <summary>What must stay readable under the lane, each with the room it needs: ways in and out, the buoys, the lock, the pockets of the quiet route.</summary>
        static List<(Vector2 at,float clear)> PicketKeep()
        {
            Vector2 F(Vector3 p)=>new(p.x,p.z);
            var keep=new List<(Vector2,float)>{(F(Picket.Arrival),6),(F(Picket.HomeWarp),6),(F(Picket.ScoriaWay),9),(F(Picket.TownWay),9),(new Vector2(0,Picket.WallZ-6),12),(F(HomeBuilder.PicketWarp),8)};
            foreach(var b in Picket.Buoys)keep.Add((b.at,6));
            return keep;
        }
        [ZoneLook.Source] static void PicketLooks()
        {
            // The border under a dim red star: a cool white key so hulls and the buoys' amber read true, an iron-violet fill off the nebula behind.
            ZoneLook.Register(PicketZone,new ZoneLook.Spec{sun=C(1f,.94f,.9f),sunIntensity=1.3f,shadowStrength=.8f,
                sky=C(.34f,.32f,.38f),equator=C(.3f,.24f,.26f),ground=C(.12f,.1f,.12f),
                contrast=14,saturation=6,splitShadows=C(.42f,.42f,.56f),splitHighlights=C(.6f,.5f,.44f),
                bloomThreshold=.85f,bloomIntensity=.8f,vignette=.24f});
        }

        // ---------------------------------------------------------------- the rocks
        /// <summary>The builds a rock can take (<see cref="Rock"/>): six, as the generated pieces it was drawn from were six, so a rock draws the same numbers it always drew.</summary>
        const int RockBuilds=6;
        /// <summary>Basalt and iron, a little rust: the rocks of the border, each a little apart from its neighbours. Tinted warmer they read as one brown, near the danger colours.</summary>
        static readonly Color[] RockTones={new(.37f,.37f,.41f),new(.41f,.39f,.38f),new(.33f,.35f,.4f),new(.4f,.37f,.35f),new(.36f,.37f,.41f)};
        /// <summary>A rock filling a circle, made in code (<see cref="Rock"/>), turned any way up and tumbling about its own middle; drawn only. It takes from <paramref name="rng"/> exactly
        /// what the generated piece it replaced took (which piece, how turned, its tint, then how it tumbles), in that order, so every rock placed after it stands where it always stood.</summary>
        static GameObject Asteroid(ZoneKit kit,string name,Vector3 centre,float radius,System.Random rng,bool tumbling)
        {
            int build=rng.Next(RockBuilds);var turned=Quaternion.Euler(rng.Next(360),rng.Next(360),rng.Next(360));var tone=RockTones[rng.Next(RockTones.Length)];
            var holder=new GameObject(name);holder.transform.position=centre;
            var shape=new ZoneKit.Shape();Rock(shape,centre,radius,turned,build,Mathf.RoundToInt(centre.x*31+centre.z*17+radius*7),tone);
            Stone(kit,name+" stone",shape,LaneRock,true,holder.transform);
            if(tumbling){var t=holder.AddComponent<Tumble>();t.axis=new Vector3((float)rng.NextDouble()-.5f,1,(float)rng.NextDouble()-.5f).normalized;t.rate=3+(float)rng.NextDouble()*5;}
            return holder;
        }
        /// <summary>The rocks in the lane: the eleven that shade the quiet route and more for the field's look, each a sensor shadow and solid to a hull. Extras are drawn evenly and dropped,
        /// not moved, when they would come near the quiet route, a buoy, the lock's approach or another rock.</summary>
        static List<(Vector2 at,float r)> LaneRocks(ZoneKit kit)
        {
            var rng=new System.Random(808);var root=new GameObject("Lane rocks");int made=0;
            var placed=new List<(Vector2 at,float r)>();
            void Rock((Vector2 at,float r) s)
            {
                var holder=Asteroid(kit,"Rock "+(made+1),new Vector3(s.at.x,1,s.at.y),s.r,rng,true);holder.transform.SetParent(root.transform,true);
                var solid=new GameObject("Rock body",typeof(SphereCollider),typeof(SensorShade));solid.transform.SetParent(root.transform,false);solid.transform.position=new Vector3(s.at.x,1,s.at.y);solid.isStatic=true;
                solid.GetComponent<SphereCollider>().radius=s.r;solid.GetComponent<SensorShade>().radius=s.r;solid.name="Rock "+(made+1)+" body";
                placed.Add(s);made++;
            }
            foreach(var s in Picket.Shades)Rock(s);
            for(int k=0;k<60;k++)
            {
                var at=new Vector2(-46+(float)rng.NextDouble()*92,130+(float)rng.NextDouble()*470);float r=3+(float)rng.NextDouble()*3.5f;
                bool clear=Picket.FromQuiet(at)>r+Picket.RouteClear&&Picket.Buoys.All(b=>Vector2.Distance(b.at,at)>r+9)&&placed.All(p=>Vector2.Distance(p.at,at)>p.r+r+5.2f)&&at.y<590
                    &&Vector2.Distance(at,new Vector2(Picket.WingLaunch.x,Picket.WingLaunch.z))>r+10&&Mathf.Abs(at.x)<Picket.Half-r-5;
                if(clear)Rock((at,r));
            }
            // The quiet route's own check: a rock in its way is a fault in the layout, said by name before anything is saved.
            foreach(var (at,r) in placed)if(Picket.FromQuiet(at)<r+Picket.RouteClear-.25f)throw new InvalidOperationException("PicketOfTeeth: a rock at "+at+" stands "+(Picket.FromQuiet(at)-r).ToString("0.0")+" m from the quiet route");
            return placed;
        }

        // ---------------------------------------------------------------- the buoys
        static SensorSweep Buoy(ZoneKit kit,int n)
        {
            var b=Picket.Buoys[n];var at=new Vector3(b.at.x,1,b.at.y);
            var root=new GameObject("Buoy "+(n+1));root.transform.position=at;
            var sweep=root.AddComponent<SensorSweep>();sweep.from=b.from;sweep.to=b.to;sweep.half=b.half;sweep.reach=b.reach;sweep.period=b.period;sweep.phase=b.phase;
            string id=Foe("SensorBuoy",null);
            if(id!=null)
            {
                // On show and asleep from the start; woken by the lead within 15 m. Its death is its beam's.
                var fight=kit.FlightFight("Buoy_"+(n+1),at,new Vector3(30,6,30),(id,1));fight.spawners[0].transform.position=at;fight.spawners[0].radius=0;
                var show=new GameObject("Buoy on show").transform;show.SetParent(fight.transform,false);show.position=at;fight.previewPoint=show;fight.previewRadius=0;
                sweep.encounter=fight;
            }
            else
            {
                var body=DrawnEnemy("SensorBuoy",at+Vector3.down*1.5f,3,(b.from+b.to)*.5f);if(body!=null){body.transform.SetParent(root.transform,true);body.name="Buoy body (drawn)";}
            }
            MeshFilter Layer(string name,int order)
            {
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);go.transform.localPosition=Vector3.zero;
                var view=go.GetComponent<MeshRenderer>();view.sharedMaterial=BeamGlow;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;view.sortingOrder=order;
                return go.GetComponent<MeshFilter>();
            }
            sweep.arc=Layer("What it can sweep",0);sweep.beam=Layer("Its beam",1);
            // The lamp: a light over the buoy and a halo the flight camera reads from its height.
            var lamp=new GameObject("Buoy lamp",typeof(Light));lamp.transform.SetParent(root.transform,false);lamp.transform.localPosition=Vector3.up*2.4f;
            var l=lamp.GetComponent<Light>();l.type=LightType.Point;l.range=18;l.intensity=3.2f;l.shadows=LightShadows.None;l.color=sweep.cold;sweep.lamp=l;
            var halo=GameObject.CreatePrimitive(PrimitiveType.Quad);halo.name="Buoy halo";UnityEngine.Object.DestroyImmediate(halo.GetComponent<Collider>());halo.transform.SetParent(root.transform,false);
            halo.transform.localPosition=Vector3.up*2.2f;halo.transform.rotation=Quaternion.Euler(48,0,0);halo.transform.localScale=Vector3.one*4.5f;
            var haloView=halo.GetComponent<Renderer>();haloView.sharedMaterial=HaloGlow;haloView.shadowCastingMode=ShadowCastingMode.Off;haloView.receiveShadows=false;sweep.halos=new[]{haloView};
            // Its mount: a stub of rock it is bolted to, so it isn't hung on nothing.
            var mount=Asteroid(kit,"Buoy "+(n+1)+" mount",at+new Vector3(0,-4.2f,0),3.2f,new System.Random(40+n),false);mount.transform.SetParent(root.transform,true);
            return sweep;
        }

        // ---------------------------------------------------------------- the carrier
        static (Transform carrier,Light[] lights) CarrierBelow(ZoneKit kit)
        {
            var root=new GameObject("The picket carrier");root.transform.position=Picket.Carrier;
            if(ZoneKit.Landed("Hauler"))
            {
                // A long hull in the Muster's gunmetal and rust, lying north on station: the Hauler fitted to ninety metres, drawn only.
                var hull=kit.Prop("Hauler",Picket.Carrier,90,0,false,false);hull.AddComponent<PropTint>().tint=new Color(.52f,.46f,.44f);
                hull.transform.position+=Picket.Carrier-ZoneKit.Drawn(hull).center;hull.transform.SetParent(root.transform,true);
                foreach(var r in hull.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
            }
            var lights=new List<Light>();
            foreach(var offset in new[]{new Vector3(0,6,30),new Vector3(0,6,-30),new Vector3(8,4,0),new Vector3(-8,4,0)})
            {
                var go=new GameObject("Running light",typeof(Light));go.transform.SetParent(root.transform,false);go.transform.localPosition=offset;
                var l=go.GetComponent<Light>();l.type=LightType.Point;l.range=26;l.intensity=4;l.shadows=LightShadows.None;lights.Add(l);
            }
            return (root.transform,lights.ToArray());
        }

        // ---------------------------------------------------------------- the wall and the lock
        /// <summary>Teeth across the lane at the wall's line, standing through the plane, set closer than they are wide and with rubble between them at the plane, so the solid boxes along
        /// it are stone wherever a hull meets them; the two flak gates in it. The lock's gap is left open here.</summary>
        static EncounterVolume Wall(ZoneKit kit)
        {
            var rng=new System.Random(640);var root=new GameObject("The wall of teeth");var stone=new ZoneKit.Shape();
            foreach(int side in new[]{-1,1})
            {
                float from=Picket.Gap+4.5f,to=Picket.Half+8;
                // No taller than sixteen: the camera sees a ship just past the wall (a tooth's top stands under half its height over the plane).
                for(float x=from;x<to;x+=4.2f+(float)rng.NextDouble()*2.4f)
                {
                    float wide=3.6f+(float)rng.NextDouble()*2.2f,tall=11+(float)rng.NextDouble()*5,dz=(float)rng.NextDouble()*3-1.5f;int seed=rng.Next(99999);
                    if(Picket.Flak.Any(f=>Mathf.Abs(f.x-side*x)<5.5f))continue;
                    Tooth(stone,new Vector3(side*x,1,Picket.WallZ+dz),tall,wide,seed,.6f);
                }
                // Rubble at the plane between them, kept off the lock's clean lane by its own reach (a point drawn out half again as far as the rest).
                for(float x=Picket.Gap+4;x<Picket.Half+5;x+=2.6f)
                {
                    float size=.75f+.25f*(float)rng.NextDouble(),dz=(float)rng.NextDouble()*1.6f-.8f;int seed=rng.Next(99999);
                    if(Picket.Flak.Any(f=>Mathf.Abs(f.x-side*x)<3.5f))continue;
                    Crag(stone,new Vector3(side*x,.4f,Picket.WallZ+dz),new Vector3(2.3f,2.6f,2.1f)*size,Quaternion.Euler(0,seed%360,0),seed,.24f,new Color(.5f,.49f,.53f),new Color(.2f,.2f,.24f),7,4);
                }
                // Solid from the lock's post to past the lane's edge: the teeth and the rubble are its look, this is what a hull meets.
                var solid=new GameObject("Wall body "+(side<0?"west":"east"),typeof(BoxCollider));solid.transform.SetParent(root.transform,false);solid.isStatic=true;
                float inner=Picket.Gap,outer=Picket.Half+4;solid.transform.position=new Vector3(side*(inner+outer)*.5f,1,Picket.WallZ);solid.GetComponent<BoxCollider>().size=new Vector3(outer-inner,14,3);
            }
            // The anchor rocks the lock's posts are collared to: two fangs.
            foreach(int side in new[]{-1,1})
            {
                var foot=new Vector3(side*(Picket.Gap+2.6f),1-16*.55f,Picket.WallZ+1);
                Crag(stone,foot+Vector3.up*1.2f,new Vector3(4.2f,2.8f,3.8f),Quaternion.Euler(0,side*40,0),side+3,.22f,new Color(.4f,.4f,.44f),new Color(.2f,.2f,.24f),8,4);
                Fang(stone,foot,new Vector3(side*.08f,1,.05f),16,3.2f,1,6,side+5,new Color(.62f,.61f,.64f),new Color(.2f,.2f,.25f));
            }
            Stone(kit,"The wall's stone",stone,LaneRock,true,root.transform);
            string id=Foe("FlakGate",null);
            if(id==null)
            {
                foreach(var f in Picket.Flak){var gate=DrawnEnemy("FlakGate",f+Vector3.down*4,10,0);if(gate!=null){gate.name="Flak gate (drawn)";gate.transform.SetParent(root.transform,true);}}
                return null;
            }
            // Asleep until the first piece of the lock comes off, or the carrier wakes them: their curtain is fired across the lock's approach.
            var fight=kit.FlightFight("Lock_Flak",new Vector3(0,1,624),new Vector3(Picket.Half*2,6,30),(id,1));
            var first=fight.spawners[0];first.transform.position=Picket.Flak[0];first.radius=0;first.transform.rotation=Quaternion.Euler(0,180,0);first.faceAsPlaced=true;
            var second=ZoneKit.Spawn(fight,id,1,Picket.Flak[1],0);second.transform.rotation=Quaternion.Euler(0,180,0);second.faceAsPlaced=true;
            fight.spawners=new[]{first,second};fight.requiredFlag="vask.lock1";
            var show=new GameObject("Flak gates on show").transform;show.SetParent(fight.transform,false);show.position=new Vector3(0,1,Picket.WallZ);fight.previewPoint=show;fight.previewRadius=0;
            return fight;
        }
        /// <summary>An enemy's generated body stood as scenery (no brain, no collider), fitted to a height: what a buoy or a gate is drawn as before its definition exists.</summary>
        static GameObject DrawnEnemy(string id,Vector3 foot,float height,float yaw)
        {
            var prefab=ZoneKit.Prefab("Enemies",id);if(prefab==null)return null;
            var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(body,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            foreach(var c in body.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            foreach(var b in body.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)UnityEngine.Object.DestroyImmediate(b);
            body.transform.rotation=Quaternion.Euler(0,yaw,0);var box=ModelGeometry.BoundsOf(body);body.transform.localScale*=height/Mathf.Max(.01f,box.size.y);
            box=ModelGeometry.BoundsOf(body);body.transform.position+=foot-new Vector3(box.center.x,box.min.y,box.center.z);
            return body;
        }
        /// <summary>The Compact route lock across the clean lane: the generated frame cut by code into the four pieces Idra calls (the port and starboard collars are its posts, the keel
        /// collar its bottom rail, the keeper bar its middle works) and what is left (its top rail), each drawn on its own so it can come off; a seal across the gap until the bar is out; a
        /// hand at each piece, offered in order.</summary>
        static RouteLock RouteLockAcross(ZoneKit kit)
        {
            var at=new Vector3(0,1,Picket.WallZ);var root=new GameObject("The route lock");root.transform.position=at;
            var routeLock=root.AddComponent<RouteLock>();
            var seal=new GameObject("Lock seal");seal.transform.SetParent(root.transform,false);
            var box=seal.AddComponent<BoxCollider>();box.size=new Vector3(Picket.Gap*2,14,2.5f);box.center=Vector3.zero;
            routeLock.blockers=seal;
            var pieces=new Transform[4];Transform rest=null;
            if(ZoneKit.Landed("RouteLock"))
            {
                var frame=kit.Prop("RouteLock",at,10,0,false,false);var drawn=ZoneKit.Drawn(frame);
                // Its length lies across the lane: turned if the piece came in lengthwise.
                if(drawn.size.z>drawn.size.x){frame.transform.rotation=Quaternion.Euler(0,90,0);drawn=ZoneKit.Drawn(frame);}
                frame.transform.localScale*=Picket.LockLength/Mathf.Max(.01f,drawn.size.x);drawn=ZoneKit.Drawn(frame);
                // The keeper bar at the plane: the frame's middle works are a little over half way up it.
                frame.transform.position+=at-new Vector3(drawn.center.x,drawn.min.y+drawn.size.y*.52f,drawn.center.z);drawn=ZoneKit.Drawn(frame);
                var cut=CutLock(kit,frame,drawn);for(int i=0;i<4;i++)pieces[i]=cut[i];rest=cut[4];
                foreach(var t in cut)if(t!=null)t.SetParent(root.transform,true);
                UnityEngine.Object.DestroyImmediate(frame);
            }
            routeLock.pieces=pieces;routeLock.frame=rest;
            for(int i=0;i<Picket.Pieces.Length;i++)
            {
                var (name,place,prompt,flag)=Picket.Pieces[i];
                var hand=kit.Discovery(name+"'s bolts",place,prompt,flag,"LockPiece",null,"Idra",null,0,WorldBuilder.FlightDockRange);
                hand.requiredFlag=i==0?"vask.picketCrossed":Picket.Pieces[i-1].flag;hand.saveSpawn="Lock";hand.situation="";hand.transform.SetParent(root.transform,true);
            }
            WorldBuilder.Label("ROUTE LOCK",at+new Vector3(0,-1,-14),3);
            return routeLock;
        }
        /// <summary>The lock's one mesh cut into its pieces by where each triangle lies in the frame (across it 0 to 1 west to east, up it 0 to 1): the posts, the bottom rail, the middle works,
        /// and the top rail with whatever else is left. Each piece is saved beside the scene, its vertices about its own middle so it can tumble away on its own.</summary>
        static Transform[] CutLock(ZoneKit kit,GameObject frame,Bounds drawn)
        {
            int Piece(Vector3 p)
            {
                float u=(p.x-drawn.min.x)/drawn.size.x,v=(p.y-drawn.min.y)/drawn.size.y;
                if(u<.15f)return 0;if(u>.85f)return 1;if(v<.3f)return 2;if(v>.33f&&v<.68f)return 3;return 4;
            }
            string[] names={"Port collar","Starboard collar","Keel collar","Keeper bar","Lock frame"};
            var parts=new List<(List<Vector3> v,List<Vector3> n,List<Vector2> uv,List<List<int>> tris,Material[] mats)>[5];
            for(int k=0;k<5;k++)parts[k]=new();
            var result=new Transform[5];
            foreach(var filter in frame.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;var view=filter.GetComponent<MeshRenderer>();if(mesh==null||view==null)continue;
                var world=filter.transform.localToWorldMatrix;var v=mesh.vertices;var n=mesh.normals;var uv=mesh.uv;
                var wv=v.Select(p=>world.MultiplyPoint3x4(p)).ToArray();var wn=n.Length==v.Length?n.Select(q=>world.MultiplyVector(q).normalized).ToArray():null;
                for(int k=0;k<5;k++)
                {
                    var map=new Dictionary<int,int>();var verts=new List<Vector3>();var norms=new List<Vector3>();var uvs=new List<Vector2>();var tris=new List<List<int>>();
                    for(int s=0;s<mesh.subMeshCount;s++)
                    {
                        var t=mesh.GetTriangles(s);var kept=new List<int>();
                        for(int i=0;i+2<t.Length;i+=3)
                        {
                            if(Piece((wv[t[i]]+wv[t[i+1]]+wv[t[i+2]])/3f)!=k)continue;
                            for(int j=0;j<3;j++){int o=t[i+j];if(!map.TryGetValue(o,out int m)){m=verts.Count;map[o]=m;verts.Add(wv[o]);norms.Add(wn!=null?wn[o]:Vector3.up);uvs.Add(uv.Length==v.Length?uv[o]:Vector2.zero);}kept.Add(m);}
                        }
                        tris.Add(kept);
                    }
                    if(verts.Count>0)parts[k].Add((verts,norms,uvs,tris,view.sharedMaterials));
                }
            }
            for(int k=0;k<5;k++)
            {
                if(parts[k].Count==0)continue;
                var all=parts[k].SelectMany(p=>p.v).ToList();var middle=all.Aggregate(Vector3.zero,(a,b)=>a+b)/all.Count;
                var holder=new GameObject(names[k]);holder.transform.position=middle;result[k]=holder.transform;
                int part=0;
                foreach(var (verts,norms,uvs,tris,mats) in parts[k])
                {
                    var mesh=new Mesh{name="PicketOfTeeth lock "+names[k]+" "+part};mesh.indexFormat=verts.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16;
                    mesh.SetVertices(verts.Select(p=>p-middle).ToList());mesh.SetNormals(norms);mesh.SetUVs(0,uvs);mesh.subMeshCount=tris.Count;
                    for(int s=0;s<tris.Count;s++)mesh.SetTriangles(tris[s],s);mesh.RecalculateBounds();
                    var saved=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-Lock-"+names[k].Replace(" ","")+"-"+part+".asset");
                    var go=new GameObject(names[k]+" mesh",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(holder.transform,false);
                    go.GetComponent<MeshFilter>().sharedMesh=saved;var r=go.GetComponent<MeshRenderer>();r.sharedMaterials=mats.Take(Mathf.Max(1,tris.Count)).ToArray();r.shadowCastingMode=ShadowCastingMode.On;
                    part++;
                }
            }
            return result;
        }

        // ---------------------------------------------------------------- the backdrop
        /// <summary>Debris under the lane, 4 to 30 m down: broken rock in every build (<see cref="Rock"/>), one mesh to each 140 m, and a few dead hulls, kept clear of what must be read by
        /// where each shows on screen.</summary>
        static void Debris(ZoneKit kit,List<(Vector2 at,float clear)> keep)
        {
            var rng=new System.Random(4141);var root=new GameObject("Debris below");string[] hulls={"Skiff","PatrolCutter","Hauler"};var stretches=new SortedDictionary<int,ZoneKit.Shape>();
            for(int k=0;k<560;k++)
            {
                var at=new Vector3(-62+(float)rng.NextDouble()*124,-4-(float)rng.NextDouble()*(float)rng.NextDouble()*26,-20+(float)rng.NextDouble()*840);float size=1.2f+(float)rng.NextDouble()*(float)rng.NextDouble()*6;
                bool hull=k%23==11;int pick=rng.Next(RockBuilds),seed=rng.Next(99999);var turned=Quaternion.Euler(rng.Next(360),rng.Next(360),rng.Next(360));var tone=RockTones[rng.Next(RockTones.Length)];
                if(!ZoneKit.ClearOnScreen(at,size*1.4f,keep))continue;
                if(hull)
                {
                    string key=hulls[pick%hulls.Length];if(!ZoneKit.Landed(key))continue;
                    var piece=kit.Loose(key,at,size*1.6f,turned,new Color(.36f,.34f,.36f));piece.transform.SetParent(root.transform,true);piece.isStatic=true;foreach(var t in piece.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;
                    foreach(var r in piece.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;continue;
                }
                int stretch=Mathf.FloorToInt((at.z+20)/140);if(!stretches.TryGetValue(stretch,out var shape))stretches[stretch]=shape=new ZoneKit.Shape();
                Rock(shape,at,size*.75f,turned,pick,seed,ZoneKit.Shape.Dim(tone,.85f));
            }
            foreach(var (stretch,shape) in stretches){var part=Stone(kit,"Debris "+stretch,shape,LaneRockDeep,false,root.transform);part.isStatic=true;part.GetComponent<MeshRenderer>().receiveShadows=false;}
            // Old buoys of the picket, dead and drifting under the approach, where the first frames are otherwise stars: what the party is flying into, before it is met.
            foreach(var (at,turn) in new[]{(new Vector3(9,-7,34),new Vector3(70,20,10)),(new Vector3(-16,-10,62),new Vector3(-40,110,25)),(new Vector3(15,-12,98),new Vector3(20,200,-60)),(new Vector3(-8,-6,8),new Vector3(-80,35,40))})
            {
                if(!ZoneKit.ClearOnScreen(at,3,keep))continue;
                var dead=DrawnEnemy("SensorBuoy",at,3.2f,0);if(dead==null)continue;
                var middle=ZoneKit.Drawn(dead).center;dead.transform.rotation=Quaternion.Euler(turn);dead.transform.position+=middle-ZoneKit.Drawn(dead).center;
                dead.AddComponent<PropTint>().tint=new Color(.42f,.42f,.46f);dead.name="Dead buoy";dead.transform.SetParent(root.transform,true);
            }
        }
        /// <summary>The teeth the Picket is named for: fangs of rock along both edges of the lane in their five builds, standing through the plane a few metres past the lane's walls, with
        /// lower rock between them at the plane, so the lane's sides read as a jaw a hull stops at. One mesh to each 120 m of a side.</summary>
        static void Teeth(ZoneKit kit)
        {
            var rng=new System.Random(77);var root=new GameObject("The teeth");
            foreach(int side in new[]{-1,1})
            {
                var shape=new ZoneKit.Shape();float from=-30;int made=0;
                void Flush(){if(made==0)return;Stone(kit,"Teeth "+(side<0?"west ":"east ")+Mathf.RoundToInt(from+30),shape,LaneRock,true,root.transform);shape=new ZoneKit.Shape();made=0;}
                for(float z=-30;z<870;z+=9+(float)rng.NextDouble()*7)
                {
                    float wide=3.6f+(float)rng.NextDouble()*3.2f,tall=16+(float)rng.NextDouble()*20,x=side*(Picket.Half+2.5f+wide*.8f+(float)rng.NextDouble()*5);int seed=rng.Next(99999);
                    float gapX=side*(Picket.Half+4.5f+(float)rng.NextDouble()*3),gapZ=z+4+(float)rng.NextDouble()*3;int gapSeed=rng.Next(99999);
                    if(z-from>=120){Flush();from=z;}
                    if(Mathf.Abs(z-Picket.WallZ)<10)continue;
                    Tooth(shape,new Vector3(x,1,z),tall,wide,seed);made++;
                    Crag(shape,new Vector3(gapX,-.6f,gapZ),new Vector3(2.6f,2.4f,3.4f)*(.8f+.4f*(gapSeed%100)/100f),Quaternion.Euler(0,gapSeed%360,0),gapSeed,.24f,new Color(.5f,.49f,.53f),new Color(.2f,.2f,.24f),7,4);
                }
                Flush();
            }
        }
    }
}
