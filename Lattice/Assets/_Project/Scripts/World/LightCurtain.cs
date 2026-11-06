using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>The strands of a curtain of relay light, apart from the door they show: the door goes at once, so nothing waits on a fade to
    /// fly through. The first time the curtain opens it parts from the middle with an amber burst, a sound and a line from Sela, because
    /// the relay that opens it can be 30 m away and off screen. A save that already has it open loads with nothing there.</summary>
    public sealed class LightCurtain:MonoBehaviour
    {
        const float Parting=2.4f,BurstSpacing=24;
        public string flag="wick.gateOpen",speaker="Sela",bark="gate.open";
        LineRenderer[] strands;Color[] bottoms,tops;float[] delays;float elapsed=-1;
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
            if(elapsed>=Parting)gameObject.SetActive(false);
        }
        void Open()
        {
            for(int i=-2;i<=2;i++)CombatVfx.Burst(transform.position+Vector3.right*i*BurstSpacing,new Color(1,.7f,.3f),"kill");
            AudioManager.PlayAt("doorOpen_000",null,Vector3.zero,.3f,AudioBus.SFX,40,.06f,.6f);
            BarkService.Play(speaker,bark,true);
            Debug.Log("CURTAIN_OPENED "+flag);
        }
    }
}
