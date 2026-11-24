using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Mother of Doors, chapter 10's first boss (SorrelSecondSky; brief, "The fights"; wiring 6.2; the room's contract is docs/maps/SorrelSecondSky.md, "Boss arena contract"). Not an
    /// enemy with a bar to empty: a rescue expressed as a boss fight. The adult Choir is coiled round the nursery under Stillwater's clamp, her head raised at the crater's middle facing
    /// whoever comes (her body: <see cref="MotherCoil"/>), pinned by four clamps. <b>Nobody shoots at her</b> (Idra's plan): in her room she can't be struck or targeted, and <b>her bar is
    /// her four clamps</b>, a quarter each (each a <see cref="BridleClamp"/> on a shelf beside her coil, chapter 7's hold: take its weight, wait for the slack, ease it). The party flies a
    /// clamp's guns down, docks at its shelf (a berth, wiring 6.1), works the clamp on foot while the partner stops the Clamp Tender that steps out to work it shut, and launches; four times,
    /// in order. She is frightened, not hostile: her sweeps and pressure waves are huge, marked and leave lanes, and each clamp off <b>narrows</b> them (the herders' hum on the radio is
    /// reaching her) while she <b>quickens</b> (shorter tells and rests, more waves at once, the lash from the first clamp off, the heave and a back-swing from the third), so the fight gets
    /// more dangerous as she gets less afraid. Her <b>slack</b> after every move (she draws back) is when a held clamp eases. From the third clamp she strains against the last: the fourth
    /// shelf swings on a marked arc (<see cref="MotherStrain"/>) and is docked alongside her. With the fourth open every clamp lets go and she goes up her road, the young after her
    /// (<see cref="Ending.Leaves"/>, <see cref="MotherCoil.Release"/>): nothing barks, the music goes out, and <c>SurvivorMotherGoes</c> (the zone's after-talk) is the only line for a while.
    /// <para>What a defeat and a Continue keep (D285, wiring 6.1 rule 3): each clamp opened is saved the moment it opens (<see cref="SaveSystem.KeepCheckpoint"/>) and loads open
    /// (<see cref="BridleClamp.keepsOpened"/>); she wakes at the line of the clamps still shut, in the phase they leave her.</para>
    /// <para>In a room with no clamps of hers (the proving yards) she fights on integrity alone, as the Last Warden does there, so the shared boss tests measure her as any boss: her phases
    /// by her bar, every move marked and rollable, an opening after each. Her length in her room is four cycles of fly, dock, work and launch, measured by a test that docks
    /// (<c>LastMooringBossTests</c>); her integrity is sized so the yard's fight lasts as long.</para>
    /// <para>Her moves, from her middle C (every number in <see cref="MotherTune"/>): <b>Sweep</b>, her head swings and three wedges land in turn across the lead's lanes from her snout, gaps
    /// wider than two hulls between them (on a shelf, every other sweep leaves the seat in a gap: the marking says whether a hand on the clamp may stay); with three clamps off a back-swing
    /// marks the gaps as the first pass lands. <b>Wave</b>, her coil inhales (a band at its edge, filling) and pressure bands run out from it, one to three; cover stops one (a rock in the
    /// lanes, the clamp's works in front of its seat). <b>Lash</b> (from the first clamp off), her coil whips out along a marked lane through the lead. <b>Heave</b> (from the third), the
    /// coil surges up under a marked ring round her that reaches the seats. Logs <c>MOTHER_WAKES</c>, <c>MOTHER_CLAMP_OPEN</c>, <c>MOTHER_TENDERS</c>, <c>MOTHER_SWEEP</c>,
    /// <c>MOTHER_WAVE</c>, <c>MOTHER_SLACK</c>, <c>MOTHER_STRAINS</c>, then <see cref="MotherCoil"/>'s.</para>
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit MotherOfDoorsKit=Register(new Kit
        {
            id="MotherOfDoors",title="MOTHER OF DOORS",epithet="A grown road, come back for her young",thresholds=new[]{.75f,.5f,.25f},
            choose=(boss,hero)=>boss.ChooseMother(hero),
            moves={["Sweep"]=boss=>boss.MotherSweep(),["Wave"]=boss=>boss.MotherWave(),["Lash"]=boss=>boss.MotherLash(),["Heave"]=boss=>boss.MotherHeave()},
            signature=phase=>phase==2?"Wave":phase==3?"Lash":phase>=4?"Heave":null,
            shift=boss=>boss.MotherEases(),shiftText="SHE EASES  ·  BRACE",
            status=boss=>boss.MotherStatus(),guarded=boss=>boss.MotherStatus(),
            fightGoal=(boss,label)=>boss.MotherGoal(label),
            // She goes, and SurvivorMotherGoes follows on its own: nothing is said over her (manifest 8.4). Her head never stands between the camera and the party (it is at her middle,
            // north of every lane). Her weak spots are a boss's three (BossController.WeakSpotAt): in her room nothing strikes her and they take nothing; in a yard she is a boss like any other.
            capsuleRadius=MotherTune.Radius,occlusion=false,ending=Ending.Leaves,downBark="",
            start=boss=>boss.MotherStart(),update=boss=>boss.MotherUpdate(),down=boss=>boss.MotherDown(),
        });
        /// <summary>Mother of Doors' numbers, all of them (her integrity is her EnemyDef's: LastMooringBuilder.Mother). Arrays run by clamps open, 0 to 3 (in a yard, her phase less one).
        /// Every blow is authored at 175 to 185 and her phases add nothing to it (the hum narrows what she lashes; it never grows): 12 to 16% of a hero's bar at Sync 27 and 28 in the
        /// Choirwrought parts chapter 10 arrives in, measured on all three by LastMooringBossTests.</summary>
        public static class MotherTune
        {
            /// <summary>Her head's capsule (8 m tall: her EnemyDef's), where its drawn middle stands over her root, how far its snout is lifted, how far back from her root it runs, and how
            /// far forward her snout is.</summary>
            public const float Radius=4,Height=8,HeadLift=1.2f,HeadRaised=12,HeadBack=7.5f,Snout=8;
            /// <summary>How far round her middle she is solid at the plane in her room: her drawn head's reach (16 m wide, 9 m out), so no hull comes under it or behind it.</summary>
            public const float Solid=9.5f;
            /// <summary>And behind her head, where her neck climbs from her coil to it: a capsule lying along her neck, this round, from her middle to this far behind her (its rounded end
            /// included), so no hull comes in behind her where her neck hides it.</summary>
            public const float NeckSolid=8,NeckSolidLength=24;
            /// <summary>Her coil: the drums (and a tail), the centreline's radius round her middle, how far under the plane, how much it draws in over its turn; how far it breathes, how
            /// high it heaves, how far a lash throws it.</summary>
            public const int Drums=11,Clamps=4;
            public const float CoilRadius=16,CoilDepth=4.5f,Spiral=2.5f,Breath=.35f,HeaveRise=3.2f,LashReach=4.5f;
            /// <summary>How far from her middle she still fights a hero (the shared controller stops at 30; her bar shows to 34), and how near a seat a hero on foot stands at its shelf.</summary>
            public const float Reach=38,ShelfReach=14;
            /// <summary>Sweep: each wedge's angle (narrowing), the gap between wedges (wider than two hulls, 8.7 m, from r 21 out), the wedges' reach from her middle, the beat between
            /// landings, the tell, the back-swing's tell (three clamps off), the blow.</summary>
            public static readonly float[] SweepWedge={34,30,26,22},SweepTell={1.6f,1.5f,1.4f,1.3f};
            public const float SweepGap=28,SweepReach=42,SweepStep=.3f,SweepBack=1.1f,SweepDamage=180;
            /// <summary>Wave: how many bands, each band's depth (narrowing), the inhale's tell; then the bands' spacing, speed, where they start (the coil's outer edge), how far they run, the
            /// tell each later band has at the coil before it goes, the blow.</summary>
            public static readonly int[] WaveCount={1,2,2,3};
            public static readonly float[] WaveBand={4,3.5f,3,2.5f},WaveTell={1.4f,1.3f,1.2f,1.1f};
            public const float WaveSpacing=1.2f,WaveSpeed=12,WaveFrom=19.5f,WaveReach=54,WaveFollowTell=.6f,WaveDamage=175;
            /// <summary>Lash: its lane's width (narrowing) and tell, where it starts out from her middle, its length, the blow and the shove.</summary>
            public static readonly float[] LashWidth={6,6,5.5f,5},LashTell={1.2f,1.2f,1.1f,1};
            public const float LashFrom=12,LashLength=32,LashDamage=185,LashShove=1.6f;
            /// <summary>Heave: its tell, the ring it surges under (to the seats at r 23), the blow and the shove.</summary>
            public const float HeaveTell=1.6f,HeaveInner=9,HeaveOuter=24.5f,HeaveDamage=185,HeaveShove=2.2f;
            /// <summary>Her slack after every move (she draws back: a held clamp eases), and her rest after it.</summary>
            public static readonly float[] Slack={3.2f,3,2.8f,2.6f},Rest={2.6f,2.4f,2.2f,2};
            /// <summary>How many of her slacks a clamp is held through before it opens, a notch each (BridleClamp.slacks), so a shelf is worked through one of her moves between
            /// them: on foot under her sweep, her wave or her heave, with the hand kept on the clamp where her marking leaves the seat in a gap and the tender kept off it. One slack each
            /// left her room four quick cycles, 101 to 104 s in the player against a bar from 120 (D364).</summary>
            public const int EaseSlacks=2;
            /// <summary>The Clamp Tenders that step out beside a seat as the party docks at it, by clamps off, and how far to each side.</summary>
            public static readonly int[] Tenders={1,1,2,2};
            public const float TenderBeside=2.6f;
            /// <summary>Hask's hum on the radio, every so often over the whole fight; a radio line after the party's own.</summary>
            public const float HumEvery=18,RadioAfter=2.2f;
            /// <summary>Her release (<see cref="MotherCoil"/>): the longest it takes (she is gone once her tail is up her road past its mouth: about 12 s from Sorrel's crater), the lift, the
            /// turn and its radius, how high she lifts, her speed up the road, where she goes with no BossExit, how far back the view may draw to hold her and the party in one frame while she goes, and the longest the
            /// quiet after her lasts if the zone's after-talk never comes.</summary>
            public const float ReleaseSeconds=16,LiftSeconds=1.4f,TurnSeconds=1.8f,TurnRadius=8,ReleaseRise=3.5f,GoSpeed=22,RoadIfNone=80,FramedFarthest=80,QuietAtMost=45;
        }
        /// <summary>A Clamp Tender's step out is a boundary, not a danger (D217): the clamps' cream.</summary>
        static readonly Color MotherTenderTone=new(1f,.93f,.74f,.4f);
        readonly List<BridleClamp> motherClamps=new();
        MotherCoil motherCoil;BridleClamp motherTendedAt;
        bool motherHears,motherSaidWide,motherSaidNarrow,motherStrainSaid;int motherSweeps,motherShelfSweeps,motherOpeningSaid=-1;float motherNextHum;
        readonly List<Telegraph> motherOne=new(1);
        List<Telegraph> MotherOnly(Telegraph marking){motherOne.Clear();motherOne.Add(marking);return motherOne;}
        /// <summary>Her room has her clamps in it: nobody can strike her and her bar is them. With none (a yard) she fights on integrity.</summary>
        public bool MotherRoom=>motherClamps.Count>0;
        public int MotherOpen{get{int n=0;foreach(var c in motherClamps)if(c!=null&&!MotherShutClamp(c))n++;return n;}}
        /// <summary>Still shut: its own state, or (before its Start has read the save) its flag kept from an earlier try (D285).</summary>
        static bool MotherShutClamp(BridleClamp clamp)=>clamp.State==BridleClamp.Hold.Shut&&!(clamp.keepsOpened&&!string.IsNullOrEmpty(clamp.openedFlag)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(clamp.openedFlag));
        int MotherShut=>motherClamps.Count-MotherOpen;
        /// <summary>How far on she is: clamps off in her room, her phase less one in a yard.</summary>
        public int MotherStage=>Mathf.Clamp(MotherRoom?MotherOpen:Phase-1,0,3);
        public MotherCoil MotherBody=>motherCoil;
        /// <summary>Her bar stops at the line of the clamps still shut: a blow (there is none in her room) is never a clamp.</summary>
        float MotherHoldFloor{get{int shut=MotherShut;return shut==0?0:health.maximum*(shut-1)/MotherTune.Clamps+1;}}

        void MotherStart()
        {
            motherCoil=GetComponent<MotherCoil>();brain.Rooted=true;MotherStrain.Straining=false;
            motherClamps.Clear();
            foreach(var clamp in BridleClamp.All)if(clamp!=null&&clamp.settledBy==health.id)motherClamps.Add(clamp);
            motherClamps.Sort((a,b)=>a.index.CompareTo(b.index));
            // Taken in order, 1 to 4: a shelf out of its turn says which comes first and takes nobody (World's Berth). Each eases over two of her slacks (D364).
            foreach(var clamp in motherClamps){clamp.inOrder=true;clamp.slacks=MotherTune.EaseSlacks;}
            BridleClamp.Opened+=MotherClampOpened;motherHears=true;
            int open=MotherOpen;
            if(MotherRoom)
            {
                // Nobody shoots at her: her bar is her clamps, a quarter each, and what was opened stays opened (D285): she wakes at the line of those still shut, in their phase, unshifted.
                health.Damageable=false;health.Holds=()=>MotherHoldFloor;
                if(open>0){health.integrity=Mathf.Max(1,health.maximum*(MotherTune.Clamps-open)/MotherTune.Clamps-1);HealthPhase=Phase=Mathf.Min(open+1,MotherTune.Clamps);}
                // Her raised head is drawn 16 m wide and 9 m out from her middle, and her capsule is 4 m: a ship under her snout was inside her head, and one just behind her or
                // under a horn was hidden from the fixed camera by it. She is solid at the plane as far as she is drawn there, so a hull stops where nothing of her can stand between
                // it and the lens. Under her body, so a shot, a marking and her wave's cover all pass it by (CombatCover.Opaque); it goes with her colliders as she goes.
                var solid=new GameObject("Her body at the plane");solid.transform.SetParent(transform,false);
                var shape=solid.AddComponent<CapsuleCollider>();shape.radius=MotherTune.Solid;shape.height=MotherTune.Solid*2+2;shape.center=Vector3.zero;
                // Behind her head her neck climbs from her coil to it, and from the south it hid a ship up to 14 m behind her (stills mother-c-04): nothing of her fight is there, so
                // she is solid behind her head as far as her neck stands over the plane.
                // Upright, as the one round her middle is: a hull rides a sphere held to the plane, and against a surface that isn't upright at its height (a ball, a capsule on its
                // side) each push let it rise a little, the plane drew it back down into the solid, and it ratcheted through (a hull reached 9.7 m behind her through both).
                foreach(float back in new[]{MotherTune.NeckSolidLength-MotherTune.NeckSolid,MotherTune.NeckSolid})
                {var neck=solid.AddComponent<CapsuleCollider>();neck.radius=MotherTune.NeckSolid;neck.height=MotherTune.NeckSolid*2+2;neck.center=Vector3.back*back;}
            }
            // Her fight is under way from her door: travel waits on it, and her length is timed from it.
            engaged=Time.realtimeSinceStartup;motherNextHum=Time.time+8;
            Debug.Log($"MOTHER_WAKES opened={open} clamps={motherClamps.Count}");
        }
        void MotherUpdate()
        {
            brain.Rooted=true;brain.DamageScale=1;
            if(MotherRoom)health.Damageable=false;
            // A conversation holds her: her next move waits as long as it did (her moves' own clocks are the fight's, GameTime.CombatHeld).
            if(GameServices.Current.Input.Blocked)nextSpecial+=Time.deltaTime;
            bool straining=MotherRoom&&MotherOpen>=MotherTune.Clamps-1&&!brain.Passive;
            if(motherCoil!=null)motherCoil.Straining=straining;
            if(straining){MotherStrain.Straining=true;if(!motherStrainSaid){motherStrainSaid=true;Debug.Log("MOTHER_STRAINS");}}
            var seat=DockedSeat();
            if(MotherRoom){BridleClamp.Offer(seat!=null&&!shifting&&!brain.Passive);BridleClamp.Slack=health.Broken&&!shifting;}
            if(seat!=null&&seat!=motherTendedAt){motherTendedAt=seat;MotherTenders(seat);}else if(seat==null)motherTendedAt=null;
            // Hask's hum under all of it, three rules in turn and round again.
            var party=PartyController.Current;
            if(!brain.Passive&&Time.time>=motherNextHum&&party!=null&&Flat(party.Active.transform.position-transform.position).magnitude<MotherTune.Reach+20)
            {motherNextHum=Time.time+MotherTune.HumEvery;MotherRadio("Hask","hail.hum");}
            // The first clamp's line is the party's ("Three to go"); past it the shift says nothing over the hold's own words and Iru's. Hushed only on the frame the shift begins (it
            // waits out her move and her slack first), so nothing else is lost.
            float d=party!=null?Flat(party.Active.transform.position-transform.position).magnitude:999;
            BarkService.Hush(this,shiftPending&&HealthPhase>2&&!brain.Passive&&!busy&&!health.Broken&&!GameServices.Current.Input.Blocked&&d<=MotherTune.Reach);
            MotherBeyond();
        }
        /// <summary>Her lanes run to r 34 and the shared controller chooses only within 30 m: past that, to her reach, she chooses for herself the same way.</summary>
        void MotherBeyond()
        {
            if(brain.Passive||busy||health.Broken||GameServices.Current.Input.Blocked||PartyController.Current==null)return;
            var hero=PartyController.Current.Active;float d=Flat(hero.transform.position-transform.position).magnitude;
            if(d<=30||d>MotherTune.Reach)return;
            if(shiftPending){shiftPending=false;Begin("PhaseShift",PhaseShift());return;}
            if(Time.time<nextSpecial)return;
            if(queuedMove!=null){var promised=queuedMove;queuedMove=null;if(Perform(promised))return;}
            var (move,routine)=ChooseMother(hero);if(routine!=null)Begin(move,routine);
        }
        /// <summary>The shut clamp whose shelf the lead stands on, on foot: the berth he is on, else (a yard's stand-in) the nearest seat within <see cref="MotherTune.ShelfReach"/>.</summary>
        public BridleClamp DockedSeat()
        {
            var party=PartyController.Current;if(party==null||party.Active==null||party.Active.flight)return null;
            var on=ZoneController.Current!=null?ZoneController.Current.BerthOn:null;var lead=party.Active.transform.position;
            BridleClamp best=null;float least=MotherTune.ShelfReach*MotherTune.ShelfReach;
            foreach(var clamp in motherClamps)
            {
                if(clamp==null||!MotherShutClamp(clamp)||clamp.inOrder&&clamp!=BridleClamp.NextOf(clamp.settledBy))continue;
                if(on!=null&&!on.Contains(clamp.transform.position+Vector3.up*.5f))continue;
                var o=clamp.transform.position-lead;o.y=0;if(o.sqrMagnitude<least){least=o.sqrMagnitude;best=clamp;}
            }
            return best;
        }
        void MotherClampOpened(BridleClamp clamp)
        {
            // A static event outlives a boss the scene took away.
            if(this==null){BridleClamp.Opened-=MotherClampOpened;return;}
            if(health==null||!health.Alive||!isActiveAndEnabled||!motherClamps.Contains(clamp))return;
            int open=MotherOpen;Debug.Log($"MOTHER_CLAMP_OPEN {clamp.index} opened={open}");
            // Kept the moment it is done (wiring 6.1 rule 3, D285): a defeat or a Continue never shuts it again, and nobody opens a clamp twice.
            SaveSystem.KeepCheckpoint();
            Tremor(.45f,.08f);AudioManager.PlayAt("joint_release",null,clamp.transform.position,.36f,AudioBus.SFX,40,.05f,.8f);
            if(open<MotherTune.Clamps)
            {
                // Her bar goes over the line that clamp kept, and the shift (she eases) follows.
                float line=health.maximum*(MotherTune.Clamps-open)/MotherTune.Clamps;if(health.integrity>line-1)health.integrity=Mathf.Max(1,line-1);
                if(open==1)MotherRadio("Iru","hail.hum",0);else if(open==3)MotherRadio("Iru","hail.hum",1);
                return;
            }
            // The fourth: every clamp lets go together, and she goes.
            StopAllCoroutines();Interrupt();busy=false;openingUntil=0;
            health.Holds=null;health.Plate=null;health.InvulnerableUntil=0;health.ShieldUntil=0;health.Damageable=true;
            var by=clamp.OpenedBy!=null?clamp.OpenedBy.Health:null;
            health.Receive(new DamagePacket{source=by,amount=health.integrity*10+1,type=health.weakness,weight=HitWeight.Light,tag="release"});
        }
        void MotherEases()=>Debug.Log($"MOTHER_EASES stage={MotherStage}");
        void MotherDown()
        {
            if(motherHears){BridleClamp.Opened-=MotherClampOpened;motherHears=false;}
            BridleClamp.Offer(false);BridleClamp.LetGoTogether();BridleClamp.Slack=false;MotherStrain.Straining=false;BarkService.Hush(this,false);
            // She isn't laid down or cleared away: she goes up her road, her whole body after her head.
            var presentation=GetComponent<DefeatPresentation>();if(presentation!=null)presentation.enabled=false;
            if(motherCoil!=null)motherCoil.Release();
        }
        /// <summary>One Clamp Tender (two from the third clamp) steps out beside the seat as the party docks at it, on a pale ring: it works the clamp shut, and the partner's job is to stop it.
        /// Not again while one of hers is up beside that seat.</summary>
        void MotherTenders(BridleClamp seat)
        {
            var definition=GameCatalog.Find<EnemyDef>("StillwaterTender");if(definition==null)return;
            summoned.RemoveAll(s=>s==null||!s.Health.Alive);
            foreach(var up in summoned)if(Flat(up.transform.position-seat.transform.position).magnitude<8)return;
            int n=MotherTune.Tenders[MotherStage];
            for(int i=0;i<n;i++)
            {
                var spot=seat.transform.position+seat.transform.right*((i%2==0?1:-1)*MotherTune.TenderBeside);
                var point=CombatCover.Ground(spot+Vector3.up*2);
                var ring=Mark(Telegraph.Circle(point,1.1f,1.2f,MotherTenderTone));ring.Release();
                var tender=ActorFactory.Enemy(definition,point+Vector3.up*.05f);summoned.Add(tender);
                CombatVfx.Burst(point+Vector3.up*.6f,new Color(1.2f,.9f,.5f),"kill");
            }
            Debug.Log($"MOTHER_TENDERS {n} at {seat.index}");
        }
        /// <summary>A voice on the radio after the party's own, on the party's clock (she may be gone by then).</summary>
        void MotherRadio(string speaker,string situation,int index=-1)
        {
            var party=PartyController.Current;if(party==null)return;party.StartCoroutine(Later());
            IEnumerator Later(){yield return new WaitForSeconds(MotherTune.RadioAfter);if(index>=0)BarkService.PlayAt(speaker,situation,index,true);else BarkService.Play(speaker,situation,true);}
        }
        string MotherStatus()
        {
            if(health==null||!health.Alive)return "";
            return health.Broken&&DockedSeat()!=null?"SHE GOES SLACK  ·  EASE THE CLAMP":"SHE IS FRIGHTENED  ·  OPEN A CLAMP";
        }
        string MotherGoal(string label)
        {
            if(health==null||!health.Alive||!MotherRoom)return label;
            int open=MotherOpen;
            var seat=DockedSeat();
            if(seat!=null)return seat.Eased>0?$"Hold it through her next slack ({Mathf.Min(open+1,MotherTune.Clamps)}/{MotherTune.Clamps})":$"Take its weight and wait for the slack ({Mathf.Min(open+1,MotherTune.Clamps)}/{MotherTune.Clamps})";
            return open>=MotherTune.Clamps-1?"Open the last clamp alongside her as she moves":"Fly the clamp's guns down, then dock";
        }

        // ---------------------------------------------------------------- choosing
        (string,IEnumerator) ChooseMother(CombatActor hero)
        {
            int k=MotherStage;
            string move=Pick(("Sweep",1.6f),("Wave",k==0?1.1f:1.3f),("Lash",k>=1?1.2f:0),("Heave",k>=3?1f:0));
            return move switch{"Wave"=>(move,MotherWave()),"Lash"=>(move,MotherLash()),"Heave"=>(move,MotherHeave()),_=>(move,MotherSweep())};
        }

        // ---------------------------------------------------------------- shared pieces of her moves
        /// <summary>The lead stands on a shelf: what she marks now is painted onto it, and reaches a hero standing there although she is a flier (D291's <see cref="Health.AnyForm"/>). A
        /// move marked at the plane before the party docked still passes a hero who has docked since, as D291 has it.</summary>
        bool MotherOnShelf{get{var party=PartyController.Current;var lead=party!=null?party.Active:null;return lead!=null&&!lead.flight;}}
        DamagePacket MotherPacket(float amount,DamageType type,bool shelf){var packet=Packet(amount,type);if(shelf)packet.tag=Health.AnyForm;return packet;}
        /// <summary>The shared <see cref="Strike"/>, with her packet.</summary>
        void MotherStrike(Telegraph marking,float amount,DamageType type,float stagger,float shove,HashSet<Health> once,bool landing,bool shelf)
        {
            marking.Release();
            foreach(var actor in PartyController.Current.members)
            {
                if(!actor.Health.Alive||!marking.Contains(actor.transform.position))continue;
                if(once!=null&&(landing?once.Contains(actor.Health):!once.Add(actor.Health)))continue;
                float dealt=actor.Health.Receive(MotherPacket(amount,type,shelf));
                if(dealt<=0)continue;
                if(landing)once.Add(actor.Health);
                if(stagger>0)actor.Stagger(stagger);
                if(shove>0)StartCoroutine(Shove(actor,Flat(actor.transform.position-marking.Origin),shove));
            }
        }
        /// <summary>The fight's own clock: still through a pause or a conversation (the markings' too), so nothing of hers lands the moment a talk ends.</summary>
        static float MotherDt=>GameTime.CombatHeld?0:Time.deltaTime;
        static float Bearing(Vector3 way)=>Mathf.Atan2(way.x,way.z)*Mathf.Rad2Deg;
        static Vector3 Heading(float bearing)=>Quaternion.Euler(0,bearing,0)*Vector3.forward;
        /// <summary>Where her markings lie: the floor of the shelf the lead stands on, else the plane at her root.</summary>
        float MotherFloor{get{var party=PartyController.Current;var lead=party!=null?party.Active:null;return lead!=null&&!lead.flight?lead.transform.position.y:transform.position.y;}}
        /// <summary>Her head turned toward a bearing (her body never turns: it is laid), within what her neck allows.</summary>
        void MotherLook(float bearing,float pitch=0)
        {
            if(visual==null)return;float yaw=Mathf.Clamp(Mathf.DeltaAngle(Bearing(transform.forward),bearing),-55,55);
            visual.localRotation=Quaternion.Euler(0,yaw,0)*visualRestRotation*Quaternion.Euler(pitch,0,0);
        }
        /// <summary>She draws back: her slack. A held clamp eases now (BridleClamp.Slack), and the first slack of each stage with the next clamp's guns down says so.</summary>
        IEnumerator MotherSlack()
        {
            int k=MotherStage;float seconds=MotherTune.Slack[k];
            Pose(8,-.6f);OpenFor(seconds);Debug.Log($"MOTHER_SLACK {seconds:0.0}");
            var party=PartyController.Current;
            if(MotherRoom&&motherOpeningSaid<k&&party!=null&&party.Active.flight&&GameServices.Current.Flags.GetBool("clear.Sorrel_Clamp"+Mathf.Min(MotherOpen+1,MotherTune.Clamps)))
            {motherOpeningSaid=k;BarkService.Play(party.Active.character,Situation("bossopening"),true);}
            for(float t=0;t<seconds;)
            {
                if(!health.Alive)yield break;
                // Held by a conversation, her slack is held with her: it is still there when the talk ends.
                if(GameTime.CombatHeld)health.Expose(Mathf.Max(.05f,seconds-t));else t+=Time.deltaTime;
                yield return null;
            }
            openingUntil=0;Pose(0);if(visual!=null)visual.localRotation=visualRestRotation;Finish(MotherTune.Rest[k]);
        }

        // ---------------------------------------------------------------- the moves
        IEnumerator MotherSweep()
        {
            int k=MotherStage;bool shelf=MotherOnShelf;var lead=PartyController.Current.Active.transform.position;
            float wedge=MotherTune.SweepWedge[k],step=wedge+MotherTune.SweepGap,tell=MotherTune.SweepTell[k];
            float aim=Bearing(Flat(lead-transform.position));
            // On a shelf every other sweep leaves the seat in a gap: a hand on the clamp may stay, or must let go, and the marking says which before it lands.
            var seat=DockedSeat();
            if(seat!=null)aim=Bearing(Flat(seat.transform.position-transform.position))+(motherShelfSweeps++%2==0?step*.5f:0);
            // Her head swings at her middle: every wedge fans from there, so the lanes between them widen from her coil out (10 m at r 21, where the first lane is).
            int dir=motherSweeps++%2==0?1:-1;var origin=transform.position;origin.y=MotherFloor;
            var wedges=new List<Telegraph>();var landAt=new List<float>();var bearings=new List<float>();
            void Wedge(float bearing,float lands){wedges.Add(Mark(Telegraph.Cone(origin,Heading(bearing),MotherTune.SweepReach,wedge,lands,Telegraph.Heavy)));landAt.Add(lands);bearings.Add(bearing);}
            for(int i=-1;i<=1;i++)Wedge(aim+i*step,tell+(i*dir+1)*MotherTune.SweepStep);
            // The fright in it, and later how much less there is.
            var party=PartyController.Current;
            if(k==0&&!motherSaidWide){motherSaidWide=true;BarkService.PlayAt(party.Active.character,"mother",0,true);}
            else if(k>0&&!motherSaidNarrow){motherSaidNarrow=true;BarkService.PlayAt(party.Active.character,"mother",1,true);}
            Debug.Log($"MOTHER_SWEEP wedge={wedge} stage={k}{(seat!=null?" shelf":"")}");
            AudioManager.PlayAt("forceField_000",transform,origin+Vector3.up*3,.28f,AudioBus.SFX,40,.05f,.55f);
            bool back=k>=3;float backAt=tell,last=tell+2*MotherTune.SweepStep;var once=new HashSet<Health>();var landed=new List<bool>{false,false,false};
            for(float t=0;;)
            {
                t+=MotherDt;if(Interrupted)yield break;
                // Three clamps off, she swings back: as the first pass lands, its gaps are marked.
                if(back&&t>=backAt)
                {
                    back=false;
                    for(int i=0;i<4;i++){int side=i<2?-1:1;float off=(i%2==0?.5f:1.5f)*step*side;Wedge(aim+off,t+MotherTune.SweepBack+i*MotherTune.SweepStep);landed.Add(false);}
                    last=t+MotherTune.SweepBack+3*MotherTune.SweepStep;
                }
                for(int i=0;i<wedges.Count;i++)
                {
                    if(landed[i]||t<landAt[i])continue;landed[i]=true;
                    MotherStrike(wedges[i],MotherTune.SweepDamage,DamageType.Kinetic,.3f,0,once,true,shelf);MotherLook(bearings[i],-4);
                    AudioManager.PlayAt("explosionCrunch_000",transform,origin+Heading(bearings[i])*12,.24f,AudioBus.SFX,50,.05f,.6f);
                }
                // Her head follows the wedge about to land.
                float soonest=float.PositiveInfinity;int next=-1;for(int i=0;i<wedges.Count;i++)if(!landed[i]&&landAt[i]<soonest){soonest=landAt[i];next=i;}
                if(next>=0)MotherLook(Mathf.LerpAngle(bearings[next],aim,.4f),-3*Mathf.Clamp01(t/tell));
                ThreatFrom(wedges);
                if(t>=last&&!back&&landed.TrueForAll(l=>l))break;
                yield return null;
            }
            strikeAt=0;yield return MotherSlack();
        }
        IEnumerator MotherWave()
        {
            int k=MotherStage;bool shelf=MotherOnShelf;int count=MotherTune.WaveCount[k];float depth=MotherTune.WaveBand[k],tell=MotherTune.WaveTell[k];
            var centre=transform.position;centre.y=MotherFloor;var from=MotherTune.WaveFrom;
            var bands=new List<(Telegraph mark,float leaves,HashSet<Health> met)>();var struck=new HashSet<Health>();int made=0;
            // Each band says it runs (Telegraph.Spreads): what reads the floor reads it coming, as a player sees it come.
            void Band(float leaves,float told){var mark=Mark(Telegraph.Ring(centre,from,from+depth,told,Telegraph.Heavy));mark.Spreads=MotherTune.WaveSpeed;bands.Add((mark,leaves,new HashSet<Health>()));made++;}
            // The inhale: the first band lies at her coil's edge, filling, before it goes.
            Band(tell,tell);if(motherCoil!=null)motherCoil.Heave(.25f);
            Debug.Log($"MOTHER_WAVE bands={count} depth={depth} stage={k}");
            AudioManager.PlayAt("root_groan",transform,transform.position+Vector3.up*3,.3f,AudioBus.SFX,40,.1f,.8f);
            // Cover is judged from over her middle down to the body: a rock at the plane, or a clamp's works between its seat and her, lies across that line.
            var cover=centre+Vector3.up*3;
            for(float t=0;made<count||bands.Count>0;)
            {
                t+=MotherDt;if(!health.Alive)yield break;
                if(made<count&&t>=tell+made*MotherTune.WaveSpacing-MotherTune.WaveFollowTell)Band(tell+made*MotherTune.WaveSpacing,MotherTune.WaveFollowTell);
                float soonest=float.PositiveInfinity;var lead=PartyController.Current.Active;float leadAt=Flat(lead.transform.position-centre).magnitude;
                // Behind cover a band never reaches him: nothing to dodge, and a hand on a sheltered clamp needn't let go (the companion and the driver read this).
                bool sheltered=!CombatCover.Clear(cover,lead.transform.position+Vector3.up*.8f,.2f);
                for(int i=bands.Count-1;i>=0;i--)
                {
                    var (mark,leaves,met)=bands[i];
                    if(t<leaves){if(!met.Contains(lead.Health)&&!sheltered)soonest=Mathf.Min(soonest,leaves-t+Mathf.Max(0,leadAt-from-depth)/MotherTune.WaveSpeed);continue;}
                    float outer=from+depth+(t-leaves)*MotherTune.WaveSpeed,inner=outer-depth;
                    if(inner>MotherTune.WaveReach){mark.Cancel();bands.RemoveAt(i);continue;}
                    if(!mark.Steady){mark.Steady=true;if(motherCoil!=null)motherCoil.Heave(0);AudioManager.PlayAt("forceField_000",transform,transform.position,.26f,AudioBus.SFX,50,.05f,.4f);}
                    mark.SetRing(inner,outer);
                    foreach(var actor in PartyController.Current.members)
                    {
                        if(!actor.Health.Alive||struck.Contains(actor.Health)||met.Contains(actor.Health))continue;
                        float d=Flat(actor.transform.position-centre).magnitude;
                        if(d<inner-Telegraph.BodyAllowance||d>outer+Telegraph.BodyAllowance)continue;
                        met.Add(actor.Health);
                        // Cover stops it: a rock in the lanes, the clamp's works in front of its seat.
                        if(!CombatCover.Clear(cover,actor.transform.position+Vector3.up*.8f,.2f)){Debug.Log("MOTHER_WAVE_COVERED "+actor.character);continue;}
                        if(actor.Health.Receive(MotherPacket(MotherTune.WaveDamage,DamageType.Pulse,shelf))>0){struck.Add(actor.Health);actor.Stagger(.3f);}
                    }
                    if(!met.Contains(lead.Health)&&!sheltered&&leadAt>outer)soonest=Mathf.Min(soonest,(leadAt-outer)/MotherTune.WaveSpeed);
                }
                strikeAt=float.IsPositiveInfinity(soonest)?0:Time.time+soonest;
                yield return null;
            }
            strikeAt=0;yield return MotherSlack();
        }
        IEnumerator MotherLash()
        {
            int k=MotherStage;bool shelf=MotherOnShelf;var lead=PartyController.Current.Active.transform.position;var way=Toward(lead);
            var origin=transform.position+way*MotherTune.LashFrom;origin.y=MotherFloor;float tell=MotherTune.LashTell[k];
            var lane=Mark(Telegraph.Line(origin,way,MotherTune.LashLength,MotherTune.LashWidth[k],tell,Telegraph.Heavy));
            AudioManager.PlayAt("thrusterFire_000",transform,origin,.26f,AudioBus.SFX,50,.05f,.55f);Debug.Log($"MOTHER_LASH stage={k}");
            for(float t=0;t<tell;){t+=MotherDt;if(Interrupted)yield break;if(motherCoil!=null)motherCoil.Lash(way,.15f*Mathf.Clamp01(t/tell));MotherLook(Bearing(way),-4*Mathf.Clamp01(t/tell));ThreatFrom(MotherOnly(lane));yield return null;}
            MotherStrike(lane,MotherTune.LashDamage,DamageType.Kinetic,.35f,MotherTune.LashShove,null,false,shelf);strikeAt=0;
            if(motherCoil!=null)motherCoil.Lash(way,1);Tremor(.35f,.08f);
            AudioManager.PlayAt("explosionCrunch_000",transform,origin+way*10,.28f,AudioBus.SFX,50,.05f,.7f);
            for(float t=0;t<.45f;){t+=MotherDt;yield return null;}
            if(motherCoil!=null)motherCoil.Lash(way,0);
            yield return MotherSlack();
        }
        IEnumerator MotherHeave()
        {
            bool shelf=MotherOnShelf;var centre=transform.position;centre.y=MotherFloor;
            var ring=Mark(Telegraph.Ring(centre,MotherTune.HeaveInner,MotherTune.HeaveOuter,MotherTune.HeaveTell,Telegraph.Heavy));
            AudioManager.PlayAt("district_lift",null,centre,.3f,AudioBus.SFX,40,.1f,.8f);Debug.Log("MOTHER_HEAVE");
            for(float t=0;t<MotherTune.HeaveTell;){t+=MotherDt;if(Interrupted)yield break;if(motherCoil!=null)motherCoil.Heave(.35f*Mathf.Clamp01(t/MotherTune.HeaveTell));Pose(-6*Mathf.Clamp01(t/MotherTune.HeaveTell),.4f);ThreatFrom(MotherOnly(ring));yield return null;}
            MotherStrike(ring,MotherTune.HeaveDamage,DamageType.Kinetic,.4f,MotherTune.HeaveShove,null,false,shelf);strikeAt=0;
            if(motherCoil!=null)motherCoil.Heave(1);Tremor(.6f,.12f);
            AudioManager.PlayAt("explosionCrunch_000",transform,centre,.32f,AudioBus.SFX,50,.05f,.5f);
            for(float t=0;t<.6f;){t+=MotherDt;yield return null;}
            if(motherCoil!=null)motherCoil.Heave(0);
            yield return MotherSlack();
        }
    }
}
