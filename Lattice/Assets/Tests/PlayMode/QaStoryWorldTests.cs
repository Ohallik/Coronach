using System.Collections;
using System.Collections.Generic;
using System.IO;
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
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The November QA pass, world and story: what the script says has to be standing in the room, the way home has to
    /// exist, a loss has to cost at most a walk from the door, and a town of lanterns has to be lit when you arrive. Every check
    /// here failed on the build it was written against.</summary>
    public sealed class QaStoryWorldTests
    {
        string heard;readonly List<string> log=new();
        void Listen(string node,string speaker,System.Action<bool> done)=>heard=node;
        void Logged(string text,string stack,LogType type){if(text.StartsWith("RELAY_LIT"))log.Add(text);}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");heard=null;log.Clear();Npc.TalkRequested+=Listen;Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {Npc.TalkRequested-=Listen;Application.logMessageReceived-=Logged;GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static GameState State=>GameServices.Current.State;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static IEnumerator Zone(string zone,string spawn="Arrival",bool quiet=true)
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+20;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=quiet;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.4f);
        }
        static string Objective()=>Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Objective").text;
        static IEnumerator ObjectiveSettles(){yield return new WaitForSecondsRealtime(.35f);}
        static void Put(CombatActor hero,Vector3 at)
        {
            var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.position=at;cc.enabled=true;Physics.SyncTransforms();
        }
        static EncounterVolume Encounter(string id)=>All<EncounterVolume>().Single(e=>e.encounterId==id);
        static DiscoveryPoint Discovery(string flag)=>All<DiscoveryPoint>().Single(d=>d.flag==flag);
        static WarpBeacon WarpTo(string scene)=>All<WarpBeacon>().Single(w=>w.scene==scene);
        static IEnumerator Finish(float seconds=25)
        {
            float deadline=Time.unscaledTime+seconds;
            while((DialogueSystem.Current.Running||GameInput.Current.Blocked)&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.2f);
        }

        // ---------------------------------------------------------------- Hessa Korl
        [UnityTest] public IEnumerator HessaUnderGuardWearsHerOwnBody()
        {
            yield return null;
            Assert.AreEqual(GameCatalog.Find<EnemyDef>("HessaKorl").prefab,GameCatalog.Find<CharacterDef>("Hessa").natural,"the captain the party beat turns into someone else when she yields");
        }
        [UnityTest] public IEnumerator HessaStandsInTheRoomForHerOwnConfrontation()
        {
            yield return Zone("SunkenRelay");
            var body=All<EnemyBrain>().FirstOrDefault(e=>e.definition.id=="HessaKorl");
            Assert.IsNotNull(body,"Taren says 'put the glass down' to an empty hall");
            Assert.IsTrue(body.GetComponent<DormantEnemy>()!=null&&body.GetComponent<DormantEnemy>().Held,"she should wait until the talk is over");
            // Walking straight in starts the conversation; it can't be walked around in a hall this wide, and she is on screen for it.
            foreach(var hero in PartyController.Current.members)Put(hero,new Vector3(8,0.2f,121));
            yield return new WaitForSecondsRealtime(1);Assert.IsFalse(DialogueSystem.Current.Running,"the talk started before the party reached her");
            foreach(var hero in PartyController.Current.members)Put(hero,new Vector3(8,0.2f,123));
            float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&!Has("relay.hessaConfronted")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Running,"crossing 8 m off the centre line walks past the captain");
            yield return new WaitForSecondsRealtime(.5f);
            var camera=Camera.main;
            foreach(var (height,part) in new[]{(0f,"feet"),(2.2f,"head")})
            {
                var view=camera.WorldToViewportPoint(body.transform.position+Vector3.up*height);
                Assert.IsTrue(view.z>0&&view.x>0&&view.x<1&&view.y>0&&view.y<1,$"her {part} is off screen during her own confrontation ({view})");
            }
            DialogueSystem.Current.AutoAdvance=true;yield return Finish();
            Assert.IsTrue(Has("relay.hessaConfronted"));
            // She wakes only then, and it is the same body.
            foreach(var hero in PartyController.Current.members)Put(hero,new Vector3(2,0.2f,127));
            deadline=Time.unscaledTime+3;var duel=Encounter("Relay_Hessa");while(!duel.Started&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(duel.Started,"the duel never began");
            Assert.AreEqual(1,All<EnemyBrain>().Count(e=>e.definition.id=="HessaKorl"),"a second Hessa appeared");
        }
        [UnityTest] public IEnumerator TheTalkCannotBeWalkedAroundAtTheHallsFarSide()
        {
            yield return Zone("SunkenRelay");
            foreach(var hero in PartyController.Current.members)Put(hero,new Vector3(-17,0.2f,123));
            float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Running,"the hall's edge walks past the captain into a dead chamber");
        }
        [UnityTest] public IEnumerator TheLiftWaitsToHearHessaOut()
        {
            Flag("relay.hessaConfronted");Flag("bossdown.HessaKorl");Flag("clear.Relay_Hessa");
            yield return Zone("SunkenRelay");
            var lift=WarpTo("Lanternwick");
            Assert.AreEqual("relay.hessaYielded",lift.requiredFlag,"the party can leave without hearing her");
            StringAssert.Contains("captain",lift.lockedPrompt);
            var person=All<Npc>().Single(n=>n.speaker=="Hessa");
            Assert.IsTrue(person.gameObject.activeInHierarchy,"she isn't at her post");
            // From across the map she stays quiet; walk up and she speaks, and the hearing is saved.
            yield return new WaitForSecondsRealtime(1.3f);Assert.IsNull(heard,"she surrenders from across the map");
            foreach(var hero in PartyController.Current.members)Put(hero,person.transform.position+new Vector3(0,.2f,-4));
            DialogueSystem.Current.AutoAdvance=true;
            float deadline=Time.unscaledTime+4;while(heard==null&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual("HessaYields",heard);yield return Finish();yield return null;
            Assert.IsTrue(GameServices.Current.Saves.Load("autosave").flags.TryGetValue("relay.hessaYielded",out var kept)&&kept,"a quit before the lift replays the scene");
        }
        [UnityTest] public IEnumerator HessaDoesNotWakeBeforeSheHasSpoken()
        {
            yield return Zone("SunkenRelay");
            var talk=Discovery("relay.hessaConfronted");talk.gameObject.SetActive(false);
            foreach(var hero in PartyController.Current.members)Put(hero,new Vector3(2,0.2f,130));
            yield return new WaitForSecondsRealtime(1);
            Assert.IsFalse(Encounter("Relay_Hessa").Started,"the duel started without the confrontation");
        }
        [UnityTest] public IEnumerator HessaYieldsAsOneBodyAndSpeaksUnasked()
        {
            Flag("relay.hessaConfronted");
            yield return Zone("SunkenRelay");
            var duel=Encounter("Relay_Hessa");var lead=PartyController.Current.Active;
            foreach(var hero in PartyController.Current.members){Put(hero,new Vector3(hero==lead?2:-2,0.2f,126));hero.Health.maximum=hero.Health.integrity=9000;}
            duel.Begin();yield return null;
            var boss=All<EnemyBrain>().Single(e=>e.definition.id=="HessaKorl");boss.Passive=true;
            var person=All<Npc>().Single(n=>n.speaker=="Hessa");
            Assert.IsFalse(person.gameObject.activeSelf,"she is standing under guard before the duel");
            var fell=boss.transform.position;var scale=boss.transform.localScale;var keptBoss=boss.gameObject;
            DialogueSystem.Current.AutoAdvance=true;
            boss.Health.Receive(new DamagePacket{source=lead.Health,amount=boss.Health.maximum*50,type=DamageType.Plasma});
            float deadline=Time.unscaledTime+8;
            while(heard==null&&Time.unscaledTime<deadline)
            {
                bool fighter=keptBoss!=null&&keptBoss.activeInHierarchy;
                Assert.IsFalse(fighter&&person.gameObject.activeInHierarchy,"two Hessas are on screen at once");
                if(fighter)Assert.AreEqual(scale,keptBoss.transform.localScale,"she shrinks away like a dead monster; she was arrested, not killed");
                yield return null;
            }
            Assert.AreEqual("HessaYields",heard,"the chapter's only post-fight scene never plays on its own");
            Assert.IsTrue(keptBoss==null,"the fighter's body is still there beside the person");
            Assert.IsTrue(person.gameObject.activeInHierarchy,"nobody stands where she fell");
            Assert.Less(Vector3.Distance(person.transform.position,fell),1.5f,"she pops up somewhere other than where she fell");
            yield return Finish();
            Assert.IsTrue(Has("relay.hessaYielded"));
        }
        [UnityTest] public IEnumerator TheObjectiveWaitsToHearHessaOutAndKnowsWhereSheIs()
        {
            Flag("relay.hessaConfronted");
            yield return Zone("SunkenRelay");
            yield return ObjectiveSettles();
            StringAssert.Contains("Stop Hessa Korl",Objective());StringAssert.Contains(" m",Objective(),"the one chapter 2 objective with no distance or direction");
            Flag("bossdown.HessaKorl");yield return ObjectiveSettles();
            StringAssert.Contains("Hessa",Objective(),"the objective walks past her yield");
            Flag("relay.hessaYielded");yield return ObjectiveSettles();
            StringAssert.Contains("lift",Objective());
        }
        [UnityTest] public IEnumerator TheGreatLanternSealClosesTheWholeMouth()
        {
            yield return Zone("SunkenRelay");
            var seal=All<Membrane>().Single(m=>m.name=="Great Lantern seal");
            seal.SetOpen(false);Physics.SyncTransforms();yield return null;
            foreach(float x in new[]{-10f,-7f,-4f,0f,4f,7f,10f})
            {
                bool blocked=Physics.Raycast(new Vector3(x,1,112.5f),Vector3.forward,out var hit,9,~0,QueryTriggerInteraction.Ignore);
                Assert.IsTrue(blocked&&hit.point.z<121,$"the closed seal leaves a gap at x={x}");
            }
            Assert.IsNotNull(seal.GetComponentInChildren<NavMeshObstacle>(true),"the companion can path through the closed seal");
            foreach(var r in seal.GetComponentsInChildren<Renderer>(true))if(r.sharedMaterial!=null)StringAssert.DoesNotContain("gullet",r.sharedMaterial.name.ToLowerInvariant(),"a relay door made of Choir tissue");
            Assert.IsTrue(seal.GetComponentsInChildren<Renderer>(true).Length>0,"nothing shows the seal is shut");
        }

        // ---------------------------------------------------------------- checkpoints
        [UnityTest] public IEnumerator EveryBossDoorIsACheckpoint()
        {
            foreach(var (zone,id,flight) in new[]{("Sorrel_Ridges","Sorrel_Burrower",false),("Hushwell","Hushwell_Bellows",false),("Gullet_Tunnel","Gullet_Cantor",true),("SunkenRelay","Relay_Hessa",false)})
            {
                yield return Zone(zone);
                var door=Encounter(id);
                Assert.IsFalse(string.IsNullOrEmpty(door.checkpointSpawn),id+" has no checkpoint: a defeat replays the zone");
                var spawn=All<SpawnPoint>().SingleOrDefault(s=>s.id==door.checkpointSpawn);
                Assert.IsNotNull(spawn,zone+" has no spawn called "+door.checkpointSpawn);
                Assert.Less(Vector3.Distance(spawn.transform.position,door.transform.position),60,id+"'s checkpoint is a long way from its door");
                if(!flight)Assert.IsTrue(NavMesh.SamplePosition(spawn.transform.position,out var near,1.5f,NavMesh.AllAreas),id+"'s checkpoint is not on walkable ground");
            }
        }
        [UnityTest] public IEnumerator EnteringABossArenaSavesAtItsDoor()
        {
            yield return Zone("Sorrel_Ridges");
            var door=Encounter("Sorrel_Burrower");
            State.spawn="Arrival";GameServices.Current.Saves.Save("autosave",State);
            foreach(var hero in PartyController.Current.members){Put(hero,new Vector3(0,.2f,164));hero.Health.integrity=hero.Health.maximum;}
            door.Begin();yield return null;
            foreach(var enemy in All<EnemyBrain>())enemy.Passive=true;
            var saved=GameServices.Current.Saves.Load("autosave");
            Assert.AreEqual(door.checkpointSpawn,saved.spawn,"a defeat here still goes back to where the zone began");
        }
        [UnityTest] public IEnumerator SorrelAndHushwellOfferARestBeforeTheirBosses()
        {
            yield return Zone("Sorrel_Ridges");
            Assert.IsTrue(All<RepairBay>().Any(r=>r.free&&Mathf.Abs(r.transform.position.z-145)<12),"no repair before the Burrower");
            yield return Zone("Hushwell");
            Assert.IsTrue(All<RepairBay>().Any(r=>r.free&&Mathf.Abs(r.transform.position.z-232)<12),"the whole of Hushwell has no repair");
        }
        [UnityTest] public IEnumerator TheCollarStudyMovesTheGulletsRespawnBesideIt()
        {
            yield return Zone("Gullet_Tunnel");
            var look=Discovery("gullet.collarObserved");
            Assert.IsFalse(string.IsNullOrEmpty(look.saveSpawn),"a Cantor loss goes back to the Gullet's mouth, 760 m away");
            Assert.IsTrue(All<SpawnPoint>().Any(s=>s.id==look.saveSpawn));
        }
        [UnityTest] public IEnumerator TheRelayCradleIsSigned()
        {
            yield return Zone("SunkenRelay");
            var cradle=All<RepairBay>().Single();
            Assert.IsTrue(All<TextMeshPro>().Any(t=>Vector3.Distance(t.transform.position,cradle.transform.position)<6&&t.text.Contains("CRADLE")),"the checkpoint is an unlabelled console off the route line");
        }

        // ---------------------------------------------------------------- the road back
        [UnityTest] public IEnumerator TheRoadBackOpensOnceTheCantorIsFree()
        {
            yield return Zone("TallowApproach");
            var home=WarpTo("Hub_CinderHalo");
            Assert.AreEqual("bossdown.Cantor",home.requiredFlag,"chapter 1's open quests are stranded for good");
            Assert.IsTrue(All<SpawnPoint>().Any(s=>s.id==home.spawn||s.id=="Halo"),"nowhere to arrive coming back from the Halo");
            string spawn=home.spawn;
            yield return Zone("Hub_CinderHalo");
            var back=WarpTo("TallowApproach");
            Assert.AreEqual("bossdown.Cantor",back.requiredFlag);
            Assert.IsTrue(All<SpawnPoint>().Any(s=>s.id==spawn),"the Halo has no arrival for the warp from Tallow");
            foreach(var f in new[]{"met.Orrin","warpkey","hushwell.nursery","bossdown.Cantor"})Flag(f);
            yield return ObjectiveSettles();
            StringAssert.Contains("Tallow",Objective(),"after the Cantor the Halo still sends you into the Gullet");
            StringAssert.DoesNotContain("Enter the Gullet",Objective());
        }

        // ---------------------------------------------------------------- Pell
        [UnityTest] public IEnumerator PellGoesToTallowAndLeavesLanternwick()
        {
            yield return Zone("TallowDrift");
            var pell=All<Npc>().SingleOrDefault(n=>n.speaker=="Pell");
            Assert.IsNotNull(pell,"Abri says the apprentice is fussing with her lamps, and nobody is");
            Assert.IsFalse(pell.gameObject.activeInHierarchy,"she is on Tallow before Aumi sends her");
            Flag("lanternwick.chartGiven");yield return null;yield return null;
            Assert.IsTrue(pell.gameObject.activeInHierarchy,"she never arrives");
            yield return null;Assert.IsTrue(Has("pell.aboard"),"reaching Tallow with the chart doesn't take her out of Lanternwick");
            Flag("pell.aboard",false);Flag("lanternwick.chartGiven",false);
            yield return Zone("Lanternwick");
            var townPell=All<Npc>().Single(n=>n.speaker=="Pell");
            Assert.IsTrue(townPell.gameObject.activeInHierarchy,"she should be in town until she leaves");
            Flag("lanternwick.chartGiven");yield return null;yield return null;
            Assert.IsTrue(townPell.gameObject.activeInHierarchy,"she should still be packing while the party is in town");
            Flag("pell.aboard");yield return null;yield return null;
            Assert.IsFalse(townPell.gameObject.activeInHierarchy,"she is in two places");
        }
        [UnityTest] public IEnumerator PellsLampIsNotLostWhenTheChartIsGiven()
        {
            yield return null;
            Flag("met.Pell");Flag("lanternwick.chartGiven");State.questSteps["LanternRoad"]=6;
            Assert.AreEqual("PellLampOffer",SideStories.NodeFor("Pell",State),"one of the ten quests silently never exists");
        }
        [UnityTest] public IEnumerator AbriKeepsUpWithTheRoad()
        {
            yield return null;
            Flag("met.Keeper");Flag("compact.hailHeard");Flag("tallow.driftDiscussed");
            State.questSteps["LanternRoad"]=3;
            Assert.AreEqual("AbriLanternWaiting",SideStories.NodeFor("Keeper",State),"mid-road she goes back to a chapter 1 line");
            Flag("lanternwick.chartGiven");Flag("chapter2.homecoming");State.questSteps["LanternRoad"]=6;
            Assert.AreEqual("AbriLanternAfter",SideStories.NodeFor("Keeper",State),"after the chapter's ending she goes back to a chapter 1 line");
        }
        [UnityTest] public IEnumerator DaceHearsAboutTheEggsBeforeTheBeacons()
        {
            yield return Zone("Sorrel_Ridges");
            Flag("met.Survivor");Flag("bossdown.Burrower");Flag("hushwell.nursery");
            Assert.IsNull(SideStories.NodeFor("Survivor",State),"the job pre-empts his reaction to the eggs");
            var dace=All<Npc>().Single(n=>n.speaker=="Survivor");
            Assert.AreEqual("sorrel.nurseryHeard",dace.finalCompletionFlag);
            Flag("sorrel.nurseryHeard");
            Assert.AreEqual("SurvivorBeaconsOffer",SideStories.NodeFor("Survivor",State),"the offer is lost");
        }
        [UnityTest] public IEnumerator EachSideStoryLeavesSomethingBehind()
        {
            yield return null;
            Flag("met.Neve");Flag("quest.NeveParcel.complete");State.questSteps["NeveParcel"]=2;
            Assert.IsNull(SideStories.NodeFor("Neve",State),"she talks about the courier landing while the drill still runs");
            Flag("bossdown.Burrower");
            Assert.AreEqual("NeveAfterParcel",SideStories.NodeFor("Neve",State));
            Flag("met.Oda");Flag("met.Orrin");Flag("quest.OdaBerth.complete");State.questSteps["OdaBerth"]=2;
            Assert.AreEqual("OdaAfterBerth",SideStories.NodeFor("Oda",State));
            Flag("met.Pell");Flag("pell.lampReturned");State.questSteps["PellsLamp"]=2;State.questSteps["LanternRoad"]=3;
            Assert.AreEqual("PellLampAfter",SideStories.NodeFor("Pell",State),"after the lamp she goes back to 'if you see a lamp out'");
        }

        // ---------------------------------------------------------------- what the world says
        [UnityTest] public IEnumerator EveryLockedWarpNamesWhatOpensIt()
        {
            foreach(var (zone,scene,word) in new[]{("Hub_CinderHalo","Sorrel_Ridges","Orrin"),("Hub_CinderHalo","Gullet_Tunnel","survey key"),("Gullet_Tunnel","TallowApproach","Cantor"),
                ("TallowApproach","WickReach","Abri"),("Lanternwick","LanternGorge","Aumi"),("Hushwell","Sorrel_Ridges","Bellows")})
            {
                yield return Zone(zone);
                var warp=All<WarpBeacon>().Where(w=>w.scene==scene&&!string.IsNullOrEmpty(w.requiredFlag)).First();
                StringAssert.Contains(word,warp.lockedPrompt,$"{zone}'s warp to {scene} doesn't say what opens it");
                StringAssert.DoesNotContain("Sorrel key",warp.lockedPrompt);
            }
        }
        [UnityTest] public IEnumerator OneSignPerWarp()
        {
            yield return Zone("TallowApproach");
            Assert.AreEqual(1,All<TextMeshPro>().Count(t=>t.text.Contains("Wick Reach")),"re-staging left copies of the warp's sign on top of each other");
        }
        [UnityTest] public IEnumerator EveryChapterTwoExitIsSigned()
        {
            foreach(var zone in new[]{"Lanternwick","LanternGorge","SunkenRelay"})
            {
                yield return Zone(zone);
                var signs=All<TextMeshPro>();
                foreach(var warp in All<WarpBeacon>())
                {
                    var at=warp.transform.position;
                    Assert.IsTrue(signs.Any(s=>{var d=s.transform.position-at;d.y=0;return d.magnitude<7;}),$"{zone}: {warp.prompt} is an unlabelled column");
                }
            }
            yield return Zone("LanternGorge");
            Assert.IsTrue(All<FlagGate>().Any(g=>g.flag=="gorge.relaysLit"),"the relay door looks the same lit or dark");
        }
        [UnityTest] public IEnumerator QuestPropsComeAndGoWithTheirQuests()
        {
            yield return Zone("LanternGorge");
            foreach(var (flag,quest) in new[]{("pell.lampFound","PellsLamp"),("unsworn.rationsFound","UnswornRations")})
            {
                var point=Discovery(flag);
                Assert.IsNotNull(point.visual,flag+": the prop is plain scenery beside an invisible prompt");
                yield return null;yield return null;
                Assert.IsFalse(point.visual.activeSelf,flag+": on show before anyone has asked for it");
                RpgServices.Quests.Start(quest);yield return null;yield return null;
                Assert.IsTrue(point.visual.activeSelf,flag+": missing while the quest wants it");
                point.Interact();yield return null;yield return null;
                Assert.IsFalse(point.visual.activeSelf,flag+": still on the floor after being picked up");
            }
            yield return Zone("SunkenRelay");
            RpgServices.Quests.Start("IshensRecord");
            foreach(var flag in new[]{"relay.plate1","relay.plate2","relay.plate3"})
            {
                var point=Discovery(flag);Assert.IsNotNull(point.visual,flag);
                yield return null;yield return null;Assert.IsTrue(point.visual.activeSelf,flag);
                Assert.IsTrue(point.visual.GetComponentInChildren<Light>(true)!=null,flag+": looks exactly like the dungeon's scatter");
                point.Interact();yield return null;yield return null;Assert.IsFalse(point.visual.activeSelf,flag);
            }
        }
        [UnityTest] public IEnumerator TheTownOfLanternsArrivesLit()
        {
            yield return Zone("Lanternwick");
            yield return new WaitForSecondsRealtime(.3f);
            var towers=All<RelayLantern>().Where(r=>r.name.StartsWith("Lantern tower")).ToArray();
            Assert.AreEqual(3,towers.Length);
            Assert.IsTrue(towers.All(t=>t.Lit),"the town famous for its lanterns arrives dark");
            Flag("met.Aumi");yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(0,log.Count,"three bursts and force-field sounds fire behind Aumi's first line");
        }
        [UnityTest] public IEnumerator TheWickRelaysAreBackdropNotWalls()
        {
            yield return Zone("WickReach");
            foreach(var relay in All<RelayLantern>())foreach(var c in relay.GetComponentsInChildren<Collider>())
                Assert.IsTrue(c.isTrigger||!c.enabled,$"{relay.name} reads as below the flight plane but costs 12 integrity to touch");
        }
        [UnityTest] public IEnumerator TheSlackLineWaitsForAbrisRequest()
        {
            yield return Zone("TallowApproach");
            var line=Discovery("tallow.lineObserved");
            Assert.IsFalse(line.Available,"reaching it first skips Abri's introduction");
            Flag("met.Keeper");Assert.IsTrue(line.Available,"Abri's job can't be done");
        }
        [UnityTest] public IEnumerator TheArchiveAnswersTheStory()
        {
            yield return Zone("Lanternwick");
            var wall=All<FlagGate>().FirstOrDefault(g=>g.flag=="bossdown.HessaKorl"&&g.targets.Any(t=>t!=null&&t.name=="Archive light wall"));
            Assert.IsNotNull(wall,"'light patterns' are spoken of in a town with no light patterns");
            yield return null;Assert.IsFalse(wall.targets[0].activeSelf,"lit before the relays' captain has fallen");
            Flag("bossdown.HessaKorl");yield return null;yield return null;Assert.IsTrue(wall.targets[0].activeSelf);
            Assert.IsTrue(All<RelayLantern>().Any(r=>r.flag=="ishen.recordRestored"),"'the archive's lit again' and nothing changes");
        }
        [UnityTest] public IEnumerator SpokenCachesSpeakOnceTheFightIsOver()
        {
            Flag("neve.cache");
            yield return Zone("Gullet_Tunnel");
            var cache=All<SalvageField>().Single(c=>c.cacheId=="Neve");
            Assert.AreEqual("CacheFound",cache.dialogueNode,"'Tell it I sent you' has no answer");
            var awake=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDart"),cache.transform.position+Vector3.right*3);yield return null;
            cache.Interact();yield return new WaitForSecondsRealtime(.4f);
            Assert.IsNull(heard,"a recorded message freezes a live fight");
            // Nor while something is chasing the party, wherever the crate has been left behind.
            var lead=PartyController.Current.Active;var far=cache.transform.position+Vector3.forward*-60;
            lead.GetComponent<CharacterController>().enabled=false;lead.transform.position=far;lead.GetComponent<CharacterController>().enabled=true;Physics.SyncTransforms();
            var cc=awake.GetComponent<CharacterController>();if(cc!=null)cc.enabled=false;awake.transform.position=far+Vector3.right*4;Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.4f);
            Assert.IsNull(heard,"a chase that has left the crate behind is still a fight");
            Object.Destroy(awake.gameObject);
            float deadline=Time.unscaledTime+3;while(heard==null&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual("CacheFound",heard,"the cache never speaks");
        }
        [UnityTest] public IEnumerator EachCacheNamesItsOwner()
        {
            Flag("bossdown.Burrower");Flag("neve.cache");
            yield return Zone("Hushwell");
            var cache=All<SalvageField>().Single(c=>c.cacheId=="HushwellMiners");
            string said=null;void Note(string heading,string body)=>said=body;Notices.Posted+=Note;
            cache.Interact();yield return null;Notices.Posted-=Note;
            Assert.IsNotNull(said);StringAssert.DoesNotContain("Neve",said,"the miners' cache says it's Neve's");
        }
        [UnityTest] public IEnumerator TheNurseryKeepsItsSecretUntilItIsFound()
        {
            yield return Zone("Hushwell");
            var hidden=All<TextMeshPro>().Where(t=>t.text=="NURSERY").ToArray();
            Assert.IsTrue(hidden.Length==0||hidden.All(t=>!t.gameObject.activeInHierarchy),"a sign names the reveal before it happens");
            Assert.IsFalse(GameServices.Current.Flags.GetBool("hushwell.nursery"));
            var card=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("BellowsBelow"),new Vector3(0,-8,262)).GetComponent<BossController>();yield return null;
            StringAssert.DoesNotContain("nursery",card.Epithet.ToLowerInvariant(),"the boss card gives away what it guards");
            Object.Destroy(card.gameObject);
        }
        [UnityTest] public IEnumerator EachBossBarkFitsItsBoss()
        {
            yield return null;
            foreach(var who in new[]{"Taren","Sela"})
            {
                Assert.IsNotNull(BarkService.Line(who,"bossphase.HessaKorl"),who+" has no line for Hessa's phase shift");
                Assert.IsNotNull(BarkService.Line(who,"bossopening.HessaKorl"),who+" has no line for Hessa's opening");
                Assert.IsNotNull(BarkService.Line(who,"bossphase.Cantor"),who+" has no line for the Cantor's phase shift");
                StringAssert.DoesNotContain("It's",BarkService.Line(who,"bossphase.HessaKorl"));
                StringAssert.DoesNotContain("wedged",BarkService.Line(who,"bossopening.HessaKorl"));
                StringAssert.DoesNotContain("floor",BarkService.Line(who,"bossphase.Cantor"));
                StringAssert.DoesNotContain("ground",BarkService.Line(who,"bossphase.Cantor"));
            }
        }
        [UnityTest] public IEnumerator MusterScavengersAreCalledThat()
        {
            yield return null;
            Assert.AreEqual("Muster Scavenger",DisplayNames.Target("MusterScav"));
        }
        [UnityTest] public IEnumerator CorraRunsTheArchMarket()
        {
            yield return Zone("Lanternwick");
            var stock=GameCatalog.Find<ShopInventory>("Lanternwick");
            StringAssert.Contains("ARCH MARKET",stock.title??"","Corra's stall is headed with Mira's trade");
            var shop=Object.FindFirstObjectByType<ShopUi>();shop.Open(stock);yield return null;
            StringAssert.Contains("ARCH MARKET",All<TMP_Text>().First(t=>t.name=="Heading"&&t.gameObject.activeInHierarchy).text);
            shop.Close();
        }
        [UnityTest] public IEnumerator TheChapterSaysItHasEnded()
        {
            foreach(var f in new[]{"met.Keeper","sliceComplete","tallow.lineObserved","tallow.driftDiscussed","compact.hailHeard","lanternwick.chartGiven","chapter2.homecoming"})Flag(f);
            yield return Zone("TallowDrift");yield return ObjectiveSettles();
            StringAssert.Contains("Chapter 2 complete",Objective(),"the objective reads like a task nothing can satisfy");
            StringAssert.Contains("Xylos",Objective());
            yield return Zone("Lanternwick");yield return ObjectiveSettles();
            StringAssert.Contains("Xylos",Objective(),"after the homecoming Lanternwick still says head home");
        }
        [UnityTest] public IEnumerator ObjectivesSayWhy()
        {
            foreach(var f in new[]{"met.Orrin","warpkey","hushwell.nursery"})Flag(f);
            yield return Zone("Hub_CinderHalo");yield return ObjectiveSettles();
            StringAssert.Contains("Tallow",Objective(),"nothing says why the party crosses the Gullet");
            Flag("gorge.relay1");
            yield return Zone("LanternGorge");yield return ObjectiveSettles();
            StringAssert.Contains("Aumi",Objective(),"nothing says who the relays are for");
        }

        // ---------------------------------------------------------------- the camera
        [UnityTest] public IEnumerator TheMarketArchGetsOutOfTheWay()
        {
            yield return Zone("Lanternwick");
            var arch=All<SceneryOcclusion>().FirstOrDefault(s=>s.name=="Market arch");
            Assert.IsNotNull(arch,"the arch fills the screen and hides the hero around Corra's stall");
            var lead=PartyController.Current.Active;
            Put(lead,new Vector3(0,.2f,9));
            yield return new WaitForSecondsRealtime(.8f);
            var block=new MaterialPropertyBlock();float fade=0;
            foreach(var r in arch.GetComponentsInChildren<Renderer>()){r.GetPropertyBlock(block);fade=Mathf.Max(fade,block.GetFloat("_OccFade"));}
            Assert.Greater(fade,.5f,"standing behind the arch leaves the hero hidden");
            foreach(var r in arch.GetComponentsInChildren<MeshRenderer>())Assert.IsTrue(r.sharedMaterial.IsKeywordEnabled("_OCCFADE_ON"),"the fade is asked for, but the material can't draw it");
            Put(lead,new Vector3(0,.2f,-14));
            yield return new WaitForSecondsRealtime(.8f);fade=0;
            foreach(var r in arch.GetComponentsInChildren<Renderer>()){r.GetPropertyBlock(block);fade=Mathf.Max(fade,block.GetFloat("_OccFade"));}
            Assert.Less(fade,.05f,"the arch stays faded with nobody behind it");
        }
        [UnityTest] public IEnumerator FloorSignsMoveAsideForTheHero()
        {
            yield return Zone("Hushwell");
            var sign=All<TextMeshPro>().First(t=>t.text=="DRILL BREACH");
            var lead=PartyController.Current.Active;
            Put(lead,sign.transform.position+new Vector3(4,.1f,16));yield return new WaitForSecondsRealtime(.3f);
            Assert.Greater(sign.alpha,.9f,"a sign with nobody near it starts faded");
            Put(lead,sign.transform.position+Vector3.up*.1f);yield return new WaitForSecondsRealtime(.3f);
            Assert.Less(sign.alpha,.25f,"a label on the floor reads across the hero's legs");
            Put(lead,sign.transform.position+new Vector3(4,.1f,16));yield return new WaitForSecondsRealtime(.3f);
            Assert.Greater(sign.alpha,.9f,"the label never comes back");
        }
        [UnityTest] public IEnumerator TheRelayDoorOpensOnTheRoomNotACloseUp()
        {
            yield return Zone("SunkenRelay");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.Greater(Object.FindFirstObjectByType<CameraRig>().GroundCameraDistance,14,"the zone opens on a close-up jammed against the gate");
        }

        // ---------------------------------------------------------------- the script
        [UnityTest] public IEnumerator TheScriptStopsPromisingWhatIsntThere()
        {
            yield return null;
            string root=Path.Combine(Application.dataPath,"_Project/Resources/Dialogue");
            string all=string.Concat(Directory.GetFiles(root,"*.yarn").Select(File.ReadAllText));
            foreach(var phrase in new[]{"Copper beacon","lamp oil","Light, parts, and more light","Knock twice","Close the door","warp is off the approach, past the port marker","past the second relay","Your keepers have her",
                "We met at the Halo","Your route's safe","Read the wall","as your director put it","You've been down the gorge","We saw it.","Are you going back down","The grazers are backing off","Maybe that's what your chart",
                "bridge"})
                StringAssert.DoesNotContain(phrase,all,"still in the script: "+phrase);
            foreach(var node in new[]{"PellTallow","PellLampAfter","AbriLanternWaiting","AbriLanternAfter","NeveAfterParcel","OdaAfterBerth","SurvivorAfterParcel","SurvivorPostNursery","OrrinAfterCantor","NeveAfterCantor"})
                StringAssert.Contains("title: "+node,all,"the script has no "+node);
        }
    }
}
