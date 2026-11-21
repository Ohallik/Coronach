using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 9's town, the Meridian Crawl (docs/maps/MeridianCrawl.md; a town on treads that has never stopped). The town as it is first walked: its services, its people at
    /// their places, its ways out locked on what opens them, each prompt offered from where its way ends; the town moving past a deck that stays still; tread-hall four rolling in once
    /// it has been ridden in, the lip shut at every moment of that until it has coupled and Dorrow has said so; who is where as the chapter goes on; the three side-story points.</summary>
    public sealed class MeridianCrawlTests
    {
        const string Zone="MeridianCrawl";
        // The builder's numbers (HomeBuilder.Crawl), copied: the tests can't see the editor assembly.
        static readonly Vector2 Arrival=new(2,1.2f),Pad=new(2,6),Cradle=new(-9,5.2f),TreadShop=new(11.5f,5.4f),Stall=new(-37,1.4f),StallHand=new(-37,-.6f),TamAt=new(-41,-6.8f),TamHand=new(-41,-5),
            DorrowAt=new(-79,.6f),DorrowHand=new(-77,.2f),ParcelHand=new(12.5f,-5.6f),MiraHand=new(46.5f,6.4f),CageHand=new(58.4f,5.4f),GapSpawn=new(55.5f,-1),DorrowLip=new(58.4f,.8f),
            LoHand=new(68.6f,-3);
        const float Raised=2.4f;
        readonly List<string> log=new();
        void Heard(string text,string stack,LogType type){if(text.StartsWith("COUPLE")||text.StartsWith("AFTER_TALK"))log.Add(text);}
        bool Said(string start)=>log.Any(l=>l.StartsWith(start));
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter9");log.Clear();Application.logMessageReceived+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what this zone writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Crawl(ContentRegistry.Book book)
        {
            // The drop cage to the run waits on Dorrow's ask at the helm (DorrowFirst); the pad's launch home waits on tread-hall four, which is his to say is in (DorrowTreadHome).
            book.Lock("home.crawlAsked","Dorrow");
            book.Lock("home.treadRecovered","Dorrow");
        }

        // ------------------------------------------------------------------ fixtures
        static void Set(params string[] keys){foreach(var key in keys)GameServices.Current.Flags.SetBool(key,true);}
        static bool Flag(string key)=>GameServices.Current.Flags.GetBool(key);
        static IEnumerator Load(string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(Zone,spawn);float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,Zone+" never finished loading");Assert.AreEqual(Zone,SceneManager.GetActiveScene().name);
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;}
            deadline=Time.unscaledTime+10;while(DialogueSystem.Current!=null&&!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.5f);
        }
        static Vector3 Ground(Vector2 p)
        {
            var hits=Physics.RaycastAll(new Vector3(p.x,40,p.y),Vector3.down,90,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<CombatActor>()==null&&!h.collider.isTrigger).OrderBy(h=>h.distance).ToArray();
            Assert.IsNotEmpty(hits,"fixture: there is ground at "+p);return hits[0].point;
        }
        static void Put(CombatActor actor,Vector2 p)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.SetPositionAndRotation(Ground(p)+Vector3.up*.05f,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;Physics.SyncTransforms();
        }
        static IEnumerator Wait(float seconds){float until=Time.time+seconds;while(Time.time<until)yield return null;}
        static IEnumerator Until(System.Func<bool> done,float seconds){float until=Time.unscaledTime+seconds;while(!done()&&Time.unscaledTime<until)yield return null;}
        static Vector2 Plan(Vector3 p)=>new(p.x,p.z);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        /// <summary>What the lead is offered standing at a plan point.</summary>
        static InteractionPrompt OfferedAt(Vector2 p){var lead=PartyController.Current.Active;Put(lead,p);PromptService.FindInteraction(lead.transform.position);return PromptService.Interaction;}
        static bool Reach(Vector2 from,Vector2 to)
        {
            var path=new NavMeshPath();
            return NavMesh.SamplePosition(Ground(from),out var a,1.5f,NavMesh.AllAreas)&&NavMesh.SamplePosition(Ground(to),out var b,1.5f,NavMesh.AllAreas)&&
                NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        }

        [UnityTest] public IEnumerator TheTownOpensOnItsYardWithItsServicesAndEachIsOfferedFromWhereItsWayEnds()
        {
            yield return Load();
            Assert.AreEqual(ZoneKind.GroundSafe,ZoneController.Current.definition.kind,"the Crawl isn't a safe town");
            Assert.Less(Vector2.Distance(Plan(PartyController.Current.Active.transform.position),Arrival),3,"the party didn't arrive at the yard's spawn");
            foreach(var id in new[]{"Arrival","Yard","Gap"})Assert.IsTrue(All<SpawnPoint>().Any(s=>s.id==id),"no spawn "+id);
            var shop=All<Shop>().Single();Assert.AreEqual("Trade — The Lashed Stall",shop.prompt);Assert.IsNotNull(shop.inventory,"the Lashed Stall has no stock");
            Assert.AreSame(shop,OfferedAt(StallHand),"from the street in front of the Lashed Stall it isn't offered");
            var bench=All<Bench>().First(b=>b.prompt=="Fabricate — The Tread Shop");Assert.AreSame(bench,OfferedAt(new Vector2(TreadShop.x,3.6f)),"from the way's end at the tread shop its bench isn't offered");
            var bay=All<RepairBay>().Single();Assert.AreEqual("Yard",bay.saveSpawn,"the oil shop's cradle doesn't save at the yard");Assert.IsFalse(bay.free,"the town's cradle is free");
            Assert.AreSame(bay,OfferedAt(new Vector2(Cradle.x,3.4f)),"from the way's end at the oil shop its cradle isn't offered");
            // The ways out, each locked on what opens it, its refusal naming who.
            var run=All<WarpBeacon>().Single(w=>w.scene=="Terminator");Assert.AreEqual("home.crawlAsked",run.requiredFlag);StringAssert.Contains("Dorrow",run.lockedPrompt);
            Assert.AreSame(run,OfferedAt(CageHand),"from the tail's way end the drop cage isn't offered");
            var home=All<WarpBeacon>().Single(w=>w.scene=="DecksLeaving");Assert.AreEqual("home.treadRecovered",home.requiredFlag);StringAssert.Contains("Dorrow",home.lockedPrompt);
            Assert.AreSame(home,OfferedAt(new Vector2(Pad.x,3)),"from the pad's way end the launch isn't offered");
            Assert.IsNotNull(Object.FindFirstObjectByType<GroundNavigation>(),"the Crawl has no navigation");
            foreach(var p in new[]{DorrowHand,StallHand,TamHand,new Vector2(Pad.x,3),ParcelHand,MiraHand,CageHand,GapSpawn})Assert.IsTrue(Reach(Arrival,p),"no path from the arrival to "+p);
        }

        [UnityTest] public IEnumerator DorrowIsAtTheHelmAndTamOnTheMarketStreetAndEachIsSpokenToFromTheirWaysEnd()
        {
            yield return Load();
            var dorrow=All<Npc>().Single(n=>n.speaker=="Dorrow");Assert.Less(Vector2.Distance(Plan(dorrow.transform.position),DorrowAt),.6f,"Dorrow isn't at the helm");
            Assert.AreEqual(Raised,dorrow.transform.position.y,.3f,"Dorrow isn't on the helm platform");Assert.AreSame(dorrow,OfferedAt(DorrowHand),"from the helm's way end Dorrow isn't offered");
            var tam=All<Npc>().Single(n=>n.speaker=="Tam");Assert.Less(Vector2.Distance(Plan(tam.transform.position),TamAt),.6f,"Tam isn't at the market's south rail");Assert.AreSame(tam,OfferedAt(TamHand),"Tam isn't offered from the street");
            Assert.IsFalse(All<Transform>().Any(t=>t.name=="Dorrow at the lip"),"Dorrow stands at the lip before number four is out of the dawn");
            // The town's people: in more than one coat and build, none on a way's end.
            var folk=All<Transform>().Where(t=>t.name.StartsWith("Townsperson")||t.name.StartsWith("A child")||t.name.StartsWith("A Crawler")||t.name.StartsWith("The mail runner")||t.name.StartsWith("A gardener")||t.name.StartsWith("An oiler")).ToArray();
            Assert.GreaterOrEqual(folk.Length,18,"the Crawl has fewer than eighteen people about it");
            Assert.GreaterOrEqual(folk.Select(f=>Mathf.Round(f.localScale.x*20)).Distinct().Count(),3,"the Crawlers are all one build");
            Assert.GreaterOrEqual(folk.Select(f=>f.GetComponentInChildren<SkinnedMeshRenderer>()?.sharedMaterial).Where(m=>m!=null).Distinct().Count(),4,"the Crawlers wear fewer than four coats");
            Assert.GreaterOrEqual(All<WalkLoop>().Length,5,"fewer than five Crawlers are going anywhere");
        }

        [UnityTest] public IEnumerator TheWorldGoesPastADeckThatStaysStillAndEverythingHangingSwingsTogether()
        {
            yield return Load();var motion=CrawlMotion.Current;Assert.IsNotNull(motion,"the Crawl isn't moving");
            Assert.Greater(motion.hanging.Length,40,"fewer than forty things hang and swing");Assert.Greater(motion.belts.Length,7,"the halls' treads aren't running");
            var deck=Ground(Arrival);var pivot=motion.hanging[0].pivot;var before=pivot.localRotation;var plain=motion.plain[0];var block=new MaterialPropertyBlock();plain.GetPropertyBlock(block);var st=block.GetVector("_BaseMap_ST");
            var drift=motion.drift[0].position;
            yield return Wait(1.3f);
            Assert.Greater(Quaternion.Angle(before,pivot.localRotation),.3f,"a lamp on its chain didn't swing in over a second");
            plain.GetPropertyBlock(block);Assert.AreNotEqual(st.z,block.GetVector("_BaseMap_ST").z,"the plain didn't slide under the town");
            Assert.Greater(motion.drift[0].position.x,drift.x+.5f,"the plain's stones didn't go past east");
            Assert.AreEqual(deck,Ground(Arrival),"the deck moved under the party");
            // In step: two lanterns in one hall lean the same way at once.
            var market=motion.hanging.Where(h=>h.pivot!=null&&Mathf.Approximately(h.lag,motion.hanging[0].lag)).Take(2).ToArray();
            Assert.AreEqual(motion.Swing(GameTime.Now,market[0].lag),motion.Swing(GameTime.Now,market[1].lag),.001f,"two things hanging in one hall don't swing together");
        }

        [UnityTest] public IEnumerator NumberFourRollsInOnceRiddenAndTheLipStaysShutUntilItHasCoupledAndDorrowHasSaidSo()
        {
            Set("home.crawlAsked","home.coupling1","home.coupling2","home.coupling3","home.rideDone");yield return Load("Gap");
            Assert.IsTrue(Said("COUPLES_IN"),"coming back from the run with the hall ridden in, it wasn't seen coming");
            var comes=Object.FindFirstObjectByType<CouplesIn>();var gate=All<Transform>().FirstOrDefault(t=>t.name=="The lip's gate");Assert.IsNotNull(gate,"fixture: no lip gate");
            Assert.IsTrue(All<Transform>().Any(t=>t.name=="Dorrow at the lip"),"Dorrow isn't at the lip to watch it come in");Assert.IsFalse(All<Npc>().Any(n=>n.speaker=="Dorrow"),"Dorrow is still at the helm");
            // Every frame of the roll: the lip shut. Dorrow's line is said early here, as a player who taps through it would: a gate on the flag alone opens now, onto a hall
            // still rolling in (the broken state this check is for); this one waits for the hall to couple.
            yield return Until(()=>Said("AFTER_TALK DorrowTreadHome")||DialogueSystem.Current!=null&&DialogueSystem.Current.Node=="DorrowTreadHome",6);
            Assert.IsTrue(Said("AFTER_TALK DorrowTreadHome")||DialogueSystem.Current.Node=="DorrowTreadHome","Dorrow didn't say she's home as she came in");
            Set("home.treadRecovered");Assert.Less(comes.Coupled,.9f,"fixture: the hall is already in as the talk ends");
            int frames=0;while(comes.Coupled<1&&frames<1200){Assert.IsTrue(gate.gameObject.activeInHierarchy,$"the lip opened with the hall {comes.Coupled:0.00} of the way in");frames++;yield return null;}
            Assert.AreEqual(1,comes.Coupled,.001f,"number four never coupled");Assert.IsTrue(Said("COUPLED"));Assert.Greater(frames,10,"fixture: the roll in was never watched");
            yield return Wait(.3f);
            Assert.IsFalse(gate.gameObject.activeInHierarchy,"coupled and greeted, the lip stays shut");
            Assert.IsFalse(All<WarpBeacon>().Any(w=>w.scene=="Terminator"&&w.isActiveAndEnabled),"with number four home, the drop cage still offers the run");
        }

        [UnityTest] public IEnumerator TheThreeSideStoryPointsAreOfferedOnlyAtTheirStepsAndLoOnlyOnceTheHallIsHome()
        {
            yield return Load();
            var parcel=All<DiscoveryPoint>().Single(p=>p.report=="CrawlParcel");Assert.AreEqual("ReturnAddress",parcel.requiredQuest);Assert.AreEqual(1,parcel.requiredStep);Assert.AreEqual("neve.parcelCrawl",parcel.flag);
            Assert.Less(Vector2.Distance(Plan(parcel.transform.position),ParcelHand),parcel.range,"the parcel can't be handed over from the rack's way end");
            var lot=All<DiscoveryPoint>().Single(p=>p.report=="Consignment");Assert.AreEqual("SecondInventory",lot.requiredQuest);Assert.AreEqual(0,lot.requiredStep);Assert.AreEqual("mira.lotCrawl",lot.flag);
            Assert.Less(Vector2.Distance(Plan(lot.transform.position),MiraHand),lot.range,"Mira's tag can't be read from the sky-garden's way end");
            Assert.IsFalse(All<DiscoveryPoint>().Any(p=>p.report=="PortableBench"),"Lo's bench is offered before tread-hall four is home");
            Set("home.rideDone","home.treadRecovered","home.returned");yield return Load("Gap");
            var bench=All<DiscoveryPoint>().Single(p=>p.report=="PortableBench");Assert.AreEqual("PortableBenches",bench.requiredQuest);Assert.AreEqual("bench.lo",bench.flag);
            Assert.Less(Vector2.Distance(Plan(bench.transform.position),LoHand),bench.range,"Lo's bench can't be handed over from the porch's way end");
            Assert.IsTrue(Reach(GapSpawn,LoHand),"with number four home there's no way onto its porch");
            Assert.IsFalse(All<Npc>().Any(n=>n.speaker=="Tam"),"Tam is still on the Crawl once the party has gone home");
        }
    }
}
