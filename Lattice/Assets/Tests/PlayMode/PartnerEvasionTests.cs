using System.Collections;
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
    /// <summary>The AI partner reads incoming fire as a player would: a hostile shot
    /// about to strike it is rolled away from, in flight and on the ground.</summary>
    public sealed class PartnerEvasionTests
    {
        GameObject fixture;
        [UnitySetUp] public IEnumerator Boot()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");}
        [UnityTearDown] public IEnumerator Cleanup()
        {if(fixture!=null)Object.Destroy(fixture);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}

        IEnumerator ShotAtPartner(string zone,float y)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            var party=PartyController.Current;var leader=party.Active;var partner=party.members[1-party.index];
            leader.GetComponent<PlayerBrain>().AutoPilot=true;
            void Place(CombatActor a,Vector3 p){var cc=a.GetComponent<CharacterController>();cc.enabled=false;a.transform.SetPositionAndRotation(p,Quaternion.identity);cc.enabled=true;}
            // The partner settles on its formation point behind the leader, clear of the arena.
            Place(leader,new Vector3(200,y,-200));Place(partner,new Vector3(202,y,-203));
            yield return new WaitForSecondsRealtime(1.2f);
            fixture=new GameObject("Evasion fixture");
            var shooter=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDrifter"),new Vector3(260,y,-260));shooter.transform.SetParent(fixture.transform);shooter.Passive=true;
            float before=partner.Health.integrity;bool rolled=false;
            var target=partner.transform.position+Vector3.up*.5f;var from=target+Vector3.right*9;
            Projectile.Fire(from,target-from,new DamagePacket{source=shooter.Health,amount=14,type=DamageType.Beam},11);
            for(float t=0;t<1.6f;t+=Time.unscaledDeltaTime){if(partner.State==ActorState.Dodge)rolled=true;yield return null;}
            Assert.IsTrue(rolled,$"the {zone} partner never rolled from the incoming shot");
            Assert.AreEqual(before,partner.Health.integrity,$"the {zone} partner was hit by a shot it could see coming");
        }
        [UnityTest] public IEnumerator TheFlightPartnerRollsAwayFromAnIncomingShot()=>ShotAtPartner("Arena_Flight",1);
        [UnityTest] public IEnumerator TheGroundPartnerSidestepsAnIncomingShot()=>ShotAtPartner("Arena_Ground",0);
    }
}
