using System;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// What holds a place where it is, cut: one of the Walking Canopy's anchor roots, and later an ice shelf's couplings. When its flag is set the whole place lurches: the view heaves and shudders,
    /// the timber groans, dust comes off every door that moves. The doors themselves are <see cref="FlagGate"/>s on the same flag, each a solid thing with a carving NavMeshObstacle, so the ways
    /// through change as a membrane's do and nothing is baked again; this only makes the moment, and points at where the place answered (<see cref="answers"/>).
    /// </summary>
    public sealed class ShiftAnchor:WorldSwitch
    {
        /// <summary>An anchor has just gone, for anything that wants the moment itself and not just the flag.</summary>
        public static event Action<ShiftAnchor> Lurched;
        protected override string Kind=>"ANCHOR_CUT";
        /// <summary>The root taut, and the root cut: one is shown at a time. Either can be missing.</summary>
        public GameObject whole,severed;
        /// <summary>The lurch: how long the view shudders and how far, in metres at the camera's own distance (a boss's slam is about .1).</summary>
        public float tremorSeconds=1.3f,tremorAmplitude=.16f;
        /// <summary>Where the place answers: the doors this anchor opens and shuts. Dust comes off each.</summary>
        public Transform[] answers=Array.Empty<Transform>();
        public string sound="root_groan";public float volume=.5f;
        void Show(bool cut){if(whole!=null)whole.SetActive(!cut);if(severed!=null)severed.SetActive(cut);}
        void Start(){if(!Worked)Show(false);}
        protected override void Settle()=>Show(true);
        protected override void Work()
        {
            Show(true);
            var rig=FindFirstObjectByType<CameraRig>();
            // A heave to one side and the shudder after it. Both are laid over the framing: the party's view never turns or loses them.
            if(rig!=null){rig.Impulse(Vector3.right,CameraRig.MostShake,.45f);rig.Tremor(tremorSeconds,tremorAmplitude);}
            AudioManager.PlayAt(sound,transform,transform.position+Vector3.up,volume,AudioBus.SFX,30);
            CombatVfx.Burst(transform.position+Vector3.up*.8f,new Color(.74f,.66f,.5f,.6f),"shape");
            foreach(var door in answers)if(door!=null)CombatVfx.Burst(door.position+Vector3.up*1.2f,new Color(.7f,.64f,.52f,.5f),"steam");
            Lurched?.Invoke(this);
        }
    }
}
