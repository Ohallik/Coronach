using System.Collections;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>D195: the first town's morning on the Decks (walkway repair, Mira's stall, Sela's arrival).</summary>
    public sealed class OpeningMarketTests
    {
        static readonly Vector3 BridgeWest=new Vector3(-17.5f,.5f,-3),BridgeEast=new Vector3(-10.8f,.5f,-3);
        // Mirrors Lattice.EditorTools.MarketOpeningStaging (an editor assembly).
        static readonly Vector3 RepairPoint=new Vector3(-15,0,-3),SelaArrival=new Vector3(-11.3f,0,-4.4f);
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static IEnumerator Decks(bool opening,string spawn="Office")
        {
            if(opening){GameServices.Current.NewGame();GameServices.Current.Flags.SetBool(OpeningMarketSequence.Requested,true);}
            else DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Hub_Decks",spawn);float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);yield return new WaitForSecondsRealtime(.3f);
        }
        static OpeningMarketSequence Sequence()
        {
            var found=Object.FindObjectsByType<OpeningMarketSequence>(FindObjectsSortMode.None);
            Assert.AreEqual(1,found.Length,"the Decks have no opening scene");return found[0];
        }
        static Npc Speaker(string name)=>Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(n=>n.speaker==name);
        static void Place(Vector3 at)
        {
            foreach(var hero in PartyController.Current.members)
            {var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.position=at;cc.enabled=true;at+=Vector3.left*1.5f;}
            Physics.SyncTransforms();
        }
        static bool BridgeClosed()=>Physics.Linecast(BridgeWest,BridgeEast,out var hit,~0,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<CombatActor>()==null;
        static float Flat(Vector3 a,Vector3 b)=>Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));

        [UnityTest] public IEnumerator SavesAndFixturesWithoutTheRequestNeverSeeTheScene()
        {
            yield return Decks(false,"Arrival");
            var sequence=Sequence();var orrin=Speaker("Orrin");
            Assert.IsFalse(sequence.Fresh);Assert.IsFalse(BridgeClosed(),"the walkway is closed for a save that never had the scene");
            Assert.IsTrue(orrin.Available,"Orrin is unavailable outside the opening");
            Assert.IsFalse(sequence.repair.isActiveAndEnabled,"the repair job is offered outside the opening");
            Assert.Less(Flat(Speaker("Sela").transform.position,new Vector3(-23,0,-1)),.3f,"Sela is not at her waiting place");
            Assert.Less(Flat(Speaker("Mira").transform.position,new Vector3(0,0,4)),.6f,"Mira is not at her sales bay");
        }

        // Capture a talk request instead of opening the real conversation.
        sealed class Heard{public string node,speaker;public System.Action<bool> complete;public int requests;}
        static Heard Listen(out System.Action<string,string,System.Action<bool>> handler)
        {
            var heard=new Heard();
            foreach(var bridge in Object.FindObjectsByType<WorldUiBridge>(FindObjectsSortMode.None))bridge.enabled=false;
            handler=(node,speaker,complete)=>{heard.node=node;heard.speaker=speaker;heard.complete=complete;heard.requests++;};
            Npc.TalkRequested+=handler;return heard;
        }
        static void Stop(System.Action<string,string,System.Action<bool>> handler)
        {
            Npc.TalkRequested-=handler;
            foreach(var bridge in Object.FindObjectsByType<WorldUiBridge>(FindObjectsInactive.Include,FindObjectsSortMode.None))bridge.enabled=true;
        }
        // Read through reflection so the same checks also run (and fail) against the code that preceded D197.
        static string Hold(Npc speaker)=>typeof(Npc).GetField("holdNode")?.GetValue(speaker) as string;
        static bool Compiled(string node)=>System.Array.IndexOf(Lattice.Dialogue.DialogueSystem.Current.Runner.YarnProject.NodeNames,node)>=0;

        [UnityTest] public IEnumerator FreshMorningClosesTheBridgeAndHoldsOrrinsBriefingUntilTheJobIsDone()
        {
            yield return Decks(true);
            var sequence=Sequence();var orrin=Speaker("Orrin");var sela=Speaker("Sela").GetComponent<ScriptedWalk>();
            Assert.IsTrue(sequence.Fresh);Assert.IsTrue(BridgeClosed(),"the walkway is open during the repair");
            // D197: Orrin answers with a holding line, never with silence (Nathan read silence as a broken talk button),
            // and never with the briefing that brings Sela aboard.
            var heard=Listen(out var handler);
            try
            {
                Assert.IsTrue(orrin.Available,"Orrin cannot be talked to during the opening");
                orrin.Interact();
                Assert.AreEqual("OrrinOpening",heard.node,"Orrin skips the scene with his full conversation");
                Assert.IsTrue(Compiled(heard.node),"Orrin's holding line is missing from the dialogue");
                heard.complete(true);
                Assert.IsFalse(GameServices.Current.Flags.GetBool("met.Orrin"),"the holding line marks Orrin met and skips Sela's join");
                Assert.IsFalse(GameServices.Current.State.questSteps.ContainsKey("Sorrel"),"the holding line starts the Sorrel quest");
                Assert.IsTrue(orrin.Available,"Orrin stays busy after his holding line");
            }
            finally{Stop(handler);}
            Assert.AreEqual(ScriptedWalk.Stage.Waiting,sela.Phase,"Sela starts walking before Taren reaches the job");
            Assert.IsFalse(sequence.repair.Available,"the job is offered before Sela sets off");
            Place(RepairPoint+Vector3.left*1.2f);
            float began=Time.unscaledTime;
            while(sela.Phase==ScriptedWalk.Stage.Waiting&&Time.unscaledTime-began<3)yield return null;
            Assert.AreEqual(ScriptedWalk.Stage.Walking,sela.Phase,"Sela never sets off when Taren reaches the job");
            yield return null;
            Assert.IsTrue(sequence.repair.Available,"Taren stands at a silent barrier while Sela crosses the market");
            while(!sela.Arrived&&Time.unscaledTime-began<15)yield return null;
            Assert.IsTrue(sela.Arrived,"Sela never arrives across the market");
            Assert.Less(Time.unscaledTime-began,12,"Sela takes too long to arrive");
            yield return null;
            Assert.IsTrue(sequence.repair.Available,"the job is not offered once Sela is there");
            Assert.AreEqual("OrrinOpening",Hold(orrin),"Orrin's briefing opens before the job is done");
        }

        [UnityTest] public IEnumerator EveryoneAtTheWalkwayAnswersDuringTheOpeningWithoutSkippingIt()
        {
            yield return Decks(true);
            var sequence=Sequence();var heard=Listen(out var handler);
            try
            {
                foreach(var (name,node) in new[]{("Mira","MiraOpening"),("Sela","SelaOpening")})
                {
                    var speaker=Speaker(name);heard.node=null;
                    Assert.IsTrue(speaker.Available,name+" cannot be talked to during the opening");
                    speaker.Interact();
                    Assert.AreEqual(node,heard.node,name+" plays the wrong conversation during the opening");
                    Assert.IsTrue(Compiled(node),name+"'s holding line is missing from the dialogue");
                    heard.complete(true);
                    Assert.IsFalse(GameServices.Current.Flags.GetBool("met."+name),name+"'s holding line marks them met");
                }
                Assert.IsFalse(GameServices.Current.State.questSteps.ContainsKey("MiraCrystals"),"Mira's holding line starts her favour");
            }
            finally{Stop(handler);}
            GameServices.Current.Flags.SetBool(OpeningMarketSequence.Repaired,true);
            Place(new Vector3(-28,0,-9));
            yield return new WaitForSecondsRealtime(1.6f);
            Assert.AreEqual(1,sequence.Closure,.001f);
            foreach(var name in new[]{"Orrin","Mira","Sela"})
                Assert.IsTrue(string.IsNullOrEmpty(Hold(Speaker(name))),name+" keeps a holding line after the walkway opens");
        }

        [UnityTest] public IEnumerator SelaFinishesHerApproachWhileTheRepairConversationPlays()
        {
            yield return Decks(true);
            var sequence=Sequence();var walk=Speaker("Sela").GetComponent<ScriptedWalk>();
            float ready=Time.unscaledTime;
            while(!Lattice.Dialogue.DialogueSystem.Current.Prepared&&Time.unscaledTime-ready<10)yield return null;
            Assert.IsTrue(Lattice.Dialogue.DialogueSystem.Current.Prepared);
            Place(RepairPoint+Vector3.left*1.2f);
            float began=Time.unscaledTime;
            while(!sequence.repair.Available&&Time.unscaledTime-began<3)yield return null;
            Assert.IsFalse(walk.Arrived,"control: Sela must still be crossing when the job is first offered");
            sequence.repair.Interact();yield return null;
            Assert.IsTrue(GameInput.Current.Blocked,"the repair conversation never opened");
            while(!walk.Arrived&&Time.unscaledTime-began<15)yield return null;
            Assert.IsTrue(walk.Arrived,"Sela freezes mid-market while the repair conversation plays");
            Assert.IsTrue(GameInput.Current.Blocked,"control: Sela must arrive under the conversation, not after it");
        }

        [UnityTest] public IEnumerator FinishingTheJobOpensTheWalkwayAndSendsEveryoneOnTheirWay()
        {
            yield return Decks(true);
            var sequence=Sequence();var orrin=Speaker("Orrin");var selaWalk=Speaker("Sela").GetComponent<ScriptedWalk>();var mira=Speaker("Mira");
            Place(RepairPoint+Vector3.left*1.2f);
            float began=Time.unscaledTime;while(!selaWalk.Arrived&&Time.unscaledTime-began<15)yield return null;
            Assert.IsTrue(selaWalk.Arrived);
            GameServices.Current.Flags.SetBool(OpeningMarketSequence.Repaired,true);
            // Step out of Sela's way to Orrin's office.
            Place(new Vector3(-28,0,-9));
            yield return new WaitForSecondsRealtime(1.6f);
            Assert.AreEqual(1,sequence.Closure,.001f,"the cover and gates never finish moving");
            Assert.IsFalse(BridgeClosed(),"the walkway stays closed after the repair");
            Assert.IsTrue(orrin.Available,"Orrin stays unavailable after the repair");
            began=Time.unscaledTime;
            while((selaWalk.Phase!=ScriptedWalk.Stage.Done||mira.GetComponent<ScriptedWalk>().Phase!=ScriptedWalk.Stage.Done)&&Time.unscaledTime-began<30)yield return null;
            Assert.AreEqual(ScriptedWalk.Stage.Done,selaWalk.Phase,"Sela never reaches her waiting place");
            Assert.AreEqual(ScriptedWalk.Stage.Done,mira.GetComponent<ScriptedWalk>().Phase,"Mira never returns to her sales bay");
            Assert.Less(Flat(selaWalk.transform.position,new Vector3(-23,0,-1)),.2f);
            Assert.Less(Flat(mira.transform.position,new Vector3(0,0,4)),.6f);
        }

        [UnityTest] public IEnumerator SelasWalkHoldsForPauseAndForSomeoneInTheWay()
        {
            yield return Decks(true);
            var walk=Speaker("Sela").GetComponent<ScriptedWalk>();
            Place(RepairPoint+Vector3.left*1.2f);
            float began=Time.unscaledTime;while(walk.Phase!=ScriptedWalk.Stage.Walking&&Time.unscaledTime-began<3)yield return null;
            Assert.AreEqual(ScriptedWalk.Stage.Walking,walk.Phase);
            GameTime.Paused=true;var held=walk.transform.position;yield return new WaitForSecondsRealtime(1);
            Assert.Less(Vector3.Distance(held,walk.transform.position),.001f,"Sela keeps walking while paused");
            GameTime.Paused=false;
            // Stand Taren on her path: she stops rather than walking through him.
            var hero=PartyController.Current.Active;var cc=hero.GetComponent<CharacterController>();cc.enabled=false;
            var ahead=walk.transform.position+(SelaArrival-walk.transform.position).normalized*1.1f;ahead.y=0;
            hero.transform.position=ahead;cc.enabled=true;Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(walk.Blocked,"Sela walks into Taren");
            Assert.Greater(Flat(walk.transform.position,hero.transform.position),.6f,"Sela overlaps Taren");
        }
    }
}
