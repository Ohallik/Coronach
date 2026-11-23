using System.Collections.Generic;
using Lattice.Core;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>What a hit does beyond its damage: it holds the fight for a few frames, kicks the camera along the blow, throws sparks off the side it landed on, flashes and
    /// shoves the body, and lays a sound over the impact for its weight. It answers <see cref="Health.Impacted"/>, so only damage that resolved is ever answered, and only a
    /// packet that carries a <see cref="HitWeight"/>: one built by hand weighs nothing and changes nothing but integrity. The clock, the camera, the accents and the pad answer
    /// the hero the player holds and nobody else; a partner's hit still marks the body it struck. No random numbers are drawn here: the fight's own rolls come off that stream.</summary>
    public sealed class Impact:MonoBehaviour
    {
        /// <summary>One beat: how long the fight holds, how far the camera is kicked and for how long it rings, and what the pad's two motors do.</summary>
        public readonly struct Beat
        {
            /// <summary>Seconds of real time.</summary>
            public readonly float stop;
            /// <summary>A share of the view's height: .001 is about a pixel at 1080p.</summary>
            public readonly float shake;
            public readonly float ring,low,high;
            public Beat(float stopMilliseconds,float shake,float ring=.14f,float low=0,float high=0){stop=stopMilliseconds*.001f;this.shake=shake;this.ring=ring;this.low=low;this.high=high;}
        }
        /// <summary>Every number of the layer, in one place, to retune from captures. Arrays by weight run in <see cref="HitWeight"/>'s order: None, Tick, Light, Medium, Heavy, Finisher.</summary>
        public static class Tune
        {
            // ---- a hit the held hero lands
            public static readonly Beat[] Dealt={default,new(0,.001f,.08f),new(40,.002f,.12f,.1f,.25f),new(55,.0035f,.14f,.2f,.3f),new(70,.005f,.16f,.35f,.4f),new(90,.006f,.2f,.55f,.5f)};
            /// <summary>Milliseconds a crit and a weak spot add, what a crit multiplies the kick by, and all a resisted hit gets: a short clank and no kick.</summary>
            public const float CritStop=30,CritShake=1.3f,WeakSpotStop=20,ResistedStop=25;
            /// <summary>Needles, pellets and ticks kick the camera at most this often.</summary>
            public const float TickShakeEvery=.1f;
            public static readonly Beat Break=new(120,.008f,.25f,.6f,.6f),Kill=new(70,.006f,.2f,.4f,.3f),LastKill=new(130,.008f,.3f,.6f,.4f),BossKill=new(250,.015f,.5f,1,.8f);
            /// <summary>A kill is the fight's last when nothing else is awake this near.</summary>
            public const float LastKillRange=30;
            // ---- a hit that lands on the held hero, by its share of their integrity
            public static readonly Beat HurtLight=new(0,.004f,.14f,.25f,.2f),HurtMedium=new(50,.007f,.18f,.5f,.3f),HurtHeavy=new(90,.011f,.25f,.8f,.5f);
            public const float HurtMediumFrom=.05f,HurtHeavyFrom=.15f;
            public static readonly Beat GuardBlock=new(30,.003f,.12f,.2f,.1f),GuardBreak=new(110,.009f,.25f,.7f,.5f),FlashGuard=new(100,.006f,.2f,.3f,.6f),
                FlashMove=new(50,0,.1f,.1f,.4f),Downed=new(150,.012f,.35f,1,.6f),Thud=new(0,.004f,.18f,.3f,.2f);
            // ---- the clock's budget: stops are at most this share of any window, half as long while a Flash runs, and never shorter than a frame is worth
            public const float BudgetWindow=2,BudgetShare=.15f,FlashStops=.5f,LeastStop=.012f;
            /// <summary>The pad's pulse outlasts the stop by this.</summary>
            public const float RumbleTail=.05f;
            // ---- the body struck
            public static readonly int[] SparkCount={0,5,8,11,15,22};
            public static readonly float[] SparkSize={0,.14f,.2f,.24f,.3f,.38f},SparkSpeed={0,5,6,7,8.5f,10};
            /// <summary>How strongly a struck body lights (HitFlash), and for how long from the blow, through any stop it brings; a boss by this share of it. A tick's is faint: under a
            /// stream of needles a body would strobe.</summary>
            public static readonly float[] Flash={0,.25f,.55f,.65f,.75f,.9f};
            public const float FlashSeconds=.09f,BossFlash=.6f;
            /// <summary>A killing blow's flash goes out with the body, over this long; and no frame, however long, moves a dying body's paint or flash by more than this (a hitch is not a pop).</summary>
            public const float DyingFlashSeconds=.5f,MostStepAFrame=.06f;
            /// <summary>An unrigged body is pressed flat by this share and set back this far, for this long; a boss by this much of both.</summary>
            public static readonly float[] Squash={0,0,.05f,.07f,.1f,.14f},Recoil={0,0,.08f,.12f,.18f,.25f};
            public const float PoseSeconds=.14f,BossPose=.4f,BreakSquash=.18f;
            /// <summary>How far a blow carries a light body, a pack hunter or one of a swarm, over these seconds. Never one that has marked an attack.</summary>
            public static readonly float[] Knockback={0,0,.35f,.35f,.35f,.8f};
            public const float KnockbackSeconds=.1f;
            /// <summary>A struck hero's spine bows, and a struck hull rolls, by these degrees: a light, a medium and a heavy hit.</summary>
            public static readonly float[] Flinch={8,13,20},HullJolt={5,9,15};
            public const float FlinchSeconds=.24f,JoltSeconds=.32f;
            // ---- the screen's edge (ImpactHud): how red a hit goes by its class and how long it takes to clear, when the low-integrity pulse starts and how strong it is
            public static readonly float[] Vignette={.25f,.4f,.55f};
            public const float VignetteSeconds=.3f,DownedVignette=.7f,LowIntegrity=.3f,LowPulse=.2f,LowPulseEvery=1.1f,FlashMoveTint=.1f;
            // ---- the accents, under the skills' cue ceiling of .26 except a boss going down. The Flash Move's swell is the densest clip of the set (twice the others' RMS), so it sits lowest.
            public const float ThumpVolume=.2f,FinisherThumpVolume=.24f,FinisherThumpPitch=.86f,CritVolume=.18f,BreakVolume=.24f,KillVolume=.2f,LastKillPitch=.85f,BossDownVolume=.3f,
                GuardBlockVolume=.2f,GuardBreakPitch=.82f,HullHitVolume=.22f,HurtThumpPitch=.8f,FlashMoveVolume=.16f,SwapStrikeVolume=.2f,LevelUpVolume=.22f;
            // ---- colours. Nothing here is the orange, red or purple that means danger on the floor.
            public static readonly Color CritTint=new(1.8f,1.6f,.7f),BreakTint=new(1.5f,1.3f,.6f),GuardTint=new(.6f,1.1f,1.6f),FlashGuardTint=new(1.7f,1.4f,.6f),FlashMoveColor=new(.3f,1.1f,1.6f),
                DebrisTint=new(.55f,.6f,.66f),LevelTint=new(1.4f,1.25f,.6f),HitFlash=new(1.25f,1.25f,1.25f),ArmourFlash=new(.6f,.75f,.95f),HurtFlash=new(1,.3f,.22f);
        }

        public static Impact Current{get;private set;}
        /// <summary>The hero the player holds took a hit: its share of their integrity, and whether it put them down. For the screen's edge.</summary>
        public static event System.Action<float,bool> LeadHurt;
        /// <summary>Whether a packet is answered here at all: one with a weight, while the layer is on and somebody is listening.</summary>
        public static bool Answers(in DamagePacket packet)=>Current!=null&&packet.weight!=HitWeight.None&&ImpactSettings.Enabled;
        /// <summary>How many stops have begun and how much real time they have held, since this scene began.</summary>
        public int Stops{get;private set;}
        public float Held{get;private set;}
        CameraRig rig;
        readonly Queue<(float at,float seconds)> spent=new();
        CombatActor swingOf;int swingSequence;float swingHeld,swingShaken;bool swingThumped;
        float tickShakenAt=-1;
        struct Shove{public EnemyBrain body;public Vector3 along;public float left,speed;}
        readonly List<Shove> shoves=new();
        static CombatActor Lead{get{var party=PartyController.Current;return party!=null&&party.members!=null&&party.members.Length>0?party.Active:null;}}
        static bool Flying=>ZoneController.Current!=null&&ZoneController.Current.Flight;
        CameraRig Rig{get{if(rig==null)rig=FindFirstObjectByType<CameraRig>();return rig;}}
        void Awake(){Current=this;Health.Impacted+=OnImpact;Levels.LevelRaised+=OnLevel;}
        void OnDestroy(){Health.Impacted-=OnImpact;Levels.LevelRaised-=OnLevel;PadRumble.Stop();if(Current==this)Current=null;}
        void Update()
        {
            PadRumble.Tick();
            for(int i=shoves.Count-1;i>=0;i--)
            {
                var shove=shoves[i];
                if(shove.body==null||shove.left<=0||!shove.body.Health.Alive){shoves.RemoveAt(i);continue;}
                if(GameTime.CombatHeld)continue;
                float step=Mathf.Min(shove.left,shove.speed*Time.deltaTime);if(step<=0)continue;
                // It has marked an attack since, or a wall holds it: the rest of the shove is let go, never the attack.
                if(!shove.body.Shoved(shove.along*step)){shoves.RemoveAt(i);continue;}
                shove.left-=step;shoves[i]=shove;
            }
        }
        /// <summary>Asks the clock to hold for this long from now, and says how much longer that made it hold. A stop still running is extended, not added to, and the whole is
        /// held to the budget: a mash of heavy hits can't put the game in slow motion.</summary>
        /// <param name="whatever">A boss going down or the held hero falling happens once, and is held whatever the budget has left. It still counts against what follows.</param>
        public float Hold(float seconds,bool whatever=false)
        {
            if(seconds<=0||!ImpactSettings.Enabled||!ImpactSettings.HitStop||GameTime.Paused)return 0;
            float now=Time.unscaledTime;while(spent.Count>0&&now-spent.Peek().at>Tune.BudgetWindow)spent.Dequeue();
            float used=0;foreach(var stop in spent)used+=stop.seconds;
            float running=GameTime.StopLeft,extra=Mathf.Min(seconds,GameTime.MostStop)-running;
            if(!whatever)extra=Mathf.Min(extra,Tune.BudgetWindow*Tune.BudgetShare-used);
            if(extra<Tune.LeastStop)return 0;
            spent.Enqueue((now,extra));GameTime.Stop(running+extra);Held+=extra;if(running<=0)Stops++;
            return extra;
        }
        static float Slowed(float stop)=>GameTime.Flashing?stop*Tune.FlashStops:stop;
        void Land(in Beat beat,Vector3 along,bool whatever=false)
        {
            float held=Hold(Slowed(beat.stop),whatever);
            if(beat.shake>0&&Rig!=null)Rig.Impulse(along,beat.shake,beat.ring);
            PadRumble.Pulse(beat.low,beat.high,Mathf.Max(held,beat.stop)+Tune.RumbleTail);
        }
        static Vector3 Centre(Health body,out float radius)
        {
            if(body.TryGetComponent<CapsuleCollider>(out var capsule))
            {
                radius=Mathf.Clamp(capsule.radius*Mathf.Max(capsule.transform.lossyScale.x,capsule.transform.lossyScale.z),.25f,1.6f);
                return capsule.bounds.center;
            }
            radius=.4f;return body.transform.position+Vector3.up*(Flying?0:.9f);
        }
        void OnImpact(Health victim,DamagePacket packet,float damage,HitKind kind,bool broke,bool killed)
        {
            if(victim==null||!Answers(packet))return;
            var centre=Centre(victim,out float radius);
            // Along the blow: from whoever dealt it, flat. A blow from nobody, or from right on top of the body, comes at its front.
            var along=packet.source!=null?centre-packet.source.transform.position:-victim.transform.forward;along.y=0;
            along=along.sqrMagnitude>.0001f?along.normalized:-victim.transform.forward;
            // The side it landed on, not the middle of the chest.
            var point=centre-along*(radius*.8f);
            if(victim.friendly)Hurt(victim,damage,killed,point,centre,along);else Dealt(victim,packet,damage,kind,broke,killed,point,centre,along);
        }
        void Dealt(Health victim,DamagePacket packet,float damage,HitKind kind,bool broke,bool killed,Vector3 point,Vector3 centre,Vector3 along)
        {
            int weight=(int)packet.weight;bool resisted=kind==HitKind.Resisted,flying=Flying;
            var hero=packet.source!=null?packet.source.GetComponent<CombatActor>():null;bool held=hero!=null&&hero==Lead;
            // A collar's link and a pressure organ have integrity and no brain.
            victim.TryGetComponent<EnemyBrain>(out var brain);
            bool boss=brain!=null&&brain.definition!=null&&brain.definition.boss;
            var lift=flying?Vector3.zero:Vector3.up*.3f;
            if(damage>0)
            {
                var tint=CombatVfx.Tint(kind);
                // Armour throws it back the way it came; a body lets it through.
                if(resisted)CombatVfx.Spark(point,lift-along,tint,.12f,Tune.SparkCount[weight]/2+2,Tune.SparkSpeed[weight],40,.16f);
                else CombatVfx.Spark(point,along+lift,tint,Tune.SparkSize[weight],Tune.SparkCount[weight],Tune.SparkSpeed[weight]);
                if(packet.isCrit&&!resisted)
                {CombatVfx.Burst(point,Tune.CritTint,"flash");CombatVfx.Spark(point,along+lift,Tune.CritTint,.18f,10,Tune.SparkSpeed[weight]*1.4f,55,.2f);}
                // A hull sheds a little of itself, which drifts where the shot left it.
                if(flying&&!resisted&&packet.weight>=HitWeight.Light)CombatVfx.Spark(point,along,Tune.DebrisTint,.16f,5,3.5f,70,.55f);
                bool shoved=!killed&&!resisted&&brain!=null&&Push(brain,along,Tune.Knockback[weight]);
                if(victim.TryGetComponent<HitFlash>(out var body))
                    body.Struck(packet.weight,along,resisted?Tune.ArmourFlash:Tune.HitFlash,!killed&&!shoved,boss?Tune.BossPose:1,broke?Tune.BreakSquash:0);
            }
            if(broke)
            {
                CombatVfx.Burst(centre,Tune.BreakTint,"crack");CombatVfx.Burst(centre,Tune.BreakTint,"flash");
                SkillRing.Expand(flying?centre:victim.transform.position,2.2f,GroundRing.Friendly,.32f,flying);
                // Anyone's break is news: the body is open for three seconds. The held hero's is louder, and leans on the music.
                CombatAudio.Accent("break",2,victim.transform,centre,held?Tune.BreakVolume:Tune.BreakVolume*.7f,1,held?48:110);
            }
            // A mine bursts by itself.
            if(killed&&(brain==null||!brain.Mine))CombatVfx.Spark(centre,along+Vector3.up*(flying?0:1.2f),Tune.DebrisTint,.2f,14,7,60,.6f,flying?0:2.4f);
            if(!held)
            {
                // A boss going down is the whole party's beat, whoever struck last.
                if(killed&&boss&&hero!=null){Land(Tune.BossKill,along,true);CombatAudio.Accent("boss_down",1,null,centre,Tune.BossDownVolume,1,40);}
                return;
            }

            var beat=Tune.Dealt[weight];float stop=beat.stop,shake=beat.shake,ring=beat.ring,low=beat.low,high=beat.high;
            if(resisted){stop=packet.weight>=HitWeight.Light?Tune.ResistedStop*.001f:0;shake=0;}
            else
            {
                if(packet.isCrit){stop+=Tune.CritStop*.001f;shake*=Tune.CritShake;}
                if(kind==HitKind.WeakSpot&&packet.weight>=HitWeight.Light)stop+=Tune.WeakSpotStop*.001f;
            }
            bool last=killed&&!boss&&!EnemyBrain.AwakeNear(centre,Tune.LastKillRange);
            if(broke)Most(Tune.Break,ref stop,ref shake,ref ring,ref low,ref high);
            if(killed)Most(boss?Tune.BossKill:last?Tune.LastKill:Tune.Kill,ref stop,ref shake,ref ring,ref low,ref high);
            stop=Slowed(stop);
            // One swing is one beat, its heaviest: a blade through three bodies holds the fight once, and a kill among them lengthens that hold rather than adding another.
            if(hero!=swingOf||hero.AttackSequence!=swingSequence){swingOf=hero;swingSequence=hero.AttackSequence;swingHeld=swingShaken=0;swingThumped=false;}
            if(stop>swingHeld)swingHeld+=Hold(stop-swingHeld+GameTime.StopLeft,killed&&boss);
            bool tick=packet.weight==HitWeight.Tick&&!broke&&!killed&&!packet.isCrit;
            if(shake>swingShaken&&(!tick||Time.unscaledTime-tickShakenAt>=Tune.TickShakeEvery)&&Rig!=null)
            {Rig.Impulse(along,shake,ring);swingShaken=shake;if(tick)tickShakenAt=Time.unscaledTime;}
            PadRumble.Pulse(low,high,stop+Tune.RumbleTail);

            if(!resisted&&packet.weight>=HitWeight.Heavy&&!swingThumped)
            {
                swingThumped=true;bool finisher=packet.weight==HitWeight.Finisher;
                CombatAudio.Accent("thump",3,victim.transform,centre,finisher?Tune.FinisherThumpVolume:Tune.ThumpVolume,finisher?Tune.FinisherThumpPitch:1);
            }
            if(packet.isCrit&&!resisted)CombatAudio.Accent("crit",2,victim.transform,centre,Tune.CritVolume);
            if(killed&&boss)CombatAudio.Accent("boss_down",1,null,centre,Tune.BossDownVolume,1,40);
            else if(killed)CombatAudio.Accent("kill",2,victim.transform,centre,Tune.KillVolume,last?Tune.LastKillPitch:1,last?48:110);
        }
        static void Most(in Beat beat,ref float stop,ref float shake,ref float ring,ref float low,ref float high)
        {stop=Mathf.Max(stop,beat.stop);if(beat.shake>shake){shake=beat.shake;ring=beat.ring;}low=Mathf.Max(low,beat.low);high=Mathf.Max(high,beat.high);}
        /// <summary>A blow carries a light body back, through its own controller so walls still hold it. Not a boss, a heavy or an anchored body, not one that's asleep, and never
        /// one that has marked an attack or is in it: what it marked is where it strikes from, and a combo that shoved it off that would cancel every attack it began.</summary>
        bool Push(EnemyBrain body,Vector3 along,float distance)
        {
            if(distance<=0||!body.Shovable)return false;
            for(int i=0;i<shoves.Count;i++)
            {
                if(shoves[i].body!=body)continue;
                var running=shoves[i];running.along=along;running.left=Mathf.Max(running.left,distance);running.speed=running.left/Tune.KnockbackSeconds;shoves[i]=running;return true;
            }
            shoves.Add(new Shove{body=body,along=along,left=distance,speed=distance/Tune.KnockbackSeconds});return true;
        }
        void Hurt(Health victim,float damage,bool killed,Vector3 point,Vector3 centre,Vector3 along)
        {
            if(damage<=0||!victim.TryGetComponent<CombatActor>(out var hero))return;
            float share=damage/Mathf.Max(1,victim.maximum);int level=share>=Tune.HurtHeavyFrom?2:share>=Tune.HurtMediumFrom?1:0;
            // A guard that held is still up; one this hit broke has just come down.
            bool guarded=hero.GuardDamageMultiplier<1,opened=hero.GuardBroken&&GameTime.Now-hero.GuardBrokenAt<.01f,flying=hero.flight;
            var lift=flying?Vector3.zero:Vector3.up*.3f;
            if(guarded)CombatVfx.Spark(point,lift-along,Tune.GuardTint,.16f,8,6,45,.16f);
            else CombatVfx.Spark(point,along+lift,CombatVfx.Hurt,.18f+.06f*level,7+4*level,5+1.5f*level);
            if(victim.TryGetComponent<HitFlash>(out var body))
                body.Struck(level==2?HitWeight.Heavy:level==1?HitWeight.Medium:HitWeight.Light,along,guarded?Tune.ArmourFlash:Tune.HurtFlash,false,1,0);
            if(!killed&&!guarded)
            {
                if(flying){var ship=hero.GetComponentInChildren<FlightShipMotion>();if(ship!=null)ship.Jolt(along,Tune.HullJolt[level],Tune.JoltSeconds);}
                else{var pose=hero.GetComponentInChildren<GeneratedAnimator>();if(pose!=null)pose.Flinch(along,Tune.Flinch[level],Tune.FlinchSeconds);}
            }
            if(hero!=Lead)return;
            Land(killed?Tune.Downed:opened?Tune.GuardBreak:guarded?Tune.GuardBlock:level==2?Tune.HurtHeavy:level==1?Tune.HurtMedium:Tune.HurtLight,along,killed);
            if(opened)CombatAudio.Accent("break",2,hero.transform,centre,Tune.BreakVolume,Tune.GuardBreakPitch);
            else if(guarded)CombatAudio.Accent("guard_block",3,hero.transform,centre,Tune.GuardBlockVolume);
            else if(flying)CombatAudio.Accent("hull_hit",3,hero.transform,centre,Tune.HullHitVolume,level==2?Tune.HurtThumpPitch:1);
            else if(level==2)CombatAudio.Accent("thump",3,hero.transform,centre,Tune.ThumpVolume,Tune.HurtThumpPitch);
            LeadHurt?.Invoke(share,killed);
        }
        /// <summary>A guard raised on the beat: the blow stops dead on it. The burst is anyone's; the held hero's holds the fight. Like any hit it answers only a blow
        /// somebody struck: a fixture's packet turned away on a perfect guard holds nothing.</summary>
        public static void FlashGuard(CombatActor hero,DamagePacket packet)
        {
            if(hero==null||!Answers(packet))return;
            CombatVfx.Burst(hero.transform.position+Vector3.up*1.2f+hero.transform.forward*.5f,Tune.FlashGuardTint,"flash");
            if(hero==Lead)Current.Land(Tune.FlashGuard,-hero.transform.forward);
        }
        /// <summary>A roll through a blow on the beat: a held breath, and then the world goes slow. Before the Flash begins, or its own stop would be one of the halved ones.</summary>
        public static void FlashMove(CombatActor hero,DamagePacket packet)
        {
            if(hero==null||hero!=Lead||!Answers(packet))return;
            var at=hero.transform.position+Vector3.up*(hero.flight?.8f:.9f);
            CombatVfx.Burst(at,Tune.FlashMoveColor,"form");
            CombatAudio.Accent("flash_move",1,hero.transform,at,Tune.FlashMoveVolume);
            Current.Land(Tune.FlashMove,Vector3.zero);
        }
        /// <summary>The tag-in strike had a sound and nothing to see unless it hit: a ring of its reach, and a body behind the sound.</summary>
        public static void SwapStrike(CombatActor hero)
        {
            if(Current==null||hero==null||!ImpactSettings.Enabled)return;
            var at=hero.transform.position+Vector3.up*(hero.flight?.8f:1);
            SkillRing.Expand(hero.flight?at:hero.transform.position,3,GroundRing.Friendly,.3f,hero.flight);
            CombatVfx.Burst(at,GroundRing.Friendly,"shot");
            CombatAudio.Accent("swap_strike",1,hero.transform,at,Tune.SwapStrikeVolume);
        }
        /// <summary>Something heavy comes down, whether or not anybody is under it: the camera feels it and the clock doesn't.</summary>
        public static void Thud(CombatActor hero,Vector3 along)
        {
            if(Current==null||hero==null||hero!=Lead||!ImpactSettings.Enabled)return;
            var beat=Tune.Thud;if(Current.Rig!=null)Current.Rig.Impulse(along,beat.shake,beat.ring);
            PadRumble.Pulse(beat.low,beat.high,.1f);
        }
        void OnLevel(MemberState member,int from,int to)
        {
            var party=PartyController.Current;if(party==null||party.members==null||!ImpactSettings.Enabled)return;
            foreach(var hero in party.members)
            {
                if(hero==null||hero.character!=member.id||!hero.Health.Alive)continue;
                var at=hero.transform.position+Vector3.up*(hero.flight?.8f:1);
                CombatVfx.Burst(at,Tune.LevelTint,"shape");
                // Not through a conversation, which holds a ring's clock: a reward's level would lie on the floor unseen until the talk ended and open then.
                if(!GameTime.CombatHeld)SkillRing.Expand(hero.flight?at:hero.transform.position,2.4f,GroundRing.Friendly,.6f,hero.flight);
                CombatAudio.Accent("level_up",1,null,at,Tune.LevelUpVolume,1,60);
            }
        }
    }
}
