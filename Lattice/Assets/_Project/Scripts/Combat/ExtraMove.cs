using System;
using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>What fills the EXTRA gauge (S3): a hero's landed hits by how much of a whole hit each was, the hits they take by their share of an Integrity bar, and
    /// every Flash Move and Flash Guard. The partner's fills at half rate (ExtraGauge.PartnerShare). An EXTRA's own hits pay nothing back.</summary>
    public static class ExtraFill
    {
        static bool Lead(CombatActor actor)=>PartyController.Current==null||PartyController.Current.Active==actor;
        /// <summary>From CombatActor.PayHit, so it counts what Charge counts: a hit on an enemy or on an organ the bar says to break. A weak spot or a break pays no more.
        /// A partner's hits are already scaled down; the scale comes back out, so half rate is half and not a third.</summary>
        public static void Dealt(CombatActor actor,DamagePacket packet,float amount)
        {
            if(actor==null||amount<=0||packet.tag==ExtraMove.Tag)return;
            float whole=Mathf.Max(1,(actor.character=="Sela"?actor.rangedDamage:actor.damage)*Mathf.Max(.01f,actor.damageScale));
            ExtraGauge.Add(actor.character,ExtraGauge.PerHit*Mathf.Min(ExtraGauge.MostHit,Mathf.Min(amount,packet.amount)/whole),Lead(actor));
        }
        public static void Taken(CombatActor actor,float amount)
        {
            if(actor==null||amount<=0)return;
            ExtraGauge.Add(actor.character,ExtraGauge.PerBarTaken*amount/Mathf.Max(1,actor.Health.maximum),Lead(actor));
        }
        public static void Flashed(CombatActor actor,int times=1)
        {
            if(actor!=null&&times>0)ExtraGauge.Add(actor.character,ExtraGauge.PerFlash*times,Lead(actor));
        }
    }

    /// <summary>
    /// A hero's EXTRA move (S3), the Ys-style super D201 left unbuilt. RB+LT (or the key) with a full gauge, by the hero the player is playing: the partner never fires one.
    /// A cut-in comes first: half a second with the world in the Flash's slow motion while the UI slides the hero's portrait and the move's name across (<see cref="Began"/>),
    /// and then the move plays at full speed. Through all of it the hero can't be hurt, staggered or steered, and no roll or guard of their own cuts it short.
    /// <list type="bullet">
    /// <item>Taren's Anvil Chorus: he closes on his target, lands eight hammer blows of 1x Output and a last one of 4x, twelve in all, with enough Break to open nearly anything.</item>
    /// <item>Sela's Lattice Aria: eight volleys of beams from above on every enemy on the screen at 1x and a last chord at 2x, ten in all, and the chord mends both heroes by
    /// 30% of their Integrity. It can't revive: a hero who's down stays down.</item>
    /// <item>Idra's Last One Out (S6a): a breaching charge down a line at 4x through everything on it, a shockwave where it ends at 5x, nine in all, and the other hero
    /// has half their Integrity back, on their feet if they were down. The least damage of the three, and the only one that raises.</item>
    /// </list>
    /// Both are the same move in flight, with a ship's reach. Bosses: every hit goes through Health.Receive, so a phase shift's invulnerability holds against it, and one EXTRA
    /// takes at most half of the phase it lands in and never crosses its line (BossController.ExtraFloor, D375): what's left of it past that is spent on the floor.
    /// </summary>
    public sealed class ExtraMove:MonoBehaviour
    {
        /// <summary>The tag on an EXTRA's hits, and the bark situations: each hero's shout and the lesson the first time a gauge is full.</summary>
        public const string Tag="extra",Shout="extra",Lesson="teach.extra";
        /// <summary>How long the cut-in holds the fight, in real seconds.</summary>
        public const float CutIn=.5f;
        public const int Beats=8;
        /// <summary>Seconds between blows or volleys, the last blow's wind-up, and the rest after it.</summary>
        public const float Beat=.1f,WindUp=.3f,Rest=.35f,Close=.16f;
        public const float ChorusBlow=1,ChorusLast=4,ChorusBreak=40,ChorusLastBreak=220;
        public const float AriaVolley=1,AriaChord=2,AriaBreak=30,AriaChordBreak=100,AriaHeal=.3f,AriaReach=45;
        public const int AriaMost=16;
        /// <summary>What each move adds up to on one body, in the hero's Output.</summary>
        public const float ChorusTotal=Beats*ChorusBlow+ChorusLast,AriaTotal=Beats*AriaVolley+AriaChord;
        /// <summary>Last One Out: the charge and the shockwave in Output, their Break, what the other hero gets back, how long the charge takes, and what it adds up to on one body.</summary>
        public const float OutCharge=4,OutShock=5,OutChargeBreak=120,OutShockBreak=200,OutHeal=.5f,OutSeconds=.22f,OutTotal=OutCharge+OutShock;
        /// <summary>How far the charge carries her, what it strikes on the way, and what the shockwave reaches: (on foot, in flight).</summary>
        static readonly Vector2 OutLength=new(10,12),OutWidth=new(2.2f,3),OutReach=new(6,9);
        /// <summary>How far Taren closes before the first blow, and what a blow reaches: (on foot, in flight).</summary>
        static readonly Vector2 Stand=new(1.9f,3.5f),Travel=new(9,11),BlowReach=new(3.1f,5.2f),LastReach=new(4.6f,8);
        /// <summary>The most he steps after his target between two blows.</summary>
        static readonly Vector2 Follow=new(1.2f,2);
        static readonly Color Ember=new(1.6f,.75f,.2f),Cyan=new(.3f,1,1.6f),Mint=new(.45f,1.5f,1.1f);
        /// <summary>A rising scale, one note a volley.</summary>
        static readonly float[] Scale={1,1.12f,1.26f,1.33f,1.5f,1.68f,1.89f,2};

        /// <summary>The cut-in begins: (hero, the move). The UI draws it; Combat only holds the clock.</summary>
        public static event Action<CombatActor,SkillDef> Began;
        public static event Action<CombatActor,SkillDef> Ended;
        public bool Performing{get;private set;}
        /// <summary>When the player last pressed for it with the gauge short, for the HUD's flash.</summary>
        public float RefusedAt{get;private set;}=-99;
        /// <summary>What the last (or the running) move has dealt, all bodies together.</summary>
        public float Dealt{get;private set;}
        public int Released{get;private set;}
        /// <summary>This hero's EXTRA, or none until its definition is built.</summary>
        public SkillDef Definition{get{var hero=actor!=null?GameCatalog.Find<CharacterDef>(actor.character):null;return hero!=null?hero.extra:null;}}

        CombatActor actor;PlayerBrain brain;int flashes;bool slowed;Material beamMaterial;
        /// <summary>Each boss's floor for this move, read when the move first touches it.</summary>
        readonly Dictionary<Health,float> floors=new();
        readonly List<Health> reached=new();
        static readonly Collider[] Near=new Collider[128];
        /// <summary>A lesson waits for the line before it, as FlashLessons' do: BarkPanel holds one 3.5 s.</summary>
        const float LineStays=3.6f;
        static float lastLine=-99;static bool listening;

        void Awake()
        {
            actor=GetComponent<CombatActor>();GetComponent<Health>().Damaged+=OnHurt;
            if(!listening){listening=true;BarkService.Spoken+=(_,__)=>lastLine=Time.unscaledTime;}
        }
        void Start(){flashes=actor.FlashMoves+actor.FlashGuards;}
        void OnDestroy(){if(TryGetComponent<Health>(out var health))health.Damaged-=OnHurt;}
        void OnHurt(Health _,DamagePacket __,float amount)=>ExtraFill.Taken(actor,amount);
        void OnDisable()
        {
            StopAllCoroutines();
            if(slowed){slowed=false;GameTime.EndFlash();}
            if(Performing){Performing=false;actor.committedUntil=actor.rootedUntil=0;Ended?.Invoke(actor,Definition);}
            Pass(false,0);
            if(beamMaterial!=null){Destroy(beamMaterial);beamMaterial=null;}
        }
        void Update()
        {
            if(actor==null||GameServices.Current==null)return;
            // CombatActor counts its Flashes; nothing there needs to know about this gauge.
            int now=actor.FlashMoves+actor.FlashGuards;if(now>flashes)ExtraFill.Flashed(actor,now-flashes);flashes=now;
            var party=PartyController.Current;
            if(Performing||party==null||party.Active!=actor||GameTime.Paused||GameServices.Current.Input.Blocked)return;
            Teach();
            if(brain==null)brain=GetComponent<PlayerBrain>();
            if(brain!=null&&brain.enabled&&!brain.AutoPilot&&GameServices.Current.Input.Pressed("Extra"))TryFire();
        }
        /// <summary>The first full gauge of a save, in a fight: the partner says what it is (or the hero, alone), and a card says what to press.</summary>
        void Teach()
        {
            if(!ExtraGauge.Ready(actor.character)||Time.unscaledTime-lastLine<LineStays)return;
            var zone=ZoneController.Current;var move=Definition;if(zone==null||!zone.Combat||move==null)return;
            var flags=GameServices.Current.Flags;if(flags.GetBool(ExtraGauge.TaughtFlag))return;
            flags.SetBool(ExtraGauge.TaughtFlag,true);
            var voice=actor;foreach(var member in PartyController.Current.members)if(member!=actor&&member.Health.Alive)voice=member;
            BarkService.Play(voice.character,Lesson,true);
            Notices.Post("EXTRA READY",$"{ExtraGauge.Binding(actor.flight)}  releases {DisplayNames.Speaker(actor.character)}'s {move.displayName}. The gauge fills as you deal and take damage, and with every Flash.");
            Debug.Log("EXTRA_TAUGHT "+actor.character);
        }
        /// <summary>Releases the move if this is the player's hero, in a fight, on their feet, with a full gauge. A press with the gauge short is refused and says so.</summary>
        public bool TryFire()
        {
            var move=Definition;var party=PartyController.Current;var zone=ZoneController.Current;
            if(Performing||move==null||party==null||party.Active!=actor||zone==null||!zone.Combat||GameTime.Paused||!ExtraGauge.Unlocked)return false;
            if(!actor.Health.Alive||actor.Recovering||actor.ChangingForm||actor.State==ActorState.Stagger||actor.State==ActorState.Down)return false;
            if(!ExtraGauge.Spend(actor.character)){RefusedAt=GameTime.Now;return false;}
            StartCoroutine(Perform(move));return true;
        }

        IEnumerator Perform(SkillDef move)
        {
            Performing=true;Dealt=0;floors.Clear();Released++;
            bool chorus=move.kind!=SkillKind.LatticeAria,leaving=move.kind==SkillKind.LastOneOut;float body=leaving?OutSeconds+WindUp+Rest:chorus?Close+Beats*Beat+WindUp+Rest:Beats*Beat+WindUp+Rest;
            actor.Guard(false);Halt();
            Face(Aim());Pose("Buff",CutIn+.1f,CutIn+body);
            Debug.Log($"EXTRA_BEGIN {actor.character} {move.id}");
            Began?.Invoke(actor,move);BarkService.Play(actor.character,Shout,true);
            HeroSkills.Sound(actor,transform.position+Vector3.up,"forceField_000",.26f,chorus?.7f:1.4f);
            // The Flash's own slow motion, counted on the hero's clock as a Flash is: a pause holds it, and a hit-stop doesn't shorten it.
            GameTime.BeginFlash();slowed=true;
            for(float t=0;t<CutIn;t+=actor.ActionDelta)yield return null;
            slowed=false;GameTime.EndFlash();
            float power=1+Mathf.Max(0,actor.resonance-10)*.025f;
            yield return leaving?LastOut(power,body):chorus?Chorus(power,body):Aria(power,body);
            Performing=false;actor.committedUntil=actor.rootedUntil=0;
            if(beamMaterial!=null){Destroy(beamMaterial,.5f);beamMaterial=null;}
            Debug.Log($"EXTRA_OK {actor.character} {move.id} dealt={Dealt:0}");
            Ended?.Invoke(actor,move);
        }

        // ---------- Anvil Chorus ----------
        IEnumerator Chorus(float power,float left)
        {
            bool flight=actor.flight;int form=flight?1:0;var aim=Aim();Face(aim);
            // He goes to what he's fighting: an EXTRA spent on the air a stride short of it would be the worst press in the game.
            float gap=actor.target!=null&&actor.target.Alive?Planar(actor.target.transform.position-transform.position).magnitude:0,travel=Mathf.Clamp(gap-Stand[form],0,Travel[form]);
            Pose("Dash",Close+.04f,left);
            if(travel>.3f)HeroSkills.Sound(actor,transform.position+Vector3.up,"forceField_000",.2f,.9f);
            for(float t=0;t<Close;)
            {
                float dt=Held?0:actor.ActionDelta;
                if(travel>.3f&&dt>0)
                {
                    Carry(aim*(travel*Mathf.Min(dt,Close-t)/Close),aim);
                    CombatVfx.Burst(Chest,GroundRing.Friendly,"lunge");
                }
                t+=dt;yield return null;
            }
            left-=Close;
            for(int i=0;i<Beats;i++)
            {
                aim=Aim();Face(aim);
                // He follows what he's hammering: every blow shoves a small body back a little, and eight of them would have walked it out from under the last.
                gap=actor.target!=null&&actor.target.Alive?Planar(actor.target.transform.position-transform.position).magnitude:0;
                if(gap>Stand[form]+.3f)Carry(aim*Mathf.Min(gap-Stand[form],Follow[form]),aim);
                Pose(flight?(i%2==0?"Cleave":"Pulse"):"Attack"+(i%3+1),Beat*1.6f,left);
                yield return Wait(Beat,left);left-=Beat;
                var centre=Ahead(aim,flight?2.4f:1.3f);
                Strike(centre,BlowReach[form],Packet(actor.damage*ChorusBlow*power,DamageType.Kinetic,ChorusBreak,HitWeight.Medium));
                // The hammers fall left and right of the anvil, each a note higher.
                CombatVfx.Burst(centre+Vector3.Cross(Vector3.up,aim)*(i%2==0?-.8f:.8f),Ember,"hit");
                SkillRing.Expand(Floor(centre),BlowReach[form]*.55f,GroundRing.Friendly,.16f,flight);
                HeroSkills.Sound(actor,centre,"impactMetal_000",.2f,.8f+i*.09f);
            }
            aim=Aim();Face(aim);
            gap=actor.target!=null&&actor.target.Alive?Planar(actor.target.transform.position-transform.position).magnitude:0;
            if(gap>Stand[form]+.3f)Carry(aim*Mathf.Min(gap-Stand[form],Follow[form]),aim);
            Pose(flight?"Pulse":"Cleave",WindUp+.25f,left);
            var last=Ahead(aim,flight?2:1.2f);
            SkillRing.Expand(Floor(last),LastReach[form],GroundRing.Friendly,WindUp,flight,true,LastReach[form]*1.7f);
            HeroSkills.Sound(actor,last,"forceField_000",.2f,.55f);
            yield return Wait(WindUp,left);left-=WindUp;
            last=Ahead(aim,flight?2:1.2f);
            Strike(last,LastReach[form],Packet(actor.damage*ChorusLast*power,DamageType.Kinetic,ChorusLastBreak,HitWeight.Finisher));
            SkillRing.Expand(Floor(last),LastReach[form],GroundRing.Friendly,.4f,flight);
            CombatVfx.Burst(last,Ember,"kill");HeroSkills.Sound(actor,last,"explosionCrunch_000",.26f,.65f);
            // It comes down whether or not anyone is left under it.
            Impact.Thud(actor,aim);
            yield return Wait(Rest,left);
        }

        // ---------- Lattice Aria ----------
        IEnumerator Aria(float power,float left)
        {
            bool flight=actor.flight;Face(Aim());
            Pose("Buff",Beats*Beat+.1f,left);
            for(int i=0;i<Beats;i++)
            {
                yield return Wait(Beat,left);left-=Beat;
                foreach(var body in OnScreen())
                {
                    Beam(body.transform.position,.34f);
                    Land(body,Packet(actor.rangedDamage*AriaVolley*power,DamageType.Beam,AriaBreak,HitWeight.Medium));
                }
                // The sky is full of them, whether or not anything stands under each: with one enemy up it's still the whole screen.
                for(int stray=0;stray<3;stray++)if(Stray(out var point))Beam(point,.2f);
                HeroSkills.Sound(actor,transform.position+Vector3.up,"laserSmall_000",.16f,Scale[i%Scale.Length]);
            }
            Face(Aim());Pose("Shoot",WindUp+.2f,left);
            HeroSkills.Sound(actor,transform.position+Vector3.up,"forceField_000",.2f,1.8f);
            yield return Wait(WindUp,left);left-=WindUp;
            foreach(var body in OnScreen())
            {
                Beam(body.transform.position,.7f);CombatVfx.Burst(body.transform.position+Vector3.up*(flight?0:.9f),Cyan,"kill");
                Land(body,Packet(actor.rangedDamage*AriaChord*power,DamageType.Beam,AriaChordBreak,HitWeight.Finisher));
            }
            // The chord mends whoever is standing. A hero who's down stays down: Heal on them would be a revive.
            foreach(var member in PartyController.Current.members)
            {
                if(member==null||!member.Health.Alive)continue;
                member.Health.Heal(member.Health.maximum*AriaHeal);
                var at=member.transform.position;
                CombatVfx.Burst(at+Vector3.up*(flight?.8f:1),Mint,"shape");SkillRing.Expand(Floor(at+Vector3.up*.5f),1.8f,Mint,.5f,flight);
            }
            HeroSkills.Sound(actor,transform.position+Vector3.up,"explosionCrunch_000",.24f,1.5f);
            AudioManager.PlayAt("ui_confirm",transform,transform.position+Vector3.up,.2f);
            yield return Wait(Rest,left);
        }
        // ---------- Last One Out ----------
        readonly HashSet<Health> charged=new();readonly List<Collider> passed=new();
        IEnumerator LastOut(float power,float left)
        {
            bool flight=actor.flight;int form=flight?1:0;var aim=Aim();Face(aim);
            Pose("Dash",OutSeconds+.04f,left);
            HeroSkills.Sound(actor,transform.position+Vector3.up,"forceField_000",.24f,.6f);
            var charge=Packet(actor.damage*OutCharge*power,DamageType.Kinetic,OutChargeBreak,HitWeight.Heavy);
            // Small bodies on the line don't stop her: she's through them, and each is struck once as she passes. A wall stops her, as it stops anyone.
            Pass(true,OutLength[form]+4);charged.Clear();
            for(float t=0;t<OutSeconds;)
            {
                float dt=Held?0:actor.ActionDelta;
                if(dt>0)
                {
                    Carry(aim*(OutLength[form]*Mathf.Min(dt,OutSeconds-t)/OutSeconds),aim);
                    Strike(Ahead(aim,.6f),OutWidth[form],charge,charged);
                    CombatVfx.Burst(Chest,Ember,"lunge");
                }
                t+=dt;yield return null;
            }
            Pass(false,0);left-=OutSeconds;
            // Where the charge ends she plants, and the ring that gathers round her is what the shockwave reaches.
            Pose(flight?"Pulse":"Attack3",WindUp+.25f,left);
            SkillRing.Expand(Floor(transform.position+Vector3.up*.5f),OutReach[form],GroundRing.Friendly,WindUp,flight,true,OutReach[form]*1.7f);
            HeroSkills.Sound(actor,Chest,"forceField_000",.2f,.5f);
            yield return Wait(WindUp,left);left-=WindUp;
            Strike(Chest,OutReach[form],Packet(actor.damage*OutShock*power,DamageType.Kinetic,OutShockBreak,HitWeight.Finisher));
            SkillRing.Expand(Floor(transform.position+Vector3.up*.5f),OutReach[form],GroundRing.Friendly,.4f,flight);
            CombatVfx.Burst(Chest,Ember,"kill");HeroSkills.Sound(actor,Chest,"explosionCrunch_000",.26f,.6f);
            Impact.Thud(actor,aim);
            // And nobody is left behind: the other hero has half their Integrity back, and is on their feet if they were down. Heal on a downed hero is the revive.
            foreach(var member in PartyController.Current.members)
            {
                if(member==null||member==actor)continue;
                bool down=!member.Health.Alive;member.Health.Heal(member.Health.maximum*OutHeal);
                if(down)Debug.Log("EXTRA_RAISED "+member.character);
                var at=member.transform.position;
                CombatVfx.Burst(at+Vector3.up*(flight?.8f:1),Ember,"shape");SkillRing.Expand(Floor(at+Vector3.up*.5f),1.8f,GroundRing.Friendly,.5f,flight);
            }
            AudioManager.PlayAt("ui_confirm",transform,transform.position+Vector3.up,.2f);
            yield return Wait(Rest,left);
        }
        /// <summary>For the charge, bodies she can go through stop being solid to her, as they do for Bulwark Rush: a boss, a heavy body or a mine holds its ground.</summary>
        void Pass(bool on,float reach)
        {
            if(!TryGetComponent<CharacterController>(out var hull))return;
            if(!on){foreach(var body in passed)if(body!=null)Physics.IgnoreCollision(hull,body,false);passed.Clear();return;}
            foreach(var health in Health.All)
            {
                if(health==null||health.friendly==actor.Health.friendly||(health.transform.position-transform.position).sqrMagnitude>reach*reach)continue;
                if(!health.TryGetComponent<EnemyBrain>(out var brain)||!brain.CanBeMoved||brain.Heavy||!health.TryGetComponent<CharacterController>(out var body))continue;
                Physics.IgnoreCollision(hull,body,true);passed.Add(body);
            }
        }

        /// <summary>Every enemy the player can see, nearest first: on the screen, in reach and able to be hurt. Cover doesn't count: these come down from above.</summary>
        List<Health> OnScreen()
        {
            reached.Clear();var camera=Camera.main;var from=transform.position;float lift=actor.flight?0:.9f;
            foreach(var body in Health.All)
            {
                if(body==null||body.friendly==actor.Health.friendly||!body.Targetable)continue;
                if(Planar(body.transform.position-from).sqrMagnitude>AriaReach*AriaReach)continue;
                if(camera!=null){var at=camera.WorldToViewportPoint(body.transform.position+Vector3.up*lift);if(at.z<=0||at.x<0||at.x>1||at.y<0||at.y>1)continue;}
                reached.Add(body);
            }
            reached.Sort((a,b)=>(a.transform.position-from).sqrMagnitude.CompareTo((b.transform.position-from).sqrMagnitude));
            if(reached.Count>AriaMost)reached.RemoveRange(AriaMost,reached.Count-AriaMost);
            return reached;
        }
        /// <summary>Somewhere on the screen, on the hero's level, for a beam that strikes nothing.</summary>
        bool Stray(out Vector3 point)
        {
            point=default;var camera=Camera.main;if(camera==null)return false;
            var ray=camera.ViewportPointToRay(new Vector3(UnityEngine.Random.Range(.08f,.92f),UnityEngine.Random.Range(.14f,.9f),0));
            if(Mathf.Abs(ray.direction.y)<.05f)return false;
            float along=(transform.position.y-ray.origin.y)/ray.direction.y;if(along<=0)return false;
            point=ray.origin+ray.direction*along;
            return Planar(point-transform.position).sqrMagnitude<AriaReach*AriaReach;
        }
        void Beam(Vector3 point,float width)
        {
            if(beamMaterial==null){beamMaterial=new Material(Resources.Load<Material>("Effects/flare_01"));beamMaterial.SetTexture("_BaseMap",Texture2D.whiteTexture);}
            var go=new GameObject("Aria beam");var line=go.AddComponent<LineRenderer>();line.positionCount=2;line.useWorldSpace=true;
            line.startWidth=width;line.endWidth=width*.35f;line.sharedMaterial=beamMaterial;line.startColor=new Color(.5f,1.4f,2,0);line.endColor=new Color(.6f,1.6f,2.2f,1);
            line.SetPosition(0,point+Vector3.up*14);line.SetPosition(1,point);line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Destroy(go,.14f);
            CombatVfx.Burst(point+Vector3.up*(actor.flight?0:.2f),Cyan,"hit");
        }

        // ---------- shared pieces ----------
        static Vector3 Planar(Vector3 v){v.y=0;return v;}
        Vector3 Chest=>transform.position+Vector3.up*(actor.flight?.8f:.9f);
        /// <summary>Where a blow is centred: ahead of the hero, and never past a wall they stand at, or it would reach what's behind it.</summary>
        Vector3 Ahead(Vector3 aim,float distance)=>Chest+aim*(CombatCover.Sweep(Chest,Chest+aim*distance,.2f,out var wall)?Mathf.Max(0,wall.distance-.2f):distance);
        Vector3 Floor(Vector3 at)=>actor.flight?at:CombatCover.Ground(at+Vector3.up*1.5f);
        /// <summary>A conversation or a load holds the move as it holds the fight; a pause holds the hero's clock already.</summary>
        bool Held=>GameServices.Current!=null&&GameServices.Current.Input.Blocked;
        /// <summary>At the target, wherever the hero was looking: an EXTRA turns to what it's for.</summary>
        Vector3 Aim()
        {
            if(actor.target!=null&&actor.target.Alive){var to=Planar(actor.target.transform.position-transform.position);if(to.sqrMagnitude>.01f)return to.normalized;}
            var facing=Planar(actor.motor!=null?actor.motor.Facing:transform.forward);return facing.sqrMagnitude>.01f?facing.normalized:Vector3.forward;
        }
        void Face(Vector3 aim)
        {
            if(actor.flight){if(TryGetComponent<FlightMotor>(out var ship))ship.KeepFacing(aim);return;}
            transform.rotation=Quaternion.LookRotation(aim);if(TryGetComponent<GroundMotor>(out var ground))ground.KeepFacing(aim);
        }
        void Halt(){if(TryGetComponent<GroundMotor>(out var ground))ground.Halt();if(TryGetComponent<FlightMotor>(out var ship))ship.Halt();}
        /// <summary>The move carries the hero itself, through the controller, so walls and bodies still hold them: the motor's own clock is stopped (Rooted).</summary>
        void Carry(Vector3 delta,Vector3 heading)
        {
            delta.y=0;
            if(!actor.flight){if(TryGetComponent<GroundMotor>(out var ground))ground.MoveAction(delta,heading);return;}
            if(TryGetComponent<CharacterController>(out var hull)&&hull.enabled)hull.Move(delta);
            Face(heading);
        }
        /// <summary>A pose of the move, and the hold that goes with it for what's left: untouchable, unstaggered, unsteered. Each pose is a new action, so the root is taken again.</summary>
        void Pose(string clip,float duration,float left){actor.Perform(clip,duration,left);Arm(left);}
        void Arm(float left)
        {
            actor.committedUntil=GameTime.Now+left;actor.Root(left);
            actor.Health.InvulnerableUntil=Mathf.Max(actor.Health.InvulnerableUntil,GameTime.Now+left);
        }
        /// <summary>Waits on the hero's clock. Held by a conversation, the holds would run out behind it: they're taken again as it lets go.</summary>
        IEnumerator Wait(float seconds,float left)
        {
            bool held=false;
            for(float t=0;t<seconds;)
            {
                if(Held)held=true;
                else{if(held){held=false;actor.Perform(actor.VisualAction,left-t,left-t);Arm(left-t);}t+=actor.ActionDelta;}
                yield return null;
            }
        }
        /// <summary>An EXTRA's hit: at the lead's whole strength even if the player swaps away mid-move, and tagged so it pays its own gauge nothing. It can crit, as any
        /// hit can, and it pays Charge as one of many does.</summary>
        DamagePacket Packet(float amount,DamageType type,float breakPower,HitWeight weight)
        {
            var packet=actor.Packet(amount/Mathf.Max(.01f,actor.damageScale),type,breakPower,Tag,weight);
            packet.chargeCut=CombatActor.MultiHitCut;return packet;
        }
        /// <summary>Every enemy with a part in the sphere and no wall between, once each; and once only across several strikes that share a set (a charge's passing).</summary>
        void Strike(Vector3 centre,float radius,DamagePacket packet,HashSet<Health> once=null)
        {
            reached.Clear();int count=Physics.OverlapSphereNonAlloc(centre,radius,Near,~0,QueryTriggerInteraction.Collide);
            for(int i=0;i<count;i++)
            {
                var part=Near[i];
                if(!part.TryGetComponent<Hurtbox>(out var box)||box.owner==null||box.owner.friendly==actor.Health.friendly||!box.owner.Targetable||reached.Contains(box.owner))continue;
                var nearest=part is MeshCollider mesh&&!mesh.convex?part.bounds.ClosestPoint(centre):part.ClosestPoint(centre);
                if(CombatCover.Clear(centre,nearest)&&(once==null||once.Add(box.owner)))reached.Add(box.owner);
            }
            foreach(var body in reached)Land(body,packet);
        }
        /// <summary>One hit on one body, straight on its Health: no weak spot doubles it, so the move adds up to what it says. On a boss it stops at the move's floor.</summary>
        void Land(Health body,DamagePacket packet)
        {
            if(!floors.TryGetValue(body,out float floor)){floor=body.TryGetComponent<BossController>(out var boss)?boss.ExtraFloor:0;floors[body]=floor;}
            if(floor>0)
            {
                float room=body.integrity-floor;if(room<=0)return;
                float would=Would(body,packet);if(would>room)packet.amount*=room/would;
            }
            Dealt+=body.Receive(packet);
            // What Receive made of it is Receive's to say: if that was more than the reckoning here, the line still holds.
            if(floor>0&&body.Alive&&body.integrity<floor)body.integrity=floor;
        }
        /// <summary>What Health.Receive will make of a hit on an enemy, as it reckons it.</summary>
        float Would(Health body,DamagePacket packet)
        {
            var reaction=body.reactionOwner!=null?body.reactionOwner:body;
            float damage=DamageMath.Resolve(packet.amount,packet.type,body.weakness,body.resistance,reaction.Broken,packet.isCrit);
            if(Time.time<reaction.ShieldUntil&&!reaction.Broken)damage*=.5f;
            if(reaction.Branded)damage*=Health.BrandTaken;
            return damage*GearEffects.Dealt(actor,packet,body,reaction);
        }
    }
}
