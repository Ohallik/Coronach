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
        // A ground fight or a conversation keeps the fixed angle and only moves what it looks at and how far back it stands. A one-frame lease, as above;
        // a conversation outranks a fight. Nothing moves until someone in frame would leave their part of the screen, so a close fight or a one-line talk stays where it is.
        Bounds groundSubject;Rect groundSubjectArea,groundHeroArea;
        float groundFarthest,framedDistance=-1;
        int groundLease=-10,groundRank;
        bool GroundFramed=>Time.frameCount-groundLease<=1;
        public void FrameGround(Bounds subject,Rect subjectArea,Rect heroArea,float farthest,int rank=0)
        {
            if(groundLease==Time.frameCount&&rank<groundRank)return;
            groundSubject=subject;groundSubjectArea=subjectArea;groundHeroArea=heroArea;groundFarthest=farthest;groundRank=rank;groundLease=Time.frameCount;
        }
        /// <summary>What a set of points asks of the camera, in the camera's own axes: where it may stand for all of them to sit inside an area of the screen.</summary>
        struct GroundReach
        {
            readonly float left,right,bottom,top;float highX,lowX,highY,lowY;
            public GroundReach(Rect area,float tan,float aspect)
            {
                left=tan*aspect*(2*area.xMin-1);right=tan*aspect*(2*area.xMax-1);bottom=tan*(2*area.yMin-1);top=tan*(2*area.yMax-1);
                highX=highY=float.NegativeInfinity;lowX=lowY=float.PositiveInfinity;
            }
            public void Add(Vector3 point)
            {
                highX=Mathf.Max(highX,point.x-right*point.z);lowX=Mathf.Min(lowX,point.x-left*point.z);
                highY=Mathf.Max(highY,point.y-top*point.z);lowY=Mathf.Min(lowY,point.y-bottom*point.z);
            }
            /// <summary>The nearest camera depth at which this set and another can both fit.</summary>
            public float Fit(in GroundReach other)=>Mathf.Min((other.lowX-highX)/(right-other.left),(other.lowY-highY)/(top-other.bottom));
            public float MinX(float depth)=>highX+right*depth;
            public float MaxX(float depth)=>lowX+left*depth;
            public float MinY(float depth)=>highY+top*depth;
            public float MaxY(float depth)=>lowY+bottom*depth;
        }
        void MeasureGround(Quaternion inverse,out GroundReach hero,out GroundReach subject)
        {
            float tan=Mathf.Tan(profile.fov*.5f*Mathf.Deg2Rad),aspect=Camera.main!=null?Camera.main.aspect:16f/9;
            hero=new GroundReach(groundHeroArea,tan,aspect);subject=new GroundReach(groundSubjectArea,tan,aspect);
            hero.Add(inverse*target.position);hero.Add(inverse*(target.position+Vector3.up*1.9f));
            for(int corner=0;corner<8;corner++)
            {
                var sign=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
                subject.Add(inverse*(groundSubject.center+Vector3.Scale(groundSubject.extents,sign)));
            }
        }
        /// <summary>The view stays where it is if it can; if both can't fit, the hero's part of the screen wins and the subject gets as near as that allows.</summary>
        static float GroundSlide(float rest,float heroMin,float heroMax,float subjectMin,float subjectMax)
        {
            float low=Mathf.Max(heroMin,subjectMin),high=Mathf.Min(heroMax,subjectMax);
            return low<=high?Mathf.Clamp(rest,low,high):Mathf.Clamp((subjectMin+subjectMax)*.5f,heroMin,heroMax);
        }
        float cameraDistance,groundDistance=-1;
        static readonly RaycastHit[] Occluders=new RaycastHit[8];
        /// <summary>Closest the ground camera comes when a terrain rim blocks the party (D198).</summary>
        public const float NearestGroundDistance=6;
        /// <summary>Closer than that only where the lens at that distance still can't see the hero.</summary>
        const float LastGroundDistance=4;
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
        float tremorAge=1,tremorLength=1,tremorAmplitude;
        /// <summary>A brief deck tremor (the station shifting under the opening scene): a presentation offset that decays
        /// over its length and never moves the gameplay framing target.</summary>
        public void Tremor(float seconds,float amplitude){tremorLength=Mathf.Max(.05f,seconds);tremorAmplitude=Mathf.Max(0,amplitude);tremorAge=0;}
        public bool Trembling=>tremorAge<tremorLength;
        public void Apply(Transform follow,CameraProfile settings)
        {
            bool preserve=initialized&&follow!=null&&profile==settings&&(encounterBounds.HasValue||returning||HasSceneInterest)&&
                ZoneController.Current!=null&&ZoneController.Current.Flight;
            target=follow;profile=settings;
            if(preserve){lastTarget=follow.position;velocity=Vector3.zero;}
            else{initialized=false;encounterBounds=null;returning=false;viewOffset=Vector3.zero;framedDistance=-1;}
        }
        /// <summary>The fixed ground camera keeps its angle, but where a terrain rim (Sorrel's excavation, Hushwell's
        /// walls) stands between it and the party it slides in along its own view line until the party is in sight,
        /// quickly in and gently back out. Station interiors have no terrain and keep their framing exactly.</summary>
        float GroundDistance(Quaternion rotation,float dt)
        {
            float wanted=Mathf.Max(profile.distance,framedDistance),clear=wanted;var back=-(rotation*Vector3.forward);
            var zone=ZoneController.Current;
            if(zone!=null&&zone.definition!=null&&zone.definition.cameraClearsScenery)
            {
                // What stands between the camera and the hero is what counts. A slid view looks away from the hero, and its own axis can start under the floor
                // and come up through a crate that hides nobody: clearing on that dived the camera to its nearest in the middle of a fight.
                var from=viewOffset.sqrMagnitude>.01f&&target!=null?target.position:center-viewOffset;
                int count=Physics.SphereCastNonAlloc(from+Vector3.up*1.2f,.4f,back,Occluders,wanted,~0,QueryTriggerInteraction.Ignore);
                for(int i=0;i<count;i++)if(Occluders[i].distance>0&&Scenery(Occluders[i].collider))
                    clear=Mathf.Min(clear,Mathf.Max(NearestGroundDistance,Occluders[i].distance-.6f));
                // At its nearest the lens can still stand just above a rim, with the lip between it and the hero (twice on Sorrel's south rim). A little further in it goes
                // under the plateau, where one-sided terrain draws nothing and he's in plain view.
                if(clear<=NearestGroundDistance&&target!=null)
                    for(var chest=target.position+Vector3.up*1.2f;clear>LastGroundDistance&&!Sees(center-viewOffset+back*clear,chest);)clear-=.5f;
            }
            if(groundDistance<0||dt<=0&&groundDistance>clear)groundDistance=clear;
            else groundDistance=Mathf.Lerp(groundDistance,clear,1-Mathf.Exp(-(clear<groundDistance?14:2.5f)*dt));
            return groundDistance;
        }
        static bool Sees(Vector3 eye,Vector3 chest)
        {
            var line=chest-eye;float length=line.magnitude;if(length<.31f)return true;
            int count=Physics.RaycastNonAlloc(eye,line/length,Occluders,length-.3f,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(Scenery(Occluders[i].collider))return false;
            return true;
        }
        /// <summary>Visible, static scenery: not a body, not an invisible boundary.</summary>
        public static bool Scenery(Collider collider)=>!(collider is CharacterController)&&collider.attachedRigidbody==null&&
            collider.TryGetComponent<Renderer>(out var view)&&view.enabled&&collider.GetComponentInParent<CharacterController>()==null;
        public float GroundCameraDistance=>groundDistance<0?profile!=null?profile.distance:0:groundDistance;
        void LateUpdate()
        {
            if(target==null||profile==null)return;
            bool isFlight=ZoneController.Current!=null&&ZoneController.Current.Flight;
            if(sceneInterestBounds.HasValue&&!HasSceneInterest)ReleaseSceneInterest();
            bool sceneFraming=isFlight&&!encounterBounds.HasValue&&HasSceneInterest;
            bool groundFramed=!isFlight&&GroundFramed;
            Bounds? framedBounds=encounterBounds??(sceneFraming?sceneInterestBounds:null);
            var active=isFlight?flight:ground;ground.Priority=isFlight?0:20;flight.Priority=isFlight?20:0;
            if(!initialized){center=lastTarget=target.position;velocity=Vector3.zero;cameraDistance=profile.distance;initialized=true;}
            if(!isFlight)returning=false;
            float dt=(returning||sceneFraming||groundFramed)&&GameTime.Paused?0:Time.unscaledDeltaTime;
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
            GroundReach groundHero=default,groundSubjectReach=default;var fromWorld=Quaternion.Inverse(rotation);
            if(!isFlight)
            {
                float wanted=profile.distance;
                if(groundFramed)
                {
                    MeasureGround(fromWorld,out groundHero,out groundSubjectReach);
                    float depth=Mathf.Min(groundHero.Fit(groundHero),Mathf.Min(groundHero.Fit(groundSubjectReach),Mathf.Min(groundSubjectReach.Fit(groundHero),groundSubjectReach.Fit(groundSubjectReach))));
                    wanted=Mathf.Clamp((fromWorld*center).z-depth,profile.distance,Mathf.Max(profile.distance,groundFarthest));
                }
                framedDistance=framedDistance<0?wanted:Mathf.Lerp(framedDistance,wanted,1-Mathf.Exp(-2.2f*dt));
            }
            cameraDistance=isFlight?Mathf.Lerp(cameraDistance,required,1-Mathf.Exp(-6*dt)):GroundDistance(rotation,dt);
            Vector3 requestedOffset=Vector3.zero;
            if(groundFramed)
            {
                // At the distance the camera really has, which a rim of terrain can have pulled in: the view moves only as far as someone in frame needs.
                var anchor=fromWorld*center;float depth=anchor.z-cameraDistance;
                float x=GroundSlide(anchor.x,groundHero.MinX(depth),groundHero.MaxX(depth),groundSubjectReach.MinX(depth),groundSubjectReach.MaxX(depth));
                float y=GroundSlide(anchor.y,groundHero.MinY(depth),groundHero.MaxY(depth),groundSubjectReach.MinY(depth),groundSubjectReach.MaxY(depth));
                requestedOffset=rotation*new Vector3(anchor.x-x,anchor.y-y,0);
            }
            if(sceneFraming)
            {
                float tan=Mathf.Tan(profile.fov*.5f*Mathf.Deg2Rad),aspect=Camera.main!=null?Camera.main.aspect:16f/9;
                requestedOffset=rotation*new Vector3(tan*aspect*(sceneInterestViewport.xMin+sceneInterestViewport.xMax-1),tan*(sceneInterestViewport.yMin+sceneInterestViewport.yMax-1),0)*cameraDistance;
            }
            viewOffset=Vector3.Lerp(viewOffset,requestedOffset,1-Mathf.Exp(-(isFlight?10:3.5f)*dt));
            Vector3 tremor=Vector3.zero;
            if(tremorAge<tremorLength)
            {
                tremorAge+=Time.unscaledDeltaTime;float fade=1-Mathf.Clamp01(tremorAge/tremorLength),t=Time.unscaledTime*23;
                tremor=new Vector3(Mathf.PerlinNoise(t,.3f)-.5f,Mathf.PerlinNoise(.7f,t)-.5f,0)*2*tremorAmplitude*fade*fade;
            }
            active.transform.SetPositionAndRotation(center-viewOffset-rotation*Vector3.forward*cameraDistance+rotation*tremor,rotation);
            var lens=active.Lens;lens.FieldOfView=profile.fov;lens.NearClipPlane=.1f;lens.FarClipPlane=1600;active.Lens=lens;
        }
    }
}
