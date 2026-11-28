using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class MusicCueTests
    {
        GameObject fixture;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(fixture!=null)Object.Destroy(fixture);
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Load(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            yield return new WaitForSecondsRealtime(.3f);
        }
        EnemyBrain Boss(string id,Vector3 offset)
        {
            if(fixture==null)fixture=new GameObject("Boss music fixture");
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),PartyController.Current.Active.transform.position+offset);
            boss.transform.SetParent(fixture.transform);boss.Passive=true;return boss;
        }
        static void Defeat(EnemyBrain boss)
        {
            var packet=new DamagePacket{source=PartyController.Current.Active.Health,amount=boss.Health.maximum*50,type=DamageType.Pulse};
            if(boss.TryGetComponent<CantorCollar>(out var collar))foreach(var link in collar.Links)link.GetComponent<Hurtbox>().Hit(packet);
            boss.Health.Receive(packet);
        }

        [UnityTest] public IEnumerator FlightCombatAndBossEncountersHaveTheirOwnCues()
        {
            yield return Load("Gullet_Tunnel");
            Assert.AreEqual("Starfight",MusicDirector.Current.Track,"the Gullet's combat route is silent");
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.IsTrue(MusicDirector.Current.ActiveSource.isPlaying);Assert.Greater(MusicDirector.Current.ActiveSource.volume,.4f);

            // The wild creatures fight to Nathan's "Creature Boss" since round eleven (Alien Boss Battle before, which the Muster's people and the Compact keep).
            var cantor=Boss("Cantor",Vector3.forward*25);yield return null;yield return null;
            Assert.AreEqual(Creature,MusicDirector.Current.Track,"a boss encounter keeps the zone cue");
            Defeat(cantor);yield return null;
            Assert.IsFalse(cantor.Health.Alive);
            Assert.AreEqual("Starfight",MusicDirector.Current.Track,"boss defeat did not return to the zone cue");

            // A retry or zone reset removes a living boss without a death.
            var again=Boss("Cantor",Vector3.forward*25);yield return null;yield return null;
            Assert.AreEqual(Creature,MusicDirector.Current.Track);
            Object.Destroy(again.gameObject);yield return null;yield return null;
            Assert.AreEqual("Starfight",MusicDirector.Current.Track,"a removed boss left its cue playing");

            // Ground bosses use the same cue over the moon's combat music; the
            // four original assignments are unchanged.
            yield return Load("Sorrel_Ridges");ZoneController.Current.SafePocket(false);
            Assert.AreEqual("Adventure Awaits",MusicDirector.Current.Track);
            var burrower=Boss("Burrower",Vector3.forward*12);yield return null;yield return null;
            Assert.AreEqual(Creature,MusicDirector.Current.Track);
            Defeat(burrower);yield return null;
            Assert.AreEqual("Adventure Awaits",MusicDirector.Current.Track);
            yield return Load("Hub_Decks");Assert.AreEqual("Hub Town Groove",MusicDirector.Current.Track);
        }

        // ------------------------------------------------------------------ round eleven: Nathan's fourteen tracks (November 27, docs/music/NEW_TRACKS_3.md)
        const string Creature="Creature Boss";
        /// <summary>Where each zone round eleven changed or kept on purpose plays: its own music and its fight cue (null: none, so its music plays on through a fight).</summary>
        static readonly (string zone,string music,string fight)[] Places=
        {
            ("ManyHands","Many Hands",null),("ManyHandsHull","Starward Journey","Starfight"),("AuroraShear","Aurora Shear","Starfight"),("Orrery","Orrery Nine Moons","Starfight"),
            ("PicketOfTeeth","Picket of Teeth","Starfight"),("Harrowgate","Galactic Conquest",null),("HarrowgateHarbour","Galactic Conquest",null),("NacreGrave","Nacre Grave","Starfight"),
            ("SeparationWorks","Separation Works",null),("Hushwell","Moon Caverns",null),("SunkenRelay","Moon Caverns",null),("BrokenRing","Broken Ring",null),
            ("Terminator","Dventure at Dusk","Sword Skirmish"),("Continuance","Last Mooring","Sword Skirmish"),("LastMooring","Last Mooring",null),
            ("StillwaterChamber","Regal Alien Town",null),("ReturningPassage","Room To Turn Back","Starfight"),("NeighbourhoodAfloat","Neighborhood Afloat",null),
            ("WickReach","Endless Flight","Starfight"),("XylosNearspace","Starfield Frontier","Starfight"),("SporeVeil","Starfield Frontier","Starfight"),
        };
        /// <summary>Each boss's track as its fight plays it (a definition that names none plays MusicDirector.BossTrack).</summary>
        static readonly (string boss,string track)[] Bosses=
        {
            ("Burrower",Creature),("BellowsBelow",Creature),("Cantor",Creature),("Seedthane",Creature),("Floodcrown",Creature),("Rotbloom",Creature),
            ("HessaKorl","Alien Boss Battle"),("VessaKorl","Alien Boss Battle"),("DraskVaal","Alien Boss Battle"),("DraskSkiff","Alien Boss Battle"),("DebrisCutter","Alien Boss Battle"),
            ("FoundrySaint","Glitch Boss"),
        };
        /// <summary>What round ten had where round eleven changed something: the state the check is first shown to catch.</summary>
        static readonly Dictionary<string,(string music,string fight)> RoundTenPlaces=new()
        {
            ["ManyHands"]=("Starward Journey",null),["AuroraShear"]=("Starfield Frontier","Starfight"),["Orrery"]=("Starfield Frontier","Starfight"),["PicketOfTeeth"]=("Galactic Conquest","Starfight"),
            ["NacreGrave"]=("Endless Flight","Starfight"),["SeparationWorks"]=("Moon Caverns",null),["BrokenRing"]=("Starfight",null),["Terminator"]=("Dventure at Dusk","Starfight"),
            ["Continuance"]=("Regal Alien Town","Starfight"),["LastMooring"]=("Starfight",null),["ReturningPassage"]=("Endless Flight","Starfight"),["NeighbourhoodAfloat"]=("Hub Town Groove",null),
            ["WickReach"]=("Starfight",null),
        };
        static string Effective(string named)=>string.IsNullOrWhiteSpace(named)?MusicDirector.BossTrack:named;
        static string Or(string track)=>string.IsNullOrWhiteSpace(track)?null:track;
        static List<string> Misplaced(System.Func<string,(string music,string fight)> place,System.Func<string,string> boss)
        {
            var faults=new List<string>();
            foreach(var (zone,music,fight) in Places)
            {
                var (has,fights)=place(zone);
                if(has!=music)faults.Add($"{zone} plays {has ?? "nothing"}, not {music}");
                if(Or(fights)!=fight)faults.Add($"{zone}'s fights play {fights ?? "its own music"}, not {fight ?? "its own music"}");
            }
            foreach(var (id,track) in Bosses){string has=boss(id);if(has!=track)faults.Add($"{id}'s fight plays {has}, not {track}");}
            return faults;
        }

        /// <summary>Every zone and boss of round eleven's table asks for the track written for it, the places that kept theirs still do, and no zone gained a fight cue the table
        /// doesn't give it. The Wick Reach's own music is the code's (MusicDirector's first thirteen zones), so it is read from what plays there, with a fight awake and after.
        /// Shown first to catch round ten's assignments (thirteen zones and six bosses).</summary>
        [UnityTest] public IEnumerator EveryPlaceAndBossAsksForTheTrackWrittenForIt()
        {
            yield return Load("WickReach");
            string reach=MusicDirector.Current.Track;
            var owner=new GameObject("A fight in the Wick Reach");MusicDirector.Fight(owner,true);yield return null;string reachFight=MusicDirector.Current.Track;
            MusicDirector.Fight(owner,false);Object.Destroy(owner);yield return null;
            Assert.AreEqual(reach,MusicDirector.Current.Track,"the Wick Reach's music didn't come back after its fight");
            (string,string) Live(string zone)
            {
                var definition=GameCatalog.Find<ZoneDef>(zone);Assert.IsNotNull(definition,"fixture: "+zone+" has no ZoneDef");
                return zone=="WickReach"?(reach,reachFight==reach?null:reachFight):(Or(definition.musicId),Or(definition.fightMusicId));
            }
            string LiveBoss(string id){var definition=GameCatalog.Find<EnemyDef>(id);Assert.IsNotNull(definition,"fixture: "+id+" has no EnemyDef");Assert.IsTrue(definition.boss,"fixture: "+id+" isn't a boss");return Effective(definition.bossMusicId);}
            var before=Misplaced(z=>RoundTenPlaces.TryGetValue(z,out var was)?was:Live(z),id=>Bosses.Any(b=>b.boss==id&&b.track==Creature)?MusicDirector.BossTrack:LiveBoss(id));
            // Round ten: thirteen zones' music or fight cue (two each in the Continuance and the Wick Reach) and the six creatures' fights.
            Assert.AreEqual(15+6,before.Count,"fixture: the check doesn't catch round ten's assignments:\n"+string.Join("\n",before));
            var faults=Misplaced(Live,LiveBoss);
            Assert.IsEmpty(faults,string.Join("\n",faults));
            // No fight cue anywhere else on foot: a ground zone's own music plays on through its fights, as the design has it.
            var extra=GameCatalog.All<ZoneDef>().Where(z=>z!=null&&(z.kind==ZoneKind.GroundCombat||z.kind==ZoneKind.GroundSafe)&&Or(z.fightMusicId)!=null&&!Places.Any(p=>p.zone==z.name)).Select(z=>$"{z.name}: {z.fightMusicId}").ToList();
            var known=new[]{"BraidedRiver: Riverbed Stomp"};
            Assert.IsEmpty(extra.Except(known),"a ground zone names a fight cue round eleven didn't give it");
        }

        /// <summary>Every track the game asks for (MusicIntake.Named: the code's, every zone's and every boss's) is staged, no staged track is asked for by nobody, and the credits and
        /// docs/music/tracks.json list exactly the staged ones. Shown first to catch Endless Flight left playing nowhere, as it would have been with Nacre Grave and the Returning
        /// Passage moved off it and the Wick Reach not given it.</summary>
        [UnityTest] public IEnumerator EveryTrackAskedForIsStagedAndEveryStagedTrackIsAskedForAndCredited()
        {
            yield return null;
            // The intake is the editor's (this assembly doesn't reference it): found by name, as RestageTests finds Restage.
            var intake=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Lattice.EditorTools.MusicIntake",false)).FirstOrDefault(t=>t!=null);
            Assert.IsNotNull(intake,"fixture: MusicIntake isn't loaded");
            var named=((List<(string title,string by)>)intake.GetMethod("Named").Invoke(null,null)).Select(n=>n.title).Distinct().ToList();
            var staged=Directory.GetFiles("Assets/_Project/Resources/Audio/Music","*.ogg").Select(Path.GetFileNameWithoutExtension).ToList();
            static List<string> Orphans(IEnumerable<string> onDisk,IEnumerable<string> asked)=>onDisk.Except(asked).OrderBy(t=>t,System.StringComparer.Ordinal).ToList();
            CollectionAssert.Contains(Orphans(staged,named.Where(t=>t!="Endless Flight")),"Endless Flight","fixture: the check lets a track nobody asks for through");
            Assert.IsEmpty(named.Except(staged).ToList(),"asked for and not staged (run MusicIntake.Stage)");
            Assert.IsEmpty(Orphans(staged,named),"staged and asked for by nobody");
            Assert.AreEqual(41,staged.Count,"the 27 tracks and round eleven's 14");
            CollectionAssert.AreEquivalent(staged,CreditsRoll.Tracks,"the credits' tracks aren't the staged ones");
            CollectionAssert.AreEqual(CreditsRoll.Tracks.OrderBy(t=>t.ToLowerInvariant(),System.StringComparer.Ordinal).ToArray(),CreditsRoll.Tracks,"the credits' tracks aren't in alphabetical order");
            string manifest=File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../docs/music/tracks.json")));
            var listed=System.Text.RegularExpressions.Regex.Matches(manifest,"\"title\":\\s*\"([^\"]+)\"").Select(m=>m.Groups[1].Value).ToList();
            CollectionAssert.AreEquivalent(staged,listed,"docs/music/tracks.json doesn't list exactly the staged tracks");
            // Each staged as the intake stages music: streamed Vorbis, stereo, loaded in the background.
            foreach(string title in staged)
            {
                var importer=(UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath("Assets/_Project/Resources/Audio/Music/"+title+".ogg");
                var settings=importer.defaultSampleSettings;
                Assert.AreEqual(AudioClipLoadType.Streaming,settings.loadType,title);Assert.AreEqual(AudioCompressionFormat.Vorbis,settings.compressionFormat,title);
                Assert.IsFalse(importer.forceToMono,title);Assert.IsTrue(importer.loadInBackground,title);
            }
        }

        /// <summary>The three story moments play the track written for each and give the zone its own back: the Coronach walk from the call to the close, the launch from its
        /// start until the zone goes (here, until it is removed, as a load removes it), the anchor's release likewise. Each loops (MusicDirector's voices loop the whole track,
        /// as for every other). Shown first on round ten's state, when each played the stand-in its scene was built with.</summary>
        [UnityTest] public IEnumerator EachStoryMomentPlaysItsOwnTrackAndGivesTheZoneBack()
        {
            yield return Load("Hub_Decks");string home=MusicDirector.Current.Track;Assert.AreEqual("Hub Town Groove",home,"fixture: the Decks' own");
            var flags=GameServices.Current.Flags;var played=new Dictionary<(string moment,bool before),string>();var back=new List<string>();
            string[] keys={"home.coronachCalled","home.coronachWalked","home.launched","end.collarTaken","end.sequenceRead","end.anchorReleased","end.custody"};
            void Clear(){foreach(var key in keys)flags.SetBool(key,false);GameServices.Current.Input.Blocked=false;}
            foreach(bool before in new[]{true,false})
            {
                CoronachWalk.AsRoundTen=CoronachLaunch.AsRoundTen=AnchorRelease.AsRoundTen=before;
                try
                {
                    Clear();yield return null;
                    var walk=new GameObject("The walk").AddComponent<CoronachWalk>();yield return null;
                    flags.SetBool("home.coronachCalled",true);yield return null;yield return null;played[("walk",before)]=MusicDirector.Current.Track;
                    Assert.IsTrue(MusicDirector.Current.ActiveSource.loop,"the walk's track doesn't loop");
                    flags.SetBool("home.coronachWalked",true);yield return null;yield return null;
                    if(MusicDirector.Current.Track!=home)back.Add($"the walk's close left {MusicDirector.Current.Track} playing ({(before?"round ten":"round eleven")})");
                    Object.Destroy(walk.gameObject);yield return null;
                    var launch=new GameObject("The launch").AddComponent<CoronachLaunch>();yield return null;yield return null;
                    Assert.AreNotEqual(CoronachLaunch.Stage.Waiting,launch.Now,"fixture: the launch never began after the close");
                    played[("launch",before)]=MusicDirector.Current.Track;
                    Object.Destroy(launch.gameObject);yield return null;GameServices.Current.Input.Blocked=false;
                    if(MusicDirector.Current.Track!=home)back.Add($"the launch, gone, left {MusicDirector.Current.Track} playing ({(before?"round ten":"round eleven")})");
                    var anchor=new GameObject("The anchor").AddComponent<AnchorRelease>();yield return null;yield return null;
                    flags.SetBool("end.collarTaken",true);yield return null;yield return null;
                    Assert.AreEqual(AnchorRelease.Stage.Opening,anchor.Now,"fixture: the anchor never let go");
                    played[("anchor",before)]=MusicDirector.Current.Track;
                    Object.Destroy(anchor.gameObject);yield return null;
                    if(MusicDirector.Current.Track!=home)back.Add($"the anchor, gone, left {MusicDirector.Current.Track} playing ({(before?"round ten":"round eleven")})");
                }
                finally{CoronachWalk.AsRoundTen=CoronachLaunch.AsRoundTen=AnchorRelease.AsRoundTen=false;Clear();}
            }
            var written=new Dictionary<string,string>{["walk"]="Coronach",["launch"]="The House That Can Leave",["anchor"]="Room To Turn Back"};
            Assert.AreEqual(CoronachWalk.Track,written["walk"]);Assert.AreEqual(CoronachLaunch.Track,written["launch"]);Assert.AreEqual(AnchorRelease.Track,written["anchor"]);
            foreach(var (moment,track) in written)Assert.AreNotEqual(track,played[(moment,true)],$"fixture: the check doesn't catch the {moment} playing its stand-in");
            var wrong=written.Where(w=>played[(w.Key,false)]!=w.Value).Select(w=>$"the {w.Key} plays {played[(w.Key,false)]}, not {w.Value}").ToList();
            Assert.IsEmpty(wrong,string.Join("\n",wrong));
            Assert.IsEmpty(back,string.Join("\n",back));
            TestContext.WriteLine("STORY_MUSIC "+string.Join("; ",played.Select(p=>$"{p.Key.moment} {(p.Key.before?"round ten":"round eleven")}: {p.Value}")));
        }

        /// <summary>D461. The first replays with the story tracks (November 28) logged the Decks' groove cutting in for the half second
        /// between the Coronach and the launch, and for the load at the launch's cut; and the anchor chamber's theme for the load into the
        /// Returning Passage. A place's own track waits a moment before it comes back after a cue: the next cue takes over directly, a zone
        /// left under a cue hands straight to the next place, and with nothing following the place's track does come back. What was asked
        /// for (Track) is still said at once. As round eleven left it the place's track is logged in between, both times.</summary>
        [UnityTest] public IEnumerator APlacesTrackNeverCutsInBetweenTwoCuesOrOnTheWayOut()
        {
            var heard=new List<string>();
            void Log(string text,string trace,LogType type){if(text.StartsWith("MUSIC_CUE "))heard.Add(text.Substring(10));}
            Application.logMessageReceived+=Log;
            try
            {
                foreach(bool before in new[]{true,false})
                {
                    MusicDirector.AsRoundEleven=before;string when=before?"as round eleven":"now";
                    yield return Load("Hub_Decks");string home=MusicDirector.Current.Track;Assert.AreEqual("Hub Town Groove",home,"fixture: the Decks' own");
                    // One story cue ends and the next begins half a second later, as the walk's close and the launch do.
                    var first=new GameObject("The walk's cue");var second=new GameObject("The launch's cue");heard.Clear();
                    MusicDirector.Encounter(first,CoronachWalk.Track,true);yield return null;
                    MusicDirector.Encounter(first,null,false);
                    Assert.AreEqual(home,MusicDirector.Current.Track,"the director no longer says at once what was asked for");
                    yield return new WaitForSecondsRealtime(.5f);
                    MusicDirector.Encounter(second,CoronachLaunch.Track,true);yield return null;
                    string between=when+": "+string.Join(" > ",heard);Debug.Log("MUSIC_HAND_BACK between two cues, "+between);
                    if(before)CollectionAssert.Contains(heard,home,"fixture: the Decks' track didn't cut in between two cues, "+between);
                    else
                    {
                        CollectionAssert.AreEqual(new[]{CoronachWalk.Track,CoronachLaunch.Track},heard,"between two cues, "+between);
                        Assert.AreEqual(CoronachLaunch.Track,MusicDirector.Current.Sounding);
                    }
                    // The second cue's owner goes with its zone (both objects are the zone's): the next place's track follows the cue's directly.
                    heard.Clear();yield return Load("Gullet_Tunnel");
                    string leaving=when+": "+string.Join(" > ",heard);Debug.Log("MUSIC_HAND_BACK leaving a zone under a cue, "+leaving);
                    Assert.AreEqual("Starfight",MusicDirector.Current.Track,"fixture: the Gullet's own");
                    if(before)CollectionAssert.Contains(heard,home,"fixture: the Decks' track didn't cut in on the way out, "+leaving);
                    else CollectionAssert.AreEqual(new[]{"Starfight"},heard,"leaving a zone under a cue, "+leaving);
                    Assert.AreEqual("Starfight",MusicDirector.Current.Sounding,"the new place's track isn't the one sounding");
                    if(before)continue;
                    // And with nothing following, the place's track comes back, after the wait and not before.
                    var third=new GameObject("A boss's cue");MusicDirector.Encounter(third,MusicDirector.BossTrack,true);yield return null;
                    MusicDirector.Encounter(third,null,false);yield return null;
                    Assert.AreEqual("Starfight",MusicDirector.Current.Track);
                    Assert.AreEqual(MusicDirector.BossTrack,MusicDirector.Current.Sounding,"the cue's track didn't play on through the wait");
                    yield return new WaitForSecondsRealtime(MusicDirector.HandBackSeconds+.3f);
                    Assert.AreEqual("Starfight",MusicDirector.Current.Sounding,"the place's track never came back after the cue");
                    Object.Destroy(third);
                }
            }
            finally{MusicDirector.AsRoundEleven=false;Application.logMessageReceived-=Log;}
        }
    }
}
