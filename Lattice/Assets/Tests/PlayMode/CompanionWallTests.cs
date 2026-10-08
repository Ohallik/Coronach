using System.Collections;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class CompanionWallTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Gullet_Tunnel");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var encounter in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))encounter.GetComponent<Collider>().enabled=false;
            foreach(var hero in PartyController.Current.members)
            {hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;hero.GetComponent<FlightMotor>().Halt();}
            yield return new WaitForSecondsRealtime(.3f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor actor,Vector3 point)
        {
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.SetPositionAndRotation(point,Quaternion.identity);cc.enabled=true;
            actor.GetComponent<FlightMotor>().Halt();actor.target=null;actor.TargetLocked=false;
        }
        IEnumerator Travel(string companion,bool fold,bool switchTarget=false,bool acrossFold=false)
        {
            var party=PartyController.Current;if(party.Active.character==companion)Assert.IsTrue(party.Swap());
            var leader=party.Active;var partner=party.members.First(a=>a!=leader);
            var origin=fold?new Vector3(2.9f,1,349.5f):new Vector3(8.2f,1,397.5f);
            Place(leader,fold?new Vector3(.91556f,1,349.4799f):new Vector3(13.42f,1,392.6f));Place(partner,origin);
            if(acrossFold){Place(leader,new Vector3(0,1,355));Place(partner,new Vector3(-5.2f,1,355));}
            leader.GetComponent<FlightMotor>().FaceTarget(Vector3.forward);
            EnemyBrain target=null;int hits=0;
            if(fold)
            {
                target=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDrifter"),switchTarget?new Vector3(4.5f,1,368):new Vector3(-6.2f,1,367.5f));
                target.Passive=true;target.Health.maximum=target.Health.integrity=1000000;target.Health.breakThreshold=1000000;
                if(acrossFold)target.transform.position=new Vector3(-8,1,369);
                target.Health.Damaged+=(victim,__,___)=>{if(victim==leader.target)hits++;};leader.target=target.Health;leader.TargetLocked=true;
            }
            EnemyBrain second=null;
            if(switchTarget)
            {
                second=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDrifter"),new Vector3(-7.5f,1,366));second.Passive=true;
                second.Health.maximum=second.Health.integrity=1000000;second.Health.breakThreshold=1000000;
                var cc=second.GetComponent<CharacterController>();Physics.SyncTransforms();
                Assert.IsFalse(Physics.OverlapSphere(second.transform.position+cc.center,cc.radius,~0,QueryTriggerInteraction.Ignore).Any(CombatCover.Opaque),"new target fixture is inside solid tissue");
            }
            float initial=partner.Health.integrity,travel=0,closest=float.PositiveInfinity;int samples=0,contacts=0;
            var previous=partner.transform.position;
            Application.LogCallback observe=(message,stack,type)=>{if(message.StartsWith("WALL_CONTACT "+partner.name+" "))contacts++;};
            Application.logMessageReceived+=observe;
            try
            {
                partner.GetComponent<PartnerBrain>().enabled=true;float began=GameTime.Now;
                while(GameTime.Now-began<12)
                {
                    float age=GameTime.Now-began;
                    if(second!=null&&age>=4)
                    {target=second;second=null;hits=0;target.Health.Damaged+=(_,__,___)=>hits++;leader.target=target.Health;}
                    if(!fold)leader.motor.Move(age>=1&&age<5?Vector2.up:Vector2.zero,false,true);
                    yield return null;samples++;travel+=Vector3.Distance(previous,partner.transform.position);previous=partner.transform.position;
                    closest=Mathf.Min(closest,Vector3.Distance(partner.transform.position,fold?target.transform.position:leader.transform.position));
                }
            }
            finally{Application.logMessageReceived-=observe;}
            Debug.Log($"COMPANION_WALL hero={companion} fold={fold} switch={switchTarget} samples={samples} travel={travel:R} closest={closest:R} hits={hits} contacts={contacts} health={initial:R}->{partner.Health.integrity:R} final={partner.transform.position}");
            Assert.Greater(samples,500);Assert.Greater(travel,5,"fixture must move the actual craft");
            Assert.Less(closest,fold?8:6,"avoiding walls must still reach the target or leader");
            if(fold)Assert.GreaterOrEqual(hits,4,"wall avoidance must preserve real attacks");
            Assert.AreEqual(0,contacts,"ordinary companion travel collides with tissue");
            Assert.AreEqual(initial,partner.Health.integrity,"companion loses integrity to real wall contact");
        }
        [UnityTest] public IEnumerator SelaPursuesAroundTheActualThroatFold()=>Travel("Sela",true);
        [UnityTest] public IEnumerator TarenPursuesAroundTheActualThroatFold()=>Travel("Taren",true);
        [UnityTest] public IEnumerator SelaFormsUpInsideTheActualEddyShell()=>Travel("Sela",false);
        [UnityTest] public IEnumerator TarenFormsUpInsideTheActualEddyShell()=>Travel("Taren",false);
        [UnityTest] public IEnumerator SelaChangesTargetsBesideTheActualFold()=>Travel("Sela",true,true);
        [UnityTest] public IEnumerator TarenChangesTargetsBesideTheActualFold()=>Travel("Taren",true,true);
        [UnityTest] public IEnumerator SelaPursuesTheFarSideOfTheActualFold()=>Travel("Sela",true,false,true);
        [UnityTest] public IEnumerator TarenPursuesTheFarSideOfTheActualFold()=>Travel("Taren",true,false,true);
    }
}
