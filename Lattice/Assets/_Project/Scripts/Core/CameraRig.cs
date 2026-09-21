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
        void Awake()
        {
            var camera=Camera.main;
            if(camera!=null&&!camera.TryGetComponent<CinemachineBrain>(out _))camera.gameObject.AddComponent<CinemachineBrain>();
            ground=new GameObject("GroundFollow",typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            flight=new GameObject("FlightFollow",typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            ground.transform.SetParent(transform);flight.transform.SetParent(transform);
        }
        public void Apply(Transform follow,CameraProfile settings)
        {target=follow;profile=settings;initialized=false;}
        void LateUpdate()
        {
            if(target==null||profile==null)return;
            bool isFlight=ZoneController.Current!=null&&ZoneController.Current.Flight;
            var active=isFlight?flight:ground;ground.Priority=isFlight?0:20;flight.Priority=isFlight?20:0;
            if(!initialized){center=lastTarget=target.position;initialized=true;}
            velocity=Vector3.Lerp(velocity,(target.position-lastTarget)/Mathf.Max(.001f,Time.unscaledDeltaTime),.15f);lastTarget=target.position;
            Vector3 desired=target.position+(isFlight?velocity*profile.lookAhead:Vector3.zero);
            if((desired-center).magnitude>profile.deadZone)center=Vector3.Lerp(center,desired,1-Mathf.Exp(-10*Time.unscaledDeltaTime));
            var rotation=Quaternion.Euler(profile.pitch,profile.yaw,0);
            active.transform.SetPositionAndRotation(center-rotation*Vector3.forward*(profile.distance+(isFlight?velocity.magnitude*profile.speedZoom:0)),rotation);
            var lens=active.Lens;lens.FieldOfView=profile.fov;lens.NearClipPlane=.1f;lens.FarClipPlane=1600;active.Lens=lens;
        }
    }
}
