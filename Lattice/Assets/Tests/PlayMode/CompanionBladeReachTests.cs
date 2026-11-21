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
    /// <summary>November 11, found standing QaPlanCombatTests' Cleave fixture up again: with his target near the top of the screen, where the place he'd come to rest
    /// has his head off it, the blade companion was sent to a place "within his reach" 2.6 m from the body and swung from 2.7 for as long as it stood there, landing
    /// nothing. A blade's place in the frame is one his swing reaches from (PartnerBrain.BladeReach); with none, he closes as he always did.</summary>
    public sealed class CompanionBladeReachTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static void Quiet(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            Physics.SyncTransforms();
        }
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        [UnityTest] public IEnumerator TheBladeCompanionComesWithinHisSwingOfATargetAtTheTopOfTheScreen()
        {
            SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"the arena never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            Quiet();yield return new WaitForSecondsRealtime(.7f);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Blade fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();
            var party=PartyController.Current;if(party.Active.character!="Sela")Assert.IsTrue(party.Swap(),"fixture: the swap");Quiet();
            var lead=party.Active;var taren=party.members.First(m=>m!=lead);
            // Nine metres up the screen from the lead: the body shows, a stride short of the top edge, and a man standing at arm's length from it has his head off it.
            Place(lead,new Vector3(200,0,-9));Place(taren,new Vector3(203,0,-9));
            var body=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),new Vector3(200,0,0));body.Passive=true;body.Health.maximum=body.Health.integrity=1000000;body.Health.breakThreshold=1e9f;
            yield return null;yield return null;yield return null;lead.target=body.Health;
            var camera=Camera.main;Assert.IsNotNull(camera,"fixture: the game's camera");
            var seen=camera.WorldToViewportPoint(body.transform.position+Vector3.up*.9f);
            Assert.IsTrue(seen.z>0&&seen.y>.8f&&seen.y<1&&seen.x>0&&seen.x<1,$"fixture: the body stands near the top of the screen, on it (at {seen.y:0.00} of its height)");
            float landed=0;body.Health.Damaged+=(_,packet,amount)=>{if(packet.source==taren.Health)landed+=amount;};
            taren.GetComponent<PartnerBrain>().enabled=true;
            // Skills out of it: this is his plain swing, which is what he was left doing.
            float nearest=float.PositiveInfinity;
            for(float end=Time.unscaledTime+6;landed<=0&&Time.unscaledTime<end;)
            {
                taren.charge=0;lead.target=body.Health;nearest=Mathf.Min(nearest,Flat(taren.transform.position,body.transform.position));yield return null;
            }
            taren.GetComponent<PartnerBrain>().enabled=false;
            Assert.Greater(landed,0,$"in six seconds beside a body at the top of the screen the blade companion landed nothing: he came no nearer than {nearest:0.0} m and swung at the air");
        }
    }
}
