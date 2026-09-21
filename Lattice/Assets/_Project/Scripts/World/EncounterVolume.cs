using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    public sealed class EncounterVolume:MonoBehaviour
    {
        public Spawner[] spawners;
        public Membrane[] membranes;
        public int Remaining{get;private set;}
        public bool Started{get;private set;}
        public string encounterId;
        public bool Cleared=>Started&&Remaining==0;
        void OnTriggerEnter(Collider other){if(other.TryGetComponent<Health>(out var h)&&h.friendly)Begin();}
        public void Begin()
        {
            if(Started)return;Started=true;
            if(!string.IsNullOrEmpty(encounterId)&&GameServices.Current.Flags.GetBool("clear."+encounterId)){foreach(var membrane in membranes)membrane.SetOpen(true);return;}
            foreach(var membrane in membranes)membrane.SetOpen(false);
            foreach(var spawner in spawners)foreach(var enemy in spawner.Spawn()){Remaining++;enemy.Health.Died+=OnDeath;}
        }
        void OnDeath(Health health,DamagePacket packet)
        {Remaining--;if(Remaining==0){foreach(var membrane in membranes)membrane.SetOpen(true);if(!string.IsNullOrEmpty(encounterId))GameServices.Current.Flags.SetBool("clear."+encounterId,true);Debug.Log("ENCOUNTER_CLEAR "+name);}}
    }
}
