using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>An EXTRA move's cut-in (S3): a dark band slides across the screen with the hero's Shaped portrait, cut to the face, and the move's name, holds for the half
    /// second the move holds the fight (ExtraMove.CutIn), and is gone. Nothing about the camera changes (D212). It takes no input and blocks none.</summary>
    public sealed class ExtraCutIn:MonoBehaviour
    {
        /// <summary>How long the band takes to arrive, and to leave once the move begins: the fight is behind it for the cut-in and this much more.</summary>
        public const float In=.12f,Out=.14f;
        /// <summary>The band's height and how far over the screen's middle it stands, so the hero's feet and the hint bar stay in sight under it; the face's size; where the
        /// face and the words stand from the middle.</summary>
        const float BandHeight=236,BandUp=70,Face=560,FaceAt=-470,WordsAt=330;
        static readonly Color Plate=new(.015f,.03f,.06f,.92f),Cream=new(1,.93f,.8f);
        Canvas canvas;RectTransform band,seat,rule;Image wash,top,foot,picture,underline;TMP_Text title,kicker;
        float age=99;
        /// <summary>Whether any of it is on the screen.</summary>
        public bool Showing=>band!=null&&band.gameObject.activeSelf;
        /// <summary>Some cut-in is on the screen: the quest cards wait for it (D389), as the band is drawn over them and cut the last line of a MASTERY card.</summary>
        public static bool Up{get;private set;}
        public string Title=>title!=null?title.text:"";
        public Sprite Portrait=>picture!=null?picture.sprite:null;
        void Start()
        {
            canvas=UiKit.CreateCanvas("ExtraCutIn",9,transform);
            band=(RectTransform)new GameObject("Band",typeof(RectTransform)).transform;band.SetParent(canvas.transform,false);
            band.anchorMin=new(0,.5f);band.anchorMax=new(1,.5f);band.pivot=new(.5f,.5f);band.sizeDelta=new(0,BandHeight);band.anchoredPosition=new(0,BandUp);
            var plate=Fill("Plate",Plate);wash=Fill("Wash",Color.clear);
            top=Edge("TopRule",1);foot=Edge("FootRule",0);
            // The face rises out of the band and is cut by it: forehead to chin of a 576 cell at this size.
            seat=(RectTransform)new GameObject("FaceSeat",typeof(RectTransform),typeof(RectMask2D)).transform;seat.SetParent(band,false);
            UiKit.Rect(seat.gameObject,new(.5f,.5f),new(.5f,.5f),new(FaceAt,0),new(Face,BandHeight));
            picture=UiKit.Panel(seat,"Face",Color.white);picture.raycastTarget=false;picture.preserveAspect=true;UiKit.Rect(picture.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(Face,Face));
            kicker=UiKit.Legible(UiKit.Text(band,"Kicker","EXTRA",30,Cream,TextAlignmentOptions.Left));kicker.characterSpacing=14;kicker.fontStyle=FontStyles.Bold;
            UiKit.Rect(kicker.gameObject,new(.5f,.5f),new(.5f,.5f),new(WordsAt,62),new(900,40));
            title=UiKit.Legible(UiKit.Heading(band,"MoveName","",76,Cream,TextAlignmentOptions.Left));title.textWrappingMode=TextWrappingModes.NoWrap;
            UiKit.Rect(title.gameObject,new(.5f,.5f),new(.5f,.5f),new(WordsAt,-6),new(900,96));
            underline=UiKit.Panel(band,"NameRule",UiKit.GoldColor);underline.raycastTarget=false;
            rule=UiKit.Rect(underline.gameObject,new(.5f,.5f),new(.5f,.5f),new(WordsAt-450,-62),new(0,3));rule.pivot=new(0,.5f);
            band.gameObject.SetActive(false);
            ExtraMove.Began+=Play;
            Image Fill(string name,Color color){var image=UiKit.Panel(band,name,color);image.raycastTarget=false;UiKit.Rect(image.gameObject,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);image.rectTransform.sizeDelta=Vector2.zero;return image;}
            Image Edge(string name,float at){var image=UiKit.Panel(band,name,Color.white);image.raycastTarget=false;var r=image.rectTransform;r.anchorMin=new(0,at);r.anchorMax=new(1,at);r.pivot=new(.5f,at);r.sizeDelta=new(0,4);r.anchoredPosition=Vector2.zero;return image;}
        }
        void OnDestroy(){ExtraMove.Began-=Play;Up=false;}
        void Play(CombatActor hero,SkillDef move)
        {
            if(band==null||hero==null||move==null)return;
            var who=GameCatalog.Find<CharacterDef>(hero.character);var hue=who!=null?who.syncHue:Color.cyan;hue.a=1;
            picture.sprite=PortraitLookup.Get(hero.character,BodyForm.Shaped,PortraitEmotion.Determined);picture.enabled=picture.sprite!=null;
            // The move's own name, as its definition has it: never an id (D200).
            title.text=move.displayName.ToUpperInvariant();
            wash.color=new Color(hue.r,hue.g,hue.b,.16f);top.color=foot.color=hue;kicker.color=Color.Lerp(hue,Color.white,.25f);
            age=0;band.gameObject.SetActive(true);Up=true;Step();
        }
        void Update()
        {
            if(!Showing)return;
            // The clock the move's own cut-in runs on: a pause holds both, and so does a hit-stop.
            age+=GameTime.Delta;
            bool hidden=GameInput.Current!=null&&GameInput.Current.Blocked;if(canvas.enabled==hidden)canvas.enabled=!hidden;
            if(age>=ExtraMove.CutIn+Out){band.gameObject.SetActive(false);Up=false;return;}
            Step();
        }
        void Step()
        {
            float far=((RectTransform)canvas.transform).rect.width+200;
            float arrive=Mathf.Clamp01(age/In);arrive=1-(1-arrive)*(1-arrive)*(1-arrive);
            float leave=Mathf.Clamp01((age-ExtraMove.CutIn)/Out);leave*=leave;
            band.anchoredPosition=new Vector2(Mathf.Lerp(-far,0,arrive)+far*leave,BandUp);
            // The face comes to rest a beat after the band, and the name's letters close up as the boss card's do.
            float settle=Mathf.Clamp01(age/(In*2.2f));settle=1-(1-settle)*(1-settle);
            seat.anchoredPosition=new Vector2(FaceAt-90*(1-settle),0);
            title.characterSpacing=Mathf.Lerp(26,8,settle);rule.sizeDelta=new Vector2(Mathf.Lerp(0,520,settle),3);
        }
    }
}
