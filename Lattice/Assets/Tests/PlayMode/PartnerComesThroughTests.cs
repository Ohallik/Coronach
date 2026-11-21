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
    /// <summary>A fight behind an open gate begins when everybody is through it (EncounterVolume.TryEnter), and the partner's place is three metres behind the lead. A lead who
    /// stops two steps inside therefore left her short of the line: the Last Warden stood at its engine for seven minutes of the first play of route 41 with both heroes in
    /// its hall and nothing to say why (November 14). The gate now says whom it waits for and she comes through (D294). The test stands the two as that run had them, in that
    /// hall, first with the way through taken away, which must leave the fight unbegun.</summary>
    public sealed class PartnerComesThroughTests
    {
        System.Func<CombatActor,CombatActor,Vector3?> hook;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup(){PartnerBrain.ComeThrough=hook??EncounterVolume.Usher;yield return null;}

        static void Stand(CombatActor hero,Vector3 at,Vector3 facing)
        {
            if(UnityEngine.AI.NavMesh.SamplePosition(at,out var floor,3,UnityEngine.AI.NavMesh.AllAreas))at=floor.position;
            var body=hero.GetComponent<CharacterController>();body.enabled=false;
            hero.transform.SetPositionAndRotation(at,Quaternion.LookRotation(facing,Vector3.up));body.enabled=true;
        }
        static float Through(CombatActor hero,Membrane gate,Vector3 inward)=>Vector3.Dot(hero.transform.position-gate.transform.position,inward)-hero.GetComponent<CharacterController>().radius-.5f;

        [UnityTest] public IEnumerator ALeadWhoStopsTwoStepsInsideAnOpenGateIsJoinedByThePartnerAndTheFightBegins()
        {
            DevLoadout.Apply("chapter7");
            foreach(string flag in new[]{"oriel.doorReached","oriel.choirSeen","oriel.releaseLearned","oriel.stay1","oriel.stay2","oriel.stay3","clear.Choir_Sentinels","clear.Choir_Fibres","clear.Choir_Tenders"})
                GameServices.Current.Flags.SetBool(flag,true);
            SceneFlow.Current.LoadZone("GlassChoir","Door");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: the Glass Choir never finished loading");
            yield return new WaitForSecondsRealtime(.8f);
            var party=PartyController.Current;var lead=party.Active;var partner=party.members.First(m=>m!=lead&&m.gameObject.activeInHierarchy);
            var fight=Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(v=>v.encounterId=="LastWarden");
            Assert.IsFalse(fight.Started,"fixture: the Warden's fight hasn't begun at the checkpoint");
            // The duel sleeps until the Warden has been met (its talk on the stair): met here by hand, or nothing below would be about the gate at all.
            if(!string.IsNullOrEmpty(fight.requiredFlag))GameServices.Current.Flags.SetBool(fight.requiredFlag,true);
            var gate=fight.membranes.First(m=>m!=null&&m.Open);
            var inward=fight.transform.position-gate.transform.position;inward.y=0;inward.Normalize();
            hook=PartnerBrain.ComeThrough;
            Assert.IsNotNull(hook,"a live fight's gate has not told the partner's brain how to ask it");

            // Both short of the line: nobody is asked through a gate the lead has not gone through himself.
            float leadRadius=lead.GetComponent<CharacterController>().radius;
            Stand(lead,gate.transform.position-inward*1.5f,inward);Stand(partner,gate.transform.position-inward*4.5f,inward);
            yield return new WaitForSeconds(1);
            Assert.IsNull(EncounterVolume.Usher(lead,partner),"the gate asked the partner through before the lead had gone through it");
            Assert.IsFalse(fight.Started,"fixture: the fight began with nobody through its gate");

            // As route 41 had them: the lead just through and stopped, the partner three metres behind him. Without the way through, nothing begins.
            PartnerBrain.ComeThrough=null;
            Stand(lead,gate.transform.position+inward*(leadRadius+.5f+.6f),inward);Stand(partner,lead.transform.position-inward*3,inward);
            yield return null;
            Assert.Greater(Through(lead,gate,inward),0,"fixture: the lead stands through the gate");
            Assert.Less(Through(partner,gate,inward),0,"fixture: the partner stands short of the gate's line");
            yield return new WaitForSeconds(6);
            Assert.IsFalse(fight.Started,"fixture: with no way through offered, a partner three metres behind the lead was expected to leave the fight unbegun (the fault this test is for)");
            Assert.Less(Through(partner,gate,inward),0,"fixture: the partner went through by herself, so this hall no longer shows the fault");

            // With it: she comes through, and the fight begins.
            PartnerBrain.ComeThrough=hook;
            float until=Time.time+8;
            while(!fight.Started&&Time.time<until)yield return null;
            var asked=EncounterVolume.Usher(lead,partner);
            Assert.IsTrue(fight.Started,$"the lead stood {Through(lead,gate,inward)+leadRadius+.5f:F1} m inside the Warden's gate for eight seconds and the fight never began: the partner is at {Through(partner,gate,inward):F2} m from being through; "+
                $"lead {lead.transform.position:F2}, partner {partner.transform.position:F2} ({partner.State}), gate {gate.transform.position:F2} inward {inward:F2}, asked to {(asked.HasValue?asked.Value.ToString("F2"):"nowhere")}, party of {party.members.Length}: {string.Join(", ",party.members.Select(m=>m.character+(m.gameObject.activeInHierarchy?"":" (away)")+" "+m.transform.position.ToString("F1")))}");
            Assert.Greater(Through(partner,gate,inward),0,"the fight began with the partner still outside its gate");
        }
    }
}
