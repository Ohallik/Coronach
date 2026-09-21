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
        }
        void Fade(float alpha){overlay.color=new Color(.01f,.02f,.04f,alpha);overlay.raycastTarget=alpha>.01f;}
        void OnDestroy(){if(SceneFlow.Current!=null)SceneFlow.Current.FadeChanged-=Fade;}
    }
}
