using Lattice.Combat;
using Lattice.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>The EXTRA gauge on the HUD (S3): the lead's move as an icon in the right of the vitals frame, filling round like a clock as the gauge fills, with the partner's
    /// small at the end of the partner's line. Full, it glows and the buttons stand beside it. Nothing is drawn until the story unlocks the gauge (ExtraGauge.Flag).
    /// It reads after the HUD and stands inside the vitals frame, which is already a plate the fight steps round; the cut-in is its own piece (<see cref="ExtraCutIn"/>).</summary>
    [DefaultExecutionOrder(906)]
    public sealed class ExtraHud:MonoBehaviour
    {
        static readonly Vector2 TopLeft=new(0,1);
        /// <summary>By the 1080p layout GameHud places the vitals by: the frame's clear inside runs to x 522 and the three bars end at 462, so the icon has the 60 between
        /// them, at the Thrust rows' end: the ends of the Integrity and Charge rows are the draught's and the meal's (BuffMarks). The buttons stand left of it on the
        /// Thrust label's row, whose words end near x 300. The partner's is at the right end of the partner's line, which is words to about x 490 only when the partner
        /// is down, and it isn't drawn then.</summary>
        static readonly Vector2 IconAt=new(492,880),PromptAt=new(385,890),PartnerAt=new(506,833);
        const float IconSize=50,PartnerSize=26;
        static readonly Color Unfilled=new(.3f,.34f,.4f),Filling=new(.86f,.9f,.96f),Refused=new(1,.3f,.25f),Ready=new(1,.92f,.62f);
        Canvas canvas;Image back,fill,partnerBack,partnerFill;TMP_Text prompt;Outline glow;float refusedHeard;
        /// <summary>How full the lead's gauge is drawn, 0 to 1, or -1 while there's no gauge to draw.</summary>
        public float Shown=>fill!=null&&fill.enabled?fill.fillAmount:-1;
        public float PartnerShown=>partnerFill!=null&&partnerFill.enabled?partnerFill.fillAmount:-1;
        public string Prompt=>prompt!=null?prompt.text:"";
        void Start()
        {
            canvas=UiKit.CreateCanvas("ExtraHud",6,transform);
            back=Icon("ExtraGaugeBack",IconAt,IconSize,false);fill=Icon("ExtraGauge",IconAt,IconSize,true);
            glow=fill.gameObject.AddComponent<Outline>();glow.effectDistance=new Vector2(2.5f,2.5f);glow.enabled=false;
            partnerBack=Icon("PartnerExtraGaugeBack",PartnerAt,PartnerSize,false);partnerFill=Icon("PartnerExtraGauge",PartnerAt,PartnerSize,true);
            prompt=UiKit.Legible(UiKit.Text(canvas.transform,"ExtraPrompt","",24,Ready,TextAlignmentOptions.Right));prompt.textWrappingMode=TextWrappingModes.NoWrap;
            UiKit.Rect(prompt.gameObject,TopLeft,TopLeft,PromptAt-new Vector2(0,1080),new(150,32));
            gameObject.AddComponent<ExtraCutIn>();
        }
        Image Icon(string name,Vector2 at,float size,bool radial)
        {
            var image=UiKit.Panel(canvas.transform,name,Color.white);image.raycastTarget=false;image.enabled=false;
            UiKit.Rect(image.gameObject,TopLeft,TopLeft,at-new Vector2(0,1080),new(size,size));
            if(radial){image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Radial360;image.fillOrigin=(int)Image.Origin360.Top;image.fillClockwise=true;image.fillAmount=0;}
            return image;
        }
        void LateUpdate()
        {
            var party=PartyController.Current;
            if(canvas==null)return;
            if(party==null||party.members==null||party.members.Length==0||GameInput.Current==null)
            {Draw(null,back,fill,0);Draw(null,partnerBack,partnerFill,0);glow.enabled=false;if(prompt.text.Length>0)prompt.text="";return;}
            canvas.enabled=!GameInput.Current.Blocked;
            bool unlocked=ExtraGauge.Unlocked;var lead=party.Active;
            ExtraMove extra=null;var move=unlocked&&lead!=null&&lead.TryGetComponent(out extra)?extra.Definition:null;
            float gauge=move!=null?ExtraGauge.Of(lead.character)/ExtraGauge.Full:0;
            Draw(move,back,fill,gauge);
            bool full=move!=null&&gauge>=1,refused=move!=null&&extra.RefusedAt>0&&GameTime.Now-extra.RefusedAt<.25f;
            // A press with the gauge short answers as a refused skill does: the icon flashes and the press is heard.
            if(refused&&extra.RefusedAt>refusedHeard){refusedHeard=extra.RefusedAt;UiSounds.Error();}
            float beat=.5f+.5f*Mathf.Sin(Time.unscaledTime*6);
            fill.color=refused?Refused:full?Color.Lerp(Color.white,Ready,beat):Filling;
            if(move!=null)back.color=refused?Refused*.6f:Unfilled;
            glow.enabled=full&&!refused;if(glow.enabled)glow.effectColor=new Color(1,.8f,.35f,.35f+.45f*beat);
            // What to press, only once there's something to press it for, and only where it can be pressed.
            var zone=ZoneController.Current;
            string words=full&&zone!=null&&zone.Combat&&!extra.Performing?ExtraGauge.Binding(lead.flight):"";
            if(prompt.text!=words)prompt.text=words;
            // The partner's: small, and only while they're standing, when their line is short enough to leave it room.
            CombatActor other=party.members.Length>1?party.Partner:null;
            var theirs=unlocked&&other!=null&&other.Health.Alive&&!other.Recovering&&other.TryGetComponent<ExtraMove>(out var second)?second.Definition:null;
            Draw(theirs,partnerBack,partnerFill,theirs!=null?ExtraGauge.Of(other.character)/ExtraGauge.Full:0);
            if(theirs!=null){partnerBack.color=Unfilled;partnerFill.color=Filling;}
        }
        static void Draw(Lattice.Data.SkillDef move,Image under,Image over,float gauge)
        {
            bool on=move!=null;
            if(under.enabled!=on)under.enabled=on;if(over.enabled!=on)over.enabled=on;
            if(!on)return;
            var icon=UiSkin.Skill(move);if(under.sprite!=icon){under.sprite=icon;over.sprite=icon;}
            over.fillAmount=Mathf.Clamp01(gauge);
        }
    }
}
