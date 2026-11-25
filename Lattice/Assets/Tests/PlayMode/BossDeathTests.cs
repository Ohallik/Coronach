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
    /// <summary>A beaten boss shows it at once (D355). In the whole-game capture of November 22 the Rotbloom stood whole and upright for three seconds after its integrity reached
    /// nought, with Taren inside its petals (s26/454 to 469): its collapse was a squash in height, which the fixed camera looking down on a flat bloom cannot see. The Bellows kept
    /// its cyan seams lit (s08/092 to 098). Every boss here is beaten in an arena and its body drawn as the game draws it (its own shader and the room's light), from the game's
    /// framing, on a layer of its own over a flat colour, a second before and a second after; what is measured is the share of the pixels its body covered that changed colour by
    /// more than a tenth or stopped being its body (one that yields is judged by the hand-over to the person instead). Within that second nothing lands on it or on its parts, no
    /// damage number spawns on it, and the lead's lock has let go.
    /// When its body is gone is written for the lead's table (BOSS_DEATH).</summary>
    public sealed class BossDeathTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();ImpactSettings.Reset();
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();ImpactSettings.Reset();
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        /// <summary>Every EnemyDef with boss set (checked against the catalog by <see cref="EveryBossIsInTheList"/>).</summary>
        static readonly string[] All={"BellowsBelow","Burrower","Cantor","DebrisCutter","DessikVarn","DraskSkiff","DraskVaal","Floodcrown","FoundrySaint","HessaKorl","HostMarshal",
            "Hulljack","LastWarden","MeretGantry","MeretShip","MeretWell","MotherOfDoors","Rotbloom","Seedthane","SlagColossus","Tollkeeper","VessaKorl"};
        static IEnumerable<string> Bosses()=>All;
        /// <summary>Fought in flight: beaten in the flight arena.</summary>
        static readonly HashSet<string> Fliers=new(){"Cantor","DebrisCutter","DraskSkiff","MeretShip"};
        /// <summary>The least share of what its body covered a second before that has changed a second after.</summary>
        const float Shows=.3f;
        /// <summary>Who yields is handed over to the person within this (DefeatPresentation's .35 s and .75 s beat, and a kill's hit stop).</summary>
        const float YieldsWithin=1.5f;
        const int Layer=31,W=480,H=270;

        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+20;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.7f);
        }
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            if(actor.motor is GroundMotor ground)ground.KeepFacing(Vector3.forward);
            if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            Physics.SyncTransforms();
        }
        /// <summary>Puts a body at nought now, whatever holds its bar up (a plate, a held line, a shield), with a hero's finishing blow as the routes end a fight: weighted, so the
        /// impact layer answers it as it does in play (its flash, its stop). D385: unweighted, the killing blow lit nothing here, and the pale first frame the player showed
        /// (nov23-whole s49/052, s36/071) could not be seen.</summary>
        static void Beat(EnemyBrain body,CombatActor by)
        {
            var health=body.Health;health.Damageable=true;health.InvulnerableUntil=0;health.ShieldUntil=0;health.Plate=null;health.Holds=null;
            health.Receive(new DamagePacket{source=by.Health,amount=1e7f,type=DamageType.Kinetic,weight=HitWeight.Finisher});
        }
        /// <summary>Endings whose first frame is their own presentation's, not a body going dark: who yields hands over to the person, the freed Cantor swims out (CantorRelease), the
        /// Burrower burns out to its wreck's tint frame by frame (QaFightThirdTests), and the Last Warden stands down unbeaten.</summary>
        static bool OwnFirstFrame(string id)=>BossController.EndingOf(id) is BossController.Ending.Yields or BossController.Ending.Released||id=="Burrower"||id=="LastWarden";
        /// <summary>Its drawn body only, on a layer of its own: everything with a renderer under it, and a serpent's sections wherever they hang.</summary>
        static List<Renderer> Drawn(EnemyBrain boss)
        {
            var list=boss.GetComponentsInChildren<Renderer>(true).Where(r=>!(r is ParticleSystemRenderer)&&!(r is LineRenderer)&&!(r is TrailRenderer)&&r.name!="Telegraph").ToList();
            if(boss.TryGetComponent<SerpentSegments>(out var segments))foreach(var part in segments.Parts)if(part!=null)foreach(var r in part.GetComponentsInChildren<Renderer>(true))if(!list.Contains(r))list.Add(r);
            return list;
        }
        static Bounds Box(List<Renderer> drawn)
        {
            bool any=false;var box=new Bounds();
            foreach(var r in drawn){if(r==null||!r.enabled||!r.gameObject.activeInHierarchy)continue;if(!any){box=r.bounds;any=true;}else box.Encapsulate(r.bounds);}
            return box;
        }
        static readonly Color Ground=new(1,0,1,1);
        /// <summary>The game's ground framing (pitch 40, yaw 20, a 30 degree lens), as far off as the body needs to fill about half the frame.</summary>
        static Camera Eye(Bounds box)
        {
            var camera=new GameObject("Boss death fixture camera").AddComponent<Camera>();camera.enabled=false;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Ground;camera.cullingMask=1<<Layer;camera.fieldOfView=30;camera.nearClipPlane=.3f;camera.farClipPlane=400;
            float reach=Mathf.Max(1.5f,box.extents.magnitude),distance=Mathf.Max(12,reach/Mathf.Tan(15*Mathf.Deg2Rad)*1.4f);
            var turn=Quaternion.Euler(40,20,0);camera.transform.SetPositionAndRotation(box.center-turn*Vector3.forward*distance,turn);
            return camera;
        }
        static readonly int Aerial=Shader.PropertyToID("_FbAerialDist");
        /// <summary>Its body as the game draws it, without the room's fog and distance haze: framed from far enough to hold a body 90 m long (the Tollkeeper's carrier), the haze
        /// filled it with one flat colour and nothing it did could show.</summary>
        static Color[] Shot(Camera camera)
        {
            var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);Assert.IsTrue(rt.Create());var active=RenderTexture.active;
            bool fog=RenderSettings.fog;var aerial=Shader.GetGlobalVector(Aerial);
            try
            {
                RenderSettings.fog=false;Shader.SetGlobalVector(Aerial,new Vector4(aerial.x,aerial.y,0,aerial.w));
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(W,H,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();var pixels=image.GetPixels();Object.Destroy(image);return pixels;
            }
            finally{RenderSettings.fog=fog;Shader.SetGlobalVector(Aerial,aerial);camera.targetTexture=null;RenderTexture.active=active;Object.Destroy(rt);}
        }
        static void Save(Color[] pixels,string still)
        {
            var image=new Texture2D(W,H,TextureFormat.RGB24,false);image.SetPixels(pixels);image.Apply();
            string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/focused/boss-death"));System.IO.Directory.CreateDirectory(folder);
            string file=System.IO.Path.Combine(folder,still+".png");System.IO.File.WriteAllBytes(file,image.EncodeToPNG());Object.Destroy(image);TestContext.WriteLine("BOSS_DEATH_STILL "+file);
        }
        /// <summary>Not the flat colour behind it, as the pipeline wrote it: read from the frame's own corner, which no body reaches (the colour asked for comes back graded, and the
        /// flight arena's otherwise).</summary>
        static bool Body(Color c,Color ground)=>Mathf.Abs(c.r-ground.r)+Mathf.Abs(c.g-ground.g)+Mathf.Abs(c.b-ground.b)>.06f;
        /// <summary>Of the pixels its body covered in <paramref name="before"/>, the share that in <paramref name="after"/> changed by more than a tenth in any channel or show the ground.</summary>
        static float Changed(Color[] before,Color[] after,out int covered)
        {
            covered=0;int changed=0;var ground=before[0];
            for(int i=0;i<before.Length;i++)
            {
                if(!Body(before[i],ground))continue;covered++;var a=before[i];var b=after[i];
                if(!Body(b,ground)||Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Max(Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b)))>.1f)changed++;
            }
            return covered>0?changed/(float)covered:0;
        }
        static int Covered(Color[] pixels){int n=0;var ground=pixels[0];foreach(var c in pixels)if(Body(c,ground))n++;return n;}
        /// <summary>How light what shows its body looks, on average (luma, 0 to 1).</summary>
        static float Lightness(Color[] pixels,Color ground){double sum=0;int n=0;foreach(var c in pixels)if(Body(c,ground)){sum+=.299f*c.r+.587f*c.g+.114f*c.b;n++;}return n>0?(float)(sum/n):0;}
        /// <summary>A body that goes dark (BodyGoesDark: killed, shut down or driven off) looks at most this light a second after as it did alive. D363: the ash it went to was paler
        /// than the body, the Slag Colossus's dark iron .21 to .30 (nov22b-whole s49/049, a pale ochre shell).</summary>
        const float Darker=.8f;

        IEnumerator Arena(string id)
        {
            bool flier=Fliers.Contains(id);
            yield return Zone(flier?"Arena_Flight":"Arena_Ground");
            if(!flier){var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Boss death fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(160,1,160);Physics.SyncTransforms();}
        }
        Vector3 BossAt(string id)=>Fliers.Contains(id)?new Vector3(40,PartyController.Current.Active.transform.position.y,10):new Vector3(232,0,14);
        Vector3 LeadAt(string id)=>Fliers.Contains(id)?new Vector3(40,PartyController.Current.Active.transform.position.y,-14):new Vector3(232,0,-8);

        /// <summary>Every boss the catalog has is one this class beats.</summary>
        [UnityTest,Timeout(60000)] public IEnumerator EveryBossIsInTheList()
        {
            yield return null;
            var bosses=GameCatalog.All<EnemyDef>().Where(d=>d.boss).Select(d=>d.id).OrderBy(i=>i,System.StringComparer.Ordinal).ToArray();
            Assert.IsNotEmpty(bosses,"fixture: the catalog has its bosses");
            CollectionAssert.AreEquivalent(bosses,All,"BossDeathTests.All must name every boss: add a new one there, so its ending is checked");
        }

        /// <summary>D363: a net of Sela's that lands after its fight is won lies for no time. Begun as the Bellows died, hers landed after the win had cleared the field and lay round the
        /// dead dome for four seconds (nov22b-whole s08/098 to 104). One laid with a foe in reach lies its life out.</summary>
        [UnityTest,Timeout(60000)] public IEnumerator ANetLandingAfterTheFightIsWonLiesForNoTime()
        {
            yield return Arena("Rotbloom");
            var lead=PartyController.Current.Active;Place(lead,LeadAt("Rotbloom"));
            var packet=new DamagePacket{source=lead.Health,amount=10,type=DamageType.Pulse};
            var standing=Health.All.Where(h=>h!=null&&h.Alive&&h.Damageable&&!h.friendly&&(h.transform.position-BossAt("Rotbloom")).magnitude<40).Select(h=>h.id).ToList();
            Assert.IsEmpty(standing,"fixture: the arena has no foe left standing near the net");
            NetSeed.Land(BossAt("Rotbloom"),packet);
            yield return new WaitForSecondsRealtime(.6f);
            int afterWin=Object.FindObjectsByType<PulseField>(FindObjectsSortMode.None).Length;
            var foe=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Rotbloom"),BossAt("Rotbloom")+new Vector3(0,0,6));foe.Passive=true;
            foe.Health.InvulnerableUntil=float.PositiveInfinity;
            yield return new WaitForSecondsRealtime(.3f);
            NetSeed.Land(BossAt("Rotbloom"),packet);
            yield return new WaitForSecondsRealtime(.6f);
            int inFight=Object.FindObjectsByType<PulseField>(FindObjectsSortMode.None).Length;
            Assert.AreEqual(0,afterWin,"a net laid with no foe left in reach still lay on the floor .6 s later");
            Assert.AreEqual(1,inFight,"fixture: a net laid with a foe in reach lies its life out");
        }

        /// <summary>D385: a net laid in the fight, round the boss, goes on the frame the fight ends. Dismissed over a quarter second, Sela's cyan ring stood round the dead Bellows at
        /// full strength in the still the route took as the fight ended, the rewards already up (nov23-whole s08/092), and round the driven-off Hulljack (s29/034).</summary>
        [UnityTest,Timeout(60000)] public IEnumerator ANetLaidInTheFightGoesOnTheFrameItEnds()
        {
            yield return Arena("BellowsBelow");
            var lead=PartyController.Current.Active;Place(lead,LeadAt("BellowsBelow"));lead.Health.InvulnerableUntil=float.PositiveInfinity;
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("BellowsBelow"),BossAt("BellowsBelow"));boss.Passive=true;
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(boss!=null&&boss.Health.Alive,"fixture: the Bellows stands");
            NetSeed.Land(BossAt("BellowsBelow"),new DamagePacket{source=lead.Health,amount=10,type=DamageType.Pulse});
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(1,Object.FindObjectsByType<PulseField>(FindObjectsSortMode.None).Length,"fixture: a net laid round a living boss lies");
            Beat(boss,lead);
            yield return null;
            int left=Object.FindObjectsByType<PulseField>(FindObjectsSortMode.None).Length;
            Assert.AreEqual(0,left,"the frame after the Bellows' bar emptied, the net laid in its fight still lay round it");
        }

        /// <summary>D385: on a beaten boss only its killing blow's number finishes its rise; any other hero's number on it goes with the end. The Bellows wore '616' and '438' over its
        /// rewards (nov23-whole s08/092), the Host Marshal '1514' and '828!' (s36/071): the partner's number stood on beside the killing one.</summary>
        [UnityTest,Timeout(60000)] public IEnumerator OnlyTheKillingBlowsNumberStaysOnABeatenBoss()
        {
            yield return Arena("BellowsBelow");
            var lead=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=lead);
            Place(lead,LeadAt("BellowsBelow"));Place(partner,LeadAt("BellowsBelow")+new Vector3(3,0,0));
            foreach(var member in PartyController.Current.members)member.Health.InvulnerableUntil=float.PositiveInfinity;
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("BellowsBelow"),BossAt("BellowsBelow"));boss.Passive=true;
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(boss!=null&&boss.Health.Alive,"fixture: the Bellows stands");
            boss.Health.ShieldUntil=0;
            Assert.Greater(boss.Health.Receive(new DamagePacket{source=partner.Health,amount=300,type=DamageType.Kinetic,weight=HitWeight.Light}),0,"fixture: the partner's blow lands");
            yield return null;
            int living=Numbers();
            Assert.AreEqual(1,living,"fixture: the partner's blow shows its number on the living boss");
            Beat(boss,lead);
            yield return null;yield return null;
            int beaten=Numbers();
            Assert.AreEqual(1,beaten,$"two frames after the Bellows' bar emptied, {beaten} numbers stood on it: only the killing blow's may finish its rise");
        }
        /// <summary>The damage numbers on the HUD now (GameHud's, each a text named Damage).</summary>
        static int Numbers()=>Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Count(t=>t!=null&&t.name=="Damage"&&t.gameObject.activeInHierarchy&&t.alpha>0&&t.text!="");

        /// <summary>D385: released, the Foundry Saint is powered down, arms and all: a second after its bar empties its six arms are down and darker than they stood in the fight.
        /// Its arms are the room's, not under its root, and lay in full gold and cream through the loot lines and Orrin's call (nov23-whole s59/038, 043).</summary>
        [UnityTest,Timeout(90000)] public IEnumerator TheReleasedSaintsArmsGoDarkWithIt()
        {
            LogAssert.ignoreFailingMessages=true;
            yield return Arena("FoundrySaint");
            var lead=PartyController.Current.Active;Place(lead,LeadAt("FoundrySaint"));
            foreach(var member in PartyController.Current.members)member.Health.InvulnerableUntil=float.PositiveInfinity;
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("FoundrySaint"),BossAt("FoundrySaint"));boss.Passive=true;
            boss.transform.rotation=Quaternion.LookRotation(LeadAt("FoundrySaint")-BossAt("FoundrySaint"));
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsTrue(boss!=null&&boss.Health.Alive,"fixture: the Saint stands");
            var arms=SaintArm.All.Where(a=>a!=null&&(a.transform.position-boss.transform.position).magnitude<14).ToList();
            Assert.AreEqual(BossController.SaintTune.Arms,arms.Count,"fixture: the Saint has its six arms");
            List<Renderer> ArmsDrawn()=>arms.SelectMany(a=>a.GetComponentsInChildren<Renderer>(true)).Where(r=>!(r is LineRenderer)&&r.name!="Collar ring").ToList();
            // Framed on where they will lie as well as where they stand: their box now and the press's reach round it.
            var drawn=ArmsDrawn();foreach(var r in drawn)r.gameObject.layer=Layer;
            var box=Box(drawn);box.Encapsulate(new Bounds(boss.transform.position,Vector3.one*18));var camera=Eye(box);
            var before=Shot(camera);float lightBefore=Lightness(before,before[0]);
            Beat(boss,lead);
            float died=Time.unscaledTime;while(Time.unscaledTime-died<SaintArm.Tune.Falls+.6f)yield return null;
            foreach(var r in ArmsDrawn())r.gameObject.layer=Layer;
            var after=Shot(camera);float lightAfter=Lightness(after,before[0]);
            Save(before,"saint-arms-0-before");Save(after,"saint-arms-1-after");Object.Destroy(camera.gameObject);
            LogAssert.ignoreFailingMessages=false;
            string line=$"BOSS_DEATH_SAINT_ARMS light {lightBefore:0.00} standing -> {lightAfter:0.00} down; powered down {arms.Count(a=>a.PoweredDown)} of {arms.Count}";
            TestContext.WriteLine(line);Debug.Log(line);
            Assert.IsTrue(arms.All(a=>a.Resting),line+": its arms are not all down and still");
            Assert.LessOrEqual(lightAfter,lightBefore*Darker,line+": released, its arms look as light as they did in the fight");
        }

        /// <summary>A boss beaten shows it within a second, and from that moment nothing lands on it or its parts, no number spawns on it, and the lead's lock lets go. Shows it: one
        /// that is killed, shut down, driven off or freed has changed at least <see cref="Shows"/> of the pixels its body covered (and a tenth more than it changes alive in a second,
        /// for a body that hovers); one that yields is handed over to the person she is within <see cref="YieldsWithin"/> s (DefeatPresentation.Yielded).</summary>
        [UnityTest,Timeout(150000)] public IEnumerator ABeatenBossShowsItWithinASecondAndTakesNothingMore([ValueSource(nameof(Bosses))] string id)
        {
            LogAssert.ignoreFailingMessages=true;// a boss away from its own zone may miss its room's fixtures; this watches its body, its bar and its lock
            yield return Arena(id);
            var lead=PartyController.Current.Active;Place(lead,LeadAt(id));
            foreach(var member in PartyController.Current.members)if(member!=lead)Place(member,LeadAt(id)+new Vector3(-30,0,-10));
            foreach(var member in PartyController.Current.members){member.Health.InvulnerableUntil=float.PositiveInfinity;}
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),BossAt(id));boss.Passive=true;
            // Facing the lead, as it does in its fight: one that leaves turns away from the party, and a body facing away already would show its turn as nothing.
            boss.transform.rotation=Quaternion.LookRotation(LeadAt(id)-BossAt(id));
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(boss!=null&&boss.Health.Alive,"fixture: "+id+" stands");
            var drawn=Drawn(boss);foreach(var r in drawn)r.gameObject.layer=Layer;
            var camera=Eye(Box(drawn));
            // Its living second first: what idling, breathing and hovering change by themselves.
            var rest=Shot(camera);yield return new WaitForSecondsRealtime(1f);
            if(boss==null)Assert.Fail("fixture: "+id+" left before it was beaten");
            foreach(var r in Drawn(boss))r.gameObject.layer=Layer;
            var before=Shot(camera);float living=Changed(rest,before,out int covered);
            Assert.Greater(covered,400,$"fixture: {id} covers only {covered} pixels of the fixture's frame");
            Assert.Less(covered,W*H/2,$"fixture: {id} covers {covered} pixels, over half the frame: the ground colour isn't read");
            // The lead's lock on it, as a player holds it through the last blow; his brain runs, so whatever lets a lock go can.
            lead.target=boss.Health;lead.TargetLocked=true;lead.GetComponent<PlayerBrain>().AutoPilot=false;
            var parts=Health.All.Where(h=>h!=null&&h!=boss.Health&&(h.reactionOwner==boss.Health||h.transform.IsChildOf(boss.transform))).ToList();
            var numbered=new List<string>();
            System.Action<Health,float,HitKind> number=(victim,amount,_)=>{if(victim==boss.Health||parts.Contains(victim))numbered.Add($"{victim.id} {amount:0}");};
            var ending=BossController.EndingOf(id);
            Beat(boss,lead);Assert.IsFalse(boss.Health.Alive,"fixture: "+id+" is at nought");
            float died=Time.unscaledTime,diedOnClock=Time.time,handed=-1;string landed="";
            // D395: a body that stands down (the Last Warden: StandsDown, LowerTheStaff) plays its end on the game's clock, and a boss's end holds that clock for the kill's stop
            // (Impact.Tune.BossKill, a quarter second) as play does: the Warden's own end is the fourth clamp's release, a boss kill by whoever opened it, held the same. Its second is
            // counted on that clock, from where its end begins to move; a body that goes dark does so through the stop (BodyGoesDark) and is counted in real time. D385's weighted
            // finisher measured the Warden .75 s into its 1.2 s bow (28 %, d385b) where the unweighted blow before it, which held nothing, measured a whole second (50 %, d385a).
            bool onClock=false;System.Func<float> since=()=>onClock?Time.time-diedOnClock:Time.unscaledTime-died;
            System.Action<string,Vector3,Quaternion> yielded=(who,_,__)=>{if(who==id&&handed<0)handed=Time.unscaledTime-died;};DefeatPresentation.Yielded+=yielded;
            Health.DamageNumber+=number;
            Color[] half=null,after=null,onEnd=null;float flashFirst=-1;
            try
            {
                // D385: the frame its end came on, as the game drew it (the still a route takes as the fight's step ends is this one): the killing blow's flash and whatever its end
                // has written by then. The Slag Colossus was a pale chrome shell here and black half a second later (nov23-whole s49/052, 054).
                yield return null;
                if(boss!=null){foreach(var r in Drawn(boss))r.gameObject.layer=Layer;onEnd=Shot(camera);flashFirst=boss.TryGetComponent<HitFlash>(out var flash)?flash.Shown:-1;}
                onClock=boss!=null&&boss.GetComponent<StandsDown>()!=null;
                while(since()<1f&&Time.unscaledTime-died<3f)
                {
                    yield return null;
                    // Blows keep coming as a player's would: on its body and on each of its parts.
                    if(boss!=null)
                    {
                        float took=boss.Health.Receive(new DamagePacket{source=lead.Health,amount=500,type=DamageType.Kinetic});
                        if(took>0)landed+=$" {id} took {took:0};";
                        foreach(var part in parts)if(part!=null){float on=part.Receive(new DamagePacket{source=lead.Health,amount=500,type=DamageType.Kinetic});if(on>0)landed+=$" {part.id} took {on:0};";}
                    }
                    if(half==null&&since()>=.5f){if(boss!=null)foreach(var r in Drawn(boss))r.gameObject.layer=Layer;half=Shot(camera);}
                }
                if(boss!=null)foreach(var r in Drawn(boss))r.gameObject.layer=Layer;
                after=Shot(camera);
            }
            finally{Health.DamageNumber-=number;}
            bool locked=lead.TargetLocked&&lead.target==(boss!=null?boss.Health:null);
            var darkening=boss!=null?boss.GetComponent<BodyGoesDark>():null;bool wentDark=darkening!=null;
            string dark=darkening!=null?$"{darkening.Painted} renderers at {darkening.Share:0.00}":"none";
            string shaders=boss!=null?string.Join("+",Drawn(boss).Where(r=>r!=null&&r.sharedMaterial!=null).Select(r=>r.sharedMaterial.shader.name).Distinct().Take(3)):"";
            float shown=Changed(before,after,out _),early=Changed(before,half,out _),lightBefore=Lightness(before,before[0]),lightAfter=Lightness(after,before[0]);
            float lightFirst=onEnd!=null?Lightness(onEnd,before[0]):lightBefore;
            Save(before,id+"-0-before");Save(half,id+"-1-half");Save(after,id+"-2-second");
            // When it is gone: destroyed, or nothing of it drawn.
            string gone="stays";int first=covered;
            for(float end=Time.unscaledTime+7;Time.unscaledTime<end;)
            {
                if(boss==null){gone=$"{Time.unscaledTime-died:0.0}s";break;}
                foreach(var r in Drawn(boss))r.gameObject.layer=Layer;
                if(Covered(Shot(camera))<first*.03f){gone=$"{Time.unscaledTime-died:0.0}s (undrawn)";break;}
                yield return new WaitForSecondsRealtime(.2f);
            }
            Object.Destroy(camera.gameObject);DefeatPresentation.Yielded-=yielded;
            LogAssert.ignoreFailingMessages=false;
            string line=$"BOSS_DEATH {id} ending={ending} clock={(onClock?"game":"real")} living={living:0.00} half-second={early:0.00} second={shown:0.00} handed-over={(handed<0?"no":handed.ToString("0.00")+"s")} dark={dark} light={lightBefore:0.00}->first frame {lightFirst:0.00} (flash {flashFirst:0.00})->{lightAfter:0.00} drawn-with={shaders} numbers={numbered.Count} landed={(landed==""?"none":landed)} lock={(locked?"held":"let go")} gone={gone}";
            TestContext.WriteLine(line);Debug.Log(line);
            var faults=new List<string>();
            // What is killed, shut down, driven off or freed shows it on its body within the second: it darkens, draws in, falls, turns away. Who yields gives up where she stands (Meret
            // Well yields standing by decision: LastMooringBossTests) and the person she is takes over from her body within the beat DefeatPresentation gives a yield.
            if(ending==BossController.Ending.Yields){if(handed<0||handed>YieldsWithin)faults.Add($"{line}: yielding, the person did not take over from the body within {YieldsWithin} s");}
            else if(shown<Shows||shown<living+.1f)faults.Add($"{line}: a second after its bar emptied, {shown:P0} of its body had changed (alive, {living:P0} changes in a second)");
            if(darkening!=null&&lightAfter>lightBefore*Darker)faults.Add($"{line}: going dark, a second after its bar emptied it looks {lightAfter/Mathf.Max(.001f,lightBefore):P0} as light as it did alive");
            // D385: killed, shut down or driven off, it goes dark (the Host Marshal and Mother of Doors had none: s36/076 in full rust, s63/150 in full lilac), and the frame its
            // end comes on is already darker than it lived (s49/052, s36/071, s29/034: pale).
            if(!OwnFirstFrame(id))
            {
                if(!wentDark)faults.Add($"{line}: beaten ({ending}), its body doesn't go dark");
                if(onEnd!=null&&lightFirst>=lightBefore)faults.Add($"{line}: the frame its end came on it looks {lightFirst/Mathf.Max(.001f,lightBefore):P0} as light as it did alive (its killing blow's flash {flashFirst:0.00})");
            }
            if(landed!="")faults.Add(line+": blows still landed on it after its bar emptied:"+landed);
            if(numbered.Count>0)faults.Add(line+": damage numbers spawned on it after its bar emptied: "+string.Join(", ",numbered.Take(4)));
            if(locked)faults.Add(line+": the lead's lock still held it a second after its bar emptied");
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        // ------------------------------------------------------------------ D407: a hero left inside a beaten boss is eased out of it
        /// <summary>Bosses whose body stays where it ends, or reels there before it goes, in flight (the Warmaster's skiff, the Cutter, Meret's ship) and on foot.</summary>
        static readonly string[] Staying={"DraskSkiff","DebrisCutter","MeretShip","SlagColossus","HostMarshal","Rotbloom","BellowsBelow","FoundrySaint","LastWarden","Hulljack"};
        static IEnumerable<string> StayingBosses()=>Staying;
        static float Apart(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        /// <summary>D407: after the Warmaster's skiff was beaten Taren's ship sat inside its darkened hull through the quest card and his own line (nov24-recon-h-52 s52/177 to
        /// 183): he had lunged into it as it ended, and a boss's body is not thrown clear as a small one is (D386). Each boss here is beaten with the lead half a metre from its
        /// middle on his side, as a charge through the party or his own lunge leaves him; a second on he stands clear of its capsule, at his own height or plane.</summary>
        [UnityTest,Timeout(120000)] public IEnumerator ABeatenBossEasesAHeroLeftInsideItOutOfItWithinASecond([ValueSource(nameof(StayingBosses))] string id)
        {
            LogAssert.ignoreFailingMessages=true;// a boss away from its own zone may miss its room's fixtures
            yield return Arena(id);bool flier=Fliers.Contains(id);
            var lead=PartyController.Current.Active;Place(lead,LeadAt(id));
            foreach(var member in PartyController.Current.members){if(member!=lead)Place(member,LeadAt(id)+new Vector3(-30,0,-10));member.Health.InvulnerableUntil=float.PositiveInfinity;}
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),BossAt(id));boss.Passive=true;
            boss.transform.rotation=Quaternion.LookRotation(LeadAt(id)-BossAt(id));
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(boss!=null&&boss.Health.Alive,"fixture: "+id+" stands");
            var capsule=boss.GetComponent<CharacterController>();var own=lead.GetComponent<CharacterController>();
            var way=LeadAt(id)-BossAt(id);way.y=0;way.Normalize();
            var inside=boss.transform.TransformPoint(capsule.center)+way*.5f;inside.y=lead.transform.position.y;
            Place(lead,inside);float height=lead.transform.position.y,its=capsule.radius*boss.transform.lossyScale.x,clear=its+own.radius;
            Beat(boss,lead);
            yield return new WaitForSecondsRealtime(1f);
            float apart=boss!=null?Apart(lead.transform.position,boss.transform.TransformPoint(capsule.center)):float.PositiveInfinity;
            LogAssert.ignoreFailingMessages=false;
            string line=$"D407 {id}: a second after its end the lead stands {apart:0.00} m from its middle, clear at {clear:0.00} (its {its:0.00}, his {own.radius:0.00}); his height moved {lead.transform.position.y-height:0.00} m";
            TestContext.WriteLine(line);
            Assert.GreaterOrEqual(apart,clear-.05f,line+": he is still inside its body");
            Assert.AreEqual(height,lead.transform.position.y,flier?.05f:.3f,line+": easing him out took him off his floor or his plane");
        }
        /// <summary>D407: eased out, a hero goes only over floor at his own height and round what is solid. Inside the Host Marshal on a deck a metre up, its edge a step behind
        /// him and a wall at his side, he comes out along the deck the other way: not off it, not through the wall.</summary>
        [UnityTest,Timeout(120000)] public IEnumerator EasedOutOfABeatenBossAHeroIsNeverPutOffADeckOrThroughAWall()
        {
            LogAssert.ignoreFailingMessages=true;
            const string id="HostMarshal";
            yield return Arena(id);
            var deck=GameObject.CreatePrimitive(PrimitiveType.Cube);deck.name="D407 deck";deck.transform.position=new Vector3(232,.5f,21.15f);deck.transform.localScale=new Vector3(24,1,13.7f);
            Physics.SyncTransforms();
            var lead=PartyController.Current.Active;Place(lead,new Vector3(226,1,25));
            foreach(var member in PartyController.Current.members){if(member!=lead)Place(member,new Vector3(202,0,-18));member.Health.InvulnerableUntil=float.PositiveInfinity;}
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),new Vector3(232,1,15));boss.Passive=true;boss.transform.rotation=Quaternion.LookRotation(Vector3.back);
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(boss!=null&&boss.Health.Alive,"fixture: the Marshal stands");
            var capsule=boss.GetComponent<CharacterController>();var own=lead.GetComponent<CharacterController>();
            var middle=boss.transform.TransformPoint(capsule.center);var inside=new Vector3(middle.x,1,middle.z-.5f);
            // The deck's near edge runs .2 m behind where he stands; a wall stands a hand's width off his east side.
            float edge=deck.transform.position.z-6.85f;deck.transform.position+=Vector3.forward*(inside.z-.2f-edge);edge=inside.z-.2f;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="D407 wall";float wallX=inside.x+own.radius+.05f;
            wall.transform.position=new Vector3(wallX+.15f,2.5f,inside.z+2);wall.transform.localScale=new Vector3(.3f,3,10);
            Physics.SyncTransforms();
            Place(lead,inside);float its=capsule.radius*boss.transform.lossyScale.x,clear=its+own.radius;
            Assert.IsTrue(Physics.Raycast(lead.transform.position+Vector3.up*.5f,Vector3.down,1,~0,QueryTriggerInteraction.Ignore),"fixture: the lead doesn't stand on the deck");
            Beat(boss,lead);
            yield return new WaitForSecondsRealtime(1.2f);
            var at=lead.transform.position;float apart=boss!=null?Apart(at,boss.transform.TransformPoint(capsule.center)):float.PositiveInfinity;
            bool floored=Physics.Raycast(at+Vector3.up*.5f,Vector3.down,1,~0,QueryTriggerInteraction.Ignore);
            LogAssert.ignoreFailingMessages=false;
            string line=$"D407 {id} on a deck: a second after its end the lead stands at {at} ({apart:0.00} m from its middle, clear at {clear:0.00}), the edge at z {edge:0.00}, the wall at x {wallX:0.00}";
            TestContext.WriteLine(line);
            Assert.GreaterOrEqual(apart,clear-.05f,line+": he is still inside its body");
            Assert.IsTrue(floored&&Mathf.Abs(at.y-1)<.3f,line+": he was put off the deck");
            Assert.Less(at.x,wallX,line+": he was put through the wall");
        }

        /// <summary>The game's own camera as it stands, drawn to a 1920 by 1080 still for the lead to open beside the capture's (Builds/quality/focused/boss-death in the test's mirror).</summary>
        internal static void GameStill(string still)
        {
            var camera=Camera.main;Assert.IsNotNull(camera,"fixture: the game has its camera");var target=camera.targetTexture;
            var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);Assert.IsTrue(rt.Create());var active=RenderTexture.active;
            try
            {
                camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/focused/boss-death"));System.IO.Directory.CreateDirectory(folder);
                string file=System.IO.Path.Combine(folder,still+".jpg");System.IO.File.WriteAllBytes(file,image.EncodeToJPG(88));TestContext.WriteLine("BOSS_DEATH_STILL "+file);Object.Destroy(image);
            }
            finally{camera.targetTexture=target;RenderTexture.active=active;Object.Destroy(rt);}
        }
        static void Hold(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=float.PositiveInfinity;}}
        static void Face(CombatActor actor,Vector3 at,Vector3 facing)
        {
            Place(actor,at);facing.y=0;if(facing.sqrMagnitude<.001f)return;
            if(actor.motor is GroundMotor ground)ground.KeepFacing(facing.normalized);actor.transform.rotation=Quaternion.LookRotation(facing.normalized);
        }
        static float InHeroes(EnemyBrain body)
        {
            if(body==null)return 0;var points=BodiesKeepApartTests.DrawnPoints(body);float deepest=0;
            foreach(var hero in PartyController.Current.members)if(!hero.flight)deepest=Mathf.Max(deepest,BodiesKeepApartTests.Inside(points,hero));
            return deepest;
        }
        /// <summary>A boss beaten in its own room with the heroes where the capture had them, drawn from the game's camera alive, half a second after and a second after: within the second
        /// none of what it draws is left inside either hero.</summary>
        IEnumerator InItsRoom(string loadout,string zone,string spawn,string id,Vector3 bossAt,Vector3 leadAt,Vector3 partnerAt,string still,string[] flags)
        {
            LogAssert.ignoreFailingMessages=true;
            DevLoadout.Apply(loadout);foreach(var flag in flags)GameServices.Current.Flags.SetBool(flag,true);
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var volume in Object.FindObjectsByType<Lattice.World.EncounterVolume>(FindObjectsSortMode.None))foreach(var box in volume.GetComponents<Collider>())box.enabled=false;
            Hold();yield return new WaitForSecondsRealtime(.6f);
            for(int i=0;i<4&&PartyController.Current.Active.character!="Taren";i++){PartyController.Current.Swap();Hold();yield return null;}
            var lead=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=lead);
            Face(lead,leadAt,bossAt-leadAt);Face(partner,partnerAt,bossAt-partnerAt);
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),bossAt);boss.Passive=true;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsTrue(boss!=null&&boss.Health.Alive,"fixture: "+id+" stands in its room");
            float alive=InHeroes(boss);GameStill(still+"-0-alive");
            Beat(boss,lead);float died=Time.unscaledTime;
            while(Time.unscaledTime-died<.5f)yield return null;GameStill(still+"-1-half-second");
            while(Time.unscaledTime-died<1f)yield return null;GameStill(still+"-2-second");float second=InHeroes(boss);
            while(Time.unscaledTime-died<2f)yield return null;GameStill(still+"-3-two-seconds");
            LogAssert.ignoreFailingMessages=false;
            string line=$"BOSS_DEATH_ROOM {id} in {zone}: alive, {alive:0.00} m of it inside a hero; a second after, {second:0.00} m";
            TestContext.WriteLine(line);Debug.Log(line);
            Assert.LessOrEqual(second,.03f,line+": a second after its bar emptied a hero still stood in what it draws");
        }
        /// <summary>The Rotbloom in its great hall, Taren where the capture's stood in its left petal (nov22-whole s26/464, -3.5, 283.8) and Sela beside him: it folds in off them.</summary>
        [UnityTest,Timeout(150000)] public IEnumerator TheRotbloomFoldsOffTheHeroesInItsHallWithinASecond()=>InItsRoom("chapter4","DrownedWaystation","Door","Rotbloom",
            new Vector3(0,-1.6f,289),new Vector3(-3.53f,-1.61f,283.78f),new Vector3(-6.33f,-1.61f,283.08f),"rotbloom-hall",
            new[]{"wend.tideTurned","wend.lureLearned","wend.pump1","wend.pump2","wend.pimFound","wend.pump3"});
        /// <summary>The Bellows in its chamber, Taren against its south-east side and Sela behind him (nov22-whole s08/089 to 098): its seams go out and it draws in off him.</summary>
        [UnityTest,Timeout(150000)] public IEnumerator TheBellowsGoesDarkAndDrawsInOffTheHeroInItsChamber()
        {
            var at=Lattice.World.HushwellLayout.OnFloor(Lattice.World.HushwellLayout.Bellows);
            return InItsRoom("hushwell","Hushwell","Arrival","BellowsBelow",at,at+new Vector3(2.1f,0,-2.1f),at+new Vector3(6,0,-6),"bellows-chamber",new string[0]);
        }

        // ------------------------------------------------------------------ a part that breaks closes nothing else (the route 8 scout of November 22)
        /// <summary>The Bellows in its chamber with both pressure organs as the zone builds them. Taren breaks the west organ where route 8 has him (steps 58 to 60), the lock lets go of
        /// it, and from where the route stands him for the east one (step 62: 10.1, 269.7, facing it) one press of the lock takes the east organ, and his blows break it. What closes a
        /// beaten boss's parts (EnemyBrain.ShutItsParts) runs on the boss's own death alone, never on a part's.</summary>
        [UnityTest,Timeout(150000)] public IEnumerator AfterTheWestOrganBreaksOnePressLocksTheEastOneAndItBreaks()
        {
            LogAssert.ignoreFailingMessages=true;
            DevLoadout.Apply("hushwell");
            SceneFlow.Current.LoadZone("Hushwell");float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: Hushwell never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            Hold();yield return new WaitForSecondsRealtime(.6f);
            for(int i=0;i<4&&PartyController.Current.Active.character!="Taren";i++){PartyController.Current.Swap();Hold();yield return null;}
            var lead=PartyController.Current.Active;var brain=lead.GetComponent<PlayerBrain>();
            foreach(var member in PartyController.Current.members)if(member!=lead)Place(member,new Vector3(-12,-7.92f,252));
            var organs=Object.FindObjectsByType<PressureOrgan>(FindObjectsSortMode.None);
            var west=organs.FirstOrDefault(o=>o.name.EndsWith("west"));var east=organs.FirstOrDefault(o=>o.name.EndsWith("east"));
            Assert.IsNotNull(west,"fixture: the west organ");Assert.IsNotNull(east,"fixture: the east organ");
            Assert.IsTrue(west.Pumping&&east.Pumping,"fixture: both organs stand");
            var middle=Lattice.World.HushwellLayout.OnFloor(Lattice.World.HushwellLayout.Bellows);
            var bellows=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("BellowsBelow"),middle);bellows.Passive=true;
            yield return new WaitForSecondsRealtime(.8f);
            DamagePacket Blow()=>new DamagePacket{source=lead.Health,amount=400,type=DamageType.Kinetic,weight=HitWeight.Light};
            // The west organ: faced, locked with one press, broken.
            var westAt=west.transform.position+Vector3.right*3.5f;westAt.y=lead.transform.position.y;Face(lead,westAt,west.transform.position-westAt);yield return null;
            lead.target=brain.FindTarget();lead.TargetLocked=lead.target!=null;
            Assert.AreEqual(west.Health,lead.target,"fixture: one press facing the west organ locks it");
            for(int n=0;n<40&&west.Health.Alive;n++)west.Health.Receive(Blow());
            Assert.IsFalse(west.Health.Alive,"fixture: the west organ breaks");
            // The lead's brain runs, as in play, so whatever a part's death does to the lock happens.
            brain.AutoPilot=false;for(int i=0;i<10;i++)yield return null;
            Assert.IsFalse(lead.TargetLocked&&lead.target==west.Health,"the lock still holds the broken west organ");
            // Where route 8 stands him for the east organ, facing it: one press.
            var eastAt=new Vector3(10.06f,lead.transform.position.y,269.72f);brain.AutoPilot=true;Face(lead,eastAt,east.transform.position-eastAt);yield return null;
            if(lead.TargetLocked)lead.TargetLocked=false;
            lead.target=brain.FindTarget();lead.TargetLocked=lead.target!=null;
            string locked=lead.target==null?"nothing":lead.target.name;
            Assert.AreEqual(east.Health,lead.target,$"one press facing the east organ after the west one broke locked {locked}");
            Assert.IsTrue(east.Health.Targetable,"the east organ can't be struck after the west one broke");
            float taken=0;for(int n=0;n<40&&east.Health.Alive;n++)taken+=east.Health.Receive(Blow());
            LogAssert.ignoreFailingMessages=false;
            TestContext.WriteLine($"BOSS_PARTS Bellows west organ down, east locked={locked} took={taken:0} down={!east.Health.Alive}");
            Assert.Greater(taken,0f,"the east organ took nothing");Assert.IsFalse(east.Health.Alive,"the east organ never broke");
        }
        /// <summary>Every boss whose parts break one at a time on its own body (BossParts' targets, a collar's links): with its parts open, one broken leaves the others to be struck
        /// and to take damage. Shut only when the boss itself dies (EnemyBrain.ShutItsParts).</summary>
        [UnityTest,Timeout(300000)] public IEnumerator OnePartBrokenLeavesTheBossesOtherPartsToStrike()
        {
            LogAssert.ignoreFailingMessages=true;
            yield return Zone("Arena_Ground");var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(160,1,160);
            var lead=PartyController.Current.Active;Place(lead,new Vector3(232,0,-8));
            var lines=new List<string>();var faults=new List<string>();
            foreach(var id in All)
            {
                var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),new Vector3(232,0,14));boss.Passive=true;
                yield return new WaitForSecondsRealtime(.6f);if(boss==null)continue;
                foreach(var set in boss.GetComponentsInChildren<BossParts>(true))set.Expose(true);
                var parts=Health.All.Where(h=>h!=null&&h!=boss.Health&&h.reactionOwner==boss.Health&&h.Alive).ToList();
                if(parts.Count<2){Object.Destroy(boss.gameObject);yield return null;continue;}
                foreach(var part in parts)part.Damageable=true;
                var first=parts[0];for(int n=0;n<200&&first.Alive;n++)first.Receive(new DamagePacket{source=lead.Health,amount=first.maximum,type=DamageType.Kinetic});
                for(int i=0;i<3;i++)yield return null;
                int struck=0;foreach(var part in parts.Skip(1))if(part!=null&&part.Alive&&part.Targetable&&part.Receive(new DamagePacket{source=lead.Health,amount=1,type=DamageType.Kinetic})>0)struck++;
                string line=$"BOSS_PARTS {id} parts={parts.Count} first-broken={!first.Alive} others-struck={struck}/{parts.Count-1}";lines.Add(line);
                if(!first.Alive&&struck<parts.Count-1)faults.Add(line+": breaking one part closed the others");
                Object.Destroy(boss.gameObject);yield return new WaitForSecondsRealtime(.2f);
            }
            LogAssert.ignoreFailingMessages=false;
            foreach(var l in lines)TestContext.WriteLine(l);
            Assert.IsNotEmpty(lines,"fixture: some boss has parts that break one at a time");
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        /// <summary>The measure sees a beaten body that shows nothing: the Rotbloom beaten and then held exactly as it stood (its presentation, its brain and its rig stopped) changes
        /// under the share <see cref="ABeatenBossShowsItWithinASecondAndTakesNothingMore"/> asks for.</summary>
        [UnityTest,Timeout(120000)] public IEnumerator TheMeasureSeesABeatenBossThatShowsNothing()
        {
            LogAssert.ignoreFailingMessages=true;
            const string id="Rotbloom";
            yield return Arena(id);
            var lead=PartyController.Current.Active;Place(lead,LeadAt(id));
            foreach(var member in PartyController.Current.members)member.Health.InvulnerableUntil=float.PositiveInfinity;
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),BossAt(id));boss.Passive=true;
            yield return new WaitForSecondsRealtime(1.2f);
            var drawn=Drawn(boss);foreach(var r in drawn)r.gameObject.layer=Layer;
            var camera=Eye(Box(drawn));var before=Shot(camera);
            Beat(boss,lead);
            // Held as it stood: everything on it that could change what it draws, stopped in the frame it was beaten. Its darkening writes its first share in the Died itself
            // (D385), so that is given back first.
            var darkening=boss.GetComponent<BodyGoesDark>();if(darkening!=null)darkening.Restore();
            foreach(var behaviour in boss.GetComponentsInChildren<Behaviour>(true))if(!(behaviour is Health))behaviour.enabled=false;
            yield return new WaitForSecondsRealtime(1f);
            var after=Shot(camera);float shown=Changed(before,after,out int covered);Object.Destroy(camera.gameObject);
            LogAssert.ignoreFailingMessages=false;
            TestContext.WriteLine($"BOSS_DEATH_HELD {id} covered={covered} second={shown:0.00}");
            Assert.Greater(covered,400,"fixture: the Rotbloom is in frame");Assert.Less(covered,W*H/2,"fixture: the ground colour is read");
            Assert.Less(shown,Shows,$"the measure passes a beaten Rotbloom held as it stood ({shown:P0} changed)");
        }
    }
}
