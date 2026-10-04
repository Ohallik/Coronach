using System.Collections;
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
    public sealed class HushwellTests
    {
        GameObject fixture;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(fixture!=null)Object.Destroy(fixture);
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Load(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            Assert.AreEqual(zone,SceneManager.GetActiveScene().name);
            yield return new WaitForSecondsRealtime(.3f);
        }
        static Membrane Seal(string name)=>Object.FindObjectsByType<Membrane>(FindObjectsSortMode.None).Single(m=>m.name==name);
        static IEnumerator FinishDiscovery()
        {
            var dialogue=DialogueSystem.Current;dialogue.AutoAdvance=true;
            float deadline=Time.unscaledTime+8;
            while(dialogue.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(dialogue.Running,"nursery conversation did not finish");
            yield return new WaitForSecondsRealtime(.4f);
        }
        // Membrane.Open is runtime state; a loaded seal is open when nothing collides.
        static bool Passable(Membrane seal)=>seal.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled);
        static bool Walkable(Vector3 from,Vector3 to)
        {
            var path=new NavMeshPath();
            return NavMesh.SamplePosition(to,out var hit,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(from,hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        }

        [UnityTest] public IEnumerator TheCaveDescendsThroughThreeLevelsAndTheBellowsGuardsTheNursery()
        {
            yield return Load("Hushwell");
            Assert.AreEqual("Moon Caverns",MusicDirector.Current.Track,"Hushwell has no cave cue");
            var hero=PartyController.Current.Active.transform.position;
            Assert.AreEqual(HushwellLayout.Upper,hero.y,.5f,"the party does not arrive on the upper level");
            foreach(var (name,at,level) in new[]{("gallery",new Vector2(-12,48),HushwellLayout.Upper),("pressure gallery",new Vector2(0,164),HushwellLayout.Middle),
                ("Bellows chamber",HushwellLayout.Bellows,HushwellLayout.Middle),("nursery",HushwellLayout.Nursery,HushwellLayout.Lower)})
            {
                Assert.IsTrue(Physics.Raycast(new Vector3(at.x,30,at.y),Vector3.down,out var hit,80,~0,QueryTriggerInteraction.Ignore),name+" has no floor");
                Assert.AreEqual(level,hit.point.y,.3f,name+" is not on its level");
            }
            var start=HushwellLayout.OnFloor(HushwellLayout.Arrival);
            foreach(var p in HushwellLayout.Main.Where(p=>p.y<HushwellLayout.BellowsExitZ-4))
                Assert.IsTrue(Walkable(start,HushwellLayout.OnFloor(p)),"the route is blocked before the Bellows at "+p);
            Assert.IsTrue(Passable(Seal("Bellows entry")),"the Bellows chamber is sealed before the fight");
            Assert.IsFalse(Passable(Seal("Bellows exit")),"the nursery is open before the Bellows falls");
            Assert.IsFalse(Walkable(start,HushwellLayout.OnFloor(HushwellLayout.Nursery)),"the closed exit does not block the nursery");
            Seal("Bellows exit").SetOpen(true);yield return null;yield return null;
            Assert.IsTrue(Walkable(start,HushwellLayout.OnFloor(HushwellLayout.Nursery)),"the opened exit does not reach the nursery");
            var encounters=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Select(e=>e.encounterId).OrderBy(id=>id).ToArray();
            CollectionAssert.AreEqual(new[]{"Hushwell_0","Hushwell_1","Hushwell_2","Hushwell_3","Hushwell_Bellows"},encounters);
        }

        [UnityTest] public IEnumerator EveryFloorStaysInTheCameraSightline()
        {
            // The cutaway rule: from the fixed ground camera, no rock or wall face
            // hides a hero standing anywhere on the cave floor.
            yield return Load("Hushwell");
            var cave=GameObject.Find("Cave rock and floor").GetComponent<Collider>();
            var view=Quaternion.Euler(40,20,0);var hidden=new System.Collections.Generic.List<string>();int samples=0;
            for(float x=-60;x<=60;x+=2.5f)for(float z=-20;z<=360;z+=2.5f)
            {
                var p=new Vector2(x,z);if(HushwellLayout.Clear(p)>-1.2f)continue;samples++;
                var body=HushwellLayout.OnFloor(p)+Vector3.up;var eye=body-view*Vector3.forward*19.5f;
                foreach(var hit in Physics.RaycastAll(eye,body-eye,Vector3.Distance(eye,body)-.6f,~0,QueryTriggerInteraction.Ignore))
                    if(hit.collider==cave||hit.collider.name=="Grooved wall"){hidden.Add($"{p} by {hit.collider.name} at {hit.point}");break;}
            }
            Assert.Greater(samples,600,"the sightline sweep sampled too little floor");
            Assert.IsEmpty(hidden,"rock hides the hero:\n"+string.Join("\n",hidden.Take(12)));
        }

        [UnityTest] public IEnumerator TheNurseryBreathesThroughItsVents()
        {
            yield return Load("Hushwell");
            var pulses=Object.FindObjectsByType<PressurePulse>(FindObjectsSortMode.None);
            var vents=pulses.Where(p=>p.vent!=null).ToArray();var plumes=pulses.Where(p=>p.hazardRadius>0).ToArray();
            Assert.AreEqual(2,vents.Length,"both side passages need a breathing vent");Assert.GreaterOrEqual(plumes.Length,6,"the pressure gallery needs its floor vents");
            // A vent's membrane follows the shared cycle: closed on the inhale, open on the exhale.
            var vent=vents[0];bool was=vent.Open;
            Assert.AreEqual(PressurePulse.OpenAt(GameTime.Now,vent.phase),was);
            Assert.AreEqual(!was,vent.vent.GetComponentsInChildren<Collider>().All(c=>c.enabled),"membrane collision disagrees with the pulse");
            float deadline=Time.unscaledTime+PressurePulse.Period;while(vent.Open==was&&Time.unscaledTime<deadline)yield return null;
            Assert.AreNotEqual(was,vent.Open,"the vent never breathed");
            Assert.AreEqual(!vent.Open,vent.vent.GetComponentsInChildren<Collider>().All(c=>c.enabled));

            // A plume scalds whatever stands in it on the exhale, hounds included;
            // a dry ledge beside it is untouched.
            var plume=plumes.OrderBy(v=>(v.transform.position-HushwellLayout.OnFloor(new Vector2(-7,158))).sqrMagnitude).First();fixture=new GameObject("Plume fixture");
            var inPlume=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),plume.transform.position+Vector3.right*2.5f);
            var onLedge=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),plume.transform.position+Vector3.right*(plume.hazardRadius+3));
            inPlume.transform.SetParent(fixture.transform);onLedge.transform.SetParent(fixture.transform);inPlume.Passive=onLedge.Passive=true;
            while(plume.Open)yield return null;
            deadline=Time.unscaledTime+PressurePulse.Period+1;while(!plume.Open&&Time.unscaledTime<deadline)yield return null;
            yield return null;
            Assert.Less(inPlume.Health.integrity,inPlume.Health.maximum,"the exhale did not scald the hound in the plume");
            Assert.AreEqual(onLedge.Health.maximum,onLedge.Health.integrity,"the dry ledge was scalded");
        }

        [UnityTest] public IEnumerator TheBellowsIsBracedUntilBothOrgansBreak()
        {
            yield return Load("Hushwell");
            var organs=Object.FindObjectsByType<PressureOrgan>(FindObjectsSortMode.None);
            Assert.AreEqual(2,organs.Length,"the Bellows chamber needs two pressure organs");
            foreach(var organ in organs)Assert.Less(Vector2.Distance(new Vector2(organ.transform.position.x,organ.transform.position.z),HushwellLayout.Bellows),21,organ.name+" is outside the chamber");
            // Each organ blasts its own half of the chamber; breaking one makes its half safe.
            var mid=PressureOrgan.Middle;
            foreach(var organ in organs)
            {
                var side=organ.transform.position-mid;side.y=0;side.Normalize();
                Assert.IsTrue(organ.Covers(mid+side*10),organ.name+" does not blast its own half");
                Assert.IsFalse(organ.Covers(mid-side*10),organ.name+" blasts the far half too");
            }
            fixture=new GameObject("Bellows fixture");var hero=PartyController.Current.Active;
            var bellows=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("BellowsBelow"),hero.transform.position+Vector3.forward*12);
            bellows.transform.SetParent(fixture.transform);bellows.Passive=true;yield return null;yield return null;
            Assert.AreEqual("Alien Boss Battle",MusicDirector.Current.Track);
            DamagePacket Hit()=>new DamagePacket{source=hero.Health,amount=100,type=DamageType.Beam};
            float braced=bellows.Health.Receive(Hit());
            foreach(var organ in organs)organ.Health.Receive(new DamagePacket{source=hero.Health,amount=organ.Health.maximum*10,type=DamageType.Kinetic});
            Assert.IsFalse(PressureOrgan.AnyPumping,"broken organs still pump");
            yield return new WaitForSeconds(.4f);
            float soft=bellows.Health.Receive(Hit());
            Assert.AreEqual(50,braced,.01f,"the Bellows is not braced while its organs pump");
            Assert.AreEqual(100,soft,.01f,"the Bellows stays braced after both organs break");
        }

        [UnityTest] public IEnumerator TheBellowsLastPhaseCrossesTheChamberToItsRibs()
        {
            // Below 30% the Bellows leaves the centre and lunges across the chamber
            // to one of its exposed ribs, then breathes from there.
            yield return Load("Hushwell");
            var ribs=Object.FindObjectsByType<BellowsRib>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(ribs.Length,4,"the Bellows chamber has no exposed ribs");
            foreach(var rib in ribs)Assert.Less(Vector2.Distance(new Vector2(rib.Stance.x,rib.Stance.z),HushwellLayout.Bellows),21,rib.name+" is outside the chamber");
            var party=PartyController.Current;var hero=party.Active;
            foreach(var member in party.members){member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;member.Health.InvulnerableUntil=GameTime.Now+120;}
            var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.position=HushwellLayout.OnFloor(HushwellLayout.Bellows+new Vector2(6,-8));cc.enabled=true;
            fixture=new GameObject("Bellows phase fixture");
            var bellows=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("BellowsBelow"),HushwellLayout.OnFloor(HushwellLayout.Bellows));bellows.transform.SetParent(fixture.transform);
            bellows.Health.integrity=bellows.Health.maximum*.25f;
            float deadline=Time.realtimeSinceStartup+16;bool atRib=false;
            while(!atRib&&Time.realtimeSinceStartup<deadline)
            {
                yield return null;
                atRib=ribs.Any(r=>{var d=r.Stance-bellows.transform.position;d.y=0;return d.magnitude<1.5f;});
            }
            Assert.AreEqual(3,bellows.GetComponent<BossController>().Phase,"the Bellows has no last phase");
            Assert.IsTrue(atRib,"the Bellows never crossed to a rib; it ended at "+bellows.transform.position);
        }

        [UnityTest] public IEnumerator TheCaveHasItsOwnObjectivesWithinTheChapter()
        {
            // Entering starts "What the drill found"; the HUD leads to the Bellows,
            // then the nursery, then the lift; the quest completes on the discovery.
            yield return Load("Hushwell");
            var state=GameServices.Current.State;
            Assert.IsTrue(state.questSteps.ContainsKey("Hushwell"),"entering Hushwell does not start its quest");
            string Objective()=>Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMPro.TMP_Text>(true).First(t=>t.name=="Objective").text;
            yield return new WaitForSecondsRealtime(.4f);
            StringAssert.Contains("Bellows",Objective(),"the cave has no objective of its own");
            var bellows=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("BellowsBelow"),PartyController.Current.Active.transform.position+Vector3.forward*12);
            bellows.Passive=true;yield return null;
            bellows.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=bellows.Health.maximum*50,type=DamageType.Pulse});
            yield return new WaitForSecondsRealtime(.4f);
            Assert.IsTrue(GameServices.Current.Flags.GetBool("bossdown.BellowsBelow"));
            StringAssert.Contains("guarding",Objective(),"the objective does not move on to the nursery");
            Object.FindFirstObjectByType<DiscoveryPoint>().Interact();yield return FinishDiscovery();
            Assert.IsTrue(GameServices.Current.Flags.GetBool("quest.Hushwell.complete"),"finding the nursery does not complete its quest");
            StringAssert.Contains("lift",Objective());
            // The Survivor keeps the nursery thread: a third tier after the discovery.
            yield return Load("Sorrel_Ridges","Hushwell");
            string node=null;void Heard(string n,string speaker){if(speaker=="Survivor")node=n;}
            Npc.TalkRequested+=Heard;
            try{GameServices.Current.Flags.SetBool("warpkey",true);Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Single(n=>n.speaker=="Survivor").Interact();}
            finally{Npc.TalkRequested-=Heard;}
            Assert.AreEqual("SurvivorNursery",node,"the Survivor does not acknowledge the nursery");
        }

        [UnityTest] public IEnumerator TheNurseryIsFoundAndItsLiftAndTheBoreConnectSorrel()
        {
            yield return Load("Hushwell");
            var look=Object.FindFirstObjectByType<DiscoveryPoint>();Assert.IsNotNull(look,"the nursery has no discovery");
            Assert.Less(Vector3.Distance(look.transform.position,HushwellLayout.OnFloor(HushwellLayout.Nursery)),6);
            var beacons=Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None);
            var lift=beacons.Single(b=>b.requiredFlag=="hushwell.nursery");var climb=beacons.Single(b=>string.IsNullOrEmpty(b.requiredFlag));
            Assert.AreEqual(("Sorrel_Ridges","Hushwell"),(lift.scene,lift.spawn));Assert.AreEqual(("Sorrel_Ridges","Hushwell"),(climb.scene,climb.spawn));
            Assert.IsFalse(GameServices.Current.Flags.GetBool("hushwell.nursery"));
            Assert.IsTrue(look.Available);look.Interact();yield return FinishDiscovery();
            Assert.IsTrue(GameServices.Current.Flags.GetBool("hushwell.nursery"));Assert.IsFalse(look.Available,"the discovery repeats");

            yield return Load("Sorrel_Ridges","Hushwell");
            var arrival=PartyController.Current.Active.transform.position;
            Assert.Less(Vector2.Distance(new Vector2(arrival.x,arrival.z),new Vector2(8,172)),3,"Sorrel has no Hushwell arrival at the bore");
            var bore=Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).Single(b=>b.scene=="Hushwell");
            Assert.AreEqual(("bossdown.Burrower","Arrival"),(bore.requiredFlag,bore.spawn));
        }
    }
}
