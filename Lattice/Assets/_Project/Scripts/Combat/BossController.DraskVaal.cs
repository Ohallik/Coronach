using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Warmaster Drask Vaal, chapter 8's second boss, part one: the duel on Harrowgate's muster field, a war-frame lance and shield against the party, with the town watching. The Muster
    /// settles a question on the field where everyone can see the answer, and the duel is fought by the rules the Vaskar keep. He is not killed: at the end of his bar he yields
    /// (<see cref="Ending.Yields"/>) and the person takes over (<c>DraskLeavesField</c>), and then he runs for his skiff (part two: <c>BossController.DraskSkiff.cs</c>). The field's contract is
    /// in docs/maps/Harrowgate.md, "Duel ground contract".
    /// <para><b>The rules, as a player meets them.</b> He <b>waits for the first blow</b> ("Whenever you like"): rooted at his post, lance grounded, no special until a hero strikes him or comes
    /// within <see cref="DraskTune.WaitReach"/>. <b>His shield takes the front</b>: a blow from inside his front <see cref="DraskTune.ShieldArc"/> degrees lands at half and reads as
    /// resisted; a flank or his back takes it whole, and in his openings everything does. One to one is the custom; he called it even against a pair, and the pair's way in is one at his shield
    /// and one at his back. <b>A second blade is answered by his guard wing</b>: three command shouts (as the duel opens, at his shift, near the end), answered by two, then one, then nobody,
    /// as the town pulls his guard away. <b>He doesn't strike the fallen, or a hero helping them up</b>: while a hero is down he steps back, grounds his lance and waits, his guard too, until
    /// they are up or <see cref="DraskTune.StandOff"/> seconds pass. <b>Striking him while he stands off breaks the custom</b>: the blow lands, he comes on at once, he never stands off again,
    /// and his next shout is answered as the last one was (the town saw it). A hero who lands a clean blow in an opening hears him say so (his <c>duel</c> lines). It ends at a yield.</para>
    /// <list type="bullet">
    /// <item><b>Thrust</b>: a long thrust down a marked line, two in a row once he presses; a last thrust that meets nobody leaves him overreached, open. <b>Haft Sweep</b>: the haft swung
    /// round a marked fan before him, shoving. <b>Shield Rush</b>: behind his shield down a marked lane; one that meets nobody leaves his shield down, open.</item>
    /// <item><b>Pressing</b> (under half): quicker, the thrusts doubled, and the <b>Lunge</b>, a running thrust down a long marked lane that always ends overextended.</item>
    /// </list>
    /// He keeps to the field (<see cref="DuelGround"/>) where there is one; in the proving yard he fights on open ground and his guard steps in beside him.
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit DraskVaalKit=Register(new Kit
        {
            id="DraskVaal",title="WARMASTER DRASK VAAL",epithet="Thirty years, and every company home",thresholds=new[]{.5f},
            // The manifest's epithet ("Thirty years, eleven campaigns, every company home") ran 954 px with his name on the bar, past its 900: the campaigns are in his own line instead.
            choose=(boss,hero)=>boss.ChooseDrask(hero),
            moves={["Thrust"]=boss=>boss.DraskThrust(boss.Phase>=2?2:1),["HaftSweep"]=boss=>boss.HaftSweep(),["ShieldRush"]=boss=>boss.ShieldRush(),["Lunge"]=boss=>boss.DraskLunge(),
                ["Shout"]=boss=>boss.DraskShout()},
            // His shift is his second command shout ("Arrek! Sool! To me!"): the half promises it.
            signature=phase=>phase>=2?"Shout":null,
            shiftText="THE WARMASTER PRESSES  ·  BRACE",
            status=boss=>boss.DraskStatus(),
            fightGoal=(boss,label)=>boss.DraskGoal(label),
            // He yields and speaks (DraskLeavesField): the shared "boss" bark would be said over him.
            capsuleRadius=DraskTune.Radius,occlusion=false,ending=Ending.Yields,downBark="",
            // The war-frame's power pack on his back and the joints at both hips, a hand proud of a .9 m body at 1.4 m. His front is his shield's: nothing there to find.
            weakSpots=new[]{new Vector3(0,1.4f,-.78f),new Vector3(-.78f,1.4f,0),new Vector3(.78f,1.4f,0)},
            start=boss=>boss.DraskStart(),update=boss=>boss.DraskUpdate(),down=boss=>boss.DraskDown(),
        });
        /// <summary>Drask Vaal's numbers, all of them (his integrity is his EnemyDef's: HarrowgateBuilder.Bosses). Damage is before his second phase's +18%; a boss stays at threat 1. Every
        /// move he has in both halves is authored at 144 to 150 (12.1% of Idra's bar at Sync 25 to 15.3% of Taren's at 24: HarrowgateBuilder.Bosses' sizing), and the Lunge, which he has only
        /// pressing, at 126 (149 with the half's +18%).</summary>
        public static class DraskTune
        {
            /// <summary>His capsule's radius (2.5 m tall: his definition's).</summary>
            public const float Radius=.9f;
            /// <summary>His shield: how wide a front it covers, in degrees, and what of a blow from inside it lands.</summary>
            public const float ShieldArc=120,ShieldTakes=.5f;
            /// <summary>How near a hero must come to him to start the duel without a blow.</summary>
            public const float WaitReach=4;
            /// <summary>Thrust: the line and its width, the first tell held and pressing, the next one's, the step into it, the blow, the shove, and how long a last thrust on nobody leaves him overreached.</summary>
            public const float ThrustLength=9,ThrustWidth=1.6f,ThrustTell=.8f,ThrustTellLater=.7f,ThrustNext=.55f,ThrustStep=.8f,ThrustDamage=148,ThrustShove=1.5f,Overreached=1.4f;
            /// <summary>Haft Sweep: the fan's reach and angle, the tell held and pressing, the blow and the shove.</summary>
            public const float SweepReach=4.4f,SweepAngle=160,SweepTell=.7f,SweepTellLater=.6f,SweepDamage=144,SweepShove=2.6f;
            /// <summary>Shield Rush: the lane's shortest and longest and its width, the tell, the pace, what it strikes in passing, and how long a rush on nobody leaves his shield down.</summary>
            public const float RushShortest=6,RushLongest=14,RushWidth=2.6f,RushTell=.9f,RushSpeed=16,RushContact=1.5f,RushDamage=150,ShieldDown=1.6f;
            /// <summary>Lunge: the lane and its width, the tell, the pace, what it strikes in passing, and how long it leaves him overextended.</summary>
            public const float LungeLength=14,LungeWidth=1.8f,LungeTell=.9f,LungeSpeed=22,LungeContact=1.4f,LungeDamage=126,Overextended=1.8f;
            /// <summary>A command shout: how long the lance is raised, how far from him his guard steps in, when the hero's count comes after it, and the share of his bar under which the third comes.</summary>
            public const float ShoutTell=.8f,GuardIn=5,CountAfter=1.4f,ThirdShoutAt=.2f;
            /// <summary>How many of his guard answer each shout, first to third (and after).</summary>
            public static readonly int[] Answering={2,1,0};
            /// <summary>His courtesy: how long he stands off for a fallen hero at most, how far he steps back, how soon he may praise a blow again, and how long the bar says the custom is broken.</summary>
            public const float StandOff=12,StandBack=3,PraiseEvery=12,BrokenSaid=4;
            public static float Rest(int phase)=>phase>=2?2.1f:2.6f;
        }
        static readonly Color GuardTone=new(.86f,.84f,.8f,.4f);
        bool draskWaiting,draskStandingOff,draskCustomBroken,draskHears;int draskShouts,draskAnswered=-1,draskHeldOver=-1;
        float draskPraisedOpening,draskPraiseAt,draskBrokenAt=-99;string draskOpenWhy="";DuelGround draskGround;
        /// <summary>He hasn't been struck or come up to yet: the duel is the party's to begin.</summary>
        public bool Waiting=>draskWaiting;
        /// <summary>A hero is down and he is standing off for them.</summary>
        public bool StandingOff=>draskStandingOff;
        public bool CustomBroken=>draskCustomBroken;
        public int ShoutsMade=>draskShouts;
        float DraskRest=>DraskTune.Rest(Phase);

        void DraskStart()
        {
            draskGround=DuelGround.For(health.id);draskWaiting=true;draskStandingOff=draskCustomBroken=false;draskShouts=0;draskAnswered=draskHeldOver=-1;brain.Rooted=true;
            // His shield takes the front: a blow from inside it lands at half, read as resisted. An opening is a break, which a plate never touches: it lands whole from anywhere.
            health.Plate=packet=>DraskShielded(packet)?DraskTune.ShieldTakes:1;
            health.Damaged+=DraskStruck;draskHears=true;
            Debug.Log("DRASK_WAITS");
        }
        bool DraskShielded(DamagePacket packet)
        {
            if(packet.source==null)return false;
            var from=Flat(packet.source.transform.position-transform.position);if(from.sqrMagnitude<.01f)return true;
            return Vector3.Angle(Flat(transform.forward),from)<DraskTune.ShieldArc*.5f;
        }
        void DraskBegins(string why)
        {
            if(!draskWaiting)return;
            draskWaiting=false;brain.Rooted=false;if(engaged<0)engaged=Time.realtimeSinceStartup;
            // He opens with his first command shout, whoever began it.
            if(draskShouts==0&&queuedMove==null)queuedMove="Shout";
            nextSpecial=Mathf.Min(nextSpecial,Time.time+.6f);
            Debug.Log("DRASK_DUEL_BEGINS "+why);
        }
        void DraskStruck(Health _,DamagePacket packet,float amount)
        {
            if(packet.source==null||!packet.source.friendly)return;
            if(draskWaiting)DraskBegins("struck");
            if(draskStandingOff&&!draskCustomBroken)
            {
                // A blow on a man standing off for the fallen: the custom is broken, and the town saw it.
                draskCustomBroken=true;draskBrokenAt=Time.time;draskHeldOver=draskAnswered;
                Debug.Log("DRASK_CUSTOM_BROKEN");
            }
            // A clean blow in his opening, once an opening, and not every few seconds: he says so.
            if(amount>0&&health.Broken&&Time.time<openingUntil&&!Mathf.Approximately(draskPraisedOpening,openingUntil)&&Time.time>=draskPraiseAt)
            {
                draskPraisedOpening=openingUntil;draskPraiseAt=Time.time+DraskTune.PraiseEvery;
                BarkService.Play("Drask","duel",true);Debug.Log("DRASK_PRAISES");
            }
        }
        void DraskUpdate()
        {
            var party=PartyController.Current;
            // A move begun is the duel under way, however it began.
            if(draskWaiting&&busy)DraskBegins("moved");
            if(draskWaiting)
            {
                brain.Rooted=true;
                if(party!=null)foreach(var member in party.members)if(member.Health.Alive&&Flat(member.transform.position-transform.position).magnitude<DraskTune.WaitReach+GetComponent<CharacterController>().radius){DraskBegins("approached");break;}
            }
            else if(!draskStandingOff)brain.Rooted=false;
            // He keeps to the field: the kerb is the crowd's front rank.
            if(draskGround!=null&&brain.Controller.enabled&&!draskGround.Holds(transform.position,GetComponent<CharacterController>().radius))
            {var kept=draskGround.Keep(transform.position,GetComponent<CharacterController>().radius);brain.Controller.Move(kept-transform.position);}
            // His third shout comes near the end of his bar.
            if(!draskWaiting&&draskShouts==2&&queuedMove==null&&health.integrity<health.maximum*DraskTune.ThirdShoutAt&&!shifting)queuedMove="Shout";
            // A hero down: he stands off, unless the custom has been broken.
            if(!draskWaiting&&!draskCustomBroken&&!busy&&!shifting&&!health.Broken&&!brain.Passive&&party!=null&&DraskFallen()!=null)Begin("StandsOff",DraskStandsOff());
            if(busy||health.Broken||brain.Passive||brain.Telegraphing)return;
            if(draskWaiting)Pose(2,-.05f);
            else Pose(Phase>=2?1.5f*Mathf.Sin(Time.time*2.4f):.8f*Mathf.Sin(Time.time*1.3f));
        }
        CombatActor DraskFallen(){var party=PartyController.Current;if(party==null)return null;foreach(var member in party.members)if(!member.Health.Alive)return member;return null;}
        void DraskDown()
        {
            if(draskHears){health.Damaged-=DraskStruck;draskHears=false;}
            draskStandingOff=false;LingeringArea.DismissAll();
            Debug.Log("DRASK_YIELDS");
        }
        string DraskStatus()
        {
            if(health==null||!health.Alive)return "";
            if(health.Broken&&Time.time<openingUntil)return draskOpenWhy+"  ·  STRIKE";
            if(draskWaiting)return "HE WAITS FOR YOUR FIRST BLOW";
            if(draskStandingOff)return "HE STANDS OFF FOR THE FALLEN";
            if(Time.time<draskBrokenAt+DraskTune.BrokenSaid)return "THE CUSTOM IS BROKEN  ·  THE MUSTER SAW IT";
            return "THE MUSTER IS WATCHING";
        }
        string DraskGoal(string label)
        {
            if(health==null||!health.Alive)return label;
            if(draskStandingOff)return "Help your partner up: he won't strike while you do";
            return label;
        }
        /// <summary>His opening: he is overreached, his shield is down or he is overextended. Open as a stunned body is, for a set time.</summary>
        IEnumerator DraskOpens(float seconds,string why,string said)
        {
            draskOpenWhy=why;Debug.Log(said);strikeAt=0;Pose(12,-.2f);
            BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);
            yield return Opening(seconds,DraskRest);Pose(0);
        }

        (string,IEnumerator) ChooseDrask(CombatActor hero)
        {
            // Waiting, he does nothing of his own: the first blow is the party's.
            if(draskWaiting)return (null,null);
            float distance=Flat(hero.transform.position-transform.position).magnitude;
            // His thrust and his rush are his openings when they meet nobody, and the plan wants one in every phase: whatever the range they come round.
            string move=Phase<2
                ?Pick(("Thrust",distance<DraskTune.ThrustLength?2f:FullOffer),("HaftSweep",distance<DraskTune.SweepReach+1?1.8f:0),("ShieldRush",distance>4?1.5f:FullOffer))
                :Pick(("Thrust",distance<DraskTune.ThrustLength?1.8f:FullOffer),("HaftSweep",distance<DraskTune.SweepReach+1?1.6f:0),("ShieldRush",distance>4?1.2f:.4f),("Lunge",distance>5?1.6f:FullOffer));
            return move switch{"HaftSweep"=>(move,HaftSweep()),"ShieldRush"=>(move,ShieldRush()),"Lunge"=>(move,DraskLunge()),_=>(move,DraskThrust(Phase>=2?2:1))};
        }
        IEnumerator DraskThrust(int thrusts)
        {
            var controller=GetComponent<CharacterController>();bool reached=false;
            for(int n=0;n<thrusts;n++)
            {
                var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
                float tell=n>0?DraskTune.ThrustNext:Phase>=2?DraskTune.ThrustTellLater:DraskTune.ThrustTell;
                // Painted from where he will stand when it lands: what is seen is what is struck.
                var line=Mark(Telegraph.Line(transform.position+direction*(DraskTune.ThrustStep*.5f),direction,DraskTune.ThrustLength,DraskTune.ThrustWidth,tell,Telegraph.Threat));
                strikeAt=Time.time+tell;
                yield return Windup(tell,-8,0,.03f);
                if(Interrupted)yield break;
                for(float t=0;t<.1f&&!Interrupted;){if(GameTime.Paused){yield return null;continue;}float dt=Time.deltaTime;t+=dt;if(controller!=null&&controller.enabled)controller.Move(direction*(DraskTune.ThrustStep/.1f)*dt);yield return null;}
                Pose(10);var met=new HashSet<Health>();Strike(line,DraskTune.ThrustDamage,DamageType.Kinetic,.3f,DraskTune.ThrustShove,met,true);reached=met.Count>0;
                AudioManager.PlayAt("impactMetal_000",transform,transform.position+Vector3.up*1.2f,.22f,AudioBus.SFX,50,.04f,.9f);
                yield return Windup(.12f,0);
            }
            if(reached){Pose(0);Finish(DraskRest);yield break;}
            // The last thrust met nobody: he is overreached.
            yield return DraskOpens(DraskTune.Overreached,"HE HAS OVERREACHED","DRASK_OVERREACHED");
        }
        IEnumerator HaftSweep()
        {
            var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            float tell=Phase>=2?DraskTune.SweepTellLater:DraskTune.SweepTell;
            var fan=Mark(Telegraph.Cone(transform.position,direction,DraskTune.SweepReach,DraskTune.SweepAngle,tell,Telegraph.Heavy));
            strikeAt=Time.time+tell;
            // The haft goes back over his shoulder as the fan fills, and comes round through it.
            var rest=transform.rotation;
            for(float t=0;t<tell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted){transform.rotation=rest;yield break;}
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/tell));Pose(-5*k,0,.03f*k*k);transform.rotation=rest*Quaternion.Euler(0,-60*k,0);yield return null;
            }
            Strike(fan,DraskTune.SweepDamage,DamageType.Kinetic,.35f,DraskTune.SweepShove);
            AudioManager.PlayAt("swing_2",transform,transform.position+Vector3.up*1.4f,.3f,AudioBus.SFX,50,.04f,.8f);
            for(float t=0;t<.25f&&!Interrupted;){if(!GameTime.Paused)t+=Time.deltaTime;transform.rotation=rest*Quaternion.Euler(0,-60+140*Mathf.SmoothStep(0,1,t/.25f),0);Pose(6);yield return null;}
            transform.rotation=rest;Pose(0);strikeAt=0;Finish(DraskRest);
        }
        /// <summary>Behind his shield, or at the end of his lance, down a marked lane: whoever he touches inside it is struck once. It stops at a wall and at the field's kerb. True if it met someone.</summary>
        IEnumerator DraskRun(Vector3 direction,float length,float width,float tell,float speed,float contact,float damage,System.Action<bool> met)
        {
            var controller=GetComponent<CharacterController>();
            var lane=Mark(Telegraph.Line(transform.position,direction,length,width,tell,Telegraph.Heavy));
            strikeAt=Time.time+tell;
            yield return Windup(tell,-9,0,.04f);
            if(Interrupted)yield break;
            lane.Release();Pose(9);strikeAt=0;
            AudioManager.PlayAt("thrusterFire_000",transform,transform.position+Vector3.up,.22f,AudioBus.SFX,50,.04f,.9f);
            var hit=new HashSet<Health>();float travelled=0,elapsed=0;Collide(false);
            while(travelled<length&&elapsed<length/speed+.3f&&!Interrupted)
            {
                if(GameTime.Paused){yield return null;continue;}
                elapsed+=Time.deltaTime;float step=Mathf.Min(speed*Time.deltaTime,length-travelled);
                if(CombatCover.Sweep(transform.position+Vector3.up,transform.position+Vector3.up+direction*(step+controller.radius+.2f),.4f,out _))break;
                if(draskGround!=null&&!draskGround.Holds(transform.position+direction*(step+controller.radius),0))break;
                var before=transform.position;if(controller.enabled)controller.Move(direction*step);travelled+=Flat(transform.position-before).magnitude;
                Contact(hit,transform.position,contact,damage,.4f,lane);
                yield return null;
            }
            Collide(true);Pose(0);met(hit.Count>0);
        }
        IEnumerator ShieldRush()
        {
            var target=PartyController.Current.Active.transform.position;var direction=Toward(target);transform.rotation=Quaternion.LookRotation(direction);
            float length=Mathf.Clamp(Flat(target-transform.position).magnitude+3,DraskTune.RushShortest,DraskTune.RushLongest);bool met=false;
            yield return DraskRun(direction,length,DraskTune.RushWidth,DraskTune.RushTell,DraskTune.RushSpeed,DraskTune.RushContact,DraskTune.RushDamage,m=>met=m);
            if(Interrupted)yield break;
            if(met){Finish(DraskRest);yield break;}
            // He met nobody: he skids, and his shield is down.
            yield return DraskOpens(DraskTune.ShieldDown,"HIS SHIELD IS DOWN","DRASK_SHIELD_DOWN");
        }
        IEnumerator DraskLunge()
        {
            var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);bool met=false;
            yield return DraskRun(direction,DraskTune.LungeLength,DraskTune.LungeWidth,DraskTune.LungeTell,DraskTune.LungeSpeed,DraskTune.LungeContact,DraskTune.LungeDamage,m=>met=m);
            if(Interrupted)yield break;
            // A running thrust ends with all of him behind it: overextended, met or not.
            yield return DraskOpens(DraskTune.Overextended,"HE IS OVEREXTENDED","DRASK_OVEREXTENDED");
        }
        /// <summary>A command shout: his line, his lance raised, and as many of his guard wing as still answer step onto the field on pale rings, from his guard post's side. Then the
        /// hero's count of who came. The third shout is answered by nobody; a broken custom holds the count where it was.</summary>
        [SetsFlag("vask.shout",Prefix=true)] // vask.shout1 to vask.shout3
        IEnumerator DraskShout()
        {
            int n=++draskShouts;int answering=DraskTune.Answering[Mathf.Clamp(n-1,0,DraskTune.Answering.Length-1)];
            if(draskHeldOver>answering){answering=draskHeldOver;draskHeldOver=-1;}
            draskAnswered=answering;
            var party=PartyController.Current;if(party!=null&&party.Active!=null)transform.rotation=Quaternion.LookRotation(Toward(party.Active.transform.position));
            BarkService.PlayAt("Drask","shout",Mathf.Clamp(n-1,0,2),true);
            if(n<=3&&GameServices.Current!=null)GameServices.Current.Flags.SetBool("vask.shout"+n,true);
            Debug.Log($"DRASK_SHOUT {n} answered={answering}");
            // Where his guard steps in: toward his post on the field, or beside him.
            var post=draskGround!=null&&draskGround.guardPost!=null?draskGround.guardPost.position:transform.position+transform.right*10;
            var side=Flat(post-transform.position);side=side.sqrMagnitude>.01f?side.normalized:transform.right;
            var definition=GameCatalog.Find<EnemyDef>("MusterPikeguard");var rings=new List<(Vector3 at,Telegraph ring)>();
            summoned.RemoveAll(s=>s==null||!s.Health.Alive);
            if(definition!=null)for(int i=0;i<answering;i++)
            {
                var at=CombatCover.Ground(transform.position+Quaternion.Euler(0,(i-(answering-1)*.5f)*40,0)*side*DraskTune.GuardIn+Vector3.up*3);
                if(draskGround!=null)at=draskGround.Keep(at,1);
                var ring=Mark(Telegraph.Circle(at,1.1f,DraskTune.ShoutTell,GuardTone));rings.Add((at,ring));
            }
            else if(answering>0)Debug.Log("DRASK_NO_GUARD MusterPikeguard is not defined");
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up*2,.2f,AudioBus.SFX,50,.05f,.7f);
            yield return Windup(DraskTune.ShoutTell,-12,.15f);
            if(Interrupted)yield break;
            foreach(var (at,ring) in rings)
            {
                ring.Release();var guard=ActorFactory.Enemy(definition,at+Vector3.up*.05f);summoned.Add(guard);
                guard.transform.rotation=Quaternion.LookRotation(party!=null&&party.Active!=null?Toward(party.Active.transform.position):transform.forward);
                CombatVfx.Burst(at+Vector3.up*.8f,new Color(.8f,.75f,.65f),"shot");
            }
            if(rings.Count>0)Debug.Log("BOSS_SUMMON "+health.id+" "+summoned.Count);
            // The count, a beat after: who came. On the party's clock, so a blow that cuts the shout short doesn't swallow it.
            if(party!=null&&party.Active!=null){string lead=party.Active.character;int line=answering>=2?0:answering==1?1:2;party.StartCoroutine(CountAfter(lead,line));}
            Pose(0);Finish(DraskRest*.6f);
            static IEnumerator CountAfter(string lead,int line){yield return new WaitForSeconds(DraskTune.CountAfter);BarkService.PlayAt(lead,"shout",line,true);}
        }
        /// <summary>A hero is down: he steps back from the lead, grounds his lance and waits, and his guard waits with him, until the fallen is up or his patience runs out, or until someone
        /// strikes him while he stands off.</summary>
        IEnumerator DraskStandsOff()
        {
            draskStandingOff=true;Debug.Log("DRASK_STANDS_OFF");strikeAt=0;
            var controller=GetComponent<CharacterController>();var party=PartyController.Current;
            foreach(var guard in summoned)if(guard!=null&&guard.Health.Alive)guard.Passive=true;
            var away=party!=null&&party.Active!=null?-Toward(party.Active.transform.position):-transform.forward;
            for(float t=0;t<.4f;)
            {
                if(GameTime.Paused){yield return null;continue;}float dt=Time.deltaTime;t+=dt;
                if(controller.enabled)controller.Move(away*(DraskTune.StandBack/.4f)*dt);Pose(-3*Mathf.Clamp01(t/.4f));yield return null;
            }
            brain.Rooted=true;
            for(float until=Time.time+DraskTune.StandOff;Time.time<until&&!draskCustomBroken&&health.Alive&&DraskFallen()!=null;)
            {
                if(party!=null&&party.Active!=null)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(Toward(party.Active.transform.position)),Time.deltaTime*3);
                Pose(6,-.12f);yield return null;
            }
            foreach(var guard in summoned)if(guard!=null&&guard.Health.Alive)guard.Passive=false;
            draskStandingOff=false;brain.Rooted=false;Pose(0);
            // Up again, or his patience gone: he says so, as he would to anyone at drill.
            if(!draskCustomBroken){Debug.Log("DRASK_RESUMES");BarkService.PlayAt("Drask","duel",2,true);}
            Finish(draskCustomBroken?.3f:1.2f);
        }
    }
}
