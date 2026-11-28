using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Lattice.Tests.PlayMode
{
    /// <summary>QA-034, round eleven: the frame a boss wakes in. The capture of record (nov25-whole s06 step 20) entered the Burrower's excavation in one 40 ms frame,
    /// its main thread about 25 ms of it: the first boss of the game, so the first time anything of a boss's runs. Here the encounter at its door begins as the party
    /// crosses in, in its own zone, three times a run each for the Burrower and the Bellows Below, and each waking is timed frame by frame with what was made in it.</summary>
    public sealed class BossWakingFrameTests
    {
        [UnitySetUp] public IEnumerator Boot(){GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);}
        [UnityTearDown] public IEnumerator Cleanup(){GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}

        /// <summary>The frames read before the door and after it.</summary>
        const int Quiet=12,Window=6;
        /// <summary>One waking: the call that began it, then each frame from it on, its whole length and its main thread's work (the player loop less its waits), and what
        /// the window made: objects, texts, textures/sprites/materials/meshes, audio clips, glyphs, garbage.</summary>
        sealed class Waking
        {
            public string boss;public int time;public double call,quiet;public double[] wall=new double[Window],work=new double[Window];
            public int made,texts,assets,clips,glyphs;public long garbage;public bool bar;public string card,track;
            public double Worst=>work.Max();
            public override string ToString()=>$"{boss} #{time}: the call {call:0.0} ms; frames {string.Join(" ",wall.Select(w=>w.ToString("0.0")))}; work {string.Join(" ",work.Select(w=>w.ToString("0.0")))} (quiet {quiet:0.0}); "+
                $"{made} objects, {texts} texts, {assets} assets, {clips} clips, {glyphs} glyphs, {garbage/1024} KB; bar {(bar?"up":"down")}, card '{card}', track '{track}'";
        }
        static int Objects()=>Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
        static int Texts()=>Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
        static int Assets()=>Resources.FindObjectsOfTypeAll<Texture>().Length+Resources.FindObjectsOfTypeAll<Sprite>().Length+Resources.FindObjectsOfTypeAll<Material>().Length+Resources.FindObjectsOfTypeAll<Mesh>().Length;
        static int Clips()=>Resources.FindObjectsOfTypeAll<AudioClip>().Length;
        static int Glyphs()=>Resources.FindObjectsOfTypeAll<TMP_FontAsset>().Sum(f=>f.characterTable!=null?f.characterTable.Count:0);
        /// <summary>A recorder for a marker by its name, whatever its category.</summary>
        static ProfilerRecorder Recorder(string name)
        {
            var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
            foreach(var handle in handles)if(ProfilerRecorderHandle.GetDescription(handle).Name==name)return new ProfilerRecorder(handle,1,ProfilerRecorderOptions.Default|ProfilerRecorderOptions.StartImmediately);
            return default;
        }
        static double Ms(ProfilerRecorder r)=>r.Valid?r.LastValue/1e6:0;

        /// <summary>Loads the zone with the loadout, lets it settle, and begins the encounter at the door with the party put inside it, timing that frame and the next.</summary>
        static IEnumerator Wake(string loadout,string zone,string door,int time,List<Waking> into)
        {
            DevLoadout.Apply(loadout);SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            var party=PartyController.Current;
            foreach(var hero in party.members){hero.GetComponent<PlayerBrain>().AutoPilot=true;var partner=hero.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;hero.Health.InvulnerableUntil=GameTime.Now+600;}
            var encounter=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId==door);
            Assert.IsFalse(encounter.Started,$"fixture: {door} began before the party reached it");
            var inside=encounter.transform.position;if(NavMesh.SamplePosition(inside,out var floor,8,NavMesh.AllAreas))inside=floor.position;
            // Past the load and what is built as a zone comes in (the pages' warm-up, two frames in), as a player walking to the door is.
            yield return new WaitForSecondsRealtime(2f);
            using var loop=Recorder("PlayerLoop");using var target=Recorder("WaitForTargetFPS");using var present=Recorder("Gfx.WaitForPresentOnGfxThread");
            double Work()=>Ms(loop)-Ms(target)-Ms(present);
            var quiet=new List<double>();for(int i=0;i<Quiet;i++){yield return null;quiet.Add(Work());}
            var w=new Waking{boss=door,time=time,quiet=quiet.OrderBy(q=>q).ElementAt(Quiet/2)};
            int made=Objects(),texts=Texts(),assets=Assets(),clips=Clips(),glyphs=Glyphs();long garbage=System.GC.GetAllocatedBytesForCurrentThread();
            var clock=Stopwatch.StartNew();
            // A save is written behind the frame in a player (SaveSystem.WriteBehind); the editor writes it on the frame, so the door's checkpoint is played as the player plays it.
            SaveSystem.WriteBehind=true;
            encounter.Begin();
            for(int i=0;i<party.members.Length;i++)Place(party.members[i],inside+new Vector3(i*1.6f,0,0));
            Physics.SyncTransforms();w.call=clock.Elapsed.TotalMilliseconds;
            double last=0;
            for(int k=0;k<Window;k++){yield return null;double now=clock.Elapsed.TotalMilliseconds;w.wall[k]=now-last;last=now;w.work[k]=Work();}
            w.garbage=System.GC.GetAllocatedBytesForCurrentThread()-garbage;SaveSystem.Drain();SaveSystem.WriteBehind=false;
            w.made=Objects()-made;w.texts=Texts()-texts;w.assets=Assets()-assets;w.clips=Clips()-clips;w.glyphs=Glyphs()-glyphs;
            var bar=GameObject.Find("BossBar");w.bar=bar!=null&&bar.activeInHierarchy;
            var card=Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="BossTitleCard");w.card=card!=null?card.text:"(none)";
            w.track=MusicDirector.Current!=null?MusicDirector.Current.Track:"(none)";
            Assert.AreEqual(1,encounter.Remaining,$"fixture: {door} woke no boss");
            into.Add(w);TestContext.WriteLine("BOSS_WAKE "+w);
            if(!loop.Valid||!target.Valid)TestContext.WriteLine($"BOSS_WAKE recorders: PlayerLoop {loop.Valid}, WaitForTargetFPS {target.Valid}, present {present.Valid}");
        }
        static void Place(CombatActor actor,Vector3 point)
        {var controller=actor.GetComponent<CharacterController>();if(controller!=null)controller.enabled=false;actor.transform.position=point;if(controller!=null)controller.enabled=true;}

        /// <summary>The Burrower woken three times in a run, then the Bellows Below three times: the run's first boss against its own third, and a later boss's first against its third.</summary>
        [UnityTest,Timeout(400000)] public IEnumerator ABossWakesInAFrameLikeItsThird()
        {
            var burrower=new List<Waking>();var bellows=new List<Waking>();
            for(int i=1;i<=3;i++)yield return Wake("starter","Sorrel_Ridges","Sorrel_Burrower",i,burrower);
            for(int i=1;i<=3;i++)yield return Wake("hushwell","Hushwell","Hushwell_Bellows",i,bellows);
            TestContext.WriteLine($"BOSS_WAKE worst work: Burrower {string.Join(" ",burrower.Select(w=>w.Worst.ToString("0.0")))}; Bellows {string.Join(" ",bellows.Select(w=>w.Worst.ToString("0.0")))}");
            // Read (r11c-boss-before, the save written on the frame as the editor writes it): the Burrower's call 23.2, 14.8 and 12.2 ms, its worst frame's work 49.8, 20.6
            // and 18.6; the Bellows 38.4, 22.4 and 26.6. With the save written behind the frame as a player writes it (r11c-boss-diag): the Burrower 20.8 then 10.5 ms in its
            // first two frames against 9.2 and 3.7 for its third, the Bellows 9.1, 10.0 and 11.6, the quiet frames about 3. The boss's track is streamed and loads in .3 ms.
            // So the run's first boss pays about 18 ms of first-run code over two frames and every boss about 6 ms of its own waking; nothing is loaded or drawn for it.
            // Those clocks are left on the record, not held: two runs are not five (D452). What is held is the count, which doesn't scatter.
            foreach(var w in burrower.Concat(bellows))
            {
                Assert.AreEqual(0,w.assets,$"{w.boss} #{w.time}: its waking made {w.assets} textures, sprites, materials or meshes on its frames");
                Assert.AreEqual(0,w.texts,$"{w.boss} #{w.time}: its waking made {w.texts} texts: the boss bar or its card was built on the frame");
                Assert.AreEqual(0,w.glyphs,$"{w.boss} #{w.time}: its card drew {w.glyphs} new glyphs on the frame");
                Assert.LessOrEqual(w.clips,w==burrower[0]?1:0,$"{w.boss} #{w.time}: its waking loaded {w.clips} audio clips");
                Assert.IsTrue(w.bar&&w.card.Length>0,$"fixture: {w.boss} #{w.time} woke without its bar or its card");
            }
        }
    }
}
