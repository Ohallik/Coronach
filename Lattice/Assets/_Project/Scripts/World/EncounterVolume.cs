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
        public Transform previewPoint;
        public float previewRadius=6;
        bool ready;
        public bool Cleared=>Started&&Remaining==0;
        bool SavedClear
        {
            get
            {
                var flags=GameServices.Current.Flags;
                if(!string.IsNullOrEmpty(encounterId)&&flags.GetBool("clear."+encounterId))return true;
                // Earlier completed Cantor saves may have only the exit flag.
                return encounterId=="Gullet_Cantor"&&flags.GetBool("bossdown.Cantor");
            }
        }
        void Start(){ready=true;PreparePreview();}
        void OnEnable(){if(ready)PreparePreview();}
        void OnDisable()=>CancelPreview();
        void PreparePreview()
        {
            if(Started||previewPoint==null)return;
            if(SavedClear){FinishSaved();return;}
            foreach(var spawner in spawners)spawner.Prepare(previewPoint.position,previewRadius);
        }
        void CancelPreview(){if(spawners!=null)foreach(var spawner in spawners)if(spawner!=null)spawner.CancelPreview();}
        void FinishSaved(){Started=true;CancelPreview();foreach(var membrane in membranes)membrane.SetOpen(true);}
        void Update(){if(!Started&&previewPoint!=null&&SavedClear)FinishSaved();}
        void OnTriggerEnter(Collider other)=>TryEnter(other);
        void OnTriggerStay(Collider other)=>TryEnter(other);
        void TryEnter(Collider other)
        {
            if(Started||!other.TryGetComponent<Health>(out var h)||!h.friendly)return;
            var party=PartyController.Current;
            // An open gate is an entrance. Wait until every body has crossed it
            // before sealing; a downed companion outside still needs rescuing.
            // Closed exits do not impose an impossible pre-entry requirement.
            if(party!=null)foreach(var membrane in membranes)
            {
                if(membrane==null||!membrane.Open)continue;
                var inward=transform.position-membrane.transform.position;inward.y=0;
                if(inward.sqrMagnitude<.01f)continue;
                inward.Normalize();
                foreach(var hero in party.members)
                {
                    float radius=hero.flight?HeroCollision.HullRadius(hero.character):hero.GetComponent<CharacterController>().radius;
                    if(Vector3.Dot(hero.transform.position-membrane.transform.position,inward)<radius+.5f)return;
                }
            }
            Begin();
        }
        public void Begin()
        {
            if(Started)return;Started=true;
            if(SavedClear){FinishSaved();return;}
            foreach(var membrane in membranes)membrane.SetOpen(false);
            foreach(var spawner in spawners)foreach(var enemy in spawner.Spawn()){Remaining++;enemy.Health.Died+=OnDeath;}
        }
        void OnDeath(Health health,DamagePacket packet)
        {Remaining--;if(Remaining==0){foreach(var membrane in membranes)membrane.SetOpen(true);if(!string.IsNullOrEmpty(encounterId))GameServices.Current.Flags.SetBool("clear."+encounterId,true);Debug.Log("ENCOUNTER_CLEAR "+name);}}
    }
}
