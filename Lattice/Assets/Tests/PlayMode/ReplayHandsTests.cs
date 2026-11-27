using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>What the replay driver does of its own accord since the loot, impact and skills streams: it mends the lead as a player would (inside the game's wait
    /// between items, with the item that fits, nothing wasted), raises a fallen partner, fills the pouch to its cap and no further, spends a full EXTRA gauge on a boss
    /// or a crowd, and takes a boss lost at its door again from the door. Each is a route knob (autoHeal, useItem, stock, useExtra, bossDoor), and every press here is
    /// the driver's own virtual pad: nothing calls an actor, an item or a menu.
    /// <para>Run with CORONACH_DRIVER_KNOBS=off and every route here is written as the routes were before, without its knob. Each check but the rotation's then fails
    /// for its own reason, which is how they were proved against the driver they replace.</para></summary>
    public sealed class ReplayHandsTests
    {
        static readonly bool On=System.Environment.GetEnvironmentVariable("CORONACH_DRIVER_KNOBS")!="off";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject fixture;ContinuousReview replay;bool finished;readonly List<string> log=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        void Logged(string text,string stack,LogType type){if(text.StartsWith("QUALITY_")||text.StartsWith("SKILL_KIND ")||text.StartsWith("EXTRA_")||text.StartsWith("REVIVE_OK"))log.Add(text);}
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            // Headless has no focused Game view. Route virtual devices into play.
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            DevLoadout.Apply("starter");
            // Never active: the driver's Start wants a player's launch arguments. Its steps, its pad and its decisions are driven from here, a frame at a time.
            fixture=new GameObject("Replay hands fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();
            log.Clear();finished=false;Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;
            var shop=Object.FindFirstObjectByType<ShopUi>();if(shop!=null&&shop.IsOpen)shop.Close();
            // Unity does not call OnDestroy on a never-active fixture: its pad is taken away here, with whatever it held.
            if(replay!=null){var pad=Get<Gamepad>("pad");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);Set("pad",null);}
            if(fixture!=null)Object.Destroy(fixture);
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the driver, by its private parts
        void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
        T Get<T>(string name)=>(T)typeof(ContinuousReview).GetField(name,Private).GetValue(replay);
        object Call(string name,params object[] arguments)=>typeof(ContinuousReview).GetMethod(name,Private).Invoke(replay,arguments);
        List<string> Failures=>Get<List<string>>("failures");
        /// <summary>What the run's report will hold of one kind of record (bossAttempts, stocks), each as the JSON run.json gets.</summary>
        List<string> Records(string name)=>Get<IList>(name).Cast<object>().Select(record=>JsonUtility.ToJson(record)).ToList();
        /// <summary>Gives the driver its pad, its route and a running clock, as its Start does once the settle has let the scene's own input go by.</summary>
        IEnumerator Hands(QualityRoute route)
        {
            Set("pad",InputSystem.AddDevice<Gamepad>("ReplayHandsPad"));
            Call("InputUpdate");yield return null;yield return null;
            Set("route",route);Set("recording",true);Get<System.Diagnostics.Stopwatch>("clock").Start();
        }
        void Step(QualityStep step)=>Call("BeginStep",step);
        /// <summary>Holds the driver on its current step, feeding the pad each frame, until the time is up or the thing looked for has happened.</summary>
        IEnumerator Drive(float seconds,System.Func<bool> until=null,System.Action each=null)
        {
            for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end&&(until==null||!until());){each?.Invoke();Call("InputUpdate");yield return null;}
        }
        /// <summary>Plays a whole route's steps as the player does (RunSteps), a frame at a time.</summary>
        IEnumerator Play(QualityRoute route,float most,System.Action each=null)
        {
            yield return Hands(route);var run=(IEnumerator)Call("RunSteps");finished=false;
            for(float end=Time.unscaledTime+most;Time.unscaledTime<end;)
            {
                if(!run.MoveNext()){finished=true;break;}
                each?.Invoke();Call("InputUpdate");yield return null;
            }
        }

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static MemberState Member(string id)=>State.party.Find(m=>m.id==id);
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=PartyController.Current.Active);
        static T[] All<T>() where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>The ground yard, empty, with the party at a Sync where a gel is a quarter of the bar (at Sync 1 its flat 60 is half of it) and the partner standing still.</summary>
        static IEnumerator Arena(int sync=13)
        {
            SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: the yard never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var hero in PartyController.Current.members)hero.GetComponent<PartnerBrain>().enabled=false;
            yield return new WaitForSecondsRealtime(.7f);
            foreach(var member in State.party)member.level=sync;
            PartyController.Current.RefreshStats();
            foreach(var hero in PartyController.Current.members){hero.Health.integrity=hero.Health.maximum;hero.charge=100;}
            ItemUse.Reset();
            Assert.IsTrue(ZoneController.Current.Combat&&!ZoneController.Current.Flight,"fixture: the yard is a place with fights, on foot: the quick item answers nowhere else");
            Assert.AreEqual("Taren",Lead.character,"fixture: Taren leads");
        }
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=position;body.enabled=true;Physics.SyncTransforms();
        }
        static void Fell(CombatActor hero){hero.Health.InvulnerableUntil=0;hero.Health.Receive(new DamagePacket{amount=hero.Health.maximum*100,type=DamageType.Pulse});}
        /// <summary>A body that stands where it's put, does nothing and can't be killed or broken.</summary>
        static EnemyBrain Dummy(string id,Vector3 at)
        {
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;
            enemy.Health.maximum=enemy.Health.integrity=1000000;enemy.Health.breakThreshold=1e9f;return enemy;
        }
        /// <summary>A hound awake beside the party makes it a fight. Nobody can be hurt, so the only Integrity that moves is what a check sets and what an item gives back.</summary>
        static IEnumerator AFight()
        {
            foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+300;
            ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),Lead.transform.position+Vector3.forward*6);yield return null;
            Assert.IsTrue(GearEffects.InFight(Lead.transform.position),"fixture: a fight is on around the lead");
        }
        static QualityRoute Yard(bool heal=false,bool extra=false,params QualityStep[] steps)=>new QualityRoute{name="replay hands",scene="Arena_Ground",autoHeal=heal&&On,useExtra=extra&&On,steps=steps};
        static string Said()=>Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).First(t=>t.name=="Feedback").text;

        // ------------------------------------------------------------------ item 2: healing
        /// <summary>S2: one shared wait of four seconds between items in a fight. The routes pressed a gel on a clock; into the wait, that press is a refusal.</summary>
        [UnityTest] public IEnumerator UnderHalfInAFightTheDriverMendsOnceAWaitAndIsNeverRefused()
        {
            yield return Arena();var lead=Lead;State.consumables["RepairGel"]=8;State.quickItem="RepairGel";
            yield return AFight();
            int refused=0;System.Action<string> refusal=_=>refused++;PlayerBrain.QuickItemRefused+=refusal;
            try
            {
                yield return Hands(Yard(heal:true));Step(new QualityStep{name="a round of the fight",seconds=120});
                var used=new List<float>();int gels=8;lead.Health.integrity=lead.Health.maximum*.3f;
                yield return Drive(9.6f,null,()=>
                {
                    int now=State.consumables["RepairGel"];if(now<gels){gels=now;used.Add(GameTime.Now);}
                    // Hurt again at once: the driver would mend every frame if the game let it.
                    if(lead.Health.integrity>lead.Health.maximum*.4f)lead.Health.integrity=lead.Health.maximum*.3f;
                });
                Assert.GreaterOrEqual(used.Count,2,"at three tenths of their Integrity in a fight, with eight gels in the pouch, the lead was left unmended");
                for(int i=1;i<used.Count;i++)Assert.GreaterOrEqual(used[i]-used[i-1],ItemUse.Cooldown-.02f,"two gels went inside the game's wait between items");
                Assert.LessOrEqual(used.Count,3,"more gels went in 9.6 s than the wait between items allows");
                Assert.AreEqual(0,refused,"the driver pressed the quick item into the wait: the HUD counts it down, and a press there is a refusal");
            }
            finally{PlayerBrain.QuickItemRefused-=refusal;}
        }
        [UnityTest] public IEnumerator BetweenFightsTheDriverTopsUpWithWhatFitsAndWastesNothing()
        {
            yield return Arena();var lead=Lead;float most=lead.Health.maximum;var gel=GameCatalog.Find<ConsumableDef>("RepairGel");
            State.consumables["RepairGel"]=4;State.consumables["RepairSalve"]=2;State.quickItem="RepairGel";
            Assert.AreEqual(most*.25f,ItemUse.Heal(gel,lead),.5f,"fixture: at this Sync a gel is a quarter of the bar");
            Assert.IsFalse(GearEffects.InFight(lead.transform.position),"fixture: nothing is awake");
            yield return Hands(Yard(heal:true));Step(new QualityStep{name="the walk to the next thing",seconds=120});
            // Nine tenths: most of a gel would be thrown away.
            lead.Health.integrity=most*.9f;yield return Drive(2);
            Assert.AreEqual(4,State.consumables["RepairGel"],"a gel went at nine tenths of the bar");
            // Six tenths: a gel fits whole, and a Salve, half the bar, doesn't.
            lead.Health.integrity=most*.6f;yield return Drive(4,()=>State.consumables["RepairGel"]<4);
            Assert.AreEqual(3,State.consumables["RepairGel"],"at six tenths of the bar between fights the lead wasn't topped up");
            yield return Drive(2);
            Assert.AreEqual(3,State.consumables["RepairGel"],"a second gel went where it no longer fit");
            Assert.AreEqual(2,State.consumables["RepairSalve"],"a Salve went where half of it was wasted");
            Assert.AreEqual(most*.85f,lead.Health.integrity,1f,"the top-up didn't mend what a gel mends");
        }
        [UnityTest] public IEnumerator InAFightTheDriverCyclesToTheItemThatFitsWhatIsMissing()
        {
            yield return Arena();var lead=Lead;float most=lead.Health.maximum;
            State.consumables["RepairGel"]=3;State.consumables["RepairSalve"]=2;State.consumables["RepairCore"]=1;State.quickItem="RepairGel";
            yield return AFight();
            yield return Hands(Yard(heal:true));Step(new QualityStep{name="a round of the fight",seconds=120});
            // A fifth of the bar left: a gel gives back a quarter, a Salve half, a Core all of it.
            lead.Health.integrity=most*.2f;yield return Drive(5,()=>State.consumables["RepairCore"]==0);
            Assert.AreEqual(0,State.consumables["RepairCore"],"at a fifth of the bar, with a Repair Core in the pouch, the Core stayed there");
            Assert.AreEqual(3,State.consumables["RepairGel"],"a gel went first, at a fifth of the bar with a Core to hand");
            Assert.AreEqual(2,State.consumables["RepairSalve"]);
            Assert.AreEqual("RepairCore",State.quickItem,"the Core wasn't reached the way a player reaches it, by cycling the quick item");
            // The wait is the fight's own, and the first check sat through it: here it is let go.
            yield return Drive(.6f);ItemUse.Reset();
            // Under half by a little: the Salve fits, a gel leaves most of it unmended.
            lead.Health.integrity=most*.45f;yield return Drive(5,()=>State.consumables["RepairSalve"]<2);
            Assert.AreEqual(1,State.consumables["RepairSalve"],"at 45% of the bar the Salve, which fits, wasn't the item used");
            Assert.AreEqual(3,State.consumables["RepairGel"],"a gel went where the Salve fit");
        }
        /// <summary>D410: a partner under the line she closes at (PartnerBrain, 30%) stands off a fight until she is mended, and the driver mended only the lead: at 17% Idra stood out
        /// the Host Marshal's last 190 s (nov24-whole s36). A player mends her from the lead's pouch with what the game lets the lead give her without a swap, a Sync Tonic.</summary>
        [UnityTest] public IEnumerator APartnerTooHurtToCloseIsMendedWithAPartyItemWithoutASwap()
        {
            yield return Arena();var lead=Lead;var partner=Partner;float most=partner.Health.maximum;
            State.consumables["RepairGel"]=3;State.consumables["SyncTonic"]=1;State.quickItem="RepairGel";
            yield return AFight();
            yield return Hands(Yard(heal:true));Step(new QualityStep{name="a round of the fight",seconds=120});
            // Over the line she still closes on what she fights: nothing goes.
            partner.Health.integrity=most*.4f;yield return Drive(2.5f);
            Assert.AreEqual(1,State.consumables["SyncTonic"],"a Sync Tonic went on a partner who still closes on what she fights");
            partner.Health.integrity=most*.17f;yield return Drive(5,()=>State.consumables["SyncTonic"]<1);
            Assert.AreEqual(0,State.consumables["SyncTonic"],"at 17% the partner stands out of the fight and the lead's Sync Tonic stayed in the pouch");
            Assert.AreEqual(3,State.consumables["RepairGel"],"a gel went on a lead who was whole");
            Assert.AreSame(lead,PartyController.Current.Active,"the lead swapped to mend her");
            Assert.Greater(partner.Health.integrity,most*ReplayHands.PartnerBelow,"the tonic didn't bring her back over the line she closes at");
        }
        [UnityTest] public IEnumerator AFallenPartnerIsRaisedWithARelinkSpikeWhenThePouchHasOne()
        {
            yield return Arena();var lead=Lead;var partner=Partner;State.consumables["RelinkSpike"]=1;State.quickItem="RepairGel";
            // Out of the game's own reach: within 2 m the lead raises a partner just by standing there.
            Place(partner,lead.transform.position+Vector3.forward*7);yield return null;Fell(partner);yield return null;
            Assert.IsFalse(partner.Health.Alive,"fixture: the partner is down");var stood=lead.transform.position;
            yield return Hands(Yard(heal:true));Step(new QualityStep{name="both heroes ready",seconds=60,until="partyAlive"});
            yield return Drive(6,()=>partner.Health.Alive);
            Assert.IsTrue(partner.Health.Alive,"with a Relink Spike in the pouch the partner was left down");
            Assert.AreEqual(0,State.consumables["RelinkSpike"],"the partner got up and the Spike wasn't spent");
            Assert.Less(Flat(stood,lead.transform.position),1.5f,"the lead walked over to do what the Spike does from where they stand");
        }
        /// <summary>QA-035: the route's revive step lost a chain when nobody got to Taren. The driver now goes to a fallen partner in any fight step, whenever it happens.</summary>
        [UnityTest] public IEnumerator WithNoSpikeTheLeadWalksToAFallenPartnerAndStandsByThem()
        {
            yield return Arena();var lead=Lead;var partner=Partner;State.consumables.Remove("RelinkSpike");
            Place(partner,lead.transform.position+Vector3.forward*7);yield return null;Fell(partner);yield return null;
            Assert.IsFalse(partner.Health.Alive,"fixture: the partner is down");
            Assert.Greater(Flat(lead.transform.position,partner.transform.position),5,"fixture: the partner fell out of the lead's reach");
            yield return Hands(Yard(heal:true));Step(new QualityStep{name="both heroes ready",seconds=60,until="partyAlive"});
            yield return Drive(15,()=>partner.Health.Alive);
            Assert.IsTrue(partner.Health.Alive,"the partner lay seven metres off and the lead never went to them");
            Assert.Less(Flat(lead.transform.position,partner.transform.position),2.6f,"the partner got up with the lead somewhere else: not the walk this checks");
            Assert.IsTrue(log.Contains("REVIVE_OK"),"the partner got up some other way than the lead standing by them");
        }
        /// <summary>The driver's walk to a fallen partner steered as it did before ff99a7a, straight at them (ContinuousReview's reviveStraight), for this check's before.</summary>
        static void Straight(bool on)=>typeof(ContinuousReview).GetField("reviveStraight",BindingFlags.NonPublic|BindingFlags.Static)?.SetValue(null,on);
        /// <summary>A place on the relay's floor: the baked floor under it, a little over, for the body to settle on.</summary>
        static Vector3 Floor(float x,float z)
        {
            Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(new Vector3(x,4,z),out var hit,8,UnityEngine.AI.NavMesh.AllAreas),$"fixture: no floor at ({x}, {z})");
            return hit.position+Vector3.up*.05f;
        }
        /// <summary>The lead at one place and the partner felled at another, and the driver's step that waits for both heroes up: the seconds until the partner is up, or -1.</summary>
        IEnumerator RaiseAcross(Vector3 from,Vector3 to,float most,float[] took,int index,string what)
        {
            var lead=Lead;var partner=Partner;
            if(!partner.Health.Alive){partner.Health.Heal(partner.Health.maximum);yield return null;}
            Place(lead,from);Place(partner,to);yield return Frames(10);Fell(partner);yield return null;
            Assert.IsFalse(partner.Health.Alive,"fixture: the partner is down");
            Step(new QualityStep{name="both heroes ready",seconds=60,until="partyAlive"});
            float start=Time.unscaledTime,said=start;log.Clear();
            // Every two seconds, where the lead is and what holds him, for the record.
            yield return Drive(most,()=>partner.Health.Alive,()=>
            {
                if(Time.unscaledTime-said<2)return;said=Time.unscaledTime;var at=lead.transform.position;
                var touching=Physics.OverlapSphere(at+Vector3.up*.9f,1.4f,~0,QueryTriggerInteraction.Ignore).Where(c=>c.transform.root!=lead.transform.root).Select(c=>c.name);
                TestContext.WriteLine($"REVIVE_STEP {what} {Time.unscaledTime-start:0.0} s: lead {at:F1}, ui {Call("ObserveUi")}, blocked {GameServices.Current.Input.Blocked}, prompt {(PromptService.Interaction!=null?PromptService.Interaction.name:"-")}, raise {(Get<CombatActor>("raise")!=null)}, near {string.Join("/",touching)}");
            });
            took[index]=partner.Health.Alive?Time.unscaledTime-start:-1;
            TestContext.WriteLine($"REVIVE_WALK {what}: {(took[index]<0?"not raised":$"raised in {took[index]:0.00} s")}, the lead {Flat(lead.transform.position,to):0.00} m from them, {Flat(lead.transform.position,from):0.00} m from where he set off");
        }
        /// <summary>QA-035. In the Sunken Relay the driver steered straight at Taren, down across the Great Lantern's hall, and a column, a crew body or the yielded Hessa between
        /// them stopped Idra: he was never raised, and Hessa's talk, which waits for that, never came (final/s15-rejected-revive-step, no longer on disk). Since ff99a7a the walk goes
        /// along the baked floor's way and steps aside when it stops gaining. Here, in that hall: a fallen partner behind one of its four columns (straight first, which must fail
        /// there), behind a scavenger standing in the way, and behind Hessa yielded where her fight ends.</summary>
        [UnityTest] public IEnumerator TheLeadRaisesAFallenPartnerRoundAColumnABodyAndAYieldedCaptain()
        {
            foreach(string flag in new[]{"relay.hessaConfronted","bossdown.HessaKorl","clear.Relay_Hessa"})GameServices.Current.Flags.SetBool(flag,true);
            SceneFlow.Current.LoadZone("SunkenRelay","Vault");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: the relay never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var hero in PartyController.Current.members){hero.GetComponent<PartnerBrain>().enabled=false;hero.Health.InvulnerableUntil=0;}
            yield return new WaitForSecondsRealtime(.7f);
            State.consumables.Remove("RelinkSpike");
            yield return Hands(Yard(heal:true));
            var took=new float[4];
            // The column at (10, 132), the hall's south-east one (LanternRoadBuilder.Relay): the lead south of it, the partner north, on the line through its middle.
            var near=Physics.OverlapSphere(Floor(10,132)+Vector3.up*1.5f,4,~0,QueryTriggerInteraction.Ignore);
            var column=near.Where(c=>c.attachedRigidbody==null&&!(c is CharacterController)&&c.bounds.size.y>2.5f&&Mathf.Max(c.bounds.size.x,c.bounds.size.z)<8).OrderBy(c=>Flat(c.bounds.center,new Vector3(10,0,132))).FirstOrDefault();
            Assert.IsNotNull(column,$"fixture: no column near (10, 132), floor {Floor(10,132)}: {string.Join(", ",near.Select(c=>c.name+" "+c.bounds.center.ToString("F1")+" "+c.bounds.size.ToString("F1")))}");
            var middle=column.bounds.center;float round=Mathf.Max(column.bounds.extents.x,column.bounds.extents.z);
            Vector3 south=Floor(middle.x,middle.z-round-3.5f),north=Floor(middle.x,middle.z+round+3.5f);
            TestContext.WriteLine($"REVIVE_COLUMN {column.name} at {middle:F2} size {column.bounds.size:F2}; lead {south:F2}, partner {north:F2}");
            Assert.IsTrue(Physics.Linecast(south+Vector3.up*1.2f,north+Vector3.up*1.2f,out var between,~0,QueryTriggerInteraction.Ignore)&&between.collider==column,
                "fixture: the column stands between them");
            // Steered straight, the round column turns him: r10b-after2 read 5.08 s straight and 5.97 s by the floor's way, both raised. So a square one too, in the bake (carved),
            // its flat face square across their line, as a wall's end or a crate stack stands: straight at it he stands still.
            var pillar=GameObject.CreatePrimitive(PrimitiveType.Cube);pillar.name="QaSquarePillar";pillar.transform.position=Floor(-4,145)+Vector3.up*2;pillar.transform.localScale=new Vector3(3,4,1.2f);
            var carve=pillar.AddComponent<UnityEngine.AI.NavMeshObstacle>();carve.shape=UnityEngine.AI.NavMeshObstacleShape.Box;carve.size=Vector3.one;carve.carving=true;carve.carveOnlyStationary=false;
            yield return Frames(5);
            var straight=new float[1];
            try{Straight(true);yield return RaiseAcross(Floor(-4,139.5f),Floor(-4,150.5f),12,took,0,"straight, a square pillar between");yield return RaiseAcross(south,north,12,straight,0,"straight, the column between");}
            finally{Straight(false);}
            yield return RaiseAcross(Floor(-4,139.5f),Floor(-4,150.5f),20,took,1,"a square pillar between");
            Object.Destroy(pillar);yield return Frames(3);
            var roundTook=new float[1];yield return RaiseAcross(south,north,20,roundTook,0,"the column between");
            // A scavenger standing in the open between them, on their line.
            Vector3 from=Floor(-5,127),to=Floor(-5,139);var body=Dummy("MusterScav",Floor(-5,133));yield return Frames(5);
            yield return RaiseAcross(from,to,20,took,2,"a body between");
            Object.Destroy(body.gameObject);yield return null;
            // Hessa yielded where her fight ends, between them: as the fight leaves her, beaten and standing.
            from=Floor(2,126);to=Floor(2,140);
            var hessa=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),Floor(2,133));hessa.Passive=true;yield return Frames(5);
            hessa.Health.InvulnerableUntil=0;hessa.Health.Receive(new DamagePacket{amount=hessa.Health.maximum*100,type=DamageType.Pulse});yield return new WaitForSecondsRealtime(1);
            Assert.IsFalse(hessa.Health.Alive,"fixture: Hessa has yielded");
            // As it was (r10b-after2, r10b-after3): her prompt is up 2.8 m short of her, and the driver held the lead there for the press, raising nobody for 20 s.
            var atHer=new float[1];var talkSwitch=typeof(ContinuousReview).GetField("raiseAtTalkAsBefore",BindingFlags.NonPublic|BindingFlags.Static);
            try{talkSwitch?.SetValue(null,true);yield return RaiseAcross(from,to,10,atHer,0,"as it was, the yielded captain between");}
            finally{talkSwitch?.SetValue(null,false);}
            Assert.Less(atHer[0],0,"fixture, as it was: the lead stood at the yielded captain's prompt and never raised the partner");
            yield return RaiseAcross(from,to,20,took,3,"the yielded captain between");
            Assert.Less(took[0],0,"fixture, as it was: steered straight, the lead stood against the square pillar and never raised the partner");
            Assert.That(roundTook[0]>=0&&roundTook[0]<=15,$"behind the hall's column the partner wasn't raised inside 15 s (took {roundTook[0]:0.0} s)");
            for(int i=1;i<4;i++)Assert.That(took[i]>=0&&took[i]<=15,$"case {i}: the partner wasn't raised inside 15 s (took {took[i]:0.0} s)");
        }
        [UnityTest] public IEnumerator AStepCanKeepTheDriverOffThePouchOrPressOneItemItself()
        {
            yield return Arena();var lead=Lead;float most=lead.Health.maximum;
            State.consumables["RepairGel"]=4;State.consumables["ChargeCell"]=2;State.quickItem="RepairGel";
            yield return Hands(Yard(heal:true));
            lead.Health.integrity=most*.3f;lead.charge=0;
            Step(new QualityStep{name="the hero stays hurt for the lesson",seconds=60,useItem=On?"none":null});
            yield return Drive(2);
            Assert.AreEqual(4,State.consumables["RepairGel"],"a step that says none had a gel pressed in it");
            Step(new QualityStep{name="the Charge Cell lesson",seconds=60,useItem=On?"ChargeCell":null});
            yield return Drive(5,()=>State.consumables["ChargeCell"]<2);
            Assert.AreEqual(1,State.consumables["ChargeCell"],"the step named a Charge Cell and none was used");
            Assert.AreEqual("ChargeCell",State.quickItem,"the Cell wasn't reached by cycling the quick item");
            Assert.Greater(lead.charge,40,"the Cell gave no Charge");
            yield return Drive(1.5f);
            Assert.AreEqual(1,State.consumables["ChargeCell"],"the step's one item was pressed twice");
            Assert.AreEqual(4,State.consumables["RepairGel"],"the step's own item didn't keep the driver's mending out of the step");
            Step(new QualityStep{name="the walk on",seconds=60});
            yield return Drive(5,()=>State.consumables["RepairGel"]<4);
            Assert.AreEqual(3,State.consumables["RepairGel"],"with the lesson over, a lead at three tenths of the bar was left unmended");
        }

        // ------------------------------------------------------------------ item 2: stock to cap
        /// <summary>S2: a party carries nine gels. chapter-13 bought twelve and chapter-07 made twelve, and the last three presses of each were refusals.</summary>
        [UnityTest] public IEnumerator StockingToTheCapBuysWhatFitsAndPressesNoFurther()
        {
            yield return Arena();var shop=ZoneController.Current.GetComponent<ShopUi>();Assert.IsNotNull(shop,"fixture: the zone has a stall's screen");
            var offer=ShopUi.Stock(ShopUi.Outfitter,State).First(o=>o.id=="RepairGel");int cap=Inventory.Cap(GameCatalog.Find<ConsumableDef>("RepairGel"));
            Assert.AreEqual(9,cap,"fixture: a party carries nine gels");
            State.consumables["RepairGel"]=2;State.scrip=1000;shop.Open(null);yield return null;
            // Off, it is the step the routes had: a press every half second for as long as it runs, more of them than fit.
            var buy=new QualityStep{name="buy repair gel up to the cap",seconds=On?30:9,pickRow="Repair Gel",buttons=new[]{"South"},expectedUi="shop",stock=On?"cap":null,until=On?"stocked":null,pulseSeconds=On?0:.5f};
            yield return Play(Yard(false,false,buy),40);
            Assert.IsTrue(finished,"the stock step never ended");
            Assert.AreEqual(cap,State.consumables["RepairGel"],"the pouch wasn't filled to what it carries");
            Assert.AreEqual(1000-(cap-2)*offer.price,State.scrip,"scrip went on something other than the seven gels that fit");
            StringAssert.StartsWith("Purchased",Said(),"the last thing the stall said was a refusal: the pad was pressed past the cap");
            CollectionAssert.IsEmpty(Failures,"a pouch filled to its cap left a failure on the run");
            var record=Records("stocks").Single();
            StringAssert.Contains("\"before\":2",record);StringAssert.Contains("\"after\":9",record);StringAssert.Contains("\"presses\":7",record);StringAssert.Contains("\"refused\":false",record);
        }
        [UnityTest] public IEnumerator AStockStepStopsAtTheFirstPressThatBuysNothingAndSaysSoInTheEvidence()
        {
            yield return Arena();var shop=ZoneController.Current.GetComponent<ShopUi>();
            var offer=ShopUi.Stock(ShopUi.Outfitter,State).First(o=>o.id=="RepairGel");
            // Scrip for two and a bit.
            State.consumables["RepairGel"]=2;State.scrip=offer.price*2+5;shop.Open(null);yield return null;
            var buy=new QualityStep{name="buy repair gel up to the cap",seconds=On?30:9,pickRow="Repair Gel",buttons=new[]{"South"},expectedUi="shop",stock=On?"cap":null,until=On?"stocked":null,pulseSeconds=On?0:.5f};
            float began=Time.unscaledTime;
            yield return Play(Yard(false,false,buy),40);
            Assert.IsTrue(finished,"the stock step never ended");
            Assert.AreEqual(4,State.consumables["RepairGel"],"the two gels the scrip covers weren't bought");
            Assert.AreEqual(5,State.scrip);
            var records=Records("stocks");
            Assert.AreEqual(1,records.Count,"a pouch that came up short of its number isn't on the run's record: it would be found out at a boss");
            StringAssert.Contains("\"after\":4",records[0]);StringAssert.Contains("\"refused\":true",records[0]);StringAssert.Contains("\"presses\":3",records[0]);
            Assert.Less(Time.unscaledTime-began,20,"the step ran on after the stall said no");
            CollectionAssert.IsEmpty(Failures,"a stall that ran the party out of scrip rejected the run: it buys what it can, as a player does");
        }

        // ------------------------------------------------------------------ item 3: the EXTRA, and the rotation
        [UnityTest] public IEnumerator AFullGaugeGoesOnThreeEnemiesOrABossAndNotOnTwo()
        {
            yield return Arena();GameServices.Current.Flags.SetBool(ExtraGauge.Flag,true);
            var lead=Lead;var move=lead.GetComponent<ExtraMove>();Assert.IsNotNull(move.Definition,"fixture: Taren's EXTRA move is defined");
            var at=lead.transform.position;var crowd=new List<EnemyBrain>{Dummy("Ridgehound",at+Vector3.forward*3),Dummy("Ridgehound",at+new Vector3(1.5f,0,4))};yield return null;
            lead.target=crowd[0].Health;lead.TargetLocked=true;Member("Taren").extra=ExtraGauge.Full;
            yield return Hands(Yard(extra:true));Step(new QualityStep{name="a round of the fight",seconds=120});
            yield return Drive(2.5f);
            Assert.AreEqual(0,move.Released,"a full gauge went on two scavengers");
            Assert.AreEqual(ExtraGauge.Full,ExtraGauge.Of("Taren"),.001f,"fixture: the gauge is still full");
            crowd.Add(Dummy("Ridgehound",at+new Vector3(-1.5f,0,4)));
            yield return Drive(5,()=>move.Released>0);
            Assert.AreEqual(1,move.Released,"three enemies under the hammers and a full gauge, and the EXTRA wasn't released");
            Assert.AreEqual(0,ExtraGauge.Of("Taren"),.001f,"the EXTRA went and the gauge wasn't spent");
            yield return Drive(8,()=>!move.Performing);Assert.IsFalse(move.Performing,"fixture: the first EXTRA has finished");
            // A boss alone is reason enough.
            foreach(var body in crowd)Object.Destroy(body.gameObject);yield return null;
            var boss=Dummy("Burrower",lead.transform.position+Vector3.forward*5);yield return Frames(3);
            lead.target=boss.Health;lead.TargetLocked=true;Member("Taren").extra=ExtraGauge.Full;
            yield return Drive(6,()=>move.Released>1);
            Assert.AreEqual(2,move.Released,"a boss in reach and a full gauge, and the EXTRA wasn't released");
        }
        /// <summary>A check of what was already there, not of a change: the routes' rotation is RB and a face button, which presses a slot whatever the Skills page has
        /// put in it. With more than the first eight skills learned, a skill of Sync 16 in a slot is what goes. (It passes with the knobs off as well.)</summary>
        [UnityTest] public IEnumerator TheRotationPressesSlotsSoANewerSkillInASlotIsWhatGoes()
        {
            yield return Arena(16);var lead=Lead;
            Assert.Greater(SkillBook.Learned("Taren").Length+SkillBook.Learned("Sela").Length,8,"fixture: at Sync 16 the party has learned more than the eight skills it began with");
            var late=SkillBook.All("Taren").FirstOrDefault(s=>s.unlockLevel==16);Assert.IsNotNull(late,"fixture: Taren learns a skill at Sync 16");
            Assert.IsTrue(SkillBook.Equip(Member("Taren"),1,late),"fixture: the Sync 16 skill goes on the second slot, as the Skills page puts it there");
            var body=Dummy("Ridgehound",lead.transform.position+Vector3.forward*2.5f);yield return null;lead.target=body.Health;lead.TargetLocked=true;
            var steps=new List<QualityStep>();
            foreach(string face in new[]{"South","East","West"})
            {
                steps.Add(new QualityStep{name="skill "+face,seconds=.18f,buttons=new[]{"RightShoulder",face}});
                steps.Add(new QualityStep{name="release skill "+face,seconds=2});
            }
            yield return Play(Yard(false,false,steps.ToArray()),20,()=>lead.charge=100);
            Assert.IsTrue(finished,"the rotation never ended");
            var released=log.Where(l=>l.StartsWith("SKILL_KIND Taren ")).Select(l=>l.Split(' ')[2]).ToArray();
            CollectionAssert.AreEqual(new[]{SkillBook.Equipped("Taren",0).id,late.id,SkillBook.Equipped("Taren",2).id},released,"RB and a face button didn't release what each slot holds, the newer skill among them");
        }

        // ------------------------------------------------------------------ item 5: a boss lost at its door
        /// <summary>The QA chains were resumed by hand for this, twice. The game gives a fight lost at a boss's door back whole; the driver now takes it, with the pad.</summary>
        [UnityTest] public IEnumerator ABossLostAtItsDoorIsTakenAgainByThePadAndEveryAttemptIsOnTheRecord()
        {
            const string boss="Burrower",zone="Sorrel_Ridges",doorId="Sorrel_Burrower";
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            void Still(){foreach(var brain in All<EnemyBrain>())brain.Passive=true;foreach(var hero in PartyController.Current.members)hero.GetComponent<PartnerBrain>().enabled=false;}
            Still();yield return new WaitForSecondsRealtime(.7f);
            EncounterVolume Door()=>All<EncounterVolume>().Single(e=>e.encounterId==doorId);
            string checkpoint=Door().checkpointSpawn;Assert.IsFalse(string.IsNullOrEmpty(checkpoint),"fixture: the Burrower's door is a checkpoint");
            var route=new QualityRoute{name="a boss's door",scene=zone,steps=new[]
            {
                new QualityStep{name="enter the excavation",seconds=1,bossDoor=On?boss:null},
                new QualityStep{name="fight the Burrower",seconds=30,stopWhen="flag:bossdown."+boss},
            }};
            int phase=0,frames=0;
            yield return Play(route,80,()=>
            {
                int at=Get<int>("stepIndex");var party=PartyController.Current;
                if(phase==0&&at==1){Door().Begin();phase=1;frames=0;}
                else if(phase==1&&++frames==3)
                {
                    // The first attempt is lost as it starts: the boss out and standing, and both heroes down.
                    Still();foreach(var hero in party.members)Fell(hero);phase=2;
                }
                else if(phase==2&&at==0&&!Get<bool>("retrying")&&party!=null&&party.members.All(m=>m.Health.Alive)){Still();phase=3;}
                // The second is won as it starts: this is about the way back to the door, not the fight.
                else if(phase==3&&at==1){GameServices.Current.Flags.SetBool("bossdown."+boss,true);phase=4;}
            });
            Assert.IsTrue(finished,"the route never ended");
            var panel=Object.FindFirstObjectByType<DefeatPanel>();
            Assert.IsFalse(panel!=null&&panel.IsOpen,"the party fell at the Burrower's door and the defeat screen was left standing: the chain rejects the segment");
            Assert.AreEqual(4,phase,"the route didn't come back through the door's step and into the fight after the retry");
            Assert.IsFalse(GameTime.Paused,"the retry left the game paused");
            Assert.AreEqual(zone,State.zone,"the retry went back to another zone");
            Assert.AreEqual(checkpoint,State.spawn,"the retry didn't come back at the door's checkpoint");
            foreach(var hero in PartyController.Current.members)Assert.AreEqual(hero.Health.maximum,hero.Health.integrity,1,hero.character+" came back from the retry wounded");
            var attempts=Records("bossAttempts");
            Assert.AreEqual(2,attempts.Count,"the run's record doesn't hold both attempts at the boss");
            StringAssert.Contains("\"outcome\":\"defeat\"",attempts[0]);StringAssert.Contains("\"attempt\":1",attempts[0]);StringAssert.Contains("\"lastStepName\":\"fight the Burrower\"",attempts[0]);
            StringAssert.Contains("\"outcome\":\"won\"",attempts[1]);StringAssert.Contains("\"attempt\":2",attempts[1]);
            Assert.IsTrue(log.Any(l=>l.StartsWith("QUALITY_BOSS_RETRY "+boss)),"the player log doesn't say the boss was retried");
            CollectionAssert.IsEmpty(Failures,"a retry the game itself offers left a failure on the run");
        }
    }
}
