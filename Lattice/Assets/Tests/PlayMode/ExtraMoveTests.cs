using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>S3: the EXTRA gauge and the two EXTRA moves. The gauge: nothing until the story's flag, what fills it and at what rate, that it never runs down and is saved
    /// with the hero, and what the HUD and the Skills page show of it. The moves: what each adds up to, that the hero can't be touched, that the partner never fires one,
    /// that a mend revives nobody, the cut-in, the press on a pad, the lesson, and that a boss's phase shift is neither hurt through nor skipped.</summary>
    public sealed class ExtraMoveTests
    {
        Gamepad pad;readonly List<string> log=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        void Logged(string text,string stack,LogType type){if(text.StartsWith("EXTRA_")||text.StartsWith("BOSS_SHIFT"))log.Add(text);}
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            // Headless has no focused Game view. Route virtual devices into play.
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");log.Clear();Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);pad=null;
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static CombatActor Taren=>PartyController.Current.members.First(m=>m.character=="Taren");
        static CombatActor Sela=>PartyController.Current.members.First(m=>m.character=="Sela");
        static GameState State=>GameServices.Current.State;
        static MemberState Member(string id)=>State.party.Find(m=>m.id==id);
        static void Unlock()=>GameServices.Current.Flags.SetBool(ExtraGauge.Flag,true);
        static void Quiet(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        static IEnumerator Zone(bool flight)
        {
            SceneFlow.Current.LoadZone(flight?"Arena_Flight":"Arena_Ground");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"the arena never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            Quiet();
            yield return new WaitForSecondsRealtime(.7f);
            if(!flight){var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Extra fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
            // Sync 13, where the plan has the party when the story unlocks the gauge.
            foreach(var member in State.party){member.level=13;member.skillUses?.Clear();}
            PartyController.Current.RefreshStats();
            Assert.IsNotNull(Taren.GetComponent<ExtraMove>(),"a hero isn't built with an EXTRA move");
            Assert.IsNotNull(Taren.GetComponent<ExtraMove>().Definition,"Taren's EXTRA move has no definition: run Lattice.EditorTools.SkillsLate.Build");
        }
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}
            else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            Physics.SyncTransforms();
        }
        static EnemyBrain Enemy(string id,Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;return enemy;}
        /// <summary>A body that never dies or breaks, and that no damage type is strong or weak against.</summary>
        static EnemyBrain Dummy(bool flight,Vector3 at)
        {
            var enemy=Enemy(flight?"ChoristerDart":"Ridgehound",at);var body=enemy.Health;
            body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;body.weakness=body.resistance=DamageType.Pulse;return enemy;
        }
        static CombatActor Lead(string hero)
        {
            var party=PartyController.Current;if(party.Active.character!=hero)Assert.IsTrue(party.Swap(),"fixture: the swap");
            Quiet();return party.Active;
        }
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Drive(CombatActor actor,float seconds,Vector2 stick=default)
        {for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){actor.motor.Move(stick,false,false);yield return null;}}
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static void Hurt(CombatActor hero,float share){hero.Health.integrity=hero.Health.maximum*share;}
        static float Output(CombatActor hero,bool ranged)=>(ranged?hero.rangedDamage:hero.damage)*(1+Mathf.Max(0,hero.resonance-10)*.025f);
        sealed class Hits{public int count;public float plain;public readonly List<float> each=new();}
        /// <summary>An EXTRA's hits on a body, with a crit's bonus taken back out so the total can be held to.</summary>
        static Hits Watch(EnemyBrain body,CombatActor hero)
        {
            var hits=new Hits();
            body.Health.Damaged+=(_,packet,amount)=>
            {
                if(packet.source!=hero.Health||packet.tag!=ExtraMove.Tag||amount<=0)return;
                float plain=packet.isCrit?amount/(1.5f*(1+Mathf.Max(0,hero.fortune)*.01f+hero.mods.critDamage)):amount;
                hits.count++;hits.plain+=plain;hits.each.Add(plain);
            };
            return hits;
        }
        /// <summary>Fills the hero's gauge, fires, and drives them until the move is over.</summary>
        static IEnumerator Fire(CombatActor hero)
        {
            var move=hero.GetComponent<ExtraMove>();for(float end=Time.unscaledTime+3;move.Performing&&Time.unscaledTime<end;)yield return null;
            Member(hero.character).extra=ExtraGauge.Full;
            Assert.IsTrue(move.TryFire(),$"fixture: {hero.character}'s EXTRA wouldn't start with a full gauge");
            for(float end=Time.unscaledTime+6;move.Performing&&Time.unscaledTime<end;){hero.motor.Move(Vector2.zero,false,false);yield return null;}
            Assert.IsFalse(move.Performing,"the EXTRA never ended");
        }
        static Button Row(string name)=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name==name);
        static string Words(Component c)=>ListRow.Words(c);

        // ------------------------------------------------------------------ the gauge
        [UnityTest] public IEnumerator ThereIsNoGaugeUntilTheStoryUnlocksIt()
        {
            yield return Zone(false);var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(204,0,0));
            var hud=Object.FindFirstObjectByType<ExtraHud>();Assert.IsNotNull(hud,"the HUD has no EXTRA gauge at all");
            var body=Dummy(false,new Vector3(200,0,3));yield return Frames(3);
            Assert.IsFalse(ExtraGauge.Unlocked,"fixture: a fresh save has no gauge");
            body.Health.Receive(new DamagePacket{source=taren.Health,amount=taren.damage*3,type=DamageType.Kinetic});
            Assert.Greater(taren.Health.Receive(new DamagePacket{source=body.Health,amount=40,type=DamageType.Kinetic}),0,"fixture: a hit on him landed");
            yield return Frames(3);
            Assert.AreEqual(0,Member("Taren").extra,.0001f,"the gauge filled before the story unlocked it");
            Assert.AreEqual(-1,hud.Shown,"the gauge is drawn before the story unlocks it");Assert.AreEqual(-1,hud.PartnerShown,"the partner's gauge is drawn before the story unlocks it");
            Assert.AreEqual("",hud.Prompt,"the EXTRA's buttons are shown before there's an EXTRA");
            // Even a save that somehow holds a full one: nothing to fire.
            Member("Taren").extra=ExtraGauge.Full;
            Assert.IsFalse(taren.GetComponent<ExtraMove>().TryFire(),"an EXTRA fired before the story unlocked it");
            Member("Taren").extra=0;
            Unlock();yield return Frames(3);
            Assert.AreEqual(0,hud.Shown,.001f,"with the flag set the lead's gauge isn't on the HUD");Assert.AreEqual(0,hud.PartnerShown,.001f,"with the flag set the partner's gauge isn't on the HUD");
        }
        [UnityTest] public IEnumerator TheGaugeFillsFromHitsDealtAndTakenAndFromAFlashAndThePartnersAtHalf()
        {
            yield return Zone(false);Unlock();var taren=Lead("Taren");var sela=Sela;Place(taren,new Vector3(200,0,0));Place(sela,new Vector3(204,0,0));
            var body=Dummy(false,new Vector3(200,0,3));yield return Frames(3);
            float His()=>ExtraGauge.Of("Taren");float Hers()=>ExtraGauge.Of("Sela");
            // Dealt: a whole hit of the hero's own Output, a partner's at half, a huge one capped, and an EXTRA's own for nothing.
            body.Health.Receive(new DamagePacket{source=taren.Health,amount=taren.damage*taren.damageScale,type=DamageType.Kinetic});
            Assert.AreEqual(ExtraGauge.PerHit,His(),.01f,"a whole landed hit by the lead doesn't fill the gauge by what a hit is worth");
            body.Health.Receive(new DamagePacket{source=sela.Health,amount=sela.rangedDamage*sela.damageScale,type=DamageType.Beam});
            Assert.AreEqual(ExtraGauge.PerHit*ExtraGauge.PartnerShare,Hers(),.01f,"the partner's gauge doesn't fill at half rate from a whole hit of hers");
            Assert.AreEqual(.5f,ExtraGauge.PartnerShare,.001f,"the partner's share isn't a half");
            float was=His();body.Health.Receive(new DamagePacket{source=taren.Health,amount=taren.damage*50,type=DamageType.Kinetic});
            Assert.AreEqual(was+ExtraGauge.PerHit*ExtraGauge.MostHit,His(),.01f,"one enormous hit fills the gauge past its cap for one hit");
            was=His();body.Health.Receive(new DamagePacket{source=taren.Health,amount=taren.damage,type=DamageType.Kinetic,tag=ExtraMove.Tag});
            Assert.AreEqual(was,His(),.0001f,"an EXTRA's own hit pays its gauge back");
            // Taken: by the share of a whole Integrity bar.
            was=His();float took=taren.Health.Receive(new DamagePacket{source=body.Health,amount=50,type=DamageType.Kinetic});
            Assert.Greater(took,0,"fixture: he was hit");
            Assert.AreEqual(was+ExtraGauge.PerBarTaken*took/taren.Health.maximum,His(),.01f,"a hit taken doesn't fill the gauge by its share of his Integrity");
            was=Hers();took=sela.Health.Receive(new DamagePacket{source=body.Health,amount=50,type=DamageType.Kinetic});
            Assert.AreEqual(was+ExtraGauge.PerBarTaken*took/sela.Health.maximum*ExtraGauge.PartnerShare,Hers(),.01f,"a hit the partner takes doesn't fill hers at half rate");
            yield return new WaitForSecondsRealtime(.5f);
            // A Flash Move: ten for the lead, five for the partner.
            was=His();int flashes=taren.FlashMoves;
            Assert.IsTrue(taren.Dodge(Vector3.right),"fixture: a roll");
            taren.Health.Receive(new DamagePacket{source=body.Health,amount=30,type=DamageType.Kinetic});
            Assert.AreEqual(flashes+1,taren.FlashMoves,"fixture: the roll was a Flash Move");
            yield return Frames(3);
            Assert.AreEqual(was+ExtraGauge.PerFlash,His(),.01f,"a Flash Move doesn't add its ten");Assert.AreEqual(10,ExtraGauge.PerFlash,.001f);
            was=Hers();flashes=sela.FlashMoves;
            Assert.IsTrue(sela.Dodge(Vector3.right),"fixture: the partner's roll");
            sela.Health.Receive(new DamagePacket{source=body.Health,amount=30,type=DamageType.Kinetic});
            Assert.AreEqual(flashes+1,sela.FlashMoves,"fixture: the partner's roll was a Flash Move");
            yield return Frames(3);
            Assert.AreEqual(was+ExtraGauge.PerFlash*ExtraGauge.PartnerShare,Hers(),.01f,"the partner's Flash Move doesn't add half of ten");
            // It never runs down, and it stops at a hundred.
            float his=His(),hers=Hers();
            for(float end=Time.unscaledTime+3;Time.unscaledTime<end;){taren.motor.Move(Vector2.zero,false,false);sela.motor.Move(Vector2.zero,false,false);yield return null;}
            Assert.AreEqual(his,His(),.0001f,"the lead's gauge ran down over three idle seconds");Assert.AreEqual(hers,Hers(),.0001f,"the partner's gauge ran down over three idle seconds");
            for(int i=0;i<60;i++)body.Health.Receive(new DamagePacket{source=taren.Health,amount=taren.damage*50,type=DamageType.Kinetic});
            Assert.AreEqual(ExtraGauge.Full,His(),.0001f,"the gauge doesn't stop at a hundred");Assert.IsTrue(ExtraGauge.Ready("Taren"));
        }
        [UnityTest] public IEnumerator TheGaugeIsSavedWithTheHero()
        {
            yield return Zone(false);Unlock();Member("Taren").extra=63;Member("Sela").extra=17;
            // A folder of its own: the player's saves aren't this check's to touch.
            string folder=Path.Combine(Application.temporaryCachePath,"extra-gauge-check");if(Directory.Exists(folder))Directory.Delete(folder,true);
            var saves=new SaveSystem(folder);
            try
            {
                saves.Save("slot1",State);
                var again=saves.Load("slot1");Assert.IsNotNull(again,"fixture: the save reads back");
                Assert.AreEqual(63,again.party.Find(m=>m.id=="Taren").extra,.001f,"the lead's gauge isn't in the save");
                Assert.AreEqual(17,again.party.Find(m=>m.id=="Sela").extra,.001f,"the partner's gauge isn't in the save");
                Assert.IsTrue(again.flags.TryGetValue(ExtraGauge.Flag,out bool unlocked)&&unlocked,"the save lost that the gauge is unlocked");
                // Loaded, it's the game's gauge.
                var kept=GameServices.Current.State;
                try{GameServices.Current.State=again;Assert.AreEqual(63,ExtraGauge.Of("Taren"),.001f,"a loaded save's gauge isn't the one the game reads");}
                finally{GameServices.Current.State=kept;}
                // A save from before there was a gauge: read as an empty one, and nothing breaks.
                string path=saves.PathForSlot("slot1");string json=File.ReadAllText(path);
                string older=System.Text.RegularExpressions.Regex.Replace(json,@",\s*""extra"":\s*[0-9.eE+-]+","");
                Assert.AreNotEqual(json,older,"fixture: the gauge was written as its own field");
                File.WriteAllText(path,older);
                var old=saves.Load("slot1");Assert.IsNotNull(old,"a save with no gauge in it doesn't load");
                Assert.AreEqual(0,old.party.Find(m=>m.id=="Taren").extra,.0001f,"a save with no gauge doesn't start with an empty one");
            }
            finally{SaveSystem.Drain();if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }
        [UnityTest] public IEnumerator TheHudShowsBothGaugesAndTheButtonsOnceFull()
        {
            yield return Zone(false);Unlock();var taren=Lead("Taren");var sela=Sela;Place(taren,new Vector3(200,0,0));Place(sela,new Vector3(204,0,0));
            var hud=Object.FindFirstObjectByType<ExtraHud>();Assert.IsNotNull(hud,"the HUD has no EXTRA gauge");
            Member("Taren").extra=50;Member("Sela").extra=30;yield return Frames(3);
            Assert.AreEqual(.5f,hud.Shown,.001f,"the lead's gauge isn't drawn half full at fifty");
            Assert.AreEqual(.3f,hud.PartnerShown,.001f,"the partner's gauge isn't drawn at thirty");
            Assert.AreEqual("",hud.Prompt,"the buttons are shown with the gauge half full");
            Member("Taren").extra=ExtraGauge.Full;yield return Frames(3);
            Assert.AreEqual(1,hud.Shown,.001f);
            Assert.AreEqual(ExtraGauge.Binding(false),hud.Prompt,"a full gauge doesn't say what to press");Assert.IsNotEmpty(hud.Prompt);
            // Swapped, each gauge follows its hero.
            Lead("Sela");yield return Frames(3);
            Assert.AreEqual(.3f,hud.Shown,.001f,"after a swap the big gauge isn't the new lead's");Assert.AreEqual(1,hud.PartnerShown,.001f,"after a swap the small gauge isn't the old lead's");
            // A partner who's down has no gauge to show: their line is the revive's.
            taren.Health.Receive(new DamagePacket{amount=taren.Health.maximum*10});yield return Frames(3);
            Assert.AreEqual(-1,hud.PartnerShown,"a downed partner's gauge is still drawn over the revive's words");
        }
        [UnityTest] public IEnumerator TheSkillsPageShowsTheExtraMoveOnceItIsUnlocked()
        {
            yield return Zone(false);Assert.AreEqual("Taren",PartyController.Current.Active.character,"fixture: Taren leads");
            var menu=Object.FindFirstObjectByType<PauseMenu>();var move=GameCatalog.Find<CharacterDef>("Taren").extra;
            menu.Open(PauseMenu.SkillsPage);yield return null;
            Assert.IsNull(Row("ExtraRow"),"the Skills page shows an EXTRA move before the story has unlocked it");
            menu.Close();yield return null;
            Unlock();Member("Taren").extra=40;menu.Open(PauseMenu.SkillsPage);yield return null;
            var row=Row("ExtraRow");Assert.IsNotNull(row,"the Skills page doesn't show the EXTRA move");
            StringAssert.Contains(move.displayName,Words(row),"the row doesn't name the move");StringAssert.Contains(ExtraGauge.Binding(false),Words(row),"the row doesn't say what to press");
            StringAssert.Contains("40",Words(row),"the row doesn't say how full the gauge is");
            // The four slots keep their rows and their names.
            for(int slot=0;slot<SkillBook.Slots;slot++)StringAssert.Contains(SkillBook.Equipped("Taren",slot).displayName,Words(Row("Row_"+(slot+1))),$"the EXTRA's row took slot {slot+1}'s place");
            var worn=Enumerable.Range(0,SkillBook.Slots).Select(i=>SkillBook.Equipped("Taren",i)).ToArray();
            row.onClick.Invoke();yield return null;
            var feedback=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="Feedback");
            Assert.IsNotNull(feedback,"selecting the EXTRA's row says nothing");StringAssert.Contains(move.description,feedback.text,"selecting the EXTRA's row doesn't say what the move does");
            CollectionAssert.AreEqual(worn,Enumerable.Range(0,SkillBook.Slots).Select(i=>SkillBook.Equipped("Taren",i)).ToArray(),"selecting the EXTRA's row changed the loadout");
            menu.Close();
        }

        // ------------------------------------------------------------------ Anvil Chorus
        [UnityTest] public IEnumerator AnvilChorusIsTwelveOfHisOutputBehindAHalfSecondCutInAndNothingTouchesHim([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;Unlock();var taren=Lead("Taren");Place(taren,new Vector3(200,y,0));Place(Sela,new Vector3(260,y,0));
            var body=Dummy(flight,new Vector3(200,y,6));yield return Frames(3);taren.target=body.Health;var hits=Watch(body,taren);
            var move=taren.GetComponent<ExtraMove>();var cut=Object.FindFirstObjectByType<ExtraCutIn>();Assert.IsNotNull(cut,"there's no cut-in to play");
            // Short of full it's refused, says so, and spends nothing.
            Member("Taren").extra=99;
            Assert.IsFalse(move.TryFire(),"an EXTRA fired with the gauge short of full");Assert.AreEqual(99,ExtraGauge.Of("Taren"),.001f,"a refused press spent the gauge");
            Assert.Greater(move.RefusedAt,0,"a press with the gauge short doesn't answer");
            Member("Taren").extra=ExtraGauge.Full;
            CombatActor began=null;SkillDef named=null;void Began(CombatActor who,SkillDef what){began=who;named=what;}
            ExtraMove.Began+=Began;
            try{Assert.IsTrue(move.TryFire(),"a full gauge, a fight, a hero on his feet: the EXTRA didn't start");}finally{ExtraMove.Began-=Began;}
            Assert.AreSame(taren,began,"the cut-in wasn't asked for");Assert.AreEqual(SkillKind.AnvilChorus,named.kind,"Taren's EXTRA isn't Anvil Chorus");
            Assert.AreEqual(0,ExtraGauge.Of("Taren"),.001f,"the move didn't spend the gauge");Assert.IsTrue(move.Performing);
            Assert.IsNotNull(BarkService.Line("Taren",ExtraMove.Shout),"Taren has no line to shout: the barks weren't copied (SkillsLate.Build)");
            // The cut-in: the Flash's slow motion, his face and the move's name, and nothing lands yet.
            yield return Drive(taren,.25f);
            Assert.AreEqual(.25f,Time.timeScale,.01f,"the world isn't in the Flash's slow motion during the cut-in");
            Assert.IsTrue(cut.Showing,"no cut-in is on the screen");StringAssert.AreEqualIgnoringCase(named.displayName,cut.Title,"the cut-in doesn't name the move");
            Assert.IsNotNull(cut.Portrait,"the cut-in has no face");
            Assert.AreEqual(0,hits.count,"the move struck during its cut-in");
            Assert.AreEqual(0,taren.Health.Receive(new DamagePacket{source=body.Health,amount=50,type=DamageType.Kinetic}),.0001f,"he can be hurt during the cut-in");
            Assert.IsFalse(taren.Dodge(Vector3.right),"a roll cut the EXTRA short");
            int action=taren.AttackSequence;taren.Guard(true);taren.Stagger(.5f);
            Assert.AreEqual(action,taren.AttackSequence,"a guard or a stagger cut the EXTRA short");
            // A quarter second into the blows: full speed again, the band gone, and still untouchable.
            yield return Drive(taren,.5f,new Vector2(1,0));
            Assert.IsFalse(GameTime.Flashing,"the slow motion outlasts the cut-in");
            Assert.IsFalse(cut.Showing,$"the cut-in is still over the fight {ExtraMove.CutIn+ExtraCutIn.Out:0.00} s after it began");
            Assert.AreEqual(0,taren.Health.Receive(new DamagePacket{source=body.Health,amount=50,type=DamageType.Kinetic}),.0001f,"he can be hurt in the middle of the flurry");
            for(float end=Time.unscaledTime+5;move.Performing&&Time.unscaledTime<end;){taren.motor.Move(Vector2.zero,false,false);yield return null;}
            Assert.IsFalse(move.Performing,"the move never ended");
            Assert.AreEqual(ExtraMove.Beats+1,hits.count,"eight blows and a last one didn't each land on the body in front of him");
            Assert.AreEqual(12,ExtraMove.ChorusTotal,.001f,"Anvil Chorus isn't meant to be twelve");
            Assert.AreEqual(Output(taren,false)*ExtraMove.ChorusTotal,hits.plain,Output(taren,false)*.3f,"the move doesn't add up to twelve of his Output");
            Assert.AreEqual(Output(taren,false)*ExtraMove.ChorusLast,hits.each[hits.each.Count-1],Output(taren,false)*.15f,"it doesn't end on its heavy blow");
            Assert.GreaterOrEqual(move.Dealt,hits.plain*.99f,"the move doesn't count what it dealt");
            Assert.AreEqual(0,ExtraGauge.Of("Taren"),.0001f,"the move's own hits refilled its gauge");
            Assert.IsTrue(log.Any(l=>l.StartsWith("EXTRA_OK Taren")),"the move didn't finish as one");
            // Over, he's a hero again: hurt by what hits him, and free to act.
            yield return Drive(taren,.2f);
            Assert.Greater(taren.Health.Receive(new DamagePacket{source=body.Health,amount=50,type=DamageType.Kinetic}),0,"he's still untouchable after the move");
            Assert.IsFalse(taren.Rooted||taren.Committed,"the move's holds outlast it");
        }

        // ------------------------------------------------------------------ Lattice Aria
        [UnityTest] public IEnumerator LatticeAriaIsTenOfHerOutputOnEveryEnemyInSightAndMendsThoseStanding([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;Unlock();var sela=Lead("Sela");var taren=Taren;Place(sela,new Vector3(200,y,0));Place(taren,new Vector3(flight?206:203,y,-2));
            var near=Dummy(flight,new Vector3(200,y,6));var side=Dummy(flight,new Vector3(203,y,5));var beyond=Dummy(flight,new Vector3(200,y,80));
            yield return Frames(3);sela.target=near.Health;var first=Watch(near,sela);var second=Watch(side,sela);var third=Watch(beyond,sela);
            yield return new WaitForSecondsRealtime(.6f);
            Hurt(sela,.4f);Hurt(taren,.4f);float hers=sela.Health.integrity,his=taren.Health.integrity;
            var move=sela.GetComponent<ExtraMove>();Assert.AreEqual(SkillKind.LatticeAria,move.Definition.kind,"Sela's EXTRA isn't Lattice Aria");
            Assert.IsNotNull(BarkService.Line("Sela",ExtraMove.Shout),"Sela has no line to shout: the barks weren't copied (SkillsLate.Build)");
            Member("Sela").extra=ExtraGauge.Full;Assert.IsTrue(move.TryFire(),"fixture: the aria");
            yield return Drive(sela,.3f);
            Assert.AreEqual(0,first.count,"the aria struck during its cut-in");Assert.AreEqual(hers,sela.Health.integrity,.01f,"the aria mended before its last chord");
            Assert.AreEqual(0,sela.Health.Receive(new DamagePacket{source=near.Health,amount=50,type=DamageType.Kinetic}),.0001f,"she can be hurt during the cut-in");
            for(float end=Time.unscaledTime+5;move.Performing&&Time.unscaledTime<end;){sela.motor.Move(Vector2.zero,false,false);yield return null;}
            Assert.IsFalse(move.Performing,"the aria never ended");
            Assert.AreEqual(10,ExtraMove.AriaTotal,.001f,"Lattice Aria isn't meant to be ten");
            foreach(var (hits,what) in new[]{(first,"the body she was aiming at"),(second,"a second body on the screen")})
            {
                Assert.AreEqual(ExtraMove.Beats+1,hits.count,$"eight volleys and a chord didn't each reach {what}");
                Assert.AreEqual(Output(sela,true)*ExtraMove.AriaTotal,hits.plain,Output(sela,true)*.25f,$"the aria doesn't add up to ten of her Output on {what}");
            }
            Assert.AreEqual(0,third.count,"the aria reached a body 80 m away, far off the screen");
            Assert.AreEqual(.3f,ExtraMove.AriaHeal,.001f,"the aria's mend isn't 30%");
            Assert.AreEqual(hers+sela.Health.maximum*ExtraMove.AriaHeal,sela.Health.integrity,1,"she wasn't mended 30% of her Integrity, once");
            Assert.AreEqual(his+taren.Health.maximum*ExtraMove.AriaHeal,taren.Health.integrity,1,"her partner wasn't mended 30% of his Integrity, once");
        }
        [UnityTest] public IEnumerator LatticeAriaDoesNotReviveAHeroWhoIsDown()
        {
            yield return Zone(false);Unlock();var sela=Lead("Sela");var taren=Taren;Place(sela,new Vector3(200,0,0));Place(taren,new Vector3(206,0,0));
            var body=Dummy(false,new Vector3(200,0,6));yield return Frames(3);sela.target=body.Health;
            taren.Health.Receive(new DamagePacket{amount=taren.Health.maximum*10});yield return new WaitForSecondsRealtime(.3f);
            Assert.IsFalse(taren.Health.Alive,"fixture: her partner went down");
            Hurt(sela,.4f);float hers=sela.Health.integrity;
            yield return Fire(sela);
            Assert.Greater(sela.Health.integrity,hers,"fixture: the aria mended her");
            Assert.IsFalse(taren.Health.Alive,"Lattice Aria revived a hero who was down");Assert.AreEqual(0,taren.Health.integrity,.0001f,"Lattice Aria gave a downed hero Integrity");
        }

        // ------------------------------------------------------------------ the partner, the press, the lesson
        [UnityTest] public IEnumerator ThePartnerNeverFiresAnExtraAndItsGaugeWaitsForThePlayer()
        {
            yield return Zone(false);Unlock();var taren=Lead("Taren");var sela=Sela;Place(taren,new Vector3(200,0,0));Place(sela,new Vector3(203,0,-2));
            var body=Dummy(false,new Vector3(200,0,6));yield return Frames(3);taren.target=body.Health;
            Member("Sela").extra=ExtraGauge.Full;var hers=sela.GetComponent<ExtraMove>();
            Assert.IsFalse(hers.TryFire(),"asked outright, a hero the player isn't playing fired an EXTRA");
            sela.GetComponent<PartnerBrain>().enabled=true;
            // Long enough for a partner to have spent Charge on a skill (three seconds, then when one is in reach).
            for(float end=Time.unscaledTime+7;Time.unscaledTime<end;){sela.charge=100;taren.motor.Move(Vector2.zero,false,false);yield return null;}
            Assert.AreEqual(0,hers.Released,"the partner fired an EXTRA on its own");
            Assert.IsFalse(log.Any(l=>l.StartsWith("EXTRA_BEGIN Sela")),"the partner fired an EXTRA on its own");
            Assert.AreEqual(ExtraGauge.Full,ExtraGauge.Of("Sela"),.0001f,"the partner's full gauge didn't wait for the player");
            // Swapped to, it's the player's to fire.
            var lead=Lead("Sela");yield return Frames(2);lead.target=body.Health;
            Assert.IsTrue(hers.TryFire(),"the gauge the partner filled can't be fired once the player swaps to her");
            for(float end=Time.unscaledTime+5;hers.Performing&&Time.unscaledTime<end;){lead.motor.Move(Vector2.zero,false,false);yield return null;}
        }
        IEnumerator Press(bool rb,bool lt)
        {
            var state=new GamepadState();if(rb)state=state.WithButton(GamepadButton.RightShoulder);state.leftTrigger=lt?1:0;
            InputSystem.QueueStateEvent(pad,state);yield return null;yield return null;
        }
        [UnityTest] public IEnumerator RbWithLtFiresItAndLtAloneIsStillTheQuickItem()
        {
            pad=InputSystem.AddDevice<Gamepad>("ExtraPad");yield return null;
            yield return Zone(false);Unlock();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            var body=Dummy(false,new Vector3(200,0,5));yield return Frames(3);taren.target=body.Health;
            taren.GetComponent<PlayerBrain>().AutoPilot=false;
            Hurt(taren,.5f);State.quickItem="RepairGel";State.consumables["RepairGel"]=3;
            Member("Taren").extra=ExtraGauge.Full;var move=taren.GetComponent<ExtraMove>();
            yield return Press(false,false);
            // LT alone: the gel, and no EXTRA.
            yield return Press(false,true);yield return Press(false,false);
            Assert.AreEqual(2,State.consumables["RepairGel"],"LT alone no longer uses the quick item");
            Assert.AreEqual(0,move.Released,"LT alone fired the EXTRA");Assert.AreEqual(ExtraGauge.Full,ExtraGauge.Of("Taren"),.0001f,"LT alone spent the gauge");
            // RB held, then LT with it.
            yield return Press(true,false);
            Assert.AreEqual(0,move.Released,"RB alone fired the EXTRA");
            yield return Press(true,true);
            Assert.AreEqual(1,move.Released,"RB with LT, on a full gauge, didn't fire the EXTRA");
            Assert.AreEqual(2,State.consumables["RepairGel"],"RB with LT used the quick item as well");
            yield return Press(false,false);
            for(float end=Time.unscaledTime+5;move.Performing&&Time.unscaledTime<end;)yield return null;
            Assert.AreEqual(1,move.Released,"one press fired it more than once");
        }
        [UnityTest] public IEnumerator TheFirstFullGaugeIsTaughtOnceByThePartnerAndACard()
        {
            yield return Zone(false);Unlock();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(203,0,-2));
            var spoken=new List<(string who,string line)>();void Spoke(string who,string line){spoken.Add((who,line));}
            var cards=new List<(string heading,string body)>();void Posted(string heading,string body){cards.Add((heading,body));}
            BarkService.Spoken+=Spoke;Notices.Posted+=Posted;
            try
            {
                var flags=GameServices.Current.Flags;
                Assert.IsFalse(flags.GetBool(ExtraGauge.TaughtFlag),"fixture: nothing taught yet");
                // Past whatever was said on the way in, and short of full: nothing to teach.
                Member("Taren").extra=99;yield return new WaitForSecondsRealtime(4);
                Assert.IsFalse(flags.GetBool(ExtraGauge.TaughtFlag),"the lesson came before the gauge was full");
                Member("Taren").extra=ExtraGauge.Full;
                for(float end=Time.unscaledTime+5;!flags.GetBool(ExtraGauge.TaughtFlag)&&Time.unscaledTime<end;)yield return null;
                Assert.IsTrue(flags.GetBool(ExtraGauge.TaughtFlag),"a full gauge, in a fight, and nobody said what it's for");
                string lesson=BarkService.Line("Sela",ExtraMove.Lesson);
                Assert.IsNotNull(lesson,"Sela has no line for the lesson: the barks weren't copied (SkillsLate.Build)");
                Assert.IsTrue(spoken.Any(s=>s.who=="Sela"&&s.line==lesson),"the partner didn't say the lesson: "+string.Join(" | ",spoken.Select(s=>s.who+": "+s.line)));
                Assert.IsNotNull(BarkService.Line("Taren",ExtraMove.Lesson),"Taren has no line for the lesson, for when Sela leads");
                var card=cards.FirstOrDefault(c=>c.heading=="EXTRA READY");
                Assert.IsNotNull(card.body,"no card says what to press");
                StringAssert.Contains(taren.GetComponent<ExtraMove>().Definition.displayName,card.body,"the card doesn't name the move");
                StringAssert.Contains(ExtraGauge.Binding(false),card.body,"the card doesn't say what to press");
                StringAssert.DoesNotContain("_Extra",card.body,"the card shows the move's id");
                // Once a save.
                int said=spoken.Count(s=>s.line==lesson),carded=cards.Count(c=>c.heading=="EXTRA READY");
                Member("Taren").extra=0;yield return Frames(3);Member("Taren").extra=ExtraGauge.Full;yield return new WaitForSecondsRealtime(4.2f);
                Assert.AreEqual(said,spoken.Count(s=>s.line==lesson),"the lesson was said a second time");Assert.AreEqual(carded,cards.Count(c=>c.heading=="EXTRA READY"),"the card came up a second time");
            }
            finally{BarkService.Spoken-=Spoke;Notices.Posted-=Posted;}
        }

        // ------------------------------------------------------------------ bosses
        [UnityTest] public IEnumerator AnExtraTakesABossAcrossOnePhaseLineAndNoFurther()
        {
            yield return Zone(false);Unlock();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            var boss=Enemy("Burrower",new Vector3(200,0,5));yield return Frames(3);
            var controller=boss.GetComponent<BossController>();var health=boss.Health;var lines=controller.Thresholds;
            Assert.AreEqual(2,lines.Length,"fixture: the Burrower has two phase lines");Assert.Greater(lines[0],lines[1],"fixture: the lines come down in order");
            // A boss small enough that twelve of his Output would end the whole fight from full.
            health.maximum=health.integrity=Output(taren,false)*8;health.breakThreshold=1e9f;health.weakness=health.resistance=DamageType.Pulse;taren.target=health;
            Assert.AreEqual(lines[1]*health.maximum+1,controller.ExtraFloor,.01f,"from full, the floor isn't just over the second line");
            yield return Fire(taren);
            Assert.IsTrue(health.Alive,"one EXTRA ended a boss fight from full Integrity");
            Assert.AreEqual(lines[1]*health.maximum+1,health.integrity,1.5f,"one EXTRA carried a boss across both of its phase lines, or stopped short of the second");
            Assert.Less(health.integrity/health.maximum,lines[0],"fixture: it did cross the first line");
            // On the last line: the fight can't be ended by the move that crosses it.
            Assert.AreEqual(1,controller.ExtraFloor,.0001f,"with one line left the floor isn't 1 Integrity");
            taren.target=health;yield return Fire(taren);
            Assert.IsTrue(health.Alive,"an EXTRA crossed the last phase line and ended the fight in the same move");
            Assert.AreEqual(1,health.integrity,.6f,"past the last line the move didn't spend itself down to the floor");
            // In the last phase, with no shift owed, there's nothing left to skip.
            yield return Frames(3);
            Assert.AreEqual(0,controller.ExtraFloor,.0001f,"in its last phase a boss still has a floor");
        }
        [UnityTest] public IEnumerator AnExtraLandsNothingOnABossInTheMiddleOfItsPhaseShift()
        {
            yield return Zone(false);Unlock();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            var boss=Enemy("Burrower",new Vector3(200,0,5));yield return Frames(3);
            var controller=boss.GetComponent<BossController>();var health=boss.Health;health.maximum=health.integrity=1000000;taren.target=health;
            float meter=health.BreakMeter;
            Assert.IsTrue(controller.Perform("PhaseShift"),"fixture: the shift began");yield return null;
            Assert.IsTrue(controller.Shifting,"fixture: the boss is shifting");
            var move=taren.GetComponent<ExtraMove>();Member("Taren").extra=ExtraGauge.Full;
            float fired=Time.unscaledTime;int early=0;
            health.Damaged+=(_,packet,amount)=>{if(amount>0&&Time.unscaledTime-fired<1.3f)early++;};
            Assert.IsTrue(move.TryFire(),"fixture: the EXTRA");
            // The cut-in and all eight blows fall inside the shift; the shift runs on the world's clock, which the cut-in slows.
            bool shifting=true;
            for(float end=fired+1.25f;Time.unscaledTime<end;){taren.motor.Move(Vector2.zero,false,false);shifting&=controller.Shifting;yield return null;}
            Assert.IsTrue(shifting,"fixture: the boss was shifting for the whole of the flurry");
            Assert.AreEqual(0,early,"an EXTRA's blows landed on a boss in the middle of its phase shift");
            Assert.AreEqual(1000000,health.integrity,.01f,"the boss lost Integrity through its shift");
            Assert.AreEqual(meter,health.BreakMeter,.01f,"the EXTRA's Break went on the boss through its shift");
            for(float end=Time.unscaledTime+6;(move.Performing||controller.Shifting)&&Time.unscaledTime<end;){taren.motor.Move(Vector2.zero,false,false);yield return null;}
            Assert.IsFalse(controller.Shifting,"the shift never finished: the EXTRA broke the boss's routine");
            Assert.IsTrue(log.Any(l=>l.StartsWith("BOSS_SHIFT Burrower")),"the shift didn't bring in what its phase adds");
            Assert.IsTrue(health.Alive);
            // And it can be hurt again once it has shifted.
            Assert.Greater(health.Receive(new DamagePacket{source=taren.Health,amount=100,type=DamageType.Beam}),0,"fixture: after the shift the boss can be hurt");
        }
    }
}
