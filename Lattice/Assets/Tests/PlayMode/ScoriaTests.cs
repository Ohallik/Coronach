using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 8's wild zone, the Scoria Flows (docs/maps/ScoriaFlows.md; rule: the crust tells you). The crust's own rule on a bare floor (CrustField: a plate's clock, who it burns
    /// and where it puts them, the partner, the enemies, the blister over a hide, the hold in a conversation); then the zone: the valley walked from the landing to the gate, the camp and
    /// the pockets, every flow crossed going north on the clock the brief gives it, the lesson before the test, the talks, doors and kettle stones, the fights and the crawlers' hides, a
    /// marking laid on open melt measured against the same frame without it, the camera's line to every fight, and the ash squall. Flags are set directly where a conversation would set them.</summary>
    public sealed class ScoriaTests
    {
        const string Zone="ScoriaFlows";
        // The builder's numbers (HarrowgateBuilder.Scoria), copied: the tests can't see the editor assembly.
        const float Bed=-.32f;
        static readonly Vector2 Arrival=new(2,9),IslandA=new(-5,121),IslandB=new(4,136.5f),GlassIsland=new(-22,141),CampSpawn=new(39,204),FoundrySpawn=new(0,251),HarrowgateSpawn=new(61,205),
            TovraAt=new(46,210.5f),BrekkStall=new(49.5f,200.6f),PikeHead=new(54,208.4f);
        readonly List<string> log=new();
        void Heard(string text,string stack,LogType type){if(text.StartsWith("CRUST_")||text.StartsWith("WHITEOUT_")||text.StartsWith("SCORIA_"))log.Add(text);}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter8");log.Clear();Application.logMessageReceived+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what this zone writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Scoria(ContentRegistry.Book book)
        {
            // The town road out of the Unsworn camp stays shut until Tovra has said where it goes (TovraHarrowgateRoad): a refused press names her.
            book.Lock("vask.roadSaid","Tovra");
        }

        // ------------------------------------------------------------------ fixtures
        static void Set(params string[] keys){foreach(var key in keys)GameServices.Current.Flags.SetBool(key,true);}
        static bool Flag(string key)=>GameServices.Current.Flags.GetBool(key);
        static IEnumerator Load(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");Assert.AreEqual(zone,SceneManager.GetActiveScene().name);
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;}
            deadline=Time.unscaledTime+10;while(DialogueSystem.Current!=null&&!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.5f);
        }
        static (CombatActor lead,CombatActor partner) Party(){var lead=PartyController.Current.Active;return (lead,PartyController.Current.members.First(m=>m!=lead));}
        static void Put(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;Physics.SyncTransforms();
        }
        static IEnumerator Wait(float seconds){float until=Time.time+seconds;while(Time.time<until)yield return null;}
        static Vector3 Ground(Vector2 p)
        {
            var hits=Physics.RaycastAll(new Vector3(p.x,40,p.y),Vector3.down,90,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<CombatActor>()==null).OrderBy(h=>h.distance).ToArray();
            Assert.IsNotEmpty(hits,"fixture: there is ground at "+p);return hits[0].point;
        }
        static bool Reach(Vector3 from,Vector2 to)
        {
            var path=new NavMeshPath();
            return NavMesh.SamplePosition(from,out var start,3,NavMesh.AllAreas)&&NavMesh.SamplePosition(Ground(to),out var end,1.5f,NavMesh.AllAreas)&&
                NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        }
        static Vector2 Centre(CrustField.Plate plate)=>CrustField.Centre(plate);
        static IEnumerable<CrustField.Plate> Field(CrustField crust,string field)=>crust.plates.Where(p=>p.name.StartsWith(field+"-"));

        // ------------------------------------------------------------------ the crust's own rule, on a bare floor
        const float X=230;
        /// <summary>A floor at height nought far from the yard, and a field of one plate 6 m square on it, its east edge at x 236: dark 4, seam 2, open 3.</summary>
        static CrustField BareField(out CrustField.Plate plate)
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Crust fixture floor";floor.transform.position=new Vector3(X,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();
            var go=new GameObject("Crust fixture");go.SetActive(false);var field=go.AddComponent<CrustField>();
            plate=new CrustField.Plate{name="T-0",corners=new[]{new Vector2(X,0),new Vector2(X+6,0),new Vector2(X+6,6),new Vector2(X,6)},floor=0,offset=0,dark=4,seam=2,bright=3};
            var heave=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.Destroy(heave.GetComponent<Collider>());heave.name="Fixture heave";heave.transform.position=new Vector3(X+3,0,3);
            field.plates=new[]{plate};field.heaves=new[]{heave.transform};go.SetActive(true);return field;
        }
        static IEnumerator Arena()
        {
            yield return Load("Arena_Ground");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
        }

        [UnityTest] public IEnumerator APlatesClockGoesDarkLitOpenAndBackAndOnlyOpenMeltOrRisingCrustBurns()
        {
            yield return Arena();var field=BareField(out var plate);yield return null;field.Held=true;
            var on=new Vector3(X+3,0,3);var off=new Vector3(X-3,0,3);
            foreach(var (into,phase,burns,heat) in new[]{(1f,CrustField.Phase.Dark,false,0f),(3.9f,CrustField.Phase.Dark,false,0f),(4.5f,CrustField.Phase.Seam,false,.5f),(6.2f,CrustField.Phase.Bright,true,1f),
                (8.9f,CrustField.Phase.Bright,true,1f),(9.2f,CrustField.Phase.Dark,true,1f)})
            {
                field.Clock=CrustField.ClockFor(plate,into);
                Assert.AreEqual(phase,field.PhaseOf(plate),into+" s into its cycle the plate is in the wrong phase");
                Assert.AreEqual(burns,field.Burns(plate),into+" s into its cycle: "+(burns?"open melt or crust still rising doesn't burn":"dark crust or a lit seam burns"));
                Assert.AreEqual(heat,GroundHeat.Of(on),.01f,into+" s in, combat reads the wrong heat on the plate");
            }
            Assert.AreEqual(0,GroundHeat.Of(off),"ground off every plate reads hot to combat");Assert.AreSame(plate,field.At(on));Assert.IsNull(field.At(off));
            Assert.IsNull(field.At(on+Vector3.up*3),"a body three metres over a plate is taken to be standing on it");
            Object.Destroy(field.gameObject);yield return null;
            Assert.AreEqual(0,GroundHeat.Of(on),"with the field gone the ground still reads hot");
        }

        [UnityTest] public IEnumerator TheLeadOnOpenMeltIsBurntAndThePartySetBackOnTheLastGroundThatDoesntCycle()
        {
            yield return Arena();var field=BareField(out var plate);yield return null;field.Held=true;var (lead,partner)=Party();
            lead.Health.InvulnerableUntil=0;lead.Health.integrity=lead.Health.maximum;
            var bank=new Vector3(X-2,0,3);Put(lead,bank);Put(partner,new Vector3(X-2,0,8));yield return Wait(.3f);
            // The check has to see a field that doesn't burn: switched off, a lead stood on open melt for a second stays there whole.
            field.Clock=CrustField.ClockFor(plate,5.5f);field.enabled=false;Put(lead,new Vector3(X+3,0,3));yield return Wait(1);
            Assert.AreEqual(lead.Health.maximum,lead.Health.integrity,.01f,"fixture: a field switched off still burns");Assert.Less(Vector3.Distance(lead.transform.position,new Vector3(X+3,0,3)),.6f,"fixture: a field switched off still moves the lead");
            Put(lead,bank);field.enabled=true;yield return Wait(.3f);
            // Dark crust holds: two seconds on it and nothing.
            field.Clock=CrustField.ClockFor(plate,.8f);Put(lead,new Vector3(X+3,0,3));yield return Wait(2);
            Assert.AreEqual(lead.Health.maximum,lead.Health.integrity,.01f,"dark crust burns");Assert.IsFalse(log.Any(l=>l.StartsWith("CRUST_THROWN")),"dark crust throws the lead");
            // Open: burnt, a heavy share and never the last of it, and set back with the partner beside on the bank the lead last stood on.
            Put(lead,bank);yield return Wait(.3f);
            field.Clock=CrustField.ClockFor(plate,6.5f);Put(lead,new Vector3(X+3,0,3));yield return Wait(.8f);
            Assert.IsTrue(log.Any(l=>l.StartsWith("CRUST_THROWN "+lead.character)),"the lead stood on open melt and nothing threw them");
            float lost=1-lead.Health.integrity/lead.Health.maximum;Assert.That(lost,Is.InRange(.06f,.19f),"the melt took "+lost.ToString("P0")+" of a bar: it should be a heavy share and no more");
            Assert.IsNull(field.At(lead.transform.position),"the lead was thrown onto another plate");Assert.Less(Vector3.Distance(lead.transform.position,bank),1.2f,"the lead wasn't set back on the bank they last stood on");
            Assert.Less(Vector3.Distance(partner.transform.position,bank),3.5f,"the partner wasn't set down beside the lead");Assert.IsNull(field.At(partner.transform.position),"the partner was set down on a plate");
        }

        [UnityTest] public IEnumerator TheMeltNeverTakesTheLastOfAHeroAndARollThroughASeamIsNotCaught()
        {
            yield return Arena();var field=BareField(out var plate);yield return null;field.Held=true;var (lead,_)=Party();lead.Health.InvulnerableUntil=0;
            Put(lead,new Vector3(X-2,0,3));yield return Wait(.3f);
            // Across open melt in less than its grace (a roll through a lit seam that has just opened): not caught.
            field.Clock=CrustField.ClockFor(plate,6.5f);Put(lead,new Vector3(X+3,0,3));yield return Wait(field.grace*.4f);Put(lead,new Vector3(X+8,0,3));yield return Wait(.5f);
            Assert.IsFalse(log.Any(l=>l.StartsWith("CRUST_THROWN")),"a hero crossing open melt faster than its grace was caught");
            // Nearly gone, and stood on it: thrown, and left a sliver.
            lead.Health.integrity=lead.Health.maximum*.05f;Put(lead,new Vector3(X+3,0,3));yield return Wait(.8f);
            Assert.IsTrue(log.Any(l=>l.StartsWith("CRUST_THROWN")),"fixture: the lead wasn't thrown");Assert.Greater(lead.Health.integrity,0,"the melt took the last of a hero");
            Assert.IsTrue(lead.Health.Alive,"the melt ended the run");
        }

        [UnityTest] public IEnumerator APartnerCaughtAloneIsMovedOffAndNeverBurnt()
        {
            yield return Arena();var field=BareField(out var plate);yield return null;field.Held=true;var (lead,partner)=Party();partner.Health.InvulnerableUntil=0;
            float whole=partner.Health.integrity;Put(lead,new Vector3(X-6,0,3));Put(partner,new Vector3(X-2,0,1));yield return Wait(.3f);
            field.Clock=CrustField.ClockFor(plate,6.5f);Put(partner,new Vector3(X+3,0,3));yield return Wait(.8f);
            Assert.IsTrue(log.Any(l=>l.StartsWith("CRUST_THROWN "+partner.character)),"a partner left on open melt is left there");
            Assert.IsNull(field.At(partner.transform.position),"the partner is still on the plate");Assert.AreEqual(whole,partner.Health.integrity,.01f,"the partner, who doesn't choose where it stands, was burnt");
            Assert.Less(Vector3.Distance(lead.transform.position,new Vector3(X-6,0,3)),.4f,"the lead was moved for the partner's step");
        }

        [UnityTest] public IEnumerator AnEnemyLeftOnOpenMeltBurnsAndAHiddenOneIsLeftAlone()
        {
            yield return Arena();var field=BareField(out var plate);yield return null;field.Held=true;var (lead,partner)=Party();Put(lead,new Vector3(X-14,0,3));Put(partner,new Vector3(X-16,0,3));
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Gloomgrazer"),new Vector3(X+3,0,3));enemy.Passive=true;yield return Wait(.2f);float whole=enemy.Health.integrity;
            field.Clock=CrustField.ClockFor(plate,1);yield return Wait(1.6f);Assert.AreEqual(whole,enemy.Health.integrity,.01f,"an enemy on dark crust burns");
            enemy.Health.Damageable=false;field.Clock=CrustField.ClockFor(plate,6.3f);yield return Wait(1.8f);
            Assert.AreEqual(whole,enemy.Health.integrity,.01f,"an enemy hidden under the crust (out of reach) was burnt");
            enemy.Health.Damageable=true;yield return Wait(1.8f);
            Assert.Less(enemy.Health.integrity,whole-enemy.Health.maximum*.05f,"an enemy left standing on open melt doesn't burn");Assert.IsTrue(log.Any(l=>l.StartsWith("CRUST_BURNS")));
        }

        /// <summary>A plate's drawn outline follows its cracks into its neighbour's footprint; where the two disagree the burn is shrunk to the inside. Two plates side by side, the
        /// west one's cracks reaching a metre into the east one's footprint: open west and dark east, the overlap (drawn west, footprint east) doesn't burn; open east and dark west, the
        /// overlap (footprint east, drawn west, dark) doesn't burn either; both open, it does.</summary>
        [UnityTest] public IEnumerator WhereAPlatesCrackedOutlineAndItsFootprintDisagreeOnlyTheirOverlapOfBothBurns()
        {
            yield return Arena();var field=BareField(out var west);yield return null;field.Held=true;
            var east=new CrustField.Plate{name="T-1",corners=new[]{new Vector2(X+6,0),new Vector2(X+12,0),new Vector2(X+12,6),new Vector2(X+6,6)},floor=0,offset=0,dark=4,seam=2,bright=3};
            west.seeds=new[]{new Vector2(X+3,3),new Vector2(X+6.6f,3)};east.seeds=new[]{new Vector2(X+9.5f,3)};field.plates=new[]{west,east};
            var overlap=new Vector3(X+6.5f,0,3);Assert.AreSame(east,field.At(overlap),"fixture: the overlap is in the east plate's footprint");Assert.AreSame(west,field.Drawn(overlap),"fixture: the overlap is drawn as the west plate");
            void Clocks(float westInto,float eastInto){west.offset=0;east.offset=0;field.Clock=0;west.offset=westInto;east.offset=eastInto;}
            Clocks(6.5f,1);Assert.IsFalse(field.BurnsAt(overlap),"open west, dark east: the overlap burns though its footprint is dark crust");Assert.IsTrue(field.BurnsAt(new Vector3(X+3,0,3)),"the open west plate's middle doesn't burn");
            Clocks(1,6.5f);Assert.IsFalse(field.BurnsAt(overlap),"open east, dark west: the overlap burns though it is drawn as dark crust");Assert.IsTrue(field.BurnsAt(new Vector3(X+10,0,3)),"the open east plate's middle doesn't burn");
            Clocks(6.5f,6.5f);Assert.IsTrue(field.BurnsAt(overlap),"both open: the overlap doesn't burn");
            Clocks(1,1);Assert.Greater(field.SafeFor(overlap),2,"dark both ways: the overlap isn't safe");Clocks(4.5f,1);Assert.AreEqual(0,field.SafeFor(overlap),.01f,"the drawn plate's seam is lit and the overlap reads safe");
        }

        [UnityTest] public IEnumerator ABlisterRisesOnlyOverAHideSomethingHasGoneUnder()
        {
            yield return Arena();var field=BareField(out var plate);yield return null;field.Held=true;field.Clock=CrustField.ClockFor(plate,1);
            var heave=field.heaves[0];
            var spot=new GameObject("Fixture hide",typeof(AmbushSpot)).GetComponent<AmbushSpot>();spot.transform.position=new Vector3(X+3,0,3);yield return Wait(.2f);
            Assert.IsFalse(CrustField.Heaved(heave),"the crust is heaved over an empty hide");
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Reedstalker"),new Vector3(X+20,0,20));enemy.Passive=true;yield return null;
            spot.Take(enemy,3.6f);yield return Wait(.2f);Assert.IsFalse(CrustField.Heaved(heave),"the crust heaves over a hide whose occupant hasn't gone under");
            spot.Show(true);yield return Wait(.2f);Assert.IsTrue(CrustField.Heaved(heave),"something has gone under the crust and nothing shows it");
            spot.Show(false);spot.Leave(enemy);yield return Wait(.2f);Assert.IsFalse(CrustField.Heaved(heave),"the blister stays up once the hide is empty");
        }

        [UnityTest] public IEnumerator NothingCyclesWhileAConversationHoldsTheParty()
        {
            yield return Arena();var field=BareField(out var plate);yield return Wait(.5f);
            float before=field.Clock;Assert.Greater(before,.2f,"fixture: the clock doesn't run");
            GameServices.Current.Input.Blocked=true;float held=field.Clock;yield return Wait(1);
            Assert.AreEqual(held,field.Clock,.001f,"the crust went on cycling while a conversation held the party");
            GameServices.Current.Input.Blocked=false;yield return Wait(.5f);Assert.Greater(field.Clock,held+.3f,"the crust doesn't start again after the conversation");
        }

        // ------------------------------------------------------------------ the zone
        [UnityTest] public IEnumerator TheValleyIsWalkedFromTheLandingToTheGateTheCampAndThePockets()
        {
            yield return Load(Zone);var start=PartyController.Current.Active.transform.position;
            foreach(var (what,at) in new[]{("the first tongue's far bank",new Vector2(4,54)),("the causeway",new Vector2(0,84)),("the garrison's fire",new Vector2(6.5f,96.8f)),("the cinder hollow",new Vector2(26,108)),
                ("the hollow's cache",new Vector2(30.6f,113.5f)),("island A",IslandA),("island B",IslandB),("the glass island",GlassIsland),("the north shelf",new Vector2(2,160)),("the battery terrace",new Vector2(-8,196)),
                ("the sled",new Vector2(-19.6f,196.5f)),("the junction",new Vector2(4,210)),("the camp",CampSpawn),("Tovra",TovraAt+new Vector2(0,-1.6f)),("Brekk's stall",BrekkStall+new Vector2(-1,-1.6f)),
                ("the pike-head",PikeHead),("the town road",HarrowgateSpawn),("the Foundry spawn",FoundrySpawn)})
                Assert.IsTrue(Reach(start,at),"from the landing nobody can walk to "+what+" at "+at);
            var spawns=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Select(s=>s.id).ToArray();
            foreach(var id in new[]{"Arrival","Camp","Foundry","Harrowgate"})CollectionAssert.Contains(spawns,id,"the Flows have no spawn "+id+" (docs/maps/ScoriaFlows.md, Contracts)");
            var ways=Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None);
            var foundry=ways.Single(w=>w.scene=="BridleFoundry");Assert.AreEqual(("Arrival","met.Tovra"),(foundry.spawn,foundry.requiredFlag),"the Foundry gate isn't the contract's");
            StringAssert.Contains("Tovra Kesh",foundry.lockedPrompt,"the Foundry gate's refusal doesn't say who opens it");
            var town=ways.Single(w=>w.scene=="Harrowgate");Assert.AreEqual(("Arrival","vask.roadSaid"),(town.spawn,town.requiredFlag),"the town road isn't the contract's");
            var launch=Object.FindObjectsByType<DockingPad>(FindObjectsSortMode.None).Single(d=>d.scene=="PicketOfTeeth");Assert.AreEqual("Scoria",launch.spawn,"the launch doesn't arrive where the Picket expects");
            var bays=Object.FindObjectsByType<RepairBay>(FindObjectsSortMode.None);
            Assert.IsTrue(bays.Any(b=>b.free&&b.saveSpawn=="Arrival")&&bays.Any(b=>b.free&&b.saveSpawn=="Camp"),"there is no free cradle at the landing and at the camp");
            Assert.IsNotNull(Object.FindFirstObjectByType<Bench>(),"there is no bench before the Foundry");
            var pocket=Object.FindFirstObjectByType<SafePocket>();Assert.IsNotNull(pocket,"the Unsworn camp isn't a safe pocket");
            Assert.IsTrue(pocket.GetComponent<BoxCollider>().bounds.Contains(Ground(TovraAt)+Vector3.up),"Tovra stands outside the camp's safe pocket");
        }

        [UnityTest] public IEnumerator EveryFlowIsCrossedGoingNorthOnTheClockTheBriefGivesIt()
        {
            yield return Load(Zone);var crust=CrustField.Current;Assert.IsNotNull(crust,"the Flows have no crust");
            foreach(var field in new[]{"F1","F2","F3E","F3W","F4","F5","F6","F7","F9"})Assert.IsNotEmpty(Field(crust,field),"the flow "+field+" has no plates");
            foreach(var plate in crust.plates){Assert.IsNotNull(plate.melt,plate.name+" has no melt");Assert.IsNotNull(plate.slabs,plate.name+" has no crust");Assert.AreEqual(Bed,plate.floor,.01f);}
            // The tongues break in a wave from the east: an eastern plate opens before its western neighbour, so the bright band walks west past whoever waits on the bank.
            foreach(var field in new[]{"F2","F9"})
            {
                var row=Field(crust,field).OrderByDescending(p=>Centre(p).x).ToArray();
                for(int i=1;i<row.Length;i++)Assert.Greater(row[i-1].offset,row[i].offset,field+": "+row[i].name+" opens before the plate east of it");
            }
            // The delta's tide comes from the north-east.
            var delta=Field(crust,"F6").Where(p=>Vector2.Distance(Centre(p),GlassIsland)>10).ToArray();
            var northEast=delta.OrderByDescending(p=>Centre(p).x+Centre(p).y).First();var southWest=delta.OrderBy(p=>Centre(p).x+Centre(p).y).First();
            Assert.Greater(northEast.offset,southWest.offset+3,"the delta's tide doesn't come from the north-east");
            // The islands and every place a body is put to stand are ground that never cycles.
            foreach(var (what,at) in new[]{("island A",IslandA),("island B",IslandB),("the glass island",GlassIsland),("the arrival",Arrival),("the causeway",new Vector2(0,84)),("the camp",CampSpawn),("the Foundry spawn",FoundrySpawn)})
                Assert.IsNull(crust.At(Ground(at)),what+" cycles: it is on a plate");
            Assert.IsTrue(Field(crust,"F3E").Concat(Field(crust,"F3W")).All(p=>Mathf.Abs(Centre(p).x)>2.5f),"a plate lies on the garrison's causeway");
            // The lesson before the test: F1 lies beside the walked line, nearer the landing than F2, the way to F2 never crosses it, Brekk's talk comes first, and no fight wakes there.
            var lesson=Field(crust,"F1").ToArray();var first=Field(crust,"F2").ToArray();
            Assert.Less(lesson.Max(p=>Centre(p).y),first.Min(p=>Centre(p).y),"the lesson tongue isn't before the first crossing");
            var path=new NavMeshPath();NavMesh.SamplePosition(Ground(Arrival),out var from,2,NavMesh.AllAreas);NavMesh.SamplePosition(Ground(new Vector2(4,40)),out var to,2,NavMesh.AllAreas);
            Assert.IsTrue(NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"fixture: the walk to the first crossing");
            for(int i=1;i<path.corners.Length;i++)for(float t=0;t<=1;t+=.05f){var p=Vector3.Lerp(path.corners[i-1],path.corners[i],t);Assert.IsNull(crust.At(p),"the way from the landing to the first crossing walks over "+crust.At(p)?.name);}
            Assert.Less(lesson.Min(p=>Vector2.Distance(Centre(p),new Vector2(4,30))),10,"the lesson tongue is too far from the walked line to be watched");
            var crust1=Object.FindObjectsByType<DiscoveryPoint>(FindObjectsSortMode.None).Single(d=>d.dialogueNode=="BrekkCrust");Assert.Less(crust1.transform.position.z,lesson.Min(p=>Centre(p).y),"Brekk's talk comes after the lesson tongue");
            foreach(var fight in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))
            {var box=fight.GetComponent<BoxCollider>().bounds;foreach(var p in lesson.Concat(first)){var c=Centre(p);Assert.IsFalse(box.Contains(new Vector3(c.x,box.center.y,c.y)),fight.encounterId+" wakes on the lesson or the first crossing ("+p.name+")");}}
        }

        [UnityTest] public IEnumerator TheTalksDoorsAndKettleStonesAreWhereTheScriptWantsThem()
        {
            yield return Load(Zone);
            var points=Object.FindObjectsByType<DiscoveryPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach(var (node,flag,speaker) in new[]{("BrekkCrust","vask.crustRead","Brekk"),("BrekkCrossing","vask.garrisonSeen","Brekk")})
            {var point=points.Single(p=>p.dialogueNode==node);Assert.AreEqual((flag,speaker),(point.flag,point.dialogueSpeaker),node+" isn't the manifest's");Assert.IsTrue(point.startsOnCrossing,node+" isn't a crossing");}
            Assert.Less(points.Single(p=>p.dialogueNode=="BrekkCrust").transform.position.z,points.Single(p=>p.dialogueNode=="BrekkCrossing").transform.position.z);
            var stones=points.Where(p=>p.report=="KettleStone").ToArray();Assert.AreEqual(3,stones.Length,"Quiet Hands wants three kettle stones");
            foreach(var stone in stones){Assert.AreEqual(("QuietHands",0,"message"),(stone.requiredQuest,stone.requiredStep,stone.situation),stone.name+" isn't offered at Quiet Hands' first step");StringAssert.StartsWith("Leave it under",stone.prompt);}
            Assert.AreEqual(3,stones.Select(s=>s.flag).Distinct().Count(),"two kettle stones set the same flag");
            var pike=points.Single(p=>p.dialogueNode=="BrekkPikeHead");Assert.AreEqual(("vask.unswornMet","brekk.pikeDressed","Look at the pike-head"),(pike.requiredFlag,pike.flag,pike.prompt));
            var people=Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var tovra=people.Single(n=>n.speaker=="Tovra");Assert.AreEqual(("TovraFirst","TovraRepeat"),(tovra.firstNode,tovra.repeatNode));
            var brekk=people.Single(n=>n.speaker=="Brekk");Assert.AreEqual(("BrekkCampStall","BrekkCampRepeat","met.Brekk"),(brekk.finalNode,brekk.finalRepeatNode,brekk.finalFlag),"Brekk's stall talks aren't first and repeat");
            Assert.IsFalse(brekk.gameObject.activeInHierarchy,"Brekk is at the stall before the causeway is taken (he is walking ahead of the party)");
            var guide=Object.FindFirstObjectByType<Guide>();Assert.IsNotNull(guide,"nobody leads the party across the first crust");Assert.AreEqual("clear.Scoria_Crossing",guide.goneFlag);
            Assert.AreEqual(new[]{"","vask.crustRead","vask.tongueCrossed"},guide.legs.Select(l=>l.flag).ToArray(),"Brekk's legs aren't the landing, the first tongue and the causeway");
            // He waits at the first tongue's bank while the crust on his way is lit or open, and goes once it will hold.
            var wait=guide.GetComponent<CrustWait>();Assert.IsNotNull(wait,"Brekk walks over the crust whatever it is doing");
            Set("vask.crustRead","vask.tongueCrossed");var crust=CrustField.Current;crust.Held=true;
            var plate=Field(crust,"F2").OrderBy(p=>Vector2.Distance(Centre(p),new Vector2(7,47))).First();
            crust.Clock=CrustField.ClockFor(plate,plate.dark+plate.seam+.5f);
            float until=Time.unscaledTime+12;while(guide.Stage<2&&Time.unscaledTime<until)yield return null;
            Assert.AreEqual(2,guide.Stage,"fixture: Brekk never set off for the causeway");yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(wait.Waiting,"Brekk walks out onto open melt instead of waiting at the bank");Assert.IsNull(crust.At(guide.transform.position),"Brekk is standing on the crust");
            crust.Clock=CrustField.ClockFor(plate,.6f);until=Time.unscaledTime+12;while(guide.transform.position.z<56&&Time.unscaledTime<until)yield return null;
            Assert.Greater(guide.transform.position.z,56,"Brekk never crosses once the crust holds");
            Set("met.Brekk");
            Set("clear.Scoria_Crossing");yield return Load(Zone,"Camp");
            Assert.IsTrue(Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Any(n=>n.speaker=="Brekk"),"Brekk isn't at the stall once the causeway is taken");
            Assert.IsNull(Object.FindFirstObjectByType<Guide>(),"Brekk still waits at the causeway after its fight");
        }

        [UnityTest] public IEnumerator EveryFightIsWhereTheBriefPutsItAndEveryCrawlerHidesUnderCrust()
        {
            yield return Load(Zone);var crust=CrustField.Current;var fights=Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).ToDictionary(e=>e.encounterId);
            string Id(string id)=>GameCatalog.Find<EnemyDef>(id)!=null?id:null;
            foreach(var (id,wanted) in new[]{("Scoria_Crossing",new[]{"MusterPikeguard","ReachScav"}),("Scoria_Hollow",new[]{"AshCrawler"}),("Scoria_Delta",new[]{"AshCrawler"}),
                ("Scoria_Battery",new[]{"MusterMortar","ReachScav"}),("Scoria_Gate",new[]{"MusterPikeguard","ReachSergeant"})})
            {
                Assert.IsTrue(fights.ContainsKey(id),"there is no fight "+id);
                var kinds=fights[id].spawners.Select(s=>s.definition!=null?s.definition.id:"").Distinct().ToArray();
                foreach(var enemy in wanted)if(Id(enemy)!=null)CollectionAssert.Contains(kinds,enemy,id+" doesn't field "+enemy);
            }
            Assert.AreEqual(2,fights["Scoria_Crossing"].spawners.Where(s=>s.definition!=null&&s.definition.id==(Id("MusterPikeguard")??"ThawedGuard")).Sum(s=>s.count),"the causeway isn't held by a pikeguard pair");
            // A crawler's hide is a plate of crust: something can be under it, and its blister can show it.
            var hides=Object.FindObjectsByType<AmbushSpot>(FindObjectsSortMode.None);Assert.AreEqual(5,hides.Length,"the hollow and the delta want five hides");
            foreach(var hide in hides){Assert.IsNotNull(crust.At(hide.transform.position),hide.name+" isn't on a plate");Assert.AreEqual(3.6f,hide.radius,.01f,hide.name+"'s ring isn't the crawler's");
                Assert.IsTrue(crust.heaves.Any(h=>Vector3.Distance(h.position,hide.transform.position)<1),hide.name+" has no blister to show what's under it");}
            foreach(var id in new[]{"vask.scoria.hollow","vask.scoria.glass","vask.scoria.sled"})Assert.IsTrue(Object.FindObjectsByType<LootCache>(FindObjectsSortMode.None).Any(c=>c.cacheId==id),"the cache "+id+" is missing");
            Assert.GreaterOrEqual(Object.FindObjectsByType<MineNode>(FindObjectsSortMode.None).Where(m=>m.material=="ScoriaGlass").Sum(m=>m.amount),8,"the Flows hold too little Scoria Glass");
        }

        // ------------------------------------------------------------------ a marking on open melt
        const int ShotWidth=960,ShotHeight=540;
        static Color32[] Shot(Camera camera)
        {
            var image=new Texture2D(ShotWidth,ShotHeight,TextureFormat.RGB24,false);var rt=new RenderTexture(ShotWidth,ShotHeight,24,RenderTextureFormat.ARGB32);
            var active=RenderTexture.active;var target=camera.targetTexture;
            try{Assert.IsTrue(rt.Create());camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,ShotWidth,ShotHeight),0,0);image.Apply();return image.GetPixels32();}
            finally{camera.targetTexture=target;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);}
        }
        static float ChangeAt(Color32[] a,Color32[] b,Camera camera,Vector3 world)
        {
            var view=camera.WorldToViewportPoint(world);if(view.z<=0||view.x<.03f||view.x>.97f||view.y<.03f||view.y>.97f)return -1;
            int i=(int)(view.y*ShotHeight)*ShotWidth+(int)(view.x*ShotWidth);return (Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b))/(3*255f);
        }
        /// <summary>How much a marking changes the frame where its rim lies: at each of 32 bearings the most it changes any pixel across a band at its edge (one ring of pixels missed
        /// the rim's few pixels as often as not), the median of those, against the same frame with the marking not drawn.</summary>
        static float Rim(Camera camera,Telegraph marking,Vector3 centre,float radius)
        {
            marking.Hide(true);var without=Shot(camera);marking.Hide(false);var with=Shot(camera);var changes=new List<float>();
            for(int k=0;k<32;k++)
            {
                float most=-1;var way=Quaternion.Euler(0,k*11.25f,0)*Vector3.forward;
                for(float r=radius-.45f;r<=radius+.05f;r+=.05f)most=Mathf.Max(most,ChangeAt(without,with,camera,centre+way*r));
                if(most>=0)changes.Add(most);
            }
            Assert.Greater(changes.Count,20,"fixture: the marking is in the frame");changes.Sort();return changes[changes.Count/2];
        }
        /// <summary>The brief's hardest thing: a mortar's circle laid on open melt has to read as a marking does on the zone's ordinary ground, the basalt shelf beside the flow. Measured
        /// as what the rim changes in the frame. (Dark crust is logged as well: near-black, it is the best ground any marking gets, so it is no fair bar.) The check is shown to fail
        /// first on the fault it is for: the same plate's melt lit in a marking's own orange, as bright as a marking.</summary>
        [UnityTest] public IEnumerator AMarkingOnBrightFlowStillReadsAsAMarking()
        {
            Assert.AreNotEqual(GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType,"this reads real pixels: run it with the -Graphics runner");
            yield return Load(Zone);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var volume in Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None))volume.enabled=false;
            var crust=CrustField.Current;crust.Held=true;var (lead,partner)=Party();Camera.main.aspect=16f/9;
            var plate=Field(crust,"F2").OrderBy(p=>Mathf.Abs(Centre(p).x-4)).First();var c=Centre(plate);var centre=Ground(c);
            Put(lead,Ground(new Vector2(c.x,c.y-6.5f)));Put(partner,Ground(new Vector2(c.x-5,c.y-7)));lead.Health.InvulnerableUntil=GameTime.Now+900;partner.Health.InvulnerableUntil=GameTime.Now+900;
            crust.enabled=false;yield return Wait(1.2f);
            float Measure(float into,Color colour)
            {
                crust.Clock=CrustField.ClockFor(plate,into);crust.Pose(crust.Clock,false);
                var marking=Telegraph.Circle(centre,2.4f,99,colour);marking.Steady=true;
                try{return Rim(Camera.main,marking,centre,2.4f);}finally{marking.Clear();}
            }
            float onCrust=Measure(1,Telegraph.Threat),onMelt=Measure(plate.dark+plate.seam+1.5f,Telegraph.Threat),heavy=Measure(plate.dark+plate.seam+1.5f,Telegraph.Heavy);
            var shelf=Ground(new Vector2(c.x+5,c.y-6));Assert.IsNull(crust.At(shelf),"fixture: the shelf beside the flow is ground that doesn't cycle");
            var plain=Telegraph.Circle(shelf,2.4f,99,Telegraph.Threat);plain.Steady=true;float onShelf;try{onShelf=Rim(Camera.main,plain,shelf,2.4f);}finally{plain.Clear();}
            Debug.Log($"SCORIA_MARKING shelf={onShelf:0.000} crust={onCrust:0.000} melt={onMelt:0.000} heavy={heavy:0.000}");
            // The fault, built into the fixture: the melt lit in the marking's own orange.
            // A plain sheet of it, the whole plate lit as bright as a marking: its own map (rafts and cracks) set aside for a flat one.
            var open=crust.open;crust.open=new Color(Telegraph.Threat.r*1.6f,Telegraph.Threat.g*1.6f,Telegraph.Threat.b*1.6f);
            var sheet=new MaterialPropertyBlock();plate.melt.GetPropertyBlock(sheet);sheet.SetTexture("_EmissionMap",Texture2D.whiteTexture);sheet.SetTexture("_BaseMap",Texture2D.whiteTexture);plate.melt.SetPropertyBlock(sheet);
            float orange=Measure(plate.dark+plate.seam+1.5f,Telegraph.Threat);crust.open=open;plate.melt.SetPropertyBlock(null);
            Debug.Log($"SCORIA_MARKING orange={orange:0.000}");
            Assert.Greater(onShelf,.15f,"fixture: a marking on the shelf doesn't read either");Assert.Greater(onCrust,.15f,"a marking on dark crust doesn't read");
            Assert.Less(orange,onMelt*.6f,"fixture: the check can't tell melt lit in a marking's own orange from the zone's");
            Assert.Less(orange,onShelf*.75f,"fixture: the check can't see melt lit in a marking's own orange");
            Assert.Greater(onMelt,onShelf*.75f,$"a mortar's circle on open melt changes its rim by {onMelt:0.000}, well under what it does on the shelf beside it ({onShelf:0.000}): the melt's glow wins over the marking");
            Assert.Greater(heavy,onShelf*.75f,$"a heavy marking on open melt changes its rim by {heavy:0.000}, well under what it does on the shelf ({onShelf:0.000}): the melt's glow wins over it");
        }

        [UnityTest] public IEnumerator NoWallOrTallPieceStandsBetweenTheCameraAndAFight()
        {
            yield return Load(Zone);
            var toCamera=-(Quaternion.Euler(SightlineAudit.Pitch,SightlineAudit.Yaw,0)*Vector3.forward);var hidden=new List<string>();int samples=0;
            foreach(var fight in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))
            {
                var box=fight.GetComponent<BoxCollider>().bounds;
                for(float x=box.min.x+1;x<box.max.x;x+=2)for(float z=box.min.z+1;z<box.max.z;z+=2)
                {
                    if(!Physics.Raycast(new Vector3(x,box.max.y+8,z),Vector3.down,out var floor,40,~0,QueryTriggerInteraction.Ignore)||!NavMesh.SamplePosition(floor.point,out var on,.4f,NavMesh.AllAreas))continue;
                    if(Mathf.Abs(on.position.y-(box.center.y-1.5f))>1.6f)continue;
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

        [UnityTest] public IEnumerator AnAshSquallClosesOnBrekksWarningAndLifts()
        {
            yield return Load(Zone);var weather=Whiteout.Current;Assert.IsNotNull(weather,"the Flows have no ash squalls");Assert.AreEqual("squall",weather.bark);
            Assert.AreEqual(Whiteout.Phase.Calm,weather.Now,"a squall is down before anyone has been warned");
            Set("vask.squallMet");float until=Time.unscaledTime+6;while(weather.Now==Whiteout.Phase.Calm&&Time.unscaledTime<until)yield return null;
            Assert.AreNotEqual(Whiteout.Phase.Calm,weather.Now,"Brekk's warning brings no squall");
            until=Time.unscaledTime+20;while(!log.Any(l=>l.StartsWith("WHITEOUT_LIFTED"))&&Time.unscaledTime<until)yield return null;
            Assert.IsTrue(log.Any(l=>l.StartsWith("WHITEOUT_CLOSED")),"the squall never closed");Assert.IsTrue(log.Any(l=>l.StartsWith("WHITEOUT_LIFTED")),"the squall never lifted: it should be a few seconds");
            Assert.IsTrue(Flag("vask.squallLifted"));
        }
    }
}
