using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The fourth clamp's shelf at Sorrel (docs/maps/SorrelSecondSky.md, "Boss arena contract"): "the fourth clamp is opened racing alongside her as she starts to move". On the root of the
    /// fourth shelf's berth (wiring 6.1: a moving berth, everything under one root), put there by <c>LastMooringBuilder.Mother.StandStrain</c>. From the third clamp open until she goes
    /// (<see cref="Straining"/>, which her fight sets every frame) it swings round her middle (<see cref="pivot"/>) from where it was built out to <see cref="toBearing"/> and back, every
    /// <see cref="period"/> seconds, and its way is marked at the plane, faintly: a boundary, never a danger (D222). It moves only through the berth (<see cref="IBerth.Drive"/>), which
    /// refuses while the party stands on it, is changing onto or off it, or a conversation holds the controls (wiring 6.1 rule 5, D275): its own clock stands still then too, so it takes up
    /// again where it stopped. With no berth on it (a yard), it moves its own transform, held the same way. Logs <c>STRAIN_SWINGS</c> as it first moves, <c>STRAIN_HELD</c> as a hold stops it.
    /// </summary>
    public sealed class MotherStrain:MonoBehaviour
    {
        /// <summary>Her fight sets this every frame it strains; it lapses by itself a frame after she stops saying so (she went, or the scene changed).</summary>
        public static bool Straining{get=>Time.frameCount-strainingFrame<=2;set{if(value)strainingFrame=Time.frameCount;else strainingFrame=-10;}}
        static int strainingFrame=-10;
        /// <summary>Her middle, on the plane; the shelf's bearing as built and the one it swings out to (degrees clockwise from north, round the pivot); how far out its pad is (where its
        /// way is marked); a swing's whole period.</summary>
        public Vector3 pivot;
        public float fromBearing=120,toBearing=92,radius=27;
        public float period=16;
        /// <summary>Seconds it has swung (its own clock: it stops whenever it may not move).</summary>
        public float Clock{get;private set;}
        public float Swing=>.5f-.5f*Mathf.Cos(2*Mathf.PI*Clock/Mathf.Max(1,period));
        IBerth berth,under;Vector3 restPosition;Quaternion restRotation;bool started,swung,heldSaid;
        Telegraph[] marks;
        void Start()
        {
            // On a berth's own object it moves through the berth. On a root with the berth somewhere under it (Sorrel's fourth shelf: the zone declares the berth inside the shelf's
            // frame), it moves the root, and the berth under it says when it may: looking only on its own object it found none, and swung with the party standing on the shelf (the
            // deck slid ten metres in eight seconds under the lead's feet and out of his hands' reach, MooringBossReplayTests' first run).
            berth=GetComponent<IBerth>();if(berth==null)under=GetComponentInChildren<IBerth>(true);
            restPosition=transform.position;restRotation=transform.rotation;started=true;
        }
        /// <summary>It may move now: nobody on it and no change under way (its berth says, or the berth under it), and no conversation or pause holding the fight.</summary>
        public bool MayMove=>!GameTime.CombatHeld&&(berth==null||berth.MayMove)&&(under==null||under.MayMove);
        void Update()
        {
            if(!started)return;
            bool straining=Straining;
            if(straining&&!MayMove){if(!heldSaid){heldSaid=true;Debug.Log("STRAIN_HELD");}return;}
            heldSaid=false;
            if(straining){Clock+=Time.deltaTime;if(!swung){swung=true;Debug.Log("STRAIN_SWINGS");}}
            Mark(straining);
            if(!swung)return;
            // Round her middle, out and back: the shelf keeps its own facing toward her (its root may stand anywhere: at her middle, or under the shelf).
            float turn=Mathf.DeltaAngle(fromBearing,toBearing)*Swing;var spin=Quaternion.Euler(0,turn,0);
            var at=pivot+spin*(restPosition-pivot);at.y=restPosition.y;var facing=spin*restRotation;
            if(berth!=null)berth.Drive(at,facing);else transform.SetPositionAndRotation(at,facing);
        }
        /// <summary>Its way, at the plane: four faint strips along the arc from where it was built to where it swings to, while it strains.</summary>
        void Mark(bool on)
        {
            if(!on){if(marks!=null){foreach(var m in marks)if(m!=null)m.Cancel();marks=null;}return;}
            if(marks!=null)return;
            const int Pieces=4;marks=new Telegraph[Pieces];float r=radius,sweep=Mathf.DeltaAngle(fromBearing,toBearing);
            for(int i=0;i<Pieces;i++)
            {
                float a=fromBearing+sweep*i/Pieces,b=fromBearing+sweep*(i+1)/Pieces;
                var from=pivot+Quaternion.Euler(0,a,0)*Vector3.forward*r;var to=pivot+Quaternion.Euler(0,b,0)*Vector3.forward*r;from.y=to.y=pivot.y;
                var way=to-from;marks[i]=Telegraph.Line(from,way,way.magnitude,2.4f,9999,PathTone);marks[i].Steady=true;marks[i].owner=this;
            }
        }
        /// <summary>A boundary's colour (under .99 alpha), the clamps' cream: where the shelf will be, never where a blow lands.</summary>
        static readonly Color PathTone=new(1f,.93f,.74f,.34f);
        void OnDisable(){if(marks!=null)foreach(var m in marks)if(m!=null)m.Cancel();marks=null;}
    }
}
