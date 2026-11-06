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
        /// <summary>Stays asleep until this flag is set (empty: wakes on entry).</summary>
        public string requiredFlag;
        /// <summary>A boss door: starting this fight moves the respawn to this spawn point and saves there.</summary>
        public string checkpointSpawn;
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
        void Start()
        {
            ready=true;
            // A cleared arena's seals reflect the save on every load, wherever the party arrives: a closed exit used to wait
            // for someone to walk back into the trigger, while the objective pointed through it.
            if(!Started&&SavedClear){FinishSaved();return;}
            PreparePreview();
        }
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
            // Unity still sends trigger callbacks to disabled behaviours.
            // Suspending an encounter must also suspend automatic entry.
            if(!isActiveAndEnabled||Started||!other.TryGetComponent<Health>(out var h)||!h.friendly)return;
            if(!string.IsNullOrEmpty(requiredFlag)&&!GameServices.Current.Flags.GetBool(requiredFlag))return;
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
            Checkpoint();
            foreach(var membrane in membranes)membrane.SetOpen(false);
            foreach(var spawner in spawners)foreach(var enemy in spawner.Spawn()){Remaining++;enemy.Health.Died+=OnDeath;}
        }
        /// <summary>A defeat costs at most a walk from the door: the bar is two minutes, and a boss used to send the party back
        /// through the whole zone.</summary>
        void Checkpoint()
        {
            if(string.IsNullOrEmpty(checkpointSpawn))return;
            var services=GameServices.Current;services.State.spawn=checkpointSpawn;services.Saves.Save("autosave",services.State);
        }
        void OnDeath(Health health,DamagePacket packet)
        {
            Remaining--;if(Remaining!=0)return;
            foreach(var membrane in membranes)membrane.SetOpen(true);
            if(!string.IsNullOrEmpty(encounterId))GameServices.Current.Flags.SetBool("clear."+encounterId,true);
            Debug.Log("ENCOUNTER_CLEAR "+name);
            if(isActiveAndEnabled&&!string.IsNullOrEmpty(encounterId))StartCoroutine(SaveCleared());
        }
        /// <summary>A won fight stays won: a defeat at the next one doesn't take back this one or its rewards. One frame later,
        /// because the loot and XP of the last kill are paid by listeners that run after this one.</summary>
        System.Collections.IEnumerator SaveCleared()
        {
            yield return null;
            var services=GameServices.Current;if(services==null||SceneFlow.Current!=null&&SceneFlow.Current.Loading)yield break;
            services.Saves.Save("autosave",services.State);
        }
    }
}
