using Lattice.Combat;
using Lattice.Core;
using Lattice.World;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>A word lying in the world keeps out of the way of what's on the screen. It goes for a conversation, a menu or the chapter's card, which printed it through the box and under the panels. It goes
    /// for a boss bar, which sits over the lower middle of the screen. It goes while it's cut, by a fixed plate or by the screen's edge: half a word under the hint bar read as a fault.
    /// And a floor sign thins out under the hero who's standing on it, and only then: it was a ghost 2 m off, at the very moment the party had reached the place it names.</summary>
    [DefaultExecutionOrder(910)]
    public sealed class SignFade:MonoBehaviour
    {
        // At the sign's centre it's under the hero's own feet. Out to 4.5 m a sign beside the party read at a quarter of its strength.
        const float HeroFloor=.2f,HeroFar=1.6f;
        /// <summary>A tenth of a second. At a fifth, slower than the plates' own fade, a sign that had started to go was still there at half strength under the skill icons ('E RY ALL') and through a plate that had stepped aside.</summary>
        const float AsidePerSecond=10;
        /// <summary>How far ahead a moving sign is looked for, as a share of the screen's height: enough for a run and the camera catching up, and no more, since a jump isn't a heading.</summary>
        const float MostAhead=.12f;
        static readonly int FadeId=Shader.PropertyToID("_Fade");
        static MaterialPropertyBlock block;
        /// <summary>A sign that hangs over the party, a person's name, or a sign lying below the ships: nobody stands on it, so no hero moves it.</summary>
        public bool bossOnly;
        TextMeshPro text;Renderer plate;float full,clear=1,halfWide,halfHigh,held=1;string measured;
        bool holdsSize,tracked;Vector3 sized;Vector2 lastAt;
        void Awake()
        {
            text=GetComponent<TextMeshPro>();full=text.alpha;sized=transform.localScale;
            var backing=transform.Find(SignStyle.PlateName);if(backing!=null)plate=backing.GetComponent<Renderer>();
            var zone=ZoneController.Current;holdsSize=zone!=null&&zone.Flight&&zone.definition!=null&&zone.definition.cameraProfile!=null&&GetComponentInParent<Npc>(true)==null;
        }
        void LateUpdate()
        {
            var party=PartyController.Current;if(party==null||party.Active==null)return;
            var lead=party.Active.transform.position;var input=GameInput.Current;var view=Camera.main;
            if(holdsSize&&view!=null)HoldSize(view,lead.y);
            clear=Mathf.MoveTowards(clear,input!=null&&input.Blocked||QuestToast.CardUp||BossController.Focus(lead)!=null||Cut(view)?0:1,Time.unscaledDeltaTime*AsidePerSecond);
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
        /// <summary>A flight's camera draws back far past what a zone's signs were sized for: 60 m on the Tallow line against the 26 they stand at, and the Halo's warp sign went to 12 px letters just as the player looked for the way out. A sign holds its size on the screen. How far back goes by the camera's height over the hero, so a sign up the screen isn't taken for one further off.</summary>
        void HoldSize(Camera view,float plane)
        {
            var zone=ZoneController.Current;if(zone==null)return;var look=zone.definition.cameraProfile;
            float wanted=Mathf.Max(1,(view.transform.position.y-plane)/(look.distance*Mathf.Sin(look.pitch*Mathf.Deg2Rad)));
            // At rest it's exactly the size it was made: the camera's height settles a hair over, and the step rule below would have left it up to 3% large for good.
            if(wanted<1.01f)wanted=1;
            if(Mathf.Abs(wanted-held)<=held*.03f&&(wanted>1||held==1))return;
            held=wanted;transform.localScale=sized*held;
        }
        /// <summary>Its letters' rect on the screen against the plates: two points a frame, for the few signs in a zone. A sign reads level to the camera, so two corners are its rect. A name's words change with its mark, so they're measured when they do.
        /// Where it's heading counts as well as where it is, by as far as it'll go while it fades, so at a run it has gone by the time it gets there.</summary>
        bool Cut(Camera view)
        {
            if(view==null)return false;
            if(!ReferenceEquals(measured,text.text)){measured=text.text;var size=text.GetPreferredValues();halfWide=size.x*.5f;halfHigh=size.y*.5f;}
            var scale=transform.lossyScale;
            Vector3 at=transform.position,across=transform.right*(halfWide*scale.x),up=transform.up*(halfHigh*scale.y);
            Vector3 one=view.WorldToScreenPoint(at-across-up),two=view.WorldToScreenPoint(at+across+up);
            if(one.z<=0||two.z<=0)return false;
            var box=Rect.MinMaxRect(Mathf.Min(one.x,two.x),Mathf.Min(one.y,two.y),Mathf.Max(one.x,two.x),Mathf.Max(one.y,two.y));
            float width=view.pixelWidth,height=view.pixelHeight;var middle=box.center;
            var ahead=tracked?(middle-lastAt)/(Mathf.Max(Time.unscaledDeltaTime,.001f)*AsidePerSecond):Vector2.zero;lastAt=middle;tracked=true;
            if(PanelFade.Cuts(box,width,height))return true;
            if(ahead.sqrMagnitude<1)return false;
            float most=height*MostAhead;if(ahead.sqrMagnitude>most*most)ahead=ahead.normalized*most;
            box.position+=ahead;return PanelFade.Cuts(box,width,height);
        }
    }
}
