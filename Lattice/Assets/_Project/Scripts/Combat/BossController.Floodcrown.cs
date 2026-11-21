using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The Floodcrown, chapter 3's boss: an amphibious giant with an antler crown, at home in the Walking Canopy's flooded basin and in charge of its gates. It is not killed: at
    /// nothing it goes down the drain channel with the last of the water (<see cref="Ending.Leaves"/>, <see cref="BossExit"/>).
    /// <list type="bullet">
    /// <item><b>Flooded</b> (to half). While any <see cref="FloodGate"/> stands the flood holds it up: it is braced, and takes half. Antler Sweep, a wide marked fan. Gate Slam: it
    /// throws a gate open and a wave runs from that gate down a marked lane; the wave is water and strikes only who stands in water (<see cref="IArenaWater"/>: a platform is
    /// dry), and the gate it came through stands open to blows for a few seconds after. Wading Charge, down a marked lane: run aground on a platform or the rim it is stuck for a
    /// long opening, and at the end of any other run its own wave breaks over it for a short one; either way every gate stands open while it lasts. Each gate broken drains a third
    /// of the water (the water's owner hears the level through IArenaWater) and takes a sixth of its bar; the third takes it to the line.</item>
    /// <item><b>Drained</b> (under half). If it reaches half with gates standing it opens them itself and the flood goes out in the shift's wave. A faster duel on the bare floor:
    /// Crown Charge, twice, leaving puddles that slow (boundaries, not dangers: <see cref="LingeringArea"/>); Stomp, a slam and then a ring of the floor further out, after which
    /// its crown is lodged and it is open; the sweep, quicker.</item>
    /// </list>
    /// It fights without gates or water too (the proving yard): then it is never braced, the wave is its own bow wave, and its phases turn on integrity alone.
    /// The basin is seen from the south-west, so between moves it works round rather than stand between the camera and the hero.
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit FloodcrownKit=Register(new Kit
        {
            id="Floodcrown",title="THE FLOODCROWN",epithet="The storm's creature, at home on the crossing",thresholds=new[]{.5f},
            choose=(boss,hero)=>boss.ChooseFloodcrown(hero),
            moves={["AntlerSweep"]=boss=>boss.AntlerSweep(),["GateSlam"]=boss=>boss.GateSlam(),["WadingCharge"]=boss=>boss.FloodCharge(1,false),["CrownCharge"]=boss=>boss.FloodCharge(2,true),["Stomp"]=boss=>boss.CrownStomp()},
            signature=phase=>phase>=2?"CrownCharge":null,
            shift=boss=>boss.FloodcrownDrains(),shiftText="THE FLOOD GOES OUT  ·  BRACE",
            status=boss=>boss.Flooded?"THE FLOOD HOLDS IT  ·  BREAK THE GATES":"",
            fightGoal=(boss,label)=>boss.Flooded?"Break the flood gates on the basin's rim":label,
            capsuleRadius=2,occlusion=true,ending=Ending.Leaves,
            start=boss=>boss.FloodcrownStart(),update=boss=>boss.FloodcrownUpdate(),down=boss=>boss.FloodcrownDown(),
        });
        /// <summary>The Floodcrown's numbers, all of them (its integrity is its EnemyDef's: XylosBuilder.Enemies). Damage is before the phase's +18%: each move takes 12.8 to 15.6% of a Sync 12
        /// bar in Refined parts (bar 318, a hit landing at .725) in whichever phase it is used, more than anything in the Canopy (a Rootwarden's line, 12.5%; XylosCombatTests).</summary>
        public static class FloodcrownTune
        {
            /// <summary>A gate's integrity and the bar it takes with it, as shares of the boss's; how long a third of the water takes to go.</summary>
            public const float GateIntegrity=.06f,GateBar=1f/6,DrainSeconds=2.6f;
            public const float SweepReach=8.5f,SweepAngle=150,SweepTell=.9f,SweepTellDry=.7f,SweepDamage=56;
            /// <summary>Gate Slam: its tell, the wave's lane, pace, depth front to back and damage, how far it runs, and how long its gate stands open once the wave has gone.</summary>
            public const float SlamTell=1.1f,WaveWidth=5,WaveSpeed=16,WaveBand=1.6f,WaveDamage=60,WaveReach=34,GateStandsOpen=4;
            /// <summary>The charges: tell and pace flooded and dry, the lane, the contact, how long it is stuck aground and swamped by its own wave, and the rise of ground that grounds it.</summary>
            public const float ChargeTell=.9f,ChargeTellDry=.7f,ChargeSpeed=15,ChargeSpeedDry=20,ChargeWidth=4,ChargeContact=2.6f,ChargeDamage=58,Aground=4.5f,Swamped=1.5f,Grounds=.5f;
            public const float PuddleRadius=2.4f,PuddleSeconds=6,PuddlePace=.6f,PuddleEvery=5;
            /// <summary>Stomp: the slam, then the ring further out a beat later, then how long its crown is lodged.</summary>
            public const float StompRadius=4.5f,StompTell=.8f,StompDamage=52,RingInner=6.5f,RingOuter=11,RingAfter=.55f,RingDamage=48,Lodged=1.8f;
            public const float RestFlooded=3.4f,RestDry=2.4f;
            /// <summary>How squarely between the camera and the hero it has to stand before it works round, within how far of him, and how fast.</summary>
            public const float InTheWay=.8f,InTheWayWithin=11,WorksRound=3.2f;
        }
        static readonly Color FloodWater=new(.7f,.92f,1f,.8f);
        float gatesOpenUntil,slammedUntil,waterShown=-1;FloodGate slammed;int gateTurn;bool hearsGates;
        /// <summary>The flood still holds it up: a gate stands and it hasn't given the basin up.</summary>
        public bool Flooded=>health!=null&&health.Alive&&Phase<2&&FloodGate.Standing>0;
        float FloodcrownRest=>Phase>=2?FloodcrownTune.RestDry:FloodcrownTune.RestFlooded;
        void FloodcrownStart()
        {
            // The gates are the first half of its bar, so their integrity is the fight's to say, not the scene's: each stands at a share of its own.
            float each=brain.definition.integrity*FloodcrownTune.GateIntegrity;
            foreach(var gate in FloodGate.All)if(gate.Holding)gate.Health.maximum=gate.Health.integrity=each;
            FloodGate.Expose(false);FloodGate.Broke+=FloodcrownGateBroke;hearsGates=true;waterShown=-1;
            var leaving=gameObject.AddComponent<BossDeparture>();leaving.sound="water_rush";leaving.speed=9;
        }
        void FloodcrownUpdate()
        {
            if(Flooded)health.ShieldUntil=Time.time+.25f;
            // A gate can be hit while an opening lasts, and the one a wave came through for a little after: shut the rest of the time.
            bool open=Time.time<gatesOpenUntil;
            foreach(var gate in FloodGate.All)
            {
                if(!gate.Holding)continue;bool want=open||gate==slammed&&Time.time<slammedUntil;
                if(gate.Health.Damageable!=want)gate.Health.Damageable=want;
            }
            // The water follows the gates. The first reading is how the arena stands as the fight begins: no ceremony.
            float held=FloodGate.All.Count>0?FloodGate.Held:0;
            if(!Mathf.Approximately(held,waterShown)){bool first=waterShown<0;waterShown=held;ArenaWater.SetLevel(held,first?0:FloodcrownTune.DrainSeconds);}
            if(busy||health.Broken||brain.Passive||brain.Telegraphing)return;
            Pose(1.5f*Mathf.Sin(Time.time*1.1f),.06f*Mathf.Sin(Time.time*2));
            WorkRoundTheCamera();
        }
        /// <summary>Stood between the fixed camera and the hero, an eight-metre body is all the player can see of his own fight: it wades round to one side instead.</summary>
        void WorkRoundTheCamera()
        {
            var view=Camera.main;var zone=ZoneController.Current;var party=PartyController.Current;
            if(view==null||zone==null||zone.Flight||party==null||party.Active==null||!brain.Controller.enabled)return;
            var fromHero=Flat(transform.position-party.Active.transform.position);float gap=fromHero.magnitude;
            if(gap<.5f||gap>FloodcrownTune.InTheWayWithin)return;
            var toCamera=-Flat(view.transform.forward);if(toCamera.sqrMagnitude<.01f)return;toCamera.Normalize();
            if(Vector3.Dot(fromHero/gap,toCamera)<FloodcrownTune.InTheWay)return;
            var across=Vector3.Cross(Vector3.up,toCamera);
            brain.Controller.Move(across*((Vector3.Dot(fromHero,across)>=0?1:-1)*FloodcrownTune.WorksRound*Time.deltaTime));
        }
        void FloodcrownGateBroke(FloodGate gate)
        {
            // A static event outlives a boss the scene took away mid-fight.
            if(this==null){FloodGate.Broke-=FloodcrownGateBroke;return;}
            if(health==null||!health.Alive||!isActiveAndEnabled)return;
            bool last=FloodGate.Standing==0;float line=health.maximum*(Thresholds.Length>0?Thresholds[0]:.5f),after=health.integrity-health.maximum*FloodcrownTune.GateBar;
            // The flood is half its strength, a sixth to a gate. Only the last gate carries it over the line: the shift into its second phase is the water going.
            if(health.integrity>line)health.integrity=Mathf.Max(1,last?Mathf.Min(after,line-1):Mathf.Max(after,line+1));
            if(engaged<0)engaged=Time.realtimeSinceStartup;
            Tremor(.4f,.09f);Debug.Log($"FLOODCROWN_GATE_DOWN standing={FloodGate.Standing}");
        }
        /// <summary>What its shift brings: the water goes. Gates still standing it throws open itself.</summary>
        void FloodcrownDrains()
        {
            if(FloodGate.Standing>0)FloodGate.SettleAll();
            gatesOpenUntil=slammedUntil=0;waterShown=0;ArenaWater.SetLevel(0,FloodcrownTune.DrainSeconds);
            AudioManager.PlayAt("water_rush",null,transform.position,.34f,AudioBus.SFX,40,.2f);Debug.Log("FLOODCROWN_DRAINS");
        }
        void FloodcrownDown()
        {
            if(hearsGates){FloodGate.Broke-=FloodcrownGateBroke;hearsGates=false;}
            LingeringArea.DismissAll();
            if(FloodGate.Standing>0)FloodGate.SettleAll();
            ArenaWater.SetLevel(0,FloodcrownTune.DrainSeconds);
        }
        /// <summary>Out of the water while water stands: a platform, or floor the gates have already uncovered. A wave breaks under it. With no water to ask, nowhere is.</summary>
        static bool Dry(Vector3 point)=>ArenaWater.Current!=null&&ArenaWater.Level>0&&!ArenaWater.Submerged(point);

        (string,IEnumerator) ChooseFloodcrown(CombatActor hero)
        {
            float distance=Flat(hero.transform.position-transform.position).magnitude;bool near=distance<FloodcrownTune.SweepReach+1;
            if(Phase<2)
            {
                // The charge is its opening and comes round whatever the range; the slam is the gates' turn to stand open.
                string wet=Pick(("AntlerSweep",near?2f:.4f),("GateSlam",1.5f),("WadingCharge",distance>5?2f:FullOffer));
                return wet switch{"GateSlam"=>(wet,GateSlam()),"WadingCharge"=>(wet,FloodCharge(1,false)),_=>(wet,AntlerSweep())};
            }
            string dry=Pick(("AntlerSweep",near?1.8f:.4f),("CrownCharge",distance>5?2f:.9f),("Stomp",distance<10?1.6f:FullOffer));
            return dry switch{"CrownCharge"=>(dry,FloodCharge(2,true)),"Stomp"=>(dry,CrownStomp()),_=>(dry,AntlerSweep())};
        }
        IEnumerator AntlerSweep()
        {
            var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            float tell=Phase>=2?FloodcrownTune.SweepTellDry:FloodcrownTune.SweepTell;
            var arc=Mark(Telegraph.Cone(transform.position,direction,FloodcrownTune.SweepReach,FloodcrownTune.SweepAngle,tell,Telegraph.Heavy));
            strikeAt=Time.time+tell;
            // The crown goes back over one shoulder as the fan fills, and comes round through it.
            var rest=transform.rotation;
            for(float t=0;t<tell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted){transform.rotation=rest;yield break;}
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/tell));Pose(-7*k,0,.05f*k*k);transform.rotation=rest*Quaternion.Euler(0,-55*k,0);
                yield return null;
            }
            Strike(arc,FloodcrownTune.SweepDamage,DamageType.Kinetic,.35f,2.5f);
            AudioManager.PlayAt("swing_2",transform,transform.position+Vector3.up*2,.3f,AudioBus.SFX,50,.04f,.6f);
            for(float t=0;t<.28f&&!Interrupted;){if(!GameTime.Paused)t+=Time.deltaTime;transform.rotation=rest*Quaternion.Euler(0,-55+125*Mathf.SmoothStep(0,1,t/.28f),0);Pose(8);yield return null;}
            transform.rotation=rest;Pose(0);Finish(FloodcrownRest);
        }
        IEnumerator GateSlam()
        {
            var hero=PartyController.Current.Active.transform.position;
            // The gates take it in turn, round the rim, so the one that will stand open next can be read ahead. With none standing the wave is its own.
            FloodGate gate=null;int standing=FloodGate.Standing;
            if(standing>0)
            {
                int want=gateTurn++%standing,seen=0;FloodGate[] order=FloodGate.All.ToArray();System.Array.Sort(order,(a,b)=>a.index.CompareTo(b.index));
                foreach(var candidate in order)if(candidate.Holding&&seen++==want){gate=candidate;break;}
            }
            var origin=gate!=null?gate.transform.position:transform.position;var run=Flat(hero-origin);float toHero=run.magnitude;
            var direction=toHero>.5f?run/toHero:transform.forward;float length=Mathf.Min(FloodcrownTune.WaveReach,toHero+10);
            transform.rotation=Quaternion.LookRotation(gate!=null?Toward(origin):direction);
            var lane=Mark(Telegraph.Line(origin,direction,length,FloodcrownTune.WaveWidth,FloodcrownTune.SlamTell,Telegraph.Heavy));
            strikeAt=Time.time+FloodcrownTune.SlamTell+Mathf.Max(0,toHero-FloodcrownTune.WaveBand)/FloodcrownTune.WaveSpeed;
            yield return Windup(FloodcrownTune.SlamTell,-12,.5f,.05f);
            if(Interrupted)yield break;
            Pose(10);Tremor(.4f,.1f);
            AudioManager.PlayAt("explosionCrunch_000",transform,origin,.26f,AudioBus.SFX,50);AudioManager.PlayAt("water_rush",null,origin,.3f,AudioBus.SFX,50,.2f,1.2f);
            if(gate!=null){slammed=gate;slammedUntil=Time.time+length/FloodcrownTune.WaveSpeed+FloodcrownTune.GateStandsOpen;Debug.Log("FLOODCROWN_GATE_OPEN "+gate.index);}
            // The lane stays marked ahead of the wave until the wave has run it, and the wave itself is a band of the same paint moving down it.
            lane.Steady=true;var front=Mark(Telegraph.Line(origin,direction,FloodcrownTune.WaveBand,FloodcrownTune.WaveWidth,99,Telegraph.Heavy));front.Steady=true;
            var met=new HashSet<Health>();int frame=0;
            for(float ran=0;ran<length&&!Interrupted;)
            {
                if(GameTime.Paused){yield return null;continue;}
                ran=Mathf.Min(length,ran+FloodcrownTune.WaveSpeed*Time.deltaTime);float back=Mathf.Max(0,ran-FloodcrownTune.WaveBand);
                front.MoveTo(origin+direction*back);
                foreach(var actor in PartyController.Current.members)
                {
                    if(!actor.Health.Alive||met.Contains(actor.Health)||!lane.Contains(actor.transform.position))continue;
                    float along=Vector3.Dot(Flat(actor.transform.position-origin),direction);
                    if(along<back-Telegraph.BodyAllowance||along>ran+Telegraph.BodyAllowance)continue;
                    met.Add(actor.Health);
                    // A wave is water: it breaks under whoever stands out of it.
                    if(Dry(actor.transform.position))continue;
                    if(actor.Health.Receive(Packet(FloodcrownTune.WaveDamage,DamageType.Kinetic))<=0)continue;
                    actor.Stagger(.3f);StartCoroutine(Shove(actor,direction,2.6f));
                }
                if(frame++%3==0)CombatVfx.Burst(origin+direction*ran+Vector3.up*.6f,FloodWater,"steam");
                var lead=PartyController.Current.Active;float ahead=Vector3.Dot(Flat(lead.transform.position-origin),direction)-ran;
                strikeAt=ahead>0&&!met.Contains(lead.Health)&&lane.Contains(lead.transform.position)?Time.time+ahead/FloodcrownTune.WaveSpeed:0;
                yield return null;
            }
            front.Cancel();lane.Release();strikeAt=0;Pose(0);Finish(FloodcrownRest);
        }
        /// <summary>Ground ahead that stands higher than it wades: a platform's flank, the rim. A charge that meets it is aground.</summary>
        bool RunsAground(Vector3 direction)
        {
            var zone=ZoneController.Current;if(zone!=null&&zone.Flight)return false;
            float ahead=GetComponent<CharacterController>().radius+1.2f;
            return CombatCover.Ground(transform.position+direction*ahead+Vector3.up*3).y-transform.position.y>FloodcrownTune.Grounds;
        }
        IEnumerator FloodCharge(int charges,bool dry)
        {
            var controller=GetComponent<CharacterController>();
            for(int n=0;n<charges;n++)
            {
                var target=PartyController.Current.Active.transform.position;var direction=Toward(target);transform.rotation=Quaternion.LookRotation(direction);
                float tell=dry?FloodcrownTune.ChargeTellDry:FloodcrownTune.ChargeTell,speed=dry?FloodcrownTune.ChargeSpeedDry:FloodcrownTune.ChargeSpeed;
                // A short way past where he stood, as the Burrower's: it grounds on a platform because he put one behind him.
                float length=Mathf.Clamp(Flat(target-transform.position).magnitude+6,10,24);
                var lane=Mark(Telegraph.Line(transform.position,direction,length,FloodcrownTune.ChargeWidth,tell,Telegraph.Heavy));
                strikeAt=Time.time+tell;
                yield return Windup(tell,-9,.2f,.05f);
                if(Interrupted)yield break;
                lane.Release();Pose(10);
                AudioManager.PlayAt(dry?"thrusterFire_000":"water_rush",transform,transform.position+Vector3.up,.24f,AudioBus.SFX,50,.04f,dry?.6f:1.4f);
                var hit=new HashSet<Health>();float travelled=0,elapsed=0,sincePuddle=FloodcrownTune.PuddleEvery*.5f;bool aground=false;
                Collide(false);
                while(travelled<length&&elapsed<length/speed+.4f&&!Interrupted)
                {
                    if(GameTime.Paused){yield return null;continue;}
                    elapsed+=Time.deltaTime;float step=speed*Time.deltaTime;
                    if(Rock(direction,step+.4f)||RunsAground(direction)){aground=true;break;}
                    var before=transform.position;if(controller.enabled)controller.Move(direction*step);
                    float moved=Flat(transform.position-before).magnitude;travelled+=moved;
                    Contact(hit,transform.position,FloodcrownTune.ChargeContact,FloodcrownTune.ChargeDamage,.45f,lane);
                    if(dry&&(sincePuddle+=moved)>=FloodcrownTune.PuddleEvery)
                    {
                        sincePuddle=0;
                        LingeringArea.Lay(CombatCover.Ground(transform.position+Vector3.up),FloodcrownTune.PuddleRadius,FloodcrownTune.PuddleSeconds,AreaEffect.Slow,FloodcrownTune.PuddlePace,0,health,DamageType.Kinetic,LingeringArea.Wet);
                    }
                    else if(!dry&&Time.frameCount%3==0)CombatVfx.Burst(transform.position+direction*2+Vector3.up*.5f,FloodWater,"steam");
                    yield return null;
                }
                strikeAt=0;Collide(true);Pose(0);
                if(Interrupted)yield break;
                if(aground){yield return FloodcrownOpens(FloodcrownTune.Aground,"FLOODCROWN_AGROUND",direction);yield break;}
                // Wading, the water it was pushing catches it up and breaks over it.
                if(!dry){yield return FloodcrownOpens(FloodcrownTune.Swamped,"FLOODCROWN_SWAMPED",direction);yield break;}
                if(n+1<charges)yield return new WaitForSeconds(.35f);
            }
            Finish(FloodcrownRest);
        }
        /// <summary>Its opening: the body is open as a wedged boss is, and for the same time every gate still standing can be hit.</summary>
        IEnumerator FloodcrownOpens(float seconds,string said,Vector3 direction)
        {
            gatesOpenUntil=Time.time+seconds;Tremor(.45f,.11f);
            AudioManager.PlayAt("impactMetal_000",transform,transform.position+Vector3.up*1.4f,.3f,AudioBus.SFX,60,.04f,.6f);
            for(int i=0;i<3;i++)CombatVfx.Burst(transform.position+direction*2+Vector3.up*(.6f+i*.6f),FloodWater,"kill");
            Pose(14,-.2f);Debug.Log(said);
            BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);
            yield return Opening(seconds,1.4f);Pose(0);
        }
        IEnumerator CrownStomp()
        {
            // Two markings from the start, so the safe band between them can be read before either lands: the slam at its feet, then the ring of floor further out.
            var slam=Mark(Telegraph.Circle(transform.position,FloodcrownTune.StompRadius,FloodcrownTune.StompTell,Telegraph.Heavy));
            var ring=Mark(Telegraph.Ring(transform.position,FloodcrownTune.RingInner,FloodcrownTune.RingOuter,FloodcrownTune.StompTell+FloodcrownTune.RingAfter,Telegraph.Threat));
            var marks=new List<Telegraph>{slam,ring};
            for(float t=0;t<FloodcrownTune.StompTell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/FloodcrownTune.StompTell));Pose(-8*k,.9f*k,.04f*k*k);ThreatFrom(marks);
                yield return null;
            }
            Pose(8);Strike(slam,FloodcrownTune.StompDamage,DamageType.Kinetic,.4f,2.4f);Tremor(.5f,.12f);
            AudioManager.PlayAt("explosionCrunch_000",transform,transform.position,.28f,AudioBus.SFX,60);
            for(float t=0;t<FloodcrownTune.RingAfter;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                ThreatFrom(marks);yield return null;
            }
            Strike(ring,FloodcrownTune.RingDamage,DamageType.Pulse,.3f);Tremor(.3f,.08f);
            AudioManager.PlayAt("explosionCrunch_000",transform,transform.position,.2f,AudioBus.SFX,60,.04f,1.3f);
            // Its crown has gone into the floor with the stomp.
            strikeAt=0;Pose(13,-.25f);Debug.Log("FLOODCROWN_LODGED");
            BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);
            yield return Opening(FloodcrownTune.Lodged,FloodcrownRest);Pose(0);
        }
    }
}
