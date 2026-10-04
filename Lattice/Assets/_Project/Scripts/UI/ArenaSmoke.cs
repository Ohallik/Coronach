using System.Collections;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.UI
{
    public sealed class ArenaSmoke:MonoBehaviour
    {
        IEnumerator Start()
        {
            Debug.Log($"ARENA_SMOKE_PACING cap={Application.targetFrameRate} vsync={QualitySettings.vSyncCount}");
            var party=PartyController.Current;bool flight=ZoneController.Current.Flight;
            foreach(var member in party.members){member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;}
            var enemies=FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).OrderBy(e=>e.transform.position.x).ToArray();
            if(enemies.Length<3){Fail("fewer than three enemies");yield break;}
            foreach(var enemy in enemies)enemy.Passive=true;
            yield return new WaitForSecondsRealtime(.8f);
            int kills=0;
            for(int i=0;i<3;i++)
            {
                if(i==1){if(!party.Swap()){Fail("swap refused");yield break;}foreach(var m in party.members)m.GetComponent<PartnerBrain>().enabled=false;}
                var actor=party.Active;var enemy=enemies[i];actor.target=enemy.Health;
                float deadline=Time.realtimeSinceStartup+15;
                while(enemy!=null&&enemy.Health.Alive&&Time.realtimeSinceStartup<deadline)
                {
                    Vector3 d=enemy.transform.position-actor.transform.position;d.y=0;
                    bool close=d.magnitude<(flight&&i!=1?7:2.4f);
                    actor.motor.Move(close?Vector2.zero:new Vector2(d.x,d.z).normalized,false,true);
                    if(close)
                    {
                        // Keep aim aligned using real movement before the combat action.
                        actor.motor.Move(new Vector2(d.x,d.z).normalized,false,true);
                        if(i==1){actor.charge=Mathf.Max(actor.charge,60);actor.Skill(2);}
                        else if(flight&&i==2)actor.Lunge();else actor.Attack();
                    }
                    yield return null;
                }
                if(enemy!=null&&enemy.Health.Alive){Fail("combat could not kill "+enemy.name);yield break;}kills++;
            }
            // Exercise timing via an actual enemy HitVolume, never a fabricated OK log.
            var active=party.Active;active.motor.Move(Vector2.zero,false,true);
            float before=active.Health.integrity,dodgedAt=GameTime.Now;bool dodged=active.Dodge(Vector3.right);
            yield return new WaitForSecondsRealtime(.05f);
            float hitDelay=GameTime.Now-dodgedAt;
            CombatActor.Strike(active.transform.position+Vector3.up,3,new DamagePacket{amount=25,type=Lattice.Data.DamageType.Kinetic});
            yield return new WaitForSecondsRealtime(.15f);
            if(!dodged||active.FlashMoves==0||active.Health.integrity!=before||party.SwapCount<1||kills!=3){Fail($"timing / swap / kill assertions failed: dodge={dodged} delay={hitDelay:F4} flash={active.FlashMoves} hp={before}->{active.Health.integrity} swap={party.SwapCount} kills={kills} state={active.State}");yield break;}
            yield return new WaitForSecondsRealtime(1.5f);
            string path=DevArgs.Value("-screenshot");
            if(!string.IsNullOrEmpty(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.5f);
                ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.7f);Debug.Log("SCREENSHOT_OK "+path);
            }
            Debug.Log("ARENA_SMOKE_OK "+ZoneController.Current.definition.scene+" kills=3 swap=1 flash=1");Application.Quit();
        }
        void Fail(string reason){Debug.LogError("FAILED: arena smoke "+reason);Application.Quit(1);}
    }
}
