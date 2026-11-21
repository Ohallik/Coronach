using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The guardian's <b>pair</b> (<c>tune.pair</c>: chapter 8's Muster Pikeguard, clan conscripts on the Scoria Flows' crossings and the Foundry's floors). Brekk says the whole of it: "They
    /// fight in twos. One braces, one covers. Get between them."
    /// <list type="bullet">
    /// <item>Two of one kind woken within <see cref="Gathers"/> of each other pair up and stand <see cref="PairGap"/> apart, side by side, facing the party as one and advancing at a walk
    /// (no further than their post).</item>
    /// <item><b>One braces</b>: it turns on the hero, planted, and thrusts its pike down a marked line (attacks[1]); then its pike is in the floor and its plate down for <c>tune.recover</c>,
    /// as any guardian's after its long move. They take turns. <b>The other covers</b>: it turns its plate to the hero, braces nothing while its partner's pike is down, and slams
    /// (attacks[0]) a hero who comes inside its reach. So the guardian's own answer, its flank while its pike is down, is in front of the one covering it.</item>
    /// <item><b>Neither strikes across the other</b>: a slam or a thrust whose marking would hold its partner isn't started. So a hero standing <b>between them</b> is struck by neither, and
    /// from there both plates face away from him (each blow on either lands whole). For <see cref="Backs"/> seconds; then they step apart, <see cref="Spread"/> more between them, and turn
    /// in on him, and at that width a slam no longer reaches across to the other: time to be out from between them.</item>
    /// <item>With one of them down the other fights alone, as a lone guardian does; a third of the kind fights alone from the start.</item>
    /// </list>
    /// Logs <c>PAIR_FORMED</c>, <c>PAIR_BRACE</c>, <c>PAIR_SPLIT</c> (a hero has got between them), <c>PAIR_REFORM</c> (they back off).
    /// </summary>
    public sealed partial class EnemyGuardian
    {
        /// <summary>How far apart a pair stands, centre to centre (a hero fits between two .75 m bodies with a stride to spare), and how long a hero between them is let be before they back off.</summary>
        public const float PairGap=4.2f,Backs=1.6f,Spread=2;
        sealed class Duo
        {
            public EnemyGuardian left,right;public Vector3 centre,facing,home;public int frame=-1,thrusts;public bool between;public float betweenSince=-1,spread;
        }
        /// <summary>How far apart the pair stands now, centre to centre: its gap, and more while it spreads round a hero between it.</summary>
        public float Gap=>duo!=null?PairGap+Spread*duo.spread:PairGap;
        static readonly List<Duo> duos=new();
        Duo duo;
        bool Paired=>tune.pair;
        EnemyGuardian Partner=>duo==null?null:duo.left==this?duo.right:duo.left;
        /// <summary>Its partner, if it has one standing: what a check reads.</summary>
        public EnemyGuardian PairedWith{get{var partner=Partner;return partner!=null&&partner.health.Alive?partner:null;}}
        /// <summary>Whose turn it is to brace: the pair takes turns, thrust by thrust.</summary>
        public bool Bracing=>duo!=null&&(duo.thrusts%2==0?duo.left==this:duo.right==this);
        /// <summary>A hero stands between the two of them, where neither will strike.</summary>
        public bool HeroBetween=>duo!=null&&duo.between;
        bool Pair(CombatActor victim)
        {
            if(duo==null)JoinPair();
            var partner=PairedWith;if(partner==null)return false;
            if(duo.frame!=Time.frameCount)LeadPair(victim);
            // Its side of the pair, across the way the pair faces: the left one to the left.
            var across=Vector3.Cross(Vector3.up,duo.facing);var slot=duo.centre+across*((duo.left==this?-.5f:.5f)*Gap);var toSlot=Flat(slot-body.position);
            if(toSlot.magnitude>.35f)brain.Walk(toSlot,def.speed*1.4f);
            var to=Flat(victim.transform.position-body.position);float distance=to.magnitude;
            // Between them: plates kept the way the pair faces, and nothing struck at him, until they have stepped apart.
            if(duo.between&&duo.spread<.9f){Face(duo.facing,Turn);return true;}
            bool partnerDown=partner.brain.Telegraphing||Time.time<partner.loweredUntil;
            if(Bracing&&!partnerDown)
            {
                // The bracer turns on him where it stands and thrusts down a line.
                Face(to,Turn);
                if(brain.Rested&&distance>=Range*.8f&&distance<Reach&&Vector3.Angle(body.forward,to)<10&&brain.Sees(victim)&&!InLine(partner,body.forward,Reach))
                {Long(victim,body.forward,Second);duo.thrusts++;Debug.Log("PAIR_BRACE "+def.id);return true;}
            }
            // The cover: its plate to him, quicker to turn, and its slam for whoever comes in reach.
            else Face(to,Turn*1.3f);
            if(brain.Rested&&distance<Range&&Vector3.Angle(body.forward,to)<40&&brain.Sees(victim)&&!InSlam(partner,body.forward)){Slam(victim,body.forward);return true;}
            return true;
        }
        /// <summary>Once a frame, by whichever of the two ticks first: whether a hero stands between them, where they face and stand.</summary>
        void LeadPair(CombatActor victim)
        {
            duo.frame=Time.frameCount;
            Vector3 a=duo.left.body.position,b=duo.right.body.position,middle=(a+b)*.5f;
            bool between=false;var ab=Flat(b-a);float length=ab.magnitude;
            if(length>.5f&&PartyController.Current!=null)
                foreach(var hero in PartyController.Current.members)
                {
                    if(!hero.Health.Alive)continue;var ah=Flat(hero.transform.position-a);float t=Vector3.Dot(ah,ab)/(length*length);
                    float off=Mathf.Abs(ab.x*ah.z-ab.z*ah.x)/length;
                    if(t>.15f&&t<.85f&&off<1.4f){between=true;break;}
                }
            if(between&&!duo.between){duo.betweenSince=Time.time;Debug.Log("PAIR_SPLIT "+def.id);}
            duo.between=between;
            var to=Flat(victim.transform.position-duo.centre);float distance=to.magnitude;
            float pace=def.speed;foreach(var member in new[]{duo.left,duo.right})pace=Mathf.Min(pace,def.speed*member.health.Pace);
            // Held: neither strikes across the other. Past Backs they step apart round him, and close up again once nobody is between them.
            bool spreading=between&&Time.time-duo.betweenSince>Backs;
            if(spreading&&duo.spread<=0)Debug.Log("PAIR_REFORM "+def.id);
            duo.spread=Mathf.MoveTowards(duo.spread,spreading?1:0,Time.deltaTime/.9f);
            if(!between)
            {
                if(distance>.1f)duo.facing=Vector3.RotateTowards(duo.facing,to/distance,Turn*RankTurn*Mathf.Deg2Rad*Time.deltaTime,0);
                if(distance>Range*.9f&&Flat(victim.transform.position-duo.home).magnitude<Post+Range)duo.centre+=to/distance*Mathf.Min(distance-Range*.9f,pace*Time.deltaTime);
            }
            var lag=Flat(duo.centre-middle);if(lag.magnitude>1.2f)duo.centre=middle+Vector3.up*(duo.centre.y-middle.y)+lag.normalized*1.2f;
            var homeward=Flat(duo.centre-duo.home);if(homeward.magnitude>Post)duo.centre-=homeward.normalized*(homeward.magnitude-Post);
        }
        void JoinPair()
        {
            foreach(var other in duos)
            {
                if(other.left==null||other.right!=null||other.left==this||other.left.def.id!=def.id||!other.left.health.Alive)continue;
                if(Flat(other.left.body.position-body.position).magnitude>Gathers)continue;
                duo=other;break;
            }
            if(duo==null){duo=new Duo{left=this,centre=body.position,home=body.position,facing=Flat(body.forward).sqrMagnitude>.01f?Flat(body.forward).normalized:Vector3.forward};duos.Add(duo);return;}
            duo.right=this;
            // Left and right as the pair faces, so each walks to its own side.
            var across=Vector3.Cross(Vector3.up,duo.facing);if(Vector3.Dot(duo.right.body.position-duo.left.body.position,across)<0)(duo.left,duo.right)=(duo.right,duo.left);
            duo.centre=duo.home=(duo.left.body.position+duo.right.body.position)*.5f;
            Debug.Log("PAIR_FORMED "+def.id);
        }
        void LeavePair()
        {
            if(duo==null)return;var was=duo;duo=null;
            if(was.left==this){was.left=was.right;was.right=null;}else if(was.right==this)was.right=null;
            was.between=false;if(was.left==null)duos.Remove(was);
        }
        /// <summary>Ordered to brace at a hero now (Vessa Korl's "Pikes."): it turns on him and thrusts down its marked line, unless it is already telling a move, its pike is in the floor,
        /// it is broken, or the line would hold its partner. Its own tick then lands the thrust as any other. Answers whether it braced.</summary>
        public bool Brace(CombatActor victim)
        {
            if(victim==null||!health.Alive||health.Broken||brain.Passive||!brain.enabled||brain.Telegraphing||sweeping||Time.time<loweredUntil||Second==attack)return false;
            var to=Flat(victim.transform.position-body.position);if(to.sqrMagnitude<.01f)return false;var aim=to.normalized;
            var partner=PairedWith;if(partner!=null&&InLine(partner,aim,Reach))return false;
            body.rotation=Quaternion.LookRotation(aim);Long(victim,aim,Second);if(duo!=null)duo.thrusts++;Debug.Log("PAIR_BRACE "+def.id+" ordered");return true;
        }
        /// <summary>Whether its partner stands in the line it would thrust down, or in the circle it would slam: neither is started across the other.</summary>
        bool InLine(EnemyGuardian partner,Vector3 aim,float length)
        {
            var off=Flat(partner.body.position-body.position);float along=Vector3.Dot(off,aim),aside=Mathf.Abs(aim.x*off.z-aim.z*off.x);
            float girth=partner.controller!=null?partner.controller.radius:.6f;
            return along>-girth&&along<length+girth&&aside<Width*.5f+girth;
        }
        bool InSlam(EnemyGuardian partner,Vector3 aim)
        {
            var centre=body.position+aim*(Range*.55f);float radius=attack!=null&&attack.radius>0?attack.radius:2.2f;
            float girth=partner.controller!=null?partner.controller.radius:.6f;
            return Flat(partner.body.position-centre).magnitude<radius+girth;
        }
    }
}
