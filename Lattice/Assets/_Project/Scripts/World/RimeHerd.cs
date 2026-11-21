using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// A herd on the move that the party has to walk through: the White Reach's rime-beasts streaming south through the gap in the serac ridge. Each beast walks <see cref="line"/> at
    /// <see cref="speed"/>, its feet on the line's own heights, and stops for anyone in its way; at the line's end it is put back at its start, both out of sight. To a herd, running is an
    /// order (Idra): a dodge, a sprint, a blow or a skill from a hero within <see cref="startle"/> of any beast sets it off. Every beast then marks the lane ahead of it for
    /// <see cref="warning"/> seconds in the danger colour and bolts down it for <see cref="boltSeconds"/>; a hero in a bolting beast's lane is knocked aside and loses
    /// <see cref="trample"/> of their integrity (the place's damage: no source, never the last of it, and nothing through a dodge), once a bolt. Then it settles and walks on. Nothing
    /// of it is saved: a herd is weather. Logs <c>HERD_STARTLED who</c>, <c>HERD_TRAMPLED who</c>, <c>HERD_SETTLED</c>.
    /// </summary>
    public sealed class RimeHerd:MonoBehaviour
    {
        /// <summary>The beasts' bodies (kit pieces, no rig: each is given a walking bob here).</summary>
        public Transform[] beasts=System.Array.Empty<Transform>();
        /// <summary>The line the herd walks, at ground height, a metre or so between points.</summary>
        public Vector3[] line=System.Array.Empty<Vector3>();
        /// <summary>Each beast's place across the line (metres, + to its right) and how far along it starts.</summary>
        public float[] across=System.Array.Empty<float>(),along=System.Array.Empty<float>();
        public float speed=1.3f,startle=7,warning=.8f,boltSpeed=8,boltSeconds=2.4f,laneWidth=2.2f,trample=.06f,knock=4;
        public enum State{Walking,Warning,Bolting}
        public State Now{get;private set;}
        public int Startles{get;private set;}
        float[] at;float timer;float length;float[] lengths;Telegraph[] lanes;bool[] struck;Transform[] drawn;Vector3[] restAt;Quaternion[] restTurn;
        void Start()
        {
            lengths=new float[line.Length];for(int i=1;i<line.Length;i++)lengths[i]=lengths[i-1]+Vector3.Distance(Flat(line[i-1]),Flat(line[i]));
            length=line.Length>1?lengths[^1]:0;
            at=new float[beasts.Length];lanes=new Telegraph[beasts.Length];struck=new bool[3];drawn=new Transform[beasts.Length];restAt=new Vector3[beasts.Length];restTurn=new Quaternion[beasts.Length];
            for(int i=0;i<beasts.Length;i++)
            {
                at[i]=i<along.Length?along[i]:length*i/Mathf.Max(1,beasts.Length);
                drawn[i]=beasts[i]!=null&&beasts[i].childCount>0?beasts[i].GetChild(0):null;
                if(drawn[i]!=null){restAt[i]=drawn[i].localPosition;restTurn[i]=drawn[i].localRotation;}
                Place(i,0);
            }
        }
        static Vector3 Flat(Vector3 v)=>new(v.x,0,v.z);
        /// <summary>The point on the line at a distance along it, and the way it runs there.</summary>
        public Vector3 On(float s,out Vector3 forward)
        {
            forward=Vector3.forward;if(line.Length<2)return line.Length==1?line[0]:transform.position;
            s=Mathf.Repeat(s,length);int k=1;while(k<line.Length-1&&lengths[k]<s)k++;
            float t=Mathf.InverseLerp(lengths[k-1],lengths[k],s);forward=Flat(line[k]-line[k-1]).normalized;
            return Vector3.Lerp(line[k-1],line[k],t);
        }
        void Place(int i,float bob)
        {
            var beast=beasts[i];if(beast==null)return;
            var p=On(at[i],out var forward);var right=new Vector3(forward.z,0,-forward.x);float side=i<across.Length?across[i]:0;
            beast.SetPositionAndRotation(p+right*side,Quaternion.LookRotation(forward));
            // A stiff-legged walk read from a body with no legs to move: a nod and a roll in time with the stride.
            if(drawn[i]!=null){drawn[i].localPosition=restAt[i]+Vector3.up*Mathf.Abs(Mathf.Sin(bob))*.07f;drawn[i].localRotation=Quaternion.Euler(Mathf.Sin(bob*2)*1.6f,0,Mathf.Sin(bob)*2.2f)*restTurn[i];}
        }
        bool InTheWay(int i,Vector3 forward)
        {
            var party=PartyController.Current;if(party==null)return false;
            var front=beasts[i].position+forward*1.9f;
            foreach(var hero in party.members)
            {
                if(hero==null||!hero.isActiveAndEnabled)continue;var gap=Flat(hero.transform.position-front);if(gap.magnitude<1.5f)return true;
            }
            // And for the beast ahead, so a stopped beast holds up the ones behind it rather than walking into its back.
            for(int j=0;j<beasts.Length;j++)
            {
                if(j==i||beasts[j]==null)continue;var gap=Flat(beasts[j].position-front);if(gap.magnitude<1.3f&&Vector3.Dot(gap,forward)>-.2f)return true;
            }
            return false;
        }
        /// <summary>Whoever is doing something a herd takes for an order, close enough to be heard.</summary>
        CombatActor Startled()
        {
            var party=PartyController.Current;if(party==null)return null;
            foreach(var hero in party.members)
            {
                if(hero==null||!hero.isActiveAndEnabled||!hero.Health.Alive)continue;
                bool near=false;foreach(var beast in beasts)if(beast!=null&&Flat(beast.position-hero.transform.position).magnitude<startle){near=true;break;}
                if(!near)continue;
                // What the hero asks of their feet, not how far they moved: a body set down somewhere, or shoved, isn't running.
                var state=hero.State;bool sprinting=hero.motor is GroundMotor ground&&Flat(ground.DesiredVelocity).magnitude>ground.speed*1.2f;
                if(state==ActorState.Dodge||state==ActorState.Attack||state==ActorState.Skill||sprinting)return hero;
            }
            return null;
        }
        void Update()
        {
            if(GameTime.Paused||beasts.Length==0||length<=0)return;
            float dt=Time.deltaTime;timer+=dt;
            switch(Now)
            {
                case State.Walking:
                {
                    var who=Startled();
                    if(who!=null){Bolt(who);break;}
                    for(int i=0;i<beasts.Length;i++)
                    {
                        if(beasts[i]==null)continue;On(at[i],out var forward);
                        if(!InTheWay(i,forward))at[i]+=speed*dt;
                        if(at[i]>=length)at[i]-=length;
                        Place(i,(at[i]*2.6f)+i);
                    }
                    break;
                }
                case State.Warning:
                    if(timer>=warning){Now=State.Bolting;timer=0;for(int k=0;k<struck.Length;k++)struck[k]=false;AudioManager.PlayAt("root_groan",transform,beasts[0].position,.55f,AudioBus.SFX,60);}
                    break;
                case State.Bolting:
                {
                    for(int i=0;i<beasts.Length;i++)
                    {
                        if(beasts[i]==null)continue;at[i]+=boltSpeed*dt;if(at[i]>=length)at[i]-=length;Place(i,at[i]*4+i);
                        Trample(i);
                    }
                    if(timer>=boltSeconds)
                    {
                        Now=State.Walking;timer=0;
                        foreach(var lane in lanes)if(lane!=null){lane.Release();}
                        System.Array.Clear(lanes,0,lanes.Length);
                        Debug.Log("HERD_SETTLED");
                    }
                    break;
                }
            }
        }
        void Bolt(CombatActor who)
        {
            Now=State.Warning;timer=0;Startles++;
            Debug.Log("HERD_STARTLED "+who.character);
            for(int i=0;i<beasts.Length;i++)
            {
                if(beasts[i]==null)continue;On(at[i],out var forward);
                float run=Mathf.Min(boltSpeed*boltSeconds,12);
                lanes[i]=Telegraph.Line(beasts[i].position+Vector3.up*.05f,forward,run,laneWidth,warning+boltSeconds,Telegraph.Threat);
            }
        }
        void Trample(int i)
        {
            var party=PartyController.Current;if(party==null)return;
            On(at[i],out var forward);
            for(int k=0;k<party.members.Length&&k<struck.Length;k++)
            {
                var hero=party.members[k];if(hero==null||struck[k]||!hero.Health.Alive)continue;
                var gap=Flat(hero.transform.position-beasts[i].position);if(gap.magnitude>1.5f||Vector3.Dot(gap,forward)<-.5f)continue;
                // Only inside a lane that was marked, and not through a dodge's cover.
                if(lanes[i]==null||!lanes[i].Contains(hero.transform.position)||GameTime.Now<hero.Health.InvulnerableUntil)continue;
                struck[k]=true;var right=new Vector3(forward.z,0,-forward.x);float side=Vector3.Dot(gap,right)>=0?1:-1;
                float share=Mathf.Min(hero.Health.maximum*trample,Mathf.Max(0,hero.Health.integrity-1));
                hero.Health.Receive(new DamagePacket{amount=share,type=DamageType.Kinetic,knockback=(right*side+forward*.5f).normalized*knock,tag="herd"});
                Debug.Log("HERD_TRAMPLED "+hero.character);
            }
        }
        void OnDisable(){if(lanes!=null)foreach(var lane in lanes)if(lane!=null)lane.Cancel();}
    }
}
