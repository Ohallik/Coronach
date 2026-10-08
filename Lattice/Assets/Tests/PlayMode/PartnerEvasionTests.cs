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

        IEnumerator ShotAtPartner(string zone,float y,bool duringFlash=false,bool checkHullClearance=false)
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
            int rolls=partner.GetComponent<FlightMotor>().DashSequence;
            var motor=partner.GetComponent<FlightMotor>();float hullClearance=float.PositiveInfinity;
            void DashMoved(int sequence,Vector3 start,Vector3 end)
            {
                var separation=end-leader.transform.position;separation.y=0;
                hullClearance=Mathf.Min(hullClearance,separation.magnitude-HeroCollision.HullRadius(leader.character)-HeroCollision.HullRadius(partner.character));
            }
            if(checkHullClearance)motor.DashMoved+=DashMoved;
            if(duringFlash)GameTime.BeginFlash();
            var target=partner.transform.position+Vector3.up*.5f;var from=target+Vector3.right*9;
            Projectile.Fire(from,target-from,new DamagePacket{source=shooter.Health,amount=14,type=DamageType.Beam},11);
            var trace=duringFlash?new System.Text.StringBuilder("seconds,scale,x,z,vx,vz,health,state,rolls,shotX,shotZ,target\n"):null;
            for(float t=0;t<(duringFlash?4.5f:1.6f);t+=Time.unscaledDeltaTime)
            {
                if(partner.State==ActorState.Dodge)rolled=true;
                if(trace!=null)
                {
                    var shotPosition=new Vector3(float.NaN,0,float.NaN);
                    foreach(var shot in Projectile.Live)if(shot.Threatens(partner.Health.friendly)){shotPosition=shot.transform.position;break;}
                    var pos=partner.transform.position;var velocity=partner.motor.Velocity;
                    trace.AppendLine(System.FormattableString.Invariant($"{t:R},{Time.timeScale:R},{pos.x:R},{pos.z:R},{velocity.x:R},{velocity.z:R},{partner.Health.integrity:R},{partner.State},{partner.GetComponent<FlightMotor>().DashSequence-rolls},{shotPosition.x:R},{shotPosition.z:R},{partner.target?.id}"));
                }
                yield return null;
            }
            if(trace!=null)Debug.Log("PARTNER_EVASION_TRACE\n"+trace+"END_PARTNER_EVASION_TRACE");
            if(checkHullClearance)
            {
                motor.DashMoved-=DashMoved;Debug.Log($"PARTY_ROLL_CLEARANCE value={hullClearance:R}");
                Assert.That(hullClearance,Is.InRange(.1f,20f),"the roll scrapes the leader's collision hull");
            }
            Assert.IsTrue(rolled,$"the {zone} partner never rolled from the incoming shot");
            Assert.AreEqual(before,partner.Health.integrity,$"the {zone} partner was hit by a shot it could see coming");
            if(duringFlash)Assert.AreEqual(1,partner.GetComponent<FlightMotor>().DashSequence-rolls,"one slowed projectile makes the flight partner roll repeatedly");
        }
        [UnityTest] public IEnumerator TheFlightPartnerRollsAwayFromAnIncomingShot()=>ShotAtPartner("Arena_Flight",1);
        [UnityTest] public IEnumerator TheGroundPartnerSidestepsAnIncomingShot()=>ShotAtPartner("Arena_Ground",0);
        [UnityTest] public IEnumerator OneSlowedProjectileNeedsOneFlightRoll()=>ShotAtPartner("Arena_Flight",1,true);
        [UnityTest] public IEnumerator TheFlightPartnerRollsClearOfTheLeadersHull()=>ShotAtPartner("Arena_Flight",1,true,true);
        [UnityTest] public IEnumerator TheFlightPartnerUsesOneLateRollForASlowedTell()
        {
            SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            var party=PartyController.Current;var leader=party.Active;var partner=party.members[1-party.index];
            leader.GetComponent<PlayerBrain>().AutoPilot=true;
            foreach(var member in party.members)
            {
                var cc=member.GetComponent<CharacterController>();cc.enabled=false;
                member.transform.position=new Vector3(member==leader?200:202,1,member==leader?0:-3);
                cc.enabled=true;member.GetComponent<FlightMotor>().Halt();
            }
            yield return new WaitForSecondsRealtime(.8f);
            fixture=new GameObject("Slowed visible tell",typeof(Health));
            fixture.transform.position=partner.transform.position+Vector3.forward*8;
            var boss=fixture.AddComponent<BossController>();boss.enabled=false;
            leader.target=fixture.GetComponent<Health>();
            const System.Reflection.BindingFlags access=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            GameTime.BeginFlash();
            typeof(BossController).GetField("busy",access).SetValue(boss,true);
            typeof(BossController).GetField("strikeAt",access).SetValue(boss,Time.time+.15f);
            int sequence=partner.GetComponent<FlightMotor>().DashSequence;float before=partner.Health.integrity;
            while(boss.Telegraphing)yield return null;
            partner.Health.Receive(new DamagePacket{source=leader.target,amount=14,type=DamageType.Kinetic});
            Assert.AreEqual(before,partner.Health.integrity,"the slow tell outlasted the partner's dodge window");
            Assert.AreEqual(1,partner.GetComponent<FlightMotor>().DashSequence-sequence,"one slowed tell makes the partner roll repeatedly");
        }
    }
}
