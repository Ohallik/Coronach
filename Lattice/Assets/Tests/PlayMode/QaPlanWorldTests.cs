using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Yarn.Unity;

namespace Lattice.Tests.PlayMode
{
    /// <summary>TEST_PLAN sections 4.5, 4.6 and 4.7 as a tester works through them: every zone's arrival, exits, locks, floor and props;
    /// the words a player reads; and all ten quests at every step. These are exploratory checks, written from the plan and what a
    /// player would see rather than from the code. A failure here is a finding until someone has looked at it.</summary>
    public sealed class QaPlanWorldTests
    {
        static readonly string[] Zones={"Hub_Decks","Hub_CinderHalo","Sorrel_Ridges","Hushwell","Gullet_Tunnel","TallowApproach","TallowDrift","WickReach","Lanternwick","LanternGorge","SunkenRelay"};
        string heard;readonly List<string> problems=new(),log=new();
        void Listen(string node,string speaker,System.Action<bool> done)=>heard=node;
        void Logged(string text,string stack,LogType type){if(text.StartsWith("RELAY_LIT"))log.Add(text);}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");heard=null;problems.Clear();log.Clear();
            Npc.TalkRequested+=Listen;Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Npc.TalkRequested-=Listen;Application.logMessageReceived-=Logged;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ---------------------------------------------------------------- helpers
        static GameState State=>GameServices.Current.State;
        static SaveSystem Saves=>GameServices.Current.Saves;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static int Step(string quest)=>State.questSteps.TryGetValue(quest,out var step)?step:-1;
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        static float FlatDistance(Vector3 a,Vector3 b)=>Flat(a-b).magnitude;
        static void Quiet()
        {
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
        }
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            Quiet();
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Prepared,zone+": the conversations never finished preparing");
            yield return new WaitForSecondsRealtime(.4f);
        }
        /// <summary>The player's way back in: the autosave as it stands, the title, and Continue.</summary>
        static IEnumerator SaveQuitAndContinue()
        {
            string zone=State.zone;Saves.Save("autosave",State);
            SceneFlow.Current.LoadZone("Title");float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            yield return null;yield return null;
            var title=Object.FindFirstObjectByType<TitleScreen>();Assert.IsNotNull(title,"fixture: the title never came up");
            title.StartGame(true);yield return null;
            deadline=Time.unscaledTime+25;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"Continue never finished loading");
            Assert.AreEqual(zone,SceneFlow.Current.Zone,"Continue lands in a different zone from the one the save was made in");
            Quiet();
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.4f);
        }
        static void Put(CombatActor hero,Vector3 at)
        {
            var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.position=at;cc.enabled=true;Physics.SyncTransforms();
        }
        static Npc Person(string speaker)=>All<Npc>().Single(n=>n.speaker==speaker);
        static DiscoveryPoint Point(string flag)=>All<DiscoveryPoint>().Single(d=>d.flag==flag);
        static WarpBeacon WarpTo(string scene)=>All<WarpBeacon>().Single(w=>w.scene==scene);
        static string ObjectiveText()=>Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Objective").text;
        static void ClearFights(){foreach(var fight in All<EncounterVolume>())Flag("clear."+fight.encounterId);}
        static void OpenSeals(){foreach(var seal in All<Membrane>())seal.SetOpen(true);}
        static bool IsBody(Collider c)=>c is CharacterController||c.GetComponentInParent<CombatActor>()!=null||c.GetComponentInParent<EnemyBrain>()!=null;
        void Verdict(string heading)
        {
            string shown=string.Join("\n",problems.Distinct().Take(40));
            Assert.IsEmpty(problems,heading+":\n"+shown+(problems.Distinct().Count()>40?"\n...and more":""));
        }
        IEnumerator Talk(Npc npc)
        {
            heard=null;DialogueSystem.Current.AutoAdvance=true;npc.Interact();yield return null;
            Assert.IsNotNull(heard,npc.speaker+" doesn't answer when talked to");
            float deadline=Time.unscaledTime+25;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(DialogueSystem.Current.Running,npc.speaker+"'s conversation never ends");
            yield return null;yield return null;
            var shop=Object.FindFirstObjectByType<ShopUi>();if(shop!=null&&shop.IsOpen)shop.Close();
            yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Discover(DiscoveryPoint point)
        {
            DialogueSystem.Current.AutoAdvance=true;point.Interact();yield return null;
            float deadline=Time.unscaledTime+25;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.25f);
        }
        static void RoadToAumi()
        {
            Flag("compact.hailHeard");Flag("lantern.routeKnown");var quests=RpgServices.Quests;quests.Start("LanternRoad");
            quests.Report(ObjectiveKind.Reach,"WickReach");quests.Report(ObjectiveKind.Reach,"Lanternwick");
            Flag("met.Aumi");quests.Report(ObjectiveKind.TalkTo,"Aumi");
            Assert.AreEqual(3,Step("LanternRoad"),"fixture: the road has reached Aumi");
        }
        /// <summary>What doing one step's work leaves behind in a save, as far as the quest can see.</summary>
        static void Deed(Objective goal,int amount)
        {
            var flags=GameServices.Current.Flags;var quests=RpgServices.Quests;
            switch(goal.kind)
            {
                case ObjectiveKind.TalkTo:flags.SetBool("met."+goal.target,true);quests.Report(goal.kind,goal.target);break;
                case ObjectiveKind.Reach:State.zone=goal.target;quests.Report(goal.kind,goal.target);break;
                case ObjectiveKind.Kill:flags.SetBool("bossdown."+goal.target,true);quests.Report(goal.kind,goal.target);break;
                case ObjectiveKind.Collect:RpgServices.Inventory.Give(goal.target,amount);break;
                case ObjectiveKind.Interact:quests.Report(goal.kind,goal.target,amount);break;
                case ObjectiveKind.Flag:flags.SetBool(goal.target,true);quests.Report(goal.kind,goal.target);break;
            }
        }
        static void ForgetGathered(QuestDef quest)
        {
            foreach(var goal in quest.steps)if(goal.kind==ObjectiveKind.Collect){State.materials.Remove(goal.target);State.collectedMaterials.Remove(goal.target);}
        }
        static int Held(string id)
        {
            State.consumables.TryGetValue(id,out int item);State.materials.TryGetValue(id,out int material);
            return item+material+State.parts.Count(p=>p.definitionId==id);
        }
        static int Worth=>State.party[0].xp+State.party[0].level*100000;

        // ---------------------------------------------------------------- the script, as source and as compiled
        static string ScriptRoot=>Path.Combine(Application.dataPath,"_Project/Resources/Dialogue");
        static string ScriptText()=>string.Concat(Directory.GetFiles(ScriptRoot,"*.yarn").OrderBy(f=>f).Select(File.ReadAllText)).Replace("\r\n","\n");
        static string Body(string node)
        {
            string all=ScriptText();int at=all.IndexOf("title: "+node+"\n",System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(at,0,"the script has no "+node);
            return all.Substring(at,all.IndexOf("\n===",at,System.StringComparison.Ordinal)-at);
        }
        static IEnumerable<(string speaker,string text,PortraitEmotion emotion)> LinesOf(string node)
        {
            foreach(string raw in Body(node).Split('\n'))
            {
                var line=Regex.Match(raw.Trim(),@"^([A-Za-z]+):\s*(.*?)\s*#emotion:(\w+)\s*$");
                if(line.Success&&line.Groups[1].Value!="title")yield return (line.Groups[1].Value,line.Groups[2].Value,System.Enum.Parse<PortraitEmotion>(line.Groups[3].Value,true));
            }
        }

        // ---------------------------------------------------------------- what counts as a raw id, and as the wrong word
        static readonly Regex RawId=new(@"\b[A-Za-z]+_[A-Za-z0-9_]+\b|\b[a-z]+\.[a-z][A-Za-z0-9]*\b|\b[a-z]*[A-Z][a-z]+[A-Z][A-Za-z0-9]*\b|\b(?:Edges|Emitters|Frames|Drives|Modules)T\d\b|#emotion|<<|>>|\bTODO\b|\bTBD\b|\bFIXME\b|(?i:lorem ipsum)|(?i:placeholder)");
        static readonly (Regex pattern,string rule)[] WordsEverywhere=
        {
            (new Regex(@"(?i)\bwarp[- ]?key\b"),"it's the survey key"),(new Regex(@"(?i)\bsorrel key\b"),"it's the survey key"),
            (new Regex(@"\bSurvivor\b"),"the foreman is Dace"),(new Regex(@"(?i)\b(?:outer|warp) beacon\b"),"the outer warp is a warp"),
            (new Regex(@"(?<![Tt]he )\bWick Reach\b"),"it's the Wick Reach"),
        };
        /// <summary>Labels, cards, the log and the item and skill lists: the game's own voice. A character may say "Abri" or "the charge".</summary>
        static readonly (Regex pattern,string rule)[] WordsOnLabels=
        {
            (new Regex(@"(?<!Keeper )\bAbri\b"),"every label calls her Keeper Abri"),(new Regex(@"(?<!-)\bKeeper\b(?! (?:Abri|Aumi))"),"a keeper on a label has a name"),
            (new Regex(@"\bcharge\b"),"the resource is Charge"),(new Regex(@"\b[Rr]epair gel\b"),"the item is Repair Gel"),
            (new Regex(@"\b[Cc]harge cell\b"),"the item is Charge Cell"),(new Regex(@"\b[Rr]idge crystal\b"),"the item is Ridge Crystal"),
            (new Regex(@"\b[Ss]crap alloy\b"),"the item is Scrap Alloy"),(new Regex(@"\bHP\b|\b[Hh]ealth\b"),"the stat is Integrity"),
        };
        void CheckWords(string where,string text,bool label)
        {
            if(string.IsNullOrEmpty(text))return;
            string plain=Regex.Replace(text,"<[^>]+>","");
            var raw=RawId.Match(plain);if(raw.Success)problems.Add($"{where}: '{raw.Value}' reaches the screen as a raw id: \"{plain}\"");
            foreach(var (pattern,rule) in WordsEverywhere)if(pattern.IsMatch(plain))problems.Add($"{where}: {rule}: \"{plain}\"");
            if(label)foreach(var (pattern,rule) in WordsOnLabels)if(pattern.IsMatch(plain))problems.Add($"{where}: {rule}: \"{plain}\"");
        }

        // ================================================================ 4.5 arrival: the objective names the next step, with distance and direction
        static UnityEngine.UI.Image ObjectiveArrow()=>Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<UnityEngine.UI.Image>(true).Single(i=>i.name=="ObjectiveArrow");
        static Vector3? At<T>(System.Func<T,bool> pick)where T:Component
        {var found=All<T>().FirstOrDefault(pick);return found!=null?found.transform.position:(Vector3?)null;}
        static Vector3? Nearest<T>(System.Func<T,bool> pick)where T:Component
        {
            var hero=PartyController.Current.Active.transform.position;
            var found=All<T>().Where(pick).OrderBy(c=>(c.transform.position-hero).sqrMagnitude).FirstOrDefault();
            return found!=null?found.transform.position:(Vector3?)null;
        }
        IEnumerator Says(string word,System.Func<Vector3?> where)
        {
            yield return new WaitForSecondsRealtime(.35f);
            string zone=State.zone,text=ObjectiveText(),label=text.Split('\n')[0];
            if(!label.Contains(word))problems.Add($"{zone}: the objective should be about '{word}' and reads \"{label}\"");
            CheckWords(zone+" objective",label,true);
            // The direction is an arrow on the panel, and "Here" under 3 m has no need of one.
            string line=Regex.Replace(text,"<[^>]+>","").Split('\n').Skip(1).FirstOrDefault()??"";
            var found=Regex.Match(line,@"^\s*(\d+) m\b");bool here=line.Trim()=="Here";var arrow=ObjectiveArrow();
            if(!found.Success&&!here){problems.Add($"{zone}: \"{label}\" gives no distance and direction");yield break;}
            if(here==arrow.enabled){problems.Add($"{zone}: \"{label}\" reads \"{line.Trim()}\" with its arrow {(arrow.enabled?"pointing":"off")}");yield break;}
            if(where==null)yield break;
            var goal=where();
            if(!goal.HasValue){problems.Add($"{zone}: fixture: nothing here to compare \"{label}\" against");yield break;}
            var hero=PartyController.Current.Active.transform.position;
            float real=FlatDistance(goal.Value,hero);
            if(here){if(real>3.5f)problems.Add($"{zone}: \"{label}\" says Here and what it names is {real:0} m away");yield break;}
            int said=int.Parse(found.Groups[1].Value);
            if(Mathf.Abs(real-said)>2.5f)problems.Add($"{zone}: \"{label}\" says {said} m and what it names is {real:0} m away");
            var view=Camera.main;Vector2 way=(Vector2)view.WorldToScreenPoint(hero+Flat(goal.Value-hero).normalized*2)-(Vector2)view.WorldToScreenPoint(hero);
            Vector2 points=arrow.rectTransform.rotation*Vector3.up;
            if(Vector2.Angle(way,points)>10)problems.Add($"{zone}: \"{label}\" points its arrow {points.normalized} and on screen what it names lies toward {way.normalized}");
        }
        [UnityTest] public IEnumerator EveryChapterOneArrivalNamesTheNextStepWithDistanceAndDirection()
        {
            yield return Zone("Hub_CinderHalo");
            yield return Says("Orrin",()=>At<DockingPad>(d=>d.spawn=="Office"));
            yield return Zone("Hub_Decks","Office");
            yield return Says("Orrin",()=>At<Npc>(n=>n.speaker=="Orrin"));
            Flag("met.Orrin");
            yield return Says("Halo",()=>Nearest<DockingPad>(_=>true));
            yield return Zone("Hub_CinderHalo","Office");
            yield return Says("Sorrel",()=>At<WarpBeacon>(w=>w.scene=="Sorrel_Ridges"));
            yield return Zone("Sorrel_Ridges");
            yield return Says("outpost crew",()=>At<Npc>(n=>n.speaker=="Survivor"));
            Flag("met.Survivor");
            yield return Says("drill",()=>At<EncounterVolume>(e=>e.encounterId=="Sorrel_Burrower"));
            Flag("bossdown.Burrower");Flag("clear.Sorrel_Burrower");
            yield return Says("survey key",()=>At<KeyPickup>(_=>true));
            Flag("warpkey");
            yield return Says("Hushwell",()=>At<WarpBeacon>(w=>w.scene=="Hushwell"));
            yield return Zone("Hushwell");
            yield return Says("Bellows",()=>new Vector3(HushwellLayout.Bellows.x,HushwellLayout.Middle,HushwellLayout.Bellows.y));
            Flag("bossdown.BellowsBelow");Flag("clear.Hushwell_Bellows");
            yield return Says("guarding",()=>At<DiscoveryPoint>(d=>d.flag=="hushwell.nursery"));
            Flag("hushwell.nursery");
            yield return Says("lift",()=>At<WarpBeacon>(w=>w.requiredFlag=="hushwell.nursery"));
            yield return Zone("Sorrel_Ridges","Hushwell");
            yield return Says("Halo",()=>At<DockingPad>(d=>d.spawn=="Outer"));
            yield return Zone("Hub_CinderHalo","Outer");
            yield return Says("Gullet",()=>At<WarpBeacon>(w=>w.scene=="Gullet_Tunnel"));
            yield return Zone("Gullet_Tunnel");
            yield return Says("ollar",null);
            Flag("bossdown.Cantor");Flag("clear.Gullet_Cantor");
            yield return Says("Tallow",()=>At<WarpBeacon>(w=>w.scene=="TallowApproach"));
            yield return Zone("TallowApproach");
            yield return Says("Dock at Tallow",()=>At<DockingPad>(_=>true));
            yield return Zone("TallowDrift");
            yield return Says("keeper",()=>At<Npc>(n=>n.speaker=="Keeper"));
            Flag("met.Keeper");
            yield return Says("Repair",()=>At<RepairBay>(_=>true));
            Flag("sliceComplete");
            yield return Says("port docking line",()=>At<DockingPad>(_=>true));
            Verdict("Arriving in chapter 1, the objective panel lets the player down");
        }
        [UnityTest] public IEnumerator EveryChapterTwoArrivalNamesTheNextStepWithDistanceAndDirection()
        {
            foreach(var f in new[]{"met.Orrin","warpkey","hushwell.nursery","bossdown.Cantor","met.Keeper","sliceComplete","tallow.lineObserved","tallow.driftDiscussed","compact.hailHeard"})Flag(f);
            yield return Zone("TallowDrift");
            yield return Says("route that waits",()=>At<Npc>(n=>n.speaker=="Keeper"));
            Flag("lantern.routeKnown");RpgServices.Quests.Start("LanternRoad");
            yield return Says("Wick Reach",()=>At<DockingPad>(_=>true));
            yield return Zone("TallowApproach","Dock");
            yield return Says("Wick Reach",()=>At<WarpBeacon>(w=>w.scene=="WickReach"));
            yield return Zone("WickReach");
            yield return Says("(0/2)",()=>Nearest<DiscoveryPoint>(p=>p.flag.StartsWith("wick.relay")));
            Flag("wick.relay1");Flag("wick.relay2");
            yield return new WaitForSecondsRealtime(.2f);
            yield return Says("Dock at Lanternwick",()=>At<DockingPad>(_=>true));
            yield return Zone("Lanternwick");
            yield return Says("lantern-keeper",()=>At<Npc>(n=>n.speaker=="Aumi"));
            Flag("met.Aumi");RpgServices.Quests.Report(ObjectiveKind.TalkTo,"Aumi");
            yield return Says("orchard gate",()=>At<WarpBeacon>(w=>w.scene=="LanternGorge"));
            yield return Zone("LanternGorge");
            yield return Says("(0/3)",()=>Nearest<DiscoveryPoint>(p=>p.flag.StartsWith("gorge.relay")));
            foreach(var f in new[]{"gorge.relay1","gorge.relay2","gorge.relay3"})Flag(f);
            yield return new WaitForSecondsRealtime(.2f);
            yield return Says("Sunken Relay",()=>At<WarpBeacon>(w=>w.scene=="SunkenRelay"));
            yield return Zone("SunkenRelay");
            yield return Says("stripping",()=>At<DiscoveryPoint>(d=>d.flag=="relay.hessaConfronted"));
            Flag("relay.hessaConfronted");
            yield return Says("Hessa",()=>At<EnemyBrain>(e=>e.definition.id=="HessaKorl"));
            Flag("bossdown.HessaKorl");Flag("clear.Relay_Hessa");
            yield return Zone("SunkenRelay");
            yield return Says("Hessa",()=>At<Npc>(n=>n.speaker=="Hessa"));
            Flag("relay.hessaYielded");
            yield return Says("lift",()=>At<WarpBeacon>(w=>w.scene=="Lanternwick"));
            yield return Zone("Lanternwick","Archive");
            yield return Says("Aumi",()=>At<Npc>(n=>n.speaker=="Aumi"));
            Flag("lanternwick.chartGiven");
            yield return Says("home",()=>At<DockingPad>(_=>true));
            yield return Zone("WickReach","Lanternwick");
            yield return Says("home",()=>At<WarpBeacon>(w=>w.scene=="TallowApproach"));
            yield return Zone("TallowApproach","Wick");
            yield return Says("Dock at Tallow",()=>At<DockingPad>(_=>true));
            yield return Zone("TallowDrift");
            yield return Says("chart",()=>At<Npc>(n=>n.speaker=="Keeper"));
            Verdict("Arriving in chapter 2, the objective panel lets the player down");
        }
        [UnityTest] public IEnumerator TheTownStopsAskingForRelaysOnceTheyBurn()
        {
            RoadToAumi();foreach(var f in new[]{"gorge.relay1","gorge.relay2","gorge.relay3","gorge.relaysLit"})Flag(f);
            yield return Zone("Lanternwick");yield return new WaitForSecondsRealtime(.35f);
            StringAssert.DoesNotContain("relays lit",ObjectiveText(),"all three relays burn and, back in town, the objective still says Aumi wants them lit");
        }
        [UnityTest] public IEnumerator NoGroundArrivalOpensOnACloseUp()
        {
            Flag("relay.hessaConfronted");
            foreach(var zone in new[]{"Hub_Decks","Sorrel_Ridges","Hushwell","TallowDrift","Lanternwick","LanternGorge","SunkenRelay"})
            {
                yield return Zone(zone);ClearFights();
                var rig=Object.FindFirstObjectByType<CameraRig>();var lead=PartyController.Current.Active;
                foreach(var spawn in All<SpawnPoint>().GroupBy(s=>s.id).Select(g=>g.First()).ToArray())
                {
                    Put(lead,spawn.transform.position+Vector3.up*.1f);
                    yield return new WaitForSecondsRealtime(1.2f);
                    if(rig.GroundCameraDistance<=14)problems.Add($"{zone}, arriving at {spawn.id}: the camera opens {rig.GroundCameraDistance:0.0} m from the hero, jammed against the scenery");
                    var view=Camera.main.WorldToViewportPoint(lead.transform.position+Vector3.up);
                    if(view.z<=0||view.x<0||view.x>1||view.y<0||view.y>1)problems.Add($"{zone}, arriving at {spawn.id}: the hero isn't on screen ({view})");
                }
            }
            Verdict("Arrivals that open badly");
        }

        // ================================================================ 4.5 exits: every dock and warp leads where its label says
        static bool LabelNames(string label,string scene,string spawn)=>scene switch
        {
            "Hub_CinderHalo"=>label.Contains("Halo"),
            "Hub_Decks"=>spawn switch{"Office"=>label.Contains("Dock Office"),"Shop"=>label.Contains("Market"),"Repair"=>label.Contains("Repair"),_=>false},
            "Sorrel_Ridges"=>label.Contains("Sorrel"),"Hushwell"=>label.Contains("Hushwell"),"Gullet_Tunnel"=>label.Contains("Gullet"),
            "TallowApproach"=>label.Contains("Tallow"),"TallowDrift"=>label.Contains("Tallow Drift"),"WickReach"=>label.Contains("Wick Reach"),
            "Lanternwick"=>label.Contains("Lanternwick"),"LanternGorge"=>label.Contains("Lantern Gorge"),"SunkenRelay"=>label.Contains("Sunken Relay"),
            _=>false,
        };
        static bool FloorUnder(Vector3 at,float above,float below,out float floor)
        {
            floor=float.NegativeInfinity;bool found=false;
            foreach(var hit in Physics.RaycastAll(at+Vector3.up*above,Vector3.down,above+below,~0,QueryTriggerInteraction.Ignore))
            {
                if(IsBody(hit.collider)||hit.point.y<=floor)continue;
                floor=hit.point.y;found=true;
            }
            return found;
        }
        static readonly Collider[] Touching=new Collider[32];
        /// <summary>Whether a standing body fits with its feet here: what stops a walker, in a form Physics can answer quickly.</summary>
        static bool BodyFits(Vector3 feet,float radius=.33f)
        {
            int count=Physics.OverlapCapsuleNonAlloc(feet+Vector3.up*.47f,feet+Vector3.up*1.55f,radius,Touching,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(!IsBody(Touching[i]))return false;
            return true;
        }
        void CheckStanding(string zone,string spawn,Vector3 at,bool flight,string who)
        {
            if(flight)
            {
                float hull=Mathf.Max(HeroCollision.HullRadius("Taren"),HeroCollision.HullRadius("Sela"));
                if(Physics.OverlapSphere(at+Vector3.up*.8f,hull,~0,QueryTriggerInteraction.Ignore).Any(c=>!IsBody(c)))problems.Add($"{zone}: {who} arrives at {spawn} with a hull inside something solid");
                return;
            }
            if(!FloorUnder(at,3,6,out float floor)){problems.Add($"{zone}: {who} arrives at {spawn} over no floor at all");return;}
            // The companion lands a fixed 2 m beside the lead, wherever that is, and a controller pushes itself out of a graze on its first move, a nudge of under 20 cm as the fade ends.
            // She counts as inside something only if the middle of her body is. An authored spawn gets no such allowance.
            if(!BodyFits(new Vector3(at.x,floor,at.z),who=="the companion"?.2f:.33f))problems.Add($"{zone}: {who} arrives at {spawn} inside something solid");
            if(!NavMesh.SamplePosition(new Vector3(at.x,floor,at.z),out _,1.5f,NavMesh.AllAreas))problems.Add($"{zone}: {who} arrives at {spawn} off the walkable floor, where the companion can't find a path");
        }
        [UnityTest] public IEnumerator EveryExitNamesWhereItGoesAndItsArrivalIsThere()
        {
            var exits=new List<(string from,string label,string scene,string spawn)>();var arrivals=new Dictionary<string,HashSet<string>>();var spawnAt=new Dictionary<string,Vector3>();var warpAt=new Dictionary<string,Vector3>();
            Flag("bossdown.Cantor");
            foreach(var zone in Zones)
            {
                yield return Zone(zone);
                foreach(var warp in All<WarpBeacon>())if(!string.IsNullOrEmpty(warp.requiredFlag))Flag(warp.requiredFlag);
                yield return null;yield return null;
                foreach(var warp in All<WarpBeacon>()){exits.Add((zone,warp.prompt,warp.scene,warp.spawn));warpAt[zone+">"+warp.scene]=warp.transform.position;}
                foreach(var dock in All<DockingPad>())exits.Add((zone,dock.prompt,dock.scene,dock.spawn));
                arrivals[zone]=new HashSet<string>(All<SpawnPoint>().Select(s=>s.id));
                bool flight=ZoneController.Current.Flight;
                foreach(var spawn in All<SpawnPoint>().GroupBy(s=>s.id).Select(g=>g.First()))
                {
                    spawnAt[zone+"/"+spawn.id]=spawn.transform.position;
                    CheckStanding(zone,spawn.id,spawn.transform.position,flight,"the lead");
                    CheckStanding(zone,spawn.id,spawn.transform.position+Vector3.right*(flight?HeroCollision.ArrivalSpacing:2),flight,"the companion");
                }
                if(!arrivals[zone].Contains("Arrival"))problems.Add(zone+" has no Arrival for a plain load");
            }
            Assert.Greater(exits.Count,20,"fixture: the world's exits");
            foreach(var (from,label,scene,spawn) in exits)
            {
                if(!Application.CanStreamedLevelBeLoaded(scene)||GameCatalog.Find<ZoneDef>(scene)==null){problems.Add($"{from}: \"{label}\" leads to {scene}, which isn't in the game");continue;}
                if(!arrivals.TryGetValue(scene,out var there)){problems.Add($"{from}: \"{label}\" leads out of the eleven zones, to {scene}");continue;}
                if(!there.Contains(spawn))problems.Add($"{from}: \"{label}\" lands at {scene}/{spawn}, and no such arrival exists: the party is dropped at the world's origin");
                if(!LabelNames(label,scene,spawn))problems.Add($"{from}: \"{label}\" leads to {DisplayNames.Zone(scene)} ({spawn})");
            }
            // The two exits that promise one particular spot of the Halo.
            foreach(var (spawn,warp) in new[]{("Outer","Gullet_Tunnel"),("Moon","Sorrel_Ridges"),("Tallow","TallowApproach")})
                if(FlatDistance(spawnAt["Hub_CinderHalo/"+spawn],warpAt["Hub_CinderHalo>"+warp])>25)problems.Add($"The Halo's {spawn} arrival is nowhere near the warp it serves");
            Verdict("Exits that don't lead where they say");
        }
        [UnityTest] public IEnumerator EveryWarpToTallowCallsItTheSamePlace()
        {
            foreach(var f in new[]{"bossdown.Cantor","lantern.routeKnown"})Flag(f);
            var names=new List<string>();
            foreach(var zone in new[]{"Hub_CinderHalo","Gullet_Tunnel","WickReach"})
            {
                yield return Zone(zone);yield return null;yield return null;
                names.Add(WarpTo("TallowApproach").prompt);
            }
            var places=names.Select(n=>Regex.Match(n,@"Tallow \w+").Value).Distinct().ToArray();
            Assert.AreEqual(1,places.Length,"three warps land in the same open space above Tallow, and their labels call it different places: "+string.Join(" | ",names));
        }
        static readonly Dictionary<string,string[]> WhatOpensIt=new()
        {
            {"warpkey",new[]{"survey key"}},{"met.Orrin",new[]{"Orrin"}},{"bossdown.Burrower",new[]{"Burrower"}},{"hushwell.nursery",new[]{"Bellows"}},
            {"bossdown.Cantor",new[]{"Cantor","Gullet"}},{"lantern.routeKnown",new[]{"Abri"}},{"met.Aumi",new[]{"Aumi"}},{"gorge.relaysLit",new[]{"relay"}},{"relay.hessaYielded",new[]{"captain","Hessa"}},
        };
        [UnityTest] public IEnumerator EveryLockedExitStaysShutAndSaysWhatOpensIt()
        {
            int locks=0;bool reachOpenToAnyone=false;
            foreach(var zone in Zones)
            {
                yield return Zone(zone);
                // The way into the Wick Reach decides whether anyone can ever read its home warp's lock. Lanternwick's pad doesn't count: the only way to Lanternwick is through the Reach.
                reachOpenToAnyone|=All<WarpBeacon>().Any(w=>w.scene=="WickReach"&&w.requiredFlag!="lantern.routeKnown")||zone!="Lanternwick"&&All<DockingPad>().Any(d=>d.scene=="WickReach");
                foreach(var warp in All<WarpBeacon>().Where(w=>!string.IsNullOrEmpty(w.requiredFlag)).ToArray())
                {
                    locks++;string name=warp.name,flag=warp.requiredFlag;
                    // Nobody stands in the Reach without this flag, so nobody can press this warp and be refused: its words can't reach a player (checked below).
                    if(zone=="WickReach"&&flag=="lantern.routeKnown")continue;
                    if(!WhatOpensIt.TryGetValue(flag,out var words)){problems.Add($"{zone}: {name} waits on {flag}, a lock this check doesn't know");continue;}
                    if(Has(flag)){problems.Add($"{zone}: fixture: {flag} is already set");continue;}
                    warp.Interact();yield return null;yield return null;
                    if(SceneFlow.Current.Loading||SceneFlow.Current.Zone!=zone)
                    {
                        problems.Add($"{zone}: {name} lets the party through without {flag}");
                        float deadline=Time.unscaledTime+25;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
                        break;
                    }
                    string said=warp.prompt;
                    if(!words.Any(said.Contains))problems.Add($"{zone}: {name} is shut on {flag}, and a refused press says \"{said}\", which doesn't name {string.Join(" or ",words)}");
                    CheckWords(zone+" lock",said,true);
                    // The route to the Gullet has a second lock: the key alone isn't enough.
                    if(warp.scene=="Gullet_Tunnel")
                    {
                        Flag(flag);warp.Interact();yield return null;yield return null;
                        if(SceneFlow.Current.Loading)problems.Add($"{zone}: the key alone opens the Gullet, without the nursery");
                        else if(!warp.prompt.Contains("Hushwell"))problems.Add($"{zone}: with the key and no chart the Gullet warp says \"{warp.prompt}\", which doesn't send the player to Hushwell");
                        if(SceneFlow.Current.Loading){float deadline=Time.unscaledTime+25;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;break;}
                        Flag("hushwell.nursery");
                    }
                    else Flag(flag);
                    yield return null;yield return null;
                    if(warp.prompt==said)problems.Add($"{zone}: {name} has opened and still says \"{said}\"");
                    Flag(flag,false);Flag("hushwell.nursery",false);
                    yield return null;yield return null;
                }
            }
            Assert.GreaterOrEqual(locks,9,"fixture: the world's locked warps");
            if(reachOpenToAnyone)problems.Add("The Wick Reach can now be entered without lantern.routeKnown, so its home warp's lock can be read, and that lock's words were never written: they're the field's default, about Sorrel's survey key");
            Verdict("Locks that don't say what opens them");
        }
        IEnumerator Takes(InteractionPrompt exit,string scene,string spawn)
        {
            string from=SceneFlow.Current.Zone,label=exit.prompt;
            exit.Interact();yield return null;
            float deadline=Time.unscaledTime+25;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            if(SceneFlow.Current.Zone!=scene){problems.Add($"{from}: \"{label}\" went to {SceneFlow.Current.Zone}, not {scene}");yield break;}
            Quiet();yield return new WaitForSecondsRealtime(.3f);
            var point=All<SpawnPoint>().FirstOrDefault(s=>s.id==spawn);var hero=PartyController.Current.Active.transform.position;
            if(point==null)problems.Add($"{from}: \"{label}\" has no {spawn} to arrive at");
            else if(FlatDistance(hero,point.transform.position)>6)problems.Add($"{from}: \"{label}\" put the party {FlatDistance(hero,point.transform.position):0} m from {spawn}");
            var saved=Saves.Load("autosave");
            if(saved==null||saved.zone!=scene||saved.spawn!=spawn)problems.Add($"{from}: arriving by \"{label}\" wasn't autosaved there");
        }
        [UnityTest] public IEnumerator TheExitsNoRouteTakesLeadWhereTheySay()
        {
            foreach(var f in new[]{"met.Orrin","warpkey","hushwell.nursery","bossdown.Burrower","clear.Sorrel_Burrower","bossdown.Cantor","lantern.routeKnown","met.Aumi","gorge.relaysLit"})Flag(f);
            yield return Zone("Hub_CinderHalo","Outer");
            yield return Takes(WarpTo("TallowApproach"),"TallowApproach","Halo");
            yield return Takes(WarpTo("Hub_CinderHalo"),"Hub_CinderHalo","Tallow");
            yield return Takes(WarpTo("Sorrel_Ridges"),"Sorrel_Ridges","Arrival");
            yield return Takes(All<DockingPad>().Single(d=>d.spawn=="Moon"),"Hub_CinderHalo","Moon");
            yield return Takes(All<DockingPad>().Single(d=>d.spawn=="Shop"),"Hub_Decks","Shop");
            yield return Takes(All<DockingPad>().Single(d=>d.spawn=="Shop"),"Hub_CinderHalo","Shop");
            yield return Takes(All<DockingPad>().Single(d=>d.spawn=="Repair"),"Hub_Decks","Repair");
            yield return Takes(All<DockingPad>().Single(d=>d.spawn=="Repair"),"Hub_CinderHalo","Repair");
            yield return Zone("Hushwell");
            yield return Takes(All<WarpBeacon>().Single(w=>string.IsNullOrEmpty(w.requiredFlag)),"Sorrel_Ridges","Hushwell");
            yield return Zone("LanternGorge");
            yield return Takes(WarpTo("Lanternwick"),"Lanternwick","Gorge");
            yield return Zone("SunkenRelay");
            yield return Takes(WarpTo("LanternGorge"),"LanternGorge","Relay");
            Verdict("Exits no replay route ever takes");
        }

        // ================================================================ 4.5 art: placeholders, floating and sunken props, stretched textures, relay light
        static readonly Regex BuiltIn=new(@"^(Cube|Sphere|Cylinder|Capsule)( Instance)?$");
        [UnityTest] public IEnumerator NoPlaceholderIsDrawnInAnyZone()
        {
            var blockout=new HashSet<Material>(Resources.LoadAll<Material>("Blockout"));int drawn=0;
            foreach(var zone in Zones)
            {
                yield return Zone(zone);
                var view=GeometryUtility.CalculateFrustumPlanes(Camera.main);
                foreach(var t in All<Transform>())if(t.name.Contains("ART_PENDING")&&t.gameObject.activeInHierarchy)problems.Add($"{zone}: {t.name} is still placeholder art");
                foreach(var r in All<Renderer>())
                {
                    if(!r.enabled||!r.gameObject.activeInHierarchy||!(r is MeshRenderer||r is SkinnedMeshRenderer||r is LineRenderer)||r.GetComponent<TMP_Text>()!=null)continue;
                    // _Boot keeps a 1 m cube a kilometre under the world so a build doesn't strip the toon shader. The fixed camera looks forward and down from up here and can't
                    // frame it. Going by where it is and not by its name, and only while the camera really can't see it.
                    if(r.bounds.max.y<-500&&!GeometryUtility.TestPlanesAABB(view,r.bounds))continue;
                    drawn++;
                    if(r.sharedMaterials.Any(m=>m==null))problems.Add($"{zone}: {r.name} has a missing material and draws magenta");
                    if(r.sharedMaterials.Any(m=>m!=null&&m.shader!=null&&m.shader.name=="Hidden/InternalErrorShader"))problems.Add($"{zone}: {r.name}'s shader failed and it draws magenta");
                    if(r.GetComponentInParent<Membrane>()!=null)continue;
                    if(r.sharedMaterials.Any(blockout.Contains))problems.Add($"{zone}: {r.name} wears a blockout material");
                    // A builder renames its blocks, so a name proves nothing: the mesh itself is Unity's. A planet is a sphere with a painted map on it.
                    var filter=r.GetComponent<MeshFilter>();var look=r.sharedMaterial;
                    bool painted=look!=null&&look.HasProperty("_BaseMap")&&look.GetTexture("_BaseMap")!=null;
                    if(filter!=null&&filter.sharedMesh!=null&&BuiltIn.IsMatch(filter.sharedMesh.name)&&!painted)problems.Add($"{zone}: {r.name} is a bare {filter.sharedMesh.name}");
                }
            }
            Assert.Greater(drawn,1000,"fixture: the eleven zones draw plenty");
            Verdict("Placeholder art on show");
        }

        static IEnumerable<Transform> Split(Transform t,int depth)
        {
            if(!t.gameObject.activeInHierarchy)yield break;
            // A container holds props; a prop's own parts are measured as one thing.
            if(depth<3&&t.GetComponent<Renderer>()==null&&t.GetComponent<Collider>()==null&&t.childCount>=6){foreach(Transform child in t)foreach(var part in Split(child,depth+1))yield return part;yield break;}
            yield return t;
        }
        static IEnumerable<Transform> Props()
        {
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if(root.GetComponentInChildren<CombatActor>(true)!=null||root.GetComponent<ZoneController>()!=null||root.GetComponent<CameraRig>()!=null)continue;
                foreach(var prop in Split(root.transform,0))
                    if(prop.GetComponentInChildren<EnemyBrain>(true)==null&&prop.GetComponentInChildren<Membrane>(true)==null&&prop.GetComponentInChildren<Telegraph>(true)==null)yield return prop;
            }
        }
        static bool Drawn(Transform prop,out Bounds bounds)
        {
            bounds=default;bool any=false;
            foreach(var r in prop.GetComponentsInChildren<Renderer>())
            {
                if(!(r is MeshRenderer||r is SkinnedMeshRenderer)||!r.enabled||r.forceRenderingOff||r.GetComponent<TMP_Text>()!=null)continue;
                if(!any){bounds=r.bounds;any=true;}else bounds.Encapsulate(r.bounds);
            }
            return any;
        }
        /// <summary>Things this measure can judge: not a flat decal, and not a hull or a cliff that five rays can't take the measure of.</summary>
        static bool Measurable(Bounds b)=>b.size.y>=.15f&&b.size.y<=14&&Mathf.Max(b.size.x,b.size.z)<=14;
        /// <summary>Whether what a ray landed on is ground this prop could be buried in: the landform, or a floor slab, that reaches under the prop's middle. Another prop's collision
        /// box isn't ground: it's a flat-topped block round something lumpy, so a rock leaning into a ledge or a shrub at the toe of a spoil heap would read as buried. A door's
        /// lintel isn't either, however thin: it hangs overhead with nothing under it to sink into.</summary>
        static bool CanBury(Collider c,float middle)=>(c is MeshCollider||c.bounds.size.y<=.6f)&&c.bounds.min.y<=middle+.25f;
        static float GroundBelow(Vector3 at,float top,Transform prop,float buriedFrom=float.NaN)
        {
            float ground=float.NegativeInfinity;
            foreach(var hit in Physics.RaycastAll(new Vector3(at.x,top+20,at.z),Vector3.down,90,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.point.y>top||hit.point.y<=ground||hit.collider.transform.IsChildOf(prop)||IsBody(hit.collider))continue;
                if(!float.IsNaN(buriedFrom)&&(!CanBury(hit.collider,buriedFrom)||hit.collider.GetComponentInParent<Membrane>()!=null))continue;
                ground=hit.point.y;
            }
            return ground;
        }
        /// <summary>Null when the prop sits right. Floating: under no part of it is there ground within a quarter metre, and nothing beside it holds it up.
        /// Sunk: even where the ground is lowest, half of it is underneath and the rest still shows.</summary>
        static string Measure(Transform prop,Bounds b)
        {
            float smallestGap=float.PositiveInfinity,lowestGround=float.PositiveInfinity;
            foreach(var (x,z) in new[]{(0f,0f),(.6f,.6f),(-.6f,.6f),(.6f,-.6f),(-.6f,-.6f)})
            {
                var at=b.center+new Vector3(x*b.extents.x,0,z*b.extents.z);
                smallestGap=Mathf.Min(smallestGap,b.min.y-GroundBelow(at,b.max.y+.05f,prop));
                lowestGround=Mathf.Min(lowestGround,GroundBelow(at,b.max.y+.05f,prop,b.center.y));
            }
            if(smallestGap>.25f)
            {
                bool touching=false;
                foreach(var near in Physics.OverlapBox(b.center,b.extents+Vector3.one*.3f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                    if(!near.transform.IsChildOf(prop)&&!IsBody(near)){touching=true;break;}
                if(!touching)return float.IsInfinity(smallestGap)?"hangs over nothing at all":$"floats {smallestGap:0.00} m off the ground";
            }
            if(lowestGround-b.min.y>=.5f*b.size.y)
            {
                // Not faults: a slab let into the floor, which is the point of one; a patch laid over the ground vertex by vertex, whose box only says the ground slopes; and what's wholly out of sight under a deck.
                bool inlaid=b.size.y<=.15f*Mathf.Max(b.size.x,b.size.z)||prop.name.StartsWith("Floor detail");
                if(!inlaid&&b.max.y-lowestGround>.02f)return $"is sunk {lowestGround-b.min.y:0.00} m of its {b.size.y:0.00} m into the ground";
            }
            return null;
        }
        [UnityTest] public IEnumerator NothingFloatsOrSinksIn([Values("Hub_Decks","Sorrel_Ridges","Hushwell","TallowDrift","Lanternwick","LanternGorge","SunkenRelay")] string zone)
        {
            if(zone=="LanternGorge"){RoadToAumi();RpgServices.Quests.Start("PellsLamp");RpgServices.Quests.Start("UnswornRations");}
            if(zone=="SunkenRelay")RpgServices.Quests.Start("IshensRecord");
            yield return Zone(zone);yield return null;yield return null;
            // The measure has to be able to say both things, and to say nothing.
            var arrival=All<SpawnPoint>().First(s=>s.id=="Arrival").transform.position;
            Assert.IsTrue(FloorUnder(arrival,3,6,out float floor),"fixture: floor at the arrival");
            var crate=GameObject.CreatePrimitive(PrimitiveType.Cube);crate.transform.localScale=Vector3.one*.6f;
            crate.transform.position=new Vector3(arrival.x,floor+1.6f,arrival.z);
            Assert.IsTrue(Drawn(crate.transform,out var bounds),"fixture: the control crate draws");StringAssert.Contains("floats",Measure(crate.transform,bounds)??"","the measure can't see a crate hanging in the air");
            crate.transform.position=new Vector3(arrival.x,floor-.12f,arrival.z);
            Drawn(crate.transform,out bounds);StringAssert.Contains("sunk",Measure(crate.transform,bounds)??"","the measure can't see a crate half under the floor");
            crate.transform.position=new Vector3(arrival.x,floor+.3f,arrival.z);
            Drawn(crate.transform,out bounds);Assert.IsNull(Measure(crate.transform,bounds),"the measure faults a crate sitting on the floor");
            Object.Destroy(crate);yield return null;
            int measured=0;
            foreach(var prop in Props())
            {
                if(!Drawn(prop,out var b)||!Measurable(b))continue;
                measured++;
                string wrong=Measure(prop,b);
                if(wrong!=null)problems.Add($"{prop.name} at ({b.center.x:0.0}, {b.center.z:0.0}) {wrong}");
            }
            Assert.Greater(measured,10,"fixture: "+zone+" has props to measure");
            TestContext.WriteLine($"{zone}: measured {measured} props");
            Verdict($"{zone}: props off the ground or half under it ({problems.Count} of {measured})");
        }
        [UnityTest] public IEnumerator CliffBandsRunLevelAndTheCanyonIsNotStretched([Values("Lanternwick","LanternGorge","SunkenRelay")] string zone)
        {
            yield return Zone(zone);
            var cliffs=All<MeshFilter>().Where(f=>f.sharedMesh!=null&&f.sharedMesh.isReadable&&f.GetComponent<MeshRenderer>()!=null&&(f.name+" "+f.sharedMesh.name).ToLowerInvariant().Contains("cliffs")).ToArray();
            Assert.IsNotEmpty(cliffs,"fixture: "+zone+" has cliff faces to read");
            int faces=0,tilted=0,stretched=0;
            foreach(var filter in cliffs)
            {
                var mesh=filter.sharedMesh;var v=mesh.vertices;var uv=mesh.uv;var t=mesh.triangles;var from=filter.transform;
                for(int i=0;i+2<t.Length;i+=3)
                {
                    Vector3 a=from.TransformPoint(v[t[i]]),e1=from.TransformPoint(v[t[i+1]])-a,e2=from.TransformPoint(v[t[i+2]])-a;
                    Vector2 d1=uv[t[i+1]]-uv[t[i]],d2=uv[t[i+2]]-uv[t[i]];
                    var n=Vector3.Cross(e1,e2);float area=n.sqrMagnitude;if(area<1e-6f||Mathf.Abs(n.normalized.y)>.78f)continue;
                    faces++;
                    var gradU=(d1.x*Vector3.Cross(e2,n)+d2.x*Vector3.Cross(n,e1))/area;var gradV=(d1.y*Vector3.Cross(e2,n)+d2.y*Vector3.Cross(n,e1))/area;
                    if(gradV.sqrMagnitude<1e-8f){tilted++;continue;}
                    var level=Vector3.Cross(n,Vector3.up).normalized;
                    if(Mathf.Asin(Mathf.Clamp01(Mathf.Abs(Vector3.Dot(gradV.normalized,level))))*Mathf.Rad2Deg>12)tilted++;
                    float big=Mathf.Max(gradU.magnitude,gradV.magnitude),small=Mathf.Min(gradU.magnitude,gradV.magnitude);
                    if(small<1e-5f||big/small>4)stretched++;
                }
            }
            Assert.Greater(faces,100,"fixture: "+zone+" has cliff faces to read");
            Assert.Less(tilted,faces*.01f,$"{zone}: the strata run off the level on {tilted} of {faces} cliff faces");
            Assert.Less(stretched,faces*.02f,$"{zone}: the strata are smeared on {stretched} of {faces} cliff faces");
        }
        [UnityTest] public IEnumerator NoKitPieceIsStretchedAcrossItsTexture()
        {
            // The lead's call: the station's low walls and hull fascia at 3 to 4.3 pass, as they read fine in captures (v1/s01/009). 5 still catches a wall panel laid on its back (D110) and one fitted to a block's whole depth.
            const float Bar=5;
            int looked=0;var named=new HashSet<string>();
            foreach(var zone in Zones)
            {
                yield return Zone(zone);
                foreach(var filter in All<MeshFilter>())
                {
                    var renderer=filter.GetComponent<MeshRenderer>();var mesh=filter.sharedMesh;
                    if(renderer==null||!renderer.enabled||renderer.forceRenderingOff||!filter.gameObject.activeInHierarchy||mesh==null)continue;
                    if(filter.GetComponentInParent<Membrane>()!=null||filter.GetComponent<TMP_Text>()!=null||filter.GetComponentInParent<CombatActor>()!=null||filter.GetComponentInParent<EnemyBrain>()!=null)continue;
                    // A flat colour has no texture to stretch.
                    var look=renderer.sharedMaterial;if(look==null||!look.HasProperty("_BaseMap")||look.GetTexture("_BaseMap")==null)continue;
                    // What shows is a piece's broad face, its two longest sides as it stands. Thickening a thin wall leaves the wall's own face as it was.
                    var scale=filter.transform.lossyScale;var size=mesh.bounds.size;
                    var sides=new[]{(s:Mathf.Abs(scale.x),e:size.x),(s:Mathf.Abs(scale.y),e:size.y),(s:Mathf.Abs(scale.z),e:size.z)}.OrderByDescending(a=>a.s*a.e).Take(2).ToArray();
                    if(sides[1].s<=0||sides[1].s*sides[1].e<.75f)continue;
                    looked++;
                    float most=Mathf.Max(sides[0].s,sides[1].s),least=Mathf.Min(sides[0].s,sides[1].s);
                    if(most/least<Bar)continue;
                    // Every kit piece's mesh is called Body: say what it's part of, how big it stands and where the first of its kind is.
                    string what=filter.name=="Body"&&filter.transform.parent!=null?filter.transform.parent.name:filter.name;
                    string kind=$"{zone}: {what}, {sides[0].s*sides[0].e:0.0} by {sides[1].s*sides[1].e:0.0} m, is scaled {most/least:0.0} times longer one way than the other, across a texture that wasn't";
                    if(named.Add(kind)){var c=renderer.bounds.center;problems.Add($"{kind} (first at {c.x:0}, {c.y:0}, {c.z:0})");}
                }
            }
            Assert.Greater(looked,300,"fixture: the eleven zones hold plenty of kit pieces");
            Verdict("Kit pieces stretched across their textures");
        }
        static float Glow(RelayLantern relay)
        {
            var block=new MaterialPropertyBlock();float most=0;
            foreach(var crystal in relay.crystals)
            {
                if(crystal==null)continue;
                crystal.GetPropertyBlock(block);var v=block.GetVector("_EmissionColor");most=Mathf.Max(most,new Vector3(v.x,v.y,v.z).magnitude);
            }
            return most;
        }
        [UnityTest] public IEnumerator RelaysStayDarkUntilLitAndStayLitWithoutCeremony()
        {
            RoadToAumi();
            foreach(var zone in new[]{"WickReach","LanternGorge"})
            {
                yield return Zone(zone);yield return null;
                var relays=All<RelayLantern>().Where(r=>!string.IsNullOrEmpty(r.flag)).ToArray();
                Assert.Greater(relays.Length,1,"fixture: "+zone+" has relays to light");
                foreach(var relay in relays)
                {
                    if(relay.Lit||relay.glow.enabled)problems.Add($"{zone}: {relay.name} is lit before anyone has lit it");
                    if(Glow(relay)>.4f)problems.Add($"{zone}: {relay.name}'s crystal burns before it is relit, at {Glow(relay):0.00}");
                }
                log.Clear();foreach(var relay in relays)Flag(relay.flag);
                yield return null;yield return null;
                foreach(var relay in relays)
                {
                    if(!relay.Lit||!relay.glow.enabled)problems.Add($"{zone}: {relay.name} throws no light once relit");
                    if(Glow(relay)<1)problems.Add($"{zone}: {relay.name}'s crystal stays dull once relit, at {Glow(relay):0.00}");
                }
                if(log.Count!=relays.Length)problems.Add($"{zone}: relighting {relays.Length} relays made {log.Count} bursts");
                log.Clear();
                yield return Zone(zone);yield return null;
                foreach(var relay in All<RelayLantern>().Where(r=>!string.IsNullOrEmpty(r.flag)))
                    if(!relay.Lit||!relay.glow.enabled)problems.Add($"{zone}: {relay.name} goes dark again on a later visit");
                if(log.Count>0)problems.Add($"{zone}: walking back in plays the relighting all over again");
            }
            Verdict("Relays that don't look their state");
        }

        // ================================================================ 4.5 navigation
        static Vector3 WalkableArrival()
        {
            var spawn=All<SpawnPoint>().First(s=>s.id=="Arrival");
            Assert.IsTrue(NavMesh.SamplePosition(spawn.transform.position,out var at,3,NavMesh.AllAreas),"fixture: "+SceneManager.GetActiveScene().name+" has walkable floor at its arrival");
            return at.position;
        }
        static bool Joined(Vector3 from,Vector3 to,NavMeshPath path)=>NavMesh.CalculatePath(from,to,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        [UnityTest] public IEnumerator EveryPromptCanBeReachedOnFoot([Values("Hub_Decks","Sorrel_Ridges","Hushwell","TallowDrift","Lanternwick","LanternGorge","SunkenRelay")] string zone)
        {
            Flag("relay.hessaConfronted");
            yield return Zone(zone);ClearFights();OpenSeals();yield return new WaitForSecondsRealtime(.6f);
            Assert.IsNotNull(GroundNavigation.Current,"fixture: "+zone+" has walkable paths");
            var from=WalkableArrival();var path=new NavMeshPath();int looked=0;
            foreach(var prompt in All<InteractionPrompt>())
            {
                if(prompt.range<=0||prompt is HailPoint)continue;
                looked++;var at=prompt.transform.position;bool floor=false,reached=false;
                // Any spot in reach will do, and the nearest floor needn't be one: the bake also lays walkable patches on top of a solid prop and in the hollow a tall box leaves
                // inside itself, and no body stands there. Pressing a prompt measures from the feet to the prop's own position, in three dimensions (PromptService.FindInteraction).
                for(float r=0;r<=prompt.range&&!reached;r+=.25f)for(int a=0;a<24&&!reached;a++)
                {
                    if(!NavMesh.SamplePosition(at+Quaternion.Euler(0,a*15,0)*Vector3.forward*r,out var near,prompt.range+2,NavMesh.AllAreas))continue;
                    if(Vector3.Distance(near.position,at)>prompt.range+.25f)continue;
                    floor=true;reached=Joined(from,near.position,path);
                }
                if(!floor)problems.Add($"\"{prompt.prompt}\" at {at} has no floor to stand on within the {prompt.range:0.0} m its prompt reaches");
                else if(!reached)problems.Add($"\"{prompt.prompt}\" at {at} is cut off from the arrival: no floor within its {prompt.range:0.0} m reach joins the floor the party lands on");
            }
            Assert.Greater(looked,0,"fixture: "+zone+" has prompts");
            Verdict(zone+": prompts a walker can't reach");
        }
        [UnityTest] public IEnumerator EveryFlightPromptHasRoomForAHullWithinItsReach([Values("Hub_CinderHalo","Gullet_Tunnel","TallowApproach","WickReach")] string zone)
        {
            yield return Zone(zone);yield return null;
            float hull=Mathf.Max(HeroCollision.HullRadius("Taren"),HeroCollision.HullRadius("Sela"));int looked=0;
            foreach(var prompt in All<InteractionPrompt>())
            {
                if(prompt.range<=0)continue;
                looked++;var at=prompt.transform.position;var plane=new Vector3(at.x,1,at.z);bool room=false;
                for(float r=0;r<=prompt.range&&!room;r+=.5f)for(int a=0;a<16&&!room;a++)
                {
                    var p=plane+Quaternion.Euler(0,a*22.5f,0)*Vector3.forward*r;
                    if(Vector3.Distance(p,at)>=prompt.range-.1f)continue;
                    if(!Physics.OverlapSphere(p+Vector3.up*.8f,hull,~0,QueryTriggerInteraction.Ignore).Any(c=>!IsBody(c)))room=true;
                }
                if(!room)problems.Add($"\"{prompt.prompt}\" at {at}: nowhere on the flight plane within its {prompt.range:0.0} m is there room for a hull");
            }
            Assert.Greater(looked,0,"fixture: "+zone+" has prompts");
            Verdict(zone+": prompts a ship can't reach");
        }
        /// <summary>The rim of the walkable mesh. Given a start, only the rim of the floor a body can get to from there: the bake also lays walkable patches on every cliff top and box top,
        /// under cliffs and inside hollow boxes, and none of those joins the floor. Unity's own 45 degree limit lets a controller a little further than this mesh: the walk out past the rim is what covers that.</summary>
        static List<(Vector3 mid,Vector3 outward)> WalkableEdges(int most,Vector3? from=null)
        {
            var mesh=NavMesh.CalculateTriangulation();var v=mesh.vertices;var t=mesh.indices;
            Vector3Int Key(Vector3 p)=>new(Mathf.RoundToInt(p.x*20),Mathf.RoundToInt(p.y*20),Mathf.RoundToInt(p.z*20));
            var edges=new Dictionary<(Vector3Int,Vector3Int),(int count,Vector3 mid,Vector3 outward)>();
            for(int i=0;i+2<t.Length;i+=3)for(int e=0;e<3;e++)
            {
                Vector3 a=v[t[i+e]],b=v[t[i+(e+1)%3]],c=v[t[i+(e+2)%3]];
                var along=Flat(b-a);if(along.sqrMagnitude<.01f)continue;
                var outward=new Vector3(-along.z,0,along.x).normalized;if(Vector3.Dot(outward,Flat(c-a))>0)outward=-outward;
                var ka=Key(a);var kb=Key(b);bool order=ka.x<kb.x||ka.x==kb.x&&(ka.y<kb.y||ka.y==kb.y&&ka.z<=kb.z);
                var key=order?(ka,kb):(kb,ka);
                edges[key]=edges.TryGetValue(key,out var seen)?(seen.count+1,seen.mid,seen.outward):(1,(a+b)*.5f,outward);
            }
            var rim=edges.Values.Where(e=>e.count==1).Select(e=>(e.mid,e.outward)).ToList();
            if(from.HasValue)
            {
                var path=new NavMeshPath();
                rim=rim.Where(e=>NavMesh.SamplePosition(e.mid,out var on,.25f,NavMesh.AllAreas)&&Joined(from.Value,on.position,path)).ToList();
            }
            int stride=Mathf.Max(1,rim.Count/most);
            return rim.Where((_,i)=>i%stride==0).ToList();
        }
        [UnityTest] public IEnumerator NoWalkableEdgeDropsIntoNothing([Values("Hub_Decks","Sorrel_Ridges","Hushwell","TallowDrift","Lanternwick","LanternGorge","SunkenRelay")] string zone)
        {
            yield return Zone(zone);OpenSeals();yield return new WaitForSecondsRealtime(.6f);
            var edges=WalkableEdges(1500,WalkableArrival());Assert.Greater(edges.Count,20,"fixture: "+zone+" has a walkable edge");
            var holes=new HashSet<string>();var ledges=new HashSet<string>();int tried=0;
            foreach(var (mid,outward) in edges)
            {
                float y=mid.y;
                for(float d=.5f;d<=3;d+=.5f)
                {
                    var p=mid+outward*d;p.y=y;
                    if(!FloorUnder(p,2,60,out float ground)){holes.Add($"({p.x:0}, {p.z:0})");break;}
                    if(!BodyFits(new Vector3(p.x,ground,p.z)))break;
                    if(y-ground>6){ledges.Add($"({p.x:0}, {p.z:0}) down {y-ground:0} m");break;}
                    y=ground;
                }
                if(++tried%200==0)yield return null;
            }
            TestContext.WriteLine($"{zone}: {tried} edge points, ledges a body can walk off: {string.Join(", ",ledges.Take(20))}");
            Assert.IsEmpty(holes,$"{zone}: nothing stops a walker here and there is no floor beyond, so the party walks off the world at: "+string.Join(", ",holes.Take(30)));
        }
        [UnityTest] public IEnumerator NoInvisibleWallStandsInTheOpen([Values("Hub_Decks","Sorrel_Ridges","Hushwell","TallowDrift","Lanternwick","LanternGorge","SunkenRelay")] string zone)
        {
            yield return Zone(zone);OpenSeals();yield return new WaitForSecondsRealtime(.6f);
            int walls=0;var from=WalkableArrival();var path=new NavMeshPath();
            bool Floor(Vector3 at,float top,out float y)
            {
                y=float.NegativeInfinity;bool found=false;
                foreach(var hit in Physics.RaycastAll(new Vector3(at.x,top,at.z),Vector3.down,top+40,~0,QueryTriggerInteraction.Ignore))
                {
                    // Standing ground is something drawn or the landform itself, not the top of another invisible box.
                    if(IsBody(hit.collider)||!(hit.collider is MeshCollider||hit.collider.GetComponentInChildren<Renderer>()!=null)||hit.point.y<=y)continue;
                    y=hit.point.y;found=true;
                }
                return found;
            }
            foreach(var box in All<BoxCollider>())
            {
                if(!box.enabled||box.isTrigger||!box.gameObject.activeInHierarchy||box.GetComponentInParent<Membrane>()!=null||IsBody(box))continue;
                if(box.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&!r.forceRenderingOff)||box.GetComponentInParent<Renderer>()!=null)continue;
                var t=box.transform;var size=Vector3.Scale(box.size,t.lossyScale);size=new Vector3(Mathf.Abs(size.x),Mathf.Abs(size.y),Mathf.Abs(size.z));if(size.y<1)continue;
                walls++;
                bool alongX=size.x>=size.z;Vector3 along=alongX?t.right:t.forward,across=alongX?t.forward:t.right;float half=(alongX?size.x:size.z)*.5f,thick=(alongX?size.z:size.x)*.5f;
                var centre=t.TransformPoint(box.center);float top=centre.y+size.y*.5f+6;int open=0;Vector3 first=centre;
                for(float s=-half;s<=half;s+=2)foreach(int side in new[]{-1,1})
                {
                    var foot=centre+along*s;
                    // A wall only stops someone who can walk up to it: this side's floor has to be the floor the party lands on, not a cliff top beside the wall.
                    if(!Floor(foot+across*side*(thick+1),top,out float near)||!NavMesh.SamplePosition(new Vector3(foot.x+across.x*side*(thick+1),near,foot.z+across.z*side*(thick+1)),out var stand,1.2f,NavMesh.AllAreas)||!Joined(from,stand.position,path))continue;
                    // Beyond it the same ground runs on: no higher than a walkable slope, for a good stride past the wall.
                    bool runsOn=true;
                    foreach(float d in new[]{1f,3f,6f})
                    {
                        var far=foot-across*side*(thick+d);
                        if(!Floor(far,top,out float beyond)||Mathf.Abs(beyond-near)>.75f*(d+1)+.1f||!BodyFits(new Vector3(far.x,beyond,far.z))){runsOn=false;break;}
                    }
                    if(!runsOn)continue;
                    if(open==0)first=foot;open++;
                }
                if(open>=3)problems.Add($"{box.name} near ({first.x:0}, {first.z:0}): about {open*2} m of invisible wall across ground that visibly carries on");
            }
            TestContext.WriteLine($"{zone}: {walls} invisible boxes looked at");
            Verdict(zone+": invisible walls in the open");
        }
        [UnityTest] public IEnumerator TheBodyNeverSticksAtAWallFootOrAProp([Values("Hub_Decks","Sorrel_Ridges","Hushwell","TallowDrift","Lanternwick","LanternGorge","SunkenRelay")] string zone)
        {
            Flag("relay.hessaConfronted");
            yield return Zone(zone);ClearFights();OpenSeals();yield return new WaitForSecondsRealtime(.6f);
            var party=PartyController.Current;var lead=party.Active;var cc=lead.GetComponent<CharacterController>();
            foreach(var member in party.members)if(member!=lead)member.GetComponent<CharacterController>().enabled=false;
            var probes=new List<(Vector3 stand,Vector3 toward,string what)>();
            var solids=All<Collider>().Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.activeInHierarchy&&!IsBody(c)&&c.GetComponentInParent<Membrane>()==null&&c.bounds.size.y>=.2f&&Mathf.Max(c.bounds.size.x,c.bounds.size.z)<=30).ToArray();
            int stride=Mathf.Max(1,solids.Length/220);
            for(int i=0;i<solids.Length;i+=stride)
            {
                var b=solids[i].bounds;
                if(!NavMesh.SamplePosition(b.center,out var near,Mathf.Max(b.extents.x,b.extents.z)+3,NavMesh.AllAreas))continue;
                var toward=Flat(b.center-near.position);if(toward.sqrMagnitude<.01f)continue;
                probes.Add((near.position,toward.normalized,solids[i].name));
            }
            foreach(var (mid,outward) in WalkableEdges(220))probes.Add((mid,outward,"the wall foot"));
            Assert.Greater(probes.Count,20,"fixture: "+zone+" has wall feet and props to try");
            const float Dt=1f/60,Pace=6.7f;var down=Vector3.down*7;var ways=Enumerable.Range(0,8).Select(a=>Quaternion.Euler(0,a*45,0)*Vector3.forward).ToArray();
            int tried=0;
            foreach(var (stand,toward,what) in probes)
            {
                Put(lead,stand+Vector3.up*.05f);
                for(int f=0;f<30;f++)cc.Move((toward*Pace+down)*Dt);
                var pressed=lead.transform.position;bool free=false;
                foreach(var way in ways)
                {
                    Put(lead,pressed);
                    for(int f=0;f<15;f++)cc.Move((way*Pace+down)*Dt);
                    if(FlatDistance(lead.transform.position,pressed)>.35f){free=true;break;}
                }
                if(!free)problems.Add($"pressed against {what} near ({pressed.x:0.0}, {pressed.z:0.0}), the body can't walk away in any direction");
                if(++tried%30==0)yield return null;
            }
            TestContext.WriteLine($"{zone}: pressed into {tried} wall feet and props");
            Verdict(zone+": places the party sticks");
        }
        static Vector3[] LongestWalk(float most)
        {
            var from=WalkableArrival();Vector3[] best=new Vector3[0];float longest=0;var path=new NavMeshPath();
            foreach(var prompt in All<InteractionPrompt>())
            {
                if(!prompt.gameObject.activeInHierarchy||prompt.range<=0)continue;
                if(!NavMesh.SamplePosition(prompt.transform.position,out var near,prompt.range+1.5f,NavMesh.AllAreas))continue;
                if(!NavMesh.CalculatePath(from,near.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                var corners=path.corners;float length=0;for(int i=1;i<corners.Length;i++)length+=Vector3.Distance(corners[i-1],corners[i]);
                if(length<=most&&length>longest){longest=length;best=corners;}
            }
            return best;
        }
        [UnityTest] public IEnumerator TheCompanionKeepsUpOnFoot([Values("Sorrel_Ridges","Hushwell","Lanternwick","LanternGorge","SunkenRelay")] string zone)
        {
            Flag("relay.hessaConfronted");
            yield return Zone(zone);ClearFights();OpenSeals();yield return new WaitForSecondsRealtime(.6f);
            var party=PartyController.Current;var lead=party.Active;var partner=party.members.First(m=>m!=lead);partner.GetComponent<PartnerBrain>().enabled=true;
            var route=LongestWalk(140);Assert.Greater(route.Length,1,"fixture: "+zone+" has a walk to take");
            int corner=1;float deadline=Time.unscaledTime+45,widest=0;
            while(corner<route.Length&&Time.unscaledTime<deadline)
            {
                var gap=Flat(route[corner]-lead.transform.position);
                if(gap.magnitude<.7f){corner++;continue;}
                var way=gap.normalized;lead.motor.Move(new Vector2(way.x,way.z),false,false);
                widest=Mathf.Max(widest,FlatDistance(lead.transform.position,partner.transform.position));
                yield return null;
            }
            Assert.AreEqual(route.Length,corner,$"{zone}: the lead never finished a walk the floor says is open, and stopped at {lead.transform.position}");
            lead.motor.Move(Vector2.zero,false,false);
            deadline=Time.unscaledTime+6;while(FlatDistance(lead.transform.position,partner.transform.position)>8&&Time.unscaledTime<deadline)yield return null;
            float left=FlatDistance(lead.transform.position,partner.transform.position);
            Assert.LessOrEqual(left,8,$"{zone}: after a walk of the zone the companion is left {left:0} m behind, at {partner.transform.position}");
            Assert.Less(widest,18,$"{zone}: on the way the companion falls {widest:0} m behind: far enough to drop out of a fight");
        }

        // ================================================================ 4.5 Lanternwick
        [UnityTest] public IEnumerator EveryNamedTownspersonCanBeReachedAndAnswers()
        {
            yield return Zone("Lanternwick");
            var from=WalkableArrival();var path=new NavMeshPath();var lead=PartyController.Current.Active;
            foreach(var (speaker,node,place) in new (string speaker,string node,string place)[]{("Aumi","AumiFirst","LANTERN STAIR"),("Pell","PellFirst",null),("Ishen","IshenFirst","ARCHIVE"),("Corra","CorraFirst","ARCH MARKET"),("Brekk","BrekkFirst","ORCHARD")})
            {
                var person=Person(speaker);
                if(!person.gameObject.activeInHierarchy){problems.Add(speaker+" isn't in town");continue;}
                if(place!=null)
                {
                    var sign=All<TextMeshPro>().FirstOrDefault(t=>t.text.Trim().ToUpperInvariant()==place);
                    if(sign==null)problems.Add($"nothing in town says {place}");
                    else if(FlatDistance(sign.transform.position,person.transform.position)>25)problems.Add($"{speaker} stands {FlatDistance(sign.transform.position,person.transform.position):0} m from the {place} sign, where the story puts them");
                }
                if(!NavMesh.SamplePosition(person.transform.position,out var near,person.range+1,NavMesh.AllAreas)||!NavMesh.CalculatePath(from,near.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)
                {problems.Add($"{speaker} can't be walked up to from the dock");continue;}
                Put(lead,near.position+Vector3.up*.1f);yield return null;
                PromptService.FindInteraction(lead.transform.position);
                if(PromptService.Interaction!=person)problems.Add($"standing beside {speaker}, A goes to \"{(PromptService.Interaction!=null?PromptService.Interaction.prompt:"nothing")}\"");
                yield return Talk(person);
                if(heard!=node)problems.Add($"{speaker}'s first words are {heard}, not {node}");
            }
            Verdict("Lanternwick's people");
        }
        [UnityTest] public IEnumerator NobodyStandsInsideAProp([Values("Hub_Decks","Sorrel_Ridges","TallowDrift","Lanternwick","SunkenRelay")] string zone)
        {
            if(zone=="SunkenRelay"){Flag("relay.hessaConfronted");Flag("bossdown.HessaKorl");Flag("clear.Relay_Hessa");Flag("relay.hessaYielded");Flag("lanternwick.chartGiven");}
            yield return Zone(zone);yield return null;
            var bodies=new List<(Transform prop,Bounds b)>();
            foreach(var prop in Props())
                if(prop.GetComponentInChildren<Npc>(true)==null&&Drawn(prop,out var b)&&b.size.y>=.25f&&Mathf.Max(b.size.x,b.size.z)<=9)bodies.Add((prop,b));
            int people=0;
            foreach(var npc in All<Npc>())
            {
                if(!npc.gameObject.activeInHierarchy||npc.range<=0)continue;
                people++;var at=npc.transform.position;
                foreach(var (prop,b) in bodies)
                {
                    // The box round a turned prop is wider than the prop: the inner part of it, plus the person's own width.
                    bool inside=Mathf.Abs(at.x-b.center.x)<b.extents.x*.75f+.3f&&Mathf.Abs(at.z-b.center.z)<b.extents.z*.75f+.3f&&b.max.y>at.y+.3f&&b.min.y<at.y+1.8f;
                    if(inside)problems.Add($"{npc.speaker} stands inside {prop.name} at ({b.center.x:0.0}, {b.center.z:0.0})");
                }
            }
            Assert.Greater(people,0,"fixture: someone lives in "+zone);
            Verdict(zone+": people and props sharing a space");
        }
        [UnityTest] public IEnumerator TheTownsPlacesAreSignedBesideWhatTheyName()
        {
            yield return Zone("Lanternwick");
            var towers=All<RelayLantern>().Where(r=>r.alwaysLit).Select(r=>r.transform.position).ToArray();
            var archive=All<RelayLantern>().Where(r=>r.flag=="ishen.recordRestored").Select(r=>r.transform.position).ToArray();
            var trees=All<Transform>().Where(t=>t.name=="OrchardTree"||t.name=="DressFanShrub"&&t.position.x<-20).Select(t=>t.position).ToArray();
            var market=All<SceneryOcclusion>().Where(s=>s.name=="Market arch").Select(s=>s.transform.position).ToArray();
            foreach(var (word,things) in new[]{("LANTERN STAIR",towers),("ARCHIVE",archive),("ORCHARD",trees),("ARCH MARKET",market)})
            {
                if(things.Length==0){problems.Add($"there's nothing in town that is the {word}");continue;}
                var sign=All<TextMeshPro>().FirstOrDefault(t=>t.gameObject.activeInHierarchy&&t.text.Trim().ToUpperInvariant()==word);
                if(sign==null){problems.Add($"nothing in town says {word}");continue;}
                float gap=things.Min(t=>FlatDistance(t,sign.transform.position));
                if(gap>18)problems.Add($"the {word} sign stands {gap:0} m from the thing it names");
            }
            Verdict("Lanternwick's places");
        }
        static string[] ShopRows()=>All<TMP_Text>().Where(t=>t.gameObject.activeInHierarchy&&t.transform.parent!=null&&t.transform.parent.name.StartsWith("ShopRow")).Select(t=>t.text).ToArray();
        static string ShopHeading()=>All<TMP_Text>().First(t=>t.name=="Heading"&&t.gameObject.activeInHierarchy).text;
        [UnityTest] public IEnumerator CorrasOwnTalkOpensHerOwnStock()
        {
            Flag("met.Corra");Flag("met.Mira");
            yield return Zone("Lanternwick");
            var shop=Object.FindFirstObjectByType<ShopUi>();var corra=Person("Corra");
            heard=null;DialogueSystem.Current.AutoAdvance=true;corra.Interact();yield return null;
            float deadline=Time.unscaledTime+15;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;yield return null;yield return null;
            Assert.AreEqual("CorraRepeat",heard,"fixture: Corra's ordinary talk");
            Assert.IsTrue(shop.IsOpen,"talking to Corra opens no stall");
            StringAssert.Contains("ARCH MARKET",ShopHeading(),"Corra's own talk opens someone else's shop");
            var rows=ShopRows();
            foreach(string part in new[]{"Refined Edge","Refined Emitter","Refined Frame"})Assert.IsTrue(rows.Any(r=>r.Contains(part)),$"Corra doesn't offer {part}: "+string.Join(" | ",rows));
            foreach(string part in new[]{"Scrap Edge","Scrap Emitter","Scrap Frame"})Assert.IsFalse(rows.Any(r=>r.Contains(part)),$"Corra sells starter stock, {part}");
            shop.Close();yield return null;
            yield return Zone("Hub_Decks","Shop");
            shop=Object.FindFirstObjectByType<ShopUi>();var mira=Person("Mira");
            // Each zone builds its own DialogueSystem, so the new one has to be told too, or Mira's line waits for a press that never comes.
            heard=null;DialogueSystem.Current.AutoAdvance=true;mira.Interact();yield return null;
            deadline=Time.unscaledTime+15;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;yield return null;yield return null;
            Assert.AreEqual("MiraRepeat",heard,"fixture: Mira's ordinary talk");
            Assert.IsTrue(shop.IsOpen,"talking to Mira opens no shop");
            StringAssert.DoesNotContain("ARCH MARKET",ShopHeading(),"Mira's shop is headed with Corra's market");
            Assert.IsFalse(ShopRows().Any(r=>r.Contains("Refined")),"the Halo's outfitter sells Corra's Refined stock");
            shop.Close();
        }

        // ================================================================ 4.5 the Wick Reach
        static float StopAlong(Vector3 from,Vector3 way,float length,float hull)
        {
            foreach(var hit in Physics.SphereCastAll(from,hull,way,length,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                if(!IsBody(hit.collider))return hit.distance;
            return length;
        }
        [UnityTest] public IEnumerator TheCurtainClosesTheWholeReachUntilBothRelaysBurn()
        {
            yield return Zone("WickReach");yield return null;
            var gate=All<Membrane>().Single(m=>m.name=="Lantern gate");var dock=All<DockingPad>().Single();
            float hull=Mathf.Min(HeroCollision.HullRadius("Taren"),HeroCollision.HullRadius("Sela")),z=gate.transform.position.z;int lanes=0;
            // From the middle of the lane out to each side's limit, where a hull no longer fits.
            foreach(int side in new[]{-1,1})for(float x=0;x<90;x+=1)
            {
                var from=new Vector3(side*x,1.8f,z-8);
                if(Physics.OverlapSphere(from,hull,~0,QueryTriggerInteraction.Ignore).Any(c=>!IsBody(c)))break;
                lanes++;
                if(StopAlong(from,Vector3.forward,16,hull)>=16)problems.Add($"a hull slips past the closed curtain at x={side*x}");
            }
            Assert.Greater(lanes,50,"fixture: the reach has lanes to fly");
            Flag("wick.relay1");yield return null;yield return null;
            Assert.IsTrue(gate.gameObject.activeInHierarchy,"one relay opens the curtain the plan says takes two");
            yield return Zone("WickReach");yield return new WaitForSecondsRealtime(.35f);
            gate=All<Membrane>().Single(m=>m.name=="Lantern gate");
            Assert.IsTrue(gate.gameObject.activeInHierarchy,"one relay and a reload open the curtain");
            StringAssert.Contains("(1/2)",ObjectiveText(),"the count of relays is lost on a reload");
            Flag("wick.relay2");yield return new WaitForSecondsRealtime(.2f);
            Assert.IsTrue(Has("wick.gateOpen"),"the second relay doesn't open the curtain");Assert.IsFalse(gate.gameObject.activeInHierarchy,"the curtain still blocks the way");
            // With it open a ship can fly the lane to the dock, and stop within reach of it.
            dock=All<DockingPad>().Single();Physics.SyncTransforms();
            var start=new Vector3(dock.transform.position.x,1.8f,z-8);float length=dock.transform.position.z-start.z;
            float flown=StopAlong(start,Vector3.forward,length,HeroCollision.HullRadius("Taren"));
            var stop=start+Vector3.forward*flown;stop.y=1;
            Assert.Less(Vector3.Distance(stop,dock.transform.position),dock.range-.5f,$"flying up the open lane, a hull stops {flown:0} m on, out of the dock's reach");
            Verdict("The lantern gate");
        }
        [UnityTest] public IEnumerator TheWarpHomeWorksAfterTheChart()
        {
            foreach(var f in new[]{"lantern.routeKnown","wick.relay1","wick.relay2","wick.gateOpen","lanternwick.chartGiven","bossdown.Cantor","met.Keeper"})Flag(f);
            yield return Zone("WickReach","Lanternwick");ClearFights();yield return new WaitForSecondsRealtime(.35f);
            StringAssert.Contains("home",ObjectiveText(),"with the chart in hand the reach doesn't send the party home");
            var warp=WarpTo("TallowApproach");var hero=PartyController.Current.Active.transform.position;
            // Nothing solid lies in the long way back: a hull can fly it.
            var from=new Vector3(hero.x,1.8f,hero.z);var to=new Vector3(warp.transform.position.x,1.8f,warp.transform.position.z+warp.range);
            var way=to-from;float flown=StopAlong(from,way.normalized,way.magnitude,HeroCollision.HullRadius("Taren"));
            Assert.GreaterOrEqual(flown,way.magnitude-.5f,$"the way home is blocked {flown:0} m down the reach");
            warp.Interact();yield return null;
            float deadline=Time.unscaledTime+25;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual("TallowApproach",SceneFlow.Current.Zone,"the warp home doesn't go home");
            Assert.AreEqual("Wick",State.spawn,"the warp home arrives somewhere other than its own side of Tallow");
        }

        // ================================================================ 4.5 the gorge and the relay
        [UnityTest] public IEnumerator TheRelayDoorStaysShutUntilAllThreeRelaysBurnEvenAcrossAContinue()
        {
            RoadToAumi();
            yield return Zone("LanternGorge");
            for(int lit=0;lit<3;lit++)
            {
                var door=WarpTo("SunkenRelay");var lamp=All<Light>().Single(l=>l.name=="Relay door lamp");
                door.Interact();yield return null;yield return null;
                Assert.IsFalse(SceneFlow.Current.Loading,$"with {lit} of three relays burning the door lets the party through");
                StringAssert.Contains("relay",door.prompt,"the shut door doesn't say relays open it");
                Assert.IsFalse(lamp.gameObject.activeInHierarchy,"the door is lit before the relays burn");
                yield return new WaitForSecondsRealtime(.35f);
                StringAssert.Contains($"({lit}/3)",ObjectiveText(),"the objective has lost count of the relays");
                Assert.AreEqual(lit,State.questCounts.TryGetValue("LanternRoad/3",out int counted)?counted:0,"the quest has lost count of the relays");
                yield return Discover(Point("gorge.relay"+(lit+1)));
                Assert.IsTrue(Has("gorge.relay"+(lit+1)),"a relay won't relight");
                if(lit==1)
                {
                    log.Clear();
                    yield return SaveQuitAndContinue();
                    var lanterns=All<RelayLantern>().Where(r=>r.flag.StartsWith("gorge.relay")).ToDictionary(r=>r.flag);
                    Assert.IsTrue(lanterns["gorge.relay1"].Lit&&lanterns["gorge.relay2"].Lit,"a relit relay goes dark on Continue");
                    Assert.IsFalse(lanterns["gorge.relay3"].Lit,"the relay nobody has lit burns on Continue");
                    Assert.IsEmpty(log,"Continue plays the relighting again");
                    Assert.IsFalse(Point("gorge.relay1").Available,"a relit relay offers itself again");
                    Assert.AreEqual(3,Step("LanternRoad"),"Continue moves the step on");
                }
            }
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsTrue(Has("gorge.relaysLit"),"three relays don't open the door");
            Assert.AreEqual(4,Step("LanternRoad"),"three relays don't finish the step");
            var open=WarpTo("SunkenRelay");yield return null;yield return null;
            Assert.IsTrue(All<Light>().Single(l=>l.name=="Relay door lamp").gameObject.activeInHierarchy,"the door looks the same with the relays burning");
            StringAssert.Contains("Sunken Relay",open.prompt,"the open door still reads as shut");
            open.Interact();yield return null;
            float deadline=Time.unscaledTime+25;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual("SunkenRelay",SceneFlow.Current.Zone,"the open door leads nowhere");
        }
        [UnityTest] public IEnumerator TheCradleIsUnderAMinuteFromHessa()
        {
            yield return Zone("SunkenRelay");yield return null;
            var cradle=All<RepairBay>().Single();Assert.IsFalse(string.IsNullOrEmpty(cradle.saveSpawn),"the cradle saves no respawn beside it");
            var spawn=All<SpawnPoint>().First(s=>s.id==cradle.saveSpawn);var hessa=All<EnemyBrain>().First(e=>e.definition.id=="HessaKorl");
            Assert.IsTrue(NavMesh.SamplePosition(spawn.transform.position,out var from,2,NavMesh.AllAreas),"the cradle's respawn is off the floor");
            Assert.IsTrue(NavMesh.SamplePosition(hessa.transform.position,out var to,3,NavMesh.AllAreas),"Hessa stands off the floor");
            var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"nothing walkable joins the cradle to Hessa");
            float length=0;var corners=path.corners;for(int i=1;i<corners.Length;i++)length+=Vector3.Distance(corners[i-1],corners[i]);
            float seconds=length/PartyController.Current.Active.GetComponent<GroundMotor>().speed;
            Assert.Less(seconds,60,$"the cradle is a {seconds:0} s walk from Hessa, {length:0} m");
            cradle.Interact();yield return null;
            Assert.AreEqual(cradle.saveSpawn,Saves.Load("autosave").spawn,"using the cradle doesn't move the respawn beside it");
        }

        // ================================================================ 4.6 the script
        static IEnumerable<(string where,string text,bool label)> WordsOutsideTheWorld()
        {
            var table=Resources.Load<YarnProject>("Dialogue/Lattice").baseLocalization;
            foreach(var id in table.GetLineIDs())
            {
                string line=table.GetLocalizedString(id);if(string.IsNullOrEmpty(line))continue;
                yield return ("the script",Regex.Replace(line,@"^[A-Za-z]+:\s*",""),false);
            }
            string barks=Resources.Load<TextAsset>("Audio/barks").text;
            foreach(Match pair in Regex.Matches(barks,@"""[^""]+""\s*:\s*""([^""]*)"""))foreach(string line in pair.Groups[1].Value.Split('|'))yield return ("a bark",line,false);
            foreach(var quest in GameCatalog.All<QuestDef>())
            {
                yield return ("quest "+quest.id,quest.title,true);yield return ("quest "+quest.id,quest.description,true);
                foreach(string step in quest.stepText??new string[0])yield return ("quest "+quest.id,step,true);
                foreach(var reward in quest.rewards??new Ingredient[0])yield return ("quest "+quest.id,DisplayNames.Item(reward.id),true);
            }
            foreach(var item in GameCatalog.All<MaterialDef>())yield return ("material "+item.id,item.displayName,true);
            foreach(var item in GameCatalog.All<ConsumableDef>())yield return ("item "+item.id,item.displayName,true);
            foreach(var item in GameCatalog.All<TechPartDef>())yield return ("part "+item.id,item.displayName,true);
            foreach(var item in GameCatalog.All<RecipeDef>())yield return ("recipe "+item.id,item.displayName,true);
            foreach(var skill in GameCatalog.All<SkillDef>()){yield return ("skill "+skill.id,skill.displayName,true);yield return ("skill "+skill.id,skill.shortName,true);yield return ("skill "+skill.id,skill.description,true);}
            foreach(var shop in GameCatalog.All<ShopInventory>())yield return ("shop",shop.title,true);
            foreach(var enemy in GameCatalog.All<EnemyDef>())yield return ("target "+enemy.id,DisplayNames.Target(enemy.id),true);
            foreach(var zone in Zones)yield return ("zone name",DisplayNames.Zone(zone),true);
            foreach(var who in ScriptText().Split('\n').Select(l=>Regex.Match(l.Trim(),@"^([A-Za-z]+):\s").Groups[1].Value).Where(s=>s!=""&&s!="title").Distinct())yield return ("speaker "+who,DisplayNames.Speaker(who),true);
        }
        [UnityTest] public IEnumerator NoRawIdOrWrongWordInTheScriptOrTheData()
        {
            yield return null;int words=0;
            foreach(var (where,text,label) in WordsOutsideTheWorld()){words++;CheckWords(where,text,label);}
            Assert.Greater(words,300,"fixture: the game has words to read");
            Verdict("Words a player reads");
        }
        [UnityTest] public IEnumerator NoRawIdOrWrongWordOnAnyPromptOrSign()
        {
            int words=0;
            foreach(var zone in Zones)
            {
                yield return Zone(zone);yield return new WaitForSecondsRealtime(.35f);
                foreach(var prompt in All<InteractionPrompt>())
                {
                    words++;CheckWords(zone+" prompt",prompt.prompt,true);
                    if(prompt is WarpBeacon warp&&!string.IsNullOrEmpty(warp.requiredFlag))
                    {CheckWords(zone+" lock",warp.lockedPrompt,true);CheckWords(zone+" note",warp.unnamedPrompt,true);CheckWords(zone+" note",warp.oneWayNote,true);}
                }
                foreach(var text in All<TMP_Text>()){words++;CheckWords(zone+", "+text.name,text.text,true);}
            }
            Assert.Greater(words,200,"fixture: the zones have words to read");
            Verdict("Words on the world's prompts and signs");
        }
        sealed class Recorder:IDialogueView
        {
            readonly IDialogueView real;public readonly List<(string speaker,PortraitEmotion emotion,Sprite portrait,string body)> shown=new();
            public Recorder(IDialogueView real){this.real=real;}
            public bool AdvanceRequested=>real.AdvanceRequested;
            public void Show(string speaker,PortraitEmotion emotion,Sprite portrait,string body){shown.Add((speaker,emotion,portrait,body));real.Show(speaker,emotion,portrait,body);}
            public void SetVisibleCharacters(int count)=>real.SetVisibleCharacters(count);
            public void Hide()=>real.Hide();
            public void Options(IReadOnlyList<string> labels,System.Action<int> selected)=>real.Options(labels,selected);
            public void ClearOptions()=>real.ClearOptions();
        }
        [UnityTest] public IEnumerator TheFaceOnScreenFollowsEachLinesEmotion()
        {
            foreach(var (zone,node,speaker) in new[]{("Lanternwick","AumiFirst","Aumi"),("Lanternwick","AumiReturn","Aumi"),("SunkenRelay","HessaConfront","Hessa"),("TallowDrift","CompactHail","Keeper")})
            {
                yield return Zone(zone);
                var dialogue=DialogueSystem.Current;var form=ZoneController.Current.Form;var real=dialogue.View;var recorder=new Recorder(real);
                dialogue.View=recorder;dialogue.AutoAdvance=true;
                Assert.IsTrue(dialogue.StartNode(node,speaker),"fixture: "+node+" starts");yield return null;
                float deadline=Time.unscaledTime+25;while(dialogue.Running&&Time.unscaledTime<deadline)yield return null;
                dialogue.View=real;
                var script=LinesOf(node).ToArray();
                Assert.AreEqual(script.Length,recorder.shown.Count,node+": the panel showed a different number of lines from the script");
                var faces=new Dictionary<(string,PortraitEmotion),Sprite>();
                for(int i=0;i<script.Length;i++)
                {
                    var (who,text,emotion)=script[i];var seen=recorder.shown[i];
                    if(seen.emotion!=emotion)problems.Add($"{node} line {i+1}: tagged {emotion} and shown {seen.emotion}");
                    if(seen.portrait==null)problems.Add($"{node} line {i+1}: {who} has no face on screen");
                    else if(seen.portrait!=PortraitLookup.Get(who,form,emotion))problems.Add($"{node} line {i+1}: {who} shows the wrong sheet for {emotion} in {form}");
                    if(seen.speaker!=DisplayNames.Speaker(who))problems.Add($"{node} line {i+1}: the name plate reads {seen.speaker}");
                    if(seen.body!=text)problems.Add($"{node} line {i+1}: reads \"{seen.body}\", and the script says \"{text}\"");
                    faces[(who,emotion)]=seen.portrait;
                }
                foreach(var group in faces.GroupBy(f=>f.Key.Item1).Where(g=>g.Count()>1))
                    if(group.Select(f=>f.Value).Distinct().Count()<2)problems.Add($"{node}: {group.Key} is tagged with {group.Count()} moods and shows one face");
                // A hero fighting in the Shaped body talks with the Shaped face.
                if(form==BodyForm.Shaped)foreach(var (who,_,emotion) in script.Where(l=>l.speaker=="Taren"||l.speaker=="Sela"))
                    if(faces[(who,emotion)]==PortraitLookup.Get(who,BodyForm.Natural,emotion))problems.Add($"{node}: {who} speaks in the Shaped body with the Natural face");
            }
            Verdict("Portraits that don't follow their tags");
        }
        [UnityTest] public IEnumerator EveryCompulsoryConversationIsUnderNinetySeconds()
        {
            yield return null;
            foreach(string node in new[]{"OrrinFirst","SelaWalkwayRepair","SurvivorFirst","SelaNursery","SelaCantorPreview","KeeperFirst","SelaTallowLine","KeeperDriftReturn","CompactHail","AbriLanternTale","SelaWickRelays","AumiFirst","SelaRelayLit","HessaConfront","HessaYields","AumiReturn","AbriLanternHome"})
            {
                float seconds=0;int count=0;
                foreach(var (_,text,_) in LinesOf(node))
                {
                    count++;seconds+=1.5f; // A moment to read the line and press.
                    for(int i=0;i<text.Length;i++)seconds+=TypewriterPacing.DelayAfter(text,i);
                }
                Assert.Greater(count,0,"fixture: "+node+" has lines");
                Assert.Less(seconds,90,$"{node} holds the player for {seconds:0} s over {count} lines");
            }
        }
        [UnityTest] public IEnumerator TheStorysCausesComeInOrderAndEachIsSaidOutLoud()
        {
            yield return null;
            foreach(var (node,words,why) in new[]
            {
                ("SurvivorFirst","evacuation","nobody says Sorrel's signal is what brought the rig home"),
                ("SelaNursery","chart of the Gullet","the nursery isn't tied to the route"),
                ("SelaCantorPreview","nursery","the Cantor's collar isn't tied to the nursery"),
                ("KeeperDriftReturn","Tallow is moving","nobody says Tallow is moving"),
                ("KeeperDriftReturn","the bend from the nursery","Tallow moving isn't tied to the nursery"),
                ("CompactHail","food, power, and a route that waits","the Compact's three conditions are never read out"),
                ("AbriLanternTale","A route that waits","Lanternwick isn't tied to the condition it answers"),
                ("AumiReturn","A route that waits","the chart isn't tied to the condition it answers"),
                ("AbriLanternHome","Xylos","there's no hook to Xylos"),
                ("AbriLanternHome","Food","the hook to Xylos doesn't say why: food"),
            })StringAssert.Contains(words,Body(node),why);
            // And the world can only tell them in that order.
            var state=new GameState();
            state.flags["warpkey"]=true;Assert.IsFalse(ChapterProgress.CanEnterGullet(state),"the survey key alone skips the nursery");
            state.flags["hushwell.nursery"]=true;Assert.IsTrue(ChapterProgress.CanEnterGullet(state),"the nursery doesn't open the Gullet");
            foreach(var (flag,node) in new[]{("tallow.driftDiscussed","CompactHail"),("compact.hailHeard","AbriLanternTale")})
            {state.flags[flag]=true;Assert.AreEqual(node,SideStories.NodeFor("Keeper",state),"Abri's story comes out of order at "+flag);}
            state.questSteps["LanternRoad"]=2;Assert.AreEqual("AbriLanternWaiting",SideStories.NodeFor("Keeper",state),"Abri talks about the chart before there is one");
            state.flags["lanternwick.chartGiven"]=true;Assert.AreEqual("AbriLanternHome",SideStories.NodeFor("Keeper",state),"Abri never hears about the chart");
            state.flags["chapter2.homecoming"]=true;Assert.AreEqual("AbriLanternAfter",SideStories.NodeFor("Keeper",state),"the homecoming plays twice");
        }
        static IEnumerable<(string speaker,string node)> StoryBeats()
        {
            foreach(string field in new[]{"Beats","LateMeetings"})
            {
                var beats=(System.Array)typeof(SideStories).GetField(field,BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
                foreach(var beat in beats)yield return ((string)beat.GetType().GetField("speaker").GetValue(beat),(string)beat.GetType().GetField("node").GetValue(beat));
            }
        }
        [UnityTest] public IEnumerator EveryConversationLockAndQuestStepIsWiredToSomethingInTheWorld()
        {
            var compiled=new HashSet<string>(Resources.Load<YarnProject>("Dialogue/Lattice").NodeNames);string script=ScriptText();
            var quests=GameCatalog.All<QuestDef>();
            var speakers=new HashSet<string>();var reports=new HashSet<string>();var enemies=new HashSet<string>();var gathered=new HashSet<string>();var starters=new HashSet<string>();
            var settable=new HashSet<string>(Regex.Matches(script,@"<<setFlag\s+([^\s>]+)\s*>>").Cast<Match>().Select(m=>m.Groups[1].Value));
            foreach(var quest in quests)foreach(string flag in quest.flagsOnComplete??new string[0])settable.Add(flag);
            settable.Add("sliceComplete");
            var locks=new List<(string where,string flag)>();
            foreach(var zone in Zones)
            {
                yield return Zone(zone);
                void Asked(string who,string node){if(!string.IsNullOrEmpty(node)&&!compiled.Contains(node))problems.Add($"{zone}: {who} would say {node}, and the compiled script has no such conversation: nothing happens at all");}
                void Lock(string what,string flag){if(!string.IsNullOrEmpty(flag))locks.Add((zone+" "+what,flag));}
                foreach(var npc in All<Npc>())
                {
                    speakers.Add(npc.speaker);
                    foreach(string node in new[]{npc.firstNode,npc.repeatNode,npc.postNode,npc.finalNode,npc.finalRepeatNode,npc.holdNode})Asked(npc.speaker,node);
                    if(!string.IsNullOrEmpty(npc.postNode))Lock(npc.speaker+"'s later lines",npc.postFlag);
                    if(!string.IsNullOrEmpty(npc.finalNode))Lock(npc.speaker+"'s last lines",npc.finalFlag);
                    if(!string.IsNullOrEmpty(npc.finalCompletionFlag))settable.Add(npc.finalCompletionFlag);
                }
                foreach(var point in All<DiscoveryPoint>())
                {
                    Asked(point.name,point.dialogueNode);settable.Add(point.flag);if(!string.IsNullOrEmpty(point.report))reports.Add(point.report);Lock(point.name,point.requiredFlag);
                    if(!string.IsNullOrEmpty(point.situation)&&BarkService.Line(PartyController.Current.Active.character,point.situation)==null)problems.Add($"{zone}: {point.name} calls for a bark nobody has: {point.situation}");
                    if(string.IsNullOrEmpty(point.requiredQuest))continue;
                    var quest=quests.FirstOrDefault(q=>q.id==point.requiredQuest);
                    if(quest==null||point.requiredStep>=quest.steps.Length){problems.Add($"{zone}: {point.name} waits on a step of {point.requiredQuest} that doesn't exist");continue;}
                    var goal=quest.steps[point.requiredStep];
                    if(goal.kind!=ObjectiveKind.Interact||goal.target!=point.report)problems.Add($"{zone}: {point.name} reports {point.report} while {quest.id} step {point.requiredStep} waits for {goal.kind} {goal.target}: it can never count");
                }
                foreach(var cache in All<SalvageField>()){Asked(cache.name,cache.dialogueNode);Lock(cache.name,cache.requiredFlag);}
                foreach(var key in All<KeyPickup>()){gathered.Add(key.item);settable.Add(key.grantFlag);Lock(key.name,key.requiredFlag);}
                foreach(var node in All<MineNode>())gathered.Add(node.material);
                foreach(var fight in All<EncounterVolume>()){Lock(fight.name,fight.requiredFlag);foreach(var spawner in fight.spawners??new Spawner[0])if(spawner!=null&&spawner.definition!=null)enemies.Add(spawner.definition.id);}
                foreach(var starter in All<QuestStarter>())starters.Add(starter.questId);
                foreach(var all in All<FlagAll>()){settable.Add(all.result);foreach(string flag in all.flags)Lock(all.name,flag);}
                foreach(var gate in All<FlagGate>())Lock(gate.name,gate.flag);
                foreach(var warp in All<WarpBeacon>())Lock(warp.name,warp.requiredFlag);
                foreach(var relay in All<RelayLantern>())Lock(relay.name,relay.flag);
                foreach(var beacon in All<SignalBeacon>())Lock(beacon.name,beacon.flag);
                foreach(var hail in All<RadioHail>())
                {
                    settable.Add(hail.heardFlag);foreach(string need in hail.needs)Lock(hail.name,need);
                    foreach(string line in hail.situations)if(BarkService.Line(hail.speaker,line)==null)problems.Add($"{zone}: {hail.name} calls for a bark nobody has: {line}");
                }
            }
            foreach(var (speaker,node) in StoryBeats())
            {
                if(!compiled.Contains(node))problems.Add($"a side story would have {speaker} say {node}, and the compiled script has no such conversation");
                if(!speakers.Contains(speaker))problems.Add($"a side story waits on {speaker}, who stands nowhere in the world");
            }
            string[] always={"met.","bossdown.","clear.","quest.","mined.","cache.","opening."};
            foreach(var (where,flag) in locks)if(!settable.Contains(flag)&&!always.Any(flag.StartsWith))problems.Add($"{where} waits on {flag}, and nothing in the game ever sets it");
            foreach(var quest in quests)for(int i=0;i<quest.steps.Length;i++)
            {
                var goal=quest.steps[i];
                bool wired=goal.kind switch
                {
                    ObjectiveKind.TalkTo=>speakers.Contains(goal.target),ObjectiveKind.Reach=>Zones.Contains(goal.target),ObjectiveKind.Kill=>enemies.Contains(goal.target),
                    ObjectiveKind.Collect=>gathered.Contains(goal.target),ObjectiveKind.Interact=>reports.Contains(goal.target),ObjectiveKind.Flag=>settable.Contains(goal.target),_=>false,
                };
                if(!wired)problems.Add($"{quest.title} step {i+1} waits for {goal.kind} {goal.target}, and nothing in the world answers it");
            }
            // It starts where its giver says, and nowhere else.
            foreach(var (quest,node) in new[]{("Sorrel","OrrinFirst"),("MiraCrystals","MiraFirst"),("NeveParcel","NeveParcelOffer"),("SorrelBeacons","SurvivorBeaconsOffer"),("OdaBerth","OdaBerthOffer"),("LanternRoad","AbriLanternTale"),("PellsLamp","PellLampOffer"),("IshensRecord","IshenFirst"),("UnswornRations","CorraRationsOffer")})
            {
                if(!Body(node).Contains("<<startQuest "+quest+">>"))problems.Add($"{node} doesn't start {quest}");
                if(Regex.Matches(script,@"<<startQuest\s+"+quest+@"\s*>>").Count!=1)problems.Add($"{quest} starts from more than one conversation");
            }
            if(!starters.Contains("Hushwell"))problems.Add("nothing starts What the Drill Found on entering Hushwell");
            Assert.Greater(speakers.Count,10,"fixture: the world's people");
            Verdict("Conversations, locks and steps with nothing behind them");
        }

        // ================================================================ 4.7 quests: every step of all ten
        [UnityTest] public IEnumerator EveryQuestsCardsFollowItsStepsAndItsRewardIsPaidOnce()
        {
            yield return Zone("Hub_Decks","Office");yield return new WaitForSecondsRealtime(.3f);
            var toast=Object.FindFirstObjectByType<QuestToast>();Assert.IsNotNull(toast,"there are no quest cards");
            var timer=typeof(QuestToast).GetField("showing",BindingFlags.NonPublic|BindingFlags.Instance);Assert.IsNotNull(timer,"fixture: the card's own timer has moved");
            var cards=new List<(string heading,string body)>();
            // A card stays up for seconds: this reads each off the real panel and sends it on.
            IEnumerator Read()
            {
                cards.Clear();float until=Time.unscaledTime+.5f;
                while(Time.unscaledTime<until)
                {
                    yield return null;
                    if(toast.Showing&&toast.Heading.Contains("QUEST")&&!cards.Contains((toast.Heading,toast.Body)))cards.Add((toast.Heading,toast.Body));
                    timer.SetValue(toast,0f);
                }
            }
            var quests=GameCatalog.All<QuestDef>();Assert.AreEqual(10,quests.Length,"the plan has ten quests");
            foreach(var quest in quests)
            {
                void Expect(string heading,string words)
                {
                    if(!cards.Any(c=>c.heading==heading&&c.body.Contains(quest.title)&&c.body.Contains(words)))
                        problems.Add($"{quest.title}: no {heading} card saying \"{words}\"; saw: "+(cards.Count==0?"nothing":string.Join(" | ",cards.Select(c=>c.heading+": "+c.body.Replace("\n"," / ")))));
                    foreach(var (_,body) in cards)CheckWords(quest.title+" card",body,true);
                }
                if(string.IsNullOrWhiteSpace(quest.title)||string.IsNullOrWhiteSpace(quest.description))problems.Add(quest.id+" has no title or description for the log");
                if(quest.stepText==null||quest.stepText.Length!=quest.steps.Length||quest.stepText.Any(string.IsNullOrWhiteSpace)){problems.Add(quest.title+": a step has no text, and the log falls back to 'Step 1 of 2'");continue;}
                ForgetGathered(quest);
                int scrip=State.scrip,worth=Worth;var before=(quest.rewards??new Ingredient[0]).ToDictionary(r=>r.id,r=>Held(r.id));
                RpgServices.Quests.Start(quest.id);yield return Read();
                Expect("NEW QUEST",quest.stepText[0]);
                for(int step=0;step<quest.steps.Length;step++)
                {
                    var goal=quest.steps[step];int count=Mathf.Max(1,goal.count);
                    for(int done=1;done<=count;done++)
                    {
                        Deed(goal,1);State.zone="Hub_Decks";yield return Read();
                        if(done<count){Expect("QUEST UPDATED",$"({done}/{count})");if(Step(quest.id)!=step)problems.Add($"{quest.title}: step {step+1} moved on at {done} of {count}");}
                    }
                    if(step+1<quest.steps.Length){Expect("QUEST UPDATED",quest.stepText[step+1]);if(Step(quest.id)!=step+1)problems.Add($"{quest.title}: step {step+1} done and the quest stands at {Step(quest.id)+1}");}
                }
                Expect("QUEST COMPLETE","+"+quest.rewardXp+" XP");Expect("QUEST COMPLETE","+"+quest.rewardScrip+" scrip");
                foreach(var reward in quest.rewards??new Ingredient[0])
                {
                    Expect("QUEST COMPLETE",DisplayNames.Item(reward.id));
                    if(Held(reward.id)!=before[reward.id]+Mathf.Max(1,reward.count))problems.Add($"{quest.title}: the card names {DisplayNames.Item(reward.id)}, and the bag went from {before[reward.id]} to {Held(reward.id)}");
                }
                if(!Has("quest."+quest.id+".complete"))problems.Add(quest.title+" finishes all its steps and isn't marked complete");
                foreach(string flag in quest.flagsOnComplete??new string[0])if(!Has(flag))problems.Add($"{quest.title} never sets {flag}");
                if(State.scrip!=scrip+quest.rewardScrip)problems.Add($"{quest.title} pays {State.scrip-scrip} scrip and its card says {quest.rewardScrip}");
                if(quest.rewardXp>0&&Worth<=worth)problems.Add(quest.title+" pays no XP");
                // Doing it all again changes nothing.
                scrip=State.scrip;worth=Worth;RpgServices.Quests.Start(quest.id);
                foreach(var goal in quest.steps)Deed(goal,Mathf.Max(1,goal.count));
                State.zone="Hub_Decks";yield return Read();
                if(State.scrip!=scrip||Worth!=worth)problems.Add(quest.title+" pays its reward again");
                if(cards.Count>0)problems.Add($"{quest.title} announces itself again: {cards[0].heading}");
            }
            Verdict("What the quest cards and rewards get wrong");
        }
        IEnumerator SavesAndComesBack(QuestDef quest,int step,int done)
        {
            var before=State;int scrip=before.scrip;var flags=new Dictionary<string,bool>(before.flags);string text=QuestText.Step(quest,Mathf.Min(step,quest.steps.Length),before);
            Saves.Save("autosave",before);var back=Saves.Load("autosave");
            if(back==null){problems.Add($"{quest.title} step {step+1}: the save can't be read back");yield break;}
            GameServices.Current.State=back;yield return null;
            var bound=RpgServices.Quests; // The load's own look over every quest happens here.
            if(Step(quest.id)!=step)problems.Add($"{quest.title}: saved at step {step+1} and comes back at step {Step(quest.id)+1}");
            if(done>0){back.questCounts.TryGetValue(quest.id+"/"+step,out int kept);if(kept!=done)problems.Add($"{quest.title}: saved at {done} of a count and comes back at {kept}");}
            if(back.scrip!=scrip)problems.Add($"{quest.title}: the scrip goes from {scrip} to {back.scrip} over a save at step {step+1}");
            foreach(var pair in flags)if(pair.Value&&(!back.flags.TryGetValue(pair.Key,out bool same)||!same))problems.Add($"{quest.title}: {pair.Key} is lost over a save at step {step+1}");
            if(QuestText.Step(quest,Mathf.Min(step,quest.steps.Length),back)!=text)problems.Add($"{quest.title}: the log reads differently after a save at step {step+1}");
        }
        [UnityTest] public IEnumerator EveryQuestKeepsItsPlaceThroughASaveAtEveryStep()
        {
            yield return Zone("Hub_Decks","Office");
            foreach(var quest in GameCatalog.All<QuestDef>())
            {
                ForgetGathered(quest);RpgServices.Quests.Start(quest.id);
                for(int step=0;step<=quest.steps.Length;step++)
                {
                    yield return SavesAndComesBack(quest,step,0);
                    if(step==quest.steps.Length)break;
                    var goal=quest.steps[step];int count=Mathf.Max(1,goal.count);
                    for(int done=1;done<count;done++){Deed(goal,1);yield return SavesAndComesBack(quest,step,done);}
                    Deed(goal,1);
                }
                State.zone="Hub_Decks";
            }
            Verdict("What a save and Continue does to a quest");
        }
        [UnityTest] public IEnumerator NoQuestMovesOnOutOfTurn()
        {
            yield return Zone("Hub_Decks","Office");
            foreach(var quest in GameCatalog.All<QuestDef>())
            {
                ForgetGathered(quest);RpgServices.Quests.Start(quest.id);int scrip=State.scrip;
                for(int step=0;step<quest.steps.Length;step++)
                {
                    for(int other=0;other<quest.steps.Length;other++)
                    {
                        if(other==step)continue;
                        var wrong=quest.steps[other];RpgServices.Quests.Report(wrong.kind,wrong.target,Mathf.Max(1,wrong.count));
                        if(Step(quest.id)!=step)problems.Add($"{quest.title}: at step {step+1}, doing step {other+1}'s work moved it to step {Step(quest.id)+1}");
                    }
                    if(Has("quest."+quest.id+".complete"))problems.Add($"{quest.title} is complete at step {step+1}");
                    if(State.scrip!=scrip)problems.Add($"{quest.title} pays out at step {step+1}");
                    Deed(quest.steps[step],Mathf.Max(1,quest.steps[step].count));
                }
                State.zone="Hub_Decks";
            }
            Verdict("Quests that can be done out of order");
        }
        [Test] public void NobodyPlaysTheirHandBackBeforeItsStep()
        {
            foreach(var (quest,speaker,node) in new[]{("NeveParcel","Neve","NeveParcelReturn"),("OdaBerth","Oda","OdaBerthReturn"),("SorrelBeacons","Survivor","SurvivorBeaconsDone"),("PellsLamp","Pell","PellLampReturn"),("IshensRecord","Ishen","IshenRecordReturn"),("UnswornRations","Brekk","BrekkRations")})
                foreach(int step in new[]{-1,0,1,2})
                {
                    var state=new GameState();state.flags["met."+speaker]=true;if(step>=0)state.questSteps[quest]=step;
                    string said=SideStories.NodeFor(speaker,state);
                    if(step==1)Assert.AreEqual(node,said,$"{speaker} doesn't take back {quest} when it's ready");
                    else Assert.AreNotEqual(node,said,$"{speaker} takes back {quest} at step {step}");
                }
        }
        [UnityTest] public IEnumerator ThePickUpsRefuseUntilTheirQuestsAsk()
        {
            foreach(var (zone,points) in new[]{("LanternGorge",new[]{"gorge.relay1","gorge.relay2","gorge.relay3","pell.lampFound","unsworn.rationsFound"}),("SunkenRelay",new[]{"relay.plate1","relay.plate2","relay.plate3"}),("Sorrel_Ridges",new[]{"sorrel.beacon.1","sorrel.beacon.2","sorrel.beacon.3","sorrel.beacon.4"})})
            {
                yield return Zone(zone);yield return null;
                foreach(string flag in points)
                {
                    var point=Point(flag);
                    if(point.Available)problems.Add($"{zone}: {point.name} is on offer before its quest asks");
                    point.Interact();yield return null;
                    if(Has(flag))problems.Add($"{zone}: {point.name} can be taken before its quest asks");
                }
                if(zone=="Sorrel_Ridges")
                {
                    var key=All<KeyPickup>().Single();
                    if(key.Available)problems.Add("the survey key is on offer with the Burrower alive");
                    key.Interact();yield return null;
                    if(Has("warpkey"))problems.Add("the survey key can be taken with the Burrower alive");
                }
            }
            Verdict("Things a player can take too early");
        }
        [UnityTest] public IEnumerator TalkingEarlyHandsNothingOver()
        {
            RoadToAumi();Flag("met.Pell");Flag("met.Ishen");Flag("met.Brekk");
            foreach(var f in new[]{"gorge.relay1","gorge.relay2","gorge.relay3","gorge.relaysLit"})Flag(f);RpgServices.Quests.Report(ObjectiveKind.Interact,"GorgeRelay",3);
            yield return Zone("Lanternwick");
            yield return Talk(Person("Aumi"));
            // The relays burn in this fixture, and she says so. QaFlowTests has her asking for them while they're dark.
            Assert.AreEqual("AumiRelaysLit",heard,"Aumi moves on before the captain has been dealt with, or goes on asking for relays already burning");
            Assert.IsFalse(Has("lanternwick.chartGiven"),"Aumi hands over the chart early");
            yield return Talk(Person("Brekk"));Assert.AreEqual("BrekkRepeat",heard,"Brekk thanks the party for rations nobody brought");
            yield return Talk(Person("Ishen"));Assert.AreEqual("IshenRepeat",heard,"Ishen thanks the party for plates nobody brought");
            yield return Zone("TallowDrift");
            foreach(var f in new[]{"met.Keeper","tallow.lineObserved","tallow.driftDiscussed"})Flag(f);Flag("compact.hailHeard");
            yield return Talk(Person("Keeper"));
            Assert.AreEqual("AbriLanternWaiting",heard,"Abri closes the chapter with no chart");
            Assert.IsFalse(Has("chapter2.homecoming"),"the chapter ends with no chart");
        }
        // ---------------------------------------------------------------- each quest walked through the world, across a real Continue
        [UnityTest] public IEnumerator TheSilentOutpostKeepsItsPlaceThroughContinue()
        {
            yield return Zone("Hub_Decks","Office");
            yield return Talk(Person("Orrin"));
            Assert.AreEqual("OrrinFirst",heard,"fixture: Orrin's briefing");Assert.AreEqual(0,Step("Sorrel"),"Orrin's briefing doesn't start the quest he gives");
            yield return Zone("Sorrel_Ridges");Assert.AreEqual(1,Step("Sorrel"),"landing on Sorrel doesn't move the quest on");
            yield return SaveQuitAndContinue();Assert.AreEqual(1,Step("Sorrel"),"Continue moves the quest");
            yield return Talk(Person("Survivor"));
            Assert.AreEqual("SurvivorFirst",heard,"fixture: Dace's introduction");Assert.AreEqual(2,Step("Sorrel"),"meeting Dace doesn't move the quest on");
            yield return SaveQuitAndContinue();Assert.AreEqual(2,Step("Sorrel"),"Continue moves the quest");
            var fight=All<EncounterVolume>().Single(e=>e.encounterId=="Sorrel_Burrower");var lead=PartyController.Current.Active;
            foreach(var hero in PartyController.Current.members){Put(hero,new Vector3(hero==lead?2:-2,.2f,162));hero.Health.maximum=hero.Health.integrity=9000;}
            fight.Begin();yield return null;
            var rig=All<EnemyBrain>().Single(e=>e.definition.id=="Burrower");rig.Passive=true;yield return null;
            var body=rig.gameObject;rig.Health.Receive(new DamagePacket{source=lead.Health,amount=rig.Health.maximum*50,type=DamageType.Plasma});yield return null;
            Assert.IsTrue(Has("bossdown.Burrower"),"fixture: the Burrower is down");Assert.AreEqual(3,Step("Sorrel"),"beating the Burrower doesn't move the quest on");
            float deadline=Time.unscaledTime+10;while(body!=null&&Time.unscaledTime<deadline)yield return null;
            var key=All<KeyPickup>().Single();Assert.IsTrue(key.Available,"the survey key isn't on offer after the Burrower");
            int scrip=State.scrip;key.Interact();yield return null;
            Assert.AreEqual(4,Step("Sorrel"),"recovering the key doesn't finish the quest");Assert.IsTrue(Has("warpkey"),"the key isn't held");
            Assert.AreEqual(scrip+GameCatalog.Find<QuestDef>("Sorrel").rewardScrip,State.scrip,"the reward isn't paid");
            scrip=State.scrip;
            yield return SaveQuitAndContinue();
            Assert.AreEqual(4,Step("Sorrel"),"Continue moves the finished quest");Assert.AreEqual(scrip,State.scrip,"Continue pays the reward again");
            Assert.IsFalse(All<KeyPickup>().Any(k=>k.visual.activeInHierarchy||k.Available),"the key is back in its cradle on Continue");
            yield return Zone("Hub_Decks","Office");
            yield return Talk(Person("Orrin"));Assert.AreEqual("OrrinPost",heard,"Orrin doesn't notice the job is done");
        }
        [UnityTest] public IEnumerator MirasCrystalsCountThroughContinueAndPayOnce()
        {
            State.materials.Remove("RidgeCrystal");State.collectedMaterials.Remove("RidgeCrystal");
            yield return Zone("Hub_Decks","Shop");
            yield return Talk(Person("Mira"));
            Assert.AreEqual("MiraFirst",heard,"fixture: Mira's first talk");Assert.AreEqual(0,Step("MiraCrystals"),"Mira's request doesn't start the quest");
            yield return Zone("Sorrel_Ridges");
            var nodes=All<MineNode>().Where(n=>n.material=="RidgeCrystal").ToArray();Assert.GreaterOrEqual(nodes.Length,6,"Sorrel hasn't six crystals to mine");
            foreach(var node in nodes.Take(2)){node.Interact();yield return null;}
            string[] taken=nodes.Take(2).Select(n=>n.nodeId).ToArray();
            yield return SaveQuitAndContinue();
            Assert.AreEqual(2,State.questCounts.TryGetValue("MiraCrystals/0",out int kept)?kept:0,"the crystals' count is lost on Continue");
            nodes=All<MineNode>().Where(n=>n.material=="RidgeCrystal").ToArray();
            Assert.IsFalse(nodes.Any(n=>taken.Contains(n.nodeId)&&n.gameObject.activeInHierarchy),"mined crystals are whole again on Continue");
            yield return new WaitForSecondsRealtime(.35f);
            StringAssert.Contains("(2/6)",ObjectiveText(),"the panel has lost the count of Mira's crystals");
            int scrip=State.scrip;
            foreach(var node in nodes.Where(n=>n.gameObject.activeInHierarchy).Take(4).ToArray()){node.Interact();yield return null;}
            Assert.IsTrue(Has("quest.MiraCrystals.complete"),"six crystals don't finish the quest");
            int paid=GameCatalog.Find<QuestDef>("MiraCrystals").rewardScrip;Assert.AreEqual(scrip+paid,State.scrip,"the reward isn't paid");scrip=State.scrip;
            yield return SaveQuitAndContinue();Assert.AreEqual(scrip,State.scrip,"Continue pays Mira's reward again");
            yield return Zone("Hub_Decks","Shop");
            yield return Talk(Person("Mira"));Assert.AreEqual("MiraPost",heard,"Mira doesn't notice the crystals");Assert.AreEqual(scrip,State.scrip,"talking to Mira pays again");
        }
        [UnityTest] public IEnumerator TheQuietRidgesKeepTheirPlaceThroughContinue()
        {
            Flag("met.Survivor");Flag("bossdown.Burrower");Flag("clear.Sorrel_Burrower");
            yield return Zone("Sorrel_Ridges");
            yield return Talk(Person("Survivor"));Assert.AreEqual("SurvivorBeaconsOffer",heard,"fixture: Dace asks");Assert.AreEqual(0,Step("SorrelBeacons"),"Dace's request doesn't start the quest");
            var beacons=All<SignalBeacon>().OrderBy(b=>b.flag).ToArray();
            for(int i=0;i<2;i++){beacons[i].GetComponent<DiscoveryPoint>().Interact();yield return null;}
            yield return SaveQuitAndContinue();
            beacons=All<SignalBeacon>().OrderBy(b=>b.flag).ToArray();yield return null;
            for(int i=0;i<4;i++)if(beacons[i].Quiet!=(i<2))problems.Add($"beacon {i+1} {(i<2?"calls again":"has gone quiet")} on Continue");
            Assert.AreEqual(2,State.questCounts.TryGetValue("SorrelBeacons/0",out int kept)?kept:0,"the count of beacons is lost on Continue");
            for(int i=0;i<4;i++)if(beacons[i].GetComponent<DiscoveryPoint>().Available!=(i>=2))problems.Add($"beacon {i+1} offers the wrong thing on Continue");
            for(int i=2;i<4;i++){beacons[i].GetComponent<DiscoveryPoint>().Interact();yield return null;}
            Assert.AreEqual(1,Step("SorrelBeacons"),"four beacons don't move the quest on");
            yield return SaveQuitAndContinue();Assert.AreEqual(1,Step("SorrelBeacons"),"Continue moves the quest");
            int frames=State.parts.Count(p=>p.definitionId=="FramesT2");
            yield return Talk(Person("Survivor"));Assert.AreEqual("SurvivorBeaconsDone",heard,"Dace doesn't hear the ridges are quiet");
            Assert.AreEqual(frames+1,State.parts.Count(p=>p.definitionId=="FramesT2"),"the plating doesn't arrive");
            yield return SaveQuitAndContinue();
            Assert.AreEqual(frames+1,State.parts.Count(p=>p.definitionId=="FramesT2"),"Continue changes the plating");
            yield return Talk(Person("Survivor"));Assert.AreNotEqual("SurvivorBeaconsDone",heard,"Dace pays out twice");
            Verdict("Quiet Ridges over a Continue");
        }
        [UnityTest] public IEnumerator PellsLampRunsFromPellToTheDenAndBack()
        {
            RoadToAumi();
            yield return Zone("Lanternwick");
            var pell=Person("Pell");
            yield return Talk(pell);Assert.AreEqual("PellFirst",heard,"fixture: Pell's introduction");
            yield return Talk(pell);Assert.AreEqual("PellLampOffer",heard,"Pell never asks about her lamp");Assert.AreEqual(0,Step("PellsLamp"),"her request doesn't start the quest");
            yield return Zone("LanternGorge");yield return null;
            var den=Point("pell.lampFound");var relay=Point("gorge.relay2");
            Assert.Less(den.transform.position.x,relay.transform.position.x,"the log says the den is west of the path, and it isn't");
            Assert.Less(den.transform.position.z,relay.transform.position.z,"the log says the den comes before the second relay, and it doesn't");
            var tower=All<Transform>().Where(t=>t.name=="Fallen relay tower").OrderBy(t=>FlatDistance(t.position,den.transform.position)).FirstOrDefault();
            Assert.IsNotNull(tower,"the log says the den is under a fallen tower, and there's none");
            Assert.Less(FlatDistance(tower.position,den.transform.position),25,"the log says the den is under the fallen tower, and the tower is far from it");
            Assert.IsTrue(den.Available,"the lamp can't be taken while its quest asks");
            den.Interact();yield return null;yield return null;
            Assert.AreEqual(1,Step("PellsLamp"),"taking the lamp doesn't move the quest on");
            Assert.IsFalse(den.visual.activeSelf,"the lamp stays on the floor after it's taken");
            var saved=Saves.Load("autosave");Assert.IsTrue(saved!=null&&saved.flags.TryGetValue("pell.lampFound",out bool found)&&found,"taking the lamp isn't saved");
            yield return SaveQuitAndContinue();yield return null;
            den=Point("pell.lampFound");Assert.AreEqual(1,Step("PellsLamp"),"Continue moves the quest");Assert.IsFalse(den.visual.activeSelf,"the lamp is back on the floor on Continue");
            Assert.IsFalse(den.Available,"the lamp can be taken twice");
            yield return Zone("Lanternwick");
            int scrip=State.scrip,worth=Worth;State.consumables.TryGetValue("ChargeCell",out int cells);
            yield return Talk(Person("Pell"));Assert.AreEqual("PellLampReturn",heard,"Pell doesn't take her lamp back");
            Assert.IsTrue(Has("quest.PellsLamp.complete"),"the quest doesn't complete");
            var quest=GameCatalog.Find<QuestDef>("PellsLamp");
            Assert.AreEqual(scrip+quest.rewardScrip,State.scrip,"the scrip isn't paid");Assert.Greater(Worth,worth,"the XP isn't paid");Assert.AreEqual(cells+2,State.consumables["ChargeCell"],"the charge cells she promises don't arrive");
            yield return Talk(Person("Pell"));Assert.AreEqual("PellLampAfter",heard,"nothing changes in what she says");Assert.AreEqual(scrip+quest.rewardScrip,State.scrip,"she pays twice");
        }
        [UnityTest] public IEnumerator PellCanStillTakeHerLampBackOnTallow()
        {
            RoadToAumi();Flag("met.Pell");Flag("pell.lampFound");RpgServices.Quests.Start("PellsLamp");RpgServices.Quests.Report(ObjectiveKind.Interact,"PellsLamp");
            Flag("lanternwick.chartGiven");Flag("met.Keeper");Assert.AreEqual(1,Step("PellsLamp"),"fixture: the lamp is in hand");
            yield return Zone("TallowDrift");yield return null;yield return null;
            var pell=Person("Pell");Assert.IsTrue(pell.gameObject.activeInHierarchy,"she isn't aboard with the chart given");
            yield return new WaitForSecondsRealtime(.35f);
            StringAssert.Contains("Pell",ObjectiveText(),"the panel doesn't point at Pell with her lamp in hand and her standing here");
            yield return Talk(pell);Assert.AreEqual("PellLampReturn",heard,"she moved to Tallow, and the lamp quest can no longer finish");
            Assert.IsTrue(Has("quest.PellsLamp.complete"),"the quest doesn't complete on Tallow");
            yield return Talk(pell);Assert.AreEqual("PellTallow",heard,"she doesn't go back to being aboard");
            yield return Zone("Lanternwick");yield return null;yield return null;
            Assert.IsFalse(Person("Pell").gameObject.activeInHierarchy,"she's in two places");
        }
        [UnityTest] public IEnumerator IshensRecordRunsFromTheArchiveToTheRelayAndBack()
        {
            yield return Zone("Lanternwick");yield return null;
            var lantern=All<RelayLantern>().Single(r=>r.flag=="ishen.recordRestored");Assert.IsFalse(lantern.Lit,"the archive's lantern burns before the record comes home");
            yield return Talk(Person("Ishen"));Assert.AreEqual("IshenFirst",heard,"fixture: Ishen's introduction");Assert.AreEqual(0,Step("IshensRecord"),"Ishen's request doesn't start the quest");
            yield return Zone("SunkenRelay");yield return null;yield return null;
            foreach(string flag in new[]{"relay.plate1","relay.plate2"}){Assert.IsTrue(Point(flag).visual.activeSelf,flag+" isn't out for the quest");Point(flag).Interact();yield return null;yield return null;Assert.IsFalse(Point(flag).visual.activeSelf,flag+" stays there after it's taken");}
            yield return SaveQuitAndContinue();yield return null;yield return null;
            Assert.AreEqual(0,Step("IshensRecord"),"Continue moves the quest");
            Assert.AreEqual(2,State.questCounts.TryGetValue("IshensRecord/0",out int kept)?kept:0,"the count of plates is lost on Continue");
            Assert.IsFalse(Point("relay.plate1").visual.activeSelf||Point("relay.plate2").visual.activeSelf,"a plate already taken is back on its stand on Continue");
            Assert.IsTrue(Point("relay.plate3").visual.activeSelf,"the plate still to take has gone on Continue");
            Assert.IsFalse(Point("relay.plate1").Available,"a plate can be taken twice");
            Point("relay.plate3").Interact();yield return null;yield return null;
            Assert.AreEqual(1,Step("IshensRecord"),"three plates don't move the quest on");
            yield return Zone("Lanternwick");yield return null;
            int scrip=State.scrip,lenses=State.parts.Count(p=>p.definitionId=="EmittersT2");
            yield return Talk(Person("Ishen"));Assert.AreEqual("IshenRecordReturn",heard,"Ishen doesn't take the plates");
            Assert.IsTrue(Has("quest.IshensRecord.complete"),"the quest doesn't complete");
            Assert.AreEqual(scrip+GameCatalog.Find<QuestDef>("IshensRecord").rewardScrip,State.scrip,"the scrip isn't paid");
            Assert.AreEqual(lenses+1,State.parts.Count(p=>p.definitionId=="EmittersT2"),"the emitter Ishen hands over doesn't arrive");
            yield return null;lantern=All<RelayLantern>().Single(r=>r.flag=="ishen.recordRestored");
            Assert.IsTrue(lantern.Lit&&lantern.glow.enabled,"'the Great Lantern's lit again', and nothing changes at the archive");
            yield return Talk(Person("Ishen"));Assert.AreEqual("IshenPost",heard,"nothing changes in what Ishen says");
            scrip=State.scrip;yield return SaveQuitAndContinue();yield return null;
            Assert.AreEqual(scrip,State.scrip,"Continue pays Ishen's reward again");
            Assert.IsTrue(All<RelayLantern>().Single(r=>r.flag=="ishen.recordRestored").Lit,"the archive goes dark again on Continue");
        }
        [UnityTest] public IEnumerator TheRationsRunFromCorraToTheCampToBrekk()
        {
            RoadToAumi();
            yield return Zone("Lanternwick");
            yield return Talk(Person("Corra"));Assert.AreEqual("CorraFirst",heard,"fixture: Corra's introduction");
            yield return Talk(Person("Corra"));Assert.AreEqual("CorraRationsOffer",heard,"Corra never asks about the rations");Assert.AreEqual(0,Step("UnswornRations"),"her request doesn't start the quest");
            yield return Talk(Person("Brekk"));Assert.AreEqual("BrekkFirst",heard,"Brekk thanks the party for rations they haven't brought");
            yield return Zone("LanternGorge");yield return null;yield return null;
            var crate=Point("unsworn.rationsFound");Assert.IsTrue(crate.visual.activeSelf,"the crate isn't out for the quest");
            crate.Interact();yield return null;yield return null;
            Assert.AreEqual(1,Step("UnswornRations"),"taking the crate doesn't move the quest on");Assert.IsFalse(crate.visual.activeSelf,"the crate stays by the fire after it's taken");
            yield return SaveQuitAndContinue();yield return null;yield return null;
            Assert.AreEqual(1,Step("UnswornRations"),"Continue moves the quest");Assert.IsFalse(Point("unsworn.rationsFound").visual.activeSelf,"the crate is back by the fire on Continue");
            yield return Zone("Lanternwick");
            int scrip=State.scrip,edges=State.parts.Count(p=>p.definitionId=="EdgesT2");
            yield return Talk(Person("Brekk"));Assert.AreEqual("BrekkRations",heard,"Brekk doesn't take the rations");
            Assert.IsTrue(Has("quest.UnswornRations.complete"),"the quest doesn't complete");
            Assert.AreEqual(scrip+GameCatalog.Find<QuestDef>("UnswornRations").rewardScrip,State.scrip,"the scrip isn't paid");
            Assert.AreEqual(edges+1,State.parts.Count(p=>p.definitionId=="EdgesT2"),"the edge Brekk hands over doesn't arrive");
            yield return Talk(Person("Brekk"));Assert.AreEqual("BrekkPost",heard,"nothing changes in what Brekk says");
            yield return Talk(Person("Corra"));Assert.AreEqual("CorraPost",heard,"nothing changes in what Corra says");
            Assert.AreEqual(scrip+GameCatalog.Find<QuestDef>("UnswornRations").rewardScrip,State.scrip,"they pay twice");
        }
        [UnityTest] public IEnumerator TheDecksGiversChangeTheirWordsAfterwards()
        {
            foreach(var f in new[]{"met.Orrin","met.Mira","warpkey"})Flag(f);
            yield return Zone("Hub_Decks","Office");
            yield return Talk(Person("Orrin"));Assert.AreEqual("OrrinPost",heard,"Orrin doesn't notice the survey key");
            Flag("bossdown.Cantor");yield return Talk(Person("Orrin"));Assert.AreEqual("OrrinAfterCantor",heard,"Orrin doesn't notice the Gullet is open");
            yield return Talk(Person("Mira"));Assert.AreEqual("MiraRepeat",heard,"fixture: Mira's ordinary line");
            Flag("quest.MiraCrystals.complete");yield return Talk(Person("Mira"));Assert.AreEqual("MiraPost",heard,"Mira doesn't notice the crystals");
        }
        [UnityTest] public IEnumerator TheChaptersEndIsStillThereAfterQuittingAtItsCard()
        {
            foreach(var f in new[]{"met.Keeper","sliceComplete","tallow.lineObserved","tallow.driftDiscussed","compact.hailHeard","lanternwick.chartGiven","lantern.routeKnown"})Flag(f);
            yield return Zone("TallowDrift");
            yield return Talk(Person("Keeper"));Assert.AreEqual("AbriLanternHome",heard,"fixture: the homecoming");Assert.IsTrue(Has("chapter2.homecoming"),"fixture: the chapter ends");
            // Nothing else happens first: the card says the story stops here, and the player quits.
            var saved=Saves.Load("autosave");
            Assert.IsTrue(saved!=null&&saved.flags.TryGetValue("chapter2.homecoming",out bool ended)&&ended,"the card says CHAPTER 2 COMPLETE and the story stops here, and the save doesn't know: quit now and Continue asks for the chart again and replays the scene");
        }
    }
}
