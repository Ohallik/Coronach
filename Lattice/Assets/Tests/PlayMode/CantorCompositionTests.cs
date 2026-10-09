using System.Collections;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    // Screen coverage is a legibility floor, not visual-quality acceptance.
    public sealed class CantorCompositionTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Gullet_Tunnel");float until=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var hero in PartyController.Current.members)
            {hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;hero.GetComponent<FlightMotor>().Halt();}
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor actor,Vector3 p)
        {
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=p;cc.enabled=true;
            actor.GetComponent<FlightMotor>().Move(Vector2.up,false,false);actor.GetComponent<FlightMotor>().Halt();
        }
        static Rect Footprint(Renderer[] meshes)
        {
            var minimum=Vector2.one*float.PositiveInfinity;var maximum=Vector2.one*float.NegativeInfinity;
            Assert.IsNotEmpty(meshes);
            foreach(var mesh in meshes)
            {
                var b=mesh.bounds;
                for(int i=0;i<8;i++)
                {
                    var p=Camera.main.WorldToViewportPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                    Assert.Greater(p.z,0);Assert.That(p.x,Is.InRange(.025f,.975f),mesh.name+" horizontal crop");Assert.That(p.y,Is.InRange(.025f,.975f),mesh.name+" vertical crop");
                    minimum=Vector2.Min(minimum,p);maximum=Vector2.Max(maximum,p);
                }
            }
            return Rect.MinMaxRect(minimum.x,minimum.y,maximum.x,maximum.y);
        }
        IEnumerator Shoulder(string active)
        {
            var party=PartyController.Current;if(party.Active.character!=active)Assert.IsTrue(party.Swap());
            foreach(var hero in party.members){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;}
            var encounter=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="Gullet_Cantor");
            var animal=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Single(e=>e.definition.id=="Cantor");
            Place(party.Active,encounter.previewPoint.position);Place(party.members.First(h=>h!=party.Active),encounter.previewPoint.position+new Vector3(-4,0,-3));
            yield return new WaitForSecondsRealtime(2);
            Assert.IsFalse(encounter.Started);
            var meshes=animal.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
            float entrance=encounter.GetComponent<BoxCollider>().bounds.min.z;
            foreach(var mesh in meshes)
            {
                var b=mesh.bounds;
                Assert.GreaterOrEqual(b.min.z,entrance+.25f,"preview anatomy extends before actual activation");
                Assert.IsTrue(b.min.x>6||b.max.x< -6||b.min.z>entrance+8,"preview blocks the central entry before the animal can react");
            }
            var body=Footprint(meshes);
            var collar=animal.GetComponent<CantorCollar>();float smallestLink=float.PositiveInfinity;
            for(int i=0;i<4;i++)
            {
                var link=Footprint(collar.Presentation(i).GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray());
                smallestLink=Mathf.Min(smallestLink,Mathf.Max(link.width,link.height));
            }
            float smallestShip=float.PositiveInfinity;
            foreach(var hero in party.members)
            {
                var ship=Footprint(hero.GetComponent<FormController>().flight.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray());
                smallestShip=Mathf.Min(smallestShip,Mathf.Max(ship.width,ship.height));
            }
            Debug.Log($"CANTOR_COMPOSITION active={active} animalHeight={body.height:R} smallestLinkSpan={smallestLink:R} smallestShipSpan={smallestShip:R} camera={Camera.main.transform.position}");
            Assert.GreaterOrEqual(body.height,.28f,"quiet preview makes the whole animal too small to study");
            Assert.GreaterOrEqual(smallestLink,.025f,"an intact restraint is too small in the preview");
            Assert.GreaterOrEqual(smallestShip,.035f,"the preview reduces a party ship below the legibility floor");
            CompositionSnapshot.Write(active);
        }
        [UnityTest] public IEnumerator TarenCanStudyTheWholeAnimalAndEachSeparateCollar()=>Shoulder("Taren");
        [UnityTest] public IEnumerator SelaCanStudyTheWholeAnimalAndEachSeparateCollar()=>Shoulder("Sela");
    }
}
