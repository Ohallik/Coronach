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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 9's run against the dawn, the Terminator (docs/maps/Terminator.md; rule: the light itself is the danger). The dawn as a player meets it: standing at its reach
    /// and in frame at the start and at every coupling, still until the party sets off, the slowest pair ahead of it on every stretch with the timetable's slack on every setting, a hero
    /// behind it burnt (faster the longer) and a partner caught alone moved, an enemy and the lesson's burrower taken; a coupling mated only by hands kept on it, holding the line while
    /// they are, in order; a fall and a load starting at the last coupling with the line put back; the ride out to the way home; the pursuit track; the panel's lines.</summary>
    public sealed class TerminatorTests
    {
        const string Zone="Terminator";
        // The builder's numbers (HomeBuilder.Terminator), copied: the tests can't see the editor assembly.
        static readonly Vector2[] Seats={new(-6,594.5f),new(-6,404.5f),new(-6,160.5f)},Checkpoints={new(-3.5f,591.5f),new(-3.5f,401.5f),new(-3.5f,157.5f)};
        static readonly string[] Spawns={"Schoolroom","Kitchens","Engine"},Couplings={"home.coupling1","home.coupling2","home.coupling3"};
        static readonly Vector2 Arrival=new(0,740),Tail=new(-3.5f,51),CrawlerHand=new(5.8f,47.6f),Board=new(-1.4f,162.4f),Lesson=new(-7,716);
        /// <summary>The trodden way, north to south, as the map doc gives it to a route writer.</summary>
        static readonly Vector2[] Way=
        {
            new(0,740),new(2,722),new(-2,704),new(3,686),new(6,668),new(2,652),new(5,636),new(7,620),new(6,606),new(1,598),new(-6,594.5f),
            new(-3,588),new(1,572),new(4,556),new(2,540),new(-1,522),new(1,506),new(4,490),new(6,474),new(3,458),new(5,442),new(7,428),new(6,416),new(1,408),new(-6,404.5f),
            new(-3,398),new(1,382),new(5,366),new(3,350),new(0,334),new(-2,318),new(1,302),new(5,286),new(7,270),new(4,254),new(1,238),new(3,222),new(6,206),new(7,190),new(6,178),new(2,166),new(-6,160.5f),
        };
        readonly List<string> log=new();
        void Heard(string text,string stack,LogType type){if(text.StartsWith("DAWN_")||text.StartsWith("COUPLING_")||text.StartsWith("RIDE_"))log.Add(text);}
        bool Said(string start)=>log.Any(l=>l.StartsWith(start));
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter9");Set("home.crawlAsked");log.Clear();Application.logMessageReceived+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what this zone writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Run(ContentRegistry.Book book)
        {
            // The way home (Dorrow's crawler at the ride's end) opens once the hall has been ridden in: a refused press says what is left to do.
            book.Lock("home.rideDone","couplings");
        }

        // ------------------------------------------------------------------ fixtures
        static void Set(params string[] keys){foreach(var key in keys)GameServices.Current.Flags.SetBool(key,true);}
        static bool Flag(string key)=>GameServices.Current.Flags.GetBool(key);
        static IEnumerator Load(string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(Zone,spawn);float deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,Zone+" never finished loading");Assert.AreEqual(Zone,SceneManager.GetActiveScene().name);
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;}
            deadline=Time.unscaledTime+10;while(DialogueSystem.Current!=null&&!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsNotNull(DawnLine.Current,"fixture: the Terminator has no dawn");
        }
        static (CombatActor lead,CombatActor partner) Party(){var lead=PartyController.Current.Active;return (lead,PartyController.Current.members.First(m=>m!=lead));}
        static Vector3 Ground(Vector2 p)
        {
            var hits=Physics.RaycastAll(new Vector3(p.x,40,p.y),Vector3.down,90,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<CombatActor>()==null&&!h.collider.isTrigger).OrderBy(h=>h.distance).ToArray();
            Assert.IsNotEmpty(hits,"fixture: there is ground at "+p);return hits[0].point;
        }
        static void Put(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.SetPositionAndRotation(position+Vector3.up*.05f,Quaternion.Euler(0,180,0));body.enabled=true;actor.target=null;actor.TargetLocked=false;Physics.SyncTransforms();
        }
        static void Put(CombatActor actor,Vector2 p)=>Put(actor,Ground(p));
        static IEnumerator Wait(float seconds){float until=Time.time+seconds;while(Time.time<until)yield return null;}
        static IEnumerator Until(System.Func<bool> done,float seconds){float until=Time.unscaledTime+seconds;while(!done()&&Time.unscaledTime<until)yield return null;}
        static TreadCoupling Coupling(int index)=>TreadCoupling.All.First(c=>c.index==index);
        /// <summary>The line's point level with the lead, on the ground, as the game's camera sees it (viewport: 0 to 1 is in frame).</summary>
        static Vector3 LineOnScreen(DawnLine dawn,CombatActor lead)
        {
            // On the ground at the lead's height (the plain is level within a hand): a ray down there finds a car's roof where the line crosses one.
            var at=dawn.On(lead.transform.position);return Camera.main.WorldToViewportPoint(new Vector3(at.x,lead.transform.position.y,at.z));
        }
        static bool InFrame(Vector3 viewport,float margin=.03f)=>viewport.z>0&&viewport.x>margin&&viewport.x<1-margin&&viewport.y>margin&&viewport.y<1-margin;
        /// <summary>Gets the line moving: the lead walks off the arrival.</summary>
        static IEnumerator SetOff(DawnLine dawn,CombatActor lead)
        {
            Put(lead,Arrival+new Vector2(0,-8));yield return Until(()=>dawn.Live,2);Assert.IsTrue(dawn.Live,"the line didn't start when the lead had walked eight metres from the arrival");
        }

        // ------------------------------------------------------------------ the dawn as a player meets it
        [UnityTest] public IEnumerator TheRunStartsWithTheLineAtItsReachInFrameBehindThePartyAndStillUntilItSetsOff()
        {
            yield return Load();var dawn=DawnLine.Current;var (lead,_)=Party();
            Assert.AreEqual(11,dawn.Gap,.6f,"the line doesn't stand at its reach behind the arrival");Assert.IsTrue(Said("DAWN_RESTART"),"the line wasn't put at its place as the zone opened");
            var viewport=LineOnScreen(dawn,lead);Assert.IsTrue(InFrame(viewport),$"the line 11 m behind the arrival is out of the frame (viewport {viewport})");
            Assert.Greater(viewport.y,.6f,"the line isn't in the top of the frame, behind the party");
            Assert.AreEqual(Vector2.down,dawn.axis,"the run doesn't go south, toward the camera");
            float at=dawn.At;yield return Wait(3);
            Assert.AreEqual(at,dawn.At,.01f,"the line moved before the party had set off or eight seconds had passed");Assert.IsFalse(dawn.Live,"the line is live before the party has set off");
            yield return SetOff(dawn,lead);at=dawn.At;yield return Wait(1);Assert.Greater(dawn.At,at+.5f,"the line didn't come on once the party set off");
            Assert.AreEqual("Dventure at Dusk",MusicDirector.Current.Track,"the run doesn't start on the dusk track");
        }

        [UnityTest] public IEnumerator AtEachCouplingTheLineIsInFrameBehindTheSeatAndOnlyTheNextCouplingIsOffered()
        {
            for(int i=0;i<3;i++)
            {
                if(i>0)Set(Couplings[i-1]);
                yield return Load(i==0?"Arrival":Spawns[i-1]);var dawn=DawnLine.Current;var (lead,_)=Party();
                // Set down at the seat from the checkpoint before (240 m off for the third): the camera is given two seconds to come after the party, as it would have walked there.
                Put(lead,Seats[i]);dawn.Held=true;dawn.At=dawn.Along(lead.transform.position)-9;yield return Wait(2f);
                // The check has to be able to fail: a line 25 m behind is out of the frame.
                Assert.IsFalse(InFrame(Camera.main.WorldToViewportPoint(lead.transform.position+Vector3.forward*25)),"fixture: the frame check passes a line 25 m behind the seat");
                var viewport=LineOnScreen(dawn,lead);Assert.IsTrue(InFrame(viewport),$"coupling {i+1}: the line 9 m behind the seat is out of the frame (viewport {viewport}; camera {Camera.main.transform.position-lead.transform.position} from the lead, field {Camera.main.fieldOfView:0})");
                Assert.Greater(viewport.y,.55f,$"coupling {i+1}: the line isn't in the top of the frame, behind the party");
                var seat=Camera.main.WorldToViewportPoint(Coupling(i+1).transform.position);Assert.IsTrue(InFrame(seat,.1f),$"coupling {i+1}: its seat is out of the frame from the seat");
                Assert.AreSame(Coupling(i+1),TreadCoupling.Next,$"coupling {i+1} isn't the next to mate");Assert.AreEqual(i,TreadCoupling.MatedCount,$"coupling {i+1}: the couplings behind it aren't mated on load");
                PromptService.FindInteraction(lead.transform.position);Assert.AreSame(Coupling(i+1),PromptService.Interaction,$"standing at coupling {i+1}'s seat, it isn't offered");
                for(int k=i+2;k<=3;k++)Assert.IsFalse(Coupling(k).Available,$"coupling {k} is offered before coupling {k-1} is mated");
                Assert.IsTrue(ZoneLine.Read(out var said,out _)&&said==$"Re-mate the coupling ({i}/3)",$"at coupling {i+1} the panel says '{said}', not 'Re-mate the coupling ({i}/3)'");
            }
        }

        [UnityTest] public IEnumerator ACouplingIsMatedOnlyByHandsKeptOnItAndTheLineHoldsWhileTheyAre()
        {
            yield return Load();var dawn=DawnLine.Current;var (lead,_)=Party();yield return SetOff(dawn,lead);
            var c=Coupling(1);Put(lead,Seats[0]);yield return Wait(.2f);dawn.At=dawn.Along(lead.transform.position)-8;
            // A step out of the seat lets go, and what was wound runs back.
            c.Interact();Assert.AreSame(lead,c.Holder,"Interact didn't put the lead's hands on the coupling");yield return Wait(1);float wound=c.Wound;Assert.Greater(wound,.2f,"the jack didn't wind with hands on it");
            Put(lead,Seats[0]+new Vector2(4,-1));yield return Wait(.3f);Assert.IsNull(c.Holder,"stepping out of the seat didn't let go");Assert.IsTrue(Said("COUPLING_LET_GO 1"),"no let-go was logged");
            yield return Wait(.6f);Assert.Less(c.Wound,wound,"what was wound didn't run back with nobody's hands on it");
            // Kept on: the line holds where it stands, and in 3.2 s it is mated: the flag, the checkpoint, a save.
            Put(lead,Seats[0]);yield return Wait(.2f);dawn.At=dawn.Along(lead.transform.position)-8;c.Interact();float at=dawn.At;
            yield return Wait(1.5f);Assert.AreEqual(at,dawn.At,.01f,"the line came on while a hero's hands were on the coupling");
            yield return Until(()=>c.IsMated,4);Assert.IsTrue(c.IsMated,"3.2 s of hands didn't mate the coupling");
            Assert.IsTrue(Flag("home.coupling1"),"mating it didn't set its flag");Assert.AreEqual("Schoolroom",GameServices.Current.State.spawn,"mating it didn't make its spawn the checkpoint");
            yield return Wait(1);Assert.Greater(dawn.At,at+.5f,"the line didn't come on again once the hands were off");
            Assert.AreSame(Coupling(2),TreadCoupling.Next,"after the first, the second isn't next");
        }

        [UnityTest] public IEnumerator EachStretchKeepsTheSlowestPairAheadWithTheTimetablesSlackOnEverySetting()
        {
            yield return Load();var dawn=DawnLine.Current;
            // The slowest pair a player can field walks 6.7 m/s; the way's legs are within twenty degrees of south.
            const float Slowest=6.7f;var settings=new[]{DifficultySetting.Gentle,DifficultySetting.Standard,DifficultySetting.Demanding};
            // The map doc's timetable: the slack standing still from the reach, by stretch and setting.
            float[,] slack={{9.8f,6.9f,6f},{7.1f,5f,4.3f},{5.8f,4.1f,3.5f}};
            int[] from={0,10,24},to={10,24,41};
            // The run has to be able to fail it: a line as quick as the slowest pair runs closes on it.
            {
                float line=-Way[0].y-dawn.reach,least=float.PositiveInfinity;
                for(int k=0;k<10;k++){var a=Way[k];var b=Way[k+1];float length=Vector2.Distance(a,b);for(float d=0;d<length;d+=Slowest*.05f){var p=Vector2.Lerp(a,b,d/length);line=DawnLine.Step(line,-p.y,Slowest,dawn.reach,false,.05f);least=Mathf.Min(least,-p.y-line);}}
                Assert.Less(least,dawn.reach*.7f,"fixture: a line as fast as the slowest pair never closes on it, so the check below can't fail");
            }
            for(int stretch=0;stretch<3;stretch++)for(int s=0;s<3;s++)
            {
                float speed=dawn.speeds[stretch]*DawnLine.Pace(settings[s]);
                Assert.AreEqual(slack[stretch,s],dawn.reach/speed,.15f,$"stretch {stretch}, {settings[s]}: the slack standing still from the reach isn't the timetable's");
                Assert.Greater(dawn.reach/speed,3.4f,$"stretch {stretch}, {settings[s]}: under 3.4 s of slack is a fight nobody can stop for");
                // Run it: the lead along the way at the slowest pace, the line by the game's own step; it is never on the lead.
                float line=dawn.Along(new Vector3(Way[from[stretch]].x,0,Way[from[stretch]].y))-dawn.reach,least=float.PositiveInfinity;
                for(int k=from[stretch];k<to[stretch];k++)
                {
                    var a=Way[k];var b=Way[k+1];float length=Vector2.Distance(a,b);
                    for(float d=0;d<length;d+=Slowest*.05f)
                    {
                        var p=Vector2.Lerp(a,b,d/length);float lead=dawn.Along(new Vector3(p.x,0,p.y));
                        line=DawnLine.Step(line,lead,speed,dawn.reach,false,.05f);least=Mathf.Min(least,lead-line);
                    }
                }
                Assert.Greater(least,dawn.reach*.7f,$"stretch {stretch}, {settings[s]}: running it at the slowest pair's pace the line came within {least:0.0} m");
                TestContext.WriteLine($"STRETCH {stretch} {settings[s]} speed={speed:0.00} slack={dawn.reach/speed:0.0}s closest={least:0.0}m");
            }
        }

        [UnityTest] public IEnumerator AHeroBehindTheLineBurnsFasterTheLongerAndAPartnerCaughtAloneIsMovedNotBurnt()
        {
            yield return Load();var dawn=DawnLine.Current;var (lead,partner)=Party();yield return SetOff(dawn,lead);dawn.Held=true;
            lead.Health.InvulnerableUntil=0;lead.Health.integrity=lead.Health.maximum;
            // On the edge itself (a hand behind it) nothing burns: a roll across the line isn't caught.
            dawn.At=dawn.Along(lead.transform.position)+.15f;yield return Wait(.6f);Assert.AreEqual(lead.Health.maximum,lead.Health.integrity,.5f,"a hero a hand behind the edge was burnt");
            dawn.At=dawn.Along(lead.transform.position)+2;yield return Wait(.5f);float first=lead.Health.maximum-lead.Health.integrity;
            yield return Wait(.5f);float second=lead.Health.maximum-lead.Health.integrity-first;
            Assert.Greater(first,lead.Health.maximum*.03f,"half a second in the light took almost nothing");Assert.Greater(second,first*1.2f,"the burn didn't climb the longer the hero stayed in the light");
            Assert.Less(first,lead.Health.maximum*.2f,"the light's edge is an instant kill, not a fast burn");Assert.IsTrue(Said("DAWN_BURNS_HERO"),"no burn was logged");
            // The partner caught alone, the lead ahead: set down beside the lead.
            dawn.At=dawn.Along(lead.transform.position)-3;var behind=lead.transform.position+Vector3.forward*6;Put(partner,new Vector2(behind.x,behind.z));partner.Health.integrity=partner.Health.maximum;
            yield return Wait(.4f);Assert.IsTrue(Said("DAWN_SPARED"),"a partner behind the line with the lead ahead wasn't moved");
            Assert.Greater(dawn.Ahead(partner.transform.position),.9f,"the partner was left behind the line");Assert.AreEqual(partner.Health.maximum,partner.Health.integrity,.5f,"the partner was burnt");
        }

        [UnityTest] public IEnumerator TheLineTakesAnEnemyItReachesAndTheLessonsBurrowerBeforeItCanReachTheParty()
        {
            yield return Load();var dawn=DawnLine.Current;var (lead,_)=Party();yield return SetOff(dawn,lead);dawn.Held=true;
            // The lesson: the burrower hunting along the light is taken as the line reaches it, west of the way, while the party is ahead.
            var hunter=Object.FindObjectsByType<DawnTouched>(FindObjectsSortMode.None).First(t=>t.kind==DawnTouched.Kind.Creature);
            Assert.Less(Vector2.Distance(new Vector2(hunter.transform.position.x,hunter.transform.position.z),Lesson),1,"fixture: the lesson's burrower isn't where the map has it");
            Put(lead,new Vector2(-2,707));yield return Wait(.2f);dawn.At=-Lesson.y+1.5f;yield return Wait(1);
            Assert.IsTrue(hunter.Dead,"the line passed the lesson's burrower and it lives");Assert.IsTrue(Said("DAWN_TOUCHED"),"nothing was logged as the line took the burrower");
            Assert.Greater(dawn.Gap,5,"fixture: the party is ahead of the line while it takes the burrower");
            var viewport=Camera.main.WorldToViewportPoint(hunter.transform.position);Assert.IsTrue(InFrame(viewport),"the burrower is taken out of the frame: the lesson isn't seen");
            // An enemy the line passes falls, and pays nothing.
            var def=GameCatalog.Find<EnemyDef>("HeatScavenger");Assert.IsNotNull(def,"fixture: the Heat-scavenger isn't defined");
            Put(lead,new Vector2(2,688));var enemy=ActorFactory.Enemy(def,Ground(new Vector2(6,710)));yield return Wait(.3f);dawn.At=-710+4;
            yield return Until(()=>enemy==null||!enemy.Health.Alive,4);
            Assert.IsTrue(enemy==null||!enemy.Health.Alive,"the line passed a Heat-scavenger by four metres and it lives");
        }

        [UnityTest] public IEnumerator AFallOrALoadMidRunStartsAtTheLastCouplingWithTheLinePutBackAtItsReach()
        {
            // Left by the chart mid-run and back in at the run's start: stood at the checkpoint.
            Set(Couplings[0]);yield return Load();var (lead,_)=Party();
            Assert.IsTrue(Said("DAWN_RESUMED_AT Spawn_Schoolroom"),"in at the run's start with the first coupling mated, the party wasn't stood at its checkpoint");
            Assert.Less(Vector2.Distance(new Vector2(lead.transform.position.x,lead.transform.position.z),Checkpoints[0]),3,"the party isn't at the first coupling's checkpoint");
            Assert.AreEqual(11,DawnLine.Current.Gap,.6f,"the line isn't at its reach behind the checkpoint");Assert.AreEqual(1,DawnLine.Current.Checkpoint);
            // A save at the second coupling (its spawn), loaded: the line behind it, two mated, the third next.
            Set(Couplings[1]);GameServices.Current.State.spawn="Kitchens";GameServices.Current.Saves.Save("autosave",GameServices.Current.State);log.Clear();
            yield return Load("Kitchens");lead=PartyController.Current.Active;
            Assert.IsTrue(Said("DAWN_RESTART Spawn_Kitchens"),"the load didn't put the line back behind the second coupling");Assert.AreEqual(11,DawnLine.Current.Gap,.6f,"the line isn't at its reach after the load");
            Assert.AreEqual(2,TreadCoupling.MatedCount,"the loaded run doesn't have two couplings mated");Assert.AreSame(Coupling(3),TreadCoupling.Next,"the third isn't next after a load at the second");
            Assert.IsFalse(DawnLine.Current.Live,"the line is moving the moment the load opens");
        }

        [UnityTest] public IEnumerator TheThirdCouplingWakesTheEngineCarAndTheRideCarriesThePartyOutToTheWayHome()
        {
            Set(Couplings);yield return Load("Engine");var dawn=DawnLine.Current;var (lead,_)=Party();
            var ride=Object.FindFirstObjectByType<TreadRide>();Assert.IsNotNull(ride,"fixture: no ride");Assert.IsTrue(ride.Woken,"the engine car isn't awake with three couplings mated");
            Put(lead,Board);yield return Wait(.3f);PromptService.FindInteraction(lead.transform.position);Assert.AreSame(ride,PromptService.Interaction,"at the gallery's steps the ride isn't offered");
            Assert.IsTrue(ZoneLine.Read(out var said,out _)&&said=="Ride the tread-hall out","the panel doesn't say to ride the tread-hall out");
            var home=Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).First(w=>w.scene=="MeridianCrawl");Assert.IsFalse(home.Available&&Flag(home.requiredFlag),"the way home is open before the ride");
            ride.Interact();yield return Until(()=>ride.Riding,2);Assert.IsTrue(ride.Riding,"the ride didn't begin");Assert.IsTrue(GameServices.Current.Input.Blocked,"the party has its hands during the ride");
            // D423: the panel only moves forward. The capture (s56/061, 063) had the story's step back ("Re-mate three couplings...") once the ride began and after it.
            yield return Wait(.5f);
            Assert.IsTrue(ZoneLine.Read(out said,out _)&&said==DawnLine.RideLine,$"during the ride the panel says '{said}' (the story's step shows again), not '{DawnLine.RideLine}'");
            yield return Until(()=>Said("RIDE_DONE"),30);Assert.IsTrue(Said("RIDE_DONE"),"the ride never ended");yield return null;yield return null;
            Assert.IsTrue(Flag("home.rideDone"),"the ride didn't set its flag");Assert.AreEqual("Tail",GameServices.Current.State.spawn,"the ride's end isn't the checkpoint");
            Assert.Less(Vector2.Distance(new Vector2(lead.transform.position.x,lead.transform.position.z),Tail),1.5f,"the party wasn't set down at the ride's end");
            Assert.IsTrue(dawn.Over,"the line still runs after the ride");Assert.Greater(dawn.Gap,30,"the line didn't fall behind the ride");Assert.IsFalse(GameServices.Current.Input.Blocked,"the party never got its hands back");
            Put(lead,CrawlerHand);yield return Wait(.3f);PromptService.FindInteraction(lead.transform.position);
            Assert.AreSame(home,PromptService.Interaction,"from the crawler's side the way home isn't offered");Assert.AreEqual("Gap",home.spawn,"the way home doesn't arrive at the Crawl's gap");
            Assert.IsTrue(ZoneLine.Read(out said,out var arrow)&&arrow.HasValue&&Vector3.Distance(arrow.Value,home.transform.position)<1,"after the ride the panel's arrow isn't on the way home");
            Assert.AreEqual(DawnLine.HomeLine,said,"after the ride the panel goes back to the story's step instead of on to the way home");
        }

        [UnityTest] public IEnumerator ThePursuitTrackComesInAsTheLineClosesAndThePanelSaysKeepAheadOfTheDawn()
        {
            yield return Load();var dawn=DawnLine.Current;var (lead,_)=Party();
            Assert.AreEqual("Dventure at Dusk",MusicDirector.Current.Track,"the arrival doesn't play the dusk track");
            yield return SetOff(dawn,lead);Put(lead,new Vector2(3,686));dawn.Held=true;dawn.At=dawn.Along(lead.transform.position)-10;yield return Wait(.6f);
            Assert.IsFalse(dawn.Pursuing,"the pursuit came in with the line ten metres off");
            Assert.IsTrue(ZoneLine.Read(out var said,out var arrow)&&said=="Keep ahead of the dawn","the panel doesn't say to keep ahead of the dawn as the run goes");
            Assert.IsTrue(arrow.HasValue&&Vector3.Distance(arrow.Value,Coupling(1).transform.position)<1,"the panel's arrow isn't on the next coupling");
            dawn.At=dawn.Along(lead.transform.position)-7;yield return Wait(.6f);
            Assert.IsTrue(dawn.Pursuing,"the line came within 7 m and the pursuit didn't come in");Assert.IsTrue(Said("DAWN_PURSUIT"));
            Assert.AreEqual("Starfight",MusicDirector.Current.Track,"the pursuit track isn't playing as the line closes");
            dawn.At=dawn.Along(lead.transform.position)-11;yield return Wait(.6f);Assert.AreEqual("Starfight",MusicDirector.Current.Track,"the pursuit flapped back to the dusk track");
        }
    }
}
