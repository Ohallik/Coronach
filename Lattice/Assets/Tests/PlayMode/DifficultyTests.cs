using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The difficulty setting: Gentle, Standard, Demanding. Standard is the game as it was, to the number. The other two change what an enemy's or a boss's hit takes off
    /// a hero (60% and 130%) and the integrity they are made with (80% and 115%), and nothing else: not a hero's own blows, not what the place itself does, not XP, scrip or loot.
    /// It is saved with the game and nowhere else, so every other test class starts at Standard without doing anything.</summary>
    public sealed class DifficultyTests
    {
        const DifficultySetting Standard=DifficultySetting.Standard,Gentle=DifficultySetting.Gentle,Demanding=DifficultySetting.Demanding;
        /// <summary>The stated factors, written out here and not read from the code under test.</summary>
        static readonly (DifficultySetting setting,float damage,float integrity)[] Three={(Standard,1,1),(Gentle,.6f,.8f),(Demanding,1.3f,1.15f)};
        static readonly string[] Slots={"autosave","slot1","slot2","slot3"};
        readonly List<string> folders=new();
        /// <summary>A test that went through the game's own slots (the title, a Retry) puts them back as the save tests do.</summary>
        bool usedTheSlots;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            DifficultyUi.AsksAtNewGame=false;
            if(GameServices.Current!=null){GameServices.Current.Input.Blocked=false;State.difficulty=Standard;}
            // The batch runner's save shield holds the player's real saves elsewhere for the whole run: only then is anything here deleted.
            if(usedTheSlots&&Application.isBatchMode&&GameServices.Current!=null)
            {
                foreach(string slot in Slots)foreach(string suffix in new[]{"",".bak",".tmp"}){string path=Saves.PathForSlot(slot)+suffix;if(File.Exists(path))File.Delete(path);}
                Saves.ResumeFrom("autosave");
            }
            usedTheSlots=false;
            foreach(string folder in folders)if(Directory.Exists(folder))Directory.Delete(folder,true);
            folders.Clear();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static SaveSystem Saves=>GameServices.Current.Saves;
        static CombatActor Other(CombatActor hero)=>PartyController.Current.members.First(m=>m!=hero);
        static void Quiet(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            Quiet();
            yield return new WaitForSecondsRealtime(.7f);
        }
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}
            else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            Physics.SyncTransforms();
        }
        static void PlaceBody(EnemyBrain enemy,Vector3 position)
        {var body=enemy.GetComponent<CharacterController>();body.enabled=false;enemy.transform.position=position;body.enabled=true;Physics.SyncTransforms();}
        /// <summary>Open floor far from the arena's walls.</summary>
        static void Arena()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Difficulty fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static EnemyBrain Enemy(string id,Vector3 at,bool passive=true){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=passive;return enemy;}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>A hero no blow here can fell, with nothing left over from the last one.</summary>
        static void Fresh(CombatActor actor){actor.Health.maximum=actor.Health.integrity=5000;actor.Health.InvulnerableUntil=0;}
        static DamageType Neutral(Health body)=>System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=body.weakness&&t!=body.resistance);
        static Button Find(string name)=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name==name);
        static string Words(Component row)=>row!=null&&row.GetComponentInChildren<TMP_Text>()!=null?row.GetComponentInChildren<TMP_Text>().text:"";
        static IEnumerator ToTitle()
        {
            // A boot that was asked for no zone brings the title up by itself; asking again would load a second one beside it. From a zone it is a load away.
            if(!SceneManager.GetSceneByName("Title").IsValid())SceneFlow.Current.LoadZone("Title");
            float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            yield return null;yield return null;
        }
        string TempFolder(){string folder=Path.Combine(Application.temporaryCachePath,"difficulty-"+System.Guid.NewGuid().ToString("N"));folders.Add(folder);return folder;}
        /// <summary>Words that grade the player or the choice. The three are told apart by what they do to an enemy's numbers.</summary>
        static readonly string[] Judgements={"easy","easier","hard","harder","normal","casual","beginner","expert","recommended","story mode","for players"};
        static void NoJudgement(string words,string where){foreach(string word in Judgements)StringAssert.DoesNotContain(word,words.ToLowerInvariant(),where+" grades the choice");}

        // ------------------------------------------------------------------ Standard is the game as it was
        [UnityTest] public IEnumerator ANewGameAndEveryFixtureIsStandardAndStandardTouchesNoNumber()
        {
            yield return null;
            Assert.AreEqual(Standard,new GameState().difficulty,"a new game's state isn't Standard");
            Assert.AreEqual(Standard,State.difficulty,"a fixture's party starts at something other than Standard: every test in the suite would be measuring another game");
            Assert.AreEqual(0,(int)Standard,"Standard is no longer what a save with no value reads as");
            Assert.AreEqual(Standard,Difficulty.Current);
            // Not "multiplied by one": handed back as it came, whatever the number.
            foreach(float number in new[]{0f,7f,18f,.1f,1f/3,210f,35000f,54321.987f,float.MaxValue})
            {
                Assert.AreEqual(number,Difficulty.EnemyDamage(number),0f,"Standard changed an enemy's "+number+" damage");
                Assert.AreEqual(number,Difficulty.EnemyIntegrity(number),0f,"Standard changed an enemy's "+number+" integrity");
            }
            foreach(var (setting,damage,integrity) in Three)
            {
                Assert.AreEqual(damage,Difficulty.DamageOf(setting),0f,setting+"'s damage");Assert.AreEqual(integrity,Difficulty.IntegrityOf(setting),0f,setting+"'s integrity");
                Assert.IsTrue(Difficulty.TryNamed(Difficulty.Name(setting),out var named)&&named==setting,setting+" isn't found by its own name");
                NoJudgement(Difficulty.Name(setting)+" "+Difficulty.Says(setting),setting.ToString());
            }
            // A value from a later build's save plays as Standard and is never guessed at.
            Assert.AreEqual(1f,Difficulty.DamageOf((DifficultySetting)7),0f);Assert.AreEqual(1f,Difficulty.IntegrityOf((DifficultySetting)7),0f);Assert.AreEqual("Standard",Difficulty.Name((DifficultySetting)7));
            Assert.IsTrue(Difficulty.TryNamed(null,out var unnamed)&&unnamed==Standard,"a route that names no difficulty isn't Standard");
            Assert.IsTrue(Difficulty.TryNamed("",out unnamed)&&unnamed==Standard);
            foreach(string wrong in new[]{"gentle","Easy","Hard","1","Standard "})Assert.IsFalse(Difficulty.TryNamed(wrong,out _),"'"+wrong+"' is taken for a difficulty");
        }

        // ------------------------------------------------------------------ damage
        /// <summary>The hit itself, as every enemy source delivers it: a packet that names an enemy, received by a hero. And what the setting must leave alone.</summary>
        [UnityTest] public IEnumerator AnEnemysHitTakesTheSettingsShareAndNothingElseDoes()
        {
            yield return Zone("Arena_Ground");Arena();var lead=PartyController.Current.Active;Place(lead,new Vector3(200,0,-4));Place(Other(lead),new Vector3(260,0,0));
            var hound=Enemy("Ridgehound",new Vector3(200,0,3));var captain=Enemy("HessaKorl",new Vector3(208,.1f,3));
            var post=Enemy("SentinelHusk",new Vector3(192,0,3));post.Health.maximum=post.Health.integrity=1000000;post.Health.breakThreshold=1e9f;
            yield return Frames(3);
            float Lands(Health from){Fresh(lead);return lead.Health.Receive(new DamagePacket{source=from,amount=40,type=DamageType.Kinetic});}
            float Strikes(){post.Health.integrity=post.Health.maximum;return post.Health.Receive(new DamagePacket{source=lead.Health,amount=50,type=Neutral(post.Health)});}
            // What 40 costs this hero when nobody's setting is in it: a blow no enemy dealt, which the setting never reads.
            float plain=Lands(null),own=Strikes();
            Assert.Greater(plain,0,"fixture: a blow lands on a hero standing still");Assert.Greater(own,0,"fixture: the hero's blow lands");
            foreach(var (setting,damage,_) in Three)
            {
                State.difficulty=setting;
                foreach(var from in new[]{hound.Health,captain.Health})
                {
                    float landed=Lands(from);
                    if(setting==Standard)Assert.AreEqual(plain,landed,0f,$"on Standard a {from.id}'s 40 lands as anything but the number it always was");
                    else Assert.AreEqual(plain*damage,landed,plain*.001f,$"on {setting} a {from.id}'s hit isn't {damage*100:0}% of Standard's");
                }
                Assert.AreEqual(plain,Lands(null),0f,$"on {setting} damage no enemy dealt (the cave's pulse, rising water, a wall flown into) changed: the setting is about enemies");
                Assert.AreEqual(own,Strikes(),0f,$"on {setting} a hero's own blow changed: the setting takes nothing from the party and gives it nothing");
            }
        }
        /// <summary>The whole way, through an ordinary enemy's own attack: the Husk's bite on a hero standing in its paint, four times over.</summary>
        [UnityTest] public IEnumerator AnOrdinaryEnemysBiteLandsForTheSettingsShare()
        {
            yield return Zone("Arena_Ground");Arena();var hero=PartyController.Current.Active;Place(Other(hero),new Vector3(260,0,0));
            var body=Enemy("SentinelHusk",new Vector3(200,0,0));Place(hero,new Vector3(200,0,1.8f));Fresh(hero);
            float bitten=-1,built=0;var kind=DamageType.Kinetic;
            System.Action<Health,DamagePacket,float> hurt=(_,packet,amount)=>{if(packet.source!=body.Health||amount<=0)return;bitten=amount;built=packet.amount;kind=packet.type;};
            hero.Health.Damaged+=hurt;
            yield return new WaitForSecondsRealtime(.7f);body.Passive=false;
            float first=0;
            foreach(var setting in new[]{Standard,Gentle,Demanding,Standard})
            {
                // Set between bites, with the next one not yet told: it is the next hit that has to follow.
                State.difficulty=setting;bitten=-1;
                Place(hero,body.transform.position+Vector3.forward*1.8f);
                for(float end=Time.unscaledTime+8;bitten<0&&Time.unscaledTime<end;)yield return null;
                Assert.Greater(bitten,0,$"fixture: the Husk never bit a hero standing in front of it ({setting})");
                if(first<=0)
                {
                    first=bitten;Fresh(hero);
                    float plain=hero.Health.Receive(new DamagePacket{amount=built,type=kind});
                    Assert.AreEqual(plain,first,plain*.0001f,"on Standard the Husk's bite lands as anything but its attack's own number");
                }
                else Assert.AreEqual(first*Three.First(t=>t.setting==setting).damage,bitten,first*.001f,$"on {setting} the Husk's next bite isn't {Three.First(t=>t.setting==setting).damage*100:0}% of Standard's");
                Fresh(hero);
            }
            hero.Health.Damaged-=hurt;body.Passive=true;Object.Destroy(body.gameObject);
        }
        /// <summary>A boss's special, through its own move: Hessa's sweeping cut, one marked blow on a hero standing inside its ring. Her phase scaling is hers and stays in the packet;
        /// the setting's share is taken as the blow lands, so the frame-by-frame rewrite of her damage scale can't lose it.</summary>
        [UnityTest] public IEnumerator ABossesSpecialLandsForTheSettingsShare()
        {
            yield return Zone("Arena_Ground");Arena();var lead=PartyController.Current.Active;Place(Other(lead),new Vector3(260,.1f,0));
            var at=new Vector3(200,.1f,0);var enemy=Enemy("HessaKorl",at);yield return Frames(3);var boss=enemy.GetComponent<BossController>();
            float struck=-1,built=0;var kind=DamageType.Kinetic;
            System.Action<Health,DamagePacket,float> witness=(_,packet,amount)=>{if(packet.source!=boss.Health||amount<=0||struck>=0)return;struck=amount;built=packet.amount;kind=packet.type;};
            lead.Health.Damaged+=witness;
            float first=0;
            foreach(var setting in new[]{Standard,Gentle,Demanding,Standard})
            {
                for(float end=Time.unscaledTime+7;(boss.Busy||boss.Health.Broken||Time.timeScale<1)&&Time.unscaledTime<end;)yield return null;
                State.difficulty=setting;
                PlaceBody(enemy,at);enemy.transform.rotation=Quaternion.identity;Place(lead,at+new Vector3(0,0,3));Fresh(lead);yield return Frames(2);
                struck=-1;Assert.IsTrue(boss.Perform("SweepingCut"),"fixture: the cut would not start");
                for(float end=Time.unscaledTime+6;(boss.Busy||struck<0)&&Time.unscaledTime<end;){lead.motor.Move(Vector2.zero,false,false);yield return null;}
                Assert.Greater(struck,0,$"fixture: the cut never landed on a hero standing inside its ring ({setting})");
                if(first<=0)
                {
                    first=struck;Fresh(lead);
                    float plain=lead.Health.Receive(new DamagePacket{amount=built,type=kind});
                    Assert.AreEqual(plain,first,plain*.0001f,"on Standard her cut lands as anything but the move's own number");
                }
                else Assert.AreEqual(first*Three.First(t=>t.setting==setting).damage,struck,first*.001f,$"on {setting} her cut isn't {Three.First(t=>t.setting==setting).damage*100:0}% of Standard's");
            }
            lead.Health.Damaged-=witness;
        }

        // ------------------------------------------------------------------ integrity
        [UnityTest] public IEnumerator EnemiesAndBossesAreMadeWithTheSettingsShareOfTheirIntegrity()
        {
            yield return Zone("Arena_Ground");Arena();
            foreach(string id in new[]{"Ridgehound","SentinelHusk","Scrapmite","HessaKorl","Burrower"})
            {
                var def=GameCatalog.Find<EnemyDef>(id);Assert.IsNotNull(def,"fixture: "+id);
                foreach(var (setting,_,integrity) in Three)
                {
                    State.difficulty=setting;var body=Enemy(id,new Vector3(200,.1f,0));
                    if(setting==Standard)Assert.AreEqual(def.integrity,body.Health.maximum,0f,$"on Standard a {id} is made with anything but its definition's integrity");
                    else Assert.AreEqual(def.integrity*integrity,body.Health.maximum,def.integrity*.0001f,$"on {setting} a {id} isn't made with {integrity*100:0}% of its integrity");
                    Assert.AreEqual(body.Health.maximum,body.Health.integrity,0f,$"a {id} is made wounded, or over its own bar ({setting})");
                    // Nothing else about the body: how much it takes to stagger it, what it's weak to, what it pays.
                    Assert.AreEqual(def.breakThreshold,body.Health.breakThreshold,0f,$"{setting} changed what it takes to break a {id}");
                    Assert.AreEqual(def.weakness,body.Health.weakness);Assert.AreEqual(def.resistance,body.Health.resistance);
                    Assert.AreSame(def,body.definition,$"{setting} gave a {id} another definition: its XP, loot and attacks are read from it");
                    Object.Destroy(body.gameObject);yield return Frames(2);
                }
            }
        }
        /// <summary>The Cantor's collar shares its integrity out itself, head and three links: the same shares of the setting's whole.</summary>
        [UnityTest] public IEnumerator TheCantorsHeadAndLinksShareOutTheSettingsWhole()
        {
            yield return Zone("Arena_Flight");var def=GameCatalog.Find<EnemyDef>("Cantor");Assert.IsNotNull(def,"fixture: the Cantor");
            foreach(var (setting,_,integrity) in Three)
            {
                State.difficulty=setting;var body=Enemy("Cantor",new Vector3(200,1,0));yield return Frames(3);
                var collar=body.GetComponent<CantorCollar>();Assert.IsNotNull(collar,"fixture: its collar");
                float whole=def.integrity*integrity,slack=def.integrity*.0001f,sum=body.Health.maximum;
                Assert.AreEqual(whole*CantorCollar.HeadShare,body.Health.maximum,slack,$"on {setting} the Cantor's head isn't its share of {integrity*100:0}%");
                Assert.AreEqual(3,collar.Links.Count,"fixture: three links");
                foreach(var link in collar.Links)
                {
                    Assert.AreEqual(whole*CantorCollar.LinkShare,link.maximum,slack,$"on {setting} a collar link isn't its share of {integrity*100:0}%");
                    Assert.AreEqual(link.maximum,link.integrity,0f,"a link is made wounded");sum+=link.maximum;
                }
                Assert.AreEqual(whole,sum,slack*4,$"on {setting} the Cantor's bar doesn't add up to {integrity*100:0}% of its integrity");
                Object.Destroy(body.gameObject);yield return Frames(3);
            }
        }

        // ------------------------------------------------------------------ changed in the middle of a fight
        [UnityTest] public IEnumerator ChangedMidFightTheNextHitFollowsAndNoLivingBarMoves()
        {
            yield return Zone("Arena_Ground");Arena();var lead=PartyController.Current.Active;Place(lead,new Vector3(200,0,-4));Place(Other(lead),new Vector3(260,0,0));
            var captain=Enemy("HessaKorl",new Vector3(200,.1f,4));var hound=Enemy("Ridgehound",new Vector3(206,0,4));yield return Frames(3);
            // A fight under way: both wounded, so a bar redrawn against another maximum would show.
            captain.Health.Receive(new DamagePacket{amount=captain.Health.maximum*.2f,type=Neutral(captain.Health)});hound.Health.Receive(new DamagePacket{amount=hound.Health.maximum*.4f,type=Neutral(hound.Health)});
            Assert.Less(captain.Health.integrity,captain.Health.maximum,"fixture: the captain is wounded");Assert.Less(hound.Health.integrity,hound.Health.maximum,"fixture: the hound is wounded");
            var bars=new[]{captain.Health,hound.Health}.Select(h=>(body:h,left:h.integrity,whole:h.maximum)).ToArray();
            float Lands(Health from){Fresh(lead);return lead.Health.Receive(new DamagePacket{source=from,amount=30,type=DamageType.Kinetic});}
            float standard=Lands(captain.Health);Assert.Greater(standard,0,"fixture: her blow lands");
            var houndDef=GameCatalog.Find<EnemyDef>("Ridgehound");
            foreach(var (setting,damage,integrity) in new[]{Three[1],Three[2],Three[0]})
            {
                State.difficulty=setting;
                Assert.AreEqual(standard*damage,Lands(captain.Health),standard*.001f,$"changed to {setting} mid-fight, her very next blow doesn't follow");
                Assert.AreEqual(standard*damage,Lands(hound.Health),standard*.001f,$"changed to {setting} mid-fight, the hound's very next bite doesn't follow");
                foreach(var (body,left,whole) in bars)
                {
                    Assert.AreEqual(whole,body.maximum,0f,$"changed to {setting} mid-fight, a living {body.id}'s bar was redrawn against another maximum");
                    Assert.AreEqual(left,body.integrity,0f,$"changed to {setting} mid-fight, a living {body.id} gained or lost integrity nobody dealt");
                }
                // Whoever is made after the change takes it.
                var late=Enemy("Ridgehound",new Vector3(214,0,4));
                Assert.AreEqual(houndDef.integrity*integrity,late.Health.maximum,houndDef.integrity*.0001f,$"a hound made after the change to {setting} was made at the old setting");
                Object.Destroy(late.gameObject);yield return Frames(2);
            }
            // And nowhere on the HUD does it say which: no word and no mark of the setting stands over the fight.
            State.difficulty=Gentle;yield return Frames(3);
            foreach(var text in Object.FindFirstObjectByType<GameHud>().GetComponentsInChildren<TMP_Text>(true))
                foreach(string word in new[]{"gentle","demanding","difficulty"})StringAssert.DoesNotContain(word,text.text.ToLowerInvariant(),"the HUD's "+text.name+" names the difficulty");
            foreach(var drawn in Object.FindFirstObjectByType<GameHud>().GetComponentsInChildren<Graphic>(true))
                StringAssert.DoesNotContain("difficulty",drawn.name.ToLowerInvariant(),"the HUD draws something for the difficulty");
        }

        /// <summary>The usual way to it: beaten at a boss, back at the checkpoint, the setting turned down in the pause menu, and in through the door again. The boss has been
        /// standing in its room since the zone loaded, made at the setting that beat the party.</summary>
        [UnityTest] public IEnumerator ABossOnShowBeforeItsFightWakesAtTheSettingChosenAtItsDoor()
        {
            yield return Zone("Arena_Ground");Arena();var lead=PartyController.Current.Active;Place(lead,new Vector3(200,0,-14));Place(Other(lead),new Vector3(260,0,0));
            var def=GameCatalog.Find<EnemyDef>("HessaKorl");
            var door=new GameObject("Difficulty fixture door").AddComponent<Spawner>();door.transform.position=new Vector3(200,.1f,0);door.definition=def;door.count=1;door.radius=0;
            door.Prepare(new Vector3(200,0,-8),6);yield return Frames(2);
            EnemyBrain[] Made()=>Object.FindObjectsByType<EnemyBrain>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(e=>e.definition==def).ToArray();
            Assert.AreEqual(1,Made().Length,"fixture: one boss on show");Assert.IsNotNull(Made()[0].GetComponent<DormantEnemy>(),"fixture: it waits for its fight");
            Assert.AreEqual(def.integrity,Made()[0].Health.maximum,0f,"fixture: made on Standard");
            // At the door, the way the pause menu's row does it.
            DifficultyUi.Next();yield return Frames(2);
            Assert.AreEqual(Demanding,State.difficulty,"fixture: one press from Standard");
            Assert.AreEqual(1,Made().Length,"the change left two bosses standing in the room, or none");
            var shown=Made()[0];
            Assert.AreEqual(def.integrity*1.15f,shown.Health.maximum,def.integrity*.0001f,"the boss on show kept the integrity of the setting the zone loaded with: turned down at the door, it isn't down for the fight behind it");
            Assert.AreEqual(shown.Health.maximum,shown.Health.integrity,0f);Assert.IsNotNull(shown.GetComponent<DormantEnemy>(),"made again, the boss on show woke before its fight");
            var woken=door.Spawn();Assert.AreEqual(1,woken.Length);Assert.AreSame(shown,woken[0],"the fight woke another body than the one on show");woken[0].Passive=true;
            yield return Frames(3);
            // Awake, its bar is up: from here a change moves nothing.
            float whole=woken[0].Health.maximum,left=woken[0].Health.integrity;
            DifficultyUi.Next();yield return Frames(2);
            Assert.AreEqual(Gentle,State.difficulty,"fixture: the next press");
            Assert.AreEqual(1,Made().Length,"a change mid-fight made the boss again");Assert.AreSame(woken[0],Made()[0],"a change mid-fight replaced the boss");
            Assert.AreEqual(whole,woken[0].Health.maximum,0f,"a change mid-fight redrew a living boss's bar");Assert.AreEqual(left,woken[0].Health.integrity,0f);
            Object.Destroy(door.gameObject);Object.Destroy(woken[0].gameObject);
        }

        // ------------------------------------------------------------------ what a kill pays
        /// <summary>A hound with a table that always drops, so nothing here is luck: the same XP, scrip, scrap and loot on all three.</summary>
        [UnityTest] public IEnumerator AKillPaysTheSameXpScripAndLootOnAllThree()
        {
            yield return Zone("Arena_Ground");Arena();var lead=PartyController.Current.Active;Place(lead,new Vector3(200,0,-6));Place(Other(lead),new Vector3(260,0,0));
            var table=ScriptableObject.CreateInstance<LootTable>();table.entries=new[]{new LootEntry{id="RidgeCrystal",count=2,chance=1},new LootEntry{id="LatticeFilament",count=1,chance=1}};
            var def=Object.Instantiate(GameCatalog.Find<EnemyDef>("Ridgehound"));def.lootTable=table;def.xp=37;def.scrip=11;def.scrap=3;
            var held=new Dictionary<string,int>(State.materials);int scrip=State.scrip;string standard=null;
            foreach(var (setting,_,_) in Three)
            {
                State.difficulty=setting;
                foreach(var member in State.party){member.level=1;member.xp=0;}
                State.scrip=scrip;State.materials.Clear();foreach(var material in held)State.materials[material.Key]=material.Value;
                var body=ActorFactory.Enemy(def,new Vector3(200,0,0));body.Passive=true;yield return Frames(3);
                body.Health.Receive(new DamagePacket{source=lead.Health,amount=1e7f,type=DamageType.Kinetic});
                Assert.IsFalse(body.Health.Alive,"fixture: the blow kills ("+setting+")");
                string paid=string.Join(" ",State.party.Select(m=>$"{m.id}:{m.level}/{m.xp}"))+$" scrip+{State.scrip-scrip} "+
                    string.Join(" ",State.materials.OrderBy(m=>m.Key,System.StringComparer.Ordinal).Select(m=>(id:m.Key,gained:m.Value-(held.TryGetValue(m.Key,out int had)?had:0))).Where(m=>m.gained!=0).Select(m=>$"{m.id}+{m.gained}"));
                if(setting==Standard)
                {
                    standard=paid;
                    Assert.AreEqual(11,State.scrip-scrip,"fixture: the kill pays its scrip");
                    Assert.IsTrue(State.party.All(m=>m.level>1||m.xp>0),"fixture: the kill pays its XP");
                    StringAssert.Contains("RidgeCrystal+",paid,"fixture: the table drops");
                }
                else Assert.AreEqual(standard,paid,$"on {setting} a kill pays something other than Standard's XP, scrip and loot");
                Object.Destroy(body.gameObject);yield return Frames(3);
            }
            Object.Destroy(def);Object.Destroy(table);
        }

        // ------------------------------------------------------------------ saved with the game
        [UnityTest] public IEnumerator TheSettingIsSavedWithTheGameAndASaveFromBeforeItReadsStandard()
        {
            yield return null;
            // A save system of its own, in a folder of its own: nothing but Standard is ever written where another test might Continue from it.
            var saves=new SaveSystem(TempFolder());
            foreach(var setting in new[]{Gentle,Demanding,Standard})
            {
                State.difficulty=setting;saves.Save("slot1",State);
                var loaded=saves.Load("slot1");Assert.IsNotNull(loaded,"fixture: the save reads back");
                Assert.AreEqual(setting,loaded.difficulty,$"a game saved on {setting} loads as something else");
            }
            // An old save: the same file with the value taken out of it, as every save written before the setting is.
            State.difficulty=Demanding;State.flags["difficulty.marker"]=true;saves.Save("slot2",State);
            string path=saves.PathForSlot("slot2"),text=File.ReadAllText(path);
            string old=System.Text.RegularExpressions.Regex.Replace(text,",\\s*\"difficulty\"\\s*:\\s*-?\\d+","");
            Assert.AreNotEqual(text,old,"fixture: the save holds the setting under its own name, as a number");StringAssert.DoesNotContain("difficulty\"",old.Replace("difficulty.marker\"",""));
            File.WriteAllText(path,old);
            var before=saves.Load("slot2");Assert.IsNotNull(before,"a save from before the setting can't be read: every existing save would show as Damaged");
            Assert.AreEqual(Standard,before.difficulty,"a save from before the setting doesn't read as Standard, which is how it was played");
            Assert.IsTrue(before.flags.ContainsKey("difficulty.marker")&&before.party.Count==State.party.Count&&before.version==ChapterProgress.SaveVersion,"the rest of the old save didn't come through");
            // A number a later build wrote is kept as it is and plays as Standard.
            string future=System.Text.RegularExpressions.Regex.Replace(text,"(\"difficulty\"\\s*:\\s*)-?\\d+","${1}7");
            Assert.AreNotEqual(text,future,"fixture: the number could be rewritten");File.WriteAllText(path,future);
            var later=saves.Load("slot2");Assert.IsNotNull(later,"a save with a setting this build doesn't know can't be read");
            Assert.AreEqual(7,(int)later.difficulty,"fixture: the later build's number came through");
            Assert.AreEqual(1f,Difficulty.DamageOf(later.difficulty),0f,"a setting this build doesn't know isn't played as Standard");Assert.AreEqual(1f,Difficulty.IntegrityOf(later.difficulty),0f);
            State.flags.Remove("difficulty.marker");
        }
        /// <summary>The setting is the player's as it stands, not as the checkpoint was written: turned down mid-fight, a Retry must not turn it back up.</summary>
        [UnityTest] public IEnumerator ARetryKeepsTheSettingChosenSinceTheCheckpoint()
        {
            yield return Zone("Arena_Ground");usedTheSlots=true;
            var copier=new SaveSystem(TempFolder());copier.Save("slot3",State);var saved=copier.Load("slot3");
            saved.scrip=777;saved.zone="Arena_Ground";saved.spawn="Arrival";saved.difficulty=Standard;Saves.Save("autosave",saved);
            State.difficulty=Gentle;
            foreach(var hero in PartyController.Current.members)hero.Health.Receive(new DamagePacket{amount=100000,type=DamageType.Pulse});
            var panel=Object.FindFirstObjectByType<DefeatPanel>();
            float deadline=Time.unscaledTime+6;while(!panel.IsOpen&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(panel.IsOpen,"fixture: no defeat panel");
            Find("Retry").onClick.Invoke();
            deadline=Time.unscaledTime+15;
            yield return null;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            yield return null;yield return null;
            Assert.AreEqual(777,State.scrip,"fixture: the checkpoint came back");
            Assert.AreEqual(Gentle,State.difficulty,"a Retry put the difficulty back to what the checkpoint was saved at: turned down after a defeat, it was up again for the next try");
            Assert.AreEqual(Standard,Saves.Load("autosave").difficulty,"fixture: a yard never autosaves, so the slot still holds Standard");
        }

        // ------------------------------------------------------------------ where it is chosen
        [UnityTest] public IEnumerator TheSettingsRowStepsThroughTheThreeAndTheRowsRoundItKeepTheirPlaces()
        {
            yield return Zone("Arena_Ground");
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open(PauseMenu.SettingsPage);yield return null;
            Button With(string words)=>menu.GetComponentsInChildren<Button>().FirstOrDefault(b=>Words(b).StartsWith(words));
            float Height(Button row)=>((RectTransform)row.transform).anchoredPosition.y;
            var row=With("Difficulty");Assert.IsNotNull(row,"the pause menu's Settings has no difficulty row");
            Assert.AreEqual(DifficultyUi.RowName,row.name,"the row took a number: the rows under it are found and focused by theirs");
            StringAssert.Contains("Standard",Words(row),"the row doesn't say what it is set to");
            EventSystem.current.SetSelectedGameObject(row.gameObject);
            foreach(var setting in new[]{Demanding,Gentle,Standard})
            {
                With("Difficulty").onClick.Invoke();yield return null;
                Assert.AreEqual(setting,State.difficulty,"a press didn't step the setting on, or the last doesn't come round to the first");
                StringAssert.Contains(Difficulty.Name(setting),Words(With("Difficulty")),"the row doesn't say what it is set to");
                StringAssert.Contains(Difficulty.Says(setting),Words(With("Difficulty")),"the row doesn't say what the setting does");
                StringAssert.StartsWith("Difficulty",Words(EventSystem.current.currentSelectedGameObject.transform),"a press moved the cursor off the row it changed");
                NoJudgement(Words(With("Difficulty")),"the row");
            }
            // Beside Screen and impact, and nothing that was there has moved off its name.
            Assert.AreEqual("Row_5",With("Keyboard bindings").name);Assert.AreEqual("Row_6",With("Screen and impact").name);Assert.AreEqual("Row_7",With("Leave the game").name);
            Assert.Greater(Height(With("Screen and impact")),Height(With("Difficulty")),"the row isn't under Screen and impact");
            Assert.Greater(Height(With("Difficulty"))-Height(With("Leave the game")),50,"the row lies on Leave the game");
            Assert.Greater(Height(With("Leave the game"))-Height(menu.GetComponentsInChildren<Button>().First(b=>b.name=="Close")),50,"Leave the game was pushed onto the Back button");
            With("Leave the game").onClick.Invoke();yield return null;menu.StepBack();yield return null;
            StringAssert.StartsWith("Leave the game",Words(EventSystem.current.currentSelectedGameObject.transform),"backing out of the leave question lands on another row");
            With("Screen and impact").onClick.Invoke();yield return null;menu.StepBack();yield return null;
            StringAssert.StartsWith("Screen and impact",Words(EventSystem.current.currentSelectedGameObject.transform),"backing out of Screen and impact lands on another row");
            Assert.AreEqual(4,ImpactSettingsUi.Rows.Length,"the difficulty was put among the four impact rows, which the title draws too");
            menu.Close();yield return null;
        }
        /// <summary>The question is for a person starting the game in a player. Here it is turned on by hand, as a check may; left alone, New Game goes straight in.</summary>
        [UnityTest] public IEnumerator APersonStartingANewGameIsAskedOnceWithTheCursorOnStandard()
        {
            Assert.IsFalse(DifficultyUi.AsksAtNewGame,"New Game stops to ask in the editor: every test and route that presses through the title is now answering a question it never saw");
            usedTheSlots=true;yield return ToTitle();var title=Object.FindFirstObjectByType<TitleScreen>();Assert.IsNotNull(title,"fixture: the title");
            NoJudgement(DifficultyUi.Question+" "+DifficultyUi.Explains,"the question");
            DifficultyUi.AsksAtNewGame=true;
            IEnumerator PressNewGame()
            {
                Find("New Game").onClick.Invoke();yield return null;yield return null;
                // With an autosave on disk the older question comes first, and this one takes its place.
                if(Find("ConfirmNewGame")!=null){Find("ConfirmNewGame").onClick.Invoke();yield return null;yield return null;}
            }
            var game=State;
            yield return PressNewGame();
            Assert.IsTrue(title.NewGameQuestionOpen,"New Game didn't ask");Assert.IsFalse(SceneFlow.Current.Loading,"New Game started before it was answered");Assert.AreSame(game,State,"asking replaced the game");
            Assert.IsNull(Find("ConfirmNewGame"),"the question before it is still up underneath");Assert.IsNull(Find("New Game"),"the main menu shows through the question");
            foreach(var setting in DifficultyUi.Order)
            {
                var row=Find("Difficulty_"+Difficulty.Name(setting));Assert.IsNotNull(row,"no row for "+setting);
                StringAssert.StartsWith(Difficulty.Name(setting),Words(row));StringAssert.Contains(Difficulty.Says(setting),Words(row),"a row doesn't say what it does");NoJudgement(Words(row),"a row");
                Assert.IsNotNull(row.navigation.selectOnDown,"a pad can't reach past "+setting);
            }
            Assert.AreEqual("Difficulty_Standard",EventSystem.current.currentSelectedGameObject.name,"the cursor doesn't start on Standard: a press straight through plays another game than the one that was built");
            // Back, and nothing has happened.
            Find("DifficultyBack").onClick.Invoke();yield return null;yield return null;
            Assert.IsFalse(title.NewGameQuestionOpen);Assert.IsNotNull(Find("New Game"),"Back doesn't return to the menu");Assert.IsFalse(SceneFlow.Current.Loading);Assert.AreSame(game,State,"backing out replaced the game");
            // Answered: the new game starts, at the answer.
            yield return PressNewGame();
            Find("Difficulty_Gentle").onClick.Invoke();
            // Read and put straight back in the same frame, before the arrival's autosave: nothing but Standard is ever written where another test might Continue from it.
            var chosen=State.difficulty;bool started=SceneFlow.Current.Loading,fresh=!ReferenceEquals(game,State);State.difficulty=Standard;
            Assert.IsTrue(started,"the answer didn't start the game");Assert.IsTrue(fresh,"the answer didn't start a new game");
            Assert.AreEqual(Gentle,chosen,"the new game didn't start at the answer given");
            float deadline=Time.unscaledTime+30;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"the new game never finished loading");Assert.IsNotNull(PartyController.Current,"the new game has no party");
        }

        // ------------------------------------------------------------------ the replay driver
        /// <summary>Every existing route names no difficulty and continues the save the route before it left. Whatever that save says, it plays at Standard; a route that names one
        /// plays at that, and holds it when Continue replaces the game under it.</summary>
        [UnityTest] public IEnumerator AReplayHoldsItsRoutesSettingAndStandardWhenTheRouteNamesNone()
        {
            yield return null;
            const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
            var route=typeof(ContinuousReview).GetField("route",Private);var frame=typeof(ContinuousReview).GetMethod("LateUpdate",Private);
            Assert.IsNotNull(route,"fixture: the driver's route");Assert.IsNotNull(frame,"fixture: the driver's frame");
            // Never switched on: it is asked for one frame at a time, and reads no route file.
            var fixture=new GameObject("Difficulty replay fixture");fixture.SetActive(false);var replay=fixture.AddComponent<ContinuousReview>();
            try
            {
                State.difficulty=Demanding;route.SetValue(replay,new QualityRoute{scene="Title",starterParty=false});frame.Invoke(replay,null);
                Assert.AreEqual(Standard,State.difficulty,"a route that names no difficulty played at what the save it continued was left at");
                route.SetValue(replay,JsonUtility.FromJson<QualityRoute>("{\"scene\":\"Title\",\"starterParty\":false,\"difficulty\":\"Gentle\"}"));frame.Invoke(replay,null);
                Assert.AreEqual(Gentle,State.difficulty,"a route's \"difficulty\" isn't read from its file, or isn't applied");
                GameServices.Current.State=new GameState();frame.Invoke(replay,null);
                Assert.AreEqual(Gentle,State.difficulty,"Continue replaced the game and the route's difficulty went with the old one");
                route.SetValue(replay,new QualityRoute{scene="Title",starterParty=false,difficulty="Standard"});frame.Invoke(replay,null);
                Assert.AreEqual(Standard,State.difficulty);
            }
            finally{Object.Destroy(fixture);}
        }
    }
}
