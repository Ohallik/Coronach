using System;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// One rail of Oriel's orrery (chapter 7, the Orrery: one readable machine; docs/maps/Orrery.md). A light-rail is a circle round the planet's centre on the plan, crossing the lane due
    /// south of that centre; a moon is threaded on it and runs one way round it, the same way for ever, every moon of the machine in the same <see cref="Period"/>. Everything a player
    /// reads is worked out from where the moon is, every frame, by one rule:
    /// <list type="bullet">
    /// <item>The rail's lamps <b>stand amber</b> over the stretch the moon is about to sweep (from its trailing edge to <see cref="lead"/> seconds of its travel ahead of it) and <b>run
    /// green</b> everywhere else: light chasing along the rail the way the moon goes.</item>
    /// <item>A row of rail-gates stands <see cref="gateOut"/> outside the rail. A gate is <b>shut</b> (amber) from the moment the moon's amber reach comes to its near rim until the moon
    /// is a hull clear of its far rim, and <b>open</b> (green) the rest of the time: a gate it has passed is green, so a ship that waits in a moon's lee and goes on green is through behind it.</item>
    /// <item>While the moon is in the lane, the stretch from its trailing edge to its amber reach is marked on the plane as a danger, and a hero ship it touches inside that marking loses
    /// <see cref="strike"/> of its Integrity before its frame's plating (never the last of it) and is thrown clear off the rail. It is the place's damage: no enemy is its source.</item>
    /// <item>The lead ship going through a shut gate has <b>forced</b> it: the rail's <see cref="wardens"/> wake. Through an open one it has kept the machine's time, and nothing minds it.</item>
    /// </list>
    /// The broken moon (<see cref="shards"/>) calves a Moonshard off its leading face as it comes into the lane. A named gate (<see cref="named"/>) is read for the Names of the Moons by
    /// passing it open and slowly. Once <see cref="chartFlag"/> is set (Ishen has read the gates' hours) a gate shows its turn ahead: it flickers amber before it shuts.
    /// Logs <c>RAIL_FORCED rail gate</c>, <c>RAIL_KEPT rail gate</c>, <c>MOON_STRUCK hero rail</c>, <c>MOON_CALVED rail encounter</c>, <c>GATE_READ rail</c>.
    /// </summary>
    public sealed class OrreryRail:MonoBehaviour
    {
        /// <summary>Every moon of the machine goes round in this many seconds: a clock. Its rail's loop is its speed times this.</summary>
        public const float Period=20;
        /// <summary>A hull's clearance past a gate's far rim before it opens behind the moon: Idra's, the widest.</summary>
        public const float HullClear=2.2f;
        static readonly Color Amber=new(1f,.4f,.05f,1),Green=new(.2f,1f,.32f,1);
        static readonly int BaseMapId=Shader.PropertyToID("_BaseMap"),EmissionMapId=Shader.PropertyToID("_EmissionMap"),EmissionId=Shader.PropertyToID("_EmissionColor");

        public string railName="";
        /// <summary>The planet's centre on the plan, and this rail's radius round it. The lane crosses the rail at arc length 0, due south of the centre; arc length grows eastward.</summary>
        public Vector2 centre;public float radius=1000;
        /// <summary>+1: where it crosses the lane the moon runs east; -1, west.</summary>
        public int direction=-1;
        /// <summary>The moon's speed along the rail, the time in its period at which its centre crosses the lane, its radius, and how many seconds of its travel its amber reaches ahead.</summary>
        public float speed=10,phase,moonRadius=5.5f,lead=2.6f;
        public float plane=1,laneHalf=36;
        /// <summary>The moon as drawn: <see cref="moon"/> rides the rail facing the way it goes (its collar and yoke), <see cref="moonBody"/> turns on the rail inside it.</summary>
        public Transform moon,moonBody;public float spin=14;
        /// <summary>The row of gates: how far outside the rail it stands, each gate's middle as an arc length on its own circle, each gate's lamp band, and a gate opening's half width.</summary>
        public float gateOut=11,gateHalf=6;public float[] gateAt=Array.Empty<float>();public Renderer[] gateLamps=Array.Empty<Renderer>();
        /// <summary>The rail's lamps: one mesh whose lamp k has its uv's x at (k + .5) / <see cref="lampCount"/>, lit from a texture this writes each frame.</summary>
        public Renderer lampStrip;public float lampFrom=-120,lampSpacing=1.5f;public int lampCount;
        /// <summary>What wakes when the lead forces a gate, and the broken moon's shards, begun in turn.</summary>
        public EncounterVolume wardens;public EncounterVolume[] shards=Array.Empty<EncounterVolume>();
        /// <summary>The Names of the Moons: the point read by passing <see cref="namedGate"/> open at no more than <see cref="slowly"/>, and Ishen's word for it over the radio.</summary>
        public DiscoveryPoint named;public int namedGate=-1;public float slowly=8;public string namedSpeaker="Ishen",namedSituation="hail.survey",chartFlag="ishen.moonsRead";
        /// <summary>A moon's blow: a share of a hull's Integrity before plating (a fifth: about 11.5% of Taren's at the chapter's arrival in Tempered parts, as heavy as a boss's
        /// blow, and lighter for the guard hero), and how far it throws the hull clear.</summary>
        public float strike=.2f,throwClear=7;

        float started;Texture2D lamps;Color32[] lampColours;MaterialPropertyBlock block;Telegraph marking;Vector3 markedAlong;float markedAt;
        readonly System.Collections.Generic.List<(CombatActor hero,Vector3 way,float left)> throws=new();
        readonly bool[] struck=new bool[4];bool frontWasIn;float leadSide=float.NaN;int nearGate=-1;bool nearOpen=true;CombatActor lastLead;Vector3 lastPlace;
        static int railBarks;static float nextRailBark;

        // ---------------------------------------------------------------- the machine's arithmetic, the same for the builder, a still and a frame of play
        public float Loop=>speed*Period;
        public float GateRadius=>radius+gateOut;
        /// <summary>The machine's own time: seconds since the zone opened.</summary>
        public float Clock=>GameTime.Now-started;
        /// <summary>Where the moon's centre is along its rail (arc length, east positive) at this clock time.</summary>
        public float MoonAt(float time)=>direction*speed*(Mathf.Repeat(time-phase+Period*.5f,Period)-Period*.5f);
        public float Moon=>MoonAt(Clock);
        /// <summary>How far a point of the rail is ahead of the moon, along the way it goes, the loop's way round.</summary>
        public float AheadOfMoon(float along,float moonAt){float loop=Loop;return Mathf.Repeat((along-moonAt)*direction+loop*.5f,loop)-loop*.5f;}
        /// <summary>The amber reach: from the moon's centre to this far ahead of it.</summary>
        public float Reach=>moonRadius+speed*lead;
        public bool LampAmber(float along,float moonAt){float ahead=AheadOfMoon(along,moonAt);return ahead>=-moonRadius&&ahead<=Reach;}
        /// <summary>A gate's middle, as an arc length on the rail's own circle.</summary>
        public float GateAlongRail(int gate)=>gateAt[gate]*radius/GateRadius;
        public bool GateShut(int gate,float moonAt)
        {
            float ahead=AheadOfMoon(GateAlongRail(gate),moonAt),half=gateHalf*radius/GateRadius;
            return ahead>=-(moonRadius+half+HullClear)&&ahead<=Reach+half;
        }
        public bool Open(int gate)=>!GateShut(gate,Moon);
        /// <summary>Seconds until an open gate shuts as the moon comes round (0 if it is shut).</summary>
        public float UntilShut(int gate,float moonAt)
        {
            if(GateShut(gate,moonAt))return 0;
            float ahead=AheadOfMoon(GateAlongRail(gate),moonAt),half=gateHalf*radius/GateRadius,to=ahead-(Reach+half);
            return Mathf.Repeat(to,Loop)/speed;
        }
        /// <summary>A point of the plan at this arc length on a circle of this radius round the centre.</summary>
        public Vector2 On(float along,float onRadius){float a=along/onRadius;return centre+new Vector2(Mathf.Sin(a),-Mathf.Cos(a))*onRadius;}
        /// <summary>The way along the rail, east positive, at this arc length.</summary>
        public Vector3 Tangent(float along){float a=along/radius;return new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));}
        public Vector3 MoonPoint(float moonAt){var p=On(moonAt,radius);return new Vector3(p.x,plane-1,p.y);}
        /// <summary>A world point's arc length on a circle of this radius round the centre, and its distance from the centre.</summary>
        public float Along(Vector3 point,float onRadius)=>onRadius*Mathf.Atan2(point.x-centre.x,centre.y-point.z);
        public float FromCentre(Vector3 point)=>Vector2.Distance(new Vector2(point.x,point.z),centre);
        public int NearestGate(float alongGateRow){int best=-1;float gap=float.MaxValue;for(int i=0;i<gateAt.Length;i++){float d=Mathf.Abs(gateAt[i]-alongGateRow);if(d<gap){gap=d;best=i;}}return best;}
        /// <summary>Lays the moon where it is at this clock time: what the builder does for a still, and what play does every frame.</summary>
        public void Pose(float time)
        {
            float at=MoonAt(time);
            if(moon!=null){moon.position=MoonPoint(at);moon.rotation=Quaternion.LookRotation(Tangent(at)*direction);}
            if(moonBody!=null)moonBody.localRotation=Quaternion.AngleAxis(time*spin*direction,Vector3.forward);
        }
        /// <summary>How many seconds of the moon's travel behind it its lee is drawn: the lamps it has just passed stand steady green, full bright, so where a ship crosses behind a
        /// moon is lit on the rail and every chasing lamp beyond reads as "and so on round". Drawn only: the gates' rule is <see cref="GateShut"/>.</summary>
        public float lee=3;
        /// <summary>The colour of lamp k at this clock time.</summary>
        public Color32 Lamp(int k,float time)
        {
            float along=lampFrom+k*lampSpacing,at=MoonAt(time);
            if(LampAmber(along,at))return Amber;
            float ahead=AheadOfMoon(along,at);
            if(ahead<-moonRadius&&ahead>=-moonRadius-speed*lee)return Green;
            // Running: a chase every four lamps, travelling the moon's way half again faster than it.
            float run=Mathf.Repeat((along*direction-time*speed*1.6f)/6f,1),bright=.22f+.78f*Mathf.Pow(Mathf.Max(0,Mathf.Cos(run*Mathf.PI*2)),6);
            return Green*bright;
        }
        public Color GateColour(int gate,float time,bool chart)
        {
            float at=MoonAt(time);
            if(GateShut(gate,at))return Amber*1.5f;
            // Shown ahead of time once Ishen has the hours: a gate about to shut flickers amber first.
            if(chart&&UntilShut(gate,at)<1.5f&&Mathf.Repeat(time*6,1)<.5f)return Amber*1.2f;
            return Green*(1f+.25f*Mathf.Sin(time*5));
        }

        // ---------------------------------------------------------------- play
        void Start()
        {
            started=GameTime.Now;block=new MaterialPropertyBlock();
            if(lampStrip!=null&&lampCount>0)
            {
                lamps=new Texture2D(lampCount,1,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name=railName+" lamps"};lampColours=new Color32[lampCount];
                var material=lampStrip.material;material.SetTexture(BaseMapId,lamps);material.SetTexture(EmissionMapId,lamps);
            }
            Pose(0);
        }
        void OnDisable(){if(marking!=null){marking.Clear();marking=null;}}
        void OnDestroy(){if(lamps!=null)Destroy(lamps);}
        void Update()
        {
            float time=Clock,at=MoonAt(time);Pose(time);
            var party=PartyController.Current;var lead=party!=null?party.Active:null;
            var services=GameServices.Current;bool chart=services!=null&&services.Flags.GetBool(chartFlag);
            // Every rail every frame (two hundred lamps and nine rings each): what is drawn is always what the rule says now.
            Light(time,chart);
            Throw();
            if(lead==null||!lead.flight)return;
            Mark(at,lead);Strike(at,party);Watch(at,lead,services);Calve(at,lead);
        }
        void Light(float time,bool chart)
        {
            if(lamps!=null){for(int k=0;k<lampCount;k++)lampColours[k]=Lamp(k,time);lamps.SetPixels32(lampColours);lamps.Apply(false);}
            for(int i=0;i<gateLamps.Length&&i<gateAt.Length;i++)
            {
                if(gateLamps[i]==null)continue;
                gateLamps[i].GetPropertyBlock(block);block.SetColor(EmissionId,GateColour(i,time,chart));gateLamps[i].SetPropertyBlock(block);
            }
        }
        /// <summary>The danger on the plane: laid while the moon is in the lane and the lead near enough to meet it, from its trailing edge to the end of its amber reach.</summary>
        void Mark(float at,CombatActor lead)
        {
            float front=at+direction*Reach,back=at-direction*moonRadius;
            bool inLane=Mathf.Min(front,back)<laneHalf+4&&Mathf.Max(front,back)>-laneHalf-4&&Mathf.Abs(FromCentre(lead.transform.position)-radius)<90;
            if(!inLane){if(marking!=null){marking.Release();marking=null;}Array.Clear(struck,0,struck.Length);return;}
            var along=Tangent(at)*direction;var origin=MoonPoint(at)-along*moonRadius;origin.y=plane;
            if(marking==null||Vector3.Angle(along,markedAlong)>1.5f)
            {
                var laid=Telegraph.Line(origin,along,moonRadius+Reach,moonRadius*2,1,Telegraph.Threat);laid.Steady=true;laid.owner=this;
                if(marking!=null)marking.Clear();else markedAt=GameTime.Now;
                marking=laid;markedAlong=along;
            }
            else marking.MoveTo(origin);
        }
        void Strike(float at,PartyController party)
        {
            if(marking==null||GameTime.Now-markedAt<.5f||GameServices.Current==null||GameServices.Current.Input.Blocked)return;
            var centre3=MoonPoint(at);
            for(int k=0;k<party.members.Length&&k<struck.Length;k++)
            {
                var hero=party.members[k];if(hero==null||struck[k]||!hero.flight||!hero.Health.Alive)continue;
                var gap=hero.transform.position-centre3;gap.y=0;
                if(gap.magnitude>moonRadius+HeroCollision.HullRadius(hero.character)*.8f||!marking.Contains(hero.transform.position)||GameTime.Now<hero.Health.InvulnerableUntil)continue;
                struck[k]=true;
                float share=Mathf.Min(hero.Health.maximum*strike,Mathf.Max(0,hero.Health.integrity-1));
                if(share>0)hero.Health.Receive(new DamagePacket{amount=share,type=DamageType.Kinetic,tag="moon"});
                // Off the rail on the side the hull is on, a little the moon's way: out of the marking, never back into the moon.
                float side=Mathf.Sign(FromCentre(hero.transform.position)-radius);if(side==0)side=1;
                var outward=new Vector3(hero.transform.position.x-centre.x,0,hero.transform.position.z-centre.y).normalized*side;
                // Thrown by the rail itself, through the hull's own controller: a motor's dash only moves when a brain drives the motor that frame.
                if(hero.motor is FlightMotor ship)ship.Halt();
                throws.Add((hero,(outward+Tangent(at)*direction*.35f).normalized,throwClear));
                Debug.Log("MOON_STRUCK "+hero.character+" "+railName);
            }
        }
        /// <summary>A struck hull goes off the rail over a fifth of a second, along the plane, stopped by anything solid as a flown hull is.</summary>
        void Throw()
        {
            if(GameTime.Paused)return;
            for(int i=throws.Count-1;i>=0;i--)
            {
                var (hero,way,left)=throws[i];
                if(hero==null||!hero.TryGetComponent<CharacterController>(out var body)||!body.enabled){throws.RemoveAt(i);continue;}
                float step=Mathf.Min(left,throwClear/.2f*Time.deltaTime);body.Move(way*step+Vector3.up*(plane-hero.transform.position.y));
                left-=step;if(left<=.001f)throws.RemoveAt(i);else throws[i]=(hero,way,left);
            }
        }
        /// <summary>The lead going through the gate row: through a shut gate is forcing it; through the named one, open and slow, is reading it. And the lights' change at the
        /// lead's gate, said aloud now and then: green, amber, green, in that order (the barks are written so).</summary>
        void Watch(float at,CombatActor lead,GameServices services)
        {
            var p=lead.transform.position;float fromCentre=FromCentre(p),side=Mathf.Sign(fromCentre-GateRadius),row=Along(p,GateRadius);
            bool inLane=Mathf.Abs(row)<laneHalf;
            // A hull put somewhere (a load, a respawn, a swap of who leads) hasn't flown through anything: only a crossing a ship could fly in one frame counts.
            bool flown=lastLead==lead&&(p-lastPlace).sqrMagnitude<16;lastLead=lead;lastPlace=p;
            if(!flown)leadSide=float.NaN;
            if(!float.IsNaN(leadSide)&&side!=leadSide&&inLane)
            {
                int gate=NearestGate(row);
                if(gate>=0)
                {
                    bool shut=GateShut(gate,at);
                    if(shut)
                    {
                        Debug.Log("RAIL_FORCED "+railName+" "+gate);
                        if(wardens!=null&&!wardens.Started&&wardens.isActiveAndEnabled){wardens.Begin();BarkService.Play(lead.character,"rail.forced",true);}
                    }
                    else
                    {
                        Debug.Log("RAIL_KEPT "+railName+" "+gate);
                        float pace=lead.motor!=null?lead.motor.Velocity.magnitude:0;
                        if(gate==namedGate&&named!=null&&pace<=slowly&&named.Wanted)
                        {
                            Read(services);Debug.Log("GATE_READ "+railName);
                            if(!string.IsNullOrEmpty(namedSpeaker))BarkService.Play(namedSpeaker,namedSituation,true);
                        }
                    }
                }
            }
            leadSide=side;
            // The lights at the lead's gate, while the lead waits by the row.
            bool waiting=inLane&&Mathf.Abs(fromCentre-GateRadius)<22;
            int here=waiting?NearestGate(row):-1;
            if(here<0){nearGate=-1;return;}
            bool open=!GateShut(here,at);
            if(here==nearGate&&open!=nearOpen&&(services==null||!services.Input.Blocked)&&Time.unscaledTime>=nextRailBark&&open==(railBarks%2==0))
            {BarkService.Play(lead.character,"rail",true);railBarks++;nextRailBark=Time.unscaledTime+8;}
            nearGate=here;nearOpen=open;
        }
        /// <summary>Reads the named gate as a discovery does, with no button: <see cref="named"/> is only what the objective's arrow finds (range nought, never offered), so passing
        /// is the only way to read it. Its flag, its report toward the Names of the Moons, and a save.</summary>
        void Read(GameServices services)
        {
            if(services==null)return;
            services.Flags.SetBool(named.flag,true);Debug.Log("DISCOVERY "+named.flag);
            if(!string.IsNullOrEmpty(named.report))Lattice.Rpg.RpgServices.Quests.Report(ObjectiveKind.Interact,named.report);
            services.Saves.Save("autosave",services.State);
        }
        /// <summary>The broken moon sheds as it comes into the lane: once a pass, into the next of its shard encounters that hasn't been spent, and only with the lead near enough to see it.</summary>
        void Calve(float at,CombatActor lead)
        {
            if(shards.Length==0)return;
            float front=at+direction*moonRadius;bool inLane=Mathf.Abs(front)<laneHalf+8;
            bool comingIn=inLane&&!frontWasIn&&AheadOfMoon(0,at)>0;frontWasIn=inLane;
            if(!comingIn||Mathf.Abs(FromCentre(lead.transform.position)-radius)>80)return;
            foreach(var encounter in shards)
            {
                if(encounter==null||encounter.Started)continue;
                var from=On(front+direction*2,radius);var facing=Quaternion.LookRotation(Tangent(front)*direction);
                foreach(var spawner in encounter.spawners)if(spawner!=null)spawner.transform.SetPositionAndRotation(new Vector3(from.x,plane,from.y),facing);
                encounter.Begin();Debug.Log("MOON_CALVED "+railName+" "+encounter.encounterId);
                BarkService.Play(lead.character,"moonshard",true);
                return;
            }
        }
    }
}
