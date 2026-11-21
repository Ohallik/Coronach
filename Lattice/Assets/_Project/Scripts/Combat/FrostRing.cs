using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Frost coming in from a room's rim (chapter 6, the Host Marshal's second phase): a band of rime between its edge and the room's rim that closes toward the middle, from
    /// <see cref="outer"/> to <see cref="inner"/> over <see cref="seconds"/>, and holds there. A hero standing on it is slowed to <see cref="pace"/> (it is footing, as shallows are:
    /// <see cref="Health.Hinder"/>); it never strikes. So it is drawn as a boundary, pale and under the danger colours (D217, D222), and the room it leaves is where the fight is.
    /// One at a time; whoever lays it dismisses it. Logs <c>FROST_RING</c> as it is laid and <c>FROST_RING_CLOSED</c> as it stops.
    /// </summary>
    public sealed class FrostRing:MonoBehaviour
    {
        public static FrostRing Current{get;private set;}
        /// <summary>The rim it comes in from, the edge it stops at, how long it takes, and the share of pace a hero on it keeps.</summary>
        public float outer=17,inner=9,seconds=70,pace=.6f;
        /// <summary>Rime: pale, cold, and a boundary (alpha under .99), never a danger.</summary>
        public static readonly Color Rime=new(.72f,.86f,1f,.4f);
        Telegraph band;float laid,drawnEdge=-1;bool closed;
        /// <summary>Where its inner edge is now.</summary>
        public float Edge=>Mathf.Lerp(outer,inner,Mathf.Clamp01((GameTime.Now-laid)/Mathf.Max(.01f,seconds)));
        public static FrostRing Lay(Vector3 centre,float outer,float inner,float seconds,float pace)
        {
            if(Current!=null)Current.Dismiss();
            var go=new GameObject("Frost ring");go.transform.position=centre;
            var ring=go.AddComponent<FrostRing>();ring.outer=outer;ring.inner=inner;ring.seconds=seconds;ring.pace=pace;ring.laid=GameTime.Now;Current=ring;
            ring.band=Telegraph.Ring(centre,outer-.05f,outer,1,Rime);ring.band.owner=ring;ring.band.Steady=true;
            Debug.Log($"FROST_RING outer={outer:0} inner={inner:0} seconds={seconds:0}");
            return ring;
        }
        /// <summary>Is a point on the frost.</summary>
        public bool Holds(Vector3 point){var offset=point-transform.position;offset.y=0;float distance=offset.magnitude;return distance>=Edge&&distance<=outer+.5f;}
        public void Dismiss(){if(band!=null)band.Clear();band=null;if(Current==this)Current=null;Destroy(gameObject);}
        void Update()
        {
            if(GameTime.Paused)return;
            float edge=Edge;
            // Laying a ring on the ground samples it: a quarter of a metre at a time is smooth to the eye and costs nothing between.
            if(band!=null&&Mathf.Abs(edge-drawnEdge)>.25f){band.SetRing(edge,outer);drawnEdge=edge;}
            if(!closed&&edge<=inner+.01f){closed=true;Debug.Log("FROST_RING_CLOSED");}
            var party=PartyController.Current;if(party==null)return;
            foreach(var hero in party.members)if(hero.Health.Alive&&Holds(hero.transform.position))hero.Health.Hinder(pace);
        }
        void OnDestroy(){if(band!=null)band.Clear();if(Current==this)Current=null;}
    }
}
