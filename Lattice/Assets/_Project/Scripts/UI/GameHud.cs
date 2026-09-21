using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    public sealed class GameHud:MonoBehaviour
    {
        Canvas canvas;
        TMP_Text status,partner,target,prompt,skills,zone,probe;
        Image hp,charge,thrust,reticle;
        readonly Image[] skillIcons=new Image[4];
        readonly TMP_Text[] skillLabels=new TMP_Text[4];
        void Start()
        {
            canvas=UiKit.CreateCanvas("HUD",5,transform);
            var frame=UiKit.DarkFrame(canvas.transform,"VitalsFrame");UiKit.Rect(frame.gameObject,Vector2.zero,Vector2.zero,new(230,947),new(430,215));frame.raycastTarget=false;
            status=Text("Status",new(230,999),new(360,40),24,TextAlignmentOptions.Left);
            hp=Bar("Integrity",new(230,962),new Color(.23f,.88f,.72f));
            charge=Bar("Charge",new(230,940),new Color(.18f,.66f,1));
            thrust=Bar("Thrust",new(230,918),new Color(1,.65f,.2f));
            partner=Text("Partner",new(230,895),new(360,32),21,TextAlignmentOptions.Left);
            target=Text("Target",new(960,972),new(700,80),25);
            zone=Text("Zone",new(1635,1000),new(500,60),26,TextAlignmentOptions.Right);
            skills=Text("Skills",new(625,50),new(1190,60),21);
            prompt=Text("Interaction",new(960,205),new(1200,60),28);
            reticle=UiKit.Panel(canvas.transform,"TargetReticle",Color.white);reticle.sprite=UiSkin.Kit("reticle");reticle.raycastTarget=false;reticle.preserveAspect=true;
            UiKit.Rect(reticle.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(90,90));
            for(int i=0;i<4;i++)
            {
                skillIcons[i]=UiKit.Panel(canvas.transform,"Skill"+i,Color.white);skillIcons[i].raycastTarget=false;
                UiKit.Rect(skillIcons[i].gameObject,Vector2.zero,Vector2.zero,new(1410+i*120,102),new(74,74));
                skillLabels[i]=Text("SkillLabel"+i,new(1410+i*120,45),new(110,45),17);
            }
#if LATTICE_DEV || UNITY_EDITOR
            probe=Text("InputProbe",new(1630,280),new(500,180),16,TextAlignmentOptions.Right);
#endif
            Health.DamageNumber+=Damage;
        }
        TMP_Text Text(string name,Vector2 pos,Vector2 size,int font,TextAlignmentOptions align=TextAlignmentOptions.Center)
        {var t=UiKit.Text(canvas.transform,name,"",font,UiKit.TextColor,align);UiKit.Rect(t.gameObject,Vector2.zero,Vector2.zero,pos,size);return t;}
        Image Bar(string name,Vector2 pos,Color color)
        {
            var bg=UiKit.Panel(canvas.transform,name+"Back",new Color(.025f,.045f,.065f,.85f));UiKit.Rect(bg.gameObject,Vector2.zero,Vector2.zero,pos,new(360,14));
            var fill=UiKit.Panel(bg.transform,name,color);var rect=UiKit.Rect(fill.gameObject,new(0,.5f),new(0,.5f),Vector2.zero,new(360,14));rect.pivot=new(0,.5f);return fill;
        }
        void Update()
        {
            var party=PartyController.Current;if(party==null||party.members==null)return;
            canvas.enabled=!GameInput.Current.Blocked;
            var a=party.Active;
            hp.rectTransform.sizeDelta=new Vector2(360*a.Health.integrity/a.Health.maximum,14);
            charge.rectTransform.sizeDelta=new Vector2(360*a.charge/100,14);thrust.rectTransform.sizeDelta=new Vector2(360*a.thrust/100,14);
            var state=GameServices.Current.State;int level=state.party.Find(m=>m.id==a.character)?.level??1;
            status.text=$"{a.character.ToUpperInvariant()}   LV {level}   {a.Health.integrity:0} / {a.Health.maximum:0}";
            if(party.members.Length>1){var p=party.members[1-party.index];partner.text=$"Y  {p.character}  {p.Health.integrity:0}/{p.Health.maximum:0}"+(p.Health.Alive?"":"  ·  STAND NEAR TO REVIVE");}
            zone.text=ZoneController.Current.definition.id switch{"Hub_CinderHalo"=>"CINDER HALO","Hub_Decks"=>"THE DECKS","Sorrel_Ridges"=>"SORREL RIDGES","Gullet_Tunnel"=>"THE GULLET","TallowApproach"=>"TALLOW DRIFT · APPROACH","TallowDrift"=>"TALLOW DRIFT",var id=>id.Replace('_',' ').ToUpperInvariant()};
            if(a.target!=null&&a.target.Alive)target.text=$"{a.target.id.ToUpperInvariant()}   {a.target.integrity:0}/{a.target.maximum:0}\n"+(a.target.Broken?"BROKEN":"BREAK "+a.target.BreakMeter.ToString("0")+" / "+a.target.breakThreshold.ToString("0"));else target.text="";
            reticle.enabled=a.target!=null&&a.target.Alive;
            if(reticle.enabled)
            {
                var screen=Camera.main.WorldToScreenPoint(a.target.transform.position+Vector3.up);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,screen,null,out var point);reticle.rectTransform.anchoredPosition=point;
            }
            var interaction=PromptService.Interaction;prompt.text=interaction!=null&&!GameInput.Current.Blocked?PromptService.Tag("Interact")+"  "+interaction.prompt:"";
            string[] names=a.character=="Taren"?new[]{"Cleave","Ember","Pulse","Overdrive"}:new[]{"Lance","Scatter","Static net","Refract"};
            string[] faces={"A","B","X","Y"};
            bool combat=ZoneController.Current.Combat;
            for(int i=0;i<4;i++){skillIcons[i].enabled=combat;skillLabels[i].enabled=combat;skillIcons[i].sprite=UiSkin.Skill(a.character,i);skillIcons[i].color=a.cooldowns[i]>0?new Color(.35f,.4f,.5f):Color.white;skillLabels[i].text=(a.cooldowns[i]>0?a.cooldowns[i].ToString("0.0"):"RB + "+faces[i])+"\n"+names[i];}
            skills.text=!combat?"A  INTERACT     Y  SWAP     RT  "+(a.flight?"BOOST     LT  BRAKE":"SPRINT")+"     START  MENU":a.flight?"A  FIRE     X  LUNGE     B  ROLL     RT  BOOST     LT  BRAKE":"A  ATTACK     B  DODGE     X  GUARD     RT  SPRINT     LT  "+state.quickItem.ToUpperInvariant();
            if(probe!=null)probe.text=$"PAD: {PadBridge.Describe()}\nMOVE {GameServices.Current.Input.Move}\n{a.State}  {a.motor?.Velocity.magnitude:0.0} m/s";
        }
        void Damage(Health health,float amount,bool weak){if(canvas!=null)StartCoroutine(Number(health.transform.position+Vector3.up*1.9f,amount,weak));}
        IEnumerator Number(Vector3 position,float amount,bool weak)
        {
            var t=UiKit.Text(canvas.transform,"Damage",amount.ToString("0")+(weak?"!":""),weak?32:27,weak?new Color(1,.72f,.26f):Color.white);
            var rect=UiKit.Rect(t.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(160,60));
            for(float time=0;time<.75f;time+=Time.unscaledDeltaTime)
            {
                var screen=Camera.main.WorldToScreenPoint(position+Vector3.up*time);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,screen,null,out var point);rect.anchoredPosition=point;
                t.alpha=1-time/.75f;yield return null;
            }
            Destroy(t.gameObject);
        }
        void OnDestroy(){Health.DamageNumber-=Damage;}
    }
}
