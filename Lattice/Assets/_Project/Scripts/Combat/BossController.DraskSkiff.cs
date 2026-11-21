using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The Warmaster's skiff, chapter 8's second boss, part two (in flight over Harrowgate's war harbour): beaten on the field, Drask Vaal takes his war-skiff and runs for the carrier, the
    /// Long Patience, "under Auntie, round the flak mole, up the slip". It is a pursuit: he holds station off one waypoint of his run (<see cref="PursuitLeg"/>) and fights there, and at each
    /// quarter of his bar he breaks off for the next. At the last he can run no further. Beaten, the skiff limps back toward the muster field and goes down there
    /// (<see cref="Ending.Leaves"/> toward its <see cref="BossExit"/>), which is where the town is waiting for him. The harbour's contract is in docs/maps/Harrowgate.md, "Pursuit contract".
    /// <list type="bullet">
    /// <item><b>Lance Run</b>: he lines up on the ship he is fighting and runs a marked lane through it at speed, striking what he touches; a crane's leg stops him. At its end he turns wide, open:
    /// cross his lane, never sit in it, and strike him on the turn.</item>
    /// <item><b>Gun Call</b>: "Harbour guns, mark them." Every battery that still answers (<see cref="HarbourBattery"/>) throws a shell onto a marked circle on the water at the party, fewer
    /// each call as the Unsworn take the guns: all of them, then two, then one.</item>
    /// <item><b>Wake</b>: a ship sitting on his tail is caught in a marked fan of drive flame behind him.</item>
    /// <item><b>Run</b>: at three quarters, at half (his shift: "He's cut for the carrier") and at a quarter, he runs a marked lane to the next waypoint, striking what is in it.</item>
    /// </list>
    /// It flies in the flight yard too: with no waypoints his runs go away from the party, and with no batteries the shells come from beyond the frame.
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit DraskSkiffKit=Register(new Kit
        {
            id="DraskSkiff",title="THE WARMASTER'S SKIFF",epithet="Running for the Long Patience",thresholds=new[]{.5f},
            choose=(boss,hero)=>boss.ChooseSkiff(hero),
            moves={["LanceRun"]=boss=>boss.SkiffLanceRun(),["GunCall"]=boss=>boss.SkiffGunCall(),["Wake"]=boss=>boss.SkiffWake(),["Run"]=boss=>boss.SkiffRun()},
            signature=phase=>phase>=2?"Run":null,
            shiftText="HE CUTS FOR THE CARRIER  ·  BRACE",
            status=boss=>boss.SkiffStatus(),
            fightGoal=(boss,label)=>boss.SkiffGoal(label),
            capsuleRadius=SkiffTune.Radius,occlusion=true,ending=Ending.Leaves,
            start=boss=>boss.SkiffStart(),update=boss=>boss.SkiffUpdate(),down=boss=>boss.SkiffDown(),
        });
        /// <summary>The skiff's numbers, all of them (its integrity is its EnemyDef's: HarrowgateBuilder.Bosses). Damage is before the second phase's +18%; a boss stays at threat 1. Every move
        /// is authored at 144 to 150, as the duel's (HarrowgateBuilder.Bosses' sizing): flying, a ship's bar and Plating are its hero's.</summary>
        public static class SkiffTune
        {
            /// <summary>Its capsule's radius in flight (5 m tall: its definition's); how far from its station it drifts before it closes on it again, and how fast.</summary>
            public const float Radius=2.4f,Station=10,Drift=6,Turn=90;
            /// <summary>Lance Run: the lane's shortest and longest past the ship and its width, the tell held and cutting for the carrier, the pace, what it strikes in passing, and how long it turns wide.</summary>
            public const float LanceShortest=18,LanceLongest=30,LancePast=10,LanceWidth=3.2f,LanceTell=1,LanceTellLater=.85f,LanceSpeed=30,LanceContact=2.4f,LanceDamage=150,TurnsWide=2;
            /// <summary>Gun Call: the shells' time in the air, the circle each lands on, the blow, and how far from the lead the shells that aren't aimed at a ship land.</summary>
            public const float GunTell=1.4f,GunRadius=4,GunDamage=146,GunSpread=7;
            /// <summary>The batteries that answer each call, by number (the first call all four, the second two and four, the third and after four alone); with none in the room, how many shells.</summary>
            public static readonly int[][] Answering={new[]{1,2,3,4},new[]{2,4},new[]{4}};
            public static readonly int[] YardShells={3,2,1};
            /// <summary>Wake: the fan behind it, the tell and the blow.</summary>
            public const float WakeReach=12,WakeAngle=70,WakeTell=.8f,WakeDamage=144;
            /// <summary>Run: the tell, the lane's width, the pace, what it strikes in passing, and how far it runs where there is no waypoint to run to.</summary>
            public const float RunTell=1.2f,RunWidth=5,RunSpeed=28,RunContact=2.4f,RunDamage=148,RunYard=26;
            /// <summary>The shares of its bar at which it runs for the next waypoint (the middle one is its shift).</summary>
            public static readonly float[] RunsAt={.75f,.5f,.25f};
            /// <summary>When the party's line comes after his call to the guns.</summary>
            public const float HarbourSaidAfter=1.6f;
            public static float Rest(int phase)=>phase>=2?2.2f:2.7f;
        }
        static readonly Color ShellTone=new(1.5f,.78f,.32f,.9f),SkiffDriveTone=new(1.5f,.55f,.25f,.9f);
        Vector3 skiffStation;int skiffLeg,skiffRuns,skiffCalls;bool skiffHarbourSaid;string skiffOpenWhy="";
        /// <summary>The waypoint it holds station off now (-1 in a room with none), the runs it has made and the gun calls.</summary>
        public int SkiffLeg=>skiffLeg;
        public int SkiffRuns=>skiffRuns;
        public int SkiffCalls=>skiffCalls;
        float SkiffRest=>SkiffTune.Rest(Phase);
        void SkiffStart()
        {
            var leg=PursuitLeg.Nearest(transform.position);
            skiffLeg=leg!=null&&Flat(leg.transform.position-transform.position).magnitude<SkiffTune.Station*2?leg.index:-1;
            skiffStation=transform.position;skiffRuns=skiffCalls=0;skiffHarbourSaid=false;brain.Rooted=true;
            var leaving=gameObject.AddComponent<BossDeparture>();leaving.sound="thrusterFire_000";leaving.speed=14;
        }
        void SkiffUpdate()
        {
            brain.Rooted=true;
            // At each quarter of its bar it runs for the next waypoint (the half is its shift, which promises the run itself).
            int owed=0;foreach(float at in SkiffTune.RunsAt)if(health.integrity<health.maximum*at)owed++;
            if(owed>skiffRuns&&queuedMove==null&&!shifting&&!busy)queuedMove="Run";
            if(busy||health.Broken||brain.Passive||brain.Telegraphing)return;
            // Holding station: it closes on its waypoint once it has drifted off, and keeps its bow on the ship it is fighting.
            var off=Flat(skiffStation-transform.position);var controller=GetComponent<CharacterController>();
            if(off.magnitude>SkiffTune.Station*.4f&&controller.enabled)controller.Move(off.normalized*Mathf.Min(off.magnitude,SkiffTune.Drift*Time.deltaTime));
            var party=PartyController.Current;
            if(party!=null&&party.Active!=null){var look=Quaternion.LookRotation(Toward(party.Active.transform.position));transform.rotation=Quaternion.RotateTowards(transform.rotation,look,SkiffTune.Turn*Time.deltaTime);}
            Pose(2*Mathf.Sin(Time.time*1.7f),.15f*Mathf.Sin(Time.time*2.3f));
        }
        void SkiffDown()
        {
            // Brought down: the main quest's step 13 is this, and the town is waiting on the field (docs/maps/Harrowgate.md).
            if(GameServices.Current!=null)GameServices.Current.Flags.SetBool("vask.pursuitWon",true);
            Debug.Log("SKIFF_DOWN");
        }
        string SkiffStatus()
        {
            if(health==null||!health.Alive)return "";
            if(health.Broken&&Time.time<openingUntil)return skiffOpenWhy+"  ·  STRIKE";
            if(CurrentMove=="Run")return "HE RUNS FOR THE CARRIER";
            return "";
        }
        string SkiffGoal(string label)
        {
            if(health==null||!health.Alive)return label;
            var next=PursuitLeg.After(skiffLeg);
            return next==null&&skiffLeg>=0?"Stop him at the slip: he can run no further":label;
        }
        IEnumerator SkiffOpens(float seconds,string why,string said)
        {
            skiffOpenWhy=why;Debug.Log(said);strikeAt=0;Pose(10,-.2f);
            BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);
            yield return Opening(seconds,SkiffRest);Pose(0);
        }

        (string,IEnumerator) ChooseSkiff(CombatActor hero)
        {
            var toHero=Flat(hero.transform.position-transform.position);float distance=toHero.magnitude;
            bool behind=distance>.5f&&Vector3.Angle(Flat(transform.forward),toHero)>120;
            // The lance run is its opening and comes round whatever the range; the wake is for a ship on its tail.
            string move=Pick(("LanceRun",distance>6?1.8f:FullOffer),("GunCall",distance>8?1.3f:.6f),("Wake",behind&&distance<SkiffTune.WakeReach?2.2f:0));
            return move switch{"GunCall"=>(move,SkiffGunCall()),"Wake"=>(move,SkiffWake()),_=>(move,SkiffLanceRun())};
        }
        /// <summary>The skiff flown down a lane it marked: over the plane, through whatever it meets (it strikes a ship it touches inside the lane, once), stopping at something solid in its way.</summary>
        IEnumerator SkiffFlies(Vector3 direction,float length,Telegraph lane,float speed,float contact,float damage)
        {
            var controller=GetComponent<CharacterController>();bool walked=controller.enabled;controller.enabled=false;Collide(false);
            var from=transform.position;var hit=new HashSet<Health>();
            for(float travelled=0;travelled<length&&!Interrupted;)
            {
                if(GameTime.Paused){yield return null;continue;}
                float step=Mathf.Min(speed*Time.deltaTime,length-travelled);
                // A crane's leg stops it: the harbour's cover.
                if(CombatCover.Sweep(transform.position,transform.position+direction*(step+SkiffTune.Radius),.6f,out _))break;
                travelled+=step;transform.position=from+direction*travelled;Pose(-6,.1f);
                Contact(hit,transform.position,contact,damage,.3f,lane);
                var lead=PartyController.Current.Active;float ahead=Vector3.Dot(Flat(lead.transform.position-transform.position),direction);
                strikeAt=!hit.Contains(lead.Health)&&lane.Contains(lead.transform.position)&&ahead>0?Time.time+ahead/speed:0;
                if(Time.frameCount%3==0)CombatVfx.Burst(transform.position-direction*3,SkiffDriveTone,"steam");
                yield return null;
            }
            controller.enabled=walked;Physics.SyncTransforms();Collide(true);strikeAt=0;
        }
        IEnumerator SkiffLanceRun()
        {
            var lead=PartyController.Current.Active.transform.position;var direction=Toward(lead);transform.rotation=Quaternion.LookRotation(direction);
            float length=Mathf.Clamp(Flat(lead-transform.position).magnitude+SkiffTune.LancePast,SkiffTune.LanceShortest,SkiffTune.LanceLongest),tell=Phase>=2?SkiffTune.LanceTellLater:SkiffTune.LanceTell;
            var lane=Mark(Telegraph.Line(transform.position,direction,length,SkiffTune.LanceWidth,tell,Telegraph.Heavy));
            strikeAt=Time.time+tell;
            AudioManager.PlayAt("spaceEngineLow_000",transform,transform.position,.26f,AudioBus.SFX,50,.05f,.8f);
            yield return Windup(tell,-8,0,.05f);
            if(Interrupted)yield break;
            lane.Steady=true;Debug.Log("SKIFF_LANCE");
            AudioManager.PlayAt("thrusterFire_000",transform,transform.position,.28f,AudioBus.SFX,50,.05f,.7f);
            yield return SkiffFlies(direction,length,lane,SkiffTune.LanceSpeed,SkiffTune.LanceContact,SkiffTune.LanceDamage);
            lane.Release();
            if(Interrupted)yield break;
            // The run is spent: it turns wide to come round, open.
            yield return SkiffOpens(SkiffTune.TurnsWide,"HE IS TURNING WIDE","SKIFF_TURNS_WIDE");
        }
        IEnumerator SkiffGunCall()
        {
            int call=++skiffCalls;var party=PartyController.Current;var lead=party.Active;
            BarkService.PlayAt("Drask","hail.harbour",Mathf.Clamp(call-1,0,2),true);
            if(!skiffHarbourSaid){skiffHarbourSaid=true;party.StartCoroutine(HarbourSaid(lead.character));}
            // Who still answers: by number, as he calls them; with no batteries in the room, a count.
            var answering=SkiffTune.Answering[Mathf.Clamp(call-1,0,SkiffTune.Answering.Length-1)];var from=new List<Vector3>();
            if(HarbourBattery.All.Count>0){foreach(int number in answering){var battery=HarbourBattery.Numbered(number);if(battery!=null)from.Add(battery.transform.position);}}
            else for(int i=0;i<SkiffTune.YardShells[Mathf.Clamp(call-1,0,SkiffTune.YardShells.Length-1)];i++)from.Add(transform.position+Quaternion.Euler(0,150+i*30,0)*Vector3.forward*60+Vector3.up*12);
            // The first shell at the ship he is fighting, the second at the other if she is near, the rest about the first.
            var circles=new List<Telegraph>();var shells=new List<(Transform shell,Vector3 from,Vector3 to)>();var partner=party.Partner;float y=transform.position.y;
            for(int i=0;i<from.Count;i++)
            {
                Vector3 to;
                if(i==0)to=lead.transform.position;
                else if(i==1&&partner!=null&&partner.Health.Alive&&Flat(partner.transform.position-lead.transform.position).magnitude<40)to=partner.transform.position;
                else to=lead.transform.position+Quaternion.Euler(0,i*137,0)*Vector3.forward*SkiffTune.GunSpread;
                to.y=y;circles.Add(Mark(Telegraph.Circle(to,SkiffTune.GunRadius,SkiffTune.GunTell,Telegraph.Heavy)));
                var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.name="Harbour shell";Destroy(shell.GetComponent<Collider>());shell.transform.localScale=Vector3.one*.7f;
                shell.GetComponent<Renderer>().sharedMaterial=ActorFactory.Energy(ShellTone);shell.transform.position=from[i];temporary.Add(shell);shells.Add((shell.transform,from[i],to));
            }
            Debug.Log($"SKIFF_GUNS {from.Count}");
            AudioManager.PlayAt("explosionCrunch_000",null,lead.transform.position+Vector3.up*6,.18f,AudioBus.SFX,80,.05f,1.6f);
            for(float t=0;t<SkiffTune.GunTell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                float k=Mathf.Clamp01(t/SkiffTune.GunTell);Pose(-4*Mathf.Sin(k*Mathf.PI));ThreatFrom(circles);
                foreach(var (shell,start,to) in shells)if(shell!=null)shell.position=Vector3.Lerp(start,to,k)+Vector3.up*(Mathf.Sin(k*Mathf.PI)*14);
                yield return null;
            }
            var once=new HashSet<Health>();
            foreach(var circle in circles){Strike(circle,SkiffTune.GunDamage,DamageType.Pulse,.25f,0,once);CombatVfx.Burst(circle.Origin+Vector3.up*.4f,ShellTone,"kill");}
            foreach(var (shell,_,_) in shells)if(shell!=null){temporary.Remove(shell.gameObject);Destroy(shell.gameObject);}
            Tremor(.3f,.07f);AudioManager.PlayAt("explosionCrunch_000",null,lead.transform.position,.26f,AudioBus.SFX,60,.05f,.9f);
            strikeAt=0;Pose(0);Finish(SkiffRest);
            static IEnumerator HarbourSaid(string hero){yield return new WaitForSeconds(SkiffTune.HarbourSaidAfter);BarkService.Play(hero,"harbour",true);}
        }
        IEnumerator SkiffWake()
        {
            var behind=-Flat(transform.forward).normalized;
            var fan=Mark(Telegraph.Cone(transform.position,behind,SkiffTune.WakeReach,SkiffTune.WakeAngle,SkiffTune.WakeTell,Telegraph.Threat));
            strikeAt=Time.time+SkiffTune.WakeTell;
            AudioManager.PlayAt("thrusterFire_000",transform,transform.position-behind*-2,.24f,AudioBus.SFX,50,.05f,1.2f);
            for(float t=0;t<SkiffTune.WakeTell;){if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;if(Time.frameCount%4==0)CombatVfx.Burst(transform.position+behind*3,SkiffDriveTone,"steam");Pose(4*Mathf.Clamp01(t/SkiffTune.WakeTell));yield return null;}
            Strike(fan,SkiffTune.WakeDamage,DamageType.Plasma,.25f);
            for(int i=1;i<=3;i++)CombatVfx.Burst(transform.position+behind*(SkiffTune.WakeReach*i/4f),SkiffDriveTone,"kill");
            strikeAt=0;Pose(0);Finish(SkiffRest);
        }
        /// <summary>It breaks off for the next waypoint of its run, down a marked lane, striking what is in it; there it holds station again. With none (the yard), away from the party.</summary>
        IEnumerator SkiffRun()
        {
            skiffRuns++;var next=PursuitLeg.After(skiffLeg);Vector3 to;
            if(next!=null){to=next.transform.position;to.y=transform.position.y;}
            else if(skiffLeg>=0&&PursuitLeg.All.Count>0){Debug.Log("SKIFF_RUN held");Finish(.6f);yield break;}
            else{var away=-Toward(PartyController.Current.Active.transform.position);to=transform.position+away*SkiffTune.RunYard;}
            var way=Flat(to-transform.position);float length=way.magnitude;var direction=length>.01f?way/length:transform.forward;
            transform.rotation=Quaternion.LookRotation(direction);
            var lane=Mark(Telegraph.Line(transform.position,direction,length,SkiffTune.RunWidth,SkiffTune.RunTell,Telegraph.Threat));
            // A ship in its lane is threatened as the skiff reaches it: what the partner and the replay driver read.
            var lead=PartyController.Current.Active.transform.position;float ahead=Vector3.Dot(Flat(lead-transform.position),direction);
            strikeAt=lane.Contains(lead)&&ahead>0?Time.time+SkiffTune.RunTell+ahead/SkiffTune.RunSpeed:0;
            Debug.Log($"SKIFF_RUN {(next!=null?next.index:-1)}");
            AudioManager.PlayAt("spaceEngineLow_000",transform,transform.position,.3f,AudioBus.SFX,60,.05f,.55f);
            yield return Windup(SkiffTune.RunTell,-6,0,.04f);
            if(Interrupted)yield break;
            lane.Steady=true;
            yield return SkiffFlies(direction,length,lane,SkiffTune.RunSpeed,SkiffTune.RunContact,SkiffTune.RunDamage);
            lane.Release();
            if(next!=null)skiffLeg=next.index;skiffStation=transform.position;
            if(Interrupted)yield break;
            Pose(0);Finish(SkiffRest*.7f);
        }
    }
}
