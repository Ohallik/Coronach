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
    /// <summary>Chapter 3's fights, each on a bare floor in the proving yards with what it needs made here. One test for each family of enemy behaviour (Lattice.Combat.EnemyBehaviour)
    /// on the chapter's own enemy of that family, saying what a player would see: the charger stuck in a wall, the cloud that stays and goes, the ringed hide, the swarm that bursts, the
    /// plated front, the knot that lets go, the tether and the roll, the wing that scatters. Then the two bosses: the Seedthane's legs, pods and leaving, and the Floodcrown's gates,
    /// water (through IArenaWater, with a stand-in for the basin's), wave, grounding, puddles and drain. The chapter's expectations for the shared enemy and boss tests are here too.</summary>
    public sealed class XylosCombatTests
    {
        const float X=230;
        readonly List<string> log=new();readonly List<(string key,Transform emitter)> cues=new();
        static readonly string[] Heeded={"CHARGER_","AMBUSH_","SWARM_","GUARD_","ANCHOR_","TETHER_","STRAFER_","SEEDTHANE_","FLOODCROWN_","BOSS_","PART_","POD_","GATE_","COMBAT_KILL"};
        void Heard(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        void Cue(string key,Transform emitter)=>cues.Add((key,emitter));
        FakeWater water;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter3");log.Clear();cues.Clear();Application.logMessageReceived+=Heard;AudioManager.CueStarted+=Cue;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;AudioManager.CueStarted-=Cue;
            if(water!=null){ArenaWater.Unregister(water);water=null;}
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Xylos(ContentRegistry.Book book)
        {
            // Where a hero stands for each ordinary enemy's tell: inside the reach its attack starts from, and for the Reedstalker inside its hide's ring.
            book.Tell("Thornback",3);book.Tell("Reedstalker",2.4f);book.Tell("Sporecaster",7);book.Tell("MireflySwarm",2);book.Tell("Rootwarden",2);
            book.Tell("Podskimmer",3.5f,flight:true);book.Tell("SnagvineNode",3,flight:true);book.Tell("ClaimDrone",8,flight:true);
            // A mini-boss: the plan asks a minute to a minute and a half of it for the replay party (the shared test's floor, at the driver's share), which is under 145 s for someone
            // dodging most of it (its ceiling, at a quarter); its legs make it shorter for a party that breaks them. The share is the chain's (nov13b, November 13), against the
            // shared test's flat-out rate (BOSS_RATE, 1,498 a second): 27,000 fell in 43.6 and 39.5 s (.42 to .46, where .3 was assumed: it stands planted for half the fight and each
            // leg takes 6% of its bar with it), and 44,000 in 82.3 s (.36, the lower, as the rule is).
            // .56 since D362 (November 22): today's windowless scout of the 10:30 build (Builds/quality/nov22-recon-e/s20) felled 39,000 in 59.1 s against this morning's 1,165 and
            // 1,172 a second (d355-dome3, d355-lengths), which is .565; nov22-whole (s20, the night before) took 37.8 s. It is 50,000 since D362 (continued): TheTwoFightsLast's
            // 90 s at .56 on its slowest own reading (1,042) holds it there. The fast case (an EXTRA carried in from the road and all four legs gone: 36.9 s at 40,800) is about 45 s.
            book.Boss(new ContentRegistry.BossSheet{id="Seedthane",zone="BraidedRiver",door="Seedthane",doorFlags=new[]{"xylos.seedfallSeen"},sync=11,driverShare=.56f,minSeconds=60,maxSeconds=145,
                specials=new[]{("ThornFan",new Vector3(0,0,5)),("FanVolley",new Vector3(0,0,5)),("PodShake",new Vector3(0,0,6)),("CircleFan",new Vector3(0,0,6)),("StampAdvance",new Vector3(0,0,6))},
                windows=new[]{("FanVolley",new Vector3(0,0,5)),("StampAdvance",new Vector3(0,0,8))}});
            // The chapter's boss, two to five minutes. The share is the chain's (nov13b, November 13): 62,000 fell in 101.9 s against the shared test's 1,208 a second, which is .5
            // (while the replay driver still measured its reach to a body's middle and stood idle against this one's flank, it took 158 and 165 s, and .3 looked right).
            // .63 since D362 (November 22): today's scouts felled 88,000 in 109.4, 98.9 (nov22-recon-e s21, s21b), 107.3 (nov22-recon-f21 s21) and 114.9 s (nov22-recon-h-21, the
            // 14:52 build) against 1,215 and 1,232 a second (d355-dome3, d355-lengths): .626 at the longest, the lower share as the rule is. 112,000 since D362 (continued):
            // the slow pair's 400 s on its slowest reading (1,156) holds it there, about 126 s on the fastest scout.
            book.Boss(new ContentRegistry.BossSheet{id="Floodcrown",zone="WalkingCanopy",door="Floodcrown",doorFlags=new[]{"xylos.basinSeen"},sync=12,driverShare=.63f,
                specials=new[]{("AntlerSweep",new Vector3(0,0,5)),("GateSlam",new Vector3(0,0,9)),("WadingCharge",new Vector3(0,0,10)),("CrownCharge",new Vector3(0,0,10)),("Stomp",new Vector3(0,0,8))},
                windows=new[]{("WadingCharge",new Vector3(0,0,10)),("Stomp",new Vector3(0,0,8))}});
            // The ways out of Nearspace and Rootmarket that wait on a story flag, and what a refused press must name (the words each lockedPrompt says).
            book.Lock("xylos.charted","Pell","lantern");          // Tallow Approach's warp to Xylos: "No lane to Xylos yet. Pell's lantern has to learn it first."
            book.Lock("xylos.bargesFree","barges");              // the delta beacon: "The delta beacon is dark: cut Teo Marrow's three seed barges free"
            book.Lock("xylos.termsHeard","Ruva");                // Rootmarket's river gate: "Ruva Okh hasn't said what she wants yet: find her at the mooring"
            // Where each choice of the chapter's nodes with choices leads (docs/writing/Xylos.yarn): Ruva's and Teo's three questions are talk; Binna's first offer is her
            // stall or the bowl; Hadda's three are the press, the cradle and her parts, in that order, at every tier.
            book.Branch("RuvaRepeat","world","world","world");book.Branch("TeoRepeat","world","world","world");book.Branch("BinnaFirst","shop","world");
            foreach(string node in new[]{"HaddaFirst","HaddaRepeat","HaddaPost","HaddaAir"})book.Branch(node,"bench","repair","shop");
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
        /// <summary>The flight yard's open space, on its plane.</summary>
        static Vector3 Sky(float x,float z)=>new(200+x,1,z);
        /// <summary>Open, level floor far from the yard's walls, its top at height zero.</summary>
        static void Floor()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Xylos fixture floor";floor.transform.position=new Vector3(X,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static GameObject Block(Vector3 centre,Vector3 size)
        {var wall=new GameObject("Xylos fixture block",typeof(BoxCollider));wall.transform.position=centre;wall.GetComponent<BoxCollider>().size=size;Physics.SyncTransforms();return wall;}
        static (CombatActor lead,CombatActor partner) Party(){var lead=PartyController.Current.Active;return (lead,PartyController.Current.members.First(m=>m!=lead));}
        /// <summary>Stood there facing +z, whole, with a bar no fixture blow will empty.</summary>
        static void Put(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            actor.Health.maximum=actor.Health.integrity=5000;actor.Health.InvulnerableUntil=0;Physics.SyncTransforms();
        }
        static void Stand(EnemyBrain enemy,Vector3 position)
        {var body=enemy.GetComponent<CharacterController>();bool was=body.enabled;body.enabled=false;enemy.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=was;Physics.SyncTransforms();}
        static EnemyBrain Enemy(string id,Vector3 at,bool passive=true){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=passive;return enemy;}
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
        bool Said(string start)=>log.Any(l=>l.StartsWith(start));

        /// <summary>The basin's water as the Floodcrown sees it, with nothing of the world behind it: what it was told, and a dry place if the test wants one.</summary>
        sealed class FakeWater:IArenaWater
        {
            public float level=1;public readonly List<(float fraction,float seconds)> told=new();public System.Func<Vector3,bool> dry=_=>false;
            public float Level=>level;
            public void SetLevel(float fraction,float seconds){told.Add((fraction,seconds));level=fraction;}
            public bool Submerged(Vector3 point)=>level>0&&!dry(point);
        }
        FakeWater Flood(){water=new FakeWater();ArenaWater.Register(water);return water;}
        static FloodGate Gate(string name,int index,Vector3 at)
        {
            var go=new GameObject(name);go.SetActive(false);go.transform.position=at;
            var health=go.AddComponent<Health>();health.id="FloodGate";health.maximum=health.integrity=400;health.breakThreshold=100000;
            go.AddComponent<SphereCollider>().radius=1.2f;go.AddComponent<Hurtbox>().owner=health;
            var gate=go.AddComponent<FloodGate>();gate.index=index;gate.defeatedFlag="test.gate."+name;
            go.SetActive(true);return gate;
        }
        /// <summary>Three gates round a rim sixteen metres out, at the Canopy's own bearings.</summary>
        static FloodGate[] Gates()=>new[]{Gate("north",0,At(0,0,16)),Gate("west",1,At(-15,0,-5.5f)),Gate("east",2,At(15,0,-5.5f))};
        static CoverPod Pod(string name,Vector3 at)
        {
            var go=new GameObject(name);go.SetActive(false);go.transform.position=at;
            var hung=new GameObject("Hanging");hung.transform.SetParent(go.transform,false);hung.transform.localPosition=Vector3.up*2.4f;
            var health=hung.AddComponent<Health>();health.id="SeedPod";health.maximum=health.integrity=200;health.breakThreshold=100000;hung.AddComponent<SphereCollider>().radius=.9f;hung.AddComponent<Hurtbox>().owner=health;
            var down=new GameObject("Fallen",typeof(BoxCollider));down.transform.SetParent(go.transform,false);down.transform.localPosition=Vector3.up*.7f;down.GetComponent<BoxCollider>().size=new Vector3(2.2f,1.4f,2.2f);
            var pod=go.AddComponent<CoverPod>();pod.hanging=hung;pod.fallen=down;
            go.SetActive(true);return pod;
        }

        // ------------------------------------------------------------------ the behaviour families
        [UnityTest] public IEnumerator AThornbackRunIntoAWallIsStuckAndOpenAndOneThatMissesIsOnlyWinded()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,6));
            var wall=Block(At(0,2.5f,9.4f),new Vector3(16,7,.8f));
            var beast=Enemy("Thornback",At(0,.05f,0),false);
            yield return Until(()=>beast.Telegraphing,6,hero);Assert.IsTrue(beast.Telegraphing,"fixture: the Thornback squares up to a hero six metres off");
            var lane=beast.AttackMark;Assert.IsNotNull(lane,"its charge marks nothing");Assert.AreEqual(Telegraph.Kind.Line,lane.Shape,"its charge marks no lane");
            Assert.IsTrue(lane.Contains(hero.transform.position),"the lane isn't laid through the hero it is aimed at");
            Assert.Less(lane.Length,10.6f,"the lane is painted on through the wall the charge will stop at");Assert.Greater(lane.Length,8,"fixture: the lane reaches the wall");
            // He steps out of it, with the wall behind where he stood: the bait.
            Put(hero,At(7,.05f,6));
            yield return Until(()=>Said("CHARGER_STUCK Thornback"),5,hero);
            Assert.IsTrue(Said("CHARGER_STUCK Thornback"),"a Thornback that ran its lane into a wall wasn't stuck");
            Assert.AreEqual(5000,hero.Health.integrity,.01f,"a hero standing outside the lane was struck by the charge");
            Assert.Greater(beast.transform.position.z,6.5f,"it stopped well short of the wall");
            Assert.IsTrue(beast.Health.Broken,"stuck in the wall, it isn't open");
            Assert.That(beast.Health.BrokenUntil-Time.time,Is.InRange(1.9f,2.45f),"stuck, it is open for its recover time: 2.4 s");
            Assert.AreEqual(150,Hit(beast.Health,hero),1,"a hit on a stuck Thornback isn't an opening's half as much again");
            // A run that touches someone is no opening.
            Object.Destroy(wall);yield return Until(()=>!beast.Health.Broken,4,hero);Physics.SyncTransforms();log.Clear();
            Stand(beast,At(0,.05f,0));Put(hero,At(0,.05f,6));
            yield return Until(()=>hero.Health.integrity<5000,9,hero);Assert.Less(hero.Health.integrity,5000,"fixture: a hero who stood in the lane was run down");
            yield return Wait(.9f,hero);
            Assert.IsFalse(Said("CHARGER_"),"a charge that landed left the Thornback open: "+string.Join(", ",log));Assert.IsFalse(beast.Health.Broken);
            // A run that touches nobody, on open floor, winds it: a shorter beat.
            Stand(beast,At(0,.05f,0));Put(hero,At(0,.05f,6));
            yield return Until(()=>beast.Telegraphing,8,hero);Assert.IsTrue(beast.Telegraphing,"fixture: it charges again");
            Put(hero,At(7,.05f,6));
            yield return Until(()=>Said("CHARGER_WINDED Thornback"),5,hero);
            Assert.IsTrue(Said("CHARGER_WINDED Thornback"),"a clean miss on open floor cost it nothing");
            Assert.That(beast.Health.BrokenUntil-Time.time,Is.InRange(.7f,1.2f),"winded, it is open for under half its stuck time");
        }
        [UnityTest] public IEnumerator ASporecastersPodLeavesACloudThatSlowsIsNoDangerAndThenGoes()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,7));
            var caster=Enemy("Sporecaster",At(0,.05f,0),false);
            yield return Until(()=>caster.Telegraphing,5,hero);Assert.IsTrue(caster.Telegraphing,"fixture: the Sporecaster throws at a hero seven metres off");
            var landing=caster.AttackMark;Assert.AreEqual(Telegraph.Kind.Circle,landing.Shape);Assert.IsTrue(landing.Contains(hero.transform.position),"the pod isn't marked where it will land");
            Assert.Greater(Flat(landing.Origin,caster.transform.position),5,"the circle is at the caster's feet, not where the pod comes down");
            yield return Until(()=>!caster.Telegraphing,3,hero);yield return null;
            Assert.Less(hero.Health.integrity,5000,"the pod landed on a hero who stood in its circle and did nothing");
            caster.Passive=true;// One cloud is enough to look at.
            var cloud=LingeringArea.Over(hero.transform.position);Assert.IsNotNull(cloud,"the pod left nothing where it landed");
            Assert.AreEqual(AreaEffect.Slow,cloud.Effect);
            var ring=Telegraph.Live.FirstOrDefault(t=>t!=null&&ReferenceEquals(t.owner,cloud));Assert.IsNotNull(ring,"the cloud has no edge on the floor");
            Assert.IsTrue(Faint(ring),"a cloud that only slows is painted as a danger: it is a boundary (D222)");
            Assert.IsFalse(LateSkills.InDanger(hero),"a cloud that only slows is read as a blow about to land");
            float standing=hero.Health.integrity;yield return Wait(1.2f,hero);
            Assert.AreEqual(.55f,hero.Health.Pace,.01f,"a hero standing in the cloud keeps his pace");
            Assert.AreEqual(standing,hero.Health.integrity,.01f,"a cloud that only slows hurt the hero standing in it");
            Put(hero,At(9,.05f,7));yield return Wait(.4f,hero);Assert.AreEqual(1,hero.Health.Pace,.001f,"a hero who walked out of the cloud is still slowed");
            yield return Until(()=>LingeringArea.All.Count==0,6,hero);Assert.IsEmpty(LingeringArea.All,"the cloud never clears: it should stand five seconds");
            yield return Wait(.3f,hero);Assert.IsFalse(Telegraph.Live.Any(t=>t!=null&&t.owner is LingeringArea),"the cloud went and left its ring on the floor");
        }
        [UnityTest] public IEnumerator AReedstalkersHideIsRingedItComesUpBitesTwiceAndGoesToAnotherHide()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-22));
            var reed=Enemy("Reedstalker",At(0,.05f,0),false);yield return Frames(3);
            var stalker=reed.Behaviour as EnemyAmbusher;Assert.IsNotNull(stalker,"a Reedstalker isn't an ambusher");
            yield return Until(()=>stalker.Hidden,4,hero);Assert.IsTrue(stalker.Hidden,"with nobody near, it never went under");
            var first=stalker.Spot;Assert.IsNotNull(first);Assert.IsTrue(first.Shown,"the hide it is under isn't marked");
            var ring=Telegraph.Live.FirstOrDefault(t=>t!=null&&ReferenceEquals(t.owner,first));Assert.IsNotNull(ring,"the hide's mark isn't on the floor");
            Assert.AreEqual(3.2f,ring.Radius,.01f,"the ring isn't the distance that wakes it");Assert.IsTrue(Faint(ring),"a hide is painted as a danger: nothing strikes there until it has come up and marked its bite");
            Assert.IsFalse(reed.Health.Targetable,"hidden, it can still be targeted");Assert.AreEqual(0,Hit(reed.Health,hero),.01f,"hidden, it can still be hit");
            Assert.IsTrue(reed.GetComponentsInChildren<Renderer>().Where(r=>r is MeshRenderer||r is SkinnedMeshRenderer).All(r=>!r.enabled),"hidden, its body still shows");
            Assert.IsFalse(reed.Telegraphing,"it marks a bite with nobody inside its ring");
            // He steps inside the ring.
            int bitten=0;hero.Health.Damaged+=(_,packet,amount)=>{if(packet.source==reed.Health&&amount>0)bitten++;};
            var bites=new HashSet<Telegraph>();
            Put(hero,first.transform.position+new Vector3(0,.05f,2.5f));
            yield return Until(()=>{if(reed.AttackMark!=null)bites.Add(reed.AttackMark);return Said("AMBUSH_SPENT Reedstalker");},7,hero);
            Assert.IsTrue(Said("AMBUSH_ERUPT Reedstalker"),"a hero inside its ring and it stayed under");
            Assert.IsTrue(Said("AMBUSH_SPENT Reedstalker"),"it never finished its bites");
            Assert.AreEqual(2,bites.Count,"it should mark two bites");Assert.AreEqual(2,bitten,"two bites at a hero who stood there should both land");
            Assert.IsFalse(first.Shown,"it is out of its hide and the hide is still ringed");
            // Spent, in the open: this is when it can be hit.
            Assert.IsTrue(reed.Health.Targetable);Assert.Greater(Hit(reed.Health,hero),0,"spent in the open, it can't be hit");
            Assert.IsTrue(reed.GetComponentsInChildren<Renderer>().Any(r=>(r is MeshRenderer||r is SkinnedMeshRenderer)&&r.enabled),"out of its hide, it isn't drawn");
            yield return Until(()=>stalker.Hidden,7,hero);Assert.IsTrue(stalker.Hidden,"it never went back under");
            Assert.AreNotEqual(first,stalker.Spot,"it went back to the hide it came out of");
            Assert.Greater(Flat(first.transform.position,stalker.Spot.transform.position),3,"its second hide is the first one again");
            Assert.IsTrue(stalker.Spot.Shown,"its second hide isn't ringed");Assert.IsFalse(first.Shown,"the hide it left is still ringed");
        }
        /// <summary>November 13, in the player: the party stood beside three occupied hides on the Braided River's bank and the bank looked empty. The ring was a pale wash with no
        /// line to it, and nothing said that stepping in is how the fight goes on.</summary>
        [UnityTest] public IEnumerator AnOccupiedHidesRingHasAnEdgeAndTheLeadSaysWhatItIsOnce()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-22));
            var flags=Lattice.Core.GameServices.Current.Flags;flags.SetBool(EnemyAmbusher.HideSeen,false);
            var said=new List<string>();void Heard(string who,string line)=>said.Add(line);Lattice.Core.BarkService.Spoken+=Heard;
            try
            {
                var reed=Enemy("Reedstalker",At(0,.05f,0),false);yield return Frames(3);
                var stalker=reed.Behaviour as EnemyAmbusher;
                yield return Until(()=>stalker.Hidden,4,hero);Assert.IsTrue(stalker.Hidden,"fixture: it never went under");
                var first=stalker.Spot;
                var edge=Telegraph.Live.FirstOrDefault(t=>t!=null&&ReferenceEquals(t.owner,first)&&t.Shape==Telegraph.Kind.Ring);
                Assert.IsNotNull(edge,"the hide's ring has no edge: light on the ground with no line to it");
                Assert.AreEqual(first.radius,edge.Radius,.01f,"the edge isn't where the ring ends");Assert.Less(edge.Radius-edge.Inner,.5f,"the edge is a band, not a line");
                Assert.IsTrue(Faint(edge),"the edge is painted as a danger: nothing strikes at a hide's ring");
                Assert.Greater(AmbushSpot.Edge.a,AmbushSpot.Ringed.a+.3f,"the edge is no stronger than the wash it ends");
                string line=Lattice.Core.BarkService.Line(hero.character,"hide");Assert.IsFalse(string.IsNullOrEmpty(line),"the lead has no word for a hide");
                Assert.IsTrue(flags.GetBool(EnemyAmbusher.HideSeen),"the first hide wasn't remembered");Assert.AreEqual(1,said.Count(l=>l==line),"the lead didn't say what the ring is");
                // Drawn out and under again: the edge goes with the ring, and the lead doesn't say it twice.
                Put(hero,first.transform.position+new Vector3(0,.05f,2.5f));
                yield return Until(()=>Said("AMBUSH_SPENT Reedstalker"),7,hero);
                Assert.IsFalse(Telegraph.Live.Any(t=>t!=null&&ReferenceEquals(t.owner,first)),"it is out of its hide and the hide's ring or its edge is still on the floor");
                yield return Until(()=>stalker.Hidden,7,hero);Assert.IsTrue(stalker.Hidden,"it never went back under");
                Assert.AreEqual(1,said.Count(l=>l==line),"the lead says what a ring is every time something goes under");
            }
            finally{Lattice.Core.BarkService.Spoken-=Heard;}
        }
        [UnityTest] public IEnumerator AMireflySwarmIsItsMotesLatchesUntilRolledOffAndBurstsWhenBroken()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,2));
            var swarm=Enemy("MireflySwarm",At(0,.05f,0),false);yield return Frames(3);
            var motes=swarm.GetComponentInChildren<SwarmMotes>();Assert.IsNotNull(motes,"a swarm isn't drawn as motes");Assert.AreEqual(18,motes.count);
            Assert.IsFalse(swarm.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("ART_PENDING")),"a swarm with no model is drawn as a placeholder shape");
            // Its only mesh is its shadow (SwarmMotes, November 21): flat on the floor under the cloud. Anything else, or anything standing up off the floor, is a solid body.
            Assert.IsFalse(swarm.GetComponentsInChildren<MeshRenderer>().Any(r=>r.name!="Swarm shadow"),"a swarm has a solid body under its motes");
            foreach(var shadow in swarm.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Swarm shadow"))
                Assert.Less(shadow.bounds.size.y,.05f,"a swarm's shadow stands up off the floor: a solid body under its motes");
            yield return Until(()=>swarm.Telegraphing,4,hero);Assert.IsTrue(swarm.Telegraphing,"fixture: the swarm marks its dart");
            Assert.IsTrue(swarm.AttackMark.Contains(hero.transform.position));
            yield return Until(()=>motes.Spread<.7f||!swarm.Telegraphing,1,hero);Assert.Less(motes.Spread,.7f,"the cloud doesn't draw in through its tell");
            yield return Until(()=>Said("SWARM_LATCH MireflySwarm"),3,hero);Assert.IsTrue(Said("SWARM_LATCH MireflySwarm"),"a dart that landed didn't latch");
            Assert.IsTrue(EnemySwarmCloud.On(hero));yield return Frames(3);
            Assert.AreEqual(.7f,hero.Health.Pace,.01f,"a latched swarm doesn't slow the hero it rides");
            float ridden=hero.Health.integrity;yield return Wait(.6f,hero);
            Assert.AreEqual(ridden,hero.Health.integrity,.01f,"a latch does damage nobody marked");Assert.Less(Flat(swarm.transform.position,hero.transform.position),1.2f,"the swarm isn't on the hero it latched to");
            // A roll sheds it.
            Assert.IsTrue(hero.Dodge(Vector3.right),"fixture: the roll");yield return Wait(.45f,hero);
            Assert.IsTrue(Said("SWARM_SHED MireflySwarm rolled"),"a roll doesn't shed the swarm");Assert.IsFalse(EnemySwarmCloud.On(hero));
            yield return Wait(.3f,hero);Assert.AreEqual(1,hero.Health.Pace,.001f,"shed, the swarm still slows him");
            // Broken, it bursts: a marked blast on a short fuse where it hung.
            swarm.Passive=true;Put(hero,swarm.transform.position+new Vector3(1.2f,0,0));yield return null;
            Hit(swarm.Health,hero,1,null,500);Assert.IsTrue(swarm.Health.Broken,"fixture: the break");
            yield return Frames(2);
            Assert.IsFalse(swarm.Health.Alive,"a broken swarm holds together");Assert.IsTrue(Said("SWARM_BURST MireflySwarm"));Assert.IsTrue(Said("COMBAT_KILL MireflySwarm"),"a swarm that burst paid nobody for the kill");
            var blast=Object.FindFirstObjectByType<DeathBlast>();Assert.IsNotNull(blast,"it burst with no blast");
            var fuse=Telegraph.Live.FirstOrDefault(t=>t!=null&&ReferenceEquals(t.owner,blast));Assert.IsNotNull(fuse,"its burst marks nothing");
            Assert.IsTrue(fuse.Contains(hero.transform.position),"fixture: he stands in the burst");
            float before=hero.Health.integrity;yield return Wait(.9f,hero);
            Assert.Less(hero.Health.integrity,before,"the burst passed over a hero who stood in its marking");
        }
        [UnityTest] public IEnumerator ARootwardensFrontTurnsABlowItsBackDoesNotAndItsPlateIsDownAfterItsLongMove()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            var warden=Enemy("Rootwarden",At(0,.05f,0));yield return Frames(3);
            var guard=warden.Behaviour as EnemyGuardian;Assert.IsNotNull(guard,"a Rootwarden isn't a guardian");Assert.IsTrue(guard.Guarding);
            var kinds=new List<HitKind>();System.Action<Health,float,HitKind> seen=(body,_,kind)=>{if(body==warden.Health)kinds.Add(kind);};Health.DamageNumber+=seen;
            try
            {
                Put(hero,At(0,.05f,4));Assert.AreEqual(25,Hit(warden.Health,hero),.5f,"a blow from in front isn't cut to a quarter");Assert.AreEqual(HitKind.Resisted,kinds.Last(),"a blow its plate turned doesn't read as resisted");
                Put(hero,At(0,.05f,-4));Assert.AreEqual(100,Hit(warden.Health,hero),.5f,"a blow from behind is turned by a plate it doesn't have there");Assert.AreEqual(HitKind.Normal,kinds.Last());
                Put(hero,At(4,.05f,0));Assert.AreEqual(100,Hit(warden.Health,hero),.5f,"a blow from its flank is turned: its arc is 150 degrees");
            }
            finally{Health.DamageNumber-=seen;}
            var plate=warden.transform.Find("Front plate");Assert.IsNotNull(plate,"its plate isn't drawn");Assert.IsTrue(plate.gameObject.activeSelf);
            // From range it sends a root line, and then its arms are in the ground: plate down, and it doesn't turn.
            Put(hero,At(0,.05f,8));warden.Passive=false;
            yield return Until(()=>warden.Telegraphing,5,hero);Assert.IsTrue(warden.Telegraphing,"fixture: it attacks a hero eight metres off");
            Assert.AreEqual(Telegraph.Kind.Line,warden.AttackMark.Shape,"out of its slam's reach it doesn't send a root line");Assert.IsTrue(warden.AttackMark.Contains(hero.transform.position));
            yield return Until(()=>Said("GUARD_DOWN Rootwarden"),3,hero);Assert.IsTrue(Said("GUARD_DOWN Rootwarden"),"its long move leaves no opening");
            Assert.Less(hero.Health.integrity,5000,"the root line passed under a hero who stood on it");
            yield return null;Assert.IsFalse(guard.Guarding);Assert.IsFalse(plate.gameObject.activeSelf,"its plate is down and still drawn");
            Put(hero,warden.transform.position+warden.transform.forward*4);Assert.AreEqual(100,Hit(warden.Health,hero),.5f,"with its arms in the ground its front still turns a blow");
            var facing=warden.transform.forward;Put(hero,warden.transform.position-warden.transform.forward*4);yield return Wait(.8f,hero);
            Assert.Less(Vector3.Angle(facing,warden.transform.forward),3,"it turned to follow him with its arms in the ground");
            yield return Until(()=>guard.Guarding,2.5f,hero);Assert.IsTrue(guard.Guarding,"its plate never comes back up");
            // It holds its post: led off, it doesn't follow.
            Put(hero,At(0,.05f,34));yield return Wait(4,hero);
            Assert.Less(Flat(warden.transform.position,At(0,0,0)),10.5f,"it left its post to chase a hero thirty metres off");
        }
        [UnityTest] public IEnumerator ASnagvineKnotHoldsItsGroundALungeTakesItsLashAndItsDeathFreesWhatItHeld()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-60,-60));Put(hero,Sky(0,3));
            var knot=Enemy("SnagvineNode",Sky(0,0));yield return Frames(3);
            Assert.IsInstanceOf<EnemyAnchored>(knot.Behaviour,"a Snagvine Knot isn't anchored");
            Assert.IsFalse(knot.CanBeMoved,"a knot can be dragged");var held=knot.transform.position;knot.Displace(Vector3.right*3);Assert.AreEqual(held,knot.transform.position,"a drag moved the knot");
            knot.Passive=false;
            yield return Until(()=>knot.Telegraphing,4,hero);Assert.IsTrue(knot.Telegraphing,"fixture: the knot lashes at a ship inside its reach");
            Assert.AreEqual(Telegraph.Kind.Cone,knot.AttackMark.Shape,"its lash isn't marked as a fan");Assert.IsTrue(knot.AttackMark.Contains(hero.transform.position));
            // A blade lunge landing mid-tell: the lash is gone, and it doesn't start another at once.
            Assert.Greater(Hit(knot.Health,hero,20,"lunge"),0);
            Assert.IsFalse(knot.Telegraphing,"a lunge landed mid-tell and the lash still came");Assert.IsTrue(Said("ANCHOR_LUNGED SnagvineNode"));
            yield return Wait(1.1f,hero);Assert.AreEqual(5000,hero.Health.integrity,.01f,"the knot lashed a ship that had just lunged it");Assert.IsFalse(knot.Telegraphing,"it began another lash within the beat a lunge buys");
            Assert.AreEqual(held,knot.transform.position,"the knot moved");
            // Any other hit doesn't take the tell from it.
            yield return Until(()=>knot.Telegraphing,4,hero);Assert.IsTrue(knot.Telegraphing,"fixture: it lashes again");
            Hit(knot.Health,hero,20);Assert.IsTrue(knot.Telegraphing,"an ordinary shot took its lash away: only a lunge does");
            yield return Until(()=>hero.Health.integrity<5000,2,hero);Assert.Less(hero.Health.integrity,5000,"its lash missed a ship inside its fan");
            EnemyBrain freed=null;Vector3 where=default;System.Action<EnemyBrain,Vector3> let=(brain,at)=>{freed=brain;where=at;};EnemyAnchored.Freed+=let;
            try{Kill(knot.Health,hero);}finally{EnemyAnchored.Freed-=let;}
            Assert.AreEqual(knot,freed,"its death frees nothing");Assert.Less(Vector3.Distance(where,held),.1f);Assert.IsTrue(Said("ANCHOR_FREED SnagvineNode"));
        }
        [UnityTest] public IEnumerator AClaimDronesTetherSlowsAndReelsAShipUntilItRollsFreeOrTheLineIsCut()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-60,-60));Put(hero,Sky(0,9));
            var drone=Enemy("ClaimDrone",Sky(0,0),false);
            yield return Until(()=>drone.Telegraphing,5,hero);Assert.IsTrue(drone.Telegraphing,"fixture: the drone tags a ship nine metres off");
            Assert.AreEqual(Telegraph.Kind.Line,drone.AttackMark.Shape,"its tether isn't marked as a line");Assert.IsTrue(drone.AttackMark.Contains(hero.transform.position));
            yield return Until(()=>Said("TETHER_ON ClaimDrone"),3,hero);Assert.IsTrue(Said("TETHER_ON ClaimDrone"),"a tether that struck didn't hold");
            Assert.IsTrue(EnemyTetherer.On(hero));Assert.Less(hero.Health.integrity,5000,"the catch is a hit, and it did nothing");
            yield return Frames(3);Assert.AreEqual(.5f,hero.Health.Pace,.01f,"a tethered ship keeps its speed");
            Assert.IsNotNull(GameObject.Find("Tether line"),"the tether isn't drawn");
            float gap=Flat(hero.transform.position,drone.transform.position);yield return Wait(1,hero);
            Assert.Less(Flat(hero.transform.position,drone.transform.position),gap-2,"a tethered ship isn't reeled in");
            // Held, it flies at half its speed.
            var from=hero.transform.position;float flown=Time.time;
            yield return Until(()=>{hero.motor.Move(Vector2.right,false,false);return false;},1.5f);
            float held=Flat(hero.transform.position,from)/(Time.time-flown);
            Assert.IsTrue(EnemyTetherer.On(hero),"fixture: still held");Assert.Less(held,9,$"a tethered ship flew at {held:0.0} m/s: its top speed is 12, and held it should be about half");
            // A roll frees it.
            Assert.IsTrue(hero.Dodge(Vector3.right),"fixture: the roll");yield return Wait(.45f,hero);
            Assert.IsTrue(Said("TETHER_OFF ClaimDrone rolled"),"a roll doesn't free a tethered ship: "+string.Join(", ",log));Assert.IsFalse(EnemyTetherer.On(hero));
            yield return Wait(.3f,hero);Assert.AreEqual(1,hero.Health.Pace,.001f,"freed, it is still slowed");Assert.IsNull(GameObject.Find("Tether line"),"the line is still drawn");
            // Caught again, and this time the line is cut: any hit on the drone.
            log.Clear();Put(hero,drone.transform.position+new Vector3(0,0,8));
            yield return Until(()=>Said("TETHER_ON ClaimDrone"),10,hero);Assert.IsTrue(Said("TETHER_ON ClaimDrone"),"fixture: it tethers again");
            Hit(drone.Health,hero,10);
            Assert.IsTrue(Said("TETHER_OFF ClaimDrone cut"),"a hit on the drone doesn't cut its line");Assert.IsFalse(EnemyTetherer.On(hero));
        }
        [UnityTest] public IEnumerator PodskimmersRunTheirLanesAndTheWingScattersWhenOneDies()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-60,-60));Put(hero,Sky(0,0));
            var wing=new[]{Enemy("Podskimmer",Sky(-7,-7),false),Enemy("Podskimmer",Sky(7,-7),false),Enemy("Podskimmer",Sky(0,9),false)};
            yield return Until(()=>wing.Any(w=>w.Telegraphing),6,hero);
            var runner=wing.FirstOrDefault(w=>w.Telegraphing);Assert.IsNotNull(runner,"fixture: one of the wing marks a run");
            Assert.AreEqual(Telegraph.Kind.Line,runner.AttackMark.Shape,"a pass isn't marked as a lane");Assert.IsTrue(runner.AttackMark.Contains(hero.transform.position),"the lane isn't laid through the ship it is aimed at");
            var dies=wing.First(w=>w!=runner);var at=dies.transform.position;var others=wing.Where(w=>w!=dies).ToArray();var gaps=others.Select(w=>Flat(w.transform.position,at)).ToArray();
            Kill(dies.Health,hero);
            Assert.AreEqual(2,log.Count(l=>l=="STRAFER_SCATTER Podskimmer"),"one of a wing died and the rest flew on as they were");
            Assert.IsFalse(runner.Telegraphing,"the one marking its run kept it when its wingmate died");
            foreach(var other in others)Assert.IsTrue(((EnemyStrafer)other.Behaviour).Scattering);
            float before=hero.Health.integrity;yield return Wait(1,hero);
            for(int i=0;i<others.Length;i++)Assert.Greater(Flat(others[i].transform.position,at),gaps[i]+3,"a scattering skimmer didn't fly from where its wingmate died");
            Assert.AreEqual(before,hero.Health.integrity,.01f,"a scattering wing struck the ship");
            // And they come round again.
            yield return Until(()=>others.Any(w=>w.Telegraphing),8,hero);Assert.IsTrue(others.Any(w=>w.Telegraphing),"the wing never came back for another run");
        }

        // ------------------------------------------------------------------ the Seedthane
        [UnityTest] public IEnumerator TheSeedthaneStandsPlantedEachLegBrokenOpensItAndTheLastTopplesItIntoItsSecondPhase()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,14));
            var thane=Enemy("Seedthane",At(0,.1f,0),false);yield return Frames(3);
            var boss=thane.GetComponent<BossController>();var body=thane.Health;var legs=thane.GetComponent<BossParts>();
            Assert.IsNotNull(legs,"the Seedthane has no legs to break");Assert.AreEqual(BossController.SeedthaneTune.Legs,legs.Parts.Count);Assert.AreEqual(4,legs.Standing);
            StringAssert.Contains("LEGS",boss.StatusText,"its bar doesn't say what to break");
            // Planted: a hero standing off isn't walked down.
            var planted=thane.transform.position;yield return Wait(2.5f,hero);
            Assert.Less(Flat(planted,thane.transform.position),.4f,"planted, it walked");
            thane.Passive=true;yield return Until(()=>!boss.Busy,6,hero);yield return Wait(.2f,hero);
            float whole=body.maximum,share=BossController.SeedthaneTune.LegBar;
            for(int i=0;i<3;i++)
            {
                var leg=legs.Parts[i];Assert.IsTrue(leg.Targetable,"a planted leg can't be hit");Assert.AreEqual(whole*BossController.SeedthaneTune.LegIntegrity,leg.maximum,1);
                float bar=body.integrity;log.Clear();Kill(leg,hero);
                Assert.IsTrue(Said("SEEDTHANE_LISTS"),"a leg broke and it stood as it was");Assert.AreEqual(bar-whole*share,body.integrity,2,"a leg should take six hundredths of its bar with it");
                Assert.IsTrue(body.Broken,"listing, it isn't open");Assert.That(body.BrokenUntil-Time.time,Is.InRange(2.6f,3.05f),"it lists for three seconds");
                Assert.AreEqual(150,Hit(body,hero),1,"a hit on the listing Seedthane isn't an opening's half as much again");
                yield return Until(()=>!body.Broken,4,hero);yield return Wait(.1f,hero);
            }
            Assert.AreEqual(1,legs.Standing);log.Clear();float last=body.integrity;Kill(legs.Parts[3],hero);
            Assert.IsTrue(Said("SEEDTHANE_TOPPLED"),"its last leg broke and it didn't go over");
            Assert.AreEqual(last-whole*share,body.integrity,2,"the last leg takes its own share of the bar, as the others did");
            Assert.That(body.BrokenUntil-Time.time,Is.InRange(4.5f,5.05f),"toppled, it is down for five seconds");
            Assert.AreEqual("",boss.StatusText,"with no legs left its bar still says to break them");
            // Under half it shifts: whatever legs it has left come out of the ground, and it walks.
            yield return Until(()=>!body.Broken,6,hero);body.integrity=whole*.48f;thane.Passive=false;
            yield return Until(()=>Said("BOSS_SHIFT Seedthane 2"),12,hero);Assert.IsTrue(Said("BOSS_SHIFT Seedthane 2"),"toppled under half, it never shifted");
            Assert.IsTrue(Said("SEEDTHANE_UPROOTS"));Assert.IsFalse(legs.Exposed,"its legs can still be hit once it is up and walking");
            Put(hero,thane.transform.position+new Vector3(0,0,16));var stood=thane.transform.position;
            yield return Wait(4,hero);Assert.Greater(Flat(stood,thane.transform.position),2,"in its second phase it still stands planted");
        }
        /// <summary>D375: the planted half is its legs' to end. In four of five fights of November 22 the bar crossed half with two or three legs standing, on the openings of the
        /// two that went (37 to 55 s against a bar from 60). While a leg stands no blow and no leg but the last takes it over half; the last takes its own share and carries it.</summary>
        [UnityTest] public IEnumerator PlantedOnALegTheSeedthanesBarHoldsAtHalfAndOnlyTheLastLegCarriesItOver()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,14));
            var thane=Enemy("Seedthane",At(0,.1f,0));yield return Frames(3);
            var boss=thane.GetComponent<BossController>();var body=thane.Health;var legs=thane.GetComponent<BossParts>();float whole=body.maximum,line=whole*boss.Thresholds[0];
            Assert.AreEqual(BossController.SeedthaneTune.LegIntegrity,BossController.SeedthaneTune.LegBar,.0001f,"a leg costs less than it takes off the bar");
            // The broken state, in this test only: with nothing holding the bar, a heavy blow just over half takes it over with every leg standing.
            var holds=body.Holds;Assert.IsNotNull(holds,"nothing holds the planted Seedthane's bar");
            body.Holds=null;body.integrity=line+whole*.02f;Hit(body,hero,whole*.05f);
            Assert.Less(body.integrity,line,"fixture: the blow is heavy enough to cross half");
            body.Holds=holds;body.integrity=line+whole*.02f;yield return Frames(2);
            float took=Hit(body,hero,whole*.05f);
            Assert.AreEqual(line+1,body.integrity,.5f,"a blow took the planted Seedthane over half with four legs standing");
            Assert.AreEqual(whole*.02f-1,took,1,"a blow on the held line doesn't say what it took");
            Assert.IsTrue(body.Alive);Assert.AreEqual(1,boss.Phase,"with its legs in the ground it shifted");
            // A leg with others standing stops there too.
            for(int i=0;i<3;i++)
            {
                log.Clear();Kill(legs.Parts[i],hero);Assert.IsTrue(Said("SEEDTHANE_LISTS"));
                Assert.AreEqual(line+1,body.integrity,.5f,$"leg {i+1} of four took it over half");
                yield return Until(()=>!body.Broken,4,hero);Hit(body,hero,whole*.05f);Assert.AreEqual(line+1,body.integrity,.5f,"held, a blow went through");
            }
            // The last takes its share and carries it over: it topples, and once it is up the bar is its own again.
            log.Clear();Kill(legs.Parts[3],hero);
            Assert.IsTrue(Said("SEEDTHANE_TOPPLED"),"its last leg broke and it didn't go over");
            Assert.AreEqual(line+1-whole*BossController.SeedthaneTune.LegBar,body.integrity,2,"the last leg didn't take its own share over half");
            float bar=body.integrity;Assert.AreEqual(150,Hit(body,hero),1,"toppled, a blow doesn't land whole and half again");Assert.AreEqual(bar-150,body.integrity,1);
        }
        [UnityTest] public IEnumerator AShakenPodComesDownOnItsMarkAndAFallenPodStopsTheFan()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(12,.05f,-12));Put(hero,At(0,.05f,4.5f));
            var pod=Pod("Fixture pod",At(0,0,4.5f));var far=Pod("Far pod",At(60,0,0));
            var thane=Enemy("Seedthane",At(0,.1f,0));yield return Frames(3);var boss=thane.GetComponent<BossController>();
            Assert.IsTrue(pod.Hanging);Assert.IsTrue(boss.Perform("PodShake"),"fixture: the shake");yield return null;
            var marks=Telegraph.Live.Where(t=>t!=null&&t.owner==boss&&!t.Released).ToArray();
            Assert.IsTrue(marks.Any(t=>Flat(t.Origin,pod.transform.position)<.1f),"the pod it is shaking loose marks nothing where it will land");
            Assert.IsTrue(marks.Any(t=>t.Contains(hero.transform.position)),"its loose husks aren't marked at the hero");
            yield return Until(()=>!boss.Busy,4,hero);
            Assert.IsTrue(pod.Down,"the shake brought no pod down");Assert.IsTrue(Said("POD_DOWN Fixture pod"));Assert.IsTrue(far.Hanging,"a pod sixty metres off came down too");
            Assert.Less(hero.Health.integrity,5000,"a pod came down on a hero and did nothing");
            Assert.Greater(Flat(hero.transform.position,pod.transform.position),1.2f,"a hero is left standing inside the fallen pod");
            // Behind the pod, the fan is stopped; in the open beside it, it lands.
            Stand(thane,At(0,.1f,0));Put(hero,At(0,.05f,6.6f));Put(partner,At(3.6f,.05f,5));Physics.SyncTransforms();yield return Wait(.3f,hero,partner);
            Assert.IsTrue(boss.Perform("ThornFan"),"fixture: the fan");yield return null;
            var fan=Telegraph.Live.FirstOrDefault(t=>t!=null&&t.owner==boss&&!t.Released&&t.Shape==Telegraph.Kind.Cone);Assert.IsNotNull(fan,"the fan marks nothing");
            Assert.IsTrue(fan.Contains(hero.transform.position)&&fan.Contains(partner.transform.position),"fixture: both stand in the fan");
            yield return Until(()=>!boss.Busy,4,hero,partner);
            Assert.AreEqual(5000,hero.Health.integrity,.01f,"the fan went through a fallen pod: it is cover");
            Assert.Less(partner.Health.integrity,5000,"the fan missed a hero standing in it in the open");
        }
        [UnityTest] public IEnumerator TheSeedthaneIsDrivenOffAndLeavesByItsWayOut()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-6));
            var way=new GameObject("Fixture way out").AddComponent<BossExit>();way.boss="Seedthane";way.transform.position=At(40,0,0);
            var thane=Enemy("Seedthane",At(0,.1f,0));yield return Frames(3);var body=thane.transform;
            Assert.AreEqual(BossController.Ending.Leaves,BossController.EndingOf("Seedthane"));
            // A fight ends with its legs gone, where nothing holds the bar (D375): this one is ended from full, so its held line is set aside.
            thane.Health.Holds=null;cues.Clear();Kill(thane.Health,hero);
            Assert.IsTrue(Set("bossdown.Seedthane"),"driven off, it isn't counted as beaten");Assert.IsTrue(Said("BOSS_DOWN Seedthane"));Assert.IsTrue(Said("BOSS_LEAVES Seedthane"),"at nothing it lay down where it stood");
            Assert.IsFalse(cues.Any(c=>c.key=="explosionCrunch_000"&&c.emitter==body),"it went with a boss's explosion");
            Assert.IsFalse(thane.GetComponent<BossParts>().Parts.Any(p=>p.Targetable),"its legs can be targeted as it leaves");
            float x=body.position.x;yield return Wait(2.2f,hero);
            Assert.IsTrue(thane!=null,"it vanished before it could be seen to go");Assert.Greater(body.position.x,x+4,"it didn't make for its way out");
            yield return Wait(2.2f,hero);Assert.IsTrue(thane==null,"it never left");
        }

        // ------------------------------------------------------------------ the Floodcrown
        [UnityTest] public IEnumerator TheFloodHoldsItUntilItsGatesBreakAndEachGateDrainsAThirdAndTakesASixth()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,10));
            var basin=Flood();var gates=Gates();yield return Frames(2);
            var crown=Enemy("Floodcrown",At(0,.1f,0));yield return Frames(3);var boss=crown.GetComponent<BossController>();var body=crown.Health;float whole=body.maximum;
            // The gates stand at a share of its own integrity, shut to blows, and the water is told how the arena stands.
            foreach(var gate in gates)Assert.AreEqual(whole*BossController.FloodcrownTune.GateIntegrity,gate.Health.maximum,1,"a gate's integrity isn't the fight's to say");
            Assert.AreEqual(0,Kill(gates[0].Health,hero),.01f,"a gate can be broken outside its openings");
            Assert.AreEqual((1f,0f),basin.told.First(),"the water isn't told the flood stands as the fight begins");
            Assert.IsTrue(boss.Flooded);StringAssert.Contains("GATES",boss.StatusText,"its bar doesn't say what holds it up");
            Assert.AreEqual(50,Hit(body,hero),1,"while the flood holds, a blow on it isn't halved");
            float third=BossController.FloodcrownTune.DrainSeconds;
            // Its wading charge ends with its own wave over it: open, and every gate with it.
            for(int n=0;n<3;n++)
            {
                Stand(crown,At(0,.1f,0));Put(hero,At(0,.05f,10));log.Clear();yield return Wait(.2f,hero);
                Assert.IsTrue(boss.Perform(n==1?"GateSlam":"WadingCharge"),"fixture: its move");
                FloodGate target;
                if(n==1)
                {
                    // The slam's own gate stands open after its wave, and only that one.
                    yield return Until(()=>Said("FLOODCROWN_GATE_OPEN"),4,hero);Assert.IsTrue(Said("FLOODCROWN_GATE_OPEN"),"a slam opened no gate");
                    int opened=int.Parse(log.First(l=>l.StartsWith("FLOODCROWN_GATE_OPEN")).Split(' ')[1]);target=gates.Single(g=>g.index==opened);Assert.IsTrue(target.Holding,"it slammed a gate that was already down");
                    Assert.IsTrue(Telegraph.Live.Any(t=>t!=null&&t.owner==boss&&t.Shape==Telegraph.Kind.Line&&Flat(t.Origin,target.transform.position)<.5f),"the wave's lane doesn't start at the gate it came through");
                    yield return Frames(2);
                    foreach(var gate in gates.Where(g=>g.Holding))Assert.AreEqual(gate==target,gate.Health.Damageable,"after a slam only the gate the wave came through stands open");
                }
                else
                {
                    yield return Until(()=>Said("FLOODCROWN_SWAMPED"),6,hero);Assert.IsTrue(Said("FLOODCROWN_SWAMPED"),"its wading charge ended with no opening");
                    yield return Frames(2);Assert.IsTrue(body.Broken);
                    Assert.IsTrue(gates.Where(g=>g.Holding).All(g=>g.Health.Damageable),"in its opening the gates are still shut");
                    target=gates.First(g=>g.Holding);
                }
                float bar=body.integrity;int standing=FloodGate.Standing;
                // The last gate goes with the fight awake, as it would: a boss held for a scene crosses its line without a shift.
                if(standing==1)crown.Passive=false;
                Assert.Greater(Kill(target.Health,hero),0,"an open gate took no blow");yield return Frames(2);
                Assert.AreEqual(standing-1,FloodGate.Standing);Assert.IsTrue(Said("FLOODCROWN_GATE_DOWN standing="+(standing-1)));
                Assert.AreEqual((standing-1)/3f,basin.told.Last().fraction,.001f,"a broken gate didn't take a third of the water");Assert.AreEqual(third,basin.told.Last().seconds,.001f);
                if(standing>1)Assert.AreEqual(bar-whole/6,body.integrity,2,"a gate should take a sixth of its bar with it");
                else{Assert.Less(body.integrity,whole*.5f,"the last gate doesn't carry it to its second phase");Assert.Greater(body.integrity,whole*.49f);}
                yield return Until(()=>!boss.Busy&&!body.Broken,9,hero);yield return Frames(2);
                Assert.IsFalse(gates.Any(g=>g.Holding&&g.Health.Damageable),"with its opening over a gate still stands open");
            }
            // Drained, it shifts: nothing braces it now.
            yield return Until(()=>Said("BOSS_SHIFT Floodcrown 2"),8,hero);Assert.IsTrue(Said("BOSS_SHIFT Floodcrown 2"),"with the basin drained it never shifted");
            Assert.IsTrue(Said("FLOODCROWN_DRAINS"));Assert.IsFalse(boss.Flooded);Assert.AreEqual("",boss.StatusText);
            crown.Passive=true;yield return Until(()=>!boss.Busy&&!boss.Shifting&&!body.Broken,9,hero);yield return Wait(.3f,hero);
            Assert.AreEqual(100,Hit(body,hero),1,"drained, it is still braced");
        }
        [UnityTest] public IEnumerator AtHalfWithGatesStandingItThrowsThemOpenItselfAndTheWaterGoes()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,8));
            var basin=Flood();var gates=Gates();yield return Frames(2);
            var crown=Enemy("Floodcrown",At(0,.1f,0));yield return Frames(3);var boss=crown.GetComponent<BossController>();var body=crown.Health;
            Hit(body,hero,1);Assert.IsTrue(boss.Engaged,"fixture: the fight is on");
            crown.Passive=false;body.integrity=body.maximum*.48f;
            yield return Until(()=>Said("BOSS_SHIFT Floodcrown 2"),10,hero);Assert.IsTrue(Said("BOSS_SHIFT Floodcrown 2"),"under half with its gates standing, it never shifted");
            Assert.IsTrue(Said("FLOODCROWN_DRAINS"),"its second phase began in a flooded basin");
            Assert.AreEqual(0,FloodGate.Standing,"its second phase began with gates standing");Assert.IsFalse(boss.Flooded);
            Assert.AreEqual(0,basin.told.Last().fraction,.001f,"the water wasn't told to go");
            Assert.IsFalse(gates.Any(g=>Set(g.defeatedFlag)),"gates it opened itself are counted as broken by the party");
        }
        [UnityTest] public IEnumerator ItsWaveBreaksUnderAHeroOnDryGroundAndAChargeRunAgroundLeavesItStuck()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();
            var basin=Flood();var dry=At(0,0,8);basin.dry=point=>Flat(point,dry)<2.5f;
            var gate=Gate("north",0,At(0,0,16));yield return Frames(2);
            var crown=Enemy("Floodcrown",At(12,.1f,-6));yield return Frames(3);var boss=crown.GetComponent<BossController>();var body=crown.Health;
            // The lead stands on the dry place, the partner in the water further down the same lane.
            Put(hero,At(0,.05f,8));Put(partner,At(0,.05f,2));
            Assert.IsTrue(boss.Perform("GateSlam"),"fixture: the slam");yield return null;
            var lane=Telegraph.Live.FirstOrDefault(t=>t!=null&&t.owner==boss&&t.Shape==Telegraph.Kind.Line);Assert.IsNotNull(lane,"the wave marks no lane");
            Assert.IsTrue(lane.Contains(hero.transform.position)&&lane.Contains(partner.transform.position),"fixture: both stand in the wave's lane");
            yield return Until(()=>!boss.Busy,7,hero,partner);
            Assert.AreEqual(5000,hero.Health.integrity,.01f,"the wave struck a hero standing out of the water");
            Assert.Less(partner.Health.integrity,5000,"the wave passed a hero standing in the water in its lane");
            // A block of ground in its lane, behind where the hero stood: aground, and stuck for a long opening with the gates open.
            Block(At(0,.6f,14),new Vector3(7,1.2f,6));Stand(crown,At(0,.1f,0));Put(hero,At(0,.05f,7.5f));Put(partner,At(-50,.05f,-50));log.Clear();yield return Wait(.2f,hero);
            Assert.IsTrue(boss.Perform("WadingCharge"),"fixture: the charge");
            yield return Until(()=>Said("FLOODCROWN_AGROUND")||Said("FLOODCROWN_SWAMPED"),6,hero);
            Assert.IsTrue(Said("FLOODCROWN_AGROUND"),"a charge that met rising ground ran on: "+string.Join(", ",log));
            Assert.Less(crown.transform.position.z,11,"it climbed what it should have grounded on");
            Assert.IsTrue(body.Broken);Assert.That(body.BrokenUntil-Time.time,Is.InRange(4,4.55f),"aground, it is stuck for four and a half seconds");
            yield return Frames(2);Assert.IsTrue(gate.Health.Damageable,"aground, its gates are still shut");
        }
        [UnityTest] public IEnumerator DrainedItsChargesLeavePuddlesThatSlowAndAreNoDanger()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,12));
            var crown=Enemy("Floodcrown",At(0,.1f,0));yield return Frames(3);var boss=crown.GetComponent<BossController>();
            crown.Health.integrity=crown.Health.maximum*.4f;yield return Frames(2);Assert.AreEqual(2,boss.Phase,"fixture: its second phase");
            Assert.IsFalse(boss.Flooded);Assert.IsTrue(boss.Perform("CrownCharge"),"fixture: the charge");
            int lanes=0;var seen=new HashSet<Telegraph>();
            yield return Until(()=>{foreach(var mark in Telegraph.Live)if(mark!=null&&mark.owner==boss&&mark.Shape==Telegraph.Kind.Line&&seen.Add(mark))lanes++;return !boss.Busy;},10,hero);
            Assert.AreEqual(2,lanes,"drained, it should charge twice");
            Assert.GreaterOrEqual(LingeringArea.All.Count,3,"its charges left no puddles behind them");
            foreach(var puddle in LingeringArea.All)
            {
                Assert.AreEqual(AreaEffect.Slow,puddle.Effect);
                var edge=Telegraph.Live.FirstOrDefault(t=>t!=null&&ReferenceEquals(t.owner,puddle));Assert.IsNotNull(edge,"a puddle has no edge");Assert.IsTrue(Faint(edge),"a puddle is painted as a danger");
            }
            Assert.IsFalse(Telegraph.Live.Any(t=>t!=null&&t.owner==boss&&!t.Released),"a puddle's marking is the boss's: the partner would run from it");
            var wet=LingeringArea.All[LingeringArea.All.Count-1];Put(hero,wet.transform.position+Vector3.up*.05f);yield return Wait(.5f,hero);
            Assert.AreEqual(BossController.FloodcrownTune.PuddlePace,hero.Health.Pace,.01f,"a hero in a puddle keeps his pace");
            Assert.AreEqual(5000,hero.Health.integrity,.01f,"a puddle hurt the hero standing in it");Assert.IsFalse(LateSkills.InDanger(hero));
            yield return Until(()=>LingeringArea.All.Count==0,8,hero);Assert.IsEmpty(LingeringArea.All,"the puddles never dry");
        }
        [UnityTest] public IEnumerator TheFloodcrownGoesDownTheDrainWithTheLastOfTheWater()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-8));
            var basin=Flood();var gates=Gates();yield return Frames(2);
            var drain=new GameObject("Fixture drain").AddComponent<BossExit>();drain.boss="Floodcrown";drain.sinks=true;drain.transform.position=At(16,0,0);
            var crown=Enemy("Floodcrown",At(0,.1f,0));yield return Frames(3);var body=crown.transform;
            LingeringArea.Lay(At(4,0,4),2,30,AreaEffect.Slow,.6f,0,crown.Health,DamageType.Kinetic);yield return null;
            cues.Clear();Kill(crown.Health,hero);
            Assert.AreEqual(0,crown.Health.integrity,"fixture: the kill, through the brace");
            Assert.IsTrue(Set("bossdown.Floodcrown"));Assert.IsTrue(Said("BOSS_LEAVES Floodcrown"),"at nothing it lay down where it stood");
            Assert.IsFalse(cues.Any(c=>c.key=="explosionCrunch_000"&&c.emitter==body),"it went with a boss's explosion");
            Assert.AreEqual(0,FloodGate.Standing,"it has gone and its gates still hold the water");Assert.AreEqual(0,basin.told.Last().fraction,.001f,"it has gone and the water wasn't told to follow");
            yield return Frames(2);Assert.IsEmpty(LingeringArea.All,"its puddles outlast the fight");
            float x=body.position.x;yield return Wait(2.2f,hero);
            Assert.IsTrue(crown!=null,"it vanished before it could be seen to go");Assert.Greater(body.position.x,x+4,"it didn't make for the drain");
            yield return Wait(2.2f,hero);Assert.IsTrue(crown==null,"it never left");
        }

        /// <summary>From the basin's door the gates are in a shot's reach, and the Seedthane stands on show across an open bank: neither may be worn down, or even locked onto,
        /// before its fight has begun. A gate that names its boss is shut from the moment the zone loads; a boss on show has no parts yet.</summary>
        [UnityTest] public IEnumerator AGateOrALegCanBeNeitherStruckNorTargetedBeforeItsBossIsAwake()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,10));
            Flood();var gates=Gates();foreach(var gate in gates)gate.settledBy="Floodcrown";
            yield return Frames(3);
            foreach(var gate in gates)
            {
                Assert.IsFalse(gate.Health.Targetable,"with no Floodcrown awake a gate can be targeted");
                Assert.AreEqual(0,Kill(gate.Health,hero),.01f,"with no Floodcrown awake a gate can be struck");Assert.IsTrue(gate.Holding);
            }
            // Both on show, as a door's preview holds them (Spawner.Prepare): still nothing to hit.
            var crown=Enemy("Floodcrown",At(0,.1f,0),false);var shown=crown.gameObject.AddComponent<DormantEnemy>();shown.Hold(At(0,0,-20),6);
            var thane=Enemy("Seedthane",At(30,.1f,0),false);var perched=thane.gameObject.AddComponent<DormantEnemy>();perched.Hold(At(30,0,-20),6);
            yield return Wait(1,hero);
            Assert.IsFalse(gates.Any(g=>g.Health.Targetable),"with the Floodcrown on show its gates can be targeted");
            Assert.AreEqual(0,Kill(gates[1].Health,hero),.01f,"with the Floodcrown on show a gate can be struck");
            Assert.IsNull(thane.GetComponent<BossParts>(),"a Seedthane on show has legs to break");
            foreach(var body in crown.GetComponentsInChildren<Health>().Concat(thane.GetComponentsInChildren<Health>()))Assert.IsFalse(body.Targetable,body.id+" can be targeted on a boss that is on show");
            // Awake: the Seedthane's legs are there to break, and the gates open with the Floodcrown's openings and not before.
            shown.Activate();perched.Activate();yield return Frames(3);thane.Passive=true;crown.Passive=true;yield return Frames(2);
            var legs=thane.GetComponent<BossParts>();Assert.IsNotNull(legs,"awake, the Seedthane has no legs to break");
            Assert.IsTrue(legs.Parts.All(p=>p.Targetable),"awake and planted, its legs can't be targeted");
            Assert.IsFalse(gates.Any(g=>g.Health.Targetable),"awake and not open, the Floodcrown's gates can be targeted");
            Assert.IsTrue(crown.GetComponent<BossController>().Perform("WadingCharge"),"fixture: the charge");
            yield return Until(()=>Said("FLOODCROWN_SWAMPED")||Said("FLOODCROWN_AGROUND"),7,hero);yield return Frames(2);
            Assert.IsTrue(gates.All(g=>g.Health.Targetable),"in its opening its gates still can't be targeted");
            Assert.Greater(Kill(gates[0].Health,hero),0,"in its opening a gate took no blow");
        }

        /// <summary>A body's capsule can't stand shorter than it is wide: Unity stretches it round its middle, and a boss wide enough for that hung in the air over its own floor (the
        /// Floodcrown half a metre, the Seedthane a tenth, the Rotbloom 1.8 m, whose rot-pods then found no floor near enough and were seeded inside it). Every boss fought on foot since
        /// chapter 2, set down on a floor, comes to rest on it.</summary>
        [UnityTest] public IEnumerator EveryBossSinceTheSecondChapterStandsOnTheFloorItFightsOn()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(hero,At(-40,.05f,-40));Put(partner,At(-44,.05f,-40));
            // Every later boss is made here, and one whose chapter hasn't staged its track yet logs MUSIC_MISSING as it wakes: that is the music tests' to say
            // (EachBossFightPlaysTheBossTrack), not a fault in where it stands. Anything thrown still fails this.
            LogAssert.ignoreFailingMessages=true;
            var failures=new List<string>();int tried=0;
            foreach(string id in ContentRegistry.LaterBosses)
            {
                if(!BossController.Registered(id))continue;
                ContentRegistry.BossSheet sheet;try{sheet=ContentRegistry.Boss(id);}catch(System.InvalidOperationException){continue;}
                if(sheet.flight)continue;
                tried++;var boss=Enemy(id,At(0,.1f,0));yield return Wait(.8f,hero);
                var feet=boss.GetComponent<CharacterController>();
                if(feet.height<feet.radius*2-.001f)failures.Add($"{id}'s capsule is {feet.height:0.00} m tall on a {feet.radius:0.00} m radius");
                if(boss.transform.position.y>.15f)failures.Add($"{id} rests {boss.transform.position.y:0.00} m over the floor it was set down on");
                boss.Passive=true;Object.Destroy(boss.gameObject);yield return Frames(3);
            }
            Assert.GreaterOrEqual(tried,2,"fixture: the bosses since chapter 2");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ what the two fights are sized on
        const float GateUptime=.35f,FlatOutSeconds=15;
        /// <summary>The party flat out on a body that never moves, as QaPlanCombatTests measures it: both heroes attacking, every skill as its cooldown and Charge allow.</summary>
        IEnumerator Rate(string id,System.Action<float> measured)
        {
            var at=At(0,.1f,0);var party=PartyController.Current;if(party.Active.character!="Taren")Assert.IsTrue(party.Swap(),"fixture: the swap");
            var taren=party.Active;var sela=party.members.First(m=>m!=taren);
            var enemy=Enemy(id,at);yield return Frames(3);var body=enemy.Health;
            // The body alone: its legs would list it and its gates would brace it, and those are counted apart.
            enemy.GetComponent<BossController>().enabled=false;var parts=enemy.GetComponent<BossParts>();if(parts!=null)parts.Expose(false);
            body.maximum=body.integrity=10000000;
            float reach=BossReach.Edge(enemy);var tarenAt=at+Vector3.back*reach;var selaAt=at+Vector3.back*8+Vector3.right*2;
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
        static void Sync(int level,bool prime)
        {
            var state=GameServices.Current.State;var inventory=new Inventory(state);
            foreach(var member in state.party)
            {
                member.level=level;member.skillUses?.Clear();if(!prime)continue;
                var weapon=GameCatalog.Find<TechPartDef>(member.id=="Taren"?"EdgesT3":"EmittersT3");Assert.IsNotNull(weapon,"fixture: the Prime weapon");
                inventory.Equip(member,inventory.GivePart(weapon),weapon);
            }
            PartyController.Current.RefreshStats();
        }
        /// <summary>The standing rule: an ordinary hit costs a hero 9 to 13% of the bar. Measured on the real party through its real Plating wherever the chapter's enemies are met: the
        /// nearspace at Sync 10 in the Refined parts the party arrives in (DevLoadout chapter3), the river at 10 and 11, and the Canopy at 12 in those parts and at 13 in the Prime parts it
        /// leaves in (chapter4), each at its zone's own threat, read off its ZoneDef (1, 1 and 1.1). The first numbers came to 5 to 9%, which a 5 to 15% band let through.</summary>
        [UnityTest] public IEnumerator AnOrdinaryHitCostsAboutATenthOfTheBarFromArrivalToTheChaptersEnd()
        {
            yield return Zone("Arena_Ground");Floor();var source=Enemy("Thornback",At(0,.05f,40)).Health;yield return Frames(2);
            string[] near={"SkimPass","VineLash","ClaimTether"},river={"ThornCharge","ReedBite","SporePod","MireDart","MireBurst"},canopy={"SporePod","MireDart","MireBurst","RootSlam","RootLine"};
            var places=new[]
            {
                (place:"XylosNearspace",sync:10,prime:false,expected:1f,hits:near),
                (place:"BraidedRiver",sync:10,prime:false,expected:1f,hits:river),(place:"BraidedRiver",sync:11,prime:false,expected:1f,hits:river),
                (place:"WalkingCanopy",sync:12,prime:false,expected:1.1f,hits:canopy),(place:"WalkingCanopy",sync:13,prime:true,expected:1.1f,hits:canopy),
            };
            var failures=new List<string>();
            foreach(var (place,sync,prime,expected,hits) in places)
            {
                var zone=GameCatalog.Find<ZoneDef>(place);Assert.IsNotNull(zone,"fixture: "+place);float threat=zone.enemyThreat;
                Assert.AreEqual(expected,threat,.001f,$"{place} is at threat {threat}: the chapter's hits are sized for {expected}");
                if(prime){DevLoadout.Apply("chapter4");foreach(var member in GameServices.Current.State.party)member.level=sync;PartyController.Current.RefreshStats();}else Sync(sync,false);
                foreach(var hero in PartyController.Current.members)
                    foreach(string id in hits)
                    {
                        var attack=GameCatalog.Find<AttackDef>(id);Assert.IsNotNull(attack,"fixture: "+id);
                        hero.Health.integrity=hero.Health.maximum;hero.Health.InvulnerableUntil=0;
                        float dealt=hero.Health.Receive(new DamagePacket{source=source,amount=attack.damage*threat,type=attack.type}),share=dealt/hero.Health.maximum;
                        string line=$"XYLOS_HIT {place} sync={sync} {hero.character} bar={hero.Health.maximum:0} plating={hero.plating:0} {id} x{threat} dealt={dealt:0.0} share={share:P1}";
                        TestContext.WriteLine(line);Debug.Log(line);
                        if(share<LeastHit||share>MostHit)failures.Add(line);
                    }
            }
            Assert.IsEmpty(failures,"an ordinary hit should cost between "+LeastHit.ToString("P0")+" and "+MostHit.ToString("P0")+" of the bar: "+string.Join("; ",failures));
        }
        /// <summary>The rule: an ordinary hit takes 9 to 13% of the bar where it is met.</summary>
        const float LeastHit=.09f,MostHit=.13f;
        /// <summary>A boss's move takes 12 to 16% of the bar of the party that fights it.</summary>
        const float LeastBossHit=.12f,MostBossHit=.16f;
        /// <summary>A boss hits harder than the things on the road to it: each of its moves, and its plain attack, takes 12 to 16% of the bar of the party that fights it (the
        /// chapter's Refined parts at its sheet's Sync), in whichever phase it is used (the second adds <see cref="BossController.PhaseStep"/>), and more than the heaviest ordinary
        /// hit met on the way there at that Sync. Measured through Health.Receive, as the ordinary hits are. The first numbers came to 7 to 10%: the fan's old 38 is shown to fall short.</summary>
        [UnityTest] public IEnumerator EachBossMoveHitsHarderThanTheRoadToIt()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            var source=Enemy("Seedthane",At(40,.1f,40)).Health;yield return Frames(2);
            float Share(float amount){hero.Health.integrity=hero.Health.maximum;hero.Health.InvulnerableUntil=0;return hero.Health.Receive(new DamagePacket{source=source,amount=amount,type=DamageType.Kinetic,weight=HitWeight.Heavy})/hero.Health.maximum;}
            var failures=new List<string>();var shares=new List<string>();
            void Fight(string boss,int sync,string road,string[] roadHits,params (string move,float damage,int[] phases)[] moves)
            {
                Sync(sync,false);float threat=GameCatalog.Find<ZoneDef>(road).enemyThreat,heaviest=0;string worst="";
                foreach(string id in roadHits){float s=Share(GameCatalog.Find<AttackDef>(id).damage*threat);if(s>heaviest){heaviest=s;worst=id;}}
                var plain=GameCatalog.Find<EnemyDef>(boss).attacks[0];
                foreach(var (move,damage,phases) in moves.Append((plain.name,plain.damage,new[]{1,2})))
                    foreach(int phase in phases)
                    {
                        float s=Share(damage*(1+(phase-1)*BossController.PhaseStep));shares.Add($"{boss} {move} p{phase} {s:P1}");
                        if(s<LeastBossHit||s>MostBossHit)failures.Add($"{boss}'s {move} in phase {phase} takes {s:P1} of a Sync {sync} bar: a boss's move takes 12 to 16%");
                        if(s<=heaviest)failures.Add($"{boss}'s {move} in phase {phase} takes {s:P1}, no more than {worst} on the road to it ({heaviest:P1})");
                    }
                shares.Add($"{boss} road {worst} {heaviest:P1}");
            }
            Sync(11,false);Assert.Less(Share(38),LeastBossHit,"fixture: the Seedthane's old fan, 38, should fall short of the rule");
            Fight("Seedthane",11,"BraidedRiver",new[]{"ThornCharge","ReedBite","SporePod","MireDart","MireBurst"},
                ("thorn fan",BossController.SeedthaneTune.FanDamage,new[]{1,2}),("pod shake",BossController.SeedthaneTune.PodDamage,new[]{1,2}),("stamp",BossController.SeedthaneTune.StampDamage,new[]{2}));
            Fight("Floodcrown",12,"WalkingCanopy",new[]{"SporePod","MireDart","MireBurst","RootSlam","RootLine"},
                ("antler sweep",BossController.FloodcrownTune.SweepDamage,new[]{1,2}),("gate wave",BossController.FloodcrownTune.WaveDamage,new[]{1}),("charge",BossController.FloodcrownTune.ChargeDamage,new[]{1,2}),
                ("stomp",BossController.FloodcrownTune.StompDamage,new[]{2}),("stomp ring",BossController.FloodcrownTune.RingDamage,new[]{2}));
            TestContext.WriteLine("XYLOS_BOSS_SHARES "+string.Join("; ",shares));
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>The plan's lengths, on today's build and nothing assumed but the driver's share, for the party the chapter's loadout arrives with (DevLoadout chapter3: Refined parts in
        /// every slot), as QaPlanCombatTests now measures every boss since chapter 2: the Seedthane a minute to a minute and a half at Sync 11, the Floodcrown about three minutes at 12. The
        /// Floodcrown's first half is its three gates, which can be hit only in its openings (about a third of the time), and its second its own bar. And the two integrities are pinned to
        /// the builder: a rebuild writes whatever its source says.</summary>
        [UnityTest] public IEnumerator TheTwoFightsLastWhatThePlanAsksAtThePartysMeasuredRate()
        {
            string source=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"_Project/Scripts/Editor/World/XylosBuilder.Enemies.cs"));
            float thane=GameCatalog.Find<EnemyDef>("Seedthane").integrity,crown=GameCatalog.Find<EnemyDef>("Floodcrown").integrity;
            StringAssert.Contains($"e.id=\"Seedthane\";",source);StringAssert.Contains($"e.integrity={thane:0};e.breakThreshold=650;",source,"a rebuild would put the Seedthane's old integrity back");
            StringAssert.Contains($"e.integrity={crown:0};e.breakThreshold=850;",source,"a rebuild would put the Floodcrown's old integrity back");
            yield return Zone("Arena_Ground");Floor();
            float rate=0;Sync(11,false);yield return Rate("Seedthane",r=>rate=r);
            // The Seedthane's share of this rate is the chain's: 44,000 fell in 82.3 s (nov13b, November 13) against the 1,060 a second measured here, which is .5. (Its sheet's
            // .36 is the same fight against the shared test's rate, which stands the heroes differently and reads 1,498.) The Floodcrown's assumed .3 held: 157.8 and 164.9 s, .31 to .33.
            // .56 since D362 (November 22): this test read 1,174 a second that morning (d355-chapters), and 39,000 fell in 59.1 s in today's scout (nov22-recon-e s20), which is .562.
            const float thaneShare=.56f;float seconds=thane/(rate*thaneShare);
            TestContext.WriteLine($"XYLOS_RATE Seedthane sync=11 refined rate={rate:0}/s atDriverShare={seconds:0}s");Debug.Log($"XYLOS_RATE Seedthane rate={rate:0} seconds={seconds:0}");
            Assert.That(seconds,Is.InRange(60,90),$"the Seedthane's {thane:0} integrity at {rate:0} a second flat out and the driver's {thaneShare:P0} of it lasts {seconds:0} s: the plan asks 60 to 90");
            Sync(12,false);yield return Rate("Floodcrown",r=>rate=r);
            // Its share of this rate is the chain's too: 62,000 fell in 101.9 s, which these two terms give at .57 of the 1,080 a second measured here.
            // .77 since D362 (November 22): at .57 these terms gave 156 s for 88,000 at the 1,003 a second read that morning (d355-chapters), and today's scouts felled it in 98.9 to
            // 114.9 s (nov22-recon-e s21 and s21b, nov22-recon-f21 s21, nov22-recon-h-21): .77 at the longest.
            const float crownShare=.77f;
            float gates=3*crown*BossController.FloodcrownTune.GateIntegrity/(rate*crownShare*GateUptime),bar=crown*.5f/(rate*crownShare);seconds=gates+bar;
            TestContext.WriteLine($"XYLOS_RATE Floodcrown sync=12 refined rate={rate:0}/s gates={gates:0}s bar={bar:0}s total={seconds:0}s");Debug.Log($"XYLOS_RATE Floodcrown rate={rate:0} gates={gates:0} bar={bar:0} seconds={seconds:0}");
            Assert.That(seconds,Is.InRange(125,240),$"the Floodcrown's {crown:0} integrity at {rate:0} a second flat out lasts {gates:0} s of gates and {bar:0} s of bar, {seconds:0} s: the plan asks two to five minutes, about two and a half");
        }
    }
}
