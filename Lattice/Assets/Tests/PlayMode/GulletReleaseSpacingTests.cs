using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
    [DefaultExecutionOrder(2002)]
    public sealed class GulletRegroupTrace:MonoBehaviour
    {
        CombatActor actor,leader;PartnerBrain brain;IMotor original;RecordedReleaseMotor motor;float began;
        readonly List<string> rows=new(){"age,x,z,vx,vz,leaderX,leaderZ,distance,inputX,inputZ,boost,brake,wallAdjusted,wallX,wallZ,goalX,goalZ,state,guide"};
        static object Field(object owner,string name)=>owner?.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);
        public void Bind(CombatActor who,CombatActor target)
        {actor=who;leader=target;brain=who.GetComponent<PartnerBrain>();original=who.motor;motor=new RecordedReleaseMotor(original,brain);who.motor=motor;began=Time.unscaledTime;}
        void LateUpdate()
        {
            if(actor==null||leader==null)return;var p=actor.transform.position;var v=motor.Velocity;var l=leader.transform.position;
            var walls=Field(brain,"flightObstacles");var w=walls!=null?(Vector3)Field(walls,"waypoint"):Vector3.zero;
            var g=walls!=null?(Vector3)Field(walls,"lastGoal"):Vector3.zero;
            rows.Add(System.FormattableString.Invariant($"{Time.unscaledTime-began:R},{p.x:R},{p.z:R},{v.x:R},{v.z:R},{l.x:R},{l.z:R},{Vector3.Distance(p,l):R},{motor.input.x:R},{motor.input.y:R},{motor.boost},{motor.brake},{motor.wallAdjusted},{w.x:R},{w.z:R},{g.x:R},{g.z:R},{actor.State},{Field(brain,"flightSeparation")!=null}"));
        }
        public void Write(string label)
        {
            var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C8/cantor-composition/regroup-"+label+"-"+System.Guid.NewGuid().ToString("N")+".csv"));
            File.WriteAllLines(path,rows);Debug.Log("GULLET_REGROUP_TRACE "+path);
        }
        void OnDestroy(){if(actor!=null&&ReferenceEquals(actor.motor,motor))actor.motor=original;}
    }
    public sealed class GulletReleaseSpacingTests
    {
        int wallContacts;
        void Record(string message,string stack,LogType type){if(message.StartsWith("WALL_CONTACT "))wallContacts++;}
        [UnitySetUp] public IEnumerator Boot()
        {
            wallContacts=0;GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Gullet_Tunnel");float until=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            // Controlled scene-geometry fixture, not an ordinary earned fight.
            // Cancel dormant/automatic encounters before positioning the pair.
            foreach(var encounter in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))encounter.enabled=false;
            yield return null;yield return null;
            foreach(var hero in PartyController.Current.members)
            {hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;hero.GetComponent<FlightMotor>().Halt();hero.Health.Damageable=false;}
            Application.logMessageReceived+=Record;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {Application.logMessageReceived-=Record;GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor actor,Vector3 p)
        {
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=p;cc.enabled=true;
            var motor=actor.GetComponent<FlightMotor>();motor.Move(Vector2.up,false,false);motor.Halt();
        }
        static float Clearance(CombatActor actor,Renderer[] anatomy)
        {
            float minimum=float.PositiveInfinity;
            foreach(var mesh in anatomy)
            {
                if(mesh==null||!mesh.enabled||!mesh.gameObject.activeInHierarchy)continue;
                var b=mesh.bounds;var p=actor.transform.position;p.y=b.center.y;
                minimum=Mathf.Min(minimum,Vector3.Distance(p,b.ClosestPoint(p))-HeroCollision.HullRadius(actor.character));
            }
            return minimum;
        }
        static object Field(object owner,string name)=>owner?.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);
        IEnumerator Depart(string companion,float x,float bearing)
        {
            var party=PartyController.Current;if(party.Active.character==companion)Assert.IsTrue(party.Swap());
            var leader=party.Active;var partner=party.members.First(h=>h!=leader);var origin=new Vector3(x,1,820);
            foreach(var member in party.members){member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;}
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),origin);enemy.Passive=true;enemy.transform.rotation=Quaternion.Euler(0,bearing,0);
            yield return new WaitForSecondsRealtime(.5f);
            Place(leader,origin+new Vector3(6,0,-2));Place(partner,origin+new Vector3(-6,0,12));
            var collar=enemy.GetComponent<CantorCollar>();leader.target=collar.Links[0];leader.TargetLocked=true;
            Debug.Log("GULLET_CONTROLLED_ENEMIES "+string.Join(";",Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>e.definition.id=="Cantor").Select(e=>$"id={e.GetInstanceID()} passive={e.Passive} position={e.transform.position}")));
            partner.GetComponent<PartnerBrain>().enabled=true;yield return new WaitForSecondsRealtime(.2f);
            var anatomy=enemy.GetComponentsInChildren<Renderer>().Where(r=>r.GetComponentInParent<Health>()==enemy.Health).ToArray();Assert.GreaterOrEqual(anatomy.Length,8);
            var packet=new DamagePacket{source=leader.Health,amount=100000,type=DamageType.Pulse};
            foreach(var link in collar.Links.ToArray())link.Receive(packet);enemy.Health.Receive(packet);Assert.IsFalse(enemy.Health.Alive);
            var probe=partner.gameObject.AddComponent<ReleaseClearanceProbe>();probe.Bind(partner,anatomy);
            var rows=new List<string>{"age,x,z,vx,vz,bodyX,bodyZ,clearance,wayX,wayZ,goalX,goalZ,detour,wallRedirected,wallX,wallZ,future1X,future1Z,future2X,future2Z,future3X,future3Z,future4X,future4Z,future5X,future5Z"};
            float began=GameTime.Now,worst=float.PositiveInfinity,heightError=0;int samples=0;
            while(enemy!=null&&GameTime.Now-began<4)
            {
                yield return null;if(enemy==null)break;
                var brain=partner.GetComponent<PartnerBrain>();var guide=Field(brain,"flightSeparation");var walls=Field(brain,"flightObstacles");
                var waypoint=guide!=null?(Vector3)Field(guide,"waypoint"):Vector3.zero;
                var goal=guide!=null?(Vector3)Field(guide,"lastGoal"):Vector3.zero;
                var wallPoint=walls!=null?(Vector3)Field(walls,"waypoint"):Vector3.zero;
                var centres=guide!=null?(Vector3[])Field(guide,"centers"):new Vector3[13];
                var p=partner.transform.position;var v=partner.motor.Velocity;var b=enemy.transform.position;
                string row=System.FormattableString.Invariant($"{GameTime.Now-began:R},{p.x:R},{p.z:R},{v.x:R},{v.z:R},{b.x:R},{b.z:R},{Clearance(partner,anatomy):R},{waypoint.x:R},{waypoint.z:R},{goal.x:R},{goal.z:R},{Field(guide,"detour")},{Field(walls,"redirected")},{wallPoint.x:R},{wallPoint.z:R}");
                for(int i=8;i<13;i++){var point=i<centres.Length?centres[i]:Vector3.zero;row+=System.FormattableString.Invariant($",{point.x:R},{point.z:R}");}
                rows.Add(row);
                probe.RecordCoroutinePhase();
                worst=Mathf.Min(worst,Clearance(partner,anatomy));heightError=Mathf.Max(heightError,Mathf.Abs(partner.transform.position.y-1));samples++;
            }
            var path=Path.GetFullPath(Path.Combine(Application.dataPath,$"../../Builds/quality/C4/release-spacing/gullet-trace-{companion}-{x}-{bearing}-"+System.Guid.NewGuid().ToString("N")+".csv"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllLines(path,rows);Debug.Log("GULLET_RELEASE_TRACE "+path);
            Debug.Log($"GULLET_RELEASE_SPACING hero={companion} x={x} bearing={bearing} clearance={worst:R} samples={samples} walls={wallContacts} heightError={heightError:R}");
            probe.Check("Gullet-"+companion+"-"+x+"-"+bearing);
            Assert.IsTrue(enemy==null);Assert.Greater(samples,140);Assert.GreaterOrEqual(worst,.25f,"companion crosses the actual Gullet departure");
            Assert.Less(heightError,.1f);Assert.AreEqual(0,wallContacts,"release guidance routes the companion into real tissue");
            var regroup=partner.gameObject.AddComponent<GulletRegroupTrace>();regroup.Bind(partner,leader);
            yield return new WaitForSecondsRealtime(3);
            regroup.Write(companion+"-"+x+"-"+bearing);
            Assert.Less(Vector3.Distance(partner.transform.position,leader.transform.position),6,"Gullet departure leaves companion stranded");
            Assert.AreEqual(0,wallContacts);
        }
        [UnityTest] public IEnumerator DisabledEncounterDoesNotReactivateOnPhysicsEntry()
        {
            var encounter=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="Gullet_Cantor");
            Assert.IsFalse(encounter.enabled);Assert.IsFalse(encounter.Started);
            foreach(var hero in PartyController.Current.members)Place(hero,encounter.transform.position+Vector3.right*(hero.character=="Sela"?4:-4));
            yield return new WaitForSecondsRealtime(.3f);
            var animals=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>e.definition.id=="Cantor").ToArray();
            Debug.Log($"DISABLED_ENCOUNTER_ENTRY enabled={encounter.enabled} started={encounter.Started} animals={animals.Length} live={animals.Count(e=>e.enabled&&!e.Passive)}");
            Assert.IsFalse(encounter.Started,"disabled encounter starts from a physics callback");
            Assert.IsEmpty(animals,"disabled entry spawns a second uncontrolled animal");
            encounter.enabled=true;yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(encounter.Started,"re-enabled entry does not resume for a hero already inside");
            animals=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>e.definition.id=="Cantor").ToArray();
            Assert.AreEqual(1,animals.Length);Assert.IsTrue(animals[0].enabled);Assert.IsFalse(animals[0].Passive);
        }
        IEnumerator RegroupFromRecordedExit(string companion)
        {
            var party=PartyController.Current;if(party.Active.character==companion)Assert.IsTrue(party.Swap());
            var leader=party.Active;var partner=party.members.First(h=>h!=leader);
            foreach(var hero in party.members){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;}
            Place(leader,new Vector3(26,1,818));Place(partner,new Vector3(1.491014f,1,836.876831f));
            leader.target=null;leader.TargetLocked=false;
            var trace=partner.gameObject.AddComponent<GulletRegroupTrace>();trace.Bind(partner,leader);
            partner.GetComponent<PartnerBrain>().enabled=true;
            yield return new WaitForSecondsRealtime(3);trace.Write(companion+"-recorded-exit");
            Assert.Less(Vector3.Distance(partner.transform.position,leader.transform.position),6,"recorded exit regroup leaves companion stranded");
            Assert.AreEqual(0,wallContacts);Assert.Less(Mathf.Abs(partner.transform.position.y-1),.1f);
        }
        [UnityTest] public IEnumerator SelaRegroupsFromTheRecordedReverseExit()=>RegroupFromRecordedExit("Sela");
        [UnityTest] public IEnumerator TarenRegroupsFromTheRecordedSelaReverseExit()=>RegroupFromRecordedExit("Taren");
        [UnityTest] public IEnumerator SelaClearsTheEastForwardDeparture()=>Depart("Sela",20,0);
        [UnityTest] public IEnumerator SelaClearsTheEastReverseDeparture()=>Depart("Sela",20,180);
        [UnityTest] public IEnumerator TarenClearsTheWestRightDeparture()=>Depart("Taren",-20,90);
        [UnityTest] public IEnumerator TarenClearsTheWestLeftDeparture()=>Depart("Taren",-20,270);
    }
}
