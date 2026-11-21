using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// A wheel that lets water go: the Braided River's sluices, and later a swamp's pump or a forge's coolant valve, which are the same switch with other words and another sound. Its flag is what a
    /// <see cref="WaterVolume"/> drains on (or anything else listens to); the wheel only turns and is heard.
    /// A player has to see what a wheel will change before turning it. So a sluice stands at the head of its channel, and while a hero stands where it is worked, and again as the water goes, the
    /// view draws back to take in <see cref="shows"/>: the stretch of water that answers to it.
    /// </summary>
    public sealed class FlowSwitch:WorldSwitch
    {
        static readonly Rect FramedIn=new(.06f,.18f,.88f,.74f);
        protected override string Kind=>"SLUICE_OPEN";
        /// <summary>What turns, about which of its own axes, how far and for how long.</summary>
        public Transform wheel;public Vector3 axis=Vector3.forward;public float turns=1.5f,seconds=2.6f;
        public string sound="water_rush";public float volume=.45f;
        /// <summary>The water this changes, as a box in the world, for the view to take in. No size: the view stays where it is.</summary>
        public Bounds shows;
        /// <summary>How near a hero stands to count as at the wheel, past the reach of the point it is worked from (or from the wheel itself, with no point).</summary>
        public float showsFrom=2.5f;
        float turning=-1;Quaternion rest;bool measured;CameraRig rig;
        void Measure(){if(measured)return;measured=true;if(wheel!=null)rest=wheel.localRotation;}
        void Awake()=>Measure();
        protected override void Settle(){Measure();if(wheel!=null)wheel.localRotation=rest*Quaternion.AngleAxis(turns*360,axis);}
        protected override void Work()
        {
            Measure();turning=0;
            AudioManager.PlayAt(sound,transform,transform.position+Vector3.up,volume,AudioBus.SFX,40);
            CombatVfx.Burst(transform.position+Vector3.up*.6f,new Color(.8f,.92f,.9f,.5f),"steam");
        }
        protected override void Update()
        {
            base.Update();
            if(turning>=0&&!GameTime.Paused)
            {
                turning+=Time.deltaTime;
                if(wheel!=null)wheel.localRotation=rest*Quaternion.AngleAxis(turns*360*Mathf.SmoothStep(0,1,Mathf.Clamp01(turning/seconds)),axis);
                // The view holds a moment past the wheel's stop: the water is still going down.
                if(turning>=seconds+1.4f)turning=-1;
            }
            Show();
        }
        void Show()
        {
            if(shows.size.sqrMagnitude<=0||GameServices.Current==null||GameServices.Current.Input.Blocked)return;
            var party=PartyController.Current;if(party==null||party.Active==null)return;
            var at=party.Active.transform.position;
            if(turning<0)
            {
                // Not yet turned: only for someone standing at it, and only while it is still there to turn.
                if(Worked||point!=null&&!point.Wanted)return;
                var offset=at-(point!=null?point.transform.position:transform.position);offset.y=0;
                if(offset.magnitude>(point!=null?point.range:0)+showsFrom)return;
            }
            if(rig==null)rig=FindFirstObjectByType<CameraRig>();if(rig==null)return;
            var view=new Bounds(at+Vector3.up,Vector3.one*3);view.Encapsulate(shows);
            rig.FrameSceneInterest(this,view,FramedIn);
        }
    }
}
