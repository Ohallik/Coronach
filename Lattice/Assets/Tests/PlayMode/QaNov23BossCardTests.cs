using System.Collections;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
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
    /// <summary>D376: the Burrower, the Bellows Below and the Cantor open with the same title card as every later boss. The capture of November 22 stills them only after
    /// the card's two seconds had run (s06 020 is seven seconds into the step), so the stills never showed one.</summary>
    public sealed class QaNov23BossCardTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();PromptService.ForceDevice(PromptDevice.Gamepad);SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("hushwell");
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone("Arena_Ground","Arrival");deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;}
            yield return new WaitForSecondsRealtime(.6f);
        }
        [UnityTearDown] public IEnumerator Cleanup(){PromptService.ForceDevice(null);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static TMP_Text Text(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);

        [UnityTest] public IEnumerator TheFirstThreeBossesOpenWithATitleCard()
        {
            var faults=new System.Collections.Generic.List<string>();
            foreach(string id in new[]{"Burrower","BellowsBelow","Cantor","HessaKorl"})
            {
                foreach(var m in PartyController.Current.members)m.Health.InvulnerableUntil=GameTime.Now+120;
                var at=PartyController.Current.Active.transform.position+new Vector3(0,0,9);
                var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);boss.Passive=false;
                float seen=0;
                for(float end=Time.unscaledTime+1.2f;Time.unscaledTime<end;){boss.transform.position=at;Physics.SyncTransforms();yield return null;seen=Mathf.Max(seen,Text("BossTitleCard").alpha);}
                if(seen<.99f)faults.Add($"{id}: the title card reached {seen:0.00}, text '{Text("BossTitleCard").text}'");
                else if(!Text("BossTitleCard").text.Contains(boss.GetComponent<BossController>().DisplayName))faults.Add($"{id}: the card names '{Text("BossTitleCard").text}'");
                boss.Passive=true;Object.Destroy(boss.gameObject);yield return new WaitForSecondsRealtime(3f);
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        static Health Dummy(Vector3 at,float integrity)
        {var body=new GameObject("Body fixture").AddComponent<Health>();body.id="Fixture";body.maximum=body.integrity=integrity;body.breakThreshold=1e9f;body.transform.position=at;return body;}
        static TMP_Text[] Live()=>Object.FindFirstObjectByType<GameHud>().GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name=="Damage"&&t.isActiveAndEnabled).ToArray();

        // A number is full strength every frame it exists (the old fade left pale ghosts, s65 066), and goes with its body (s14 017, s03 007).
        [UnityTest] public IEnumerator ANumberIsFullStrengthUntilGoneAndGoesWithItsBody()
        {
            var faults=new System.Collections.Generic.List<string>();
            var hero=PartyController.Current.Active;var at=hero.transform.position+new Vector3(0,0,6);
            var alive=Dummy(at,1000000);alive.Receive(new DamagePacket{amount=48,type=DamageType.Pulse});
            float firstGone=-1,start=Time.unscaledTime;
            for(float end=start+1.6f;Time.unscaledTime<end;)
            {
                var up=Live();
                foreach(var n in up)if(n.alpha<.99f)faults.Add($"at {Time.unscaledTime-start:0.00} s a number '{n.text}' stands at alpha {n.alpha:0.00}");
                if(up.Length==0&&firstGone<0)firstGone=Time.unscaledTime-start;
                yield return null;
            }
            if(firstGone<0)faults.Add("a number never left");
            Object.Destroy(alive.gameObject);yield return null;

            var victim=Dummy(at,100);victim.Receive(new DamagePacket{amount=60,type=DamageType.Pulse});yield return null;
            if(Live().Length==0)faults.Add("fixture: no number on the victim");
            victim.Receive(new DamagePacket{amount=500,type=DamageType.Pulse});
            start=Time.unscaledTime;float down=-1;
            for(float end=start+.8f;Time.unscaledTime<end;){var up=Live();foreach(var n in up)if(n.alpha<.99f)faults.Add($"a beaten body's number is pale: {n.alpha:0.00}");if(up.Length==0&&down<0)down=Time.unscaledTime-start;yield return null;}
            if(down<0||down>.5f)faults.Add($"a beaten body's numbers went after {down:0.00} s");
            // Its body taken from the world: its number goes at once.
            var gone=Dummy(at,1000000);gone.Receive(new DamagePacket{amount=77,type=DamageType.Pulse});yield return null;
            Object.Destroy(gone.gameObject);yield return null;yield return null;
            if(Live().Length>0)faults.Add("a number hangs over a body that has left the world");
            Assert.IsEmpty(faults,string.Join(", ",faults.Distinct()));
        }
    }
}
