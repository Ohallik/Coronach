using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
using Yarn.Unity;

namespace Lattice.Tests.PlayMode
{
    /// <summary>
    /// What the game holds, read from the definitions and the compiled dialogue, so a test walks every zone, enemy, boss and quest there is and not a list
    /// someone remembered to extend. Until this, no test discovered anything: about twenty lists named chapters 1 and 2 by hand, and a zone left off one was
    /// simply never checked.
    /// <para>The first two chapters stay written out, in the order the tests always walked them (the <c>First...</c> arrays): what a test protected there it still
    /// protects, on exactly those items. Everything defined since is appended from the assets, so a new zone is checked the day its ZoneDef exists.</para>
    /// <para>Where a check needs something written by hand for each item (where to stand for a boss's move, the words a lock must say), a chapter says it in its own
    /// test class, in a method marked <see cref="ExpectationsAttribute"/>, and a missing entry fails by name ("Floodcrown has no boss sheet") instead of being skipped
    /// or tested against someone else's.</para>
    /// </summary>
    public static class ContentRegistry
    {
        // ------------------------------------------------------------ chapters 1 and 2, as the tests have always listed them
        public static readonly string[] FirstZones={"Hub_Decks","Hub_CinderHalo","Sorrel_Ridges","Hushwell","Gullet_Tunnel","TallowApproach","TallowDrift","WickReach","Lanternwick","LanternGorge","SunkenRelay"};
        public static readonly string[] Arenas={"Arena_Ground","Arena_Flight"};
        public static readonly string[] FirstEnemies={"Ridgehound","Scrapmite","SentinelHusk","Burrower","ChoristerDart","ChoristerDrifter","Shellmine","Cantor","BellowsBelow","Gloomgrazer","MusterScav","MusterSergeant","MusterSkiff","HessaKorl"};
        public static readonly string[] FirstBosses={"Burrower","BellowsBelow","Cantor","HessaKorl"};
        public static readonly string[] FirstQuests={"Sorrel","Hushwell","LanternRoad","MiraCrystals","NeveParcel","SorrelBeacons","OdaBerth","PellsLamp","IshensRecord","UnswornRations"};
        /// <summary>The script files of chapters 1 and 2 (Resources/Dialogue, without the extension).</summary>
        public static readonly string[] FirstScripts={"Arena","Decks","Gullet","Halo","Lanternwick","RuntimeReady","SelaNursery","Sorrel","Tallow"};
        public static readonly ZoneKind[] OnFoot={ZoneKind.GroundSafe,ZoneKind.GroundCombat},InFlight={ZoneKind.SpaceSafe,ZoneKind.SpaceCombat},Towns={ZoneKind.GroundSafe},Wilds={ZoneKind.GroundCombat};

