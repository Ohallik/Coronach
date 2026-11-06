using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The November QA pass, last combat batch: hits say what they did, deaths fit their bodies, a ward shows where it
    /// ends, every skill sounds from its caster, skills do what their rows say, and the curve and the fights run to plan. Every
    /// check here failed on the build it was written against.</summary>
    public sealed class QaCombatTests
    {
        CombatActor Taren=>PartyController.Current.members.First(m=>m.character=="Taren");
        CombatActor Sela=>PartyController.Current.members.First(m=>m.character=="Sela");
        static MemberState Member(string id)=>GameServices.Current.State.party.Find(m=>m.id==id);
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");yield return Zone("Arena_Ground");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+15;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.7f);
        }
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.TryGetComponent<FlightMotor>(out var ship))ship.Halt();
            actor.transform.position=position;body.enabled=true;actor.target=null;actor.TargetLocked=false;Physics.SyncTransforms();
        }
        /// <summary>Open floor far from the arena's walls, for bodies that settle or skills that read the ground.</summary>
        static void Arena()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Combat fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static EnemyBrain Enemy(string id,Vector3 at,bool passive=true){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=passive;return enemy;}
        static BossController Boss(string id,Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=false;return enemy.GetComponent<BossController>();}
        static void Kill(Health body)=>body.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=body.maximum*100,type=DamageType.Pulse});
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Drive(CombatActor actor,float seconds)
        {for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){actor.motor.Move(Vector2.zero,false,false);yield return null;}}
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static SkillDef Skill(string hero,SkillKind kind)=>SkillBook.All(hero).First(s=>s.kind==kind);
        static IEnumerator UntilFree(BossController boss,float seconds)
        {for(float end=Time.unscaledTime+seconds;boss.Busy&&Time.unscaledTime<end;)yield return null;}

        // ------------------------------------------------------------------ hits say what they did
        static (Color color,float size,string text) NewestNumber()
        {
            var text=Object.FindFirstObjectByType<GameHud>().GetComponentsInChildren<TMP_Text>(true).Last(t=>t.name=="Damage");
            return (text.color,text.fontSize,text.text);
        }
        static bool Grey(Color c)=>Mathf.Abs(c.r-c.g)<.1f&&Mathf.Abs(c.g-c.b)<.1f&&c.r<.8f;
        [UnityTest] public IEnumerator ResistedHitsLookDifferentAndWeakSpotsKeepTheirOwnLook()
        {
            var scav=Enemy("MusterScav",new Vector3(200,0,0));yield return Frames(3);
            var body=scav.Health;body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;var source=Taren.Health;
            var plain=System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=body.weakness&&t!=body.resistance);
            body.Receive(new DamagePacket{source=source,amount=100,type=body.resistance});
            var resisted=NewestNumber();
            bool spark=Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Any(p=>p.name=="Lattice particles"&&Grey(p.main.startColor.color));
            body.Receive(new DamagePacket{source=source,amount=100,type=plain});var clean=NewestNumber();
            body.Receive(new DamagePacket{source=source,amount=100,type=body.weakness});var weak=NewestNumber();
            body.Receive(new DamagePacket{source=source,amount=100,type=plain},2);var spot=NewestNumber();
            body.Receive(new DamagePacket{source=source,amount=100,type=body.resistance},2);var resistedSpot=NewestNumber();
            Assert.IsTrue(Grey(resisted.color),$"a resisted hit's number is {resisted.color}: a blade hitting for half looks like any other hit, so nobody learns why");
            Assert.AreNotEqual(clean.color,resisted.color,"a resisted hit and a clean hit show the same number");
            Assert.LessOrEqual(resisted.size,clean.size,"a resisted hit's number shouts as loud as a clean one");
            Assert.GreaterOrEqual(resisted.size,24,"a resisted hit's number falls below the type floor");
            Assert.IsTrue(spark,"a resisted hit throws the same cyan burst as a clean one: nothing grey shows the armour took it");
            StringAssert.Contains("!",weak.text,"a weakness hit lost its mark");
            Assert.AreNotEqual(clean.color,weak.color,"a weakness hit lost its colour");
            Assert.AreNotEqual(clean.color,spot.color,"a boss's hidden weak spot doubles the damage with no sign at all");
            Assert.AreNotEqual(weak.color,spot.color,"a weak spot and a damage-type weakness are told apart by nothing");
            Assert.IsTrue(Grey(resistedSpot.color),$"a resisted hit on a weak spot is only plain damage, and its number is {resistedSpot.color}: the Cantor's links shout !! at a skill they resist");
        }

        // ------------------------------------------------------------------ deaths fit their bodies
        [UnityTest] public IEnumerator AKilledSkiffFallsLikeAFlierAndFailsLikeAMachine()
        {
            yield return Zone("Arena_Flight");
            var skiff=Enemy("MusterSkiff",new Vector3(40,1,-20));yield return Frames(3);
            var body=skiff.GetComponent<DefeatPresentation>().visual;var start=body.position;
            using var log=new CueLog();
            Kill(skiff.Health);yield return new WaitForSecondsRealtime(1.3f);
            Assert.Greater(start.y-body.position.y,1.2f,"a killed Skiff stops level with the flight plane and hangs there tilted, where every other flier falls away");
            Assert.Greater(log.Count("death_mech",skiff.transform),0,"a raider ship dies with no machine failure: "+log.All);
            Assert.AreEqual(0,log.Count("death_creature",skiff.transform),"a raider ship screams like a creature: "+log.All);
        }
        [UnityTest] public IEnumerator AKilledMineBurstsAndIsGone()
        {
            Arena();var mine=Enemy("Shellmine",new Vector3(200,0,0));yield return Frames(3);
            var body=mine.GetComponent<DefeatPresentation>().visual;var rest=body.rotation;var size=body.localScale;float widest=0;
            Kill(mine.Health);
            for(float end=Time.unscaledTime+.35f;Time.unscaledTime<end;){widest=Mathf.Max(widest,body.localScale.x);yield return null;}
            Assert.Greater(widest,size.x*1.1f,"a killed mine doesn't burst: it just tips over");
            Assert.IsTrue(mine!=null,"the mine vanished in a single frame");
            Assert.Less(Quaternion.Angle(rest,body.rotation),2,"a mine that has exploded hangs tilted in the air like a corpse");
            yield return new WaitForSecondsRealtime(.9f);
            Assert.IsTrue(mine==null,"a mine that has exploded is still there more than a second later");
        }
        [UnityTest] public IEnumerator TheBellowsDeflatesInsteadOfTippingOver()
        {
            Arena();var bellows=Enemy("BellowsBelow",new Vector3(200,.1f,0));yield return Frames(3);
            var body=bellows.GetComponent<DefeatPresentation>().visual;var size=body.localScale;var upright=body.rotation;
            Kill(bellows.Health);yield return new WaitForSecondsRealtime(1.6f);
            Assert.Less(body.localScale.y,size.y*.6f,"the Bellows' breath never leaves it: the dome keeps its full height at death");
            Assert.Less(Quaternion.Angle(upright,body.rotation),5,"the Bellows tips over like a statue");
        }

        // ------------------------------------------------------------------ the bosses talk and show
        [UnityTest] public IEnumerator TheFirstHitOnABracedBellowsSaysWhy()
        {
            Arena();var organ=new GameObject("Organ fixture").AddComponent<PressureOrgan>();
            var bellows=Enemy("BellowsBelow",new Vector3(200,.1f,0));yield return Frames(3);
            Assert.IsTrue(PressureOrgan.AnyPumping,"fixture: an organ pumps");
            var said=new List<string>();void Heard(string who,string line){said.Add(line);}BarkService.Spoken+=Heard;
            bellows.Health.Receive(new DamagePacket{source=Taren.Health,amount=10,type=DamageType.Plasma});yield return null;
            bellows.Health.Receive(new DamagePacket{source=Taren.Health,amount=10,type=DamageType.Plasma});yield return null;
            BarkService.Spoken-=Heard;
            Assert.AreEqual(1,said.Count(l=>l.Contains("organ")),"half damage while the organs pump, and nobody says so: a player can double the fight's length without knowing why");
            foreach(var hero in new[]{"Taren","Sela"})Assert.IsNotNull(BarkService.Line(hero,"bossbraced"),hero+" has no line for a braced Bellows");
            Object.Destroy(organ.gameObject);
        }
        [UnityTest] public IEnumerator TheGrappleChainIsALineYouCanSee()
        {
            Arena();var taren=Taren;Place(Sela,new Vector3(260,0,0));
            var boss=Boss("HessaKorl",new Vector3(200,.1f,0));Place(taren,new Vector3(200,.1f,9));yield return Frames(3);
            boss.GetComponent<EnemyBrain>().Passive=true;yield return null;
            LineRenderer Chain()=>Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).FirstOrDefault(l=>l.name=="Grapple chain");
            Assert.IsTrue(boss.Perform("GrappleChain"));
            for(float end=Time.unscaledTime+1.5f;Chain()==null&&Time.unscaledTime<end;)yield return null;
            var chain=Chain();Assert.IsNotNull(chain,"a caught hero is dragged in by nothing visible");
            yield return null;
            Assert.Less(Flat(chain.GetPosition(0),boss.transform.position),2.5f,"the chain doesn't start at her hand");
            Assert.Less(Flat(chain.GetPosition(1),taren.transform.position),1.5f,"the chain doesn't reach the hero it is dragging");
            yield return UntilFree(boss,3);
            Assert.IsNull(Chain(),"the chain is left hanging after the slam");
            // A dodged chain still flies out along its lane.
            yield return new WaitForSecondsRealtime(.3f);Place(taren,new Vector3(200,.1f,9));
            Assert.IsTrue(boss.Perform("GrappleChain"));yield return null;var lane=(taren.transform.position-boss.transform.position).normalized;lane.y=0;
            Place(taren,new Vector3(209,.1f,9));
            for(float end=Time.unscaledTime+1.5f;Chain()==null&&Time.unscaledTime<end;)yield return null;
            Assert.IsNotNull(Chain(),"a dodged chain is a lane flash with no chain in it");
            float reach=0;
            for(float end=Time.unscaledTime+.3f;Time.unscaledTime<end;)
            {
                chain=Chain();if(chain!=null)reach=Mathf.Max(reach,Vector3.Dot(chain.GetPosition(1)-chain.GetPosition(0),lane));
                yield return null;
            }
            Assert.Greater(reach,2,"the chain doesn't fly along its lane");
        }
        [UnityTest] public IEnumerator HessaClosesTheGapWithAMarkedLunge()
        {
            Arena();var taren=Taren;Place(Sela,new Vector3(260,0,0));
            var boss=Boss("HessaKorl",new Vector3(200,.1f,15));Place(taren,new Vector3(200,.1f,0));yield return Frames(3);
            boss.GetComponent<EnemyBrain>().Passive=true;yield return null;
            Assert.IsTrue(boss.Perform("Advance"),"she walks at 3.4 m/s against a 6.7 m/s jog and has no way to close: a duellist looks lost at range");
            yield return null;
            Assert.IsTrue(Telegraph.Live.Any(t=>t!=null&&t.owner==boss&&t.Shape==Telegraph.Kind.Line&&!t.Released),"the lunge comes with no marked lane");
            for(float end=Time.unscaledTime+2;Flat(boss.transform.position,taren.transform.position)>5&&Time.unscaledTime<end;)yield return null;
            Assert.LessOrEqual(Flat(boss.transform.position,taren.transform.position),5,"the lunge didn't close the gap");
            bool combo=false;for(float end=Time.unscaledTime+1.2f;!combo&&Time.unscaledTime<end;){combo=Telegraph.Live.Any(t=>t!=null&&t.owner==boss&&t.Shape==Telegraph.Kind.Cone&&!t.Released);yield return null;}
            Assert.IsTrue(combo,"she arrives and does nothing with it: the lance doesn't follow the lunge");
            // The cone shows before her first step, so this is where the lunge stopped. Signed: past the hero it goes negative.
            Assert.Greater(boss.transform.position.z-taren.transform.position.z,1.5f,"the lunge ran through the hero instead of stopping short");
        }
        [UnityTest] public IEnumerator HessaDoesNotLanceThinAirWhenTheHeroHasRunOn()
        {
            Arena();var taren=Taren;Place(Sela,new Vector3(260,0,0));
            var boss=Boss("HessaKorl",new Vector3(200,.1f,15));Place(taren,new Vector3(200,.1f,0));yield return Frames(3);
            boss.GetComponent<EnemyBrain>().Passive=true;yield return null;
            Assert.IsTrue(boss.Perform("Advance"),"she walks at 3.4 m/s against a 6.7 m/s jog and has no way to close: a duellist looks lost at range");
            yield return null;
            // A jog away through the .6 s tell and the lunge covers about 8 m, and her lane was aimed where he stood.
            Place(taren,new Vector3(200,.1f,-8));
            bool lanced=false;
            for(float end=Time.unscaledTime+3;boss.Busy&&Time.unscaledTime<end;){lanced|=Telegraph.Live.Any(t=>t!=null&&t.owner==boss&&t.Shape==Telegraph.Kind.Cone);yield return null;}
            Assert.Less(boss.transform.position.z,5,"she never lunged at all");
            Assert.IsFalse(lanced,"her lunge stops 11 m short of a hero who kept running, and she thrusts a full lance combo at nobody: a duellist looks lost");
            Assert.IsFalse(boss.Busy,"after a lunge that fell short she stands there and never picks another move");
        }
        [UnityTest] public IEnumerator AColumnBetweenHessaAndAHeroTakesTheFlash()
        {
            Arena();var taren=Taren;var sela=Sela;
            var boss=Boss("HessaKorl",new Vector3(200,.1f,0));yield return Frames(3);boss.GetComponent<EnemyBrain>().Passive=true;yield return null;
            Place(taren,new Vector3(200,.1f,8));Place(sela,new Vector3(206,.1f,0));
            var column=GameObject.CreatePrimitive(PrimitiveType.Cylinder);column.transform.position=new Vector3(200,2.5f,4);column.transform.localScale=new Vector3(1.5f,2.5f,1.5f);Physics.SyncTransforms();
            float behind=taren.Health.integrity,open=sela.Health.integrity;
            Assert.IsTrue(boss.Perform("Lampbreaker"));yield return UntilFree(boss,4);
            Assert.AreEqual(behind,taren.Health.integrity,.01f,"the columns the room was built around give no cover: the flash passes straight through them");
            Assert.Less(sela.Health.integrity,open,"the flash missed a hero in the open");
        }

        // ------------------------------------------------------------------ the relay's ward
        static RelayLantern Lantern(Vector3 at,float lightHeight)
        {
            var go=new GameObject("Relay fixture");go.transform.position=at;
            var lantern=go.AddComponent<RelayLantern>();lantern.alwaysLit=true;lantern.radius=10;
            var light=new GameObject("Relay light",typeof(Light)).GetComponent<Light>();light.transform.SetParent(go.transform,false);light.transform.localPosition=Vector3.up*lightHeight;
            light.type=LightType.Point;light.range=18;light.enabled=false;lantern.glow=light;
            return lantern;
        }
        [UnityTest] public IEnumerator ALitRelayShowsWhereItsWardEndsAndHoldsGrazersOff()
        {
            Arena();var taren=Taren;Place(taren,new Vector3(202,0,0));Place(Sela,new Vector3(260,0,0));
            var lantern=Lantern(new Vector3(200,0,0),9);
            var grazer=Enemy("Gloomgrazer",new Vector3(197,0,0),false);yield return Frames(3);
            var ring=Telegraph.Live.FirstOrDefault(t=>t!=null&&t.owner==lantern);
            Assert.IsNotNull(ring,"nothing on the ground shows where a relay's ward ends: a grazer in plain view of the glow may be outside it");
            Assert.AreEqual(Telegraph.Kind.Ring,ring.Shape);
            Assert.AreEqual(lantern.radius,ring.Radius,.01f,"the ring on the ground isn't the ward's real reach");
            float pool=Mathf.Sqrt(Mathf.Max(0,lantern.glow.range*lantern.glow.range-81));
            Assert.AreEqual(lantern.radius,pool,.6f,"the light on the floor reaches well past the ward: it looks safe out there and isn't");
            float before=taren.Health.integrity,start=Flat(grazer.transform.position,lantern.transform.position);bool told=false;
            for(float end=Time.unscaledTime+2.5f;Time.unscaledTime<end;){told|=grazer.Telegraphing;yield return null;}
            Assert.IsFalse(told,"a grazer inside the light still winds up its lunge at full speed: the ward only slows its walk");
            Assert.AreEqual(before,taren.Health.integrity,.01f,"a grazer standing in a lit relay's light bit the hero");
            Assert.Greater(Flat(grazer.transform.position,lantern.transform.position),start+1,"a light-shy grazer stands in the light instead of backing out of it");
            grazer.Passive=true;
        }
        [UnityTest] public IEnumerator AGrazerFightSitsInsideARelaysReach()
        {
            yield return Zone("LanternGorge");
            var lanterns=Object.FindObjectsByType<RelayLantern>(FindObjectsSortMode.None).Where(r=>r.flag!=null&&r.flag.StartsWith("gorge.relay")).ToArray();
            Assert.IsNotEmpty(lanterns,"fixture: the gorge's relays");
            var fights=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Where(e=>e.spawners.Any(s=>s.definition!=null&&s.definition.id=="Gloomgrazer")).ToArray();
            Assert.IsNotEmpty(fights,"fixture: the gorge's grazer fights");
            float nearest=fights.Min(e=>lanterns.Min(l=>Flat(e.transform.position,l.transform.position)));
            Assert.LessOrEqual(nearest,lanterns[0].radius,$"the nearest grazer fight is {nearest:0} m from a relay: the ward never touches a fight unless a pack is led back to it");
        }

        // ------------------------------------------------------------------ sound
        static void Remove<T>() where T:Component{foreach(var left in Object.FindObjectsByType<T>(FindObjectsSortMode.None))Object.Destroy(left.gameObject);}
        IEnumerator EverySkillSoundsFromItsCaster(float y)
        {
            Place(Taren,new Vector3(200,y,0));Place(Sela,new Vector3(260,y,0));
            foreach(var member in GameServices.Current.State.party)member.level=12;
            yield return new WaitForSecondsRealtime(.6f);
            var heard=new List<(string key,Transform owner)>();void Cue(string key,Transform owner){heard.Add((key,owner));}
            var failures=new List<string>();AudioManager.CueStarted+=Cue;
            try
            {
                foreach(var hero in PartyController.Current.members)foreach(var skill in SkillBook.All(hero.character))
                {
                    for(float end=Time.unscaledTime+2;!hero.CanAct&&Time.unscaledTime<end;){hero.motor.Move(Vector2.zero,false,false);yield return null;}
                    // A blink's mine and a fold's well sound from the hero after she can act again: left alone, they would speak for a silent skill.
                    Remove<WaypointCharge>();Remove<GravityWell>();Remove<StarfallStrike>();yield return null;
                    Assert.IsTrue(SkillBook.Equip(Member(hero.character),0,skill),skill.id+" can't be equipped");
                    hero.charge=100;hero.cooldowns[0]=0;heard.Clear();
                    if(!hero.Skill(0)){failures.Add(skill.displayName+" did not start");continue;}
                    for(float end=Time.unscaledTime+1;Time.unscaledTime<end&&!heard.Any(c=>c.owner==hero.transform);){hero.motor.Move(Vector2.zero,false,false);yield return null;}
                    if(!heard.Any(c=>c.owner==hero.transform))failures.Add($"{hero.character}'s {skill.displayName} has no sound from the hero who cast it (heard: {string.Join(",",heard.Select(c=>c.key))})");
                }
            }
            finally{AudioManager.CueStarted-=Cue;}
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator EverySkillSoundsFromItsCasterOnTheGround(){Arena();yield return EverySkillSoundsFromItsCaster(0);}
        [UnityTest] public IEnumerator EverySkillSoundsFromItsCasterInFlight(){yield return Zone("Arena_Flight");yield return EverySkillSoundsFromItsCaster(1);}
        [UnityTest] public IEnumerator ASkillIsNoLouderThanABoss()
        {
            Arena();var taren=Taren;Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));Member("Taren").level=12;yield return Frames(2);
            float loudest=0;
            void Cue(string key,Transform owner){foreach(var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))if(source.clip!=null&&source.clip.name==key)loudest=Mathf.Max(loudest,source.volume);}
            AudioManager.CueStarted+=Cue;
            Assert.IsTrue(SkillBook.Equip(Member("Taren"),0,Skill("Taren",SkillKind.AnchorSlam)));taren.charge=100;taren.cooldowns[0]=0;Assert.IsTrue(taren.Skill(0));
            yield return Drive(taren,1);AudioManager.CueStarted-=Cue;
            Assert.Greater(loudest,0,"fixture: the slam made a sound");
            Assert.LessOrEqual(loudest,.3f,"Anchor Slam is louder than nearly every boss cue: a partner's slam drowns the fight");
        }
        [UnityTest] public IEnumerator ALungeAndASwapStrikeAreHeard()
        {
            using var log=new CueLog();
            PartyController.Current.Swap();yield return null;var sela=PartyController.Current.Active;
            Assert.Greater(log.Count("forceField_000",sela.transform),0,"the tag-in strike lands in silence: "+log.All);
            log.Clear();PartyController.Current.Swap();yield return null;
            Assert.AreEqual(0,log.Count("forceField_000",PartyController.Current.Active.transform),"a swap with no strike still sounds like one: "+log.All);
            yield return Zone("Arena_Flight");
            var taren=Taren;Place(taren,new Vector3(200,1,0));Place(Sela,new Vector3(260,1,0));yield return Frames(2);
            log.Clear();Assert.IsTrue(taren.Lunge(),"fixture: a lunge");
            Assert.Greater(log.Count("thrusterFire_000",taren.transform),0,"a flight lunge that misses makes no sound at all: "+log.All);
        }

        // ------------------------------------------------------------------ skills do what their rows say
        [UnityTest] public IEnumerator SkillRowsSayWhatTheSkillsDo()
        {
            yield return null;
            var row=new Dictionary<SkillKind,string>();
            foreach(var hero in new[]{"Taren","Sela"})foreach(var skill in SkillBook.All(hero))row[skill.kind]=skill.description;
            foreach(var (kind,text) in row)
            {
                StringAssert.DoesNotContain("the ground",text,$"{kind}'s row talks about the ground, and it is read in flight too");
                Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(text,@"\bcharge\b"),$"{kind}'s row says 'charge' on a row that lists Charge as its cost: {text}");
            }
            StringAssert.DoesNotContain("burning",row[SkillKind.EmberStep],"Ember Step promises a burn; there is one plasma hit and nothing burns");
            StringAssert.DoesNotContain("through",row[SkillKind.EmberStep],"Ember Step promises to pass through a body that stops it");
            StringAssert.Contains("moment",row[SkillKind.Refract],"Refract promises 'the next hit': its window is 1.4 s");
            StringAssert.DoesNotContain("guards",row[SkillKind.PulseBurst],"Pulse Burst talks about guards; the HUD calls it Break");
            StringAssert.DoesNotContain("Spin forward",row[SkillKind.ForgeSpiral],"in flight Forge Spiral neither spins nor goes forward");
            StringAssert.StartsWith("At least",row[SkillKind.Overdrive],"mastery raises Overdrive past the twenty percent its row states");
            // The chooser's widest row, on a pad: a label has 1136 of the row's 1220 between its margins, and the row's height holds one line at 26.
            // What a skill does follows the focus in the note, which gives it one line of 1220 at 24.
            var probe=UiKit.Text(Object.FindFirstObjectByType<GameHud>().transform,"Row probe","",26,Color.white);var noteProbe=UiKit.Text(probe.transform.parent,"Note probe","",24,Color.white);
            foreach(var hero in new[]{"Taren","Sela"})foreach(var skill in SkillBook.All(hero))
            {
                float width=probe.GetPreferredValues($"On RB+Y · {skill.displayName}  ·  {SkillBook.Cost(hero,skill):0} Charge  ·  Mastery 4 (59/60)  ·  {skill.cooldown:0} s").x;
                Assert.LessOrEqual(width,1136,$"{skill.displayName}'s row is {width:0} wide in a 1136 label: in the skill chooser its last word drops to a line the row has no height for");
                float said=noteProbe.GetPreferredValues($"{skill.displayName}: {skill.description}").x;
                Assert.LessOrEqual(said,1220,$"{skill.displayName}'s description is {said:0} wide on a 1220 line: in the skill chooser it takes the line its note needs for the trade rule");
            }
            Object.Destroy(probe.gameObject);Object.Destroy(noteProbe.gameObject);
        }
        [UnityTest] public IEnumerator MasteryMakesOverdriveAndRefractHitHarderAsTheCardSays()
        {
            Arena();var taren=Taren;var sela=Sela;Place(taren,new Vector3(260,0,0));Place(sela,new Vector3(200,0,0));yield return Frames(2);
            float Sample(){taren.fortune=0;return taren.Packet(100,DamageType.Pulse,0).amount;}
            float baseline=Sample();
            for(int i=0;i<200;i++)SkillBook.RecordUse("Taren",Skill("Taren",SkillKind.Overdrive));
            taren.charge=100;Assert.IsTrue(taren.Skill(3),"fixture: Overdrive");yield return new WaitForSecondsRealtime(.65f);
            Assert.IsTrue(taren.Overdriving,"fixture: armed");
            Assert.Greater(Sample(),baseline*1.25f,"the card says Overdrive hits harder at mastery 5, and it still gives twenty percent");
            var victim=Enemy("Ridgehound",new Vector3(200,0,4));victim.enabled=false;yield return Frames(2);
            var body=victim.Health;body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;sela.target=body;
            var dealt=new List<float>();
            for(int round=0;round<2;round++)
            {
                if(round==1)for(int i=0;i<200;i++)SkillBook.RecordUse("Sela",Skill("Sela",SkillKind.Refract));
                sela.charge=100;sela.cooldowns[3]=0;Assert.IsTrue(sela.Skill(3),"fixture: Refract");yield return new WaitForSecondsRealtime(.65f);
                float before=body.integrity;sela.Health.Receive(new DamagePacket{source=body,amount=1,type=DamageType.Pulse});
                yield return new WaitForSecondsRealtime(.7f);dealt.Add(before-body.integrity);
            }
            Assert.Greater(dealt[0],0,"fixture: the counter landed");
            Assert.Greater(dealt[1],dealt[0]*1.25f,"the card says Refract hits harder at mastery 5, and its counter deals the same");
        }
        [UnityTest] public IEnumerator RefractDeflectsBackAtWhoeverHitHer()
        {
            Arena();var sela=Sela;Place(sela,new Vector3(200,0,0));Place(Taren,new Vector3(260,0,0));
            var selected=Enemy("Ridgehound",new Vector3(200,0,4));var attacker=Enemy("Ridgehound",new Vector3(204,0,0));selected.enabled=false;attacker.enabled=false;yield return Frames(2);
            foreach(var body in new[]{selected.Health,attacker.Health}){body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;}
            sela.target=selected.Health;sela.charge=100;sela.cooldowns[3]=0;Assert.IsTrue(sela.Skill(3),"fixture: Refract");
            yield return new WaitForSecondsRealtime(.65f);
            sela.Health.Receive(new DamagePacket{source=attacker.Health,amount=1,type=DamageType.Pulse});
            yield return new WaitForSecondsRealtime(.7f);
            Assert.Less(attacker.Health.integrity,attacker.Health.maximum,"the row says the hit deflects back; the beam went to whatever she had selected, not to who hit her");
            Assert.AreEqual(selected.Health.maximum,selected.Health.integrity,.01f,"the counter struck a body that never touched her");
        }
        [UnityTest] public IEnumerator FaultlineIsWorthItsCharge()
        {
            Arena();var taren=Taren;Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            foreach(var member in GameServices.Current.State.party)member.level=12;yield return new WaitForSecondsRealtime(.7f);
            var victim=Enemy("Ridgehound",new Vector3(200,0,6));victim.enabled=false;yield return Frames(2);
            var body=victim.Health;body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;body.weakness=DamageType.Beam;body.resistance=DamageType.Plasma;taren.target=body;
            Assert.IsTrue(SkillBook.Equip(Member("Taren"),0,Skill("Taren",SkillKind.Faultline)));taren.charge=100;taren.cooldowns[0]=0;
            float power=(1+Mathf.Max(0,taren.resonance-10)*.025f)*SkillBook.Power(1);
            Assert.IsTrue(taren.Skill(0),"fixture: Faultline");yield return Drive(taren,1.1f);
            float dealt=body.maximum-body.integrity;
            Assert.GreaterOrEqual(dealt,taren.damage*1.9f*power,"the last skill Taren learns, for 28 Charge, hits one target for less than a skill he has had since the start");
        }
        [UnityTest] public IEnumerator TheHookClimbsAGentleRise()
        {
            var ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);ramp.transform.SetPositionAndRotation(new Vector3(200,0,10),Quaternion.Euler(-10,0,0));ramp.transform.localScale=new Vector3(20,1,60);Physics.SyncTransforms();
            Assert.IsTrue(Physics.Raycast(new Vector3(200,30,0),Vector3.down,out var near,60),"fixture: the ramp");
            Assert.IsTrue(Physics.Raycast(new Vector3(200,30,8),Vector3.down,out var far,60),"fixture: the ramp");
            var taren=Taren;Place(taren,near.point);Place(Sela,new Vector3(260,near.point.y,0));
            var victim=Enemy("Ridgehound",far.point+Vector3.up*.05f);victim.enabled=false;yield return Frames(2);
            var body=victim.Health;float before=body.integrity;
            TetherLine.Cast(taren,taren.transform.position+Vector3.up*.9f+Vector3.forward*.6f,Vector3.forward,12,taren.Packet(10,DamageType.Pulse,1));
            yield return new WaitForSecondsRealtime(.6f);
            Assert.Less(body.integrity,before,"a 10 degree rise reads as a wall: the hook stops at the slope about 5 m out");
        }
        [UnityTest] public IEnumerator AWellAWallStoppedStillOpensOnTheFloor()
        {
            Arena();var sela=Sela;Place(sela,new Vector3(200,0,0));Place(Taren,new Vector3(260,0,0));
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(201.2f,2,0);wall.transform.localScale=new Vector3(.4f,4,8);Physics.SyncTransforms();
            var well=GravityWell.Throw(sela,new Vector3(200.4f,1.5f,0),new Vector3(207,0,0),false,1);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.IsTrue(well!=null,"fixture: the well is open");
            Assert.Less(well.transform.position.y,.4f,"a Gravity Fold thrown at a wall opens in mid-air, its ring floating over everyone's heads");
        }
        [UnityTest] public IEnumerator AFlightBlinkWithNoStickGoesAwayAndKeepsItsNose()
        {
            yield return Zone("Arena_Flight");
            var sela=Sela;Place(sela,new Vector3(200,1,0));Place(Taren,new Vector3(260,1,0));
            foreach(var member in GameServices.Current.State.party)member.level=12;
            var nose=sela.motor.Facing;
            var enemy=Enemy("ChoristerDart",sela.transform.position+nose*6);yield return Frames(2);
            sela.target=enemy.Health;float gap=Flat(sela.transform.position,enemy.transform.position);
            Assert.IsTrue(SkillBook.Equip(Member("Sela"),0,Skill("Sela",SkillKind.WaypointBlink)));sela.charge=100;sela.cooldowns[0]=0;
            Assert.IsTrue(sela.Skill(0),"fixture: Blink");yield return Drive(sela,.35f);
            Assert.Greater(Flat(sela.transform.position,enemy.transform.position),gap+3,"the row says 'Blink away' and in flight she dashes straight at the enemy");
            var toEnemy=enemy.transform.position-sela.transform.position;toEnemy.y=0;
            Assert.Less(Vector3.Angle(sela.motor.Facing,toEnemy),30,"after the blink her nose points away: the next shot goes into empty space");
        }

        // ------------------------------------------------------------------ the companion
        [UnityTest] public IEnumerator TheCompanionDoesNotDodgeOutOfASkillItJustPaidFor()
        {
            Arena();var taren=Taren;var sela=Sela;Place(taren,new Vector3(200,0,0));Place(sela,new Vector3(220,0,0));
            var shooter=Enemy("ChoristerDrifter",new Vector3(280,0,60));yield return Frames(2);
            sela.charge=100;sela.cooldowns[0]=0;Assert.IsTrue(sela.Skill(0),"fixture: a paid skill");int sequence=sela.AttackSequence;
            sela.GetComponent<PartnerBrain>().enabled=true;
            var at=sela.transform.position+Vector3.up*.5f;var from=at+Vector3.right*3;
            Projectile.Fire(from,at-from,new DamagePacket{source=shooter.Health,amount=1,type=DamageType.Beam},11);
            bool cancelled=false;
            for(float end=Time.unscaledTime+.3f;Time.unscaledTime<end;){cancelled|=sela.State==ActorState.Dodge||sela.AttackSequence!=sequence;yield return null;}
            sela.GetComponent<PartnerBrain>().enabled=false;
            Assert.IsFalse(cancelled,"the companion's roll threw away the skill it had just spent Charge on");
        }
        [UnityTest] public IEnumerator TheCompanionDoesNotDashItsHullIntoAWall()
        {
            yield return Zone("Arena_Flight");
            var taren=Taren;Place(taren,new Vector3(200,1,0));Place(Sela,new Vector3(260,1,0));yield return Frames(2);
            var nose=taren.motor.Facing;var side=Vector3.Cross(Vector3.up,nose).normalized;
            var enemy=Enemy("ChoristerDart",taren.transform.position+nose*5);yield return Frames(2);taren.target=enemy.Health;
            var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.transform.position=taren.transform.position+nose*3+side*1.2f+Vector3.up;post.transform.localScale=new Vector3(.4f,6,.4f);Physics.SyncTransforms();
            var worthwhile=typeof(PartnerBrain).GetMethod("Worthwhile",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(worthwhile,"fixture: the companion's skill check");
            var brain=taren.GetComponent<PartnerBrain>();
            Assert.IsFalse((bool)worthwhile.Invoke(brain,new object[]{SkillKind.EmberStep}),"a clear line to the target isn't a clear path for the hull: the companion dashes into the post for 12 integrity");
            Object.Destroy(post);yield return null;Physics.SyncTransforms();
            Assert.IsTrue((bool)worthwhile.Invoke(brain,new object[]{SkillKind.EmberStep}),"fixture: with the post gone the dash is fine");
        }

        // ------------------------------------------------------------------ the curve and the fights
        static int SyncAt(int xp){var hero=new MemberState{id="Taren",level=1};Levels.Grant(hero,xp);return hero.level;}
        [UnityTest] public IEnumerator TheMainPathLandsOnThePlansSyncLevels()
        {
            yield return null;
            // XP earned on the main path by each chapter boundary, summed from the scenes' spawners and the quests (notes.md).
            foreach(var (place,xp,sync) in new[]{("after Sorrel",832,4),("after Hushwell",1768,5),("after the Cantor",2872,7),("after the Wick Reach",3560,8),("after the gorge",4510,9),("before Hessa",5035,9),("at chapter 2's end",6155,10)})
                Assert.AreEqual(sync,SyncAt(xp),$"{place}: the plan's chapter 2 tops out at Sync 8 to 10 and the curve overshoots it");
        }
        static float Share(float raw,string hero,int sync)
        {
            float plating=Levels.Calculate(GameCatalog.Find<CharacterDef>(hero),sync).plating;
            return raw*(100f/(100f+Mathf.Max(0f,plating-10f)))/Levels.MaxIntegrity(sync);
        }
        [UnityTest] public IEnumerator ChapterTwosOrdinaryHitsDoNotFallBehindTheParty()
        {
            yield return null;
            float bite=GameCatalog.Find<EnemyDef>("Gloomgrazer").attacks[0].damage;
            // The Sync each zone is fought at, not the one it is left at: the Reach gives Sync 8 at 3500 of its 3560 XP, the gorge Sync 9 at 4500 of its 4510.
            foreach(var (zone,sync) in new[]{("WickReach",7),("LanternGorge",8),("SunkenRelay",9)})
            {
                float share=Share(bite*GameCatalog.Find<ZoneDef>(zone).enemyThreat,"Taren",sync);
                Assert.GreaterOrEqual(share,.09f,$"in {zone} at Sync {sync} a bite takes {share:P1} of a hero: the second chapter hits softer than the first");
                Assert.LessOrEqual(share,.125f,$"in {zone} at Sync {sync} a bite takes {share:P1} of a hero: more than a boss's phase 1 hit");
            }
        }
        [UnityTest] public IEnumerator EveryBossLastsAboutTwoMinutesAtTheRateThePartyDealsDamage()
        {
            yield return null;
            // Integrity per second the replay party actually took off each boss (BOSS_DURATION, median of the QA and C7 chains).
            foreach(var (boss,rate) in new[]{("Burrower",240f),("BellowsBelow",126f),("Cantor",285f),("HessaKorl",392f)})
            {
                float seconds=GameCatalog.Find<EnemyDef>(boss).integrity/rate;
                Assert.GreaterOrEqual(seconds,100,$"{boss} falls in {seconds:0} s at the rate the replay party deals damage: the bar is 2 to 5 minutes");
            }
        }
        [UnityTest] public IEnumerator TheLongerBurrowerHitsSofter()
        {
            Arena();var taren=Taren;Place(Sela,new Vector3(260,0,0));
            foreach(var member in GameServices.Current.State.party)member.level=3;yield return new WaitForSecondsRealtime(.7f);
            var boss=Boss("Burrower",new Vector3(200,.1f,0));Place(taren,new Vector3(200,.1f,-3.6f));yield return Frames(3);
            boss.GetComponent<EnemyBrain>().Passive=true;boss.transform.rotation=Quaternion.LookRotation(Vector3.forward);yield return null;
            float before=taren.Health.integrity;
            Assert.IsTrue(boss.Perform("TailSweep"));yield return UntilFree(boss,3);
            float share=(before-taren.Health.integrity)/taren.Health.maximum;
            Assert.Greater(share,0,"fixture: the sweep landed");
            Assert.LessOrEqual(share,.125f,$"a phase 1 tail sweep takes {share:P1} of a Sync 3 hero: a longer fight at that rate beats the party more often than the 2 in 4 it already did");
        }
        [UnityTest] public IEnumerator HessaHitsAsHardAsTheBossesBeforeHer()
        {
            Arena();var taren=Taren;Place(Sela,new Vector3(260,0,0));
            foreach(var member in GameServices.Current.State.party)member.level=9;yield return new WaitForSecondsRealtime(.7f);
            var boss=Boss("HessaKorl",new Vector3(200,.1f,0));Place(taren,new Vector3(200,.1f,3));yield return Frames(3);
            boss.GetComponent<EnemyBrain>().Passive=true;yield return null;
            float biggest=0;taren.Health.Damaged+=(_,__,amount)=>biggest=Mathf.Max(biggest,amount);
            Assert.IsTrue(boss.Perform("HookLance"));yield return UntilFree(boss,6);
            float share=biggest/taren.Health.maximum;
            Assert.GreaterOrEqual(share,.10f,$"her finishing thrust takes {share:P1} of a Sync 9 hero: the chapter 2 boss hits softer than the first boss of the game");
        }
        [UnityTest] public IEnumerator ABuilderRebuildWouldNotPutRawIdsBack()
        {
            yield return null;
            string Source(string path)=>File.ReadAllText(Path.Combine(Application.dataPath,"_Project/Scripts/Editor/"+path));
            string arena=Source("ArenaBuilder.cs"),world=Source("World/WorldBuilder.cs"),hushwell=Source("World/HushwellBuilder.cs");
            StringAssert.DoesNotContain("m.id=m.displayName=mat",arena,"a rebuild shows 'ScrapAlloy' on the Items page");
            foreach(var old in new[]{"\"Repair gel\"","\"The silent outpost\"","\"Six bright answers\""})StringAssert.DoesNotContain(old,arena,"a rebuild puts "+old+" back on screen");
            StringAssert.DoesNotContain("\"Charge cell\"",world,"a rebuild puts 'Charge cell' back in the bag");
            StringAssert.DoesNotContain("r.displayName=\"ChargeCell\"",world,"a rebuild shows 'ChargeCell' in Fabricate");
            StringAssert.DoesNotContain("\"What the drill found\"",hushwell,"a rebuild puts a lower-case quest title back in the log");
        }
    }
}
