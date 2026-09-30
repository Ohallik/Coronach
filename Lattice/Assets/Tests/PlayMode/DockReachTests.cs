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
    /// <summary>A ship must be able to dock, not just nearly: from every open side,
    /// the point where a hull physically stops against a flight pad lies well
    /// inside that pad's prompt range.</summary>
    public sealed class DockReachTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");}
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}

        [UnityTest] public IEnumerator EveryFlightDockIsReachableByEitherHullWithMargin()
        {
            float hull=Mathf.Max(HeroCollision.HullRadius("Taren"),HeroCollision.HullRadius("Sela"));int checkedDocks=0;
            foreach(string zone in new[]{"Hub_CinderHalo","TallowApproach"})
            {
                SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+10;
                while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
                Assert.IsFalse(SceneFlow.Current.Loading);yield return new WaitForSecondsRealtime(.3f);
                foreach(var hero in PartyController.Current.members){var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.position+=Vector3.up*200;}
                Physics.SyncTransforms();
                foreach(var dock in Object.FindObjectsByType<DockingPad>(FindObjectsSortMode.None))
                {
                    var pad=dock.transform.position;int open=0;
                    foreach(var dir in new[]{Vector3.back,Vector3.left,Vector3.right,Vector3.forward,(Vector3.back+Vector3.left).normalized,(Vector3.back+Vector3.right).normalized})
                    {
                        // Approach at the flight plane from 14 m out; skip sides the hull
                        // cannot even reach (the pad's own vestibule, a neighbouring hull).
                        var from=new Vector3(pad.x,1,pad.z)+dir*14;
                        if(Physics.CheckSphere(from,hull,~0,QueryTriggerInteraction.Ignore))continue;
                        var toward=-dir;float stop=14;
                        foreach(var hit in Physics.SphereCastAll(from,hull,toward,14,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                        {if(hit.collider.GetComponentInParent<CombatActor>()!=null)continue;stop=hit.distance;break;}
                        var rest=from+toward*stop;
                        if(stop<14-dock.range-2)continue; // blocked well before the pad: not an approach side
                        open++;
                        Assert.Less(Vector3.Distance(rest,pad),dock.range-.5f,$"{zone} {dock.name}: a hull approaching from {dir} stops {Vector3.Distance(rest,pad):0.00} m away, outside its {dock.range} m prompt");
                    }
                    Assert.Greater(open,0,$"{zone} {dock.name} has no open approach side");checkedDocks++;
                }
            }
            Assert.GreaterOrEqual(checkedDocks,4,"the Cinder and Tallow flight docks were not all checked");
        }
    }
}
