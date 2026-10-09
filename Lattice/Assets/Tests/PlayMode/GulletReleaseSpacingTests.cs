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
            yield return new WaitForSecondsRealtime(3);
            Assert.Less(Vector3.Distance(partner.transform.position,leader.transform.position),6,"Gullet departure leaves companion stranded");
            Assert.AreEqual(0,wallContacts);
        }
        [UnityTest] public IEnumerator SelaClearsTheEastForwardDeparture()=>Depart("Sela",20,0);
        [UnityTest] public IEnumerator SelaClearsTheEastReverseDeparture()=>Depart("Sela",20,180);
        [UnityTest] public IEnumerator TarenClearsTheWestRightDeparture()=>Depart("Taren",-20,90);
        [UnityTest] public IEnumerator TarenClearsTheWestLeftDeparture()=>Depart("Taren",-20,270);
    }
}
