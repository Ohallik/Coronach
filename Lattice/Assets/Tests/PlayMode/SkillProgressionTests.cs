using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>D201: eight skills learned with Sync level, a four-slot loadout, Ys-style mastery, the companion using
    /// skills, and each new skill doing its own job on the ground and in flight.</summary>
    public sealed class SkillProgressionTests
    {
        GameObject fixture;CombatActor taren,sela;EnemyBrain victim;
        static MemberState Member(string id)=>GameServices.Current.State.party.First(m=>m.id==id);
        static SkillDef Skill(string character,SkillKind kind)=>SkillBook.All(character).Single(s=>s.kind==kind);

        IEnumerator Boot(string zone)
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var member in PartyController.Current.members)
            {
                member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;
                if(member.character=="Taren")taren=member;else sela=member;
            }
            foreach(var m in GameServices.Current.State.party)m.level=12;
            yield return new WaitForSecondsRealtime(.7f);
            fixture=new GameObject("Skill progression fixture");
        }
        IEnumerator Ground()
        {
            yield return Boot("Arena_Ground");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixture.transform);
            floor.transform.position=new Vector3(200,-.5f,5);floor.transform.localScale=new Vector3(30,1,30);
            Place(taren,new Vector3(200,0,0));Place(sela,new Vector3(212,0,0));
            victim=Victim("Ridgehound",new Vector3(200,0,5));
            Physics.SyncTransforms();yield return null;yield return null;
        }
        IEnumerator Flight()
        {
            yield return Boot("Arena_Flight");
            Place(taren,new Vector3(200,1,0));Place(sela,new Vector3(212,1,0));
            victim=Victim("ChoristerDart",new Vector3(200,1,5));
            Physics.SyncTransforms();yield return null;yield return null;
        }
        EnemyBrain Victim(string id,Vector3 at)
        {
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;enemy.enabled=false;
            enemy.transform.SetParent(fixture.transform);enemy.Health.maximum=enemy.Health.integrity=5000;
            if(taren!=null)taren.target=enemy.Health;if(sela!=null)sela.target=enemy.Health;return enemy;
        }
        static void Place(CombatActor actor,Vector3 p)
        {
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;
            if(actor.TryGetComponent<FlightMotor>(out var ship))ship.Halt();
            actor.transform.SetPositionAndRotation(p,Quaternion.identity);cc.enabled=true;actor.TargetLocked=false;actor.charge=100;
            for(int i=0;i<4;i++)actor.cooldowns[i]=0;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {if(fixture!=null)Object.Destroy(fixture);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}

        IEnumerator Release(CombatActor actor,SkillKind kind,float seconds)
        {
            Assert.IsTrue(SkillBook.Equip(Member(actor.character),0,Skill(actor.character,kind)),kind+" could not be equipped at Sync 12");
            actor.charge=100;actor.cooldowns[0]=0;
            Assert.IsTrue(actor.Skill(0),kind+" did not start");
            float until=Time.unscaledTime+seconds;
            while(Time.unscaledTime<until){actor.motor.Move(Vector2.zero,false,false);yield return null;}
        }

        [UnityTest] public IEnumerator EachHeroLearnsFourMoreSkillsAsTheirSyncLevelRises()
        {
            yield return Boot("Arena_Ground");
            foreach(var hero in new[]{"Taren","Sela"})
            {
                var all=SkillBook.All(hero);
                Assert.AreEqual(8,all.Length,hero+" has the wrong number of skills");
                Assert.AreEqual(8,all.Select(s=>s.kind).Distinct().Count(),hero+" has two skills that do the same thing");
                for(int i=0;i<4;i++)Assert.AreEqual(GameCatalog.Find<CharacterDef>(hero).skills[i],SkillBook.Equipped(hero,i),hero+" does not start with the original four");
                foreach(var skill in all)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(skill.description),skill.id+" has no description");
                    Assert.IsNotNull(UiSkin.Skill(skill),skill.id+" has no icon");
                }
                var member=Member(hero);member.level=1;
                Assert.AreEqual(4,SkillBook.Learned(hero).Length,hero+" knows a level-gated skill at Sync 1");
                Assert.IsFalse(SkillBook.Equip(member,0,all.First(s=>s.unlockLevel>1)),"a locked skill can be equipped");
                member.level=5;Assert.AreEqual(6,SkillBook.Learned(hero).Length,hero+" at Sync 5");
                member.level=12;Assert.AreEqual(8,SkillBook.Learned(hero).Length,hero+" at Sync 12");
                CollectionAssert.AreEquivalent(new[]{3,5,8,12},all.Where(s=>s.unlockLevel>1).Select(s=>s.unlockLevel).ToArray(),hero+"'s skill pacing changed");
            }
        }

        [UnityTest] public IEnumerator EquippingTradesPlacesAndTheButtonReleasesTheChosenSkill()
        {
            yield return Ground();
            var member=Member("Taren");var faultline=Skill("Taren",SkillKind.Faultline);
            Assert.IsTrue(SkillBook.Equip(member,0,faultline));Assert.AreEqual(faultline,SkillBook.Equipped("Taren",0));
            var ember=SkillBook.Equipped("Taren",1);
            Assert.IsTrue(SkillBook.Equip(member,1,faultline));
            Assert.AreEqual(faultline,SkillBook.Equipped("Taren",1),"the chosen skill is not on its new button");
            Assert.AreEqual(ember,SkillBook.Equipped("Taren",0),"the displaced skill did not trade places");
            var released=new List<string>();void Watch(string line,string _,LogType __){if(line.StartsWith("SKILL_KIND Taren"))released.Add(line);}
            Application.logMessageReceived+=Watch;
            try{Assert.IsTrue(taren.Skill(1));}finally{Application.logMessageReceived-=Watch;}
            Assert.IsTrue(released.Any(l=>l.Contains("Faultline")),"the button released "+string.Join(",",released));
        }

        [UnityTest] public IEnumerator MasteryMakesASkillStrongerAndCheaper()
        {
            yield return Boot("Arena_Ground");
            var cleave=SkillBook.Equipped("Taren",0);
            Assert.AreEqual(1,SkillBook.Mastery("Taren",cleave.id));float cost=SkillBook.Cost("Taren",cleave);
            for(int i=0;i<SkillBook.MasteryUses[0];i++)SkillBook.RecordUse("Taren",cleave);
            Assert.AreEqual(2,SkillBook.Mastery("Taren",cleave.id),"six releases did not raise mastery");
            Assert.Less(SkillBook.Cost("Taren",cleave),cost,"mastery did not lower the Charge cost");
            Assert.Greater(SkillBook.Power(2),SkillBook.Power(1));
            for(int i=0;i<200;i++)SkillBook.RecordUse("Taren",cleave);
            Assert.AreEqual(SkillBook.MaxMastery,SkillBook.Mastery("Taren",cleave.id));
            Assert.GreaterOrEqual(SkillBook.Cost("Taren",cleave),SkillBook.MinimumCost);
        }

        [UnityTest] public IEnumerator AnchorSlamClosesTheGapAndHitsTheTarget()
        {
            yield return Ground();float start=Vector3.Distance(taren.transform.position,victim.transform.position);
            yield return Release(taren,SkillKind.AnchorSlam,1.1f);
            Assert.Less(Vector3.Distance(taren.transform.position,victim.transform.position),start-2,"Anchor Slam did not close the gap");
            Assert.Less(victim.Health.integrity,5000,"Anchor Slam did not hit its target");
        }

        [UnityTest] public IEnumerator TetherHookReelsASmallEnemyInAndStaggersIt()
        {
            yield return Ground();victim.transform.position=new Vector3(200,0,9);Physics.SyncTransforms();
            yield return Release(taren,SkillKind.TetherHook,1.2f);
            Assert.Less(Vector3.Distance(taren.transform.position,victim.transform.position),3.2f,"the hound was not reeled in");
            Assert.Less(victim.Health.integrity,5000,"the hook did not hit");
            Assert.IsTrue(victim.Health.Broken,"the reeled enemy is not left open");
        }

        [UnityTest] public IEnumerator ForgeSpiralHitsTheSameEnemySeveralTimes()
        {
            yield return Ground();victim.transform.position=new Vector3(200,0,2);Physics.SyncTransforms();
            int hits=0;System.Action<Health,DamagePacket,float> count=(_,p,__)=>{if(p.source==taren.Health)hits++;};victim.Health.Damaged+=count;
            yield return Release(taren,SkillKind.ForgeSpiral,1.4f);victim.Health.Damaged-=count;
            Assert.GreaterOrEqual(hits,3,"the spin landed "+hits+" hits");
        }

        [UnityTest] public IEnumerator FaultlineReachesAnEnemyTenMetresAway()
        {
            yield return Ground();victim.transform.position=new Vector3(200,0,10);Physics.SyncTransforms();
            yield return Release(taren,SkillKind.Faultline,1.2f);
            Assert.Less(victim.Health.integrity,5000,"Faultline did not reach ten metres");
        }

        [UnityTest] public IEnumerator SurveyDroneKeepsFiringAfterSelaMovesOn()
        {
            yield return Ground();Place(sela,new Vector3(200,0,-2));sela.target=victim.Health;
            yield return Release(sela,SkillKind.SurveyDrone,.8f);
            Assert.IsNotNull(SurveyDrone.Current,"no drone was deployed");
            float before=victim.Health.integrity;yield return new WaitForSecondsRealtime(2.5f);
            Assert.GreaterOrEqual(SurveyDrone.Current.Shots,3,"the drone fired "+SurveyDrone.Current.Shots+" shots");
            Assert.Less(victim.Health.integrity,before,"the drone's shots did no damage");
        }

        [UnityTest] public IEnumerator WaypointBlinkEscapesAndLeavesAChargeThatBursts()
        {
            // Taren stands clear of the blink's path: a body behind Sela would stop her, as it should.
            yield return Ground();Place(taren,new Vector3(190,0,0));Place(sela,new Vector3(200,0,3.5f));Physics.SyncTransforms();sela.target=victim.Health;var origin=sela.transform.position;
            yield return Release(sela,SkillKind.WaypointBlink,1.2f);
            Assert.Greater(Vector3.Distance(sela.transform.position,origin),4,"Sela did not blink away");
            Assert.Less(victim.Health.integrity,5000,"the waypoint charge did not burst on the enemy beside it");
        }

        [UnityTest] public IEnumerator GravityFoldDragsEnemiesTogetherThenImplodes()
        {
            yield return Ground();Place(sela,new Vector3(200,0,-4));
            var second=Victim("Ridgehound",new Vector3(204.5f,0,5));sela.target=victim.Health;
            float before=Vector3.Distance(victim.transform.position,second.transform.position);
            yield return Release(sela,SkillKind.GravityFold,2.9f);
            Assert.Less(Vector3.Distance(victim.transform.position,second.transform.position),before-2,"the well did not gather the two hounds");
            Assert.Less(second.Health.integrity,5000,"the fold did no damage to the gathered hound");
        }

        [UnityTest] public IEnumerator StarfallStrikesTheMarkedCircleAfterItsWarning()
        {
            yield return Ground();Place(sela,new Vector3(200,0,-3));sela.target=victim.Health;
            Assert.IsTrue(SkillBook.Equip(Member("Sela"),0,Skill("Sela",SkillKind.Starfall)));Assert.IsTrue(sela.Skill(0));
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(5000,victim.Health.integrity,.01f,"Starfall struck before its warning ran out");
            yield return new WaitForSecondsRealtime(1.4f);
            Assert.Less(victim.Health.integrity,5000,"Starfall missed its marked target");
        }

        [UnityTest] public IEnumerator EveryLearnedSkillHasAWorkingFlightVariant()
        {
            yield return Flight();
            foreach(var (hero,kind) in new[]{(taren,SkillKind.AnchorSlam),(taren,SkillKind.TetherHook),(taren,SkillKind.ForgeSpiral),(taren,SkillKind.Faultline),
                (sela,SkillKind.SurveyDrone),(sela,SkillKind.WaypointBlink),(sela,SkillKind.GravityFold),(sela,SkillKind.Starfall)})
            {
                Place(hero,new Vector3(200,1,0));victim.transform.position=new Vector3(200,1,kind==SkillKind.WaypointBlink?2.5f:5);victim.Health.ResetFull();Physics.SyncTransforms();
                hero.target=victim.Health;
                yield return Release(hero,kind,kind==SkillKind.SurveyDrone?2.5f:kind==SkillKind.GravityFold?2.9f:1.6f);
                Assert.Less(victim.Health.integrity,5000,kind+" did nothing in flight");
                foreach(var drone in Object.FindObjectsByType<SurveyDrone>(FindObjectsSortMode.None))Object.Destroy(drone.gameObject);
                yield return null;
            }
        }

        [UnityTest] public IEnumerator TheCompanionSpendsChargeOnItsSkills()
        {
            yield return Ground();Place(sela,new Vector3(203,0,-2));
            PartyController.Current.Swap();yield return null;if(PartyController.Current.Active!=taren){PartyController.Current.Swap();yield return null;}
            Assert.AreEqual(taren,PartyController.Current.Active);taren.target=victim.Health;
            var partner=sela.GetComponent<PartnerBrain>();partner.enabled=true;sela.charge=100;
            var released=new List<string>();void Watch(string line,string _,LogType __){if(line.StartsWith("SKILL_KIND Sela"))released.Add(line);}
            Application.logMessageReceived+=Watch;
            try{float until=Time.unscaledTime+8;while(Time.unscaledTime<until&&released.Count==0){sela.charge=100;yield return null;}}
            finally{Application.logMessageReceived-=Watch;}
            Assert.IsNotEmpty(released,"the companion never used a skill with full Charge and a target in reach");
        }

        [UnityTest] public IEnumerator TheSkillsPageSwapsASkillOntoAButton()
        {
            yield return Ground();
            var state=GameServices.Current.State;Assert.AreEqual("Taren",state.party[state.activeMember].id,"fixture expects Taren to lead");
            var pause=Object.FindFirstObjectByType<PauseMenu>();pause.Open(PauseMenu.SkillsPage);yield return null;
            Row("Row_1").onClick.Invoke();yield return null;
            var rows=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.name.StartsWith("Row_")).ToArray();
            var anchor=rows.First(b=>b.GetComponentInChildren<TMPro.TMP_Text>().text.Contains("Anchor Slam"));
            anchor.onClick.Invoke();yield return null;
            Assert.AreEqual(SkillKind.AnchorSlam,SkillBook.Equipped("Taren",0).kind,"choosing Anchor Slam for the first slot did not equip it");
            pause.Close();
        }
        static Button Row(string name)=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name==name);
    }
}
