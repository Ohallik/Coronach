using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The Last Warden, chapter 7's boss: an Ancelane guardian in bronze and glass, twice a man's height, with a staff that plucks the fibres, still enforcing the first Bridle round a Choir
    /// a thousand years dead. It is not beaten down. Its bar is its hold on the road, and the fight is the party opening the Bridle's four clamps (<see cref="BridleClamp"/>) in front of it:
    /// with the fourth open it sees the release done, lowers its staff and stands down where it stands (<see cref="Ending.ShutDown"/>: its body stays, and nothing barks over
    /// <c>WardenAsks</c>, manifest 8.3). The room's contract is in docs/maps/GlassChoir.md, "Boss arena contract".
    /// <para><b>Its attacks follow the fibres.</b> Every one of them runs down a line (<see cref="ForceLine"/>): a fibre brightens (its strip of floor is marked), then the force runs down it.
    /// Pluck, a single fibre, the one the hero stands on: the force runs down it from the engine end as a travelling front. Chord, three at once (his and the two beside it), struck together:
    /// stand between them. Draw, a pull along a fibre toward its staff (a pale band: it drags, it doesn't strike) that ends on a marked snap at its staff; a roll breaks the pull, and a hero with
    /// his hands on a clamp has its weight and is not moved. A Draw that closes on nobody fouls its staff in the fibre: it is down, open. With no fibre near the hero (the floor an opened clamp
    /// has cleared, or a yard with none) it spins its own from the staff, a beat slower to brighten.</para>
    /// <list type="bullet">
    /// <item><b>The road held</b> (to half). Its hold comes down by fighting it, and stops at a line for each clamp still shut (three quarters, half, a quarter, nought): there it holds (a blow
    /// takes nothing off it, and reads as resisted; a break still drops its staff), and the clamps' seats light. A hero takes a clamp's weight; the clamp eases only while the staff is down;
    /// held through the ease it opens, its three fibres go slack, the Warden counts it, and its hold goes over the line. The second clamp is the shift.</item>
    /// <item><b>The Bridle slackening</b> (under half). Two Clamp Tenders step out beside the clamps still shut, to close whatever a hero opens. It takes its staff in both hands: its tells
    /// are quicker and it adds Toll, the staff struck on the floor and every fibre still held lit and struck at once, after which the staff rings in the floor and it is open.</item>
    /// </list>
    /// It fights without clamps or fibres too (the proving yard): its own lines, no hold at any line, its phase on integrity alone, two tenders beside it at the shift, and it stands down at nothing.
    /// It keeps within <see cref="WardenTune.Anchor"/> of the engine it was set at, and works round rather than stand between the camera and the hero.
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit LastWardenKit=Register(new Kit
        {
            id="LastWarden",title="THE LAST WARDEN",epithet="Guardian of the first Bridle, a thousand years on watch",thresholds=new[]{.5f},
            choose=(boss,hero)=>boss.ChooseWarden(hero),
            moves={["Pluck"]=boss=>boss.Pluck(),["Chord"]=boss=>boss.Chord(),["Draw"]=boss=>boss.Draw(),["Toll"]=boss=>boss.Toll()},
            signature=phase=>phase>=2?"Toll":null,
            shift=boss=>boss.TheBridleSlackens(),shiftText="THE BRIDLE SLACKENS  ·  BRACE",
            status=boss=>boss.WardenStatus(),
            fightGoal=(boss,label)=>boss.WardenGoal(label),
            // It stands down, and WardenAsks follows: the shared "boss" bark would land on that (manifest 8.3: nobody speaks when the Bridle opens but the Warden and Ishen).
            capsuleRadius=WardenTune.Radius,occlusion=true,ending=Ending.ShutDown,downBark="",
            start=boss=>boss.WardenStart(),update=boss=>boss.WardenUpdate(),down=boss=>boss.WardenDown(),
        });
        /// <summary>The Last Warden's numbers, all of them (its integrity is its EnemyDef's: OrielBuilder.Enemies). A boss stays at threat 1. Every move is authored at 120 to 128: 12.3 to
        /// 15.7% of a Sync 20 or 21 hero's bar in the Tempered parts chapter 7 arrives in (Idra at 21 the least, Taren and Sela at 20 the most; OrielCombatTests measures it on all three),
        /// more than anything on the road to it (100 at most), in both halves: what changes as the Bridle slackens is what it does, not how hard a blow lands.</summary>
        public static class WardenTune
        {
            /// <summary>What its second half adds to every blow: nothing. The shared +18% (PhaseStep) carried the Draw's snap to 18.6% of Taren's bar at Sync 20, over a boss's 16.</summary>
            public const float SlackBonus=0;
            /// <summary>Its capsule's radius (the capsule is 3.6 m tall: its EnemyDef's), and how far from the engine it was set at it will go.</summary>
            public const float Radius=1.5f,Anchor=3;
            /// <summary>How near a fibre a hero must stand for it to use that fibre rather than spin its own; its own line's length and width; and the beat longer its own takes to brighten.</summary>
            public const float Catch=2.2f,OwnLength=15,LineWidth=1.8f,OwnSlower=.25f;
            /// <summary>Pluck: its tell held and slackening, the front's speed and depth, the blow.</summary>
            public const float PluckTell=1f,PluckTellLater=.85f,PluckSpeed=22,PluckBand=1.6f,PluckDamage=124;
            /// <summary>Chord: its tell held and slackening, how far apart its own three stand where the room has too few, the blow.</summary>
            public const float ChordTell=1.1f,ChordTellLater=.9f,ChordSpread=28,ChordDamage=120;
            /// <summary>Draw: its tell, the pull's length and pace, the pull band's width, how far its own line reaches, the snap's radius and blow, and how long its staff is fouled after
            /// a snap on nobody.</summary>
            public const float DrawTell=.9f,DrawPull=1.4f,DrawSpeed=4.5f,DrawWidth=2.4f,DrawReach=16,SnapRadius=2.6f,SnapDamage=128,Fouled=2.8f;
            /// <summary>Toll: its tell, the blow down every fibre, how long its staff rings in the floor after (open), and how many spokes of its own it strikes where the room has none.</summary>
            public const float TollTell=1.2f,TollDamage=122,TollRings=2.4f;
            public const int TollSpokes=6;
            /// <summary>Its rest between specials, held and slackening.</summary>
            public const float RestHeld=2.6f,RestSlack=2.1f;
            /// <summary>The tenders its shift calls out: how far to a clamp's side, their ring, and how many beside it in a room with no clamps.</summary>
            public const float TenderBeside=2.5f,TenderRing=1.1f;
            public const int YardTenders=2;
            /// <summary>How long it takes, at the fourth clamp, to turn back to its post's facing, lower its staff and bow.</summary>
            public const float StandDown=1.2f;
        }
        /// <summary>A pull is no blow: a pale band in the fibres' own white-gold, never a danger's colour (D217, D222). Where a tender steps out is a boundary too.</summary>
        static readonly Color DrawTone=new(1f,.95f,.78f,.36f),TenderTone=new(.86f,.92f,1f,.4f),FibreFlash=new(1.2f,1.15f,1f,.9f);
        readonly List<BridleClamp> wardenClamps=new();
        bool hearsClamps,heldSaid;float wardenRadius=WardenTune.Radius;string wardenOpenWhy="";
        /// <summary>The way it was set facing at the engine (the hall's door, yaw 180): it turns back to it as it stands down, the way the hall shows it on every later visit.</summary>
        Vector3 wardenFacing;
        bool Slackening=>Phase>=2;
        float WardenRest=>Slackening?WardenTune.RestSlack:WardenTune.RestHeld;
        /// <summary>Clamps that answer to it, and how many of them are still shut.</summary>
        public int ClampsTotal=>wardenClamps.Count;
        public int ClampsShut{get{int n=0;foreach(var clamp in wardenClamps)if(clamp!=null&&clamp.State==BridleClamp.Hold.Shut)n++;return n;}}
        /// <summary>The least its hold can be fought down to: just over the line of the next clamp still shut. None in a room with no clamps, or once they are all open.</summary>
        float HoldFloor{get{int total=ClampsTotal,shut=ClampsShut;if(health==null||total==0||shut==0)return 0;return health.maximum*(shut-1)/total+1;}}
        /// <summary>Fought down to its line: the road is held, and a clamp can be opened.</summary>
        public bool RoadHeld=>health!=null&&health.Alive&&ClampsTotal>0&&ClampsShut>0&&health.integrity<=HoldFloor+.5f;
        void WardenStart()
        {
            wardenRadius=GetComponent<CharacterController>().radius;wardenFacing=transform.forward;
            wardenClamps.Clear();
            foreach(var clamp in BridleClamp.All)if(clamp!=null&&(string.IsNullOrEmpty(clamp.settledBy)||clamp.settledBy==health.id))wardenClamps.Add(clamp);
            BridleClamp.Opened+=WardenClampOpened;hearsClamps=true;
            // Struck open it staggers on its feet, bowed over its staff (WardenStagger): the shared Stagger frame alone left it as it stood.
            if(!TryGetComponent<WardenStagger>(out _))gameObject.AddComponent<WardenStagger>();
            // Its hold stops at the line of the next clamp still shut. At the line a blow takes nothing and reads as resisted; a break still drops its staff, and a blow in that opening
            // takes nothing off it either (Health.Holds), so a held line is never a kill.
            health.Holds=()=>HoldFloor;
            health.Plate=_=>RoadHeld?0:1;
        }
        void WardenUpdate()
        {
            brain.DamageScale=1+(Phase-1)*WardenTune.SlackBonus;
            // It keeps to the engine: whatever its walk toward the hero would make of that, it is drawn back within its anchor.
            if(brain.Controller.enabled){var off=Flat(transform.position-brain.Home);float out_=off.magnitude;if(out_>WardenTune.Anchor)brain.Controller.Move(-off/out_*(out_-WardenTune.Anchor));}
            bool held=RoadHeld;
            if(held&&!heldSaid){heldSaid=true;Debug.Log($"WARDEN_HOLDS shut={ClampsShut}");}else if(!held)heldSaid=false;
            BridleClamp.Offer(held&&!shifting);BridleClamp.Slack=health.Broken;
            if(busy||health.Broken||brain.Passive||brain.Telegraphing)return;
            Pose(Slackening?2+1.2f*Mathf.Sin(Time.time*1.6f):1.4f*Mathf.Sin(Time.time*.8f),Slackening?-.08f:0);
            WorkRoundTheCamera();
        }
        string WardenStatus()
        {
            if(health==null||!health.Alive)return "";
            // Its staff down says so first: at a held line that is the slack, and the clamps are what to strike at.
            if(health.Broken&&Time.time<openingUntil)return wardenOpenWhy+(RoadHeld?"  ·  OPEN A CLAMP":"  ·  STRIKE");
            return RoadHeld?"THE ROAD IS HELD  ·  OPEN A CLAMP":"";
        }
        string WardenGoal(string label)
        {
            if(ClampsTotal==0)return label;
            foreach(var clamp in wardenClamps)if(clamp!=null&&clamp.Holder!=null&&clamp.Tended.Tending)return "Stop the Clamp Tender: it is closing the clamp";
            if(BridleClamp.Held!=null)return "Take its weight: hold until the slack";
            int open=ClampsTotal-ClampsShut;
            return RoadHeld?$"Open a clamp while the staff is down ({open}/{ClampsTotal})":$"Wear down the Warden's hold on the road ({open}/{ClampsTotal})";
        }
        void WardenClampOpened(BridleClamp clamp)
        {
            // A static event outlives a boss the scene took away mid-fight.
            if(this==null){BridleClamp.Opened-=WardenClampOpened;return;}
            if(health==null||!health.Alive||!isActiveAndEnabled||!wardenClamps.Contains(clamp))return;
            if(engaged<0)engaged=Time.realtimeSinceStartup;
            int shut=ClampsShut;
            Tremor(.35f,.07f);Debug.Log($"WARDEN_COUNTS {ClampsTotal-shut}");
            if(shut>0)
            {
                // It counts what has been opened, and nothing else (manifest: "One clamp is opened."), by its place in the pool: spoken in turn, the count ran on from an earlier fight
                // in the same session (a lost one, fought again), and its first clamp was "Two" (OrielCombatTests, November 12). Its hold goes over the line that clamp kept.
                BarkService.PlayAt("Warden","clamp.open",ClampsTotal-shut-1,true);
                float line=health.maximum*shut/ClampsTotal;if(health.integrity>line-1)health.integrity=Mathf.Max(1,line-1);
                return;
            }
            // The fourth: every clamp lets go together and it sees the release done. Its hold is gone, and it stands down where it stands.
            Debug.Log("WARDEN_RELEASED");
            StopAllCoroutines();Interrupt();busy=false;openingUntil=0;
            health.Holds=null;health.Plate=null;health.InvulnerableUntil=0;health.ShieldUntil=0;health.Damageable=true;
            var by=clamp.OpenedBy!=null?clamp.OpenedBy.Health:null;
            health.Receive(new DamagePacket{source=by,amount=health.integrity*10+1,type=health.weakness,weight=HitWeight.Light,tag="release"});
        }
        /// <summary>What its shift brings: two tenders, one beside each clamp still shut (in a yard, beside it), each stepping out on a pale ring.</summary>
        void TheBridleSlackens()
        {
            Debug.Log("WARDEN_SLACKENS");
            var definition=GameCatalog.Find<EnemyDef>("ClampTender");if(definition==null)return;
            summoned.RemoveAll(s=>s==null||!s.Health.Alive);
            var spots=new List<Vector3>();int side=1;
            foreach(var clamp in wardenClamps)if(clamp!=null&&clamp.State==BridleClamp.Hold.Shut){spots.Add(clamp.transform.position+clamp.transform.right*(side*WardenTune.TenderBeside));side=-side;}
            if(spots.Count==0){var ahead=Toward(PartyController.Current.Active.transform.position);var right=Vector3.Cross(Vector3.up,ahead);for(int i=0;i<WardenTune.YardTenders;i++)spots.Add(transform.position+right*((i*2-1)*(wardenRadius+2.5f))+ahead*1.5f);}
            foreach(var spot in spots)
            {
                var point=CombatCover.Ground(spot+Vector3.up*3);
                var ring=Mark(Telegraph.Circle(point,WardenTune.TenderRing,1.2f,TenderTone));ring.Release();
                var tender=ActorFactory.Enemy(definition,point+Vector3.up*.05f);summoned.Add(tender);
                CombatVfx.Burst(point+Vector3.up*.6f,new Color(1.2f,.9f,.5f),"kill");
            }
            Debug.Log("BOSS_SUMMON "+health.id+" "+summoned.Count);
        }
        void WardenDown()
        {
            if(hearsClamps){BridleClamp.Opened-=WardenClampOpened;hearsClamps=false;}
            BridleClamp.Offer(false);BridleClamp.LetGoTogether();
            foreach(var line in ForceLine.All)if(line!=null)line.Dim();
            // It stands down, not beaten: its body stays where it stood for the rest of the visit, staff lowered, instead of being cleared away as a beaten body is. Over 1.2 s it turns
            // back to the way it was set facing, lowers the staff's point to the floor and bows its head (StandsDown: a pose made on its own rig, no clip), and holds it; the hall's own
            // stood-down Warden, which the same flag shows, stays hidden while this one stands. A body with no rig sinks and tips instead.
            var presentation=GetComponent<DefeatPresentation>();if(presentation!=null)Destroy(presentation);
            var stands=gameObject.AddComponent<StandsDown>();stands.id=health.id;stands.fought=true;stands.seconds=WardenTune.StandDown;stands.face=wardenFacing;
            if(visual!=null&&GetComponentInChildren<Animator>()==null)StartCoroutine(LowerTheStaff());
            Debug.Log("WARDEN_STOOD_DOWN");
        }
        IEnumerator LowerTheStaff()
        {
            for(float t=0;t<1.2f;){if(!GameTime.Paused)t+=Time.deltaTime;float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/1.2f));visual.localRotation=visualRestRotation*Quaternion.Euler(9*k,0,0);visual.localPosition=visualRest+Vector3.down*(.18f*k);yield return null;}
        }

        // ---------------------------------------------------------------- the lines it strikes down
        /// <summary>One strike's line: where it starts (the engine end), its way and length, and the fibre it runs down if it is one of the room's.</summary>
        struct Strand{public Vector3 from,way;public float length;public ForceLine fibre;public bool Spun=>fibre==null;}
        /// <summary>The room's fibres still held, near enough to the engine to be its own.</summary>
        List<ForceLine> RoomFibres()
        {
            var lines=new List<ForceLine>();
            foreach(var line in ForceLine.All)if(line!=null&&line.Live&&!line.Made&&Flat(line.Closest(brain.Home)-brain.Home).magnitude<WardenTune.OwnLength)lines.Add(line);
            var home=brain.Home;lines.Sort((a,b)=>a.BearingFrom(home).CompareTo(b.BearingFrom(home)));
            return lines;
        }
        static Strand Of(ForceLine line)=>new Strand{from=line.From,way=line.Direction,length=line.Length,fibre=line};
        /// <summary>Its own line, spun from the staff toward a point.</summary>
        Strand Spun(Vector3 toward,float length)
        {
            var way=Toward(toward);var from=transform.position+way*(wardenRadius*.6f);
            return new Strand{from=from,way=way,length=length};
        }
        Strand Spun(float bearing,float length){var way=Quaternion.Euler(0,bearing,0)*Vector3.forward;return new Strand{from=transform.position+way*(wardenRadius*.6f),way=way,length=length};}
        /// <summary>The room's fibre a hero stands on (within <see cref="WardenTune.Catch"/>), or its own toward him.</summary>
        Strand LineAt(Vector3 hero,List<ForceLine> room,float ownLength)
        {
            ForceLine best=null;float least=WardenTune.Catch;
            foreach(var line in room){float d=line.Distance(hero);if(d<least){least=d;best=line;}}
            return best!=null?Of(best):Spun(hero,ownLength);
        }
        Telegraph MarkStrand(Strand strand,float width,float tell,Color colour)
        {
            if(strand.fibre!=null)strand.fibre.Light(tell);
            var from=strand.from;from.y=CombatCover.Ground(from+Vector3.up*3).y;
            return Mark(Telegraph.Line(from,strand.way,strand.length,width,tell,colour));
        }
        void StrumFibre(Strand strand)
        {
            for(int i=1;i<=3;i++)CombatVfx.Burst(strand.from+strand.way*(strand.length*i/4f)+Vector3.up*.4f,FibreFlash,"kill");
            if(strand.fibre!=null)strand.fibre.Dim();
        }
        IEnumerator WardenOpen(string why,float seconds)
        {
            wardenOpenWhy=why;BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);
            yield return Opening(seconds,WardenRest);
        }

        // ---------------------------------------------------------------- choosing
        (string,IEnumerator) ChooseWarden(CombatActor hero)
        {
            float distance=Flat(hero.transform.position-transform.position).magnitude;
            string move;
            // The release under way: a hero has his hands on a clamp. It pulls the line that clamp holds (which a held clamp answers), or strikes down it.
            if(BridleClamp.Held!=null)move=Pick(("Draw",2.2f),("Pluck",1f),("Chord",.6f),("Toll",Slackening?.8f:0));
            else move=Pick(("Pluck",1.6f),("Chord",distance>wardenRadius+3?1.4f:FullOffer),("Draw",distance>wardenRadius+4?1.5f:FullOffer),("Toll",Slackening?1.2f:0));
            return move switch{"Chord"=>(move,Chord()),"Draw"=>(move,Draw()),"Toll"=>(move,Toll()),_=>(move,Pluck())};
        }

        // ---------------------------------------------------------------- the moves
        IEnumerator Pluck()
        {
            var hero=PartyController.Current.Active.transform.position;var strand=LineAt(hero,RoomFibres(),WardenTune.OwnLength);
            float tell=(Slackening?WardenTune.PluckTellLater:WardenTune.PluckTell)+(strand.Spun?WardenTune.OwnSlower:0);
            transform.rotation=Quaternion.LookRotation(Toward(strand.from+strand.way*strand.length*.5f));
            var lane=MarkStrand(strand,WardenTune.LineWidth,tell,Telegraph.Threat);
            float along=Vector3.Dot(Flat(hero-strand.from),strand.way);
            strikeAt=Time.time+tell+Mathf.Max(0,along-WardenTune.PluckBand)/WardenTune.PluckSpeed;
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up*2.5f,.24f,AudioBus.SFX,50,.05f,1.6f);
            yield return Windup(tell,-8,.25f,.03f);
            if(Interrupted)yield break;
            Pose(9);Tremor(.2f,.05f);AudioManager.PlayAt("laserSmall_000",transform,strand.from+Vector3.up,.24f,AudioBus.SFX,50,.05f,.7f);
            // The force runs down the fibre from the engine end: the lane stays marked ahead of it, and the front is a band of the same paint moving down it.
            lane.Steady=true;var front=Mark(Telegraph.Line(lane.Origin,strand.way,WardenTune.PluckBand,WardenTune.LineWidth,99,Telegraph.Threat));front.Steady=true;
            var met=new HashSet<Health>();int frame=0;
            for(float ran=0;ran<strand.length&&!Interrupted;)
            {
                if(GameTime.Paused){yield return null;continue;}
                ran=Mathf.Min(strand.length,ran+WardenTune.PluckSpeed*Time.deltaTime);float back=Mathf.Max(0,ran-WardenTune.PluckBand);
                front.MoveTo(lane.Origin+strand.way*back);
                foreach(var actor in PartyController.Current.members)
                {
                    if(!actor.Health.Alive||met.Contains(actor.Health)||!lane.Contains(actor.transform.position))continue;
                    float at=Vector3.Dot(Flat(actor.transform.position-lane.Origin),strand.way);
                    if(at<back-Telegraph.BodyAllowance||at>ran+Telegraph.BodyAllowance)continue;
                    met.Add(actor.Health);if(actor.Health.Receive(Packet(WardenTune.PluckDamage,DamageType.Pulse))>0)actor.Stagger(.3f);
                }
                if(frame++%3==0)CombatVfx.Burst(lane.Origin+strand.way*ran+Vector3.up*.4f,FibreFlash,"steam");
                var lead=PartyController.Current.Active;float ahead=Vector3.Dot(Flat(lead.transform.position-lane.Origin),strand.way)-ran;
                strikeAt=ahead>0&&!met.Contains(lead.Health)&&lane.Contains(lead.transform.position)?Time.time+ahead/WardenTune.PluckSpeed:0;
                yield return null;
            }
            front.Cancel();lane.Release();strikeAt=0;if(strand.fibre!=null)strand.fibre.Dim();
            Pose(0);Finish(WardenRest);
        }
        IEnumerator Chord()
        {
            var hero=PartyController.Current.Active.transform.position;var room=RoomFibres();
            var strands=new List<Strand>();var middle=LineAt(hero,room,WardenTune.OwnLength);strands.Add(middle);
            // The two either side of his: the room's neighbours by bearing round the engine, or its own where the room has too few.
            if(!middle.Spun&&room.Count>=3)
            {
                int at=room.IndexOf(middle.fibre);strands.Add(Of(room[(at+room.Count-1)%room.Count]));strands.Add(Of(room[(at+1)%room.Count]));
            }
            else
            {
                float bearing=Mathf.Atan2(middle.way.x,middle.way.z)*Mathf.Rad2Deg;
                strands.Add(Spun(bearing-WardenTune.ChordSpread,WardenTune.OwnLength));strands.Add(Spun(bearing+WardenTune.ChordSpread,WardenTune.OwnLength));
            }
            bool own=false;foreach(var strand in strands)own|=strand.Spun;
            float tell=(Slackening?WardenTune.ChordTellLater:WardenTune.ChordTell)+(own?WardenTune.OwnSlower:0);
            var lanes=new List<Telegraph>();foreach(var strand in strands)lanes.Add(MarkStrand(strand,WardenTune.LineWidth,tell,Telegraph.Threat));
            transform.rotation=Quaternion.LookRotation(Toward(hero));
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up*2.5f,.26f,AudioBus.SFX,50,.05f,1.2f);
            for(float t=0;t<tell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/tell));Pose(-9*k,.3f*k,.03f*k*k);ThreatFrom(lanes);yield return null;
            }
            // All three at once, each hero struck once at most.
            var once=new HashSet<Health>();
            foreach(var lane in lanes)Strike(lane,WardenTune.ChordDamage,DamageType.Pulse,.3f,0,once);
            foreach(var strand in strands)StrumFibre(strand);
            Pose(10);Tremor(.3f,.07f);AudioManager.PlayAt("laserSmall_000",transform,transform.position+Vector3.up*2,.26f,AudioBus.SFX,50,.05f,.55f);
            strikeAt=0;yield return Windup(.2f,0);Pose(0);Finish(WardenRest);
        }
        IEnumerator Draw()
        {
            var lead=PartyController.Current.Active;var room=RoomFibres();
            // The line a hero holding a clamp is on, if one is: the clamp's own fibre. Else the one the lead stands on, or its own toward him.
            var held=BridleClamp.Held;Strand strand;
            if(held!=null){ForceLine best=null;float least=float.PositiveInfinity;foreach(var line in room){if(line.clamp!=held.index)continue;float d=line.Distance(held.transform.position);if(d<least){least=d;best=line;}}strand=best!=null?Of(best):Spun(held.transform.position,WardenTune.DrawReach);}
            else strand=LineAt(lead.transform.position,room,WardenTune.DrawReach);
            float tell=WardenTune.DrawTell+(strand.Spun?WardenTune.OwnSlower:0),whole=tell+WardenTune.DrawPull;
            transform.rotation=Quaternion.LookRotation(strand.way);
            // The pull's reach (pale: it drags, it doesn't strike) and the snap at its staff that the pull ends on (the blow), both from the start, so where it ends is read before it begins.
            if(strand.fibre!=null)strand.fibre.Light(whole);
            var from=strand.from;from.y=CombatCover.Ground(from+Vector3.up*3).y;
            var band=Mark(Telegraph.Line(from,strand.way,strand.length,WardenTune.DrawWidth,whole,DrawTone));
            var staff=transform.position+strand.way*(wardenRadius+WardenTune.SnapRadius*.35f);staff=CombatCover.Ground(staff+Vector3.up*3);
            var snap=Mark(Telegraph.Circle(staff,WardenTune.SnapRadius,whole,Telegraph.Heavy));
            strikeAt=Time.time+whole;
            yield return Windup(tell,-6,.3f,.03f);
            if(Interrupted)yield break;
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up*2.5f,.26f,AudioBus.SFX,50,.05f,.45f);
            // The pull: whoever stands on the band is drawn to the staff. A roll breaks its hold for the rest of this Draw; a hero with his hands on a clamp has its weight and is not moved.
            var freed=new HashSet<CombatActor>();
            for(float t=0;t<WardenTune.DrawPull;)
            {
                if(GameTime.Paused){yield return null;continue;}
                float dt=Time.deltaTime;t+=dt;if(Interrupted)yield break;
                foreach(var actor in PartyController.Current.members)
                {
                    if(!actor.Health.Alive||freed.Contains(actor))continue;
                    if(actor.State==ActorState.Dodge){freed.Add(actor);Debug.Log("WARDEN_DRAW_BROKEN "+actor.character);continue;}
                    if(held!=null&&held.Holder==actor)continue;
                    if(!band.Contains(actor.transform.position))continue;
                    var way=Flat(staff-actor.transform.position);float gap=way.magnitude;if(gap<.4f)continue;
                    if(actor.TryGetComponent<CharacterController>(out var feet)&&feet.enabled)feet.Move(way/gap*Mathf.Min(gap,WardenTune.DrawSpeed*dt));
                }
                Pose(-7+2*Mathf.Sin(t*30),.25f);
                if(Time.frameCount%4==0)CombatVfx.Burst(from+strand.way*Random.Range(2f,strand.length)+Vector3.up*.5f,FibreFlash,"steam");
                yield return null;
            }
            band.Release();if(strand.fibre!=null)strand.fibre.Dim();
            // The snap at its staff.
            Pose(10);snap.Release();int took=0;
            foreach(var actor in PartyController.Current.members)
            {
                if(!actor.Health.Alive||!snap.Contains(actor.transform.position))continue;
                if(actor.Health.Receive(Packet(WardenTune.SnapDamage,DamageType.Pulse))>0){took++;actor.Stagger(.4f);}
            }
            AudioManager.PlayAt("explosionCrunch_000",transform,staff+Vector3.up,.26f,AudioBus.SFX,50,.05f,1.2f);Tremor(.25f,.07f);strikeAt=0;
            if(took>0){yield return Windup(.2f,0);Pose(0);Finish(WardenRest);yield break;}
            // It closed on nothing: its staff is fouled in the fibre, and it is down. A clamp held through this is the slack.
            Pose(14,-.3f);Debug.Log("WARDEN_FOULED");
            yield return WardenOpen("ITS STAFF IS FOULED",WardenTune.Fouled);Pose(0);
        }
        IEnumerator Toll()
        {
            var hero=PartyController.Current.Active.transform.position;var room=RoomFibres();
            var strands=new List<Strand>();foreach(var line in room)strands.Add(Of(line));
            // Where the room holds no fibre, it strikes its own spokes round itself, one through the hero.
            if(strands.Count==0)
            {
                var way=Toward(hero);float bearing=Mathf.Atan2(way.x,way.z)*Mathf.Rad2Deg;
                for(int i=0;i<WardenTune.TollSpokes;i++)strands.Add(Spun(bearing+i*360f/WardenTune.TollSpokes,WardenTune.OwnLength));
            }
            var lanes=new List<Telegraph>();foreach(var strand in strands)lanes.Add(MarkStrand(strand,WardenTune.LineWidth,WardenTune.TollTell,Telegraph.Heavy));
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up*2.5f,.3f,AudioBus.SFX,50,.05f,.8f);
            for(float t=0;t<WardenTune.TollTell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/WardenTune.TollTell));Pose(-12*k,.45f*k,.04f*k*k);ThreatFrom(lanes);yield return null;
            }
            var once=new HashSet<Health>();
            foreach(var lane in lanes)Strike(lane,WardenTune.TollDamage,DamageType.Pulse,.35f,0,once);
            foreach(var strand in strands)StrumFibre(strand);
            Pose(16,-.35f);Tremor(.5f,.11f);AudioManager.PlayAt("explosionCrunch_000",transform,transform.position,.3f,AudioBus.SFX,60,.05f,.7f);strikeAt=0;
            Debug.Log("WARDEN_TOLLS "+strands.Count);
            // The staff rings in the floor where it struck it: it has to wait for it to be still.
            yield return WardenOpen("ITS STAFF RINGS IN THE FLOOR",WardenTune.TollRings);Pose(0);
        }
    }
}
