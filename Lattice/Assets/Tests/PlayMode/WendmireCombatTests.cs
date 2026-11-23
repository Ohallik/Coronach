using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 4's fights, each on a bare floor in the proving yards with what it needs made here (nothing waits on the Drowned Waystation being built). The hit sizes against the
    /// party the chapter is met by; one test for each family the chapter added or changed, saying what a player sees: the Bogstrider's front, back and flank and its stuck foot, the Spore Pod
    /// set off and its blinding cloud, the Lurelight's lamp and spring, the Rot-pod's shell, the Veil Ray's circuit, pass and tail, the Spore Swarm and a ship flown dark. Then the Rotbloom:
    /// its stalks (shut before the fight, a mouth gaping for each, the second draining the hall), its lashes that need water, its drag and its gaping miss, its quiet end, and its length.
    /// The chapter's expectations for the shared enemy and boss tests are here too.</summary>
    public sealed class WendmireCombatTests
    {
        const float X=230;
        readonly List<string> log=new();readonly List<(string key,Transform emitter)> cues=new();
        static readonly string[] Heeded={"STOMPER_","BURSTER_","AMBUSH_","LOBBER_","SWARM_","ROTBLOOM_","BOSS_","STALK_","COMBAT_KILL","SHIP_LIT"};
        void Heard(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        void Cue(string key,Transform emitter)=>cues.Add((key,emitter));
        FakeWater water;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter4");log.Clear();cues.Clear();Application.logMessageReceived+=Heard;AudioManager.CueStarted+=Cue;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;AudioManager.CueStarted-=Cue;
            if(water!=null){ArenaWater.Unregister(water);water=null;}
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Wendmire(ContentRegistry.Book book)
        {
            // Where a hero stands for each ordinary enemy's tell: inside the reach its attack starts from (before the Bogstrider, inside the Lurelight's ring, inside the pod's wake).
            book.Tell("Lurelight",2.4f);book.Tell("Bogstrider",3);book.Tell("MireLeeches",2);book.Tell("RotPod",7);book.Tell("DrownedSentry",2);
            book.Tell("SporeSwarm",5,flight:true);book.Tell("SporePod",3,flight:true);book.Tell("VeilRay",8,flight:true);
            // The Rotbloom: met at the plan's Sync 14 (the chapter runs 13 to 15). Its door is the great hall's (docs/maps/DrownedWaystation.md, "Boss arena contract").
            // Its share is measured by RotbloomDriverTests (November 14): the replay driver fighting it as a person does, up a ramp, a stalk, the gaping mouth, a second stalk,
            // against the party's flat-out rate measured in the same run. With stalks at a quarter of its integrity: 74,000 lasted 161.6 s at 986 a second (.465); 68,000 lasted
            // 100.5 s at 921 (.73) and 150.1 s at 937 (.48), the lower where two fights differ. The chain's .57 (nov13b, 54,000 in 91.3 s) was a driver that ground its shut
            // mouths and never found the stalks' ramps; .55 was that with room.
            // driverShare .39, not .48 (D355, continued): the flat-out measure stood an edge .9 m off its capsule, where his blade didn't reach its broad body, and read 932 a second; from a
            // quarter metre off it (BossReach.Edge) it reads 1,160, so the chain's own share of flat out is .48 x 932 / 1,160. The fight and its predicted length are unchanged.
            // .43 since D362 (November 22): at 63,500 today's scout of the 10:30 build felled it in 51.2 s (nov22-recon-e s26), 1.03 of the 1,160 and 1,246 a second read that morning
            // (d355-dome3, d355-lengths), and at 83,000 the 14:52 build's in 162.2 s (nov22-recon-h-26), which is .425: the lower, as the rule is, and RotbloomDriverTests' .46 at
            // 83,000 (d362-ch3to7) agrees with it. In both the hall drained on one stalk: an EXTRA on the gaping mouth took the bar past half before a second was burned.
            book.Boss(new ContentRegistry.BossSheet{id="Rotbloom",zone="DrownedWaystation",door="Rotbloom",doorFlags=new[]{"wend.pimFound","wend.pump3","wend.bloomSeen"},sync=14,driverShare=.43f,
                specials=new[]{("SporeBreath",new Vector3(0,0,6)),("RootLash",new Vector3(0,0,8)),("LureFlash",new Vector3(0,0,8)),("Drag",new Vector3(0,0,9)),("CrackLash",new Vector3(0,0,8)),("Thrash",new Vector3(0,0,6))},
                // Its drag closes on nothing at a hero beyond its pull (the cone reaches 18.4 m from its middle), and its third crack lash lodges in the floor.
                windows=new[]{("Drag",new Vector3(0,0,19.5f)),("CrackLash",new Vector3(0,0,8))}});
        }

        // ------------------------------------------------------------------ fixtures
        static bool Set(string key)=>GameServices.Current.Flags.GetBool(key);
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.5f);
        }
        static Vector3 At(float x,float y,float z)=>new(X+x,y,z);
        static Vector3 Sky(float x,float z)=>new(200+x,1,z);
        static void Floor()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Wendmire fixture floor";floor.transform.position=new Vector3(X,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static (CombatActor lead,CombatActor partner) Party(){var lead=PartyController.Current.Active;return (lead,PartyController.Current.members.First(m=>m!=lead));}
        static void Put(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            actor.Health.maximum=actor.Health.integrity=5000;actor.Health.InvulnerableUntil=0;Physics.SyncTransforms();
        }
        static void Stand(EnemyBrain enemy,Vector3 position,float yaw=0)
        {var body=enemy.GetComponent<CharacterController>();bool was=body.enabled;body.enabled=false;enemy.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));body.enabled=was;Physics.SyncTransforms();}
        static EnemyBrain Enemy(string id,Vector3 at,bool passive=true){var definition=GameCatalog.Find<EnemyDef>(id);Assert.IsNotNull(definition,"fixture: "+id+" is defined (WendmireBuilder.Definitions)");var enemy=ActorFactory.Enemy(definition,at);enemy.Passive=passive;return enemy;}
        static DamageType Neutral(Health body)=>System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=body.weakness&&t!=body.resistance);
        static float Hit(Health body,CombatActor from,float amount=100,string tag=null,float breakPower=0)=>body.Receive(new DamagePacket{source=from.Health,amount=amount,type=Neutral(body),tag=tag,breakPower=breakPower});
        static float Kill(Health body,CombatActor from)=>body.Receive(new DamagePacket{source=from.Health,amount=body.maximum*20,type=Neutral(body)});
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>Waits on the fight's own clock, driving the heroes' motors as their brains would: a roll is only carried out by a motor that is being driven.</summary>
        static IEnumerator Until(System.Func<bool> done,float seconds,params CombatActor[] held)
        {
            for(float end=Time.unscaledTime+seconds*1.5f+1,until=Time.time+seconds;!done()&&Time.time<until&&Time.unscaledTime<end;)
            {foreach(var actor in held)if(actor!=null&&actor.Health.Alive)actor.motor.Move(Vector2.zero,false,false);yield return null;}
        }
        static IEnumerator Wait(float seconds,params CombatActor[] held)=>Until(()=>false,seconds,held);
        static bool Faint(Telegraph marking)=>marking.GetComponent<MeshRenderer>().sharedMaterials[1].name.Contains("faint");
        static bool Drawn(Telegraph marking)=>marking!=null&&marking.GetComponent<MeshRenderer>().enabled;
        bool Said(string start)=>log.Any(l=>l.StartsWith(start));

        /// <summary>The great hall's water as the Rotbloom sees it, with nothing of the world behind it: what it was told, and a dry place (a plinth) if the test wants one.</summary>
        sealed class FakeWater:IArenaWater
        {
            public float level=1;public readonly List<(float fraction,float seconds)> told=new();public System.Func<Vector3,bool> dry=_=>false;
            public float Level=>level;
            public void SetLevel(float fraction,float seconds){told.Add((fraction,seconds));level=fraction;}
            public bool Submerged(Vector3 point)=>level>0&&!dry(point);
        }
        FakeWater Flood(){water=new FakeWater();ArenaWater.Register(water);return water;}
        /// <summary>A lure-stalk as the arena contract asks for one: a Health with the target card's name, a trigger and a hurtbox, answering to the Rotbloom.</summary>
        static LureStalk Stalk(string name,int index,Vector3 at)
        {
            var go=new GameObject(name);go.SetActive(false);go.transform.position=at;
            var health=go.AddComponent<Health>();health.id="Lure-stalk";health.maximum=health.integrity=400;health.breakThreshold=100000;
            var shape=go.AddComponent<SphereCollider>();shape.radius=1.3f;shape.center=Vector3.up*2.2f;shape.isTrigger=true;go.AddComponent<Hurtbox>().owner=health;
            var stalk=go.AddComponent<LureStalk>();stalk.index=index;stalk.settledBy="Rotbloom";stalk.defeatedFlag="test.stalk."+name;
            go.SetActive(true);return stalk;
        }
        /// <summary>Four stalks at the contract's bearings, 15 m out: NE, NW, SW, SE.</summary>
        static LureStalk[] Stalks()=>new[]{Stalk("ne",0,At(10.6f,0,10.6f)),Stalk("nw",1,At(-10.6f,0,10.6f)),Stalk("sw",2,At(-10.6f,0,-10.6f)),Stalk("se",3,At(10.6f,0,-10.6f))};

        // ------------------------------------------------------------------ the numbers the chapter is tuned by
        /// <summary>Every ordinary attack of the chapter, against the party it is met by: the chapter 4 loadout (Prime parts) at Sync 13, and the same party at 15, struck through
        /// Health.Receive as a real blow is, Plating and all. The plan asks a ninth to an eighth of the bar a hit (CHAPTER_GUIDE section 5 asks about 10%); every chapter 4 zone is at
        /// threat 1, so the authored damage is the damage. And no guardian's arc reaches 180 degrees: the shared weakness test strikes from the flank.</summary>
        [UnityTest] public IEnumerator EveryOrdinaryHitTakesANinthToAnEighthOfAHerosBarAcrossTheChapter()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();
            var ids=new[]{"SporeSwarm","SporePod","VeilRay","Lurelight","Bogstrider","MireLeeches","RotPod","DrownedSentry"};
            var source=Enemy("MireLeeches",At(30,.05f,30));yield return Frames(2);
            var failures=new List<string>();var shares=new List<string>();
            foreach(int sync in new[]{13,15})
            {
                foreach(var member in GameServices.Current.State.party)member.level=sync;PartyController.Current.RefreshStats();yield return null;
                float bar=hero.Health.maximum;Assert.AreEqual(Levels.MaxIntegrity(sync),bar,.5f,"fixture: the hero's bar at Sync "+sync);
                foreach(string id in ids)
                {
                    var def=GameCatalog.Find<EnemyDef>(id);Assert.IsNotNull(def,"fixture: "+id+" is defined");
                    foreach(var attack in def.attacks)
                    {
                        hero.Health.integrity=bar;hero.Health.InvulnerableUntil=0;
                        float dealt=hero.Health.Receive(new DamagePacket{source=source.Health,amount=attack.damage,type=attack.type,weight=HitWeight.Medium});
                        float share=dealt/bar;shares.Add($"{attack.name} sync{sync} {share:P1}");
                        if(share<(sync==13?.09f:.085f)||share>.13f)failures.Add($"{id}'s {attack.name} ({attack.damage:0}) takes {share:P1} of a Sync {sync} hero's bar: the plan asks 9 to 13%");
                    }
                    if(def.behaviour==EnemyBehaviourKind.Guardian&&def.tune.arc>=180)failures.Add($"{id}'s guarded arc is {def.tune.arc}: a flank blow must land whole");
                }
            }
            TestContext.WriteLine("WENDMIRE_SHARES "+string.Join("; ",shares));
            foreach(string zone in new[]{"SporeVeil","Driftreed","DrownedWaystation"})
            {var z=GameCatalog.Find<ZoneDef>(zone);if(z!=null&&!Mathf.Approximately(z.enemyThreat,1))failures.Add($"{zone} is at threat {z.enemyThreat}: chapter 4's damage is authored at its real weight, at 1");}
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        /// <summary>A boss hits harder than the things on the road to it: each of the Rotbloom's moves, and its plain snap, takes 12 to 16% of a Sync 14 bar in the Prime parts the
        /// chapter's party has, in whichever phase it is used (the second adds <see cref="BossController.PhaseStep"/>), and more than the heaviest ordinary hit in the mire and the
        /// station (the sentry's spear). The first numbers broke it both ways (a lash at 11.2%, the snap at 18.4% grounded): the old snap, 84, is shown to overshoot.</summary>
        [UnityTest] public IEnumerator EachOfTheRotbloomsMovesHitsHarderThanTheRoadToIt()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            foreach(var member in GameServices.Current.State.party)member.level=14;PartyController.Current.RefreshStats();yield return null;
            var source=Enemy("Rotbloom",At(40,.1f,40)).Health;yield return Frames(2);
            float Share(float amount){hero.Health.integrity=hero.Health.maximum;hero.Health.InvulnerableUntil=0;return hero.Health.Receive(new DamagePacket{source=source,amount=amount,type=DamageType.Kinetic,weight=HitWeight.Heavy})/hero.Health.maximum;}
            Assert.Greater(Share(84*(1+BossController.PhaseStep)),.16f,"fixture: the old snap, 84, should overshoot grounded");
            float threat=GameCatalog.Find<ZoneDef>("DrownedWaystation").enemyThreat,heaviest=0;string worst="";
            foreach(string id in new[]{"LureSnap","BogStomp","BogKick","LeechDart","LeechBurst","RotLob","SentryBash","SentrySpear"}){float s=Share(GameCatalog.Find<AttackDef>(id).damage*threat);if(s>heaviest){heaviest=s;worst=id;}}
            var plain=GameCatalog.Find<EnemyDef>("Rotbloom").attacks[0];
            var moves=new (string move,float damage,int[] phases)[]
            {
                ("spore breath",BossController.RotbloomTune.BreathDamage,new[]{1,2}),("root lash",BossController.RotbloomTune.LashDamage,new[]{1}),("lure flash",BossController.RotbloomTune.FlashDamage,new[]{1}),
                ("drag's snap",BossController.RotbloomTune.SnapDamage,new[]{1,2}),("crack lash",BossController.RotbloomTune.CrackDamage,new[]{2}),("thrash",BossController.RotbloomTune.ThrashDamage,new[]{2}),
                (plain.name,plain.damage,new[]{1,2}),
            };
            var failures=new List<string>();var shares=new List<string>{$"road {worst} {heaviest:P1}"};
            foreach(var (move,damage,phases) in moves)foreach(int phase in phases)
            {
                float s=Share(damage*(1+(phase-1)*BossController.PhaseStep));shares.Add($"{move} p{phase} {s:P1}");
                if(s<.12f||s>.16f)failures.Add($"the Rotbloom's {move} in phase {phase} takes {s:P1} of a Sync 14 bar: a boss's move takes 12 to 16%");
                if(s<=heaviest)failures.Add($"the Rotbloom's {move} in phase {phase} takes {s:P1}, no more than {worst} on the road to it ({heaviest:P1})");
            }
            TestContext.WriteLine("WENDMIRE_BOSS_SHARES "+string.Join("; ",shares));
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ the behaviour families
        [UnityTest] public IEnumerator ABogstriderStompsBeforeItKicksBehindItLeavesItsFlankAloneAndSticksAMissedStomp()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,3));
            var strider=Enemy("Bogstrider",At(0,.05f,0));yield return Frames(3);
            Assert.IsInstanceOf<EnemyStomper>(strider.Behaviour,"a Bogstrider isn't a stomper");
            // Before it: a stomp on a circle a stride ahead.
            strider.Passive=false;
            yield return Until(()=>strider.Telegraphing,4,hero);Assert.IsTrue(strider.Telegraphing,"fixture: it stomps at a hero three metres before it");
            var stomp=strider.AttackMark;Assert.AreEqual(Telegraph.Kind.Circle,stomp.Shape,"its stomp isn't marked as a circle");Assert.IsTrue(stomp.Contains(hero.transform.position),"the stomp isn't marked where he stands");
            Assert.Greater(Vector3.Dot(stomp.Origin-strider.transform.position,strider.transform.forward),1,"the stomp isn't before it");
            yield return Until(()=>!strider.Telegraphing,3,hero);yield return null;
            Assert.Less(hero.Health.integrity,5000,"the stomp missed a hero standing in its circle");Assert.IsFalse(Said("STOMPER_STUCK"),"a stomp that landed stuck its foot");
            // Behind it: a kick across a fan behind it.
            strider.Passive=true;yield return Wait(.3f,hero);Stand(strider,At(0,.05f,0));Put(hero,At(0,.05f,-3));yield return Wait(2.6f,hero);strider.Passive=false;
            yield return Until(()=>strider.Telegraphing,4,hero);Assert.IsTrue(strider.Telegraphing,"it never kicks at a hero standing behind it");
            var kick=strider.AttackMark;Assert.AreEqual(Telegraph.Kind.Cone,kick.Shape,"its kick isn't marked as a fan");Assert.IsTrue(kick.Contains(hero.transform.position),"the kick isn't marked where the hero stands");
            Assert.Less(Vector3.Dot(kick.Forward,strider.transform.forward),-.9f,"the kick's fan isn't behind it");
            yield return Until(()=>!strider.Telegraphing,2,hero);yield return null;Assert.Less(hero.Health.integrity,5000,"the kick missed a hero standing behind it");
            // At its flank: nothing, and it comes round slowly.
            strider.Passive=true;yield return Wait(.3f,hero);Stand(strider,At(0,.05f,0));Put(hero,At(3,.05f,0));yield return Wait(2.2f,hero);log.Clear();
            var facing=strider.transform.forward;strider.Passive=false;yield return Wait(.7f,hero);
            Assert.IsFalse(strider.Telegraphing,"it struck at a hero at its flank: neither its stomp nor its kick reaches there");
            float turned=Vector3.Angle(facing,strider.transform.forward);Assert.That(turned,Is.InRange(10,45),$"in .7 s it turned {turned:0} degrees toward a hero at its flank: it is slow to come round (55 a second)");
            // A stomp drawn and stepped out of: its foot sticks, open.
            strider.Passive=true;yield return Wait(.3f,hero);Stand(strider,At(0,.05f,0));Put(hero,At(0,.05f,3));yield return Wait(2.6f,hero);log.Clear();strider.Passive=false;
            yield return Until(()=>strider.Telegraphing,4,hero);Assert.IsTrue(strider.Telegraphing,"fixture: it stomps again");
            Put(hero,At(5,.05f,3));
            yield return Until(()=>Said("STOMPER_STUCK Bogstrider"),3,hero);Assert.IsTrue(Said("STOMPER_STUCK Bogstrider"),"a stomp that struck nobody left it free");
            Assert.AreEqual(5000,hero.Health.integrity,.01f,"a hero who stepped out of the circle was struck");
            Assert.IsTrue(strider.Health.Broken,"stuck, it isn't open");Assert.That(strider.Health.BrokenUntil-Time.time,Is.InRange(1.4f,1.85f),"stuck, it is open for 1.8 s");
            Assert.AreEqual(150,Hit(strider.Health,hero),1,"a hit on a stuck Bogstrider isn't an opening's half as much again");
        }
        [UnityTest] public IEnumerator ASporePodSetOffSwellsAndBurstsIntoACloudThatHidesTheMarkingsUnderItAndShotDeadStrikesNobody()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-60,-60));Put(hero,Sky(0,12));
            var pod=Enemy("SporePod",Sky(0,0),false);yield return Frames(3);
            Assert.IsInstanceOf<EnemyBurster>(pod.Behaviour,"a Spore Pod isn't a burster");Assert.IsFalse(pod.CanBeMoved,"a pod can be dragged");
            yield return Wait(1,hero);Assert.IsFalse(pod.Telegraphing,"a pod went off with no ship near it");
            var drifted=pod.transform.position;Assert.That(Flat(drifted,Sky(0,0)),Is.InRange(.1f,2),"it doesn't drift on its loop");
            var party=GameServices.Current.State.party;int xp=party[0].xp;
            // Flown close: it swells on a marked circle and bursts.
            Put(hero,new Vector3(drifted.x,1,drifted.z+2.5f));
            yield return Until(()=>pod.Telegraphing,2,hero);Assert.IsTrue(pod.Telegraphing,"a ship three metres off didn't set it off");
            var blast=pod.AttackMark;Assert.AreEqual(Telegraph.Kind.Circle,blast.Shape);Assert.IsTrue(blast.Contains(hero.transform.position),"its burst isn't marked where the ship is");
            yield return Until(()=>pod==null||!pod.Health.Alive,2,hero);
            Assert.IsTrue(Said("BURSTER_BURST SporePod"),"it never burst");Assert.Less(hero.Health.integrity,5000,"its burst missed a ship inside its circle");
            Assert.AreEqual(xp,party[0].xp,"a pod that went off by itself paid the party for it");
            var cloud=LingeringArea.All.FirstOrDefault(a=>a!=null&&a.Effect==AreaEffect.Blind);Assert.IsNotNull(cloud,"it burst and left no cloud");
            var ring=Telegraph.Live.FirstOrDefault(t=>t!=null&&ReferenceEquals(t.owner,cloud));Assert.IsNotNull(ring);Assert.IsTrue(Faint(ring),"a blinding cloud is painted as a danger: nothing strikes there");
            // What is laid inside it is drawn as nothing, and is still what it is; what is laid outside it is drawn.
            var inside=Telegraph.Circle(cloud.transform.position+Vector3.right,1,6,Telegraph.Threat);var outside=Telegraph.Circle(cloud.transform.position+Vector3.right*12,1,6,Telegraph.Threat);
            yield return Frames(2);
            Assert.IsFalse(Drawn(inside),"a marking laid inside the spore cloud is drawn");Assert.IsTrue(Drawn(outside),"a marking outside the spore cloud is hidden");
            Assert.IsTrue(inside.Contains(cloud.transform.position+Vector3.right),"a hidden marking stopped being the shape it is");
            yield return Until(()=>LingeringArea.All.Count==0,6,hero);Assert.IsEmpty(LingeringArea.All,"the cloud never clears: it should stand 3.5 s");
            yield return null;Assert.IsTrue(Drawn(inside),"the cloud went and the marking under it is still hidden");
            Object.Destroy(inside.gameObject);Object.Destroy(outside.gameObject);
            // Shot dead from range: its cloud where it hung, and nobody struck.
            var far=Enemy("SporePod",Sky(20,0),false);yield return Frames(3);Put(hero,Sky(20,14));float before=hero.Health.integrity;log.Clear();
            Kill(far.Health,hero);yield return Frames(2);
            Assert.IsTrue(Said("BURSTER_BURST SporePod"),"shot dead it didn't burst");Assert.AreEqual(xp+GameCatalog.Find<EnemyDef>("SporePod").xp,party[0].xp,"a pod shot dead paid nobody");
            Assert.IsTrue(LingeringArea.All.Any(a=>a!=null&&a.Effect==AreaEffect.Blind&&Flat(a.transform.position,Sky(20,0))<.5f),"shot dead it left no cloud where it hung");
            yield return Wait(1,hero);Assert.AreEqual(before,hero.Health.integrity,.01f,"a pod shot dead from range struck the ship");
        }
        /// <summary>The Spore Pod's own body has its origin at the tip of its stalk, below and maybe beside the pod: set down as it comes, the pod a player sees would hang off the
        /// circle that marks its burst and off the capsule that is hit. The bare model is measured first (what it does on its own), then the pod as it fights: drawn round its capsule,
        /// over its circle.</summary>
        [UnityTest] public IEnumerator ASporePodIsDrawnWhereItsCapsuleAndItsCircleAre()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-60,-60));Put(hero,Sky(0,12));
            var def=GameCatalog.Find<EnemyDef>("SporePod");Assert.IsNotNull(def.prefab,"fixture: the Spore Pod has a body");
            Assert.AreEqual(1,def.drawnScale,.001f,"its own body is drawn at its own size");
            Assert.GreaterOrEqual(def.capsuleHeight,def.capsuleRadius*2-.001f,"its capsule is shorter than it is wide");
            var bare=Object.Instantiate(def.prefab,Sky(30,30),Quaternion.identity);yield return null;
            var parts=bare.GetComponentsInChildren<Renderer>();Assert.IsNotEmpty(parts,"fixture: the body draws something");
            var raw=parts[0].bounds;foreach(var part in parts)raw.Encapsulate(part.bounds);
            TestContext.WriteLine($"SPORE_POD_BARE origin={bare.transform.position} middle={raw.center} size={raw.size} lowest={raw.min.y:0.00}");
            Object.Destroy(bare);
            var pod=Enemy("SporePod",Sky(0,0));yield return Frames(3);
            var burster=(EnemyBurster)pod.Behaviour;var capsule=pod.GetComponent<CharacterController>();var middle=burster.DrawnMiddle;var want=pod.transform.position+capsule.center;
            TestContext.WriteLine($"SPORE_POD_DRAWN root={pod.transform.position} capsule={want} middle={middle}");
            Assert.Less(Flat(middle,pod.transform.position),.3f,"the pod is drawn off the middle of its circle and its capsule");
            Assert.AreEqual(want.y,middle.y,.3f,"the pod is drawn above or below its capsule");
            // Its burst's circle is laid round its middle, where it is drawn.
            pod.Passive=false;Put(hero,new Vector3(pod.transform.position.x,1,pod.transform.position.z+2.5f));
            yield return Until(()=>pod.Telegraphing,2,hero);Assert.IsTrue(pod.Telegraphing,"fixture: it is set off");
            Assert.Less(Flat(pod.AttackMark.Origin,middle),.3f,"its burst is marked off where it is drawn");
        }
        [UnityTest] public IEnumerator ALurelightStandsAsALampFlickeringShortsAndSpringsAtWhoeverStepsInsideItsRing()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-22));
            var lure=Enemy("Lurelight",At(0,.05f,0),false);yield return Frames(3);
            var ambusher=lure.Behaviour as EnemyAmbusher;Assert.IsNotNull(ambusher,"a Lurelight isn't an ambusher");
            yield return Until(()=>ambusher.Hidden,4,hero);Assert.IsTrue(ambusher.Hidden,"with nobody near, it never took its post");
            // On show as a lamp: drawn, where it stands, solid, and out of reach.
            var drawn=lure.GetComponentsInChildren<Renderer>().Where(r=>r is MeshRenderer||r is SkinnedMeshRenderer).ToArray();
            Assert.IsNotEmpty(drawn,"fixture: a Lurelight has a body");Assert.IsTrue(drawn.All(r=>r.enabled),"a lure waiting as a lamp isn't drawn: it went under like a Reedstalker");
            var visual=lure.GetComponent<DefeatPresentation>().visual;Assert.Greater(visual.position.y,lure.transform.position.y-.2f,"a lure waiting as a lamp sank into the floor");
            Assert.IsFalse(lure.Health.Targetable,"waiting as a lamp, it can be targeted");Assert.AreEqual(0,Hit(lure.Health,hero),.01f,"waiting as a lamp, it can be hit");
            Assert.IsTrue(ambusher.Spot.Shown,"its post isn't ringed");
            // Its lamp: short flashes, and never a long one (Driftreed's lamps keep two short, one long).
            float lit=0,longest=0;int flashes=0;bool wasLit=false;
            for(float end=Time.time+3.4f;Time.time<end;)
            {
                bool now=ambusher.LampLevel>1;if(now){lit+=Time.deltaTime;longest=Mathf.Max(longest,lit);}else lit=0;
                if(now&&!wasLit)flashes++;wasLit=now;hero.motor.Move(Vector2.zero,false,false);yield return null;
            }
            Assert.GreaterOrEqual(flashes,5,$"its lamp flashed {flashes} times in 3.4 s: a lure flickers");Assert.Less(longest,.3f,$"its lamp held a flash {longest:0.00} s: a lure never holds a long");
            // A hero steps inside its ring: it springs, blazing, snaps twice and stands spent.
            int bitten=0;hero.Health.Damaged+=(_,packet,amount)=>{if(packet.source==lure.Health&&amount>0)bitten++;};float blaze=0;
            var first=ambusher.Spot;Put(hero,first.transform.position+new Vector3(0,.05f,2.4f));
            yield return Until(()=>{blaze=Mathf.Max(blaze,ambusher.LampLevel);return Said("AMBUSH_SPENT Lurelight");},6,hero);
            Assert.IsTrue(Said("AMBUSH_ERUPT Lurelight"),"a hero inside its ring and it stayed a lamp");Assert.IsTrue(Said("AMBUSH_SPENT Lurelight"),"it never finished its snaps");
            Assert.AreEqual(2,bitten,"two snaps at a hero who stood there should both land");Assert.Greater(blaze,1.8f,"it sprang without its lamp blazing");
            Assert.IsTrue(lure.Health.Targetable,"spent in the open, it can't be targeted");Assert.Greater(Hit(lure.Health,hero),0,"spent in the open, it can't be hit");
            // Then it hops to another post, and waits there as a lamp again.
            yield return Until(()=>ambusher.Hidden,7,hero);Assert.IsTrue(ambusher.Hidden,"it never took another post");
            Assert.AreNotEqual(first,ambusher.Spot,"it went back to the post it sprang from");Assert.IsTrue(drawn.All(r=>r!=null&&r.enabled),"at its new post it isn't drawn");
        }
        [UnityTest] public IEnumerator ARotPodShutsWhenStruckThrowsNothingShutAndIsSoftAsItOpens()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,8));
            var pod=Enemy("RotPod",At(0,.05f,0));yield return Frames(3);
            var lobber=pod.Behaviour as EnemyLobber;Assert.IsNotNull(lobber,"a Rot-pod isn't a lobber");
            var kinds=new List<HitKind>();System.Action<Health,float,HitKind> seen=(body,_,kind)=>{if(body==pod.Health)kinds.Add(kind);};Health.DamageNumber+=seen;
            try
            {
                // Held for a scene it takes a blow as it stands: the shell is an awake pod's.
                Assert.AreEqual(100,Hit(pod.Health,hero),1,"fixture: a blow on a pod");Assert.IsFalse(lobber.Shut,"a pod held for a scene shut");
                pod.Passive=false;
                Assert.AreEqual(100,Hit(pod.Health,hero),1,"the first blow on an open pod isn't whole");
                Assert.IsTrue(lobber.Shut,"struck, it didn't shut");Assert.IsTrue(Said("LOBBER_SHUT RotPod"));
                Assert.AreEqual(15,Hit(pod.Health,hero),1,"shut, a blow isn't cut to .15");Assert.AreEqual(HitKind.Resisted,kinds.Last(),"a blow its shell turned doesn't read as resisted");
            }
            finally{Health.DamageNumber-=seen;}
            // Shut, it throws nothing.
            yield return Wait(1.4f,hero);Assert.IsFalse(pod.Telegraphing,"shut, it lobbed");
            // It opens again, soft: half as much again, and a blow then doesn't shut it.
            yield return Until(()=>Said("LOBBER_OPENS RotPod"),2,hero);Assert.IsTrue(Said("LOBBER_OPENS RotPod"),"it never opened again");
            Assert.IsTrue(pod.Health.Broken,"opening, it isn't soft");Assert.That(pod.Health.BrokenUntil-Time.time,Is.InRange(.9f,1.25f),"it is soft for 1.2 s as it opens");
            Assert.AreEqual(150,Hit(pod.Health,hero),1,"a blow as it opens isn't an opening's half as much again");Assert.IsFalse(lobber.Shut,"a blow as it opened shut it again");
            // Open again, it lobs again.
            yield return Until(()=>pod.Telegraphing,5,hero);Assert.IsTrue(pod.Telegraphing,"open again, it never lobs");
            Assert.AreEqual(Telegraph.Kind.Circle,pod.AttackMark.Shape);Assert.IsTrue(pod.AttackMark.Contains(hero.transform.position),"its lob isn't marked where it will land");
        }
        [UnityTest] public IEnumerator AVeilRayGlidesItsCircuitPassesAShipThatComesCloseAndSweepsOneOnItsTail()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-90,-90));Put(hero,Sky(70,70));
            var ray=Enemy("VeilRay",Sky(0,0),false);yield return Frames(3);
            var strafer=ray.Behaviour as EnemyStrafer;Assert.IsNotNull(strafer,"a Veil Ray isn't a strafer");
            // Nobody near: it keeps its loop round where it was set, and comes round it.
            yield return Wait(4,hero);float out1=Flat(ray.transform.position,Sky(0,0));float bearing=strafer.Bearing;
            Assert.That(out1,Is.InRange(16,26),$"with nobody near it is {out1:0} m from where it was set: its circuit is 22 m round");
            yield return Wait(3,hero);Assert.That(Flat(ray.transform.position,Sky(0,0)),Is.InRange(16,26),"it left its circuit");
            Assert.Greater(Mathf.Abs(strafer.Bearing-bearing),.6f,"it doesn't come round its circuit");Assert.IsFalse(ray.Telegraphing,"it struck at a ship 70 m off");
            // A ship inside its run-in: a long lane through it.
            var ahead=ray.transform.position+ray.transform.forward*9;Put(hero,new Vector3(ahead.x,1,ahead.z));
            yield return Until(()=>ray.Telegraphing,5,hero);Assert.IsTrue(ray.Telegraphing,"a ship nine metres before it and it made no pass");
            var lane=ray.AttackMark;Assert.AreEqual(Telegraph.Kind.Line,lane.Shape,"its pass isn't marked as a lane");Assert.IsTrue(lane.Contains(hero.transform.position),"the lane isn't laid through the ship");
            Assert.AreEqual(24,lane.Length,.5f,"its pass is 24 m");Assert.AreEqual(4,lane.Width,.1f,"its lane is 4 m wide");
            yield return Until(()=>hero.Health.integrity<5000,3,hero);Assert.Less(hero.Health.integrity,5000,"its pass went through a ship standing in its lane and touched nothing");
            // A fresh ray, a ship close on its tail: the sweep.
            ray.Passive=true;Object.Destroy(ray.gameObject);yield return Frames(2);
            var second=Enemy("VeilRay",Sky(40,0));yield return Frames(3);Stand(second,Sky(40,0));Put(hero,Sky(40,-4.5f));yield return Wait(1,hero);second.Passive=false;
            yield return Until(()=>second.Telegraphing,3,hero);Assert.IsTrue(second.Telegraphing,"a ship close behind it and it never swept");
            var sweep=second.AttackMark;Assert.AreEqual(Telegraph.Kind.Cone,sweep.Shape,"its tail isn't marked as a fan");Assert.IsTrue(sweep.Contains(hero.transform.position));
            Assert.Less(Vector3.Dot(sweep.Forward,second.transform.forward),-.9f,"the tail's fan isn't behind it");
            float before=hero.Health.integrity;yield return Until(()=>hero.Health.integrity<before,2,hero);Assert.Less(hero.Health.integrity,before,"its tail missed a ship inside its fan");
        }
        [UnityTest] public IEnumerator ASporeSwarmLetsGoOfAShipFlownDarkForTwoSecondsAndLeavesADarkShipAloneUnlessItComesClose()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-60,-60));Put(hero,Sky(0,4));
            var glow=ShipGlow.Of(hero);Assert.IsNotNull(glow,"fixture: the Veil's rule");glow.Set(1);
            var swarm=Enemy("SporeSwarm",Sky(0,0),false);yield return Frames(3);
            var motes=swarm.GetComponentInChildren<SwarmMotes>();Assert.IsNotNull(motes,"a Spore Swarm isn't drawn as motes");
            Assert.AreEqual(GameCatalog.Find<EnemyDef>("SporeSwarm").tune.tint,motes.tone,"its motes aren't its own colour");
            // Kept lit, it rides the ship its whole latch, draining its Thrust.
            yield return Until(()=>{glow.Set(1);return Said("SWARM_LATCH SporeSwarm");},5,hero);Assert.IsTrue(Said("SWARM_LATCH SporeSwarm"),"fixture: the swarm latches onto a lit ship");
            hero.thrust=100;yield return Until(()=>{glow.Set(1);return Said("SWARM_SHED");},2.4f,hero);
            Assert.IsFalse(Said("SWARM_SHED"),"a ship kept lit lost its swarm before its latch ran out: "+string.Join(", ",log));Assert.Less(hero.thrust,70,"riding a ship it doesn't drain its Thrust");
            yield return Until(()=>Said("SWARM_SHED"),2,hero);
            // Caught again and flown dark: two seconds and it lets go, well before its latch would.
            swarm.Passive=true;yield return Wait(1.4f,hero);log.Clear();Put(hero,swarm.transform.position+new Vector3(0,0,3));glow.Set(1);swarm.Passive=false;
            yield return Until(()=>{if(!Said("SWARM_LATCH"))glow.Set(1);return Said("SWARM_LATCH SporeSwarm");},6,hero);Assert.IsTrue(Said("SWARM_LATCH SporeSwarm"),"fixture: it latches again");
            float latchedAt=Time.time;yield return Until(()=>Said("SWARM_SHED"),3.2f,hero);
            Assert.IsTrue(Said("SWARM_SHED SporeSwarm dark"),"a ship flown dark kept its swarm: "+string.Join(", ",log));
            Assert.That(Time.time-latchedAt,Is.InRange(1.8f,2.6f),"it let go of a ship flown dark after two seconds");
            // A dark ship keeping its distance is left alone, and one that comes close is found.
            swarm.Passive=true;yield return Wait(.3f,hero);Stand(swarm,Sky(0,0));
            Put(hero,Sky(0,13));yield return Wait(2.2f,hero);Assert.IsTrue(glow.Dark,"fixture: the ship has flown dark two seconds");
            swarm.Passive=false;yield return Wait(2,hero);
            Assert.IsFalse(swarm.Telegraphing,"a swarm went for a ship flown dark 13 m off");Assert.Less(Flat(swarm.transform.position,Sky(0,0)),3,"a swarm chased a ship flown dark 13 m off instead of keeping where it lives");
            Put(hero,swarm.transform.position+new Vector3(0,0,4));
            yield return Until(()=>swarm.Telegraphing,3,hero);Assert.IsTrue(swarm.Telegraphing,"a dark ship that flew into a swarm wasn't found");
        }

        // ------------------------------------------------------------------ the Rotbloom
        [UnityTest] public IEnumerator TheRotbloomsStalksAreShutUntilItWakesEachBurnedOpensAMouthAndTheSecondDrainsTheHall()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,12));
            var basin=Flood();var stalks=Stalks();yield return Frames(2);
            // Before the fight the stalks answer to nobody's blow.
            foreach(var stalk in stalks){Assert.IsFalse(stalk.Health.Targetable,"a lure-stalk can be targeted before its boss is awake");Assert.AreEqual(0,Kill(stalk.Health,hero),.01f,"a lure-stalk can be burned before the fight");}
            var bloom=Enemy("Rotbloom",At(0,.1f,0));yield return Frames(3);var boss=bloom.GetComponent<BossController>();var body=bloom.Health;float whole=body.maximum;
            foreach(var stalk in stalks)
            {
                Assert.IsTrue(stalk.Health.Targetable,"awake, its stalks still can't be hit");
                Assert.AreEqual(whole*BossController.RotbloomTune.StalkIntegrity,stalk.Health.maximum,1,"a stalk's integrity isn't the fight's to say");
                Assert.AreEqual(DamageType.Plasma,stalk.Health.weakness,"a stalk doesn't burn");Assert.AreEqual(DamageType.Beam,stalk.Health.resistance,"a stalk doesn't drink light");
            }
            Assert.IsTrue(boss.Fishing);StringAssert.Contains("BURN A LURE-STALK",boss.StatusText,"its bar doesn't say what to burn");
            Assert.AreEqual(100*BossController.RotbloomTune.Shell,Hit(body,hero),1,"with its mouths shut a blow on it isn't cut to a share");
            Assert.IsFalse(basin.told.Any(t=>t.fraction<1),"the water moved before any stalk burned");
            // The first stalk: a mouth gapes, and a quarter of its bar goes.
            float bar=body.integrity;log.Clear();Kill(stalks[0].Health,hero);yield return Frames(2);
            Assert.IsTrue(Said("ROTBLOOM_STALK_BURNED standing=3"),"a stalk burned and nothing heard it");Assert.IsTrue(Said("ROTBLOOM_GAPES stalk"),"a stalk burned and no mouth gaped");
            Assert.AreEqual(bar-whole*BossController.RotbloomTune.StalkBar,body.integrity,2,"a stalk should take a quarter of its bar");
            Assert.IsTrue(body.Broken,"gaping, it isn't open");Assert.That(body.BrokenUntil-Time.time,Is.InRange(4.5f,5.05f),"a mouth gapes for five seconds");
            Assert.AreEqual(150,Hit(body,hero),1,"a hit on a gaping mouth isn't an opening's half as much again");StringAssert.Contains("GAPES",boss.StatusText,"its bar doesn't say a mouth gapes");
            Assert.IsTrue(Set("test.stalk.ne"),"a burned stalk isn't kept in the save");Assert.IsFalse(basin.told.Any(t=>t.fraction<1),"one stalk drained the hall");
            yield return Until(()=>!body.Broken,6,hero);yield return Frames(2);
            // The second: over half; once its mouth has gaped, the last pump runs and the hall drains.
            bloom.Passive=false;log.Clear();Kill(stalks[1].Health,hero);yield return Frames(2);
            Assert.Less(body.integrity,whole*.5f,"the second stalk doesn't carry it over half");Assert.Greater(body.integrity,whole*.48f);Assert.IsTrue(body.Broken,"the second stalk's mouth didn't gape");
            yield return Until(()=>Said("BOSS_SHIFT Rotbloom 2"),12,hero);Assert.IsTrue(Said("BOSS_SHIFT Rotbloom 2"),"with two stalks burned it never shifted");
            Assert.IsTrue(Said("ROTBLOOM_DRAINS"),"its shift didn't drain the hall");
            Assert.AreEqual(0,basin.told.Last().fraction,.001f,"the water wasn't told to go");Assert.AreEqual(BossController.RotbloomTune.DrainSeconds,basin.told.Last().seconds,.01f);
            Assert.IsFalse(stalks[2].Standing||stalks[3].Standing,"the stalks still standing didn't go dark with the water");
            Assert.IsFalse(Set("test.stalk.sw")||Set("test.stalk.se"),"stalks that went dark with the water are counted as burned by the party");
            var seeds=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>e.definition!=null&&e.definition.id=="RotPod").ToArray();
            Assert.AreEqual(BossController.RotbloomTune.Seeds,seeds.Length,"its shift didn't seed the rim with rot-pods");
            foreach(var seed in seeds)Assert.That(Flat(seed.transform.position,At(0,0,0)),Is.InRange(9,13.5f),"a rot-pod wasn't seeded on the rim");
            Assert.IsFalse(boss.Fishing);Assert.AreEqual("",boss.StatusText,"drained, its bar still says its mouths are shut");
            bloom.Passive=true;foreach(var seed in seeds)seed.Passive=true;
            yield return Until(()=>!boss.Busy&&!boss.Shifting&&!body.Broken,9,hero);yield return Wait(.3f,hero);
            Assert.AreEqual(100,Hit(body,hero),1,"grounded, its mouths are still shut");
            var at=bloom.transform.position;bloom.Passive=false;Put(hero,bloom.transform.position+new Vector3(0,0,16));yield return Wait(2,hero);
            Assert.Less(Flat(at,bloom.transform.position),.3f,"grounded, it still moves");
        }
        [UnityTest] public IEnumerator ItsRootLashesStrikeOnlyWhoStandsInTheWater()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();
            var basin=Flood();var plinth=At(6,0,8);basin.dry=point=>Flat(point,plinth)<2.6f;
            var bloom=Enemy("Rotbloom",At(0,.1f,0));yield return Frames(3);var boss=bloom.GetComponent<BossController>();
            // The lead on the dry plinth, the partner in the water on the same line, nearer.
            Put(hero,At(6,.05f,8));Put(partner,At(3,.05f,4));yield return null;
            Assert.IsTrue(boss.Perform("RootLash"),"fixture: the lash");yield return null;
            var lines=Telegraph.Live.Where(t=>t!=null&&t.owner==boss&&t.Shape==Telegraph.Kind.Line&&!t.Released).ToArray();
            Assert.AreEqual(BossController.RotbloomTune.Lashes,lines.Length,"its lash doesn't mark three lines");
            Assert.IsTrue(lines.Any(l=>l.Contains(hero.transform.position)&&l.Contains(partner.transform.position)),"fixture: both stand on one line");
            yield return Frames(2);Assert.IsFalse(boss.Telegraphing,"a lead on dry ground is told a lash under the water is coming for him");
            yield return Until(()=>!boss.Busy,4,hero,partner);
            Assert.AreEqual(5000,hero.Health.integrity,.01f,"a root lash struck a hero standing out of the water");
            Assert.Less(partner.Health.integrity,5000,"a root lash passed under a hero standing in the water on its line");
        }
        [UnityTest] public IEnumerator ItsDragPullsTowardAMarkedMouthARollBreaksItsHoldAndASnapOnNothingLeavesItGaping()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,10));
            var bloom=Enemy("Rotbloom",At(0,.1f,0));yield return Frames(3);var boss=bloom.GetComponent<BossController>();var body=bloom.Health;
            Assert.IsTrue(boss.Perform("Drag"),"fixture: the drag");yield return null;
            var reach=Telegraph.Live.FirstOrDefault(t=>t!=null&&t.owner==boss&&t.Shape==Telegraph.Kind.Cone);var mouth=Telegraph.Live.FirstOrDefault(t=>t!=null&&t.owner==boss&&t.Shape==Telegraph.Kind.Circle);
            Assert.IsNotNull(reach,"its pull's reach isn't marked");Assert.IsNotNull(mouth,"its mouth isn't marked");
            Assert.IsTrue(Faint(reach),"the pull's reach is painted as a danger: it drags, it doesn't strike");Assert.IsFalse(Faint(mouth),"its mouth isn't painted as the danger it is");
            Assert.IsTrue(reach.Contains(hero.transform.position),"fixture: he stands in the pull");Assert.IsFalse(mouth.Contains(hero.transform.position),"fixture: he starts outside the mouth");
            yield return Until(()=>!boss.Busy,5,hero);
            Assert.Less(Flat(hero.transform.position,bloom.transform.position),9,"the drag didn't pull him toward its mouth");
            Assert.Less(hero.Health.integrity,5000,"the snap missed a hero it had pulled into its mouth");Assert.IsFalse(Said("ROTBLOOM_GAPES"),"a snap that took someone left it gaping");
            // Rolled out of the pull: free of it, and the mouth closes on nothing.
            yield return Wait(.5f,hero);Stand(bloom,At(0,.1f,0));Put(hero,At(0,.05f,10));yield return Frames(2);log.Clear();
            Assert.IsTrue(boss.Perform("Drag"),"fixture: the drag again");
            yield return Until(()=>boss.TelegraphRemaining<BossController.RotbloomTune.DragPull-.25f,3,hero);
            Assert.IsTrue(hero.Dodge(Vector3.right),"fixture: the roll");yield return Wait(.4f,hero);
            Assert.IsTrue(Said("ROTBLOOM_DRAG_BROKEN"),"a roll didn't break the drag's hold");
            var stood=hero.transform.position;yield return Wait(.5f,hero);Assert.Less(Flat(stood,hero.transform.position),.25f,"after the roll the drag still pulls him");
            yield return Until(()=>Said("ROTBLOOM_GAPES drag"),3,hero);Assert.IsTrue(Said("ROTBLOOM_GAPES drag"),"its mouth closed on nothing and it didn't gape");
            Assert.AreEqual(5000,hero.Health.integrity,.01f,"the snap took a hero who had rolled out of the pull");
            Assert.IsTrue(body.Broken,"gaping, it isn't open");Assert.That(body.BrokenUntil-Time.time,Is.InRange(1.4f,1.85f),"a missed snap gapes for 1.8 s");
            Assert.AreEqual(150,Hit(body,hero),1,"a hit on its gaping mouth isn't an opening's half as much again");
        }
        [UnityTest] public IEnumerator TheRotbloomDiesWhereItLiesWithNoWordOverTheQuietAndTheHallSettles()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-8));
            var basin=Flood();var stalks=Stalks();yield return Frames(2);
            var bloom=Enemy("Rotbloom",At(0,.1f,0));yield return Frames(3);var body=bloom.transform;
            LingeringArea.Lay(At(4,0,4),2,30,AreaEffect.Slow,.6f,0,bloom.Health,DamageType.Kinetic);yield return null;
            var spoken=new List<string>();System.Action<string,string> heard=(who,line)=>spoken.Add(who+": "+line);BarkService.Spoken+=heard;
            try{cues.Clear();Kill(bloom.Health,hero);yield return Frames(3);}finally{BarkService.Spoken-=heard;}
            Assert.AreEqual(0,bloom.Health.integrity,"fixture: the kill, through its shut mouths");
            Assert.IsTrue(Set("bossdown.Rotbloom"),"killed, it isn't counted as beaten");Assert.IsTrue(Said("BOSS_DOWN Rotbloom"));
            Assert.AreEqual(BossController.Ending.Destroyed,BossController.EndingOf("Rotbloom"),"it is a predator and it dies");Assert.IsFalse(Said("BOSS_LEAVES"),"it left instead of dying");
            Assert.IsEmpty(spoken,"a bark talks over the quiet the hall's own scene is for: "+string.Join(", ",spoken));
            Assert.IsTrue(stalks.All(s=>!s.Standing),"its stalks still stand after it");Assert.IsFalse(stalks.Any(s=>Set(s.defeatedFlag)),"stalks settled by its fall are counted as burned by the party");
            Assert.AreEqual(0,basin.told.Last().fraction,.001f,"it is dead and the hall wasn't told to drain");
            Assert.IsEmpty(LingeringArea.All.Where(a=>a!=null),"its clouds outlast it");
        }

        // ------------------------------------------------------------------ what the fight is sized on
        const float FlatOutSeconds=15;
        /// <summary>The party flat out on a body that never moves, as QaPlanCombatTests measures it: both heroes attacking, every skill as its cooldown and Charge allow.</summary>
        IEnumerator Rate(string id,System.Action<float> measured)
        {
            var at=At(0,.1f,0);var party=PartyController.Current;if(party.Active.character!="Taren")Assert.IsTrue(party.Swap(),"fixture: the swap");
            var taren=party.Active;var sela=party.members.First(m=>m!=taren);
            var enemy=Enemy(id,at);yield return Frames(3);var body=enemy.Health;
            enemy.GetComponent<BossController>().enabled=false;body.maximum=body.integrity=10000000;
            float reach=BossReach.Edge(enemy);var tarenAt=at+Vector3.back*reach;var selaAt=at+Vector3.back*(reach+5)+Vector3.right*2;
            Put(taren,tarenAt);Put(sela,selaAt);taren.target=sela.target=body;taren.charge=sela.charge=100;yield return null;
            float landed=0;body.Damaged+=(_,packet,amount)=>{if(packet.source==taren.Health||packet.source==sela.Health)landed+=amount;};
            for(float end=Time.unscaledTime+FlatOutSeconds;Time.unscaledTime<end;)
            {
                foreach(var hero in new[]{taren,sela})
                {
                    hero.motor.Move(Vector2.zero,false,false);var home=hero==taren?tarenAt:selaAt;
                    if(hero.CanAct&&Flat(hero.transform.position,home)>.6f){var keep=hero.Health.integrity;Put(hero,home);hero.Health.integrity=keep;}
                    hero.target=body;if(!hero.CanAct)continue;
                    bool acted=false;
                    for(int slot=0;slot<SkillBook.Slots&&!acted;slot++)
                    {
                        var skill=SkillBook.Equipped(hero.character,slot);
                        if(skill==null||hero.cooldowns[slot]>0||hero.charge<SkillBook.Cost(hero.character,skill))continue;
                        acted=hero.Skill(slot);
                    }
                    if(!acted)hero.Attack();
                }
                yield return null;
            }
            Assert.Greater(landed,0,"fixture: the party is hitting it");measured(landed/FlatOutSeconds);
            enemy.Passive=true;Object.Destroy(enemy.gameObject);
            foreach(var left in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(left is GravityWell||left is PulseField||left is StarfallStrike||left is NetSeed)Object.Destroy(left.gameObject);
            yield return Frames(3);
        }
        /// <summary>The plan's length on today's build, with nothing assumed: two to five minutes for a Sync 14 party in the chapter's arrival parts (Prime). The first half is its two
        /// stalks (each a share of its own integrity), the second its own bar, both at the share of flat out the replay driver was measured to get of the whole fight, walks to the
        /// ramps and time under its moves included (its sheet's driverShare, from RotbloomDriverTests). Until November 14 this assumed .3 and a stalk struck half the time, which
        /// held a fight at three and a half minutes that the driver, fighting it as a person does, finished in 74 s. And the integrity is pinned to the builder: a rebuild writes
        /// whatever its source says.</summary>
        [UnityTest] public IEnumerator TheRotbloomLastsWhatThePlanAsksAtThePartysMeasuredRate()
        {
            string source=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"_Project/Scripts/Editor/World/WendmireBuilder.Enemies.cs"));
            float bloom=GameCatalog.Find<EnemyDef>("Rotbloom").integrity;
            StringAssert.Contains("e.id=\"Rotbloom\";",source);StringAssert.Contains($"e.integrity={bloom:0};e.breakThreshold=900;",source,"a rebuild would put the Rotbloom's old integrity back");
            yield return Zone("Arena_Ground");Floor();
            foreach(var member in GameServices.Current.State.party)member.level=14;PartyController.Current.RefreshStats();yield return null;
            float rate=0;yield return Rate("Rotbloom",r=>rate=r);
            float share=ContentRegistry.Boss("Rotbloom").driverShare;
            float stalks=BossController.RotbloomTune.StalksToDrain*bloom*BossController.RotbloomTune.StalkIntegrity/(rate*share);
            float bar=bloom*(1-BossController.RotbloomTune.StalksToDrain*BossController.RotbloomTune.StalkBar)/(rate*share);float seconds=stalks+bar;
            TestContext.WriteLine($"WENDMIRE_RATE Rotbloom sync=14 prime rate={rate:0}/s stalks={stalks:0}s bar={bar:0}s total={seconds:0}s");Debug.Log($"WENDMIRE_RATE Rotbloom rate={rate:0} stalks={stalks:0} bar={bar:0} seconds={seconds:0}");
            Assert.That(seconds,Is.InRange(120,300),$"the Rotbloom's {bloom:0} integrity at {rate:0} a second flat out lasts {stalks:0} s of stalks and {bar:0} s of bar, {seconds:0} s: the plan asks 120 to 300");
        }
    }
}
