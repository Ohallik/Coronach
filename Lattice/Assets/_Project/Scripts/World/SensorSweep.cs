using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// A sensor buoy's beam (chapter 8's Picket of Teeth: staged detection). It sweeps a cone of <see cref="half"/> degrees either side of a yaw that runs from <see cref="from"/> to
    /// <see cref="to"/> and back once a <see cref="period"/>, eased at each end, out to <see cref="reach"/>, and sees whatever is in it on the flight plane that no rock's shadow
    /// (<see cref="SensorShade"/>) hides. What it can see is drawn: the beam itself, pale and brighter while it holds the lead ship, and under it, fainter, the whole fan it can sweep, both cut
    /// short where a rock stands in the way, so the shadows are dark wedges in the light. Its lamp burns cold white while the picket is quiet and pulses amber once it has half heard.
    /// <para>The body is the buoy enemy of <see cref="encounter"/>, on show and asleep until the lead comes near: once that encounter is cleared the buoy is dead, its beam and lamp go dark
    /// for good, and <see cref="Silenced"/> tells the picket (it got its last call off). A save that has the buoy dead loads it dark, and says nothing.</para>
    /// The sweep keeps its own clock, so every buoy in a zone stays in the step it was built with: the quiet route's windows are worked out on <see cref="YawAt"/>.
    /// </summary>
    public sealed class SensorSweep:MonoBehaviour
    {
        public float from=220,to=320,half=11,reach=100,period=10,phase;
        /// <summary>The buoy's own fight; null for a buoy nobody can shoot.</summary>
        public EncounterVolume encounter;
        /// <summary>The beam (rebuilt each frame) and the fan it can sweep (built once), each a renderer with a mesh filter, drawn with an additive glow.</summary>
        public MeshFilter beam,arc;
        public Light lamp;public Renderer[] halos=System.Array.Empty<Renderer>();
        public Color cold=new(.6f,.86f,1f,1),amber=new(1f,.66f,.2f,1);
        /// <summary>How fast the beam sweeps: the picket's alarm sets it (1 quiet, faster once it has half heard).</summary>
        public float Pace{get;set;}=1;
        /// <summary>The lamp's state: 0 quiet, 1 and over amber.</summary>
        public int Stage{get;set;}
        public float Clock{get;private set;}
        public bool Dark{get;private set;}
        public float Yaw=>YawAt(from,to,period,phase,Clock);
        /// <summary>Whether the beam is on the lead ship this frame (it draws brighter).</summary>
        public bool Holding{get;set;}
        public static event System.Action<SensorSweep> Silenced;
        static readonly List<SensorSweep> live=new();
        public static IReadOnlyList<SensorSweep> All=>live;
        /// <summary>The flight plane the beam lies just under.</summary>
        const float Plane=1,Under=.55f;const int Rays=36,ArcRays=96;
        Mesh beamMesh;MaterialPropertyBlock block;bool started;

        public static float YawAt(float from,float to,float period,float phase,float clock)
        {
            float u=Mathf.Repeat(clock/Mathf.Max(.01f,period)+phase,1);
            return from+(to-from)*(.5f-.5f*Mathf.Cos(2*Mathf.PI*u));
        }
        public static Vector2 Direction(float yaw)=>new(Mathf.Sin(yaw*Mathf.Deg2Rad),Mathf.Cos(yaw*Mathf.Deg2Rad));
        /// <summary>Whether a beam at <paramref name="yaw"/> from <paramref name="origin"/> sees <paramref name="point"/>, on the plane, past the given shades (null: the scene's).</summary>
        public static bool Sees(Vector2 origin,float yaw,float half,float reach,Vector2 point,IReadOnlyList<(Vector2 centre,float radius)> shades=null)
        {
            var d=point-origin;float distance=d.magnitude;if(distance>reach||distance<.01f)return false;
            float bearing=Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg;if(Mathf.Abs(Mathf.DeltaAngle(bearing,yaw))>half)return false;
            return shades!=null?!SensorShade.Blocks(origin,point,shades):!SensorShade.Blocks(new Vector3(origin.x,0,origin.y),new Vector3(point.x,0,point.y));
        }
        /// <summary>Whether this buoy could ever see the point, at any yaw of its sweep: what a hiding place is measured against.</summary>
        public static bool EverSees(Vector2 origin,float from,float to,float half,float reach,Vector2 point,IReadOnlyList<(Vector2 centre,float radius)> shades=null)
        {
            var d=point-origin;float distance=d.magnitude;if(distance>reach)return false;
            float bearing=Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg;float mid=(from+to)*.5f,span=(to-from)*.5f+half;
            if(Mathf.Abs(Mathf.DeltaAngle(bearing,mid))>span)return false;
            return shades!=null?!SensorShade.Blocks(origin,point,shades):!SensorShade.Blocks(new Vector3(origin.x,0,origin.y),new Vector3(point.x,0,point.y));
        }
        public Vector2 Origin=>new(transform.position.x,transform.position.z);
        /// <summary>Whether no live buoy could see this point at any turn of its sweep: a rock's shadow, or out of every reach.</summary>
        public static bool HiddenFromAll(Vector3 point)
        {
            var at=new Vector2(point.x,point.z);
            foreach(var sweep in live)if(sweep!=null&&!sweep.Dark&&EverSees(sweep.Origin,sweep.from,sweep.to,sweep.half,sweep.reach,at))return false;
            return true;
        }
        /// <summary>Whether the beam sees this point now. A dead buoy sees nothing.</summary>
        public bool Sees(Vector3 point)=>!Dark&&Sees(Origin,Yaw,half,reach,new Vector2(point.x,point.z));

        void OnEnable(){if(!live.Contains(this))live.Add(this);}
        void OnDisable()=>live.Remove(this);
        void Start()
        {
            // Every buoy's clock starts from the zone's: built in step, they stay in step.
            Clock=Time.time;started=true;
            if(encounter!=null&&IsDead())GoDark(false);
            BuildArc();
        }
        bool IsDead()
        {
            var flags=GameServices.Current!=null?GameServices.Current.Flags:null;
            if(flags!=null&&!string.IsNullOrEmpty(encounter.encounterId)&&flags.GetBool("clear."+encounter.encounterId))return true;
            return encounter.Cleared;
        }
        void Update()
        {
            if(!started)return;
            if(!GameTime.Paused)Clock+=Time.deltaTime*Pace;
            if(!Dark&&encounter!=null&&encounter.Cleared)GoDark(true);
            if(Dark)return;
            DrawBeam();ShowLamp();
        }
        void GoDark(bool inPlay)
        {
            Dark=true;Holding=false;
            if(beam!=null)beam.gameObject.SetActive(false);if(arc!=null)arc.gameObject.SetActive(false);
            if(lamp!=null)lamp.enabled=false;foreach(var h in halos)if(h!=null)h.enabled=false;
            if(inPlay){Debug.Log("BUOY_DOWN "+name);Silenced?.Invoke(this);}
        }
        void ShowLamp()
        {
            bool alarmed=Stage>=1;
            // Amber pulses at a heartbeat; quiet, the lamp burns steady and cold.
            float pulse=alarmed?.55f+.45f*Mathf.Abs(Mathf.Sin(Time.time*Mathf.PI*1.6f)):1;
            var colour=alarmed?amber:cold;
            if(lamp!=null){lamp.color=colour;lamp.intensity=(alarmed?5.5f:3.2f)*pulse;}
            block??=new MaterialPropertyBlock();
            foreach(var h in halos)
            {
                if(h==null)continue;h.GetPropertyBlock(block);block.SetColor("_BaseColor",new Color(colour.r*1.6f,colour.g*1.6f,colour.b*1.6f,pulse));h.SetPropertyBlock(block);
            }
        }
        /// <summary>The live beam: a fan of rays, each as long as the nearest rock lets it be, bright at the buoy and fading to its reach. Once the picket has half heard it throbs on the
        /// lamp's heartbeat, still cold: a buoy's lamp is fifty metres off the lane's middle and out of frame, and the beam over the ship is what a player sees (November 14).</summary>
        void DrawBeam()
        {
            if(beam==null)return;
            if(beamMesh==null){beamMesh=new Mesh{name="sensor beam"};beamMesh.MarkDynamic();beam.sharedMesh=beamMesh;}
            float throb=Stage>=1?.7f+.8f*Mathf.Abs(Mathf.Sin(Time.time*Mathf.PI*1.6f)):1;
            Fan(beamMesh,Yaw-half,Yaw+half,Rays,(Holding?.5f:.26f)*throb,(Holding?.18f:.06f)*throb);
            // The whole fan it can sweep is a little brighter once alarmed, and back when the picket is quiet again.
            if(arcStage!=Mathf.Min(Stage,1))BuildArc();
        }
        int arcStage;
        void BuildArc()
        {
            if(arc==null)return;arcStage=Mathf.Min(Stage,1);float lift=arcStage>=1?1.6f:1;
            if(arcMesh==null)arcMesh=new Mesh{name="sensor reach"};Fan(arcMesh,from-half,to+half,ArcRays,.07f*lift,.025f*lift);arc.sharedMesh=arcMesh;
        }
        /// <summary>The beam stays cold at every stage: amber, orange and red on the plane are a danger marking's (D217), and a beam strikes nobody. The lamp says the stage.</summary>
        void Fan(Mesh mesh,float a,float b,int rays,float near,float far)=>Fan(mesh,Origin,new Vector3(Origin.x,Plane-Under,Origin.y)-transform.position,a,b,rays,near,far,reach,cold,null);
        /// <summary>A fan of rays from <paramref name="o"/> (on the plane), each as long as the nearest shade lets it be (the scene's, or <paramref name="shades"/>), into a mesh whose points
        /// are <paramref name="centre"/> plus the ray: bright at the buoy and fading to its reach. The builder draws the editor's stills with it; play redraws it every frame.</summary>
        public static void Fan(Mesh mesh,Vector2 o,Vector3 centre,float a,float b,int rays,float near,float far,float reach,Color tint,IReadOnlyList<(Vector2 centre,float radius)> shades)
        {
            var points=new List<Vector3>(rays*2+2);var tones=new List<Color>(rays*2+2);var faces=new List<int>(rays*6);var uv=new List<Vector2>(rays*2+2);
            for(int i=0;i<=rays;i++)
            {
                float yaw=Mathf.Lerp(a,b,i/(float)rays);var d=Direction(yaw);float length=shades!=null?SensorShade.Reach(o,d,reach,shades):SensorShade.Reach(o,d,reach);
                // Two points a ray: where it starts and where it stops, so a ray cut short by a rock ends there, and the wedge behind the rock stays dark.
                // Across the fan 0 to 1 and along it 0 to 1: the beam's texture brightens its two edges, so a beam reads as a sweep of light and not a grey sheet.
                float across=i/(float)rays;
                points.Add(centre);tones.Add(new Color(tint.r,tint.g,tint.b,near));uv.Add(new Vector2(across,0));
                points.Add(centre+new Vector3(d.x,0,d.y)*length);tones.Add(new Color(tint.r,tint.g,tint.b,Mathf.Lerp(near,far,length/Mathf.Max(1,reach))));uv.Add(new Vector2(across,length/Mathf.Max(1,reach)));
                if(i>0){int p=(i-1)*2;faces.Add(p);faces.Add(p+1);faces.Add(p+3);faces.Add(p);faces.Add(p+3);faces.Add(p+2);}
            }
            mesh.Clear();mesh.SetVertices(points);mesh.SetColors(tones);mesh.SetUVs(0,uv);mesh.SetTriangles(faces,0);mesh.RecalculateBounds();
            // Seen from above whichever way it was wound.
            var normals=new Vector3[points.Count];for(int i=0;i<normals.Length;i++)normals[i]=Vector3.up;mesh.normals=normals;
        }
        /// <summary>The beam's look at a moment and the fan it can sweep, as meshes, for the builder's stills (a scene isn't playing in the editor, so the live beam isn't there).</summary>
        public static void Still(Mesh beam,Mesh arc,Vector2 o,Vector3 centre,float from,float to,float half,float reach,float yaw,Color tint,IReadOnlyList<(Vector2 centre,float radius)> shades)
        {
            Fan(beam,o,centre,yaw-half,yaw+half,Rays,.26f,.06f,reach,tint,shades);Fan(arc,o,centre,from-half,to+half,ArcRays,.07f,.025f,reach,tint,shades);
        }
        public const float BeamHeight=Plane-Under;
        Mesh arcMesh;
        void OnDestroy(){if(beamMesh!=null)Destroy(beamMesh);if(arcMesh!=null)Destroy(arcMesh);}
    }
}
