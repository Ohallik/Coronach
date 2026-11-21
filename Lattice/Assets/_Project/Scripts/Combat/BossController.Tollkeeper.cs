using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The Tollkeeper, chapter 10's second boss (Last Mooring; brief, "The fights"; wiring 6.3; the room's contract is docs/maps/LastMooring.md, "What the fight needs of the room"): the
    /// Compact's 90 m anchor-fleet carrier lying across the passage north of its gate, holding the moored convoy under the guns of its cage (<see cref="TollkeeperCage"/>). Nobody strikes
    /// the carrier: <b>its bar is the three stages of its gate motor</b>, a third each. Three times over: a brace on the cage's outriggers is broken from flight (six of them, the player's
    /// choice), and the carrier runs its arm out of the gate to mend the cage, its gate motor on it; the party docks on the arm (a berth, wiring 6.1), stops one stage of the motor on foot
    /// while the partner keeps the Clamp Tenders off it (from the second stage), and launches before the arm draws in (its clock is a marked band along the deck: run out, the party is
    /// put out with a scrape and that stage's progress is lost, never the brace). From the second stage the cage <b>contracts on a clock</b> (four lamps along each bar light toward
    /// it): each outrigger sweeps a marked fan from its hinge, shorter by a broken brace (the gap), and what it passes is struck and shoved clear. The guns fire marked lanes down the
    /// lanes the dividing chains make, one, then two, then three in turn. On the arm two things reach the deck, marked on it: the motor surges, and a turret rakes across the deck. The
    /// third stage stopped is the bar's end (<see cref="Ending.ShutDown"/>): the carrier stays where it lies, the cage swings out and hangs, and the convoy is let through.
    /// <para>What a defeat and a Continue keep (D285): each brace broken and each stage stopped is kept the moment it is (<see cref="SaveSystem.KeepCheckpoint"/>); the fight wakes at the
    /// line of the stages still running, in their phase, with no shift played, and the cage stands with what was broken. A conversation holds all of it: the moves' clocks, the cage's
    /// clock and sweep, the arm and its clock (D275). Nothing it makes is solid (D289).</para>
    /// <para>In a room with no cage (the proving yards) it fights on integrity as any boss, every move aimed at the lead and an opening after each, so the shared boss tests measure it.
    /// Its length in its room is three cycles of fly, dock, work and launch, measured by a test that docks (<c>LastMooringBossTests</c>). Logs <c>TOLL_WAKES</c>, <c>TOLL_BRACE</c>,
    /// <c>TOLL_ARM_OUT</c>, <c>TOLL_DOCKED</c>, <c>TOLL_STAGE</c>, <c>TOLL_PUT_OUT</c>, <c>TOLL_GUNS</c>, <c>TOLL_CAGE</c>, <c>TOLL_CAGE_STRUCK</c>, <c>TOLL_SURGE</c>, <c>TOLL_RAKE</c>,
    /// <c>TOLL_TENDERS</c>, <c>TOLL_GATE_OPENS</c>.</para>
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit TollkeeperKit=Register(new Kit
        {
            id="Tollkeeper",title="THE TOLLKEEPER",epithet="An anchor-fleet carrier, with a cage for a gate",thresholds=new[]{2/3f,1/3f},
            choose=(boss,hero)=>boss.ChooseToll(hero),
            moves={["Guns"]=boss=>boss.TollGuns(),["Contract"]=boss=>boss.TollContract(),["Surge"]=boss=>boss.TollSurge(),["Rake"]=boss=>boss.TollRake()},
            // A stage stopped pulls the cage in: the shift promises the contraction.
            signature=phase=>phase>=2?"Contract":null,
            shift=boss=>boss.TollShifts(),shiftText="IT PULLS THE CAGE IN  ·  BRACE",
            status=boss=>boss.TollStatus(),guarded=boss=>boss.TollStatus(),
            fightGoal=(boss,label)=>boss.TollGoal(label),
            // Its lane is 116 m across and 78 deep: the bar, the panel's line and the framing are measured from its middle and reach to its corners.
            centre=boss=>boss.TollCentre,reach=boss=>boss.TollRoom?TollTune.Reach:0,
            // The carrier stays where it lies, and OrrinGateOpen follows: nothing is said over the gate.
            capsuleRadius=TollTune.Radius,occlusion=false,ending=Ending.ShutDown,downBark="",
            start=boss=>boss.TollStart(),update=boss=>boss.TollUpdate(),down=boss=>boss.TollDown(),
        });
        /// <summary>The Tollkeeper's numbers, all of them (its integrity is its EnemyDef's: LastMooringBuilder.Tollkeeper). Arrays run by stages stopped, 0 to 2 (in a yard, its phase less
        /// one). Every blow is authored at 186 to 190 and its phases add nothing to them (a stage stopped brings more and quicker, never heavier): 12 to 16% of any hero's bar at Sync 28 in
        /// the Choirwrought parts chapter 10 arrives in: the band between Idra at 29 (12.3%) and Taren or Sela at 28 (15.2%) is 182 to 199 (LastMooringBossTests).</summary>
        public static class TollTune
        {
            /// <summary>Its capsule in a yard (8 m tall: its EnemyDef's). In its room it has none: it lies under the plane north of the gate and nobody strikes it.</summary>
            public const float Radius=4,Height=8;
            /// <summary>The cage: an outrigger's bar (its length from the hinge, its middle's depth under the plane), its braces (how far along, the coupling drawn and struck, how much
            /// short of a broken one the bar still sweeps), the clock's lamps, a brace's integrity, the motor's integrity a stage, what a blow does with a Clamp Tender at the motor.</summary>
            public const float Length=40,BarBelow=-1.8f,BraceDrawn=2.4f,BraceTarget=1.8f,BraceGap=3;
            public static readonly float[] BraceAt={16,26,36};
            public const int Lamps=4,Stages=3;
            /// <summary>Sized on the fight played through the replay driver's pad at its door (MooringBossReplayTests): at 9,500 and 11,000 a brace went in about 13 s of the pair's fire and a
            /// stage in about 4 s on foot, the whole fight in 101 s against its sheet's 120 to 300, and the arm's clock never mattered.</summary>
            public const float BraceIntegrity=19000,MotorIntegrity=22000,Tended=.25f;
            /// <summary>The fall: the bars swing out past lying open to this angle, pitched down, dropped, over these seconds; the view holds the gate this long.</summary>
            public const float HangAngle=100,HangPitch=32,HangDrop=2.5f,HangSeconds=3.2f,FrameSeconds=6;
            /// <summary>The arm: its clock on the deck by stage, its last clock once the stage is stopped, how long it takes to run out or draw in, how long after drawing in it runs out
            /// again for a dock still owed.</summary>
            public static readonly float[] ArmClock={26,22,18};
            public const float ArmLast=5,ArmSeconds=3.2f,ArmAgain=4;
            /// <summary>The cage's clock by stage (none before the first stage is stopped), its tell, where the bars stop (degrees off the gate line), the sweep, the hold, the way back, the
            /// blow and the shove.</summary>
            public static readonly float[] CageEvery={0,15,12},CageTell={2.2f,2f,1.8f};
            /// <summary>Where the bars lie at rest (degrees off the gate line, toward the lanes): a little in from lying straight down the lanes, so the cage closes toward the arm in
            /// every frame that shows the carrier, where at 90 they lay along the frame's edges.</summary>
            public const float CageLies=70;
            public const float CageShut=30,CageSweep=.9f,CageHold=.8f,CageBack=2.2f,CageDamage=186,CageShove=4,CageRest=1.6f;
            /// <summary>The guns: lanes a volley, each one's tell, the rest after; a lane's width, the step between lanes landing, the blow, how long the shot shows; in a yard a lane's length
            /// and the gap between two side by side.</summary>
            public static readonly int[] GunLanes={1,2,3};
            public static readonly float[] GunTell={1.4f,1.3f,1.2f},GunRest={2.6f,2.2f,1.9f};
            public const float GunWidth=7,GunStep=.45f,GunDamage=190,GunShows=.3f,YardLane=40,YardLaneGap=11;
            /// <summary>On the deck: the motor's surge (its ring, the blow), a turret's rake across the deck (its width, its reach, the blow), each one's tell, the rest after.</summary>
            public static readonly float[] SurgeTell={1.3f,1.2f,1.1f},RakeTell={1.2f,1.1f,1f},DeckRest={3,2.6f,2.2f};
            public const float SurgeRadius=3.4f,SurgeDamage=188,RakeWidth=3,RakeReach=16,RakeDamage=188;
            /// <summary>The Clamp Tenders that step out beside the motor as the party docks, by stage, and how far to each side.</summary>
            public static readonly int[] Tenders={0,1,2};
            public const float TenderBeside=2.6f;
            /// <summary>A scrape as the arm draws in with the party still on it, never the last of a hero's bar.</summary>
            public const float ScrapeDamage=150;
            /// <summary>In a yard: the opening after every move, and its rest.</summary>
            public const float Opening=2.2f,Rest=2.2f;
            /// <summary>Its fight measured from the lane's middle (this far down from the gate), reaching this much past an asker's range; its root, in its room, this far down from the
            /// gate's middle and this far under the plane: what the ground view frames on the deck and where its shift's ring lies, clear of the deck.</summary>
            public const float CentreDown=40,Reach=36,RootDown=4,RootBelow=-3;
            /// <summary>Each brace broken (1 to 6, which) and each stage stopped (1, 2: the third is its fall) is a flag kept the moment it is (D285).</summary>
            public static string BraceFlag(int index)=>"end.toll.brace"+(index+1);
            public static string StageFlag(int stage)=>"end.motor"+stage;
        }
        TollkeeperCage tollCage;bool tollHears,tollWasDocked,tollFinal,tollDrawIn,tollPuttingOut;
        /// <summary>What the cage is is said once a session, not again at every retry.</summary>
        static bool tollNoticed;
        int tollStages,tollOpeningSaid=-1;float tollCageClock,tollFinalFrom,tollArmAgainAt;Telegraph tollBand;
        readonly List<Telegraph> tollOne=new(1);
        List<Telegraph> TollOnly(Telegraph marking){tollOne.Clear();tollOne.Add(marking);return tollOne;}
        /// <summary>Its room has its cage in it: nobody strikes it and its bar is its motor's stages. With none (a yard) it fights on integrity.</summary>
        public bool TollRoom=>tollCage!=null;
        public TollkeeperCage TollCage=>tollCage;
        /// <summary>How far on it is: stages stopped in its room, its phase less one in a yard.</summary>
        public int TollStage=>Mathf.Clamp(TollRoom?tollStages:Phase-1,0,2);
        public int TollStagesStopped=>tollStages;
        /// <summary>A brace is broken whose stage isn't stopped: the arm is owed a dock.</summary>
        public bool TollOwed=>TollRoom&&tollCage.Broken>tollStages;
        public bool TollArmOut=>TollRoom&&tollCage.ArmOut>=.999f;
        public bool TollFinal=>tollFinal;
        public Telegraph TollBand=>tollBand;
        Vector3 TollCentre=>TollRoom?tollCage.Middle+tollCage.South*TollTune.CentreDown:transform.position;
        static float TollDt=>GameTime.CombatHeld?0:Time.deltaTime;
        IBerth TollBerth=>TollRoom?tollCage.Berth:null;
        /// <summary>The party stands on the arm (on foot, the change done).</summary>
        public bool TollDocked{get{var berth=TollBerth;var party=PartyController.Current;return berth!=null&&ZoneController.Current!=null&&ZoneController.Current.BerthOn==berth&&party!=null&&party.Active!=null&&!party.Active.flight;}}
        float TollLine(int stagesLeft)=>health.maximum*stagesLeft/TollTune.Stages;

        void TollStart()
        {
            brain.Rooted=true;tollCage=TollkeeperCage.Current;
            if(tollCage==null&&ZoneController.FindBerth("Arm")!=null)tollCage=TollMakeCage();
            engaged=Time.realtimeSinceStartup;
            if(TollRoom)
            {
                tollCage.Build();tollCage.Restore();var flags=GameServices.Current.Flags;
                tollStages=0;for(int s=1;s<TollTune.Stages;s++)if(flags.GetBool(TollTune.StageFlag(s)))tollStages=s;
                // Nobody strikes the carrier: its bar is its motor's stages, and what was stopped stays stopped (D285): it wakes at their line, in their phase, unshifted.
                health.Damageable=false;health.Holds=()=>tollStages<TollTune.Stages?TollLine(TollTune.Stages-tollStages-1)+1:0;
                if(tollStages>0){health.integrity=Mathf.Max(1,TollLine(TollTune.Stages-tollStages)-1);HealthPhase=Phase=tollStages+1;}
                TollRootAtTheGate();
                tollCage.Motor.Damaged+=TollMotorStruck;tollCage.Motor.Died+=TollMotorStopped;tollCage.BraceBroken+=TollBraceBroken;tollHears=true;
                // The arm draws in as it wakes (the zone builds it out), unless a dock is owed (a Continue with a brace broken and its stage still running): then it is out.
                if(TollOwed)tollCage.SetArm(1);tollCage.ShowMotor(true);
                if(!tollNoticed){tollNoticed=true;Notices.Post("THE TOLLKEEPER'S GATE","Its cage is held by braces on its two outriggers. Break one from your ship and the carrier runs its arm out of the gate to mend it, with its gate motor on the arm: dock on the arm, stop that stage of the motor on foot, and launch before the arm draws in. Three stages and the gate opens. From the second, the cage closes on a clock: its lamps count to it.");}
            }
            Debug.Log($"TOLL_WAKES room={TollRoom} stages={tollStages} braces={(TollRoom?tollCage.Broken:0)}");
        }
        /// <summary>Its room as the zone would stand it, if the zone hasn't: from where the carrier lies (its spawner), the arm's berth and its root (docs/maps/LastMooring.md, as built).</summary>
        TollkeeperCage TollMakeCage()
        {
            var at=transform.position;var go=new GameObject("The Tollkeeper's cage (stood by its fight)");var cage=go.AddComponent<TollkeeperCage>();
            float plane=ZoneController.Current!=null?ZoneController.Current.definition.flightPlane:1;
            cage.hingeWest=new Vector3(at.x-40,plane,at.z-24);cage.hingeEast=new Vector3(at.x+40,plane,at.z-24);cage.carrier=at;
            var berth=ZoneController.FindBerth("Arm") as Component;
            if(berth!=null&&berth.transform.parent!=null){cage.arm=berth.transform.parent;cage.armOut=cage.arm.position;cage.armIn=cage.armOut+new Vector3(0,-6,36);}
            cage.outriggerPrefab=brain.definition!=null?brain.definition.bodySegment:null;
            Debug.Log("TOLL_CAGE_STOOD_BY_ITS_FIGHT");return cage;
        }
        /// <summary>In its room its root goes to the gate (down the lanes a little, under the plane, clear of the deck), and its body stays drawn where it lies, its middle on the
        /// carrier's: the bar and the panel are measured from the lane's middle (<see cref="Kit.centre"/>), the ground view frames its root from the deck, its shift's ring lies off the deck.
        /// Its capsule and its weak spots go: nothing strikes it, and nothing of it stops a shot meant for a brace.</summary>
        void TollRootAtTheGate()
        {
            TollkeeperCage.LayCarrier(visual,tollCage.carrier);
            Vector3 keep=visual!=null?visual.position:default;Quaternion turn=visual!=null?visual.rotation:Quaternion.identity;
            var controller=GetComponent<CharacterController>();if(controller!=null)controller.enabled=false;
            transform.position=tollCage.Middle+tollCage.South*TollTune.RootDown+Vector3.up*TollTune.RootBelow;
            if(visual!=null){visual.SetPositionAndRotation(keep,turn);visualRest=visual.localPosition;visualRestRotation=visual.localRotation;}
            foreach(var shape in GetComponentsInChildren<Collider>())if(shape!=null&&shape.GetComponent<Hurtbox>()!=null&&shape.GetComponent<Hurtbox>().owner==health)shape.enabled=false;
            Physics.SyncTransforms();
        }
        void TollUpdate()
        {
            brain.Rooted=true;brain.DamageScale=1;
            if(!TollRoom)return;
            health.Damageable=false;
            // A conversation holds it: its next move waits as long as the talk did (its moves' own clocks are the fight's, GameTime.CombatHeld).
            if(GameServices.Current.Input.Blocked)nextSpecial+=Time.deltaTime;
            var party=PartyController.Current;if(party==null||party.Active==null)return;
            bool fighting=!brain.Passive;float dt=TollDt;int k=TollStage;
            // The braces: struck only from flight, and only while no dock is owed.
            tollCage.OpenBraces(fighting&&!TollOwed&&party.Active.flight&&!GameServices.Current.Input.Blocked);
            // The cage's clock, from the first stage stopped: its lamps light toward the contraction.
            if(fighting&&k>=1){tollCage.Clock(Mathf.Clamp01(tollCageClock/TollTune.CageEvery[k]));if(CurrentMove!="Contract")tollCageClock=Mathf.Min(TollTune.CageEvery[k],tollCageClock+dt);}
            // The arm: out while a dock is owed, in once its work is done or its clock ran out (and out again a beat later for a dock still owed).
            bool docked=TollDocked;
            bool wantOut=fighting&&TollOwed&&!tollDrawIn&&Time.time>=tollArmAgainAt;
            // The arm is out by one measure, the one its bar's words read (TollArmOut): its line, bark and sound waited for the whole of the way while the words said "THE ARM IS
            // OUT" from .999 of it, a frame or two sooner (and an arm held short of the whole, as a berth with the party on it is, would never have said it at all).
            bool wasOut=TollArmOut;
            if(dt>0)tollCage.DriveArm(wantOut?1:0,dt/TollTune.ArmSeconds);
            if(tollDrawIn&&tollCage.ArmOut<=0&&!docked){tollDrawIn=false;tollArmAgainAt=Time.time+TollTune.ArmAgain;}
            tollCage.ShowMotor(tollCage.ArmOut>0&&tollStages<TollTune.Stages,docked&&!tollFinal);
            if(!wasOut&&TollArmOut&&TollOwed)TollArmIsOut();
            // On the arm: the tenders, its clock, and the scrape as it draws in.
            if(docked)
            {
                var berth=TollBerth;
                if(!tollWasDocked){tollWasDocked=true;TollDocks(berth);}
                tollCage.Kept.wanted=!tollFinal&&tollCage.Motor.Alive;
                float clock=tollFinal?TollTune.ArmLast:TollTune.ArmClock[k],on=berth.DockedSeconds-(tollFinal?tollFinalFrom:0);
                if(on>=clock&&!tollPuttingOut)TollPutOut(berth);
            }
            else if(tollWasDocked)TollLeaves();
            TollBeyond();
        }
        /// <summary>The arm is out with a stage still running: once a stage, the party's line for it.</summary>
        void TollArmIsOut()
        {
            Debug.Log($"TOLL_ARM_OUT stage={tollStages+1}");
            AudioManager.PlayAt("district_lift",null,tollCage.armOut,.3f,AudioBus.SFX,40,.1f,1.1f);
            var party=PartyController.Current;
            if(tollOpeningSaid<tollStages&&party!=null&&party.Active!=null){tollOpeningSaid=tollStages;BarkService.Play(party.Active.character,Situation("bossopening"),true);}
        }
        /// <summary>Docked: the arm's clock is marked along its deck, and from the second stage the tenders step out beside the motor.</summary>
        void TollDocks(IBerth berth)
        {
            int k=TollStage;tollPuttingOut=false;
            TollLayBand(tollFinal?TollTune.ArmLast-(berth.DockedSeconds-tollFinalFrom):TollTune.ArmClock[k]-berth.DockedSeconds);
            if(!tollFinal)TollTenders(k);
            Debug.Log($"TOLL_DOCKED stage={tollStages+1} clock={TollTune.ArmClock[k]}");
        }
        /// <summary>The arm's clock: a band along its deck from its south edge to the motor, filling as it runs. Filled, the arm draws in.</summary>
        void TollLayBand(float seconds)
        {
            if(tollBand!=null&&!tollBand.Released)tollBand.Cancel();
            var arm=tollCage.arm!=null?tollCage.arm.position:tollCage.armOut;var north=-tollCage.South;
            var lead=PartyController.Current.Active.transform.position;var origin=arm-north*6.2f;origin.y=lead.y;
            // The arm's, not a move of the boss's: owned by the cage, so the partner still picks her ground on the deck and nobody rolls from a clock (both pass over a boss's own
            // markings), and the view isn't drawn to its edge.
            tollBand=Telegraph.Line(origin,north,12.4f,6.6f,Mathf.Max(.5f,seconds),Telegraph.Threat);tollBand.owner=tollCage;
        }
        void TollLeaves()
        {
            tollWasDocked=false;tollCage.Kept.wanted=false;
            if(tollBand!=null&&!tollBand.Released)tollBand.Cancel();tollBand=null;
            DismissSummons();
            // Its work done (or put out), the arm draws in; launched with the stage still running, it stays out for another try.
            if(tollFinal||tollPuttingOut)tollDrawIn=true;
            tollFinal=false;tollPuttingOut=false;
        }
        /// <summary>The clock has run out with the party on the arm: what is on the deck is scraped (marked by the band all along), and the party is put out; the stage's progress is lost.</summary>
        void TollPutOut(IBerth berth)
        {
            tollPuttingOut=true;
            if(tollBand!=null){TollStrike(tollBand,TollTune.ScrapeDamage,DamageType.Kinetic,.2f,0,true,true);tollBand=null;}
            if(!tollFinal&&tollCage.Motor.Alive)tollCage.Motor.Heal(tollCage.Motor.maximum);
            TollBarFromMotor();Tremor(.5f,.08f);
            AudioManager.PlayAt("thrusterFire_000",null,tollCage.armOut,.3f,AudioBus.SFX,40,.1f,.6f);
            berth.Launch(tollFinal?"drawn in":"drawn in, the stage still running");
            Debug.Log("TOLL_PUT_OUT "+(tollFinal?"done":"lost"));
        }
        /// <summary>Past 30 m of its root (its lane is wider than the shared controller looks) it chooses for itself the same way.</summary>
        void TollBeyond()
        {
            if(brain.Passive||busy||health.Broken||GameServices.Current.Input.Blocked||PartyController.Current==null)return;
            var hero=PartyController.Current.Active;if((hero.transform.position-transform.position).sqrMagnitude<=900)return;
            if(Flat(hero.transform.position-TollCentre).magnitude>TollTune.Reach+34)return;
            if(shiftPending){shiftPending=false;Begin("PhaseShift",PhaseShift());return;}
            if(Time.time<nextSpecial)return;
            if(queuedMove!=null){var promised=queuedMove;queuedMove=null;if(Perform(promised))return;}
            var (move,routine)=ChooseToll(hero);if(routine!=null)Begin(move,routine);
        }
        void TollBraceBroken(TollkeeperCage.Brace brace)
        {
            if(this==null){return;}
            if(health==null||!health.Alive)return;
            int count=tollCage.Broken;var flags=GameServices.Current.Flags;
            flags.SetBool(TollTune.BraceFlag(brace.index),true);if(count<=TollTune.Stages)flags.SetBool("end.brace"+count,true);
            // Kept the moment it breaks (wiring 6.1 rule 3, D285): a defeat or a Continue never mends it.
            SaveSystem.KeepCheckpoint();
            Debug.Log($"TOLL_BRACE {brace.index+1} broken={count}");
            var party=PartyController.Current;if(party!=null&&party.Active!=null)BarkService.PlayAt(party.Active.character,"tollbrace",Mathf.Clamp(count-1,0,2),true);
            Tremor(.35f,.07f);
        }
        void TollMotorStruck(Health _,DamagePacket packet,float amount){if(amount>0&&engaged<0)engaged=Time.realtimeSinceStartup;TollBarFromMotor();}
        /// <summary>The bar is the stages: those stopped gone, the one running as much of its third as the motor has left.</summary>
        void TollBarFromMotor()
        {
            if(!TollRoom||health==null||!health.Alive||tollStages>=TollTune.Stages)return;
            float top=TollLine(TollTune.Stages-tollStages),floor=TollLine(TollTune.Stages-tollStages-1),share=tollCage.Motor.maximum>0?tollCage.Motor.integrity/tollCage.Motor.maximum:1;
            health.integrity=Mathf.Clamp(floor+(top-floor)*share,floor+1,top);
        }
        void TollMotorStopped(Health _,DamagePacket packet)
        {
            if(this==null||health==null||!health.Alive)return;
            tollStages++;Debug.Log($"TOLL_STAGE {tollStages} of {TollTune.Stages}");
            CombatVfx.Burst(tollCage.Motor.transform.position+Vector3.up,new Color(1.5f,.7f,.3f),"kill");AudioManager.PlayAt("joint_release",null,tollCage.Motor.transform.position,.38f,AudioBus.SFX,30,.05f,.8f);
            DismissSummons();
            if(tollStages>=TollTune.Stages)
            {
                // The third: the motor dies, and with it the gate. Its fall is this blow, with its striker.
                StopAllCoroutines();Interrupt();busy=false;
                health.Holds=null;health.Plate=null;health.InvulnerableUntil=0;health.ShieldUntil=0;health.Damageable=true;
                health.Receive(new DamagePacket{source=packet.source,amount=health.integrity*10+1,type=health.weakness,weight=HitWeight.Heavy,tag="motor"});
                return;
            }
            GameServices.Current.Flags.SetBool(TollTune.StageFlag(tollStages),true);
            // Kept the moment it stops (D285).
            SaveSystem.KeepCheckpoint();
            // Over its line (the shift, "it pulls the cage in", follows), the motor's next stage waits for the next dock, and the arm's last clock runs: launch.
            health.integrity=Mathf.Max(1,TollLine(TollTune.Stages-tollStages)-1);
            tollCage.Motor.Heal(tollCage.Motor.maximum);tollCage.Kept.wanted=false;tollCage.Kept.Clear();
            var berth=TollBerth;tollFinal=true;tollFinalFrom=berth!=null?berth.DockedSeconds:0;TollLayBand(TollTune.ArmLast);
            tollCageClock=0;
        }
        void TollShifts()=>Debug.Log($"TOLL_SHIFT stage={TollStage}");
        void TollDown()
        {
            if(tollHears&&tollCage!=null&&tollCage.Motor!=null){tollCage.Motor.Damaged-=TollMotorStruck;tollCage.Motor.Died-=TollMotorStopped;tollCage.BraceBroken-=TollBraceBroken;tollHears=false;}
            var presentation=GetComponent<DefeatPresentation>();if(presentation!=null)presentation.enabled=false;
            if(visual!=null){visual.localPosition=visualRest;visual.localRotation=visualRestRotation;}
            if(!TollRoom)return;
            GameServices.Current.Flags.SetBool("end.motorStopped",true);
            if(tollBand!=null&&!tollBand.Released)tollBand.Cancel();tollBand=null;
            // The motor dies and the gate with it: the party is put off the arm, the cage swings out and hangs, and the convoy is let through (the zone's hulls, from bossdown).
            var berth=TollBerth;if(berth!=null&&berth.Occupied)berth.Launch("the motor dies");
            tollCage.Release();
            Debug.Log("TOLL_GATE_OPENS");
        }
        string TollStatus()
        {
            if(health==null||!health.Alive||!TollRoom)return "";
            if(TollDocked)return tollFinal?"THE ARM IS DRAWING IN  ·  LAUNCH":tollCage.Kept.Tending?"A CLAMP TENDER HOLDS THE MOTOR  ·  STOP IT":"THE ARM IS DOCKED  ·  STOP THE GATE MOTOR";
            if(CurrentMove=="Contract")return "THE CAGE IS CLOSING  ·  WEAVE OUT";
            if(TollOwed)return TollArmOut?"THE ARM IS OUT  ·  DOCK ON IT":"THE ARM IS COMING OUT  ·  WAIT FOR IT";
            return "THE CAGE IS BRACED  ·  BREAK A CAGE BRACE";
        }
        string TollGoal(string label)
        {
            if(health==null||!health.Alive||!TollRoom)return label;
            if(TollDocked)return tollFinal?"Launch before the arm draws in":"Stop the gate motor before the arm draws in";
            if(CurrentMove=="Contract")return "Weave out before the cage closes";
            if(TollOwed)return "Dock on the arm while it is out";
            return $"Break a brace on the cage ({Mathf.Min(tollStages+1,TollTune.Stages)}/{TollTune.Stages})";
        }

        // ---------------------------------------------------------------- choosing
        (string,IEnumerator) ChooseToll(CombatActor hero)
        {
            if(!TollRoom)
            {
                string pick=Pick(("Guns",1.4f),("Contract",Phase>=2?1.2f:0),("Surge",1),("Rake",1));
                return pick switch{"Contract"=>(pick,TollContract()),"Surge"=>(pick,TollSurge()),"Rake"=>(pick,TollRake()),_=>(pick,TollGuns())};
            }
            int k=TollStage;
            if(k>=1&&tollCageClock>=TollTune.CageEvery[k])return ("Contract",TollContract());
            if(!hero.flight)
            {
                // On foot anywhere but the arm, nothing of its reaches him.
                if(!TollDocked)return ("",null);
                string deck=Pick(("Surge",1.2f),("Rake",1.2f));
                return deck=="Surge"?(deck,TollSurge()):(deck,TollRake());
            }
            return ("Guns",TollGuns());
        }

        // ---------------------------------------------------------------- shared pieces of its moves
        DamagePacket TollPacket(float amount,DamageType type,bool anyForm)
        {
            var packet=Packet(amount,type);if(anyForm)packet.tag=Health.AnyForm;return packet;
        }
        /// <summary>Its marking lands, as <see cref="Strike"/>, with a packet that reaches a hero on the deck when the move is painted onto it (<see cref="Health.AnyForm"/>), and that never takes
        /// the last of a hero's bar when it is a scrape.</summary>
        void TollStrike(Telegraph marking,float amount,DamageType type,float stagger,float shove,bool anyForm,bool scrape=false,HashSet<Health> once=null)
        {
            marking.Release();
            foreach(var actor in PartyController.Current.members)
            {
                if(!actor.Health.Alive||!marking.Contains(actor.transform.position))continue;
                if(once!=null&&!once.Add(actor.Health))continue;
                float give=scrape?Mathf.Min(amount,Mathf.Max(0,actor.Health.integrity-1)/1.4f):amount;if(give<=0)continue;
                float dealt=actor.Health.Receive(TollPacket(give,type,anyForm));
                if(dealt<=0)continue;
                if(stagger>0)actor.Stagger(stagger);
                if(shove>0)StartCoroutine(Shove(actor,Flat(actor.transform.position-marking.Origin),shove));
            }
        }
        /// <summary>Where its markings lie for a hero: the floor he stands on, else the plane.</summary>
        float TollFloor{get{var lead=PartyController.Current.Active;return lead.transform.position.y;}}
        void TollRests(float rest)
        {
            strikeAt=0;
            if(!TollRoom){OpenFor(TollTune.Opening);StartCoroutine(TollOpening(TollTune.Opening,rest));return;}
            Finish(rest);
        }
        IEnumerator TollOpening(float seconds,float rest)
        {
            for(float t=0;t<seconds;){if(!health.Alive)yield break;if(GameTime.CombatHeld)health.Expose(Mathf.Max(.05f,seconds-t));else t+=Time.deltaTime;yield return null;}
            openingUntil=0;Finish(rest);
        }

        // ---------------------------------------------------------------- the moves
        /// <summary>Its guns: marked lanes down the lanes the chains make, the lead's first (on him, inside his lane), then the others in turn; in a yard, lanes from it at the lead.</summary>
        IEnumerator TollGuns()
        {
            int k=TollStage;int count=TollTune.GunLanes[k];float tell=TollTune.GunTell[k];
            var lead=PartyController.Current.Active.transform.position;var lanes=new List<(Telegraph mark,float lands,Vector3 from,Vector3 to)>();
            if(TollRoom)
            {
                float off=tollCage.OffsetOf(lead),half=TollTune.GunWidth*.5f;var (from,to)=tollCage.LaneAt(off);
                var aims=new List<float>{Mathf.Clamp(off,from+half,to-half)};
                // The other lanes, nearest his first: each down the middle of its own.
                var others=new List<(float,float)>{(-tollCage.outer,tollCage.dividers[0])};for(int i=0;i+1<tollCage.dividers.Length;i++)others.Add((tollCage.dividers[i],tollCage.dividers[i+1]));others.Add((tollCage.dividers[tollCage.dividers.Length-1],tollCage.outer));
                others.RemoveAll(l=>Mathf.Approximately(l.Item1,from)&&Mathf.Approximately(l.Item2,to));others.Sort((a,b)=>Mathf.Abs((a.Item1+a.Item2)*.5f-off).CompareTo(Mathf.Abs((b.Item1+b.Item2)*.5f-off)));
                foreach(var (a,b) in others)aims.Add((a+b)*.5f);
                for(int i=0;i<Mathf.Min(count,aims.Count);i++)
                {
                    var origin=tollCage.Middle+tollCage.Across*aims[i]+tollCage.South*1.5f;origin.y=tollCage.Plane;float lands=tell+i*TollTune.GunStep;
                    lanes.Add((Mark(Telegraph.Line(origin,tollCage.South,tollCage.lanesRun,TollTune.GunWidth,lands,Telegraph.Heavy)),lands,origin,origin+tollCage.South*tollCage.lanesRun));
                }
            }
            else
            {
                var way=Toward(lead);var across=Vector3.Cross(Vector3.up,way);var start=transform.position;start.y=lead.y;
                for(int i=0;i<count;i++)
                {
                    float side=i==0?0:(i%2==1?1:-1)*TollTune.YardLaneGap;var origin=start+across*side;float lands=tell+i*TollTune.GunStep;
                    lanes.Add((Mark(Telegraph.Line(origin,way,TollTune.YardLane,TollTune.GunWidth,lands,Telegraph.Heavy)),lands,origin,origin+way*TollTune.YardLane));
                }
            }
            Debug.Log($"TOLL_GUNS lanes={lanes.Count} stage={k}");
            AudioManager.PlayAt("forceField_000",transform,lanes[0].from,.24f,AudioBus.SFX,50,.05f,.6f);
            var landed=new bool[lanes.Count];var once=new HashSet<Health>();var marks=new List<Telegraph>();foreach(var l in lanes)marks.Add(l.mark);
            float last=lanes[lanes.Count-1].lands;
            for(float t=0;;)
            {
                t+=TollDt;if(Interrupted)yield break;
                for(int i=0;i<lanes.Count;i++)
                {
                    if(landed[i]||t<lanes[i].lands)continue;landed[i]=true;
                    Strike(lanes[i].mark,TollTune.GunDamage,DamageType.Beam,.25f,0,once,true);
                    TollShot(lanes[i].from,lanes[i].to);
                }
                ThreatFrom(marks);
                if(t>=last)break;
                yield return null;
            }
            for(float t=0;t<TollTune.GunShows;){t+=TollDt;yield return null;}
            TollRests(TollTune.GunRest[k]);
        }
        /// <summary>A gun's shot down its lane, seen for a moment, with its report at the gate.</summary>
        void TollShot(Vector3 from,Vector3 to)
        {
            var go=new GameObject("The Tollkeeper's shot");temporary.Add(go);var line=go.AddComponent<LineRenderer>();line.positionCount=2;line.useWorldSpace=true;
            line.SetPosition(0,from+Vector3.up*.4f);line.SetPosition(1,to+Vector3.up*.4f);line.startWidth=1.2f;line.endWidth=.6f;line.sharedMaterial=ActorFactory.Energy(new Color(1.6f,.55f,.25f,.95f));
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            CombatVfx.Burst(from+Vector3.up,new Color(1.6f,.7f,.3f),"kill");AudioManager.PlayAt("laserSmall_000",null,from,.28f,AudioBus.SFX,50,.05f,.55f);
            StartCoroutine(Gone());
            IEnumerator Gone(){for(float t=0;t<TollTune.GunShows;){t+=TollDt;yield return null;}temporary.Remove(go);if(go!=null)Destroy(go);}
        }
        /// <summary>The cage contracts: each outrigger sweeps a marked fan from its hinge in toward the gate (shorter by a broken brace), striking and shoving clear what it passes, holds,
        /// and swings back. Seen from the deck it moves with nothing marked, since nothing of it reaches there. In a yard, one fan from it at the lead.</summary>
        IEnumerator TollContract()
        {
            int k=TollStage;float tell=TollTune.CageTell[k];tollCageClock=0;var party=PartyController.Current;
            if(!TollRoom)
            {
                var way=Toward(party.Active.transform.position);var origin=transform.position;origin.y=party.Active.transform.position.y;
                var fan=Mark(Telegraph.Cone(origin,way,36,TollTune.CageLies-TollTune.CageShut,tell,Telegraph.Heavy));
                for(float t=0;t<tell;){t+=TollDt;if(Interrupted)yield break;ThreatFrom(TollOnly(fan));yield return null;}
                Strike(fan,TollTune.CageDamage,DamageType.Kinetic,.3f,TollTune.CageShove);strikeAt=0;Tremor(.4f,.08f);
                AudioManager.PlayAt("explosionCrunch_000",transform,origin+way*12,.28f,AudioBus.SFX,50,.05f,.6f);
                TollRests(TollTune.Rest);yield break;
            }
            bool marked=party.Active.flight;var fans=new List<(TollkeeperCage.Rigger rigger,Telegraph mark,float reach)>();
            foreach(var rigger in tollCage.riggers)
            {
                float reach=tollCage.Reach(rigger);if(!marked||reach<2)continue;
                var mid=tollCage.Along(rigger,(TollTune.CageLies+TollTune.CageShut)*.5f);var hinge=rigger.hinge;hinge.y=tollCage.Plane;
                fans.Add((rigger,Mark(Telegraph.Cone(hinge,mid,reach,TollTune.CageLies-TollTune.CageShut,tell,Telegraph.Heavy)),reach));
            }
            Debug.Log($"TOLL_CAGE stage={k} marked={fans.Count} reach={tollCage.Reach(tollCage.riggers[0]):0}/{tollCage.Reach(tollCage.riggers[1]):0}");
            AudioManager.PlayAt("district_lift",null,tollCage.Middle,.34f,AudioBus.SFX,40,.1f,.75f);tollCage.Clock(1);
            var struck=new HashSet<Health>();
            float AngleOf(TollkeeperCage.Rigger rigger,Vector3 point){var o=point-rigger.hinge;o.y=0;var across=rigger.west?tollCage.Across:-tollCage.Across;return Mathf.Atan2(Vector3.Dot(o,tollCage.South),Vector3.Dot(o,across))*Mathf.Rad2Deg;}
            // The tell: the bars stir in place, the fans fill. When the bar reaches the lead is what the companion and the driver read.
            for(float t=0;t<tell;)
            {
                t+=TollDt;if(Interrupted)yield break;
                foreach(var rigger in tollCage.riggers)rigger.angle=TollTune.CageLies-2.5f*Mathf.Sin(t*14)*Mathf.Clamp01(t/tell);tollCage.Pose();
                float soonest=float.PositiveInfinity;var lead=party.Active.transform.position;
                foreach(var (rigger,mark,reach) in fans)if(mark.Contains(lead)){float a=Mathf.Clamp(AngleOf(rigger,lead),TollTune.CageShut,TollTune.CageLies);soonest=Mathf.Min(soonest,tell-t+TollTune.CageSweep*(TollTune.CageLies-a)/(TollTune.CageLies-TollTune.CageShut));}
                strikeAt=float.IsPositiveInfinity(soonest)?0:Time.time+soonest;
                yield return null;
            }
            foreach(var (_,mark,_) in fans)mark.Steady=true;
            // The sweep: what the bar passes inside its fan is struck once and shoved clear.
            for(float t=0;t<TollTune.CageSweep;)
            {
                t+=TollDt;float angle=Mathf.Lerp(TollTune.CageLies,TollTune.CageShut,Mathf.SmoothStep(0,1,Mathf.Clamp01(t/TollTune.CageSweep)));
                foreach(var rigger in tollCage.riggers)rigger.angle=angle;tollCage.Pose();
                foreach(var (rigger,mark,reach) in fans)
                    foreach(var actor in party.members)
                    {
                        if(!actor.Health.Alive||struck.Contains(actor.Health)||!mark.Contains(actor.transform.position))continue;
                        if(AngleOf(rigger,actor.transform.position)<angle)continue;
                        struck.Add(actor.Health);
                        if(actor.Health.Receive(Packet(TollTune.CageDamage,DamageType.Kinetic))>0)
                        {
                            actor.Stagger(.3f);var away=Flat(actor.transform.position-rigger.hinge);StartCoroutine(Shove(actor,away,TollTune.CageShove));
                            Debug.Log("TOLL_CAGE_STRUCK "+actor.character);
                        }
                    }
                float soonest=float.PositiveInfinity;var lead=party.Active.transform.position;
                if(!struck.Contains(party.Active.Health))foreach(var (rigger,mark,reach) in fans)if(mark.Contains(lead)){float a=Mathf.Clamp(AngleOf(rigger,lead),TollTune.CageShut,TollTune.CageLies);if(a<angle)soonest=Mathf.Min(soonest,TollTune.CageSweep*Mathf.Max(0,angle-a)/(TollTune.CageLies-TollTune.CageShut));}
                strikeAt=float.IsPositiveInfinity(soonest)?0:Time.time+soonest;
                yield return null;
            }
            foreach(var (_,mark,_) in fans)mark.Release();strikeAt=0;Tremor(.5f,.1f);
            AudioManager.PlayAt("explosionCrunch_000",null,tollCage.Middle,.32f,AudioBus.SFX,40,.05f,.5f);
            for(float t=0;t<TollTune.CageHold;){t+=TollDt;yield return null;}
            for(float t=0;t<TollTune.CageBack;){t+=TollDt;float angle=Mathf.Lerp(TollTune.CageShut,TollTune.CageLies,Mathf.SmoothStep(0,1,Mathf.Clamp01(t/TollTune.CageBack)));foreach(var rigger in tollCage.riggers)rigger.angle=angle;tollCage.Pose();yield return null;}
            tollCage.Pose(TollTune.CageLies);tollCageClock=0;tollCage.Clock(0);
            TollRests(TollTune.CageRest);
        }
        /// <summary>On the deck: the motor surges, a marked ring round its seat (in a yard, round the lead). It reaches a hero on foot on the arm (it is painted there).</summary>
        IEnumerator TollSurge()
        {
            int k=TollStage;float tell=TollTune.SurgeTell[k];var lead=PartyController.Current.Active.transform.position;
            var centre=TollRoom&&tollCage.seat!=null?tollCage.seat.position:lead;centre.y=TollFloor;
            var ring=Mark(Telegraph.Circle(centre,TollTune.SurgeRadius,tell,Telegraph.Heavy));
            Debug.Log($"TOLL_SURGE stage={k}");AudioManager.PlayAt("forceField_000",null,centre,.26f,AudioBus.SFX,50,.05f,1.3f);
            for(float t=0;t<tell;){t+=TollDt;if(Interrupted)yield break;ThreatFrom(TollOnly(ring));if(Time.frameCount%5==0)CombatVfx.Burst(centre+Vector3.up*1.2f,new Color(1.5f,.6f,.25f,.6f),"steam");yield return null;}
            TollStrike(ring,TollTune.SurgeDamage,DamageType.Pulse,.3f,1.6f,true);strikeAt=0;
            CombatVfx.Burst(centre+Vector3.up,new Color(1.6f,.7f,.3f),"kill");AudioManager.PlayAt("explosionCrunch_000",null,centre,.26f,AudioBus.SFX,50,.05f,1.2f);
            TollRests(TollTune.DeckRest[k]);
        }
        /// <summary>On the deck: a turret on the carrier rakes across the deck through the lead, a marked lane from rail to rail (in a yard, across him). It reaches a hero on foot.</summary>
        IEnumerator TollRake()
        {
            int k=TollStage;float tell=TollTune.RakeTell[k];var lead=PartyController.Current.Active.transform.position;
            var across=TollRoom?tollCage.Across:Vector3.Cross(Vector3.up,Toward(lead));
            var middle=lead;if(TollRoom&&tollCage.arm!=null){var deck=tollCage.arm.position;middle=deck+Vector3.Project(Flat(lead-deck),-tollCage.South);}
            var origin=middle-across*(TollTune.RakeReach*.5f);origin.y=TollFloor;
            var lane=Mark(Telegraph.Line(origin,across,TollTune.RakeReach,TollTune.RakeWidth,tell,Telegraph.Heavy));
            Debug.Log($"TOLL_RAKE stage={k}");AudioManager.PlayAt("laserSmall_000",null,middle,.24f,AudioBus.SFX,50,.05f,.7f);
            for(float t=0;t<tell;){t+=TollDt;if(Interrupted)yield break;ThreatFrom(TollOnly(lane));yield return null;}
            TollStrike(lane,TollTune.RakeDamage,DamageType.Beam,.25f,0,true);strikeAt=0;
            TollShot(origin,origin+across*TollTune.RakeReach);
            TollRests(TollTune.DeckRest[k]);
        }
        /// <summary>From the second stage, Clamp Tenders step out beside the motor as the party docks, on a pale ring: one works it (a blow on it takes a quarter), and the partner's
        /// job is to stop them.</summary>
        void TollTenders(int k)
        {
            int n=TollTune.Tenders[k];if(n<=0||tollCage.seat==null)return;
            var definition=GameCatalog.Find<EnemyDef>("StillwaterTender");if(definition==null)return;
            summoned.RemoveAll(s=>s==null||!s.Health.Alive);if(summoned.Count>0)return;
            for(int i=0;i<n;i++)
            {
                var spot=tollCage.seat.position+tollCage.seat.right*((i%2==0?1:-1)*TollTune.TenderBeside)+tollCage.seat.forward*1.2f;
                var point=CombatCover.Ground(spot+Vector3.up*2);
                var ring=Mark(Telegraph.Circle(point,1.1f,1.2f,MotherTenderTone));ring.Release();
                var tender=ActorFactory.Enemy(definition,point+Vector3.up*.05f);summoned.Add(tender);
                CombatVfx.Burst(point+Vector3.up*.6f,new Color(1.2f,.9f,.5f),"kill");
            }
            Debug.Log($"TOLL_TENDERS {n} stage={k+1}");
        }
    }
}