        // ------------------------------------------------------------ the definitions
        static T[] Load<T>()where T:ScriptableObject
        {
            var all=Resources.LoadAll<T>("Definitions/"+typeof(T).Name);Array.Sort(all,(a,b)=>string.CompareOrdinal(a.name,b.name));return all;
        }
        static string[] After(string[] first,IEnumerable<string> every)=>first.Concat(every.Where(id=>Array.IndexOf(first,id)<0)).ToArray();
        public static ZoneDef[] ZoneDefs=>Load<ZoneDef>();
        public static ZoneDef Zone(string id)=>ZoneDefs.FirstOrDefault(z=>z.name==id)??throw new InvalidOperationException(id+" has no ZoneDef");
        public static bool IsFirst(string zone)=>Array.IndexOf(FirstZones,zone)>=0||Array.IndexOf(Arenas,zone)>=0;
        /// <summary>The zones built since chapter 2, by chapter and then id: of these kinds, or of every kind.</summary>
        public static string[] LaterZones(params ZoneKind[] kinds)=>ZoneDefs.Where(z=>!IsFirst(z.name)&&(kinds.Length==0||Array.IndexOf(kinds,z.kind)>=0))
            .OrderBy(z=>z.chapter<=0?int.MaxValue:z.chapter).ThenBy(z=>z.name,StringComparer.Ordinal).Select(z=>z.name).ToArray();
        /// <summary>Every zone a player visits: the first eleven, then each one built since. The two yards are not among them.</summary>
        public static string[] Zones=>FirstZones.Concat(LaterZones()).ToArray();
        /// <summary>The zones a check has always run on, then every later zone of these kinds: how a <c>[ValueSource]</c> keeps its old cases and gains the new.
        /// A later zone the check truly doesn't apply to is left out by name, with the reason, in <see cref="Book.Exempt"/>.</summary>
        public static string[] With(string check,string[] first,params ZoneKind[] kinds)=>first.Concat(LaterZones(kinds).Where(zone=>!Expected.exempt.ContainsKey(check+"/"+zone))).ToArray();
        public static EnemyDef[] EnemyDefs=>Load<EnemyDef>();
        /// <summary>Every enemy, bosses among them: the first fourteen, then each defined since.</summary>
        public static string[] Enemies=>After(FirstEnemies,EnemyDefs.Select(e=>e.name));
        public static string[] OrdinaryEnemies=>After(FirstEnemies.Except(FirstBosses).ToArray(),EnemyDefs.Where(e=>!e.boss).Select(e=>e.name));
        /// <summary>Every boss: the first four, then each EnemyDef since that says boss. One that no BossController file registers is still listed, so its tests say so.</summary>
        public static string[] Bosses=>After(FirstBosses,EnemyDefs.Where(e=>e.boss).Select(e=>e.name));
        public static string[] LaterBosses=>Bosses.Skip(FirstBosses.Length).ToArray();
        public static QuestDef[] QuestDefs=>Load<QuestDef>();
        public static string[] Quests=>After(FirstQuests,QuestDefs.Select(q=>q.name));
        public static string[] LaterQuests=>Quests.Skip(FirstQuests.Length).ToArray();
        /// <summary>The story's quests, in story order, and everything else: read from QuestDef.main (StoryQuests), as the pause menu and the objective panel read them.</summary>
        public static string[] MainQuests=>StoryQuests.Ordered.Where(id=>QuestDefs.Any(q=>q.name==id)).ToArray();
        public static string[] SideQuests=>Quests.Where(id=>!StoryQuests.IsMain(id)).ToArray();
        public static bool IsMain(string quest)=>StoryQuests.IsMain(quest);
        /// <summary>Every CharacterDef's id: the names a Yarn line may be spoken by.</summary>
        public static string[] Speakers=>Load<CharacterDef>().Select(c=>c.name).ToArray();

        // ------------------------------------------------------------ the dialogue
        /// <summary>Every compiled node a player can reach (the runtime's own start with two underscores).</summary>
        public static string[] YarnNodes=>Resources.Load<YarnProject>("Dialogue/Lattice").NodeNames.Where(n=>!n.StartsWith("__")).OrderBy(n=>n,StringComparer.Ordinal).ToArray();
        public static string ScriptRoot=>Path.Combine(Application.dataPath,"_Project/Resources/Dialogue");
        /// <summary>The staged script files, and whether one is chapters 1 and 2's.</summary>
        public static string[] ScriptFiles=>Directory.GetFiles(ScriptRoot,"*.yarn").OrderBy(f=>f,StringComparer.Ordinal).ToArray();
        public static bool IsFirstScript(string file)=>Array.IndexOf(FirstScripts,Path.GetFileNameWithoutExtension(file))>=0;
        /// <summary>The titles of the nodes written in one script file.</summary>
        public static string[] NodesIn(string file)=>File.ReadAllText(file).Replace("\r\n","\n").Split('\n').Where(l=>l.StartsWith("title:")).Select(l=>l.Substring(6).Trim()).ToArray();
        /// <summary>Every node written since chapter 2, by the file it is in.</summary>
        public static string[] LaterNodes=>ScriptFiles.Where(f=>!IsFirstScript(f)).SelectMany(NodesIn).ToArray();

