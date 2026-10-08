using Lattice.Data;
using Unity.Cinemachine;
using UnityEngine;
namespace Lattice.Core
{
    public sealed class CameraRig:MonoBehaviour
    {
        public Transform target;
        public CameraProfile profile;
        CinemachineCamera ground,flight;
        Vector3 center,lastTarget,velocity;
        bool initialized;
        Bounds? encounterBounds;
        float cameraDistance;
        Vector3 returnOffset;
        float returnDistance,returnAge;
        bool returning;
        // Combat supplies geometry through this generic bridge; Core owns no actors.
        public void FrameEncounter(Bounds? bounds)
        {
            if(bounds.HasValue)returning=false;
            else if(encounterBounds.HasValue&&initialized&&target!=null&&profile!=null)
            {
                returning=true;returnAge=0;returnDistance=cameraDistance;
                returnOffset=center-(target.position+velocity*profile.lookAhead);
            }
            encounterBounds=bounds;
        }
        void Awake()
        {
            var camera=Camera.main;
            if(camera!=null&&!camera.TryGetComponent<CinemachineBrain>(out _))camera.gameObject.AddComponent<CinemachineBrain>();
            ground=new GameObject("GroundFollow",typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            flight=new GameObject("FlightFollow",typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            ground.transform.SetParent(transform);flight.transform.SetParent(transform);
        }
        public void Apply(Transform follow,CameraProfile settings)
        {
            bool preserve=initialized&&follow!=null&&profile==settings&&(encounterBounds.HasValue||returning)&&
                ZoneController.Current!=null&&ZoneController.Current.Flight;
            target=follow;profile=settings;
            if(preserve){lastTarget=follow.position;velocity=Vector3.zero;}
            else{initialized=false;encounterBounds=null;returning=false;}
        }
        void LateUpdate()
        {
            if(target==null||profile==null)return;
            bool isFlight=ZoneController.Current!=null&&ZoneController.Current.Flight;
            var active=isFlight?flight:ground;ground.Priority=isFlight?0:20;flight.Priority=isFlight?20:0;
            if(!initialized){center=lastTarget=target.position;velocity=Vector3.zero;cameraDistance=profile.distance;initialized=true;}
            if(!isFlight)returning=false;
            float dt=returning&&GameTime.Paused?0:Time.unscaledDeltaTime;
            velocity=Vector3.Lerp(velocity,(target.position-lastTarget)/Mathf.Max(.001f,dt),1-Mathf.Exp(-9.75f*dt));lastTarget=target.position;
            Vector3 desired=isFlight&&encounterBounds.HasValue?encounterBounds.Value.center:
                target.position+(isFlight?velocity*profile.lookAhead:Vector3.zero);
            float returnBlend=1;
            if(isFlight&&returning)
            {
                returnAge+=dt;
                returnBlend=Mathf.SmoothStep(0,1,Mathf.Clamp01(returnAge));
                // Release the encounter offset relative to the moving hero.
                // A frozen world point would leave ordinary thrust off-screen.
                desired+=returnOffset*(1-returnBlend);
            }
            // Ease toward the nearest edge of the dead zone. Easing toward its
            // center crossed back inside the threshold every few walking frames,
            // repeatedly stopping the camera despite constant target velocity.
            Vector3 offset=desired-center;
            float distance=offset.magnitude;
            if(distance>profile.deadZone)
                center+=offset*((distance-profile.deadZone)/distance)*(1-Mathf.Exp(-10*dt));
            var rotation=Quaternion.Euler(profile.pitch,profile.yaw,0);
            float required=profile.distance+(isFlight?velocity.magnitude*profile.speedZoom:0);
            if(isFlight&&encounterBounds.HasValue)
            {
                var bounds=encounterBounds.Value;
                var inverse=Quaternion.Inverse(rotation);
                float vertical=Mathf.Tan(profile.fov*.5f*Mathf.Deg2Rad)*.76f;
                float horizontal=vertical*(Camera.main!=null?Camera.main.aspect:16f/9);
                for(int corner=0;corner<8;corner++)
                {
                    var sign=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
                    var point=inverse*(bounds.center+Vector3.Scale(bounds.extents,sign)-center);
                    required=Mathf.Max(required,Mathf.Max(Mathf.Abs(point.x)/horizontal-point.z,Mathf.Abs(point.y)/vertical-point.z));
                }
            }
            if(isFlight&&returning)
            {
                required=Mathf.Lerp(returnDistance,required,returnBlend);
                // Keep pause protection through the filter's settling tail.
                if(returnAge>=2)returning=false;
            }
            cameraDistance=isFlight?Mathf.Lerp(cameraDistance,required,1-Mathf.Exp(-6*dt)):profile.distance;
            active.transform.SetPositionAndRotation(center-rotation*Vector3.forward*cameraDistance,rotation);
            var lens=active.Lens;lens.FieldOfView=profile.fov;lens.NearClipPlane=.1f;lens.FarClipPlane=1600;active.Lens=lens;
        }
    }
}
