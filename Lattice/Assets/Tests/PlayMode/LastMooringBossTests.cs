using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>
    /// Chapter 10's first boss, Mother of Doors, in the flight yard with her room stood the way docs/maps/SorrelSecondSky.md ("Boss arena contract") asks for it (nothing waits on the
    /// zone): her middle C, her coil round it, four shelves with their works, seats and holds (chapter 7's <see cref="BridleClamp"/>, hers) and a real berth each (stream A's
    /// <see cref="Berth"/>), the fourth on her strain. The party is the chapter's arrival (DevLoadout chapter10: Taren and Idra out at Sync 27, Choirwrought). Her sheet for the shared boss
    /// tests; what every move costs a hero; her sweep's and wave's lanes and how they narrow; cover against her wave; her markings on a shelf; the hold, eased only in her slack and never
    /// with a Clamp Tender at it; a clamp opened kept through a Continue (D285); a conversation holding her and the fourth shelf (D275); her release; her length in four real docks; her body.
    /// Each test builds its fixture wrong first under <c>MOTHER_BROKEN=1</c> (this class's own runs only), so the run shows every check failing the way it should.
    /// </summary>
    public sealed class LastMooringBossTests
    {
        readonly List<string> log=new();
        static readonly string[] Heeded={"BOSS_","MOTHER_","CLAMP_","CLAMPS_","BERTH_","STRAIN_","CHECKPOINT_KEPT","TENDED"};
        void Heard(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        static bool Broken=>System.Environment.GetEnvironmentVariable("MOTHER_BROKEN")=="1";

        // ------------------------------------------------------------------ her room in the flight yard, stood on every load of it a test asks for (a scene's own objects, as a builder's)
        static readonly Vector3 C=new(300,1,300);
        /// <summary>The Clamp Tender she steps out at a seat: chapter 10's own weight of chapter 7's (LastMooringBuilder.Enemies, D296).</summary>
        const string Tender="StillwaterTender";
        const float Floor=-.8f;
        static readonly float[] Bearings={240,205,155,120};
        /// <summary>How many of her clamps the next load of the flight yard stands (0: a bare yard), and whether their holds keep an opened clamp open (the broken state: they don't).</summary>
        static int roomClamps;static bool roomKeeps=true;static bool roomExit,roomExitSouth;
        static Vector3 Heading(float bearing)=>Quaternion.Euler(0,bearing,0)*Vector3.forward;
        static Vector3 On(float bearing,float r,float y){var at=C+Heading(bearing)*r;at.y=y;return at;}
        static void Built(Scene scene,LoadSceneMode mode)
        {
            if(scene.name!="Arena_Flight"||roomClamps==0)return;
            var yard=new GameObject("Mother's room");SceneManager.MoveGameObjectToScene(yard,scene);
            for(int i=1;i<=roomClamps;i++)
            {
                float b=Bearings[i-1];var middle=On(b,27,Floor);var inward=Quaternion.LookRotation(-Heading(b));
                var berth=Berth.Declare("Clamp"+i,"The clamp's works",ZoneKind.GroundCombat,new Bounds(middle+Vector3.up*.6f,new Vector3(14,1.2f,10)),On(b,27,1),On(b,28.5f,Floor),On(b,30,Floor),On(b,31,1),
                    "Shelf"+i,On(b,35,1),true,true,yard.transform);
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Shelf "+i;floor.transform.SetParent(berth.transform,false);floor.transform.position=middle+Vector3.down*.5f;floor.transform.localScale=new Vector3(14,1,10);
                var works=GameObject.CreatePrimitive(PrimitiveType.Cube);works.name="The works "+i;works.transform.SetParent(berth.transform,false);
                works.transform.SetPositionAndRotation(On(b,21.6f,Floor+.625f),inward);works.transform.localScale=new Vector3(3.2f,1.25f,.9f);
                var seat=new GameObject("Clamp seat "+i);seat.transform.SetParent(berth.transform,false);seat.transform.SetPositionAndRotation(On(b,23,Floor),inward);
                var clamp=seat.AddComponent<BridleClamp>();clamp.index=i;clamp.settledBy="MotherOfDoors";clamp.openedFlag="end.clamp"+i;clamp.keepsOpened=roomKeeps;
                if(i==4){var strain=berth.gameObject.AddComponent<MotherStrain>();strain.pivot=C;strain.fromBearing=120;strain.toBearing=92;strain.radius=27;}
            }
            // The fight's door spawn, in flight on the approach, as the zone has it.
            var door=new GameObject("Clamps",typeof(SpawnPoint));door.transform.SetParent(yard.transform,false);door.transform.position=C+Vector3.back*60;door.GetComponent<SpawnPoint>().id="Clamps";
            if(roomExit){var exit=new GameObject("Her way out",typeof(BossExit));exit.transform.SetParent(yard.transform,false);exit.transform.position=C+Vector3.forward*70*(roomExitSouth?-1:1);exit.GetComponent<BossExit>().boss="MotherOfDoors";}
            Physics.SyncTransforms();
        }

        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();roomClamps=0;roomKeeps=true;roomExit=roomExitSouth=false;SceneManager.sceneLoaded+=Built;Application.logMessageReceived+=Heard;log.Clear();
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("chapter10");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded-=Built;Application.logMessageReceived-=Heard;roomClamps=0;
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;BridleClamp.Slack=false;MotherStrain.Straining=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        /// <summary>Her own bounds: four cycles of fly, dock, work and launch, measured by <see cref="HerFourCyclesOfFlyDockWorkAndLaunchLastAsHerSheetSays"/> with the clamps' guns
        /// allowed for (the first combat engineer's encounters, not hers), and the yard's integrity sized to the same (QaPlanCombatTests' length test).</summary>
        const float MinSeconds=120,MaxSeconds=300;
        [ContentRegistry.Expectations] static void MotherOfDoors(ContentRegistry.Book book)
        {
            // Met at Sync 27 in Sorrel's second sky (docs/maps/SorrelSecondSky.md, "Boss arena contract") once Dace's crossing has been flown (end.sorrelSeen). In her room nobody strikes her and
            // her bar is her clamps; in the yard, where these are measured, she fights on integrity as any boss. Her share is what the replay driver's pad got from her door in the zone (MooringBossReplayTests, November 15: .26 to .29 of the pair's sixty-second flat out at mastery 5, 1,891 to 1,993 a second, over fights of 144 to 152 s), the lower. The stands are from
            // her middle with her facing north (the shared tests stand her so): every move is aimed at the lead.
            // .53 since D362 (November 22): today's scout of the 12:35 build was through her room in 103.8 s (nov22-recon-g s63) against 1,463 and 1,412 a second on her yard integrity
            // (d355-dome3, d355-lengths), which is .536. Her room's length is her four clamps and their guns, not her integrity, so her integrity is left: what would lengthen her is D362's.
            book.Boss(new ContentRegistry.BossSheet{id="MotherOfDoors",zone="SorrelSecondSky",door="MotherOfDoors",doorFlags=new[]{"end.sorrelSeen"},flight=true,sync=27,driverShare=.53f,
                minSeconds=MinSeconds,maxSeconds=MaxSeconds,
                specials=new[]{("Sweep",new Vector3(0,0,22)),("Wave",new Vector3(0,0,27)),("Lash",new Vector3(0,0,22)),("Heave",new Vector3(0,0,16))},
                // Every move ends in her slack (she draws back), in every phase.
                windows=new[]{("Sweep",new Vector3(0,0,22)),("Wave",new Vector3(0,0,27)),("Lash",new Vector3(0,0,22)),("Heave",new Vector3(0,0,16))}});
        }

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static bool OnDisk(string key)=>new FlagService(GameServices.Current.Saves.Load("autosave").flags).GetBool(key);
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=PartyController.Current.Active);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Load(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+20;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            Hold();yield return new WaitForSecondsRealtime(.5f);
        }
        static void Hold(){foreach(var hero in PartyController.Current.members){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;}}
        static void Fresh(CombatActor hero,float bar=9000){hero.Health.maximum=hero.Health.integrity=bar;hero.Health.InvulnerableUntil=0;}
        static void Put(CombatActor hero,Vector3 at)
        {
            var body=hero.GetComponent<CharacterController>();bool on=body.enabled;body.enabled=false;
            if(hero.motor is GroundMotor ground)ground.Halt();else if(hero.motor is FlightMotor ship)ship.Halt();
            hero.transform.position=at;body.enabled=on;Physics.SyncTransforms();
        }
        static EnemyDef Def(string id){var definition=GameCatalog.Find<EnemyDef>(id);Assert.IsNotNull(definition,"fixture: "+id+" is defined (LastMooringBuilder.Definitions)");return definition;}
        static BossController Her(bool passive=true,float yaw=180,Vector3? at=null)
        {
            var enemy=ActorFactory.Enemy(Def("MotherOfDoors"),at??C);enemy.transform.rotation=Quaternion.Euler(0,yaw,0);enemy.Passive=passive;return enemy.GetComponent<BossController>();
        }
        static Telegraph[] Marks(Object owner)=>Telegraph.Live.Where(t=>t!=null&&t.owner==owner&&!t.Released).ToArray();
        /// <summary>Drives the heroes' motors as their brains would while the fight's clock runs (a roll only happens on a driven motor).</summary>
        static IEnumerator Until(System.Func<bool> done,float seconds,params CombatActor[] held)
        {
            for(float end=Time.unscaledTime+seconds*1.5f+1,until=Time.time+seconds;!done()&&Time.time<until&&Time.unscaledTime<end;)
            {foreach(var actor in held)if(actor!=null&&actor.Health.Alive)actor.motor.Move(Vector2.zero,false,false);yield return null;}
        }
        bool Said(string start)=>log.Any(l=>l.StartsWith(start));
        static IEnumerator Level(int sync){foreach(var member in State.party)member.level=sync;PartyController.Current.RefreshStats();yield return null;}
        static IEnumerator Deploy(params string[] pair){Assert.IsTrue(Deployment.Set(State,pair),"fixture: the pair can be put out");PartyField.Reform(PartyController.Current);yield return Frames(3);PartyController.Current.RefreshStats();yield return null;Hold();}
        static IEnumerator Wear(string frame)
        {
            var part=GameCatalog.Find<TechPartDef>(frame);Assert.IsNotNull(part,"fixture: "+frame+" is defined");
            var inv=new Inventory(State);foreach(var member in State.party)inv.Equip(member,inv.GivePart(part),part);PartyController.Current.RefreshStats();yield return null;
        }
        /// <summary>A pad pressed as the player presses it, and the change waited out.</summary>
        static IEnumerator Press(BerthPrompt prompt)
        {
            yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.1f);
            // On a fight's shelf the launch waits until the lead's hands have been off the fight a moment (Berth.FightLaunchCalm): a surge or a roll just before the press hides it for
            // that long, and a player waits for it to come back.
            for(float wait=Time.unscaledTime+Berth.FightLaunchCalm+.5f;!prompt.Available&&Time.unscaledTime<wait;)yield return null;
            Assert.IsTrue(prompt.Available,$"fixture: \"{prompt.prompt}\" isn't offered");prompt.Interact();
            float end=Time.unscaledTime+3;while(Berth.Changing&&Time.unscaledTime<end)yield return null;
            Assert.IsFalse(Berth.Changing,"fixture: the change never finished");yield return null;Hold();
        }

        // ------------------------------------------------------------------ her definition, her words
        /// <summary>Her EnemyDef and her Kit came in one change, and every word of hers on screen is the manifest's (section 5 and section 6): her name, her epithet, her state line, the
        /// panel's three lines. Nothing anywhere calls her defeated: she goes. Her music is Boss of the Ages; her capsule stands on its floor; her brain has no blow of its own; her
        /// integrity is the builder's literal.</summary>
        [UnityTest] public IEnumerator HerDefinitionAndKitCameTogetherAndEveryWordOfHersIsTheManifests()
        {
            yield return Load("Arena_Flight");
            var def=Def("MotherOfDoors");
            Assert.IsTrue(def.boss,"she isn't a boss");Assert.IsTrue(BossController.Registered("MotherOfDoors"),"her definition exists without her Kit");
            Assert.AreEqual("MOTHER OF DOORS",def.displayName);Assert.AreEqual("Boss of the Ages",def.bossMusicId,"her fight doesn't name its track");
            Assert.GreaterOrEqual(def.capsuleHeight,BossController.CapsuleRadius("MotherOfDoors")*2,"her capsule is shorter than twice its radius: Unity stretches it and she floats");
            Assert.IsFalse(def.attacks.Any(a=>a!=null&&a.range>0),"her brain has a blow that reaches someone: every blow she lands is a marked special");
            Assert.AreEqual(BossController.Ending.Leaves,BossController.EndingOf("MotherOfDoors"),"she is to go, not to be destroyed");
            string source=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"_Project/Scripts/Editor/World/LastMooringBuilder.Mother.cs"));
            StringAssert.Contains($"e.integrity={def.integrity:0};",source,"a rebuild would put her old integrity back");
            var boss=Her(false);yield return Frames(3);
            // Built wrong on purpose: the epithet as the brief first had it.
            string epithet=Broken?"An adult Choir, coiled round the nursery":"A grown road, come back for her young";
            Assert.AreEqual(epithet,boss.Epithet,"her epithet isn't the manifest's");Assert.AreEqual("MOTHER OF DOORS",boss.DisplayName);
            Assert.AreEqual("SHE IS FRIGHTENED  ·  OPEN A CLAMP",boss.StatusText,"her state line isn't the manifest's");
            var words=new List<string>{boss.DisplayName,boss.Epithet,boss.StatusText,boss.ShiftText,"SHE GOES SLACK  ·  EASE THE CLAMP","Fly the clamp's guns down, then dock",
                "Take its weight and wait for the slack (1/4)","Open the last clamp alongside her as she moves"};
            foreach(var line in words)foreach(string bad in new[]{"defeat","DEFEAT","kill","KILL","beaten","BEATEN"})StringAssert.DoesNotContain(bad,line,"a line of hers says "+bad);
            // In her room the panel's lines are the manifest's three, by where the party is.
            boss.GetComponent<EnemyBrain>().Passive=true;Object.Destroy(boss.gameObject);yield return Frames(2);
            roomClamps=4;yield return Load("Arena_Flight");boss=Her(false);yield return Frames(3);
            Put(Lead,On(155,30,1));yield return Frames(2);
            Assert.AreEqual("Fly the clamp's guns down, then dock",boss.FightGoal("zone"),"in flight with a clamp to open the panel doesn't send the party at its guns");
            Assert.AreEqual("SHE IS FRIGHTENED  ·  OPEN A CLAMP",boss.GuardedText,"in her room the bar doesn't say her state line");
            Assert.IsFalse(boss.Health.Targetable,"in her room she can be struck or locked onto: nobody shoots at her");
            boss.GetComponent<EnemyBrain>().Passive=true;
        }

        // ------------------------------------------------------------------ the numbers she is tuned by
        /// <summary>The standing rule (CHAPTER_GUIDE 0.1): a boss's blow costs a hero 12 to 16% of the bar and more than anything on the road to it, measured through Health.Receive on all
        /// three heroes at Sync 27 and 28 in the Choirwrought parts chapter 10 arrives in. Her phases add nothing (the hum narrows her, it never grows her blows).</summary>
        [UnityTest] public IEnumerator EveryBlowOfHersTakesABossesShareOfAnyHerosBarAndMoreThanTheRoadToIt()
        {
            yield return Load("Arena_Flight");
            var source=Her().Health;yield return Frames(2);
            var roadIds=new[]{"Interceptor","CompactWarden","ClampGun","FlakGate","ClampTender",Tender,"StillwaterWarden","StillwaterWeldArm","StillwaterRig","StillwaterInterceptor","StillwaterTowDrone","TowDrone","WeldArm","OverrideRig"};
            var road=roadIds.Select(id=>GameCatalog.Find<EnemyDef>(id)).Where(d=>d!=null&&!d.boss).SelectMany(d=>d.attacks.Where(a=>a!=null).Select(a=>(id:d.id,damage:a.damage))).ToList();
            var moves=new List<(string what,float damage)>{("Sweep",BossController.MotherTune.SweepDamage),("Wave",BossController.MotherTune.WaveDamage),("Lash",BossController.MotherTune.LashDamage),("Heave",BossController.MotherTune.HeaveDamage)};
            // Built wrong on purpose: a lash authored at 230 is over the band, and the check has to say so.
            if(Broken)moves.Add(("a lash authored at 230",230));
            var failures=new List<string>();
            foreach(var pair in new[]{new[]{"Taren","Idra"},new[]{"Sela","Idra"}})
            {
                yield return Deploy(pair);yield return Wear("FramesT5");
                foreach(int sync in new[]{27,28})
                {
                    yield return Level(sync);
                    foreach(var hero in PartyController.Current.members)
                    {
                        if(pair[0]=="Sela"&&hero.character=="Idra")continue;
                        float bar=hero.Health.maximum;
                        float Share(float damage){hero.Health.integrity=bar;hero.Health.InvulnerableUntil=0;return hero.Health.Receive(new DamagePacket{source=source,amount=damage,type=DamageType.Kinetic,weight=HitWeight.Heavy})/bar;}
                        float heaviest=0;string named="nothing";foreach(var (id,damage) in road){float s=Share(damage);if(s>heaviest){heaviest=s;named=id;}}
                        foreach(var (what,damage) in moves)
                        {
                            float share=Share(damage);string line=$"MOTHER_HIT {hero.character} sync={sync} plating={hero.Plating:0} {what} {damage:0} share={share:P1} road={named} {heaviest:P1}";
                            TestContext.WriteLine(line);
                            if(share<.12f||share>.16f)failures.Add(line+": a boss's blow is 12 to 16% of the bar");
                            if(share<=heaviest)failures.Add(line+": no more than the road to it");
                        }
                    }
                }
            }
            foreach(string zone in new[]{"SorrelSecondSky"}){var z=GameCatalog.Find<ZoneDef>(zone);if(z!=null&&!Mathf.Approximately(z.enemyThreat,1))failures.Add($"{zone} is at threat {z.enemyThreat}");}
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ her lanes
        /// <summary>Her sweep's wedges leave gaps wider than two hulls (Idra's, the widest: 8.7 m) everywhere a ship flies in her fight (from r 21 out), and her wave's bands run out from
        /// her coil's edge; as she eases (her stage, clamp by clamp: in the yard her phase) each wedge and each band is narrower and every tell shorter, while more bands come at once and
        /// the back-swing comes in: narrower, and quicker.</summary>
        [UnityTest] public IEnumerator HerSweepAndWaveLeaveLanesWiderThanTwoHullsAndNarrowAsSheEases()
        {
            yield return Load("Arena_Flight");
            var boss=Her(true,0);yield return Frames(3);var body=boss.Health;
            var lead=Lead;Fresh(lead);Fresh(Partner);Put(Partner,C+new Vector3(80,0,0));
            float hulls=2*2*HeroCollision.HullRadius("Idra");
            // Built wrong on purpose: measured at r 10, over her own coil, where no ship's lane is.
            float lanesFrom=Broken?10:21;
            var failures=new List<string>();float lastWedge=999,lastBand=999,lastTell=999;int lastCount=0;
            var thresholds=boss.Thresholds;
            for(int phase=1;phase<=4;phase++)
            {
                body.integrity=body.maximum*(phase==1?1:thresholds[phase-2]-.05f);yield return Frames(2);
                if(boss.MotherStage!=phase-1){failures.Add($"fixture: her stage is {boss.MotherStage} in phase {phase}");continue;}
                Put(lead,C+Vector3.forward*24);yield return Until(()=>!boss.Busy&&!body.Broken,9,lead);
                var before=new HashSet<Telegraph>(Telegraph.Live);
                Assert.IsTrue(boss.Perform("Sweep"),"fixture: her sweep");yield return null;
                var wedges=Telegraph.Live.Where(t=>t!=null&&t.owner==boss&&!before.Contains(t)&&t.Shape==Telegraph.Kind.Cone).OrderBy(t=>Mathf.Atan2(t.Forward.x,t.Forward.z)).ToArray();
                if(wedges.Length<3){failures.Add($"phase {phase}: her sweep laid {wedges.Length} wedges");continue;}
                float wedge=wedges[0].Angle,tell=wedges.Min(w=>w.Duration);
                for(int i=1;i<wedges.Length;i++)
                {
                    float between=Vector3.Angle(wedges[i-1].Forward,wedges[i].Forward)-wedge;var apex=wedges[i].Origin;
                    float at=lanesFrom-Flat(apex,C);float width=2*Mathf.Max(0,at)*Mathf.Sin(between*.5f*Mathf.Deg2Rad);
                    TestContext.WriteLine($"MOTHER_GAP phase={phase} wedge={wedge:0} gap={between:0}deg width={width:0.0}m at r{lanesFrom}");
                    if(width<hulls)failures.Add($"phase {phase}: the lane between two of her wedges is {width:0.0} m at r {lanesFrom}, under two hulls ({hulls:0.0})");
                }
                if(wedge>=lastWedge)failures.Add($"phase {phase}: her wedge is {wedge:0} degrees, no narrower than the {lastWedge:0} before");
                if(tell>=lastTell)failures.Add($"phase {phase}: her sweep's tell is {tell:0.00} s, no quicker than {lastTell:0.00}");
                lastWedge=wedge;lastTell=tell;
                if(phase==4&&!log.Any(l=>l.StartsWith("MOTHER_SWEEP")&&l.Contains("stage=3")))failures.Add("fixture: no sweep at stage 3 in the log");
                yield return Until(()=>!boss.Busy&&!body.Broken,14,lead);
                before=new HashSet<Telegraph>(Telegraph.Live);int bandsSeen=0;float depth=0;
                Assert.IsTrue(boss.Perform("Wave"),"fixture: her wave");
                for(float end=Time.time+12;boss.Busy&&!body.Broken&&Time.time<end;)
                {
                    foreach(var t in Telegraph.Live)if(t!=null&&t.owner==boss&&!before.Contains(t)&&t.Shape==Telegraph.Kind.Ring){before.Add(t);bandsSeen++;depth=t.Radius-t.Inner;
                        if(Mathf.Abs(t.Inner-BossController.MotherTune.WaveFrom)>.5f)failures.Add($"phase {phase}: a band starts at r {t.Inner:0.0}, not at her coil's edge");}
                    lead.motor.Move(Vector2.zero,false,false);yield return null;
                }
                if(bandsSeen<=lastCount&&phase>1&&phase!=3)failures.Add($"phase {phase}: {bandsSeen} bands, no more than the {lastCount} before");
                if(depth>=lastBand)failures.Add($"phase {phase}: a band {depth:0.0} m deep, no narrower than {lastBand:0.0}");
                lastBand=depth;lastCount=bandsSeen;
                yield return Until(()=>!boss.Busy,8,lead);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>Her pressure wave stops at cover: a ship behind a rock in the lanes isn't struck, one in the open lane at the same distance is (D198: what lands was marked; and here
        /// a band doesn't land where cover lies between her and the ship).</summary>
        [UnityTest] public IEnumerator ARockBetweenHerAndAShipStopsHerWaveAndTheOpenLaneDoesNot()
        {
            yield return Load("Arena_Flight");
            var boss=Her(true,0);yield return Frames(3);
            var behind=Lead;var open=Partner;Fresh(behind);Fresh(open);
            Put(behind,C+Vector3.forward*31);Put(open,C+Quaternion.Euler(0,40,0)*Vector3.forward*31);
            // Built wrong on purpose: no rock stands in the lane.
            if(!Broken){var rock=GameObject.CreatePrimitive(PrimitiveType.Sphere);rock.name="Lane rock";rock.transform.position=C+Vector3.forward*27;rock.transform.localScale=Vector3.one*5.6f;Physics.SyncTransforms();}
            float a=behind.Health.integrity,b=open.Health.integrity;
            Assert.IsTrue(boss.Perform("Wave"),"fixture: her wave");
            yield return Until(()=>!boss.Busy,12,behind,open);
            Assert.Less(open.Health.integrity,b,"her wave didn't strike a ship in the open lane at the same distance");
            Assert.AreEqual(a,behind.Health.integrity,.01f,"her wave struck a ship behind a rock: cover is to stop it");
            Assert.IsTrue(log.Any(l=>l.StartsWith("MOTHER_WAVE_COVERED")),"no MOTHER_WAVE_COVERED in the log");
        }

        // ------------------------------------------------------------------ on a shelf
        /// <summary>Docked at a shelf (a real berth), she is no target, her markings are laid on the shelf's floor, and every other sweep leaves the seat in a gap while the one between
        /// lays a wedge over it: a hand on the clamp may stay, or must let go, and the marking says which before it lands. The panel says to take the clamp's weight.</summary>
        [UnityTest] public IEnumerator OnAShelfHerMarkingsLieOnItsFloorAndEveryOtherSweepLeavesTheSeatInAGap()
        {
            roomClamps=4;yield return Load("Arena_Flight");
            var boss=Her(false);yield return Frames(3);boss.GetComponent<EnemyBrain>().Passive=true;
            // The first shelf: her clamps are taken in order, and a later shelf's pad takes nobody before the first is open (MooringBossReplayTests). Until November 15 this docked at
            // the second, out of turn.
            var berth=Berth.Find("Clamp1");Assert.IsNotNull(berth,"fixture: the first shelf's berth");
            Put(Lead,On(240,27,1));yield return Press(berth.pad);
            Assert.IsTrue(Berth.Current==berth&&!Lead.flight,"fixture: the party is on the shelf on foot");
            var seat=BridleClamp.All.First(c=>c.index==1);Put(Lead,seat.transform.position);yield return Frames(2);
            Assert.AreSame(seat,boss.DockedSeat(),"she doesn't know which shelf the party stands on");
            Assert.AreEqual("Take its weight and wait for the slack (1/4)",boss.FightGoal("zone"),"on the shelf the panel doesn't say to take the clamp's weight");
            var failures=new List<string>();var inGap=new List<bool>();
            for(int sweep=0;sweep<2;sweep++)
            {
                yield return Until(()=>!boss.Busy&&!boss.Health.Broken,12,Lead);
                var before=new HashSet<Telegraph>(Telegraph.Live);Assert.IsTrue(boss.Perform("Sweep"),"fixture: her sweep");yield return null;
                var wedges=Telegraph.Live.Where(t=>t!=null&&t.owner==boss&&!before.Contains(t)).ToArray();
                foreach(var w in wedges)if(Mathf.Abs(w.Origin.y-Floor)>.3f)failures.Add($"a wedge is laid at y {w.Origin.y:0.0}, not on the shelf's floor at {Floor}");
                inGap.Add(!wedges.Any(w=>w.Contains(seat.transform.position)));
                Put(Lead,seat.transform.position);Fresh(Lead);float hers=0;var her=boss.Health;
                System.Action<Health,DamagePacket,float> struck=(_,packet,amount)=>{if(packet.source==her)hers+=amount;};Lead.Health.Damaged+=struck;
                yield return Until(()=>!boss.Busy,10,Lead);Lead.Health.Damaged-=struck;
                // Her wedge is painted onto the shelf, so it reaches a hero there though she is a flier (D291's Health.AnyForm): with the seat under it, a hand on the clamp must let go.
                // Only her own blows count: the tender on the shelf strikes too.
                if(!inGap[sweep]&&hers<=0)failures.Add("her wedge over the seat landed and a hero at the seat took nothing of hers: what she paints on the shelf has to reach it");
            }
            // Built wrong on purpose: asking for the seat in a gap at both.
            bool[] wanted=Broken?new[]{true,true}:new[]{true,false};
            for(int i=0;i<2;i++)if(inGap[i]!=wanted[i])failures.Add($"sweep {i+1} on the shelf {(inGap[i]?"left the seat in a gap":"laid a wedge over the seat")}: every other sweep is to leave it in a gap, the one between to cover it");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>The hold (chapter 7's, hers): docked, a Clamp Tender steps out beside the seat; with it working the clamp, a hand on the clamp never eases it, even in her slack; with
        /// it stopped (the partner's job, real blows), the next slack eases it open. The clamp takes a quarter of her bar, its flag is kept on the disk the moment it opens without moving
        /// where a defeat returns (D285), and she eases (her shift).</summary>
        [UnityTest] public IEnumerator TheClampEasesOnlyInHerSlackAndNeverWithATenderAtItAndTakesAQuarterOfHerBar()
        {
            roomClamps=4;yield return Load("Arena_Flight");
            State.spawn="Clamps";GameServices.Current.Saves.Save("autosave",State);
            var boss=Her(false);yield return Frames(3);var body=boss.Health;
            var berth=Berth.Find("Clamp1");Put(Lead,On(240,27,1));yield return Press(berth.pad);
            var lead=Lead;var partner=Partner;Fresh(lead);Fresh(partner);var seat=BridleClamp.All.First(c=>c.index==1);
            yield return Until(()=>log.Any(l=>l.StartsWith("MOTHER_TENDERS")),3,lead,partner);
            Assert.IsTrue(Said("MOTHER_TENDERS"),"no Clamp Tender stepped out as the party docked");
            var tenders=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>Tended.IsTender(e.Health)).ToList();
            Assert.Greater(tenders.Count,0,"fixture: a tender on the shelf");
            bool tenderedSlack=false,openedEarly=false;float line=body.maximum*.75f;
            // The lead takes the weight and keeps it as a player does: out of a wedge over the seat as it lands, back in after.
            IEnumerator Work(float seconds,System.Func<bool> stop,bool partnerCovers)
            {
                for(float end=Time.time+seconds;Time.time<end&&!stop();)
                {
                    foreach(var hero in PartyController.Current.members)if(hero.Health.Alive)hero.motor.Move(Vector2.zero,false,false);
                    bool threat=boss.Telegraphing&&boss.TelegraphRemaining<.15f;
                    if(threat&&lead.State!=ActorState.Dodge){lead.Dodge(lead.transform.position-C);}
                    else if(lead.State!=ActorState.Dodge&&lead.CanAct&&seat.Holder==null&&seat.State==BridleClamp.Hold.Shut)
                    {if(Flat(lead.transform.position,seat.transform.position)>.5f)Put(lead,seat.transform.position);if(seat.Available)seat.Interact();}
                    if(partnerCovers)
                    {
                        var tender=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).FirstOrDefault(e=>Tended.IsTender(e.Health)&&e.Health.Alive);
                        if(tender!=null&&partner.CanAct){if(Flat(partner.transform.position,tender.transform.position)>1.8f)Put(partner,tender.transform.position+(partner.transform.position-tender.transform.position).normalized*1.4f);
                            partner.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(tender.transform.position-partner.transform.position,Vector3.up));partner.target=tender.Health;partner.Attack();}
                    }
                    if(body.Broken&&seat.Tended.Tending&&seat.Holder!=null)tenderedSlack=true;
                    yield return null;
                }
            }
            // Through her slack with the tender at work: no ease.
            yield return Work(30,()=>tenderedSlack&&!body.Broken,false);
            Assert.IsTrue(tenderedSlack,"fixture: her slack never came with the tender working the clamp and a hand on it");
            openedEarly=seat.State!=BridleClamp.Hold.Shut;
            Assert.IsFalse(openedEarly,"the clamp eased open with a Clamp Tender working it shut");
            Assert.IsFalse(OnDisk("end.clamp1"),"fixture: the clamp is on the disk before it opened");
            // The partner stops the tender; the next slack eases it. Built wrong on purpose: nobody covers, and it never opens.
            yield return Work(45,()=>seat.State!=BridleClamp.Hold.Shut,!Broken);
            Assert.AreNotEqual(BridleClamp.Hold.Shut,seat.State,"with the tender stopped and a hand on the clamp, her slack never eased it open");
            Assert.IsTrue(Has("end.clamp1"),"the clamp opened and end.clamp1 isn't set");
            Assert.IsTrue(OnDisk("end.clamp1"),"the clamp opened and the disk doesn't keep it: a defeat would shut it again (D285)");
            Assert.AreEqual("Clamps",GameServices.Current.Saves.Load("autosave").spawn,"keeping the clamp moved where a defeat returns");
            Assert.Less(body.integrity,line,"the first clamp didn't take her bar over its quarter's line");Assert.Greater(body.integrity,body.maximum*.5f,"the first clamp took more than its quarter");
            yield return Until(()=>log.Any(l=>l.StartsWith("BOSS_SHIFT MotherOfDoors")),12,lead,partner);
            Assert.IsTrue(Said("BOSS_SHIFT MotherOfDoors 2"),"she didn't ease (her shift) with the first clamp off");
        }

        /// <summary>The replay driver's clamp hands (chapter 7's, ContinuousReview.Oriel) know a tender by what it is, not by chapter 7's id: with hers (StillwaterTender, D296) up beside a seat
        /// they find it, to stop it before taking the clamp, as with chapter 7's. Known by the id, they took a clamp her tender kept shut and waited on it for good.</summary>
        [UnityTest] public IEnumerator TheDriversClampHandsKnowHerTenderAsAClampTender()
        {
            roomClamps=4;yield return Load("Arena_Flight");
            var seat=BridleClamp.All.First(c=>c.index==1);
            // Built wrong on purpose: her tender 40 m off, out of the hands' reach.
            var tender=ActorFactory.Enemy(Def(Tender),seat.transform.position+seat.transform.right*(Broken?40:2.5f));tender.Passive=true;yield return Frames(3);
            Assert.IsTrue(Tended.IsTender(tender.Health),"her tender isn't known as a tender");
            var warden=ActorFactory.Enemy(Def("StillwaterWarden"),seat.transform.position+seat.transform.right*-3);warden.Passive=true;yield return Frames(2);
            Assert.IsFalse(Tended.IsTender(warden.Health),"a Warden is taken for a tender");
            var near=typeof(ContinuousReview).GetMethod("TenderNear",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);Assert.IsNotNull(near,"fixture: the driver's TenderNear");
            var found=(Health)near.Invoke(null,new object[]{seat.transform.position});
            Assert.AreSame(tender.Health,found,"the replay driver's clamp hands don't see her tender beside the seat: they would take a clamp it keeps shut and wait on it for good");
        }

        // ------------------------------------------------------------------ what a defeat and a Continue keep (D285)
        /// <summary>Two clamps opened and kept on the disk; quit to the title and Continue: in the yard stood again they load open, the other two shut, and she wakes at the line the open
        /// two leave her, in her third phase, with no shift played over the load.</summary>
        [UnityTest] public IEnumerator ClampsOpenedStayOpenThroughAContinueAndSheWakesAtTheirLine()
        {
            // Built wrong on purpose: holds that don't keep what they opened, as chapter 7's don't.
            roomKeeps=!Broken;roomClamps=4;yield return Load("Arena_Flight");
            GameServices.Current.Flags.SetBool("end.clamp1",true);GameServices.Current.Flags.SetBool("end.clamp2",true);State.spawn="Arrival";SaveSystem.KeepCheckpoint();
            SceneFlow.Current.LoadZone("Title");float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            yield return Frames(2);var title=Object.FindFirstObjectByType<TitleScreen>();Assert.IsNotNull(title,"fixture: the title never came up");
            title.StartGame(true);yield return null;
            deadline=Time.unscaledTime+15;while((SceneFlow.Current.Loading||SceneFlow.Current.Zone!="Arena_Flight")&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual("Arena_Flight",SceneFlow.Current.Zone,"fixture: the Continue didn't come back to the yard");yield return Frames(3);Hold();
            log.Clear();var boss=Her(false);yield return Frames(3);
            foreach(var clamp in BridleClamp.All.OrderBy(c=>c.index))
                Assert.AreEqual(clamp.index<=2?BridleClamp.Hold.Open:BridleClamp.Hold.Shut,clamp.State,$"clamp {clamp.index} loads {clamp.State}: what was opened is kept open, and nobody opens a clamp twice");
            Assert.AreEqual(2,boss.MotherOpen,"she doesn't count the two clamps kept open");
            Assert.LessOrEqual(boss.Health.integrity,boss.Health.maximum*.5f,"she wakes over the line the two open clamps leave her");
            Assert.AreEqual(3,boss.Phase,"she wakes in the wrong phase for two clamps off");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsFalse(log.Any(l=>l.StartsWith("BOSS_SHIFT")),"a shift played over the load");
            boss.GetComponent<EnemyBrain>().Passive=true;
        }

        // ------------------------------------------------------------------ a conversation holds her (D275)
        /// <summary>With three clamps off the fourth shelf swings on her strain; a conversation holding the controls holds it where it is, and her marking with it (its fill, its clock),
        /// and nothing lands on anyone; the moment the talk ends both go on from where they stood.</summary>
        [UnityTest] public IEnumerator AConversationHoldsHerMarkingsAndTheFourthShelfsSwingWhereTheyStand()
        {
            roomClamps=4;yield return Load("Arena_Flight");
            foreach(int i in new[]{1,2,3})GameServices.Current.Flags.SetBool("end.clamp"+i,true);
            var boss=Her(false);yield return Frames(3);
            var shelf=Berth.Find("Clamp4");var strain=shelf.GetComponent<MotherStrain>();Assert.IsNotNull(strain,"fixture: the fourth shelf swings");
            var lead=Lead;Fresh(lead);Fresh(Partner);Put(lead,C+Vector3.back*26);Put(Partner,C+Vector3.back*30+Vector3.right*5);
            yield return Until(()=>strain.Clock>2,6,lead);
            Assert.Greater(strain.Clock,2,"with three clamps off the fourth shelf doesn't swing on her strain");
            yield return Until(()=>!boss.Busy&&!boss.Health.Broken,14,lead);boss.GetComponent<EnemyBrain>().Passive=true;
            Assert.IsTrue(boss.Perform("Sweep"),"fixture: her sweep");yield return Frames(3);
            var mark=Marks(boss).First();var input=GameServices.Current.Input;
            Vector3 shelfAt=shelf.transform.position;float elapsed=mark.Elapsed,clock=strain.Clock,bar=lead.Health.integrity;
            boss.GetComponent<EnemyBrain>().Passive=false;
            // Built wrong on purpose: the talk never takes the controls.
            for(float until=Time.time+2.5f;Time.time<until;){input.Blocked=!Broken;yield return null;}
            var failures=new List<string>();
            if(Flat(shelf.transform.position,shelfAt)>.05f)failures.Add($"the fourth shelf swung {Flat(shelf.transform.position,shelfAt):0.00} m through a conversation");
            if(strain.Clock-clock>.05f)failures.Add($"the swing's clock ran {strain.Clock-clock:0.00} s through a conversation");
            if(mark.Elapsed-elapsed>.05f)failures.Add($"her marking filled {mark.Elapsed-elapsed:0.00} s through a conversation");
            if(lead.Health.integrity<bar)failures.Add("something of hers landed through a conversation");
            input.Blocked=false;
            yield return Until(()=>mark.Released,4,lead);
            if(!mark.Released)failures.Add("her marking never landed once the talk ended");
            yield return Until(()=>strain.Clock>clock+1,4,lead);
            if(strain.Clock<=clock+.5f)failures.Add("the fourth shelf never swung on once the talk ended");
            Assert.IsEmpty(failures,string.Join("\n",failures));
            boss.GetComponent<EnemyBrain>().Passive=true;
        }

        // ------------------------------------------------------------------ her release
        /// <summary>With the fourth clamp open every clamp lets go: she is counted down (and never called defeated), lifts her head, turns and goes up her road with her coil after her,
        /// gone in her release's time; the fight's music goes out and nothing barks until the zone's after-talk has been heard, and then the place's music comes back.</summary>
        [UnityTest] public IEnumerator WithTheFourthClampOpenSheGoesUpHerRoadInQuietUntilTheAfterTalk()
        {
            // Built wrong on purpose: her way out stood on the camera's side, south of her, where the party's lanes are: she would go through them.
            roomExit=true;roomExitSouth=Broken;roomClamps=4;yield return Load("Arena_Flight");
            foreach(int i in new[]{1,2,3})GameServices.Current.Flags.SetBool("end.clamp"+i,true);
            var boss=Her(false);yield return Frames(3);var coil=boss.MotherBody;Assert.IsNotNull(coil,"fixture: her body");
            var tail=coil.Drums[coil.Drums.Count-1];var tailFrom=tail.position;var berth=Berth.Find("Clamp4");
            var lead=Lead;Fresh(lead);Fresh(Partner);
            yield return Until(()=>berth.MayMove,2,lead);Put(lead,berth.pad.transform.position);yield return Press(berth.pad);
            var seat=BridleClamp.All.First(c=>c.index==4);lead=Lead;Fresh(lead);Fresh(Partner);
            for(float end=Time.time+40;!Has("bossdown.MotherOfDoors")&&Time.time<end;)
            {
                foreach(var hero in PartyController.Current.members)if(hero.Health.Alive)hero.motor.Move(Vector2.zero,false,false);
                // The partner has the tenders (that is measured elsewhere): here they are simply stopped.
                foreach(var t in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>Tended.IsTender(e.Health)&&e.Health.Alive))t.Health.Receive(new DamagePacket{source=Partner.Health,amount=1e7f,type=DamageType.Pulse});
                if(seat.Holder==null&&lead.CanAct&&lead.State!=ActorState.Dodge){if(Flat(lead.transform.position,seat.transform.position)>.5f)Put(lead,seat.transform.position);if(seat.Available)seat.Interact();}
                lead.Health.InvulnerableUntil=GameTime.Now+1;
                yield return null;
            }
            Assert.IsTrue(Has("bossdown.MotherOfDoors"),"the fourth clamp open, she isn't counted down");Assert.IsTrue(Said("MOTHER_GOES"),"she didn't go");
            Assert.IsTrue(log.Any(l=>l.StartsWith("CLAMPS_LET_GO")),"her clamps didn't let go together");
            yield return new WaitForSecondsRealtime(1.5f);
            var failures=new List<string>();
            if(MusicDirector.Current!=null&&MusicDirector.Current.Track!=null)failures.Add($"the music plays {MusicDirector.Current.Track} as she goes: it goes out until SurvivorMotherGoes");
            if(!BarkService.Hushed)failures.Add("something may bark over her going");
            // SurvivorMotherGoes opens as she goes (the zone's after-talk, 6 s in): her going is that talk's own staging and goes on through it, moving no hero (D275's other half).
            // Held with the controls, she stood frozen half way up her road for the whole of Dace's line (QaDebug release stills, November 14).
            var input=GameServices.Current.Input;var heldFrom=boss.transform.position;
            for(float until=Time.unscaledTime+1.5f;Time.unscaledTime<until;){input.Blocked=true;yield return null;}
            input.Blocked=false;float held=Flat(boss.transform.position,heldFrom);TestContext.WriteLine($"MOTHER_HELD_MOVED {held:0.0}");
            if(held<8)failures.Add($"her going stood still through a conversation: {held:0.0} m in 1.5 s");
            yield return Until(()=>Said("MOTHER_GONE"),BossController.MotherTune.ReleaseSeconds+2);
            if(!Said("MOTHER_GONE"))failures.Add("she wasn't gone in her release's time");
            var goneAt=log.FirstOrDefault(l=>l.StartsWith("MOTHER_GONE"));TestContext.WriteLine("MOTHER_RELEASE "+goneAt);
            if(tail!=null&&tail.position.z<C.z+60)failures.Add($"her tail ended at {tail.position} (from {tailFrom}): not up her road past its mouth, it never left the crater after her");
            if(boss!=null&&boss.transform.position.z<C.z+70)failures.Add($"her head ended at {boss.transform.position}, not up her road to the north");
            GameServices.Current.Flags.SetBool("end.motherFreed",true);
            yield return new WaitForSecondsRealtime(1);
            if(!Said("MOTHER_QUIET_ENDS"))failures.Add("the quiet after her never ended when the after-talk had been heard");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        /// <summary>The view that draws back to show her going keeps a ship in flight in it. In flight it held her head alone, so a launch from the east clamp's works while she went put the
        /// flight controls on screen with no ship anywhere for two stills, and then an empty crater (whole-game capture, November 20, s63/162 and 163). The lead flies 40 m south of her
        /// middle, out of an ordinary fight's framing, and must stay on screen from the moment the view has had a second to move until she is gone.</summary>
        [UnityTest] public IEnumerator AShipInFlightStaysInTheViewThatShowsHerGoing()
        {
            roomExit=true;roomClamps=4;yield return Load("Arena_Flight");
            var boss=Her(true);yield return Frames(3);var coil=boss.MotherBody;Assert.IsNotNull(coil,"fixture: her body");
            var lead=Lead;Fresh(lead);Fresh(Partner);Assert.IsTrue(lead.flight,"fixture: the lead isn't flying in the flight yard");
            Put(lead,C+Vector3.back*40);Put(Partner,C+Vector3.back*40+Vector3.right*4.4f);yield return Frames(30);
            var view=Camera.main;Assert.IsNotNull(view,"fixture: no camera");var rig=Object.FindFirstObjectByType<CameraRig>();Assert.IsNotNull(rig,"fixture: no camera rig");
            bool InView(){var q=view.WorldToViewportPoint(lead.transform.position+Vector3.up*.8f);return q.z>0&&q.x>0&&q.x<1&&q.y>0&&q.y<1;}
            // The check first, on the view as it was: framed on her head alone, half way up her road.
            var owner=new GameObject("Her head alone, as the view had it");var alone=new Bounds(C+Vector3.forward*50+Vector3.up*2,new Vector3(14,8,14));bool lostAlone=false;
            for(float until=Time.unscaledTime+2;Time.unscaledTime<until;){rig.FrameSceneInterest(owner,alone,new Rect(.08f,.16f,.84f,.72f));yield return null;if(Time.unscaledTime>until-.6f&&!InView())lostAlone=true;}
            rig.FrameSceneInterest(owner,null,new Rect(.08f,.16f,.84f,.72f));Object.Destroy(owner);
            Assert.IsTrue(lostAlone,"fixture: framed on her head alone the ship stays in view, so the check can't see what s63/162 showed");
            yield return Frames(30);
            coil.Release();
            int seen=0,lost=0;string first=null;
            for(float start=Time.time,end=Time.time+BossController.MotherTune.ReleaseSeconds+2;Time.time<end&&!coil.Gone;)
            {
                foreach(var hero in PartyController.Current.members)if(hero.Health.Alive)hero.motor.Move(Vector2.zero,false,false);
                lead.Health.InvulnerableUntil=GameTime.Now+1;
                yield return null;
                if(Time.time-start<1)continue;
                if(InView())seen++;else{lost++;first??=$"{Time.time-start:0.0} s in, the lead at {lead.transform.position} is at {view.WorldToViewportPoint(lead.transform.position)} of the view, her head at {boss.transform.position}";}
            }
            TestContext.WriteLine($"MOTHER_GOING_VIEW seen={seen} lost={lost}");
            Assert.IsTrue(coil.Gone||Said("MOTHER_GONE"),"fixture: she wasn't gone in her release's time");
            Assert.AreEqual(0,lost,$"the ship in flight went out of the view showing her going for {lost} of {seen+lost} frames; first: {first}");
        }

        // ------------------------------------------------------------------ her length, really docking
        /// <summary>The clamps' guns are the first combat engineer's encounters, not hers: each is allowed this long (two guns flown down at the chapter's arrival).</summary>
        /// 12 since D362 (November 22), where 24 was allowed: today's scout was through her room, guns and all, in 103.8 s (Builds/quality/nov22-recon-g s63), and this test's own
        /// four cycles last took 56 s (nov21b-part6), which leaves 48 s for four clamps' guns.
        const float GunsSeconds=12;
        /// <summary>Her length in her room as a player has it, four times over: fly to the next shelf's pad (ships at their cruise, rolling out of what she lands on them), dock by the
        /// pad (stream A's berth, its fades and all), walk to the seat, take the clamp's weight and keep it as a player does (out of a wedge over the seat as it lands, back after; the partner
        /// stopping the tender with real blows), wait for her slack, and launch; the fourth shelf chased on her strain. With each clamp's guns allowed for, the whole is her sheet's bounds.</summary>
        [UnityTest] public IEnumerator HerFourCyclesOfFlyDockWorkAndLaunchLastAsHerSheetSays()
        {
            roomClamps=4;yield return Load("Arena_Flight");
            var boss=Her(false);yield return Frames(3);
            var lead=Lead;var partner=Partner;Fresh(lead);Fresh(partner);Put(lead,C+Vector3.back*40);Put(partner,C+Vector3.back*40+Vector3.right*4.2f);
            float started=Time.time;var legs=new List<string>();int rolls=0,drops=0,tenders=0;
            IEnumerator Fly(System.Func<Vector3> to)
            {
                for(float end=Time.time+30;Time.time<end;)
                {
                    var at=Lead.transform.position;var goal=to();goal.y=1;var way=goal-at;way.y=0;if(way.magnitude<2)break;
                    bool threat=boss.Telegraphing&&boss.TelegraphRemaining<.15f;
                    if(threat&&Lead.State!=ActorState.Dodge){Lead.Dodge(Vector3.Cross(Vector3.up,way).normalized);rolls++;}
                    else if(Lead.State!=ActorState.Dodge)Put(Lead,at+way.normalized*Mathf.Min(way.magnitude,12*Time.deltaTime));
                    foreach(var hero in PartyController.Current.members)if(hero.Health.Alive)hero.motor.Move(Vector2.zero,false,false);
                    if(Partner.Health.Alive&&Flat(Partner.transform.position,Lead.transform.position)>8)Put(Partner,Lead.transform.position+Vector3.right*4.2f);
                    yield return null;
                }
            }
            for(int i=1;i<=4;i++)
            {
                float t0=Time.time;var berth=Berth.Find("Clamp"+i);var seat=BridleClamp.All.First(c=>c.index==i);
                yield return Fly(()=>berth.pad.transform.position);
                for(float end=Time.time+20;!berth.pad.Available&&Time.time<end;)yield return null;
                float flown=Time.time-t0;yield return Press(berth.pad);lead=Lead;partner=Partner;Fresh(lead,lead.Health.integrity);
                float docked=Time.time;
                for(float end=Time.time+90;seat.State==BridleClamp.Hold.Shut&&Time.time<end;)
                {
                    foreach(var hero in PartyController.Current.members)if(hero.Health.Alive)hero.motor.Move(Vector2.zero,false,false);
                    bool threat=boss.Telegraphing&&boss.TelegraphRemaining<.15f;
                    if(threat&&lead.State!=ActorState.Dodge){if(seat.Holder!=null)drops++;lead.Dodge(lead.transform.position-C);rolls++;}
                    else if(lead.State!=ActorState.Dodge&&lead.CanAct&&seat.Holder==null)
                    {
                        var to=seat.transform.position;var gap=to-lead.transform.position;gap.y=0;
                        if(gap.magnitude>.5f)Put(lead,lead.transform.position+gap.normalized*Mathf.Min(gap.magnitude,6*Time.deltaTime));else if(seat.Available)seat.Interact();
                    }
                    // The partner covers: at a tender a beat after it steps out, with her own blows. Built wrong on purpose: nobody covers.
                    var tender=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).FirstOrDefault(e=>Tended.IsTender(e.Health)&&e.Health.Alive);
                    if(!Broken&&tender!=null&&partner.CanAct&&Time.time-docked>1.5f)
                    {
                        if(Flat(partner.transform.position,tender.transform.position)>1.8f)Put(partner,tender.transform.position+Vector3.ProjectOnPlane(partner.transform.position-tender.transform.position,Vector3.up).normalized*1.4f);
                        partner.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(tender.transform.position-partner.transform.position,Vector3.up));partner.target=tender.Health;partner.Attack();
                    }
                    if(!lead.Health.Alive||!partner.Health.Alive){Fresh(lead);Fresh(partner);}
                    yield return null;
                }
                tenders+=log.Count(l=>l.StartsWith("MOTHER_TENDERS"))-tenders;
                Assert.AreNotEqual(BridleClamp.Hold.Shut,seat.State,$"clamp {i} never opened in 90 s on its shelf");
                float worked=Time.time-docked;
                if(i<4){for(float end=Time.time+5;!berth.launch.Available&&Time.time<end;)yield return null;yield return Press(berth.launch);}
                legs.Add($"clamp {i}: flown {flown:0.0}s, on the shelf {worked:0.0}s");
            }
            float fight=Time.time-started,whole=fight+4*GunsSeconds;
            string line=$"MOTHER_LENGTH fight={fight:0}s guns={4*GunsSeconds:0}s whole={whole:0}s rolls={rolls} drops={drops} :: {string.Join("; ",legs)}";
            TestContext.WriteLine(line);Debug.Log(line);
            Assert.IsTrue(Has("bossdown.MotherOfDoors"),"the fourth clamp open, she isn't counted down");
            Assert.GreaterOrEqual(whole,MinSeconds,line+": shorter than her sheet's bounds");Assert.LessOrEqual(whole,MaxSeconds,line+": longer than her sheet's bounds");
        }

        /// <summary>Her raised head is drawn 16 m wide and 9 m out from her middle, over a 4 m capsule: a ship flown at her from the south stopped under her snout, inside her head, and one from
        /// the north or under a horn stood where the fixed camera (19 m over, 17 m south, 48 degrees) sees her head and not the ship (stills mother-c-03). In her room a hull flown straight at
        /// her from the south, the east, the north and the north-east stops where nothing of her can stand between it and the lens: past her snout and her horns (11 m), past where her neck
        /// stands over the plane behind her head (16.5 m north).</summary>
        [UnityTest] public IEnumerator NoHullComesUnderHerHeadOrBehindItWhereItCouldBeHidden()
        {
            roomClamps=4;yield return Load("Arena_Flight");
            var boss=Her(true);yield return Frames(3);
            // Built wrong on purpose: her capsule alone, as she was.
            if(Broken){var solid=boss.transform.Find("Her body at the plane");if(solid!=null)Object.Destroy(solid.gameObject);yield return Frames(2);}
            var ship=Lead;Fresh(ship);Fresh(Partner);Put(Partner,C+Vector3.back*60);
            foreach(var shape in boss.GetComponentsInChildren<Collider>())if(!shape.isTrigger)TestContext.WriteLine($"MOTHER_SOLID {shape.GetType().Name} {shape.name} bounds={shape.bounds.min}..{shape.bounds.max}");
            TestContext.WriteLine($"MOTHER_HULL {ship.character} radius={ship.GetComponent<CharacterController>().radius:0.00}");
            var body=ship.GetComponent<CharacterController>();var failures=new List<string>();
            // Where a hull comes to rest is what matters, not how far along the way it was flown (one meeting her neck at a slant slides round it): never within 11 m of her middle, and
            // never behind her head and neck (north of her middle, within their 8 m half-width) within 16.5 m.
            foreach(float bearing in new[]{180f,90f,0f,35f,325f})
            {
                Put(ship,On(bearing,30,1));yield return Frames(2);var inward=-Heading(bearing);
                for(float t=0;t<3.5f;t+=Time.deltaTime){if(body.enabled)body.Move(inward*12*Time.deltaTime);yield return null;}
                var at=ship.transform.position;float r=Flat(at,C),dx=Mathf.Abs(at.x-C.x),dz=at.z-C.z;TestContext.WriteLine($"MOTHER_NEAREST bearing={bearing} r={r:0.0} at=({dx:0.0} across, {dz:0.0} north)");
                if(r<11)failures.Add($"a hull flown at her from bearing {bearing} came to {r:0.0} m of her middle: under her snout or a horn, where the camera can't see it");
                else if(dz>0&&dx<8.5f&&r<16.5f)failures.Add($"a hull flown at her from bearing {bearing} came to rest {dz:0.0} m behind her middle and {dx:0.0} m across: behind her head and neck, where the camera can't see it");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
            boss.GetComponent<EnemyBrain>().Passive=true;
        }

        // ------------------------------------------------------------------ her body
        /// <summary>One creature: her coil is laid round her middle as she is made (before her fight wakes her), every drum a drum's length from the next with nothing between, her neck
        /// running back from her head into it, all of it under the plane but her head and neck, and it stays where it was laid when her root is turned. A Cantor-sized chain behind a
        /// 16 m head (the broken state) reads as a head beside a pile of parts.</summary>
        [UnityTest] public IEnumerator HerHeadAndCoilAreOneUnbrokenBodyUnderThePlaneRoundHerMiddle()
        {
            yield return Load("Arena_Flight");
            var boss=Her();yield return Frames(3);var coil=boss.MotherBody;Assert.IsNotNull(coil,"no coil was laid as she was made");
            var drums=coil.Drums.Where(d=>d!=null).ToList();
            // Built wrong on purpose: the Cantor's own chain on her, seven drums 2.2 m apart.
            if(Broken){var serpent=boss.gameObject.AddComponent<SerpentSegments>();serpent.Initialize(Def("MotherOfDoors"),boss.Health);yield return Frames(2);drums=serpent.Parts.ToList();}
            var failures=new List<string>();
            float headLength=0;foreach(var r in coil.Head.GetComponentsInChildren<Renderer>())headLength=Mathf.Max(headLength,r.bounds.size.z,r.bounds.size.x);
            float body=0;for(int i=1;i<drums.Count;i++){float gap=Vector3.Distance(drums[i-1].position,drums[i].position);body+=gap;if(gap>10)failures.Add($"drums {i-1} and {i} lie {gap:0.0} m apart: a break in her body");}
            TestContext.WriteLine($"MOTHER_BODY drums={drums.Count} head={headLength:0.0}m body={body:0}m");
            if(drums.Count<BossController.MotherTune.Drums)failures.Add($"her coil is {drums.Count} drums");
            if(body<headLength*4)failures.Add($"her body runs {body:0} m behind a {headLength:0} m head: a head beside a few parts, not a coiled creature");
            if(Vector3.Distance(drums[0].position,boss.transform.position)>BossController.MotherTune.HeadBack+8)failures.Add("her first drum isn't at her head's back: her neck is missing");
            for(int i=2;i<drums.Count;i++)if(drums[i].position.y>1)failures.Add($"drum {i} stands over the plane at y {drums[i].position.y:0.0}");
            float round=drums.Skip(2).Average(d=>Flat(d.position,C));if(Mathf.Abs(round-BossController.MotherTune.CoilRadius)>3)failures.Add($"her coil lies {round:0.0} m round her middle, not {BossController.MotherTune.CoilRadius}");
            var at=drums.Select(d=>d.position).ToArray();boss.transform.rotation=Quaternion.Euler(0,40,0);yield return Frames(3);
            for(int i=0;i<drums.Count;i++)if(Vector3.Distance(drums[i].position,at[i])>1.5f){failures.Add("her coil swung round with her root: it is laid where she lies");break;}
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        #region The Tollkeeper (stream G: chapter 10's second boss)
        // ================================================================== the Tollkeeper, in the flight yard with its room stood as docs/maps/LastMooring.md has it ("What the fight
        // needs of the room"): the gate's middle TM (the zone's (0, 1, 468)), the hinges 40 m either side, the carrier under the plane 24 m north, the arm run out 18 m south of the gate
        // with its deck 3 m under the plane, its berth (stream A's), the motor's seat and housing, the door's spawn. Every offset is the zone's, from the gate's middle. Each test builds its
        // fixture wrong first under TOLLKEEPER_BROKEN=1 (this region's own runs only), so the run shows every check failing the way it should.
        static readonly Vector3 TM=new(300,1,360);
        static bool TollBroken=>System.Environment.GetEnvironmentVariable("TOLLKEEPER_BROKEN")=="1";
        static Vector3 TollAt(float x,float y,float z)=>TM+new Vector3(x,y,z);
        // TollShare .40 since D362 (November 22): today's scouts were through its room in 116.6 s (Builds/quality/nov22-recon-a-65 s65) and 122.4 s (nov22-recon-g s65) at 68,000,
        // and 145.9 s at 81,000 (nov22-recon-h-65), against its yard rate read five times today at 1,130 to 1,570 a second (d355-dome3, d355-lengths, d362-shared,
        // d362-final-shared): at their mean, 1,365, both builds give .407, rounded down. Its room is its braces and its motor's stages: the braces
        // went from 19,000 to 24,000 and its yard integrity from 68,000 to 77,000 with them (D362); the motor is on the arm's clock and stays 22,000.
        const float TollMin=120,TollMax=300,TollShare=.4f;
        static readonly List<string> tollLog=new();static bool tollListening,tollStand;
        static void TollListen()
        {
            if(!tollListening){tollListening=true;Application.logMessageReceived+=(text,stack,type)=>{if(text.StartsWith("TOLL_")||text.StartsWith("BOSS_")||text.StartsWith("BERTH_")||text.StartsWith("CHECKPOINT_KEPT"))tollLog.Add(text);};}
            tollLog.Clear();
        }
        static bool TollSaid(string start)=>tollLog.Any(l=>l.StartsWith(start));
        /// <summary>The flight yard with the Tollkeeper's room stood in it, as the zone stands it (the next load only).</summary>
        static IEnumerator TollYard(){TollListen();tollStand=true;SceneManager.sceneLoaded+=TollBuilt;yield return Load("Arena_Flight");}
        static void TollBuilt(Scene scene,LoadSceneMode mode)
        {
            if(scene.name!="Arena_Flight")return;SceneManager.sceneLoaded-=TollBuilt;if(!tollStand)return;tollStand=false;
            var room=new GameObject("The Tollkeeper's room");SceneManager.MoveGameObjectToScene(room,scene);
            var arm=new GameObject("The Tollkeeper's arm");arm.transform.SetParent(room.transform,false);arm.transform.position=TollAt(0,-3,-18);
            var deck=GameObject.CreatePrimitive(PrimitiveType.Cube);deck.name="Arm deck";deck.transform.SetParent(arm.transform,false);deck.transform.position=TollAt(0,-3.3f,-18);deck.transform.localScale=new Vector3(6,.6f,12);
            var housing=GameObject.CreatePrimitive(PrimitiveType.Cube);housing.name="The gate motor's housing";housing.transform.SetParent(arm.transform,false);housing.transform.position=TollAt(0,-2.3f,-12.6f);housing.transform.localScale=new Vector3(3.6f,1.4f,1.2f);
            var seat=new GameObject("Gate motor seat");seat.transform.SetParent(arm.transform,false);seat.transform.SetPositionAndRotation(TollAt(0,-3,-13.4f),Quaternion.Euler(0,180,0));
            Berth.Declare("Arm","The Tollkeeper's arm",ZoneKind.GroundCombat,new Bounds(TollAt(0,-2.4f,-18),new Vector3(6,1.2f,12)),TollAt(0,0,-18),TollAt(-1.6f,-3,-18.5f),TollAt(1.4f,-3,-22.6f),
                TollAt(10,0,-28),"ArmReturn",TollAt(0,0,-86),true,true,arm.transform);
            var door=new GameObject("Gate",typeof(SpawnPoint));door.transform.SetParent(room.transform,false);door.transform.position=TollAt(0,0,-96);door.GetComponent<SpawnPoint>().id="Gate";
            var cage=new GameObject("The Tollkeeper's cage").AddComponent<TollkeeperCage>();cage.transform.SetParent(room.transform,false);cage.transform.position=TM;
            cage.hingeWest=TollAt(-40,0,0);cage.hingeEast=TollAt(40,0,0);cage.carrier=TollAt(0,-30.6f,24);cage.lanesRun=80.5f;cage.dividers=new[]{-18f,18f};cage.outer=58;
            cage.arm=arm.transform;cage.armOut=TollAt(0,-3,-18);cage.armIn=TollAt(0,-9,18);cage.seat=seat.transform;cage.berthId="Arm";
            var def=GameCatalog.Find<EnemyDef>("Tollkeeper");cage.outriggerPrefab=def!=null?def.bodySegment:null;
            Physics.SyncTransforms();
        }
        static BossController Toll(bool passive=false)
        {
            var enemy=ActorFactory.Enemy(Def("Tollkeeper"),TollAt(0,-30.6f,24));enemy.transform.rotation=Quaternion.Euler(0,180,0);enemy.Passive=passive;return enemy.GetComponent<BossController>();
        }
        static TollkeeperCage Cage=>TollkeeperCage.Current;
        /// <summary>A brace struck through by the lead's own blows (a ship's weapon type it is weak to), as many as it takes.</summary>
        static void BreakBrace(TollkeeperCage.Brace brace)
        {
            for(int i=0;i<200&&brace.health.Alive;i++)brace.health.Receive(new DamagePacket{source=Lead.Health,amount=500,type=DamageType.Pulse,weight=HitWeight.Medium});
        }
        static IEnumerator ArmOut(BossController boss,float seconds=8){for(float end=Time.time+seconds;!boss.TollArmOut&&Time.time<end;){foreach(var hero in PartyController.Current.members)if(hero.Health.Alive)hero.motor.Move(Vector2.zero,false,false);yield return null;}}
        /// <summary>A brace broken and the arm out, the party docked on it as a player docks.</summary>
        static IEnumerator DockOnTheArm(BossController boss,int brace=0)
        {
            var b=Cage.braces[brace];if(!b.broken){Put(Lead,b.node.position+new Vector3(0,0,-8));yield return Frames(2);BreakBrace(b);}
            yield return ArmOut(boss);Assert.IsTrue(boss.TollArmOut,"fixture: the arm never ran out");
            var berth=Berth.Find("Arm");Put(Lead,berth.pad.transform.position);Put(Partner,berth.pad.transform.position+Vector3.right*4.2f);yield return Frames(2);
            yield return Press(berth.pad);Assert.IsTrue(Berth.Current==berth&&!Lead.flight,"fixture: the party is on the arm on foot");
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Tollkeeper(ContentRegistry.Book book)
        {
            // Met at Sync 28 (D296: 28.1) at Last Mooring's gate once Maru's section holds (end.sectionHeld). In its room nobody strikes the carrier and its bar is its motor's three stages; in
            // the yard, where these are measured, it fights on integrity as any boss, every move aimed at the lead. Its share is what the replay driver's pad got from its door in the zone (MooringBossReplayTests, November 15: .21 to .26 of the pair's flat out at mastery 5, fights of 139 to 160 s), the lower, rounded down.
            book.Boss(new ContentRegistry.BossSheet{id="Tollkeeper",zone="LastMooring",door="Tollkeeper",doorFlags=new[]{"end.sectionHeld"},flight=true,sync=28,driverShare=TollShare,
                minSeconds=TollMin,maxSeconds=TollMax,
                specials=new[]{("Guns",new Vector3(0,0,12)),("Contract",new Vector3(0,0,14)),("Surge",new Vector3(0,0,10)),("Rake",new Vector3(0,0,10))},
                // Every move ends in an opening in a yard; the guns' is the one a player waits for.
                windows=new[]{("Guns",new Vector3(0,0,12)),("Guns",new Vector3(0,0,12)),("Guns",new Vector3(0,0,12))}});
        }

        // ------------------------------------------------------------------ its definition, its words
        /// <summary>Its EnemyDef and its Kit came in one change, and every word of it on screen is the manifest's: its name and epithet (section 5), the panel's two lines (section 6: the cage
        /// closing, the arm), its state lines in every situation the fight has. Nothing calls it defeated or killed: it is shut down. Its music is Glitch Boss; its capsule stands; its brain
        /// has no blow of its own; its integrity is the builder's literal; its XP is the ledger's.</summary>
        [UnityTest] public IEnumerator TheTollkeepersDefinitionAndKitCameTogetherAndEveryWordIsTheManifests()
        {
            yield return TollYard();
            var def=Def("Tollkeeper");
            Assert.IsTrue(def.boss,"it isn't a boss");Assert.IsTrue(BossController.Registered("Tollkeeper"),"its definition exists without its Kit");
            Assert.AreEqual("THE TOLLKEEPER",def.displayName);Assert.AreEqual("Glitch Boss",def.bossMusicId,"its fight doesn't name its track");Assert.AreEqual(1600,def.xp,"its XP isn't the ledger's 1,600 (D296)");
            Assert.GreaterOrEqual(def.capsuleHeight,BossController.CapsuleRadius("Tollkeeper")*2,"its capsule is shorter than twice its radius");
            Assert.IsFalse(def.attacks.Any(a=>a!=null&&a.range>0),"its brain has a blow that reaches someone: every blow it lands is a marked special");
            Assert.AreEqual(BossController.Ending.ShutDown,BossController.EndingOf("Tollkeeper"),"the carrier is to be shut down, not destroyed");
            Assert.IsNotNull(def.bodySegment,"its outrigger isn't on its definition: the cage has no bars");
            string source=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"_Project/Scripts/Editor/World/LastMooringBuilder.Tollkeeper.cs"));
            StringAssert.Contains($"e.integrity={def.integrity:0};",source,"a rebuild would put its old integrity back");
            var boss=Toll();yield return Frames(3);
            // Built wrong on purpose: the epithet as a first draft might have had it.
            string epithet=TollBroken?"The Compact's carrier at the gate":"An anchor-fleet carrier, with a cage for a gate";
            Assert.AreEqual(epithet,boss.Epithet,"its epithet isn't the manifest's");Assert.AreEqual("THE TOLLKEEPER",boss.DisplayName);
            Assert.IsTrue(boss.TollRoom,"fixture: its room");Assert.IsFalse(boss.Health.Targetable,"in its room the carrier can be struck or locked onto: its bar is its motor");
            var failures=new List<string>();var words=new List<string>{boss.DisplayName,boss.Epithet,boss.ShiftText};
            void Say(string line,string expected,string where){words.Add(line);if(line!=expected)failures.Add($"{where}: \"{line}\", not \"{expected}\"");}
            Put(Lead,TollAt(0,0,-40));Fresh(Lead);Fresh(Partner);yield return Frames(2);
            Say(boss.FightGoal("zone"),"Break a brace on the cage (0/3)","in flight before a brace (the count is what is done)");Say(boss.StatusText,"THE CAGE IS BRACED  ·  BREAK A CAGE BRACE","its bar before a brace");
            yield return DockOnTheArm(boss);
            Say(boss.FightGoal("zone"),"Stop the gate motor before the arm draws in","on the arm (manifest section 6)");Say(boss.StatusText,"THE ARM IS DOCKED  ·  STOP THE GATE MOTOR","its bar on the arm");
            Assert.IsTrue(Cage.MotorOpen,"on the arm the gate motor can't be struck");
            var berth=Berth.Find("Arm");yield return Press(berth.launch);
            GameServices.Current.Flags.SetBool(BossController.TollTune.StageFlag(1),true);
            boss.GetComponent<EnemyBrain>().Passive=true;Object.Destroy(boss.gameObject);yield return Frames(2);
            yield return TollYard();GameServices.Current.Flags.SetBool(BossController.TollTune.StageFlag(1),true);boss=Toll();yield return Frames(3);
            Put(Lead,TollAt(-16,0,-14));Fresh(Lead);Fresh(Partner);Put(Partner,TollAt(60,0,-60));
            yield return Until(()=>!boss.Busy,8,Lead);Assert.IsTrue(boss.Perform("Contract"),"fixture: the cage contracts");yield return Frames(2);
            Say(boss.FightGoal("zone"),"Weave out before the cage closes","the cage closing (manifest section 6)");Say(boss.StatusText,"THE CAGE IS CLOSING  ·  WEAVE OUT","its bar as the cage closes");
            foreach(var line in words)foreach(string bad in new[]{"defeat","DEFEAT","kill","KILL","destroy","DESTROY"})if(line.Contains(bad))failures.Add($"a line of its says {bad}: {line}");
            Assert.IsEmpty(failures,string.Join("\n",failures));
            boss.GetComponent<EnemyBrain>().Passive=true;
        }

        // ------------------------------------------------------------------ the numbers it is tuned by
        /// <summary>The standing rule (CHAPTER_GUIDE 0.1): a boss's blow costs a hero 12 to 16% of the bar and more than anything on the road to it, measured through Health.Receive on all three
        /// heroes at Sync 28 and 29 in the Choirwrought parts chapter 10 arrives in. Its stages add nothing to a blow (they bring more and quicker).</summary>
        [UnityTest] public IEnumerator EveryBlowOfTheTollkeepersTakesABossesShareOfAnyHerosBarAndMoreThanTheRoadToIt()
        {
            yield return Load("Arena_Flight");
            var source=ActorFactory.Enemy(Def("Tollkeeper"),new Vector3(200,1,0));source.Passive=true;yield return Frames(2);
            var roadIds=new[]{"StillwaterInterceptor","StillwaterWarden","ClampGun","StillwaterTender","Interceptor","ClampTender"};
            var road=roadIds.Select(id=>GameCatalog.Find<EnemyDef>(id)).Where(d=>d!=null&&!d.boss).SelectMany(d=>d.attacks.Where(a=>a!=null).Select(a=>(id:d.id,damage:a.damage))).ToList();
            var moves=new List<(string what,float damage)>{("Guns",BossController.TollTune.GunDamage),("Contract",BossController.TollTune.CageDamage),("Surge",BossController.TollTune.SurgeDamage),("Rake",BossController.TollTune.RakeDamage)};
            // Built wrong on purpose: a gun authored at 230 is over the band, and the check has to say so.
            if(TollBroken)moves.Add(("a gun authored at 230",230));
            var failures=new List<string>();
            foreach(var pair in new[]{new[]{"Taren","Idra"},new[]{"Sela","Idra"}})
            {
                yield return Deploy(pair);yield return Wear("FramesT5");
                foreach(int sync in new[]{28,29})
                {
                    yield return Level(sync);
                    foreach(var hero in PartyController.Current.members)
                    {
                        if(pair[0]=="Sela"&&hero.character=="Idra")continue;
                        float bar=hero.Health.maximum;
                        float Share(float damage){hero.Health.integrity=bar;hero.Health.InvulnerableUntil=0;return hero.Health.Receive(new DamagePacket{source=source.Health,amount=damage,type=DamageType.Kinetic,weight=HitWeight.Heavy})/bar;}
                        float heaviest=0;string named="nothing";foreach(var (id,damage) in road){float s=Share(damage);if(s>heaviest){heaviest=s;named=id;}}
                        foreach(var (what,damage) in moves)
                        {
                            float share=Share(damage);string line=$"TOLL_HIT {hero.character} sync={sync} plating={hero.Plating:0} {what} {damage:0} share={share:P1} road={named} {heaviest:P1}";
                            TestContext.WriteLine(line);
                            if(share<.12f||share>.16f)failures.Add(line+": a boss's blow is 12 to 16% of the bar");
                            if(share<=heaviest)failures.Add(line+": no more than the road to it");
                        }
                    }
                }
            }
            var z=GameCatalog.Find<ZoneDef>("LastMooring");if(z!=null&&!Mathf.Approximately(z.enemyThreat,1))failures.Add($"LastMooring is at threat {z.enemyThreat}");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ the cycle: a brace, the arm, the motor
        /// <summary>A brace is struck only from flight and only while no dock is owed; broken, it is counted (end.brace1) and kept on the disk at once without moving where a defeat returns
        /// (D285), the party says so (tollbrace), and the arm runs out of the gate with the motor on it (TOLL_ARM_OUT); while that dock is owed the other braces can't be struck. On foot
        /// the motor can be struck, from a ship it can't.</summary>
        [UnityTest] public IEnumerator ABraceBrokenFromFlightRunsTheArmOutAndIsKeptAtOnce()
        {
            yield return TollYard();
            State.spawn="Gate";GameServices.Current.Saves.Save("autosave",State);
            var boss=Toll();yield return Frames(3);Fresh(Lead);Fresh(Partner);
            var failures=new List<string>();var brace=Cage.braces[1];
            Put(Lead,brace.node.position+new Vector3(6,0,-6));yield return Frames(3);
            if(!brace.health.Targetable)failures.Add("in flight with nothing owed, a brace can't be struck");
            if(Cage.MotorOpen)failures.Add("from a ship the gate motor can be struck");
            if(boss.TollArmOut)failures.Add("the arm stands out before a brace is broken");
            // Built wrong on purpose: struck from the deck, on foot, where a brace isn't the party's to strike.
            if(TollBroken){var berth=Berth.Find("Arm");Cage.SetArm(1);Put(Lead,berth.pad.transform.position);yield return Frames(2);yield return Press(berth.pad);yield return Frames(2);}
            BreakBrace(brace);yield return Frames(2);
            if(brace.health.Alive)failures.Add("the brace never broke");
            if(!Has("end.brace1"))failures.Add("the first brace broken isn't counted (end.brace1)");
            if(!OnDisk("end.brace1")||!OnDisk(BossController.TollTune.BraceFlag(1)))failures.Add("a brace broken isn't on the disk at once: a defeat would mend it (D285)");
            if(GameServices.Current.Saves.Load("autosave").spawn!="Gate")failures.Add("keeping the brace moved where a defeat returns");
            // The arm reads as out (TollArmOut, what its bar says) on the frame it says so (TOLL_ARM_OUT, with its bark and its sound). The words went at .999 of the way and the line
            // at the whole of it, a frame or two apart, so this check read the log before the line came about one run in five. The arm's last tenth is played slowly, so that it spends
            // many frames between the two: a gap between them is found every time.
            for(float end=Time.time+8;Cage.ArmOut<.9f&&!boss.TollArmOut&&Time.time<end;){foreach(var hero in PartyController.Current.members)if(hero.Health.Alive)hero.motor.Move(Vector2.zero,false,false);yield return null;}
            Time.captureDeltaTime=.001f;
            try{yield return ArmOut(boss);}finally{Time.captureDeltaTime=0;}
            if(!boss.TollArmOut)failures.Add("a brace broken, the arm never ran out");
            if(!TollSaid("TOLL_ARM_OUT"))failures.Add("no TOLL_ARM_OUT in the log on the frame the arm reads as out");
            if(Cage.braces.Any(b=>!b.broken&&b.health.Targetable))failures.Add("with a dock owed, another brace can still be struck");
            Assert.IsEmpty(failures,string.Join("\n",failures));
            boss.GetComponent<EnemyBrain>().Passive=true;
        }
        /// <summary>On the arm: the arm's clock is a band along its deck, laid on the deck's floor; the motor's surge and the turret's rake are marked on the deck too and strike a hero on
        /// foot there (they are painted onto it); the motor's blows bring the bar down; a Clamp Tender at the motor turns three blows in four; with the stage stopped the bar passes its line,
        /// the stage is kept on the disk, the shift pulls the cage in, and the arm's last clock says launch.</summary>
        [UnityTest] public IEnumerator OnTheArmWhatReachesTheDeckIsMarkedOnItAndTheMotorsBlowsAreTheBar()
        {
            yield return TollYard();
            State.spawn="Gate";GameServices.Current.Saves.Save("autosave",State);
            GameServices.Current.Flags.SetBool(BossController.TollTune.BraceFlag(0),true);GameServices.Current.Flags.SetBool("end.brace1",true);
            var boss=Toll();yield return Frames(3);Fresh(Lead);Fresh(Partner);
            yield return DockOnTheArm(boss,0);
            var lead=Lead;Fresh(lead);Fresh(Partner);float deck=TollAt(0,-3,0).y;var failures=new List<string>();
            if(boss.TollBand==null)failures.Add("docked, the arm's clock isn't marked");
            else if(Mathf.Abs(boss.TollBand.Origin.y-deck)>.3f)failures.Add($"the arm's clock is laid at y {boss.TollBand.Origin.y:0.0}, not on the deck at {deck}");
            boss.GetComponent<EnemyBrain>().Passive=true;
            foreach(var move in new[]{"Surge","Rake"})
            {
                yield return Until(()=>!boss.Busy,8,lead);Fresh(lead);
                Put(lead,Cage.seat.position+Cage.seat.forward*1.4f);yield return Frames(2);
                var before=new HashSet<Telegraph>(Telegraph.Live);Assert.IsTrue(boss.Perform(move),"fixture: "+move);yield return null;
                var marks=Telegraph.Live.Where(t=>t!=null&&t.owner==boss&&!before.Contains(t)).ToArray();
                // Built wrong on purpose: asked of the plane over the deck, where nothing of the deck's is.
                float floor=TollBroken?TM.y:deck;
                foreach(var m in marks)if(Mathf.Abs(m.Origin.y-floor)>.3f)failures.Add($"its {move} is laid at y {m.Origin.y:0.0}, not on the deck at {floor:0.0}");
                if(!marks.Any(m=>m.Contains(lead.transform.position)))failures.Add($"its {move} doesn't mark where the lead stands at the motor");
                float bar=lead.Health.integrity;yield return Until(()=>!boss.Busy||boss.Health.Broken,6,lead);
                if(lead.Health.integrity>=bar)failures.Add($"its {move}, marked on the deck, didn't strike a hero on foot standing in it");
            }
            // The motor's blows are the bar; with a tender at it, a blow takes a quarter.
            var motor=Cage.Motor;float top=boss.Health.integrity;
            motor.Receive(new DamagePacket{source=lead.Health,amount=1000,type=DamageType.Kinetic});yield return null;
            if(boss.Health.integrity>=top-1)failures.Add("a blow on the motor didn't bring the bar down");
            float clean=motor.maximum-motor.integrity;
            Cage.Kept.Tend(boss.GetComponent<EnemyBrain>(),.1f);float was=motor.integrity;motor.Receive(new DamagePacket{source=lead.Health,amount=1000,type=DamageType.Kinetic});
            float held=was-motor.integrity;if(held>clean*.3f)failures.Add($"with a tender at the motor a blow took {held:0} of {clean:0}: a tender holds it");
            // Struck through, the stage stops: over its line, kept, the shift, launch. (Fighting, so the line it passes is a shift.)
            yield return Until(()=>!boss.Busy,8,lead);boss.GetComponent<EnemyBrain>().Passive=false;Fresh(lead);
            for(int i=0;i<200&&motor.Alive&&boss.TollStagesStopped==0;i++)motor.Receive(new DamagePacket{source=lead.Health,amount=2000,type=DamageType.Kinetic});
            yield return Frames(2);
            if(boss.TollStagesStopped!=1)failures.Add("struck through, the motor's stage didn't stop");
            if(boss.Health.integrity>boss.Health.maximum*2/3f)failures.Add("the stage stopped and the bar didn't pass its first line");
            if(!OnDisk(BossController.TollTune.StageFlag(1)))failures.Add("the stage stopped isn't on the disk at once (D285)");
            if(!boss.TollFinal||boss.StatusText!="THE ARM IS DRAWING IN  ·  LAUNCH")failures.Add($"with the stage stopped the bar says \"{boss.StatusText}\", not to launch");
            yield return Until(()=>tollLog.Any(l=>l.StartsWith("BOSS_SHIFT Tollkeeper")),8,lead);
            if(!TollSaid("BOSS_SHIFT Tollkeeper 2"))failures.Add("the stage stopped and no shift pulled the cage in");
            Assert.IsEmpty(failures,string.Join("\n",failures));
            boss.GetComponent<EnemyBrain>().Passive=true;
        }
        /// <summary>The arm's clock run out with the party still on it: what stands on the deck is scraped (the band marked it all along), never to the last of a hero's bar, the party is put
        /// out in flight beside the pad, the stage's progress is lost (the motor whole again) and the arm draws in, to run out again for the dock still owed.</summary>
        [UnityTest] public IEnumerator TheArmsClockRunOutPutsThePartyOutWithAScrapeAndTheStageIsLost()
        {
            yield return TollYard();
            GameServices.Current.Flags.SetBool(BossController.TollTune.BraceFlag(0),true);GameServices.Current.Flags.SetBool("end.brace1",true);
            var boss=Toll();yield return Frames(3);Fresh(Lead);Fresh(Partner);
            yield return DockOnTheArm(boss,0);boss.GetComponent<EnemyBrain>().Passive=true;
            var lead=Lead;Fresh(lead,400);Fresh(Partner);lead.Health.integrity=40;
            var motor=Cage.Motor;motor.Receive(new DamagePacket{source=lead.Health,amount=3000,type=DamageType.Kinetic});float hurt=motor.integrity;
            var failures=new List<string>();
            // What the drawing-in lands on the deck, as it lands (the party's bars are made again in flight after the change).
            float scraped=0,left=-1;System.Action<Health,DamagePacket,float> witness=(body,packet,amount)=>{if(packet.tag==Health.AnyForm&&amount>0){scraped+=amount;left=body.integrity;}};lead.Health.Damaged+=witness;
            // Built wrong on purpose: the party launches first, and the clock never runs out on it.
            if(TollBroken)yield return Press(Berth.Find("Arm").launch);
            for(float end=Time.time+BossController.TollTune.ArmClock[0]+4;!TollSaid("TOLL_PUT_OUT")&&Time.time<end;){foreach(var h in PartyController.Current.members)if(h.Health.Alive)h.motor.Move(Vector2.zero,false,false);lead.Health.InvulnerableUntil=0;yield return null;}
            if(!TollSaid("TOLL_PUT_OUT lost"))failures.Add("the arm's clock ran out and the party wasn't put out");
            for(float end=Time.unscaledTime+3;Berth.Current!=null&&Time.unscaledTime<end;)yield return null;
            yield return Frames(3);lead=PartyController.Current.members.First(m=>m.character==lead.character);
            lead.Health.Damaged-=witness;TestContext.WriteLine($"TOLL_SCRAPE {scraped:0} left={left:0}");
            if(scraped<=0)failures.Add("nothing scraped a hero still on the deck as the arm drew in");
            else if(left<1)failures.Add("the scrape took the last of a hero's bar");
            if(!lead.flight)failures.Add("put out, the party isn't in flight");
            if(motor.integrity<=hurt)failures.Add("put out, the stage's progress wasn't lost: the motor isn't whole again");
            yield return Until(()=>Cage.ArmOut<=0,6,PartyController.Current.members);
            if(Cage.ArmOut>0)failures.Add("put out, the arm never drew in");
            boss.GetComponent<EnemyBrain>().Passive=false;foreach(var h in PartyController.Current.members)Fresh(h);
            yield return Until(()=>boss.TollArmOut,10,PartyController.Current.members);
            if(!boss.TollArmOut)failures.Add("with the dock still owed, the arm never ran out again");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ the cage and the guns, read from flight
        /// <summary>From the first stage stopped the cage contracts on its clock: its lamps light in turn toward it, and each outrigger's sweep is a marked fan from its hinge, filling
        /// before the bar moves; what the bar passes inside its fan is struck and shoved clear. A broken brace shortens its outrigger's fan to short of it: a ship beyond that, in the angle
        /// the fan would have had, is left alone (the gap), and one in the other fan is struck.</summary>
        [UnityTest] public IEnumerator TheCageContractsOnItsClockMarkedFromItsHingesAndABrokenBraceShortensItsSweep()
        {
            yield return TollYard();
            var flags=GameServices.Current.Flags;flags.SetBool(BossController.TollTune.StageFlag(1),true);
            // Built wrong on purpose: no brace broken on the west outrigger, so its sweep is whole.
            if(!TollBroken)flags.SetBool(BossController.TollTune.BraceFlag(1),true);
            var boss=Toll();yield return Frames(3);
            var gap=Lead;var east=Partner;Fresh(gap);Fresh(east);
            var west=Cage.riggers[0];var eastRigger=Cage.riggers[1];
            // In the gap: 30 m from the west hinge, 60 degrees off the gate line, past where its broken brace (26 m) leaves the bar. In the east fan: 18 m from its hinge, the same angle.
            Put(gap,west.hinge+Cage.Along(west,60)*30);Put(east,eastRigger.hinge+Cage.Along(eastRigger,60)*18);
            var failures=new List<string>();
            yield return Until(()=>Cage.LitLamps>=2,BossController.TollTune.CageEvery[1],gap,east);
            if(Cage.LitLamps<2)failures.Add("the cage's clock lights nothing as it runs");
            yield return Until(()=>boss.CurrentMove=="Contract",BossController.TollTune.CageEvery[1]+4,gap,east);
            if(boss.CurrentMove!="Contract")failures.Add("the cage never contracted on its clock");
            yield return Frames(2);
            var fans=Marks(boss).Where(t=>t.Shape==Telegraph.Kind.Cone).ToArray();
            if(fans.Length!=2)failures.Add($"the cage's contraction marked {fans.Length} fans, not one from each hinge");
            foreach(var f in fans)if(Flat(f.Origin,west.hinge)>.5f&&Flat(f.Origin,eastRigger.hinge)>.5f)failures.Add($"a fan starts at {f.Origin}, not at a hinge");
            float a=gap.Health.integrity,b=east.Health.integrity;
            yield return Until(()=>boss.CurrentMove!="Contract",8,gap,east);
            TestContext.WriteLine($"TOLL_SWEEP gap={a-gap.Health.integrity:0} east={b-east.Health.integrity:0} reach={Cage.Reach(west):0}");
            if(gap.Health.integrity<a)failures.Add("a ship in the gap a broken brace leaves was struck by the cage");
            if(east.Health.integrity>=b)failures.Add("a ship inside the other fan wasn't struck as the bar swept it");
            if(!TollSaid("TOLL_CAGE_STRUCK"))failures.Add("no TOLL_CAGE_STRUCK in the log");
            Assert.IsEmpty(failures,string.Join("\n",failures));
            boss.GetComponent<EnemyBrain>().Passive=true;
        }
        /// <summary>The guns fire marked lanes down the lanes the chains make: the lead's on him and inside his lane (between the dividing chain and the outer one), every lane from the gate
        /// south its whole run; one lane before a stage is stopped, three in turn with two stopped, each landing after the one before.</summary>
        [UnityTest] public IEnumerator TheGunsFireMarkedLanesBetweenTheChainsMoreAndInTurnAsStagesStop()
        {
            var failures=new List<string>();
            foreach(int stages in new[]{0,2})
            {
                yield return TollYard();
                if(stages>=1)GameServices.Current.Flags.SetBool(BossController.TollTune.StageFlag(1),true);if(stages>=2)GameServices.Current.Flags.SetBool(BossController.TollTune.StageFlag(2),true);
                var boss=Toll(true);yield return Frames(3);var lead=Lead;Fresh(lead);Fresh(Partner);Put(Partner,TollAt(0,0,-70));
                // In the east lane, near its dividing chain.
                Put(lead,TollAt(21,0,-30));yield return Frames(2);
                var before=new HashSet<Telegraph>(Telegraph.Live);Assert.IsTrue(boss.Perform("Guns"),"fixture: the guns");yield return null;
                var lanes=Telegraph.Live.Where(t=>t!=null&&t.owner==boss&&!before.Contains(t)&&t.Shape==Telegraph.Kind.Line).OrderBy(t=>t.Duration).ToArray();
                int wanted=TollBroken?3-stages:BossController.TollTune.GunLanes[stages];
                if(lanes.Length!=wanted)failures.Add($"with {stages} stages stopped the guns marked {lanes.Length} lanes, not {wanted}");
                if(lanes.Length>0&&!lanes[0].Contains(lead.transform.position))failures.Add("the first lane isn't on the lead");
                foreach(var lane in lanes)
                {
                    float off=Cage.OffsetOf(lane.Origin),half=lane.Width*.5f;var (from,to)=Cage.LaneAt(off);
                    if(off-half<from-.01f||off+half>to+.01f)failures.Add($"a lane at {off:0.0} spills over its chains ({from}..{to})");
                    if(Vector3.Angle(lane.Forward,Cage.South)>1)failures.Add("a lane doesn't run down the lanes from the gate");
                    if(lane.Length<70)failures.Add($"a lane runs {lane.Length:0} m, not the lane's length");
                }
                for(int i=1;i<lanes.Length;i++)if(lanes[i].Duration<=lanes[i-1].Duration+.2f)failures.Add("two lanes land together: in turn, each after the one before");
                yield return Until(()=>!boss.Busy,8,lead);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ what a Continue keeps (D285), and a conversation (D275)
        /// <summary>Two braces broken and a stage stopped, kept on the disk; quit to the title and Continue: in the yard stood again the cage loads with those two braces broken, the fight
        /// wakes at the line of the two stages left, in its second phase, with no shift played, and the dock the second brace owes is out.</summary>
        [UnityTest] public IEnumerator BracesBrokenAndAStageStoppedStayThroughAContinue()
        {
            yield return TollYard();
            var flags=GameServices.Current.Flags;
            foreach(var key in new[]{BossController.TollTune.BraceFlag(1),BossController.TollTune.BraceFlag(4),"end.brace1","end.brace2",BossController.TollTune.StageFlag(1)})flags.SetBool(key,true);
            State.spawn="Gate";
            // Built wrong on purpose: nothing kept, as a fight with no parts of its own keeps nothing.
            if(!TollBroken)SaveSystem.KeepCheckpoint();else foreach(var key in new[]{BossController.TollTune.BraceFlag(1),BossController.TollTune.BraceFlag(4),BossController.TollTune.StageFlag(1)})flags.SetBool(key,false);
            if(TollBroken)GameServices.Current.Saves.Save("autosave",State);
            SceneFlow.Current.LoadZone("Title");float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            yield return Frames(2);var title=Object.FindFirstObjectByType<TitleScreen>();Assert.IsNotNull(title,"fixture: the title never came up");
            tollStand=true;SceneManager.sceneLoaded+=TollBuilt;title.StartGame(true);yield return null;
            deadline=Time.unscaledTime+15;while((SceneFlow.Current.Loading||SceneFlow.Current.Zone!="Arena_Flight")&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual("Arena_Flight",SceneFlow.Current.Zone,"fixture: the Continue didn't come back to the yard");yield return Frames(3);Hold();
            tollLog.Clear();var boss=Toll();yield return Frames(3);
            var failures=new List<string>();
            var broken=Cage.braces.Where(b=>b.broken).Select(b=>b.index).OrderBy(i=>i).ToArray();
            if(!broken.SequenceEqual(new[]{1,4}))failures.Add($"the cage loads with braces {string.Join(",",broken.Select(i=>i+1))} broken, not 2 and 5: what was broken stays broken");
            if(boss.TollStagesStopped!=1)failures.Add($"it wakes with {boss.TollStagesStopped} stages stopped, not 1");
            if(boss.Health.integrity>boss.Health.maximum*2/3f)failures.Add("it wakes over the line its stopped stage leaves it");
            if(boss.Phase!=2)failures.Add($"it wakes in phase {boss.Phase}, not 2");
            yield return new WaitForSecondsRealtime(1.5f);
            if(TollSaid("BOSS_SHIFT"))failures.Add("a shift played over the load");
            if(!boss.TollOwed)failures.Add("two braces and one stage: the second dock isn't owed");
            yield return ArmOut(boss,6);if(!boss.TollArmOut)failures.Add("the dock owed, the arm isn't out");
            Assert.IsEmpty(failures,string.Join("\n",failures));
            boss.GetComponent<EnemyBrain>().Passive=true;
        }
        /// <summary>A conversation holding the controls holds the cage (its bars, its fans' fill, its clock), the arm running out, and nothing lands on anyone; the moment the talk ends all of it
        /// goes on from where it stood.</summary>
        [UnityTest] public IEnumerator AConversationHoldsTheCageTheArmAndTheirClocksWhereTheyStand()
        {
            yield return TollYard();
            var flags=GameServices.Current.Flags;flags.SetBool(BossController.TollTune.StageFlag(1),true);flags.SetBool(BossController.TollTune.BraceFlag(0),true);flags.SetBool(BossController.TollTune.BraceFlag(2),true);flags.SetBool("end.brace1",true);flags.SetBool("end.brace2",true);
            var boss=Toll();yield return Frames(3);var lead=Lead;Fresh(lead);Fresh(Partner);
            var rigger=Cage.riggers[1];Put(lead,rigger.hinge+Cage.Along(rigger,60)*14);Put(Partner,TollAt(0,0,-70));
            // The arm drawn in, to run out for the dock the second brace owes: caught by the talk on its way.
            Cage.SetArm(0);
            Assert.IsTrue(boss.Perform("Contract"),"fixture: the cage contracts");yield return Frames(3);
            var fan=Marks(boss).First(t=>t.Shape==Telegraph.Kind.Cone);var input=GameServices.Current.Input;
            float elapsed=fan.Elapsed,angle=rigger.angle,arm=Cage.ArmOut,bar=lead.Health.integrity;
            Assert.Less(arm,1,"fixture: the arm is still running out");
            // Built wrong on purpose: the talk never takes the controls.
            for(float until=Time.time+2.5f;Time.time<until;){input.Blocked=!TollBroken;yield return null;}
            var failures=new List<string>();
            if(fan.Elapsed-elapsed>.05f)failures.Add($"the cage's fan filled {fan.Elapsed-elapsed:0.00} s through a conversation");
            if(Mathf.Abs(rigger.angle-angle)>.5f)failures.Add($"the outrigger moved {Mathf.Abs(rigger.angle-angle):0.0} degrees through a conversation");
            if(Mathf.Abs(Cage.ArmOut-arm)>.01f)failures.Add("the arm ran on through a conversation");
            if(lead.Health.integrity<bar)failures.Add("something of the cage's landed through a conversation");
            input.Blocked=false;
            yield return Until(()=>fan.Released,4,lead);if(!fan.Released)failures.Add("the cage's fan never landed once the talk ended");
            yield return Until(()=>Cage.ArmOut>arm+.05f,4,lead);if(Cage.ArmOut<=arm)failures.Add("the arm never ran on once the talk ended");
            Assert.IsEmpty(failures,string.Join("\n",failures));
            boss.GetComponent<EnemyBrain>().Passive=true;
        }

        // ------------------------------------------------------------------ the gate opens
        /// <summary>The third stage stopped is the bar's end: it is counted down (and shut down, never called destroyed), the party is put off the arm into flight, the cage swings out and
        /// hangs in its fall's time, the arm draws in, the fight's music goes out, and nothing it made is solid (D289: no collider of the cage's or the motor's stops a ship).</summary>
        [UnityTest] public IEnumerator TheThirdStageStoppedOpensTheGateAndTheCageHangs()
        {
            yield return TollYard();
            var flags=GameServices.Current.Flags;foreach(int i in new[]{0,1,2})flags.SetBool(BossController.TollTune.BraceFlag(i),true);
            foreach(var key in new[]{"end.brace1","end.brace2","end.brace3",BossController.TollTune.StageFlag(1),BossController.TollTune.StageFlag(2)})flags.SetBool(key,true);
            var boss=Toll();yield return Frames(3);Fresh(Lead);Fresh(Partner);
            yield return DockOnTheArm(boss,0);var lead=Lead;Fresh(lead);Fresh(Partner);
            var failures=new List<string>();
            foreach(var shape in Cage.GetComponentsInChildren<Collider>(true))if(!shape.isTrigger)failures.Add($"the cage's {shape.name} is solid: nothing the fight makes may wall a ship in (D289)");
            // Built wrong on purpose: struck from a ship, where the motor isn't the party's to strike.
            if(TollBroken)yield return Press(Berth.Find("Arm").launch);
            var motor=Cage.Motor;for(int i=0;i<200&&motor.Alive&&!Has("bossdown.Tollkeeper");i++)motor.Receive(new DamagePacket{source=lead.Health,amount=2000,type=DamageType.Kinetic});
            yield return Frames(2);
            if(!Has("bossdown.Tollkeeper"))failures.Add("the third stage stopped, it isn't counted down");
            if(!Has("end.motorStopped"))failures.Add("the third stage stopped, end.motorStopped isn't set");
            if(!TollSaid("TOLL_GATE_OPENS"))failures.Add("no TOLL_GATE_OPENS in the log");
            for(float end=Time.unscaledTime+4;Berth.Current!=null&&Time.unscaledTime<end;)yield return null;
            yield return Frames(2);
            if(!PartyController.Current.Active.flight)failures.Add("the motor dead, the party wasn't put off the arm");
            yield return Until(()=>Cage.Hanging,BossController.TollTune.HangSeconds+2);
            if(!Cage.Hanging)failures.Add("the cage didn't hang open in its fall's time");
            foreach(var r in Cage.riggers)if(r.pivot!=null&&r.pivot.position.y>TM.y)failures.Add("an outrigger hangs over the plane: it is to hang open under it");
            yield return Until(()=>Cage.ArmOut<=0,BossController.TollTune.ArmSeconds+2);
            if(Cage.ArmOut>0)failures.Add("the motor dead, the arm never drew in");
            if(MusicDirector.Current!=null&&MusicDirector.Current.Track=="Glitch Boss")failures.Add("its fight's music plays on after the gate opens");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ its length, really docking
        /// <summary>Its length in its room, three times over and really played: break a brace with the ships' own weapons (rolling out of what is marked on him), fly to the arm as it runs
        /// out, dock by the pad (stream A's berth, its fades and all), strike the motor on foot with the heroes' own blows (the partner at the tenders), roll out of what reaches the deck, and
        /// launch with the stage stopped; the cage's contractions are flown through as they come (and back to the brace after a shove). The hand strikes as fast as its weapons let it (no
        /// skills), the slowest a party that plays the fight right goes: its fight as played is its sheet's bounds. Until November 15 the striking was counted again at the sheet's share, a
        /// model of the replay driver; played through the driver's own pad at its door (MooringBossReplayTests) the fight ran the other way, the pad's skills striking about twice as fast as
        /// this hand, and the sizes that model chose fell in 101 s.</summary>
        [UnityTest,Timeout(600000)] public IEnumerator ItsThreeCyclesOfFlyDockWorkAndLaunchLastAsItsSheetSays()
        {
            yield return TollYard();
            var boss=Toll();yield return Frames(3);
            var lead=Lead;var partner=Partner;Fresh(lead);Fresh(partner);Put(lead,TollAt(0,0,-60));Put(partner,TollAt(4.2f,0,-60));
            float started=Time.time,striking=0;var legs=new List<string>();int rolls=0,puts=0;
            bool Roll(CombatActor hero,Vector3 away)
            {
                if(!(boss.Telegraphing&&boss.TelegraphRemaining<.15f)||hero.State==ActorState.Dodge||!hero.CanAct)return false;
                if(!Telegraph.Live.Any(t=>t!=null&&t.owner==boss&&!t.Released&&t!=boss.TollBand&&t.Contains(hero.transform.position)))return false;
                hero.Dodge(away);rolls++;return true;
            }
            // A plain hand: the weapon, as soon as it can be used again, and no skills (flat out with every skill is the shared length test's measure, not a player's).
            void Act(CombatActor hero,Health target)
            {
                hero.target=target;if(!hero.CanAct)return;
                // Turned to it as a player steers at what he means to hit: a shot goes where the hull points, or at its target within 30 degrees of that.
                var at=Vector3.ProjectOnPlane(target.transform.position-hero.transform.position,Vector3.up);
                if(at.sqrMagnitude>.01f){hero.transform.rotation=Quaternion.LookRotation(at);if(hero.motor is FlightMotor ship)ship.KeepFacing(at);else if(hero.motor is GroundMotor ground)ground.KeepFacing(at);}
                hero.Attack();
            }
            IEnumerator Fly(System.Func<Vector3> to,float within)
            {
                for(float end=Time.time+30;Time.time<end;)
                {
                    var at=Lead.transform.position;var goal=to();goal.y=TM.y;var way=goal-at;way.y=0;if(way.magnitude<within)break;
                    foreach(var hero in PartyController.Current.members)if(hero.Health.Alive)hero.motor.Move(Vector2.zero,false,false);
                    if(!Roll(Lead,Vector3.Cross(Vector3.up,way).normalized)&&Lead.State!=ActorState.Dodge)Put(Lead,at+way.normalized*Mathf.Min(way.magnitude,12*Time.deltaTime));
                    if(Partner.Health.Alive&&Flat(Partner.transform.position,Lead.transform.position)>8)Put(Partner,Lead.transform.position+Vector3.right*4.2f);
                    yield return null;
                }
            }
            for(int stage=1;stage<=3;stage++)
            {
                float t0=Time.time;
                // A brace: the nearest whole one to where the lead is, struck from 8 m off by both ships.
                var brace=Cage.braces.Where(b=>!b.broken).OrderBy(b=>Flat(b.node.position,Lead.transform.position)).First();int brokenBefore=Cage.Broken;
                yield return Fly(()=>brace.node.position+(Lead.transform.position-brace.node.position).normalized*8,1.5f);
                // Whichever brace goes first is this cycle's (a shot past the one aimed at can take the next one along the bar), as for a player.
                for(float end=Time.time+60;Cage.Broken<=brokenBefore&&Time.time<end;)
                {
                    // Shoved off by the cage's contraction, or left behind as its bar swings in and back, a player flies back to the brace: the hand stayed where it was and shot at
                    // nothing (lead 111 m off for the rest of the minute, once a brace outlasted a contraction: MooringBossReplayTests' sizes, November 15). Only the seconds in reach strike.
                    bool near=true;
                    foreach(var hero in PartyController.Current.members)
                    {
                        if(!hero.Health.Alive)continue;hero.motor.Move(Vector2.zero,false,false);
                        var off=brace.node.position-hero.transform.position;off.y=0;
                        if(off.magnitude>12&&hero.State!=ActorState.Dodge&&hero.CanAct){near=false;var to=brace.node.position-off.normalized*8;to.y=TM.y;Put(hero,hero.transform.position+(to-hero.transform.position).normalized*Mathf.Min(Flat(to,hero.transform.position),12*Time.deltaTime));continue;}
                        if(!Roll(hero,Vector3.left)&&brace.health.Targetable)Act(hero,brace.health);
                    }
                    if(!Lead.Health.Alive||!Partner.Health.Alive){Fresh(Lead);Fresh(Partner);}
                    if(near)striking+=Time.deltaTime;
                    yield return null;
                }
                Assert.Greater(Cage.Broken,brokenBefore,$"stage {stage}: no brace broke in 60 s (left {brace.health.integrity:0} of {brace.health.maximum:0}, open {brace.health.Targetable}, controls held {GameServices.Current.Input.Blocked}, lead {Flat(Lead.transform.position,brace.node.position):0.0} m off)");
                float braced=Time.time-t0;
                yield return ArmOut(boss,10);var berth=Berth.Find("Arm");
                yield return Fly(()=>berth.pad.transform.position,1);
                for(float end=Time.time+15;!berth.pad.Available&&Time.time<end;)yield return null;
                // Built wrong on purpose: the party never docks, and works at the motor from its ships.
                if(!TollBroken)yield return Press(berth.pad);
                lead=Lead;partner=Partner;float docked=Time.time;
                for(float end=Time.time+(TollBroken?20:90);boss.TollStagesStopped<stage&&!Has("bossdown.Tollkeeper")&&Time.time<end;)
                {
                    lead=Lead;partner=Partner;
                    if(Berth.Current==null&&TollBroken){foreach(var hero in PartyController.Current.members)if(hero.Health.Alive){hero.motor.Move(Vector2.zero,false,false);Act(hero,Cage.Motor);}yield return null;continue;}
                    if(Berth.Current==null)
                    {
                        // Put out with the stage still running: fly back to the pad as the arm runs out again, and dock.
                        puts++;yield return ArmOut(boss,12);yield return Fly(()=>berth.pad.transform.position,1);for(float wait=Time.time+10;!berth.pad.Available&&Time.time<wait;)yield return null;yield return Press(berth.pad);continue;
                    }
                    foreach(var hero in PartyController.Current.members)if(hero.Health.Alive)hero.motor.Move(Vector2.zero,false,false);
                    if(!Roll(lead,lead.transform.position-Cage.seat.position)&&lead.State!=ActorState.Dodge)
                    {
                        var stand=Cage.seat.position+Cage.seat.forward*1.5f;if(Flat(lead.transform.position,stand)>.6f)Put(lead,lead.transform.position+(stand-lead.transform.position).normalized*Mathf.Min(Flat(lead.transform.position,stand),5*Time.deltaTime));
                        else{lead.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(Cage.seat.position-lead.transform.position,Vector3.up));if(Cage.MotorOpen)Act(lead,Cage.Motor);}
                    }
                    // The partner covers: at a tender a beat after it steps out, with her own blows; with none, at the motor beside him.
                    var tender=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).FirstOrDefault(e=>e.definition.id=="StillwaterTender"&&e.Health.Alive);
                    var aim=tender!=null&&Time.time-docked>1.5f?tender.Health:Cage.MotorOpen?Cage.Motor:null;
                    if(aim!=null&&partner.CanAct&&!Roll(partner,partner.transform.position-Cage.seat.position)&&partner.State!=ActorState.Dodge)
                    {
                        if(Flat(partner.transform.position,aim.transform.position)>1.8f)Put(partner,aim.transform.position+Vector3.ProjectOnPlane(partner.transform.position-aim.transform.position,Vector3.up).normalized*1.5f);
                        partner.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(aim.transform.position-partner.transform.position,Vector3.up));Act(partner,aim);
                    }
                    if(!lead.Health.Alive||!partner.Health.Alive){Fresh(lead);Fresh(partner);}
                    if(Cage.MotorOpen)striking+=Time.deltaTime;
                    yield return null;
                }
                Assert.IsTrue(boss.TollStagesStopped>=stage||Has("bossdown.Tollkeeper"),$"stage {stage} never stopped on the arm (the motor {Cage.Motor.integrity:0} of {Cage.Motor.maximum:0}, open {Cage.MotorOpen}, docked {Berth.Current!=null})");
                float worked=Time.time-docked;
                if(stage<3&&Berth.Current!=null){for(float end=Time.time+3;!berth.launch.Available&&Time.time<end;)yield return null;yield return Press(berth.launch);}
                legs.Add($"stage {stage}: the brace {braced:0.0}s, on the arm {worked:0.0}s");
            }
            float fight=Time.time-started;
            string line=$"TOLL_LENGTH fight={fight:0}s striking={striking:0}s rolls={rolls} putOut={puts} :: {string.Join("; ",legs)}";
            TestContext.WriteLine(line);Debug.Log(line);
            Assert.IsTrue(Has("bossdown.Tollkeeper"),"the third stage stopped, it isn't counted down");
            Assert.GreaterOrEqual(fight,TollMin,line+": the hand's own fight is shorter than its sheet's bounds");
            Assert.LessOrEqual(fight,TollMax,line+": the hand's own fight is longer than its sheet's ceiling");
        }
        #endregion

        // =================================================================== Meret Venn (stream H): MeretShip, MeretGantry, MeretWell
        // Her three fights in the yards, as docs/maps/Continuance.md and docs/maps/StillwaterChamber.md ("Boss arena contract") ask for their rooms: the ship in the flight yard (her clamps her
        // own, where the room's seats are), the gantry and the well in the ground yard on a plain floor. The party is the chapter's arrival (DevLoadout chapter10, Sync 27, Choirwrought).
        // Each case builds its fixture wrong first under MERET_BROKEN=1 (this region's own runs only), so a broken run shows every check failing the way it should.
        static bool MeretBroken=>System.Environment.GetEnvironmentVariable("MERET_BROKEN")=="1";
        static readonly List<string> meretLog=new();static bool meretListening;
        static void MeretHeard(string text,string stack,LogType type){if(text.StartsWith("MERET_")||text.StartsWith("COUPLING_")||text.StartsWith("BOSS_"))meretLog.Add(text);}
        static void MeretListen(){if(!meretListening){meretListening=true;Application.logMessageReceived+=MeretHeard;}meretLog.Clear();}
        static bool MeretSaid(string start)=>meretLog.Any(l=>l.StartsWith(start));
        /// <summary>Where her craft is fought from in the flight yard, facing south as in the Continuance; and where her frames stand on the ground yard's plain floor.</summary>
        static readonly Vector3 ShipAt=new(-300,1,300),FrameAt=new(200,.1f,0);
        /// <summary>Each phase's own bounds (wiring 6.5: about 80 to 150 s each, the three together about five minutes).</summary>
        const float MeretMin=80,MeretMax=150;

        [ContentRegistry.Expectations] static void MeretShipSheet(ContentRegistry.Book book)
        {
            // Met at Sync 28.8 (D296's ledger) in the Continuance's flight berth once she has hailed them (end.meretHailed). Stands from her facing north as the shared tests stand her.
            // Her three shares are what the replay driver's pad got of the same pair's flat out, playing her doors with routes 66 and 67's own steps (MeretReplayTests, November 15).
            book.Boss(new ContentRegistry.BossSheet{id="MeretShip",zone="Continuance",door="MeretShip",doorFlags=new[]{"end.meretHailed"},flight=true,sync=29,driverShare=.15f,
                minSeconds=MeretMin,maxSeconds=MeretMax,
                specials=new[]{("Broadside",new Vector3(0,0,22)),("Lance",new Vector3(0,0,20)),("Tractor",new Vector3(0,0,20)),("Volley",new Vector3(0,0,22)),("Clamp",new Vector3(0,0,30))},
                // Two clamps at once: HerClampsRiseMarkedSetAHeroBesideThemAndStandAsCover, in this class (the shared table stands one hero in one marking).
                testedApart=new[]{"Clamps"},
                // Her lance in every phase: she fires through her shield, and it is down after.
                windows=new[]{("Lance",new Vector3(0,0,20)),("Lance",new Vector3(0,0,20)),("Lance",new Vector3(0,0,20))}});
        }
        [ContentRegistry.Expectations] static void MeretGantrySheet(ContentRegistry.Book book)
        {
            // Met at Sync 29.1 on the frame gantry, the dock's arrival its door's checkpoint (end.meretShip: MeretGantry heard).
            // .33 since D362 (November 22): today's scouts felled 104,000 in 71.0 s (nov22-recon-g s67, the 12:35 build) and 122,000 in 104.8 s (nov22-recon-h-67, the 14:52 build)
            // against 3,365 and 3,529 a second (d355-dome3, d355-lengths): .425 and .338, the lower. She is 122,000 (a quarter 145 of 150).
            book.Boss(new ContentRegistry.BossSheet{id="MeretGantry",zone="StillwaterChamber",door="MeretGantry",doorFlags=new[]{"end.meretShip"},sync=29,driverShare=.33f,
                minSeconds=MeretMin,maxSeconds=MeretMax,
                specials=new[]{("Slam",new Vector3(0,0,4)),("Weld",new Vector3(0,0,8)),("Coolant",new Vector3(0,0,8)),("Charge",new Vector3(0,0,8)),("Stomp",new Vector3(0,0,2.6f))},
                // The weld always ends overreached; from her first coupling open the frame's charge always ends overextended.
                windows=new[]{("Weld",new Vector3(0,0,8)),("Charge",new Vector3(0,0,8)),("Charge",new Vector3(0,0,8))}});
        }
        [ContentRegistry.Expectations] static void MeretWellSheet(ContentRegistry.Book book)
        {
            // Met at Sync 29.5 in the control well once the party has come in at the lift's foot (end.wellEntered).
            // .49 since D362 (November 22): today's scout of the 12:35 build felled 140,000 in 65.7 s (nov22-recon-g s67) against 4,311 and 4,387 a second (d355-dome3, d355-lengths),
            // which is .490. She is 190,000 since D362 (continued), the slow pair's 200 s (her 150 at four thirds) on her slowest reading (4,012) holding her there: about 89 s.
            book.Boss(new ContentRegistry.BossSheet{id="MeretWell",zone="StillwaterChamber",door="MeretWell",doorFlags=new[]{"end.wellEntered"},sync=29,driverShare=.49f,
                minSeconds=MeretMin,maxSeconds=MeretMax,
                specials=new[]{("Thrust",new Vector3(0,0,4)),("Cut",new Vector3(0,0,2.4f)),("Traps",new Vector3(0,0,6)),("Lunge",new Vector3(0,0,8)),("Guard",new Vector3(0,0,4))},
                // Her question and the quiet after it strike nobody: SheAsksHerQuestionAndListensInAQuietNothingIsSaidOver, in this class.
                testedApart=new[]{"Listens"},
                // The lunge always ends overextended (her spent counter is the opening a player earns: HerGuardTurnsABlowAnswersItWithAMarkedRiposteAndIsSpent).
                windows=new[]{("Lunge",new Vector3(0,0,8)),("Lunge",new Vector3(0,0,8)),("Lunge",new Vector3(0,0,8))}});
        }

        // ------------------------------------------------------------------ fixtures
        static BossController Meret(string id,Vector3 at,bool passive,float yaw)
        {
            var enemy=ActorFactory.Enemy(Def(id),at);enemy.transform.rotation=Quaternion.Euler(0,yaw,0);enemy.Passive=passive;return enemy.GetComponent<BossController>();
        }
        /// <summary>The ground yard's plain floor under her (the shared tests' own).</summary>
        static void MeretFloor(){var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Meret fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static IEnumerator Idle(BossController boss,float seconds=10){for(float end=Time.unscaledTime+seconds;(boss.Busy||boss.Health.Broken)&&Time.unscaledTime<end;)yield return null;}
        static Telegraph[] NewMarks(Object owner,HashSet<Telegraph> before)=>Telegraph.Live.Where(t=>t!=null&&t.owner==owner&&!before.Contains(t)).ToArray();
        static float HeadOf(Component boss){var rig=boss.GetComponentInChildren<Animator>();var head=rig!=null&&rig.isHuman?rig.GetBoneTransform(HumanBodyBones.Head):null;return head!=null?head.position.y-boss.transform.position.y:0;}

        // ------------------------------------------------------------------ her relays, as a player sees them
        /// <summary>Her shield's two relays are drawn as big as what a shot at them is judged against (chapter 10's look pass, November 21: in the whole-game capture each was a 1.25 m ball on a
        /// thin grey pole, under half its 2.8 m target, and no reviewer found it). Lit, each head is at least nine tenths of its target's width, its ring round it at the plane is drawn and wider
        /// than its head, and its beam runs to her dome; shot through and dark, its head and its ring are still drawn (a mast with a dark head, its ring amber), so the player sees what
        /// comes back. The measure is first shown to catch the old head.</summary>
        [UnityTest] public IEnumerator HerRelaysAreDrawnAsBigAsTheTargetAShotIsJudgedAgainstLitOrDark()
        {
            yield return Load("Arena_Flight");MeretListen();
            var failures=new List<string>();
            var ship=Meret("MeretShip",ShipAt,false,180);Put(Lead,ShipAt+Vector3.back*24);Fresh(Lead);Fresh(Partner);
            yield return Until(()=>ship.ShipShieldUp&&ship.ShipRelays.Count==2&&ship.ShipRelays.All(r=>r!=null&&r.Lit),4,Lead);
            Assert.AreEqual(2,ship.ShipRelays.Count,"fixture: her two relays");Assert.IsTrue(ship.ShipRelays.All(r=>r.Lit),"fixture: her relays lit as her shield came up");
            float target=BossController.ShipTune.RelayTarget*2;
            string Fault(MeretRelay r)
            {
                if(r.head==null||!r.head.enabled)return $"relay {r.index} has no head drawn";
                float drawn=Mathf.Min(r.head.bounds.size.x,r.head.bounds.size.z);
                if(drawn<target*.9f)return $"relay {r.index}'s head is drawn {drawn:0.00} m across against its {target:0.0} m target";
                if(r.ring==null||!r.ring.enabled||r.ring.positionCount<12)return $"relay {r.index} has no ring at the plane";
                var far=r.ring.transform.TransformPoint(r.ring.GetPosition(0));if(Vector3.Distance(new Vector3(far.x,0,far.z),new Vector3(r.transform.position.x,0,r.transform.position.z))<drawn*.5f+.3f)return $"relay {r.index}'s ring is inside its head";
                if(Mathf.Abs(far.y-r.transform.position.y)>.2f)return $"relay {r.index}'s ring doesn't lie at the plane";
                return null;
            }
            // The measure catches the head as it was drawn (1.25 across).
            var first=ship.ShipRelays[0];var scale=first.head.transform.localScale;first.head.transform.localScale=Vector3.one*1.25f;
            Assert.IsNotNull(Fault(first),"fixture: the measure lets the old 1.25 m head through");first.head.transform.localScale=scale;
            foreach(var r in ship.ShipRelays){var f=Fault(r);if(f!=null)failures.Add("lit: "+f);if(r.beam==null||!r.beam.enabled)failures.Add($"lit: relay {r.index} has no beam to her dome");}
            // Shot through: dark, still drawn, its ring amber.
            foreach(var r in ship.ShipRelays)r.health.Receive(new DamagePacket{source=Lead.Health,amount=r.health.maximum*10,type=r.health.weakness});
            yield return Frames(3);
            // Her dome down: the card counts what is down, in step with the bar's word (it counted the projector being struck: "3/3" with the third still up, s66/162).
            if(!ship.ShipShieldUp)
            {
                if(ship.FightGoal("zone")!=$"Put out a shield projector ({BossController.ShipTune.Projectors.Length-ship.ShipProjectorsLeft}/{BossController.ShipTune.Projectors.Length})")failures.Add($"with her shield down and {ship.ShipProjectorsLeft} projectors up the card says '{ship.FightGoal("zone")}'");
                if(!ship.StatusText.Contains("STRIKE A PROJECTOR"))failures.Add($"with her shield down the bar says '{ship.StatusText}'");
            }
            else failures.Add("fixture: her shield didn't drop with both relays shot through");
            foreach(var r in ship.ShipRelays)
            {
                if(r.Lit)failures.Add($"relay {r.index} is still lit, shot through");
                if(r.head==null||!r.head.enabled||Mathf.Min(r.head.bounds.size.x,r.head.bounds.size.z)<MeretRelay.HeadDark*.9f)failures.Add($"dark: relay {r.index}'s head isn't drawn");
                if(r.ring==null||!r.ring.enabled)failures.Add($"dark: relay {r.index}'s ring isn't drawn");
                else{var c=r.ring.sharedMaterial.GetColor("_BaseColor");if(!(c.r>c.b))failures.Add($"dark: relay {r.index}'s ring isn't amber ({c})");}
                if(r.beam!=null&&r.beam.enabled)failures.Add($"dark: relay {r.index} still beams to her dome");
            }
            ship.GetComponent<EnemyBrain>().Passive=true;Object.Destroy(ship.gameObject);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ her definitions, her words
        /// <summary>Her three EnemyDefs came with their Kits, ids exactly the manifest's, and every word of hers on screen is the manifest's (section 5, section 6): her names and epithets, the
        /// ship's state line and the objective string while her shield is up, her music, how each phase ends (the craft and the frame leave; she yields standing), the builder's integrities
        /// pinned to the assets, D296's XP, her bark pools (two, three and four in turn) and the two expressions her sheet gains for the chapter. Nothing calls her defeated or killed.</summary>
        [UnityTest] public IEnumerator HerThreeDefinitionsCameWithTheirKitsAndEveryWordOfHersIsTheManifests()
        {
            yield return Load("Arena_Flight");MeretListen();
            var failures=new List<string>();
            string source=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"_Project/Scripts/Editor/World/LastMooringBuilder.Meret.cs"));
            var expected=new[]{("MeretShip","DIRECTOR MERET VENN","Aboard the Continuance, with the last anchor",BossController.Ending.Leaves,1200),
                ("MeretGantry","DIRECTOR MERET VENN","Every machine you have beaten, built properly",BossController.Ending.Leaves,1300),
                ("MeretWell","MERET VENN","Sixty-one, in an emergency frame, with a cough",BossController.Ending.Yields,1400)};
            foreach(var (id,name,epithet,ending,xp) in expected)
            {
                var def=Def(id);
                if(!def.boss)failures.Add(id+" isn't a boss");
                if(!BossController.Registered(id))failures.Add(id+"'s definition exists without its Kit");
                if(def.displayName!=name)failures.Add($"{id} is called {def.displayName}, not {name}");
                if(def.bossMusicId!="Final Stand")failures.Add($"{id}'s fight plays {def.bossMusicId}, not Final Stand");
                if(BossController.EndingOf(id)!=ending)failures.Add($"{id} ends {BossController.EndingOf(id)}, not {ending}");
                if(def.xp!=xp)failures.Add($"{id} pays {def.xp} XP: the ledger (D296) has {xp}");
                if(def.capsuleHeight<BossController.CapsuleRadius(id)*2)failures.Add($"{id}'s capsule is shorter than twice its radius: Unity stretches it");
                if(!source.Contains($"e.id=\"{id}\"")||!System.Text.RegularExpressions.Regex.IsMatch(source,$"\"{id}\",e=>\\{{[^}}]*e\\.integrity={def.integrity:0};"))failures.Add($"{id}'s integrity {def.integrity:0} isn't the builder's literal: a rebuild would change it");
                var boss=Meret(id,ShipAt+Vector3.right*40*System.Array.IndexOf(expected.Select(e=>e.Item1).ToArray(),id),true,180);yield return Frames(3);
                // Built wrong on purpose: the gantry's epithet as the brief first had it.
                string want=MeretBroken&&id=="MeretGantry"?"Machinery you have beaten, run by someone who understands it":epithet;
                if(boss.Epithet!=want)failures.Add($"{id}'s epithet is '{boss.Epithet}', not the manifest's '{want}'");
                if(boss.DisplayName!=name)failures.Add($"{id}'s bar says {boss.DisplayName}");
                foreach(string line in new[]{boss.DisplayName,boss.Epithet,boss.ShiftText})foreach(string bad in new[]{"defeat","DEFEAT","kill","KILL","beaten down","BEATEN"})
                    if(line.Contains(bad))failures.Add($"{id}'s '{line}' says {bad}");
                boss.GetComponent<EnemyBrain>().Passive=true;Object.Destroy(boss.gameObject);yield return Frames(2);
            }
            if(Def("MeretWell").yieldsOnDefeat!=true)failures.Add("MeretWell doesn't yield: she is taken into custody, not killed");
            // Her names and epithets fit the title card as the HUD draws it (QaHudLookTests holds every boss to it, and stops at the first that doesn't).
            var card=Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="BossTitleCard");
            var cardLine=Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="BossTitleEpithet");
            if(card==null||cardLine==null)failures.Add("fixture: the HUD's title card");
            else foreach(var (id,name,epithet,_,__) in expected)
            {
                // At the size the card sets this name (GameHud.CardSizeFor: its own 58, or smaller for a long one, never under the least): her whole name is the manifest's.
                var hud=Object.FindFirstObjectByType<Lattice.UI.GameHud>();float fits=hud.CardSizeFor(name),was=card.fontSize,spaced=card.characterSpacing;
                card.fontSize=fits;card.characterSpacing=Lattice.UI.GameHud.CardNameSpacing;
                var wide=card.GetPreferredValues(name,card.rectTransform.rect.width,0);var tall=cardLine.GetPreferredValues(epithet,cardLine.rectTransform.rect.width,0);
                card.fontSize=was;card.characterSpacing=spaced;
                if(fits<Lattice.UI.GameHud.CardNameLeast)failures.Add($"{id}'s name is set at {fits:0}, under the least a title may be read at");
                TestContext.WriteLine($"MERET_CARD {id} name={wide.x:0}/{card.rectTransform.rect.width:0} epithet={tall.y:0}/{cardLine.rectTransform.rect.height:0}");
                if(wide.x>card.rectTransform.rect.width+1)failures.Add($"{id}'s name runs off its title card ({wide.x:0} of {card.rectTransform.rect.width:0})");
                if(tall.y>cardLine.rectTransform.rect.height+1)failures.Add($"{id}'s epithet doesn't fit one line of its card");
            }
            // Her shield up: the state line and the panel's line are the manifest's objective string.
            var ship=Meret("MeretShip",ShipAt,false,180);Put(Lead,ShipAt+Vector3.back*24);Fresh(Lead);Fresh(Partner);
            yield return Until(()=>ship.ShipShieldUp,4,Lead);
            if(!ship.ShipShieldUp)failures.Add("her shield never came up as her fight began");
            // With a relay of hers lit, what to shoot instead (MeretReplayTests: held fire seven tenths of the fight without them); with none lit, the manifest's words.
            if(ship.FightGoal("zone")!="Shoot a shield relay: her shield gives back what reaches it")failures.Add($"with her shield up and a relay lit the panel says '{ship.FightGoal("zone")}'");
            if(ship.StatusText!="HER SHIELD IS UP  ·  SHOOT A RELAY")failures.Add($"with her shield up and a relay lit the bar says '{ship.StatusText}'");
            foreach(var relay in ship.ShipRelays)if(relay!=null)relay.health.Receive(new DamagePacket{source=Lead.Health,amount=relay.health.maximum*10,type=relay.health.weakness});
            yield return Until(()=>ship.ShipShieldUp,8,Lead);yield return Frames(2);
            if(ship.ShipShieldUp&&ship.FightGoal("zone")!="Hold your fire: her shield gives it back")failures.Add($"with her shield up and both relays dark the panel says '{ship.FightGoal("zone")}', not the manifest's 'Hold your fire: her shield gives it back'");
            if(ship.ShipShieldUp&&ship.StatusText!="HER SHIELD IS UP  ·  HOLD YOUR FIRE")failures.Add($"with her shield up and both relays dark the bar says '{ship.StatusText}'");
            ship.GetComponent<EnemyBrain>().Passive=true;Object.Destroy(ship.gameObject);
            // Her lines: two, three and four in turn; her sheet has the two expressions it gains (manifest 5).
            // Her pools: the first of each as the manifest has it, and as many lines as it gives her (two, three and four in turn).
            var said=new List<string>();System.Action<string,string> heard=(who,line)=>{if(who=="Meret")said.Add(line);};BarkService.Spoken+=heard;
            try
            {
                foreach(var (key,first,count) in new[]{("ship","Shield's up. Shoot it now and you'll have it back.",2),("gantry","That's the press. You know its tell.",3),("duel","Don't mash. I shall only counter.",4)})
                {
                    said.Clear();for(int i=0;i<count+1;i++)BarkService.PlayAt("Meret",key,i,true);
                    if(said.Count==0||said[0]!=first)failures.Add($"Meret's '{key}' lines open '{(said.Count>0?said[0]:"nothing")}', not the manifest's '{first}'");
                    else if(said.Distinct().Count()!=count)failures.Add($"Meret has {said.Distinct().Count()} '{key}' lines, not {count}");
                }
            }
            finally{BarkService.Spoken-=heard;}
            var sheet=Resources.Load<Lattice.Data.PortraitSet>("Portraits/Meret_Natural");
            if(sheet==null)failures.Add("Meret has no portrait sheet");
            else foreach(var emotion in new[]{Lattice.Data.PortraitEmotion.Determined,Lattice.Data.PortraitEmotion.Shocked,Lattice.Data.PortraitEmotion.Neutral,Lattice.Data.PortraitEmotion.Tender})
                if(!sheet.entries.Any(e=>e.emotion==emotion&&e.sprite!=null))failures.Add($"Meret's sheet has no {emotion} face, which chapter 10 asks of her");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ the numbers she is tuned by
        /// <summary>The standing rule (CHAPTER_GUIDE 0.1): a boss's blow costs a hero 12 to 16% of the bar and more than anything on the road to her, measured through Health.Receive on all
        /// three heroes at Sync 29 and 30 (she is met at 28.8 to 29.5) in Choirwrought. Every blow of all three phases, her brains' own included; her phases add nothing.</summary>
        [UnityTest] public IEnumerator EveryBlowOfHersInAllThreePhasesTakesABossesShareOfAnyHerosBarAndMoreThanTheRoadToHer()
        {
            yield return Load("Arena_Flight");
            var well=Meret("MeretWell",ShipAt,true,180);yield return Frames(3);var source=well.Health;Assert.IsNotNull(source,"fixture: her body");
            var roadIds=new[]{"StillwaterWarden","StillwaterWeldArm","StillwaterRig","StillwaterInterceptor","ClampGun","StillwaterTender","StillwaterTowDrone"};
            var road=roadIds.Select(id=>GameCatalog.Find<EnemyDef>(id)).Where(d=>d!=null&&!d.boss).SelectMany(d=>d.attacks.Where(a=>a!=null).Select(a=>(id:d.id,damage:a.damage))).ToList();
            var moves=new List<(string what,float damage)>
            {
                ("ship broadside",BossController.ShipTune.BroadsideDamage),("ship lance",BossController.ShipTune.LanceDamage),("ship tractor",BossController.ShipTune.TractorDamage),
                ("ship clamp",BossController.ShipTune.ClampDamage),("ship's shield giving it back",BossController.ShipTune.ReturnDamage),
                ("gantry slam",BossController.GantryTune.SlamDamage),("gantry weld",BossController.GantryTune.WeldDamage),("gantry coolant",BossController.GantryTune.CoolantDamage),
                ("gantry charge",BossController.GantryTune.ChargeDamage),("gantry stomp",BossController.GantryTune.StompDamage),
                ("well thrust",BossController.WellTune.ThrustDamage),("well cut",BossController.WellTune.CutDamage),("well riposte",BossController.WellTune.RiposteDamage),
                ("well clamp",BossController.WellTune.TrapDamage),("well lunge",BossController.WellTune.LungeDamage),
            };
            foreach(var id in new[]{"MeretShip","MeretGantry","MeretWell"})foreach(var attack in Def(id).attacks)if(attack!=null&&attack.range>0)moves.Add((id+"'s own "+attack.name,attack.damage));
            // Built wrong on purpose: a lunge authored at 230 is over the band, and the check has to say so.
            if(MeretBroken)moves.Add(("a lunge authored at 230",230));
            var failures=new List<string>();
            foreach(var pair in new[]{new[]{"Taren","Idra"},new[]{"Sela","Idra"}})
            {
                yield return Deploy(pair);yield return Wear("FramesT5");
                foreach(int sync in new[]{29,30})
                {
                    yield return Level(sync);
                    foreach(var hero in PartyController.Current.members)
                    {
                        if(pair[0]=="Sela"&&hero.character=="Idra")continue;
                        float bar=hero.Health.maximum;
                        float Share(float damage){hero.Health.integrity=bar;hero.Health.InvulnerableUntil=0;return hero.Health.Receive(new DamagePacket{source=source,amount=damage,type=DamageType.Kinetic,weight=HitWeight.Heavy})/bar;}
                        float heaviest=0;string named="nothing";foreach(var (id,damage) in road){float s=Share(damage);if(s>heaviest){heaviest=s;named=id;}}
                        foreach(var (what,damage) in moves)
                        {
                            float share=Share(damage);string line=$"MERET_HIT {hero.character} sync={sync} plating={hero.Plating:0} {what} {damage:0} share={share:P1} road={named} {heaviest:P1}";
                            TestContext.WriteLine(line);
                            if(share<.12f||share>.16f)failures.Add(line+": a boss's blow is 12 to 16% of the bar");
                            if(share<=heaviest)failures.Add(line+": no more than the road to her");
                        }
                    }
                }
            }
            foreach(string zone in new[]{"Continuance","StillwaterChamber"}){var z=GameCatalog.Find<ZoneDef>(zone);if(z!=null&&!Mathf.Approximately(z.enemyThreat,1))failures.Add($"{zone} is at threat {z.enemyThreat}");}
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ phase one: her shield
        /// <summary>Her shield up, the player's shot fired into it burns out on it and comes straight back down a marked lane at him (her bar untouched, the lane laid at least .3 s
        /// before it lands, and it lands on him if he stays); the first time the screen says why. The partner's shot only burns out. After her lance her shield is down: the same shot lands on her, a projector is open, and
        /// nothing comes back.</summary>
        [UnityTest] public IEnumerator HerShieldGivesAShotBackDownAMarkedLaneAndOnlyAfterHerLanceDoesAShotLand()
        {
            yield return Load("Arena_Flight");MeretListen();
            var boss=Meret("MeretShip",ShipAt,false,180);var lead=Lead;var partner=Partner;Fresh(lead);Fresh(partner);
            // Out past her reach from her station (she chooses nothing at him), well inside a shot's, and off the line through her seats (a risen clamp is cover).
            Put(lead,ShipAt+Vector3.back*54+Vector3.right*20);Put(partner,ShipAt+Vector3.back*54+Vector3.left*12);
            string posted=null;System.Action<string,string> post=(h,b)=>{posted??=h;};Notices.Posted+=post;
            try
            {
                yield return Until(()=>boss.ShipShieldUp&&!boss.Busy,10,lead,partner);
                Assert.IsTrue(boss.ShipShieldUp,"fixture: her shield is up");boss.GetComponent<EnemyBrain>().Passive=true;yield return Frames(2);
                // Built wrong on purpose: her shield isn't raised (she is held passive), and nothing should come back.
                boss.GetComponent<EnemyBrain>().Passive=MeretBroken;yield return Frames(2);
                float bar=boss.Health.integrity,before=lead.Health.integrity;var earlier=new HashSet<Telegraph>(Telegraph.Live);float firedAt=Time.time;
                var aim=(boss.transform.position-lead.transform.position).normalized;
                for(int i=0;i<3;i++){Projectile.Fire(lead.transform.position+Vector3.up*.9f+aim*2,aim,new DamagePacket{source=lead.Health,amount=60,type=DamageType.Beam},24);yield return new WaitForSeconds(.08f);}
                yield return Until(()=>MeretSaid("MERET_RETURN"),3.5f,lead,partner);
                var failures=new List<string>();
                if(!MeretSaid("MERET_RETURN Taren")&&!MeretSaid("MERET_RETURN "+lead.character))failures.Add("three shots fired into her raised shield, and nothing came back at the shooter");
                if(boss.Health.integrity<bar)failures.Add("a shot into her raised shield took her bar down");
                var lane=NewMarks(boss,earlier).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Line);
                if(lane==null)failures.Add("what her shield gives back marks nothing");
                else
                {
                    if(!lane.Contains(lead.transform.position))failures.Add("her shield's answer isn't marked through the hero who fired");
                    yield return Until(()=>lane.Released,2,lead,partner);
                    if(lane.Duration<.3f)failures.Add($"her shield's answer is marked {lane.Duration:0.00} s: too short to read");
                    if(lead.Health.integrity>=before)failures.Add("her shield's answer didn't land on the hero who stood in its lane");
                }
                if(posted!="HER SHIELD GIVES IT BACK")failures.Add($"the first shot given back says nothing on screen (posted: {posted??"nothing"})");
                // The partner's shot only burns out: nobody pays for what the companion does.
                meretLog.Clear();var fromHer=(boss.transform.position-partner.transform.position).normalized;
                Projectile.Fire(partner.transform.position+Vector3.up*.9f+fromHer*2,fromHer,new DamagePacket{source=partner.Health,amount=60,type=DamageType.Beam},24);
                yield return Until(()=>MeretSaid("MERET_RETURN"),3.5f,lead,partner);
                if(MeretSaid("MERET_RETURN"))failures.Add("the partner's shot into her shield came back at her: the companion's fire is nobody's fault");
                if(boss.Health.integrity<bar)failures.Add("the partner's shot into her raised shield took her bar down");
                Assert.IsEmpty(failures,string.Join("\n",failures));
                // After her lance her shield is down: a shot lands, a projector is open, nothing comes back.
                boss.GetComponent<EnemyBrain>().Passive=false;yield return Idle(boss);
                for(float end=Time.unscaledTime+6;!boss.Perform("Lance")&&Time.unscaledTime<end;)yield return null;
                yield return Until(()=>MeretSaid("MERET_SHIELD_DROPS"),5,lead,partner);
                Assert.IsTrue(MeretSaid("MERET_SHIELD_DROPS"),"her lance never dropped her shield");yield return Frames(2);
                Assert.IsFalse(boss.ShipShieldUp,"her shield is still up after she fired through it");
                Assert.IsTrue(Enumerable.Range(0,3).Any(i=>boss.ShipProjectorOpen(i)),"no projector opened with her shield down");
                Fresh(lead);meretLog.Clear();bar=boss.Health.integrity;aim=(boss.transform.position-lead.transform.position).normalized;
                Projectile.Fire(lead.transform.position+Vector3.up*.9f+aim*2,aim,new DamagePacket{source=lead.Health,amount=60,type=DamageType.Beam},24);
                yield return Until(()=>boss.Health.integrity<bar,3f,lead,partner);
                Assert.Less(boss.Health.integrity,bar,"with her shield down a shot at her didn't land");
                Assert.IsFalse(MeretSaid("MERET_RETURN"),"with her shield down a shot still came back");
            }
            finally{Notices.Posted-=post;}
            boss.GetComponent<EnemyBrain>().Passive=true;
        }
        /// <summary>A projector carries a third of her bar and is struck only while her shield is down; the third the bar passes puts out the one struck most (the player's choice, D281), the
        /// first out is her shift (she changes the lanes) and promises two clamps at once, and the third ends her: she leaves for the gantry's dock by her way out.</summary>
        [UnityTest] public IEnumerator TheProjectorStruckMostGoesAtEachThirdAndTheThirdOutSendsHerToTheGantry()
        {
            yield return Load("Arena_Flight");MeretListen();
            var exit=new GameObject("Her way out",typeof(BossExit));exit.transform.position=ShipAt+new Vector3(28,0,6);exit.GetComponent<BossExit>().boss="MeretShip";
            var boss=Meret("MeretShip",ShipAt,false,180);yield return Frames(3);var lead=Lead;var partner=Partner;Fresh(lead);Fresh(partner);
            Put(lead,ShipAt+Vector3.back*16);Put(partner,ShipAt+Vector3.back*16+Vector3.right*40);
            var body=boss.Health;var failures=new List<string>();
            IEnumerator Window()
            {
                yield return Idle(boss,14);meretLog.RemoveAll(l=>l.StartsWith("MERET_SHIELD_DROPS"));
                for(float end=Time.unscaledTime+6;!boss.Perform("Lance")&&Time.unscaledTime<end;)yield return null;
                yield return Until(()=>MeretSaid("MERET_SHIELD_DROPS"),5,lead,partner);
                // The projectors open on her next frame.
                yield return Frames(2);
            }
            // The starboard projector, struck through its windows until the bar passes its first third (a window gives up a twelfth of her bar: ShipTune.WindowShare). Built wrong on purpose:
            // the hull struck instead.
            for(int window=0;window<9&&body.integrity>body.maximum*2/3f-1;window++)
            {
                yield return Window();
                for(float end=Time.time+2.5f;Time.time<end&&body.integrity>body.maximum*2/3f-1&&boss.ShipProjectorOpen(1);)
                {
                    var struck=MeretBroken?body:boss.ShipProjectorHealth(1);
                    struck.Receive(new DamagePacket{source=lead.Health,amount=body.maximum*.06f,type=DamageType.Beam,weight=HitWeight.Heavy});
                    lead.Health.integrity=lead.Health.maximum;yield return new WaitForSeconds(.1f);
                }
            }
            yield return Frames(3);
            if(body.integrity>body.maximum*2/3f)failures.Add($"fixture: her bar never passed its first third ({body.integrity/body.maximum:P0})");
            if(!MeretSaid("MERET_PROJECTOR_DOWN 1"))failures.Add("the bar passed its first third and the starboard projector, the one struck, didn't go: "+string.Join(" | ",meretLog.Where(l=>l.StartsWith("MERET_PROJECTOR"))));
            if(boss.ShipProjectorsLeft!=2)failures.Add($"{boss.ShipProjectorsLeft} projectors left after the first third");
            yield return Until(()=>MeretSaid("BOSS_SHIFT MeretShip"),12,lead,partner);
            if(!MeretSaid("BOSS_SHIFT MeretShip 2"))failures.Add("the first projector out wasn't her shift");
            yield return Until(()=>MeretSaid("BOSS_MOVE MeretShip Clamps"),8,lead,partner);
            if(!MeretSaid("BOSS_MOVE MeretShip Clamps"))failures.Add("after the first projector the next move isn't two clamps at once (she changes the lanes)");
            // The other two: the last out is the end of her.
            for(int window=0;window<16&&body.Alive;window++)
            {
                yield return Window();
                for(float end=Time.time+2.5f;Time.time<end&&body.Alive;)
                {
                    var struck=Enumerable.Range(0,3).Where(i=>boss.ShipProjectorOpen(i)).Select(i=>boss.ShipProjectorHealth(i)).FirstOrDefault()??body;
                    struck.Receive(new DamagePacket{source=lead.Health,amount=body.maximum*.06f,type=DamageType.Beam,weight=HitWeight.Heavy});
                    lead.Health.integrity=lead.Health.maximum;yield return new WaitForSeconds(.1f);
                }
            }
            if(body.Alive)failures.Add($"fixture: she still stands at {body.integrity/body.maximum:P0} after sixteen windows");
            if(!Has("bossdown.MeretShip"))failures.Add("the third projector out, she isn't counted down");
            if(!MeretSaid("BOSS_LEAVES MeretShip"))failures.Add("she didn't leave by her way out");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>A clamp comes up through the plating where it was marked: a hero standing on the seat is struck and set beside it, never walled inside it (D289); it stands solid at the plane
        /// (a hull flown at it stops at its side; her broadside's lane through it ends at it, and a ship in its lee isn't struck); then it sinks and lets go of the lane. Two at once from her first
        /// projector out.</summary>
        [UnityTest] public IEnumerator HerClampsRiseMarkedSetAHeroBesideThemAndStandAsCover()
        {
            yield return Load("Arena_Flight");MeretListen();
            var boss=Meret("MeretShip",ShipAt,true,180);yield return Frames(3);var lead=Lead;var partner=Partner;Fresh(lead);Fresh(partner);
            var clamps=boss.ShipClampsHeld.ToList();Assert.AreEqual(3,clamps.Count,"fixture: her three clamps");
            var middle=clamps.OrderBy(c=>Flat(c.AtPlane,ShipAt+Vector3.back*30)).First();
            Put(lead,middle.AtPlane);Put(partner,middle.AtPlane+Vector3.right*40);float before=lead.Health.integrity;
            var failures=new List<string>();
            if(MeretBroken){middle.Rise(9);yield return Frames(2);}
            else
            {
                var earlier=new HashSet<Telegraph>(Telegraph.Live);
                Assert.IsTrue(boss.Perform("Clamp"),"fixture: a clamp");yield return null;
                var mark=NewMarks(boss,earlier).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Circle);
                if(mark==null||!mark.Contains(middle.AtPlane))failures.Add("the clamp's seat isn't marked before it rises");
                yield return Until(()=>middle.Up,4,lead,partner);
                if(mark!=null&&mark.Elapsed<.3f)failures.Add("the clamp rose with its marking under .3 s old");
                if(lead.Health.integrity>=before)failures.Add("a hero on the seat as it rose wasn't struck");
            }
            if(!middle.Up)failures.Add("fixture: the clamp never stood up");
            float hull=HeroCollision.HullRadius(lead.character),body=lead.GetComponent<CharacterController>().radius;
            if(Flat(lead.transform.position,middle.AtPlane)<MeretClamp.Tune.Solid+hull)failures.Add($"a hero stood on the seat is walled inside the risen clamp, {Flat(lead.transform.position,middle.AtPlane):0.0} m from its middle (D289)");
            // Solid at the plane: a hull flown straight at it stops at its side.
            yield return Idle(boss);var ship=lead.GetComponent<CharacterController>();
            Put(lead,middle.AtPlane+Vector3.left*14);yield return Frames(2);
            for(float t=0;t<2;t+=Time.deltaTime){if(ship.enabled)ship.Move(Vector3.right*12*Time.deltaTime);yield return null;}
            // Stopped at its side by the ship's own body (its controller), give or take the controller's skin.
            if(Flat(lead.transform.position,middle.AtPlane)<MeretClamp.Tune.Solid+body-.2f)failures.Add($"a hull flew into the risen clamp, {Flat(lead.transform.position,middle.AtPlane):0.0} m from its middle: it isn't solid at the plane");
            // Cover: her broadside's lane at a ship in its lee ends at the clamp.
            var lee=middle.AtPlane+(middle.AtPlane-boss.transform.position).normalized*7;Put(lead,lee);Fresh(lead);before=lead.Health.integrity;
            var earlier2=new HashSet<Telegraph>(Telegraph.Live);
            Assert.IsTrue(boss.Perform("Broadside"),"fixture: her broadside");yield return null;
            var through=NewMarks(boss,earlier2).Where(t=>t.Shape==Telegraph.Kind.Line).OrderBy(t=>Vector3.Cross(t.Forward,lee-t.Origin).magnitude).FirstOrDefault();
            yield return Idle(boss);
            if(through==null)failures.Add("fixture: no broadside lane");
            else if(through.Contains(lee))failures.Add("her broadside's lane runs through a risen clamp to the ship in its lee");
            if(lead.Health.integrity<before)failures.Add("a ship in a risen clamp's lee was struck by her broadside");
            // It sinks in its time, and lets go at once.
            yield return Until(()=>!middle.Up,BossController.ShipTune.ClampStands[0]+2,lead,partner);
            if(middle.Up)failures.Add("the clamp never sank");
            // Two at once from her first projector out.
            boss.Health.integrity=boss.Health.maximum*.6f;yield return Frames(3);yield return Idle(boss);
            var earlier3=new HashSet<Telegraph>(Telegraph.Live);
            Assert.IsTrue(boss.Perform("Clamps"),"fixture: two clamps");yield return null;
            int marked=NewMarks(boss,earlier3).Count(t=>t.Shape==Telegraph.Kind.Circle);
            if(marked!=2)failures.Add($"her first projector out, she marks {marked} clamps, not two");
            yield return Idle(boss);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>A conversation that holds the controls holds her (D275): her broadside's marking stops filling, nothing of hers lands, a risen clamp's clock stands, and the moment the talk
        /// ends both go on from where they stood.</summary>
        [UnityTest] public IEnumerator AConversationHoldsHerMarkingsAndHerClampsWhereTheyStand()
        {
            yield return Load("Arena_Flight");MeretListen();
            var boss=Meret("MeretShip",ShipAt,true,180);yield return Frames(3);var lead=Lead;var partner=Partner;Fresh(lead);Fresh(partner);
            var clamp=boss.ShipClampsHeld.OrderBy(c=>Flat(c.AtPlane,ShipAt+Vector3.back*30)).First();
            Put(lead,clamp.AtPlane);Put(partner,clamp.AtPlane+Vector3.right*40);
            Assert.IsTrue(boss.Perform("Clamp"),"fixture: a clamp");yield return Until(()=>clamp.Up,4,lead,partner);yield return Idle(boss);
            Put(lead,ShipAt+Vector3.back*22+Vector3.left*20);Fresh(lead);
            Assert.IsTrue(boss.Perform("Broadside"),"fixture: her broadside");yield return Frames(3);
            var mark=Marks(boss).First();var input=GameServices.Current.Input;float elapsed=mark.Elapsed,bar=lead.Health.integrity;
            var failures=new List<string>();
            // Built wrong on purpose: the talk never takes the controls.
            float stands=BossController.ShipTune.ClampStands[0];
            for(float until=Time.time+stands+1;Time.time<until;){input.Blocked=!MeretBroken;yield return null;}
            if(mark.Elapsed-elapsed>.05f)failures.Add($"her broadside's marking filled {mark.Elapsed-elapsed:0.00} s through a conversation");
            if(lead.Health.integrity<bar)failures.Add("something of hers landed through a conversation");
            if(!clamp.Up)failures.Add("a risen clamp sank through a conversation: its clock ran");
            input.Blocked=false;
            yield return Until(()=>mark.Released,4,lead,partner);
            if(!mark.Released)failures.Add("her marking never landed once the talk ended");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ phase two: her couplings
        /// <summary>Her bar is held at its first third while both couplings are shut (blows stop at the line); a weld always ends with her welding arm overreached, its coupling's seat lit
        /// beside her and her frame open; a hero who takes its weight there and holds through the ease opens it: the welding arm hangs dead, the bar passes the line, and her shift (she is down
        /// an arm) brings the coolant flood next. Taken while no arm is overreached, the coupling never opens.</summary>
        [UnityTest] public IEnumerator AnOverreachedArmsCouplingIsOpenedByTakingItsWeightAndHerBarIsHeldUntilItIs()
        {
            yield return Load("Arena_Ground");MeretFloor();MeretListen();
            var boss=Meret("MeretGantry",FrameAt,true,0);yield return Frames(3);var lead=Lead;var partner=Partner;Fresh(lead);Fresh(partner);
            Put(lead,FrameAt+Vector3.forward*8);Put(partner,FrameAt+Vector3.right*30);var body=boss.Health;var failures=new List<string>();
            // Held at its first third: big blows stop at the line while both couplings are shut.
            boss.GetComponent<EnemyBrain>().Passive=false;
            for(int i=0;i<8;i++){body.Receive(new DamagePacket{source=lead.Health,amount=body.maximum*.2f,type=DamageType.Pulse});yield return null;}
            if(body.integrity<body.maximum*2/3f-1)failures.Add($"with both couplings shut her bar went to {body.integrity/body.maximum:P0}: it is held at its first third");
            boss.GetComponent<EnemyBrain>().Passive=true;yield return Idle(boss);
            var seat=boss.GantryWelder;Assert.IsNotNull(seat,"fixture: her welding arm's coupling");
            // Built wrong on purpose: its weight taken with no arm overreached.
            if(!MeretBroken)
            {
                Assert.IsTrue(boss.Perform("Weld"),"fixture: her weld");
                yield return Until(()=>seat.Offered,6,lead,partner);
                if(!seat.Offered)failures.Add("her weld ran out and the welding arm's coupling wasn't offered");
                if(!body.Broken)failures.Add("her welding arm at full stretch and her frame isn't open");
                if(boss.GantryFrame.Right!=MeretFrame.Arm.Reaching)failures.Add("her welding arm isn't drawn at full stretch while its coupling is offered");
            }
            boss.GetComponent<EnemyBrain>().Passive=false;
            Put(lead,seat.transform.position);Fresh(lead);yield return Frames(2);
            if(seat.Available)seat.Interact();
            for(float end=Time.time+4;!seat.IsOpen&&Time.time<end;){foreach(var hero in PartyController.Current.members)hero.motor.Move(Vector2.zero,false,false);yield return null;}
            if(!seat.IsOpen)failures.Add("a hero in the seat took the coupling's weight and held: it never opened");
            yield return Frames(3);
            if(seat.IsOpen)
            {
                if(boss.GantryFrame.Right!=MeretFrame.Arm.Hanging)failures.Add("the coupling opened and the welding arm doesn't hang");
                if(boss.GantryWelderOn)failures.Add("the coupling opened and she still has her welding arm");
                if(body.integrity>=body.maximum*2/3f)failures.Add("the coupling opened and her bar didn't pass its first third");
                yield return Until(()=>MeretSaid("BOSS_MOVE MeretGantry Coolant"),10,lead,partner);
                if(!MeretSaid("BOSS_SHIFT MeretGantry 2"))failures.Add("the first coupling open wasn't her shift");
                if(!MeretSaid("BOSS_MOVE MeretGantry Coolant"))failures.Add("after her first coupling the next move isn't the coolant flood");
            }
            boss.GetComponent<EnemyBrain>().Passive=true;
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>A slam that meets nobody leaves the press overreached and, with her bar held at its line, its coupling offered; one that meets a hero doesn't. Each machine is named as she
        /// first uses it, by her and then by the hero (manifest 7: the press, the welding line, the coolant, each its own line).</summary>
        [UnityTest] public IEnumerator ASlamOnNobodyOverreachesThePressAndSheNamesEachMachine()
        {
            yield return Load("Arena_Ground");MeretFloor();MeretListen();
            var spoken=new List<(string who,string line)>();System.Action<string,string> heard=(who,line)=>spoken.Add((who,line));BarkService.Spoken+=heard;
            try
            {
                // She fights, and between the presses the lead stands out past the 30 m she chooses within, so every move she makes is the one asked for.
                var boss=Meret("MeretGantry",FrameAt,false,0);yield return Frames(3);
                var lead=Lead;var partner=Partner;Fresh(lead);Fresh(partner);var away=FrameAt+Vector3.back*34;Put(lead,away);Put(partner,FrameAt+Vector3.back*34+Vector3.right*6);
                boss.Health.Receive(new DamagePacket{source=lead.Health,amount=1,type=DamageType.Pulse});
                // Her bar held at its first line (a collar is offered only there: MeretReplayTests.ACouplingIsOfferedOnlyWhileHerBarIsHeldAndTakesNothingOffIt), still in her first phase.
                boss.Health.integrity=boss.Health.maximum*2/3f+.4f;
                var failures=new List<string>();
                IEnumerator Ask(string move,Vector3 at)
                {
                    yield return Idle(boss);Fresh(lead);meretLog.Clear();Put(lead,at);
                    for(float end=Time.unscaledTime+4;!boss.Perform(move)&&Time.unscaledTime<end;)yield return null;
                }
                // On a hero: no overreach.
                yield return Ask("Slam",FrameAt+Vector3.forward*4);
                yield return Until(()=>MeretSaid("MERET_SLAM"),4,lead,partner);yield return Frames(2);
                if(boss.GantryPress.Offered)failures.Add("a slam that met a hero left the press overreached");
                Put(lead,away);
                // On nobody: he steps out of the strip as it is marked. Built wrong on purpose: he stays in it.
                yield return Ask("Slam",FrameAt+Vector3.forward*4);
                if(!MeretBroken)Put(lead,FrameAt+Vector3.forward*4+Vector3.right*6);
                yield return Until(()=>MeretSaid("MERET_SLAM"),4,lead,partner);yield return Frames(2);
                if(!boss.GantryPress.Offered)failures.Add("a slam that met nobody didn't leave the press overreached with its coupling offered");
                Put(lead,away);
                yield return Ask("Weld",FrameAt+Vector3.forward*8);yield return Idle(boss);Put(lead,away);
                yield return Ask("Coolant",FrameAt+Vector3.forward*8);yield return Idle(boss);Put(lead,away);
                yield return new WaitForSeconds(BossController.GantryTune.RadioAfter+.5f);
                var hers=spoken.Where(s=>s.who=="Meret").Select(s=>s.line).ToList();
                foreach(string line in new[]{"That's the press. You know its tell.","The welding line. You've jumped this before.","Coolant. Mind your footing. I do."})
                    if(hers.Count(l=>l==line)!=1)failures.Add($"she said '{line}' {hers.Count(l=>l==line)} times: once, as she first uses its machine ({string.Join(" / ",hers)})");
                // The hero's three, from barks.json (manifest 7: "oldmachines", three in turn: the press, the welding line, the coolant).
                var theirs=new Dictionary<string,string[]>
                {
                    ["Taren"]=new[]{"That's the Saint's slam. Same tell.","The Works' welding line. Over it.","Foundry sluices. Dry floor, quick."},
                    ["Sela"]=new[]{"That's your press, Vosk. Built properly.","The welding line. We've jumped that.","Coolant. Sluices. Move."},
                    ["Idra"]=new[]{"Press. You know it.","Weld line. Jump.","Coolant. Clear the floor."},
                };
                var answers=spoken.Where(s=>s.who==lead.character).Select(s=>s.line).ToList();
                foreach(string line in theirs[lead.character])if(!answers.Contains(line))failures.Add($"{lead.character} never answered her machine with '{line}' ({string.Join(" / ",answers)})");
                boss.GetComponent<EnemyBrain>().Passive=true;
                Assert.IsEmpty(failures,string.Join("\n",failures));
            }
            finally{BarkService.Spoken-=heard;}
        }

        // ------------------------------------------------------------------ phase three: the duel
        /// <summary>Her guard up, a blow into it turns (nothing lands) and the first one is answered: a riposte down a short marked fan at whoever struck, at least .3 s marked, that lands on
        /// him if he stays; then her counter is spent and she is open (a blow lands half again). Left alone her guard runs out and she steps in with a marked thrust instead.</summary>
        [UnityTest] public IEnumerator HerGuardTurnsABlowAnswersItWithAMarkedRiposteAndIsSpent()
        {
            yield return Load("Arena_Ground");MeretFloor();MeretListen();
            var boss=Meret("MeretWell",FrameAt,false,0);yield return Frames(3);boss.GetComponent<EnemyBrain>().Passive=true;var body=boss.Health;
            var lead=Lead;var partner=Partner;Fresh(lead);Fresh(partner);Put(lead,FrameAt+Vector3.forward*2.2f);Put(partner,FrameAt+Vector3.right*30);
            boss.GetComponent<EnemyBrain>().Passive=false;yield return null;
            var failures=new List<string>();
            // Built wrong on purpose: struck before her guard is up.
            if(MeretBroken){body.Receive(new DamagePacket{source=lead.Health,amount=200,type=DamageType.Kinetic});}
            Assert.IsTrue(boss.Perform("Guard"),"fixture: her guard");
            yield return Until(()=>boss.WellGuarding,2,lead,partner);
            if(!MeretBroken)
            {
                float bar=body.integrity;var earlier=new HashSet<Telegraph>(Telegraph.Live);
                float landed=body.Receive(new DamagePacket{source=lead.Health,amount=200,type=DamageType.Kinetic});
                if(landed>0||body.integrity<bar)failures.Add($"a blow into her guard landed {landed:0}");
                yield return Until(()=>MeretSaid("MERET_COUNTER"),1,lead,partner);
                var fan=NewMarks(boss,earlier).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Cone);
                if(fan==null)failures.Add("her riposte marks nothing");
                else
                {
                    if(!fan.Contains(lead.transform.position))failures.Add("her riposte isn't marked through the hero who struck");
                    float before=lead.Health.integrity;
                    yield return Until(()=>fan.Released,2,lead,partner);
                    if(fan.Duration<.3f)failures.Add($"her riposte is marked {fan.Duration:0.00} s: too short to read");
                    if(lead.Health.integrity>=before)failures.Add("her riposte didn't land on the hero who stood in it");
                }
            }
            yield return Until(()=>MeretSaid("MERET_COUNTER_SPENT"),3,lead,partner);
            if(!MeretSaid("MERET_COUNTER "+lead.character))failures.Add("she didn't answer the blow struck into her guard");
            if(!MeretSaid("MERET_COUNTER_SPENT")||!body.Broken)failures.Add("her counter thrown, she isn't open");
            else
            {
                float hit=body.Receive(new DamagePacket{source=lead.Health,amount=100,type=DamageType.Kinetic});
                if(hit<149)failures.Add($"a blow in her spent counter dealt {hit:0} of 100: an opening is half as much again");
            }
            boss.GetComponent<EnemyBrain>().Passive=true;yield return Idle(boss);
            // Left alone: her guard runs out and she steps in with a thrust.
            meretLog.Clear();Fresh(lead);Put(lead,FrameAt+Vector3.forward*3);Assert.IsTrue(boss.Perform("Guard"),"fixture: her guard");
            yield return Until(()=>MeretSaid("MERET_THRUST"),BossController.WellTune.Stance[0]+2,lead,partner);
            if(!MeretSaid("MERET_THRUST"))failures.Add("her guard left alone ran out and she didn't step in");
            if(MeretSaid("MERET_COUNTER"))failures.Add("she countered a blow nobody struck");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>The hero the player leads, standing still, has a clamp marked under him and closing: it strikes and holds him. One who keeps moving never has one, and nor does the
        /// companion, who stands where her brain stands her (MeretReplayTests: played through the pad, Idra was clamped five times in one duel).</summary>
        [UnityTest] public IEnumerator AHeroWhoStandsStillHasAClampMarkedUnderHimAndOneWhoMovesDoesNot()
        {
            yield return Load("Arena_Ground");MeretFloor();MeretListen();
            var boss=Meret("MeretWell",FrameAt,false,0);yield return Frames(3);
            var lead=Lead;var partner=Partner;Fresh(lead);Fresh(partner);
            var still=FrameAt+new Vector3(-7,0,8);var round=FrameAt+new Vector3(7,0,8);Put(lead,still);
            // Long enough for several still spells between her own moves at him (a blow that shoves him starts his count again).
            for(float end=Time.time+9;Time.time<end;)
            {
                foreach(var hero in PartyController.Current.members)hero.motor.Move(Vector2.zero,false,false);
                // The companion stands still too, the whole time; the lead stands still. Built wrong on purpose: the lead keeps moving, round and round.
                if(Flat(partner.transform.position,round)>.5f)Put(partner,round);
                float a=Time.time*1.6f;if(MeretBroken)Put(lead,still+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*2.5f);else if(Flat(lead.transform.position,still)>.6f)Put(lead,still);
                lead.Health.integrity=lead.Health.maximum;partner.Health.integrity=partner.Health.maximum;
                yield return null;
            }
            var failures=new List<string>();
            if(!MeretSaid("MERET_TRAP_STILL "+lead.character))failures.Add("a hero who stood still had no clamp marked under him");
            if(!MeretSaid("MERET_CLAMPED "+lead.character))failures.Add("no clamp ever closed on the hero who stood still");
            if(MeretSaid("MERET_TRAP_STILL "+partner.character))failures.Add("the companion, standing where her brain stood her, had a clamp marked under her");
            boss.GetComponent<EnemyBrain>().Passive=true;
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>Into her last phase she steps back and asks her question (her third duel line), and listens: nothing barks over the quiet after it, and nothing of hers is marked or lands
        /// in it; then she fights on.</summary>
        [UnityTest] public IEnumerator SheAsksHerQuestionAndListensInAQuietNothingIsSaidOver()
        {
            yield return Load("Arena_Ground");MeretFloor();MeretListen();
            var spoken=new List<(string who,string line)>();System.Action<string,string> heard=(who,line)=>spoken.Add((who,line));BarkService.Spoken+=heard;
            try
            {
                var boss=Meret("MeretWell",FrameAt,false,0);yield return Frames(3);var body=boss.Health;
                var lead=Lead;var partner=Partner;Fresh(lead);Fresh(partner);Put(lead,FrameAt+Vector3.forward*4);Put(partner,FrameAt+Vector3.right*30);
                body.Receive(new DamagePacket{source=lead.Health,amount=1,type=DamageType.Kinetic});
                yield return Idle(boss);
                // Built wrong on purpose: into her second phase, not her last.
                body.integrity=body.maximum*(MeretBroken?.5f:.25f);
                yield return Until(()=>boss.WellListening||MeretSaid("BOSS_MOVE MeretWell Traps"),8,lead,partner);
                var failures=new List<string>();
                if(!boss.WellListening)failures.Add("into her last phase she didn't stop and listen");
                else
                {
                    yield return new WaitForSeconds(.7f);
                    if(!spoken.Any(s=>s.who=="Meret"&&s.line=="Good. Is the nursery clear? Tell me truthfully."))failures.Add("she didn't ask her question");
                    int said=spoken.Count;float bar=lead.Health.integrity;
                    for(float end=Time.time+BossController.WellTune.Quiet-1;Time.time<end&&boss.WellListening;)
                    {
                        foreach(var hero in PartyController.Current.members)hero.motor.Move(Vector2.zero,false,false);
                        if(Marks(boss).Any(t=>t.Shape!=Telegraph.Kind.Circle||t.Radius<BossController.WellTune.GuardRing))failures.Add("something of hers is marked in the quiet after her question");
                        if(!BarkService.Hushed)failures.Add("barks may play over the quiet after her question");
                        yield return null;
                    }
                    if(spoken.Count>said)failures.Add("something was said in the quiet after her question: "+spoken[said].line);
                    if(lead.Health.integrity<bar)failures.Add("something of hers landed in the quiet after her question");
                    yield return Until(()=>!boss.WellListening,4,lead,partner);
                    if(boss.WellListening)failures.Add("she never fought on after her question");
                }
                boss.GetComponent<EnemyBrain>().Passive=true;
                Assert.IsEmpty(failures.Distinct().ToList(),string.Join("\n",failures.Distinct()));
            }
            finally{BarkService.Spoken-=heard;}
        }
        /// <summary>At nought she yields standing: counted down (bossdown.MeretWell, the flag the chamber's MeretYields waits on), nothing barked over her, and her body upright, its head where it was,
        /// until the person takes over at the same spot (DefeatPresentation.Yielded). A captain who gives up low (the broken state: the Warmaster's yield) would fail here.</summary>
        [UnityTest] public IEnumerator AtNoughtSheYieldsStandingAndTheCustodySceneTakesOverWhereSheStands()
        {
            yield return Load("Arena_Ground");MeretFloor();MeretListen();
            var spoken=new List<(string who,string line)>();System.Action<string,string> heard=(who,line)=>spoken.Add((who,line));BarkService.Spoken+=heard;
            string handedId=null;Vector3 handedAt=Vector3.zero;float headAtHandOver=-1;Component body=null;
            System.Action<string,Vector3,Quaternion> yielded=(id,at,_)=>{handedId=id;handedAt=at;if(body!=null)headAtHandOver=HeadOf(body);};DefeatPresentation.Yielded+=yielded;
            try
            {
                // Built wrong on purpose: the Warmaster, who gives up low.
                string id=MeretBroken?"DraskVaal":"MeretWell";
                var enemy=ActorFactory.Enemy(Def(id),FrameAt);enemy.Passive=false;body=enemy;yield return new WaitForSeconds(.6f);
                var lead=Lead;var partner=Partner;Fresh(lead);Fresh(partner);Put(lead,FrameAt+Vector3.forward*6);Put(partner,FrameAt+Vector3.right*30);
                enemy.Passive=true;yield return new WaitForSeconds(.6f);
                float standing=HeadOf(enemy);var where=enemy.transform.position;Assert.Greater(standing,1,"fixture: her head is found on her rig");
                spoken.Clear();
                enemy.Health.InvulnerableUntil=0;enemy.Health.Receive(new DamagePacket{source=lead.Health,amount=enemy.Health.maximum*100,type=DamageType.Plasma});
                float lowest=standing;
                for(float end=Time.unscaledTime+3;handedId==null&&Time.unscaledTime<end;){if(enemy!=null)lowest=Mathf.Min(lowest,HeadOf(enemy));yield return null;}
                var failures=new List<string>();
                if(!Has("bossdown."+id))failures.Add($"at nought bossdown.{id} isn't set: the custody scene waits on it");
                if(handedId!=id)failures.Add("the person never took over from the fighter's body");
                else if(Flat(handedAt,where)>.5f)failures.Add($"the person takes over {Flat(handedAt,where):0.0} m from where she stood");
                TestContext.WriteLine($"MERET_YIELD head standing={standing:0.00} lowest={lowest:0.00} atHandOver={headAtHandOver:0.00}");
                if(lowest<standing*.9f)failures.Add($"she went down as she yielded: her head from {standing:0.00} m to {lowest:0.00} (she does not fall, kneel or shout)");
                var party=spoken.Where(s=>s.who=="Taren"||s.who=="Sela"||s.who=="Idra").ToList();
                if(party.Count>0)failures.Add("the party barked over her yielding: "+party[0].line);
                Assert.IsEmpty(failures,string.Join("\n",failures));
            }
            finally{BarkService.Spoken-=heard;DefeatPresentation.Yielded-=yielded;}
        }

        // ------------------------------------------------------------------ between her phases (wiring 6.5)
        /// <summary>Which of her doors the next load of the ground yard stands (null: none), as a builder would stand it: its checkpoint spawn 14 m from her, her spawner, a box round her.</summary>
        static string meretDoor;
        static void MeretDoorBuilt(Scene scene,LoadSceneMode mode)
        {
            if(scene.name!="Arena_Ground"||meretDoor==null)return;
            var root=new GameObject("Meret's door");SceneManager.MoveGameObjectToScene(root,scene);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Meret fixture floor";floor.transform.SetParent(root.transform,false);floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);
            string spawnId=meretDoor=="MeretGantry"?"Gantry":"Well";
            var spawn=new GameObject(spawnId,typeof(SpawnPoint));spawn.transform.SetParent(root.transform,false);spawn.transform.position=FrameAt+Vector3.back*14;spawn.GetComponent<SpawnPoint>().id=spawnId;
            var door=new GameObject(meretDoor+" door");door.SetActive(false);door.transform.SetParent(root.transform,false);door.transform.position=FrameAt;
            var box=door.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(24,4,24);
            var post=new GameObject("Her spawner");post.transform.SetParent(door.transform,false);post.transform.SetPositionAndRotation(FrameAt,Quaternion.Euler(0,180,0));
            var spawner=post.AddComponent<Spawner>();spawner.definition=GameCatalog.Find<EnemyDef>(meretDoor);spawner.count=1;spawner.radius=0;spawner.faceAsPlaced=true;
            var volume=door.AddComponent<EncounterVolume>();volume.encounterId=meretDoor;volume.spawners=new[]{spawner};volume.membranes=new Membrane[0];volume.checkpointSpawn=spawnId;
            volume.requiredFlag=meretDoor=="MeretGantry"?"end.meretShip":"end.wellEntered";
            door.SetActive(true);Physics.SyncTransforms();
        }
        /// <summary>A defeat at the gantry comes back at the gantry's door with her ship kept, and a defeat in the well at the well's door with the ship and the gantry kept: never further back
        /// than her door (wiring 6.5), the party whole and the phase's boss whole, the earlier phases' flags (and so their talks) as they were won.</summary>
        [UnityTest] public IEnumerator ADefeatInAnyPhaseComesBackAtThatPhasesDoorWithTheEarlierPhasesKept()
        {
            SceneManager.sceneLoaded+=MeretDoorBuilt;var failures=new List<string>();
            try
            {
                foreach(var (phase,spawnId,earlier) in new[]{("MeretGantry","Gantry",new[]{"bossdown.MeretShip","end.meretShip"}),
                    ("MeretWell","Well",new[]{"bossdown.MeretShip","end.meretShip","bossdown.MeretGantry","end.meretGantry","end.wellEntered"})})
                {
                    meretDoor=phase;
                    // Built wrong on purpose: the earlier phases won only after her door has saved, so the save at the door doesn't hold them.
                    if(!MeretBroken)foreach(string flag in earlier)GameServices.Current.Flags.SetBool(flag,true);
                    GameServices.Current.Flags.SetBool(phase=="MeretGantry"?"end.meretShip":"end.wellEntered",true);
                    State.spawn="Arrival";yield return Load("Arena_Ground");
                    var door=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).FirstOrDefault(e=>e.encounterId==phase);
                    Assert.IsNotNull(door,"fixture: her door stands in the yard");
                    foreach(var hero in PartyController.Current.members)Put(hero,FrameAt+Vector3.back*6);
                    door.Begin();yield return Frames(3);
                    if(MeretBroken)foreach(string flag in earlier)GameServices.Current.Flags.SetBool(flag,true);
                    var her=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).FirstOrDefault(e=>e.definition.id==phase);
                    Assert.IsNotNull(her,"fixture: "+phase+" came out at her door");her.Passive=true;her.Health.integrity=her.Health.maximum*.5f;
                    var saved=GameServices.Current.Saves.Load("autosave");
                    if(saved.spawn!=spawnId)failures.Add($"walking into {phase}'s fight saved at {saved.spawn}, not her door's {spawnId}");
                    foreach(var hero in PartyController.Current.members)hero.Health.Receive(new DamagePacket{amount=hero.Health.maximum*100,type=DamageType.Pulse});
                    var panel=Object.FindFirstObjectByType<DefeatPanel>();Assert.IsNotNull(panel,"fixture: there is a defeat panel");
                    for(float end=Time.unscaledTime+6;!panel.IsOpen&&Time.unscaledTime<end;)yield return null;
                    Assert.IsTrue(panel.IsOpen,"fixture: both heroes down opens the defeat panel");
                    Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b=>b.name=="Retry").onClick.Invoke();yield return null;
                    for(float end=Time.unscaledTime+25;SceneFlow.Current.Loading&&Time.unscaledTime<end;)yield return null;
                    yield return Frames(3);Hold();
                    if(State.spawn!=spawnId)failures.Add($"a defeat at {phase} came back at {State.spawn}, not her door's {spawnId}");
                    var at=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).FirstOrDefault(sp=>sp.id==spawnId);
                    if(at!=null&&Flat(Lead.transform.position,at.transform.position)>6)failures.Add($"a defeat at {phase} put the party {Flat(Lead.transform.position,at.transform.position):0} m from her door");
                    foreach(string flag in earlier)if(!Has(flag))failures.Add($"a defeat at {phase} lost {flag}: an earlier phase undone");
                    if(Has("bossdown."+phase))failures.Add($"a lost fight counts {phase} as down");
                    foreach(var hero in PartyController.Current.members)if(hero.Health.integrity<hero.Health.maximum-1)failures.Add($"{hero.character} comes back wounded");
                    door=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).FirstOrDefault(e=>e.encounterId==phase);
                    if(door==null||door.Started)failures.Add($"{phase}'s door came back missing or already begun");
                    else
                    {
                        door.Begin();yield return Frames(3);
                        var again=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).FirstOrDefault(e=>e.definition.id==phase);
                        if(again==null)failures.Add($"{phase} isn't there for the second attempt");
                        else{again.Passive=true;if(again.Health.integrity<again.Health.maximum-1)failures.Add($"{phase} keeps her wounds from the lost attempt");}
                    }
                    foreach(var left in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))left.Passive=true;
                }
            }
            finally{SceneManager.sceneLoaded-=MeretDoorBuilt;meretDoor=null;}
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

    }
}
