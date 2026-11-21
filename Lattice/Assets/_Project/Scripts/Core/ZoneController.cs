using Lattice.Data;
using UnityEngine;
namespace Lattice.Core
{
    public sealed class ZoneController:MonoBehaviour
    {
        public static ZoneController Current {get;private set;}
        public ZoneDef definition;
        public ZoneKind Kind {get;private set;}
        public bool Flight=>Kind==ZoneKind.SpaceCombat||Kind==ZoneKind.SpaceSafe;
        public bool Combat=>Kind==ZoneKind.GroundCombat||Kind==ZoneKind.SpaceCombat;
        /// <summary>The party has stepped into a sheltered place in a combat zone, where it can't fight. (Not a fight staged in a safe zone: that is a combat place, <see cref="FightPocket"/>.)</summary>
        public bool InSafePocket=>definition!=null&&Kind!=definition.kind&&!Combat&&berth==null;
        public BodyForm Form=>Kind switch{ZoneKind.GroundSafe=>BodyForm.Natural,ZoneKind.GroundCombat=>BodyForm.Shaped,ZoneKind.SpaceSafe=>BodyForm.CivilFlight,_=>BodyForm.Flight};
        public event System.Action Changed;
        void Awake(){Current=this;Kind=definition.kind;}
        void Start(){Apply();}
        public void SafePocket(bool inside){Kind=inside?ZoneKind.GroundSafe:definition.kind;Apply();}
        /// <summary>The party has stepped into a room of a safe zone staged for a fight (World's FightPocket: chapter 5's freight hall): a combat place on foot there, the reverse of a safe pocket.</summary>
        public void FightPocket(bool inside){Kind=inside?ZoneKind.GroundCombat:definition.kind;Apply();}
        // ---------- chapter 10's berth (World's Berth; ch10-wiring 6.1): a part of the zone of the other form, entered and left by a prompt ----------
        /// <summary>Every berth in the scene, whether or not the party is on one: what tells a flier from a body on a shelf (<see cref="FlightAt"/>).</summary>
        public static readonly System.Collections.Generic.List<IBerth> Berths=new();
        IBerth berth;CameraProfile berthCamera;float berthPlane;
        /// <summary>The party stands on a berth: the zone's Kind is the berth's.</summary>
        public bool Berthed=>berth!=null;
        public IBerth BerthOn=>berth;
        public string BerthId=>berth?.Id;
        public static IBerth FindBerth(string id){foreach(var b in Berths)if(b!=null&&b.Id==id)return b;return null;}
        /// <summary>The zone has a berth: only then does a body's own form differ from the zone's.</summary>
        public static bool HasBerth=>Berths.Count>0;
        /// <summary>The camera the party is seen with: the zone's, or a berth's while it stands on one. Whatever steers by the camera (the stick, the replay driver) reads this, never the definition's.</summary>
        public CameraProfile CameraProfile=>berth!=null&&berthCamera!=null?berthCamera:definition.cameraProfile;
        /// <summary>The height ships fly at: the zone's plane, or a flight berth's beside a ground zone.</summary>
        public float FlightPlane=>berth!=null&&IsFlight(Kind)?berthPlane:definition.flightPlane;
        /// <summary>Whether a point is in flight: inside a berth, the berth's form; anywhere else the zone's own (its Kind, while nobody is berthed).</summary>
        public bool FlightAt(Vector3 point)
        {
            foreach(var b in Berths)if(b!=null&&b.Contains(point))return IsFlight(b.Kind);
            return berth!=null?IsFlight(definition.kind):Flight;
        }
        public static bool IsFlight(ZoneKind kind)=>kind==ZoneKind.SpaceCombat||kind==ZoneKind.SpaceSafe;
        /// <summary>The party has docked onto a berth (its form, camera and, in flight, plane), or with null has launched back into the zone's own form. Apply runs: input mode, music, form.</summary>
        public void Berth(IBerth on,ZoneKind kind=ZoneKind.GroundCombat,CameraProfile camera=null,float plane=1)
        {
            berth=on;berthCamera=on!=null?camera:null;berthPlane=plane;Kind=on!=null?kind:definition.kind;Apply();
        }
        void Apply()
        {
            GameServices.Current.Input.SetFlight(Flight);AudioManager.Zone(Flight,definition.ambientAudio,definition.ambientVolume);
            MusicDirector.SetLocation(definition.id,Combat);
            RenderSettings.skybox=definition.skybox;if(Camera.main!=null)Camera.main.clearFlags=definition.skybox!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;
            Changed?.Invoke();
        }
        void OnDestroy(){if(Current==this)Current=null;}
    }
}
