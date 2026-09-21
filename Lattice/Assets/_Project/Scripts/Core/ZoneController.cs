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
        public BodyForm Form=>Kind switch{ZoneKind.GroundSafe=>BodyForm.Natural,ZoneKind.GroundCombat=>BodyForm.Shaped,ZoneKind.SpaceSafe=>BodyForm.CivilFlight,_=>BodyForm.Flight};
        public event System.Action Changed;
        void Awake(){Current=this;Kind=definition.kind;}
        void Start(){Apply();}
        public void SafePocket(bool inside){Kind=inside?ZoneKind.GroundSafe:definition.kind;Apply();}
        void Apply()
        {
            GameServices.Current.Input.SetFlight(Flight);AudioManager.Zone(Flight);
            RenderSettings.skybox=definition.skybox;if(Camera.main!=null)Camera.main.clearFlags=definition.skybox!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;
            Changed?.Invoke();
        }
        void OnDestroy(){if(Current==this)Current=null;}
    }
}
