using Lattice.Combat;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>The far end of a big arena can leave a boss off screen even with the camera drawn back as far as it goes: a small arrow on the screen's edge points to it.</summary>
    public sealed class BossEdgeMark:MonoBehaviour
    {
        const float Inset=.05f,Margin=.02f;
        Image mark;RectTransform place;
        public void Build(Transform parent)
        {
            mark=UiKit.Panel(parent,"BossEdgeMark",new Color(1,.46f,.2f,.95f));mark.sprite=UiSkin.Pointer();mark.raycastTarget=false;mark.preserveAspect=true;
            place=UiKit.Rect(mark.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(56,56));mark.enabled=false;
        }
        void Update()
        {
            if(mark==null)return;
            var party=PartyController.Current;var camera=Camera.main;
            var boss=party!=null&&party.Active!=null&&!party.Active.flight?BossController.Focus(party.Active.transform.position):null;
            bool show=false;
            if(boss!=null&&camera!=null)
            {
                var view=camera.WorldToViewportPoint(boss.transform.position+Vector3.up);
                if(view.z<=0){view.x=1-view.x;view.y=1-view.y;}
                show=view.z<=0||view.x<Margin||view.x>1-Margin||view.y<Margin||view.y>1-Margin;
                if(show)
                {
                    var from=new Vector2(view.x-.5f,view.y-.5f);
                    float reach=Mathf.Min((.5f-Inset)/Mathf.Max(.0001f,Mathf.Abs(from.x)),(.5f-Inset)/Mathf.Max(.0001f,Mathf.Abs(from.y)));
                    var at=new Vector2(.5f,.5f)+from*reach;
                    place.anchorMin=place.anchorMax=at;place.anchoredPosition=Vector2.zero;
                    place.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(from.y*9,from.x*16)*Mathf.Rad2Deg-90);
                }
            }
            if(mark.enabled!=show)mark.enabled=show;
        }
    }
}
