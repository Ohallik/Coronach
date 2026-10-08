using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.Combat;
using Lattice.Dialogue;
using Lattice.World;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>Development-only route driver. Uses motors, attacks, NPCs, pickups and saves; never awards flags or kills directly.</summary>
    public sealed class SliceSmoke:MonoBehaviour
    {
        bool failed,swapped,flashSeen,lungeSeen,lungeKilled;
        void OnEnable()=>Application.logMessageReceived+=Heard;
        void OnDisable()=>Application.logMessageReceived-=Heard;
        void Heard(string message,string stack,LogType type){if(message=="LUNGE_KILL")lungeKilled=true;}
        float started;
        Health preferredTarget;
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
            // Flight travel is a straight line; pass south of the repair hull to the
            // moon approach, as a pilot would, instead of into its casing.
            yield return Travel(new Vector3(46,1,-34),3);
            yield return Use(FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).First(w=>w.scene=="Sorrel_Ridges"));
            yield return Zone("Sorrel_Ridges");yield return Talk("Survivor");
            yield return Use(FindFirstObjectByType<RepairBay>());yield return Capture("Sorrel_Ridges");
            // The redesigned basin's haul road bends between ridge strata; walk its
            // actual bends to the drill and the Burrower's excavation.
            foreach(var p in new[]{new Vector2(9,39),new Vector2(4,58),new Vector2(-8,75),new Vector2(-5,94),new Vector2(9,111),new Vector2(16,129),new Vector2(7,145),new Vector2(0,164),new Vector2(3,181)})
                yield return Travel(new Vector3(p.x,0,p.y),2);
            yield return FightNearby(45);yield return Use(FindFirstObjectByType<KeyPickup>());
            if(!GameServices.Current.Flags.GetBool("warpkey")){Fail("warp key not earned");yield break;}
            // The nursery chart now supplies the living route missing from the key.
            yield return Use(FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).First(w=>w.scene=="Hushwell"));
            yield return Zone("Hushwell");
            foreach(var p in HushwellLayout.Main.Where(p=>p.y<=226))yield return Travel(HushwellLayout.OnFloor(p),2);
            if(PartyController.Current.Active.character!="Taren"){PartyController.Current.Swap();PrepareParty();}
            foreach(var organ in PressureOrgan.All.ToArray())
            {
                if(!organ.Pumping)continue;
                preferredTarget=organ.Health;
                yield return Travel(organ.transform.position,2);yield return FightNearby(45);
                preferredTarget=null;
            }
            yield return FightNearby(45);
            if(PressureOrgan.AnyPumping||!GameServices.Current.Flags.GetBool("bossdown.BellowsBelow"))
            {Fail("the Bellows or its organs remain");yield break;}
            foreach(var p in HushwellLayout.Main.Where(p=>p.y>=284))yield return Travel(HushwellLayout.OnFloor(p),2);
            DialogueSystem.Current.AutoAdvance=true;yield return Use(FindFirstObjectByType<DiscoveryPoint>());
            float discoveryDeadline=Time.realtimeSinceStartup+15;
            while(DialogueSystem.Current.Running&&Time.realtimeSinceStartup<discoveryDeadline)yield return null;
            if(!GameServices.Current.Flags.GetBool("hushwell.nursery")){Fail("nursery chart not earned");yield break;}
            yield return Capture("Hushwell");
            // The lift's pivot sits above its floor; approach the same walkable
            // stance used by the ordinary route before requesting interaction.
            yield return Travel(HushwellLayout.OnFloor(new Vector2(25.2f,340.2f)),1);
            yield return Use(FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).First(w=>w.requiredFlag=="hushwell.nursery"));
            yield return Zone("Sorrel_Ridges");
            yield return Use(FindObjectsByType<DockingPad>(FindObjectsSortMode.None).First(p=>p.spawn=="Outer"));
            yield return Zone("Hub_CinderHalo");yield return Use(FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).First(w=>w.scene=="Gullet_Tunnel"));
            yield return Zone("Gullet_Tunnel");
            for(float z=35;z<=875&&!failed;z+=25)
            {
                float x=GulletProfile.RouteX(z);yield return Travel(new Vector3(x,1,z),4);
                if(z>=390&&z<=415){var cache=FindFirstObjectByType<SalvageField>();if(cache!=null&&cache.Available)yield return Use(cache);}
                if(z==410)yield return Capture("Gullet_Tunnel");
            }
            yield return FightNearby(70);yield return Use(FindFirstObjectByType<WarpBeacon>());
            yield return Zone("TallowApproach");yield return Capture("TallowApproach");yield return Use(FindFirstObjectByType<DockingPad>());
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
            yield return Travel(npc.transform.position,speaker=="Neve"?5:2.5f);DialogueSystem.Current.AutoAdvance=true;npc.Interact();yield return null;
            float until=Time.realtimeSinceStartup+20;while(DialogueSystem.Current.Running&&Time.realtimeSinceStartup<until)yield return null;
            if(DialogueSystem.Current.Running)Fail("dialogue timeout "+speaker);PrepareParty();
        }
        IEnumerator Use(InteractionPrompt prompt)
        {
            if(failed)yield break;if(prompt==null){Fail("missing interactable");yield break;}
            // A ship's hull stops against a solid pad about 4.3 m from its centre,
            // so a flight approach aims just inside the prompt's own range.
            float reach=PartyController.Current.Active.flight?prompt.range-.12f:Mathf.Max(1,prompt.range-.5f);
            yield return Travel(prompt.transform.position,reach);if(failed)yield break;
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
            var path=new UnityEngine.AI.NavMeshPath();
            Vector3[] corners=null;int corner=1;float nextPath=0;
            while(!failed&&Time.realtimeSinceStartup<until)
            {
                if(preferredTarget!=null&&!preferredTarget.Alive)yield break;
                var actor=PartyController.Current.Active;Vector3 delta=goal-actor.transform.position;delta.y=0;if(delta.magnitude<=distance)yield break;
                if(ZoneController.Current.Combat&&Nearest(22)!=null)
                {
                    float combatStarted=Time.realtimeSinceStartup;
                    yield return FightNearby(28);
                    until+=Time.realtimeSinceStartup-combatStarted;
                    continue;
                }
                if(!actor.flight&&GroundNavigation.Current!=null)
                {
                    if(Time.realtimeSinceStartup>=nextPath)
                    {
                        nextPath=Time.realtimeSinceStartup+.3f;
                        if(!GroundNavigation.Current.FindPath(actor.transform.position,goal,goal,path))
                        {
                            // A solid interactable (an anvil, a crystal) has no floor at its
                            // centre: walk to any reachable stance within reach instead.
                            bool found=false;
                            for(int a=0;a<12&&!found;a++)
                            {var stance=goal+Quaternion.Euler(0,a*30,0)*Vector3.forward*Mathf.Max(.8f,distance*.8f);found=GroundNavigation.Current.FindPath(actor.transform.position,stance,stance,path);}
                            if(!found){Fail("no walking path to "+goal);yield break;}
                        }
                        corners=path.corners;corner=1;
                    }
                    if(corners!=null&&corners.Length>1)
                    {
                        while(corner<corners.Length-1&&Vector3.Distance(actor.transform.position,corners[corner])<.4f)corner++;
                        delta=corners[corner]-actor.transform.position;delta.y=0;
                    }
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
                if(preferredTarget!=null&&!preferredTarget.Alive)yield break;
                var party=PartyController.Current;var actor=party.Active;var target=preferredTarget!=null&&preferredTarget.Alive?preferredTarget:Nearest(range);if(target==null)yield break;
                if(party.members.All(m=>!m.Health.Alive)){Fail("party defeated");yield break;}
                actor.target=target;var brain=target.GetComponent<EnemyBrain>();var d=target.transform.position-actor.transform.position;d.y=0;
                // Like the skills below, the partner would finish every Dart before a
                // lunge could reach one, so in flight it holds until a lunge has killed.
                if(flashSeen)party.members[1-party.index].GetComponent<PartnerBrain>().enabled=!actor.flight||lungeKilled;
                float reach=actor.flight?5:!flashSeen?2.1f:actor.character=="Sela"?8:2.1f;
                // A mine arms at 2.8 m and ship momentum carries past a 5 m stop:
                // shoot mines from 7 m and back off if drifting closer, as pilots do.
                bool mine=brain!=null&&brain.definition.archetype==Lattice.Data.EnemyArchetype.Mine;
                if(mine)reach=7;
                actor.motor.Move(d.magnitude>reach?new Vector2(d.x,d.z).normalized:mine&&d.magnitude<reach-1.5f?-new Vector2(d.x,d.z).normalized:Vector2.zero,false,true);
                if(d.magnitude<reach+1+(mine?2:0))
                {
                    // Stay above the motors' 0.1-facing threshold after a boss crosses us.
                    actor.motor.Move(new Vector2(d.x,d.z).normalized*.15f,false,true);
                    var boss=target.GetComponent<BossController>();
                    if(boss!=null&&boss.Telegraphing&&boss.TelegraphRemaining<.065f)actor.Dodge(d);
                    else if(brain!=null&&brain.Telegraphing&&brain.TelegraphRemaining<.065f&&d.magnitude<4)actor.Dodge(d);
                    else if(actor.flight&&!mine&&d.magnitude<7&&target.integrity<=actor.damage){if(actor.Lunge())lungeSeen=true;}
                    else actor.Attack();
                    // In flight, hold skills until a lunge has landed a kill: they would
                    // otherwise finish every target first and leave the lunge unproven.
                    if(flashSeen&&actor.charge>=20&&(!actor.flight||lungeKilled))actor.Skill(actor.character=="Taren"?1:0);
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
        void Fail(string message)
        {
            failed=true;
            var party=PartyController.Current;
            if(party!=null)
            {
                foreach(var actor in party.members)Debug.Log($"ROUTE_FAILURE_PARTY {actor.character} active={actor==party.Active} hp={actor.Health.integrity:0}/{actor.Health.maximum:0} charge={actor.charge:0} state={actor.State} pos={actor.transform.position}");
                foreach(var enemy in Health.All.Where(h=>h!=null&&!h.friendly&&h.Alive))Debug.Log($"ROUTE_FAILURE_ENEMY {enemy.id} hp={enemy.integrity:0}/{enemy.maximum:0} pos={enemy.transform.position}");
            }
            Debug.LogError("FAILED: route "+message);Application.Quit(1);
        }
    }
}
