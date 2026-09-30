using System.Collections;
using System.Collections.Generic;
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
    /// <summary>A Scrapmite swarm is six weak mites, not six hounds: its sustained
    /// bite on the active hero stays within a Ridgehound pack's in the same zone.
    /// Its death bursts are the swarm's own punishment for staying adjacent.</summary>
    public sealed class SwarmPressureTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");}
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}

        // EnemyBrain opens a tell once cooled down and in reach, strikes when it
        // expires, then cools down again: one bite per telegraph plus cooldown.
        static float Pressure(Spawner s)
        {var bite=s.definition.attacks[0];return s.count*bite.damage/(bite.telegraph+bite.cooldown);}

        [UnityTest] public IEnumerator AScrapmiteSwarmBitesNoHarderThanAHoundPack()
        {
            var failures=new List<string>();int swarms=0;
            foreach(string zone in new[]{"Sorrel_Ridges","Hushwell"})
            {
                SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+10;
                while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
                Assert.IsFalse(SceneFlow.Current.Loading);
                var spawners=Object.FindObjectsByType<Spawner>(FindObjectsSortMode.None).Where(s=>s.definition!=null&&s.definition.attacks?.Length>0).ToArray();
                var packs=spawners.Where(s=>s.definition.id=="Ridgehound").ToArray();
                var mites=spawners.Where(s=>s.definition.id=="Scrapmite").ToArray();
                Assert.IsNotEmpty(packs,zone+" has no Ridgehound pack to compare with");
                Assert.IsNotEmpty(mites,zone+" has no Scrapmite swarm");
                float pack=packs.Min(Pressure);swarms+=mites.Length;
                foreach(var swarm in mites)
                    if(Pressure(swarm)>pack)failures.Add($"{zone} {swarm.name}: {swarm.count} mites bite for {Pressure(swarm):0.0}/s, a hound pack for {pack:0.0}/s");
            }
            Assert.GreaterOrEqual(swarms,6,"the Sorrel and Hushwell swarms were not all checked");
            Assert.IsEmpty(failures,"a swarm of weak mites out-bites a hound pack:\n"+string.Join("\n",failures));
        }
    }
}
