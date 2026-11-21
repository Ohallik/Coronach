using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 9's Coronach and launch (docs/maps/DecksLeaving.md, "The Coronach" and "The launch"). The first half builds its parts on a bare floor in the proving yard, as the world
    /// mechanics' tests do, and plays the script's own nodes through them: a lit lantern meaning somebody still has something to say, the extras in the same stop with no gap, nobody set out
    /// for a story not finished, a hero resting standing at their place, the walk's music and its silence, nothing moving on by itself, the launch from the close to the next zone's load.
    /// The second half is the walk as a player meets it in the built scene, DecksLeaving: the call, every door reached and heard in any order, the count, the stories, nobody on a way, the
    /// panel and its missing arrow, a save in the middle, ten minutes of talking to nobody, and the launch into the Broken Ring. Stills are left in Builds/quality/focused/coronach.</summary>
    public sealed class CoronachTests
    {
        const string Called="home.coronachCalled",Walked="home.coronachWalked",Launched="home.launched",DoorsHeard="coronach.doorsHeard",LastHeard="coronach.lastHeard";
        static readonly string[] Needed={"coronach.mira","coronach.orrin","coronach.hal","coronach.neve","coronach.oda","coronach.jun","coronach.ilo"};
        readonly List<string> log=new(),cues=new(),barks=new(),talks=new();NavMeshDataInstance paths;
        void Heard(string text,string stack,LogType type){if(text.StartsWith("CORONACH_")||text.StartsWith("LAUNCH_")||text.StartsWith("MUSIC_CUE")||text.StartsWith("ZONE_ENTER")||text.StartsWith("GUIDE_"))log.Add(text);}
        void Cue(string key,Transform emitter)=>cues.Add(key);
        void Spoke(string who,string line)=>barks.Add(who+": "+line);
        void Talked(string node,string speaker,System.Action<bool> done)=>talks.Add(node);
        void Looked(DiscoveryPoint point){if(!string.IsNullOrEmpty(point.dialogueNode))talks.Add(point.dialogueNode);}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter9");log.Clear();cues.Clear();barks.Clear();talks.Clear();
            Application.logMessageReceived+=Heard;AudioManager.CueStarted+=Cue;BarkService.Spoken+=Spoke;Npc.TalkRequested+=Talked;Npc.FollowRequested+=Talked;DiscoveryPoint.DialogueRequested+=Looked;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;AudioManager.CueStarted-=Cue;BarkService.Spoken-=Spoke;Npc.TalkRequested-=Talked;Npc.FollowRequested-=Talked;DiscoveryPoint.DialogueRequested-=Looked;
            if(paths.valid)paths.Remove();
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            Time.timeScale=1;GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.FirstOrDefault(m=>m!=Lead);
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Seconds(float s){float until=Time.time+s;while(Time.time<until)yield return null;}
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Zone(string zone,string spawn="Arrival",bool saved=false)
        {
            if(saved)SceneFlow.Current.LoadSavedZone(zone,spawn);else SceneFlow.Current.LoadZone(zone,spawn);
            float deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.6f);
        }
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=at;body.enabled=true;actor.target=null;actor.TargetLocked=false;Physics.SyncTransforms();
        }
        static IEnumerator Settle(float most=40)
        {
            float deadline=Time.unscaledTime+most;
            while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(DialogueSystem.Current.Running,"a conversation never ended");
        }
        /// <summary>Stops at a place: the lead walked up to within its reach, its prompt pressed, every line read through, every extra after it.</summary>
        static IEnumerator Hear(CoronachDoor door,Vector3? from=null)
        {
            DialogueSystem.Current.AutoAdvance=true;
            if(from is Vector3 stand)Put(Lead,stand);
            float deadline=Time.unscaledTime+25;while(!door.Offered&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(door.Offered,$"{door.name} offers nothing to stop for");
            Assert.LessOrEqual(Flat(Lead.transform.position,door.point.transform.position),door.point.range,$"fixture: the lead isn't in {door.name}'s reach");
            door.point.Interact();
            deadline=Time.unscaledTime+4;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Running,$"{door.name}'s line never began");
            // The door's line and every extra after it: done once nothing has been said for a moment.
            for(float quiet=0;quiet<.6f;){yield return null;quiet=DialogueSystem.Current.Running?0:quiet+Time.unscaledDeltaTime;if(Time.unscaledTime>deadline+60)break;}
            Assert.IsFalse(DialogueSystem.Current.Running,$"{door.name}'s stop never ended");
        }

        // ------------------------------------------------------------------ the yard: a bare floor and the parts on it
        const float X=230;
        static Vector3 At(float x,float z)=>new(X+x,0,z);
        IEnumerator Yard()
        {
            yield return Zone("Arena_Ground");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Coronach fixture floor";floor.transform.position=new Vector3(X,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();
            var sources=new List<NavMeshBuildSource>{new(){shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.Translate(new Vector3(X,-.5f,0)),size=new Vector3(140,1,140),area=0}};
            var built=NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),sources,new Bounds(new Vector3(X,0,0),new Vector3(150,10,150)),Vector3.zero,Quaternion.identity);
            Assert.IsNotNull(built,"fixture: no paths over the fixture floor");paths=NavMesh.AddNavMeshData(built);
            Put(Partner,At(-40,-40));Put(Lead,At(0,-30));yield return Frames(3);
        }
        static Material glow;
        static Material Glow(){if(glow==null){glow=new Material(Shader.Find("Lattice/Toon"));glow.SetColor("_EmissionColor",new Color(2.6f,1.62f,.66f));}return glow;}
        /// <summary>A place as the builder makes one: a point at what is looked at, a body standing still facing it, a lantern at its feet.</summary>
        static CoronachDoor Place(string name,Vector3 stands,Vector3 looks,string flag,string node,string speaker,bool person=true,bool lamp=true)
        {
            var holder=new GameObject(name);holder.SetActive(false);holder.transform.position=stands;
            var door=holder.AddComponent<CoronachDoor>();door.speaker=speaker;door.node=node;
            var at=new GameObject(name+" (the point)");at.transform.SetParent(holder.transform,false);at.transform.position=looks;
            var point=at.AddComponent<DiscoveryPoint>();point.prompt="Stop and listen";point.flag=flag;point.report="";point.situation="";point.dialogueNode=node;point.dialogueSpeaker=speaker;
            point.requiredQuest="";point.requiredFlag="";point.range=3;door.point=point;
            if(person){var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name=name+", standing";body.transform.SetParent(holder.transform,false);body.transform.position=stands+Vector3.up;Object.Destroy(body.GetComponent<Collider>());door.body=body;}
            if(lamp)
            {
                var lantern=new GameObject(name+"'s lantern");lantern.transform.SetParent(holder.transform,false);lantern.transform.position=stands+new Vector3(-.4f,0,-.3f);
                var pane=GameObject.CreatePrimitive(PrimitiveType.Cube);pane.name="Glass";Object.Destroy(pane.GetComponent<Collider>());pane.transform.SetParent(lantern.transform,false);pane.transform.localScale=Vector3.one*.2f;pane.GetComponent<Renderer>().sharedMaterial=Glow();
                var light=new GameObject("Lantern light",typeof(Light)).GetComponent<Light>();light.transform.SetParent(lantern.transform,false);light.type=LightType.Point;light.intensity=1.8f;light.range=3.2f;
                door.lamp=lantern;door.lampLight=light;door.glass=new[]{pane.GetComponent<Renderer>()};
            }
            holder.SetActive(true);return door;
        }
        static GameObject Thing(string name){var thing=GameObject.CreatePrimitive(PrimitiveType.Sphere);thing.name=name;Object.Destroy(thing.GetComponent<Collider>());return thing;}
        static CoronachWalk Walk(string music,params string[] doors)
        {
            var walk=new GameObject("Coronach walk fixture").AddComponent<CoronachWalk>();walk.music=music;walk.doors=doors;
            var all=walk.gameObject.AddComponent<FlagAll>();all.flags=doors;all.result=DoorsHeard;return walk;
        }

        // ================================================================== the parts, in the yard
        [UnityTest] public IEnumerator ALitLanternMeansSomebodyHereStillHasSomethingToSay()
        {
            yield return Yard();
            var door=Place("Mira, at pitch nine",At(0,0),At(0,1.4f),"coronach.mira","MiraStallDoor","Mira");
            var shutter=new GameObject("A shutter, down");shutter.SetActive(false);door.shuts=new[]{shutter};var awning=new GameObject("An awning");door.takes=new[]{awning};
            yield return Frames(3);
            Assert.IsFalse(door.body.activeSelf||door.lamp.activeSelf||door.Offered,"before the Coronach is called somebody stands at the door with a lantern, or offers a line");
            Flag(Called);yield return Seconds(CoronachDoor.LampSeconds+.4f);
            Assert.IsTrue(door.body.activeSelf,"nobody stands at the door once the Coronach is called");
            Assert.IsTrue(door.Lit,"the lantern at a door still to be heard isn't lit");
            Assert.Greater(door.lampLight.intensity,1.6f,"the lantern is lit but gives no light");
            Put(Lead,At(0,-1.2f));yield return Frames(3);
            Assert.IsTrue(door.Offered,"a door still to be heard offers nothing to stop for");
            Assert.AreEqual("Stop and listen",door.point.prompt);
            yield return Hear(door);
            Assert.IsTrue(Has("coronach.mira"),"hearing the door didn't set its flag");
            CollectionAssert.AreEqual(new[]{"MiraStallDoor"},talks,"the door played something other than its own line");
            yield return Seconds(CoronachDoor.LampSeconds+.3f);
            Assert.IsFalse(door.Lit,"the lantern is still lit after the door has been heard: a lit lantern means somebody still has something to say");
            Assert.IsTrue(door.body.activeSelf&&door.lamp.activeSelf,"she went aboard with the party still beside her");
            Assert.IsFalse(door.Offered,"a door heard offers its line again");
            // The party walks on. Nobody goes while any of it is in view, and once nothing is, she has gone aboard and the door is shut behind her.
            bool seenGoing=false;Put(Lead,At(0,-45));
            for(float t=0;t<4;t+=Time.deltaTime){bool was=door.body.activeSelf;yield return null;if(was&&!door.body.activeSelf&&CoronachDoor.Seen(door.body.transform.position))seenGoing=true;}
            Assert.IsFalse(seenGoing,"she vanished in the camera's view");
            Assert.IsTrue(door.Gone,"she never went aboard once the party had gone on");
            Assert.IsFalse(door.body.activeSelf||door.lamp.activeSelf,"she or her lantern is still there once she has gone aboard");
            Assert.IsTrue(shutter.activeSelf,"what she faced isn't shut behind her");Assert.IsFalse(awning.activeSelf,"she didn't take the awning");
            CollectionAssert.Contains(log,"CORONACH_ABOARD Mira, at pitch nine");
        }

        [UnityTest] public IEnumerator AnExtraFollowsInTheSameStopOnlyIfItsStoryWasDoneAndTheBoxNeverGoes()
        {
            yield return Yard();
            Flag("quest.MiraCrystals.complete");
            var door=Place("Mira, at pitch nine",At(0,0),At(0,1.4f),"coronach.mira","MiraStallDoor","Mira");
            var crystals=Thing("Six Ridge Crystals");var halves=new[]{Thing("Half a table").transform,Thing("The other half").transform};foreach(var h in halves)h.gameObject.SetActive(false);
            door.extras=new[]
            {
                new CoronachDoor.Extra{node="MiraDoorCrystals",needs="quest.MiraCrystals.complete",shows=new[]{crystals}},
                new CoronachDoor.Extra{node="MiraDoorTable",needs="quest.SecondInventory.complete",carried=halves,path=new[]{At(-6,4),At(6,4)},pace=.8f},
            };
            Flag(Called);yield return Frames(3);
            Assert.IsTrue(crystals.activeSelf,"the crystals of a story finished aren't there");
            CollectionAssert.AreEqual(new[]{"MiraDoorCrystals"},door.ExtrasDue().ToArray(),"the extras due aren't exactly the finished stories'");
            // Watch the dialogue box from the door's first line to the extra's last: it never closes between them.
            var panel=Object.FindFirstObjectByType<DialoguePanel>();Assert.IsNotNull(panel,"fixture: no dialogue box");
            DialogueSystem.Current.AutoAdvance=true;Put(Lead,At(0,-1.2f));yield return Frames(3);
            door.point.Interact();
            var shown=new List<bool>();float deadline=Time.unscaledTime+40;
            while(Time.unscaledTime<deadline){yield return null;shown.Add(panel.IsVisible);if(talks.Count>=2&&!DialogueSystem.Current.Running&&!panel.IsVisible)break;}
            CollectionAssert.AreEqual(new[]{"MiraStallDoor","MiraDoorCrystals"},talks,"the stop didn't play the door's line and then its one finished story's");
            int first=shown.IndexOf(true),last=shown.LastIndexOf(true);
            Assert.GreaterOrEqual(first,0,"the box never opened");
            int gaps=shown.Skip(first).Take(last-first+1).Count(v=>!v);
            Assert.AreEqual(0,gaps,$"the dialogue box closed for {gaps} frame(s) between the door's line and its extra: it is one stop, not two");
            Assert.IsFalse(halves.Any(h=>h.gameObject.activeSelf),"the table halves came out for a story never finished");
            CollectionAssert.Contains(log,"CORONACH_EXTRA MiraDoorCrystals");CollectionAssert.DoesNotContain(log,"CORONACH_EXTRA MiraDoorTable");
        }

        /// <summary>The shared dialogue's follow-on (<see cref="DialogueSystem.Follow"/>, asked through <see cref="Npc.SayNext"/>): asked while a conversation runs, the next takes the box
        /// over without it closing, the party held throughout; asked with nothing running, refused; behind a conversation cut short, dropped with it. The measure is shown to see a gap
        /// first: the same two nodes said the old way, the second asked once the first has let go, close the box between them.</summary>
        [UnityTest] public IEnumerator AConversationFollowedOnKeepsTheBoxUpWhereTheOldWayClosedIt()
        {
            yield return Yard();
            var panel=Object.FindFirstObjectByType<DialoguePanel>();Assert.IsNotNull(panel,"fixture: no dialogue box");
            var dialogue=DialogueSystem.Current;dialogue.AutoAdvance=true;
            // The old way: the second asked, again each frame, once the first has let go of the dialogue.
            var shown=new List<bool>();var held=new List<bool>();bool firstDone=false,secondAsked=false,secondDone=false;
            Assert.IsTrue(Npc.Say("MiraStallDoor","Mira",_=>firstDone=true),"fixture: nobody hears a talk");
            float deadline=Time.unscaledTime+40;
            while(Time.unscaledTime<deadline)
            {
                yield return null;shown.Add(panel.IsVisible);held.Add(GameServices.Current.Input.Blocked);
                if(firstDone&&!secondAsked){bool refused=false;Npc.Say("MiraDoorCrystals","Mira",c=>{if(!secondAsked)refused=!c;else secondDone=true;});secondAsked=!refused;}
                if(secondDone&&!dialogue.Running&&!panel.IsVisible)break;
            }
            Assert.IsTrue(secondDone,"fixture: the old way's second talk never ran");
            Assert.Greater(Gaps(shown),0,"fixture: the measure sees no gap in the old way, where the box closes between two talks");
            // The new way: the second asked to follow while the first runs.
            shown.Clear();held.Clear();var ends=new List<string>();
            Npc.Say("MiraStallDoor","Mira",c=>ends.Add("door "+c));yield return null;
            Assert.IsTrue(dialogue.Running,"fixture: the door's talk didn't start");
            Assert.IsTrue(Npc.SayNext("MiraDoorCrystals","Mira",c=>ends.Add("extra "+c)),"nobody hears a follow-on");
            Assert.IsEmpty(ends,"the follow-on was refused while a talk was running");
            deadline=Time.unscaledTime+40;
            while(Time.unscaledTime<deadline){yield return null;shown.Add(panel.IsVisible);held.Add(GameServices.Current.Input.Blocked);if(ends.Count>=2&&!dialogue.Running&&!panel.IsVisible)break;}
            CollectionAssert.AreEqual(new[]{"door True","extra True"},ends,"the two talks didn't both run to their end, in order");
            Assert.AreEqual(0,Gaps(shown),"the box closed between a talk and the one asked to follow it");
            int from=shown.IndexOf(true),to=shown.LastIndexOf(true);
            Assert.IsTrue(held.Skip(from).Take(to-from+1).All(h=>h),"the party was let go between a talk and the one following it");
            Assert.IsFalse(GameServices.Current.Input.Blocked,"the party is still held once both have ended");
            // With nothing running, refused: a follow-on starts nothing of its own.
            bool? alone=null;Npc.SayNext("MiraDoorCrystals","Mira",c=>alone=c);yield return Frames(3);
            Assert.AreEqual(false,alone,"a follow-on asked with nothing running wasn't refused");Assert.IsFalse(dialogue.Running||panel.IsVisible,"a follow-on started a talk of its own");
            // Behind a talk cut short, it goes with it.
            bool? after=null;Npc.Say("MiraStallDoor","Mira");yield return null;Npc.SayNext("MiraDoorCrystals","Mira",c=>after=c);yield return Frames(2);
            dialogue.Cancel();deadline=Time.unscaledTime+5;while((dialogue.Running||panel.IsVisible)&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual(false,after,"a follow-on played after the talk before it was cut short");
            Assert.IsFalse(dialogue.Running||panel.IsVisible,"the box stayed up after a cut-short talk's follow-on was dropped");
            Assert.IsFalse(GameServices.Current.Input.Blocked,"the party is still held after a cut-short talk");
        }
        static int Gaps(List<bool> shown){int first=shown.IndexOf(true),last=shown.LastIndexOf(true);return first<0?-1:shown.Skip(first).Take(last-first+1).Count(v=>!v);}

        [UnityTest] public IEnumerator SomeoneHereOnlyForTheirStoriesIsNotThereUnlessOneWasDone()
        {
            yield return Yard();
            var ishen=Place("Ishen, taking it down",At(-4,0),At(-3.3f,.4f),"coronach.ishen","","Ishen",lamp:false);
            ishen.extras=new[]{new CoronachDoor.Extra{node="IshenDoorRecord",needs="quest.IshensRecord.complete"},new CoronachDoor.Extra{node="IshenDoorQuill",needs="quest.FourthPump.complete"}};
            Flag("quest.SchoolBell.complete");
            var tray=Thing("A tray nobody has touched");
            var dov=Place("Dov, with a tray",At(8,0),At(8.7f,.3f),"coronach.dov","","Dov",lamp:false);
            dov.extras=new[]{new CoronachDoor.Extra{node="DovDoorEel",needs="quest.SmokehouseOrder.complete",shows=new[]{tray}},
                new CoronachDoor.Extra{node="DovDoorBell",needs="quest.SchoolBell.complete",sound="school_bell",volume=.3f,soundReach=7}};
            Flag(Called);Put(Lead,At(8,-20));yield return Frames(4);
            Assert.IsTrue(ishen.Nobody,"someone whose stories were none of them finished is counted as here");
            Assert.IsFalse(ishen.body.activeSelf||ishen.Offered,"Ishen stands in the walk, or offers a line, with none of his stories finished");
            Assert.IsTrue(dov.body.activeSelf,"Dov isn't there though the school bell's story was finished");
            Assert.AreEqual("DovDoorBell",dov.point.dialogueNode,"someone here only for one story doesn't say that story's line first");
            Assert.IsFalse(tray.activeSelf,"the eel's tray stands there for a story never finished");
            Assert.AreEqual(0,cues.Count(c=>c=="school_bell"),"the bell rang before the party came near");
            Put(Lead,At(8,-6));yield return Frames(4);
            Assert.AreEqual(1,cues.Count(c=>c=="school_bell"),"the school raft's bell didn't ring once as the party came near Dov");
            Put(Lead,At(8,-20));yield return Frames(4);Put(Lead,At(8,-5));yield return Frames(4);
            Assert.AreEqual(1,cues.Count(c=>c=="school_bell"),"the bell rang more than once: one stroke, they managed the once");
            yield return Hear(dov,At(8,-1.3f));
            CollectionAssert.AreEqual(new[]{"DovDoorBell"},talks,"Dov said more than the one story finished");
        }

        [UnityTest] public IEnumerator AHeroRestingStandsAtTheirPlaceAndAHeroOutDoesNot()
        {
            yield return Yard();
            Assert.IsTrue(Deployment.Choosing(State),"fixture: chapter 9 arrives as three, two out");
            Assert.IsTrue(Deployment.Set(State,new[]{"Taren","Idra"}),"fixture: Taren and Idra out");PartyField.Reform(PartyController.Current);yield return Frames(3);Put(Lead,At(0,-30));
            var sela=Place("Sela, at gate six",At(0,0),At(0,1.6f),"coronach.sela","SelaGateSix","Sela");sela.hero="Sela";sela.opensWith=DoorsHeard;sela.stays=true;
            Flag(Called);yield return Frames(4);
            Assert.IsTrue(sela.body.activeSelf,"Sela is resting and isn't standing at her own place in the street");
            Assert.IsFalse(sela.lamp.activeSelf||sela.Offered,"a hero's last word is lit or offered before the seven doors are heard");
            Flag(DoorsHeard);yield return Seconds(CoronachDoor.LampSeconds+.3f);
            Assert.IsTrue(sela.Lit,"Sela's lantern isn't lit once the seven doors are heard");
            Put(Lead,At(0,-1.3f));yield return Frames(3);
            Assert.IsTrue(sela.Offered,"Sela's last word isn't offered");
            // Brought out, she leaves her place at once: there is never a second Sela.
            Assert.IsTrue(Deployment.Set(State,new[]{"Taren","Sela"}),"fixture: Sela out");PartyField.Reform(PartyController.Current);yield return Frames(2);
            Assert.IsFalse(sela.body.activeSelf,"Sela is out in the party and still standing at her place: two of her");
            // Sent back to rest with her place in view, she doesn't appear in front of the party; out of view, she does.
            Assert.IsTrue(Deployment.Set(State,new[]{"Taren","Idra"}),"fixture: Sela rests again");PartyField.Reform(PartyController.Current);Put(Lead,At(0,-3));yield return Frames(6);
            Assert.IsFalse(sela.body.activeSelf,"Sela appeared at her place in the camera's view");
            Put(Lead,At(0,-45));yield return Seconds(2);
            Assert.IsTrue(sela.body.activeSelf,"Sela, resting, isn't back at her place once it is out of view");
            yield return Hear(sela,At(0,-1.3f));
            Assert.IsTrue(Has("coronach.sela"),"Sela's last word didn't count");CollectionAssert.AreEqual(new[]{"SelaGateSix"},talks);
            Assert.IsTrue(sela.body.activeSelf,"a hero's place emptied after her word: the heroes stand to the end");
        }

        [UnityTest] public IEnumerator TheWalksOwnMusicPlaysAndNothingBarksFromTheCallToTheLaunch()
        {
            yield return Yard();
            var walk=Walk("Old Village","coronach.mira");yield return Frames(3);
            string before=MusicDirector.Current.Track;Assert.AreNotEqual("Old Village",before,"fixture: the yard plays the walk's music already");
            BarkService.Play("Taren","swap",true);Assert.IsNotEmpty(barks,"fixture: a swap's bark isn't heard before the walk");barks.Clear();
            Flag(Called);yield return Frames(3);
            Assert.AreEqual("Old Village",MusicDirector.Current.Track,"the walk doesn't play its own music");
            Assert.IsTrue(BarkService.Hushed,"barks aren't hushed for the walk");
            BarkService.Play("Taren","swap",true);BarkService.Play("Idra","low",true);
            Assert.IsEmpty(barks,"something barked during the Coronach: "+string.Join("; ",barks));
            var shop=new GameObject("A shop someone opens");MusicDirector.Shop(shop,true);yield return Frames(2);
            Assert.AreEqual(MusicDirector.MarketTrack,MusicDirector.Current.Track,"fixture: a shop's counter outranks every cue");
            MusicDirector.Shop(shop,false);yield return Frames(2);
            Assert.AreEqual("Old Village",MusicDirector.Current.Track,"the walk's music didn't come back after the counter shut");
            Flag(Walked);yield return Frames(3);
            Assert.AreNotEqual("Old Village",MusicDirector.Current.Track,"the walk's music plays on after the close");
            Assert.IsTrue(BarkService.Hushed,"barks came back between the close and the launch's talk");
            Flag(Launched);yield return Frames(3);
            Assert.IsFalse(BarkService.Hushed,"barks stay hushed after the launch");
            BarkService.Play("Taren","swap",true);Assert.IsNotEmpty(barks,"barks never came back");
            Object.Destroy(walk.gameObject);
        }

        [UnityTest] public IEnumerator NothingInTheWalkMovesOnByItselfForTenMinutes()
        {
            yield return Yard();
            var walk=Walk("Old Village","coronach.mira","coronach.hal");
            var mira=Place("Mira, at pitch nine",At(-3,0),At(-3,1.4f),"coronach.mira","MiraStallDoor","Mira");
            var hal=Place("Hal, at the bolt holes",At(4,0),At(4,1.4f),"coronach.hal","HalBenchDoor","Hal");
            Flag(Called);yield return Seconds(CoronachDoor.LampSeconds+.3f);Put(Lead,At(.5f,-2));yield return Frames(3);
            Assert.IsTrue(mira.body.activeSelf&&hal.body.activeSelf,"fixture: the two aren't standing at their doors");
            var flags=new Dictionary<string,bool>(State.flags);int lines=DialogueSystem.Current.LinesPresented;
            // Ten minutes of the game's own time, standing in the street.
            Time.timeScale=20;yield return new WaitForSecondsRealtime(30);Time.timeScale=1;yield return Frames(2);
            CollectionAssert.AreEquivalent(flags,State.flags,"a flag moved while the player stood still");
            Assert.AreEqual(lines,DialogueSystem.Current.LinesPresented,"somebody said something to a player who stopped for nobody");
            foreach(var door in new[]{mira,hal})Assert.IsTrue(door.body.activeSelf&&door.Lit,$"{door.name} gave up waiting");
            Put(Lead,At(-3,-1.3f));yield return Frames(3);Assert.IsTrue(mira.Offered,"Mira's door is no longer offered after ten minutes");
            Assert.AreEqual("Old Village",MusicDirector.Current.Track,"the walk's music stopped");
            Assert.IsFalse(log.Any(l=>l.StartsWith("CORONACH_ABOARD")||l.StartsWith("CORONACH_HEARD")),"somebody was taken as heard or went aboard with nobody stopping");
            Object.Destroy(walk.gameObject);
        }

        [UnityTest] public IEnumerator TheLaunchHoldsThePartyOpensTheJointLiftsTheDistrictAndLoadsTheNextZone()
        {
            yield return Yard();
            var walk=Walk("Old Village","coronach.mira");Flag(Called);Flag("coronach.mira");Flag(DoorsHeard);Flag(LastHeard);
            var district=GameObject.CreatePrimitive(PrimitiveType.Cube);district.name="Market district";district.transform.position=At(8,0)+Vector3.down*.4f;district.transform.localScale=new Vector3(12,.8f,10);
            var edge=new GameObject("The market's edge and engines");edge.transform.SetParent(district.transform,true);edge.SetActive(false);
            var chalk=GameObject.CreatePrimitive(PrimitiveType.Quad);chalk.name="Chalked joint 1";chalk.transform.position=At(-6,3)+Vector3.up*.02f;chalk.GetComponent<Renderer>().sharedMaterial=Glow();
            var launch=new GameObject("The launch").AddComponent<CoronachLaunch>();
            launch.music="Title Theme";launch.ring="Arena_Flight";launch.ringSpawn="Arrival";launch.district=new[]{district.transform};launch.lineA=At(2,-5);launch.lineB=At(2,5);launch.stays=At(-3,0);
            launch.watch=new[]{At(-1.8f,-.8f),At(-2.2f,1.4f)};launch.lookAt=At(8,0);launch.chalk=new[]{chalk.GetComponent<Renderer>()};launch.vents=new[]{At(2,-2),At(2,2)};launch.released=new[]{edge};
            launch.shows=new Bounds(At(4,0)+Vector3.up*3,new Vector3(16,8,14));launch.liftDelay=.6f;
            // The lead on the market side of the line, as a player could be standing when the walk closes.
            Put(Lead,At(3.5f,0));Put(Partner,At(-1,-3));var home=district.transform.position;yield return Frames(3);
            DialogueSystem.Current.AutoAdvance=true;
            Flag(Walked);float deadline=Time.unscaledTime+6;
            while(launch.Now==CoronachLaunch.Stage.Waiting&&Time.unscaledTime<deadline)yield return null;
            Assert.AreNotEqual(CoronachLaunch.Stage.Waiting,launch.Now,"the launch never began after the close");
            Assert.IsTrue(GameServices.Current.Input.Blocked,"the party isn't held for the launch");
            Assert.AreEqual("Title Theme",MusicDirector.Current.Track,"the launch doesn't play its own music");
            deadline=Time.unscaledTime+launch.stepSeconds+1;while(launch.Now==CoronachLaunch.Stage.Stepping&&Time.unscaledTime<deadline)yield return null;
            Assert.GreaterOrEqual(launch.Side(Lead.transform.position),1.5f,"the lead was left standing on the district's side of the line");
            deadline=Time.unscaledTime+launch.openSeconds+2;while(launch.Now==CoronachLaunch.Stage.Opening&&Time.unscaledTime<deadline)yield return null;
            CollectionAssert.Contains(log,"LAUNCH_JOINT","the walkway joint never let go");CollectionAssert.Contains(cues,"joint_release");CollectionAssert.Contains(cues,"joint_hiss");
            Assert.IsTrue(edge.activeSelf,"the district's cut edge and engines didn't show as the joint let go");
            deadline=Time.unscaledTime+60;float highest=0;
            while(!log.Any(l=>l.StartsWith("ZONE_ENTER Arena_Flight"))&&Time.unscaledTime<deadline)
            {
                if(district!=null)highest=Mathf.Max(highest,district.transform.position.y-home.y);
                yield return null;
            }
            Assert.IsTrue(talks.Contains("NeveFirstOut"),"Neve's line never played");Assert.IsTrue(Has(Launched),"the launch's talk didn't set its flag");
            Assert.Greater(highest,1.5f,"the district never lifted before the cut");
            Assert.IsTrue(barks.Any(b=>b.StartsWith("Orrin: Market district is away")),"Orrin's radio line didn't follow Neve's: "+string.Join("; ",barks));
            CollectionAssert.Contains(log,"ZONE_ENTER Arena_Flight","the launch never loaded the next zone");
            CollectionAssert.Contains(cues,"district_lift");
        }

        // ================================================================== the walk in the Decks on separation day
        const string Decks="DecksLeaving";
        /// <summary>The ways people walk the day: HomeBuilder.Decks.Ways, point for point (the builder's are the ones its people are kept off; the map brief's table is written from them),
        /// and the walk's own to the four doors those don't reach, HomeBuilder.Coronach.DoorWays (Jask stood on the dock office's front, the way to Oda, November 14).</summary>
        static readonly Vector2[][] Ways=
        {
            new[]{new Vector2(-39,-3),new Vector2(-12.6f,-3),new Vector2(-11.3f,-3.75f),new Vector2(-7.2f,-3.75f),new Vector2(-6,-3),new Vector2(39,-3)},
            new[]{new Vector2(-28,-14),new Vector2(-28,9.4f)},new[]{new Vector2(0,-14),new Vector2(0,15)},
            new[]{new Vector2(28,-14),new Vector2(28,-3),new Vector2(30.2f,1.4f),new Vector2(33,2.4f)},new[]{new Vector2(0,-11),new Vector2(6,-11.2f)},
            new[]{new Vector2(-28,-8.6f),new Vector2(-25.2f,-8.6f)},new[]{new Vector2(28,-11),new Vector2(30.4f,-11.2f)},new[]{new Vector2(-34.4f,6.8f),new Vector2(-21,6.8f)},
            new[]{new Vector2(-28,1.6f),new Vector2(-32.7f,1.6f),new Vector2(-34.4f,3)},new[]{new Vector2(-30.6f,6.8f),new Vector2(-30.6f,8.3f),new Vector2(-31,9)},
            new[]{new Vector2(-28,-13.8f),new Vector2(-29.9f,-14.9f)},new[]{new Vector2(0,-11),new Vector2(-1.7f,-12.4f)},
        };
        static float Segment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(1e-6f,d.sqrMagnitude));return Vector2.Distance(p,a+d*t);}
        static float FromWays(Vector2 p)=>Ways.Min(w=>FromWay(p,w));
        static float FromWay(Vector2 p,Vector2[] w)=>Enumerable.Range(1,w.Length-1).Min(k=>Segment(p,w[k-1],w[k]));
        /// <summary>A way that ends at this person: they are what it is walked to (Ilo at the notice board), standing at its end and past it, not beside it. That way is no fault of theirs; every
        /// other way still is.</summary>
        static bool GoalOf(Vector2 p,Vector2[] w)
        {
            foreach(var (end,before) in new[]{(w[^1],w[^2]),(w[0],w[1])})
            {
                if(Vector2.Distance(p,end)>GoalReach)continue;
                // Past the end, not alongside the way's last stretch: the nearest point of the way to them is its end.
                var d=end-before;if(Vector2.Dot(p-end,d)>=-.05f*d.magnitude)return true;
            }
            return false;
        }
        const float GoalReach=1.2f;
        static Vector2 Plan(Vector3 p)=>new(p.x,p.z);
        /// <summary>The story up to the moment Hal's machine is in nine pieces: the meeting held, the joints opened, the Saint let go; the main quest on its step 12.</summary>
        static void UpToHal(bool called)
        {
            foreach(string f in new[]{"home.treadRecovered","home.returned","home.meetingGathered","home.meetingOpened","home.offerMade","home.halAsked","home.manifestShown","home.namesRead","home.odaStood",
                "home.orrinSaid","home.berthRefused","home.meetingHeld","home.worksRead","home.joint1","home.joint2","home.joint3","bossdown.FoundrySaint","home.saintReleased","home.halChose",
                "met.Orrin","met.Mira","met.Hal","met.Neve","met.Oda","met.Ilo","met.Jun","met.Survivor","met.Dorrow"})Flag(f);
            if(called)Flag(Called);
            if(GameCatalog.Find<QuestDef>("HouseLeaves")!=null){Lattice.Rpg.RpgServices.Quests.Start("HouseLeaves");State.questSteps["HouseLeaves"]=12;}
        }
        static CoronachDoor[] Doors=>All<CoronachDoor>();
        static CoronachDoor At(string flag)=>Doors.Single(d=>d.point!=null&&d.point.flag==flag);
        IEnumerator Town(bool called,string spawn="Arrival")
        {
            Assert.GreaterOrEqual(SceneUtility.GetBuildIndexByScenePath("Assets/_Project/Scenes/"+Decks+".unity"),0,"fixture: DecksLeaving isn't built");
            UpToHal(called);yield return Zone(Decks,spawn);
            Assert.IsNotNull(Object.FindFirstObjectByType<CoronachWalk>(),"the Decks on the day has no Coronach staged in it");
        }
        /// <summary>A floor point the lead can stand on inside a place's reach, as near its point as the floor allows.</summary>
        static Vector3 Beside(CoronachDoor door)
        {
            var at=door.point.transform.position;float reach=door.point.range-.4f;Vector3 best=default;float closest=float.MaxValue;
            for(int k=0;k<24;k++)for(float r=.6f;r<=reach;r+=.6f)
            {
                float a=k*Mathf.PI/12;var probe=at+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);
                if(!NavMesh.SamplePosition(probe,out var hit,.4f,NavMesh.AllAreas)||Flat(hit.position,at)>reach)continue;
                // From the camera's side if it can be: a still of someone at their door shows their door.
                float score=Flat(hit.position,at)-(hit.position.z<at.z?.8f:0);if(score<closest){closest=score;best=hit.position;}
            }
            Assert.Less(closest,float.MaxValue,$"{door.name} can't be stood beside on foot");
            return best;
        }
        static bool Reached(Vector3 from,Vector3 to)
        {
            var path=new NavMeshPath();
            return NavMesh.SamplePosition(from,out var a,2,NavMesh.AllAreas)&&NavMesh.SamplePosition(to,out var b,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        }
        static Vector3 Spawn(string id)=>All<SpawnPoint>().Single(s=>s.id==id).transform.position;
        /// <summary>What the game's camera draws, saved for the eye (in the mirror's Builds/quality/focused/coronach).</summary>
        static void Still(string name)
        {
            const int W=1280,H=720;var camera=Camera.main;if(camera==null)return;
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/focused/coronach"));Directory.CreateDirectory(folder);
            var image=new Texture2D(W,H,TextureFormat.RGB24,false);var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var active=RenderTexture.active;var target=camera.targetTexture;
            try{rt.Create();camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());}
            finally{camera.targetTexture=target;RenderTexture.active=active;rt.Release();Object.Destroy(rt);Object.Destroy(image);}
        }

        [UnityTest] public IEnumerator TheCallIsMadeAsThePartyComesOutOfHalsWorkshop()
        {
            yield return Town(false,"Lift");
            var call=Doors.Single(d=>d.point!=null&&d.point.flag==Called);
            Assert.IsNotNull(call.arrives,"fixture: the call has no Orrin");Assert.IsTrue(call.arrives.gameObject.activeInHierarchy,"Orrin isn't waiting outside Hal's workshop once the Saint is let go");
            Assert.IsFalse(All<Npc>().Any(n=>n.speaker=="Orrin"&&n.gameObject.activeInHierarchy),"the day's Orrin is still at his board: two of him");
            var dayHal=All<Npc>().FirstOrDefault(n=>n.speaker=="Hal"&&n.gameObject.activeInHierarchy);
            var hal=At("coronach.hal");Assert.IsFalse(hal.body.activeSelf,"the Coronach's Hal stands at the bolt holes before the walk is called");
            Still("call-before");
            // Out through the workshop's open front: the crossing.
            Put(Lead,new Vector3(31,0,2.2f));yield return Frames(3);DialogueSystem.Current.AutoAdvance=true;
            Put(Lead,new Vector3(31,0,-.2f));
            float deadline=Time.unscaledTime+5;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Running,"coming out of Hal's workshop didn't start Orrin's call");
            yield return Settle();
            CollectionAssert.Contains(talks,"OrrinCallsCoronach");Assert.IsTrue(Has(Called),"the call didn't set home.coronachCalled");
            yield return Frames(3);
            Assert.IsTrue(hal.body.activeSelf,"Hal wasn't there to take over from the day's Hal at the call");
            if(dayHal!=null)Assert.IsFalse(dayHal.gameObject.activeInHierarchy,"the day's Hal and the Coronach's stand in the workshop together");
            Assert.AreEqual("Walk the town. There is no hurry.",Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Objective").text.Split('\n')[0].Trim(),"the panel doesn't say there is no hurry");
            Assert.IsNull(Object.FindFirstObjectByType<ObjectiveHud>().Goal,"an arrow points somewhere during the walk");
            deadline=Time.unscaledTime+6;while(!log.Contains("GUIDE_LEG 1")&&Time.unscaledTime<deadline)yield return null;
            CollectionAssert.Contains(log,"GUIDE_LEG 1","Orrin never set off for his board after the call");
            yield return Seconds(2);Still("call-after");
        }

        [UnityTest] public IEnumerator EveryDoorCanBeReachedAndHeardInAnyOrderAndTheSeventhOpensTheLastThree()
        {
            yield return Town(true);
            var arrival=Spawn("Arrival");var needed=Needed.Select(At).ToArray();
            foreach(var door in Doors.Where(d=>d.Live&&d.point!=null))Assert.IsTrue(Reached(arrival,Beside(door)),$"{door.name} can't be walked to from the arrival");
            Put(Lead,new Vector3(-20,0,-3));yield return Seconds(1);Still("street-start");
            var order=needed.OrderBy(_=>0).ToList();var rng=new System.Random(9);for(int i=order.Count-1;i>0;i--){int j=rng.Next(i+1);(order[i],order[j])=(order[j],order[i]);}
            for(int i=0;i<order.Count;i++)
            {
                var door=order[i];Assert.IsTrue(door.Lit,$"{door.name}'s lantern isn't lit while they wait to be heard");
                var stand=Beside(door);Put(Lead,NavMesh.SamplePosition(stand+new Vector3(0,0,-3.2f),out var back,2.5f,NavMesh.AllAreas)?back.position:stand);yield return Seconds(.8f);
                if(door.body!=null)Still("door-"+door.speaker.ToLowerInvariant());
                Put(Lead,stand);yield return Hear(door);
                Assert.IsTrue(Has(door.point.flag),$"{door.name} heard and not counted");
                Assert.AreEqual(i==order.Count-1,Has(DoorsHeard),i==order.Count-1?"the seventh door didn't open the last three":"the last three opened before the seventh door");
            }
            yield return Seconds(CoronachDoor.LampSeconds+.5f);
            var last=new[]{"coronach.taren","coronach.sela","coronach.idra"}.Select(At).ToArray();
            foreach(var door in last)Assert.IsTrue(door.Lit,$"{door.name}'s lantern isn't lit once the seven are heard");
            Put(Lead,new Vector3(-20,0,-3));yield return Seconds(1.5f);Still("street-near-the-end");
            foreach(var door in last.OrderBy(_=>rng.Next()))
            {
                Put(Lead,Beside(door));yield return Seconds(.8f);Still("last-"+door.speaker.ToLowerInvariant());yield return Hear(door);Assert.AreEqual("Say yours",door.point.prompt);
            }
            Assert.IsTrue(Has(LastHeard),"the three last words didn't close the count");
            var close=Doors.Single(d=>d.point!=null&&d.point.flag==Walked);
            float deadline=Time.unscaledTime+60;while(!close.Offered&&Time.unscaledTime<deadline){Put(Lead,Beside(close));yield return Seconds(1);}
            Assert.IsTrue(close.Offered,"Orrin never stood at the last joint to close the walk");Assert.AreEqual("Stand with Orrin",close.point.prompt);
            Still("close-orrin");
            Assert.IsFalse(Has(Walked),"the walk closed by itself");
        }

        [UnityTest] public IEnumerator EachStoryStandsInTheWalkExactlyWhenItWasFinished()
        {
            var done=new[]{"MiraCrystals","PellsLamp","SchoolBell","EmptyGardens","CutterDown","QuietHands","OdrunsKitchen"};
            foreach(var quest in done)Flag("quest."+quest+".complete");
            yield return Town(true);
            foreach(var door in Doors.Where(d=>d.point!=null))
            {
                bool any=door.extras.Any(e=>done.Any(q=>e.needs=="quest."+q+".complete"));
                if(string.IsNullOrEmpty(door.node)&&door.extras.Length>0)
                {
                    Assert.AreEqual(!any,door.Nobody,$"{door.name}: here {(any?"without":"with")} a finished story");
                    if(door.body!=null)Assert.AreEqual(any,door.body.activeSelf,$"{door.name} {(any?"isn't there though":"is there though none of")} their stories {(any?"was":"were")} finished");
                }
                foreach(var extra in door.extras)
                {
                    bool finished=done.Any(q=>extra.needs=="quest."+q+".complete");
                    if(extra.shows!=null&&(door.body==null||door.body.activeSelf||!string.IsNullOrEmpty(door.hero)))
                        foreach(var thing in extra.shows)if(thing!=null)Assert.AreEqual(finished,thing.activeSelf,$"{thing.name} {(finished?"isn't":"is")} in the walk with {extra.needs} {(finished?"":"not ")}set");
                }
            }
            // Guests are there whatever their stories.
            foreach(string guest in new[]{"pell","ruva","maru","hask","tovra","dorrow","abri"})
                Assert.IsTrue(At("coronach."+guest).body.activeSelf,$"{guest} isn't at the edge of the walk: a guest whose story wasn't done is still there");
        }

        /// <summary>Every door, every last word and the close is the prompt a player gets from wherever they would stand to press it: from a ring of stands round each (one metre and 1.75 m
        /// off, every sixteenth of a turn, wherever there is floor) the game offers that prompt and nothing else, and anything else that could be pressed there is at least a metre further
        /// off. Every side story is done, so everyone who can stand in the walk does, and each ring is walked in the state the player meets it in: the doors from the call, the last words
        /// once the seven are heard, the close once the three are. Gate six's dock to the Halo stood 0.63 m from where a route stood for Sela's last word and won the press, and Jun's
        /// door and Mira's were 0.7 m apart (November 14).</summary>
        [UnityTest] public IEnumerator EachDoorLastWordAndTheCloseIsThePromptOfferedAllRoundIt()
        {
            yield return Town(true);
            foreach(var need in Doors.SelectMany(d=>d.extras).Select(e=>e.needs).Where(n=>!string.IsNullOrEmpty(n)).Distinct().ToArray())Flag(need);
            yield return Zone(Decks);
            var faults=new List<string>();
            // The measure sees today's two faults, built into the walk for a moment: a guest spoken to beside a door (Abri's prompt 0.7 m from Ilo's), then the docks left on.
            var ilo=At("coronach.ilo");var abri=At("coronach.abri");
            yield return Offered(ilo);yield return Offered(abri);
            var abriAt=abri.point.transform.position;abri.point.transform.position=ilo.point.transform.position+new Vector3(.7f,0,0);
            var control=new List<string>();Probe(ilo,control);abri.point.transform.position=abriAt;
            Assert.IsTrue(control.Any(f=>f.Contains(abri.name)),"fixture: the ring doesn't see a guest's prompt 0.7 m from a door's:\n"+string.Join("\n",control));
            yield return Ring(Needed,faults);
            foreach(var flag in Needed)Flag(flag);
            yield return Zone(Decks);
            var last=new[]{"coronach.taren","coronach.sela","coronach.idra"};
            var sela=At("coronach.sela");yield return Offered(sela);
            var shut=CoronachWalk.Current.shut;Assert.IsNotEmpty(shut,"the walk shuts nothing: the docks to the Halo are pressable through it");
            Assert.IsTrue(shut.All(p=>p==null||!p.enabled),"something the walk shuts is still on during it");
            foreach(var p in shut)if(p!=null)p.enabled=true;
            control.Clear();Probe(sela,control);foreach(var p in shut)if(p!=null)p.enabled=false;
            Assert.IsTrue(control.Any(f=>f.Contains("Launch")),"fixture: with the docks left on, the ring doesn't see gate six's dock beside Sela's last word:\n"+string.Join("\n",control));
            yield return Ring(last,faults);
            foreach(var flag in last)Flag(flag);
            yield return Zone(Decks);
            yield return Ring(new[]{Walked},faults);
            Assert.IsEmpty(faults,"somewhere a player would stand to press a door, a last word or the close, something else is offered or nearly so:\n"+string.Join("\n",faults.Distinct().Take(60)));
        }
        static IEnumerator Offered(CoronachDoor door){float deadline=Time.unscaledTime+90;while(!door.Offered&&Time.unscaledTime<deadline)yield return null;}
        static IEnumerator Ring(string[] flags,List<string> faults)
        {
            foreach(string flag in flags)
            {
                var door=Doors.Single(d=>d.point!=null&&d.point.flag==flag);
                yield return Offered(door);
                if(!door.Offered){faults.Add(door.name+" is never offered");continue;}
                Probe(door,faults);
            }
        }
        /// <summary>The ring round one place, all in one frame: what the game offers at each stand with floor, and how much nearer it is than anything else in reach.</summary>
        static void Probe(CoronachDoor door,List<string> faults)
        {
            {
                var at=door.point.transform.position;int stands=0;
                // Rings round the prompt, and one beside the person it is (Orrin, who walks, at his post): where a player walks up to them. Wherever the prompt is in reach.
                var by=door.body!=null?door.body.transform.position:door.post!=null?door.post.position:at;
                var spots=new List<Vector3>();
                foreach(float r in new[]{1f,1.75f})for(int k=0;k<16;k++){float a=k*Mathf.PI/8;spots.Add(at+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r));}
                for(int k=0;k<16;k++){float a=k*Mathf.PI/8;spots.Add(new Vector3(by.x,at.y,by.z)+new Vector3(Mathf.Cos(a)*1.2f,0,Mathf.Sin(a)*1.2f));}
                foreach(var spot in spots)
                {
                    if(!NavMesh.SamplePosition(spot,out var hit,.3f,NavMesh.AllAreas)||Flat(hit.position,spot)>.2f||Flat(hit.position,at)>door.point.range-.1f)continue;
                    var stand=hit.position;stands++;
                    PromptService.FindInteraction(stand);var got=PromptService.Interaction;
                    if(got!=door.point){faults.Add($"{door.name}: at {Plan(stand)} the game offers {(got!=null?$"{got.name} (\"{got.prompt}\")":"nothing")}");continue;}
                    float own=Vector3.Distance(at,stand);
                    foreach(var other in InteractionPrompt.Active)
                    {
                        if(other==door.point||other==null||!other.Available)continue;var gap=other.transform.position-stand;
                        if(gap.x*gap.x+gap.z*gap.z>=other.range*other.range||Mathf.Abs(gap.y)>other.range)continue;
                        float margin=gap.magnitude-own;if(margin<1)faults.Add($"{door.name}: at {Plan(stand)} {other.name} (\"{other.prompt}\") is only {margin:0.00} m further off");
                    }
                }
                if(stands<6)faults.Add($"{door.name}: only {stands} of 48 stands round it and its person have floor in its reach");
            }
        }

        [UnityTest] public IEnumerator NobodyOfTheWalkStandsOnAWayOrCutsTheTownInTwo()
        {
            yield return Town(true);
            // The rule's own exemption, shown to hold: Ilo at the notice board is the end of the way to it, and someone beside that way's middle is not.
            var toBoard=Ways.Single(w=>w[^1]==new Vector2(-25.2f,-8.6f));
            Assert.IsTrue(GoalOf(new Vector2(-24.4f,-8.9f),toBoard),"fixture: someone at the end of the way to them isn't read as its goal");
            Assert.IsFalse(GoalOf(new Vector2(-25.4f,-8.9f),toBoard),"fixture: someone beside the way's last stretch is read as its goal");
            Assert.IsFalse(GoalOf(new Vector2(-24.4f,-8.9f),Ways[0]),"fixture: a way that doesn't end at them is read as theirs");
            var faults=new List<string>();
            foreach(var door in Doors)
            {
                var bodies=new List<GameObject>();if(door.body!=null)bodies.Add(door.body);foreach(var extra in door.extras)if(extra.shows!=null)bodies.AddRange(extra.shows.Where(s=>s!=null&&s.GetComponent<Collider>()!=null));
                foreach(var body in bodies)
                {
                    // Every way but one this person is the goal of; within reach of another way's end (a door at a corner), .6 m, as the builder's own check has it.
                    var at=Plan(body.transform.position);var others=Ways.Where(w=>!GoalOf(at,w)).ToArray();if(others.Length==0)continue;
                    float gap=others.Min(w=>FromWay(at,w));bool atAnEnd=others.SelectMany(w=>new[]{w[0],w[^1]}).Any(e=>Vector2.Distance(e,at)<2.6f);
                    if(gap<(atAnEnd?.6f:1.6f))faults.Add($"{door.name}: {body.name} at {at} is {gap:0.00} m from a way");
                }
            }
            Assert.IsEmpty(faults,"somebody of the walk stands on a way people walk:\n"+string.Join("\n",faults));
            var arrival=Spawn("Arrival");
            foreach(var spawn in All<SpawnPoint>())Assert.IsTrue(Reached(arrival,spawn.transform.position),$"the walk cuts the arrival off from the spawn {spawn.id}");
            foreach(var way in Ways)foreach(var end in new[]{way[0],way[^1]})Assert.IsTrue(Reached(arrival,new Vector3(end.x,0,end.y)),$"the walk cuts the arrival off from the way's end at {end}");
        }

        [UnityTest] public IEnumerator NothingBarksAndOnlyTheWalksOwnMusicPlaysThroughTheWalk()
        {
            yield return Town(true);
            var walk=Object.FindFirstObjectByType<CoronachWalk>();
            CollectionAssert.Contains(ZoneController.Current.definition.cueMusicIds,walk.music,"the walk's music isn't named on the zone, so nothing stages it");
            Assert.IsNotNull(Resources.Load<AudioClip>("Audio/Music/"+walk.music),$"the walk's music, {walk.music}, isn't staged");
            Assert.AreEqual(walk.music,MusicDirector.Current.Track,"the walk doesn't play its own music");
            Assert.IsEmpty(barks,"something barked as the zone loaded into the walk: "+string.Join("; ",barks));
            PartyController.Current.Swap();yield return Seconds(1);PartyController.Current.Swap();
            yield return Hear(At("coronach.ilo"),Beside(At("coronach.ilo")));
            yield return Seconds(4);
            Assert.IsEmpty(barks,"something barked during the walk: "+string.Join("; ",barks));
            Assert.AreEqual(walk.music,MusicDirector.Current.Track,"the walk's music gave way to something");
            Assert.IsFalse(cues.Any(c=>c.StartsWith("shop")||c.StartsWith("ui_buy")),"a counter's sound played in the walk");
        }

        [UnityTest] public IEnumerator ASaveMadeMidWalkLoadsIntoTheWalkWithWhatWasSaidStillSaid()
        {
            yield return Town(true);
            foreach(var flag in new[]{"coronach.mira","coronach.jun","coronach.hal"}){var door=At(flag);yield return Hear(door,Beside(door));}
            var saved=GameServices.Current.Saves.Load("autosave");Assert.IsNotNull(saved,"hearing a door wrote no save");
            foreach(var flag in new[]{"coronach.mira","coronach.jun","coronach.hal",Called})Assert.IsTrue(saved.flags.TryGetValue(flag,out bool on)&&on,$"the save doesn't hold {flag}");
            // Quit and continue: the save, loaded as the title loads it.
            GameServices.Current.State=saved;yield return Zone(saved.zone,saved.spawn,true);
            Assert.AreEqual(Decks,SceneManager.GetActiveScene().name,"the save didn't load back into the Decks");
            foreach(var flag in new[]{"coronach.mira","coronach.jun","coronach.hal"})
            {
                var door=At(flag);Assert.IsTrue(door.Gone,$"{door.name} was heard before the save and stands at the door again");
                Assert.IsFalse(door.lamp!=null&&door.lamp.activeSelf,$"{door.name}'s lantern is back after the load");
                foreach(var shut in door.shuts)Assert.IsTrue(shut.activeSelf,$"what {door.name} faced isn't shut after the load");
            }
            foreach(var flag in new[]{"coronach.orrin","coronach.neve","coronach.oda","coronach.ilo"})Assert.IsTrue(At(flag).Lit,$"{At(flag).name} lost their lantern in the load");
            Assert.AreEqual(Object.FindFirstObjectByType<CoronachWalk>().music,MusicDirector.Current.Track,"the walk's music didn't come back with the load");
            Assert.IsTrue(BarkService.Hushed,"the walk isn't quiet after the load");
        }

        [UnityTest] public IEnumerator APlayerWhoStopsForNobodyForTenMinutesLosesNothing()
        {
            yield return Town(true);
            Put(Lead,new Vector3(-3,0,-6));yield return Seconds(1);
            var flags=new Dictionary<string,bool>(State.flags);int lines=DialogueSystem.Current.LinesPresented;
            var waiting=Doors.Where(d=>d.Live&&d.Lit).ToArray();Assert.IsNotEmpty(waiting,"fixture: nobody is waiting at a door");
            Time.timeScale=20;yield return new WaitForSecondsRealtime(30);Time.timeScale=1;yield return Frames(3);
            CollectionAssert.AreEquivalent(flags,State.flags,"the walk moved on by itself while the player stood still");
            Assert.AreEqual(lines,DialogueSystem.Current.LinesPresented,"somebody spoke to a player who stopped for nobody");
            foreach(var door in waiting)Assert.IsTrue(door.Live&&door.Lit,$"{door.name} gave up waiting");
            Assert.AreEqual(Object.FindFirstObjectByType<CoronachWalk>().music,MusicDirector.Current.Track,"the walk's music stopped");
            Assert.IsEmpty(barks,"something barked at a player standing still: "+string.Join("; ",barks));
            var mira=At("coronach.mira");yield return Hear(mira,Beside(mira));Assert.IsTrue(Has("coronach.mira"),"a door can't be heard after ten minutes");
        }

        [UnityTest] public IEnumerator TheLaunchRunsFromTheCloseToTheRingsLoad()
        {
            foreach(var flag in Needed.Concat(new[]{DoorsHeard,"coronach.taren","coronach.sela","coronach.idra",LastHeard}))Flag(flag);
            yield return Town(true);
            Assert.GreaterOrEqual(SceneUtility.GetBuildIndexByScenePath("Assets/_Project/Scenes/BrokenRing.unity"),0,"fixture: the Broken Ring isn't built, so the launch has nowhere to cut to");
            var close=Doors.Single(d=>d.point!=null&&d.point.flag==Walked);var launch=Object.FindFirstObjectByType<CoronachLaunch>();
            Assert.IsNotEmpty(launch.district,"nothing is set to lift at the launch");
            var homes=launch.district.Select(t=>t.position).ToArray();
            yield return Hear(close,Beside(close));
            CollectionAssert.Contains(talks,"OrrinGiveItRoom");Assert.IsTrue(Has(Walked),"the close didn't set home.coronachWalked");
            // Neve's talk at a player's pace: held on its first line while the market lifts (the lift runs on its own clock), then read through. With every line read at once the talk
            // ended before the lift had gone far, and the cut took the scene, and the market's parts with it, before they were read.
            DialogueSystem.Current.AutoAdvance=false;
            var parts=launch.district.ToArray();float highest=0;bool cut=false;
            IEnumerator Watching(float seconds)
            {
                for(float until=Time.unscaledTime+seconds;Time.unscaledTime<until;)
                {
                    if(log.Any(l=>l.StartsWith("LAUNCH_CUT")))cut=true;
                    if(!cut)for(int i=0;i<parts.Length;i++)if(parts[i]!=null)highest=Mathf.Max(highest,parts[i].position.y-homes[i].y);
                    yield return null;
                }
            }
            float deadline=Time.unscaledTime+12;while(!log.Contains("LAUNCH_JOINT")&&Time.unscaledTime<deadline)yield return null;
            CollectionAssert.Contains(log,"LAUNCH_JOINT","the last joint never let go");
            Assert.GreaterOrEqual(launch.Side(Lead.transform.position),1.5f,"the lead stands on the market's side of the parting as it lets go");
            yield return Seconds(.5f);Still("launch-1-joint");
            deadline=Time.unscaledTime+20;while(!log.Contains("LAUNCH_LIFT")&&Time.unscaledTime<deadline)yield return null;
            CollectionAssert.Contains(log,"LAUNCH_LIFT","the market never lifted");
            Assert.IsTrue(talks.Contains("NeveFirstOut")&&DialogueSystem.Current.Running,"the market lifted outside Neve's talk");
            yield return Watching(3);Still("launch-2-lift");yield return Watching(4);Still("launch-3-away");
            Assert.IsFalse(cut,"the launch cut to the Ring with Neve's talk still on its first line");
            Assert.Greater(highest,1,"the market's parts didn't rise");
            DialogueSystem.Current.AutoAdvance=true;
            deadline=Time.unscaledTime+60;while(!log.Any(l=>l=="ZONE_ENTER BrokenRing")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(talks.Contains("NeveFirstOut"),"Neve's line never played");Assert.IsTrue(Has(Launched),"the launch's talk didn't set home.launched");
            Assert.IsTrue(barks.Any(b=>b.StartsWith("Orrin: Market district is away")),"Orrin's radio line didn't follow the talk");
            CollectionAssert.Contains(log,"ZONE_ENTER BrokenRing","the launch never cut to the Broken Ring");
        }

        [UnityTest] public IEnumerator ASaveFromBetweenTheCloseAndTheLaunchsTalkPlaysTheLaunchAgain()
        {
            foreach(var flag in Needed.Concat(new[]{DoorsHeard,"coronach.taren","coronach.sela","coronach.idra",LastHeard,Walked}))Flag(flag);
            yield return Town(true);
            var launch=Object.FindFirstObjectByType<CoronachLaunch>();
            float deadline=Time.unscaledTime+6;while(!log.Contains("LAUNCH_BEGIN")&&Time.unscaledTime<deadline)yield return null;
            CollectionAssert.Contains(log,"LAUNCH_BEGIN","a save between the close and the launch's talk loaded into a town that can't be left");
            Assert.GreaterOrEqual(launch.Side(Lead.transform.position),2,"the party wasn't set back from the line behind the load's black");
            Assert.IsTrue(GameServices.Current.Input.Blocked,"the party isn't held for the launch");
            deadline=Time.unscaledTime+12;while(!talks.Contains("NeveFirstOut")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(talks.Contains("NeveFirstOut"),"the launch's talk never began");
        }
    }
}
