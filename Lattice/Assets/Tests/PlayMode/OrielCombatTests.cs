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
    /// <summary>Chapter 7's fights, each on a bare floor in the proving yards with what it needs made here (nothing waits on the Orrery, the Skyfields or the Glass Choir being built). The
    /// party is the one the chapter's fights are met by: chapter 7's arrival (DevLoadout chapter7: three heroes, Taren and Idra out, Tempered parts), with every pair a player can put out
    /// where the fight is about the party. The hit sizes on all three heroes; one test for each family the chapter added or changed, saying what a player sees: the wisp and the rail warden
    /// riding their lines, the shard on its track, the pruner's ring with a hole in it, the sentinel's glass front, the glassback's plates, the tender at the clamp. Then the clamp itself, and
    /// the Last Warden: its hold stopping at each clamp's line, its strikes down the fibres, its Draw and a held clamp, its standing down at the fourth, and its length for every pair. The
    /// chapter's expectations for the shared enemy and boss tests are here too.</summary>
    public sealed class OrielCombatTests
    {
        const float X=230;
        readonly List<string> log=new();
        static readonly string[] Heeded={"RIDER_","SPINNER_","GUARD_","CHARGER_","TENDER_","TENDED","CLAMP","WARDEN_","FORCE_LINE","BOSS_","COMBAT_KILL","BREAK_OK","LEVEL_UP"};
        void Heard(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        /// <summary>Off unless this class's own run sets it (ORIEL_COMBAT_BROKEN=1): each test then builds its fixture wrong, so the run shows every test failing the way it should.</summary>
        static bool Broken=>System.Environment.GetEnvironmentVariable("ORIEL_COMBAT_BROKEN")=="1";
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter7");log.Clear();Application.logMessageReceived+=Heard;BridleClamp.Slack=false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;BridleClamp.Slack=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Oriel(ContentRegistry.Book book)
        {
            // Where a hero stands for each ordinary enemy's tell: on its line (the wisp's own, made through him; the warden's rail, ahead of it as it was set facing; the shard's track),
            // inside the pruner's ring, the glassback's run-in, the sentinel's bash and the tender's torch.
            book.Tell("FibreWisp",3);book.Tell("Pruner",2.5f);book.Tell("Glassback",5);book.Tell("GlassSentinel",2);book.Tell("ClampTender",2);
            // The warden's ship stands 13 m down its rail, inside its 16 m reach: at 8 m it stood in the burning wake the Muster Tender's entry leaves two entries before it in the shared
            // flying test (4 s at 8 m), was ticked mid-tell, and on that test's Sync 1 bar the bolt then downed him and every flyer after it "never told".
            book.Tell("RailWarden",13,flight:true);book.Tell("Moonshard",6,flight:true);
            // The Last Warden: met at Sync 20 to 21 (the chapter runs 19 to 22; OrielBuilder.Enemies' ledger), measured at the lower. Its door is the engine hall's (docs/maps/GlassChoir.md,
            // "Boss arena contract"): the release has been read at the first clamp and the Warden has spoken (WardenFirst).
            book.Boss(new ContentRegistry.BossSheet{id="LastWarden",zone="GlassChoir",door="LastWarden",doorFlags=new[]{"oriel.releaseLearned","oriel.wardenMet"},sync=20,driverShare=.3f,
                // In the yard every strike runs down a line of its own spun toward the lead, so a hero six metres before it is on the line it plucks, the three it strums, the one it draws
                // and the spokes it tolls.
                specials=new[]{("Pluck",new Vector3(0,0,6)),("Chord",new Vector3(0,0,6)),("Draw",new Vector3(0,0,6)),("Toll",new Vector3(0,0,6))},
                // A Draw at a hero past its 16 m line closes on nobody and fouls its staff; slackening, its Toll leaves the staff ringing in the floor.
                windows=new[]{("Draw",new Vector3(0,0,20)),("Toll",new Vector3(0,0,6))}});
        }

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static bool Set(string key)=>GameServices.Current.Flags.GetBool(key);
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            Hold();yield return new WaitForSecondsRealtime(.5f);
        }
        /// <summary>The heroes out are the test's to move: no pad, no partner's brain.</summary>
        static void Hold(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        static IEnumerator Deploy(params string[] pair)
        {
            Assert.IsTrue(Deployment.Set(State,pair),"fixture: the pair "+string.Join(" and ",pair)+" can be put out");
            PartyField.Reform(PartyController.Current);yield return Frames(3);PartyController.Current.RefreshStats();yield return null;Hold();
            foreach(string hero in pair)Assert.IsTrue(PartyController.Current.members.Any(m=>m.character==hero),"fixture: "+hero+" is out");
        }
        static IEnumerator Level(int sync)
        {
            foreach(var member in State.party)member.level=sync;PartyController.Current.RefreshStats();yield return null;
            foreach(var hero in PartyController.Current.members)Assert.AreEqual(Levels.MaxIntegrity(sync),hero.Health.maximum,.5f,"fixture: "+hero.character+"'s bar at Sync "+sync);
        }
        static Vector3 At(float x,float y,float z)=>new(X+x,y,z);
        static Vector3 Sky(float x,float z)=>new(200+x,1,z);
        static void Floor()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Oriel fixture floor";floor.transform.position=new Vector3(X,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static (CombatActor lead,CombatActor partner) Party(){var lead=PartyController.Current.Active;return (lead,PartyController.Current.members.First(m=>m!=lead));}
        static void Put(CombatActor actor,Vector3 position,bool fresh=true)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            if(fresh){actor.Health.maximum=actor.Health.integrity=5000;actor.Health.InvulnerableUntil=0;}Physics.SyncTransforms();
        }
        static void Stand(EnemyBrain enemy,Vector3 position,float yaw=0)
        {var body=enemy.GetComponent<CharacterController>();bool was=body.enabled;body.enabled=false;enemy.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));body.enabled=was;Physics.SyncTransforms();}
        static EnemyDef Def(string id){var definition=GameCatalog.Find<EnemyDef>(id);Assert.IsNotNull(definition,"fixture: "+id+" is defined (OrielBuilder.Definitions)");return definition;}
        static EnemyBrain Enemy(string id,Vector3 at,bool passive=true,float yaw=0){var enemy=ActorFactory.Enemy(Def(id),at);enemy.transform.rotation=Quaternion.Euler(0,yaw,0);enemy.Passive=passive;return enemy;}
        static DamageType Neutral(Health body)=>System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=body.weakness&&t!=body.resistance);
        static float Hit(Health body,CombatActor from,float amount=100,float breakPower=0,DamageType? type=null)=>body.Receive(new DamagePacket{source=from.Health,amount=amount,type=type??Neutral(body),breakPower=breakPower});
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>Waits on the fight's own clock, driving the heroes' motors as their brains would: a roll is only carried out by a motor that is being driven.</summary>
        static IEnumerator Until(System.Func<bool> done,float seconds,params CombatActor[] held)
        {
            for(float end=Time.unscaledTime+seconds*1.5f+1,until=Time.time+seconds;!done()&&Time.time<until&&Time.unscaledTime<end;)
            {foreach(var actor in held)if(actor!=null&&actor.Health.Alive)actor.motor.Move(Vector2.zero,false,false);yield return null;}
        }
        static IEnumerator Wait(float seconds,params CombatActor[] held)=>Until(()=>false,seconds,held);
        static bool Faint(Telegraph marking)=>marking.GetComponent<MeshRenderer>().sharedMaterials[1].name.Contains("faint");
        bool Said(string start)=>log.Any(l=>l.StartsWith(start));
        static Telegraph[] Marks(Object owner)=>Telegraph.Live.Where(t=>t!=null&&t.owner==owner&&!t.Released).ToArray();
        static EnemyBrain[] Alive(string id)=>Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>e.definition!=null&&e.definition.id==id&&e.Health.Alive).ToArray();

        /// <summary>A fibre on the floor, as the zones lay them (the contract's ForceLine).</summary>
        static ForceLine Fibre(Vector3 from,Vector3 to,int clamp=0)
        {var go=new GameObject("Fixture fibre");go.SetActive(false);go.transform.position=from;var line=go.AddComponent<ForceLine>();line.end=to;line.clamp=clamp;go.SetActive(true);return line;}
        /// <summary>A Bridle clamp as the contract stands one: on its seat, facing the hall's middle, answering to the Warden, its flag by its index, with its three states drawn.</summary>
        static BridleClamp Clamp(int index,Vector3 seat,Vector3 facing)
        {
            var go=new GameObject("Bridle clamp "+index);go.SetActive(false);go.transform.SetPositionAndRotation(seat,Quaternion.LookRotation(facing));
            var clamp=go.AddComponent<BridleClamp>();clamp.index=index;clamp.settledBy="LastWarden";clamp.openedFlag="test.clamp"+index;
            clamp.whole=new GameObject("whole");clamp.whole.transform.SetParent(go.transform,false);
            clamp.onCatch=new GameObject("on its catch");clamp.onCatch.transform.SetParent(go.transform,false);
            clamp.released=new GameObject("let go");clamp.released.transform.SetParent(go.transform,false);
            go.SetActive(true);return clamp;
        }
        /// <summary>The engine hall as the contract has it, round a middle: four clamps' seats 11 m out on bearings 45, 315, 225, 135 (indices 1 to 4), and twelve fibres from 2.6 m to
        /// 17.5 m every thirty degrees from 15, three to a clamp, the middle one of each under its clamp's seat.</summary>
        static (BridleClamp[] clamps,ForceLine[] fibres) Hall(Vector3 centre)
        {
            Vector3 On(float bearing,float r)=>centre+Quaternion.Euler(0,bearing,0)*Vector3.forward*r;
            var bearings=new[]{45f,315f,225f,135f};var clamps=new BridleClamp[4];
            for(int i=0;i<4;i++)clamps[i]=Clamp(i+1,On(bearings[i],11),(centre-On(bearings[i],11)).normalized);
            var fibres=new ForceLine[12];
            for(int k=0;k<12;k++)
            {
                float bearing=15+30*k;int owner=bearing<90?1:bearing<180?4:bearing<270?3:2;
                fibres[k]=Fibre(On(bearing,2.6f),On(bearing,17.5f),owner);
            }
            return (clamps,fibres);
        }
        /// <summary>Takes a clamp's weight with this hero (standing in its seat) and holds it, the boss's staff down, until it opens.</summary>
        static IEnumerator Open(BridleClamp clamp,CombatActor hero,Health boss)
        {
            Put(hero,clamp.transform.position+Vector3.up*.05f,false);yield return null;
            Assert.IsTrue(clamp.Available,$"fixture: clamp {clamp.index} can be worked");clamp.Interact();Assert.AreEqual(hero,clamp.Holder,"fixture: his hands are on it");
            if(boss!=null)boss.Expose(3);
            yield return Until(()=>clamp.State!=BridleClamp.Hold.Shut,4,hero);
            Assert.AreNotEqual(BridleClamp.Hold.Shut,clamp.State,$"clamp {clamp.index} held through the slack didn't open");
        }

        // ------------------------------------------------------------------ the chapter's loadout
        [UnityTest] public IEnumerator TheChapterSevenLoadoutArrivesAsThreeAtSyncNineteenInTemperedPartsWithTarenAndIdraOut()
        {
            // Built wrong on purpose: chapter 6's arrival is two heroes at 16 in Prime parts.
            if(Broken)DevLoadout.Apply("chapter6");
            yield return Zone("Arena_Ground");
            CollectionAssert.AreEquivalent(new[]{"Taren","Sela","Idra"},State.party.Select(m=>m.id).ToArray(),"chapter 7 arrives without its three heroes");
            foreach(var member in State.party)Assert.AreEqual(19,member.level,member.id+" arrives at the wrong Sync: the plan ends chapter 6 at 19");
            CollectionAssert.AreEquivalent(new[]{"Taren","Idra"},PartyController.Current.members.Select(m=>m.character).ToArray(),"the pair out isn't the one the join leaves out");
            foreach(var member in State.party)
            {
                string worn=member.equipped.TryGetValue(GearSlot.Frame,out var id)?State.parts.FirstOrDefault(p=>p.instanceId==id)?.definitionId:null;
                Assert.AreEqual("FramesT4",worn,member.id+" doesn't arrive in the Tempered frame chapter 6 sells");
            }
        }

        // ------------------------------------------------------------------ the numbers the chapter is tuned by
        /// <summary>The standing rule (CHAPTER_GUIDE section 0.1): an ordinary hit costs a hero 9 to 13% of the bar, a boss's move 12 to 16 and more than anything on the road to it. Measured
        /// through Health.Receive as a real blow lands, Plating and all, on all three heroes (Idra, the guard hero, is built with more Plating, so she is the floor and Taren and Sela the
        /// ceiling): every ordinary attack of the chapter at Sync 19 and 22 in the parts it arrives in (Tempered), and the Warden's at 20 and 21, where it is met; its second phase's +18%
        /// stays under a fifth. Every chapter 7 zone is at threat 1, every capsule is at least twice its radius tall, and no guardian's arc reaches 180 degrees.</summary>
        [UnityTest] public IEnumerator EveryOrdinaryHitTakesANinthToAnEighthOfAnyHerosBarAndTheWardensMovesABossesShare()
        {
            yield return Zone("Arena_Ground");Floor();
            var source=Enemy("ClampTender",At(40,.05f,40)).Health;yield return Frames(2);
            var ordinary=new[]{"RailWarden","Moonshard","Pruner","FibreWisp","Glassback","GlassSentinel","ClampTender"};
            var attacks=ordinary.SelectMany(id=>Def(id).attacks.Select(a=>(id,what:a.name,damage:a.damage))).ToList();
            // Built wrong on purpose: a hit authored at 106 is over the band at 19, and the check has to say so.
            if(Broken)attacks.Add(("fixture","a hit authored at 106",106));
            var warden=new (string what,float damage,bool later)[]{("its staff",Def("LastWarden").attacks[0].damage,false),("Pluck",BossController.WardenTune.PluckDamage,false),
                ("Chord",BossController.WardenTune.ChordDamage,false),("Draw's snap",BossController.WardenTune.SnapDamage,false),("Toll",BossController.WardenTune.TollDamage,true)};
            var failures=new List<string>();
            foreach(var pair in new[]{new[]{"Taren","Idra"},new[]{"Sela","Idra"}})
            {
                yield return Deploy(pair);
                foreach(int sync in new[]{19,20,21,22})
                {
                    yield return Level(sync);
                    foreach(var hero in PartyController.Current.members)
                    {
                        if(pair[0]=="Sela"&&hero.character=="Idra")continue;
                        float bar=hero.Health.maximum;
                        float Share(float damage){hero.Health.integrity=bar;hero.Health.InvulnerableUntil=0;return hero.Health.Receive(new DamagePacket{source=source,amount=damage,type=DamageType.Kinetic,weight=HitWeight.Medium})/bar;}
                        float road=0;string heaviest="";
                        foreach(var (id,what,damage) in attacks)
                        {
                            float share=Share(damage);if(share>road&&id!="fixture"){road=share;heaviest=what;}
                            string line=$"ORIEL_HIT {hero.character} sync={sync} plating={hero.plating:0} {id} {what} {damage:0} share={share:P1}";
                            if(sync==19||sync==22){TestContext.WriteLine(line);if(share<.09f||share>.13f)failures.Add(line+": the plan asks 9 to 13%");}
                        }
                        if(sync==20||sync==21)foreach(var (what,damage,later) in warden)
                        {
                            // Both halves: its second half adds what its tune says (nothing), and each blow is held to the same 12 to 16%. Built wrong on purpose: the shared +18%.
                            float slack=Share(damage*(1+(Broken?BossController.PhaseStep:BossController.WardenTune.SlackBonus))),share=Share(damage);
                            string line=$"ORIEL_BOSS_HIT {hero.character} sync={sync} {what} {damage:0} share={share:P1} slackening={slack:P1} road={heaviest} {road:P1}";TestContext.WriteLine(line);
                            foreach(var (half,part) in new[]{("held",share),("slackening",slack)})
                            {
                                if(part<.12f||part>.16f)failures.Add(line+$": {half}, a boss's move is 12 to 16% of the bar");
                                if(part<=road)failures.Add(line+$": {half}, no more than the road to it");
                            }
                        }
                    }
                }
            }
            foreach(string zone in new[]{"Orrery","OrielSkyfields","GlassChoir"})
            {var z=GameCatalog.Find<ZoneDef>(zone);if(z!=null&&!Mathf.Approximately(z.enemyThreat,1))failures.Add($"{zone} is at threat {z.enemyThreat}: chapter 7's damage is authored at its real weight, at 1");}
            foreach(string id in ordinary.Append("LastWarden"))
            {
                var def=Def(id);float round=def.boss?BossController.CapsuleRadius(id):def.capsuleRadius;
                if(def.capsuleHeight<round*2)failures.Add($"{id}'s capsule is {def.capsuleHeight} m tall on a {round} m radius: Unity stretches it and the body floats");
                if(def.behaviour==EnemyBehaviourKind.Guardian&&def.tune.arc>=180)failures.Add($"{id}'s guarded arc is {def.tune.arc}: a flank blow must land whole");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        /// <summary>The shared tell tests (QaPlanCombatTests.EveryGroundEnemyTellsBeforeItLands and its flying twin) walk every enemy there is in one yard, one after another, and what an
        /// earlier one leaves on the floor or on the hero (a burning wake, a pin, a downed hero swapped out) has twice been read there as a later one's fault. This holds each of the chapter's
        /// seven to the same eye in a yard loaded fresh for it, at the distance its expectations give: its attack starts with a marking on the ground under the hero, lasting half a second
        /// or more; its body winds up; and only then is he hurt, inside what was marked.</summary>
        [UnityTest] public IEnumerator EachOfTheChaptersEnemiesByItselfMarksTheHeroWindsUpAndHitsOnlyAfterItsTell()
        {
            var failures=new List<string>();
            // Built wrong on purpose: the Glass Sentinel's hero stands 40 m off, past the 10 m it keeps round its post and its halberd's 10 m beyond, so it never tells at him and the check
            // has to say so. (At 12 m, the first try, it stepped in and swept its halberd at him fairly: a 1.1 s tell and the blade 1.43 s in.)
            var table=new (string id,float distance,bool flight)[]{("FibreWisp",3,false),("Pruner",2.5f,false),("Glassback",5,false),("GlassSentinel",Broken?40:2,false),("ClampTender",2,false),
                ("RailWarden",13,true),("Moonshard",6,true)};
            foreach(var (id,distance,flight) in table)
            {
                yield return Zone(flight?"Arena_Flight":"Arena_Ground");if(!flight)Floor();
                var (hero,partner)=Party();float y=flight?1:.05f;
                Put(partner,flight?Sky(60,60):At(-50,y,-50));Put(hero,flight?Sky(0,distance):At(0,y,distance));
                var body=Enemy(id,flight?Sky(0,0):At(0,y,0));yield return Frames(2);
                var show=body.GetComponent<DefeatPresentation>().visual;var rig=body.GetComponentInChildren<GeneratedAnimator>();var rest=show.localRotation;float restScale=show.localScale.x;
                yield return new WaitForSecondsRealtime(.7f);
                float bitten=-1;System.Action<Health,DamagePacket,float> hurt=(_,__,amount)=>{if(bitten<0&&amount>0)bitten=Time.time;};hero.Health.Damaged+=hurt;
                try
                {
                    body.Passive=false;
                    for(float end=Time.unscaledTime+3;!body.Telegraphing&&bitten<0&&Time.unscaledTime<end;){hero.motor.Move(Vector2.zero,false,false);yield return null;}
                    if(!body.Telegraphing){failures.Add(bitten>=0?$"a {id} hurt the hero with no tell at all":$"a {id} stood {distance} m from the hero for 3 s and never told an attack");continue;}
                    float toldAt=Time.time,told=body.TellDuration;var mark=body.AttackMark;
                    TestContext.WriteLine($"ORIEL_TELL {id} distance={distance} tell={told:0.00} shape={(mark!=null?mark.Shape.ToString():"none")}");
                    if(told<.5f)failures.Add($"a {id}'s tell lasts {told:0.00} s: too short to read and dodge");
                    if(mark==null)failures.Add($"a {id}'s attack marks nothing on the ground");
                    else if(!mark.Contains(hero.transform.position))failures.Add($"a {id}'s marking doesn't reach the hero it is aimed at, {distance} m away");
                    bool windingUp=false;float lean=0,swell=restScale;
                    for(float end=Time.unscaledTime+2.5f;body!=null&&body.Telegraphing&&Time.unscaledTime<end;)
                    {
                        windingUp|=rig!=null&&rig.CurrentAnimation=="Attack";
                        lean=Mathf.Max(lean,Vector3.Angle(rest*Vector3.forward,show.localRotation*Vector3.forward));swell=Mathf.Max(swell,show.localScale.x);
                        hero.motor.Move(Vector2.zero,false,false);yield return null;
                    }
                    if(rig!=null?!windingUp:lean<6&&swell<restScale*1.1f)failures.Add($"a {id}'s body doesn't move through its tell (lean {lean:0} degrees, swell {swell/restScale:0.00})");
                    for(float end=Time.unscaledTime+2;bitten<0&&Time.unscaledTime<end;){hero.motor.Move(Vector2.zero,false,false);yield return null;}
                    TestContext.WriteLine($"ORIEL_TELL_HIT {id} after={(bitten>=0?bitten-toldAt:-1):0.00}");
                    if(bitten<0)failures.Add($"a {id} told an attack at a hero standing in its marking and never hit him");
                    else if(bitten-toldAt<told-.06f)failures.Add($"a {id} hit the hero {bitten-toldAt:0.00} s into a {told:0.00} s tell");
                }
                finally{hero.Health.Damaged-=hurt;}
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ the families the chapter added or changed
        [UnityTest] public IEnumerator AFibreWispLightsTheFibreItRidesStrikesOnlyWhoStandsOnItAndHopsOpenToTheFibreHeIsOn()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();
            // Three fibres north to south across the floor, 7 m apart, as the Skyfields lays them. Built wrong on purpose: none, and the wisp rides a line of its own.
            var fibres=Broken?new ForceLine[0]:new[]{Fibre(At(-7,.05f,-12),At(-7,.05f,12)),Fibre(At(0,.05f,-12),At(0,.05f,12)),Fibre(At(7,.05f,-12),At(7,.05f,12))};
            var wisp=Enemy("FibreWisp",At(0,.05f,-4));yield return Frames(3);var rider=wisp.Behaviour as EnemyRider;Assert.IsNotNull(rider,"a Fibre Wisp isn't a rider");
            Put(hero,At(0,.05f,3));Put(partner,At(3.6f,.05f,3));
            wisp.Passive=false;yield return Until(()=>wisp.Telegraphing,4,hero,partner);
            Assert.IsTrue(wisp.Telegraphing,"it never lit the fibre a hero stood on");
            Assert.AreEqual(fibres.Length>0?fibres[1]:null,rider.Line,"it doesn't ride the fibre it was set on");
            var lit=wisp.AttackMark;Assert.AreEqual(Telegraph.Kind.Line,lit.Shape,"its tell isn't the fibre lit");
            Assert.Less(Vector3.Angle(lit.Forward,Vector3.forward),3,"what it lit doesn't run down its fibre");Assert.Greater(fibres[1].Glow,0,"the fibre itself doesn't brighten");
            Assert.IsTrue(lit.Contains(hero.transform.position),"fixture: he stands on the lit fibre");Assert.IsFalse(lit.Contains(partner.transform.position),"fixture: his partner stands between two fibres");
            yield return Until(()=>!wisp.Telegraphing,2,hero,partner);yield return Frames(2);
            Assert.Less(hero.Health.integrity,5000,"the shock ran down the fibre and missed a hero on it");Assert.AreEqual(5000,partner.Health.integrity,.01f,"the shock reached a hero off the fibre");
            // Then it hops to the fibre he is on (he has stepped onto the east one), open while it is in the air.
            Put(hero,At(7,.05f,4),false);
            yield return Until(()=>rider.Hopping,4,hero,partner);Assert.IsTrue(rider.Hopping,"it never hopped to the fibre he stepped onto");
            Assert.IsTrue(wisp.Health.Broken,"hopping, it isn't open");Assert.AreEqual(150,Hit(wisp.Health,hero),1,"a blow on it in the air isn't an opening's half as much again");
            yield return Until(()=>!rider.Hopping,3,hero,partner);
            Assert.AreEqual(fibres[2],rider.Line,"it landed on some other line than the fibre he is on");Assert.Less(fibres[2].Distance(wisp.transform.position),.6f,"it isn't on its new fibre");
        }
        [UnityTest] public IEnumerator ARailWardenFiresOnlyDownItsRailAtAShipOnItAhead()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();
            // Set facing east along its rail, as the Orrery stands it. Built wrong on purpose: facing the ship that is off the rail, which then sits on its rail.
            var warden=Enemy("RailWarden",Sky(0,0),yaw:Broken?53.13f:90);yield return Frames(3);Assert.IsInstanceOf<EnemyRider>(warden.Behaviour,"a Rail Warden isn't a rider");
            Put(hero,Sky(8,6));Put(partner,Sky(-40,-40));
            warden.Passive=false;
            // Beside its rail, however near: nothing.
            yield return Wait(2.5f,hero,partner);Assert.IsFalse(Said("RIDER_LIT"),"it fired at a ship beside its rail");Assert.AreEqual(5000,hero.Health.integrity,.01f,"a ship beside its rail was struck");
            // On its rail ahead of it: the rail lit and a bolt down it.
            Put(hero,Sky(9,0));Put(partner,Sky(8,6));
            yield return Until(()=>warden.Telegraphing,4,hero,partner);Assert.IsTrue(warden.Telegraphing,"it never fired at a ship on its rail ahead of it");
            var rail=warden.AttackMark;Assert.AreEqual(Telegraph.Kind.Line,rail.Shape);Assert.Less(Vector3.Angle(rail.Forward,Vector3.right),3,"what it marked doesn't run along its rail");
            Assert.IsTrue(rail.Contains(hero.transform.position),"its rail isn't marked where the ship sits");
            yield return Until(()=>hero.Health.integrity<5000,3,hero,partner);
            Assert.Less(hero.Health.integrity,5000,"its bolt went down the rail and missed the ship on it");Assert.AreEqual(5000,partner.Health.integrity,.01f,"its bolt struck a ship off its rail");
        }
        [UnityTest] public IEnumerator TheRailWardensRodRunsAlongItsRunAndItsFinsOutToItsSides()
        {
            // The body is a spindle with a rod through its bore (the rail it rides) and two swept fins, and the fins span as wide as the rod is long: seen from above it is as broad as it
            // is long (2.6 by 2.6 m), so its outline can't say which way it runs. What can: out past the barrel, the rod is thin as a rail and the fins are broad. The intake turns it
            // -90 degrees so the rod runs along its forward, the way it glides and fires. Built wrong on purpose: turned a quarter.
            var drawn=Object.Instantiate(Def("RailWarden").prefab);drawn.transform.rotation=Quaternion.Euler(0,Broken?90:0,0);yield return null;
            var points=drawn.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh!=null).SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToList();
            Object.Destroy(drawn);Assert.IsNotEmpty(points,"fixture: the Rail Warden has a body");
            var box=new Bounds(points[0],Vector3.zero);foreach(var p in points)box.Encapsulate(p);
            // How thick it is out past the barrel along one way: across the other way and up, of every point beyond half its half-length out (the barrel reaches about .42 of it).
            // As built: along the rod about .07 by .07 m, along a fin about .14 by .38.
            (float across,float up) Outer(Vector3 way,Vector3 side)
            {
                float half=Mathf.Abs(Vector3.Dot(box.extents,way));var beyond=points.Where(p=>Mathf.Abs(Vector3.Dot(p-box.center,way))>half/2).ToList();
                if(beyond.Count==0)return (0,0);
                return (beyond.Max(p=>Vector3.Dot(p,side))-beyond.Min(p=>Vector3.Dot(p,side)),beyond.Max(p=>p.y)-beyond.Min(p=>p.y));
            }
            var run=Outer(Vector3.forward,Vector3.right);var sides=Outer(Vector3.right,Vector3.forward);
            TestContext.WriteLine($"RAIL_WARDEN_DRAWN x={box.size.x:0.00} z={box.size.z:0.00} alongRun={run.across:0.00}x{run.up:0.00} toSides={sides.across:0.00}x{sides.up:0.00}");
            Assert.Less(Mathf.Max(run.across,run.up),.15f,"what reaches out along its run isn't its rod: the rail it rides would cut through it side to side");
            Assert.Greater(Mathf.Max(sides.across,sides.up),2*Mathf.Max(run.across,run.up),"what reaches out to its sides isn't broader than its rod: its fins don't stand out to its sides");
        }
        [UnityTest] public IEnumerator AMoonshardRunsItsMarkedTrackAheadOnceAndIsSpentPayingNothing()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();
            // Calved facing north, along the moon's way. The ship beside the track is the nearer: it runs its track whoever is about. Built wrong on purpose: the Orrery's stand-in, a mine.
            Put(partner,Sky(4.5f,6));Put(hero,Sky(0,16));int xp=State.party[0].xp;
            var shard=Enemy(Broken?"Shellmine":"Moonshard",Sky(0,0));yield return Frames(3);shard.Passive=false;
            yield return Until(()=>shard.Telegraphing,3,hero,partner);Assert.IsTrue(shard.Telegraphing,"it never marked its track");
            var track=shard.AttackMark;Assert.AreEqual(Telegraph.Kind.Line,track.Shape,"its track isn't marked as a lane");Assert.Less(Vector3.Angle(track.Forward,Vector3.forward),2,"its track isn't ahead of it");
            Assert.Greater(track.Length,25,"its track is shorter than the stretch ahead of a moon");Assert.IsFalse(track.Contains(partner.transform.position),"fixture: the near ship is beside the track");
            yield return Until(()=>Said("CHARGER_SPENT"),4,hero,partner);
            Assert.IsTrue(Said("CHARGER_SPENT Moonshard"),"it ran its track and wasn't spent");Assert.IsFalse(shard.Health.Alive,"spent, it is still there");
            Assert.Less(hero.Health.integrity,5000,"it ran its track and missed the ship on it");Assert.AreEqual(5000,partner.Health.integrity,.01f,"it struck a ship beside its track");
            Assert.AreEqual(xp,State.party[0].xp,"a shard spent by itself paid the party");
            // Shot down first, it is a kill like any other.
            var second=Enemy("Moonshard",Sky(-20,0));yield return Frames(3);Hit(second.Health,hero,second.Health.maximum*2);yield return Frames(2);
            Assert.Greater(State.party[0].xp+State.party[0].level*1000,xp+19*1000,"a shard shot down paid nothing");
        }
        [UnityTest] public IEnumerator APrunerTurnsItsShearsOnARingWithAHoleInItsMiddleAndIsGiddyAfter()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();
            // Built wrong on purpose: a Bogstrider stomps a circle before it, with no hole in it.
            var pruner=Enemy(Broken?"Bogstrider":"Pruner",At(0,.05f,0));yield return Frames(3);
            Put(hero,At(0,.05f,2.6f));Put(partner,At(1.05f,.05f,-.3f));
            pruner.Passive=false;yield return Until(()=>pruner.Telegraphing,4,hero,partner);Assert.IsTrue(pruner.Telegraphing,"it never opened its shears at a hero inside its reach");
            var ring=pruner.AttackMark;Assert.AreEqual(Telegraph.Kind.Ring,ring.Shape,"its cut isn't marked as a ring");
            Assert.AreEqual(Def("Pruner").tune.width,ring.Inner,.05f,"the ring's hole isn't the handles' reach");Assert.AreEqual(Def("Pruner").attacks[0].radius,ring.Radius,.05f,"the ring isn't the blades' reach");
            Assert.IsTrue(ring.Contains(hero.transform.position),"fixture: he stands under its blades");Assert.IsFalse(ring.Contains(partner.transform.position),"fixture: his partner hugs it, inside the handles");
            yield return Until(()=>Said("SPINNER_GIDDY"),3,hero,partner);
            Assert.Less(hero.Health.integrity,5000,"its blades missed a hero under them");Assert.AreEqual(5000,partner.Health.integrity,.01f,"its blades reached a hero inside the handles");
            Assert.IsTrue(pruner.Health.Broken,"giddy, it isn't open");Assert.That(pruner.Health.BrokenUntil-Time.time,Is.InRange(1.2f,1.65f),"giddy for 1.6 s");
            Assert.AreEqual(150,Hit(pruner.Health,hero),1,"a blow on it giddy isn't an opening's half as much again");
        }
        [UnityTest] public IEnumerator AGlassSentinelThrowsAShotOnItsFrontBackDownAMarkedLineAndNotOneFromItsFlank()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            // Built wrong on purpose: a Drowned Sentry's shield is only a shield.
            var glass=Enemy(Broken?"DrownedSentry":"GlassSentinel",At(0,.05f,0));yield return Frames(3);var guardian=glass.Behaviour as EnemyGuardian;
            // From its flank: a shot is a shot.
            Put(hero,At(9,.05f,0));Hit(glass.Health,hero,100,0,DamageType.Beam);yield return Frames(2);
            Assert.IsFalse(glass.Telegraphing,"a shot from its flank was thrown back");
            // From before it, a blade's blow: a blow is a blow.
            Put(hero,At(0,.05f,9));Hit(glass.Health,hero,100,0,DamageType.Kinetic);yield return Frames(2);Assert.IsFalse(glass.Telegraphing,"a blow that wasn't a shot was thrown back");
            // From before it, a shot: gathered, and thrown back down a marked line at him.
            Hit(glass.Health,hero,100,0,DamageType.Beam);yield return Frames(2);
            Assert.IsTrue(glass.Telegraphing&&guardian!=null&&guardian.Throwing,"a shot on its glass front wasn't gathered to be thrown back");glass.Passive=false;
            var line=glass.AttackMark;Assert.AreEqual(Telegraph.Kind.Line,line.Shape,"the throw-back isn't marked as a line");Assert.IsTrue(line.Contains(hero.transform.position),"the throw-back isn't marked at the shooter");
            Assert.GreaterOrEqual(glass.TellDuration,.5f,"the throw-back comes too soon to read");
            yield return Until(()=>hero.Health.integrity<5000,3,hero);
            Assert.Less(hero.Health.integrity,5000,"the shot thrown back missed the shooter it was marked at");Assert.IsTrue(Said("GUARD_THROWBACK GlassSentinel"));
        }
        [UnityTest] public IEnumerator AGlassbackShedsItsPlatesAsCaltropsWhereItStopsAndTheyCut()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,7));
            // Built wrong on purpose: a Thornback's run leaves nothing behind it.
            var beast=Enemy(Broken?"Thornback":"Glassback",At(0,.05f,0));yield return Frames(3);beast.Passive=false;
            yield return Until(()=>beast.Telegraphing,4,hero);Assert.IsTrue(beast.Telegraphing,"fixture: it charges a hero seven metres off");
            // He steps aside as the lane fills.
            Put(hero,At(5,.05f,4),false);
            yield return Until(()=>LingeringArea.All.Count>0,4,hero);
            var plates=LingeringArea.All.FirstOrDefault();Assert.IsNotNull(plates,"its run ended and it shed nothing");
            Assert.AreEqual(AreaEffect.Burn,plates.Effect,"its plates don't cut");Assert.Less(Flat(plates.transform.position,beast.transform.position),1,"it shed its plates somewhere other than where it stopped");
            var ring=Telegraph.Live.FirstOrDefault(t=>t!=null&&t.owner==plates);Assert.IsNotNull(ring,"its plates aren't drawn");Assert.IsFalse(Faint(ring),"its plates cut and are drawn as a boundary");
            beast.Passive=true;Put(hero,plates.transform.position+Vector3.up*.05f,false);float before=hero.Health.integrity;
            yield return Wait(1.2f,hero);Assert.Less(hero.Health.integrity,before,"a hero standing in its plates isn't cut");
        }
        [UnityTest] public IEnumerator AClampTenderGoesToTheClampAHeroIsWorkingAndClosesWhatHeTookUntilItIsStopped()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            var clamp=Clamp(1,At(0,.05f,0),Vector3.back);yield return Frames(2);BridleClamp.Offer(true);
            // With nothing it keeps in reach, its torch. Built wrong on purpose: the clamp it should keep is out of its sight.
            var tender=Enemy("ClampTender",At(Broken?40:-9,.05f,6));yield return Frames(3);
            Put(hero,clamp.transform.position+Vector3.up*.05f);yield return null;clamp.Interact();Assert.AreEqual(hero,clamp.Holder,"fixture: his hands are on the clamp");
            tender.Passive=false;
            yield return Until(()=>clamp.Tended.Tending,8,hero);
            Assert.IsTrue(clamp.Tended.Tending,"the tender never came to the clamp a hero was working");Assert.IsTrue(Said("TENDER_CLOSING ClampTender"),"it closed a clamp in a hero's hands with no word from him");
            // While it works the clamp, what he takes drains and the slack never comes.
            BridleClamp.Slack=true;yield return Wait(2.5f,hero);
            Assert.AreEqual(BridleClamp.Hold.Shut,clamp.State,"the clamp opened with a tender at work on it");Assert.Less(clamp.Weight,.1f,"what he took of its weight held with a tender closing it");
            Assert.AreEqual(5000,hero.Health.integrity,.01f,"at work, the tender burned the hero");
            // Stopped (it is fragile), and the clamp opens in his hands.
            Assert.LessOrEqual(Def("ClampTender").integrity,450,"the tender isn't fragile");
            Hit(tender.Health,hero,tender.Health.maximum*3);yield return Until(()=>clamp.State!=BridleClamp.Hold.Shut,4,hero);
            Assert.AreEqual(BridleClamp.Hold.Open,clamp.State,"with the tender stopped, the clamp held through the slack didn't open");
            // With nothing to keep, it torches a hero before it.
            var second=Enemy("ClampTender",At(20,.05f,0));yield return Frames(3);Put(hero,At(20,.05f,2));second.Passive=false;
            yield return Until(()=>second.Telegraphing,4,hero);Assert.IsTrue(second.Telegraphing,"with nothing to keep, it never torched a hero before it");
            Assert.AreEqual(Telegraph.Kind.Cone,second.AttackMark.Shape,"its torch isn't marked as a fan");
        }

        // ------------------------------------------------------------------ the clamp
        [UnityTest] public IEnumerator AClampIsWorkedOnlyOnceOfferedOpensOnlyHeldThroughTheSlackAndItsFibresGoSlack()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            var clamp=Clamp(2,At(0,.05f,0),Vector3.back);var mine=Fibre(At(0,.05f,-3),At(0,.05f,12),2);var other=Fibre(At(6,.05f,-3),At(6,.05f,12),1);yield return Frames(2);
            Put(hero,clamp.transform.position+Vector3.up*.05f);PromptService.FindInteraction(hero.transform.position);
            Assert.IsTrue(clamp.InFight,"a clamp doesn't answer with the boss bar up");Assert.AreEqual("Take the clamp's weight",clamp.prompt);
            // Not offered (its boss's hold is above its line): nothing to take.
            Assert.IsFalse(clamp.Available,"a clamp can be worked before its boss's hold is down to it");clamp.Interact();Assert.IsNull(clamp.Holder,"a clamp took a hero's hands before it was offered");
            // Built wrong on purpose: never offered, and nothing below can happen.
            if(!Broken)BridleClamp.Offer(true);yield return null;
            Assert.IsTrue(clamp.Available,"offered, it can't be worked");Assert.IsTrue(Marks(clamp).Any(Faint),"offered, its seat isn't drawn as a pale ring");
            Assert.IsFalse(Marks(clamp).Any(m=>!Faint(m)),"its seat is drawn as a danger");
            clamp.Interact();Assert.AreEqual(hero,clamp.Holder,"his hands aren't on it");Assert.IsTrue(Said("CLAMP_TAKEN 2"));
            // It pulls: its weight is taken, and without the slack it never opens.
            yield return Wait(2.6f,hero);Assert.AreEqual(1,clamp.Weight,.01f,"held, its weight isn't taken");Assert.AreEqual(BridleClamp.Hold.Shut,clamp.State,"it opened against the pull");
            // A roll lets go, and what was taken drains.
            hero.Dodge(Vector3.left);yield return Wait(.6f,hero);Assert.IsNull(clamp.Holder,"a roll didn't let go of it");Assert.Less(clamp.Weight,1,"let go, what was taken holds");
            // Taken again and held through the slack: it opens onto its catch.
            Put(hero,clamp.transform.position+Vector3.up*.05f,false);yield return null;clamp.Interact();BridleClamp.Slack=true;
            yield return Until(()=>clamp.State!=BridleClamp.Hold.Shut,4,hero);
            Assert.AreEqual(BridleClamp.Hold.Open,clamp.State,"held through the slack, it didn't open");Assert.IsTrue(Said("CLAMP_EASED 2"));
            Assert.IsTrue(clamp.onCatch.activeSelf&&!clamp.whole.activeSelf,"opened, it isn't drawn on its catch");Assert.IsTrue(Set("test.clamp2"),"opened, its flag isn't set");
            Assert.IsTrue(mine.Slack,"its fibre still carries force");Assert.IsFalse(other.Slack,"another clamp's fibre went slack with it");
            Assert.AreEqual(clamp.Tended.Worked,0,.01f);Assert.IsFalse(clamp.Available,"opened, it is still offered");
            // All of them open: they let go together.
            BridleClamp.LetGoTogether();Assert.AreEqual(BridleClamp.Hold.Released,clamp.State);Assert.IsTrue(clamp.released.activeSelf,"let go, it isn't drawn let go");
        }

        // ------------------------------------------------------------------ the Last Warden
        [UnityTest] public IEnumerator TheWardensHoldStopsAtEachClampsLineAndOnlyAClampTakesItOverTheSecondIsTheShift()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-6));
            // Built wrong on purpose: a hall with no clamps, where nothing holds its bar.
            var hall=Broken?(new BridleClamp[0],new ForceLine[0]):Hall(At(0,0,0));yield return Frames(2);
            var warden=Enemy("LastWarden",At(0,.1f,0));yield return Frames(3);var boss=warden.GetComponent<BossController>();var body=warden.Health;float whole=body.maximum;
            Assert.AreEqual(4,boss.ClampsTotal,"the clamps don't answer to it");
            foreach(var clamp in hall.Item1)Assert.IsFalse(clamp.Available,"a clamp is offered before its hold is down to it");
            StringAssert.Contains("(0/4)",boss.FightGoal(""),"the objective doesn't count the clamps");
            // Fought down: it stops at the first line, and holds there.
            Hit(body,hero,whole);yield return Frames(2);
            Assert.AreEqual(whole*.75f+1,body.integrity,2,"its hold didn't stop at the first clamp's line");Assert.IsTrue(boss.RoadHeld,"at its line the road isn't held");
            StringAssert.Contains("THE ROAD IS HELD",boss.StatusText,"its bar doesn't say the road is held");StringAssert.Contains("OPEN A CLAMP",boss.StatusText);
            StringAssert.Contains("Open a clamp while the staff is down (0/4)",boss.FightGoal(""));Assert.IsTrue(Said("WARDEN_HOLDS shut=4"));
            foreach(var clamp in hall.Item1)Assert.IsTrue(clamp.Available,"at its line a clamp isn't offered");
            var kinds=new List<HitKind>();System.Action<Health,float,HitKind> seen=(victim,_,kind)=>{if(victim==body)kinds.Add(kind);};Health.DamageNumber+=seen;
            try{Assert.AreEqual(0,Hit(body,hero),.01f,"a blow on a held line takes something off it");Assert.AreEqual(HitKind.Resisted,kinds.Last(),"a blow on a held line doesn't read as resisted");}
            finally{Health.DamageNumber-=seen;}
            // A break still drops its staff, and a blow in that opening takes nothing off the line either: a held line is never a kill.
            Hit(body,hero,1,100000);Assert.IsTrue(body.Broken,"at its line, a break doesn't drop its staff");Hit(body,hero,whole*10);Assert.AreEqual(whole*.75f+1,body.integrity,2,"a blow in an opening took its hold over a line no clamp has opened");
            Assert.IsTrue(body.Alive);
            // The first clamp opened: the Warden counts it, and its hold goes over the line.
            var spoken=new List<string>();System.Action<string,string> heard=(who,line)=>spoken.Add(who+": "+line);BarkService.Spoken+=heard;
            try{yield return Open(hall.Item1[0],hero,body);}finally{BarkService.Spoken-=heard;}
            Assert.Less(body.integrity,whole*.75f,"the first clamp didn't take its hold over the line");Assert.IsTrue(Said("WARDEN_COUNTS 1"));
            Assert.IsTrue(spoken.Any(s=>s.StartsWith("Warden: One clamp is opened.")),"it doesn't count the clamp: "+string.Join(", ",spoken));
            StringAssert.Contains("(1/4)",boss.FightGoal(""));Assert.IsFalse(boss.RoadHeld);
            // Down to half, held; the second clamp is the shift: two tenders step out beside the clamps still shut.
            yield return Until(()=>!body.Broken,4,hero);Hit(body,hero,whole);yield return Frames(2);
            Assert.AreEqual(whole*.5f+1,body.integrity,2,"its hold didn't stop at the second clamp's line");
            warden.Passive=false;hero.Health.InvulnerableUntil=GameTime.Now+60;partner.Health.InvulnerableUntil=GameTime.Now+60;
            yield return Open(hall.Item1[2],hero,body);
            yield return Until(()=>Said("BOSS_SHIFT LastWarden 2"),6,hero);Assert.IsTrue(Said("BOSS_SHIFT LastWarden 2"),"the second clamp opened and it never shifted");
            Assert.IsTrue(Said("WARDEN_SLACKENS"));warden.Passive=true;
            var tenders=Alive("ClampTender");Assert.AreEqual(2,tenders.Length,"its shift didn't call a tender out for each clamp still shut");
            foreach(var tender in tenders)Assert.Less(new[]{hall.Item1[1],hall.Item1[3]}.Min(c=>Flat(c.transform.position,tender.transform.position)),3.5f,"a tender stepped out somewhere other than beside a clamp still shut");
        }
        [UnityTest] public IEnumerator ItsStrikesRunDownTheFibresItPlucksHisStrumsThreeAndSpinsItsOwnWhereTheFloorIsClear()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            // Built wrong on purpose: every fibre in the hall slack, so every line it strikes is its own.
            var hall=Hall(At(0,0,0));if(Broken)foreach(var fibre in hall.Item2)fibre.GoSlack();yield return Frames(2);
            var warden=Enemy("LastWarden",At(0,.1f,0));yield return Frames(3);var boss=warden.GetComponent<BossController>();
            // On the fibre at bearing 15 (fibres[0]), 8 m out.
            var on=At(0,0,0)+Quaternion.Euler(0,15,0)*Vector3.forward*8;on.y=.05f;Put(hero,on);
            Assert.IsTrue(boss.Perform("Pluck"),"fixture: its pluck");yield return null;
            var lane=Marks(boss).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Line);Assert.IsNotNull(lane,"its pluck marks nothing");
            Assert.Less(Flat(lane.Origin,hall.Item2[0].From),.3f,"its pluck doesn't start at the engine end of the fibre he stands on");
            Assert.Less(Vector3.Angle(lane.Forward,hall.Item2[0].Direction),2,"its pluck doesn't run down the fibre he stands on");Assert.Greater(hall.Item2[0].Glow,0,"the fibre it plucks doesn't brighten");
            Assert.AreEqual(BossController.WardenTune.PluckTell,lane.Duration,.01f,"a fibre of the hall's is lit as slowly as one of its own");
            yield return Until(()=>!boss.Busy,4,hero);Assert.Less(hero.Health.integrity,5000,"the force ran down his fibre and missed him");
            // Strummed: his and the two beside it, together. Between two of them, out where they part, nothing.
            yield return Wait(.3f,hero);Put(hero,on);var gap=At(0,0,0)+Quaternion.Euler(0,30,0)*Vector3.forward*9;gap.y=.05f;Put(partner,gap);
            Assert.IsTrue(boss.Perform("Chord"),"fixture: its chord");yield return null;
            var three=Marks(boss).Where(t=>t.Shape==Telegraph.Kind.Line).ToArray();Assert.AreEqual(3,three.Length,"its chord isn't three fibres");
            foreach(var fibre in new[]{hall.Item2[11],hall.Item2[0],hall.Item2[1]})Assert.IsTrue(three.Any(t=>Vector3.Angle(t.Forward,fibre.Direction)<2),"its chord isn't his fibre and the two beside it");
            Assert.IsFalse(three.Any(t=>t.Contains(partner.transform.position)),"fixture: his partner stands between two fibres");
            yield return Until(()=>!boss.Busy,4,hero,partner);Assert.AreEqual(5000,partner.Health.integrity,.01f,"its chord struck a hero standing between its fibres");
            // The north-east clamp's three gone slack: on that clear floor it spins its own, a beat slower to brighten.
            foreach(var fibre in hall.Item2.Where(f=>f.clamp==1))fibre.GoSlack();
            yield return Wait(.3f,hero);Put(hero,on);Put(partner,At(-50,.05f,-50));
            Assert.IsTrue(boss.Perform("Pluck"),"fixture: its pluck again");yield return null;
            lane=Marks(boss).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Line);Assert.IsNotNull(lane,"on clear floor its pluck marks nothing");
            Assert.Less(Flat(lane.Origin,warden.transform.position),BossController.WardenTune.Radius+.5f,"on clear floor its line isn't its own, spun from its staff");
            Assert.IsTrue(lane.Contains(hero.transform.position),"its own line isn't spun toward him");
            Assert.AreEqual(BossController.WardenTune.PluckTell+BossController.WardenTune.OwnSlower,lane.Duration,.01f,"a line of its own brightens as fast as the hall's");
        }
        [UnityTest] public IEnumerator ItsDrawPullsAlongAFibreToItsStaffAHeldClampKeepsItsHeroAndASnapOnNobodyFoulsTheStaffAndEasesTheClamp()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            var hall=Hall(At(0,0,0));yield return Frames(2);
            var warden=Enemy("LastWarden",At(0,.1f,0));yield return Frames(3);var boss=warden.GetComponent<BossController>();var body=warden.Health;
            // On a fibre, 8 m out: drawn in and snapped.
            var on=At(0,0,0)+Quaternion.Euler(0,135,0)*Vector3.forward*8;on.y=.05f;Put(hero,on);
            Assert.IsTrue(boss.Perform("Draw"),"fixture: its draw");yield return null;
            var marks=Marks(boss);var band=marks.FirstOrDefault(t=>t.Shape==Telegraph.Kind.Line);var snap=marks.FirstOrDefault(t=>t.Shape==Telegraph.Kind.Circle);
            Assert.IsNotNull(band,"its pull isn't marked along the fibre");Assert.IsTrue(Faint(band),"its pull is painted as a danger: it drags, it doesn't strike");
            Assert.IsNotNull(snap,"the snap at its staff isn't marked");Assert.IsFalse(Faint(snap),"the snap is painted as a boundary: it strikes");
            Assert.Less(Vector3.Angle(band.Forward,hall.Item2[4].Direction),2,"its pull isn't down the fibre he stands on");
            float from=Flat(hero.transform.position,warden.transform.position);
            yield return Until(()=>!boss.Busy,5,hero);
            Assert.Less(Flat(hero.transform.position,warden.transform.position),from-3,"its pull didn't draw him in");Assert.Less(hero.Health.integrity,5000,"drawn into the snap, he wasn't struck");
            Assert.IsFalse(Said("WARDEN_FOULED"),"a snap that took him fouled its staff");
            // Held: the south-east clamp's weight in his hands, the bar at its line. Its draw pulls that clamp's fibre; he is not moved, the snap closes on nobody, its staff is fouled,
            // and that is the slack: the clamp opens in his hands. Built wrong on purpose: he only stands in the seat.
            yield return Wait(.5f,hero);body.Receive(new DamagePacket{source=hero.Health,amount=body.maximum,type=Neutral(body)});yield return Frames(2);
            var clamp=hall.Item1[3];Put(hero,clamp.transform.position+Vector3.up*.05f);yield return null;
            Assert.IsTrue(clamp.Available,"fixture: at its line the clamp is offered");if(!Broken)clamp.Interact();
            var seat=hero.transform.position;
            Assert.IsTrue(boss.Perform("Draw"),"fixture: its draw at a held clamp");yield return null;
            band=Marks(boss).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Line);Assert.Less(Vector3.Angle(band.Forward,hall.Item2[4].Direction),2,"its draw doesn't pull the held clamp's fibre");
            yield return Until(()=>Said("WARDEN_FOULED")||!boss.Busy,5,hero);
            Assert.Less(Flat(hero.transform.position,seat),.5f,"its pull moved a hero with a clamp's weight in his hands");
            Assert.IsTrue(Said("WARDEN_FOULED"),"its snap closed on nobody and its staff isn't fouled");Assert.IsTrue(body.Broken,"fouled, its staff isn't down");
            Assert.That(body.BrokenUntil-Time.time,Is.InRange(2.2f,2.85f),"fouled for 2.8 s");StringAssert.Contains("STAFF IS FOULED",boss.StatusText);
            yield return Until(()=>clamp.State!=BridleClamp.Hold.Shut,3,hero);
            Assert.AreEqual(BridleClamp.Hold.Open,clamp.State,"held through its fouled staff, the clamp didn't open");
        }
        [UnityTest] public IEnumerator AtTheFourthClampTheBridleLetsGoTogetherAndItStandsDownWhereItStandsWithNoWord()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            var hall=Hall(At(0,0,0));yield return Frames(2);
            var warden=Enemy("LastWarden",At(0,.1f,0));yield return Frames(3);var boss=warden.GetComponent<BossController>();var body=warden.Health;
            string track=Def("LastWarden").bossMusicId;Assert.AreEqual("Boss of the Ages",track);Assert.AreEqual(track,MusicDirector.Current.Track,"fixture: its fight plays its own track");
            int xp=State.party[0].xp+State.party[0].level*100000;
            for(int i=0;i<3;i++)
            {
                body.Receive(new DamagePacket{source=hero.Health,amount=body.maximum,type=Neutral(body)});yield return Frames(2);
                yield return Open(hall.Item1[i],hero,body);yield return Until(()=>!body.Broken&&!boss.Shifting,6,hero);
            }
            body.Receive(new DamagePacket{source=hero.Health,amount=body.maximum,type=Neutral(body)});yield return Frames(2);
            Assert.AreEqual(1,body.integrity,1,"its hold isn't held at nought until the last clamp");Assert.IsTrue(body.Alive);
            // His hand on the last one (his own "Last one" is the drill, said as he takes it); from then on nobody speaks.
            var last=hall.Item1[3];Put(hero,last.transform.position+Vector3.up*.05f,false);yield return null;last.Interact();Assert.AreEqual(hero,last.Holder,"fixture: his hands are on the last clamp");
            var spoken=new List<string>();System.Action<string,string> heard=(who,line)=>spoken.Add(who+": "+line);BarkService.Spoken+=heard;
            try
            {
                // Built wrong on purpose: the clamps let go with nobody telling the Warden, and it must not be taken for standing down.
                if(Broken)BridleClamp.LetGoTogether();else body.Expose(3);
                yield return Until(()=>!body.Alive||Broken&&Time.time>0,4,hero);yield return Frames(3);
            }
            finally{BarkService.Spoken-=heard;}
            Assert.IsFalse(body.Alive,"the fourth clamp opened and it still holds the road");Assert.IsTrue(Said("WARDEN_RELEASED"));Assert.IsTrue(Said("WARDEN_STOOD_DOWN"),"it fell and didn't stand down");
            Assert.IsTrue(Set("bossdown.LastWarden"),"stood down, it isn't counted as beaten");Assert.IsTrue(Said("BOSS_DOWN LastWarden"));
            Assert.IsTrue(hall.Item1.All(c=>c.State==BridleClamp.Hold.Released),"the four didn't let go together");Assert.IsTrue(Said("CLAMPS_LET_GO"));
            Assert.IsTrue(hall.Item2.All(f=>f.Slack),"a fibre still carries force with the Bridle open");
            Assert.AreEqual(BossController.Ending.ShutDown,BossController.EndingOf("LastWarden"),"it isn't stood down");Assert.IsTrue(Def("LastWarden").yieldsOnDefeat,"it falls, where it should lower its staff and yield");
            Assert.IsEmpty(spoken,"a bark talks over the Bridle letting go: "+string.Join(", ",spoken));
            Assert.AreNotEqual(track,MusicDirector.Current.Track,"it has stood down and its track plays on: the zone's own music should come back");
            Assert.Greater(State.party[0].xp+State.party[0].level*100000,xp,"standing down paid the party nothing");
            yield return Wait(4.5f,hero);
            Assert.IsTrue(warden!=null,"stood down, its body was cleared away like a beaten one's");
            Assert.IsTrue(warden.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"stood down, it isn't drawn where it stood");
            // A hall made after it has stood down loads let go, quietly.
            var later=Clamp(5,At(0,.05f,40),Vector3.back);yield return Frames(2);
            Assert.AreEqual(BridleClamp.Hold.Released,later.State,"after the Warden stands down a clamp loads shut");Assert.IsFalse(Set(later.openedFlag),"a clamp settled by its standing down is counted as opened");
        }

        // ------------------------------------------------------------------ standing down, seen
        /// <summary>Where the Warden's staff's point is, read off the mesh as it is drawn: the vertices furthest out along the staff arm in the rig's bind pose (arms out, the staff running
        /// on from the right hand), found once on a fresh copy and then read on any body made from the same prefab.</summary>
        sealed class StaffProbe
        {
            readonly List<(int part,int index)> tip=new();readonly bool scaled;
            static Transform Bone(Animator animator,HumanBodyBones bone,string name){var t=animator.GetBoneTransform(bone);return t!=null?t:animator.GetComponentsInChildren<Transform>().FirstOrDefault(c=>c.name==name);}
            static Vector3 World(SkinnedMeshRenderer skin,Vector3 v,bool withScale)=>withScale?skin.transform.TransformPoint(v):Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one).MultiplyPoint3x4(v);
            static Vector3[] Baked(SkinnedMeshRenderer skin){var mesh=new Mesh();skin.BakeMesh(mesh);var vertices=mesh.vertices;Object.Destroy(mesh);return vertices;}
            public StaffProbe(GameObject prefab)
            {
                // Read the frame it is made, before its animator has posed it: the bind pose.
                var copy=Object.Instantiate(prefab,new Vector3(-400,0,-400),Quaternion.identity);
                try
                {
                    var animator=copy.GetComponentInChildren<Animator>();Assert.IsNotNull(animator,"fixture: the Warden is rigged");
                    var hand=Bone(animator,HumanBodyBones.RightHand,"RightHand");var fore=Bone(animator,HumanBodyBones.RightLowerArm,"RightForeArm");Assert.IsTrue(hand!=null&&fore!=null,"fixture: the Warden has a right arm");
                    var along=(hand.position-fore.position).normalized;var skins=copy.GetComponentsInChildren<SkinnedMeshRenderer>();Assert.IsNotEmpty(skins,"fixture: the Warden is a skinned body");
                    // Whichever way a baked mesh maps to the world (with its renderer's scale or without it), the right one puts a vertex at the hand.
                    float Nearest(bool withScale){float best=float.MaxValue;foreach(var skin in skins)foreach(var v in Baked(skin))best=Mathf.Min(best,(World(skin,v,withScale)-hand.position).sqrMagnitude);return best;}
                    scaled=Nearest(true)<Nearest(false);
                    var all=new List<(int part,int index,float out_)>();
                    for(int p=0;p<skins.Length;p++){var vertices=Baked(skins[p]);for(int i=0;i<vertices.Length;i++)all.Add((p,i,Vector3.Dot(World(skins[p],vertices[i],scaled)-hand.position,along)));}
                    foreach(var (p,i,_) in all.OrderByDescending(a=>a.out_).Take(8))tip.Add((p,i));
                    Assert.Greater(all.Max(a=>a.out_),.2f,"fixture: the staff runs on past the Warden's right hand");
                }
                finally{Object.DestroyImmediate(copy);}
            }
            public Vector3 Tip(GameObject body)
            {
                var skins=body.GetComponentsInChildren<SkinnedMeshRenderer>(true);var sum=Vector3.zero;var baked=new Dictionary<int,Vector3[]>();
                foreach(var (p,i) in tip){if(!baked.TryGetValue(p,out var vertices))baked[p]=vertices=Baked(skins[p]);sum+=World(skins[p],vertices[i],scaled);}
                return sum/tip.Count;
            }
        }
        /// <summary>The staff's point, the shoulder and the hips as drawn now, how far before the body the point is, and how far down its head looks (the rig's "headfront" off its head).</summary>
        static (Vector3 tip,float shoulder,float hips,float ahead,float bow) Pose(GameObject body,StaffProbe probe)
        {
            var animator=body.GetComponentInChildren<Animator>();var head=animator.GetBoneTransform(HumanBodyBones.Head);
            var front=head!=null?head.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="headfront"):null;Assert.IsNotNull(front,"fixture: the Warden's rig marks the front of its head");
            var tip=probe.Tip(body);var hips=animator.GetBoneTransform(HumanBodyBones.Hips).position;var facing=body.transform.forward;facing.y=0;facing.Normalize();
            var look=(front.position-head.position).normalized;
            return (tip,animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position.y,hips.y,Vector3.Dot(tip-hips,facing),Mathf.Asin(Mathf.Clamp(-look.y,-1,1))*Mathf.Rad2Deg);
        }
        static void Lowered(string who,(Vector3 tip,float shoulder,float hips,float ahead,float bow) pose)
        {
            TestContext.WriteLine($"WARDEN_LOWERED {who} tip={pose.tip.y:0.00} shoulder={pose.shoulder:0.00} hips={pose.hips:0.00} ahead={pose.ahead:0.00} bow={pose.bow:0}");
            Assert.Less(pose.tip.y,pose.shoulder,who+": stood down, its staff's point is above its shoulder");
            Assert.Less(pose.tip.y,pose.hips,who+": stood down, its staff's point isn't lowered toward the floor");
            Assert.Greater(pose.ahead,.35f,who+": stood down, the staff isn't held lowered before it: the arm only hangs");
            Assert.Greater(pose.bow,20,who+": stood down, its head isn't bowed");
        }
        /// <summary>A still for the eye: the game's camera, three-quarters before the body on its staff side, saved under Builds/quality/focused/oriel-warden.</summary>
        static void Still(string name,Transform subject)
        {
            const int W=1280,H=720;var camera=Camera.main;bool own=camera==null;if(own)camera=new GameObject("Still camera").AddComponent<Camera>();
            string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/focused/oriel-warden"));System.IO.Directory.CreateDirectory(folder);
            var facing=subject.forward;facing.y=0;facing.Normalize();var right=Vector3.Cross(Vector3.up,facing);
            var place=camera.transform.position;var turn=camera.transform.rotation;var target=camera.targetTexture;float fov=camera.fieldOfView;var active=RenderTexture.active;
            var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var image=new Texture2D(W,H,TextureFormat.RGB24,false);
            try
            {
                camera.transform.position=subject.position+facing*6.5f+right*2.6f+Vector3.up*3.2f;camera.transform.LookAt(subject.position+Vector3.up*1.9f);camera.fieldOfView=40;
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();
                string file=System.IO.Path.Combine(folder,name+".png");System.IO.File.WriteAllBytes(file,image.EncodeToPNG());TestContext.WriteLine("WARDEN_STILL "+file);
            }
            finally
            {
                camera.targetTexture=target;RenderTexture.active=active;camera.fieldOfView=fov;camera.transform.SetPositionAndRotation(place,turn);
                Object.Destroy(rt);Object.Destroy(image);if(own)Object.Destroy(camera.gameObject);
            }
        }
        /// <summary>The fight's whole meaning is that it lowers its staff. At the fourth clamp (in a yard with no clamps, when its hold is gone) the Warden turns back to the way it was set
        /// facing, brings its staff's point down to the floor before it and bows its head, over 1.2 s, and holds it. The hall shows the same, lowered, on every later visit (OrielBuilder.
        /// GlassChoir's "The Warden, stood down": the prefab under a holder with a solid box, its behaviours off and a StandsDown on it, shown by the boss's down flag), and keeps it hidden
        /// while the fought Warden still stands, so one Warden stands. Neither is a clip: StandsDown makes the pose on the rig. A still of each is saved.</summary>
        [UnityTest] public IEnumerator StoodDownItsStaffsPointIsLoweredBeforeItItsHeadBowedAndTheHallShowsOneWardenSoAfter()
        {
            string builder=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"_Project/Scripts/Editor/World/OrielBuilder.GlassChoir.cs"));
            StringAssert.Contains("var lowered=body.AddComponent<StandsDown>();lowered.id=\"LastWarden\";",builder,"the hall's stood-down Warden isn't given its lowered staff");
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(-20,.05f,-20));
            var prefab=Def("LastWarden").prefab;var probe=new StaffProbe(prefab);
            // The hall's, as its builder stands it, shown once the boss is down. Built wrong on purpose: the hall's Warden as it was, the prefab at rest with nothing to lower its staff;
            // and the fought one's StandsDown taken away as it is put on.
            var holder=new GameObject("The Warden, stood down (fixture)");holder.transform.SetPositionAndRotation(At(9,0,0),Quaternion.Euler(0,180,0));holder.SetActive(false);
            var room=Object.Instantiate(prefab,holder.transform,false);foreach(var b in room.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
            if(!Broken){var lowered=room.AddComponent<StandsDown>();lowered.id="LastWarden";lowered.seconds=0;}
            var box=holder.AddComponent<BoxCollider>();box.center=Vector3.up*1.6f;box.size=new Vector3(1.4f,3.2f,1.4f);
            // The fight's: set facing south, as the hall sets it, and stood down.
            var warden=Enemy("LastWarden",At(0,.1f,0),yaw:180);yield return Frames(3);var body=warden.Health;body.InvulnerableUntil=0;body.ShieldUntil=0;
            body.Receive(new DamagePacket{source=hero.Health,amount=body.maximum*10,type=Neutral(body)});yield return null;
            Assert.IsFalse(body.Alive,"fixture: its hold gone, it stands down");Assert.IsTrue(Said("WARDEN_STOOD_DOWN"),"fixture: it stood down");
            holder.SetActive(true);
            var stands=warden.GetComponent<StandsDown>();Assert.IsNotNull(stands,"stood down, nothing lowers its staff");
            if(Broken)Object.Destroy(stands);
            yield return Wait(BossController.WardenTune.StandDown+.6f,hero);
            Assert.IsTrue(stands!=null&&stands.Lowered,"it hasn't finished lowering its staff");
            var facing=warden.transform.forward;facing.y=0;Assert.Less(Vector3.Angle(facing,Vector3.back),3,"stood down, it doesn't face the way it was set");
            Lowered("fought",Pose(warden.gameObject,probe));
            Assert.IsTrue(warden.GetComponent<CharacterController>().enabled,"stood down, it can be walked through");
            // One Warden: the hall's stays hidden, box and all, while this one stands.
            var roomDown=room.GetComponent<StandsDown>();
            Assert.IsTrue(roomDown!=null&&roomDown.Hidden,"the hall's stood-down Warden shows beside the one that stood down: two Wardens");
            Assert.IsFalse(room.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"the hall's hidden Warden is still drawn");Assert.IsFalse(box.enabled,"the hall's hidden Warden's box still stands");
            Still("warden-stood-down-fought",warden.transform);
            // A later visit: no fought Warden, and the hall's stands, lowered, where the contract puts it.
            Object.Destroy(warden.gameObject);yield return Frames(3);
            Assert.IsFalse(roomDown.Hidden,"with no fought Warden the hall's stays hidden");Assert.IsTrue(box.enabled,"the hall's Warden isn't solid");
            Assert.IsTrue(room.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"the hall's Warden isn't drawn");
            Lowered("hall",Pose(room,probe));
            Still("warden-stood-down-yard",room.transform);
        }
        /// <summary>The engine hall as built, visited after the Warden has stood down (and after it has asked its question): the Warden the hall shows stands at the engine facing the
        /// stair, staff lowered and head bowed, alone. A still of it in the hall's own light.</summary>
        [UnityTest] public IEnumerator TheBuiltHallShowsItsWardenStoodDownStaffLoweredOnALaterVisit()
        {
            GameServices.Current.Flags.SetBool("bossdown.LastWarden",true);GameServices.Current.Flags.SetBool("oriel.wardenStoodDown",true);
            yield return Zone("GlassChoir");
            var probe=new StaffProbe(Def("LastWarden").prefab);
            var shown=Object.FindObjectsByType<StandsDown>(FindObjectsSortMode.None).Where(s=>s.id=="LastWarden"&&!s.fought).ToArray();
            Assert.AreEqual(1,shown.Length,"the hall doesn't show one stood-down Warden");var warden=shown[0];var body=warden.gameObject;
            Assert.IsTrue(warden.isActiveAndEnabled&&!warden.Hidden,"the hall's stood-down Warden isn't shown");
            // Built wrong on purpose: the hall's Warden with nothing to lower its staff, as it was built before.
            if(Broken)Object.Destroy(warden);
            yield return new WaitForSecondsRealtime(.6f);
            var facing=body.transform.forward;facing.y=0;Assert.Less(Vector3.Angle(facing,Vector3.back),3,"the hall's Warden doesn't face the stair");
            Lowered("built hall",Pose(body,probe));
            Still("warden-stood-down-hall",body.transform);
        }

        // ------------------------------------------------------------------ what the fight is sized on
        const float DriverShare=.3f,FlatOutSeconds=15;
        /// <summary>The four clamps: getting to one across the hall once the road is held, taking its weight (1.2 s) and waiting for the slack (a Draw it can be baited into every few seconds
        /// with the clamp held, or the partner's break), and its ease (.8 s): about ten seconds each.</summary>
        const float ClampSeconds=10;
        /// <summary>A pair flat out on a body that never moves, as QaPlanCombatTests measures it: both heroes attacking, every skill as its cooldown and Charge allow; one who fights with an
        /// Edge stands at its reach, one with an Emitter a few steps off.</summary>
        IEnumerator Rate(string[] pair,System.Action<float> measured)
        {
            yield return Deploy(pair);
            var at=At(0,.1f,0);var heroes=PartyController.Current.members.ToArray();
            var enemy=Enemy("LastWarden",at);yield return Frames(3);var body=enemy.Health;
            enemy.GetComponent<BossController>().enabled=false;body.maximum=body.integrity=10000000;
            float reach=enemy.GetComponent<CharacterController>().radius+.9f;var homes=new Vector3[heroes.Length];
            for(int i=0;i<heroes.Length;i++)homes[i]=DevLoadout.Weapon(heroes[i].character)==GearSlot.Edge?at+Quaternion.Euler(0,i==0?-20:20,0)*Vector3.back*reach:at+Vector3.back*(reach+5)+Vector3.right*(i==0?-2:2);
            for(int i=0;i<heroes.Length;i++){Put(heroes[i],homes[i]);heroes[i].target=body;heroes[i].charge=100;}
            yield return null;
            float landed=0;System.Action<Health,DamagePacket,float> count=(_,packet,amount)=>{if(heroes.Any(h=>packet.source==h.Health))landed+=amount;};body.Damaged+=count;
            for(float end=Time.unscaledTime+FlatOutSeconds;Time.unscaledTime<end;)
            {
                for(int i=0;i<heroes.Length;i++)
                {
                    var hero=heroes[i];hero.motor.Move(Vector2.zero,false,false);
                    if(hero.CanAct&&Flat(hero.transform.position,homes[i])>.6f){var keep=hero.Health.integrity;Put(hero,homes[i]);hero.Health.integrity=keep;}
                    hero.target=body;if(!hero.CanAct)continue;
                    bool acted=false;
                    for(int slot=0;slot<SkillBook.Slots&&!acted;slot++)
                    {
                        var skill=SkillBook.Equipped(hero.character,slot);
                        if(skill==null||hero.cooldowns[slot]>0||hero.charge<SkillBook.Cost(hero.character,skill))continue;
                        acted=hero.Skill(slot);
                    }
                    if(!acted)hero.Attack();
                }
                yield return null;
            }
            body.Damaged-=count;Assert.Greater(landed,0,"fixture: "+string.Join(" and ",pair)+" are hitting it");measured(landed/FlatOutSeconds);
            enemy.Passive=true;Object.Destroy(enemy.gameObject);
            foreach(var left in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(left is GravityWell||left is PulseField||left is StarfallStrike||left is NetSeed)Object.Destroy(left.gameObject);
            yield return Frames(3);
        }
        /// <summary>The plan's length on today's build, with nothing assumed but the driver's share and the time the four clamps take: two to five minutes, for every pair a player can have out
        /// by then, at Sync 20 in the chapter's arrival parts (Tempered). Its whole bar is fought down (each quarter held at its line until a clamp takes it over), and each clamp is about
        /// ten seconds of work. Each pair is held to the shared test's own floor and ceiling too. And the integrity is pinned to the builder: a rebuild writes whatever its source says.</summary>
        [UnityTest] public IEnumerator TheLastWardenLastsWhatThePlanAsksForEveryPairAPlayerCanPutOut()
        {
            string source=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"_Project/Scripts/Editor/World/OrielBuilder.Enemies.cs"));
            float integrity=Def("LastWarden").integrity;
            StringAssert.Contains("e.id=\"LastWarden\";",source);StringAssert.Contains($"e.integrity={integrity:0};e.breakThreshold=800;",source,"a rebuild would put the Last Warden's old integrity back");
            // Built wrong on purpose: three times the integrity, which no pair gets through in five minutes.
            if(Broken)integrity*=3;
            yield return Zone("Arena_Ground");Floor();yield return Level(20);
            var failures=new List<string>();
            foreach(var pair in new[]{new[]{"Taren","Idra"},new[]{"Taren","Sela"},new[]{"Sela","Idra"}})
            {
                float rate=0;yield return Rate(pair,r=>rate=r);
                float bar=integrity/(rate*DriverShare),clamps=4*ClampSeconds,seconds=bar+clamps;
                string line=$"ORIEL_RATE {pair[0]}+{pair[1]} sync=20 tempered rate={rate:0}/s bar={bar:0}s clamps={clamps:0}s total={seconds:0}s shared={integrity/(rate*DriverShare):0}s quarter={integrity/(rate*.25f):0}s";
                TestContext.WriteLine(line);Debug.Log(line);
                if(seconds<120||seconds>300)failures.Add($"{line}: the plan asks 120 to 300");
                if(integrity<rate*DriverShare*120)failures.Add($"{line}: the shared floor, 120 s at the driver's share");
                if(integrity/(rate*.25f)>300)failures.Add($"{line}: the shared ceiling, 300 s at a quarter of flat out");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
