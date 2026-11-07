using Lattice.Combat;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>A floor sign is read from across a room. A hero standing on one read it across their own legs, so it steps aside; and every sign steps
    /// aside for a boss bar, which sits over the lower middle of the screen and made a sign run through the fight's own words.</summary>
    [DefaultExecutionOrder(910)]
    public sealed class SignFade:MonoBehaviour
    {
        // It only has to clear the hero's legs. Out to 6.5 m a sign was half faded for most of the time it was on a screen that follows the hero.
        const float Near=1.5f,Far=4.5f,BossFadePerSecond=3;
        static readonly int FadeId=Shader.PropertyToID("_Fade");
        static MaterialPropertyBlock block;
        /// <summary>A sign that hangs over the party, or lies below the ships: nobody stands on it, so only a boss bar moves it.</summary>
        public bool bossOnly;
        TextMeshPro text;Renderer plate;float full,clear=1;
        void Awake()
        {
            text=GetComponent<TextMeshPro>();full=text.alpha;
            var backing=transform.Find(SignStyle.PlateName);if(backing!=null)plate=backing.GetComponent<Renderer>();
        }
        void LateUpdate()
        {
            var party=PartyController.Current;if(party==null||party.Active==null)return;
            var lead=party.Active.transform.position;
            clear=Mathf.MoveTowards(clear,BossController.Focus(lead)!=null?0:1,Time.unscaledDeltaTime*BossFadePerSecond);
            float shown=clear;
            if(!bossOnly){var offset=transform.position-lead;offset.y=0;shown*=Mathf.Clamp01((offset.magnitude-Near)/(Far-Near));}
            float alpha=full*shown;
            if(Mathf.Abs(text.alpha-alpha)<=.01f&&alpha>0)return;
            if(text.alpha==alpha)return;
            text.alpha=alpha;
            if(plate==null)return;
            block??=new MaterialPropertyBlock();
            plate.GetPropertyBlock(block);block.SetFloat(FadeId,shown);plate.SetPropertyBlock(block);
        }
    }
}
