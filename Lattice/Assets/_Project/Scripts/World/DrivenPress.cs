using System;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// A press of Cinder Halo's separation plant under a Compact override (chapter 9, the Separation Works; docs/maps/SeparationWorks.md, "The machines"). The zone's rule is that a white
    /// rig is clamped to whatever has been turned on the party, a cyan cable runs from the rig to the machine, and cutting the rig gives the machine back. This is the machine.
    /// <para><b>Driven</b> (its rig alive: <see cref="rigFlag"/> unset) it does one of two things. A hunting press (<see cref="pins"/> off) waits with its ram up until a hero steps into its
    /// bay, marks the bay (<see cref="warn"/>) as the ram climbs past its rest and shudders, and drops: whoever is inside the marking is struck, a hero for <see cref="strike"/> of his
    /// Integrity (no source: the place's damage, never the last of it, nothing through a dodge's cover) and thrown out of the nearer side, and anything else alive under it (a Compact crew,
    /// a Weld Arm) for <see cref="enemyShare"/> of its own, because iron doesn't know whose side it is on; a boss is left alone. Then it holds, climbs back and rests before it looks again.
    /// A pinning press holds its ram down on its bay (a joint's jack), <see cref="pinned"/> standing there as a solid block, and never hunts.</para>
    /// <para><b>Freed</b> (the flag set: the rig's encounter cleared) it goes to local control: the ram climbs to rest and stays there, <see cref="drivenLamps"/> go out and
    /// <see cref="localLamps"/> come on, and its <see cref="cable"/> drops slack and dark. A save with the flag loads it freed with nothing played. While driven, a hero standing within
    /// <see cref="showsWithin"/> of <see cref="showsAt"/> (the rig's post, or a pinned joint's hand) has the view drawn back over <see cref="shows"/>, so the rig, its cable and what it
    /// drives are seen together. Logs <c>PRESS_MARKS name</c>, <c>PRESS_STRUCK hero name</c>, <c>PRESS_CRUSHED enemy name</c>, <c>PRESS_FREED name</c>.</para>
    /// </summary>
    public sealed class DrivenPress:MonoBehaviour
    {
        static readonly Rect FramedIn=new(.06f,.18f,.88f,.74f);
        /// <summary>The bay a stamp strikes: from this object's place along <see cref="along"/> for <see cref="length"/>, <see cref="width"/> across.</summary>
        public Vector3 along=Vector3.forward;public float length=5,width=5;
        /// <summary>The rig's flag (its encounter's <c>clear.</c>): unset, the press is driven; set, it is home's again.</summary>
        public string rigFlag="";
        /// <summary>Pins rather than hunts: driven, the ram stands down on the bay and <see cref="pinned"/> (a Membrane holder: a box and a carving obstacle) is shut.</summary>
        public bool pins;public GameObject pinned;
        /// <summary>The hunt's beats: marked and climbing, held down, climbing back, resting before it looks again; how far the ram climbs past its rest for the tell.</summary>
        public float warn=.9f,hold=.35f,rise=.8f,rest=.7f,climb=.3f;
        public float strike=.18f,enemyShare=.3f,throwClear=3;
        /// <summary>What is drawn: the ram (moved between its two world points) and the two lamp sets.</summary>
        public Transform ram;public Vector3 ramUp,ramDown;public GameObject drivenLamps,localLamps;
        /// <summary>The cable from this press's control box to its rig, through <see cref="hangers"/> (world points, the press's end first and the rig's last, so its pulse runs from the rig
        /// to the press); <see cref="floor"/> is how low a slack span may hang.</summary>
        public LineRenderer cable;public Vector3[] hangers=Array.Empty<Vector3>();public float floor;
        public Color cableBody=new(.1f,.62f,.7f),cablePulse=new(.72f,1f,1f),cableDead=new(.16f,.17f,.18f);
        public Bounds shows;public Vector3 showsAt;public float showsWithin;
        public string sound="impactMetal_000",freedSound="forceField_000";

        enum Beat{Waiting,Marked,Down,Rising,Resting}
        Beat beat;float beatAt,dropAt=-1;Telegraph marking;bool known,freed,freedLive;float freedAt=-1;CameraRig rig;readonly bool[] struck=new bool[3];
        /// <summary>Whether its rig has been cut, for a test.</summary>
        public bool Freed=>freed;
        public bool Marked=>marking!=null&&!marking.Released;
        public Telegraph Marking=>marking;
        /// <summary>Slams since the zone opened, and enemies struck by them: what a test counts.</summary>
        public int Slams{get;private set;}
        public int Crushed{get;private set;}
        /// <summary>Whether its ram stands down on its bay holding it (a pinning press, driven).</summary>
        public bool Pinning=>pins&&!freed;
        /// <summary>Whether the ram is at rest, up, under local control (the climb after the rig is cut is over).</summary>
        public bool AtRest=>freed&&(freedAt<0||GameTime.Now-freedAt>=Lift);
        float Lift=>pins?rise*1.6f:rise;
        Vector3 Axis{get{var a=along;a.y=0;return a.sqrMagnitude>.0001f?a.normalized:Vector3.forward;}}
        Vector3 Centre=>transform.position+Axis*(length*.5f);
        public bool Contains(Vector3 point)
        {
            var o=point-transform.position;o.y=0;float on=Vector3.Dot(o,Axis),across=Mathf.Abs(Vector3.Dot(o,Vector3.Cross(Vector3.up,Axis)));
            return on>=-Telegraph.BodyAllowance&&on<=length+Telegraph.BodyAllowance&&across<=width*.5f+Telegraph.BodyAllowance;
        }
        void Start(){Pose(pins?1:0);DrawCable(0,0);}
        void OnDisable(){if(marking!=null){marking.Clear();marking=null;}}
        void Update()
        {
            var services=GameServices.Current;if(services==null)return;
            bool now=!string.IsNullOrEmpty(rigFlag)&&services.Flags.GetBool(rigFlag);
            if(!known){known=true;freed=now;if(freed)Settle();else Drive();}
            else if(now&&!freed)Free();
            if(freed){Rest();return;}
            DrawCable(GameTime.Now*.6f+transform.position.x*.05f,0);
            Show();
            if(pins){Pose(1);return;}
            Hunt();
        }
        /// <summary>Driven, as loaded: cyan lamps, the cable taut, a pin down.</summary>
        void Drive(){if(drivenLamps!=null)drivenLamps.SetActive(true);if(localLamps!=null)localLamps.SetActive(false);if(pinned!=null)pinned.SetActive(pins);Pose(pins?1:0);}
        /// <summary>Freed with nothing played: a save with the rig's flag.</summary>
        void Settle(){freedAt=-1;if(drivenLamps!=null)drivenLamps.SetActive(false);if(localLamps!=null)localLamps.SetActive(true);if(pinned!=null)pinned.SetActive(false);Pose(0);DrawCable(0,1);}
        /// <summary>The rig cut while the zone is open: the press lets go where it is and climbs to rest.</summary>
        void Free()
        {
            freed=true;freedLive=true;freedAt=GameTime.Now;dropAt=ramDrop;
            if(marking!=null){marking.Clear();marking=null;}
            if(drivenLamps!=null)drivenLamps.SetActive(false);if(localLamps!=null)localLamps.SetActive(true);
            AudioManager.PlayAt(freedSound,transform,Centre+Vector3.up*3,.4f,AudioBus.SFX,60,.06f,.55f);
            Debug.Log("PRESS_FREED "+name);
        }
        float ramDrop;
        void Rest()
        {
            if(!freedLive){Pose(0);return;}
            float t=Mathf.Clamp01((GameTime.Now-freedAt)/Mathf.Max(.05f,Lift));
            Pose(Mathf.Lerp(dropAt,0,Mathf.SmoothStep(0,1,t)));
            // A pinning press's block goes as its ram clears the jack, not before.
            if(pinned!=null&&pinned.activeSelf&&t>.45f)pinned.SetActive(false);
            DrawCable(0,Mathf.Clamp01((GameTime.Now-freedAt)/1.2f));
            if(t>=1)freedLive=false;
        }
        void Hunt()
        {
            float t=GameTime.Now-beatAt;var party=PartyController.Current;
            switch(beat)
            {
                case Beat.Waiting:
                    Pose(0);
                    if(GameTime.CombatHeld||party==null)return;
                    foreach(var hero in party.members)
                    {
                        if(hero==null||!hero.Health.Alive||hero.flight||!Contains(hero.transform.position))continue;
                        marking=Telegraph.Line(transform.position,Axis,length,width,warn,Telegraph.Threat);marking.owner=this;Array.Clear(struck,0,struck.Length);
                        beat=Beat.Marked;beatAt=GameTime.Now;Debug.Log("PRESS_MARKS "+name);break;
                    }
                    return;
                case Beat.Marked:
                {
                    // The tell: the ram climbs past its rest to the top and shudders there.
                    float s=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/warn));Pose(-climb*s+(s>.6f?.02f*Mathf.Sin(Time.time*60):0));
                    if(t>=warn){Slam();beat=Beat.Down;beatAt=GameTime.Now;}
                    return;
                }
                case Beat.Down:Pose(1);if(t>=hold){beat=Beat.Rising;beatAt=GameTime.Now;}return;
                case Beat.Rising:Pose(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(t/rise)));if(t>=rise){beat=Beat.Resting;beatAt=GameTime.Now;}return;
                case Beat.Resting:Pose(0);if(t>=rest){beat=Beat.Waiting;beatAt=GameTime.Now;}return;
            }
        }
        void Slam()
        {
            Slams++;
            AudioManager.PlayAt(sound,transform,Centre+Vector3.up*.5f,.55f,AudioBus.SFX,40,.06f,.62f);
            CombatVfx.Burst(Centre+Vector3.up*.3f,new Color(.74f,.76f,.78f,.6f),"steam");
            if(rig==null)rig=FindFirstObjectByType<CameraRig>();
            var party=PartyController.Current;
            if(marking!=null&&marking.Elapsed>=.5f&&!GameTime.CombatHeld)
            {
                if(party!=null)
                    for(int k=0;k<party.members.Length&&k<struck.Length;k++)
                    {
                        var hero=party.members[k];if(hero==null||struck[k]||!hero.Health.Alive||hero.flight)continue;
                        var at=hero.transform.position;if(!marking.Contains(at)||GameTime.Now<hero.Health.InvulnerableUntil)continue;
                        struck[k]=true;
                        float share=Mathf.Min(hero.Health.maximum*strike,Mathf.Max(0,hero.Health.integrity-1));
                        if(share>0)hero.Health.Receive(new DamagePacket{amount=share,type=DamageType.Kinetic,tag="press"});
                        if(hero.motor is GroundMotor ground)ground.Dash(OutOf(at,out float clear),Mathf.Max(throwClear,clear+1.2f));
                        if(rig!=null)rig.Tremor(.25f,.06f);
                        Debug.Log("PRESS_STRUCK "+hero.character+" "+name);
                    }
                // Iron is iron: whatever else is under it when it falls is struck too, a Compact crew as hard as anyone. A boss is left to its own fight.
                foreach(var enemy in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
                {
                    if(enemy==null||enemy.Health==null||!enemy.Health.Alive||enemy.definition==null||enemy.definition.boss)continue;
                    var dormant=enemy.GetComponent<DormantEnemy>();if(dormant!=null&&dormant.Held)continue;
                    if(!marking.Contains(enemy.transform.position))continue;
                    enemy.Health.Receive(new DamagePacket{amount=enemy.Health.maximum*enemyShare,breakPower=enemy.Health.breakThreshold*.5f,type=DamageType.Kinetic,tag="press"});
                    Crushed++;Debug.Log("PRESS_CRUSHED "+enemy.definition.id+" "+name);
                }
            }
            if(marking!=null){marking.Release();marking=null;}
        }
        /// <summary>Out of the bay by its nearer side, and how far that side is.</summary>
        Vector3 OutOf(Vector3 at,out float clear)
        {
            var o=at-Centre;o.y=0;var a=Axis;var side=Vector3.Cross(Vector3.up,a);
            float on=Vector3.Dot(o,a),across=Vector3.Dot(o,side);
            float toEnd=length*.5f-Mathf.Abs(on),toSide=width*.5f-Mathf.Abs(across);
            if(toEnd<toSide){clear=toEnd;return a*(on<0?-1:1);}
            clear=toSide;return side*(across<0?-1:1);
        }
        /// <summary>The ram's place: 0 up (at rest), 1 down (on the bay), under 0 climbing past its rest for the tell.</summary>
        void Pose(float drop){ramDrop=drop;if(ram!=null)ram.position=Vector3.LerpUnclamped(ramUp,ramDown,drop);}
        /// <summary>The cable through its hangers: taut and pulsing cyan from the rig toward the press while driven (<paramref name="give"/> 0), dropping slack and dark as it gives (1).</summary>
        void DrawCable(float phase,float give)
        {
            if(cable==null||hangers==null||hangers.Length<2)return;
            const int Steps=8;int count=(hangers.Length-1)*Steps+1;if(cable.positionCount!=count)cable.positionCount=count;
            for(int i=0;i+1<hangers.Length;i++)
            {
                Vector3 a=hangers[i],b=hangers[i+1];float span=Vector3.Distance(a,b);
                float low=Mathf.Min(a.y,b.y)-floor-.06f,sag=Mathf.Lerp(Mathf.Min(.04f*span,low*.5f),Mathf.Max(0,low),Mathf.SmoothStep(0,1,give));
                for(int k=0;k<Steps;k++){float t=k/(float)Steps;cable.SetPosition(i*Steps+k,Vector3.Lerp(a,b,t)+Vector3.down*sag*4*t*(1-t));}
            }
            cable.SetPosition(count-1,hangers[^1]);
            if(give>0)BridleHold.Pulse(cable,0,Color.Lerp(cableBody,cableDead,Mathf.Clamp01(give*1.6f)),cableDead,0);
            else BridleHold.Pulse(cable,phase,cableBody,cablePulse,.9f);
        }
        void Show()
        {
            if(showsWithin<=0||shows.size.sqrMagnitude<=0||GameServices.Current==null||GameServices.Current.Input.Blocked)return;
            var party=PartyController.Current;if(party==null||party.Active==null)return;
            var at=party.Active.transform.position;var offset=at-showsAt;offset.y=0;if(offset.magnitude>showsWithin)return;
            if(rig==null)rig=FindFirstObjectByType<CameraRig>();if(rig==null)return;
            var view=new Bounds(at+Vector3.up,Vector3.one*3);view.Encapsulate(shows);
            rig.FrameSceneInterest(this,view,FramedIn);
        }
    }
}
