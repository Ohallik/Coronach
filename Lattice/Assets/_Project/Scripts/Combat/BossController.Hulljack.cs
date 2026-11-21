using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Hulljack, chapter 5's fight, part one (in flight over Many Hands' hull): Dessik Varn's hired salvage frame, four-footed, clamped across a join of the hull with its magnet rings and
    /// cutting at it. It is not killed: with its fourth foot cut it has nothing left to hold on by and goes down through the freight hall's roof door (<see cref="Ending.Leaves"/>, a
    /// <see cref="BossExit"/>), where the fight's second part (<c>BossController.DessikVarn.cs</c>) is the same frame with no feet. The hull is its: the contract is in
    /// docs/maps/ManyHandsHull.md, "Boss arena contract".
    /// <list type="bullet">
    /// <item><b>Clamped.</b> While any foot holds it to the hull a blow on the frame lands at <see cref="HulljackTune.Clamp"/>, read as resisted. Its feet are what to cut, and each stands
    /// shut until it is planted. <b>Plant</b>: it shifts its weight and stamps one foot down on a marked circle; that foot glows and stands exposed, and the whole frame is open, for
    /// <see cref="HulljackTune.PlantOpen"/> seconds. A foot cut is a quarter of its bar (its integrity is the fight's to say, a share of the frame's): the frame reels, open, then lets go
    /// and leaps to the next join (<see cref="HullJoin"/>) as its tug launches two claim haulers. The second foot carries it over half, its one phase shift, which promises the same
    /// leap. Battered over a quarter's line with the foot still on, the grip gives anyway and the most-cut foot tears off: the bar and the feet never disagree. One shift and not
    /// four keeps every blow inside a boss's 12 to 16% of the bar (CHAPTER_GUIDE 0.1): the phase step compounded three times put its last quarter at a fifth.</item>
    /// <item><b>Between plants</b>, the moves flying is good for. Torch Sweep, a marked fan of cutting flame. Grapple Shot, a marked line: the first ship in it is hooked and reeled toward
    /// the frame and crushed on a marked circle unless it rolls free; a grapple that hooks nothing whips back and leaves the frame open a moment. From the second join, Debris Fan:
    /// magnetised scrap flung down five marked lanes with gaps between them. All three stop at anything solid at the plane: the hull's booms and masts are the cover.</item>
    /// </list>
    /// It fights in the proving yard too, with no joins, booms or tug: its leaps go toward the party instead of to a join, and the haulers come from the south.
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit HulljackKit=Register(new Kit
        {
            id="Hulljack",title="HULLJACK",epithet="A salvage frame with a filed claim on your hull",thresholds=new[]{.5f},
            choose=(boss,hero)=>boss.ChooseHulljack(hero),
            moves={["TorchSweep"]=boss=>boss.HullTorch(),["GrappleShot"]=boss=>boss.GrappleShot(),["DebrisFan"]=boss=>boss.DebrisFan(),["Plant"]=boss=>boss.HullPlant(),["Leap"]=boss=>boss.HullLeap()},
            signature=phase=>phase>=2?"Leap":null,
            shift=boss=>boss.HullGripGives(),shiftText="IT LETS GO  ·  BRACE",
            status=boss=>boss.HulljackStatus(),
            fightGoal=(boss,label)=>$"Cut the feet off the salvage frame ({boss.FeetCut}/{HulljackTune.Feet})",
            // Its last foot's lines are the fight's last word: the shared "boss" bark would say the fight is over, and it isn't.
            capsuleRadius=2.4f,occlusion=true,ending=Ending.Leaves,downBark="",
            start=boss=>boss.HulljackStart(),update=boss=>boss.HulljackUpdate(),down=boss=>boss.HulljackDown(),
        });
        /// <summary>Hulljack's numbers, all of them (its integrity is its EnemyDef's: ManyHandsBuilder.Enemies). Damage is before the phases' +18% each; a boss stays at threat 1.</summary>
        public static class HulljackTune
        {
            /// <summary>The feet: how many, where they stand round its middle (at the plane), how big a target each is, each one's integrity and the bar it takes, as shares of the frame's.
            /// The bar is four quarters, one a foot; the fight's one phase shift is at half, the second foot.</summary>
            public const int Feet=4;
            public const float FootRing=3.3f,FootTurn=45,FootRadius=1.4f,FootIntegrity=.05f,FootBar=.25f;
            /// <summary>What of a blow on the frame lands while it is clamped to a join.</summary>
            public const float Clamp=.3f;
            /// <summary>Plant: the tell, the stamp's circle round the foot and its damage, how long the foot and the frame stand open, and the least a cut foot leaves it reeling.</summary>
            public const float PlantTell=.9f,PlantRadius=4.5f,PlantDamage=72,PlantOpen=5,Reels=3.5f;
            /// <summary>How long after the party's line Dessik's own comes over the radio, so each can be read.</summary>
            public const float DessikSays=1.9f;
            public const float TorchReach=18,TorchAngle=110,TorchTell=1,TorchDamage=74;
            /// <summary>Grapple Shot: the line and its tell, the hook's bite, the reel (pace, speed, how long, where it stops), the crush, and how long a hook on nothing leaves it open.</summary>
            public const float GrappleLength=26,GrappleWidth=2.4f,GrappleTell=.85f,GrappleCatch=70,ReelPace=.35f,ReelSpeed=8,ReelSeconds=2.4f,ReelStops=6,
                CrushRadius=3.4f,CrushTell=.55f,CrushDamage=76,GrappleMissed=1.4f;
            public const int DebrisLanes=5;
            public const float DebrisSpread=80,DebrisLength=26,DebrisWidth=1.8f,DebrisTell=.95f,DebrisDamage=70;
            /// <summary>Leap: the tell, the lane and the landing circle, its speed and arc, what it strikes in passing and landing, and how far it goes with no join to go to.</summary>
            public const float LeapTell=1.3f,LeapWidth=5,LeapLanding=7,LeapSpeed=26,LeapArc=5,LeapContact=3,LeapDamage=70,LandingDamage=76,LeapYard=24,LeapShortest=8;
            /// <summary>The claim haulers its tug launches at each leap: how many, the most alive at once, and how far from the frame toward the tug they come in.</summary>
            public const int Haulers=2,HaulersAlive=3;
            public const float HaulerOut=30;
            public static float Rest(int phase)=>phase>=2?2.4f:2.9f;
        }
        static readonly Color MagnetTone=new(.45f,1.5f,1.6f,.95f),TorchTone=new(1.5f,.75f,.3f,.9f);
        BossParts hullFeet;int hullJoin=-1,hullPlanted=-1,hullPlantPhase;float hullPlantedUntil;
        /// <summary>Feet off the frame, cut or torn: the count the objective line and the flags go by.</summary>
        public int FeetCut=>hullFeet==null?0:HulljackTune.Feet-hullFeet.Standing;
        /// <summary>The foot planted and open now, or -1.</summary>
        public int PlantedFoot=>hullPlanted;
        /// <summary>The join it is clamped to (its HullJoin index), or -1 in a room with none.</summary>
        public int AtJoin=>hullJoin;
        /// <summary>Held to a hull by its magnets: on a join, with a foot on, and not in the air between two. In a room with no hull (the proving yard) it has nothing to clamp to.</summary>
        public bool Clamped=>hullFeet!=null&&hullFeet.Standing>0&&hullJoin>=0&&!hullLeaping;
        bool hullLeaping,hullCallsTug;Vector3 hullLeapTo;int hullLeapJoin;
        public BossParts HullFeet=>hullFeet;
        float HulljackRest=>HulljackTune.Rest(Phase);
        string HulljackStatus()=>health==null||!health.Alive||hullFeet==null?"":hullPlanted>=0?"A FOOT IS PLANTED  ·  CUT IT":hullFeet.Standing>0?"CLAMPED TO THE HULL  ·  WAIT FOR A FOOT TO PLANT":"";
        void HulljackStart()
        {
            // The feet are made as it wakes, and shut: none of them can be cut from the approach, or before it plants one (chapter 3's gates could be broken from the door).
            hullFeet=gameObject.AddComponent<BossParts>();
            hullFeet.Build(health,"Magnetic foot",HulljackTune.Feet,HulljackTune.FootRing,0,HulljackTune.FootRadius,brain.definition.integrity*HulljackTune.FootIntegrity,HulljackTune.FootTurn,MagnetTone);
            hullFeet.Expose(false);
            for(int i=0;i<hullFeet.Parts.Count;i++){int index=i;hullFeet.Parts[i].Died+=(_,packet)=>HulljackFootCut(index,packet);}
            // Clamped to a join, the frame shrugs a blow off; open (a plant, a reel, a break) it takes it whole, as every opening does.
            health.Plate=_=>Clamped?HulljackTune.Clamp:1;
            var join=HullJoin.Nearest(transform.position);hullJoin=join!=null&&Flat(join.transform.position-transform.position).magnitude<6?join.index:-1;
            brain.Rooted=true;
            var leaving=gameObject.AddComponent<BossDeparture>();leaving.sound="impactMetal_000";leaving.speed=12;
        }
        void HulljackUpdate()
        {
            brain.Rooted=true;
            // A leap cut short (a break mid-air) lands where it was going: the frame never hangs between two joins with its capsule off.
            if(hullLeaping&&!busy)
            {
                hullLeaping=false;transform.position=hullLeapTo;hullJoin=hullLeapJoin;var capsule=GetComponent<CharacterController>();if(health.Alive)capsule.enabled=true;Physics.SyncTransforms();
            }
            if(hullPlanted>=0&&Time.time>=hullPlantedUntil){hullFeet.Expose(hullPlanted,false);hullPlanted=-1;}
            HullGripGives();
            if(busy||health.Broken||brain.Passive||brain.Telegraphing)return;
            // Clamped and cutting: the torch arm working at the seam, the frame rocking on its rings.
            Pose(2.2f*Mathf.Sin(Time.time*3.1f),.05f*Mathf.Sin(Time.time*6.2f));
        }
        void HulljackDown()
        {
            if(hullFeet!=null)hullFeet.Expose(false);hullPlanted=-1;
        }
        /// <summary>The flag the brief names for the nth foot off (manifest section 3: the fight's).</summary>
        static void FootFlag(int cut){if(GameServices.Current!=null&&cut>=1&&cut<=HulljackTune.Feet)GameServices.Current.Flags.SetBool("hands.feet"+cut,true);}
        /// <summary>Lines one after another on the bark plate, each held long enough to read: a beat later, on the party's clock (the frame may be gone by then).</summary>
        static void SayLater(string speaker,string situation,float seconds)
        {
            var party=PartyController.Current;if(party==null)return;
            party.StartCoroutine(Later());
            IEnumerator Later(){yield return new WaitForSeconds(seconds);BarkService.Play(speaker,situation,true);}
        }
        void HulljackFootCut(int index,DamagePacket packet)
        {
            if(health==null||!health.Alive||!isActiveAndEnabled)return;
            if(hullPlanted==index)hullPlanted=-1;
            int cut=FeetCut;FootFlag(cut);if(engaged<0)engaged=Time.realtimeSinceStartup;
            Debug.Log($"HULLJACK_FOOT_CUT {cut}");
            BarkService.Play(PartyController.Current.Active.character,"foot",true);SayLater("Dessik","foot",HulljackTune.DessikSays);
            Tremor(.45f,.11f);AudioManager.PlayAt("explosionCrunch_000",null,transform.position+Vector3.up,.3f,AudioBus.SFX,50,.05f,.75f);
            if(hullFeet.Standing==0)
            {
                // Nothing left to hold on by. The cut that took the last foot is the blow that beat it.
                health.InvulnerableUntil=0;health.Plate=null;
                health.Receive(new DamagePacket{source=packet.source,amount=health.maximum*100,type=health.weakness,tag="foot",weight=HitWeight.Heavy});
                return;
            }
            // A quarter of its bar goes with the foot, over the quarter's line that foot held and never past the next one.
            float max=health.maximum,line=Mathf.Max(0,1-cut*HulljackTune.FootBar)*max,next=Mathf.Max(0,1-(cut+1)*HulljackTune.FootBar)*max,after=health.integrity-max*HulljackTune.FootBar;
            if(health.integrity>line)health.integrity=Mathf.Max(next+1,Mathf.Min(after,line-1));
            // Then it lets go and leaps to the next join, calling the tug: after the reel, or after the shift if this foot carried it over half (which promises the same leap).
            hullCallsTug=true;queuedMove="Leap";
            // Whatever it was doing, it reels on three feet, open, before it lets go.
            StopAllCoroutines();Interrupt();busy=false;strikeAt=0;
            float open=Mathf.Max(health.BrokenUntil-Time.time,HulljackTune.Reels);OpenFor(open);nextSpecial=Time.time+open+.6f;
            StartCoroutine(HullReels(open));
        }
        IEnumerator HullReels(float seconds)
        {
            for(float t=0;t<seconds;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.35f))*Mathf.SmoothStep(0,1,Mathf.Clamp01((seconds-t)/.4f));
                Pose(12*k+3*Mathf.Sin(t*17)*k,-.25f*k);yield return null;
            }
            openingUntil=0;Pose(0);
        }
        /// <summary>The quarters of its bar already gone: one for each foot it should have lost.</summary>
        int QuartersGone{get{if(health==null||health.maximum<=0)return 0;float left=health.integrity/health.maximum;int gone=0;for(int k=1;k<HulljackTune.Feet;k++)if(left<1-k*HulljackTune.FootBar)gone++;return gone;}}
        /// <summary>Battered over a quarter's line on the frame itself with that quarter's foot still on (or set there), the grip gives: the most-cut foot tears off, so the feet and the bar
        /// never disagree, and it lets go and leaps as a cut would make it. Asked every frame, and by its shift.</summary>
        void HullGripGives()
        {
            if(hullFeet==null||health==null||!health.Alive)return;
            while(hullFeet.Standing>1&&hullFeet.Standing>HulljackTune.Feet-QuartersGone)
            {
                int weakest=-1;float least=float.PositiveInfinity;
                for(int i=0;i<hullFeet.Parts.Count;i++){var part=hullFeet.Parts[i];if(part!=null&&part.Alive&&part.integrity<least){least=part.integrity;weakest=i;}}
                if(weakest<0)break;hullFeet.Settle(weakest);if(hullPlanted==weakest)hullPlanted=-1;FootFlag(FeetCut);Debug.Log($"HULLJACK_FOOT_TORN {FeetCut}");
                hullCallsTug=true;if(!hullLeaping&&CurrentMove!="Leap")queuedMove="Leap";
            }
        }
        void LaunchHaulers()
        {
            var definition=GameCatalog.Find<EnemyDef>("ClaimHauler");if(definition==null)return;
            summoned.RemoveAll(s=>s==null||!s.Health.Alive);
            int launch=Mathf.Min(HulljackTune.Haulers,HulljackTune.HaulersAlive-summoned.Count);if(launch<=0)return;
            var toward=ClaimTug.From(transform.position);var across=Vector3.Cross(Vector3.up,toward);
            for(int i=0;i<launch;i++)
            {
                // In from the tug's side, a short flight from the frame and either side of the line to the tug, so the pair is seen coming.
                var point=transform.position+toward*HulljackTune.HaulerOut+across*((i-(launch-1)*.5f)*8);point.y=transform.position.y;
                var hauler=ActorFactory.Enemy(definition,point);summoned.Add(hauler);
                CombatVfx.Burst(point,MagnetTone,"kill");
            }
            Debug.Log("BOSS_SUMMON "+health.id+" "+summoned.Count);
        }
        /// <summary>Who has something solid at the plane between the frame and them: a boom or a mast stops its torch, its hook and its scrap, as a wall stops a bite.</summary>
        HashSet<Health> Sheltered()
        {
            bool flying=ZoneController.Current!=null&&ZoneController.Current.Flight;var from=transform.position+Vector3.up*(flying?0:1);var covered=new HashSet<Health>();
            foreach(var member in PartyController.Current.members)if(!CombatCover.Clear(from,member.transform.position+Vector3.up*(member.flight?0:.9f)))covered.Add(member.Health);
            return covered;
        }
        float FrameRadius=>GetComponent<CharacterController>().radius;

        (string,IEnumerator) ChooseHulljack(CombatActor hero)
        {
            float distance=Flat(hero.transform.position-transform.position).magnitude;bool near=distance<HulljackTune.TorchReach;
            // The plant is its opening and never waits long; the grapple is for a ship off its torch; the scrap from the second join on.
            string move=Pick(("TorchSweep",near?2f:.3f),("GrappleShot",distance>FrameRadius+3&&distance<HulljackTune.GrappleLength?1.6f:.3f),
                ("DebrisFan",FeetCut>=1?(distance>8?1.7f:.8f):0),("Plant",hullFeet!=null&&hullFeet.Standing>0?1.3f:0));
            return move switch{"GrappleShot"=>(move,GrappleShot()),"DebrisFan"=>(move,DebrisFan()),"Plant"=>(move,HullPlant()),_=>(move,HullTorch())};
        }
        IEnumerator HullPlant()
        {
            // The standing foot nearest the ship it's fighting: the one it shifts its weight onto.
            int foot=-1;float least=float.PositiveInfinity;var hero=PartyController.Current.Active.transform.position;
            if(hullFeet!=null)for(int i=0;i<hullFeet.Parts.Count;i++){var part=hullFeet.Parts[i];if(part==null||!part.Alive)continue;float gap=Flat(part.transform.position-hero).sqrMagnitude;if(gap<least){least=gap;foot=i;}}
            var at=foot>=0?hullFeet.Parts[foot].transform.position:transform.position;at.y=transform.position.y;
            var stamp=Mark(Telegraph.Circle(at,HulljackTune.PlantRadius,HulljackTune.PlantTell,Telegraph.Heavy));
            strikeAt=Time.time+HulljackTune.PlantTell;
            AudioManager.PlayAt("spaceEngineLow_000",transform,transform.position+Vector3.up,.24f,AudioBus.SFX,50,.05f,.7f);
            yield return Windup(HulljackTune.PlantTell,-5,.35f,.03f);
            if(Interrupted)yield break;
            Pose(6,-.1f);Strike(stamp,HulljackTune.PlantDamage,DamageType.Kinetic,.3f,2);Tremor(.3f,.08f);
            AudioManager.PlayAt("impactMetal_000",transform,at,.3f,AudioBus.SFX,50,.05f,.6f);CombatVfx.Burst(at+Vector3.up*.3f,MagnetTone,"kill");
            strikeAt=0;
            if(foot>=0){hullFeet.Expose(foot,true);hullPlanted=foot;hullPlantedUntil=Time.time+HulljackTune.PlantOpen;}
            Debug.Log($"HULLJACK_PLANT {foot}");
            // Said once a join: the second plant there is the same lesson.
            if(hullPlantPhase!=Phase){hullPlantPhase=Phase;BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);}
            yield return Opening(HulljackTune.PlantOpen,HulljackRest);Pose(0);
        }
        IEnumerator HullTorch()
        {
            var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            var fan=Mark(Telegraph.Cone(transform.position,direction,HulljackTune.TorchReach,HulljackTune.TorchAngle,HulljackTune.TorchTell,Telegraph.Threat));
            strikeAt=Time.time+HulljackTune.TorchTell;
            // The torch arm swings back over its shoulder as the fan fills, and comes round through it.
            var rest=transform.rotation;AudioManager.PlayAt("thrusterFire_000",transform,transform.position+Vector3.up,.22f,AudioBus.SFX,50,.05f,1.4f);
            for(float t=0;t<HulljackTune.TorchTell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted){transform.rotation=rest;yield break;}
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/HulljackTune.TorchTell));Pose(-6*k,0,.03f*k*k);transform.rotation=rest*Quaternion.Euler(0,-50*k,0);
                yield return null;
            }
            Strike(fan,HulljackTune.TorchDamage,DamageType.Plasma,.3f,0,Sheltered());
            AudioManager.PlayAt("laserSmall_000",transform,transform.position+Vector3.up,.26f,AudioBus.SFX,50,.04f,.55f);
            for(int i=-2;i<=2;i++)CombatVfx.Burst(transform.position+Quaternion.Euler(0,i*HulljackTune.TorchAngle*.2f,0)*direction*(HulljackTune.TorchReach*.6f),TorchTone,"kill");
            for(float t=0;t<.3f&&!Interrupted;){if(!GameTime.Paused)t+=Time.deltaTime;transform.rotation=rest*Quaternion.Euler(0,-50+110*Mathf.SmoothStep(0,1,t/.3f),0);Pose(6);yield return null;}
            transform.rotation=rest;Pose(0);strikeAt=0;Finish(HulljackRest);
        }
        LineRenderer Cable(Color tone)
        {
            var go=new GameObject("Grapple cable");temporary.Add(go);var cable=go.AddComponent<LineRenderer>();cable.positionCount=2;cable.useWorldSpace=true;cable.startWidth=.22f;cable.endWidth=.16f;
            cable.sharedMaterial=ActorFactory.Energy(tone);cable.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return cable;
        }
        IEnumerator GrappleShot()
        {
            var party=PartyController.Current;var direction=Toward(party.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            var origin=transform.position+direction*(FrameRadius*.5f);
            var line=Mark(Telegraph.Line(origin,direction,HulljackTune.GrappleLength,HulljackTune.GrappleWidth,HulljackTune.GrappleTell,Telegraph.Heavy));
            strikeAt=Time.time+HulljackTune.GrappleTell;
            yield return Windup(HulljackTune.GrappleTell,5,0,.03f);
            if(Interrupted)yield break;
            line.Release();strikeAt=0;
            // The first ship along the line with nothing solid in the way: the hook takes one.
            var sheltered=Sheltered();CombatActor hooked=null;float nearest=float.PositiveInfinity;
            foreach(var member in party.members)
            {
                if(!member.Health.Alive||!line.Contains(member.transform.position)||sheltered.Contains(member.Health))continue;
                float along=Vector3.Dot(Flat(member.transform.position-origin),direction);if(along<nearest){nearest=along;hooked=member;}
            }
            AudioManager.PlayAt("laserSmall_000",transform,transform.position+Vector3.up,.26f,AudioBus.SFX,50,.04f,.5f);
            if(hooked!=null&&hooked.Health.Receive(Packet(HulljackTune.GrappleCatch,DamageType.Pulse))<=0)hooked=null;
            var cable=Cable(MagnetTone);
            if(hooked==null)
            {
                // Hooked nothing: the cable whips out to its end (or the boom it met) and back, and the frame is thrown off its balance.
                float reach=HulljackTune.GrappleLength;if(CombatCover.Sweep(origin,origin+direction*reach,.3f,out var wall))reach=Mathf.Max(1,wall.distance);
                Debug.Log("HULLJACK_GRAPPLE missed");
                for(float t=0;t<.5f;){if(!GameTime.Paused)t+=Time.deltaTime;float k=Mathf.Sin(Mathf.Clamp01(t/.5f)*Mathf.PI);cable.SetPosition(0,origin);cable.SetPosition(1,origin+direction*reach*k);yield return null;}
                temporary.Remove(cable.gameObject);Destroy(cable.gameObject);
                Pose(9,-.1f);yield return Opening(HulljackTune.GrappleMissed,HulljackRest);Pose(0);yield break;
            }
            Debug.Log("HULLJACK_GRAPPLE caught "+hooked.character);
            // The reel: held and slowed, drawn toward the frame, until he rolls free or is in reach of the crush.
            bool free=false;var body=hooked.GetComponent<CharacterController>();
            for(float t=0;t<HulljackTune.ReelSeconds;)
            {
                if(GameTime.Paused){yield return null;continue;}
                float dt=Time.deltaTime;t+=dt;if(Interrupted)yield break;
                if(!hooked.Health.Alive||hooked.State==ActorState.Dodge){free=true;break;}
                hooked.Health.Hinder(HulljackTune.ReelPace);
                var gap=Flat(transform.position-hooked.transform.position);float distance=gap.magnitude;
                if(distance<=HulljackTune.ReelStops)break;
                if(body!=null&&body.enabled)body.Move(gap/distance*Mathf.Min(distance-HulljackTune.ReelStops,HulljackTune.ReelSpeed*dt));
                cable.SetPosition(0,origin);cable.SetPosition(1,hooked.transform.position);Pose(-4,.1f,.02f);
                yield return null;
            }
            temporary.Remove(cable.gameObject);Destroy(cable.gameObject);
            if(free){Debug.Log("HULLJACK_GRAPPLE rolled "+hooked.character);Pose(0);Finish(HulljackRest);yield break;}
            // The crush: a marked circle at its front, where the reel brought him.
            var at=transform.position+direction*(FrameRadius+1.2f);
            var crush=Mark(Telegraph.Circle(at,HulljackTune.CrushRadius,HulljackTune.CrushTell,Telegraph.Heavy));
            strikeAt=Time.time+HulljackTune.CrushTell;
            yield return Windup(HulljackTune.CrushTell,-10,.2f,.04f);
            if(Interrupted)yield break;
            Pose(9);Strike(crush,HulljackTune.CrushDamage,DamageType.Kinetic,.4f,2.5f);Tremor(.3f,.08f);
            AudioManager.PlayAt("impactMetal_000",transform,at,.3f,AudioBus.SFX,50,.04f,.7f);
            strikeAt=0;yield return Windup(.2f,0);Pose(0);Finish(HulljackRest);
        }
        IEnumerator DebrisFan()
        {
            var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            var lanes=new List<Telegraph>();var origin=transform.position+direction*(FrameRadius*.6f);
            for(int i=0;i<HulljackTune.DebrisLanes;i++)
            {
                float turn=HulljackTune.DebrisLanes>1?(i/(HulljackTune.DebrisLanes-1f)-.5f)*HulljackTune.DebrisSpread:0;
                lanes.Add(Mark(Telegraph.Line(origin,Quaternion.Euler(0,turn,0)*direction,HulljackTune.DebrisLength,HulljackTune.DebrisWidth,HulljackTune.DebrisTell,Telegraph.Threat)));
            }
            // The rings flare and the hull's loose scrap jumps to them: drawn in, then flung.
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up,.26f,AudioBus.SFX,50,.05f,.7f);
            for(float t=0;t<HulljackTune.DebrisTell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/HulljackTune.DebrisTell));Pose(-6*k,.25f*k,.03f*k*k);ThreatFrom(lanes);
                if(Time.frameCount%5==0)CombatVfx.Burst(transform.position+Random.insideUnitSphere*2.5f,MagnetTone,"steam");
                yield return null;
            }
            var once=Sheltered();var chunks=new List<Transform>();
            foreach(var lane in lanes)
            {
                Strike(lane,HulljackTune.DebrisDamage,DamageType.Kinetic,.25f,0,once);
                var chunk=GameObject.CreatePrimitive(PrimitiveType.Cube);chunk.name="Magnetised scrap";Destroy(chunk.GetComponent<Collider>());chunk.transform.localScale=new Vector3(.7f,.4f,.9f);
                chunk.GetComponent<Renderer>().sharedMaterial=ActorFactory.Energy(MagnetTone);chunk.transform.SetPositionAndRotation(lane.Origin,Quaternion.LookRotation(lane.Forward));temporary.Add(chunk);chunks.Add(chunk.transform);
            }
            AudioManager.PlayAt("explosionCrunch_000",transform,transform.position+Vector3.up,.24f,AudioBus.SFX,50,.04f,1.3f);Pose(7);strikeAt=0;
            // The scrap flies the lanes it marked, and is gone at their ends.
            for(float t=0;t<.45f;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;float k=Mathf.Clamp01(t/.45f);
                for(int i=0;i<chunks.Count;i++)if(chunks[i]!=null){chunks[i].position=lanes[i].Origin+lanes[i].Forward*(lanes[i].Length*k);chunks[i].Rotate(720*Time.deltaTime,0,0);}
                yield return null;
            }
            ClearTemporary();Pose(0);Finish(HulljackRest);
        }
        /// <summary>Where it leaps to: the next join in its order; with none (the proving yard, or past its last), toward the ship it is fighting, as far as the yard's leap.</summary>
        (Vector3 to,int join) LeapTarget()
        {
            var next=HullJoin.After(hullJoin);
            if(next!=null){var at=next.transform.position;at.y=transform.position.y;return(at,next.index);}
            var hero=PartyController.Current.Active.transform.position;var way=Flat(hero-transform.position);float gap=way.magnitude;
            var direction=gap>.01f?way/gap:transform.forward;float reach=Mathf.Clamp(gap,HulljackTune.LeapShortest,HulljackTune.LeapYard);
            return(transform.position+direction*reach,hullJoin);
        }
        IEnumerator HullLeap()
        {
            // A leap that follows a lost foot calls the tug as it goes; one started by hand (a test) doesn't.
            if(hullCallsTug){hullCallsTug=false;LaunchHaulers();}
            var (to,join)=LeapTarget();var from=transform.position;var way=Flat(to-from);float length=way.magnitude;var direction=length>.01f?way/length:transform.forward;
            transform.rotation=Quaternion.LookRotation(direction);
            var lane=Mark(Telegraph.Line(from,direction,length,HulljackTune.LeapWidth,HulljackTune.LeapTell,Telegraph.Threat));
            var landing=Mark(Telegraph.Circle(to,HulljackTune.LeapLanding,HulljackTune.LeapTell,Telegraph.Heavy));
            Debug.Log($"HULLJACK_LEAP {join}");
            AudioManager.PlayAt("spaceEngineLow_000",transform,transform.position+Vector3.up,.28f,AudioBus.SFX,50,.05f,.55f);
            // Who it threatens and when, measured from how far it has come: a ship on the lane as the frame's edge reaches it, a ship in the landing circle as it comes down.
            float Ahead(Vector3 at,float travelled)
            {
                if(lane.Contains(at))return Mathf.Max(0,Mathf.Clamp(Vector3.Dot(Flat(at-from),direction),0,length)-HulljackTune.LeapContact-travelled)/HulljackTune.LeapSpeed;
                return landing.Contains(at)?Mathf.Max(0,length-travelled)/HulljackTune.LeapSpeed:-1;
            }
            for(float t=0;t<HulljackTune.LeapTell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/HulljackTune.LeapTell));Pose(-8*k,-.3f*k,.05f*k*k);
                float ahead=Ahead(PartyController.Current.Active.transform.position,0);strikeAt=ahead<0?0:Time.time+(HulljackTune.LeapTell-t)+ahead;
                yield return null;
            }
            lane.Release();
            // The leap: over whatever lies between, so not through the capsule's own walk.
            var controller=GetComponent<CharacterController>();bool walked=controller.enabled;controller.enabled=false;Collide(false);hullLeaping=true;hullLeapTo=to;hullLeapJoin=join;
            var hit=new HashSet<Health>();
            for(float travelled=0;travelled<length&&!Interrupted;)
            {
                if(GameTime.Paused){yield return null;continue;}
                travelled=Mathf.Min(length,travelled+HulljackTune.LeapSpeed*Time.deltaTime);float k=travelled/Mathf.Max(.01f,length);
                transform.position=from+direction*travelled;Pose(-10+20*k,HulljackTune.LeapArc*Mathf.Sin(k*Mathf.PI));
                Contact(hit,transform.position,HulljackTune.LeapContact,HulljackTune.LeapDamage,.3f,lane);
                var lead=PartyController.Current.Active;float ahead=hit.Contains(lead.Health)?-1:Ahead(lead.transform.position,travelled);
                strikeAt=ahead<0?0:Time.time+ahead;
                yield return null;
            }
            controller.enabled=walked;Physics.SyncTransforms();Collide(true);hullLeaping=false;
            if(Interrupted)yield break;
            // Down, and clamped again: the landing circle, and none struck twice.
            Strike(landing,HulljackTune.LandingDamage,DamageType.Kinetic,.4f,2.5f,hit);Tremor(.5f,.12f);
            AudioManager.PlayAt("explosionCrunch_000",transform,to+Vector3.up,.3f,AudioBus.SFX,50,.05f,.65f);CombatVfx.Burst(to+Vector3.up*.4f,MagnetTone,"kill");
            hullJoin=join;strikeAt=0;Pose(0);Finish(HulljackRest);
        }
    }
}
