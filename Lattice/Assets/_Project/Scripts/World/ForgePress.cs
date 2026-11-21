using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// A drop press over a way (chapter 8, the Bridle Foundry; docs/maps/BridleFoundry.md): the second wheel's cost. Quenched (its coolant standing in the tank that cools it) it hangs
    /// still with its ram up and steam at its seals (<see cref="quenched"/>), and the floor under it is any floor. Once <see cref="armFlag"/> is set (the tank drained) it runs on a clock
    /// a player reads: every <see cref="period"/> seconds the square under it is marked as the ram climbs to its top and shudders (<see cref="warn"/>), then the ram drops, strikes
    /// whoever is inside the marking (<see cref="strike"/> of his Integrity, never the last of it: the place's damage, no enemy its source; nothing through a dodge's cover) and throws
    /// him out of the near side; it holds down a moment (<see cref="hold"/>), climbs back over <see cref="rise"/>, and waits up the rest of the period. A save with the flag loads it
    /// running, with nothing played. Logs <c>PRESS_ARMED name</c>, <c>PRESS_STRUCK hero name</c>.
    /// </summary>
    public sealed class ForgePress:MonoBehaviour
    {
        /// <summary>The square a stamp strikes: from this object's place along <see cref="along"/> for <see cref="length"/>, <see cref="width"/> across.</summary>
        public Vector3 along=Vector3.forward;public float length=8,width=4;
        public float period=5,phase,warn=1.2f,hold=.4f,rise=1.1f;
        public string armFlag="";
        public float strike=.18f,throwClear=3;
        /// <summary>What is drawn: the ram (moved between its two world points), and what shows only while it is held (steam, the coolant's lamps).</summary>
        public Transform ram;public Vector3 ramUp,ramDown;public float climb=.25f;public GameObject quenched;
        public string sound="impactMetal_000";

        float started=float.NaN;Telegraph marking;bool armed,known,down;readonly bool[] struck=new bool[3];
        public bool Armed=>armed;
        /// <summary>Slams since it was armed in this visit: what a test counts.</summary>
        public int Stamps{get;private set;}
        public bool Marked=>marking!=null&&!marking.Released;
        public Telegraph Marking=>marking;
        public float Clock=>(float.IsNaN(started)?0:GameTime.Now-started)+phase;
        /// <summary>Seconds from a clock time to the next slam.</summary>
        public float UntilSlam(float time)=>Mathf.Repeat(warn-Mathf.Repeat(time,period),period);
        public bool Contains(Vector3 point)
        {
            var o=point-transform.position;o.y=0;var a=along;a.y=0;a=a.sqrMagnitude>.0001f?a.normalized:Vector3.forward;
            float on=Vector3.Dot(o,a),across=Mathf.Abs(Vector3.Dot(o,Vector3.Cross(Vector3.up,a)));
            return on>=-Telegraph.BodyAllowance&&on<=length+Telegraph.BodyAllowance&&across<=width*.5f+Telegraph.BodyAllowance;
        }
        void Start(){started=GameTime.Now;Pose(0);}
        void OnDisable(){if(marking!=null){marking.Clear();marking=null;}}
        void Update()
        {
            var services=GameServices.Current;if(services==null)return;
            bool armNow=string.IsNullOrEmpty(armFlag)||services.Flags.GetBool(armFlag);
            if(!known){known=true;armed=armNow;}
            if(armNow&&!armed){armed=true;started=GameTime.Now+phase;Debug.Log("PRESS_ARMED "+name);}
            if(quenched!=null&&quenched.activeSelf==armed)quenched.SetActive(!armed);
            if(!armed){Pose(0);return;}
            float t=Mathf.Repeat(Clock,period);
            if(t<warn)
            {
                // The tell: marked as the ram climbs past its rest to its top and shudders there.
                if(marking==null){var a=along;a.y=0;marking=Telegraph.Line(transform.position,a,length,width,Mathf.Max(.05f,warn-t),Telegraph.Threat);marking.owner=this;System.Array.Clear(struck,0,struck.Length);down=false;}
                float s=Mathf.SmoothStep(0,1,t/warn);Pose(-climb*s+(s>.6f?.03f*Mathf.Sin(Time.time*60):0));
                return;
            }
            if(!down){down=true;Slam();}
            if(t<warn+hold){Pose(1);return;}
            Pose(1-Mathf.SmoothStep(0,1,(t-warn-hold)/rise));
        }
        void Slam()
        {
            Stamps++;
            AudioManager.PlayAt(sound,transform,transform.position+Vector3.up,.5f,AudioBus.SFX,40,.06f,.7f);
            CombatVfx.Burst(transform.position+along.normalized*(length*.5f)+Vector3.up*.3f,new Color(.82f,.8f,.78f,.6f),"steam");
            var party=PartyController.Current;
            if(party!=null&&marking!=null&&marking.Elapsed>=.5f&&!GameTime.CombatHeld)
                for(int k=0;k<party.members.Length&&k<struck.Length;k++)
                {
                    var hero=party.members[k];if(hero==null||struck[k]||!hero.Health.Alive||hero.flight)continue;
                    var at=hero.transform.position;if(!marking.Contains(at)||GameTime.Now<hero.Health.InvulnerableUntil)continue;
                    struck[k]=true;
                    float share=Mathf.Min(hero.Health.maximum*strike,Mathf.Max(0,hero.Health.integrity-1));
                    if(share>0)hero.Health.Receive(new DamagePacket{amount=share,type=DamageType.Kinetic,tag="press"});
                    // Out of the nearer end, along its length: a press spans its passage wall to wall, and thrown across it a hero only met the wall still under the ram.
                    var a=along;a.y=0;a=a.normalized;float on=Vector3.Dot(at-transform.position,a);
                    if(hero.motor is GroundMotor ground)ground.Dash(on<length*.5f?-a:a,Mathf.Max(throwClear,Mathf.Min(on,length-on)+1.2f));
                    Debug.Log("PRESS_STRUCK "+hero.character+" "+name);
                }
            if(marking!=null){marking.Release();marking=null;}
        }
        /// <summary>The ram's place: 0 up (at rest), 1 down (on the floor), under 0 climbing past its rest for the tell.</summary>
        void Pose(float drop){if(ram!=null)ram.position=Vector3.LerpUnclamped(ramUp,ramDown,drop);}
    }
}
