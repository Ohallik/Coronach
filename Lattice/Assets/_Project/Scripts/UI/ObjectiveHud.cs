using System.Linq;
using Lattice.Core;
using Lattice.Combat;
using Lattice.Data;
using Lattice.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>Read after the frame's blows have landed, as the HUD is: it said 'Release the exposed collar lock' over the rewards.</summary>
    [DefaultExecutionOrder(905)]
    public sealed class ObjectiveHud:MonoBehaviour
    {
        /// <summary>What the panel asks of the player now, without its distance line: the quest log falls back on it.</summary>
        public static string CurrentLabel{get{var hud=FindFirstObjectByType<ObjectiveHud>();return hud!=null&&hud.text!=null?hud.text.text.Split('\n')[0]:"";}}
        TMP_Text text;float next;Canvas canvas;bool inFight;
        Image arrow,alsoArrow;Vector3? goalAt,alsoAt;float hereAt=Here;
        /// <summary>The frame stands from here down, and is as tall as what it says: four lines' height round one line left a dark block over whatever the fight was doing, and hid half of the launch pad it named.</summary>
        public const float FrameTop=92.5f;
        const float LinePitch=27.6f,FrameAir=60,FrameLeast=88;
        /// <summary>Where the panel is sending the party: nowhere in a fight, or with nothing to go to.</summary>
        public Vector3? Goal=>goalAt;
        /// <summary>That's a hero who's down: worth a mark at the screen's edge on foot as well, where they can lie out of shot.</summary>
        public bool GoalIsHero{get;private set;}
        /// <summary>Nearer than this the goal is simply here: a distance and a direction mean nothing.</summary>
        const float Here=3;
        /// <summary>Room at the head of the distance line for the arrow.</summary>
        const string ArrowRoom="<space=44>";
        /// <summary>What floating numbers keep clear of.</summary>
        public RectTransform Frame{get;private set;}
        public PanelFade Fade{get;private set;}
        void Start()
        {
            canvas=UiKit.CreateCanvas("Objective",6,transform);var frame=UiKit.DarkFrame(canvas.transform,"ObjectiveFrame");frame.raycastTarget=false;
            // At the weight of the card under it, and of the vitals in the other corner. The full border left this frame room for three lines of type, and it can need four.
            frame.pixelsPerUnitMultiplier=2;
            Frame=UiKit.Rect(frame.gameObject,new(1,1),new(1,1),new(-300,-FrameTop),new(570,FrameLeast));Frame.pivot=new(.5f,1);Frame.anchoredPosition=new(-300,-FrameTop);
            // Its plate steps aside, so what it says has to hold on the floor without it, and gets a backing of its own as the plate goes: the faded panel's words lay straight on the launch pad they named.
            text=UiKit.Legible(UiKit.Text(frame.transform,"Objective","",24,UiKit.LooseTextColor,TextAlignmentOptions.MidlineLeft));
            UiKit.Rect(text.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(480,112));
            Fade=new PanelFade(Frame,frame).Backs(text);
            arrow=Pointer("ObjectiveArrow",36,UiKit.GoldColor);alsoArrow=Pointer("ObjectiveAlsoArrow",30,UiKit.DimTextColor);
        }
        Image Pointer(string name,float size,Color color)
        {
            var image=UiKit.Panel(text.transform,name,color);image.sprite=UiSkin.Pointer();image.preserveAspect=true;image.raycastTarget=false;image.enabled=false;
            UiKit.Rect(image.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(size,size));return image;
        }
        /// <summary>The camera is fixed, so the way to go is a way on the screen, and that's what the arrow shows. The word was the target's bearing from the
        /// camera ("behind" was down-screen), and read as wrong to anyone flying straight at it.</summary>
        static void Point(Image image,Vector3? goal,Vector3 hero,float here)
        {
            bool on=false;var camera=Camera.main;
            if(goal.HasValue&&camera!=null)
            {
                var gap=goal.Value-hero;gap.y=0;
                if(gap.sqrMagnitude>=here*here)
                {
                    on=true;
                    // A short step toward it: a straight line stays straight on screen, and a far goal can lie behind the camera itself.
                    Vector2 way=camera.WorldToScreenPoint(hero+gap.normalized*2)-camera.WorldToScreenPoint(hero);
                    image.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(way.y,way.x)*Mathf.Rad2Deg-90);
                }
            }
            if(image.enabled!=on)image.enabled=on;
        }
        /// <summary>The arrows stand on their lines, in the label's own space: the main one in the room left at the head of the distance, the small one after the last word.</summary>
        void SeatArrows(int distanceFrom)
        {
            text.ForceMeshUpdate();var info=text.textInfo;float middle=text.fontSize*.33f;
            for(int i=0;i<info.characterCount;i++)
            {
                var c=info.characterInfo[i];if(!c.isVisible||c.index<distanceFrom)continue;
                arrow.rectTransform.anchoredPosition=new Vector2(c.origin-25,c.baseLine+middle);break;
            }
            for(int i=info.characterCount-1;i>=0;i--)
            {
                var c=info.characterInfo[i];if(!c.isVisible)continue;
                alsoArrow.rectTransform.anchoredPosition=new Vector2(c.xAdvance+20,c.baseLine+middle);break;
            }
        }
        /// <summary>A hero down with no fight on: nothing goes on until they're up. Hessa waits to speak until the party stands, and the panel said 'Hear Hessa out' with its arrow on her, 3 m away, and nothing to press.</summary>
        static CombatActor Downed(PartyController party)
        {
            if(party.members.Length<2)return null;
            var other=party.Partner;if(other==null||other.Health.Alive)return null;
            var lead=party.Active;return EnemyBrain.AwakeNear(lead.transform.position,lead.flight?GameHud.FlightFightRange:GameHud.FightRange)?null:other;
        }
        /// <summary>With the bar up the boss is the objective, not the fixed spot the walk there pointed at. The Cantor's lines and Hessa's already name the fight.</summary>
        static string FightGoal(BossController boss,string label)=>boss.FightGoal(label);
        /// <summary>The nearest way out of here that leads to a scene.</summary>
        static InteractionPrompt WayTo(string scene,Vector3 from)=>Nearest(InteractionPrompt.Active.Where(p=>p is DockingPad pad&&pad.scene==scene||p is WarpBeacon warp&&warp.scene==scene),from);
        static InteractionPrompt Nearest(System.Collections.Generic.IEnumerable<InteractionPrompt> among,Vector3 from)=>among.OrderBy(p=>(p.transform.position-from).sqrMagnitude).FirstOrDefault();
        EncounterVolume[] fights;string fightsIn;
        /// <summary>Where a story step sends the arrow from here, by its data (QuestDef.stepTargets) or, unsaid, by the step itself: the way out to a Reach's scene,
        /// a TalkTo's speaker, the nearest discovery still to make with an Interact's report, a Kill's boss or the fight that holds it. In another zone than the step's
        /// it is the way out toward it. Nothing found is no arrow, and the words still stand.</summary>
        void Aim(QuestDef quest,int step,GameState state,Vector3 hero,ref InteractionPrompt target,ref Vector3? goal)
        {
            var aim=quest.stepTargets!=null&&step<quest.stepTargets.Length?quest.stepTargets[step]:default;var asked=quest.steps[step];
            if(string.IsNullOrEmpty(aim.scene)&&asked.kind==ObjectiveKind.Reach)aim.scene=asked.target;
            if(string.IsNullOrEmpty(aim.speaker)&&asked.kind==ObjectiveKind.TalkTo)aim.speaker=asked.target;
            if(string.IsNullOrEmpty(aim.report)&&asked.kind==ObjectiveKind.Interact)aim.report=asked.target;
            Aim(aim,asked.kind==ObjectiveKind.Kill?asked.target:null,state,hero,ref target,ref goal);
        }
        /// <summary>Where a target sends the arrow from here: a story step's (above), or between chapters the told chapter's <see cref="QuestDef.next"/>.</summary>
        void Aim(QuestTarget aim,string enemy,GameState state,Vector3 hero,ref InteractionPrompt target,ref Vector3? goal)
        {
            if(!string.IsNullOrEmpty(aim.scene)&&aim.scene!=state.zone)
            {
                target=WayTo(aim.scene,hero);
                // No door straight there: the way to the nearest scene between that this zone does lead to. Past our own place in the list is the way back.
                if(target==null&&aim.via!=null)foreach(string between in aim.via){if(between==state.zone)break;target=WayTo(between,hero);if(target!=null)break;}
                return;
            }
            if(!string.IsNullOrEmpty(aim.speaker))target=Nearest(InteractionPrompt.Active.Where(p=>p is Npc npc&&npc.speaker==aim.speaker),hero);
            if(target==null&&!string.IsNullOrEmpty(aim.report))target=Nearest(InteractionPrompt.Active.Where(p=>p is DiscoveryPoint point&&point.report==aim.report&&point.Wanted),hero);
            if(target!=null)return;
            if(!string.IsNullOrEmpty(aim.encounter)||enemy!=null)
            {
                // The boss itself once it's up, as the Relay's line follows Hessa; before that, the fight's own ground.
                var boss=BossController.Focus(hero,200);
                if(boss!=null&&(enemy==null||boss.Health.id==enemy)){goal=boss.transform.position;return;}
                if(fightsIn!=state.zone||fights==null||System.Array.Exists(fights,f=>f==null)){fights=FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None);fightsIn=state.zone;}
                var flags=GameServices.Current.Flags;float best=float.MaxValue;
                foreach(var fight in fights)
                {
                    if(string.IsNullOrEmpty(fight.encounterId)||flags.GetBool("clear."+fight.encounterId))continue;
                    bool named=!string.IsNullOrEmpty(aim.encounter)?fight.encounterId==aim.encounter:fight.spawners!=null&&System.Array.Exists(fight.spawners,s=>s!=null&&s.definition!=null&&s.definition.id==enemy);
                    if(!named)continue;
                    float gap=(fight.transform.position-hero).sqrMagnitude;if(gap<best){best=gap;goal=fight.transform.position;}
                }
                if(goal.HasValue)return;
            }
            if(aim.hasPoint)goal=aim.point;
        }
        void LateUpdate()
        {
            canvas.enabled=!GameInput.Current.Blocked;
            var party=PartyController.Current;if(party==null||party.members==null)return;
            var hero=party.Active.transform.position;
            // A bar going up or a boss falling changes what the panel should say now, not at its next look: it said 'Release the collar lock' over the rewards.
            var fight=BossController.Focus(hero);if((fight!=null)!=inFight){inFight=fight!=null;next=0;}
            Point(arrow,goalAt,hero,hereAt);Point(alsoArrow,alsoAt,hero,Here);
            if(Time.unscaledTime<next)return;next=Time.unscaledTime+.25f;
            var state=GameServices.Current.State;var flags=GameServices.Current.Flags;Vector3? goal=null;string label="";
            var prompts=InteractionPrompt.Active;
            InteractionPrompt target=null;
            // From chapter 3 on the story's own quest is the line, wherever the party stands: its step's words, and its data for the arrow (QuestDef.stepTargets).
            // Once one of those has begun, the first two chapters' lines below have had their say, in their own zones too.
            var story=StoryQuests.Open(state,out int storyStep,true);bool written=story==null&&!StoryQuests.BegunPastTheFirst(state);
            switch(written?state.zone:"")
            {
                case "Hub_CinderHalo":
                    if(!flags.GetBool("met.Orrin")){label="Dock at Orrin's office: Sorrel has gone quiet";target=prompts.OfType<DockingPad>().FirstOrDefault(p=>p.spawn=="Office");}
                    else if(flags.GetBool("bossdown.Cantor")){label="Warp to Tallow Approach: Keeper Abri is waiting";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.scene=="TallowApproach");}
                    else if(ChapterProgress.CanEnterGullet(state)){label="Enter the Gullet: Tallow Drift still answers beyond it";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.scene=="Gullet_Tunnel");}
                    else{label=flags.GetBool("warpkey")?"Explore Hushwell beneath Sorrel: the key's route has gaps":"Land on Sorrel and find out why it went quiet";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.scene=="Sorrel_Ridges");}
                    break;
                case "Hub_Decks":
                    if(flags.GetBool(OpeningMarketSequence.Requested)&&!flags.GetBool(OpeningMarketSequence.Repaired))
                    {label="Reopen the market walkway";target=prompts.OfType<DiscoveryPoint>().FirstOrDefault(p=>p.flag==OpeningMarketSequence.Repaired);goal=new Vector3(-14.6f,0,-3);}
                    else if(!flags.GetBool("met.Orrin")){label="Talk to Orrin";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Orrin");}
                    else{label="Return to the Halo";target=prompts.OfType<DockingPad>().OrderBy(p=>(p.transform.position-PartyController.Current.Active.transform.position).sqrMagnitude).FirstOrDefault();}
                    break;
                case "Sorrel_Ridges":
                    if(!flags.GetBool("met.Survivor")){label="Find the outpost crew: why did they stop answering?";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Survivor");}
                    else if(!flags.GetBool("bossdown.Burrower")){label="Follow the ridges to the drill and stop the Burrower";goal=new Vector3(0,0,164);}
                    else if(!flags.GetBool("warpkey")){label="Recover the survey key from the drill's cradle";target=prompts.OfType<KeyPickup>().FirstOrDefault();}
                    else if(ChapterProgress.NeedsNursery(state)){label="Descend the bore into Hushwell: the key's route has gaps";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.scene=="Hushwell");}
                    else{label=flags.GetBool("bossdown.Cantor")?"Return to the Halo":"Return to the Halo: the Gullet warp can read your route now";target=prompts.OfType<DockingPad>().FirstOrDefault(p=>p.spawn=="Outer");}
                    break;
                case "Hushwell":
                    if(!flags.GetBool("bossdown.BellowsBelow")){label="Follow the breathing down to the Bellows";goal=new Vector3(HushwellLayout.Bellows.x,HushwellLayout.Middle,HushwellLayout.Bellows.y);}
                    else if(!flags.GetBool("hushwell.nursery")){label="Find what the Bellows was guarding";target=prompts.OfType<DiscoveryPoint>().FirstOrDefault();}
                    else{label="Ride the drill-shaft lift back to Sorrel";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.requiredFlag=="hushwell.nursery");}
                    break;
                case "Gullet_Tunnel":
                    if(!flags.GetBool("bossdown.Cantor"))
                    {
                        label="Find the collared Cantor";goal=new Vector3(Lattice.Data.GulletProfile.Center(Lattice.Data.GulletProfile.CantorZ),1,Lattice.Data.GulletProfile.CantorZ);
                        foreach(var collar in CantorCollar.All)if(!collar.Resolved)
                        {
                            label=collar.Remaining>0?$"Cut the collar links ({3-collar.Remaining}/3)":"Release the exposed collar lock";goal=collar.transform.position;
                            var dormant=collar.GetComponent<DormantEnemy>();
                            if(dormant!=null&&dormant.Held)
                            {
                                var look=prompts.OfType<DiscoveryPoint>().FirstOrDefault(p=>p.flag=="gullet.collarObserved");
                                if(look!=null&&look.Available){label="Study the strained collar";goal=look.transform.position;}
                                else label="Approach the collar links";
                            }
                            break;
                        }
                    }
                    else{label="Warp to Tallow Approach: the way past the Cantor is open";target=prompts.OfType<WarpBeacon>().FirstOrDefault();}
                    break;
                case "TallowApproach":
                    if(Lantern(state)>=0&&!flags.GetBool("lanternwick.chartGiven"))
                    {label="Take the warp to the Wick Reach";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.scene=="WickReach");}
                    else if(flags.GetBool("met.Keeper")&&!flags.GetBool("tallow.lineObserved"))
                    {label="Inspect the port docking line";target=prompts.OfType<DiscoveryPoint>().FirstOrDefault(p=>p.flag=="tallow.lineObserved");}
                    else{label=flags.GetBool("met.Keeper")?"Dock at Tallow Drift":"Dock at Tallow Drift: it's still answering";target=prompts.OfType<DockingPad>().FirstOrDefault();}
                    break;
                case "TallowDrift":
                    if(flags.GetBool("tallow.lineObserved")&&!flags.GetBool("tallow.driftDiscussed"))
                    {label="Tell Keeper Abri what you found";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Keeper");}
                    else if(!flags.GetBool("met.Keeper")){label="Talk to Tallow's keeper";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Keeper");}
                    else if(!flags.GetBool("sliceComplete")){label="Repair and save at Tallow Drift, then see to Keeper Abri's line";target=prompts.OfType<RepairBay>().FirstOrDefault();}
                    else if(!flags.GetBool("tallow.lineObserved")){label="Launch to inspect the port docking line for Keeper Abri";target=prompts.OfType<DockingPad>().FirstOrDefault();}
                    else if(!flags.GetBool("compact.hailHeard")){label="The beacon band is crackling: see Keeper Abri";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Keeper");}
                    else if(Lantern(state)<0&&!flags.GetBool("lanternwick.chartGiven")){label="Ask Keeper Abri about a route that waits";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Keeper");}
                    else if(flags.GetBool("lanternwick.chartGiven")&&!flags.GetBool("chapter2.homecoming")){label="Bring Lanternwick's chart to Keeper Abri";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Keeper");}
                    // Chapter 2 is told and chapter 3 hasn't begun: Abri's last word on the lanterns, then her next talk is the Seed Road (SideStories). Until that quest opens
                    // and takes the line, this one sent the party back out to the Wick Reach, away from the one person who had something to say (first player run, November 13).
                    else if(flags.GetBool("chapter2.homecoming")){label=flags.GetBool(Lattice.World.SideStories.AfterwordHeard)?"Ask Keeper Abri about the cabins' dinner":"Hear what Keeper Abri says next";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Keeper");}
                    else{label="Launch for the Wick Reach warp";target=prompts.OfType<DockingPad>().FirstOrDefault();}
                    break;
                case "WickReach":
                {
                    var relays=prompts.OfType<DiscoveryPoint>().Where(p=>p.flag.StartsWith("wick.relay")&&p.Available).ToArray();
                    if(flags.GetBool("lanternwick.chartGiven")){label="Warp home to Tallow Approach";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.scene=="TallowApproach");}
                    else if(!flags.GetBool("wick.gateOpen")&&relays.Length>0)
                    {label=$"Relight the lantern relays to open the way ({2-relays.Length}/2)";target=relays.OrderBy(p=>(p.transform.position-PartyController.Current.Active.transform.position).sqrMagnitude).First();}
                    else{label="Dock at Lanternwick";target=prompts.OfType<DockingPad>().FirstOrDefault();}
                    break;
                }
                case "Lanternwick":
                    if(!flags.GetBool("met.Aumi")){label="Find Lanternwick's lantern-keeper on the lantern stair";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Aumi");}
                    else if(flags.GetBool("lanternwick.chartGiven")){label="Launch for home: the Wick Reach and Tallow";target=prompts.OfType<DockingPad>().FirstOrDefault();}
                    else if(flags.GetBool("bossdown.HessaKorl")){label="Tell Keeper Aumi the gorge is clear";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Aumi");}
                    else if(flags.GetBool("gorge.relaysLit")){label="Take the orchard gate back to the Sunken Relay: the scavengers are still inside";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.scene=="LanternGorge");}
                    else{label="Take the orchard gate down to Lantern Gorge: Aumi wants its relays lit";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.scene=="LanternGorge");}
                    break;
                case "LanternGorge":
                {
                    int lit=new[]{"gorge.relay1","gorge.relay2","gorge.relay3"}.Count(f=>flags.GetBool(f));
                    if(flags.GetBool("bossdown.HessaKorl")){label=flags.GetBool("lanternwick.chartGiven")?"Climb back to Lanternwick":"Climb back to Lanternwick and tell Keeper Aumi";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.scene=="Lanternwick");}
                    else if(lit<3)
                    {
                        label=$"Relight the gorge relays ({lit}/3) for Keeper Aumi";
                        target=prompts.OfType<DiscoveryPoint>().Where(p=>p.flag.StartsWith("gorge.relay")&&p.Available).OrderBy(p=>(p.transform.position-PartyController.Current.Active.transform.position).sqrMagnitude).FirstOrDefault();
                    }
                    else{label="Enter the Sunken Relay: the scavengers are still inside";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.scene=="SunkenRelay");}
                    break;
                }
                case "SunkenRelay":
                    if(flags.GetBool("bossdown.HessaKorl")&&!flags.GetBool("relay.hessaYielded")){label="Hear Hessa out";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Hessa");}
                    else if(flags.GetBool("bossdown.HessaKorl")){label=flags.GetBool("lanternwick.chartGiven")?"Ride the relay lift up to Lanternwick":"Ride the relay lift up to Lanternwick and tell Keeper Aumi";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.scene=="Lanternwick");}
                    else if(flags.GetBool("relay.hessaConfronted")){label="Stop Hessa Korl before she strips the relay";var boss=BossController.Focus(PartyController.Current.Active.transform.position,200);goal=boss!=null?boss.transform.position:new Vector3(3,0,129);}
                    else{label="Find who is stripping the relay";target=prompts.OfType<DiscoveryPoint>().FirstOrDefault(p=>p.flag=="relay.hessaConfronted");}
                    break;
                case "Arena_Ground":case "Arena_Flight":label="Practice attacks, dodges and swaps";break;
                default:
                    // A zone with no lines written for it: the open story quest's step (the latest chapter's), or with none open, the last one's closing word,
                    // or the place's name. It used to say the yard's 'Practice attacks, dodges and swaps' anywhere nobody had written a case for.
                    written=false;if(story==null)story=StoryQuests.Open(state,out storyStep);
                    if(story!=null){label=QuestText.Step(story,storyStep,state);Aim(story,storyStep,state,hero,ref target,ref goal);}
                    else
                    {
                        label=StoryQuests.Closing(state)??DisplayNames.Zone(state.zone);
                        // Between chapters the told chapter says where the next begins (QuestDef.next): the way toward it from here, and there the one to see or the place to stand.
                        // The first player run read "Chapter 4 complete. Many Hands is waiting" on a bridge in Driftreed with no arrow, two zones and a launch from where chapter 5 starts.
                        if(StoryQuests.Between(state,out var next))Aim(next,null,state,hero,ref target,ref goal);
                        // Keeper Abri has the next word aboard Tallow (her last on the chapter told, then her offer), so the arrow finds her where she stands in this zone.
                        if(target==null&&!goal.HasValue&&Lattice.World.SideStories.KeeperOpensAChapter(state))target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Keeper");
                    }
                    // A zone's own mechanism, while it says something the steps can't (World's ZoneLine: chapter 9's dawn line), and where its arrow points.
                    if(Lattice.World.ZoneLine.Read(out var said,out var there)){if(!string.IsNullOrEmpty(said))label=said;if(there.HasValue){target=null;goal=there;}}
                    break;
            }
            if(target!=null)goal=target.transform.position;
            // What the arrow points at is what a press beside it should answer (PromptService.Wanted): the plan at Many Hands' chart table stands on the very spot of the
            // table's own chart, and which of the two a press opened was down to the order the scene happened to hold them in.
            PromptService.Wanted=target;
            if(fight!=null){label=FightGoal(fight,label);goal=null;}
            // Chapter 2's homecoming used to end every line here with 'Xylos's river towns are next, but their route isn't charted yet' and no arrow: true while chapter 2
            // was the last one built, and a dead end once chapter 3 stood behind Abri's next talk. Each zone's own line now leads home to her (Tallow Drift's above).
            // No 'Here' for them: the arrow holds until the revive has started, or it stops a step short of the reach and nothing happens.
            var down=fight==null?Downed(party):null;GoalIsHero=down!=null;hereAt=down!=null?0:Here;
            if(down!=null){label=$"Get {DisplayNames.Speaker(down.character)} back up";goal=party.ReviveTarget==down?null:down.transform.position;}
            static int Lantern(GameState state)=>state.questSteps.TryGetValue("LanternRoad",out var step)?step:-1;
            string direction="";goalAt=goal;
            if(goal.HasValue)
            {
                var delta=goal.Value-hero;delta.y=0;
                direction=delta.sqrMagnitude<hereAt*hereAt?"\nHere":$"\n{ArrowRoom}{delta.magnitude:0} m";
            }
            string words=label+direction+Also(state,hero);
            if(text.text==words)return;
            text.text=words;SeatArrows(label.Length+1+ArrowRoom.Length);
            // By its lines, not its letters: a tail on a new word mustn't make the frame breathe, and only a change of words can move it.
            float high=Mathf.Max(FrameLeast,text.textInfo.lineCount*LinePitch+FrameAir);if(Frame.sizeDelta.y!=high)Frame.sizeDelta=new(Frame.sizeDelta.x,high);
        }
        /// <summary>The nearest thing an active side quest wants in this zone, as a dim line under the main objective, with its distance.
        /// The den, the crate and the plates were otherwise found from the log's words alone. A boss fight has the player's whole attention.</summary>
        string Also(GameState state,Vector3 from)
        {
            alsoAt=null;
            if(BossController.Focus(from,45)!=null)return "";
            QuestDef found=null;int step=0;float best=float.MaxValue;Vector3 at=default;
            foreach(var pair in state.questSteps)
            {
                if(System.Array.IndexOf(PauseMenu.MainQuests,pair.Key)>=0)continue;
                var quest=GameCatalog.Find<QuestDef>(pair.Key);if(quest==null||pair.Value<0||pair.Value>=quest.steps.Length)continue;
                foreach(var prompt in InteractionPrompt.Active)
                {
                    if(!Wants(quest,pair.Value,prompt))continue;
                    var gap=prompt.transform.position-from;gap.y=0;
                    if(gap.sqrMagnitude<best){best=gap.sqrMagnitude;found=quest;step=pair.Value;at=prompt.transform.position;}
                }
            }
            if(found==null)return "";
            alsoAt=at;
            return $"\n<color=#{ColorUtility.ToHtmlStringRGB(UiKit.DimTextColor)}>Also: {QuestText.Brief(found,step,state)}  ·  {Mathf.Sqrt(best):0} m</color>";
        }
        static bool Wants(QuestDef quest,int step,InteractionPrompt prompt)=>prompt switch
        {
            DiscoveryPoint point=>point.requiredQuest==quest.id&&point.requiredStep==step&&point.Wanted,
            MineNode node=>quest.steps[step].kind==ObjectiveKind.Collect&&node.material==quest.steps[step].target,
            Npc npc=>QuestText.WaitsOn(quest,step)==npc.speaker,
            _=>false,
        };
    }
}
