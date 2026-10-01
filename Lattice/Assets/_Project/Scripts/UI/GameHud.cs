using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Profiling;
namespace Lattice.UI
{
    public sealed class GameHud:MonoBehaviour
    {
        Canvas canvas;
        TMP_Text status,partner,target,prompt,skills,zone,probe,hpLabel,chargeLabel,thrustLabel;
        Image hp,charge,thrust,reticle;
        readonly Image[] skillIcons=new Image[4];
        readonly TMP_Text[] skillLabels=new TMP_Text[4];
        static readonly ProfilerMarker UpdateMarker=new("Coronach.HUD.Refresh");
        static bool measureCosts,allocationCounterSupported;
        public static bool MeasureCosts
        {
            get=>measureCosts;
            set
            {
                if(value&&!measureCosts)
                {
                    long before=System.GC.GetAllocatedBytesForCurrentThread();
                    var probe=new byte[4096];
                    allocationCounterSupported=System.GC.GetAllocatedBytesForCurrentThread()-before>=4096;
                    System.GC.KeepAlive(probe);
                }
                measureCosts=value;
            }
        }
        public static long LastUpdateNanoseconds,LastAllocatedBytes=-1;
        void Start()
        {
            canvas=UiKit.CreateCanvas("HUD",5,transform);
            // Every line reads at 16 px or more on a 1280x720 screen (24 units at the
            // 1080p reference): each label has its own row above its bar.
            var frame=UiKit.DarkFrame(canvas.transform,"VitalsFrame");UiKit.Rect(frame.gameObject,Vector2.zero,Vector2.zero,new(277,918),new(524,298));frame.raycastTarget=false;
            status=Text("Status",new(297,1010),new(470,38),28,TextAlignmentOptions.Left);
            hpLabel=Text("IntegrityLabel",new(297,976),new(470,30),24,TextAlignmentOptions.Left);
            chargeLabel=Text("ChargeLabel",new(297,933),new(470,30),24,TextAlignmentOptions.Left);
            thrustLabel=Text("ThrustLabel",new(297,890),new(470,30),24,TextAlignmentOptions.Left);
            hp=Bar("Integrity",new(262,955),new Color(.23f,.88f,.72f));
            charge=Bar("Charge",new(262,912),new Color(.18f,.66f,1));
            thrust=Bar("Thrust",new(262,869),new Color(1,.65f,.2f));
            partner=Text("Partner",new(279,833),new(435,32),24,TextAlignmentOptions.Left);
            target=Text("Target",new(960,972),new(700,80),25);
            zone=Text("Zone",new(1635,1000),new(500,60),26,TextAlignmentOptions.Right);
            skills=Text("Skills",new(625,50),new(1190,60),24);
            prompt=Text("Interaction",new(960,205),new(1200,60),28);
            skills.outlineWidth=.2f;skills.outlineColor=new Color32(5,12,22,255);
            prompt.outlineWidth=.22f;prompt.outlineColor=new Color32(5,12,22,255);
            reticle=UiKit.Panel(canvas.transform,"TargetReticle",Color.white);reticle.sprite=UiSkin.Kit("reticle");reticle.raycastTarget=false;reticle.preserveAspect=true;
            UiKit.Rect(reticle.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(90,90));
            for(int i=0;i<4;i++)
            {
                skillIcons[i]=UiKit.Panel(canvas.transform,"Skill"+i,Color.white);skillIcons[i].raycastTarget=false;
                UiKit.Rect(skillIcons[i].gameObject,Vector2.zero,Vector2.zero,new(1340+i*150,112),new(74,74));
                skillLabels[i]=Text("SkillLabel"+i,new(1340+i*150,42),new(145,62),24);
            }
#if LATTICE_DEV || UNITY_EDITOR
            probe=Text("InputProbe",new(1630,280),new(500,180),16,TextAlignmentOptions.Right);
#endif
            Health.DamageNumber+=Damage;
        }
        const float BarWidth=400;
        TMP_Text Text(string name,Vector2 pos,Vector2 size,int font,TextAlignmentOptions align=TextAlignmentOptions.Center)
        {var t=UiKit.Text(canvas.transform,name,"",font,UiKit.TextColor,align);UiKit.Rect(t.gameObject,Vector2.zero,Vector2.zero,pos,size);return t;}
        Image Bar(string name,Vector2 pos,Color color)
        {
            var bg=UiKit.Panel(canvas.transform,name+"Back",new Color(.025f,.045f,.065f,.85f));UiKit.Rect(bg.gameObject,Vector2.zero,Vector2.zero,pos,new(BarWidth,14));
            var fill=UiKit.Panel(bg.transform,name,color);var rect=UiKit.Rect(fill.gameObject,new(0,.5f),new(0,.5f),Vector2.zero,new(BarWidth,14));rect.pivot=new(0,.5f);return fill;
        }
        void Update()
        {
            if(!MeasureCosts){Refresh();return;}
            long started=System.Diagnostics.Stopwatch.GetTimestamp();
            long allocated=System.GC.GetAllocatedBytesForCurrentThread();
            using(UpdateMarker.Auto())Refresh();
            LastAllocatedBytes=allocationCounterSupported?System.GC.GetAllocatedBytesForCurrentThread()-allocated:-1;
            LastUpdateNanoseconds=(long)((System.Diagnostics.Stopwatch.GetTimestamp()-started)*(1e9/System.Diagnostics.Stopwatch.Frequency));
        }
        void Refresh()
        {
            var party=PartyController.Current;if(party==null||party.members==null)return;
            canvas.enabled=!GameInput.Current.Blocked;
            var a=party.Active;
            hp.rectTransform.sizeDelta=new Vector2(BarWidth*a.Health.integrity/a.Health.maximum,14);
            charge.rectTransform.sizeDelta=new Vector2(BarWidth*a.charge/100,14);thrust.rectTransform.sizeDelta=new Vector2(BarWidth*a.thrust/100,14);
            var state=GameServices.Current.State;int level=state.party.Find(m=>m.id==a.character)?.level??1;
            status.text=$"{a.character.ToUpperInvariant()}   ·   SYNC {level}";
            hpLabel.text=$"INTEGRITY   {a.Health.integrity:0} / {a.Health.maximum:0}";
            chargeLabel.text=$"CHARGE   {a.charge:0} / 100";
            thrustLabel.text=$"THRUST   {a.thrust:0} / 100";
            // A downed partner's 0/max says nothing the state doesn't; the line stays on one row.
            if(party.members.Length>1){var p=party.members[1-party.index];partner.text=$"{PromptService.Tag("Swap")}  {p.character}"+(p.Health.Alive?$"  {p.Health.integrity:0}/{p.Health.maximum:0}":"")+(p.Recovering?"  ·  RECOVERING":p.Health.Alive?"":party.ReviveTarget==p?"  ·  RESTORING "+(party.ReviveProgress*100).ToString("0")+"%":"  ·  APPROACH TO REVIVE");}
            zone.text=ZoneController.Current.definition.id switch{"Hub_CinderHalo"=>"CINDER HALO","Hub_Decks"=>"THE DECKS","Sorrel_Ridges"=>"SORREL RIDGES","Gullet_Tunnel"=>"THE GULLET","TallowApproach"=>"TALLOW DRIFT · APPROACH","TallowDrift"=>"TALLOW DRIFT",var id=>id.Replace('_',' ').ToUpperInvariant()};
            if(a.target!=null&&a.target.Alive)
                target.text=(a.TargetLocked?"LOCKED  ·  ":"")+$"{Readable(a.target.id).ToUpperInvariant()}   {a.target.integrity:0}/{a.target.maximum:0}\n"+
                    (a.target.Broken?"BROKEN":"BREAK "+a.target.BreakMeter.ToString("0")+" / "+a.target.breakThreshold.ToString("0"))+
                    "    "+PromptService.Tag("LockOn")+(a.TargetLocked?"  UNLOCK":"  LOCK");
            else target.text="";
            reticle.enabled=a.target!=null&&a.target.Alive;
            if(reticle.enabled)
            {
                reticle.color=a.TargetLocked?new Color(1,.76f,.3f):new Color(1,1,1,.55f);
                var screen=Camera.main.WorldToScreenPoint(a.target.transform.position+Vector3.up);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,screen,null,out var point);reticle.rectTransform.anchoredPosition=point;
            }
            var interaction=PromptService.Interaction;prompt.text=interaction!=null&&!GameInput.Current.Blocked?PromptService.Tag("Interact")+"  "+interaction.prompt:"";
            string[] names=a.character=="Taren"?new[]{"Cleave","Ember","Pulse","Overdrive"}:new[]{"Lance","Scatter","Static net","Refract"};
            string[] faces={"A","B","X","Y"};
            bool combat=ZoneController.Current.Combat;
            var character=GameCatalog.Find<Lattice.Data.CharacterDef>(a.character);
            for(int i=0;i<4;i++)
            {
                float cost=character!=null&&character.skills.Length>i?character.skills[i].chargeCost:20;
                skillIcons[i].enabled=combat;skillLabels[i].enabled=combat;skillIcons[i].sprite=UiSkin.Skill(a.character,i);
                skillIcons[i].color=a.cooldowns[i]>0||a.charge<cost?new Color(.4f,.46f,.53f):Color.white;
                string binding=PromptService.Device==PromptDevice.Gamepad?"RB+"+faces[i]:PromptService.Tag("Skill"+(i+1));
                skillLabels[i].text=(a.cooldowns[i]>0?a.cooldowns[i].ToString("0.0")+"s":binding+" · "+cost.ToString("0")+"C")+"\n"+names[i];
            }
            string Hint(string action,string label)=>PromptService.Tag(action)+"  "+label;
            skills.text=!combat?Hint("Interact","INTERACT")+"    "+Hint("Swap","SWAP")+"    "+Hint(a.flight?"Boost":"Sprint",a.flight?"BOOST":"SPRINT")+(a.flight?"    "+Hint("Brake","BRAKE"):"")+"    "+Hint("Pause","MENU"):
                a.flight?Hint("Fire","FIRE")+"    "+Hint("Lunge","LUNGE")+"    "+Hint("Roll","ROLL")+"    "+Hint("Boost","BOOST")+"    "+Hint("Brake","BRAKE"):
                Hint("Attack","ATTACK")+"    "+Hint("Dodge","DODGE")+"    "+Hint("Guard","GUARD")+"    "+Hint("Sprint","SPRINT")+"    "+Hint("QuickItem",Readable(state.quickItem).ToUpperInvariant());
            if(probe!=null)probe.text=$"PAD: {PadBridge.Describe()}\nMOVE {GameServices.Current.Input.Move}\n{a.State}  {a.motor?.Velocity.magnitude:0.0} m/s";
        }
        static string Readable(string value)=>System.Text.RegularExpressions.Regex.Replace(value??"","(?<=[a-z])(?=[A-Z])"," ");
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
