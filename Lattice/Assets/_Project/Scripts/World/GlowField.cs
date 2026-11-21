using Lattice.Combat;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// A place where light is what hunts you (chapter 4's Spore Veil: visibility is the resource). Lights every hero ship in the zone with a <see cref="ShipGlow"/> from the moment it
    /// arrives (the combat side's: what the ship does lights it, and a swarm drawn to light reads it), and wakes each of <see cref="drawn"/> (the swarms hanging in the lane, on show
    /// and asleep) the moment the lead ship, lit, comes within <see cref="reach"/> of it: much further than a ship flown dark would have to come, which only wakes one by flying into it.
    /// So a party that boosts and fires through the swarm field is mobbed, and one that coasts dark between the clouds passes. Logs <c>SWARM_DRAWN id</c> for each swarm a light woke.
    /// <para>Only the ship the player flies draws them. The partner boosts by itself whenever it falls ten metres behind (PartnerBrain), and a boost lights a hull: counted, its
    /// catching up woke the field under a player who had flown it dark, for nothing the player did.</para>
    /// </summary>
    public sealed class GlowField:MonoBehaviour
    {
        /// <summary>The encounters a lit ship draws from afar.</summary>
        public EncounterVolume[] drawn=System.Array.Empty<EncounterVolume>();
        /// <summary>How far a lit ship's light carries to a sleeping swarm, on the plane.</summary>
        public float reach=30;
        void Update()
        {
            var party=PartyController.Current;if(party==null)return;
            // Every hero ship carries a glow here (the gauge and the swarms read it), the partner's too.
            foreach(var hero in party.members)if(hero!=null&&hero.flight)ShipGlow.Of(hero);
            var lead=party.Active;
            if(lead==null||!lead.flight||!ShipGlow.Of(lead).IsLit)return;
            var from=lead.transform.position;
            foreach(var swarm in drawn)
            {
                if(swarm==null||swarm.Started||!swarm.isActiveAndEnabled)continue;
                var gap=from-swarm.transform.position;gap.y=0;if(gap.sqrMagnitude>reach*reach)continue;
                Debug.Log("SWARM_DRAWN "+swarm.encounterId);swarm.Begin();
            }
        }
    }
}
