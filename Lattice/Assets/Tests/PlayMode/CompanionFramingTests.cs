using System.Collections;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class CompanionFramingTests
    {
        CombatActor hero,partner;
        EnemyBrain enemy;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var other in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))other.Passive=true;
            var party=PartyController.Current;hero=party.Active;partner=party.members.First(a=>a!=hero);
            foreach(var member in party.members)
            {member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;member.GetComponent<FlightMotor>().Halt();}
            Place(hero,new Vector3(30,1,-17));Place(partner,new Vector3(32,1,-23));
            enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(30,1,0));
            enemy.Passive=true;enemy.transform.rotation=Quaternion.Euler(0,180,0);
            hero.target=enemy.GetComponent<CantorCollar>().Links[0];hero.TargetLocked=true;
            yield return new WaitForSecondsRealtime(2);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;actor.transform.position=position;body.enabled=true;
            actor.GetComponent<FlightMotor>().Halt();
        }
        static void Check(CombatActor actor,string label)=>FlightFramingTests.CheckMeshes(actor.GetComponent<FormController>().flight,label);
        [UnityTest] public IEnumerator NearbyCompanionSharesTheEncounterAndReleaseFrame()
        {
            Check(hero,"active hero before release");Check(partner,"nearby companion before release");
            var packet=new DamagePacket{source=hero.Health,amount=1000000,type=DamageType.Kinetic};
            foreach(var link in enemy.GetComponent<CantorCollar>().Links)link.GetComponent<Hurtbox>().Hit(packet);
            enemy.GetComponent<Hurtbox>().Hit(packet);float began=GameTime.Now;int frames=0;
            while(GameTime.Now-began<1.8f)
            {
                yield return null;frames++;
                Check(hero,"active hero during release");Check(partner,"nearby companion during release");
                if(GameTime.Now-began<1.5f)FlightFramingTests.CheckMeshes(enemy.gameObject,"animal during framed introduction");
            }
            Assert.Greater(frames,50);
        }
        [UnityTest] public IEnumerator NearbyDownedCompanionRemainsInTheCombatFrame()
        {
            partner.Health.Receive(new DamagePacket{source=enemy.Health,amount=1000000,type=DamageType.Kinetic});
            Assert.IsFalse(partner.Health.Alive);yield return new WaitForSecondsRealtime(1.4f);
            Assert.IsFalse(partner.Health.Alive,"fixture automatically revived the distant downed companion");
            Check(hero,"active hero beside downed companion");Check(partner,"nearby disabled craft");
        }
        /// <summary>Past the hold, until the view hasn't moved for half a second.</summary>
        static IEnumerator Still()
        {
            yield return new WaitForSecondsRealtime(CameraRig.ComeInHold+.1f);
            var last=Camera.main.transform.position;float since=Time.unscaledTime,deadline=Time.unscaledTime+8;
            while(Time.unscaledTime-since<.5f&&Time.unscaledTime<deadline)
            {
                yield return null;var now=Camera.main.transform.position;
                if(Vector3.Distance(last,now)>.0005f){last=now;since=Time.unscaledTime;}
            }
            Assert.Less(Time.unscaledTime,deadline,"fixture: the view never came to rest with the companion gone");
        }
        [UnityTest] public IEnumerator DistantCompanionDoesNotStretchTheEncounterFrame()
        {
            enemy.GetComponent<SerpentSegments>().enabled=false;
            // With the nearby companion gone the view has less to hold. Since November 10 it stays as far back as it was for CameraRig.ComeInHold and only then comes in, at a
            // limited pace (the Gullet's pumping), so two seconds on it was still half way in: the rest of that, 5.7 m, was measured here as the remote companion's doing.
            // The view is judged once it stands still (Builds/quality/S1/companion-framing-trace.txt), and the remote companion may still not move it a quarter metre.
            partner.gameObject.SetActive(false);yield return Still();
            var expected=Camera.main.transform.position;
            Place(partner,new Vector3(230,1,-23));partner.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(2);
            Assert.Less(Vector3.Distance(expected,Camera.main.transform.position),.25f,"remote companion pulls combat framing across the arena");
            Check(hero,"active hero with remote companion");FlightFramingTests.CheckMeshes(enemy.gameObject,"target with remote companion");
        }
    }
}
