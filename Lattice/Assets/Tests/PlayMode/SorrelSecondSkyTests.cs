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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Sorrel's second sky as a player meets it, in the built scene (chapter 10's first zone; rule: fly the ridge tops). Places are the map brief's (docs/maps/SorrelSecondSky.md),
    /// which are the builder's own (LastMooringBuilder.Sorrel): the ways in and out and when each is drawn, the air a hull flies in and the chains that close the way on until Mother of Doors
    /// goes, Dace's crossing, the four shelves as berths with their works walkable, nothing above the plane on the camera's side of her fight, the young following her out, the quiet after
    /// Dace's line, and the young's road for the last survey.</summary>
    public sealed class SorrelSecondSkyTests
    {
        readonly List<string> log=new();
        static readonly string[] Heeded={"LEAVES_ALONG","QUIET_AFTER","BERTH_"};
        void Logged(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();Time.timeScale=1;SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter10");log.Clear();Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;Application.logMessageReceived-=Logged;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the zone's places (LastMooringBuilder.Sorrel; docs/maps/SorrelSecondSky.md)
        const string Sorrel="SorrelSecondSky";const float Plane=1,Half=15,ValleyHalf=40,Idra=2.17f,ChainZ=352,ShelfY=-2f;
        /// <summary>The valley's half width at a z: 40 to z 190, narrowing to the apertures' 15 by z 232.</summary>
        static float HalfAt(float z)=>Mathf.Lerp(ValleyHalf,Half,Mathf.InverseLerp(190,232,z));
        static readonly Vector2 C=new(0,330);
        static readonly Vector3 Arrival=new(0,1,0),HomeWarp=new(-10,1,-4),WarpOn=new(158,1,322),RoadFarEnd=new(44,1,514);
        static readonly float[] Bearings={240,205,155,120};
        static Vector3 Ring(float bearing,float r,float y=Plane){float a=bearing*Mathf.Deg2Rad;return new Vector3(C.x+Mathf.Sin(a)*r,y,C.y+Mathf.Cos(a)*r);}
        static readonly Vector3[] Cover={new(0,1,298),new(-21.6f,1,306.4f),new(21.6f,1,306.4f)};

        // ------------------------------------------------------------------ fixtures
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Zone(string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(Sorrel,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,Sorrel+" never finished loading");
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            yield return null;yield return null;
        }
        /// <summary>Whether a hull of Idra's reach, at the plane, meets anything solid here (her sphere's middle is .8 over the ship's root).</summary>
        static bool Solid(Vector3 at)=>Physics.CheckSphere(new Vector3(at.x,Plane+.8f,at.z),Idra,~0,QueryTriggerInteraction.Ignore);
        static bool NearCover(Vector3 p)=>Cover.Any(c=>Flat(c,p)<3.5f+Idra+.5f)||Flat(p,HomeWarp)<Idra+2||Flat(p,WarpOn)<Idra+2||PartyController.Current.members.Any(m=>m!=null&&Flat(m.transform.position,p)<Idra*2+.5f);
        /// <summary>The points a ship flies through: the canyon's middle and both sides, each aperture's middle, the fight's ring round her, the lane east, and the young's road leg by leg.</summary>
        static IEnumerable<(string where,Vector3 at)> Lanes(bool freed)
        {
            for(float z=-8;z<=236;z+=4){float h=HalfAt(z)-1-Idra-1.2f;foreach(float x in new[]{-h,0,h})yield return ("the valley",new Vector3(x,Plane,z));}
            for(float z=242;z<=262;z+=2)foreach(float x in new[]{-9.5f,9.5f})yield return ("an aperture",new Vector3(x,Plane,z));
            for(float b=80;b<=280;b+=5)foreach(float r in new[]{24f,30,35})yield return ("her ring",Ring(b,r));
            if(!freed)yield break;
            for(float x=78;x<=160;x+=4)yield return ("the lane east",new Vector3(x,Plane,322));
            for(float z=410;z<=488;z+=3)yield return ("the road's first leg",new Vector3(0,Plane,z));
            for(float z=424;z<=488;z+=3)yield return ("the road's second leg",new Vector3(22,Plane,z));
            for(float z=424;z<=518;z+=3)yield return ("the road's third leg",new Vector3(44,Plane,z));
            for(float x=0;x<=44;x+=2)yield return ("the road's turns",new Vector3(x,Plane,x<=22?487:423));
        }

        // ------------------------------------------------------------------ the ways in and out
        [UnityTest] public IEnumerator SorrelOpensInFlightWithTheWayHomeInFrameAndNoWayOnUntilSheIsFreed()
        {
            yield return Zone();
            Assert.AreEqual(ZoneKind.SpaceCombat,ZoneController.Current.definition.kind,"Sorrel's second sky is a flight zone");
            var home=All<WarpBeacon>().FirstOrDefault(w=>w.scene=="ManyHandsHull");
            Assert.IsNotNull(home,"no way home to Many Hands' hull");Assert.AreEqual("Sorrel",home.spawn,"the way home lands at the hull's Sorrel spawn");
            Assert.IsTrue(string.IsNullOrEmpty(home.requiredFlag),"the way home is always open");
            Assert.Less(Flat(home.transform.position,Arrival),14,"the way home stands in the arrival's frame");Assert.Greater(Flat(home.transform.position,Arrival),6.5f,"the arrival isn't inside the way home's reach");
            var on=All<WarpBeacon>().FirstOrDefault(w=>w.scene=="LastMooring");
            Assert.IsNotNull(on,"no way on to Last Mooring");Assert.IsFalse(on.gameObject.activeInHierarchy,"the way on is drawn before Mother of Doors is freed (D283: a lane that doesn't exist isn't drawn)");
            foreach(var id in new[]{"Arrival","Clamps","Road","Shelf1","Shelf2","Shelf3","Shelf4"})
                Assert.IsTrue(All<SpawnPoint>().Any(s=>s.id==id),"Sorrel has no spawn "+id);
            Assert.IsTrue(Solid(new Vector3(0,Plane,ChainZ)),"the chain across the crater doesn't close the way north before she goes");
            Assert.IsTrue(Solid(new Vector3(71,Plane,322)),"the chain across the east notch doesn't close the way east before she goes");
            Flag("end.motherFreed");yield return Zone("Clamps");
            on=All<WarpBeacon>().First(w=>w.scene=="LastMooring");
            Assert.IsTrue(on.gameObject.activeInHierarchy,"the way on isn't drawn once she is freed");Assert.AreEqual("Arrival",on.spawn);
            Assert.Less(Flat(on.transform.position,WarpOn),1,"the way on stands at the lane east's far end");
            Assert.IsFalse(Solid(new Vector3(0,Plane,ChainZ)),"the chain line still stands once she is freed");
            Assert.IsFalse(Solid(new Vector3(71,Plane,322)),"the east notch's chain still stands once she is freed");
        }

        [UnityTest] public IEnumerator EveryLaneIsOpenAirForIdrasHullAndItsEdgesAreSolid()
        {
            // The check first, on a box stood in the canyon: it must be found.
            yield return Zone();
            var probe=GameObject.CreatePrimitive(PrimitiveType.Cube);probe.transform.position=new Vector3(0,Plane,60);probe.transform.localScale=new Vector3(2,4,2);Physics.SyncTransforms();
            Assert.IsTrue(Solid(new Vector3(0,Plane,60)),"fixture: a box in the canyon isn't found by the check");Object.Destroy(probe);yield return null;Physics.SyncTransforms();
            foreach(bool freed in new[]{false,true})
            {
                if(freed){Flag("end.motherFreed");yield return Zone("Clamps");}
                var blocked=Lanes(freed).Where(p=>!NearCover(p.at)&&Solid(p.at)).Select(p=>p.where+" at "+p.at).ToList();
                CollectionAssert.IsEmpty(blocked.Take(12),"a hull meets something solid in open air"+(freed?" (freed)":""));
            }
            // The edges a ship sees are where it stops: past each canyon wall, behind the arrival, beyond the crater's rim, past the road's walls.
            foreach(var (where,at) in new[]{("west of the valley",new Vector3(-ValleyHalf-2,Plane,120)),("east of the valley",new Vector3(ValleyHalf+2,Plane,120)),("behind the arrival",new Vector3(0,Plane,-16)),
                ("at the pillar between the apertures",new Vector3(4,Plane,248)),("beyond the crater's west rim",Ring(270,76)),("beyond the crater's south-east rim",Ring(140,76)),("at the road's first leg's east wall",new Vector3(6,Plane,450))})
                Assert.IsTrue(Physics.CheckSphere(new Vector3(at.x,Plane+.8f,at.z),1.5f,~0,QueryTriggerInteraction.Ignore),"nothing stops a ship "+where);
        }

        // ------------------------------------------------------------------ what is said
        [UnityTest] public IEnumerator DaceTalksThemOverHisRidgeWhereTheCraterComesIntoViewAndHisLastLineIsFollowedByQuiet()
        {
            yield return Zone();
            var crossing=All<DiscoveryPoint>().FirstOrDefault(p=>p.dialogueNode=="SurvivorSecondSky");
            Assert.IsNotNull(crossing,"no crossing for SurvivorSecondSky");Assert.IsTrue(crossing.startsOnCrossing);
            Assert.AreEqual("end.sorrelSeen",crossing.flag);Assert.AreEqual("Survivor",crossing.dialogueSpeaker);
            var box=crossing.GetComponent<BoxCollider>().bounds;
            Assert.LessOrEqual(box.min.x,-Half+.5f,"the crossing doesn't span the west aperture");Assert.GreaterOrEqual(box.max.x,Half-.5f,"the crossing doesn't span the east aperture");
            Assert.IsTrue(box.min.y<=Plane&&box.max.y>=Plane,"the crossing isn't at the plane");
            Assert.Greater(box.center.z,240,"Dace speaks before the crater comes into view");
            var goes=All<AfterTalk>().FirstOrDefault(t=>t.node=="SurvivorMotherGoes");
            Assert.IsNotNull(goes,"nothing plays SurvivorMotherGoes");Assert.AreEqual("bossdown.MotherOfDoors",goes.flag);Assert.AreEqual("end.motherFreed",goes.heardFlag);Assert.AreEqual("Survivor",goes.speaker);
            // The quiet after it: hushed for eight seconds from his line's flag, and then not. At four times speed.
            Assert.IsFalse(BarkService.Hushed,"fixture: something hushes the barks before his line");
            Time.timeScale=4;Flag("end.motherFreed");yield return null;yield return null;
            Assert.IsTrue(BarkService.Hushed,"barks aren't hushed as his line closes");
            float deadline=Time.unscaledTime+1.6f;while(Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(BarkService.Hushed,"barks were let go before eight seconds");
            deadline=Time.unscaledTime+2;while(BarkService.Hushed&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(BarkService.Hushed,"barks are still hushed after eight seconds");
        }

        [UnityTest] public IEnumerator TheYoungFollowHerUpHerRoadAndAZoneOpenedAfterHasThemGone()
        {
            yield return Zone("Clamps");
            var young=All<LeavesAlong>().FirstOrDefault();Assert.IsNotNull(young,"nothing of the young leaves after her");
            Assert.AreEqual("bossdown.MotherOfDoors",young.flag);Assert.IsTrue(young.gameObject.activeInHierarchy,"the young aren't in the nursery before she goes");
            var home=young.transform.position;Assert.Less(Flat(home,new Vector3(C.x,0,C.y)),3,"the young aren't in the nursery under her");
            Time.timeScale=4;Flag("bossdown.MotherOfDoors");
            float deadline=Time.unscaledTime+.6f;while(Time.unscaledTime<deadline)yield return null;
            Assert.Less(Vector3.Distance(young.transform.position,home),.5f,"the young go before she has gone three seconds");
            deadline=Time.unscaledTime+1.5f;while(Time.unscaledTime<deadline)yield return null;
            Assert.Greater(young.transform.position.z,home.z+4,"the young aren't going north up her road");
            deadline=Time.unscaledTime+3;while(young!=null&&young.gameObject.activeInHierarchy&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(young.gameObject.activeInHierarchy,"the young are still there twelve seconds after she goes");
            Time.timeScale=1;yield return Zone("Clamps");
            Assert.IsFalse(All<LeavesAlong>().First().gameObject.activeInHierarchy,"a zone opened after she has gone still has the young in it");
        }

        // ------------------------------------------------------------------ her fight's room (the map brief's "Boss arena contract")
        [UnityTest] public IEnumerator EachShelfIsABerthTurnedToHerUnderThePlaneWithItsWorksWalkedTo()
        {
            yield return Zone("Clamps");
            for(int i=0;i<4;i++)
            {
                int n=i+1;var berth=Berth.Find("Clamp"+n);Assert.IsNotNull(berth,"shelf "+n+" has no berth");
                var seat=Ring(Bearings[i],23,ShelfY);var middle=Ring(Bearings[i],27,ShelfY);
                Assert.Less(Flat(berth.transform.position,middle),.5f,"shelf "+n+"'s berth isn't on its shelf");
                Assert.Less(berth.transform.position.y+berth.boxSize.y*.5f,Plane-.4f,"shelf "+n+"'s box reaches the plane: a ship over it would be in it");
                Assert.Less(Flat(berth.pad.transform.position,middle),.5f,"shelf "+n+"'s pad isn't over its middle");
                Assert.Greater(Flat(berth.foot.position,new Vector3(C.x,0,C.y)),Flat(seat,new Vector3(C.x,0,C.y))+4,"shelf "+n+"'s foot spawn isn't on the edge away from her");
                Assert.IsTrue(NavMesh.SamplePosition(berth.foot.position,out var from,1.5f,NavMesh.AllAreas),"shelf "+n+"'s foot spawn has no floor in the bake");
                Assert.IsTrue(NavMesh.SamplePosition(seat,out var to,1.5f,NavMesh.AllAreas),"shelf "+n+"'s seat has no floor in the bake");
                var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"shelf "+n+"'s works can't be walked to");
                var guns=All<EncounterVolume>().FirstOrDefault(e=>e.encounterId=="Sorrel_Clamp"+n);Assert.IsNotNull(guns,"clamp "+n+" has no guns");
                Assert.AreEqual(n==1?"end.sorrelSeen":"end.clamp"+(n-1),guns.requiredFlag,"clamp "+n+"'s guns don't wait for the clamp before");
                Assert.IsTrue(guns.GetComponent<BoxCollider>().bounds.Contains(berth.pad.transform.position),"clamp "+n+"'s pad isn't inside its guns' box");
                var arrow=All<DiscoveryPoint>().FirstOrDefault(p=>p.report=="Clamp"&&p.flag=="end.clamp"+n);Assert.IsNotNull(arrow,"clamp "+n+" has no point for the arrow");
                Assert.AreSame(guns,arrow.previewEncounter,"clamp "+n+"'s arrow point could be offered");Assert.AreEqual(guns.requiredFlag,arrow.requiredFlag,"clamp "+n+"'s arrow is wanted out of turn");
                Assert.IsNotNull(All<Transform>().FirstOrDefault(t=>t.name=="Clamp seat "+n),"clamp "+n+" has no seat for the hold");
            }
        }

        [UnityTest] public IEnumerator NothingStandsAboveThePlaneOnTheCamerasSideOfHerFight()
        {
            yield return Zone("Clamps");
            // What the camera's side is: south of her and within 54 m (her contract), and the crater's whole south rim. Her cover rocks stand to 2.5 by design; her own body is hers.
            bool OnTheCamerasSide(Vector3 p){float r=Flat(p,new Vector3(C.x,0,C.y));return p.z<C.y&&(r<54||r<80&&p.z>230);}
            List<string> Over()
            {
                var found=new List<string>();
                foreach(var filter in All<MeshFilter>())
                {
                    if(filter.sharedMesh==null||!filter.gameObject.activeInHierarchy||filter.name.StartsWith("Cover")||filter.GetComponent<Renderer>()==null||!filter.GetComponent<Renderer>().enabled)continue;
                    // Her own body is hers (her head stands over the plane at C by her contract): anything in a fighter's tree or her coil's is left out. It is the fight's subject, not
                    // something between the lens and a ship: in her room she is solid at the plane as far as her head is drawn (BossController.MotherTune.Solid), so no hull comes under
                    // her snout or behind her head (LastMooringBossTests.NoHullComesUnderHerHeadOrBehindItWhereItCouldBeHidden).
                    var root=filter.transform.root;
                    if(root.GetComponentInChildren<CombatActor>(true)!=null||root.GetComponentInChildren<MotherCoil>(true)!=null||filter.GetComponentInParent<TMPro.TMP_Text>()!=null)continue;
                    var bounds=filter.GetComponent<Renderer>().bounds;if(bounds.max.y<=Plane+.6f)continue;
                    // An imported piece's mesh can't be read in play: its box's top stands for it (a kit piece is small beside the region).
                    if(!filter.sharedMesh.isReadable)
                    {
                        foreach(var (dx,dz) in new[]{(0f,0f),(-1f,-1f),(1f,-1f),(-1f,1f),(1f,1f)})
                        {var p=new Vector3(bounds.center.x+dx*bounds.extents.x,bounds.max.y,bounds.center.z+dz*bounds.extents.z);if(OnTheCamerasSide(p)){found.Add(filter.name+" (its box) at "+p);break;}}
                        continue;
                    }
                    var m=filter.transform.localToWorldMatrix;var v=filter.sharedMesh.vertices;
                    for(int k=0;k<v.Length;k++){var p=m.MultiplyPoint3x4(v[k]);if(p.y>Plane+.6f&&OnTheCamerasSide(p)){found.Add(filter.name+" at "+p);break;}}
                }
                return found;
            }
            // The check first, on a tooth stood south of her: it must be found.
            var tooth=GameObject.CreatePrimitive(PrimitiveType.Cylinder);tooth.name="A tooth on the camera's side";tooth.transform.position=new Vector3(10,3,280);tooth.transform.localScale=new Vector3(2,3,2);
            Assert.IsTrue(Over().Any(f=>f.StartsWith("A tooth")),"fixture: a tooth above the plane south of her isn't found");Object.Destroy(tooth);yield return null;
            // And one stood right under her snout, which isn't hers: what is left out is her own body, not the ground she stands over.
            var beside=GameObject.CreatePrimitive(PrimitiveType.Cube);beside.name="A crate under her snout";beside.transform.position=new Vector3(C.x+2,3,C.y-6);beside.transform.localScale=Vector3.one*2;
            Assert.IsTrue(Over().Any(f=>f.StartsWith("A crate under her snout")),"fixture: a piece of scenery under her head is taken for her own body");Object.Destroy(beside);yield return null;
            CollectionAssert.IsEmpty(Over(),"something stands above the plane on the camera's side of her fight");
        }

        // ------------------------------------------------------------------ the last survey
        [UnityTest] public IEnumerator TheYoungsRoadOpensOnHerReleaseItsStartSavesAndItsFarEndIsTheSurveysPoint()
        {
            Flag("end.motherFreed");yield return Zone("Road");
            var far=All<DiscoveryPoint>().FirstOrDefault(p=>p.report=="NurseryRoad");Assert.IsNotNull(far,"the road has no far end to mark");
            Assert.AreEqual("end.surveyFlown",far.flag);Assert.AreEqual("LastSurvey",far.requiredQuest);Assert.AreEqual(0,far.requiredStep);Assert.AreEqual("end.motherFreed",far.requiredFlag);
            Assert.Less(Flat(far.transform.position,RoadFarEnd),1,"the far end isn't at the road's end");
            var start=All<DiscoveryPoint>().FirstOrDefault(p=>p.saveSpawn=="Road");Assert.IsNotNull(start,"nothing saves at the road's start, so a fall would restart elsewhere");
            Assert.AreEqual("end.motherFreed",start.requiredFlag);
            var road=All<Transform>().FirstOrDefault(t=>t.name=="The young's road");Assert.IsNotNull(road);Assert.IsTrue(road.gameObject.activeInHierarchy,"the road's marks aren't drawn once she is freed");
            var spawn=All<SpawnPoint>().First(s=>s.id=="Road");Assert.Less(Flat(spawn.transform.position,PartyController.Current.Active.transform.position),4,"the party doesn't arrive at the road's start");
        }

        // ------------------------------------------------------------------ the haze, wherever the view stands
        /// <summary>From the valley's head she is on show, and the view draws back two hundred metres and more to hold the party and her. The zone's fog was tuned for the flight camera's
        /// 26 m and, from there, laid a flat pink sheet over everything, the shelves and her coil in it as pale cut-outs (whole-game capture, November 20, s63/010, the chapter's wide view of
        /// its set piece). Drawn back, the plane under the view, her middle and her shelves are as clear as in play, and the crater's floor is hazed by its depth and no more.</summary>
        [UnityTest] public IEnumerator FromTheValleysHeadTheViewDrawnBackToShowHerKeepsThePlaneClearAndHazesTheDepths()
        {
            yield return Zone();
            foreach(var hero in PartyController.Current.members)
            {
                var body=hero.GetComponent<CharacterController>();body.enabled=false;if(hero.motor is FlightMotor ship)ship.Halt();
                hero.transform.position=new Vector3(hero==PartyController.Current.Active?0:4.4f,Plane,232);body.enabled=true;
            }
            Physics.SyncTransforms();var lens=Camera.main;Assert.IsNotNull(lens,"fixture: no camera");
            float Along()=>(lens.transform.position.y-Plane)/Mathf.Max(.01f,-lens.transform.forward.y);
            for(float until=Time.unscaledTime+15;Time.unscaledTime<until&&Along()<150;)yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.Greater(Along(),150,"fixture: the view never drew back to show her from the valley's head");
            // What the zone's fog stands at as this lens draws, read back after it has drawn.
            float start=0,end=0;
            float Fog(Vector3 p){float depth=Vector3.Dot(p-lens.transform.position,lens.transform.forward);return Mathf.Clamp01((depth-start)/Mathf.Max(.01f,end-start));}
            var under=lens.transform.position+lens.transform.forward*Along();
            var haze=All<PlaneHaze>().SingleOrDefault();Assert.IsNotNull(haze,"Sorrel has no haze measured from the plane: its fog is the lens's");
            // The check first, on the zone's fog as the lens alone would have it (the haze off): from here it covers the plane.
            haze.enabled=false;lens.Render();yield return null;lens.Render();start=RenderSettings.fogStartDistance;end=RenderSettings.fogEndDistance;
            Assert.Greater(Fog(under),.5f,$"fixture: the zone's own fog ({start:0} to {end:0}) from {Along():0} m doesn't haze the plane, so the check can't see what s63/010 showed");
            haze.enabled=true;lens.Render();yield return null;lens.Render();start=RenderSettings.fogStartDistance;end=RenderSettings.fogEndDistance;
            under=lens.transform.position+lens.transform.forward*Along();
            var floor=new Vector3(0,-86,356);float plane=Fog(under),her=Fog(new Vector3(C.x,Plane,C.y)),deep=Fog(floor);
            float shelves=Bearings.Max(b=>Fog(Ring(b,27,ShelfY)));
            TestContext.WriteLine($"SORREL_HAZE lens {Along():0} m from the plane, fog {start:0} to {end:0}: the plane under the view {plane:0.00}, her middle {her:0.00}, the shelves at most {shelves:0.00}, the moat's floor {deep:0.00}");
            var failures=new List<string>();
            if(plane>.05f)failures.Add($"the plane under the view is {plane:0.00} fog (clear in play)");
            if(her>.1f)failures.Add($"her middle is {her:0.00} fog");
            if(shelves>.12f)failures.Add($"a shelf is {shelves:0.00} fog");
            if(deep>.55f)failures.Add($"the moat's floor, 87 m under the plane, is {deep:0.00} fog: more than its depth");
            if(deep<.08f)failures.Add($"the moat's floor, 87 m under the plane, is {deep:0.00} fog: the crater has no depth in it");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
