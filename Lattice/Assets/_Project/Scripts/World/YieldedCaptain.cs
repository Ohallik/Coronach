using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>The captain's ending (D202): she yields, she isn't killed. The person she is now stays hidden through the duel,
    /// takes over from the fighter's body in the same frame and at the same spot, and says her piece without being asked: it
    /// is the chapter's only post-fight scene and carries the first word of the Warmaster, and the objective used to walk past it.
    /// Hearing it is saved at once, because the lift waits for it.</summary>
    public sealed class YieldedCaptain:MonoBehaviour
    {
        /// <summary>A save that left before she spoke: she waits at her post until the party comes to her.</summary>
        const float HailRange=8;
        public Npc npc;public string bossId="HessaKorl",downFlag="bossdown.HessaKorl",heardFlag="relay.hessaYielded";
        bool standing,nearOnly,heard;float speakAt=-1;
        void OnEnable()=>DefeatPresentation.Yielded+=TakeOver;
        void OnDisable()=>DefeatPresentation.Yielded-=TakeOver;
        void Start()
        {
            if(npc==null||GameServices.Current==null)return;
            var flags=GameServices.Current.Flags;heard=flags.GetBool(heardFlag);
            bool down=flags.GetBool(downFlag);npc.gameObject.SetActive(down);
            if(down)StandAtPost();
        }
        void StandAtPost()
        {
            standing=true;npc.gameObject.SetActive(true);
            if(!GameServices.Current.Flags.GetBool(heardFlag)){nearOnly=true;speakAt=Time.unscaledTime+1;}
        }
        void TakeOver(string id,Vector3 position,Quaternion rotation)
        {
            if(id!=bossId||npc==null)return;
            npc.transform.SetPositionAndRotation(position,rotation);npc.gameObject.SetActive(true);standing=true;
            // The incoming body has held its prefab pose since the scene loaded; it must not show that for a frame.
            var rig=npc.GetComponentInChildren<Animator>();if(rig!=null)rig.Update(0);
            if(!GameServices.Current.Flags.GetBool(heardFlag)){nearOnly=false;speakAt=Time.unscaledTime+.4f;}
        }
        /// <summary>A fighter's body still playing out its defeat; the person takes over when it finishes.</summary>
        bool BodyLeaving()
        {
            foreach(var enemy in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
                if(enemy.definition!=null&&enemy.definition.id==bossId&&!enemy.Health.Alive)return true;
            return false;
        }
        void Update()
        {
            if(npc==null||GameServices.Current==null)return;
            var flags=GameServices.Current.Flags;
            if(!standing){if(flags.GetBool(downFlag)&&!BodyLeaving())StandAtPost();return;}
            if(!heard&&flags.GetBool(heardFlag))
            {
                heard=true;speakAt=-1;
                if(SceneFlow.Current==null||!SceneFlow.Current.Loading)GameServices.Current.Saves.Save("autosave",GameServices.Current.State);
                return;
            }
            if(speakAt<0||Time.unscaledTime<speakAt||heard)return;
            var party=PartyController.Current;if(party==null||GameServices.Current.Input.Blocked||BossController.AnyFightUnderway)return;
            // She waits for the party to get up: nobody hears a surrender from the floor.
            foreach(var member in party.members)if(!member.Health.Alive||member.Recovering)return;
            if(nearOnly&&Vector3.Distance(party.Active.transform.position,npc.transform.position)>HailRange)return;
            speakAt=-1;npc.Interact();
        }
    }
}
