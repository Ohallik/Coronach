using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Dessik Varn, chapter 5's fight, part two (on foot, in Many Hands' freight hall): the same hired frame that was Hulljack (<c>BossController.Hulljack.cs</c>), come down through the roof
    /// door with its four feet cut off and its canopy torn. It is one fight in two places, and part two is part one's consequences:
    /// <list type="bullet">
    /// <item><b>No feet</b>: it can't walk. It turns where it sits and moves only by its claw. <b>Claw</b>, a marked line: a hero caught is hauled in and slammed on a marked circle (roll
    /// it, or guard it and the claw clamps on the guard and yanks the frame open). Dodged, the claw bites whatever solid stands behind the hero (a stack, a crate, a wall) and the frame
    /// hauls itself there, down the lane it marked, and arrives winded: that is how it crosses the hall, and its opening. Bitten on nothing, it reels in empty, open a moment.</item>
    /// <item><b>The rings</b> that held the feet to the hull are free. <b>Magnet Pulse</b>: a marked disc round the frame, and every loose crate (<see cref="LooseCargo"/>) in reach dragged
    /// along the floor to it, each down its own marked lane, striking whoever stands in it; crates that slam into the frame jolt it open.</item>
    /// <item><b>The torch</b>: <b>Torch Lines</b>, a marked fan that stops at cover and leaves three burning lines on the deck for a few seconds.</item>
    /// <item><b>The canopy</b>: broken (its break meter), the frame kneels and its cockpit is the target, at twice a hit, until it rises.</item>
    /// <item><b>His temper</b> (under half): he starts throwing the cargo (<b>Cargo Throw</b>: crates onto marked circles at the party, each landing as cover), and the cabin alarm
    /// sounds: the breach the party cannot reach.</item>
    /// </list>
    /// At nothing he yields (<see cref="Ending.Yields"/>) and nothing barks over what he says. The hall's contract is in docs/maps/ManyHandsHull.md, "Freight hall contract".
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit DessikKit=Register(new Kit
        {
            id="DessikVarn",title="DESSIK VARN",epithet="Licensed claimant, in a hired suit",thresholds=new[]{.5f},
            choose=(boss,hero)=>boss.ChooseDessik(hero),
            moves={["Claw"]=boss=>boss.DessikClaw(),["TorchLines"]=boss=>boss.TorchLines(),["MagnetPulse"]=boss=>boss.MagnetPulse(),["CargoThrow"]=boss=>boss.CargoThrow()},
            signature=phase=>phase>=2?"CargoThrow":null,
            shift=boss=>boss.DessikLosesHisTemper(),shiftText="HE STARTS ON THE CARGO  ·  BRACE",
            status=boss=>boss.Kneeling?"THE SUIT KNEELS  ·  STRIKE THE COCKPIT":"",
            fightGoal=(boss,label)=>boss.alarmSounded?"Stop Dessik Varn: the cabins are open and you can't reach them":"Break the suit, then strike the cockpit",
            // He yields and speaks (DessikYields): the shared "boss" bark would land on top of it (manifest 8.10).
            capsuleRadius=1.8f,occlusion=true,ending=Ending.Yields,downBark="",
            // His one weak spot is the cockpit, and it is there only while the suit kneels: none of the shared spots, which would make the operator a target standing.
            weakSpots=System.Array.Empty<Vector3>(),showWeakSpot=(boss,open)=>boss.DessikKneels(open),
            start=boss=>boss.DessikStart(),update=boss=>boss.DessikUpdate(),down=boss=>boss.DessikDown(),
        });
        /// <summary>Dessik Varn's numbers, all of them (his integrity is his EnemyDef's: ManyHandsBuilder.Enemies). Damage is before the second phase's +18%.</summary>
        public static class DessikTune
        {
            /// <summary>Claw: the line and its tell, the bite, the haul in and the slam; the haul out (speed, how short of what it bit, what it strikes on the way, how far from its post it may
            /// go); and how long each way of missing leaves it open.</summary>
            public const float ClawLength=16,ClawWidth=2.2f,ClawTell=.8f,ClawCatch=70,ClawPull=26,ClawPullSeconds=.4f,ClawStops=1.2f,SlamRadius=2.6f,SlamTell=.45f,SlamDamage=76,SlamShove=2;
            public const float HaulSpeed=14,HaulStops=2,HaulContact=2,HaulDamage=70,Leash=13,Winded=1.6f,Clamped=1.6f,Empty=1.2f;
            /// <summary>Torch Lines: the fan, and the burning lines it leaves (how many, how far apart, how long and wide, for how long, and their burn a second).</summary>
            public const float TorchReach=9,TorchAngle=110,TorchTell=.95f,TorchDamage=76,BurnSpread=35,BurnLength=8,BurnWidth=1.3f,BurnSeconds=4,BurnTick=14;
            public const int BurnLines=3;
            /// <summary>Magnet Pulse: the tell, the disc and its damage, the crates' reach, speed and blow, and how long a crate slammed into the frame leaves it open.</summary>
            public const float PulseTell=1.1f,PulseRadius=5.5f,PulseDamage=72,PulseReach=22,CrateSpeed=14,CrateDamage=70,Jolted=1.5f;
            /// <summary>Cargo Throw: the time in the air, the landing circle and its blow, how many, and how near the other hero must be to get one.</summary>
            public const float ThrowTell=1.2f,ThrowRadius=2.6f,ThrowDamage=74,ThrowReach=20,ThrowArc=5;
            public const int Throws=2;
            /// <summary>The kneel: how far it pitches and sinks; where its cockpit is then (pitched forward past the front of its capsule, at a blade's height), how big a target and what a hit
            /// there counts; and how long a kneel brought about by hand (<see cref="Kit.showWeakSpot"/>) lasts.</summary>
            public const float KneelPitch=18,KneelSink=1.1f,CockpitHeight=1.4f,CockpitAhead=1.55f,CockpitRadius=.7f,CockpitTakes=2,KneelShown=3;
            /// <summary>The cabin alarm after his shift: when it first sounds, how often it sounds again, and when the party's line and the patrol's come after it.</summary>
            public const float AlarmAfter=4.4f,AlarmEvery=2.6f,BreachAfter=4.4f,LatchedAfter=11;
            public static float Rest(int phase)=>phase>=2?2.2f:2.8f;
        }
        static readonly Color ClawTone=new(1.3f,1.1f,.55f,.95f),BurnTone=new(1.5f,.6f,.2f,.9f);
        Vector3 dessikPost;float kneelUntil,alarmAt=-1,nextAlarm,breachSayAt=-1,latchedSayAt=-1;bool alarmSounded,hearsBreaks;GameObject cockpit;
        readonly List<LooseCargo> carried=new();
        /// <summary>Broken, the frame is down on its stumps and the cockpit is open.</summary>
        public bool Kneeling=>health!=null&&health.Alive&&Time.time<kneelUntil;
        /// <summary>Whether the cabin alarm has sounded in this fight.</summary>
        public bool AlarmSounded=>alarmSounded;
        float DessikRest=>DessikTune.Rest(Phase);
        void DessikStart()
        {
            dessikPost=transform.position;brain.Rooted=true;
            // Its weak spot is its cockpit, and only while it kneels, when the canopy torn in the roof door is pitched forward past the front of its body to where a blade reaches it.
            cockpit=new GameObject("Cockpit");cockpit.transform.SetParent(transform,false);cockpit.transform.localPosition=new Vector3(0,DessikTune.CockpitHeight,DessikTune.CockpitAhead);
            var shape=cockpit.AddComponent<SphereCollider>();shape.isTrigger=true;shape.radius=DessikTune.CockpitRadius;
            var hurt=cockpit.AddComponent<Hurtbox>();hurt.owner=health;hurt.multiplier=DessikTune.CockpitTakes;cockpit.SetActive(false);
            Health.Impacted+=DessikImpacted;hearsBreaks=true;
            alarmAt=breachSayAt=latchedSayAt=-1;alarmSounded=false;
        }
        void DessikImpacted(Health victim,DamagePacket packet,float damage,HitKind kind,bool broke,bool killed)
        {
            // A static event outlives a boss the scene took away mid-fight.
            if(this==null){Health.Impacted-=DessikImpacted;return;}
            if(victim!=health||!broke||killed||!health.Alive)return;
            kneelUntil=health.BrokenUntil;Debug.Log("DESSIK_KNEELS");
            BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);
        }
        void DessikUpdate()
        {
            brain.Rooted=true;
            bool kneel=Kneeling;if(cockpit!=null&&cockpit.activeSelf!=kneel)cockpit.SetActive(kneel);
            // Whatever a move was carrying when it was cut short goes down where it is: a crate never hangs in the air out of the paths.
            if(!busy&&carried.Count>0)SetDown();
            DessikAlarm();
            if(kneel){Pose(DessikTune.KneelPitch,-DessikTune.KneelSink,.02f);return;}
            if(busy||health.Broken||brain.Passive||brain.Telegraphing)return;
            // No feet: it sits on its stumps and rocks, the claw arm flexing.
            Pose(1.6f*Mathf.Sin(Time.time*1.7f),-.35f+.05f*Mathf.Sin(Time.time*3.4f));
        }
        void DessikAlarm()
        {
            if(alarmAt>=0&&Time.time>=alarmAt)
            {
                alarmAt=-1;alarmSounded=true;nextAlarm=Time.time;breachSayAt=Time.time+DessikTune.BreachAfter;latchedSayAt=Time.time+DessikTune.LatchedAfter;
                BarkService.Play("Keeper","hail.breach",true);Debug.Log("BREACH_ALARM");
            }
            if(alarmSounded&&Time.time>=nextAlarm)
            {
                nextAlarm=Time.time+DessikTune.AlarmEvery;var party=PartyController.Current;var at=party!=null&&party.Active!=null?party.Active.transform.position:transform.position;
                AudioManager.PlayAt("ui_error",null,at+Vector3.up*3,.2f,AudioBus.SFX,60,.05f,.62f);AudioManager.PlayAt("computerNoise_000",null,at+Vector3.up*3,.12f,AudioBus.SFX,60,.05f,.5f);
            }
            if(breachSayAt>=0&&Time.time>=breachSayAt){breachSayAt=-1;BarkService.Play(PartyController.Current.Active.character,"breach",true);}
            if(latchedSayAt>=0&&Time.time>=latchedSayAt){latchedSayAt=-1;BarkService.Play("Patrol","hail.latched",true);}
        }
        void DessikDown()
        {
            if(hearsBreaks){Health.Impacted-=DessikImpacted;hearsBreaks=false;}
            SetDown();LingeringArea.DismissAll();if(cockpit!=null)cockpit.SetActive(false);
            // The alarm is the cabins' and the patrol has them: it stops with the fight, which is what Taren hears (DessikYields).
            alarmAt=breachSayAt=latchedSayAt=-1;alarmSounded=false;
        }
        /// <summary>The kneel brought about by hand (or ended), as a break brings it: the cockpit out and the frame down on its stumps. For the shared tests' look at its weak spot.</summary>
        void DessikKneels(bool open)
        {
            kneelUntil=open?Time.time+DessikTune.KneelShown:0;if(cockpit!=null)cockpit.SetActive(open);
            Pose(open?DessikTune.KneelPitch:0,open?-DessikTune.KneelSink:0);Physics.SyncTransforms();
        }
        /// <summary>What his shift brings: the cargo, and a beat after his shift's own words, the cabin alarm.</summary>
        void DessikLosesHisTemper(){if(!alarmSounded&&alarmAt<0)alarmAt=Time.time+DessikTune.AlarmAfter;}
        /// <summary>Every crate a move took off the floor, set down where it is now.</summary>
        void SetDown()
        {
            foreach(var crate in carried.ToArray())if(crate!=null)Drop(crate,crate.transform.position);
            carried.Clear();
        }
        void Carry(LooseCargo crate){crate.Lift();if(!carried.Contains(crate))carried.Add(crate);}
        /// <summary>A crate set down on the floor at a place, with nobody left standing inside it: whoever is there is stood clear first (the Seedthane's pods), before it is solid.</summary>
        void Drop(LooseCargo crate,Vector3 spot)
        {
            carried.Remove(crate);var at=crate.Resting(spot);
            if(PartyController.Current!=null)foreach(var member in PartyController.Current.members)if(member.Health.Alive)SetClear(member,at);
            Physics.SyncTransforms();crate.Place(spot);
        }

        (string,IEnumerator) ChooseDessik(CombatActor hero)
        {
            float distance=Flat(hero.transform.position-transform.position).magnitude;bool near=distance<DessikTune.TorchReach+1;
            int crates=0;foreach(var crate in LooseCargo.All)if(crate!=null&&!crate.Moving&&Flat(crate.transform.position-transform.position).magnitude<DessikTune.PulseReach)crates++;
            // The claw is its way across the hall and its opening, and comes round whatever the range; the pulse is for cargo out on the floor; the throw is his temper.
            string move=Pick(("TorchLines",near?2f:.3f),("Claw",distance>4?1.8f:FullOffer),("MagnetPulse",crates>1?1.4f:distance<DessikTune.PulseRadius+1?1.1f:.2f),
                ("CargoThrow",Phase>=2?(distance>5?1.7f:.8f):0));
            return move switch{"Claw"=>(move,DessikClaw()),"MagnetPulse"=>(move,MagnetPulse()),"CargoThrow"=>(move,CargoThrow()),_=>(move,TorchLines())};
        }
        IEnumerator DessikClaw()
        {
            var party=PartyController.Current;var direction=Toward(party.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            float girth=GetComponent<CharacterController>().radius;var origin=transform.position+direction*(girth*.6f);
            var line=Mark(Telegraph.Line(origin,direction,DessikTune.ClawLength,DessikTune.ClawWidth,DessikTune.ClawTell,Telegraph.Heavy));
            strikeAt=Time.time+DessikTune.ClawTell;
            yield return Windup(DessikTune.ClawTell,6,0,.03f);
            if(Interrupted)yield break;
            line.Release();strikeAt=0;
            AudioManager.PlayAt("laserSmall_000",transform,transform.position+Vector3.up*1.5f,.26f,AudioBus.SFX,50,.04f,.45f);
            // The first hero along the line with nothing solid in the way.
            var sheltered=Sheltered();CombatActor hooked=null;float nearest=float.PositiveInfinity;
            foreach(var member in party.members)
            {
                if(!member.Health.Alive||!line.Contains(member.transform.position)||sheltered.Contains(member.Health))continue;
                float along=Vector3.Dot(Flat(member.transform.position-origin),direction);if(along<nearest){nearest=along;hooked=member;}
            }
            var cable=Cable(ClawTone);var hand=origin+Vector3.up*1.2f;
            if(hooked!=null&&hooked.Guarding)
            {
                // Guarded: the claw clamps on the guard and holds nothing; the frame, braced for a weight that didn't come, is yanked off its stumps.
                hooked.Health.Receive(Packet(DessikTune.ClawCatch,DamageType.Pulse));Debug.Log("DESSIK_CLAW guarded "+hooked.character);
                for(float t=0;t<.25f;){if(!GameTime.Paused)t+=Time.deltaTime;cable.SetPosition(0,hand);cable.SetPosition(1,hooked.transform.position+Vector3.up);yield return null;}
                temporary.Remove(cable.gameObject);Destroy(cable.gameObject);
                Pose(12,-.2f);yield return Opening(DessikTune.Clamped,DessikRest);Pose(0);yield break;
            }
            if(hooked!=null&&hooked.Health.Receive(Packet(DessikTune.ClawCatch,DamageType.Pulse))>0)
            {
                // Caught: hauled in to its front and slammed.
                Debug.Log("DESSIK_CLAW caught "+hooked.character);var body=hooked.GetComponent<CharacterController>();
                for(float t=0;t<DessikTune.ClawPullSeconds&&!Interrupted;)
                {
                    if(GameTime.Paused){yield return null;continue;}float dt=Time.deltaTime;t+=dt;
                    var toward=Flat(transform.position+direction*(girth+DessikTune.ClawStops)-hooked.transform.position);
                    if(body!=null&&body.enabled&&toward.magnitude>.2f)body.Move(toward.normalized*Mathf.Min(toward.magnitude,DessikTune.ClawPull*dt));
                    cable.SetPosition(0,hand);cable.SetPosition(1,hooked.transform.position+Vector3.up);yield return null;
                }
                temporary.Remove(cable.gameObject);Destroy(cable.gameObject);
                if(Interrupted)yield break;
                var slam=Mark(Telegraph.Circle(transform.position+direction*(girth+DessikTune.ClawStops),DessikTune.SlamRadius,DessikTune.SlamTell,Telegraph.Heavy));
                strikeAt=Time.time+DessikTune.SlamTell;
                yield return Windup(DessikTune.SlamTell,-12,.1f,.03f);
                if(Interrupted)yield break;
                Pose(10);Strike(slam,DessikTune.SlamDamage,DamageType.Kinetic,.4f,DessikTune.SlamShove);Tremor(.3f,.08f);
                AudioManager.PlayAt("impactMetal_000",transform,slam.Origin+Vector3.up,.3f,AudioBus.SFX,50,.04f,.7f);
                strikeAt=0;yield return Windup(.2f,0);Pose(0);Finish(DessikRest);yield break;
            }
            // Missed the hero: the claw flies on and bites the first solid thing in its line, and the frame hauls itself there.
            float reach=DessikTune.ClawLength;bool bit=CombatCover.Sweep(hand,hand+direction*reach,.3f,out var wall);if(bit)reach=Mathf.Max(0,wall.distance);
            // It hauls itself to just short of what it bit, and never ends more than its leash from where it was set: the hall's floor is the fight's.
            float haul=bit?reach-DessikTune.HaulStops:0;
            while(haul>0&&Flat(transform.position+direction*haul-dessikPost).magnitude>DessikTune.Leash)haul-=1;
            for(float t=0;t<.3f;){if(!GameTime.Paused)t+=Time.deltaTime;float k=Mathf.Clamp01(t/.3f);cable.SetPosition(0,hand);cable.SetPosition(1,hand+direction*reach*k);yield return null;}
            if(!bit||haul<1)
            {
                Debug.Log("DESSIK_CLAW empty");temporary.Remove(cable.gameObject);Destroy(cable.gameObject);
                Pose(9,-.1f);yield return Opening(DessikTune.Empty,DessikRest);Pose(0);yield break;
            }
            Debug.Log("DESSIK_CLAW hauled");
            var controller=GetComponent<CharacterController>();var hit=new HashSet<Health>();Collide(false);
            for(float moved=0;moved<haul&&!Interrupted;)
            {
                if(GameTime.Paused){yield return null;continue;}
                float step=Mathf.Min(DessikTune.HaulSpeed*Time.deltaTime,haul-moved);var before=transform.position;
                if(controller.enabled)controller.Move(direction*step);float went=Flat(transform.position-before).magnitude;moved+=Mathf.Max(went,step*.25f);
                Contact(hit,transform.position,DessikTune.HaulContact,DessikTune.HaulDamage,.3f,line);
                cable.SetPosition(0,transform.position+direction*(girth*.6f)+Vector3.up*1.2f);cable.SetPosition(1,hand+direction*reach);Pose(-8,.2f);
                if(went<step*.2f)break;
                yield return null;
            }
            Collide(true);temporary.Remove(cable.gameObject);Destroy(cable.gameObject);
            if(Interrupted)yield break;
            // Arrived on its stumps, winded.
            Tremor(.3f,.07f);AudioManager.PlayAt("impactMetal_000",transform,transform.position+Vector3.up,.28f,AudioBus.SFX,50,.04f,.6f);
            Pose(12,-.25f);
            yield return Opening(DessikTune.Winded,DessikRest);Pose(0);
        }
        IEnumerator TorchLines()
        {
            var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            var fan=Mark(Telegraph.Cone(transform.position,direction,DessikTune.TorchReach,DessikTune.TorchAngle,DessikTune.TorchTell,Telegraph.Heavy));
            strikeAt=Time.time+DessikTune.TorchTell;
            var rest=transform.rotation;AudioManager.PlayAt("thrusterFire_000",transform,transform.position+Vector3.up,.22f,AudioBus.SFX,50,.05f,1.3f);
            for(float t=0;t<DessikTune.TorchTell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted){transform.rotation=rest;yield break;}
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/DessikTune.TorchTell));Pose(-6*k,0,.03f*k*k);transform.rotation=rest*Quaternion.Euler(0,-50*k,0);
                yield return null;
            }
            transform.rotation=rest;
            Strike(fan,DessikTune.TorchDamage,DamageType.Plasma,.3f,0,Sheltered());
            AudioManager.PlayAt("laserSmall_000",transform,transform.position+Vector3.up,.26f,AudioBus.SFX,50,.04f,.6f);
            // What the torch cut burns where it cut: lines on the deck that stop at whatever stood in the flame's way.
            float girth=GetComponent<CharacterController>().radius;
            for(int i=0;i<DessikTune.BurnLines;i++)
            {
                float turn=DessikTune.BurnLines>1?(i/(DessikTune.BurnLines-1f)-.5f)*2*DessikTune.BurnSpread:0;var way=Quaternion.Euler(0,turn,0)*direction;
                var from=CombatCover.Ground(transform.position+way*girth+Vector3.up*2);float length=DessikTune.BurnLength;
                if(CombatCover.Sweep(from+Vector3.up*.6f,from+Vector3.up*.6f+way*length,.2f,out var cut))length=Mathf.Max(.5f,cut.distance);
                LingeringArea.LayLine(from,way,length,DessikTune.BurnWidth,DessikTune.BurnSeconds,AreaEffect.Burn,1,DessikTune.BurnTick,health,DamageType.Plasma);
                CombatVfx.Burst(from+way*(length*.5f)+Vector3.up*.3f,BurnTone,"kill");
            }
            for(float t=0;t<.25f&&!Interrupted;){if(!GameTime.Paused)t+=Time.deltaTime;Pose(6);yield return null;}
            Pose(0);strikeAt=0;Finish(DessikRest);
        }
        IEnumerator MagnetPulse()
        {
            float girth=GetComponent<CharacterController>().radius;
            var disc=Mark(Telegraph.Circle(transform.position,DessikTune.PulseRadius,DessikTune.PulseTell,Telegraph.Heavy));var marks=new List<Telegraph>{disc};
            // Every loose crate in reach, and the lane it will come down: from where it stands to the frame's side.
            var crates=new List<(LooseCargo crate,Telegraph lane,float length)>();
            foreach(var crate in LooseCargo.All.ToArray())
            {
                if(crate==null||crate.Moving||!crate.isActiveAndEnabled)continue;
                var gap=Flat(transform.position-crate.Middle);float distance=gap.magnitude;if(distance>DessikTune.PulseReach)continue;
                float length=distance-girth-crate.Half-.2f;if(length<.5f)continue;
                var lane=Mark(Telegraph.Line(crate.Middle-gap/distance*crate.Half,gap/distance,length+crate.Half*2,crate.Half*2+.4f,DessikTune.PulseTell,Telegraph.Threat));
                crates.Add((crate,lane,length));marks.Add(lane);
            }
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up,.3f,AudioBus.SFX,50,.05f,.55f);
            for(float t=0;t<DessikTune.PulseTell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/DessikTune.PulseTell));Pose(-4*k,.3f*k,.04f*k*k);ThreatFrom(marks);
                if(Time.frameCount%4==0)CombatVfx.Burst(transform.position+Vector3.up*.4f+Random.insideUnitSphere*1.6f,new Color(.45f,1.5f,1.6f,.9f),"steam");
                yield return null;
            }
            Strike(disc,DessikTune.PulseDamage,DamageType.Pulse,.3f,2.2f);Tremor(.35f,.08f);
            AudioManager.PlayAt("explosionCrunch_000",transform,transform.position+Vector3.up,.26f,AudioBus.SFX,50,.04f,.8f);
            Debug.Log($"DESSIK_PULSE {crates.Count}");
            // The crates come in, each down its lane, which stays drawn until its crate is home; a hero in a lane is struck by the crate that passes him, once a pulse; a crate that
            // meets something solid stops there, short of the frame.
            foreach(var (crate,lane,_) in crates){Carry(crate);lane.Steady=true;}
            var met=new HashSet<Health>();var travelled=new float[crates.Count];var stopped=new bool[crates.Count];int slammed=0;
            while(!Interrupted)
            {
                if(GameTime.Paused){yield return null;continue;}
                bool moving=false;
                for(int i=0;i<crates.Count;i++)
                {
                    var (crate,lane,length)=crates[i];if(stopped[i])continue;
                    float step=Mathf.Min(DessikTune.CrateSpeed*Time.deltaTime,length-travelled[i]);var middle=crate!=null?crate.Middle:Vector3.zero;
                    if(crate==null||CombatCover.Sweep(middle,middle+lane.Forward*(step+crate.Half),crate.Half*.8f,out _)){stopped[i]=true;lane.Release();continue;}
                    travelled[i]+=step;crate.Hold(crate.transform.position+lane.Forward*step);moving=true;
                    if(travelled[i]>=length-.01f){stopped[i]=true;slammed++;lane.Release();}
                    foreach(var member in PartyController.Current.members)
                    {
                        if(!member.Health.Alive||met.Contains(member.Health)||!lane.Contains(member.transform.position)||Flat(member.transform.position-crate.Middle).magnitude>crate.Half+.7f)continue;
                        met.Add(member.Health);if(member.Health.Receive(Packet(DessikTune.CrateDamage,DamageType.Kinetic))>0)member.Stagger(.3f);
                    }
                }
                // Until the last crate is home the lead is in danger only from what is still coming down a lane he stands in.
                strikeAt=0;var lead=PartyController.Current.Active;
                for(int i=0;i<crates.Count;i++)
                {
                    if(stopped[i]||met.Contains(lead.Health)||crates[i].crate==null||!crates[i].lane.Contains(lead.transform.position))continue;
                    float ahead=Vector3.Dot(Flat(lead.transform.position-crates[i].crate.Middle),crates[i].lane.Forward)-crates[i].crate.Half-.7f;
                    if(ahead>=0){float at=Time.time+ahead/DessikTune.CrateSpeed;strikeAt=strikeAt<=0?at:Mathf.Min(strikeAt,at);}
                }
                if(!moving)break;
                yield return null;
            }
            foreach(var (_,lane,_) in crates)if(lane!=null&&!lane.Released)lane.Release();
            foreach(var (crate,_,_) in crates)if(crate!=null)Drop(crate,crate.transform.position);
            if(Interrupted)yield break;
            Pose(0);strikeAt=0;
            if(slammed==0){Finish(DessikRest);yield break;}
            // Its own cargo, slammed into its frame: it rocks on its stumps, open.
            Tremor(.35f,.09f);AudioManager.PlayAt("impactMetal_000",transform,transform.position+Vector3.up,.3f,AudioBus.SFX,50,.04f,.55f);
            Pose(11,-.2f);Debug.Log($"DESSIK_JOLTED {slammed}");
            yield return Opening(DessikTune.Jolted,DessikRest);Pose(0);
        }
        IEnumerator CargoThrow()
        {
            var party=PartyController.Current;float girth=GetComponent<CharacterController>().radius;
            // At the hero it is fighting, and at the other if he is in reach.
            var targets=new List<Vector3>{party.Active.transform.position};
            foreach(var member in party.members)if(member!=party.Active&&member.Health.Alive&&Flat(member.transform.position-transform.position).magnitude<DessikTune.ThrowReach&&targets.Count<DessikTune.Throws)targets.Add(member.transform.position);
            var throws=new List<(LooseCargo crate,Vector3 from,Vector3 land,Vector3 to,Telegraph circle)>();
            foreach(var target in targets)
            {
                // The crate nearest the frame (what the pulse brought in), or, in a room with none to give, one torn off its own rack.
                var crate=LooseCargo.Nearest(transform.position,DessikTune.PulseReach);
                if(crate==null)crate=LooseCargo.Make(CombatCover.Ground(transform.position-transform.forward*(girth+1.2f)+Vector3.up*3));
                var to=CombatCover.Ground(target+Vector3.up*2);var circle=Mark(Telegraph.Circle(to,DessikTune.ThrowRadius,DessikTune.ThrowTell,Telegraph.Heavy));
                Carry(crate);throws.Add((crate,crate.transform.position,crate.Resting(to),to,circle));
            }
            var marks=new List<Telegraph>();foreach(var t in throws)marks.Add(t.circle);
            AudioManager.PlayAt("spaceEngineLow_000",transform,transform.position+Vector3.up,.26f,AudioBus.SFX,50,.05f,.8f);
            var over=transform.position+Vector3.up*4.5f;
            for(float t=0;t<DessikTune.ThrowTell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                float k=Mathf.Clamp01(t/DessikTune.ThrowTell);Pose(k<.3f?-10*k/.3f:8,.2f);ThreatFrom(marks);
                // Hauled up over the frame, then in an arc onto its circle.
                foreach(var (crate,from,land,_,_) in throws)
                {
                    if(crate==null)continue;float arc=Mathf.Clamp01((k-.3f)/.7f);
                    crate.Hold(k<.3f?Vector3.Lerp(from,over,k/.3f):Vector3.Lerp(over,land,arc)+Vector3.up*(Mathf.Sin(arc*Mathf.PI)*DessikTune.ThrowArc));
                }
                yield return null;
            }
            var once=new HashSet<Health>();
            foreach(var (crate,_,_,to,circle) in throws)
            {
                Strike(circle,DessikTune.ThrowDamage,DamageType.Kinetic,.35f,0,once);
                if(crate!=null)Drop(crate,to);
                CombatVfx.Burst(to+Vector3.up*.3f,new Color(.75f,.6f,.4f),"kill");
            }
            Tremor(.3f,.08f);AudioManager.PlayAt("impactMetal_000",null,throws.Count>0?throws[0].to:transform.position,.3f,AudioBus.SFX,50,.04f,.8f);
            Debug.Log($"DESSIK_THROW {throws.Count}");
            strikeAt=0;Pose(0);Finish(DessikRest);
        }
    }
}
