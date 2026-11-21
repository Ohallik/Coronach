using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// A place in a flight lane where the view draws back to take in <see cref="shows"/> as well as the party: the Orrery's first gates, so the machine is on screen whole while Ishen
    /// names its parts (chapter 7). It holds from the moment the lead ship comes within <see cref="enter"/> of this object until it is <see cref="exit"/> away, and never again once
    /// <see cref="untilFlag"/> is set (the talk it frames has been heard). It renews its hold through a conversation, as a pump's view does over its water (<see cref="FlowSwitch"/>).
    /// </summary>
    public sealed class VantageView:MonoBehaviour
    {
        public Bounds shows;public float enter=45,exit=60;public string untilFlag="";
        public Rect framedIn=new(.06f,.18f,.88f,.74f);
        CameraRig rig;bool holding;
        public bool Holding=>holding;
        void Update()
        {
            var services=GameServices.Current;var party=PartyController.Current;
            if(services==null||party==null||party.Active==null||!party.Active.flight||!string.IsNullOrEmpty(untilFlag)&&services.Flags.GetBool(untilFlag)){Clear();return;}
            var gap=party.Active.transform.position-transform.position;gap.y=0;
            if(gap.magnitude>(holding?exit:enter)){Clear();return;}
            if(rig==null)rig=FindFirstObjectByType<CameraRig>();if(rig==null)return;
            holding=true;
            var view=shows;view.Encapsulate(new Bounds(party.Active.transform.position,Vector3.one*4));
            rig.FrameSceneInterest(this,view,framedIn);
        }
        void Clear(){if(holding&&rig!=null)rig.FrameSceneInterest(this,null,framedIn);holding=false;}
        void OnDisable(){Clear();}
    }
}
