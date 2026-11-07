using Lattice.Combat;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>The far end of a big arena can leave a boss off screen even with the camera drawn back as far as it goes: a small arrow on the screen's edge points to it.</summary>
    public sealed class BossEdgeMark:MonoBehaviour
    {
        const float Inset=.05f,Margin=.02f;
        static Sprite arrow;
        Image mark;RectTransform place;
        public void Build(Transform parent)
        {
            mark=UiKit.Panel(parent,"BossEdgeMark",new Color(1,.46f,.2f,.95f));mark.sprite=Arrow();mark.raycastTarget=false;mark.preserveAspect=true;
            place=UiKit.Rect(mark.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(46,46));mark.enabled=false;
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
        static Sprite Arrow()
        {
            if(arrow!=null)return arrow;
            const int size=48;var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=(x+.5f)/size-.5f,v=(y+.5f)/size;
                float half=Mathf.Lerp(.46f,0,Mathf.InverseLerp(.1f,.96f,v));
                pixels[y*size+x]=v>=.1f&&v<=.96f&&Mathf.Abs(u)<=half?new Color32(255,255,255,255):new Color32(255,255,255,0);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="boss edge arrow"};texture.SetPixels32(pixels);texture.Apply();
            return arrow=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100);
        }
    }
}
