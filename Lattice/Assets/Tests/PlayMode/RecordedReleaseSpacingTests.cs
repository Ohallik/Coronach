using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Lattice.Tests.PlayMode
{
    // Controlled replay of recorded leader transforms, not an earned-input run.
    // Companion movement and the animal's lethal-resolution path remain real.
    [DefaultExecutionOrder(-500)]
    public sealed class RecordedReleaseLeader:MonoBehaviour
    {
        public CombatActor actor;public float began;public bool moving;
        internal RecordedReleaseFixture.Key[] keys;
        void Update()
        {
            if(actor==null||!moving||GameTime.Paused)return;
            float age=GameTime.Now-began;
            int i=0;while(i<keys.Length-2&&keys[i+1].age<age)i++;
            var a=keys[i];var b=keys[i+1];float t=Mathf.InverseLerp(a.age,b.age,age);
            var p=Vector3.Lerp(a.position,b.position,t);var r=Quaternion.Slerp(a.rotation,b.rotation,t);
            actor.transform.SetPositionAndRotation(p,r);
            typeof(FlightMotor).GetField("<Facing>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(actor.GetComponent<FlightMotor>(),r*Vector3.forward);
            Physics.SyncTransforms();
        }
    }
    public sealed class RecordedReleaseMotor:IMotor
    {
        readonly IMotor motor;readonly PartnerBrain brain;
        public Vector2 input;public bool boost,brake;public float bodyMargin,distance;public bool wallAdjusted,detour;public Vector3 waypoint;
        public int moves;public int braked;
        public RecordedReleaseMotor(IMotor actual,PartnerBrain owner){motor=actual;brain=owner;}
        public Vector3 Facing=>motor.Facing;public Vector3 Velocity=>motor.Velocity;
        static object Field(object owner,string name)=>owner?.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);
        public void Move(Vector2 requested,bool boosted,bool braked)
        {
            input=requested;boost=boosted;brake=braked;moves++;if(braked)this.braked++;
            var guide=Field(brain,"flightSeparation");var walls=Field(brain,"flightObstacles");
            bodyMargin=float.PositiveInfinity;detour=false;wallAdjusted=false;waypoint=Vector3.zero;
            if(guide!=null)
            {
                var centres=(Vector3[])Field(guide,"centers");var radii=(float[])Field(guide,"radii");var p=brain.transform.position;p.y=0;
                for(int i=0;i<(int)Field(guide,"measuredSections");i++)bodyMargin=Mathf.Min(bodyMargin,(p-centres[i]).magnitude-radii[i]);
                detour=(bool)Field(guide,"detour");
                waypoint=detour||(bool)Field(guide,"adjustedGoal")?(Vector3)Field(guide,"waypoint"):(Vector3)Field(guide,"lastGoal");
            }
            if(walls!=null)
            {
                wallAdjusted=(bool)Field(walls,"redirected");
                if(wallAdjusted)waypoint=(Vector3)Field(walls,"waypoint");
            }
            waypoint.y=brain.transform.position.y;distance=Vector3.Distance(waypoint,brain.transform.position);
            motor.Move(requested,boosted,braked);
        }
        public void Dash(Vector3 direction,float distance)=>motor.Dash(direction,distance);
    }
    [DefaultExecutionOrder(2001)]
    public sealed class RecordedReleaseProbe:MonoBehaviour
    {
        public CombatActor actor;public EnemyBrain enemy;public RecordedReleaseMotor motor;public Renderer[] meshes;
        public readonly List<string> rows=new(){"age,x,z,vx,vz,sourceX,sourceZ,clearance,brake,boost,inputX,inputZ,bodyMargin,wayDistance,detour,wallAdjusted,wayX,wayZ"};
        public float minimum=float.PositiveInfinity;public int samples;
        public CombatActor leader;public Vector3 leaderOrigin;public float leaderExcursion;
        void LateUpdate()
        {
            if(enemy==null||actor==null||GameTime.Paused)return;
            var p=actor.transform.position;var v=motor.Velocity;var b=enemy.transform.position;float clearance=float.PositiveInfinity;
            foreach(var mesh in meshes)
            {
                if(mesh==null||!mesh.enabled)continue;var box=mesh.bounds;var at=p;at.y=box.center.y;
                clearance=Mathf.Min(clearance,Vector3.Distance(at,box.ClosestPoint(at))-HeroCollision.HullRadius(actor.character));
            }
            minimum=Mathf.Min(minimum,clearance);samples++;
            leaderExcursion=Mathf.Max(leaderExcursion,Vector3.Distance(leaderOrigin,leader.transform.position));
            rows.Add(FormattableString.Invariant($"{enemy.GetComponent<CantorRelease>().Elapsed:R},{p.x:R},{p.z:R},{v.x:R},{v.z:R},{b.x:R},{b.z:R},{clearance:R},{motor.brake},{motor.boost},{motor.input.x:R},{motor.input.y:R},{motor.bodyMargin:R},{motor.distance:R},{motor.detour},{motor.wallAdjusted},{motor.waypoint.x:R},{motor.waypoint.z:R}"));
        }
    }
    public sealed class RecordedReleaseSpacingTests
    {
        int wallContacts;
        void Record(string message,string stack,LogType type){if(message.StartsWith("WALL_CONTACT "))wallContacts++;}
        [UnitySetUp] public IEnumerator Boot()
        {
            wallContacts=0;GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Gullet_Tunnel");float until=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var encounter in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))encounter.enabled=false;
            yield return null;yield return null;
            foreach(var hero in PartyController.Current.members)
            {hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;hero.GetComponent<FlightMotor>().Halt();hero.Health.Damageable=false;}
            Application.logMessageReceived+=Record;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {Application.logMessageReceived-=Record;GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor actor,ReleaseGeometryCapture.Shape pose)
        {
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.SetPositionAndRotation(pose.position,pose.rotation);cc.enabled=true;
            var motor=actor.GetComponent<FlightMotor>();motor.Halt();
            typeof(FlightMotor).GetField("<Facing>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(motor,pose.rotation*Vector3.forward);
            typeof(FlightMotor).GetField("velocity",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(motor,pose.velocity);
        }
        IEnumerator Depart(bool moving,string companion="Sela",bool second=false,Vector3 offset=default)
        {
            var initial=second?RecordedReleaseSecondFixture.Initial:RecordedReleaseFixture.Initial;var party=PartyController.Current;
            if(party.Active.character==companion)Assert.IsTrue(party.Swap());
            var leader=party.Active;var partner=party.members.First(h=>h!=leader);
            foreach(var hero in party.members){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;}
            Assert.AreEqual(companion=="Sela"?"Taren":"Sela",leader.character);Assert.AreEqual(companion,partner.character);
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),initial.sourcePosition);enemy.Passive=true;
            yield return new WaitForSecondsRealtime(.5f);
            enemy.transform.SetPositionAndRotation(initial.sourcePosition,initial.sourceRotation);
            var serpent=enemy.GetComponent<SerpentSegments>();serpent.enabled=false;
            var head=enemy.GetComponent<DefeatPresentation>().visual;head.SetPositionAndRotation(initial.anatomy[0].position,initial.anatomy[0].rotation);
            for(int i=0;i<7;i++)serpent.Parts[i].SetPositionAndRotation(initial.anatomy[i+1].position,initial.anatomy[i+1].rotation);
            initial.heroes[1].position+=offset;
            Place(leader,initial.heroes[0]);Place(partner,initial.heroes[1]);
            var collar=enemy.GetComponent<CantorCollar>();leader.target=collar.Links[0];leader.TargetLocked=true;
            var packet=new DamagePacket{source=leader.Health,amount=100000,type=DamageType.Pulse};
            foreach(var link in collar.Links.ToArray())link.Receive(packet);enemy.Health.Receive(packet);
            Assert.IsFalse(enemy.Health.Alive);Assert.IsTrue(enemy.GetComponent<CantorRelease>().Departing);
            var anatomy=new[]{head}.Concat(serpent.Parts).SelectMany(t=>t.GetComponentsInChildren<Renderer>()).ToArray();
            Assert.AreEqual(8,anatomy.Length);
            for(int i=0;i<anatomy.Length;i++)
            {
                Assert.Less(Vector3.Distance(anatomy[i].bounds.min,initial.anatomy[i].minimum),.01f,"recorded initial geometry minimum "+i);
                Assert.Less(Vector3.Distance(anatomy[i].bounds.max,initial.anatomy[i].maximum),.01f,"recorded initial geometry maximum "+i);
            }
            var playback=leader.gameObject.AddComponent<RecordedReleaseLeader>();playback.actor=leader;playback.began=GameTime.Now;playback.moving=moving;playback.keys=second?RecordedReleaseSecondFixture.Leader:RecordedReleaseFixture.Leader;
            var brain=partner.GetComponent<PartnerBrain>();var actual=partner.motor;var capture=new RecordedReleaseMotor(actual,brain);partner.motor=capture;brain.enabled=true;
            var probe=partner.gameObject.AddComponent<RecordedReleaseProbe>();probe.actor=partner;probe.enemy=enemy;probe.motor=capture;probe.meshes=anatomy;probe.leader=leader;probe.leaderOrigin=leader.transform.position;
            Physics.SyncTransforms();float began=GameTime.Now;
            while(enemy!=null&&GameTime.Now-began<4)yield return null;
            var folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C4/release-escape"));Directory.CreateDirectory(folder);
            var path=Path.Combine(folder,"trace-"+(second?"second-":"first-")+companion+"-"+(moving?"moving":"held")+"-"+Guid.NewGuid().ToString("N")+".csv");File.WriteAllLines(path,probe.rows);
            Debug.Log($"RECORDED_RELEASE second={second} offset={offset} companion={companion} moving={moving} minimum={probe.minimum:R} samples={probe.samples} leaderExcursion={probe.leaderExcursion:R} brake={capture.braked}/{capture.moves} walls={wallContacts} trace={path}");
            partner.motor=actual;Object.Destroy(playback);
            if(moving)Assert.Greater(probe.leaderExcursion,5,"moving fixture must exercise the recorded leader excursion");
            else Assert.Less(probe.leaderExcursion,.01f,"held leader fixture drifted");
            Assert.IsTrue(enemy==null);Assert.Greater(probe.samples,140);Assert.GreaterOrEqual(probe.minimum,.25f,"recorded release crosses the companion after presentation");
            Assert.AreEqual(0,wallContacts);
            yield return new WaitForSecondsRealtime(3);
            Assert.Less(Vector3.Distance(partner.transform.position,leader.transform.position),6,"recorded release leaves companion stranded");
        }
        [UnityTest] public IEnumerator SelaClearsRecordedDepartureWithMovingLeader()=>Depart(true);
        [UnityTest] public IEnumerator SelaClearsRecordedDepartureWithHeldLeader()=>Depart(false);
        [UnityTest] public IEnumerator SelaClearsSecondDepartureWithMovingLeader()=>Depart(true,"Sela",true);
        [UnityTest] public IEnumerator SelaClearsSecondDepartureWithHeldLeader()=>Depart(false,"Sela",true);
        [UnityTest] public IEnumerator TarenClearsSecondSelaRouteWithMovingLeader()=>Depart(true,"Taren",true);
        [UnityTest] public IEnumerator TarenClearsSecondSelaRouteWithHeldLeader()=>Depart(false,"Taren",true);
        [UnityTest] public IEnumerator SelaClearsFirstNeighborLeft()=>Depart(true,"Sela",false,new Vector3(-1.5f,0,0f));
        [UnityTest] public IEnumerator SelaClearsFirstNeighborRight()=>Depart(true,"Sela",false,new Vector3(1.5f,0,0f));
        [UnityTest] public IEnumerator SelaClearsFirstNeighborBehind()=>Depart(true,"Sela",false,new Vector3(0f,0,-0.75f));
        [UnityTest] public IEnumerator SelaClearsFirstNeighborAhead()=>Depart(true,"Sela",false,new Vector3(0f,0,0.75f));
        [UnityTest] public IEnumerator TarenClearsFirstNeighborLeft()=>Depart(true,"Taren",false,new Vector3(-1.5f,0,0f));
        [UnityTest] public IEnumerator TarenClearsFirstNeighborRight()=>Depart(true,"Taren",false,new Vector3(1.5f,0,0f));
        [UnityTest] public IEnumerator TarenClearsFirstNeighborBehind()=>Depart(true,"Taren",false,new Vector3(0f,0,-0.75f));
        [UnityTest] public IEnumerator TarenClearsFirstNeighborAhead()=>Depart(true,"Taren",false,new Vector3(0f,0,0.75f));
        [UnityTest] public IEnumerator SelaClearsSecondNeighborLeft()=>Depart(true,"Sela",true,new Vector3(-1.5f,0,0f));
        [UnityTest] public IEnumerator SelaClearsSecondNeighborRight()=>Depart(true,"Sela",true,new Vector3(1.5f,0,0f));
        [UnityTest] public IEnumerator SelaClearsSecondNeighborBehind()=>Depart(true,"Sela",true,new Vector3(0f,0,-0.75f));
        [UnityTest] public IEnumerator SelaClearsSecondNeighborAhead()=>Depart(true,"Sela",true,new Vector3(0f,0,0.75f));
        [UnityTest] public IEnumerator TarenClearsSecondNeighborLeft()=>Depart(true,"Taren",true,new Vector3(-1.5f,0,0f));
        [UnityTest] public IEnumerator TarenClearsSecondNeighborRight()=>Depart(true,"Taren",true,new Vector3(1.5f,0,0f));
        [UnityTest] public IEnumerator TarenClearsSecondNeighborBehind()=>Depart(true,"Taren",true,new Vector3(0f,0,-0.75f));
        [UnityTest] public IEnumerator TarenClearsSecondNeighborAhead()=>Depart(true,"Taren",true,new Vector3(0f,0,0.75f));
        // Counterfactual hull/control coverage: Taren starts on recorded Sela path.
        [UnityTest] public IEnumerator TarenClearsRecordedSelaRouteWithMovingLeader()=>Depart(true,"Taren");
        [UnityTest] public IEnumerator TarenClearsRecordedSelaRouteWithHeldLeader()=>Depart(false,"Taren");
    }
}
