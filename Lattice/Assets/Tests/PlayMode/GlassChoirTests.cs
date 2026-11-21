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
    /// <summary>Chapter 7's dungeon, the Glass Choir (docs/maps/GlassChoir.md): the nave walked in silence to the first clamp, the stays that pin the body's ribs across the way and are eased
    /// by taking their weight (the zone's rule: each lets its rib go, opens the way and frees what it held), a stay kept in the save, the sentinels held in glass until their stay eases,
    /// the flags the talks and the record wall set, the engine hall built to the boss's contract, Ishen never in a fight, and no wall and no glass between the fixed camera and any fight.
    /// Flags are set directly where a conversation would set them: the script's nodes are tested by the dialogue tests.</summary>
    public sealed class GlassChoirTests
    {
        const string Choir="GlassChoir";
        // The builder's numbers (OrielBuilder.Choir), copied: the tests can't see the editor assembly.
        const float HallFloor=-10,HallRadius=18,ClampSeat=11,ClampBody=14;
        static readonly Vector2 C=new(0,292);
        static readonly (string flag,Vector2 seat,Vector2 before,Vector2 beyond)[] Stays=
        {
            ("oriel.stay1",new(-5.4f,162.4f),new(-2,160),new(-2,170)),
            ("oriel.stay2",new(-8.2f,201.8f),new(-3,200),new(-2,210)),
            ("oriel.stay3",new(-7.4f,245.4f),new(-3,244),new(-2,256)),
            ("oriel.stay4",new(2.2f,222.6f),new(2.4f,224),new(6.2f,228.2f)),
        };
        readonly List<string> log=new();
        void Heard(string text,string stack,LogType type){if(text.StartsWith("STAY_")||text.StartsWith("DISCOVERY")||text.StartsWith("BRIDLE_")||text.StartsWith("GUIDE_"))log.Add(text);}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter7");log.Clear();Application.logMessageReceived+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what this chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void GlassChoir(ContentRegistry.Book book)
        {
            // Nothing: the boss's sheet and the enemies' tells are the combat engineer's (OrielCombatTests), and no shared zone check needs an exemption here.
        }

        // ------------------------------------------------------------------ fixtures
        static void Set(params string[] keys){foreach(var key in keys)GameServices.Current.Flags.SetBool(key,true);}
        static bool Flag(string key)=>GameServices.Current.Flags.GetBool(key);
        static IEnumerator Zone(string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(Choir,spawn);float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,Choir+" never finished loading");Assert.AreEqual(Choir,SceneManager.GetActiveScene().name);
            Hold();yield return new WaitForSecondsRealtime(.6f);
        }
        /// <summary>The heroes stand where they are put and nobody wanders into a crossing.</summary>
        static void Hold(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;}}
        /// <summary>The ground under a plan point: the first solid thing a ray from above meets that isn't a seal's (a held rib's box, standing on the floor while it holds).</summary>
        static Vector3 Ground(Vector2 p)
        {
            var hits=Physics.RaycastAll(new Vector3(p.x,40,p.y),Vector3.down,90,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<Membrane>()==null&&!h.collider.isTrigger).OrderBy(h=>h.distance).ToArray();
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
        static BridleStay Stay(string flag)
        {var found=Object.FindObjectsByType<BridleStay>(FindObjectsSortMode.None).Where(s=>s.flag==flag).ToArray();Assert.AreEqual(1,found.Length,"the stay "+flag+" is not in the Choir exactly once");return found[0];}
        static DiscoveryPoint Point(System.Func<DiscoveryPoint,bool> which,string what)
        {var found=Object.FindObjectsByType<DiscoveryPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(which).ToArray();Assert.AreEqual(1,found.Length,what+" is not in the Choir exactly once");return found[0];}
        static EncounterVolume Encounter(string id)=>Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId==id);
        /// <summary>Waits for a stay to come to rest after its weight is taken.</summary>
        IEnumerator Slack(BridleStay stay,float seconds=8)
        {
            float deadline=Time.unscaledTime+seconds;
            while(!log.Any(l=>l=="STAY_SLACK "+stay.flag)&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.4f);
        }
        static void Put(Vector2 p){var lead=PartyController.Current.Active;var cc=lead.GetComponent<CharacterController>();cc.enabled=false;lead.transform.position=Ground(p)+Vector3.up*.05f;cc.enabled=true;}

        // ------------------------------------------------------------------ the nave
        [UnityTest] public IEnumerator TheNaveIsWalkedInSilenceToTheFirstClampAndTheFirstRibStopsTheWayDown()
        {
            yield return Zone();var start=Hero;
            foreach(var (what,at) in new[]{("the nave's start",new Vector2(-1,22)),("the first flight",new Vector2(-1,60)),("Sela's line",new Vector2(-1.5f,96)),("the clerks' chapel",new Vector2(-20,49)),
                ("the clerks' coffer",new Vector2(-20.4f,51)),("the first clamp's talk",new Vector2(-4.2f,137.5f)),("the clamp room's north",new Vector2(-6,140)),("the first stay's seat",Stays[0].seat)})
                Assert.IsTrue(Reach(start,at),"from the door nobody can walk to "+what+" at "+at);
            Assert.IsFalse(Reach(start,Stays[0].beyond),"the bay beyond the first rib can be walked to before its stay is eased");
            // The silence: nothing to fight, no bark and no voice on the way in until Sela's one line, well in.
            var nave=Point(p=>p.dialogueNode=="SelaGlassChoir","Sela's line in the nave");
            Assert.Greater(nave.transform.position.z,80,"Sela speaks before the body has been in view for a while");
            foreach(var fight in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))
                Assert.Greater(fight.GetComponent<BoxCollider>().bounds.min.z,nave.transform.position.z+20,fight.encounterId+" wakes in the nave's silence");
            foreach(var point in Object.FindObjectsByType<DiscoveryPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.transform.position.z<nave.transform.position.z+30))
                Assert.IsTrue(string.IsNullOrEmpty(point.situation),point.name+" barks in the nave's silence ("+point.situation+")");
            Assert.IsEmpty(Object.FindObjectsByType<RadioHail>(FindObjectsSortMode.None),"somebody hails over the Choir's silence");
            Assert.IsEmpty(Object.FindObjectsByType<DiscoveryPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.startsOnCrossing&&p.transform.position.z>nave.transform.position.z&&p.transform.position.z<120&&p!=nave).Select(p=>p.name),
                "a talk fills the quiet after Sela's line before the first clamp");
        }

        [UnityTest] public IEnumerator NobodySaysAnythingOnComingIntoTheChoir()
        {
            var spoken=new List<(string who,string line)>();void Heard(string who,string line)=>spoken.Add((who,line));
            var gap=typeof(BarkService).GetField("nextArrival",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);Assert.IsNotNull(gap,"fixture: the arrival's own gap can be cleared");
            var def=GameCatalog.Find<Lattice.Data.ZoneDef>(Choir);Assert.IsTrue(def.quietArrival,"the Choir's definition doesn't ask for a quiet arrival");
            bool ZoneBark()=>spoken.Any(s=>s.line==BarkService.Line(s.who,"zone"));
            BarkService.Spoken+=Heard;
            try
            {
                // The check has to be able to hear an arrival bark: the same zone with its quiet taken off says its line.
                def.quietArrival=false;gap.SetValue(null,0f);
                yield return Zone();yield return new WaitForSecondsRealtime(1);
                Assert.IsTrue(ZoneBark(),"fixture: the check can't hear an arrival bark");
                def.quietArrival=true;spoken.Clear();gap.SetValue(null,0f);
                yield return Zone();yield return new WaitForSecondsRealtime(1);
                Assert.IsFalse(ZoneBark(),"somebody speaks on coming into the Choir: "+string.Join("; ",spoken.Select(s=>s.who+": "+s.line)));
            }
            finally{def.quietArrival=true;BarkService.Spoken-=Heard;}
        }

        // ------------------------------------------------------------------ the stays: the zone's rule
        [UnityTest] public IEnumerator EachStayTakesItsWeightThenLetsItsRibGoAndTheWayOpens()
        {
            yield return Zone();var start=Hero;
            // The check has to be able to see a stay that doesn't let go: with its switch off, the first stay's flag leaves the rib across the way and the bay out of reach.
            var first=Stay("oriel.stay1");var blocks=first.blocks;var held=first.rib.rotation;
            first.enabled=false;Set("oriel.stay1");yield return new WaitForSecondsRealtime(4.5f);
            Assert.Less(Quaternion.Angle(first.rib.rotation,held),1,"fixture: with no switch the rib still moved");Assert.IsTrue(blocks.activeSelf,"fixture: with no switch the way still opened");
            Assert.IsFalse(Reach(start,Stays[0].beyond),"fixture: the check can't see a stay that doesn't let go");
            GameServices.Current.Flags.SetBool("oriel.stay1",false);first.enabled=true;yield return null;yield return null;
            Assert.IsFalse(first.Worked,"fixture: the stay is back to holding");
            for(int i=0;i<Stays.Length;i++)
            {
                var (flag,seat,before,beyond)=Stays[i];var stay=Stay(flag);
                Assert.IsTrue(Reach(start,seat),"nobody can walk to stay "+(i+1)+"'s seat once the way to it is open");
                Assert.IsFalse(Reach(start,beyond),"beyond stay "+(i+1)+"'s rib can be walked to while it holds");
                var hand=Point(p=>p.flag==flag,"stay "+(i+1)+"'s seat");Assert.AreEqual("Take the stay's weight",hand.prompt);Assert.AreSame(hand,stay.point,"stay "+(i+1)+"'s seat isn't its hand");
                Assert.Less(Vector2.Distance(new Vector2(hand.transform.position.x,hand.transform.position.z),seat),.5f,"stay "+(i+1)+"'s seat isn't where the map says");
                Assert.IsTrue(hand.Available,"stay "+(i+1)+"'s weight can't be taken");
                var restRotation=stay.rib.rotation;log.Clear();
                hand.Interact();Assert.IsTrue(Flag(flag),"taking stay "+(i+1)+"'s weight sets nothing");
                yield return new WaitForSecondsRealtime(.5f);
                // The pull first: the rib dips under the strain and the way stays shut.
                Assert.IsTrue(stay.Working,"stay "+(i+1)+" doesn't pull");Assert.IsTrue(stay.blocks.activeSelf,"stay "+(i+1)+"'s way opens before the stay has eased");
                Assert.IsTrue(log.Any(l=>l=="STAY_EASED "+flag),"stay "+(i+1)+" doesn't say it was taken");
                yield return Slack(stay);
                Assert.IsTrue(log.Any(l=>l=="STAY_SLACK "+flag),"stay "+(i+1)+"'s rib never comes to rest");
                Assert.Greater(Quaternion.Angle(stay.rib.rotation,restRotation),80,"stay "+(i+1)+"'s rib didn't lift out of the way");
                Assert.Less(Quaternion.Angle(stay.rib.rotation,stay.eased),1,"stay "+(i+1)+"'s rib stops short of where it rests");
                Assert.IsFalse(stay.blocks.activeSelf,"stay "+(i+1)+"'s way stays shut after its rib has lifted");
                yield return new WaitForSecondsRealtime(.3f);
                Assert.IsTrue(Reach(start,beyond),"beyond stay "+(i+1)+" can't be walked to once it has eased");
                // A lifted rib stands over the body, east of the walk: nothing of it hangs over a floor a hero walks.
                foreach(var drawn in stay.rib.GetComponentsInChildren<Renderer>())
                {
                    var b=drawn.bounds;
                    for(float y=b.min.y+2.5f;y<b.max.y;y+=1)foreach(float t in new[]{.2f,.5f,.8f})
                    {
                        var p=new Vector2(Mathf.Lerp(b.min.x,b.max.x,t),b.center.z);
                        if(Physics.Raycast(new Vector3(p.x,y,p.y),Vector3.down,out var hit,40,~0,QueryTriggerInteraction.Ignore)&&NavMesh.SamplePosition(hit.point,out var on,.3f,NavMesh.AllAreas)&&y-hit.point.y<6)
                            Assert.Fail("stay "+(i+1)+"'s lifted rib hangs "+(y-hit.point.y).ToString("0.0")+" m over walked floor at "+p);
                    }
                }
            }
        }

        [UnityTest] public IEnumerator AnEasedStayIsKeptInTheSaveAndLoadsWithNothingPlayed()
        {
            Set("oriel.choirSeen","oriel.releaseLearned","oriel.stay1","oriel.stay2");
            yield return Zone("Door");
            var saved=GameServices.Current.Saves.Load("autosave");Assert.AreEqual(Choir,saved.zone,"fixture: arriving autosaves");Assert.IsTrue(saved.flags["oriel.stay2"],"fixture: the save has stay two");
            // The check has to be able to see a save that lost its stays: the same save without them continues into a Choir whose ribs hold.
            var stripped=GameServices.Current.Saves.Load("autosave");stripped.flags.Remove("oriel.stay1");stripped.flags.Remove("oriel.stay2");GameServices.Current.Saves.Save("autosave",stripped);
            yield return Continue();
            Assert.IsTrue(Stay("oriel.stay2").blocks.activeSelf,"fixture: the check can't see a save that lost its stays");
            var kept=GameServices.Current.Saves.Load("autosave");kept.flags["oriel.stay1"]=kept.flags["oriel.stay2"]=true;kept.zone=Choir;kept.spawn="Door";GameServices.Current.Saves.Save("autosave",kept);
            log.Clear();yield return Continue();yield return new WaitForSecondsRealtime(.4f);
            foreach(string flag in new[]{"oriel.stay1","oriel.stay2"})
            {
                var stay=Stay(flag);
                Assert.Less(Quaternion.Angle(stay.rib.rotation,stay.eased),1,flag+"'s rib loads held");Assert.IsFalse(stay.blocks.activeSelf,flag+"'s way loads shut");Assert.IsFalse(stay.Working,flag+" plays its ease again on a load");
                Assert.IsTrue(stay.frees.All(g=>g==null||!g.activeSelf),flag+"'s cracked glass loads whole");
            }
            Assert.IsFalse(log.Any(l=>l.StartsWith("STAY_")),"a loaded stay plays its ease again: "+string.Join(", ",log));
            Assert.IsTrue(Stay("oriel.stay3").blocks.activeSelf,"a stay the save hasn't eased loads eased");
        }

        /// <summary>Title, then Continue from the autosave, as a player does.</summary>
        IEnumerator Continue()
        {
            SceneFlow.Current.LoadZone("Title");while(SceneFlow.Current.Loading)yield return null;yield return null;
            Object.FindFirstObjectByType<TitleScreen>().StartGame(true);
            float until=Time.realtimeSinceStartup+30;while(SceneFlow.Current.Loading&&Time.realtimeSinceStartup<until)yield return null;
            Assert.AreEqual(Choir,SceneFlow.Current.Zone,"the save doesn't resume in the Choir");Hold();yield return null;yield return null;
        }

        [UnityTest] public IEnumerator TheSentinelsStandHeldInTheirGlassUntilTheirStayEasesAndThenWake()
        {
            Set("oriel.choirSeen","oriel.releaseLearned","oriel.stay1");
            yield return Zone();
            var sentinels=Encounter("Choir_Sentinels");Assert.AreEqual("oriel.stay2",sentinels.requiredFlag,"the sentinels wake on something other than their stay");
            Assert.IsNotNull(sentinels.previewPoint,"the sentinels aren't shown in their glass before they wake");
            var stay=Stay("oriel.stay2");Assert.AreEqual(2,stay.frees.Length,"stay two doesn't hold the two casings");
            foreach(var spawner in sentinels.spawners)
                Assert.IsTrue(stay.frees.Any(g=>Vector2.Distance(new Vector2(g.GetComponent<Renderer>().bounds.center.x,g.GetComponent<Renderer>().bounds.center.z),new Vector2(spawner.transform.position.x,spawner.transform.position.z))<1),
                    "a sentinel stands outside the glass that holds it");
            Put(new Vector2(-5,190));yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsFalse(sentinels.Started,"the sentinels wake in the bay before their stay is eased");
            Assert.IsTrue(stay.frees.All(g=>g.activeSelf),"the casings are gone before the stay pulls");
            Put(Stays[1].seat);yield return new WaitForSecondsRealtime(.3f);
            Point(p=>p.flag=="oriel.stay2","stay two's seat").Interact();
            float until=Time.unscaledTime+4;while(!sentinels.Started&&Time.unscaledTime<until)yield return null;
            Assert.IsTrue(sentinels.Started,"easing stay two doesn't wake the sentinels it held");
            Assert.IsTrue(stay.frees.All(g=>!g.activeSelf),"the casings stand whole once the stay has pulled");
        }

        [UnityTest] public IEnumerator AClampTenderDrawsTheThirdStayTautAgainAndStoppedItEasesAgain()
        {
            Set("oriel.choirSeen","oriel.releaseLearned","oriel.stay1","oriel.stay2","oriel.stay3");
            yield return Zone("Door");yield return new WaitForSecondsRealtime(.4f);
            var stay=Stay("oriel.stay3");Assert.IsNotNull(stay.keeper,"the third stay has no keeper for its tenders to find");
            var tenders=Encounter("Choir_Tenders");Assert.AreEqual("oriel.stay3",tenders.requiredFlag,"the tenders wake on something other than the third stay");
            Assert.IsTrue(stay.keeper.wanted,"an eased stay doesn't ask its tenders to set it again");
            Assert.Less(Vector3.Distance(stay.keeper.Seat,stay.point.transform.position),.5f,"a tender works the stay somewhere other than its seat");
            // Measured from the gallery's side of the stay: the party itself stands on the landing (the door's spawn), already past it.
            var gallery=Ground(Stays[2].before);
            Assert.IsTrue(Reach(gallery,Stays[2].beyond),"fixture: the landing is open past an eased stay");
            // A tender at work: a body of the tender's kind (a stand-in while its definition isn't in) working the seat, frame after frame.
            var def=GameCatalog.Find<Lattice.Data.EnemyDef>("ClampTender")??GameCatalog.Find<Lattice.Data.EnemyDef>("Scrapmite");Assert.IsNotNull(def,"fixture: a tender's body to work it with");
            var tender=ActorFactory.Enemy(def,Ground(Stays[2].seat+new Vector2(1.2f,0)));tender.Passive=true;
            float until=Time.unscaledTime+8;log.Clear();
            while(stay.Shut<1&&Time.unscaledTime<until){stay.keeper.Tend(tender,Time.deltaTime);yield return null;}
            Assert.GreaterOrEqual(stay.Shut,1,"a tender at work never draws the stay taut again");Assert.IsTrue(log.Any(l=>l=="STAY_TENDED oriel.stay3"),"the stay doesn't say it is being set again");
            Assert.IsTrue(stay.blocks.activeSelf,"the way stays open once the stay is set again");Assert.Greater(Quaternion.Angle(stay.rib.rotation,stay.eased),80,"the rib didn't sink back across the way");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.GreaterOrEqual(stay.Shut,1,"a stay set again eases by itself while the tender that set it lives");
            Assert.IsFalse(Reach(gallery,Stays[2].beyond),"the landing can be walked to past a stay set again");
            // Stopped, the tender lets it go again.
            tender.Health.Receive(new DamagePacket{amount=99999,type=Lattice.Data.DamageType.Kinetic});
            until=Time.unscaledTime+8;while(stay.Shut>0&&Time.unscaledTime<until)yield return null;yield return new WaitForSecondsRealtime(.4f);
            Assert.AreEqual(0,stay.Shut,.01f,"the stay doesn't ease again once its tender is stopped");Assert.IsFalse(stay.blocks.activeSelf,"the way stays shut once the tender is stopped");
            Assert.IsTrue(Reach(gallery,Stays[2].beyond),"the landing can't be walked to once the tender is stopped");
        }

        // ------------------------------------------------------------------ what the script and the quest expect of the world
        [UnityTest] public IEnumerator TheTalksAndThePointsSetTheFlagsTheScriptAndTheQuestExpect()
        {
            yield return Zone();
            foreach(var (node,flag,needs,report,speaker) in new[]{("SelaGlassChoir","oriel.choirSeen","","Nave","Sela"),("IshenFirstClamp","oriel.releaseLearned","","FirstClamp","Taren"),
                ("WardenFirst","oriel.wardenMet","oriel.releaseLearned","","Warden")})
            {
                var point=Point(p=>p.dialogueNode==node,node);
                Assert.AreEqual(flag,point.flag,node+" sets the wrong flag");Assert.AreEqual(needs,point.requiredFlag??"",node+" waits on the wrong flag");Assert.AreEqual(report,point.report??"",node+" reports the wrong thing");
                Assert.IsTrue(point.startsOnCrossing&&point.TryGetComponent<BoxCollider>(out var box)&&box.isTrigger,node+" isn't a crossing");Assert.AreEqual(speaker,point.dialogueSpeaker,node+" is said by the wrong person");
            }
            var warden=Point(p=>p.dialogueNode=="WardenFirst","the Warden's door");Assert.AreEqual("Door",warden.saveSpawn,"the Warden's door doesn't save at the checkpoint");
            Assert.IsNotNull(warden.previewEncounter,"the Warden wakes before it has spoken");Assert.AreEqual("LastWarden",warden.previewEncounter.encounterId);
            var record=Point(p=>p.report=="RecordWall","the record wall");
            Assert.AreEqual(("IshenReads","Ishen","oriel.coronachHeard","oriel.wardenStoodDown","Look at the record wall"),(record.dialogueNode,record.dialogueSpeaker,record.flag,record.requiredFlag,record.prompt),"the record wall isn't the script's");
            Assert.IsFalse(record.Available,"the record wall can be read before the Warden has stood down");
            var asks=Object.FindObjectsByType<AfterTalk>(FindObjectsSortMode.None).SingleOrDefault(a=>a.node=="WardenAsks");
            Assert.IsNotNull(asks,"nothing plays WardenAsks once the four clamps let go");Assert.AreEqual(("bossdown.LastWarden","oriel.wardenStoodDown","Warden"),(asks.flag,asks.heardFlag,asks.speaker));
            var hands=Object.FindObjectsByType<DiscoveryPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.report=="Stay").ToArray();
            Assert.AreEqual(4,hands.Length,"the Choir doesn't have its four stays' seats");Assert.IsTrue(hands.All(h=>string.IsNullOrEmpty(h.dialogueNode)&&string.IsNullOrEmpty(h.situation)),"a stay's seat talks or barks");
            foreach(var id in new[]{"oriel.choir.clerks","oriel.choir.breach","oriel.choir.niche"})Assert.IsTrue(Object.FindObjectsByType<LootCache>(FindObjectsSortMode.None).Any(c=>c.cacheId==id),"the cache "+id+" is missing");
            Assert.GreaterOrEqual(Object.FindObjectsByType<MineNode>(FindObjectsSortMode.None).Where(m=>m.material=="SingingGlass").Sum(m=>m.amount),4,"the breach holds too little Singing Glass");
            // The one sign each the manifest gives the Choir, and no other.
            var signs=Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None).Select(t=>t.text.Trim()).Where(t=>t==t.ToUpperInvariant()&&t.Length>2).ToArray();
            foreach(var sign in new[]{"THE NAVE","FIRST CLAMP","ENGINE HALL"})CollectionAssert.Contains(signs,sign,"the Choir has no "+sign+" sign");
        }

        // ------------------------------------------------------------------ the engine hall and its door
        [UnityTest] public IEnumerator TheEngineHallIsBuiltToTheBossContract()
        {
            Set("oriel.choirSeen","oriel.releaseLearned","oriel.stay1","oriel.stay2","oriel.stay3");
            yield return Zone("Door");
            var duel=Encounter("LastWarden");
            var post=duel.spawners.Single();Assert.AreEqual(C.x,post.transform.position.x,.1f);Assert.AreEqual(C.y,post.transform.position.z,.1f,"the Warden's spawner isn't at C");
            Assert.IsTrue(post.faceAsPlaced);Assert.Less(Vector3.Angle(post.transform.forward,Vector3.back),5,"the Warden doesn't face the door");
            Assert.AreEqual(("oriel.wardenMet","Door",1),(duel.requiredFlag,duel.checkpointSpawn,duel.membranes.Length),"the door isn't the contract's");
            var box=duel.GetComponent<BoxCollider>();Assert.That(box.size.x,Is.InRange(36f,40f));Assert.That(box.size.z,Is.InRange(36f,40f));
            Assert.IsNotNull(duel.previewPoint,"the Warden isn't shown from the stair");Assert.GreaterOrEqual(duel.previewRadius,Vector3.Distance(duel.previewPoint.position,post.transform.position)-.5f,"the preview's radius doesn't take in C");
            Assert.Less(duel.previewPoint.position.z,C.y-HallRadius+2,"the Warden is shown from somewhere other than the stair");
            // The floor: level within r 17, nothing solid on it inside r 12 but the Warden (whose body after it stands down is the room's).
            float floor=Ground(C+new Vector2(3,0)).y;Assert.AreEqual(HallFloor,floor,.1f,"the hall's floor isn't at its level");
            for(float x=-17;x<=17;x+=1)for(float z=-17;z<=17;z+=1)
            {
                var off=new Vector2(x,z);if(off.magnitude>17)continue;var p=C+off;
                if(ClampBearingsAt(p,out _)<3.2f)continue;
                var at=Ground(p);Assert.AreEqual(floor,at.y,.3f,"the hall's floor isn't near level at "+p);
                if(off.magnitude<12&&off.magnitude>1.2f){Assert.IsTrue(Physics.Raycast(at+Vector3.up*6,Vector3.down,out var hit,8,~0,QueryTriggerInteraction.Ignore));Assert.AreEqual(at.y,hit.point.y,.05f,"something solid stands on the floor at "+p+": "+hit.collider.name);}
            }
            // The four clamps: a seat at r 11 on each bearing, on the paths and reachable from C, its body 3 m behind it at r 14 in a solid box, and open floor either side for a tender.
            var seats=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Clamp ")&&t.name.EndsWith(" seat")).ToArray();
            Assert.AreEqual(4,seats.Length,"the hall doesn't have four clamp seats");
            foreach(var (bearing,index) in new[]{(45f,1),(315f,2),(225f,3),(135f,4)})
            {
                var seat=seats.Single(s=>s.name=="Clamp "+index+" seat");var at=new Vector2(seat.position.x,seat.position.z);var want=Polar(bearing,ClampSeat);
                var clamp=seat.GetComponent<BridleClamp>();Assert.IsNotNull(clamp,"clamp "+index+"'s seat has no clamp on it");
                Assert.AreEqual((index,"LastWarden","oriel.clamp"+index),(clamp.index,clamp.settledBy,clamp.openedFlag),"clamp "+index+" isn't the contract's");
                Assert.IsNotNull(clamp.whole,"clamp "+index+" has no body shut");Assert.IsNotNull(clamp.onCatch,"clamp "+index+" has no body on its catch");Assert.IsNotNull(clamp.released,"clamp "+index+" has no body let go");
                Assert.IsTrue(clamp.whole.activeInHierarchy&&!clamp.onCatch.activeInHierarchy&&!clamp.released.activeInHierarchy,"clamp "+index+" doesn't load shut");
                Assert.Less(Vector2.Distance(at,want),.2f,"clamp "+index+"'s seat isn't at r 11 on bearing "+bearing);
                Assert.Less(Vector3.Angle(seat.forward,new Vector3(C.x-at.x,0,C.y-at.y)),5,"clamp "+index+"'s seat doesn't face C");
                Assert.IsTrue(NavMesh.SamplePosition(seat.position,out _,.6f,NavMesh.AllAreas),"clamp "+index+"'s seat isn't on the paths");
                Assert.IsTrue(Reach(Ground(C+new Vector2(0,-4)),at),"clamp "+index+"'s seat can't be walked to from the floor");
                var bodyAt=Polar(bearing,ClampBody);
                Assert.IsTrue(Physics.Raycast(new Vector3(bodyAt.x,HallFloor+8,bodyAt.y),Vector3.down,out var top,10,~0,QueryTriggerInteraction.Ignore)&&top.point.y>HallFloor+2,"clamp "+index+"'s body isn't solid at r 14");
                var side=new Vector2(-(want-C).normalized.y,(want-C).normalized.x);
                foreach(int s in new[]{-1,1})Assert.IsTrue(NavMesh.SamplePosition(Ground(want+side*s*2.5f),out _,.5f,NavMesh.AllAreas),"no open floor for a tender beside clamp "+index);
            }
            // The twelve fibres on the floor, spokes every 30 degrees from 15, r 2.6 to r 17.5.
            var fibres=Object.FindObjectsByType<ForceLine>(FindObjectsSortMode.None).Where(f=>f.name.StartsWith("Fibre ")).Select(f=>f.transform).ToArray();
            Assert.AreEqual(12,fibres.Length,"the hall doesn't have its twelve fibres");
            foreach(var fibre in fibres)
            {
                var at=new Vector2(fibre.position.x,fibre.position.z);Assert.AreEqual(2.6f,Vector2.Distance(at,C),.1f,fibre.name+" doesn't start at r 2.6");
                var end=fibre.Find("Fibre end");Assert.IsNotNull(end,fibre.name+" has no end");Assert.AreEqual(17.5f,Vector2.Distance(new Vector2(end.position.x,end.position.z),C),.1f,fibre.name+" doesn't end at r 17.5");
                var force=fibre.GetComponent<ForceLine>();Assert.IsNotNull(force,fibre.name+" carries no force");
                Assert.AreEqual(17.5f,Vector2.Distance(new Vector2(force.end.x,force.end.z),C),.1f,fibre.name+"'s force ends somewhere else");
                float bearing=Mathf.Repeat(Mathf.Atan2(at.x-C.x,at.y-C.y)*Mathf.Rad2Deg,360);int sector=bearing<90?1:bearing<180?4:bearing<270?3:2;
                Assert.AreEqual(sector,force.clamp,fibre.name+" is held by the wrong clamp");
            }
            // The checkpoint: outside the seal, on the paths, within 60 m, with the free cradle beside it; the seal open until the fight.
            var spawn=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Single(s=>s.id=="Door");var seal=duel.membranes[0];
            Assert.Less(spawn.transform.position.z,seal.transform.position.z-1,"the checkpoint is inside the seal");Assert.Less(Vector3.Distance(spawn.transform.position,seal.transform.position),60);
            Assert.IsTrue(NavMesh.SamplePosition(spawn.transform.position,out _,1.5f,NavMesh.AllAreas),"the checkpoint isn't on the paths");
            var bay=Object.FindObjectsByType<RepairBay>(FindObjectsSortMode.None).OrderBy(r=>Vector3.Distance(r.transform.position,spawn.transform.position)).First();
            Assert.IsTrue(bay.free&&bay.saveSpawn=="Door"&&Vector3.Distance(bay.transform.position,spawn.transform.position)<20,"there is no free cradle at the door");
            // On the landing itself (the console's pivot is above its foot): the paths within reach of it are the checkpoint's floor.
            Assert.IsTrue(NavMesh.SamplePosition(bay.transform.position,out var atBay,2,NavMesh.AllAreas)&&Mathf.Abs(atBay.position.y-spawn.transform.position.y)<.3f,
                "the door's cradle doesn't stand on the landing with the checkpoint");
            Assert.IsTrue(seal.Open,"the seal is shut before the fight");
            // After it stands down: its body is the room's, standing at C, shown only once the four have let go.
            var after=Object.FindObjectsByType<FlagGate>(FindObjectsInactive.Include,FindObjectsSortMode.None).SingleOrDefault(g=>g.flag=="bossdown.LastWarden"&&g.showWhenSet);
            Assert.IsNotNull(after,"the Warden's body isn't left standing once it has stood down");
            Assert.Less(Vector2.Distance(new Vector2(after.targets[0].transform.position.x,after.targets[0].transform.position.z),C),.2f,"the Warden stands down somewhere other than C");
            Assert.IsTrue(Reach(Ground(C+new Vector2(0,-4)),new Vector2(-13.9f,300)),"the record wall can't be walked to from the hall");
        }
        static Vector2 Polar(float bearing,float r)=>C+new Vector2(Mathf.Sin(bearing*Mathf.Deg2Rad),Mathf.Cos(bearing*Mathf.Deg2Rad))*r;
        /// <summary>How far a point is from the nearest clamp body (they stand outside r 12.5, and the floor round them isn't the arena's to judge).</summary>
        static float ClampBearingsAt(Vector2 p,out float bearing)
        {
            float best=float.PositiveInfinity;bearing=0;
            foreach(float b in new[]{45f,315f,225f,135f}){float d=Vector2.Distance(p,Polar(b,ClampBody));if(d<best){best=d;bearing=b;}}
            return best;
        }

        [UnityTest] public IEnumerator TheEngineHallDoorIsACheckpointAndShutsBehindTheParty()
        {
            Set("oriel.choirSeen","oriel.releaseLearned","oriel.stay1","oriel.stay2","oriel.stay3","oriel.wardenMet");
            yield return Zone("Door");
            var duel=Encounter("LastWarden");
            GameServices.Current.State.spawn="Arrival";duel.Begin();yield return null;
            Assert.AreEqual("Door",GameServices.Current.State.spawn,"starting the fight doesn't make the landing the checkpoint");
            Assert.AreEqual("Door",GameServices.Current.Saves.Load("autosave").spawn,"starting the fight doesn't save at the landing");
            Assert.IsFalse(duel.membranes[0].Open,"the seal doesn't shut behind the party");
        }

        // ------------------------------------------------------------------ Ishen
        [UnityTest] public IEnumerator IshenWalksWithThePartyStaysOnTheStairForTheFightAndReadsAtTheRecordWall()
        {
            yield return Zone();
            var ishen=Object.FindObjectsByType<KeepsPace>(FindObjectsSortMode.None).SingleOrDefault(k=>k.name.StartsWith("Ishen"));Assert.IsNotNull(ishen,"Ishen doesn't come into the Choir with the party");
            Assert.IsTrue(ishen.stages[0].follows,"Ishen doesn't walk with the party from the door");Assert.Greater(ishen.calm,8,"Ishen walks on into a fight");
            var duel=Encounter("LastWarden");var seal=duel.membranes[0].transform.position;
            var stair=ishen.stages.Single(s=>s.when=="oriel.wardenMet");
            Assert.Less(stair.point.z,seal.z-1,"Ishen's place in the fight is inside the seal, not on the stair");
            foreach(var fight in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Select(e=>e.GetComponent<BoxCollider>().bounds))
                Assert.IsFalse(fight.min.x<stair.point.x&&stair.point.x<fight.max.x&&fight.min.z<stair.point.z&&stair.point.z<fight.max.z,"Ishen waits out the fight inside a fight's box at "+stair.point);
            Assert.IsTrue(NavMesh.SamplePosition(stair.point,out _,1,NavMesh.AllAreas),"Ishen's place on the stair is off the paths");
            var reads=ishen.stages[^1];Assert.AreEqual("oriel.wardenStoodDown",reads.when,"Ishen goes to the record wall before the Warden has stood down");
            var wall=Point(p=>p.report=="RecordWall","the record wall");
            Assert.Less(Vector3.Distance(reads.point,wall.transform.position),3.5f,"Ishen reads somewhere other than the record wall");
            Assert.IsTrue(NavMesh.SamplePosition(reads.point,out _,1,NavMesh.AllAreas),"Ishen's place at the record wall is off the paths");
        }

        // ------------------------------------------------------------------ the camera
        [UnityTest] public IEnumerator NoWallGlassOrTallPieceStandsBetweenTheCameraAndAFight()
        {
            Set("oriel.choirSeen","oriel.releaseLearned","oriel.stay1","oriel.stay2","oriel.stay3","oriel.stay4");
            yield return Zone("Door");yield return new WaitForSecondsRealtime(5);
            // Glass is drawn and never solid, so give every see-through piece a collider of its own drawn shape for the length of the check: the rays have to meet the glass the camera would see through.
            var glass=new List<MeshCollider>();
            foreach(var drawn in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var look=drawn.sharedMaterial;if(look==null||look.renderQueue<=2500||!drawn.enabled||drawn.name.StartsWith("Floor detail"))continue;var filter=drawn.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
                var probe=new GameObject("Glass probe "+drawn.name);probe.transform.SetPositionAndRotation(drawn.transform.position,drawn.transform.rotation);probe.transform.localScale=drawn.transform.lossyScale;
                var collider=probe.AddComponent<MeshCollider>();collider.sharedMesh=filter.sharedMesh;glass.Add(collider);
            }
            Assert.Greater(glass.Count,4,"fixture: the Choir's glass was found");
            Physics.SyncTransforms();
            var toCamera=-(Quaternion.Euler(SightlineAudit.Pitch,SightlineAudit.Yaw,0)*Vector3.forward);var hidden=new List<string>();int samples=0;
            Assert.IsTrue(NavMesh.SamplePosition(Hero,out var party,3,NavMesh.AllAreas),"fixture: the party stands on the paths");var walk=new NavMeshPath();
            foreach(var fight in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))
            {
                var box=fight.GetComponent<BoxCollider>().bounds;
                for(float x=box.min.x+1;x<box.max.x;x+=2)for(float z=box.min.z+1;z<box.max.z;z+=2)
                {
                    if(!Physics.Raycast(new Vector3(x,box.max.y+8,z),Vector3.down,out var floor,40,~0,QueryTriggerInteraction.Ignore)||!NavMesh.SamplePosition(floor.point,out var on,.4f,NavMesh.AllAreas))continue;
                    if(Mathf.Abs(on.position.y-(box.center.y-1.5f))>1.6f)continue;
                    // A fight's floor is floor the party can walk onto: the level top behind a parapet is on the navigation as an island, but nobody can get up there.
                    if(!NavMesh.CalculatePath(party.position,on.position,NavMesh.AllAreas,walk)||walk.status!=NavMeshPathStatus.PathComplete)continue;
                    samples++;var chest=on.position+Vector3.up*SightlineAudit.Chest;
                    foreach(var hit in Physics.RaycastAll(chest,toCamera,SightlineAudit.Distance-SightlineAudit.NearIgnore,~0,QueryTriggerInteraction.Ignore))
                    {
                        if(hit.collider.GetComponentInParent<Health>()!=null||hit.collider.GetComponentInParent<CharacterController>()!=null)continue;
                        if(glass.Contains(hit.collider)){hidden.Add(fight.encounterId+" at ("+x.ToString("0")+", "+z.ToString("0")+") behind glass, "+hit.collider.name);break;}
                        // A piece that opens for a hero behind it (SceneryOcclusion: the contract's fade on the clamps) doesn't hide them. Glass and walls never fade.
                        if(hit.collider.GetComponentInParent<SceneryOcclusion>()!=null)continue;
                        var size=hit.collider.bounds.size;if(hit.collider.bounds.max.y-on.position.y<3||size.x<1.6f&&size.z<1.6f)continue;
                        hidden.Add(fight.encounterId+" at ("+x.ToString("0")+", "+z.ToString("0")+") behind "+hit.collider.name);break;
                    }
                }
            }
            foreach(var c in glass)Object.Destroy(c.gameObject);
            Assert.Greater(samples,250,"fixture: the fights' floors were sampled");
            Assert.IsEmpty(hidden,hidden.Count+" of "+samples+" fight floor points are hidden from the camera: "+string.Join("; ",hidden.Take(12)));
        }
    }
}
