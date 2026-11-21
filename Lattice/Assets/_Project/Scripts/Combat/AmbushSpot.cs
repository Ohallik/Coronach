using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// A place an ambusher hides: reedy water for a Reedstalker, a lamp-post for a Lurelight, a blister of crust for an Ash Crawler. A builder places one wherever a hide should
    /// be (an empty object with this on it, on the floor); an ambusher with none in reach makes its own where it stands and two more beside it, so it fights anywhere.
    /// The rule a player learns once: <b>a hide with something in it is ringed</b>, and the ring is how close is too close. The ring is a boundary, not a danger (D222): nothing
    /// strikes there until the thing has come up and marked its bite. An empty hide shows nothing but whatever the builder dressed it with.
    /// </summary>
    public sealed class AmbushSpot:MonoBehaviour
    {
        public static readonly List<AmbushSpot> All=new();
        /// <summary>Reed and still water: pale, and no colour a danger or the party uses.</summary>
        public static readonly Color Ringed=new(.74f,.86f,.6f,.42f);
        /// <summary>Where the ring ends: a soft bright line, still faint by the rule (no outline, no hatch, nothing a danger is painted with). The wash alone was light lying on the
        /// ground with no line to it, and pale reed on a green bank read as nothing at all: the party stood beside three occupied hides and the bank looked empty (Braided River,
        /// November 13). "How close is too close" has to be a line a player can see.</summary>
        public static readonly Color Edge=new(.95f,1f,.82f,.96f);
        /// <summary>How wide the band is that the edge's line is drawn in.</summary>
        public const float EdgeBand=.24f;
        /// <summary>How near a hero comes before what's in it erupts. An occupant with a reach of its own (EnemyDef.tune.wake) sets it as it settles in.</summary>
        public float radius=3.2f;
        /// <summary>Made by an ambusher for itself: gone when the last body that could use it is.</summary>
        public bool made;
        public EnemyBrain Occupant{get;private set;}
        public bool Free=>Occupant==null||!Occupant.Health.Alive;
        Telegraph ring,edge;float nextRipple;
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable(){All.Remove(this);if(ring!=null)ring.Cancel();ring=null;if(edge!=null)edge.Cancel();edge=null;}
        public void Take(EnemyBrain who,float reach){Occupant=who;if(reach>0)radius=reach;}
        public void Leave(EnemyBrain who){if(Occupant==who)Occupant=null;}
        /// <summary>Its ring shows from the moment the thing is under: called by the occupant, since only it knows when it has gone down.</summary>
        public void Show(bool hidden)
        {
            if(hidden&&ring==null)
            {
                ring=Telegraph.Circle(transform.position,radius,99,Ringed);ring.Steady=true;ring.owner=this;
                edge=Telegraph.Ring(transform.position,Mathf.Max(0,radius-EdgeBand),radius,99,Edge);edge.Steady=true;edge.owner=this;
            }
            else if(!hidden&&ring!=null){ring.Cancel();ring=null;if(edge!=null)edge.Cancel();edge=null;}
        }
        public bool Shown=>ring!=null;
        public bool Within(Vector3 point){var offset=point-transform.position;offset.y=0;return offset.sqrMagnitude<=radius*radius;}
        /// <summary>The free hide nearest a point, within a reach, other than one.</summary>
        public static AmbushSpot Nearest(Vector3 from,float within,AmbushSpot not=null)
        {
            AmbushSpot best=null;float least=within*within;
            foreach(var spot in All)
            {
                if(spot==null||spot==not||!spot.Free)continue;
                var offset=spot.transform.position-from;offset.y=0;if(offset.sqrMagnitude>=least)continue;
                least=offset.sqrMagnitude;best=spot;
            }
            return best;
        }
        public static AmbushSpot Make(Vector3 at,float radius)
        {
            var spot=new GameObject("Ambush hide").AddComponent<AmbushSpot>();spot.transform.position=at;spot.radius=radius;spot.made=true;return spot;
        }
        void Update()
        {
            if(ring==null||GameTime.Paused||Time.time<nextRipple)return;
            // Something breathing under it: a ripple now and then, so the ring reads as occupied and not as paint.
            nextRipple=Time.time+1.3f;CombatVfx.Burst(transform.position+Vector3.up*.15f,new Color(Ringed.r,Ringed.g,Ringed.b,.7f),"failure");
        }
    }
}
