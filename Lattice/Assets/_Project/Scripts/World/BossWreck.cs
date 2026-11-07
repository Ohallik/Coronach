using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>The Burrower is disabled equipment, and equipment stays where it stopped. A later visit finds the wreck at the drill site, as a
    /// FlagGate would show it. A FlagGate would also show it the moment the rig is beaten, with the real body still falling, so in the fight
    /// itself the wreck takes over from the body where it lies, just as the body starts to shrink away: nobody sees two rigs, or none.
    /// It has no collider: the site's navigation was baked without it, and a solid wreck could trap a route.</summary>
    public sealed class BossWreck:MonoBehaviour
    {
        static readonly int Emission=Shader.PropertyToID("_EmissionColor");
        public string bossId="Burrower",downFlag="bossdown.Burrower";
        public GameObject wreck;public Transform rig;
        /// <summary>What the fall threw clear, lying about on a later visit. Not in the fight itself: it would appear from nowhere.</summary>
        public GameObject thrownParts;
        bool settled,searched;Transform body,pose;float fullSize;
        void Start()
        {
            if(wreck==null||GameServices.Current==null)return;
            // It has no collider, so anyone can walk behind it: it opens for the hero as the rig did when it was fighting.
            if(wreck.GetComponent<BossOcclusion>()==null)wreck.AddComponent<BossOcclusion>();
            // The seams are dead: the model's own emission is what said it was running.
            var block=new MaterialPropertyBlock();
            foreach(var part in wreck.GetComponentsInChildren<Renderer>(true)){part.GetPropertyBlock(block);block.SetVector(Emission,Vector4.zero);part.SetPropertyBlock(block);}
            settled=GameServices.Current.Flags.GetBool(downFlag);wreck.SetActive(settled);
        }
        void Update()
        {
            if(settled||wreck==null||GameServices.Current==null||!GameServices.Current.Flags.GetBool(downFlag))return;
            if(!searched)
            {
                searched=true;
                foreach(var enemy in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
                    if(enemy.definition!=null&&enemy.definition.id==bossId&&!enemy.Health.Alive&&enemy.TryGetComponent<DefeatPresentation>(out var defeat)&&defeat.visual!=null)
                    {body=enemy.transform;pose=defeat.visual;fullSize=body.localScale.x;break;}
            }
            if(body!=null&&body.localScale.x>=fullSize*.999f)return;
            settled=true;
            if(body!=null)
            {
                rig.SetPositionAndRotation(pose.position,pose.rotation);
                if(thrownParts!=null)thrownParts.SetActive(false);
            }
            wreck.SetActive(true);
        }
    }
}
