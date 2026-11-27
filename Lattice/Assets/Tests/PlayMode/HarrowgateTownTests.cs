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
    /// <summary>Harrowgate as a player meets it, in the built scene: the harbour gate that waits for a vouched flag, Pip who waits inside it and goes where the party goes until the oath
    /// bell, the kitchen that doesn't exist and the quarter behind it, the trophy cases and their key, the Warmaster at his table, the muster field built to the duel's contract and a
    /// combat place only while the duel is owed, the town standing on the field after the pursuit, and the end down the stair to the cells and the radio. What every zone gets from the
    /// shared checks is theirs and not repeated here. Coordinates are the map brief's (docs/maps/Harrowgate.md), which are the builder's own (HarrowgateBuilder.Town).</summary>
    public sealed class HarrowgateTownTests
    {
        readonly List<string> heard=new();
        void Talked(string node,string speaker,System.Action<bool> done)=>heard.Add(node);
        void Looked(DiscoveryPoint point){if(!string.IsNullOrEmpty(point.dialogueNode))heard.Add(point.dialogueNode);}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter8");heard.Clear();Npc.TalkRequested+=Talked;DiscoveryPoint.DialogueRequested+=Looked;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Npc.TalkRequested-=Talked;DiscoveryPoint.DialogueRequested-=Looked;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what this class writes by hand for the shared tests
        [ContentRegistry.Expectations] static void HarrowgateTown(ContentRegistry.Book book)
        {
            // The forecourt's launch after the Warmaster's skiff: "Answer the Warmaster on the muster field first".
            book.Lock("vask.duelWon","Warmaster","muster field");
        }

        // ------------------------------------------------------------------ the town's places, on the plan (HarrowgateBuilder.Town's constants)
        static readonly Vector2 Arrival=new(-40,8),GateHead=new(8,28),PipWaits=new(11.6f,30.6f),PipFirstAt=new(8,31f),HessaGate=new(16.6f,14.6f),KitchenDoor=new(-25,40),OdrunAt=new(-21.8f,46.5f),BrekkAt=new(-24,53.4f),
            F=new(0,87),DuelMiddle=new(0,85),GuardPost=new(14,95),PipThirdFile=new(-20.2f,80),FieldSpawn=new(0,64),VerdictSpawn=new(-4,80),DraskAt=new(0,109.4f),CellAt=new(56,110.6f),CellFront=new(56,107.4f),
            ArmouryGate=new(47.6f,80.25f),StairMid=new(50.8f,88.5f),Lookout=new(54,121),PipAfterAt=new(-21.8f,38.6f);
        const float FieldHalfX=17.5f,FieldSouth=72.5f,FieldNorth=97.5f,Upper=4.8f;

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static T[] Live<T>()where T:Component=>All<T>().Where(c=>c.gameObject.activeInHierarchy).ToArray();
        static float Flat(Vector3 a,Vector2 b)=>Vector2.Distance(new Vector2(a.x,a.z),b);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>The story at the town's gate: the Foundry done, Tovra's road said.</summary>
        static void AtTheGate(){foreach(string f in new[]{"vask.lockAdmitted","vask.picketCrossed","vask.lockRemoved","vask.unswornMet","vask.vessaBeaten","bossdown.SlagColossus","vask.juvenileFreed","vask.roadSaid"})Flag(f);}
        /// <summary>In under the flag, the sisters heard.</summary>
        static void InTown(){AtTheGate();Flag("vask.sistersHeard");Flag("vask.parley");Flag("met.Hessa");}
        static IEnumerator Zone(string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone("Harrowgate",spawn);float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"Harrowgate never finished loading");
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.7f);
        }
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static Vector3 Floor(Vector2 at)
        {
            Assert.IsTrue(NavMesh.SamplePosition(new Vector3(at.x,8,at.y),out var hit,9,NavMesh.AllAreas),$"fixture: no floor near ({at.x}, {at.y})");
            return hit.position;
        }
        /// <summary>A whole path over the town's floor from the arrival, with every door and seal as it stands.</summary>
        static bool Reached(Vector2 to)
        {
            var path=new NavMeshPath();
            return NavMesh.SamplePosition(new Vector3(Arrival.x,1,Arrival.y),out var from,2,NavMesh.AllAreas)&&NavMesh.SamplePosition(new Vector3(to.x,8,to.y),out var goal,9,NavMesh.AllAreas)
                &&NavMesh.CalculatePath(from.position,goal.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        }
        static Npc[] Here(string speaker)=>All<Npc>().Where(n=>n.speaker==speaker&&n.gameObject.activeInHierarchy).ToArray();
        static DiscoveryPoint Point(string flag)=>All<DiscoveryPoint>().Single(p=>p.flag==flag);
        static IEnumerator Settle(){float deadline=Time.unscaledTime+30;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;Assert.IsFalse(DialogueSystem.Current.Running,"a conversation never ended");yield return new WaitForSecondsRealtime(.3f);}
        /// <summary>Walks the lead across a crossing box from just short of it: what a player does.</summary>
        static IEnumerator Cross(DiscoveryPoint line,Vector2 from)
        {
            DialogueSystem.Current.AutoAdvance=true;Put(Lead,Floor(from));yield return Frames(2);Put(Lead,line.transform.position+Vector3.up*.1f);
            float deadline=Time.unscaledTime+4;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            yield return Settle();
        }

        // ================================================================== the town and its ways
        [UnityTest] public IEnumerator HarrowgateIsASafeTownWithItsWaysOutAndEveryServiceItsChartStopLists()
        {
            InTown();yield return Zone();
            Assert.AreEqual(ZoneKind.GroundSafe,ZoneController.Current.definition.kind,"Harrowgate isn't a safe town");
            var ways=All<WarpBeacon>().Select(w=>(w.scene,w.spawn)).Concat(All<DockingPad>().Select(d=>(d.scene,d.spawn))).ToArray();
            CollectionAssert.Contains(ways,("ScoriaFlows","Harrowgate"),"there's no road back to the Flows");CollectionAssert.Contains(ways,("PicketOfTeeth","Harrowgate"),"there's no pad up to the Picket");
            Assert.IsNotNull(Object.FindFirstObjectByType<Bench>(),"the chart says Harrowgate has a bench");Assert.IsNotNull(Object.FindFirstObjectByType<RepairBay>(),"the chart says Harrowgate has a mender");
            Assert.IsNotNull(Object.FindFirstObjectByType<GroundNavigation>(),"Harrowgate has no navigation");
            foreach(var (name,at) in new[]{("the gate's head",GateHead),("Odrun's door",KitchenDoor),("Brekk in the quarter",BrekkAt+new Vector2(0,-1.6f)),("the field",F),("the Warmaster's table",DraskAt+new Vector2(0,-1.6f)),("the lookout",Lookout)})
                Assert.IsTrue(Reached(at),$"{name} can't be walked to from the quay once the flag is vouched");
        }

        [UnityTest] public IEnumerator TheHarbourGateIsShutUntilTheFlagIsVouchedAndTheSistersAreHeardOnTheWayUp()
        {
            AtTheGate();yield return Zone();
            var shut=All<FlagGate>().Single(g=>g.name.StartsWith("The harbour gate is shut")).targets[0];var open=All<FlagGate>().Single(g=>g.name.StartsWith("The harbour gate stands open")).targets[0];
            Assert.IsTrue(shut.activeInHierarchy&&!open.activeInHierarchy,"the harbour gate stands open before anyone has vouched for the flag");
            yield return Frames(3);Assert.IsFalse(Reached(GateHead),"the town can be walked into through a shut gate");
            var vessa=All<LeavesAfter>().Single(l=>l.name=="Vessa at the harbour gate");Assert.IsTrue(vessa.gameObject.activeInHierarchy,"Vessa isn't at the gate on the way up");
            var sisters=Point("vask.sistersHeard");Assert.AreEqual("HessaSisters",sisters.dialogueNode);Assert.IsTrue(sisters.startsOnCrossing);
            yield return Cross(sisters,Arrival+new Vector2(18,0));
            Assert.Contains("HessaSisters",heard,"walking up the quay to the gate doesn't overhear the sisters");Assert.IsTrue(Has("vask.sistersHeard"));
            var hessa=Here("Hessa");Assert.AreEqual(1,hessa.Length,"Hessa isn't on the gate, or is in two places");Assert.Less(Flat(hessa[0].transform.position,HessaGate),1);
            Assert.AreEqual("HessaParley",hessa[0].firstNode);Assert.AreEqual("HessaGateRepeat",hessa[0].repeatNode);
            Flag("vask.parley");yield return Frames(4);
            Assert.IsTrue(!shut.activeInHierarchy&&open.activeInHierarchy,"the gate didn't open once the flag was vouched");Assert.IsTrue(Reached(GateHead),"the town can't be walked into once the flag is vouched");
        }

        [UnityTest] public IEnumerator PipWaitsInsideTheGateGoesWhereThePartyGoesAndTakesHisFileWhenTheBellRings()
        {
            InTown();yield return Zone();
            var pip=All<KeepsPace>().Single(k=>k.name=="Pip");yield return Frames(3);
            Assert.AreEqual(0,pip.Current,"Pip isn't waiting inside the gate before his first word");Assert.Less(Flat(pip.transform.position,PipWaits),1.5f,"Pip isn't waiting at the gate's head");
            var first=Point("vask.pipMet");Assert.AreEqual("PipFirst",first.dialogueNode);Assert.AreEqual("vask.parley",first.requiredFlag);Assert.Less(Flat(first.transform.position,PipFirstAt),.05f,"Pip's first word isn't just inside the gate, three metres past its head");
            // His places are offered only while he is with the party.
            var places=All<DiscoveryPoint>().Where(p=>p.dialogueSpeaker=="Pip"&&!p.startsOnCrossing).ToArray();Assert.AreEqual(7,places.Length,"Pip doesn't stop at his seven places");
            foreach(var place in places)Assert.IsFalse(place.Wanted,$"\"{place.prompt}\" is offered before Pip has attached himself");
            Flag("vask.pipMet");yield return Frames(3);Assert.AreEqual(1,pip.Current,"Pip doesn't go with the party once he has spoken");
            Assert.IsTrue(places.Single(p=>p.dialogueNode=="PipWashing").Wanted,"his place under the washing isn't offered while he's with the party");
            Flag("vask.draskMet");yield return Frames(3);Assert.AreEqual(2,pip.Current,"Pip doesn't go to his file when the oath bell rings");
            foreach(var place in places)Assert.IsFalse(place.isActiveAndEnabled,$"\"{place.prompt}\" is still offered once Pip is in his file");
            Flag("vask.deposed");Flag("vask.pursuitWon");yield return Zone();
            Assert.IsFalse(All<KeepsPace>().Single(k=>k.name=="Pip").gameObject.activeInHierarchy,"Pip still follows the party after the verdict");
            var after=Here("Pip");Assert.AreEqual(1,after.Length,"Pip isn't at Odrun's door after the verdict");Assert.Less(Flat(after[0].transform.position,PipAfterAt),1);
            Assert.AreEqual("pip.oathLeft",after[0].postFlag);Assert.AreEqual("PipPost",after[0].postNode);
        }

        /// <summary>Round seven's chain, nov26-r7c-chain s50/063 (W9-harrowgate-02): Pip's first word opened with the lead at z 28.27, in the shadow of the gate's arch (its cap 7.35 m
        /// up at z 23.8) and towers, and neither Pip nor Idra in view. Its crossing (9 m by 2.4) is moved 1.4 m north, to (8, 31): walked north up the route's line from the ramp's
        /// head the talk opens with the lead at or past z 29.6, where he is seen whole; and walked at Pip from the ramp's head, or from due south of him, it still opens before he is
        /// within 0.9 m of the boy (at 31.4 or more it doesn't). As it was: the built scene can't be switched, so the first pass moves the crossing's object back to z 29.6, where the
        /// talk opens at about z 28.3. Walked at sixty frames a second, as the player runs, with physics at its own fifty.</summary>
        [UnityTest] public IEnumerator PipsFirstWordOpensWithTheLeadPastTheGatesArchAndBeforeHeReachesPip()
        {
            const float Was=29.6f;var head=new Vector2(8,27);var opened=new Vector2[4];var pips=new Vector2[4];
            for(int pass=0;pass<4;pass++)
            {
                Flag("vask.pipMet",false);Flag("met.Pip",false);InTown();yield return Zone();
                var first=Point("vask.pipMet");var line=first.transform.position;
                if(pass==0){first.transform.position=new Vector3(line.x,line.y,Was);Physics.SyncTransforms();}
                else Assert.Less(Flat(line,PipFirstAt),.05f,"Pip's first word isn't where the builder puts it");
                var at=All<KeepsPace>().Single(k=>k.name=="Pip").transform.position;pips[pass]=new Vector2(at.x,at.z);
                var from=pass==3?new Vector2(pips[pass].x,27.6f):head;var to=pass<2?new Vector2(8,34):pips[pass];
                Assert.Less(Flat(Floor(from),from),.3f,$"fixture: no floor at ({from.x}, {from.y}) to walk from");
                heard.Clear();opened[pass]=new Vector2(float.NaN,float.NaN);
                Put(Lead,Floor(from));yield return Frames(2);Time.captureDeltaTime=1/60f;
                try
                {
                    for(int frame=0;frame<360;frame++)
                    {
                        if(heard.Contains("PipFirst")||DialogueSystem.Current.Running){var p=Lead.transform.position;opened[pass]=new Vector2(p.x,p.z);break;}
                        var go=to-new Vector2(Lead.transform.position.x,Lead.transform.position.z);Lead.motor.Move(go.magnitude<.05f?Vector2.zero:go.normalized,false,false);yield return null;
                    }
                }
                finally{Time.captureDeltaTime=0;Lead.motor.Move(Vector2.zero,false,false);}
                if(DialogueSystem.Current.Running){DialogueSystem.Current.Cancel();yield return Frames(2);}
            }
            float Near(int pass)=>Vector2.Distance(opened[pass],pips[pass]);
            Debug.Log($"PIP_FIRST_WALKS as it was (z {Was}): opened at z {opened[0].y:0.00}; at (8, {PipFirstAt.y}): up the line z {opened[1].y:0.00}, at Pip from the ramp's head {Near(2):0.00} m "
                +$"(z {opened[2].y:0.00}), from due south {Near(3):0.00} m (z {opened[3].y:0.00}); Pip at ({pips[1].x:0.00}, {pips[1].y:0.00})");
            Assert.IsFalse(float.IsNaN(opened[0].y),"as it was, the talk never opened up the route's line");
            Assert.That(opened[0].y,Is.InRange(28.0f,28.6f),"as it was, the talk didn't open in the gate's shadow at about z 28.3: the test isn't measuring what the still showed");
            for(int pass=1;pass<4;pass++)Assert.IsFalse(float.IsNaN(opened[pass].y),$"walk {pass}: the lead reached his goal and Pip's first word never opened");
            // He is seen whole from about z 29.4 (D444). The frame the crossing fires on moves his place by a frame's run, about .1 m: it read 29.72 alone (r8f-pip) and 29.596
            // in the whole suite (nov26c) against a bar of 29.6 that was the round's target, not the arch's shadow. The bar is 29.5.
            Assert.GreaterOrEqual(opened[1].y,29.5f,"up the route's line Pip's first word opens with the lead still under the gate's arch and towers");
            Assert.GreaterOrEqual(Near(2),.9f,"walked straight at Pip from the ramp's head, the lead is beside him before he speaks");
            Assert.GreaterOrEqual(Near(3),.9f,"walked at Pip from due south, the lead is beside him before he speaks");
        }

        [UnityTest] public IEnumerator OdrunsKitchenHasNoSignItsQuarterIsBehindItAndHerThingsAreOfferedOnlyForHerQuest()
        {
            InTown();yield return Zone();
            var odrun=Here("Odrun");Assert.AreEqual(1,odrun.Length);Assert.Less(Flat(odrun[0].transform.position,OdrunAt),1,"Odrun isn't at her range");
            Assert.AreEqual("OdrunFirst",odrun[0].firstNode);Assert.AreEqual("OdrunRepeat",odrun[0].repeatNode);Assert.AreEqual("odrun.kitchenStocked",odrun[0].postFlag);Assert.AreEqual("OdrunShop",odrun[0].postNode);
            foreach(var sign in Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None))StringAssert.DoesNotContain("KITCHEN",sign.text.ToUpperInvariant(),"the kitchen that doesn't exist has a sign");
            var brekk=Here("Brekk");Assert.AreEqual(1,brekk.Length,"Brekk isn't in the quarter");Assert.Less(Flat(brekk[0].transform.position,BrekkAt),1);Assert.AreEqual("BrekkKitchen",brekk[0].repeatNode);
            // The quarter is reached through the kitchen and no other way: the path to Brekk passes Odrun's door.
            var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(Floor(Arrival),Floor(BrekkAt+new Vector2(0,-1.6f)),NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"the quarter can't be reached");
            Assert.IsTrue(path.corners.Zip(path.corners.Skip(1),(a,b)=>(a,b)).Any(s=>DistanceToSegment(new Vector2(KitchenDoor.x,KitchenDoor.y),s.a,s.b)<1.6f),"the way to the hidden quarter doesn't go through Odrun's door");
            var wants=All<DiscoveryPoint>().Where(p=>p.report=="KitchenWant").ToArray();Assert.AreEqual(3,wants.Length,"Odrun's three things aren't in the town");
            foreach(var want in wants){Assert.IsFalse(want.Wanted,$"\"{want.prompt}\" is offered before Odrun has asked");Assert.IsNotNull(want.visual,$"\"{want.prompt}\" has nothing to see");}
            State.questSteps["OdrunsKitchen"]=0;yield return Frames(3);
            foreach(var want in wants){Assert.IsTrue(want.Wanted,$"\"{want.prompt}\" isn't offered for Odrun's quest");Assert.IsTrue(want.visual.activeInHierarchy,$"\"{want.prompt}\" can't be seen while it can be taken");}
            var tovra=All<Transform>().Single(t=>t.name=="Tovra at the kitchen table").gameObject;var table=Point("vask.kitchenTable");
            Assert.IsFalse(tovra.activeInHierarchy,"Tovra is at the kitchen table before the Warmaster has spoken");Assert.IsFalse(table.Wanted);
            Flag("vask.draskMet");yield return Frames(3);
            Assert.IsTrue(tovra.activeInHierarchy,"Tovra hasn't come in the back way once the Warmaster has spoken");Assert.IsTrue(table.Wanted,"the kitchen table isn't offered once Tovra is at it");Assert.AreEqual("OdrunTovra",table.dialogueNode);
        }
        static float DistanceToSegment(Vector2 p,Vector3 a,Vector3 b){var a2=new Vector2(a.x,a.z);var d=new Vector2(b.x,b.z)-a2;float t=Mathf.Clamp01(Vector2.Dot(p-a2,d)/Mathf.Max(.0001f,d.sqrMagnitude));return Vector2.Distance(p,a2+d*t);}

        [UnityTest] public IEnumerator TheGlassCasesOpenOncePipHasSaidWhereTheKeyIsOrTheTownHasAnswered()
        {
            InTown();Flag("vask.pipMet");yield return Zone();
            var gallery=Point("vask.gallerySeen");Assert.AreEqual("SelaTrophyGallery",gallery.dialogueNode);Assert.IsTrue(gallery.startsOnCrossing);
            var cases=All<DiscoveryPoint>().Where(p=>p.report=="GlassCase").ToArray();Assert.AreEqual(3,cases.Length,"Lanternwick's glass isn't in three cases");
            foreach(var c in cases){Assert.AreEqual("Open the case",c.prompt);Assert.AreEqual("glass",c.situation);Assert.AreEqual("RelayGlass",c.requiredQuest);Assert.IsNotNull(c.visual,"a case has no glass in it");}
            State.questSteps["RelayGlass"]=0;yield return Frames(3);
            foreach(var c in cases)Assert.IsFalse(c.Wanted,"a case opens before anyone has said where the key is");
            Flag("pip.galleryTalked");yield return Frames(3);
            Assert.IsTrue(Has("vask.casesOpen"),"Pip's word about the key doesn't open the cases");foreach(var c in cases)Assert.IsTrue(c.Wanted,"a case stays shut once Pip has said where the key is");
            Flag("pip.galleryTalked",false);Flag("vask.casesOpen",false);Flag("vask.deposed");yield return Frames(3);
            Assert.IsTrue(Has("vask.casesOpen"),"the cases stay shut after the town has answered");
        }

        [UnityTest] public IEnumerator TheWarmasterComesToHisTableOnceTheTownIsSeenAndGoesOutToTheFieldAfterHeHasSpoken()
        {
            InTown();yield return Zone();
            Assert.AreEqual(0,Here("Drask").Length,"the Warmaster receives anyone before they have seen the town");
            Flag("vask.odrunMet");yield return Frames(3);Assert.AreEqual(0,Here("Drask").Length,"the Warmaster is at his table with only the kitchen seen");
            Flag("vask.gallerySeen");yield return Frames(4);
            var drask=Here("Drask");Assert.AreEqual(1,drask.Length,"the Warmaster isn't at his table once the town is seen");Assert.Less(Flat(drask[0].transform.position,DraskAt),1);Assert.AreEqual("DraskReceives",drask[0].firstNode);
            Flag("vask.draskMet");yield return Zone();Assert.AreEqual(0,Here("Drask").Length,"the Warmaster is still at his table after he has sent them to the field");
        }

        // ================================================================== the duel ground (docs/maps/Harrowgate.md, "Duel ground contract")
        const string WarmasterOnHisPost="The Warmaster standing on his post";
        /// <summary>Whole-game capture of November 22, s52/014 and 017: the Warmaster stood on show on the field at F while he sat at his table in the hall, 22 m on, before the
        /// party had met him. He is on the field, with what stops a hero at his post, only once he has received them (vask.draskMet) and until the duel begins (D383).</summary>
        [UnityTest] public IEnumerator TheWarmasterIsAtHisTableOrOnTheFieldNeverBoth()
        {
            InTown();foreach(string f in new[]{"vask.odrunMet","vask.gallerySeen","vask.townSeen"})Flag(f);yield return Zone();
            yield return null;yield return null;
            System.Func<int> onShow=()=>All<DormantEnemy>().Count(d=>Flat(d.transform.position,F)<1.5f);
            System.Func<int> stops=()=>All<Collider>().Count(c=>c.name==WarmasterOnHisPost&&c.gameObject.activeInHierarchy);
            Assert.AreEqual(0,onShow(),"the Warmaster is on show on the field before he has received the party at his table");
            Assert.AreEqual(0,stops(),"something stops a hero at the Warmaster's post while he sits at his table");
            Flag("vask.draskMet");for(int i=0;i<5;i++)yield return null;
            Assert.AreEqual(1,onShow(),"the Warmaster isn't on show on the field once he has gone out to it");
            Assert.AreEqual(1,stops(),"nothing stops a hero walking into the Warmaster on his post once he has gone out to it");
            Flag("vask.fieldTaken");for(int i=0;i<5;i++)yield return null;
            Assert.AreEqual(0,stops(),"what stops a hero at the Warmaster's post stays once the duel has begun");
        }
        // ================================================================== nobody stands in anybody where the party meets them (D391)
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=Lead);
        /// <summary>Where the party meets the Warmaster at his table (HarrowgateBuilder.Town.MeetingGround): his place and the four metres in front of it, 2.2 m either side of his line.</summary>
        static readonly Rect MeetingGround=new(DraskAt.x-2.2f,DraskAt.y-4,4.4f,5);
        /// <summary>Route 52's stand for his talk (docs/quality/routes/chapter-52-*.json, "up to the oath hall 10/10": within .4 m of it).</summary>
        static readonly Vector2 DraskTalkStand=new(0,107.8f);
        /// <summary>How far what a body draws may stand inside a hero's capsule, or past its own solid: a few centimetres an idle moves.</summary>
        const float LiesIn=.05f;
        /// <summary>Every point a body draws this frame between its feet and a hero's head (1.9 m), but what it carries and its name: its skin as its bones hold it, a mesh where it hangs.</summary>
        static List<Vector3> Drawn(Component who)
        {
            var points=new List<Vector3>();var mesh=new List<Vector3>();
            foreach(var r in who.GetComponentsInChildren<Renderer>())
            {
                if(!r.enabled||!r.gameObject.activeInHierarchy||r.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly||r.name.EndsWith(" carries")||r.name.StartsWith("Sign_"))continue;
                if(r is SkinnedMeshRenderer skin&&skin.sharedMesh!=null)
                {var baked=new Mesh();skin.BakeMesh(baked,true);var pose=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);baked.GetVertices(mesh);foreach(var p in mesh)points.Add(pose.MultiplyPoint3x4(p));Object.Destroy(baked);}
                else if(r is MeshRenderer&&r.TryGetComponent<MeshFilter>(out var filter)&&filter.sharedMesh!=null){filter.sharedMesh.GetVertices(mesh);foreach(var p in mesh)points.Add(r.transform.TransformPoint(p));}
            }
            float feet=who.transform.position.y;points.RemoveAll(p=>p.y<feet+.05f||p.y>feet+1.9f);return points;
        }
        static float Reach(Component who){var points=Drawn(who);return points.Count==0?0:points.Max(p=>Flat(p,who.transform.position));}
        /// <summary>Its idle runs off screen too (a townsperson's rig stops animating off screen, and stands in its import pose).</summary>
        static void Animate(Component who){foreach(var rig in who.GetComponentsInChildren<Animator>())rig.cullingMode=AnimatorCullingMode.AlwaysAnimate;}
        static IEnumerator WalkTo(CombatActor lead,Vector2 point,System.Action each=null)
        {
            float began=Time.unscaledTime;
            while(Time.unscaledTime-began<8)
            {
                var to=new Vector3(point.x,0,point.y)-lead.transform.position;to.y=0;if(to.magnitude<.35f)break;
                lead.motor.Move(new Vector2(to.x,to.z).normalized,false,false);yield return null;each?.Invoke();
            }
            lead.motor.Move(Vector2.zero,false,false);
        }

        /// <summary>Whole-game capture of November 23, s51/179 and s52/016: at his table the Warmaster had a civilian's 0.35 m capsule under his war-frame and mantle, and the lead walked
        /// to 1.2 m of his middle, his head in the Warmaster's chest. He stops a hero at what he draws, the way his post on the field does (D371): walked at from the hall steps the lead
        /// stops in front of him, facing him, with his talk offered; and route 52's stand for the talk is clear of what stops a hero at him (D391).</summary>
        [UnityTest] public IEnumerator AtHisTableTheWarmasterStopsTheLeadAtWhatHeDrawsFacingHimWithHisTalkOffered()
        {
            InTown();foreach(string f in new[]{"vask.odrunMet","vask.gallerySeen"})Flag(f);yield return Zone();yield return Frames(4);
            var drask=Here("Drask").Single();var at=drask.transform.position;Animate(drask);yield return new WaitForSecondsRealtime(.4f);
            var lead=Lead;float leadR=lead.GetComponent<CharacterController>().radius;
            var stand=Floor(DraskTalkStand);
            var inTheWay=Physics.OverlapCapsule(stand+Vector3.up*(leadR+.06f),stand+Vector3.up*(1.8f-leadR),leadR,~0,QueryTriggerInteraction.Ignore).Where(c=>c.transform.IsChildOf(drask.transform)).Select(c=>c.name).ToArray();
            Assert.IsEmpty(inTheWay,$"route 52's stand for the Warmaster's talk, ({DraskTalkStand.x}, {DraskTalkStand.y}), is inside what stops a hero at him");
            PromptService.FindInteraction(stand);Assert.AreSame(drask,PromptService.Interaction,"the Warmaster's talk isn't the one offered at route 52's stand for it");
            foreach(var from in new[]{new Vector2(0,102.6f),new Vector2(-1.6f,103.4f),new Vector2(1.8f,103.6f)})
            {
                Put(lead,Floor(from));yield return Frames(2);float began=Time.unscaledTime,deepest=0;
                while(Time.unscaledTime-began<3.5f)
                {
                    var way=at-lead.transform.position;way.y=0;lead.motor.Move(new Vector2(way.x,way.z).normalized,false,false);yield return null;
                    deepest=Mathf.Max(deepest,BodiesKeepApartTests.Inside(Drawn(drask),lead));
                }
                lead.motor.Move(Vector2.zero,false,false);yield return Frames(3);
                var to=at-lead.transform.position;to.y=0;var facing=lead.transform.forward;facing.y=0;
                TestContext.WriteLine($"D391 the lead walked at the Warmaster from ({from.x}, {from.y}): stopped at ({lead.transform.position.x:0.00}, {lead.transform.position.z:0.00}), {to.magnitude:0.00} m from his middle; what he draws stood {deepest:0.000} m into the lead; he draws {Reach(drask):0.00} m out from his feet");
                Assert.LessOrEqual(deepest,LiesIn,$"walking at the Warmaster from ({from.x}, {from.y}) the lead stood {deepest:0.00} m inside what he draws");
                Assert.Less(lead.transform.position.z,at.z-.3f,"the lead isn't in front of the Warmaster where he stops");
                Assert.Less(Vector3.Angle(facing,to),35,"the lead doesn't face the Warmaster where he stops");
                PromptService.FindInteraction(lead.transform.position);Assert.AreSame(drask,PromptService.Interaction,$"where the lead stops at the Warmaster, {to.magnitude:0.00} m from him, his talk isn't offered");
            }
        }

        /// <summary>Whole-game capture of November 23, s51/179: Idra pressed into the Warmaster's back as the lead walked the portico past him (0.7 m from his middle from s51/160 to 179),
        /// drawn through him as a pale ghost. Her way went through him and his 0.35 m capsule held her there. Walked as route 52 comes up the steps to him and as route 51 walks the
        /// portico past him to the wall of names and back, what he draws is never in her, and wherever she comes to rest near him it is on the camera's side of him (D391).</summary>
        [UnityTest] public IEnumerator AtHisTableThePartnerNeverStandsInTheWarmasterAndRestsNearHimOnlyOnTheCamerasSide()
        {
            InTown();foreach(string f in new[]{"vask.odrunMet","vask.gallerySeen"})Flag(f);yield return Zone();yield return Frames(4);
            var drask=Here("Drask").Single();var at=drask.transform.position;Animate(drask);
            var lead=Lead;var partner=Partner;var camera=Camera.main;Assert.IsNotNull(camera,"fixture: the play camera");
            Put(lead,Floor(new Vector2(0,97.6f)));Put(partner,Floor(new Vector2(2,93.8f)));partner.GetComponent<PartnerBrain>().enabled=true;yield return Frames(2);
            float reach=Reach(drask),worst=0;string where="";var faults=new List<string>();
            void Judge(string leg){float deep=BodiesKeepApartTests.Inside(Drawn(drask),partner);if(deep>worst){worst=deep;where=$"{leg}: Idra at ({partner.transform.position.x:0.00}, {partner.transform.position.z:0.00}), lead at ({lead.transform.position.x:0.00}, {lead.transform.position.z:0.00})";}}
            foreach(var (leg,way,rest) in new (string,Vector2[],float)[]
            {
                ("up the hall steps to his talk (route 52)",new[]{new Vector2(0,107.44f)},4),
                ("along the portico to the oath hall's case (route 51)",new[]{new Vector2(.7f,107.8f),new Vector2(9,108.2f)},2.5f),
                ("back along the portico past him to the wall of names",new[]{new Vector2(-7.8f,108.2f)},5),
                ("back to him from the wall of names",new[]{new Vector2(0,108.2f),new Vector2(0,107.8f)},5),
            })
            {
                foreach(var point in way)yield return WalkTo(lead,point,()=>Judge(leg));
                float still=Time.unscaledTime;while(Time.unscaledTime-still<rest){yield return null;Judge(leg);}
                var her=partner.transform.position;float off=Flat(her,at);var view=camera.transform.forward;view.y=0;float behind=Vector3.Dot(her-at,view.normalized);
                TestContext.WriteLine($"D391 {leg}: the lead at ({lead.transform.position.x:0.00}, {lead.transform.position.z:0.00}), Idra at rest at ({her.x:0.00}, {her.z:0.00}), {off:0.00} m from the Warmaster and {behind:0.00} m behind him along the camera's view, {Flat(her,lead.transform.position):0.00} m from the lead");
                if(off<reach+2.5f&&behind>-.3f)faults.Add($"{leg}: Idra rests {off:0.00} m from the Warmaster and not on the camera's side of him ({behind:0.00} m behind him along its view)");
            }
            TestContext.WriteLine($"D391 the deepest the Warmaster's drawn body stood in Idra: {worst:0.000} m ({where}); he draws {reach:0.00} m out from his feet");
            Assert.LessOrEqual(worst,LiesIn,$"the partner stood {worst:0.00} m inside what the Warmaster draws ({where})");
            Assert.IsEmpty(faults,string.Join("; ",faults));
        }

        /// <summary>Whole-game capture of November 23: Pip, following the party, stood at the Warmaster's feet as the lead talked to him, the lead having walked back past him along the
        /// portico (s51/179), and the cook carrying a pot set off from 0.7 m behind Pip at Odrun's door as the lead talked to him (s52/233). No errand's or drill's loop crosses where
        /// the party meets the Warmaster or comes within 2.5 m of anyone the party talks to, no townsperson stands there, and Pip, following, walks off it (D391).</summary>
        [UnityTest] public IEnumerator NobodyWalksThroughOrStandsWhereThePartyMeetsTheWarmasterOrBesideAnyoneTheyTalkTo()
        {
            InTown();foreach(string f in new[]{"vask.odrunMet","vask.gallerySeen","vask.pipMet"})Flag(f);yield return Zone();yield return Frames(3);
            var faults=new List<string>();var meeting=new Rect(MeetingGround.x-.5f,MeetingGround.y-.5f,MeetingGround.width+1,MeetingGround.height+1);
            var named=All<Npc>().Select(n=>(who:n.speaker,at:new Vector2(n.transform.position.x,n.transform.position.z))).ToArray();
            foreach(var walk in All<WalkLoop>())for(int i=0;i<walk.path.Length-(walk.thereAndBack?1:0);i++)
            {
                var a=walk.path[i];var b=walk.path[(i+1)%walk.path.Length];
                for(float t=0;t<=1.001f;t+=.05f)
                {
                    var p=Vector3.Lerp(a,b,t);var plan=new Vector2(p.x,p.z);
                    if(meeting.Contains(plan))faults.Add($"{walk.name} walks across where the party meets the Warmaster");
                    foreach(var (who,at) in named)if(Vector2.Distance(plan,at)<2.5f)faults.Add($"{walk.name} walks within 2.5 m of {who} at ({at.x:0.0}, {at.y:0.0})");
                }
            }
            foreach(var one in All<PropTint>().Where(t=>t.name.StartsWith("Townsperson")||t.name.StartsWith("A child")))
                if(meeting.Contains(new Vector2(one.transform.position.x,one.transform.position.z)))faults.Add($"{one.name} stands where the party meets the Warmaster");
            var pip=All<KeepsPace>().Single(k=>k.name=="Pip");var lead=Lead;
            // Route 51 at the wall of names, Pip seven metres of path behind on the portico; then the lead walks back to the Warmaster's talk (s51/177 to 179).
            Put(lead,Floor(new Vector2(-7.8f,108.2f)));yield return Frames(2);pip.transform.position=Floor(new Vector2(-.8f,108));yield return new WaitForSecondsRealtime(.5f);
            yield return WalkTo(lead,new Vector2(0,108.2f));yield return WalkTo(lead,DraskTalkStand);yield return new WaitForSecondsRealtime(3);
            var stood=pip.transform.position;
            TestContext.WriteLine($"D391 Pip, following, stands at ({stood.x:0.00}, {stood.z:0.00}) as the lead talks to the Warmaster from ({lead.transform.position.x:0.00}, {lead.transform.position.z:0.00})");
            if(MeetingGround.Contains(new Vector2(stood.x,stood.z)))faults.Add($"Pip stands where the party meets the Warmaster, at ({stood.x:0.00}, {stood.z:0.00})");
            Assert.IsEmpty(faults.Distinct().ToArray(),string.Join("; ",faults.Distinct()));
        }

        /// <summary>Whole-game capture of November 23, s52/014: Idra walked through the drill sergeant on the muster medallion, two metres off the way up the field, where she walks
        /// beside the lead; he had nothing solid, and every garrison frame in town stood in a 0.7 m box whatever it drew. Everyone standing in Harrowgate stops a hero at what they draw
        /// (their solid holds their drawn body, idle, between the floor and a hero's head), and everyone walking has a solid (D391).</summary>
        [UnityTest] public IEnumerator EveryoneInHarrowgateStopsAHeroAtWhatTheyDraw()
        {
            InTown();foreach(string f in new[]{"vask.odrunMet","vask.gallerySeen"})Flag(f);yield return Zone();
            var standing=Live<PropTint>().Where(t=>(t.name.StartsWith("Townsperson")||t.name.StartsWith("A child"))&&t.GetComponent<WalkLoop>()==null).ToArray();
            foreach(var one in standing)Animate(one);yield return new WaitForSecondsRealtime(.6f);
            var faults=new List<string>();float worst=0;string worstWho="";
            foreach(var one in standing)
            {
                var solid=one.GetComponents<Collider>().FirstOrDefault(c=>c.enabled&&!c.isTrigger);var at=one.transform.position;
                if(solid==null){faults.Add($"{one.name} at ({at.x:0.0}, {at.z:0.0}) has nothing that stops a hero");continue;}
                float most=0;
                foreach(var p in Drawn(one))
                {
                    float past;
                    if(solid is BoxCollider box)
                    {
                        var local=box.transform.InverseTransformPoint(p)-box.center;var scale=box.transform.lossyScale;
                        past=Mathf.Max((Mathf.Abs(local.x)-box.size.x*.5f)*Mathf.Abs(scale.x),(Mathf.Abs(local.z)-box.size.z*.5f)*Mathf.Abs(scale.z));
                    }
                    else if(solid is CapsuleCollider capsule)past=Flat(p,capsule.transform.TransformPoint(capsule.center))-capsule.radius*Mathf.Abs(capsule.transform.lossyScale.x);
                    else continue;
                    most=Mathf.Max(most,past);
                }
                if(most>worst){worst=most;worstWho=$"{one.name} at ({at.x:0.0}, {at.z:0.0})";}
                if(most>LiesIn)faults.Add($"{one.name} at ({at.x:0.0}, {at.z:0.0}) draws {most:0.00} m past what stops a hero");
            }
            foreach(var walk in Live<WalkLoop>())if(!walk.GetComponents<Collider>().Any(c=>c.enabled&&!c.isTrigger))faults.Add($"{walk.name} walks with nothing that stops a hero");
            TestContext.WriteLine($"D391 {standing.Length} standing; the furthest anyone draws past their solid: {worst:0.000} m ({worstWho}); {faults.Count} faults");
            Assert.IsEmpty(faults,string.Join("; ",faults));
        }

        [UnityTest] public IEnumerator TheMusterFieldIsBuiltToTheDuelsContractAndIsACombatPlaceOnlyWhileTheDuelIsOwed()
        {
            InTown();foreach(string f in new[]{"vask.odrunMet","vask.gallerySeen","vask.townSeen","met.Drask"})Flag(f);yield return Zone();
            var ground=All<DuelGround>().Single();Assert.AreEqual("DraskVaal",ground.boss);Assert.Less(Flat(ground.transform.position,DuelMiddle),.3f,"the duel ground isn't at the field's middle");
            Assert.AreEqual(FieldHalfX,ground.half.x,.01f);Assert.AreEqual((FieldNorth-FieldSouth)*.5f,ground.half.y,.01f);
            Assert.IsNotNull(ground.guardPost);Assert.Less(Flat(ground.guardPost.position,GuardPost),.3f,"the guard wing's post isn't inside the kerb's north-east corner");
            Assert.IsTrue(ground.Holds(ground.guardPost.position),"the guard wing's post is outside the duel's floor");
            // Level and clear inside the kerb: nothing solid stands on it, and its floor is one height.
            var at=Floor(F);
            for(float x=-FieldHalfX+.8f;x<FieldHalfX;x+=2.4f)for(float z=FieldSouth+.8f;z<FieldNorth;z+=2.4f)
            {
                // The cadets at drill walk the field before the duel is owed, and the Warmaster stands on his post at F (checked below); anything else standing there is in the way.
                var hits=Physics.RaycastAll(new Vector3(x,Upper+6,z),Vector3.down,10,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<WalkLoop>()==null&&h.collider.name!=WarmasterOnHisPost).OrderBy(h=>h.distance).ToArray();
                Assert.IsNotEmpty(hits,$"no floor at ({x:0.0}, {z:0.0})");
                Assert.AreEqual(Upper,hits[0].point.y,.08f,$"something stands on the field or its floor isn't level at ({x:0.0}, {z:0.0}): {hits[0].collider.name}");
            }
            // Shown before the duel he is a dormant body with every collider off, and Taren walked into him (whole-game capture of November 22, s51/147): what stops a hero
            // stands on his post at F, no wider than he is, and only until the duel begins (D371).
            var standing=All<Collider>().Where(c=>c.name==WarmasterOnHisPost).ToArray();
            Assert.AreEqual(1,standing.Length,"nothing stops a hero walking into the Warmaster on his post before the duel");
            Assert.Less(Flat(standing[0].transform.position,F),.3f,"what stops a hero at the Warmaster isn't on his post at F");Assert.IsFalse(standing[0].isTrigger);
            Assert.LessOrEqual(Mathf.Max(standing[0].bounds.extents.x,standing[0].bounds.extents.z),.9f,"what stops a hero at the Warmaster is wider than he is");
            Assert.IsTrue(All<FlagGate>().Any(g=>g.flag=="vask.fieldTaken"&&!g.showWhenSet&&g.targets.Contains(standing[0].gameObject)),"what stops a hero at the Warmaster's post stays once the duel has begun");
            Assert.IsTrue(All<FlagGate>().Any(g=>g.flag=="vask.draskMet"&&g.showWhenSet&&g.targets.Any(t=>t!=null&&standing[0].transform.IsChildOf(t.transform))),"what stops a hero at the Warmaster's post is there before he has gone out to it");
            // Four openings, each its own seal, the encounter's when there is one.
            var seals=All<Membrane>().Where(m=>m.name.StartsWith("Muster field seal")).ToArray();Assert.AreEqual(4,seals.Length,"the field's four openings aren't each sealed");
            foreach(var seal in seals)Assert.IsTrue(seal.Open,$"{seal.name} is shut before the duel");
            var duel=All<EncounterVolume>().SingleOrDefault(e=>e.encounterId=="DraskVaal");
            if(GameCatalog.Find<EnemyDef>("DraskVaal")!=null)
            {
                Assert.IsNotNull(duel,"the field has no duel");Assert.AreEqual("vask.fieldTaken",duel.requiredFlag);Assert.AreEqual("Field",duel.checkpointSpawn);CollectionAssert.AreEquivalent(seals,duel.membranes);
                var post=duel.spawners.Single();Assert.Less(Flat(post.transform.position,F),.3f,"the Warmaster's post isn't F");Assert.AreEqual(180,post.transform.eulerAngles.y,1);Assert.IsTrue(post.faceAsPlaced);Assert.AreEqual(0,post.radius);
            }
            var spawn=All<SpawnPoint>().Single(s=>s.id=="Field");Assert.Less(Flat(spawn.transform.position,F),Flat(All<SpawnPoint>().Single(s=>s.id=="Arrival").transform.position,new Vector2(F.x,F.y)),"the checkpoint is no nearer the field than the arrival");
            Assert.Less(Flat(spawn.transform.position,F),60);Assert.IsTrue(NavMesh.SamplePosition(spawn.transform.position,out _,1.5f,NavMesh.AllAreas),"the checkpoint isn't on navigation");
            Assert.IsFalse(ground.Holds(spawn.transform.position),"the checkpoint is on the duel's floor");
            // Nothing tall between the fixed camera and anywhere the two fighters can be.
            for(float x=-FieldHalfX+1;x<FieldHalfX;x+=3)for(float z=FieldSouth+1;z<FieldNorth;z+=3)
            {
                var chest=new Vector3(x,Upper+SightlineAudit.Chest,z);var eye=chest+SightlineAudit.ToCamera*SightlineAudit.Distance;
                foreach(var block in Physics.RaycastAll(eye,chest-eye,SightlineAudit.Distance-SightlineAudit.NearIgnore,~0,QueryTriggerInteraction.Ignore))
                    if(block.collider.GetComponentInParent<WalkLoop>()==null&&block.collider.GetComponentInParent<CombatActor>()==null)Assert.Fail($"{block.collider.name} stands between the camera and a fighter at ({x:0}, {z:0})");
            }
            // A combat place only while the duel is owed, and only on the field.
            var pocket=All<FightPocket>().Single();yield return Frames(2);Assert.AreEqual(0,pocket.State,"the field is staged for the duel before the Warmaster has spoken");
            // Just inside the east opening: on the field, short of the Warmaster's talk (which would start the duel itself).
            var onField=new Vector2(FieldHalfX-.3f,85);
            Put(Lead,Floor(onField));yield return Frames(4);Assert.AreEqual(ZoneKind.GroundSafe,ZoneController.Current.Kind,"the field is a combat place before the duel is owed");
            Flag("vask.draskMet");Put(Lead,Floor(onField));yield return Frames(4);Assert.AreEqual(1,pocket.State);
            Assert.AreEqual(ZoneKind.GroundCombat,ZoneController.Current.Kind,"the field isn't a combat place while the duel is owed");
            Put(Lead,Floor(FieldSpawn));yield return Frames(4);Assert.AreEqual(ZoneKind.GroundSafe,ZoneController.Current.Kind,"the forecourt became a combat place");
            Flag("vask.duelWon");Put(Lead,Floor(onField));yield return Frames(4);Assert.AreEqual(2,pocket.State);Assert.AreEqual(ZoneKind.GroundSafe,ZoneController.Current.Kind,"the field is still a combat place after the duel");
        }

        [UnityTest] public IEnumerator TheTownWatchesTheDuelFromTheStepsAndTheKerbsAndTheGuardWingWaitsAtItsPost()
        {
            InTown();foreach(string f in new[]{"vask.odrunMet","vask.gallerySeen","vask.townSeen","met.Drask","vask.draskMet"})Flag(f);yield return Zone("Field");yield return Frames(3);
            var watching=Live<PropTint>().Where(t=>t.name=="Townsperson, watching the duel").ToArray();
            Assert.GreaterOrEqual(watching.Length,20,"fewer than twenty of the town watch the duel");
            var ground=All<DuelGround>().Single();
            foreach(var one in watching)Assert.IsFalse(ground.Holds(one.transform.position,-1),$"someone watches from inside the kerb at {one.transform.position}");
            Assert.Greater(watching.Count(w=>w.transform.position.z>FieldNorth+1),8,"the town isn't on the hall steps");
            Assert.Greater(watching.Count(w=>Mathf.Abs(w.transform.position.x)>FieldHalfX+1),8,"the town isn't along the kerbs");
            Assert.IsFalse(watching.Any(w=>w.transform.position.z<FieldSouth&&Mathf.Abs(w.transform.position.x)<FieldHalfX),"someone stands between the camera and the field");
            Transform[] Guard(string who)=>Live<Transform>().Where(t=>t.name.StartsWith("Townsperson")&&t.name.Contains(who)).ToArray();
            var arrek=Guard("Arrek").SingleOrDefault();var sool=Guard("Sool");
            Assert.IsNotNull(arrek,"Arrek isn't at the guard wing's post");Assert.AreEqual(1,sool.Length,"Sool isn't at the guard wing's post, or is there twice");
            Assert.Less(Flat(arrek.position,GuardPost),7,"the guard wing's front rank isn't by its post");
            Flag("vask.shout1");yield return Frames(3);Assert.IsNull(Guard("Arrek").SingleOrDefault(),"Arrek is still at the post after he has answered the first shout");
            Assert.IsFalse(Live<Transform>().Any(t=>t.name=="The Unsworn come in behind the steps"),"the smiths come in before the second shout");
            Flag("vask.shout2");yield return Frames(3);Assert.IsTrue(Live<Transform>().Any(t=>t.name=="The Unsworn come in behind the steps"),"the smiths don't come in behind the steps at the second shout");
            Flag("bossdown.DraskVaal");yield return Frames(3);Assert.AreEqual(1,Guard("Sool").Length,"Sool isn't where he stayed once the Warmaster has yielded");
            var pip=All<KeepsPace>().Single(k=>k.name=="Pip");Assert.AreEqual(2,pip.Current);Assert.Less(Flat(pip.transform.position,PipThirdFile),2,"Pip isn't in the third file by the drain");
        }

        [UnityTest] public IEnumerator AfterThePursuitTheTownStandsOnTheFieldAndOdrunAnswersAtOnce()
        {
            InTown();foreach(string f in new[]{"vask.odrunMet","vask.gallerySeen","vask.townSeen","met.Drask","vask.draskMet","vask.fieldTaken","bossdown.DraskVaal","clear.DraskVaal","vask.duelWon","vask.pursuitWon"})Flag(f);
            yield return Zone("Verdict");
            var field=All<Transform>().Single(t=>t.name=="The field after the pursuit").gameObject;Assert.IsTrue(field.activeInHierarchy,"the town isn't standing on the field after the pursuit");
            Assert.IsTrue(Live<Transform>().Any(t=>t.name=="The Warmaster's skiff, down"),"his skiff isn't down on his own field");
            DialogueSystem.Current.AutoAdvance=true;float deadline=Time.unscaledTime+6;while(!heard.Contains("OdrunVerdict")&&Time.unscaledTime<deadline)yield return null;
            Assert.Contains("OdrunVerdict",heard,"Harrowgate doesn't answer when the party comes down on the field");
            yield return Settle();Assert.IsTrue(Has("vask.deposed"),"the verdict didn't take his tabs");
        }

        // ================================================================== the end
        [UnityTest] public IEnumerator TheEndRunsThroughTheArmouryGateDownTheStairToTheCellsAndEveryRadio()
        {
            InTown();foreach(string f in new[]{"vask.odrunMet","vask.gallerySeen","vask.townSeen","met.Drask","vask.draskMet","vask.fieldTaken","bossdown.DraskVaal","clear.DraskVaal","vask.duelWon"})Flag(f);
            yield return Zone("Field");yield return Frames(3);
            Assert.IsFalse(Reached(CellFront),"the stair down to the cells can be walked before the town has answered");
            Flag("vask.pursuitWon");Flag("vask.deposed");yield return Zone("Field");yield return Frames(3);
            Assert.IsTrue(Reached(CellFront),"the cells can't be walked to once the town has answered");
            var hessa=Here("Hessa");Assert.AreEqual(1,hessa.Length,"Hessa isn't at the armoury gate, or is in two places");Assert.Less(Flat(hessa[0].transform.position,ArmouryGate),5,"Hessa isn't by the armoury gate");
            var gate=Point("vask.gateDenied");Assert.AreEqual("HessaArmouryGate",gate.dialogueNode);Assert.AreEqual("vask.deposed",gate.requiredFlag);
            var stair=Point("idra.lockCounted");Assert.AreEqual("IdraLockCounted",stair.dialogueNode);Assert.AreEqual("vask.gateDenied",stair.requiredFlag);Assert.Less(Flat(stair.transform.position,StairMid),1);
            var drask=Here("Drask");Assert.AreEqual(1,drask.Length,"the Warmaster isn't in his cell, or is in two places");Assert.Less(Flat(drask[0].transform.position,CellAt),1);
            Assert.AreEqual("vask.deposed",drask[0].finalFlag);Assert.AreEqual("DraskCustody",drask[0].finalNode);Assert.AreEqual("vask.orderSaid",drask[0].finalCompletionFlag);
            Assert.LessOrEqual(Vector3.Distance(Floor(CellFront),drask[0].transform.position),drask[0].range+.3f,"the Warmaster can't be talked to from outside his cell");
            var radio=All<AfterTalk>().Single(a=>a.node=="BrynNacreTraffic");Assert.AreEqual("vask.orderSaid",radio.flag);Assert.AreEqual("vask.nacreHeard",radio.heardFlag);
            var meret=All<AfterTalk>().Single(a=>a.node=="MeretAllStations");Assert.AreEqual("vask.nacreHeard",meret.flag);Assert.AreEqual("chapter8.complete",meret.heardFlag);
            Flag("chapter8.complete");yield return Zone();Assert.AreEqual(0,Here("Drask").Length,"the Warmaster is still in his cell once the chapter is over: he's aboard Many Hands");
        }

        // ================================================================== the Warmaster's own body, and the end at his cell (the look pass of November 21)
        /// <summary>What a body is drawn as: each renderer's mesh and material, under the person's model if there is one (an Npc's label is its own).</summary>
        static string[] Looks(GameObject body)
        {
            var model=body.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Generated civilian");var root=model!=null?model.gameObject:body;
            string Mesh(Renderer r)
            {
                var mesh=r is SkinnedMeshRenderer s?s.sharedMesh:r.TryGetComponent<MeshFilter>(out var f)?f.sharedMesh:null;
                return mesh!=null?mesh.name+" ("+mesh.vertexCount+")":"none";
            }
            return root.GetComponentsInChildren<Renderer>(true).Where(r=>!(r is ParticleSystemRenderer)&&!r.TryGetComponent<TMPro.TMP_Text>(out _))
                .Select(r=>Mesh(r)+" / "+(r.sharedMaterial!=null?r.sharedMaterial.name:"none")).OrderBy(n=>n).ToArray();
        }
        static float DrawnHeight(GameObject prefab,float scale)
        {
            var one=Object.Instantiate(prefab,new Vector3(0,-200,0),Quaternion.identity);one.transform.localScale*=scale;
            var parts=one.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)).ToArray();var all=parts[0].bounds;foreach(var p in parts)all.Encapsulate(p.bounds);
            Object.DestroyImmediate(one);return all.size.y;
        }
        [UnityTest] public IEnumerator TheWarmasterWearsOneBodyOfHisOwnAtHisTableInTheDuelAndInHisCellAndNotHisGuardsOrASmiths()
        {
            // s52/085: in the duel he was his guard wing's own armoured body, told apart only by the lock box; in town and in the cell (s51/149, s52/339) a smith's red apron.
            var duel=GameCatalog.Find<EnemyDef>("DraskVaal");var guard=GameCatalog.Find<EnemyDef>("MusterPikeguard");
            Assume.That(duel!=null&&duel.prefab!=null&&guard!=null&&guard.prefab!=null,"the duel's bodies aren't taken in");
            var his=Looks(duel.prefab);var theirs=Looks(guard.prefab);
            // A material of his own on the same shape is a recolour, which is what s52/085 showed: his silhouette has to have something theirs hasn't.
            string Shape(string look)=>look.Substring(0,look.IndexOf(" / ",System.StringComparison.Ordinal));
            Assert.IsTrue(his.Select(Shape).Except(theirs.Select(Shape)).Any(),"the Warmaster in the duel is his guard wing's shape recoloured: "+string.Join(", ",his));
            float tall=DrawnHeight(duel.prefab,duel.drawnScale>0?duel.drawnScale:1),guardTall=DrawnHeight(guard.prefab,guard.drawnScale>0?guard.drawnScale:1);
            Assert.Greater(tall,guardTall*1.15f,$"the Warmaster doesn't stand a head over his guard wing ({tall:0.00} m against {guardTall:0.00})");
            var smith=GameCatalog.Find<CharacterDef>("Brekk");var smithLooks=smith!=null&&smith.natural!=null?Looks(smith.natural):System.Array.Empty<string>();
            InTown();Flag("vask.odrunMet");Flag("vask.gallerySeen");yield return Zone();yield return Frames(4);
            var table=Here("Drask");Assert.AreEqual(1,table.Length,"the Warmaster isn't at his table once the town is seen");
            CollectionAssert.AreEquivalent(his,Looks(table[0].gameObject),"the Warmaster at his table isn't the body he fights in");
            Assert.IsFalse(Looks(table[0].gameObject).Intersect(smithLooks).Any(),"the Warmaster at his table wears a smith's body");
            foreach(string f in new[]{"vask.townSeen","met.Drask","vask.draskMet","vask.fieldTaken","bossdown.DraskVaal","clear.DraskVaal","vask.duelWon","vask.pursuitWon","vask.deposed"})Flag(f);
            yield return Zone("Field");yield return Frames(4);
            var cell=Here("Drask");Assert.AreEqual(1,cell.Length,"the Warmaster isn't in his cell");
            CollectionAssert.AreEquivalent(his,Looks(cell[0].gameObject),"the Warmaster in his cell isn't the body he fought in");
        }
        [UnityTest] public IEnumerator TheWarmasterStaysInHisCellWhileThePartyStandsThereAsTheChapterEndsAndHisGuardsStandClearOfThem()
        {
            InTown();foreach(string f in new[]{"vask.odrunMet","vask.gallerySeen","vask.townSeen","met.Drask","vask.draskMet","vask.fieldTaken","bossdown.DraskVaal","clear.DraskVaal","vask.duelWon","vask.pursuitWon","vask.deposed","vask.gateDenied","idra.lockCounted","vask.orderSaid","vask.nacreHeard"})Flag(f);
            yield return Zone("Field");Put(Lead,Floor(CellFront));yield return Frames(4);
            Assert.AreEqual(1,Here("Drask").Length,"the Warmaster isn't in his cell");
            // s52/339 to 370: the chapter's card came up and he was gone from behind his bars, the party standing in front of them.
            Flag("chapter8.complete");yield return Frames(6);
            Assert.AreEqual(1,Here("Drask").Length,"the Warmaster vanishes from his cell in front of the party as the chapter's card comes up");
            // s52/339: Idra stood inside the guard by the bench. Whoever keeps the cell stands well off where the party stands to talk through the bars.
            var guards=Live<Transform>().Where(t=>t.name=="Townsperson, an Unsworn smith on guard").ToArray();Assert.AreEqual(2,guards.Length,"the cell's two guards aren't there");
            foreach(var g in guards)Assert.Greater(Flat(g.position,CellFront),4,$"a guard at {g.position} stands where the party stands to talk to the Warmaster");
        }
        [UnityTest] public IEnumerator TheMusterFieldsMarkAndTheTownsWalksAreLaidInTexturedStone()
        {
            InTown();yield return Zone();
            // s51/180, s52/254: the field's mark was a flat white ring, grey wings and a white disc, and the walks a smooth lavender band: colour on a mesh, no stone in it.
            bool Over(Bounds b,float x0,float x1,float z0,float z1)=>b.max.x>x0&&b.min.x<x1&&b.max.z>z0&&b.min.z<z1;
            var marks=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("Floor detail")&&r.bounds.size.x>1&&r.bounds.size.z>1
                &&(Over(r.bounds,-FieldHalfX,FieldHalfX,FieldSouth,FieldNorth)||Over(r.bounds,-48,42,33.5f,36.5f)||Over(r.bounds,-48,39.6f,64.5f,67.5f))).ToArray();
            Assert.IsNotEmpty(marks,"nothing marks the field or the walks");
            foreach(var mark in marks)foreach(var material in mark.sharedMaterials)
                Assert.IsTrue(material!=null&&material.HasProperty("_BaseMap")&&material.GetTexture("_BaseMap")!=null,$"{mark.name} is drawn in flat colour ({(material!=null?material.name:"none")}), with no stone laid in it");
        }
        [UnityTest] public IEnumerator TheHarboursRippleIsWiderThanAFewPixelsAtThePlayCamera()
        {
            InTown();yield return Zone();
            // s51/107, s51/123: the water was a two-tone speckle like a dead channel's static. Its finer ripple (the shader's second octave, 1.9 times the first) was a seventh of a
            // metre, under eight pixels at the play camera; thresholded into glints it shimmered as grain.
            var water=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Single(r=>r.name=="Harbour water").sharedMaterial;
            var ground=GameCatalog.Find<CameraProfile>("Ground");Assert.IsNotNull(ground);
            float perMetre=1080/(2*ground.distance*Mathf.Tan(ground.fov*.5f*Mathf.Deg2Rad)),cell=1/(water.GetVector("_WaterParams").x*1.9f);
            Assert.GreaterOrEqual(cell*perMetre,24,$"the harbour's finer ripple is {cell*perMetre:0.0} px across at the play camera ({cell:0.00} m): it reads as static");
        }
        [UnityTest] public IEnumerator EveryoneStandingInHarrowgateFacesSomeoneOrSomething()
        {
            InTown();yield return Zone();yield return Frames(3);
            // s50/131, s50/190, s51/159: townsfolk stood facing open paving, nobody and nothing in front of them.
            // Whoever is watching the drill faces the drill, which is open ground: what they look at is the field, not something in front of them.
            var standing=Live<PropTint>().Where(t=>(t.name.StartsWith("Townsperson")||t.name.StartsWith("A child"))&&!t.name.Contains("watching")).ToArray();
            var people=standing.Select(s=>s.transform.position).Concat(Live<Npc>().Select(n=>n.transform.position)).Concat(Live<WalkLoop>().Select(w=>w.transform.position)).Concat(Live<KeepsPace>().Select(k=>k.transform.position)).ToArray();
            var faults=new List<string>();
            foreach(var one in standing)
            {
                var at=one.transform.position;var ahead=one.transform.forward;ahead.y=0;ahead.Normalize();
                bool someone=people.Any(p=>{var to=p-at;to.y=0;return to.magnitude>.3f&&to.magnitude<4.6f&&Vector3.Angle(ahead,to)<55;});
                bool something=false;
                foreach(float up in new[]{.5f,1.2f})foreach(var hit in Physics.RaycastAll(at+Vector3.up*up+ahead*.45f,ahead,4.2f,~0,QueryTriggerInteraction.Ignore))
                    if(!hit.collider.transform.IsChildOf(one.transform))something=true;
                if(!someone&&!something)faults.Add($"{one.name} at ({at.x:0.0}, {at.z:0.0}) facing {one.transform.eulerAngles.y:0}");
            }
            Assert.IsEmpty(faults,"people stand facing nothing: "+string.Join("; ",faults));
        }

        // ================================================================== people
        [UnityTest] public IEnumerator HarrowgateIsLivedInByVaskarInSeveralLooksWhoStandOffTheWaysAndNeverTwoAlikeSideBySide()
        {
            InTown();yield return Zone();
            var standing=Live<PropTint>().Where(t=>t.name.StartsWith("Townsperson")||t.name.StartsWith("A child")).ToArray();var walking=Live<WalkLoop>();
            Assert.GreaterOrEqual(standing.Length+walking.Length,24,"Harrowgate has fewer than twenty-four residents about");
            Assert.GreaterOrEqual(walking.Length,8,"most of Harrowgate stands about: fewer than eight residents walk an errand or a drill");
            string Coat(Component who){var model=who.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Generated civilian");var r=model!=null?model.GetComponentInChildren<SkinnedMeshRenderer>():null;
                return r!=null&&r.sharedMaterial!=null?r.sharedMaterial.name:"?";}
            var coats=standing.Select(s=>(at:s.transform.position,coat:Coat(s))).ToArray();
            Assert.GreaterOrEqual(coats.Select(c=>c.coat).Distinct().Count(),6,"Harrowgate's people wear fewer than six looks: "+string.Join(", ",coats.Select(c=>c.coat).Distinct()));
            for(int a=0;a<coats.Length;a++)for(int b=a+1;b<coats.Length;b++)
                if(Flat(coats[a].at,coats[b].at)<3.2f)Assert.AreNotEqual(coats[a].coat,coats[b].coat,$"two residents side by side at {coats[a].at} and {coats[b].at} wear the same look");
            foreach(var named in new[]{"Hessa","Odrun","Brekk"})foreach(var npc in Here(named))foreach(var (at,coat) in coats)
            {
                Assert.Greater(Flat(at,npc.transform.position),1.4f,$"a resident stands on top of {named}");
                var own=npc.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r=>r.sharedMaterial!=null?r.sharedMaterial.name:"").FirstOrDefault();
                if(coat==own)Assert.Greater(Flat(at,npc.transform.position),8,$"a resident in {named}'s own look stands {Flat(at,npc.transform.position):0.0} m from {named}");
            }
        }

        [UnityTest] public IEnumerator SavingInHarrowgateAndContinuingKeepsTheVouchedFlagAndThePlaceSavedAt()
        {
            InTown();yield return Zone();
            var frame=Live<RepairBay>().Single();Put(Lead,Floor(new Vector2(16,43.6f)));yield return Frames(2);frame.Interact();yield return Frames(4);
            string zone=State.zone;GameServices.Current.Saves.Save("autosave",State);
            SceneFlow.Current.LoadZone("Title");float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;yield return null;yield return null;
            var title=Object.FindFirstObjectByType<TitleScreen>();Assert.IsNotNull(title,"fixture: the title never came up");title.StartGame(true);yield return null;
            deadline=Time.unscaledTime+30;while((State.zone!=zone||SceneFlow.Current.Loading)&&Time.unscaledTime<deadline)yield return null;yield return new WaitForSecondsRealtime(1);
            Assert.AreEqual("Harrowgate",SceneFlow.Current.Zone);
            Assert.Less(Flat(Lead.transform.position,All<SpawnPoint>().Single(s=>s.id==frame.saveSpawn).transform.position),4,"Continue didn't come back where the armourer's frame saved");
            Assert.IsTrue(Reached(GateHead),"after a Continue the harbour gate is shut again");
        }
    }
}
