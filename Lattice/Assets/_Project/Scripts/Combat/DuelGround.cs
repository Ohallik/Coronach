using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The ground a duel is fought on (chapter 8: Harrowgate's muster field, where Drask Vaal answers the party): an empty object at the field's middle, on the floor. Its <see cref="half"/> is the
    /// field's half-size inside the kerb, which a duelist keeps to; its <see cref="guardPost"/> is where his guard wing steps onto the field when he calls it (inside the kerb, by the crowd's
    /// front rank). With none in the room (the proving yard) the duelist fights on open ground and his guard steps in beside him. The room: docs/maps/Harrowgate.md, "Duel ground contract".
    /// </summary>
    public sealed class DuelGround:MonoBehaviour
    {
        public static readonly List<DuelGround> All=new();
        /// <summary>The EnemyDef id of the boss who fights here.</summary>
        public string boss="DraskVaal";
        /// <summary>The field's half-size inside its kerb, x by z, about this object.</summary>
        public Vector2 half=new(17.5f,12.5f);
        public Transform guardPost;
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
        public static DuelGround For(string id){foreach(var ground in All)if(ground!=null&&ground.boss==id)return ground;return null;}
        /// <summary>Whether a point is on the field, this far in from its kerb.</summary>
        public bool Holds(Vector3 point,float inset=0)
        {
            var o=point-transform.position;
            return Mathf.Abs(o.x)<=half.x-inset&&Mathf.Abs(o.z)<=half.y-inset;
        }
        /// <summary>The nearest point to this one that is on the field, this far in from its kerb, at the point's own height.</summary>
        public Vector3 Keep(Vector3 point,float inset=0)
        {
            var o=point-transform.position;
            o.x=Mathf.Clamp(o.x,-Mathf.Max(0,half.x-inset),Mathf.Max(0,half.x-inset));o.z=Mathf.Clamp(o.z,-Mathf.Max(0,half.y-inset),Mathf.Max(0,half.y-inset));
            var kept=transform.position+o;kept.y=point.y;return kept;
        }
    }
}
