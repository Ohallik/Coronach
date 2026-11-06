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
        static readonly Vector2 TopLeft=new(0,1),TopCentre=new(.5f,1),TopRight=new(1,1),BottomLeft=new(0,0),BottomCentre=new(.5f,0),BottomRight=new(1,0),Middle=new(.5f,.5f);
        /// <summary>Places by the 1080p layout's coordinates, held from the nearest corner or edge: the same pixels at 16:9, and still in
        /// its corner on any other screen. Everything used to hang from the bottom-left and ran off the top of a 21:9 screen.</summary>
        static RectTransform Corner(GameObject go,Vector2 anchor,Vector2 pos,Vector2 size)=>UiKit.Rect(go,anchor,anchor,pos-Vector2.Scale(anchor,new(1920,1080)),size);
        RectTransform vitals;ObjectiveHud objective;
        TMP_Text status,partner,target,prompt,skills,zone,probe,hpLabel,chargeLabel,thrustLabel;
        Image hp,charge,thrust,reticle;
        readonly Image[] skillIcons=new Image[4];
        // The boss bar (D198): name, integrity with a lagging chip, phase notches, break gauge and the fight's state.
        GameObject bossRoot;TMP_Text bossName,bossEpithet,bossState;Image bossBack,bossFill,bossChip,bossBreak;
        readonly System.Collections.Generic.List<Image> bossNotches=new();float bossChipFraction=1;BossController shownBoss,introduced;
        TMP_Text bossCard,bossCardEpithet;float bossCardAge=99;
        const float BossBarWidth=900;
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
            var frame=UiKit.DarkFrame(canvas.transform,"VitalsFrame");frame.pixelsPerUnitMultiplier=2;vitals=Corner(frame.gameObject,TopLeft,new(277,918),new(524,298));frame.raycastTarget=false;objective=GetComponent<ObjectiveHud>();
            status=Text("Status",TopLeft,new(297,1010),new(470,38),28,TextAlignmentOptions.Left);
            hpLabel=Text("IntegrityLabel",TopLeft,new(297,976),new(470,30),24,TextAlignmentOptions.Left);
            chargeLabel=Text("ChargeLabel",TopLeft,new(297,933),new(470,30),24,TextAlignmentOptions.Left);
            thrustLabel=Text("ThrustLabel",TopLeft,new(297,890),new(470,30),24,TextAlignmentOptions.Left);
            hp=Bar("Integrity",new(262,955),new Color(.23f,.88f,.72f));
            charge=Bar("Charge",new(262,912),new Color(.18f,.66f,1));
            thrust=Bar("Thrust",new(262,869),new Color(1,.65f,.2f));
            partner=Text("Partner",TopLeft,new(279,833),new(435,32),24,TextAlignmentOptions.Left);
            target=Text("Target",TopCentre,new(960,972),new(700,80),25);
            // Centred between the objective frame's top edge (987.5) and the top of the screen: at 1000 its words sat on that edge.
            zone=Text("Zone",TopRight,new(1635,1034),new(500,60),26,TextAlignmentOptions.Right);
            skills=Text("Skills",BottomLeft,new(625,50),new(1190,60),24);
            prompt=Text("Interaction",BottomCentre,new(960,205),new(1200,60),28);
            skills.outlineWidth=.2f;skills.outlineColor=new Color32(5,12,22,255);
            prompt.outlineWidth=.22f;prompt.outlineColor=new Color32(5,12,22,255);
            target.outlineWidth=.2f;target.outlineColor=new Color32(5,12,22,255);
            zone.outlineWidth=.2f;zone.outlineColor=new Color32(5,12,22,255);
            TextPlate.Attach(prompt,"InteractionPlate");TextPlate.Attach(skills,"SkillsPlate",34,10);TextPlate.Attach(target,"TargetPlate",34,10);
            reticle=UiKit.Panel(canvas.transform,"TargetReticle",Color.white);reticle.sprite=UiSkin.Kit("reticle");reticle.raycastTarget=false;reticle.preserveAspect=true;
            UiKit.Rect(reticle.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(90,90));
            for(int i=0;i<4;i++)
            {
                skillIcons[i]=UiKit.Panel(canvas.transform,"Skill"+i,Color.white);skillIcons[i].raycastTarget=false;
                Corner(skillIcons[i].gameObject,BottomRight,new(1340+i*150,112),new(74,74));
                skillLabels[i]=Text("SkillLabel"+i,BottomRight,new(1340+i*150,42),new(145,62),24);
            }
