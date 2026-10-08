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
    public sealed class CompanionTravelTests
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
            {hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;hero.GetComponent<FlightMotor>().Halt();hero.Health.Damageable=false;}
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor actor,Vector3 p)
        {var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=p;cc.enabled=true;actor.GetComponent<FlightMotor>().Halt();}
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
        IEnumerator Travel(string companion,bool regroup)
        {
            var party=PartyController.Current;if(party.Active.character==companion)Assert.IsTrue(party.Swap());
            var leader=party.Active;var partner=party.members.First(a=>a!=leader);
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(200,1,0));
            enemy.Passive=true;enemy.transform.rotation=Quaternion.Euler(0,180,0);
            yield return new WaitForSecondsRealtime(.5f);
            var links=enemy.GetComponent<CantorCollar>().Links;
            foreach(var link in links){link.maximum=link.integrity=1000000;link.breakThreshold=1000000;}
            var anatomy=enemy.GetComponentsInChildren<Renderer>().Where(r=>r is MeshRenderer||r is SkinnedMeshRenderer).ToArray();
            Place(leader,new Vector3(212,1,-4));Place(partner,new Vector3(206,1,2));
            leader.target=links[0];leader.TargetLocked=true;
            int hits=0,samples=0,remote=0,turns=0;float worst=float.PositiveInfinity,heightError=0;
            foreach(var link in links)link.Damaged+=(_,__,___)=>hits++;
            float began=GameTime.Now;var previous=enemy.transform.rotation;
            partner.GetComponent<PartnerBrain>().enabled=true;enemy.Passive=regroup;
            while(GameTime.Now-began<24)
            {
                float age=GameTime.Now-began;
                // The leader uses its real flight motor. The live carrier turns
                // toward it; the alternative leaves the animal stationary and
                // takes the leader across the far side to demand regrouping.
                var goal=regroup?(age<5?new Vector3(212,1,-4):new Vector3(180,1,12)):
                    new Vector3(200+Mathf.Cos(age*.5f)*4,1,Mathf.Sin(age*.5f)*4);
                var delta=goal-leader.transform.position;
                leader.motor.Move(new Vector2(delta.x,delta.z).normalized,false,true);
                leader.target=links[Mathf.Min(2,(int)(age/8))];
                yield return null;
                if(Quaternion.Angle(previous,enemy.transform.rotation)>.2f)turns++;
                previous=enemy.transform.rotation;
                if(GameTime.Now-began<4)continue;
                worst=Mathf.Min(worst,Clearance(partner,anatomy));
                heightError=Mathf.Max(heightError,Mathf.Abs(partner.transform.position.y-1));samples++;
                if(Vector3.Distance(partner.transform.position,leader.transform.position)>=18)remote++;
            }
            Debug.Log($"COMPANION_TRAVEL hero={companion} regroup={regroup} clearance={worst:R} hits={hits} heightError={heightError:R} samples={samples} remote={remote} turns={turns}");
            Assert.Greater(samples,500,"fixture needs sustained travel, including both target changes");
            if(regroup)Assert.Greater(remote,30,"fixture never exercises return to a distant leader");
            else Assert.Greater(turns,100,"fixture never exercises a live turning carrier");
            Assert.GreaterOrEqual(worst,.25f,companion+" crosses visible anatomy during turns, target changes or regrouping");
            Assert.GreaterOrEqual(hits,4,"avoidance must preserve actual link attacks");
            Assert.Less(heightError,.1f,"avoidance must retain the flight plane");
        }
        [UnityTest] public IEnumerator SelaClearsALiveTurningCarrier()=>Travel("Sela",false);
        [UnityTest] public IEnumerator TarenClearsALiveTurningCarrier()=>Travel("Taren",false);
        [UnityTest] public IEnumerator SelaRegroupsAroundTheWholeAnimal()=>Travel("Sela",true);
        [UnityTest] public IEnumerator TarenRegroupsAroundTheWholeAnimal()=>Travel("Taren",true);
        [UnityTest] public IEnumerator RedirectedFlankStillAimsAtTheSelectedLink()
        {
            var party=PartyController.Current;if(party.Active.character=="Sela")Assert.IsTrue(party.Swap());
            var leader=party.Active;var partner=party.members.First(a=>a!=leader);
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(200,1,0));
            enemy.Passive=true;yield return new WaitForSecondsRealtime(.5f);
            var chain=enemy.GetComponent<SerpentSegments>();chain.enabled=false;
            var points=new[]{new Vector2(0,2.2f),new Vector2(1.2f,4.1f),new Vector2(3.2f,5.1f),
                new Vector2(5.4f,4.6f),new Vector2(6.6f,2.8f),new Vector2(7.5f,.8f),new Vector2(6.8f,-1.3f)};
            var previous=enemy.transform.position+Vector3.up*SerpentSegments.CenterHeight;
            for(int i=0;i<points.Length;i++)
            {
                var joint=chain.Parts[i];joint.position=new Vector3(200+points[i].x,2.5f,points[i].y);
                joint.rotation=Quaternion.LookRotation(previous-joint.position);previous=joint.position;
            }
            var link=enemy.GetComponent<CantorCollar>().Links[0];link.maximum=link.integrity=1000000;link.breakThreshold=1000000;
            Place(leader,new Vector3(200,1,-6));Place(partner,new Vector3(206,1,-12));
            leader.target=link;leader.TargetLocked=true;
            var anatomy=enemy.GetComponentsInChildren<Renderer>().Where(r=>r is MeshRenderer||r is SkinnedMeshRenderer).ToArray();
            int hits=0,samples=0;float worst=float.PositiveInfinity,bestAim=-1;
            float began=GameTime.Now;link.Damaged+=(_,__,___)=>{if(GameTime.Now-began>6)hits++;};
            partner.GetComponent<PartnerBrain>().enabled=true;
            while(GameTime.Now-began<12)
            {
                yield return null;worst=Mathf.Min(worst,Clearance(partner,anatomy));
                if(GameTime.Now-began<6)continue;samples++;
                var aim=link.transform.position-partner.transform.position;aim.y=0;
                bestAim=Mathf.Max(bestAim,Vector3.Dot(partner.motor.Facing,aim.normalized));
            }
            Debug.Log($"COMPANION_REDIRECT hits={hits} clearance={worst:R} bestAim={bestAim:R} samples={samples} position={partner.transform.position}");
            Assert.Greater(samples,250);Assert.GreaterOrEqual(worst,.25f,"redirected travel enters anatomy");
            Assert.GreaterOrEqual(hits,4,"settled redirected firing position never lands ordinary attacks");
            Assert.Greater(bestAim,.95f,"ship never aims toward selected link after reaching safe position");
        }
        [UnityTest] public IEnumerator LiveCarrierDoesNotFoldNonAdjacentBodySectionsTogether()
        {
            var leader=PartyController.Current.Active;
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(200,1,0));
            enemy.Passive=true;enemy.transform.rotation=Quaternion.Euler(0,180,0);
            yield return new WaitForSecondsRealtime(.5f);
            var chain=enemy.GetComponent<SerpentSegments>();Place(leader,new Vector3(204,1,0));
            float began=GameTime.Now,minimum=float.PositiveInfinity,moved=0,maximumBend=0;int samples=0,turns=0;
            var previousPosition=enemy.transform.position;var previousRotation=enemy.transform.rotation;
            enemy.Passive=false;
            while(GameTime.Now-began<30)
            {
                // A close circle exercises retreat and changes of bearing
                // together, using the actual ship motor throughout.
                float age=GameTime.Now-began;
                var goal=new Vector3(200+Mathf.Cos(age*.5f)*4,1,Mathf.Sin(age*.5f)*4);
                var delta=goal-leader.transform.position;leader.motor.Move(new Vector2(delta.x,delta.z).normalized,false,true);
                yield return null;samples++;
                moved+=Vector3.Distance(previousPosition,enemy.transform.position);previousPosition=enemy.transform.position;
                if(Quaternion.Angle(previousRotation,enemy.transform.rotation)>.2f)turns++;previousRotation=enemy.transform.rotation;
                var ahead=enemy.transform.position;var heading=enemy.transform.forward;
                foreach(var part in chain.Parts)
                {
                    var tangent=ahead-part.position;tangent.y=0;
                    maximumBend=Mathf.Max(maximumBend,Vector3.Angle(heading,tangent));heading=tangent;ahead=part.position;
                }
                for(int a=0;a<8;a++)for(int b=a+3;b<8;b++)
                {
                    var p=a==0?enemy.transform.position:chain.Parts[a-1].position;
                    var q=chain.Parts[b-1].position;var separation=p-q;separation.y=0;
                    minimum=Mathf.Min(minimum,separation.magnitude);
                }
            }
            Debug.Log($"SERPENT_SELF_CLEARANCE minimum={minimum:R} moved={moved:R} turns={turns} samples={samples} maximumBend={maximumBend:R}");
            Assert.Greater(samples,1400);Assert.Greater(moved,10,"fixture must exercise actual carrier travel");
            Assert.Greater(turns,100,"fixture must exercise actual carrier turns");
            Assert.GreaterOrEqual(minimum,2.5f,"non-neighboring body sections fold through one another");
            Assert.Less(maximumBend,70,"head or adjacent joints fold sharply back through the following section");
        }
        IEnumerator EvadeBesideAnimal(string companion)
        {
            var party=PartyController.Current;if(party.Active.character==companion)Assert.IsTrue(party.Swap());
            var leader=party.Active;var partner=party.members.First(a=>a!=leader);
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(200,1,0));
            enemy.Passive=true;enemy.transform.rotation=Quaternion.Euler(0,180,0);
            yield return new WaitForSecondsRealtime(.5f);
            Place(leader,new Vector3(212,1,-4));Place(partner,new Vector3(206,1,2));
            leader.target=enemy.GetComponent<CantorCollar>().Links[0];leader.TargetLocked=true;
            partner.GetComponent<PartnerBrain>().enabled=true;yield return new WaitForSecondsRealtime(1.5f);
            partner.Health.Damageable=true;float health=partner.Health.integrity;
            var motor=partner.GetComponent<FlightMotor>();int before=motor.DashSequence;
            var anatomy=enemy.GetComponentsInChildren<Renderer>().Where(r=>r is MeshRenderer||r is SkinnedMeshRenderer).ToArray();
            // The default perpendicular points into the animal. The partner
            // must use a clear ordinary roll and still evade the real shot.
            Projectile.Fire(partner.transform.position+Vector3.up*.5f+Vector3.forward*9,Vector3.back,
                new DamagePacket{source=enemy.Health,amount=14,type=DamageType.Beam},11);
            float began=GameTime.Now,worst=float.PositiveInfinity;int samples=0;
            while(GameTime.Now-began<2.2f)
            {yield return null;worst=Mathf.Min(worst,Clearance(partner,anatomy));samples++;}
            Debug.Log($"COMPANION_BODY_EVASION hero={companion} clearance={worst:R} rolls={motor.DashSequence-before} health={partner.Health.integrity:R} samples={samples}");
            Assert.Greater(motor.DashSequence,before,"partner fails to use an available ordinary roll");
            Assert.AreEqual(health,partner.Health.integrity,"safe route fails to evade the real incoming shot");
            Assert.Greater(samples,100);
            Assert.GreaterOrEqual(worst,.25f,"roll crosses the visible animal");
        }
        [UnityTest] public IEnumerator SelaEvadesWithoutRollingThroughTheAnimal()=>EvadeBesideAnimal("Sela");
        [UnityTest] public IEnumerator TarenEvadesWithoutRollingThroughTheAnimal()=>EvadeBesideAnimal("Taren");
        [UnityTest] public IEnumerator TurningTheHeadDoesNotTeleportTheTail()
        {
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(200,1,0));
            enemy.Passive=true;yield return new WaitForSecondsRealtime(.5f);
            var chain=enemy.GetComponent<SerpentSegments>();var tail=chain.Parts[6];
            var before=tail.position;enemy.transform.rotation=Quaternion.Euler(0,90,0);
            yield return null;yield return null;
            var displacement=tail.position-before;displacement.y=0;
            Debug.Log($"SERPENT_TURN tailDisplacement={displacement.magnitude:R}");
            Assert.Less(displacement.magnitude,.25f,"head yaw teleports the trailing tail through nearby ships");
        }
        [UnityTest] public IEnumerator TranslatingTheHeadPullsTheChainInsteadOfTeleportingIt()
        {
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(200,1,0));
            enemy.Passive=true;yield return new WaitForSecondsRealtime(.5f);
            var chain=enemy.GetComponent<SerpentSegments>();var tail=chain.Parts[6];
            var before=tail.position;enemy.transform.position+=Vector3.forward*2;
            yield return null;yield return null;
            var displacement=tail.position-before;displacement.y=0;
            Debug.Log($"SERPENT_TRANSLATE tailDisplacement={displacement.magnitude:R}");
            Assert.Less(displacement.magnitude,.25f,"head translation teleports the trailing tail");
            float began=GameTime.Now;
            while(GameTime.Now-began<4){enemy.transform.position+=Vector3.forward*(2*Time.deltaTime);yield return null;}
            displacement=tail.position-before;displacement.y=0;
            Assert.Greater(displacement.magnitude,4,"tail stays frozen while the carrier travels");
            var previous=enemy.transform.position+Vector3.up*SerpentSegments.CenterHeight;
            foreach(var joint in chain.Parts)
            {Assert.That(Vector3.Distance(previous,joint.position),Is.InRange(1.9f,2.7f),"travelling chain disconnects");previous=joint.position;}
        }
    }
}
