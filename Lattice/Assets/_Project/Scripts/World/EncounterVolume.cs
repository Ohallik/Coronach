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
        /// <summary>Its body isn't put on show until this flag is set (empty: on show from the first). For a boss who is somewhere else as a person until then: the
        /// Warmaster stood dormant on his field while he sat at his table in the hall, before the party had met him (whole-game capture of November 22, s52/014 and 017).</summary>
        public string previewFlag;
        bool previewHeld;
        bool PreviewWaits=>!string.IsNullOrEmpty(previewFlag)&&!GameServices.Current.Flags.GetBool(previewFlag);
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
        void OnEnable(){if(ready)PreparePreview();PartnerBrain.ComeThrough=Usher;if(!live.Contains(this))live.Add(this);}
        void OnDisable(){CancelPreview();MusicDirector.Fight(this,false);live.Remove(this);}
        // ---------- the partner comes through (D294)
        static readonly System.Collections.Generic.List<EncounterVolume> live=new();
        /// <summary>How far past an open gate's line the partner is brought: past the line by her own width and the half metre the gate asks, and room to stop in.</summary>
        public const float UsherPast=2.6f;
        /// <summary>How long a gate goes on asking after the last body touched it: the trigger speaks every physics step while somebody stands in it.</summary>
        const float AwaitedFor=.5f;
        CombatActor awaited;Vector3 awaitedPast;float awaitedAt=-10;
        /// <summary>Where a hero who is not leading should go because a fight's open gate is waiting for her alone: the lead is through it and standing inside, and her
        /// place three metres behind him is short of the line, so the fight would never begin and nothing would say why (the Last Warden's hall, November 14: seven
        /// minutes of a boss standing at its engine). Null when no gate waits for her, or the lead is not through yet himself.</summary>
        public static Vector3? Usher(CombatActor lead,CombatActor partner)
        {
            foreach(var volume in live)
                if(volume!=null&&!volume.Started&&volume.awaited==partner&&Time.time-volume.awaitedAt<AwaitedFor)return volume.awaitedPast;
            return null;
        }
        /// <summary>The bodies this fight woke, for the zone's fight cue (ZoneDef.fightMusicId: travel music, then a fight's). It holds while one of them is alive within
        /// earshot of the party, so a fight flown away from gives the lane its music back. A boss brings its own cue, and its door asks for none.</summary>
        readonly System.Collections.Generic.List<Health> woken=new();
        bool cued;float nextCue;
        const float CueRange=60;
        bool InEarshot()
        {
            var party=PartyController.Current;if(party==null||party.Active==null)return false;
            var at=party.Active.transform.position;
            foreach(var body in woken)
            {
                if(body==null||!body.Alive)continue;
                var gap=body.transform.position-at;gap.y=0;if(gap.sqrMagnitude<CueRange*CueRange)return true;
            }
            return false;
        }
        void PreparePreview()
        {
            if(Started||previewPoint==null)return;
            if(SavedClear){FinishSaved();return;}
            previewHeld=PreviewWaits;if(previewHeld)return;
            foreach(var spawner in spawners)spawner.Prepare(previewPoint.position,previewRadius);
        }
        void CancelPreview(){if(spawners!=null)foreach(var spawner in spawners)if(spawner!=null)spawner.CancelPreview();}
        void FinishSaved(){Started=true;CancelPreview();foreach(var membrane in membranes)membrane.SetOpen(true);}
        void Update()
        {
            if(!Started&&previewPoint!=null&&SavedClear)FinishSaved();
            if(previewHeld&&!Started&&!PreviewWaits)PreparePreview();
            if(cued&&Remaining>0&&Time.unscaledTime>=nextCue){nextCue=Time.unscaledTime+.5f;MusicDirector.Fight(this,InEarshot());}
        }
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
                    if(Vector3.Dot(hero.transform.position-membrane.transform.position,inward)<radius+.5f)
                    {
                        // The lead is through and it is somebody else the gate waits for: say where she should come to (Usher). She is brought through abreast of
                        // where she stands, so she walks straight in rather than round the lead.
                        var lead=party.Active;
                        if(lead!=null&&hero!=lead&&hero.Health.Alive&&Vector3.Dot(lead.transform.position-membrane.transform.position,inward)>=HeroRadius(lead)+.5f)
                        {
                            var across=Vector3.Cross(Vector3.up,inward);var from=hero.transform.position-membrane.transform.position;from.y=0;
                            if(awaited!=hero||Time.time-awaitedAt>=AwaitedFor)Debug.Log($"GATE_WAITS {encounterId} for {hero.character} at {hero.transform.position:F1}, the lead through at {lead.transform.position:F1}");
                            awaited=hero;awaitedAt=Time.time;
                            awaitedPast=membrane.transform.position+across*Vector3.Dot(from,across)+inward*(radius+UsherPast);awaitedPast.y=lead.transform.position.y;
                        }
                        return;
                    }
                }
            }
            Begin();
        }
        static float HeroRadius(CombatActor hero)=>hero.flight?HeroCollision.HullRadius(hero.character):hero.GetComponent<CharacterController>().radius;
        public void Begin()
        {
            if(Started)return;Started=true;awaited=null;
            if(SavedClear){FinishSaved();return;}
            Checkpoint();
            foreach(var membrane in membranes)membrane.SetOpen(false);
            cued=true;
            foreach(var spawner in spawners)
            {
                if(spawner.definition!=null&&spawner.definition.boss)cued=false;
                foreach(var enemy in spawner.Spawn()){Remaining++;enemy.Health.Died+=OnDeath;woken.Add(enemy.Health);}
            }
            if(cued)MusicDirector.Fight(this,true);
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
            cued=false;MusicDirector.Fight(this,false);
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
