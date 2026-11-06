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
        void OnEnable(){nextSkill=GameTime.Now+3;corners=null;nextPath=0;flankTarget=null;flightSeparation=null;departingSerpent=null;flightObstacles=null;}
        void Awake(){actor=GetComponent<CombatActor>();path=new NavMeshPath();}
        void Update()
        {
            var party=PartyController.Current;if(GameTime.Paused||party==null||party.Active==actor||!actor.Health.Alive||actor.Recovering||actor.motor==null||GameServices.Current.Input.Blocked)return;
            var active=party.Active;actor.target=active.target;
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
            bool flanking=false;
            if(fighting&&separation<18&&actor.Health.integrity>actor.Health.maximum*.3f)
            {
                goal=actor.target.transform.position;
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
                if(GameTime.Now>=nextSkill&&TrySkill(fromTarget.magnitude))nextSkill=GameTime.Now+Random.Range(6f,9f);
                if(fromTarget.magnitude<(actor.flight||actor.character=="Sela"?14:3))actor.Attack();
            }
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
            if(kind==Lattice.Data.SkillKind.Overdrive||kind==Lattice.Data.SkillKind.Refract||kind==Lattice.Data.SkillKind.SurveyDrone||kind==Lattice.Data.SkillKind.WaypointBlink)return true;
            if(actor.target==null)return false;
            var to=actor.target.transform.position-transform.position;to.y=0;
            if(actor.flight&&Vector3.Angle(actor.motor.Facing,to)>30)return false;
            var from=transform.position+Vector3.up*(actor.flight?0:.9f);
            return CombatCover.Clear(from,from+to);
        }
        static float Reach(Lattice.Data.SkillKind kind)=>kind switch
        {
            Lattice.Data.SkillKind.ScatterBloom=>5f,Lattice.Data.SkillKind.StaticNet=>9f,
            Lattice.Data.SkillKind.ArcCleave or Lattice.Data.SkillKind.ForgeSpiral=>3.2f,
            Lattice.Data.SkillKind.PulseBurst or Lattice.Data.SkillKind.WaypointBlink=>3.8f,
            Lattice.Data.SkillKind.EmberStep=>6f,Lattice.Data.SkillKind.AnchorSlam=>8f,Lattice.Data.SkillKind.Faultline=>9f,
            Lattice.Data.SkillKind.TetherHook=>11f,Lattice.Data.SkillKind.Overdrive=>6f,Lattice.Data.SkillKind.Refract=>0f,
            _=>12f,
        };
        static bool Departing(SerpentSegments body)=>body!=null&&body.gameObject.activeInHierarchy&&
            body.TryGetComponent<CantorRelease>(out var release)&&release.Departing;
        bool Dodge(Vector3 direction)
        {
            if(direction.sqrMagnitude<.01f)return false;
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
