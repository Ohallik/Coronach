using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// The formation the Debris Cutter is fought in (chapter 9, the Broken Ring; docs/maps/BrokenRing.md, "Cutter arena contract"): sets <see cref="formedFlag"/> once <see cref="need"/>
    /// districts (<see cref="SectionFlight"/>) hold their stations with their cover within <see cref="reach"/> of <see cref="centre"/> on the plane. The Cutter's door waits on that flag, so
    /// the fight never begins with fewer than two districts about it, and a save that has it (the door's own checkpoint) loads every district on station. One to a zone.
    /// Logs <c>RING_FORMED n</c>.
    /// </summary>
    public sealed class RingFormation:MonoBehaviour
    {
        public static RingFormation Current{get;private set;}
        public Vector3 centre;public float reach=45;public int need=2;public string formedFlag="ring.formed";
        void OnEnable()=>Current=this;
        void OnDisable(){if(Current==this)Current=null;}
        /// <summary>How many districts hold their stations with their cover within reach of the centre.</summary>
        public int Holding()
        {
            int n=0;var c=new Vector2(centre.x,centre.z);
            foreach(var section in SectionFlight.All)if(section!=null&&section.OnStation&&section.AtPlane&&SectionFlight.From(section.Footprint,c)<=reach)n++;
            return n;
        }
        /// <summary>How many districts are at the plane with their cover within reach of the centre, wherever they are on their way.</summary>
        public int Within()
        {
            int n=0;var c=new Vector2(centre.x,centre.z);
            foreach(var section in SectionFlight.All)if(section!=null&&section.AtPlane&&SectionFlight.From(section.Footprint,c)<=reach)n++;
            return n;
        }
        [SetsFlag("ring.formed")]
        void Update()
        {
            var services=GameServices.Current;if(services==null||string.IsNullOrEmpty(formedFlag)||services.Flags.GetBool(formedFlag))return;
            int n=Holding();if(n<need)return;
            services.Flags.SetBool(formedFlag,true);Debug.Log("RING_FORMED "+n);
        }
    }
}
