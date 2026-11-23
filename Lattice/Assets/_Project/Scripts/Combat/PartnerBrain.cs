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
        void OnEnable(){nextSkill=Mathf.Max(nextSkill,GameTime.Now+3);corners=null;nextPath=0;flankTarget=null;flightSeparation=null;departingSerpent=null;flightObstacles=null;framed=null;nextFrameSearch=0;wayOutOf=rolledOutOf=null;leadsWay.Clear();leadsWayOf=null;}
        void Awake(){actor=GetComponent<CombatActor>();path=new NavMeshPath();}
        // ---------- the lead's way in flight ----------
        /// <summary>Where the lead has flown, a point every <see cref="WayStep"/> metres, the newest last, for as far back as <see cref="WayKept"/> of them reach: what her steering
        /// follows back to him when neither the straight line nor one corner is open (FlightObstacles.Guide). On foot the baked floor gives her a way to him; in flight nothing did, and
        /// she waited at the first solid on the line to him for as long as he stayed beyond it. It begins again when somebody else leads, either of them leaves flight, or he is moved
        /// further than a hull ever flies in a frame (a load, an arrival).</summary>
        readonly System.Collections.Generic.List<Vector3> leadsWay=new();CombatActor leadsWayOf;
        const float WayStep=2.5f,WayJump=15;const int WayKept=96;
        void KeepLeadsWay(CombatActor lead)
        {
            var at=lead.transform.position;int count=leadsWay.Count;
            if(lead!=leadsWayOf||!lead.flight||!actor.flight||count>0&&(at-leadsWay[count-1]).sqrMagnitude>WayJump*WayJump){leadsWay.Clear();leadsWayOf=lead;count=0;}
            if(!lead.flight||!actor.flight)return;
            if(count==0||(at-leadsWay[count-1]).sqrMagnitude>=WayStep*WayStep){leadsWay.Add(at);if(leadsWay.Count>WayKept)leadsWay.RemoveAt(0);}
        }
        void Update()
        {
            var party=PartyController.Current;
            // Kept while she can't act on it (down, held by a talk): it is the way back once she can.
            if(party!=null&&party.Active!=null&&party.Active!=actor&&!GameTime.Paused)KeepLeadsWay(party.Active);
            if(GameTime.Paused||party==null||party.Active==actor||!actor.Health.Alive||actor.Recovering||actor.motor==null||GameServices.Current.Input.Blocked)return;
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
            // In flight, with him travelling and not toward her, beside him on her own side (Beside).
            var leadTravel=LeadTravel(active);Carried(active);bool adrift=Adrift(out var drift);bool travelling=leadTravel.magnitude>(actor.flight?FlightTravel:MovingLead)&&!LeadComing(active);
            if(actor.flight&&travelling)goal=Beside(active,leadTravel);else besideSide=0;
            bool fighting=ZoneController.Current!=null&&ZoneController.Current.Combat&&actor.target!=null&&actor.target.Alive;
            // A gate is waiting for her: she comes through it and stops past the line, wherever the frame or the formation would rather have her.
            bool ushered=false;
            if(!fighting&&ComeThrough!=null){var through=ComeThrough(active,actor);if(through.HasValue){goal=through.Value;ushered=true;}}
            // Nothing of hers is solid to the lead (HeroCollision.ThroughParty, D354): he goes through her as if she were not there, so none of what follows decides whether he gets where
            // he is going. It is where she belongs: her place behind his nose, her own side of a body, and never resting inside him.
            float separation=(active.transform.position-transform.position).magnitude;
            bool flanking=false,closing=false,standing=false;
            if(fighting&&separation<18&&actor.Health.integrity>actor.Health.maximum*.3f)
            {
                goal=actor.target.transform.position;closing=true;
                // On foot with a blade or a hammer, beside a lead who is at the same body: her own side of it (MeleeStand).
                if(!actor.flight&&actor.character!="Sela")goal=MeleeStand(active,actor.target,goal,out standing);
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
            // On foot with the lead stopped (a talk, a lift, a door) where she comes to rest is in the camera's sight too (Seen).
            restingInSight=!actor.flight&&!fighting&&leadTravel.magnitude<=MovingLead;
            float rest=flanking?.6f:standing?StandStop:fighting?(actor.flight?ShipStandOff(actor.target):actor.character=="Sela"?7.5f:1.35f):1.5f;
            if(!escaping&&camera!=null&&!ushered)
            {
                // Closing on a target she can see, her place keeps her reach of it; following, it keeps near the lead.
                goal=KeepInFrame(camera,active,goal,rest,closing&&seen,out framedNow);
            }
            if(standing&&!escaping)goal=RoundTheLead(active,goal);
            // In flight her way back to her place goes round him, not ahead of his nose down his next leg; while he flies toward her she comes to it through him.
            if(actor.flight&&!fighting&&!escaping&&!ushered&&flightSeparation==null&&!LeadComing(active))goal=ShipRoundTheLead(active,goal);
            // Where she comes to rest is never inside him; inside him (he went through where she was) she leaves him by the shortest way out. Not over an animal's own guide
            // (FlightSeparation): its departure has its own way out.
            bool leaving=false;
            // In flight with him travelling, her place beside him is already out of his hull, and she doesn't leave him: as he set off, or as she came in beside him in a current, she
            // read herself as leaving him, braked, and was left behind as he went (route 17's leg past barge two's rest, the Aurora Shear's short lane; PartnerInViewTests).
            if(!escaping&&flightSeparation==null&&!(actor.flight&&travelling))goal=OutOfTheLead(active,goal,ushered?.4f:framedNow?Mathf.Min(rest,FrameStop):rest,out leaving);
            // Following a lead on the move (KeepUp): her place goes with him.
            bool onTheMove=!fighting&&!escaping&&!ushered&&!leaving&&flightSeparation==null&&travelling;
            // Carried by a current that doesn't carry him, she leaves it where he did: by his own way (HisPath), not straight at her place across whatever the current carries her into.
            if(adrift&&!fighting&&!escaping&&!ushered&&flightSeparation==null)goal=HisPath(goal);
            bool avoiding=false,avoidanceBrake=false;
            if(flightSeparation!=null)goal=flightSeparation.Guide(transform.position,goal,flanking?actor.target:null,out avoiding,out avoidanceBrake);
            bool wallAdjusted=false;var aim=goal;
            if(actor.flight)
            {
                flightObstacles??=new FlightObstacles(GetComponent<CharacterController>());
                // Living bodies are kept off as solids are, but for the one she is fighting (and what it is part of), which she closes on to her stand-off (ShipStandOff).
                flightObstacles.Fighting=fighting?(actor.target.reactionOwner!=null?actor.target.reactionOwner:actor.target):null;flightObstacles.Part=fighting?actor.target:null;
                goal=flightObstacles.Guide(transform.position,goal,actor.motor.Velocity,leadsWay,out wallAdjusted,out var wallCorner,out var wallBrake);
                avoiding|=wallCorner;avoidanceBrake|=wallBrake;
            }
            else flightObstacles=null;
            var d=goal-transform.position;
            // Ground melee closes to the actual wrist edge. The old 2.2 m
            // stand-off relied on the removed oversized invisible hit sphere.
            float distance=d.magnitude,stop=fighting?(actor.flight?ShipStandOff(actor.target):actor.character=="Sela"?7.5f:1.35f):1.5f;
            if(standing)stop=StandStop;
            if(flanking||wallAdjusted)stop=.6f;
            if(framedNow)stop=Mathf.Min(stop,FrameStop);
            if(escaping)stop=.3f;
            if(avoiding)stop=.45f;
            if(ushered)stop=.4f;
            if(leaving)stop=Mathf.Min(stop,LeaveStop);
            if(onTheMove&&!wallAdjusted&&!avoiding&&!framedNow)stop=Mathf.Min(stop,MovingStop);
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
                    // At the way's end the stop her place asks for, never more than 1.2 m. A place picked to clear a plate (FrameStop) was walked to within 1.2 m and no nearer, and the nearest
                    // such place is 1.25 m off: she stayed under the plate (a quest's card over her in Lastlight; PartnerInViewTests).
                    stop=corner<corners.Length-1?.15f:Mathf.Min(stop,1.2f);distance=d.magnitude;
                }
                else {d=Vector3.zero;distance=0;}
            }
            // Permanent flight braking capped the partner near 2 m/s, leaving her
            // a whole encounter behind and making swap pull the camera backwards.
            // An approaching release needs prompt outward thrust even when the
            // partner is already close to the leader. Use the same motor boost;
            // current wall/approach braking still takes priority.
            bool arriving=actor.flight&&flightSeparation==null&&Arriving(active,aim,stop);
            // Not at a boost while her steering turns her round the corner of something solid: on leg 10 off the Picket's pocket 9 she struck Rock 8 boosting (HarrowgateReplayTests).
            var stick=distance>stop?new Vector2(d.x,d.z).normalized:Vector2.zero;bool hurry=KeepUp(onTheMove,Planar(aim-transform.position).magnitude,stop)&&!avoiding;
            // At her place with him on the move she goes his way at his pace: stopped there (and, in flight, braked to a crawl within two metres of it) she dropped behind
            // a place that had already gone on.
            // At her place while he gathers speed she boosts to his pace: at her own thrust she kept the gap she set off with, four metres behind her place by the time he was at
            // full speed, and under the control strip as he went up the screen (route 17's leg past barge two's rest; PartnerInViewTests).
            // His pace is his travel less what carries her too: both in one current, her thrust matches his, not his and the current's (the Aurora Shear's short lane, where she ran past
            // her place, turned back for it and fell seven metres behind). Slower than that and not past her place, she boosts.
            var hisPace=actor.flight?leadTravel-ownCarry:leadTravel;
            bool pacing=onTheMove&&actor.flight&&Planar(actor.motor.Velocity).magnitude<hisPace.magnitude-PaceGap&&Vector3.Dot(Planar(aim-transform.position),hisPace)>-1;
            // On foot only at the end of her way to her place, and only where the floor runs on along his travel: within a stop of a corner of her way, his heading took her off the
            // Frozen Host's walkway as he turned, down to a floor with no way back up to him, and she was left 57 m behind (the whole suite of November 22; PartnerInViewTests).
            // Elsewhere she goes on along her own way at his pace rather than stopping at a point of it (stopped, she fell back under Lastlight's quest card).
            if(onTheMove&&distance<=stop)
            {
                if(actor.flight||(corners==null?GroundNavigation.Current==null:corner>=corners.Length-1)&&FloorAlong(hisPace))stick=Pace(hisPace);
                else if(Planar(d).sqrMagnitude>.0025f)stick=new Vector2(d.x,d.z).normalized*Pace(hisPace).magnitude;
            }
            // Past her place along his travel (a current took her on ahead of him), she eases off and lets it come back to her, rather than turning about at a boost for it and
            // being left eight metres behind as he goes on (the Aurora Shear's short lane; PartnerInViewTests).
            if(actor.flight&&onTheMove&&hisPace.sqrMagnitude>1&&Vector3.Dot(Planar(aim-transform.position),hisPace.normalized)<-PastPlace){stick=Pace(hisPace*EaseOff);hurry=pacing=false;}
            // Carried by a current that doesn't carry him (Adrift), she flies out of it as a player does: at a boost, the stick leaned into the current by as much as it carries her.
            if(adrift&&stick.sqrMagnitude>.01f&&actor.motor is FlightMotor own)
            {
                var want=new Vector3(stick.x,0,stick.y).normalized*own.maxSpeed*1.9f-drift;stick=new Vector2(want.x,want.z).normalized;
            }
            actor.motor.Move(stick,(separation>10||hurry||pacing||adrift&&stick.sqrMagnitude>.01f||flightSeparation!=null&&flightSeparation.EscapingDeparture)&&!avoidanceBrake&&!arriving,actor.flight&&(distance<stop+2&&!onTheMove&&!adrift||avoidanceBrake||arriving));
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
        // ---------- never resting inside the lead ----------
        /// <summary>How far apart the two middles keep at rest: in flight both hulls' reach (HeroCollision.HullRadius) and a little, on foot <see cref="HeroesApart"/>.</summary>
        float Apart(CombatActor lead)=>actor.flight?HeroCollision.HullRadius(lead.character)+HeroCollision.HullRadius(actor.character)+.3f:HeroesApart;
        /// <summary>How close she comes to a place picked to take her out of him.</summary>
        const float LeaveStop=.2f;
        /// <summary>Where she goes, so that she comes to rest out of him. Nothing of hers is solid to the lead (D354): he flies or walks through her as if she were not there, and what she
        /// does can't decide whether he gets where he is going. That used to be done by rules about when she gives way, one for each narrow place: off his way on foot (D328, GivingWayTests),
        /// off his line in flight as he came (route 10's regroup), and the next narrow place stopped him again (the Steward's middle gate, nov22-whole s42c). What is left to her is how it looks: the place she would come to rest at, short of where she is going by her stop, is never inside him. If it would be, she goes instead
        /// to the nearest place just outside him on its side (on foot one she can walk straight to) and comes right up to it; inside him, because he went through where she was, that
        /// takes her out of him by the shortest way. Her way there may pass through him: that is a moment of two hulls drawn through each other, and nothing stops.</summary>
        Vector3 OutOfTheLead(CombatActor lead,Vector3 goal,float stop,out bool leaving)
        {
            leaving=false;float apart=Apart(lead);var him=Planar(lead.transform.position);var from=transform.position;
            var to=Planar(goal-from);float far=to.magnitude;var rest=Planar(far>stop?from+to/far*(far-stop):from);
            var off=rest-him;if(off.magnitude>=apart)return goal;
            leaving=true;var mine=Planar(from)-him;
            var away=off.sqrMagnitude>.01f?off.normalized:mine.sqrMagnitude>.01f?mine.normalized:-Planar(lead.motor.Facing).normalized;
            if(away.sqrMagnitude<.01f)away=Vector3.back;
            Vector3 first=default;
            for(int i=0;i<7;i++)
            {
                float turn=i==0?0:(i%2==1?1:-1)*((i+1)/2)*45f;
                var place=him+Quaternion.Euler(0,turn,0)*away*(apart+.15f);place.y=from.y;
                if(i==0)first=place;
                if(actor.flight||Room(place))return place;
            }
            return first;
        }
        // ---------- her own side of the body the lead is at ----------
        /// <summary>How far apart two heroes' middles stand at the least (both capsules and what their drawn bodies carry past them: a shoulder plate, a hammer's head); how close she
        /// comes to her place at the body; and how far from a body's middle a blade or a hammer stands at the least (the old stand-off).</summary>
        public const float HeroesApart=1.1f,StandStop=.25f,MeleeStandOff=1.35f;
        int meleeSide;
        /// <summary>Where she fights a body on foot from, with a blade or a hammer. Her place was the body itself, from wherever she stood: with the lead at its near side and her behind him,
        /// the way to it was through him, and she stood in him and swung from inside him (the Host Marshal, nov20-whole s36/062; BodiesKeepApartTests). While the lead is at it too, she takes a
        /// place on her own side of it at her stand-off, at least a quarter-turn round from him and never nearer him than <see cref="HeroesApart"/>, keeps that side while it holds, and goes
        /// round to it outside him when the straight way passes him. With no such place (a body against a wall, or one too small for two) she holds a step back on her side. With the lead
        /// elsewhere she closes as she always did.</summary>
        Vector3 MeleeStand(CombatActor lead,Health target,Vector3 goal,out bool standing)
        {
            standing=false;
            var centre=target.transform.position;
            float targetRadius=target.TryGetComponent<CharacterController>(out var shape)?shape.radius:.5f,own=GetComponent<CharacterController>().radius;
            float ring=Mathf.Max(MeleeStandOff,targetRadius+own+.1f);
            var leadOff=Planar(lead.transform.position-centre);var from=Planar(transform.position-centre);
            // At it, or walking to it (his side of it is where he is going: she cut across his way to the Tollkeeper's gate motor and stood at his place striking it, nov21-recon1 s65).
            var asked=lead.motor is GroundMotor walking?Planar(walking.Asked):Vector3.zero;
            bool coming=asked.sqrMagnitude>.25f&&leadOff.magnitude<12&&Vector3.Angle(asked,-leadOff)<35;
            if(leadOff.magnitude>ring+HeroesApart+1&&!coming||leadOff.sqrMagnitude<.01f){meleeSide=0;return goal;}
            if(meleeSide==0)meleeSide=Vector3.Cross(leadOff,from).y<0?-1:1;
            float least=2*Mathf.Asin(Mathf.Min(1,HeroesApart/(2*ring)))*Mathf.Rad2Deg,first=Mathf.Max(least+10,75);
            standing=true;var up=Vector3.up*transform.position.y;
            for(int pass=0;pass<2;pass++)
            {
                int side=pass==0?meleeSide:-meleeSide;
                for(float turn=first;turn<=180.01f;turn+=15)
                {
                    var place=Planar(centre)+Quaternion.Euler(0,side*turn,0)*leadOff.normalized*ring+up;
                    if(Planar(place-lead.transform.position).magnitude<HeroesApart||!Standable(centre,place))continue;
                    meleeSide=side;standCentre=centre;standRing=ring;return place;
                }
            }
            standCentre=centre;standRing=ring;
            var back=from.sqrMagnitude>.01f?from.normalized:-leadOff.normalized;var held=Planar(centre)+back*(ring+1.2f)+up;
            if(Planar(held-lead.transform.position).magnitude<HeroesApart)held=lead.transform.position+Quaternion.Euler(0,meleeSide*90,0)*leadOff.normalized*(HeroesApart+.3f);
            return held;
        }
        Vector3 standCentre;float standRing;
        /// <summary>The way to her place at the body, whoever chose it (her side, or the frame): straight unless that passes the lead closer than <see cref="HeroesApart"/> and a
        /// turn's width; else a step round the body on her side, outside him, toward it.</summary>
        Vector3 RoundTheLead(CombatActor lead,Vector3 place)
        {
            if(Clearance(transform.position,place,lead.transform.position)>=HeroesApart+.3f)return place;
            var leadOff=Planar(lead.transform.position-standCentre);var from=Planar(transform.position-standCentre);if(leadOff.sqrMagnitude<.01f)return place;
            float want=Vector3.SignedAngle(leadOff,Planar(place-standCentre),Vector3.up)*meleeSide,at=Mathf.Max(0,Vector3.SignedAngle(leadOff,from,Vector3.up)*meleeSide);
            var round=Planar(standCentre)+Quaternion.Euler(0,meleeSide*Mathf.Min(Mathf.Max(want,at),at+45),0)*leadOff.normalized*Mathf.Max(from.magnitude,standRing+HeroesApart);
            round.y=transform.position.y;return round;
        }
        /// <summary>How close the way from one point to another passes a third, on the plan.</summary>
        static float Clearance(Vector3 from,Vector3 to,Vector3 by)
        {
            var way=Planar(to-from);var off=Planar(by-from);float t=way.sqrMagnitude>.0001f?Mathf.Clamp01(Vector3.Dot(off,way)/way.sqrMagnitude):0;
            return (off-way*t).magnitude;
        }
        /// <summary>A place at a body she can stand at: floor under it, and nothing solid between it and the body.</summary>
        bool Standable(Vector3 centre,Vector3 place)
        {
            if(GroundNavigation.Current!=null&&(!NavMesh.SamplePosition(place,out var floor,.6f,NavMesh.AllAreas)||Mathf.Abs(floor.position.y-place.y)>.6f))return false;
            return CombatCover.Clear(centre+Vector3.up*.9f,place+Vector3.up*.9f);
        }
        /// <summary>How far a ship's middle stays from the middle of what it is fighting: the old 2.2 m, or, for a body that is more than that, its hull and the body's (its capsule, or what it
        /// draws if wider) and half a metre between them. At 2.2 Sela's hull sat inside the Drifter she was shooting (nov20-whole s10b/089; BodiesKeepApartTests). A part of a bigger
        /// body (a collar's link, a pod) is closed on as before.</summary>
        float ShipStandOff(Health target)
        {
            if(target==null||target.reactionOwner!=null||!target.TryGetComponent<EnemyBrain>(out var body))return 2.2f;
            return Mathf.Max(2.2f,HeroCollision.HullRadius(actor.character)+body.BodyRadius+.5f);
        }
        // ---------- in flight: coming to rest ----------
        /// <summary>Whether a ship closing on where it means to rest has to brake now to stop there: the brake damps at 9 a second, so from her closing speed (on it, less the lead's,
        /// whose place it is) she needs about a ninth of it in metres, and it begins with room to spare. Braked only within two metres of where she was going, she came off the lead's
        /// way at 27 m/s, ran six metres past her place behind him and came back to it down the line of his next leg (the Picket's pocket 9, nov21-recon1 s45 and s45b).</summary>
        bool Arriving(CombatActor lead,Vector3 rest,float stop)
        {
            var toward=Planar(rest-transform.position);float far=toward.magnitude;if(far<.01f)return false;
            var own=Planar(actor.motor.Velocity);var relative=own-Planar(lead.motor.Velocity);float closing=Vector3.Dot(relative,toward/far);
            // Only while she is at least at his pace: slower than a lead setting off, she closed on her place only because it ran from her sideways, and braking her lost her more
            // ground (route 17's leg past barge two's rest; PartnerInViewTests).
            if(closing>4&&far<stop+2+closing/6&&own.magnitude>=Planar(lead.motor.Velocity).magnitude-1)return true;
            // Faster than a cruise and the way to her place well off her travel: off the lead's way at 27 m/s, the way to her place was across it, she never closed on it, and ran six
            // metres past it sideways. Her own travel, not his: at rest behind a lead flying off, she would read as going backwards at his speed. Only near it: far behind a lead who has turned
            // (the Aurora Shear's short lane, PartnerInViewTests) braking for the turn only left her further behind.
            return own.magnitude>FastTurn&&Vector3.Angle(own,toward)>45&&far<stop+TurnBrakeWithin;
        }
        /// <summary>How near her place a turn she can't make at speed is braked for.</summary>
        const float TurnBrakeWithin=6;
        /// <summary>The speed past a hull's cruise (12 m/s) above which a ship can't turn onto a way well off its travel.</summary>
        const float FastTurn=15;
        // ---------- keeping up with a lead on the move ----------
        /// <summary>How fast the lead goes before she counts him on the move; how close she follows her place while he is; how far behind it she falls before she boosts (in flight)
        /// or sprints (on foot) to it, and how near it she comes before she lets that go.</summary>
        const float MovingLead=1,MovingStop=.6f,HurryFlight=.8f,HurryFoot=1f,HurryEnds=.3f;
        /// <summary>How fast a ship goes before she counts it travelling: a regroup's braked approach (2.1 m/s) is the lead coming to her, not a leg (route 10; PartnerFlightRejoinTests).</summary>
        const float FlightTravel=4;
        /// <summary>The lead's travel as it is, from where he was a frame ago, smoothed: his motor's own velocity leaves out what carries him besides (the Aurora Shear's currents carried
        /// Taren at 20 m/s on a 14 m/s cruise, and keeping his motor's pace she was left ten metres behind on the short lane; PartnerInViewTests). A placing reads as his motor's.</summary>
        Vector3 LeadTravel(CombatActor lead)
        {
            // Over the time since she last looked, which isn't always a frame (a roll, a brace and a stagger skip her reading).
            var at=lead.transform.position;float now=Time.time,dt=now-leadAt;
            if(lead!=leadWasOf||leadAt<0||dt<=0||dt>.25f){leadWasOf=lead;leadWas=at;leadAt=now;leadActual=Planar(lead.motor.Velocity);return leadActual;}
            var moved=Planar(at-leadWas)/dt;leadWas=at;leadAt=now;if(moved.magnitude>40)moved=Planar(lead.motor.Velocity);
            leadActual=Vector3.Lerp(leadActual,moved,1-Mathf.Exp(-12*dt));return leadActual;
        }
        Vector3 leadWas,leadActual;CombatActor leadWasOf;float leadAt=-1;
        // ---------- carried by what isn't her ----------
        /// <summary>How fast something besides their own thrust carries her and the lead (a current of the Aurora Shear): what each actually moved, less what its motor flew, smoothed
        /// over a sixth of a second (CarryRead); more than <see cref="CarryMost"/> (a placing) reads as nothing.</summary>
        void Carried(CombatActor lead)
        {
            float now=Time.time,dt=now-ownAt;var at=transform.position;
            if(ownAt<0||dt<=0||dt>.25f||!actor.flight){ownAt=now;ownWas=at;ownCarry=leadCarry=Vector3.zero;return;}
            var drift=Planar(at-ownWas)/dt-Planar(actor.motor.Velocity);ownWas=at;ownAt=now;if(drift.magnitude>CarryMost)drift=Vector3.zero;
            var his=leadActual-Planar(lead.motor.Velocity);if(his.magnitude>CarryMost)his=Vector3.zero;
            float k=1-Mathf.Exp(-CarryRead*dt);ownCarry=Vector3.Lerp(ownCarry,drift,k);leadCarry=Vector3.Lerp(leadCarry,his,k);
        }
        Vector3 ownWas,ownCarry,leadCarry;float ownAt=-1;
        /// <summary>How quickly a carry is read (per second): over half a second, a current she came into reached her thrust a second late, and it took her on past her place.</summary>
        const float CarryMost=12,AdriftFrom=2.5f,CarryRead=6;
        /// <summary>How far past her place along his travel she eases off, and to what share of his pace.</summary>
        const float PastPlace=1,EaseOff=.8f;
        /// <summary>Whether a current carries her and not the lead, and by how much more. On the Aurora Shear's short lane he turned out of the current at its first bend while it carried
        /// her on, at six metres a second, into the crystals beyond it; she braked for them and was left fifteen metres behind, off the frame (PartnerInViewTests). A player leans into a
        /// current to leave it, at a boost: so does she (Update), and nearing her place doesn't brake her while it carries her.</summary>
        bool Adrift(out Vector3 drift)
        {
            drift=ownCarry-leadCarry;
            return actor.flight&&ownCarry.magnitude>AdriftFrom&&drift.magnitude>AdriftFrom;
        }
        /// <summary>The newest point of the lead's own way (KeepLeadsWay) she can fly to straight, within <see cref="PathWithin"/>: where he flew is open, and it leaves the current
        /// where he left it. With none, where she was going.</summary>
        Vector3 HisPath(Vector3 goal)
        {
            var lift=Vector3.up*.8f;float hull=HeroCollision.HullRadius(actor.character);var from=transform.position;
            for(int i=leadsWay.Count-1;i>=0;i--)
            {
                var point=leadsWay[i];point.y=from.y;if(Planar(point-from).magnitude>PathWithin)continue;
                if(!CombatCover.Sweep(from+lift,point+lift,hull,out _))return point;
            }
            return goal;
        }
        /// <summary>How far off a point of his way she looks for one to leave a current by; and how much slower than him at her place she boosts to his pace.</summary>
        const float PathWithin=30,PaceGap=1.5f;
        /// <summary>How far ahead of a travelling lead his way is kept free of a place picked for the frame.</summary>
        const float OnHisWay=14;
        /// <summary>In flight beside a travelling lead: how far behind his middle her place is, and how far she must be on his other side before it changes sides.</summary>
        const float BesideBack=.5f,BesideSwitch=1.5f;
        /// <summary>How many seconds of his travel ahead of her place beside him must be open.</summary>
        const float LaneAhead=.5f;
        int besideSide;
        /// <summary>Her place in flight while the lead travels (and isn't flying at her): beside him, a little behind, out of his hull, on the side she is on, and kept there until she
        /// is clearly on the other. Her place behind his nose swung to his far side with every turn he made setting off, and the way to it, round him, left her eight to ten metres
        /// out for a second and a half each time, off the frame or under the control strip (route 17's leg past barge two's rest, the Aurora Shear's short lane; PartnerInViewTests).
        /// Beside him a turn moves her place a few metres, and a body beside the lead stays in the camera's frame as one behind him going up the screen does not.</summary>
        Vector3 Beside(CombatActor lead,Vector3 travel)
        {
            var way=travel.normalized;var right=Vector3.Cross(Vector3.up,way);
            float across=Vector3.Dot(Planar(transform.position-lead.transform.position),right);
            int was=besideSide;
            if(besideSide==0||Mathf.Abs(across)>BesideSwitch)besideSide=across<0?-1:1;
            if(besideSide!=was&&was!=0)besideHeld=Time.time+BesideHold;
            // A side with something solid in it, or ahead of it along his travel for the next half second, is no place: the other side, but not more often than BesideHold (with
            // panes passing on both sides of route 17's lane she crossed behind him from one side to the other and back, nine metres out). With her side shut and no change allowed,
            // its place while the way out to it is open, else her place behind his nose, on his own line (Rock 8 beside the Picket's pocket 9; beside him down the Aurora Shear's
            // short lane she met a crystal his own line missed, braked for it to a crawl and was left fifteen metres behind).
            var lift=Vector3.up*.8f;float hull=HeroCollision.HullRadius(actor.character),ahead=Mathf.Max(3,travel.magnitude*LaneAhead);var from=lead.transform.position+lift;
            Vector3 At(int side){var p=lead.transform.position-way*BesideBack+right*(side*Apart(lead));p.y=transform.position.y;return p;}
            bool Out(Vector3 p)=>!CombatCover.Sweep(from,p+lift,hull,out _);
            bool Lane(Vector3 p)=>!CombatCover.Sweep(p+lift,p+way*ahead+lift,hull,out _);
            var mine=At(besideSide);
            if(Out(mine)&&Lane(mine))return mine;
            var other=At(-besideSide);
            if(Time.time>=besideHeld&&Out(other)&&Lane(other)){besideSide=-besideSide;besideHeld=Time.time+BesideHold;return other;}
            if(Out(mine))return mine;
            var behind=lead.transform.position-lead.motor.Facing*3+Vector3.Cross(Vector3.up,lead.motor.Facing).normalized*2;behind.y=transform.position.y;
            return behind;
        }
        float besideHeld;
        /// <summary>The least time between two changes of her side beside a travelling lead.</summary>
        const float BesideHold=1.5f;
        bool hurrying;
        /// <summary>Whether she goes faster than he does to catch up her place. It is three metres behind him, and while he travelled she trailed it by seven to nine metres and never
        /// closed: at his own pace on foot, and in flight slower than his cruise (below Taren's 13.76 m/s, until PartyController.RefreshStats) and braked to a crawl within two
        /// metres of a place that had already gone on. So whenever he went up the screen she was under the control strip or the skill icons or off the bottom edge (nov22-whole s17/229,
        /// s17/102, s12/055, s23/159, s32/123 in flight; s33/104, s34/116, s34/068, s16/144 on foot). A little behind it she boosts or sprints, to within a little of it, and then keeps
        /// his pace (Pace).</summary>
        bool KeepUp(bool onTheMove,float behind,float stop)
        {
            if(!onTheMove){hurrying=false;return false;}
            hurrying=hurrying?behind>stop+HurryEnds:behind>stop+(actor.flight?HurryFlight:HurryFoot);
            return hurrying;
        }
        /// <summary>The stick that keeps her at his pace along his travel: in flight the thrust whose speed against drag is his (full until she is up to it); on foot his walk or his jog
        /// in civil form, a combat stride's share of hers.</summary>
        Vector2 Pace(Vector3 travel)
        {
            float speed=travel.magnitude;if(speed<.01f)return Vector2.zero;var way=travel/speed;float tilt=1;
            if(actor.motor is FlightMotor ship)tilt=Planar(ship.Velocity).magnitude<speed-.5f?1:Mathf.Clamp01(speed*Mathf.Max(.01f,ship.drag)/Mathf.Max(.01f,ship.acceleration));
            else if(actor.motor is GroundMotor ground)
            {
                bool civil=TryGetComponent<FormController>(out var forms)&&forms.Current==Lattice.Data.BodyForm.Natural;
                tilt=civil?(speed<(GroundMotor.NaturalWalk+GroundMotor.NaturalJog)*.5f?Mathf.Min(GroundMotor.WalkTilt*.98f,speed*GroundMotor.WalkTilt/GroundMotor.NaturalWalk):1):Mathf.Clamp01(speed/Mathf.Max(.1f,ground.speed));
            }
            return new Vector2(way.x,way.z)*tilt;
        }
        /// <summary>Her way to her place in flight round the lead, not through him or ahead of his nose: with him between (a hull's width of the straight way), a place beside him on
        /// the side the way already leans to, two hulls and a little off. In the Picket's pocket 9 his nose turned down leg 10 put her place across him, and the straight way to it ran
        /// down leg 10's line while he waited to fly it (nov21-recon1 s45, s45b; with D354 too, where nothing of hers stops him, she crept back along it ahead of his nose). Where she
        /// belongs, not his way through: that is never hers to decide (D354).</summary>
        Vector3 ShipRoundTheLead(CombatActor lead,Vector3 goal)
        {
            var from=transform.position;var way=Planar(goal-from);float length=way.magnitude;if(length<.5f)return goal;
            var to=Planar(lead.transform.position-from);float t=Vector3.Dot(to,way)/(length*length);if(t<=.05f||t>=.95f)return goal;
            var closest=from+way*t;float room=HeroCollision.HullRadius(lead.character)+HeroCollision.HullRadius(actor.character)+.4f;
            var away=Planar(closest-lead.transform.position);if(away.magnitude>=room)return goal;
            if(away.sqrMagnitude<.01f)away=Vector3.Cross(Vector3.up,way/length);away.Normalize();
            // The side the way leans to unless something solid stands on it (Rock 8 beside pocket 9: round its side she stopped short on leg 10's line): then the other.
            float hull=HeroCollision.HullRadius(actor.character);var lift=Vector3.up*.8f;Vector3 first=default;
            for(int pass=0;pass<2;pass++,away=-away)
            {
                var round=lead.transform.position+away*(room+.5f);round.y=from.y;if(pass==0)first=round;
                if(!CombatCover.Sweep(from+lift,round+lift,hull,out _)&&!CombatCover.Sweep(round+lift,goal+lift,hull,out _))return round;
            }
            return first;
        }
        /// <summary>A lead flying toward her (his travel within 40 degrees of her, inside 30 m) is coming to her, or through where she is: her way to her place doesn't go round him then.
        /// Going round him she kept ten metres off a lead flying up to her at a regroup's braked pace for all eight seconds, circling as he turned (route 10's "regroup beside the measured
        /// hulls", nov21-whole s10 to s10c). She comes to her place through him, which nothing of hers can stop (D354).</summary>
        bool LeadComing(CombatActor lead)
        {
            var moving=Planar(lead.motor.Velocity);if(moving.sqrMagnitude<2.25f)return false;
            var to=Planar(transform.position-lead.transform.position);float far=to.magnitude;
            return far>.01f&&far<=30&&Vector3.Angle(moving,to)<=40;
        }
        /// <summary>On foot, whether the floor runs on from where she stands for <see cref="FloorAhead"/> along this way (in flight, always): no edge, no wall, at her own level.</summary>
        bool FloorAlong(Vector3 way)
        {
            if(actor.flight||way.sqrMagnitude<.01f)return true;
            if(GroundNavigation.Current==null||!NavMesh.SamplePosition(transform.position,out var here,.6f,NavMesh.AllAreas))return GroundNavigation.Current==null;
            var ahead=here.position+Planar(way).normalized*FloorAhead;
            if(NavMesh.Raycast(here.position,ahead,out _,NavMesh.AllAreas))return false;
            return NavMesh.SamplePosition(ahead,out var there,.4f,NavMesh.AllAreas)&&Mathf.Abs(there.position.y-here.position.y)<=.5f;
        }
        const float FloorAhead=1.5f;
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
            return Inside(camera,feet,edge)&&Inside(camera,head,edge)&&!HudKeepOut.Covers(feet,PlateGap)&&!HudKeepOut.Covers(waist,PlateGap)&&!HudKeepOut.Covers(head,PlateGap)&&
                (!restingInSight||InSight(camera,at));
        }
        bool restingInSight;
        static readonly RaycastHit[] Sight=new RaycastHit[16];
        /// <summary>Whether the camera sees her whole where she stands: nothing drawn and solid between it and her waist, nor her head. Coming to rest behind the lead as he stepped out of
        /// Tallow Drift's pressure lift she stood in its door, an outline through its frame with the lift's sign across her (nov22-whole s16/144). An unseen wall (no renderer) hides nothing.</summary>
        public static bool InSight(Camera camera,Vector3 at)=>!Hidden(camera.transform.position,at+Vector3.up*.9f)&&!Hidden(camera.transform.position,at+Vector3.up*1.6f);
        static bool Hidden(Vector3 eye,Vector3 point)
        {
            var to=point-eye;float far=to.magnitude-.3f;if(far<=0)return false;
            int count=Physics.RaycastNonAlloc(eye,to.normalized,Sight,far,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var c=Sight[i].collider;if(!CombatCover.Opaque(c))continue;
                var drawn=c.GetComponent<Renderer>();if(drawn==null)drawn=c.GetComponentInParent<Renderer>();
                if(drawn!=null&&drawn.enabled&&drawn.gameObject.activeInHierarchy)return true;
            }
            return false;
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
            // Never the ground the lead stands on (MeleeStand). In flight OutOfTheLead keeps where she rests out of his hull.
            if(!actor.flight&&Planar(lead.transform.position-place).magnitude<HeroesApart)return false;
            // Nor, while he travels, a place on his way ahead of him (D354): a place in the frame is beside him or behind him.
            var travel=leadActual;
            if(travel.magnitude>(actor.flight?FlightTravel:MovingLead)&&!LeadComing(lead))
            {
                var off=Planar(place-lead.transform.position);var way=travel.normalized;float ahead=Vector3.Dot(off,way);
                if(ahead>0&&ahead<OnHisWay&&Mathf.Abs(Vector3.Dot(off,Vector3.Cross(Vector3.up,way)))<Apart(lead))return false;
            }
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
                // A marking can outlive what drew it by a few frames (an Interceptor shot down as it scatters): its owner is then a destroyed object, and reading where
                // it stands threw on every frame until the marking went (eleven frames in the Broken Ring, November 22).
                if(marking==null||marking.Released||!(marking.owner is Component drewIt&&drewIt!=null)||!(marking.owner is BossController||actor.flight&&Hostile(marking.owner)))continue;
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
