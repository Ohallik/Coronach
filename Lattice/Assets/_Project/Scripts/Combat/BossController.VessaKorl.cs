using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Vessa Korl, the Muster's dig-master, holding the Bridle Foundry's casting gallery: chapter 8's elite, fought before the Slag Colossus. Hessa's sister and her opposite: Hessa came in
    /// close with a hook and a chain; Vessa weighs a fight from a lance's length and spends what it costs. Her thaw-lance (the dig's heat lance, drawn here: her body was made without it) is
    /// light and heat, so solid cover between her and a hero takes it.
    /// <list type="bullet">
    /// <item><b>Thaw Beam</b>: the lance levelled at the lead and a marked line 16 m long; the beam lances down it. Hot (her second half), it leaves the line burning.</item>
    /// <item><b>Heat Sweep</b>: a 110-degree fan to 9.5 m, marked whole; the beam crosses it edge to edge, always from her left to her right (the lance swings to its start through the tell),
    /// and strikes each hero as it passes him. Then the lance vents and she is open: the first half's window. Behind her, behind cover, or a roll into the beam as it comes.</item>
    /// <item><b>Pike Call</b> (her first half): "Pikes." Her own braced thrust down a marked line at the lead, and every pikeguard of hers within reach thrusts at him with her, each down its
    /// own marked line. With none left she holds the pike herself.</item>
    /// <item><b>At half her bar she falls back</b> (the shift, and its promise, <b>Fall Back</b>): she gives up the middle of the gallery, goes back to the gantry door to the heart (its
    /// <see cref="BossExit"/>; with none, straight away from the party) and holds it; she writes her squad off (the room's VessaFallsBack, on <see cref="FallingBackFlag"/>) and lays the
    /// floor's pour across the way to her. From there: <b>Pour Order</b>, three marked strips across the approach that burn after they land ("I've costed the floor"); the beam, hot; the
    /// sweep; and <b>Dead Weight</b>, the lance brought down overhead on a marked line, where it wedges and she is open while she wrenches it free: the second half's window.</item>
    /// </list>
    /// At nought she doesn't fall: she walks out by the gantry door to the heart (<see cref="Ending.Leaves"/>), and her squad, which is the encounter's own, fights on. Every number is
    /// <see cref="VessaTune"/>'s (her integrity, XP and loot are HarrowgateBuilder.Enemies'). The room: docs/maps/BridleFoundry.md, "Casting gallery contract".
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit VessaKit=Register(new Kit
        {
            id="VessaKorl",title="VESSA KORL",epithet="Dig-master of the Muster, counting the cost",thresholds=new[]{.5f},
            choose=(boss,hero)=>boss.ChooseVessa(hero),
            moves={["ThawBeam"]=boss=>boss.ThawBeam(),["HeatSweep"]=boss=>boss.HeatSweep(),["PikeCall"]=boss=>boss.PikeCall(),["FallBack"]=boss=>boss.FallBack(),
                ["PourOrder"]=boss=>boss.PourOrder(true),["DeadWeight"]=boss=>boss.DeadWeight()},
            signature=phase=>phase>=2?"FallBack":null,
            shift=boss=>boss.VessaFallsBack(),shiftText="SHE'S FALLING BACK  ·  BRACE",
            status=boss=>boss.Phase>=2?"THE LANCE RUNS HOT":"",
            fightGoal=(boss,label)=>boss.Phase>=2?"Drive Vessa Korl out of the gallery":"Take the casting gallery from Vessa Korl",
            // A person: on a rig's 1.4 m capsule the party couldn't get inside her lance.
            capsuleRadius=VessaTune.Radius,occlusion=true,ending=Ending.Leaves,downBark="",
            // Her two flank seams and the lance's heat pack on her back, each standing proud of her .8 m body: a blade at her side or behind her reaches them.
            weakSpots=new[]{new Vector3(-.95f,1.3f,0),new Vector3(.95f,1.3f,0),new Vector3(0,1.3f,-.95f)},
            start=boss=>boss.VessaStart(),update=boss=>boss.VessaUpdate(),down=boss=>boss.VessaDown(),
        });
        /// <summary>Vessa's numbers, all of them. She is met at Sync 22 to 23 (HarrowgateBuilder.Enemies' ledger), so each move is held to 12 to 16% of a bar there, in both halves, in the
        /// Tempered parts chapter 8 arrives in: Taren or Sela at 22 pay the most (bar 498, a hit landing at .5556: 16% allows 143), Idra at 23 the least (bar 516, at .4762: 12% needs 130).
        /// Her moves are 132 to 142 and her second half hits no harder (<see cref="HotBonus"/> is nought, where every other boss takes the shared +18%): what changes at her shift is where
        /// she stands and the floor, which burns. Measured through Health.Receive (HarrowgateCombatTests), the same in both halves:
        /// <code>
        ///                  Taren/Sela 22  Idra 22  Taren/Sela 23  Idra 23
        ///   her jab   132      14.7%       12.8%      14.1%         12.2%
        ///   Pour      134      15.0%       13.0%      14.3%         12.4%
        ///   Beam      136      15.2%       13.2%      14.5%         12.6%
        ///   Sweep     136      15.2%       13.2%      14.5%         12.6%
        ///   Pike Call 140      15.6%       13.6%      14.9%         12.9%   (first half only)
        ///   Dead Wt.  142      15.8%       13.8%      15.1%         13.1%   (second half only)
        ///   the road  116      12.9%       11.3%      12.4%         10.7%   (a Pikeguard's thrust, the heaviest before her)
        /// </code></summary>
        public static class VessaTune
        {
            public const float Radius=.8f;
            /// <summary>What her second half adds to every blow: nothing. The shared +18% (PhaseStep) would carry Dead Weight to 18.7% of Taren's bar, over an elite's 16.</summary>
            public const float HotBonus=0;
            public const float BeamLength=16,BeamWidth=1.8f,BeamTell=.85f,BeamDamage=136;
            public const float SweepReach=9.5f,SweepArc=110,SweepTell=1.1f,SweepCross=.7f,SweepDamage=136,Vents=1.8f;
            public const float PikeLength=11,PikeWidth=2,PikeTell=1,PikeDamage=140,PikeReach=16;
            public const float WeightLength=7,WeightWidth=2.4f,WeightTell=1.1f,WeightDamage=142,Wedged=2.2f;
            public const float PourTell=1.1f,PourDamage=134,PourLength=14,PourWidth=2.2f,PourNear=4;
            public const int Pours=3;
            /// <summary>What her hot beam and her pour leave burning: seconds, and a Burn's damage a second.</summary>
            public const float Burning=4.5f,BurnTick=36;
            /// <summary>Falling back: how far before her way out she holds, how far she goes with no way out in the room, how fast, and how long she waits for her talk to start.</summary>
            public const float PostBeforeExit=5,RetreatWithNoExit=8,RetreatSpeed=7,TalkGrace=2.8f;
            public const float RestPlanted=2.6f,RestHot=2.1f;
        }
        /// <summary>The flag her fall back sets, which the room's AfterTalk plays VessaFallsBack on, with her squad still standing.</summary>
        public const string FallingBackFlag="vask.vessaFallsBack";
        /// <summary>That talk heard (the room's AfterTalk's heard flag); and the main quest's step, "Take the casting gallery from Vessa Korl", done: set as she leaves at nought.</summary>
        public const string FallBackHeardFlag="vask.vessaFellBack",BeatenFlag="vask.vessaBeaten";
        float VessaRest=>Phase>=2?VessaTune.RestHot:VessaTune.RestPlanted;
        Transform lance;Renderer lanceHead;LineRenderer vessaBeam;bool vessaHailed;Vector3 vessaPost;bool vessaHolding;
        static readonly Color LanceCold=new(1.35f,.95f,.6f,.95f),LanceHot=new(1.9f,1.6f,1.2f,1);
        void VessaStart()
        {
            // D419: a person beaten, not a machine: she walks out in her own colours, as her sister yields in hers (Hessa, s15/158).
            var leaving=gameObject.AddComponent<BossDeparture>();leaving.sound="";leaving.outSound="doorOpen_000";leaving.speed=6;leaving.keepsColours=true;
            DrawLance();
        }
        /// <summary>At nought: the floor she laid goes with her (nothing of hers burns on behind her into the fight with her squad); and the gallery is taken now, not at her fall
        /// back. The story's step moved on with her fall-back talk, a third of her bar still to go (s48/221): it is set here instead, and the talk owed at half is marked heard, so it
        /// is never played after she has gone. She walks out on her feet by the shared path (a body with a BossDeparture is not laid down by its driver: D341), where her own driver
        /// once put her down and she was slid out lying, one leg in the air (s48/232).</summary>
        [SetsFlag("vask.vessaBeaten")][SetsFlag("vask.vessaFellBack")]
        void VessaDown()
        {
            foreach(var strip in vessaStrips)if(strip!=null)strip.Dismiss();vessaStrips.Clear();
            var flags=GameServices.Current!=null?GameServices.Current.Flags:null;if(flags==null)return;
            flags.SetBool(FallBackHeardFlag,true);
            if(!flags.GetBool(BeatenFlag)){flags.SetBool(BeatenFlag,true);Lattice.Rpg.RpgServices.Quests?.Report(ObjectiveKind.Flag,BeatenFlag);}
            Debug.Log("VESSA_LEAVES walking");
        }
        readonly List<LingeringArea> vessaStrips=new();
        void VessaUpdate()
        {
            // Holding the gantry door: she turns where she stands and lets the floor come to her.
            brain.Rooted=vessaHolding;
            // Her own second half's weight in place of the shared phase step (VessaTune.HotBonus): Packet, her jab and what she leaves burning all read the brain's scale.
            brain.DamageScale=1+(Phase-1)*VessaTune.HotBonus;
            if(lanceHead!=null)lanceHead.sharedMaterial=ActorFactory.Energy(Phase>=2?LanceHot:LanceCold);
        }
        /// <summary>The thaw-lance, which her body was made without: a dark shaft and a glowing head, in her right hand if she has one, levelled ahead.</summary>
        void DrawLance()
        {
            var rig=GetComponentInChildren<Animator>();var hand=rig!=null&&rig.isHuman?rig.GetBoneTransform(HumanBodyBones.RightHand):null;
            var root=new GameObject("Thaw-lance").transform;root.SetParent(hand!=null?hand:transform,false);
            if(hand==null)root.localPosition=new Vector3(.45f,1.25f,.2f);
            root.rotation=Quaternion.LookRotation(transform.forward+Vector3.down*.15f);lance=root;
            var shaft=GameObject.CreatePrimitive(PrimitiveType.Cylinder);shaft.name="Shaft";Destroy(shaft.GetComponent<Collider>());shaft.transform.SetParent(root,false);
            // A cylinder stands along y: laid along the lance's forward, 2.8 m, the grip a third of the way up.
            shaft.transform.localRotation=Quaternion.Euler(90,0,0);shaft.transform.localPosition=Vector3.forward*.45f;shaft.transform.localScale=new Vector3(.07f,1.4f,.07f);
            var iron=new Material(Resources.Load<Material>("Blockout/Enemy"));iron.SetColor("_BaseColor",new Color(.2f,.19f,.19f));shaft.GetComponent<Renderer>().sharedMaterial=iron;
            var head=GameObject.CreatePrimitive(PrimitiveType.Capsule);head.name="Heat head";Destroy(head.GetComponent<Collider>());head.transform.SetParent(root,false);
            head.transform.localRotation=Quaternion.Euler(90,0,0);head.transform.localPosition=Vector3.forward*1.95f;head.transform.localScale=new Vector3(.16f,.22f,.16f);
            lanceHead=head.GetComponent<Renderer>();lanceHead.sharedMaterial=ActorFactory.Energy(LanceCold);
            foreach(var part in new[]{shaft.GetComponent<Renderer>(),lanceHead}){part.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
        }
        /// <summary>Her lines on the gallery's tannoy: the first as she opens, the second as she lays the floor.</summary>
        void Hail(int index){if(index==0&&vessaHailed)return;vessaHailed=true;BarkService.PlayAt("Vessa","hail.gallery",index,true);}

        (string,IEnumerator) ChooseVessa(CombatActor hero)
        {
            float distance=Flat(hero.transform.position-transform.position).magnitude;
            if(Phase<2)
            {
                // The sweep is the first half's one opening that isn't a stagger: light where the fan doesn't reach, and never left out for long.
                string move=Pick(("ThawBeam",distance>5?2f:.6f),("HeatSweep",distance<VessaTune.SweepReach?1.8f:FullOffer),("PikeCall",distance>3?1.4f:.6f));
                return move switch{"HeatSweep"=>(move,HeatSweep()),"PikeCall"=>(move,PikeCall()),_=>(move,ThawBeam())};
            }
            string hot=Pick(("PourOrder",distance>6?2f:.8f),("ThawBeam",distance>5?1.6f:.5f),("DeadWeight",distance<VessaTune.WeightLength+1?2.2f:FullOffer),("HeatSweep",distance<VessaTune.SweepReach?1f:.3f));
            return hot switch{"PourOrder"=>(hot,PourOrder(false)),"DeadWeight"=>(hot,DeadWeight()),"HeatSweep"=>(hot,HeatSweep()),_=>(hot,ThawBeam())};
        }
        /// <summary>The lance's aim, laid along the ground at a pitch: levelled at a hero, raised, brought down.</summary>
        void Level(Vector3 direction,float pitch=0){if(lance!=null)lance.rotation=Quaternion.LookRotation(Quaternion.AngleAxis(pitch,Vector3.Cross(direction,Vector3.up))*direction+Vector3.down*.05f);}

        IEnumerator ThawBeam()
        {
            Hail(0);
            var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);Level(direction);
            float tell=VessaTune.BeamTell;
            var line=Mark(Telegraph.Line(transform.position,direction,VessaTune.BeamLength,VessaTune.BeamWidth,tell,Telegraph.Threat));
            strikeAt=Time.time+tell;
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up,.18f,AudioBus.SFX,50,.04f,.6f);
            yield return Windup(tell,-6,0,.02f);
            if(Interrupted)yield break;
            // Light and heat: whoever has solid cover between them and her lance is not reached.
            Strike(line,VessaTune.BeamDamage,DamageType.Plasma,0,0,BehindCover());
            BeamFlash(direction,VessaTune.BeamLength,.25f);
            AudioManager.PlayAt("laserSmall_000",transform,transform.position+Vector3.up,.24f,AudioBus.SFX,50,.04f,.55f);
            if(Phase>=2)vessaStrips.Add(LingeringArea.LayLine(transform.position,direction,VessaTune.BeamLength,VessaTune.BeamWidth,VessaTune.Burning,AreaEffect.Burn,1,VessaTune.BurnTick,health,DamageType.Plasma));
            Debug.Log("VESSA_BEAM"+(Phase>=2?" hot":""));
            yield return Windup(.2f,0);Pose(0);Finish(VessaRest);
        }

        IEnumerator HeatSweep()
        {
            Hail(0);
            var party=PartyController.Current;var aim=Toward(party.Active.transform.position);transform.rotation=Quaternion.LookRotation(aim);
            float tell=VessaTune.SweepTell;
            var fan=Mark(Telegraph.Cone(transform.position,aim,VessaTune.SweepReach,VessaTune.SweepArc,tell,Telegraph.Heavy));
            // Always from her left to her right, the way her lance hangs: a habit a player learns, so a roll into the beam as it comes (to her left) is the roll through it.
            const int side=1;
            // When the beam will cross the lead where he stands: what the partner and the driver dodge by.
            float Crossing(Vector3 at,float started)
            {
                var off=Flat(at-transform.position);float bearing=off.sqrMagnitude>.01f?Vector3.SignedAngle(aim,off,Vector3.up):0;
                float k=Mathf.Clamp01((bearing*side/VessaTune.SweepArc)+.5f);return started+k*VessaTune.SweepCross;
            }
            float from=Time.time+tell;strikeAt=Crossing(party.Active.transform.position,from);
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up,.2f,AudioBus.SFX,50,.04f,.5f);
            for(float t=0;t<tell;)
            {
                if(GameTime.Paused){yield return null;continue;}t+=Time.deltaTime;if(Interrupted)yield break;
                float k=Mathf.Clamp01(t/tell);Pose(-8*k,0,.02f*k);Level(Quaternion.Euler(0,-side*VessaTune.SweepArc*.5f*k,0)*aim);
                strikeAt=Crossing(party.Active.transform.position,Time.time+(tell-t));yield return null;
            }
            var swept=new HashSet<Health>();var covered=BehindCover();
            Debug.Log("VESSA_SWEEP");AudioManager.PlayAt("laserSmall_000",transform,transform.position+Vector3.up,.22f,AudioBus.SFX,50,.04f,.5f);
            for(float t=0;t<VessaTune.SweepCross;)
            {
                if(GameTime.Paused){yield return null;continue;}t+=Time.deltaTime;if(Interrupted){Unbeam();yield break;}
                float now=(Mathf.Clamp01(t/VessaTune.SweepCross)-.5f)*VessaTune.SweepArc*side;var heading=Quaternion.Euler(0,now,0)*aim;
                Level(heading);DrawBeam(heading,VessaTune.SweepReach);
                foreach(var hero in party.members)
                {
                    if(!hero.Health.Alive||swept.Contains(hero.Health)||!fan.Contains(hero.transform.position))continue;
                    var off=Flat(hero.transform.position-transform.position);float bearing=off.sqrMagnitude>.01f?Vector3.SignedAngle(aim,off,Vector3.up):0;
                    // The beam has come round to him, from whichever side it started: struck as it passes, once, unless something solid stands between.
                    if(side>0?bearing>now:bearing<now)continue;
                    swept.Add(hero.Health);if(covered.Contains(hero.Health))continue;
                    hero.Health.Receive(Packet(VessaTune.SweepDamage,DamageType.Plasma));
                }
                strikeAt=SweepReachesLead(party,swept,aim,side,now);
                yield return null;
            }
            fan.Release();Unbeam();strikeAt=0;
            // The lance vents: plate down, lance dry, and her back to whoever went round.
            Pose(10,-.1f);Debug.Log("VESSA_VENTS");BarkService.Play(party.Active.character,Situation("bossopening"),true);
            yield return Opening(VessaTune.Vents,VessaRest);Pose(0);Level(transform.forward);
        }
        /// <summary>While the beam crosses: when it reaches the lead, if it hasn't yet.</summary>
        float SweepReachesLead(PartyController party,HashSet<Health> swept,Vector3 aim,int side,float now)
        {
            var lead=party.Active;if(swept.Contains(lead.Health))return 0;
            var off=Flat(lead.transform.position-transform.position);float bearing=off.sqrMagnitude>.01f?Vector3.SignedAngle(aim,off,Vector3.up):0;
            float ahead=(bearing-now)*side;return ahead<0?0:Time.time+ahead/VessaTune.SweepArc*VessaTune.SweepCross;
        }

        IEnumerator PikeCall()
        {
            Hail(0);
            var lead=PartyController.Current.Active;var direction=Toward(lead.transform.position);transform.rotation=Quaternion.LookRotation(direction);Level(direction);
            // Every pikeguard of hers in reach braces at him with her: each its own marked line, landed by its own tick.
            int answered=0;
            foreach(var brain in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
            {
                if(brain==null||brain.definition==null||brain.definition.id!="MusterPikeguard"||!brain.Health.Alive||Flat(brain.transform.position-transform.position).magnitude>VessaTune.PikeReach)continue;
                if(brain.Behaviour is EnemyGuardian pike&&pike.Brace(lead))answered++;
            }
            Debug.Log("VESSA_PIKES "+answered);
            float tell=VessaTune.PikeTell;
            var line=Mark(Telegraph.Line(transform.position,direction,VessaTune.PikeLength,VessaTune.PikeWidth,tell,Telegraph.Heavy));
            strikeAt=Time.time+tell;
            AudioManager.PlayAt("impactMetal_000",transform,transform.position+Vector3.up,.18f,AudioBus.SFX,50,.04f,1.2f);
            yield return Windup(tell,-10,0,.03f);
            if(Interrupted)yield break;
            Pose(9);Strike(line,VessaTune.PikeDamage,DamageType.Kinetic,.3f,1.6f);
            AudioManager.PlayAt("impactMetal_000",transform,transform.position+Vector3.up,.24f,AudioBus.SFX,50,.04f,.8f);
            yield return Windup(.2f,0);Pose(0);Finish(VessaRest);
        }

        /// <summary>The shift: she gives up the middle. The room plays her talk on the flag (with her squad still standing); the move it promises takes her back.</summary>
        void VessaFallsBack()
        {
            GameServices.Current.Flags.SetBool(FallingBackFlag,true);
            if(lanceHead!=null)lanceHead.sharedMaterial=ActorFactory.Energy(LanceHot);
            Debug.Log("VESSA_HOT");
        }
        /// <summary>Where she holds in her second half: before her way out, or with none in the room, a stretch further from the party than she stands.</summary>
        Vector3 HoldingPost()
        {
            var exit=BossExit.For(health.id);var lead=PartyController.Current.Active.transform.position;
            if(exit!=null){var back=Flat(transform.position-exit.transform.position);return exit.transform.position+(back.sqrMagnitude>.01f?back.normalized:Vector3.back)*VessaTune.PostBeforeExit;}
            var away=Flat(transform.position-lead);return transform.position+(away.sqrMagnitude>.01f?away.normalized:-transform.forward)*VessaTune.RetreatWithNoExit;
        }
        IEnumerator FallBack()
        {
            var controller=GetComponent<CharacterController>();vessaPost=HoldingPost();vessaHolding=false;
            Debug.Log("VESSA_FALLS_BACK");
            // Back to the gantry door at a run, through whoever is in the way; nothing is struck on the way.
            Collide(false);
            for(float t=0,most=Flat(vessaPost-transform.position).magnitude/VessaTune.RetreatSpeed+1;t<most;)
            {
                if(GameTime.Paused||GameServices.Current.Input.Blocked){yield return null;continue;}
                t+=Time.deltaTime;if(Interrupted){Collide(true);yield break;}
                var way=Flat(vessaPost-transform.position);if(way.magnitude<.4f)break;
                transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(way),540*Time.deltaTime);
                if(controller!=null&&controller.enabled)controller.Move(way.normalized*Mathf.Min(way.magnitude,VessaTune.RetreatSpeed*Time.deltaTime));
                Pose(4*Mathf.Sin(t*12),0);yield return null;
            }
            Collide(true);vessaHolding=true;Pose(0);
            // Her talk (the room's), if it comes: nothing of hers starts until it is over.
            for(float wait=Time.time+VessaTune.TalkGrace;Time.time<wait&&!GameServices.Current.Input.Blocked;)yield return null;
            while(GameServices.Current.Input.Blocked)yield return null;
            if(Interrupted)yield break;
            yield return PourOrder(true);
        }

        /// <summary>"Pour drones, tip on my mark. I've costed the floor." Three strips across the way from her to the lead (through him, halfway, and just before her), marked, then the
        /// pour: struck where it falls, and burning after.</summary>
        IEnumerator PourOrder(bool hail)
        {
            if(hail)BarkService.PlayAt("Vessa","hail.gallery",1,true);
            var lead=PartyController.Current.Active.transform.position;var direction=Toward(lead);transform.rotation=Quaternion.LookRotation(direction);Level(direction,-35);
            var across=Vector3.Cross(Vector3.up,direction);float far=Mathf.Max(VessaTune.PourNear+2,Flat(lead-transform.position).magnitude);
            var marks=new List<Telegraph>();var lines=new List<(Vector3 origin,Vector3 across)>();
            for(int i=0;i<VessaTune.Pours;i++)
            {
                float at=Mathf.Lerp(far,VessaTune.PourNear,i/(float)(VessaTune.Pours-1));var centre=transform.position+direction*at;
                var origin=centre-across*(VessaTune.PourLength*.5f);lines.Add((origin,across));
                marks.Add(Mark(Telegraph.Line(origin,across,VessaTune.PourLength,VessaTune.PourWidth,VessaTune.PourTell,Telegraph.Threat)));
            }
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up*2,.2f,AudioBus.SFX,50,.04f,.7f);
            for(float t=0;t<VessaTune.PourTell;)
            {
                if(GameTime.Paused){yield return null;continue;}t+=Time.deltaTime;if(Interrupted)yield break;
                Pose(-5*Mathf.Clamp01(t/VessaTune.PourTell),.1f);ThreatFrom(marks);yield return null;
            }
            var once=new HashSet<Health>();
            for(int i=0;i<marks.Count;i++)
            {
                Strike(marks[i],VessaTune.PourDamage,DamageType.Plasma,0,0,once,true);
                var (origin,way)=lines[i];
                vessaStrips.Add(LingeringArea.LayLine(origin,way,VessaTune.PourLength,VessaTune.PourWidth,VessaTune.Burning,AreaEffect.Burn,1,VessaTune.BurnTick,health,DamageType.Plasma));
                for(int k=1;k<=3;k++)CombatVfx.Burst(origin+way*(VessaTune.PourLength*k/4f)+Vector3.up*.4f,new Color(1.5f,1.05f,.55f,.9f),"kill");
            }
            AudioManager.PlayAt("water_rush",transform,transform.position+direction*6,.26f,AudioBus.SFX,60,.04f,.6f);Tremor(.2f,.05f);
            Debug.Log("VESSA_POUR");strikeAt=0;Pose(0);Level(transform.forward);Finish(VessaRest);
        }

        IEnumerator DeadWeight()
        {
            var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            float tell=VessaTune.WeightTell;
            var line=Mark(Telegraph.Line(transform.position,direction,VessaTune.WeightLength,VessaTune.WeightWidth,tell,Telegraph.Heavy));
            strikeAt=Time.time+tell;
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up*2,.2f,AudioBus.SFX,50,.04f,.45f);
            // Raised high, the whole of her behind it.
            for(float t=0;t<tell;)
            {
                if(GameTime.Paused){yield return null;continue;}t+=Time.deltaTime;if(Interrupted)yield break;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/tell));Pose(-14*k,.15f*k,.03f*k*k);Level(direction,70*k);yield return null;
            }
            Level(direction,-28);Pose(12,-.15f);
            Strike(line,VessaTune.WeightDamage,DamageType.Kinetic,.45f,2);Tremor(.3f,.08f);
            CombatVfx.Burst(transform.position+direction*(VessaTune.WeightLength*.6f)+Vector3.up*.2f,new Color(1.3f,.9f,.5f),"kill");
            AudioManager.PlayAt("explosionCrunch_000",transform,transform.position+direction*3,.26f,AudioBus.SFX,50,.04f,.8f);
            // Wedged in the floor: she wrenches at it, open.
            Debug.Log("VESSA_WEDGED");BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);
            yield return Opening(VessaTune.Wedged,VessaRest);Pose(0);Level(transform.forward);
        }

        // ---------- the beam's light
        void DrawBeam(Vector3 heading,float reach)
        {
            if(vessaBeam==null)
            {
                var go=new GameObject("Thaw-lance beam",typeof(LineRenderer));temporary.Add(go);vessaBeam=go.GetComponent<LineRenderer>();
                vessaBeam.sharedMaterial=ActorFactory.Energy(Phase>=2?LanceHot:LanceCold);vessaBeam.positionCount=2;vessaBeam.startWidth=.34f;vessaBeam.endWidth=.18f;
                vessaBeam.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;vessaBeam.receiveShadows=false;
            }
            var from=transform.position+Vector3.up*1.3f;vessaBeam.SetPosition(0,from+heading*.8f);
            float length=reach;if(CombatCover.Sweep(from,from+heading*reach,.05f,out var wall))length=wall.distance;
            vessaBeam.SetPosition(1,CombatCover.Ground(from+heading*length+Vector3.up)+Vector3.up*.15f);
        }
        void Unbeam(){if(vessaBeam!=null){temporary.Remove(vessaBeam.gameObject);Destroy(vessaBeam.gameObject);}vessaBeam=null;}
        IEnumerator BeamFlashFor(Vector3 heading,float reach,float seconds)
        {
            for(float t=0;t<seconds;){if(!GameTime.Paused)t+=Time.deltaTime;DrawBeam(heading,reach);yield return null;}
            Unbeam();
        }
        void BeamFlash(Vector3 heading,float reach,float seconds)=>StartCoroutine(BeamFlashFor(heading,reach,seconds));
    }
}
