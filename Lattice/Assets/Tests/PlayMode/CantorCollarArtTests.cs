using System.Collections;
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
    public sealed class CantorCollarArtTests
    {
        CantorCollar collar;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            var cantor=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(30,1,0));cantor.Passive=true;
            collar=cantor.GetComponent<CantorCollar>();yield return null;yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}

        [UnityTest] public IEnumerator AllAttachedPlatesUseGeneratedGeometryAndItsOwnTexture()
        {
            for(int band=0;band<4;band++)
            {
                var plates=collar.Presentation(band).GetComponentsInChildren<MeshFilter>();Assert.AreEqual(8,plates.Length);
                foreach(var plate in plates)
                {
                    var mesh=plate.sharedMesh;Assert.Greater(mesh.vertexCount,100,"primitive fallback survived on the actual collar");
                    Assert.LessOrEqual(mesh.triangles.Length/3,1500,"repeated module exceeds its bounded mesh budget");
                    Assert.AreEqual(mesh.vertexCount,mesh.uv.Length,"generated surface lost its UVs");
                    var material=plate.GetComponent<Renderer>().sharedMaterial;
                    Assert.AreEqual("Lattice/Toon",material.shader.name);
                    var albedo=material.GetTexture("_BaseMap");Assert.IsNotNull(albedo,"generated plate has no bound albedo");
                    StringAssert.Contains("CantorCollarPlate",albedo.name,"plate displays an unrelated fallback texture");
                    Assert.LessOrEqual(Mathf.Max(albedo.width,albedo.height),1024);
                    Assert.IsNotNull(material.GetTexture("_EmissionMap"),"status pigment has no derived mask");
                }
            }
            yield return null;
        }
        [UnityTest] public IEnumerator OuterFacesPointAwayFromTheAnimalWithinTheOriginalEnvelope()
        {
            for(int band=0;band<4;band++)
            {
                var root=collar.Presentation(band);var plates=root.GetComponentsInChildren<MeshFilter>();Assert.AreEqual(8,plates.Length);
                foreach(var plate in plates)
                {
                    var radial=(plate.transform.position-root.position).normalized;
                    Assert.Greater(Vector3.Dot(plate.transform.up,radial),.99f,"authored outer face points into the animal");
                    foreach(var vertex in plate.sharedMesh.vertices)
                    {
                        var point=root.InverseTransformPoint(plate.transform.TransformPoint(vertex));
                        Assert.LessOrEqual(new Vector2(point.x,point.y).magnitude,1.4f,"module expands the collar's radial envelope");
                        Assert.LessOrEqual(Mathf.Abs(point.z),.25f,"module expands the collar's axial envelope");
                    }
                }
            }
            yield return null;
        }
    }
}
