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
    public sealed class CompanionSpacingTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var hero in PartyController.Current.members)
            {hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;hero.GetComponent<FlightMotor>().Halt();}
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor actor,Vector3 position)
        {var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=position;cc.enabled=true;actor.GetComponent<FlightMotor>().Halt();}
        static float Clearance(CombatActor actor,Renderer[] anatomy)
        {
            float result=float.PositiveInfinity;
            foreach(var mesh in anatomy)
            {
                if(!mesh.enabled||!mesh.gameObject.activeInHierarchy)continue;
                var bounds=mesh.bounds;var point=actor.transform.position;point.y=bounds.center.y;
                result=Mathf.Min(result,Vector3.Distance(point,bounds.ClosestPoint(point))-HeroCollision.HullRadius(actor.character));
            }
            return result;
        }
        IEnumerator Approach(int linkIndex,string companion)
        {
            var party=PartyController.Current;
            if(party.Active.character==companion)Assert.IsTrue(party.Swap());
            var leader=party.Active;leader.GetComponent<PlayerBrain>().AutoPilot=true;
            var partner=party.members.First(a=>a!=leader);
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(200,1,0));
            enemy.Passive=true;enemy.transform.rotation=Quaternion.Euler(0,180,0);
            yield return new WaitForSecondsRealtime(.5f);
            var target=enemy.GetComponent<CantorCollar>().Links[linkIndex];
            target.maximum=target.integrity=1000000;
            Place(leader,new Vector3(200,1,target.transform.position.z-12));
            Place(partner,new Vector3(202,1,target.transform.position.z-9));
            leader.target=target;leader.TargetLocked=true;
            // The renderer envelope is independent of the AI goal and existing
            // disabled anatomy colliders. Keep actual moving segment geometry.
            var anatomy=enemy.GetComponentsInChildren<Renderer>().Where(r=>r is MeshRenderer||r is SkinnedMeshRenderer).ToArray();
            float began=GameTime.Now;
            int hits=0,settledHits=0,samples=0;
            target.Damaged+=(_,__,___)=>{hits++;if(GameTime.Now-began>4)settledHits++;};
            partner.GetComponent<PartnerBrain>().enabled=true;
            float worst=float.PositiveInfinity,heightError=0;
            while(GameTime.Now-began<8)
            {
                yield return null;
                if(GameTime.Now-began>4)
                {worst=Mathf.Min(worst,Clearance(partner,anatomy));heightError=Mathf.Max(heightError,Mathf.Abs(partner.transform.position.y-1));samples++;}
            }
            Debug.Log($"COMPANION_SPACING hero={companion} link={linkIndex} clearance={worst:R} hits={hits} settledHits={settledHits} heightError={heightError:R} samples={samples} position={partner.transform.position}");
            Assert.Greater(samples,100,"fixture lacks sustained settled flight samples");
            Assert.GreaterOrEqual(worst,.25f,companion+" flight hull enters the visible animal envelope at link "+linkIndex);
            Assert.GreaterOrEqual(hits,4,companion+" cannot damage the actual selected link from its firing position");
            Assert.GreaterOrEqual(settledHits,4,companion+" stops landing shots after settling beside the link");
            Assert.Less(heightError,.1f,companion+" is pushed off the flight plane beside the link");
        }
        [UnityTest] public IEnumerator SelaClearsTheSweepLink()=>Approach(0,"Sela");
        [UnityTest] public IEnumerator SelaClearsTheChorusLink()=>Approach(1,"Sela");
        [UnityTest] public IEnumerator SelaClearsTheVolleyLink()=>Approach(2,"Sela");
        [UnityTest] public IEnumerator TarenClearsTheSweepLink()=>Approach(0,"Taren");
        [UnityTest] public IEnumerator TarenClearsTheChorusLink()=>Approach(1,"Taren");
        [UnityTest] public IEnumerator TarenClearsTheVolleyLink()=>Approach(2,"Taren");
    }
}
