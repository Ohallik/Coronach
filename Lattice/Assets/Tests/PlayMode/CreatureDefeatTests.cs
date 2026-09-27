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
    public sealed class CreatureDefeatTests
    {
        GameObject fixture;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var e in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))e.Passive=true;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.7f);fixture=new GameObject("Creature collapse fixture");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {if(fixture!=null)Object.Destroy(fixture);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static float Minimum(Transform visual,Vector3 plane,Vector3 normal,out float centroidHeight)
        {
            float value=float.PositiveInfinity;double sum=0;int count=0;
            foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=new Mesh();skin.BakeMesh(mesh);
                var matrix=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);
                foreach(var v in mesh.vertices)
                {float height=Vector3.Dot(matrix.MultiplyPoint3x4(v)-plane,normal);value=Mathf.Min(value,height);sum+=height;count++;}
                Object.Destroy(mesh);
            }
            foreach(var mesh in visual.GetComponentsInChildren<MeshFilter>())
                if(mesh.sharedMesh!=null)foreach(var v in mesh.sharedMesh.vertices)
                {float height=Vector3.Dot(mesh.transform.TransformPoint(v)-plane,normal);value=Mathf.Min(value,height);sum+=height;count++;}
            Assert.Greater(count,0,"collapse requires visible geometry");centroidHeight=(float)(sum/count);
            return value;
        }
        [UnityTest] public IEnumerator GroundCreaturesKeepTheirActualMeshAboveTheFloorThroughCollapseAndHold()
        {
            var failures=new List<string>();
            foreach(string id in new[]{"Ridgehound","Scrapmite","Burrower"})foreach(float slope in new[]{0f,10f,-10f})
            {
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixture.transform);
                floor.transform.SetPositionAndRotation(new Vector3(150,0,0),Quaternion.Euler(0,0,slope));floor.transform.localScale=new Vector3(30,1,30);
                var plane=floor.transform.TransformPoint(Vector3.up*.5f);var normal=floor.transform.up;Physics.SyncTransforms();
                var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),plane);enemy.Passive=true;enemy.transform.SetParent(fixture.transform);
                enemy.transform.rotation=Quaternion.Euler(0,slope==0?0:135,0);yield return new WaitForSecondsRealtime(.25f);
                var visual=enemy.GetComponent<DefeatPresentation>().visual;var root=enemy.transform.position;
                var standing=visual.rotation;enemy.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=enemy.Health.maximum*100,type=DamageType.Pulse});
                float duration=id=="Burrower"?1.5f:id=="Ridgehound"?.9f:.75f;
                float start=GameTime.Now,worst=float.PositiveInfinity,terminal=float.PositiveInfinity,centroid=0,at=0;
                while(GameTime.Now-start<duration+.15f)
                {
                    yield return null;float elapsed=GameTime.Now-start;
                    float minimum=Minimum(visual,plane,normal,out centroid);
                    if(minimum<worst){worst=minimum;at=elapsed;}
                    terminal=minimum;
                    if(Vector3.Distance(root,enemy.transform.position)>.001f)failures.Add(id+" moved its gameplay root during death");
                }
                if(worst<-.03f)failures.Add($"{id} slope={slope} mesh penetrates {worst:F5}m at {at:F5}s");
                if(terminal<-.03f||terminal>.05f)failures.Add($"{id} slope={slope} corpse floats/penetrates {terminal:F5}m");
                // Eye-reviewed generated bodies must lie down, not balance on an
                // extended paw, leg or jaw. Clearance alone cannot reject that.
                float restingHeightLimit=id=="Ridgehound"?.52f:id=="Scrapmite"?.48f:1.1f;
                if(centroid>restingHeightLimit)failures.Add($"{id} slope={slope} body remains propped up: centroid={centroid:F5}m limit={restingHeightLimit:F2}m");
                if(Quaternion.Angle(standing,visual.rotation)<20)failures.Add(id+" has no visible collapse attitude");
                Vector3 held=visual.position;Quaternion rotation=visual.rotation;GameTime.Paused=true;
                yield return new WaitForSecondsRealtime(.2f);
                Assert.Less(Vector3.Distance(held,visual.position),.0001f);Assert.Less(Quaternion.Angle(rotation,visual.rotation),.01f);
                GameTime.Paused=false;yield return new WaitForSecondsRealtime(.15f);
                Assert.Less(Vector3.Distance(held,visual.position),.001f,"settled corpse drifts");
                Debug.Log($"CREATURE_COLLAPSE {id} slope={slope} minimum={worst:F6} at={at:F6} terminal={terminal:F6} centroid={centroid:F6}");
                Object.Destroy(enemy.gameObject);Object.Destroy(floor);yield return null;
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
