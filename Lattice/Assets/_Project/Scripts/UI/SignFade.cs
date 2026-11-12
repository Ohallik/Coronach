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
        /// <summary>A hero standing between the camera and a floor sign's letters: legs through the middle of a word read 'LO R CHAMBER' (E4-hushwell-03). He stands in front
        /// of the word, not on it, so measured from its middle he was too far off to thin it. Covered, it thins as it would under his feet, quickly, and comes back gently.</summary>
        const float CoveredFloor=.25f,UnveilPerSecond=3,BodyTall=1.8f,BodyHalfWide=.45f,InFront=.2f;
        /// <summary>A sign below this lies on the floor under the ships, and how much bigger than made a held sign may grow at most. In a flight zone a lying sign grows only as far as its
        /// letters stay over the floor: grown three times at the farthest pull, SALVAGE EDDY's outer letters went into the Gullet's curving bed (QA-037).</summary>
        const float LyingBelow=1,MostLying=3.4f,GrowthStep=.1f;
        /// <summary>A sign that hangs over the party, a person's name, or a sign lying below the ships: nobody stands on it, so no hero moves it.</summary>
        public bool bossOnly;
        TextMeshPro text;Renderer plate;float full,clear=1,halfWide,halfHigh,held=1,veil,mostGrowth=-1;string measured;
        bool holdsSize,tracked;Vector3 sized;Vector2 lastAt;
        static readonly RaycastHit[] floors=new RaycastHit[8];
        /// <summary>How far this sign may grow to hold its size: as far as the camera asks, unless it lies on a floor.</summary>
        public float MostGrowth{get{if(mostGrowth<0)mostGrowth=MeasureGrowth();return mostGrowth;}}
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
            if(!bossOnly)
            {
                bool covered=Covered(view,party);veil=Mathf.MoveTowards(veil,covered?1:0,Time.unscaledDeltaTime*(covered?AsidePerSecond:UnveilPerSecond));
                var offset=transform.position-lead;offset.y=0;
                shown*=Mathf.Min(Mathf.Lerp(HeroFloor,1,Mathf.Clamp01(offset.magnitude/HeroFar)),Mathf.Lerp(1,CoveredFloor,veil));
            }
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
            wanted=Mathf.Min(wanted,MostGrowth);
            if(Mathf.Abs(wanted-held)<=held*.03f&&(wanted>1||held==1)&&wanted!=MostGrowth)return;
            if(wanted==held)return;
            held=wanted;transform.localScale=sized*held;
        }
        /// <summary>The most a held sign grows before its letters go into the floor it lies on: grown about its middle, a tilted sign's lower letters come down toward the floor, and on a
        /// curving bed its ends meet the floor first. A sign standing clear of the floor, a warp's or a hanging one, grows as far as the camera asks.</summary>
        float MeasureGrowth()
        {
            if(!holdsSize||transform.position.y>=LyingBelow||text==null)return float.PositiveInfinity;
            var size=text.GetPreferredValues(text.text);float wide=size.x*.5f,high=size.y*.5f;
            float most=1;
            for(float grown=1+GrowthStep;grown<=MostLying+.001f;grown+=GrowthStep){if(!AboveTheFloor(grown,wide,high))break;most=grown;}
            return most;
        }
        /// <summary>Whether the letters, grown this much about the sign's middle, all stand over the floor below them: across the word, along its middle and its letters' lower half.</summary>
        bool AboveTheFloor(float grown,float wide,float high)
        {
            for(int i=0;i<=8;i++)for(int j=-1;j<=1;j++)
            {
                var point=transform.position+transform.TransformVector(new Vector3((i/4f-1)*wide,j*.5f*high,0))*grown;
                int count=Physics.RaycastNonAlloc(point+Vector3.up*3,Vector3.down,floors,6,~0,QueryTriggerInteraction.Ignore);
                for(int k=0;k<count;k++)
                {
                    var hit=floors[k].collider;
                    if(hit is CharacterController||hit.attachedRigidbody!=null||hit.GetComponentInParent<Membrane>()!=null)continue;
                    if(floors[k].point.y>point.y)return false;
                }
            }
            return true;
        }
        /// <summary>Whether a hero on foot stands between the camera and the sign's letters, by the box his body takes on the screen. Only in front of the word: one standing on its
        /// line, even at its end, is beside it, and a word gone to a quarter with the hero merely beside it was the older complaint. A body behind a floor sign stands higher on the
        /// screen than it does, so that doesn't count either.</summary>
        bool Covered(Camera view,PartyController party)
        {
            if(view==null||party.members==null||!Box(view,out var box,out float front))return false;
            foreach(var member in party.members)
            {
                if(member==null||!member.gameObject.activeInHierarchy||member.flight)continue;
                var at=member.transform.position;var feet=view.WorldToScreenPoint(at);if(feet.z<=0||feet.z>front-InFront)continue;
                var head=view.WorldToScreenPoint(at+Vector3.up*BodyTall);float half=Mathf.Abs(view.WorldToScreenPoint(at+view.transform.right*BodyHalfWide).x-feet.x);
                var body=Rect.MinMaxRect(feet.x-half,Mathf.Min(feet.y,head.y),feet.x+half,Mathf.Max(feet.y,head.y));
                if(body.Overlaps(box))return true;
            }
            return false;
        }
        /// <summary>Its letters' rect on the screen, and how far from the camera the near edge of its letters lies.</summary>
        bool Box(Camera view,out Rect box,out float front)
        {
            box=default;front=0;
            if(!ReferenceEquals(measured,text.text)){measured=text.text;var size=text.GetPreferredValues();halfWide=size.x*.5f;halfHigh=size.y*.5f;}
            var scale=transform.lossyScale;
            Vector3 at=transform.position,across=transform.right*(halfWide*scale.x),up=transform.up*(halfHigh*scale.y);
            Vector3 one=view.WorldToScreenPoint(at-across-up),two=view.WorldToScreenPoint(at+across+up);
            if(one.z<=0||two.z<=0)return false;
            box=Rect.MinMaxRect(Mathf.Min(one.x,two.x),Mathf.Min(one.y,two.y),Mathf.Max(one.x,two.x),Mathf.Max(one.y,two.y));front=view.WorldToScreenPoint(at-up).z;
            return true;
        }
        /// <summary>Its letters' rect on the screen against the plates: two points a frame, for the few signs in a zone. A sign reads level to the camera, so two corners are its rect. A name's words change with its mark, so they're measured when they do.
        /// Where it's heading counts as well as where it is, by as far as it'll go while it fades, so at a run it has gone by the time it gets there.</summary>
        bool Cut(Camera view)
        {
            if(view==null||!Box(view,out var box,out _))return false;
            float width=view.pixelWidth,height=view.pixelHeight;var middle=box.center;
            var ahead=tracked?(middle-lastAt)/(Mathf.Max(Time.unscaledDeltaTime,.001f)*AsidePerSecond):Vector2.zero;lastAt=middle;tracked=true;
            if(PanelFade.Cuts(box,width,height))return true;
            if(ahead.sqrMagnitude<1)return false;
            float most=height*MostAhead;if(ahead.sqrMagnitude>most*most)ahead=ahead.normalized*most;
            box.position+=ahead;return PanelFade.Cuts(box,width,height);
        }
    }
}
