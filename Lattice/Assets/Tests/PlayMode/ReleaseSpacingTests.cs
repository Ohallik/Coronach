using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
    // Sample after the actual animal and ship presentation, independently of
    // coroutine scheduling. Keep the older checks as a separate measurement.
    [DefaultExecutionOrder(2000)]
    public sealed class ReleaseClearanceProbe:MonoBehaviour
    {
        CombatActor actor;
        Renderer[] anatomy,other;
        float minimum=float.PositiveInfinity,otherMinimum=float.PositiveInfinity,height;
        int samples,frame=-1,beforeLate,sameFrame;
        public void Bind(CombatActor hero,Renderer[] body,Renderer[] second=null)
        {actor=hero;anatomy=body;other=second;}
        public void RecordCoroutinePhase()
        {if(frame==Time.frameCount)sameFrame++;else beforeLate++;}
        static float Measure(CombatActor hero,Renderer[] body)
        {
            float result=float.PositiveInfinity;
            if(body==null)return result;
            foreach(var mesh in body)
            {
                if(mesh==null||!mesh.enabled||!mesh.gameObject.activeInHierarchy)continue;
                var box=mesh.bounds;var p=hero.transform.position;p.y=box.center.y;
                result=Mathf.Min(result,Vector3.Distance(p,box.ClosestPoint(p))-HeroCollision.HullRadius(hero.character));
            }
            return result;
        }
        void LateUpdate()
        {
            if(actor==null||GameTime.Paused)return;
            float value=Measure(actor,anatomy);if(float.IsInfinity(value))return;
            frame=Time.frameCount;samples++;minimum=Mathf.Min(minimum,value);
            otherMinimum=Mathf.Min(otherMinimum,Measure(actor,other));
            height=Mathf.Max(height,Mathf.Abs(actor.transform.position.y-1));
        }
        public void Check(string label)
        {
            Debug.Log($"RELEASE_EVALUATED label={label} clearance={minimum:R} otherClearance={otherMinimum:R} samples={samples} heightError={height:R} coroutineBeforeLate={beforeLate} coroutineSameFrame={sameFrame}");
            Assert.Greater(samples,140,"evaluated departure coverage");
            Assert.GreaterOrEqual(minimum,.25f,"evaluated companion crosses the departing animal");
            if(other!=null&&other.Length>0)Assert.GreaterOrEqual(otherMinimum,.25f,"evaluated companion crosses the new animal");
            Assert.Less(height,.1f,"evaluated departure changes flight height");
        }
    }

    // Real-resolution movement checks; original failing geometry remains unchanged.
    public sealed class ReleaseSpacingTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Flight");float until=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var hero in PartyController.Current.members)
            {hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;hero.GetComponent<FlightMotor>().Halt();hero.Health.Damageable=false;}
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor actor,Vector3 p)
        {
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=p;cc.enabled=true;
            var motor=actor.GetComponent<FlightMotor>();motor.Move(Vector2.up,false,false);motor.Halt();
        }
        static float Clearance(CombatActor actor,Renderer[] anatomy)
        {
            float closest=float.PositiveInfinity;
            foreach(var renderer in anatomy)
            {
                if(renderer==null||!renderer.enabled||!renderer.gameObject.activeInHierarchy)continue;
                var box=renderer.bounds;var p=actor.transform.position;p.y=box.center.y;
                closest=Mathf.Min(closest,Vector3.Distance(p,box.ClosestPoint(p))-HeroCollision.HullRadius(actor.character));
            }
            return closest;
        }
        IEnumerator Regroup(string companion)
        {
            var party=PartyController.Current;if(party.Active.character==companion)Assert.IsTrue(party.Swap());
            var leader=party.Active;var partner=party.members.First(h=>h!=leader);
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(200,1,0));enemy.Passive=true;
            yield return new WaitForSecondsRealtime(.5f);
            Place(leader,new Vector3(198,1,12));Place(partner,new Vector3(206,1,-2));
            var collar=enemy.GetComponent<CantorCollar>();leader.target=collar.Links[0];leader.TargetLocked=true;
            partner.GetComponent<PartnerBrain>().enabled=true;yield return new WaitForSecondsRealtime(.2f);
            var anatomy=enemy.GetComponentsInChildren<Renderer>().Where(r=>r.GetComponentInParent<Health>()==enemy.Health).ToArray();
            Assert.GreaterOrEqual(anatomy.Length,8,"fixture must measure the real head and articulated body");
            // Real lethal resolution drops the target and shuts collision/AI
            // down; the complete visible departure still occupies this space.
            var packet=new DamagePacket{source=leader.Health,amount=100000,type=DamageType.Pulse};
            foreach(var link in collar.Links.ToArray())link.Receive(packet);enemy.Health.Receive(packet);
            Assert.IsFalse(enemy.Health.Alive);Assert.IsNotNull(enemy.GetComponent<CantorRelease>());
            var probe=partner.gameObject.AddComponent<ReleaseClearanceProbe>();probe.Bind(partner,anatomy);
            float began=GameTime.Now,worst=float.PositiveInfinity,heightError=0;int samples=0,untargeted=0;
            while(enemy!=null&&GameTime.Now-began<4)
            {
                yield return null;if(enemy==null)break;
                probe.RecordCoroutinePhase();
                worst=Mathf.Min(worst,Clearance(partner,anatomy));heightError=Mathf.Max(heightError,Mathf.Abs(partner.transform.position.y-1));samples++;
                if(leader.target==null||!leader.target.Alive)untargeted++;
            }
            Debug.Log($"RELEASE_SPACING hero={companion} clearance={worst:R} samples={samples} untargeted={untargeted} heightError={heightError:R}");
            probe.Check("regroup-"+companion);
            Assert.IsTrue(enemy==null,"bounded release failed to finish");Assert.Greater(samples,140);Assert.Greater(untargeted,140);
            Assert.GreaterOrEqual(worst,.25f,"regrouping companion crosses the visibly departing animal");Assert.Less(heightError,.1f);
            yield return new WaitForSecondsRealtime(2);
            Assert.Less(Vector3.Distance(partner.transform.position,leader.transform.position),6,"cleared animal leaves companion stranded");
        }
        IEnumerator ChangedLifecycle(string mode,float bearing=0)
        {
            var party=PartyController.Current;var leader=party.Active;var partner=party.members.First(h=>h!=leader);
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(200,1,0));enemy.Passive=true;enemy.transform.rotation=Quaternion.Euler(0,bearing,0);
            EnemyBrain other=null;
            if(mode=="target"||mode=="serpent")
            {
                other=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(mode=="serpent"?"Cantor":"ChoristerDrifter"),new Vector3(188,1,0));other.Passive=true;
                other.Health.Damageable=false;
                if(other.TryGetComponent<CantorCollar>(out var otherCollar))foreach(var link in otherCollar.Links)link.Damageable=false;
            }
            yield return new WaitForSecondsRealtime(.5f);
            Place(leader,new Vector3(mode=="swap"?194:198,1,12));Place(partner,new Vector3(206,1,-2));
            if(mode=="front"){Place(leader,new Vector3(206,1,-2));Place(partner,new Vector3(194,1,12));}
            var collar=enemy.GetComponent<CantorCollar>();leader.target=collar.Links[0];leader.TargetLocked=true;
            partner.GetComponent<PartnerBrain>().enabled=true;yield return new WaitForSecondsRealtime(.2f);
            var anatomy=enemy.GetComponentsInChildren<Renderer>().Where(r=>r.GetComponentInParent<Health>()==enemy.Health).ToArray();
            Assert.GreaterOrEqual(anatomy.Length,8);
            var otherAnatomy=other!=null&&mode=="serpent"?other.GetComponentsInChildren<Renderer>().Where(r=>r.GetComponentInParent<Health>()==other.Health).ToArray():new Renderer[0];
            if(mode=="serpent")Assert.GreaterOrEqual(otherAnatomy.Length,8);
            var packet=new DamagePacket{source=leader.Health,amount=100000,type=DamageType.Pulse};
            foreach(var link in collar.Links.ToArray())link.Receive(packet);enemy.Health.Receive(packet);
            var release=enemy.GetComponent<CantorRelease>();Assert.IsTrue(release.Departing);
            if(other!=null)
            {leader.target=other.TryGetComponent<CantorCollar>(out var otherCollar)?otherCollar.Links[0]:other.Health;leader.TargetLocked=true;}
            if(mode=="swap")
            {
                Assert.IsTrue(party.Swap());leader=party.Active;partner=party.members.First(h=>h!=leader);
                foreach(var member in party.members)member.GetComponent<PlayerBrain>().AutoPilot=true;
            }
            var probe=partner.gameObject.AddComponent<ReleaseClearanceProbe>();probe.Bind(partner,anatomy,otherAnatomy);
            var rows=new List<string>{"age,x,z,vx,vz,bodyX,bodyZ,clearance,guide,wayX,wayZ,goalX,goalZ,detour,brakeRadius,centreX,centreZ,state"};
            float began=GameTime.Now,worst=float.PositiveInfinity,otherWorst=float.PositiveInfinity;int samples=0,retargeted=0;bool changed=false;
            while(enemy!=null&&GameTime.Now-began<4)
            {
                yield return null;if(enemy==null)break;
                if(!changed&&GameTime.Now-began>.45f)
                {
                    changed=true;
                    if(mode=="pause")
                    {
                        GameTime.Paused=true;yield return null;
                        var held=partner.transform.position;var body=enemy.transform.position;float clock=GameTime.Now;
                        yield return new WaitForSecondsRealtime(4);
                        Assert.AreEqual(clock,GameTime.Now);Assert.AreEqual(held,partner.transform.position);Assert.AreEqual(body,enemy.transform.position);
                        Assert.IsTrue(release.Departing,"real-time pause expired the release hazard");GameTime.Paused=false;
                    }
                    if(mode=="recover")
                    {
                        enemy.Health.Heal(enemy.Health.maximum);Assert.IsFalse(release.Departing,"recovered animal remains a departure hazard");
                        yield return new WaitForSecondsRealtime(1.6f);Assert.IsTrue(enemy.Health.Alive);Assert.IsFalse(release.Departing);
                        Object.Destroy(enemy.gameObject);yield return null;break;
                    }
                    if(mode=="remove"){Object.Destroy(enemy.gameObject);yield return null;break;}
                }
                if(mode=="swap"||mode=="front")
                {
                    var guide=typeof(PartnerBrain).GetField("flightSeparation",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(partner.GetComponent<PartnerBrain>());
                    var waypoint=guide!=null?(Vector3)guide.GetType().GetField("waypoint",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(guide):Vector3.zero;
                    var goal=guide!=null?(Vector3)guide.GetType().GetField("lastGoal",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(guide):Vector3.zero;
                    var centres=guide!=null?(Vector3[])guide.GetType().GetField("centers",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(guide):new[]{Vector3.zero};
                    var radii=guide!=null?(float[])guide.GetType().GetField("radii",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(guide):new[]{0f};
                    var detour=guide!=null?(bool)guide.GetType().GetField("detour",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(guide):false;
                    var p=partner.transform.position;var v=partner.motor.Velocity;var b=enemy.transform.position;
                    rows.Add(System.FormattableString.Invariant($"{GameTime.Now-began:R},{p.x:R},{p.z:R},{v.x:R},{v.z:R},{b.x:R},{b.z:R},{Clearance(partner,anatomy):R},{guide!=null},{waypoint.x:R},{waypoint.z:R},{goal.x:R},{goal.z:R},{detour},{radii[0]:R},{centres[0].x:R},{centres[0].z:R},{partner.State}"));
                }
                probe.RecordCoroutinePhase();
                worst=Mathf.Min(worst,Clearance(partner,anatomy));otherWorst=Mathf.Min(otherWorst,Clearance(partner,otherAnatomy));samples++;
                if(other!=null&&leader.target!=null&&leader.target.Alive)retargeted++;
            }
            if(mode=="swap"||mode=="front")
            {
                string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C4/release-spacing/"+mode+"-trace-"+System.Guid.NewGuid().ToString("N")+".csv"));
                Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllLines(path,rows);Debug.Log("RELEASE_TRACE "+path);
            }
            Debug.Log($"RELEASE_LIFECYCLE mode={mode} bearing={bearing} clearance={worst:R} otherClearance={otherWorst:R} samples={samples} retargeted={retargeted}");
            if(mode!="recover"&&mode!="remove")
            {
                probe.Check(mode+"-"+bearing);
                Assert.IsTrue(enemy==null);Assert.Greater(samples,140);Assert.GreaterOrEqual(worst,.25f,"changed control abandons the moving release body");
                if(other!=null)Assert.Greater(retargeted,140,"fixture did not retain its new live target");
                if(mode=="serpent")Assert.GreaterOrEqual(otherWorst,.25f,"release avoidance crosses the newly selected animal");
            }
            leader.target=null;leader.TargetLocked=false;
            if(other!=null)Object.Destroy(other.gameObject);
            yield return new WaitForSecondsRealtime(3);
            Assert.Less(Vector3.Distance(partner.transform.position,leader.transform.position),6,"released/removed guide leaves companion stranded");
        }
        [UnityTest] public IEnumerator PauseKeepsTheDepartureAndCompanionStill()=>ChangedLifecycle("pause");
        [UnityTest] public IEnumerator RecoveryEndsTheDeparturePhase()=>ChangedLifecycle("recover");
        [UnityTest] public IEnumerator EarlyRemovalAllowsRegrouping()=>ChangedLifecycle("remove");
        [UnityTest] public IEnumerator NewLiveTargetRetainsTheDepartureClearance()=>ChangedLifecycle("target");
        [UnityTest] public IEnumerator NewSerpentTargetRetainsTheDepartureClearance()=>ChangedLifecycle("serpent");
        [UnityTest] public IEnumerator ExistingCompanionAnticipatesTheDepartureAcrossItsRoute()=>ChangedLifecycle("front");
        [UnityTest] public IEnumerator RightFacingReleaseClearsTheCompanionRoute()=>ChangedLifecycle("front",90);
        [UnityTest] public IEnumerator ReverseFacingReleaseClearsTheCompanionRoute()=>ChangedLifecycle("front",180);
        [UnityTest] public IEnumerator LeftFacingReleaseClearsTheCompanionRoute()=>ChangedLifecycle("front",270);
        [UnityTest] public IEnumerator SwappingHeroesRetainsTheDepartureClearance()=>ChangedLifecycle("swap");
        [UnityTest] public IEnumerator SelaRegroupsOutsideTheDepartingAnimal()=>Regroup("Sela");
        [UnityTest] public IEnumerator TarenRegroupsOutsideTheDepartingAnimal()=>Regroup("Taren");
    }
}
