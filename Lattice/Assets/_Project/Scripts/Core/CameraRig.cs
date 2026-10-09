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
        Bounds? encounterBounds,sceneInterestBounds;
        Object sceneInterestOwner;
        Rect sceneInterestViewport;
        int sceneInterestFrame;
        Vector3 viewOffset;
        bool HasSceneInterest=>sceneInterestOwner!=null&&sceneInterestBounds.HasValue&&Time.frameCount-sceneInterestFrame<=1;
        // An opt-in, one-frame scene lease. Combat framing keeps priority;
        // disabled/destroyed/stale callers cannot retain the camera.
        public void FrameSceneInterest(Object owner,Bounds? bounds,Rect viewport)
        {
            if(owner==null)return;
            if(!bounds.HasValue)
            {
                if(sceneInterestOwner==owner)ReleaseSceneInterest();
                return;
            }
            if(viewport.width<=.1f||viewport.height<=.1f||viewport.xMin<0||viewport.yMin<0||viewport.xMax>1||viewport.yMax>1)return;
            sceneInterestOwner=owner;sceneInterestBounds=bounds;sceneInterestViewport=viewport;sceneInterestFrame=Time.frameCount;
            if(!encounterBounds.HasValue)returning=false;
        }
        void ReleaseSceneInterest()
        {
            bool wasFramed=sceneInterestBounds.HasValue;
            sceneInterestBounds=null;sceneInterestOwner=null;
            if(wasFramed&&!encounterBounds.HasValue)BeginReturn();
        }
        void BeginReturn()
        {
            if(!initialized||target==null||profile==null)return;
            returning=true;returnAge=0;returnDistance=cameraDistance;
            // Preserve the actual displayed centre when the safe viewport had
            // shifted the camera below the story geometry.
            center-=viewOffset;viewOffset=Vector3.zero;
            returnOffset=center-(target.position+velocity*profile.lookAhead);
        }
        float cameraDistance;
        Vector3 returnOffset;
        float returnDistance,returnAge;
        bool returning;
        // Combat supplies geometry through this generic bridge; Core owns no actors.
        public void FrameEncounter(Bounds? bounds)
        {
            if(bounds.HasValue)returning=false;
            else if(encounterBounds.HasValue&&!HasSceneInterest)BeginReturn();
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
            bool preserve=initialized&&follow!=null&&profile==settings&&(encounterBounds.HasValue||returning||HasSceneInterest)&&
                ZoneController.Current!=null&&ZoneController.Current.Flight;
            target=follow;profile=settings;
            if(preserve){lastTarget=follow.position;velocity=Vector3.zero;}
            else{initialized=false;encounterBounds=null;returning=false;viewOffset=Vector3.zero;}
        }
        void LateUpdate()
        {
            if(target==null||profile==null)return;
            bool isFlight=ZoneController.Current!=null&&ZoneController.Current.Flight;
            if(sceneInterestBounds.HasValue&&!HasSceneInterest)ReleaseSceneInterest();
            bool sceneFraming=isFlight&&!encounterBounds.HasValue&&HasSceneInterest;
            Bounds? framedBounds=encounterBounds??(sceneFraming?sceneInterestBounds:null);
            var active=isFlight?flight:ground;ground.Priority=isFlight?0:20;flight.Priority=isFlight?20:0;
            if(!initialized){center=lastTarget=target.position;velocity=Vector3.zero;cameraDistance=profile.distance;initialized=true;}
            if(!isFlight)returning=false;
            float dt=(returning||sceneFraming)&&GameTime.Paused?0:Time.unscaledDeltaTime;
            velocity=Vector3.Lerp(velocity,(target.position-lastTarget)/Mathf.Max(.001f,dt),1-Mathf.Exp(-9.75f*dt));lastTarget=target.position;
            Vector3 desired=isFlight&&framedBounds.HasValue?framedBounds.Value.center:
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
            if(isFlight&&framedBounds.HasValue)
            {
                var bounds=framedBounds.Value;
                var inverse=Quaternion.Inverse(rotation);
                float vertical=Mathf.Tan(profile.fov*.5f*Mathf.Deg2Rad)*.76f;
                float horizontal=vertical*(Camera.main!=null?Camera.main.aspect:16f/9);
                for(int corner=0;corner<8;corner++)
                {
                    var sign=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
                    var point=inverse*(bounds.center+Vector3.Scale(bounds.extents,sign)-center);
                    if(sceneFraming)
                    {
                        float tan=Mathf.Tan(profile.fov*.5f*Mathf.Deg2Rad),aspect=Camera.main!=null?Camera.main.aspect:16f/9;
                        float left=tan*aspect*(2*sceneInterestViewport.xMin-1),right=tan*aspect*(2*sceneInterestViewport.xMax-1);
                        float bottom=tan*(2*sceneInterestViewport.yMin-1),top=tan*(2*sceneInterestViewport.yMax-1);
                        float midX=(left+right)*.5f,midY=(bottom+top)*.5f;
                        required=Mathf.Max(required,Mathf.Max((point.x-right*point.z)/(right-midX),(-point.x+left*point.z)/(midX-left)));
                        required=Mathf.Max(required,Mathf.Max((point.y-top*point.z)/(top-midY),(-point.y+bottom*point.z)/(midY-bottom)));
                    }
                    else required=Mathf.Max(required,Mathf.Max(Mathf.Abs(point.x)/horizontal-point.z,Mathf.Abs(point.y)/vertical-point.z));
                }
            }
            if(isFlight&&returning)
            {
                required=Mathf.Lerp(returnDistance,required,returnBlend);
                // Keep pause protection through the filter's settling tail.
                if(returnAge>=2)returning=false;
            }
            cameraDistance=isFlight?Mathf.Lerp(cameraDistance,required,1-Mathf.Exp(-6*dt)):profile.distance;
            Vector3 requestedOffset=Vector3.zero;
            if(sceneFraming)
            {
                float tan=Mathf.Tan(profile.fov*.5f*Mathf.Deg2Rad),aspect=Camera.main!=null?Camera.main.aspect:16f/9;
                requestedOffset=rotation*new Vector3(tan*aspect*(sceneInterestViewport.xMin+sceneInterestViewport.xMax-1),tan*(sceneInterestViewport.yMin+sceneInterestViewport.yMax-1),0)*cameraDistance;
            }
            viewOffset=Vector3.Lerp(viewOffset,requestedOffset,1-Mathf.Exp(-10*dt));
            active.transform.SetPositionAndRotation(center-viewOffset-rotation*Vector3.forward*cameraDistance,rotation);
            var lens=active.Lens;lens.FieldOfView=profile.fov;lens.NearClipPlane=.1f;lens.FarClipPlane=1600;active.Lens=lens;
        }
    }
}
