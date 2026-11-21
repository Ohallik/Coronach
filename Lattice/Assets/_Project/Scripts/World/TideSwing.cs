using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// A rope walk that the tide swings (Driftreed, chapter 4: when the tide turns two walks leave and two arrive, and the mire gate comes within reach). The drawn walk turns about its
    /// own pivot from <see cref="fromYaw"/> to <see cref="toYaw"/> over <see cref="seconds"/> when the flag is set; a save that has the flag loads with it already across.
    /// Feet go by its ends: each end is a <see cref="Membrane"/> (a rope strung across the gap, a box a body stops at and a carving obstacle) that is up while the walk is away and down
    /// while it is across, so the floor the bake laid under the walk's crossing is only ever reached when the walk is there. The world tests that open every seal open these too.
    /// Logs <c>TIDE_TURNED flag</c>.
    /// </summary>
    public sealed class TideSwing:WorldSwitch
    {
        protected override string Kind=>"TIDE_TURNED";
        /// <summary>What swings, turned about its own position.</summary>
        public Transform walk;
        public float fromYaw,toYaw,seconds=7;
        /// <summary>True for a walk the tide brings across; false for one it takes away.</summary>
        public bool arrives;
        /// <summary>The ropes across each end.</summary>
        public Membrane[] ends=System.Array.Empty<Membrane>();
        public string sound="root_groan";
        void Start(){if(!Worked)Place(0);}
        protected override void Settle()=>Place(1);
        protected override void Work()=>StartCoroutine(Swing());
        bool Across(float t)=>arrives?t>=1:t<=0;
        void Place(float t)
        {
            if(walk!=null)walk.localRotation=Quaternion.Euler(0,Mathf.LerpAngle(fromYaw,toYaw,Mathf.SmoothStep(0,1,t)),0);
            bool across=Across(t);foreach(var end in ends)if(end!=null&&end.Open!=across)end.SetOpen(across);
        }
        bool swinging;
        IEnumerator Swing()
        {
            // Both ends close the moment it moves: nobody steps onto a walk that is leaving, or one that hasn't got there.
            swinging=true;
            foreach(var end in ends)if(end!=null&&end.Open)end.SetOpen(false);
            if(walk!=null)AudioManager.PlayAt(sound,walk,walk.position+Vector3.up,.35f,AudioBus.SFX,45);
            for(float t=0;t<1;t+=Time.deltaTime/Mathf.Max(.1f,seconds)){if(walk!=null)walk.localRotation=Quaternion.Euler(0,Mathf.LerpAngle(fromYaw,toYaw,Mathf.SmoothStep(0,1,t)),0);yield return null;}
            swinging=false;Place(1);
        }
        /// <summary>An end is open only where the walk is. Something else that opens an end (the world tests' "open every seal", which shows the town with every way open) brings the walk
        /// across with it, so an open end never gives onto water: it did, and a body pressed through one fell into the gap under the rim and stuck.</summary>
        void LateUpdate()
        {
            if(swinging||walk==null)return;
            bool open=false;foreach(var end in ends)if(end!=null&&end.Open){open=true;break;}
            if(!open)return;
            float across=arrives?toYaw:fromYaw;
            if(Mathf.Abs(Mathf.DeltaAngle(walk.localEulerAngles.y,across))<.5f)return;
            walk.localRotation=Quaternion.Euler(0,across,0);foreach(var end in ends)if(end!=null&&!end.Open)end.SetOpen(true);
        }
    }
}
