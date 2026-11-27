using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Meret Venn, phase two: the frame gantry (chapter 10, the last boss; brief "The fights" 2; wiring 6.5; the room is docs/maps/StillwaterChamber.md, "Boss arena contract: the gantry"). On
    /// foot on the Continuance's moving exterior, in her heavy engineering frame (<c>MeretGantry</c>, 3.2 m) with the ship's maintenance arms fitted to it: a welding arm on her right, a
    /// press on her left, and the gantry's coolant sluices under her hand. <b>The frame that out-builds you</b>: every machine the party has beaten, built properly and run by someone who
    /// understands it, and she names each as she uses it.
    /// <list type="bullet">
    /// <item><b>Slam</b>: the Saint's press, down a marked strip at the lead (twice from her first coupling open); a slam that meets nobody leaves the press arm <b>overreached</b>, planted on the
    /// deck. <b>Weld</b>: the Works' welding line, a marked lane through the lead that a front runs down and leaves burning; it always ends with the welding arm at full stretch, overreached.
    /// <b>Coolant</b>: the Foundry's sluices, marked strips of the deck flooded through the lead's, the dry floor between them (from her first coupling open a second flood takes the dry strips
    /// as the first lands), and what floods stays slick. <b>Charge</b> (an arm gone): the frame runs down a marked lane and ends overextended, open. <b>Stomp</b>: a marked ring round her for
    /// anyone at her feet.</item>
    /// <item><b>Two arm couplings</b> (<see cref="MeretCoupling"/>, one at each shoulder, drawn by <see cref="MeretFrame"/>): her bar is paid in blows and <b>held at each third</b> until a
    /// coupling opens. While it is held, an arm overreached offers its coupling (its seat lights beside her) and the party can take its weight and ease it, Oriel's release; opened, the arm
    /// hangs dead, she fights without it and the bar goes through its line, nothing more (D281): two lines, two couplings, every third in blows. While both arms bear it her frame turns half
    /// of a blow outside her openings; while overreached she is open, so the partner strikes while the lead takes the weight.</item>
    /// </list>
    /// Her blows never grow with her phase. At nought she takes the lift down to the well (<see cref="Ending.Leaves"/>, its <see cref="BossExit"/> sinking): she is not beaten down, and the
    /// zone's <c>MeretWell</c> conversation waits at the bottom. Logs <c>MERET_GANTRY_WAKES</c>, <c>MERET_SLAM</c>, <c>MERET_WELD</c>, <c>MERET_COOLANT</c>, <c>MERET_CHARGE</c>,
    /// <c>MERET_STOMP</c>, <c>MERET_OVERREACHED &lt;arm&gt;</c>, <c>MERET_ARM_HANGS &lt;arm&gt;</c>, and <see cref="MeretCoupling"/>'s.
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit MeretGantryKit=Register(new Kit
        {
            id="MeretGantry",title="DIRECTOR MERET VENN",epithet="Every machine you have beaten, built properly",thresholds=new[]{2/3f,1/3f},
            choose=(boss,hero)=>boss.ChooseGantry(hero),
            moves={["Slam"]=boss=>boss.GantrySlam(),["Weld"]=boss=>boss.GantryWeld(),["Coolant"]=boss=>boss.GantryCoolant(),["Charge"]=boss=>boss.GantryCharge(),["Stomp"]=boss=>boss.GantryStomp()},
            signature=phase=>phase==2?"Coolant":phase>=3?"Charge":null,
            shift=boss=>boss.GantryShifts(),shiftText="SHE IS DOWN AN ARM  ·  BRACE",
            status=boss=>boss.GantryStatus(),guarded=boss=>boss.GantryStatus(),
            fightGoal=(boss,label)=>boss.GantryGoal(label),
            // She takes the lift, and MeretWell waits below: nothing is said over her going.
            capsuleRadius=GantryTune.Radius,occlusion=false,ending=Ending.Leaves,downBark="",
            // The frame's power pack on its back and its two knees, a hand proud of its 1.3 m capsule: each reached by a blade from its edge.
            weakSpots=new[]{new Vector3(0,1.8f,-1.5f),new Vector3(-1.5f,1f,.2f),new Vector3(1.5f,1f,.2f)},
            start=boss=>boss.GantryStart(),update=boss=>boss.GantryUpdate(),down=boss=>boss.GantryDown(),
        });
        /// <summary>Phase two's numbers, all of them (her integrity is her EnemyDef's: LastMooringBuilder.Meret). Every blow is authored at 192 to 198 and her phases add nothing to it: 12 to 16%
        /// of any hero's bar at Sync 29 and 30 in Choirwrought (LastMooringBossTests).</summary>
        public static class GantryTune
        {
            /// <summary>Her capsule (3.2 m tall: her definition's), and where each coupling's seat stands from her (out to her side, a little in front).</summary>
            public const float Radius=1.3f,SeatOut=2.1f,SeatAhead=.5f;
            /// <summary>What of a blow reaches the frame while its arms bear it (until both couplings are open): it is the Saint's press built properly, and lands whole and half again only in its
            /// openings (an arm overreached, the charge overextended, a break). Played through the pad the pair struck it at .47 of their flat out and it fell in 73 s against a floor of 80.</summary>
            public const float FramePlate=.5f;
            /// <summary>Slam: the strip's start out from her, its length and width, the tell by phase, the second slam's, the blow, the shove, and how long a slam on nobody leaves the press overreached.</summary>
            public static readonly float[] SlamTell={1.1f,.95f,.85f};
            public const float SlamFrom=1,SlamLength=7.4f,SlamWidth=2.6f,SlamNext=.6f,SlamDamage=196,SlamShove=2.2f,Overreach=3.8f;
            /// <summary>Weld: where it starts from her (her right side), its line, the tell by phase, the front's pace, the blow, the seam it leaves (how long, how wide, its burn a second), and how
            /// long the welding arm stays at full stretch after.</summary>
            public static readonly float[] WeldTell={1.2f,1.05f,.95f};
            public const float WeldSide=1.1f,WeldAhead=1.2f,WeldLength=16,WeldWidth=2.2f,WeldSpeed=18,WeldDamage=194,SeamSeconds=2.5f,SeamWidth=1.4f,SeamTick=30,WeldStretch=3.4f;
            /// <summary>Coolant: each flooded strip's width, the spacing (a strip and a dry strip as wide), how far behind her they start and how far they run, the strips each side of the lead's,
            /// the tell by phase, the second flood's tell, the blow, and the slick it leaves (how long, the pace on it).</summary>
            public static readonly float[] CoolantTell={1.5f,1.4f,1.3f};
            public const float StripWidth=4.5f,StripSpacing=9,StripBehind=3,StripLength=28,SecondFloodTell=.9f,CoolantDamage=192,SlickSeconds=3.5f,SlickPace=.55f;
            public const int StripsEachSide=1;
            /// <summary>Charge: the lane's shortest and longest and its width, the tell by phase, the pace, what it strikes in passing, the blow, how long it leaves her overextended by phase.</summary>
            public static readonly float[] ChargeTell={1f,.9f,.8f},Overextended={2.6f,2.4f,2.2f};
            public const float ChargeShortest=6,ChargeLongest=14,ChargeWidth=2.8f,ChargeSpeed=16,ChargeContact=1.6f,ChargeDamage=196;
            /// <summary>Stomp: the ring round her, the tell, the blow, the shove; who is at her feet.</summary>
            public const float StompRadius=4.6f,StompTell=.9f,StompDamage=192,StompShove=2.6f,AtHerFeet=4.2f;
            /// <summary>Her rest between moves by phase, and how long a hero's line waits after hers.</summary>
            public static readonly float[] Rest={2.6f,2.3f,2f};
            public const float RadioAfter=2.2f;
        }
        static readonly Color GantrySpark=new(1.5f,.9f,.45f,.9f),GantryDust=new(.55f,.52f,.48f),Coolant=new(.55f,1.1f,1.4f,.9f);
        MeretCoupling gantryPress,gantryWelder;MeretFrame gantryFrame;bool gantryCut,gantryHears,gantryHushedShift;int gantryOpeningSaid=-1;
        readonly bool[] gantryNamed=new bool[3];
        /// <summary>A test's switch for the frame as it was told (a turned blow showed only as grey numbers): never set in a scene.</summary>
        public static bool GantryPlateSilent;
        bool gantryPlateSaid;float gantryGlanceAt;
        /// <summary>Her frame turns this blow now: her arms brace it (fewer than two couplings open), she fights, and it isn't open.</summary>
        bool GantryBraced=>!brain.Passive&&GantryCouplingsOpen<2&&!health.Broken;
        /// <summary>A test's switch for the old fault (a coupling offered at any overreach, and opened it took her bar down to its line from wherever it stood): never set in a scene.</summary>
        [System.NonSerialized] public bool gantryCouplingTakes;
        /// <summary>A test's switch for the arm as it was drawn in (its stretch run out, it was pulled back from under a hero holding its coupling, the weight dropped a breath before it
        /// opened): never set in a scene.</summary>
        public static bool GantryDrawsInHeld;
        /// <summary>A test's switch for her choices as they were while her bar stood held at a line (weighted, a rest as likely the first time as any other, and any number of rests
        /// running): never set in a scene.</summary>
        public static bool GantryHeldRestsAsItWas;
        /// <summary>Held at a line, her next choice has to be a reach (a slam or a weld): true at the hold's first choice and after each rest.</summary>
        bool gantryMustReach=true;
        /// <summary>Her couplings open so far (of two), and whether each arm still answers.</summary>
        public int GantryCouplingsOpen=>(gantryPress!=null&&gantryPress.IsOpen?1:0)+(gantryWelder!=null&&gantryWelder.IsOpen?1:0);
        public bool GantryPressOn=>gantryPress!=null&&!gantryPress.IsOpen;
        public bool GantryWelderOn=>gantryWelder!=null&&!gantryWelder.IsOpen;
        public MeretCoupling GantryPress=>gantryPress;
        public MeretCoupling GantryWelder=>gantryWelder;
        public MeretFrame GantryFrame=>gantryFrame;
        int GantryStage=>Mathf.Clamp(Phase,1,3)-1;
        static float GantryDt=>GameTime.CombatHeld?0:Time.deltaTime;
        /// <summary>Her bar stops at each third until a coupling has opened for it: over two thirds, one; over a third, both.</summary>
        float GantryHoldFloor
        {
            get
            {
                // Measured in integrity, with half a point's grace: a bar standing exactly on its line is held there, not let through to the next.
                if(health==null)return 0;float bar=health.integrity,most=health.maximum;int open=GantryCouplingsOpen;
                if(bar>=most*2/3f-.5f&&open<1)return most*2/3f;
                if(bar>=most/3f-.5f&&open<2)return most/3f;
                return 0;
            }
        }

        void GantryStart()
        {
            // Her two couplings, a seat each beside her shoulders on the deck, shut until an arm overreaches.
            MeretCoupling Coupling(string arm,float side)
            {
                var go=new GameObject(arm=="press"?"Press coupling seat":"Welder coupling seat");go.transform.SetParent(transform,false);
                go.transform.localPosition=new Vector3(side*GantryTune.SeatOut,0,GantryTune.SeatAhead);
                var coupling=go.AddComponent<MeretCoupling>();coupling.arm=arm;return coupling;
            }
            // The model's welding torch is on her right arm and the press on her left (docs/maps/StillwaterChamber.md; the model sheet's front view mirrors them).
            gantryPress=Coupling("press",-1);gantryWelder=Coupling("welder",1);
            MeretCoupling.Opened+=GantryCouplingOpened;health.Damaged+=GantryStruck;gantryHears=true;
            gantryFrame=gameObject.AddComponent<MeretFrame>();gantryFrame.Bind(visual!=null?visual.GetComponentInChildren<Animator>():GetComponentInChildren<Animator>());
            health.Holds=()=>GantryHoldFloor;
            // Only while she fights: the shared length test measures flat out on a passive body.
            health.Plate=_=>!brain.Passive&&GantryCouplingsOpen<2?GantryTune.FramePlate:1;
            var leaving=gameObject.AddComponent<BossDeparture>();leaving.sound="district_lift";leaving.speed=4;leaving.Duration=4.2f;leaving.reel=.9f;
            Debug.Log($"MERET_GANTRY_WAKES rigged={gantryFrame.Rigged}");
        }
        void GantryCouplingOpened(MeretCoupling coupling)
        {
            if(this==null){MeretCoupling.Opened-=GantryCouplingOpened;return;}
            if(health==null||!health.Alive||(coupling!=gantryPress&&coupling!=gantryWelder))return;
            bool right=coupling==gantryWelder;int open=GantryCouplingsOpen;
            gantryFrame.Set(right,MeretFrame.Arm.Hanging);gantryCut=true;
            Debug.Log($"MERET_ARM_HANGS {coupling.arm} open={open}");
            Tremor(.4f,.09f);
            // A bar held at its line goes through it, and the shift (she is down an arm) follows. A bar still over its line stays where the blows left it: a coupling chooses, it never
            // takes (D281). It took the bar down to its line from wherever it stood, and played through the pad both opened at her first two overreaches: two thirds of her bar paid with no
            // blow, and the gantry over in 61 s against a floor of 80.
            float line=health.maximum*(1-open/3f);if(health.integrity>line-1&&(gantryCouplingTakes||health.integrity<=line+1))health.integrity=Mathf.Max(1,line-1);
        }
        void GantryShifts()
        {
            Debug.Log("MERET_GANTRY_SHIFT "+HealthPhase);
        }
        void GantryUpdate()
        {
            brain.DamageScale=1;
            // The party's line is for the first coupling ("She's down an arm"); at the second the hold's own words are enough.
            bool hush=shiftPending&&HealthPhase>2&&!brain.Passive&&!busy&&!health.Broken&&!GameServices.Current.Input.Blocked;
            if(hush&&!gantryHushedShift){gantryHushedShift=true;BarkService.Hush(this,true);}
            else if(gantryHushedShift&&!shiftPending&&!shifting){gantryHushedShift=false;BarkService.Hush(this,false);}
            // An arm drawn back in with its coupling still shut stands raised again; one that's open hangs.
            if(!busy)
            {
                gantryFrame.Set(false,GantryPressOn?MeretFrame.Arm.Raised:MeretFrame.Arm.Hanging);
                gantryFrame.Set(true,GantryWelderOn?MeretFrame.Arm.Raised:MeretFrame.Arm.Hanging);
            }
        }
        void GantryDown()
        {
            if(gantryHears){MeretCoupling.Opened-=GantryCouplingOpened;health.Damaged-=GantryStruck;gantryHears=false;}
            BarkService.Hush(this,false);
            health.Holds=null;health.Plate=null;
            if(gantryPress!=null)gantryPress.Retire();if(gantryWelder!=null)gantryWelder.Retire();
            if(gantryFrame!=null)gantryFrame.Dark();
            Debug.Log("MERET_GANTRY_TAKES_THE_LIFT");
        }
        string GantryStatus()
        {
            if(health==null||!health.Alive||gantryPress==null)return "";
            if((gantryPress.Offered||gantryWelder.Offered)&&!brain.Passive)return "AN ARM IS OVERREACHED  ·  TAKE ITS COLLAR";
            if(!brain.Passive&&GantryHoldFloor>0&&health.integrity<=GantryHoldFloor+1)return "HER ARMS HOLD  ·  OPEN A COUPLING";
            if(GantryCouplingsOpen>=2)return "BOTH COUPLINGS OPEN  ·  STRIKE THE FRAME";
            if(GantryPlateSilent||brain.Passive)return "";
            // Open (an arm overreached with no collar wanted, the frame run out at the end of its charge, a break): every blow lands, and half again.
            if(health.Broken)return "THE FRAME IS OPEN  ·  STRIKE";
            return "HER ARMS BRACE THE FRAME  ·  STRIKE WHEN IT OPENS";
        }
        /// <summary>A blow on the braced frame: half of it turns (GantryTune.FramePlate). It glances with a spark and a ring of metal where it lands, and the first one says why on screen and in
        /// the lead's mouth, as the Slag Colossus's glowing armour does: the grey numbers alone said nothing to a player about what opens her.</summary>
        void GantryStruck(Health _,DamagePacket packet,float amount)
        {
            if(packet.source==null||!packet.source.friendly||amount<=0||packet.tag==Health.CutsPlate||!GantryBraced||GantryPlateSilent)return;
            if(Time.time>=gantryGlanceAt)
            {
                gantryGlanceAt=Time.time+.3f;var toward=Flat(packet.source.transform.position-transform.position);float gap=toward.magnitude;
                var at=transform.position+(gap>.01f?toward/gap:transform.forward)*GantryTune.Radius+Vector3.up*1.5f;
                CombatVfx.Burst(at,GantrySpark,"spark");AudioManager.PlayAt("impactMetal_000",transform,at,.16f,AudioBus.SFX,40,.06f,1.5f);
            }
            if(gantryPlateSaid)return;gantryPlateSaid=true;Debug.Log("MERET_FRAME_TURNS");
            Notices.Post("HER FRAME TURNS A BLOW","While her arms brace it, her frame turns half of every blow. It opens when an arm overreaches (a slam that meets nobody, the end of a weld) and when it runs out at the end of its charge: strike it then, and every blow lands and half again. With both couplings open it takes every blow.");
            var party=PartyController.Current;if(party!=null)BarkService.Play(party.Active.character,Situation("bossbraced"),true);
        }
        string GantryGoal(string label)
        {
            if(health==null||!health.Alive||gantryPress==null)return label;
            int open=GantryCouplingsOpen;
            return open>=2?label:$"Open the two arm couplings on her frame ({open}/2)";
        }
        /// <summary>Her name for the machine, once each, and the hero's a beat after.</summary>
        void GantryNames(int machine)
        {
            if(machine<0||machine>2||gantryNamed[machine]||brain.Passive)return;gantryNamed[machine]=true;
            BarkService.PlayAt("Meret","gantry",machine,true);
            var party=PartyController.Current;if(party==null)return;string lead=party.Active.character;party.StartCoroutine(Later());
            IEnumerator Later(){yield return new WaitForSeconds(GantryTune.RadioAfter);BarkService.PlayAt(lead,"oldmachines",machine,true);}
        }
        /// <summary>An arm at full stretch on the deck: its coupling's seat lit beside her, the frame open, for as long as it stays out. Ends early if the coupling opens.</summary>
        IEnumerator GantryOverreached(bool right,Vector3 way,float seconds)
        {
            var coupling=right?gantryWelder:gantryPress;
            if(coupling==null||coupling.IsOpen){Finish(GantryTune.Rest[GantryStage]);yield break;}
            // Its collar is offered while her bar stands held at a line: a coupling releases a held third. Offered whenever an arm overreached, both were open in the first 22 s of a fight
            // played through the pad, at 89% and 81% of her bar, and the frame stood bare for the rest of it.
            bool offer=gantryCouplingTakes||GantryHoldFloor>0&&health.integrity<=GantryHoldFloor+1;
            gantryCut=false;gantryFrame.Set(right,MeretFrame.Arm.Reaching,way,true);if(offer)coupling.Offer(true);OpenFor(seconds);
            Debug.Log("MERET_OVERREACHED "+coupling.arm);
            var party=PartyController.Current;
            if(party!=null&&gantryOpeningSaid<Phase&&!brain.Passive){gantryOpeningSaid=Phase;BarkService.Play(party.Active.character,Situation("bossopening"),true);}
            // A hero holding its coupling's weight pins the arm: she can't draw it in from under him. The weld's stretch is 3.4 s and the weight and the ease take 2, so a hero at the seat
            // 1.5 s after the arm came down held it to a breath of open and had it pulled away: twice a fight in the player's own captures (nov24-whole and nov25-whole s67), seven
            // times in one fight through the pad (meretpad1, November 26: 200 s of her 150). The hold ends by itself: it opens in two seconds, or he lets go and the arm goes in.
            for(float t=0;(t<seconds||!GantryDrawsInHeld&&coupling.Holder!=null&&!coupling.IsOpen)&&!gantryCut;)
            {
                if(!health.Alive)yield break;
                // Held by a conversation, the stretch is held with it.
                if(GameTime.CombatHeld)health.Expose(Mathf.Max(.05f,seconds-t));else t+=Time.deltaTime;
                // Pinned past its stretch she is as open as the arm is out.
                if(t>=seconds&&!GameTime.CombatHeld)OpenFor(.1f);
                yield return null;
            }
            coupling.Offer(false);gantryFrame.Set(right,coupling.IsOpen?MeretFrame.Arm.Hanging:MeretFrame.Arm.Raised);
            openingUntil=0;Pose(0);Finish(gantryCut?.8f:GantryTune.Rest[GantryStage]);
        }

        // ---------------------------------------------------------------- choosing
        (string,IEnumerator) ChooseGantry(CombatActor hero)
        {
            float d=Flat(hero.transform.position-transform.position).magnitude;int phase=Mathf.Clamp(Phase,1,3);bool press=GantryPressOn,weld=GantryWelderOn;
            // Her bar held at a line, she reaches with an arm still coupled: the bar says to open a coupling, and one is offered only while an arm is overreached. Played through the pad
            // with the lead at her feet she went twenty-five seconds held without a weld (it waits on a lead three metres off) and nothing on screen said why.
            // And she reaches first, and never rests twice running: the choice was weighted (two in three for a reach with one arm left), so one hold in nine opened with two moves
            // that offer nothing. Played through the pad in suite nov26b she flooded, stomped and flooded again at her second line: 24 s under OPEN A COUPLING with none to open.
            bool held=GantryHoldFloor>0&&health.integrity<=GantryHoldFloor+1,reach=held&&(press||weld);
            string move=reach&&gantryMustReach&&!GantryHeldRestsAsItWas?Pick(("Slam",press?2.4f:0),("Weld",weld?2.4f:0)):
                reach?Pick(("Slam",press?2.4f:0),("Weld",weld?2.4f:0),("Coolant",.6f),("Stomp",d<GantryTune.AtHerFeet?.6f:0)):
                Pick(("Slam",press?(d<9?1.8f:.7f):0),("Weld",weld?(d>3?1.6f:FullOffer):0),("Coolant",phase>=2?1.4f:1f),
                ("Charge",phase>=2||!press||!weld?(d>4?1.6f:FullOffer):0),("Stomp",d<GantryTune.AtHerFeet?1.2f:0));
            gantryMustReach=!reach||move!="Slam"&&move!="Weld";
            return move switch{"Weld"=>(move,GantryWeld()),"Coolant"=>(move,GantryCoolant()),"Charge"=>(move,GantryCharge()),"Stomp"=>(move,GantryStomp()),_=>(move,GantrySlam())};
        }

        // ---------------------------------------------------------------- the moves
        /// <summary>The Saint's press, built properly: down a marked strip at the lead (twice from her first coupling open). A slam that meets nobody leaves the press planted, overreached.</summary>
        IEnumerator GantrySlam()
        {
            if(!GantryPressOn){Finish(.4f);yield break;}
            int k=GantryStage;int slams=k>=1?2:1;bool met=false;Vector3 way=transform.forward;GantryNames(0);
            for(int s=0;s<slams;s++)
            {
                way=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(way);
                var origin=transform.position+way*GantryTune.SlamFrom-Vector3.Cross(Vector3.up,way)*.5f;float length=GantryTune.SlamLength;
                if(CombatCover.Sweep(origin+Vector3.up*.6f,origin+Vector3.up*.6f+way*length,.3f,out var wall))length=Mathf.Max(1.5f,wall.distance);
                float tell=s>0?GantryTune.SlamNext:GantryTune.SlamTell[k];
                var strip=Mark(Telegraph.Line(origin,way,length,GantryTune.SlamWidth,tell,Telegraph.Heavy));
                AudioManager.PlayAt("spaceEngineLow_000",transform,transform.position+Vector3.up*2,.26f,AudioBus.SFX,50,.05f,.75f);
                gantryFrame.Set(false,MeretFrame.Arm.Raised);
                // Counted from the marking's own first frame: it was laid this frame, and counting this one put the blow a frame ahead of what the marking (and whoever reads it) says.
                yield return null;
                for(float t=0;t<tell;){t+=GantryDt;if(Interrupted)yield break;ThreatFrom(ShipOnly(strip));Pose(-6*Mathf.Clamp01(t/tell),.1f*Mathf.Clamp01(t/tell),.03f);yield return null;}
                var hit=new HashSet<Health>();Strike(strip,GantryTune.SlamDamage,DamageType.Kinetic,.35f,GantryTune.SlamShove,hit,true);met=hit.Count>0;strikeAt=0;
                gantryFrame.Set(false,MeretFrame.Arm.Reaching,way);Pose(10,-.15f);
                Tremor(.45f,.11f);AudioManager.PlayAt("explosionCrunch_000",transform,origin+way*length*.5f,.3f,AudioBus.SFX,60,.05f,.75f);
                for(int i=1;i<=3;i++)CombatVfx.Burst(origin+way*(length*i/3.5f)+Vector3.up*.3f,GantryDust,"kill");
                Debug.Log($"MERET_SLAM {s+1} met={met}");
                if(s<slams-1)for(float t=0;t<.25f;){t+=GantryDt;yield return null;}
            }
            // On nobody, the press is planted and she can't draw it in at once: overreached.
            if(!met){yield return GantryOverreached(false,way,GantryTune.Overreach);yield break;}
            for(float t=0;t<.35f;){t+=GantryDt;yield return null;}
            gantryFrame.Set(false,MeretFrame.Arm.Raised);Pose(0);Finish(GantryTune.Rest[k]);
        }
        /// <summary>The Works' welding line, built properly: a marked lane from her right side through the lead; a front runs down it, whoever it reaches is struck, and the seam burns behind it.
        /// It always ends with the welding arm at full stretch, overreached.</summary>
        IEnumerator GantryWeld()
        {
            if(!GantryWelderOn){Finish(.4f);yield break;}
            int k=GantryStage;GantryNames(1);
            var lead=PartyController.Current.Active.transform.position;var face=Toward(lead);transform.rotation=Quaternion.LookRotation(face);
            var origin=transform.position+Vector3.Cross(Vector3.up,face)*GantryTune.WeldSide+face*GantryTune.WeldAhead;var way=Flat(lead-origin);way=way.sqrMagnitude>.04f?way.normalized:face;
            float length=GantryTune.WeldLength;if(CombatCover.Sweep(origin+Vector3.up*.6f,origin+Vector3.up*.6f+way*length,.3f,out var wall))length=Mathf.Max(2,wall.distance);
            float tell=GantryTune.WeldTell[k];var lane=Mark(Telegraph.Line(origin,way,length,GantryTune.WeldWidth,tell,Telegraph.Threat));
            float AlongOf(Vector3 p)=>Vector3.Dot(Flat(p-origin),way);
            gantryFrame.Set(true,MeretFrame.Arm.Reaching,way);
            AudioManager.PlayAt("forceField_000",transform,origin+Vector3.up,.24f,AudioBus.SFX,50,.05f,.6f);
            yield return null;// from the marking's own first frame
            for(float t=0;t<tell;)
            {
                t+=GantryDt;if(Interrupted)yield break;
                var at=PartyController.Current.Active.transform.position;strikeAt=lane.Contains(at)?Time.time+(tell-t)+Mathf.Max(0,AlongOf(at))/GantryTune.WeldSpeed:0;
                if(Time.frameCount%5==0)CombatVfx.Burst(origin+Vector3.up*.3f,GantrySpark,"steam");Pose(-3*Mathf.Clamp01(t/tell));yield return null;
            }
            lane.Steady=true;var hit=new HashSet<Health>();Debug.Log("MERET_WELD");
            AudioManager.PlayAt("laserSmall_000",transform,origin+Vector3.up,.26f,AudioBus.SFX,50,.05f,.5f);
            for(float front=0;front<length+.5f;)
            {
                float dt=GantryDt;if(Interrupted)yield break;if(dt<=0){yield return null;continue;}
                front+=GantryTune.WeldSpeed*dt;
                var at=PartyController.Current.Active.transform.position;float along=AlongOf(at);strikeAt=!hit.Contains(PartyController.Current.Active.Health)&&lane.Contains(at)&&along>=front?Time.time+(along-front)/GantryTune.WeldSpeed:0;
                foreach(var member in PartyController.Current.members)
                {
                    if(!member.Health.Alive||hit.Contains(member.Health)||!lane.Contains(member.transform.position))continue;
                    float a=AlongOf(member.transform.position);if(a>front||a<front-2.5f)continue;
                    hit.Add(member.Health);if(member.Health.Receive(Packet(GantryTune.WeldDamage,DamageType.Plasma))>0)member.Stagger(.25f);
                }
                if(Time.frameCount%2==0)CombatVfx.Burst(origin+way*Mathf.Min(front,length)+Vector3.up*.25f,GantrySpark,"steam");
                yield return null;
            }
            lane.Release();strikeAt=0;
            LingeringArea.LayLine(origin,way,length,GantryTune.SeamWidth,GantryTune.SeamSeconds,AreaEffect.Burn,1,GantryTune.SeamTick,health,DamageType.Plasma);
            // The line run out to its end: the welding arm is at full stretch.
            yield return GantryOverreached(true,way,GantryTune.WeldStretch);
        }
        /// <summary>The Foundry's sluices, built properly: strips of deck flooded through the lead's and either side, the dry floor between (from her first coupling open, a second flood takes the
        /// dry strips as the first lands). What floods stays slick a while.</summary>
        IEnumerator GantryCoolant()
        {
            int k=GantryStage;GantryNames(2);bool twice=k>=1;
            var lead=PartyController.Current.Active.transform.position;var way=Toward(lead);var across=Vector3.Cross(Vector3.up,way);
            float lateral=0;// the lead's strip is centred on him
            var from=transform.position-way*GantryTune.StripBehind;from.y=transform.position.y;
            float leadAcross=Vector3.Dot(Flat(lead-from),across);
            var strips=new List<Telegraph>();var landAt=new List<float>();var landed=new List<bool>();
            float tell=GantryTune.CoolantTell[k];
            void Flood(float offset,float lands,float told)
            {
                var origin=from+across*(leadAcross+offset);
                strips.Add(Mark(Telegraph.Line(origin,way,GantryTune.StripLength,GantryTune.StripWidth,told,Telegraph.Threat)));landAt.Add(lands);landed.Add(false);
            }
            for(int i=-GantryTune.StripsEachSide;i<=GantryTune.StripsEachSide;i++)Flood(lateral+i*GantryTune.StripSpacing,tell,tell);
            Debug.Log($"MERET_COOLANT twice={twice} phase={Phase}");
            AudioManager.PlayAt("water_rush",transform,transform.position,.3f,AudioBus.SFX,50,.05f,.8f);
            bool second=twice;var first=new HashSet<Health>();var again=new HashSet<Health>();int firstCount=strips.Count;
            // Counted from the marking's own first frame: it was laid this frame, and counting this one put the blow a frame ahead of what the marking (and whoever reads it) says.
            yield return null;
            for(float t=0;;)
            {
                t+=GantryDt;if(Interrupted)yield break;
                if(second&&t>=tell)
                {
                    second=false;
                    for(int i=-GantryTune.StripsEachSide-1;i<=GantryTune.StripsEachSide;i++)Flood(lateral+(i+.5f)*GantryTune.StripSpacing,t+GantryTune.SecondFloodTell,GantryTune.SecondFloodTell);
                }
                for(int i=0;i<strips.Count;i++)
                {
                    if(landed[i]||t<landAt[i])continue;landed[i]=true;
                    Strike(strips[i],GantryTune.CoolantDamage,DamageType.Pulse,.25f,0,i<firstCount?first:again,true);
                    var s=strips[i];LingeringArea.LayLine(s.Origin,s.Forward,s.Length,s.Width,GantryTune.SlickSeconds,AreaEffect.Slow,GantryTune.SlickPace,0,health,DamageType.Pulse,LingeringArea.Wet);
                    CombatVfx.Burst(s.Origin+s.Forward*(s.Length*.5f)+Vector3.up*.3f,Coolant,"steam");
                }
                ThreatFrom(strips);Pose(-2*Mathf.Clamp01(t/tell));
                if(!second&&landed.TrueForAll(l=>l))break;
                yield return null;
            }
            strikeAt=0;Pose(0);Finish(GantryTune.Rest[k]);
        }
        /// <summary>The frame runs down a marked lane at the lead (a wall or an anchor stops it), striking whoever it meets, and ends overextended: open.</summary>
        IEnumerator GantryCharge()
        {
            int k=GantryStage;var controller=GetComponent<CharacterController>();
            var target=PartyController.Current.Active.transform.position;var way=Toward(target);transform.rotation=Quaternion.LookRotation(way);
            float length=Mathf.Clamp(Flat(target-transform.position).magnitude+3,GantryTune.ChargeShortest,GantryTune.ChargeLongest);
            if(CombatCover.Sweep(transform.position+Vector3.up,transform.position+Vector3.up+way*(length+GantryTune.Radius),.5f,out var wall))length=Mathf.Max(1.5f,wall.distance-GantryTune.Radius-.3f);
            float tell=GantryTune.ChargeTell[k];var lane=Mark(Telegraph.Line(transform.position,way,length,GantryTune.ChargeWidth,tell,Telegraph.Heavy));
            AudioManager.PlayAt("spaceEngineLow_000",transform,transform.position+Vector3.up,.26f,AudioBus.SFX,50,.05f,.6f);

            yield return null;// from the marking's own first frame
            for(float t=0;t<tell;){t+=GantryDt;if(Interrupted)yield break;var at=PartyController.Current.Active.transform.position;
                strikeAt=lane.Contains(at)?Time.time+(tell-t)+Mathf.Max(0,Vector3.Dot(Flat(at-transform.position),way)-GantryTune.ChargeContact)/GantryTune.ChargeSpeed:0;Pose(-8*Mathf.Clamp01(t/tell),0,.04f);yield return null;}
            lane.Steady=true;Debug.Log("MERET_CHARGE");
            var hit=new HashSet<Health>();float travelled=0;Collide(false);
            while(travelled<length&&!Interrupted)
            {
                float dt=GantryDt;if(dt<=0){yield return null;continue;}
                float step=Mathf.Min(GantryTune.ChargeSpeed*dt,length-travelled);var before=transform.position;if(controller.enabled)controller.Move(way*step);
                float moved=Flat(transform.position-before).magnitude;travelled+=Mathf.Max(moved,step*.5f);if(moved<step*.25f)break;
                Contact(hit,transform.position,GantryTune.ChargeContact+GantryTune.Radius*.5f,GantryTune.ChargeDamage,.4f,lane);Pose(6);
                var now=PartyController.Current.Active;float left=Vector3.Dot(Flat(now.transform.position-transform.position),way);
                strikeAt=!hit.Contains(now.Health)&&lane.Contains(now.transform.position)&&left>0?Time.time+Mathf.Max(0,left-GantryTune.ChargeContact)/GantryTune.ChargeSpeed:0;
                yield return null;
            }
            Collide(true);lane.Release();strikeAt=0;
            if(Interrupted)yield break;
            // All of the frame behind it: overextended, met or not.
            Pose(12,-.2f);var party=PartyController.Current;
            if(party!=null&&gantryOpeningSaid<Phase&&!brain.Passive){gantryOpeningSaid=Phase;BarkService.Play(party.Active.character,Situation("bossopening"),true);}
            yield return Opening(GantryTune.Overextended[k],GantryTune.Rest[k]);Pose(0);
        }
        /// <summary>Anyone at her feet: a marked ring round her, then the frame stamps and shoves them off.</summary>
        IEnumerator GantryStomp()
        {
            var ring=Mark(Telegraph.Circle(transform.position,GantryTune.StompRadius,GantryTune.StompTell,Telegraph.Heavy));
            // Counted from the marking's own first frame: it was laid this frame, and counting this one put the blow a frame ahead of what the marking (and whoever reads it) says.
            yield return null;
            for(float t=0;t<GantryTune.StompTell;){t+=GantryDt;if(Interrupted)yield break;ThreatFrom(ShipOnly(ring));Pose(0,.3f*Mathf.Clamp01(t/GantryTune.StompTell),.04f);yield return null;}
            Strike(ring,GantryTune.StompDamage,DamageType.Kinetic,.35f,GantryTune.StompShove);strikeAt=0;Debug.Log("MERET_STOMP");
            Pose(0,-.2f);Tremor(.4f,.1f);AudioManager.PlayAt("explosionCrunch_000",transform,transform.position,.3f,AudioBus.SFX,50,.05f,.7f);
            for(int i=0;i<6;i++)CombatVfx.Burst(transform.position+Quaternion.Euler(0,i*60,0)*Vector3.forward*(GantryTune.StompRadius*.7f)+Vector3.up*.3f,GantryDust,"kill");
            for(float t=0;t<.4f;){t+=GantryDt;yield return null;}
            Pose(0);Finish(GantryTune.Rest[GantryStage]);
        }
    }
}
