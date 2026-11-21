using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>The screen's edge says what the held hero's body is going through: it goes red for a hit, by how much of them the hit took, pulses slowly while they fight on under
    /// three tenths of their integrity, and cools to cyan for the length of a Flash. The middle of the screen, where the fight is, stays clear. It lies under the HUD, so nothing
    /// written on the screen is ever tinted, and it is gone for a menu or a conversation.</summary>
    public sealed class ImpactHud:MonoBehaviour
    {
        static Sprite edgeSprite;
        Canvas canvas;Image hurt,calm;
        float pulse,low,cool,clock,nextLook;bool fighting;
        static readonly Color HurtTint=new(.95f,.12f,.08f),CalmTint=new(.25f,.85f,1f);
        /// <summary>How red the edge is now, and how cyan, 0 to 1, for a check to read.</summary>
        public float Hurt=>hurt!=null&&hurt.enabled?hurt.color.a:0;
        public float Calm=>calm!=null&&calm.enabled?calm.color.a:0;
        /// <summary>White, clear in an oval over the middle and fullest in the corners, to be tinted. Drawn here, not painted: it is a gradient.</summary>
        static Sprite Edge()
        {
            if(edgeSprite!=null)return edgeSprite;
            const int w=96,h=54;var texture=new Texture2D(w,h,TextureFormat.RGBA32,false){name="Impact edge",wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            var pixels=new Color32[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                float u=(x+.5f)/w*2-1,v=(y+.5f)/h*2-1,d=Mathf.Sqrt(u*u+v*v);
                pixels[y*w+x]=new Color32(255,255,255,(byte)Mathf.RoundToInt(255*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.66f,1.36f,d))));
            }
            texture.SetPixels32(pixels);texture.Apply(false,true);
            edgeSprite=Sprite.Create(texture,new Rect(0,0,w,h),new Vector2(.5f,.5f),100);edgeSprite.hideFlags=HideFlags.HideAndDontSave;
            return edgeSprite;
        }
        void Start()
        {
            canvas=UiKit.CreateCanvas("ImpactEdge",4,transform);
            hurt=Layer("Hurt");calm=Layer("Calm");
            Impact.LeadHurt+=OnHurt;
        }
        Image Layer(string name)
        {
            var image=UiKit.Panel(canvas.transform,name,Color.clear);image.sprite=Edge();image.raycastTarget=false;image.enabled=false;
            UiKit.Rect(image.gameObject,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).sizeDelta=Vector2.zero;return image;
        }
        void OnDestroy(){Impact.LeadHurt-=OnHurt;}
        void OnHurt(float share,bool down)
        {
            int level=share>=Impact.Tune.HurtHeavyFrom?2:share>=Impact.Tune.HurtMediumFrom?1:0;
            pulse=Mathf.Max(pulse,down?Impact.Tune.DownedVignette:Impact.Tune.Vignette[level]);
        }
        void Update()
        {
            if(hurt==null)return;
            float dt=GameTime.Paused?0:Time.unscaledDeltaTime;clock+=dt;
            bool hidden=!ImpactSettings.Enabled||GameInput.Current!=null&&GameInput.Current.Blocked;
            // The heaviest hit clears in the time the table gives; a lighter one sooner.
            pulse=Mathf.MoveTowards(pulse,0,dt*Impact.Tune.DownedVignette/Impact.Tune.VignetteSeconds);
            var party=PartyController.Current;var lead=party!=null&&party.members!=null&&party.members.Length>0?party.Active:null;
            // Only with something awake nearby: a hero walking home hurt isn't warned about a fight that is over.
            if(Time.unscaledTime>=nextLook)
            {
                nextLook=Time.unscaledTime+.25f;
                fighting=lead!=null&&lead.Health.Alive&&lead.Health.integrity<lead.Health.maximum*Impact.Tune.LowIntegrity&&ZoneController.Current!=null&&ZoneController.Current.Combat&&
                    (BossController.AnyFightUnderway||EnemyBrain.AwakeNear(lead.transform.position,EnemyBrain.InteractionCalm));
            }
            low=Mathf.MoveTowards(low,fighting?1:0,dt*3);
            cool=Mathf.MoveTowards(cool,GameTime.Flashing?1:0,dt*6);
            float scale=hidden?0:ImpactSettings.FlashScale;
            float red=Mathf.Max(pulse,low*Impact.Tune.LowPulse*(.6f+.4f*Mathf.Sin(clock*2*Mathf.PI/Impact.Tune.LowPulseEvery)))*scale,cyan=cool*Impact.Tune.FlashMoveTint*scale;
            Show(hurt,HurtTint,red);Show(calm,CalmTint,cyan);
        }
        static void Show(Image layer,Color tint,float alpha)
        {
            bool on=alpha>.004f;if(layer.enabled!=on)layer.enabled=on;
            if(on){tint.a=alpha;layer.color=tint;}
        }
    }
}
