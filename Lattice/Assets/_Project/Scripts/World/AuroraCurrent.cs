using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// A current of the aurora (chapter 6's Aurora Shear; its rule is riding the currents). A ribbon of light at the flight plane, laid along <see cref="path"/> in the direction it
    /// flows. A hero ship inside it is carried along it at <see cref="push"/> metres a second; flying with it (heading within 70 degrees of the flow) fills the ship's Thrust,
    /// flying against it (within 70 degrees of the flow's opposite) holds the hull to <see cref="pace"/> of its top speed and drains its Thrust. Across it, a ship is only carried.
    /// So a ship that rides the currents goes fast with full tanks, and one that fights them crawls and runs dry.
    /// <para>What says so on screen is the current itself: its ribbon's chevrons and its streaks of light move the way it flows (<see cref="ribbons"/>, <see cref="streaks"/>),
    /// and turn round with it; the HUD's current gauge (Lattice.UI.CurrentGauge) and the Thrust bar say what it is doing to the lead ship. A current with a <see cref="turnFlag"/>
    /// turns round, over <see cref="turnSeconds"/>, once that flag is set, and loads turned in a save that has it.</para>
    /// <para>Every hero ship is carried, the partner's too (it flies the same lane), and so is everything else at the plane inside a current: an enemy fighting the party, the markings,
    /// what a lob leaves burning, the shots in the air (<see cref="Float"/>). A fight in a current is a running fight down the lane. Nothing is carried while a conversation holds the
    /// controls (D275). Logs <c>CURRENT_WITH name</c>, <c>CURRENT_AGAINST name</c> and <c>CURRENT_CLEAR</c> as the
    /// lead ship's riding changes, and <c>CURRENT_TURNED name</c>.</para>
    /// </summary>
    public sealed class AuroraCurrent:MonoBehaviour
    {
        public enum Riding{None,Across,With,Against}
        /// <summary>The current's line at the flight plane, in the direction it flows as laid.</summary>
        public Vector3[] path=System.Array.Empty<Vector3>();
        /// <summary>Half its width; the last <see cref="Edge"/> of it carries less, so a ship eases in and out of it.</summary>
        public float half=8;
        /// <summary>How fast it carries a ship along it, in metres a second.</summary>
        public float push=6;
        /// <summary>Thrust a second a ship flying with it gains, and one flying against it loses (a hull makes back 17 by itself).</summary>
        public float fill=26,drain=32;
        /// <summary>The share of its top speed a ship flying against it keeps.</summary>
        public float pace=.7f;
        /// <summary>The flag on which it turns round; empty, it never does.</summary>
        public string turnFlag="";
        public float turnSeconds=2.5f;
        /// <summary>Drawn ribbons whose map scrolls along the flow (uv x runs along the current, one repeat to <see cref="period"/> metres).</summary>
        public Renderer[] ribbons=System.Array.Empty<Renderer>();
        /// <summary>Streaks of light carried along the flow, one system to a stretch, each with the stretch's own direction as laid.</summary>
        public ParticleSystem[] streaks=System.Array.Empty<ParticleSystem>();
        public Vector3[] streakFlow=System.Array.Empty<Vector3>();
        public float period=6,streakSpeed=9;
        public const float Edge=2,WithinDegrees=70;
        public static readonly List<AuroraCurrent> All=new();
        static readonly Dictionary<CombatActor,(AuroraCurrent current,Riding riding)> under=new();
        static Riding leadWas=Riding.None;static string leadIn="";static float againstSaid=float.NegativeInfinity;
        /// <summary>1 as laid, -1 turned round, between while it turns.</summary>
        public float Sense{get;private set;}=1;
        public bool Turned=>!string.IsNullOrEmpty(turnFlag)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(turnFlag);
        float offset;bool turnSaid;MaterialPropertyBlock block;
        void OnEnable(){All.Add(this);Sense=Turned?-1:1;turnSaid=Turned;Show(true);}
        void OnDisable(){All.Remove(this);under.Clear();}
        /// <summary>How this ship is riding whatever current it is in now (worked out each frame the currents run), and which.</summary>
        public static Riding Of(CombatActor ship,out AuroraCurrent current)
        {
            current=null;if(ship==null||!under.TryGetValue(ship,out var found))return Riding.None;
            current=found.current;return found.riding;
        }
        /// <summary>The flow at a point of the plane (unit, times <see cref="Sense"/>), how far the point lies from the current's line, and how strongly it carries there (1 inside,
        /// easing to 0 at its edge).</summary>
        public Vector3 Flow(Vector3 point,out float distance,out float strength)
        {
            distance=float.PositiveInfinity;var flow=Vector3.zero;point.y=0;
            for(int i=1;i<path.Length;i++)
            {
                var a=path[i-1];var b=path[i];a.y=b.y=0;var d=b-a;float t=Mathf.Clamp01(Vector3.Dot(point-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));
                float gap=Vector3.Distance(point,a+d*t);if(gap<distance){distance=gap;flow=d.normalized;}
            }
            strength=distance>=half?0:Mathf.Clamp01((half-distance)/Edge);
            return flow*Sense;
        }
        /// <summary>Whether a ship with this heading rides with, across or against a flow.</summary>
        public static Riding Judge(Vector3 heading,Vector3 flow)
        {
            heading.y=0;flow.y=0;if(heading.sqrMagnitude<.0001f||flow.sqrMagnitude<.0001f)return Riding.Across;
            float angle=Vector3.Angle(heading,flow);
            return angle<=WithinDegrees?Riding.With:angle>=180-WithinDegrees?Riding.Against:Riding.Across;
        }
        void Update()
        {
            // Turning round: eased, so the ribbon's chevrons slow, stop and run back rather than jump.
            float target=Turned?-1:1;
            if(!Mathf.Approximately(Sense,target))
            {
                Sense=Mathf.MoveTowards(Sense,target,2*Time.deltaTime/Mathf.Max(.1f,turnSeconds));
                if(!turnSaid&&target<0){turnSaid=true;Debug.Log("CURRENT_TURNED "+name);}
            }
            Show(false);
            if(this==All[0])Carry();
        }
        /// <summary>The ribbons' maps scroll and the streaks move with the flow, every frame (a property block isn't saved with a scene).</summary>
        void Show(bool first)
        {
            if(!GameTime.Paused)offset+=Time.deltaTime*streakSpeed*Sense/Mathf.Max(.1f,period);
            block??=new MaterialPropertyBlock();
            foreach(var ribbon in ribbons)
            {
                if(ribbon==null)continue;ribbon.GetPropertyBlock(block);
                block.SetVector("_BaseMap_ST",new Vector4(1,1,-offset,0));ribbon.SetPropertyBlock(block);
            }
            for(int i=0;i<streaks.Length&&i<streakFlow.Length;i++)
            {
                var system=streaks[i];if(system==null)continue;var velocity=system.velocityOverLifetime;var v=streakFlow[i]*streakSpeed*Sense;
                if(first){velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;}
                velocity.x=new ParticleSystem.MinMaxCurve(v.x);velocity.y=new ParticleSystem.MinMaxCurve(0);velocity.z=new ParticleSystem.MinMaxCurve(v.z);
            }
        }
        /// <summary>Every current's work on every hero ship, once a frame (the first current does it for all, so a ship where two meet is carried by the stronger only).</summary>
        static void Carry()
        {
            under.Clear();
            var party=PartyController.Current;if(party==null||party.members==null)return;
            // A conversation holds the controls, so it holds the ships too. The currents carried on through Idra's call about the tender (first player run, November 14):
            // thirteen seconds of radio took the party 87 m down the short lane, through the fight she was describing and out the far side, with nobody able to steer.
            bool held=GameServices.Current!=null&&GameServices.Current.Input.Blocked;
            foreach(var ship in party.members)
            {
                if(ship==null||!ship.flight||!ship.Health.Alive)continue;
                AuroraCurrent best=null;Vector3 flow=Vector3.zero;float strength=0;
                foreach(var current in All){var f=current.Flow(ship.transform.position,out _,out float s);if(s>strength){strength=s;flow=f;best=current;}}
                if(best==null||strength<=0)continue;
                var motor=ship.GetComponent<FlightMotor>();var heading=motor!=null&&motor.Velocity.sqrMagnitude>1?motor.Velocity:ship.transform.forward;
                var riding=Judge(heading,flow);under[ship]=(best,riding);
                if(GameTime.Paused||ship.Recovering||held)continue;
                float dt=Time.deltaTime,carried=strength*Mathf.Abs(best.Sense);
                var body=ship.GetComponent<CharacterController>();
                if(body!=null&&body.enabled&&carried>0)body.Move(flow.normalized*best.push*carried*dt);
                if(riding==Riding.With)ship.thrust=Mathf.Min(100,ship.thrust+best.fill*carried*dt);
                else if(riding==Riding.Against){ship.Health.Hinder(Mathf.Lerp(1,best.pace,carried));ship.thrust=Mathf.Max(0,ship.thrust-best.drain*carried*dt);}
            }
            var lead=party.Active;
            if(!GameTime.Paused&&!held)Float(lead,Time.deltaTime);
            var now=Of(lead,out var at);string name=at!=null?at.name:"";
            if(now!=leadWas||name!=leadIn)
            {
                Debug.Log(now==Riding.None?"CURRENT_CLEAR":now==Riding.With?"CURRENT_WITH "+name:now==Riding.Against?"CURRENT_AGAINST "+name:"CURRENT_ACROSS "+name);
                // The heroes notice when a current turns against them, not every time a stick wanders: one call in eight seconds.
                if(now==Riding.Against&&lead!=null&&Time.unscaledTime-againstSaid>8){againstSaid=Time.unscaledTime;BarkService.Play(lead.character,"current");}
                leadWas=now;leadIn=name;
            }
        }
        /// <summary>How far off the lead an ordinary body still fights the party (EnemyBrain walks it home past this): inside it, a fight rides with the lead's current.</summary>
        public const float FightsWithin=35;
        /// <summary>What the currents carry a thing at this point through this frame: the strongest current there, as a ship is carried.</summary>
        static Vector3 Drift(Vector3 point,float dt)
        {
            AuroraCurrent best=null;Vector3 flow=Vector3.zero;float strength=0;
            foreach(var current in All){var f=current.Flow(point,out _,out float s);if(s>strength){strength=s;flow=f;best=current;}}
            if(best==null||strength<=0)return Vector3.zero;
            return flow.normalized*best.push*strength*Mathf.Abs(best.Sense)*dt;
        }
        /// <summary>Everything else at the plane in a current rides it as the ships do: the Muster's craft while they fight the party, what they mark and what they lob, the
        /// burning melt a tender's charge leaves, and every shot in the air. So a fight in a current is a running fight down the lane: between the bodies in it, it plays as it would in
        /// still space (a tender's circle comes down on the ship it was thrown at, a skiff's lane runs through the ship it was laid through, the shot a ship fires goes where it was
        /// aimed), and what the current changes is where the fight is going, and what riding it does to a hull (with it, Thrust for the rolls the fight asks for; against it, a crawl,
        /// and the fight carried on without you). In the first player run only the ships rode: every circle and lane landed where a ship had been, two runs took not one blow from a
        /// fight, and a fight begun in the fast lane was left standing a hundred metres back in ten seconds (November 14). <b>A fight rides as one</b>, at the pace of the current
        /// the lead is in: everything within <see cref="FightsWithin"/> of the lead is carried as the lead is. Each carried by the current where it was, a skiff circling ten metres
        /// out sat at the lane's eased edge, drifted at a third of the ship's pace, and its lane slid off the ship within a quarter of a second: riding, a ship that did nothing took 8
        /// blows in 24 s where it took 15 in still space (lead-own-2, November 14). A body that has lost the party is not carried: it holds and goes home, as anywhere. What a hero lays in
        /// the world with a skill (a well, a waypoint charge) stays where it is laid.</summary>
        static void Float(CombatActor lead,float dt)
        {
            if(dt<=0||lead==null)return;
            var near=lead.transform.position;var fight=Drift(near,dt);
            bool InFight(Vector3 at){var gap=at-near;gap.y=0;return gap.sqrMagnitude<=FightsWithin*FightsWithin;}
            Vector3 Carried(Vector3 at)=>InFight(at)?fight:Drift(at,dt);
            if(fight!=Vector3.zero)
                foreach(var health in Health.All)
                {
                    if(health==null||health.friendly||!health.Alive||!health.TryGetComponent<EnemyBrain>(out var brain)||!brain.enabled||brain.Passive||!InFight(health.transform.position))continue;
                    brain.Carry(fight);
                }
            foreach(var marking in Telegraph.Live){if(marking==null)continue;var d=Carried(marking.Origin);if(d!=Vector3.zero)marking.Shift(d);}
            foreach(var area in LingeringArea.All){if(area==null)continue;var d=Carried(area.transform.position);if(d!=Vector3.zero)area.transform.position+=d;}
            foreach(var shot in Projectile.Live){if(shot==null||!shot.isActiveAndEnabled)continue;var d=Carried(shot.transform.position);if(d!=Vector3.zero)shot.transform.position+=d;}
        }
    }
}
