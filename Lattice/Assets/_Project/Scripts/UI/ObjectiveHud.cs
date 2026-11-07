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
    public sealed class ObjectiveHud:MonoBehaviour
    {
        /// <summary>What the panel asks of the player now, without its distance line: the quest log falls back on it.</summary>
        public static string CurrentLabel{get{var hud=FindFirstObjectByType<ObjectiveHud>();return hud!=null&&hud.text!=null?hud.text.text.Split('\n')[0]:"";}}
        TMP_Text text;float next;Canvas canvas;bool inFight;
        Image arrow,alsoArrow;Vector3? goalAt,alsoAt;
        /// <summary>Nearer than this the goal is simply here: a distance and a direction mean nothing.</summary>
        const float Here=3;
        /// <summary>Room at the head of the distance line for the arrow.</summary>
        const string ArrowRoom="<space=34>";
        /// <summary>What floating numbers keep clear of.</summary>
        public RectTransform Frame{get;private set;}
        public PanelFade Fade{get;private set;}
        void Start()
        {
            canvas=UiKit.CreateCanvas("Objective",6,transform);var frame=UiKit.DarkFrame(canvas.transform,"ObjectiveFrame");frame.raycastTarget=false;
            // At the weight of the card under it, and of the vitals in the other corner. The full border left this frame room for three lines of type, and it can need four.
            frame.pixelsPerUnitMultiplier=2;
            Frame=UiKit.Rect(frame.gameObject,new(1,1),new(1,1),new(-300,-175),new(570,165));
            var group=frame.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;Fade=new PanelFade(Frame,frame,group);
            text=UiKit.Text(frame.transform,"Objective","",24,UiKit.TextColor,TextAlignmentOptions.MidlineLeft);
            UiKit.Rect(text.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(480,112));
            arrow=Pointer("ObjectiveArrow",26,UiKit.GoldColor);alsoArrow=Pointer("ObjectiveAlsoArrow",20,UiKit.DimTextColor);
        }
        Image Pointer(string name,float size,Color color)
        {
            var image=UiKit.Panel(text.transform,name,color);image.sprite=UiSkin.Pointer();image.preserveAspect=true;image.raycastTarget=false;image.enabled=false;
            UiKit.Rect(image.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(size,size));return image;
        }
        /// <summary>The camera is fixed, so the way to go is a way on the screen, and that's what the arrow shows. The word was the target's bearing from the
        /// camera ("behind" was down-screen), and read as wrong to anyone flying straight at it.</summary>
        static void Point(Image image,Vector3? goal,Vector3 hero)
        {
            bool on=false;var camera=Camera.main;
            if(goal.HasValue&&camera!=null)
            {
                var gap=goal.Value-hero;gap.y=0;
                if(gap.sqrMagnitude>=Here*Here)
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
                arrow.rectTransform.anchoredPosition=new Vector2(c.origin-20,c.baseLine+middle);break;
            }
            for(int i=info.characterCount-1;i>=0;i--)
            {
                var c=info.characterInfo[i];if(!c.isVisible)continue;
                alsoArrow.rectTransform.anchoredPosition=new Vector2(c.xAdvance+16,c.baseLine+middle);break;
            }
        }
        /// <summary>With the bar up the boss is the objective, not the fixed spot the walk there pointed at. The Cantor's lines and Hessa's already name the fight.</summary>
        static string FightGoal(BossController boss,string label)=>boss.Health.id switch
        {
            "Burrower"=>"Stop the Burrower",
            "BellowsBelow"=>PressureOrgan.AnyPumping?"Break the pressure organs bracing the Bellows":"Bring down the Bellows",
            _=>label,
        };
        void Update()
        {
            canvas.enabled=!GameInput.Current.Blocked;
            var party=PartyController.Current;if(party==null||party.members==null)return;
            var hero=party.Active.transform.position;
            // A bar going up or a boss falling changes what the panel should say now, not at its next look: it said 'Release the collar lock' over the rewards.
            var fight=BossController.Focus(hero);if((fight!=null)!=inFight){inFight=fight!=null;next=0;}
            Point(arrow,goalAt,hero);Point(alsoArrow,alsoAt,hero);
            if(Time.unscaledTime<next)return;next=Time.unscaledTime+.25f;
            var state=GameServices.Current.State;var flags=GameServices.Current.Flags;Vector3? goal=null;string label="";
            var prompts=InteractionPrompt.Active;
            InteractionPrompt target=null;
            switch(state.zone)
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
                    else{label="Warp to Tallow Drift: the way past the Cantor is open";target=prompts.OfType<WarpBeacon>().FirstOrDefault();}
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
                    else if(!flags.GetBool("sliceComplete")){label="Repair and save at Tallow Drift, then see to Abri's line";target=prompts.OfType<RepairBay>().FirstOrDefault();}
                    else if(!flags.GetBool("tallow.lineObserved")){label="Launch to inspect the port docking line for Keeper Abri";target=prompts.OfType<DockingPad>().FirstOrDefault();}
                    else if(!flags.GetBool("compact.hailHeard")){label="The beacon band is crackling: see Keeper Abri";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Keeper");}
                    else if(Lantern(state)<0&&!flags.GetBool("lanternwick.chartGiven")){label="Ask Keeper Abri about a route that waits";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Keeper");}
                    else if(flags.GetBool("lanternwick.chartGiven")&&!flags.GetBool("chapter2.homecoming")){label="Bring Lanternwick's chart to Keeper Abri";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Keeper");}
                    else{label="Launch for the Wick Reach warp";target=prompts.OfType<DockingPad>().FirstOrDefault();}
                    break;
                case "WickReach":
                {
                    var relays=prompts.OfType<DiscoveryPoint>().Where(p=>p.flag.StartsWith("wick.relay")&&p.Available).ToArray();
                    if(flags.GetBool("lanternwick.chartGiven")){label="Warp home to Tallow Drift";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.scene=="TallowApproach");}
                    else if(!flags.GetBool("wick.gateOpen")&&relays.Length>0)
                    {label=$"Relight the lantern relays to open the way ({2-relays.Length}/2)";target=relays.OrderBy(p=>(p.transform.position-PartyController.Current.Active.transform.position).sqrMagnitude).First();}
                    else{label="Dock at Lanternwick";target=prompts.OfType<DockingPad>().FirstOrDefault();}
                    break;
                }
                case "Lanternwick":
                    if(!flags.GetBool("met.Aumi")){label="Find Lanternwick's lantern-keeper on the lantern stair";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Aumi");}
                    else if(flags.GetBool("lanternwick.chartGiven")){label="Launch for home: the Wick Reach and Tallow";target=prompts.OfType<DockingPad>().FirstOrDefault();}
                    else if(flags.GetBool("bossdown.HessaKorl")){label="Tell Keeper Aumi the gorge is clear";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Aumi");}
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
                default:label="Practice attacks, dodges and swaps";break;
            }
            if(target!=null)goal=target.transform.position;
            if(fight!=null){label=FightGoal(fight,label);goal=null;}
            if(flags.GetBool("chapter2.homecoming")){label="Chapter 2 complete. Xylos's river towns are next, but their route isn't charted yet.";goal=null;}
            static int Lantern(GameState state)=>state.questSteps.TryGetValue("LanternRoad",out var step)?step:-1;
            string direction="";goalAt=goal;
            if(goal.HasValue)
            {
                var delta=goal.Value-hero;delta.y=0;
                direction=delta.sqrMagnitude<Here*Here?"\nHere":$"\n{ArrowRoom}{delta.magnitude:0} m";
            }
            string words=label+direction+Also(state,hero);
            if(text.text!=words){text.text=words;SeatArrows(label.Length+1+ArrowRoom.Length);}
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
