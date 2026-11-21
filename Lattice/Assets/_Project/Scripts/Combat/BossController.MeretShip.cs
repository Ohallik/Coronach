using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Meret Venn, phase one: ship to ship (chapter 10, the last boss; brief "The fights" 1; wiring 6.5; the room is docs/maps/Continuance.md, "Boss arena contract"). Her craft (<c>MeretShip</c>,
    /// 9 m) holds station at the mouth of its shelter on the Continuance's back, nose on the lead, and the great ship fights with her: its anchor clamps come up through the plating to reshape
    /// the lanes, its batteries lay broadsides of marked lanes across the berth, its tractor lines run from her craft. <b>The craft that out-flies you.</b>
    /// <list type="bullet">
    /// <item><b>Her shield gives it back.</b> While it is up (a dome round her craft, and the bar says so) a blow on her is turned, a shot burns out on the dome, and one the player fired comes
    /// straight back down a marked lane at him (a Flash Guard's answer, marked: a roll clears it); the first time it happens the screen says why. She stays locked and framed through it, and the
    /// partner's shots only burn out: nobody is punished for what the companion does. It drops when she fires through it: after her lance, and while
    /// her tractor pulls. Then her three <b>shield projectors</b> (the knots at her wingtips and her stern) open and glow amber, and they and her hull take blows: the read and the lunge are the fight.</item>
    /// <item><b>The bar is paid in blows; a projector decides which, never how much</b> (D281, the Debris Cutter's pods): a blow on an open projector comes off her bar as a blow on the hull does,
    /// each third the bar passes puts out the projector struck most, and a projector carries a third, so one struck through by itself goes at its line. The first two out are her shifts (she
    /// changes the lanes: two clamps at once, then a double broadside); the third ends it.</item>
    /// </list>
    /// Moves: <b>Broadside</b> (marked parallel lanes laid through the lead from her line, middle first, gaps wider than two hulls; a risen clamp stops a lane), <b>Lance</b> (her craft's beam down
    /// a marked lane, then her shield is down: the opening), <b>Tractor</b> (a marked line; the first ship along it is struck and reeled toward her until it rolls, cover breaks the line or it
    /// comes close: her shield is down while it pulls, and a ship pulled in is a lunge from a projector), <b>Clamp</b> and <b>Clamps</b> (the seat nearest the lead marked, then its clamp rises solid
    /// through the plane: whoever stood there is struck and set beside it first, D289), <b>Volley</b> (from her last projector: a broadside, and a second laid in its gaps as the first lands).
    /// Her blows never grow with her phase (she gets quicker, not angrier). She leaves by her <see cref="BossExit"/> at the gantry's dock (<see cref="Ending.Leaves"/>); the after-talk
    /// <c>MeretGantry</c> is the zone's. Logs <c>MERET_SHIP_WAKES</c>, <c>MERET_SHIELD up|down</c>, <c>MERET_RETURN</c>, <c>MERET_PROJECTOR_DOWN</c>, <c>MERET_BROADSIDE</c>, <c>MERET_LANCE</c>,
    /// <c>MERET_TRACTOR</c>, <c>MERET_CLAMPS</c>, and <see cref="MeretClamp"/>'s.
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit MeretShipKit=Register(new Kit
        {
            id="MeretShip",title="DIRECTOR MERET VENN",epithet="Aboard the Continuance, with the last anchor",thresholds=new[]{2/3f,1/3f},
            choose=(boss,hero)=>boss.ChooseShip(hero),
            moves={["Broadside"]=boss=>boss.ShipBroadside(false),["Lance"]=boss=>boss.ShipLance(),["Tractor"]=boss=>boss.ShipTractor(),
                ["Clamp"]=boss=>boss.ShipClamps(1),["Clamps"]=boss=>boss.ShipClamps(2),["Volley"]=boss=>boss.ShipBroadside(true)},
            signature=phase=>phase==2?"Clamps":phase>=3?"Volley":null,
            shift=boss=>boss.ShipChangesLanes(),shiftText="SHE CHANGES THE LANES  ·  BRACE",
            status=boss=>boss.ShipStatus(),guarded=boss=>boss.ShipStatus(),
            fightGoal=(boss,label)=>boss.ShipGoal(label),
            // She leaves for the gantry, and MeretGantry follows by itself: nothing is said over it.
            capsuleRadius=ShipTune.Radius,occlusion=false,ending=Ending.Leaves,downBark="",
            start=boss=>boss.ShipStart(),update=boss=>boss.ShipUpdate(),down=boss=>boss.ShipDown(),
        });
        /// <summary>Phase one's numbers, all of them (her integrity is her EnemyDef's: LastMooringBuilder.Meret). Every blow is authored at 192 to 198 and her phases add nothing to it: 12 to 16%
        /// of any hero's bar at Sync 29 and 30 in the Choirwrought parts chapter 10 is fought in (LastMooringBossTests measures all three heroes).</summary>
        public static class ShipTune
        {
            /// <summary>Her capsule (6 m tall: her definition's), her shield's reach round her, and how far she is fought from (the shared controller stops at 30; her lanes run past it).</summary>
            public const float Radius=3,Shield=6.5f,Reach=46;
            /// <summary>Where she holds station: out of her shelter's mouth along her facing as she woke, swaying across it toward the lead, at her pace, turning her nose at this rate.</summary>
            public const float StationOut=6,StationSway=5,Pace=7,Turn=120;
            /// <summary>Broadside: lanes by phase, their width, the spacing (a lane and a gap wider than two of Idra's hulls, 8.7 m), where they start out from her, their length, the tell by phase,
            /// the ripple from the middle lane out, the second pass's tell (Volley), the blow.</summary>
            public static readonly int[] Lanes={3,5,5};
            public static readonly float[] BroadsideTell={1.3f,1.2f,1.1f};
            public const float LaneWidth=3.6f,LaneSpacing=12.8f,LaneFrom=4,LaneLength=40,Ripple=.15f,VolleyTell=.9f,BroadsideDamage=194;
            /// <summary>Lance: its lane's width and length, the tell by phase, the second shot's tell (from her last projector), the blow, how long the cut shows, and how long her shield is down after.</summary>
            // Down a little longer than it was (3.4, 3.1, 2.8), and her lance a little likelier (1.5 of the pick): played through the pad her dome stood three quarters of the fight, and a
            // window came once in about twelve seconds of holding fire.
            public static readonly float[] LanceTell={1.1f,1f,.9f},ShieldDown={4.2f,3.8f,3.4f};
            public const float LanceWidth=2.8f,LanceLength=40,LanceNext=.7f,LanceDamage=196,LanceShows=.3f;
            /// <summary>Tractor: its line's longest and width, the tell, the blow, the reel (pace, speed, longest, where it stops, past what it breaks), and how long her shield stays down after.</summary>
            public const float TractorLength=36,TractorWidth=2.4f,TractorTell=.9f,TractorDamage=192,ReelPace=.45f,ReelSpeed=7,ReelSeconds=2.5f,ReelStops=9,ReelBreaks=40,TractorOpen=2.4f;
            /// <summary>Clamps: the marking round a seat (the clamp's solid width and a hull's grace), the tell by phase, how long a clamp stands by phase, the blow.</summary>
            public static readonly float[] ClampTell={1.6f,1.5f,1.4f},ClampStands={9,11,12};
            public const float ClampMark=4.4f,ClampDamage=192;
            /// <summary>Where the room's three seats stand from her as she woke, in her own frame (docs/maps/Continuance.md: (-30, 16, 228), (-4, 16, 222), (22, 16, 228) from her (-4, 19, 252)
            /// facing south), when the room stands none of its own.</summary>
            public static readonly Vector3[] Seats={new(26,-3,24),new(0,-3,30),new(-26,-3,24)};
            /// <summary>Her shield's answer: how soon after a shot reaches it, its lane's width and the grace past the shooter, its tell, its blow; at most one lane a shooter in this long.</summary>
            public const float ReturnGather=.2f,ReturnWidth=2.4f,ReturnPast=6,ReturnTell=.6f,ReturnDamage=192,ReturnEvery=1;
            /// <summary>A shot fired while her shield was down, or this soon after it rose again (no thumb comes off a trigger quicker, and nothing on screen says the moment is coming), was
            /// fired through the gap: it only burns out on the dome. Played through the pad, every shot she gave back was one fired in the open window and still in the air as it closed.</summary>
            public const float ReturnGrace=.3f;
            /// <summary>The longest her shield stands before she fires through it: past this her next move is her lance (her tractor beyond its reach). On the picks alone a dome stood for
            /// half a minute at a time, and the same bar lasted 83 s in one fight through the pad and 181 s in the next.</summary>
            public const float MostUp=8;
            /// <summary>Her shield's two relays (<see cref="MeretRelay"/>): where each stands from her as she woke (her own frame, as the seats: to her side, out along her facing), in front of
            /// each shelter wing and well outside her dome; its target's size; its own bar as a share of hers (about three seconds of one ship's fire); how long it is dark once it is shot
            /// through. Her dome is down for her lance's time (<see cref="ShieldDown"/>) when one is. Played through the pad without them a player held fire about seven tenths of the fight,
            /// and the same bar lasted 80 to 170 s as her lances fell.</summary>
            public static readonly Vector3[] Relays={new(14,0,10),new(-14,0,10)};
            public const float RelayTarget=1.4f,RelayShare=.06f,RelayRecharge=12;
            /// <summary>The most of her bar one window of her dome down gives up (about four windows a projector): a window worth more ends there, a relay's at once (her dome is back up) and
            /// her lance's or her tractor's with her bar held. So her fight is as many windows as the player makes, at the rhythm he makes them: played through the pad with no such limit
            /// the windows were worth what the ships' places made them, and three fights with one bar lasted 69 to 108 s.</summary>
            public const float WindowShare=1/12f;
            /// <summary>Her projectors: where each stands on her craft (port wingtip, starboard wingtip, stern), its target's size, its share of her bar.</summary>
            // Over the wingtip nacelles and the stern, clear of the hull: inside the nacelles' rings an open (amber) projector didn't read.
            public static readonly Vector3[] Projectors={new(-3.4f,1.5f,-.8f),new(3.4f,1.5f,-.8f),new(0,1.9f,-3.4f)};
            public static readonly string[] ProjectorNames={"Port projector","Starboard projector","Stern projector"};
            public const float ProjectorTarget=1.3f,ProjectorShare=1/3f;
            /// <summary>Her rest between moves by phase, and how long her line waits into the fight.</summary>
            public static readonly float[] Rest={2.4f,2.1f,1.9f};
            public const float FirstWord=1.6f,RadioAfter=2.2f;
        }
        static readonly Color ShieldTone=new(.45f,1.3f,1.6f,.35f),ProjectorShut=new(.45f,1.45f,1.6f,.95f),ProjectorOpen=new(1.7f,.95f,.35f,.95f),ShipBeamTone=new(1.6f,.75f,.35f,.95f),
            ShipTractorTone=new(.6f,1.4f,1.6f,.9f);
        sealed class ShipProjector{public Health health;public Transform node;public Renderer knot;public SphereCollider shape;public float left;public bool down,open;public int index;}
        readonly List<ShipProjector> shipProjectors=new();
        readonly List<MeretClamp> shipClamps=new();
        readonly Dictionary<Health,float> shipReturnAt=new();
        Transform shipShield;Vector3 shipHome,shipFacing;float shipStruckAt=-1,shipWordAt=-1,shipReraisedAt=-1,shipUpSince=-1;bool shipDropped;int shipDown;
        /// <summary>A test's switch for the old fault (her shield gave back a shot fired while it was down, still in the air as it rose): never set in a scene.</summary>
        [System.NonSerialized] public bool shipReturnsTheGap;bool shipPulling,shipHears,shipShieldShown,shipFirstReturn,shipClampSaid,shipWordSaid,shipReturnSaid;
        /// <summary>Her shield is up: a blow on her is turned, a shot burns out on it and the player's comes back. Down only while she fights through it (an opening, her tractor pulling).</summary>
        public bool ShipShieldUp=>health!=null&&health.Alive&&brain!=null&&!brain.Passive&&!health.Broken&&!shipPulling&&shipProjectors.Count>0&&Time.time>=shipRelayOpenUntil;
        /// <summary>A test's switch for the fight as it was (no relays: her dome dropped only for her lance and her tractor): never set in a scene.</summary>
        public static bool ShipRelaysOff;
        readonly List<MeretRelay> shipRelays=new();float shipRelayOpenUntil=-1,shipWindowFloor=-1;bool shipWindowSpent;
        public IReadOnlyList<MeretRelay> ShipRelays=>shipRelays;
        bool AnyRelayLit{get{foreach(var r in shipRelays)if(r!=null&&r.Lit)return true;return false;}}
        public int ShipProjectorsLeft=>ShipTune.Projectors.Length-shipDown;
        public Health ShipProjectorHealth(int i)=>i>=0&&i<shipProjectors.Count?shipProjectors[i].health:null;
        public bool ShipProjectorOpen(int i)=>i>=0&&i<shipProjectors.Count&&shipProjectors[i].open;
        public IReadOnlyList<MeretClamp> ShipClampsHeld=>shipClamps;
        /// <summary>The way her shelter's mouth faces (as she woke): her lanes run out along it, and the open berth lies that way.</summary>
        public Vector3 ShipFacing=>shipFacing;
        int ShipStage=>Mathf.Clamp(Phase,1,3)-1;
        static float ShipDt=>GameTime.CombatHeld?0:Time.deltaTime;
        readonly List<Telegraph> shipOne=new(1);
        List<Telegraph> ShipOnly(Telegraph marking){shipOne.Clear();shipOne.Add(marking);return shipOne;}

        void ShipStart()
        {
            brain.Rooted=true;shipHome=transform.position;shipFacing=Flat(transform.forward).sqrMagnitude>.01f?Flat(transform.forward).normalized:Vector3.forward;
            // The hull's clamps: the room's, or three of her own where the room's contract stands them.
            shipClamps.Clear();
            foreach(var clamp in MeretClamp.For(health.id))if(Flat(clamp.transform.position-shipHome).magnitude<60)shipClamps.Add(clamp);
            if(shipClamps.Count==0)
            {
                var facing=Quaternion.LookRotation(shipFacing);var made=new List<(float x,Vector3 at)>();
                foreach(var seat in ShipTune.Seats){var at=shipHome+facing*seat;made.Add((at.x,at));}
                made.Sort((a,b)=>a.x.CompareTo(b.x));
                for(int i=0;i<made.Count;i++)shipClamps.Add(MeretClamp.Make(health.id,i+1,made[i].at,this));
            }
            foreach(var clamp in shipClamps)clamp.Claim(brain.definition,transform.position.y);
            // Her three projectors, shut until her shield drops: no blow reaches one and nothing locks on it before.
            shipProjectors.Clear();
            for(int i=0;i<ShipTune.Projectors.Length;i++)
            {
                var node=new GameObject(ShipTune.ProjectorNames[i]);node.transform.SetParent(transform,false);node.transform.localPosition=ShipTune.Projectors[i];
                var part=node.AddComponent<Health>();part.id=ShipTune.ProjectorNames[i];part.maximum=part.integrity=health.maximum*ShipTune.ProjectorShare;
                part.weakness=health.weakness;part.resistance=health.resistance;part.breakThreshold=1e9f;part.Damageable=false;
                var shape=node.AddComponent<SphereCollider>();shape.isTrigger=true;shape.radius=ShipTune.ProjectorTarget;shape.enabled=false;
                node.AddComponent<Hurtbox>().owner=part;
                var drawn=GameObject.CreatePrimitive(PrimitiveType.Sphere);drawn.name="Knot";Destroy(drawn.GetComponent<Collider>());drawn.transform.SetParent(node.transform,false);drawn.transform.localScale=Vector3.one*ShipTune.ProjectorTarget*.95f;
                var knot=drawn.GetComponent<Renderer>();knot.sharedMaterial=ActorFactory.Energy(ProjectorShut);knot.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;knot.receiveShadows=false;
                var projector=new ShipProjector{health=part,node=node.transform,knot=knot,shape=shape,index=i,left=part.integrity};shipProjectors.Add(projector);
                part.Damaged+=(_,packet,amount)=>ShipProjectorStruck(projector,packet,amount);
                part.Died+=(_,packet)=>ShipProjectorDown(projector,packet,false);
            }
            // Her shield's two relays, masts on the hull's back in front of each wing, lit while the dome stands.
            shipRelays.Clear();
            if(!ShipRelaysOff)
            {
                var frame=Quaternion.LookRotation(shipFacing);
                for(int i=0;i<ShipTune.Relays.Length;i++)
                {
                    var at=shipHome+frame*ShipTune.Relays[i];at.y=transform.position.y;bool west=at.x<shipHome.x;
                    var go=new GameObject(west?"West shield relay":"East shield relay");go.transform.position=at;
                    var part=go.AddComponent<Health>();part.id=go.name;part.maximum=part.integrity=health.maximum*ShipTune.RelayShare;
                    part.weakness=health.weakness;part.resistance=health.resistance;part.breakThreshold=1e9f;part.Damageable=false;
                    var shape=go.AddComponent<SphereCollider>();shape.isTrigger=true;shape.radius=ShipTune.RelayTarget;shape.enabled=false;
                    go.AddComponent<Hurtbox>().owner=part;
                    var relay=go.AddComponent<MeretRelay>();relay.boss=this;relay.index=west?0:1;relay.health=part;relay.shape=shape;
                    var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name="Relay head";Destroy(head.GetComponent<Collider>());head.transform.SetParent(go.transform,false);
                    var headR=head.GetComponent<Renderer>();headR.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;headR.receiveShadows=false;relay.head=headR;
                    var mast=GameObject.CreatePrimitive(PrimitiveType.Cylinder);mast.name="Relay mast";Destroy(mast.GetComponent<Collider>());mast.transform.SetParent(go.transform,false);
                    mast.transform.localPosition=Vector3.down*1.6f;mast.transform.localScale=new Vector3(.45f,1.6f,.45f);mast.GetComponent<Renderer>().sharedMaterial=ActorFactory.Energy(new Color(.5f,.55f,.6f,1));
                    var beam=go.AddComponent<LineRenderer>();beam.positionCount=2;beam.useWorldSpace=true;beam.startWidth=.3f;beam.endWidth=.6f;
                    beam.sharedMaterial=ActorFactory.Energy(ShieldTone);beam.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;relay.beam=beam;
                    part.Died+=(_,packet)=>ShipRelayFalls(relay);
                    relay.Light(false,transform.position);shipRelays.Add(relay);
                }
            }
            // Her shield: a dome round her craft at the plane, drawn while it is up.
            var dome=GameObject.CreatePrimitive(PrimitiveType.Sphere);dome.name="Her shield";Destroy(dome.GetComponent<Collider>());dome.transform.SetParent(transform,false);
            dome.transform.localPosition=Vector3.up*.6f;dome.transform.localScale=new Vector3(ShipTune.Shield*2,ShipTune.Shield*.9f,ShipTune.Shield*2);
            var skin=dome.GetComponent<Renderer>();skin.sharedMaterial=ActorFactory.Energy(ShieldTone);skin.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;skin.receiveShadows=false;
            shipShield=dome.transform;dome.SetActive(false);
            // Her shield turns a blow while it is up (only while she fights: held passive she is measured open). Lockable throughout: the flight camera frames what the lead has locked.
            // A window of her dome down gives up a window's worth of her bar and no more (only while she fights: held passive she is measured open).
            health.Holds=()=>!brain.Passive&&shipWindowFloor>=0?shipWindowFloor:0;health.Plate=_=>ShipShieldUp?0:1;health.Damaged+=ShipStruck;shipHears=true;
            var leaving=gameObject.AddComponent<BossDeparture>();leaving.sound="thrusterFire_000";leaving.speed=16;leaving.Duration=3.2f;
            // Her fight is under way from her door: travel waits on it, and her length is timed from it.
            engaged=Time.realtimeSinceStartup;shipWordAt=Time.time+ShipTune.FirstWord;
            Debug.Log($"MERET_SHIP_WAKES clamps={shipClamps.Count} projectors={shipProjectors.Count}");
        }
        void ShipStruck(Health _,DamagePacket packet,float amount)
        {
            if(packet.source==null||!packet.source.friendly)return;
            if(engaged<0)engaged=Time.realtimeSinceStartup;
            if(amount>0)shipStruckAt=Time.time;
        }
        /// <summary>A blow on an open projector comes off her bar too, as one on her hull would (it is part of her). The blow that would empty the bar is the one that beats her.</summary>
        void ShipProjectorStruck(ShipProjector projector,DamagePacket packet,float amount)
        {
            if(packet.source==null||!packet.source.friendly||health==null||!health.Alive)return;
            float lost=Mathf.Max(0,projector.left-projector.health.integrity);projector.left=projector.health.integrity;
            // A window's worth and no more: what a projector takes past it comes off nobody's bar.
            if(shipWindowFloor>=0)lost=Mathf.Min(lost,Mathf.Max(0,health.integrity-shipWindowFloor));
            if(amount<=0||lost<=0)return;
            CombatActor.PayHit(packet,amount);shipStruckAt=Time.time;
            if(health.integrity-lost>1){health.integrity-=lost;return;}
            if(!projector.health.Alive&&shipDown+1>=ShipTune.Projectors.Length){health.integrity=1;return;}
            health.InvulnerableUntil=0;health.Receive(new DamagePacket{source=packet.source,amount=health.maximum*100,type=health.weakness,tag="projector",weight=HitWeight.Heavy});
        }
        /// <summary>A projector out: the one struck most as the bar passes a third, or one struck through by itself. Nothing more comes off the bar than the blows it took; the third out ends
        /// the fight. <paramref name="settling"/>: its bar set past the line with no blow landing (a test), which plays nothing.</summary>
        void ShipProjectorDown(ShipProjector projector,DamagePacket packet,bool settling)
        {
            if(this==null||health==null||!health.Alive||projector.down)return;
            ShipProjectorOpen(projector,false);projector.down=true;if(projector.knot!=null)projector.knot.enabled=false;
            shipDown++;if(engaged<0)engaged=Time.realtimeSinceStartup;
            CombatVfx.Burst(projector.node.position,new Color(.55f,.55f,.6f),"kill");AudioManager.PlayAt("explosionCrunch_000",transform,projector.node.position,.3f,AudioBus.SFX,60,.05f,1.1f);
            Debug.Log($"MERET_PROJECTOR_DOWN {projector.index} left={ShipProjectorsLeft}");
            if(shipDown>=ShipTune.Projectors.Length)
            {
                health.Holds=null;health.Plate=null;health.InvulnerableUntil=0;health.Damageable=true;
                health.Receive(new DamagePacket{source=packet.source,amount=health.maximum*100,type=health.weakness,tag="projector",weight=HitWeight.Heavy});
                return;
            }
            if(!settling)health.integrity=Mathf.Max(1,Mathf.Min(health.integrity,(1-shipDown*ShipTune.ProjectorShare)*health.maximum-1));
        }
        void ShipProjectorOpen(ShipProjector projector,bool open)
        {
            if(projector.down)open=false;if(projector.open==open)return;projector.open=open;
            projector.health.Damageable=open;if(projector.shape!=null)projector.shape.enabled=open;
        }
        void ShipChangesLanes()
        {
            // The lanes change: whatever stood up sinks, and the next is the phase's own (two clamps, then the double broadside).
            foreach(var clamp in shipClamps)if(clamp!=null)clamp.Sink();
            Debug.Log("MERET_LANES_CHANGE "+HealthPhase);
        }
        void ShipUpdate()
        {
            brain.Rooted=true;brain.DamageScale=1;
            // Each third the bar passes puts out the projector struck most (the player's choice; one nobody struck goes in order). Set past a line with no blow landing (a test): quietly.
            if(shipDown<ShipTune.Projectors.Length-1)
            {
                int crossed=0;for(int k=1;k<ShipTune.Projectors.Length;k++)if(health.integrity<health.maximum*(1-k*ShipTune.ProjectorShare)-.5f)crossed++;
                bool struck=shipStruckAt>=0&&Time.time-shipStruckAt<.5f;
                for(int guard=0;shipDown<crossed&&guard<ShipTune.Projectors.Length;guard++)
                {
                    ShipProjector most=null;float least=float.PositiveInfinity;
                    foreach(var p in shipProjectors){if(p.down)continue;float left=p.health.integrity/Mathf.Max(1,p.health.maximum);if(left<least-.001f){least=left;most=p;}}
                    if(most==null)break;ShipProjectorDown(most,default,!struck);
                }
            }
            bool up=ShipShieldUp;
            // Behind it a blow is turned and fills nothing: a shield beaten on is no break, and no way round waiting for her to drop it. A body held passive (on show at its door, or measured
            // flat out) is open: the shield is the fight's.
            if(up)health.ClearBreak();
            bool open=!up&&!brain.Passive;
            foreach(var p in shipProjectors)
            {
                if(p.down||p.knot==null)continue;ShipProjectorOpen(p,open);p.knot.sharedMaterial=ActorFactory.Energy(open?ProjectorOpen:ProjectorShut);
                p.knot.transform.localScale=Vector3.one*ShipTune.ProjectorTarget*.95f*(open?1+.25f*Mathf.Sin(Time.time*10):1);
            }
            if(shipShield!=null&&shipShield.gameObject.activeSelf!=up)shipShield.gameObject.SetActive(up);
            if(up!=shipShieldShown)
            {
                shipShieldShown=up;if(up)shipUpSince=Time.time;Debug.Log("MERET_SHIELD "+(up?"up":"down"));
                // A window opens: what it may give up is a window's worth from here (all of what is left, near the end).
                shipWindowSpent=false;shipWindowFloor=up?-1:health.integrity-health.maximum*ShipTune.WindowShare<health.maximum*.02f?0:health.integrity-health.maximum*ShipTune.WindowShare;
            }
            // A window's worth given up: a relay's window closes (her dome comes back), and any other's holds her bar till it does.
            if(!up&&!brain.Passive&&!shipWindowSpent&&shipWindowFloor>0&&health.integrity<=shipWindowFloor+.5f)
            {
                shipWindowSpent=true;Debug.Log("MERET_WINDOW_SPENT");
                if(Time.time<shipRelayOpenUntil)shipRelayOpenUntil=Time.time;
            }
            // Up again after she fired through it: what was fired in the gap is in the air still.
            if(up&&shipDropped){shipDropped=false;shipReraisedAt=Time.time;}
            if(up)ShipShieldTakes();
            foreach(var relay in shipRelays)
            {
                if(relay==null||relay.health==null)continue;
                if(!relay.health.Alive&&relay.RechargedAt>=0&&Time.time>=relay.RechargedAt){relay.RechargedAt=-1;relay.health.Heal(relay.health.maximum);}
                relay.Light(up&&relay.health.Alive,transform.position+Vector3.up*.6f);
            }
            if(brain.Passive||GameServices.Current.Input.Blocked)return;
            // Her first word, once her card has been read: what not to shoot.
            if(!shipWordSaid&&shipWordAt>0&&Time.time>=shipWordAt){shipWordSaid=true;BarkService.PlayAt("Meret","ship",0,true);}
            ShipBeyond();
            if(!busy&&!health.Broken)ShipStation(Time.deltaTime);
        }
        /// <summary>Her lanes run past the 30 m the shared controller chooses within: out to her reach she chooses for herself the same way.</summary>
        void ShipBeyond()
        {
            if(busy||health.Broken||PartyController.Current==null)return;
            var hero=PartyController.Current.Active;float d=Flat(hero.transform.position-transform.position).magnitude;
            if(d<=30||d>ShipTune.Reach)return;
            if(shiftPending){shiftPending=false;Begin("PhaseShift",PhaseShift());return;}
            if(Time.time<nextSpecial)return;
            if(queuedMove!=null){var promised=queuedMove;queuedMove=null;if(Perform(promised))return;}
            var (move,routine)=ChooseShip(hero);if(routine!=null)Begin(move,routine);
        }
        /// <summary>Between moves: at her station out of her shelter's mouth, swaying across it toward the lead, her nose on him.</summary>
        void ShipStation(float dt)
        {
            var party=PartyController.Current;if(party==null||party.Active==null||dt<=0)return;
            var lead=party.Active.transform.position;var across=Vector3.Cross(Vector3.up,shipFacing);
            float sway=Mathf.Clamp(Vector3.Dot(Flat(lead-shipHome),across),-ShipTune.StationSway,ShipTune.StationSway);
            var station=shipHome+shipFacing*ShipTune.StationOut+across*sway;station.y=transform.position.y;
            var controller=GetComponent<CharacterController>();var off=Flat(station-transform.position);
            if(off.magnitude>.3f&&controller.enabled)controller.Move(off.normalized*Mathf.Min(off.magnitude,ShipTune.Pace*dt));
            var toLead=Flat(lead-transform.position);
            if(toLead.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(toLead),ShipTune.Turn*dt);
            Pose(1.5f*Mathf.Sin(Time.time*1.2f),.12f*Mathf.Sin(Time.time*1.9f));
        }
        /// <summary>A party shot that reaches her raised shield burns out on it; the player's own comes back, one marked lane at him a beat later (the first time, the screen says why). The
        /// partner's only burn out: the companion fires as she always does, and nobody pays for it.</summary>
        void ShipShieldTakes()
        {
            var party=PartyController.Current;if(party==null)return;
            List<Projectile> taken=null;
            foreach(var shot in Projectile.Live)
            {
                if(shot==null||!shot.isActiveAndEnabled||!shot.Threatens(false))continue;
                var off=shot.transform.position-transform.position;if(Mathf.Abs(off.y)>3||Flat(off).magnitude>ShipTune.Shield)continue;
                (taken??=new List<Projectile>()).Add(shot);
            }
            if(taken==null)return;
            foreach(var shot in taken)
            {
                // Whose it was: the hero behind it along its flight.
                CombatActor shooter=null;float best=float.PositiveInfinity;var way=shot.Velocity.sqrMagnitude>.01f?shot.Velocity.normalized:Vector3.zero;
                foreach(var member in party.members)
                {
                    if(member==null||!member.Health.Alive)continue;var rel=member.transform.position-shot.transform.position;float along=Vector3.Dot(rel,way);
                    float off=(rel-way*along).magnitude+(along>0?50:0);if(off<best){best=off;shooter=member;}
                }
                bool throughTheGap=!shipReturnsTheGap&&shipReraisedAt>=0&&shot.Fired<shipReraisedAt+ShipTune.ReturnGrace;
                shot.Spend();
                if(shooter==null||shooter!=party.Active||throughTheGap)continue;
                if(shipReturnAt.TryGetValue(shooter.Health,out float at)&&Time.time<at)continue;
                shipReturnAt[shooter.Health]=Time.time+ShipTune.ReturnEvery;
                StartCoroutine(ShipGivesItBack(shooter));
            }
        }
        IEnumerator ShipGivesItBack(CombatActor shooter)
        {
            for(float t=0;t<ShipTune.ReturnGather;){t+=ShipDt;yield return null;}
            if(shooter==null||!shooter.Health.Alive||!health.Alive)yield break;
            var way=Toward(shooter.transform.position);var origin=transform.position+way*ShipTune.Radius;origin.y=transform.position.y;
            float length=Flat(shooter.transform.position-origin).magnitude+ShipTune.ReturnPast;
            if(CombatCover.Sweep(origin,origin+way*length,.4f,out var wall))length=Mathf.Max(2,wall.distance);
            var lane=Mark(Telegraph.Line(origin,way,length,ShipTune.ReturnWidth,ShipTune.ReturnTell,Telegraph.Heavy));
            Debug.Log("MERET_RETURN "+shooter.character);
            AudioManager.PlayAt("forceField_000",transform,origin,.26f,AudioBus.SFX,50,.05f,1.3f);
            if(!shipFirstReturn)
            {
                shipFirstReturn=true;
                Notices.Post("HER SHIELD GIVES IT BACK","While the dome round her craft is up, a shot that reaches it comes straight back down a marked lane, and a blow on her is turned. Shoot a shield relay instead: the two masts beaming cyan into her dome (lock one with the d-pad). Shot through, a relay drops her dome for a few seconds; so do her lance and her tractor. Then strike the projector of your choice while it glows amber.");
            }
            if(!shipReturnSaid&&PartyController.Current!=null){shipReturnSaid=true;BarkService.Play(PartyController.Current.Active.character,"returnfire",true);}
            yield return null;// from the marking's own first frame
            for(float t=0;t<ShipTune.ReturnTell;){t+=ShipDt;if(!health.Alive){lane.Cancel();yield break;}yield return null;}
            Strike(lane,ShipTune.ReturnDamage,DamageType.Beam,.25f);
            CombatVfx.Burst(origin,ShipBeamTone,"kill");
        }
        void ShipDown()
        {
            if(shipHears){health.Damaged-=ShipStruck;shipHears=false;}
            health.Holds=null;health.Plate=null;shipPulling=false;health.Damageable=true;
            foreach(var p in shipProjectors){ShipProjectorOpen(p,false);if(p.knot!=null)p.knot.enabled=false;}
            if(shipShield!=null)shipShield.gameObject.SetActive(false);
            foreach(var clamp in shipClamps)if(clamp!=null)clamp.Sink();
            foreach(var relay in shipRelays)if(relay!=null){relay.Light(false,transform.position);Destroy(relay.gameObject);}shipRelays.Clear();
            Debug.Log("MERET_SHIP_LEAVES");
        }
        string ShipStatus()
        {
            if(health==null||!health.Alive||shipProjectors.Count==0)return "";
            if(ShipShieldUp)return AnyRelayLit?"HER SHIELD IS UP  ·  SHOOT A RELAY":"HER SHIELD IS UP  ·  HOLD YOUR FIRE";
            if(brain!=null&&brain.Passive)return "";
            if(shipWindowSpent)return "HER SHIELD IS COMING BACK";
            return "HER SHIELD IS DOWN  ·  STRIKE A PROJECTOR";
        }
        string ShipGoal(string label)
        {
            if(health==null||!health.Alive||shipProjectors.Count==0)return label;
            if(ShipShieldUp)return AnyRelayLit?"Shoot a shield relay: her shield gives back what reaches it":"Hold your fire: her shield gives it back";
            return $"Put out a shield projector ({Mathf.Min(shipDown+1,ShipTune.Projectors.Length)}/{ShipTune.Projectors.Length})";
        }
        /// <summary>A relay shot through: it goes dark and recharges, and her dome is down for her lance's time (the projectors open with it). Not an opening of her body: she fights on.</summary>
        void ShipRelayFalls(MeretRelay relay)
        {
            if(this==null||health==null||!health.Alive||relay==null)return;
            relay.RechargedAt=Time.time+ShipTune.RelayRecharge;shipRelayOpenUntil=Time.time+ShipTune.ShieldDown[ShipStage];shipDropped=true;
            relay.Light(false,transform.position);
            CombatVfx.Burst(relay.transform.position,ShieldTone,"kill");AudioManager.PlayAt("explosionCrunch_000",null,relay.transform.position,.28f,AudioBus.SFX,60,.05f,1.2f);
            Debug.Log("MERET_RELAY_DOWN "+relay.index);
            var party=PartyController.Current;
            if(party!=null&&shipOpenSaid<Phase){shipOpenSaid=Phase;BarkService.Play(party.Active.character,Situation("bossopening"),true);}
        }
        /// <summary>Her shield is down: she fired through it, or her tractor runs through it. The party's line the first time each phase, and the projectors open with it.</summary>
        int shipOpenSaid=-1;
        void ShipShieldDrops(float seconds)
        {
            OpenFor(seconds);shipDropped=true;
            var party=PartyController.Current;
            if(party!=null&&shipOpenSaid<Phase){shipOpenSaid=Phase;BarkService.Play(party.Active.character,Situation("bossopening"),true);}
        }

        // ---------------------------------------------------------------- choosing
        (string,IEnumerator) ChooseShip(CombatActor hero)
        {
            float d=Flat(hero.transform.position-transform.position).magnitude;int phase=Mathf.Clamp(Phase,1,3);
            // Her dome has stood its longest: she fires through it.
            if(ShipShieldUp&&shipUpSince>=0&&Time.time-shipUpSince>ShipTune.MostUp)return d<=ShipTune.LanceLength?("Lance",ShipLance()):("Tractor",ShipTractor());
            int down=0;foreach(var c in shipClamps)if(c!=null&&!c.Up)down++;
            // With her relays the windows are the player's to make, at a rhythm he can count on: her lance is her answer to a dome left standing (MostUp), never a roll. Rolled as well, its
            // windows (each an opening, a blow half again) came as they fell, and three fights through the pad with the same bar lasted 93 to 119 s.
            bool relays=shipRelays.Count>0;
            string move=Pick(("Broadside",phase>=3?1.1f:1.6f),("Lance",relays?0:d<=ShipTune.LanceLength?2f:FullOffer),("Tractor",d>28?2.2f:d>12?1.2f:.4f),
                ("Clamp",phase==1&&down>0?1f:0),("Clamps",phase>=2&&down>1?1.2f:0),("Volley",phase>=3?1.5f:0));
            return move switch{"Lance"=>(move,ShipLance()),"Tractor"=>(move,ShipTractor()),"Clamp"=>(move,ShipClamps(1)),"Clamps"=>(move,ShipClamps(2)),"Volley"=>(move,ShipBroadside(true)),_=>(move,ShipBroadside(false))};
        }

        // ---------------------------------------------------------------- the moves
        /// <summary>The Continuance's batteries lay parallel lanes from her line through the lead, the middle one first and outward, a risen clamp stopping any lane that meets it. A volley lays a
        /// second pass in the first's gaps as the first lands.</summary>
        IEnumerator ShipBroadside(bool volley)
        {
            int k=ShipStage;int n=ShipTune.Lanes[k];float tell=ShipTune.BroadsideTell[k];
            var lead=PartyController.Current.Active.transform.position;var way=Toward(lead);transform.rotation=Quaternion.LookRotation(way);
            var across=Vector3.Cross(Vector3.up,way);var front=transform.position+way*ShipTune.LaneFrom;front.y=transform.position.y;
            var lanes=new List<Telegraph>();var landAt=new List<float>();var landed=new List<bool>();
            void Lay(float offset,float lands,float told)
            {
                var origin=front+across*offset;float length=ShipTune.LaneLength;
                if(CombatCover.Sweep(origin,origin+way*length,.4f,out var wall))length=Mathf.Max(2,wall.distance);
                lanes.Add(Mark(Telegraph.Line(origin,way,length,ShipTune.LaneWidth,told,Telegraph.Heavy)));landAt.Add(lands);landed.Add(false);
            }
            float mid=(n-1)*.5f;
            for(int i=0;i<n;i++){float lands=tell+Mathf.Abs(i-mid)*ShipTune.Ripple;Lay((i-mid)*ShipTune.LaneSpacing,lands,lands);}
            Debug.Log($"MERET_BROADSIDE lanes={n} volley={volley} phase={Phase}");
            AudioManager.PlayAt("spaceEngineLow_000",transform,front,.28f,AudioBus.SFX,60,.05f,.7f);
            // One hit a pass: overlapping lanes of one pass never land twice on a hero, and the volley's second pass is a pass of its own.
            bool second=volley;var first=new HashSet<Health>();var again=new HashSet<Health>();
            // Counted from the marking's own first frame: it was laid this frame, and counting this one put the blow a frame ahead of what the marking (and whoever reads it) says.
            yield return null;
            for(float t=0;;)
            {
                t+=ShipDt;if(Interrupted)yield break;
                // The volley: as the first pass's middle lands, its gaps are marked.
                if(second&&t>=tell)
                {
                    second=false;
                    for(int i=0;i<n-1;i++){float lands=t+ShipTune.VolleyTell+Mathf.Abs(i-(n-2)*.5f)*ShipTune.Ripple;Lay((i-(n-2)*.5f)*ShipTune.LaneSpacing,lands,lands-t);}
                }
                for(int i=0;i<lanes.Count;i++)
                {
                    if(landed[i]||t<landAt[i])continue;landed[i]=true;
                    Strike(lanes[i],ShipTune.BroadsideDamage,DamageType.Kinetic,.3f,0,i<n?first:again,true);
                    AudioManager.PlayAt("explosionCrunch_000",transform,lanes[i].Origin+way*12,.22f,AudioBus.SFX,50,.05f,.8f);
                }
                ThreatFrom(lanes);Pose(-2*Mathf.Clamp01(t/tell));
                if(!second&&landed.TrueForAll(l=>l))break;
                yield return null;
            }
            strikeAt=0;Pose(0);Finish(ShipTune.Rest[k]);
        }
        /// <summary>Her craft's beam down a marked lane through the lead (twice from her last projector), then her shield is down while it recovers: the opening.</summary>
        IEnumerator ShipLance()
        {
            int k=ShipStage;int shots=k>=2?2:1;
            for(int s=0;s<shots;s++)
            {
                var lead=PartyController.Current.Active.transform.position;var way=Toward(lead);transform.rotation=Quaternion.LookRotation(way);
                var origin=transform.position+way*ShipTune.Radius;origin.y=transform.position.y;float length=ShipTune.LanceLength;
                if(CombatCover.Sweep(origin,origin+way*length,.4f,out var wall))length=Mathf.Max(2,wall.distance);
                float tell=s>0?ShipTune.LanceNext:ShipTune.LanceTell[k];
                var lane=Mark(Telegraph.Line(origin,way,length,ShipTune.LanceWidth,tell,Telegraph.Heavy));
                AudioManager.PlayAt("forceField_000",transform,origin,.26f,AudioBus.SFX,50,.05f,.7f);
                yield return null;// from the marking's own first frame
                for(float t=0;t<tell;)
                {
                    t+=ShipDt;if(Interrupted)yield break;ThreatFrom(ShipOnly(lane));
                    if(Time.frameCount%3==0)CombatVfx.Burst(origin,ShipBeamTone,"steam");Pose(-3*Mathf.Clamp01(t/tell));yield return null;
                }
                Strike(lane,ShipTune.LanceDamage,DamageType.Beam,.3f);strikeAt=0;Debug.Log("MERET_LANCE "+(s+1));
                AudioManager.PlayAt("laserSmall_000",transform,origin,.3f,AudioBus.SFX,60,.05f,.45f);
                var cut=new GameObject("Her lance");temporary.Add(cut);var drawn=cut.AddComponent<LineRenderer>();drawn.positionCount=2;drawn.useWorldSpace=true;
                drawn.SetPosition(0,origin+Vector3.up*.3f);drawn.SetPosition(1,origin+way*length+Vector3.up*.3f);drawn.startWidth=1.2f;drawn.endWidth=.8f;
                drawn.sharedMaterial=ActorFactory.Energy(ShipBeamTone);drawn.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                for(float t=0;t<ShipTune.LanceShows;){t+=ShipDt;yield return null;}
                temporary.Remove(cut);Destroy(cut);Pose(0);
            }
            // She fired through her shield: it is down while it comes back.
            Debug.Log("MERET_SHIELD_DROPS lance");ShipShieldDrops(ShipTune.ShieldDown[k]);
            for(float t=0;t<ShipTune.ShieldDown[k];){t+=ShipDt;yield return null;}
            openingUntil=0;Finish(ShipTune.Rest[k]);
        }
        /// <summary>A tractor line at the lead: the first ship along it with nothing between is struck and reeled toward her until it rolls, cover breaks it, it runs out or it comes close. Her
        /// shield is down while it pulls and a moment after, caught or not: the projectors are a lunge away from a ship pulled in.</summary>
        IEnumerator ShipTractor()
        {
            var party=PartyController.Current;var lead=party.Active.transform.position;var way=Toward(lead);transform.rotation=Quaternion.LookRotation(way);
            var origin=transform.position+way*ShipTune.Radius;origin.y=transform.position.y;
            float length=Mathf.Clamp(Flat(lead-origin).magnitude+2,6,ShipTune.TractorLength);
            var line=Mark(Telegraph.Line(origin,way,length,ShipTune.TractorWidth,ShipTune.TractorTell,Telegraph.Heavy));
            AudioManager.PlayAt("forceField_000",transform,origin,.26f,AudioBus.SFX,50,.05f,.5f);
            // Counted from the marking's own first frame: it was laid this frame, and counting this one put the blow a frame ahead of what the marking (and whoever reads it) says.
            yield return null;
            for(float t=0;t<ShipTune.TractorTell;){t+=ShipDt;if(Interrupted)yield break;ThreatFrom(ShipOnly(line));Pose(0,0,.03f*Mathf.Clamp01(t/ShipTune.TractorTell));yield return null;}
            line.Release();strikeAt=0;
            CombatActor caught=null;float nearest=float.PositiveInfinity;
            foreach(var member in party.members)
            {
                if(!member.Health.Alive||!line.Contains(member.transform.position)||!CombatCover.Clear(origin,member.transform.position,.3f))continue;
                float along=Vector3.Dot(Flat(member.transform.position-origin),way);if(along<nearest){nearest=along;caught=member;}
            }
            if(caught!=null&&caught.Health.Receive(Packet(ShipTune.TractorDamage,DamageType.Pulse))<=0)caught=null;
            Debug.Log("MERET_TRACTOR "+(caught!=null?"caught "+caught.character:"missed"));
            // The tractor runs through her shield: down while it pulls, and a moment after.
            shipPulling=true;ShipShieldDrops(caught!=null?ShipTune.ReelSeconds+ShipTune.TractorOpen:ShipTune.TractorOpen);
            if(caught!=null)
            {
                caught.Stagger(.2f);
                var go=new GameObject("Her tractor line");temporary.Add(go);var tether=go.AddComponent<LineRenderer>();tether.positionCount=2;tether.useWorldSpace=true;tether.startWidth=.35f;tether.endWidth=.5f;
                tether.sharedMaterial=ActorFactory.Energy(ShipTractorTone);tether.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                var body=caught.GetComponent<CharacterController>();string broke="close";
                for(float t=0;t<ShipTune.ReelSeconds;)
                {
                    float dt=ShipDt;t+=dt;if(!health.Alive){broke="down";break;}
                    if(dt<=0){yield return null;continue;}
                    var from=transform.position+Toward(caught.transform.position)*ShipTune.Radius;from.y=transform.position.y;var gap=Flat(from-caught.transform.position);float distance=gap.magnitude;
                    if(!caught.Health.Alive){broke="fell";break;}
                    if(caught.State==ActorState.Dodge){broke="rolled";break;}
                    if(distance>ShipTune.ReelBreaks){broke="far";break;}
                    if(!CombatCover.Clear(from,caught.transform.position,.3f)){broke="cover";break;}
                    if(distance<=ShipTune.ReelStops)break;
                    caught.Health.Hinder(ShipTune.ReelPace);
                    if(body!=null&&body.enabled)body.Move(gap/distance*Mathf.Min(distance-ShipTune.ReelStops,ShipTune.ReelSpeed*dt));
                    tether.SetPosition(0,from);tether.SetPosition(1,caught.transform.position);Pose(-3,.08f,.02f);
                    yield return null;
                }
                temporary.Remove(go);Destroy(go);Pose(0);Debug.Log("MERET_TRACTOR_ENDS "+broke);
            }
            shipPulling=false;
            for(float t=0;Time.time<openingUntil&&t<ShipTune.TractorOpen+.1f;){t+=ShipDt;yield return null;}
            openingUntil=0;Finish(ShipTune.Rest[ShipStage]);
        }
        /// <summary>Clamps up through the plating at the seats nearest the lead: each marked, then risen and solid at the plane for as long as she holds it, whoever stood in it struck and set
        /// beside it first (D289: nothing a fight makes solid walls a hero in).</summary>
        IEnumerator ShipClamps(int count)
        {
            int k=ShipStage;var party=PartyController.Current;var lead=party.Active.transform.position;
            var free=new List<MeretClamp>();foreach(var c in shipClamps)if(c!=null&&!c.Up)free.Add(c);
            free.Sort((a,b)=>Flat(a.AtPlane-lead).sqrMagnitude.CompareTo(Flat(b.AtPlane-lead).sqrMagnitude));
            if(free.Count==0){Finish(.4f);yield break;}
            var rising=free.GetRange(0,Mathf.Min(count,free.Count));float tell=ShipTune.ClampTell[k];
            var marks=new List<Telegraph>();foreach(var clamp in rising)marks.Add(Mark(Telegraph.Circle(clamp.AtPlane,ShipTune.ClampMark,tell,Telegraph.Heavy)));
            Debug.Log($"MERET_CLAMPS {string.Join(",",rising.ConvertAll(c=>c.index.ToString()))} phase={Phase}");
            // She marks the first for them herself.
            if(!shipClampSaid){shipClampSaid=true;BarkService.PlayAt("Meret","ship",1,true);}
            AudioManager.PlayAt("district_lift",null,rising[0].AtPlane,.28f,AudioBus.SFX,50,.05f,.8f);
            // Counted from the marking's own first frame: it was laid this frame, and counting this one put the blow a frame ahead of what the marking (and whoever reads it) says.
            yield return null;
            for(float t=0;t<tell;){t+=ShipDt;if(Interrupted)yield break;ThreatFrom(marks);yield return null;}
            var once=new HashSet<Health>();
            for(int i=0;i<rising.Count;i++)
            {
                Strike(marks[i],ShipTune.ClampDamage,DamageType.Kinetic,.35f,0,once);
                ShipClearSeat(rising[i]);rising[i].Rise(ShipTune.ClampStands[k]);
                CombatVfx.Burst(rising[i].AtPlane,new Color(.8f,.85f,.9f),"kill");
            }
            Tremor(.35f,.07f);strikeAt=0;Finish(ShipTune.Rest[k]);
        }
        /// <summary>Anyone where a clamp is about to stand solid is set beside it, clear by a hull's width (D289).</summary>
        void ShipClearSeat(MeretClamp clamp)
        {
            var party=PartyController.Current;if(party==null)return;var at=clamp.AtPlane;
            foreach(var member in party.members)
            {
                if(member==null)continue;float hull=HeroCollision.HullRadius(member.character);var off=Flat(member.transform.position-at);
                float clear=MeretClamp.Tune.Solid+hull+.4f;if(off.magnitude>=clear)continue;
                var dir=off.sqrMagnitude>.01f?off.normalized:-shipFacing;var to=at+dir*clear;to.y=member.transform.position.y;
                var body=member.GetComponent<CharacterController>();bool on=body!=null&&body.enabled;if(body!=null)body.enabled=false;member.transform.position=to;if(body!=null)body.enabled=on;
                Debug.Log($"MERET_CLAMP_CLEARS {member.character} {clamp.index}");
            }
            Physics.SyncTransforms();
        }
    }
}
