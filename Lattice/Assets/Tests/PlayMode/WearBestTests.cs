using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The replay driver wears the best it holds, as a player does (QualityRoute.wearBest: ContinuousReview.Gear.cs decides when, ReplayGear what). Until November 15 it
    /// wore only what the game dropped into an empty slot: after route 62 Taren had a Refined Edge and Scrap parts on, with Choirwrought Edges, Frames and Drives in the bag,
    /// and every boss length the chains measured from chapter 3 on was that party's. Each case plays the driver's own steps (RunSteps) in the ground yard and is shown failing
    /// first in the same case: the route without the option (the driver as it was), or the driver as it might have been written (wearing in a fight or a talk, or off
    /// another hero: ContinuousReview.wearAnyTime, wearFromAnyone).</summary>
    public sealed class WearBestTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject fixture;ContinuousReview replay;readonly List<(float at,string line)> log=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        void Logged(string text,string stack,LogType type){if(text.StartsWith("QUALITY_WEARS"))log.Add((Time.unscaledTime,text));}
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            log.Clear();Application.logMessageReceived+=Logged;Broken(false,false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;Broken(false,false);
            if(DialogueSystem.Current!=null&&DialogueSystem.Current.Running)DialogueSystem.Current.Cancel();
            DropDriver();
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the driver, by its private parts (as DriverBoundsTests drives it)
        void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
        T Get<T>(string name)=>(T)typeof(ContinuousReview).GetField(name,Private).GetValue(replay);
        object Call(string name,params object[] arguments)=>typeof(ContinuousReview).GetMethod(name,Private).Invoke(replay,arguments);
        /// <summary>The driver as it might have been written, for a case's broken state; put back by every case and the tear-down.</summary>
        static void Broken(bool anyTime,bool fromAnyone)
        {
            typeof(ContinuousReview).GetField("wearAnyTime",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,anyTime);
            typeof(ContinuousReview).GetField("wearFromAnyone",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,fromAnyone);
        }
        /// <summary>A driver of its own for each part of a case, as each route is a player of its own: never active (its Start wants a player's launch arguments).</summary>
        void NewDriver()
        {
            DropDriver();
            fixture=new GameObject("Wear best fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();
        }
        void DropDriver()
        {
            if(replay!=null){var pad=Get<Gamepad>("pad");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);Set("pad",null);}
            if(fixture!=null)Object.Destroy(fixture);fixture=null;replay=null;
        }
        /// <summary>Plays a route's steps through the driver's own RunSteps, a frame at a time.</summary>
        IEnumerator Play(QualityRoute route,float most)
        {
            if(Get<Gamepad>("pad")==null)Set("pad",InputSystem.AddDevice<Gamepad>("WearBestPad"));
            Call("InputUpdate");yield return null;yield return null;
            Set("route",route);Set("recording",true);var clock=Get<System.Diagnostics.Stopwatch>("clock");if(!clock.IsRunning)clock.Start();
            var run=(IEnumerator)Call("RunSteps");
            for(float end=Time.unscaledTime+most;Time.unscaledTime<end;){if(!run.MoveNext())break;Call("InputUpdate");yield return null;}
            Set("recording",false);Call("InputUpdate");
        }
        static QualityRoute Standing(bool wear,float seconds,string name="stands in the yard")=>
            new(){name="wear best",scene="Arena_Ground",wearBest=wear,steps=new[]{new QualityStep{name=name,seconds=seconds}}};

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static MemberState Member(string id)=>State.party.Find(m=>m.id==id);
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>A party from a chapter's loadout in the ground yard, empty, nobody's brain fighting for them.</summary>
        static IEnumerator Yard(string loadout)
        {
            DevLoadout.Apply(loadout);
            SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: the yard never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var hero in PartyController.Current.members){hero.GetComponent<PartnerBrain>().enabled=false;hero.Health.InvulnerableUntil=GameTime.Now+600;}
            yield return new WaitForSecondsRealtime(.7f);
            Assert.AreEqual("Taren",Lead.character,"fixture: Taren leads");
        }
        static PartInstance Spare(string id,int upgrade=0)
        {
            var def=GameCatalog.Find<TechPartDef>(id);Assert.IsNotNull(def,"fixture: a part called "+id);
            var part=new Inventory(State).GivePart(def);part.upgrade=upgrade;return part;
        }
        static PartInstance Wearing(string hero,GearSlot slot,bool second=false)=>ReplayGear.Worn(State,Member(hero),slot,second);
        static string Id(PartInstance part)=>part!=null?part.definitionId:"nothing";
        /// <summary>Everything every hero has on, by hero and slot, by instance.</summary>
        static Dictionary<string,string> Worn()
        {
            var worn=new Dictionary<string,string>();
            foreach(var member in State.party){foreach(var on in member.equipped)worn[member.id+" "+on.Key]=on.Value;worn[member.id+" Module2"]=member.module2??"";}
            return worn;
        }

        // ================================================================== a route begins
        /// <summary>Chapter 3's party (Refined Edge, Scrap Emitter on Taren; a Refined Emitter and a Scrap Edge on Sela) with Taren's Edge a Tempered one at +5 and, in the bag, a
        /// plain Choirwrought Edge, a Tempered Frame and its light twin, a Prime Emitter and a Prime Module. When the route begins each goes where the page's numbers put it: the
        /// Prime Emitter to Sela, who shoots with it, and her old Refined one to Taren; the Choirwrought Edge to Sela's Edge slot and not Taren's, where the page shows it 95 against
        /// his 98; the better Frame to the lead and the light one to Sela; the Prime Module into the lead's first module slot, and his Refined one into his empty second.</summary>
        [UnityTest] public IEnumerator WhenARouteBeginsEachHeroOutIsPutIntoTheBestSpareThePagesNumbersShow()
        {
            PartInstance tempered=null;
            IEnumerator Begin(bool wear)
            {
                yield return Yard("chapter3");
                Assert.AreEqual("EdgesT2",Id(Wearing("Taren",GearSlot.Edge)),"fixture: chapter 3's Taren arrives in a Refined Edge");
                Assert.AreEqual("EdgesT1",Id(Wearing("Sela",GearSlot.Edge)),"fixture: chapter 3's Sela carries a Scrap Edge");
                var inventory=new Inventory(State);tempered=Spare("EdgesT4",5);Assert.IsTrue(inventory.Equip(Member("Taren"),tempered,GameCatalog.Find<TechPartDef>("EdgesT4")),"fixture: Taren puts on a Tempered Edge at +5");
                foreach(string id in new[]{"EdgesT5","FramesT4","FramesT4Light","EmittersT3","ModulesT3"})Spare(id);
                PartyController.Current.RefreshStats();
                Assert.Greater(ReplayGear.Worth(GameCatalog.Find<TechPartDef>("EdgesT4"),tempered),ReplayGear.Worth(GameCatalog.Find<TechPartDef>("EdgesT5"),new PartInstance{definitionId="EdgesT5"}),
                    "fixture: the page shows the Tempered Edge at +5 above a plain Choirwrought one");
                NewDriver();yield return Play(Standing(wear,2),6);
            }
            // As the driver was: the route doesn't ask, and the spares stay in the bag.
            yield return Begin(false);
            Assert.AreEqual("EmittersT1",Id(Wearing("Taren",GearSlot.Emitter)),"fixture: without wearBest the driver put an Emitter on Taren: the check below proves nothing");
            Assert.AreEqual("EdgesT1",Id(Wearing("Sela",GearSlot.Edge)),"fixture: without wearBest the driver put an Edge on Sela: the check below proves nothing");
            CollectionAssert.IsEmpty(log,"fixture: without wearBest the driver logged a part put on");
            yield return Cleanup();yield return Boot();
            yield return Begin(true);
            string Said()=>string.Join(" | ",log.Select(l=>l.line));
            Assert.AreSame(tempered,Wearing("Taren",GearSlot.Edge),"Taren's Tempered Edge at +5 (98 on the page) came off for a plain Choirwrought one (95): the highest tier isn't the best part. "+Said());
            Assert.AreEqual("EdgesT5",Id(Wearing("Sela",GearSlot.Edge)),"Sela's Scrap Edge wasn't changed for the spare Choirwrought one. "+Said());
            Assert.AreEqual("EmittersT3",Id(Wearing("Sela",GearSlot.Emitter)),"the Prime Emitter didn't go to Sela, who shoots with it. "+Said());
            Assert.AreEqual("EmittersT2",Id(Wearing("Taren",GearSlot.Emitter)),"Taren's Scrap Emitter wasn't changed for the Refined one Sela put down. "+Said());
            Assert.AreEqual("FramesT4",Id(Wearing("Taren",GearSlot.Frame)),"the Tempered Frame isn't on the lead. "+Said());
            Assert.AreEqual("FramesT4Light",Id(Wearing("Sela",GearSlot.Frame)),"the light Tempered Frame isn't on Sela. "+Said());
            // Every hero's first module before anybody's second: the Prime Module replaces the lead's Refined one, which then fills his empty second slot.
            Assert.AreEqual("ModulesT3",Id(Wearing("Taren",GearSlot.Module)),"the Prime Module isn't in Taren's first module slot. "+Said());
            Assert.AreEqual("ModulesT2",Id(Wearing("Taren",GearSlot.Module,true)),"the Refined Module Taren put down isn't in his empty second slot. "+Said());
            Assert.IsTrue(log.Any(l=>l.line=="QUALITY_WEARS Sela Edge EdgesT5 (was EdgesT1)"),"the log doesn't say what was put on, in what slot, in place of what: "+Said());
            // What was put on reaches the fight at once: the body's blow is the Party page's sum for the parts she has on now (PartyController.RefreshStats).
            var sela=PartyController.Current.members.First(m=>m.character=="Sela");
            float blade=Gear.Calculate(State,Member("Sela"),GameCatalog.Find<CharacterDef>("Sela"),GearSlot.Edge,GameCatalog.Find<TechPartDef>,GameCatalog.Find<AffixDef>).output;
            Assert.AreEqual(blade,sela.damage,.01f,"Sela's blow in the fight isn't what the parts she now wears add up to: the stats weren't refreshed after the change");
        }

        // ================================================================== a fight, then what it left
        /// <summary>A hound awake beside the party and, while it is, a Tempered Edge into the bag (as a drop arrives): nothing is put on while it fights, and the Edge goes on Taren
        /// once it is dead. The driver as it might have been written, putting parts on whenever it looks, wears it mid-fight.</summary>
        [UnityTest] public IEnumerator APartTheBagGetsInAFightIsPutOnOnceTheFightIsOverAndNeverDuringIt()
        {
            EnemyBrain hound=null;
            IEnumerator Fight(bool anyTime)
            {
                yield return Yard("chapter3");Broken(anyTime,false);
                NewDriver();yield return Play(Standing(true,1.5f,"before the fight"),4);
                log.Clear();
                hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),Lead.transform.position+Vector3.forward*6);yield return Frames(3);
                hound.Health.maximum=hound.Health.integrity=1000000;
                Assert.IsTrue(GearEffects.InFight(Lead.transform.position),"fixture: a fight is on around the lead");
                Spare("EdgesT4");
                yield return Play(Standing(true,4,"in the fight"),8);
                Assert.IsTrue(hound!=null&&hound.Health.Alive,"fixture: the hound fought on through the step");
            }
            // As it might have been written: worn in the fight.
            yield return Fight(true);
            Assert.AreEqual("EdgesT4",Id(Wearing("Taren",GearSlot.Edge)),"fixture: the driver that wears whenever it looks didn't put the Tempered Edge on in the fight: the check below proves nothing");
            Broken(false,false);yield return Cleanup();yield return Boot();
            yield return Fight(false);
            Assert.AreEqual("EdgesT2",Id(Wearing("Taren",GearSlot.Edge)),"a part was put on with a hound awake beside the lead: "+string.Join(" | ",log.Select(l=>l.line)));
            CollectionAssert.IsEmpty(log,"the driver changed parts in a fight");
            hound.Health.Receive(new DamagePacket{amount=hound.Health.maximum*10,type=DamageType.Kinetic});float killed=Time.unscaledTime;yield return Frames(3);
            Assert.IsFalse(hound.Health.Alive,"fixture: the hound is dead");
            yield return Play(Standing(true,3,"after the fight"),6);
            Assert.AreEqual("EdgesT4",Id(Wearing("Taren",GearSlot.Edge)),"the Tempered Edge the bag got in the fight wasn't put on once it was over: "+string.Join(" | ",log.Select(l=>l.line)));
            Assert.IsTrue(log.All(l=>l.at>=killed),"a part went on before the hound was dead");
        }

        // ================================================================== a talk
        /// <summary>Hal's yard talk open, a Tempered Edge in the bag: nothing is put on while it runs, and the Edge goes on once it is over. Wearing whenever it looks, it went on mid-talk.</summary>
        [UnityTest] public IEnumerator NothingIsPutOnInATalkAndTheBestIsPutOnOnceItEnds()
        {
            IEnumerator Talk(bool anyTime)
            {
                yield return Yard("chapter3");Broken(anyTime,false);
                var dialogue=DialogueSystem.Current;float deadline=Time.unscaledTime+10;while(!dialogue.Prepared&&Time.unscaledTime<deadline)yield return null;
                dialogue.AutoAdvance=false;Assert.IsTrue(dialogue.StartNode("ArenaGuide","Hal"),"fixture: Hal's yard talk starts");yield return Frames(5);
                Assert.IsTrue(dialogue.Running,"fixture: the talk is running");
                Spare("EdgesT4");NewDriver();
                yield return Play(Standing(true,3,"in the talk"),6);
                Assert.IsTrue(dialogue.Running,"fixture: the talk ran through the step");
            }
            yield return Talk(true);
            Assert.AreEqual("EdgesT4",Id(Wearing("Taren",GearSlot.Edge)),"fixture: the driver that wears whenever it looks didn't put the Edge on in the talk: the check below proves nothing");
            DialogueSystem.Current.Cancel();Broken(false,false);yield return Cleanup();yield return Boot();
            yield return Talk(false);
            Assert.AreEqual("EdgesT2",Id(Wearing("Taren",GearSlot.Edge)),"a part was put on in the middle of a talk: "+string.Join(" | ",log.Select(l=>l.line)));
            DialogueSystem.Current.Cancel();yield return Frames(10);
            Assert.IsFalse(DialogueSystem.Current.Running,"fixture: the talk is over");
            yield return Play(Standing(true,3,"after the talk"),6);
            Assert.AreEqual("EdgesT4",Id(Wearing("Taren",GearSlot.Edge)),"the Tempered Edge wasn't put on once the talk was over: "+string.Join(" | ",log.Select(l=>l.line)));
        }

        // ================================================================== another hero's parts
        /// <summary>Chapter 8's three, Taren and Idra out and Sela resting, Idra in a Choirwrought Frame that would be an upgrade on Taren's Tempered one, Sela's Tempered Emitter an
        /// upgrade on his Prime one, and only a worse Frame spare: nothing comes off Idra or Sela, and nothing changes at all. The driver as it might have been written, counting
        /// a worn part as spare, takes parts off them.</summary>
        [UnityTest] public IEnumerator NoPartIsEverTakenOffAnotherHero()
        {
            PartInstance frame=null;Dictionary<string,string> before=null;
            IEnumerator Party(bool fromAnyone)
            {
                yield return Yard("chapter8");Broken(false,fromAnyone);
                Assert.IsTrue(PartyController.Current.members.Any(m=>m.character=="Idra")&&!PartyController.Current.members.Any(m=>m.character=="Sela"),"fixture: Taren and Idra are out, Sela rests");
                frame=Spare("FramesT5");Assert.IsTrue(new Inventory(State).Equip(Member("Idra"),frame,GameCatalog.Find<TechPartDef>("FramesT5")),"fixture: Idra wears a Choirwrought Frame");
                Spare("FramesT3");PartyController.Current.RefreshStats();
                Assert.AreEqual("FramesT4",Id(Wearing("Taren",GearSlot.Frame)),"fixture: Taren wears a Tempered Frame");
                Assert.AreEqual("EmittersT4",Id(Wearing("Sela",GearSlot.Emitter)),"fixture: Sela rests in a Tempered Emitter");
                Assert.AreEqual("EmittersT3",Id(Wearing("Taren",GearSlot.Emitter)),"fixture: Taren's Emitter is a Prime one");
                before=Worn();NewDriver();yield return Play(Standing(true,2),6);
            }
            yield return Party(true);
            Assert.IsTrue(log.Any(l=>l.line.EndsWith(" off Idra")||l.line.EndsWith(" off Sela")),
                "fixture: the driver that counts a worn part as spare took nothing off Idra or Sela: the check below proves nothing. "+string.Join(" | ",log.Select(l=>l.line)));
            Broken(false,false);yield return Cleanup();yield return Boot();
            yield return Party(false);
            Assert.AreSame(frame,Wearing("Idra",GearSlot.Frame),"Idra's Choirwrought Frame was taken off her: "+string.Join(" | ",log.Select(l=>l.line)));
            Assert.AreEqual("EmittersT4",Id(Wearing("Sela",GearSlot.Emitter)),"the resting Sela's Emitter was taken: "+string.Join(" | ",log.Select(l=>l.line)));
            CollectionAssert.AreEquivalent(before,Worn(),"with nothing spare better than what anyone wears, the driver changed somebody's parts: "+string.Join(" | ",log.Select(l=>l.line)));
            CollectionAssert.IsEmpty(log,"the driver said it put something on with nothing better spare");
        }

        // ================================================================== the routes
        static string Folder=>System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
        static QualityRoute Read(string name)=>JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(System.IO.Path.Combine(Folder,name+".json")));
        /// <summary>The drill (chapter-05-drill-equipment) teaches the Gear page with the pad, and is played as it always was: read from its file, the driver puts nothing on in its
        /// route. The route after it (chapter-06-burrower), read from its file, does. And every chapter route after the drill says so, and none before it.</summary>
        [UnityTest] public IEnumerator TheDrillIsLeftToItsOwnPressesAndEveryRouteAfterItWearsTheBest()
        {
            var drill=Read("chapter-05-drill-equipment");var after=Read("chapter-06-burrower");
            Assert.IsFalse(drill.wearBest,"the Gear page's drill asks the driver to wear parts for itself: its own presses on the page would meet parts already moved");
            IEnumerator Played(QualityRoute file)
            {
                yield return Yard("chapter3");Spare("EdgesT4");NewDriver();
                var route=Standing(file.wearBest,2,"a step of "+file.name);route.name=file.name;
                yield return Play(route,6);
            }
            yield return Played(drill);
            Assert.AreEqual("EdgesT2",Id(Wearing("Taren",GearSlot.Edge)),"the driver put a part on in the drill's route: "+string.Join(" | ",log.Select(l=>l.line)));
            yield return Cleanup();yield return Boot();
            yield return Played(after);
            Assert.AreEqual("EdgesT4",Id(Wearing("Taren",GearSlot.Edge)),"chapter-06-burrower, the route after the drill, doesn't wear the best the party holds");
            var wrong=new List<string>();
            foreach(string path in System.IO.Directory.GetFiles(Folder,"chapter-*.json"))
            {
                string name=System.IO.Path.GetFileNameWithoutExtension(path);if(!int.TryParse(name.Split('-')[1],out int number))continue;
                bool wears=Read(name).wearBest;
                if(number<=5&&wears)wrong.Add(name+" wears the best, at or before the drill");
                if(number>5&&!wears)wrong.Add(name+" doesn't wear the best");
            }
            CollectionAssert.IsEmpty(wrong,"routes that don't wear what they should: "+string.Join("; ",wrong));
        }
    }
}
