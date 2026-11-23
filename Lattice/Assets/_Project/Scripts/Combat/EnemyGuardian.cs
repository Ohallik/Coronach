using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The guardian (the Rootwarden; later the Drowned Sentry, the Thawed Guard, the Glass Sentinel, the Pikeguard): slow, and plated to the front. A blow that comes at it from
    /// inside its guarded arc is let through at <c>tune.guard</c> of its weight and reads as resisted (<see cref="Health.Plate"/>); from the side or behind it lands whole. It
    /// turns at a walking pace, so behind it is a place a player can get to, and it does not turn at all while it winds up or while its arms are still in the ground after its
    /// long move, when its plate is down too: that is its opening. It holds a post: led further than <c>tune.wake</c> from where it was set, it walks back.
    /// Two moves: attacks[0], a slam on a marked circle before it; attacks[1], a line of ground that erupts (a root line, a spear thrust), used when the hero stands off.
    /// Reads: attacks[0] and [1] (telegraph, range, radius, damage, cooldown), tune.arc (the guarded front, 150 degrees: under 180, so its flank is open), guard (.25), reach
    /// (the line, 11 m), width (1.9), recover (plate down after the line, 1.5 s), wake (the post, 10 m; none if negative), speed (its turn, 75 degrees a second).
    /// <para><b>A rank</b> (<c>tune.count</c> over one: chapter 6's Thawed Guard). Guardians of one kind woken together within <see cref="Gathers"/> of each other stand as a rank of up to
    /// <c>count</c>, shoulder to shoulder <see cref="Spacing"/> apart: they face the party as one (turning at <see cref="RankTurn"/> of their own pace), advance together, and thrust their
    /// spears together, a wall of parallel lines. Three abreast their shields are locked through the middle one: while the middle one is broken (its Break meter filled) every plate in the
    /// rank is down; and with the middle one dead the rank comes apart, its others stand open for <see cref="Unlocked"/> seconds and fight alone from then on. So the answer is to break
    /// the middle, or to go round the end of the rank, whose outer flank is as open as any guardian's.</para>
    /// <para><b>A swept beam</b> (<c>tune.sweep</c> over zero: chapter 6's Muster Excavator and its thaw-lance). Its long move is a fan of that many degrees, out to <c>tune.reach</c>, marked
    /// whole for its tell; then a beam crosses it from one edge to the other in <see cref="SweepSeconds"/>, striking each hero as it passes him. Behind it, or inside its slam's reach, is
    /// out of it; a roll through the beam as it comes is the other answer. Then its lance vents: plate down and no turning for <c>tune.recover</c>, as after a line.</para>
    /// <para><b>A pair</b> (<c>tune.pair</c>: chapter 8's Muster Pikeguard) is <c>EnemyGuardian.Pair.cs</c>.</para>
    /// <para><b>One that gives ground</b> (<c>tune.givesGround</c>: chapter 9's Compact Warden, a person doing a job) is <c>EnemyGuardian.Ground.cs</c>.</para>
    /// </summary>
    public sealed partial class EnemyGuardian:EnemyBehaviour
    {
        Transform plate;bool line;float loweredUntil;
        static readonly Color PlateTone=new(.62f,.9f,.5f,.55f);
        float Arc=>Mathf.Clamp(Or(tune.arc,150),10,340);float Lets=>tune.guard>0?Mathf.Clamp01(tune.guard):.25f;float Reach=>Or(tune.reach,11);float Width=>Or(tune.width,1.9f);
        float Down=>Or(tune.recover,1.5f);float Post=>tune.wake<0?float.PositiveInfinity:Or(tune.wake,10);float Turn=>Or(tune.speed,75);
        /// <summary>Its plate is up: alive, unbroken, not recovering from its long move, and (in a rank of three) its rank's middle unbroken.</summary>
        public bool Guarding=>health.Alive&&!health.Broken&&Time.time>=loweredUntil&&!Unlocked3;
        public override bool Roams=>false;
        public override bool Yields=>tune.givesGround;
        protected override void Begin()
        {
            health.Plate=Through;
            // One that gives ground walks out of the fight at nought instead of falling (EnemyGuardian.Ground.cs): DefeatPresentation runs its leaving in place of a body.
            if(tune.givesGround&&!body.TryGetComponent<BossDeparture>(out _)){var leaving=body.gameObject.AddComponent<BossDeparture>();leaving.sound="";leaving.speed=StandAsideSpeed;leaving.reel=.5f;leaving.Duration=StandAsideSeconds;}
            // The Compact Warden's riot shield was cut from its body to be drawn here (RiotShield), raised while its guard is.
            if(tune.givesGround&&!body.TryGetComponent<RiotShield>(out _))RiotShield.On(body,this);
            // What the plate covers is drawn where it is: a low arc of the root's own green standing before it, gone while it is down. (Its colour stays the plate's own, whatever a
            // definition tints its beam: a guarded front reads the same on every guardian.)
            float radius=(controller!=null?controller.radius:.6f)+.55f,height=controller!=null?controller.height*.62f:1.2f;
            var go=new GameObject("Front plate",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(body,false);plate=go.transform;
            go.GetComponent<MeshFilter>().sharedMesh=ArcMesh(radius,height,Arc);
            var view=go.GetComponent<MeshRenderer>();view.sharedMaterial=PlateLook();view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;view.receiveShadows=false;
        }
        /// <summary>What of a blow gets through: all of it, unless the plate is up and the striker stands inside the guarded arc.</summary>
        float Through(DamagePacket packet)
        {
            if(!Guarding||packet.source==null)return 1;
            var from=Flat(packet.source.transform.position-body.position);if(from.sqrMagnitude<.01f)return 1;
            return Vector3.Angle(body.forward,from)<=Arc*.5f?Lets:1;
        }
        public override void Tick(CombatActor victim)
        {
            var to=Flat(victim.transform.position-body.position);float distance=to.magnitude;
            if(sweeping){Sweep();return;}
            if(throwing&&brain.Telegraphing){if(brain.TellRunOut)Throw();return;}
            if(brain.Telegraphing)
            {
                if(!brain.TellRunOut)return;
                if(line&&Swept){BeginSweep();return;}
                var mark=brain.EndTell();var move=line?Second:attack;
                if(mark!=null){StrikeIn(mark,brain.Blow(move,HitWeight.Heavy),Chest);mark.Release();CombatVfx.Burst(mark.Origin+mark.Forward*(line?mark.Length*.5f:0)+Vector3.up*.4f,new Color(.6f,.5f,.36f),"kill");}
                Sound("explosionCrunch_000",.2f,line?.8f:1.1f);
                brain.Rest(move!=null?move.cooldown:1.8f);
                if(line){loweredUntil=Time.time+Down;Debug.Log("GUARD_DOWN "+def.id);}
                return;
            }
            // Arms in the ground: it neither turns nor steps until they are out.
            if(Time.time<loweredUntil)return;
            if(Ranked&&FormUp(victim))return;
            if(Paired&&Pair(victim))return;
            var home=Flat(brain.Home-body.position);
            if(home.magnitude>Post){Face(home,Turn*2);brain.Walk(home,def.speed);return;}
            Face(to,Turn);
            bool faced=Vector3.Angle(body.forward,to)<25;
            if(brain.Rested&&faced&&brain.Sees(victim))
            {
                var aim=body.forward;
                if(distance<Range){Slam(victim,aim);return;}
                var far=Second;
                if(far!=attack&&distance<Reach){Long(victim,aim,far);return;}
            }
            brain.Spread(to,distance,Range);
            if(distance>Range*.7f&&Flat(victim.transform.position-brain.Home).magnitude<Post+Range)brain.Walk(to,def.speed);
        }
        void Slam(CombatActor victim,Vector3 aim)
        {
            line=false;float tell=Tell;
            Mark(Telegraph.Circle(body.position+aim*(Range*.55f),attack!=null&&attack.radius>0?attack.radius:2.2f,tell,Telegraph.Threat,true),tell,aim,victim,"root_groan",1.05f);
        }
        void Long(CombatActor victim,Vector3 aim,AttackDef far)
        {
            line=true;float tell=Mathf.Max(.5f,far.telegraph);
            if(Swept)Mark(Telegraph.Cone(body.position,aim,Reach,SweepArc,tell,Telegraph.Heavy,true),tell,aim,victim,"forceField_000",.55f);
            else Mark(Telegraph.Line(body.position,aim,Reach,Width,tell,Telegraph.Heavy,true),tell,aim,victim,"root_groan",.8f);
        }
        public override void Present()
        {
            GiveGround();
            if(plate!=null&&plate.gameObject.activeSelf!=Guarding)plate.gameObject.SetActive(Guarding);
            if(beam!=null&&sweeping)DrawBeam();
        }
        public override void Cancel()
        {
            throwing=false;
            if(!sweeping)return;
            sweeping=false;brain.Acting=false;if(sweepMark!=null)sweepMark.Cancel();sweepMark=null;Unbeam();
        }
        public override void Fell(DamagePacket packet){health.Plate=null;if(plate!=null)plate.gameObject.SetActive(false);Cancel();LeaveRank(true);LeavePair();StandAside();}
        public override void Dispose(){Unbeam();LeaveRank(false);LeavePair();}

        // ------------------------------------------------------------------ a glass front (tune.reflects)
        /// <summary>Chapter 7's Glass Sentinel: a Beam shot that lands on its guarded front, from a hero inside its arc, is gathered in the glass and thrown back at whoever fired it, down a
        /// marked line (attacks[2]: its tell, damage and the wait before it can gather another). A shot from the flank or behind, or any other kind of blow, is only what it always was.
        /// So a shooter has to get round it, as a blade does.</summary>
        bool throwing;float nextThrow;
        AttackDef Third=>def.attacks!=null&&def.attacks.Length>2&&def.attacks[2]!=null?def.attacks[2]:attack;
        /// <summary>A shot gathered in its glass, about to be thrown back.</summary>
        public bool Throwing=>throwing&&brain.Telegraphing;
        public override void Struck(DamagePacket packet,float amount)
        {
            if(!tune.reflects||packet.type!=DamageType.Beam||!Guarding||throwing||sweeping||brain.Telegraphing||Time.time<nextThrow||packet.source==null)return;
            if(!packet.source.TryGetComponent<CombatActor>(out var shooter)||!shooter.Health.Alive)return;
            var from=Flat(shooter.transform.position-body.position);if(from.sqrMagnitude<.01f||Vector3.Angle(body.forward,from)>Arc*.5f)return;
            var way=from.normalized;float tell=Mathf.Max(.5f,Third.telegraph);
            throwing=true;nextThrow=Time.time+tell+Mathf.Max(.5f,Third.cooldown);
            Mark(Telegraph.Line(body.position,way,Mathf.Max(13,from.magnitude+3),1.2f,tell,Telegraph.Threat,true),tell,way,shooter,"forceField_000",1.9f,true);
            if(!string.IsNullOrEmpty(tune.bark)&&PartyController.Current!=null)BarkService.Play(PartyController.Current.Active.character,tune.bark);
            Debug.Log("GUARD_GATHERS "+def.id);
        }
        void Throw()
        {
            throwing=false;var mark=brain.EndTell(.3f);brain.Rest(.6f);
            if(mark==null)return;
            var packet=ProjectileProvenance.Volley(brain.Blow(Third,HitWeight.Medium));CombatAudio.HostileShot(body,Chest);
            mark.Carry(new[]{Projectile.Fire(Chest,mark.Forward,packet,20,mark.Length)});
            CombatVfx.Burst(Chest+mark.Forward*.8f,new Color(.75f,.95f,1.3f,.9f),"kill");Debug.Log("GUARD_THROWBACK "+def.id);
        }

        // ------------------------------------------------------------------ a swept beam (tune.sweep)
        /// <summary>How long the beam takes to cross its fan, and its colour.</summary>
        public const float SweepSeconds=.6f;
        static readonly Color BeamTone=new(1.6f,.62f,.2f,1);
        bool Swept=>tune.sweep>0;float SweepArc=>Mathf.Clamp(tune.sweep,20,300);
        bool sweeping;float sweepT;int sweepSide=1;Telegraph sweepMark;Vector3 sweepAim;readonly HashSet<Health> swept=new();LineRenderer beam;
        /// <summary>Its beam is crossing its fan.</summary>
        public bool Sweeping=>sweeping;
        /// <summary>Where the beam points now, in degrees off the fan's middle.</summary>
        public float BeamAngle=>(Mathf.Clamp01(sweepT/SweepSeconds)-.5f)*SweepArc*sweepSide;
        void BeginSweep()
        {
            sweepMark=brain.EndTell(SweepSeconds+.2f);sweepAim=sweepMark!=null?sweepMark.Forward:body.forward;sweeping=true;sweepT=0;swept.Clear();brain.Acting=true;
            sweepSide=-sweepSide;
            // Its own colour if the definition gives one (the Glass Sentinel's halberd of light), else the thaw-lance's.
            var go=new GameObject("Thaw-lance beam",typeof(LineRenderer));beam=go.GetComponent<LineRenderer>();beam.sharedMaterial=ActorFactory.Energy(tune.tint.a>0?tune.tint:BeamTone);
            beam.positionCount=2;beam.startWidth=.32f;beam.endWidth=.16f;beam.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;beam.receiveShadows=false;
            Sound("laserSmall_000",.22f,.5f);Debug.Log("GUARD_SWEEP "+def.id);
        }
        void Sweep()
        {
            sweepT+=Time.deltaTime;float now=BeamAngle;
            if(sweepMark!=null&&PartyController.Current!=null)
                foreach(var hero in PartyController.Current.members)
                {
                    if(!hero.Health.Alive||swept.Contains(hero.Health)||!sweepMark.Contains(hero.transform.position))continue;
                    var off=Flat(hero.transform.position-body.position);float bearing=off.sqrMagnitude>.01f?Vector3.SignedAngle(sweepAim,off,Vector3.up):0;
                    // The beam has come round to him (from whichever side it started): he is struck as it passes, once.
                    if(sweepSide>0?bearing>now:bearing<now)continue;
                    if(!CombatCover.Clear(Chest,hero.transform.position+Vector3.up*(hero.flight?0:.9f)))continue;
                    swept.Add(hero.Health);hero.Health.Receive(brain.Blow(Second,HitWeight.Heavy));
                }
            if(sweepT<SweepSeconds)return;
            sweeping=false;brain.Acting=false;if(sweepMark!=null)sweepMark.Release();sweepMark=null;Unbeam();
            brain.Rest(Second!=null?Second.cooldown:1.8f);loweredUntil=Time.time+Down;Sound("doorOpen_000",.2f,.6f);Debug.Log("GUARD_DOWN "+def.id);
        }
        void DrawBeam()
        {
            var from=Chest+Vector3.up*.3f;var heading=Quaternion.Euler(0,BeamAngle,0)*sweepAim;
            beam.SetPosition(0,from+heading*.6f);beam.SetPosition(1,CombatCover.Ground(from+heading*Reach+Vector3.up)+Vector3.up*.15f);
        }
        void Unbeam(){if(beam!=null)Object.Destroy(beam.gameObject);beam=null;}

        // ------------------------------------------------------------------ a rank (tune.count)
        /// <summary>How far apart a rank stands, shoulder to shoulder; how near guardians must be to form one; how fast it turns as a share of one's own turn; how long its others stand
        /// open when its middle falls.</summary>
        public const float Spacing=2.3f,Gathers=7,RankTurn=.6f,Unlocked=1.6f;
        sealed class Rank
        {
            public readonly List<EnemyGuardian> members=new();public Vector3 centre,facing,home;public int frame=-1,thrust;public float thrustAt;
            public EnemyGuardian Middle=>members.Count>=3?members[members.Count/2]:null;
        }
        static readonly List<Rank> ranks=new();
        Rank rank;bool alone;int thrustSeen;
        bool Ranked=>tune.count>1&&!alone;
        /// <summary>In a rank of three whose middle one is broken: every plate in it is down.</summary>
        bool Unlocked3=>rank!=null&&rank.members.Count>=3&&rank.Middle!=null&&rank.Middle.health.Broken;
        /// <summary>Its place in a rank (-1 for none) and how many stand in it: what a check reads.</summary>
        public int RankPlace=>rank!=null?rank.members.IndexOf(this):-1;
        public int RankSize=>rank!=null?rank.members.Count:0;
        public bool InTheMiddle=>rank!=null&&rank.Middle==this;
        /// <summary>Stands in its rank's line: answers false when it has none worth the name (one left), and the guardian fights alone.</summary>
        bool FormUp(CombatActor victim)
        {
            if(rank==null)Join();
            if(rank==null||rank.members.Count<2)return false;
            if(rank.frame!=Time.frameCount)Lead(victim);
            int place=rank.members.IndexOf(this);var right=Vector3.Cross(Vector3.up,rank.facing);
            var slot=rank.centre+right*((place-(rank.members.Count-1)*.5f)*Spacing);var toSlot=Flat(slot-body.position);
            if(toSlot.magnitude>.35f)brain.Walk(toSlot,def.speed*1.4f);
            Face(rank.facing,Turn);
            var to=Flat(victim.transform.position-body.position);float distance=to.magnitude;
            // The rank's thrust: every one of it marks its line together, down the way the rank faces.
            // One that missed the call (broken, or still told) lets it go: a spear a second late is no part of the wall.
            if(rank.thrust!=thrustSeen){thrustSeen=rank.thrust;if(Time.time-rank.thrustAt<.15f&&brain.Sees(victim)){Long(victim,body.forward,Second);return true;}}
            if(brain.Rested&&distance<Range&&Vector3.Angle(body.forward,to)<40&&brain.Sees(victim)){Slam(victim,body.forward);return true;}
            return true;
        }
        /// <summary>Once a frame, by whichever of the rank ticks first: where it faces, where it stands, and whether it thrusts.</summary>
        void Lead(CombatActor victim)
        {
            rank.frame=Time.frameCount;
            var middle=Vector3.zero;foreach(var member in rank.members)middle+=member.body.position;middle/=rank.members.Count;
            var to=Flat(victim.transform.position-rank.centre);float distance=to.magnitude;
            if(distance>.1f)rank.facing=Vector3.RotateTowards(rank.facing,to/distance,Turn*RankTurn*Mathf.Deg2Rad*Time.deltaTime,0);
            // It advances at a walk, no further ahead of its slowest than a stride, and no further from where it woke than its post.
            float pace=def.speed;foreach(var member in rank.members)pace=Mathf.Min(pace,def.speed*member.health.Pace);
            if(distance>Range*.9f&&Flat(victim.transform.position-rank.home).magnitude<Post+Range)rank.centre+=to/distance*Mathf.Min(distance-Range*.9f,pace*Time.deltaTime);
            var lag=Flat(rank.centre-middle);if(lag.magnitude>1.2f)rank.centre=middle+Vector3.up*(rank.centre.y-middle.y)+lag.normalized*1.2f;
            var homeward=Flat(rank.centre-rank.home);if(homeward.magnitude>Post)rank.centre-=homeward.normalized*(homeward.magnitude-Post);
            // Off the end of its reach and before it: the rank thrusts, all of it at once, when every one of it is free to.
            bool ready=true;foreach(var member in rank.members)ready&=member.brain.Rested&&!member.brain.Telegraphing&&!member.sweeping&&Time.time>=member.loweredUntil;
            if(ready&&distance>=Range&&distance<Reach&&Vector3.Angle(rank.facing,to)<22&&Second!=attack){rank.thrust++;rank.thrustAt=Time.time;Debug.Log($"RANK_THRUST {def.id} {rank.members.Count}");}
        }
        void Join()
        {
            foreach(var other in ranks)
            {
                if(other.members.Count==0||other.members.Count>=tune.count||other.members[0].def.id!=def.id)continue;
                if(Flat(other.centre-body.position).magnitude>Gathers)continue;
                rank=other;break;
            }
            if(rank==null){rank=new Rank{centre=body.position,home=body.position,facing=body.forward};ranks.Add(rank);}
            rank.members.Add(this);Order();
            if(rank.members.Count>1){var middle=Vector3.zero;foreach(var member in rank.members)middle+=member.body.position;rank.centre=rank.home=middle/rank.members.Count;}
            thrustSeen=rank.thrust;
        }
        /// <summary>Left to right as the rank faces, so the middle one is the one in the middle.</summary>
        void Order()
        {
            var right=Vector3.Cross(Vector3.up,rank.facing);
            rank.members.Sort((a,b)=>Vector3.Dot(a.body.position,right).CompareTo(Vector3.Dot(b.body.position,right)));
        }
        void LeaveRank(bool fell)
        {
            if(rank==null)return;var was=rank;bool middle=was.Middle==this;
            was.members.Remove(this);rank=null;
            if(was.members.Count==0){ranks.Remove(was);return;}
            if(!fell||!middle)return;
            // The middle one is down and the rank comes apart: its shields unlock and each of the others stands open a moment, then fights alone.
            Debug.Log("RANK_BROKEN "+def.id);
            foreach(var member in was.members.ToArray())
            {
                member.rank=null;member.alone=true;
                if(member.health.Alive){member.health.Expose(Unlocked);if(member.brain.Telegraphing)member.brain.Interrupt();}
            }
            was.members.Clear();ranks.Remove(was);
        }

        static Material plateLook;
        /// <summary>The plate's look, one for every guardian: a see-through shield in the plate's green with a lit rim and seams (Lattice/ShieldPlate, drawn from both sides). It was
        /// the energy bubble on this strip, whose two faces shared their vertices: its normals summed to nothing, and it drew as a flat slab of lime (s46/047).</summary>
        static Material PlateLook()
        {
            if(plateLook!=null)return plateLook;
            var shader=Resources.Load<Shader>("Effects/ShieldPlate");if(shader==null)return ActorFactory.Energy(PlateTone);
            plateLook=new Material(shader){name="guardian plate (runtime)"};plateLook.SetColor("_BaseColor",PlateTone);return plateLook;
        }
        /// <summary>A standing strip of an arc, facing out, centred on the body's forward: one face, which its shader draws from both sides; u runs along the arc, v up it.</summary>
        static Mesh ArcMesh(float radius,float height,float degrees)
        {
            const int steps=14;var vertices=new Vector3[(steps+1)*2];var triangles=new int[steps*6];var uv=new Vector2[vertices.Length];
            for(int i=0;i<=steps;i++)
            {
                float angle=(i/(float)steps-.5f)*degrees*Mathf.Deg2Rad;var at=new Vector3(Mathf.Sin(angle)*radius,0,Mathf.Cos(angle)*radius);
                vertices[i*2]=at+Vector3.up*.15f;vertices[i*2+1]=at+Vector3.up*height;uv[i*2]=new Vector2(i/(float)steps,0);uv[i*2+1]=new Vector2(i/(float)steps,1);
            }
            for(int i=0,t=0;i<steps;i++)
            {
                int a=i*2,b=a+1,c=a+2,d=a+3;
                // One face: it is seen from in front and, over its shoulder, from behind, and its shader culls neither.
                triangles[t++]=a;triangles[t++]=b;triangles[t++]=c;triangles[t++]=c;triangles[t++]=b;triangles[t++]=d;
            }
            var mesh=new Mesh{name="guardian plate"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
