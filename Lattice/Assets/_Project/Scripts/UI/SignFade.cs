using Lattice.Combat;
using Lattice.Core;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>A word lying in the world keeps out of the way of what's on the screen. It goes for a conversation or a menu, which printed it through the box and under the panels. It goes
    /// for a boss bar, which sits over the lower middle of the screen. It goes while it's cut, by a fixed plate or by the screen's edge: half a word under the hint bar read as a fault.
    /// And a floor sign thins out under the hero who's standing on it, and only then: it was a ghost 2 m off, at the very moment the party had reached the place it names.</summary>
    [DefaultExecutionOrder(910)]
    public sealed class SignFade:MonoBehaviour
    {
        // At the sign's centre it's under the hero's own feet. Out to 4.5 m a sign beside the party read at a quarter of its strength.
        const float HeroFloor=.2f,HeroFar=1.6f,AsidePerSecond=5;
        static readonly int FadeId=Shader.PropertyToID("_Fade");
        static MaterialPropertyBlock block;
        /// <summary>A sign that hangs over the party, a person's name, or a sign lying below the ships: nobody stands on it, so no hero moves it.</summary>
        public bool bossOnly;
        TextMeshPro text;Renderer plate;float full,clear=1,halfWide,halfHigh;string measured;
        void Awake()
        {
            text=GetComponent<TextMeshPro>();full=text.alpha;
            var backing=transform.Find(SignStyle.PlateName);if(backing!=null)plate=backing.GetComponent<Renderer>();
        }
        void LateUpdate()
        {
            var party=PartyController.Current;if(party==null||party.Active==null)return;
            var lead=party.Active.transform.position;var input=GameInput.Current;
            clear=Mathf.MoveTowards(clear,input!=null&&input.Blocked||BossController.Focus(lead)!=null||Cut()?0:1,Time.unscaledDeltaTime*AsidePerSecond);
            float shown=clear;
            if(!bossOnly){var offset=transform.position-lead;offset.y=0;shown*=Mathf.Lerp(HeroFloor,1,Mathf.Clamp01(offset.magnitude/HeroFar));}
            float alpha=full*shown;
            if(Mathf.Abs(text.alpha-alpha)<=.01f&&alpha>0)return;
            if(text.alpha==alpha)return;
            text.alpha=alpha;
            if(plate==null)return;
            block??=new MaterialPropertyBlock();
            plate.GetPropertyBlock(block);block.SetFloat(FadeId,shown);plate.SetPropertyBlock(block);
        }
        /// <summary>Its letters' rect on the screen against the plates: two points a frame, for the few signs in a zone. A sign reads level to the camera, so two corners are its rect. A name's words change with its mark, so they're measured when they do.</summary>
        bool Cut()
        {
            var view=Camera.main;if(view==null)return false;
            if(!ReferenceEquals(measured,text.text))
            {
                measured=text.text;var size=text.GetPreferredValues();var scale=transform.lossyScale;
                halfWide=size.x*scale.x*.5f;halfHigh=size.y*scale.y*.5f;
            }
            Vector3 at=transform.position,across=transform.right*halfWide,up=transform.up*halfHigh;
            Vector3 one=view.WorldToScreenPoint(at-across-up),two=view.WorldToScreenPoint(at+across+up);
            if(one.z<=0||two.z<=0)return false;
            return PanelFade.Cuts(Rect.MinMaxRect(Mathf.Min(one.x,two.x),Mathf.Min(one.y,two.y),Mathf.Max(one.x,two.x),Mathf.Max(one.y,two.y)),view.pixelWidth,view.pixelHeight);
        }
    }
}
