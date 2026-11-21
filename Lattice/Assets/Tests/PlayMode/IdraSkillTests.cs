using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>S6a: Idra Sorn's eight skills and her EXTRA, on foot and in flight, and how she spends them as a companion. Each skill's own job and what its row promises in
    /// numbers; Shelter against shots and the lanes that carry them; the one skill in the game that raises a downed hero; a taunt that never turns a boss; Last One Out;
    /// and the companion's rules: she braces where the others roll, shelters a lead under fire, throws the flare when the lead is failing. She fights in a party of two,
    /// in Taren's place beside Sela.</summary>
    public sealed class IdraSkillTests
    {
        const int Level=IdraKitTests.Level;
        readonly List<string> log=new();
        void Logged(string text,string stack,LogType type){if(text.StartsWith("SKILL_KIND ")||text.StartsWith("EXTRA_")||text.StartsWith("FLARE_"))log.Add(text);}
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
        static CombatActor Idra=>PartyController.Current.members.First(m=>m.character=="Idra");
        static CombatActor Sela=>PartyController.Current.members.First(m=>m.character=="Sela");
        static MemberState Member(string id)=>GameServices.Current.State.party.Find(m=>m.id==id);
        static void Quiet(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        /// <summary>The arena with Idra in Taren's place beside Sela, both at the Sync she joins at (every one of her eight is known by then), on open floor.</summary>
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
        /// <summary>A body that never dies or breaks, and that Kinetic and Plasma are neither strong nor weak against. Her Pulse skills are asked of a body Beam is the odd one for.</summary>
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
        /// <summary>What a skill's multiple is a multiple of: her Output, by Resonance as every skill is. Mastery is 1 here.</summary>
        static float Output(CombatActor hero)=>hero.damage*(1+Mathf.Max(0,hero.resonance-10)*.025f);
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
        bool Cast(string hero,SkillKind kind)=>log.Any(l=>l.StartsWith($"SKILL_KIND {hero} ")&&l.Contains(" "+kind+" "));
        static Projectile Shot(EnemyBrain from,Vector3 at,Vector3 toward,float amount=30,float reach=40)
        {var aim=toward-at;aim.y=0;return Projectile.Fire(at,aim.normalized,new DamagePacket{source=from.Health,amount=amount,type=DamageType.Kinetic},11,reach);}
        /// <summary>Wreckage a tow line can take: it comes a step at a time toward whoever reels it.</summary>
        sealed class Wreck:MonoBehaviour,ITowable
        {
            public float towed;
            public bool CanTow=>true;
            public void Tow(Vector3 toward,float metres){transform.position+=toward*metres;towed+=metres;Physics.SyncTransforms();}
        }
        static Wreck Wreckage(Vector3 at)
        {var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name="Tow fixture wreck";cube.transform.position=at;cube.GetComponent<Collider>().isTrigger=true;Physics.SyncTransforms();return cube.AddComponent<Wreck>();}

        // ------------------------------------------------------------------ the table
        static readonly SkillKind[] Eight={SkillKind.Breach,SkillKind.TowLine,SkillKind.Shelter,SkillKind.SlagHammer,SkillKind.RescueFlare,SkillKind.Counterweight,SkillKind.Avalanche,SkillKind.HoldTheLine};
        [UnityTest] public IEnumerator EveryOneOfHerSkillsDoesItsWork([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;var failures=new List<string>();int turn=0;string form=flight?"in flight":"on foot";
            var idra=Lead("Idra");var sela=Sela;var kit=idra.GetComponent<BraceKit>();
            foreach(var kind in Eight)
            {
                int slot=turn++%SkillBook.Slots;yield return UntilFree(idra);
                Place(idra,new Vector3(200,y,0));Place(sela,new Vector3(flight?206:203,y,-3));
                float distance=kind switch{SkillKind.Breach=>flight?7:5,SkillKind.TowLine=>8,SkillKind.SlagHammer=>flight?4.5f:1.8f,SkillKind.Avalanche=>flight?5:4,_=>6};
                var body=Dummy(flight,new Vector3(200,y,distance));yield return Frames(3);idra.target=body.Health;var hits=Watch(body,idra);
                Hurt(sela,.5f);float hers=sela.Health.integrity;
                var skill=Equip(idra,kind,slot);
                if(!idra.Skill(slot)){failures.Add($"{kind} won't start {form} with full Charge");Object.Destroy(body.gameObject);continue;}
                if(idra.charge>100-skill.chargeCost+.5f)failures.Add($"{kind} {form} didn't spend its {skill.chargeCost:0} Charge");
                bool worked=false;
                for(float end=Time.unscaledTime+4;!worked&&Time.unscaledTime<end;)
                {
                    idra.motor.Move(Vector2.zero,false,false);yield return null;
                    worked=kind switch{SkillKind.Shelter=>ShelterDome.Latest!=null,SkillKind.RescueFlare=>sela.Health.integrity>hers,SkillKind.Counterweight=>kit.Counterweights==BraceKit.CounterweightHits,
                        SkillKind.HoldTheLine=>kit.Holding,_=>hits.count>0};
                }
                if(!worked)failures.Add($"{kind} from slot {slot+1} {form} spends its Charge and does nothing");
                yield return UntilFree(idra);Object.Destroy(body.gameObject);if(ShelterDome.Latest!=null)Object.Destroy(ShelterDome.Latest.gameObject);yield return Frames(2);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
            foreach(var kind in Eight)Assert.IsTrue(Cast("Idra",kind),$"{kind} was never released {form}");
        }

        // ------------------------------------------------------------------ Breach
        [UnityTest] public IEnumerator BreachGoesThroughALineBehindHerBraceStrikingEachBodyOnce([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;var idra=Lead("Idra");Place(idra,new Vector3(200,y,0));Place(Sela,new Vector3(260,y,0));
            var near=Dummy(flight,new Vector3(200,y,flight?4:3));var far=Dummy(flight,new Vector3(200,y,flight?7.5f:5.5f));yield return Frames(3);idra.target=near.Health;
            var first=Watch(near,idra);var second=Watch(far,idra);
            float bare=Take(idra,near.Health,60);Assert.Greater(bare,0,"fixture: a hit on her lands");yield return new WaitForSecondsRealtime(.4f);
            Equip(idra,SkillKind.Breach);Assert.IsTrue(idra.Skill(0),"fixture: the breach");
            Assert.IsTrue(idra.Armored,"she isn't braced as the breach begins");
            float thrust=idra.thrust;float braced=Take(idra,near.Health,60);
            Assert.AreEqual(bare*CombatActor.GuardTakes,braced,bare*.04f,"a hit on the way through doesn't land as on a guard");Assert.GreaterOrEqual(idra.thrust,thrust-.01f,"the breach's brace is paid for in Thrust");
            yield return Drive(idra,1);
            Assert.AreEqual(1,first.count,"the breach doesn't strike the first body on its line once");Assert.AreEqual(1,second.count,"the breach stops at the first body: it's meant to go through a line");
            Assert.AreEqual(Output(idra)*HeroSkills.BreachHit,first.each[0],Output(idra)*.07f,"the breach isn't twice her Output");
            Assert.AreEqual(HitWeight.Heavy,first.weights[0],"the breach doesn't land as a heavy hit");
            Assert.Greater(idra.transform.position.z,flight?7.5f:5.5f,"she was stopped by the first small body she met");
            Assert.IsFalse(idra.Armored,"the brace outlasts the breach");
            // And with nothing in the way it goes its whole length.
            yield return UntilFree(idra);Object.Destroy(near.gameObject);Object.Destroy(far.gameObject);yield return Frames(2);
            Place(idra,new Vector3(200,y,0));idra.cooldowns[0]=0;idra.charge=100;Assert.IsTrue(idra.Skill(0),"fixture: the breach again");
            // On foot she stops where it ends. A ship's burn is the same quarter second and then it coasts, as every ship's dash does: it's measured as the burn ends.
            yield return Drive(idra,flight?.25f:1);
            float gone=Flat(idra.transform.position,new Vector3(200,y,0)),length=flight?HeroSkills.BreachFlightLength:HeroSkills.BreachLength;
            Assert.Greater(gone,length-1.2f,"with nothing in the way the breach doesn't go its length");Assert.Less(gone,length+(flight?3:1.2f),"the breach overshoots its length");
        }

        // ------------------------------------------------------------------ Tow Line
        [UnityTest] public IEnumerator TowLineReelsALightBodyInPullsHerToAHeavyOneAndDragsWreckage([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;var idra=Lead("Idra");var home=new Vector3(200,y,0);Place(idra,home);Place(Sela,new Vector3(260,y,0));
            // A light body 9 m off comes to her; she stays where she is.
            var light=Dummy(flight,new Vector3(200,y,9),DamageType.Beam);yield return Frames(3);idra.target=light.Health;var hits=Watch(light,idra);
            Equip(idra,SkillKind.TowLine);Assert.IsTrue(idra.Skill(0),"fixture: the tow line");
            yield return Until(idra,()=>Flat(light.transform.position,idra.transform.position)<3,2);yield return Drive(idra,.3f);
            Assert.AreEqual(1,hits.count,"the line didn't strike what it took hold of, once");
            Assert.AreEqual(Output(idra)*HeroSkills.TowHit,hits.each[0],Output(idra)*.07f,"the line's hit isn't its share of her Output");
            Assert.Less(Flat(light.transform.position,idra.transform.position),3,"a light body 9 m off wasn't reeled in to her");
            Assert.Less(Flat(idra.transform.position,home),1,"reeling a light body in moved her instead");
            Assert.IsTrue(light.Health.Broken,"what she reels in isn't left open for a moment");
            Object.Destroy(light.gameObject);yield return UntilFree(idra);yield return Frames(2);
            // A heavy body holds its ground, and the line pulls her to it. On foot that's a husk; a ship's is a body held fast.
            Place(idra,home);var heavy=Enemy(flight?"Shellmine":"SentinelHusk",new Vector3(200,y,9));heavy.Health.maximum=heavy.Health.integrity=1000000;heavy.Health.breakThreshold=1e9f;
            yield return Frames(3);idra.target=heavy.Health;var stood=heavy.transform.position;
            if(!flight)
            {
                idra.cooldowns[0]=0;idra.charge=100;Assert.IsTrue(idra.Skill(0),"fixture: the line at a husk");
                yield return Until(idra,()=>Flat(idra.transform.position,heavy.transform.position)<3.2f,2);yield return Drive(idra,.3f);
                Assert.Less(Flat(idra.transform.position,heavy.transform.position),3.2f,"the line on a body too heavy to reel didn't pull her to it");
                Assert.Less(Flat(heavy.transform.position,stood),.5f,"the line dragged a heavy body");
            }
            else
            {
                // A mine is only struck: nobody is moved toward a thing that goes off.
                idra.cooldowns[0]=0;idra.charge=100;Assert.IsTrue(idra.Skill(0),"fixture: the line at a mine");yield return Drive(idra,1);
                Assert.Less(Flat(idra.transform.position,home),1.5f,"the line pulled her ship onto a mine");Assert.Less(Flat(heavy.transform.position,stood),.5f,"the line reeled a mine in to her");
            }
            Object.Destroy(heavy.gameObject);yield return UntilFree(idra);yield return Frames(2);
            // Wreckage: dragged toward her for as long as the reel runs.
            Place(idra,home);var wreck=Wreckage(new Vector3(200,y+(flight?0:.9f),8));yield return Frames(2);
            idra.cooldowns[0]=0;idra.charge=100;Assert.IsTrue(idra.Skill(0),"fixture: the line at wreckage");yield return Drive(idra,1.4f);
            Assert.Greater(wreck.towed,2,"the line took hold of wreckage and didn't drag it");
            Assert.Less(Flat(wreck.transform.position,home),6,"the wreckage wasn't dragged toward her");Assert.Less(Flat(idra.transform.position,home),1,"dragging wreckage moved her");
            // And a wall stops the line, as it stops a hook.
            Object.Destroy(wreck.gameObject);yield return UntilFree(idra);yield return Frames(2);
            Place(idra,home);var hidden=Dummy(flight,new Vector3(200,y,9),DamageType.Beam);var wall=new GameObject("Tow fixture wall",typeof(BoxCollider));wall.transform.position=new Vector3(200,y+1,5);wall.GetComponent<BoxCollider>().size=new Vector3(8,6,.6f);Physics.SyncTransforms();
            yield return Frames(3);var behind=Watch(hidden,idra);var where=hidden.transform.position;
            idra.cooldowns[0]=0;idra.charge=100;Assert.IsTrue(idra.Skill(0),"fixture: the line at a wall");yield return Drive(idra,1.2f);
            Assert.AreEqual(0,behind.count,"the line struck a body behind a wall");Assert.Less(Flat(hidden.transform.position,where),.3f,"the line reeled a body through a wall");
        }

        // ------------------------------------------------------------------ Shelter
        [UnityTest] public IEnumerator ShelterStopsEnemyShotsAtItsEdgeAndTheLanesThatCarryThemGoToo([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;var idra=Lead("Idra");var sela=Sela;Place(idra,new Vector3(200,y,0));Place(sela,new Vector3(204,y,0));
            var shooter=Dummy(flight,new Vector3(202,y,16));yield return Frames(3);
            Vector3 muzzle=shooter.transform.position+Vector3.up*(flight?0:1);float lift=flight?0:1;
            // With no dome a shot at each of them lands.
            float hers=idra.Health.integrity,partner=sela.Health.integrity;
            Shot(shooter,muzzle,idra.transform.position+Vector3.up*lift);Shot(shooter,muzzle,sela.transform.position+Vector3.up*lift);
            yield return Until(idra,()=>idra.Health.integrity<hers&&sela.Health.integrity<partner,2.5f);
            Assert.Less(idra.Health.integrity,hers,"fixture: a shot at her lands");Assert.Less(sela.Health.integrity,partner,"fixture: a shot at Sela lands");
            // The dome stands between the two of them and covers both.
            Equip(idra,SkillKind.Shelter);Assert.IsTrue(idra.Skill(0),"fixture: Shelter");
            yield return Until(idra,()=>ShelterDome.Latest!=null,1);var dome=ShelterDome.Latest;Assert.IsNotNull(dome,"Shelter raised no dome");
            Assert.AreEqual(flight?ShelterDome.FlightRadius:ShelterDome.Radius,dome.Reach,.01f);
            Assert.IsTrue(dome.Covers(idra.transform.position)&&dome.Covers(sela.transform.position),"the dome doesn't stand over both heroes four metres apart");
            hers=idra.Health.integrity;partner=sela.Health.integrity;
            // A volley of three down a marked lane, as a shooter fires one: none arrives, and the lane goes with its shots rather than lying on under the dome.
            var aim=idra.transform.position-shooter.transform.position;aim.y=0;
            var shots=new[]{Shot(shooter,muzzle,idra.transform.position+Vector3.up*lift),Shot(shooter,muzzle,sela.transform.position+Vector3.up*lift),Shot(shooter,muzzle,(idra.transform.position+sela.transform.position)*.5f+Vector3.up*lift)};
            var lane=Telegraph.Line(shooter.transform.position,aim.normalized,20,1.2f,.4f,Telegraph.Threat,true);lane.owner=shooter;lane.Carry(shots);
            yield return Until(idra,()=>dome.Stopped>=3,2.5f);
            Assert.AreEqual(3,dome.Stopped,"the dome didn't stop each shot of a volley at its edge");
            foreach(var shot in shots)Assert.IsFalse(Projectile.Live.Contains(shot),"a stopped shot is still in the air");
            Assert.AreEqual(hers,idra.Health.integrity,.01f,"a shot reached her under the dome");Assert.AreEqual(partner,sela.Health.integrity,.01f,"a shot reached Sela under the dome");
            yield return new WaitForSecondsRealtime(.4f);
            Assert.IsTrue(lane==null,"a lane whose shots the dome stopped is still marked on the floor: its danger is over");
            // The party's own shots pass, and so does a body: it stops shots, and only the enemy's.
            var out_=Projectile.Fire(idra.transform.position+Vector3.up*lift,Vector3.forward,new DamagePacket{source=idra.Health,amount=10,type=DamageType.Kinetic},24,30);
            yield return Until(idra,()=>out_.transform.position.z>dome.transform.position.z+dome.Reach+1||!Projectile.Live.Contains(out_),1.5f);
            Assert.AreEqual(3,dome.Stopped,"the dome stopped one of the party's own shots");
            Assert.IsFalse(dome.GetComponentsInChildren<Collider>().Any(c=>!c.isTrigger),"the dome is solid: nothing but a shot may be stopped by it");
            // A shot fired from under it is somebody's to dodge.
            var inside=Shot(shooter,(idra.transform.position+sela.transform.position)*.5f+Vector3.up*lift+Vector3.forward*2,idra.transform.position+Vector3.up*lift);
            yield return Until(idra,()=>idra.Health.integrity<hers,1.5f);
            Assert.Less(idra.Health.integrity,hers,"a shot fired from inside the dome was stopped: it stops what reaches it from outside");
            // It lasts its five seconds and no longer.
            yield return Until(idra,()=>ShelterDome.Latest==null,ShelterDome.Seconds+1);
            Assert.IsTrue(ShelterDome.Latest==null,"the dome outlasts its five seconds");
            hers=idra.Health.integrity;Shot(shooter,muzzle,idra.transform.position+Vector3.up*lift);yield return Until(idra,()=>idra.Health.integrity<hers,2.5f);
            Assert.Less(idra.Health.integrity,hers,"with the dome gone a shot still doesn't land");
        }

        // ------------------------------------------------------------------ Slag Hammer
        [UnityTest] public IEnumerator SlagHammerIsAPlantedOverheadSlamThatBreaksWhatItLandsOn([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;var idra=Lead("Idra");Place(idra,new Vector3(200,y,0));Place(Sela,new Vector3(260,y,0));
            var body=Enemy(flight?"ChoristerDart":"Ridgehound",new Vector3(200,y,flight?4:1.8f));body.Health.maximum=body.Health.integrity=1000000;body.Health.weakness=body.Health.resistance=DamageType.Pulse;
            var beyond=Dummy(flight,new Vector3(200,y,flight?10:6.5f));yield return Frames(3);idra.target=body.Health;var hits=Watch(body,idra);var far=Watch(beyond,idra);
            Equip(idra,SkillKind.SlagHammer);Assert.IsTrue(idra.Skill(0),"fixture: the slam");
            // On foot the stick is pushed through the wind-up: planted means planted. A ship isn't planted, and its slam leaves along the nose.
            yield return Drive(idra,.12f,flight?default:new Vector2(1,0));
            Assert.AreEqual(0,hits.count,"the slam lands as it's pressed: it's a wind-up");
            if(!flight){Assert.IsTrue(idra.Rooted,"she isn't planted for the wind-up");Assert.Less(Flat(idra.transform.position,new Vector3(200,0,0)),.3f,"a stick walked her out of the wind-up");}
            yield return Until(idra,()=>hits.count>0,1.5f);
            Assert.AreEqual(1,hits.count,"the slam didn't land once on the body in front of her");
            Assert.AreEqual(Output(idra)*HeroSkills.SlagHit,hits.each[0],Output(idra)*.1f,"the slam isn't 2.8 of her Output");
            Assert.AreEqual(HitWeight.Finisher,hits.weights[0],"the slam doesn't land as the heaviest hit she has");
            Assert.IsTrue(body.Health.Broken,"the slam's Break doesn't open a body a Pulse Burst opens");
            Assert.AreEqual(0,far.count,"the slam reached a body well past its ring");
            Assert.Greater(HeroSkills.SlagBreak,HeroSkills.HammerBreak[2],"the slam breaks no more than a plain third swing");
        }

        // ------------------------------------------------------------------ Rescue Flare
        [UnityTest] public IEnumerator RescueFlareMendsHerPartnerAndRaisesOneWhoIsDownFromAcrossTheFight([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;var idra=Lead("Idra");var sela=Sela;Place(idra,new Vector3(200,y,0));Place(sela,new Vector3(200,y,20));
            var body=Dummy(flight,new Vector3(190,y,6));yield return Frames(3);idra.target=body.Health;
            var flare=Equip(idra,SkillKind.RescueFlare);
            // The only skill that raises is the dearest to keep ready: no cooldown in the game is longer.
            foreach(string hero in new[]{"Taren","Sela","Idra"})foreach(var other in SkillBook.All(hero))Assert.LessOrEqual(other.cooldown,flare.cooldown,$"{other.displayName} waits longer than the Rescue Flare");
            // Standing, twenty metres off: a quarter of her Integrity back.
            Hurt(sela,.4f);float was=sela.Health.integrity;
            Assert.IsTrue(idra.Skill(0),"fixture: the flare");yield return Until(idra,()=>sela.Health.integrity>was,2);
            Assert.AreEqual(was+sela.Health.maximum*RescueFlare.Mend,sela.Health.integrity,1,"the flare didn't mend a partner 20 m off by a quarter of her Integrity");
            yield return UntilFree(idra);
            // Down, twenty metres off, nobody standing over her: raised.
            sela.Health.Receive(new DamagePacket{amount=sela.Health.maximum*10});yield return new WaitForSecondsRealtime(.3f);
            Assert.IsFalse(sela.Health.Alive,"fixture: her partner went down");Assert.Greater(Flat(idra.transform.position,sela.transform.position),15,"fixture: she's nowhere near her");
            idra.cooldowns[0]=0;idra.charge=100;Assert.IsTrue(idra.Skill(0),"fixture: the flare at a downed partner");
            yield return Until(idra,()=>sela.Health.Alive,2);
            Assert.IsTrue(sela.Health.Alive,"the Rescue Flare didn't raise a downed partner from 20 m: it's the one skill that does");
            Assert.AreEqual(sela.Health.maximum*RescueFlare.Raise,sela.Health.integrity,1,"she isn't raised to 35% of her Integrity");
            Assert.IsTrue(log.Any(l=>l.StartsWith("FLARE_RAISED Sela")),"the raise isn't logged");
            Assert.Greater(Flat(idra.transform.position,sela.transform.position),15,"fixture: it was the flare, not standing over her");
            yield return UntilFree(idra);yield return new WaitForSecondsRealtime(.5f);
            // Out of its reach there's nobody to throw it to: it mends her a little instead.
            Place(sela,new Vector3(200,y,RescueFlare.Reach+12));Hurt(sela,.4f);Hurt(idra,.5f);was=sela.Health.integrity;float mine=idra.Health.integrity;
            idra.cooldowns[0]=0;idra.charge=100;Assert.IsTrue(idra.Skill(0),"fixture: the flare with nobody in reach");
            yield return Until(idra,()=>idra.Health.integrity>mine,2);
            Assert.AreEqual(mine+idra.Health.maximum*RescueFlare.Self,idra.Health.integrity,1,"alone, the flare doesn't mend her its 15%");
            Assert.AreEqual(was,sela.Health.integrity,.01f,"the flare reached a partner past its thirty metres");
        }

        // ------------------------------------------------------------------ Counterweight
        [UnityTest] public IEnumerator CounterweightAnswersHerNextThreeGuardedHitsAndNoMore()
        {
            yield return Zone(false);var idra=Lead("Idra");Place(idra,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            var body=Dummy(false,new Vector3(202.5f,0,0));var beyond=Dummy(false,new Vector3(200,0,7));yield return Frames(3);idra.target=body.Health;
            var hits=Watch(body,idra);var far=Watch(beyond,idra);var kit=idra.GetComponent<BraceKit>();float hit=idra.Health.maximum*.05f;
            // Unarmed, a held guard answers nothing.
            idra.Guard(true);yield return new WaitForSecondsRealtime(.5f);Take(idra,body.Health,hit);yield return Frames(2);
            Assert.AreEqual(0,hits.count,"a held guard answered a hit with no Counterweight armed");idra.Guard(false);yield return Frames(2);
            Equip(idra,SkillKind.Counterweight);Assert.IsTrue(idra.Skill(0),"fixture: Counterweight");
            Assert.AreEqual(BraceKit.CounterweightHits,kit.Counterweights,"Counterweight didn't arm three answers");
            yield return UntilFree(idra);
            // An unguarded hit spends none of them.
            Take(idra,body.Health,hit);yield return Frames(2);
            Assert.AreEqual(0,hits.count,"an unguarded hit was answered");Assert.AreEqual(3,kit.Counterweights,"an unguarded hit spent an answer");
            idra.Guard(true);for(float end=Time.unscaledTime+.5f;Time.unscaledTime<end;){idra.Guard(true);yield return null;}
            for(int i=1;i<=3;i++)
            {
                idra.thrust=100;float taken=Take(idra,body.Health,hit);Assert.Greater(taken,0,"fixture: a held guard, not a perfect one");
                yield return Frames(2);
                Assert.AreEqual(i,hits.count,$"guarded hit {i} wasn't answered with one shockwave");
                Assert.AreEqual(idra.damage*BraceKit.CounterweightHit,hits.each[i-1],idra.damage*.06f,"a Counterweight's answer isn't 1.2 of her Output");
                Assert.AreEqual(3-i,kit.Counterweights,"an answer wasn't spent");
                for(float end=Time.unscaledTime+.25f;Time.unscaledTime<end;){idra.Guard(true);yield return null;}
            }
            idra.thrust=100;Take(idra,body.Health,hit);yield return Frames(2);
            Assert.AreEqual(3,hits.count,"a fourth guarded hit was answered: Counterweight is three");
            Assert.AreEqual(0,far.count,$"an answer reached a body 7 m off: it's {BraceKit.CounterweightReach} m");
            Assert.AreEqual(0,idra.FlashGuards,"fixture: none of these was a perfect guard");
            idra.Guard(false);
        }

        // ------------------------------------------------------------------ Avalanche
        [UnityTest] public IEnumerator AvalancheIsThreeLeapingSlamsTheLastTheHeaviest([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;var idra=Lead("Idra");Place(idra,new Vector3(200,y,0));Place(Sela,new Vector3(260,y,0));
            var body=Dummy(flight,new Vector3(200,y,flight?6:4.5f));yield return Frames(3);idra.target=body.Health;var hits=Watch(body,idra);
            Equip(idra,SkillKind.Avalanche);Assert.IsTrue(idra.Skill(0),"fixture: Avalanche");
            yield return Drive(idra,.1f);Assert.IsTrue(idra.Armored,"she isn't braced through the leaps");
            yield return Until(idra,()=>hits.count>=3,3);yield return Drive(idra,.2f);
            Assert.AreEqual(3,hits.count,"three slams didn't each land on a body ahead of her");
            for(int beat=0;beat<3;beat++)Assert.AreEqual(Output(idra)*HeroSkills.AvalancheHit[beat],hits.each[beat],Output(idra)*.1f,$"slam {beat+1} isn't {HeroSkills.AvalancheHit[beat]} of her Output");
            Assert.AreEqual(HitWeight.Finisher,hits.weights[2],"the last slam isn't the heaviest");Assert.Greater(hits.each[2],hits.each[0]*1.5f,"the last slam is no heavier than the first");
            Assert.Greater(idra.transform.position.z,1.5f,"she didn't leap");
            // The brace runs on her own clock, which the slams' hit-stops hold: a moment past the last of them, not a wall-clock instant.
            yield return UntilFree(idra);yield return Until(idra,()=>!idra.Armored,1);Assert.IsFalse(idra.Armored,"the brace outlasts the slams");
            // A roll lets go of it: she's a hero again, not a ride.
            Object.Destroy(body.gameObject);yield return Frames(2);Place(idra,new Vector3(200,y,0));var open=Dummy(flight,new Vector3(200,y,flight?14:12));yield return Frames(2);var none=Watch(open,idra);
            idra.cooldowns[0]=0;idra.charge=100;idra.thrust=100;Assert.IsTrue(idra.Skill(0),"fixture: Avalanche again");yield return Drive(idra,.15f);
            Assert.IsTrue(idra.Dodge(Vector3.right),"she can't hop out of Avalanche");yield return Drive(idra,1.6f);
            Assert.Less(idra.transform.position.z,4,"a hop didn't stop the leaps");Assert.AreEqual(0,none.count);
        }

        // ------------------------------------------------------------------ Hold the Line
        [UnityTest] public IEnumerator HoldTheLineTurnsOrdinaryEnemiesOnHerRaisesHerPlatingAndNeverTurnsABoss()
        {
            yield return Zone(false);var sela=Lead("Sela");var idra=Idra;Place(sela,new Vector3(200,0,0));Place(idra,new Vector3(208,0,0));
            sela.Health.InvulnerableUntil=idra.Health.InvulnerableUntil=GameTime.Now+60;
            var hound=Enemy("Ridgehound",new Vector3(204,0,9),false);hound.Health.maximum=hound.Health.integrity=1000000;hound.Health.breakThreshold=1e9f;
            var asBoss=Dummy(false,new Vector3(204,0,-9));asBoss.definition=GameCatalog.Find<EnemyDef>("Burrower");Assert.IsTrue(asBoss.definition.boss,"fixture: a boss's definition");
            var kit=idra.GetComponent<BraceKit>();
            // Before it's cast, the hound is the lead's.
            yield return new WaitForSecondsRealtime(.8f);
            Assert.IsNull(BraceKit.Holder(hound),"fixture: nobody holds the line yet");
            Assert.Less(Flat(hound.transform.position,sela.transform.position),Flat(hound.transform.position,idra.transform.position)+.5f,"fixture: the hound makes for the hero the player is playing");
            float bare=idra.plating;idra.Health.InvulnerableUntil=0;float open=Take(idra,asBoss.Health,60);idra.Health.integrity=idra.Health.maximum;idra.Health.InvulnerableUntil=GameTime.Now+60;
            Equip(idra,SkillKind.HoldTheLine);Assert.IsTrue(idra.Skill(0),"fixture: Hold the Line");
            Assert.IsTrue(kit.Holding,"she isn't holding the line");
            Assert.AreSame(idra,BraceKit.Holder(hound),"an ordinary enemy near her doesn't turn to her");
            Assert.IsNull(BraceKit.Holder(asBoss),"Hold the Line turns a boss: a boss's specials are aimed at the hero the player is playing");
            Assert.AreEqual(bare+BraceKit.HoldPlating,idra.Plating,.01f,"her Plating doesn't rise while she holds");
            idra.Health.InvulnerableUntil=0;float held=Take(idra,asBoss.Health,60);idra.Health.integrity=idra.Health.maximum;idra.Health.InvulnerableUntil=GameTime.Now+60;
            Assert.Less(held,open*.85f,"a hit on her lands as hard as before she set her feet");
            // It comes for her: across the lead and on, to where she stands.
            yield return Until(idra,()=>Flat(hound.transform.position,idra.transform.position)<4,3.5f);
            Assert.Less(Flat(hound.transform.position,idra.transform.position),Flat(hound.transform.position,sela.transform.position),"the hound still makes for the lead while she holds the line");
            // A body out of the line's reach keeps to the lead.
            var distant=Enemy("Ridgehound",idra.transform.position+Vector3.right*(BraceKit.HoldReach+6),true);yield return Frames(2);
            Assert.IsNull(BraceKit.Holder(distant),"a body past the line's reach turns to her");
            // Six seconds, and the fight is the lead's again.
            yield return Until(idra,()=>!kit.Holding,BraceKit.HoldSeconds+1);
            Assert.IsFalse(kit.Holding,"she holds the line past its six seconds");Assert.IsNull(BraceKit.Holder(hound),"the hound is still hers after the line is let go");
            Assert.AreEqual(bare,idra.Plating,.01f,"the Plating it lent outlasts it");
        }

        // ------------------------------------------------------------------ Last One Out
        [UnityTest] public IEnumerator LastOneOutIsNineOfHerOutputARaisedPartnerAndNothingTouchesHer([Values(false,true)] bool flight)
        {
            yield return Zone(flight);float y=flight?1:0;GameServices.Current.Flags.SetBool(ExtraGauge.Flag,true);
            var idra=Lead("Idra");var sela=Sela;Place(idra,new Vector3(200,y,0));Place(sela,new Vector3(212,y,0));
            // One on the line she charges down, one where the charge ends, and one off to the side of both.
            var onLine=Dummy(flight,new Vector3(200,y,flight?8:6));var aside=Dummy(flight,new Vector3(flight?240:225,y,0));yield return Frames(3);idra.target=onLine.Health;
            var first=Watch(onLine,idra);var none=Watch(aside,idra);
            sela.Health.Receive(new DamagePacket{amount=sela.Health.maximum*10});yield return new WaitForSecondsRealtime(.3f);Assert.IsFalse(sela.Health.Alive,"fixture: her partner is down");
            var move=idra.GetComponent<ExtraMove>();Assert.IsNotNull(move.Definition,"she has no EXTRA: run Lattice.EditorTools.IdraBuilder.Build");Assert.AreEqual(SkillKind.LastOneOut,move.Definition.kind);
            var cut=Object.FindFirstObjectByType<ExtraCutIn>();Assert.IsNotNull(cut,"there's no cut-in to play");
            Member("Idra").extra=ExtraGauge.Full;CombatActor began=null;SkillDef named=null;void Began(CombatActor who,SkillDef what){began=who;named=what;}
            ExtraMove.Began+=Began;try{Assert.IsTrue(move.TryFire(),"a full gauge, a fight, a hero on her feet: Last One Out didn't start");}finally{ExtraMove.Began-=Began;}
            Assert.AreSame(idra,began,"the cut-in wasn't asked for");Assert.AreEqual(SkillKind.LastOneOut,named.kind);
            // The cut-in plays with her name on it whether or not her portrait is made yet.
            yield return Drive(idra,.25f);
            Assert.IsTrue(cut.Showing,"no cut-in is on the screen");StringAssert.AreEqualIgnoringCase("Last One Out",cut.Title,"the cut-in doesn't name the move");
            bool drawn=Lattice.Dialogue.PortraitLookup.Get("Idra",BodyForm.Shaped,PortraitEmotion.Determined)!=null;
            Assert.AreEqual(drawn,cut.Portrait!=null,drawn?"her Braced portrait exists and the cut-in doesn't use it":"with no portrait made, the cut-in shows somebody else's face");
            Assert.AreEqual(0,first.count,"the move struck during its cut-in");
            Assert.AreEqual(0,Take(idra,onLine.Health,50),.0001f,"she can be hurt during the cut-in");
            for(float end=Time.unscaledTime+5;move.Performing&&Time.unscaledTime<end;){idra.motor.Move(Vector2.zero,false,false);yield return null;}
            Assert.IsFalse(move.Performing,"the move never ended");
            Assert.AreEqual(9,ExtraMove.OutTotal,.001f,"Last One Out isn't meant to be nine");
            Assert.AreEqual(2,first.count,"the charge and the shockwave didn't each land once on a body on her line");
            Assert.AreEqual(Output(idra)*ExtraMove.OutCharge,first.each[0],Output(idra)*.2f,"the charge isn't four of her Output");
            Assert.AreEqual(Output(idra)*ExtraMove.OutShock,first.each[1],Output(idra)*.25f,"the shockwave isn't five of her Output");
            Assert.IsTrue(first.tags.All(t=>t==ExtraMove.Tag),"the move's hits aren't an EXTRA's");
            Assert.AreEqual(0,none.count,"the move reached a body far off to one side");
            Assert.Greater(idra.transform.position.z,flight?9:7,"the charge was stopped by the first small body on its line");
            Assert.IsTrue(sela.Health.Alive,"Last One Out left her partner down");
            Assert.AreEqual(sela.Health.maximum*ExtraMove.OutHeal,sela.Health.integrity,1,"her partner isn't raised to half her Integrity");
            Assert.IsTrue(log.Any(l=>l.StartsWith("EXTRA_RAISED Sela")),"the raise isn't logged");Assert.IsTrue(log.Any(l=>l.StartsWith("EXTRA_OK Idra")),"the move didn't finish as one");
            Assert.AreEqual(0,ExtraGauge.Of("Idra"),.0001f,"the move's own hits refilled its gauge");
            yield return Drive(idra,.2f);
            Assert.Greater(Take(idra,onLine.Health,50),0,"she's still untouchable after the move");Assert.IsFalse(idra.Rooted||idra.Committed,"the move's holds outlast it");
            // A partner on her feet gets the half as a mend.
            Hurt(sela,.3f);float was=sela.Health.integrity;Member("Idra").extra=ExtraGauge.Full;idra.target=onLine.Health;
            Assert.IsTrue(move.TryFire(),"fixture: the move again");for(float end=Time.unscaledTime+6;move.Performing&&Time.unscaledTime<end;){idra.motor.Move(Vector2.zero,false,false);yield return null;}
            Assert.AreEqual(was+sela.Health.maximum*ExtraMove.OutHeal,sela.Health.integrity,1,"a standing partner isn't mended half her Integrity");
        }

        // ------------------------------------------------------------------ as a companion
        [UnityTest] public IEnumerator TheCompanionsRulesForEachOfHerSkills()
        {
            yield return Zone(false);var sela=Lead("Sela");var idra=Idra;Place(sela,new Vector3(200,0,0));Place(idra,new Vector3(204,0,0));
            var body=Dummy(false,new Vector3(204,0,8));yield return Frames(3);sela.target=idra.target=body.Health;var kit=idra.GetComponent<BraceKit>();
            bool Refuses(SkillKind kind)=>LateSkills.Decides(idra,kind,out bool worth)&&!worth;
            bool Wants(SkillKind kind)=>LateSkills.Decides(idra,kind,out bool worth)&&worth;
            // Rescue Flare: not on a lead who's well; for one who's failing, or down.
            Assert.IsTrue(Refuses(SkillKind.RescueFlare),"a companion would throw the flare at a lead at full Integrity");Assert.IsFalse(IdraSkills.Urgent(idra,SkillKind.RescueFlare));
            Hurt(sela,.3f);Assert.IsTrue(Wants(SkillKind.RescueFlare),"a companion wouldn't throw the flare to a lead at 30%");Assert.IsTrue(IdraSkills.Urgent(idra,SkillKind.RescueFlare),"a failing lead waits the companion's turn");
            Hurt(sela,.6f);Assert.IsTrue(Refuses(SkillKind.RescueFlare),"a companion would spend the flare on a lead at 60%");Hurt(sela,1);
            // Shelter: for shots on their way, not for a quiet yard; and not a second dome under the first.
            Assert.IsTrue(Refuses(SkillKind.Shelter),"a companion would raise a dome with nothing in the air");
            var shot=Shot(body,new Vector3(200,1,14),sela.transform.position+Vector3.up);
            Assert.IsTrue(Wants(SkillKind.Shelter),"a companion wouldn't shelter a lead with a shot coming in");Assert.IsTrue(IdraSkills.Urgent(idra,SkillKind.Shelter),"a shot on its way waits the companion's turn");
            var dome=ShelterDome.Plant(idra,idra.transform.position,false);Assert.IsTrue(Refuses(SkillKind.Shelter),"a companion would raise a second dome under the first");
            Object.Destroy(dome.gameObject);shot.Spend();yield return Frames(2);
            var away=Shot(body,new Vector3(200,1,14),new Vector3(160,1,14));Assert.IsTrue(Refuses(SkillKind.Shelter),"a companion would raise a dome for a shot flying away from them both");away.Spend();
            // Counterweight: with something near enough to hit her and the Thrust to guard it.
            Assert.IsTrue(Refuses(SkillKind.Counterweight),"a companion would arm Counterweight with nothing within reach of her");
            var near=Enemy("Ridgehound",idra.transform.position+Vector3.right*3,false);near.Interrupt();yield return null;
            idra.thrust=100;Assert.IsTrue(Wants(SkillKind.Counterweight),"with a hound three metres off and Thrust to guard, a companion wouldn't arm Counterweight");
            idra.thrust=10;Assert.IsTrue(Refuses(SkillKind.Counterweight),"a companion would arm Counterweight with no Thrust to guard with");idra.thrust=100;
            kit.Arm();Assert.IsTrue(Refuses(SkillKind.Counterweight),"a companion would arm Counterweight again with three answers still owed");
            // Hold the Line: a crowd on the lead, and never for a boss alone.
            Assert.IsTrue(Refuses(SkillKind.HoldTheLine),"a companion would hold the line for one hound");
            var crowd=Enemy("Ridgehound",sela.transform.position+Vector3.forward*4,false);crowd.Interrupt();var third=Enemy("Ridgehound",sela.transform.position+Vector3.left*4,false);third.Interrupt();yield return null;
            Assert.IsTrue(Wants(SkillKind.HoldTheLine),"with two hounds on the lead a companion wouldn't hold the line");
            Hurt(idra,.4f);Assert.IsTrue(Refuses(SkillKind.HoldTheLine),"a companion at 40% would call a crowd onto herself");Hurt(idra,1);
            Object.Destroy(crowd.gameObject);Object.Destroy(third.gameObject);Object.Destroy(near.gameObject);yield return Frames(2);
            var asBoss=Dummy(false,sela.transform.position+Vector3.forward*4);asBoss.definition=GameCatalog.Find<EnemyDef>("Burrower");var second=Dummy(false,sela.transform.position+Vector3.left*4);second.definition=asBoss.definition;
            asBoss.Passive=second.Passive=false;asBoss.Interrupt();second.Interrupt();yield return null;
            Assert.IsTrue(Refuses(SkillKind.HoldTheLine),"a companion would hold the line against bosses, which it can't turn");
            asBoss.Passive=second.Passive=true;Object.Destroy(asBoss.gameObject);Object.Destroy(second.gameObject);yield return Frames(2);
            // Breach closes a gap; Tow Line wants something worth reeling; the slam is never planted in a marked danger.
            Assert.IsFalse(LateSkills.Decides(idra,SkillKind.Breach,out _),"with a body 8 m off the rules stop a companion's breach");
            Place(idra,new Vector3(204,0,6));idra.target=body.Health;Assert.IsTrue(Refuses(SkillKind.Breach),"a companion would breach a body 2 m away");Assert.IsTrue(Refuses(SkillKind.TowLine),"a companion would throw a line at a body at her feet");
            Assert.IsFalse(LateSkills.Decides(idra,SkillKind.SlagHammer,out _),"the rules stop a companion's slam on a body in front of her");
            var marking=Telegraph.Circle(idra.transform.position,4,30,Telegraph.Threat,true);marking.owner=body;
            Assert.IsTrue(Refuses(SkillKind.SlagHammer),"a companion would plant herself for the slam inside a marked danger");marking.Clear();
            Place(idra,new Vector3(204,0,0));idra.target=body.Health;Assert.IsFalse(LateSkills.Decides(idra,SkillKind.TowLine,out _),"with a body 8 m off the rules stop a companion's line");
            // The first two heroes' skills are none of this file's business.
            Assert.IsFalse(IdraSkills.Decides(sela,SkillKind.ThreadLance,out _));Assert.IsFalse(IdraSkills.Urgent(sela,SkillKind.MendLattice));
        }
        [UnityTest] public IEnumerator AsACompanionSheBracesWhatIsAboutToLandWhereSelaRolls()
        {
            // She stands where a companion follows to (three behind the lead, two to the right), so nothing but the shot gives her a reason to move.
            yield return Zone(false);var sela=Lead("Sela");var idra=Idra;Place(sela,new Vector3(200,0,0));Place(idra,new Vector3(202,0,-3));
            var shooter=Dummy(false,new Vector3(202,0,14));yield return Frames(3);
            // No Charge: this is her guard's test, not Shelter's.
            idra.GetComponent<PartnerBrain>().enabled=true;idra.charge=0;idra.thrust=100;yield return Frames(2);
            float was=idra.Health.integrity;var from=idra.transform.position;bool braced=false,rolled=false;
            Shot(shooter,shooter.transform.position+Vector3.up,idra.transform.position+Vector3.up,idra.Health.maximum*.2f);
            for(float end=Time.unscaledTime+2.5f;idra.Health.integrity>=was&&idra.FlashGuards==0&&Time.unscaledTime<end;){idra.charge=0;braced|=idra.Guarding;rolled|=idra.State==ActorState.Dodge;yield return null;}
            Assert.IsTrue(braced,"with a shot about to land the companion never raised her guard");Assert.IsFalse(rolled,"she rolled from a shot she could brace");
            float taken=was-idra.Health.integrity;
            Assert.Less(taken,idra.Health.maximum*.2f*.3f,"the shot landed on her open: the guard wasn't up when it arrived");
            Assert.Less(Flat(idra.transform.position,from),1.5f,"she left her ground to a shot she braced");
            // And she lets the guard go again.
            for(float end=Time.unscaledTime+1.5f;idra.Guarding&&Time.unscaledTime<end;){idra.charge=0;yield return null;}
            Assert.IsFalse(idra.Guarding,"the companion never lowers her guard");
            // Short of Thrust she can't brace it: she hops, as anyone would.
            idra.thrust=5;rolled=false;braced=false;
            Shot(shooter,shooter.transform.position+Vector3.up,idra.transform.position+Vector3.up,10);
            for(float end=Time.unscaledTime+2.5f;!rolled&&Time.unscaledTime<end;){idra.charge=0;idra.thrust=Mathf.Min(idra.thrust,20);braced|=idra.Guarding;rolled|=idra.State==ActorState.Dodge;yield return null;}
            Assert.IsTrue(rolled,"with no Thrust to brace a shot she stood and took it");Assert.IsFalse(braced,"she raised a guard she couldn't pay for");
            idra.GetComponent<PartnerBrain>().enabled=false;
            // With Idra leading, Sela is the companion she always was: she rolls.
            var lead=Lead("Idra");Place(lead,new Vector3(200,0,0));Place(sela,new Vector3(202,0,-3));yield return Frames(2);
            sela.GetComponent<PartnerBrain>().enabled=true;sela.thrust=100;yield return Frames(2);rolled=false;braced=false;
            Shot(shooter,shooter.transform.position+Vector3.up,sela.transform.position+Vector3.up,10);
            for(float end=Time.unscaledTime+2.5f;!rolled&&Time.unscaledTime<end;){braced|=sela.Guarding;rolled|=sela.State==ActorState.Dodge;yield return null;}
            Assert.IsTrue(rolled,"Sela as a companion no longer rolls across a shot");Assert.IsFalse(braced,"Sela as a companion raised a guard");
        }
        [UnityTest] public IEnumerator AsACompanionSheSheltersALeadUnderFireAndFlaresALeadWhoIsFailing()
        {
            yield return Zone(false);var sela=Lead("Sela");var idra=Idra;Place(sela,new Vector3(200,0,0));Place(idra,new Vector3(203,0,-2));
            var shooter=Dummy(false,new Vector3(200,0,16));yield return Frames(3);
            Equip(idra,SkillKind.Shelter,0);Equip(idra,SkillKind.RescueFlare,1);
            void Ready(){idra.charge=100;idra.cooldowns[0]=idra.cooldowns[1]=0;idra.cooldowns[2]=idra.cooldowns[3]=30;}
            idra.GetComponent<PartnerBrain>().enabled=true;
            // A quiet yard, a lead who's well: she keeps both.
            for(float end=Time.unscaledTime+1.5f;Time.unscaledTime<end;){Ready();yield return null;}
            Assert.IsFalse(Cast("Idra",SkillKind.Shelter),"the companion raised a dome with nothing in the air");Assert.IsFalse(Cast("Idra",SkillKind.RescueFlare),"the companion threw the flare at a lead who's well");
            // Shots at the lead: the dome goes up at once, not when her turn comes round, and the volley dies on it.
            float started=Time.unscaledTime,was=sela.Health.integrity;
            Shot(shooter,shooter.transform.position+Vector3.up,sela.transform.position+Vector3.up,5);
            for(float end=Time.unscaledTime+2;!Cast("Idra",SkillKind.Shelter)&&Time.unscaledTime<end;){Ready();yield return null;}
            Assert.IsTrue(Cast("Idra",SkillKind.Shelter),"with a shot on its way to the lead the companion never raised Shelter");
            Assert.Less(Time.unscaledTime-started,1.2f,"Shelter waited for the companion's turn: the three seconds before her first skill were still running");
            for(float end=Time.unscaledTime+1;ShelterDome.Latest==null&&Time.unscaledTime<end;){Ready();yield return null;}
            Assert.IsNotNull(ShelterDome.Latest,"fixture: the dome");Assert.IsTrue(ShelterDome.Latest.Covers(sela.transform.position),"the companion's dome doesn't stand over the lead");
            var dome=ShelterDome.Latest;
            Shot(shooter,shooter.transform.position+Vector3.up,sela.transform.position+Vector3.up,5);
            for(float end=Time.unscaledTime+2.5f;dome!=null&&dome.Stopped<2&&Time.unscaledTime<end;){Ready();yield return null;}
            Assert.IsTrue(dome!=null&&dome.Stopped>=2,"the companion's dome didn't stop the shot that brought it up and the one after");
            Assert.AreEqual(was,sela.Health.integrity,.01f,"a shot reached the lead under the companion's dome");
            // The lead failing: the flare, at once.
            Hurt(sela,.3f);was=sela.Health.integrity;started=Time.unscaledTime;
            for(float end=Time.unscaledTime+2;!Cast("Idra",SkillKind.RescueFlare)&&Time.unscaledTime<end;){Ready();yield return null;}
            Assert.IsTrue(Cast("Idra",SkillKind.RescueFlare),"with the lead at 30% the companion never threw the flare");
            for(float end=Time.unscaledTime+2;sela.Health.integrity<=was&&Time.unscaledTime<end;){Ready();yield return null;}
            Assert.Greater(sela.Health.integrity,was,"the companion's flare mended nobody");
            idra.GetComponent<PartnerBrain>().enabled=false;
        }
    }
}
