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
        Bounds groundSubject,groundFirst,groundBeside;Rect groundSubjectArea,groundHeroArea,groundBesideArea,groundFirstArea;
        bool groundHasFirst,groundHasBeside,groundSubjectOptional;
        /// <summary>Where the hero stays when it's only the partner asking: he isn't pushed to the edge of the screen for her.</summary>
        static readonly Rect BesideHeroArea=Rect.MinMaxRect(.25f,.3f,.75f,.76f);
        /// <summary>How quickly a framing lets go: it draws back at 2.2 a second, and on the way in at that speed the view breathed with every step a partner took.</summary>
        const float FramingLetsGo=.9f,BesideGivesUp=8,SubjectGivesUp=8;
        float groundFarthest,framedDistance=-1;
        int groundLease=-10,groundRank;
        bool GroundFramed=>Time.frameCount-groundLease<=1;
        /// <param name="first">What the hero is sent at when it isn't the subject itself (an organ the bar says to break): it stays in frame before the subject does.</param>
        /// <param name="beside">Who else is worth seeing, the partner, last of all: she gets what room is left, up to the farthest the view may go, and never at anyone else's cost.</param>
        /// <param name="firstArea">Where <paramref name="first"/> stays, when it is held to less of the screen than the subject (clear of the HUD's panels); unset, the subject's.</param>
        /// <param name="subjectOptional">The subject draws the view back only as far as it fits by <paramref name="farthest"/>, and past that has what room is left, as the partner does: a
        /// boss's body bigger than any view the party reads in. Unset, the view goes to <paramref name="farthest"/> for it whatever it leaves of it.</param>
        public void FrameGround(Bounds subject,Rect subjectArea,Rect heroArea,float farthest,int rank=0,Bounds? first=null,Bounds? beside=null,Rect besideArea=default,Rect firstArea=default,bool subjectOptional=false)
        {
            if(groundLease==Time.frameCount&&rank<groundRank)return;
            groundSubject=subject;groundSubjectArea=subjectArea;groundHeroArea=heroArea;groundFarthest=farthest;groundRank=rank;groundLease=Time.frameCount;
            groundHasFirst=first.HasValue;if(first.HasValue)groundFirst=first.Value;groundFirstArea=firstArea.width>0&&firstArea.height>0?firstArea:subjectArea;
            groundHasBeside=beside.HasValue;if(beside.HasValue){groundBeside=beside.Value;groundBesideArea=besideArea;}
            groundSubjectOptional=subjectOptional;
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
        void MeasureGround(Quaternion inverse,out GroundReach hero,out GroundReach subject,out GroundReach first,out GroundReach beside,out GroundReach easy)
        {
            float tan=Mathf.Tan(profile.fov*.5f*Mathf.Deg2Rad),aspect=Camera.main!=null?Camera.main.aspect:16f/9;
            // Whoever frames him (a fight, a talk, a room's own moment), a hero in a side third stays under the corner panels.
            var heroArea=groundHeroArea;var seen=Camera.main!=null?Camera.main.WorldToViewportPoint(target.position+Vector3.up*.95f):Vector3.zero;
            float side=heroAtSide?PanelSide+.05f:PanelSide;heroAtSide=seen.z>0&&(seen.x<side||seen.x>1-side);
            if(heroAtSide)heroArea.yMax=Mathf.Max(heroArea.yMin+.2f,Mathf.Min(heroArea.yMax,PanelFoot));
            hero=new GroundReach(heroArea,tan,aspect);subject=new GroundReach(groundSubjectArea,tan,aspect);first=new GroundReach(groundFirstArea,tan,aspect);
            beside=new GroundReach(groundBesideArea,tan,aspect);easy=new GroundReach(BesideHeroArea,tan,aspect);
            hero.Add(inverse*target.position);hero.Add(inverse*(target.position+Vector3.up*1.9f));
            easy.Add(inverse*target.position);easy.Add(inverse*(target.position+Vector3.up*1.9f));
            for(int corner=0;corner<8;corner++)
            {
                var sign=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
                subject.Add(inverse*(groundSubject.center+Vector3.Scale(groundSubject.extents,sign)));
                if(groundHasFirst)first.Add(inverse*(groundFirst.center+Vector3.Scale(groundFirst.extents,sign)));
                if(groundHasBeside)beside.Add(inverse*(groundBeside.center+Vector3.Scale(groundBeside.extents,sign)));
            }
        }
        static float Needed(in GroundReach a,in GroundReach b)=>Mathf.Min(a.Fit(a),Mathf.Min(a.Fit(b),Mathf.Min(b.Fit(a),b.Fit(b))));
        /// <summary>What must stay in frame narrows where the view may stand; if the two can't share a view, the one already holding it wins and the other gets as near as that allows.</summary>
        static void Narrow(ref float low,ref float high,float min,float max)
        {
            float from=Mathf.Max(low,min),to=Mathf.Min(high,max);
            if(from<=to){low=from;high=to;}else low=high=Mathf.Clamp((min+max)*.5f,low,high);
        }
        float cameraDistance,groundDistance=-1;
        static readonly RaycastHit[] Occluders=new RaycastHit[8];
        /// <summary>D198's nearest, when a rim blocking the party slid the lens in: since the nov20 capture it doesn't slide at all (<see cref="GroundDistance"/>), and this is
        /// only the floor the old tests hold it above.</summary>
        public const float NearestGroundDistance=6;
        Vector3 returnOffset;
        float returnDistance,returnAge;
        bool returning;
        /// <summary>How long the flight view takes to come home after a fight: at a second it snapped in, straight after a whip out.</summary>
        const float ReturnSeconds=1.4f;
        /// <summary>The pull back as an encounter starts begins from nothing and is at its full 6 a second by a little over a second in: it used to go from 26 m to 100 in a
        /// third of a second as the first enemies appeared. By then the fight is framed, and a body moving through it has to be kept up with.</summary>
        const float PullEaseIn=5;
        /// <summary>How old the encounter is, and how much of its usual speed the view has: the middle of the view eases with the pull. On its own quick clock it reached the encounter's
        /// middle long before the pull had made room, and with a target 30 m up the screen the ship fell off the bottom for half a second.</summary>
        float pullAge,pullShare=1,lastRequired;
        /// <summary>A step in what the view has to hold mid-fight, in metres in one frame, past anything flying can ask of it: a new target, or the partner coming inside the range that frames her.
        /// It's a first target again and the ease starts over. At full speed the view went from 28 m to 80 in a fifth of a second.</summary>
        const float PullStep=6;
        /// <summary>The top speed of the pull back, and how it comes in with the fight still on: at 6 a second it went from 85 m to 40 in a third of a second, straight after a whip out, with the same
        /// enemies on screen. CantorCollarTests' 1.3 s to frame a Cantor from the ordinary view is what stops the pull back going any slower.</summary>
        const float MostPull=50,ComeIn=2.4f;
        /// <summary>With the fight still on, the view holds as far back as it last needed for this long, and then comes in at no more than this. Coming in the moment the bounds
        /// shrank, at 28 a second, it pumped: out for a far target, in as the ship closed on it, out again for the next (E2-gullet-10, three times a cluster in the Gullet).</summary>
        public const float ComeInHold=1.6f,MostComeIn=20;
        float holdAge;
        /// <summary>Where a flight fight's ships and target stand on the screen: in from the sides as they always were (.12), and over the boss bar's words and the skill row at the foot
        /// (.18, where it was .12 too). Taren's ship flew under the Cantor's name for its fight (nov20 s10b 141), and a ship between the lens and a boss sat on the hint bar.</summary>
        static readonly Rect EncounterView=Rect.MinMaxRect(.12f,.18f,.88f,.88f);
        /// <summary>The party panel and the objective card hang in the top corners: a hero in a side third of the screen is kept a body's depth under their foot (.715), in a fight's frame
        /// on foot (Combat's PartyController) and in flight. The Cantor's fight had Taren's ship under the party panel's corner.</summary>
        public const float PanelSide=.3f,PanelFoot=.69f;
        /// <summary>W9-picket-01: the foot of the HUD's left column as the HUD tells it each frame, a share of the frame's height up from its foot. It is <see cref="PanelFoot"/> unless
        /// a zone's plate hangs lower under the party panel (the Picket's alarm plate, to about .605 at 1080), and a frame the HUD says nothing it is that again. A flight fight keeps a
        /// ship in the left third under it: under .69 alone, the frame drawn back for the Flak Gate stood the lead's ship under the alarm plate's corner (nov26-r7-chain s45/179).
        /// Core can't read the HUD, so the HUD says it.</summary>
        public static float LeftFoot=>!footAsBefore&&Time.frameCount-leftFootFrame<=1?leftFoot:PanelFoot;
        static float leftFoot=PanelFoot;static int leftFootFrame=-10;
        /// <summary>For a test's before only (PicketPlateFootTests): a ship in either side third kept under <see cref="PanelFoot"/>, whatever the HUD says.</summary>
        static bool footAsBefore;
        /// <param name="foot">The column's real foot, as a share of the frame's height up from its foot: kept only where it is under <see cref="PanelFoot"/>.</param>
        public static void TellLeftFoot(float foot){leftFoot=Mathf.Min(PanelFoot,foot);leftFootFrame=Time.frameCount;}
        /// <summary>The column is gone (the HUD hidden or destroyed): the constant holds from this frame.</summary>
        public static void ForgetLeftFoot()=>leftFootFrame=-10;
        bool shipAtSide,heroAtSide,underColumn;
        /// <summary>The farthest a flight fight's frame stands back: a ship spans about 75 px of 1080 there. The Tollkeeper's carrier and its cage drew the view 140 to 300 m back,
        /// the ship a speck in its own fight (nov20 s65 010); a body bigger than this is shown as far as it reaches, the ship kept in the frame.</summary>
        public const float FlightFarthest=125;
        float encounterNeed;
        /// <summary>A composed view, not a fight (a quiet preview of an animal, a departure, a reveal drawn back to show a boss): framed in the middle of the screen as it was authored,
        /// at whatever distance it takes. The fight's frame (its foot over the boss bar, the ship kept under the corner panels, its 125 m) would have made the Cantor's quiet preview
        /// too small to study and held Mother of Doors' reveal from the valley's head at 123 m where it was authored at 314 (CantorCompositionTests, SorrelSecondSkyTests).</summary>
        static readonly Rect ComposedView=Rect.MinMaxRect(.12f,.12f,.88f,.88f);
        bool encounterComposed;
        // Combat supplies geometry through this generic bridge; Core owns no actors.
        public void FrameEncounter(Bounds? bounds,bool composed=false)
        {
            if(bounds.HasValue){returning=false;if(!encounterBounds.HasValue){pullAge=0;pullShare=0;holdAge=0;}}
            else if(encounterBounds.HasValue&&!HasSceneInterest)BeginReturn();
            encounterBounds=bounds;encounterComposed=bounds.HasValue&&composed;
        }
        void Awake()
        {
            var camera=Camera.main;
            if(camera!=null&&!camera.TryGetComponent<CinemachineBrain>(out _))camera.gameObject.AddComponent<CinemachineBrain>();
            // Ground and flight are two cameras, and the brain blended between them over two seconds by default. A berth changes form inside a zone, under a fade, with the party
            // set down somewhere else: the blend carried the lens from the shelf to the ship low over the hull, the deck filling the frame and the ship nowhere (nov20 s66 151, 152).
            if(camera!=null&&camera.TryGetComponent<CinemachineBrain>(out var brain))brain.DefaultBlend=new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut,0);
            ground=new GameObject("GroundFollow",typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            flight=new GameObject("FlightFollow",typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            ground.transform.SetParent(transform);flight.transform.SetParent(transform);
        }
        float tremorAge=1,tremorLength=1,tremorAmplitude,tremorClock;
        /// <summary>A brief deck tremor (the station shifting under the opening scene): a presentation offset that decays
        /// over its length and never moves the gameplay framing target. Its amplitude is metres at the profile's own distance, and it keeps that size on screen however far
        /// back the view stands. A lighter tremor doesn't cut a heavier one short: a hit's shudder used to end a boss's slam.</summary>
        public void Tremor(float seconds,float amplitude)
        {
            seconds=Mathf.Max(.05f,seconds);amplitude=Mathf.Max(0,amplitude);
            if(tremorAge<tremorLength){float fade=1-tremorAge/tremorLength;if(tremorAmplitude*fade*fade>amplitude)return;}
            tremorLength=seconds;tremorAmplitude=amplitude;tremorAge=0;
        }
        public bool Trembling=>tremorAge<tremorLength;
        struct Kick{public Vector2 along;public float share,age,life;}
        readonly Kick[] kicks=new Kick[4];int kickTurn;
        /// <summary>The most the view is ever moved off its framing, as a share of its height (about 22 px at 1080p), and how fast a kick rings.</summary>
        public const float MostShake=.02f,KickHz=14;
        /// <summary>Where the last frame's shake put the camera off its framing: in its own right and up as a share of the view's height, and in the world.</summary>
        public Vector2 ShakeShare{get;private set;}
        public Vector3 ShakeOffset{get;private set;}
        Vector3 posedAt;Quaternion posedRotation;bool posed;
        /// <summary>A hit's kick: the view jumps along the blow and rings back over its length. Like a tremor it is laid over the framing and moves only in the camera's own plane:
        /// the angle and the lens never change. Its size is a share of the view's height, so it reads the same at 19 m on foot and at 90 m in flight.</summary>
        public void Impulse(Vector3 worldDirection,float share,float seconds=.14f)
        {
            if(share<=0||seconds<=0||profile==null)return;
            var local=Quaternion.Inverse(Quaternion.Euler(profile.pitch,profile.yaw,0))*worldDirection;var along=new Vector2(local.x,local.y);
            // Straight down the lens there's nothing to kick along: a diagonal, to one side and then the other.
            if(along.sqrMagnitude<.0001f)along=(kickTurn++&1)==0?new Vector2(.6f,.8f):new Vector2(-.6f,.8f);
            int slot=0;float least=float.PositiveInfinity;
            for(int i=0;i<kicks.Length;i++){float left=Left(kicks[i]);if(left<least){least=left;slot=i;}}
            // Four already ringing harder than this one.
            if(least>share)return;
            kicks[slot]=new Kick{along=along.normalized,share=share,age=0,life=seconds};
        }
        static float Left(in Kick kick){if(kick.age>=kick.life)return 0;float fade=1-kick.age/kick.life;return kick.share*fade*fade;}
        /// <summary>Kicks and the tremor together, as one offset in the camera's plane. Their clocks are real time, so a kick rings through the hit-stop it comes with, and they
        /// hold for a pause: the tremor used to run on behind the menu.</summary>
        Vector3 Shake(Quaternion rotation)
        {
            float dt=GameTime.Paused?0:Time.unscaledDeltaTime;var sum=Vector2.zero;
            for(int i=0;i<kicks.Length;i++)
            {
                if(kicks[i].age>=kicks[i].life)continue;
                float fade=1-kicks[i].age/kicks[i].life;
                sum+=kicks[i].along*(kicks[i].share*fade*fade*Mathf.Cos(kicks[i].age*KickHz*2*Mathf.PI));kicks[i].age+=dt;
            }
            float view=2*cameraDistance*Mathf.Tan(profile.fov*.5f*Mathf.Deg2Rad);var metres=sum*view;
            if(tremorAge<tremorLength)
            {
                float fade=1-Mathf.Clamp01(tremorAge/tremorLength),t=tremorClock*23;
                metres+=new Vector2(Mathf.PerlinNoise(t,.3f)-.5f,Mathf.PerlinNoise(.7f,t)-.5f)*(2*tremorAmplitude*fade*fade*cameraDistance/Mathf.Max(1,profile.distance));
                tremorAge+=dt;tremorClock+=dt;
            }
            metres=Vector2.ClampMagnitude(metres*ImpactSettings.ShakeScale,MostShake*view);
            ShakeShare=view>0?metres/view:Vector2.zero;ShakeOffset=rotation*new Vector3(metres.x,metres.y,0);
            return ShakeOffset;
        }
        public void Apply(Transform follow,CameraProfile settings)
        {
            bool preserve=initialized&&follow!=null&&profile==settings&&(encounterBounds.HasValue||returning||HasSceneInterest)&&
                ZoneController.Current!=null&&ZoneController.Current.Flight;
            target=follow;profile=settings;
            if(preserve){lastTarget=follow.position;velocity=Vector3.zero;}
            else{initialized=false;encounterBounds=null;returning=false;viewOffset=Vector3.zero;framedDistance=-1;lean=leanGoal=0;}
        }
        /// <summary>The fixed ground camera stands at its play distance or further back (a framed fight or talk), never nearer. It used to slide in along its own line wherever
        /// a rim stood between it and the hero (D198), as near as 4 m: in the Drowned Waystation's lower gallery that put the lens inside the gallery's wall, with the sky
        /// behind the zone's walls in a third of the frame and the heroes from the thighs down at four times their size (nov20 s26 301, 306). Where a rim hides the hero it
        /// now leans (<see cref="LeanFor"/>), and what nothing in reach of the lean clears is drawn through (Combat's HeroThrough).</summary>
        float GroundDistance(float dt)
        {
            float wanted=Mathf.Max(profile.distance,framedDistance);
            if(groundDistance<0||dt<=0&&groundDistance>wanted)groundDistance=wanted;
            else groundDistance=Mathf.Lerp(groundDistance,wanted,1-Mathf.Exp(-(wanted<groundDistance?14:2.5f)*dt));
            return Mathf.Max(profile.distance,groundDistance);
        }
        /// <summary>The lean: how far over its usual pitch the ground camera tips, about the point it looks at and at the distance it stands, so its line to the hero clears a
        /// rim on its own side. Only the pitch: it never turns (D119, D212), and nothing is nearer than it was. In five-degree steps to <see cref="LeanMost"/>; quick over and
        /// gently back once the lower angle is clear again. The last step is straight down (89 degrees): at the foot of Sorrel's south rim and the Braided River's banks nothing
        /// short of it clears the bank behind him, where D198 slid the lens to 4 m.</summary>
        public const float LeanMost=50,LeanStep=5;
        float lean,leanGoal;
        /// <summary>Degrees the ground view is leaning over its profile's pitch now.</summary>
        public float Lean=>lean;
        float LeanFor(float distance,float dt)
        {
            var zone=ZoneController.Current;
            if(target==null||zone==null||zone.definition==null||!zone.definition.cameraClearsScenery){leanGoal=0;}
            else
            {
                // His chest, as D198 asked: a rim that leaves only a head over it has hidden him.
                var look=center-viewOffset;var chest=target.position+Vector3.up*1.2f;float goal=0;bool seen=false;
                for(float tip=0;tip<=LeanMost+.01f;tip+=LeanStep)
                {
                    var eye=look-Quaternion.Euler(Mathf.Min(89,profile.pitch+tip),profile.yaw,0)*Vector3.forward*distance;
                    if(Sees(eye,chest)){goal=tip;seen=true;break;}
                }
                // Nothing in reach of the lean sees him (a roof over him, a pit with walls all round): the view stays as it is, and he is drawn through what hides him.
                leanGoal=seen?goal:0;
            }
            if(dt<=0)return lean;
            lean=Mathf.Lerp(lean,leanGoal,1-Mathf.Exp(-(leanGoal>lean?6:1.4f)*dt));
            if(Mathf.Abs(lean-leanGoal)<.02f)lean=leanGoal;
            return lean;
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
            if(!initialized){center=lastTarget=target.position;velocity=Vector3.zero;cameraDistance=profile.distance;initialized=true;posed=false;}
            // A hit-stop holds the view where it stood: nobody in frame moves, and a solver running on real time would drift in toward them and back. Only the kick moves it.
            if(posed&&GameTime.Stopped){active.transform.SetPositionAndRotation(posedAt+Shake(posedRotation),posedRotation);return;}
            if(!isFlight)returning=false;
            float dt=(returning||sceneFraming||groundFramed)&&GameTime.Paused?0:Time.unscaledDeltaTime;
            velocity=Vector3.Lerp(velocity,(target.position-lastTarget)/Mathf.Max(.001f,dt),1-Mathf.Exp(-9.75f*dt));lastTarget=target.position;
            Vector3 desired=isFlight&&framedBounds.HasValue?framedBounds.Value.center:
                target.position+(isFlight?velocity*profile.lookAhead:Vector3.zero);
            // A fight's frame that would need the view further back than the ships still read at holds there, and leans its middle toward the ship by what it can't show.
            bool encounterFrame=isFlight&&encounterBounds.HasValue&&!sceneFraming&&!encounterComposed;
            if(encounterFrame&&encounterNeed>FlightFarthest)desired=Vector3.Lerp(desired,target.position,Mathf.Clamp01((encounterNeed-FlightFarthest)/encounterNeed));
            float returnBlend=1;
            if(isFlight&&returning)
            {
                returnAge+=dt;
                returnBlend=Mathf.SmoothStep(0,1,Mathf.Clamp01(returnAge/ReturnSeconds));
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
                center+=offset*((distance-profile.deadZone)/distance)*(1-Mathf.Exp(-10*(isFlight&&encounterBounds.HasValue?pullShare:1)*dt));
            if(isFlight)lean=leanGoal=0;else LeanFor(GroundCameraDistance,dt);
            var rotation=Quaternion.Euler(Mathf.Min(89,profile.pitch+lean),profile.yaw,0);
            float required=profile.distance+(isFlight?velocity.magnitude*profile.speedZoom:0);
            if(isFlight&&framedBounds.HasValue)
            {
                var bounds=framedBounds.Value;
                var inverse=Quaternion.Inverse(rotation);var view=sceneFraming?sceneInterestViewport:encounterComposed?ComposedView:EncounterView;
                float tan=Mathf.Tan(profile.fov*.5f*Mathf.Deg2Rad),aspect=Camera.main!=null?Camera.main.aspect:16f/9;
                float left=tan*aspect*(2*view.xMin-1),right=tan*aspect*(2*view.xMax-1),bottom=tan*(2*view.yMin-1),top=tan*(2*view.yMax-1);
                float midX=(left+right)*.5f,midY=(bottom+top)*.5f;
                for(int corner=0;corner<8;corner++)
                {
                    var sign=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
                    var point=inverse*(bounds.center+Vector3.Scale(bounds.extents,sign)-center);
                    required=Mathf.Max(required,Mathf.Max((point.x-right*point.z)/(right-midX),(-point.x+left*point.z)/(midX-left)));
                    required=Mathf.Max(required,Mathf.Max((point.y-top*point.z)/(top-midY),(-point.y+bottom*point.z)/(midY-bottom)));
                }
                // A fight's ship in a side third stays under the corner panels: the frame draws back that little more, rather than the ship flying under the party panel.
                // Held a little past the third once in it, or the drawing back that brings the ship inward would let go and come in again by turns.
                var seen=!sceneFraming&&Camera.main!=null?Camera.main.WorldToViewportPoint(target.position):Vector3.zero;
                float side=shipAtSide?PanelSide+.05f:PanelSide;shipAtSide=!encounterComposed&&seen.z>0&&(seen.x<side||seen.x>1-side);
                // Under a column that hangs lower than the corner panels (LeftFoot: the Picket's alarm plate) a ship counts once its wing is under it, not its middle only: at
                // s45/179's place its middle stood at .31 of the width and its wing under the plate. Where nothing hangs lower, this is never asked and the rule is as it was.
                float wing=!encounterComposed&&seen.z>0&&LeftFoot<PanelFoot?Camera.main.WorldToViewportPoint(target.position-Camera.main.transform.right*2.5f).x:1;
                underColumn=wing<(underColumn?PanelSide+.05f:PanelSide);
                if(shipAtSide||underColumn)
                {
                    // On the left under the column's real foot; on the right under the objective card's, as ever.
                    float under=tan*(2*(underColumn?LeftFoot:PanelFoot)-1);
                    for(int corner=0;corner<8;corner++)
                    {
                        var sign=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
                        var point=inverse*(target.position+Vector3.up*.8f+sign*2.5f-center);
                        if(under>midY)required=Mathf.Max(required,(point.y-under*point.z)/(under-midY));
                    }
                }
                if(encounterFrame){encounterNeed=required;required=Mathf.Min(required,FlightFarthest);}
            }
            else encounterNeed=0;
            if(isFlight&&returning)
            {
                required=Mathf.Lerp(returnDistance,required,returnBlend);
                // Keep pause protection through the filter's settling tail.
                if(returnAge>=2)returning=false;
            }
            GroundReach groundHero=default,groundSubjectReach=default,groundFirstReach=default,groundBesideReach=default,groundEasyReach=default;var fromWorld=Quaternion.Inverse(rotation);
            if(!isFlight)
            {
                float wanted=profile.distance;
                if(groundFramed)
                {
                    MeasureGround(fromWorld,out groundHero,out groundSubjectReach,out groundFirstReach,out groundBesideReach,out groundEasyReach);
                    float core=groundHasFirst?Needed(groundHero,groundFirstReach):Needed(groundHero,groundHero);
                    float depth=Mathf.Min(core,Needed(groundHero,groundSubjectReach));
                    if(groundHasFirst)depth=Mathf.Min(depth,Needed(groundFirstReach,groundSubjectReach));
                    float from=(fromWorld*center).z,limit=Mathf.Max(profile.distance,groundFarthest);
                    bool subjectHeld=!groundSubjectOptional||from-depth<=limit;
                    if(groundSubjectOptional)
                    {
                        // The hero and what he's fighting first; the body as far as it fits by the limit, and fading out of the ask past it rather than holding the view at the limit for good.
                        float held=Mathf.Clamp(from-core,profile.distance,limit),need=from-depth;
                        wanted=Mathf.Max(held,Mathf.Min(limit,Mathf.Lerp(need,held,Mathf.InverseLerp(limit,limit+SubjectGivesUp,need))));
                    }
                    else wanted=Mathf.Clamp(from-depth,profile.distance,limit);
                    if(groundHasBeside)
                    {
                        float withHer=Mathf.Min(subjectHeld?depth:core,Needed(groundEasyReach,groundBesideReach));
                        if(subjectHeld)withHer=Mathf.Min(withHer,Needed(groundSubjectReach,groundBesideReach));
                        if(groundHasFirst)withHer=Mathf.Min(withHer,Needed(groundFirstReach,groundBesideReach));
                        // If she fits. Past the limit the ask fades out, so the view doesn't draw back for someone it still can't show, or jump as she crosses the line.
                        float need=from-withHer;
                        wanted=Mathf.Max(wanted,Mathf.Min(limit,Mathf.Lerp(need,wanted,Mathf.InverseLerp(limit,limit+BesideGivesUp,need))));
                    }
                }
                framedDistance=framedDistance<0?wanted:Mathf.Lerp(framedDistance,wanted,1-Mathf.Exp(-(wanted<framedDistance?FramingLetsGo:2.2f)*dt));
            }
            float chase=6,most=1e5f;
            if(isFlight&&encounterBounds.HasValue)
            {
                pullAge+=dt;holdAge+=dt;if(required-lastRequired>PullStep+40*dt)pullAge=0;lastRequired=required;
                // Still wanted about this far back: the hold starts over. Half a metre, so the settling of the pull itself doesn't count as letting go.
                if(required>cameraDistance-.5f)holdAge=0;
                if(required>cameraDistance){chase=Mathf.Min(chase,PullEaseIn*pullAge);most=MostPull;pullShare=chase/6;}
                else if(holdAge<ComeInHold){chase=0;pullShare=1;}
                else{chase=ComeIn;most=MostComeIn;pullShare=1;}
            }
            cameraDistance=isFlight?Mathf.MoveTowards(cameraDistance,Mathf.Lerp(cameraDistance,required,1-Mathf.Exp(-chase*dt)),most*dt):GroundDistance(dt);
            Vector3 requestedOffset=Vector3.zero;
            if(groundFramed)
            {
                // At the distance the camera really has, which a rim of terrain can have pulled in: the view moves only as far as someone in frame needs.
                var anchor=fromWorld*center;float depth=anchor.z-cameraDistance;
                float lowX=groundHero.MinX(depth),highX=groundHero.MaxX(depth),lowY=groundHero.MinY(depth),highY=groundHero.MaxY(depth);
                if(groundHasFirst){Narrow(ref lowX,ref highX,groundFirstReach.MinX(depth),groundFirstReach.MaxX(depth));Narrow(ref lowY,ref highY,groundFirstReach.MinY(depth),groundFirstReach.MaxY(depth));}
                Narrow(ref lowX,ref highX,groundSubjectReach.MinX(depth),groundSubjectReach.MaxX(depth));Narrow(ref lowY,ref highY,groundSubjectReach.MinY(depth),groundSubjectReach.MaxY(depth));
                if(groundHasBeside)
                {
                    Narrow(ref lowX,ref highX,groundEasyReach.MinX(depth),groundEasyReach.MaxX(depth));Narrow(ref lowY,ref highY,groundEasyReach.MinY(depth),groundEasyReach.MaxY(depth));
                    Narrow(ref lowX,ref highX,groundBesideReach.MinX(depth),groundBesideReach.MaxX(depth));Narrow(ref lowY,ref highY,groundBesideReach.MinY(depth),groundBesideReach.MaxY(depth));
                }
                float x=Mathf.Clamp(anchor.x,lowX,highX),y=Mathf.Clamp(anchor.y,lowY,highY);
                requestedOffset=rotation*new Vector3(anchor.x-x,anchor.y-y,0);
            }
            if(isFlight&&framedBounds.HasValue)
            {
                float tan=Mathf.Tan(profile.fov*.5f*Mathf.Deg2Rad),aspect=Camera.main!=null?Camera.main.aspect:16f/9;var view=sceneFraming?sceneInterestViewport:encounterComposed?ComposedView:EncounterView;
                requestedOffset=rotation*new Vector3(tan*aspect*(view.xMin+view.xMax-1),tan*(view.yMin+view.yMax-1),0)*cameraDistance;
            }
            viewOffset=Vector3.Lerp(viewOffset,requestedOffset,1-Mathf.Exp(-(isFlight?10:3.5f)*dt));
            posedAt=center-viewOffset-rotation*Vector3.forward*cameraDistance;posedRotation=rotation;posed=true;
            active.transform.SetPositionAndRotation(posedAt+Shake(rotation),rotation);
            var lens=active.Lens;lens.FieldOfView=profile.fov;lens.NearClipPlane=.1f;lens.FarClipPlane=1600;active.Lens=lens;
        }
    }
}