#if LATTICE_DEV || UNITY_EDITOR
            probe=Text("InputProbe",BottomRight,new(1630,280),new(500,180),16,TextAlignmentOptions.Right);
#endif
            BuildBossBar();
            Health.DamageNumber+=Damage;PlayerBrain.QuickItemRefused+=OnItemRefused;
        }
        void BuildBossBar()
        {
            bossRoot=new GameObject("BossBar",typeof(RectTransform));bossRoot.transform.SetParent(canvas.transform,false);
            var root=(RectTransform)bossRoot.transform;root.anchorMin=root.anchorMax=BottomCentre;root.pivot=Vector2.zero;root.anchoredPosition=Vector2.zero;root.sizeDelta=Vector2.zero;
            bossName=BossText("BossName",new(0,216),28,TextAlignmentOptions.Left);
            bossEpithet=BossText("BossEpithet",new(0,216),24,TextAlignmentOptions.Right);
            bossBack=UiKit.Panel(bossRoot.transform,"BossIntegrityBack",new Color(.025f,.045f,.065f,.9f));bossBack.raycastTarget=false;
            UiKit.Rect(bossBack.gameObject,Vector2.zero,Vector2.zero,new(0,186),new(BossBarWidth,20));
            bossChip=BossFill(bossBack.transform,"BossIntegrityChip",new Color(1,.9f,.72f,.8f),20);
            bossFill=BossFill(bossBack.transform,"BossIntegrity",new Color(.86f,.25f,.2f),20);
            var breakBack=UiKit.Panel(bossRoot.transform,"BossBreakBack",new Color(.025f,.045f,.065f,.9f));breakBack.raycastTarget=false;
            UiKit.Rect(breakBack.gameObject,Vector2.zero,Vector2.zero,new(0,168),new(BossBarWidth,7));
            bossBreak=BossFill(breakBack.transform,"BossBreak",new Color(1,.62f,.2f,.85f),7);
            bossState=BossText("BossState",new(0,140),24,TextAlignmentOptions.Center);
            for(int i=0;i<3;i++)
            {
                var notch=UiKit.Panel(bossBack.transform,"BossNotch"+i,new Color(.95f,.95f,1,.95f));notch.raycastTarget=false;
                var rect=UiKit.Rect(notch.gameObject,new(0,.5f),new(0,.5f),Vector2.zero,new(3,28));rect.pivot=new(.5f,.5f);bossNotches.Add(notch);
            }
            bossRoot.SetActive(false);
            // The title card: the boss's name across the middle of the screen as its fight begins.
            bossCard=UiKit.Text(canvas.transform,"BossTitleCard","",64,new Color(1,.93f,.8f),TextAlignmentOptions.Center);
            Corner(bossCard.gameObject,Middle,new(960,640),new(1400,90));bossCard.outlineWidth=.25f;bossCard.outlineColor=new Color32(5,12,22,255);bossCard.raycastTarget=false;
            bossCardEpithet=UiKit.Text(canvas.transform,"BossTitleEpithet","",28,UiKit.TextColor,TextAlignmentOptions.Center);
            Corner(bossCardEpithet.gameObject,Middle,new(960,580),new(1400,44));bossCardEpithet.outlineWidth=.2f;bossCardEpithet.outlineColor=new Color32(5,12,22,255);bossCardEpithet.raycastTarget=false;
            bossCard.alpha=bossCardEpithet.alpha=0;
        }
        TMP_Text BossText(string name,Vector2 pos,int font,TextAlignmentOptions align)
        {
            var t=UiKit.Text(bossRoot.transform,name,"",font,UiKit.TextColor,align);UiKit.Rect(t.gameObject,Vector2.zero,Vector2.zero,pos,new(BossBarWidth,36));
            t.outlineWidth=.2f;t.outlineColor=new Color32(5,12,22,255);t.raycastTarget=false;return t;
        }
        Image BossFill(Transform parent,string name,Color color,float height)
        {
            var fill=UiKit.Panel(parent,name,color);fill.raycastTarget=false;
            var rect=UiKit.Rect(fill.gameObject,new(0,.5f),new(0,.5f),Vector2.zero,new(BossBarWidth,height));rect.pivot=new(0,.5f);return fill;
        }
        /// <summary>Shows the boss bar for the fighting boss nearest the active hero; returns whether it is up.</summary>
        bool RefreshBossBar(CombatActor active)
        {
            var boss=BossController.Focus(active.transform.position);bool show=boss!=null;
            if(bossRoot.activeSelf!=show)bossRoot.SetActive(show);
            if(show&&boss!=introduced){introduced=boss;bossCardAge=0;bossCard.text=boss.DisplayName;bossCardEpithet.text=boss.Epithet;}
            bossCardAge+=Time.unscaledDeltaTime;
            float card=bossCardAge<.35f?bossCardAge/.35f:bossCardAge<2.2f?1:Mathf.Clamp01(1-(bossCardAge-2.2f)/.6f);
            bossCard.alpha=bossCardEpithet.alpha=card;
            if(!show){shownBoss=null;return false;}
            var health=boss.Health;float fraction=Mathf.Clamp01(health.integrity/Mathf.Max(1,health.maximum));
            if(boss!=shownBoss){shownBoss=boss;bossChipFraction=fraction;}
            bossChipFraction=fraction>=bossChipFraction?fraction:Mathf.MoveTowards(bossChipFraction,fraction,Time.unscaledDeltaTime*.22f);
            bossFill.rectTransform.sizeDelta=new Vector2(BossBarWidth*fraction,20);bossChip.rectTransform.sizeDelta=new Vector2(BossBarWidth*bossChipFraction,20);
            bossFill.color=boss.Shifting?new Color(.95f,.85f,1f):new Color(.86f,.25f,.2f);
            float meter=health.Broken?1:Mathf.Clamp01(health.BreakMeter/Mathf.Max(1,health.breakThreshold));
            bossBreak.rectTransform.sizeDelta=new Vector2(BossBarWidth*meter,7);bossBreak.color=health.Broken?new Color(1,.92f,.45f):new Color(1,.62f,.2f,.85f);
            if(bossName.text!=boss.DisplayName)bossName.text=boss.DisplayName;
            if(bossEpithet.text!=boss.Epithet)bossEpithet.text=boss.Epithet;
            var thresholds=boss.Thresholds;
            for(int i=0;i<bossNotches.Count;i++)
            {
                bool on=i<thresholds.Length;if(bossNotches[i].gameObject.activeSelf!=on)bossNotches[i].gameObject.SetActive(on);
                if(on)bossNotches[i].rectTransform.anchoredPosition=new Vector2(BossBarWidth*thresholds[i],0);
            }
            string state=boss.Shifting?boss.ShiftText:health.Broken?"OPENING  ·  STRIKE NOW":!health.Damageable?boss.GuardedText:boss.StatusText;
            if(bossState.text!=state)bossState.text=state;
            return true;
        }
        const float BarWidth=400;
        float refusedHeard,itemRefusedAt=-99;
        /// <summary>A break threshold this high means the target can't be broken at all, so it gets no gauge (pressure organs).</summary>
        const float Unbreakable=10000;
        TMP_Text Text(string name,Vector2 anchor,Vector2 pos,Vector2 size,int font,TextAlignmentOptions align=TextAlignmentOptions.Center)
        {var t=UiKit.Text(canvas.transform,name,"",font,UiKit.TextColor,align);Corner(t.gameObject,anchor,pos,size);return t;}
        Image Bar(string name,Vector2 pos,Color color)
        {
            var bg=UiKit.Panel(canvas.transform,name+"Back",new Color(.025f,.045f,.065f,.85f));Corner(bg.gameObject,TopLeft,pos,new(BarWidth,14));
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
            status.text=$"{DisplayNames.Speaker(a.character).ToUpperInvariant()}   ·   SYNC {level}";
            hpLabel.text=$"INTEGRITY   {a.Health.integrity:0} / {a.Health.maximum:0}";
            chargeLabel.text=$"CHARGE   {a.charge:0} / 100";
            thrustLabel.text=$"THRUST   {a.thrust:0} / 100";
            // A roll refused for Thrust answers as a refused skill's icon does: a mash just stops, and the bar says why.
            bool rollRefused=a.RollRefusedAt>0&&GameTime.Now-a.RollRefusedAt<.25f;
            thrust.color=rollRefused?new Color(1,.3f,.25f):new Color(1,.65f,.2f);
            if(rollRefused&&a.RollRefusedAt>refusedHeard){refusedHeard=a.RollRefusedAt;UiSounds.Error();}
            // A downed partner's 0/max says nothing the state doesn't; the line stays on one row.
            if(party.members.Length>1){var p=party.members[1-party.index];partner.text=$"{PromptService.Tag("Swap")}  {DisplayNames.Speaker(p.character).ToUpperInvariant()}"+(p.Health.Alive?$"  {p.Health.integrity:0}/{p.Health.maximum:0}":"")+(p.Recovering?"  ·  RECOVERING":p.Health.Alive?"":party.ReviveTarget==p?"  ·  RESTORING "+(party.ReviveProgress*100).ToString("0")+"%":"  ·  APPROACH TO REVIVE");}
            zone.text=DisplayNames.Zone(ZoneController.Current.definition.id).ToUpperInvariant();
            bool bossUp=RefreshBossBar(a);
            if(a.target!=null&&a.target.Alive&&bossUp&&shownBoss!=null&&a.target==shownBoss.Health)
                target.text=(a.TargetLocked?"LOCKED    ":"")+TargetKeys(a);
            else if(a.target!=null&&a.target.Alive)
                target.text=(a.TargetLocked?"LOCKED  ·  ":"")+$"{(a.target.GetComponent<CantorCollar>()!=null?"Cantor collar lock":DisplayNames.Target(a.target.id)).ToUpperInvariant()}   {a.target.integrity:0}/{a.target.maximum:0}\n"+
                    (a.target.Broken?"BROKEN    ":a.target.breakThreshold>=Unbreakable?"":"BREAK "+a.target.BreakMeter.ToString("0")+" / "+a.target.breakThreshold.ToString("0")+"    ")+
                    TargetKeys(a);
            else target.text="";
            reticle.enabled=a.target!=null&&a.target.Alive;
            if(reticle.enabled)
            {
                reticle.color=a.TargetLocked?new Color(1,.76f,.3f):new Color(1,1,1,.55f);
                var screen=Camera.main.WorldToScreenPoint(a.target.transform.position+Vector3.up);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,screen,null,out var point);reticle.rectTransform.anchoredPosition=point;
            }
            var interaction=PromptService.Interaction;prompt.text=interaction!=null&&!GameInput.Current.Blocked&&!bossUp?PromptService.Tag("Interact")+"  "+interaction.prompt+PeopleMarks.PromptNote(interaction):"";
            bool combat=ZoneController.Current.Combat;
            for(int i=0;i<4;i++)
            {
                var skill=SkillBook.Equipped(a.character,i);
                float cost=SkillBook.Cost(a.character,skill);
                skillIcons[i].enabled=combat&&skill!=null;skillLabels[i].enabled=combat&&skill!=null;if(skill==null)continue;
                skillIcons[i].sprite=UiSkin.Skill(skill);
                // A refused press answers: the icon flashes, and the press itself gets a sound below.
                bool refused=a.SkillRefusedAt[i]>0&&GameTime.Now-a.SkillRefusedAt[i]<.25f;
                skillIcons[i].color=refused?new Color(1,.3f,.25f):a.cooldowns[i]>0||a.charge<cost?new Color(.4f,.46f,.53f):Color.white;
                if(refused&&a.SkillRefusedAt[i]>refusedHeard){refusedHeard=a.SkillRefusedAt[i];UiSounds.Error();}
                skillLabels[i].text=(a.cooldowns[i]>0?a.cooldowns[i].ToString("0.0")+"s":SkillBinding(i,a.flight)+" · "+cost.ToString("0")+"C")+"\n"+(string.IsNullOrEmpty(skill.shortName)?skill.displayName:skill.shortName);
            }
            string Hint(string action,string label)=>PromptService.Tag(action)+"  "+label;
            skills.text=!combat?Hint("Interact","INTERACT")+(party.members.Length>1?"    "+Hint("Swap","SWAP"):"")+"    "+Hint(a.flight?"Boost":"Sprint",a.flight?"BOOST":"SPRINT")+(a.flight?"    "+Hint("Brake","BRAKE"):"")+"    "+Hint("Log","QUESTS")+"    "+Hint("Pause","MENU"):
                a.flight?Hint("Fire","FIRE")+"    "+Hint("Lunge","LUNGE")+"    "+Hint("Roll","ROLL")+"    "+Hint("Boost","BOOST")+"    "+Hint("Brake","BRAKE"):
                Hint("Attack","ATTACK")+"    "+Hint("Dodge","DODGE")+"    "+Hint("Guard","GUARD")+"    "+Hint("Sprint","SPRINT")+"    "+QuickItem(state);
            if(probe!=null)probe.text=$"PAD: {PadBridge.Describe()}\nMOVE {GameServices.Current.Input.Move}\n{a.State}  {a.motor?.Velocity.magnitude:0.0} m/s";
        }
        static string TargetKeys(CombatActor a)=>PromptService.Tag("LockOn")+(a.TargetLocked?"  UNLOCK":"  LOCK")+"    "+DeviceHints.CycleTarget()+"  SWITCH";
        /// <summary>The quick item and how many are left. None left reads as out of reach; a refused press flashes, as a refused skill's icon does.</summary>
        string QuickItem(GameState state)
        {
            state.consumables.TryGetValue(state.quickItem??"",out int count);
            string hint=PromptService.Tag("QuickItem")+"  "+DisplayNames.Item(state.quickItem).ToUpperInvariant()+" ×"+count;
            return Time.unscaledTime-itemRefusedAt<.25f?"<color=#FF4D40>"+hint+"</color>":count<=0?"<color=#7D8894>"+hint+"</color>":hint;
        }
        void OnItemRefused(string why){itemRefusedAt=Time.unscaledTime;UiSounds.Error();}
        /// <summary>The skill's button on the active device: RB plus the face glyph on a pad, the number key on a keyboard.</summary>
        public static string SkillBinding(int slot,bool flight)
        {
            if(PromptService.Device!=PromptDevice.Gamepad)return PromptService.Tag("Skill"+(slot+1));
            string[] faces=flight?new[]{"Fire","Roll","Lunge","Swap"}:new[]{"Attack","Dodge","Guard","Swap"};
            return PromptService.Tag("SkillMod")+"+"+PromptService.Tag(faces[slot]);
        }
        static string Readable(string value)=>System.Text.RegularExpressions.Regex.Replace(value??"","(?<=[a-z])(?=[A-Z])"," ");
        void Damage(Health health,float amount,HitKind kind){if(canvas!=null)StartCoroutine(Number(health.transform.position+Vector3.up*1.9f,amount,kind));}
        IEnumerator Number(Vector3 position,float amount,HitKind kind)
        {
            var (mark,size,color)=kind switch{HitKind.Weak=>("!",32f,new Color(1,.72f,.26f)),HitKind.WeakSpot=>("!!",34f,new Color(1,.93f,.45f)),HitKind.Resisted=>("",24f,new Color(.62f,.66f,.7f)),_=>("",27f,Color.white)};
            var t=UiKit.Text(canvas.transform,"Damage",amount.ToString("0")+mark,size,color);
            var rect=UiKit.Rect(t.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(160,60));
            for(float time=0;time<.75f;time+=Time.unscaledDeltaTime)
            {
                var screen=Camera.main.WorldToScreenPoint(position+Vector3.up*time);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,screen,null,out var point);rect.anchoredPosition=ClearOfFrames(point,rect.sizeDelta);
                t.alpha=1-time/.75f;yield return null;
            }
            Destroy(t.gameObject);
        }
        static readonly Vector3[] corners=new Vector3[4];
        /// <summary>A number floats where its body stands, but not across what the player reads mid-fight: the vitals and the objective
        /// hold the top corners, so it leaves them downward or toward the middle of the screen, whichever is nearer.</summary>
        Vector2 ClearOfFrames(Vector2 point,Vector2 size)
        {
            var root=(RectTransform)canvas.transform;
            for(int i=0;i<2;i++)
            {
                var frame=i==0?vitals:objective!=null?objective.Frame:null;if(frame==null)continue;
                frame.GetWorldCorners(corners);
                Vector2 low=(Vector2)root.InverseTransformPoint(corners[0])-size*.5f,high=(Vector2)root.InverseTransformPoint(corners[2])+size*.5f;
                if(point.x<=low.x||point.x>=high.x||point.y<=low.y||point.y>=high.y)continue;
                bool left=low.x+high.x<0;float across=left?high.x-point.x:point.x-low.x,down=point.y-low.y;
                if(down<across)point.y=low.y;else point.x=left?high.x:low.x;
            }
            return point;
        }
        void OnDestroy(){Health.DamageNumber-=Damage;PlayerBrain.QuickItemRefused-=OnItemRefused;}
    }
}
