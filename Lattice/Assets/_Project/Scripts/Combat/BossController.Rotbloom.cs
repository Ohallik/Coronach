using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The Rotbloom, chapter 4's boss: a floating bloom colony the size of a house in the Drowned Waystation's great hall, a predator that learned Driftreed's lamps and fishes with lures
    /// on four stalks round the rim. It is killed (<see cref="Ending.Destroyed"/>): this one is not misunderstood. The hall is its: the room's contract is in docs/maps/DrownedWaystation.md.
    /// <list type="bullet">
    /// <item><b>Fishing</b> (to half), the hall flooded knee-deep. While a lure-stalk (<see cref="LureStalk"/>) stands its mouths are shut and a blow on its body lands at
    /// <see cref="RotbloomTune.Shell"/>. It teaches every move here. Spore Breath, a marked cone that leaves slowing clouds on the water (boundaries, not dangers). Root Lash, three marked
    /// lines that erupt under the water and strike only who stands in it: a plinth is dry. Lure Flash: the lure nearest the hero blazes, a stalk he stands at or its own crown, and the ring it
    /// marks is struck; roll through it, guard it, or hug the bloom. Drag: a pale cone of pull toward a marked mouth that snaps when the pull ends; a roll breaks the pull, and a snap that
    /// takes nobody leaves it gaping (an opening). Burn a stalk and one of its mouths gapes for <see cref="RotbloomTune.Gape"/> seconds; a stalk is a quarter of what holds it up and takes
    /// that much of its bar, and the second carries it over half.</item>
    /// <item><b>Grounded</b> (under half). The shift is the last pump running: the water goes, the stalks still standing go dark, it grounds where it is and seeds two rot-pods on the rim.
    /// On the bare floor, faster: Crack Lash, three lashes anywhere, the last of which lodges a root in the cracked floor and leaves it open; Thrash, a marked circle round it that shoves;
    /// the breath, quicker; the drag still.</item>
    /// </list>
    /// It fights without stalks or water too (the proving yard): then its mouths are never shut, every lash is wet, the flash is its own, and its phase turns on integrity alone.
    /// Afloat, it drifts within <see cref="RotbloomTune.Drift"/> of the hall's middle and works round rather than stand between the camera and the hero.
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit RotbloomKit=Register(new Kit
        {
            id="Rotbloom",title="THE ROTBLOOM",epithet="It learned Driftreed's lamps, and it is fishing",thresholds=new[]{.5f},
            choose=(boss,hero)=>boss.ChooseRotbloom(hero),
            moves={["SporeBreath"]=boss=>boss.SporeBreath(),["RootLash"]=boss=>boss.RootLash(),["LureFlash"]=boss=>boss.LureFlash(),["Drag"]=boss=>boss.Drag(),
                ["CrackLash"]=boss=>boss.CrackLash(),["Thrash"]=boss=>boss.Thrash()},
            signature=phase=>phase>=2?"CrackLash":null,
            shift=boss=>boss.RotbloomDrains(),shiftText="THE HALL DRAINS  ·  BRACE",
            status=boss=>boss.Fishing?boss.health.Broken?"A MOUTH GAPES  ·  STRIKE IT":"ITS MOUTHS ARE SHUT  ·  BURN A LURE-STALK":"",
            fightGoal=(boss,label)=>boss.Fishing?"Burn a lure-stalk to open one of its mouths":LureStalk.All.Count>0?"Kill the Rotbloom":label,
            // Its fall is followed by Sela's and Taren's own quiet words in the hall (SelaBloomDead): the shared "boss" bark would talk over them (manifest 8.8).
            capsuleRadius=3.4f,occlusion=true,ending=Ending.Destroyed,downBark="",
            start=boss=>boss.RotbloomStart(),update=boss=>boss.RotbloomUpdate(),down=boss=>boss.RotbloomDown(),
        });
        /// <summary>The Rotbloom's numbers, all of them (its integrity is its EnemyDef's: WendmireBuilder.Enemies). Damage is before the phase's +18%; a boss stays at threat 1.</summary>
        public static class RotbloomTune
        {
            /// <summary>A stalk's integrity and the bar it takes with it, as shares of the boss's; how many burned send the water out; how long a burned stalk's mouth gapes; what of a blow its
            /// shut mouths let through. A stalk costs what it takes: a quarter of what holds it up for a quarter of the bar. At a tenth each the two stalks were a bargain (a tenth of
            /// its integrity burned off a quarter of its bar): once the replay driver fought it as a person does, up a ramp, a stalk, the gaping mouth, a second stalk, 74,000 fell in
            /// 74 s for a Sync 14 party in Prime parts (RotbloomDriverTests, November 14), where its sheet asks two to five minutes.</summary>
            public const float StalkIntegrity=.25f,StalkBar=.25f,Gape=5,Shell=.3f;
            public const int StalksToDrain=2;
            /// <summary>How far from the hall's middle it drifts afloat, and how long the water takes to go.</summary>
            public const float Drift=6,DrainSeconds=3;
            /// <summary>Spore Breath: the cone, its tell afloat and grounded, its damage, and the clouds it leaves along its middle.</summary>
            public const float BreathReach=9,BreathAngle=70,BreathTell=.9f,BreathTellGrounded=.7f,BreathDamage=66,CloudRadius=2.4f,CloudSeconds=6,CloudPace=.55f;
            public const int BreathClouds=2;
            /// <summary>Root Lash: three lines under the water, at the hero and either side of him.</summary>
            public const float LashLength=16,LashWidth=1.8f,LashTell=1,LashSpread=24,LashDamage=68;
            public const int Lashes=3;
            /// <summary>Lure Flash: its tell and damage; how near a stalk a hero must be for that stalk to flash, and its ring; the ring round its own crown.</summary>
            public const float FlashTell=1.1f,FlashDamage=70,FlashStalkReach=7,FlashStalkInner=1,FlashStalkOuter=5.5f,FlashGap=1.2f,FlashOuter=12;
            /// <summary>Drag: the tell, the pull's length and pace, its cone, where the mouth is (past the body's edge) and how wide, the snap, and how long a snap on nothing gapes.</summary>
            public const float DragTell=.9f,DragPull=1.4f,DragReach=15,DragAngle=50,DragSpeed=4,MouthAhead=2.2f,MouthRadius=3,SnapDamage=72,DragMissed=1.8f;
            /// <summary>Crack Lash: the first tell and the next two, the lines, the damage, and how long its root is lodged after the third.</summary>
            public const float CrackTell=.75f,CrackNext=.55f,CrackLength=18,CrackWidth=2.2f,CrackDamage=60,Lodged=2.2f;
            public const int Cracks=3;
            /// <summary>Thrash: how far past its body the circle round it reaches, its tell, damage and shove.</summary>
            public const float ThrashReach=5,ThrashTell=.9f,ThrashDamage=64,ThrashShove=2.5f;
            /// <summary>The rot-pods its shift seeds: how far out on the rim, and how many.</summary>
            public const float SeedRing=12;
            public const int Seeds=2;
            public const float RestAfloat=3.2f,RestGrounded=2.4f;
        }
        /// <summary>A drag's pull is no blow: a pale boundary in the swamp's own green, never a danger's orange, red or purple (D217, D222).</summary>
        static readonly Color PullTone=new(.56f,.84f,.5f,.38f),LureLight=new(1.3f,1.15f,.55f,.9f),SporeTone=new(.7f,.95f,.6f,.8f);
        int stalksBurned;bool hearsStalks;float bloomRadius=3.4f;
        /// <summary>Afloat with a lure-stalk standing: its mouths are shut.</summary>
        public bool Fishing=>health!=null&&health.Alive&&Phase<2&&LureStalk.StandingCount>0;
        float RotbloomRest=>Phase>=2?RotbloomTune.RestGrounded:RotbloomTune.RestAfloat;
        void RotbloomStart()
        {
            bloomRadius=GetComponent<CharacterController>().radius;
            // The stalks are a quarter of its bar each, so their integrity is the fight's to say: a share of its own, burned by fire and drinking light as it does.
            float each=health.maximum*RotbloomTune.StalkIntegrity;stalksBurned=0;
            foreach(var stalk in LureStalk.All)
            {
                if(!stalk.Standing){stalksBurned++;continue;}
                var part=stalk.Health;part.maximum=part.integrity=each;part.weakness=DamageType.Plasma;part.resistance=DamageType.Beam;part.breakThreshold=1e7f;
            }
            LureStalk.Expose(true);LureStalk.Burned+=RotbloomStalkBurned;hearsStalks=true;
            // Shut mouths: a blow on its body lands at a share while it fishes. An opening (a gaping mouth, a lodged root) is a break, which the plate never touches.
            health.Plate=_=>Fishing?RotbloomTune.Shell:1;
        }
        void RotbloomUpdate()
        {
            // Grounded it is rooted where the water left it; afloat it keeps to the middle of its hall, whatever its walk toward the hero would make of that.
            brain.Rooted=Phase>=2;
            if(Phase<2&&brain.Controller.enabled)
            {
                var off=Flat(transform.position-brain.Home);float out_=off.magnitude;
                if(out_>RotbloomTune.Drift)brain.Controller.Move(-off/out_*(out_-RotbloomTune.Drift));
            }
            if(busy||health.Broken||brain.Passive||brain.Telegraphing)return;
            // Afloat it rides the water, turning slowly; grounded it heaves where it lies.
            if(Phase<2)Pose(2.5f*Mathf.Sin(Time.time*.9f),.12f*Mathf.Sin(Time.time*1.3f));else Pose(1.5f*Mathf.Sin(Time.time*2.2f),-.15f);
            if(Phase<2)WorkRoundTheCamera();
        }
        void RotbloomStalkBurned(LureStalk stalk)
        {
            // A static event outlives a boss the scene took away mid-fight.
            if(this==null){LureStalk.Burned-=RotbloomStalkBurned;return;}
            if(health==null||!health.Alive||!isActiveAndEnabled)return;
            stalksBurned++;Debug.Log($"ROTBLOOM_STALK_BURNED standing={LureStalk.StandingCount}");
            if(engaged<0)engaged=Time.realtimeSinceStartup;
            // Mid-shift the water is already going: the stalk is down and nothing else follows.
            if(shifting||HealthPhase>=2)return;
            bool drains=stalksBurned>=RotbloomTune.StalksToDrain;
            float line=health.maximum*(Thresholds.Length>0?Thresholds[0]:.5f),after=health.integrity-health.maximum*RotbloomTune.StalkBar;
            // A quarter of what holds it up goes with each stalk; only the second carries it over the line, and the shift into its second phase is the water going.
            if(health.integrity>line)health.integrity=Mathf.Max(1,drains?Mathf.Min(after,line-1):Mathf.Max(after,line+1));
            // Whatever it was doing, a mouth gapes where the lure fed it.
            StopAllCoroutines();Interrupt();busy=false;strikeAt=0;
            OpenFor(RotbloomTune.Gape);nextSpecial=Time.time+RotbloomTune.Gape+.6f;
            Tremor(.4f,.09f);AudioManager.PlayAt("root_groan",transform,transform.position+Vector3.up*2,.3f,AudioBus.SFX,50,.06f,.8f);
            BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);
            Debug.Log("ROTBLOOM_GAPES stalk");
            StartCoroutine(RotbloomGapes(RotbloomTune.Gape));
        }
        IEnumerator RotbloomGapes(float seconds)
        {
            for(float t=0;t<seconds;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.4f))*Mathf.SmoothStep(0,1,Mathf.Clamp01((seconds-t)/.5f));
                Pose(14*k,-.35f*k);yield return null;
            }
            openingUntil=0;Pose(0);
        }
        /// <summary>The last pump runs: the water goes, the stalks still standing go dark, it grounds, and it seeds the rim.</summary>
        void RotbloomDrains()
        {
            LureStalk.SettleAll();ArenaWater.SetLevel(0,RotbloomTune.DrainSeconds);brain.Rooted=true;
            AudioManager.PlayAt("water_rush",null,transform.position,.36f,AudioBus.SFX,40,.2f,.8f);Debug.Log("ROTBLOOM_DRAINS");
            var definition=GameCatalog.Find<EnemyDef>("RotPod");if(definition==null)return;
            summoned.RemoveAll(s=>s==null||!s.Health.Alive);
            // Between the plinths, the two places nearest the hero it is fighting: what he walks into next.
            var lead=PartyController.Current.Active.transform.position;var spots=new List<Vector3>();
            for(int i=0;i<4;i++)spots.Add(brain.Home+Quaternion.Euler(0,i*90,0)*Vector3.forward*RotbloomTune.SeedRing);
            spots.Sort((a,b)=>Flat(a-lead).sqrMagnitude.CompareTo(Flat(b-lead).sqrMagnitude));
            for(int i=0;i<RotbloomTune.Seeds&&i<spots.Count;i++)
            {
                var point=SummonPoint(spots[i]-transform.position);
                var pod=ActorFactory.Enemy(definition,point+Vector3.up*.05f);summoned.Add(pod);
                CombatVfx.Burst(point+Vector3.up*.6f,SporeTone,"kill");
            }
            Debug.Log("BOSS_SUMMON "+health.id+" "+summoned.Count);
        }
        void RotbloomDown()
        {
            if(hearsStalks){LureStalk.Burned-=RotbloomStalkBurned;hearsStalks=false;}
            LureStalk.SettleAll();LingeringArea.DismissAll();ArenaWater.SetLevel(0,RotbloomTune.DrainSeconds);
        }

        (string,IEnumerator) ChooseRotbloom(CombatActor hero)
        {
            var at=hero.transform.position;float distance=Flat(at-transform.position).magnitude;bool near=distance<bloomRadius+RotbloomTune.BreathReach-2;
            if(Phase<2)
            {
                // The flash defends the stalk he is at; the lash is for whoever wades; the drag is its one opening that isn't a stalk, and it never leaves it out for long.
                bool atStalk=LureStalk.Nearest(at,RotbloomTune.FlashStalkReach)!=null,wet=!Dry(at);
                string afloat=Pick(("SporeBreath",near?2f:.3f),("RootLash",wet?1.6f:.2f),("LureFlash",atStalk?2.2f:distance>bloomRadius+2?1f:.3f),("Drag",distance>bloomRadius+4?1.5f:FullOffer));
                return afloat switch{"RootLash"=>(afloat,RootLash()),"LureFlash"=>(afloat,LureFlash()),"Drag"=>(afloat,Drag()),_=>(afloat,SporeBreath())};
            }
            string grounded=Pick(("SporeBreath",near?1.6f:.4f),("CrackLash",1.8f),("Thrash",distance<bloomRadius+RotbloomTune.ThrashReach?1.8f:.2f),("Drag",distance>bloomRadius+5?1.4f:FullOffer));
            return grounded switch{"CrackLash"=>(grounded,CrackLash()),"Thrash"=>(grounded,Thrash()),"Drag"=>(grounded,Drag()),_=>(grounded,SporeBreath())};
        }
        IEnumerator SporeBreath()
        {
            var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            float tell=Phase>=2?RotbloomTune.BreathTellGrounded:RotbloomTune.BreathTell;
            var cone=Mark(Telegraph.Cone(transform.position,direction,RotbloomTune.BreathReach+bloomRadius,RotbloomTune.BreathAngle,tell,Telegraph.Threat));
            strikeAt=Time.time+tell;
            yield return Windup(tell,-8,.3f,.03f);
            if(Interrupted)yield break;
            Pose(6);Strike(cone,RotbloomTune.BreathDamage,DamageType.Pulse,.2f);
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up*2,.24f,AudioBus.SFX,50,.05f,.5f);
            // What it breathes settles on the water: slowing clouds along the cone's middle, boundaries and not dangers.
            for(int i=1;i<=RotbloomTune.BreathClouds;i++)
            {
                var at=CombatCover.Ground(transform.position+direction*(bloomRadius+RotbloomTune.BreathReach*i/(RotbloomTune.BreathClouds+1f))+Vector3.up*2);
                CombatVfx.Burst(at+Vector3.up*.6f,SporeTone,"steam");
                LingeringArea.Lay(at,RotbloomTune.CloudRadius,RotbloomTune.CloudSeconds,AreaEffect.Slow,RotbloomTune.CloudPace,0,health,DamageType.Pulse);
            }
            yield return Windup(.2f,0);
            Pose(0);Finish(RotbloomRest);
        }
        IEnumerator RootLash()
        {
            var hero=PartyController.Current.Active.transform.position;var aim=Toward(hero);transform.rotation=Quaternion.LookRotation(aim);
            var lines=new List<Telegraph>();
            for(int i=0;i<RotbloomTune.Lashes;i++)
            {
                float turn=(i-(RotbloomTune.Lashes-1)*.5f)*RotbloomTune.LashSpread;var direction=Quaternion.Euler(0,turn,0)*aim;
                lines.Add(Mark(Telegraph.Line(transform.position+direction*(bloomRadius*.8f),direction,RotbloomTune.LashLength,RotbloomTune.LashWidth,RotbloomTune.LashTell,Telegraph.Threat)));
            }
            AudioManager.PlayAt("root_groan",transform,transform.position,.24f,AudioBus.SFX,50,.05f,1.3f);
            for(float t=0;t<RotbloomTune.LashTell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/RotbloomTune.LashTell));Pose(-5*k,-.2f*k,.03f*k*k);
                // Only a hero in the water is under threat: one on a plinth reads none.
                ThreatFrom(lines);if(Dry(PartyController.Current.Active.transform.position))strikeAt=0;
                yield return null;
            }
            // Up through the water, all three at once, each hero struck once at most; whoever is out of the water is out of their reach.
            var once=new HashSet<Health>();
            foreach(var line in lines)
            {
                line.Release();
                foreach(var actor in PartyController.Current.members)
                {
                    if(!actor.Health.Alive||once.Contains(actor.Health)||!line.Contains(actor.transform.position)||Dry(actor.transform.position))continue;
                    once.Add(actor.Health);if(actor.Health.Receive(Packet(RotbloomTune.LashDamage,DamageType.Kinetic))>0)actor.Stagger(.3f);
                }
                CombatVfx.Burst(line.Origin+line.Forward*(line.Length*.5f)+Vector3.up*.4f,new Color(.55f,.7f,.6f),"kill");
            }
            Tremor(.3f,.07f);AudioManager.PlayAt("water_splash",null,hero,.3f,AudioBus.SFX,50,.08f,.7f);
            strikeAt=0;Pose(4);yield return Windup(.2f,0);Pose(0);Finish(RotbloomRest);
        }
        IEnumerator LureFlash()
        {
            var hero=PartyController.Current.Active.transform.position;var stalk=LureStalk.Nearest(hero,RotbloomTune.FlashStalkReach);
            // The lure nearest what it is fishing for: the stalk he has come to burn, or its own crown.
            Vector3 centre;float inner,outer;
            if(stalk!=null){centre=stalk.transform.position;inner=RotbloomTune.FlashStalkInner;outer=RotbloomTune.FlashStalkOuter;stalk.Flare(RotbloomTune.FlashTell+.3f);}
            else{centre=transform.position;inner=bloomRadius+RotbloomTune.FlashGap;outer=RotbloomTune.FlashOuter+bloomRadius;}
            var ring=Mark(Telegraph.Ring(centre,inner,outer,RotbloomTune.FlashTell,Telegraph.Threat));var marks=new List<Telegraph>{ring};
            AudioManager.PlayAt("forceField_000",stalk!=null?stalk.transform:transform,centre+Vector3.up*2,.24f,AudioBus.SFX,50,.05f,.8f);
            for(float t=0;t<RotbloomTune.FlashTell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/RotbloomTune.FlashTell));Pose(-5*k,.5f*k,.04f*k*k);ThreatFrom(marks);
                yield return null;
            }
            Pose(3);Strike(ring,RotbloomTune.FlashDamage,DamageType.Beam,.25f);
            for(int i=0;i<3;i++)CombatVfx.Burst(centre+Vector3.up*(1.5f+i*.8f),LureLight,"kill");
            AudioManager.PlayAt("laserSmall_000",null,centre+Vector3.up*2,.24f,AudioBus.SFX,50,.05f,.6f);
            strikeAt=0;yield return Windup(.2f,0);Pose(0);Finish(RotbloomRest);
        }
        IEnumerator Drag()
        {
            var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            float whole=RotbloomTune.DragTell+RotbloomTune.DragPull;var mouthAt=transform.position+direction*(bloomRadius+RotbloomTune.MouthAhead);
            // The reach of the pull (pale: it drags, it doesn't strike) and the mouth it drags into (the blow), both from the start, so where the pull ends is read before it begins.
            var reach=Mark(Telegraph.Cone(transform.position,direction,RotbloomTune.DragReach+bloomRadius,RotbloomTune.DragAngle,whole,PullTone));
            var mouth=Mark(Telegraph.Circle(mouthAt,RotbloomTune.MouthRadius,whole,Telegraph.Heavy));
            strikeAt=Time.time+whole;
            yield return Windup(RotbloomTune.DragTell,-6,.2f,.03f);
            if(Interrupted)yield break;
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up*2,.26f,AudioBus.SFX,50,.05f,.45f);
            // The pull: whoever is in its reach is drawn toward the mouth; a roll breaks its hold for the rest of this drag.
            var freed=new HashSet<CombatActor>();
            for(float t=0;t<RotbloomTune.DragPull;)
            {
                if(GameTime.Paused){yield return null;continue;}
                float dt=Time.deltaTime;t+=dt;if(Interrupted)yield break;
                foreach(var actor in PartyController.Current.members)
                {
                    if(!actor.Health.Alive||freed.Contains(actor))continue;
                    if(actor.State==ActorState.Dodge){freed.Add(actor);Debug.Log("ROTBLOOM_DRAG_BROKEN "+actor.character);continue;}
                    if(!reach.Contains(actor.transform.position))continue;
                    var way=Flat(mouthAt-actor.transform.position);float gap=way.magnitude;if(gap<.4f)continue;
                    if(actor.TryGetComponent<CharacterController>(out var feet)&&feet.enabled)feet.Move(way/gap*Mathf.Min(gap,RotbloomTune.DragSpeed*dt));
                }
                Pose(-6+2*Mathf.Sin(t*30),.2f);
                if(Time.frameCount%4==0)CombatVfx.Burst(mouthAt+direction*Random.Range(2f,8f)+Vector3.up*.6f,SporeTone,"steam");
                yield return null;
            }
            reach.Release();
            // The snap.
            Pose(9);mouth.Release();int took=0;
            foreach(var actor in PartyController.Current.members)
            {
                if(!actor.Health.Alive||!mouth.Contains(actor.transform.position))continue;
                if(actor.Health.Receive(Packet(RotbloomTune.SnapDamage,DamageType.Kinetic))>0){took++;actor.Stagger(.4f);}
            }
            AudioManager.PlayAt("explosionCrunch_000",transform,mouthAt+Vector3.up,.26f,AudioBus.SFX,50,.05f,1.4f);Tremor(.25f,.07f);strikeAt=0;
            if(took>0){yield return Windup(.2f,0);Pose(0);Finish(RotbloomRest);yield break;}
            // Its mouth closed on nothing: it gapes after what got away.
            Pose(12,-.2f);Debug.Log("ROTBLOOM_GAPES drag");
            // Its own words: the shared opening line is "Stalk's burned", which the lead said of a mouth that had only missed him, with every stalk standing (first whole chain, November 14).
            BarkService.Play(PartyController.Current.Active.character,BarkService.Line(PartyController.Current.Active.character,"bossmissed."+health.id)!=null?"bossmissed."+health.id:Situation("bossopening"),true);
            yield return Opening(RotbloomTune.DragMissed,RotbloomRest);Pose(0);
        }
        IEnumerator CrackLash()
        {
            for(int n=0;n<RotbloomTune.Cracks;n++)
            {
                var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
                float tell=n==0?RotbloomTune.CrackTell:RotbloomTune.CrackNext;bool last=n==RotbloomTune.Cracks-1;
                var line=Mark(Telegraph.Line(transform.position+direction*(bloomRadius*.7f),direction,RotbloomTune.CrackLength,RotbloomTune.CrackWidth,tell,last?Telegraph.Heavy:Telegraph.Threat));
                strikeAt=Time.time+tell;
                yield return Windup(tell,-7,.2f,.03f);
                if(Interrupted)yield break;
                Pose(9);Strike(line,RotbloomTune.CrackDamage,DamageType.Kinetic,.3f);Tremor(.3f,.08f);
                AudioManager.PlayAt("explosionCrunch_000",transform,line.Origin+line.Forward*4,.24f,AudioBus.SFX,50,.05f,last?.8f:1.1f);
                for(int i=1;i<=3;i++)CombatVfx.Burst(line.Origin+line.Forward*(line.Length*i/4f)+Vector3.up*.3f,new Color(.5f,.46f,.38f),"kill");
                yield return Windup(.14f,0);
                if(Interrupted)yield break;
            }
            // The last lash went into the floor and stuck there.
            strikeAt=0;Pose(14,-.3f);Debug.Log("ROTBLOOM_LODGED");
            BarkService.Play(PartyController.Current.Active.character,BarkService.Line(PartyController.Current.Active.character,"bossstuck."+health.id)!=null?"bossstuck."+health.id:Situation("bossopening"),true);
            yield return Opening(RotbloomTune.Lodged,RotbloomRest);Pose(0);
        }
        IEnumerator Thrash()
        {
            var circle=Mark(Telegraph.Circle(transform.position,RotbloomTune.ThrashReach+bloomRadius,RotbloomTune.ThrashTell,Telegraph.Heavy));
            strikeAt=Time.time+RotbloomTune.ThrashTell;
            yield return Windup(RotbloomTune.ThrashTell,-4,.4f,.08f);
            if(Interrupted)yield break;
            Pose(6);Strike(circle,RotbloomTune.ThrashDamage,DamageType.Kinetic,.35f,RotbloomTune.ThrashShove);Tremor(.4f,.1f);
            AudioManager.PlayAt("explosionCrunch_000",transform,transform.position,.28f,AudioBus.SFX,50,.05f,.9f);
            yield return Windup(.2f,0);
            Pose(0);Finish(RotbloomRest);
        }
    }
}
