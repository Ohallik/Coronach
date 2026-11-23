using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 8's dungeon, the Bridle Foundry (docs/maps/BridleFoundry.md): walked down three floors, its rule (hot metal runs on a clock, a marking always says where it will
    /// be, coolant stops it) as the pours, the two wheels and the press play it, a wheel's work kept in the save, the marking read over the glow from the fixed camera, the casting
    /// gallery and the heart's door built to their contracts, the young road's cadence and the release staged from the Colossus's fall, and no wall between the camera and a fight.
    /// Flags are set directly where a conversation would set them: the script's nodes are the dialogue tests'.</summary>
    public sealed class BridleFoundryTests
    {
        const string Foundry="BridleFoundry";
        // The builder's numbers (HarrowgateBuilder.Foundry), copied: the tests can't see the editor assembly.
        const float Cast=-5,Deep=-10;
        static readonly Vector2 G=new(0,180),VessaPost=new(0,190),SquadPost=new(0,185),GantryDoor=new(16,201),PressAt=new(26,96),WheelOneHand=new(-11.6f,49.4f),WheelTwoHand=new(42.4f,99.2f);
        static readonly Vector2[] Racks={new(-9,174),new(9,174),new(-11,188),new(11,188)};
        readonly List<string> log=new();
        void Heard(string text,string stack,LogType type)
        {
            if(text.StartsWith("POUR_")||text.StartsWith("PRESS_")||text.StartsWith("COOLANT_OPEN")||text.StartsWith("WATER_")||text.StartsWith("CHOIR_")||text.StartsWith("DISCOVERY"))log.Add(text);
        }
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter8");log.Clear();Application.logMessageReceived+=Heard;
        }
        readonly List<string> offered=new();
        void Offered(string node,string speaker,System.Action<bool> done)=>offered.Add(node);
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;Npc.TalkRequested-=Offered;offered.Clear();
            var dialogue=Lattice.Dialogue.DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;}
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static void Set(params string[] keys){foreach(var key in keys)GameServices.Current.Flags.SetBool(key,true);}
        static bool Flag(string key)=>GameServices.Current.Flags.GetBool(key);
        static GameState State=>GameServices.Current.State;
        static IEnumerator Zone(string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(Foundry,spawn);float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,Foundry+" never finished loading");Assert.AreEqual(Foundry,SceneManager.GetActiveScene().name);
            Hold();yield return new WaitForSecondsRealtime(.6f);
        }
        /// <summary>The heroes stand where they are put and nobody wanders into a crossing.</summary>
        static void Hold(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;}}
        /// <summary>The ground under a plan point: the first solid thing a ray from above meets that isn't a seal's blocker (molten metal's, deep water's: a Membrane's).</summary>
        static Vector3 Ground(Vector2 p,float from=40)
        {
            var hits=Physics.RaycastAll(new Vector3(p.x,from,p.y),Vector3.down,90,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<Membrane>()==null).OrderBy(h=>h.distance).ToArray();
            Assert.IsNotEmpty(hits,"fixture: there is ground at "+p);
            return hits[0].point;
        }
        static Vector3 Hero=>PartyController.Current.Active.transform.position;
        static void Put(CombatActor actor,Vector2 p){var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=Ground(p)+Vector3.up*.05f;cc.enabled=true;Physics.SyncTransforms();}
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.Partner;
        static bool Reach(Vector3 from,Vector2 to)
        {
            var path=new NavMeshPath();
            return NavMesh.SamplePosition(from,out var start,3,NavMesh.AllAreas)&&NavMesh.SamplePosition(Ground(to),out var end,1.5f,NavMesh.AllAreas)&&
                NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        }
        static PourChannel Pour(string name){var found=PourChannel.All.Where(p=>p.name==name).ToArray();Assert.AreEqual(1,found.Length,name+" is not in the Foundry exactly once");return found[0];}
        static ForgePress Press=>Object.FindFirstObjectByType<ForgePress>();
        static DiscoveryPoint Point(System.Func<DiscoveryPoint,bool> which,string what)
        {var found=Object.FindObjectsByType<DiscoveryPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(which).ToArray();Assert.AreEqual(1,found.Length,what+" is not in the Foundry exactly once");return found[0];}
        static IEnumerator Until(System.Func<bool> done,float seconds,string what){float deadline=Time.unscaledTime+seconds;while(!done()&&Time.unscaledTime<deadline)yield return null;Assert.IsTrue(done(),what);}

        // ------------------------------------------------------------------ the way down, and what stops it
        [UnityTest] public IEnumerator TheFoundryIsWalkedDownFromTheMouthAndTheTapRunsStopItUntilEachWheelTurns()
        {
            yield return Zone();var start=Hero;
            foreach(var (what,at) in new[]{("the yard",new Vector2(0,26)),("the skip store",new Vector2(-30,24)),("the first wheel",WheelOneHand),("the wheel hall short of the run",new Vector2(0,53))})
                Assert.IsTrue(Reach(start,at),"from the mouth nobody can walk to "+what+" at "+at);
            // The tap run is the hall's wall until the first wheel turns: the fixture has to see a run that doesn't stop the way, so with its seal open the far side is reachable.
            var a=Pour("Tap run A");var seal=a.seal.GetComponent<Membrane>();
            seal.SetOpen(true);yield return null;yield return null;Assert.IsTrue(Reach(start,new Vector2(0,62)),"fixture: the check can't see across an open run");
            seal.SetOpen(false);yield return null;yield return null;
            foreach(var (what,at) in new[]{("the far side of the first run",new Vector2(0,62)),("the gangway",new Vector2(0,79)),("the charging floor",new Vector2(0,97)),("the gallery",new Vector2(0,146))})
                Assert.IsFalse(Reach(start,at),what+" can be walked to before the first wheel turns");
            // The first wheel, by its hand: the run crusts, its seal goes, and the charging floor and the press run open; the second run still holds the stair.
            var one=Point(p=>p.flag=="vask.sluice1","the first wheel's hand");Assert.AreEqual("Turn the sluice wheel",one.prompt);Assert.AreEqual("sluice",one.situation,"the first wheel says nothing as it turns");
            one.Interact();Assert.IsTrue(Flag("vask.sluice1"),"working the first wheel sets nothing");
            yield return Until(()=>a.Cooled&&!a.seal.activeSelf,4,"the first run doesn't crust when its wheel turns");yield return new WaitForSecondsRealtime(.4f);
            Assert.IsTrue(log.Any(l=>l.StartsWith("COOLANT_OPEN vask.sluice1")),"the first wheel doesn't turn and say so");Assert.IsTrue(log.Any(l=>l=="POUR_COOLED vask.sluice1"),"the first run doesn't cool and say so");
            foreach(var (what,at) in new[]{("the far side of the first run",new Vector2(0,62)),("the gangway",new Vector2(0,79)),("the charging floor",new Vector2(0,97)),("the press run",PressAt+new Vector2(2,0)),("the second wheel",WheelTwoHand)})
                Assert.IsTrue(Reach(start,at),"after the first wheel nobody can walk to "+what);
            foreach(var (what,at) in new[]{("the stair down",new Vector2(-13,124)),("the gallery",new Vector2(0,146)),("the quench tank's floor",new Vector2(38,114.5f))})
                Assert.IsFalse(Reach(start,at),what+" can be walked to after the first wheel alone");
            // The second wheel: the stair's run crusts and the tank drains; the way down to the heart's door opens.
            var two=Point(p=>p.flag=="vask.sluice2","the second wheel's hand");two.Interact();Assert.IsTrue(Flag("vask.sluice2"));
            var b=Pour("Tap run B");yield return Until(()=>b.Cooled,4,"the second run doesn't crust when its wheel turns");yield return new WaitForSecondsRealtime(4);
            foreach(var (what,at) in new[]{("the stair down",new Vector2(-13,124)),("the gallery's south room",new Vector2(0,146)),("the loft",new Vector2(35,144)),("the gallery checkpoint",new Vector2(-3,149)),
                ("the quench tank's floor",new Vector2(38,114.5f))})Assert.IsTrue(Reach(start,at),"after the second wheel nobody can walk to "+what);
        }

        // ------------------------------------------------------------------ the wheels: every wheel opens one way and arms another
        [UnityTest] public IEnumerator EachWheelOpensOneWayAndArmsAnother()
        {
            yield return Zone();
            var l=Pour("Ladle line L");var a=Pour("Tap run A");var press=Press;
            Assert.IsFalse(l.Armed,"the gangway's ladle runs before the first wheel has drained its trough");Assert.IsFalse(press.Armed,"the press runs before the second wheel has drained its tank");
            var coolantL=Object.FindObjectsByType<WaterVolume>(FindObjectsSortMode.None).Single(w=>w.drainFlag=="vask.sluice1");
            Assert.Greater(coolantL.Level-Ground(new Vector2(0,73)).y,.1f,"fixture: coolant stands in the gangway's trough before the first wheel");
            yield return new WaitForSecondsRealtime(11);Assert.IsFalse(l.Marked||l.Pouring,"a quenched ladle line marked or poured");
            Set("vask.sluice1");
            yield return Until(()=>a.Cooled&&l.Armed,3,"the first wheel doesn't cool one run and arm the ladle");
            Assert.IsTrue(log.Any(x=>x.StartsWith("POUR_ARMED Ladle line L")),"the ladle doesn't say it is armed");
            yield return Until(()=>coolantL.Drained,6,"the gangway's trough keeps its coolant after the first wheel");
            yield return Until(()=>l.Marked,12,"the armed ladle never marks its strip");
            Set("vask.sluice2");
            yield return Until(()=>press.Armed&&Pour("Tap run B").Cooled,3,"the second wheel doesn't cool the stair's run and arm the press");
            var tank=Object.FindObjectsByType<WaterVolume>(FindObjectsSortMode.None).Single(w=>w.drainFlag=="vask.sluice2");
            yield return Until(()=>tank.Drained,8,"the quench tank doesn't drain with the second wheel");
            yield return Until(()=>press.Stamps>0,8,"the armed press never stamps");
        }

        // ------------------------------------------------------------------ a pour
        [UnityTest] public IEnumerator APourStrikesOnlyInsideItsMarkingWhileItPoursAndNeverTakesTheLast()
        {
            Set("vask.sluice1","clear.Foundry_Yard");
            yield return Zone();
            var l=Pour("Ladle line L");Assert.IsTrue(l.Armed,"fixture: the ladle line is armed");
            var inside=new Vector2(0,73);var beside=new Vector2(-1.5f,73-l.width*.5f-.7f);
            Assert.IsTrue(l.Contains(Ground(inside)),"fixture: the trough's middle is in the strip");Assert.IsFalse(l.Contains(Ground(beside)),"fixture: the point beside the strip is out of it");
            // Wait out a pour so the clock is known, then stand the lead in the trough while the ladle rests and the partner a step outside the strip.
            yield return Until(()=>l.Pouring,12,"the ladle never pours");yield return Until(()=>!l.Pouring&&!l.Marked,6,"the pour never ends");
            yield return new WaitForSecondsRealtime(.6f);
            // The lead stands on its own brain with the pad untouched, as a player does: a held hero's motor isn't driven, and a throw is a dash the motor makes.
            Put(Lead,inside);Put(Partner,beside);Lead.GetComponent<PlayerBrain>().AutoPilot=false;float before=Lead.Health.integrity,besideBefore=Partner.Health.integrity;
            // Resting, the trough is any floor.
            yield return new WaitForSecondsRealtime(1.2f);Assert.AreEqual(before,Lead.Health.integrity,.01f,"the trough struck between pours");
            // The marking stands well before the pour: what strikes is marked first.
            yield return Until(()=>l.Marked,12,"the strip is never marked");float marked=Time.time;
            yield return Until(()=>l.Pouring,4,"the marked strip never pours");Assert.GreaterOrEqual(Time.time-marked,.5f,"the pour lands under half a second after its marking");
            yield return Until(()=>log.Any(x=>x.StartsWith("POUR_STRUCK "+Lead.character)),2,"the pour doesn't strike a hero standing in its marking");
            // Measured as every hit is (guide 0.1): through Health.Receive, in the parts a player has here, so the hero's plating takes its part. An ordinary hit's band, 9 to 13 percent.
            float took=(before-Lead.Health.integrity)/Lead.Health.maximum;Assert.That(took,Is.InRange(.09f,.13f),"a pour doesn't take an ordinary hit's share of the bar ("+took.ToString("0.000")+")");
            yield return new WaitForSecondsRealtime(.4f);Assert.IsFalse(l.Contains(Lead.transform.position),"the pour leaves its hero standing in the metal");
            Assert.AreEqual(besideBefore,Partner.Health.integrity,.01f,"the pour struck a hero standing outside its marking");
            // Never the last of it.
            yield return Until(()=>!l.Marked,6,"the pour never ends");
            Lead.Health.integrity=1;Put(Lead,inside);
            yield return Until(()=>l.Pouring,14,"the ladle never pours again");yield return new WaitForSecondsRealtime(1);
            Assert.IsTrue(Lead.Health.Alive,"a pour killed a hero");Assert.GreaterOrEqual(Lead.Health.integrity,1,"a pour took the last of a hero's Integrity");
        }

        [UnityTest] public IEnumerator NothingGlowsWarmOnTheFloorThatIsntMarked()
        {
            Set("vask.sluice1");
            yield return Zone();
            var block=new MaterialPropertyBlock();var l=Pour("Ladle line L");float floorHigh=.45f;
            // Watched over a whole period of the ladle's: every warm light near the floor at every frame is a pour's metal or stream, and that pour is marked.
            float until=Time.unscaledTime+l.period+1;int frames=0,lit=0;
            while(Time.unscaledTime<until)
            {
                frames++;
                foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    // The zone's floor, not a body standing on it: a hero's or an enemy's own glowing parts are theirs.
                    if(!r.enabled||!r.gameObject.activeInHierarchy||r is ParticleSystemRenderer||r.GetComponentInParent<Telegraph>()!=null||r.GetComponentInParent<CombatActor>()!=null||r.GetComponentInParent<Health>()!=null)continue;
                    // What lies on the floor: flat, and within a hand or two of the ground under it (D217 is about the floor, not a lamp or a console's screen standing on it).
                    var b=r.bounds;if(b.size.y>.8f||b.size.x<.3f&&b.size.z<.3f)continue;
                    if(!Physics.Raycast(new Vector3(b.center.x,b.max.y+.05f,b.center.z),Vector3.down,out var hit,20,~0,QueryTriggerInteraction.Ignore)||b.min.y>hit.point.y+floorHigh)continue;
                    r.GetPropertyBlock(block);Color glow=block.HasColor("_EmissionColor")?block.GetColor("_EmissionColor"):r.sharedMaterial!=null&&r.sharedMaterial.HasProperty("_EmissionColor")?r.sharedMaterial.GetColor("_EmissionColor"):Color.black;
                    bool warm=glow.r>.3f&&glow.r>glow.b*1.5f;if(!warm)continue;
                    var pour=PourChannel.All.FirstOrDefault(p=>p.metal.Contains(r)||p.stream!=null&&r.transform.IsChildOf(p.stream.transform));
                    Assert.IsNotNull(pour,r.name+" glows warm on the floor and is no pour's");Assert.IsTrue(pour.Shown||pour.Pouring,r.name+" glows warm on the floor while no marking is drawn over "+pour.name);lit++;
                }
                yield return null;
            }
            Assert.Greater(lit,frames/4,"fixture: the pours were seen lit");
        }

        // ------------------------------------------------------------------ the marking from the fixed camera
        [UnityTest] public IEnumerator APoursMarkingReadsOverItsGlowFromTheFixedCamera()
        {
            Assert.AreNotEqual(UnityEngine.Rendering.GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType,"fixture: the runner renders");
            Set("vask.sluice1","clear.Foundry_Yard");
            yield return Zone();
            var l=Pour("Ladle line L");Put(Lead,new Vector2(0,66.6f));Put(Partner,new Vector2(2.2f,66.6f));
            yield return new WaitForSecondsRealtime(1.5f);
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/focused/foundry-pour"));Directory.CreateDirectory(folder);
            // The rim on the camera's side of the strip at the trough's middle, a hand inside it (the margin over the kerb), a hand outside it (the floor), and the metal.
            float south=73-l.width*.5f;var rim=new Vector3(0,0,south);var inMargin=new Vector3(0,0,south+.35f);var floor=new Vector3(0,0,south-.45f);var metal=new Vector3(0,0,73);
            Vector3 OnGround(Vector3 p){var g=Ground(new Vector2(p.x,p.z));return g+Vector3.up*.05f;}
            yield return Until(()=>l.Marked&&!l.Pouring,14,"the strip is never marked before its pour");yield return new WaitForSecondsRealtime(.5f);
            var (warning,_)=Grab(folder,"1-warning",OnGround(rim),OnGround(floor),OnGround(inMargin),OnGround(metal));
            Assert.Less(warning.floorLuma,warning.rimLuma,"fixture: the warning's frame was taken");
            yield return Until(()=>l.Pouring,4,"the ladle never pours");yield return new WaitForSecondsRealtime(.8f);
            var (pouring,glowAt)=Grab(folder,"2-pouring",OnGround(rim),OnGround(floor),OnGround(inMargin),OnGround(metal));
            yield return Until(()=>!l.Marked,6,"the pour never ends");yield return new WaitForSecondsRealtime(.5f);
            Grab(folder,"3-after",OnGround(rim),OnGround(floor),OnGround(inMargin),OnGround(metal));
            // The rim reads: there is the danger's orange where the rim is, brighter than the floor beside it, in the warning and through the pour, with the metal lit.
            foreach(var (what,shot) in new[]{("the warning",warning),("the pour",pouring)})
            {
                Assert.Greater(shot.rimOrange,.0f,"the marking's rim shows no danger orange from the fixed camera in "+what);
                Assert.Greater(shot.rimLuma-shot.floorLuma,.12f,"the marking's rim doesn't stand out from the floor beside it in "+what+" ("+shot.rimLuma.ToString("0.00")+" against "+shot.floorLuma.ToString("0.00")+")");
            }
            Assert.Greater(glowAt,pouring.floorLuma+.15f,"fixture: the metal is lit in the pour's frame");
        }
        /// <summary>The heat is everywhere a marking isn't: the furnaces' breathing mouths, the crucibles over the charging floor and the gallery, the rings cooling in their rack, the
        /// heart's vents, a tap run's ember and its surge. Wherever a hero can stand by one, a danger's marking laid there reads from the fixed camera as a pour's does: the danger's
        /// orange at its rim, brighter than the floor beside it by what the pour's must be. Frames in Builds/quality/focused/foundry-heat for the eye.</summary>
        [UnityTest] public IEnumerator AMarkingReadsWhereverTheForgeIsHottest()
        {
            Assert.AreNotEqual(UnityEngine.Rendering.GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType,"fixture: the runner renders");
            Set("vask.sluice1","clear.Foundry_Yard","clear.Charging_Floor","vask.sluiceTube1","vask.sluiceTube2");
            yield return Zone();
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/focused/foundry-heat"));Directory.CreateDirectory(folder);
            var toCamera=new Vector3(-Mathf.Sin(SightlineAudit.Yaw*Mathf.Deg2Rad),0,-Mathf.Cos(SightlineAudit.Yaw*Mathf.Deg2Rad));var weak=new List<string>();int read=0;
            Vector3 OnGround(Vector3 p)=>Ground(new Vector2(p.x,p.z))+Vector3.up*.05f;
            void Judge(string name,Shot shot,Vector2 at)
            {
                read++;
                if(shot.rimOrange<=0||shot.rimLuma-shot.floorLuma<=.12f)weak.Add($"{name} at ({at.x:0.#}, {at.y:0.#}): rim {shot.rimLuma:0.00} (orange {shot.rimOrange:0.00}) against the floor's {shot.floorLuma:0.00}");
            }
            // A marking the size of a hound's leap laid at each, the lead a few steps off it toward the room (the camera follows the lead), the partner on the other side.
            foreach(var (name,at,lead) in new[]{("furnace-mouth",new Vector2(9,110.2f),new Vector2(3,-2.6f)),("furnace-east",new Vector2(18,110.2f),new Vector2(-3,-2.6f)),
                ("under-the-charging-crucible",new Vector2(4.4f,107.6f),new Vector2(3,-1.5f)),("charge-buckets",new Vector2(13.5f,108.8f),new Vector2(-3,-2)),
                ("under-the-gallery-crucible",new Vector2(-7.5f,196.2f),new Vector2(3,-1.5f)),("by-the-cooling-rings",new Vector2(-21.2f,183.6f),new Vector2(3,-1.5f)),
                ("under-a-heart-vent",new Vector2(9.15f,268.2f),new Vector2(-2.2f,-2.6f)),("under-the-east-vents",new Vector2(19.2f,247.6f),new Vector2(-3.2f,-1)),
                // Over the grated runners let into the fights' floors (November 21): a marking laid across one reads over its glow.
                ("over-the-gallery-runner",new Vector2(4,169.5f),new Vector2(3,-2.6f)),("over-the-heart-runner",new Vector2(2.87f,267.25f),new Vector2(-2.2f,-2.6f))})
            {
                Put(Lead,at+lead);Put(Partner,at+lead+new Vector2(1.8f*Mathf.Sign(lead.x),0));yield return new WaitForSecondsRealtime(1.4f);
                var centre=Ground(at)+Vector3.up*.05f;var marking=Telegraph.Circle(centre,1.6f,4,Telegraph.Threat);marking.Steady=true;
                yield return new WaitForSecondsRealtime(.6f);
                var (shot,_)=Grab(folder,name,OnGround(centre+toCamera*1.6f),OnGround(centre+toCamera*2.05f),OnGround(centre+toCamera*1.25f),centre);
                Judge(name,shot,at);marking.Clear();yield return null;
            }
            // The stair head's tap run, its own steady marking over its ember and over its surge.
            var b=Pour("Tap run B");Assert.IsTrue(b.Marked&&!b.Cooled,"fixture: the stair head's run is running");
            Put(Lead,new Vector2(-11,113.6f));Put(Partner,new Vector2(-15.4f,113.6f));yield return new WaitForSecondsRealtime(1.4f);
            float south=117-b.width*.5f;var rim=new Vector3(-13,0,south);var floor=new Vector3(-13,0,south-.45f);var metal=new Vector3(-13,0,117);
            yield return Until(()=>b.SurgeAt(b.Clock)>.9f,b.surgeEvery+1,"the run never surges");
            var (surge,surgeGlow)=Grab(folder,"tap-run-surge",OnGround(rim),OnGround(floor),OnGround(rim+Vector3.forward*.35f),OnGround(metal));Judge("tap-run-surge",surge,new Vector2(-13,south));
            yield return Until(()=>b.SurgeAt(b.Clock)<.02f,b.surgeEvery+1,"the run never settles to its ember");yield return new WaitForSecondsRealtime(.4f);
            var (ember,emberGlow)=Grab(folder,"tap-run-ember",OnGround(rim),OnGround(floor),OnGround(rim+Vector3.forward*.35f),OnGround(metal));Judge("tap-run-ember",ember,new Vector2(-13,south));
            Assert.Greater(surgeGlow,emberGlow,"fixture: the run's surge is brighter than its ember");
            Assert.AreEqual(12,read,"fixture: every hot place was photographed");
            Assert.IsEmpty(weak,"a marking doesn't read over the forge's heat: "+string.Join("; ",weak));
        }
        struct Shot{public float rimOrange,rimLuma,floorLuma;}
        /// <summary>What the game's camera draws, saved for the eye; and at the rim (the best of a few pixels round its point), the floor beside it, and the metal.</summary>
        static (Shot shot,float metalLuma) Grab(string folder,string name,Vector3 rim,Vector3 floor,Vector3 margin,Vector3 metal)
        {
            const int W=1280,H=720;var camera=Camera.main;
            var image=new Texture2D(W,H,TextureFormat.RGB24,false);var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var active=RenderTexture.active;var target=camera.targetTexture;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
                Color At(Vector3 world,int spread,System.Func<Color,float> best)
                {
                    var s=camera.WorldToScreenPoint(world);int cx=Mathf.RoundToInt(s.x),cy=Mathf.RoundToInt(s.y);Color pick=Color.black;float score=float.NegativeInfinity;
                    for(int y=-spread;y<=spread;y++)for(int x=-spread;x<=spread;x++){int px=Mathf.Clamp(cx+x,0,W-1),py=Mathf.Clamp(cy+y,0,H-1);var c=image.GetPixel(px,py);float v=best(c);if(v>score){score=v;pick=c;}}
                    return pick;
                }
                static float Orange(Color c)=>Mathf.Min(c.r-c.g*1.15f,c.r-c.b*1.8f);
                var r=At(rim,3,Orange);var f=At(floor,1,c=>-c.grayscale);var m=At(metal,2,c=>c.grayscale);
                Debug.Log($"FOUNDRY_POUR_SHOT {name} rim=({r.r:0.00},{r.g:0.00},{r.b:0.00}) floor={f.grayscale:0.00} metal={m.grayscale:0.00}");
                return (new Shot{rimOrange=Orange(r),rimLuma=r.grayscale,floorLuma=f.grayscale},m.grayscale);
            }
            finally{camera.targetTexture=target;RenderTexture.active=active;rt.Release();Object.Destroy(rt);Object.Destroy(image);}
        }

        // ------------------------------------------------------------------ the press
        [UnityTest] public IEnumerator ThePressStampsOnItsClockOnlyOnceTheSecondWheelHasDrainedItsTank()
        {
            Set("vask.sluice1","clear.Press_Bay","clear.Charging_Floor");
            yield return Zone();
            var press=Press;var under=PressAt+new Vector2(2,0);Assert.IsTrue(press.Contains(Ground(under)),"fixture: the press's middle is under it");
            Put(Lead,under);Put(Partner,under+new Vector2(-press.length*.5f-1.6f,0));Lead.GetComponent<PlayerBrain>().AutoPilot=false;float before=Lead.Health.integrity;
            yield return new WaitForSecondsRealtime(press.period+1.5f);
            Assert.AreEqual(0,press.Stamps,"the press stamped before the second wheel");Assert.AreEqual(before,Lead.Health.integrity,.01f,"the quenched press struck");
            Set("vask.sluice2");
            yield return Until(()=>press.Marked,press.period+2,"the armed press never marks its square");float marked=Time.time;
            yield return Until(()=>press.Stamps>0,press.warn+1,"the marked press never stamps");Assert.GreaterOrEqual(Time.time-marked,.5f,"the press stamps under half a second after its marking");
            Assert.IsTrue(log.Any(x=>x.StartsWith("PRESS_STRUCK "+Lead.character)),"the press doesn't strike a hero under it");
            float took=(before-Lead.Health.integrity)/Lead.Health.maximum;Assert.That(took,Is.InRange(.09f,.13f),"the press doesn't take an ordinary hit's share of the bar ("+took.ToString("0.000")+")");
            yield return new WaitForSecondsRealtime(.4f);Assert.IsFalse(press.Contains(Lead.transform.position),"the press leaves its hero under the ram");
            Assert.IsFalse(log.Any(x=>x.StartsWith("PRESS_STRUCK "+Partner.character)),"the press struck a hero standing beside its square");
        }

        // ------------------------------------------------------------------ the save
        [UnityTest] public IEnumerator AWheelsWorkIsKeptInTheSaveAndLoadsWithNothingPlayed()
        {
            Set("vask.sluice1","vask.sluice2");
            yield return Zone("Gallery");
            var saved=GameServices.Current.Saves.Load("autosave");Assert.AreEqual(Foundry,saved.zone,"fixture: arriving autosaves");Assert.IsTrue(saved.flags["vask.sluice2"],"fixture: the save has the second wheel");
            // The check has to be able to see a save that lost its wheels: the same save without them continues into a foundry whose runs are hot.
            var stripped=GameServices.Current.Saves.Load("autosave");stripped.flags.Remove("vask.sluice1");stripped.flags.Remove("vask.sluice2");GameServices.Current.Saves.Save("autosave",stripped);
            yield return Continue();yield return new WaitForSecondsRealtime(.3f);
            Assert.IsFalse(Pour("Tap run A").Cooled,"fixture: the check can't see a save that lost its wheels");
            var kept=GameServices.Current.Saves.Load("autosave");kept.flags["vask.sluice1"]=kept.flags["vask.sluice2"]=true;kept.zone=Foundry;kept.spawn="Gallery";GameServices.Current.Saves.Save("autosave",kept);
            log.Clear();yield return Continue();yield return new WaitForSecondsRealtime(.4f);
            Assert.IsTrue(Pour("Tap run A").Cooled&&Pour("Tap run B").Cooled,"a wheel's run loads hot again");Assert.IsTrue(Press.Armed,"the press loads quenched again");
            Assert.IsFalse(Pour("Tap run A").seal.activeSelf||Pour("Tap run B").seal.activeSelf,"a cooled run loads with its seal standing");
            Assert.IsTrue(Object.FindObjectsByType<WaterVolume>(FindObjectsSortMode.None).Single(w=>w.drainFlag=="vask.sluice2").Drained,"the quench tank loads full again");
            Assert.IsFalse(log.Any(l=>l.StartsWith("POUR_COOLED")||l.StartsWith("COOLANT_OPEN")||l.StartsWith("PRESS_ARMED")||l.StartsWith("WATER_STAGE ")),"a loaded wheel plays its work again: "+string.Join(", ",log));
            Assert.IsTrue(Reach(Hero,new Vector2(-13,114)),"the stair head can't be walked to after a load with both wheels turned");
        }
        /// <summary>Title, then Continue from the autosave, as a player does.</summary>
        IEnumerator Continue()
        {
            SceneFlow.Current.LoadZone("Title");while(SceneFlow.Current.Loading)yield return null;yield return null;
            Object.FindFirstObjectByType<TitleScreen>().StartGame(true);
            float until=Time.realtimeSinceStartup+30;while(SceneFlow.Current.Loading&&Time.realtimeSinceStartup<until)yield return null;
            Assert.AreEqual(Foundry,SceneFlow.Current.Zone,"the save doesn't resume in the Foundry");Hold();yield return null;yield return null;
        }

        // ------------------------------------------------------------------ what the script expects of the world
        [UnityTest] public IEnumerator TheTalksWheelsAndCachesSetTheFlagsTheScriptExpects()
        {
            yield return Zone();
            foreach(var (node,flag,needs,save) in new[]{("IdraFirstSluice","vask.sluiceRead","",""),("VessaGallery","vask.vessaMet","","Gallery"),("SelaCagedRoad","vask.cageSeen","","Heart")})
            {
                var point=Point(p=>p.dialogueNode==node,node);
                Assert.AreEqual(flag,point.flag,node+" sets the wrong flag");Assert.AreEqual(needs,point.requiredFlag??"",node+" waits on the wrong flag");
                Assert.IsTrue(point.startsOnCrossing&&point.TryGetComponent<BoxCollider>(out var box)&&box.isTrigger,node+" isn't a crossing");
                if(save!="")Assert.AreEqual(save,point.saveSpawn,node+" doesn't save at its door");
            }
            Assert.AreEqual("VessaKorl",Point(p=>p.dialogueNode=="VessaGallery","VessaGallery").previewEncounter.encounterId,"Vessa wakes before her talk");
            Assert.AreEqual("SlagColossus",Point(p=>p.dialogueNode=="SelaCagedRoad","SelaCagedRoad").previewEncounter.encounterId,"the Colossus wakes before the door's talk");
            var anvil=Point(p=>p.dialogueNode=="BrekkAnvil","BrekkAnvil");Assert.AreEqual(("vask.anvilSeen","vask.juvenileFreed","Stand by the anvil",""),(anvil.flag,anvil.requiredFlag,anvil.prompt,anvil.situation),"the anvil isn't the manifest's");
            var talks=Object.FindObjectsByType<AfterTalk>(FindObjectsSortMode.None);
            var falls=talks.SingleOrDefault(t=>t.node=="VessaFallsBack");Assert.IsNotNull(falls,"nothing plays VessaFallsBack");
            Assert.AreEqual((BossController.FallingBackFlag,BossController.FallBackHeardFlag,"Vessa",true,1.6f),(falls.flag,falls.heardFlag,falls.speaker,falls.duringFight,falls.delay),"Vessa's talk isn't on her falling back at half (the contract, revised)");
            var release=talks.SingleOrDefault(t=>t.node=="TovraRelease");Assert.IsNotNull(release,"nothing plays TovraRelease");Assert.AreEqual(("bossdown.SlagColossus","vask.juvenileFreed","Tovra"),(release.flag,release.heardFlag,release.speaker));
            var tubes=Object.FindObjectsByType<RadioHail>(FindObjectsSortMode.None).Where(h=>h.speaker=="Tovra").ToArray();
            Assert.IsTrue(tubes.All(h=>h.situations.SequenceEqual(new[]{"hail.sluice"})),"Tovra's speaking tube says something other than her sluice lines");
            Assert.AreEqual(new[]{"vask.sluiceTube1","vask.sluiceTube2"},tubes.Select(h=>h.heardFlag).Distinct().OrderBy(f=>f).ToArray(),"Tovra's two sluice calls aren't each heard once");
            foreach(var id in new[]{"vask.foundry.skips","vask.foundry.tank","vask.foundry.loft"})Assert.IsTrue(Object.FindObjectsByType<LootCache>(FindObjectsSortMode.None).Any(c=>c.cacheId==id),"the cache "+id+" is missing");
            var gate=Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).Single(w=>w.scene=="ScoriaFlows");Assert.AreEqual("Foundry",gate.spawn,"the mouth doesn't lead back to the Flows' Foundry spawn");Assert.IsEmpty(gate.requiredFlag??"","the way home is locked");
        }

        /// <summary>The contract, revised: Vessa's `VessaFallsBack` is offered as she falls back at half her bar (her shift's flag), in the fight, and not again as she leaves at
        /// nought.</summary>
        [UnityTest] public IEnumerator VessasTalkIsOfferedAsSheFallsBackAndNotAgainAsSheLeaves()
        {
            Set("vask.sluice1","vask.sluice2","vask.vessaMet","clear.Gallery_Drones");
            yield return Zone("Gallery");
            Npc.TalkRequested+=Offered;
            yield return new WaitForSecondsRealtime(2);Assert.IsFalse(offered.Contains("VessaFallsBack"),"Vessa's talk is offered before she falls back");
            // At half: her shift sets the flag.
            Set(BossController.FallingBackFlag);float at=Time.unscaledTime;
            yield return Until(()=>offered.Contains("VessaFallsBack"),5,"Vessa's talk isn't offered as she falls back at half");
            Assert.GreaterOrEqual(Time.unscaledTime-at,1.4f,"Vessa's talk doesn't wait its beat after she falls back");
            // Its last line is heard (the script sets the flag); then at nought she leaves.
            Set(BossController.FallBackHeardFlag);var dialogue=Lattice.Dialogue.DialogueSystem.Current;if(dialogue!=null&&dialogue.Running)dialogue.Cancel();
            yield return new WaitForSecondsRealtime(.5f);
            Set("bossdown.VessaKorl","clear.VessaKorl",BossController.BeatenFlag);yield return new WaitForSecondsRealtime(4);
            Assert.AreEqual(1,offered.Count(n=>n=="VessaFallsBack"),"Vessa's talk is offered again as she leaves at nought");
        }

        /// <summary>The whole-game capture (November 20): with a third of Vessa's bar to go the card already said "Cool the Slag Colossus" (s48/221: her fall-back talk set the
        /// gallery's step), and at nought she lay sprawled on the gallery floor, one leg in the air, and slid away lying (s48/232: her body's own driver played its fall under her
        /// leaving). Now her talk at half sets nothing the main quest waits on, the step moves as she leaves, and she walks out on her feet. The body's fault is built into the fixture
        /// first and seen: a Vessa whose driver is left on as she falls goes down.</summary>
        [UnityTest] public IEnumerator TheGalleryIsTakenAsVessaWalksOutNotAsSheFallsBack()
        {
            Set("vask.sluice1","vask.sluice2","clear.Gallery_Drones");
            yield return Zone("Gallery");
            var quest=GameCatalog.Find<Lattice.Data.QuestDef>("Harrowgate");int step=System.Array.FindIndex(quest.steps,s=>s.target==BossController.BeatenFlag);
            Assert.Greater(step,0,"fixture: the main quest has the gallery's step");
            // The script and the room: hearing her talk sets nothing that step waits on.
            var falls=Object.FindObjectsByType<AfterTalk>(FindObjectsSortMode.None).Single(t=>t.node=="VessaFallsBack");
            Assert.AreNotEqual(quest.steps[step].target,falls.heardFlag,"hearing Vessa's fall-back talk completes the gallery's step");
            var yarn=File.ReadAllText(Path.Combine(Application.dataPath,"_Project/Resources/Dialogue/Harrowgate.yarn"));int from=yarn.IndexOf("title: VessaFallsBack");Assert.GreaterOrEqual(from,0,"fixture: the node is in the script");
            StringAssert.DoesNotContain("<<setFlag "+quest.steps[step].target+">>",yarn.Substring(from,yarn.IndexOf("===",from)-from),"VessaFallsBack's last line sets the gallery's step");
            State.questSteps["Harrowgate"]=step;
            GameServices.Current.Flags.SetBool(falls.heardFlag,true);Lattice.Rpg.RpgServices.Quests.Report(Lattice.Data.ObjectiveKind.Flag,falls.heardFlag);
            Assert.AreEqual(step,State.questSteps["Harrowgate"],"the main quest moves on as Vessa falls back, with half her bar to go");
            var hero=Lead;Put(hero,new Vector2(0,150));hero.Health.InvulnerableUntil=GameTime.Now+60;Put(Partner,new Vector2(-2,150));Partner.Health.InvulnerableUntil=GameTime.Now+60;
            var exit=BossExit.For("VessaKorl");Assert.IsNotNull(exit,"fixture: her way out is in the gallery");
            var definition=GameCatalog.Find<Lattice.Data.EnemyDef>("VessaKorl");
            bool Down(EnemyBrain body){var rig=body!=null?body.GetComponentInChildren<Animator>():null;return rig!=null&&rig.GetCurrentAnimatorStateInfo(0).IsName("Down");}
            bool Walking(EnemyBrain body){var rig=body!=null?body.GetComponentInChildren<Animator>():null;if(rig==null)return false;var now=rig.GetCurrentAnimatorStateInfo(0);var next=rig.GetNextAnimatorStateInfo(0);return now.IsName("Walk")||rig.IsInTransition(0)&&next.IsName("Walk");}
            // The fault, built: her driver left on as she falls, with nothing telling it she leaves (the shared driver now stands a body up that has a departure, so that goes too).
            var fallen=ActorFactory.Enemy(definition,Ground(new Vector2(-8,146))+Vector3.up*.1f);yield return new WaitForSecondsRealtime(.4f);
            fallen.Health.Receive(new DamagePacket{source=hero.Health,amount=fallen.Health.maximum*20,type=DamageType.Kinetic});
            foreach(var leaving in fallen.GetComponents<BossDeparture>())Object.DestroyImmediate(leaving);
            foreach(var rig in fallen.GetComponentsInChildren<GeneratedAnimator>())rig.enabled=true;
            float deadline=Time.unscaledTime+2;while(fallen!=null&&!Down(fallen)&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(fallen==null||Down(fallen),"fixture: the check can't see a Vessa whose own driver puts her down as she leaves");
            yield return new WaitForSecondsRealtime(4);
            // As she is: the step waits for her leaving, and moves with it; she walks out toward her door and never lies down.
            foreach(var key in new[]{BossController.BeatenFlag,BossController.FallBackHeardFlag,"bossdown.VessaKorl"})GameServices.Current.Flags.SetBool(key,false);State.questSteps["Harrowgate"]=step;
            var vessa=ActorFactory.Enemy(definition,Ground(new Vector2(8,146))+Vector3.up*.1f);yield return new WaitForSecondsRealtime(.4f);var started=vessa.transform.position;
            Assert.AreEqual(step,State.questSteps["Harrowgate"],"fixture: the main quest is at the gallery's step while she stands");
            vessa.Health.Receive(new DamagePacket{source=hero.Health,amount=vessa.Health.maximum*20,type=DamageType.Kinetic});yield return null;
            Assert.IsTrue(Flag(BossController.BeatenFlag),"at nought the gallery's step isn't done");Assert.AreEqual(step+1,State.questSteps["Harrowgate"],"the main quest doesn't move on as she leaves");
            Assert.IsTrue(Flag(BossController.FallBackHeardFlag),"her fall-back talk is still owed after she has gone");
            bool lay=false,walked=false;float moved=0;
            for(float until=Time.unscaledTime+3.2f;vessa!=null&&Time.unscaledTime<until;)
            {
                if(Down(vessa))lay=true;if(Walking(vessa))walked=true;
                moved=Mathf.Max(moved,Vector3.Dot(Flat(vessa.transform.position-started)>0?vessa.transform.position-started:Vector3.zero,(exit.transform.position-started).normalized));yield return null;
            }
            Assert.IsFalse(lay,"Vessa lies down on the gallery floor as she leaves");Assert.IsTrue(walked,"Vessa doesn't walk out");Assert.Greater(moved,1.5f,"Vessa doesn't go toward her way out");
        }

        /// <summary>The whole-game capture (s49/033, s49/046): the Colossus's crust was a grey, see-through disc in its own grey steam with no edge of a colour, and nobody could tell it
        /// from steam, from hazard or from safe ground. A crust is now a body of black slag lying under its boundary (a slow, so a boundary: D222), under every marking, the size of what
        /// slows; bright slag is the danger and is drawn as one. The old crust (a grey ring and nothing under it) is laid in the fixture first and fails the same check.</summary>
        [UnityTest] public IEnumerator TheColossussCrustIsBlackSlagUnderItsBoundary()
        {
            Set("vask.sluice1","vask.sluice2","bossdown.VessaKorl","clear.VessaKorl",BossController.BeatenFlag);
            yield return Zone("Heart");Put(Lead,new Vector2(0,221));Put(Partner,new Vector2(2,221));
            string Plain(LingeringArea area)
            {
                var body=area.GetComponentsInChildren<MeshRenderer>().FirstOrDefault(r=>r.name=="Slag crust");if(body==null)return "nothing is drawn under it";
                if(body.sharedMaterial==null||!body.sharedMaterial.name.Contains("crust"))return "its body isn't crust ("+(body.sharedMaterial!=null?body.sharedMaterial.name:"none")+")";
                var floor=Ground(new Vector2(area.transform.position.x,area.transform.position.z)).y;
                if(body.bounds.max.y>floor+.058f)return "its body stands over the height a marking is laid at ("+(body.bounds.max.y-floor).ToString("0.000")+" m)";
                if(Mathf.Min(body.bounds.extents.x,body.bounds.extents.z)<area.Radius*.7f)return "its body is smaller than the ground that slows";
                return null;
            }
            // The fault, built: the crust as it was laid.
            var old=LingeringArea.Lay(Ground(new Vector2(-6,236)),2.4f,6,AreaEffect.Slow,.6f,0,null,DamageType.Plasma,new Color(.6f,.62f,.68f,.38f));yield return null;
            Assert.IsNotNull(Plain(old),"fixture: the check can't tell the old crust (a grey ring over nothing) from slag");old.Dismiss();
            var colossus=ActorFactory.Enemy(GameCatalog.Find<Lattice.Data.EnemyDef>("SlagColossus"),Ground(new Vector2(0,245))+Vector3.up*.1f);yield return new WaitForSecondsRealtime(.5f);
            var boss=colossus.GetComponent<BossController>();Lead.Health.InvulnerableUntil=GameTime.Now+60;Partner.Health.InvulnerableUntil=GameTime.Now+60;
            Assert.IsTrue(boss.Perform("Pour"),"fixture: the Colossus pours");
            yield return Until(()=>LingeringArea.All.Any(a=>a!=null&&a.Effect==AreaEffect.Slow),4,"its pour lays no crust");
            var crusts=LingeringArea.All.Where(a=>a!=null&&a.Effect==AreaEffect.Slow).ToArray();
            foreach(var crust in crusts){var fault=Plain(crust);Assert.IsNull(fault,"the Colossus's crust at "+crust.transform.position+": "+fault);}
            Object.Destroy(colossus.gameObject);yield return null;
        }

        // ------------------------------------------------------------------ the casting gallery and its door
        [UnityTest] public IEnumerator TheCastingGalleryIsBuiltToItsContract()
        {
            Set("vask.sluice1","vask.sluice2");
            yield return Zone("Gallery");
            var duel=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="VessaKorl");
            Assert.AreEqual(2,duel.spawners.Length,"Vessa's encounter isn't her and her squad");
            var vessa=duel.spawners[0];var squad=duel.spawners[1];
            Assert.AreEqual(VessaPost.x,vessa.transform.position.x,.1f);Assert.AreEqual(VessaPost.y,vessa.transform.position.z,.1f,"Vessa's post isn't the contract's");
            Assert.IsTrue(vessa.faceAsPlaced&&Vector3.Angle(vessa.transform.forward,Vector3.back)<5,"Vessa doesn't face the door");
            Assert.AreEqual(2,squad.count);Assert.AreEqual(2.1f,squad.radius,.01f,"her squad isn't a pair at the pair's own spacing");
            Assert.AreEqual(SquadPost.y,squad.transform.position.z,.1f,"her squad isn't at its post");
            Assert.AreEqual(("vask.vessaMet","Gallery",2),(duel.requiredFlag,duel.checkpointSpawn,duel.membranes.Length),"the door isn't the contract's");
            Assert.IsTrue(duel.membranes.Any(m=>Mathf.Abs(m.transform.position.z-157)<1)&&duel.membranes.Any(m=>Mathf.Abs(m.transform.position.z-202.6f)<1),"the seals aren't at the south door and the heart stair");
            Assert.IsTrue(duel.membranes.All(m=>m.Open),"a seal is shut before the fight");
            var box=duel.GetComponent<BoxCollider>();Assert.That(box.size.x,Is.InRange(46f,52f));Assert.That(box.size.z,Is.InRange(40f,44f));
            Assert.GreaterOrEqual(duel.previewRadius,Vector3.Distance(duel.previewPoint.position,vessa.transform.position)-.5f,"her preview doesn't take in her post");
            // Cover: four racks, solid, at the contract's places and heights; nothing else solid in the middle or by her post; the floor near level inside r 16.
            float floor=Ground(G).y;
            foreach(var at in Racks)
            {
                Assert.IsTrue(Physics.Raycast(new Vector3(at.x,floor+10,at.y),Vector3.down,out var hit,20,~0,QueryTriggerInteraction.Ignore),"fixture: something under "+at);
                Assert.That(hit.point.y-floor,Is.InRange(1.8f,2.4f),"the rack at "+at+" isn't 1.8 to 2.4 m of cover ("+hit.collider.name+")");
            }
            for(float x=-16;x<=16;x+=1)for(float z=-16;z<=16;z+=1)
            {
                var p=G+new Vector2(x,z);if(new Vector2(x,z).magnitude>16||Racks.Any(r=>Mathf.Abs(p.x-r.x)<2.6f&&Mathf.Abs(p.y-r.y)<1.2f))continue;
                Assert.IsTrue(Physics.Raycast(new Vector3(p.x,floor+10,p.y),Vector3.down,out var hit,20,~0,QueryTriggerInteraction.Ignore));
                Assert.AreEqual(floor,hit.point.y,.3f,"something stands on the gallery's floor at "+p+": "+hit.collider.name);
            }
            // Her way out, and the checkpoint.
            var exit=BossExit.For("VessaKorl");Assert.IsNotNull(exit,"Vessa has no way out");Assert.Less(Vector2.Distance(new Vector2(exit.transform.position.x,exit.transform.position.z),GantryDoor),.5f,"her way out isn't the gantry door");Assert.IsFalse(exit.sinks);
            Assert.IsTrue(Physics.Raycast(new Vector3(GantryDoor.x,floor+1.2f,GantryDoor.y),Vector3.forward,out var bars,4,~0,QueryTriggerInteraction.Ignore),"the gantry door isn't barred to the party");
            var spawn=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Single(s=>s.id=="Gallery");
            Assert.Less(spawn.transform.position.z,157-1,"the checkpoint is inside the seal");Assert.IsTrue(NavMesh.SamplePosition(spawn.transform.position,out _,1.5f,NavMesh.AllAreas),"the checkpoint isn't on the paths");
            var bay=Object.FindObjectsByType<RepairBay>(FindObjectsSortMode.None).OrderBy(r=>Vector3.Distance(r.transform.position,spawn.transform.position)).First();
            Assert.IsTrue(bay.free&&bay.saveSpawn=="Gallery"&&Vector3.Distance(bay.transform.position,spawn.transform.position)<20,"there is no free cradle at the gallery door");
        }

        [UnityTest] public IEnumerator TheHeartIsBuiltToTheBossContract()
        {
            Set("vask.sluice1","vask.sluice2","bossdown.VessaKorl","clear.VessaKorl","vask.vessaBeaten");
            yield return Zone("Heart");
            var C=new Vector2(0,251);
            var duel=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="SlagColossus");
            var post=duel.spawners.Single();Assert.AreEqual(C.x,post.transform.position.x,.1f);Assert.AreEqual(C.y,post.transform.position.z,.1f,"the Colossus's spawner isn't at C");
            Assert.IsTrue(post.faceAsPlaced&&Vector3.Angle(post.transform.forward,Vector3.back)<5,"the Colossus doesn't face the door");
            Assert.AreEqual(("vask.cageSeen","Heart",1),(duel.requiredFlag,duel.checkpointSpawn,duel.membranes.Length),"the door isn't the contract's");
            Assert.AreEqual(227,duel.membranes[0].transform.position.z,.3f,"the seal isn't across the door at z 227");Assert.IsTrue(duel.membranes[0].Open,"the seal is shut before the fight");
            var box=duel.GetComponent<BoxCollider>();Assert.That(box.size.x,Is.InRange(42f,46f));Assert.That(box.size.z,Is.InRange(42f,46f));
            Assert.AreEqual(229,duel.previewPoint.position.z,.3f,"the Colossus isn't previewed from the landing inside the door");Assert.GreaterOrEqual(duel.previewRadius,Vector3.Distance(duel.previewPoint.position,post.transform.position)-.5f);
            // The sluices: three seats at r 15 on their bearings, facing C, each with its wheel, its spout on the rim, a solid housing behind it and nothing else solid near it.
            var seats=new Dictionary<int,Vector2>{[1]=new(-5.13f,236.90f),[2]=new(14.77f,253.60f),[3]=new(-9.64f,262.49f)};
            var sluices=Object.FindObjectsByType<CoolantSluice>(FindObjectsSortMode.None);Assert.AreEqual(3,sluices.Length,"the heart hasn't three coolant sluices");
            foreach(var sluice in sluices)
            {
                Assert.IsTrue(seats.TryGetValue(sluice.index,out var at),"a sluice has no place in the contract: "+sluice.index);
                Assert.Less(Vector2.Distance(new Vector2(sluice.transform.position.x,sluice.transform.position.z),at),.1f,"sluice "+sluice.index+" isn't at its seat");
                var toC=new Vector3(C.x-at.x,0,C.y-at.y).normalized;Assert.Less(Vector3.Angle(sluice.transform.forward,toC),3,"sluice "+sluice.index+" doesn't face C");
                Assert.AreEqual("SlagColossus",sluice.settledBy);Assert.IsNotNull(sluice.wheel,"sluice "+sluice.index+" has no wheel");Assert.IsNotNull(sluice.spout,"sluice "+sluice.index+" has no spout");
                var spout=new Vector2(sluice.spout.position.x,sluice.spout.position.z);Assert.AreEqual(18.5f,Vector2.Distance(spout,C),.3f,"sluice "+sluice.index+"'s spout isn't on the rim");
                Assert.AreEqual(2.6f,sluice.spout.position.y-sluice.transform.position.y,.2f,"sluice "+sluice.index+"'s spout isn't 2.6 m up");
                Assert.AreEqual(1.2f,sluice.wheel.position.y-sluice.transform.position.y,.15f,"sluice "+sluice.index+"'s wheel hub isn't 1.2 m up");
                Assert.IsTrue(NavMesh.SamplePosition(sluice.transform.position,out _,.6f,NavMesh.AllAreas),"sluice "+sluice.index+"'s seat isn't on the paths");
                Assert.IsTrue(Physics.Raycast(sluice.transform.position+Vector3.up,-toC,out var housing,2.2f,~0,QueryTriggerInteraction.Ignore),"sluice "+sluice.index+" has no solid housing behind its seat");
                // Round the seat: the floor is open but for the housing behind it.
                for(float a=0;a<360;a+=30)
                {
                    var dir=Quaternion.Euler(0,a,0)*Vector3.forward;if(Vector3.Angle(dir,-toC)<50)continue;
                    var probe=sluice.transform.position+dir*1.8f;Assert.IsFalse(Physics.Raycast(probe+Vector3.up*4,Vector3.down,out var top,3.7f,~0,QueryTriggerInteraction.Ignore)&&top.collider.GetComponentInParent<Health>()==null,"something solid stands by sluice "+sluice.index+"'s seat: "+top.collider?.name);
                }
            }
            // The floor: level inside r 20, and nothing solid on it but the three stacks; the stacks where the contract has them, 2.2 m tall.
            float floor=Ground(C).y;var stacks=new[]{new Vector2(5.79f,244.11f),new Vector2(-8.86f,249.44f),new Vector2(3.08f,259.46f)};
            foreach(var at in stacks){Assert.IsTrue(Physics.Raycast(new Vector3(at.x,floor+10,at.y),Vector3.down,out var hit,20,~0,QueryTriggerInteraction.Ignore));Assert.AreEqual(2.2f,hit.point.y-floor,.15f,"the stack at "+at+" isn't 2.2 m of cover ("+hit.collider.name+")");}
            for(float x=-20;x<=20;x+=1)for(float z=-20;z<=20;z+=1)
            {
                var p=C+new Vector2(x,z);if(new Vector2(x,z).magnitude>20||stacks.Any(s=>Vector2.Distance(s,p)<1.8f))continue;
                Assert.IsTrue(Physics.Raycast(new Vector3(p.x,floor+10,p.y),Vector3.down,out var hit,20,~0,QueryTriggerInteraction.Ignore));
                if(hit.collider.GetComponentInParent<Health>()!=null)continue;
                // The contract puts the sluices' housings on the rim's inner edge (centred at r 16.4, outside r 16 bar their front faces): nothing else may stand there.
                if(hit.collider.name=="Heart sluice housing"&&new Vector2(x,z).magnitude>15.6f)continue;
                Assert.AreEqual(floor,hit.point.y,.3f,"the heart's floor isn't level and open at "+p+": "+hit.collider.name);
            }
            Assert.IsNull(BossExit.For("SlagColossus"),"the Colossus has a way out: it is destroyed");
        }

        [UnityTest] public IEnumerator BothBossDoorsAreCheckpointsAndShutBehindTheParty()
        {
            Set("vask.sluice1","vask.sluice2","vask.vessaMet","bossdown.VessaKorl","clear.VessaKorl","vask.vessaBeaten","vask.cageSeen");
            foreach(var (door,spawn) in new[]{("VessaKorl","Gallery"),("SlagColossus","Heart")})
            {
                if(door=="VessaKorl"){GameServices.Current.Flags.SetBool("clear.VessaKorl",false);GameServices.Current.Flags.SetBool("bossdown.VessaKorl",false);}
                yield return Zone(spawn);
                var duel=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId==door);
                GameServices.Current.State.spawn="Arrival";duel.Begin();yield return null;
                Assert.AreEqual(spawn,GameServices.Current.State.spawn,"starting "+door+"'s fight doesn't make its door the checkpoint");
                Assert.AreEqual(spawn,GameServices.Current.Saves.Load("autosave").spawn,"starting "+door+"'s fight doesn't save at its door");
                Assert.IsTrue(duel.membranes.All(m=>!m.Open),door+"'s seals don't shut behind the party");
                Set("bossdown.VessaKorl","clear.VessaKorl");
            }
        }

        // ------------------------------------------------------------------ the young road and the release
        [UnityTest] public IEnumerator TheYoungRoadPullsNineAndEasesFourAndGoesWhenFreed()
        {
            var probe=new GameObject("Cadence probe").AddComponent<CagedChoir>();
            float pulling=0,easing=0;for(float t=0;t<probe.Period;t+=.01f){if(probe.PullingAt(t))pulling+=.01f;else easing+=.01f;}
            Assert.AreEqual(9,pulling,.05f,"it doesn't pull for nine");Assert.AreEqual(4,easing,.05f,"it doesn't ease for four");Object.Destroy(probe.gameObject);
            Set("vask.sluice1","vask.sluice2","bossdown.VessaKorl","clear.VessaKorl","vask.vessaBeaten");
            yield return Zone("Heart");
            var choir=Object.FindFirstObjectByType<CagedChoir>();Assert.IsNotNull(choir,"there is no young road in the cage");Assert.IsTrue(choir.body.gameObject.activeInHierarchy,"the cage is empty before it is opened");
            // Its rest is where it hangs easing (it may be pulling as the zone opens).
            yield return Until(()=>!choir.Pulling,14,"it never eases");yield return new WaitForSecondsRealtime(1.4f);var rest=choir.body.position;
            yield return Until(()=>choir.Pulling,6,"it never pulls");yield return new WaitForSecondsRealtime(2);
            Assert.Greater(choir.body.position.y-rest.y,.2f,"pulling, it doesn't draw up against the cage");
            Set("vask.juvenileFreed");
            yield return Until(()=>log.Any(l=>l.StartsWith("CHOIR_FREED")),2,"it isn't freed with the release's last line");
            yield return Until(()=>choir.Gone&&!choir.body.gameObject.activeInHierarchy,choir.wait+choir.riseSeconds+3,"it never goes up through the vent");
            log.Clear();yield return Zone("Heart");
            choir=Object.FindFirstObjectByType<CagedChoir>();yield return new WaitForSecondsRealtime(.3f);
            Assert.IsFalse(choir.body.gameObject.activeInHierarchy,"it is back in its cage on a load after the release");Assert.IsFalse(log.Any(l=>l.StartsWith("CHOIR_FREED")),"a loaded release plays again");
        }

        /// <summary>Nobody appears in view (the look review: "the smiths appear" is a fault a player sees). Through the fight they wait out of frame, behind the barred door in the
        /// dais's north wall and beyond the gantry's east door; at the fall the bars go and each walks to their place; as the release ends Tovra and Brekk walk to the anvil and Vessa
        /// off the gantry; a load stands everyone where the story has put them, and Vessa nowhere.</summary>
        [UnityTest] public IEnumerator TheReleaseIsStagedFromTheColossussFall()
        {
            Set("vask.sluice1","vask.sluice2","bossdown.VessaKorl","clear.VessaKorl","vask.vessaBeaten");
            yield return Zone("Heart");
            GameObject Named(string name)=>Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.gameObject.scene==SceneManager.GetActiveScene()&&t.name==name).Select(t=>t.gameObject).Single();
            var smiths=Named("The smiths at the clamps");var tovra=Named("Tovra");var brekk=Named("Brekk");var vessa=Named("Vessa on the gantry");var grille=Named("Smiths door grille");
            Assert.IsFalse(smiths.activeInHierarchy||tovra.activeInHierarchy||brekk.activeInHierarchy||vessa.activeInHierarchy,"the release's people are in the heart before its fight");
            Assert.GreaterOrEqual(smiths.transform.childCount,8,"the smiths at the clamps are too few to read as sixty");
            // The fight's door talk has been heard: they are waiting. (Its fight counted as done, so a hero stood on its floor wakes nothing: what is measured is the waiting.)
            Set("vask.cageSeen","clear.SlagColossus");yield return new WaitForSecondsRealtime(.4f);
            var walkers=smiths.GetComponentsInChildren<Guide>().Concat(new[]{tovra,brekk,vessa}.Select(w=>w.GetComponent<Guide>())).ToArray();
            Assert.IsTrue(walkers.All(g=>g!=null)&&walkers.Length>=11,"fixture: everyone at the release walks to it (a Guide each)");
            Assert.IsTrue(grille.activeInHierarchy,"the smiths' door isn't barred through the fight");
            var starts=walkers.ToDictionary(g=>g,g=>g.transform.position);
            bool Seen(GameObject who){var planes=GeometryUtility.CalculateFrustumPlanes(Camera.main);return who.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&GeometryUtility.TestPlanesAABB(planes,r.bounds));}
            // From anywhere on the fight's floor, nobody waiting is in the frame.
            foreach(var at in new[]{new Vector2(0,251),new Vector2(0,270),new Vector2(-10,262),new Vector2(10,262),new Vector2(-14,251),new Vector2(14,251),new Vector2(0,234)})
            {
                Put(Lead,at);Put(Partner,at+new Vector2(2.2f,-1));yield return new WaitForSecondsRealtime(1.2f);
                foreach(var g in walkers)Assert.IsFalse(Seen(g.gameObject),g.name+" can be seen waiting from the heart's floor at "+at);
            }
            Put(Lead,new Vector2(0,251));Put(Partner,new Vector2(2.2f,250));yield return new WaitForSecondsRealtime(1.2f);
            // The Colossus falls: the bars go, and nobody is put anywhere: each sets off from where they waited, out of frame.
            Set("bossdown.SlagColossus");yield return new WaitForSecondsRealtime(.25f);
            Assert.IsFalse(grille.activeInHierarchy,"the smiths' door stays barred after the Colossus's fall");
            foreach(var g in walkers){Assert.Less(Flat(g.transform.position-starts[g]),1.6f,g.name+" was put at its place, not walked there, as the Colossus fell");Assert.IsFalse(Seen(g.gameObject),g.name+" appears in view as the Colossus falls");}
            yield return Until(()=>walkers.All(g=>g.Stage>=1),2,"the release's people don't set off at the Colossus's fall");
            // The release's own flag, set before its talk's delay is out, so the test hears no conversation; Tovra, Brekk and Vessa finish their way in before they go on.
            yield return new WaitForSecondsRealtime(2);Set("vask.juvenileFreed");
            Vector3 End(Guide g,int leg)=>g.legs[leg].path[^1];
            var smithWalkers=smiths.GetComponentsInChildren<Guide>();
            yield return Until(()=>smithWalkers.All(g=>Flat(g.transform.position-End(g,1))<.3f),12,"the smiths never reach their clamps");
            // Each faces the cage it holds: with no face for a last leg's end a Guide stands as its walk in left it, and five smiths stood in a row facing nothing (whole-game capture s49/140).
            float Facing(GameObject who,Vector3 target){var look=who.transform.forward;look.y=0;var to=target-who.transform.position;to.y=0;return Vector3.Angle(look,to);}
            // Within reach of its clamp a smith may still be on its last step (a Guide stops a tenth of a metre short and turns to its face the frame after): read them stood.
            yield return Until(()=>smithWalkers.All(g=>Flat(g.transform.position-End(g,1))<.1f),4,"the smiths never stop at their clamps");
            for(int frame=0;frame<3;frame++)yield return null;var cage=new Vector3(0,Deep,279);
            // Built wrong on purpose, from their own paths: with the old faces, one short, each would stand as its last leg brought it in, and the check must catch that.
            float WalkedIn(Guide g){var path=g.legs[^1].path;var from=path.Length>1?path[^2]:g.legs.Length>1?g.legs[^2].path[^1]:starts[g];var look=path[^1]-from;look.y=0;var to=cage-path[^1];to.y=0;return Vector3.Angle(look,to);}
            Assert.IsTrue(smithWalkers.Any(g=>WalkedIn(g)>=40),"fixture: standing as their walk in left them the smiths would face the cage anyway, so the check below can't fail");
            foreach(var g in smithWalkers)Assert.Greater(g.faces.Length,g.legs.Length,g.name+" has no face for its last leg's end");
            foreach(var g in smithWalkers)Assert.Less(Facing(g.gameObject,cage),40,g.name+" at its clamp doesn't face the cage");
            foreach(var w in new[]{tovra,brekk,vessa}){var g=w.GetComponent<Guide>();yield return Until(()=>g.Stage==2&&Flat(w.transform.position-End(g,2))<.3f,16,w.name+" never walks on after the release");}
            Assert.IsTrue(smiths.activeInHierarchy,"the smiths leave with the release");
            // A built piece's mesh is laid in the world's own space (its transform at the origin): where it is is where it is drawn.
            var anvil=GameObject.Find("Tovra's anvil").GetComponent<Renderer>().bounds.center;
            Assert.Less(Flat(tovra.transform.position-anvil),1.6f,"Tovra isn't at the anvil after the release");Assert.Less(Flat(brekk.transform.position-anvil),2.8f,"Brekk isn't beside her");
            foreach(var w in new[]{tovra,brekk}){var g=w.GetComponent<Guide>();yield return Until(()=>Flat(w.transform.position-End(g,2))<.1f,4,w.name+" never stops at the anvil");}
            for(int frame=0;frame<3;frame++)yield return null;
            foreach(var w in new[]{tovra,brekk})Assert.Less(Facing(w,anvil),45,w.name+" doesn't turn to the anvil after the release");
            var stand=Point(p=>p.dialogueNode=="BrekkAnvil","BrekkAnvil");Assert.IsTrue(stand.Available,"Brekk's line by the anvil can't be heard after the release");
            Assert.Less(Flat(stand.transform.position-anvil),4,"Brekk's line is offered away from the anvil");
            // Loaded after it: everyone where the story put them, nothing walked again, and Vessa gone.
            yield return Zone("Heart");yield return new WaitForSecondsRealtime(.3f);
            smiths=Named("The smiths at the clamps");tovra=Named("Tovra");brekk=Named("Brekk");vessa=Named("Vessa on the gantry");
            Assert.IsFalse(vessa.activeInHierarchy,"Vessa is on the gantry on a load after the release");
            foreach(var w in new[]{tovra,brekk}){var g=w.GetComponent<Guide>();Assert.Less(Flat(w.transform.position-End(g,2)),.3f,w.name+" isn't at the anvil on a load after the release");}
            foreach(var g in smiths.GetComponentsInChildren<Guide>())Assert.Less(Flat(g.transform.position-End(g,1)),.3f,g.name+" isn't at the clamps on a load after the release");
        }
        static float Flat(Vector3 v){v.y=0;return v.magnitude;}

        // ------------------------------------------------------------------ the camera
        [UnityTest] public IEnumerator NoWallOrTallPieceStandsBetweenTheCameraAndAFight()
        {
            Set("vask.sluice1","vask.sluice2");
            yield return Zone("Gallery");
            var toCamera=-(Quaternion.Euler(SightlineAudit.Pitch,SightlineAudit.Yaw,0)*Vector3.forward);var hidden=new List<string>();int samples=0;
            foreach(var fight in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))
            {
                var box=fight.GetComponent<BoxCollider>().bounds;
                for(float x=box.min.x+1;x<box.max.x;x+=2)for(float z=box.min.z+1;z<box.max.z;z+=2)
                {
                    if(!Physics.Raycast(new Vector3(x,box.max.y+8,z),Vector3.down,out var floor,40,~0,QueryTriggerInteraction.Ignore)||!NavMesh.SamplePosition(floor.point,out var on,.4f,NavMesh.AllAreas))continue;
                    if(Mathf.Abs(on.position.y-(box.center.y-1.5f))>1.7f)continue;
                    samples++;var chest=on.position+Vector3.up*SightlineAudit.Chest;
                    foreach(var hit in Physics.RaycastAll(chest,toCamera,SightlineAudit.Distance-SightlineAudit.NearIgnore,~0,QueryTriggerInteraction.Ignore))
                    {
                        if(hit.collider.GetComponentInParent<Health>()!=null||hit.collider.GetComponentInParent<CharacterController>()!=null)continue;
                        var size=hit.collider.bounds.size;if(hit.collider.bounds.max.y-on.position.y<3||size.x<1.6f&&size.z<1.6f)continue;
                        hidden.Add(fight.encounterId+" at ("+x.ToString("0")+", "+z.ToString("0")+") behind "+hit.collider.name);break;
                    }
                }
            }
            Assert.Greater(samples,400,"fixture: the fights' floors were sampled");
            Assert.IsEmpty(hidden,hidden.Count+" of "+samples+" fight floor points are hidden from the camera: "+string.Join("; ",hidden.Take(12)));
        }
        /// <summary>The camera engineer's check of November 21 (HeroInSightTests.TheFoundrysHangingIronHidesNoHeroItDoesNotDrawThrough): the charging cranes' girders and crucibles and the ladle
        /// line L2's rail hung across two fights under the camera, and a band of each fight's floor lay out of the play view behind them, so a danger marking painted there would not be seen
        /// (the guide: a marking is never hidden by anything). From the play camera's line at every metre of every fight's floor, nothing hung overhead (drawn, opaque, 2.4 m or more over
        /// the floor under it, with nothing solid there) hides the floor there: of a patch the width of a marking's rim (a ring 20 cm round the point, and the point), most of it. A slender
        /// rail or a hanger the floor reads past is not a hiding piece; the drones' rails, that were girders, are such (and are measured: they are out of the static batch). Heroes are drawn
        /// through what hides them; a marking is not. A girder hung as the east charging crane's was is laid in the fixture first and must be caught.</summary>
        [UnityTest] public IEnumerator NothingHungOverheadHidesAFightsFloorFromTheCamera()
        {
            Set("vask.sluice1","vask.sluice2");
            yield return Zone("Gallery");
            var toCamera=-(Quaternion.Euler(SightlineAudit.Pitch,SightlineAudit.Yaw,0)*Vector3.forward);
            // Built wrong on purpose: a girder across the charging floor ten and a half metres up, drawn and not solid, where the east charging crane's was.
            var girder=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.DestroyImmediate(girder.GetComponent<Collider>());girder.name="Fixture girder";
            girder.GetComponent<MeshFilter>().sharedMesh=Object.Instantiate(girder.GetComponent<MeshFilter>().sharedMesh);
            girder.transform.position=new Vector3(0,10.5f,88);girder.transform.localScale=new Vector3(46,.8f,.5f);
            // What is drawn, made solid for this measure only, on a layer of its own so it is never taken for the solid world it is measured against.
            const int Sight=2;int world=~(1<<Sight);var named=new Dictionary<Collider,string>();var seen=new HashSet<Mesh>();var unread=new List<string>();
            foreach(var view in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if(!view.enabled||!view.gameObject.activeInHierarchy||view.GetComponentInParent<Health>()!=null||view.GetComponentInParent<Telegraph>()!=null)continue;
                if(view.sharedMaterials.All(m=>m==null||m.renderQueue>=2500))continue;
                var filter=view.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
                // A static body's filter holds its batch in play, which can't be read: named, so what this measure could not take is seen.
                if(!filter.sharedMesh.isReadable){if(view.bounds.size.y<6&&view.bounds.min.y>-12)unread.Add(view.name);continue;}
                if(!seen.Add(filter.sharedMesh))continue;
                var solid=new GameObject("Sight fixture: "+view.name){layer=Sight};solid.transform.SetParent(filter.transform,false);
                var body=solid.AddComponent<MeshCollider>();body.sharedMesh=filter.sharedMesh;named[body]=(view.isPartOfStaticBatch?"(a static batch with) ":"")+view.name;
            }
            // The ladle lines' ladles at rest, as the floor sees them between pours: a ladle and its stream over the trough as it pours are the hazard itself, read by watching them come.
            var pours=Object.FindObjectsByType<PourChannel>(FindObjectsSortMode.None).Where(p=>p.ladle!=null).ToArray();
            foreach(var pour in pours){pour.enabled=false;pour.ladle.position=pour.ladleRest;if(pour.hanger!=null)pour.hanger.position=pour.ladleRest;if(pour.stream!=null)pour.stream.SetActive(false);}
            Physics.SyncTransforms();
            // What hung overhead stands between the lens and this point of the floor, if anything.
            string HungOver(Vector3 point)
            {
                foreach(var hit in Physics.RaycastAll(point,toCamera,SightlineAudit.Distance,1<<Sight,QueryTriggerInteraction.Ignore))
                {
                    // Hung, not stood: well over the floor beneath it, and nothing solid where the line meets it (a wall's face, a rack's, is the wall test's).
                    var outside=hit.point+toCamera*.05f;
                    if(!Physics.Raycast(outside,Vector3.down,out var beneath,40,world,QueryTriggerInteraction.Ignore)||outside.y-beneath.point.y<2.4f)continue;
                    if(Physics.CheckSphere(hit.point,.2f,world,QueryTriggerInteraction.Ignore))continue;
                    // A column or a post stands on its own foot: it is there, a body's width round, at the floor straight under where the line meets it.
                    if(Physics.OverlapCapsule(beneath.point+Vector3.up*.1f,beneath.point+Vector3.up*.8f,.35f,1<<Sight,QueryTriggerInteraction.Ignore).Contains(hit.collider))continue;
                    return named.TryGetValue(hit.collider,out var n)?n:hit.collider.name;
                }
                return null;
            }
            var hidden=new Dictionary<string,List<string>>();int samples=0;
            foreach(var fight in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))
            {
                var box=fight.GetComponent<BoxCollider>().bounds;
                for(float x=box.min.x+.5f;x<box.max.x;x+=1)for(float z=box.min.z+.5f;z<box.max.z;z+=1)
                {
                    if(!Physics.Raycast(new Vector3(x,box.max.y+8,z),Vector3.down,out var floor,40,world,QueryTriggerInteraction.Ignore)||!NavMesh.SamplePosition(floor.point,out var on,.4f,NavMesh.AllAreas))continue;
                    if(Mathf.Abs(on.position.y-(box.center.y-1.5f))>1.7f)continue;
                    samples++;var centre=on.position+Vector3.up*.1f;var by=new List<string>();
                    for(int k=-1;k<8;k++){var from=k<0?centre:centre+Quaternion.Euler(0,k*45,0)*Vector3.forward*.2f;var one=HungOver(from);if(one!=null)by.Add(one);}
                    if(by.Count<5)continue;
                    string piece=by.GroupBy(b=>b).OrderByDescending(g=>g.Count()).First().Key;
                    if(!hidden.TryGetValue(piece,out var where))hidden[piece]=where=new List<string>();
                    where.Add(fight.encounterId+" ("+x.ToString("0")+", "+z.ToString("0")+")");
                }
            }
            foreach(var body in named.Keys)Object.Destroy(body.gameObject);Object.Destroy(girder);foreach(var pour in pours)pour.enabled=true;yield return null;
            TestContext.WriteLine($"FOUNDRY_HUNG measured {named.Count} drawn pieces over {samples} fight floor points; unreadable (static batches, not measured): {string.Join(", ",unread.Distinct().Take(40))}");
            foreach(var pair in hidden)TestContext.WriteLine($"FOUNDRY_HUNG {pair.Key} hides {pair.Value.Count}: {string.Join(", ",pair.Value.Take(10))}");
            Assert.Greater(samples,400,"fixture: the fights' floors were sampled");
            Assert.IsTrue(named.Values.Any(v=>v.StartsWith("Drone rail girder")),"fixture: the drones' rails, hung over their fight, aren't measured (a static batch can't be read in play)");
            Assert.IsTrue(hidden.ContainsKey("Fixture girder"),"fixture: the check can't see a girder hung across the charging floor under the camera");
            hidden.Remove("Fixture girder");
            Assert.IsEmpty(hidden,"fight floor hidden from the camera by what hangs over it ("+samples+" points): "+string.Join("; ",hidden.Select(p=>p.Key+" over "+p.Value.Count+", "+string.Join(", ",p.Value.Take(4)))));
        }
    }
}
