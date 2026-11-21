using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>S3: the six skills learned at Sync 16, 20 and 24. Each one's own job on foot and in flight, what its row promises in numbers, that no wall lets one through,
    /// that its caster is heard, and that a partner spends Charge on one as a player would: no mend at full Integrity, no channel in a marked danger.</summary>
    public sealed class SkillsLateTests
    {
        readonly List<string> log=new();
        void Logged(string text,string stack,LogType type){if(text.StartsWith("SKILL_KIND ")||text.StartsWith("SEVERANCE_REFUND"))log.Add(text);}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");log.Clear();Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static CombatActor Taren=>PartyController.Current.members.First(m=>m.character=="Taren");
        static CombatActor Sela=>PartyController.Current.members.First(m=>m.character=="Sela");
        static CombatActor Other(CombatActor hero)=>PartyController.Current.members.First(m=>m!=hero);
        static MemberState Member(string id)=>GameServices.Current.State.party.Find(m=>m.id==id);
        static void Quiet(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        static IEnumerator Zone(bool flight)
        {
            SceneFlow.Current.LoadZone(flight?"Arena_Flight":"Arena_Ground");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"the arena never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            Quiet();
            yield return new WaitForSecondsRealtime(.7f);
            // Open floor far from the arena's walls, and everyone at the level the last of these is learned.
            if(!flight){var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Late skill fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
            foreach(var member in GameServices.Current.State.party){member.level=24;member.skillUses?.Clear();}
            PartyController.Current.RefreshStats();
        }
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}
            else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            Physics.SyncTransforms();
        }
        static GameObject Wall(Vector3 center,Vector3 size)
        {var wall=new GameObject("Late skill fixture wall",typeof(BoxCollider));wall.transform.position=center;wall.GetComponent<BoxCollider>().size=size;Physics.SyncTransforms();return wall;}
        static EnemyBrain Enemy(string id,Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;return enemy;}
        /// <summary>A body that never dies or breaks, and that no damage type is strong or weak against: what lands on it is what was sent.</summary>
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
        static SkillDef Equip(CombatActor hero,SkillKind kind,int slot=0)
        {
            var skill=SkillBook.All(hero.character).FirstOrDefault(s=>s.kind==kind);
            Assert.IsNotNull(skill,$"{hero.character} has no {kind}: run Lattice.EditorTools.SkillsLate.Build");
            Assert.IsTrue(SkillBook.Equip(Member(hero.character),slot,skill),"fixture: "+kind+" can't be equipped at Sync 24");
            hero.charge=100;hero.cooldowns[slot]=0;return skill;
        }
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Drive(CombatActor actor,float seconds,Vector2 stick=default)
        {for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){actor.motor.Move(stick,false,false);yield return null;}}
        static IEnumerator UntilFree(CombatActor hero)
        {for(float end=Time.unscaledTime+4;!hero.CanAct&&Time.unscaledTime<end;){hero.motor.Move(Vector2.zero,false,false);yield return null;}}
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static void Hurt(CombatActor hero,float share){hero.Health.integrity=hero.Health.maximum*share;}
        /// <summary>What a skill's multiple is a multiple of: the hero's Output, by Resonance as every skill is. Mastery is 1 here.</summary>
        static float Output(CombatActor hero,bool ranged)=>(ranged?hero.rangedDamage:hero.damage)*(1+Mathf.Max(0,hero.resonance-10)*.025f);
        static TMP_Text HudText(string name)=>Object.FindFirstObjectByType<GameHud>().GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.name==name);
        sealed class Hits{public int count;public float plain;public readonly List<float> each=new();public void Clear(){count=0;plain=0;each.Clear();}}
        /// <summary>The hero's hits on a body, with a crit's bonus taken back out so a number can be held to.</summary>
        static Hits Watch(EnemyBrain body,CombatActor hero)
        {
            var hits=new Hits();
            body.Health.Damaged+=(_,packet,amount)=>
            {
                if(packet.source!=hero.Health||amount<=0)return;
                float plain=packet.isCrit?amount/(1.5f*(1+Mathf.Max(0,hero.fortune)*.01f+hero.mods.critDamage)):amount;
                hits.count++;hits.plain+=plain;hits.each.Add(plain);
            };
            return hits;
        }

        // ------------------------------------------------------------------ the table
        static readonly (string hero,int level,SkillKind kind)[] Table=
        {
            ("Taren",16,SkillKind.BulwarkRush),("Taren",20,SkillKind.CinderBrand),("Taren",24,SkillKind.Severance),
            ("Sela",16,SkillKind.MendLattice),("Sela",20,SkillKind.PrismRelay),("Sela",24,SkillKind.LongExposure),
        };
        [UnityTest] public IEnumerator ThreeMoreSkillsAHeroAreLearnedAtSync16And20And24()
        {
            yield return Zone(false);
            var cells=new List<int>();
            foreach(var (hero,level,kind) in Table)
            {
                var skill=SkillBook.All(hero).FirstOrDefault(s=>s.kind==kind);
                Assert.IsNotNull(skill,$"{hero} has no {kind}");
                Assert.AreEqual(level,skill.unlockLevel,skill.displayName+" is learned at the wrong level");
                Assert.IsFalse(string.IsNullOrWhiteSpace(skill.displayName)||string.IsNullOrWhiteSpace(skill.shortName)||string.IsNullOrWhiteSpace(skill.description),kind+" is missing a name, its HUD label or its row's words");
                Assert.AreNotEqual(skill.id,skill.displayName,kind+" shows its id for a name");
                Assert.IsNotNull(UiSkin.Skill(skill),skill.displayName+" has no icon");Assert.AreEqual("skill-icons-3",skill.iconSheet,skill.displayName+" isn't on the new sheet");cells.Add(skill.iconCell);
                Assert.Greater(skill.cooldown,0,skill.displayName+" has no cooldown");Assert.GreaterOrEqual(skill.chargeCost,SkillBook.MinimumCost,skill.displayName+" costs nothing");
                var member=Member(hero);member.level=level-1;
                Assert.IsFalse(SkillBook.Knows(member,skill),$"{skill.displayName} is known a level early");Assert.IsFalse(SkillBook.Equip(member,0,skill),$"{skill.displayName} can be equipped before it's learned");
                member.level=level;
                Assert.IsTrue(SkillBook.Equip(member,0,skill),$"{skill.displayName} can't be equipped at Sync {level}");
                CollectionAssert.Contains(SkillBook.LearnedBetween(hero,level-1,level),skill,$"the Sync {level} card wouldn't name {skill.displayName}");
            }
            foreach(string hero in new[]{"Taren","Sela"})
            {
                var extra=GameCatalog.Find<CharacterDef>(hero).extra;
                Assert.IsNotNull(extra,hero+" has no EXTRA move");CollectionAssert.DoesNotContain(SkillBook.All(hero),extra,hero+"'s EXTRA move can be put in a skill slot");
                Assert.IsNotNull(UiSkin.Skill(extra),extra.displayName+" has no icon");cells.Add(extra.iconCell);
                Assert.IsFalse(string.IsNullOrWhiteSpace(extra.description),extra.displayName+" doesn't say what it does");
            }
            CollectionAssert.AreEquivalent(Enumerable.Range(0,8).ToArray(),cells,"the eight icons on the sheet aren't one each");
        }
        /// <summary>The check the first sixteen have at Sync 12 (QaPlanCombatTests), for these six at theirs: cast from a slot with a body in reach, each does something.</summary>
        [UnityTest] public IEnumerator AtSync24EveryLateSkillDoesItsWork([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;var failures=new List<string>();int turn=0;string form=flight?"in flight":"on foot";
            foreach(var (who,_,kind) in Table)
            {
                var hero=Lead(who);var other=Other(hero);int slot=turn++%SkillBook.Slots;
                yield return UntilFree(hero);
                Place(hero,new Vector3(200,y,0));Place(other,new Vector3(flight?206:203,y,-3));
                float distance=kind switch{SkillKind.BulwarkRush=>flight?7:5,SkillKind.CinderBrand=>flight?4.5f:1.6f,SkillKind.Severance=>flight?4.5f:1.6f,_=>6};
                var body=Dummy(flight,new Vector3(200,y,distance));yield return Frames(3);hero.target=body.Health;var hits=Watch(body,hero);
                Hurt(hero,.5f);float was=hero.Health.integrity;
                Equip(hero,kind,slot);
                if(!hero.Skill(slot)){failures.Add($"{who}'s {kind} won't start {form} with full Charge");Object.Destroy(body.gameObject);continue;}
                bool worked=false;
                for(float end=Time.unscaledTime+4;!worked&&Time.unscaledTime<end;)
                {
                    hero.motor.Move(Vector2.zero,false,false);yield return null;
                    worked=kind==SkillKind.MendLattice?hero.Health.integrity>was:hits.count>0;
                }
                if(!worked)failures.Add($"{who}'s {kind} from slot {slot+1} {form} spends its Charge and does nothing");
                yield return UntilFree(hero);Object.Destroy(body.gameObject);yield return Frames(2);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ Bulwark Rush
        [UnityTest] public IEnumerator BulwarkRushShrugsOffAHitOnTheWayAndBreaksWhatItMeets()
        {
            yield return Zone(false);var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            // A hound's body and its Break, with nothing strong or weak against it and too much Integrity to fall.
            var body=Enemy("Ridgehound",new Vector3(200,0,7));body.Health.maximum=body.Health.integrity=1000000;body.Health.weakness=body.Health.resistance=DamageType.Pulse;
            yield return Frames(3);taren.target=body.Health;var hits=Watch(body,taren);
            float bare=taren.Health.Receive(new DamagePacket{source=body.Health,amount=60,type=DamageType.Kinetic});
            Assert.Greater(bare,0,"fixture: a hit on him lands");
            yield return new WaitForSecondsRealtime(.4f);
            Equip(taren,SkillKind.BulwarkRush);Assert.IsTrue(taren.Skill(0),"fixture: the rush");
            yield return Drive(taren,.05f);
            Assert.IsTrue(taren.Armored,"he isn't braced as the rush begins");
            float thrust=taren.thrust;int action=taren.AttackSequence;
            float braced=taren.Health.Receive(new DamagePacket{source=body.Health,amount=60,type=DamageType.Kinetic});
            Assert.AreEqual(bare*CombatActor.GuardTakes,braced,bare*.03f,"a hit on the way in doesn't land as on a guard");
            Assert.Greater(braced,0,"the rush is untouchable, where its row says hits barely land");
            Assert.GreaterOrEqual(taren.thrust,thrust-.01f,"the rush's armour is paid for in Thrust, as a held guard is");
            taren.Stagger(.5f);
            Assert.AreEqual(action,taren.AttackSequence,"a stagger stopped the rush");Assert.AreNotEqual(ActorState.Stagger,taren.State,"a stagger stopped the rush");
            yield return Drive(taren,1.2f);
            Assert.Less(Flat(taren.transform.position,body.transform.position),3,"the rush didn't carry him to a body 7 m off");
            Assert.AreEqual(1,hits.count,"the rush strikes what it meets once");
            Assert.AreEqual(Output(taren,false)*HeroSkills.RushImpact,hits.plain,Output(taren,false)*.06f,"its impact isn't the 1.8 of his Output its numbers say");
            Assert.IsTrue(body.Health.Broken,"it didn't break a body that a Pulse Burst breaks");
            Assert.IsFalse(taren.Armored,"the armour outlasts the rush");
            // And nothing in front of him: it goes its whole eight metres.
            yield return UntilFree(taren);Object.Destroy(body.gameObject);yield return Frames(2);
            Place(taren,new Vector3(200,0,0));taren.cooldowns[0]=0;taren.charge=100;Assert.IsTrue(taren.Skill(0),"fixture: the rush again");
            yield return Drive(taren,1.2f);
            Assert.AreEqual(HeroSkills.RushLength,Flat(taren.transform.position,new Vector3(200,0,0)),.8f,"with nothing in the way the rush isn't eight metres");
        }
        [UnityTest] public IEnumerator BulwarkRushInFlightRamsThroughALine()
        {
            yield return Zone(true);var taren=Lead("Taren");Place(taren,new Vector3(200,1,0));Place(Sela,new Vector3(260,1,0));
            var near=Dummy(true,new Vector3(200,1,5));var far=Dummy(true,new Vector3(200,1,8.5f));yield return Frames(3);taren.target=near.Health;
            var first=Watch(near,taren);var second=Watch(far,taren);
            Equip(taren,SkillKind.BulwarkRush);Assert.IsTrue(taren.Skill(0),"fixture: the burn");
            Assert.IsTrue(taren.Armored,"the hull isn't braced for the burn");
            yield return Drive(taren,.9f);
            Assert.AreEqual(1,first.count,"the burn doesn't strike the first body on its line once");Assert.AreEqual(1,second.count,"the burn stops at the first body: it's meant to go through a line");
            Assert.AreEqual(Output(taren,false)*HeroSkills.RushImpact,first.plain,Output(taren,false)*.06f,"the burn isn't 1.8 of his Output");
            Assert.Greater(taren.transform.position.z,7,"the hull was stopped by the first small body it met");
        }

        // ------------------------------------------------------------------ Cinder Brand
        [UnityTest] public IEnumerator CinderBrandMarksABodyToTakeAFifthMoreFromEveryoneForEightSeconds([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;var taren=Lead("Taren");var sela=Sela;Place(taren,new Vector3(200,y,0));Place(sela,new Vector3(260,y,0));
            var body=Dummy(flight,new Vector3(200,y,flight?4.5f:1.6f));yield return Frames(3);taren.target=body.Health;
            float Probe()=>body.Health.Receive(new DamagePacket{source=sela.Health,amount=100,type=DamageType.Beam});
            float before=Probe();Assert.AreEqual(100,before,.5f,"fixture: a plain hit of 100");
            Assert.IsFalse(body.Health.Branded,"fixture: no brand yet");
            var hits=Watch(body,taren);
            Equip(taren,SkillKind.CinderBrand);Assert.IsTrue(taren.Skill(0),"fixture: the brand");
            yield return Drive(taren,.8f);
            Assert.AreEqual(1,hits.count,"the branding strike didn't land once");
            Assert.AreEqual(Output(taren,false)*HeroSkills.BrandStrike,hits.plain,Output(taren,false)*.05f,"the branding strike isn't 1.5 of his Output: its own hit lands before the brand");
            Assert.IsTrue(body.Health.Branded,"the strike left no brand");
            Assert.AreEqual(before*Health.BrandTaken,Probe(),.5f,"the partner's hit on a branded body isn't a fifth more");
            Assert.AreEqual(1.2f,Health.BrandTaken,.001f,"a brand isn't worth the 20% its row says");
            Assert.IsNotNull(body.GetComponentInChildren<CinderBrand>(),"nothing on the body shows the brand");
            yield return Frames(3);
            StringAssert.Contains("BRANDED",HudText("Target").text,"the target card doesn't say it's branded");
            // Eight seconds of the world's clock, and it's gone from the body, the card and the numbers.
            for(float end=Time.unscaledTime+HeroSkills.BrandSeconds+2;body.Health.Branded&&Time.unscaledTime<end;)yield return null;
            Assert.IsFalse(body.Health.Branded,"the brand never ran out");
            yield return Frames(3);
            Assert.AreEqual(before,Probe(),.5f,"a brand that has run out still adds damage");
            Assert.IsNull(body.GetComponentInChildren<CinderBrand>(),"the mark stays on the body after the brand has gone");
            StringAssert.DoesNotContain("BRANDED",HudText("Target").text,"the card still says branded");
        }

        // ------------------------------------------------------------------ Severance
        [UnityTest] public IEnumerator SeveranceIsPlantedHitsABrokenBodyHalfAgainHarderAndAKillGivesHalfItsChargeBack()
        {
            yield return Zone(false);var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            var body=Dummy(false,new Vector3(200,0,1.6f));yield return Frames(3);taren.target=body.Health;var hits=Watch(body,taren);
            var skill=Equip(taren,SkillKind.Severance);float cost=taren.CostOf(skill);var stood=taren.transform.position;
            Assert.IsTrue(taren.Skill(0),"fixture: the cut");
            yield return Drive(taren,HeroSkills.SeverWindUp-.2f,new Vector2(1,0));
            Assert.AreEqual(0,hits.count,"it landed before its wind-up was done");
            Assert.Less(Flat(taren.transform.position,stood),.1f,"the stick walked him out of a wind-up he's meant to be planted for");
            yield return Drive(taren,.8f);
            Assert.AreEqual(1,hits.count,"the cut didn't land once");float whole=hits.plain;
            Assert.AreEqual(Output(taren,false)*HeroSkills.SeverCut,whole,Output(taren,false)*.1f,"the cut isn't 3.5 of his Output");
            Assert.Less(taren.charge,100-cost+cost*HeroSkills.SeverRefund-1,"a cut that killed nothing gave Charge back");
            Assert.IsEmpty(log.Where(l=>l.StartsWith("SEVERANCE_REFUND")).ToArray(),"a cut that killed nothing was refunded");
            // On a Broken body: the game's own half again, and the finisher's.
            yield return UntilFree(taren);hits.Clear();body.Health.Expose(6);Assert.IsTrue(body.Health.Broken,"fixture: Broken");
            taren.cooldowns[0]=0;taren.charge=100;Assert.IsTrue(taren.Skill(0),"fixture: the cut on a Broken body");
            yield return Drive(taren,1.4f);
            Assert.AreEqual(1,hits.count,"the cut on a Broken body didn't land once");
            Assert.AreEqual(1.5f*HeroSkills.SeverBroken,hits.plain/whole,.12f,"a Broken body doesn't take the finisher's half again on top of the game's");
            // A kill pays half of it back.
            yield return UntilFree(taren);Object.Destroy(body.gameObject);yield return Frames(2);
            var small=Enemy("Ridgehound",new Vector3(200,0,1.6f));small.Health.maximum=small.Health.integrity=10;yield return Frames(3);taren.target=small.Health;
            taren.cooldowns[0]=0;taren.charge=100;Assert.IsTrue(taren.Skill(0),"fixture: the killing cut");
            yield return Drive(taren,1.4f);
            Assert.IsFalse(small!=null&&small.Health.Alive,"fixture: the cut killed a body of 10 Integrity");
            Assert.IsNotEmpty(log.Where(l=>l.StartsWith("SEVERANCE_REFUND")).ToArray(),"a kill wasn't refunded");
            Assert.GreaterOrEqual(taren.charge,100-cost+cost*HeroSkills.SeverRefund-1,"a kill didn't give half the cut's Charge back");
        }
        [UnityTest] public IEnumerator SeveranceInFlightIsAWoundUpCutAhead()
        {
            yield return Zone(true);var taren=Lead("Taren");Place(taren,new Vector3(200,1,0));Place(Sela,new Vector3(260,1,0));
            var body=Dummy(true,new Vector3(200,1,4.5f));yield return Frames(3);taren.target=body.Health;var hits=Watch(body,taren);
            Equip(taren,SkillKind.Severance);Assert.IsTrue(taren.Skill(0),"fixture: the cut");
            yield return Drive(taren,HeroSkills.SeverFlightWindUp-.2f);
            Assert.AreEqual(0,hits.count,"in flight it landed before its wind-up");
            yield return Drive(taren,.9f);
            Assert.AreEqual(1,hits.count,"in flight the cut didn't land once on a body 4.5 m ahead");
            Assert.AreEqual(Output(taren,false)*HeroSkills.SeverCut,hits.plain,Output(taren,false)*.1f,"in flight the cut isn't 3.5 of his Output");
        }

        // ------------------------------------------------------------------ Mend Lattice
        [UnityTest] public IEnumerator MendLatticeMendsBothHeroesInItAndNeverRevives()
        {
            yield return Zone(false);var sela=Lead("Sela");var taren=Taren;
            Place(sela,new Vector3(200,0,0));Place(taren,new Vector3(204,0,0));yield return Frames(2);
            Hurt(sela,.4f);Hurt(taren,.4f);float hers=sela.Health.integrity,his=taren.Health.integrity;
            Equip(sela,SkillKind.MendLattice);Assert.IsTrue(sela.Skill(0),"fixture: the lattice");
            yield return Drive(sela,.6f);
            var field=MendField.Latest;Assert.IsNotNull(field,"no field was laid");
            Assert.Less(Flat(field.transform.position,new Vector3(202,0,0)),.6f,"with her partner 4 m off the field isn't laid between them");
            Assert.AreEqual(hers,sela.Health.integrity,sela.Health.maximum*.031f,"it mended most of its worth at once: it's meant to take five seconds");
            yield return new WaitForSecondsRealtime(MendField.Seconds+.5f);
            float share=MendField.PerSecond*MendField.Seconds;
            Assert.AreEqual(.3f,share,.001f,"6% a second for 5 s is its row's promise");
            Assert.AreEqual(hers+sela.Health.maximum*share,sela.Health.integrity,sela.Health.maximum*.035f,"she wasn't mended 30% of her Integrity over the five seconds");
            Assert.AreEqual(his+taren.Health.maximum*share,taren.Health.integrity,taren.Health.maximum*.035f,"her partner, standing in it, wasn't mended as she was");
            Assert.IsTrue(MendField.Latest==null,"the field is still there after its five seconds");
            // A hero out of it gets nothing.
            yield return UntilFree(sela);Place(taren,new Vector3(230,0,0));Hurt(sela,.4f);Hurt(taren,.4f);his=taren.Health.integrity;
            sela.cooldowns[0]=0;sela.charge=100;Assert.IsTrue(sela.Skill(0),"fixture: the lattice again");
            yield return Drive(sela,.6f);
            Assert.Less(Flat(MendField.Latest.transform.position,sela.transform.position),.4f,"with her partner far off the field isn't laid where she stands");
            yield return new WaitForSecondsRealtime(MendField.Seconds+.5f);
            Assert.AreEqual(his,taren.Health.integrity,.01f,"a hero 30 m from the field was mended");
            Assert.Greater(sela.Health.integrity,sela.Health.maximum*.6f,"fixture: she was mended");
            // A hero who's down in it stays down: 3.5 m off is inside the field and out of reach of the party's own revive.
            yield return UntilFree(sela);Place(taren,new Vector3(203.5f,0,0));
            taren.Health.Receive(new DamagePacket{amount=taren.Health.maximum*10});yield return new WaitForSecondsRealtime(.3f);
            Assert.IsFalse(taren.Health.Alive,"fixture: her partner went down");
            Hurt(sela,.4f);sela.cooldowns[0]=0;sela.charge=100;Assert.IsTrue(sela.Skill(0),"fixture: the lattice over a downed partner");
            yield return Drive(sela,.6f);
            Assert.Less(Flat(MendField.Latest.transform.position,taren.transform.position),MendField.Radius,"fixture: the downed hero lies inside the field");
            yield return new WaitForSecondsRealtime(MendField.Seconds+.5f);
            Assert.IsFalse(taren.Health.Alive,"Mend Lattice revived a hero who was down");Assert.AreEqual(0,taren.Health.integrity,.001f,"Mend Lattice gave a downed hero Integrity");
        }
        [UnityTest] public IEnumerator MendLatticeInFlightIsAWakeThatMendsWhoeverFliesInIt()
        {
            yield return Zone(true);var sela=Lead("Sela");var taren=Taren;
            Place(sela,new Vector3(200,1,0));Place(taren,new Vector3(200,1,-6));yield return Frames(2);
            Hurt(sela,.4f);Hurt(taren,.4f);float hers=sela.Health.integrity,his=taren.Health.integrity;
            Equip(sela,SkillKind.MendLattice);Assert.IsTrue(sela.Skill(0),"fixture: the wake");
            // She flies ahead while it lays, and he follows her line.
            for(float end=Time.unscaledTime+2;Time.unscaledTime<end;){sela.motor.Move(new Vector2(0,1),false,false);taren.motor.Move(new Vector2(0,1),false,false);yield return null;}
            var wake=MendWake.Latest;Assert.IsNotNull(wake,"no wake was laid");
            Assert.IsTrue(wake.In(taren.transform.position),"fixture: he's flying in her wake");
            for(float end=Time.unscaledTime+6.5f;Time.unscaledTime<end;){sela.motor.Move(Vector2.zero,false,true);taren.motor.Move(Vector2.zero,false,true);yield return null;}
            float most=MendField.PerSecond*MendField.Seconds;
            Assert.Greater(sela.Health.integrity,hers+sela.Health.maximum*.1f,"she wasn't mended by her own wake");
            Assert.Greater(taren.Health.integrity,his+taren.Health.maximum*.1f,"her partner, flying in the wake, wasn't mended");
            Assert.LessOrEqual(sela.Health.integrity,hers+sela.Health.maximum*(most+.005f),"one wake mended her more than the field's whole worth");
            Assert.LessOrEqual(taren.Health.integrity,his+taren.Health.maximum*(most+.005f),"one wake mended him more than the field's whole worth");
            Assert.IsTrue(MendWake.Latest==null,"the wake is still there after it has run out");
            // A ship that never comes near it gets nothing.
            yield return UntilFree(sela);Place(sela,new Vector3(200,1,0));Place(taren,new Vector3(240,1,0));Hurt(sela,.4f);Hurt(taren,.4f);his=taren.Health.integrity;
            sela.cooldowns[0]=0;sela.charge=100;Assert.IsTrue(sela.Skill(0),"fixture: the wake again");
            for(float end=Time.unscaledTime+6;Time.unscaledTime<end;){sela.motor.Move(Vector2.zero,false,true);taren.motor.Move(Vector2.zero,false,true);yield return null;}
            Assert.AreEqual(his,taren.Health.integrity,.01f,"a ship 40 m from the wake was mended");
        }

        // ------------------------------------------------------------------ Prism Relay
        [UnityTest] public IEnumerator PrismRelayJumpsThroughFiveBodiesAndNoFurtherAndNotRoundAWall()
        {
            yield return Zone(false);var sela=Lead("Sela");Place(sela,new Vector3(200,0,0));Place(Taren,new Vector3(260,0,0));
            // Six in a zig-zag, each nearest the one before it.
            var spots=new[]{new Vector3(200,0,6),new Vector3(203,0,9),new Vector3(198,0,12),new Vector3(203,0,15),new Vector3(198,0,18),new Vector3(203,0,21)};
            var bodies=spots.Select(at=>Dummy(false,at)).ToArray();yield return Frames(3);sela.target=bodies[0].Health;
            var hits=bodies.Select(b=>Watch(b,sela)).ToArray();
            Equip(sela,SkillKind.PrismRelay);Assert.IsTrue(sela.Skill(0),"fixture: the relay");
            yield return Drive(sela,1.3f);
            for(int i=0;i<5;i++)
            {
                Assert.AreEqual(1,hits[i].count,$"body {i+1} of the chain was struck {hits[i].count} times: a relay strikes each body once");
                Assert.AreEqual(Output(sela,true)*HeroSkills.RelayEach,hits[i].plain,Output(sela,true)*.05f,$"body {i+1} didn't take 1.2 of her Output");
            }
            Assert.AreEqual(0,hits[5].count,"the relay reached a sixth body: five is its most");
            // A wall between two bodies ends it at the first.
            yield return UntilFree(sela);foreach(var body in bodies)Object.Destroy(body.gameObject);yield return Frames(2);
            var front=Dummy(false,new Vector3(200,0,6));var hidden=Dummy(false,new Vector3(200,0,10));var wall=Wall(new Vector3(200,1.5f,8),new Vector3(10,3,.5f));
            yield return Frames(3);sela.target=front.Health;var struck=Watch(front,sela);var behind=Watch(hidden,sela);
            sela.cooldowns[0]=0;sela.charge=100;Assert.IsTrue(sela.Skill(0),"fixture: the relay at a wall");
            yield return Drive(sela,1.3f);
            Assert.AreEqual(1,struck.count,"fixture: the first body was struck");Assert.AreEqual(0,behind.count,"the relay jumped to a body behind a wall");
            Object.Destroy(wall);
        }
        [UnityTest] public IEnumerator PrismRelayJumpsBetweenBodiesInFlight()
        {
            yield return Zone(true);var sela=Lead("Sela");Place(sela,new Vector3(200,1,0));Place(Taren,new Vector3(260,1,0));
            var bodies=new[]{new Vector3(200,1,6),new Vector3(204,1,9),new Vector3(199,1,12)}.Select(at=>Dummy(true,at)).ToArray();yield return Frames(3);sela.target=bodies[0].Health;
            var hits=bodies.Select(b=>Watch(b,sela)).ToArray();
            Equip(sela,SkillKind.PrismRelay);Assert.IsTrue(sela.Skill(0),"fixture: the relay");
            yield return Drive(sela,1.2f);
            for(int i=0;i<3;i++)Assert.AreEqual(1,hits[i].count,$"in flight body {i+1} of three was struck {hits[i].count} times");
        }

        // ------------------------------------------------------------------ Long Exposure
        [UnityTest] public IEnumerator LongExposureRootsHerForTenPiercingTicksAndARollLetsGo([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;var sela=Lead("Sela");Place(sela,new Vector3(200,y,0));Place(Taren,new Vector3(260,y,0));
            var near=Dummy(flight,new Vector3(200,y,6));var far=Dummy(flight,new Vector3(200,y,10));yield return Frames(3);sela.target=near.Health;
            var first=Watch(near,sela);var second=Watch(far,sela);var stood=sela.transform.position;
            Equip(sela,SkillKind.LongExposure);Assert.IsTrue(sela.Skill(0),"fixture: the exposure");
            // The stick is held over the whole of it: she's rooted.
            yield return Drive(sela,HeroSkills.ExposureSeconds-.3f,new Vector2(1,0));
            Assert.IsTrue(sela.Rooted,"she isn't rooted two seconds into a channel of two and a half");
            Assert.Less(Flat(sela.transform.position,stood),flight?.4f:.1f,"the stick moved her out of a channel she's meant to be rooted for");
            yield return Drive(sela,.9f);
            Assert.AreEqual(HeroSkills.ExposureTicks,first.count,"the channel didn't tick ten times on the body in its line");
            Assert.AreEqual(HeroSkills.ExposureTicks,second.count,"the beam doesn't pierce: the body behind the first wasn't ticked as it was");
            foreach(float tick in first.each)Assert.AreEqual(Output(sela,true)*HeroSkills.ExposureEach,tick,Output(sela,true)*.03f,"a tick isn't half her Output");
            Assert.AreEqual(2.5f,HeroSkills.ExposureSeconds,.001f);Assert.AreEqual(.25f,HeroSkills.ExposureTick,.001f);
            Assert.IsFalse(sela.Rooted,"she's still rooted after the channel");
            // A roll lets go of it.
            yield return UntilFree(sela);Place(sela,new Vector3(200,y,0));sela.target=near.Health;first.Clear();
            sela.cooldowns[0]=0;sela.charge=100;Assert.IsTrue(sela.Skill(0),"fixture: the exposure again");
            yield return Drive(sela,.65f);
            Assert.IsTrue(sela.Rooted,"fixture: mid-channel");int ticked=first.count;Assert.Greater(ticked,0,"fixture: it had begun to tick");
            Assert.IsTrue(sela.Dodge(Vector3.right),"a roll is refused during the channel: it's the way out of it");
            Assert.IsFalse(sela.Rooted,"a roll didn't free her");
            yield return Drive(sela,1.4f);
            Assert.LessOrEqual(first.count,ticked+1,"the beam went on ticking after she rolled out of it");
            Assert.Greater(Flat(sela.transform.position,new Vector3(200,y,0)),1.5f,"the roll that let go of the channel went nowhere");
        }

        // ------------------------------------------------------------------ the partner
        [UnityTest] public IEnumerator ThePartnersRulesForEachLateSkill()
        {
            yield return Zone(false);var taren=Taren;var sela=Sela;Place(taren,new Vector3(200,0,0));Place(sela,new Vector3(204,0,0));
            var body=Dummy(false,new Vector3(200,0,8));yield return Frames(3);taren.target=sela.target=body.Health;
            bool Refuses(CombatActor hero,SkillKind kind)=>LateSkills.Decides(hero,kind,out bool worth)&&!worth;
            // Mend Lattice: never at full Integrity, and for either of them once one is hurt.
            Assert.IsTrue(Refuses(sela,SkillKind.MendLattice),"a partner would lay a Mend Lattice with both heroes at full Integrity");
            Hurt(taren,.4f);Assert.IsTrue(LateSkills.Decides(sela,SkillKind.MendLattice,out bool mend)&&mend,"a partner wouldn't mend a lead at 40% four metres away");
            Hurt(taren,1);Hurt(sela,.4f);Assert.IsTrue(LateSkills.Decides(sela,SkillKind.MendLattice,out mend)&&mend,"a partner at 40% wouldn't mend herself");
            Hurt(sela,1);
            // Long Exposure: not in a marked danger, and not at arm's length.
            Assert.IsFalse(LateSkills.Decides(sela,SkillKind.LongExposure,out _),"with a body 9 m off and nothing marked, the rules stop a partner's Long Exposure");
            var marking=Telegraph.Circle(sela.transform.position,4,30,Telegraph.Threat,true);marking.owner=body;
            Assert.IsTrue(LateSkills.InDanger(sela),"fixture: a marking lies over her");
            Assert.IsTrue(Refuses(sela,SkillKind.LongExposure),"a partner would root herself for two and a half seconds inside a marked danger");
            marking.Clear();yield return Frames(2);
            Assert.IsFalse(LateSkills.InDanger(sela),"a marking that has gone still counts as a danger");
            Place(sela,new Vector3(200,0,5.5f));sela.target=body.Health;Assert.IsTrue(Refuses(sela,SkillKind.LongExposure),"a partner would channel at a body 2.5 m off");
            Place(sela,new Vector3(204,0,0));sela.target=body.Health;
            // Prism Relay wants somewhere to go.
            Assert.IsTrue(Refuses(sela,SkillKind.PrismRelay),"a partner would spend a relay on a body with nothing near it to jump to");
            var second=Dummy(false,new Vector3(203,0,10));yield return Frames(2);
            Assert.IsFalse(LateSkills.Decides(sela,SkillKind.PrismRelay,out _),"with a second body in reach of the first, the rules stop a partner's relay");
            Object.Destroy(second.gameObject);
            // Bulwark Rush closes a gap; it isn't used from on top of the target.
            Assert.IsFalse(LateSkills.Decides(taren,SkillKind.BulwarkRush,out _),"with a body 8 m off the rules stop a partner's rush");
            Place(taren,new Vector3(200,0,6));taren.target=body.Health;Assert.IsTrue(Refuses(taren,SkillKind.BulwarkRush),"a partner would rush a body 2 m away");
            // Cinder Brand goes on what will live to be hit, once.
            Assert.IsFalse(LateSkills.Decides(taren,SkillKind.CinderBrand,out _),"the rules stop a partner branding a healthy body");
            body.Health.Brand(5);Assert.IsTrue(Refuses(taren,SkillKind.CinderBrand),"a partner would brand a body that's already branded");
            body.Health.BrandUntil=0;body.Health.integrity=taren.damage;Assert.IsTrue(Refuses(taren,SkillKind.CinderBrand),"a partner would brand a body one swing from the floor");
            body.Health.integrity=body.Health.maximum;
            // Severance waits for a break.
            Assert.IsTrue(Refuses(taren,SkillKind.Severance),"a partner would spend the finisher on a healthy, unbroken body");
            body.Health.Expose(4);Assert.IsFalse(LateSkills.Decides(taren,SkillKind.Severance,out _),"the rules stop a partner's finisher on a Broken body");
        }
        /// <summary>One skill in the partner's hands: its slot ready, the other three held on cooldown.</summary>
        IEnumerator Partnered(CombatActor partner,float seconds,System.Func<bool> until=null)
        {
            for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end&&(until==null||!until());)
            {partner.charge=100;partner.cooldowns[0]=0;for(int i=1;i<4;i++)partner.cooldowns[i]=5;yield return null;}
        }
        bool Cast(string hero,SkillKind kind)=>log.Any(l=>l.StartsWith($"SKILL_KIND {hero} ")&&l.Contains(" "+kind+" "));
        [UnityTest] public IEnumerator APartnerDoesNotMendAtFullIntegrityAndDoesWhenHurt()
        {
            yield return Zone(false);var taren=Lead("Taren");var sela=Sela;Place(taren,new Vector3(200,0,0));Place(sela,new Vector3(203,0,-2));
            var body=Dummy(false,new Vector3(200,0,6));yield return Frames(3);taren.target=body.Health;
            Equip(sela,SkillKind.MendLattice);sela.GetComponent<PartnerBrain>().enabled=true;
            // Past the three seconds a partner waits before its first skill, with full Charge and a fight on: at full Integrity it keeps its Charge.
            yield return Partnered(sela,5.5f);
            Assert.IsFalse(Cast("Sela",SkillKind.MendLattice),"the partner laid a Mend Lattice with both heroes at full Integrity");
            Hurt(sela,.4f);float was=sela.Health.integrity;
            yield return Partnered(sela,4,()=>Cast("Sela",SkillKind.MendLattice));
            Assert.IsTrue(Cast("Sela",SkillKind.MendLattice),"a partner at 40% Integrity, with Charge and the skill ready, never mended");
            yield return Partnered(sela,2);
            Assert.Greater(sela.Health.integrity,was,"the partner's lattice mended nobody");
        }
        [UnityTest] public IEnumerator APartnerDoesNotRootHerselfInAMarkedDangerAndLetsGoWhenOneArrives()
        {
            yield return Zone(false);var taren=Lead("Taren");var sela=Sela;Place(taren,new Vector3(200,0,0));Place(sela,new Vector3(203,0,-2));
            var body=Dummy(false,new Vector3(200,0,7));yield return Frames(3);taren.target=body.Health;
            Equip(sela,SkillKind.LongExposure);
            // A danger marked over the whole yard, by something that isn't a boss, so she doesn't run from it: only the skill's own rule is in play.
            var marking=Telegraph.Circle(new Vector3(200,0,0),45,120,Telegraph.Threat,true);marking.owner=body;
            sela.GetComponent<PartnerBrain>().enabled=true;
            yield return Partnered(sela,5.5f);
            Assert.IsFalse(Cast("Sela",SkillKind.LongExposure),"the partner rooted herself for a Long Exposure inside a marked danger");
            marking.Clear();
            yield return Partnered(sela,5,()=>Cast("Sela",SkillKind.LongExposure));
            Assert.IsTrue(Cast("Sela",SkillKind.LongExposure),"with the danger gone, a body in reach and the skill ready, the partner never used it");
            yield return Partnered(sela,.5f);
            Assert.IsTrue(sela.Rooted,"fixture: the partner is mid-channel");
            marking=Telegraph.Circle(new Vector3(200,0,0),45,120,Telegraph.Threat,true);marking.owner=body;
            yield return Partnered(sela,.4f);
            Assert.IsFalse(sela.Rooted,"a danger was marked over the partner mid-channel and she stayed rooted in it");
            marking.Clear();
        }

        // ------------------------------------------------------------------ heard, and stopped by walls
        [UnityTest] public IEnumerator EveryLateSkillSoundsFromItsCaster([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;var failures=new List<string>();
            var heard=new List<(string key,Transform owner)>();void Cue(string key,Transform owner){heard.Add((key,owner));}
            AudioManager.CueStarted+=Cue;
            try
            {
                foreach(var (who,_,kind) in Table)
                {
                    var hero=Lead(who);yield return UntilFree(hero);Place(hero,new Vector3(200,y,0));Place(Other(hero),new Vector3(260,y,0));
                    yield return new WaitForSecondsRealtime(.3f);
                    Equip(hero,kind);heard.Clear();
                    if(!hero.Skill(0)){failures.Add(kind+" did not start");continue;}
                    for(float end=Time.unscaledTime+1;Time.unscaledTime<end&&!heard.Any(c=>c.owner==hero.transform);){hero.motor.Move(Vector2.zero,false,false);yield return null;}
                    if(!heard.Any(c=>c.owner==hero.transform))failures.Add($"{who}'s {kind} has no sound from the hero who cast it (heard: {string.Join(",",heard.Select(c=>c.key))})");
                }
            }
            finally{AudioManager.CueStarted-=Cue;}
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator NoLateSkillReachesABodyBehindAWall([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;string form=flight?"in flight":"on foot";var failures=new List<string>();
            // (the skill, where the wall stands, where the body stands behind it, how long the skill could still land)
            var cases=flight?new[]{(SkillKind.BulwarkRush,4f,6.5f,1.2f),(SkillKind.CinderBrand,3f,4.5f,.9f),(SkillKind.Severance,3f,5f,1.4f),(SkillKind.PrismRelay,3f,5f,1f),(SkillKind.LongExposure,3f,5f,3f)}:
                new[]{(SkillKind.BulwarkRush,3f,4.2f,1.2f),(SkillKind.CinderBrand,1.2f,2.2f,.9f),(SkillKind.Severance,1.2f,2.2f,1.5f),(SkillKind.PrismRelay,3f,5f,1f),(SkillKind.LongExposure,3f,5f,3f)};
            foreach(var (kind,wallAt,bodyAt,wait) in cases)
            {
                var hero=Lead(Table.First(t=>t.kind==kind).hero);yield return UntilFree(hero);
                Place(hero,new Vector3(200,y,0));Place(Other(hero),new Vector3(260,y,0));
                var wall=Wall(new Vector3(200,1.5f,wallAt),new Vector3(12,3,.5f));var body=Dummy(flight,new Vector3(200,y,bodyAt));yield return Frames(3);
                hero.target=body.Health;var hits=Watch(body,hero);
                Equip(hero,kind);
                if(!hero.Skill(0)){failures.Add("fixture: "+kind+" would not start");}
                else
                {
                    yield return Drive(hero,wait);
                    if(hits.count>0)failures.Add($"{kind} {form} struck a body {bodyAt} m off through a wall {wallAt} m off");
                }
                yield return UntilFree(hero);Object.Destroy(wall);Object.Destroy(body.gameObject);yield return Frames(2);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
