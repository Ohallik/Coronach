using System.Collections;
using System.Linq;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>D199: chapter 1's side stories play through real dialogue, advance only on their own conversations, never
    /// take a first meeting, pay out their rewards once, and leave visible changes behind.</summary>
    public sealed class SideStoryTests
    {
        string heard;
        void Listen(string node,string speaker,System.Action<bool> completed){heard=node;}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");heard=null;Npc.TalkRequested+=Listen;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {Npc.TalkRequested-=Listen;GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+12;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<Lattice.Combat.EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Prepared);
        }
        static GameState State=>GameServices.Current.State;
        static void Flag(string key){GameServices.Current.Flags.SetBool(key,true);}
        static int Step(string quest)=>State.questSteps.TryGetValue(quest,out var step)?step:-1;
        static Npc Fixture(string speaker)
        {
            var npc=new GameObject(speaker+" fixture").AddComponent<Npc>();
            npc.speaker=speaker;npc.firstNode=speaker+"First";npc.repeatNode=speaker+"Repeat";npc.postNode=speaker+"Post";return npc;
        }
        static Npc Speaker(string name)=>Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(n=>n.speaker==name);
        IEnumerator Talk(Npc npc)
        {
            heard=null;DialogueSystem.Current.AutoAdvance=true;npc.Interact();yield return null;
            Assert.IsNotNull(heard,npc.speaker+" did not speak");
            float deadline=Time.unscaledTime+25;
            while((DialogueSystem.Current.Running||GameInput.Current.Blocked)&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(DialogueSystem.Current.Running,npc.speaker+"'s conversation never ended");
            yield return new WaitForSecondsRealtime(.3f);
        }

        [Test] public void SideStoryLinesNeverTakeAFirstMeeting()
        {
            var state=new GameState();
            state.questSteps["NeveParcel"]=0;
            Assert.IsNull(SideStories.NodeFor("Survivor",state),"the parcel would replace the Survivor's first conversation");
            state.flags["met.Survivor"]=true;
            Assert.AreEqual("SurvivorParcel",SideStories.NodeFor("Survivor",state));
            state.questSteps["OdaBerth"]=0;
            Assert.IsNull(SideStories.NodeFor("Orrin",state),"the berth token would replace Orrin's briefing");
            Assert.IsNull(SideStories.NodeFor("Oda",new GameState()),"Oda's favour would replace her first conversation");
        }

        [UnityTest] public IEnumerator TheOverdueParcelTravelsFromNeveToSorrelAndBack()
        {
            yield return Zone("Hub_Decks","Office");
            var neve=Fixture("Neve");var survivor=Fixture("Survivor");
            Flag("met.Neve");
            yield return Talk(neve);
            Assert.AreEqual("NeveParcelOffer",heard);Assert.AreEqual(0,Step("NeveParcel"),"Neve's offer does not start the parcel task");
            var toast=Object.FindFirstObjectByType<QuestToast>();Assert.IsNotNull(toast,"no task announcements");
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(toast.Showing,"a new task is not announced once the conversation closes");
            StringAssert.Contains("NEW TASK",toast.Heading);StringAssert.Contains("Survivor",toast.Body);
            // Meeting the Survivor for the first time does not tick the delivery.
            yield return Talk(survivor);
            Assert.AreEqual("SurvivorFirst",heard,"the parcel replaced the Survivor's first conversation");
            Assert.AreEqual(0,Step("NeveParcel"),"an ordinary talk delivered the parcel");
            yield return Talk(survivor);
            Assert.AreEqual("SurvivorParcel",heard);Assert.AreEqual(1,Step("NeveParcel"));
            int scrip=State.scrip;State.consumables.TryGetValue("ChargeCell",out int cells);
            yield return Talk(neve);
            Assert.AreEqual("NeveParcelReturn",heard);
            Assert.IsTrue(GameServices.Current.Flags.GetBool("quest.NeveParcel.complete"),"the parcel task does not complete");
            Assert.AreEqual(scrip+80,State.scrip,"the parcel's scrip");
            Assert.AreEqual(cells+2,State.consumables["ChargeCell"],"the charge cells did not reach the item bag");
            yield return Talk(neve);
            Assert.AreNotEqual("NeveParcelReturn",heard,"the completed parcel plays again");Assert.AreNotEqual("NeveParcelOffer",heard);
            Assert.AreEqual(scrip+80,State.scrip,"the reward paid twice");
        }

        [UnityTest] public IEnumerator TheBerthTokenPassesThroughOrrinsOffice()
        {
            yield return Zone("Hub_Decks","Office");
            var oda=Fixture("Oda");var orrin=Speaker("Orrin");
            Flag("met.Oda");
            yield return Talk(oda);
            Assert.AreNotEqual("OdaBerthOffer",heard,"Oda asks a favour of someone who has never met Orrin");
            Flag("met.Orrin");
            yield return Talk(oda);
            Assert.AreEqual("OdaBerthOffer",heard);Assert.AreEqual(0,Step("OdaBerth"));
            yield return Talk(orrin);
            Assert.AreEqual("OrrinBerth",heard);Assert.AreEqual(1,Step("OdaBerth"));
            State.consumables.TryGetValue("RepairGel",out int gels);
            yield return Talk(oda);
            Assert.AreEqual("OdaBerthReturn",heard);
            Assert.IsTrue(GameServices.Current.Flags.GetBool("quest.OdaBerth.complete"));
            Assert.AreEqual(gels+2,State.consumables["RepairGel"],"the repair gel did not reach the item bag");
            yield return Talk(orrin);
            Assert.AreEqual("OrrinRepeat",heard,"Orrin keeps handing over the token");
        }

        [UnityTest] public IEnumerator TallowsDriftEndsWithTheCompactOnTheBeaconBandOnce()
        {
            yield return Zone("Hub_Decks","Office");
            var keeper=Fixture("Keeper");keeper.finalNode="KeeperDriftReturn";keeper.finalFlag="tallow.lineObserved";
            keeper.finalCompletionFlag="tallow.driftDiscussed";keeper.finalRepeatNode="KeeperDriftRepeat";
            Flag("met.Keeper");Flag("tallow.lineObserved");
            yield return Talk(keeper);
            Assert.AreEqual("KeeperDriftReturn",heard,"the hail interrupted the drift conversation itself");
            Assert.IsTrue(GameServices.Current.Flags.GetBool("tallow.driftDiscussed"));
            yield return Talk(keeper);
            Assert.AreEqual("CompactHail",heard,"the Compact never calls once Tallow knows it is moving");
            Assert.IsTrue(GameServices.Current.Flags.GetBool("compact.hailHeard"));
            yield return Talk(keeper);
            Assert.AreEqual("KeeperDriftRepeat",heard,"the Compact's call repeats");
        }

        [UnityTest] public IEnumerator SorrelsBeaconsWaitForTheJobGoDarkAndStayDark()
        {
            Flag("met.Survivor");Flag("bossdown.Burrower");Flag("clear.Sorrel_Burrower");
            yield return Zone("Sorrel_Ridges");
            var beacons=Object.FindObjectsByType<SignalBeacon>(FindObjectsSortMode.None);
            Assert.AreEqual(4,beacons.Length,"Sorrel has no survey beacons to silence");
            foreach(var beacon in beacons)
            {
                Assert.IsFalse(beacon.Quiet);Assert.IsTrue(beacon.glow.enabled,"a calling beacon shows no light");
                Assert.IsFalse(beacon.GetComponent<DiscoveryPoint>().Available,"a beacon can be silenced before the Survivor asks");
            }
            var survivor=Speaker("Survivor");
            yield return Talk(survivor);
            Assert.AreEqual("SurvivorBeaconsOffer",heard);Assert.AreEqual(0,Step("SorrelBeacons"));
            for(int i=0;i<beacons.Length;i++)
            {
                var point=beacons[i].GetComponent<DiscoveryPoint>();Assert.IsTrue(point.Available,"the beacon job is taken but beacon "+i+" cannot be silenced");
                point.Interact();yield return null;
                Assert.IsTrue(beacons[i].Quiet);
                Assert.AreEqual(i<3?0:1,Step("SorrelBeacons"),"the task counts the wrong number of beacons");
            }
            yield return null;
            foreach(var beacon in beacons)Assert.IsFalse(beacon.glow.enabled,"a silenced beacon still calls");
            int parts=State.parts.Count(p=>p.definitionId=="FramesT2");
            yield return Talk(survivor);
            Assert.AreEqual("SurvivorBeaconsDone",heard);
            Assert.IsTrue(GameServices.Current.Flags.GetBool("quest.SorrelBeacons.complete"));
            Assert.AreEqual(parts+1,State.parts.Count(p=>p.definitionId=="FramesT2"),"the frame plating never reached the gear list");
            yield return Zone("Sorrel_Ridges");
            beacons=Object.FindObjectsByType<SignalBeacon>(FindObjectsSortMode.None);yield return null;
            Assert.IsTrue(beacons.All(b=>b.Quiet&&!b.glow.enabled),"silenced beacons call again on the next visit");
        }
    }
}
