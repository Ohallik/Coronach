using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Core;
using Lattice.Combat;
using Lattice.Dialogue;
using Lattice.World;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>Development-only route driver. Uses motors, attacks, NPCs, pickups and saves; never awards flags or kills directly.</summary>
    public sealed class SliceSmoke:MonoBehaviour
    {
        bool failed,swapped,flashSeen,lungeSeen;
        float started;
        IEnumerator Start()
        {
            started=Time.realtimeSinceStartup;
            yield return new WaitForSecondsRealtime(1);
            var title=FindFirstObjectByType<TitleScreen>();if(title==null){Fail("title absent");yield break;}title.StartGame(false);
            yield return Zone("Hub_CinderHalo");yield return Capture("Hub_CinderHalo");
            yield return Use(FindObjectsByType<DockingPad>(FindObjectsSortMode.None).First(p=>p.spawn=="Office"));
            yield return Zone("Hub_Decks");yield return Talk("Orrin");
            if(GameServices.Current.State.party.Count!=2){Fail("Sela never joined");yield break;}
            yield return Talk("Hal");var menu=FindFirstObjectByType<PauseMenu>();if(menu.IsOpen)menu.Close();
            yield return Capture("Hub_Decks");yield return Use(FindObjectsByType<DockingPad>(FindObjectsSortMode.None).First(p=>p.spawn=="Office"));
            yield return Zone("Hub_CinderHalo");yield return Talk("Neve");
            yield return Use(FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).First(w=>w.scene=="Sorrel_Ridges"));
            yield return Zone("Sorrel_Ridges");yield return Talk("Survivor");
            yield return Use(FindFirstObjectByType<RepairBay>());yield return Capture("Sorrel_Ridges");
            foreach(float z in new[]{45f,73f,101f,129f,164f,181f})yield return Travel(new Vector3(0,0,z),2);
            yield return FightNearby(45);yield return Use(FindFirstObjectByType<KeyPickup>());
            if(!GameServices.Current.Flags.GetBool("warpkey")){Fail("warp key not earned");yield break;}
            yield return Use(FindObjectsByType<DockingPad>(FindObjectsSortMode.None).First(p=>p.spawn=="Outer"));
            yield return Zone("Hub_CinderHalo");yield return Use(FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).First(w=>w.scene=="Gullet_Tunnel"));
            yield return Zone("Gullet_Tunnel");
            for(float z=35;z<=875&&!failed;z+=25)
            {
                float x=Mathf.Sin(z/900*Mathf.PI*4)*12;yield return Travel(new Vector3(x,1,z),4);
                if(z>=390&&z<=415){var cache=FindFirstObjectByType<SalvageField>();if(cache!=null&&cache.Available)yield return Use(cache);}
                if(z==410)yield return Capture("Gullet_Tunnel");
            }
            yield return FightNearby(70);yield return Use(FindFirstObjectByType<WarpBeacon>());
            yield return Zone("TallowDrift");yield return Talk("Keeper");yield return Use(FindFirstObjectByType<RepairBay>());yield return Capture("TallowDrift");
            if(!flashSeen||!swapped||!lungeSeen||!GameServices.Current.Flags.GetBool("sliceComplete")||GameServices.Current.Saves.Load("autosave")?.flags.GetValueOrDefault("sliceComplete")!=true)
            {Fail($"end contract flash={flashSeen} swap={swapped} lunge={lungeSeen}");yield break;}
            string final=DevArgs.Value("-screenshot");yield return Shot(final);
            Debug.Log($"SLICE_SMOKE_OK elapsed={Time.realtimeSinceStartup-started:0.0} level={GameServices.Current.State.party[0].level}");Application.Quit();
        }
        IEnumerator Zone(string name)
        {
            float until=Time.realtimeSinceStartup+15;
            while((SceneFlow.Current.Loading||SceneFlow.Current.Zone!=name)&&Time.realtimeSinceStartup<until)yield return null;
            if(SceneFlow.Current.Zone!=name){Fail("zone transition "+name);yield break;}
            yield return new WaitForSecondsRealtime(.75f);PrepareParty();
        }
        void PrepareParty()
        {
            foreach(var m in PartyController.Current.members){m.GetComponent<PlayerBrain>().AutoPilot=true;m.GetComponent<PartnerBrain>().enabled=m!=PartyController.Current.Active&&flashSeen;}
        }
        IEnumerator Talk(string speaker)
        {
            var npc=FindObjectsByType<Npc>(FindObjectsSortMode.None).FirstOrDefault(n=>n.speaker==speaker);if(npc==null){Fail("missing NPC "+speaker);yield break;}
            if(SceneFlow.Current.Zone=="Hub_Decks")
            {yield return Travel(new Vector3(PartyController.Current.Active.transform.position.x,0,-6),1);yield return Travel(new Vector3(npc.transform.position.x,0,-6),1);}
            yield return Travel(npc.transform.position,speaker=="Neve"?5:2.5f);DialogueSystem.Current.AutoAdvance=true;npc.Interact();yield return null;
            float until=Time.realtimeSinceStartup+20;while(DialogueSystem.Current.Running&&Time.realtimeSinceStartup<until)yield return null;
            if(DialogueSystem.Current.Running)Fail("dialogue timeout "+speaker);PrepareParty();
        }
        IEnumerator Use(InteractionPrompt prompt)
        {
            if(failed)yield break;if(prompt==null){Fail("missing interactable");yield break;}
            if(prompt is DockingPad&&SceneFlow.Current.Zone=="Hub_Decks")yield return Travel(new Vector3(PartyController.Current.Active.transform.position.x,0,-6),1);
            yield return Travel(prompt.transform.position,Mathf.Max(1,prompt.range-.5f));if(failed)yield break;
            if(!prompt.Available)
            {
                if(prompt is SalvageField cache&&GameServices.Current.Flags.GetBool("cache."+cache.cacheId))yield break;
                Fail("unavailable "+prompt.name);yield break;
            }
            prompt.Interact();yield return null;
        }
        IEnumerator Travel(Vector3 goal,float distance)
        {
            float until=Time.realtimeSinceStartup+75;
            while(!failed&&Time.realtimeSinceStartup<until)
            {
                var actor=PartyController.Current.Active;Vector3 delta=goal-actor.transform.position;delta.y=0;if(delta.magnitude<=distance)yield break;
                if(ZoneController.Current.Combat&&Nearest(22)!=null)
                {
                    float combatStarted=Time.realtimeSinceStartup;
                    yield return FightNearby(28);
                    until+=Time.realtimeSinceStartup-combatStarted;
                    continue;
                }
                actor.motor.Move(new Vector2(delta.x,delta.z).normalized,false,delta.magnitude<9);yield return null;
            }
            if(!failed)Fail("travel stalled toward "+goal);
        }
        Health Nearest(float range)
        {
            var actor=PartyController.Current.Active;return Health.All.Where(h=>h!=null&&!h.friendly&&h.Alive&&(h.transform.position-actor.transform.position).sqrMagnitude<range*range).OrderBy(h=>(h.transform.position-actor.transform.position).sqrMagnitude).FirstOrDefault();
        }
        IEnumerator FightNearby(float range)
        {
            float until=Time.realtimeSinceStartup+150;
            while(!failed&&Time.realtimeSinceStartup<until)
            {
                var party=PartyController.Current;var actor=party.Active;var target=Nearest(range);if(target==null)yield break;
                if(party.members.All(m=>!m.Health.Alive)){Fail("party defeated");yield break;}
                actor.target=target;var brain=target.GetComponent<EnemyBrain>();var d=target.transform.position-actor.transform.position;d.y=0;
                float reach=actor.flight?5:!flashSeen?2.1f:actor.character=="Sela"?8:2.1f;
                actor.motor.Move(d.magnitude>reach?new Vector2(d.x,d.z).normalized:Vector2.zero,false,true);
                if(d.magnitude<reach+1)
                {
                    actor.motor.Move(new Vector2(d.x,d.z).normalized*.08f,false,true);
                    var boss=target.GetComponent<BossController>();
                    if(boss!=null&&boss.Telegraphing&&boss.TelegraphRemaining<.065f)actor.Dodge(d);
                    else if(brain!=null&&brain.Telegraphing&&brain.TelegraphRemaining<.065f&&d.magnitude<4)actor.Dodge(d);
                    else if(actor.flight&&d.magnitude<7&&target.integrity<=actor.damage){if(actor.Lunge())lungeSeen=true;}
                    else actor.Attack();
                    if(flashSeen&&actor.charge>=20)actor.Skill(actor.character=="Taren"?1:0);
                }
                if(actor.Health.integrity<actor.Health.maximum*.5f)actor.GetComponent<PlayerBrain>().UseItem("RepairGel");
                if(swapped&&actor.FlashMoves>0&&!flashSeen){flashSeen=true;PrepareParty();}
                if(!swapped&&actor.Kills>0&&party.Swap()){swapped=true;PrepareParty();}
                yield return null;
            }
            if(!failed)Fail("combat timeout");
        }
        IEnumerator Capture(string zone)
        {
            if(failed)yield break;var rig=FindFirstObjectByType<CameraRig>();var original=rig.profile;var profile=Instantiate(original);
            for(int i=0;i<3;i++)
            {profile.yaw=original.yaw+(i-1)*18;rig.Apply(PartyController.Current.Active.transform,profile);yield return new WaitForSecondsRealtime(.7f);yield return Shot(Path.Combine(Path.GetDirectoryName(DevArgs.Value("-screenshot")),"zones",zone+"-"+i+".png"));}
            rig.Apply(PartyController.Current.Active.transform,original);Destroy(profile);
        }
        IEnumerator Shot(string path)
        {
            if(string.IsNullOrEmpty(path))yield break;Directory.CreateDirectory(Path.GetDirectoryName(path));
            ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.5f);Debug.Log("SCREENSHOT_OK "+path);
        }
        void Fail(string message){failed=true;Debug.LogError("FAILED: route "+message);Application.Quit(1);}
    }
}
