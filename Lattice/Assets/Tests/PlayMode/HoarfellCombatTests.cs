using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 6's fights, each on a bare floor in the proving yards with what it needs made here (nothing waits on the Frozen Host being built). The party is the one the
    /// chapter's fights are met by: chapter 6 once Idra has joined (DevLoadout chapter6joined), with every pair a player can put out where the fight is about the party. The hit sizes on
    /// all three heroes; one test for each family the chapter added or changed, saying what a player sees: the Thawed Guards' rank and its middle, the Excavator's swept lance, the
    /// Bridle-hound's pin, its shattering in the air and a braced hero throwing it off. Then the Host Marshal: its couplings (shut before the fight, each a third of the wall and its rank put
    /// back to sleep as ice, the rig holding its bar, the third the shift), its lunge lodging in that ice, the cold (the frost closing, its own pace, the sweep that cracks the floor and the
    /// knee it falls to), its sleep, and its length for every pair. The chapter's expectations for the shared enemy and boss tests are here too.</summary>
    public sealed class HoarfellCombatTests
    {
        const float X=230;
        readonly List<string> log=new();
        static readonly string[] Heeded={"LEAPER_","GUARD_","RANK_","MARSHAL_","COUPLING_","FROST_","BOSS_","COMBAT_KILL","BREAK_OK","ENEMY_PUT_ON_FLOOR","ENEMY_OFF_FLOOR"};
        void Heard(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply(DevLoadout.JoinedLoadout);log.Clear();Application.logMessageReceived+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;
            if(FrostRing.Current!=null)FrostRing.Current.Dismiss();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Hoarfell(ContentRegistry.Book book)
        {
            // Where a hero stands for each ordinary enemy's tell: inside the reach its attack starts from (inside the stalker's ring, a guard's and the excavator's slam, the hound's spring).
            book.Tell("RimeStalker",2.6f);book.Tell("FrostMotes",2);book.Tell("ThawedGuard",2);book.Tell("MusterExcavator",2);book.Tell("BridleHound",5);
            book.Tell("MusterTender",8,flight:true);book.Tell("CurrentSkiff",8,flight:true);
            // The Host Marshal: met at Sync 17 to 18 (the chapter runs 16 to 19; HoarfellBuilder.Enemies' ledger), measured at the lower. Its door is the bottom gallery's
            // (docs/maps/FrozenHost.md, "Boss arena contract"): Idra has joined by then and the crossing at its door is heard.
            // Its driver's share is measured, not assumed: it fell in 94.8 s with the lead free and 120.8 s with him walled in by frozen ranks (reconnaissance on the 03:16 build,
            // November 14: .65 and .51 of the 1,859 a second this party deals flat out). That is under the two minutes the plan asks, and its integrity can't answer it: at 115,000
            // the slowest pair already stands at 292 s at a quarter of flat out. What gives the bar away is how long it stands open in the cold (four falters and two lodged spears in
            // a hundred seconds, each at half as much again): retune those against a measured fight, then set this from the chain. Until then the shared length test says so.
            // Retuned the same afternoon (BossController.HostMarshal.cs): its largest opening was the break, seven a fight, each letting a blow past the warm shell at 4.3 times;
            // warm, nothing breaks it now, the shell is .3 (.35), a falter 2 s (2.4) and a spear caught in ice 2.5 s (3). Laid over the clean fight's own events that is about 125 s
            // for the driver, a share near .5. This stays at .6 until a chain measures it: with .6 the shared floor wants this party under 1,597 a second flat out and the pair test's
            // ceiling wants every pair over 1,533, a window narrower than the 1,711 to 1,835 this one measure has read on one build.
            // Measured after the retune (reconnaissance, the 14:26 build, Builds/quality/nov14-recon19/s36): 115,000 fell in 139 s, which against 1,835 a second is .45.
            // That party wore a Refined Edge and Scrap parts, Idra no Emitter or Module (the driver put nothing on after chapter 2). Fought through the pad by chapter 6's floor party from
            // its real door (FloorPartyBossReplayTests, November 15), 115,000 fell in 107.0 and 122.1 s: .575 and .526 of 1,869 and 1,789 a second, under its two minutes. It is 125,000,
            // which fell the same way in 130.0, 141.9 and 135.4 s (.538, .497 and .494 of 1,788, 1,771 and 1,870): this is the lowest. The bar can't go much further: the slowest pair's
            // quarter of flat out (TheHostMarshalLastsWhatThePlanAsksForEveryPairAPlayerCanPutOut) stands at 266 to 280 s at 125,000, against 300.
            // .58 since D362 (November 22): today's scout of the 10:30 build felled 125,000 in 115.5 s (nov22-recon-e s36) against 1,798 and 1,879 a second (d355-dome3, d355-lengths),
            // which is .589, rounded down; nov22-whole took 102.9 s. The floor party through the pad read .481 the same morning (d355-chapters): the player's party gets more.
            // 155,000 since D362 (continued): the slow pair's 400 s at a quarter, on the slowest pair read today (Taren and Sela, 1,637 a second, d362c-ch3to7, where 165,000
            // stood at 403), holds it there. Its fast case is set by its openings, not its integrity (it is open after every move): 75.3 s at 130,000 (nov22-recon-h-36) is about 90 s.
            book.Boss(new ContentRegistry.BossSheet{id="HostMarshal",zone="FrozenHost",door="HostMarshal",doorFlags=new[]{"hoar.idraJoined","hoar.marshalSeen"},sync=17,driverShare=.58f,
                specials=new[]{("SpearLunge",new Vector3(0,0,8)),("ShieldBash",new Vector3(0,0,3)),("CrackingSweep",new Vector3(0,0,5))},
                // Its call strikes nobody: what it marks is where a rank steps out (TheMarshalsCouplingsAreShutUntilItWakesEachPutsItsRankBackToSleepAndTheThirdIsTheShift).
                testedApart=new[]{"Rally"},
                // A lunge at a hero beyond its 14 m reach plants its spear in the floor; cold, its sweep's blow fails it and it sinks to a knee.
                windows=new[]{("SpearLunge",new Vector3(0,0,19)),("CrackingSweep",new Vector3(0,0,5))}});
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
            Hold();
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.5f);
        }
        /// <summary>The heroes out are the test's to move: no pad, no partner's brain.</summary>
        static void Hold(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        /// <summary>Puts this pair out, as the Party page would.</summary>
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
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Hoarfell fixture floor";floor.transform.position=new Vector3(X,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static (CombatActor lead,CombatActor partner) Party(){var lead=PartyController.Current.Active;return (lead,PartyController.Current.members.First(m=>m!=lead));}
        static void Put(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            actor.Health.maximum=actor.Health.integrity=5000;actor.Health.InvulnerableUntil=0;Physics.SyncTransforms();
        }
        static void Stand(EnemyBrain enemy,Vector3 position,float yaw=0)
        {var body=enemy.GetComponent<CharacterController>();bool was=body.enabled;body.enabled=false;enemy.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));body.enabled=was;Physics.SyncTransforms();}
        static EnemyDef Def(string id){var definition=GameCatalog.Find<EnemyDef>(id);Assert.IsNotNull(definition,"fixture: "+id+" is defined (HoarfellBuilder.Definitions)");return definition;}
        static EnemyBrain Enemy(string id,Vector3 at,bool passive=true){var enemy=ActorFactory.Enemy(Def(id),at);enemy.Passive=passive;return enemy;}
        static DamageType Neutral(Health body)=>System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=body.weakness&&t!=body.resistance);
        static float Hit(Health body,CombatActor from,float amount=100,float breakPower=0)=>body.Receive(new DamagePacket{source=from.Health,amount=amount,type=Neutral(body),breakPower=breakPower});
        static float Kill(Health body,CombatActor from)=>body.Receive(new DamagePacket{source=from.Health,amount=body.maximum*20,type=Neutral(body)});
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>What the game's camera draws, saved for the eye (in the test mirror's Builds/quality/focused/hoarfell).</summary>
        static void Still(string name)
        {
            const int W=1280,H=720;var camera=Camera.main;if(camera==null)return;
            string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/focused/hoarfell"));System.IO.Directory.CreateDirectory(folder);
            var image=new Texture2D(W,H,TextureFormat.RGB24,false);var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var active=RenderTexture.active;var target=camera.targetTexture;
            try{rt.Create();camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,name+".png"),image.EncodeToPNG());}
            finally{camera.targetTexture=target;RenderTexture.active=active;rt.Release();Object.Destroy(rt);Object.Destroy(image);}
        }
        /// <summary>Waits on the fight's own clock, driving the heroes' motors as their brains would: a roll is only carried out by a motor that is being driven.</summary>
        static IEnumerator Until(System.Func<bool> done,float seconds,params CombatActor[] held)
        {
            for(float end=Time.unscaledTime+seconds*1.5f+1,until=Time.time+seconds;!done()&&Time.time<until&&Time.unscaledTime<end;)
            {foreach(var actor in held)if(actor!=null&&actor.Health.Alive)actor.motor.Move(Vector2.zero,false,false);yield return null;}
        }
        static IEnumerator Wait(float seconds,params CombatActor[] held)=>Until(()=>false,seconds,held);
        static bool Faint(Telegraph marking)=>marking.GetComponent<MeshRenderer>().sharedMaterials[1].name.Contains("faint");
        bool Said(string start)=>log.Any(l=>l.StartsWith(start));
        static EnemyBrain[] Guards()=>Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>e.definition!=null&&e.definition.id=="ThawedGuard"&&e.Health.Alive).ToArray();
        /// <summary>Soldiers put back to sleep, as ice: every one, solid or not (D289: one frozen within a hero's reach is ice to look at and nothing to walk into).</summary>
        static GameObject[] Statues()=>Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Frozen rank").Select(t=>t.gameObject).ToArray();
        static Telegraph[] Marks(BossController boss)=>Telegraph.Live.Where(t=>t!=null&&t.owner==boss&&!t.Released).ToArray();

        /// <summary>A thaw coupling as the arena contract asks for one: a Health with the target card's name, a trigger and a hurtbox, answering to the Marshal, 13.5 m from the
        /// gallery's middle at a bearing, with its stretch of wall (thawing and refrozen) and its rank's three places 16 m out either side of it.</summary>
        static ThawCoupling Coupling(int index,float bearing,Vector3 centre)
        {
            var go=new GameObject("Thaw coupling "+index);go.SetActive(false);go.transform.position=centre+Quaternion.Euler(0,bearing,0)*Vector3.forward*13.5f;
            var health=go.AddComponent<Health>();health.id="Thaw coupling";health.maximum=health.integrity=400;health.breakThreshold=100000;
            var shape=go.AddComponent<SphereCollider>();shape.radius=1.3f;shape.center=Vector3.up*1.5f;shape.isTrigger=true;go.AddComponent<Hurtbox>().owner=health;
            var coupling=go.AddComponent<ThawCoupling>();coupling.index=index;coupling.settledBy="HostMarshal";coupling.defeatedFlag="test.coupling"+index;
            coupling.thawing=new GameObject("thawing");coupling.thawing.transform.SetParent(go.transform,false);
            coupling.refrozen=new GameObject("refrozen");coupling.refrozen.transform.SetParent(go.transform,false);
            coupling.ranks=new Transform[3];
            for(int i=0;i<3;i++){var spot=new GameObject("rank place "+i).transform;spot.position=centre+Quaternion.Euler(0,bearing+(i-1)*16,0)*Vector3.forward*16;coupling.ranks[i]=spot;}
            go.SetActive(true);return coupling;
        }
        /// <summary>The contract's three, round a gallery's middle: north-west, north-east, east.</summary>
        static ThawCoupling[] Couplings(Vector3 centre)=>new[]{Coupling(1,330,centre),Coupling(2,30,centre),Coupling(3,90,centre)};

        // ------------------------------------------------------------------ the numbers the chapter is tuned by
        /// <summary>The standing rule (CHAPTER_GUIDE section 0.1): an ordinary hit costs a hero 9 to 13% of the bar, a boss's move 12 to 16. Measured through Health.Receive as a real blow
        /// lands, Plating and all, on all three heroes (Idra, the guard hero, is built with more Plating, so she is the floor and Taren and Sela the ceiling): every ordinary attack of the
        /// chapter at Sync 16 and 19 in the parts it arrives in (Prime), and the Marshal's blows at 17 and 18, where it is met, in Prime and in the Tempered frame Hask sells. Its cold
        /// phase's +18% stays under a fifth. Every chapter 6 zone is at threat 1, every capsule is at least twice its radius tall, and no guardian's arc reaches 180 degrees.</summary>
        [UnityTest] public IEnumerator EveryOrdinaryHitTakesANinthToAnEighthOfAnyHerosBarAndTheMarshalsBlowsABossesShare()
        {
            yield return Zone("Arena_Ground");Floor();
            var source=Enemy("FrostMotes",At(40,.05f,40)).Health;yield return Frames(2);
            var ordinary=new[]{"MusterTender","CurrentSkiff","RimeStalker","FrostMotes","ThawedGuard","MusterExcavator","BridleHound"};
            var attacks=ordinary.SelectMany(id=>Def(id).attacks.Select(a=>(id,what:a.name,damage:a.damage,type:a.type))).ToList();
            var marshal=new (string what,float damage)[]{("its spear-butt",Def("HostMarshal").attacks[0].damage),("Spear Lunge",BossController.MarshalTune.LungeDamage),
                ("Shield Bash",BossController.MarshalTune.BashDamage),("Cracking Sweep",BossController.MarshalTune.SweepDamage),("its cracks",BossController.MarshalTune.CrackDamage)};
            var failures=new List<string>();var prime=GameCatalog.Find<TechPartDef>("FramesT3");var tempered=GameCatalog.Find<TechPartDef>("FramesT4");
            Assert.IsNotNull(prime,"fixture: the Prime frame the chapter arrives in");Assert.IsNotNull(tempered,"fixture: Hask's Tempered frame");
            foreach(var pair in new[]{new[]{"Taren","Idra"},new[]{"Sela","Idra"}})
            {
                yield return Deploy(pair);
                foreach(bool hasks in new[]{false,true})
                {
                    var frame=hasks?tempered:prime;var inv=new Inventory(State);foreach(var member in State.party)inv.Equip(member,inv.GivePart(frame),frame);PartyController.Current.RefreshStats();yield return null;
                    foreach(int sync in new[]{16,17,18,19})
                    {
                        yield return Level(sync);
                        foreach(var hero in PartyController.Current.members)
                        {
                            if(pair[0]=="Sela"&&hero.character=="Idra")continue;
                            float bar=hero.Health.maximum;
                            float Share(float damage,DamageType type){hero.Health.integrity=bar;hero.Health.InvulnerableUntil=0;return hero.Health.Receive(new DamagePacket{source=source,amount=damage,type=type,weight=HitWeight.Medium})/bar;}
                            string parts=hasks?"Tempered":"Prime";
                            if(sync==16||sync==19)foreach(var (id,what,damage,type) in attacks)
                            {
                                float share=Share(damage,type);string line=$"HOARFELL_HIT {hero.character} sync={sync} {parts} plating={hero.plating:0} {id} {what} {damage:0} share={share:P1}";TestContext.WriteLine(line);
                                // In Hask's Tempered frame the guard hero at 19 is a hair under the band; the band is held in the parts the chapter arrives in.
                                if(!hasks&&(share<.09f||share>.13f))failures.Add(line+": the plan asks 9 to 13%");
                            }
                            if(sync==17||sync==18)foreach(var (what,damage) in marshal)
                            {
                                float share=Share(damage,DamageType.Kinetic),cold=Share(damage*1.18f,DamageType.Kinetic);
                                string line=$"HOARFELL_BOSS_HIT {hero.character} sync={sync} {parts} {what} {damage:0} share={share:P1} cold={cold:P1}";TestContext.WriteLine(line);
                                if(share<.12f||share>.16f)failures.Add(line+": a boss's move is 12 to 16% of the bar");
                                if(cold>.2f)failures.Add(line+": in its cold phase no blow takes a fifth of the bar");
                            }
                        }
                    }
                }
            }
            foreach(string zone in new[]{"AuroraShear","Lastlight","FrozenHost"})
            {var z=GameCatalog.Find<ZoneDef>(zone);if(z!=null&&!Mathf.Approximately(z.enemyThreat,1))failures.Add($"{zone} is at threat {z.enemyThreat}: chapter 6's damage is authored at its real weight, at 1");}
            foreach(string id in ordinary.Append("HostMarshal"))
            {
                var def=Def(id);float round=def.boss?BossController.CapsuleRadius(id):def.capsuleRadius;
                if(def.capsuleHeight<round*2)failures.Add($"{id}'s capsule is {def.capsuleHeight} m tall on a {round} m radius: Unity stretches it and the body floats");
                if(def.behaviour==EnemyBehaviourKind.Guardian&&def.tune.arc>=180)failures.Add($"{id}'s guarded arc is {def.tune.arc}: a flank blow must land whole");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ the Aurora Shear: a fight in a current
        /// <summary>What one Shear fight did (HOARFELL_SHEAR): the wing's blows that landed on the lead and on the partner, the bites of a tender's burning wake on the lead, the least
        /// and the most share of a Sync 16 bar one blow took, how far off the wing was on average and at most twelve seconds in, and how far the lead was carried along the lane.</summary>
        sealed class Bout{public int blows,bites,partnerBlows;public float least=1,most,mean=-1,farthest=-1,carried;}
        const float ShearBout=24,InReachAt=12,GivesUpAt=35;
        /// <summary>The fast lane's fight as a ship riding the lane meets it (Fast_0 as HoarfellBuilder sets it: a tender and two skiffs, three metres apart round the fight's middle,
        /// seventeen metres ahead of where the ship comes into its box): the lead in the middle of the lane facing the way it runs, the partner five metres behind and four to starboard.
        /// <paramref name="flowing"/> false switches the fast lane off: the same fight in still space. With <paramref name="answering"/> the lead rolls out of any marking of theirs it is
        /// inside as its tell runs out (the replay driver's rule in flight); otherwise neither ship touches the stick.</summary>
        IEnumerator Meet(Lattice.World.AuroraCurrent fast,bool flowing,bool answering,System.Action<Bout> done)
        {
            var (lead,partner)=Party();var at=Vector3.Lerp(fast.path[0],fast.path[1],.3f);at.y=1;var flow=fast.Flow(at,out _,out _).normalized;var starboard=Vector3.Cross(Vector3.up,flow);
            fast.enabled=flowing;yield return Frames(2);
            Put(lead,at);Put(partner,at-flow*5+starboard*4);
            foreach(var ship in new[]{lead,partner}){(ship.motor as FlightMotor).KeepFacing(flow);ship.thrust=100;}
            var ahead=at+flow*17;
            var wing=new[]{Enemy("MusterTender",ahead,false),Enemy("CurrentSkiff",ahead+starboard*3,false),Enemy("CurrentSkiff",ahead-starboard*3,false)};
            var bout=new Bout();float bar=Levels.MaxIntegrity(16);
            void Struck(CombatActor who,DamagePacket packet,float amount)
            {
                if(amount<=0||packet.source==null||packet.source.friendly)return;
                if(packet.weight==HitWeight.Tick){if(who==lead)bout.bites++;return;}
                if(who!=lead){bout.partnerBlows++;return;}
                bout.blows++;bout.least=Mathf.Min(bout.least,amount/bar);bout.most=Mathf.Max(bout.most,amount/bar);
            }
            System.Action<Health,DamagePacket,float> onLead=(_,p,a)=>Struck(lead,p,a),onPartner=(_,p,a)=>Struck(partner,p,a);
            lead.Health.Damaged+=onLead;partner.Health.Damaged+=onPartner;
            try
            {
                var start=lead.transform.position;float began=Time.time;
                for(float end=Time.unscaledTime+ShearBout*2+5;Time.time<began+ShearBout&&Time.unscaledTime<end;)
                {
                    // The brake held and the stick still, as the replay driver holds them through a chase: a roll then doesn't coast the ship off at twelve metres a second.
                    foreach(var ship in new[]{lead,partner})if(ship.Health.Alive)ship.motor.Move(Vector2.zero,false,true);
                    if(answering)
                        foreach(var marking in Telegraph.Live)
                        {
                            if(marking==null||marking.Released||!(marking.owner is EnemyBrain)||marking.Remaining>.3f*Time.timeScale||!marking.Contains(lead.transform.position))continue;
                            var way=marking.WayOut(lead.transform.position);way.y=0;lead.Dodge(way);break;
                        }
                    if(bout.mean<0&&Time.time>=began+InReachAt)
                    {
                        var gaps=wing.Where(e=>e!=null&&e.Health.Alive).Select(e=>Flat(e.transform.position,lead.transform.position)).ToArray();
                        bout.mean=gaps.Length>0?gaps.Average():0;bout.farthest=gaps.Length>0?gaps.Max():0;
                    }
                    yield return null;
                }
                bout.carried=Vector3.Dot(lead.transform.position-start,flow);
            }
            finally{lead.Health.Damaged-=onLead;partner.Health.Damaged-=onPartner;}
            foreach(var enemy in wing)if(enemy!=null){enemy.Passive=true;Object.Destroy(enemy.gameObject);}
            LingeringArea.DismissAll();foreach(var marking in Telegraph.Live.ToArray())if(marking!=null)marking.Clear();
            fast.enabled=true;yield return Frames(3);
            done(bout);
        }
        /// <summary>The first player run of the Shear (November 14, Builds/quality/nov13b/s32-rejected-current-a and -b): its wings launched their tells at a ship the fast lane carried
        /// at 7 to 9 m/s, every circle and lane landed where the ship had been, and in two runs not one blow reached the party; a 30 s chase step was carried 244 m, through two fights
        /// and into a third, the first two left standing behind it. A fight in a current has to be a fight: a ship that rides it and does nothing is hit about as often as one sitting
        /// in still space and by blows of the chapter's weight; one that answers each tell (rolls out of what is marked as it runs out) is not; the wing is still about the ship twelve
        /// seconds in; and the current still carries the ship at its pace, fight or no fight. Seen failing on the first run's rule (only the ships carried, behind a switch since
        /// removed; lead-own-2, November 14): 1 blow in 24 s against 15 in still space, the wing 23 m off on average and 36 at most. And on each body carried by the current where it
        /// was, not with the fight: 8 blows, the skiffs at the lane's eased edge drifting out of their own lanes.</summary>
        [UnityTest] public IEnumerator AShearFightRidesTheCurrentWithTheShipAndHitsOneThatDoesNothingButNotOneThatAnswersItsTells()
        {
            yield return Zone("AuroraShear");
            foreach(var volume in Object.FindObjectsByType<Lattice.World.EncounterVolume>(FindObjectsSortMode.None))volume.enabled=false;
            yield return Deploy("Taren","Sela");
            var prime=GameCatalog.Find<TechPartDef>("FramesT3");Assert.IsNotNull(prime,"fixture: the Prime frame the chapter arrives in");
            var inv=new Inventory(State);foreach(var member in State.party)inv.Equip(member,inv.GivePart(prime),prime);PartyController.Current.RefreshStats();yield return null;
            yield return Level(16);
            var fast=Object.FindObjectsByType<Lattice.World.AuroraCurrent>(FindObjectsSortMode.None).SingleOrDefault(c=>c.name=="The fast lane");Assert.IsNotNull(fast,"fixture: the Shear's fast lane");
            Bout still=null,riding=null,answered=null;
            yield return Meet(fast,false,false,b=>still=b);
            yield return Meet(fast,true,false,b=>riding=b);
            yield return Meet(fast,true,true,b=>answered=b);
            foreach(var (what,b) in new[]{("in still space",still),("riding the fast lane",riding),("riding it, rolling out of every tell",answered)})
            {
                string line=$"HOARFELL_SHEAR {what}: {b.blows} blows on the lead in {ShearBout:0} s ({b.blows*60/ShearBout:0.0} a minute), each {(b.blows>0?b.least:0):P1} to {b.most:P1} of its Sync 16 bar; "
                    +$"{b.bites} bites of a burning wake; {b.partnerBlows} blows on the partner; the wing {b.mean:0.0} m off on average, {b.farthest:0.0} at most, at {InReachAt:0} s; the lead carried {b.carried:0} m";
                TestContext.WriteLine(line);Debug.Log(line);
            }
            var failures=new List<string>();
            if(still.blows<4)failures.Add($"fixture: in still space the wing landed only {still.blows} blows on a ship that did nothing for {ShearBout} s");
            if(riding.blows<Mathf.Max(4,still.blows*.6f))failures.Add($"riding the fast lane a ship that does nothing took {riding.blows} blows in {ShearBout} s, against {still.blows} in still space: the current carries it out from under the tells");
            foreach(var (what,b) in new[]{("in still space",still),("riding",riding)})
                if(b.blows>0&&(b.least<.09f||b.most>.13f))failures.Add($"{what} a blow took {b.least:P1} to {b.most:P1} of a Sync 16 bar: an ordinary hit is 9 to 13%");
            if(answered.blows>Mathf.Max(1,riding.blows/4))failures.Add($"riding, a ship that rolls out of every tell as it runs out still took {answered.blows} blows (doing nothing, {riding.blows})");
            if(riding.mean>20||riding.farthest>GivesUpAt)failures.Add($"{InReachAt} s into a fight in the fast lane the wing was {riding.mean:0} m from the ship on average and {riding.farthest:0} at most: left behind (an enemy gives up at {GivesUpAt})");
            if(riding.carried<ShearBout*fast.push*.6f)failures.Add($"the fight held the ship: it rode {riding.carried:0} m of the fast lane in {ShearBout} s, where the lane carries {ShearBout*fast.push:0}");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ the families the chapter added or changed
        [UnityTest] public IEnumerator ThreeThawedGuardsStandAsOneRankThrustTogetherAndBreakingTheMiddleDropsEveryShield()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,7));
            const string kind="ThawedGuard";
            var guards=new[]{Enemy(kind,At(-2.3f,.05f,0)),Enemy(kind,At(0,.05f,0)),Enemy(kind,At(2.3f,.05f,0))};yield return Frames(3);
            foreach(var guard in guards)guard.Passive=false;
            var told=new float[3];
            yield return Until(()=>{for(int i=0;i<3;i++)if(told[i]==0&&guards[i].Telegraphing)told[i]=Time.time;return told.All(t=>t>0);},6,hero);
            var ranked=guards.Select(g=>g.Behaviour as EnemyGuardian).ToArray();
            Assert.IsTrue(ranked.All(g=>g!=null&&g.RankSize==3),"three of them woken together don't stand as one rank of three");
            Assert.IsTrue(told.All(t=>t>0),"the rank never thrust at a hero seven metres before it");
            Assert.Less(told.Max()-told.Min(),.2f,"the rank's spears aren't thrust together");
            var lines=guards.Select(g=>g.AttackMark).ToArray();
            Assert.IsTrue(lines.All(l=>l!=null&&l.Shape==Telegraph.Kind.Line),"a rank's thrust isn't three marked lines");
            Assert.IsTrue(lines.All(l=>Vector3.Angle(l.Forward,lines[0].Forward)<6),"a rank's spears aren't thrust down one heading: it doesn't face as one");
            Assert.IsTrue(Said("RANK_THRUST ThawedGuard 3"));
            var middle=guards.First(g=>(g.Behaviour as EnemyGuardian).InTheMiddle);var end=guards.First(g=>g!=middle);
            Assert.AreEqual(guards[1],middle,"the one in the middle of the rank isn't its middle");
            // Its spears come out of the floor and its shields go up again.
            yield return Until(()=>guards.All(g=>!g.Telegraphing),3,hero);yield return Wait(1.7f,hero);
            Assert.AreEqual(20,Hit(end.Health,hero),1,"a blow on a rank's shield from before it isn't cut to a fifth");
            // Break the middle one and the whole rank's shields are down while it reels.
            Hit(middle.Health,hero,1,100000);Assert.IsTrue(middle.Health.Broken,"fixture: the middle one is broken");
            Assert.AreEqual(100,Hit(end.Health,hero),1,"with the middle of its rank broken, the end of the rank still has its shield up");
            Assert.IsFalse((end.Behaviour as EnemyGuardian).Guarding,"with its middle broken a rank's plate is still drawn up");
            // Kill it and the rank comes apart: the others stand open a moment, then fight alone.
            Kill(middle.Health,hero);yield return Frames(2);
            Assert.IsTrue(Said("RANK_BROKEN ThawedGuard"),"its middle fell and the rank held");
            foreach(var left in guards.Where(g=>g!=middle))
            {
                Assert.IsTrue(left.Health.Broken,"the rank came apart and its others aren't open");Assert.That(left.Health.BrokenUntil-Time.time,Is.InRange(1.3f,1.65f),"a broken rank's others are open 1.6 s");
                Assert.AreEqual(0,(left.Behaviour as EnemyGuardian).RankSize,"a guard of a broken rank still stands in it");
            }
            Assert.AreEqual(150,Hit(end.Health,hero),1,"a blow on a guard of a broken rank isn't an opening's half as much again");
        }
        [UnityTest] public IEnumerator AnExcavatorsLanceCrossesItsFanStrikingEachHeroAsItPassesAndThenVents()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();
            var dig=Enemy("MusterExcavator",At(0,.05f,0));yield return Frames(3);
            // Either side of its heading and close together, so wherever it has turned and walked to by the time it marks, both are in a 100-degree fan.
            Put(hero,At(-1.6f,.05f,7.5f));Put(partner,At(1.6f,.05f,7.5f));
            var struck=new Dictionary<CombatActor,float>();
            void Note(CombatActor who,DamagePacket packet,float amount){if(packet.source==dig.Health&&amount>0&&!struck.ContainsKey(who))struck[who]=Time.time;}
            System.Action<Health,DamagePacket,float> leadHit=(_,p,a)=>Note(hero,p,a),partnerHit=(_,p,a)=>Note(partner,p,a);
            hero.Health.Damaged+=leadHit;partner.Health.Damaged+=partnerHit;
            try
            {
                dig.Passive=false;
                yield return Until(()=>dig.Telegraphing,5,hero,partner);Assert.IsTrue(dig.Telegraphing,"fixture: it uses its lance on heroes past its slam");
                var fan=dig.AttackMark;
                Assert.AreEqual(Telegraph.Kind.Cone,fan.Shape,"its lance isn't marked as a fan");Assert.AreEqual(100,fan.Angle,1,"its lance's fan is 100 degrees");Assert.AreEqual(11,fan.Radius,.1f);
                Assert.IsTrue(fan.Contains(hero.transform.position)&&fan.Contains(partner.transform.position),"fixture: both stand in its fan");
                // A body telling its move neither turns nor steps, so the beam reaches them as far apart in time as they stand apart in angle.
                float Bearing(CombatActor who)=>Vector3.SignedAngle(fan.Forward,Vector3.ProjectOnPlane(who.transform.position-fan.Origin,Vector3.up),Vector3.up);
                float expected=Mathf.Abs(Bearing(hero)-Bearing(partner))/fan.Angle*EnemyGuardian.SweepSeconds;
                yield return Until(()=>Said("GUARD_DOWN MusterExcavator"),4,hero,partner);
                Assert.IsTrue(Said("GUARD_SWEEP MusterExcavator"),"its beam never crossed the fan");
                Assert.AreEqual(2,struck.Count,"its beam crossed the fan and missed a hero standing in it");
                float apart=Mathf.Abs(struck[hero]-struck[partner]);
                Assert.Greater(expected,.12f,"fixture: the two stand far enough apart in its fan to tell a sweep from a blow");
                Assert.That(apart,Is.InRange(expected-.08f,expected+.08f),$"the two heroes {expected:0.00} s of beam apart were struck {apart:0.00} s apart: the beam crosses the fan, it doesn't land at once");
            }
            finally{hero.Health.Damaged-=leadHit;partner.Health.Damaged-=partnerHit;}
            // The lance vents: its shield is down and a blow from before it lands whole.
            Assert.IsFalse((dig.Behaviour as EnemyGuardian).Guarding,"venting, its plate is still up");
            Assert.AreEqual(100,Hit(dig.Health,hero),1,"venting, a blow from before it is still cut by its plate");
        }
        [UnityTest] public IEnumerator ABridleHoundPinsWhoItLandsOnShattersCaughtInTheAirAndIsThrownOffABracedHero()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,5));
            var hound=Enemy("BridleHound",At(0,.05f,0));yield return Frames(3);var leaper=hound.Behaviour as EnemyLeaper;Assert.IsNotNull(leaper,"a Bridle-hound isn't a leaper");
            // Landed on: struck and pinned, and a roll frees him.
            hound.Passive=false;
            yield return Until(()=>hound.Telegraphing,4,hero);Assert.IsTrue(hound.Telegraphing,"it never sprang at a hero five metres off");
            var lane=hound.AttackMark;Assert.AreEqual(Telegraph.Kind.Line,lane.Shape,"its spring isn't marked as a lane");Assert.IsTrue(lane.Contains(hero.transform.position),"its lane isn't laid through him");
            yield return Until(()=>Said("LEAPER_PIN"),3,hero);
            Assert.IsTrue(Said("LEAPER_PIN BridleHound"),"it landed on a hero standing in its lane and didn't pin him");Assert.Less(hero.Health.integrity,5000,"it pinned him without striking");
            Assert.AreEqual(hero,leaper.Pinned);yield return null;Assert.AreEqual(Def("BridleHound").tune.pace,hero.Health.Pace,.01f,"pinned, he still walks at his own pace");
            yield return Until(()=>hero.Dodge(Vector3.right),1,hero);yield return Wait(.3f,hero);
            Assert.IsTrue(Said("LEAPER_FREED BridleHound rolled"),"a roll didn't free him");
            // Caught in the air: any blow, however small, and it shatters. The same blow on the ground leaves it standing.
            hound.Passive=true;Object.Destroy(hound.gameObject);yield return Frames(2);Put(hero,At(0,.05f,5));
            var second=Enemy("BridleHound",At(0,.05f,0));yield return Frames(3);var secondLeaper=second.Behaviour as EnemyLeaper;
            Hit(second.Health,hero,10);yield return Frames(2);Assert.IsTrue(second.Health.Alive,"a small blow on a hound on the ground shattered it");
            second.Passive=false;yield return Until(()=>secondLeaper.Airborne,5,hero);Assert.IsTrue(secondLeaper.Airborne,"fixture: it springs");
            Hit(second.Health,hero,10);yield return Frames(2);
            Assert.IsTrue(Said("LEAPER_SHATTERED BridleHound"),"a blow caught it in the air and it didn't shatter");Assert.IsFalse(second.Health.Alive,"shattered, it still stands");
            // Braced for it: it is thrown off, open, and pins nobody.
            yield return Wait(.5f,hero);log.Clear();Put(hero,At(0,.05f,5));(hero.motor as GroundMotor).KeepFacing(Vector3.back);
            var third=Enemy("BridleHound",At(0,.05f,0));yield return Frames(3);third.Passive=false;
            for(float end=Time.time+5;!Said("LEAPER_THROWN")&&!Said("LEAPER_PIN")&&Time.time<end;){hero.Guard(true);hero.motor.Move(Vector2.zero,false,false);yield return null;}
            hero.Guard(false);
            Assert.IsTrue(Said("LEAPER_THROWN BridleHound"),"it landed on a braced hero and wasn't thrown off: "+string.Join(", ",log));Assert.IsFalse(Said("LEAPER_PIN"),"it pinned a braced hero");
            Assert.IsTrue(third.Health.Broken,"thrown off, it isn't open");Assert.AreEqual(1,hero.Health.Pace,.01f,"a braced hero is held as if pinned");
        }

        // ------------------------------------------------------------------ the Host Marshal
        [UnityTest] public IEnumerator TheMarshalsCouplingsAreShutUntilItWakesEachPutsItsRankBackToSleepAndTheThirdIsTheShift()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-6));
            var couplings=Couplings(At(0,0,0));yield return Frames(2);
            // Before the fight they answer to nobody's blow.
            foreach(var coupling in couplings){Assert.IsFalse(coupling.Health.Targetable,"a coupling can be targeted before its boss is awake");Assert.AreEqual(0,Kill(coupling.Health,hero),.01f,"a coupling can be broken before the fight");}
            var marshal=Enemy("HostMarshal",At(0,.1f,0));yield return Frames(3);
            foreach(var coupling in couplings)Assert.IsTrue(coupling.Health.Targetable,"awake, its couplings still can't be hit");
            var boss=marshal.GetComponent<BossController>();var body=marshal.Health;float whole=body.maximum;
            foreach(var coupling in couplings)
            {
                Assert.AreEqual(whole*BossController.MarshalTune.CouplingIntegrity,coupling.Health.maximum,1,"a coupling's integrity isn't the fight's to say");
                Assert.AreEqual(DamageType.Kinetic,coupling.Health.weakness,"a coupling doesn't break to a blow");Assert.AreEqual(DamageType.Plasma,coupling.Health.resistance,"a coupling doesn't shrug off heat");
            }
            StringAssert.Contains("BREAK A COUPLING",boss.StatusText,"its bar doesn't say what to break");StringAssert.Contains("(0/3)",boss.FightGoal(""),"the objective doesn't count the couplings");
            // Its call: the rank of the coupling nearest the lead steps out where the wall marks them.
            Put(hero,At(8,.05f,12));yield return null;
            Assert.IsTrue(boss.Perform("Rally"),"fixture: its call");yield return null;
            var rings=Marks(boss);Assert.AreEqual(3,rings.Length,"its call marks no places for the rank to step out");
            Assert.IsTrue(rings.All(Faint),"where a rank steps out is painted as a danger: nothing strikes there");
            foreach(var spot in couplings[1].ranks)Assert.IsTrue(rings.Any(r=>Flat(r.Origin,spot.position)<.3f),"its call didn't mark the nearest coupling's rank");
            yield return Until(()=>!boss.Busy,4,hero);
            var rank=Guards();Assert.AreEqual(3,rank.Length,"its call brought no rank out of the wall");Assert.IsTrue(Said("MARSHAL_RALLY 2"));Assert.IsTrue(couplings[1].rankWoken);
            foreach(var guard in rank)Assert.Less(couplings[1].ranks.Min(s=>Flat(s.position,guard.transform.position)),2,"a soldier stepped out somewhere other than its rank's place");
            // That coupling broken: its third of the wall and its rank go back to sleep, where they stand, as ice that is cover; nobody is paid for them; a sixth of its bar goes.
            int xp=State.party[0].xp;float bar=body.integrity;
            Kill(couplings[1].Health,hero);yield return Frames(2);
            Assert.IsTrue(Said("COUPLING_BROKEN 2"));Assert.IsTrue(Set("test.coupling2"),"a broken coupling isn't kept in the save");
            Assert.IsTrue(couplings[1].refrozen.activeSelf&&!couplings[1].thawing.activeSelf,"its third of the wall didn't refreeze");
            Assert.IsEmpty(Guards(),"its rank is still up after its coupling broke");Assert.AreEqual(3,Statues().Length,"its rank didn't stay where it stood, as ice");
            // Cover, those out of a hero's reach (the lead stands beside the middle place of this rank, as the call was made toward him).
            var solid=Statues().Where(g=>g.GetComponent<BoxCollider>()!=null).ToArray();Assert.IsNotEmpty(solid,"no soldier put back to sleep out of a hero's reach is solid");
            var statue=solid[0].transform.position;Assert.IsFalse(CombatCover.Clear(statue+new Vector3(0,1,-3),statue+new Vector3(0,1,3)),"a soldier put back to sleep isn't cover");
            Assert.AreEqual(xp,State.party[0].xp,"the party was paid for soldiers put back to sleep");
            Assert.AreEqual(bar-whole*BossController.MarshalTune.CouplingBar,body.integrity,2,"a coupling should take a sixth of its bar");
            StringAssert.Contains("(1/3)",boss.FightGoal(""));
            // The second: another sixth, and still not over half. With a coupling running the rig keeps it warm: a blow on it lands at a share and reads as resisted; an opening lands whole.
            Kill(couplings[0].Health,hero);yield return Frames(2);Assert.AreEqual(whole*4/6f,body.integrity,3,"the second coupling should take another sixth");
            Assert.IsTrue(boss.Warm,"with a coupling running it isn't warm");
            var kinds=new List<HitKind>();System.Action<Health,float,HitKind> seen=(victim,_,kind)=>{if(victim==body)kinds.Add(kind);};Health.DamageNumber+=seen;
            try{Assert.AreEqual(100*BossController.MarshalTune.Shell,Hit(body,hero),1,"with a coupling running a blow on it isn't cut to its share");Assert.AreEqual(HitKind.Resisted,kinds.Last(),"a blow its warmth turned doesn't read as resisted");}
            finally{Health.DamageNumber-=seen;}
            body.Expose(1);Assert.AreEqual(150,Hit(body,hero),1,"open, a blow on it is still cut by its warmth");yield return Until(()=>!body.Broken,2,hero);
            // The third carries it over, and that is the shift: the wall sleeps and the frost comes in.
            marshal.Passive=false;Kill(couplings[2].Health,hero);yield return Frames(2);
            Assert.Less(body.integrity,whole*.5f,"the last coupling doesn't carry it over half");
            yield return Until(()=>Said("BOSS_SHIFT HostMarshal 2"),9,hero);Assert.IsTrue(Said("BOSS_SHIFT HostMarshal 2"),"with three couplings broken it never shifted");
            Assert.IsTrue(Said("MARSHAL_COLD"));var ring=FrostRing.Current;Assert.IsNotNull(ring,"its shift brings no frost in");
            Assert.Less(Flat(ring.transform.position,At(0,0,0)),.3f,"the frost doesn't close on the gallery's middle");
            StringAssert.Contains("Outlast",boss.FightGoal(""));Assert.IsFalse(boss.Warm,"cold, a blow on it is still cut");
            marshal.Passive=true;
        }
        [UnityTest] public IEnumerator RanksThatFreezeRoundAHeroLeaveHimAWayOut()
        {
            // The first player run: when the wall went back to sleep the three ranks still out froze where they were fighting the lead, beside a thaw rig, and their boxes
            // and the rig's made a cell. He rolled against its walls for the last sixty-five seconds of the fight and never left it (Builds/quality/nov14-recon4/r36, from 103 s).
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            var centre=At(0,.05f,20);var round=new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left};
            // The old way first: four ranks frozen at arm's length, each a solid box, are a cell.
            Put(hero,centre);var cell=new System.Collections.Generic.List<GameObject>();
            foreach(var offset in round)
            {
                var guard=Enemy("ThawedGuard",centre+offset*.95f);yield return Frames(2);var shape=guard.GetComponent<CharacterController>();float radius=shape.radius,height=shape.height;
                var statue=BossController.Refreeze(guard);Assert.IsNull(statue.GetComponent<Collider>(),"a rank frozen at arm's length of a hero is still a wall");
                var box=statue.AddComponent<BoxCollider>();box.size=new Vector3(radius*1.6f,height,radius*1.6f);box.center=Vector3.up*height*.5f;cell.Add(statue);
            }
            Physics.SyncTransforms();yield return Frames(2);
            float held=0;yield return Walk(hero,new Vector2(.7f,.7f),1.5f,d=>held=d);
            Assert.Less(held,1.2f,$"fixture: four solid ranks at arm's length didn't hold a hero ({held:0.0} m in a second and a half), so the old way isn't the fault this is about");
            foreach(var statue in cell)Object.Destroy(statue);yield return Frames(2);
            // As the game freezes them now.
            Put(hero,centre);
            foreach(var offset in round){var guard=Enemy("ThawedGuard",centre+offset*.95f);yield return Frames(2);BossController.Refreeze(guard);}
            Physics.SyncTransforms();yield return Frames(2);
            float free=0;yield return Walk(hero,new Vector2(.7f,.7f),1.5f,d=>free=d);
            Assert.Greater(free,3,$"with four ranks frozen round him the lead walked {free:0.0} m in a second and a half: he is walled in");
            // One that freezes across the room is cover, as before.
            var far=Enemy("ThawedGuard",centre+Vector3.forward*8+Vector3.right*20);yield return Frames(2);var cover=BossController.Refreeze(far);
            Assert.IsNotNull(cover.GetComponent<BoxCollider>(),"a rank frozen across the room is no longer cover");
        }
        static IEnumerator Walk(CombatActor hero,Vector2 way,float seconds,System.Action<float> done)
        {
            var start=hero.transform.position;way.Normalize();
            for(float until=Time.time+seconds;Time.time<until;){hero.motor.Move(way,false,false);yield return null;}
            done(Flat(start,hero.transform.position));
        }
        /// <summary>The replay driver broke the Marshal seven times in each of its first two fights (Builds/quality/nov14-recon4 and -recon5, r36), and a break lets a blow past
        /// the warm shell whole and half again: it beat the Marshal past half with one coupling of three broken, and the fight was over in 94.8 s. Warm, the rig's heat holds it
        /// together: no blow breaks it and its meter doesn't fill; the openings its own moves give (a shield wide, a spear caught) still open it; cold, it breaks as anything does.</summary>
        [UnityTest] public IEnumerator WarmTheMarshalCannotBeBrokenAndColdItBreaksAsAnythingDoes()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-8));
            // The check first, on the body with no coupling to keep it warm: a blow that fills its meter breaks it.
            var marshal=Enemy("HostMarshal",At(0,.1f,0));yield return Frames(3);var boss=marshal.GetComponent<BossController>();var body=marshal.Health;
            Assert.IsFalse(boss.Warm,"fixture: with no coupling running it is warm");
            Hit(body,hero,1,1e6f);Assert.IsTrue(body.Broken,"fixture: a blow that fills its meter doesn't break it even cold");
            marshal.Passive=true;Object.Destroy(marshal.gameObject);yield return Frames(3);
            var couplings=Couplings(At(0,0,0));yield return Frames(2);
            marshal=Enemy("HostMarshal",At(0,.1f,0));yield return Frames(3);boss=marshal.GetComponent<BossController>();body=marshal.Health;
            Assert.IsTrue(boss.Warm,"fixture: with its couplings running it isn't warm");
            Hit(body,hero,1,1e6f);yield return null;
            Assert.IsFalse(body.Broken,"warm, a blow that would break anything broke it");Assert.AreEqual(0,body.BreakMeter,.01f,"warm, its break meter fills, and would pay out the moment it went cold");
            body.Expose(1);Assert.IsTrue(body.Broken,"warm, an opening its own move gives doesn't open it");yield return Until(()=>!body.Broken,2,hero);
            // Cold: all three broken, the shift, and then it breaks as anything does.
            marshal.Passive=false;foreach(var coupling in couplings)Kill(coupling.Health,hero);
            yield return Until(()=>Said("BOSS_SHIFT HostMarshal 2"),9,hero);Assert.IsTrue(Said("BOSS_SHIFT HostMarshal 2"),"fixture: with its couplings broken it never shifted");
            marshal.Passive=true;yield return Until(()=>!boss.Busy&&!boss.Shifting&&!body.Broken,9,hero);
            Assert.IsFalse(boss.Warm,"fixture: shifted, it is still warm");
            Hit(body,hero,1,1e6f);Assert.IsTrue(body.Broken,"cold, a blow that fills its meter doesn't break it");
        }
        [UnityTest] public IEnumerator ItsLungeEndsAtTheFirstIceInItsLaneAndItsSpearStaysCaughtThereLongerThanInTheFloor()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,12));
            var marshal=Enemy("HostMarshal",At(0,.1f,0));yield return Frames(3);var boss=marshal.GetComponent<BossController>();var body=marshal.Health;
            // A soldier put back to sleep between them.
            var guard=Enemy("ThawedGuard",At(0,.05f,7.5f));yield return Frames(3);var statue=BossController.Refreeze(guard);yield return Frames(2);
            Assert.IsTrue(boss.Perform("SpearLunge"),"fixture: its lunge");yield return null;
            var lane=Marks(boss).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Line);Assert.IsNotNull(lane,"its lunge marks no lane");
            Assert.Less(lane.Length,9,"its lane runs on through the ice");Assert.IsFalse(lane.Contains(hero.transform.position),"fixture: he stands behind the ice");
            yield return Until(()=>Said("MARSHAL_LODGED"),4,hero);
            Assert.IsTrue(Said("MARSHAL_LODGED ice"),"its spear didn't lodge in the ice it ran into: "+string.Join(", ",log));Assert.AreEqual(5000,hero.Health.integrity,.01f,"its lunge went through the ice to him");
            Assert.IsTrue(body.Broken,"lodged, it isn't open");Assert.That(body.BrokenUntil-Time.time,Is.InRange(2.05f,2.55f),"its spear is caught in ice for 2.5 s");
            StringAssert.Contains("SPEAR",boss.StatusText,"its bar doesn't say its spear is caught");
            Assert.AreEqual(150,Hit(body,hero),1,"a hit on it with its spear caught isn't an opening's half as much again");
            // In open floor, beyond its reach: the spear goes into the floor, for a shorter while.
            yield return Until(()=>!boss.Busy,4,hero);if(statue!=null)Object.Destroy(statue);yield return Frames(2);
            Stand(marshal,At(0,.1f,0));Put(hero,At(0,.05f,19));log.Clear();
            Assert.IsTrue(boss.Perform("SpearLunge"),"fixture: its lunge again");
            yield return Until(()=>Said("MARSHAL_LODGED"),4,hero);
            Assert.IsTrue(Said("MARSHAL_LODGED floor"),"a lunge at nothing doesn't plant its spear in the floor");Assert.That(body.BrokenUntil-Time.time,Is.InRange(1.8f,2.25f),"planted in the floor it is open 2.2 s");
        }
        [UnityTest] public IEnumerator InTheColdTheFrostClosesItIsSlowerItsSweepCracksTheFloorAndItsHeatFailsIt()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,5));
            var marshal=Enemy("HostMarshal",At(0,.1f,0));yield return Frames(3);var boss=marshal.GetComponent<BossController>();var body=marshal.Health;
            Hit(body,hero,1);marshal.Passive=false;
            body.integrity=body.maximum*.45f;
            yield return Until(()=>Said("BOSS_SHIFT HostMarshal 2"),9,hero);marshal.Passive=true;
            Assert.IsTrue(Said("MARSHAL_COLD"),"under half it never went cold");var ring=FrostRing.Current;Assert.IsNotNull(ring,"the cold brings no frost in");
            Assert.Less(Flat(ring.transform.position,At(0,0,0)),.3f,"with no couplings the frost doesn't close on where it woke");
            var band=Telegraph.Live.FirstOrDefault(t=>t!=null&&t.owner==ring);Assert.IsNotNull(band,"the frost isn't drawn");Assert.IsTrue(Faint(band),"the frost is painted as a danger: it strikes nobody");
            float edge=ring.Edge;yield return Wait(2,hero);Assert.Less(ring.Edge,edge-.15f,"the frost doesn't close in");
            Put(partner,At(17.2f,.05f,0));yield return Frames(3);Assert.IsTrue(ring.Holds(partner.transform.position),"fixture: he stands on the frost");
            Assert.AreEqual(BossController.MarshalTune.RingPace,partner.Health.Pace,.01f,"a hero on the frost isn't slowed");Assert.AreEqual(5000,partner.Health.integrity,.01f,"the frost struck a hero");
            Assert.AreEqual(BossController.MarshalTune.ColdPace,body.Pace,.01f,"cold, it walks at its full pace");
            StringAssert.Contains("HEAT",boss.StatusText,"its bar doesn't say its heat is failing");StringAssert.Contains("Outlast",boss.FightGoal(""));
            // Its sweep: a wide fan, then three cracks out through it, then its heat fails it.
            yield return Until(()=>!boss.Busy&&!boss.Shifting&&!body.Broken,9,hero);yield return Wait(.3f,hero);
            Stand(marshal,At(0,.1f,0));Put(hero,At(0,.05f,5));Put(partner,At(-50,.05f,-50));log.Clear();
            Assert.IsTrue(boss.Perform("CrackingSweep"),"fixture: its sweep");yield return null;
            var fan=Marks(boss).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Cone);Assert.IsNotNull(fan,"its sweep marks no fan");
            Assert.AreEqual(BossController.MarshalTune.SweepAngle,fan.Angle,1);Assert.IsTrue(fan.Contains(hero.transform.position),"fixture: he stands in its fan");
            Telegraph[] cracks=null;yield return Until(()=>(cracks=Marks(boss).Where(t=>t.Shape==Telegraph.Kind.Line).ToArray()).Length==BossController.MarshalTune.Cracks,3,hero);
            Assert.AreEqual(BossController.MarshalTune.Cracks,cracks.Length,"its sweep didn't crack the floor down three marked lines");Assert.Less(hero.Health.integrity,5000,"its sweep missed a hero in its fan");
            Assert.IsTrue(cracks.Any(c=>c.Contains(hero.transform.position)),"no crack runs where its blow fell");
            float before=hero.Health.integrity;yield return Until(()=>Said("MARSHAL_FALTERS"),3,hero);
            Assert.Less(hero.Health.integrity,before,"a crack erupted under a hero standing on it and missed him");
            Assert.IsTrue(body.Broken,"its heat failed and it isn't open");Assert.That(body.BrokenUntil-Time.time,Is.InRange(1.6f,2.05f),"down on one knee it is open 2 s");
            Assert.AreEqual(150,Hit(body,hero),1,"a hit on it down on its knee isn't an opening's half as much again");
        }
        [UnityTest] public IEnumerator PutToSleepTheMarshalKneelsAsIceWithNoWordOverItAndWhatItWokeSleepsWithIt()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-8));
            // Its couplings still running: whatever they do to its bar, a blow that ends the fight ends it (the shared music test puts it down with one).
            var couplings=Couplings(At(0,0,0));yield return Frames(2);
            var marshal=Enemy("HostMarshal",At(0,.1f,0));yield return Frames(3);var boss=marshal.GetComponent<BossController>();var body=marshal.Health;
            Assert.IsTrue(boss.Perform("Rally"),"fixture: its call");yield return Until(()=>!boss.Busy,4,hero);Assert.AreEqual(3,Guards().Length,"fixture: its call brings a rank out of the wall");
            FrostRing.Lay(At(0,0,0),17,9,70,.6f);
            string track=Def("HostMarshal").bossMusicId;Assert.AreEqual(track,MusicDirector.Current.Track,"fixture: its fight plays its own track");
            // How high its head stands over the floor before it falls, to see it kneel: on "It's kneeling" in the first player run it had tipped and sunk too little to be seen.
            var drawn=marshal.GetComponent<DefeatPresentation>().visual;Assert.IsNotNull(drawn,"fixture: it has a drawn body");
            var heads=marshal.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Head").ToArray();
            var head=drawn.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Head");Assert.IsNotNull(head,"fixture: its drawn body's rig has a head");
            // Stood where the camera has it in frame for the stills: south of it and a little west.
            Put(hero,At(-2,.05f,-4));yield return Wait(.5f,hero);float floor=marshal.transform.position.y,standing=head.position.y-floor;var stoodLocal=drawn.localPosition;Still("marshal-standing");
            var spoken=new List<string>();System.Action<string,string> heard=(who,line)=>spoken.Add(who+": "+line);BarkService.Spoken+=heard;
            try{Kill(body,hero);yield return Frames(3);}finally{BarkService.Spoken-=heard;}
            Assert.AreEqual(0,body.integrity,"a blow that ends the fight left it standing while a coupling ran");
            Assert.AreNotEqual(track,MusicDirector.Current.Track,"it is asleep and its track plays on: the zone's own music should come back");
            Assert.IsTrue(couplings.All(c=>!c.Running),"its couplings run on after it sleeps");Assert.IsFalse(couplings.Any(c=>Set(c.defeatedFlag)),"couplings settled by its sleep are counted as broken by the party");
            Assert.IsTrue(Set("bossdown.HostMarshal"),"put to sleep, it isn't counted as beaten");Assert.IsTrue(Said("BOSS_DOWN HostMarshal"));Assert.IsTrue(Said("MARSHAL_ASLEEP"),"it fell and wasn't put to sleep");
            Assert.AreEqual(BossController.Ending.ShutDown,BossController.EndingOf("HostMarshal"),"a construct doing its last order is put to sleep, not destroyed");
            Assert.IsEmpty(spoken,"a bark talks over the scene its sleep is for: "+string.Join(", ",spoken));
            Assert.IsEmpty(Guards(),"the soldiers it called out are still up after it sleeps");Assert.AreEqual(3,Statues().Length,"the soldiers it called out didn't go back to sleep as ice where they stood");
            Assert.IsNull(FrostRing.Current,"the frost outlasts the fight");
            yield return Wait(4.5f,hero);
            Assert.IsTrue(marshal!=null,"put to sleep, its body was cleared away like a beaten one's");
            float knelt=head.position.y-floor;Still("marshal-knelt");TestContext.WriteLine($"HOARFELL_KNEEL its head {standing:0.00} m over the floor standing, {knelt:0.00} m knelt; {heads.Length} heads in the body, this one under {head.parent.name}, the drawn body at {stoodLocal} standing and {drawn.localPosition} knelt; "+string.Join(", ",log.Where(l=>l.StartsWith("MARSHAL_KNELT"))));
            Assert.IsTrue(Said("MARSHAL_KNELT"),"put to sleep, its kneel never finished: "+string.Join(", ",log));
            Assert.Less(knelt,standing*.8f,$"put to sleep it barely moved: its head went from {standing:0.00} m to {knelt:0.00} m over the floor (a kneel takes a fifth of its height at least)");
            Assert.IsTrue(marshal.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"put to sleep, it isn't drawn where it knelt");
            // The gallery loads asleep once it is: a coupling made after its fall is broken, quietly, and its stretch of wall is blue.
            var later=Coupling(1,330,At(0,0,40));yield return Frames(2);
            Assert.IsFalse(later.Running,"after the Marshal sleeps a coupling loads running");Assert.IsTrue(later.refrozen.activeSelf,"after the Marshal sleeps the wall loads thawing");
            Assert.IsFalse(Set(later.defeatedFlag),"a coupling settled by its fall is counted as broken by the party");
        }

        /// <summary>Nothing that fights on foot may come to rest where a hero on the floor can't strike it. On the 17:17 build a Bridle-hound came to rest up on the north-east corner of
        /// Rig three's box (3 m of solid box against the third level's north wall) while the lead, against the rig's flank under it, swung at it for two minutes: it never leapt and never
        /// came down, and the rank's fight never cleared (nov14-recon24 s35). Here a hound of that fight (its home the hounds' pen floor) stands on that corner, doing nothing of its own,
        /// and the lead stands where he stood, swinging at it: it has to be down within his reach and struck within a few seconds (EnemyBrain.KeepToTheFloor).</summary>
        [UnityTest] public IEnumerator AHoundLeftOnTopOfRigThreeComesDownWithinReachOfAHeroOnTheFloor()
        {
            yield return Zone("FrozenHost");var (hero,partner)=Party();
            var rig=GameObject.Find("Rig three");Assert.IsNotNull(rig,"fixture: the Frozen Host has Rig three");
            var box=rig.GetComponentsInChildren<BoxCollider>().FirstOrDefault(b=>b.enabled&&!b.isTrigger);Assert.IsNotNull(box,"fixture: Rig three is solid");
            var frame=box.transform;var half=box.size*.5f;
            var corners=new[]{(-1f,-1f),(1f,-1f),(-1f,1f),(1f,1f)}.Select(s=>frame.TransformPoint(box.center+new Vector3(half.x*s.Item1,half.y,half.z*s.Item2))).ToArray();
            var corner=corners.OrderByDescending(p=>p.x+p.z).First();var top=frame.TransformPoint(box.center+Vector3.up*half.y);
            var inward=top-corner;inward.y=0;inward.Normalize();var perch=corner+inward*.6f+Vector3.up*.05f;
            // Where the lead stood: against the rig's flank under that corner (149.7, 233.9), on the floor.
            Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(new Vector3(149.7f,-12.9f,233.9f),out var under,1.5f,UnityEngine.AI.NavMesh.AllAreas),"fixture: the floor where the lead stood is baked");
            float floor=under.position.y;
            Assert.Greater(perch.y-floor,1,"fixture: the rig's top stands over the floor where the lead stood");
            Put(partner,At(-60,0,-60));Put(hero,under.position+Vector3.up*.05f);
            // Made on the hounds' pen floor (its home, as Dig_Rank3's are), then stood on the rig.
            Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(new Vector3(158,-14,223),out var pen,3,UnityEngine.AI.NavMesh.AllAreas),"fixture: the hounds' pen floor is baked");
            var hound=Enemy("BridleHound",pen.position+Vector3.up*.05f,false);yield return Frames(3);
            // Stood open there, as if just thrown off a guard: it does nothing of its own that could bring it down (the one on the 17:17 build never leapt or walked off either; free to,
            // this one leaps down onto the lead at once, as it did in the engineer's replay).
            hound.Health.maximum=hound.Health.integrity=100000;Stand(hound,perch,225);hound.Health.Expose(6);yield return Frames(2);
            Assert.Greater(hound.transform.position.y-floor,1,"fixture: the hound stands on the rig's top");
            float landed=0;int swings=0;System.Action<Health,DamagePacket,float> count=(_,packet,amount)=>{if(packet.source==hero.Health)landed+=amount;};hound.Health.Damaged+=count;
            float highest=0;var standAt=hero.transform.position;
            try
            {
                for(float end=Time.time+4;Time.time<end&&landed<=0;)
                {
                    hero.motor.Move(Vector2.zero,false,false);
                    if(hero.CanAct&&Flat(hero.transform.position,standAt)>.6f){var keep=hero.Health.integrity;Put(hero,standAt);hero.Health.integrity=keep;}
                    hero.target=hound.Health;hero.TargetLocked=true;highest=Mathf.Max(highest,hound.transform.position.y-floor);
                    if(hero.CanAct&&hero.Attack())swings++;
                    yield return null;
                }
            }
            finally{hound.Health.Damaged-=count;}
            // What the bake says of the top: floor of its own, or joined to the pen's.
            string bake="no floor on the top";
            if(UnityEngine.AI.NavMesh.SamplePosition(perch,out var onTop,2,UnityEngine.AI.NavMesh.AllAreas))
            {
                var path=new UnityEngine.AI.NavMeshPath();UnityEngine.AI.NavMesh.CalculatePath(onTop.position,pen.position,UnityEngine.AI.NavMesh.AllAreas,path);
                bake=$"floor at {onTop.position} on the top, its way to the pen {path.status} by {string.Join(" ",path.corners.Select(c=>c.ToString("F1")))}";
            }
            {var path=new UnityEngine.AI.NavMeshPath();UnityEngine.AI.NavMesh.CalculatePath(under.position,pen.position,UnityEngine.AI.NavMesh.AllAreas,path);bake+=$"; the lead's floor to the pen {path.status}";}
            TestContext.WriteLine($"HOARFELL_PERCH {bake}; the rig's top {perch.y-floor:0.00} m over the floor; the hound at {hound.transform.position} ({hound.transform.position.y-floor:0.00} m up), "+
                $"{swings} swings, {landed:0} landed; "+string.Join(", ",log.Where(l=>l.StartsWith("LEAPER_")||l.StartsWith("ENEMY_"))));
            Assert.Greater(landed,0,$"a hound left on Rig three's top stood {hound.transform.position.y-floor:0.0} m over the floor for 4 s while the lead, against the rig under it, swung at it {swings} times and landed nothing: a fight that can't be finished");
        }

        /// <summary>The same fault as the driver's replay of it found it (DriverBoundsTests.TheFrozenHostsThirdRankIsFoughtToItsEnd): both hounds of the third rank standing at
        /// (148.2, 235.4), in the gap between Rig three's north-east face and the north wall, and the lead at the rig's corner (149.8, 233.9) striking at them, the rig's corner
        /// between. A hound stood as near there as one fits, doing nothing of its own, has to be struck from the floor within a few seconds.</summary>
        [UnityTest] public IEnumerator AHoundInTheGapBehindRigThreeIsStruckFromTheFloor()
        {
            yield return Zone("FrozenHost");var (hero,partner)=Party();
            Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(new Vector3(149.8f,-12.9f,233.9f),out var under,1.5f,UnityEngine.AI.NavMesh.AllAreas),"fixture: the floor where the lead stood is baked");
            Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(new Vector3(158,-14,223),out var pen,3,UnityEngine.AI.NavMesh.AllAreas),"fixture: the hounds' pen floor is baked");
            Put(partner,At(-60,0,-60));Put(hero,under.position+Vector3.up*.05f);
            // What the camera shows there: the rig, and its back against the wall.
            yield return Wait(1,hero);Still("rig-three-back");
            var hound=Enemy("BridleHound",pen.position+Vector3.up*.05f,false);yield return Frames(3);
            // As near to where they were found as a hound fits (there itself while the gap is open).
            var found=new Vector3(148.2f,under.position.y+.05f,235.4f);var stand=found;var shape=hound.GetComponent<CharacterController>();bool room=false;
            for(float r=0;r<=4&&!room;r+=.25f)
                for(int k=0;k<(r==0?1:24)&&!room;k++)
                {
                    var at=found+Quaternion.Euler(0,k*15,0)*Vector3.forward*r;
                    // On the floor (not on a rig's top, nor inside the wall's rock, which a capsule check doesn't see from within).
                    if(!Physics.Raycast(at+Vector3.up*5,Vector3.down,out var ground,7,~0,QueryTriggerInteraction.Ignore)||Mathf.Abs(ground.point.y-under.position.y)>.4f)continue;
                    at.y=ground.point.y+.05f;
                    if(Physics.CheckCapsule(at+Vector3.up*(shape.radius+.06f),at+Vector3.up*Mathf.Max(shape.radius+.06f,shape.height-shape.radius),shape.radius*.98f,~0,QueryTriggerInteraction.Ignore))continue;
                    stand=at;room=true;
                }
            Assert.IsTrue(room,"fixture: a hound fits somewhere near where they were found");
            hound.Health.maximum=hound.Health.integrity=100000;Stand(hound,stand,135);hound.Health.Expose(6);yield return Frames(2);
            float landed=0;int swings=0;System.Action<Health,DamagePacket,float> count=(_,packet,amount)=>{if(packet.source==hero.Health)landed+=amount;};hound.Health.Damaged+=count;
            var standAt=hero.transform.position;
            try
            {
                for(float end=Time.time+4;Time.time<end&&landed<=0;)
                {
                    hero.motor.Move(Vector2.zero,false,false);
                    if(hero.CanAct&&Flat(hero.transform.position,standAt)>.6f){var keep=hero.Health.integrity;Put(hero,standAt);hero.Health.integrity=keep;}
                    hero.target=hound.Health;hero.TargetLocked=true;
                    if(hero.CanAct&&hero.Attack())swings++;
                    yield return null;
                }
            }
            finally{hound.Health.Damaged-=count;}
            TestContext.WriteLine($"HOARFELL_GAP stood at {stand} ({Flat(stand,found):0.00} m from where they were found); the hound at {hound.transform.position}, {Flat(hound.transform.position,standAt):0.00} m from the lead; {swings} swings, {landed:0} landed; "+
                string.Join(", ",log.Where(l=>l.StartsWith("ENEMY_"))));
            Assert.Greater(landed,0,$"a hound in the gap behind Rig three stood {Flat(hound.transform.position,standAt):0.0} m from the lead, round the rig's corner, for 4 s while he swung at it {swings} times and landed nothing");
        }

        /// <summary>Cold in its room it is outlasted, as its goal says (D364): its bar comes down no faster than its heat fails, from where it stood as the wall slept to nothing over
        /// <see cref="BossController.MarshalTune.HeatSeconds"/>, so a blow big enough to end the fight as it shifts takes only what the frost has taken. Open after every move, it lasted 30
        /// to 48 s cold in the player (Builds/quality/nov22b-whole s36, nov22-recon-h-36 and -k-36).</summary>
        [UnityTest] public IEnumerator ColdInItsRoomItsBarComesDownNoFasterThanItsHeatFails()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-6));
            var couplings=Couplings(At(0,0,0));yield return Frames(2);
            var marshal=Enemy("HostMarshal",At(0,.1f,0));yield return Frames(3);var boss=marshal.GetComponent<BossController>();var body=marshal.Health;
            marshal.Passive=false;foreach(var coupling in couplings)Kill(coupling.Health,hero);yield return Frames(2);
            // MARSHAL_COLD and not MARSHAL_COLDER (a coupling broken): the wall asleep.
            yield return Until(()=>Said("MARSHAL_COLD heat"),9,hero);Assert.IsTrue(Said("MARSHAL_COLD heat"),"fixture: three couplings broken and the wall never slept");
            // Past the shift's own brace, when nothing lands on it.
            yield return Until(()=>Said("BOSS_SHIFT HostMarshal 2")&&GameTime.Now>body.InvulnerableUntil+.05f,4,hero);
            Put(hero,At(0,.05f,-40));float from=body.integrity,blow=body.maximum*.4f;
            // The old way first: its bar held nowhere, the blow takes it far under what its heat still holds.
            var holds=body.Holds;body.Holds=null;Hit(body,hero,blow);
            Assert.Less(body.integrity,from*boss.MarshalHeatLeft-blow*.5f,"fixture: with its bar held nowhere the blow didn't land, so the check can't see the fault");
            body.integrity=from;body.Holds=holds;
            Hit(body,hero,blow);yield return Frames(2);float left=boss.MarshalHeatLeft;
            Assert.Greater(left,.8f,"fixture: its heat was already failing well before the blow");Assert.IsTrue(body.Alive,"the blow ended the fight as it went cold");
            Assert.GreaterOrEqual(body.integrity,from*left-2,"a blow took its bar past what its failing heat still holds");
            // What the frost has taken since comes off.
            yield return Wait(3,hero);float later=boss.MarshalHeatLeft;Hit(body,hero,blow);
            Assert.AreEqual(from*later,body.integrity,from*.02f,"three seconds on, a blow doesn't take what its heat has lost");
            StringAssert.Contains("Outlast",boss.FightGoal(""));marshal.Passive=true;
        }
        /// <summary>D410: cold in its room its heat fails on its own, as its goal says, whoever is left to strike it. In nov24-whole s36 its partner stood out at 17% and the lead ground
        /// its cold half alone for 190 s. Its bar stands at or under what is left of its heat over <see cref="BossController.MarshalTune.ColdMost"/>, never under 1, and the next blow ends it.</summary>
        [UnityTest] public IEnumerator ColdInItsRoomItsHeatFailsOnItsOwnWithNobodyToStrikeIt()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-6));
            var couplings=Couplings(At(0,0,0));yield return Frames(2);
            var marshal=Enemy("HostMarshal",At(0,.1f,0));yield return Frames(3);var boss=marshal.GetComponent<BossController>();var body=marshal.Health;
            marshal.Passive=false;foreach(var coupling in couplings)Kill(coupling.Health,hero);yield return Frames(2);
            // As ColdInItsRoomItsBarComesDownNoFasterThanItsHeatFails: the wall asleep, then past the shift's brace, and the lead out of its reach.
            yield return Until(()=>Said("MARSHAL_COLD heat"),9,hero);Assert.IsTrue(Said("MARSHAL_COLD heat"),"fixture: three couplings broken and the wall never slept");
            yield return Until(()=>Said("BOSS_SHIFT HostMarshal 2")&&GameTime.Now>body.InvulnerableUntil+.05f,4,hero);Put(hero,At(0,.05f,-40));
            const System.Reflection.BindingFlags Own=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var coldAt=typeof(BossController).GetField("marshalColdAt",Own);float from=(float)typeof(BossController).GetField("marshalHeatFrom",Own).GetValue(boss);
            var asBefore=typeof(BossController).GetField("heatWithoutCeiling",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            // A hundred seconds cold with nothing landing on it: the clock put back rather than waited out.
            float stood=body.integrity;Assert.Greater(stood,from*.9f,"fixture: something struck it after the wall slept");
            coldAt.SetValue(boss,GameTime.Now-100);
            try{asBefore.SetValue(null,true);yield return Wait(.3f,hero);Assert.AreEqual(stood,body.integrity,from*.01f,"fixture: as before, its bar came down with nobody striking it, so the check can't see the fault");}
            finally{asBefore.SetValue(null,false);}
            yield return Wait(.3f,hero);
            float most=from*(1-(GameTime.Now-(float)coldAt.GetValue(boss))/BossController.MarshalTune.ColdMost);
            Assert.LessOrEqual(body.integrity,most+from*.01f,$"a hundred seconds cold with nobody striking it, its bar stands at {body.integrity:0} over the {most:0} its heat still holds");
            Assert.Greater(body.integrity,1,"its heat failed before its time");
            // Its time out: its last point, and the next blow ends it.
            coldAt.SetValue(boss,GameTime.Now-BossController.MarshalTune.ColdMost-1);yield return Wait(.3f,hero);
            Assert.AreEqual(1,body.integrity,.01f,"its heat spent, its bar isn't at its last point");Assert.IsTrue(Said("MARSHAL_HEAT_SPENT"),"its heat spent, the log doesn't say so");
            Hit(body,hero,10);Assert.IsFalse(body.Alive,"its heat spent, a blow didn't end it");
        }
        /// <summary>D410: the replay's chase strikes what the bar names (ContinuousReview.Named: the target card's name in the status line or the objective). "BREAK A COUPLING" never
        /// named a "Thaw coupling", so in every capture of route 36 it ground the warm shell with a rank out and broke one coupling of three by accident (nov24-whole s36: 160 s warm).</summary>
        [UnityTest] public IEnumerator WithARankOutTheReplayGoesForARunningThawCouplingNotTheMarshalsShell()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-6));
            var couplings=Couplings(At(0,0,0));yield return Frames(2);
            var marshal=Enemy("HostMarshal",At(0,.1f,0));yield return Frames(3);var boss=marshal.GetComponent<BossController>();
            Assert.IsTrue(boss.Perform("Rally"),"fixture: its call");yield return Until(()=>!boss.Busy,4,hero);
            Assert.AreEqual(3,Guards().Length,"fixture: its call brought no rank out");Assert.IsTrue(boss.Warm,"fixture: with its couplings running a blow on it lands at a share");
            string card=DisplayNames.Target(couplings[0].Health.id).ToUpperInvariant(),said=(boss.StatusText+"  "+boss.FightGoal("")).ToUpperInvariant();
            Assert.IsFalse("THE RANKS ARE WAKING  ·  BREAK A COUPLING  BREAK A COUPLING TO PUT THE WALL BACK TO SLEEP (0/3)".Contains(card),"fixture: the words as they were name the coupling's card, so the check can't see the fault");
            StringAssert.Contains(card,said,"the bar and the objective don't name a thaw coupling by the name on its card");
            StringAssert.Contains("BREAK A COUPLING",boss.StatusText,"its bar's line changed");
            const System.Reflection.BindingFlags Own=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var fixture=new GameObject("Marshal chase fixture");fixture.SetActive(false);var replay=fixture.AddComponent<ContinuousReview>();
            try
            {
                marshal.Passive=false;
                var wanted=(Health)typeof(ContinuousReview).GetMethod("WantedTarget",Own).Invoke(replay,new object[]{hero});
                Assert.IsNotNull(wanted,"with a rank out and its couplings running in reach, the chase leaves the lock where it is: on the Marshal's shell");
                Assert.IsTrue(wanted.TryGetComponent<ThawCoupling>(out var locked)&&locked.Running,"the chase locks "+wanted.name+", not a running thaw coupling");
                Assert.AreEqual(couplings.OrderBy(c=>Flat(c.transform.position,hero.transform.position)).First(),locked,"the chase locks a coupling further off than the nearest");
                // From beyond the lock's thirty metres, still at the fight: walked toward.
                Put(hero,At(0,.05f,-33));typeof(ContinuousReview).GetField("wantAt",Own).SetValue(replay,-1.0);
                var toward=(Vector2?)typeof(ContinuousReview).GetMethod("TowardNamed",Own).Invoke(replay,new object[]{hero});
                var far=(Health)typeof(ContinuousReview).GetField("namedFar",Own).GetValue(replay);
                Assert.IsTrue(toward.HasValue&&far!=null&&far.GetComponent<ThawCoupling>()!=null,"with every coupling beyond the lock's reach the chase doesn't walk toward one");
            }
            finally{marshal.Passive=true;Object.Destroy(fixture);}
        }

        // ------------------------------------------------------------------ what the fight is sized on
        /// <summary>The driver's share of this test's own flat out. .51 since D362 (November 22): at .3 the two terms gave 199 s for 125,000 at the 1,791 to 1,811 a second read
        /// that morning (d355-chapters), and today's scout felled it in 115.5 s (Builds/quality/nov22-recon-e s36): .52, rounded down.</summary>
        const float DriverShare=.51f,CouplingUptime=.5f,FlatOutSeconds=15;
        /// <summary>A pair flat out on a body that never moves, as QaPlanCombatTests measures it: both heroes attacking, every skill as its cooldown and Charge allow; one who fights with an
        /// Edge stands at its reach, one with an Emitter a few steps off.</summary>
        IEnumerator Rate(string[] pair,System.Action<float> measured)
        {
            yield return Deploy(pair);
            var at=At(0,.1f,0);var heroes=PartyController.Current.members.ToArray();
            var enemy=Enemy("HostMarshal",at);yield return Frames(3);var body=enemy.Health;
            enemy.GetComponent<BossController>().enabled=false;body.maximum=body.integrity=10000000;
            float reach=BossReach.Edge(enemy);var homes=new Vector3[heroes.Length];
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
        /// <summary>The plan's length on today's build, with nothing assumed but the driver's share and how much of the first half a pair spends at a coupling: two to five minutes, for every
        /// pair a player can have out by then, at Sync 17 in the chapter's arrival parts (Prime). The first half is its three couplings (each a share of its own integrity, reached across the
        /// gallery and broken under its moves and its ranks, so struck about half the time, and counted at its body's rate though a coupling breaks to a Kinetic blow, the long side), the
        /// second its own half of the bar. Each pair is held to the shared test's own floor and ceiling too. And the integrity is pinned to the builder: a rebuild writes whatever its source says.</summary>
        [UnityTest] public IEnumerator TheHostMarshalLastsWhatThePlanAsksForEveryPairAPlayerCanPutOut()
        {
            string source=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"_Project/Scripts/Editor/World/HoarfellBuilder.Enemies.cs"));
            float integrity=Def("HostMarshal").integrity;
            StringAssert.Contains("e.id=\"HostMarshal\";",source);StringAssert.Contains($"e.integrity={integrity:0};e.breakThreshold=800;",source,"a rebuild would put the Host Marshal's old integrity back");
            yield return Zone("Arena_Ground");Floor();yield return Level(17);
            var failures=new List<string>();
            foreach(var pair in new[]{new[]{"Taren","Idra"},new[]{"Taren","Sela"},new[]{"Sela","Idra"}})
            {
                float rate=0;yield return Rate(pair,r=>rate=r);
                // The cold half is its heat's, not the bar's alone (D364, D410): no shorter than its heat takes to fail and no longer than the most it holds out.
                float couplings=3*integrity*BossController.MarshalTune.CouplingIntegrity/(rate*DriverShare*CouplingUptime),
                    bar=Mathf.Clamp(integrity*.5f/(rate*DriverShare),BossController.MarshalTune.HeatSeconds,BossController.MarshalTune.ColdMost),seconds=couplings+bar;
                string line=$"HOARFELL_RATE {pair[0]}+{pair[1]} sync=17 prime rate={rate:0}/s couplings={couplings:0}s bar={bar:0}s total={seconds:0}s shared={integrity/(rate*DriverShare):0}s quarter={integrity/(rate*.25f):0}s";
                TestContext.WriteLine(line);Debug.Log(line);
                if(seconds<120||seconds>300)failures.Add($"{line}: the plan asks 120 to 300");
                if(integrity<rate*DriverShare*120)failures.Add($"{line}: the shared floor, 120 s at the driver's share");
                if(integrity/(rate*.25f)>ContentRegistry.SlowPairCeiling(300))failures.Add($"{line}: the shared ceiling, {ContentRegistry.SlowPairCeiling(300):0} s at a quarter of flat out (D362 continued)");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
