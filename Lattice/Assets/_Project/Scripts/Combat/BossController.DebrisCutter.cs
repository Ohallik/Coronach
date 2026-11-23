using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The Debris Cutter, chapter 9's second boss (in flight, in the Broken Ring, while Cinder Halo's districts lift away round the party): a Compact interceptor with three weapon pods, sent with
    /// orders Meret wrote. It is a chase among things the player loves, and it ends with the Cutter dead in space and whole (<see cref="Ending.ShutDown"/>): its crew live, and Bryn's tug takes it
    /// (<see cref="TowedOff"/>). The Ring's contract is docs/maps/BrokenRing.md, "Cutter arena contract". There is no escort bar and no civilian-loss score: the districts (<see cref="RingDistrict"/>)
    /// are landmarks and cover.
    /// <list type="bullet">
    /// <item><b>Three pods, the player's choice.</b> The cutting beam under its nose, the mine-layer at its stern, the tractor on its starboard flank, each a target of its own. A pod is shut while
    /// cold (a cyan knot; a blow passes to the hull) and open while it <b>vents</b> (amber), for <see cref="CutterTune.Vent"/> seconds after it has fired: the first vent says so on screen. While a
    /// pod vents the Cutter keeps it on the ship it fired at, to fire again the moment it cools, so whichever pod a player chooses faces him while it can be struck (the mine-layer at its stern
    /// faced away from the lead for the whole fight). <b>The bar is paid in blows, and a pod decides which and when, never how much</b> (D272: a boss whose parts take bar with them gave the
    /// replay party half again of flat out): a blow on a venting pod comes off the bar as a blow on the hull does, and each time the bar passes a third the pod struck most is disabled, its
    /// hunt changing with it. A pod carries a third of the bar, so one struck through by itself goes at its line and no sooner. The bar run out, it is traffic.</item>
    /// <item><b>Its pursuit changes with what it has lost.</b> The first pod gone is its shift ("It's changed how it hunts"): with no beam it closes in, rams on its runs and crushes a ship its
    /// tractor reels in; with no mines it stands off and cuts twice; with no tractor it boxes a ship in with two rows of mines. Two players who went for different pods first fought different
    /// fights from there.</item>
    /// </list>
    /// Its moves: <b>Beam</b> (a marked lane through the lead, stopped by a district), <b>Mines</b> (a row of marked circles across the lane the lead is flying, each a fuse and a burst, with gaps),
    /// <b>Tractor</b> (a marked line; the first ship in it is struck and reeled toward it until it rolls, cover breaks the line, or the line runs out), and <b>Run</b> (a marked lane through the
    /// party at speed, struck by what it touches, then a wide turn round a district: open). Between them it weaves among the districts to a point at its distance (<see cref="CutterLane"/>; in the
    /// yard it circles the lead). It never fights by its brain's own attack: a Mine to its brain, with an attack that reaches nobody, flown by this file.
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit DebrisCutterKit=Register(new Kit
        {
            id="DebrisCutter",title="THE DEBRIS CUTTER",epithet="A Compact interceptor among the departing streets",thresholds=new[]{CutterTune.Shift},
            choose=(boss,hero)=>boss.ChooseCutter(hero),
            moves={["Beam"]=boss=>boss.CutterBeam(),["Mines"]=boss=>boss.CutterMines(),["Tractor"]=boss=>boss.CutterTractor(),["Run"]=boss=>boss.CutterRun()},
            signature=phase=>phase>=2?"Run":null,
            shift=boss=>boss.CutterChangesHunt(),shiftText="IT CHANGES HOW IT HUNTS  ·  BRACE",
            status=boss=>boss.CutterStatus(),
            fightGoal=(boss,label)=>boss.CutterGoal(label),
            // Dead in space, and BrynTowsCutter follows: the shared "boss" bark would be said over Bryn.
            capsuleRadius=CutterTune.Radius,occlusion=true,ending=Ending.ShutDown,downBark="",
            start=boss=>boss.CutterStart(),update=boss=>boss.CutterUpdate(),down=boss=>boss.CutterDown(),
        });
        /// <summary>The Debris Cutter's numbers, all of them (its integrity is its EnemyDef's: HomeBuilder.Bosses). Damage is before the second phase's +18%; a boss stays at threat 1. Every move is
        /// authored at 168 to 170: 12.1% of Idra's bar at Sync 27 in Choirwrought parts to 15.9% of Taren's at 26 in Tempered (flying, a ship's bar and Plating are its hero's:
        /// HomeBuilder.Bosses' sizing).</summary>
        public static class CutterTune
        {
            /// <summary>Its capsule's radius in flight (5.6 m tall: its definition's). Its one shift is the first pod gone.</summary>
            public const float Radius=2.6f,Shift=2/3f,PodBar=1/3f;
            /// <summary>The pods: how far proud of its capsule each stands, its target's size, its integrity as a share of the Cutter's (the third of the bar it goes at), how long it vents (open)
            /// after it fires.</summary>
            public const float PodOut=.7f,PodTarget=1.4f,PodShare=PodBar,Vent=4.5f;
            public const int Beam=0,Mines=1,Tractor=2,Pods=3;
            public static readonly string[] PodNames={"Beam pod","Mine-layer","Tractor pod"},PodWords={"BEAM POD","MINE-LAYER","TRACTOR POD"};
            /// <summary>How it flies between moves: its pace, its turn, and the distance it keeps from the lead whole, closing (no beam), standing off (no mines), boxing (no tractor); with one pod
            /// left, by which.</summary>
            public const float Cruise=13,Turn=140,KeepWhole=18,KeepClose=10,KeepOff=26,KeepBoxing=18,KeepBeamOnly=24,KeepMinesOnly=16,KeepTractorOnly=12,OrbitRate=18;
            /// <summary>Beam: its lane's reach and width, the tell whole and hunting, the blow, how long the cut shows, the second cut's tell (standing off).</summary>
            public const float BeamLength=34,BeamWidth=2.6f,BeamTell=1.1f,BeamTellLater=.95f,BeamDamage=170,BeamHolds=.35f,SecondCut=.6f;
            /// <summary>Mines: how many in a row, how far apart, each burst's circle, the first fuse and the step between fuses, the blow, and (boxing) how far behind the lead the second row lies.</summary>
            public const int MineCount=5;
            public const float MineSpacing=9.6f,MineRadius=2.6f,MineFuse=1.5f,MineStagger=.12f,MineDamage=168,MineBehind=11;
            /// <summary>Tractor: its line's longest and width, the tell, the catch, the reel (pace, speed, longest, where it stops, past what distance it breaks), and (closing) the crush at its nose.</summary>
            public const float TractorLength=30,TractorWidth=2.4f,TractorTell=.9f,TractorDamage=168,ReelPace=.45f,ReelSpeed=7,ReelSeconds=2.5f,ReelStops=7,ReelBreaks=32,CrushTell=.6f,CrushRadius=3.6f,CrushDamage=170;
            /// <summary>Run: the tell whole and hunting, the lane's width, the pace, what it strikes in passing, how far past the lead and its shortest and longest, how long it turns wide (open).</summary>
            public const float RunTell=1,RunTellLater=.85f,RunWidth=3.6f,RunSpeed=28,RunContact=2.8f,RunDamage=170,RunPast=12,RunShortest=16,RunLongest=36,TurnsWide=2.6f;
            /// <summary>Its rest between moves, whole and hunting.</summary>
            public const float RestWhole=2.2f,RestHunting=1.9f;
            /// <summary>When a radio line follows the party's own, so each can be read.</summary>
            public const float RadioAfter=1.9f;
        }
        static readonly Color PodCold=new(.45f,1.45f,1.6f,.95f),PodVenting=new(1.7f,.95f,.35f,.95f),CutterBeamTone=new(1.6f,.7f,.35f,.95f),MineTone=new(1.5f,.5f,.25f,.9f),TractorTone=new(.6f,1.4f,1.6f,.9f);
        sealed class CutterPod{public Health health;public Transform node;public Renderer knot;public SphereCollider shape;public float ventUntil=-1,left;public bool down,open;public int index;}
        readonly List<CutterPod> cutterPods=new();
        int cutterFirstDown=-1,cutterDown,cutterRadio,cutterLastVent=-1;float cutterKeepUntil,cutterOrbit,cutterStruckAt=-1;Vector3 cutterStation;bool cutterVentSaid,cutterMarketSaid,cutterHears;string cutterOpenWhy="";
        float cutterRadius=CutterTune.Radius;HullKeepsOff cutterHull;bool cutterRunning;
        /// <summary>Its pods still working, and whether one is (by index: <see cref="CutterTune.Beam"/>, <see cref="CutterTune.Mines"/>, <see cref="CutterTune.Tractor"/>).</summary>
        public int CutterPodsLeft=>CutterTune.Pods-cutterDown;
        public bool CutterHas(int pod)=>pod>=0&&pod<cutterPods.Count&&!cutterPods[pod].down;
        public Health CutterPodHealth(int pod)=>pod>=0&&pod<cutterPods.Count?cutterPods[pod].health:null;
        public bool CutterVenting(int pod)=>CutterHas(pod)&&Time.time<cutterPods[pod].ventUntil;
        /// <summary>The pod disabled first, or -1: what its pursuit became.</summary>
        public int CutterFirstDown=>cutterFirstDown;
        /// <summary>How it hunts now, in a word: whole, close (no beam), standoff (no mines), boxing (no tractor); with one pod left, the one it has.</summary>
        public string CutterMode{get{if(cutterDown==0)return "whole";if(cutterDown==1)return cutterFirstDown==CutterTune.Beam?"close":cutterFirstDown==CutterTune.Mines?"standoff":"boxing";
            return CutterHas(CutterTune.Beam)?"beam":CutterHas(CutterTune.Mines)?"mines":"tractor";}}
        float CutterKeep=>CutterMode switch{"close"=>CutterTune.KeepClose,"standoff"=>CutterTune.KeepOff,"boxing"=>CutterTune.KeepBoxing,"beam"=>CutterTune.KeepBeamOnly,"mines"=>CutterTune.KeepMinesOnly,"tractor"=>CutterTune.KeepTractorOnly,_=>CutterTune.KeepWhole};
        float CutterRest=>cutterDown>0?CutterTune.RestHunting:CutterTune.RestWhole;
        /// <summary>The pod it keeps on the ship it fired at while it vents (the one vented last, else any venting), or -1.</summary>
        int CutterHeldPod{get{if(CutterVenting(cutterLastVent))return cutterLastVent;for(int i=0;i<CutterTune.Pods;i++)if(CutterVenting(i))return i;return -1;}}
        Vector3 CutterPodAt(int pod)=>pod==CutterTune.Beam?transform.position+transform.forward*(cutterRadius+CutterTune.PodOut):pod==CutterTune.Mines?transform.position-transform.forward*(cutterRadius+CutterTune.PodOut):transform.position+transform.right*(cutterRadius+CutterTune.PodOut);

        void CutterStart()
        {
            cutterRadius=GetComponent<CharacterController>().radius;brain.Rooted=true;cutterStation=transform.position;cutterOrbit=0;
            cutterPods.Clear();
            for(int i=0;i<CutterTune.Pods;i++)
            {
                var node=new GameObject(CutterTune.PodNames[i]);node.transform.SetParent(transform,false);
                node.transform.localPosition=i==CutterTune.Beam?Vector3.forward*(cutterRadius+CutterTune.PodOut):i==CutterTune.Mines?Vector3.back*(cutterRadius+CutterTune.PodOut):Vector3.right*(cutterRadius+CutterTune.PodOut);
                var part=node.AddComponent<Health>();part.id=CutterTune.PodNames[i];part.maximum=part.integrity=health.maximum*CutterTune.PodShare;part.weakness=health.weakness;part.resistance=health.resistance;part.breakThreshold=1e9f;
                var shape=node.AddComponent<SphereCollider>();shape.isTrigger=true;shape.radius=CutterTune.PodTarget;shape.enabled=false;part.Damageable=false;
                node.AddComponent<Hurtbox>().owner=part;
                var drawn=GameObject.CreatePrimitive(PrimitiveType.Sphere);drawn.name="Knot";Destroy(drawn.GetComponent<Collider>());drawn.transform.SetParent(node.transform,false);drawn.transform.localScale=Vector3.one*CutterTune.PodTarget*.8f;
                var knot=drawn.GetComponent<Renderer>();knot.sharedMaterial=ActorFactory.Energy(PodCold);knot.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;knot.receiveShadows=false;
                // Shut until it fires: no blow reaches it, and it can't be targeted, from the approach or between its vents.
                var pod=new CutterPod{health=part,node=node.transform,knot=knot,shape=shape,index=i,left=part.integrity};cutterPods.Add(pod);
                part.Damaged+=(_,packet,amount)=>CutterPodStruck(pod,packet,amount);
                part.Died+=(_,packet)=>CutterPodDown(pod,packet,false);
            }
            // No hold and no plate: the bar is paid in blows, on the hull or on a venting pod alike.
            health.Holds=null;health.Plate=null;
            // No ship sinks into its hull: its capsule is 2.6 m round a hull drawn about twice that long, and Idra's tug flew into its underside mid-fight (whole-game capture, November
            // 20, s61/065). What is drawn about the ships' height keeps them off; what strikes it and what it strikes stay its capsule's (HullKeepsOff).
            cutterHull=HullKeepsOff.Fit(gameObject,visual,-1.2f,2.8f);
            Debug.Log($"CUTTER_HULL corners={cutterHull.outline.Length} reach={cutterHull.Reach:0.0} capsule={cutterRadius:0.0} whole={HullKeepsOff.Furthest(HullKeepsOff.Outline(gameObject,visual,-10,20)):0.0}");
            health.Damaged+=CutterStruck;cutterHears=true;
            // She holds the Ring: whenever she isn't the ship leading, the radio says so (manifest section 7).
            CutterRadio("Idra","hail.ring");
            Debug.Log($"CUTTER_WAKES pods={CutterTune.Pods} lanes={CutterLane.All.Count}");
        }
        /// <summary>A line on the radio from somebody not in the fight, after the party's own, on the party's clock (the Cutter may be dead by then). Idra's only while she isn't leading.</summary>
        void CutterRadio(string speaker,string situation,int index=-1)
        {
            var party=PartyController.Current;if(party==null)return;
            if(speaker=="Idra"&&party.Active!=null&&party.Active.character=="Idra")return;
            if(speaker=="Idra"&&index<0)index=Mathf.Min(cutterRadio++,2);
            party.StartCoroutine(Later());
            IEnumerator Later(){yield return new WaitForSeconds(CutterTune.RadioAfter);if(index>=0)BarkService.PlayAt(speaker,situation,index,true);else BarkService.Play(speaker,situation,true);}
        }
        void CutterStruck(Health _,DamagePacket packet,float amount)
        {
            if(packet.source==null||!packet.source.friendly)return;
            if(engaged<0)engaged=Time.realtimeSinceStartup;
            if(amount>0)cutterStruckAt=Time.time;
        }
        /// <summary>A blow on a venting pod: what the pod lost comes off the bar too, as a blow on the hull would (the pod is part of it). A blow that would empty the bar is the one that beats
        /// it, so it lands as a blow, with its striker.</summary>
        void CutterPodStruck(CutterPod pod,DamagePacket packet,float amount)
        {
            if(packet.source==null||!packet.source.friendly||health==null||!health.Alive)return;
            if(engaged<0)engaged=Time.realtimeSinceStartup;
            float lost=Mathf.Max(0,pod.left-pod.health.integrity);pod.left=pod.health.integrity;
            if(amount<=0||lost<=0)return;
            CombatActor.PayHit(packet,amount);cutterStruckAt=Time.time;
            if(health.integrity-lost>1){health.integrity-=lost;return;}
            // The last pod struck through: its own fall is the blow that beats it, with the party's line for it (CutterPodDown, raised next).
            if(!pod.health.Alive&&cutterDown+1>=CutterTune.Pods){health.integrity=1;return;}
            health.InvulnerableUntil=0;health.Receive(new DamagePacket{source=packet.source,amount=health.maximum*100,type=health.weakness,tag="pod",weight=HitWeight.Heavy});
        }
        /// <summary>A pod open (venting) or shut: its target and its knot.</summary>
        void CutterPodOpen(CutterPod pod,bool open)
        {
            if(pod.down)open=false;if(pod.open==open)return;pod.open=open;
            pod.health.Damageable=open;if(pod.shape!=null)pod.shape.enabled=open;
        }
        /// <summary>A pod disabled: the one struck most as the bar passes a third, or one struck through by itself (a third of the bar on it, so at its line). Nothing more comes off the bar for
        /// it than the blows it took; the bar is only kept from standing over the line of the pods gone, so the bar and the pods never disagree. The first is its shift, and its hunt changes; the
        /// third leaves it traffic. <paramref name="settling"/>: taken off because its bar was set past the pod's line with no blow landing (a test sets it), which plays nothing.</summary>
        void CutterPodDown(CutterPod pod,DamagePacket packet,bool settling)
        {
            if(this==null||health==null||!health.Alive||pod.down)return;
            CutterPodOpen(pod,false);pod.down=true;pod.ventUntil=-1;
            if(pod.knot!=null)pod.knot.enabled=false;
            cutterDown++;if(engaged<0)engaged=Time.realtimeSinceStartup;if(cutterFirstDown<0)cutterFirstDown=pod.index;
            CombatVfx.Burst(pod.node.position,new Color(.5f,.5f,.52f),"kill");AudioManager.PlayAt("explosionCrunch_000",transform,pod.node.position,.3f,AudioBus.SFX,60,.05f,1.1f);
            Debug.Log($"CUTTER_POD_DOWN {pod.index} left={CutterPodsLeft}");
            if(!settling)BarkService.PlayAt(PartyController.Current.Active.character,"pod",Mathf.Clamp(cutterDown-1,0,2),true);
            if(cutterDown>=CutterTune.Pods)
            {
                // No beam, no mines and no line: the blow that took the last pod is the one that beat it.
                health.Holds=null;health.Plate=null;health.InvulnerableUntil=0;
                health.Receive(new DamagePacket{source=packet.source,amount=health.maximum*100,type=health.weakness,tag="pod",weight=HitWeight.Heavy});
                return;
            }
            if(!settling)health.integrity=Mathf.Max(1,Mathf.Min(health.integrity,(1-cutterDown*CutterTune.PodBar)*health.maximum-1));
            if(cutterDown==1)Debug.Log("CUTTER_HUNTS "+CutterMode);else Debug.Log("CUTTER_CHANGES "+CutterMode);
            if(settling)return;
            if(cutterDown<=2){CutterRadio("Neve","hail.lane",cutterDown-1);if(cutterDown==1)CutterRadio("Idra","hail.ring");}
            // Whatever it was doing, the pod going stops it: a tractor's line drops, a run ends; it lurches, open a moment. Not when its bar took the pod (a test's setting is no blow).
            StopAllCoroutines();Interrupt();busy=false;strikeAt=0;OpenFor(1.4f);nextSpecial=Mathf.Max(nextSpecial,Time.time+2);cutterOpenWhy="A POD IS GONE";
        }
        void CutterChangesHunt()=>Debug.Log("CUTTER_HUNT_SHIFT "+CutterMode);
        void CutterUpdate()
        {
            brain.Rooted=true;
            // Its hull keeps the ships off again once a run through them is over, however it ended (a run cut short by a pod going leaves no one to switch it back).
            if(cutterRunning&&!busy){cutterRunning=false;if(cutterHull!=null)cutterHull.enabled=true;}
            // Each third the bar passes takes the pod struck most (the player's choice: a pod nobody struck goes in order, beam first). Set past a line with no blow landing (a test), it goes
            // quietly.
            if(cutterDown<CutterTune.Pods-1)
            {
                int crossed=0;for(int k=1;k<CutterTune.Pods;k++)if(health.integrity<health.maximum*(1-k*CutterTune.PodBar)-.5f)crossed++;
                bool struck=cutterStruckAt>=0&&Time.time-cutterStruckAt<.5f;
                for(int guard=0;cutterDown<crossed&&guard<CutterTune.Pods;guard++)
                {
                    CutterPod most=null;float least=float.PositiveInfinity;
                    foreach(var pod in cutterPods){if(pod.down)continue;float left=pod.health.integrity/Mathf.Max(1,pod.health.maximum);if(left<least-.001f){least=left;most=pod;}}
                    if(most==null)break;CutterPodDown(most,default,!struck);
                }
            }
            CutterLooks();
            if(busy||health.Broken||brain.Passive)return;
            CutterWeave(Time.deltaTime);
        }
        void CutterLooks()
        {
            foreach(var pod in cutterPods)
            {
                if(pod.down||pod.knot==null)continue;bool open=Time.time<pod.ventUntil;CutterPodOpen(pod,open);pod.knot.sharedMaterial=ActorFactory.Energy(open?PodVenting:PodCold);
                pod.knot.transform.localScale=Vector3.one*CutterTune.PodTarget*.8f*(open?1+.3f*Mathf.Sin(Time.time*10):1);
                if(open&&Time.frameCount%7==pod.index)CombatVfx.Burst(pod.node.position,new Color(.8f,.85f,.9f,.5f),"steam");
            }
        }
        /// <summary>Between moves: off to a point of the weave at its distance from the lead (in the yard, round the lead), nose on him.</summary>
        void CutterWeave(float dt)
        {
            var party=PartyController.Current;if(party==null||party.Active==null||dt<=0)return;
            var lead=party.Active.transform.position;
            if(Time.time>=cutterKeepUntil)
            {
                cutterKeepUntil=Time.time+1.5f;var lane=CutterLane.AtDistance(lead,CutterKeep);
                if(lane!=null)cutterStation=lane.transform.position;
                else{cutterOrbit+=CutterTune.OrbitRate*1.5f;var from=Flat(transform.position-lead);from=from.sqrMagnitude>.01f?from.normalized:Vector3.back;cutterStation=lead+Quaternion.Euler(0,CutterTune.OrbitRate*1.5f,0)*from*CutterKeep;}
                cutterStation.y=transform.position.y;
            }
            var controller=GetComponent<CharacterController>();var off=Flat(cutterStation-transform.position);
            // A weave, not a straight line: it rocks across its heading as it goes.
            var across=Vector3.Cross(Vector3.up,off.sqrMagnitude>.01f?off.normalized:transform.forward);
            if(off.magnitude>1.5f&&controller.enabled)controller.Move((off.normalized+across*.35f*Mathf.Sin(Time.time*1.6f))*Mathf.Min(off.magnitude,CutterTune.Cruise*dt));
            // Nose on him; with a pod venting, that pod on him (it keeps a spent weapon on its mark, to fire again the moment it cools): the mine-layer's stern, the tractor's flank.
            var toLead=Flat(lead-transform.position);int held=CutterHeldPod;
            var look=held==CutterTune.Mines?-toLead:held==CutterTune.Tractor?Quaternion.Euler(0,-90,0)*toLead:toLead;
            if(look.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(look),CutterTune.Turn*dt);
            Pose(2*Mathf.Sin(Time.time*1.3f),.12f*Mathf.Sin(Time.time*2.1f));
        }
        void CutterDown()
        {
            if(cutterHears){health.Damaged-=CutterStruck;cutterHears=false;}
            health.Holds=null;health.Plate=null;
            // Dead in space it is still a hull a ship stops at, run or no run.
            if(cutterHull!=null)cutterHull.enabled=true;
            foreach(var pod in cutterPods){CutterPodOpen(pod,false);pod.ventUntil=-1;if(pod.knot!=null)pod.knot.enabled=false;}
            // Dead in space and whole: it stays where it went dead, its lights out, until the tug takes it.
            var presentation=GetComponent<DefeatPresentation>();if(presentation!=null)presentation.enabled=false;
            if(visual!=null){visual.localPosition=visualRest;visual.localRotation=visualRestRotation*Quaternion.Euler(4,0,9);
                var block=new MaterialPropertyBlock();foreach(var body in visual.GetComponentsInChildren<Renderer>()){body.GetPropertyBlock(block);block.SetColor("_EmissionColor",Color.black);body.SetPropertyBlock(block);}}
            Debug.Log("CUTTER_DEAD_IN_SPACE");
        }
        string CutterStatus()
        {
            if(health==null||!health.Alive)return "";
            foreach(var pod in cutterPods)if(!pod.down&&Time.time<pod.ventUntil)return "ITS "+CutterTune.PodWords[pod.index]+" IS VENTING  ·  STRIKE IT";
            if(health.Broken&&Time.time<openingUntil)return cutterOpenWhy+"  ·  STRIKE";
            return "";
        }
        string CutterGoal(string label)
        {
            if(health==null||!health.Alive||cutterPods.Count==0)return label;
            return cutterDown==0?"Disable a weapon pod: your choice which first (1/3)":$"Disable a weapon pod ({Mathf.Min(cutterDown+1,CutterTune.Pods)}/{CutterTune.Pods})";
        }
        void CutterVents(int pod,float seconds)
        {
            if(!CutterHas(pod))return;cutterPods[pod].ventUntil=Mathf.Max(cutterPods[pod].ventUntil,Time.time+seconds);CutterPodOpen(cutterPods[pod],true);cutterLastVent=pod;
            Debug.Log("CUTTER_VENTS "+pod);AudioManager.PlayAt("forceField_000",transform,CutterPodAt(pod),.2f,AudioBus.SFX,60,.05f,1.4f);
            if(cutterVentSaid)return;
            // The first pod to open says what the pods are, once: never a target struck with nothing said.
            cutterVentSaid=true;Debug.Log("CUTTER_FIRST_VENT "+pod);
            Notices.Post("A POD IS OPEN","The Cutter's three weapon pods are shut until they fire. The beam pod under its nose vents after a cut, the mine-layer at its stern after it lays, the tractor on its flank while it pulls, and it keeps a venting pod turned on you: strike the one you choose while it glows amber. Each third of its bar takes the pod struck most, and each one gone changes how it hunts.");
        }

        // ---------------------------------------------------------------- moves
        (string,IEnumerator) ChooseCutter(CombatActor hero)
        {
            float distance=Flat(hero.transform.position-transform.position).magnitude;string mode=CutterMode;
            bool beam=CutterHas(CutterTune.Beam),mines=CutterHas(CutterTune.Mines),tractor=CutterHas(CutterTune.Tractor);
            float Beam()=>!beam?0:mode=="standoff"||mode=="beam"?2:mode=="boxing"?1.4f:1.3f;
            float Mines()=>!mines?0:mode=="boxing"||mode=="mines"?1.8f:mode=="close"?1.2f:1.3f;
            float Tractor()=>!tractor||distance<cutterRadius+4||distance>CutterTune.TractorLength?(!tractor?0:.3f):mode=="close"||mode=="tractor"?1.6f:1.3f;
            float Run()=>distance<cutterRadius+3?.4f:mode=="close"?1.8f:mode=="standoff"?.8f:1f;
            string move=Pick(("Beam",Beam()),("Mines",Mines()),("Tractor",Tractor()),("Run",Run()));
            return move switch{"Beam"=>(move,CutterBeam()),"Mines"=>(move,CutterMines()),"Tractor"=>(move,CutterTractor()),_=>(move,CutterRun())};
        }
        IEnumerator CutterBeam()
        {
            if(!CutterHas(CutterTune.Beam)){Finish(.4f);yield break;}
            int cuts=CutterMode=="standoff"||CutterMode=="beam"?2:1;
            for(int cut=0;cut<cuts;cut++)
            {
                var lead=PartyController.Current.Active.transform.position;var direction=Toward(lead);transform.rotation=Quaternion.LookRotation(direction);
                var origin=CutterPodAt(CutterTune.Beam);origin.y=transform.position.y;float length=CutterTune.BeamLength;RingDistrict stopped=null;
                // A district takes the cut: the lane ends where it meets one.
                if(CombatCover.Sweep(origin,origin+direction*length,.4f,out var wall)){length=Mathf.Max(2,wall.distance);stopped=RingDistrict.Of(wall.collider);}
                float tell=cut>0?CutterTune.SecondCut:Phase>=2?CutterTune.BeamTellLater:CutterTune.BeamTell;
                var lane=Mark(Telegraph.Line(origin,direction,length,CutterTune.BeamWidth,tell,Telegraph.Heavy));
                strikeAt=lane.Contains(lead)?Time.time+tell:0;
                AudioManager.PlayAt("forceField_000",transform,origin,.26f,AudioBus.SFX,50,.05f,.7f);
                for(float t=0;t<tell;)
                {
                    if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                    if(Time.frameCount%3==0)CombatVfx.Burst(origin,CutterBeamTone,"steam");Pose(-3*Mathf.Clamp01(t/tell));yield return null;
                }
                Strike(lane,CutterTune.BeamDamage,DamageType.Beam,.3f);strikeAt=0;Debug.Log("CUTTER_BEAM");
                AudioManager.PlayAt("laserSmall_000",transform,origin,.3f,AudioBus.SFX,60,.05f,.45f);
                var cutLine=new GameObject("Cutting beam");temporary.Add(cutLine);var drawn=cutLine.AddComponent<LineRenderer>();drawn.positionCount=2;drawn.useWorldSpace=true;
                drawn.SetPosition(0,origin+Vector3.up*.3f);drawn.SetPosition(1,origin+direction*length+Vector3.up*.3f);drawn.startWidth=1.1f;drawn.endWidth=.7f;
                drawn.sharedMaterial=ActorFactory.Energy(CutterBeamTone);drawn.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                if(stopped!=null)
                {
                    Debug.Log("CUTTER_BEAM_STOPPED "+stopped.id);CombatVfx.Burst(origin+direction*length,CutterBeamTone,"kill");
                    // Mira, on the market's own radio, the first time it is her awning (manifest section 7).
                    if(stopped.id=="market"&&!cutterMarketSaid){cutterMarketSaid=true;CutterRadio("Mira","hail.market");}
                }
                for(float t=0;t<CutterTune.BeamHolds;){if(!GameTime.Paused)t+=Time.deltaTime;yield return null;}
                temporary.Remove(cutLine);Destroy(cutLine);Pose(0);
            }
            // Spent, the pod vents: the moment to strike it.
            CutterVents(CutterTune.Beam,CutterTune.Vent);cutterOpenWhy="THE BEAM POD IS VENTING";
            Finish(CutterRest);
        }
        IEnumerator CutterMines()
        {
            if(!CutterHas(CutterTune.Mines)){Finish(.4f);yield break;}
            var lead=PartyController.Current.Active;var at=lead.transform.position;var heading=lead.motor is FlightMotor ship&&Flat(ship.Velocity).sqrMagnitude>1?Flat(ship.Velocity).normalized:Flat(at-transform.position).normalized;
            if(heading.sqrMagnitude<.01f)heading=transform.forward;var across=Vector3.Cross(Vector3.up,heading);
            // A row across the lane he is flying, its middle on him, with a ship's width between each pair; boxing, a second row behind him, offset half a step.
            var spots=new List<Vector3>();
            for(int i=0;i<CutterTune.MineCount;i++)spots.Add(at+across*((i-(CutterTune.MineCount-1)*.5f)*CutterTune.MineSpacing));
            if(CutterMode=="boxing"||CutterMode=="mines")for(int i=0;i<CutterTune.MineCount-1;i++)spots.Add(at-heading*CutterTune.MineBehind+across*((i-(CutterTune.MineCount-2)*.5f)*CutterTune.MineSpacing));
            var circles=new List<Telegraph>();var fuses=new List<float>();var shells=new List<(Transform shell,Vector3 to)>();var stern=CutterPodAt(CutterTune.Mines);int laid=0;
            foreach(var spot in spots)
            {
                var to=spot;to.y=transform.position.y;if(RingDistrict.Inside(to,CutterTune.MineRadius*.5f))continue;
                float fuse=CutterTune.MineFuse+laid*CutterTune.MineStagger;laid++;
                circles.Add(Mark(Telegraph.Circle(to,CutterTune.MineRadius,fuse,Telegraph.Heavy)));fuses.Add(fuse);
                var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.name="Mine";Destroy(shell.GetComponent<Collider>());shell.transform.localScale=Vector3.one*.9f;
                shell.GetComponent<Renderer>().sharedMaterial=ActorFactory.Energy(MineTone);shell.transform.position=stern;temporary.Add(shell);shells.Add((shell.transform,to));
            }
            Debug.Log("CUTTER_MINES "+laid);
            AudioManager.PlayAt("thrusterFire_000",transform,stern,.24f,AudioBus.SFX,50,.05f,1.3f);
            CutterVents(CutterTune.Mines,CutterTune.Vent+CutterTune.MineFuse);cutterOpenWhy="THE MINE-LAYER IS VENTING";
            var once=new HashSet<Health>();var burst=new bool[circles.Count];
            for(float t=0,last=fuses.Count>0?fuses[fuses.Count-1]:0;t<last+.05f;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;ThreatFrom(circles);
                for(int i=0;i<shells.Count;i++)if(shells[i].shell!=null)shells[i].shell.position=Vector3.Lerp(stern,shells[i].to,Mathf.Clamp01(t/.45f));
                for(int i=0;i<circles.Count;i++)
                {
                    if(burst[i]||t<fuses[i])continue;burst[i]=true;
                    Strike(circles[i],CutterTune.MineDamage,DamageType.Pulse,.25f,0,once);CombatVfx.Burst(circles[i].Origin,MineTone,"kill");
                    if(shells[i].shell!=null){temporary.Remove(shells[i].shell.gameObject);Destroy(shells[i].shell.gameObject);}
                }
                yield return null;
            }
            AudioManager.PlayAt("explosionCrunch_000",null,at,.26f,AudioBus.SFX,60,.05f,1.2f);
            strikeAt=0;Finish(CutterRest);
        }
        IEnumerator CutterTractor()
        {
            if(!CutterHas(CutterTune.Tractor)){Finish(.4f);yield break;}
            var party=PartyController.Current;var lead=party.Active.transform.position;
            // It turns its starboard flank, and the tractor, to the ship.
            var toLead=Flat(lead-transform.position);if(toLead.sqrMagnitude<.01f)toLead=transform.right;
            transform.rotation=Quaternion.LookRotation(Quaternion.Euler(0,-90,0)*toLead.normalized);
            var origin=CutterPodAt(CutterTune.Tractor);origin.y=transform.position.y;var direction=Flat(lead-origin);float length=Mathf.Clamp(direction.magnitude+2,6,CutterTune.TractorLength);direction=direction.sqrMagnitude>.01f?direction.normalized:transform.right;
            var line=Mark(Telegraph.Line(origin,direction,length,CutterTune.TractorWidth,CutterTune.TractorTell,Telegraph.Heavy));
            strikeAt=line.Contains(lead)?Time.time+CutterTune.TractorTell:0;
            AudioManager.PlayAt("forceField_000",transform,origin,.26f,AudioBus.SFX,50,.05f,.5f);
            yield return Windup(CutterTune.TractorTell,0,0,.03f);
            if(Interrupted)yield break;
            line.Release();strikeAt=0;
            // The first ship along the line with nothing solid between: caught, and struck.
            CombatActor caught=null;float nearest=float.PositiveInfinity;
            foreach(var member in party.members)
            {
                if(!member.Health.Alive||!line.Contains(member.transform.position)||!CombatCover.Clear(origin,member.transform.position,.3f))continue;
                float along=Vector3.Dot(Flat(member.transform.position-origin),direction);if(along<nearest){nearest=along;caught=member;}
            }
            if(caught!=null&&caught.Health.Receive(Packet(CutterTune.TractorDamage,DamageType.Pulse))<=0)caught=null;
            // Pulling, the pod is open: the moment to strike it, from wherever the line has you.
            CutterVents(CutterTune.Tractor,CutterTune.ReelSeconds+2);cutterOpenWhy="THE TRACTOR POD IS PULLING";
            if(caught==null){Debug.Log("CUTTER_TRACTOR_MISSED");Finish(CutterRest);yield break;}
            Debug.Log("CUTTER_TRACTOR_CAUGHT "+caught.character);caught.Stagger(.2f);
            var go=new GameObject("Tractor line");temporary.Add(go);var tether=go.AddComponent<LineRenderer>();tether.positionCount=2;tether.useWorldSpace=true;tether.startWidth=.35f;tether.endWidth=.5f;
            tether.sharedMaterial=ActorFactory.Energy(TractorTone);tether.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var body=caught.GetComponent<CharacterController>();string broke=null;
            for(float t=0;t<CutterTune.ReelSeconds;)
            {
                if(GameTime.Paused){yield return null;continue;}
                float dt=Time.deltaTime;t+=dt;if(Interrupted)yield break;
                var from=CutterPodAt(CutterTune.Tractor);from.y=transform.position.y;var gap=Flat(from-caught.transform.position);float distance=gap.magnitude;
                if(!caught.Health.Alive){broke="down";break;}
                if(caught.State==ActorState.Dodge){broke="rolled";break;}
                if(distance>CutterTune.ReelBreaks){broke="far";break;}
                if(!CombatCover.Clear(from,caught.transform.position,.3f)){broke="cover";break;}
                if(distance<=CutterTune.ReelStops)break;
                caught.Health.Hinder(CutterTune.ReelPace);
                if(body!=null&&body.enabled)body.Move(gap/distance*Mathf.Min(distance-CutterTune.ReelStops,CutterTune.ReelSpeed*dt));
                tether.SetPosition(0,from);tether.SetPosition(1,caught.transform.position);Pose(-3,.08f,.02f);
                yield return null;
            }
            temporary.Remove(go);Destroy(go);Pose(0);
            if(broke!=null){Debug.Log("CUTTER_TRACTOR_BROKEN "+broke);Finish(CutterRest);yield break;}
            // Closing in (no beam), what it reels in it crushes at its nose: a marked circle, a beat to roll.
            if(CutterMode=="close"||CutterMode=="tractor")
            {
                var toward=Flat(caught.transform.position-transform.position);toward=toward.sqrMagnitude>.01f?toward.normalized:transform.forward;transform.rotation=Quaternion.LookRotation(toward);
                var at=transform.position+toward*(cutterRadius+1.4f);
                var crush=Mark(Telegraph.Circle(at,CutterTune.CrushRadius,CutterTune.CrushTell,Telegraph.Heavy));strikeAt=crush.Contains(party.Active.transform.position)?Time.time+CutterTune.CrushTell:0;
                yield return Windup(CutterTune.CrushTell,-8,.1f,.04f);
                if(Interrupted)yield break;
                Strike(crush,CutterTune.CrushDamage,DamageType.Kinetic,.35f,2.4f);Tremor(.3f,.08f);strikeAt=0;Pose(0);
                AudioManager.PlayAt("impactMetal_000",transform,at,.3f,AudioBus.SFX,50,.05f,.7f);
            }
            Finish(CutterRest);
        }
        IEnumerator CutterRun()
        {
            var lead=PartyController.Current.Active.transform.position;var direction=Toward(lead);transform.rotation=Quaternion.LookRotation(direction);
            float length=Mathf.Clamp(Flat(lead-transform.position).magnitude+CutterTune.RunPast,CutterTune.RunShortest,CutterTune.RunLongest);
            if(CombatCover.Sweep(transform.position,transform.position+direction*(length+cutterRadius),.6f,out var wall))length=Mathf.Max(4,wall.distance-cutterRadius-.5f);
            float tell=Phase>=2?CutterTune.RunTellLater:CutterTune.RunTell;
            var lane=Mark(Telegraph.Line(transform.position,direction,length,CutterTune.RunWidth,tell,Telegraph.Threat));
            // The blow lands as its body comes within RunContact of a ship, not as its middle reaches him: timed to its middle, a roll at the tell's end went a frame after the hit.
            float ahead=Vector3.Dot(Flat(lead-transform.position),direction);strikeAt=lane.Contains(lead)&&ahead>0?Time.time+tell+Mathf.Max(0,ahead-CutterTune.RunContact)/CutterTune.RunSpeed:0;
            AudioManager.PlayAt("spaceEngineLow_000",transform,transform.position,.28f,AudioBus.SFX,60,.05f,.8f);
            yield return Windup(tell,-7,0,.04f);
            if(Interrupted)yield break;
            lane.Steady=true;Debug.Log("CUTTER_RUN");
            AudioManager.PlayAt("thrusterFire_000",transform,transform.position,.28f,AudioBus.SFX,60,.05f,.75f);
            // A run goes through the party (it strikes what it touches): its hull lets them be until it has passed.
            var controller=GetComponent<CharacterController>();bool walked=controller.enabled;controller.enabled=false;Collide(false);if(cutterHull!=null){cutterHull.enabled=false;cutterRunning=true;}
            var from=transform.position;var hit=new HashSet<Health>();
            for(float travelled=0;travelled<length&&!Interrupted;)
            {
                if(GameTime.Paused){yield return null;continue;}
                float step=Mathf.Min(CutterTune.RunSpeed*Time.deltaTime,length-travelled);travelled+=step;transform.position=from+direction*travelled;Pose(-5,.1f);
                Contact(hit,transform.position,CutterTune.RunContact,CutterTune.RunDamage,.3f,lane);
                var now=PartyController.Current.Active;float before=Vector3.Dot(Flat(now.transform.position-transform.position),direction);
                strikeAt=!hit.Contains(now.Health)&&lane.Contains(now.transform.position)&&before>0?Time.time+Mathf.Max(0,before-CutterTune.RunContact)/CutterTune.RunSpeed:0;
                if(Time.frameCount%3==0)CombatVfx.Burst(transform.position-direction*3,CutterBeamTone,"steam");
                yield return null;
            }
            controller.enabled=walked;Physics.SyncTransforms();Collide(true);if(cutterHull!=null){cutterHull.enabled=true;cutterRunning=false;}strikeAt=0;lane.Release();
            if(Interrupted)yield break;
            // It banks wide round the nearest district to come about: open (the party's line names the turn).
            var round=RingDistrict.Nearest(transform.position,40);cutterOpenWhy="IT IS TURNING WIDE";
            Debug.Log("CUTTER_TURNS_WIDE "+(round!=null?round.id:"open"));
            BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);
            OpenFor(CutterTune.TurnsWide);var bank=Vector3.Cross(Vector3.up,direction);
            for(float t=0;t<CutterTune.TurnsWide;)
            {
                if(GameTime.Paused){yield return null;continue;}
                float dt=Time.deltaTime;t+=dt;float k=Mathf.Clamp01(t/CutterTune.TurnsWide);
                if(controller.enabled)controller.Move((direction*(1-k)+bank*k)*CutterTune.Cruise*.6f*dt);
                transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(Vector3.Slerp(direction,bank,k)),70*dt);Pose(4,.1f);
                yield return null;
            }
            openingUntil=0;Pose(0);Finish(CutterRest);
        }
    }
}