        // ------------------------------------------------------------ what has to be written by hand, per item
        /// <summary>On a static method taking a <see cref="Book"/>, in a chapter's own test class: it writes that chapter's expectations, so no chapter edits a shared table.
        /// <code>[ContentRegistry.Expectations] static void Xylos(ContentRegistry.Book book){book.Tell("Thornback",3);book.Boss(new ContentRegistry.BossSheet{id="Floodcrown",...});}</code></summary>
        [AttributeUsage(AttributeTargets.Method)] public sealed class ExpectationsAttribute:Attribute{}
        /// <summary>How far past its bar's ceiling a boss's modelled slow pair (a quarter of the measure's flat out, the Saint's dodging fight, the Cutter's dodging pair) may run,
        /// measured on the present flat-out measure: four thirds, so 300 s becomes 400. D362 (continued), November 22: the measure is not flat out. It spends no EXTRA (10 to 33%
        /// of several late bars in that day's scouts) and reads every skill at its first mastery (D353: up to 40% more power by the time a player meets these bosses), so what a
        /// player's pair can really do is about a third above it, and a quarter of it inside 300 s held fights to a player slower than the plan meant. 400 s of this measure is the
        /// plan's 300 for a pair with its EXTRA and its mastery. Owed: a flat-out measure that spends the EXTRA and reads skills at the mastery a player has by that chapter, after
        /// which this goes back to 1. The floors, the bars' lower ends and every share check are untouched.</summary>
        public const float SlowPairAllowance=4f/3;
        /// <summary>The ceiling a modelled slow pair is held to for a bar whose ceiling is <paramref name="planSeconds"/> (<see cref="SlowPairAllowance"/>).</summary>
        public static float SlowPairCeiling(float planSeconds)=>planSeconds*SlowPairAllowance;
        /// <summary>One boss's fight as its tests need it told. The first four leave the tables they always had in QaPlanCombatTests and give only their door here.</summary>
        public sealed class BossSheet
        {
            public string id;
            /// <summary>Its door: the zone, and the EncounterVolume's id there. And the flags its door waits on, set before the zone loads (a confrontation heard).</summary>
            public string zone,door;public string[] doorFlags=Array.Empty<string>();
            /// <summary>Fought flying: its tests run in the flight yard.</summary>
            public bool flight;
            /// <summary>The Sync a party meets it at, and what the replay driver gets of flat out (measured off a chain's BOSS_DURATION; .3 until there is one).</summary>
            public int sync;public float driverShare;
            /// <summary>The bar its length is held to, in seconds: two to five minutes for a boss (D206). A mini-boss says its own, shorter one.</summary>
            public float minSeconds=120,maxSeconds=300;
            /// <summary>Each marked special, and where the lead stands from the boss to be inside its marking. Every move the boss registers is here or in <see cref="testedApart"/>.</summary>
            public (string move,Vector3 stand)[] specials;
            /// <summary>Registered moves this table leaves to a test of their own (bullets with gaps, a move that needs the real arena): say which test in a comment.</summary>
            public string[] testedApart=Array.Empty<string>();
            /// <summary>The move that leaves an opening in phases 1, 2 and 3, and where the lead stands for it.</summary>
            public (string move,Vector3 stand)[] windows;
        }
        public sealed class Book
        {
            internal readonly Dictionary<string,BossSheet> bosses=new();
            internal readonly Dictionary<string,(float distance,bool flight)> tells=new();
            internal readonly Dictionary<string,string[]> locks=new();
            internal readonly Dictionary<string,string> exempt=new();
            internal readonly Dictionary<string,string> longTalks=new();
            internal readonly Dictionary<string,string[]> branches=new();
            public void Boss(BossSheet sheet)=>Once(bosses,sheet.id,sheet,"boss sheet");
            /// <summary>Where a hero stands for this ordinary enemy to tell its attack at him, in metres, and whether it is met flying.</summary>
            public void Tell(string enemy,float distance,bool flight=false)=>Once(tells,enemy,(distance,flight),"tell");
            /// <summary>A locked way out waits on this flag: a refused press must say one of these words, so the player knows what opens it.</summary>
            public void Lock(string flag,params string[] words)=>Once(locks,flag,words,"lock");
            /// <summary>A later zone a zone check doesn't apply to, by the check's name, and why. Without a reason it isn't taken.</summary>
            public void Exempt(string check,string zone,string why)
            {
                if(string.IsNullOrWhiteSpace(why))throw new ArgumentException($"{zone} is left out of {check} with no reason given");
                Once(exempt,check+"/"+zone,why,"exemption");
            }
            /// <summary>A conversation written since chapter 2 that nobody has to sit through, so it may run past ninety seconds: its node, and why it's optional.</summary>
            public void Optional(string node,string why)
            {
                if(string.IsNullOrWhiteSpace(why))throw new ArgumentException($"{node} is called optional with no reason given");
                Once(longTalks,node,why,"optional conversation");
            }
            /// <summary>A node with choices: the option to take at each, in order, for the rapid-input check.</summary>
            public void Branch(string node,params string[] choices)=>Once(branches,node,choices,"branch");
            static void Once<T>(Dictionary<string,T> table,string key,T value,string what)
            {
                if(table.ContainsKey(key))throw new InvalidOperationException($"{key} has two {what} entries: one chapter's expectations already say this");
                table[key]=value;
            }
        }
        static Book book;
        /// <summary>The first two chapters' doors, then every chapter's own expectations, gathered once from the methods marked <see cref="ExpectationsAttribute"/>.</summary>
        public static Book Expected
        {
            get
            {
                if(book!=null)return book;
                var gathered=new Book();
                gathered.Boss(new BossSheet{id="Burrower",zone="Sorrel_Ridges",door="Sorrel_Burrower",sync=4,driverShare=.42f});
                gathered.Boss(new BossSheet{id="BellowsBelow",zone="Hushwell",door="Hushwell_Bellows",sync=5,driverShare=.40f});
                gathered.Boss(new BossSheet{id="Cantor",zone="Gullet_Tunnel",door="Gullet_Cantor",flight=true,sync=7,driverShare=.29f});
                gathered.Boss(new BossSheet{id="HessaKorl",zone="SunkenRelay",door="Relay_Hessa",doorFlags=new[]{"relay.hessaConfronted"},sync=9,driverShare=.30f});
                const BindingFlags any=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly;
                foreach(var source in typeof(ContentRegistry).Assembly.GetTypes().SelectMany(t=>t.GetMethods(any)).Where(m=>m.GetCustomAttribute<ExpectationsAttribute>()!=null)
                    .OrderBy(m=>m.DeclaringType.FullName+"."+m.Name,StringComparer.Ordinal))
                {
                    var takes=source.GetParameters();
                    if(takes.Length!=1||takes[0].ParameterType!=typeof(Book))throw new InvalidOperationException($"{source.DeclaringType.Name}.{source.Name} is marked [ContentRegistry.Expectations] and must be static and take one ContentRegistry.Book");
                    try{source.Invoke(null,new object[]{gathered});}
                    catch(TargetInvocationException e){throw new InvalidOperationException($"{source.DeclaringType.Name}.{source.Name} failed to write its expectations: {e.InnerException?.Message}",e.InnerException);}
                }
                return book=gathered;
            }
        }
        const string Where="add it in a method marked [ContentRegistry.Expectations] in its chapter's test class";
        /// <summary>A boss's sheet, or a failure that names it: never somebody else's.</summary>
        public static BossSheet Boss(string id)=>Expected.bosses.TryGetValue(id,out var sheet)?sheet:
            throw new InvalidOperationException($"{id} has no boss sheet in ContentRegistry: book.Boss(new ContentRegistry.BossSheet{{id=\"{id}\",...}}); {Where}");
        public static (float distance,bool flight) Tell(string enemy)=>Expected.tells.TryGetValue(enemy,out var tell)?tell:
            throw new InvalidOperationException($"{enemy} has no entry in the tell table: book.Tell(\"{enemy}\",metres,flight); {Where}");
        /// <summary>The words a lock on this flag must say when refused. A person met or a boss down names itself; anything else is written by its chapter, or null.</summary>
        public static string[] LockWords(string flag)
        {
            if(Expected.locks.TryGetValue(flag,out var words))return words;
            if(flag.StartsWith("met."))return DisplayNames.Speaker(flag.Substring(4)).Split(' ').Where(w=>w.Length>2).ToArray();
            if(flag.StartsWith("bossdown.")&&Array.IndexOf(Bosses,flag.Substring(9))>=0)return DisplayNames.Target(flag.Substring(9)).Split(' ').Where(w=>w.Length>3).ToArray();
            return null;
        }
        public static string OptionalTalk(string node)=>Expected.longTalks.TryGetValue(node,out var why)?why:null;
        public static string[] Branch(string node)=>Expected.branches.TryGetValue(node,out var choices)?choices:null;
        /// <summary>A place's name as a sign or a prompt would say it: "The Wick Reach" is "Wick Reach" on a warp.</summary>
        public static string NameOnASign(string zone){string name=DisplayNames.Zone(zone);return name.StartsWith("The ")?name.Substring(4):name;}
    }
}
