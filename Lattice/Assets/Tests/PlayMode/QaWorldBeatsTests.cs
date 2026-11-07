using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Yarn.Unity;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The November QA pass, the story's small beats: a meeting that comes late, a door that says it runs one way, what is
    /// standing in the room after the script has moved on, and the moments a flag used to pass in one frame. Every check here fails
    /// on the build it was written against.</summary>
    public sealed class QaWorldBeatsTests
    {
        string heard;readonly List<string> log=new();readonly List<(string who,string line)> barks=new();
        void Listen(string node,string speaker,System.Action<bool> done)=>heard=node;
        void Logged(string text,string stack,LogType type){if(text.StartsWith("CURTAIN_OPENED"))log.Add(text);}
        void Barked(string who,string line)=>barks.Add((who,line));
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");heard=null;log.Clear();barks.Clear();
            Npc.TalkRequested+=Listen;Application.logMessageReceived+=Logged;BarkService.Spoken+=Barked;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Npc.TalkRequested-=Listen;Application.logMessageReceived-=Logged;BarkService.Spoken-=Barked;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static GameState State=>GameServices.Current.State;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static int Step(string quest)=>State.questSteps.TryGetValue(quest,out var step)?step:-1;
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+20;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Prepared);
            yield return new WaitForSecondsRealtime(.4f);
        }
        static void Put(CombatActor hero,Vector3 at)
        {
            var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.position=at;cc.enabled=true;Physics.SyncTransforms();
        }
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static DiscoveryPoint Discovery(string flag)=>All<DiscoveryPoint>().Single(d=>d.flag==flag);
        static WarpBeacon WarpTo(string scene)=>All<WarpBeacon>().Single(w=>w.scene==scene);
        static Npc Fixture(string speaker)
        {
            var npc=new GameObject(speaker+" fixture").AddComponent<Npc>();
            npc.speaker=speaker;npc.firstNode=speaker+"First";npc.repeatNode=speaker+"Repeat";npc.postNode=speaker+"Post";return npc;
        }
        IEnumerator Talk(Npc npc)
        {
            heard=null;DialogueSystem.Current.AutoAdvance=true;npc.Interact();yield return null;
            Assert.IsNotNull(heard,npc.speaker+" did not speak");
            yield return Finish();
            Assert.IsFalse(DialogueSystem.Current.Running,npc.speaker+"'s conversation never ended");
            yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Finish(float seconds=25)
        {
            float deadline=Time.unscaledTime+seconds;
            while((DialogueSystem.Current.Running||GameInput.Current.Blocked)&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.2f);
        }
        static IEnumerator Until(System.Func<bool> done,float seconds)
        {float deadline=Time.unscaledTime+seconds;while(!done()&&Time.unscaledTime<deadline)yield return null;}
        static string Objective()=>Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Objective").text;
        // The script as it stands: what the game compiles, and the copy a rebuild copies it from.
        static string Game=>Path.Combine(Application.dataPath,"_Project/Resources/Dialogue");
        static string Writing=>Path.Combine(Application.dataPath,"..","..","docs","writing");
        static string Script(string root)=>string.Concat(Directory.GetFiles(root,"*.yarn").OrderBy(f=>f).Select(File.ReadAllText)).Replace("\r\n","\n");
        static string Body(string node)
        {
            string all=Script(Game);int at=all.IndexOf("title: "+node+"\n",System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(at,0,"the script has no "+node);
            return all.Substring(at,all.IndexOf("\n===",at,System.StringComparison.Ordinal)-at);
        }

        // ---------------------------------------------------------------- copy5, prog155, copy20, scri50, scri99, ques117: the words
        [UnityTest] public IEnumerator TheScriptSaysWhatTheWorldShows()
        {
            yield return null;
            foreach(var root in new[]{Game,Writing})
            {
                string all=Script(root);
                foreach(var (gone,why) in new[]
                {
                    ("-> Repair and save.\n","Hal's option takes a fee it doesn't name, where the console beside him names it"),
                    ("the port line?","Abri calls the port docking line something else from everyone else"),
                    ("outer beacon","Taren calls the outer warp a beacon in the one scene every player hears"),
                    ("The beacon can read","Sela calls the outer warp a beacon in the one scene every player hears"),
                    ("Then food first","Taren's comeback is the script's stock 'Then...' reply"),
                    ("Then they'll lose it","Sela's comeback is the script's stock 'Then...' reply"),
                    ("palm-sized","Ishen describes something smaller than what stands on the floor"),
                    ("coasters","Ishen describes something other than what stands on the floor"),
                })StringAssert.DoesNotContain(gone,all,root+": "+why);
                foreach(var (kept,why) in new[]
                {
                    ("Were you out by the port docking line?","Abri no longer asks about the line by its name"),
                    ("Will the outer warp read a route that moves?","Taren no longer asks about the outer warp"),
                    ("The warp can read it alongside the survey key.","Sela no longer answers about the warp"),
                    ("mirror stand","Ishen doesn't say where the plates rest, so the lit stands mean nothing"),
                })StringAssert.Contains(kept,all,root+": "+why);
                Assert.AreEqual(3,Regex.Matches(all,Regex.Escape("-> Repair and save (8 scrip per Sync level).")).Count,root+": one of Hal's repair options still doesn't name the fee");
            }
            foreach(var node in new[]{"AbriLanternHome","HessaYields"})
                Assert.IsFalse(Regex.IsMatch(Body(node),@"^\w+: Then\b",RegexOptions.Multiline),node+" still answers with a stock 'Then...' retort");
            // And that is what was compiled: the player reads the string table, not the source.
            var table=Resources.Load<YarnProject>("Dialogue/Lattice").baseLocalization;
            var compiled=table.GetLineIDs().Select(id=>table.GetLocalizedString(id)).Where(line=>line!=null).ToArray();
            Assert.AreEqual(3,compiled.Count(line=>line.Contains("Repair and save (8 scrip per Sync level)")),"the compiled script still offers Hal's repair with no fee");
            Assert.IsFalse(compiled.Any(line=>line.Contains("outer beacon")),"the compiled script still says outer beacon");
        }
        [UnityTest] public IEnumerator TheWickDockNamesThePlaceAsEverySignDoes()
        {
            yield return Zone("Lanternwick");
            var dock=All<DockingPad>().Single(d=>d.scene=="WickReach");
            StringAssert.Contains("Launch — The Wick Reach",dock.prompt,"Lanternwick's dock spells the Wick Reach differently from the warp at the other end of it");
        }

        // ---------------------------------------------------------------- scri42, ques136: a first meeting the story has overtaken
        [UnityTest] public IEnumerator ALateFirstMeetingWithDaceFitsWhatHasHappened()
        {
            yield return Zone("Sorrel_Ridges");
            var dace=All<Npc>().Single(n=>n.speaker=="Survivor");
            // With the rig still running he introduces himself as written.
            yield return Talk(dace);
            Assert.AreEqual("SurvivorFirst",heard,"fixture: an ordinary first meeting plays his introduction");
            Flag("met.Survivor",false);Flag("bossdown.Burrower");Flag("clear.Sorrel_Burrower");
            yield return Talk(dace);
            Assert.AreEqual("SurvivorLate",heard,"a player who beat the Burrower first still hears that it homes in on evacuation calls now");
            Assert.IsTrue(Has("met.Survivor"),"after that talk the objective still says to find the outpost crew");
            string said=Body("SurvivorLate");
            StringAssert.DoesNotContain("survey key",said,"he points the party at a key they may already be holding");
            StringAssert.DoesNotContain("homes in on evacuation calls now",said,"he talks about a rig that is lying dead as if it were still hunting");
            StringAssert.Contains("Dace",said,"nobody ever tells the player who this is");
            StringAssert.Contains("evacuation",said,"the party never learns why Sorrel stopped answering");
            yield return Talk(dace);
            Assert.AreEqual("SurvivorBeaconsOffer",heard,"the next talk replays the meeting instead of moving on to the beacons");
        }
        [UnityTest] public IEnumerator AFirstMeetingWithTheParcelInHandDeliversIt()
        {
            yield return Zone("Hub_Decks","Office");
            var dace=Fixture("Survivor");
            RpgServices.Quests.Start("NeveParcel");Assert.AreEqual(0,Step("NeveParcel"),"fixture: the parcel is held");
            int lines=DialogueSystem.Current.LinesPresented;
            yield return Talk(dace);
            Assert.AreEqual(1,Step("NeveParcel"),"the step still reads 'Deliver Neve's parcel to Dace' after talking to Dace, and nothing says to talk again");
            Assert.GreaterOrEqual(DialogueSystem.Current.LinesPresented-lines,6,"the one talk lost his introduction or the delivery");
            Assert.IsTrue(Has("met.Survivor"),"he still counts as a stranger after that");
            yield return Talk(dace);
            Assert.AreEqual("SurvivorRepeat",heard,"the parcel is delivered a second time");Assert.AreEqual(1,Step("NeveParcel"));
            // Late as well: the fight is over and the parcel still comes up.
            Flag("met.Survivor",false);Flag("neve.parcelDelivered",false);State.questSteps["NeveParcel"]=0;Flag("bossdown.Burrower");
            lines=DialogueSystem.Current.LinesPresented;
            yield return Talk(dace);
            Assert.AreEqual("SurvivorLateParcel",heard,"a party that beat the Burrower with the parcel in hand hears the rig described as still hunting, or never hands it over");
            Assert.AreEqual(1,Step("NeveParcel"),"the late meeting doesn't deliver the parcel");
            Assert.GreaterOrEqual(DialogueSystem.Current.LinesPresented-lines,6,"the one talk lost his late introduction or the delivery");
        }

        // ---------------------------------------------------------------- ques117, scri104: the record plates
        [UnityTest] public IEnumerator RecordPlatesRestOnStandsThatOutlastThem()
        {
            StringAssert.Contains("mirror stand",GameCatalog.Find<QuestDef>("IshensRecord").stepText[0],"the quest log doesn't say what to look for, so the plates are found by luck");
            yield return Zone("SunkenRelay");
            var stands=All<Transform>().Where(t=>t.name=="Record plate stand").ToArray();
            Assert.AreEqual(3,stands.Length,"the thing Ishen calls a plate is a 0.9 m pedestal, and the whole of it vanishes when taken");
            yield return null;yield return null;
            foreach(var stand in stands)Assert.IsTrue(stand.gameObject.activeInHierarchy&&stand.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"an old mirror stand is missing before anyone has asked for the plates");
            foreach(var flag in new[]{"relay.plate1","relay.plate2","relay.plate3"})Assert.IsFalse(Discovery(flag).visual.activeSelf,flag+": on show before anyone has asked for it");
            RpgServices.Quests.Start("IshensRecord");yield return null;yield return null;
            foreach(var flag in new[]{"relay.plate1","relay.plate2","relay.plate3"})
            {
                var point=Discovery(flag);var plate=point.visual;
                Assert.IsTrue(plate.activeSelf,flag+": missing while the quest wants it");
                var stand=stands.OrderBy(s=>Flat(s.position,point.transform.position)).First();
                Assert.Less(Flat(plate.transform.position,stand.position),1.2f,flag+": the plate isn't resting on its stand");
                Assert.Less(Flat(stand.position,point.transform.position),3,flag+": the stand is out of reach of the prompt");
                var bounds=plate.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
                Assert.Less(bounds.size.y,.65f,flag+": what is taken still stands as tall as the pedestal");
                Assert.Less(Mathf.Max(bounds.size.x,bounds.size.z),.8f,flag+": what is taken is bigger than a plate");
                var glow=plate.GetComponentInChildren<Light>(true);
                Assert.IsNotNull(glow,flag+": no light on the plate, so from the fixed camera it is lost among the rubble");
                Assert.IsTrue(glow.enabled&&glow.gameObject.activeInHierarchy,flag+": its light is off while the quest wants it");
                Assert.Less(Vector3.Distance(glow.transform.position,stand.position),2.5f,flag+": the light isn't on the stand");
                point.Interact();yield return null;yield return null;
                Assert.IsFalse(plate.activeSelf,flag+": still resting there after being taken");
                Assert.IsFalse(glow.gameObject.activeInHierarchy,flag+": the light stays lit over an empty stand");
                Assert.IsTrue(stand.gameObject.activeInHierarchy&&stand.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),flag+": the stand vanishes whole with the plate");
            }
        }

        // ---------------------------------------------------------------- boss259, worl63: Hessa, afterwards
        [UnityTest] public IEnumerator KeepersStandOverHessaOnceAumiHasBeenTold()
        {
            Flag("relay.hessaConfronted");Flag("bossdown.HessaKorl");Flag("clear.Relay_Hessa");
            yield return Zone("SunkenRelay");
            GameObject[] Keepers()=>All<Transform>().Where(t=>t.name=="Keeper on guard").Select(t=>t.gameObject).ToArray();
            Assert.AreEqual(2,Keepers().Length,"the plan says she stands under guard, and no keeper exists in the room");
            Assert.IsTrue(Keepers().All(k=>!k.activeInHierarchy),"the keepers are in the room while the script says they are on their way");
            // She says her piece and they are truly on their way: nobody pops in behind it.
            Flag("relay.hessaYielded");yield return new WaitForSecondsRealtime(.4f);
            Assert.IsTrue(Keepers().All(k=>!k.activeInHierarchy),"two keepers pop into the room the moment she yields");
            // Quit and Continue right after the yield, or walk back in, before anyone has told Aumi: nobody has sent them yet.
            yield return Zone("SunkenRelay");
            var person=All<Npc>().Single(n=>n.speaker=="Hessa");
            Assert.IsTrue(Keepers().All(k=>!k.activeInHierarchy),"two keepers stand over her while the objective still says to go and tell Aumi");
            yield return Talk(person);
            Assert.AreEqual("HessaRepeat",heard,"with nobody sent yet she talks about keepers who aren't there");
            // Aumi has been told, and this visit they are here.
            Flag("lanternwick.chartGiven");
            yield return Zone("SunkenRelay");
            person=All<Npc>().Single(n=>n.speaker=="Hessa");
            var keepers=Keepers();
            Assert.IsTrue(person.gameObject.activeInHierarchy,"she isn't at her post");
            Assert.IsTrue(keepers.All(k=>k.activeInHierarchy),"coming back later she still stands alone, unguarded");
            var sides=new List<float>();var block=new MaterialPropertyBlock();
            foreach(var keeper in keepers)
            {
                Assert.Less(Vector3.Distance(keeper.transform.position,person.transform.position),3,"a keeper isn't guarding her from across the room");
                var parts=keeper.GetComponentsInChildren<Renderer>();
                Assert.Greater(parts.Length,0,"a keeper has no body");
                foreach(var part in parts)
                {
                    part.GetPropertyBlock(block);
                    Assert.IsFalse(block.isEmpty,"a keeper on guard is an exact double of Aumi or Pell, who are up in town");
                    Assert.Less(block.GetColor("_BaseColor").r,.9f,"a keeper on guard still wears Aumi's or Pell's own colours");
                }
                StringAssert.DoesNotContain("ART_PENDING",keeper.GetComponentsInChildren<Transform>().Select(t=>t.name).Aggregate("",(a,b)=>a+b),"a keeper is placeholder art");
                sides.Add(Vector3.Dot(keeper.transform.position-person.transform.position,person.transform.right));
            }
            Assert.IsTrue(sides.Any(s=>s>.5f)&&sides.Any(s=>s<-.5f),"the keepers don't flank her");
            yield return Talk(person);
            Assert.AreEqual("HessaGuarded",heard,"with two keepers standing over her she still says she's waiting on them");
        }
        [UnityTest] public IEnumerator HessaHeardAfterTheChartIsntStillShouting()
        {
            Flag("relay.hessaConfronted");Flag("bossdown.HessaKorl");Flag("clear.Relay_Hessa");Flag("lanternwick.chartGiven");
            yield return Zone("SunkenRelay");
            var person=All<Npc>().Single(n=>n.speaker=="Hessa");
            foreach(var hero in PartyController.Current.members)Put(hero,person.transform.position+new Vector3(0,.2f,-4));
            DialogueSystem.Current.AutoAdvance=true;
            yield return Until(()=>heard!=null,4);
            Assert.AreEqual("HessaYieldsLate",heard,"coming back after the chart she still plays 'Enough. Enough!' as if the fight had just ended");
            StringAssert.DoesNotContain("Enough",Body(heard),"she is still shouting about a fight long over");
            StringAssert.Contains("Warmaster",Body(heard),"the only mention of the Warmaster is lost to anyone who hears her late");
            yield return Finish();
            Assert.IsTrue(Has("relay.hessaYielded"),"the lift stays shut after she has said her piece");
            // Aumi has already been told and has handed over the chart: the way out no longer sends the party to tell her.
            yield return new WaitForSecondsRealtime(.35f);
            string objective=Objective();
            StringAssert.Contains("lift",objective,"after she has said her piece the objective doesn't point at the relay lift");
            StringAssert.DoesNotContain("tell Keeper Aumi",objective,"the objective sends the party to tell Aumi what she already knows");
            yield return Zone("LanternGorge");
            yield return new WaitForSecondsRealtime(.35f);
            objective=Objective();
            StringAssert.Contains("Climb back to Lanternwick",objective,"the gorge's objective no longer points the way back");
            StringAssert.DoesNotContain("tell Keeper Aumi",objective,"the gorge's objective sends the party to tell Aumi what she already knows");
        }

        // ---------------------------------------------------------------- ui222, prog142: the one-way warp
        [UnityTest] public IEnumerator TheGulletWarpSaysItRunsOneWay()
        {
            foreach(var f in new[]{"met.Orrin","warpkey","hushwell.nursery"})Flag(f);
            yield return Zone("Hub_CinderHalo","Outer");yield return null;yield return null;
            var warp=WarpTo("Gullet_Tunnel");
            bool Sign()=>All<TextMeshPro>().Any(t=>t.gameObject.activeInHierarchy&&t.text.ToLowerInvariant().Contains("one way")&&Flat(t.transform.position,warp.transform.position)<7);
            StringAssert.Contains("one way",warp.prompt,"the warp into the Gullet takes the party through a door with no way back, and its prompt doesn't say so");
            StringAssert.Contains("through",warp.prompt,"the prompt doesn't say when the way back opens");
            StringAssert.DoesNotContain("Cantor",warp.prompt,"the warp names a boss nobody has mentioned");
            Assert.IsTrue(Sign(),"the warp's sign doesn't say it runs one way");
            // Nobody has heard of the Cantor until the Gullet, and these stand in view from the first visit here.
            Assert.IsFalse(All<TextMeshPro>().Any(t=>t.gameObject.activeInHierarchy&&t.text.ToLowerInvariant().Contains("cantor")),"a sign on the Halo names a boss nobody has mentioned");
            var home=WarpTo("TallowApproach");
            StringAssert.DoesNotContain("Cantor",home.lockedPrompt,"the locked warp home names a boss nobody has mentioned");
            StringAssert.Contains("Gullet",home.lockedPrompt,"the locked warp home no longer says what opens it");
            // The road home opens: the warning would be a lie now.
            Flag("bossdown.Cantor");yield return null;yield return null;
            StringAssert.DoesNotContain("one way",warp.prompt,"the prompt still says one way after the warp home has opened");
            StringAssert.Contains("The Gullet",warp.prompt,"the prompt no longer names where the warp goes");
            Assert.IsFalse(Sign(),"the sign still says one way after the warp home has opened");
        }
        [UnityTest] public IEnumerator ARepairWaitsBesideTheOneWayWarp()
        {
            foreach(var f in new[]{"met.Orrin","warpkey","hushwell.nursery"})Flag(f);
            yield return Zone("Hub_CinderHalo","Outer");
            var warp=WarpTo("Gullet_Tunnel");
            var bay=All<RepairBay>().Where(b=>b.free).OrderBy(b=>Vector3.Distance(b.transform.position,warp.transform.position)).FirstOrDefault();
            Assert.IsNotNull(bay,"the line from the Hushwell lift to the one-way Gullet passes no repair, so a party arrives hurt with its partner down");
            Assert.Less(Vector3.Distance(bay.transform.position,warp.transform.position),20,"the only free repair is nowhere near the warp");
            Assert.Greater(bay.GetComponentsInChildren<Renderer>().Length,0,"the repair is an unmarked point in space");
            Assert.IsEmpty(bay.GetComponentsInChildren<Collider>().Where(c=>!c.isTrigger),"something solid in the flight lane costs integrity to touch, on the way to the repair");
            Assert.IsTrue(All<TextMeshPro>().Any(t=>t.gameObject.activeInHierarchy&&t.text.ToUpperInvariant().Contains("REPAIR")&&Flat(t.transform.position,bay.transform.position)<7),"nothing says what the thing beside the warp is for");
            StringAssert.Contains("repair and save",bay.prompt,"its prompt doesn't say what it does");
            // A ship on the flight plane above it gets its prompt, and where the routes stop for the warp still gets the warp's.
            PromptService.FindInteraction(new Vector3(bay.transform.position.x,1,bay.transform.position.z));
            Assert.AreEqual(bay,PromptService.Interaction,"a ship flown up to the repair gets no prompt");
            PromptService.FindInteraction(new Vector3(0,1,144));
            Assert.AreEqual(warp,PromptService.Interaction,"the repair takes the press meant for the warp at the spot the route stops");
            var lead=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=lead);
            lead.Health.integrity=lead.Health.maximum*.25f;partner.Health.Receive(new DamagePacket{amount=partner.Health.maximum*5,type=DamageType.Kinetic});
            yield return null;yield return null;
            Assert.IsFalse(partner.Health.Alive,"fixture: the partner is down");
            int scrip=State.scrip;string card=null;void Note(string heading,string body)=>card=heading;Notices.Posted+=Note;
            bay.Interact();yield return null;Notices.Posted-=Note;
            Assert.AreEqual(lead.Health.maximum,lead.Health.integrity,"the repair leaves the lead hurt");
            Assert.IsTrue(partner.Health.Alive,"the repair leaves the partner down in flight");
            Assert.AreEqual(scrip,State.scrip,"the repair before the one-way warp isn't free");
            Assert.AreEqual("Outer",State.spawn,"the repair doesn't save at the warp");
            StringAssert.Contains("REPAIRED",card??"","the repair shows no card");
            Assert.IsTrue(GameServices.Current.Saves.Load("autosave").party.All(m=>m.integrity>0),"the autosave still holds a downed partner");
        }

        // ---------------------------------------------------------------- worl68: Corra's stock
        [UnityTest] public IEnumerator CorrasOfferEndsAtHerStock()
        {
            Flag("met.Corra");State.questSteps["LanternRoad"]=3;
            yield return Zone("Lanternwick");
            var corra=All<Npc>().Single(n=>n.speaker=="Corra");
            DialogueSystem.Current.AutoAdvance=true;corra.Interact();yield return null;
            Assert.AreEqual("CorraRationsOffer",heard,"fixture: her offer plays on this talk");
            yield return Until(()=>!DialogueSystem.Current.Running,25);
            Assert.IsFalse(DialogueSystem.Current.Running,"her offer never ended");
            yield return null;yield return null;
            var shop=Object.FindFirstObjectByType<ShopUi>();
            Assert.IsTrue(shop.IsOpen,"a player who came to buy hears the offer and then has to talk to her a second time");
            Assert.AreEqual(0,Step("UnswornRations"),"her stock opened and the quest never started");
            shop.Close();
        }

        // ---------------------------------------------------------------- worl71: a warp nobody has named
        [UnityTest] public IEnumerator TheWickWarpHasNoNameUntilAbriGivesItOne()
        {
            yield return Zone("TallowApproach");yield return null;yield return null;
            var warp=WarpTo("WickReach");
            TextMeshPro[] Shown()=>All<TextMeshPro>().Where(t=>t.gameObject.activeInHierarchy&&Flat(t.transform.position,warp.transform.position)<7).ToArray();
            Assert.IsFalse(Shown().Any(t=>t.text.Contains("Wick Reach")),"a signed warp to the Wick Reach stands in view before anyone has mentioned the place");
            Assert.IsTrue(Shown().Any(t=>t.text=="Warp — No course plotted"),"the warp's sign has gone, not changed: an unlabelled column");
            StringAssert.DoesNotContain("Wick",warp.prompt,"flying up to the warp names the place nobody has mentioned");
            warp.Interact();
            StringAssert.Contains("Abri",warp.prompt,"a refused press no longer says who holds the route");
            Flag("lantern.routeKnown");yield return null;yield return null;
            Assert.IsTrue(Shown().Any(t=>t.text=="Warp — The Wick Reach"),"Abri has named it and the sign still says no course");
            Assert.IsFalse(Shown().Any(t=>t.text.Contains("No course plotted")),"both signs show at once");
            StringAssert.Contains("The Wick Reach",warp.prompt,"Abri has named it and the prompt still says no course");
        }

        // ---------------------------------------------------------------- ui242: A with an enemy on the hero
        static T Beside<T>(Vector3 at)where T:InteractionPrompt
        {var prompt=new GameObject(typeof(T).Name+" fixture").AddComponent<T>();prompt.range=6;prompt.transform.position=at;return prompt;}
        static IEnumerator Tap(Gamepad pad,GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return null;yield return null;
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;yield return null;yield return null;
        }
        [UnityTest] public IEnumerator WithAnEnemyOnTheHeroACacheAndTheKeyWait()
        {
            var background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            var editor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var pad=InputSystem.AddDevice<Gamepad>();
            try
            {
                yield return Zone("Arena_Ground");
                Flag("bossdown.Burrower");Flag("neve.cache");
                var hero=PartyController.Current.Active;Put(hero,new Vector3(30,0,30));hero.GetComponent<PlayerBrain>().AutoPilot=false;
                hero.Health.InvulnerableUntil=GameTime.Now+60;
                var at=hero.transform.position;
                var hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),at+Vector3.right*3);hound.Passive=false;hound.Health.InvulnerableUntil=GameTime.Now+60;
                var key=Beside<KeyPickup>(at);var cache=Beside<SalvageField>(at);
                // A quest's pick-up: the plates, Pell's lamp and the ration crate all sit where fights happen.
                var pickup=Beside<DiscoveryPoint>(at);pickup.flag="qa.pickup";pickup.report="";pickup.situation="";pickup.dialogueNode="";pickup.requiredQuest="";pickup.visual=new GameObject("qa pickup");
                yield return null;yield return null;
                Assert.IsFalse(key.Available,"with a hound on the hero, A takes the survey key instead of swinging");
                Assert.IsFalse(cache.Available,"with a hound on the hero, A opens the cache instead of swinging");
                Assert.IsFalse(pickup.Available,"with a hound on the hero, A takes a quest's pick-up instead of swinging");
                int swings=hero.AttackSequence;
                yield return Tap(pad,GamepadButton.South);
                Assert.IsFalse(Has("warpkey"),"A beside the survey key with a hound on the hero picks it up");
                Assert.IsFalse(Has("cache.Neve"),"A beside a cache with a hound on the hero opens it");
                Assert.IsFalse(Has("qa.pickup"),"A beside a quest's pick-up with a hound on the hero takes it");
                Assert.Greater(hero.AttackSequence,swings,"A with a hound on the hero and no prompt answering doesn't swing");
                // A relay keeps answering: relighting it mid-fight is what its pulse is for.
                var relay=Beside<DiscoveryPoint>(at);relay.flag="qa.relay";relay.report="";relay.situation="";relay.dialogueNode="";relay.requiredQuest="";
                Assert.IsTrue(relay.Available,"a relay can no longer be relit in the middle of a fight");
                hound.Passive=true;
                Assert.IsTrue(key.Available&&cache.Available,"the key and the cache stay shut with nothing awake near them");
                Assert.IsTrue(pickup.Available,"a quest's pick-up stays shut with nothing awake near it");
            }
            finally
            {
                if(pad.added)InputSystem.RemoveDevice(pad);
                InputSystem.settings.backgroundBehavior=background;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode=editor;
#endif
            }
        }

        // ---------------------------------------------------------------- scri54: Orrin at the outer warp
        string[] Orrin()=>barks.Where(b=>b.who=="Orrin").Select(b=>b.line).ToArray();
        [UnityTest] public IEnumerator OrrinHailsOnceAtTheOuterWarp()
        {
            Flag("met.Orrin");Flag("warpkey");
            yield return Zone("Hub_CinderHalo","Outer");
            yield return new WaitForSecondsRealtime(3);
            Assert.IsEmpty(Orrin(),"he hails about the road to Tallow before the nursery has given the key its route");
            Flag("hushwell.nursery");
            yield return Until(()=>Orrin().Length>=2,10);
            var said=Orrin();
            Assert.AreEqual(2,said.Length,"following the objective to the outer warp, Orrin says nothing, and the one-way door goes by with no word from him");
            StringAssert.Contains("Tallow",said[0],"he doesn't say what's on the far side");
            StringAssert.Contains("back",said[0],"he doesn't say the warp doesn't run back");
            StringAssert.Contains("board",said[1],"his second line got lost");
            Assert.IsTrue(Has("orrin.outerHail"),"nothing stops the hail playing again");
            barks.Clear();
            yield return Zone("Hub_CinderHalo","Outer");
            yield return new WaitForSecondsRealtime(3);
            Assert.IsEmpty(Orrin(),"he hails again on every arrival");
        }

        // ---------------------------------------------------------------- worl79: the curtain
        [UnityTest] public IEnumerator TheCurtainPartsWithAMomentOnce()
        {
            yield return Zone("WickReach");
            bool Showing(LineRenderer s)=>s.gameObject.activeInHierarchy&&s.enabled&&s.startColor.a>.02f;
            LineRenderer[] Strands()=>All<LineRenderer>().Where(l=>l.name=="Curtain strand").ToArray();
            var strands=Strands();var gate=All<Membrane>().Single(m=>m.name=="Lantern gate");
            Assert.Greater(strands.Length,20,"fixture: the curtain has its strands");
            Assert.IsTrue(strands.All(Showing),"fixture: the closed curtain shows");
            // Each against its own start: the outer strands begin faint on purpose, so the faintest one says nothing about whether they fade.
            var full=strands.ToDictionary(s=>s,s=>s.startColor.a);
            barks.Clear();log.Clear();
            Flag("wick.relay1");Flag("wick.relay2");
            yield return Until(()=>Has("wick.gateOpen"),3);
            Assert.IsTrue(Has("wick.gateOpen"),"fixture: two relays open the gate");
            yield return null;yield return null;
            Assert.IsFalse(gate.gameObject.activeInHierarchy,"the way through has to open at once: nothing waits on a fade to fly through");
            Assert.Greater(strands.Count(Showing),strands.Length/2,"the amber curtain still vanishes in one frame, often far off screen, with only the HUD changing");
            Assert.IsTrue(barks.Any(b=>b.who=="Sela"&&b.line.Contains("Curtain")),"nobody remarks on the gate opening 30 m from the relay that opened it");
            Assert.AreEqual(1,log.Count,"the opening isn't marked once in the log");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.Less(strands.Min(s=>s.startColor.a/full[s]),.5f,"the strands don't fade");
            Assert.IsTrue(strands.Any(Showing),"the curtain is gone before anyone could see it go");
            yield return new WaitForSecondsRealtime(3);
            Assert.AreEqual(0,strands.Count(Showing),"the strands never finish going");
            // A save that already has it open: nothing there, and no ceremony.
            barks.Clear();
            yield return Zone("WickReach");
            yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(0,Strands().Count(Showing),"the opened curtain is back on a later visit");
            Assert.AreEqual(1,log.Count,"the opening plays again on load");
            Assert.IsFalse(barks.Any(b=>b.who=="Sela"&&b.line.Contains("Curtain")),"Sela announces the curtain again on load");
        }

        // ---------------------------------------------------------------- TEST_PLAN 4.5: dead relays look dark
        [UnityTest] public IEnumerator DeadRelaysLookDead()
        {
            var block=new MaterialPropertyBlock();
            foreach(var zone in new[]{"WickReach","LanternGorge"})
            {
                yield return Zone(zone);
                var fallen=All<Transform>().Where(t=>t.name.StartsWith("Fallen relay ")).ToArray();
                Assert.Greater(fallen.Length,0,zone+": fixture: the fallen relays are in the zone");
                foreach(var tower in fallen)
                {
                    var parts=tower.GetComponentsInChildren<Renderer>(true);
                    Assert.Greater(parts.Length,0,zone+": "+tower.name+" has no body");
                    foreach(var part in parts)
                    {
                        part.GetPropertyBlock(block);
                        Assert.IsTrue(block.HasVector("_EmissionColor"),zone+": "+tower.name+"'s crystal burns as bright as a relit relay, so relighting one changes nothing on screen");
                        Assert.Less(block.GetVector("_EmissionColor").magnitude,.01f,zone+": "+tower.name+"'s crystal still glows");
                    }
                }
            }
        }

        // ---------------------------------------------------------------- a rebuild keeps each zone's authored light
        [UnityTest] public IEnumerator TheChapterTwoZonesKeepTheirAuthoredLight()
        {
            foreach(var zone in new[]{"WickReach","Lanternwick","LanternGorge","SunkenRelay"})
            {
                yield return Zone(zone);
                Assert.AreEqual(UnityEngine.Rendering.AmbientMode.Trilight,RenderSettings.ambientMode,zone+" has lost its authored light: flat ambient, as a rebuild leaves it without ZoneLook.Apply, so the dusk or the cave reads flat");
                var key=All<Light>().Where(l=>l.type==LightType.Directional&&l.gameObject.scene.name==zone).ToArray();
                Assert.AreEqual(1,key.Length,zone+": fixture: the zone has one key light");
                Assert.IsFalse(key[0].color==Color.white,zone+" has lost its authored light: a white key light, as a rebuild leaves it without ZoneLook.Apply");
            }
        }

        // ---------------------------------------------------------------- boss285: the wreck
        [UnityTest] public IEnumerator TheRigStaysWhereItFell()
        {
            yield return Zone("Sorrel_Ridges");
            var wreck=All<Transform>().FirstOrDefault(t=>t.name=="Burrower wreck");
            Assert.IsNotNull(wreck,"beaten, the rig shrinks to nothing and nothing stays at the drill site to walk back past");
            yield return null;
            Assert.IsFalse(wreck.gameObject.activeSelf,"a wreck lies at the drill site before the fight");
            Assert.IsEmpty(wreck.GetComponentsInChildren<Collider>(true),"the wreck has a collider the site's navigation wasn't baked with, so it can trap a route");
            var lead=PartyController.Current.Active;
            foreach(var hero in PartyController.Current.members){Put(hero,new Vector3(hero==lead?2:-2,.2f,162));hero.Health.maximum=hero.Health.integrity=9000;}
            All<EncounterVolume>().Single(e=>e.encounterId=="Sorrel_Burrower").Begin();yield return null;
            var rig=All<EnemyBrain>().Single(e=>e.definition.id=="Burrower");rig.Passive=true;
            yield return null;
            // Off to the east, away from where a later visit finds it, so a wreck that didn't take over from the body would show.
            var cc=rig.GetComponent<CharacterController>();cc.enabled=false;rig.transform.position=new Vector3(6,rig.transform.position.y,168);cc.enabled=true;Physics.SyncTransforms();
            var fell=rig.transform.position;var body=rig.gameObject;
            rig.Health.Receive(new DamagePacket{source=lead.Health,amount=rig.Health.maximum*50,type=DamageType.Plasma});
            float deadline=Time.unscaledTime+8;
            while(body!=null&&Time.unscaledTime<deadline)
            {
                Assert.IsFalse(wreck.gameObject.activeSelf&&body.transform.localScale.x>=.9995f,"a second rig pops in beside the one still falling");
                yield return null;
            }
            Assert.IsTrue(body==null,"fixture: the fighter's body leaves");
            Assert.IsTrue(wreck.gameObject.activeSelf,"beaten, the rig shrinks to nothing and nothing takes its place");
            var lying=wreck.Find("Rig");Assert.IsNotNull(lying,"the wreck has no rig");
            Assert.Less(Flat(lying.position,fell),1.5f,"the wreck appears somewhere other than where the rig fell");
            Assert.IsFalse(wreck.Find("Thrown parts").gameObject.activeSelf,"parts appear from nowhere beside the fallen rig");
            var block=new MaterialPropertyBlock();var skin=lying.GetComponentInChildren<Renderer>();skin.GetPropertyBlock(block);
            Assert.IsFalse(block.isEmpty,"nothing turns the wreck's seams off");
            Assert.Less(block.GetVector("_EmissionColor").magnitude,.01f,"the wreck's maw still glows as if it were running");
            // Walking back past it on a later visit.
            yield return Zone("Sorrel_Ridges");
            wreck=All<Transform>().First(t=>t.name=="Burrower wreck");
            Assert.IsTrue(wreck.gameObject.activeInHierarchy,"after a reload nothing is left at the drill site");
            Assert.IsTrue(wreck.position.z>153&&wreck.position.z<191,"the wreck isn't in the excavation");
            Assert.IsTrue(wreck.Find("Thrown parts").gameObject.activeSelf,"a later visit finds the rig with nothing thrown clear of it");
            Assert.Greater(wreck.GetComponentsInChildren<Renderer>().Length,1,"the wreck has nothing to show");
            Assert.IsEmpty(wreck.GetComponentsInChildren<Collider>(true),"the wreck has a collider the site's navigation wasn't baked with, so it can trap a route");
            Assert.Less(wreck.GetComponentsInChildren<Renderer>().Max(r=>r.bounds.max.x),-2,"the wreck lies across the line every route walks through the excavation, and a body walks through it");
        }
    }
}
