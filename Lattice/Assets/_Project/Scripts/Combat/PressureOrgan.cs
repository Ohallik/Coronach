using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// One of the Bellows chamber's pressure organs. While any organ pumps, the
    /// Bellows is braced against the cave (half damage, armoured impacts), and
    /// its exhales blast out through the pumping organs in turn, each across its
    /// own half of the chamber. Breaking an organ collapses it visibly and makes
    /// its half safe: the player chooses which half to keep.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class PressureOrgan:MonoBehaviour
    {
        public static readonly List<PressureOrgan> All=new();
        public Transform visual;
        public Health Health{get;private set;}
        public bool Pumping=>Health!=null&&Health.Alive;
        public static bool AnyPumping{get{foreach(var organ in All)if(organ.Pumping)return true;return false;}}
        Vector3 rest;
        /// <summary>The chamber's dividing line runs through the organs' midpoint.</summary>
        public static Vector3 Middle{get{var sum=Vector3.zero;foreach(var organ in All)sum+=organ.transform.position;return All.Count>0?sum/All.Count:Vector3.zero;}}
        /// <summary>Is this point in the half of the chamber this organ blasts?</summary>
        public bool Covers(Vector3 point)
        {
            var mid=Middle;var side=transform.position-mid;var offset=point-mid;side.y=offset.y=0;
            return side.sqrMagnitude>.01f&&Vector3.Dot(offset,side)>0;
        }
        void Awake()
        {
            Health=GetComponent<Health>();if(visual!=null)rest=visual.localScale;
            Health.Died+=(_,__)=>
            {
                CombatVfx.Burst(transform.position+Vector3.up*1.5f,new Color(.55f,.35f,.9f),"shape");
                if(visual!=null)visual.localScale=new Vector3(rest.x*1.15f,rest.y*.35f,rest.z*1.15f);
                Debug.Log("ORGAN_DOWN "+name);
            };
        }
        void OnEnable()=>All.Add(this);
        void OnDisable()=>All.Remove(this);
        /// <summary>Organs swell with the Bellows' inhale and slump on its exhale.</summary>
        public void Breathe(float amount){if(Pumping&&visual!=null)visual.localScale=rest*(1+.18f*amount);}
    }
}
