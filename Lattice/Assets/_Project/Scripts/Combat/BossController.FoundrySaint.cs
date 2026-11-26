using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The Foundry Saint, chapter 9's first boss: Cinder Halo's own separation press, older than anyone living, twelve metres of frame in the Separation Works' press hall, its six arms raised
    /// under a Compact override that makes it weld the districts back. The party doesn't kill it: they release it. Each arm is let go at its shoulder collar, comes down and lies still, and
    /// becomes the road up to the override rig on its back; with the rig broken the press settles under local control (<see cref="Ending.ShutDown"/>: it stays where it stands) and Orrin's voice
    /// comes out of its speaker. The room's contract is docs/maps/SeparationWorks.md, "Boss arena contract"; its arms are <see cref="SaintArm"/>s the room stands (or it makes its own).
    /// <list type="bullet">
    /// <item><b>Its arms hold it</b> (to two fifths). Its raised arms take the weight of every blow on the press: one takes nothing and reads as resisted (THE ARMS HOLD IT, said on screen the
    /// first time), except under the head's lift or a break, when the press is open. A collar is shut while its arm bears weight, and the first blow on a shut one says so on screen. Slammed down,
    /// an arm's collar unlocks for <see cref="SaintTune.SlamUnlock"/> seconds while it lies planted; under the head's lift every collar unlocks for <see cref="SaintTune.HeadUp"/>. A blow on an unlocked
    /// collar comes off the bar as a blow on the open press does. <b>The bar is paid in blows, and an arm decides which and when, never how much</b> (D272: a boss whose limbs take bar with them
    /// gave the replay party half again of flat out): each fifth the bar passes releases the arm whose collar was struck most, and a collar carries a fifth of the bar, so one struck through by
    /// itself goes at its line and no sooner. <b>An opening lets one arm go at most</b> (D364): the bar passes one arm's line in it and holds at the next, and once an arm has gone the
    /// others' collars shut until the press next opens; the third holds the rig's two fifths whole. <b>An opening takes a third of an arm at most</b> (D411,
    /// <see cref="SaintTune.OpeningShare"/>), in both halves (bare, the head's lift): three openings an arm, and an EXTRA in one is worth a third of an arm, not the whole arm it paid off in the
    /// short runs. A lift used to hand over the fight in whole lifts: an EXTRA under the head's lift took
    /// 32 and 41% of the bar, two arms and most of the rig's share (61 and 76 s in the player, 145 where the EXTRA met a held press). Three released, the rig is bare: the shift.</item>
    /// <item><b>The rig is bare</b>. A blow from the floor is resisted, except in the head's opening; a blow from up on a fallen arm lands, and twice over on the rig. Its last three arms sweep
    /// the roads instead of slamming: the top half of each road a sweep will cross is marked on the road itself, and a climber caught there is thrown off its side to the floor.</item>
    /// </list>
    /// Its moves: <b>Arm Slam</b> (the two raised arms nearest the lead come down on marked strips, and lie planted, their collars open), <b>Weld Line</b> (an arm drags its welding tip down a
    /// marked line through a hero on the floor: a travelling front, then a burning seam), <b>Head Drop</b> (the press rises and drops on a marked circle round itself, then lifts slowly: the
    /// opening, HEAD'S UP), and with the rig bare <b>Sweep</b>. It never walks or turns: it is bolted to the hall (a Mine to its brain, with an attack that reaches nobody).
    /// It fights with arms of its own in a room that has none (the proving yard), stood where the room's contract stands them.
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit FoundrySaintKit=Register(new Kit
        {
            // The epithet is the bar's, beside the name once the card has gone: "The Halo's own separation press, under somebody else's hand" ran 72 units past the bar's 900.
            id="FoundrySaint",title="THE FOUNDRY SAINT",epithet="The Halo's own press, under somebody else's hand",thresholds=new[]{SaintTune.Bare},
            choose=(boss,hero)=>boss.ChooseSaint(hero),
            moves={["ArmSlam"]=boss=>boss.SaintSlam(),["WeldLine"]=boss=>boss.SaintWeld(),["HeadDrop"]=boss=>boss.SaintHeadDrop(),["Sweep"]=boss=>boss.SaintSweep()},
            signature=phase=>phase>=2?"Sweep":null,
            shift=boss=>boss.SaintRigBare(),shiftText="THE RIG IS BARE  ·  BRACE",
            status=boss=>boss.SaintStatus(),
            fightGoal=(boss,label)=>boss.SaintGoal(label),
            // It settles, and OrrinSaintSpeaks follows from the press's own speaker: the shared "boss" bark would be said over him.
            capsuleRadius=SaintTune.Radius,occlusion=true,ending=Ending.ShutDown,downBark="",
            // Where the bed meets the frame, at the front, and the seal on its back between the middle collars: a hand proud of the 2.6 m capsule at 1.4 m, each reached by a blade from its edge.
            weakSpots=new[]{new Vector3(0,1.4f,-2.3f),new Vector3(-1.1f,1.4f,2.1f),new Vector3(1.1f,1.4f,2.1f)},
            start=boss=>boss.SaintStart(),update=boss=>boss.SaintUpdate(),down=boss=>boss.SaintDown(),
        });
        /// <summary>The Foundry Saint's numbers, all of them (its integrity is its EnemyDef's: HomeBuilder.Bosses). Damage is before the second phase's +18%; a boss stays at threat 1. Every move
        /// it has in both phases is authored at 161 to 163 (12.1% of Idra's bar at Sync 26 in the Choirwrought parts chapter 9's stalls sell, to 15.9% of Taren's at 25 in the Tempered parts it
        /// arrives in: HomeBuilder.Bosses' sizing), and the Sweep, which it has only with the rig bare, at 137 (162 with the +18%).</summary>
        public static class SaintTune
        {
            /// <summary>The room's own press once it has settled (HomeBuilder.Works: scenery shown by its flag from its fall), which goes dark with it (D419).</summary>
            public const string SettledName="The Foundry Saint, settled";
            /// <summary>Its capsule's radius (its definition makes it 12 m tall, the press's own height), and the share of its bar its arms hold: the shift is at two fifths, each arm a fifth.</summary>
            public const float Radius=2.6f,Bare=.4f,ArmShare=(1-Bare)/3;
            /// <summary>The most of its bar one opening may take (D411), in either half: a third of an arm, 9,333 of 140,000. With an arm an opening (D364) the short runs still freed an arm in
            /// every opening or two, an EXTRA paying off a whole one (94 and 109.6 s in the player, nov24-recon-C and nov24-whole s59). At half an arm a pair that fills every opening needs six
            /// for the arms, about 61 s at the code's opening every 10 s: the same 94 to 111 s. At a third it needs nine, and its fight is 124 s or more. A plain player's lift (about 3,800)
            /// and the length test's (.59 of flat out, about 6,200) never reach it.</summary>
            public const float OpeningShare=ArmShare/3;
            public const int Arms=6,ArmsToBare=3;
            /// <summary>Where its arms' collars stand when the room has none of its own: across its back (x), behind its middle, and their bearings from its front.</summary>
            public static readonly float[] ArmX={-4.3f,-2.6f,-.9f,.9f,2.6f,4.3f},ArmBearing={225,205,188,172,155,135};
            public const float ArmBack=2.8f;
            /// <summary>Its legs, solid: the drawn frame stands wider than its capsule, from the capsule's edge out to here, this deep. 5.8 across from its middle, not 5.05: its feet are drawn
            /// 5.78 m out at the floor on either side, and a hero stopped against the old frame stood .65 m inside them (D355, continued). Its depth is as it was: its arms' collars stand at its back.</summary>
            public const float LegInner=2.6f,LegOuter=5.8f,LegHalfDepth=2.45f,Height=12;
            /// <summary>A collar's integrity, as a share of the press's: the fifth its arm holds, so a collar struck through by itself goes at its line and never takes bar with it.</summary>
            public const float CollarShare=ArmShare;
            /// <summary>How long a slammed arm lies planted with its collar unlocked, and how long the head takes to lift after a drop (every collar unlocked, the press open). With the bar paid in
            /// blows these are the fight's length (HomeBossTests.SaintLength): 4 and 5, with a 2.6 s rest and a break every 900, left the arrival pair 137 s and Sela and Idra 114.</summary>
            public const float SlamUnlock=3.5f,HeadUp=4.5f;
            /// <summary>Arm Slam: the strip's length from the collar and its width, the tell held and bare, the blow, the shove.</summary>
            public const float SlamLength=7.4f,SlamWidth=2.4f,SlamTell=1.1f,SlamTellLater=.95f,SlamDamage=162,SlamShove=2.2f;
            /// <summary>Weld Line: its line, the tell held and bare, the front's pace, the blow, the seam it leaves burning (how long, how wide, its burn a second).</summary>
            public const float WeldLength=16,WeldWidth=2,WeldTell=1.2f,WeldTellLater=1.05f,WeldSpeed=18,WeldDamage=161,SeamSeconds=2.5f,SeamWidth=1.4f,SeamTick=30,WeldDip=12;
            /// <summary>Head Drop: the circle round its middle, the tell held and bare, the blow, the shove, how high the press rises before it drops.</summary>
            public const float HeadRadius=7.5f,HeadTell=1.4f,HeadTellLater=1.2f,HeadDamage=163,HeadShove=2.6f,HeadLift=1.1f;
            /// <summary>Sweep (bare only): the tell, the blow (162 with the +18%), how far either way of the sweeping arm its swing reaches, where on a road the marking starts (the share of the
            /// way up: about a metre over the floor), the marking's width, how far a climber is thrown the way it swings (at least, and at most while still over a fallen road).</summary>
            public const float SweepTell=1.3f,SweepDamage=137,SweepArc=62,SweepFrom=.42f,SweepWidth=1.6f,SweepThrow=2.8f,SweepThrowMost=7;
            /// <summary>How high over the floor a hero's feet must be for a blow to count as struck from up on the arms.</summary>
            public const float ClimbHeight=1;
            /// <summary>The rig on its back: its foot over the floor, out behind its middle, drawn size; the bar across the back (its height and reach); its target with the rig bare.</summary>
            public const float RigFoot=2.55f,RigOut=2.95f,RigDrawn=2.1f,BarHeight=3.9f,BarReach=4.7f;
            public static readonly Vector3 RigTarget=new(0,3.7f,-2.85f),RigTargetSize=new(9.6f,1.8f,1f);
            /// <summary>Its rest between moves, held and bare.</summary>
            public const float RestHeld=3,RestBare=2.3f;
        }
        static readonly Color SaintCyan=new(.45f,1.45f,1.6f,.95f),SaintWarm=new(1.3f,.82f,.5f,1),SaintSpark=new(1.5f,.9f,.45f,.9f),SaintDust=new(.55f,.52f,.48f);
        readonly List<SaintArm> saintArms=new();
        GameObject saintRigTarget,saintRig;LineRenderer saintBar;Renderer[] saintBody;MaterialPropertyBlock saintBlock;Color saintGlow=new(.5f,1.2f,1.3f);BoxCollider saintFrame;CharacterController saintOwn;
        /// <summary>The bar where the opening now under way began (D411), or less than nought with nothing open.</summary>
        float saintOpenedAt=-1;
        float saintFloor,saintRadius=SaintTune.Radius,saintStruckAt=-1,saintHold;bool saintHears,saintBared,saintCollarSaid,saintHeldSaid,saintFloorSaid;string saintOpenWhy="";
        readonly Dictionary<SaintArm,float> saintCollarLeft=new();
        /// <summary>Its arms let go so far (of six), and whether three are: the rig bare.</summary>
        public int SaintReleased{get{int n=0;foreach(var arm in saintArms)if(arm!=null&&arm.IsReleased)n++;return n;}}
        public bool SaintBare=>SaintReleased>=SaintTune.ArmsToBare;
        public IReadOnlyList<SaintArm> SaintArms=>saintArms;
        List<SaintArm> SaintStanding{get{var list=new List<SaintArm>();foreach(var arm in saintArms)if(arm!=null&&!arm.IsReleased)list.Add(arm);return list;}}
        List<SaintArm> SaintRoads{get{var list=new List<SaintArm>();foreach(var arm in saintArms)if(arm!=null&&arm.IsReleased)list.Add(arm);return list;}}
        /// <summary>The press open to a blow from anywhere: under the head's lift, or broken.</summary>
        bool SaintOpen=>health!=null&&health.Broken;
        float SaintRest=>SaintBare?SaintTune.RestBare:SaintTune.RestHeld;
        /// <summary>A hero up on the arms: feet a metre over the hall's floor (on a fallen arm's road, or in the air off one).</summary>
        bool SaintClimbing(Health hero)=>hero!=null&&hero.friendly&&hero.transform.position.y-saintFloor>=SaintTune.ClimbHeight;
        /// <summary>Its bearing from its own front (degrees clockwise) to a point.</summary>
        float SaintBearing(Vector3 point){var local=transform.InverseTransformPoint(point);return Mathf.Repeat(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg,360);}
        float ArmBearing(SaintArm arm)=>Mathf.Repeat(arm.Bearing-transform.eulerAngles.y,360);
        /// <summary>The bar at an arm's line: the k-th arm goes as the bar passes it.</summary>
        float SaintLine(int k)=>health.maximum*(1-k*SaintTune.ArmShare);
        /// <summary>The least its bar may come to now (D364: an opening lets one arm go at most, and the bar no further than that arm's line). With an arm gone in this opening, where the
        /// bar stood as it went; else just past the next arm's line (the third's is the rig's two fifths, kept whole), and no further than a third of an arm under where the opening
        /// began (D411). Bare, only the head's lift is held, at that third. Nought, none: held passive (its door, the shared flat-out measure), or bare with nothing held.</summary>
        float SaintHoldLine()
        {
            if(health==null||brain==null||brain.Passive)return 0;
            float opening=SaintOpeningLine();
            // A collar unlocked since is a new opening (the others were shut as the arm went, and nothing unlocks them until the press opens again).
            if(saintHold>0){foreach(var arm in saintArms)if(arm!=null&&!arm.IsReleased&&arm.Unlocked){saintHold=0;break;}}
            if(saintHold>0)return saintHold;
            if(SaintBare)return health.Broken?Mathf.Max(0,opening):0;
            return Mathf.Max(SaintLine(Mathf.Min(SaintReleased+1,SaintTune.ArmsToBare))-1,opening);
        }
        /// <summary>Something open to a blow: the press (the head's lift, a break), or a standing collar unlocked (a slam, the head's lift).</summary>
        bool SaintAnythingOpen()
        {
            if(health!=null&&health.Broken)return true;
            foreach(var arm in saintArms)if(arm!=null&&!arm.IsReleased&&arm.Unlocked)return true;
            return false;
        }
        /// <summary>The least the bar may come to in the opening under way (D411): a third of an arm under where it began, and a point more, so three full openings take the bar just past
        /// an arm's line, as the line itself stands (SaintLine less one). Nought with nothing open. Where an opening began is taken the first time it is asked or seen open (a blow is
        /// asked before it lands), and forgotten once nothing is: the log says what each took.</summary>
        float SaintOpeningLine()
        {
            if(health==null)return 0;
            if(!SaintAnythingOpen())
            {
                if(saintOpenedAt>=0){Debug.Log($"SAINT_OPENING {saintOpenedAt/health.maximum:0.000} to {health.integrity/health.maximum:0.000}");saintOpenedAt=-1;}
                return 0;
            }
            if(saintOpenedAt<0)saintOpenedAt=health.integrity;
            return saintOpenedAt-SaintTune.OpeningShare*health.maximum-1;
        }

        void SaintStart()
        {
            saintFloor=transform.position.y;saintRadius=GetComponent<CharacterController>().radius;brain.Rooted=true;
            saintArms.Clear();
            foreach(var arm in SaintArm.For(health.id))if(Flat(arm.transform.position-transform.position).magnitude<14)saintArms.Add(arm);
            // A room with none (the proving yard): its own, where the room's contract stands them.
            if(saintArms.Count==0)for(int i=0;i<SaintTune.Arms;i++)
            {
                var at=transform.TransformPoint(new Vector3(SaintTune.ArmX[i],SaintArm.Tune.CollarHeight,-SaintTune.ArmBack));
                // Not one of its temporary things: those go at every interruption, and an arm stays as long as the press does (SaintArm.Make keeps it to its maker).
                var arm=SaintArm.Make(health.id,i+1,at,transform.eulerAngles.y+SaintTune.ArmBearing[i],this);saintArms.Add(arm);
            }
            foreach(var arm in saintArms)
            {
                arm.press=transform;
                if(arm.IsReleased)continue;
                arm.Claim(health.maximum*SaintTune.CollarShare,health.weakness,health.resistance);saintCollarLeft[arm]=arm.Collar.integrity;
                arm.LetGo+=SaintArmLetGo;var held=arm;arm.Collar.Damaged+=(_,packet,amount)=>SaintCollarStruck(held,packet,amount);
                // A collar takes no more than the bar may give now (SaintHoldLine), so its number is what came off the bar.
                var collar=arm.Collar;collar.Holds=()=>{float hold=SaintHoldLine();if(hold<=0||health==null)return 0;return Mathf.Max(0,collar.integrity-Mathf.Max(0,health.integrity-hold));};
            }
            // Its frame, solid as drawn: ten metres across and five deep, wider than the capsule, and nobody walks into it. It was two legs either side of the round capsule, which left a
            // pocket between them behind each pair of middle collars a body could step into, and the arms' high ends, coming down, shut it (route 59's lead, carried in by his Extra,
            // stood there for the fight's last two minutes). As solid now as it is once it has settled, its back face a line across the collars. Its own body passes through it.
            // The hall's bake has floor under the press (its bed is flush, and the room's own settled press stands only once it has settled): its footprint is cut out of the paths for
            // as long as it stands, so the partner and anything else that walks by them goes round it, not into it.
            saintOwn=GetComponent<CharacterController>();
            var frame=new GameObject("Saint frame");frame.transform.SetParent(transform,false);frame.transform.localPosition=Vector3.up*SaintTune.Height*.5f;
            saintFrame=frame.AddComponent<BoxCollider>();saintFrame.size=new Vector3(SaintTune.LegOuter*2,SaintTune.Height,SaintTune.LegHalfDepth*2);
            if(saintOwn!=null)Physics.IgnoreCollision(saintOwn,saintFrame,true);
            var hole=frame.AddComponent<UnityEngine.AI.NavMeshObstacle>();hole.shape=UnityEngine.AI.NavMeshObstacleShape.Box;hole.size=saintFrame.size;hole.center=Vector3.zero;hole.carving=true;hole.carveOnlyStationary=false;
            // The rig on its back, and the bar its cables run from to every arm still raised.
            saintRig=brain.definition!=null&&brain.definition.tailSegment!=null?Instantiate(brain.definition.tailSegment,transform,false):MadeRig();
            saintRig.name="Override rig (drawn)";foreach(var collider in saintRig.GetComponentsInChildren<Collider>())Destroy(collider);
            saintRig.transform.localPosition=new Vector3(0,SaintTune.RigFoot,-SaintTune.RigOut);saintRig.transform.localRotation=Quaternion.Euler(0,180,0);
            saintRig.transform.localScale*=SaintTune.RigDrawn/1.4f;
            var bar=new GameObject("Override bar");bar.transform.SetParent(transform,false);saintBar=bar.AddComponent<LineRenderer>();saintBar.useWorldSpace=false;saintBar.positionCount=2;
            saintBar.SetPosition(0,new Vector3(-SaintTune.BarReach,SaintTune.BarHeight,-SaintTune.RigOut));saintBar.SetPosition(1,new Vector3(SaintTune.BarReach,SaintTune.BarHeight,-SaintTune.RigOut));
            saintBar.startWidth=saintBar.endWidth=.22f;saintBar.sharedMaterial=ActorFactory.Energy(SaintCyan);saintBar.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach(var arm in saintArms){var local=transform.InverseTransformPoint(arm.transform.position);arm.Cable(transform.TransformPoint(new Vector3(Mathf.Clamp(local.x,-SaintTune.BarReach,SaintTune.BarReach),SaintTune.BarHeight,-SaintTune.RigOut)));}
            // What a blow from up on the arms strikes once the rig is bare: twice over, as any weak spot. Off until then, so nothing reaches it before.
            saintRigTarget=new GameObject("Override rig");saintRigTarget.transform.SetParent(transform,false);saintRigTarget.transform.localPosition=SaintTune.RigTarget;
            var shape=saintRigTarget.AddComponent<BoxCollider>();shape.isTrigger=true;shape.size=SaintTune.RigTargetSize;
            var target=saintRigTarget.AddComponent<Hurtbox>();target.owner=health;target.multiplier=2;saintRigTarget.SetActive(false);
            // The bar is paid in blows, an arm's line an opening at most (SaintHoldLine). The plate is the arms' weight (SaintPlate).
            health.Holds=SaintHoldLine;health.Plate=SaintPlate;saintHold=0;
            health.Damaged+=SaintStruck;saintHears=true;
            saintBody=visual!=null?visual.GetComponentsInChildren<Renderer>():new Renderer[0];saintBlock??=new MaterialPropertyBlock();
            foreach(var body in saintBody)if(body!=null&&body.sharedMaterial!=null&&body.sharedMaterial.HasProperty("_EmissionColor")){saintGlow=body.sharedMaterial.GetColor("_EmissionColor");break;}
            Debug.Log($"SAINT_WAKES arms={saintArms.Count}");
        }
        GameObject MadeRig()
        {
            var rig=GameObject.CreatePrimitive(PrimitiveType.Cube);rig.transform.localScale=new Vector3(1.2f,1.4f,.8f);
            rig.GetComponent<Renderer>().sharedMaterial=ActorFactory.Energy(SaintCyan);rig.transform.SetParent(transform,false);return rig;
        }
        /// <summary>What a blow lets through: with its arms raised nothing on the press (their weight takes it; read as resisted), while the override fights them; with the rig bare, only a blow
        /// struck from up on the arms. An opening (the head's lift, a break) lets anything through (Health). A press held passive (at its door, or measured flat out by the shared length test)
        /// stands open: its arms bear a blow only while the override drives them, and what the party can deal an open body is what the fight's share is counted against.</summary>
        float SaintPlate(DamagePacket packet)
        {
            if(!SaintBare)return brain!=null&&brain.Passive?1:0;
            return SaintClimbing(packet.source)?1:0;
        }
        void SaintStruck(Health _,DamagePacket packet,float amount)
        {
            if(packet.source==null||!packet.source.friendly)return;
            if(engaged<0)engaged=Time.realtimeSinceStartup;
            if(amount>0){saintStruckAt=Time.time;return;}
            // A blow that did nothing says why, once: the arms hold it, or (the rig bare) it was struck from the floor.
            if(SaintBare&&!saintFloorSaid&&!SaintClimbing(packet.source))
            {
                saintFloorSaid=true;Debug.Log("SAINT_FLOOR_SHRUGS");
                Notices.Post("THE RIG IS ON ITS BACK","From the floor the press shrugs a blow off. Climb a fallen arm: from the top of one a blade reaches the rig, and it lands twice over.");
            }
            else if(!SaintBare&&!saintHeldSaid)
            {
                saintHeldSaid=true;Debug.Log("SAINT_HELD");
                Notices.Post("THE ARMS HOLD IT","Its raised arms take the weight of every blow on the press. Strike a shoulder collar, the gold ring where an arm meets it, while it glows amber: after a slam, or while its head lifts, when the press takes blows too.");
            }
        }
        /// <summary>A blow on a collar. Unlocked, what it took comes off the bar as a blow on the open press would (the collar is the press's own); shut, it took nothing, and the first such blow
        /// says why on screen.</summary>
        void SaintCollarStruck(SaintArm arm,DamagePacket packet,float amount)
        {
            if(packet.source==null||!packet.source.friendly||health==null||!health.Alive)return;
            if(engaged<0)engaged=Time.realtimeSinceStartup;
            float before=saintCollarLeft.TryGetValue(arm,out var was)?was:arm.Collar.maximum,lost=Mathf.Max(0,before-arm.Collar.integrity);saintCollarLeft[arm]=arm.Collar.integrity;
            // Never the last of it: the rig is bare by two fifths, and only the rig ends the fight. Nor past the line an opening stops at (SaintHoldLine).
            if(lost>0){health.integrity=Mathf.Min(health.integrity,Mathf.Max(Mathf.Max(1,SaintHoldLine()),health.integrity-lost));saintStruckAt=Time.time;return;}
            if(amount>0||arm.Unlocked||saintCollarSaid)return;
            // The fault the brief warned of: hitting a collar and wondering why nothing happens. The first time, it is said on screen.
            saintCollarSaid=true;Debug.Log("SAINT_COLLAR_SHUT "+arm.index);
            Notices.Post("THE COLLAR IS SHUT","Its arm's weight is on the collar, and a blow does nothing. When the Saint slams an arm down, or drops its head and lifts it, the weight comes off: the collar glows amber, and a blow on it then counts.");
        }
        /// <summary>An arm let go: as the bar passes its line (the arm whose collar was struck most), or its collar struck through by itself (a fifth of the bar on it, so at its line), or the press
        /// settling. Nothing more comes off the bar for it than the blows it took. <paramref name="settling"/>: let go with no blow landing (the press settling, or a bar a test set past its
        /// line), which plays nothing.</summary>
        void SaintArmLetGo(SaintArm arm,bool settling)
        {
            if(this==null||health==null||!health.Alive)return;
            int released=SaintReleased;if(engaged<0)engaged=Time.realtimeSinceStartup;
            if(!settling)
            {
                BarkService.PlayAt(PartyController.Current.Active.character,"saint.arm",Mathf.Clamp(released-1,0,2),true);
                Tremor(.4f,.1f);
            }
            Debug.Log($"SAINT_ARM_RELEASED {arm.index} released={released}{(settling?" by=bar":"")}");
            // One arm an opening (D364): its weight comes onto the others, their collars shut, and the bar holds where it stood as the arm went (at the third, the rig's two fifths
            // kept whole) until the press opens again.
            if(!settling&&brain!=null&&!brain.Passive)
            {
                saintHold=Mathf.Max(1,health.integrity);
                foreach(var other in SaintStanding)other.Lock();
                Debug.Log($"SAINT_HOLDS {saintHold/health.maximum:0.00}");
            }
            if(released>=SaintTune.ArmsToBare)SaintBareNow();
        }
        /// <summary>The rig bare: its target opens to blows from the arms, the arms still raised stop answering at their collars (they sweep now), and the bar and cables say so.</summary>
        void SaintBareNow()
        {
            if(saintBared)return;saintBared=true;
            if(saintRigTarget!=null)saintRigTarget.SetActive(true);
            foreach(var arm in saintArms)if(arm!=null&&!arm.IsReleased)arm.Shut();
            // Blows shrugged off from the floor still fill a break meter, and a break lets anything through: bare, its one opening is the head's lift, and the climb is the way to the rig.
            health.breakThreshold=1e9f;
            Debug.Log("SAINT_RIG_BARE");
        }
        void SaintRigBare(){SaintBareNow();}
        void SaintUpdate()
        {
            brain.Rooted=true;
            // Its frame is inside its own capsule: an ignored pair is forgotten whenever either side is switched off, so it is asked again each frame (a call, nothing more).
            if(saintFrame!=null&&saintOwn!=null)Physics.IgnoreCollision(saintOwn,saintFrame,true);
            // Each fifth the bar passes lets go the arm whose collar was struck most (one nobody struck goes in order, from the west). Set past a line with no blow landing (a test), quietly.
            if(!SaintBare)
            {
                int crossed=0;for(int k=1;k<=SaintTune.ArmsToBare;k++)if(health.integrity<health.maximum*(1-k*SaintTune.ArmShare)-.5f)crossed++;
                bool struck=saintStruckAt>=0&&Time.time-saintStruckAt<.5f;
                for(int guard=0;SaintReleased<crossed&&guard<SaintTune.Arms;guard++)
                {
                    SaintArm most=null;float least=float.PositiveInfinity;
                    foreach(var arm in SaintStanding){float left=arm.Collar.integrity/Mathf.Max(1,arm.Collar.maximum);if(left<least-.001f){least=left;most=arm;}}
                    if(most==null)break;most.Release(!struck);
                }
            }
            // The opening an arm went in is over (nothing unlocked, the press not open): the hold lifts, and the next opening may let the next arm go.
            if(saintHold>0&&!health.Broken){saintHold=0;Debug.Log("SAINT_HOLD_LIFTS");}
            // Where the opening under way began, kept or forgotten each frame (D411); held passive, none is.
            if(!brain.Passive)SaintOpeningLine();else saintOpenedAt=-1;
            if(SaintBare)SaintBareNow();
            // Broken (its meter filled by blows on the press), it reels and its arms' weight comes off their collars as the head's lift takes it off: struck enough, the press opens them too.
            // Not in the opening an arm has just gone in: their collars stay shut under its weight (D364).
            else if(health.Broken&&saintHold<=0)
            {
                if(Time.time>=openingUntil)saintOpenWhy="IT REELS";
                foreach(var arm in SaintStanding)arm.Unlock(Mathf.Max(.1f,health.BrokenUntil-Time.time));
            }
            SaintLooks(1+.18f*Mathf.Sin(Time.time*2.3f));
            if(busy||health.Broken||brain.Passive)return;
            // A press holding six booms up: it hums, it does not walk or turn.
            Pose(.35f*Mathf.Sin(Time.time*.9f),.03f*Mathf.Sin(Time.time*1.7f));
        }
        /// <summary>The override's cyan through the press, breathing; warm once it has settled.</summary>
        void SaintLooks(float pulse,bool warm=false)
        {
            if(saintBody==null)return;saintBlock??=new MaterialPropertyBlock();
            foreach(var body in saintBody){if(body==null)continue;body.GetPropertyBlock(saintBlock);saintBlock.SetColor("_EmissionColor",warm?SaintWarm*.55f:saintGlow*pulse);body.SetPropertyBlock(saintBlock);}
            if(saintBar!=null&&warm)saintBar.enabled=false;
        }
        void SaintDown()
        {
            if(saintHears){health.Damaged-=SaintStruck;saintHears=false;}
            health.Holds=null;health.Plate=null;
            // It settles where it stands: no fall, no shrink. The body stays (its presentation is stopped), solid as the room's press is solid.
            var presentation=GetComponent<DefeatPresentation>();if(presentation!=null)presentation.enabled=false;
            // Nobody is left inside it as it settles (route 59, November 14). Its frame has been solid since the fight began, so only a body put there could be; whoever is is stepped out onto the floor at its
            // back, toward the camera, before the room's settled press stands on the same ground (route 59's lead, pushed out of it under an arm's high end, never moved again). Its last
            // arms clear whoever they come down on as they come to rest (SaintArm).
            var back=-transform.forward;back.y=0;back=back.sqrMagnitude>.0001f?back.normalized:Vector3.back;
            var middle=transform.position+Vector3.up*SaintTune.Height*.5f;var half=new Vector3(SaintTune.LegOuter,SaintTune.Height*.5f,SaintTune.LegHalfDepth);
            if(PartyController.Current!=null)foreach(var member in PartyController.Current.members)
            {
                var body=member!=null?member.GetComponent<CharacterController>():null;
                if(body!=null&&SaintArm.Holds(middle,transform.rotation,half,body,member.transform.position,-.05f))SaintArm.StandClear(member,saintFloor,transform.position+back*SaintTune.LegHalfDepth,back,null,"the press settles");
            }
            var settled=new GameObject("Foundry Saint, settled");settled.transform.SetPositionAndRotation(transform.position,transform.rotation);
            var core=settled.AddComponent<BoxCollider>();core.center=Vector3.up*SaintTune.Height*.5f;core.size=new Vector3(SaintTune.LegInner*2,SaintTune.Height,SaintTune.LegHalfDepth*2);
            foreach(float side in new[]{-1f,1f}){var leg=settled.AddComponent<BoxCollider>();leg.center=new Vector3(side*(SaintTune.LegInner+SaintTune.LegOuter)*.5f,SaintTune.Height*.5f,0);leg.size=new Vector3(SaintTune.LegOuter-SaintTune.LegInner,SaintTune.Height,SaintTune.LegHalfDepth*2);}
            // The rig breaks and falls off its back; the last arms come down and lie still; the cyan goes out and its own warm lamps come on.
            if(saintRig!=null&&PartyController.Current!=null)PartyController.Current.StartCoroutine(SaintRigFalls(saintRig.transform,transform.position.y));
            if(saintRigTarget!=null)saintRigTarget.SetActive(false);
            foreach(var arm in saintArms)if(arm!=null){arm.Release(true);arm.LetGo-=SaintArmLetGo;}
            // D385: its cyan out and no warm lamps lit over the darkening (they lit it on the frame it settled): the override bar goes, and its body goes dark with its arms.
            if(saintBar!=null)saintBar.enabled=false;
            // D385: released is powered down, the whole machine: the press and all six arms dark at once, their lamps out, the arms down and still. Its arms are the room's and not under
            // its root, so its own darkening never reached them: after the win they lay in full gold and cream through the loot lines and Orrin's call, the brightest thing in the hall
            // (nov23-whole s59/038, 043). They stay dark for the rest of the visit, as the press does.
            foreach(var arm in saintArms)if(arm!=null)arm.PowerDown();
            // D419: the room's own press (scenery from its fall, shown by the flag this end has just set) stands where this body stands, in its colours: its cream panels and a
            // white shoulder block showed through the dark body through Orrin's call (nov25-r5-chain s59/053, 076). Not drawn while this body stands; dark on the visits after.
            BodyGoesDark.StandIn(SaintTune.SettledName,false,gameObject);
            if(visual!=null){visual.localPosition=visualRest;visual.localRotation=visualRestRotation;}
            Debug.Log("SAINT_SETTLES");
        }
        static IEnumerator SaintRigFalls(Transform rig,float floor)
        {
            rig.SetParent(null,true);var from=rig.position;var outward=-rig.forward;outward.y=0;outward=outward.sqrMagnitude>.01f?-outward.normalized:Vector3.back;var spin=Random.onUnitSphere*160;
            CombatVfx.Burst(from,new Color(.6f,1.3f,1.4f),"crack");AudioManager.PlayAt("impactMetal_000",null,from,.34f,AudioBus.SFX,50,.05f,.6f);
            for(float t=0;t<1.4f&&rig!=null;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;
                rig.position=from+outward*(1.4f*t)+Vector3.up*(.8f*t-4.9f*t*t);rig.Rotate(spin*Time.deltaTime,Space.World);
                if(rig.position.y<floor+.3f)break;
                yield return null;
            }
            if(rig!=null){CombatVfx.Burst(rig.position,new Color(.5f,.5f,.52f),"kill");AudioManager.PlayAt("explosionCrunch_000",null,rig.position,.24f,AudioBus.SFX,50,.05f,1.2f);Object.Destroy(rig.gameObject);}
        }
        string SaintStatus()
        {
            if(health==null||!health.Alive)return "";
            if(!SaintBare)
            {
                foreach(var arm in saintArms)if(arm!=null&&arm.Unlocked)return saintOpenWhy+"  ·  RELEASE A COLLAR";
                return "THE ARMS HOLD IT  ·  RELEASE AN ARM AT THE SHOULDER";
            }
            if(health.Broken&&Time.time<openingUntil)return "HEAD'S UP  ·  STRIKE";
            return "THE RIG IS ON ITS BACK  ·  CLIMB THE FALLEN ARMS";
        }
        string SaintGoal(string label)
        {
            if(health==null||!health.Alive||saintArms.Count==0)return label;
            return SaintBare?"Climb the fallen arms to the rig on its back":$"Release an arm at its shoulder collar ({Mathf.Min(SaintReleased,SaintTune.ArmsToBare)}/{SaintTune.ArmsToBare})";
        }

        // ---------------------------------------------------------------- moves
        (string,IEnumerator) ChooseSaint(CombatActor hero)
        {
            var local=transform.InverseTransformPoint(hero.transform.position);bool behind=local.z<0;string move;
            // Its slams and its head are its openings; a weld runs only where its arms reach (its back). Bare, it sweeps whoever climbs and welds whoever doesn't.
            if(!SaintBare)move=Pick(("ArmSlam",behind&&SaintStanding.Count>0?2f:.2f),("WeldLine",behind?1.3f:0),("HeadDrop",1.4f));
            else{bool climbing=false;foreach(var member in PartyController.Current.members)if(member.Health.Alive&&SaintClimbing(member.Health))climbing=true;
                move=Pick(("Sweep",climbing?2.2f:.6f),("WeldLine",SaintOnTheFloor()!=null?1.4f:.2f),("HeadDrop",1.2f));}
            return move switch{"ArmSlam"=>(move,SaintSlam()),"WeldLine"=>(move,SaintWeld()),"Sweep"=>(move,SaintSweep()),_=>(move,SaintHeadDrop())};
        }
        /// <summary>The hero a weld goes for with the rig bare: whoever is on the floor, the lead first.</summary>
        CombatActor SaintOnTheFloor()
        {
            var party=PartyController.Current;if(party==null)return null;
            if(party.Active!=null&&party.Active.Health.Alive&&!SaintClimbing(party.Active.Health))return party.Active;
            foreach(var member in party.members)if(member.Health.Alive&&!SaintClimbing(member.Health))return member;
            return null;
        }
        IEnumerator SaintSlam()
        {
            var lead=PartyController.Current.Active.transform.position;float toward=SaintBearing(lead);
            var standing=SaintStanding;if(standing.Count==0){Finish(.6f);yield break;}
            standing.Sort((a,b)=>Mathf.Abs(Mathf.DeltaAngle(ArmBearing(a),toward)).CompareTo(Mathf.Abs(Mathf.DeltaAngle(ArmBearing(b),toward))));
            var slamming=standing.GetRange(0,Mathf.Min(2,standing.Count));
            float tell=Phase>=2?SaintTune.SlamTellLater:SaintTune.SlamTell;var strips=new List<Telegraph>();
            foreach(var arm in slamming){strips.Add(Mark(Telegraph.Line(arm.CollarFloor,arm.Outward,SaintTune.SlamLength,SaintTune.SlamWidth,tell,Telegraph.Heavy)));arm.SwingTo(SaintArm.Tune.LiftedPitch,tell*.8f);}
            ThreatFrom(strips);
            AudioManager.PlayAt("spaceEngineLow_000",transform,transform.position+Vector3.up*4,.28f,AudioBus.SFX,60,.05f,.55f);
            for(float t=0;t<tell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                ThreatFrom(strips);Pose(-2*Mathf.Clamp01(t/tell),.08f*Mathf.Clamp01(t/tell));yield return null;
            }
            // Down on the strips, and planted: the weight off their collars while they lie.
            foreach(var arm in slamming)arm.Plant(SaintTune.SlamUnlock);
            var once=new HashSet<Health>();foreach(var strip in strips)Strike(strip,SaintTune.SlamDamage,DamageType.Kinetic,.35f,SaintTune.SlamShove,once);
            Tremor(.5f,.12f);AudioManager.PlayAt("explosionCrunch_000",transform,lead,.3f,AudioBus.SFX,60,.05f,.7f);
            foreach(var arm in slamming)for(int i=1;i<=4;i++)CombatVfx.Burst(arm.CollarFloor+arm.Outward*(SaintTune.SlamLength*i/4.5f)+Vector3.up*.3f,SaintDust,"kill");
            strikeAt=0;Pose(0);
            saintOpenWhy="ITS ARMS ARE DOWN";Debug.Log("SAINT_UNLOCKED "+string.Join(",",slamming.ConvertAll(a=>a.index.ToString())));
            // Holding them down takes all it has: nothing else comes while they lie.
            for(float t=0;t<SaintTune.SlamUnlock;){if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;yield return null;}
            Finish(SaintRest);
        }
        IEnumerator SaintWeld()
        {
            var target=SaintBare?SaintOnTheFloor()??PartyController.Current.Active:PartyController.Current.Active;var at=target.transform.position;
            SaintArm arm=null;float nearest=float.PositiveInfinity;
            foreach(var standing in SaintStanding){float d=Flat(standing.HandFloor-at).sqrMagnitude;if(d<nearest){nearest=d;arm=standing;}}
            if(arm==null){Finish(.6f);yield break;}
            var origin=arm.HandFloor;var direction=Flat(at-origin);direction=direction.sqrMagnitude>.04f?direction.normalized:arm.Outward;
            float length=SaintTune.WeldLength;if(CombatCover.Sweep(origin+Vector3.up*.6f,origin+Vector3.up*.6f+direction*length,.3f,out var wall))length=Mathf.Max(2,wall.distance);
            float tell=Phase>=2?SaintTune.WeldTellLater:SaintTune.WeldTell;
            var lane=Mark(Telegraph.Line(origin,direction,length,SaintTune.WeldWidth,tell,Telegraph.Threat));
            arm.SwingTo(SaintTune.WeldDip,tell);
            float AlongOf(Vector3 p)=>Vector3.Dot(Flat(p-origin),direction);
            void Threat(float front){var lead=PartyController.Current.Active.transform.position;float along=AlongOf(lead);strikeAt=lane.Contains(lead)&&along>=front?Time.time+(along-front)/SaintTune.WeldSpeed:0;}
            AudioManager.PlayAt("forceField_000",arm.transform,origin+Vector3.up,.24f,AudioBus.SFX,50,.05f,.6f);
            for(float t=0;t<tell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                var lead=PartyController.Current.Active.transform.position;strikeAt=lane.Contains(lead)?Time.time+(tell-t)+Mathf.Max(0,AlongOf(lead))/SaintTune.WeldSpeed:0;
                if(Time.frameCount%5==0)CombatVfx.Burst(origin+Vector3.up*.3f,SaintSpark,"steam");
                yield return null;
            }
            // The weld runs down the line: whoever is in it when the front reaches him is struck, once.
            lane.Steady=true;var hit=new HashSet<Health>();Debug.Log("SAINT_WELD");
            AudioManager.PlayAt("laserSmall_000",arm.transform,origin+Vector3.up,.26f,AudioBus.SFX,50,.05f,.5f);
            for(float front=0;front<length+.5f;)
            {
                if(GameTime.Paused){yield return null;continue;}
                if(Interrupted)yield break;
                front+=SaintTune.WeldSpeed*Time.deltaTime;Threat(front);
                foreach(var member in PartyController.Current.members)
                {
                    if(!member.Health.Alive||hit.Contains(member.Health)||!lane.Contains(member.transform.position))continue;
                    float along=AlongOf(member.transform.position);if(along>front||along<front-2.5f)continue;
                    hit.Add(member.Health);float dealt=member.Health.Receive(Packet(SaintTune.WeldDamage,DamageType.Plasma));if(dealt>0)member.Stagger(.25f);
                }
                if(Time.frameCount%2==0)CombatVfx.Burst(origin+direction*Mathf.Min(front,length)+Vector3.up*.25f,SaintSpark,"steam");
                yield return null;
            }
            lane.Release();strikeAt=0;
            // What it welded burns a moment along the seam, then cools.
            LingeringArea.LayLine(origin,direction,length,SaintTune.SeamWidth,SaintTune.SeamSeconds,AreaEffect.Burn,1,SaintTune.SeamTick,health,DamageType.Plasma);
            arm.SwingTo(SaintArm.Tune.RaisedPitch,.7f);
            Finish(SaintRest);
        }
        IEnumerator SaintHeadDrop()
        {
            float tell=Phase>=2?SaintTune.HeadTellLater:SaintTune.HeadTell;
            var ring=Mark(Telegraph.Circle(transform.position,saintRadius+SaintTune.HeadRadius-SaintTune.Radius,tell,Telegraph.Heavy));
            var marks=new List<Telegraph>{ring};ThreatFrom(marks);
            AudioManager.PlayAt("spaceEngineLow_000",transform,transform.position+Vector3.up*8,.3f,AudioBus.SFX,60,.05f,.45f);
            // The press rises in its frame through the tell, trembling at the top.
            for(float t=0;t<tell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/tell));Pose(0,SaintTune.HeadLift*k,.05f*k*k);ThreatFrom(marks);yield return null;
            }
            Pose(0,-.25f);Strike(ring,SaintTune.HeadDamage,DamageType.Kinetic,.45f,SaintTune.HeadShove);Tremor(.65f,.15f);strikeAt=0;
            AudioManager.PlayAt("explosionCrunch_000",transform,transform.position,.34f,AudioBus.SFX,60,.05f,.6f);
            for(int i=0;i<8;i++)CombatVfx.Burst(transform.position+Quaternion.Euler(0,i*45,0)*Vector3.forward*(SaintTune.HeadRadius*.7f)+Vector3.up*.3f,SaintDust,"kill");
            // HEAD'S UP: it lifts slowly back into its frame, open, and while it does the weight is off every shoulder.
            Debug.Log("SAINT_HEAD_UP");saintOpenWhy="HEAD'S UP";
            if(!SaintBare){var opened=new List<string>();foreach(var arm in SaintStanding){arm.Unlock(SaintTune.HeadUp);opened.Add(arm.index.ToString());}Debug.Log("SAINT_UNLOCKED "+string.Join(",",opened));}
            BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);
            OpenFor(SaintTune.HeadUp);
            for(float t=0;t<SaintTune.HeadUp;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;
                Pose(0,-.25f*(1-Mathf.Clamp01(t/SaintTune.HeadUp)));yield return null;
            }
            openingUntil=0;Pose(0);Finish(SaintRest);
        }
        IEnumerator SaintSweep()
        {
            var roads=SaintRoads;var standing=SaintStanding;
            if(roads.Count==0||standing.Count==0){Debug.Log("SAINT_SWEEP none");Finish(.6f);yield break;}
            var lead=PartyController.Current.Active.transform.position;SaintArm theirs=null;float nearest=float.PositiveInfinity;
            foreach(var road in roads){if(road.OnRoad(lead)){theirs=road;break;}float d=Flat(road.RoadPoint(.5f)-lead).sqrMagnitude;if(d<nearest){nearest=d;theirs=road;}}
            // The raised arm nearest that road swings across it, and across every road between, to its arc.
            SaintArm sweeper=null;float closest=float.PositiveInfinity;
            foreach(var arm in standing){float gap=Mathf.Abs(Mathf.DeltaAngle(arm.Bearing,theirs.Bearing));if(gap<closest){closest=gap;sweeper=arm;}}
            float side=Mathf.Sign(Mathf.DeltaAngle(sweeper.Bearing,theirs.Bearing));if(side==0)side=1;float reach=Mathf.Max(SaintTune.SweepArc,closest+1);
            var crossed=new List<SaintArm>();
            foreach(var road in roads){float d=Mathf.DeltaAngle(sweeper.Bearing,road.Bearing);if(Mathf.Sign(d)==side&&Mathf.Abs(d)<=reach||road==theirs)crossed.Add(road);}
            // Each road it will cross is marked along its top half, on the road itself: down the road, or off its side, is safe.
            float tell=SaintTune.SweepTell;var marks=new List<Telegraph>();
            foreach(var road in crossed)
            {
                var from=road.RoadPoint(SaintTune.SweepFrom);var top=road.RoadPoint(1);var up=Flat(top-from);float length=up.magnitude+.5f;
                marks.Add(Mark(Telegraph.Line(from,up.normalized,length,SaintTune.SweepWidth,tell,Telegraph.Threat)));
            }
            Debug.Log($"SAINT_SWEEP {sweeper.index} roads={string.Join(",",crossed.ConvertAll(r=>r.index.ToString()))}");
            sweeper.SwingTo(-6,tell*.8f);sweeper.TurnTo(-side*18,tell);ThreatFrom(marks);
            AudioManager.PlayAt("spaceEngineLow_000",sweeper.transform,sweeper.transform.position+Vector3.up*2,.26f,AudioBus.SFX,50,.05f,.7f);
            for(float t=0;t<tell;){if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;ThreatFrom(marks);yield return null;}
            sweeper.TurnTo(side*reach,.45f);
            // Whoever is up on a marked road is struck and thrown the way the arm swings, off the roads and down to the floor. Thrown a set distance off his side he landed on the next
            // road: at their tops the fallen arms lie almost edge to edge.
            var once=new HashSet<Health>();
            foreach(var mark in marks)
            {
                mark.Release();
                foreach(var member in PartyController.Current.members)
                {
                    if(!member.Health.Alive||once.Contains(member.Health)||!mark.Contains(member.transform.position))continue;
                    once.Add(member.Health);float dealt=member.Health.Receive(Packet(SaintTune.SweepDamage,DamageType.Kinetic));if(dealt<=0)continue;
                    member.Stagger(.3f);var road=roads.Find(r=>r.OnRoad(member.transform.position))??theirs;
                    var way=Vector3.Cross(Vector3.up,road.Outward)*side;float throw_=SaintTune.SweepThrow;
                    while(throw_<SaintTune.SweepThrowMost&&roads.Exists(r=>r.OnRoad(member.transform.position+way*throw_)))throw_+=.4f;
                    // And a body's width past the last road's edge, so he comes down beside it and not on its rim.
                    throw_+=.5f;
                    StartCoroutine(Shove(member,way,throw_));Debug.Log($"SAINT_THROWN {member.character} {sweeper.index} {throw_:0.0}");
                }
            }
            Tremor(.35f,.08f);AudioManager.PlayAt("impactMetal_000",sweeper.transform,lead,.3f,AudioBus.SFX,50,.05f,.65f);
            strikeAt=0;
            for(float t=0;t<.5f;){if(!GameTime.Paused)t+=Time.deltaTime;yield return null;}
            sweeper.TurnTo(0,.7f);sweeper.SwingTo(SaintArm.Tune.RaisedPitch,.7f);
            Finish(SaintRest);
        }
    }
}
