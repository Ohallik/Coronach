using System;
using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Something a Clamp Tender keeps (chapter 7): a Bridle clamp in the engine hall, a stay in the Glass Choir's gallery. While it is <see cref="wanted"/> the nearest tender walks to its
    /// <see cref="seat"/> and works it there, and it says so (<see cref="Tending"/>, <see cref="Worked"/>, <see cref="Begun"/>); what being worked does to it is its own owner's to say (a
    /// clamp a hero is opening loses the weight he has taken and never eases; a stay may close again). It lives in Combat so the tender can find it without Combat knowing a stay:
    /// the world's own pieces carry one beside them. Logs <c>TENDED &lt;name&gt;</c> as a tender sets to work.
    /// </summary>
    public sealed class Tended:MonoBehaviour
    {
        public static readonly List<Tended> All=new();
        /// <summary>A tender has set to work on it: the thing, and who.</summary>
        public static event Action<Tended,EnemyBrain> Begun;
        /// <summary>Where its work is done, on the floor. None: here.</summary>
        public Transform seat;
        /// <summary>A tender should come and keep it now.</summary>
        public bool wanted;
        /// <summary>A hero has his hands on it: a tender at work there is closing what he is opening (its owner says so; the tender barks it).</summary>
        [NonSerialized] public bool contested;
        float tendedUntil=-1;
        public Vector3 Seat=>seat!=null?seat.position:transform.position;
        /// <summary>A tender is at work on it this moment.</summary>
        public bool Tending=>Time.time<tendedUntil&&By!=null&&By.Health.Alive;
        /// <summary>The tender at it (or last at it), and the seconds it has been worked since it was last wanted.</summary>
        public EnemyBrain By{get;private set;}
        public float Worked{get;private set;}
        /// <summary>A tender works it for a frame. The first frame of a new tender's work is the one <see cref="Begun"/> answers.</summary>
        public void Tend(EnemyBrain tender,float seconds)
        {
            bool fresh=!Tending||By!=tender;
            By=tender;tendedUntil=Time.time+.2f;Worked+=Mathf.Max(0,seconds);
            if(fresh){Debug.Log("TENDED "+name);Begun?.Invoke(this,tender);}
        }
        /// <summary>Its owner has answered what was worked (a clamp opened, a stay set again): the count starts over.</summary>
        public void Clear(){Worked=0;tendedUntil=-1;By=null;}
        /// <summary>The wanted one nearest a point, within a distance, that no other tender is at.</summary>
        public static Tended Nearest(Vector3 point,float within,EnemyBrain asking)
        {
            Tended best=null;float least=within*within;
            foreach(var work in All)
            {
                if(work==null||!work.wanted||!work.isActiveAndEnabled)continue;
                if(work.Tending&&work.By!=asking)continue;
                var o=work.Seat-point;o.y=0;if(o.sqrMagnitude>=least)continue;
                least=o.sqrMagnitude;best=work;
            }
            return best;
        }
        /// <summary>A body that keeps clamps and stays shut: a Clamp Tender, known by what it is (the Tender behaviour), not by one chapter's id. Chapter 7's <c>ClampTender</c> and chapter
        /// 10's <c>StillwaterTender</c> (the same body at the last chapter's weight, D296) are both.</summary>
        public static bool IsTender(Health body)=>body!=null&&body.TryGetComponent<EnemyBrain>(out var brain)&&brain.definition!=null&&brain.definition.behaviour==Lattice.Data.EnemyBehaviourKind.Tender;
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
    }
}
