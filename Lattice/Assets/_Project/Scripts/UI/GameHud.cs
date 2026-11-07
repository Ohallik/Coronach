using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Profiling;
namespace Lattice.UI
{
    /// <summary>Reads the frame after the camera has moved and every blow has landed (MeleeContact swings at 700). Read before them, the frame a boss went down still said
    /// 'COLLAR LOCK 68/27500' and OPENING over the rewards it had just paid, and the reticle trailed a moving body by a frame.</summary>
    [DefaultExecutionOrder(905)]
    public sealed class GameHud:MonoBehaviour
    {
        Canvas canvas;
        static readonly Vector2 TopLeft=new(0,1),TopCentre=new(.5f,1),TopRight=new(1,1),BottomLeft=new(0,0),BottomCentre=new(.5f,0),BottomRight=new(1,0),Middle=new(.5f,.5f);
        /// <summary>Places by the 1080p layout's coordinates, held from the nearest corner or edge: the same pixels at 16:9, and still in
        /// its corner on any other screen. Everything used to hang from the bottom-left and ran off the top of a 21:9 screen.</summary>
        static RectTransform Corner(GameObject go,Vector2 anchor,Vector2 pos,Vector2 size)=>UiKit.Rect(go,anchor,anchor,pos-Vector2.Scale(anchor,new(1920,1080)),size);
        RectTransform vitals,skillRow;ObjectiveHud objective;QuestToast toast;BarkPanel barks;TextPlate targetPlate,skillsPlate;
        /// <summary>How far off an awake enemy is worth a look, and how near one makes this a fight, which is when a partner behind a plate counts: on a calm walk she dimmed the objective for nothing. A flight's fights stand further off.</summary>
        const float AwakeRange=60,FightRange=18,FlightFightRange=45;
        /// <summary>How far the lock's brackets reach from its middle: they cross a stepped-aside plate's words before the middle is behind it, and they dim from there, the margin they always had.</summary>
        const float ReticleReach=45;
        readonly PanelFade[] panels=new PanelFade[8];
        readonly BodyAim aim=new(),bossAim=new();
        TMP_Text status,partner,target,prompt,skills,zone,probe,hpLabel,chargeLabel,thrustLabel;
        Image hp,charge,thrust,reticle;
        readonly Image[] skillIcons=new Image[4];
        // The boss bar (D198): name, integrity with a lagging chip, phase notches, break gauge and the fight's state.
        GameObject bossRoot;TMP_Text bossName,bossEpithet,bossState;Image bossBack,bossFill,bossChip,bossBreak;
        readonly System.Collections.Generic.List<Image> bossNotches=new();float bossChipFraction=1;BossController shownBoss,introduced;
        TMP_Text bossCard,bossCardEpithet;float bossCardAge=99;Image bossRule;RectTransform bossCardRule;
        CantorCollar shownCollar;float[] bossMarks=System.Array.Empty<float>();
        /// <summary>When the card has come in, when it starts to go, and when it's gone: about a second and a half of hold. At three seconds it was still over the boss as her first attack came.</summary>
        const float BossCardIn=.25f,BossCardHold=1.75f,BossCardGone=2.1f;
        static readonly Color OpeningWords=new(1,.8f,.3f),ShiftingWords=new(.95f,.85f,1f);
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
            var frame=UiKit.DarkFrame(canvas.transform,"VitalsFrame");frame.pixelsPerUnitMultiplier=2;vitals=Corner(frame.gameObject,TopLeft,new(277,918),new(524,298));frame.raycastTarget=false;objective=GetComponent<ObjectiveHud>();toast=GetComponent<QuestToast>();barks=GetComponent<BarkPanel>();
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
            UiKit.Legible(skills);UiKit.Legible(prompt);UiKit.Legible(target);UiKit.Legible(zone).color=UiKit.LooseTextColor;
            // Only a panel's plate steps aside, so what it says has to hold on the floor without it.
            foreach(var word in new[]{status,hpLabel,chargeLabel,thrustLabel,partner})UiKit.Legible(word).color=UiKit.LooseTextColor;
            var promptPlate=TextPlate.Attach(prompt,"InteractionPlate");skillsPlate=TextPlate.Attach(skills,"SkillsPlate",34);targetPlate=TextPlate.Attach(target,"TargetPlate",34);
            // The zone's name had nothing behind it, and a strut or a neon strip ran straight through its letters.
            var zonePlate=TextPlate.Attach(zone,"ZonePlate",24);
            panels[0]=new PanelFade(vitals,frame);panels[1]=new PanelFade(targetPlate.Frame.rectTransform,targetPlate.Frame);panels[2]=new PanelFade(promptPlate.Frame.rectTransform,promptPlate.Frame);
            panels[7]=new PanelFade(zonePlate.Frame.rectTransform,zonePlate.Frame);
            // First in the canvas, so under every plate: drawn over them, its brackets cut through their words ('LO..KED').
            reticle=UiKit.Panel(canvas.transform,"TargetReticle",Color.white);reticle.sprite=UiSkin.Kit("reticle");reticle.raycastTarget=false;reticle.preserveAspect=true;reticle.transform.SetAsFirstSibling();
            UiKit.Rect(reticle.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(90,90));
            for(int i=0;i<4;i++)
            {
                skillIcons[i]=UiKit.Panel(canvas.transform,"Skill"+i,Color.white);skillIcons[i].raycastTarget=false;
                Corner(skillIcons[i].gameObject,BottomRight,new(1340+i*150,112),new(74,74));
                skillLabels[i]=UiKit.Legible(Text("SkillLabel"+i,BottomRight,new(1340+i*150,42),new(145,62),24));skillLabels[i].color=UiKit.LooseTextColor;
            }
            // The row's whole footprint, for a sign to keep out of: it has no plate of its own.
            var row=new GameObject("SkillRow",typeof(RectTransform));row.transform.SetParent(canvas.transform,false);skillRow=Corner(row,BottomRight,new(1565,80),new(595,138));
#if LATTICE_DEV || UNITY_EDITOR
            probe=Text("InputProbe",BottomRight,new(1630,280),new(500,180),16,TextAlignmentOptions.Right);
#endif
            BuildBossBar();gameObject.AddComponent<BossEdgeMark>().Build(canvas.transform);
            Health.DamageNumber+=Damage;PlayerBrain.QuickItemRefused+=OnItemRefused;PlayerBrain.SwapRefused+=OnSwapRefused;
        }
        void BuildBossBar()
        {
            bossRoot=new GameObject("BossBar",typeof(RectTransform));bossRoot.transform.SetParent(canvas.transform,false);
            var root=(RectTransform)bossRoot.transform;root.anchorMin=root.anchorMax=BottomCentre;root.pivot=Vector2.zero;root.anchoredPosition=Vector2.zero;root.sizeDelta=Vector2.zero;
            // The words have no frame and lie over the fullest markings and the brightest floors.
            var band=UiKit.SoftBand(bossRoot.transform,"BossBand",new Color(.01f,.02f,.04f,.6f));UiKit.Rect(band.gameObject,Vector2.zero,Vector2.zero,new(0,178),new(1060,132));
            panels[3]=new PanelFade(band.rectTransform,band,.55f);
            bossName=BossText("BossName",new(0,216),28,TextAlignmentOptions.Left);
            bossEpithet=BossText("BossEpithet",new(0,216),24,TextAlignmentOptions.Right);
            bossBack=UiKit.Panel(bossRoot.transform,"BossIntegrityBack",new Color(.025f,.045f,.065f,.9f));bossBack.raycastTarget=false;
            UiKit.Rect(bossBack.gameObject,Vector2.zero,Vector2.zero,new(0,186),new(BossBarWidth,20));
            bossChip=BossFill(bossBack.transform,"BossIntegrityChip",new Color(1,.9f,.72f,.8f),20);
            bossFill=BossFill(bossBack.transform,"BossIntegrity",new Color(.86f,.25f,.2f),20);
            var breakBack=UiKit.Panel(bossRoot.transform,"BossBreakBack",new Color(.025f,.045f,.065f,.9f));breakBack.raycastTarget=false;
            UiKit.Rect(breakBack.gameObject,Vector2.zero,Vector2.zero,new(0,168),new(BossBarWidth,7));
            bossBreak=BossFill(breakBack.transform,"BossBreak",new Color(1,.62f,.2f,.85f),7);
            // What the player acts on, so the strongest line of them.
            bossState=BossText("BossState",new(0,140),30,TextAlignmentOptions.Center);bossState.fontStyle=FontStyles.Bold;
            for(int i=0;i<5;i++)
            {
                var notch=UiKit.Panel(bossBack.transform,"BossNotch"+i,new Color(.95f,.95f,1,.95f));notch.raycastTarget=false;
                var rect=UiKit.Rect(notch.gameObject,new(0,.5f),new(0,.5f),Vector2.zero,new(3,28));rect.pivot=new(.5f,.5f);bossNotches.Add(notch);
            }
            // Under the skill icons: the band's lower edge reaches the first two, and a fight's next press mustn't go dim for it.
            bossRoot.transform.SetSiblingIndex(skillIcons[0].transform.GetSiblingIndex());
            bossRoot.SetActive(false);
            // The title card: the boss's name as its fight begins, in the band over the hero and under the target plate, clear of both corners.
            // Across the middle of the screen it lay on the hero and on the first marked attack. With a dark band behind it, it hid a boss standing under it from the hips up: its words hold on their own for the moment they're up.
            bossRule=UiKit.Panel(canvas.transform,"BossTitleRule",UiKit.GoldColor);bossRule.raycastTarget=false;bossRule.enabled=false;bossCardRule=Corner(bossRule.gameObject,TopCentre,new(960,811),new(620,3));
            bossCard=UiKit.Legible(UiKit.Heading(canvas.transform,"BossTitleCard","",58,new Color(1,.93f,.8f)));
            Corner(bossCard.gameObject,TopCentre,new(960,850),new(740,76));bossCard.textWrappingMode=TextWrappingModes.NoWrap;bossCard.raycastTarget=false;
            bossCardEpithet=UiKit.Legible(UiKit.Text(canvas.transform,"BossTitleEpithet","",28,UiKit.LooseTextColor,TextAlignmentOptions.Center));
            Corner(bossCardEpithet.gameObject,TopCentre,new(960,788),new(740,44));bossCardEpithet.raycastTarget=false;
            bossCard.alpha=bossCardEpithet.alpha=0;
        }
        TMP_Text BossText(string name,Vector2 pos,int font,TextAlignmentOptions align)
        {
            var t=UiKit.Legible(UiKit.Text(bossRoot.transform,name,"",font,UiKit.LooseTextColor,align));UiKit.Rect(t.gameObject,Vector2.zero,Vector2.zero,pos,new(BossBarWidth,36));
            t.raycastTarget=false;return t;
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
            AdvanceBossCard();
            if(!show){shownBoss=null;shownCollar=null;return false;}
            var health=boss.Health;
            if(boss!=shownBoss){shownBoss=boss;shownCollar=boss.GetComponent<CantorCollar>();bossMarks=MarksFor(boss);bossChipFraction=FightLeft(health);}
            float fraction=FightLeft(health);
            bossChipFraction=fraction>=bossChipFraction?fraction:Mathf.MoveTowards(bossChipFraction,fraction,Time.unscaledDeltaTime*.22f);
            bossFill.rectTransform.sizeDelta=new Vector2(BossBarWidth*fraction,20);bossChip.rectTransform.sizeDelta=new Vector2(BossBarWidth*bossChipFraction,20);
            bossFill.color=boss.Shifting?new Color(.95f,.85f,1f):new Color(.86f,.25f,.2f);
            float meter=health.Broken?1:Mathf.Clamp01(health.BreakMeter/Mathf.Max(1,health.breakThreshold));
            bossBreak.rectTransform.sizeDelta=new Vector2(BossBarWidth*meter,7);bossBreak.color=health.Broken?new Color(1,.92f,.45f):new Color(1,.62f,.2f,.85f);
            if(bossName.text!=boss.DisplayName)bossName.text=boss.DisplayName;
            // The card says it first: the bar takes it up when the card has gone, not the same words twice at once.
            string epithet=bossCardAge<BossCardGone?"":boss.Epithet;if(bossEpithet.text!=epithet)bossEpithet.text=epithet;
            for(int i=0;i<bossNotches.Count;i++)
            {
                bool on=i<bossMarks.Length;if(bossNotches[i].gameObject.activeSelf!=on)bossNotches[i].gameObject.SetActive(on);
                if(on)bossNotches[i].rectTransform.anchoredPosition=new Vector2(BossBarWidth*bossMarks[i],0);
            }
            string state=boss.Shifting?boss.ShiftText:health.Broken?"OPENING  ·  STRIKE NOW":!health.Damageable?boss.GuardedText:boss.StatusText;
            if(bossState.text!=state)bossState.text=state;
            bossState.color=boss.Shifting?ShiftingWords:health.Broken?OpeningWords:UiKit.LooseTextColor;
            return true;
        }
        /// <summary>The card enters: the name's letters close up as it fades in and a gold rule draws out under it. Its clock runs from when the bar can be seen: a conversation hides the HUD, and the card spent itself behind Hessa's.</summary>
        void AdvanceBossCard()
        {
            if(!GameInput.Current.Blocked)bossCardAge+=Time.unscaledDeltaTime;
            float card=bossCardAge<BossCardIn?bossCardAge/BossCardIn:bossCardAge<BossCardHold?1:Mathf.Clamp01(1-(bossCardAge-BossCardHold)/(BossCardGone-BossCardHold));
            bossCard.alpha=bossCardEpithet.alpha=card;
            if(bossRule.enabled!=card>0)bossRule.enabled=card>0;
            if(card<=0)return;
            bossRule.canvasRenderer.SetAlpha(card);
            if(bossCardAge<.6f||bossCard.characterSpacing!=6)
            {
                float settled=Mathf.Clamp01(bossCardAge/.45f);settled=1-(1-settled)*(1-settled)*(1-settled);
                bossCard.characterSpacing=Mathf.Lerp(24,6,settled);bossCardRule.sizeDelta=new Vector2(Mathf.Lerp(0,620,settled),3);
            }
        }
        /// <summary>What's left of the whole fight. The Cantor's bar is its head and its links together, by their shares: a link at 66/5100 over a full bar read as nearly dead with nothing gone.</summary>
        float FightLeft(Health head)
        {
            float left=head.integrity,whole=head.maximum;
            if(shownCollar!=null)
            {
                var links=shownCollar.Links;
                for(int i=0;i<links.Count;i++)if(links[i]!=null){left+=links[i].integrity;whole+=links[i].maximum;}
            }
            return Mathf.Clamp01(left/Mathf.Max(1,whole));
        }
        /// <summary>The phase notches on that same bar. The Cantor's bar comes down a link at a time, so each link has its mark and a cut lands on one, the last where the head opens; the head's two phases fall inside its share.</summary>
        float[] MarksFor(BossController boss)
        {
            var thresholds=boss.Thresholds;if(shownCollar==null)return thresholds;
            var head=boss.Health;float whole=head.maximum;var links=shownCollar.Links;int count=0;
            for(int i=0;i<links.Count;i++)if(links[i]!=null){whole+=links[i].maximum;count++;}
            whole=Mathf.Max(1,whole);float left=whole,share=head.maximum/whole;var marks=new float[count+thresholds.Length];int at=0;
            for(int i=0;i<links.Count;i++)if(links[i]!=null){left-=links[i].maximum;marks[at++]=left/whole;}
            for(int i=0;i<thresholds.Length;i++)marks[at++]=share*thresholds[i];
            return marks;
        }
        const float BarWidth=400;
        float refusedHeard,itemRefusedAt=-99,swapRefusedAt=-99;
        /// <summary>A break threshold this high means the target can't be broken at all, so it gets no gauge (pressure organs).</summary>
        const float Unbreakable=10000;
        TMP_Text Text(string name,Vector2 anchor,Vector2 pos,Vector2 size,int font,TextAlignmentOptions align=TextAlignmentOptions.Center)
        {var t=UiKit.Text(canvas.transform,name,"",font,UiKit.TextColor,align);Corner(t.gameObject,anchor,pos,size);return t;}
        Image Bar(string name,Vector2 pos,Color color)
        {
            // Their group used to let a pointer through.
            var bg=UiKit.Panel(canvas.transform,name+"Back",new Color(.025f,.045f,.065f,.85f));bg.raycastTarget=false;Corner(bg.gameObject,TopLeft,pos,new(BarWidth,14));
            var fill=UiKit.Panel(bg.transform,name,color);fill.raycastTarget=false;var rect=UiKit.Rect(fill.gameObject,new(0,.5f),new(0,.5f),Vector2.zero,new(BarWidth,14));rect.pivot=new(0,.5f);return fill;
        }
        void LateUpdate()
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
            AdvanceNumbers();
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
            if(party.members.Length>1){var p=party.members[1-party.index];string line=$"{PromptService.Tag("Swap")}  {DisplayNames.Speaker(p.character).ToUpperInvariant()}"+(p.Health.Alive?$"  {p.Health.integrity:0}/{p.Health.maximum:0}":"")+(p.Recovering?"  ·  RECOVERING":p.Health.Alive?"":party.ReviveTarget==p?"  ·  RESTORING "+(party.ReviveProgress*100).ToString("0")+"%":"  ·  APPROACH TO REVIVE");partner.text=Time.unscaledTime-swapRefusedAt<.25f?"<color=#FF4D40>"+line+"</color>":line;}
            zone.text=DisplayNames.Zone(ZoneController.Current.definition.id).ToUpperInvariant();
            bool bossUp=RefreshBossBar(a);
            if(a.target!=null&&a.target.Alive&&bossUp&&shownBoss!=null&&a.target==shownBoss.Health)
                // The bar has a boss's name and gauge. The Cantor's lock is the one thing on its body the objective names, so the plate names it too, with its progress.
                target.text=shownCollar!=null?(a.TargetLocked?"LOCKED  ·  ":"")+$"COLLAR LOCK   {a.target.integrity:0}/{a.target.maximum:0}\n"+TargetKeys(a):(a.TargetLocked?"LOCKED    ":"")+TargetKeys(a);
            else if(a.target!=null&&a.target.Alive)
                target.text=(a.TargetLocked?"LOCKED  ·  ":"")+$"{(a.target.GetComponent<CantorCollar>()!=null?"Cantor collar lock":DisplayNames.Target(a.target.id)).ToUpperInvariant()}   {a.target.integrity:0}/{a.target.maximum:0}\n"+
                    (a.target.Broken?"BROKEN    ":a.target.breakThreshold>=Unbreakable?"":"BREAK "+a.target.BreakMeter.ToString("0")+" / "+a.target.breakThreshold.ToString("0")+"    ")+
                    TargetKeys(a);
            else target.text="";
            reticle.enabled=a.target!=null&&a.target.Alive;
            if(reticle.enabled)
            {
                var screen=Camera.main.WorldToScreenPoint(aim.Centre(a.target));
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,screen,null,out var point);reticle.rectTransform.anchoredPosition=point;
                // It stays on its target under a panel that has stepped aside, but dim there: the panel's words are read through it.
                var mark=a.TargetLocked?new Color(1,.76f,.3f):new Color(1,1,1,.55f);
                float brackets=ReticleReach*canvas.scaleFactor;foreach(var panel in panels)if(panel!=null&&panel.Touches(screen,brackets)){mark.a*=.4f;break;}
                reticle.color=mark;
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
            // The first fight teaches swap (TEST_PLAN 4.1). At the widest a keyboard's words can make it, with the longest quick item, this strip stands 1091 of its 1190 at the type floor.
            string swap=combat&&party.members.Length>1?Hint("Swap","SWAP")+"    ":"";
            skills.text=!combat?Hint("Interact","INTERACT")+(party.members.Length>1?"    "+Hint("Swap","SWAP"):"")+"    "+Hint(a.flight?"Boost":"Sprint",a.flight?"BOOST":"SPRINT")+(a.flight?"    "+Hint("Brake","BRAKE"):"")+"    "+Hint("Log","QUESTS")+"    "+Hint("Pause","MENU"):
                a.flight?Hint("Fire","FIRE")+"    "+Hint("Lunge","LUNGE")+"    "+Hint("Roll","ROLL")+"    "+swap+Hint("Boost","BOOST")+"    "+Hint("Brake","BRAKE"):
                Hint("Attack","ATTACK")+"    "+Hint("Dodge","DODGE")+"    "+Hint("Guard","GUARD")+"    "+swap+Hint("Sprint","SPRINT")+"    "+QuickItem(state);
            if(probe!=null)probe.text=$"PAD: {PadBridge.Describe()}\nMOVE {GameServices.Current.Input.Move}\n{a.State}  {a.motor?.Velocity.magnitude:0.0} m/s";
            StepAside(party,a);
        }
        /// <summary>The middle of a body as it's drawn, measured once when it's first aimed at and carried on its own transform after that: a bobbing dart keeps its
        /// mark with no search every frame. A root's position plus a metre missed flying bodies by 30 to 40 px, so the brackets framed empty sky.</summary>
        sealed class BodyAim
        {
            Health of;Transform body;Vector3 local;
            public Vector3 Centre(Health target)
            {
                if(target!=of||body==null)
                {
                    of=target;body=target.TryGetComponent<DefeatPresentation>(out var shown)&&shown.visual!=null?shown.visual:target.transform;
                    var bounds=new Bounds();bool found=false;
                    foreach(var view in body.GetComponentsInChildren<Renderer>())
                    {
                        if(view is not (MeshRenderer or SkinnedMeshRenderer))continue;
                        if(found)bounds.Encapsulate(view.bounds);else{bounds=view.bounds;found=true;}
                    }
                    if(!found&&target.TryGetComponent<Collider>(out var shape)&&shape.enabled){bounds=shape.bounds;found=true;}
                    local=body.InverseTransformPoint(found?bounds.center:target.transform.position+Vector3.up);
                }
                return body.TransformPoint(local);
            }
        }
        /// <summary>Who is genuinely behind a plate this frame, from a short fixed list: the active hero, the target, the boss, an awake enemy near enough to matter, and in a fight the partner.
        /// A body counts by its middle and its top: by its middle alone, one with its upper half behind the vitals left them solid.</summary>
        void StepAside(PartyController party,CombatActor a)
        {
            var camera=Camera.main;if(camera==null)return;
            if(panels[4]==null&&objective!=null)panels[4]=objective.Fade;
            if(panels[5]==null&&toast!=null)panels[5]=toast.Fade;
            if(panels[6]==null&&barks!=null)panels[6]=barks.Fade;
            float now=Time.unscaledTime,fightRange=a.flight?FlightFightRange:FightRange;
            PanelFade.BeginPlates();
            foreach(var panel in panels)panel?.Measure();
            // A sign is cut by these two as well, though they don't fade themselves.
            if(skillsPlate.Frame.enabled)PanelFade.AddPlate(skillsPlate.Frame.rectTransform);
            if(ZoneController.Current.Combat)PanelFade.AddPlate(skillRow);
            Watch(camera,a,now);
            if(a.target!=null&&a.target.Alive)Look(camera.WorldToScreenPoint(aim.Centre(a.target)),now);
            bool fight=shownBoss!=null;
            if(fight&&shownBoss.Health!=a.target)Look(camera.WorldToScreenPoint(bossAim.Centre(shownBoss.Health)),now);
            foreach(var body in Health.All)
            {
                if(body==null||body.friendly||!body.Alive)continue;
                var at=body.transform.position;var gap=at-a.transform.position;gap.y=0;float far=gap.sqrMagnitude;if(far>AwakeRange*AwakeRange)continue;
                Vector3 low=camera.WorldToScreenPoint(at+Vector3.up*.75f),high=camera.WorldToScreenPoint(at+Vector3.up*1.6f);
                bool behind=Behind(low)||Behind(high),near=far<fightRange*fightRange;
                if(!behind&&(fight||!near))continue;
                if(!body.TryGetComponent<EnemyBrain>(out var brain)||!brain.enabled||brain.Passive)continue;
                fight|=near;
                if(behind){Look(low,now);Look(high,now);}
            }
            if(fight&&party.members.Length>1)Watch(camera,party.members[1-party.index],now);
            foreach(var panel in panels)panel?.Step(now,Time.unscaledDeltaTime);
        }
        bool Behind(Vector3 screen){foreach(var panel in panels)if(panel!=null&&panel.Contains(screen))return true;return false;}
        void Look(Vector3 screen,float now){foreach(var panel in panels)panel?.Look(screen,now);}
        /// <summary>On foot the head counts as well as the middle, being what shows first at a panel's edge. A ship or a body lying down is one low shape.</summary>
        void Watch(Camera camera,CombatActor hero,float now)
        {
            var at=hero.transform.position;bool standing=hero.Health.Alive&&!hero.flight;
            Look(camera.WorldToScreenPoint(at+Vector3.up*(standing?1:.3f)),now);
            if(standing)Look(camera.WorldToScreenPoint(at+Vector3.up*1.7f),now);
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
        void OnSwapRefused(){swapRefusedAt=Time.unscaledTime;UiSounds.Error();}
        /// <summary>The skill's button on the active device: RB plus the face glyph on a pad, the number key on a keyboard.</summary>
        public static string SkillBinding(int slot,bool flight)
        {
            if(PromptService.Device!=PromptDevice.Gamepad)return PromptService.Tag("Skill"+(slot+1));
            string[] faces=flight?new[]{"Fire","Roll","Lunge","Swap"}:new[]{"Attack","Dodge","Guard","Swap"};
            return PromptService.Tag("SkillMod")+"+"+PromptService.Tag(faces[slot]);
        }
        static string Readable(string value)=>System.Text.RegularExpressions.Regex.Replace(value??"","(?<=[a-z])(?=[A-Z])"," ");
        // ---------- damage numbers ----------
        /// <summary>A number holds for most of its life and then fades; all rise at one speed, so what's clear of its neighbours when it lands stays clear.</summary>
        const float NumberLife=1.05f,NumberHold=.7f,NumberRise=.85f;
        /// <summary>Hits on one body this close together add up on one number while they keep coming, up to the most one number runs. Inside .1 s only a volley merged: a stream of swings on a fast target left a fan of the same digits over what the player was aiming at.</summary>
        const float MergeWindow=.28f,MergeMost=.7f;
        /// <summary>The air between neighbours: with 8, '16 16 216!' read as one string.</summary>
        static readonly Vector2 NumberGap=new(18,4);
        /// <summary>How far in from the screen's edge a body's fan stands. Clamped a number at a time, the numbers of a body off the screen lay in a dead-straight column against the edge.</summary>
        static readonly Vector2 FanRoom=new(220,120);
        /// <summary>How much of the lock reticle's middle a number leaves clear when it lands, and how far under it it won't land, since all numbers rise.</summary>
        const float ReticleMiddle=34,ReticleRise=70;
        sealed class Floater{public TMP_Text text;public RectTransform rect;public Health from;public HitKind kind;public string mark;public float amount,born,lastHit,age,quiet;public Vector3 world;public Vector2 offset,size;public bool rises;}
        readonly System.Collections.Generic.List<Floater> floaters=new();
        /// <summary>Where to try a new number, in its own widths and heights from its body: beside it first, then over and under.</summary>
        static readonly Vector2[] Slots={new(0,0),new(1,0),new(-1,0),new(0,1),new(1,1),new(-1,1),new(2,0),new(-2,0),new(0,-1),new(1,-1),new(-1,-1),new(2,1),new(-2,1),new(0,2),new(1,2),new(-1,2),new(2,-1),new(-2,-1),new(2,2),new(-2,2)};
        void Damage(Health health,float amount,HitKind kind)
        {
            if(canvas==null||health==null||Camera.main==null)return;
            float now=Time.unscaledTime;
            // Hits landing together read as one: a volley's pellets or a spin's ticks were a stack of the same digits. It's held from its last hit, and goes on rising from its first.
            foreach(var open in floaters)if(open.from==health&&open.kind==kind&&now-open.lastHit<MergeWindow&&now-open.born<MergeMost)
            {open.amount+=amount;open.lastHit=now;open.quiet=0;open.text.text=open.amount.ToString("0")+open.mark;open.size=open.text.GetPreferredValues(open.text.text,160,60)+NumberGap;open.rect.SetAsLastSibling();return;}
            var (mark,size,color)=kind switch{HitKind.Weak=>("!",32f,new Color(1,.72f,.26f)),HitKind.WeakSpot=>("!!",34f,new Color(1,.93f,.45f)),HitKind.Resisted=>("",24f,new Color(.62f,.66f,.7f)),_=>("",27f,Color.white)};
            var t=UiKit.Legible(UiKit.Text(canvas.transform,"Damage",amount.ToString("0")+mark,size,color));
            var rect=UiKit.Rect(t.gameObject,Middle,Middle,Vector2.zero,new(160,60));
            var floater=new Floater{text=t,rect=rect,from=health,kind=kind,mark=mark,amount=amount,born=now,lastHit=now,world=health.transform.position+Vector3.up*1.9f,size=t.GetPreferredValues(t.text,160,60)+NumberGap};
            Seat(floater);floaters.Add(floater);
        }
        /// <summary>The first slot clear of every number still up, on anyone: two bodies side by side smeared just as one did.</summary>
        void Seat(Floater floater)
        {
            var anchor=FanAnchor(floater.world);var spot=anchor;var size=floater.rect.sizeDelta;floater.rises=true;bool crowded=false;
            foreach(var slot in Slots)
            {
                var wanted=anchor+Vector2.Scale(slot,floater.size);var tried=ClearOfFrames(OnScreen(wanted,size),size);
                if(!OffTheLock(tried,floater.size))continue;
                // With every slot taken a number lands on another number before it lands on the lock: eight up at once fills them, and the fall-back was the anchor, the lock's very middle.
                if(!crowded){crowded=true;spot=tried;floater.rises=tried==wanted;}
                if(!Free(tried,floater.size))continue;
                spot=tried;floater.rises=tried==wanted;break;
            }
            // Put aside by a frame or the screen's edge, it holds there: rising, it would only press against what it left.
            floater.offset=spot-anchor;floater.rect.anchoredPosition=spot;
        }
        /// <summary>The middle of the lock is where the player is aiming: '!!' piled on a head hid the head and the reticle both.</summary>
        bool OffTheLock(Vector2 at,Vector2 size)
        {
            if(!reticle.enabled)return true;
            var off=at-reticle.rectTransform.anchoredPosition;float above=size.y*.5f+ReticleMiddle;
            return !(Mathf.Abs(off.x)<size.x*.5f+ReticleMiddle&&off.y<above&&off.y>-above-ReticleRise);
        }
        bool Free(Vector2 at,Vector2 size)
        {
            foreach(var other in floaters)
            {
                var gap=at-other.rect.anchoredPosition;
                if(Mathf.Abs(gap.x)<(size.x+other.size.x)*.5f&&Mathf.Abs(gap.y)<(size.y+other.size.y)*.5f)return false;
            }
            return true;
        }
        void AdvanceNumbers()
        {
            if(Camera.main==null)return;
            for(int i=floaters.Count-1;i>=0;i--)
            {
                var floater=floaters[i];floater.age+=Time.unscaledDeltaTime;floater.quiet+=Time.unscaledDeltaTime;
                if(floater.quiet>=NumberLife){Destroy(floater.text.gameObject);floaters.RemoveAt(i);continue;}
                var at=floater.rises?floater.world+Vector3.up*(NumberRise*floater.age):floater.world;var size=floater.rect.sizeDelta;
                floater.rect.anchoredPosition=ClearOfFrames(OnScreen(FanAnchor(at)+floater.offset,size),size);
                floater.text.alpha=floater.quiet<NumberHold?1:1-(floater.quiet-NumberHold)/(NumberLife-NumberHold);
            }
        }
        Vector2 OnCanvas(Vector3 world)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,Camera.main.WorldToScreenPoint(world),null,out var point);return point;
        }
        Vector2 FanAnchor(Vector3 world)=>OnScreen(OnCanvas(world),FanRoom);
        /// <summary>A number never leaves the screen: a '43!' over a boss at the top edge was cut in half. This goes before the frames, or a body just off the screen is pressed back across them.</summary>
        Vector2 OnScreen(Vector2 point,Vector2 size)
        {
            var room=((RectTransform)canvas.transform).rect.size*.5f-size*.5f;
            return new Vector2(Mathf.Clamp(point.x,-room.x,room.x),Mathf.Clamp(point.y,-room.y,room.y));
        }
        static readonly Vector3[] corners=new Vector3[4];
        /// <summary>A number floats where its body stands, but not across what the player reads mid-fight: the vitals, the objective, the target
        /// plate, a boss's name while it's up, and the skill row and hint bar along the screen's foot. So it leaves them downward or toward the middle of the screen, whichever is nearer, or up off the foot.</summary>
        Vector2 ClearOfFrames(Vector2 point,Vector2 size)
        {
            var root=(RectTransform)canvas.transform;
            for(int i=0;i<7;i++)
            {
                var frame=i switch{0=>vitals,1=>objective!=null?objective.Frame:null,2=>targetPlate!=null&&targetPlate.Frame.enabled?targetPlate.Frame.rectTransform:null,3=>bossCard!=null&&bossCard.alpha>0?bossCard.rectTransform:null,4=>bossCardEpithet!=null&&bossCardEpithet.alpha>0?bossCardEpithet.rectTransform:null,5=>ZoneController.Current!=null&&ZoneController.Current.Combat?skillRow:null,_=>skillsPlate!=null&&skillsPlate.Frame.enabled?skillsPlate.Frame.rectTransform:null};if(frame==null)continue;
                frame.GetWorldCorners(corners);
                Vector2 low=(Vector2)root.InverseTransformPoint(corners[0])-size*.5f,high=(Vector2)root.InverseTransformPoint(corners[2])+size*.5f;
                if(point.x<=low.x||point.x>=high.x||point.y<=low.y||point.y>=high.y)continue;
                if(i>=5){point.y=high.y;continue;}
                bool left=low.x+high.x<0;float across=left?high.x-point.x:point.x-low.x,down=point.y-low.y;
                if(down<across)point.y=low.y;else point.x=left?high.x:low.x;
            }
            return point;
        }
        void OnDestroy(){Health.DamageNumber-=Damage;PlayerBrain.QuickItemRefused-=OnItemRefused;PlayerBrain.SwapRefused-=OnSwapRefused;}
    }
}
