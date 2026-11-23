using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 6's combat map, the Frozen Host (docs/maps/FrozenHost.md): the White Reach walked from the gate to the dig, its crevasses that stop a walker, its herd that bolts
    /// for a runner and not for a walker, its whiteouts and their vent pockets; the dig thawed a level at a time by its rigs and walked down into as each plug goes, a thaw kept in the
    /// save, the gap that jumps the party to the bottom, Idra walking ahead; the flags its talks and rigs set; the bottom gallery built to the boss's contract; and no wall between the
    /// fixed camera and any fight. Flags are set directly where a conversation would set them: the script's nodes are tested by the dialogue tests.</summary>
    public sealed class FrozenHostTests
    {
        const string Host="FrozenHost";
        // The builder's numbers (HoarfellBuilder.FrozenHost), copied: the tests can't see the editor assembly.
        const float L1=-4,L2=-8.5f,L3=-13,Bottom=-17.5f;
        static readonly Vector2 C=new(208,218);
        readonly List<string> log=new();
        void Heard(string text,string stack,LogType type)
        {
            if(text.StartsWith("ICE_")||text.StartsWith("RIG_RUN")||text.StartsWith("DISCOVERY")||text.StartsWith("HERD_")||text.StartsWith("WHITEOUT_")||text.StartsWith("LEAP_DOWN")||text.StartsWith("GUIDE_"))log.Add(text);
        }
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter6");log.Clear();Application.logMessageReceived+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what this chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void FrozenHostBook(ContentRegistry.Book book)
        {
            // The loading winch runs once the Marshal is asleep; its refusal names the Marshal.
            book.Lock("hoar.marshalAsleep","Host Marshal");
        }

        // ------------------------------------------------------------------ fixtures
        static void Set(params string[] keys){foreach(var key in keys)GameServices.Current.Flags.SetBool(key,true);}
        static bool Flag(string key)=>GameServices.Current.Flags.GetBool(key);
        static IEnumerator Zone(string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(Host,spawn);float deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,Host+" never finished loading");Assert.AreEqual(Host,SceneManager.GetActiveScene().name);
            Hold();yield return new WaitForSecondsRealtime(.6f);
        }
        /// <summary>The heroes stand where they are put and nobody wanders into a crossing.</summary>
        static void Hold(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;}}
        /// <summary>The ground under a plan point: the first solid thing a ray from above meets that isn't a seal or a block of ice that melts (a Membrane's).</summary>
        static Vector3 Ground(Vector2 p)
        {
            var hits=Physics.RaycastAll(new Vector3(p.x,40,p.y),Vector3.down,90,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<Membrane>()==null&&h.collider.GetComponentInParent<CombatActor>()==null&&h.collider.attachedRigidbody==null).OrderBy(h=>h.distance).ToArray();
            Assert.IsNotEmpty(hits,"fixture: there is ground at "+p);
            return hits[0].point;
        }
        static Vector3 Hero=>PartyController.Current.Active.transform.position;
        static bool Reach(Vector3 from,Vector2 to)
        {
            var path=new NavMeshPath();
            return NavMesh.SamplePosition(from,out var start,3,NavMesh.AllAreas)&&NavMesh.SamplePosition(Ground(to),out var end,1.5f,NavMesh.AllAreas)&&
                NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        }
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static DiscoveryPoint Point(System.Func<DiscoveryPoint,bool> which,string what)
        {var found=All<DiscoveryPoint>().Where(which).ToArray();Assert.AreEqual(1,found.Length,what+" is not in the Frozen Host exactly once");return found[0];}
        static IceThaw Heat(string flag)=>All<IceThaw>().Single(t=>t.flag==flag);
        static void Put(CombatActor actor,Vector3 at){var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=at;cc.enabled=true;}
        static Vector2 Bearing(float bearing,float radius)=>C+new Vector2(Mathf.Sin(bearing*Mathf.Deg2Rad),Mathf.Cos(bearing*Mathf.Deg2Rad))*radius;
        /// <summary>Waits for a rig's ice to have warmed and what it melts to have gone (the paths carve a frame or two after).</summary>
        static IEnumerator Thawed(IceThaw heat,float seconds=10)
        {
            float deadline=Time.unscaledTime+seconds;while(!heat.Melted&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.5f);
        }
        /// <summary>Title, then Continue from the autosave, as a player does.</summary>
        IEnumerator Continue()
        {
            SceneFlow.Current.LoadZone("Title");while(SceneFlow.Current.Loading)yield return null;yield return null;
            Object.FindFirstObjectByType<TitleScreen>().StartGame(true);
            float until=Time.realtimeSinceStartup+40;while(SceneFlow.Current.Loading&&Time.realtimeSinceStartup<until)yield return null;
            Assert.AreEqual(Host,SceneFlow.Current.Zone,"the save doesn't resume in the Frozen Host");Hold();yield return null;yield return null;
        }

        // ------------------------------------------------------------------ the Reach
        [UnityTest] public IEnumerator TheReachIsWalkedFromTheGateToTheDigAndItsCrevassesStopAWalker()
        {
            yield return Zone();var start=Hero;
            foreach(var (what,at) in new[]{("the spear field",new Vector2(0,32)),("the main bridge",new Vector2(2,50)),("the west bridge",new Vector2(-30,51)),("the sledge",new Vector2(-40,59)),
                ("the herd field",new Vector2(2,63)),("the gap",new Vector2(4,81)),("vent pocket one",new Vector2(-30,104)),("vent pocket two",new Vector2(34,118)),("the stalkers' field",new Vector2(6,120)),
                ("crevasse two's bridge",new Vector2(6,136)),("the bench",new Vector2(-8.4f,151)),("the camp",new Vector2(24,160)),("the upper gallery",new Vector2(24,181)),("rig one",new Vector2(58.5f,189.5f))})
                Assert.IsTrue(Reach(start,at),"from the gate nobody can walk to "+what+" at "+at);
            // Down a crevasse is nowhere a walker gets to: the cornice's lip stops them at the edge, either side of a bridge and along it.
            foreach(var at in new[]{new Vector2(20,50+2*Mathf.Sin(20*.06f+.4f)),new Vector2(-12,50+2*Mathf.Sin(-12*.06f+.4f)),new Vector2(30,136+1.5f*Mathf.Sin(30*.08f+2.1f))})
            {
                var floor=Physics.Raycast(new Vector3(at.x,30,at.y),Vector3.down,out var hit,60,~0,QueryTriggerInteraction.Ignore);
                Assert.IsTrue(floor&&hit.point.y<-4,"fixture: there is a crevasse at "+at);
                Assert.IsFalse(Reach(start,at),"a body can walk down into the crevasse at "+at);
            }
            var lips=All<BoxCollider>().Where(b=>b.name=="Crevasse lip"||b.name=="Bridge lip").ToArray();
            Assert.Greater(lips.Length,60,"the crevasses have no lip to stop a walker");Assert.IsTrue(lips.All(b=>b.GetComponentInParent<Renderer>()!=null),"a crevasse's lip is an invisible wall: nothing is drawn over it");
            // Until rig one has run the dig stops at its first plug.
            foreach(var (what,at) in new[]{("the second level",new Vector2(90,184)),("the third level",new Vector2(124,219)),("the bottom gallery",C)})Assert.IsFalse(Reach(start,at),what+" can be walked to before any rig has run");
        }

        // ------------------------------------------------------------------ the rigs: the zone's mechanic
        [UnityTest] public IEnumerator EachRigThawsItsIceAndWhatItUncoversCanBeWalked()
        {
            yield return Zone();var start=Hero;
            var one=Heat("hoar.rig1");Assert.AreEqual(0,one.Heat,.01f,"rig one's plug starts thawed");
            // The check has to be able to see ice that doesn't answer: with the thaw switched off, the rig's flag leaves the plug standing and the second level out of reach.
            one.enabled=false;Set("hoar.rig1");yield return new WaitForSecondsRealtime(4.5f);
            Assert.IsFalse(one.Melted,"fixture: with no thaw the plug still melted");Assert.IsFalse(Reach(start,new Vector2(90,184)),"fixture: the check can't see a plug that doesn't melt");
            GameServices.Current.Flags.SetBool("hoar.rig1",false);yield return Zone();start=Hero;
            // Rig one, worked by its hand: the plug over ramp one goes amber, melts, and the second level can be walked to. Nothing else changes.
            var hand=Point(p=>p.flag=="hoar.rig1","rig one's valve");Assert.AreEqual("Run the thaw rig",hand.prompt);Assert.IsTrue(hand.Available,"rig one can't be worked");
            one=Heat("hoar.rig1");hand.Interact();Assert.IsTrue(Flag("hoar.rig1"),"working rig one sets nothing");
            yield return new WaitForSecondsRealtime(1.4f);Assert.That(one.Heat,Is.InRange(.05f,.99f),"rig one's ice doesn't warm by degrees");
            yield return Thawed(one);
            Assert.AreEqual(1,one.Heat,.01f,"rig one's ice doesn't go amber");Assert.IsTrue(one.Melted,"rig one's plug never melts");
            Assert.IsTrue(log.Any(l=>l.StartsWith("RIG_RUN hoar.rig1")),"rig one's wheel doesn't turn and say so");Assert.IsTrue(log.Any(l=>l.StartsWith("ICE_MELTED hoar.rig1")),"the plug never says it melted");
            foreach(var (what,at) in new[]{("ramp one",new Vector2(72,187)),("the second level",new Vector2(90,184)),("rig two",new Vector2(99,192.5f)),("the Excavators' cut",new Vector2(124,184))})
                Assert.IsTrue(Reach(start,at),"after rig one nobody can walk to "+what);
            foreach(var (what,at) in new[]{("the left gallery",new Vector2(90,210)),("the third level",new Vector2(124,219))})Assert.IsFalse(Reach(start,at),what+" can be walked to after rig one alone");
            // Rig two: the plug across the left gallery and the rank in its west wall both go; the rank's fight waits for the party to walk in.
            var two=Heat("hoar.rig2");Assert.AreEqual(2,two.melts.Length,"rig two's heat doesn't reach the rank beside its plug");
            Point(p=>p.flag=="hoar.rig2","rig two's valve").Interact();yield return Thawed(two);
            Assert.IsTrue(two.Melted&&two.melts.All(m=>m==null||!m.gameObject.activeInHierarchy||m.Open),"rig two's ice doesn't melt");
            var rank=All<EncounterVolume>().Single(e=>e.encounterId=="Dig_Rank");Assert.AreEqual("hoar.rig2",rank.requiredFlag);Assert.IsFalse(rank.Started,"the rank woke before anyone walked into the gallery");
            foreach(var (what,at) in new[]{("the left gallery",new Vector2(90,210)),("where the rank stood",new Vector2(86,206.5f)),("ramp two",new Vector2(104,214)),("the third level",new Vector2(124,219)),("rig three",new Vector2(145.5f,228.5f))})
                Assert.IsTrue(Reach(start,at),"after rig two nobody can walk to "+what);
            Assert.IsFalse(Reach(start,new Vector2(172,218)),"the bottom gallery can be walked to before rig three");
            // Rig three: the last plug, the rank beside it and the hounds' pen.
            var three=Heat("hoar.rig3");Point(p=>p.flag=="hoar.rig3","rig three's valve").Interact();yield return Thawed(three);
            foreach(var (what,at) in new[]{("ramp three",new Vector2(158,219)),("the bottom gallery's mouth",new Vector2(172,218)),("the checkpoint",new Vector2(177,211)),("the bottom gallery",C)})
                Assert.IsTrue(Reach(start,at),"after rig three nobody can walk to "+what);
        }

        [UnityTest] public IEnumerator AThawIsKeptInTheSaveAndLoadsWithNothingPlayed()
        {
            Set("hoar.reachEntered","hoar.herdMet","hoar.whiteoutMet","hoar.vessaHeard","hoar.ranksSeen","hoar.rig1","hoar.offerHeard","hoar.rig2");
            yield return Zone();
            var saved=GameServices.Current.Saves.Load("autosave");Assert.AreEqual(Host,saved.zone,"fixture: arriving autosaves");Assert.IsTrue(saved.flags["hoar.rig2"],"fixture: the save has rig two");
            // The check has to be able to see a save that lost its thaw: the same save with the rigs' flags taken out continues with every plug standing.
            var stripped=GameServices.Current.Saves.Load("autosave");stripped.flags.Remove("hoar.rig1");stripped.flags.Remove("hoar.rig2");GameServices.Current.Saves.Save("autosave",stripped);
            yield return Continue();
            Assert.IsTrue(All<IceThaw>().Where(t=>t.flag=="hoar.rig1"||t.flag=="hoar.rig2").All(t=>!t.Melted&&t.Heat<.01f),"fixture: the check can't see a save that lost its thaw");
            var kept=GameServices.Current.Saves.Load("autosave");kept.flags["hoar.rig1"]=kept.flags["hoar.rig2"]=true;kept.zone=Host;kept.spawn="Arrival";GameServices.Current.Saves.Save("autosave",kept);
            log.Clear();yield return Continue();yield return new WaitForSecondsRealtime(.6f);
            foreach(var flag in new[]{"hoar.rig1","hoar.rig2"})
            {
                var heat=Heat(flag);Assert.IsTrue(heat.Melted,flag+"'s plug loads standing");Assert.AreEqual(1,heat.Heat,.01f,flag+"'s ice loads blue");
                Assert.IsTrue(heat.melts.All(m=>m==null||!m.gameObject.activeInHierarchy),flag+"'s melted ice is back in the way");
            }
            Assert.IsFalse(log.Any(l=>l.StartsWith("ICE_THAW")||l.StartsWith("ICE_MELTED")||l.StartsWith("RIG_RUN")),"a loaded thaw plays again: "+string.Join(", ",log));
            Assert.IsTrue(Reach(Hero,new Vector2(104,214)),"ramp two can't be walked to after a load with both rigs run");
            Assert.AreEqual(0,Heat("hoar.rig3").Heat,.01f,"rig three's ice loads thawed with its rig never run");
        }

        // ------------------------------------------------------------------ the herd and the whiteouts
        [UnityTest] public IEnumerator TheHerdWalksOnForAWalkerAndBoltsDownMarkedLanesForARunner()
        {
            Set("hoar.reachEntered","hoar.herdMet");
            yield return Zone();
            var herd=Object.FindFirstObjectByType<RimeHerd>();Assert.IsNotNull(herd,"there is no herd on the Reach");Assert.GreaterOrEqual(herd.beasts.Length,16,"a herd of a dozen isn't a herd");
            var lead=PartyController.Current.Active;var beast=herd.beasts.OrderBy(b=>Mathf.Abs(b.position.x-4)+Mathf.Abs(b.position.z-66)).First();
            var where=beast.position;Put(lead,Ground(new Vector2(where.x+4.5f,where.z-3)));yield return null;
            var before=herd.beasts.Select(b=>b.position).ToArray();
            // A walker at its flank: it walks on, and nothing bolts.
            for(float t=0;t<2.5f;t+=Time.deltaTime){lead.motor.Move(new Vector2(0,.4f),false,false);yield return null;}
            Assert.AreEqual(0,herd.Startles,"the herd bolted for someone walking");Assert.AreEqual(RimeHerd.State.Walking,herd.Now);
            Assert.Greater(herd.beasts.Select((b,i)=>Vector3.Distance(b.position,before[i])).Max(),1,"the herd isn't walking");
            // A dodge beside it: it marks its lanes, then bolts down them.
            var near=herd.beasts.OrderBy(b=>Vector3.Distance(b.position,lead.transform.position)).First();Assert.Less(Vector3.Distance(near.position,lead.transform.position),7,"fixture: the hero is beside the herd");
            lead.Dodge(Vector3.right);yield return null;yield return null;
            Assert.AreEqual(1,herd.Startles,"a dodge beside the herd doesn't set it off");Assert.AreEqual(RimeHerd.State.Warning,herd.Now,"the herd bolts without marking its lanes first");
            Assert.IsTrue(log.Any(l=>l.StartsWith("HERD_STARTLED")));
            float until=Time.unscaledTime+6;while(herd.Now!=RimeHerd.State.Bolting&&Time.unscaledTime<until)yield return null;
            Assert.AreEqual(RimeHerd.State.Bolting,herd.Now,"the herd never bolts");
            until=Time.unscaledTime+8;while(herd.Now!=RimeHerd.State.Walking&&Time.unscaledTime<until)yield return null;
            Assert.IsTrue(log.Any(l=>l.StartsWith("HERD_SETTLED")),"the herd never settles");
        }

        [UnityTest] public IEnumerator TheFirstWhiteoutClosesAfterIdrasLessonAndTheVentPocketIsShelter()
        {
            Set("hoar.reachEntered","hoar.herdMet","hoar.herdPassed");
            yield return Zone();
            var weather=Whiteout.Current;Assert.IsNotNull(weather,"there are no whiteouts on the Reach");Assert.AreEqual("hoar.whiteoutMet",weather.startFlag);
            Assert.AreEqual(Whiteout.Phase.Calm,weather.Now,"fixture: calm before the lesson");float clear=RenderSettings.fogEndDistance;
            var lead=PartyController.Current.Active;Put(lead,Ground(new Vector2(0,104)));yield return null;
            weather.hold=3;Set("hoar.whiteoutMet");
            float until=Time.unscaledTime+8;while(weather.Now!=Whiteout.Phase.Closed&&Time.unscaledTime<until)yield return null;
            Assert.AreEqual(Whiteout.Phase.Closed,weather.Now,"the lesson's whiteout never closes");Assert.IsTrue(log.Any(l=>l=="WHITEOUT_CLOSED"));
            Assert.Less(RenderSettings.fogEndDistance,clear*.5f,"a whiteout leaves the view as far as it was");
            Assert.Greater(RenderSettings.fogStartDistance,SightlineAudit.Distance,"a whiteout fogs the party itself");
            yield return null;Assert.Less(lead.Health.Pace,.95f,"out in the whiteout the party walks at full pace");
            Put(lead,Ground(new Vector2(-30,104)));yield return new WaitForSecondsRealtime(.4f);
            Assert.IsTrue(weather.Sheltered(lead.transform.position),"vent pocket one isn't shelter");Assert.AreEqual(1,lead.Health.Pace,.01f,"in the vent pocket the party is still slowed");
            until=Time.unscaledTime+12;while(weather.Now!=Whiteout.Phase.Calm&&Time.unscaledTime<until)yield return null;
            Assert.IsTrue(log.Any(l=>l=="WHITEOUT_LIFTED"),"the whiteout never lifts");Assert.IsTrue(Flag("hoar.whiteoutLifted"),"the lesson's whiteout lifting doesn't say so (Idra waits on it)");
            Assert.AreEqual(clear,RenderSettings.fogEndDistance,.5f,"the view doesn't come back after the whiteout");
            // The second closes as the party walks into the stalkers' field.
            log.Clear();Put(lead,Ground(new Vector2(6,118)));
            until=Time.unscaledTime+3;while(weather.Now==Whiteout.Phase.Calm&&Time.unscaledTime<until)yield return null;
            Assert.AreEqual(Whiteout.Phase.Warning,weather.Now,"walking into the stalkers' field brings no warning of a whiteout");Assert.IsTrue(log.Any(l=>l=="WHITEOUT_WARNING"));
        }

        // ------------------------------------------------------------------ what the script expects of the world
        [UnityTest] public IEnumerator TheTalksRigsAndStraysSetTheFlagsTheScriptExpects()
        {
            yield return Zone();
            // (node, flag, needs, report): the manifest's section 2, Lastlight's contract and the main quest's steps.
            foreach(var (node,flag,needs,report) in new[]{("SelaReachGate","hoar.reachEntered","",""),("IdraHerd","hoar.herdMet","",""),("IdraWhiteout","hoar.whiteoutMet","",""),
                ("VessaLoudhailer","hoar.vessaHeard","",""),("IdraDigGalleries","hoar.ranksSeen","",""),("VessaSecondLevel","hoar.offerHeard","",""),("SelaMusterSlate","hoar.slateRead","",""),
                ("IdraJoins","hoar.idraJoined","","BottomGallery"),("IdraMarshalWakes","hoar.marshalSeen","hoar.idraJoined","")})
            {
                var point=Point(p=>p.dialogueNode==node,node);Assert.IsTrue(point.startsOnCrossing&&point.TryGetComponent<BoxCollider>(out var box)&&box.isTrigger,node+" isn't a crossing");
                Assert.AreEqual(flag,point.flag,node+" sets the wrong flag");Assert.AreEqual(needs,point.requiredFlag??"",node+" waits on the wrong flag");Assert.AreEqual(report,point.report??"",node+" reports the wrong thing");
            }
            Assert.AreEqual("Door",Point(p=>p.dialogueNode=="IdraMarshalWakes","the Marshal's door").saveSpawn,"the Marshal's door doesn't save at the checkpoint");
            Assert.IsNotNull(Point(p=>p.dialogueNode=="IdraMarshalWakes","the Marshal's door").previewEncounter,"the Marshal's door talk can start the fight under it");
            var passed=Point(p=>p.flag=="hoar.herdPassed","past the herd");Assert.IsTrue(passed.startsOnCrossing,"walking through the herd isn't noticed");
            // The gap: offered once the rank it leaves is down, plays Sela's talk, and jumps.
            var gap=Point(p=>p.dialogueNode=="SelaDigShortcut","the gap");Assert.AreEqual(("Take the gap down","hoar.gapTaken","clear.Dig_Rank"),(gap.prompt,gap.flag,gap.requiredFlag));
            var leap=gap.GetComponent<LeapDown>();Assert.IsNotNull(leap,"the gap goes nowhere");Assert.AreEqual(("hoar.gapTaken","Gap"),(leap.flag,leap.spawn));
            Assert.IsTrue(All<SpawnPoint>().Any(s=>s.id=="Gap"),"there is nowhere for the gap to land");
            // The rigs: three, each a valve that sets its flag and a wheel that answers it.
            for(int i=1;i<=3;i++){var valve=Point(p=>p.flag=="hoar.rig"+i,"rig "+i);Assert.AreEqual("Run the thaw rig",valve.prompt);Assert.IsTrue(All<FlowSwitch>().Any(f=>f.flag=="hoar.rig"+i&&f.point==valve),"rig "+i+"'s wheel doesn't answer its valve");}
            // The strays: three, each offered only at Count Them Twice's first step, each its own flag.
            var strays=All<DiscoveryPoint>().Where(p=>p.report=="StrayBeast").ToArray();Assert.AreEqual(3,strays.Length,"the Reach doesn't hold Hask's three strays");
            Assert.IsTrue(strays.All(s=>s.requiredQuest=="StrayBeasts"&&s.requiredStep==0&&s.prompt=="Walk the stray in"&&s.situation=="stray"),"a stray is offered at the wrong step or in the wrong words");
            Assert.AreEqual(3,strays.Select(s=>s.flag).Distinct().Count(),"two strays share a flag");Assert.IsTrue(strays.All(s=>s.GetComponentInParent<LeavesAfter>()!=null),"a stray walked in stays where it was found");
            // Vessa's loudhailer and Idra on the radio.
            foreach(var (speaker,situation,needs) in new[]{("Idra","hail.reach",""),("Vessa","hail.rank1","hoar.ranksSeen"),("Vessa","hail.rank2","hoar.rig2"),("Vessa","hail.excavator","hoar.offerHeard")})
            {var hail=All<RadioHail>().SingleOrDefault(h=>h.speaker==speaker&&h.situations.Contains(situation));Assert.IsNotNull(hail,"nobody says "+situation);Assert.AreEqual(needs,string.Join(",",hail.needs),situation+" waits on the wrong flag");}
            var after=All<AfterTalk>().SingleOrDefault(a=>a.node=="IdraMarshalSleeps");Assert.IsNotNull(after,"nothing plays IdraMarshalSleeps after the Marshal");Assert.AreEqual(("bossdown.HostMarshal","hoar.marshalAsleep"),(after.flag,after.heardFlag));
            var winch=All<WarpBeacon>().SingleOrDefault(w=>w.prompt=="Ride the winch up — Lastlight");Assert.IsNotNull(winch,"there is no winch up");Assert.AreEqual(("Lastlight","Reach","hoar.marshalAsleep"),(winch.scene,winch.spawn,winch.requiredFlag));
            var home=All<WarpBeacon>().SingleOrDefault(w=>w.prompt=="Reach gate — Lastlight");Assert.IsNotNull(home,"there is no way home");Assert.AreEqual(("Lastlight","Reach",""),(home.scene,home.spawn,home.requiredFlag??""));
            foreach(var id in new[]{"hoar.reach.sledge","hoar.dig.tools","hoar.dig.third"})Assert.IsTrue(All<LootCache>().Any(c=>c.cacheId==id),"the cache "+id+" is missing");
            // What the combat engineer sized the chapter's experience on: four stalkers and six mote swarms on the Reach; three ranks, three excavators, six hounds and four swarms in the dig.
            int Count(string enemy,bool reach)=>All<EncounterVolume>().Where(e=>e.encounterId!="HostMarshal"&&e.encounterId.StartsWith(reach?"Reach":"Dig")).SelectMany(e=>e.spawners).Where(s=>s.definition!=null&&s.definition.id==enemy).Sum(s=>s.count);
            Assert.AreEqual((4,6),(Count("RimeStalker",true),Count("FrostMotes",true)),"the Reach's fights aren't the ones the chapter is sized for");
            Assert.AreEqual((9,3,6,4),(Count("ThawedGuard",false),Count("MusterExcavator",false),Count("BridleHound",false),Count("FrostMotes",false)),"the dig's fights aren't the ones the chapter is sized for");
        }

        [UnityTest] public IEnumerator TheGapJumpsThePartyToTheBottomGalleryAndASaveWithItJumpsNothing()
        {
            Set("hoar.reachEntered","hoar.herdMet","hoar.whiteoutMet","hoar.vessaHeard","hoar.ranksSeen","hoar.rig1","hoar.offerHeard","hoar.rig2","clear.Dig_Rank");
            yield return Zone();
            var gap=Point(p=>p.dialogueNode=="SelaDigShortcut","the gap");Assert.IsTrue(gap.Available,"the gap isn't offered once the rank is down");
            Set("hoar.gapTaken");float until=Time.unscaledTime+2;while(!SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;
            Assert.IsTrue(log.Any(l=>l=="LEAP_DOWN Gap"),"taking the gap jumps nowhere");
            until=Time.unscaledTime+40;while(SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;Hold();yield return new WaitForSecondsRealtime(.6f);
            Assert.Less(Vector2.Distance(new Vector2(Hero.x,Hero.z),new Vector2(168.5f,210)),4,"the gap doesn't land the party in the bottom gallery's mouth");
            Assert.AreEqual("Gap",GameServices.Current.Saves.Load("autosave").spawn,"landing from the gap isn't saved");
            log.Clear();yield return Zone("Arrival");yield return new WaitForSecondsRealtime(1);
            Assert.IsFalse(log.Any(l=>l.StartsWith("LEAP_DOWN")),"a save with the gap taken jumps again");Assert.AreEqual(Host,SceneManager.GetActiveScene().name);
        }

        [UnityTest] public IEnumerator IdraWalksAheadToHerNextTalkAndIsGoneOnceSheJoins()
        {
            yield return Zone();
            var guide=Object.FindFirstObjectByType<Guide>();Assert.IsNotNull(guide,"Idra isn't ahead of the party on the Reach");Assert.IsNull(guide.GetComponentInChildren<Npc>(),"Idra ahead can be talked to");
            yield return null;var first=guide.transform.position;Assert.Less(Vector2.Distance(new Vector2(first.x,first.z),new Vector2(0,57)),12,"Idra doesn't wait by the herd for her first talk on the Reach");
            Set("hoar.herdMet");float until=Time.unscaledTime+3;while(!log.Any(l=>l.StartsWith("GUIDE_LEG"))&&Time.unscaledTime<until)yield return null;
            Assert.IsTrue(log.Any(l=>l.StartsWith("GUIDE_LEG")),"Idra doesn't walk on once her talk at the herd is had");
            // A load stands her where the last talk reached sends her: at the bottom gallery's mouth, by either way down.
            Set("hoar.whiteoutMet","hoar.whiteoutLifted","hoar.vessaHeard","hoar.ranksSeen","hoar.rig1","hoar.offerHeard","clear.Dig_Rank","hoar.gapTaken");yield return Zone("Gap");yield return null;
            guide=Object.FindFirstObjectByType<Guide>();var there=guide.transform.position;Assert.Less(Vector2.Distance(new Vector2(there.x,there.z),new Vector2(178,224)),3,"after the gap Idra isn't waiting in the bottom gallery's mouth");
            Set("hoar.idraJoined");yield return null;yield return null;Assert.IsFalse(guide.gameObject.activeInHierarchy,"Idra is still walking ahead once she has joined");
        }

        /// <summary>Where Idra waits out the first whiteout is beside the party in vent pocket one, never where the party stops: her body has no collider, so a lead walked to the
        /// pocket's middle stood inside her for the whole whiteout (s35/097, her spot half a metre from the route's). Every walker stops at the route's point, so that point is
        /// what she has to stand clear of, by two bodies and a partner's room. Shown on her old spot first.</summary>
        [UnityTest] public IEnumerator IdraWaitsOutTheFirstWhiteoutBesideThePartyAndNeverWhereItStops()
        {
            var route=JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes","chapter-35-frozen-host.json")));
            var stop=route.steps.Last(s=>s.name.StartsWith("into the first vent pocket")).point;var stops=new Vector2(stop.x,stop.z);
            const float Clear=2.5f;
            bool Crowds(Vector2 idra)=>Vector2.Distance(idra,stops)<Clear;
            Assert.IsTrue(Crowds(new Vector2(-26,104.5f)),"the check doesn't see the spot she had, where the lead stood in her");
            yield return Zone();
            var guide=Object.FindFirstObjectByType<Guide>();Assert.IsNotNull(guide,"Idra isn't ahead of the party on the Reach");
            var leg=guide.legs.Single(l=>l.flag=="hoar.whiteoutMet");var end=leg.path[^1];var spot=new Vector2(end.x,end.z);
            Assert.IsFalse(Crowds(spot),$"Idra waits out the whiteout at {spot}, {Vector2.Distance(spot,stops):0.00} m from where the party stops ({stops}); she needs {Clear}");
            Assert.Less(Vector2.Distance(spot,new Vector2(-30,104)),5.5f,"Idra doesn't wait out the whiteout inside vent pocket one");
        }

        // ------------------------------------------------------------------ the bottom gallery and its door
        [UnityTest] public IEnumerator TheBottomGalleryIsBuiltToTheBossContract()
        {
            Set("hoar.rig1","hoar.rig2","hoar.rig3","hoar.slateRead","hoar.idraJoined");
            yield return Zone("Door");
            var duel=All<EncounterVolume>().Single(e=>e.encounterId=="HostMarshal");
            var post=duel.spawners.Single();var spawn=Bearing(0,10);
            Assert.AreEqual(spawn.x,post.transform.position.x,.1f);Assert.AreEqual(spawn.y,post.transform.position.z,.1f,"the Marshal's spawner isn't at bearing 0, r 10");
            Assert.IsTrue(post.faceAsPlaced);Assert.AreEqual(250,post.transform.eulerAngles.y,6,"the Marshal doesn't face the door");
            Assert.AreEqual(("hoar.marshalSeen","Door",1),(duel.requiredFlag,duel.checkpointSpawn,duel.membranes.Length),"the door isn't the contract's");
            var box=duel.GetComponent<BoxCollider>();Assert.That(box.size.x,Is.InRange(34f,38f));Assert.That(box.size.z,Is.InRange(34f,38f));
            Assert.IsNotNull(duel.previewPoint,"the Marshal isn't shown from the door");Assert.GreaterOrEqual(duel.previewRadius,Vector3.Distance(duel.previewPoint.position,post.transform.position)-.5f,"the preview's radius doesn't take in the Marshal");
            // The floor: near level to r 17 (no step over .3 inside r 16), nothing solid inside r 12.5 but the couplings, walkable from r 9 to r 17 for the frost ring.
            float floor=Ground(C).y;var couplings=All<ThawCoupling>();
            for(float x=-17;x<=17;x+=1)for(float z=-17;z<=17;z+=1)
            {
                var off=new Vector2(x,z);if(off.magnitude>17)continue;var p=C+off;var at=Ground(p);
                if(couplings.Any(c=>Vector2.Distance(new Vector2(c.transform.position.x,c.transform.position.z),p)<1.6f))continue;
                Assert.AreEqual(floor,at.y,.3f,"the bottom gallery's floor isn't near level at "+p);
                if(off.magnitude<12.5f)
                {
                    // The Marshal stands shown at its spawner before the fight: it is the one thing allowed in there.
                    var hits=Physics.RaycastAll(at+Vector3.up*6,Vector3.down,8,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<Health>()==null&&h.collider.attachedRigidbody==null).OrderBy(h=>h.distance).ToArray();
                    Assert.IsNotEmpty(hits);Assert.AreEqual(at.y,hits[0].point.y,.05f,"something solid stands on the floor at "+p+": "+hits[0].collider.name);
                }
                if(off.magnitude>=9)Assert.IsTrue(NavMesh.SamplePosition(at,out _,.6f,NavMesh.AllAreas),"the frost ring's floor isn't walkable at "+p);
            }
            // The couplings: three at r 13.5 on 330, 30 and 90, numbered 1 to 3, each a break target that answers to the Marshal, with its rig, its third of the wall and its rank.
            Assert.AreEqual(3,couplings.Length,"the bottom gallery doesn't have three couplings");
            foreach(var (index,bearing) in new[]{(1,330f),(2,30f),(3,90f)})
            {
                var c=couplings.Single(k=>k.index==index);var at=Bearing(bearing,13.5f);
                Assert.Less(Vector2.Distance(new Vector2(c.transform.position.x,c.transform.position.z),at),.1f,"coupling "+index+" isn't at bearing "+bearing+", r 13.5");
                Assert.AreEqual(("HostMarshal","hoar.coupling"+index,"Thaw coupling"),(c.settledBy,c.defeatedFlag,c.GetComponent<Health>().id));
                var sphere=c.GetComponent<SphereCollider>();Assert.IsTrue(sphere.isTrigger&&Mathf.Approximately(sphere.radius,1.3f)&&Mathf.Approximately(sphere.center.y,1.5f),"coupling "+index+"'s hit shape isn't the contract's");
                Assert.AreEqual(c.GetComponent<Health>(),c.GetComponent<Hurtbox>().owner);
                Assert.IsTrue(c.whole!=null&&c.broken!=null&&c.thawing!=null&&c.refrozen!=null,"coupling "+index+" is missing its rig or its wall");
                Assert.IsTrue(c.whole.activeInHierarchy&&c.thawing.activeInHierarchy&&!c.broken.activeInHierarchy&&!c.refrozen.activeInHierarchy,"coupling "+index+" doesn't load running");
                Assert.IsNotNull(c.whole.GetComponentInChildren<Light>(),"coupling "+index+" has no amber light");
                Assert.AreEqual(3,c.ranks.Length,"coupling "+index+" has no rank to call out");
                foreach(var (rank,offset) in c.ranks.Zip(new[]{-16f,0,16f},(r,o)=>(r,o)))
                {
                    var want=Bearing(bearing+offset,16);Assert.Less(Vector2.Distance(new Vector2(rank.position.x,rank.position.z),want),.1f,"coupling "+index+"'s rank stands off its place");
                    Assert.IsTrue(NavMesh.SamplePosition(rank.position,out var on,.6f,NavMesh.AllAreas),"coupling "+index+"'s rank steps out where there is no floor");
                    Assert.IsFalse(Physics.CheckSphere(rank.position+Vector3.up*1.2f,1.1f,~0,QueryTriggerInteraction.Ignore),"coupling "+index+"'s rank has no open floor round it");
                }
            }
            // The checkpoint: outside the seal, on the paths, within 60 m, with the free cradle beside it.
            var door=All<SpawnPoint>().Single(s=>s.id=="Door");var seal=duel.membranes[0];
            Assert.Less(door.transform.position.x,seal.transform.position.x-1,"the checkpoint is inside the seal");Assert.Less(Vector3.Distance(door.transform.position,new Vector3(C.x,Bottom,C.y)),60);
            Assert.IsTrue(NavMesh.SamplePosition(door.transform.position,out _,1.5f,NavMesh.AllAreas),"the checkpoint isn't on the paths");
            var bay=All<RepairBay>().OrderBy(r=>Vector3.Distance(r.transform.position,door.transform.position)).First();
            Assert.IsTrue(bay.free&&bay.saveSpawn=="Door"&&Vector3.Distance(bay.transform.position,door.transform.position)<20,"there is no free cradle at the door");
            Assert.IsTrue(seal.Open,"the seal is shut before the fight");
        }

        [UnityTest] public IEnumerator TheBottomGalleryDoorIsACheckpointAndShutsBehindTheParty()
        {
            Set("hoar.rig1","hoar.rig2","hoar.rig3","hoar.slateRead","hoar.idraJoined","hoar.marshalSeen");
            yield return Zone("Door");
            var duel=All<EncounterVolume>().Single(e=>e.encounterId=="HostMarshal");
            GameServices.Current.State.spawn="Arrival";duel.Begin();yield return null;
            Assert.AreEqual("Door",GameServices.Current.State.spawn,"starting the fight doesn't make the door the checkpoint");
            Assert.AreEqual("Door",GameServices.Current.Saves.Load("autosave").spawn,"starting the fight doesn't save at the door");
            Assert.IsFalse(duel.membranes[0].Open,"the seal doesn't shut behind the party");
        }

        [UnityTest] public IEnumerator AGalleryWonLoadsAsleepWithTheColdBehindTheParty()
        {
            // Before: the Muster's amber in the upper gallery is warm, and the winch refuses.
            Set("hoar.rig1","hoar.rig2","hoar.rig3","hoar.idraJoined","hoar.marshalSeen");
            yield return Zone("Door");
            var warm=All<IceThaw>().Where(t=>string.IsNullOrEmpty(t.flag)).ToArray();Assert.GreaterOrEqual(warm.Length,2,"the Muster's thaw isn't in the dig");Assert.IsTrue(warm.All(t=>t.Heat>.99f),"the Muster's thaw loads blue");
            Assert.IsTrue(All<Whereabouts>().Where(w=>w.from=="bossdown.HostMarshal").All(w=>w.targets.All(t=>!t.activeInHierarchy)),"the Marshal kneels asleep before it has been fought");
            Set("bossdown.HostMarshal","clear.HostMarshal","hoar.coupling1","hoar.coupling2","hoar.coupling3","hoar.marshalAsleep");
            yield return Zone("Door");yield return new WaitForSecondsRealtime(.4f);
            Assert.IsTrue(All<IceThaw>().Where(t=>string.IsNullOrEmpty(t.flag)).All(t=>t.Refrozen&&t.Heat<.01f),"the dig's amber doesn't go blue once the Marshal sleeps");
            foreach(var c in All<ThawCoupling>())Assert.IsTrue(!c.Running&&c.broken.activeInHierarchy&&c.refrozen.activeInHierarchy&&!c.thawing.activeInHierarchy,"coupling "+c.index+" loads running in a won gallery");
            var kneeling=All<Whereabouts>().Where(w=>w.from=="bossdown.HostMarshal").SelectMany(w=>w.targets).ToArray();
            Assert.IsNotEmpty(kneeling,"nothing of the Marshal is left in a won gallery");Assert.IsTrue(kneeling.All(t=>t.activeInHierarchy),"the Marshal isn't kneeling in its gallery on a later visit");
            Assert.IsTrue(All<WarpBeacon>().Single(w=>w.prompt=="Ride the winch up — Lastlight").Available,"the winch won't carry the party up once the Marshal sleeps");
        }

        // ------------------------------------------------------------------ the camera
        [UnityTest] public IEnumerator NoWallOrTallPieceStandsBetweenTheCameraAndAFight()
        {
            Set("hoar.rig1","hoar.rig2","hoar.rig3");
            yield return Zone("Door");
            var toCamera=-(Quaternion.Euler(SightlineAudit.Pitch,SightlineAudit.Yaw,0)*Vector3.forward);var hidden=new List<string>();int samples=0;
            foreach(var fight in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))
            {
                var box=fight.GetComponent<BoxCollider>().bounds;
                for(float x=box.min.x+1;x<box.max.x;x+=2)for(float z=box.min.z+1;z<box.max.z;z+=2)
                {
                    // A point of the fight's own floor: navigation within a hand of the ground, at the fight's level (the paths baked on the glacier's tops are no one's floor).
                    if(!Physics.Raycast(new Vector3(x,box.max.y+8,z),Vector3.down,out var floor,40,~0,QueryTriggerInteraction.Ignore)||!NavMesh.SamplePosition(floor.point,out var on,.4f,NavMesh.AllAreas))continue;
                    if(Mathf.Abs(on.position.y-(box.center.y-1.5f))>1.6f)continue;
                    samples++;var chest=on.position+Vector3.up*SightlineAudit.Chest;
                    foreach(var hit in Physics.RaycastAll(chest,toCamera,SightlineAudit.Distance-SightlineAudit.NearIgnore,~0,QueryTriggerInteraction.Ignore))
                    {
                        if(hit.collider.GetComponentInParent<Health>()!=null||hit.collider.GetComponentInParent<CharacterController>()!=null||hit.collider.attachedRigidbody!=null)continue;
                        // The house rule (TallOverAFight): nothing over three metres that is broad enough to hide a body. A post or a lamp is neither.
                        var size=hit.collider.bounds.size;if(hit.collider.bounds.max.y-on.position.y<3||size.x<1.6f&&size.z<1.6f)continue;
                        hidden.Add(fight.encounterId+" at ("+x.ToString("0")+", "+z.ToString("0")+") behind "+hit.collider.name);break;
                    }
                }
            }
            Assert.Greater(samples,400,"fixture: the fights' floors were sampled");
            Assert.IsEmpty(hidden,hidden.Count+" of "+samples+" fight floor points are hidden from the camera: "+string.Join("; ",hidden.Take(12)));
        }
    }
}
