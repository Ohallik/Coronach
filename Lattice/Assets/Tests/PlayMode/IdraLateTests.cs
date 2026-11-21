using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>S6a: Idra Sorn on the ladder the first two heroes climb. Her eleven skills come at the Sync levels theirs do, mastery makes hers stronger and cheaper as it
    /// makes theirs, and her three at Sync 16, 20 and 24 each do their own job on foot and in flight: Take the Weight takes her partner's hits on herself, Cutting Torch
    /// cuts through a plated front, Bulkhead stops shots and holds bodies back. Her companion's rules for the three, and her own voice in a fight. She fights in Taren's
    /// place beside Sela, at Sync 24, where every one of them is known.</summary>
    public sealed class IdraLateTests
    {
        const int Level=24;
        readonly List<(string who,string line)> said=new();
        void Heard(string who,string line)=>said.Add((who,line));
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");said.Clear();BarkService.Spoken+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            BarkService.Spoken-=Heard;
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static CombatActor Idra=>PartyController.Current.members.First(m=>m.character=="Idra");
        static CombatActor Sela=>PartyController.Current.members.First(m=>m.character=="Sela");
        static MemberState Member(string id)=>GameServices.Current.State.party.Find(m=>m.id==id);
        static void Quiet(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        /// <summary>The arena with Idra in Taren's place beside Sela, both at Sync 24, on open floor.</summary>
        static IEnumerator Zone(bool flight)
        {
            Assert.IsNotNull(GameCatalog.Find<CharacterDef>("Idra"),"Idra has no definition: run Lattice.EditorTools.IdraBuilder.Build");
            var state=GameServices.Current.State;state.party.RemoveAll(m=>m.id=="Taren"||m.id=="Idra");
            state.party.Insert(0,new MemberState{id="Idra",level=Level,charge=100});state.activeMember=0;
            SceneFlow.Current.LoadZone(flight?"Arena_Flight":"Arena_Ground");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"the arena never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            Quiet();
            yield return new WaitForSecondsRealtime(.7f);
            if(!flight){var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Idra fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
            foreach(var member in state.party){member.level=Level;member.skillUses?.Clear();}
            PartyController.Current.RefreshStats();
            Assert.AreEqual(2,PartyController.Current.members.Length,"fixture: a party of two, Idra and Sela");
            foreach(var hero in PartyController.Current.members){hero.Health.integrity=hero.Health.maximum;hero.thrust=100;}
        }
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}
            else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            Physics.SyncTransforms();
        }
        static EnemyBrain Enemy(string id,Vector3 at,bool passive=true){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=passive;return enemy;}
        /// <summary>A body that never dies or breaks, and that Kinetic and Plasma are neither strong nor weak against.</summary>
        static EnemyBrain Dummy(bool flight,Vector3 at,DamageType odd=DamageType.Pulse)
        {
            var enemy=Enemy(flight?"ChoristerDart":"Ridgehound",at);var body=enemy.Health;
            body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;body.weakness=body.resistance=odd;return enemy;
        }
        static CombatActor Lead(string hero)
        {
            var party=PartyController.Current;if(party.Active.character!=hero)Assert.IsTrue(party.Swap(),"fixture: the swap");
            Quiet();return party.Active;
        }
        static SkillDef Equip(CombatActor hero,SkillKind kind,int slot=0)
        {
            var skill=SkillBook.All(hero.character).FirstOrDefault(s=>s.kind==kind);
            Assert.IsNotNull(skill,$"{hero.character} has no {kind}: run Lattice.EditorTools.IdraBuilder.Build");
            Assert.IsTrue(SkillBook.Equip(Member(hero.character),slot,skill),"fixture: "+kind+" can't be equipped at Sync "+Level);
            hero.charge=100;for(int i=0;i<4;i++)hero.cooldowns[i]=0;return skill;
        }
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Drive(CombatActor actor,float seconds,Vector2 stick=default)
        {for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){actor.motor.Move(stick,false,false);yield return null;}}
        static IEnumerator UntilFree(CombatActor hero)
        {for(float end=Time.unscaledTime+4;!hero.CanAct&&Time.unscaledTime<end;){hero.motor.Move(Vector2.zero,false,false);yield return null;}}
        static IEnumerator Until(CombatActor hero,System.Func<bool> done,float seconds)
        {for(float end=Time.unscaledTime+seconds;!done()&&Time.unscaledTime<end;){hero.motor.Move(Vector2.zero,false,false);yield return null;}}
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static void Hurt(CombatActor hero,float share){hero.Health.integrity=hero.Health.maximum*share;}
        /// <summary>What a skill's multiple is a multiple of: her Output, by Resonance as every skill is. Mastery is 1 unless a check raises it.</summary>
        static float Output(CombatActor hero)=>hero.damage*(1+Mathf.Max(0,hero.resonance-10)*.025f);
        /// <summary>A hit after a hero's Plating, as Health takes it.</summary>
        static float Plated(CombatActor hero,float amount)=>amount*(100/(100+Mathf.Max(0,hero.Plating-10)));
        static float Take(CombatActor hero,Health from,float amount)
        {float before=hero.Health.integrity;hero.Health.Receive(new DamagePacket{source=from,amount=amount,type=DamageType.Kinetic});return before-hero.Health.integrity;}
        sealed class Hits{public int count;public float plain;public readonly List<float> each=new();public readonly List<HitWeight> weights=new();public readonly List<string> tags=new();}
        static Hits Watch(EnemyBrain body,CombatActor hero)
        {
            var hits=new Hits();
            body.Health.Damaged+=(_,packet,amount)=>
            {
                if(packet.source!=hero.Health||amount<=0)return;
                float plain=packet.isCrit?amount/(1.5f*(1+Mathf.Max(0,hero.fortune)*.01f+hero.mods.critDamage)):amount;
                hits.count++;hits.plain+=plain;hits.each.Add(plain);hits.weights.Add(packet.weight);hits.tags.Add(packet.tag);
            };
            return hits;
        }
        static Projectile Shot(EnemyBrain from,Vector3 at,Vector3 toward,float amount=30,float reach=40)
        {var aim=toward-at;aim.y=0;return Projectile.Fire(at,aim.normalized,new DamagePacket{source=from.Health,amount=amount,type=DamageType.Kinetic},11,reach);}

        // ------------------------------------------------------------------ the ladder
        static readonly SkillKind[] Late={SkillKind.TakeTheWeight,SkillKind.CuttingTorch,SkillKind.Bulkhead};
        /// <summary>The first two learn four more at Sync 3, 5, 8 and 12 and three more at 16, 20 and 24 (SkillProgressionTests). She stood at eight with nothing after
        /// Sync 12; she climbs the same ladder now, rung for rung.</summary>
        [UnityTest] public IEnumerator HerSkillsComeOnTheLadderTheOthersClimb()
        {
            yield return null;
            var state=GameServices.Current.State;state.party.RemoveAll(m=>m.id=="Idra");var member=new MemberState{id="Idra",level=1};state.party.Add(member);
            var hers=SkillBook.All("Idra");var his=SkillBook.All("Taren");var definition=GameCatalog.Find<CharacterDef>("Idra");
            Assert.AreEqual(his.Length,hers.Length,"she has a different number of skills from Taren: run Lattice.EditorTools.IdraBuilder.Build");
            Assert.AreEqual(hers.Length,hers.Select(s=>s.kind).Distinct().Count(),"two of her skills do the same thing");
            var theirs=his.Concat(SkillBook.All("Sela")).Select(s=>s.kind).ToArray();
            Assert.IsEmpty(hers.Select(s=>s.kind).Intersect(theirs),"she shares a skill with the first two heroes");
            CollectionAssert.AreEqual(his.Select(s=>s.unlockLevel).OrderBy(l=>l).ToArray(),hers.Select(s=>s.unlockLevel).OrderBy(l=>l).ToArray(),"her skills don't come at the Sync levels his do");
            for(int i=0;i<4;i++)Assert.AreEqual(definition.skills[i],SkillBook.Equipped("Idra",i),"she doesn't start with her first four");
            foreach(var (kind,level) in new[]{(SkillKind.TakeTheWeight,16),(SkillKind.CuttingTorch,20),(SkillKind.Bulkhead,24)})
                Assert.AreEqual(level,hers.Single(s=>s.kind==kind).unlockLevel,kind+" is learned at the wrong Sync");
            var cells=new HashSet<(string,int)>();
            foreach(var skill in hers.Append(definition.extra))
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(skill.description),skill.id+" has no description");
                Assert.IsFalse(string.IsNullOrWhiteSpace(skill.shortName),skill.id+" has no label for the HUD");
                Assert.IsNotNull(UiSkin.Skill(skill),skill.id+" has no icon");Assert.IsTrue(cells.Add((skill.iconSheet,skill.iconCell)),skill.id+" wears another skill's icon");
            }
            foreach(var (level,known) in new[]{(1,4),(3,5),(5,6),(8,7),(12,8),(15,8),(16,9),(20,10),(24,11),(30,11)})
            {member.level=level;Assert.AreEqual(known,SkillBook.Learned("Idra").Length,$"at Sync {level} she knows the wrong number of skills");}
            member.level=15;Assert.IsFalse(SkillBook.Equip(member,0,hers.Single(s=>s.kind==SkillKind.TakeTheWeight)),"Take the Weight can be equipped before Sync 16");
            member.level=16;Assert.IsTrue(SkillBook.Equip(member,0,hers.Single(s=>s.kind==SkillKind.TakeTheWeight)),"Take the Weight can't be equipped at Sync 16");
            // A level-up says what it taught, as it does for the first two.
            CollectionAssert.AreEquivalent(new[]{SkillKind.CuttingTorch},SkillBook.LearnedBetween("Idra",19,20).Select(s=>s.kind).ToArray(),"Sync 20 doesn't teach her the torch");
            // The Party page's row gives the Output she fights with: her hammer's Edge (PartyController.RefreshStats reads the Edge's sum for a blow), not an Emitter she hasn't got.
            GameServices.GiveStartingGear(state,member);var definitionGear=GameCatalog.Find<CharacterDef>("Idra");
            var edge=Gear.Calculate(state,member,definitionGear,GearSlot.Edge,GameCatalog.Find<TechPartDef>,GameCatalog.Find<AffixDef>);
            var emitter=Gear.Calculate(state,member,definitionGear,GearSlot.Emitter,GameCatalog.Find<TechPartDef>,GameCatalog.Find<AffixDef>);
            Assert.AreNotEqual(Mathf.Round(edge.output),Mathf.Round(emitter.output),"fixture: her arrival gear makes the two sums differ");
            string row=(string)typeof(PauseMenu).GetMethod("PartyLine",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{state,member});
            StringAssert.Contains($"Output {edge.output:0} ",row,"the Party page gives her an Output she doesn't fight with");
            state.party.Remove(member);
        }
        /// <summary>What QaCombatTests.SkillRowsSayWhatTheSkillsDo asks of the first two heroes' rows, asked of hers: no word a flight row can't keep, Charge only as its
        /// cost, and a row and a note that fit the skill chooser on a pad.</summary>
        [UnityTest] public IEnumerator HerSkillRowsSayWhatTheyDoInWordsTheChooserHasRoomFor()
        {
            yield return Zone(false);var definition=GameCatalog.Find<CharacterDef>("Idra");
            var probe=UiKit.Text(Object.FindFirstObjectByType<GameHud>().transform,"Row probe","",26,Color.white);var noteProbe=UiKit.Text(probe.transform.parent,"Note probe","",24,Color.white);
            var failures=new List<string>();
            foreach(var skill in SkillBook.All("Idra").Append(definition.extra))
            {
                string text=skill.description;
                if(text.Contains("the ground"))failures.Add($"{skill.displayName}'s row talks about the ground, and it is read in flight too");
                if(System.Text.RegularExpressions.Regex.IsMatch(text,@"\bcharge\b"))failures.Add($"{skill.displayName}'s row says 'charge' on a row that lists Charge as its cost");
                if(System.Text.RegularExpressions.Regex.IsMatch(text,@"\b(health|Health|HP)\b"))failures.Add($"{skill.displayName}'s row says health: the game calls it Integrity");
                float width=probe.GetPreferredValues($"On RB+Y · {skill.displayName}  ·  {SkillBook.Cost("Idra",skill):0} Charge  ·  Mastery 4 (59/60)  ·  {skill.cooldown:0} s").x;
                if(width>1136)failures.Add($"{skill.displayName}'s row is {width:0} wide in a 1136 label");
                float said=noteProbe.GetPreferredValues($"{skill.displayName}: {text}").x;
                if(said>1220)failures.Add($"{skill.displayName}'s description is {said:0} wide on a 1220 line");
            }
            Object.Destroy(probe.gameObject);Object.Destroy(noteProbe.gameObject);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator MasteryMakesHerSkillsHitHarderAndCostLess()
        {
            yield return Zone(false);var idra=Lead("Idra");Place(idra,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            var body=Dummy(false,new Vector3(200,0,1.8f));yield return Frames(3);idra.target=body.Health;var hits=Watch(body,idra);
            var slag=Equip(idra,SkillKind.SlagHammer);float cost=SkillBook.Cost("Idra",slag);
            Assert.AreEqual(1,SkillBook.Mastery("Idra",slag.id),"fixture: a fresh skill");
            // What a cast costs is read as it's paid: the slam's landing gives some Charge back.
            float before=idra.charge;Assert.IsTrue(idra.Skill(0),"fixture: the slam");float paid=before-idra.charge;
            yield return Until(idra,()=>hits.count>0,2);Assert.AreEqual(1,hits.count,"fixture: the slam lands");
            float fresh=hits.each[0];
            for(int i=0;i<SkillBook.MasteryUses[0];i++)SkillBook.RecordUse("Idra",slag);
            Assert.AreEqual(2,SkillBook.Mastery("Idra",slag.id),"six more releases didn't raise her mastery");
            Assert.Less(SkillBook.Cost("Idra",slag),cost,"mastery didn't make her skill cheaper");
            for(int i=0;i<200;i++)SkillBook.RecordUse("Idra",slag);
            Assert.AreEqual(SkillBook.MaxMastery,SkillBook.Mastery("Idra",slag.id));
            yield return UntilFree(idra);idra.cooldowns[0]=0;idra.charge=100;
            before=idra.charge;Assert.IsTrue(idra.Skill(0),"fixture: the mastered slam");float mastered=before-idra.charge;
            yield return Until(idra,()=>hits.count>1,2);
            Assert.AreEqual(2,hits.count,"fixture: the mastered slam lands");
            Assert.AreEqual(fresh*SkillBook.Power(SkillBook.MaxMastery)/SkillBook.Power(1),hits.each[1],fresh*.06f,"a mastered slam doesn't hit harder by mastery's share");
            Assert.Less(mastered,paid-1,$"a mastered slam costs {mastered:0} Charge where a fresh one cost {paid:0}");
        }

        // ------------------------------------------------------------------ Take the Weight
        [UnityTest] public IEnumerator TakeTheWeightLandsHerPartnersHitsOnHerAndOnlyThoseThatWouldHaveLanded([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;var idra=Lead("Idra");var sela=Sela;Place(idra,new Vector3(200,y,0));Place(sela,new Vector3(205,y,0));
            var hound=Dummy(flight,new Vector3(203,y,4));yield return Frames(3);
            float bare=Take(sela,hound.Health,60);Assert.AreEqual(Plated(sela,60),bare,.6f,"fixture: an open hit on Sela");sela.Health.integrity=sela.Health.maximum;
            Equip(idra,SkillKind.TakeTheWeight);Assert.IsTrue(idra.Skill(0),"fixture: Take the Weight");
            yield return Until(idra,()=>WeightLine.Latest!=null,1);var line=WeightLine.Latest;
            Assert.IsNotNull(line,"Take the Weight put nothing between them");Assert.AreSame(sela,line.Covered,"it covers the wrong hero");Assert.IsTrue(line.Holds,"it doesn't hold four metres apart");
            // The hit meant for Sela lands on her, whole, through her own Plating.
            float hers=idra.Health.integrity;float landed=Take(sela,hound.Health,60);
            Assert.AreEqual(0,landed,.01f,"a hit on Sela still lands on Sela");
            Assert.AreEqual(Plated(idra,60),hers-idra.Health.integrity,.6f,"the hit doesn't land on Idra whole, through her Plating");
            Assert.Less(hers-idra.Health.integrity,bare,"she takes it no lighter than Sela did: her Plating isn't in it");
            Assert.AreEqual(1,line.Taken);
            // And through her brace, if it's up: the prow in flight, the hit coming from ahead of it.
            yield return UntilFree(idra);idra.Health.integrity=idra.Health.maximum;idra.Guard(true);yield return new WaitForSecondsRealtime(.4f);idra.thrust=100;
            hers=idra.Health.integrity;Take(sela,hound.Health,60);float braced=hers-idra.Health.integrity;idra.Guard(false);
            Assert.AreEqual(Plated(idra,60)*idra.Style.guardTakes,braced,.6f,"her brace doesn't stop what she takes for Sela");
            // A hit Sela would have avoided was hers to avoid: nothing comes to Idra. What nobody dealt is the place's, and stays Sela's.
            yield return new WaitForSecondsRealtime(.3f);hers=idra.Health.integrity;
            sela.Health.InvulnerableUntil=GameTime.Now+1;Assert.AreEqual(0,Take(sela,hound.Health,60),.01f,"fixture: Sela's untouchable");
            Assert.AreEqual(hers,idra.Health.integrity,.01f,"a hit Sela's roll took her out of landed on Idra instead");sela.Health.InvulnerableUntil=0;
            float before=sela.Health.integrity;sela.Health.Receive(new DamagePacket{amount=20,type=DamageType.Kinetic});
            Assert.Less(sela.Health.integrity,before,"what nobody dealt (the cold, the water) was taken off Sela");Assert.AreEqual(hers,idra.Health.integrity,.01f,"Idra took what nobody dealt");
            // Past its reach it hangs slack and covers nothing; back in reach it holds again.
            Place(sela,new Vector3(200+WeightLine.Reach+3,y,0));yield return Frames(2);
            Assert.IsFalse(line.Holds,"it holds past its reach");Assert.Greater(Take(sela,hound.Health,60),0,"past its reach a hit on Sela didn't land on Sela");
            Place(sela,new Vector3(205,y,0));yield return Frames(2);Assert.AreEqual(0,Take(sela,hound.Health,60),.01f,"back in reach it doesn't cover her again");
            // Six seconds, and then the hits are Sela's again, and nothing is left asking.
            yield return Until(idra,()=>WeightLine.Latest==null,WeightLine.Seconds+1);
            Assert.IsTrue(WeightLine.Latest==null,"it outlasts its six seconds");
            Assert.Greater(Take(sela,hound.Health,60),0,"with it gone a hit on Sela still doesn't land on her");
            Assert.IsNull(Health.Covering,"with no line holding, every hit on a hero still asks who's covering them");
        }

        // ------------------------------------------------------------------ Cutting Torch
        [UnityTest] public IEnumerator CuttingTorchIsSevenPlasmaTicksThatAPlatedFrontDoesNotStop([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;var idra=Lead("Idra");Place(idra,new Vector3(200,y,0));Place(Sela,new Vector3(260,y,0));
            var body=Dummy(flight,new Vector3(200,y,flight?4:1.8f));yield return Frames(3);idra.target=body.Health;
            // A guardian's front: a quarter of a blow gets through it (EnemyGuardian).
            DamagePacket Plain(CombatActor from)=>new DamagePacket{source=from.Health,amount=100,type=DamageType.Plasma};
            float open=body.Health.Receive(Plain(idra));body.Health.Plate=_=>.25f;
            Assert.AreEqual(open*.25f,body.Health.Receive(Plain(idra)),open*.02f,"fixture: the plate takes three quarters of a plain blow");
            var hits=Watch(body,idra);float broken=body.Health.BreakMeter;
            Equip(idra,SkillKind.CuttingTorch);Assert.IsTrue(idra.Skill(0),"fixture: the torch");
            // On foot she's planted: a stick pushed through the cut doesn't walk her out of it.
            yield return Drive(idra,.3f,flight?default:new Vector2(1,0));
            if(!flight){Assert.IsTrue(idra.Rooted,"she isn't planted for the cut");Assert.Less(Flat(idra.transform.position,new Vector3(200,0,0)),.3f,"a stick walked her out of the cut");}
            yield return Until(idra,()=>hits.count>=HeroSkills.TorchTicks,HeroSkills.TorchSeconds+1);yield return Drive(idra,.4f);
            Assert.AreEqual(HeroSkills.TorchTicks,hits.count,"the torch doesn't tick seven times");
            foreach(float each in hits.each)Assert.AreEqual(Output(idra)*HeroSkills.TorchEach,each,Output(idra)*.06f,"a tick of the torch was cut by the plate, or isn't its share of her Output");
            Assert.IsTrue(hits.weights.All(w=>w==HitWeight.Tick),"the torch's ticks land as heavier hits than ticks");
            Assert.GreaterOrEqual(body.Health.BreakMeter-broken,HeroSkills.TorchTicks*HeroSkills.TorchBreak*.99f,"the torch doesn't Break as much as it says");
            Assert.IsFalse(idra.Rooted,"she's still planted after the cut");
            // A roll lets go of it.
            yield return UntilFree(idra);idra.cooldowns[0]=0;idra.charge=100;int had=hits.count;
            Assert.IsTrue(idra.Skill(0),"fixture: the torch again");yield return Until(idra,()=>hits.count>=had+2,1);
            Assert.IsTrue(idra.Dodge(Vector3.left),"fixture: the roll");int at=hits.count;yield return Drive(idra,HeroSkills.TorchSeconds);
            Assert.LessOrEqual(hits.count,at,"the torch kept cutting after a roll let go of it");
            // Everyone else's blows still meet the plate.
            body.Health.Plate=null;float hers=body.Health.Receive(Plain(Sela));body.Health.Plate=_=>.25f;
            Assert.AreEqual(hers*.25f,body.Health.Receive(Plain(Sela)),hers*.02f,"the plate stopped stopping anyone's blows");
        }

        // ------------------------------------------------------------------ Bulkhead
        [UnityTest] public IEnumerator BulkheadStopsShotsAndHoldsOrdinaryBodiesBackButNotAHeavyOneOrAHerosShot([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;var idra=Lead("Idra");Place(idra,new Vector3(200,y,0));Place(Sela,new Vector3(260,y,0));
            float ahead=flight?HeroSkills.BulkheadFlightAhead:HeroSkills.BulkheadAhead;
            var under=Dummy(flight,new Vector3(200,y,ahead+.4f));var shooter=Dummy(flight,new Vector3(200,y,16));yield return Frames(3);idra.target=under.Health;var struck=Watch(under,idra);
            Equip(idra,SkillKind.Bulkhead);Assert.IsTrue(idra.Skill(0),"fixture: Bulkhead");
            yield return Until(idra,()=>BulkheadWall.Latest!=null,1.5f);var wall=BulkheadWall.Latest;Assert.IsNotNull(wall,"Bulkhead raised no wall");
            Assert.AreEqual(ahead,Flat(wall.transform.position,new Vector3(200,y,0)),.4f,"the wall doesn't stand where it should ahead of her");
            Assert.AreEqual(1,Vector3.Dot(wall.Normal,Vector3.forward),.01f,"the wall doesn't stand across her aim");
            Assert.AreEqual((flight?BulkheadWall.FlightWidth:BulkheadWall.Width)*.5f,wall.Half,.01f);
            Assert.IsFalse(wall.GetComponentsInChildren<Collider>().Any(c=>!c.isTrigger),"the wall is solid: it decides what it stops");
            // The slam strikes what's under it.
            Assert.AreEqual(1,struck.count,"the slam doesn't strike the body under the wall once");
            Assert.AreEqual(Output(idra)*HeroSkills.BulkheadHit,struck.each[0],Output(idra)*.06f,"the slam isn't its share of her Output");Assert.AreEqual(HitWeight.Heavy,struck.weights[0]);
            // Shots from beyond it burn out on it; she isn't touched.
            float hers=idra.Health.integrity;var muzzle=shooter.transform.position+Vector3.up*(flight?0:1);
            var shots=new[]{Shot(shooter,muzzle,idra.transform.position+Vector3.up*(flight?0:1)),Shot(shooter,muzzle,idra.transform.position+Vector3.right*2+Vector3.up*(flight?0:1))};
            yield return Until(idra,()=>wall.Stopped>=2,2.5f);
            Assert.AreEqual(2,wall.Stopped,"the wall didn't stop the shots that met it");foreach(var shot in shots)Assert.IsFalse(Projectile.Live.Contains(shot),"a stopped shot is still in the air");
            Assert.AreEqual(hers,idra.Health.integrity,.01f,"a shot came through the wall");
            // Her own shots go through it (to one side of the body under it).
            var mine=Projectile.Fire(idra.transform.position+Vector3.right*2.5f+Vector3.up*(flight?0:1),Vector3.forward,new DamagePacket{source=idra.Health,amount=1,type=DamageType.Kinetic},24,20);
            yield return Until(idra,()=>!Projectile.Live.Contains(mine)||mine.transform.position.z>wall.transform.position.z+1.5f,1);
            Assert.AreEqual(2,wall.Stopped,"the wall stopped one of the party's own shots");
            if(!flight)
            {
                // An ordinary body walking into it from beyond is held on its own side, however long it walks.
                Object.Destroy(under.gameObject);var walker=Enemy("Ridgehound",new Vector3(200,0,wall.transform.position.z+4));yield return Frames(3);
                for(float end=Time.unscaledTime+1.5f;Time.unscaledTime<end;){walker.Displace(Vector3.back*3*Time.deltaTime);yield return null;}
                Assert.GreaterOrEqual(walker.transform.position.z,wall.transform.position.z+BulkheadWall.Hold-.25f,"an ordinary body walked through the wall");
                Assert.GreaterOrEqual(wall.Held,1,"the wall says it held nobody");
                // A heavy body pushes through, slowed.
                var husk=Enemy("SentinelHusk",new Vector3(203,0,wall.transform.position.z+.3f));yield return Frames(3);var stood=husk.transform.position;
                Assert.Less(husk.Health.Pace,1,"a heavy body in the wall isn't slowed");Assert.Less(Flat(husk.transform.position,stood),.2f,"the wall shoved a heavy body");
            }
            // Six seconds and it's gone.
            yield return Until(idra,()=>BulkheadWall.Latest==null,BulkheadWall.Seconds+1);Assert.IsTrue(BulkheadWall.Latest==null,"the wall outlasts its six seconds");
        }

        // ------------------------------------------------------------------ the three, each from every slot, and the companion
        [UnityTest] public IEnumerator EachOfHerLateThreeWorksFromAnySlot([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;var idra=Lead("Idra");var sela=Sela;var failures=new List<string>();int turn=1;string form=flight?"in flight":"on foot";
            var released=new List<string>();void Logged(string text,string _,LogType __){if(text.StartsWith("SKILL_KIND Idra "))released.Add(text);}
            Application.logMessageReceived+=Logged;
            try
            {
                foreach(var kind in Late)
                {
                    int slot=turn++%SkillBook.Slots;yield return UntilFree(idra);
                    Place(idra,new Vector3(200,y,0));Place(sela,new Vector3(203,y,0));
                    var body=Dummy(flight,new Vector3(200,y,kind==SkillKind.CuttingTorch?(flight?4:1.8f):3.4f));yield return Frames(3);idra.target=body.Health;var hits=Watch(body,idra);
                    var skill=Equip(idra,kind,slot);
                    if(!idra.Skill(slot)){failures.Add($"{kind} won't start {form} from slot {slot+1} with full Charge");Object.Destroy(body.gameObject);continue;}
                    if(idra.charge>100-skill.chargeCost+.5f)failures.Add($"{kind} {form} didn't spend its {skill.chargeCost:0} Charge");
                    bool worked=false;
                    for(float end=Time.unscaledTime+3;!worked&&Time.unscaledTime<end;)
                    {idra.motor.Move(Vector2.zero,false,false);yield return null;worked=kind==SkillKind.TakeTheWeight?WeightLine.Latest!=null&&WeightLine.Latest.Holds:hits.count>0;}
                    if(!worked)failures.Add($"{kind} from slot {slot+1} {form} spends its Charge and does nothing");
                    yield return UntilFree(idra);Object.Destroy(body.gameObject);
                    if(WeightLine.Latest!=null)Object.Destroy(WeightLine.Latest.gameObject);if(BulkheadWall.Latest!=null)Object.Destroy(BulkheadWall.Latest.gameObject);yield return Frames(2);
                }
            }
            finally{Application.logMessageReceived-=Logged;}
            Assert.IsEmpty(failures,string.Join("\n",failures));
            foreach(var kind in Late)Assert.IsTrue(released.Any(l=>l.Contains(" "+kind+" ")),$"{kind} was never released {form}");
        }
        [UnityTest] public IEnumerator TheCompanionsRulesForHerLateThree()
        {
            yield return Zone(false);var sela=Lead("Sela");var idra=Idra;Place(sela,new Vector3(200,0,0));Place(idra,new Vector3(204,0,0));
            var body=Dummy(false,new Vector3(204,0,8));yield return Frames(3);sela.target=idra.target=body.Health;
            bool Refuses(SkillKind kind)=>LateSkills.Decides(idra,kind,out bool worth)&&!worth;
            bool Wants(SkillKind kind)=>LateSkills.Decides(idra,kind,out bool worth)&&worth;
            // Take the Weight: for a lead who's hurt and has something on them; not for one who's well, or alone, and not with her own Integrity low.
            Assert.IsTrue(Refuses(SkillKind.TakeTheWeight),"a companion would take the weight off a lead at full Integrity");
            Hurt(sela,.4f);Assert.IsTrue(Refuses(SkillKind.TakeTheWeight),"a companion would take the weight off a hurt lead with nothing near them");
            var on=Enemy("Ridgehound",sela.transform.position+Vector3.forward*3,false);on.Interrupt();yield return null;
            Assert.IsTrue(Wants(SkillKind.TakeTheWeight),"a companion wouldn't take the weight off a lead at 40% with a hound on them");
            Assert.IsTrue(IdraSkills.Urgent(idra,SkillKind.TakeTheWeight),"a lead being beaten waits the companion's turn");
            Hurt(idra,.3f);Assert.IsTrue(Refuses(SkillKind.TakeTheWeight),"a companion at 30% would take a lead's hits on herself");Hurt(idra,1);
            WeightLine.Take(idra);Assert.IsTrue(Refuses(SkillKind.TakeTheWeight),"a companion would cast it again with her line still holding");
            Object.Destroy(WeightLine.Latest.gameObject);Hurt(sela,1);yield return Frames(2);
            // Bulkhead: for a crowd on her or shots coming in, and not a second wall.
            Object.Destroy(on.gameObject);yield return Frames(2);
            var one=Enemy("Ridgehound",idra.transform.position+Vector3.forward*4,false);one.Interrupt();yield return null;
            Assert.IsTrue(Refuses(SkillKind.Bulkhead),"a companion would raise a wall for one hound");
            var two=Enemy("Ridgehound",idra.transform.position+Vector3.right*4,false);two.Interrupt();yield return null;
            Assert.IsTrue(Wants(SkillKind.Bulkhead),"a companion wouldn't raise a wall with two hounds on her");
            var wall=BulkheadWall.Raise(idra,idra.transform.position+Vector3.forward*3,Vector3.forward,false);
            Assert.IsTrue(Refuses(SkillKind.Bulkhead),"a companion would raise a second wall with one standing");
            Object.Destroy(wall.gameObject);Object.Destroy(one.gameObject);Object.Destroy(two.gameObject);yield return Frames(2);
            // The torch is a target's, like any strike; but she never plants for it in a marked danger.
            Place(idra,new Vector3(204,0,6.2f));idra.target=body.Health;
            Assert.IsFalse(LateSkills.Decides(idra,SkillKind.CuttingTorch,out _),"the rules stop a companion's torch on a body in front of her");
            Assert.LessOrEqual(Flat(idra.transform.position,body.transform.position),LateSkills.Reach(idra,SkillKind.CuttingTorch),"fixture: the body is in the torch's reach");
            var marking=Telegraph.Circle(idra.transform.position,4,30,Telegraph.Threat,true);marking.owner=body;
            Assert.IsTrue(Refuses(SkillKind.CuttingTorch),"a companion would plant herself for the torch inside a marked danger");marking.Clear();
        }

        // ------------------------------------------------------------------ her voice
        /// <summary>Every situation the game gives Taren a line for, she has one of her own (the writing is under "Idra" in docs/writing/barks.json, and the game reads
        /// its copy); and in a fight what she says is hers, never another hero's: low on Integrity, and her EXTRA's shout.</summary>
        [UnityTest] public IEnumerator InAFightSheSpeaksInHerOwnVoice()
        {
            foreach(var situation in new[]{"kill","flash","low","swap","cover","extra","teach.extra","teach.flash","teach.guard","boss","bossbraced","zone","zone.flight"})
            {
                string hers=BarkService.Line("Idra",situation);Assert.IsNotNull(hers,"Idra has no line for "+situation);
                Assert.AreNotEqual(BarkService.Line("Taren",situation),hers,"Idra says Taren's line for "+situation);Assert.AreNotEqual(BarkService.Line("Sela",situation),hers,"Idra says Sela's line for "+situation);
            }
            yield return Zone(false);GameServices.Current.Flags.SetBool(ExtraGauge.Flag,true);var idra=Lead("Idra");Place(idra,new Vector3(200,0,0));Place(Sela,new Vector3(212,0,0));
            var body=Dummy(false,new Vector3(200,0,5));yield return Frames(3);idra.target=body.Health;
            // A line said on arrival holds the next ordinary one back four seconds.
            yield return new WaitForSecondsRealtime(4.2f);said.Clear();
            Hurt(idra,.4f);idra.Health.Receive(new DamagePacket{source=body.Health,amount=idra.Health.maximum*.4f,type=DamageType.Kinetic});yield return Frames(2);
            Assert.Less(idra.Health.integrity,idra.Health.maximum*.3f,"fixture: the hit takes her under three tenths");Assert.IsTrue(idra.Health.Alive,"fixture: and doesn't put her down");
            Assert.IsTrue(said.Any(s=>s.who=="Idra"&&BarkService.Line("Idra","low")==s.line),"low on Integrity she says nothing of her own: "+string.Join(" | ",said.Select(s=>s.who+": "+s.line)));
            idra.Health.integrity=idra.Health.maximum;said.Clear();
            Member("Idra").extra=ExtraGauge.Full;Assert.IsTrue(idra.GetComponent<ExtraMove>().TryFire(),"fixture: Last One Out");yield return Frames(2);
            Assert.IsTrue(said.Any(s=>s.who=="Idra"&&BarkService.Line("Idra","extra")==s.line),"Last One Out goes off without her shout: "+string.Join(" | ",said.Select(s=>s.who+": "+s.line)));
            Assert.IsFalse(said.Any(s=>s.who!="Idra"&&s.line==BarkService.Line(s.who,"extra")),"another hero shouted her EXTRA");
            yield return Drive(idra,2.5f);
        }
    }
}
