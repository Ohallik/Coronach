using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEditor;
using UnityEngine;
namespace Lattice.EditorTools
{
    /// <summary>
    /// The first two chapters' content, said in data as every chapter since says its own: each zone's name, music, bright floor and chapter; each enemy's
    /// drawn scale, idle motion, death sound and name; which quests are the story's. The code still knows all of it for these thirteen zones, fourteen enemies
    /// and ten quests (MusicDirector, DisplayNames, SignStyle, ActorFactory, EnemyBrain, StoryQuests), so nothing plays differently before or after: this only
    /// makes the definitions agree with the code, so a tool or a test that reads the data reads the truth. <see cref="Apply"/> stamps the assets that exist
    /// and changes nothing on a second run. The one kind it creates is the first two chapters' four stops on Sela's chart (ChartStop): a later chapter defines its
    /// own from its builder, and no builder of chapters 1 and 2 may be rerun to do it. The first chapters' own builders call <see cref="Stamp"/> after defining, so a rebuilt asset is stamped too.
    /// <see cref="Check"/> is the lint for a chapter in progress: what a definition names that isn't there.
    /// </summary>
    public static class ContentScaffold
    {
        const string Root="Assets/_Project/Resources/Definitions/";
        static readonly (string id,string name,string music,string pocket,bool bright,int chapter)[] Zones=
        {
            ("Hub_CinderHalo","Cinder Halo","Hub Town Groove",null,false,1),("Hub_Decks","The Decks","Hub Town Groove",null,false,1),
            ("Sorrel_Ridges","Sorrel Ridges","Adventure Awaits","Hub Town Groove",true,1),("Hushwell","Hushwell","Moon Caverns",null,false,1),
            ("Gullet_Tunnel","The Gullet","Starfight",null,false,1),("TallowApproach","Tallow Approach","Hub Town Groove",null,false,1),("TallowDrift","Tallow Drift","Hub Town Groove",null,false,1),
            // The yards belong to no chapter.
            ("Arena_Ground","Proving Yard","Adventure Awaits",null,false,0),("Arena_Flight","Proving Berth","Starfight",null,false,0),
            ("WickReach","The Wick Reach","Endless Flight",null,false,2),("Lanternwick","Lanternwick","Old Village",null,true,2),
            ("LanternGorge","Lantern Gorge","Adventure Awaits",null,true,2),("SunkenRelay","The Sunken Relay","Moon Caverns",null,false,2),
        };
        /// <summary>Round eleven's music (November 27, docs/music/NEW_TRACKS_3.md): the Wick Reach flies to Endless Flight and keeps Starfight, the flight-fight track it played as its
        /// own, for its fights; the wild creatures of chapter 1 fight to Nathan's "Creature Boss", as chapters 3 and 4's do (their builders name it). Empty before: they fell back to
        /// Alien Boss Battle (MusicDirector.BossTrack), which the Muster's people and the Compact keep.</summary>
        internal static readonly (string id,string track)[] FightCues={("WickReach","Starfight")},BossTracks={("Burrower","Creature Boss"),("Cantor","Creature Boss"),("BellowsBelow","Creature Boss")};
        static readonly (string id,string name,float drawn,EnemyMotion motion,bool heavy)[] Enemies=
        {
            ("MusterScav","Muster Scavenger",1,EnemyMotion.None,false),("ChoristerDart",null,1.5f,EnemyMotion.Hover,false),("ChoristerDrifter",null,1,EnemyMotion.Hover,false),
            ("MusterSkiff",null,1,EnemyMotion.Hover,true),("Shellmine",null,1.4f,EnemyMotion.None,true),("SentinelHusk",null,1,EnemyMotion.None,true),("Scrapmite",null,1,EnemyMotion.Skitter,false),
        };
        static readonly (string id,bool main,int chapter)[] Quests=
        {
            ("Sorrel",true,1),("Hushwell",true,1),("LanternRoad",true,2),
            ("SorrelBeacons",false,1),("MiraCrystals",false,1),("NeveParcel",false,1),("OdaBerth",false,1),("PellsLamp",false,2),("IshensRecord",false,2),("UnswornRations",false,2),
        };
        /// <summary>Chapters 1 and 2's settlements on Sela's chart. Each is charted by the story flag only someone standing there could have set, and which the story can't reach the chart
        /// without (Orrin at his desk, Dace at the outpost, Keeper Abri, Keeper Aumi): so a save from before the chart existed has all four, and entering these zones still writes nothing.
        /// Sorrel arrives at the outpost's own spawn, inside its shelter, where its cradle already saves. Its anvil is 130 m up the ridge at the drill, so the outpost lists no bench.</summary>
        static readonly (string id,string name,string region,string blurb,string zone,string spawn,string flag,Vector2 at,string[] routes,bool shop,bool bench,bool repair,int chapter,string[] reaches)[] Stops=
        {
            ("Decks","The Decks","Cinder Halo","Home berth. Mira's market, Hal's bench and Orrin's dock office.","Hub_Decks","Arrival","met.Orrin",new(.03f,.56f),new string[0],true,true,true,1,new[]{"Hub_CinderHalo"}),
            ("SorrelOutpost","Sorrel Outpost","Sorrel","The survey outpost under the ridges, where the drill road runs down to Hushwell.","Sorrel_Ridges","Outpost","met.Survivor",new(.05f,.12f),new[]{"Decks"},false,false,true,1,new[]{"Hushwell"}),
            ("TallowDrift","Tallow Drift","Beyond the Gullet","Keeper Abri's refuge at the far mouth of the Gullet.","TallowDrift","Arrival","met.Keeper",new(.27f,.88f),new[]{"Decks"},false,false,true,1,new[]{"TallowApproach","Gullet_Tunnel"}),
            ("Lanternwick","Lanternwick","The Lantern Road","The Wick-folk's canyon town: the arch market, the Wick House and Aumi's archive.","Lanternwick","Arrival","met.Aumi",new(.29f,.38f),new[]{"TallowDrift"},true,true,true,2,new[]{"WickReach","LanternGorge","SunkenRelay"}),
        };
        public static void Apply()=>BatchTools.Run(()=>{int changed=Stamp();AssetDatabase.SaveAssets();Debug.Log("SCAFFOLD_OK changed="+changed);});
        /// <summary>Writes the tables above onto the assets that exist. Returns how many it changed: none, the second time.</summary>
        public static int Stamp()
        {
            int changed=0;
            T Load<T>(string id)where T:ScriptableObject=>AssetDatabase.LoadAssetAtPath<T>(Root+typeof(T).Name+"/"+id+".asset");
            void Set<T>(UnityEngine.Object asset,ref T field,T value){if(EqualityComparer<T>.Default.Equals(field,value))return;field=value;EditorUtility.SetDirty(asset);changed++;}
            foreach(var row in Zones)
            {
                var zone=Load<ZoneDef>(row.id);if(zone==null)continue;
                Set(zone,ref zone.displayName,row.name);Set(zone,ref zone.musicId,row.music);Set(zone,ref zone.brightFloor,row.bright);Set(zone,ref zone.chapter,row.chapter);
                if(row.pocket!=null)Set(zone,ref zone.pocketMusicId,row.pocket);
            }
            foreach(var row in FightCues){var zone=Load<ZoneDef>(row.id);if(zone!=null)Set(zone,ref zone.fightMusicId,row.track);}
            foreach(var row in BossTracks){var enemy=Load<EnemyDef>(row.id);if(enemy!=null)Set(enemy,ref enemy.bossMusicId,row.track);}
            foreach(var row in Enemies)
            {
                var enemy=Load<EnemyDef>(row.id);if(enemy==null)continue;
                if(row.name!=null)Set(enemy,ref enemy.displayName,row.name);
                Set(enemy,ref enemy.drawnScale,row.drawn);Set(enemy,ref enemy.motion,row.motion);Set(enemy,ref enemy.heavyDeath,row.heavy);
            }
            foreach(var row in Quests)
            {
                var quest=Load<QuestDef>(row.id);if(quest==null)continue;
                Set(quest,ref quest.main,row.main);Set(quest,ref quest.chapter,row.chapter);
            }
            // The four stops: made here the first time, and from then on stamped like everything else. A list is compared by what it holds, or every run would count as a change.
            void SetList(UnityEngine.Object asset,ref string[] field,string[] value){if(field!=null&&field.SequenceEqual(value))return;field=value;EditorUtility.SetDirty(asset);changed++;}
            foreach(var row in Stops)
            {
                var stop=Load<ChartStop>(row.id);if(stop==null){stop=ArenaBuilder.Asset<ChartStop>(row.id,_=>{});changed++;}
                Set(stop,ref stop.id,row.id);Set(stop,ref stop.displayName,row.name);Set(stop,ref stop.region,row.region);Set(stop,ref stop.blurb,row.blurb);
                Set(stop,ref stop.zone,row.zone);Set(stop,ref stop.spawn,row.spawn);Set(stop,ref stop.unlockFlag,row.flag);Set(stop,ref stop.position,row.at);
                Set(stop,ref stop.shop,row.shop);Set(stop,ref stop.bench,row.bench);Set(stop,ref stop.repair,row.repair);Set(stop,ref stop.chapter,row.chapter);
                SetList(stop,ref stop.routes,row.routes);SetList(stop,ref stop.reaches,row.reaches);
            }
            return changed;
        }

