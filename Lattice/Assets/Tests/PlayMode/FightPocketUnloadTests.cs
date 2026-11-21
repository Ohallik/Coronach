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
    /// <summary>A fight pocket (CHAPTER_GUIDE 15.11) gives the zone its own kind back when it is switched off with the lead inside. At a scene's unload that refreshed the
    /// party (ZoneController.Changed, PartyController.Apply) after its heroes were already destroyed, and threw (seen in a test log, November 15). The pocket is stood round
    /// the lead in the Decks, a hero is destroyed as an unload destroys it, and the pocket goes off: nothing may throw. And with the party whole, it still hands the zone back.</summary>
    public sealed class FightPocketUnloadTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Hub_Decks","Office");
            float deadline=Time.unscaledTime+15;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: the Decks never finished loading");
            yield return new WaitForSecondsRealtime(.3f);
        }
        [UnityTearDown] public IEnumerator Cleanup(){GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);LogAssert.ignoreFailingMessages=false;}

        static IEnumerator Pocket(System.Action<FightPocket> made)
        {
            var lead=PartyController.Current.Active;
            var room=new GameObject("test fight pocket");room.SetActive(false);
            var box=room.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(8,6,8);room.transform.position=lead.transform.position+Vector3.up;
            var pocket=room.AddComponent<FightPocket>();pocket.from="";pocket.until="test.pocketNeverClosed";
            room.SetActive(true);yield return null;yield return null;
            Assert.IsTrue(ZoneController.Current.Combat,"fixture: the lead stands in the pocket and the Decks didn't become a place with fights");
            made(pocket);
        }

        [UnityTest] public IEnumerator APocketSwitchedOffAtAnUnloadLeavesHeroesAlreadyGone()
        {
            FightPocket pocket=null;yield return Pocket(p=>pocket=p);
            var zone=ZoneController.Current;var party=PartyController.Current;var gone=party.members.First(m=>m!=party.Active);
            Assert.IsFalse(zone.definition.kind==Lattice.Data.ZoneKind.GroundCombat,"fixture: the Decks is a place with fights of its own");
            // What follows is all one frame, as an unload is: a hero destroyed first (it destroys a scene's objects in no particular order), then the pocket switched off.
            // The frames after it are this fixture's own wreckage (a party whose hero has gone, which no unload leaves running): what they log is not the pocket's.
            LogAssert.ignoreFailingMessages=true;
            Object.DestroyImmediate(gone.gameObject);
            // Wrong first: handing the zone back as the pocket used to on switching off, the party is refreshed with its hero gone, and that throws.
            Assert.Catch(()=>zone.FightPocket(true),"fixture: handing the zone over with a hero destroyed no longer touches the party, so nothing here shows the fault");
            var thrown=new System.Collections.Generic.List<string>();
            void Heard(string text,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)thrown.Add(text+"\n"+stack);}
            Application.logMessageReceived+=Heard;
            try{pocket.gameObject.SetActive(false);}
            finally{Application.logMessageReceived-=Heard;}
            Assert.IsEmpty(thrown,"a pocket switched off with a hero already destroyed refreshed him: "+string.Join("\n",thrown));
            // The wreck goes before the test ends, as the unload it stands for would have taken it.
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        [UnityTest] public IEnumerator APocketSwitchedOffWithThePartyWholeGivesTheZoneItsKindBack()
        {
            FightPocket pocket=null;yield return Pocket(p=>pocket=p);
            pocket.gameObject.SetActive(false);yield return null;
            Assert.IsFalse(ZoneController.Current.Combat,"control: switched off with the lead inside, the pocket keeps the Decks a place with fights");
            Object.Destroy(pocket.gameObject);
        }
    }
}
