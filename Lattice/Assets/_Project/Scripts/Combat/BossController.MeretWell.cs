using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Meret Venn, phase three: the control well (chapter 10, the last boss; brief "The fights" 3; wiring 6.5; the room is docs/maps/StillwaterChamber.md, "Boss arena contract: the well"). A duel,
    /// on foot, in her emergency frame (<c>MeretWell</c>, a coat on her own body, 1.85 m): fast, precise and courteous, and <b>the woman who will not stop</b>. Every attack is marked and none of
    /// them is new: the Warmaster's thrust and lunge, Hessa's cut, the Cutter's mines as clamps, the Flash Guard turned on the party. She talks during it, briefly, and listens.
    /// <list type="bullet">
    /// <item><b>Her counter stance punishes mashing.</b> She raises her plate inside a pale ring and the bar says so: a blow on her turns, and the first one she answers with a riposte down a short
    /// marked fan at whoever struck (a roll clears it). Then her counter is spent and she is open. Leave her guard alone and it runs out, and she steps in with a thrust.</item>
    /// <item><b>Her clamp-traps punish standing still.</b> She throws a ring of marked clamps round the lead; and any hero who stands still too long has one closing under him (it marks first). A
    /// clamp that closes on someone holds him a moment.</item>
    /// <item>Thrust (two from her second phase), Cut (a fan before her), Lunge (a long marked lane that always ends overextended: the opening anyone can take). Her phases make her quicker,
    /// never heavier. Into her last she stops, steps back and asks her question, and listens: nothing is said over the quiet and she strikes at nobody in it.</item>
    /// </list>
    /// At nought she yields standing (<see cref="Ending.Yields"/>, <c>yieldsOnDefeat</c>): she doesn't fall, kneel or shout; her body holds its stance until the person takes over
    /// (<c>DefeatPresentation.Yielded</c>, the chamber's) and <c>MeretYields</c> (the chamber's after-talk on <c>bossdown.MeretWell</c>) is the next thing said. Logs <c>MERET_WELL_WAKES</c>,
    /// <c>MERET_GUARD</c>, <c>MERET_COUNTER &lt;hero&gt;</c>, <c>MERET_COUNTER_SPENT</c>, <c>MERET_TRAPS</c>, <c>MERET_TRAP_STILL &lt;hero&gt;</c>, <c>MERET_CLAMPED &lt;hero&gt;</c>,
    /// <c>MERET_THRUST</c>, <c>MERET_CUT</c>, <c>MERET_LUNGE</c>, <c>MERET_LISTENS</c>, <c>MERET_YIELDS_STANDING</c>.
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit MeretWellKit=Register(new Kit
        {
            id="MeretWell",title="MERET VENN",epithet="Sixty-one, in an emergency frame, with a cough",thresholds=new[]{WellTune.Second,WellTune.Third},
            choose=(boss,hero)=>boss.ChooseWell(hero),
            moves={["Thrust"]=boss=>boss.WellThrust(),["Cut"]=boss=>boss.WellCut(),["Guard"]=boss=>boss.WellGuard(),["Traps"]=boss=>boss.WellTraps(),["Lunge"]=boss=>boss.WellLunge(),
                ["Listens"]=boss=>boss.WellListens()},
            signature=phase=>phase==2?"Traps":phase>=3?"Listens":null,
            shiftText="SHE STOPS TESTING YOU  ·  BRACE",
            status=boss=>boss.WellStatus(),guarded=boss=>boss.WellStatus(),
            // She yields, and MeretYields follows from the chamber: nothing is said over her.
            capsuleRadius=WellTune.Radius,occlusion=false,ending=Ending.Yields,downBark="",
            // Her frame's power cell on her back and its two hip joints, a hand proud of her .8 m body: each reached by a blade from its edge.
            weakSpots=new[]{new Vector3(0,1.25f,-.9f),new Vector3(-.9f,1.05f,.1f),new Vector3(.9f,1.05f,.1f)},
            start=boss=>boss.WellStart(),update=boss=>boss.WellUpdate(),down=boss=>boss.WellDown(),
        });
        /// <summary>Phase three's numbers, all of them (her integrity is her EnemyDef's: LastMooringBuilder.Meret). Every blow is authored at 192 to 198 and her phases add nothing to it: 12 to 16%
        /// of any hero's bar at Sync 29 and 30 in Choirwrought (LastMooringBossTests).</summary>
        public static class WellTune
        {
            /// <summary>Her capsule (2 m tall: her definition's) and her two phase lines.</summary>
            public const float Radius=.8f,Second=.6f,Third=.3f;
            /// <summary>Thrust: the line and its width, the tell by phase, the next one's, the step into it, the blow, the shove; how many by phase.</summary>
            public static readonly float[] ThrustTell={.75f,.65f,.55f};
            public static readonly int[] Thrusts={1,2,2};
            public const float ThrustLength=8,ThrustWidth=1.5f,ThrustNext=.5f,ThrustStep=.8f,ThrustDamage=196,ThrustShove=1.5f;
            /// <summary>Cut: the fan's reach and angle, the tell by phase, the blow, the shove.</summary>
            public static readonly float[] CutTell={.65f,.55f,.5f};
            public const float CutReach=4,CutAngle=150,CutDamage=194,CutShove=2;
            /// <summary>Guard: how long her stance holds by phase, its pale ring, the riposte's fan (reach, angle, tell), its blow and stagger, and how long her spent counter leaves her open by phase.</summary>
            public static readonly float[] Stance={2.4f,2.2f,2f},Spent={2.4f,2.2f,2f};
            public const float GuardRing=2.6f,RiposteReach=4.2f,RiposteAngle=100,RiposteTell=.45f,RiposteDamage=198,RiposteStagger=.4f;
            /// <summary>Traps: how many by phase, each clamp's ring, how far round the lead the others lie, the tell by phase, the blow, how long a closed clamp holds; and the trap that closes under
            /// a hero who has stood still (within how far, for how long by phase, its tell).</summary>
            public static readonly int[] Traps={3,4,5};
            public static readonly float[] TrapTell={1.3f,1.15f,1f},StillAfter={1.8f,1.6f,1.3f};
            public const float TrapRadius=1.7f,TrapRound=3.4f,TrapDamage=192,TrapHolds=.8f,StillWithin=.8f,StillTell=1;
            /// <summary>Lunge: the lane and its width, the tell by phase, the pace, what it strikes in passing, the blow, how long it leaves her overextended by phase.</summary>
            public static readonly float[] LungeTell={.85f,.75f,.7f},Overextended={2.4f,2.2f,2f};
            public const float LungeLength=13,LungeWidth=1.8f,LungeSpeed=20,LungeContact=1.4f,LungeDamage=196;
            /// <summary>Her question: how far she steps back, how long the quiet after it lasts; and the share of her bar under which she says her last line.</summary>
            public const float ListenBack=2.5f,Quiet=4,LastWordAt=.12f;
            /// <summary>What of a blow reaches her outside her openings: she is a duellist and turns what isn't timed. In an opening (her counter spent, overextended, broken) a blow lands
            /// whole and half again. Played through the pad the pair struck her at .47 of their flat out, standing at her and swinging, and she was down in 70 s against a floor of 80.</summary>
            public const float Parry=.6f;
            /// <summary>Her rest between moves by phase.</summary>
            public static readonly float[] Rest={2.2f,1.9f,1.6f};
        }
        static readonly Color GuardBoundary=new(1f,.93f,.74f,.42f);
        bool wellGuarding,wellListening,wellHears,wellLastWord,wellGuardSaid,wellTrapSaid;CombatActor wellStruckBy;string wellOpenWhy="";
        readonly Dictionary<CombatActor,(Vector3 at,float since)> wellStill=new();
        readonly Dictionary<CombatActor,float> wellTrapUntil=new();
        readonly List<Coroutine> wellStillTraps=new();
        readonly HashSet<Telegraph> wellStillMarks=new();
        /// <summary>A clamp she has marked under a hero for standing still: a marking no move of hers counts down (the replay driver reads it off the floor, ContinuousReview.Meret.cs).</summary>
        public bool WellStillMark(Telegraph marking)=>marking!=null&&wellStillMarks.Contains(marking);
        /// <summary>As it was, for a test's broken state (MeretReplayTests sets it and puts it back; nothing in the game does): the seconds of a hero's own EXTRA counted as standing still.</summary>
        public static bool WellExtraCountsStill;
        public bool WellGuarding=>wellGuarding;
        public bool WellListening=>wellListening;
        int WellStage=>Mathf.Clamp(Phase,1,3)-1;
        static float WellDt=>GameTime.CombatHeld?0:Time.deltaTime;

        void WellStart()
        {
            // Her guard turns every blow while it is up, and while she listens; nothing else is plated. Only while she fights: held passive she is measured as she stands.
            health.Plate=packet=>brain.Passive?1:wellGuarding||wellListening?0:WellTune.Parry;
            health.Damaged+=WellStruck;wellHears=true;
            Debug.Log("MERET_WELL_WAKES");
        }
        void WellStruck(Health _,DamagePacket packet,float amount)
        {
            if(packet.source==null||!packet.source.friendly)return;
            if(engaged<0)engaged=Time.realtimeSinceStartup;
            // The first blow into her guard is the one she answers.
            if(wellGuarding&&wellStruckBy==null&&packet.source.TryGetComponent<CombatActor>(out var striker))wellStruckBy=striker;
        }
        void WellUpdate()
        {
            brain.DamageScale=1;
            // Her guard and her quiet are never broken through: a break would cut the stance (or the question) short, and the lesson with it.
            if(wellGuarding||wellListening)health.ClearBreak();
            // A conversation holds her count of who stands still with everything else (D275): it starts again once the talk ends.
            if(GameServices.Current.Input.Blocked){wellStill.Clear();return;}
            if(brain.Passive||PartyController.Current==null)return;
            // Her last word, near the end of her bar.
            if(!wellLastWord&&health.integrity<health.maximum*WellTune.LastWordAt){wellLastWord=true;BarkService.PlayAt("Meret","duel",3,true);}
            // Standing still: a clamp closes under whoever has, after a while.
            if(wellListening)return;
            float still=WellTune.StillAfter[WellStage];
            // Only the hero the player leads: a companion stands where her brain stands her, and nobody pays for what the companion does (played through the pad, Idra was clamped five
            // times in one duel standing at her blade's reach, bracing each one and standing still again). A companion in a clamp's mark is still struck by it.
            foreach(var member in PartyController.Current.members)
            {
                if(member==null||!member.Health.Alive||member.flight||member!=PartyController.Current.Active){wellStill.Remove(member);continue;}
                var at=member.transform.position;
                // His own EXTRA roots him where it goes off and nothing can be pressed through it, a roll least of all: its seconds aren't standing still, and his count starts again as it
                // ends. Played through the pad a clamp was laid under Taren inside his EXTRA and closed on him as it ended, the driver's roll pressed and refused (November 26).
                if(!WellExtraCountsStill&&member.TryGetComponent<ExtraMove>(out var extra)&&extra.Performing){wellStill[member]=(at,Time.time);continue;}
                if(!wellStill.TryGetValue(member,out var was)||Flat(at-was.at).magnitude>WellTune.StillWithin){wellStill[member]=(at,Time.time);continue;}
                if(Time.time-was.since<still||wellTrapUntil.TryGetValue(member,out float until)&&Time.time<until)continue;
                if(Flat(at-transform.position).magnitude>16)continue;
                wellTrapUntil[member]=Time.time+WellTune.StillTell+.6f;wellStill[member]=(at,Time.time);
                wellStillTraps.RemoveAll(c=>c==null);wellStillTraps.Add(StartCoroutine(WellStillTrap(member,at)));
            }
        }
        IEnumerator WellStillTrap(CombatActor hero,Vector3 at)
        {
            var mark=Mark(Telegraph.Circle(at,WellTune.TrapRadius,WellTune.StillTell,Telegraph.Heavy));wellStillMarks.Add(mark);
            Debug.Log("MERET_TRAP_STILL "+hero.character);WellSaysTraps();
            AudioManager.PlayAt("impactMetal_000",transform,at,.2f,AudioBus.SFX,40,.05f,1.3f);
            yield return null;// from the marking's own first frame
            for(float t=0;t<WellTune.StillTell;){t+=WellDt;if(!health.Alive){mark.Cancel();wellStillMarks.Remove(mark);yield break;}yield return null;}
            WellClamps(mark,null);wellStillMarks.Remove(mark);
        }
        /// <summary>A clamp closes: whoever is inside is struck and held a moment.</summary>
        void WellClamps(Telegraph mark,HashSet<Health> once)
        {
            mark.Release();var party=PartyController.Current;if(party==null)return;
            foreach(var member in party.members)
            {
                if(!member.Health.Alive||!mark.Contains(member.transform.position))continue;
                if(once!=null&&!once.Add(member.Health))continue;
                if(member.Health.Receive(Packet(WellTune.TrapDamage,DamageType.Kinetic))>0){member.Stagger(WellTune.TrapHolds);Debug.Log("MERET_CLAMPED "+member.character);}
            }
            CombatVfx.Burst(mark.Origin+Vector3.up*.3f,new Color(.85f,.9f,1f),"crack");
        }
        /// <summary>A move of hers cut short (a break): what it had set goes with it. Her guard and her listening were left up for good when a break stopped the move that raised them, and
        /// played through the pad her bar read SHE IS LISTENING for the last seventy seconds of the duel while her plate turned every blow and she went on striking. Her craft's tractor
        /// likewise, which holds her shield down while it pulls.</summary>
        void MeretInterrupted()
        {
            if(wellListening)BarkService.Hush(this,false);
            if(wellGuarding||wellListening)Debug.Log("MERET_WELL_CUT_SHORT");
            wellGuarding=wellListening=false;wellStruckBy=null;shipPulling=false;
        }
        void WellSaysTraps(){if(wellTrapSaid||brain.Passive)return;wellTrapSaid=true;BarkService.PlayAt("Meret","duel",1,true);}
        void WellDown()
        {
            if(wellHears){health.Damaged-=WellStruck;wellHears=false;}
            wellGuarding=wellListening=false;health.Plate=null;BarkService.Hush(this,false);
            // She yields standing: the rig keeps her stance, not the clip a captain gives up in.
            var rig=GetComponentInChildren<Animator>();var driver=GetComponentInChildren<GeneratedAnimator>();
            if(driver!=null)driver.enabled=false;
            if(rig!=null){rig.speed=1;if(rig.HasState(0,Animator.StringToHash("Idle")))rig.Play("Idle",0,.2f);}
            Pose(0);
            Debug.Log("MERET_YIELDS_STANDING");
        }
        string WellStatus()
        {
            if(health==null||!health.Alive||brain==null||brain.Passive)return "";
            if(wellListening)return "SHE IS LISTENING";
            if(wellGuarding)return "HER GUARD IS UP  ·  DON'T MASH";
            if(health.Broken&&Time.time<openingUntil)return wellOpenWhy+"  ·  STRIKE";
            return "";
        }

        // ---------------------------------------------------------------- choosing
        (string,IEnumerator) ChooseWell(CombatActor hero)
        {
            // Her guard is the duel's lesson (don't mash): close in, it comes half as often again as it did (1.2, 1.6), which played through the pad raised it twice in a 70 s duel.
            float d=Flat(hero.transform.position-transform.position).magnitude;int phase=Mathf.Clamp(Phase,1,3);
            string move=Pick(("Thrust",d<WellTune.ThrustLength?1.6f:.4f),("Cut",d<WellTune.CutReach+1?1.5f:0),("Guard",d<6?(phase>=3?2.2f:1.8f):.3f),
                ("Traps",1.1f),("Lunge",d>4?1.5f:FullOffer));
            return move switch{"Cut"=>(move,WellCut()),"Guard"=>(move,WellGuard()),"Traps"=>(move,WellTraps()),"Lunge"=>(move,WellLunge()),_=>(move,WellThrust())};
        }

        // ---------------------------------------------------------------- the moves
        IEnumerator WellThrust()
        {
            int k=WellStage;var controller=GetComponent<CharacterController>();
            for(int n=0;n<WellTune.Thrusts[k];n++)
            {
                var way=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(way);
                float tell=n>0?WellTune.ThrustNext:WellTune.ThrustTell[k];
                var line=Mark(Telegraph.Line(transform.position+way*(WellTune.ThrustStep*.5f),way,WellTune.ThrustLength,WellTune.ThrustWidth,tell,Telegraph.Threat));
                // Counted from the marking's own first frame: it was laid this frame, and counting this one put the blow a frame ahead of what the marking (and whoever reads it) says.
                yield return null;
                for(float t=0;t<tell;){t+=WellDt;if(Interrupted)yield break;ThreatFrom(ShipOnly(line));Pose(-7*Mathf.Clamp01(t/tell),0,.02f);yield return null;}
                for(float t=0;t<.1f&&!Interrupted;){float dt=WellDt;t+=dt;if(controller.enabled)controller.Move(way*(WellTune.ThrustStep/.1f)*dt);yield return null;}
                Pose(9);Strike(line,WellTune.ThrustDamage,DamageType.Kinetic,.3f,WellTune.ThrustShove);strikeAt=0;Debug.Log("MERET_THRUST "+(n+1));
                AudioManager.PlayAt("impactMetal_000",transform,transform.position+Vector3.up*1.2f,.22f,AudioBus.SFX,50,.04f,1.05f);
                for(float t=0;t<.12f;){t+=WellDt;yield return null;}
            }
            Pose(0);Finish(WellTune.Rest[k]);
        }
        IEnumerator WellCut()
        {
            int k=WellStage;var way=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(way);float tell=WellTune.CutTell[k];
            var fan=Mark(Telegraph.Cone(transform.position,way,WellTune.CutReach,WellTune.CutAngle,tell,Telegraph.Heavy));var rest=transform.rotation;
            yield return null;// from the marking's own first frame
            for(float t=0;t<tell;)
            {
                t+=WellDt;if(Interrupted){transform.rotation=rest;yield break;}
                float s=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/tell));Pose(-4*s,0,.02f*s);transform.rotation=rest*Quaternion.Euler(0,-55*s,0);ThreatFrom(ShipOnly(fan));yield return null;
            }
            Strike(fan,WellTune.CutDamage,DamageType.Kinetic,.35f,WellTune.CutShove);strikeAt=0;Debug.Log("MERET_CUT");
            AudioManager.PlayAt("swing_2",transform,transform.position+Vector3.up*1.3f,.28f,AudioBus.SFX,50,.04f,1.1f);
            for(float t=0;t<.22f;){t+=WellDt;transform.rotation=rest*Quaternion.Euler(0,-55+120*Mathf.SmoothStep(0,1,t/.22f),0);yield return null;}
            transform.rotation=rest;Pose(0);Finish(WellTune.Rest[k]);
        }
        /// <summary>Her counter stance: a blow into it turns, and the first she answers down a short marked fan at whoever struck; then her counter is spent and she is open. Left alone, it runs out
        /// and she steps in with a thrust.</summary>
        IEnumerator WellGuard()
        {
            int k=WellStage;float stance=WellTune.Stance[k];
            var party=PartyController.Current;if(party!=null)transform.rotation=Quaternion.LookRotation(Toward(party.Active.transform.position));
            wellStruckBy=null;wellGuarding=true;var ring=Mark(Telegraph.Circle(transform.position,WellTune.GuardRing,stance,GuardBoundary));
            Debug.Log("MERET_GUARD "+stance.ToString("0.0"));
            if(!wellGuardSaid&&!brain.Passive){wellGuardSaid=true;BarkService.PlayAt("Meret","duel",0,true);}
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up,.2f,AudioBus.SFX,40,.05f,1.2f);
            for(float t=0;t<stance&&wellStruckBy==null;){t+=WellDt;if(!health.Alive)yield break;Pose(-2,-.05f);yield return null;}
            ring.Cancel();
            if(wellStruckBy==null)
            {
                // Nobody took the bait: she lowers her plate and steps in.
                wellGuarding=false;Pose(0);
                yield return WellThrust();yield break;
            }
            // The riposte, at whoever struck: marked, short, and a roll clears it. She is still behind her plate until it lands.
            var striker=wellStruckBy;var way=Toward(striker.transform.position);transform.rotation=Quaternion.LookRotation(way);
            var fan=Mark(Telegraph.Cone(transform.position,way,WellTune.RiposteReach,WellTune.RiposteAngle,WellTune.RiposteTell,Telegraph.Heavy));
            Debug.Log("MERET_COUNTER "+striker.character);
            // Counted from the marking's own first frame: it was laid this frame, and counting this one put the blow a frame ahead of what the marking (and whoever reads it) says.
            yield return null;
            for(float t=0;t<WellTune.RiposteTell;){t+=WellDt;if(!health.Alive)yield break;ThreatFrom(ShipOnly(fan));Pose(-6*Mathf.Clamp01(t/WellTune.RiposteTell));yield return null;}
            Strike(fan,WellTune.RiposteDamage,DamageType.Kinetic,WellTune.RiposteStagger,1.4f);strikeAt=0;wellGuarding=false;wellStruckBy=null;
            AudioManager.PlayAt("impactMetal_000",transform,transform.position+Vector3.up*1.2f,.26f,AudioBus.SFX,50,.04f,.95f);
            // Spent: open.
            Debug.Log("MERET_COUNTER_SPENT");wellOpenWhy="HER COUNTER IS SPENT";Pose(8,-.1f);
            if(party!=null&&!brain.Passive)BarkService.Play(party.Active.character,Situation("bossopening"),true);
            yield return Opening(WellTune.Spent[k],WellTune.Rest[k]);Pose(0);
        }
        /// <summary>Clamps thrown round the lead, one under him and the rest in a ring with gaps between: marked, then they close and hold whoever is in one.</summary>
        IEnumerator WellTraps()
        {
            int k=WellStage;int n=WellTune.Traps[k];float tell=WellTune.TrapTell[k];var lead=PartyController.Current.Active.transform.position;
            var marks=new List<Telegraph>{Mark(Telegraph.Circle(lead,WellTune.TrapRadius,tell,Telegraph.Heavy))};
            float turn=Random.Range(0,360f);
            for(int i=0;i<n-1;i++){var at=lead+Quaternion.Euler(0,turn+i*360f/(n-1),0)*Vector3.forward*WellTune.TrapRound;at.y=lead.y;marks.Add(Mark(Telegraph.Circle(at,WellTune.TrapRadius,tell,Telegraph.Heavy)));}
            Debug.Log($"MERET_TRAPS {n} phase={Phase}");WellSaysTraps();
            transform.rotation=Quaternion.LookRotation(Toward(lead));
            AudioManager.PlayAt("thrusterFire_000",transform,transform.position+Vector3.up,.2f,AudioBus.SFX,50,.05f,1.3f);
            // Counted from the marking's own first frame: it was laid this frame, and counting this one put the blow a frame ahead of what the marking (and whoever reads it) says.
            yield return null;
            for(float t=0;t<tell;){t+=WellDt;if(Interrupted)yield break;ThreatFrom(marks);Pose(-3*Mathf.Clamp01(t/tell));yield return null;}
            var once=new HashSet<Health>();foreach(var mark in marks)WellClamps(mark,once);
            AudioManager.PlayAt("impactMetal_000",transform,lead,.28f,AudioBus.SFX,50,.05f,1.2f);
            strikeAt=0;Pose(0);for(float t=0;t<.3f;){t+=WellDt;yield return null;}Finish(WellTune.Rest[k]);
        }
        /// <summary>A running thrust down a long marked lane: it ends with all of her behind it, overextended, met or not.</summary>
        IEnumerator WellLunge()
        {
            int k=WellStage;var controller=GetComponent<CharacterController>();
            var way=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(way);
            float length=WellTune.LungeLength;
            if(CombatCover.Sweep(transform.position+Vector3.up,transform.position+Vector3.up+way*(length+WellTune.Radius),.4f,out var wall))length=Mathf.Max(1.5f,wall.distance-WellTune.Radius-.2f);
            float tell=WellTune.LungeTell[k];var lane=Mark(Telegraph.Line(transform.position,way,length,WellTune.LungeWidth,tell,Telegraph.Heavy));
            yield return null;// from the marking's own first frame
            for(float t=0;t<tell;)
            {
                t+=WellDt;if(Interrupted)yield break;var at=PartyController.Current.Active.transform.position;
                strikeAt=lane.Contains(at)?Time.time+(tell-t)+Mathf.Max(0,Vector3.Dot(Flat(at-transform.position),way)-WellTune.LungeContact)/WellTune.LungeSpeed:0;
                Pose(-9*Mathf.Clamp01(t/tell),0,.04f);yield return null;
            }
            lane.Steady=true;Debug.Log("MERET_LUNGE");AudioManager.PlayAt("thrusterFire_000",transform,transform.position+Vector3.up,.22f,AudioBus.SFX,50,.04f,1.1f);
            var hit=new HashSet<Health>();float travelled=0;Collide(false);
            while(travelled<length&&!Interrupted)
            {
                float dt=WellDt;if(dt<=0){yield return null;continue;}
                float step=Mathf.Min(WellTune.LungeSpeed*dt,length-travelled);var before=transform.position;if(controller.enabled)controller.Move(way*step);
                float moved=Flat(transform.position-before).magnitude;travelled+=Mathf.Max(moved,step*.5f);if(moved<step*.25f)break;
                Contact(hit,transform.position,WellTune.LungeContact,WellTune.LungeDamage,.35f,lane);Pose(9);
                var now=PartyController.Current.Active;float left=Vector3.Dot(Flat(now.transform.position-transform.position),way);
                strikeAt=!hit.Contains(now.Health)&&lane.Contains(now.transform.position)&&left>0?Time.time+Mathf.Max(0,left-WellTune.LungeContact)/WellTune.LungeSpeed:0;
                yield return null;
            }
            Collide(true);lane.Release();strikeAt=0;
            if(Interrupted)yield break;
            wellOpenWhy="SHE IS OVEREXTENDED";Pose(12,-.2f);Debug.Log("MERET_OVEREXTENDED");
            var party=PartyController.Current;if(party!=null&&!brain.Passive)BarkService.Play(party.Active.character,Situation("bossopening"),true);
            yield return Opening(WellTune.Overextended[k],WellTune.Rest[k]);Pose(0);
        }
        /// <summary>Into her last phase she stops, steps back and asks her question, and listens: a quiet nothing is said over, in which she strikes at nobody and her plate turns a blow.</summary>
        IEnumerator WellListens()
        {
            var controller=GetComponent<CharacterController>();var party=PartyController.Current;
            var away=party!=null?-Toward(party.Active.transform.position):-transform.forward;wellListening=true;strikeAt=0;
            // Nothing of hers is left to land in the quiet: a clamp still closing under someone is called off.
            foreach(var trap in wellStillTraps)if(trap!=null)StopCoroutine(trap);wellStillTraps.Clear();wellStillMarks.Clear();Telegraph.CancelAll(this);
            Debug.Log("MERET_LISTENS");
            for(float t=0;t<.4f;){float dt=WellDt;t+=dt;if(controller.enabled)controller.Move(away*(WellTune.ListenBack/.4f)*dt);Pose(-2*Mathf.Clamp01(t/.4f));yield return null;}
            if(!brain.Passive)BarkService.PlayAt("Meret","duel",2,true);
            // Nothing is said over her question, and nothing of hers lands in the quiet after it.
            BarkService.Hush(this,true);
            for(float t=0;t<WellTune.Quiet;)
            {
                t+=WellDt;if(!health.Alive)break;
                if(party!=null)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(Toward(party.Active.transform.position)),Time.deltaTime*3);
                Pose(3,-.05f);yield return null;
            }
            BarkService.Hush(this,false);wellListening=false;Pose(0);Finish(1);
        }
    }
}