        /// <summary>Everything a definition names that isn't there, in one list: run it after a chapter's definitions, zones and music are in. It reads; it changes nothing.
        /// With <c>-content-unbuilt</c>, a zone defined and not yet built is passed over, for a chapter mid-construction.</summary>
        public static void Check()=>BatchTools.Run(()=>
        {
            var faults=Faults(DevArgs.Has("-content-unbuilt"));
            if(faults.Count>0)throw new Exception("CONTENT_INCOMPLETE "+faults.Count+"\n"+string.Join("\n",faults));
            Debug.Log("CONTENT_OK");
        });
        public static List<string> Faults(bool allowUnbuilt=false)
        {
            var faults=new List<string>();
            T[] All<T>()where T:ScriptableObject=>Directory.Exists(Root+typeof(T).Name)?Directory.GetFiles(Root+typeof(T).Name,"*.asset").Select(p=>AssetDatabase.LoadAssetAtPath<T>(p.Replace('\\','/'))).Where(a=>a!=null).OrderBy(a=>a.name,StringComparer.Ordinal).ToArray():Array.Empty<T>();
            var zones=All<ZoneDef>();var enemies=All<EnemyDef>();var quests=All<QuestDef>();var people=All<CharacterDef>();
            var registered=EditorBuildSettings.scenes.Select(s=>Path.GetFileNameWithoutExtension(s.path)).ToArray();
            bool Staged(string track)=>File.Exists("Assets/_Project/Resources/Audio/Music/"+track+".ogg");
            foreach(var zone in zones)
            {
                string id=zone.name;
                if(zone.id!=id||zone.scene!=id){faults.Add($"ZoneDef {id}: its id ('{zone.id}'), its scene ('{zone.scene}') and its file name must be the same string");continue;}
                if(zone.cameraProfile==null)faults.Add($"ZoneDef {id} has no cameraProfile");
                if(zone.hd2dProfile==null)faults.Add($"ZoneDef {id} has no hd2dProfile: WorldBuilder.Begin reads its bloom");
                if(!ZoneLook.Has(id))faults.Add($"{id} has no ZoneLook spec: register one from its builder ([ZoneLook.Source])");
                bool first=Zones.Any(z=>z.id==id);
                if(!first&&string.IsNullOrWhiteSpace(zone.musicId))faults.Add($"ZoneDef {id} names no musicId: it plays in silence");
                if(!first&&zone.chapter<=0)faults.Add($"ZoneDef {id} has no chapter: new scenes join the build list in chapter order");
                foreach(string track in new[]{zone.musicId,zone.fightMusicId,zone.pocketMusicId,zone.shopMusicId})
                    if(!string.IsNullOrWhiteSpace(track)&&!Staged(track))faults.Add($"ZoneDef {id} names the track '{track}', which isn't staged in Resources/Audio/Music: run MusicIntake.Stage");
                if(!File.Exists("Assets/_Project/Scenes/"+id+".unity")){if(!allowUnbuilt)faults.Add($"{id} has a ZoneDef and no scene: build it");}
                else if(!registered.Contains(id))faults.Add($"{id} is built and not in the build list: run a zone build or a player build (BatchTools.RegisterScenes)");
            }
            foreach(var enemy in enemies)
            {
                string id=enemy.name;
                if(enemy.id!=id)faults.Add($"EnemyDef {id}: its id ('{enemy.id}') and its file name must be the same string");
                if(enemy.boss&&!BossController.Registered(id))faults.Add($"EnemyDef {id} is a boss and no BossController.<Name>.cs registers a Kit for it: it would have no specials");
                if(enemy.boss&&!string.IsNullOrWhiteSpace(enemy.bossMusicId)&&!Staged(enemy.bossMusicId))faults.Add($"EnemyDef {id} names the track '{enemy.bossMusicId}', which isn't staged: run MusicIntake.Stage");
                if(enemy.attacks==null||enemy.attacks.Length==0||enemy.attacks[0]==null)faults.Add($"EnemyDef {id} has no first attack: its brain strikes with a placeholder");
                // A swarm cloud with no model is drawn as its motes (SwarmMotes): that is a body.
                if(enemy.prefab==null&&enemy.behaviour!=EnemyBehaviourKind.SwarmCloud)faults.Add($"EnemyDef {id} has no body: it would be drawn as an ART_PENDING shape");
            }
            foreach(string id in BossController.RegisteredIds)if(!enemies.Any(e=>e.name==id&&e.boss))faults.Add($"BossController registers '{id}' and no boss EnemyDef has that id");
            foreach(var quest in quests)
            {
                string id=quest.name;int steps=quest.steps?.Length??0;
                if(quest.id!=id)faults.Add($"QuestDef {id}: its id ('{quest.id}') and its file name must be the same string");
                if(steps==0){faults.Add($"QuestDef {id} has no steps");continue;}
                if(quest.stepText==null||quest.stepText.Length!=steps||quest.stepText.Any(string.IsNullOrWhiteSpace))faults.Add($"QuestDef {id}: stepText needs one line for each of its {steps} steps, none blank");
                if(quest.stepTargets!=null&&quest.stepTargets.Length!=0&&quest.stepTargets.Length!=steps)faults.Add($"QuestDef {id}: stepTargets has {quest.stepTargets.Length} entries for {steps} steps (one per step, or none)");
                if(quest.main&&quest.chapter<=0&&Array.IndexOf(StoryQuests.First,id)<0)faults.Add($"QuestDef {id} is a main quest with no chapter: the log orders the story by it and DevLoadout's chapter loadouts tell it by it");
                for(int i=0;i<steps;i++)
                {
                    var step=quest.steps[i];
                    if(step.kind==ObjectiveKind.Reach&&!zones.Any(z=>z.name==step.target))faults.Add($"QuestDef {id} step {i} reaches '{step.target}', which is no ZoneDef");
                    if(step.kind==ObjectiveKind.TalkTo&&!people.Any(p=>p.name==step.target))faults.Add($"QuestDef {id} step {i} talks to '{step.target}', which is no CharacterDef");
                    if(step.kind==ObjectiveKind.Kill&&!enemies.Any(e=>e.name==step.target))faults.Add($"QuestDef {id} step {i} kills '{step.target}', which is no EnemyDef");
                    if(quest.stepTargets==null||i>=quest.stepTargets.Length)continue;
                    var aim=quest.stepTargets[i];
                    foreach(string scene in new[]{aim.scene}.Concat(aim.via??Array.Empty<string>()))
                        if(!string.IsNullOrEmpty(scene)&&!zones.Any(z=>z.name==scene))faults.Add($"QuestDef {id} step {i} aims at the scene '{scene}', which is no ZoneDef");
                    if(!string.IsNullOrEmpty(aim.speaker)&&!people.Any(p=>p.name==aim.speaker))faults.Add($"QuestDef {id} step {i} aims at the speaker '{aim.speaker}', which is no CharacterDef");
                }
            }
            // Sela's chart: a stop that arrives nowhere would hang the load it starts, and one the d-pad can't reach or whose name lies under another's can't be chosen.
            var stops=All<ChartStop>();
            foreach(var stop in stops)
            {
                string id=stop.name;
                if(stop.id!=id)faults.Add($"ChartStop {id}: its id ('{stop.id}') and its file name must be the same string");
                if(!zones.Any(z=>z.name==stop.zone))faults.Add($"ChartStop {id} arrives in '{stop.zone}', which is no ZoneDef");
                else if(string.IsNullOrWhiteSpace(stop.spawn))faults.Add($"ChartStop {id} names no spawn to arrive at");
                else
                {
                    // A SpawnPoint is saved with its id on a line of its own. The scene isn't opened for this: RouteChartTests loads each stop's zone and looks.
                    string scene="Assets/_Project/Scenes/"+stop.zone+".unity";
                    if(File.Exists(scene)&&!System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(scene),@"^\s*id: "+System.Text.RegularExpressions.Regex.Escape(stop.spawn)+@"\s*$",System.Text.RegularExpressions.RegexOptions.Multiline))
                        faults.Add($"ChartStop {id} arrives at the spawn '{stop.spawn}', and {stop.zone} has no SpawnPoint by that id");
                }
                if(string.IsNullOrWhiteSpace(stop.displayName)||string.IsNullOrWhiteSpace(stop.region)||string.IsNullOrWhiteSpace(stop.blurb))faults.Add($"ChartStop {id} needs a displayName, a region and a blurb: the chart shows all three");
                if(stop.chapter<=0)faults.Add($"ChartStop {id} has no chapter: the chart lists stops by it, and DevLoadout charts the earlier chapters' stops by it");
                if(!string.IsNullOrEmpty(stop.lockedFlag)&&string.IsNullOrWhiteSpace(stop.lockedReason))faults.Add($"ChartStop {id} is closed by '{stop.lockedFlag}' and gives no lockedReason: a refused press has to say why");
                foreach(string other in stop.routes??Array.Empty<string>())if(other==id||!stops.Any(s=>s.name==other))faults.Add($"ChartStop {id} draws a route to '{other}', which is no other ChartStop");
                foreach(string reached in stop.reaches??Array.Empty<string>())if(!zones.Any(z=>z.name==reached))faults.Add($"ChartStop {id} reaches '{reached}', which is no ZoneDef");
                foreach(var twin in stops)if(twin!=stop&&twin.zone==stop.zone&&string.CompareOrdinal(twin.name,id)>0)faults.Add($"ChartStops {id} and {twin.name} both arrive in {stop.zone}: a zone is one stop");
            }
            faults.AddRange(Lattice.UI.RouteChart.LayoutFaults(stops));
            try{MusicIntake.Validate();}catch(InvalidDataException e){faults.AddRange(e.Message.Split('\n').Skip(1));}
            return faults;
        }
    }
}
