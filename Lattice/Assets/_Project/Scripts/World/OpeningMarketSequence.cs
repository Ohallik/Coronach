using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// The first town's ordinary morning (D195, STORY_CAMPAIGN Act I): on a genuine New Game the office-to-market bridge
    /// is closed for Taren's repair, Mira's mobile stall has crept over the safety line and Sela comes across from the
    /// market with a receipt for a ship that already left. Completion belongs to the repair's DiscoveryPoint; this
    /// coordinator only presents the saved state and gives the speakers whose full conversations would skip the scene a
    /// holding line until it has played (D197). Saves made before this scene existed, and every controlled fixture,
    /// never see it.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class OpeningMarketSequence:MonoBehaviour
    {
        public const string Requested="opening.marketRequested",Repaired="opening.walkwayRepaired";
        public Npc orrin,sela,mira;
        public ScriptedWalk selaWalk;
        public DiscoveryPoint repair;
        public Transform cover,stall;
        public Transform[] gates;
        public Collider openingGuard;
        Collider[][] gateColliders;
        public Vector3 openCoverEuler=new Vector3(0,0,-70);
        public float[] gateOpenYaw={90,-90};
        public Vector3 stallBack=new Vector3(.7f,0,0);
        GameState owner;bool fresh;float closure;Vector3 stallOrigin;Quaternion[] gateRest;
        public bool Fresh=>fresh;
        public float Closure=>closure;

        void Start()
        {
            var service=GameServices.Current;if(service==null){enabled=false;return;}
            owner=service.State;fresh=service.Flags.GetBool(Requested)&&!service.Flags.GetBool(Repaired);
            stallOrigin=stall.localPosition;gateRest=new Quaternion[gates.Length];gateColliders=new Collider[gates.Length][];
            for(int i=0;i<gates.Length;i++){gateRest[i]=gates[i].localRotation;gateColliders[i]=gates[i].GetComponentsInChildren<Collider>(true);}
            closure=fresh?0:1;Apply();
        }
        void Update()
        {
            var service=GameServices.Current;
            if(service==null||!ReferenceEquals(owner,service.State)||GameTime.Paused)return;
            if(fresh&&service.Flags.GetBool(Repaired))closure=Mathf.MoveTowards(closure,1,Time.deltaTime/1.1f);
            Apply();
        }
        void Apply()
        {
            bool repaired=!fresh||GameServices.Current.Flags.GetBool(Repaired);
            // Offer the job as soon as Sela sets off: she finishes her walk during the conversation, so a player who reaches
            // the cover first is never left at a silent barrier (D197).
            bool underway=selaWalk==null||selaWalk.Phase!=ScriptedWalk.Stage.Waiting;
            // A premature full talk with Orrin would mark him met and skip the conversation that brings Sela aboard, so
            // until the bridge is open the three of them answer with a short holding line instead of not answering at all.
            orrin.holdNode=repaired&&closure>=1?null:"OrrinOpening";
            sela.holdNode=repaired?null:"SelaOpening";
            mira.holdNode=repaired?null:"MiraOpening";
            repair.enabled=fresh&&!repaired&&underway;
            float k=Mathf.SmoothStep(0,1,closure);
            cover.localRotation=Quaternion.Slerp(Quaternion.Euler(openCoverEuler),Quaternion.identity,k);
            for(int i=0;i<gates.Length;i++)gates[i].localRotation=gateRest[i]*Quaternion.Euler(0,gateOpenYaw[Mathf.Min(i,gateOpenYaw.Length-1)]*k,0);
            stall.localPosition=stallOrigin+stallBack*k;
            // Gates block only while shut or folded away: a swinging panel must never shove the party.
            bool settled=closure<=0||closure>=1;
            foreach(var set in gateColliders)foreach(var c in set)if(c!=null)c.enabled=settled;
            if(openingGuard!=null)openingGuard.enabled=closure<.5f;
        }
    }
}
