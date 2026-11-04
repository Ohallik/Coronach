using System.Collections;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>D194: ordinary enemies grow more dangerous through the chapter (ZoneDef.enemyThreat); bosses keep their
    /// own phase scaling, and Sorrel stays at its original strength.</summary>
    public sealed class EnemyThreatTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static IEnumerator Load(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            yield return null;
        }
        [UnityTest] public IEnumerator OrdinaryEnemiesTakeTheirZonesThreatAndBossesDoNot([Values("Sorrel_Ridges","Hushwell","Gullet_Tunnel")] string zone)
        {
            yield return Load(zone);
            float expected=zone=="Hushwell"?1.25f:zone=="Gullet_Tunnel"?1.2f:1f;
            var spawners=Object.FindObjectsByType<Spawner>(FindObjectsSortMode.None).Where(s=>s.definition!=null).ToArray();
            var ordinary=spawners.FirstOrDefault(s=>!s.definition.boss);
            Assert.IsNotNull(ordinary,zone+" has no ordinary spawner");
            var enemies=ordinary.Spawn();
            Assert.IsNotEmpty(enemies);
            foreach(var enemy in enemies)Assert.AreEqual(expected,enemy.DamageScale,.001f,$"{zone} {enemy.definition.id} threat");
            foreach(var enemy in enemies)Object.Destroy(enemy.gameObject);
            var boss=spawners.FirstOrDefault(s=>s.definition.boss);
            if(boss!=null)
            {
                var bosses=boss.Spawn();
                foreach(var b in bosses){Assert.AreEqual(1f,b.DamageScale,.001f,$"{zone} boss {b.definition.id} takes the zone threat");Object.Destroy(b.gameObject);}
            }
        }
    }
}
