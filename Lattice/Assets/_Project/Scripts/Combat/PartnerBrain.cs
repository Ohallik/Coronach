using Lattice.Core;
using UnityEngine;
using UnityEngine.AI;
namespace Lattice.Combat
{
    public sealed class PartnerBrain:MonoBehaviour
    {
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
        void OnEnable(){nextSkill=Mathf.Max(nextSkill,GameTime.Now+3);corners=null;nextPath=0;flankTarget=null;flightSeparation=null;departingSerpent=null;flightObstacles=null;framed=null;nextFrameSearch=0;}
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
            // Read incoming fire as a player does: roll across a shot about to hit.
            var shot=IncomingShot();if(shot.HasValue&&Dodge(shot.Value))return;
            var followingRight=Vector3.Cross(Vector3.up,active.motor.Facing).normalized;
            Vector3 goal=active.transform.position-active.motor.Facing*3+followingRight*2;
            bool fighting=ZoneController.Current!=null&&ZoneController.Current.Combat&&actor.target!=null&&actor.target.Alive;
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
            bool escaping=MarkedEscape(out var escape,out float landing);
            if(escaping)
            {
                goal=transform.position+escape*5;flanking=false;
                if(landing<.14f*Time.timeScale&&Dodge(escape))return;
            }
            // Getting out of a marking comes first; where she then comes to rest stays where the player can see her.
            bool framedNow=false;
            if(!escaping&&camera!=null)
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
                    stop=corner<corners.Length-1?.15f:1.2f;distance=d.magnitude;
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
                    if(Dodge(evade))return;
                }
                var fromTarget=Planar(actor.target.transform.position-transform.position);
                // Nothing the player can't see: not at a target off the screen, and not from off it, where her bolts flew in from nowhere.
                bool inView=seen&&Shows(camera,transform.position+Vector3.up*(actor.flight?0:.9f),0);
                if(inView&&GameTime.Now>=nextSkill&&TrySkill(fromTarget.magnitude))nextSkill=GameTime.Now+Random.Range(6f,9f);
                bool shooter=actor.flight||actor.character=="Sela";
                // A shot leaves along the heading unless the target is inside the aim's cone (CombatActor.Aim), so a gun fires only once it's
                // pointed: run out of a marking or round the lead with the trigger held, her shots went off the screen with the boss behind her.
                if(inView&&fromTarget.magnitude<(shooter?14:3)&&(!shooter||Vector3.Angle(actor.motor.Facing,fromTarget)<(actor.flight?FlightFireCone:GroundFireCone)))actor.Attack();
            }
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
        /// <summary>A place she may be sent to keep her in the frame.</summary>
        bool Allowed(Camera camera,CombatActor lead,Vector3 place,bool engaged,out Vector3 floor)
        {
            floor=place;float lift=actor.flight?.8f:.9f;
            if(engaged)
            {
                float reach=Planar(actor.target.transform.position-place).magnitude;
                if(actor.flight||actor.character=="Sela"?reach<3.5f||reach>12:reach>2.6f)return false;
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
        bool MarkedEscape(out Vector3 escape,out float landing)
        {
            escape=default;landing=float.PositiveInfinity;
            foreach(var marking in Telegraph.Live)
            {
                if(marking==null||marking.Released||!(marking.owner is BossController)||!marking.Contains(transform.position))continue;
                if(marking.Remaining<landing){landing=marking.Remaining;escape=marking.WayOut(transform.position);}
            }
            return !float.IsPositiveInfinity(landing);
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
            _=>12f,
        };
        static bool Departing(SerpentSegments body)=>body!=null&&body.gameObject.activeInHierarchy&&
            body.TryGetComponent<CantorRelease>(out var release)&&release.Departing;
        bool Dodge(Vector3 direction)
        {
            if(direction.sqrMagnitude<.01f)return false;
            // Committed, as a player is: a roll would throw away the skill it has just spent Charge on.
            if(actor.State==Lattice.Data.ActorState.Skill)return false;
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
                // Roll to whichever side the shot is already passing, or square across it.
                var across=Vector3.Cross(Vector3.up,v.normalized);
                return miss.sqrMagnitude>.01f&&Vector3.Dot(miss,across)<0?-across:across;
            }
            return null;
        }
        static Vector3 Planar(Vector3 value){value.y=0;return value;}
    }
}
