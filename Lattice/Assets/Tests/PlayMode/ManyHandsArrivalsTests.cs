using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
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
    /// <summary>What comes to live aboard Many Hands after chapter 5, in the built scene, as a player docks at each stage of the story: each arrival absent before the talk that
    /// brings it and there from the next load on, never in the navigation bake while hidden and cutting the paths once there, every prompt still walkable at every stage, the freight
    /// hall's fight never sharing its room with the herd or the forge, the arrivals' people off every way and out of the camera's line to every talk, the herd walking and the forge
    /// burning and both heard, and the named people who came aboard standing in their places and saying their chapter's lines. Places and flags are the builder's
    /// (ManyHandsBuilder.Arrivals.cs; docs/maps/ManyHands.md, "Arrivals as built").</summary>
    public sealed class ManyHandsArrivalsTests
    {
        readonly List<string> heard=new();
        void Talked(string node,string speaker,System.Action<bool> done)=>heard.Add(node);
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);heard.Clear();Npc.TalkRequested+=Talked;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Npc.TalkRequested-=Talked;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the stages, and what is aboard at each (the builder's flags)
        const string Zone="ManyHands",Prefix="Arrivals: ";
        /// <summary>Chapter 9's flags by hand from its manifest (docs/writing/pending/ch09-manifest.md, section 3). Midway through it (after Nacre) is chapter 8's loadout and these:
        /// DevLoadout chapter10 tells chapter 9 to its launch (HouseLeaves), which is the stage after it.</summary>
        static readonly string[] Nacre={"home.nacreReached","home.odaFound","home.tugFound","home.navigatorsFound","home.noticeHeard","home.chartLent","home.crawlNamed"},
            Launched={"home.returned","home.meetingHeld","home.launched","quest.PortableBenches.complete"};
        /// <summary>A stage: the loadout a party docks with (DevLoadout chapterN has every earlier chapter told) and what else its story has set.</summary>
        static readonly (string name,string loadout,string[] flags)[] Stages=
        {
            ("after chapter 5","chapter6",new string[0]),("after chapter 6","chapter7",new string[0]),("after chapter 7","chapter8",new string[0]),("after chapter 8","chapter9",new string[0]),
            ("chapter 9 after Nacre","chapter9",Nacre.Where(f=>f!="home.crawlNamed").ToArray()),("after chapter 9","chapter10",Nacre.Concat(Launched).ToArray()),
        };
        /// <summary>Each arrival's root and the stages it is aboard at (by index into <see cref="Stages"/>).</summary>
        static readonly (string root,int from,int until)[] Aboard=
        {
            ("Idra's cutter on the visitors' pad",1,5),("Idra's cutters lashed off the stern",1,3),("the herd in the freight hall",1,5),("Iru's lamps on the gallery",1,5),("Idra's crew in the galley",1,5),
            ("Ishen's archive in the cabins",2,5),("the gardener's bed by Marrow-1",2,5),("the release posted in the galley",2,5),("Oriel's lantern on the gallery",2,5),
            ("Tovra's forge in the freight hall",3,5),("Drask's cell on the freight yard",3,5),
            ("Oda's cabin",4,5),("Oda and her mother at their cabin",4,4),("Bryn's navigators at the chart table",4,5),("the tug Welcome Back off the stern",4,5),("Keeper Abri's wall of names",4,5),
            ("Nacre's families in the freight yard",4,5),("Orrin's board under the gallery",5,5),("Ket's bench in the arcade",5,5),
        };

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static float Flat(Vector3 a,Vector2 b)=>Vector2.Distance(new Vector2(a.x,a.z),b);
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>A party docking at a stage of the story: a new game, the stage's loadout and flags, the city loaded at a spawn, and a moment for anything that carves the paths.</summary>
        static IEnumerator Dock(string loadout,IEnumerable<string> flags,string spawn="Arrival")
        {
            GameServices.Current.NewGame();DevLoadout.Apply(loadout);foreach(string f in flags)Flag(f);
            SceneFlow.Current.LoadZone(Zone,spawn);float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: Many Hands never finished loading");
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.8f);
        }
        static IEnumerator Dock(int stage,params string[] more)=>Dock(Stages[stage].loadout,Stages[stage].flags.Concat(more));
        static GameObject Root(string what)=>All<Transform>().Where(t=>t.parent==null&&t.name==Prefix+what).Select(t=>t.gameObject).SingleOrDefault();
        /// <summary>What stands wrong at a stage: an arrival missing that should be aboard, or aboard that shouldn't be yet (or any more).</summary>
        static List<string> Wrong(int stage)
        {
            var wrong=new List<string>();
            foreach(var (root,from,until) in Aboard)
            {
                var go=Root(root);if(go==null){wrong.Add($"\"{root}\" isn't in the scene at all");continue;}
                bool should=stage>=from&&stage<=until;
                if(go.activeInHierarchy!=should)wrong.Add(should?$"\"{root}\" isn't aboard {Stages[stage].name}":$"\"{root}\" is aboard {Stages[stage].name}, before the talk that brings it (or after it has gone)");
            }
            return wrong;
        }
        static NavMeshPath path=new();
        static bool Walkable(Vector3 from,Vector3 to,float near=2.5f)=>NavMesh.SamplePosition(to,out var goal,near,NavMesh.AllAreas)&&NavMesh.CalculatePath(from,goal.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        static Vector3 Start(){Assert.IsTrue(NavMesh.SamplePosition(All<SpawnPoint>().Single(s=>s.id=="Arrival").transform.position,out var hit,2,NavMesh.AllAreas),"fixture: no floor at the arrival");return hit.position;}
        static Vector3 On(Vector2 p,float y=0){return Physics.Raycast(new Vector3(p.x,y+6,p.y),Vector3.down,out var hit,12,~0,QueryTriggerInteraction.Ignore)?hit.point:new Vector3(p.x,y,p.y);}
        /// <summary>Every prompt a player could be offered, with a walkable point inside its reach that has a whole path from the arrival.</summary>
        static List<string> Unreachable(string stage)
        {
            var start=Start();var cut=new List<string>();
            foreach(var prompt in Object.FindObjectsByType<InteractionPrompt>(FindObjectsSortMode.None))
            {
                if(!prompt.isActiveAndEnabled)continue;var at=prompt.transform.position;bool reached=false;
                for(int k=0;k<9&&!reached;k++)
                {
                    var probe=k==0?at:at+Quaternion.Euler(0,k*45,0)*Vector3.forward*prompt.range*.7f;
                    if(NavMesh.SamplePosition(probe,out var hit,Mathf.Max(.6f,prompt.range*.5f),NavMesh.AllAreas)&&Vector3.Distance(new Vector3(hit.position.x,at.y,hit.position.z),at)<=prompt.range+.3f
                        &&NavMesh.CalculatePath(start,hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)reached=true;
                }
                if(!reached)cut.Add($"{stage}: \"{prompt.prompt}\" ({prompt.name}) at ({at.x:0.0}, {at.z:0.0}) can't be walked to");
            }
            return cut;
        }

        // ================================================================== each arrival, as the party docks
        [UnityTest] public IEnumerator EachArrivalIsThereFromTheDockingAfterTheTalkThatBringsItAndNotBefore()
        {
            // The check itself, shown failing first: the herd and Idra's crew set aboard a chapter early (chapter 6's closing flag on the chapter 6 arrival) must be named.
            yield return Dock(0,"chapter6.complete");
            var early=Wrong(0);
            Assert.IsTrue(early.Any(w=>w.Contains("the herd"))&&early.Any(w=>w.Contains("Idra's crew")),"fixture: the check didn't catch arrivals brought aboard a chapter early: "+string.Join("; ",early));
            var problems=new List<string>();
            for(int stage=0;stage<Stages.Length;stage++)
            {
                yield return Dock(stage);
                problems.AddRange(Wrong(stage));
                problems.AddRange(Unreachable(Stages[stage].name));
            }
            Assert.IsEmpty(problems,"The city at each stage:\n"+string.Join("\n",problems));
        }

        [UnityTest] public IEnumerator WhatComesWithASideStoryWaitsForItAndChapterNinesPeopleKeepTheirOwnHours()
        {
            // The gardener's planted bed and Oriel's lantern are each a side story's (the Empty Gardens, the Far End); more smiths come with Quiet Hands.
            yield return Dock(2);
            foreach(string part in new[]{"the gardener's bed, planted","Oriel's lantern, lit"})Assert.IsFalse(Root("the gardener's bed by Marrow-1").transform.root.GetComponentsInChildren<Transform>(true).Concat(Root("Oriel's lantern on the gallery").GetComponentsInChildren<Transform>(true)).Any(t=>t.name==Prefix+part&&t.gameObject.activeInHierarchy),part+" is there without its side story done");
            yield return Dock(2,"quest.EmptyGardens.complete","quest.LumesLantern.complete");
            foreach(var (root,part) in new[]{("the gardener's bed by Marrow-1","the gardener's bed, planted"),("Oriel's lantern on the gallery","Oriel's lantern, lit")})
                Assert.IsTrue(Root(root).GetComponentsInChildren<Transform>().Any(t=>t.name==Prefix+part),part+" isn't there with its side story done");
            Assert.IsTrue(All<Light>().Any(l=>l.isActiveAndEnabled&&Flat(l.transform.position,new Vector2(-17,56.3f))<1.5f),"Oriel's lantern is up and gives no light");
            yield return Dock(3);
            Assert.IsFalse(Root("Tovra's forge in the freight hall").GetComponentsInChildren<Transform>().Any(t=>t.name==Prefix+"more smiths aboard"),"Quiet Hands' smiths are aboard without the quest");
            yield return Dock(3,"quest.QuietHands.complete");
            Assert.IsTrue(Root("Tovra's forge in the freight hall").GetComponentsInChildren<Transform>().Any(t=>t.name==Prefix+"more smiths aboard"),"Quiet Hands is done and its smiths aren't aboard");
            // Oda and her mother go home for the Halo's last day; their frame stays on the wall. Sorrel goes up on Orrin's board only with the chapter's last talk.
            yield return Dock(5);
            Assert.IsTrue(Root("Oda's cabin").activeInHierarchy&&!Root("Oda and her mother at their cabin").activeInHierarchy,"Oda's cabin isn't as she left it for the Halo");
            Assert.IsTrue(Root("Orrin's board under the gallery").GetComponentsInChildren<Transform>().Any(t=>t.name==Prefix+"Sorrel on Orrin's board"),"chapter 9 is over and Sorrel isn't on the board");
            Assert.IsFalse(All<GameObject>().Any(g=>g.name=="Herb stall"&&g.activeInHierarchy),"the herb stall still stands where Orrin's board is");
        }

        // ================================================================== the bake, and the paths once they're there
        [UnityTest] public IEnumerator NothingHiddenIsInTheBakeAndWhatArrivesCutsThePaths()
        {
            // Points inside what arrives solid: inside the herd's pen, the forge's mending table and anvil, inside Drask's cell, the archive's desk, the moved herb stall.
            var inside=new[]{("the herd's pen",new Vector2(19.6f,-35.6f)),("Drask's cell",new Vector2(42.2f,-59.5f))};
            var under=new[]{("the forge's mending table",new Vector2(43,-34.2f)),("the forge's anvil",new Vector2(46.2f,-29.6f)),("the archive's reading desk",new Vector2(-31.3f,40.6f)),("the herb stall's new place",new Vector2(21.6f,33.6f))};
            yield return Dock(0);
            var start=Start();var problems=new List<string>();
            foreach(var (what,at) in inside)if(!Walkable(start,On(at),.6f))problems.Add($"before anything arrives, {what}'s floor at {at} can't be walked to: something hidden was baked");
            foreach(var (what,at) in under)if(!NavMesh.SamplePosition(On(at),out _,.4f,NavMesh.AllAreas))problems.Add($"before anything arrives, there's no floor under {what} at {at}: something hidden was baked");
            // And no hidden solid of theirs anywhere in the bake: the floor under every one of their boxes that isn't by a wall is walkable.
            foreach(var (root,_,_) in Aboard)foreach(var box in Root(root).GetComponentsInChildren<BoxCollider>(true).Where(b=>!b.isTrigger&&b.attachedRigidbody==null))
            {
                // Measured from its transform: a switched-off collider's bounds are an empty box at its object's place (a rail's at rail height, so every sample missed the floor).
                var c=box.transform.TransformPoint(box.center);var s=Vector3.Scale(box.size,box.transform.lossyScale);s=new Vector3(Mathf.Abs(s.x),Mathf.Abs(s.y),Mathf.Abs(s.z));
                if(Mathf.Max(s.x,s.z)>3||c.y>3.2f&&c.y<5||Physics.CheckBox(new Vector3(c.x,c.y,c.z),new Vector3(.9f,.2f,.9f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))continue;
                if(!NavMesh.SamplePosition(new Vector3(c.x,c.y-s.y/2+.1f,c.z),out _,.5f,NavMesh.AllAreas))problems.Add($"{root}: there's a hole in the paths under its {box.name} at ({c.x:0.0}, {c.z:0.0}) while it isn't there");
            }
            Assert.IsEmpty(problems,string.Join("\n",problems));
            yield return Dock(5);yield return new WaitForSecondsRealtime(.5f);
            start=Start();
            foreach(var (what,at) in inside)if(Walkable(start,On(at),.6f))problems.Add($"once it's aboard, {what} at {at} can still be walked into: its fence or bars don't cut the paths");
            foreach(var (what,at) in under)if(NavMesh.SamplePosition(On(at),out var hit,.25f,NavMesh.AllAreas)&&Flat(hit.position,at)<.25f)problems.Add($"once it's aboard, the paths still run through {what} at {at}");
            Assert.IsEmpty(problems,string.Join("\n",problems));
        }

        // ================================================================== the freight hall's fight
        [UnityTest] public IEnumerator TheFreightHallsFightNeverSharesItsRoomWithTheHerdOrTheForge()
        {
            // The window the hall is a fight (the frame in through the roof, its operator not yet down), with every later chapter's flag set as well: the hall is still the fight's.
            var during=new[]{"hands.ruvaHeard","hands.maruHeard","hands.mooringSettled","hands.ventFixed","hands.chartOpen","hands.jobsDone","hands.iloAboard","hands.claimRead","bossdown.Hulljack","hands.feet1","hands.feet2","hands.feet3","hands.feet4",
                "chapter6.complete","chapter7.complete","chapter8.complete","hoar.sleepLearned"};
            GameServices.Current.NewGame();DevLoadout.Apply("chapter5");foreach(string flag in during)Flag(flag);State.questSteps["ManyHands"]=6;
            SceneFlow.Current.LoadZone(Zone,"FreightHall");float deadline=Time.unscaledTime+30;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;yield return Frames(4);
            var pocket=All<FightPocket>().Single();Assert.AreEqual(1,pocket.State,"fixture: the hall isn't in its fight's window");
            foreach(string root in new[]{"the herd in the freight hall","Tovra's forge in the freight hall"})Assert.IsFalse(Root(root).activeInHierarchy,root+" is in the freight hall while its fight is on");
            var f=new Vector3(32,1,-34.5f);
            Assert.IsEmpty(Physics.OverlapCapsule(f+Vector3.up*.6f,f+Vector3.up*2,16,~0,QueryTriggerInteraction.Ignore).Where(c=>c.GetComponentInParent<CombatActor>()==null&&c.name!="Many Hands deck"&&c.transform.root.name.StartsWith(Prefix)).Select(c=>c.transform.root.name).ToArray(),
                "something of an arrival's is solid in the fight's room");
            // Once the operator is down the room is the town's again, and from the next docking theirs.
            Flag("bossdown.DessikVarn");Flag("hands.dessikYielded");SceneFlow.Current.LoadZone(Zone,"FreightHall");deadline=Time.unscaledTime+30;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;yield return Frames(4);
            Assert.AreEqual(2,All<FightPocket>().Single().State);
            foreach(string root in new[]{"the herd in the freight hall","Tovra's forge in the freight hall"})Assert.IsTrue(Root(root).activeInHierarchy,root+" isn't in the hall once its fight is long over");
            Assert.AreEqual(Lattice.Data.ZoneKind.GroundSafe,ZoneController.Current.Kind);
        }

        // ================================================================== the people who came, where people walk and where the camera looks
        /// <summary>The ways feet go (ManyHandsBuilder.Hands.Ways), as ManyHandsWorldTests holds them.</summary>
        static readonly Vector2[][] Ways=
        {
            new[]{new Vector2(-4,-60),new Vector2(0,-55),new Vector2(0,-40),new Vector2(0,-20),new Vector2(0,3),new Vector2(0,20),new Vector2(0,23),new Vector2(0,38.6f),new Vector2(0,45.2f)},
            new[]{new Vector2(-4,-60),new Vector2(-12,-59.6f),new Vector2(-22,-58.6f)},new[]{new Vector2(0,-57),new Vector2(12,-59.4f),new Vector2(32,-59),new Vector2(32,-50),new Vector2(31.4f,-47.6f)},
            new[]{new Vector2(0,-45),new Vector2(-8.6f,-45)},
            new[]{new Vector2(0,-24),new Vector2(-14.5f,-24),new Vector2(-24,-24),new Vector2(-31,-23),new Vector2(-32.5f,-22.5f),new Vector2(-49,-21),new Vector2(-56,-18.6f)},new[]{new Vector2(-24,-24),new Vector2(-29,-29),new Vector2(-42.5f,-32),new Vector2(-44.75f,-32.5f),new Vector2(-44.5f,-33.6f),new Vector2(-48,-34.3f)},
            new[]{new Vector2(-24,3),new Vector2(-40,2),new Vector2(-58,-4),new Vector2(-58,-11.5f),new Vector2(-58,-15.5f)},new[]{new Vector2(-24,-2),new Vector2(-24,-11.5f),new Vector2(-24,-19)},
            new[]{new Vector2(-34,3),new Vector2(0,3),new Vector2(50,3),new Vector2(56,2.4f),new Vector2(66,3.6f)},new[]{new Vector2(56,2.4f),new Vector2(55.6f,4.6f),new Vector2(55.6f,10.4f),new Vector2(64.8f,10.4f)},
            new[]{new Vector2(-24,3),new Vector2(-24,6)},new[]{new Vector2(-12,3),new Vector2(-12,6)},new[]{new Vector2(18,3),new Vector2(18,6)},new[]{new Vector2(29,3),new Vector2(29,8.6f)},
            new[]{new Vector2(-19,3),new Vector2(-19,-8.4f)},new[]{new Vector2(56,2.4f),new Vector2(56.3f,-3.4f),new Vector2(58.4f,-5.2f),new Vector2(74,-5.2f)},
            new[]{new Vector2(0,20),new Vector2(16,20.4f),new Vector2(27,24.2f),new Vector2(44,24.2f),new Vector2(60,24.2f),new Vector2(73.4f,24.2f)},new[]{new Vector2(44,24.2f),new Vector2(44,52)},
            new[]{new Vector2(60,24.2f),new Vector2(60,52)},new[]{new Vector2(73.4f,24.2f),new Vector2(73.4f,47.4f)},
            new[]{new Vector2(0,19.6f),new Vector2(-16,20.4f),new Vector2(-30,20),new Vector2(-52,20),new Vector2(-52,30.6f),new Vector2(-52,40.6f),new Vector2(-52,50.2f),new Vector2(-58,50.2f)},
            new[]{new Vector2(-52,30.6f),new Vector2(-70,30.6f)},new[]{new Vector2(-52,40.6f),new Vector2(-34,40.6f)},
            new[]{new Vector2(0,40.4f),new Vector2(7,45.4f)},new[]{new Vector2(0,40.4f),new Vector2(-11,42),new Vector2(-17.4f,42)},new[]{new Vector2(7,45.4f),new Vector2(12.6f,48.6f)},
        };
        static float Segment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));return Vector2.Distance(p,a+d*t);}
        static float FromWays(Vector2 p)=>Ways.Min(w=>Enumerable.Range(1,w.Length-1).Min(k=>Segment(p,w[k-1],w[k])));
        static bool Theirs(Component c)=>c.transform.root.name.StartsWith(Prefix);

        [UnityTest] public IEnumerator TheArrivalsKeepOffEveryWayAndOutOfTheCamerasLineToEveryTalk()
        {
            yield return Dock(5,"quest.EmptyGardens.complete","quest.LumesLantern.complete","quest.QuietHands.complete");
            var problems=new List<string>();
            string Coat(Component who){var skin=who.GetComponentInChildren<SkinnedMeshRenderer>(true);return skin!=null&&skin.sharedMaterial!=null?skin.sharedMaterial.name:"";}
            var theirs=All<PropTint>().Where(p=>p.gameObject.activeInHierarchy&&p.name.StartsWith("Townsperson")&&Theirs(p)).ToArray();
            Assert.Greater(theirs.Length,25,"fixture: hardly anyone has come to live aboard");
            var npcs=All<Npc>().Where(n=>n.gameObject.activeInHierarchy&&n.enabled).ToArray();
            var everyone=All<PropTint>().Where(p=>p.gameObject.activeInHierarchy&&(p.name.StartsWith("Townsperson")||p.name.StartsWith("A child"))&&p.GetComponent<WalkLoop>()==null).ToArray();
            foreach(var p in theirs)
            {
                var at=new Vector2(p.transform.position.x,p.transform.position.z);
                if(FromWays(at)<1.6f)problems.Add($"{p.name} stands on a way people walk, at {at}");
                foreach(var npc in npcs)if(Vector3.Distance(npc.transform.position,p.transform.position)<1.4f)problems.Add($"{p.name} stands on {npc.speaker} at {at}");
                foreach(var other in everyone)if(other!=p&&Vector3.Distance(other.transform.position,p.transform.position)<3.2f&&Coat(other)==Coat(p)&&!Coat(p).StartsWith("Idra"))problems.Add($"{p.name} and {other.name} wear one body side by side at {at}");
            }
            // Between the fixed camera and every talk aboard: nothing of theirs stands across a speaker's chest.
            var toCamera=-(Quaternion.Euler(40,20,0)*Vector3.forward);
            foreach(var npc in npcs)
            {
                var chest=npc.transform.position+Vector3.up;
                foreach(var hit in Physics.RaycastAll(chest+toCamera*19.5f,-toCamera,19.5f-.8f,~0,QueryTriggerInteraction.Ignore))
                    if(Theirs(hit.collider)&&hit.collider.GetComponentInParent<PropTint>()==null&&hit.collider.bounds.size.y>.8f)problems.Add($"{hit.collider.transform.root.name}'s {hit.collider.name} stands between the camera and {npc.speaker}");
            }
            Assert.IsEmpty(problems,string.Join("\n",problems.Distinct()));
        }

        // ================================================================== the herd walks, the forge burns, and both are heard
        [UnityTest] public IEnumerator TheHerdWalksItsPenAndTheForgeBurnsAndBothAreHeardOnlyNearThem()
        {
            yield return Dock(3);
            var herd=Root("the herd in the freight hall").GetComponentInChildren<RimeHerd>();Assert.IsNotNull(herd,"the herd has no beasts walking");
            Assert.GreaterOrEqual(herd.beasts.Length,5,"fewer than five beasts in the herd");
            var before=herd.beasts.Select(b=>b.position).ToArray();
            // The party walks up to the pen's gate at a walk, and the herd doesn't startle: it's their pen, not the Reach.
            var lead=Lead;var body=lead.GetComponent<CharacterController>();body.enabled=false;lead.transform.position=On(new Vector2(28.6f,-43));body.enabled=true;Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(4);
            Assert.GreaterOrEqual(herd.beasts.Where((b,i)=>Vector3.Distance(b.position,before[i])>.6f).Count(),herd.beasts.Length-1,"the beasts stand still in their pen");
            Assert.AreEqual(0,herd.Startles,"the herd bolted in its pen");
            foreach(var b in herd.beasts){var at=new Vector2(b.position.x,b.position.z);Assert.IsTrue(at.x>12.9f&&at.x<27&&at.y<-22.2f&&at.y>-48.2f,$"a beast walked out of the pen to {at}");}
            // The forge: its coals and its light breathing, sparks, and the sound of it.
            var rhythm=Root("Tovra's forge in the freight hall").GetComponentInChildren<LampRhythm>();Assert.IsNotNull(rhythm,"the forge's hearth has no fire in it");
            // Watched for 1.5 s of game time, ten of the flicker's 0.14 s steps: forty frames could all fall inside one step when frames come fast.
            var light=rhythm.lights.Single();float low=9,high=0,until=Time.time+1.5f,cap=Time.realtimeSinceStartup+10;
            while(Time.time<until&&Time.realtimeSinceStartup<cap){yield return null;low=Mathf.Min(low,light.intensity);high=Mathf.Max(high,light.intensity);}
            Assert.Greater(high-low,.1f,"the hearth's light never moves");Assert.Greater(low,.5f,"the hearth goes dark between flickers");
            foreach(var (root,clip) in new[]{("the herd in the freight hall","AMB_HandsHerd"),("Tovra's forge in the freight hall","AMB_HandsForge")})
            {
                var source=Root(root).GetComponentsInChildren<AudioSource>().SingleOrDefault();Assert.IsNotNull(source,root+" makes no sound");
                Assert.AreEqual(clip,source.clip!=null?source.clip.name:"");Assert.IsTrue(source.isPlaying,root+"'s sound isn't playing");Assert.IsTrue(source.loop);
                Assert.AreEqual(1,source.spatialBlend,root+" is heard across the whole ship");Assert.LessOrEqual(source.maxDistance,32,root+" carries too far");
                Assert.AreEqual("Ambience",source.outputAudioMixerGroup!=null?source.outputAudioMixerGroup.name:"",root+"'s sound isn't on the ambience bus, so the player's slider doesn't reach it");
            }
            // Before the forge has come, the hall is quiet of it.
            yield return Dock(1);
            Assert.IsFalse(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Any(s=>s.isPlaying&&s.clip!=null&&s.clip.name=="AMB_HandsForge"),"the forge is heard before it has come aboard");
        }

        // ================================================================== the named people who came aboard, in their places, saying their chapter's lines
        [UnityTest] public IEnumerator TheNamedPeopleWhoCameAboardStandInTheirPlacesAndSayTheirChaptersLines()
        {
            // Where each place has room for its person (ManyHandsBuilder.Arrivals: HaskAt, IruAt, IshenAt, TovraAt, BrekkAt, DraskAt), and what they say aboard (each chapter's manifest).
            var places=new (string speaker,int stage,Vector2 at,float within,string node)[]
            {
                ("Hask",1,new(28.5f,-48),2.5f,"HaskAboard"),("Iru",1,new(15.4f,46.4f),2.5f,"IruAboard"),("Ishen",3,new(-33.2f,42.3f),2.5f,"IshenAboard"),
                // Chapter 8's people the docking after it (from chapter 9's Oda, Drask has his count to ask: DraskCount).
                ("Tovra",3,new(46.2f,-27.9f),2.5f,"TovraAboard"),("Brekk",3,new(48.6f,-26.2f),2.5f,"BrekkAboard"),("Drask",3,new(42.2f,-58.8f),2,"DraskAboard"),
            };
            var problems=new List<string>();int lastStage=-1;
            foreach(var (speaker,stage,at,within,node) in places)
            {
                if(stage!=lastStage){yield return Dock(stage);lastStage=stage;}
                var here=All<Npc>().Where(n=>n.speaker==speaker&&n.gameObject.activeInHierarchy&&n.enabled).ToArray();
                if(here.Length==0){problems.Add($"{speaker} isn't aboard {Stages[stage].name} (their chapter's StageManyHands puts them there)");continue;}
                if(here.Length>1){problems.Add($"{speaker} stands in {here.Length} places at once");continue;}
                var npc=here[0];if(Flat(npc.transform.position,at)>within)problems.Add($"{speaker} stands at ({npc.transform.position.x:0.0}, {npc.transform.position.z:0.0}), not in their place aboard at {at}");
                heard.Clear();DialogueSystem.Current.AutoAdvance=true;npc.Interact();yield return null;
                float deadline=Time.unscaledTime+30;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;yield return Frames(3);
                if(heard.FirstOrDefault()!=node)problems.Add($"{speaker} says {heard.FirstOrDefault()??"nothing"} aboard, not {node}");
            }
            // The gardener's body at its planted bed, with the planted bed done.
            yield return Dock(2,"quest.EmptyGardens.complete");
            var gardener=All<GameObject>().FirstOrDefault(g=>g.name=="The gardener"&&g.activeInHierarchy);
            if(gardener==null)problems.Add("the gardener isn't aboard with the Empty Gardens done");else if(Flat(gardener.transform.position,new Vector2(41.8f,49.7f))>2)problems.Add($"the gardener stands at ({gardener.transform.position.x:0.0}, {gardener.transform.position.z:0.0}), not at its planted bed by Marrow-1 (41.8, 49.7)");
            Assert.IsEmpty(problems,string.Join("\n",problems));
        }
    }
}
