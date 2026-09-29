using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// An exposed rib at the Bellows chamber's edge. In its last phase the Bellows
    /// lunges across the chamber to the rib nearest the party (never the one it
    /// already holds) and breathes from there, so the fight moves.
    /// </summary>
    public sealed class BellowsRib:MonoBehaviour
    {
        public static readonly List<BellowsRib> All=new();
        public Vector3 stanceOffset;
        public Vector3 Stance=>transform.position+stanceOffset;
        void OnEnable()=>All.Add(this);
        void OnDisable()=>All.Remove(this);
        public static BellowsRib Choose(Vector3 from,Vector3 party)
        {
            BellowsRib best=null;float score=float.PositiveInfinity;
            foreach(var rib in All)
            {
                var away=rib.Stance-from;away.y=0;if(away.magnitude<8)continue;
                var near=rib.Stance-party;near.y=0;if(near.sqrMagnitude<score){score=near.sqrMagnitude;best=rib;}
            }
            return best;
        }
    }
}
