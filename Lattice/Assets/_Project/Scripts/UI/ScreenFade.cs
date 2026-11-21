using Lattice.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    public sealed class ScreenFade:MonoBehaviour
    {
        Image overlay;
        void Start()
        {
            var canvas=UiKit.CreateCanvas("Fade",100,transform);overlay=UiKit.Panel(canvas.transform,"Fade",Color.clear);
            UiKit.Rect(overlay.gameObject,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).sizeDelta=Vector2.zero;
            overlay.raycastTarget=false;SceneFlow.Current.FadeChanged+=Fade;
            // A cut in time (Lattice.World.TimePlate, chapter 10's SEVEN WEEKS ON): words on black over everything, held through the load it covers.
            plate=UiKit.Panel(canvas.transform,"TimePlate",Color.clear);plate.raycastTarget=false;UiKit.Rect(plate.gameObject,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).sizeDelta=Vector2.zero;
            plateWords=UiKit.Legible(UiKit.Heading(plate.transform,"TimePlateWords","",58,new Color(1,.93f,.8f)));UiKit.Rect(plateWords.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(1400,90));
            plate.gameObject.SetActive(false);
        }
        Image plate;TMPro.TMP_Text plateWords;
        void Update()
        {
            if(plate==null)return;float a=Lattice.World.TimePlate.Alpha(SceneFlow.Current!=null&&SceneFlow.Current.Loading);
            if(plate.gameObject.activeSelf!=a>0)plate.gameObject.SetActive(a>0);if(a<=0)return;
            plate.color=new Color(.01f,.02f,.04f,Mathf.Clamp01(a*1.6f));plateWords.text=Lattice.World.TimePlate.Words;plateWords.alpha=Mathf.Clamp01(a*1.25f-.15f);
        }
        void Fade(float alpha){overlay.color=new Color(.01f,.02f,.04f,alpha);overlay.raycastTarget=alpha>.01f;}
        void OnDestroy(){if(SceneFlow.Current!=null)SceneFlow.Current.FadeChanged-=Fade;}
    }
}
