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
    public sealed class FlashBarkTests
    {
        CombatActor sela;
        readonly List<string> lines=new();
        void Heard(string speaker,string line){lines.Add(speaker+": "+line);}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var hero in PartyController.Current.members)
            {
                hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;
                hero.GetComponent<FlightMotor>().Halt();if(hero.character=="Sela")sela=hero;
            }
            // Let any preceding scene/test bark expire through the public clock.
            yield return new WaitForSecondsRealtime(12.2f);lines.Clear();BarkService.Spoken+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {BarkService.Spoken-=Heard;GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        void Flash()
        {
            int before=sela.FlashMoves;
            Assert.IsTrue(sela.Dodge(Vector3.right));Assert.IsTrue(sela.Deflect(new DamagePacket{amount=1,type=DamageType.Beam}));
            Assert.AreEqual(before+1,sela.FlashMoves,"fixture must perform another successful Flash dodge");
        }
        int FlashLines=>lines.FindAll(line=>line=="Sela: "+BarkService.Line("Sela","flash")).Count;
        [UnityTest] public IEnumerator RepeatedSuccessfulDodgesDoNotRestartTheSameLine()
        {
            Flash();Assert.AreEqual(1,FlashLines);
            yield return new WaitForSecondsRealtime(5);
            Flash();Assert.AreEqual(1,FlashLines,"another successful dodge repeats the same short bark after only five seconds");
            yield return new WaitForSecondsRealtime(7.3f);
            Flash();Assert.AreEqual(2,FlashLines,"the same line never becomes eligible again after the quiet interval");
        }
        [UnityTest] public IEnumerator AFlashCannotInterruptTheForcedCoverLine()
        {
            var party=PartyController.Current;Assert.AreEqual("Taren",party.Active.character);
            party.Active.Health.Receive(new DamagePacket{amount=1000000,type=DamageType.Kinetic});
            float deadline=Time.unscaledTime+2;while(party.Active.character!="Sela"&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual("Sela",party.Active.character);
            Assert.AreEqual("Sela: "+BarkService.Line("Sela","cover"),lines[lines.Count-1]);
            yield return new WaitForSecondsRealtime(.1f);Flash();
            Assert.AreEqual("Sela: "+BarkService.Line("Sela","cover"),lines[lines.Count-1],"routine dodge banter interrupts the urgent cover line");
        }
        [UnityTest] public IEnumerator AForcedCoverCanInterruptFlashBanter()
        {
            Flash();Assert.AreEqual(1,FlashLines);
            var party=PartyController.Current;party.Active.Health.Receive(new DamagePacket{amount=1000000,type=DamageType.Kinetic});
            float deadline=Time.unscaledTime+2;while(party.Active.character!="Sela"&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual("Sela",party.Active.character);
            Assert.AreEqual("Sela: "+BarkService.Line("Sela","cover"),lines[lines.Count-1],"repeat suppression swallowed urgent cover dialogue");
        }
    }
}
