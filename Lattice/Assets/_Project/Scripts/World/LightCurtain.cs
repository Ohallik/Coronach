using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>The strands of a curtain of relay light, apart from the door they show: the door goes at once, so nothing waits on a fade to
    /// fly through. The first time the curtain opens it parts from the middle with an amber burst, a sound and a line from Sela. The relay that
    /// opens it can be 30 m away with the curtain off screen, and readers heard it come down with nothing to see, so a beam runs from the relay to
    /// the curtain and the view pulls back to show it going. A save that already has it open loads with nothing there.</summary>
    public sealed class LightCurtain:MonoBehaviour
    {
        const float Parting=3.2f,BurstSpacing=24,SweepTime=.7f;
        /// <summary>From further south than this the curtain is too far off to frame with the ship: there the beam shows which way it went.</summary>
        const float FramedWithin=70;
        static readonly Rect FramedIn=new(.06f,.18f,.88f,.74f);
        public string flag="wick.gateOpen",speaker="Sela",bark="gate.open";
        public LineRenderer sweep;
        LineRenderer[] strands;Color[] bottoms,tops;float[] delays;float elapsed=-1;
        CameraRig rig;Vector3 sweepFrom,sweepTo;Color sweepTone;
        void Start()
        {
            if(GameServices.Current==null)return;
            if(GameServices.Current.Flags.GetBool(flag)){gameObject.SetActive(false);return;}
            strands=GetComponentsInChildren<LineRenderer>();bottoms=new Color[strands.Length];tops=new Color[strands.Length];delays=new float[strands.Length];
            float widest=0;
            for(int i=0;i<strands.Length;i++)
            {
                bottoms[i]=strands[i].startColor;tops[i]=strands[i].endColor;
                delays[i]=Mathf.Abs(strands[i].transform.TransformPoint(strands[i].GetPosition(0)).x-transform.position.x);widest=Mathf.Max(widest,delays[i]);
            }
            for(int i=0;i<strands.Length;i++)delays[i]=widest>0?delays[i]/widest*Parting*.5f:0;
        }
        void Update()
        {
            if(strands==null||GameServices.Current==null)return;
            if(elapsed<0)
            {
                // A conversation on screen holds the moment: Sela's line would be hidden behind it.
                if(!GameServices.Current.Flags.GetBool(flag)||GameServices.Current.Input.Blocked)return;
                elapsed=0;Open();
            }
            elapsed+=Time.deltaTime;
            for(int i=0;i<strands.Length;i++)
            {
                float left=1-Mathf.Clamp01((elapsed-delays[i])/(Parting*.5f));
                strands[i].startColor=bottoms[i]*left;strands[i].endColor=tops[i]*left;
            }
            Frame();
            if(sweep!=null&&sweep.enabled)
            {
                sweep.SetPosition(1,Vector3.Lerp(sweepFrom,sweepTo,Mathf.Clamp01(elapsed/SweepTime)));
                sweep.startColor=sweep.endColor=sweepTone*(1-Mathf.Clamp01((elapsed-SweepTime)/(Parting-SweepTime)));
            }
            if(elapsed>=Parting)
            {
                if(sweep!=null)sweep.enabled=false;
                gameObject.SetActive(false);
            }
        }
        /// <summary>The stretch of curtain nearest the ship.</summary>
        Vector3 Nearest(Vector3 ship)=>new(ship.x*.6f,transform.position.y,transform.position.z);
        /// <summary>The ship and that stretch in one view. The camera eases out, and back when the lease stops being renewed.</summary>
        void Frame()
        {
            var party=PartyController.Current;if(party==null||party.Active==null||rig==null)return;
            var at=party.Active.transform.position;if(Mathf.Abs(transform.position.z-at.z)>FramedWithin)return;
            var door=Nearest(at);var view=new Bounds(at,Vector3.one*3);
            view.Encapsulate(door+new Vector3(-10,-6,0));view.Encapsulate(door+new Vector3(10,5,0));
            rig.FrameSceneInterest(this,view,FramedIn);
        }
        void Open()
        {
            for(int i=-2;i<=2;i++)CombatVfx.Burst(transform.position+Vector3.right*i*BurstSpacing,new Color(1,.7f,.3f),"kill");
            AudioManager.PlayAt("doorOpen_000",null,Vector3.zero,.3f,AudioBus.SFX,40,.06f,.6f);
            BarkService.Play(speaker,bark,true);
            Debug.Log("CURTAIN_OPENED "+flag);
            rig=FindFirstObjectByType<CameraRig>();
            var party=PartyController.Current;if(sweep==null||party==null||party.Active==null)return;
            var at=party.Active.transform.position;float nearest=float.PositiveInfinity;bool found=false;
            foreach(var lantern in RelayLantern.All)
            {
                if(lantern==null||!lantern.Lit)continue;
                float gap=(lantern.transform.position-at).sqrMagnitude;if(gap>=nearest)continue;
                nearest=gap;found=true;sweepFrom=lantern.glow!=null?lantern.glow.transform.position:lantern.transform.position;
            }
            if(!found)return;
            sweepTo=Nearest(at);sweepTone=sweep.startColor;sweep.SetPosition(0,sweepFrom);sweep.SetPosition(1,sweepFrom);sweep.enabled=true;
        }
    }
}
