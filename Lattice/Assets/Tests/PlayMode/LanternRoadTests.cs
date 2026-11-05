using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>D202: chapter 2, The Lantern Road. Its four zones are wired into the world, its story runs from Tallow to
    /// Lanternwick's archive, its relays do what the story says, and its captain fights like a duelist.</summary>
    public sealed class LanternRoadTests
    {
        string heard;readonly List<string> log=new();
        void Listen(string node,string speaker,System.Action<bool> done)=>heard=node;
        void Heard(string text,string stack,LogType type){if(text.StartsWith("BOSS_")||text.StartsWith("HESSA_")||text.StartsWith("RELAY_")||text.StartsWith("FLAG_ALL"))log.Add(text);}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");heard=null;log.Clear();Npc.TalkRequested+=Listen;Application.logMessageReceived+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {Npc.TalkRequested-=Listen;Application.logMessageReceived-=Heard;GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static GameState State=>GameServices.Current.State;
        static void Flag(string key){GameServices.Current.Flags.SetBool(key,true);}
        static int Step(string quest)=>State.questSteps.TryGetValue(quest,out var step)?step:-1;
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+15;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.4f);
        }
        static string Objective()=>Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Objective").text;
        static IEnumerator Talk(Npc npc)
        {
            DialogueSystem.Current.AutoAdvance=true;npc.Interact();yield return null;
            float deadline=Time.unscaledTime+25;
            while((DialogueSystem.Current.Running||GameInput.Current.Blocked)&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(DialogueSystem.Current.Running,npc.speaker+"'s conversation never ended");
            yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Discover(DiscoveryPoint point)
        {
            DialogueSystem.Current.AutoAdvance=true;Assert.IsTrue(point.Available,point.name+" is not offered");point.Interact();yield return null;
            float deadline=Time.unscaledTime+25;
            while((DialogueSystem.Current.Running||GameInput.Current.Blocked)&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.3f);
        }

        [UnityTest] public IEnumerator EveryChapterTwoZoneLoadsWithItsWayInAndOut()
        {
            var expected=new Dictionary<string,(ZoneKind kind,string[] exits)>
            {
                ["WickReach"]=(ZoneKind.SpaceCombat,new[]{"Lanternwick","TallowApproach"}),
                ["Lanternwick"]=(ZoneKind.GroundSafe,new[]{"WickReach","LanternGorge"}),
                ["LanternGorge"]=(ZoneKind.GroundCombat,new[]{"Lanternwick","SunkenRelay"}),
                ["SunkenRelay"]=(ZoneKind.GroundCombat,new[]{"LanternGorge","Lanternwick"}),
            };
            foreach(var (zone,(kind,exits)) in expected)
            {
                yield return Zone(zone);
                Assert.AreEqual(kind,ZoneController.Current.definition.kind,zone);
                var ways=Object.FindObjectsByType<DockingPad>(FindObjectsSortMode.None).Select(d=>d.scene).Concat(Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).Select(w=>w.scene)).ToArray();
                foreach(var exit in exits)CollectionAssert.Contains(ways,exit,zone+" has no way to "+exit);
                StringAssert.DoesNotContain("Practice",Objective(),zone+" has no objective of its own");
                if(kind!=ZoneKind.SpaceCombat)Assert.IsNotNull(Object.FindFirstObjectByType<GroundNavigation>(),zone+" has no navigation");
                Assert.IsNotNull(PartyController.Current.Active,zone+" spawned no party");
            }
            yield return Zone("TallowApproach");
            Assert.IsTrue(Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).Any(w=>w.scene=="WickReach"),"Tallow's approach has no warp to the Wick Reach");
        }

        [UnityTest] public IEnumerator AbriSendsThePartyDownTheLanternRoadAfterTheCompactsCall()
        {
            foreach(var f in new[]{"met.Keeper","sliceComplete","tallow.lineObserved","tallow.driftDiscussed","compact.hailHeard"})Flag(f);
            yield return Zone("TallowDrift");
            StringAssert.Contains("route that waits",Objective());
            var keeper=Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Single(n=>n.speaker=="Keeper");
            yield return Talk(keeper);
            Assert.AreEqual("AbriLanternTale",heard,"Abri does not tell the lantern tale after the call");
            Assert.AreEqual(0,Step("LanternRoad"),"the Lantern Road did not begin");
            Assert.IsTrue(GameServices.Current.Flags.GetBool("lantern.routeKnown"));
            yield return Zone("TallowApproach");
            var warp=Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).Single(w=>w.scene=="WickReach");
            StringAssert.Contains("Wick Reach",Objective());
            warp.Interact();float deadline=Time.unscaledTime+15;
            while(State.zone!="WickReach"&&Time.unscaledTime<deadline)yield return null;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual("WickReach",State.zone);yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(1,Step("LanternRoad"),"arriving in the Wick Reach did not advance the road");
        }

        [UnityTest] public IEnumerator TheWickRelaysOpenTheLanternGate()
        {
            Flag("lantern.routeKnown");RpgServices.Quests.Start("LanternRoad");
            yield return Zone("WickReach");
            var gate=Object.FindObjectsByType<Membrane>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(m=>m.name=="Lantern gate");
            Assert.AreEqual(1,Object.FindObjectsByType<FlagGate>(FindObjectsSortMode.None).Count(g=>g.flag=="wick.gateOpen"),"nothing opens the gate");
            Assert.IsTrue(gate.gameObject.activeInHierarchy,"the gate is open before any relay burns");
            StringAssert.Contains("Relight",Objective());
            foreach(var point in Object.FindObjectsByType<DiscoveryPoint>(FindObjectsSortMode.None).Where(p=>p.flag.StartsWith("wick.relay")).ToArray())yield return Discover(point);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(GameServices.Current.Flags.GetBool("wick.gateOpen"),"two relays did not open the gate");
            Assert.IsFalse(gate.gameObject.activeInHierarchy,"the gate still blocks the dock");
            StringAssert.Contains("Lanternwick",Objective());
        }

        [UnityTest] public IEnumerator RelightingTheGorgeRelaysWardsTheGrazersAndOpensTheRelay()
        {
            Flag("met.Aumi");RpgServices.Quests.Start("LanternRoad");
            foreach(var (kind,target) in new[]{(ObjectiveKind.Reach,"WickReach"),(ObjectiveKind.Reach,"Lanternwick"),(ObjectiveKind.TalkTo,"Aumi")})RpgServices.Quests.Report(kind,target);
            Assert.AreEqual(3,Step("LanternRoad"));
            yield return Zone("LanternGorge");
            var door=Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).Single(w=>w.scene=="SunkenRelay");
            Assert.IsFalse(GameServices.Current.Flags.GetBool(door.requiredFlag),"the relay door opens before the relays burn");
            var relays=Object.FindObjectsByType<DiscoveryPoint>(FindObjectsSortMode.None).Where(p=>p.flag.StartsWith("gorge.relay")).OrderBy(p=>p.flag).ToArray();
            Assert.AreEqual(3,relays.Length,"the gorge needs three relays");
            // A grazer beside the first relay is slowed once it burns.
            var grazer=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Gloomgrazer"),relays[0].transform.position+Vector3.right*2.5f);grazer.Passive=true;
            yield return null;Assert.LessOrEqual(grazer.Health.SlowUntil,Time.time,"the grazer is slowed before any light");
            foreach(var relay in relays)yield return Discover(relay);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(4,Step("LanternRoad"),"three relays did not complete the step");
            Assert.IsTrue(GameServices.Current.Flags.GetBool("gorge.relaysLit"));
            Assert.Greater(grazer.Health.SlowUntil,Time.time,"the relit relay does not ward the grazer");
            var lanterns=Object.FindObjectsByType<RelayLantern>(FindObjectsSortMode.None).Where(r=>r.flag.StartsWith("gorge.relay")).ToArray();
            Assert.AreEqual(3,lanterns.Length,"the gorge's relay towers are missing their lantern behaviour");
            Assert.IsTrue(lanterns.All(r=>r.Lit),"a relay stayed dark");
            StringAssert.Contains("Sunken Relay",Objective());
        }

        [UnityTest] public IEnumerator HessasDodgedChainLeavesHerOpenAndHerFinisherWaitsABeat()
        {
            yield return Zone("Arena_Ground");
            var hessa=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),new Vector3(0,.1f,0));hessa.Passive=true;
            yield return null;yield return null;yield return new WaitForSecondsRealtime(.3f);
            var boss=hessa.GetComponent<BossController>();Assert.IsNotNull(boss);Assert.AreEqual("HESSA KORL",boss.DisplayName);
            var lead=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=lead);
            void Put(CombatActor a,Vector3 p){var cc=a.GetComponent<CharacterController>();cc.enabled=false;a.transform.position=p;cc.enabled=true;a.Health.maximum=a.Health.integrity=5000;Physics.SyncTransforms();}
            // The chain thrown down a lane the heroes have stepped out of: she is overextended.
            Put(lead,new Vector3(0,.1f,8));Put(partner,new Vector3(14,.1f,-6));hessa.transform.rotation=Quaternion.LookRotation(Vector3.forward);
            Assert.IsTrue(boss.Perform("GrappleChain"));yield return null;
            Put(lead,new Vector3(6,.1f,8));
            float end=Time.unscaledTime+3;while(boss.Busy&&Time.unscaledTime<end)yield return null;
            Assert.IsTrue(log.Contains("HESSA_OPEN"),"a dodged chain left no opening");Assert.IsTrue(hessa.Health.Broken,"she is not exposed after the miss");
            yield return new WaitForSecondsRealtime(1.6f);
            // Caught in the lane, the hero is dragged in and slammed.
            Put(lead,new Vector3(0,.1f,9));float before=lead.Health.integrity;
            Assert.IsTrue(boss.Perform("GrappleChain"));end=Time.unscaledTime+3;while(boss.Busy&&Time.unscaledTime<end)yield return null;
            Assert.Less(Vector3.Distance(lead.transform.position,hessa.transform.position),6,"the chain did not drag the hero in");
            Assert.Less(lead.Health.integrity,before,"the slam did not land");
            // Hook Lance: the finishing thrust is held longer than the ones before it.
            yield return new WaitForSecondsRealtime(1f);
            var tells=new List<float>();Put(lead,new Vector3(0,.1f,2.5f));
            Assert.IsTrue(boss.Perform("HookLance"));
            float previous=0;end=Time.unscaledTime+6;
            while(boss.Busy&&Time.unscaledTime<end)
            {
                var marking=Telegraph.Live.FirstOrDefault(t=>t!=null&&t.owner==boss&&!t.Released&&t.Elapsed<.05f);
                if(marking!=null&&marking.Duration!=previous){tells.Add(marking.Duration);previous=marking.Duration;}
                yield return null;
            }
            Assert.GreaterOrEqual(tells.Count,3,"the combo marked fewer than three thrusts");
            Assert.Greater(tells.Last(),tells[tells.Count-2]+.3f,"the finisher keeps the same rhythm as the thrusts before it");
        }

        [UnityTest] public IEnumerator HessasPhaseShiftsCallHerCrew()
        {
            yield return Zone("Arena_Ground");
            var hessa=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),new Vector3(0,.1f,0));
            yield return null;yield return null;yield return new WaitForSecondsRealtime(.3f);
            var lead=PartyController.Current.Active;var cc=lead.GetComponent<CharacterController>();cc.enabled=false;lead.transform.position=new Vector3(0,.1f,10);cc.enabled=true;lead.Health.maximum=lead.Health.integrity=9000;
            hessa.Health.Receive(new DamagePacket{source=lead.Health,amount=hessa.Health.maximum*.4f,type=DamageType.Kinetic});
            float end=Time.unscaledTime+8;while(!log.Any(l=>l.StartsWith("BOSS_SHIFT HessaKorl"))&&Time.unscaledTime<end)yield return null;
            Assert.IsTrue(log.Any(l=>l.StartsWith("BOSS_SHIFT HessaKorl")),"no phase shift below two-thirds");
            Assert.IsTrue(log.Any(l=>l.StartsWith("BOSS_SUMMON HessaKorl")),"the shift called no crew");
            Assert.GreaterOrEqual(Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Count(e=>e.definition.id=="MusterScav"),2);
        }

        [UnityTest] public IEnumerator TheLanternRoadEndsAtTheArchiveWithAChartAndAHook()
        {
            RpgServices.Quests.Start("LanternRoad");
            RpgServices.Quests.Report(ObjectiveKind.Reach,"WickReach");RpgServices.Quests.Report(ObjectiveKind.Reach,"Lanternwick");RpgServices.Quests.Report(ObjectiveKind.TalkTo,"Aumi");
            for(int i=0;i<3;i++)RpgServices.Quests.Report(ObjectiveKind.Interact,"GorgeRelay");
            RpgServices.Quests.Report(ObjectiveKind.Kill,"HessaKorl");Assert.AreEqual(5,Step("LanternRoad"));
            foreach(var f in new[]{"met.Aumi","bossdown.HessaKorl","met.Pell","met.Ishen"})Flag(f);
            yield return Zone("Lanternwick");
            StringAssert.Contains("Aumi",Objective());
            var aumi=Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Single(n=>n.speaker=="Aumi");
            int xp=State.party[0].xp+State.party[0].level*100000;
            yield return Talk(aumi);
            Assert.AreEqual("AumiReturn",heard);
            Assert.IsTrue(GameServices.Current.Flags.GetBool("lanternwick.chartGiven"));
            Assert.IsTrue(GameServices.Current.Flags.GetBool("quest.LanternRoad.complete"),"the chapter's quest did not complete");
            Assert.IsTrue(GameServices.Current.Flags.GetBool("chapter2.complete"));
            Assert.Greater(State.party[0].xp+State.party[0].level*100000,xp,"no reward XP");
            StringAssert.Contains("home",Objective());
            Assert.AreEqual("AbriLanternHome",SideStories.NodeFor("Keeper",State),"Abri does not hear the homecoming");
        }

        [UnityTest] public IEnumerator LanternwicksPeopleHaveTheirOwnFacesNamesAndWares()
        {
            yield return Zone("Lanternwick");
            foreach(var id in new[]{"Aumi","Pell","Ishen","Corra","Brekk"})
            {
                var npc=Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).SingleOrDefault(n=>n.speaker==id);
                Assert.IsNotNull(npc,id+" is not in Lanternwick");
                var face=PortraitLookup.Get(id,BodyForm.Natural,PortraitEmotion.Neutral);
                Assert.IsNotNull(face,id+" has no portrait");
                Assert.AreNotEqual(face,PortraitLookup.Get(id,BodyForm.Natural,PortraitEmotion.Worried),id+"'s expressions do not change");
                StringAssert.DoesNotContain("ART_PENDING",npc.GetComponentsInChildren<Transform>().Select(t=>t.name).Aggregate("",(a,b)=>a+b),id+" has no body");
            }
            Assert.AreEqual("Keeper Aumi",DisplayNames.Speaker("Aumi"));
            var stock=GameCatalog.Find<ShopInventory>("Lanternwick");Assert.IsNotNull(stock,"Corra has no stock of her own");
            Assert.IsTrue(stock.parts.All(p=>p.tier==2),"Lanternwick sells starter parts");
        }
    }
}
