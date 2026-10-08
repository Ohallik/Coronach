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
    public sealed class CantorReleaseTests
    {
        GameObject fixture;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.GetComponent<FlightMotor>().Halt();}
            foreach(AudioBus bus in System.Enum.GetValues(typeof(AudioBus)))AudioMix.Current.SetLevel(bus,1,false);
            fixture=new GameObject("Cantor release fixture");yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {Object.Destroy(fixture);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        EnemyBrain Spawn()
        {
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(30,1,0));
            enemy.Passive=true;enemy.transform.SetParent(fixture.transform);return enemy;
        }
        static void Resolve(EnemyBrain enemy)=>enemy.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=enemy.Health.maximum*100,type=DamageType.Pulse});
        [UnityTest] public IEnumerator FreedCantorUsesAReleaseCueInsteadOfAnExplosionOrDeath()
        {
            var enemy=Spawn();yield return null;
            using var cues=new CueLog();Resolve(enemy);Resolve(enemy);yield return null;
            Assert.AreEqual(1,cues.Count("doorOpen_000",enemy.transform),"collar release needs one actuator cue: "+cues.All);
            Assert.AreEqual(0,cues.Count("explosionCrunch_000",enemy.transform),"freed animal still explodes: "+cues.All);
            Assert.AreEqual(0,cues.Count("death_creature",enemy.transform));
            Assert.AreEqual(0,cues.Count("death_mech",enemy.transform));
        }
        [UnityTest] public IEnumerator FreedCantorDepartsAtFullSizeWithItsHeadAndBodyConnected()
        {
            var enemy=Spawn();yield return null;yield return new WaitForSecondsRealtime(.3f);
            var root=enemy.transform;var visual=enemy.GetComponent<DefeatPresentation>().visual;
            var rotation=visual.localRotation;var scale=root.localScale;var origin=root.position;
            var parts=enemy.GetComponent<SerpentSegments>().Parts;Assert.AreEqual(7,parts.Count);
            Resolve(enemy);float started=GameTime.Now;int frames=0;float furthest=0;
            while(GameTime.Now-started<3.35f)
            {
                yield return null;Assert.IsNotNull(enemy,"release vanished before its departure completed");
                Assert.AreEqual(scale,root.localScale,"living animal shrinks like a corpse");
                Assert.Less(Quaternion.Angle(rotation,visual.localRotation),1,"head falls away from the attached body");
                Assert.GreaterOrEqual(root.position.y,origin.y-.03f,"released animal sinks like a corpse");
                var previous=root.position+Vector3.up*SerpentSegments.CenterHeight;
                foreach(var part in parts)
                {Assert.That(Vector3.Distance(previous,part.position),Is.InRange(1.8f,2.6f),"release tears the segment chain apart");previous=part.position;}
                Assert.IsFalse(enemy.GetComponentsInChildren<Collider>().Any(c=>c.enabled),"released animal retains combat collision");
                furthest=Mathf.Max(furthest,Vector3.Distance(origin,root.position));frames++;
            }
            Assert.Greater(frames,50);Assert.Greater(furthest,40,"resolved animal never leaves the encounter");
            yield return new WaitForSecondsRealtime(1);
            Assert.IsTrue(enemy==null,"departed presentation never cleans up");
        }
        [UnityTest] public IEnumerator ReleaseHoldsDuringPauseAndHealthRecoveryRestoresItsPose()
        {
            var enemy=Spawn();yield return null;yield return new WaitForSecondsRealtime(.3f);
            var origin=enemy.transform.position;var heading=enemy.transform.rotation;
            var visual=enemy.GetComponent<DefeatPresentation>().visual;var pose=visual.localRotation;
            var parts=enemy.GetComponent<SerpentSegments>().Parts;
            var originalParts=parts.Select(p=>p.localPosition).ToArray();
            Resolve(enemy);yield return new WaitForSecondsRealtime(1);
            Assert.Greater(Vector3.Distance(origin,enemy.transform.position),1,"fixture never enters departure");
            GameTime.Paused=true;yield return null;
            var held=enemy.transform.position;var heldParts=parts.Select(p=>p.position).ToArray();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(held,enemy.transform.position,"release moved while paused");
            for(int i=0;i<parts.Count;i++)Assert.AreEqual(heldParts[i],parts[i].position,"segment moved while paused");
            GameTime.Paused=false;yield return new WaitForSecondsRealtime(.2f);
            Assert.Greater(Vector3.Distance(held,enemy.transform.position),.5f);
            // Existing Health recovery callback only; this is not a new enemy
            // revival ability and does not claim to reset hostile AI.
            enemy.Health.Heal(enemy.Health.maximum);yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsNotNull(enemy);Assert.Less(Vector3.Distance(origin,enemy.transform.position),.001f);
            Assert.Less(Quaternion.Angle(heading,enemy.transform.rotation),.01f);
            Assert.Less(Quaternion.Angle(pose,visual.localRotation),.01f);
            for(int i=0;i<parts.Count;i++)Assert.Less(Vector3.Distance(originalParts[i],parts[i].localPosition),.001f);
            Assert.IsTrue(enemy.GetComponent<CharacterController>().enabled);
        }
        [UnityTest] public IEnumerator TheWholeReleasedBodyLeavesTheOrdinaryCameraBeforeRemoval()
        {
            var hero=PartyController.Current.Active;
            var controller=hero.GetComponent<CharacterController>();controller.enabled=false;
            hero.transform.position=new Vector3(30,1,-17);controller.enabled=true;
            foreach(float bearing in new[]{0f,90f,180f,270f})
            {
                var enemy=Spawn();enemy.transform.rotation=Quaternion.Euler(0,bearing,0);
                hero.target=enemy.Health;hero.TargetLocked=true;
                yield return new WaitForSecondsRealtime(1.5f);
                var renderers=enemy.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.name!="Telegraph"&&r is MeshRenderer or SkinnedMeshRenderer).ToArray();
                Assert.Greater(renderers.Length,7,"fixture needs the complete generated animal");
                var planes=GeometryUtility.CalculateFrustumPlanes(Camera.main);
                foreach(var renderer in renderers)Assert.IsTrue(GeometryUtility.TestPlanesAABB(planes,renderer.bounds),"release fixture starts outside the view at "+bearing);
                Resolve(enemy);yield return new WaitForSecondsRealtime(3.35f);
                Assert.IsNotNull(enemy,"body vanished before the off-screen check");
                planes=GeometryUtility.CalculateFrustumPlanes(Camera.main);
                foreach(var renderer in renderers)Assert.IsFalse(GeometryUtility.TestPlanesAABB(planes,renderer.bounds),"cleanup would remove a visible body at bearing "+bearing+": "+renderer.name);
                yield return new WaitForSecondsRealtime(.4f);Assert.IsTrue(enemy==null);
            }
        }
        [UnityTest] public IEnumerator TheWholeReleasedBodyClearsTheNarrowingGulletExit()
        {
            SceneFlow.Current.LoadZone("Gullet_Tunnel");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);yield return new WaitForSecondsRealtime(.3f);
            fixture=new GameObject("Cantor Gullet release fixture");
            foreach(var other in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))other.Passive=true;
            // The actual coil is broad, while its exit is only 20 m wide. A
            // departure keeping the lethal X coordinate can pass arena checks
            // and still carry the animal through the living wall.
            foreach(float x in new[]{-20f,20f})foreach(float bearing in new[]{0f,180f})
            {
                var enemy=Spawn();enemy.transform.SetPositionAndRotation(new Vector3(x,1,820),Quaternion.Euler(0,bearing,0));
                yield return new WaitForSecondsRealtime(.4f);
                var renderers=enemy.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.name!="Telegraph"&&r is MeshRenderer or SkinnedMeshRenderer).ToArray();
                Assert.Greater(renderers.Length,7);
                Resolve(enemy);float started=GameTime.Now,minimum=float.PositiveInfinity;int frames=0;
                while(GameTime.Now-started<3.35f)
                {
                    yield return null;Assert.IsNotNull(enemy);
                    foreach(var renderer in renderers)
                    {
                        var bounds=renderer.bounds;
                        foreach(float z in new[]{bounds.min.z,bounds.center.z,bounds.max.z})
                        {
                            if(z>GulletProfile.End)continue;
                            minimum=Mathf.Min(minimum,bounds.min.x-GulletProfile.LeftEdge(z),GulletProfile.RightEdge(z)-bounds.max.x);
                        }
                    }
                    frames++;
                }
                float tail=renderers.Min(r=>r.bounds.min.z);
                Debug.Log($"CANTOR_EXIT x={x} bearing={bearing} frames={frames} clearance={minimum:R} tailZ={tail:R}");
                Assert.Greater(frames,50);
                Assert.GreaterOrEqual(minimum,.15f,"released body crosses the authored Gullet wall");
                Assert.Greater(tail,GulletProfile.End,"cleanup occurs before the whole animal clears the exit");
                yield return new WaitForSecondsRealtime(.4f);Assert.IsTrue(enemy==null);
            }
        }
    }
}
