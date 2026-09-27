using System.Collections;
using System.Collections.Generic;
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
    [DefaultExecutionOrder(1000)]
    public sealed class NetBirthObserver:MonoBehaviour
    {
        public Animator rig;
        public readonly List<float> errors=new(),phases=new();
        readonly HashSet<NetSeed> seen=new();
        void LateUpdate()
        {
            foreach(var seed in Object.FindObjectsByType<NetSeed>(FindObjectsSortMode.None))
            {
                if(!seen.Add(seed))continue;
                errors.Add(Vector3.Distance(seed.transform.position,rig.GetBoneTransform(HumanBodyBones.RightHand).position));
                var state=rig.GetCurrentAnimatorStateInfo(0);
                if(rig.IsInTransition(0)&&rig.GetNextAnimatorStateInfo(0).IsName("Pulse"))state=rig.GetNextAnimatorStateInfo(0);
                phases.Add(state.normalizedTime);
            }
        }
    }
    public sealed class GroundSkillTests
    {
        GameObject fixture;CombatActor taren,sela;EnemyBrain victim;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var member in PartyController.Current.members)
            {
                member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;
                if(member.character=="Taren")taren=member;else if(member.character=="Sela")sela=member;
            }
            yield return new WaitForSecondsRealtime(.7f);
            fixture=new GameObject("Ground skill fixture");var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(fixture.transform);floor.transform.position=new Vector3(200,-.5f,5);floor.transform.localScale=new Vector3(20,1,20);
            Place(taren,new Vector3(200,0,0));Place(sela,new Vector3(208,0,0));
            victim=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),new Vector3(200,0,4));victim.Passive=true;victim.enabled=false;
            victim.transform.SetParent(fixture.transform);victim.Health.maximum=victim.Health.integrity=1000;
            taren.target=victim.Health;sela.target=victim.Health;Physics.SyncTransforms();yield return null;yield return null;
        }
        static void Place(CombatActor actor,Vector3 p)
        {var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.SetPositionAndRotation(p,Quaternion.identity);cc.enabled=true;actor.TargetLocked=false;actor.charge=100;}
        GameObject Cover(float z)
        {var g=new GameObject("Skill cover",typeof(BoxCollider));g.transform.SetParent(fixture.transform);g.transform.position=new Vector3(200,1,z);g.GetComponent<BoxCollider>().size=new Vector3(6,4,.2f);g.isStatic=true;Physics.SyncTransforms();return g;}
        [UnityTearDown] public IEnumerator Cleanup()
        {if(fixture!=null)Object.Destroy(fixture);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        [UnityTest] public IEnumerator EmberDashCannotDamageBeyondTheWallThatStopsItsBody()
        {
            var cover=Cover(2);Assert.IsTrue(taren.Skill(1));float deadline=Time.unscaledTime+1;
            while(Time.unscaledTime<deadline){taren.motor.Move(Vector2.zero,false,false);yield return null;}
            Assert.Less(taren.transform.position.z,2,"fixture wall did not stop the dash");
            Assert.AreEqual(1000,victim.Health.integrity,"dash damaged an enemy beyond its blocking wall");
            cover.SetActive(false);Place(taren,new Vector3(200,0,0));taren.cooldowns[1]=0;Physics.SyncTransforms();
            Assert.IsTrue(taren.Skill(1));deadline=Time.unscaledTime+1;
            while(Time.unscaledTime<deadline){taren.motor.Move(Vector2.zero,false,false);yield return null;}
            Assert.Greater(taren.transform.position.z,3,"unobstructed dash failed to travel");
            Assert.Less(victim.Health.integrity,1000,"unobstructed dash failed to hit the same target");
        }
        [UnityTest] public IEnumerator PulseRequiresAnUnobstructedPathToItsVictim()
        {
            var cover=Cover(2);Assert.IsTrue(taren.Skill(2));yield return new WaitForSecondsRealtime(.8f);
            Assert.AreEqual(1000,victim.Health.integrity,"Pulse bypassed solid cover");
            cover.SetActive(false);taren.cooldowns[2]=0;taren.charge=100;Physics.SyncTransforms();
            Assert.IsTrue(taren.Skill(2));yield return new WaitForSecondsRealtime(.8f);
            Assert.Less(victim.Health.integrity,1000,"unobstructed Pulse failed to hit the same target");
        }
        [UnityTest] public IEnumerator StaticNetCannotAppearThroughCoverAtADistantTarget()
        {
            Place(taren,new Vector3(208,0,0));Place(sela,new Vector3(200,0,0));victim.transform.position=new Vector3(200,0,8);Physics.SyncTransforms();
            var cover=Cover(2);Assert.IsTrue(sela.Skill(2));yield return new WaitForSecondsRealtime(1);
            Assert.AreEqual(1000,victim.Health.integrity,"Static Net teleported through cover to its remote target");
            foreach(var field in Object.FindObjectsByType<PulseField>(FindObjectsSortMode.None))
            {
                Assert.Less(field.transform.position.z,2,"field appeared on the far side of solid cover");
                var surface=field.GetComponentInChildren<MeshFilter>();Assert.IsNotNull(surface,"field needs a visible ground surface");
                foreach(var vertex in surface.sharedMesh.vertices)
                    Assert.Less(surface.transform.TransformPoint(vertex).z,2.02f,"visible field advertises damage beyond its blocking wall");
            }
            foreach(var field in Object.FindObjectsByType<PulseField>(FindObjectsSortMode.None))Object.Destroy(field.gameObject);
            cover.SetActive(false);sela.cooldowns[2]=0;sela.charge=100;Physics.SyncTransforms();
            Assert.IsTrue(sela.Skill(2));yield return new WaitForSecondsRealtime(1.3f);
            Assert.Less(victim.Health.integrity,1000,"unobstructed Static Net failed to reach the same target");
        }
        [UnityTest] public IEnumerator NearThrowCoverCannotBeBypassedByTheAnimatedHand()
        {
            Place(taren,new Vector3(208,0,0));Place(sela,new Vector3(200,0,0));
            foreach(float distance in new[]{.38f,.45f,.65f})
            {
                var cover=Cover(distance);sela.charge=100;sela.cooldowns[2]=0;Assert.IsTrue(sela.Skill(2));yield return new WaitForSecondsRealtime(1);
                Assert.AreEqual(1000,victim.Health.integrity,$"throwing wrist spawned energy beyond close cover at {distance}");
                foreach(var field in Object.FindObjectsByType<PulseField>(FindObjectsSortMode.None))
                {Assert.Less(field.transform.position.z,distance,"field passed the cover between body and wrist");Object.Destroy(field.gameObject);}
                Object.Destroy(cover);yield return null;
            }
            sela.charge=100;sela.cooldowns[2]=0;Assert.IsTrue(sela.Skill(2));yield return new WaitForSecondsRealtime(1);
            Assert.Less(victim.Health.integrity,1000,"unobstructed release must still damage the same target");
        }
        [UnityTest] public IEnumerator NetIsReleasedAtTheForwardHandAndHasABoundedThrowRange()
        {
            Place(taren,new Vector3(208,0,0));Place(sela,new Vector3(200,0,0));victim.transform.position=new Vector3(200,0,20);Physics.SyncTransforms();
            var observer=sela.gameObject.AddComponent<NetBirthObserver>();observer.rig=sela.GetComponentInChildren<Animator>();
            Assert.IsTrue(sela.Skill(2));yield return new WaitForSecondsRealtime(1);
            Assert.AreEqual(1,observer.errors.Count,"a throw must create one visible seed");
            Assert.Less(observer.errors[0],.025f,"seed detached from the actual posed throwing hand");
            Assert.That(observer.phases[0],Is.InRange(.24f,.35f),"release is outside the forward hand pass");
            Assert.AreEqual(1000,victim.Health.integrity,"distant target pulled the field beyond its range");
            var fields=Object.FindObjectsByType<PulseField>(FindObjectsSortMode.None);Assert.AreEqual(1,fields.Length);
            Assert.That(fields[0].transform.position.z,Is.InRange(5.5f,6.2f));
            TestContext.WriteLine($"NET handError={observer.errors[0]:F6} phase={observer.phases[0]:F6} landing={fields[0].transform.position}");
        }
        [UnityTest] public IEnumerator PulseDamageFollowsTheVisibleWaveAndHitsOnce()
        {
            int hits=0;float contactRadius=0,visibleRadius=0;float delay=0;float started=GameTime.Now;
            // Find the actual bright crest in the owned texture, then locate
            // that UV in the rendered mesh. A padded texture on a radius-sized
            // quad/fan can appear much smaller than the numeric damage radius.
            var texture=new Texture2D(2,2);texture.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(Application.dataPath,"_Project/Art/Particles/circle_02.png")));
            int peak=texture.height/2;float alpha=0;
            for(int y=texture.height/2;y<texture.height;y++)if(texture.GetPixel(texture.width/2,y).a>alpha){alpha=texture.GetPixel(texture.width/2,y).a;peak=y;}
            var crestUv=new Vector2(.5f,(peak+.5f)/texture.height);Object.Destroy(texture);
            var legacy=new GameObject("Legacy padded ring",typeof(MeshFilter));legacy.transform.SetParent(fixture.transform);
            var legacyMesh=new Mesh();legacyMesh.vertices=new[]{new Vector3(-5,0,-5),new Vector3(-5,0,5),new Vector3(5,0,5),new Vector3(5,0,-5)};
            legacyMesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};legacyMesh.triangles=new[]{0,1,2,0,2,3};legacy.GetComponent<MeshFilter>().sharedMesh=legacyMesh;
            float legacyError=Mathf.Abs(VisibleRadius(legacy.GetComponent<MeshFilter>(),crestUv,legacy.transform.position)-5);
            Assert.Greater(legacyError,.12f,"legacy padded surface must fail the same visible crest bound");
            TestContext.WriteLine($"LEGACY_RING_REJECTED crestError={legacyError:F6}");Object.Destroy(legacyMesh);Object.Destroy(legacy);
            victim.Health.Damaged+=(_,__,___)=>
            {
                hits++;var wave=Object.FindFirstObjectByType<PulseWave>();Assert.IsNotNull(wave,"hidden damage without a visible wave");contactRadius=wave.Radius;delay=GameTime.Now-started;
                var mesh=wave.GetComponentInChildren<MeshFilter>();Assert.IsNotNull(mesh);
                visibleRadius=VisibleRadius(mesh,crestUv,wave.transform.position);
                Assert.Less(Mathf.Abs(visibleRadius-wave.Radius),.12f,"bright visible crest does not match the damage wave");
            };
            Assert.IsTrue(taren.Skill(2));yield return new WaitForSecondsRealtime(.8f);
            Assert.AreEqual(1,hits,"one wave must hit each victim once");
            Assert.That(contactRadius,Is.InRange(3.9f,5));Assert.Greater(delay,.35f,"Pulse dealt damage before its cast and wave reached the target");
            TestContext.WriteLine($"PULSE radius={contactRadius:F6} visibleCrest={visibleRadius:F6} delay={delay:F6} hits={hits}");
        }
        static float VisibleRadius(MeshFilter mesh,Vector2 crestUv,Vector3 origin)
        {
            var uv=mesh.sharedMesh.uv;var vertices=mesh.sharedMesh.vertices;var triangles=mesh.sharedMesh.triangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];var e=uv[b]-uv[a];var f=uv[c]-uv[a];var p=crestUv-uv[a];
                float det=e.x*f.y-e.y*f.x;if(Mathf.Abs(det)<.0000001f)continue;
                float u=(p.x*f.y-p.y*f.x)/det,v=(e.x*p.y-e.y*p.x)/det;
                if(u<-.0001f||v<-.0001f||u+v>1.0001f)continue;
                var point=mesh.transform.TransformPoint(vertices[a]+u*(vertices[b]-vertices[a])+v*(vertices[c]-vertices[a]));
                var offset=point-origin;offset.y=0;return offset.magnitude;
            }
            Assert.Fail("particle mesh does not contain its bright crest UV");return float.PositiveInfinity;
        }
        [UnityTest] public IEnumerator GuardAndStaggerCancelUnreleasedDashPulseAndNet()
        {
            for(int i=0;i<6;i++)
            {
                var actor=i%3==2?sela:taren;int slot=i%3==0?1:2;actor.charge=100;actor.cooldowns[slot]=0;
                var before=actor.transform.position;Assert.IsTrue(actor.Skill(slot));
                if(i<3)actor.Guard(true);else actor.Stagger(.3f);
                yield return new WaitForSecondsRealtime(.8f);
                Assert.AreEqual(1000,victim.Health.integrity,$"cancel {i} left hidden damage");
                Assert.Less(Vector3.Distance(before,actor.transform.position),.01f,$"cancel {i} left dash travel");
                Assert.AreEqual(0,Object.FindObjectsByType<NetSeed>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(0,Object.FindObjectsByType<PulseField>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(0,Object.FindObjectsByType<PulseWave>(FindObjectsSortMode.None).Length);
                actor.Guard(false);
            }
        }
        [UnityTest] public IEnumerator PauseHoldsCastAndReleasedEnergyThenResumesOneEffect()
        {
            Assert.IsTrue(taren.Skill(2));GameTime.Paused=true;yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(0,Object.FindObjectsByType<PulseWave>(FindObjectsSortMode.None).Length);Assert.AreEqual(1000,victim.Health.integrity);
            GameTime.Paused=false;float deadline=Time.unscaledTime+1;PulseWave wave=null;
            while(wave==null&&Time.unscaledTime<deadline){yield return null;wave=Object.FindFirstObjectByType<PulseWave>();}
            Assert.IsNotNull(wave);GameTime.Paused=true;float radius=wave.Radius;yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(radius,wave.Radius);GameTime.Paused=false;yield return new WaitForSecondsRealtime(.6f);Assert.Less(victim.Health.integrity,1000);
            Place(taren,new Vector3(208,0,0));Place(sela,new Vector3(200,0,0));Assert.IsTrue(sela.Skill(2));
            deadline=Time.unscaledTime+1;NetSeed seed=null;
            while(seed==null&&Time.unscaledTime<deadline){yield return null;seed=Object.FindFirstObjectByType<NetSeed>();}
            Assert.IsNotNull(seed);GameTime.Paused=true;var position=seed.transform.position;yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(position,seed.transform.position);Assert.AreEqual(0,Object.FindObjectsByType<PulseField>(FindObjectsSortMode.None).Length);
            GameTime.Paused=false;sela.Stagger(.3f);yield return new WaitForSecondsRealtime(.5f);
            var fields=Object.FindObjectsByType<PulseField>(FindObjectsSortMode.None);Assert.AreEqual(1,fields.Length,"already released seed was incorrectly cancelled");
            GameTime.Paused=true;float life=fields[0].life;yield return new WaitForSecondsRealtime(.3f);Assert.AreEqual(life,fields[0].life);
            GameTime.Paused=false;
        }
    }
}
