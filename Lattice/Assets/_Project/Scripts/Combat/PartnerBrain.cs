using Lattice.Core;
using UnityEngine;
using UnityEngine.AI;
namespace Lattice.Combat
{
    public sealed class PartnerBrain:MonoBehaviour
    {
        /// <summary>Set by the world (EncounterVolume.Usher, D294): where this hero should go because a fight's open gate is waiting for her alone. A fight behind an open
        /// gate begins when everybody is through it; the lead stops two steps inside, her place is three metres behind him and short of the line, and nothing begins.</summary>
        public static System.Func<CombatActor,CombatActor,Vector3?> ComeThrough;
        CombatActor actor;
        NavMeshPath path;
        Vector3[] corners;
        int corner;
        float nextPath;
        Health flankTarget;
        float flankSide;
        FlightSeparation flightSeparation;
        SerpentSegments departingSerpent;
        FlightObstacles flightObstacles;
        // Three seconds at least, and never less than what it was already waiting: swapping out and back used to shorten the six to nine seconds to three.
        void OnEnable(){nextSkill=Mathf.Max(nextSkill,GameTime.Now+3);corners=null;nextPath=0;flankTarget=null;flightSeparation=null;departingSerpent=null;flightObstacles=null;framed=null;nextFrameSearch=0;wayOutOf=rolledOutOf=null;}
        void Awake(){actor=GetComponent<CombatActor>();path=new NavMeshPath();}
        void Update()
        {
            var party=PartyController.Current;if(GameTime.Paused||party==null||party.Active==actor||!actor.Health.Alive||actor.Recovering||actor.motor==null||GameServices.Current.Input.Blocked)return;
            var active=party.Active;actor.target=active.target;
            // The lead's lock waits out a dive, but a buried rig is nothing to fight: take what stands open, or follow. Burrowed rather
            // than !Targetable: a collared Cantor can't be hurt either, and it is still what the partner flies at.
            if(actor.target!=null&&actor.target.TryGetComponent<BossController>(out var dive)&&dive.Burrowed)actor.target=GetComponent<PlayerBrain>().FindTarget();
            var camera=Camera.main;
            actor.target=Seen(camera,actor.target,out bool seen);
            var selectedCarrier=actor.target!=null?(actor.target.reactionOwner!=null?actor.target.reactionOwner:actor.target):null;
            var serpent=actor.flight&&selectedCarrier!=null&&selectedCarrier.Alive?selectedCarrier.GetComponent<SerpentSegments>():null;
            // Lethal resolution clears the lock before the full-size animal
            // leaves. Its visible body still needs room during regrouping.
            if(flightSeparation!=null&&Departing(flightSeparation.Body))departingSerpent=flightSeparation.Body;
            if(!actor.flight||!Departing(departingSerpent))departingSerpent=null;
            // Swapping enables a different companion brain with no previous
            // selection. Discover one nearby release from the existing collar
            // registry; 48 m includes the whole trailing body and local route.
            if(actor.flight&&departingSerpent==null)
            {
                float nearest=48*48;
                foreach(var collar in CantorCollar.All)
                {
                    var body=collar.GetComponent<SerpentSegments>();
                    float releaseDistance=(collar.transform.position-transform.position).sqrMagnitude;
                    if(releaseDistance<nearest&&Departing(body)){nearest=releaseDistance;departingSerpent=body;}
                }
            }
            var guidedBody=serpent!=null?serpent:departingSerpent;
            var otherBody=serpent!=null&&departingSerpent!=serpent?departingSerpent:null;
            if(guidedBody==null)flightSeparation=null;
            else if(flightSeparation==null||flightSeparation.Body!=guidedBody||!ReferenceEquals(flightSeparation.OtherBody,otherBody)||
                !ReferenceEquals(flightSeparation.DepartingBody,departingSerpent))
                flightSeparation=new FlightSeparation(guidedBody,HeroCollision.HullRadius(actor.character),otherBody);
            if(actor.State==Lattice.Data.ActorState.Stagger){actor.motor.Move(Vector2.zero,false,true);return;}
            // The guard hero as a companion (S6a): a brace is held through the hit it was raised for, then let go. And what answers the lead's trouble (a dome for
            // shots coming in, a flare for a lead nearly down) doesn't wait for a companion's turn.
            if(braceUntil>0){if(GameTime.Now<braceUntil){actor.Guard(true);actor.motor.Move(Vector2.zero,false,actor.flight);return;}braceUntil=0;actor.Guard(false);}
            if(GameTime.Now>=nextUrgent){nextUrgent=GameTime.Now+.25f;if(Urgent())return;}
            // Read incoming fire as a player does: roll across a shot about to hit.
            var shot=IncomingShot();if(shot.HasValue&&(Brace(shotFrom,BraceThrust)||Dodge(shot.Value)))return;
            var followingRight=Vector3.Cross(Vector3.up,active.motor.Facing).normalized;
            Vector3 goal=active.transform.position-active.motor.Facing*3+followingRight*2;
            bool fighting=ZoneController.Current!=null&&ZoneController.Current.Combat&&actor.target!=null&&actor.target.Alive;
            // A gate is waiting for her: she comes through it and stops past the line, wherever the frame or the formation would rather have her.
            bool ushered=false;
            if(!fighting&&ComeThrough!=null){var through=ComeThrough(active,actor);if(through.HasValue){goal=through.Value;ushered=true;}}
            // The hero she follows has right of way: on his way ahead of him, she steps off it until he has gone by (GivesWay).
            Vector3 aside=default;bool yielding=!fighting&&!ushered&&!actor.flight&&GivesWay(active,goal,out aside);
            if(yielding)goal=aside;
            float separation=(active.transform.position-transform.position).magnitude;
            bool flanking=false,closing=false;
            if(fighting&&separation<18&&actor.Health.integrity>actor.Health.maximum*.3f)
            {
                goal=actor.target.transform.position;closing=true;
                var carrier=actor.target.reactionOwner!=null?actor.target.reactionOwner:actor.target;
                if(actor.flight&&carrier.TryGetComponent<SerpentSegments>(out _))
                {
                    // A link lies inside a long body: chasing its centre sends
                    // the ship through the animal. Keep one side per selection.
                    var across=Planar(actor.target.transform.right).normalized;
                    if(flankTarget!=actor.target)
                    {flankTarget=actor.target;flankSide=Vector3.Dot(transform.position-goal,across)<0?-1:1;}
                    goal+=across*(6*flankSide);goal.y=transform.position.y;flanking=true;
                }
            }
            // Read the ground as a player does: out of any marking a boss has laid, by the shortest way, rolling if it is about to land.
            bool escaping=MarkedEscape(out var escape,out float landing,out var marking);
            if(escaping)
            {
                // In flight that way keeps clear of anything large drawn beside her (WayOutInFlight). A band running out at her is still met toward where it comes from.
                bool chosen=false,cornered=false,rollable=true;
                if(actor.flight&&marking.Spreads<=0)escape=WayOutInFlight(marking,escape,out chosen,out cornered,out rollable);
                goal=transform.position+escape*5;flanking=false;
                if(landing<.14f*Time.timeScale&&(Brace(markedFrom,BossBraceThrust)||(chosen?rollable&&RollOut(escape):Dodge(escape))))return;
                // No way out keeps that room: the one that keeps the most, rolled through at once rather than flown slowly past the body.
                if(cornered&&rollable&&rolledOutOf!=marking&&RollOut(escape)){rolledOutOf=marking;return;}
            }
            // Getting out of a marking comes first; where she then comes to rest stays where the player can see her.
            bool framedNow=false;
            if(!escaping&&camera!=null&&!ushered&&!yielding)
            {
                float rest=flanking?.6f:fighting?(actor.flight?2.2f:actor.character=="Sela"?7.5f:1.35f):1.5f;
                // Closing on a target she can see, her place keeps her reach of it; following, it keeps near the lead.
                goal=KeepInFrame(camera,active,goal,rest,closing&&seen,out framedNow);
            }
            bool avoiding=false,avoidanceBrake=false;
            if(flightSeparation!=null)goal=flightSeparation.Guide(transform.position,goal,flanking?actor.target:null,out avoiding,out avoidanceBrake);
            bool wallAdjusted=false;
            if(actor.flight)
            {
                flightObstacles??=new FlightObstacles(GetComponent<CharacterController>());
                goal=flightObstacles.Guide(transform.position,goal,actor.motor.Velocity,out wallAdjusted,out var wallCorner,out var wallBrake);
                avoiding|=wallCorner;avoidanceBrake|=wallBrake;
            }
            else flightObstacles=null;
            var d=goal-transform.position;
            // Ground melee closes to the actual wrist edge. The old 2.2 m
            // stand-off relied on the removed oversized invisible hit sphere.
            float distance=d.magnitude,stop=fighting?(actor.flight?2.2f:actor.character=="Sela"?7.5f:1.35f):1.5f;
            if(flanking||wallAdjusted)stop=.6f;
            if(framedNow)stop=Mathf.Min(stop,FrameStop);
            if(escaping)stop=.3f;
            if(avoiding)stop=.45f;
            if(ushered)stop=.4f;
            yielding&=!escaping;if(yielding)stop=WayStop;
            if(!actor.flight&&GroundNavigation.Current!=null&&!fighting)
            {
                if(Time.unscaledTime>=nextPath)
                {
                    nextPath=Time.unscaledTime+.3f;
                    corners=GroundNavigation.Current.FindPath(transform.position,goal,active.transform.position,path)?path.corners:null;
                    corner=1;
                }
                if(corners!=null&&corners.Length>1)
                {
                    while(corner<corners.Length-1&&Planar(corners[corner]-transform.position).magnitude<.35f)corner++;
                    d=Planar(corners[corner]-transform.position);
                    // Do not stop at the formation radius before traversing a doorway.
                    stop=corner<corners.Length-1?.15f:ushered?.4f:yielding?WayStop:1.2f;distance=d.magnitude;
                }
                else {d=Vector3.zero;distance=0;}
            }
            // Permanent flight braking capped the partner near 2 m/s, leaving her
            // a whole encounter behind and making swap pull the camera backwards.
            // An approaching release needs prompt outward thrust even when the
            // partner is already close to the leader. Use the same motor boost;
            // current wall/approach braking still takes priority.
            actor.motor.Move(distance>stop?new Vector2(d.x,d.z).normalized:Vector2.zero,(separation>10||flightSeparation!=null&&flightSeparation.EscapingDeparture)&&!avoidanceBrake,actor.flight&&(distance<stop+2||avoidanceBrake));
            if((flanking||wallAdjusted&&fighting)&&!avoiding&&distance<stop+1&&actor.State!=Lattice.Data.ActorState.Dodge)
                GetComponent<FlightMotor>().FaceTarget(actor.target.transform.position-transform.position);
            // Come to rest facing what she's fighting, as a player turns before firing: brought up short (a marking, the frame, the lead's side), she
            // kept the heading she arrived on, and every shot left along it.
            else if(fighting&&seen&&!avoiding&&!escaping&&distance<=stop+(actor.flight?1:0)&&(actor.State==Lattice.Data.ActorState.Idle||actor.State==Lattice.Data.ActorState.Move))
                FaceToward(actor.target.transform.position-transform.position);
            if(fighting)
            {
                var opponent=actor.target.reactionOwner!=null?actor.target.reactionOwner:actor.target;
                var threat=opponent.GetComponent<EnemyBrain>();var boss=opponent.GetComponent<BossController>();
                var fromThreat=transform.position-opponent.transform.position;fromThreat.y=0;
                bool incoming=boss!=null&&boss.Telegraphing&&boss.TelegraphRemaining<.12f*Time.timeScale&&fromThreat.magnitude<18||
                    threat!=null&&threat.Telegraphing&&threat.TelegraphRemaining<.12f*Time.timeScale&&fromThreat.magnitude<5;
                if(incoming)
                {
                    // Dodge across a committed charge, rather than chase it through
                    // another attack wind-up. The same defensive rules apply to AI.
                    var evade=Vector3.Cross(Vector3.up,fromThreat.normalized);
                    if(evade.sqrMagnitude<.01f)evade=Vector3.right;
                    if(Brace(opponent.transform.position,boss!=null?BossBraceThrust:BraceThrust)||Dodge(evade))return;
                }
                var fromTarget=Planar(actor.target.transform.position-transform.position);
                // Nothing the player can't see: not at a target off the screen, and not from off it, where her bolts flew in from nowhere.
                bool inView=seen&&Shows(camera,transform.position+Vector3.up*(actor.flight?0:.9f),0);
                if(inView&&GameTime.Now>=nextSkill&&TrySkill(fromTarget.magnitude))nextSkill=GameTime.Now+Random.Range(6f,9f);
                bool shooter=actor.flight||actor.character=="Sela";
                // A shot leaves along the heading unless the target is inside the aim's cone (CombatActor.Aim), so a gun fires only once it's
                // pointed: run out of a marking or round the lead with the trigger held, her shots went off the screen with the boss behind her.
                // A hammer reaches what a blade does; her cutter, in flight, a short arc off the prow and nothing like a gun's fourteen metres.
                bool hammer=actor.Style.attack==Lattice.Data.AttackStyle.Hammer;
                if(inView&&fromTarget.magnitude<(hammer&&actor.flight?HeroSkills.CutterReach-.5f:shooter?14:3)&&(!shooter||Vector3.Angle(actor.motor.Facing,fromTarget)<(actor.flight?FlightFireCone:GroundFireCone)))actor.Attack();
            }
        }
        // ---------- the hero she follows has right of way ----------
        /// <summary>How far ahead of a walking lead his way is read, how far either side of his line it reaches (two bodies and a hand), how far off his line she waits for
        /// him to pass, and how close she comes to that place.</summary>
        const float WayAhead=5,WayHalf=1.1f,WayAside=1.5f,WayStop=.25f;
        bool givingWay;
        /// <summary>Her place is behind the lead, so when he turns back past where she stands (off a talk, out of a load) the way to it is through him: she walked into him,
        /// the two bodies slid off together, and once that stopped each pushed into the other for good (routes 42, 55 and 56, November 15; GivingWayTests). While he walks and
        /// she stands on his way ahead of him, or would cross it ahead of him to reach her place, she steps off it to the side she is on (never nearer his line than she
        /// already is) and waits there until he has gone by; then she falls in behind as before. With no room to that side she goes on ahead of him.</summary>
        bool GivesWay(CombatActor lead,Vector3 place,out Vector3 aside)
        {
            aside=default;bool was=givingWay;givingWay=false;
            var heading=lead.motor is GroundMotor ground?Planar(ground.Asked):Vector3.zero;
            if(heading.sqrMagnitude>.25f)
            {
                heading.Normalize();var right=Vector3.Cross(Vector3.up,heading);
                var from=Planar(transform.position-lead.transform.position);
                float ahead=Vector3.Dot(from,heading),across=Vector3.Dot(from,right),placeAcross=Vector3.Dot(Planar(place-lead.transform.position),right);
                givingWay=was?ahead>-.5f&&ahead<WayAhead+2&&Mathf.Abs(across)<WayAside+1:
                    ahead>-.3f&&ahead<WayAhead&&(Mathf.Abs(across)<WayHalf||ahead>0&&Mathf.Abs(across)<WayAside+.5f&&placeAcross*across<0);
                if(givingWay)
                {
                    float side=Mathf.Abs(across)>.05f?Mathf.Sign(across):placeAcross<0?-1:1;
                    var onLine=lead.transform.position+heading*ahead;
                    aside=onLine+right*(side*Mathf.Max(WayAside,Mathf.Abs(across)));aside.y=transform.position.y;
                    if(!Room(aside))
                    {
                        var other=onLine-right*(side*WayAside);other.y=transform.position.y;
                        aside=Mathf.Abs(across)<.35f&&Room(other)?other:transform.position+heading*2.5f;
                    }
                }
            }
            if(givingWay!=was)nextPath=0;
            return givingWay;
        }
        /// <summary>Whether she can walk straight from where she stands to a place on the floor at her own level. With no baked floor, anywhere.</summary>
        bool Room(Vector3 to)
        {
            if(GroundNavigation.Current==null||!NavMesh.SamplePosition(transform.position,out var here,1.2f,NavMesh.AllAreas))return true;
            if(!NavMesh.SamplePosition(to,out var there,.4f,NavMesh.AllAreas)||Mathf.Abs(there.position.y-here.position.y)>.5f)return false;
            return !NavMesh.Raycast(here.position,there.position,out _,NavMesh.AllAreas);
        }
        // ---------- what the player can see ----------
        /// <summary>A gun fires only within this of its target, well inside Aim's own 30 and 120 degrees: past it on foot the shot left the posed arm
        /// sideways, and in flight a nose 25 degrees off read as shooting past.</summary>
        const float FlightFireCone=20,GroundFireCone=35;
        /// <summary>How far in from the screen's edge her body stands, as a share of the screen's height, and the gap she keeps from a plate, in pixels.</summary>
        const float FrameEdge=.045f,PlateGap=10;
        /// <summary>How close she comes to a place picked to keep her in the frame: the fighting stand-offs would stop her metres short of it, still under the plate.</summary>
        const float FrameStop=.4f;
        /// <summary>A target only counts while its middle is on the screen: half a body at the edge is something the player can see her shoot.</summary>
        const float TargetInset=0;
        static readonly float[] FrameRings={1.25f,2.5f,4,6};
        const float FrameSearchEvery=.2f;
        Vector3? framed;float nextFrameSearch;
        /// <summary>The lead's target while the player can see it. Off the screen: the boss in a boss fight if it shows, else whatever shows nearest her,
        /// else the lead's still, for where to go, with her fire held (seen false). With no camera (a fixture) everything counts as seen.</summary>
        Health Seen(Camera camera,Health chosen,out bool seen)
        {
            seen=chosen!=null;if(camera==null||chosen==null||Shows(camera,AimPoint(chosen),TargetInset))return chosen;
            var boss=BossController.Focus(transform.position,45);
            if(boss!=null&&boss.Health!=null&&boss.Health.Targetable&&!boss.Burrowed&&Shows(camera,AimPoint(boss.Health),TargetInset))return boss.Health;
            Health nearest=null;float best=30*30;
            foreach(var body in Health.All)
            {
                if(body==null||body.friendly||!body.Targetable||body.TryGetComponent<BossController>(out var buried)&&buried.Burrowed)continue;
                float far=Planar(body.transform.position-transform.position).sqrMagnitude;
                if(far<best&&Shows(camera,AimPoint(body),TargetInset)){best=far;nearest=body;}
            }
            if(nearest!=null)return nearest;
            seen=false;return chosen;
        }
        Vector3 AimPoint(Health body)=>body.transform.position+Vector3.up*(actor.flight?0:.9f);
        /// <summary>Whether a point stands on the screen at least this share of its height in from every edge.</summary>
        static bool Shows(Camera camera,Vector3 world,float inset)
        {
            if(camera==null)return true;
            var at=camera.WorldToViewportPoint(world);float across=inset*camera.pixelHeight/Mathf.Max(1,camera.pixelWidth);
            return at.z>0&&at.x>across&&at.x<1-across&&at.y>inset&&at.y<1-inset;
        }
        /// <summary>Her body at a place, whole on the screen and clear of every HUD plate: on foot her feet, middle and head; in flight the hull as wide as it shows.</summary>
        bool Fits(Camera camera,Vector3 at)
        {
            float edge=FrameEdge*camera.pixelHeight;
            if(actor.flight)
            {
                var middle=camera.WorldToScreenPoint(at);if(middle.z<=0)return false;
                float hull=Mathf.Abs(camera.WorldToScreenPoint(at+camera.transform.right*HeroCollision.HullRadius(actor.character)).x-middle.x);
                return Inside(camera,middle,hull+edge)&&!HudKeepOut.Covers(middle,hull+PlateGap);
            }
            var feet=camera.WorldToScreenPoint(at);var waist=camera.WorldToScreenPoint(at+Vector3.up*.9f);var head=camera.WorldToScreenPoint(at+Vector3.up*1.7f);
            return Inside(camera,feet,edge)&&Inside(camera,head,edge)&&!HudKeepOut.Covers(feet,PlateGap)&&!HudKeepOut.Covers(waist,PlateGap)&&!HudKeepOut.Covers(head,PlateGap);
        }
        static bool Inside(Camera camera,Vector3 screen,float inset)=>screen.z>0&&screen.x>inset&&screen.x<camera.pixelWidth-inset&&screen.y>inset&&screen.y<camera.pixelHeight-inset;
        /// <summary>Where she'll come to rest, kept in the open frame: on the screen with a margin and out from under the HUD's plates. Her place came from the
        /// lead's heading and the target alone, so she stood under the skill row and the control bar, behind the boss card, across the warp prompt and off the
        /// bottom edge with her bolts coming in from nowhere. Where that place doesn't fit, the nearest one that does, within her reach of a target she can see
        /// (or near the lead), on open floor, out of every boss marking, with no wall on the way; and the one she picked last while it still does, so she doesn't
        /// hop between two. Nothing fits: she goes where she was going.</summary>
        Vector3 KeepInFrame(Camera camera,CombatActor lead,Vector3 goal,float stop,bool engaged,out bool moved)
        {
            moved=false;
            var to=Planar(goal-transform.position);float far=to.magnitude;
            var rest=far>stop?transform.position+to/far*(far-stop):transform.position;rest.y=transform.position.y;
            if(Fits(camera,rest)){framed=null;return goal;}
            if(framed.HasValue&&Planar(framed.Value-rest).magnitude<8&&Allowed(camera,lead,framed.Value,engaged,out _)){moved=true;return framed.Value;}
            framed=null;
            // A search is up to 68 places: a few times a second is plenty for something that moves at a walk.
            if(Time.unscaledTime<nextFrameSearch)return goal;
            nextFrameSearch=Time.unscaledTime+FrameSearchEvery;
            // Round where she'd have stood, the side toward the lead first; then along the way back to the lead.
            var toLead=Planar(lead.transform.position-rest);float start=toLead.sqrMagnitude>.01f?Mathf.Atan2(toLead.x,toLead.z)*Mathf.Rad2Deg:0;
            foreach(float ring in FrameRings)
                for(int i=0;i<16;i++)
                {
                    float turn=start+(i%2==0?1:-1)*((i+1)/2)*22.5f;
                    var place=rest+Quaternion.Euler(0,turn,0)*Vector3.forward*ring;
                    if(Allowed(camera,lead,place,engaged,out var floor)){framed=floor;moved=true;return floor;}
                }
            if(toLead.sqrMagnitude>4)
            {
                var beside=lead.transform.position-toLead.normalized*2;beside.y=rest.y;
                for(float t=.25f;t<=1.001f;t+=.25f)
                    if(Allowed(camera,lead,Vector3.Lerp(rest,beside,t),engaged,out var floor)){framed=floor;moved=true;return floor;}
            }
            return goal;
        }
        /// <summary>How far from his target a blade's place in the frame may be. He comes to rest within FrameStop of it, so this and that together are inside what his
        /// swing reaches. It was 2.6: with his target near the top of the screen he stood 2.7 m off it and swung at nothing for as long as it stayed there, and never
        /// came within his Cleave's two metres (the November 11 probe of QaPlanCombatTests' Cleave fixture). With no such place in the frame he closes as he always did.</summary>
        const float BladeReach=1.5f;
        /// <summary>A place she may be sent to keep her in the frame.</summary>
        bool Allowed(Camera camera,CombatActor lead,Vector3 place,bool engaged,out Vector3 floor)
        {
            floor=place;float lift=actor.flight?.8f:.9f;
            if(engaged)
            {
                float reach=Planar(actor.target.transform.position-place).magnitude;
                if(actor.flight||actor.character=="Sela"?reach<3.5f||reach>(actor.Style.attack==Lattice.Data.AttackStyle.Hammer?HeroSkills.CutterReach-.5f:12):reach>BladeReach)return false;
            }
            else{float near=Planar(lead.transform.position-place).magnitude;if(near<1.5f||near>9)return false;}
            if(!Fits(camera,place))return false;
            if(!actor.flight&&GroundNavigation.Current!=null)
            {
                if(!NavMesh.SamplePosition(place,out var hit,.8f,NavMesh.AllAreas))return false;
                floor=hit.position;if((floor-place).sqrMagnitude>.04f&&!Fits(camera,floor))return false;
            }
            foreach(var marking in Telegraph.Live)if(marking!=null&&!marking.Released&&marking.owner is BossController&&marking.Contains(floor))return false;
            if(!CombatCover.Clear(transform.position+Vector3.up*lift,floor+Vector3.up*lift))return false;
            return !engaged||CombatCover.Clear(floor+Vector3.up*lift,AimPoint(actor.target));
        }
        /// <summary>Turns her where she stands; a ship turns its nose.</summary>
        void FaceToward(Vector3 direction)
        {
            direction.y=0;if(direction.sqrMagnitude<.01f)return;
            if(actor.flight){GetComponent<FlightMotor>().FaceTarget(direction);return;}
            if(actor.motor is not GroundMotor ground)return;
            transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),GroundMotor.CombatTurnRate*actor.MotorDelta);
            ground.KeepFacing(transform.forward);
        }
        /// <summary>A marking an ordinary enemy laid (its own tell, a gun's flak rows, a dying body's blast). In flight she read only a boss's markings and incoming shots, and flew
        /// into every other marked area: a clamp gun's rows struck Idra four times in every Mother of Doors fight through the replay driver's pad (MooringBossReplayTests). On foot she reads
        /// as she always did (her target's tell close by, PartnerEvasionTests).</summary>
        static bool Hostile(Object owner)=>owner is EnemyBrain||owner is DeathBlast||owner is Component part&&part!=null&&part.GetComponentInParent<EnemyBrain>()!=null;
        /// <summary>How soon a band running out at her (Mother of Doors' wave) is read: from then she goes to meet it, and rolls through it toward where it comes from as it reaches her.</summary>
        const float RunningBandRead=.6f;
        bool MarkedEscape(out Vector3 escape,out float landing,out Telegraph which)
        {
            escape=default;landing=float.PositiveInfinity;which=null;
            foreach(var marking in Telegraph.Live)
            {
                if(marking==null||marking.Released||!(marking.owner is BossController||actor.flight&&Hostile(marking.owner)))continue;
                // A band that runs outward (Telegraph.Spreads) is read as it comes, not only once it is over her: it reached her the frame it struck, and she took every wave. Out of it is
                // toward where it comes from, through it: rolled away from it, it follows faster than a roll and is on her as the roll ends.
                bool running=marking.Spreads>0;float lands=running?marking.Reaches(transform.position):marking.Contains(transform.position)?marking.Remaining:float.PositiveInfinity;
                if(float.IsPositiveInfinity(lands)||running&&lands>RunningBandRead)continue;
                if(lands<landing)
                {
                    landing=lands;which=marking;var inward=marking.Origin-transform.position;inward.y=0;
                    escape=running&&inward.sqrMagnitude>.01f?inward.normalized:marking.WayOut(transform.position);markedFrom=((Component)marking.owner).transform.position;
                }
            }
            return !float.IsPositiveInfinity(landing);
        }
        /// <summary>The rays tried for a way out of a marking in flight, how far along one she looks for its edge, and in what steps.</summary>
        const int WayOutRays=16;const float WayOutReach=14,WayOutStep=.25f;
        FlightBodies flightBodies;Telegraph wayOutOf,rolledOutOf;Vector3 wayOutWas;
        /// <summary>In flight the way out of a marking keeps clear of the drawn body of anything large near her by the room her travel keeps off the Cantor's (FlightBodies): the
        /// marking's own way out while that is clear, else the shortest way out that is, else the one that keeps the most room (cornered), which is rolled. A way she rolls along is
        /// clear of the party's hulls as well. With nothing large near, the marking's own way and the roll as before (chosen false). Since D323 she leaves every enemy's marking in flight,
        /// and the marking's way alone took her across the Cantor: inside its volley cone with the lead in her way out, the roll for shots turned the other way, toward the head, which
        /// swam on into her (CompanionTravelTests: 9 of 24 runs on November 20).</summary>
        Vector3 WayOutInFlight(Telegraph marking,Vector3 way,out bool chosen,out bool cornered,out bool rollable)
        {
            chosen=cornered=false;rollable=true;
            flightBodies??=new FlightBodies();flightBodies.Gather(transform.position);
            if(!flightBodies.Any){wayOutOf=null;return way;}
            chosen=true;var here=transform.position;float hull=HeroCollision.HullRadius(actor.character),roll=Mathf.Max(1,actor.Style.roll);
            float Out(Vector3 d){for(float t=WayOutStep;t<=WayOutReach;t+=WayOutStep)if(!marking.Contains(here+d*t))return t;return float.PositiveInfinity;}
            // The way is read to half a metre past the edge and for at least a roll's length.
            float RoomOn(Vector3 d,float length)=>flightBodies.Room(here,here+d*Mathf.Max((float.IsPositiveInfinity(length)?WayOutReach:length)+.5f,roll),hull);
            bool Clear(Vector3 d){float length=Out(d);return !float.IsPositiveInfinity(length)&&RoomOn(d,length)>=0&&RollClearsParty(d);}
            // The way she took a moment ago, while it stays clear: she doesn't swing between two.
            if(wayOutOf==marking&&Clear(wayOutWas))return wayOutWas;
            wayOutOf=marking;
            if(Clear(way))return wayOutWas=way;
            Vector3 best=default,safest=way;float shortest=float.PositiveInfinity,most=float.NegativeInfinity;bool safestRolls=false;
            for(int i=-1;i<WayOutRays;i++)
            {
                var d=i<0?way:Quaternion.Euler(0,i*360f/WayOutRays,0)*Vector3.forward;
                float length=Out(d),room=RoomOn(d,length);bool party=RollClearsParty(d),leaves=!float.IsPositiveInfinity(length);
                if(leaves&&room>=0&&party&&length<shortest){shortest=length;best=d;}
                // Cornered, a way that leaves within reach comes first, then one the party lets her roll along, then the room it keeps.
                float score=room+(leaves?0:-100)+(party?0:-50);
                if(score>most){most=score;safest=d;safestRolls=party;}
            }
            if(!float.IsPositiveInfinity(shortest))return wayOutWas=best;
            cornered=true;rollable=safestRolls;wayOutOf=null;return safest;
        }
        /// <summary>A roll along a way already chosen clear (WayOutInFlight), with none of the shot roll's fallbacks: for a shot the other way round is as good, out of a marking it is back in.</summary>
        bool RollOut(Vector3 direction)
        {
            if(direction.sqrMagnitude<.01f||actor.State==Lattice.Data.ActorState.Skill&&!actor.Rooted)return false;
            return actor.Dodge(direction);
        }
        // ---------- the guard hero as a companion (S6a) ----------
        /// <summary>The Thrust she wants in hand before she'll meet an ordinary hit with the guard, and a boss's; and how long she holds it: a shot is read a third of a
        /// second out, and the guard has to be up when it arrives, not let go the frame before.</summary>
        const float BraceThrust=30,BossBraceThrust=60,BraceHold=.6f;
        float braceUntil,nextUrgent;Vector3 shotFrom,markedFrom;
        void OnDisable(){if(braceUntil>0&&actor!=null&&actor.Health!=null&&actor.Health.Alive)actor.Guard(false);braceUntil=0;}
        /// <summary>A hero whose style says so (HeroStyle.partnerGuards) meets what's about to land with the guard where the others roll, when she can brace it: on
        /// foot with the Thrust to pay for the hit, in flight only what comes at her prow. Not out of a skill: raising a guard lets go of it.</summary>
        bool Brace(Vector3 from,float thrust)
        {
            var style=actor.Style;
            if(!style.partnerGuards||actor.GuardBroken||actor.thrust<thrust||actor.State==Lattice.Data.ActorState.Skill||actor.State==Lattice.Data.ActorState.Dodge||actor.Committed)return false;
            if(actor.flight)
            {
                if(!style.flightGuard)return false;
                var to=Planar(from-transform.position);if(to.sqrMagnitude>.01f&&Vector3.Angle(actor.motor.Facing,to)>style.prowArc*.5f-10)return false;
            }
            braceUntil=GameTime.Now+BraceHold;actor.Guard(true);actor.motor.Move(Vector2.zero,false,actor.flight);return actor.Guarding;
        }
        /// <summary>The skills that answer what's happening to the party (IdraSkills.Urgent), cast as soon as they're wanted and can be paid for.</summary>
        bool Urgent()
        {
            if(ZoneController.Current==null||!ZoneController.Current.Combat||!actor.CanAct)return false;
            for(int slot=0;slot<Lattice.Core.SkillBook.Slots;slot++)
            {
                var skill=Lattice.Core.SkillBook.Equipped(actor.character,slot);
                if(skill==null||!IdraSkills.Urgent(actor,skill.kind)||actor.cooldowns[slot]>0||actor.charge<Lattice.Core.SkillBook.Cost(actor.character,skill))continue;
                if(actor.Skill(slot))return true;
            }
            return false;
        }
        float nextSkill;
        /// <summary>Like a Ys partner, the companion spends Charge on its equipped skills when a target sits in that
        /// skill's reach (D201), at most every six to nine seconds so the player's own skills still lead.</summary>
        bool TrySkill(float distance)
        {
            int start=Random.Range(0,Lattice.Core.SkillBook.Slots);
            for(int n=0;n<Lattice.Core.SkillBook.Slots;n++)
            {
                int slot=(start+n)%Lattice.Core.SkillBook.Slots;var skill=Lattice.Core.SkillBook.Equipped(actor.character,slot);
                if(skill==null||actor.cooldowns[slot]>0||distance>Reach(skill.kind))continue;
                // Short of Charge there's nothing to try: every refused Skill() stamps the slot, and a swap in that moment read it as the new lead's refused press.
                if(actor.charge<Lattice.Core.SkillBook.Cost(actor.character,skill))continue;
                if(GapCloser(skill.kind)&&(distance<2.5f||actor.Health.integrity<actor.Health.maximum*.3f))continue;
                if(!Worthwhile(skill.kind))continue;
                if(actor.Skill(slot))return true;
            }
            return false;
        }
        static bool GapCloser(Lattice.Data.SkillKind kind)=>kind==Lattice.Data.SkillKind.AnchorSlam||kind==Lattice.Data.SkillKind.TetherHook||kind==Lattice.Data.SkillKind.EmberStep;
        /// <summary>Charge isn't spent on a directional skill with a wall in the way or, in flight, with the nose off the target.</summary>
        bool Worthwhile(Lattice.Data.SkillKind kind)
        {
            // The skills learned at Sync 16 to 24 bring their own rules (S3): no mend at full Integrity, no channel in a marked danger, a finisher that waits for a break.
            if(LateSkills.Decides(actor,kind,out bool late))return late;
            // A dash in flight goes its full length with nothing steering it, and a clear line to the target isn't a clear path for
            // the hull: a wall costs 12 integrity and the skill.
            if(actor.flight&&actor.target!=null)
            {
                var line=Planar(actor.target.transform.position-transform.position);
                float dash=kind==Lattice.Data.SkillKind.AnchorSlam?Mathf.Clamp(line.magnitude-2.5f,3,11):kind==Lattice.Data.SkillKind.EmberStep?10:kind==Lattice.Data.SkillKind.WaypointBlink?8:kind==Lattice.Data.SkillKind.TetherHook&&HookPullsHull(line.magnitude)?line.magnitude-2:0;
                if(dash>0&&line.sqrMagnitude>.01f)
                {
                    var hull=transform.position+Vector3.up*.8f;var along=(kind==Lattice.Data.SkillKind.WaypointBlink?-line:line).normalized;
                    if(CombatCover.Sweep(hull,hull+along*dash,HeroCollision.HullRadius(actor.character),out _))return false;
                    // Nor through the animal her travel keeps clear of: Taren's Ember Step went ten metres at a collar link, through the Cantor and out of its other side (CompanionTravelTests' probe, November 20).
                    if(flightSeparation!=null&&!flightSeparation.PathClear(transform.position,transform.position+along*dash))return false;
                }
            }
            if(kind==Lattice.Data.SkillKind.Overdrive||kind==Lattice.Data.SkillKind.Refract||kind==Lattice.Data.SkillKind.SurveyDrone||kind==Lattice.Data.SkillKind.WaypointBlink)return true;
            if(actor.target==null)return false;
            var to=actor.target.transform.position-transform.position;to.y=0;
            // Aim only follows a target in front of the hero: past that a directional skill leaves along the heading, into empty space
            // (retreating, following the lead, leaving a marking). On the ground a burst goes all round, and a Fold or a Starfall lands on the target itself.
            bool placed=kind==Lattice.Data.SkillKind.GravityFold||kind==Lattice.Data.SkillKind.Starfall;
            if(Vector3.Angle(actor.motor.Facing,to)>=(actor.flight?30:120)&&(actor.flight||kind!=Lattice.Data.SkillKind.PulseBurst&&!placed))return false;
            var from=transform.position+Vector3.up*(actor.flight?0:.9f);
            return CombatCover.Clear(from,from+to);
        }
        /// <summary>A hook on a body too heavy to reel (a boss, a collar link) pulls the hull instead, as TetherLine does it.</summary>
        bool HookPullsHull(float gap)
        {
            if(gap<=2.4f)return false;
            var brain=actor.target.GetComponent<EnemyBrain>();
            return brain==null||!brain.Mine&&(!brain.CanBeMoved||brain.Heavy);
        }
        float Reach(Lattice.Data.SkillKind kind)=>kind switch
        {
            Lattice.Data.SkillKind.ScatterBloom=>5f,Lattice.Data.SkillKind.StaticNet=>9f,
            Lattice.Data.SkillKind.ForgeSpiral=>3.2f,
            // On foot the blade passes a small body 1.7 m ahead, and a hero running in closes up to half a metre before it comes round: from 3.2 m it cut air, 20 Charge and all.
            // In flight it's a 5.4 m wave, and a hull can't get within 2 m of anything.
            Lattice.Data.SkillKind.ArcCleave=>actor.flight?3.2f:2f,
            Lattice.Data.SkillKind.PulseBurst or Lattice.Data.SkillKind.WaypointBlink=>3.8f,
            Lattice.Data.SkillKind.EmberStep=>6f,Lattice.Data.SkillKind.AnchorSlam=>8f,Lattice.Data.SkillKind.Faultline=>9f,
            Lattice.Data.SkillKind.TetherHook=>11f,Lattice.Data.SkillKind.Overdrive=>6f,
            // Never: a parry wants a read on the hit that's coming, and this pick knows only how far off the target is. At random it's 20 Charge on nothing.
            Lattice.Data.SkillKind.Refract=>0f,
            _=>LateSkills.Reach(actor,kind),
        };
        static bool Departing(SerpentSegments body)=>body!=null&&body.gameObject.activeInHierarchy&&
            body.TryGetComponent<CantorRelease>(out var release)&&release.Departing;
        bool Dodge(Vector3 direction)
        {
            if(direction.sqrMagnitude<.01f)return false;
            // Committed, as a player is: a roll would throw away the skill it has just spent Charge on. Not while it's rooted, though (Long Exposure's two and a half
            // seconds, Severance's wind-up): standing there is what the roll is for, as it is for a player.
            if(actor.State==Lattice.Data.ActorState.Skill&&!actor.Rooted)return false;
            if(!actor.flight)return actor.Dodge(direction);
            for(int i=0;i<8;i++)
            {
                // Prefer the other perpendicular before rolling along an
                // incoming shot. A nearby ally must not block that travel.
                float angle=i==0?0:i==1?180:i==2?45:i==3?-45:i==4?90:i==5?-90:i==6?135:-135;
                var choice=(Quaternion.Euler(0,angle,0)*direction).normalized;
                if(flightSeparation!=null)choice=flightSeparation.Dodge(transform.position,choice);
                if(choice.sqrMagnitude>.01f&&RollClearsParty(choice))return actor.Dodge(choice);
            }
            return false;
        }
        bool RollClearsParty(Vector3 direction)
        {
            var travel=direction*4;var origin=transform.position;
            foreach(var member in PartyController.Current.members)
            {
                if(member==actor||!member.flight||!member.gameObject.activeInHierarchy)continue;
                var away=Planar(origin-member.transform.position);
                float radius=HeroCollision.HullRadius(actor.character)+HeroCollision.HullRadius(member.character)+.1f;
                if(away.sqrMagnitude<radius*radius)
                {if(Vector3.Dot(travel,away)<0)return false;continue;}
                float t=Mathf.Clamp01(-Vector3.Dot(away,travel)/travel.sqrMagnitude);
                if((away+travel*t).sqrMagnitude<radius*radius)return false;
            }
            return true;
        }
        Vector3? IncomingShot()
        {
            float body=GetComponent<CharacterController>().radius+.45f;
            float motionScale=actor.flight?actor.MotorDelta/Mathf.Max(.000001f,Time.unscaledDeltaTime):1;
            foreach(var shot in Projectile.Live)
            {
                if(!shot.Threatens(actor.Health.friendly))continue;
                var v=Planar(shot.Velocity*Time.timeScale-actor.motor.Velocity*motionScale);if(v.sqrMagnitude<.01f)continue;
                var rel=Planar(transform.position-shot.transform.position);
                float t=Vector3.Dot(rel,v)/v.sqrMagnitude;if(t<0)continue;
                var miss=rel-v*t;if(miss.magnitude>body)continue;
                // Predict entry into the hull in real seconds. Flash slows a
                // shot; time to its centre would react after slow contact.
                t-=Mathf.Sqrt(Mathf.Max(0,body*body-miss.sqrMagnitude)/v.sqrMagnitude);
                if(t>.35f)continue;
                shotFrom=shot.transform.position;
                // Roll to whichever side the shot is already passing, or square across it.
                var across=Vector3.Cross(Vector3.up,v.normalized);
                return miss.sqrMagnitude>.01f&&Vector3.Dot(miss,across)<0?-across:across;
            }
            return null;
        }
        static Vector3 Planar(Vector3 value){value.y=0;return value;}
    }
}
